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
    /// <summary>
    /// OpenAsync_OpensSerialPortAdapter 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// OpenAsync_SendAsync_WritesBytesToSerialStream 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// OpenAsync_ReceiveAsync_ReturnsBytesFromSerialStream 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// SendAsync_HalfDuplex_WaitsConfiguredTurnGap 작업을 수행합니다.
    /// </summary>
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
    /// <summary>
    /// CloseAsync_PendingReceive_IsCanceled 작업을 수행합니다.
    /// </summary>
    public async Task CloseAsync_PendingReceive_IsCanceled()
    {
        var adapter = new FakeSerialPortAdapter();
        var transport = CreateTransport(adapter);

        await transport.OpenAsync();
        var pendingReceive = transport.ReceiveAsync();

        await transport.CloseAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await pendingReceive);
    }

    /// <summary>
    /// CreateTransport 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// ISerialPortAdapter 값을 가져오거나 설정합니다.
    /// </summary>
    private sealed class FakeSerialPortAdapter : ISerialPortAdapter
    {
        /// <summary>
        /// StreamImpl 값을 가져오거나 설정합니다.
        /// </summary>
        public FakeSerialStream StreamImpl { get; } = new();

        /// <summary>
        /// IsOpen 값을 가져오거나 설정합니다.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// OpenCount 값을 가져오거나 설정합니다.
        /// </summary>
        public int OpenCount { get; private set; }

        /// <summary>
        /// Stream 값을 가져옵니다.
        /// </summary>
        public Stream Stream => StreamImpl;

        /// <summary>
        /// Open 작업을 수행합니다.
        /// </summary>
        public void Open()
        {
            IsOpen = true;
            OpenCount++;
        }

        /// <summary>
        /// Close 작업을 수행합니다.
        /// </summary>
        public void Close()
        {
            IsOpen = false;
            StreamImpl.SignalClosed();
        }

        /// <summary>
        /// Dispose 작업을 수행합니다.
        /// </summary>
        public void Dispose()
        {
            IsOpen = false;
            StreamImpl.Dispose();
        }
    }

    /// <summary>
    /// Stream 값을 가져오거나 설정합니다.
    /// </summary>
    private sealed class FakeSerialStream : Stream
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
        /// LastWritten 값을 가져오거나 설정합니다.
        /// </summary>
        public byte[]? LastWritten { get; private set; }

        /// <summary>
        /// WriteCount 값을 가져오거나 설정합니다.
        /// </summary>
        public int WriteCount { get; private set; }

        /// <summary>
        /// ReadCount 값을 가져오거나 설정합니다.
        /// </summary>
        public int ReadCount { get; private set; }

        /// <summary>
        /// CanRead 값을 가져옵니다.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// CanSeek 값을 가져옵니다.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// CanWrite 값을 가져옵니다.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Length 값을 가져옵니다.
        /// </summary>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// Position 값을 가져옵니다.
        /// </summary>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// EnqueueInbound 작업을 수행합니다.
        /// </summary>
        public void EnqueueInbound(byte[] frame)
        {
            _inbound.Writer.TryWrite(frame);
        }

        /// <summary>
        /// SignalClosed 작업을 수행합니다.
        /// </summary>
        public void SignalClosed()
        {
            if (_closeTokenSource.IsCancellationRequested)
            {
                return;
            }

            _closeTokenSource.Cancel();
            _inbound.Writer.TryComplete();
        }

        /// <summary>
        /// WriteAsync 작업을 수행합니다.
        /// </summary>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastWritten = buffer.ToArray();
            WriteCount++;
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// FlushAsync 작업을 수행합니다.
        /// </summary>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <summary>
        /// ReadAsync 작업을 수행합니다.
        /// </summary>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);
            var inbound = await _inbound.Reader.ReadAsync(linkedTokenSource.Token).ConfigureAwait(false);
            inbound.AsMemory(0, Math.Min(inbound.Length, buffer.Length)).CopyTo(buffer);
            ReadCount++;
            return Math.Min(inbound.Length, buffer.Length);
        }

        /// <summary>
        /// Dispose 작업을 수행합니다.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SignalClosed();
                _closeTokenSource.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Read 작업을 수행합니다.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// Write 작업을 수행합니다.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>
        /// Flush 작업을 수행합니다.
        /// </summary>
        public override void Flush() => throw new NotSupportedException();

        /// <summary>
        /// Seek 작업을 수행합니다.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>
        /// SetLength 작업을 수행합니다.
        /// </summary>
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
