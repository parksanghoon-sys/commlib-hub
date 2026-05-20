namespace CommLib.Domain.Configuration;

/// <summary>
/// 메시지 프레이밍에 사용하는 프로토콜 설정을 나타냅니다.
/// </summary>
public sealed class ProtocolOptions
{
    /// <summary>
    /// 프로토콜 구현 이름을 가져옵니다.
    /// </summary>
    public string Type { get; init; } = ProtocolTypes.LengthPrefixed;

    /// <summary>
    /// protocol envelope를 포함한 최대 encoded frame 길이를 가져옵니다.
    /// </summary>
    public int MaxFrameLength { get; init; } = 65536;

    /// <summary>
    /// <c>BinaryFrame</c> protocol을 사용할 때 적용할 frame envelope 설정입니다.
    /// </summary>
    public BinaryFrameOptions BinaryFrame { get; init; } = new();
}
