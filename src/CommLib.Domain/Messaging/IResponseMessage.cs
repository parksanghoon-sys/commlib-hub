namespace CommLib.Domain.Messaging;

/// <summary>
/// Represents a response message correlated to a prior request.
/// </summary>
public interface IResponseMessage : IMessage
{
    /// <summary>
    /// Gets the correlation identifier of the originating request.
    /// </summary>
    Guid CorrelationId { get; }
    /// <summary>
    /// Gets a value indicating whether the response represents a successful outcome.
    /// </summary>
    bool IsSuccess { get; }
}
