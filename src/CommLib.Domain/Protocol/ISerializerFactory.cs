using CommLib.Domain.Configuration;

namespace CommLib.Domain.Protocol;

/// <summary>
/// 검증된 serializer 설정으로부터 serializer 구현을 생성합니다.
/// </summary>
public interface ISerializerFactory
{
    /// <summary>
    /// 지정한 설정에 맞는 serializer 구현을 생성합니다.
    /// </summary>
    /// <param name="options">검증된 serializer 설정입니다.</param>
    /// <returns>설정 형식에 맞는 serializer 구현입니다.</returns>
    ISerializer Create(SerializerOptions options);
}
