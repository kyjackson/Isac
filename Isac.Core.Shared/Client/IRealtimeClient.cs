using System.Threading;
using System.Threading.Tasks;

namespace Isac.Core.Shared.Client;

/// <summary>
/// Abstraction for OpenAI Realtime API WebSocket client.
/// Handles audio-to-text transcription and LLM reasoning.
/// </summary>
public interface IRealtimeClient
{
    /// <summary>
    /// Connects to OpenAI Realtime API WebSocket.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends audio data and receives text response from LLM.
    /// </summary>
    /// <param name="audioData">Raw PCM16 audio bytes</param>
    /// <param name="sampleRate">Audio sample rate (e.g., 16000)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Text response from LLM</returns>
    Task<string> SendAudioAndGetResponseAsync(byte[] audioData, int sampleRate, CancellationToken ct = default);

    /// <summary>
    /// Closes the WebSocket connection.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);
}
