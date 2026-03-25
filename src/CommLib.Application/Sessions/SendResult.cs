using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// 큐에 들어간 송신 메시지의 완료 핸들을 나타냅니다.
/// </summary>
public sealed class SendResult : ISendResult
{
    /// <summary>
    /// <see cref="SendResult"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="sendCompletedTask">메시지 전송이 수락되면 완료되는 작업입니다.</param>
    public SendResult(Task sendCompletedTask)
    {
        SendCompletedTask = sendCompletedTask;
    }

    /// <summary>
    /// 메시지가 큐에 들어가거나 전송되면 완료되는 작업을 가져옵니다.
    /// </summary>
    public Task SendCompletedTask { get; }
}

/// <summary>
/// 형식화된 응답을 기대하는 요청의 완료 핸들을 나타냅니다.
/// </summary>
/// <typeparam name="TResponse">기대하는 응답 형식입니다.</typeparam>
public sealed class SendResult<TResponse> : ISendResult<TResponse>
{
    /// <summary>
    /// <see cref="SendResult{TResponse}"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="sendCompletedTask">요청이 큐에 들어가거나 전송되면 완료되는 작업입니다.</param>
    /// <param name="responseTask">형식화된 응답을 받을 때 완료되는 작업입니다.</param>
    public SendResult(Task sendCompletedTask, Task<TResponse> responseTask)
    {
        SendCompletedTask = sendCompletedTask;
        ResponseTask = responseTask;
    }

    /// <summary>
    /// 요청이 큐에 들어가거나 전송되면 완료되는 작업을 가져옵니다.
    /// </summary>
    public Task SendCompletedTask { get; }
    /// <summary>
    /// 형식화된 응답과 함께 완료되는 작업을 가져옵니다.
    /// </summary>
    public Task<TResponse> ResponseTask { get; }
}
