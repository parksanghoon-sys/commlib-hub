using CommLib.Domain.Configuration;

namespace CommLib.Application.Configuration;

/// <summary>
/// 장치 프로필이 런타임에서 사용되기 전에 유효한지 검증합니다.
/// </summary>
public static class DeviceProfileValidator
{
    /// <summary>
    /// 지정한 프로필을 검증하고 필수 항목이나 범위가 잘못되면 예외를 발생시킵니다.
    /// </summary>
    /// <param name="profile">검증할 장치 프로필입니다.</param>
    public static void ValidateAndThrow(DeviceProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.DeviceId))
        {
            throw new InvalidOperationException("DeviceId is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] DisplayName is required.");
        }

        if (profile.Protocol.MaxFrameLength <= 0)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] MaxFrameLength must be greater than 0.");
        }

        if (profile.RequestResponse.MaxPendingRequests <= 0)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] MaxPendingRequests must be greater than 0.");
        }

        switch (profile.Transport)
        {
            case TcpClientTransportOptions tcp:
                if (string.IsNullOrWhiteSpace(tcp.Host))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] TCP Host is required.");
                }
                if (tcp.Port is <= 0 or > 65535)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] TCP Port is invalid.");
                }
                break;

            case UdpTransportOptions udp:
                if (udp.LocalPort is < 0 or > 65535)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] UDP LocalPort is invalid.");
                }
                if (udp.RemotePort is < 0 or > 65535)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] UDP RemotePort is invalid.");
                }
                if (!string.IsNullOrWhiteSpace(udp.RemoteHost) != udp.RemotePort.HasValue)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] UDP RemoteHost and RemotePort must be configured together.");
                }
                break;

            case SerialTransportOptions serial:
                if (string.IsNullOrWhiteSpace(serial.PortName))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial PortName is required.");
                }
                if (serial.BaudRate <= 0)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial BaudRate is invalid.");
                }
                break;

            case MulticastTransportOptions multicast:
                if (string.IsNullOrWhiteSpace(multicast.GroupAddress))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast GroupAddress is required.");
                }
                if (multicast.Port is <= 0 or > 65535)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast Port is invalid.");
                }
                if (multicast.Ttl <= 0)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast Ttl must be greater than 0.");
                }
                break;

            default:
                throw new InvalidOperationException($"[{profile.DeviceId}] Unsupported transport type.");
        }
    }
}
