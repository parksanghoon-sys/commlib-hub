using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// bitfield payload schema validator가 payload 길이, overlap, duplicate 조건을 검증하는지 확인합니다.
/// </summary>
public sealed class BitFieldPayloadSchemaValidatorTests
{
    [Fact]
    public void ValidateAndThrow_FieldOutsidePayload_Throws()
    {
        var schema = new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 1,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "status", BitOffset = 4, BitLength = 8 }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => BitFieldPayloadSchemaValidator.ValidateAndThrow(schema));

        Assert.Contains("status", exception.Message);
    }

    [Fact]
    public void ValidateAndThrow_DuplicateFieldName_Throws()
    {
        var schema = new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 2,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 3 },
                new BitFieldPayloadField { Name = "mode", BitOffset = 8, BitLength = 3 }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => BitFieldPayloadSchemaValidator.ValidateAndThrow(schema));

        Assert.Contains("duplicated", exception.Message);
    }

    [Fact]
    public void ValidateAndThrow_OverlappingFields_Throws()
    {
        var schema = new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 2,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 8 },
                new BitFieldPayloadField { Name = "delta", BitOffset = 4, BitLength = 8 }
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => BitFieldPayloadSchemaValidator.ValidateAndThrow(schema));

        Assert.Contains("overlap", exception.Message);
    }
}
