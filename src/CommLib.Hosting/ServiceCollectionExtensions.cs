using CommLib.Application.Bootstrap;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace CommLib.Hosting;

/// <summary>
/// CommLib 핵심 서비스를 DI 컨테이너에 등록하는 확장 메서드를 제공합니다.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 기본 런타임 옵션으로 CommLib 핵심 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <returns>연쇄 호출을 위한 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddCommLibCore(this IServiceCollection services)
    {
        return services.AddCommLibCore(static _ => { });
    }

    /// <summary>
    /// 지정한 런타임 옵션으로 CommLib 핵심 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <param name="configure">런타임 옵션을 조정하는 콜백입니다.</param>
    /// <returns>연쇄 호출을 위한 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddCommLibCore(
        this IServiceCollection services,
        Action<CommLibRuntimeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var runtimeOptions = new CommLibRuntimeOptions();
        configure(runtimeOptions);

        if (runtimeOptions.InboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runtimeOptions),
                "Inbound queue capacity must be greater than 0.");
        }

        var runtimeOptionsSnapshot = new CommLibRuntimeOptions
        {
            InboundQueueCapacity = runtimeOptions.InboundQueueCapacity
        };

        services.AddSingleton(runtimeOptionsSnapshot);
        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IProtocolFactory, ProtocolFactory>();
        services.AddSingleton<ISerializerFactory, SerializerFactory>();
        services.AddSingleton<IConnectionManager>(serviceProvider => new ConnectionManager(
            serviceProvider.GetRequiredService<ITransportFactory>(),
            serviceProvider.GetRequiredService<IProtocolFactory>(),
            serviceProvider.GetRequiredService<ISerializerFactory>(),
            serviceProvider.GetService<IConnectionEventSink>(),
            inboundQueueCapacity: runtimeOptionsSnapshot.InboundQueueCapacity));
        services.AddTransient<DeviceBootstrapper>();
        return services;
    }
}
