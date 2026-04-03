using System.Buffers.Binary;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 4바이트 길이 prefix 기반 프레임 프로토콜 구현입니다.
/// </summary>
public sealed class LengthPrefixedProtocol : IProtocol
{
    private const int HeaderSize = 4;

    /// <summary>
    /// 프로토콜 이름을 가져옵니다.
    /// </summary>
    public string Name => "LengthPrefixed";

    /// <summary>
    /// 페이로드 앞에 4바이트 big-endian 길이 값을 붙여 프레임을 생성합니다.
    /// </summary>
    /// <param name="payload">프레임에 담을 원본 페이로드입니다.</param>
    /// <returns>길이 prefix가 포함된 프레임입니다.</returns>
    public byte[] Encode(ReadOnlySpan<byte> payload)
    {
        var frame = new byte[HeaderSize + payload.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, HeaderSize), payload.Length);
        payload.CopyTo(frame.AsSpan(HeaderSize));
        return frame;
    }

    /// <summary>
    /// 입력 버퍼에서 하나의 완전한 프레임을 읽어 페이로드를 추출합니다.
    /// </summary>
    /// <param name="buffer">프레임 추출을 시도할 입력 버퍼입니다.</param>
    /// <param name="payload">추출된 페이로드입니다.</param>
    /// <param name="bytesConsumed">사용한 전체 바이트 수입니다.</param>
    /// <returns>완전한 프레임을 읽었으면 <see langword="true"/>이고, 아니면 <see langword="false"/>입니다.</returns>
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
        if (buffer.Length < frameLength)
        {
            return false;
        }

        payload = buffer.Slice(HeaderSize, payloadLength).ToArray();
        bytesConsumed = frameLength;
        return true;
    }
}
