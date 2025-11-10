namespace Isac.Mobile.Services;

/// <summary>
/// Stub implementation of IBluetoothService.
/// TODO: Implement actual Wear OS Data Layer API communication.
/// </summary>
public class BluetoothService : IBluetoothService
{
    public event Action<byte[]>? OnAudioReceived;
    public event Action<bool>? OnConnectionStateChanged;

    public bool IsConnected { get; private set; }

    public Task SendAudioAsync(byte[] audioData, CancellationToken ct = default)
    {
        // TODO: Implement Wear OS Data Layer API to send audio to watch
        // For now, just simulate success
        System.Diagnostics.Debug.WriteLine($"[BluetoothService] Sending {audioData.Length} bytes to watch (stub)");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        IsConnected = false;
        OnConnectionStateChanged?.Invoke(false);
        System.Diagnostics.Debug.WriteLine("[BluetoothService] Disconnected (stub)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Simulate receiving audio from watch (for testing).
    /// </summary>
    public void SimulateAudioReceived(byte[] audioData)
    {
        OnAudioReceived?.Invoke(audioData);
    }

    /// <summary>
    /// Simulate connection state change (for testing).
    /// </summary>
    public void SimulateConnection(bool connected)
    {
        IsConnected = connected;
        OnConnectionStateChanged?.Invoke(connected);
    }
}
