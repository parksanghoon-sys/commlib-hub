namespace CommLib.Domain.Messaging;

public interface IResponseMessage : IMessage
{
    Guid CorrelationId { get; }
    bool IsSuccess { get; }
}
