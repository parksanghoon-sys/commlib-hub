namespace CommLib.Domain.Transport;

/// <summary>
/// 장치와 바이트를 주고받을 수 있는 구체적인 전송 메커니즘을 나타냅니다.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// 사람이 읽을 수 있는 전송 이름을 가져옵니다.
    /// </summary>
    string Name { get; }
}
