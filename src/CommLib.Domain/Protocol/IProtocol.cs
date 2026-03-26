namespace CommLib.Domain.Protocol;

/// <summary>
/// 장치 메시지 프레임 인코드/디코드 동작을 정의합니다.
/// </summary>
public interface IProtocol
{
    /// <summary>
    /// 프로토콜 이름을 가져옵니다.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 페이로드를 전송 가능한 프레임으로 감쌉니다.
    /// </summary>
    /// <param name="payload">프레임에 담을 원본 페이로드입니다.</param>
    /// <returns>프로토콜 형식으로 인코드된 프레임입니다.</returns>
    byte[] Encode(ReadOnlySpan<byte> payload);

    /// <summary>
    /// 입력 버퍼에서 하나의 완전한 프레임을 읽어 페이로드를 추출합니다.
    /// </summary>
    /// <param name="buffer">프레임 추출을 시도할 입력 버퍼입니다.</param>
    /// <param name="payload">추출된 페이로드입니다.</param>
    /// <param name="bytesConsumed">사용한 전체 바이트 수입니다.</param>
    /// <returns>완전한 프레임을 읽었으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed);
}
