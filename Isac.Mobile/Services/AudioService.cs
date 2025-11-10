namespace Isac.Mobile.Services;

/// <summary>
/// Stub implementation of IAudioService.
/// TODO: Implement platform-specific audio playback using MAUI audio APIs or platform-specific code.
/// </summary>
public class AudioService : IAudioService
{
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public async Task PlayAudioAsync(byte[] audioData, int sampleRate = 16000, CancellationToken ct = default)
    {
        _isPlaying = true;
        
        try
        {
            // TODO: Implement actual audio playback
            // For now, simulate playback with a delay based on audio length
            var durationMs = (audioData.Length / 2 / sampleRate) * 1000; // PCM16 = 2 bytes per sample
            System.Diagnostics.Debug.WriteLine($"[AudioService] Playing {audioData.Length} bytes at {sampleRate}Hz for ~{durationMs}ms (stub)");
            
            await Task.Delay((int)durationMs, ct);
        }
        finally
        {
            _isPlaying = false;
        }
    }

    public Task StopAsync()
    {
        _isPlaying = false;
        System.Diagnostics.Debug.WriteLine("[AudioService] Stopped (stub)");
        return Task.CompletedTask;
    }
}
