using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Isac.Core.Shared.Client;
using Isac.Core.Shared.Configuration;

namespace Isac.Mobile.Services;

public class OpenAIRealtimeClient : IRealtimeClient, IDisposable
{
    private readonly OpenAIRealtimeOptions _options;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _realtimeUrl = "wss://api.openai.com/v1/realtime";
    private bool _disposed;

    public event Action<string>? OnTextReceived;
    public event Action<byte[]>? OnAudioReceived;
    public event Action<string>? OnTranscriptReceived;
    public event Action<string>? OnError;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public OpenAIRealtimeClient(OpenAIRealtimeOptions options)
    {
        _options = options;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_options.ApiKey}");
            _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
            
            var uri = new Uri($"{_realtimeUrl}?model={_options.Model}");
            await _webSocket.ConnectAsync(uri, ct);

            // Send session configuration
            await ConfigureSessionAsync();

            // Start listening for messages
            _ = Task.Run(() => ListenForMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Connection failed: {ex.Message}");
            OnError?.Invoke($"Connection failed: {ex.Message}");
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
                instructions = "You are ISAC, a helpful voice assistant. Provide concise, natural responses.",
                voice = _options.Voice,
                input_audio_format = "pcm16",
                output_audio_format = "pcm16",
                input_audio_transcription = new { model = "whisper-1" },
                turn_detection = (object?)null // Disable server VAD - we'll manually control when to process audio
            }
        };
        await SendMessageAsync(sessionConfig);
        System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Session configured with manual turn detection");
    }

    private async Task ListenForMessagesAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(text);

                    if (result.EndOfMessage)
                    {
                        var fullMessage = messageBuilder.ToString();
                        messageBuilder.Clear();
                        HandleMessage(fullMessage);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Connection closed by server");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Listen error: {ex.Message}");
            OnError?.Invoke($"Listen error: {ex.Message}");
        }
    }

    private void HandleMessage(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var type = typeElement.GetString();
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Received: {type}");

            switch (type)
            {
                case "session.created":
                    System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Session created");
                    break;

                case "response.text.delta":
                    if (root.TryGetProperty("delta", out var textDelta))
                    {
                        var text = textDelta.GetString();
                        if (!string.IsNullOrEmpty(text))
                            OnTextReceived?.Invoke(text);
                    }
                    break;

                case "response.text.done":
                    if (root.TryGetProperty("text", out var finalText))
                    {
                        var text = finalText.GetString();
                        if (!string.IsNullOrEmpty(text))
                            OnTextReceived?.Invoke(text);
                    }
                    break;

                case "conversation.item.created":
                    if (root.TryGetProperty("item", out var item) &&
                        item.TryGetProperty("role", out var role) &&
                        role.GetString() == "assistant")
                    {
                        if (item.TryGetProperty("content", out var contentArray))
                        {
                            foreach (var content in contentArray.EnumerateArray())
                            {
                                if (content.TryGetProperty("type", out var contentType) &&
                                    contentType.GetString() == "text" &&
                                    content.TryGetProperty("text", out var text))
                                {
                                    var responseText = text.GetString();
                                    if (!string.IsNullOrEmpty(responseText))
                                        OnTextReceived?.Invoke(responseText);
                                }
                            }
                        }
                    }
                    break;

                case "response.audio.delta":
                    if (root.TryGetProperty("delta", out var audioDelta))
                    {
                        var audioBase64 = audioDelta.GetString();
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
                        if (!string.IsNullOrEmpty(transcript))
                        {
                            OnTranscriptReceived?.Invoke(transcript);
                            // DON'T send individual deltas as text - only the final transcript
                        }
                    }
                    break;

                case "response.audio_transcript.done":
                    if (root.TryGetProperty("transcript", out var fullTranscript))
                    {
                        var transcript = fullTranscript.GetString();
                        if (!string.IsNullOrEmpty(transcript))
                        {
                            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Audio transcript: {transcript}");
                            OnTranscriptReceived?.Invoke(transcript);
                            // DON'T send transcripts as text - transcripts are what the user said, not the AI response
                            // OnTextReceived?.Invoke(transcript);  // ? REMOVED - This was causing duplicate speech
                        }
                    }
                    break;

                case "conversation.item.input_audio_transcription.failed":
                    System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Transcription failed - audio might be unclear");
                    if (root.TryGetProperty("error", out var transcriptError))
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Transcription error: {transcriptError}");
                    }
                    // Send a placeholder message
                    OnTextReceived?.Invoke("I heard something but couldn't understand it clearly. Could you try again?");
                    break;

                case "response.done":
                    System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Response completed");
                    // Check if we have any content in the response
                    if (root.TryGetProperty("response", out var response))
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Response data: {response}");
                        
                        if (response.TryGetProperty("output", out var outputArray))
                        {
                            bool hasTextContent = false;
                            foreach (var output in outputArray.EnumerateArray())
                            {
                                if (output.TryGetProperty("type", out var outputType))
                                {
                                    var outputTypeStr = outputType.GetString();
                                    System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Output type: {outputTypeStr}");
                                    
                                    // Extract text from message content
                                    if (outputTypeStr == "message" && 
                                        output.TryGetProperty("content", out var contentArray))
                                    {
                                        foreach (var contentItem in contentArray.EnumerateArray())
                                        {
                                            if (contentItem.TryGetProperty("type", out var contentType))
                                            {
                                                var contentTypeStr = contentType.GetString();
                                                System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Content type: {contentTypeStr}");
                                                
                                                // For audio content, use the transcript
                                                if (contentTypeStr == "audio" && 
                                                    contentItem.TryGetProperty("transcript", out var transcript))
                                                {
                                                    var responseText = transcript.GetString();
                                                    if (!string.IsNullOrEmpty(responseText))
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Extracted AI response: {responseText}");
                                                        OnTextReceived?.Invoke(responseText);
                                                        hasTextContent = true;
                                                    }
                                                }
                                                // For text content, use the text property
                                                else if (contentTypeStr == "text" && 
                                                         contentItem.TryGetProperty("text", out var text))
                                                {
                                                    var responseText = text.GetString();
                                                    if (!string.IsNullOrEmpty(responseText))
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Extracted AI response: {responseText}");
                                                        OnTextReceived?.Invoke(responseText);
                                                        hasTextContent = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            
                            if (!hasTextContent)
                            {
                                System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] No text content in response");
                                OnTextReceived?.Invoke("I couldn't generate a response. Please try speaking more clearly.");
                            }
                        }
                    }
                    break;

                case "error":
                    if (root.TryGetProperty("error", out var error))
                    {
                        var errorMsg = error.TryGetProperty("message", out var msg) 
                            ? msg.GetString() 
                            : error.ToString();
                        OnError?.Invoke($"API Error: {errorMsg}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Handle message error: {ex.Message}");
        }
    }

    public async Task<bool> SendAudioAsync(byte[] audioData, CancellationToken ct = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Cannot send audio - WebSocket not connected");
            return false;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Sending {audioData.Length} bytes of audio ({audioData.Length / 48000.0:F2}s at 24kHz PCM16)");
            
            var audioMessage = new
            {
                type = "input_audio_buffer.append",
                audio = Convert.ToBase64String(audioData)
            };
            await SendMessageAsync(audioMessage, ct);
            
            System.Diagnostics.Debug.WriteLine("[OpenAIRealtimeClient] Audio sent successfully");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Send audio error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CommitAudioAsync(CancellationToken ct = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
            return false;

        try
        {
            var commitMessage = new { type = "input_audio_buffer.commit" };
            await SendMessageAsync(commitMessage, ct);

            var createResponse = new { type = "response.create" };
            await SendMessageAsync(createResponse, ct);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Commit audio error: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> SendTextAsync(string text, CancellationToken ct = default)
    {
        if (_webSocket?.State != WebSocketState.Open)
            return null;

        try
        {
            var conversationItem = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new { type = "input_text", text = text }
                    }
                }
            };
            await SendMessageAsync(conversationItem, ct);

            var createResponse = new { type = "response.create" };
            await SendMessageAsync(createResponse, ct);

            return "Response will be delivered via events";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Send text error: {ex.Message}");
            return null;
        }
    }

    private async Task SendMessageAsync(object message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        try
        {
            _cancellationTokenSource?.Cancel();

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenAIRealtimeClient] Disconnect error: {ex.Message}");
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DisconnectAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}
