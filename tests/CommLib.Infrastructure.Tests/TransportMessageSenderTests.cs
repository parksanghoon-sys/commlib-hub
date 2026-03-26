using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;
using CommLib.Domain.Transport;
using CommLib.Infrastructure.Protocol;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 메시지 송신기가 frame encoder와 transport를 올바르게 조합하는지 검증합니다.
/// </summary>
public sealed class TransportMessageSenderTests
{
    /// <summary>
    /// 메시지를 보내면 인코드된 프레임이 transport로 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_Message_SendsEncodedFrameToTransport()
    {
        var transport = new FakeTransport();
        var sender = new TransportMessageSender(
            new MessageFrameEncoder(new FakeSerializer(new byte[] { 0x10, 0x20 }), new FakeProtocol(new byte[] { 0xAA, 0xBB, 0xCC })),
            transport);

        await sender.SendAsync(new FakeMessage(1));

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, transport.LastFrame);
        Assert.Equal(1, transport.SendCount);
    }

    /// <summary>
    /// 취소 토큰을 전달하면 transport 전송에도 그대로 전달되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_WithCancellationToken_PassesTokenToTransport()
    {
        var transport = new FakeTransport();
        var sender = new TransportMessageSender(
            new MessageFrameEncoder(new FakeSerializer(new byte[] { 0x01 }), new FakeProtocol(new byte[] { 0x02 })),
            transport);
        using var cts = new CancellationTokenSource();

        await sender.SendAsync(new FakeMessage(2), cts.Token);

        Assert.Equal(cts.Token, transport.LastCancellationToken);
    }

    private sealed record FakeMessage(ushort MessageId) : IMessage;

    private sealed class FakeSerializer : ISerializer
    {
        private readonly byte[] _payload;

        public FakeSerializer(byte[] payload)
        {
            _payload = payload;
        }

        public byte[] Serialize(IMessage message) => _payload;

        public IMessage Deserialize(ReadOnlySpan<byte> payload)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeProtocol : IProtocol
    {
        private readonly byte[] _frame;

        public FakeProtocol(byte[] frame)
        {
            _frame = frame;
        }

        public string Name => "Fake";

        public byte[] Encode(ReadOnlySpan<byte> payload) => _frame;

        public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
        {
            payload = Array.Empty<byte>();
            bytesConsumed = 0;
            return false;
        }
    }

    private sealed class FakeTransport : ITransport
    {
        public string Name => "FakeTransport";

        public byte[]? LastFrame { get; private set; }

        public int SendCount { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
        {
            LastFrame = frame.ToArray();
            LastCancellationToken = cancellationToken;
            SendCount++;
            return Task.CompletedTask;
        }
    }
}
