using CommLib.Application.Bootstrap;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        return AddCommLibCoreInternal(services, configuration: null, static _ => { });
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
        return AddCommLibCoreInternal(services, configuration: null, configure);
    }

    /// <summary>
    /// 지정한 설정을 바인딩하고 Generic Host 수명주기와 함께 CommLib 핵심 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <param name="configuration">루트 설정 또는 <c>CommLib</c> 섹션입니다.</param>
    /// <returns>연쇄 호출을 위한 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddCommLibCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return AddCommLibCoreInternal(services, configuration, static _ => { });
    }

    /// <summary>
    /// 지정한 설정과 런타임 옵션으로 Generic Host 수명주기까지 포함한 CommLib 핵심 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <param name="configuration">루트 설정 또는 <c>CommLib</c> 섹션입니다.</param>
    /// <param name="configure">런타임 옵션을 조정하는 콜백입니다.</param>
    /// <returns>연쇄 호출을 위한 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddCommLibCore(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CommLibRuntimeOptions> configure)
    {
        return AddCommLibCoreInternal(services, configuration, configure);
    }

    /// <summary>
    /// CommLib 핵심 서비스 등록의 공통 구현을 수행합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <param name="configuration">선택적 CommLib 설정입니다.</param>
    /// <param name="configure">런타임 옵션을 조정하는 콜백입니다.</param>
    /// <returns>연쇄 호출을 위한 동일한 서비스 컬렉션입니다.</returns>
    private static IServiceCollection AddCommLibCoreInternal(
        IServiceCollection services,
        IConfiguration? configuration,
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

        if (configuration is not null)
        {
            services.AddSingleton(_ => BindCommLibOptions(configuration));
            services.AddSingleton<IHostedService, CommLibHostedService>();
        }

        return services;
    }

    /// <summary>
    /// 입력 설정에서 CommLib 루트 옵션을 바인딩합니다.
    /// </summary>
    /// <param name="configuration">루트 설정 또는 <c>CommLib</c> 섹션입니다.</param>
    /// <returns>바인딩된 CommLib 옵션입니다.</returns>
    private static CommLibOptions BindCommLibOptions(IConfiguration configuration)
    {
        var source = GetCommLibConfiguration(configuration);
        var options = new CommLibOptions();
        source.Bind(options);
        return options;
    }

    /// <summary>
    /// 루트 설정이 들어온 경우 <c>CommLib</c> 섹션을 우선 사용하고, 아니면 현재 섹션 자체를 사용합니다.
    /// </summary>
    /// <param name="configuration">검사할 설정 루트 또는 섹션입니다.</param>
    /// <returns>CommLib 옵션을 바인딩할 실제 설정 소스입니다.</returns>
    private static IConfiguration GetCommLibConfiguration(IConfiguration configuration)
    {
        var commLibSection = configuration.GetSection("CommLib");
        return commLibSection.Exists()
            ? commLibSection
            : configuration;
    }
}
