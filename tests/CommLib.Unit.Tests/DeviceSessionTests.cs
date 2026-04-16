using System.Reflection;
using CommLib.Application.Sessions;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 장치 세션의 기본 송신/응답 추적 동작을 검증합니다.
/// </summary>
public sealed class DeviceSessionTests
{
    /// <summary>
    /// 생성 시 전달한 장치 식별자를 그대로 노출하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Constructor_SetsDeviceId()
    {
        var session = new DeviceSession("device-1");

        Assert.Equal("device-1", session.DeviceId);
    }

    /// <summary>
    /// 일반 메시지를 송신하면 송신 완료 작업이 성공 상태로 끝나는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_MessageWithinQueueCapacity_CompletesSuccessfully()
    {
        var session = new DeviceSession("device-1");

        var result = session.Send(new FakeMessage(1));

        await result.SendCompletedTask;
        Assert.True(result.SendCompletedTask.IsCompletedSuccessfully);
    }

    /// <summary>
    /// 일반 메시지를 송신하면 outbound 큐에서 같은 메시지를 꺼낼 수 있는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_MessageWithinQueueCapacity_EnqueuesOutboundMessage()
    {
        var session = new DeviceSession("device-1");
        var message = new FakeMessage(1);

        var result = session.Send(message);

        await result.SendCompletedTask;
        var dequeued = session.TryDequeueOutbound(out var outbound);

        Assert.True(dequeued);
        Assert.Same(message, outbound);
    }

    /// <summary>
    /// 요청 메시지를 송신하면 응답 작업이 대기 상태로 시작되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestMessage_ReturnsPendingResponseTask()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);

        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(request);

        await result.SendCompletedTask;
        var dequeued = session.TryDequeueOutbound(out var outbound);

