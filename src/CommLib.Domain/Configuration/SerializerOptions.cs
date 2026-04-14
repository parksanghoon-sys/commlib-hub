using CommLib.Domain.Messaging;

namespace CommLib.Domain.Configuration;

/// <summary>
/// 장치 메시지에 사용할 직렬화기 선택 설정을 나타냅니다.
/// </summary>
public sealed class SerializerOptions
{
    /// <summary>
    /// 직렬화기 구현 이름을 가져옵니다.
    /// </summary>
    public string Type { get; init; } = SerializerTypes.AutoBinary;

    /// <summary>
    /// raw payload bitfield schema 설정이 필요할 때 사용하는 optional schema입니다.
    /// </summary>
    public BitFieldPayloadSchema? BitFieldSchema { get; init; }
}
