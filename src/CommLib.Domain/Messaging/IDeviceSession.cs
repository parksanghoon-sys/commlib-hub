namespace CommLib.Domain.Messaging;

/// <summary>
/// Defines the public message-sending surface for a connected device session.
/// </summary>
public interface IDeviceSession
{
    /// <summary>
    /// Gets the connected device identifier.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Sends a message that does not wait for a correlated response.
    /// </summary>
    /// <param name="message">The outbound message.</param>
    /// <returns>A send result that completes when the transport send finishes.</returns>
    ISendResult Send(IMessage message);

    /// <summary>
    /// Sends a request message and tracks the correlated response.
    /// </summary>
    /// <typeparam name="TRequest">The concrete request message type.</typeparam>
    /// <typeparam name="TResponse">The expected response message type.</typeparam>
    /// <param name="request">The outbound request message.</param>
    /// <param name="timeout">An optional response timeout.</param>
    /// <returns>A send result that exposes both send completion and response completion.</returns>
    ISendResult<TResponse> Send<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage;
}
