using CommLib.Domain.Messaging;

namespace CommLib.Domain.Protocol;

public interface ISerializer
{
    byte[] Serialize(IMessage message);
}
