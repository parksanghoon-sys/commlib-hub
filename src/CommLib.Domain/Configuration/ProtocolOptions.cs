namespace CommLib.Domain.Configuration;

/// <summary>
/// 메시지 프레이밍에 사용하는 프로토콜 설정을 나타냅니다.
/// </summary>
public sealed class ProtocolOptions
{
    /// <summary>
    /// 프로토콜 구현 이름을 가져옵니다.
    /// </summary>
    public string Type { get; init; } = "LengthPrefixed";

    /// <summary>
    /// 4바이트 길이 prefix를 포함한 최대 인코딩 프레임 길이를 가져옵니다.
    /// </summary>
    public int MaxFrameLength { get; init; } = 65536;
}
