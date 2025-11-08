using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Isac.Core.Shared.Configuration;
using Isac.Core.Shared.Transport;

namespace Isac.Core.Shared.Client;

public class IsacHttpClient : IIsacClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public IsacHttpClient(HttpClient http, IsacApiOptions options)
    {
        _http = http;
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _http.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        }
    }

    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync("api/v1/ping", ct).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<QueryResponse> SendQueryAsync(QueryRequest request, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(request.UserId), "userId");
        form.Add(new StringContent(request.DeviceId), "deviceId");
        var audioContent = new ByteArrayContent(request.Payload);
        audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.AudioFormat);
        form.Add(audioContent, "audio", "audio" + (request.AudioFormat switch { "audio/wav" => ".wav", _ => ".bin" }));

        using var resp = await _http.PostAsync("api/v1/query", form, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var model = await resp.Content.ReadFromJsonAsync<QueryResponse>(_jsonOptions, ct).ConfigureAwait(false);
        return model!;
    }

    public async Task<VoiceEnrollResponse> EnrollVoiceAsync(VoiceEnrollRequest request, CancellationToken ct = default)
    {
        // Simple JSON post for MVP
        using var resp = await _http.PostAsJsonAsync("api/v1/voice/enroll", request, _jsonOptions, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var model = await resp.Content.ReadFromJsonAsync<VoiceEnrollResponse>(_jsonOptions, ct).ConfigureAwait(false);
        return model!;
    }

    public async Task DeleteVoiceAsync(string userId, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/voice?userId={Uri.EscapeDataString(userId)}");
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
    }
}
