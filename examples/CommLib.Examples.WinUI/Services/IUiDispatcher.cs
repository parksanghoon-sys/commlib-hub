namespace CommLib.Examples.WinUI.Services;

public interface IUiDispatcher
{
    void Enqueue(Action action);
}
