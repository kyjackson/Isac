namespace Isac.Mobile.Services;

/// <summary>
/// Service for Bluetooth communication with the watch (Wear OS Data Layer API).
/// </summary>
public interface IBluetoothService
{
    /// <summary>
    /// Event raised when audio data is received from the watch.
    /// </summary>
    event Action<byte[]>? OnAudioReceived;

    /// <summary>
    /// Event raised when the Bluetooth connection state changes.
    /// </summary>
    event Action<bool>? OnConnectionStateChanged;

    /// <summary>
    /// Sends audio data to the connected watch.
    /// </summary>
    Task SendAudioAsync(byte[] audioData, CancellationToken ct = default);

    /// <summary>
    /// Disconnects from the watch.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether the service is currently connected to a watch.
    /// </summary>
    bool IsConnected { get; }
}
