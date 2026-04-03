using System.Net;
using System.Net.Sockets;
using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

public sealed class LocalMockEndpointService : ILocalMockEndpointService
{
    // mock peer는 "동시에 하나만" 살아 있게 두면 UI 상태와 실제 바인딩 포트가 가장 단순해진다.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _runtimeCts;
    private Task? _runtimeTask;
    private TcpListener? _tcpListener;
    private UdpClient? _udpServer;
    private UdpClient? _multicastReceiver;
    private UdpClient? _multicastReplySender;

    public async Task<LocalMockEndpointBinding> StartAsync(
        LocalMockEndpointRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // transport를 바꿔가며 반복 테스트할 수 있게
            // 새 mock 시작 전에는 항상 이전 runtime을 완전히 내려 버린다.
            await StopCoreAsync().ConfigureAwait(false);

            return request.TransportKind switch
            {
                TransportKind.Tcp => StartTcp(request.Port),
                TransportKind.Udp => StartUdp(request.Port),
                TransportKind.Multicast => StartMulticast(request),
                TransportKind.Serial => throw new NotSupportedException(
                    "In-app serial mock peers are not supported. Use a paired COM port or hardware loopback."),
                _ => throw new InvalidOperationException("Unsupported mock transport selection.")
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await StopCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private LocalMockEndpointBinding StartTcp(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        var runtimeCts = new CancellationTokenSource();
        _tcpListener = listener;
        _runtimeCts = runtimeCts;
        _runtimeTask = Task.Run(() => RunTcpEchoLoopAsync(listener, runtimeCts.Token));

        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        return new LocalMockEndpointBinding(TransportKind.Tcp, IPAddress.Loopback.ToString(), endpoint.Port);
    }

    private LocalMockEndpointBinding StartUdp(int port)
    {
        var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        var runtimeCts = new CancellationTokenSource();

        _udpServer = server;
        _runtimeCts = runtimeCts;
        _runtimeTask = Task.Run(() => RunUdpEchoLoopAsync(server, runtimeCts.Token));

        var endpoint = (IPEndPoint)server.Client.LocalEndPoint!;
        return new LocalMockEndpointBinding(TransportKind.Udp, IPAddress.Loopback.ToString(), endpoint.Port);
    }

    private LocalMockEndpointBinding StartMulticast(LocalMockEndpointRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            throw new InvalidOperationException("Multicast group address is required.");
        }

        var groupAddress = IPAddress.Parse(request.Address.Trim());
        var receiver = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false
        };

        try
        {
            // multicast는 다른 프로세스와 포트를 공유할 수 있어야 해서 ReuseAddress를 켠다.
            receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            receiver.Client.Bind(new IPEndPoint(IPAddress.Any, request.Port));

            if (TryGetLocalInterface(request.LocalInterface, out var localInterface))
            {
                receiver.JoinMulticastGroup(groupAddress, localInterface);
            }
            else
            {
                receiver.JoinMulticastGroup(groupAddress);
            }

            // 수신 소켓과 응답 소켓을 분리해 둬야
            // 같은 포트에서 자기 자신이 보낸 응답을 다시 주워오는 루프를 완화하기 쉽다.
            var replySender = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            var runtimeCts = new CancellationTokenSource();

            _multicastReceiver = receiver;
            _multicastReplySender = replySender;
            _runtimeCts = runtimeCts;
            _runtimeTask = Task.Run(() => RunMulticastEchoLoopAsync(receiver, replySender, runtimeCts.Token));

            return new LocalMockEndpointBinding(TransportKind.Multicast, request.Address.Trim(), request.Port);
        }
        catch
        {
            receiver.Dispose();
            throw;
        }
    }

    private async Task StopCoreAsync()
    {
        // 필드를 먼저 지역 변수로 옮겨 놓고 null 처리하면
        // 중간에 다른 코드가 "아직 살아 있는 runtime"으로 착각하는 시간을 최소화할 수 있다.
        var runtimeTask = _runtimeTask;
        var runtimeCts = _runtimeCts;
        var tcpListener = _tcpListener;
        var udpServer = _udpServer;
        var multicastReceiver = _multicastReceiver;
        var multicastReplySender = _multicastReplySender;

        _runtimeTask = null;
        _runtimeCts = null;
        _tcpListener = null;
        _udpServer = null;
        _multicastReceiver = null;
        _multicastReplySender = null;

        if (runtimeCts is not null)
        {
            runtimeCts.Cancel();
        }

        tcpListener?.Stop();
        udpServer?.Dispose();
        multicastReceiver?.Dispose();
        multicastReplySender?.Dispose();

        if (runtimeTask is not null)
        {
            try
            {
                await runtimeTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException)
            {
            }
        }

        runtimeCts?.Dispose();
    }

    private static async Task RunTcpEchoLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;

            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Device Lab 예제 목적상 동시 다중 클라이언트 처리보다
            // "하나의 로컬 peer를 안정적으로 echo"하는 단순성이 더 중요해서 직렬 처리로 둔다.
            await HandleTcpClientAsync(client, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task HandleTcpClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var stream = client.GetStream())
        {
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;

                try
                {
                    bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (bytesRead == 0)
                {
                    return;
                }

                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task RunUdpEchoLoopAsync(UdpClient server, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult inbound;

            try
            {
                inbound = await server.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await server.SendAsync(inbound.Buffer, inbound.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task RunMulticastEchoLoopAsync(
        UdpClient receiver,
        UdpClient replySender,
        CancellationToken cancellationToken)
    {
        var replyPort = ((IPEndPoint)replySender.Client.LocalEndPoint!).Port;

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult inbound;

            try
            {
                inbound = await receiver.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // reply sender가 만든 패킷이 다시 receiver로 들어오면 self-echo가 한 번 더 증폭될 수 있다.
            // 동일 포트에서 돌아온 패킷은 우리가 만든 응답으로 보고 무시한다.
            if (inbound.RemoteEndPoint.Port == replyPort)
            {
                continue;
            }

            await replySender.SendAsync(inbound.Buffer, inbound.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool TryGetLocalInterface(string? value, out IPAddress localInterface)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            localInterface = IPAddress.Parse(value.Trim());
            return true;
        }

        localInterface = IPAddress.None;
        return false;
    }
}
