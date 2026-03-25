namespace CommLib.Domain.Messaging;

/// <summary>
/// Represents a logical messaging session for a single device.
/// </summary>
public interface IDeviceSession
{
    /// <summary>
    /// Gets the identifier of the device associated with the session.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Enqueues a fire-and-forget message for transmission.
    /// </summary>
    /// <param name="message">The outbound message to send.</param>
    /// <returns>A handle that completes when the message has been accepted for sending.</returns>
    ISendResult Send(IMessage message);

    /// <summary>
    /// Enqueues a request message and returns handles for send completion and response completion.
    /// </summary>
    /// <typeparam name="TRequest">The concrete request message type.</typeparam>
    /// <typeparam name="TResponse">The expected response message type.</typeparam>
    /// <param name="request">The request message to transmit.</param>
    /// <param name="timeout">The optional response timeout override.</param>
    /// <returns>A handle for awaiting both send completion and the eventual response.</returns>
    ISendResult<TResponse> Send<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage;
}
