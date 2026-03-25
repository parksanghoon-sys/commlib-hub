namespace CommLib.Domain.Protocol;

/// <summary>
/// 장치 메시지 프레이밍에 적용되는 프로토콜 추상을 정의합니다.
/// </summary>
public interface IProtocol
{
    /// <summary>
    /// 프로토콜 이름을 가져옵니다.
    /// </summary>
    string Name { get; }
}
