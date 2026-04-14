using System.Globalization;
using System.Text;

namespace CommLib.Domain.Messaging;

/// <summary>
/// 메시지 본문을 사용자 표시용 문자열로 정규화하는 도우미입니다.
/// </summary>
public static class MessagePayloadFormatter
{
    /// <summary>
    /// 텍스트 본문은 그대로 반환하고, binary payload는 공백 구분 대문자 hex로 변환합니다.
    /// </summary>
    /// <param name="message">표시 문자열로 바꿀 메시지입니다.</param>
    /// <returns>사용자 표시용 본문 문자열입니다.</returns>
    public static string FormatBody(IMessage message)
    {
        if (message is IMessageBody bodyMessage)
        {
            return bodyMessage.Body;
        }

        if (message is IBinaryMessagePayload binaryPayload)
        {
            return FormatHex(binaryPayload.Payload.Span);
        }

        return string.Empty;
    }

    /// <summary>
    /// Tries to inspect a binary payload with a bitfield schema and returns a compact field summary for logs.
    /// </summary>
    /// <param name="message">The message whose payload should be inspected.</param>
    /// <param name="schema">The optional schema to apply to the payload.</param>
    /// <param name="summary">The formatted field summary when inspection succeeds.</param>
    /// <param name="error">The inspection error when schema inspection fails.</param>
    /// <returns><see langword="true"/> when a schema summary was produced; otherwise <see langword="false"/>.</returns>
    public static bool TryFormatBitFieldSummary(
        IMessage message,
        BitFieldPayloadSchema? schema,
        out string? summary,
        out string? error)
    {
        summary = null;
        error = null;

        if (schema is null || message is not IBinaryMessagePayload binaryPayload)
        {
            return false;
        }

        try
        {
            var values = BitFieldPayloadSchemaCodec.Inspect(schema, binaryPayload.Payload.Span);
            if (values.Count == 0)
            {
                return false;
            }

            summary = FormatBitFieldValues(values);
            return true;
        }
        catch (InvalidOperationException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// raw binary payload를 공백 구분 대문자 hex 문자열로 변환합니다.
    /// </summary>
    /// <param name="payload">변환할 payload 바이트입니다.</param>
    /// <returns>공백 구분 대문자 hex 문자열입니다.</returns>
    public static string FormatHex(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return string.Empty;
        }

        var hex = Convert.ToHexString(payload);
        var builder = new StringBuilder((payload.Length * 3) - 1);

        for (var index = 0; index < payload.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(hex, index * 2, 2);
        }

        return builder.ToString();
    }

    /// <summary>
    /// FormatBitFieldValues 작업을 수행합니다.
    /// </summary>
    private static string FormatBitFieldValues(IReadOnlyList<BitFieldFieldValue> values)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            var value = values[index];
            builder.Append(value.Field.Name);
            builder.Append('=');
            builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
