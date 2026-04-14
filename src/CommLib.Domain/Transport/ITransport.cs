namespace CommLib.Domain.Transport;

/// <summary>
/// 장치와 바이트 프레임을 주고받을 수 있는 구체적인 전송 매체의 계약입니다.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// transport가 송수신을 시작할 수 있도록 연결 및 내부 리소스를 엽니다.
    /// </summary>
    /// <param name="cancellationToken">열기 작업 취소에 사용하는 토큰입니다.</param>
    /// <returns>열기 작업입니다.</returns>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 프레임을 전송합니다.
    /// </summary>
    /// <param name="frame">전송할 프레임 바이트입니다.</param>
    /// <param name="cancellationToken">전송 취소에 사용하는 토큰입니다.</param>
    /// <returns>전송 작업입니다.</returns>
    Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default);

    /// <summary>
    /// transport에서 읽은 다음 바이트 청크를 비동기로 가져옵니다.
    /// </summary>
    /// <param name="cancellationToken">수신 취소에 사용하는 토큰입니다.</param>
    /// <returns>수신한 바이트 청크입니다.</returns>
    Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// transport가 보유한 연결 및 수신 리소스를 비동기로 정리합니다.
    /// </summary>
    /// <param name="cancellationToken">정리 작업 취소 토큰입니다.</param>
    /// <returns>정리 작업입니다.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
