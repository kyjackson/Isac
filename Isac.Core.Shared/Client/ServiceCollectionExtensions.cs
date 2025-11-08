using Isac.Core.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Isac.Core.Shared.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIsacClient(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection("Isac:Api");
        var opts = section.Get<IsacApiOptions>() ?? new IsacApiOptions();
        services.AddSingleton(opts);
        services.AddHttpClient<IIsacClient, IsacHttpClient>();
        return services;
    }

    public static IServiceCollection AddIsacClient(this IServiceCollection services, Action<IsacApiOptions> configure)
    {
        var opts = new IsacApiOptions();
        configure(opts);
        services.AddSingleton(opts);
        services.AddHttpClient<IIsacClient, IsacHttpClient>();
        return services;
    }
}
