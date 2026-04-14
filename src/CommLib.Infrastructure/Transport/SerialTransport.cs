using System.IO.Ports;
using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// 직렬 포트를 사용해 바이트 청크를 송수신하는 전송 구현입니다.
/// </summary>
public sealed class SerialTransport : ITransport
{
    /// <summary>
    /// _options 값을 나타냅니다.
    /// </summary>
    private readonly SerialTransportOptions _options;
    /// <summary>
    /// _serialPortFactory 값을 나타냅니다.
    /// </summary>
    private readonly Func<SerialTransportOptions, ISerialPortAdapter> _serialPortFactory;
    /// <summary>
    /// _closeTokenSource 값을 나타냅니다.
    /// </summary>
    private readonly CancellationTokenSource _closeTokenSource = new();
    /// <summary>
    /// _serialPort 값을 나타냅니다.
    /// </summary>
    private ISerialPortAdapter? _serialPort;
    /// <summary>
    /// _stream 값을 나타냅니다.
    /// </summary>
    private Stream? _stream;

    /// <summary>
    /// <see cref="SerialTransport"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">직렬 포트 연결 설정입니다.</param>
    public SerialTransport(SerialTransportOptions options)
        : this(options, static options => new SystemSerialPortAdapter(CreateSerialPort(options)))
    {
    }

    /// <summary>
    /// 테스트 가능한 직렬 포트 팩터리와 함께 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">직렬 포트 연결 설정입니다.</param>
    /// <param name="serialPortFactory">직렬 포트 어댑터 생성기입니다.</param>
    internal SerialTransport(
        SerialTransportOptions options,
        Func<SerialTransportOptions, ISerialPortAdapter> serialPortFactory)
    {
        _options = options;
        _serialPortFactory = serialPortFactory;
    }

    /// <summary>
    /// transport 이름을 가져옵니다.
    /// </summary>
    public string Name => "SerialTransport";

    /// <summary>
    /// 현재 transport가 열린 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsOpen => _serialPort is { IsOpen: true } && !IsClosed;

    /// <summary>
    /// transport가 닫힌 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// 직렬 포트를 열고 base stream을 준비합니다.
    /// </summary>
    /// <param name="cancellationToken">열기 취소 토큰입니다.</param>
    /// <returns>열기 작업입니다.</returns>
    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfClosed();
        if (IsOpen)
        {
            return Task.CompletedTask;
        }

        var serialPort = _serialPortFactory(_options);

        try
        {
            serialPort.Open();
            _serialPort = serialPort;
            _stream = serialPort.Stream;
            return Task.CompletedTask;
        }
        catch
        {
            serialPort.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 바이트 프레임을 직렬 포트로 전송합니다.
    /// </summary>
    /// <param name="frame">전송할 프레임 바이트입니다.</param>
    /// <param name="cancellationToken">전송 취소 토큰입니다.</param>
    /// <returns>전송 작업입니다.</returns>
    public async Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
    {
        var stream = GetRequiredStream();
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);

        try
        {
            await stream.WriteAsync(frame, linkedTokenSource.Token).ConfigureAwait(false);
            await stream.FlushAsync(linkedTokenSource.Token).ConfigureAwait(false);
            await ApplyTurnGapAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsCloseCancellation(exception, cancellationToken))
        {
            throw CreateClosedCancellationException();
        }
    }

