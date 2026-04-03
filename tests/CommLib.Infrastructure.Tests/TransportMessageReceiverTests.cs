using System.Threading.Channels;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// transport message receiver가 transport와 frame decoder를 조합하는지 검증합니다.
/// </summary>
public sealed class TransportMessageReceiverTests
{
    /// <summary>
    /// transport에서 수신한 프레임을 디코드해 메시지를 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_FrameFromTransport_ReturnsDecodedMessage()
    {
        var transport = new FakeTransport(new byte[] { 0x01, 0x02 });
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new FakeProtocol(new byte[] { 0x10 }), new FakeSerializer(new FakeMessage(42))),
            transport);

        var message = await receiver.ReceiveAsync();

        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal(1, transport.ReceiveCount);
    }

    /// <summary>
    /// decoder가 완전한 메시지를 복원하지 못하면 예외를 발생시키는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_IncompleteChunk_WaitsUntilCancellation()
    {
        var transport = new FakeTransport(new byte[] { 0x01 });
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new IncompleteProtocol(), new FakeSerializer(new FakeMessage(42))),
            transport);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<OperationCanceledException>(() => receiver.ReceiveAsync(cancellationTokenSource.Token));
        Assert.Equal(1, transport.ReceiveCount);
    }

    /// <summary>
    /// transport 수신 취소가 receiver까지 그대로 전파되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var transport = new FakeTransport(new byte[] { 0x01, 0x02 });
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new FakeProtocol(new byte[] { 0x10 }), new FakeSerializer(new FakeMessage(42))),
            transport);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => receiver.ReceiveAsync(cancellationTokenSource.Token));
    }

    /// <summary>
    /// transport가 프레임을 여러 조각으로 나눠 전달해도 내부 버퍼로 완성해 복원하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_PartialChunksAcrossMultipleReads_ReturnsDecodedMessage()
    {
        var frame = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol()).Encode(new MessageModel(42));
        var transport = new FakeTransport(frame[..3], frame[3..]);
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new LengthPrefixedProtocol(), new NoOpSerializer()),
            transport);

        var message = await receiver.ReceiveAsync();

        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal(2, transport.ReceiveCount);
    }

    /// <summary>
    /// 한 번의 transport 수신에 여러 프레임이 들어오면 나머지 버퍼를 다음 ReceiveAsync 호출에 재사용하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_MultipleFramesInSingleChunk_ReusesBufferedRemainder()
    {
        var encoder = new MessageFrameEncoder(new NoOpSerializer(), new LengthPrefixedProtocol());
        var firstFrame = encoder.Encode(new MessageModel(42));
        var secondFrame = encoder.Encode(new MessageModel(43));
        var combinedChunk = new byte[firstFrame.Length + secondFrame.Length];
        firstFrame.CopyTo(combinedChunk, 0);
        secondFrame.CopyTo(combinedChunk, firstFrame.Length);

        var transport = new FakeTransport(combinedChunk);
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new LengthPrefixedProtocol(), new NoOpSerializer()),
            transport);

        var firstMessage = await receiver.ReceiveAsync();
        var secondMessage = await receiver.ReceiveAsync();

        Assert.Equal((ushort)42, firstMessage.MessageId);
        Assert.Equal((ushort)43, secondMessage.MessageId);
        Assert.Equal(1, transport.ReceiveCount);
    }

    /// <summary>
    /// malformed frame으로 프로토콜 디코더가 예외를 던지면 그대로 전파하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task ReceiveAsync_MalformedFrame_PropagatesDecoderException()
    {
        var transport = new FakeTransport(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new LengthPrefixedProtocol(), new NoOpSerializer()),
            transport);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => receiver.ReceiveAsync());

        Assert.Equal("Frame length cannot be negative.", exception.Message);
    }

    /// <summary>
    /// 직접 decode 경로도 완전한 frame을 메시지로 복원하는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryDecode_CompleteFrame_ReturnsDecodedMessage()
    {
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new FakeProtocol(new byte[] { 0x10 }), new FakeSerializer(new FakeMessage(42))),
            new FakeTransport(new byte[] { 0x01, 0x02 }));

        var handled = receiver.TryDecode(new byte[] { 0x01, 0x02 }, out var message, out var bytesConsumed);

        Assert.True(handled);
        Assert.Equal((ushort)42, message.MessageId);
        Assert.Equal(2, bytesConsumed);
    }

    /// <summary>
    /// 직접 decode 경로는 미완전 frame에서 false를 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public void TryDecode_IncompleteFrame_ReturnsFalse()
    {
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new IncompleteProtocol(), new FakeSerializer(new FakeMessage(42))),
            new FakeTransport(new byte[] { 0x01 }));

        var handled = receiver.TryDecode(new byte[] { 0x01 }, out var message, out var bytesConsumed);

        Assert.False(handled);
        Assert.Null(message);
        Assert.Equal(0, bytesConsumed);
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;

    private sealed class FakeTransport : ITransport
    {
        private readonly Channel<byte[]> _frames = Channel.CreateUnbounded<byte[]>();

        public FakeTransport(params byte[][] frames)
        {
            foreach (var frame in frames)
            {
                _frames.Writer.TryWrite(frame);
            }
        }

        public string Name => "FakeTransport";

        public int ReceiveCount { get; private set; }

        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = await _frames.Reader.ReadAsync(cancellationToken);
            ReceiveCount++;
            return frame;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProtocol : IProtocol
    {
        private readonly byte[] _payload;

        public FakeProtocol(byte[] payload)
        {
            _payload = payload;
        }

        public string Name => "Fake";

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            if (buffer.IsEmpty)
            {
                payload = Array.Empty<byte>();
                bytesConsumed = 0;
                return false;
            }

            payload = _payload;
            bytesConsumed = buffer.Length;
            return true;
        }
    }

    private sealed class IncompleteProtocol : IProtocol
    {
        public string Name => "Incomplete";

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }
    }

    private sealed class FakeSerializer : ISerializer
    {
        private readonly IMessage _message;

        public FakeSerializer(IMessage message)
        {
            _message = message;
        }

        public byte[] Serialize(IMessage message)
        {
            throw new NotSupportedException();
        }

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            return _message;
        }
    }
}
