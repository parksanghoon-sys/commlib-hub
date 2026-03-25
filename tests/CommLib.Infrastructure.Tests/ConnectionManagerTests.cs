using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Sessions;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 연결 관리자의 세션 등록 동작을 검증합니다.
/// </summary>
public sealed class ConnectionManagerTests
{
    /// <summary>
    /// 연결 시 장치 프로필의 전송 옵션으로 전송 팩토리를 호출하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_TransportFactoryIsCalledWithProfileTransport()
    {
        var factory = new FakeTransportFactory();
        var manager = new ConnectionManager(factory);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        Assert.Same(profile.Transport, factory.LastOptions);
    }

    /// <summary>
    /// 장치 프로필을 연결하면 장치 식별자로 조회 가능한 세션이 등록되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_RegistersSessionAccessibleByDeviceId()
    {
        var manager = new ConnectionManager(new FakeTransportFactory());
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        var session = manager.GetSession(profile.DeviceId);

        Assert.NotNull(session);
        Assert.Equal(profile.DeviceId, session.DeviceId);
    }

    /// <summary>
    /// 서로 다른 장치 프로필을 순서대로 연결하면 각 장치별 세션이 각각 유지되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_MultipleProfiles_RegistersEachSessionIndependently()
    {
        var manager = new ConnectionManager(new FakeTransportFactory());
        var firstProfile = CreateTcpProfile("device-1", 502);
        var secondProfile = CreateTcpProfile("device-2", 503);

        await manager.ConnectAsync(firstProfile);
        await manager.ConnectAsync(secondProfile);

        Assert.Equal("device-1", manager.GetSession("device-1")?.DeviceId);
        Assert.Equal("device-2", manager.GetSession("device-2")?.DeviceId);
    }

    /// <summary>
    /// 같은 장치를 다시 연결하면 새 세션으로 교체되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SameDeviceConnectedTwice_ReplacesSession()
    {
        var manager = new ConnectionManager(new FakeTransportFactory());
        var profile = CreateTcpProfile("device-1", 502);

        await manager.ConnectAsync(profile);
        var firstSession = manager.GetSession("device-1");

        await manager.ConnectAsync(profile);
        var secondSession = manager.GetSession("device-1");

        Assert.NotNull(firstSession);
        Assert.NotNull(secondSession);
        Assert.NotSame(firstSession, secondSession);
    }

    /// <summary>
    /// 알 수 없는 장치 식별자를 조회하면 세션이 없음을 확인합니다.
    /// </summary>
    [Fact]
    public void GetSession_UnknownDevice_ReturnsNull()
    {
        var manager = new ConnectionManager(new FakeTransportFactory());

        var session = manager.GetSession("missing-device");

        Assert.Null(session);
    }

    /// <summary>
    /// 테스트에 사용할 TCP 장치 프로필을 생성합니다.
    /// </summary>
    /// <param name="deviceId">생성할 장치 식별자입니다.</param>
    /// <param name="port">생성할 장치 포트입니다.</param>
    /// <returns>테스트용 TCP 장치 프로필입니다.</returns>
    private static DeviceProfile CreateTcpProfile(string deviceId = "device-1", int port = 502)
    {
        return new DeviceProfile
        {
            DeviceId = deviceId,
            DisplayName = deviceId,
            Enabled = true,
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = "127.0.0.1",
                Port = port
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    /// <summary>
    /// 테스트용 전송 팩토리 호출 정보를 기록하는 가짜 구현입니다.
    /// </summary>
    private sealed class FakeTransportFactory : ITransportFactory
    {
        /// <summary>
        /// 마지막으로 전달된 전송 옵션을 가져옵니다.
        /// </summary>
        public TransportOptions? LastOptions { get; private set; }

        /// <summary>
        /// 전달된 전송 옵션을 기록하고 가짜 전송 객체를 반환합니다.
        /// </summary>
        /// <param name="options">생성 요청에 사용된 전송 옵션입니다.</param>
        /// <returns>테스트용 가짜 전송 객체입니다.</returns>
        public ITransport Create(TransportOptions options)
        {
            LastOptions = options;
            return new FakeTransport();
        }
    }

    /// <summary>
    /// 테스트에서만 사용하는 가짜 전송 구현입니다.
    /// </summary>
    private sealed class FakeTransport : ITransport
    {
        /// <summary>
        /// 가짜 전송 이름을 가져옵니다.
        /// </summary>
        public string Name => "FakeTransport";
    }
}
