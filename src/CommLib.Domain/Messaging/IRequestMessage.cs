namespace CommLib.Domain.Messaging;

/// <summary>
/// Represents a message that initiates a request expecting a correlated response.
/// </summary>
public interface IRequestMessage : IMessage
{
    /// <summary>
    /// Gets the correlation identifier used to match the request with a response.
    /// </summary>
    Guid CorrelationId { get; }
}
