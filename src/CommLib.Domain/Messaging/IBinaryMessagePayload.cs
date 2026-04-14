namespace CommLib.Domain.Messaging;

/// <summary>
/// 메시지 payload를 raw binary 형태로 직접 전달하는 메시지 계약입니다.
/// </summary>
public interface IBinaryMessagePayload
{
    /// <summary>
    /// 직렬화 전후로 유지할 raw payload 바이트입니다.
    /// </summary>
    ReadOnlyMemory<byte> Payload { get; }
}
