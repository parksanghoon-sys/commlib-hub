namespace CommLib.Domain.Messaging;

/// <summary>
/// Reads and writes scalar values against bitfield definitions in a raw payload.
/// </summary>
public static class BitFieldCodec
{
    /// <summary>
    /// Reads a bitfield as an unsigned integer.
    /// </summary>
    /// <param name="payload">The source payload.</param>
    /// <param name="field">The field to read.</param>
    /// <returns>The unsigned value stored in the field.</returns>
    public static ulong ReadUnsigned(ReadOnlySpan<byte> payload, BitFieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureFieldFits(payload.Length, field);

        return field.Endianness switch
        {
            BitFieldEndianness.LittleEndian => ReadUnsignedLittleEndian(payload, field),
            BitFieldEndianness.BigEndian => ReadUnsignedBigEndian(payload, field),
            _ => throw new ArgumentOutOfRangeException(nameof(field), $"Bit field '{field.Name}' has an unsupported endianness.")
        };
    }

    /// <summary>
    /// Reads a bitfield as a signed integer.
    /// </summary>
    /// <param name="payload">The source payload.</param>
    /// <param name="field">The field to read.</param>
    /// <returns>The signed value stored in the field.</returns>
    public static long ReadSigned(ReadOnlySpan<byte> payload, BitFieldDefinition field)
    {
        var value = ReadUnsigned(payload, field);
        if (field.BitLength == 64)
        {
            return unchecked((long)value);
        }

        var signBitMask = 1UL << (field.BitLength - 1);
        if ((value & signBitMask) == 0)
        {
            return (long)value;
        }

        var extensionMask = ulong.MaxValue << field.BitLength;
        return unchecked((long)(value | extensionMask));
    }

    /// <summary>
    /// Writes an unsigned integer value into a bitfield.
    /// </summary>
    /// <param name="payload">The destination payload.</param>
    /// <param name="field">The field to write.</param>
    /// <param name="value">The unsigned value to write.</param>
    public static void WriteUnsigned(Span<byte> payload, BitFieldDefinition field, ulong value)
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureFieldFits(payload.Length, field);
        EnsureValueFits(field, value);

        switch (field.Endianness)
        {
            case BitFieldEndianness.LittleEndian:
                WriteUnsignedLittleEndian(payload, field, value);
                return;

            case BitFieldEndianness.BigEndian:
                WriteUnsignedBigEndian(payload, field, value);
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(field), $"Bit field '{field.Name}' has an unsupported endianness.");
        }
    }

    /// <summary>
    /// ReadUnsignedLittleEndian 작업을 수행합니다.
    /// </summary>
    private static ulong ReadUnsignedLittleEndian(ReadOnlySpan<byte> payload, BitFieldDefinition field)
    {
        ulong value = 0;
        for (var bitIndex = 0; bitIndex < field.BitLength; bitIndex++)
        {
            var absoluteBitIndex = field.BitOffset + bitIndex;
            var byteIndex = absoluteBitIndex / 8;
            var bitInByte = absoluteBitIndex % 8;
            var bit = (payload[byteIndex] >> bitInByte) & 0x01;
            value |= (ulong)bit << bitIndex;
        }

        return value;
    }

    /// <summary>
    /// ReadUnsignedBigEndian 작업을 수행합니다.
    /// </summary>
    private static ulong ReadUnsignedBigEndian(ReadOnlySpan<byte> payload, BitFieldDefinition field)
    {
        if (field.BitLength <= 8)
        {
            return ReadUnsignedLittleEndian(payload, field);
        }

        var byteIndex = field.BitOffset / 8;
        var byteCount = field.BitLength / 8;
        ulong value = 0;

        for (var index = 0; index < byteCount; index++)
        {
            value = (value << 8) | payload[byteIndex + index];
        }

        return value;
    }

    /// <summary>
    /// WriteUnsignedLittleEndian 작업을 수행합니다.
    /// </summary>
    private static void WriteUnsignedLittleEndian(Span<byte> payload, BitFieldDefinition field, ulong value)
    {
        for (var bitIndex = 0; bitIndex < field.BitLength; bitIndex++)
        {
            var absoluteBitIndex = field.BitOffset + bitIndex;
            var byteIndex = absoluteBitIndex / 8;
            var bitInByte = absoluteBitIndex % 8;
            var mask = (byte)(1 << bitInByte);
            var bit = (value >> bitIndex) & 0x01;

            if (bit == 1)
            {
                payload[byteIndex] |= mask;
            }
            else
            {
                payload[byteIndex] &= unchecked((byte)~mask);
            }
        }
    }

    /// <summary>
    /// WriteUnsignedBigEndian 작업을 수행합니다.
    /// </summary>
    private static void WriteUnsignedBigEndian(Span<byte> payload, BitFieldDefinition field, ulong value)
    {
        if (field.BitLength <= 8)
        {
            WriteUnsignedLittleEndian(payload, field, value);
            return;
        }

        var byteIndex = field.BitOffset / 8;
        var byteCount = field.BitLength / 8;

        for (var index = 0; index < byteCount; index++)
        {
            var shift = (byteCount - index - 1) * 8;
            payload[byteIndex + index] = (byte)((value >> shift) & 0xFF);
        }
    }

    /// <summary>
    /// EnsureFieldFits 작업을 수행합니다.
    /// </summary>
    private static void EnsureFieldFits(int payloadLength, BitFieldDefinition field)
    {
        var payloadBitLength = checked(payloadLength * 8);
        var fieldEnd = checked(field.BitOffset + field.BitLength);
        if (fieldEnd > payloadBitLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(field),
                $"Bit field '{field.Name}' exceeds payload length {payloadLength} byte(s).");
        }
    }

    /// <summary>
    /// EnsureValueFits 작업을 수행합니다.
    /// </summary>
    private static void EnsureValueFits(BitFieldDefinition field, ulong value)
    {
        if (field.BitLength == 64)
        {
            return;
        }

        var maxValue = (1UL << field.BitLength) - 1;
        if (value > maxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Value {value} does not fit in bit field '{field.Name}' ({field.BitLength} bit(s)).");
        }
    }
}
