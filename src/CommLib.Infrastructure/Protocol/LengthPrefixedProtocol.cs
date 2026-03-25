using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 길이 접두부 기반 프로토콜의 자리표시자 구현을 제공합니다.
/// </summary>
public sealed class LengthPrefixedProtocol : IProtocol
{
    /// <summary>
    /// 시스템 전반에 노출할 프로토콜 이름을 가져옵니다.
    /// </summary>
    public string Name => "LengthPrefixed";
}
