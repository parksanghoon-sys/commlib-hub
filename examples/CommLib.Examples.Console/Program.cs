using System.Buffers.Binary;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Infrastructure.Factories;
using CommLib.Infrastructure.Protocol;
using CommLib.Infrastructure.Sessions;
using CommLib.Infrastructure.Transport;

return await ExampleConsole.RunAsync(args);

internal static class ExampleConsole
{
    private const int DemoMaxFrameLength = 4096;
    private static readonly LengthPrefixedProtocol Protocol = new(DemoMaxFrameLength);
    private static readonly NoOpSerializer Serializer = new();

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelpCommand(args[0]))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "tcp-demo" => await RunTcpDemoAsync(args[1..]).ConfigureAwait(false),
                "tcp-echo-server" => await RunTcpEchoServerOnlyAsync(args[1..]).ConfigureAwait(false),
                "udp-demo" => await RunUdpDemoAsync(args[1..]).ConfigureAwait(false),
                "udp-echo-server" => await RunUdpEchoServerOnlyAsync(args[1..]).ConfigureAwait(false),
                "multicast-send" => await RunMulticastSendAsync(args[1..]).ConfigureAwait(false),
                "multicast-receive" => await RunMulticastReceiveAsync(args[1..]).ConfigureAwait(false),
                "serial-demo" => await RunSerialDemoAsync(args[1..]).ConfigureAwait(false),
                _ => ThrowUnknownCommand(args[0])
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[error] {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> RunTcpDemoAsync(string[] args)
    {
        var port = GetIntOption(args, "--port") ?? GetFreeTcpPort();
        var messageText = GetStringOption(args, "--message") ?? "hello from tcp demo";
        var outboundMessage = new MessageModel(100, messageText);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        var actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;

        Console.WriteLine($"[tcp] echo server listening on 127.0.0.1:{actualPort}");

        var serverTask = RunTcpEchoServerAsync(listener, cts.Token);
        await using var manager = CreateConnectionManager();
        var profile = CreateProfile(
            "tcp-demo",
            new TcpClientTransportOptions
            {
                Type = "TcpClient",
                Host = IPAddress.Loopback.ToString(),
                Port = actualPort,
                ConnectTimeoutMs = 1000,
                BufferSize = 1024,
                NoDelay = true
            });

        await SendAndReceiveAsync(manager, profile, outboundMessage, cts.Token).ConfigureAwait(false);
        await serverTask.ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> RunTcpEchoServerOnlyAsync(string[] args)
    {
        var port = GetIntOption(args, "--port") ?? 7001;
        var timeoutMs = GetIntOption(args, "--timeout-ms");
        using var lifetime = CreatePeerLifetime(timeoutMs);
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        var actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;

        Console.WriteLine($"[tcp-echo-server] listening on 127.0.0.1:{actualPort}");
        if (timeoutMs is > 0)
        {
            Console.WriteLine($"[tcp-echo-server] will stop after {timeoutMs} ms if no one cancels earlier");
        }
        else
        {
            Console.WriteLine("[tcp-echo-server] press Ctrl+C to stop");
        }

        while (!lifetime.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync(lifetime.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                break;
            }

            using (client)
            using (var stream = client.GetStream())
            {
                Console.WriteLine($"[tcp-echo-server] client connected from {client.Client.RemoteEndPoint}");

                while (!lifetime.IsCancellationRequested)
                {
                    byte[] frame;
                    try
                    {
                        frame = await ReadLengthPrefixedFrameAsync(stream, lifetime.Token).ConfigureAwait(false);
                    }
                    catch (EndOfStreamException)
                    {
                        Console.WriteLine("[tcp-echo-server] client disconnected");
                        break;
                    }
                    catch (IOException) when (!lifetime.IsCancellationRequested)
                    {
                        Console.WriteLine("[tcp-echo-server] client disconnected");
                        break;
                    }
                    catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
                    {
                        break;
                    }

                    var message = DecodeFrame(frame);
                    Console.WriteLine($"[tcp-echo-server] recv {DescribeMessage(message)}");
                    await stream.WriteAsync(frame, lifetime.Token).ConfigureAwait(false);
                    await stream.FlushAsync(lifetime.Token).ConfigureAwait(false);
                    Console.WriteLine("[tcp-echo-server] echoed frame");
                }
            }
        }

        return 0;
    }

    private static async Task<int> RunUdpDemoAsync(string[] args)
    {
        var serverPort = GetIntOption(args, "--server-port") ?? GetFreeUdpPort();
        var clientPort = GetIntOption(args, "--client-port") ?? 0;
        var messageText = GetStringOption(args, "--message") ?? "hello from udp demo";
        var outboundMessage = new MessageModel(200, messageText);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, serverPort));

        Console.WriteLine($"[udp] echo server listening on 127.0.0.1:{serverPort}");

        var serverTask = RunUdpEchoServerAsync(server, cts.Token);
        await using var manager = CreateConnectionManager();
        var profile = CreateProfile(
            "udp-demo",
            new UdpTransportOptions
            {
                Type = "Udp",
                LocalPort = clientPort,
                RemoteHost = IPAddress.Loopback.ToString(),
                RemotePort = serverPort
            });

        await SendAndReceiveAsync(manager, profile, outboundMessage, cts.Token).ConfigureAwait(false);
        await serverTask.ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> RunUdpEchoServerOnlyAsync(string[] args)
    {
        var port = GetIntOption(args, "--port") ?? 7002;
        var timeoutMs = GetIntOption(args, "--timeout-ms");
        using var lifetime = CreatePeerLifetime(timeoutMs);
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));

        Console.WriteLine($"[udp-echo-server] listening on 127.0.0.1:{port}");
        if (timeoutMs is > 0)
        {
            Console.WriteLine($"[udp-echo-server] will stop after {timeoutMs} ms if no one cancels earlier");
        }
        else
        {
            Console.WriteLine("[udp-echo-server] press Ctrl+C to stop");
        }

        while (!lifetime.IsCancellationRequested)
        {
            UdpReceiveResult inbound;
            try
            {
                inbound = await server.ReceiveAsync(lifetime.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                break;
            }

            var message = DecodeFrame(inbound.Buffer);
            Console.WriteLine($"[udp-echo-server] recv {DescribeMessage(message)} from {inbound.RemoteEndPoint}");
            await server.SendAsync(inbound.Buffer, inbound.RemoteEndPoint, lifetime.Token).ConfigureAwait(false);
            Console.WriteLine("[udp-echo-server] echoed datagram");
        }

        return 0;
    }

    private static async Task<int> RunMulticastSendAsync(string[] args)
    {
        var groupAddressText = GetStringOption(args, "--group") ?? "239.0.0.241";
        var port = GetIntOption(args, "--port") ?? GetFreeUdpPort();
        var messageText = GetStringOption(args, "--message") ?? "hello from multicast demo";
        var outboundMessage = new MessageModel(300, messageText);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var transport = new MulticastTransport(new MulticastTransportOptions
        {
            Type = "Multicast",
            GroupAddress = groupAddressText,
            Port = port,
            Ttl = 1,
            Loopback = true
        });

        try
        {
            await transport.OpenAsync(cts.Token).ConfigureAwait(false);
            Console.WriteLine($"[multicast-send] group {groupAddressText}:{port}");
            Console.WriteLine($"[send] {DescribeMessage(outboundMessage)}");
            await transport.SendAsync(EncodeFrame(outboundMessage), cts.Token).ConfigureAwait(false);
        }
        finally
        {
            await transport.CloseAsync().ConfigureAwait(false);
        }

        return 0;
    }

    private static async Task<int> RunMulticastReceiveAsync(string[] args)
    {
        var groupAddressText = GetStringOption(args, "--group") ?? "239.0.0.241";
        var port = GetIntOption(args, "--port") ?? GetFreeUdpPort();
        var timeoutMs = GetIntOption(args, "--timeout-ms") ?? 10000;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        var transport = new MulticastTransport(new MulticastTransportOptions
        {
            Type = "Multicast",
            GroupAddress = groupAddressText,
            Port = port,
            Ttl = 1,
            Loopback = true
        });

        try
        {
            await transport.OpenAsync(cts.Token).ConfigureAwait(false);
            Console.WriteLine($"[multicast-receive] waiting on {groupAddressText}:{port}");
            var receivedFrame = await transport.ReceiveAsync(cts.Token).ConfigureAwait(false);
            Console.WriteLine($"[recv] {DescribeMessage(DecodeFrame(receivedFrame.Span))}");
        }
        finally
        {
            await transport.CloseAsync().ConfigureAwait(false);
        }

        return 0;
    }

    private static async Task<int> RunSerialDemoAsync(string[] args)
    {
        var port = GetRequiredStringOption(args, "--port");
        var peerPort = GetRequiredStringOption(args, "--peer-port");
        var baudRate = GetIntOption(args, "--baud") ?? 115200;
        var messageText = GetStringOption(args, "--message") ?? "hello from serial demo";
        var outboundMessage = new MessageModel(400, messageText);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        Console.WriteLine($"[serial] client port={port}, peer port={peerPort}, baud={baudRate}");
        Console.WriteLine("[serial] use a paired COM setup such as com0com or a hardware loopback pair before running this demo.");

        using var peer = CreateSerialPort(peerPort, baudRate);
        peer.Open();

        var peerTask = RunSerialEchoPeerAsync(peer, cts.Token);
        await using var manager = CreateConnectionManager();
        var profile = CreateProfile(
            "serial-demo",
            new SerialTransportOptions
            {
                Type = "Serial",
                PortName = port,
                BaudRate = baudRate,
                DataBits = 8,
                Parity = "None",
                StopBits = "One",
                HalfDuplex = false,
                TurnGapMs = 0,
                ReadBufferSize = 1024,
                WriteBufferSize = 1024
            });

        await SendAndReceiveAsync(manager, profile, outboundMessage, cts.Token).ConfigureAwait(false);
        await peerTask.ConfigureAwait(false);
        return 0;
    }

    private static async Task SendAndReceiveAsync(
        ConnectionManager manager,
        DeviceProfile profile,
        IMessage outboundMessage,
        CancellationToken cancellationToken)
    {
        await manager.ConnectAsync(profile, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[send] {DescribeMessage(outboundMessage)}");
        await manager.SendAsync(profile.DeviceId, outboundMessage, cancellationToken).ConfigureAwait(false);
        var inboundMessage = await manager.ReceiveAsync(profile.DeviceId, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[recv] {DescribeMessage(inboundMessage)}");
    }

    private static async Task RunTcpEchoServerAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        using var stream = client.GetStream();
        var frame = await ReadLengthPrefixedFrameAsync(stream, cancellationToken).ConfigureAwait(false);
        var message = DecodeFrame(frame);
        Console.WriteLine($"[server] {DescribeMessage(message)}");
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunUdpEchoServerAsync(UdpClient server, CancellationToken cancellationToken)
    {
        var inbound = await server.ReceiveAsync(cancellationToken).ConfigureAwait(false);
        var message = DecodeFrame(inbound.Buffer);
        Console.WriteLine($"[server] {DescribeMessage(message)}");
        await server.SendAsync(inbound.Buffer, inbound.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunSerialEchoPeerAsync(SerialPort peer, CancellationToken cancellationToken)
    {
        var stream = peer.BaseStream;
        var frame = await ReadLengthPrefixedFrameAsync(stream, cancellationToken).ConfigureAwait(false);
        var message = DecodeFrame(frame);
        Console.WriteLine($"[peer] {DescribeMessage(message)}");
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ConnectionManager CreateConnectionManager()
    {
        return new ConnectionManager(
            new TransportFactory(),
            new ProtocolFactory(),
            new SerializerFactory(),
            new ConsoleConnectionEventSink());
    }

    private static DeviceProfile CreateProfile(string deviceId, TransportOptions transport)
    {
        return new DeviceProfile
        {
            DeviceId = deviceId,
            DisplayName = deviceId,
            Transport = transport,
            Protocol = new ProtocolOptions
            {
                Type = "LengthPrefixed",
                MaxFrameLength = DemoMaxFrameLength
            },
            Serializer = new SerializerOptions
            {
                Type = "AutoBinary"
            },
            Reconnect = new ReconnectOptions
            {
                Type = "None",
                MaxAttempts = 0
            },
            RequestResponse = new RequestResponseOptions
            {
                DefaultTimeoutMs = 3000,
                MaxPendingRequests = 8
            }
        };
    }

    private static byte[] EncodeFrame(IMessage message)
    {
        return Protocol.Encode(Serializer.Serialize(message));
    }

    private static CancellationTokenSource CreatePeerLifetime(int? timeoutMs)
    {
        var lifetime = timeoutMs is > 0
            ? new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs.Value))
            : new CancellationTokenSource();

        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = (_, args) =>
        {
            args.Cancel = true;
            lifetime.Cancel();
            Console.CancelKeyPress -= cancelHandler;
        };

        Console.CancelKeyPress += cancelHandler;
        lifetime.Token.Register(() => Console.CancelKeyPress -= cancelHandler);
        return lifetime;
    }

    private static IMessage DecodeFrame(ReadOnlySpan<byte> frame)
    {
        if (!Protocol.TryDecode(frame, out var payload, out var bytesConsumed) || bytesConsumed != frame.Length)
        {
            throw new InvalidOperationException("Frame did not contain a complete length-prefixed payload.");
        }

        return Serializer.Deserialize(payload);
    }

    private static async Task<byte[]> ReadLengthPrefixedFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
        if (payloadLength < 0)
        {
            throw new InvalidOperationException("Frame length cannot be negative.");
        }

        var frameLength = header.Length + payloadLength;
        if (frameLength > DemoMaxFrameLength)
        {
            throw new InvalidOperationException(
                $"Frame length {frameLength} exceeds the configured maximum of {DemoMaxFrameLength}.");
        }

        var payload = new byte[payloadLength];
        await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);

        var frame = new byte[header.Length + payload.Length];
        header.CopyTo(frame, 0);
        payload.CopyTo(frame, header.Length);
        return frame;
    }

    private static SerialPort CreateSerialPort(string portName, int baudRate)
    {
        return new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = SerialPort.InfiniteTimeout,
            WriteTimeout = SerialPort.InfiniteTimeout
        };
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static int GetFreeUdpPort()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }

    private static string DescribeMessage(IMessage message)
    {
        var body = message is IMessageBody bodyMessage ? bodyMessage.Body : string.Empty;
        return message switch
        {
            IResponseMessage response => $"response id={message.MessageId}, correlation={response.CorrelationId}, success={response.IsSuccess}, body=\"{body}\"",
            IRequestMessage request => $"request id={message.MessageId}, correlation={request.CorrelationId}, body=\"{body}\"",
            _ => $"message id={message.MessageId}, body=\"{body}\""
        };
    }

    private static bool IsHelpCommand(string command)
    {
        return command is "help" or "--help" or "-h" or "/?";
    }

    private static string? GetStringOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string GetRequiredStringOption(IReadOnlyList<string> args, string name)
    {
        return GetStringOption(args, name) ?? throw new InvalidOperationException($"Missing required option: {name}");
    }

    private static int? GetIntOption(IReadOnlyList<string> args, string name)
    {
        var value = GetStringOption(args, name);
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"Option {name} must be an integer.");
        }

        return parsed;
    }

    private static int ThrowUnknownCommand(string command)
    {
        throw new InvalidOperationException($"Unknown command: {command}");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("CommLib example console");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  tcp-demo [--port 7001] [--message \"hello\"]");
        Console.WriteLine("  tcp-echo-server [--port 7001] [--timeout-ms 30000]");
        Console.WriteLine("  udp-demo [--server-port 7002] [--client-port 7003] [--message \"hello\"]");
        Console.WriteLine("  udp-echo-server [--port 7002] [--timeout-ms 30000]");
        Console.WriteLine("  multicast-receive [--group 239.0.0.241] [--port 7004] [--timeout-ms 10000]");
        Console.WriteLine("  multicast-send [--group 239.0.0.241] [--port 7004] [--message \"hello\"]");
        Console.WriteLine("  serial-demo --port COM3 --peer-port COM4 [--baud 115200] [--message \"hello\"]");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("  tcp-demo and udp-demo are self-contained loopback examples.");
        Console.WriteLine("  tcp-echo-server and udp-echo-server stay alive for external WinUI/manual validation.");
        Console.WriteLine("  multicast-send and multicast-receive are intended to be run in separate terminals.");
        Console.WriteLine("  serial-demo requires a paired serial environment such as com0com or a hardware loopback pair.");
    }

    private sealed class ConsoleConnectionEventSink : IConnectionEventSink
    {
        public void OnConnectAttempt(string deviceId, int attemptNumber, int totalAttempts)
        {
            Console.WriteLine($"[connect] attempt {attemptNumber}/{totalAttempts} for {deviceId}");
        }

        public void OnConnectRetryScheduled(string deviceId, int attemptNumber, TimeSpan delay, Exception exception)
        {
            Console.WriteLine($"[connect] retry scheduled for {deviceId} after {delay.TotalMilliseconds:0} ms (attempt {attemptNumber} failed: {exception.Message})");
        }

        public void OnConnectSucceeded(string deviceId, int attemptNumber)
        {
            Console.WriteLine($"[connect] {deviceId} connected on attempt {attemptNumber}");
        }

        public void OnOperationFailed(string deviceId, string operation, Exception exception)
        {
            Console.WriteLine($"[connect] {deviceId} {operation} failed: {exception.Message}");
        }
    }
}
