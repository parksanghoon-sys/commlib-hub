using CommLib.Domain.Messaging;

namespace CommLib.Domain.Protocol;

/// <summary>
/// 메시지 직렬화 동작을 정의합니다.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// 지정한 메시지를 바이너리 페이로드로 변환합니다.
    /// </summary>
    /// <param name="message">직렬화할 메시지 인스턴스입니다.</param>
    /// <returns>직렬화된 바이너리 페이로드입니다.</returns>
    byte[] Serialize(IMessage message);
}
