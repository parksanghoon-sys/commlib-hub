using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Transport;

namespace CommLib.Infrastructure.Factories;

/// <summary>
/// 전송 옵션 형식에 따라 전송 스텁을 생성합니다.
/// </summary>
public sealed class TransportFactory : ITransportFactory
{
    /// <summary>
    /// 지정한 전송 옵션에 맞는 전송 구현을 생성합니다.
    /// </summary>
    /// <param name="options">생성할 전송을 설명하는 전송 옵션입니다.</param>
    /// <returns>전달된 옵션에 맞는 전송 구현입니다.</returns>
    public ITransport Create(TransportOptions options)
    {
        return options switch
        {
            TcpClientTransportOptions => new TcpTransport(),
            UdpTransportOptions => new UdpTransport(),
            SerialTransportOptions => new SerialTransport(),
            MulticastTransportOptions => new MulticastTransport(),
            _ => throw new NotSupportedException($"Unsupported transport: {options.GetType().Name}")
        };
    }
}
