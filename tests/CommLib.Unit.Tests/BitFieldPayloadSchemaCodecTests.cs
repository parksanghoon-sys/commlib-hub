using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// bitfield payload schema codec가 named field 값을 raw payload로 compose/inspect하는지 검증합니다.
/// </summary>
public sealed class BitFieldPayloadSchemaCodecTests
{
    [Fact]
    public void Compose_SignedAndUnsignedFields_ReturnsExpectedPayload()
    {
        var payload = BitFieldPayloadSchemaCodec.Compose(
            CreateSchema(),
            new[]
            {
                new BitFieldFieldAssignment("mode", 5),
                new BitFieldFieldAssignment("enabled", 1),
                new BitFieldFieldAssignment("delta", -100)
            });

        Assert.Equal(new byte[] { 0xCD, 0xF9 }, payload);
    }

    [Fact]
    public void Inspect_SignedAndUnsignedFields_ReturnsExpectedValues()
    {
        var values = BitFieldPayloadSchemaCodec.Inspect(CreateSchema(), new byte[] { 0xCD, 0xF9 });

        Assert.Collection(
            values,
            item =>
            {
                Assert.Equal("mode", item.Field.Name);
                Assert.Equal(BitFieldScalarKind.Unsigned, item.ScalarKind);
                Assert.Equal(5m, item.Value);
            },
            item =>
            {
                Assert.Equal("enabled", item.Field.Name);
                Assert.Equal(BitFieldScalarKind.Unsigned, item.ScalarKind);
                Assert.Equal(1m, item.Value);
            },
            item =>
            {
                Assert.Equal("delta", item.Field.Name);
                Assert.Equal(BitFieldScalarKind.Signed, item.ScalarKind);
                Assert.Equal(-100m, item.Value);
            });
    }

    [Fact]
    public void Compose_UnknownField_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => BitFieldPayloadSchemaCodec.Compose(
                CreateSchema(),
                new[] { new BitFieldFieldAssignment("unknown", 1) }));

        Assert.Contains("unknown", exception.Message);
    }

    [Fact]
    public void Compose_FractionalValue_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => BitFieldPayloadSchemaCodec.Compose(
                CreateSchema(),
                new[] { new BitFieldFieldAssignment("mode", 1.5m) }));

        Assert.Contains("integer", exception.Message);
    }

    [Fact]
    public void Inspect_PayloadLengthMismatch_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => BitFieldPayloadSchemaCodec.Inspect(CreateSchema(), new byte[] { 0xCD }));

        Assert.Contains("does not match", exception.Message);
    }

    private static BitFieldPayloadSchema CreateSchema()
    {
        return new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 2,
            Fields = new[]
            {
                new BitFieldPayloadField { Name = "mode", BitOffset = 0, BitLength = 3 },
                new BitFieldPayloadField { Name = "enabled", BitOffset = 3, BitLength = 1 },
                new BitFieldPayloadField { Name = "delta", BitOffset = 4, BitLength = 12, ScalarKind = BitFieldScalarKind.Signed }
            }
        };
    }
}
