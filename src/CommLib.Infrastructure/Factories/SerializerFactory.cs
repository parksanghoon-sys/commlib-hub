using CommLib.Domain.Configuration;
using CommLib.Domain.Protocol;
using CommLib.Infrastructure.Protocol;

namespace CommLib.Infrastructure.Factories;

/// <summary>
/// serializer 설정 형식에 따라 serializer 구현을 생성합니다.
/// </summary>
public sealed class SerializerFactory : ISerializerFactory
{
    /// <summary>
    /// 지정한 serializer 설정에 맞는 구현을 생성합니다.
    /// </summary>
    /// <param name="options">생성할 serializer 설정입니다.</param>
    /// <returns>설정 형식에 맞는 serializer 구현입니다.</returns>
    public ISerializer Create(SerializerOptions options)
    {
        return options.Type switch
        {
            SerializerTypes.AutoBinary => new NoOpSerializer(),
            SerializerTypes.RawHex => new RawHexSerializer(),
            _ => throw new NotSupportedException($"Unsupported serializer: {options.Type}")
        };
    }
}
