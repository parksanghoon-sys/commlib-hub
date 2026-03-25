using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

public sealed class SendResult : ISendResult
{
    public SendResult(Task sendCompletedTask)
    {
        SendCompletedTask = sendCompletedTask;
    }

    public Task SendCompletedTask { get; }
}

public sealed class SendResult<TResponse> : ISendResult<TResponse>
{
    public SendResult(Task sendCompletedTask, Task<TResponse> responseTask)
    {
        SendCompletedTask = sendCompletedTask;
        ResponseTask = responseTask;
    }

    public Task SendCompletedTask { get; }
    public Task<TResponse> ResponseTask { get; }
}
