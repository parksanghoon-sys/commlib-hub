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
/// 통신 라이브러리용 의존성 주입 등록 도우미를 제공합니다.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 전송 생성과 부트스트랩 실행에 필요한 CommLib 핵심 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">구성할 서비스 컬렉션입니다.</param>
    /// <returns>연쇄 호출을 위해 동일한 서비스 컬렉션을 반환합니다.</returns>
    public static IServiceCollection AddCommLibCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IProtocolFactory, ProtocolFactory>();
        services.AddSingleton<ISerializerFactory, SerializerFactory>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddTransient<DeviceBootstrapper>();
        return services;
    }

    /// <summary>
    /// 설정 기반 장치 프로필 바인딩과 함께 CommLib 핵심 서비스를 등록하고 Generic Host 수명주기에 연결합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <param name="configuration">루트 설정 또는 <c>CommLib</c> 섹션입니다.</param>
    /// <returns>연쇄 호출을 위한 동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddCommLibCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddCommLibCore(services);
        services.AddSingleton(_ => BindCommLibOptions(configuration));
        services.AddSingleton<IHostedService, CommLibHostedService>();
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
