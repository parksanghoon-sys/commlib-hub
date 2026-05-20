using System.Buffers.Binary;
using CommLib.Domain.Configuration;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 설정으로 start bytes, payload length prefix, checksum을 조합하는 범용 binary frame protocol입니다.
/// </summary>
public sealed class BinaryFrameProtocol : IProtocol
{
    private readonly byte[] _startBytes;
    private readonly int _lengthPrefixSizeBytes;
    private readonly BitFieldEndianness _lengthPrefixEndianness;
    private readonly string _checksumType;
    private readonly BitFieldEndianness _checksumEndianness;
    private readonly string _checksumCoverage;
    private readonly int _checksumSizeBytes;
    private readonly int _maxFrameLength;

    /// <summary>
    /// <see cref="BinaryFrameProtocol"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">frame envelope 설정입니다.</param>
    /// <param name="maxFrameLength">허용할 최대 encoded frame byte 길이입니다.</param>
    public BinaryFrameProtocol(BinaryFrameOptions options, int maxFrameLength)
    {
        ArgumentNullException.ThrowIfNull(options);

        var lengthPrefix = options.LengthPrefix ?? new BinaryFrameLengthPrefixOptions();
        var checksum = options.Checksum ?? new BinaryFrameChecksumOptions();

        _startBytes = ParseStartBytes(options.StartHex);
        _lengthPrefixSizeBytes = ValidateLengthPrefixSize(lengthPrefix.SizeBytes);
        _lengthPrefixEndianness = ValidateEndianness(lengthPrefix.Endianness, nameof(lengthPrefix.Endianness));
        _checksumType = ValidateChecksumType(checksum.Type);
        _checksumEndianness = ValidateEndianness(checksum.Endianness, nameof(checksum.Endianness));
        _checksumCoverage = ValidateChecksumCoverage(checksum.Coverage);
        _checksumSizeBytes = IsCrc16Modbus(_checksumType) ? 2 : 0;

        var minimumFrameLength = checked(_startBytes.Length + _lengthPrefixSizeBytes + _checksumSizeBytes);
        if (maxFrameLength < minimumFrameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFrameLength),
                maxFrameLength,
                $"Max frame length must be at least {minimumFrameLength} byte(s) for the configured BinaryFrame protocol.");
        }

        _maxFrameLength = maxFrameLength;
    }

    /// <summary>
    /// 프로토콜 이름을 가져옵니다.
    /// </summary>
    public string Name => ProtocolTypes.BinaryFrame;

    /// <summary>
    /// 현재 인스턴스가 허용하는 최대 frame byte 길이입니다.
    /// </summary>
    public int MaxFrameLength => _maxFrameLength;

    /// <summary>
    /// payload를 설정된 binary frame envelope로 감쌉니다.
    /// </summary>
    /// <param name="payload">전송할 raw payload입니다.</param>
    /// <returns>전송 가능한 encoded frame입니다.</returns>
    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        EnsurePayloadLengthFitsPrefix(payload.Length);

        var frameLength = checked(_startBytes.Length + _lengthPrefixSizeBytes + payload.Length + _checksumSizeBytes);
        if (frameLength > _maxFrameLength)
        {
            throw new InvalidOperationException(
                $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
        }

        var frame = new byte[frameLength];
        var offset = 0;
        _startBytes.CopyTo(frame.AsSpan(offset));
        offset += _startBytes.Length;

        WriteLengthPrefix(frame.AsSpan(offset, _lengthPrefixSizeBytes), payload.Length);
        offset += _lengthPrefixSizeBytes;

        payload.CopyTo(frame.AsSpan(offset));
        offset += payload.Length;

        if (_checksumSizeBytes > 0)
        {
            var checksum = ComputeChecksum(GetChecksumCoverage(frame.AsSpan(0, offset), frame.AsSpan(_startBytes.Length + _lengthPrefixSizeBytes, payload.Length)));
            WriteChecksum(frame.AsSpan(offset, _checksumSizeBytes), checksum);
        }

        return frame;
    }

    /// <summary>
    /// 입력 buffer에서 완전한 binary frame 하나를 읽어 payload를 추출합니다.
    /// </summary>
    /// <param name="buffer">decode할 입력 buffer입니다.</param>
    /// <param name="payload">추출된 payload입니다.</param>
    /// <param name="bytesConsumed">소비한 전체 frame byte 수입니다.</param>
    /// <returns>완전한 frame을 읽었으면 <see langword="true"/>입니다.</returns>
    public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
    {
        payload = Array.Empty<byte>();
        bytesConsumed = 0;

        if (buffer.Length < _startBytes.Length)
        {
            return false;
        }

        if (_startBytes.Length > 0 && !buffer[.._startBytes.Length].SequenceEqual(_startBytes))
        {
            throw new InvalidOperationException("Frame start bytes do not match the configured BinaryFrame start bytes.");
        }

        var lengthPrefixOffset = _startBytes.Length;
        if (buffer.Length < lengthPrefixOffset + _lengthPrefixSizeBytes)
        {
            return false;
        }

        var payloadLength = ReadLengthPrefix(buffer.Slice(lengthPrefixOffset, _lengthPrefixSizeBytes));
        var frameLength = checked(_startBytes.Length + _lengthPrefixSizeBytes + payloadLength + _checksumSizeBytes);
        if (frameLength > _maxFrameLength)
        {
            throw new InvalidOperationException(
                $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
        }

        if (buffer.Length < frameLength)
        {
            return false;
        }

        var payloadOffset = lengthPrefixOffset + _lengthPrefixSizeBytes;
        var payloadSpan = buffer.Slice(payloadOffset, payloadLength);

        if (_checksumSizeBytes > 0)
        {
            var checksumOffset = payloadOffset + payloadLength;
            var expected = ReadChecksum(buffer.Slice(checksumOffset, _checksumSizeBytes));
            var actual = ComputeChecksum(GetChecksumCoverage(buffer[..checksumOffset], payloadSpan));
            if (expected != actual)
            {
                throw new InvalidOperationException("Frame checksum is invalid.");
            }
        }

        payload = payloadSpan.ToArray();
        bytesConsumed = frameLength;
        return true;
    }

    private static byte[] ParseStartBytes(string? startHex)
    {
        if (string.IsNullOrWhiteSpace(startHex))
        {
            return [];
        }

        try
        {
            return HexPayloadParser.Parse(startHex);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("BinaryFrame StartHex must contain valid hexadecimal byte pairs.", nameof(startHex), exception);
        }
    }

    private static int ValidateLengthPrefixSize(int sizeBytes)
    {
        return sizeBytes is 1 or 2 or 4
            ? sizeBytes
            : throw new ArgumentOutOfRangeException(nameof(sizeBytes), sizeBytes, "BinaryFrame length prefix size must be 1, 2, or 4 byte(s).");
    }

    private static BitFieldEndianness ValidateEndianness(BitFieldEndianness endianness, string parameterName)
    {
        return Enum.IsDefined(endianness)
            ? endianness
            : throw new ArgumentOutOfRangeException(parameterName, endianness, "BinaryFrame endianness is invalid.");
    }

    private static string ValidateChecksumType(string? type)
    {
        var value = string.IsNullOrWhiteSpace(type) ? BinaryFrameChecksumTypes.None : type;
        if (value.Equals(BinaryFrameChecksumTypes.None, StringComparison.OrdinalIgnoreCase))
        {
            return BinaryFrameChecksumTypes.None;
        }

        if (value.Equals(BinaryFrameChecksumTypes.Crc16Modbus, StringComparison.OrdinalIgnoreCase))
        {
            return BinaryFrameChecksumTypes.Crc16Modbus;
        }

        throw new ArgumentException("BinaryFrame checksum type is invalid.", nameof(type));
    }

    private static string ValidateChecksumCoverage(string? coverage)
    {
        var value = string.IsNullOrWhiteSpace(coverage)
            ? BinaryFrameChecksumCoverageTypes.FrameWithoutChecksum
            : coverage;

        if (value.Equals(BinaryFrameChecksumCoverageTypes.FrameWithoutChecksum, StringComparison.OrdinalIgnoreCase))
        {
            return BinaryFrameChecksumCoverageTypes.FrameWithoutChecksum;
        }

        if (value.Equals(BinaryFrameChecksumCoverageTypes.Payload, StringComparison.OrdinalIgnoreCase))
        {
            return BinaryFrameChecksumCoverageTypes.Payload;
        }

        throw new ArgumentException("BinaryFrame checksum coverage is invalid.", nameof(coverage));
    }

    private static bool IsCrc16Modbus(string checksumType)
    {
        return checksumType.Equals(BinaryFrameChecksumTypes.Crc16Modbus, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsurePayloadLengthFitsPrefix(int payloadLength)
    {
        var maxPayloadLength = _lengthPrefixSizeBytes switch
        {
            1 => byte.MaxValue,
            2 => ushort.MaxValue,
            4 => int.MaxValue,
            _ => throw new InvalidOperationException("BinaryFrame length prefix size is invalid.")
        };

        if (payloadLength > maxPayloadLength)
        {
            throw new InvalidOperationException(
                $"Payload length {payloadLength} exceeds the configured BinaryFrame length prefix capacity of {maxPayloadLength}.");
        }
    }

    private void WriteLengthPrefix(Span<byte> target, int payloadLength)
    {
        switch (_lengthPrefixSizeBytes)
        {
            case 1:
                target[0] = checked((byte)payloadLength);
                return;

            case 2 when _lengthPrefixEndianness == BitFieldEndianness.BigEndian:
                BinaryPrimitives.WriteUInt16BigEndian(target, checked((ushort)payloadLength));
                return;

            case 2:
                BinaryPrimitives.WriteUInt16LittleEndian(target, checked((ushort)payloadLength));
                return;

            case 4 when _lengthPrefixEndianness == BitFieldEndianness.BigEndian:
                BinaryPrimitives.WriteInt32BigEndian(target, payloadLength);
                return;

            case 4:
                BinaryPrimitives.WriteInt32LittleEndian(target, payloadLength);
                return;
        }
    }

    private int ReadLengthPrefix(ReadOnlySpan<byte> source)
    {
        return _lengthPrefixSizeBytes switch
        {
            1 => source[0],
            2 when _lengthPrefixEndianness == BitFieldEndianness.BigEndian => BinaryPrimitives.ReadUInt16BigEndian(source),
            2 => BinaryPrimitives.ReadUInt16LittleEndian(source),
            4 when _lengthPrefixEndianness == BitFieldEndianness.BigEndian => ReadInt32Length(BinaryPrimitives.ReadInt32BigEndian(source)),
            4 => ReadInt32Length(BinaryPrimitives.ReadInt32LittleEndian(source)),
            _ => throw new InvalidOperationException("BinaryFrame length prefix size is invalid.")
        };
    }

    private static int ReadInt32Length(int value)
    {
        return value >= 0
            ? value
            : throw new InvalidOperationException("Frame length cannot be negative.");
    }

    private ReadOnlySpan<byte> GetChecksumCoverage(ReadOnlySpan<byte> frameWithoutChecksum, ReadOnlySpan<byte> payload)
    {
        return _checksumCoverage.Equals(BinaryFrameChecksumCoverageTypes.Payload, StringComparison.OrdinalIgnoreCase)
            ? payload
            : frameWithoutChecksum;
    }

    private static ushort ComputeChecksum(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0xFFFF;
        foreach (var current in bytes)
        {
            crc ^= current;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x0001) != 0
                    ? (ushort)((crc >> 1) ^ 0xA001)
                    : (ushort)(crc >> 1);
            }
        }

        return crc;
    }

    private void WriteChecksum(Span<byte> target, ushort checksum)
    {
        if (_checksumEndianness == BitFieldEndianness.BigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(target, checksum);
            return;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(target, checksum);
    }

    private ushort ReadChecksum(ReadOnlySpan<byte> source)
    {
        return _checksumEndianness == BitFieldEndianness.BigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(source)
            : BinaryPrimitives.ReadUInt16LittleEndian(source);
    }
}
