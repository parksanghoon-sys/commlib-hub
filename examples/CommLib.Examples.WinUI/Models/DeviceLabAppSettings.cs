namespace CommLib.Examples.WinUI.Models;

public sealed class DeviceLabAppSettings
{
    public UiAppSettings Ui { get; set; } = new();

    public SessionAppSettings Session { get; set; } = new();

    public MessageComposerAppSettings MessageComposer { get; set; } = new();

    public TcpTransportAppSettings Tcp { get; set; } = new();

    public UdpTransportAppSettings Udp { get; set; } = new();

    public MulticastTransportAppSettings Multicast { get; set; } = new();

    public SerialTransportAppSettings Serial { get; set; } = new();
}

public sealed class UiAppSettings
{
    public AppLanguageMode LanguageMode { get; set; } = AppLanguageMode.English;
}

public sealed class SessionAppSettings
{
    public string DeviceId { get; set; } = "device-lab";

    public string DisplayName { get; set; } = "Device Lab";

    public string DefaultTimeoutMs { get; set; } = "3000";

    public string MaxPendingRequests { get; set; } = "8";

    public TransportKind SelectedTransport { get; set; } = TransportKind.Tcp;
}

public sealed class MessageComposerAppSettings
{
    public string OutboundMessageId { get; set; } = "100";

    public string OutboundBody { get; set; } = "hello from the mvvm winui lab";
}

public sealed class TcpTransportAppSettings
{
    public string Host { get; set; } = "127.0.0.1";

    public string Port { get; set; } = "7001";

    public string ConnectTimeoutMs { get; set; } = "1000";

    public string BufferSize { get; set; } = "1024";

    public bool NoDelay { get; set; } = true;
}

public sealed class UdpTransportAppSettings
{
    public string LocalPort { get; set; } = "0";

    public string RemoteHost { get; set; } = "127.0.0.1";

    public string RemotePort { get; set; } = "7002";
}

public sealed class MulticastTransportAppSettings
{
    public string GroupAddress { get; set; } = "239.0.0.241";

    public string Port { get; set; } = "7004";

    public string Ttl { get; set; } = "1";

    public string LocalInterface { get; set; } = string.Empty;

    public bool Loopback { get; set; } = true;
}

public sealed class SerialTransportAppSettings
{
    public string PortName { get; set; } = "COM3";

    public string BaudRate { get; set; } = "115200";

    public string DataBits { get; set; } = "8";

    public string Parity { get; set; } = "None";

    public string StopBits { get; set; } = "One";

    public string TurnGapMs { get; set; } = "0";

    public string ReadBufferSize { get; set; } = "1024";

    public string WriteBufferSize { get; set; } = "1024";

    public bool HalfDuplex { get; set; }
}
