using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// Provides a minimal serializer that encodes only the message identifier as text.
/// </summary>
public sealed class NoOpSerializer : ISerializer
{
    /// <summary>
    /// Serializes the supplied message into a UTF-8 byte array containing its message identifier.
    /// </summary>
    /// <param name="message">The message to serialize.</param>
    /// <returns>A UTF-8 encoded representation of the message identifier.</returns>
    public byte[] Serialize(IMessage message)
    {
        return Encoding.UTF8.GetBytes(message.MessageId.ToString());
    }
}
