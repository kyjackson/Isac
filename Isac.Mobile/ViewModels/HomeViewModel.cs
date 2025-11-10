using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Isac.Core.Shared.Client;
using Isac.Mobile.Services;
using Isac.Core.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Isac.Mobile.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly IBluetoothService _bluetoothService;
        private readonly IAudioService _audioService;
        private readonly IRealtimeClient _realtimeClient;
        private readonly ICartesiaTTSClient _cartesiaTTSClient;
        private readonly ILogger<HomeViewModel> _logger;

        private string _status = "Disconnected";
        private string _lastResponse = string.Empty;
        private bool _isConnected;
        private bool _isProcessing;

        public HomeViewModel(
            IBluetoothService bluetoothService,
            IAudioService audioService,
            IRealtimeClient realtimeClient,
            ICartesiaTTSClient cartesiaTTSClient,
            ILogger<HomeViewModel> logger)
        {
            _bluetoothService = bluetoothService;
            _audioService = audioService;
            _realtimeClient = realtimeClient;
            _cartesiaTTSClient = cartesiaTTSClient;
            _logger = logger;

            ConnectCommand = new Command(async () => await ConnectAsync());
            DisconnectCommand = new Command(async () => await DisconnectAsync());
            TestAPICommand = new Command(async () => await TestAPIAsync());

            // Subscribe to Realtime API events
            _realtimeClient.OnTextReceived += OnTextReceived;
            _realtimeClient.OnAudioReceived += OnAudioReceived;
            _realtimeClient.OnTranscriptReceived += OnTranscriptReceived;
            _realtimeClient.OnError += OnError;

            // Subscribe to Bluetooth events
            _bluetoothService.OnAudioReceived += OnWatchAudioReceived;
            _bluetoothService.OnConnectionStateChanged += OnBluetoothConnectionChanged;

            // Initialize connection on startup
            _ = InitializeAsync();
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastResponse
        {
            get => _lastResponse;
            set
            {
                if (_lastResponse != value)
                {
                    _lastResponse = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand TestAPICommand { get; }

        private async Task InitializeAsync()
        {
            try
            {
                // Check if API keys are configured
                var openAIKey = Preferences.Get("openai_api_key", string.Empty);
                var cartesiaKey = Preferences.Get("cartesia_api_key", string.Empty);

                if (string.IsNullOrEmpty(openAIKey) || string.IsNullOrEmpty(cartesiaKey))
                {
                    Status = "API keys not configured. Please go to Settings.";
                    return;
                }

                // Auto-connect to OpenAI Realtime API
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize");
                Status = $"Initialization failed: {ex.Message}";
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                Status = "Connecting to OpenAI...";
                IsProcessing = true;

                var connected = await _realtimeClient.ConnectAsync();
                if (connected)
                {
                    IsConnected = true;
                    Status = "Connected to OpenAI Realtime API";
                    _logger.LogInformation("Successfully connected to OpenAI Realtime API");

                    // Now connect to watch via Bluetooth
                    Status = "Waiting for watch connection...";
                    // Bluetooth connection will be initiated from watch
                }
                else
                {
                    Status = "Failed to connect to OpenAI API";
                    _logger.LogWarning("Failed to connect to OpenAI Realtime API");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error");
                Status = $"Connection error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task DisconnectAsync()
        {
            try
            {
                Status = "Disconnecting...";
                IsProcessing = true;

                await _realtimeClient.DisconnectAsync();
                await _bluetoothService.DisconnectAsync();

                IsConnected = false;
                Status = "Disconnected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disconnect error");
                Status = $"Disconnect error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task TestAPIAsync()
        {
            try
            {
                if (!IsConnected)
                {
                    await ConnectAsync();
                    if (!IsConnected) return;
                }

                Status = "Testing API...";
                IsProcessing = true;

                // Send a test message
                var testMessage = "Hello, this is a test message. Please respond briefly.";
                await _realtimeClient.SendTextAsync(testMessage);

                Status = "Test message sent. Waiting for response...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API test error");
                Status = $"Test error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void OnWatchAudioReceived(byte[] audioData)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogWarning("Received audio but not connected to OpenAI");
                    return;
                }

                Status = "Processing audio from watch...";
                IsProcessing = true;

                // Send audio to OpenAI Realtime API
                await _realtimeClient.SendAudioAsync(audioData);
                await _realtimeClient.CommitAudioAsync();

                Status = "Audio sent to OpenAI, waiting for response...";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process watch audio");
                Status = $"Audio processing error: {ex.Message}";
            }
        }

        private async void OnTextReceived(string text)
        {
            try
            {
                LastResponse = text;
                Status = "Generating voice response...";

                // Get Cartesia voice ID
                var voiceId = Preferences.Get("cartesia_voice_id", string.Empty);
                if (string.IsNullOrEmpty(voiceId))
                {
                    _logger.LogWarning("No voice ID configured, skipping TTS");
                    Status = "No voice configured for TTS";
                    return;
                }

                // Convert text to speech using Cartesia
                var audioData = await _cartesiaTTSClient.SynthesizeSpeechAsync(text, voiceId);
                if (audioData != null && audioData.Length > 0)
                {
                    Status = "Sending audio to watch...";
                    
                    // Send audio back to watch
                    await _bluetoothService.SendAudioAsync(audioData);
                    
                    // Also play on phone for testing
                    await _audioService.PlayAudioAsync(audioData);
                    
                    Status = "Response sent to watch";
                }
                else
                {
                    Status = "Failed to generate audio response";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process text response");
                Status = $"TTS error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void OnAudioReceived(byte[] audioData)
        {
            // This is audio directly from OpenAI (if using audio output mode)
            // For now, we're using text + Cartesia TTS instead
            _logger.LogDebug("Received {Bytes} bytes of audio from OpenAI", audioData.Length);
        }

        private void OnTranscriptReceived(string transcript)
        {
            _logger.LogDebug("Transcript: {Transcript}", transcript);
            // Could display this in UI if desired
        }

        private void OnError(string error)
        {
            _logger.LogError("OpenAI API error: {Error}", error);
            Status = $"API Error: {error}";
            IsProcessing = false;
        }

        private void OnBluetoothConnectionChanged(bool connected)
        {
            if (connected)
            {
                Status = IsConnected ? "Ready - Watch connected" : "Watch connected - Connecting to API...";
            }
            else
            {
                Status = IsConnected ? "Watch disconnected" : "Disconnected";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}