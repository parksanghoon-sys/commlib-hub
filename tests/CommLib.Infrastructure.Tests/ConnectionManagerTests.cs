using System.Threading.Channels;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Protocol;
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
    /// 연결 시 생성된 transport를 실제 open 단계까지 진행하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_OpensCreatedTransport()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        Assert.True(transportFactory.Transport.IsOpen);
        Assert.Equal(1, transportFactory.Transport.OpenCount);
    }

    [Fact]
    public async Task ConnectAsync_ConcurrentSameDeviceCalls_SerializesTransportOpen()
    {
        var firstTransport = new BlockingOpenTransport();
        var secondTransport = new FakeTransport();
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(firstTransport, secondTransport));
        var profile = CreateTcpProfile();

        var firstConnect = manager.ConnectAsync(profile);
        await firstTransport.OpenStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var secondConnect = manager.ConnectAsync(profile);
        await Task.Delay(100);
        Assert.Equal(0, secondTransport.OpenCount);

        firstTransport.ReleaseOpen();
        await firstConnect;
        await secondConnect;

        Assert.Equal(1, firstTransport.OpenCount);
        Assert.Equal(1, secondTransport.OpenCount);
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
    /// inbound frame을 처리하면 메시지를 복원하고 소비 바이트 수를 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TryHandleInboundFrame_CompleteFrame_ReturnsDecodedMessage()
    {
        var manager = CreateManager(protocolFactory: new ProtocolFactory());
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        var handled = manager.TryHandleInboundFrame(
            profile.DeviceId,
            new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'4', (byte)'2' },
            out var message,
            out var bytesConsumed);

        Assert.True(handled);
        Assert.NotNull(message);
        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal(6, bytesConsumed);
    }

    /// <summary>
    /// inbound frame이 미완성이면 메시지를 복원하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TryHandleInboundFrame_IncompleteFrame_ReturnsFalse()
    {
        var manager = CreateManager(protocolFactory: new ProtocolFactory());
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        var handled = manager.TryHandleInboundFrame(
            profile.DeviceId,
            new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'4' },
            out var message,
            out var bytesConsumed);

        Assert.False(handled);
        Assert.Null(message);
        Assert.Equal(0, bytesConsumed);
    }

    /// <summary>
    /// 응답 frame을 처리하면 세션의 pending 요청이 완료되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TryHandleInboundFrame_ResponseFrame_CompletesPendingRequest()
    {
        var manager = CreateManager(
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new ResponseSerializerFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var request = new FakeRequestMessage(7) { CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111") };
        var sendResult = session.Send<FakeRequestMessage, FakeResponseMessage>(request);
        session.TryDequeueOutbound(out _);

        var handled = manager.TryHandleInboundFrame(
            profile.DeviceId,
            new byte[] { 0x00, 0x00, 0x00, 0x01, (byte)'7' },
            out var message,
            out var bytesConsumed);

        var response = await sendResult.ResponseTask;

        Assert.True(handled);
        Assert.NotNull(message);
        Assert.Equal((ushort)7, response.MessageId);
        Assert.Equal(request.CorrelationId, response.CorrelationId);
        Assert.Equal(5, bytesConsumed);
    }

    /// <summary>
    /// transport 수신 경로를 타면 inbound 메시지를 복원해 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_QueuedInboundFrame_ReturnsDecodedMessage()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        transportFactory.Transport.EnqueueInboundFrame(new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'4', (byte)'2' });

        var message = await manager.ReceiveAsync(profile.DeviceId);

        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal(1, transportFactory.Transport.ReceiveCount);
    }

    /// <summary>
    /// transport 수신 응답 메시지는 pending 요청 완료까지 연결되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceivePump_ResponseMessage_CompletesPendingRequest()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new ResponseSerializerFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var request = new FakeRequestMessage(7) { CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111") };
        var sendResult = session.Send<FakeRequestMessage, FakeResponseMessage>(request);
        session.TryDequeueOutbound(out _);
        transportFactory.Transport.EnqueueInboundFrame(new byte[] { 0x00, 0x00, 0x00, 0x01, (byte)'7' });

        var completed = await Task.WhenAny(sendResult.ResponseTask, Task.Delay(TimeSpan.FromSeconds(1)));
        var response = await sendResult.ResponseTask;

        Assert.Same(sendResult.ResponseTask, completed);
        Assert.Equal(request.CorrelationId, response.CorrelationId);
    }

    /// <summary>
    /// 연결되지 않은 장치 식별자로 송신하면 예외를 발생시키는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithDefaultFactories_RequestMessageIncludesCorrelationPayload()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new SerializerFactory());
        var profile = CreateTcpProfile();
        var request = new FakeRequestMessage(7)
        {
            CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        await manager.ConnectAsync(profile);
        await manager.SendAsync(profile.DeviceId, request);

        Assert.NotNull(transportFactory.Transport.LastFrame);
        Assert.Equal(47, transportFactory.Transport.LastFrame![3]);
        Assert.Equal(
            "request|7|11111111-1111-1111-1111-111111111111|",
            System.Text.Encoding.UTF8.GetString(transportFactory.Transport.LastFrame, 4, transportFactory.Transport.LastFrame.Length - 4));
    }

    /// <summary>
    /// bounded inbound queue가 가득 차면 transport 수신도 추가로 진행되지 않고 대기하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceivePump_WithBoundedInboundQueue_BackpressuresTransportUntilConsumerDrains()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            inboundQueueCapacity: 1);
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        transportFactory.Transport.EnqueueInboundFrame(CreateInboundFrame(41));
        transportFactory.Transport.EnqueueInboundFrame(CreateInboundFrame(42));
        transportFactory.Transport.EnqueueInboundFrame(CreateInboundFrame(43));

        await WaitForAsync(() => transportFactory.Transport.ReceiveCount >= 2);
        await Task.Delay(150);

        Assert.Equal(2, transportFactory.Transport.ReceiveCount);

        var first = await manager.ReceiveAsync(profile.DeviceId);
        Assert.Equal((ushort)41, first.MessageId);

        await WaitForAsync(() => transportFactory.Transport.ReceiveCount == 3);

        var second = await manager.ReceiveAsync(profile.DeviceId);
        var third = await manager.ReceiveAsync(profile.DeviceId);

        Assert.Equal((ushort)42, second.MessageId);
        Assert.Equal((ushort)43, third.MessageId);
    }

    /// <summary>
    /// body가 있는 메시지는 기본 serializer 조합에서 frame payload에 본문까지 포함하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithDefaultFactories_MessageBodyIncludesEncodedPayload()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new SerializerFactory());
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        await manager.SendAsync(profile.DeviceId, new FakeBodyMessage(12, "hello|world"));

        Assert.NotNull(transportFactory.Transport.LastFrame);
        Assert.Equal(
            "message|12|aGVsbG98d29ybGQ=",
            System.Text.Encoding.UTF8.GetString(transportFactory.Transport.LastFrame!, 4, transportFactory.Transport.LastFrame!.Length - 4));
    }

    /// <summary>
    /// 기본 protocol/serializer 조합으로 수신한 응답 frame이 pending 요청 완료까지 이어지는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceivePump_WithDefaultFactories_ResponseMessageCompletesPendingRequest()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new SerializerFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var request = new FakeRequestMessage(7)
        {
            CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };
        var sendResult = session.Send<FakeRequestMessage, IResponseMessage>(request);
        session.TryDequeueOutbound(out _);

        var frame = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol()).Encode(
            new FakeResponseMessage(7)
            {
                CorrelationId = request.CorrelationId,
                IsSuccess = true
            });
        transportFactory.Transport.EnqueueInboundFrame(frame);

        var completed = await Task.WhenAny(sendResult.ResponseTask, Task.Delay(TimeSpan.FromSeconds(1)));
        var response = await sendResult.ResponseTask;

        Assert.Same(sendResult.ResponseTask, completed);
        Assert.Equal(request.CorrelationId, response.CorrelationId);
        Assert.True(response.IsSuccess);
    }

    /// <summary>
    /// background receive pump가 응답을 자동 처리해 명시적 수신 호출 없이도 pending 요청을 완료하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_BackgroundReceivePump_CompletesPendingRequestWithoutExplicitReceive()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new SerializerFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var request = new FakeRequestMessage(7)
        {
            CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };
        var sendResult = session.Send<FakeRequestMessage, IResponseMessage>(request);
        session.TryDequeueOutbound(out _);

        var frame = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol()).Encode(
            new FakeResponseMessage(7)
            {
                CorrelationId = request.CorrelationId,
                IsSuccess = true
            });
        transportFactory.Transport.EnqueueInboundFrame(frame);

        var completed = await Task.WhenAny(sendResult.ResponseTask, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.Same(sendResult.ResponseTask, completed);
        var response = await sendResult.ResponseTask;
        Assert.Equal(request.CorrelationId, response.CorrelationId);
        Assert.True(response.IsSuccess);
    }

    /// <summary>
    /// pending 요청과 매칭되지 않은 응답은 일반 inbound 메시지로 수신할 수 있는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_UnmatchedResponseMessage_ReturnsInboundResponse()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new SerializerFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        var frame = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol()).Encode(
            new FakeResponseMessage(9)
            {
                CorrelationId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                IsSuccess = false
            });
        transportFactory.Transport.EnqueueInboundFrame(frame);

        var message = Assert.IsAssignableFrom<IResponseMessage>(await manager.ReceiveAsync(profile.DeviceId));

        Assert.Equal((ushort)9, message.MessageId);
        Assert.Equal(Guid.Parse("99999999-9999-9999-9999-999999999999"), message.CorrelationId);
        Assert.False(message.IsSuccess);
    }

    [Fact]
    public async Task ReceiveAsync_WhenBackgroundReceiveFails_ThrowsDeviceConnectionExceptionAndEmitsReceiveFailure()
    {
        var failingTransport = new FakeTransport
        {
            ReceiveException = new InvalidOperationException("FakeTransport failed to receive.")
        };
        var eventSink = new RecordingConnectionEventSink();
        var manager = CreateManager(
            transportFactory: new SequencedTransportFactory(failingTransport),
            eventSink: eventSink);
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);

        var first = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.ReceiveAsync(profile.DeviceId));
        var second = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.ReceiveAsync(profile.DeviceId));

        Assert.Equal(profile.DeviceId, first.DeviceId);
        Assert.Equal("receive", first.Operation);
        Assert.Equal("FakeTransport failed to receive.", first.InnerException?.Message);
        Assert.Equal(profile.DeviceId, second.DeviceId);
        Assert.Equal("receive", second.Operation);
        Assert.Contains(
            "failure:device-1:receive:Device 'device-1' failed during receive. See inner exception for details.",
            eventSink.Events);
    }

    [Fact]
    public async Task SendAsync_WhenBackgroundReceiveFails_ThrowsStoredReceiveFailureAndHidesSession()
    {
        var transport = new BlockingReceiveTransport();
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(transport));
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        await transport.ReceiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        transport.FailReceive(new InvalidOperationException("FakeTransport failed to receive."));
        await WaitForAsync(() => manager.GetSession(profile.DeviceId) is null);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.SendAsync(profile.DeviceId, new FakeMessage(41)));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("receive", exception.Operation);
        Assert.Equal("FakeTransport failed to receive.", exception.InnerException?.Message);
        Assert.Null(manager.GetSession(profile.DeviceId));
    }

    [Fact]
    public async Task ReceivePump_WhenBackgroundReceiveFails_FailsPendingRequestImmediately()
    {
        var transport = new BlockingReceiveTransport();
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(transport));
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var request = new FakeRequestMessage(77);
        var sendResult = session.Send<FakeRequestMessage, FakeResponseMessage>(request);
        await sendResult.SendCompletedTask;
        session.TryDequeueOutbound(out _);
        await transport.ReceiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        transport.FailReceive(new InvalidOperationException("FakeTransport failed to receive."));

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(async () => await sendResult.ResponseTask.WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("receive", exception.Operation);
        Assert.Equal("FakeTransport failed to receive.", exception.InnerException?.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// ?곌껐?섏? ?딆? ?μ튂 ?앸퀎?먮줈 ?≪떊?섎㈃ ?덉쇅瑜?諛쒖깮?쒗궎?붿? ?뺤씤?⑸땲??
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
        var transportFactory = new FreshTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile("device-1", 502);

        await manager.ConnectAsync(profile);
        var firstSession = manager.GetSession("device-1");
        var firstTransport = transportFactory.Transports[0];

        await manager.ConnectAsync(profile);
        var secondSession = manager.GetSession("device-1");

        Assert.NotNull(firstSession);
        Assert.NotNull(secondSession);
        Assert.NotSame(firstSession, secondSession);
        Assert.True(firstTransport.IsClosed);
    }

    [Fact]
    public async Task ConnectAsync_SameDeviceConnectedTwice_FailsPendingRequestsFromReplacedSession()
    {
        var transportFactory = new FreshTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile("device-1", 502);

        await manager.ConnectAsync(profile);
        var firstSession = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var pending = firstSession.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(55));
        await pending.SendCompletedTask;
        firstSession.TryDequeueOutbound(out _);

        await manager.ConnectAsync(profile);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(async () => await pending.ResponseTask.WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("disconnect", exception.Operation);
        Assert.Equal("Device session closed before a pending response arrived.", exception.InnerException?.Message);
        Assert.Equal(0, firstSession.PendingRequestCount);
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

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.ConnectAsync(profile));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("connect", exception.Operation);
        Assert.Equal("Transport creation failed.", exception.InnerException?.Message);
        Assert.Null(manager.GetSession(profile.DeviceId));
    }

    /// <summary>
    /// 재연결용 새 transport open이 실패하면 기존 세션을 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ReconnectOpenFails_KeepsExistingSession()
    {
        var firstTransport = new FakeTransport();
        var secondTransport = new FakeTransport { FailOnOpen = true };
        var transportFactory = new SequencedTransportFactory(firstTransport, secondTransport);
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile("device-1", 502);

        await manager.ConnectAsync(profile);
        var existingSession = manager.GetSession(profile.DeviceId);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.ConnectAsync(profile));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("connect", exception.Operation);
        Assert.Same(existingSession, manager.GetSession(profile.DeviceId));
        Assert.True(firstTransport.IsOpen);
        Assert.False(firstTransport.IsClosed);
        Assert.False(secondTransport.IsOpen);
        Assert.True(secondTransport.IsClosed);
    }

    [Fact]
    public async Task ConnectAsync_WithLinearReconnectPolicy_RetriesOpenAndEmitsEvents()
    {
        var firstTransport = new FakeTransport { FailOnOpen = true };
        var secondTransport = new FakeTransport();
        var eventSink = new RecordingConnectionEventSink();
        var retryDelays = new List<TimeSpan>();
        var manager = CreateManager(
            transportFactory: new SequencedTransportFactory(firstTransport, secondTransport),
            eventSink: eventSink,
            delayAsync: (delay, cancellationToken) =>
            {
                retryDelays.Add(delay);
                return Task.CompletedTask;
            });
        var profile = CreateProfileWithReconnect(
            CreateTcpProfile("device-1", 502),
            new ReconnectOptions
            {
                Type = "Linear",
                MaxAttempts = 1,
                IntervalMs = 250
            });

        await manager.ConnectAsync(profile);

        Assert.NotNull(manager.GetSession(profile.DeviceId));
        Assert.True(firstTransport.IsClosed);
        Assert.True(secondTransport.IsOpen);
        Assert.Equal(new[] { TimeSpan.FromMilliseconds(250) }, retryDelays);
        Assert.Equal(
            new[]
            {
                "attempt:device-1:1/2",
                "retry:device-1:1:250:FakeTransport failed to open.",
                "attempt:device-1:2/2",
                "success:device-1:2"
            },
            eventSink.Events);
    }

    [Fact]
    public async Task ConnectAsync_WithBackoffReconnectPolicy_UsesCappedRetryDelaysAndThrowsConnectException()
    {
        var firstTransport = new FakeTransport { FailOnOpen = true };
        var secondTransport = new FakeTransport { FailOnOpen = true };
        var thirdTransport = new FakeTransport { FailOnOpen = true };
        var eventSink = new RecordingConnectionEventSink();
        var retryDelays = new List<TimeSpan>();
        var manager = CreateManager(
            transportFactory: new SequencedTransportFactory(firstTransport, secondTransport, thirdTransport),
            eventSink: eventSink,
            delayAsync: (delay, cancellationToken) =>
            {
                retryDelays.Add(delay);
                return Task.CompletedTask;
            });
        var profile = CreateProfileWithReconnect(
            CreateTcpProfile("device-1", 502),
            new ReconnectOptions
            {
                Type = "Exponential",
                MaxAttempts = 2,
                BaseDelayMs = 100,
                MaxDelayMs = 150
            });

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.ConnectAsync(profile));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("connect", exception.Operation);
        Assert.Equal("FakeTransport failed to open.", exception.InnerException?.Message);
        Assert.Null(manager.GetSession(profile.DeviceId));
        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(150)
            },
            retryDelays);
        Assert.Contains("failure:device-1:connect:Device 'device-1' failed during connect. See inner exception for details.", eventSink.Events);
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

    /// <summary>
    /// 여러 inbound 청크로 들어온 메시지도 누적 수신으로 복원되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_PartialInboundChunks_ReturnsDecodedMessage()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory());
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        transportFactory.Transport.EnqueueInboundFrame(new byte[] { 0x00, 0x00, 0x00, 0x02, (byte)'4' });
        transportFactory.Transport.EnqueueInboundFrame(new byte[] { (byte)'2' });

        var message = await manager.ReceiveAsync(profile.DeviceId);

        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal(2, transportFactory.Transport.ReceiveCount);
    }

    /// <summary>
    /// 연결되지 않은 장치에서 수신을 시도하면 예외를 발생시키는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_UnknownDevice_Throws()
    {
        var manager = CreateManager();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ReceiveAsync("missing-device"));

        Assert.Contains("missing-device", exception.Message);
    }

    /// <summary>
    /// 연결 해제를 수행하면 장치 세션이 제거되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_ConnectedDevice_RemovesSession()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        await manager.DisconnectAsync(profile.DeviceId);

        Assert.Null(manager.GetSession(profile.DeviceId));
        Assert.True(transportFactory.Transport.IsClosed);
    }

    [Fact]
    public async Task DisconnectAsync_WithPendingRequest_FailsPendingRequestImmediately()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));
        var pending = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(66));
        await pending.SendCompletedTask;
        session.TryDequeueOutbound(out _);

        await manager.DisconnectAsync(profile.DeviceId);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(async () => await pending.ResponseTask.WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("disconnect", exception.Operation);
        Assert.Equal("Device session closed before a pending response arrived.", exception.InnerException?.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// 연결 해제 후에는 해당 장치로 송신할 수 없는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_ConnectedDevice_SendAfterDisconnectThrows()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        await manager.DisconnectAsync(profile.DeviceId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SendAsync(profile.DeviceId, new FakeMessage(1)));

        Assert.Contains(profile.DeviceId, exception.Message);
    }

    /// <summary>
    /// 연결 해제 후에는 해당 장치로 수신할 수 없는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_ConnectedDevice_ReceiveAfterDisconnectThrows()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        await manager.DisconnectAsync(profile.DeviceId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ReceiveAsync(profile.DeviceId));

        Assert.Contains(profile.DeviceId, exception.Message);
    }

    /// <summary>
    /// 연결 해제한 장치를 다시 연결하면 새 세션으로 복구되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_ThenConnectAgain_RegistersFreshSession()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        var firstSession = manager.GetSession(profile.DeviceId);

        await manager.DisconnectAsync(profile.DeviceId);
        await manager.ConnectAsync(profile);
        var secondSession = manager.GetSession(profile.DeviceId);

        Assert.NotNull(firstSession);
        Assert.NotNull(secondSession);
        Assert.NotSame(firstSession, secondSession);
    }

    /// <summary>
    /// 연결 해제 시 이전 연결의 queued inbound는 버려지고 재연결 후 새 연결로 넘어오지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_DropsQueuedInboundBeforeReconnect()
    {
        var transportFactory = new FreshTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            serializerFactory: new SerializerFactory());
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        transportFactory.Transports[0].EnqueueInboundFrame(
            new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol()).Encode(new FakeMessage(42)));

        await manager.DisconnectAsync(profile.DeviceId);
        await manager.ConnectAsync(profile);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<OperationCanceledException>(() => manager.ReceiveAsync(profile.DeviceId, cts.Token));
    }

    /// <summary>
    /// 연결되지 않은 장치를 해제하려 하면 예외를 발생시키는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_UnknownDevice_Throws()
    {
        var manager = CreateManager();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.DisconnectAsync("missing-device"));

        Assert.Contains("missing-device", exception.Message);
    }

    /// <summary>
    /// manager를 dispose하면 활성 연결이 모두 정리되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WithActiveConnections_ClosesAllTransportsAndRemovesSessions()
    {
        var transportFactory = new FreshTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var firstProfile = CreateTcpProfile("device-1", 502);
        var secondProfile = CreateTcpProfile("device-2", 503);

        await manager.ConnectAsync(firstProfile);
        await manager.ConnectAsync(secondProfile);

        await manager.DisposeAsync();

        Assert.Null(manager.GetSession(firstProfile.DeviceId));
        Assert.Null(manager.GetSession(secondProfile.DeviceId));
        Assert.All(transportFactory.Transports, static transport => Assert.True(transport.IsClosed));
    }

    /// <summary>
    /// manager dispose 후에는 기존 장치 송신이 차단되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_AfterDispose_SendThrowsForFormerDevice()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        await manager.DisposeAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SendAsync(profile.DeviceId, new FakeMessage(1)));

        Assert.Contains(profile.DeviceId, exception.Message);
    }

    /// <summary>
    /// queue pressure로 writer가 막힌 상태에서도 disconnect가 receive pump를 정리하고 재연결을 허용하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_WhenReceivePumpIsBackpressured_CleansUpBlockedWriterAndAllowsReconnect()
    {
        var transportFactory = new FreshTransportFactory();
        var manager = CreateManager(
            transportFactory: transportFactory,
            protocolFactory: new ProtocolFactory(),
            inboundQueueCapacity: 1);
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);

        var firstTransport = transportFactory.Transports[0];
        firstTransport.EnqueueInboundFrame(CreateInboundFrame(51));
        firstTransport.EnqueueInboundFrame(CreateInboundFrame(52));
        firstTransport.EnqueueInboundFrame(CreateInboundFrame(53));

        await WaitForAsync(() => firstTransport.ReceiveCount >= 2);
        await Task.Delay(150);

        Assert.Equal(2, firstTransport.ReceiveCount);

        await manager.DisconnectAsync(profile.DeviceId);

        Assert.Null(manager.GetSession(profile.DeviceId));
        Assert.True(firstTransport.IsClosed);

        await manager.ConnectAsync(profile);

        Assert.NotNull(manager.GetSession(profile.DeviceId));
        Assert.Equal(2, transportFactory.Transports.Count);
    }

    /// <summary>
    /// disconnect 시 transport에 걸려 있던 대기 수신도 함께 종료되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_CancelsPendingTransportReceive()
    {
        var transportFactory = new FakeTransportFactory();
        var manager = CreateManager(transportFactory: transportFactory);
        var profile = CreateTcpProfile();
        await manager.ConnectAsync(profile);
        var pendingReceive = transportFactory.Transport.ReceiveAsync();

        await manager.DisconnectAsync(profile.DeviceId);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pendingReceive);
    }

    /// <summary>
    /// 연결 시 profile의 request/response 설정이 세션의 pending 제한과 기본 timeout에 반영되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ProfileRequestResponseOptions_AreAppliedToSession()
    {
        var manager = CreateManager();
        var profile = CreateTcpProfile();
        profile = new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Enabled = profile.Enabled,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = profile.Serializer,
            RequestResponse = new RequestResponseOptions
            {
                DefaultTimeoutMs = 50,
                MaxPendingRequests = 1
            },
            Reconnect = profile.Reconnect
        };

        await manager.ConnectAsync(profile);
        var session = Assert.IsType<CommLib.Application.Sessions.DeviceSession>(manager.GetSession(profile.DeviceId));

        var first = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(40));
        await first.SendCompletedTask;
        var second = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(41));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await second.SendCompletedTask);
        await Assert.ThrowsAsync<TimeoutException>(async () => await first.ResponseTask);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// transport close가 실패하면 예외를 전파하고 기존 세션은 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisconnectAsync_WhenTransportCloseThrows_PropagatesExceptionAndKeepsSession()
    {
        var transport = new FakeTransport { FailOnClose = true };
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(transport));
        var profile = CreateTcpProfile();

        await manager.ConnectAsync(profile);
        var existingSession = manager.GetSession(profile.DeviceId);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.DisconnectAsync(profile.DeviceId));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("disconnect", exception.Operation);
        Assert.Equal("FakeTransport failed to close.", exception.InnerException?.Message);
        Assert.Same(existingSession, manager.GetSession(profile.DeviceId));
        Assert.True(transport.IsOpen);
        Assert.False(transport.IsClosed);
    }

    /// <summary>
    /// 재연결 중 기존 transport close가 실패하면 새 transport만 정리하고 기존 세션은 유지하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ReconnectCloseFails_KeepsExistingSessionAndClosesReplacementTransport()
    {
        var firstTransport = new FakeTransport { FailOnClose = true };
        var secondTransport = new FakeTransport();
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(firstTransport, secondTransport));
        var profile = CreateTcpProfile("device-1", 502);

        await manager.ConnectAsync(profile);
        var existingSession = manager.GetSession(profile.DeviceId);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(() => manager.ConnectAsync(profile));

        Assert.Equal(profile.DeviceId, exception.DeviceId);
        Assert.Equal("disconnect", exception.Operation);
        Assert.Equal("FakeTransport failed to close.", exception.InnerException?.Message);
        Assert.Same(existingSession, manager.GetSession(profile.DeviceId));
        Assert.True(firstTransport.IsOpen);
        Assert.False(firstTransport.IsClosed);
        Assert.True(secondTransport.IsClosed);
    }

    /// <summary>
    /// dispose 중 일부 transport close가 실패해도 나머지 연결 정리는 계속 시도하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_WhenOneTransportCloseThrows_ContinuesClosingRemainingConnections()
    {
        var firstTransport = new FakeTransport { FailOnClose = true };
        var secondTransport = new FakeTransport();
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(firstTransport, secondTransport));
        var firstProfile = CreateTcpProfile("device-1", 502);
        var secondProfile = CreateTcpProfile("device-2", 503);

        await manager.ConnectAsync(firstProfile);
        await manager.ConnectAsync(secondProfile);

        var exception = await Assert.ThrowsAsync<DeviceConnectionException>(async () => await manager.DisposeAsync());

        Assert.Equal(firstProfile.DeviceId, exception.DeviceId);
        Assert.Equal("disconnect", exception.Operation);
        Assert.Equal("FakeTransport failed to close.", exception.InnerException?.Message);
        Assert.NotNull(manager.GetSession(firstProfile.DeviceId));
        Assert.Null(manager.GetSession(secondProfile.DeviceId));
        Assert.True(secondTransport.IsClosed);
    }

    [Fact]
    public async Task DisposeAsync_WhenMultipleTransportCloseOperationsFail_ReturnsAggregateWithDeviceContext()
    {
        var firstTransport = new FakeTransport { FailOnClose = true };
        var secondTransport = new FakeTransport { FailOnClose = true };
        var manager = CreateManager(transportFactory: new SequencedTransportFactory(firstTransport, secondTransport));
        var firstProfile = CreateTcpProfile("device-1", 502);
        var secondProfile = CreateTcpProfile("device-2", 503);

        await manager.ConnectAsync(firstProfile);
        await manager.ConnectAsync(secondProfile);

        var exception = await Assert.ThrowsAsync<AggregateException>(async () => await manager.DisposeAsync());

        Assert.Contains("One or more device disconnect operations failed during disposal.", exception.Message);
        Assert.Collection(
            exception.InnerExceptions,
            inner =>
            {
                var deviceException = Assert.IsType<DeviceConnectionException>(inner);
                Assert.Equal(firstProfile.DeviceId, deviceException.DeviceId);
                Assert.Equal("disconnect", deviceException.Operation);
                Assert.Equal("FakeTransport failed to close.", deviceException.InnerException?.Message);
            },
            inner =>
            {
                var deviceException = Assert.IsType<DeviceConnectionException>(inner);
                Assert.Equal(secondProfile.DeviceId, deviceException.DeviceId);
                Assert.Equal("disconnect", deviceException.Operation);
                Assert.Equal("FakeTransport failed to close.", deviceException.InnerException?.Message);
            });
        Assert.NotNull(manager.GetSession(firstProfile.DeviceId));
        Assert.NotNull(manager.GetSession(secondProfile.DeviceId));
    }

    private static ConnectionManager CreateManager(
        ITransportFactory? transportFactory = null,
        IProtocolFactory? protocolFactory = null,
        ISerializerFactory? serializerFactory = null,
        IConnectionEventSink? eventSink = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        int inboundQueueCapacity = 256)
    {
        return new ConnectionManager(
            transportFactory ?? new FakeTransportFactory(),
            protocolFactory ?? new FakeProtocolFactory(),
            serializerFactory ?? new FakeSerializerFactory(),
            eventSink,
            delayAsync ?? ((_, _) => Task.CompletedTask),
            inboundQueueCapacity);
    }

    private static byte[] CreateInboundFrame(ushort messageId)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes(messageId.ToString());
        var frame = new byte[payload.Length + 4];
        frame[0] = 0x00;
        frame[1] = 0x00;
        frame[2] = 0x00;
        frame[3] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(4));
        return frame;
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

    private static DeviceProfile CreateProfileWithReconnect(DeviceProfile profile, ReconnectOptions reconnect)
    {
        return new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Enabled = profile.Enabled,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = profile.Serializer,
            RequestResponse = profile.RequestResponse,
            Reconnect = reconnect
        };
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;
    private sealed record FakeBodyMessage(ushort MessageId, string Body) : IMessage, IMessageBody;
    private sealed record FakeRequestMessage(ushort MessageId) : IRequestMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }

    private sealed record FakeResponseMessage(ushort MessageId) : IResponseMessage
    {
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public bool IsSuccess { get; init; } = true;
    }

    private sealed class FakeTransportFactory : ITransportFactory
    {
        public TransportOptions? LastOptions { get; private set; }

        public FakeTransport Transport { get; private set; } = new();

        public ITransport Create(TransportOptions options)
        {
            LastOptions = options;
            Transport = new FakeTransport();
            return Transport;
        }
    }

    private class FakeTransport : ITransport
    {
        private readonly Channel<byte[]> _inbound = Channel.CreateUnbounded<byte[]>();
        private readonly CancellationTokenSource _closeTokenSource = new();

        public string Name => "FakeTransport";

        public byte[]? LastFrame { get; private set; }

        public bool IsOpen { get; private set; }

        public int OpenCount { get; private set; }

        public int SendCount { get; private set; }

        public int ReceiveCount { get; private set; }

        public bool IsClosed { get; private set; }

        public bool FailOnOpen { get; init; }

        public bool FailOnClose { get; init; }

        public Exception? ReceiveException { get; init; }

        public virtual Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsClosed)
            {
                throw new InvalidOperationException("FakeTransport is closed.");
            }

            if (FailOnOpen)
            {
                throw new InvalidOperationException("FakeTransport failed to open.");
            }

            OpenCount++;
            IsOpen = true;
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfUnavailable();
            LastFrame = frame.ToArray();
            SendCount++;
            return Task.CompletedTask;
        }

        public void EnqueueInboundFrame(byte[] frame)
        {
            ThrowIfUnavailable();
            _inbound.Writer.TryWrite(frame);
        }

        public virtual async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfUnavailable();

            if (ReceiveException is not null)
            {
                throw ReceiveException;
            }

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);
            var frame = await _inbound.Reader.ReadAsync(linkedTokenSource.Token).ConfigureAwait(false);
            ReceiveCount++;
            return frame;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsClosed)
            {
                return Task.CompletedTask;
            }

            if (FailOnClose)
            {
                throw new InvalidOperationException("FakeTransport failed to close.");
            }

            IsClosed = true;
            IsOpen = false;
            _closeTokenSource.Cancel();
            _inbound.Writer.TryComplete();
            return Task.CompletedTask;
        }

        private void ThrowIfUnavailable()
        {
            if (IsClosed)
            {
                throw new InvalidOperationException("FakeTransport is closed.");
            }

            if (!IsOpen)
            {
                throw new InvalidOperationException("FakeTransport is not open.");
            }
        }
    }

    private sealed class BlockingOpenTransport : FakeTransport
    {
        public TaskCompletionSource OpenStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource OpenReleased { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenStarted.TrySetResult();
            await OpenReleased.Task.WaitAsync(cancellationToken);
            await base.OpenAsync(cancellationToken);
        }

        public void ReleaseOpen()
        {
            OpenReleased.TrySetResult();
        }
    }

    private sealed class BlockingReceiveTransport : FakeTransport
    {
        public TaskCompletionSource ReceiveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<Exception> _receiveFailure = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsClosed)
            {
                throw new InvalidOperationException("FakeTransport is closed.");
            }

            if (!IsOpen)
            {
                throw new InvalidOperationException("FakeTransport is not open.");
            }

            ReceiveStarted.TrySetResult();
            var exception = await _receiveFailure.Task.WaitAsync(cancellationToken);
            throw exception;
        }

        public void FailReceive(Exception exception)
        {
            _receiveFailure.TrySetResult(exception);
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

    private sealed class ResponseSerializerFactory : ISerializerFactory
    {
        public ISerializer Create(SerializerOptions options)
        {
            return new ResponseSerializer();
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

    private sealed class ResponseSerializer : ISerializer
    {
        public byte[] Serialize(IMessage message)
        {
            return System.Text.Encoding.UTF8.GetBytes(message.MessageId.ToString());
        }

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            var text = System.Text.Encoding.UTF8.GetString(payload);
            return new FakeResponseMessage(ushort.Parse(text))
            {
                CorrelationId = Guid.Parse("11111111-1111-1111-1111-111111111111")
            };
        }
    }

    private sealed class ThrowingTransportFactory : ITransportFactory
    {
        public ITransport Create(TransportOptions options)
        {
            throw new InvalidOperationException("Transport creation failed.");
        }
    }

    private sealed class FreshTransportFactory : ITransportFactory
    {
        public List<FakeTransport> Transports { get; } = new();

        public ITransport Create(TransportOptions options)
        {
            var transport = new FakeTransport();
            Transports.Add(transport);
            return transport;
        }
    }

    private sealed class SequencedTransportFactory : ITransportFactory
    {
        private readonly Queue<FakeTransport> _transports;

        public SequencedTransportFactory(params FakeTransport[] transports)
        {
            _transports = new Queue<FakeTransport>(transports);
        }

        public ITransport Create(TransportOptions options)
        {
            return _transports.Dequeue();
        }
    }

    private sealed class RecordingConnectionEventSink : IConnectionEventSink
    {
        public List<string> Events { get; } = new();

        public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
        {
            Events.Add($"attempt:{deviceId}:{attemptNumber}/{totalAttempts}");
        }

        public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
        {
            Events.Add($"retry:{deviceId}:{attemptNumber}:{delay.TotalMilliseconds:0}:{exception.Message}");
        }

        public void OnConnectSucceeded(string deviceId, int attemptNumber)
        {
            Events.Add($"success:{deviceId}:{attemptNumber}");
        }

        public void OnOperationFailed(string deviceId, string operation, Exception exception)
        {
            Events.Add($"failure:{deviceId}:{operation}:{exception.Message}");
        }
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 1000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        while (!condition())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, cts.Token);
        }
    }
}
