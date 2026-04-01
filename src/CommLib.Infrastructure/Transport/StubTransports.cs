using System.Threading.Channels;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// 테스트 및 골격 구현에 공통으로 쓰는 메모리 기반 전송 기본형입니다.
/// </summary>
public abstract class RecordingTransport : ITransport
{
    private readonly Channel<byte[]> _inbound = Channel.CreateUnbounded<byte[]>();
    private readonly CancellationTokenSource _closeTokenSource = new();

    /// <summary>
    /// transport 정리 여부를 나타냅니다.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// 마지막으로 전송한 프레임을 가져옵니다.
    /// </summary>
    public byte[]? LastSentFrame { get; private set; }

    /// <summary>
    /// 누적 전송 횟수를 가져옵니다.
    /// </summary>
    public int SendCount { get; private set; }

    /// <summary>
    /// 수신 대기열에 적재한 마지막 프레임을 가져옵니다.
    /// </summary>
    public byte[]? LastQueuedInboundFrame { get; private set; }

    /// <summary>
    /// 누적 수신 횟수를 가져옵니다.
    /// </summary>
    public int ReceiveCount { get; private set; }

    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 프레임을 전송하고 마지막 프레임 정보를 기록합니다.
    /// </summary>
    /// <param name="frame">전송할 프레임 바이트입니다.</param>
    /// <param name="cancellationToken">전송 취소에 사용하는 토큰입니다.</param>
    /// <returns>완료 작업입니다.</returns>
    public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfClosed();
        LastSentFrame = frame.ToArray();
        SendCount++;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 테스트용 inbound 프레임을 수신 대기열에 적재합니다.
    /// </summary>
    /// <param name="frame">수신 대기열에 넣을 프레임입니다.</param>
    public void EnqueueInboundFrame(byte[] frame)
    {
        ThrowIfClosed();
        LastQueuedInboundFrame = frame;
        _inbound.Writer.TryWrite(frame);
    }

    /// <summary>
    /// 다음 inbound 프레임을 비동기로 가져옵니다.
    /// </summary>
    /// <param name="cancellationToken">수신 취소에 사용하는 토큰입니다.</param>
    /// <returns>수신한 프레임입니다.</returns>
    public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfClosed();
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);
        var frame = await _inbound.Reader.ReadAsync(linkedTokenSource.Token).ConfigureAwait(false);
        ReceiveCount++;
        return frame;
    }

    /// <summary>
    /// transport가 보유한 수신 리소스를 정리하고 이후 송수신을 차단합니다.
    /// </summary>
    /// <param name="cancellationToken">정리 작업 취소 토큰입니다.</param>
    /// <returns>완료된 작업입니다.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsClosed)
        {
            return Task.CompletedTask;
        }

        IsClosed = true;
        _closeTokenSource.Cancel();
        _inbound.Writer.TryComplete();
        return Task.CompletedTask;
    }

    private void ThrowIfClosed()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException($"Transport '{Name}' is closed.");
        }
    }
}

/// <summary>
/// 자리표시용 TCP 전송 구현입니다.
/// </summary>
public sealed class TcpTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "TcpTransport";
}

/// <summary>
/// 자리표시용 UDP 전송 구현입니다.
/// </summary>
public sealed class UdpTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "UdpTransport";
}

/// <summary>
/// 자리표시용 시리얼 전송 구현입니다.
/// </summary>
public sealed class SerialTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "SerialTransport";
}

/// <summary>
/// 자리표시용 멀티캐스트 전송 구현입니다.
/// </summary>
public sealed class MulticastTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "MulticastTransport";
}
