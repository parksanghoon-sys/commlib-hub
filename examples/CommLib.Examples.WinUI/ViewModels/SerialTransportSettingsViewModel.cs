using CommLib.Domain.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommLib.Examples.WinUI.ViewModels;

public sealed class SerialTransportSettingsViewModel : TransportSettingsViewModel
{
    public IReadOnlyList<string> ParityOptions { get; } = ["None", "Odd", "Even", "Mark", "Space"];

    public IReadOnlyList<string> StopBitsOptions { get; } = ["None", "One", "Two", "OnePointFive"];

    private string _portName = "COM3";
    private string _baudRate = "115200";
    private string _dataBits = "8";
    private string _parity = "None";
    private string _stopBits = "One";
    private string _turnGapMs = "0";
    private string _readBufferSize = "1024";
    private string _writeBufferSize = "1024";
    private bool _halfDuplex;

    public string PortName
    {
        get => _portName;
        set => SetProperty(ref _portName, value);
    }

    public string BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    public string DataBits
    {
        get => _dataBits;
        set => SetProperty(ref _dataBits, value);
    }

    public string Parity
    {
        get => _parity;
        set => SetProperty(ref _parity, value);
    }

    public string StopBits
    {
        get => _stopBits;
        set => SetProperty(ref _stopBits, value);
    }

    public string TurnGapMs
    {
        get => _turnGapMs;
        set => SetProperty(ref _turnGapMs, value);
    }

    public string ReadBufferSize
    {
        get => _readBufferSize;
        set => SetProperty(ref _readBufferSize, value);
    }

    public string WriteBufferSize
    {
        get => _writeBufferSize;
        set => SetProperty(ref _writeBufferSize, value);
    }

    public bool HalfDuplex
    {
        get => _halfDuplex;
        set => SetProperty(ref _halfDuplex, value);
    }

    public override string Title => "Serial Target";

    public override string Subtitle => "COM-based device channels, dongles, and virtual loopback pairs.";

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
