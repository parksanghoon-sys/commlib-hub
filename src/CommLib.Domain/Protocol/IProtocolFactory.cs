using CommLib.Domain.Configuration;

namespace CommLib.Domain.Protocol;

/// <summary>
/// 검증된 프로토콜 설정으로부터 프로토콜 구현을 생성합니다.
/// </summary>
public interface IProtocolFactory
{
    /// <summary>
    /// 지정한 설정에 맞는 프로토콜 구현을 생성합니다.
    /// </summary>
    /// <param name="options">검증된 프로토콜 설정입니다.</param>
    /// <returns>설정 형식에 맞는 프로토콜 구현입니다.</returns>
    IProtocol Create(ProtocolOptions options);
}
