using Microsoft.Extensions.Logging;
using Isac.Core.Shared.Client;
using Isac.Mobile.Services;
using Isac.Core.Shared.Configuration;
using Isac.Mobile.Pages;
using Isac.Mobile.Configuration; // NEW: Add configuration helper

namespace Isac.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // NEW: Load .env configuration at startup (must be synchronous in CreateMauiApp)
            // Note: This will block, but only happens once at startup
            EmbeddedConfiguration.LoadAsync().GetAwaiter().GetResult();
            
            // NEW: Pre-populate Preferences with .env values if not already set
            if (string.IsNullOrEmpty(Preferences.Get("OpenAIApiKey", string.Empty)))
            {
                var openAIKey = EmbeddedConfiguration.OpenAIApiKey;
                if (!string.IsNullOrEmpty(openAIKey))
                {
                    Preferences.Set("OpenAIApiKey", openAIKey);
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Injected OpenAI API key from .env");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] OpenAI API key not found in .env");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MauiProgram] OpenAI API key already set in Preferences");
            }

            if (string.IsNullOrEmpty(Preferences.Get("CartesiaApiKey", string.Empty)))
            {
                var cartesiaKey = EmbeddedConfiguration.CartesiaApiKey;
                if (!string.IsNullOrEmpty(cartesiaKey))
                {
                    Preferences.Set("CartesiaApiKey", cartesiaKey);
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Injected Cartesia API key from .env");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Cartesia API key not found in .env");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MauiProgram] Cartesia API key already set in Preferences");
            }

            if (string.IsNullOrEmpty(Preferences.Get("VoiceId", string.Empty)))
            {
                var voiceId = EmbeddedConfiguration.CartesiaVoiceId;
                if (!string.IsNullOrEmpty(voiceId))
                {
                    Preferences.Set("VoiceId", voiceId);
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Injected Cartesia voice ID from .env");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MauiProgram] Cartesia voice ID not found in .env");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MauiProgram] Cartesia voice ID already set in Preferences");
            }

            // Configure services with actual API keys from environment or secure storage
            builder.Services.Configure<OpenAIRealtimeOptions>(options =>
            {
                // Get API key from secure storage or environment variable
                var apiKey = Preferences.Get("OpenAIApiKey", string.Empty);
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Fallback to environment variable for development
                    apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey") ?? string.Empty;
                }

                options.ApiKey = apiKey;
                options.Model = "gpt-realtime";
                options.Voice = "alloy";
            });

            builder.Services.Configure<CartesiaOptions>(options =>
            {
                // Get API key from secure storage or environment variable
                var apiKey = Preferences.Get("CartesiaApiKey", string.Empty);
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Fallback to environment variable for development
                    apiKey = Environment.GetEnvironmentVariable("CartesiaApiKey") ?? string.Empty;
                }

                var voiceId = Preferences.Get("VoiceId", string.Empty);
                if (string.IsNullOrEmpty(voiceId))
                {
                    // Fallback to environment variable for development
                    voiceId = Environment.GetEnvironmentVariable("VoiceId") ?? string.Empty;
                }

                options.ApiKey = apiKey;
                options.VoiceId = voiceId;
                options.Model = "sonic-english";
            });

            // Register API clients
            builder.Services.AddOpenAIRealtimeClient();
            builder.Services.AddCartesiaTTSClient();

            // Register platform services
            builder.Services.AddSingleton<IBluetoothService, BluetoothService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();

            // Register ViewModels
            builder.Services.AddTransient<ViewModels.HomeViewModel>();
            builder.Services.AddTransient<ViewModels.SettingsViewModel>();

            // Register Pages
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.VoiceSettingsPage>();
            builder.Services.AddTransient<Pages.AISettingsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Register routes (add this after the app is built, before return)
            var app = builder.Build();

            // Register Shell routes
            Routing.RegisterRoute("HomePage",          typeof(HomePage));
            Routing.RegisterRoute("VoiceSettingsPage", typeof(VoiceSettingsPage));
            Routing.RegisterRoute("AISettingsPage",    typeof(AISettingsPage));

            return app;
        }
    }
}
