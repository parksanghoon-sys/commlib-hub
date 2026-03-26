using CommLib.Application.Sessions;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 장치 세션의 기본 송신 계약을 검증합니다.
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
    /// 일반 메시지를 송신하면 전송 완료 작업이 성공 상태로 끝나는지 확인합니다.
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
    /// 요청 메시지를 송신하면 응답 대기 작업은 아직 완료되지 않은 상태로 반환되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestMessage_ReturnsPendingResponseTask()
    {
        var session = new DeviceSession("device-1");

        var result = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(10));

        await result.SendCompletedTask;
        Assert.True(result.SendCompletedTask.IsCompletedSuccessfully);
        Assert.False(result.ResponseTask.IsCompleted);
    }

    /// <summary>
    /// 큐 용량을 초과하면 마지막 송신이 실패하는지 확인합니다.
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
    /// 요청 큐가 가득 찬 상태에서 요청을 보내면 응답 작업은 완료되지 않은 채 유지되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task Send_RequestWhenQueueIsFull_KeepsResponseTaskPending()
    {
        var session = new DeviceSession("device-1");

        for (ushort messageId = 0; messageId < 64; messageId++)
        {
            var accepted = session.Send(new FakeMessage(messageId));
            await accepted.SendCompletedTask;
        }

        var overflow = session.Send<FakeRequestMessage, FakeResponseMessage>(new FakeRequestMessage(1000));

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await overflow.SendCompletedTask);
        Assert.False(overflow.ResponseTask.IsCompleted);
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
}
