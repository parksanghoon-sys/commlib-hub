namespace CommLib.Domain.Messaging;

public interface IRequestMessage : IMessage
{
    Guid CorrelationId { get; }
}
