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
    /// <summary>
    /// Separator 상수입니다.
    /// </summary>
    private const char Separator = '|';

    /// <summary>
    /// 지정한 메시지를 UTF-8 payload로 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>메시지 payload 바이트 배열입니다.</returns>
    public byte[] Serialize(IMessage message)
    {
        var encodedBody = TryEncodeBody(message);
        var payload = message switch
        {
            IResponseMessage response => string.Join(
                Separator,
                "response",
                message.MessageId.ToString(CultureInfo.InvariantCulture),
                response.CorrelationId.ToString("D", CultureInfo.InvariantCulture),
                response.IsSuccess ? "1" : "0",
                encodedBody),
            IRequestMessage request => string.Join(
                Separator,
                "request",
                message.MessageId.ToString(CultureInfo.InvariantCulture),
                request.CorrelationId.ToString("D", CultureInfo.InvariantCulture),
                encodedBody),
            _ => string.Join(
                Separator,
                "message",
                message.MessageId.ToString(CultureInfo.InvariantCulture),
                encodedBody)
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
            "message" when parts.Length is 2 or 3 && TryParseBody(parts, 2, out var messageBody)
                => new MessageModel(messageId, messageBody),
            "request" when parts.Length is 3 or 4 &&
                            TryParseCorrelationId(parts[2], out var requestCorrelationId) &&
                            TryParseBody(parts, 3, out var requestBody)
                => new RequestMessageModel(messageId, requestCorrelationId, requestBody),
            "response" when parts.Length is 4 or 5 &&
                                   TryParseCorrelationId(parts[2], out var responseCorrelationId) &&
                                   TryParseSuccess(parts[3], out var isSuccess) &&
                                   TryParseBody(parts, 4, out var responseBody)
                => new ResponseMessageModel(messageId, responseCorrelationId, isSuccess, responseBody),
            _ => throw new InvalidOperationException("Payload does not contain a supported message shape.")
        };
    }

    /// <summary>
    /// TryEncodeBody 작업을 수행합니다.
    /// </summary>
    private static string TryEncodeBody(IMessage message)
    {
        if (message is not IMessageBody bodyMessage)
        {
            return string.Empty;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyMessage.Body));
    }

    /// <summary>
    /// TryParseBody 작업을 수행합니다.
    /// </summary>
    private static bool TryParseBody(string[] parts, int index, out string body)
    {
        if (parts.Length <= index)
        {
            body = string.Empty;
            return true;
        }

        if (string.IsNullOrEmpty(parts[index]))
        {
            body = string.Empty;
            return true;
        }

        try
        {
            body = Encoding.UTF8.GetString(Convert.FromBase64String(parts[index]));
            return true;
        }
        catch (FormatException)
        {
            body = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// TryParseCorrelationId 작업을 수행합니다.
    /// </summary>
    private static bool TryParseCorrelationId(string text, out Guid correlationId)
    {
        return Guid.TryParseExact(text, "D", out correlationId);
    }

    /// <summary>
    /// TryParseSuccess 작업을 수행합니다.
    /// </summary>
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
