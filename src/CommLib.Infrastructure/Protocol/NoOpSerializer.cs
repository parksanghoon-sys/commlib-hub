using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

public sealed class NoOpSerializer : ISerializer
{
    public byte[] Serialize(IMessage message)
    {
        return Encoding.UTF8.GetBytes(message.MessageId.ToString());
    }
}
