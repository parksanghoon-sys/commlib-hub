using System.Text;
using CommLib.Domain.Messaging;
using CommLib.Domain.Protocol;

namespace CommLib.Infrastructure.Protocol;

/// <summary>
/// 메시지 식별자만 텍스트로 인코딩하는 최소 직렬화기 구현을 제공합니다.
/// </summary>
public sealed class NoOpSerializer : ISerializer
{
    /// <summary>
    /// 지정한 메시지를 메시지 식별자 기반 UTF-8 바이트 배열로 직렬화합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지입니다.</param>
    /// <returns>메시지 식별자의 UTF-8 인코딩 결과입니다.</returns>
    public byte[] Serialize(IMessage message)
    {
        return Encoding.UTF8.GetBytes(message.MessageId.ToString());
    }
}
