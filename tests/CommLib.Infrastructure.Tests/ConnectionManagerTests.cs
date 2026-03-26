using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Sessions;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 연결 관리자의 세션 등록과 송신기 조립 동작을 검증합니다.
/// </summary>
public sealed class ConnectionManagerTests
{
    /// <summary>
    /// 연결 시 장치 프로필의 transport 설정으로 transport factory를 호출하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_TransportFactoryIsCalledWithProfileTransport()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        Assert.Same(profile.Transport, transportFactory.LastOptions);
    }

    /// <summary>
    /// 연결 시 프로필의 protocol 설정으로 protocol factory를 호출하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ProtocolFactoryIsCalledWithProfileProtocol()
    {
        var protocolFactory = new FakeProtocolFactory();
        var manager = CreateManager(protocolFactory: protocolFactory);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        Assert.Same(profile.Protocol, protocolFactory.LastOptions);
    }

    /// <summary>
    /// 연결 시 프로필의 serializer 설정으로 serializer factory를 호출하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SerializerFactoryIsCalledWithProfileSerializer()
    {
        var serializerFactory = new FakeSerializerFactory();
        var manager = CreateManager(serializerFactory: serializerFactory);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        Assert.Same(profile.Serializer, serializerFactory.LastOptions);
    }

    /// <summary>
    /// 장치 프로필을 연결하면 장치 식별자로 조회 가능한 세션을 등록하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_RegistersSessionAccessibleByDeviceId()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        var session = manager.GetSession(profile.DeviceId);

        Assert.NotNull(session);
        Assert.Equal(profile.DeviceId, session.DeviceId);
    }

    /// <summary>
    /// 연결 후 같은 장치 식별자로 메시지를 보내면 조립된 sender를 통해 transport까지 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_ConnectedDevice_SendsEncodedFrameThroughTransport()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        await manager.SendAsync(profile.DeviceId, new FakeMessage(42));

        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'4', (byte)'2' }, transportFactory.Transport.LastFrame);
        Assert.Equal(1, transportFactory.Transport.SendCount);
    }

    /// <summary>
    /// 연결 후 송신하면 세션 outbound 큐가 비워진 상태로 유지되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_ConnectedDevice_DrainsSessionOutboundQueue()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        await manager.SendAsync(profile.DeviceId, new FakeMessage(42));

        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        Assert.False(session.TryDequeueOutbound(out _));
    }

    /// <summary>
    /// 연결되지 않은 장치 식별자로 송신하면 예외를 발생시키는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_UnknownDevice_Throws()
    {
        var manager = CreateManager();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SendAsync("missing-device", new FakeMessage(1)));

        Assert.Contains("missing-device", exception.Message);
    }

    /// <summary>
    /// 서로 다른 장치 프로필을 연결하면 각 장치별 세션이 독립적으로 유지되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_MultipleProfiles_RegistersEachSessionIndependently()
    {
        var manager = CreateManager();
        var firstProfile = CreateTcpProfile("device-1", 502);
        var secondProfile = CreateTcpProfile("device-2", 503);

        await manager.ConnectAsync(firstProfile);
        await manager.ConnectAsync(secondProfile);

        Assert.Equal("device-1", manager.GetSession("device-1")?.DeviceId);
        Assert.Equal("device-2", manager.GetSession("device-2")?.DeviceId);
    }

    /// <summary>
    /// 같은 장치를 다시 연결하면 새 세션으로 교체하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SameDeviceConnectedTwice_ReplacesSession()
    {
        var manager = CreateManager();
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
    /// 한 장치를 재연결해도 다른 장치 세션은 그대로 유지되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ReconnectingOneDevice_DoesNotReplaceOtherDeviceSession()
    {
        var manager = CreateManager();
        var firstProfile = CreateTcpProfile("device-1", 502);
        var secondProfile = CreateTcpProfile("device-2", 503);

        await manager.ConnectAsync(firstProfile);
        await manager.ConnectAsync(secondProfile);
        var secondSessionBeforeReconnect = manager.GetSession("device-2");

        await manager.ConnectAsync(firstProfile);
        var secondSessionAfterReconnect = manager.GetSession("device-2");

        Assert.NotNull(secondSessionBeforeReconnect);
        Assert.Same(secondSessionBeforeReconnect, secondSessionAfterReconnect);
    }

    /// <summary>
    /// transport 생성이 실패하면 예외를 그대로 전달하고 세션을 남기지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_WhenTransportFactoryThrows_DoesNotRegisterSession()
    {
        var manager = CreateManager(transportFactory: new ThrowingTransportFactory());
        var profile = CreateTcpProfile("device-1", 502);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ConnectAsync(profile));

        Assert.Null(manager.GetSession(profile.DeviceId));
    }

    /// <summary>
    /// 존재하지 않는 장치 식별자를 조회하면 세션이 없음을 확인합니다.
    /// </summary>
    [Fact]
    public void GetSession_UnknownDevice_ReturnsNull()
    {
        var manager = CreateManager();

        var session = manager.GetSession("missing-device");

        Assert.Null(session);
    }

    private static ConnectionManager CreateManager(
        ITransportFactory? transportFactory = null,
        IProtocolFactory? protocolFactory = null,
        ISerializerFactory? serializerFactory = null)
    {
        return new ConnectionManager(
            transportFactory ?? new FakeTransportFactory(),
            protocolFactory ?? new FakeProtocolFactory(),
            serializerFactory ?? new FakeSerializerFactory());
    }

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
            Protocol = new ProtocolOptions
            {
                Type = "LengthPrefixed"
            },
            Serializer = new SerializerOptions
            {
                Type = "AutoBinary"
            }
        };
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;

    private sealed class FakeTransportFactory : ITransportFactory
    {
        public TransportOptions? LastOptions { get; private set; }

        public FakeTransport Transport { get; } = new();

        public ITransport Create(TransportOptions options)
        {
            LastOptions = options;
            return Transport;
        }
    }

    private sealed class FakeTransport : ITransport
    {
        public string Name => "FakeTransport";

        public byte[]? LastFrame { get; private set; }

        public int SendCount { get; private set; }

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastFrame = frame.ToArray();
            SendCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProtocolFactory : IProtocolFactory
    {
        public ProtocolOptions? LastOptions { get; private set; }

        public IProtocol Create(ProtocolOptions options)
        {
            LastOptions = options;
            return new FakeProtocol();
        }
    }

    private sealed class FakeProtocol : IProtocol
    {
        public string Name => "FakeProtocol";

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            var frame = new byte[payload.Length + 4];
            frame[3] = (byte)payload.Length;
            payload.CopyTo(frame.AsSpan(4));
            return frame;
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }
    }

    private sealed class FakeSerializerFactory : ISerializerFactory
    {
        public SerializerOptions? LastOptions { get; private set; }

        public ISerializer Create(SerializerOptions options)
        {
            LastOptions = options;
            return new FakeSerializer();
        }
    }

    private sealed class FakeSerializer : ISerializer
    {
        public byte[] Serialize(IMessage message)
        {
            return System.Text.Encoding.UTF8.GetBytes(message.MessageId.ToString());
        }

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            var text = System.Text.Encoding.UTF8.GetString(payload);
            return new FakeMessage(ushort.Parse(text));
        }
    }

    private sealed class ThrowingTransportFactory : ITransportFactory
    {
        public ITransport Create(TransportOptions options)
        {
            throw new InvalidOperationException("Transport creation failed.");
        }
    }
}
