using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Converters;
using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.Styles;
using CommLib.Examples.WinUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CommLib.Examples.WinUI.Views;

public sealed class SettingsView : Grid
{
    private static readonly BooleanToVisibilityConverter VisibilityConverter = new();
    private readonly IAppLocalizer _localizer;
    private readonly ScrollViewer _scrollViewer;
    // Settings도 코드 기반 뷰라서 언어가 바뀌면 다시 써야 하는 텍스트를 모아 둔다.
    private readonly List<Action> _localizedTextUpdates = [];

    public SettingsView(SettingsViewModel viewModel, IAppLocalizer localizer)
    {
        _localizer = localizer;
        _scrollViewer = CreatePageScrollViewer();
        ViewModel = viewModel;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContext = ViewModel;
        _localizer.LanguageChanged += OnLanguageChanged;
        Children.Add(BuildContent());
        ApplyLocalizedText();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        ApplyLocalizedText();
    }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = GetThemeBrush(DeviceLabTheme.WindowBackgroundBrushKey)
        };

        var layout = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        layout.Children.Add(CreateHeroCard());
        layout.Children.Add(CreateGeneralCard());
        layout.Children.Add(CreateMessageCard());
        layout.Children.Add(CreateTransportCard());
        layout.Children.Add(CreatePersistenceCard());

        _scrollViewer.Content = layout;
        root.Children.Add(_scrollViewer);
        return root;
    }

    private UIElement CreateHeroCard()
    {
        var card = CreateCard(GetThemeBrush(DeviceLabTheme.HeroPanelBrushKey));
        var stack = CreateVerticalStack(8);
        var title = new TextBlock
        {
            FontSize = 30,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetThemeBrush(DeviceLabTheme.HeroForegroundBrushKey)
        };
        RegisterLocalizedText(() => title.Text = _localizer.Get("settings.hero.title"));
        stack.Children.Add(title);

        var description = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = CreateSolid("#FFD4E7F5")
        };
        RegisterLocalizedText(() => description.Text = _localizer.Get("settings.hero.description"));
        stack.Children.Add(description);

        var status = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FFE8F4FF")
        };
        Bind(status, TextBlock.TextProperty, "StatusTitle");
        stack.Children.Add(status);

        var detail = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = CreateSolid("#FFD4E7F5")
        };
        Bind(detail, TextBlock.TextProperty, "StatusDetail");
        stack.Children.Add(detail);

        card.Child = stack;
        return card;
    }

    private UIElement CreateGeneralCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("settings.section.general"));

        var language = new ComboBox
        {
            DisplayMemberPath = "Label",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 40,
            Background = GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Foreground = GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            Padding = new Thickness(10, 7, 10, 7)
        };
        Bind(language, ItemsControl.ItemsSourceProperty, "Settings.LanguageChoices");
        Bind(language, Selector.SelectedItemProperty, "Settings.SelectedLanguage", BindingMode.TwoWay);

        var transport = new ComboBox
        {
            DisplayMemberPath = "Label",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 40,
            Background = GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Foreground = GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            Padding = new Thickness(10, 7, 10, 7)
        };
        Bind(transport, ItemsControl.ItemsSourceProperty, "Settings.TransportChoices");
        Bind(transport, Selector.SelectedItemProperty, "Settings.SelectedTransport", BindingMode.TwoWay);
        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("field.languageMode", language),
            CreateLabeledField("settings.field.selectedTransport", transport)));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("field.deviceId", CreateTextBox("Settings.DeviceId")),
            CreateLabeledField("field.displayName", CreateTextBox("Settings.DisplayName"))));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("field.defaultTimeoutMs", CreateTextBox("Settings.DefaultTimeoutMs")),
            CreateLabeledField("field.maxPendingRequests", CreateTextBox("Settings.MaxPendingRequests"))));

        card.Child = stack;
        return card;
    }

    private UIElement CreateMessageCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("settings.section.messageDefaults"));

        var serializer = CreateComboBox("Settings.SerializerChoices", "Settings.SelectedSerializer");
        serializer.DisplayMemberPath = "Label";

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("field.messageId", CreateTextBox("Settings.OutboundMessageId")),
            CreateLabeledField("field.serializer", serializer)));

        var serializerHint = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyCaptionStyleKey),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        Bind(serializerHint, TextBlock.TextProperty, "Settings.SelectedSerializerSubtitle");
        stack.Children.Add(serializerHint);

        stack.Children.Add(CreateLabeledField("field.body", CreateTextBox("Settings.OutboundBody", 120, true)));
        card.Child = stack;
        return card;
    }

    private UIElement CreateTransportCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("settings.section.transportPresets"));

        stack.Children.Add(CreateTransportPanel(TransportKind.Tcp, [
            CreateLabeledField("field.host", CreateTextBox("Settings.TcpSettings.Host")),
            CreateLabeledField("field.port", CreateTextBox("Settings.TcpSettings.Port")),
            CreateLabeledField("field.connectTimeoutMs", CreateTextBox("Settings.TcpSettings.ConnectTimeoutMs")),
            CreateLabeledField("field.bufferSize", CreateTextBox("Settings.TcpSettings.BufferSize")),
            CreateCheckBox("check.noDelay", "Settings.TcpSettings.NoDelay")
        ]));

        stack.Children.Add(CreateTransportPanel(TransportKind.Udp, [
            CreateLabeledField("field.localPort", CreateTextBox("Settings.UdpSettings.LocalPort")),
            CreateLabeledField("field.remoteHost", CreateTextBox("Settings.UdpSettings.RemoteHost")),
            CreateLabeledField("field.remotePort", CreateTextBox("Settings.UdpSettings.RemotePort"))
        ]));

        stack.Children.Add(CreateTransportPanel(TransportKind.Multicast, [
            CreateLabeledField("field.groupAddress", CreateTextBox("Settings.MulticastSettings.GroupAddress")),
            CreateLabeledField("field.port", CreateTextBox("Settings.MulticastSettings.Port")),
            CreateLabeledField("field.ttl", CreateTextBox("Settings.MulticastSettings.Ttl")),
            CreateLabeledField("field.localInterface", CreateTextBox("Settings.MulticastSettings.LocalInterface")),
            CreateCheckBox("check.loopback", "Settings.MulticastSettings.Loopback")
        ]));

        stack.Children.Add(CreateTransportPanel(TransportKind.Serial, [
            CreateLabeledField("field.portName", CreateTextBox("Settings.SerialSettings.PortName")),
            CreateLabeledField("field.baudRate", CreateTextBox("Settings.SerialSettings.BaudRate")),
            CreateLabeledField("field.dataBits", CreateTextBox("Settings.SerialSettings.DataBits")),
            CreateLabeledField("field.parity", CreateComboBox("Settings.SerialSettings.ParityOptions", "Settings.SerialSettings.Parity")),
            CreateLabeledField("field.stopBits", CreateComboBox("Settings.SerialSettings.StopBitsOptions", "Settings.SerialSettings.StopBits")),
            CreateLabeledField("field.turnGapMs", CreateTextBox("Settings.SerialSettings.TurnGapMs")),
            CreateLabeledField("field.readBufferSize", CreateTextBox("Settings.SerialSettings.ReadBufferSize")),
            CreateLabeledField("field.writeBufferSize", CreateTextBox("Settings.SerialSettings.WriteBufferSize")),
            CreateCheckBox("check.halfDuplex", "Settings.SerialSettings.HalfDuplex")
        ]));

        card.Child = stack;
        return card;
    }

    private UIElement CreatePersistenceCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("settings.section.persistence"));

        var filePath = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyCaptionStyleKey)
        };
        Bind(filePath, TextBlock.TextProperty, "SettingsFilePath");
        stack.Children.Add(filePath);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };
        buttons.Children.Add(CreateCommandButton("button.save", "SaveSettingsCommand", isPrimary: true));
        buttons.Children.Add(CreateCommandButton("button.reload", "ReloadSettingsCommand"));
        buttons.Children.Add(CreateCommandButton("button.restoreDefaults", "ResetDefaultsCommand"));
        stack.Children.Add(buttons);

        var statusTitle = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyTitleStyleKey)
        };
        Bind(statusTitle, TextBlock.TextProperty, "StatusTitle");
        stack.Children.Add(statusTitle);

        var statusDetail = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyCaptionStyleKey)
        };
        Bind(statusDetail, TextBlock.TextProperty, "StatusDetail");
        stack.Children.Add(statusDetail);

        card.Child = stack;
        return card;
    }

    private Border CreateTransportPanel(TransportKind kind, IEnumerable<UIElement> children)
    {
        var border = new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            Background = GetThemeBrush(DeviceLabTheme.TransportPanelBrushKey)
        };
        Bind(border, VisibilityProperty, GetTransportVisibilityPath(kind), converter: VisibilityConverter);

        var stack = CreateVerticalStack(10);
        var title = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyTitleStyleKey)
        };
        RegisterLocalizedText(() => title.Text = _localizer.GetTransportLabel(kind));
        stack.Children.Add(title);

        foreach (var child in children)
        {
            stack.Children.Add(child);
        }

        border.Child = stack;
        return border;
    }

    private Border CreateCard(Brush? background = null)
    {
        return new Border
        {
            Style = GetThemeStyle(DeviceLabTheme.CardBorderStyleKey),
            Background = background ?? GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey)
        };
    }

    private TextBlock CreateSectionTitle(string key)
    {
        var textBlock = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.SectionTitleStyleKey)
        };
        RegisterLocalizedText(() => textBlock.Text = _localizer.Get(key));
        return textBlock;
    }

    private StackPanel CreateVerticalStack(double spacing)
    {
        return new StackPanel { Spacing = spacing };
    }

    private FrameworkElement CreateLabeledField(string labelKey, FrameworkElement input)
    {
        var stack = CreateVerticalStack(6);
        var label = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.FieldLabelStyleKey)
        };
        RegisterLocalizedText(() => label.Text = _localizer.Get(labelKey));
        stack.Children.Add(label);
        stack.Children.Add(input);
        return stack;
    }

    private Grid CreateTwoColumnRow(FrameworkElement left, FrameworkElement right)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(left);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }

    private TextBox CreateTextBox(string path, double height = 40, bool acceptsReturn = false)
    {
        var textBox = new TextBox
        {
            Height = height,
            AcceptsReturn = acceptsReturn,
            TextWrapping = acceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Foreground = GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            Padding = new Thickness(12, 9, 12, 9)
        };
        PointerWheelScrollBridge.Attach(textBox, _scrollViewer);
        Bind(textBox, TextBox.TextProperty, path, BindingMode.TwoWay);
        return textBox;
    }

    private static ScrollViewer CreatePageScrollViewer()
    {
        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private ComboBox CreateComboBox(string itemsSourcePath, string selectedPath)
    {
        var comboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 40,
            Background = GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Foreground = GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            Padding = new Thickness(10, 7, 10, 7)
        };
        Bind(comboBox, ItemsControl.ItemsSourceProperty, itemsSourcePath);
        Bind(comboBox, Selector.SelectedItemProperty, selectedPath, BindingMode.TwoWay);
        return comboBox;
    }

    private static string GetTransportVisibilityPath(TransportKind kind)
    {
        // visibility는 별도 UI 상태를 만들지 않고 현재 선택된 transport에서 직접 파생시킨다.
        // 그래야 Settings와 Device Lab이 같은 source of truth를 공유한다.
        return kind switch
        {
            TransportKind.Tcp => "Settings.IsTcpSelected",
            TransportKind.Udp => "Settings.IsUdpSelected",
            TransportKind.Multicast => "Settings.IsMulticastSelected",
            TransportKind.Serial => "Settings.IsSerialSelected",
            _ => throw new InvalidOperationException($"Unsupported transport selection: {kind}")
        };
    }

    private CheckBox CreateCheckBox(string contentKey, string path)
    {
        var checkBox = new CheckBox
        {
            Foreground = GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            Padding = new Thickness(2),
            MinHeight = 32
        };
        RegisterLocalizedText(() => checkBox.Content = _localizer.Get(contentKey));
        Bind(checkBox, ToggleButton.IsCheckedProperty, path, BindingMode.TwoWay);
        return checkBox;
    }

    private Button CreateCommandButton(string labelKey, string commandPath, bool isPrimary = false)
    {
        var button = new Button
        {
            Padding = new Thickness(16, 10, 16, 10),
            MinHeight = 40,
            CornerRadius = new CornerRadius(12),
            Background = isPrimary
                ? GetThemeBrush(DeviceLabTheme.AccentBrushKey)
                : GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            Foreground = isPrimary
                ? GetThemeBrush(DeviceLabTheme.HeroForegroundBrushKey)
                : GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            BorderBrush = isPrimary
                ? GetThemeBrush(DeviceLabTheme.AccentBrushKey)
                : GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.SemiBold
        };
        RegisterLocalizedText(() => button.Content = _localizer.Get(labelKey));
        Bind(button, Button.CommandProperty, commandPath);
        return button;
    }

    private T GetTheme<T>(string key) where T : class
    {
        return DeviceLabTheme.Get<T>(this, key);
    }

    private Brush GetThemeBrush(string key)
    {
        return GetTheme<Brush>(key);
    }

    private Style GetThemeStyle(string key)
    {
        return GetTheme<Style>(key);
    }

    private void RegisterLocalizedText(Action applyText)
    {
        _localizedTextUpdates.Add(applyText);
    }

    private void ApplyLocalizedText()
    {
        foreach (var updateText in _localizedTextUpdates)
        {
            updateText();
        }
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
