using Isac.Core.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Isac.Core.Shared.Client;

public static class CloudApiExtensions
{
    /// <summary>
    /// Registers OpenAI Realtime API client with configuration.
    /// </summary>
    public static IServiceCollection AddOpenAIRealtimeClient(
        this IServiceCollection services,
        Action<OpenAIRealtimeOptions> configure)
    {
        var options = new OpenAIRealtimeOptions();
        configure(options);
        services.AddSingleton(options);
        // Client implementation will be added in Mobile project
        // services.AddSingleton<IRealtimeClient, OpenAIRealtimeClient>();
        return services;
    }

    /// <summary>
    /// Registers Cartesia TTS API client with configuration.
    /// </summary>
    public static IServiceCollection AddCartesiaTTSClient(
        this IServiceCollection services,
        Action<CartesiaOptions> configure)
    {
        var options = new CartesiaOptions();
        configure(options);
        services.AddSingleton(options);
        // Client implementation will be added in Mobile project
        // services.AddHttpClient<ICartesiaTTSClient, CartesiaTTSClient>();
        return services;
    }
}
