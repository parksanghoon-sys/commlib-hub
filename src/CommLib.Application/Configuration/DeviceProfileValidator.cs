using System.Net;
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

        if (profile.RequestResponse.DefaultTimeoutMs <= 0)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] DefaultTimeoutMs must be greater than 0.");
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
                if (udp.RemotePort is <= 0 or > 65535)
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
                if (serial.DataBits is < 5 or > 8)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial DataBits must be between 5 and 8.");
                }
                if (!IsSupportedSerialParity(serial.Parity))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial Parity is invalid.");
                }
                if (!IsSupportedSerialStopBits(serial.StopBits))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial StopBits is invalid.");
                }
                if (serial.TurnGapMs < 0)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial TurnGapMs must be greater than or equal to 0.");
                }
                if (serial.ReadBufferSize <= 0)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial ReadBufferSize must be greater than 0.");
                }
                if (serial.WriteBufferSize <= 0)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Serial WriteBufferSize must be greater than 0.");
                }
                break;

            case MulticastTransportOptions multicast:
                if (string.IsNullOrWhiteSpace(multicast.GroupAddress))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast GroupAddress is required.");
                }
                if (!IPAddress.TryParse(multicast.GroupAddress, out var groupAddress))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast GroupAddress must be a valid IP address.");
                }
                if (!IsIpv4Multicast(groupAddress))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast GroupAddress must be an IPv4 multicast address.");
                }
                if (multicast.Port is <= 0 or > 65535)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast Port is invalid.");
                }
                if (multicast.Ttl <= 0)
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast Ttl must be greater than 0.");
                }
                if (!string.IsNullOrWhiteSpace(multicast.LocalInterface) &&
                    !IPAddress.TryParse(multicast.LocalInterface, out _))
                {
                    throw new InvalidOperationException($"[{profile.DeviceId}] Multicast LocalInterface must be a valid IP address.");
                }
                break;

            default:
                throw new InvalidOperationException($"[{profile.DeviceId}] Unsupported transport type.");
        }
    }

    private static bool IsSupportedSerialParity(string value)
    {
        return value.Equals("None", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Odd", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Even", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Mark", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Space", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedSerialStopBits(string value)
    {
        return value.Equals("None", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("One", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Two", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("OnePointFive", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIpv4Multicast(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
               bytes[0] is >= 224 and <= 239;
    }
}
