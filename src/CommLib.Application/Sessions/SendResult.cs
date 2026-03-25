using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// Represents the completion handle for a queued outbound message.
/// </summary>
public sealed class SendResult : ISendResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendResult"/> class.
    /// </summary>
    /// <param name="sendCompletedTask">The task that completes when the message send is acknowledged.</param>
    public SendResult(Task sendCompletedTask)
    {
        SendCompletedTask = sendCompletedTask;
    }

    /// <summary>
    /// Gets the task that completes when the message has been queued or sent.
    /// </summary>
    public Task SendCompletedTask { get; }
}

/// <summary>
/// Represents the completion handles for a queued request that expects a typed response.
/// </summary>
/// <typeparam name="TResponse">The expected response type.</typeparam>
public sealed class SendResult<TResponse> : ISendResult<TResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendResult{TResponse}"/> class.
    /// </summary>
    /// <param name="sendCompletedTask">The task that completes when the request has been queued or sent.</param>
    /// <param name="responseTask">The task that completes when the typed response is available.</param>
    public SendResult(Task sendCompletedTask, Task<TResponse> responseTask)
    {
        SendCompletedTask = sendCompletedTask;
        ResponseTask = responseTask;
    }

    /// <summary>
    /// Gets the task that completes when the request has been queued or sent.
    /// </summary>
    public Task SendCompletedTask { get; }
    /// <summary>
    /// Gets the task that completes with the typed response.
    /// </summary>
    public Task<TResponse> ResponseTask { get; }
}
