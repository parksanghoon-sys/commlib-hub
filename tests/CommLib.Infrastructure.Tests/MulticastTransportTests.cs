using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 실제 멀티캐스트 전송 구현의 기본 송수신과 취소 동작을 검증합니다.
/// </summary>
public sealed class MulticastTransportTests
{
    /// <summary>
    /// 전송한 datagram이 같은 그룹에 참여한 수신 소켓으로 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_SendAsync_WritesBytesToMulticastGroup()
    {
        var groupAddress = IPAddress.Parse("239.0.0.241");
        var port = GetFreeUdpPort();
        using var listener = CreateMulticastListener(groupAddress, port);
        var transport = CreateTransport(groupAddress, port);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await transport.OpenAsync();
        await transport.SendAsync(new byte[] { 0x01, 0x02, 0x03 });
        var result = await listener.ReceiveAsync(cts.Token);

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result.Buffer);
    }

    /// <summary>
    /// 그룹으로 유입된 datagram 바이트를 transport 수신 결과로 그대로 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_ReceiveAsync_ReturnsBytesFromMulticastGroup()
    {
        var groupAddress = IPAddress.Parse("239.0.0.242");
        var port = GetFreeUdpPort();
        using var sender = CreateMulticastSender();
        var transport = CreateTransport(groupAddress, port);

        await transport.OpenAsync();
        await sender.SendAsync(
            new byte[] { 0x0A, 0x0B, 0x0C },
            new IPEndPoint(groupAddress, port));
        var frame = await transport.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);

        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, frame.ToArray());
    }

    /// <summary>
    /// 대기 중인 멀티캐스트 수신이 close와 함께 취소되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task CloseAsync_PendingReceive_IsCanceled()
    {
        var groupAddress = IPAddress.Parse("239.0.0.243");
        var port = GetFreeUdpPort();
        var transport = CreateTransport(groupAddress, port);

        await transport.OpenAsync();
        var pendingReceive = transport.ReceiveAsync();

        await transport.CloseAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pendingReceive);
    }

    private static MulticastTransport CreateTransport(IPAddress groupAddress, int port)
    {
        return new MulticastTransport(new MulticastTransportOptions
        {
            Type = "Multicast",
            GroupAddress = groupAddress.ToString(),
            Port = port,
            Ttl = 1,
            Loopback = true
        });
    }

    private static UdpClient CreateMulticastListener(IPAddress groupAddress, int port)
    {
        var listener = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false
        };

        listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        listener.JoinMulticastGroup(groupAddress);
        return listener;
    }

    private static UdpClient CreateMulticastSender()
    {
        var sender = new UdpClient(AddressFamily.InterNetwork);
        sender.MulticastLoopback = true;
        sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
        return sender;
    }

    private static int GetFreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }
}
