using System.Collections.Generic;

namespace CommLib.Domain.Messaging;

/// <summary>
/// named field 값을 schema-backed raw payload로 compose하고, raw payload를 named field 값으로 inspect합니다.
/// </summary>
public static class BitFieldPayloadSchemaCodec
{
    /// <summary>
    /// named field 값을 schema에서 정의한 payload byte 배열로 compose합니다. 값이 없는 field는 0으로 남겨둡니다.
    /// </summary>
    /// <param name="schema">compose에 적용할 payload schema입니다.</param>
    /// <param name="fieldValues">schema field 이름 기반 assignment 목록입니다.</param>
    /// <returns>schema 길이에 맞는 raw payload byte 배열입니다.</returns>
    public static byte[] Compose(BitFieldPayloadSchema schema, IEnumerable<BitFieldFieldAssignment> fieldValues)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(fieldValues);

        BitFieldPayloadSchemaValidator.ValidateAndThrow(schema);

        var payload = new byte[schema.PayloadLengthBytes];
        var fieldsByName = BuildFieldMap(schema);
        var assignedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assignment in fieldValues)
        {
            if (assignment is null)
            {
                throw new InvalidOperationException("Bit field assignments cannot contain null entries.");
            }

            if (!assignedNames.Add(assignment.Name))
            {
                throw new InvalidOperationException($"Bit field '{assignment.Name}' was assigned more than once.");
            }

            if (!fieldsByName.TryGetValue(assignment.Name, out var field))
            {
                throw new InvalidOperationException($"Bit field '{assignment.Name}' is not defined in the schema.");
            }

            var rawValue = NormalizeValue(field, assignment.Value);
            BitFieldCodec.WriteUnsigned(payload, field.ToDefinition(), rawValue);
        }

        return payload;
    }

    /// <summary>
    /// raw payload를 schema 기반 field 값 목록으로 inspect합니다.
    /// </summary>
    /// <param name="schema">payload를 해석할 payload schema입니다.</param>
    /// <param name="payload">inspect할 raw payload byte들입니다.</param>
    /// <returns>schema field 순서대로 정렬된 inspect 결과 목록입니다.</returns>
    public static IReadOnlyList<BitFieldFieldValue> Inspect(BitFieldPayloadSchema schema, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(schema);

        BitFieldPayloadSchemaValidator.ValidateAndThrow(schema);

        if (payload.Length != schema.PayloadLengthBytes)
        {
            throw new InvalidOperationException(
                $"Payload length {payload.Length} byte(s) does not match schema length {schema.PayloadLengthBytes} byte(s).");
        }

        var values = new List<BitFieldFieldValue>(schema.Fields.Count);
        foreach (var field in schema.Fields)
        {
            var definition = field.ToDefinition();
            decimal value = field.ScalarKind switch
            {
                BitFieldScalarKind.Unsigned => BitFieldCodec.ReadUnsigned(payload, definition),
                BitFieldScalarKind.Signed => BitFieldCodec.ReadSigned(payload, definition),
                _ => throw new InvalidOperationException($"Bit field '{definition.Name}' has an unsupported scalar kind.")
            };

            values.Add(new BitFieldFieldValue(definition, field.ScalarKind, value));
        }

        return values;
    }

    private static Dictionary<string, BitFieldPayloadField> BuildFieldMap(BitFieldPayloadSchema schema)
    {
        var fieldsByName = new Dictionary<string, BitFieldPayloadField>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in schema.Fields)
        {
            fieldsByName.Add(field.Name, field);
        }

        return fieldsByName;
    }

    private static ulong NormalizeValue(BitFieldPayloadField field, decimal value)
    {
        EnsureIntegralValue(field, value);

        var definition = field.ToDefinition();
        return field.ScalarKind switch
        {
            BitFieldScalarKind.Unsigned => NormalizeUnsignedValue(definition, value),
            BitFieldScalarKind.Signed => NormalizeSignedValue(definition, value),
            _ => throw new InvalidOperationException($"Bit field '{definition.Name}' has an unsupported scalar kind.")
        };
    }

    private static void EnsureIntegralValue(BitFieldPayloadField field, decimal value)
    {
        if (decimal.Truncate(value) != value)
        {
            throw new InvalidOperationException($"Bit field '{field.Name}' requires an integer value.");
        }
    }

    private static ulong NormalizeUnsignedValue(BitFieldDefinition field, decimal value)
    {
        if (value < 0)
        {
            throw new InvalidOperationException($"Bit field '{field.Name}' requires an unsigned value.");
        }

        var maxValue = GetUnsignedMaxValue(field.BitLength);
        if (value > maxValue)
        {
            throw new InvalidOperationException(
                $"Value {value} does not fit in unsigned bit field '{field.Name}' ({field.BitLength} bit(s)).");
        }

        return checked((ulong)value);
    }

    private static ulong NormalizeSignedValue(BitFieldDefinition field, decimal value)
    {
        var minValue = GetSignedMinValue(field.BitLength);
        var maxValue = GetSignedMaxValue(field.BitLength);
        if (value < minValue || value > maxValue)
        {
            throw new InvalidOperationException(
                $"Value {value} does not fit in signed bit field '{field.Name}' ({field.BitLength} bit(s)).");
        }

        var signedValue = checked((long)value);
        if (signedValue >= 0)
        {
            return checked((ulong)signedValue);
        }

        if (field.BitLength == 64)
        {
            return unchecked((ulong)signedValue);
        }

        var twoToBitLength = (decimal)(1UL << field.BitLength);
        return checked((ulong)(twoToBitLength + value));
    }

    private static decimal GetUnsignedMaxValue(int bitLength)
    {
        return bitLength == 64
            ? ulong.MaxValue
            : (decimal)((1UL << bitLength) - 1);
    }

    private static decimal GetSignedMinValue(int bitLength)
    {
        return bitLength == 64
            ? long.MinValue
            : -(decimal)(1L << (bitLength - 1));
    }

    private static decimal GetSignedMaxValue(int bitLength)
    {
        return bitLength == 64
            ? long.MaxValue
            : (decimal)((1L << (bitLength - 1)) - 1);
    }
}