    /// <summary>
    /// 직렬 포트에서 다음 바이트 청크를 수신합니다.
    /// </summary>
    /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
    /// <returns>수신한 바이트 청크입니다.</returns>
    public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var stream = GetRequiredStream();
        var buffer = new byte[_options.ReadBufferSize];
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);

        try
        {
            var bytesRead = await stream.ReadAsync(buffer, linkedTokenSource.Token).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException($"Transport '{Name}' remote endpoint closed the connection.");
            }

            return new ReadOnlyMemory<byte>(buffer, 0, bytesRead).ToArray();
        }
        catch (Exception exception) when (IsCloseCancellation(exception, cancellationToken))
        {
            throw CreateClosedCancellationException();
        }
    }

    /// <summary>
    /// 직렬 포트를 닫고 대기 중인 송수신을 중단합니다.
    /// </summary>
    /// <param name="cancellationToken">닫기 취소 토큰입니다.</param>
    /// <returns>닫기 작업입니다.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsClosed)
        {
            return Task.CompletedTask;
        }

        IsClosed = true;
        _closeTokenSource.Cancel();
        _serialPort?.Dispose();
        _stream = null;
        _serialPort = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// GetRequiredStream 작업을 수행합니다.
    /// </summary>
    private Stream GetRequiredStream()
    {
        ThrowIfClosed();
        return _stream ?? throw new InvalidOperationException($"Transport '{Name}' is not open.");
    }

    /// <summary>
    /// ApplyTurnGapAsync 작업을 수행합니다.
    /// </summary>
    private async Task ApplyTurnGapAsync(CancellationToken cancellationToken)
    {
        if (_options.HalfDuplex && _options.TurnGapMs > 0)
        {
            await Task.Delay(_options.TurnGapMs, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// ThrowIfClosed 작업을 수행합니다.
    /// </summary>
    private void ThrowIfClosed()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException($"Transport '{Name}' is closed.");
        }
    }

    /// <summary>
    /// IsCloseCancellation 작업을 수행합니다.
    /// </summary>
    private bool IsCloseCancellation(Exception exception, CancellationToken cancellationToken)
    {
        return _closeTokenSource.IsCancellationRequested &&
               !cancellationToken.IsCancellationRequested &&
               exception is OperationCanceledException or ObjectDisposedException or IOException;
    }

    /// <summary>
    /// CreateClosedCancellationException 작업을 수행합니다.
    /// </summary>
    private OperationCanceledException CreateClosedCancellationException()
    {
        return new OperationCanceledException($"Transport '{Name}' was closed.", innerException: null, _closeTokenSource.Token);
    }

    /// <summary>
    /// CreateSerialPort 작업을 수행합니다.
    /// </summary>
    private static SerialPort CreateSerialPort(SerialTransportOptions options)
    {
        return new SerialPort(
            options.PortName,
            options.BaudRate,
            ParseParity(options.Parity),
            options.DataBits,
            ParseStopBits(options.StopBits))
        {
            ReadBufferSize = options.ReadBufferSize,
            WriteBufferSize = options.WriteBufferSize,
            ReadTimeout = SerialPort.InfiniteTimeout,
            WriteTimeout = SerialPort.InfiniteTimeout
        };
    }

    /// <summary>
    /// ParseParity 작업을 수행합니다.
    /// </summary>
    private static Parity ParseParity(string value)
    {
        if (!Enum.TryParse<Parity>(value, ignoreCase: true, out var parity))
        {
            throw new InvalidOperationException($"Unsupported serial parity '{value}'.");
        }

        return parity;
    }

    /// <summary>
    /// ParseStopBits 작업을 수행합니다.
    /// </summary>
    private static StopBits ParseStopBits(string value)
    {
        if (!Enum.TryParse<StopBits>(value, ignoreCase: true, out var stopBits))
        {
            throw new InvalidOperationException($"Unsupported serial stop bits '{value}'.");
        }

        return stopBits;
    }
}

/// <summary>
/// ISerialPortAdapter 계약을 정의하는 인터페이스입니다.
/// </summary>
internal interface ISerialPortAdapter : IDisposable
{
    /// <summary>
    /// 포트가 열린 상태인지 여부를 가져옵니다.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// 직렬 포트의 기본 스트림을 가져옵니다.
    /// </summary>
    Stream Stream { get; }

    /// <summary>
    /// 직렬 포트를 엽니다.
    /// </summary>
    void Open();

    /// <summary>
    /// 직렬 포트를 닫습니다.
    /// </summary>
    void Close();
}

/// <summary>
/// SystemSerialPortAdapter 타입입니다.
/// </summary>
internal sealed class SystemSerialPortAdapter : ISerialPortAdapter
{
    /// <summary>
    /// _serialPort 값을 나타냅니다.
    /// </summary>
    private readonly SerialPort _serialPort;

    /// <summary>
    /// <see cref="SystemSerialPortAdapter"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public SystemSerialPortAdapter(SerialPort serialPort)
    {
        _serialPort = serialPort;
    }

    /// <summary>
    /// IsOpen 값을 가져옵니다.
    /// </summary>
    public bool IsOpen => _serialPort.IsOpen;

    /// <summary>
    /// Stream 값을 가져옵니다.
    /// </summary>
    public Stream Stream => _serialPort.BaseStream;

    /// <summary>
    /// Open 작업을 수행합니다.
    /// </summary>
    public void Open()
    {
        _serialPort.Open();
    }

    /// <summary>
    /// Close 작업을 수행합니다.
    /// </summary>
    public void Close()
    {
        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }
    }

    /// <summary>
    /// Dispose 작업을 수행합니다.
    /// </summary>
    public void Dispose()
    {
        _serialPort.Dispose();
    }
}
