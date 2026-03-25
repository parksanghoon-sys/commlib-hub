using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// 자리표시자 TCP 전송 구현을 나타냅니다.
/// </summary>
public sealed class TcpTransport : ITransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public string Name => "TcpTransport";
}

/// <summary>
/// 자리표시자 UDP 전송 구현을 나타냅니다.
/// </summary>
public sealed class UdpTransport : ITransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public string Name => "UdpTransport";
}

/// <summary>
/// 자리표시자 시리얼 전송 구현을 나타냅니다.
/// </summary>
public sealed class SerialTransport : ITransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public string Name => "SerialTransport";
}

/// <summary>
/// 자리표시자 멀티캐스트 전송 구현을 나타냅니다.
/// </summary>
public sealed class MulticastTransport : ITransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public string Name => "MulticastTransport";
}
