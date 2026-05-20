namespace CommLib.Domain.Protocol;

/// <summary>
/// 최종 protocol frame 안에서 payload가 들어갈 위치와 전체 frame 길이를 설명합니다.
/// </summary>
/// <param name="FrameLength">header, payload, suffix/checksum을 모두 포함한 최종 frame 길이입니다.</param>
/// <param name="PayloadOffset">최종 frame 안에서 payload가 시작되는 byte 위치입니다.</param>
/// <param name="PayloadLength">serializer가 기록해야 할 payload byte 길이입니다.</param>
public readonly record struct ProtocolFrameLayout(int FrameLength, int PayloadOffset, int PayloadLength);
