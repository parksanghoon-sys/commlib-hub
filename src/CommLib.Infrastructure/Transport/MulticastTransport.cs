using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Domain.Transport;

namespace CommLib.Infrastructure.Transport;

/// <summary>
/// UDP multicast 그룹을 사용해 datagram 송수신을 수행하는 전송 구현입니다.
/// </summary>
public sealed class MulticastTransport : ITransport
{
    /// <summary>
    /// _options 값을 나타냅니다.
    /// </summary>
    private readonly MulticastTransportOptions _options;
    /// <summary>
    /// _closeTokenSource 값을 나타냅니다.
    /// </summary>
    private readonly CancellationTokenSource _closeTokenSource = new();
    /// <summary>
    /// _groupAddress 값을 나타냅니다.
    /// </summary>
    private readonly IPAddress _groupAddress;
    /// <summary>
    /// _groupEndpoint 값을 나타냅니다.
    /// </summary>
    private readonly IPEndPoint _groupEndpoint;
    /// <summary>
    /// _client 값을 나타냅니다.
    /// </summary>
    private UdpClient? _client;

    /// <summary>
    /// <see cref="MulticastTransport"/> 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">멀티캐스트 연결 설정입니다.</param>
    public MulticastTransport(MulticastTransportOptions options)
    {
        _options = options;
        _groupAddress = IPAddress.Parse(options.GroupAddress);
        _groupEndpoint = new IPEndPoint(_groupAddress, options.Port);
    }

    /// <summary>
    /// transport 이름을 가져옵니다.
    /// </summary>
    public string Name => "MulticastTransport";

    /// <summary>
    /// 현재 transport가 열린 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsOpen => _client is not null && !IsClosed;

    /// <summary>
    /// transport가 닫힌 상태인지 여부를 반환합니다.
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// 멀티캐스트 그룹에 참여하고 송수신 소켓을 준비합니다.
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

        var client = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false
        };

        try
        {
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, _options.Port));
            client.MulticastLoopback = _options.Loopback;
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, _options.Ttl);

            if (TryGetLocalInterface(out var localInterface))
            {
                client.JoinMulticastGroup(_groupAddress, localInterface);
            }
            else
            {
                client.JoinMulticastGroup(_groupAddress);
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
    /// 멀티캐스트 그룹 endpoint로 datagram을 전송합니다.
    /// </summary>
    /// <param name="frame">전송할 datagram 바이트입니다.</param>
    /// <param name="cancellationToken">전송 취소 토큰입니다.</param>
    /// <returns>전송 작업입니다.</returns>
    public async Task SendAsync(ReadOnlyMemory<byte> frame, CancellationToken cancellationToken = default)
    {
        var client = GetRequiredClient();
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closeTokenSource.Token);

        try
        {
            await client.Client.SendToAsync(frame, SocketFlags.None, _groupEndpoint, linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsCloseCancellation(exception, cancellationToken))
        {
            throw CreateClosedCancellationException();
        }
    }

    /// <summary>
    /// 다음 inbound multicast datagram을 수신합니다.
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
    /// 멀티캐스트 그룹 참여를 해제하고 소켓을 닫습니다.
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

        if (_client is { } client)
        {
            try
            {
                client.DropMulticastGroup(_groupAddress);
            }
            catch
            {
            }

            client.Dispose();
            _client = null;
        }

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
    /// TryGetLocalInterface 작업을 수행합니다.
    /// </summary>
    private bool TryGetLocalInterface(out IPAddress localInterface)
    {
        if (!string.IsNullOrWhiteSpace(_options.LocalInterface))
        {
            localInterface = IPAddress.Parse(_options.LocalInterface);
            return true;
        }

        localInterface = IPAddress.None;
        return false;
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
