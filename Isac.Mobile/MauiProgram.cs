using Microsoft.Extensions.Logging;
using Isac.Mobile.Pages;
using Microsoft.Extensions.DependencyInjection;
using Isac.Core.Shared.Client; // Add this if IRealtimeClient exists

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

            // Register pages
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<VoiceSettingsPage>();
            builder.Services.AddTransient<AISettingsPage>();

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
