using System.Net;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

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

        ValidateProtocolOptions(profile);
        ValidateRequestResponseOptions(profile);
        ValidateSerializerOptions(profile);
        ValidateReconnectOptions(profile);
        ValidateTransportOptions(profile);
    }

    private static void ValidateProtocolOptions(DeviceProfile profile)
    {
        if (profile.Protocol.Type.Equals(ProtocolTypes.LengthPrefixed, StringComparison.OrdinalIgnoreCase))
        {
            if (profile.Protocol.MaxFrameLength < 4)
            {
                throw new InvalidOperationException(
                    $"[{profile.DeviceId}] LengthPrefixed MaxFrameLength must be greater than or equal to 4.");
            }

            return;
        }

        if (profile.Protocol.Type.Equals(ProtocolTypes.BinaryFrame, StringComparison.OrdinalIgnoreCase))
        {
            ValidateBinaryFrameProtocolOptions(profile.DeviceId, profile.Protocol);
            return;
        }

        throw new InvalidOperationException($"[{profile.DeviceId}] Protocol Type is invalid.");
    }

    private static void ValidateBinaryFrameProtocolOptions(string deviceId, ProtocolOptions protocol)
    {
        var binaryFrame = protocol.BinaryFrame ?? new BinaryFrameOptions();
        var lengthPrefix = binaryFrame.LengthPrefix ?? new BinaryFrameLengthPrefixOptions();
        var checksum = binaryFrame.Checksum ?? new BinaryFrameChecksumOptions();

        var startLength = GetStartHexLength(deviceId, binaryFrame.StartHex);
        if (lengthPrefix.SizeBytes is not (1 or 2 or 4))
        {
            throw new InvalidOperationException($"[{deviceId}] BinaryFrame LengthPrefix SizeBytes must be 1, 2, or 4.");
        }

        if (!Enum.IsDefined(lengthPrefix.Endianness))
        {
            throw new InvalidOperationException($"[{deviceId}] BinaryFrame LengthPrefix Endianness is invalid.");
        }

        if (!checksum.Type.Equals(BinaryFrameChecksumTypes.None, StringComparison.OrdinalIgnoreCase) &&
            !checksum.Type.Equals(BinaryFrameChecksumTypes.Crc16Modbus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"[{deviceId}] BinaryFrame Checksum Type is invalid.");
        }

        if (!Enum.IsDefined(checksum.Endianness))
        {
            throw new InvalidOperationException($"[{deviceId}] BinaryFrame Checksum Endianness is invalid.");
        }

        if (!checksum.Coverage.Equals(BinaryFrameChecksumCoverageTypes.FrameWithoutChecksum, StringComparison.OrdinalIgnoreCase) &&
            !checksum.Coverage.Equals(BinaryFrameChecksumCoverageTypes.Payload, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"[{deviceId}] BinaryFrame Checksum Coverage is invalid.");
        }

        var checksumLength = checksum.Type.Equals(BinaryFrameChecksumTypes.Crc16Modbus, StringComparison.OrdinalIgnoreCase)
            ? 2
            : 0;
        var minimumFrameLength = checked(startLength + lengthPrefix.SizeBytes + checksumLength);
        if (protocol.MaxFrameLength < minimumFrameLength)
        {
            throw new InvalidOperationException(
                $"[{deviceId}] BinaryFrame MaxFrameLength must be greater than or equal to {minimumFrameLength}.");
        }
    }

    private static int GetStartHexLength(string deviceId, string? startHex)
    {
        if (string.IsNullOrWhiteSpace(startHex))
        {
            return 0;
        }

        try
        {
            return HexPayloadParser.Parse(startHex).Length;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"[{deviceId}] BinaryFrame StartHex must contain valid hexadecimal byte pairs.", exception);
        }
    }

    private static void ValidateRequestResponseOptions(DeviceProfile profile)
    {
        if (profile.RequestResponse.DefaultTimeoutMs <= 0)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] DefaultTimeoutMs must be greater than 0.");
        }

        if (profile.RequestResponse.MaxPendingRequests <= 0)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] MaxPendingRequests must be greater than 0.");
        }
    }

    private static void ValidateSerializerOptions(DeviceProfile profile)
    {
        if (profile.Serializer.BitFieldSchema is null)
        {
            return;
        }

        if (!string.Equals(profile.Serializer.Type, SerializerTypes.RawHex, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] BitFieldSchema requires the RawHex serializer.");
        }

        try
        {
            BitFieldPayloadSchemaValidator.ValidateAndThrow(profile.Serializer.BitFieldSchema);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] Invalid BitFieldSchema: {exception.Message}", exception);
        }
    }

    private static void ValidateReconnectOptions(DeviceProfile profile)
    {
        var reconnect = profile.Reconnect;

        if (reconnect.MaxAttempts < 0)
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect MaxAttempts must be greater than or equal to 0.");
        }

        if (reconnect.Type.Equals(ReconnectTypes.None, StringComparison.OrdinalIgnoreCase))
        {
            if (reconnect.MaxAttempts != 0)
            {
                throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect MaxAttempts must be 0 when Type is None.");
            }
        }
        else if (reconnect.Type.Equals(ReconnectTypes.Linear, StringComparison.OrdinalIgnoreCase))
        {
            if (reconnect.IntervalMs <= 0)
            {
                throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect IntervalMs must be greater than 0.");
            }
        }
        else if (reconnect.Type.Equals(ReconnectTypes.Exponential, StringComparison.OrdinalIgnoreCase) ||
                 reconnect.Type.Equals(ReconnectTypes.Backoff, StringComparison.OrdinalIgnoreCase) ||
                 reconnect.Type.Equals(ReconnectTypes.ExponentialBackoff, StringComparison.OrdinalIgnoreCase))
        {
            if (reconnect.BaseDelayMs <= 0)
            {
                throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect BaseDelayMs must be greater than 0.");
            }

            if (reconnect.MaxDelayMs <= 0)
            {
                throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect MaxDelayMs must be greater than 0.");
            }

            if (reconnect.MaxDelayMs < reconnect.BaseDelayMs)
            {
                throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect MaxDelayMs must be greater than or equal to BaseDelayMs.");
            }
        }
        else
        {
            throw new InvalidOperationException($"[{profile.DeviceId}] Reconnect Type is invalid.");
        }
    }

    private static void ValidateTransportOptions(DeviceProfile profile)
    {
        switch (profile.Transport)
        {
            case TcpClientTransportOptions tcp:
                ValidateTcpTransportOptions(profile.DeviceId, tcp);
                break;

            case UdpTransportOptions udp:
                ValidateUdpTransportOptions(profile.DeviceId, udp);
                break;

            case SerialTransportOptions serial:
                ValidateSerialTransportOptions(profile.DeviceId, serial);
                break;

            case MulticastTransportOptions multicast:
                ValidateMulticastTransportOptions(profile.DeviceId, multicast);
                break;

            default:
                throw new InvalidOperationException($"[{profile.DeviceId}] Unsupported transport type.");
        }
    }

    private static void ValidateTcpTransportOptions(string deviceId, TcpClientTransportOptions tcp)
    {
        if (string.IsNullOrWhiteSpace(tcp.Host))
        {
            throw new InvalidOperationException($"[{deviceId}] TCP Host is required.");
        }

        if (tcp.Port is <= 0 or > 65535)
        {
            throw new InvalidOperationException($"[{deviceId}] TCP Port is invalid.");
        }

        if (tcp.ConnectTimeoutMs <= 0)
        {
            throw new InvalidOperationException($"[{deviceId}] TCP ConnectTimeoutMs must be greater than 0.");
        }

        if (tcp.BufferSize <= 0)
        {
            throw new InvalidOperationException($"[{deviceId}] TCP BufferSize must be greater than 0.");
        }
    }

    private static void ValidateUdpTransportOptions(string deviceId, UdpTransportOptions udp)
    {
        if (udp.LocalPort is < 0 or > 65535)
        {
            throw new InvalidOperationException($"[{deviceId}] UDP LocalPort is invalid.");
        }

        if (udp.RemotePort is <= 0 or > 65535)
        {
            throw new InvalidOperationException($"[{deviceId}] UDP RemotePort is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(udp.RemoteHost) != udp.RemotePort.HasValue)
        {
            throw new InvalidOperationException($"[{deviceId}] UDP RemoteHost and RemotePort must be configured together.");
        }
    }

    private static void ValidateSerialTransportOptions(string deviceId, SerialTransportOptions serial)
    {
        if (string.IsNullOrWhiteSpace(serial.PortName))
        {
            throw new InvalidOperationException($"[{deviceId}] Serial PortName is required.");
        }

        if (serial.BaudRate <= 0)
        {
            throw new InvalidOperationException($"[{deviceId}] Serial BaudRate is invalid.");
        }

        if (serial.DataBits is < 5 or > 8)
        {
            throw new InvalidOperationException($"[{deviceId}] Serial DataBits must be between 5 and 8.");
        }

        if (!IsSupportedSerialParity(serial.Parity))
        {
            throw new InvalidOperationException($"[{deviceId}] Serial Parity is invalid.");
        }

        if (!IsSupportedSerialStopBits(serial.StopBits))
        {
            throw new InvalidOperationException($"[{deviceId}] Serial StopBits is invalid.");
        }

        if (serial.TurnGapMs < 0)
        {
            throw new InvalidOperationException($"[{deviceId}] Serial TurnGapMs must be greater than or equal to 0.");
        }

        if (serial.ReadBufferSize <= 0)
        {
            throw new InvalidOperationException($"[{deviceId}] Serial ReadBufferSize must be greater than 0.");
        }

        if (serial.WriteBufferSize <= 0)
        {
            throw new InvalidOperationException($"[{deviceId}] Serial WriteBufferSize must be greater than 0.");
        }
    }

    private static void ValidateMulticastTransportOptions(string deviceId, MulticastTransportOptions multicast)
    {
        if (string.IsNullOrWhiteSpace(multicast.GroupAddress))
        {
            throw new InvalidOperationException($"[{deviceId}] Multicast GroupAddress is required.");
        }

        if (!IPAddress.TryParse(multicast.GroupAddress, out var groupAddress))
        {
            throw new InvalidOperationException($"[{deviceId}] Multicast GroupAddress must be a valid IP address.");
        }

        if (!IsIpv4Multicast(groupAddress))
        {
            throw new InvalidOperationException($"[{deviceId}] Multicast GroupAddress must be an IPv4 multicast address.");
        }

        if (multicast.Port is <= 0 or > 65535)
        {
            throw new InvalidOperationException($"[{deviceId}] Multicast Port is invalid.");
        }

        if (multicast.Ttl <= 0)
        {
            throw new InvalidOperationException($"[{deviceId}] Multicast Ttl must be greater than 0.");
        }

        if (!string.IsNullOrWhiteSpace(multicast.LocalInterface) &&
            !IPAddress.TryParse(multicast.LocalInterface, out _))
        {
            throw new InvalidOperationException($"[{deviceId}] Multicast LocalInterface must be a valid IP address.");
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
