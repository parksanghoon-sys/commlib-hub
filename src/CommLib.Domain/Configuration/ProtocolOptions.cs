namespace CommLib.Domain.Configuration;

public sealed class ProtocolOptions
{
    public string Type { get; init; } = "LengthPrefixed";
    public int MaxFrameLength { get; init; } = 65536;
    public bool UseCrc { get; init; } = true;
    public byte? Stx { get; init; }
    public byte? Etx { get; init; }
}
