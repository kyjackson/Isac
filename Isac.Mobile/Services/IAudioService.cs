namespace Isac.Mobile.Services;

/// <summary>
/// Service for audio playback on the phone.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Plays audio data on the device speaker.
    /// </summary>
    /// <param name="audioData">Raw PCM16 audio bytes</param>
    /// <param name="sampleRate">Sample rate in Hz (e.g., 16000)</param>
    Task PlayAudioAsync(byte[] audioData, int sampleRate = 16000, CancellationToken ct = default);

    /// <summary>
    /// Stops any currently playing audio.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Gets whether audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }
}
