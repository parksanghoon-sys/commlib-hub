using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

/// <summary>
/// SerialTransportSettingsViewModel 타입입니다.
/// </summary>
public sealed class SerialTransportSettingsViewModel : TransportSettingsViewModel
{
    /// <summary>
    /// ParityOptions 값을 가져옵니다.
    /// </summary>
    public IReadOnlyList<string> ParityOptions { get; } = ["None", "Odd", "Even", "Mark", "Space"];

    /// <summary>
    /// StopBitsOptions 값을 가져옵니다.
    /// </summary>
    public IReadOnlyList<string> StopBitsOptions { get; } = ["None", "One", "Two", "OnePointFive"];

    /// <summary>
    /// _portName 값을 나타냅니다.
    /// </summary>
    private string _portName = "COM3";
    /// <summary>
    /// _baudRate 값을 나타냅니다.
    /// </summary>
    private string _baudRate = "115200";
    /// <summary>
    /// _dataBits 값을 나타냅니다.
    /// </summary>
    private string _dataBits = "8";
    /// <summary>
    /// _parity 값을 나타냅니다.
    /// </summary>
    private string _parity = "None";
    /// <summary>
    /// _stopBits 값을 나타냅니다.
    /// </summary>
    private string _stopBits = "One";
    /// <summary>
    /// _turnGapMs 값을 나타냅니다.
    /// </summary>
    private string _turnGapMs = "0";
    /// <summary>
    /// _readBufferSize 값을 나타냅니다.
    /// </summary>
    private string _readBufferSize = "1024";
    /// <summary>
    /// _writeBufferSize 값을 나타냅니다.
    /// </summary>
    private string _writeBufferSize = "1024";
    /// <summary>
    /// _halfDuplex 값을 나타냅니다.
    /// </summary>
    private bool _halfDuplex;

    /// <summary>
    /// PortName 값을 가져옵니다.
    /// </summary>
    public string PortName
    {
        get => _portName;
        set => SetProperty(ref _portName, value);
    }

    /// <summary>
    /// BaudRate 값을 가져옵니다.
    /// </summary>
    public string BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    /// <summary>
    /// DataBits 값을 가져옵니다.
    /// </summary>
    public string DataBits
    {
        get => _dataBits;
        set => SetProperty(ref _dataBits, value);
    }

    /// <summary>
    /// Parity 값을 가져옵니다.
    /// </summary>
    public string Parity
    {
        get => _parity;
        set => SetProperty(ref _parity, value);
    }

    /// <summary>
    /// StopBits 값을 가져옵니다.
    /// </summary>
    public string StopBits
    {
        get => _stopBits;
        set => SetProperty(ref _stopBits, value);
    }

    /// <summary>
    /// TurnGapMs 값을 가져옵니다.
    /// </summary>
    public string TurnGapMs
    {
        get => _turnGapMs;
        set => SetProperty(ref _turnGapMs, value);
    }

    /// <summary>
    /// ReadBufferSize 값을 가져옵니다.
    /// </summary>
    public string ReadBufferSize
    {
        get => _readBufferSize;
        set => SetProperty(ref _readBufferSize, value);
    }

    /// <summary>
    /// WriteBufferSize 값을 가져옵니다.
    /// </summary>
    public string WriteBufferSize
    {
        get => _writeBufferSize;
        set => SetProperty(ref _writeBufferSize, value);
    }

    /// <summary>
    /// HalfDuplex 값을 가져옵니다.
    /// </summary>
    public bool HalfDuplex
    {
        get => _halfDuplex;
        set => SetProperty(ref _halfDuplex, value);
    }

    /// <summary>
    /// Title 값을 가져옵니다.
    /// </summary>
    public override string Title => "Serial Target";

    /// <summary>
    /// Subtitle 값을 가져옵니다.
    /// </summary>
    public override string Subtitle => "COM-based device channels, dongles, and virtual loopback pairs.";

    /// <summary>
    /// BuildTransportOptions 작업을 수행합니다.
    /// </summary>
    public override TransportOptions BuildTransportOptions()
    {
        return new SerialTransportOptions
        {
            Type = "Serial",
            PortName = PortName.Trim(),
            BaudRate = ParseInt(BaudRate, "Serial Baud Rate"),
            DataBits = ParseInt(DataBits, "Serial Data Bits"),
            Parity = Parity,
            StopBits = StopBits,
            HalfDuplex = HalfDuplex,
            TurnGapMs = ParseInt(TurnGapMs, "Serial Turn Gap"),
            ReadBufferSize = ParseInt(ReadBufferSize, "Serial Read Buffer Size"),
            WriteBufferSize = ParseInt(WriteBufferSize, "Serial Write Buffer Size")
        };
    }
}