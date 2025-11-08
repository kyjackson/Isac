using Microsoft.Extensions.Logging;
using Isac.Mobile.Pages;
using Isac.Mobile.Services;
using Isac.Core.Shared.Client;

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

            // Register pages (DI)
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<EnrollmentPage>();
            builder.Services.AddTransient<SettingsPage>();

            // Register OpenAI Realtime client
            builder.Services.AddOpenAIRealtimeClient(options =>
            {
                options.ApiKey = Preferences.Get("OpenAI:ApiKey", string.Empty);
                options.Model = "gpt-4o-mini-realtime-preview";
                options.Voice = "alloy";
            });
            builder.Services.AddSingleton<IRealtimeClient, OpenAIRealtimeClient>();

            // Register Cartesia TTS client
            builder.Services.AddCartesiaTTSClient(options =>
            {
                options.ApiKey = Preferences.Get("Cartesia:ApiKey", string.Empty);
                options.VoiceId = Preferences.Get("Cartesia:VoiceId", string.Empty);
                options.Model = "sonic-english";
            });
            builder.Services.AddHttpClient<ICartesiaTTSClient, CartesiaTTSClient>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
