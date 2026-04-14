namespace CommLib.Domain.Configuration;

/// <summary>
/// 저장소와 예제 앱 전반에서 공용으로 사용하는 serializer 식별자 모음입니다.
/// </summary>
public static class SerializerTypes
{
    /// <summary>
    /// 텍스트 본문을 기존 예제 serializer 형식으로 처리합니다.
    /// </summary>
    public const string AutoBinary = "AutoBinary";

    /// <summary>
    /// payload를 raw hexadecimal 바이트로 해석합니다.
    /// </summary>
    public const string RawHex = "RawHex";
}
