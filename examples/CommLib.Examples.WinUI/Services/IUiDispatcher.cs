namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// IUiDispatcher 계약을 정의하는 인터페이스입니다.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>
    /// 지정한 작업을 UI 스레드 큐에 등록합니다.
    /// </summary>
    void Enqueue(Action action);
}
