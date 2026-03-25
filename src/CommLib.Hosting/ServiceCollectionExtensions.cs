using CommLib.Application.Bootstrap;
using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace CommLib.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommLibCore(this IServiceCollection services)
    {
        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddTransient<DeviceBootstrapper>();
        return services;
    }
}
