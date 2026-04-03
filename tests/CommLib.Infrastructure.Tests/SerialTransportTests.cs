using System.Diagnostics;
using System.Threading.Channels;
using CommLib.Domain.Configuration;
using CommLib.Infrastructure.Transport;
using Xunit;

namespace CommLib.Infrastructure.Tests;

/// <summary>
/// 직렬 포트 전송 구현의 기본 수명주기와 송수신 동작을 검증합니다.
/// </summary>
public sealed class SerialTransportTests
{
    /// <summary>
    /// open 시 직렬 포트 어댑터를 열고 열린 상태로 전환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_OpensSerialPortAdapter()
    {
        var adapter = new FakeSerialPortAdapter();
        var transport = CreateTransport(adapter);

        await transport.OpenAsync();

        Assert.True(adapter.IsOpen);
        Assert.Equal(1, adapter.OpenCount);
    }

    /// <summary>
    /// send 시 직렬 포트 stream으로 프레임 바이트를 기록하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_SendAsync_WritesBytesToSerialStream()
    {
        var adapter = new FakeSerialPortAdapter();
        var transport = CreateTransport(adapter);

        await transport.OpenAsync();
        await transport.SendAsync(new byte[] { 0x01, 0x02, 0x03 });

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, adapter.StreamImpl.LastWritten);
        Assert.Equal(1, adapter.StreamImpl.WriteCount);
    }

    /// <summary>
    /// receive 시 직렬 포트 stream의 다음 바이트 청크를 그대로 반환하는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task OpenAsync_ReceiveAsync_ReturnsBytesFromSerialStream()
    {
        var adapter = new FakeSerialPortAdapter();
        var transport = CreateTransport(adapter);

        await transport.OpenAsync();
        adapter.StreamImpl.EnqueueInbound(new byte[] { 0x0A, 0x0B, 0x0C });
        var frame = await transport.ReceiveAsync();

        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, frame.ToArray());
        Assert.Equal(1, adapter.StreamImpl.ReadCount);
    }

    /// <summary>
    /// half duplex가 활성화되면 전송 완료 전에 turn gap을 기다리는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task SendAsync_HalfDuplex_WaitsConfiguredTurnGap()
    {
        var adapter = new FakeSerialPortAdapter();
        var transport = CreateTransport(adapter, halfDuplex: true, turnGapMs: 40);
        var stopwatch = Stopwatch.StartNew();

        await transport.OpenAsync();
        await transport.SendAsync(new byte[] { 0x01 });
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds >= 30);
    }

    /// <summary>
    /// 대기 중인 receive가 close와 함께 취소되는지 확인합니다.
    /// </summary>
    [Fact]
    public async Task CloseAsync_PendingReceive_IsCanceled()
    {
        var adapter = new FakeSerialPortAdapter();
        var transport = CreateTransport(adapter);

        await transport.OpenAsync();
        var pendingReceive = transport.ReceiveAsync();

        await transport.CloseAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pendingReceive);
    }

    private static SerialTransport CreateTransport(
        FakeSerialPortAdapter adapter,
        bool halfDuplex = false,
        int turnGapMs = 0)
    {
        return new SerialTransport(
            new SerialTransportOptions
            {
                Type = "Serial",
                PortName = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = "None",
                StopBits = "One",
                HalfDuplex = halfDuplex,
                TurnGapMs = turnGapMs,
                ReadBufferSize = 1024,
                WriteBufferSize = 1024
            },
            _ => adapter);
    }

    private sealed class FakeSerialPortAdapter : ISerialPortAdapter
    {
        public FakeSerialStream StreamImpl { get; } = new();

        public bool IsOpen { get; private set; }

        public int OpenCount { get; private set; }

        public Stream Stream => StreamImpl;

        public void Open()
        {
            IsOpen = true;
            OpenCount++;
        }

        public void Close()
        {
            IsOpen = false;
            StreamImpl.SignalClosed();
        }

        public void Dispose()
        {
            IsOpen = false;
            StreamImpl.Dispose();
        }
    }

    private sealed class FakeSerialStream : Stream
    {
        private readonly Channel<byte[]> _inbound = Channel.CreateUnbounded<byte[]>();
        private readonly CancellationTokenSource _closeTokenSource = new();

        public byte[]? LastWritten { get; private set; }

        public int WriteCount { get; private set; }

        public int ReadCount { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public void EnqueueInbound(byte[] frame)
        {
            _inbound.Writer.TryWrite(frame);
        }

        public void SignalClosed()
        {
            if (_closeTokenSource.IsCancellationRequested)
            {
                return;
            }

            _closeTokenSource.Cancel();
            _inbound.Writer.TryComplete();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastWritten = buffer.ToArray();
            WriteCount++;
            return ValueTask.CompletedTask;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);
            var inbound = await _inbound.Reader.ReadAsync(linkedTokenSource.Token).ConfigureAwait(false);
            inbound.AsMemory(0, Math.Min(inbound.Length, buffer.Length)).CopyTo(buffer);
            ReadCount++;
            return Math.Min(inbound.Length, buffer.Length);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SignalClosed();
                _closeTokenSource.Dispose();
            }

            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
