using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Isac.Core.Shared.Configuration;
using Isac.Core.Shared.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Isac.Mobile.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private string _openAIKey = string.Empty;
        private string _cartesiaKey = string.Empty;
        private string _voiceId = string.Empty;
        private string _statusMessage = string.Empty;

        public SettingsViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            SaveCommand = new Command(async () => await SaveSettingsAsync());
            ClearCommand = new Command(ClearSettings);
            
            // Load existing settings
            LoadSettings();
        }

        public string OpenAIKey
        {
            get => _openAIKey;
            set
            {
                if (_openAIKey != value)
                {
                    _openAIKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CartesiaKey
        {
            get => _cartesiaKey;
            set
            {
                if (_cartesiaKey != value)
                {
                    _cartesiaKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VoiceId
        {
            get => _voiceId;
            set
            {
                if (_voiceId != value)
                {
                    _voiceId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }

        private void LoadSettings()
        {
            // Load from secure storage
            OpenAIKey = Preferences.Get("openai_api_key", string.Empty);
            CartesiaKey = Preferences.Get("cartesia_api_key", string.Empty);
            VoiceId = Preferences.Get("cartesia_voice_id", string.Empty);
            
            // If not in preferences, try environment variables (for development)
            if (string.IsNullOrEmpty(OpenAIKey))
                OpenAIKey = Environment.GetEnvironmentVariable("isac_ai_api_key") ?? string.Empty;
            if (string.IsNullOrEmpty(CartesiaKey))
                CartesiaKey = Environment.GetEnvironmentVariable("isac_api_key") ?? string.Empty;
            if (string.IsNullOrEmpty(VoiceId))
                VoiceId = Environment.GetEnvironmentVariable("voice_id") ?? string.Empty;
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(OpenAIKey))
                {
                    StatusMessage = "OpenAI API key is required";
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(CartesiaKey))
                {
                    StatusMessage = "Cartesia API key is required";
                    return;
                }

                // Save to secure storage
                Preferences.Set("openai_api_key", OpenAIKey.Trim());
                Preferences.Set("cartesia_api_key", CartesiaKey.Trim());
                Preferences.Set("cartesia_voice_id", VoiceId.Trim());
                
                // Update the service configurations
                var openAIOptions = _serviceProvider.GetService<IOptionsMonitor<OpenAIRealtimeOptions>>();
                var cartesiaOptions = _serviceProvider.GetService<IOptionsMonitor<CartesiaOptions>>();
                
                if (openAIOptions != null)
                {
                    openAIOptions.CurrentValue.ApiKey = OpenAIKey.Trim();
                }
                
                if (cartesiaOptions != null)
                {
                    cartesiaOptions.CurrentValue.ApiKey = CartesiaKey.Trim();
                    cartesiaOptions.CurrentValue.VoiceId = VoiceId.Trim();
                }
                
                // Reconnect the API clients with new credentials
                var realtimeClient = _serviceProvider.GetService<IRealtimeClient>();
                if (realtimeClient != null && realtimeClient.IsConnected)
                {
                    await realtimeClient.DisconnectAsync();
                    await Task.Delay(500); // Brief delay before reconnecting
                    await realtimeClient.ConnectAsync();
                }
                
                StatusMessage = "Settings saved successfully!";
                
                // Clear message after 3 seconds
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    StatusMessage = string.Empty;
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving: {ex.Message}";
            }
        }

        private void ClearSettings()
        {
            OpenAIKey = string.Empty;
            CartesiaKey = string.Empty;
            VoiceId = string.Empty;
            
            Preferences.Remove("openai_api_key");
            Preferences.Remove("cartesia_api_key");
            Preferences.Remove("cartesia_voice_id");
            
            StatusMessage = "Settings cleared";
            
            // Clear message after 2 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}