namespace CommLib.Domain.Messaging;

/// <summary>
/// schema-backed payload compose 시점의 field 값 한 건을 표현합니다.
/// </summary>
public sealed record BitFieldFieldAssignment
{
    /// <summary>
    /// <see cref="BitFieldFieldAssignment"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="name">값을 할당할 field 이름입니다.</param>
    /// <param name="value">적용할 integer 값입니다.</param>
    public BitFieldFieldAssignment(string name, decimal value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bit field assignment name is required.", nameof(name));
        }

        Name = name;
        Value = value;
    }

    /// <summary>
    /// 값을 할당할 field 이름입니다.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// compose 시점에 field에 쓸 integer 값입니다.
    /// </summary>
    public decimal Value { get; }
}
