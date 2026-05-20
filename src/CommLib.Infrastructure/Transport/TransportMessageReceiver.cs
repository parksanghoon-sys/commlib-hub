using CommLib.Domain.Messaging;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// transport에서 프레임을 수신해 메시지로 복원하는 수신기입니다.
/// </summary>
public sealed class TransportMessageReceiver
{
    private const int InitialPendingBufferSize = 256;
    private readonly MessageFrameDecoder _frameDecoder;
    private readonly ITransport _transport;
    private byte[] _pendingBuffer = Array.Empty<byte>();
    private int _pendingOffset;
    private int _pendingLength;

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

    private void AppendChunk(ReadOnlySpan<byte> chunk)
    {
        if (chunk.IsEmpty)
        {
            return;
        }

        EnsureWritableCapacity(chunk.Length);
        chunk.CopyTo(_pendingBuffer.AsSpan(_pendingOffset + _pendingLength, chunk.Length));
        _pendingLength += chunk.Length;
    }

    private bool TryDecodePending(out IMessage message)
    {
        if (_pendingLength == 0 ||
            !_frameDecoder.TryDecode(_pendingBuffer.AsMemory(_pendingOffset, _pendingLength), out var decodedMessage, out var bytesConsumed) ||
            decodedMessage is null)
        {
            message = null!;
            return false;
        }

        AdvancePending(bytesConsumed);
        message = decodedMessage;
        return true;
    }

    private void AdvancePending(int bytesConsumed)
    {
        _pendingOffset += bytesConsumed;
        _pendingLength -= bytesConsumed;

        if (_pendingLength == 0)
        {
            _pendingOffset = 0;
            return;
        }

        // 앞쪽 절반 이상을 소비한 뒤에는 남은 조각을 앞으로 당겨 다음 append가 같은 배열을 재사용하게 합니다.
        if (_pendingOffset > _pendingBuffer.Length / 2)
        {
            CompactPendingBuffer();
        }
    }

    private void EnsureWritableCapacity(int additionalLength)
    {
        if (_pendingBuffer.Length == 0)
        {
            _pendingBuffer = new byte[Math.Max(InitialPendingBufferSize, additionalLength)];
            _pendingOffset = 0;
            return;
        }

        var requiredWithCurrentOffset = _pendingOffset + _pendingLength + additionalLength;
        if (requiredWithCurrentOffset <= _pendingBuffer.Length)
        {
            return;
        }

        var requiredCompactedLength = _pendingLength + additionalLength;
        if (requiredCompactedLength <= _pendingBuffer.Length)
        {
            CompactPendingBuffer();
            return;
        }

        var nextLength = Math.Max(requiredCompactedLength, _pendingBuffer.Length * 2);
        var next = new byte[nextLength];
        _pendingBuffer.AsSpan(_pendingOffset, _pendingLength).CopyTo(next);
        _pendingBuffer = next;
        _pendingOffset = 0;
    }

    private void CompactPendingBuffer()
    {
        if (_pendingLength == 0)
        {
            _pendingOffset = 0;
            return;
        }

        _pendingBuffer.AsSpan(_pendingOffset, _pendingLength).CopyTo(_pendingBuffer);
        _pendingOffset = 0;
    }
}
