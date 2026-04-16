namespace CommLib.Domain.Messaging;

/// <summary>
/// Controls the byte order used for multi-byte bitfield values.
/// </summary>
public enum BitFieldEndianness
{
    /// <summary>
    /// The lowest-addressed byte is the least-significant byte.
    /// </summary>
    LittleEndian = 0,

    /// <summary>
    /// The lowest-addressed byte is the most-significant byte.
    /// </summary>
    BigEndian = 1
}
