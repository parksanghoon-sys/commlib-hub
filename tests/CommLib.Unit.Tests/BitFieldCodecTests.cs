using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// raw payload bitfield codec가 비트 오프셋 기준 read/write를 올바르게 수행하는지 검증합니다.
/// </summary>
public sealed class BitFieldCodecTests
{
    [Fact]
    public void ReadUnsigned_SingleBitField_ReturnsExpectedValue()
    {
        var field = new BitFieldDefinition("ready", 4, 1);

        var value = BitFieldCodec.ReadUnsigned(new byte[] { 0b_0001_0000 }, field);

        Assert.Equal(1UL, value);
    }

    [Fact]
    public void ReadUnsigned_FieldAcrossByteBoundary_ReturnsExpectedValue()
    {
        var field = new BitFieldDefinition("temperatureRaw", 0, 12);

        var value = BitFieldCodec.ReadUnsigned(new byte[] { 0xBC, 0x0A }, field);

        Assert.Equal(0xABCUL, value);
    }

    [Fact]
    public void ReadSigned_NegativeField_SignExtendsValue()
    {
        var field = new BitFieldDefinition("delta", 0, 12);

        var value = BitFieldCodec.ReadSigned(new byte[] { 0x9C, 0x0F }, field);

        Assert.Equal(-100, value);
    }

    [Fact]
    public void WriteUnsigned_FieldAcrossByteBoundary_WritesExpectedBitsOnly()
    {
        var field = new BitFieldDefinition("temperatureRaw", 4, 12);
        var payload = new byte[] { 0x0F, 0x00, 0xF0 };

        BitFieldCodec.WriteUnsigned(payload, field, 0xABC);

        Assert.Equal(new byte[] { 0xCF, 0xAB, 0xF0 }, payload);
    }

    [Fact]
    public void WriteUnsigned_ValueTooLarge_Throws()
    {
        var field = new BitFieldDefinition("mode", 0, 3);
        var payload = new byte[1];

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BitFieldCodec.WriteUnsigned(payload, field, 0b1000));

        Assert.Contains("mode", exception.Message);
    }

    [Fact]
    public void ReadUnsigned_FieldOutsidePayload_Throws()
    {
        var field = new BitFieldDefinition("status", 12, 8);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BitFieldCodec.ReadUnsigned(new byte[] { 0x00, 0x01 }, field));

        Assert.Contains("status", exception.Message);
    }
}
