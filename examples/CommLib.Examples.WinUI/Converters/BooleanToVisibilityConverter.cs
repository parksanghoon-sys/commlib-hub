using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CommLib.Examples.WinUI.Converters;

/// <summary>
/// BooleanToVisibilityConverter 타입입니다.
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Convert 작업을 수행합니다.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// ConvertBack 작업을 수행합니다.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}