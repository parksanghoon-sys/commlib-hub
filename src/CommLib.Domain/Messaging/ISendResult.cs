namespace CommLib.Domain.Messaging;

/// <summary>
/// Represents the asynchronous result of queueing or sending a message.
/// </summary>
public interface ISendResult
{
    /// <summary>
    /// Gets a task that completes when the message has been accepted for sending.
    /// </summary>
    Task SendCompletedTask { get; }
}

/// <summary>
/// Represents the asynchronous result of a request that expects a typed response.
/// </summary>
/// <typeparam name="TResponse">The expected response message type.</typeparam>
public interface ISendResult<TResponse> : ISendResult
{
    /// <summary>
    /// Gets a task that completes when the typed response arrives.
    /// </summary>
    Task<TResponse> ResponseTask { get; }
}
