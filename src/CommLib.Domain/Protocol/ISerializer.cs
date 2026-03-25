using CommLib.Domain.Messaging;

namespace CommLib.Domain.Protocol;

/// <summary>
/// Defines message serialization behavior.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Converts the specified message into a binary payload.
    /// </summary>
    /// <param name="message">The message instance to serialize.</param>
    /// <returns>The serialized binary payload.</returns>
    byte[] Serialize(IMessage message);
}
