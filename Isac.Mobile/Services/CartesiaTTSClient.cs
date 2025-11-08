using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Isac.Core.Shared.Client;
using Isac.Core.Shared.Configuration;
using Isac.Core.Shared.Transport;

namespace Isac.Mobile.Services;

public class CartesiaTTSClient : ICartesiaTTSClient
{
    private readonly HttpClient _httpClient;
    private readonly CartesiaOptions _options;
    private const string BaseUrl = "https://api.cartesia.ai";

    public CartesiaTTSClient(HttpClient httpClient, CartesiaOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("Cartesia-Version", "2024-06-10");
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, string voiceId, CancellationToken ct = default)
    {
        var request = new
        {
            model_id = _options.Model,
            transcript = text,
            voice = new
            {
                mode = "id",
                id = voiceId
            },
            output_format = new
            {
                container = "raw",
                encoding = "pcm_s16le",
                sample_rate = 16000
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/tts/bytes", request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<string> CreateVoiceAsync(IReadOnlyList<VoiceSampleDescriptor> samples, string voiceName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        
        // Add voice name
        content.Add(new StringContent(voiceName), "name");
        content.Add(new StringContent("en"), "language");

        // Add audio samples
        for (int i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            var audioContent = new ByteArrayContent(sample.AudioBytes);
            audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(sample.MimeType);
            content.Add(audioContent, "samples", sample.FileName);
        }

        var response = await _httpClient.PostAsync("/voices/clone", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return result.GetProperty("id").GetString() ?? throw new Exception("Failed to get voice ID");
    }

    public async Task DeleteVoiceAsync(string voiceId, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/voices/{voiceId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
