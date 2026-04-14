using CommLib.Domain.Configuration;
using CommLib.Domain.Protocol;
using CommLib.Infrastructure.Protocol;

namespace CommLib.Infrastructure.Factories;

/// <summary>
/// 프로토콜 설정 형식에 따라 프로토콜 구현을 생성합니다.
/// </summary>
public sealed class ProtocolFactory : IProtocolFactory
{
    /// <summary>
    /// 지정한 프로토콜 설정에 맞는 구현을 생성합니다.
    /// </summary>
    /// <param name="options">생성할 프로토콜 설정입니다.</param>
    /// <returns>설정 형식에 맞는 프로토콜 구현입니다.</returns>
    public IProtocol Create(ProtocolOptions options)
    {
        if (string.Equals(options.Type, "LengthPrefixed", StringComparison.OrdinalIgnoreCase))
        {
            return new LengthPrefixedProtocol(options.MaxFrameLength);
        }

        throw new NotSupportedException($"Unsupported protocol: {options.Type}");
    }
}
