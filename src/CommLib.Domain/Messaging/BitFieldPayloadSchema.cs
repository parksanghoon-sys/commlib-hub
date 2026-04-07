using System;
using System.Collections.Generic;

namespace CommLib.Domain.Messaging;

/// <summary>
/// raw payload의 고정 길이와 named bitfield 목록을 함께 정의하는 schema입니다.
/// </summary>
public sealed class BitFieldPayloadSchema
{
    /// <summary>
    /// schema가 적용되는 payload 총 byte 길이입니다.
    /// </summary>
    public int PayloadLengthBytes { get; init; }

    /// <summary>
    /// payload에 배치된 bitfield 목록입니다.
    /// </summary>
    public IReadOnlyList<BitFieldPayloadField> Fields { get; init; } = Array.Empty<BitFieldPayloadField>();
}
