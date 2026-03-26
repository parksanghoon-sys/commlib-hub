using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// 테스트 및 골격 구현에 공통으로 쓰는 메모리 기반 전송 기본형입니다.
/// </summary>
public abstract class RecordingTransport : ITransport
{
    /// <summary>
    /// 마지막으로 전송한 프레임을 가져옵니다.
    /// </summary>
    public byte[]? LastSentFrame { get; private set; }

    /// <summary>
    /// 누적 전송 횟수를 가져옵니다.
    /// </summary>
    public int SendCount { get; private set; }

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
        LastSentFrame = frame.ToArray();
        SendCount++;
        return Task.CompletedTask;
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
