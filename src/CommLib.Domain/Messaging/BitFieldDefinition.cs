namespace CommLib.Domain.Messaging;

/// <summary>
/// Describes a named bitfield range inside a raw payload.
/// </summary>
public sealed record BitFieldDefinition
{
    /// <summary>
    /// Creates a definition from a byte index and inclusive bit range within that byte.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="byteIndex">The zero-based byte index from the start of the payload.</param>
    /// <param name="startBit">The inclusive start bit in the byte. Bit 0 is the LSB.</param>
    /// <param name="endBit">The inclusive end bit in the byte. Bit 7 is the MSB.</param>
    /// <param name="endianness">The byte order used when a field spans multiple whole bytes.</param>
    /// <returns>A validated <see cref="BitFieldDefinition"/> instance.</returns>
    public static BitFieldDefinition FromByteBits(
        string name,
        int byteIndex,
        int startBit,
        int endBit,
        BitFieldEndianness endianness = BitFieldEndianness.LittleEndian)
    {
        if (byteIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteIndex), "Byte index must be greater than or equal to 0.");
        }

        if (startBit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(startBit), "Start bit must be between 0 and 7.");
        }

        if (endBit is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(endBit), "End bit must be between 0 and 7.");
        }

        if (endBit < startBit)
        {
            throw new ArgumentException("End bit must be greater than or equal to start bit.", nameof(endBit));
        }

        var bitOffset = checked((byteIndex * 8) + startBit);
        var bitLength = endBit - startBit + 1;
        return new BitFieldDefinition(name, bitOffset, bitLength, endianness);
    }

    /// <summary>
    /// Initializes a <see cref="BitFieldDefinition"/> instance.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="bitOffset">The field offset from the start of the payload. Bit 0 is the LSB of the first byte.</param>
    /// <param name="bitLength">The field length in bits. The current implementation supports up to 64 bits.</param>
    /// <param name="endianness">The byte order used when a field spans multiple whole bytes.</param>
    public BitFieldDefinition(
        string name,
        int bitOffset,
        int bitLength,
        BitFieldEndianness endianness = BitFieldEndianness.LittleEndian)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bit field name is required.", nameof(name));
        }

        if (bitOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitOffset), "Bit field offset must be greater than or equal to 0.");
        }

        if (bitLength is <= 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitLength), "Bit field length must be between 1 and 64 bits.");
        }

        if (!Enum.IsDefined(endianness))
        {
            throw new ArgumentOutOfRangeException(nameof(endianness), "Bit field endianness is invalid.");
        }

        if (endianness == BitFieldEndianness.BigEndian &&
            bitLength > 8 &&
            (bitOffset % 8 != 0 || bitLength % 8 != 0))
        {
            throw new ArgumentException(
                "Big-endian bit fields wider than one byte must be byte-aligned and use a whole-byte length.",
                nameof(bitLength));
        }

        Name = name;
        BitOffset = bitOffset;
        BitLength = bitLength;
        Endianness = endianness;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the field offset from the start of the payload. Bit 0 is the LSB of the first byte.
    /// </summary>
    public int BitOffset { get; }

    /// <summary>
    /// Gets the field length in bits.
    /// </summary>
    public int BitLength { get; }

    /// <summary>
    /// Gets the byte order used when a field spans multiple whole bytes.
    /// </summary>
    public BitFieldEndianness Endianness { get; }
}
