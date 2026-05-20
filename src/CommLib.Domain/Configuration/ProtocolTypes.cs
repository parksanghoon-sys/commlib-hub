namespace CommLib.Domain.Configuration;

/// <summary>
/// 프로토콜 구현을 선택할 때 사용하는 문자열 상수입니다.
/// </summary>
public static class ProtocolTypes
{
    /// <summary>
    /// 4바이트 big-endian 길이 prefix 기반 프레임입니다.
    /// </summary>
    public const string LengthPrefixed = "LengthPrefixed";

    /// <summary>
    /// 설정으로 start bytes, length prefix, checksum을 조합하는 범용 binary frame입니다.
    /// </summary>
    public const string BinaryFrame = "BinaryFrame";
}
