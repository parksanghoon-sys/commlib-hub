using System.Threading.Channels;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// 테스트 및 골격 구현에 공통으로 쓰는 메모리 기반 전송 기본형입니다.
/// </summary>
public abstract class RecordingTransport : ITransport
{
    /// <summary>
    /// _inbound 값을 나타냅니다.
    /// </summary>
    private readonly Channel<byte[]> _inbound = Channel.CreateUnbounded<byte[]>();
    /// <summary>
    /// _closeTokenSource 값을 나타냅니다.
    /// </summary>
    private readonly CancellationTokenSource _closeTokenSource = new();

    /// <summary>
    /// transport open 여부를 나타냅니다.
    /// </summary>
    public bool IsOpen { get; private set; }

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
    /// placeholder transport를 open 상태로 전환합니다.
    /// </summary>
    /// <param name="cancellationToken">열기 취소 토큰입니다.</param>
    /// <returns>완료된 작업입니다.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsClosed)
        {
            throw new InvalidOperationException($"Transport '{Name}' is closed.");
        }

        IsOpen = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 프레임을 전송하고 마지막 프레임 정보를 기록합니다.
    /// </summary>
    /// <param name="frame">전송할 프레임 바이트입니다.</param>
    /// <param name="cancellationToken">전송 취소에 사용하는 토큰입니다.</param>
    /// <returns>완료 작업입니다.</returns>
    public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfUnavailable();
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
        ThrowIfUnavailable();
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
        ThrowIfUnavailable();
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
        IsOpen = false;
        _closeTokenSource.Cancel();
        _inbound.Writer.TryComplete();
        return Task.CompletedTask;
    }

    /// <summary>
    /// ThrowIfUnavailable 작업을 수행합니다.
    /// </summary>
    private void ThrowIfUnavailable()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException($"Transport '{Name}' is closed.");
        }

        if (!IsOpen)
        {
            throw new InvalidOperationException($"Transport '{Name}' is not open.");
        }
    }
}

/// <summary>
/// 자리표시용 UDP 전송 구현입니다.
/// </summary>
public sealed class StubUdpTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "StubUdpTransport";
}

/// <summary>
/// 자리표시용 시리얼 전송 구현입니다.
/// </summary>
public sealed class StubSerialTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "StubSerialTransport";
}

/// <summary>
/// 자리표시용 멀티캐스트 전송 구현입니다.
/// </summary>
public sealed class StubMulticastTransport : RecordingTransport
{
    /// <summary>
    /// 전송 이름을 가져옵니다.
    /// </summary>
    public override string Name => "StubMulticastTransport";
}
