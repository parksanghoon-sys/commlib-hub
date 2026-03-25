using System.Threading.Channels;
using CommLib.Domain.Messaging;

namespace CommLib.Application.Sessions;

public sealed class DeviceSession : IDeviceSession
{
    private readonly Channel<IMessage> _outbound = Channel.CreateBounded<IMessage>(64);

    public DeviceSession(string deviceId)
    {
        DeviceId = deviceId;
    }

    public string DeviceId { get; }

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

    public ISendResult<TResponse> Send<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        var sendResult = Send(request);
        var responseTcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        return new SendResult<TResponse>(sendResult.SendCompletedTask, responseTcs.Task);
    }
}
