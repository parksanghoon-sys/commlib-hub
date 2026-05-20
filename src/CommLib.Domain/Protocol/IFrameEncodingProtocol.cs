namespace CommLib.Domain.Protocol;

/// <summary>
/// serializer가 payload를 최종 frame buffer에 직접 쓴 뒤, protocol이 prefix와 suffix만 채울 수 있게 하는 선택적 계약입니다.
/// </summary>
public interface IFrameEncodingProtocol : IProtocol
{
    /// <summary>
    /// payload 길이를 기준으로 최종 frame 길이와 payload slot 위치를 계산합니다.
    /// </summary>
    /// <param name="payloadLength">serializer가 기록할 payload byte 수입니다.</param>
    /// <returns>최종 frame layout입니다.</returns>
    ProtocolFrameLayout CreateFrameLayout(int payloadLength);

    /// <summary>
    /// payload가 기록되기 전에 frame의 prefix/header 영역을 씁니다.
    /// </summary>
    /// <param name="frame">최종 frame buffer입니다.</param>
    /// <param name="layout">계산된 frame layout입니다.</param>
    void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout);

    /// <summary>
    /// payload가 기록된 뒤 suffix/checksum 영역을 마무리합니다.
    /// </summary>
    /// <param name="frame">최종 frame buffer입니다.</param>
    /// <param name="layout">계산된 frame layout입니다.</param>
    void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout);
}
