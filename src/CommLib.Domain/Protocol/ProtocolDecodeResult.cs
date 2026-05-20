namespace CommLib.Domain.Protocol;

/// <summary>
/// 디코딩된 payload 메모리 조각과 원본 버퍼에서 소비한 전체 frame 길이를 함께 전달합니다.
/// </summary>
/// <param name="Payload">프로토콜 envelope를 제거한 payload 메모리 조각입니다.</param>
/// <param name="BytesConsumed">원본 입력 버퍼에서 하나의 frame으로 소비한 byte 수입니다.</param>
public readonly record struct ProtocolDecodeResult(ReadOnlyMemory<byte> Payload, int BytesConsumed);
