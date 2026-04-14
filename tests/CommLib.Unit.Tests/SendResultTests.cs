using CommLib.Application.Sessions;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 송신 결과 핸들이 전달받은 작업을 그대로 노출하는지 검증합니다.
/// </summary>
public sealed class SendResultTests
{
    /// <summary>
    /// 비형식화된 송신 결과가 전달받은 완료 작업을 그대로 보관하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// Constructor_StoresSendCompletedTask 작업을 수행합니다.
    /// </summary>
    public void Constructor_StoresSendCompletedTask()
    {
        var sendTask = Task.CompletedTask;

        var result = new SendResult(sendTask);

        Assert.Same(sendTask, result.SendCompletedTask);
    }

    /// <summary>
    /// 형식화된 송신 결과가 전송 완료 작업과 응답 작업을 각각 그대로 보관하는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// GenericConstructor_StoresSendAndResponseTasks 작업을 수행합니다.
    /// </summary>
    public void GenericConstructor_StoresSendAndResponseTasks()
    {
        var sendTask = Task.CompletedTask;
        var responseTask = Task.FromResult(new FakeResponseMessage(7));

        var result = new SendResult<FakeResponseMessage>(sendTask, responseTask);

        Assert.Same(sendTask, result.SendCompletedTask);
        Assert.Same(responseTask, result.ResponseTask);
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
