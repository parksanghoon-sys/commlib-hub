using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// UDP 소켓을 사용해 datagram 송수신을 수행하는 전송 구현입니다.
/// </summary>
public sealed class UdpTransport : ITransport
{
    /// <summary>
    /// _options 값을 나타냅니다.
    /// </summary>
    private readonly UdpTransportOptions _options;
    /// <summary>
    /// _closeTokenSource 값을 나타냅니다.
    /// </summary>
    private readonly CancellationTokenSource _closeTokenSource = new();
    /// <summary>
    /// _client 값을 나타냅니다.
    /// </summary>
    private UdpClient? _client;

    /// <summary>
    /// <see cref="UdpTransport"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">UDP 연결 설정입니다.</param>
    public UdpTransport(UdpTransportOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// transport 이름을 가져옵니다.
    /// </summary>
    public string Name => "UdpTransport";

    /// <summary>
    /// 현재 transport가 열린 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsOpen => _client is not null && !IsClosed;

    /// <summary>
    /// transport가 닫힌 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// UDP 소켓을 열고 필요하면 기본 원격 endpoint를 연결합니다.
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

        var client = new UdpClient(new IPEndPoint(IPAddress.Any, _options.LocalPort));

        try
        {
            if (HasConfiguredRemoteEndpoint())
            {
                client.Connect(_options.RemoteHost!, _options.RemotePort!.Value);
            }

            _client = client;
            return Task.CompletedTask;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 기본 원격 endpoint로 datagram을 전송합니다.
    /// </summary>
    /// <param name="frame">전송할 datagram 바이트입니다.</param>
    /// <param name="cancellationToken">전송 취소 토큰입니다.</param>
    /// <returns>전송 작업입니다.</returns>
    public async Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
    {
        var client = GetRequiredClient();
        if (!client.Client.Connected)
        {
            throw new InvalidOperationException($"Transport '{Name}' has no remote endpoint configured.");
        }

        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);

        try
        {
            await client.Client.SendAsync(frame, SocketFlags.None, linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsCloseCancellation(exception, cancellationToken))
        {
            throw CreateClosedCancellationException();
        }
    }

    /// <summary>
    /// 다음 inbound datagram을 수신합니다.
    /// </summary>
    /// <param name="cancellationToken">수신 취소 토큰입니다.</param>
    /// <returns>수신한 datagram 바이트입니다.</returns>
    public async Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var client = GetRequiredClient();
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);

        try
        {
            var result = await client.ReceiveAsync(linkedTokenSource.Token).ConfigureAwait(false);
            return result.Buffer;
        }
        catch (Exception exception) when (IsCloseCancellation(exception, cancellationToken))
        {
            throw CreateClosedCancellationException();
        }
    }

    /// <summary>
    /// UDP 소켓을 닫고 대기 중인 송수신을 중단합니다.
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
        _client?.Dispose();
        _client = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// GetRequiredClient 작업을 수행합니다.
    /// </summary>
    private UdpClient GetRequiredClient()
    {
        ThrowIfClosed();
        return _client ?? throw new InvalidOperationException($"Transport '{Name}' is not open.");
    }

    /// <summary>
    /// HasConfiguredRemoteEndpoint 작업을 수행합니다.
    /// </summary>
    private bool HasConfiguredRemoteEndpoint()
    {
        return !string.IsNullOrWhiteSpace(_options.RemoteHost) &&
               _options.RemotePort is int;
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
               exception is OperationCanceledException or ObjectDisposedException or SocketException;
    }

    /// <summary>
    /// CreateClosedCancellationException 작업을 수행합니다.
    /// </summary>
    private OperationCanceledException CreateClosedCancellationException()
    {
        return new OperationCanceledException($"Transport '{Name}' was closed.", innerException: null, _closeTokenSource.Token);
    }
}
