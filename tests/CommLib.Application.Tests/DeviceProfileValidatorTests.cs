using CommLib.Application.Configuration;
using CommLib.Domain.Configuration;
using Xunit;

namespace CommLib.Application.Tests;

/// <summary>
/// Verifies device profile validation rules for valid and invalid configuration.
/// </summary>
public sealed class DeviceProfileValidatorTests
{
    /// <summary>
    /// Ensures TCP profiles with out-of-range ports are rejected.
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
    /// Ensures a valid serial profile passes validation without throwing.
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
}
