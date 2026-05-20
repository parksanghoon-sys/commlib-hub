namespace CommLib.Domain.Protocol;

/// <summary>
/// serializer가 payload를 최종 frame buffer에 직접 쓴 뒤, protocol이 prefix와 suffix만 채울 수 있게 하는 선택적 계약입니다.
/// </summary>
/// <remarks>
/// 호출자는 반드시 <see cref="CreateFrameLayout"/>, <see cref="WriteFramePrefix"/>, payload 기록,
/// <see cref="WriteFrameSuffix"/> 순서로 실행해야 합니다.
/// </remarks>
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
    /// <remarks>
    /// 이 메서드는 payload slot이 채워지기 전에 호출합니다.
    /// </remarks>
    void WriteFramePrefix(Span<byte> frame, ProtocolFrameLayout layout);

    /// <summary>
    /// payload가 기록된 뒤 suffix/checksum 영역을 마무리합니다.
    /// </summary>
    /// <param name="frame">최종 frame buffer입니다.</param>
    /// <param name="layout">계산된 frame layout입니다.</param>
    /// <remarks>
    /// checksum이 payload를 포함할 수 있으므로 payload 기록이 끝난 뒤 호출합니다.
    /// </remarks>
    void WriteFrameSuffix(Span<byte> frame, ProtocolFrameLayout layout);
}
