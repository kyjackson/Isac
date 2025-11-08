using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Isac.Core.Shared.Client;
using Isac.Core.Shared.Configuration;

namespace Isac.Mobile.Services;

public class OpenAIRealtimeClient : IRealtimeClient
{
    private readonly OpenAIRealtimeOptions _options;
    private ClientWebSocket? _webSocket;
    private readonly string _realtimeUrl = "wss://api.openai.com/v1/realtime";

    public OpenAIRealtimeClient(OpenAIRealtimeOptions options)
    {
        _options = options;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_options.ApiKey}");
        _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
        
        var uri = new Uri($"{_realtimeUrl}?model={_options.Model}");
        await _webSocket.ConnectAsync(uri, ct);
    }

    public async Task<string> SendAudioAndGetResponseAsync(byte[] audioData, int sampleRate, CancellationToken ct = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket not connected. Call ConnectAsync first.");
        }

        // Convert PCM16 audio to base64
        var base64Audio = Convert.ToBase64String(audioData);

        // Send session configuration
        var sessionConfig = new
        {
            type = "session.update",
            session = new
            {
                modalities = new[] { "text", "audio" },
                instructions = "You are ISAC, a helpful voice assistant. Provide concise, natural responses.",
                voice = _options.Voice,
                input_audio_format = "pcm16",
                output_audio_format = "pcm16",
                input_audio_transcription = new { model = "whisper-1" },
                turn_detection = new { type = "server_vad" }
            }
        };
        await SendMessageAsync(sessionConfig, ct);

        // Send audio buffer
        var audioMessage = new
        {
            type = "input_audio_buffer.append",
            audio = base64Audio
        };
        await SendMessageAsync(audioMessage, ct);

        // Commit audio buffer
        var commitMessage = new { type = "input_audio_buffer.commit" };
        await SendMessageAsync(commitMessage, ct);

        // Wait for response
        return await ReceiveTextResponseAsync(ct);
    }

    private async Task SendMessageAsync(object message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task<string> ReceiveTextResponseAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        var responseText = string.Empty;

        while (_webSocket!.State == WebSocketState.Open)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            // Look for response completion with text
            if (type == "response.done")
            {
                // Extract text from response
                if (root.TryGetProperty("response", out var response) &&
                    response.TryGetProperty("output", out var output) &&
                    output.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in output.EnumerateArray())
                    {
                        if (item.TryGetProperty("type", out var itemType) &&
                            itemType.GetString() == "message" &&
                            item.TryGetProperty("content", out var content) &&
                            content.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var contentItem in content.EnumerateArray())
                            {
                                if (contentItem.TryGetProperty("text", out var text))
                                {
                                    responseText += text.GetString();
                                }
                            }
                        }
                    }
                }
                break;
            }

            // Handle errors
            if (type == "error")
            {
                var error = root.GetProperty("error").GetProperty("message").GetString();
                throw new Exception($"OpenAI Realtime API error: {error}");
            }
        }

        return responseText;
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            _webSocket.Dispose();
            _webSocket = null;
        }
    }
}
