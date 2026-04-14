using CommLib.Domain.Configuration;

namespace CommLib.Domain.Transport;

/// <summary>
/// 검증된 전송 설정으로부터 전송 인스턴스를 생성합니다.
/// </summary>
public interface ITransportFactory
{
    /// <summary>
    /// 지정한 옵션에 맞는 전송 구현을 생성합니다.
    /// </summary>
    /// <param name="options">검증된 전송 옵션입니다.</param>
    /// <returns>옵션 형식에 맞는 전송 구현입니다.</returns>
    ITransport Create(TransportOptions options);
}
