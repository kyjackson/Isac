using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddCors(o => o.AddPolicy("default", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddLogging();

// In-memory stores (very simple dictionaries for MVP)
var voiceProfiles = new Dictionary<string, string>(); // userId -> voiceProfileId

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("default");
app.UseHttpsRedirection();

// Ping endpoint
app.MapGet("/api/v1/ping", (HttpContext ctx) =>
{
    Log.Information("Ping requested from {RemoteIp}", ctx.Connection.RemoteIpAddress?.ToString());
    return Results.Json(new { status = "ok", time = DateTimeOffset.UtcNow });
})
.WithName("Ping");

// Query endpoint (multipart/form-data)
app.MapPost("/api/v1/query", async (HttpRequest request) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        if (!request.HasFormContentType) return Results.BadRequest("Expected multipart/form-data");
        var form = await request.ReadFormAsync();
        var userId = form["userId"].ToString();
        var deviceId = form["deviceId"].ToString();
        var file = form.Files.GetFile("audio");
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(deviceId) || file is null)
        {
            Log.Warning("Query invalid payload: userId={UserId}, deviceId={DeviceId}, hasFile={HasFile}", userId, deviceId, file != null);
            return Results.BadRequest("Missing userId/deviceId/audio");
        }
        Log.Information("Query received: user={UserId}, device={DeviceId}, size={Size}", userId, deviceId, file.Length);

        // Stub: ignore actual audio, return a small WAV placeholder
        byte[] wavBytes = GenerateSilenceWav(seconds:1, sampleRate:16000);
        var response = new Isac.Core.Shared.Transport.QueryResponse(
            AudioFormat: "audio/wav",
            AudioBytes: wavBytes,
            Text: "stub"
        );
        return Results.Json(response);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Query processing failed");
        return Results.Problem("Internal error");
    }
    finally
    {
        sw.Stop();
        Log.Information("Query handled in {ElapsedMs} ms", sw.ElapsedMilliseconds);
    }
});

// Voice enroll endpoint
app.MapPost("/api/v1/voice/enroll", (Isac.Core.Shared.Transport.VoiceEnrollRequest enrollRequest) =>
{
    if (string.IsNullOrWhiteSpace(enrollRequest.UserId))
    {
        Log.Warning("Enroll missing userId");
        return Results.BadRequest("Missing userId");
    }
    // Stub: generate dummy profile id
    var profileId = Guid.NewGuid().ToString("N");
    voiceProfiles[enrollRequest.UserId] = profileId;
    Log.Information("Voice enrolled for user {UserId} -> {ProfileId}", enrollRequest.UserId, profileId);
    var resp = new Isac.Core.Shared.Transport.VoiceEnrollResponse(profileId);
    return Results.Json(resp);
});

// Voice delete endpoint
app.MapDelete("/api/v1/voice", (string userId) =>
{
    var removed = voiceProfiles.Remove(userId);
    Log.Information("Voice delete user {UserId}, removed={Removed}", userId, removed);
    return Results.NoContent();
});

app.Run();

// Helper to generate minimal WAV (PCM 16-bit mono silence)
static byte[] GenerateSilenceWav(int seconds, int sampleRate)
{
    int samples = seconds * sampleRate;
    int dataSize = samples * 2; // 16-bit
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    // RIFF header
    bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
    bw.Write(36 + dataSize);
    bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
    // fmt chunk
    bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
    bw.Write(16); // PCM chunk size
    bw.Write((short)1); // format = PCM
    bw.Write((short)1); // channels
    bw.Write(sampleRate);
    bw.Write(sampleRate * 2); // byte rate
    bw.Write((short)2); // block align
    bw.Write((short)16); // bits per sample
    // data chunk
    bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
    bw.Write(dataSize);
    // silence samples
    for (int i = 0; i < samples; i++) bw.Write((short)0);
    bw.Flush();
    return ms.ToArray();
}
