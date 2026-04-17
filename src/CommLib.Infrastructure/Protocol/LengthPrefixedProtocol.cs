using System.Buffers.Binary;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 4바이트 big-endian 길이 prefix 프레임을 인코드하고 디코드합니다.
/// </summary>
public sealed class LengthPrefixedProtocol : IProtocol
{
    private const int HeaderSize = 4;
    private readonly int _maxFrameLength;

    /// <summary>
    /// 기본 최대 인코딩 프레임 길이입니다.
    /// </summary>
    public const int DefaultMaxFrameLength = 65536;

    /// <summary>
    /// <see cref="LengthPrefixedProtocol"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="maxFrameLength">4바이트 헤더를 포함한 최대 인코딩 프레임 길이입니다.</param>
    /// <exception cref="ArgumentOutOfRangeException">설정한 길이가 헤더를 담을 수 없을 때 발생합니다.</exception>
    public LengthPrefixedProtocol(int maxFrameLength = DefaultMaxFrameLength)
    {
        if (maxFrameLength < HeaderSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFrameLength),
                maxFrameLength,
                $"Max frame length must be at least {HeaderSize} bytes.");
        }

        _maxFrameLength = maxFrameLength;
    }

    /// <summary>
    /// 프로토콜 이름을 가져옵니다.
    /// </summary>
    public string Name => "LengthPrefixed";

    /// <summary>
    /// 현재 인스턴스가 허용하는 최대 인코딩 프레임 길이를 가져옵니다.
    /// </summary>
    public int MaxFrameLength => _maxFrameLength;

    /// <summary>
    /// 페이로드를 4바이트 big-endian 길이 prefix 프레임으로 감쌉니다.
    /// </summary>
    /// <param name="payload">인코딩할 원본 페이로드입니다.</param>
    /// <returns>인코딩된 프레임입니다.</returns>
    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        var frameLength = HeaderSize + payload.Length;
        if (frameLength > _maxFrameLength)
        {
            throw new InvalidOperationException(
                $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
        }

        var frame = new byte[frameLength];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, HeaderSize), payload.Length);
        payload.CopyTo(frame.AsSpan(HeaderSize));
        return frame;
    }

    /// <summary>
    /// 입력 버퍼에서 완전한 페이로드 하나를 디코드합니다.
    /// </summary>
    /// <param name="buffer">입력 버퍼입니다.</param>
    /// <param name="payload">디코드된 페이로드입니다.</param>
    /// <param name="bytesConsumed">소비한 전체 바이트 수입니다.</param>
    /// <returns>완전한 프레임을 디코드했으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
    public bool TryDecode(ReadOnlySpan<byte> buffer, out byte[] payload, out int bytesConsumed)
    {
        payload = Array.Empty<byte>();
        bytesConsumed = 0;

        if (buffer.Length < HeaderSize)
        {
            return false;
        }

        var payloadLength = BinaryPrimitives.ReadInt32BigEndian(buffer[..HeaderSize]);
        if (payloadLength < 0)
        {
            throw new InvalidOperationException("Frame length cannot be negative.");
        }

        var frameLength = HeaderSize + payloadLength;
        if (frameLength > _maxFrameLength)
        {
            throw new InvalidOperationException(
                $"Frame length {frameLength} exceeds the configured maximum of {_maxFrameLength}.");
        }

        if (buffer.Length < frameLength)
        {
            return false;
        }

        payload = buffer.Slice(HeaderSize, payloadLength).ToArray();
        bytesConsumed = frameLength;
        return true;
    }
}
