using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 실제 TCP 전송 구현의 기본 연결/송수신 동작을 검증합니다.
/// </summary>
public sealed class TcpTransportTests
{
    /// <summary>
    /// open 이후 송신한 바이트가 서버 측으로 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// OpenAsync_SendAsync_WritesBytesToServer 작업을 수행합니다.
    /// </summary>
    public async Task OpenAsync_SendAsync_WritesBytesToServer()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var acceptTask = listener.AcceptTcpClientAsync();
        var transport = CreateTransport(listener);

        await transport.OpenAsync();
        using var serverClient = await acceptTask;
        await transport.SendAsync(new byte[] { 0x01, 0x02, 0x03 });

        var buffer = new byte[3];
        await serverClient.GetStream().ReadExactlyAsync(buffer);

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, buffer);
    }

    /// <summary>
    /// 서버가 보낸 바이트 청크를 transport 수신으로 그대로 읽는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// OpenAsync_ReceiveAsync_ReturnsBytesFromServer 작업을 수행합니다.
    /// </summary>
    public async Task OpenAsync_ReceiveAsync_ReturnsBytesFromServer()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var acceptTask = listener.AcceptTcpClientAsync();
        var transport = CreateTransport(listener);

        await transport.OpenAsync();
        using var serverClient = await acceptTask;
        await serverClient.GetStream().WriteAsync(new byte[] { 0x0A, 0x0B, 0x0C });

        var frame = await transport.ReceiveAsync();

        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, frame.ToArray());
    }

    /// <summary>
    /// 대기 중인 TCP 수신은 close 시 취소되어 빠져나오는지 확인합니다.
    /// </summary>
    [Fact]
    /// <summary>
    /// CloseAsync_PendingReceive_IsCanceled 작업을 수행합니다.
    /// </summary>
    public async Task CloseAsync_PendingReceive_IsCanceled()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var acceptTask = listener.AcceptTcpClientAsync();
        var transport = CreateTransport(listener);

        await transport.OpenAsync();
        using var serverClient = await acceptTask;
        var pendingReceive = transport.ReceiveAsync();

        await transport.CloseAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pendingReceive);
    }

    /// <summary>
    /// CreateTransport 작업을 수행합니다.
    /// </summary>
    private static TcpTransport CreateTransport(TcpListener listener)
    {
        return new TcpTransport(new TcpClientTransportOptions
        {
            Type = "TcpClient",
            Host = IPAddress.Loopback.ToString(),
            Port = ((IPEndPoint)listener.LocalEndpoint).Port,
            ConnectTimeoutMs = 1000,
            BufferSize = 1024,
            NoDelay = true
        });
    }
}
