using System.Threading;
using System.Threading.Tasks;

namespace Isac.Core.Shared.Client;

/// <summary>
/// Abstraction for OpenAI Realtime API WebSocket client.
/// Handles audio-to-text transcription and LLM reasoning with event-based responses.
/// </summary>
public interface IRealtimeClient
{
    /// <summary>
    /// Event raised when text response is received from the LLM.
    /// </summary>
    event Action<string>? OnTextReceived;

    /// <summary>
    /// Event raised when audio data is received (if audio output is enabled).
    /// </summary>
    event Action<byte[]>? OnAudioReceived;

    /// <summary>
    /// Event raised when transcript/transcription is received.
    /// </summary>
    event Action<string>? OnTranscriptReceived;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    event Action<string>? OnError;

    /// <summary>
    /// Connects to OpenAI Realtime API WebSocket.
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends audio data asynchronously (for streaming).
    /// </summary>
    /// <param name="audioData">Raw PCM16 audio bytes</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> SendAudioAsync(byte[] audioData, CancellationToken ct = default);

    /// <summary>
    /// Commits the audio buffer and requests a response.
    /// </summary>
    Task<bool> CommitAudioAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a text message and receives response via events.
    /// </summary>
    /// <param name="text">Text message to send</param>
    /// <param name="ct">Cancellation token</param>
    Task<string?> SendTextAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Closes the WebSocket connection.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether the client is currently connected.
    /// </summary>
    bool IsConnected { get; }
}
