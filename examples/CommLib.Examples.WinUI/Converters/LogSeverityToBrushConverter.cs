using CommLib.Examples.WinUI.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CommLib.Examples.WinUI.Converters;

public sealed class LogSeverityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not LogSeverity severity)
        {
            return new SolidColorBrush(Colors.SlateGray);
        }

        return severity switch
        {
            LogSeverity.Info => new SolidColorBrush(ColorHelper.FromArgb(255, 54, 96, 146)),
            LogSeverity.Success => new SolidColorBrush(ColorHelper.FromArgb(255, 33, 112, 72)),
            LogSeverity.Warning => new SolidColorBrush(ColorHelper.FromArgb(255, 167, 92, 30)),
            LogSeverity.Error => new SolidColorBrush(ColorHelper.FromArgb(255, 160, 52, 54)),
            _ => new SolidColorBrush(Colors.SlateGray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
