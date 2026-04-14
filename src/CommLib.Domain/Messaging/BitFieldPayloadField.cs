namespace CommLib.Domain.Messaging;

/// <summary>
/// payload schema 안의 하나의 named bitfield를 정의합니다.
/// </summary>
public sealed class BitFieldPayloadField
{
    /// <summary>
    /// 필드 이름입니다.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// payload 시작 기준 bit offset입니다. bit 0은 첫 번째 byte의 LSB입니다.
    /// </summary>
    public int BitOffset { get; init; }

    /// <summary>
    /// 필드 bit 길이입니다.
    /// </summary>
    public int BitLength { get; init; }

    /// <summary>
    /// 이 필드를 unsigned/signed 중 어떤 방식으로 읽고 쓸지 지정합니다.
    /// </summary>
    public BitFieldScalarKind ScalarKind { get; init; } = BitFieldScalarKind.Unsigned;

    /// <summary>
    /// low-level bit codec가 사용하는 <see cref="BitFieldDefinition"/>으로 변환합니다.
    /// </summary>
    /// <returns>검증 가능한 <see cref="BitFieldDefinition"/> 인스턴스입니다.</returns>
    public BitFieldDefinition ToDefinition() => new(Name, BitOffset, BitLength);
}
