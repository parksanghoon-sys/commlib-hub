namespace CommLib.Domain.Configuration;

public sealed class CommLibOptions
{
    public List<DeviceProfileRaw> Devices { get; init; } = [];
}
