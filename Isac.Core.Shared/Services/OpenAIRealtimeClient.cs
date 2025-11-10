using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Isac.Core.Shared.Client;
using Isac.Core.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Isac.Core.Shared.Services
{
    public class OpenAIRealtimeClient : IRealtimeClient, IDisposable
    {
        private readonly ILogger<OpenAIRealtimeClient> _logger;
        private readonly OpenAIRealtimeOptions _options;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed;

        public OpenAIRealtimeClient(ILogger<OpenAIRealtimeClient> logger, IOptions<OpenAIRealtimeOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();

                // Set headers for authentication
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_options.ApiKey}");
                _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

                var uri = new Uri($"wss://api.openai.com/v1/realtime?model={_options.Model}");
                await _webSocket.ConnectAsync(uri, cancellationToken);

                _logger.LogInformation("Connected to OpenAI Realtime API");

                // Send initial session configuration
                await ConfigureSessionAsync();

                // Start listening for messages
                _ = Task.Run(() => ListenForMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to OpenAI Realtime API");
                return false;
            }
        }

        private async Task ConfigureSessionAsync()
        {
            var sessionConfig = new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    instructions = "You are ISAC (Intelligent System Analytic Computer), the helpful AI assistant for Strategic Homeland Division agents. Be concise and direct in your responses.",
                    voice = _options.Voice,
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    input_audio_transcription = new
                    {
                        model = "whisper-1"
                    },
                    turn_detection = new
                    {
                        type = "server_vad",
                        threshold = 0.5,
                        prefix_padding_ms = 300,
                        silence_duration_ms = 500
                    },
                    tools = Array.Empty<object>(),
                    tool_choice = "auto",
                    temperature = 0.8,
                    max_response_output_tokens = "inf"
                }
            };

            var json = JsonSerializer.Serialize(sessionConfig);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogDebug("Session configured with voice: {Voice}", _options.Voice);
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var messageBuilder = new StringBuilder();

            try
            {
                while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(buffer.Array!, 0, result.Count);
                        messageBuilder.Append(text);

                        if (result.EndOfMessage)
                        {
                            var fullMessage = messageBuilder.ToString();
                            messageBuilder.Clear();
                            await HandleMessageAsync(fullMessage);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket connection closed by server");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in message listening loop");
            }
        }

        private async Task HandleMessageAsync(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (root.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();
                    _logger.LogDebug("Received message type: {Type}", type);

                    switch (type)
                    {
                        case "session.created":
                            _logger.LogInformation("Session created successfully");
                            break;

                        case "conversation.item.created":
                            if (root.TryGetProperty("item", out var item) && 
                                item.TryGetProperty("role", out var role) &&
                                role.GetString() == "assistant" &&
                                item.TryGetProperty("content", out var contentArray))
                            {
                                foreach (var content in contentArray.EnumerateArray())
                                {
                                    if (content.TryGetProperty("type", out var contentType) &&
                                        contentType.GetString() == "text" &&
                                        content.TryGetProperty("text", out var text))
                                    {
                                        var responseText = text.GetString();
                                        OnTextReceived?.Invoke(responseText ?? string.Empty);
                                    }
                                }
                            }
                            break;

                        case "response.audio.delta":
                            if (root.TryGetProperty("delta", out var delta))
                            {
                                var audioBase64 = delta.GetString();
                                if (!string.IsNullOrEmpty(audioBase64))
                                {
                                    var audioBytes = Convert.FromBase64String(audioBase64);
                                    OnAudioReceived?.Invoke(audioBytes);
                                }
                            }
                            break;

                        case "response.audio_transcript.delta":
                            if (root.TryGetProperty("delta", out var transcriptDelta))
                            {
                                var transcript = transcriptDelta.GetString();
                                OnTranscriptReceived?.Invoke(transcript ?? string.Empty);
                            }
                            break;

                        case "response.done":
                            _logger.LogDebug("Response completed");
                            break;

                        case "error":
                            if (root.TryGetProperty("error", out var error))
                            {
                                _logger.LogError("API Error: {Error}", error.ToString());
                                OnError?.Invoke($"API Error: {error}");
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message: {Message}", message);
            }
        }

        public async Task<bool> SendAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot send audio - WebSocket is not open");
                return false;
            }

            try
            {
                // Append audio to the current conversation item
                var audioAppend = new
                {
                    type = "input_audio_buffer.append",
                    audio = Convert.ToBase64String(audioData)
                };

                var json = JsonSerializer.Serialize(audioAppend);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                _logger.LogDebug("Sent {Bytes} bytes of audio data", audioData.Length);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send audio data");
                return false;
            }
        }

        public async Task<bool> CommitAudioAsync(CancellationToken cancellationToken = default)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot commit audio - WebSocket is not open");
                return false;
            }

            try
            {
                // Commit the audio buffer to trigger processing
                var commit = new
                {
                    type = "input_audio_buffer.commit"
                };

                var json = JsonSerializer.Serialize(commit);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                // Request a response
                var createResponse = new
                {
                    type = "response.create"
                };

                json = JsonSerializer.Serialize(createResponse);
                bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                _logger.LogDebug("Committed audio buffer and requested response");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit audio buffer");
                return false;
            }
        }

        public async Task<string?> SendTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot send text - WebSocket is not open");
                return null;
            }

            try
            {
                // Create a conversation item with text
                var conversationItem = new
                {
                    type = "conversation.item.create",
                    item = new
                    {
                        type = "message",
                        role = "user",
                        content = new[]
                        {
                            new
                            {
                                type = "input_text",
                                text = text
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(conversationItem);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                // Request a response
                var createResponse = new
                {
                    type = "response.create"
                };

                json = JsonSerializer.Serialize(createResponse);
                bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);

                _logger.LogDebug("Sent text message: {Text}", text);

                // The response will be handled asynchronously via events
                return "Response will be delivered via events";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send text message");
                return null;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", cancellationToken);
                    _logger.LogInformation("Disconnected from OpenAI Realtime API");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect");
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public event Action<string>? OnTextReceived;
        public event Action<byte[]>? OnAudioReceived;
        public event Action<string>? OnTranscriptReceived;
        public event Action<string>? OnError;

        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectAsync().GetAwaiter().GetResult();
                _disposed = true;
            }
        }
    }
}