namespace CommLib.Domain.Configuration;

/// <summary>
/// 장치의 프레이밍 및 프로토콜 동작 옵션을 나타냅니다.
/// </summary>
public sealed class ProtocolOptions
{
    /// <summary>
    /// 프로토콜 구현 이름을 가져옵니다.
    /// </summary>
    public string Type { get; init; } = "LengthPrefixed";
    /// <summary>
    /// 프로토콜이 허용하는 최대 프레임 길이를 가져옵니다.
    /// </summary>
    public int MaxFrameLength { get; init; } = 65536;
    /// <summary>
    /// CRC 검증 사용 여부를 가져옵니다.
    /// </summary>
    public bool UseCrc { get; init; } = true;
    /// <summary>
    /// 선택적인 시작 문자 마커 바이트를 가져옵니다.
    /// </summary>
    public byte? Stx { get; init; }
    /// <summary>
    /// 선택적인 종료 문자 마커 바이트를 가져옵니다.
    /// </summary>
    public byte? Etx { get; init; }
}
