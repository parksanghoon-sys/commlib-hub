using System.Text.Json;
using CommLib.Application.Bootstrap;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// <see cref="CommLibHostedService"/>와 설정 기반 host registration 경로를 검증합니다.
/// </summary>
public sealed class CommLibHostedServiceTests
{
    /// <summary>
    /// 활성화된 장치만 설정에서 매핑해 호스트 시작 시 연결하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StartAsync_MapsOnlyEnabledProfilesFromConfiguration()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);
        var hostedService = new CommLibHostedService(
            bootstrapper,
            manager,
            new CommLibOptions
            {
                Devices =
                [
                    CreateRawProfile("enabled-1", enabled: true, port: 1000),
                    CreateInvalidRawProfile("disabled-invalid", enabled: false, port: 1001),
                    CreateRawProfile("enabled-2", enabled: true, port: 1002)
                ]
            });

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(new[] { "enabled-1", "enabled-2" }, manager.ConnectedIds);
    }

    /// <summary>
    /// 호스트 종료 시 연결 관리자의 비동기 정리를 호출하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task StopAsync_DisposesConnectionManager()
    {
        var manager = new FakeConnectionManager();
        var bootstrapper = new DeviceBootstrapper(manager);
        var hostedService = new CommLibHostedService(bootstrapper, manager, new CommLibOptions());

        await hostedService.StopAsync(CancellationToken.None);

        Assert.Equal(1, manager.DisposeAsyncCallCount);
    }

    /// <summary>
    /// 설정 기반 등록이 CommLib 섹션 바인딩과 hosted service wiring을 함께 구성하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task AddCommLibCore_WithConfiguration_RegistersBoundOptionsAndHostedService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                KeyValuePair.Create<string, string?>("CommLib:Devices:0:DeviceId", "disabled-1"),
                KeyValuePair.Create<string, string?>("CommLib:Devices:0:DisplayName", "Disabled 1"),
                KeyValuePair.Create<string, string?>("CommLib:Devices:0:Enabled", "false"),
                KeyValuePair.Create<string, string?>("CommLib:Devices:0:Transport:Type", "TcpClient"),
                KeyValuePair.Create<string, string?>("CommLib:Devices:0:Transport:Host", "127.0.0.1"),
                KeyValuePair.Create<string, string?>("CommLib:Devices:0:Transport:Port", "7001")
            ])
            .Build();

        var services = new ServiceCollection();
        services.AddCommLibCore(configuration, options => options.InboundQueueCapacity = 8);

        await using var serviceProvider = services.BuildServiceProvider();
        var runtimeOptions = serviceProvider.GetRequiredService<CommLibRuntimeOptions>();
        var commLibOptions = serviceProvider.GetRequiredService<CommLibOptions>();
        var hostedService = Assert.Single(serviceProvider.GetServices<IHostedService>());

        Assert.Equal(8, runtimeOptions.InboundQueueCapacity);
        Assert.Single(commLibOptions.Devices);
        Assert.IsType<CommLibHostedService>(hostedService);

        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// 유효한 원시 장치 프로필을 생성합니다.
    /// </summary>
    /// <param name="deviceId">장치 식별자입니다.</param>
    /// <param name="enabled">활성 여부입니다.</param>
    /// <param name="port">TCP 포트입니다.</param>
    /// <returns>유효한 원시 장치 프로필입니다.</returns>
    private static DeviceProfileRaw CreateRawProfile(string deviceId, bool enabled, int port)
    {
        return new DeviceProfileRaw
        {
            DeviceId = deviceId,
            DisplayName = deviceId,
            Enabled = enabled,
            Transport = CreateTransportElement("127.0.0.1", port),
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    /// <summary>
    /// 비활성 프로필 무시 동작을 검증하기 위한 잘못된 원시 장치 프로필을 생성합니다.
    /// </summary>
    /// <param name="deviceId">장치 식별자입니다.</param>
    /// <param name="enabled">활성 여부입니다.</param>
    /// <param name="port">TCP 포트입니다.</param>
    /// <returns>비활성화된 잘못된 원시 장치 프로필입니다.</returns>
    private static DeviceProfileRaw CreateInvalidRawProfile(string deviceId, bool enabled, int port)
    {
        return new DeviceProfileRaw
        {
            DeviceId = deviceId,
            DisplayName = deviceId,
            Enabled = enabled,
            Transport = CreateTransportElement(string.Empty, port),
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    /// <summary>
    /// 테스트용 TCP 전송 JSON 요소를 생성합니다.
    /// </summary>
    /// <param name="host">호스트 값입니다.</param>
    /// <param name="port">포트 값입니다.</param>
    /// <returns>TCP 전송 JSON 요소입니다.</returns>
    private static JsonElement CreateTransportElement(string host, int port)
    {
        using var document = JsonDocument.Parse($$"""
        {
          "Type": "TcpClient",
          "Host": "{{host}}",
          "Port": {{port}}
        }
        """);

        return document.RootElement.Clone();
    }

    /// <summary>
    /// 부트스트랩 테스트에 사용하는 최소 연결 관리자 구현입니다.
    /// </summary>
    private sealed class FakeConnectionManager : IConnectionManager, IAsyncDisposable
    {
        /// <summary>
        /// 연결 시도가 들어온 장치 식별자 목록입니다.
        /// </summary>
        public List<string> ConnectedIds { get; } = new();

        /// <summary>
        /// 비동기 정리 호출 횟수입니다.
        /// </summary>
        public int DisposeAsyncCallCount { get; private set; }

        /// <summary>
        /// 연결 요청을 기록합니다.
        /// </summary>
        /// <param name="profile">연결할 장치 프로필입니다.</param>
        /// <param name="cancellationToken">작업 취소 토큰입니다.</param>
        /// <returns>즉시 완료되는 작업입니다.</returns>
        public Task ConnectAsync(DeviceProfile profile, CancellationToken cancellationToken = default)
        {
            ConnectedIds.Add(profile.DeviceId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 테스트에서는 연결 해제를 별도로 사용하지 않습니다.
        /// </summary>
        /// <param name="deviceId">연결 해제할 장치 식별자입니다.</param>
        /// <param name="cancellationToken">작업 취소 토큰입니다.</param>
        /// <returns>즉시 완료되는 작업입니다.</returns>
        public Task DisconnectAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 테스트에서는 송신 기능을 사용하지 않습니다.
        /// </summary>
        /// <param name="deviceId">메시지를 보낼 장치 식별자입니다.</param>
        /// <param name="message">전송할 메시지입니다.</param>
        /// <param name="cancellationToken">작업 취소 토큰입니다.</param>
        /// <returns>즉시 완료되는 작업입니다.</returns>
        public Task SendAsync(string deviceId, IMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 테스트에서는 수신 기능을 지원하지 않습니다.
        /// </summary>
        /// <param name="deviceId">메시지를 받을 장치 식별자입니다.</param>
        /// <param name="cancellationToken">작업 취소 토큰입니다.</param>
        /// <returns>항상 예외를 발생시킵니다.</returns>
        public Task<IMessage> ReceiveAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 테스트용 구현에서는 활성 세션을 보관하지 않습니다.
        /// </summary>
        /// <param name="deviceId">조회할 장치 식별자입니다.</param>
        /// <returns>항상 <see langword="null"/>입니다.</returns>
        public IDeviceSession? GetSession(string deviceId)
        {
            return null;
        }

        /// <summary>
        /// 연결 관리자 정리 호출 횟수를 증가시킵니다.
        /// </summary>
        /// <returns>즉시 완료되는 비동기 정리 작업입니다.</returns>
        public ValueTask DisposeAsync()
        {
            DisposeAsyncCallCount++;
            return ValueTask.CompletedTask;
        }
    }
}
