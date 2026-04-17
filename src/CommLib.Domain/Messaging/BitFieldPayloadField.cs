namespace CommLib.Domain.Messaging;

/// <summary>
/// Defines one named bitfield inside a payload schema.
/// </summary>
public sealed class BitFieldPayloadField
{
    /// <summary>
    /// Gets or initializes the field name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the field offset from the start of the payload. Bit 0 is the LSB of the first byte.
    /// </summary>
    public int BitOffset { get; init; }

    /// <summary>
    /// Gets or initializes the field length in bits.
    /// </summary>
    public int BitLength { get; init; }

    /// <summary>
    /// Gets or initializes whether the value should be interpreted as unsigned or signed.
    /// </summary>
    public BitFieldScalarKind ScalarKind { get; init; } = BitFieldScalarKind.Unsigned;

    /// <summary>
    /// Gets or initializes the byte order used when a field spans multiple whole bytes.
    /// </summary>
    public BitFieldEndianness Endianness { get; init; } = BitFieldEndianness.LittleEndian;

    /// <summary>
    /// Converts the schema field to the low-level codec definition.
    /// </summary>
    /// <returns>A validated <see cref="BitFieldDefinition"/> instance.</returns>
    public BitFieldDefinition ToDefinition() => new(Name, BitOffset, BitLength, Endianness);
}
