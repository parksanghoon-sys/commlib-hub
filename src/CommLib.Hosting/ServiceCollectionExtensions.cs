using CommLib.Application.Bootstrap;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<ITransportFactory, TransportFactory>();
        services.AddSingleton<IProtocolFactory, ProtocolFactory>();
        services.AddSingleton<ISerializerFactory, SerializerFactory>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddTransient<DeviceBootstrapper>();
        return services;
    }
}
