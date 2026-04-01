using System.Globalization;
using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 메시지 식별자와 기본 상관관계 정보를 텍스트로 인코드하는 최소 serializer 구현입니다.
/// </summary>
public sealed class NoOpSerializer : ISerializer
{
    private const char Separator = '|';

    /// <summary>
    /// 지정한 메시지를 UTF-8 payload로 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>메시지 payload 바이트 배열입니다.</returns>
    public byte[] Serialize(IMessage message)
    {
        var payload = message switch
        {
            IResponseMessage response => string.Join(
                Separator,
                "response",
                message.MessageId.ToString(CultureInfo.InvariantCulture),
                response.CorrelationId.ToString("D", CultureInfo.InvariantCulture),
                response.IsSuccess ? "1" : "0"),
            IRequestMessage request => string.Join(
                Separator,
                "request",
                message.MessageId.ToString(CultureInfo.InvariantCulture),
                request.CorrelationId.ToString("D", CultureInfo.InvariantCulture)),
            _ => string.Join(
                Separator,
                "message",
                message.MessageId.ToString(CultureInfo.InvariantCulture))
        };

        return Encoding.UTF8.GetBytes(payload);
    }

    /// <summary>
    /// UTF-8 payload를 메시지로 복원합니다.
    /// </summary>
    /// <param name="payload">역직렬화할 payload입니다.</param>
    /// <returns>복원된 메시지입니다.</returns>
    public IMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        var parts = Encoding.UTF8.GetString(payload).Split(Separator);
        if (parts.Length < 2 ||
            !ushort.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var messageId))
        {
            throw new InvalidOperationException("Payload does not contain a valid message id.");
        }

        return parts[0] switch
        {
            "message" when parts.Length == 2 => new DeserializedMessage(messageId),
            "request" when parts.Length == 3 && TryParseCorrelationId(parts[2], out var requestCorrelationId)
                => new DeserializedRequestMessage(messageId, requestCorrelationId),
            "response" when parts.Length == 4 &&
                                   TryParseCorrelationId(parts[2], out var responseCorrelationId) &&
                                   TryParseSuccess(parts[3], out var isSuccess)
                => new DeserializedResponseMessage(messageId, responseCorrelationId, isSuccess),
            _ => throw new InvalidOperationException("Payload does not contain a supported message shape.")
        };
    }

    private sealed record DeserializedMessage(ushort MessageId) : IMessage;
    private sealed record DeserializedRequestMessage(ushort MessageId, Guid CorrelationId) : IRequestMessage;
    private sealed record DeserializedResponseMessage(ushort MessageId, Guid CorrelationId, bool IsSuccess) : IResponseMessage;

    private static bool TryParseCorrelationId(string text, out Guid correlationId)
    {
        return Guid.TryParseExact(text, "D", out correlationId);
    }

    private static bool TryParseSuccess(string text, out bool isSuccess)
    {
        if (text == "1")
        {
            isSuccess = true;
            return true;
        }

        if (text == "0")
        {
            isSuccess = false;
            return true;
        }

        isSuccess = false;
        return false;
    }
}
