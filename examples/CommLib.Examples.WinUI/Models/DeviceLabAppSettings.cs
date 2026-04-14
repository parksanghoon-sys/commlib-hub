using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;

namespace CommLib.Examples.WinUI.Models;

/// <summary>
/// DeviceLabAppSettings 타입입니다.
/// </summary>
public sealed class DeviceLabAppSettings
{
    /// <summary>
    /// Ui 값을 가져오거나 설정합니다.
    /// </summary>
    public UiAppSettings Ui { get; set; } = new();

    /// <summary>
    /// Session 값을 가져오거나 설정합니다.
    /// </summary>
    public SessionAppSettings Session { get; set; } = new();

    /// <summary>
    /// MessageComposer 값을 가져오거나 설정합니다.
    /// </summary>
    public MessageComposerAppSettings MessageComposer { get; set; } = new();

    /// <summary>
    /// Tcp 값을 가져오거나 설정합니다.
    /// </summary>
    public TcpTransportAppSettings Tcp { get; set; } = new();

    /// <summary>
    /// Udp 값을 가져오거나 설정합니다.
    /// </summary>
    public UdpTransportAppSettings Udp { get; set; } = new();

    /// <summary>
    /// Multicast 값을 가져오거나 설정합니다.
    /// </summary>
    public MulticastTransportAppSettings Multicast { get; set; } = new();

    /// <summary>
    /// Serial 값을 가져오거나 설정합니다.
    /// </summary>
    public SerialTransportAppSettings Serial { get; set; } = new();
}

/// <summary>
/// UiAppSettings 타입입니다.
/// </summary>
public sealed class UiAppSettings
{
    /// <summary>
    /// LanguageMode 값을 가져오거나 설정합니다.
    /// </summary>
    public AppLanguageMode LanguageMode { get; set; } = AppLanguageMode.English;
}

/// <summary>
/// SessionAppSettings 타입입니다.
/// </summary>
public sealed class SessionAppSettings
{
    /// <summary>
    /// DeviceId 값을 가져오거나 설정합니다.
    /// </summary>
    public string DeviceId { get; set; } = "device-lab";

    /// <summary>
    /// DisplayName 값을 가져오거나 설정합니다.
    /// </summary>
    public string DisplayName { get; set; } = "Device Lab";

    /// <summary>
    /// DefaultTimeoutMs 값을 가져오거나 설정합니다.
    /// </summary>
    public string DefaultTimeoutMs { get; set; } = "3000";

    /// <summary>
    /// MaxPendingRequests 값을 가져오거나 설정합니다.
    /// </summary>
    public string MaxPendingRequests { get; set; } = "8";

    /// <summary>
    /// SelectedTransport 값을 가져오거나 설정합니다.
    /// </summary>
    public TransportKind SelectedTransport { get; set; } = TransportKind.Tcp;
}

/// <summary>
/// MessageComposerAppSettings 타입입니다.
/// </summary>
public sealed class MessageComposerAppSettings
{
    /// <summary>
    /// SerializerType 값을 가져오거나 설정합니다.
    /// </summary>
    public string SerializerType { get; set; } = SerializerTypes.AutoBinary;

    /// <summary>
    /// BitFieldSchema 값을 가져오거나 설정합니다.
    /// </summary>
    public BitFieldPayloadSchema? BitFieldSchema { get; set; }

    /// <summary>
    /// OutboundMessageId 값을 가져오거나 설정합니다.
    /// </summary>
    public string OutboundMessageId { get; set; } = "100";

    /// <summary>
    /// OutboundBody 값을 가져오거나 설정합니다.
    /// </summary>
    public string OutboundBody { get; set; } = "hello from the mvvm winui lab";
}

/// <summary>
/// TcpTransportAppSettings 타입입니다.
/// </summary>
public sealed class TcpTransportAppSettings
{
    /// <summary>
    /// Host 값을 가져오거나 설정합니다.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Port 값을 가져오거나 설정합니다.
    /// </summary>
    public string Port { get; set; } = "7001";

    /// <summary>
    /// ConnectTimeoutMs 값을 가져오거나 설정합니다.
    /// </summary>
    public string ConnectTimeoutMs { get; set; } = "1000";

    /// <summary>
    /// BufferSize 값을 가져오거나 설정합니다.
    /// </summary>
    public string BufferSize { get; set; } = "1024";

    /// <summary>
    /// NoDelay 값을 가져오거나 설정합니다.
    /// </summary>
    public bool NoDelay { get; set; } = true;
}

/// <summary>
/// UdpTransportAppSettings 타입입니다.
/// </summary>
public sealed class UdpTransportAppSettings
{
    /// <summary>
    /// LocalPort 값을 가져오거나 설정합니다.
    /// </summary>
    public string LocalPort { get; set; } = "0";

    /// <summary>
    /// RemoteHost 값을 가져오거나 설정합니다.
    /// </summary>
    public string RemoteHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// RemotePort 값을 가져오거나 설정합니다.
    /// </summary>
    public string RemotePort { get; set; } = "7002";
}

/// <summary>
/// MulticastTransportAppSettings 타입입니다.
/// </summary>
public sealed class MulticastTransportAppSettings
{
    /// <summary>
    /// GroupAddress 값을 가져오거나 설정합니다.
    /// </summary>
    public string GroupAddress { get; set; } = "239.0.0.241";

    /// <summary>
    /// Port 값을 가져오거나 설정합니다.
    /// </summary>
    public string Port { get; set; } = "7004";

    /// <summary>
    /// Ttl 값을 가져오거나 설정합니다.
    /// </summary>
    public string Ttl { get; set; } = "1";

    /// <summary>
    /// LocalInterface 값을 가져오거나 설정합니다.
    /// </summary>
    public string LocalInterface { get; set; } = string.Empty;

    /// <summary>
    /// Loopback 값을 가져오거나 설정합니다.
    /// </summary>
    public bool Loopback { get; set; } = true;
}

/// <summary>
/// SerialTransportAppSettings 타입입니다.
/// </summary>
public sealed class SerialTransportAppSettings
{
    /// <summary>
    /// PortName 값을 가져오거나 설정합니다.
    /// </summary>
    public string PortName { get; set; } = "COM3";

    /// <summary>
    /// BaudRate 값을 가져오거나 설정합니다.
    /// </summary>
    public string BaudRate { get; set; } = "115200";

    /// <summary>
    /// DataBits 값을 가져오거나 설정합니다.
    /// </summary>
    public string DataBits { get; set; } = "8";

    /// <summary>
    /// Parity 값을 가져오거나 설정합니다.
    /// </summary>
    public string Parity { get; set; } = "None";

    /// <summary>
    /// StopBits 값을 가져오거나 설정합니다.
    /// </summary>
    public string StopBits { get; set; } = "One";

    /// <summary>
    /// TurnGapMs 값을 가져오거나 설정합니다.
    /// </summary>
    public string TurnGapMs { get; set; } = "0";

    /// <summary>
    /// ReadBufferSize 값을 가져오거나 설정합니다.
    /// </summary>
    public string ReadBufferSize { get; set; } = "1024";

    /// <summary>
    /// WriteBufferSize 값을 가져오거나 설정합니다.
    /// </summary>
    public string WriteBufferSize { get; set; } = "1024";

    /// <summary>
    /// HalfDuplex 값을 가져오거나 설정합니다.
    /// </summary>
    public bool HalfDuplex { get; set; }
}