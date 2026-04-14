namespace CommLib.Domain.Messaging;

/// <summary>
/// bitfield scalar 값을 schema에서 어떤 방식으로 해석할지 지정합니다.
/// </summary>
public enum BitFieldScalarKind
{
    /// <summary>
    /// 부호 없는 unsigned 정수로 해석합니다.
    /// </summary>
    Unsigned = 0,

    /// <summary>
    /// two's complement signed 정수로 해석합니다.
    /// </summary>
    Signed = 1
}
