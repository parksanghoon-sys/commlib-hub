namespace CommLib.Domain.Messaging;

/// <summary>
/// Represents the minimal contract shared by all messages.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Gets the message type or command identifier.
    /// </summary>
    ushort MessageId { get; }
}
