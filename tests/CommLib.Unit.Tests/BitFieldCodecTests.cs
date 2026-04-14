using CommLib.Domain.Messaging;
using Xunit;

namespace CommLib.Unit.Tests;

/// <summary>
/// Verifies that the low-level bitfield codec reads and writes payload bits correctly.
/// </summary>
public sealed class BitFieldCodecTests
{
    [Fact]
    /// <summary>
    /// ReadUnsigned_SingleBitField_ReturnsExpectedValue 작업을 수행합니다.
    /// </summary>
    public void ReadUnsigned_SingleBitField_ReturnsExpectedValue()
    {
        var field = new BitFieldDefinition("ready", 4, 1);

        var value = BitFieldCodec.ReadUnsigned(new byte[] { 0b_0001_0000 }, field);

        Assert.Equal(1UL, value);
    }

    [Fact]
    /// <summary>
    /// ReadUnsigned_FieldAcrossByteBoundary_ReturnsExpectedValue 작업을 수행합니다.
    /// </summary>
    public void ReadUnsigned_FieldAcrossByteBoundary_ReturnsExpectedValue()
    {
        var field = new BitFieldDefinition("temperatureRaw", 0, 12);

        var value = BitFieldCodec.ReadUnsigned(new byte[] { 0xBC, 0x0A }, field);

        Assert.Equal(0xABCUL, value);
    }

    [Fact]
    /// <summary>
    /// ReadSigned_NegativeField_SignExtendsValue 작업을 수행합니다.
    /// </summary>
    public void ReadSigned_NegativeField_SignExtendsValue()
    {
        var field = new BitFieldDefinition("delta", 0, 12);

        var value = BitFieldCodec.ReadSigned(new byte[] { 0x9C, 0x0F }, field);

        Assert.Equal(-100, value);
    }

    [Fact]
    /// <summary>
    /// ReadUnsigned_BigEndianWholeByteField_ReturnsExpectedValue 작업을 수행합니다.
    /// </summary>
    public void ReadUnsigned_BigEndianWholeByteField_ReturnsExpectedValue()
    {
        var field = new BitFieldDefinition("register", 0, 16, BitFieldEndianness.BigEndian);

        var value = BitFieldCodec.ReadUnsigned(new byte[] { 0x12, 0x34 }, field);

        Assert.Equal(0x1234UL, value);
    }

    [Fact]
    /// <summary>
    /// ReadUnsigned_BigEndianWholeByteFieldAtOffset_ReturnsExpectedValue 작업을 수행합니다.
    /// </summary>
    public void ReadUnsigned_BigEndianWholeByteFieldAtOffset_ReturnsExpectedValue()
    {
        var field = new BitFieldDefinition("register", 8, 16, BitFieldEndianness.BigEndian);

        var value = BitFieldCodec.ReadUnsigned(new byte[] { 0xAA, 0x12, 0x34, 0xBB }, field);

        Assert.Equal(0x1234UL, value);
    }

    [Fact]
    /// <summary>
    /// ReadSigned_BigEndianWholeByteField_SignExtendsValue 작업을 수행합니다.
    /// </summary>
    public void ReadSigned_BigEndianWholeByteField_SignExtendsValue()
    {
        var field = new BitFieldDefinition("delta", 0, 16, BitFieldEndianness.BigEndian);

        var value = BitFieldCodec.ReadSigned(new byte[] { 0xFF, 0x9C }, field);

        Assert.Equal(-100, value);
    }

    [Fact]
    /// <summary>
    /// WriteUnsigned_FieldAcrossByteBoundary_WritesExpectedBitsOnly 작업을 수행합니다.
    /// </summary>
    public void WriteUnsigned_FieldAcrossByteBoundary_WritesExpectedBitsOnly()
    {
        var field = new BitFieldDefinition("temperatureRaw", 4, 12);
        var payload = new byte[] { 0x0F, 0x00, 0xF0 };

        BitFieldCodec.WriteUnsigned(payload, field, 0xABC);

        Assert.Equal(new byte[] { 0xCF, 0xAB, 0xF0 }, payload);
    }

    [Fact]
    /// <summary>
    /// WriteUnsigned_BigEndianWholeByteField_WritesMostSignificantByteFirst 작업을 수행합니다.
    /// </summary>
    public void WriteUnsigned_BigEndianWholeByteField_WritesMostSignificantByteFirst()
    {
        var field = new BitFieldDefinition("register", 0, 16, BitFieldEndianness.BigEndian);
        var payload = new byte[2];

        BitFieldCodec.WriteUnsigned(payload, field, 0x1234);

        Assert.Equal(new byte[] { 0x12, 0x34 }, payload);
    }

    [Fact]
    /// <summary>
    /// WriteUnsigned_BigEndianWholeByteFieldAtOffset_PreservesSurroundingBytes 작업을 수행합니다.
    /// </summary>
    public void WriteUnsigned_BigEndianWholeByteFieldAtOffset_PreservesSurroundingBytes()
    {
        var field = new BitFieldDefinition("register", 8, 16, BitFieldEndianness.BigEndian);
        var payload = new byte[] { 0xAA, 0x00, 0x00, 0xBB };

        BitFieldCodec.WriteUnsigned(payload, field, 0x1234);

        Assert.Equal(new byte[] { 0xAA, 0x12, 0x34, 0xBB }, payload);
    }

    [Fact]
    /// <summary>
    /// WriteUnsigned_ValueTooLarge_Throws 작업을 수행합니다.
    /// </summary>
    public void WriteUnsigned_ValueTooLarge_Throws()
    {
        var field = new BitFieldDefinition("mode", 0, 3);
        var payload = new byte[1];

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BitFieldCodec.WriteUnsigned(payload, field, 0b1000));

        Assert.Contains("mode", exception.Message);
    }

    [Fact]
    /// <summary>
    /// ReadUnsigned_FieldOutsidePayload_Throws 작업을 수행합니다.
    /// </summary>
    public void ReadUnsigned_FieldOutsidePayload_Throws()
    {
        var field = new BitFieldDefinition("status", 12, 8);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BitFieldCodec.ReadUnsigned(new byte[] { 0x00, 0x01 }, field));

        Assert.Contains("status", exception.Message);
    }

    [Fact]
    /// <summary>
    /// Ctor_BigEndianMultiByteFieldWithoutWholeBytes_Throws 작업을 수행합니다.
    /// </summary>
    public void Ctor_BigEndianMultiByteFieldWithoutWholeBytes_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => new BitFieldDefinition("bad", 4, 12, BitFieldEndianness.BigEndian));

        Assert.Contains("byte-aligned", exception.Message);
    }
}
