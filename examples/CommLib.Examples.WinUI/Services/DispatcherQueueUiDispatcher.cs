using Microsoft.UI.Dispatching;

namespace CommLib.Examples.WinUI.Services;

/// <summary>
/// DispatcherQueueUiDispatcher 타입입니다.
/// </summary>
public sealed class DispatcherQueueUiDispatcher(DispatcherQueue dispatcherQueue) : IUiDispatcher
{
    /// <summary>
    /// _dispatcherQueue 값을 나타냅니다.
    /// </summary>
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    /// <summary>
    /// Enqueue 작업을 수행합니다.
    /// </summary>
    public void Enqueue(Action action)
    {
        if (!_dispatcherQueue.TryEnqueue(delegate { action(); }))
        {
            action();
        }
    }
}