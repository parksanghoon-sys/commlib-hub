using CommLib.Examples.WinUI.Converters;
using CommLib.Examples.WinUI.Styles;
using CommLib.Examples.WinUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace CommLib.Examples.WinUI.Views;

public sealed class AppShellView : UserControl
{
    private readonly ResourceDictionary _resources = DeviceLabTheme.Create();

    public AppShellView(
        ShellViewModel viewModel,
        DeviceLabView deviceLabView,
        SettingsView settingsView)
    {
        ViewModel = viewModel;
        DeviceLabView = deviceLabView;
        SettingsView = settingsView;

        _resources["BoolToVisibilityConverter"] = new BooleanToVisibilityConverter();

        Content = BuildContent();
        DataContext = ViewModel;
    }

    public ShellViewModel ViewModel { get; }

    public DeviceLabView DeviceLabView { get; }

    public SettingsView SettingsView { get; }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = GetBrush(DeviceLabTheme.WindowBackgroundBrushKey),
            Resources = _resources
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var headerCard = new Border
        {
            Margin = new Thickness(28, 24, 28, 0),
            Padding = new Thickness(24, 18, 24, 18),
            Background = GetBrush(DeviceLabTheme.HeroPanelBrushKey),
            BorderBrush = CreateSolid("#18FFFFFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Transitions = CreateTransitions(fromVerticalOffset: 20)
        };

        var headerGrid = new Grid { ColumnSpacing = 24 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Spacing = 6 };
        titleStack.Children.Add(new TextBlock
        {
            Text = "CommLib Control Center",
            FontFamily = new FontFamily("Segoe UI Variable Display Semib"),
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey)
        });

        var subtitle = new TextBlock
        {
            MaxWidth = 760,
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = CreateSolid("#D7F8FBFD")
        };
        Bind(subtitle, TextBlock.TextProperty, "CurrentPageSubtitle");
        titleStack.Children.Add(subtitle);
        headerGrid.Children.Add(titleStack);

        var navRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 12
        };
        navRow.Children.Add(CreateNavigationButton(
            "Device Lab",
            "Operate",
            "OpenDeviceLabPageCommand",
            "IsDeviceLabSelected"));
        navRow.Children.Add(CreateNavigationButton(
            "Settings",
            "Persist",
            "OpenSettingsPageCommand",
            "IsSettingsSelected"));
        Grid.SetColumn(navRow, 1);
        headerGrid.Children.Add(navRow);

        headerCard.Child = headerGrid;
        root.Children.Add(headerCard);

        var contentGrid = new Grid
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(contentGrid, 1);

        DeviceLabView.Transitions = CreateTransitions(fromHorizontalOffset: -24);
        SettingsView.Transitions = CreateTransitions(fromHorizontalOffset: 24);

        Bind(DeviceLabView, UIElement.VisibilityProperty, "IsDeviceLabSelected", converter: GetConverter<BooleanToVisibilityConverter>("BoolToVisibilityConverter"));
        Bind(SettingsView, UIElement.VisibilityProperty, "IsSettingsSelected", converter: GetConverter<BooleanToVisibilityConverter>("BoolToVisibilityConverter"));

        contentGrid.Children.Add(DeviceLabView);
        contentGrid.Children.Add(SettingsView);
        root.Children.Add(contentGrid);

        return root;
    }

    private FrameworkElement CreateNavigationButton(string title, string caption, string commandPath, string isSelectedPath)
    {
        var stack = new StackPanel
        {
            Spacing = 6,
            Width = 164
        };

        var buttonContent = new StackPanel { Spacing = 2 };
        buttonContent.Children.Add(new TextBlock
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        });
        buttonContent.Children.Add(new TextBlock
        {
            Text = caption,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = CreateSolid("#C0EAF4FB"),
            FontSize = 12
        });

        var button = new Button
        {
            Content = buttonContent,
            Style = GetStyle(DeviceLabTheme.SecondaryButtonStyleKey)
        };
        button.Background = CreateSolid("#18FFFFFF");
        button.BorderBrush = CreateSolid("#2EFFFFFF");
        button.Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey);
        Bind(button, Button.CommandProperty, commandPath);
        stack.Children.Add(button);

        var indicator = new Border
        {
            Height = 4,
            Background = CreateSolid("#FFF8FBFD"),
            CornerRadius = new CornerRadius(999),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Bind(indicator, UIElement.VisibilityProperty, isSelectedPath, converter: GetConverter<BooleanToVisibilityConverter>("BoolToVisibilityConverter"));
        stack.Children.Add(indicator);

        return stack;
    }

    private TransitionCollection CreateTransitions(double fromHorizontalOffset = 0, double fromVerticalOffset = 0)
    {
        return
        [
            new RepositionThemeTransition(),
            new EntranceThemeTransition
            {
                FromHorizontalOffset = fromHorizontalOffset,
                FromVerticalOffset = fromVerticalOffset
            }
        ];
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

    private Style GetStyle(string key)
    {
        return (Style)_resources[key];
    }

    private Brush GetBrush(string key)
    {
        return (Brush)_resources[key];
    }

    private T GetConverter<T>(string key) where T : class
    {
        return (T)_resources[key];
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
