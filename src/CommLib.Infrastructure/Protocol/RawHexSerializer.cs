using System.Globalization;
using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 메시지 메타데이터 헤더 뒤에 raw binary payload를 그대로 붙여 직렬화하는 serializer입니다.
/// </summary>
public sealed class RawHexSerializer : ISerializer, ISpanSerializer
{
    private const char Separator = '|';
    private const byte SeparatorByte = (byte)Separator;

    /// <summary>
    /// binary payload 또는 hex text body를 raw payload로 변환해 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>헤더와 raw payload가 결합된 바이트 배열입니다.</returns>
    public byte[] Serialize(IMessage message)
    {
        var serialized = new byte[GetSerializedLength(message)];
        Serialize(message, serialized);
        return serialized;
    }

    /// <summary>
    /// message metadata header와 raw payload를 합친 직렬화 결과 길이를 계산합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>필요한 destination byte 길이입니다.</returns>
    public int GetSerializedLength(IMessage message)
    {
        return Encoding.ASCII.GetByteCount(CreateHeaderText(message)) + GetPayloadLength(message);
    }

    /// <summary>
    /// 별도 payload 배열을 만들지 않고 caller가 제공한 destination에 header와 raw payload를 순서대로 씁니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <param name="destination">정확한 길이로 준비된 출력 span입니다.</param>
    public void Serialize(IMessage message, Span<byte> destination)
    {
        var headerText = CreateHeaderText(message);
        var headerLength = Encoding.ASCII.GetBytes(headerText, destination);
        WritePayload(message, destination[headerLength..]);
    }

    /// <summary>
    /// 헤더와 raw payload가 결합된 바이트 배열을 binary-capable 메시지로 복원합니다.
    /// </summary>
    /// <param name="payload">역직렬화할 바이트 배열입니다.</param>
    /// <returns>복원된 binary 메시지입니다.</returns>
    public IMessage Deserialize(ReadOnlySpan<byte> payload)
    {
        var index = 0;
        var shape = ReadToken(payload, ref index, "message shape");
        var messageIdToken = ReadToken(payload, ref index, "message id");
        if (!ushort.TryParse(messageIdToken, NumberStyles.None, CultureInfo.InvariantCulture, out var messageId))
        {
            throw new InvalidOperationException("Payload does not contain a valid message id.");
        }

        return shape switch
        {
            "message" => new BinaryMessageModel(messageId, payload[index..].ToArray()),
            "request" => DeserializeRequest(payload, index, messageId),
            "response" => DeserializeResponse(payload, index, messageId),
            _ => throw new InvalidOperationException("Payload does not contain a supported message shape.")
        };
    }

    private static string CreateHeaderText(IMessage message)
    {
        return message switch
        {
            IResponseMessage response => string.Create(
                CultureInfo.InvariantCulture,
                $"response|{message.MessageId}|{response.CorrelationId:D}|{(response.IsSuccess ? "1" : "0")}|"),
            IRequestMessage request => string.Create(
                CultureInfo.InvariantCulture,
                $"request|{message.MessageId}|{request.CorrelationId:D}|"),
            _ => string.Create(
                CultureInfo.InvariantCulture,
                $"message|{message.MessageId}|")
        };
    }

    private static int GetPayloadLength(IMessage message)
    {
        if (message is IBinaryMessagePayload binaryPayload)
        {
            return binaryPayload.Payload.Length;
        }

        if (message is IMessageBody bodyMessage)
        {
            return ParseBodyPayload(bodyMessage).Length;
        }

        return 0;
    }

    private static void WritePayload(IMessage message, Span<byte> destination)
    {
        if (message is IBinaryMessagePayload binaryPayload)
        {
            binaryPayload.Payload.Span.CopyTo(destination);
            return;
        }

        if (message is IMessageBody bodyMessage)
        {
            ParseBodyPayload(bodyMessage).CopyTo(destination);
            return;
        }
    }

    private static byte[] ParseBodyPayload(IMessageBody bodyMessage)
    {
        try
        {
            return HexPayloadParser.Parse(bodyMessage.Body);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Message body must be valid hexadecimal text.", exception);
        }
    }

    private static IMessage DeserializeRequest(ReadOnlySpan<byte> payload, int index, ushort messageId)
    {
        var correlationToken = ReadToken(payload, ref index, "correlation id");
        if (!Guid.TryParseExact(correlationToken, "D", out var correlationId))
        {
            throw new InvalidOperationException("Payload does not contain a valid correlation id.");
        }

        return new BinaryRequestMessageModel(messageId, correlationId, payload[index..].ToArray());
    }

    private static IMessage DeserializeResponse(ReadOnlySpan<byte> payload, int index, ushort messageId)
    {
        var correlationToken = ReadToken(payload, ref index, "correlation id");
        if (!Guid.TryParseExact(correlationToken, "D", out var correlationId))
        {
            throw new InvalidOperationException("Payload does not contain a valid correlation id.");
        }

        var successToken = ReadToken(payload, ref index, "success flag");
        var isSuccess = successToken switch
        {
            "1" => true,
            "0" => false,
            _ => throw new InvalidOperationException("Payload does not contain a valid success flag.")
        };

        return new BinaryResponseMessageModel(messageId, correlationId, isSuccess, payload[index..].ToArray());
    }

    private static string ReadToken(ReadOnlySpan<byte> payload, ref int index, string fieldName)
    {
        var remaining = payload[index..];
        var separatorIndex = remaining.IndexOf(SeparatorByte);
        if (separatorIndex < 0)
        {
            throw new InvalidOperationException($"Payload does not contain a valid {fieldName}.");
        }

        var token = Encoding.ASCII.GetString(remaining[..separatorIndex]);
        index += separatorIndex + 1;
        return token;
    }
}
