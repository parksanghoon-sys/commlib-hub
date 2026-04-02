using CommLib.Examples.WinUI.Converters;
using CommLib.Examples.WinUI.Styles;
using CommLib.Examples.WinUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace CommLib.Examples.WinUI.Views;

public sealed class SettingsView : UserControl
{
    private readonly ResourceDictionary _resources = DeviceLabTheme.Create();

    public SettingsView(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        _resources["BoolToVisibilityConverter"] = new BooleanToVisibilityConverter();

        Content = BuildContent();
        DataContext = ViewModel;
    }

    public SettingsViewModel ViewModel { get; }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = GetBrush(DeviceLabTheme.WindowBackgroundBrushKey),
            Resources = _resources
        };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var layout = new StackPanel
        {
            MaxWidth = 1580,
            Margin = new Thickness(28, 18, 28, 28),
            Spacing = 18
        };
        layout.Children.Add(BuildHeroCard());

        var bodyGrid = new Grid { ColumnSpacing = 18 };
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(520) });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var leftColumn = new StackPanel { Spacing = 18 };
        leftColumn.Children.Add(BuildGeneralCard());
        leftColumn.Children.Add(BuildPersistenceCard());
        Grid.SetColumn(leftColumn, 0);

        var rightColumn = new StackPanel { Spacing = 18 };
        rightColumn.Children.Add(BuildMessageCard());
        rightColumn.Children.Add(BuildTransportCard());
        Grid.SetColumn(rightColumn, 1);

        bodyGrid.Children.Add(leftColumn);
        bodyGrid.Children.Add(rightColumn);
        layout.Children.Add(bodyGrid);

        scrollViewer.Content = layout;
        root.Children.Add(scrollViewer);
        return root;
    }

    private UIElement BuildHeroCard()
    {
        var card = CreateCard(fromVerticalOffset: 40);
        card.Background = GetBrush(DeviceLabTheme.HeroPanelBrushKey);
        card.BorderBrush = CreateSolid("#18FFFFFF");

        var grid = new Grid { ColumnSpacing = 18 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Spacing = 8 };
        titleStack.Children.Add(new TextBlock
        {
            Text = "Settings Studio",
            FontFamily = new FontFamily("Segoe UI Variable Display Semib"),
            FontSize = 32,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey)
        });
        titleStack.Children.Add(new TextBlock
        {
            MaxWidth = 760,
            Text = "Edit transport defaults, session identity, and message presets here. Everything is persisted to appsettings.json and loaded back when the app starts.",
            Foreground = CreateSolid("#D7F8FBFD"),
            TextWrapping = TextWrapping.WrapWholeWords
        });

        var badgeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        badgeRow.Children.Add(CreateHeroBadge("Shared State"));
        badgeRow.Children.Add(CreateHeroBadge("appsettings.json"));
        badgeRow.Children.Add(CreateHeroBadge("Startup Load"));
        titleStack.Children.Add(badgeRow);
        grid.Children.Add(titleStack);

        var statusCard = new Border
        {
            MinWidth = 320,
            Padding = new Thickness(16),
            Background = CreateSolid("#18FFFFFF"),
            BorderBrush = CreateSolid("#14FFFFFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20)
        };

        var statusStack = new StackPanel { Spacing = 10 };
        var headerRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };
        var progress = new ProgressRing
        {
            Width = 22,
            Height = 22
        };
        Bind(progress, ProgressRing.IsActiveProperty, "IsBusy");
        Bind(progress, UIElement.VisibilityProperty, "IsBusy", converter: GetConverter<BooleanToVisibilityConverter>("BoolToVisibilityConverter"));
        headerRow.Children.Add(progress);

        var textStack = new StackPanel { Spacing = 2 };
        var title = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey)
        };
        Bind(title, TextBlock.TextProperty, "StatusTitle");
        textStack.Children.Add(title);

        var detail = new TextBlock
        {
            Foreground = CreateSolid("#D7F8FBFD"),
            TextWrapping = TextWrapping.WrapWholeWords,
            MaxWidth = 280
        };
        Bind(detail, TextBlock.TextProperty, "StatusDetail");
        textStack.Children.Add(detail);

        headerRow.Children.Add(textStack);
        statusStack.Children.Add(headerRow);

        var filePath = new TextBlock
        {
            Foreground = CreateSolid("#C2F8FBFD"),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        Bind(filePath, TextBlock.TextProperty, "SettingsFilePath");
        statusStack.Children.Add(filePath);

        statusCard.Child = statusStack;
        Grid.SetColumn(statusCard, 1);
        grid.Children.Add(statusCard);

        card.Child = grid;
        return card;
    }

    private UIElement BuildGeneralCard()
    {
        var card = CreateCard(fromHorizontalOffset: -24);
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(CreateCardHeader(
            "General Settings",
            "Configure the session identity and default transport that the Device Lab page will use."));

        var transportCombo = CreateComboBox();
        Bind(transportCombo, ItemsControl.ItemsSourceProperty, "Settings.TransportChoices");
        Bind(transportCombo, Selector.SelectedItemProperty, "Settings.SelectedTransport", BindingMode.TwoWay);

        var layout = new Grid { RowSpacing = 12, ColumnSpacing = 12 };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddToGrid(layout, CreateLabeledInput("Device Id", CreateTextBox("Settings.DeviceId")), 0, 0);
        AddToGrid(layout, CreateLabeledInput("Display Name", CreateTextBox("Settings.DisplayName")), 0, 1);
        AddToGrid(layout, CreateLabeledInput("Default Timeout (ms)", CreateTextBox("Settings.DefaultTimeoutMs")), 1, 0);
        AddToGrid(layout, CreateLabeledInput("Max Pending Requests", CreateTextBox("Settings.MaxPendingRequests")), 1, 1);
        AddToGrid(layout, CreateLabeledInput("Selected Transport", transportCombo), 2, 0);

        var preview = new Border
        {
            Padding = new Thickness(14),
            Background = GetBrush(DeviceLabTheme.TransportPanelBrushKey),
            CornerRadius = new CornerRadius(16)
        };
        var previewStack = new StackPanel { Spacing = 4 };
        var previewTitle = new TextBlock { Style = GetStyle(DeviceLabTheme.BodyTitleStyleKey) };
        Bind(previewTitle, TextBlock.TextProperty, "Settings.SelectedTransportTitle");
        previewStack.Children.Add(previewTitle);

        var previewSubtitle = new TextBlock { Style = GetStyle(DeviceLabTheme.BodyCaptionStyleKey) };
        Bind(previewSubtitle, TextBlock.TextProperty, "Settings.SelectedTransportSubtitle");
        previewStack.Children.Add(previewSubtitle);
        preview.Child = previewStack;
        Grid.SetRow(preview, 2);
        Grid.SetColumn(preview, 1);
        layout.Children.Add(preview);

        content.Children.Add(layout);
        card.Child = content;
        return card;
    }

    private UIElement BuildPersistenceCard()
    {
        var card = CreateCard(fromHorizontalOffset: -8);
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(CreateCardHeader(
            "Persistence",
            "Save, reload, or reset the current configuration. The file is stored next to the executable as appsettings.json."));

        var pathBlock = new TextBlock
        {
            Style = GetStyle(DeviceLabTheme.BodyCaptionStyleKey)
        };
        Bind(pathBlock, TextBlock.TextProperty, "SettingsFilePath");
        content.Children.Add(pathBlock);

        var buttonGrid = new Grid { ColumnSpacing = 12 };
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        AddButton(buttonGrid, 0, "Save", "SaveSettingsCommand", DeviceLabTheme.PrimaryButtonStyleKey);
        AddButton(buttonGrid, 1, "Reload", "ReloadSettingsCommand", DeviceLabTheme.SecondaryButtonStyleKey);
        AddButton(buttonGrid, 2, "Restore Defaults", "ResetDefaultsCommand", DeviceLabTheme.SecondaryButtonStyleKey);
        content.Children.Add(buttonGrid);

        var statusBox = new Border
        {
            Padding = new Thickness(14),
            Background = GetBrush(DeviceLabTheme.TransportPanelBrushKey),
            CornerRadius = new CornerRadius(16)
        };
        var statusStack = new StackPanel { Spacing = 4 };
        var statusTitle = new TextBlock { Style = GetStyle(DeviceLabTheme.BodyTitleStyleKey) };
        Bind(statusTitle, TextBlock.TextProperty, "StatusTitle");
        statusStack.Children.Add(statusTitle);

        var statusDetail = new TextBlock { Style = GetStyle(DeviceLabTheme.BodyCaptionStyleKey) };
        Bind(statusDetail, TextBlock.TextProperty, "StatusDetail");
        statusStack.Children.Add(statusDetail);
        statusBox.Child = statusStack;
        content.Children.Add(statusBox);

        card.Child = content;
        return card;
    }

    private UIElement BuildMessageCard()
    {
        var card = CreateCard(fromHorizontalOffset: 18);
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(CreateCardHeader(
            "Message Defaults",
            "These values populate the outbound message composer on the Device Lab page."));

        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(CreateLabeledInput("Message Id", CreateTextBox("Settings.OutboundMessageId")));
        var body = CreateLabeledInput("Default Body", CreateTextBox("Settings.OutboundBody", height: 160, acceptsReturn: true));
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);

        content.Children.Add(grid);
        card.Child = content;
        return card;
    }

    private UIElement BuildTransportCard()
    {
        var card = CreateCard(fromHorizontalOffset: 36);
        var content = new StackPanel { Spacing = 18 };
        content.Children.Add(CreateCardHeader(
            "Transport Presets",
            "Maintain each transport preset here so switching pages keeps one shared configuration document."));

        content.Children.Add(CreateTransportSection(
            "TCP",
            "Settings.TcpSettings.Host",
            "Settings.TcpSettings.Port",
            "Settings.TcpSettings.ConnectTimeoutMs",
            "Settings.TcpSettings.BufferSize",
            "Settings.TcpSettings.NoDelay"));

        content.Children.Add(CreateTransportSection(
            "UDP",
            "Settings.UdpSettings.LocalPort",
            "Settings.UdpSettings.RemoteHost",
            "Settings.UdpSettings.RemotePort"));

        content.Children.Add(CreateTransportSection(
            "Multicast",
            "Settings.MulticastSettings.GroupAddress",
            "Settings.MulticastSettings.Port",
            "Settings.MulticastSettings.Ttl",
            "Settings.MulticastSettings.LocalInterface",
            "Settings.MulticastSettings.Loopback"));

        content.Children.Add(CreateSerialSection());

        card.Child = content;
        return card;
    }

    private UIElement CreateTransportSection(string title, params string[] bindings)
    {
        var panel = new Border
        {
            Padding = new Thickness(16),
            Background = GetBrush(DeviceLabTheme.TransportPanelBrushKey),
            CornerRadius = new CornerRadius(18)
        };

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Style = GetStyle(DeviceLabTheme.BodyTitleStyleKey)
        });

        var grid = new Grid { RowSpacing = 12, ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var index = 0; index < bindings.Length; index++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var path = bindings[index];
            if (path.EndsWith(".NoDelay", StringComparison.Ordinal) || path.EndsWith(".Loopback", StringComparison.Ordinal))
            {
                var toggle = CreateCheckBox(GetFieldLabel(path), path);
                Grid.SetRow(toggle, index);
                Grid.SetColumnSpan(toggle, 2);
                grid.Children.Add(toggle);
                continue;
            }

            AddToGrid(
                grid,
                CreateLabeledInput(GetFieldLabel(path), CreateTextBox(path)),
                index,
                0);
        }

        stack.Children.Add(grid);
        panel.Child = stack;
        return panel;
    }

    private UIElement CreateSerialSection()
    {
        var panel = new Border
        {
            Padding = new Thickness(16),
            Background = GetBrush(DeviceLabTheme.TransportPanelBrushKey),
            CornerRadius = new CornerRadius(18)
        };

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock
        {
            Text = "Serial",
            Style = GetStyle(DeviceLabTheme.BodyTitleStyleKey)
        });

        var grid = new Grid { RowSpacing = 12, ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var index = 0; index < 3; index++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        AddToGrid(grid, CreateLabeledInput("Port Name", CreateTextBox("Settings.SerialSettings.PortName")), 0, 0);
        AddToGrid(grid, CreateLabeledInput("Baud Rate", CreateTextBox("Settings.SerialSettings.BaudRate")), 0, 1);
        AddToGrid(grid, CreateLabeledInput("Data Bits", CreateTextBox("Settings.SerialSettings.DataBits")), 0, 2);
        AddToGrid(grid, CreateLabeledInput("Parity", CreateComboBox("Settings.SerialSettings.ParityOptions", "Settings.SerialSettings.Parity")), 1, 0);
        AddToGrid(grid, CreateLabeledInput("Stop Bits", CreateComboBox("Settings.SerialSettings.StopBitsOptions", "Settings.SerialSettings.StopBits")), 1, 1);
        AddToGrid(grid, CreateLabeledInput("Turn Gap (ms)", CreateTextBox("Settings.SerialSettings.TurnGapMs")), 1, 2);
        AddToGrid(grid, CreateLabeledInput("Read Buffer Size", CreateTextBox("Settings.SerialSettings.ReadBufferSize")), 2, 0);
        AddToGrid(grid, CreateLabeledInput("Write Buffer Size", CreateTextBox("Settings.SerialSettings.WriteBufferSize")), 2, 1);

        var halfDuplex = CreateCheckBox("Use half-duplex timing", "Settings.SerialSettings.HalfDuplex");
        halfDuplex.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(halfDuplex, 2);
        Grid.SetColumn(halfDuplex, 2);
        grid.Children.Add(halfDuplex);

        stack.Children.Add(grid);
        panel.Child = stack;
        return panel;
    }

    private static string GetFieldLabel(string path)
    {
        return path switch
        {
            "Settings.TcpSettings.Host" => "Host",
            "Settings.TcpSettings.Port" => "Port",
            "Settings.TcpSettings.ConnectTimeoutMs" => "Connect Timeout (ms)",
            "Settings.TcpSettings.BufferSize" => "Buffer Size",
            "Settings.TcpSettings.NoDelay" => "Disable Nagle (NoDelay)",
            "Settings.UdpSettings.LocalPort" => "Local Port",
            "Settings.UdpSettings.RemoteHost" => "Remote Host",
            "Settings.UdpSettings.RemotePort" => "Remote Port",
            "Settings.MulticastSettings.GroupAddress" => "Group Address",
            "Settings.MulticastSettings.Port" => "Port",
            "Settings.MulticastSettings.Ttl" => "TTL",
            "Settings.MulticastSettings.LocalInterface" => "Local Interface",
            "Settings.MulticastSettings.Loopback" => "Enable loopback",
            _ => path
        };
    }

    private Border CreateCard(double fromHorizontalOffset = 0, double fromVerticalOffset = 0)
    {
        var card = new Border
        {
            Background = GetBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(20),
            Transitions = CreateTransitions(fromHorizontalOffset, fromVerticalOffset)
        };
        return card;
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

    private UIElement CreateCardHeader(string title, string caption)
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Style = GetStyle(DeviceLabTheme.SectionTitleStyleKey)
        });
        stack.Children.Add(new TextBlock
        {
            Text = caption,
            Style = GetStyle(DeviceLabTheme.BodyCaptionStyleKey)
        });
        return stack;
    }

    private Border CreateHeroBadge(string text)
    {
        var badge = new Border
        {
            Style = GetStyle(DeviceLabTheme.BadgeBorderStyleKey),
            Background = CreateSolid("#1FFFFFFF")
        };

        badge.Child = new TextBlock
        {
            Text = text,
            Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey)
        };

        return badge;
    }

    private TextBlock CreateFieldLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Style = GetStyle(DeviceLabTheme.FieldLabelStyleKey)
        };
    }

    private FrameworkElement CreateLabeledInput(string label, FrameworkElement input)
    {
        var stack = new StackPanel();
        stack.Children.Add(CreateFieldLabel(label));
        stack.Children.Add(input);
        return stack;
    }

    private TextBox CreateTextBox(string path, double height = 40, bool acceptsReturn = false)
    {
        var textBox = new TextBox
        {
            Height = height,
            AcceptsReturn = acceptsReturn,
            TextWrapping = acceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Style = GetStyle(DeviceLabTheme.TextInputStyleKey)
        };
        Bind(textBox, TextBox.TextProperty, path, BindingMode.TwoWay);
        return textBox;
    }

    private ComboBox CreateComboBox(string itemsSourcePath, string selectedPath)
    {
        var comboBox = CreateComboBox();
        Bind(comboBox, ItemsControl.ItemsSourceProperty, itemsSourcePath);
        Bind(comboBox, Selector.SelectedItemProperty, selectedPath, BindingMode.TwoWay);
        return comboBox;
    }

    private ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            MinHeight = 40,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Style = GetStyle(DeviceLabTheme.ComboInputStyleKey)
        };
    }

    private CheckBox CreateCheckBox(string content, string path)
    {
        var checkBox = new CheckBox
        {
            Content = content,
            Style = GetStyle(DeviceLabTheme.InlineToggleStyleKey)
        };
        Bind(checkBox, ToggleButton.IsCheckedProperty, path, BindingMode.TwoWay);
        return checkBox;
    }

    private static void AddToGrid(Grid grid, FrameworkElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        grid.Children.Add(element);
    }

    private void AddButton(Grid grid, int column, string content, string commandPath, string styleKey)
    {
        var button = new Button
        {
            Content = content,
            Style = GetStyle(styleKey)
        };
        Bind(button, Button.CommandProperty, commandPath);
        Grid.SetColumn(button, column);
        grid.Children.Add(button);
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
