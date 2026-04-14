using CommLib.Examples.WinUI.Models;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// IAppLocalizer 계약을 정의하는 인터페이스입니다.
/// </summary>
public interface IAppLocalizer
{
    /// <summary>
    /// 언어 변경 시 발생하는 이벤트입니다.
    /// </summary>
    event EventHandler? LanguageChanged;

    /// <summary>
    /// 현재 선택된 언어를 가져오거나 설정합니다.
    /// </summary>
    AppLanguageMode CurrentLanguage { get; set; }

    /// <summary>
    /// 지정한 키에 대응하는 지역화 문자열을 반환합니다.
    /// </summary>
    string Get(string key);

    /// <summary>
    /// 지정한 키의 지역화 문자열에 인자를 적용해 반환합니다.
    /// </summary>
    string Format(string key, params object?[] args);

    /// <summary>
    /// 셸 페이지의 표시 이름을 반환합니다.
    /// </summary>
    string GetShellPageLabel(ShellPageKind kind);

    /// <summary>
    /// 셸 페이지의 보조 설명을 반환합니다.
    /// </summary>
    string GetShellPageSubtitle(ShellPageKind kind);

    /// <summary>
    /// 전송 방식 표시 이름을 반환합니다.
    /// </summary>
    string GetTransportLabel(TransportKind kind);

    /// <summary>
    /// 전송 방식 보조 설명을 반환합니다.
    /// </summary>
    string GetTransportSubtitle(TransportKind kind);

    /// <summary>
    /// 직렬화기 표시 이름을 반환합니다.
    /// </summary>
    string GetSerializerLabel(string serializerType);

    /// <summary>
    /// 직렬화기 보조 설명을 반환합니다.
    /// </summary>
    string GetSerializerSubtitle(string serializerType);

    /// <summary>
    /// 언어 모드 표시 이름을 반환합니다.
    /// </summary>
    string GetLanguageLabel(AppLanguageMode mode);
}
