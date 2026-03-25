using CommLib.Domain.Configuration;

namespace CommLib.Application.Configuration;

public static class DeviceProfileValidator
{
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
                break;

            default:
                throw new InvalidOperationException($"[{profile.DeviceId}] Unsupported transport type.");
        }
    }
}
