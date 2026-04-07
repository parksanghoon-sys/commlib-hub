using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 유효한 설정과 잘못된 설정에 대한 장치 프로필 검증 규칙을 확인합니다.
/// </summary>
public sealed class DeviceProfileValidatorTests
{
    /// <summary>
    /// 비어 있는 장치 식별자는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_EmptyDeviceId_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "", displayName: "TCP 01", host: "127.0.0.1", port: 502);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 비어 있는 표시 이름은 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_EmptyDisplayName_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-01", displayName: "", host: "127.0.0.1", port: 502);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 범위를 벗어난 TCP 포트를 가진 프로필을 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_TcpInvalidPort_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-01", displayName: "TCP 01", host: "127.0.0.1", port: 70000);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 비어 있는 TCP 호스트는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_TcpEmptyHost_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-01", displayName: "TCP 01", host: "", port: 502);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 유효한 시리얼 프로필은 예외 없이 검증을 통과하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_ValidSerial_DoesNotThrow()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "serial-01",
            DisplayName = "Serial 01",
            Transport = new SerialTransportOptions
            {
                Type = "Serial",
                PortName = "COM3",
                BaudRate = 115200
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        DeviceProfileValidator.ValidateAndThrow(profile);
    }

    /// <summary>
    /// 잘못된 시리얼 baud rate는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_InvalidSerialBaudRate_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "serial-01",
            DisplayName = "Serial 01",
            Transport = new SerialTransportOptions
            {
                Type = "Serial",
                PortName = "COM3",
                BaudRate = 0
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_InvalidSerialDataBits_Throws()
    {
        var profile = CreateSerialProfile(dataBits: 4);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_InvalidSerialParity_Throws()
    {
        var profile = CreateSerialProfile(parity: "Invalid");

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_InvalidSerialStopBits_Throws()
    {
        var profile = CreateSerialProfile(stopBits: "Three");

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_NegativeSerialTurnGap_Throws()
    {
        var profile = CreateSerialProfile(turnGapMs: -1);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_NonPositiveSerialReadBufferSize_Throws()
    {
        var profile = CreateSerialProfile(readBufferSize: 0);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_NonPositiveSerialWriteBufferSize_Throws()
    {
        var profile = CreateSerialProfile(writeBufferSize: 0);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_NonPositiveDefaultTimeout_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03a", displayName: "TCP 03A", host: "127.0.0.1", port: 502);
        profile = new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = profile.Serializer,
            RequestResponse = new RequestResponseOptions
            {
                DefaultTimeoutMs = 0,
                MaxPendingRequests = profile.RequestResponse.MaxPendingRequests
            },
            Reconnect = profile.Reconnect
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_InvalidReconnectType_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03b", displayName: "TCP 03B", host: "127.0.0.1", port: 502);
        profile = CreateProfileWithReconnect(profile, new ReconnectOptions
        {
            Type = "Custom",
            MaxAttempts = 1
        });

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_BitFieldSchemaWithAutoBinarySerializer_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03bb", displayName: "TCP 03BB", host: "127.0.0.1", port: 502);
        profile = new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Enabled = profile.Enabled,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = new SerializerOptions
            {
                Type = SerializerTypes.AutoBinary,
                BitFieldSchema = CreateValidBitFieldSchema()
            },
            Reconnect = profile.Reconnect,
            RequestResponse = profile.RequestResponse
        };

        var exception = Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));

        Assert.Contains("RawHex", exception.Message);
    }

    [Fact]
    public void Validate_InvalidBitFieldSchema_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03bc", displayName: "TCP 03BC", host: "127.0.0.1", port: 502);
        profile = new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Enabled = profile.Enabled,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = new SerializerOptions
            {
                Type = SerializerTypes.RawHex,
                BitFieldSchema = new BitFieldPayloadSchema
                {
                    PayloadLengthBytes = 1,
                    Fields = new[]
                    {
                        new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 6 },
                        new BitFieldPayloadField { Name = "status", BitOffset = 4, BitLength = 4 }
                    }
                }
            },
            Reconnect = profile.Reconnect,
            RequestResponse = profile.RequestResponse
        };

        var exception = Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));

        Assert.Contains("BitFieldSchema", exception.Message);
    }

    [Fact]
    public void Validate_ReconnectNoneWithAttempts_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03c", displayName: "TCP 03C", host: "127.0.0.1", port: 502);
        profile = CreateProfileWithReconnect(profile, new ReconnectOptions
        {
            Type = "None",
            MaxAttempts = 1
        });

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_LinearReconnectWithNonPositiveInterval_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03d", displayName: "TCP 03D", host: "127.0.0.1", port: 502);
        profile = CreateProfileWithReconnect(profile, new ReconnectOptions
        {
            Type = "Linear",
            MaxAttempts = 2,
            IntervalMs = 0
        });

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_BackoffReconnectWithInvalidDelays_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03e", displayName: "TCP 03E", host: "127.0.0.1", port: 502);
        profile = CreateProfileWithReconnect(profile, new ReconnectOptions
        {
            Type = "Exponential",
            MaxAttempts = 2,
            BaseDelayMs = 1000,
            MaxDelayMs = 500
        });

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 최대 프레임 길이가 0 이하면 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_InvalidMaxFrameLength_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-02", displayName: "TCP 02", host: "127.0.0.1", port: 502);
        profile = new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Transport = profile.Transport,
            Protocol = new ProtocolOptions
            {
                MaxFrameLength = 0
            },
            Serializer = profile.Serializer,
            RequestResponse = profile.RequestResponse,
            Reconnect = profile.Reconnect
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 최대 대기 요청 수가 0 이하면 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_InvalidMaxPendingRequests_Throws()
    {
        var profile = CreateTcpProfile(deviceId: "tcp-03", displayName: "TCP 03", host: "127.0.0.1", port: 502);
        profile = new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = profile.Serializer,
            RequestResponse = new RequestResponseOptions
            {
                MaxPendingRequests = 0
            },
            Reconnect = profile.Reconnect
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 범위를 벗어난 UDP 로컬 포트는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_UdpInvalidLocalPort_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "udp-01",
            DisplayName = "UDP 01",
            Transport = new UdpTransportOptions
            {
                Type = "Udp",
                LocalPort = 70000
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_UdpRemotePortZero_Throws()
    {
        var profile = CreateUdpProfile(remoteHost: "127.0.0.1", remotePort: 0);

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// UDP 원격 호스트만 있고 원격 포트가 없으면 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_UdpRemoteHostWithoutRemotePort_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "udp-02",
            DisplayName = "UDP 02",
            Transport = new UdpTransportOptions
            {
                Type = "Udp",
                LocalPort = 5000,
                RemoteHost = "127.0.0.1"
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// UDP 원격 포트만 있고 원격 호스트가 없으면 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_UdpRemotePortWithoutRemoteHost_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "udp-03",
            DisplayName = "UDP 03",
            Transport = new UdpTransportOptions
            {
                Type = "Udp",
                LocalPort = 5000,
                RemotePort = 9000
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 비어 있는 멀티캐스트 그룹 주소는 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_MulticastEmptyGroupAddress_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "mc-01",
            DisplayName = "MC 01",
            Transport = new MulticastTransportOptions
            {
                Type = "Multicast",
                GroupAddress = "",
                Port = 5000
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 유효하지 않은 멀티캐스트 TTL은 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_MulticastInvalidTtl_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "mc-02",
            DisplayName = "MC 02",
            Transport = new MulticastTransportOptions
            {
                Type = "Multicast",
                GroupAddress = "239.0.0.1",
                Port = 5000,
                Ttl = 0
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_MulticastInvalidGroupAddress_Throws()
    {
        var profile = CreateMulticastProfile(groupAddress: "not-an-ip");

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_MulticastUnicastAddress_Throws()
    {
        var profile = CreateMulticastProfile(groupAddress: "192.168.0.10");

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    [Fact]
    public void Validate_MulticastInvalidLocalInterface_Throws()
    {
        var profile = CreateMulticastProfile(localInterface: "bad-interface");

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 지원하지 않는 전송 형식은 거부하는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_UnsupportedTransport_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "custom-01",
            DisplayName = "Custom 01",
            Transport = new UnsupportedTransportOptions { Type = "Custom" },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    private static DeviceProfile CreateTcpProfile(string deviceId, string displayName, string host, int port)
    {
        return new DeviceProfile
        {
            DeviceId = deviceId,
            DisplayName = displayName,
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = host,
                Port = port
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    private static DeviceProfile CreateSerialProfile(
        int baudRate = 115200,
        int dataBits = 8,
        string parity = "None",
        string stopBits = "One",
        int turnGapMs = 50,
        int readBufferSize = 4096,
        int writeBufferSize = 4096)
    {
        return new DeviceProfile
        {
            DeviceId = "serial-02",
            DisplayName = "Serial 02",
            Transport = new SerialTransportOptions
            {
                Type = "Serial",
                PortName = "COM3",
                BaudRate = baudRate,
                DataBits = dataBits,
                Parity = parity,
                StopBits = stopBits,
                TurnGapMs = turnGapMs,
                ReadBufferSize = readBufferSize,
                WriteBufferSize = writeBufferSize
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    private static DeviceProfile CreateUdpProfile(
        int localPort = 5000,
        string? remoteHost = null,
        int? remotePort = null)
    {
        return new DeviceProfile
        {
            DeviceId = "udp-04",
            DisplayName = "UDP 04",
            Transport = new UdpTransportOptions
            {
                Type = "Udp",
                LocalPort = localPort,
                RemoteHost = remoteHost,
                RemotePort = remotePort
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    private static DeviceProfile CreateMulticastProfile(
        string groupAddress = "239.0.0.1",
        int port = 5000,
        string? localInterface = null,
        int ttl = 1)
    {
        return new DeviceProfile
        {
            DeviceId = "mc-03",
            DisplayName = "MC 03",
            Transport = new MulticastTransportOptions
            {
                Type = "Multicast",
                GroupAddress = groupAddress,
                Port = port,
                LocalInterface = localInterface,
                Ttl = ttl
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };
    }

    private static DeviceProfile CreateProfileWithReconnect(DeviceProfile profile, ReconnectOptions reconnect)
    {
        return new DeviceProfile
        {
            DeviceId = profile.DeviceId,
            DisplayName = profile.DisplayName,
            Enabled = profile.Enabled,
            Transport = profile.Transport,
            Protocol = profile.Protocol,
            Serializer = profile.Serializer,
            Reconnect = reconnect,
            RequestResponse = profile.RequestResponse
        };
    }

    private static BitFieldPayloadSchema CreateValidBitFieldSchema()
    {
        return new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 2,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 3 },
                new BitFieldPayloadField { Name = "delta", BitOffset = 4, BitLength = 12, ScalarKind = BitFieldScalarKind.Signed }
            }
        };
    }

    private sealed record UnsupportedTransportOptions : TransportOptions;
}
