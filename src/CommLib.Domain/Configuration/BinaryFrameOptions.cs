using CommLib.Domain.Messaging;

namespace CommLib.Domain.Configuration;

/// <summary>
/// 설정 기반 binary frame protocol의 frame envelope 구성을 나타냅니다.
/// </summary>
public sealed class BinaryFrameOptions
{
    /// <summary>
    /// frame 시작을 식별하는 선택적 hex byte sequence입니다. 예: <c>AA 55</c>.
    /// </summary>
    public string? StartHex { get; init; }

    /// <summary>
    /// payload 길이를 담는 length prefix 설정입니다.
    /// </summary>
    public BinaryFrameLengthPrefixOptions LengthPrefix { get; init; } = new();

    /// <summary>
    /// frame 무결성 확인에 사용하는 checksum 설정입니다.
    /// </summary>
    public BinaryFrameChecksumOptions Checksum { get; init; } = new();
}

/// <summary>
/// binary frame의 payload length prefix 표현 방식을 나타냅니다.
/// </summary>
public sealed class BinaryFrameLengthPrefixOptions
{
    /// <summary>
    /// length prefix byte 수입니다. 현재 <c>1</c>, <c>2</c>, <c>4</c>만 지원합니다.
    /// </summary>
    public int SizeBytes { get; init; } = 2;

    /// <summary>
    /// length prefix byte order입니다.
    /// </summary>
    public BitFieldEndianness Endianness { get; init; } = BitFieldEndianness.BigEndian;
}

/// <summary>
/// binary frame checksum 계산 방식을 나타냅니다.
/// </summary>
public sealed class BinaryFrameChecksumOptions
{
    /// <summary>
    /// checksum 알고리즘 이름입니다.
    /// </summary>
    public string Type { get; init; } = BinaryFrameChecksumTypes.None;

    /// <summary>
    /// checksum 값을 frame에 기록할 때 사용할 byte order입니다.
    /// </summary>
    public BitFieldEndianness Endianness { get; init; } = BitFieldEndianness.LittleEndian;

    /// <summary>
    /// checksum을 계산할 byte 범위입니다.
    /// </summary>
    public string Coverage { get; init; } = BinaryFrameChecksumCoverageTypes.FrameWithoutChecksum;
}

/// <summary>
/// binary frame checksum 알고리즘 이름 상수입니다.
/// </summary>
public static class BinaryFrameChecksumTypes
{
    /// <summary>
    /// checksum을 사용하지 않습니다.
    /// </summary>
    public const string None = "None";

    /// <summary>
    /// Modbus 계열 장비에서 널리 쓰는 CRC16/Modbus checksum입니다.
    /// </summary>
    public const string Crc16Modbus = "Crc16Modbus";
}

/// <summary>
/// binary frame checksum 계산 범위 이름 상수입니다.
/// </summary>
public static class BinaryFrameChecksumCoverageTypes
{
    /// <summary>
    /// checksum field를 제외한 frame 전체를 checksum 대상으로 사용합니다.
    /// </summary>
    public const string FrameWithoutChecksum = "FrameWithoutChecksum";

    /// <summary>
    /// payload byte만 checksum 대상으로 사용합니다.
    /// </summary>
    public const string Payload = "Payload";
}
