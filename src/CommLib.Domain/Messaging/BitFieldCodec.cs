namespace CommLib.Domain.Messaging;

/// <summary>
/// raw payload를 bitfield 단위로 읽고 쓰는 공용 codec입니다.
/// </summary>
public static class BitFieldCodec
{
    /// <summary>
    /// 지정한 bitfield를 unsigned 정수로 읽습니다.
    /// </summary>
    /// <param name="payload">원본 payload입니다.</param>
    /// <param name="field">읽을 field 정의입니다.</param>
    /// <returns>field 구간의 unsigned 값입니다.</returns>
    public static ulong ReadUnsigned(ReadOnlySpan<byte> payload, BitFieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureFieldFits(payload.Length, field);

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
    /// 지정한 bitfield를 signed 정수로 읽습니다.
    /// </summary>
    /// <param name="payload">원본 payload입니다.</param>
    /// <param name="field">읽을 field 정의입니다.</param>
    /// <returns>field 구간의 signed 값입니다.</returns>
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
    /// 지정한 bitfield 구간에 unsigned 정수 값을 씁니다.
    /// </summary>
    /// <param name="payload">쓰기 대상 payload입니다.</param>
    /// <param name="field">쓸 field 정의입니다.</param>
    /// <param name="value">쓸 unsigned 값입니다.</param>
    public static void WriteUnsigned(Span<byte> payload, BitFieldDefinition field, ulong value)
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureFieldFits(payload.Length, field);
        EnsureValueFits(field, value);

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
