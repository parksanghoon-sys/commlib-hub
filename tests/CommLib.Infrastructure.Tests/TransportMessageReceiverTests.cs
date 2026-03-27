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
    public async Task ReceiveAsync_IncompleteFrame_ThrowsInvalidOperationException()
    {
        var transport = new FakeTransport(new byte[] { 0x01 });
        var receiver = new TransportMessageReceiver(
            new MessageFrameDecoder(new IncompleteProtocol(), new FakeSerializer(new FakeMessage(42))),
            transport);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => receiver.ReceiveAsync());

        Assert.Equal("Received frame did not contain a complete message.", exception.Message);
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

    private sealed record FakeMessage(ushort MessageId) : IMessage;

    private sealed class FakeTransport : ITransport
    {
        private readonly byte[] _frame;

        public FakeTransport(byte[] frame)
        {
            _frame = frame;
        }

        public string Name => "FakeTransport";

        public int ReceiveCount { get; private set; }

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceiveCount++;
            return Task.FromResult<ReadOnlyMemory<byte>>(_frame);
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
