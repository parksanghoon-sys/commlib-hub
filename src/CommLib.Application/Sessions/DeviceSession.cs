using System.Threading.Channels;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

/// <summary>
/// Provides an in-memory device session backed by a bounded outbound channel.
/// </summary>
public sealed class DeviceSession : IDeviceSession
{
    /// <summary>
    /// Stores outbound messages waiting to be processed by the transport pipeline.
    /// </summary>
    private readonly Channel<IMessage> _outbound = Channel.CreateBounded<IMessage>(64);

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceSession"/> class.
    /// </summary>
    /// <param name="deviceId">The identifier of the device bound to the session.</param>
    public DeviceSession(string deviceId)
    {
        DeviceId = deviceId;
    }

    /// <summary>
    /// Gets the identifier of the device associated with this session.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// Attempts to queue a message to the outbound channel.
    /// </summary>
    /// <param name="message">The outbound message to enqueue.</param>
    /// <returns>A send result that completes when the message is accepted or faults if the queue is full.</returns>
    public ISendResult Send(IMessage message)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_outbound.Writer.TryWrite(message))
        {
            tcs.TrySetResult();
        }
        else
        {
            tcs.TrySetException(new InvalidOperationException("Outbound queue is full."));
        }

        return new SendResult(tcs.Task);
    }

    /// <summary>
    /// Queues a request and returns handles for send completion and the awaited response.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The expected response message type.</typeparam>
    /// <param name="request">The request message to enqueue.</param>
    /// <param name="timeout">The optional response timeout override.</param>
    /// <returns>A typed send result for awaiting both the send and the response.</returns>
    public ISendResult<TResponse> Send<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        var sendResult = Send(request);
        var responseTcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        return new SendResult<TResponse>(sendResult.SendCompletedTask, responseTcs.Task);
    }
}
