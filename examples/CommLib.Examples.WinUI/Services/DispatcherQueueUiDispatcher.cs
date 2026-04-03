using Microsoft.UI.Dispatching;

namespace CommLib.Examples.WinUI.Services;

public sealed class DispatcherQueueUiDispatcher(DispatcherQueue dispatcherQueue) : IUiDispatcher
{
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    public void Enqueue(Action action)
    {
        if (!_dispatcherQueue.TryEnqueue(delegate { action(); }))
        {
            action();
        }
    }
}
