using System.Threading;
using System.Threading.Tasks;

namespace Isac.Core.Shared.Client;

/// <summary>
/// Abstraction for Cartesia TTS API client.
/// Handles text-to-speech with custom voice cloning.
/// </summary>
public interface ICartesiaTTSClient
{
    /// <summary>
    /// Converts text to speech using the configured custom voice.
    /// </summary>
    /// <param name="text">Text to synthesize</param>
    /// <param name="voiceId">Cartesia voice ID (custom cloned voice)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Audio bytes (WAV format)</returns>
    Task<byte[]> SynthesizeSpeechAsync(string text, string voiceId, CancellationToken ct = default);

    /// <summary>
    /// Creates a custom voice by uploading audio samples.
    /// </summary>
    /// <param name="samples">Voice sample descriptors with audio data</param>
    /// <param name="voiceName">Name for the custom voice</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Voice ID for future TTS requests</returns>
    Task<string> CreateVoiceAsync(IReadOnlyList<Transport.VoiceSampleDescriptor> samples, string voiceName, CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom voice.
    /// </summary>
    /// <param name="voiceId">Voice ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteVoiceAsync(string voiceId, CancellationToken ct = default);
}
