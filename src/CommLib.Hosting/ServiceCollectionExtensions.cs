using CommLib.Application.Bootstrap;
using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace CommLib.Hosting;

/// <summary>
/// Provides dependency injection registration helpers for the communication library.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core CommLib services required for transport creation and bootstrap execution.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCommLibCore(this IServiceCollection services)
    {
        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddTransient<DeviceBootstrapper>();
        return services;
    }
}
