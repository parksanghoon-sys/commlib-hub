using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// transport에서 프레임을 수신해 메시지로 복원하는 수신기입니다.
/// </summary>
public sealed class TransportMessageReceiver
{
    /// <summary>
    /// _frameDecoder 값을 나타냅니다.
    /// </summary>
    private readonly MessageFrameDecoder _frameDecoder;
    /// <summary>
    /// _transport 값을 나타냅니다.
    /// </summary>
    private readonly ITransport _transport;
    /// <summary>
    /// _pendingBuffer 값을 나타냅니다.
    /// </summary>
    private byte[] _pendingBuffer = Array.Empty<byte>();

    /// <summary>
    /// <see cref="TransportMessageReceiver"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="frameDecoder">프레임 디코더입니다.</param>
    /// <param name="transport">프레임을 읽어올 전송 매체입니다.</param>
    public TransportMessageReceiver(MessageFrameDecoder frameDecoder, ITransport transport)
    {
        _frameDecoder = frameDecoder;
        _transport = transport;
    }

    /// <summary>
    /// transport에서 수신한 바이트 청크를 누적해 다음 완전한 메시지를 복원합니다.
    /// </summary>
    /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
    /// <returns>복원된 메시지입니다.</returns>
    public async Task<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (TryDecodePending(out var message))
            {
                return message;
            }

            var chunk = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            AppendChunk(chunk.Span);
        }
    }

    /// <summary>
    /// 입력 frame 버퍼에서 메시지를 복원합니다.
    /// </summary>
    /// <param name="buffer">복원할 frame 버퍼입니다.</param>
    /// <param name="message">복원된 메시지입니다.</param>
    /// <param name="bytesConsumed">소비한 바이트 수입니다.</param>
    /// <returns>완전한 메시지를 복원했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryDecode(ReadOnlySpan<byte> buffer, out IMessage message, out int bytesConsumed)
    {
        if (_frameDecoder.TryDecode(buffer, out var decodedMessage, out bytesConsumed) && decodedMessage is not null)
        {
            message = decodedMessage;
            return true;
        }

        message = null!;
        return false;
    }

    /// <summary>
    /// AppendChunk 작업을 수행합니다.
    /// </summary>
    private void AppendChunk(ReadOnlySpan<byte> chunk)
    {
        if (chunk.IsEmpty)
        {
            return;
        }

        var merged = new byte[_pendingBuffer.Length + chunk.Length];
        _pendingBuffer.AsSpan().CopyTo(merged);
        chunk.CopyTo(merged.AsSpan(_pendingBuffer.Length));
        _pendingBuffer = merged;
    }

    /// <summary>
    /// TryDecodePending 작업을 수행합니다.
    /// </summary>
    private bool TryDecodePending(out IMessage message)
    {
        if (!TryDecode(_pendingBuffer, out message, out var bytesConsumed))
        {
            return false;
        }

        _pendingBuffer = _pendingBuffer.AsSpan(bytesConsumed).ToArray();
        return true;
    }
}
