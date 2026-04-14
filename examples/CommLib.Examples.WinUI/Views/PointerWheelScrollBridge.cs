using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace CommLib.Examples.WinUI.Views;

/// <summary>
/// PointerWheelScrollBridge 타입입니다.
/// </summary>
internal static class PointerWheelScrollBridge
{
    /// <summary>
    /// Attach 작업을 수행합니다.
    /// </summary>
    public static void Attach(TextBox textBox, ScrollViewer parentScrollViewer)
    {
        // 중첩 TextBox는 마우스 휠을 내부에서 먼저 소비하는 경우가 많아서
        // handled 된 이벤트까지 받아 부모 ScrollViewer로 다시 전달할 수 있게 등록한다.
        textBox.AddHandler(
            UIElement.PointerWheelChangedEvent,
            new PointerEventHandler((_, args) => ForwardToParentScrollViewer(parentScrollViewer, args)),
            handledEventsToo: true);
    }

    /// <summary>
    /// ForwardToParentScrollViewer 작업을 수행합니다.
    /// </summary>
    private static void ForwardToParentScrollViewer(ScrollViewer parentScrollViewer, PointerRoutedEventArgs args)
    {
        if (parentScrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        // WinUI에서 mouse-wheel chaining은 touch 조작만큼 일관되지 않아서
        // 현재 포인터 delta를 기준으로 부모 스크롤러의 새 오프셋을 직접 계산한다.
        var point = args.GetCurrentPoint(parentScrollViewer);
        var targetOffset = Math.Clamp(
            parentScrollViewer.VerticalOffset - point.Properties.MouseWheelDelta,
            0,
            parentScrollViewer.ScrollableHeight);

        if (Math.Abs(targetOffset - parentScrollViewer.VerticalOffset) < 0.5)
        {
            return;
        }

        parentScrollViewer.ChangeView(null, targetOffset, null, true);
        args.Handled = true;
    }
}