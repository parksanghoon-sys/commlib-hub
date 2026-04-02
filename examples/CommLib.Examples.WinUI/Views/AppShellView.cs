using CommLib.Examples.WinUI.Converters;
using CommLib.Examples.WinUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CommLib.Examples.WinUI.Views;

public sealed class AppShellView : Grid
{
    private readonly BooleanToVisibilityConverter _boolToVisibility = new();

    public AppShellView(
        ShellViewModel viewModel,
        DeviceLabView deviceLabView,
        SettingsView settingsView)
    {
        ViewModel = viewModel;
        DeviceLabView = deviceLabView;
        SettingsView = settingsView;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContext = ViewModel;
        Children.Add(BuildContent());
    }

    public ShellViewModel ViewModel { get; }

    public DeviceLabView DeviceLabView { get; }

    public SettingsView SettingsView { get; }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = CreateSolid("#FFF3F7FB")
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Border
        {
            Margin = new Thickness(20, 20, 20, 0),
            Padding = new Thickness(20),
            Background = CreateSolid("#FF123B5A"),
            CornerRadius = new CornerRadius(20)
        };

        var headerGrid = new Grid { ColumnSpacing = 16 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Spacing = 4 };
        titleStack.Children.Add(new TextBlock
        {
            Text = "CommLib Control Center",
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FFFFFFFF")
        });

        var pageTitle = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FFE7F4FF")
        };
        Bind(pageTitle, TextBlock.TextProperty, "CurrentPageTitle");
        titleStack.Children.Add(pageTitle);

        var subtitle = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = CreateSolid("#FFD4E7F5"),
            MaxWidth = 720
        };
        Bind(subtitle, TextBlock.TextProperty, "CurrentPageSubtitle");
        titleStack.Children.Add(subtitle);
        headerGrid.Children.Add(titleStack);

        var nav = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        nav.Children.Add(CreateHeaderButton("Device Lab", "OpenDeviceLabPageCommand"));
        nav.Children.Add(CreateHeaderButton("Settings", "OpenSettingsPageCommand"));
        Grid.SetColumn(nav, 1);
        headerGrid.Children.Add(nav);

        header.Child = headerGrid;
        root.Children.Add(header);

        var contentHost = new Grid
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(contentHost, 1);

        var deviceLabHost = new Grid();
        deviceLabHost.Children.Add(DeviceLabView);
        Bind(deviceLabHost, UIElement.VisibilityProperty, "IsDeviceLabSelected", converter: _boolToVisibility);

        var settingsHost = new Grid();
        settingsHost.Children.Add(SettingsView);
        Bind(settingsHost, UIElement.VisibilityProperty, "IsSettingsSelected", converter: _boolToVisibility);

        contentHost.Children.Add(deviceLabHost);
        contentHost.Children.Add(settingsHost);
        root.Children.Add(contentHost);
        return root;
    }

    private Button CreateHeaderButton(string label, string commandPath)
    {
        var button = new Button
        {
            Content = label,
            MinWidth = 120,
            Padding = new Thickness(16, 10, 16, 10),
            Background = CreateSolid("#1FFFFFFF"),
            Foreground = CreateSolid("#FFFFFFFF"),
            BorderBrush = CreateSolid("#33FFFFFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12)
        };
        Bind(button, Button.CommandProperty, commandPath);
        return button;
    }

    private void Bind(FrameworkElement element, DependencyProperty property, string path, BindingMode mode = BindingMode.OneWay, IValueConverter? converter = null)
    {
        element.SetBinding(property, new Binding
        {
            Path = new PropertyPath(path),
            Mode = mode,
            Converter = converter
        });
    }

    private static SolidColorBrush CreateSolid(string hex)
    {
        var value = hex.TrimStart('#');
        return new SolidColorBrush(Windows.UI.Color.FromArgb(
            Convert.ToByte(value.Substring(0, 2), 16),
            Convert.ToByte(value.Substring(2, 2), 16),
            Convert.ToByte(value.Substring(4, 2), 16),
            Convert.ToByte(value.Substring(6, 2), 16)));
    }
}
