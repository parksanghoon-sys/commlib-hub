using System.Globalization;
using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 메시지 메타데이터 헤더 뒤에 raw binary payload를 그대로 붙여 직렬화하는 serializer입니다.
/// </summary>
public sealed class RawHexSerializer : ISerializer
{
    /// <summary>
    /// Separator 상수입니다.
    /// </summary>
    private const char Separator = '|';
    /// <summary>
    /// SeparatorByte 상수입니다.
    /// </summary>
    private const byte SeparatorByte = (byte)Separator;

    /// <summary>
    /// binary payload 또는 hex text body를 raw payload로 변환해 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>헤더와 raw payload가 결합된 바이트 배열입니다.</returns>
    public byte[] Serialize(IMessage message)
    {
        var header = CreateHeader(message);
        var payload = ExtractPayload(message);
        var serialized = new byte[header.Length + payload.Length];
        header.CopyTo(serialized, 0);
        payload.CopyTo(serialized.AsSpan(header.Length));
        return serialized;
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

    /// <summary>
    /// CreateHeader 작업을 수행합니다.
    /// </summary>
    private static byte[] CreateHeader(IMessage message)
    {
        var header = message switch
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

        return Encoding.ASCII.GetBytes(header);
    }

    /// <summary>
    /// ExtractPayload 작업을 수행합니다.
    /// </summary>
    private static byte[] ExtractPayload(IMessage message)
    {
        if (message is IBinaryMessagePayload binaryPayload)
        {
            return binaryPayload.Payload.ToArray();
        }

        if (message is IMessageBody bodyMessage)
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

        return [];
    }

    /// <summary>
    /// DeserializeRequest 작업을 수행합니다.
    /// </summary>
    private static IMessage DeserializeRequest(ReadOnlySpan<byte> payload, int index, ushort messageId)
    {
        var correlationToken = ReadToken(payload, ref index, "correlation id");
        if (!Guid.TryParseExact(correlationToken, "D", out var correlationId))
        {
            throw new InvalidOperationException("Payload does not contain a valid correlation id.");
        }

        return new BinaryRequestMessageModel(messageId, correlationId, payload[index..].ToArray());
    }

    /// <summary>
    /// DeserializeResponse 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// ReadToken 작업을 수행합니다.
    /// </summary>
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
