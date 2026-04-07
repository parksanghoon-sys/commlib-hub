namespace CommLib.Domain.Messaging;

/// <summary>
/// raw payload 안의 bitfield 한 구간을 설명하는 정의입니다.
/// </summary>
public sealed record BitFieldDefinition
{
    /// <summary>
    /// <see cref="BitFieldDefinition"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="name">필드 이름입니다.</param>
    /// <param name="bitOffset">payload 시작점 기준 bit offset입니다. bit 0은 첫 번째 byte의 LSB입니다.</param>
    /// <param name="bitLength">필드 bit 길이입니다. 현재 최대 64 bit까지 지원합니다.</param>
    public BitFieldDefinition(string name, int bitOffset, int bitLength)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bit field name is required.", nameof(name));
        }

        if (bitOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitOffset), "Bit field offset must be greater than or equal to 0.");
        }

        if (bitLength is <= 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitLength), "Bit field length must be between 1 and 64 bits.");
        }

        Name = name;
        BitOffset = bitOffset;
        BitLength = bitLength;
    }

    /// <summary>
    /// 필드 이름입니다.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// payload 시작점 기준 bit offset입니다. bit 0은 첫 번째 byte의 LSB입니다.
    /// </summary>
    public int BitOffset { get; }

    /// <summary>
    /// 필드 bit 길이입니다.
    /// </summary>
    public int BitLength { get; }
}
