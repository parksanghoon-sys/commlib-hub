using System.Collections.Generic;

namespace CommLib.Domain.Messaging;

/// <summary>
/// <see cref="BitFieldPayloadSchema"/>가 low-level bit codec로 안전하게 사용 가능한지 검증합니다.
/// </summary>
public static class BitFieldPayloadSchemaValidator
{
    /// <summary>
    /// payload 길이, field 범위, 중복 이름, overlap 여부를 검증하고 조건을 만족하지 않으면 예외를 발생시킵니다.
    /// </summary>
    /// <param name="schema">검증할 payload schema입니다.</param>
    public static void ValidateAndThrow(BitFieldPayloadSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (schema.PayloadLengthBytes <= 0)
        {
            throw new InvalidOperationException("Bit field schema PayloadLengthBytes must be greater than 0.");
        }

        if (schema.Fields is null)
        {
            throw new InvalidOperationException("Bit field schema Fields collection is required.");
        }

        var payloadBitLength = checked(schema.PayloadLengthBytes * 8);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var definitions = new List<BitFieldDefinition>(schema.Fields.Count);

        foreach (var field in schema.Fields)
        {
            if (field is null)
            {
                throw new InvalidOperationException("Bit field schema cannot contain null fields.");
            }

            if (!Enum.IsDefined(field.ScalarKind))
            {
                throw new InvalidOperationException($"Bit field '{field.Name}' has an unsupported scalar kind.");
            }

            BitFieldDefinition definition;
            try
            {
                definition = field.ToDefinition();
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"Bit field schema contains an invalid field definition: {exception.Message}",
                    exception);
            }

            var fieldEnd = checked(definition.BitOffset + definition.BitLength);
            if (fieldEnd > payloadBitLength)
            {
                throw new InvalidOperationException(
                    $"Bit field '{definition.Name}' exceeds schema payload length {schema.PayloadLengthBytes} byte(s).");
            }

            if (!seenNames.Add(definition.Name))
            {
                throw new InvalidOperationException($"Bit field '{definition.Name}' is duplicated in the schema.");
            }

            definitions.Add(definition);
        }

        for (var leftIndex = 0; leftIndex < definitions.Count; leftIndex++)
        {
            var left = definitions[leftIndex];
            for (var rightIndex = leftIndex + 1; rightIndex < definitions.Count; rightIndex++)
            {
                var right = definitions[rightIndex];
                if (Overlaps(left, right))
                {
                    throw new InvalidOperationException(
                        $"Bit fields '{left.Name}' and '{right.Name}' overlap in the schema.");
                }
            }
        }
    }

    /// <summary>
    /// Overlaps 작업을 수행합니다.
    /// </summary>
    private static bool Overlaps(BitFieldDefinition left, BitFieldDefinition right)
    {
        var leftEnd = checked(left.BitOffset + left.BitLength);
        var rightEnd = checked(right.BitOffset + right.BitLength);
        return left.BitOffset < rightEnd && right.BitOffset < leftEnd;
    }
}
