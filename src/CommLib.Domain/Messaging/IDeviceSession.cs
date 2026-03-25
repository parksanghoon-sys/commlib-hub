namespace CommLib.Domain.Messaging;

public interface IDeviceSession
{
    string DeviceId { get; }

    ISendResult Send(IMessage message);

    ISendResult<TResponse> Send<TRequest, TResponse>(
        TRequest request,
        TimeSpan? timeout = null)
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage;
}
