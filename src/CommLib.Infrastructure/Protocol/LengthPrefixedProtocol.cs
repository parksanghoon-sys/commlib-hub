using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

public sealed class LengthPrefixedProtocol : IProtocol
{
    public string Name => "LengthPrefixed";
}
