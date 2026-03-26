using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// 유효한 설정과 잘못된 설정에 대한 장치 프로필 검증 규칙을 확인합니다.
/// </summary>
public sealed class DeviceProfileValidatorTests
{
    /// <summary>
    /// 범위를 벗어난 포트를 가진 TCP 프로필은 거부되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_TcpInvalidPort_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "tcp-01",
            DisplayName = "TCP 01",
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = "127.0.0.1",
                Port = 70000
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions()
        };

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
    /// 최대 프레임 길이가 0 이하이면 거부되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_InvalidMaxFrameLength_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "tcp-02",
            DisplayName = "TCP 02",
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = "127.0.0.1",
                Port = 502
            },
            Protocol = new ProtocolOptions
            {
                MaxFrameLength = 0
            },
            Serializer = new SerializerOptions()
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }

    /// <summary>
    /// 최대 대기 요청 수가 0 이하이면 거부되는지 확인합니다.
    /// </summary>
    [Fact]
    public void Validate_InvalidMaxPendingRequests_Throws()
    {
        var profile = new DeviceProfile
        {
            DeviceId = "tcp-03",
            DisplayName = "TCP 03",
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = "127.0.0.1",
                Port = 502
            },
            Protocol = new ProtocolOptions(),
            Serializer = new SerializerOptions(),
            RequestResponse = new RequestResponseOptions
            {
                MaxPendingRequests = 0
            }
        };

        Assert.Throws<InvalidOperationException>(() => DeviceProfileValidator.ValidateAndThrow(profile));
    }
}
