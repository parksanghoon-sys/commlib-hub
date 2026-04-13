using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// Verifies that the schema-backed payload codec can compose and inspect named fields.
/// </summary>
public sealed class BitFieldPayloadSchemaCodecTests
{
    [Fact]
    public void Compose_SignedAndUnsignedFields_ReturnsExpectedPayload()
    {
        var payload = BitFieldPayloadSchemaCodec.Compose(
            CreateLittleEndianSchema(),
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
        var values = BitFieldPayloadSchemaCodec.Inspect(CreateLittleEndianSchema(), new byte[] { 0xCD, 0xF9 });

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
    public void Compose_BigEndianWholeByteField_ReturnsExpectedPayload()
    {
        var payload = BitFieldPayloadSchemaCodec.Compose(
            CreateBigEndianSchema(),
            new[]
            {
                new BitFieldFieldAssignment("register", 0x1234),
                new BitFieldFieldAssignment("tail", 0xAB)
            });

        Assert.Equal(new byte[] { 0x12, 0x34, 0xAB }, payload);
    }

    [Fact]
    public void Inspect_BigEndianWholeByteField_ReturnsExpectedValues()
    {
        var values = BitFieldPayloadSchemaCodec.Inspect(CreateBigEndianSchema(), new byte[] { 0x12, 0x34, 0xAB });

        Assert.Collection(
            values,
            item =>
            {
                Assert.Equal("register", item.Field.Name);
                Assert.Equal(BitFieldEndianness.BigEndian, item.Field.Endianness);
                Assert.Equal(4660m, item.Value);
            },
            item =>
            {
                Assert.Equal("tail", item.Field.Name);
                Assert.Equal(BitFieldEndianness.LittleEndian, item.Field.Endianness);
                Assert.Equal(171m, item.Value);
            });
    }

    [Fact]
    public void Compose_MixedEndianSchemaWithOffsetAndSignedField_ReturnsExpectedPayload()
    {
        var payload = BitFieldPayloadSchemaCodec.Compose(
            CreateMixedEndianSchema(),
            new[]
            {
                new BitFieldFieldAssignment("prefix", 0xAA),
                new BitFieldFieldAssignment("register", 0x1234),
                new BitFieldFieldAssignment("delta", -100)
            });

        Assert.Equal(new byte[] { 0xAA, 0x12, 0x34, 0xFF, 0x9C }, payload);
    }

    [Fact]
    public void Inspect_MixedEndianSchemaWithOffsetAndSignedField_ReturnsExpectedValues()
    {
        var values = BitFieldPayloadSchemaCodec.Inspect(
            CreateMixedEndianSchema(),
            new byte[] { 0xAA, 0x12, 0x34, 0xFF, 0x9C });

        Assert.Collection(
            values,
            item =>
            {
                Assert.Equal("prefix", item.Field.Name);
                Assert.Equal(170m, item.Value);
            },
            item =>
            {
                Assert.Equal("register", item.Field.Name);
                Assert.Equal(BitFieldEndianness.BigEndian, item.Field.Endianness);
                Assert.Equal(4660m, item.Value);
            },
            item =>
            {
                Assert.Equal("delta", item.Field.Name);
                Assert.Equal(BitFieldScalarKind.Signed, item.ScalarKind);
                Assert.Equal(BitFieldEndianness.BigEndian, item.Field.Endianness);
                Assert.Equal(-100m, item.Value);
            });
    }

    [Fact]
    public void Compose_UnknownField_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => BitFieldPayloadSchemaCodec.Compose(
                CreateLittleEndianSchema(),
                new[] { new BitFieldFieldAssignment("unknown", 1) }));

        Assert.Contains("unknown", exception.Message);
    }

    [Fact]
    public void Compose_FractionalValue_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => BitFieldPayloadSchemaCodec.Compose(
                CreateLittleEndianSchema(),
                new[] { new BitFieldFieldAssignment("mode", 1.5m) }));

        Assert.Contains("integer", exception.Message);
    }

    [Fact]
    public void Inspect_PayloadLengthMismatch_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => BitFieldPayloadSchemaCodec.Inspect(CreateLittleEndianSchema(), new byte[] { 0xCD }));

        Assert.Contains("does not match", exception.Message);
    }

    private static BitFieldPayloadSchema CreateLittleEndianSchema()
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

    private static BitFieldPayloadSchema CreateBigEndianSchema()
    {
        return new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 3,
            Fields = new[]
            {
                new BitFieldPayloadField
                {
                    Name = "register",
                    BitOffset = 0,
                    BitLength = 16,
                    Endianness = BitFieldEndianness.BigEndian
                },
                new BitFieldPayloadField
                {
                    Name = "tail",
                    BitOffset = 16,
                    BitLength = 8
                }
            }
        };
    }

    private static BitFieldPayloadSchema CreateMixedEndianSchema()
    {
        return new BitFieldPayloadSchema
        {
            PayloadLengthBytes = 5,
            Fields = new[]
            {
                new BitFieldPayloadField
                {
                    Name = "prefix",
                    BitOffset = 0,
                    BitLength = 8
                },
                new BitFieldPayloadField
                {
                    Name = "register",
                    BitOffset = 8,
                    BitLength = 16,
                    Endianness = BitFieldEndianness.BigEndian
                },
                new BitFieldPayloadField
                {
                    Name = "delta",
                    BitOffset = 24,
                    BitLength = 16,
                    ScalarKind = BitFieldScalarKind.Signed,
                    Endianness = BitFieldEndianness.BigEndian
                }
            }
        };
    }
}
