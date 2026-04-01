using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// TCP 클라이언트 소켓을 사용해 바이트 청크를 송수신하는 전송 구현입니다.
/// </summary>
public sealed class TcpTransport : ITransport
{
    private readonly TcpClientTransportOptions _options;
    private readonly CancellationTokenSource _closeTokenSource = new();
    private TcpClient? _client;
    private NetworkStream? _stream;

    /// <summary>
    /// <see cref="TcpTransport"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">TCP 연결 설정입니다.</param>
    public TcpTransport(TcpClientTransportOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// transport 이름을 가져옵니다.
    /// </summary>
    public string Name => "TcpTransport";

    /// <summary>
    /// 현재 transport가 열린 상태인지 나타냅니다.
    /// </summary>
    public bool IsOpen => _stream is not null && !IsClosed;

    /// <summary>
    /// transport가 닫힌 상태인지 나타냅니다.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// TCP 연결과 스트림을 엽니다.
    /// </summary>
    /// <param name="cancellationToken">열기 취소 토큰입니다.</param>
    /// <returns>열기 작업입니다.</returns>
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfClosed();
        if (IsOpen)
        {
            return;
        }

        var client = new TcpClient
        {
            ReceiveBufferSize = _options.BufferSize,
            SendBufferSize = _options.BufferSize,
            NoDelay = _options.NoDelay
        };

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);
        linkedTokenSource.CancelAfter(_options.ConnectTimeoutMs);

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, linkedTokenSource.Token).ConfigureAwait(false);
            _client = client;
            _stream = client.GetStream();
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 지정한 바이트를 TCP 스트림에 기록합니다.
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
        }
        catch (Exception exception) when (IsCloseCancellation(exception, cancellationToken))
        {
            throw CreateClosedCancellationException();
        }
    }

    /// <summary>
    /// TCP 스트림에서 다음 바이트 청크를 읽습니다.
    /// </summary>
    /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
    /// <returns>수신한 바이트 청크입니다.</returns>
    public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var stream = GetRequiredStream();
        var buffer = new byte[_options.BufferSize];
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
    /// TCP 연결과 스트림을 닫습니다.
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
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        return Task.CompletedTask;
    }

    private NetworkStream GetRequiredStream()
    {
        ThrowIfClosed();
        return _stream ?? throw new InvalidOperationException($"Transport '{Name}' is not open.");
    }

    private void ThrowIfClosed()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException($"Transport '{Name}' is closed.");
        }
    }

    private bool IsCloseCancellation(Exception exception, CancellationToken cancellationToken)
    {
        return _closeTokenSource.IsCancellationRequested &&
               !cancellationToken.IsCancellationRequested &&
               exception is OperationCanceledException or ObjectDisposedException or IOException;
    }

    private OperationCanceledException CreateClosedCancellationException()
    {
        return new OperationCanceledException($"Transport '{Name}' was closed.", innerException: null, _closeTokenSource.Token);
    }
}
