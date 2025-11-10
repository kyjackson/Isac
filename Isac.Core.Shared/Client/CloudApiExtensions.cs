using Isac.Core.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Isac.Core.Shared.Client;

public static class CloudApiExtensions
{
    /// <summary>
    /// Registers OpenAI Realtime API client with configuration from IOptions.
    /// The concrete implementation (OpenAIRealtimeClient) should be registered in the Mobile project.
    /// </summary>
    public static IServiceCollection AddOpenAIRealtimeClient(this IServiceCollection services)
    {
        // Register the client using factory pattern that resolves options
        services.AddSingleton<IRealtimeClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAIRealtimeOptions>>().Value;
            // This assumes the concrete type is registered in Isac.Mobile
            var clientType = Type.GetType("Isac.Mobile.Services.OpenAIRealtimeClient, Isac.Mobile");
            if (clientType == null)
            {
                throw new InvalidOperationException("OpenAIRealtimeClient implementation not found. Ensure Isac.Mobile is loaded.");
            }
            return (IRealtimeClient)Activator.CreateInstance(clientType, options)!;
        });
        return services;
    }

    /// <summary>
    /// Registers Cartesia TTS API client with configuration from IOptions.
    /// The concrete implementation (CartesiaTTSClient) should be registered in the Mobile project.
    /// </summary>
    public static IServiceCollection AddCartesiaTTSClient(this IServiceCollection services)
    {
        // Register the client using factory pattern with HttpClient and options
        services.AddHttpClient<ICartesiaTTSClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.cartesia.ai");
            client.DefaultRequestHeaders.Add("Cartesia-Version", "2025-04-16"); // Updated to latest API version
        })
        .AddTypedClient<ICartesiaTTSClient>((httpClient, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<CartesiaOptions>>().Value;
            httpClient.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            
            var clientType = Type.GetType("Isac.Mobile.Services.CartesiaTTSClient, Isac.Mobile");
            if (clientType == null)
            {
                throw new InvalidOperationException("CartesiaTTSClient implementation not found. Ensure Isac.Mobile is loaded.");
            }
            return (ICartesiaTTSClient)Activator.CreateInstance(clientType, httpClient, options)!;
        });
        return services;
    }
}
