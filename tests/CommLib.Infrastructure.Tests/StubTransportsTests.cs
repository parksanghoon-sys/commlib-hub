using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 자리표시 전송 구현의 최소 송신 동작을 검증합니다.
/// </summary>
public sealed class StubTransportsTests
{
    /// <summary>
    /// TCP 전송이 마지막 프레임과 전송 횟수를 기록하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TcpTransport_SendAsync_StoresLastFrame()
    {
        var transport = new TcpTransport();
        var frame = new byte[] { 0x01, 0x02, 0x03 };

        await transport.SendAsync(frame);

        Assert.Equal(frame, transport.LastSentFrame);
        Assert.Equal(1, transport.SendCount);
    }

    /// <summary>
    /// UDP 전송이 취소 토큰을 존중하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task UdpTransport_SendAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        var transport = new UdpTransport();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await transport.SendAsync(new byte[] { 0x01 }, cts.Token));
        Assert.Equal(0, transport.SendCount);
        Assert.Null(transport.LastSentFrame);
    }

    /// <summary>
    /// 서로 다른 전송 인스턴스가 각자 독립적으로 프레임을 기록하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task MultipleTransports_SendAsync_RecordFramesIndependently()
    {
        var serial = new SerialTransport();
        var multicast = new MulticastTransport();

        await serial.SendAsync(new byte[] { 0x10, 0x11 });
        await multicast.SendAsync(new byte[] { 0x20, 0x21, 0x22 });

        Assert.Equal(new byte[] { 0x10, 0x11 }, serial.LastSentFrame);
        Assert.Equal(new byte[] { 0x20, 0x21, 0x22 }, multicast.LastSentFrame);
        Assert.Equal(1, serial.SendCount);
        Assert.Equal(1, multicast.SendCount);
    }

    /// <summary>
    /// inbound 큐에 적재한 프레임을 ReceiveAsync로 다시 꺼낼 수 있는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task TcpTransport_ReceiveAsync_ReturnsQueuedInboundFrame()
    {
        var transport = new TcpTransport();
        transport.EnqueueInboundFrame(new byte[] { 0x01, 0x02, 0x03 });

        var frame = await transport.ReceiveAsync();

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, frame.ToArray());
        Assert.Equal(1, transport.ReceiveCount);
    }
}
