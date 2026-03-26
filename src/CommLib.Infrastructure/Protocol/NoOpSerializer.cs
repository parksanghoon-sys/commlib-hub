using System.Globalization;
using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 메시지 식별자만 텍스트로 인코드하는 최소 serializer 구현입니다.
/// </summary>
public sealed class NoOpSerializer : ISerializer
{
    /// <summary>
    /// 지정한 메시지를 메시지 식별자 기반 UTF-8 바이트 배열로 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>메시지 식별자의 UTF-8 인코드 결과입니다.</returns>
    public byte[] Serialize(IMessage message)
    {
        return Encoding.UTF8.GetBytes(message.MessageId.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// UTF-8 문자열로 인코드된 메시지 식별자를 메시지로 복원합니다.
    /// </summary>
    /// <param name="payload">역직렬화할 바이너리 페이로드입니다.</param>
    /// <returns>식별자만 담은 메시지입니다.</returns>
    public IMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        var messageIdText = Encoding.UTF8.GetString(payload);
        if (!ushort.TryParse(messageIdText, NumberStyles.None, CultureInfo.InvariantCulture, out var messageId))
        {
            throw new InvalidOperationException("Payload does not contain a valid message id.");
        }

        return new DeserializedMessage(messageId);
    }

    private sealed record DeserializedMessage(ushort MessageId) : IMessage;
}
