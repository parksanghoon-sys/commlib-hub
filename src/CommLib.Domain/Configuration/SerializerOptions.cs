namespace CommLib.Domain.Configuration;

/// <summary>
/// Represents serializer selection settings for device messages.
/// </summary>
public sealed class SerializerOptions
{
    /// <summary>
    /// Gets the serializer implementation name.
    /// </summary>
    public string Type { get; init; } = "AutoBinary";
}
