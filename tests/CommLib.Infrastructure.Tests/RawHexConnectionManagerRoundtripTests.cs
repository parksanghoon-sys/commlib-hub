using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Sessions;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 실제 TCP transport/session 경로에서 raw-hex serializer roundtrip이 유지되는지 검증합니다.
/// </summary>
public sealed class RawHexConnectionManagerRoundtripTests
{
    [Fact]
    public async Task SendAndReceiveAsync_WithBinaryMessage_RoundTripsRawPayloadThroughTcpEcho()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var echoTask = EchoSingleLengthPrefixedFrameAsync(listener);

        await using var manager = CreateManager();
        var profile = CreateRawHexTcpProfile(((IPEndPoint)listener.LocalEndpoint).Port);
        var outbound = new BinaryMessageModel(21, new byte[] { 0xDE, 0xAD, 0x00, 0x7C, 0xFF });

        await manager.ConnectAsync(profile);
        await manager.SendAsync(profile.DeviceId, outbound);

        var inbound = Assert.IsType<BinaryMessageModel>(
            await manager.ReceiveAsync(profile.DeviceId).WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.Equal(outbound.MessageId, inbound.MessageId);
        Assert.Equal(outbound.Payload.ToArray(), inbound.Payload.ToArray());
        Assert.Equal("DE AD 00 7C FF", MessagePayloadFormatter.FormatBody(inbound));
        await echoTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SendAndReceiveAsync_WithHexTextBody_BridgesToBinaryMessageThroughTcpEcho()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var echoTask = EchoSingleLengthPrefixedFrameAsync(listener);

        await using var manager = CreateManager();
        var profile = CreateRawHexTcpProfile(((IPEndPoint)listener.LocalEndpoint).Port);

        await manager.ConnectAsync(profile);
        await manager.SendAsync(profile.DeviceId, new MessageModel(22, "de ad 00 7c ff"));

        var inbound = Assert.IsType<BinaryMessageModel>(
            await manager.ReceiveAsync(profile.DeviceId).WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.Equal((ushort)22, inbound.MessageId);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0x00, 0x7C, 0xFF }, inbound.Payload.ToArray());
        Assert.Equal("DE AD 00 7C FF", MessagePayloadFormatter.FormatBody(inbound));
        await echoTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private static async Task EchoSingleLengthPrefixedFrameAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
        using var stream = client.GetStream();
        var header = new byte[4];

        await stream.ReadExactlyAsync(header).ConfigureAwait(false);
        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
        var payload = new byte[payloadLength];
        await stream.ReadExactlyAsync(payload).ConfigureAwait(false);
        await stream.WriteAsync(header).ConfigureAwait(false);
        await stream.WriteAsync(payload).ConfigureAwait(false);
    }

    private static ConnectionManager CreateManager()
    {
        return new ConnectionManager(
            new TransportFactory(),
            new ProtocolFactory(),
            new SerializerFactory());
    }

    private static DeviceProfile CreateRawHexTcpProfile(int port)
    {
        return new DeviceProfile
        {
            DeviceId = "rawhex-device",
            DisplayName = "rawhex-device",
            Enabled = true,
            Transport = new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = IPAddress.Loopback.ToString(),
                Port = port,
                ConnectTimeoutMs = 1000,
                BufferSize = 1024,
                NoDelay = true
            },
            Protocol = new ProtocolOptions
            {
                Type = "LengthPrefixed"
            },
            Serializer = new SerializerOptions
            {
                Type = SerializerTypes.RawHex
            }
        };
    }
}
