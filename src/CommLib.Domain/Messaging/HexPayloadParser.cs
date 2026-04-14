using System.Text;

namespace CommLib.Domain.Messaging;

/// <summary>
/// 공백 허용 hex 텍스트를 raw payload 바이트로 변환하는 도우미입니다.
/// </summary>
public static class HexPayloadParser
{
    /// <summary>
    /// 공백을 무시하고 hex byte pair 텍스트를 byte 배열로 변환합니다.
    /// </summary>
    /// <param name="text">변환할 hex 텍스트입니다.</param>
    /// <returns>변환된 payload 바이트 배열입니다.</returns>
    public static byte[] Parse(string text)
    {
        var trimmed = RemoveWhitespace(text);
        if (string.IsNullOrEmpty(trimmed))
        {
            return [];
        }

        return Convert.FromHexString(trimmed);
    }

    /// <summary>
    /// RemoveWhitespace 작업을 수행합니다.
    /// </summary>
    private static string RemoveWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
