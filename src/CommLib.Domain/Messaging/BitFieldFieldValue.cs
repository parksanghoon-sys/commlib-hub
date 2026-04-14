namespace CommLib.Domain.Messaging;

/// <summary>
/// schema 기반 inspect 결과로 읽어낸 field 값 한 건입니다.
/// </summary>
public sealed record BitFieldFieldValue
{
    /// <summary>
    /// <see cref="BitFieldFieldValue"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="field">값을 읽어낸 field 정의입니다.</param>
    /// <param name="scalarKind">field 값을 어떤 방식으로 해석했는지 나타냅니다.</param>
    /// <param name="value">해석된 integer 값입니다.</param>
    public BitFieldFieldValue(BitFieldDefinition field, BitFieldScalarKind scalarKind, decimal value)
    {
        ArgumentNullException.ThrowIfNull(field);

        Field = field;
        ScalarKind = scalarKind;
        Value = value;
    }

    /// <summary>
    /// inspect 결과의 대상 field 정의입니다.
    /// </summary>
    public BitFieldDefinition Field { get; }

    /// <summary>
    /// 값을 unsigned/signed 중 어떤 방식으로 해석했는지 나타냅니다.
    /// </summary>
    public BitFieldScalarKind ScalarKind { get; }

    /// <summary>
    /// field에서 읽어낸 integer 값입니다.
    /// </summary>
    public decimal Value { get; }
}