        Assert.True(result.SendCompletedTask.IsCompletedSuccessfully);
        Assert.False(result.ResponseTask.IsCompleted);
        Assert.Equal(1, session.PendingRequestCount);
        Assert.True(dequeued);
        Assert.Same(request, outbound);
    }

    /// <summary>
    /// 응답을 완료 처리하면 대기 중이던 요청이 제거되고 응답 작업이 완료되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TryCompleteResponse_MatchingResponse_CompletesTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);
        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(request);
        var response = new FakeResponseMessage(11) { CorrelationId = request.CorrelationId };

        var completed = session.TryCompleteResponse(response);
        var completedResponse = await result.ResponseTask;

        Assert.True(completed);
        Assert.Same(response, completedResponse);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// 비제네릭 응답 완료 경로도 대기 중인 요청을 완료 처리하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TryCompleteResponse_NonGenericOverload_CompletesTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(10);
        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(request);
        IResponseMessage response = new FakeResponseMessage(11) { CorrelationId = request.CorrelationId };

        var completed = session.TryCompleteResponse(response);
        var completedResponse = await result.ResponseTask;

        Assert.True(completed);
        Assert.Equal((ushort)11, completedResponse.MessageId);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// 응답이 먼저 완료되면 대기 중인 timeout 등록도 즉시 취소되고 정리되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TryCompleteResponse_CompletesResponse_CancelsPendingTimeoutRegistration()
    {
        var session = new DeviceSession("device-1");
        var request = new FakeRequestMessage(12);
        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(request, TimeSpan.FromSeconds(5));
        var response = new FakeResponseMessage(13) { CorrelationId = request.CorrelationId };

        Assert.Equal(1, GetPendingTimeoutRegistrationCount(session));

        var completed = session.TryCompleteResponse(response);
        var completedResponse = await result.ResponseTask;

        Assert.True(completed);
        Assert.Same(response, completedResponse);
        Assert.Equal(0, session.PendingRequestCount);
        Assert.Equal(0, GetPendingTimeoutRegistrationCount(session));
    }

    /// <summary>
    /// 알 수 없는 상관관계 응답은 완료 처리하지 않고 무시하는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryCompleteResponse_UnknownCorrelationId_ReturnsFalse()
    {
        var session = new DeviceSession("device-1");

        var completed = session.TryCompleteResponse(new FakeResponseMessage(1));

        Assert.False(completed);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// 응답 대기 시간이 지나면 응답 작업이 시간 초과로 끝나고 pending 엔트리가 정리되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestWithTimeout_FailsResponseTaskAndRemovesPendingEntry()
    {
        var session = new DeviceSession("device-1");
        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(
            new FakeRequestMessage(20),
            TimeSpan.FromMilliseconds(50));

        var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await result.ResponseTask);

        Assert.Contains("Timed out waiting for response", exception.Message);
        Assert.Equal(0, session.PendingRequestCount);
        Assert.Equal(0, GetPendingTimeoutRegistrationCount(session));
    }

    /// <summary>
    /// 기본 응답 제한 시간이 구성되면 명시적 timeout 없이도 응답 task가 시간 초과되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestWithoutExplicitTimeout_UsesConfiguredDefaultTimeout()
    {
        var session = new DeviceSession(
            "device-1",
            new CommLib.Domain.Configuration.RequestResponseOptions
            {
                DefaultTimeoutMs = 50,
                MaxPendingRequests = 8
            });
        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(21));

        var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await result.ResponseTask);

        Assert.Contains("Timed out waiting for response", exception.Message);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// pending 요청 수가 설정 상한에 도달하면 후속 요청을 거부하고 추가 추적을 남기지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestWhenPendingLimitReached_FailsWithoutRegisteringAdditionalPending()
    {
        var session = new DeviceSession(
            "device-1",
            new CommLib.Domain.Configuration.RequestResponseOptions
            {
                DefaultTimeoutMs = 0,
                MaxPendingRequests = 1
            });
        var first = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(30));
        await first.SendCompletedTask;

        var second = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(31));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await second.SendCompletedTask);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await second.ResponseTask);
        Assert.Equal(1, session.PendingRequestCount);

        var completed = session.TryCompleteResponse(new FakeResponseMessage(32)
        {
            CorrelationId = ((FakeRequestMessage)Assert.IsType<FakeRequestMessage>(GetDequeuedMessage(session))).CorrelationId
        });

        Assert.True(completed);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// 송신 큐가 가득 차면 일반 메시지 송신 완료 작업이 실패하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_WhenQueueIsFull_FailsSendCompletedTask()
    {
        var session = new DeviceSession("device-1");

        for (ushort messageId = 0; messageId < 64; messageId++)
        {
            var accepted = session.Send(new FakeMessage(messageId));
            await accepted.SendCompletedTask;
        }

        var overflow = session.Send(new FakeMessage(999));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await overflow.SendCompletedTask);
    }

    /// <summary>
    /// 요청 큐가 가득 차면 응답 작업도 함께 실패하고 pending 엔트리가 남지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestWhenQueueIsFull_FailsResponseTaskAndDoesNotTrackPending()
    {
        var session = new DeviceSession("device-1");

        for (ushort messageId = 0; messageId < 64; messageId++)
        {
            var accepted = session.Send(new FakeMessage(messageId));
            await accepted.SendCompletedTask;
        }

        var overflow = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(1000));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await overflow.SendCompletedTask);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await overflow.ResponseTask);
        Assert.Equal(0, session.PendingRequestCount);
    }

    /// <summary>
    /// 테스트용 일반 메시지입니다.
    /// </summary>
    /// <param name="messageId">메시지 식별자입니다.</param>
    private sealed record FakeMessage(ushort MessageId) : IMessage;

    /// <summary>
    /// 테스트용 요청 메시지입니다.
    /// </summary>
    /// <param name="messageId">메시지 식별자입니다.</param>
    private sealed record FakeRequestMessage(ushort MessageId) : IRequestMessage
    {
        /// <summary>
        /// 요청-응답 연결에 사용하는 상관관계 식별자입니다.
        /// </summary>
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
    }

    /// <summary>
    /// 테스트용 응답 메시지입니다.
    /// </summary>
    /// <param name="messageId">메시지 식별자입니다.</param>
    private sealed record FakeResponseMessage(ushort MessageId) : IResponseMessage
    {
        /// <summary>
        /// 원본 요청과 연결되는 상관관계 식별자입니다.
        /// </summary>
        public Guid CorrelationId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// 테스트 응답은 기본적으로 성공으로 간주합니다.
        /// </summary>
        public bool IsSuccess { get; init; } = true;
    }

    private static IMessage GetDequeuedMessage(DeviceSession session)
    {
        Assert.True(session.TryDequeueOutbound(out var message));
        return Assert.IsAssignableFrom<IMessage>(message);
    }

    private static int GetPendingTimeoutRegistrationCount(DeviceSession session)
    {
        var field = typeof(DeviceSession).GetField("_pendingResponseTimeouts", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var registrations = Assert.IsType<Dictionary<Guid, CancellationTokenSource>>(field.GetValue(session));
        return registrations.Count;
    }
}
