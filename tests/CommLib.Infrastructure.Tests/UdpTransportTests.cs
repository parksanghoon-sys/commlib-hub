using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 실제 UDP 전송 구현의 기본 송수신과 취소 동작을 검증합니다.
/// </summary>
public sealed class UdpTransportTests
{
    /// <summary>
    /// 기본 원격 endpoint가 구성된 경우 datagram을 원격 UDP 소켓으로 전송하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_SendAsync_WritesBytesToRemoteEndpoint()
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var transport = new UdpTransport(new UdpTransportOptions
        {
            Type = "Udp",
            LocalPort = 0,
            RemoteHost = IPAddress.Loopback.ToString(),
            RemotePort = ((IPEndPoint)server.Client.LocalEndPoint!).Port
        });

        await transport.OpenAsync();
        await transport.SendAsync(new byte[] { 0x01, 0x02, 0x03 });
        var result = await server.ReceiveAsync();

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result.Buffer);
    }

    /// <summary>
    /// 수신한 datagram 바이트를 transport 수신 결과로 그대로 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_ReceiveAsync_ReturnsBytesFromRemoteEndpoint()
    {
        var localPort = GetFreeUdpPort();
        using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var transport = new UdpTransport(new UdpTransportOptions
        {
            Type = "Udp",
            LocalPort = localPort
        });

        await transport.OpenAsync();
        await sender.SendAsync(new byte[] { 0x0A, 0x0B, 0x0C }, new IPEndPoint(IPAddress.Loopback, localPort));
        var frame = await transport.ReceiveAsync();

        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, frame.ToArray());
    }

    /// <summary>
    /// 기본 원격 endpoint가 없으면 send를 수행할 수 없는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithoutConfiguredRemoteEndpoint_Throws()
    {
        var transport = new UdpTransport(new UdpTransportOptions
        {
            Type = "Udp",
            LocalPort = 0
        });

        await transport.OpenAsync();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendAsync(new byte[] { 0x01 }));

        Assert.Contains("remote endpoint", exception.Message);
    }

    /// <summary>
    /// 대기 중인 UDP 수신이 close와 함께 취소되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task CloseAsync_PendingReceive_IsCanceled()
    {
        var transport = new UdpTransport(new UdpTransportOptions
        {
            Type = "Udp",
            LocalPort = GetFreeUdpPort()
        });

        await transport.OpenAsync();
        var pendingReceive = transport.ReceiveAsync();

        await transport.CloseAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pendingReceive);
    }

    private static int GetFreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }
}
