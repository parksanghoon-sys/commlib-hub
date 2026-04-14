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

/// <summary>
/// DeviceLabView 타입입니다.
/// </summary>
public sealed class DeviceLabView : Grid
{
    /// <summary>
    /// VisibilityConverter 값을 나타냅니다.
    /// </summary>
    private static readonly BooleanToVisibilityConverter VisibilityConverter = new();
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;
    /// <summary>
    /// _scrollViewer 값을 나타냅니다.
    /// </summary>
    private readonly ScrollViewer _scrollViewer;
    // 이 뷰는 XAML 대신 코드로 화면을 조합하므로,
    // 언어 변경 시 다시 써야 하는 텍스트 setter를 별도로 모아 두고 일괄 재적용한다.
    /// <summary>
    /// _localizedTextUpdates 값을 나타냅니다.
    /// </summary>
    private readonly List<Action> _localizedTextUpdates = [];
    /// <summary>
    /// _liveLogScrollViewer 값을 나타냅니다.
    /// </summary>
    private ScrollViewer? _liveLogScrollViewer;

    /// <summary>
    /// <see cref="DeviceLabView"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public DeviceLabView(MainViewModel viewModel, IAppLocalizer localizer)
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

    /// <summary>
    /// ViewModel 값을 가져옵니다.
    /// </summary>
    public MainViewModel ViewModel { get; }

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        ApplyLocalizedText();
    }

    /// <summary>
    /// BuildContent 작업을 수행합니다.
    /// </summary>
    private FrameworkElement BuildContent()
    {
        // Device Lab은 "세션 설정 -> transport 설정 -> mock peer -> 전송 -> 로그" 순서로 읽히게 구성해
        // 사용자가 실제 테스트 흐름을 화면 위에서 아래로 그대로 따라가게 만든다.
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
        layout.Children.Add(CreateSessionCard());
        layout.Children.Add(CreateTransportCard());
        layout.Children.Add(CreateMockEndpointCard());
        layout.Children.Add(CreateComposerCard());
        layout.Children.Add(CreateLogCard());

        _scrollViewer.Content = layout;
        root.Children.Add(_scrollViewer);
        return root;
    }

    /// <summary>
    /// CreateHeroCard 작업을 수행합니다.
    /// </summary>
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
        RegisterLocalizedText(() => title.Text = _localizer.Get("deviceLab.hero.title"));
        stack.Children.Add(title);

        var description = new TextBlock
        {
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = CreateSolid("#FFD4E7F5")
        };
        RegisterLocalizedText(() => description.Text = _localizer.Get("deviceLab.hero.description"));
        stack.Children.Add(description);

        var status = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FFE8F4FF")
        };
        Bind(status, TextBlock.TextProperty, "StatusText");
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

    /// <summary>
    /// CreateSessionCard 작업을 수행합니다.
    /// </summary>
    private UIElement CreateSessionCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("deviceLab.section.sessionSetup"));

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
        stack.Children.Add(CreateLabeledField("field.transport", transport));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("field.deviceId", CreateTextBox("Settings.DeviceId")),
            CreateLabeledField("field.displayName", CreateTextBox("Settings.DisplayName"))));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("field.defaultTimeoutMs", CreateTextBox("Settings.DefaultTimeoutMs")),
            CreateLabeledField("field.maxPendingRequests", CreateTextBox("Settings.MaxPendingRequests"))));

        card.Child = stack;
        return card;
    }

    /// <summary>
    /// CreateTransportCard 작업을 수행합니다.
    /// </summary>
    private UIElement CreateTransportCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("deviceLab.section.transportSettings"));

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

    /// <summary>
    /// CreateComposerCard 작업을 수행합니다.
    /// </summary>
    private UIElement CreateComposerCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("deviceLab.section.messageComposer"));

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

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        buttons.Children.Add(CreateCommandButton("button.connect", "ConnectCommand", isPrimary: true));
        buttons.Children.Add(CreateCommandButton("button.disconnect", "DisconnectCommand"));
        buttons.Children.Add(CreateCommandButton("button.send", "SendCommand"));
        buttons.Children.Add(CreateCommandButton("button.clearLog", "ClearLogCommand"));
        stack.Children.Add(buttons);

        card.Child = stack;
        return card;
    }

    /// <summary>
    /// CreateMockEndpointCard 작업을 수행합니다.
    /// </summary>
    private UIElement CreateMockEndpointCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("deviceLab.section.mockEndpoint"));

        var description = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyCaptionStyleKey),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        RegisterLocalizedText(() => description.Text = _localizer.Get("deviceLab.mockEndpoint.description"));
        stack.Children.Add(description);

        var statusTitle = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyTitleStyleKey)
        };
        Bind(statusTitle, TextBlock.TextProperty, "MockEndpointStatusTitle");
        stack.Children.Add(statusTitle);

        var statusDetail = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.BodyCaptionStyleKey),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        Bind(statusDetail, TextBlock.TextProperty, "MockEndpointStatusDetail");
        stack.Children.Add(statusDetail);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        buttons.Children.Add(CreateCommandButton("button.startMockEndpoint", "StartMockEndpointCommand", isPrimary: true));
        buttons.Children.Add(CreateCommandButton("button.stopMockEndpoint", "StopMockEndpointCommand"));
        stack.Children.Add(buttons);

        card.Child = stack;
        return card;
    }

    /// <summary>
    /// CreateLogCard 작업을 수행합니다.
    /// </summary>
    private UIElement CreateLogCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("deviceLab.section.liveLog"));

        var logBox = CreateLiveLogBox();
        Bind(logBox, TextBox.TextProperty, "LogText");
        stack.Children.Add(logBox);

        card.Child = stack;
        return card;
    }

    /// <summary>
    /// CreateTransportPanel 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateCard 작업을 수행합니다.
    /// </summary>
    private Border CreateCard(Brush? background = null)
    {
        return new Border
        {
            Style = GetThemeStyle(DeviceLabTheme.CardBorderStyleKey),
            Background = background ?? GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey)
        };
    }

    /// <summary>
    /// CreateSectionTitle 작업을 수행합니다.
    /// </summary>
    private TextBlock CreateSectionTitle(string key)
    {
        var textBlock = new TextBlock
        {
            Style = GetThemeStyle(DeviceLabTheme.SectionTitleStyleKey)
        };
        RegisterLocalizedText(() => textBlock.Text = _localizer.Get(key));
        return textBlock;
    }

    /// <summary>
    /// CreateVerticalStack 작업을 수행합니다.
    /// </summary>
    private StackPanel CreateVerticalStack(double spacing)
    {
        return new StackPanel { Spacing = spacing };
    }

    /// <summary>
    /// CreateLabeledField 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateTwoColumnRow 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateTextBox 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateLiveLogBox 작업을 수행합니다.
    /// </summary>
    private TextBox CreateLiveLogBox()
    {
        var logBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            Height = 320,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = GetThemeBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetThemeBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Foreground = GetThemeBrush(DeviceLabTheme.SectionForegroundBrushKey),
            Padding = new Thickness(12, 9, 12, 9)
        };
        ScrollViewer.SetVerticalScrollBarVisibility(logBox, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(logBox, ScrollBarVisibility.Auto);
        // TextBox 템플릿 내부 ScrollViewer는 Loaded 이후에야 생기므로
        // 처음 한 번 캐시한 뒤 이후 변경에서는 그대로 재사용한다.
        logBox.Loaded += (_, _) => CacheLiveLogScrollViewer(logBox);
        logBox.TextChanged += (_, _) => ScrollLiveLogToLatest(logBox);
        return logBox;
    }

    /// <summary>
    /// CacheLiveLogScrollViewer 작업을 수행합니다.
    /// </summary>
    private void CacheLiveLogScrollViewer(TextBox logBox)
    {
        _liveLogScrollViewer = FindDescendant<ScrollViewer>(logBox);
        ScrollLiveLogToLatest(logBox);
    }

    /// <summary>
    /// ScrollLiveLogToLatest 작업을 수행합니다.
    /// </summary>
    private void ScrollLiveLogToLatest(TextBox logBox)
    {
        if (string.IsNullOrEmpty(logBox.Text))
        {
            return;
        }

        // TextChanged 시점에는 템플릿/레이아웃 갱신이 아직 끝나지 않았을 수 있어서
        // dispatcher에 한 박자 넘긴 뒤 caret과 실제 스크롤 오프셋을 함께 끝으로 보낸다.
        DispatcherQueue.TryEnqueue(() =>
        {
            logBox.Select(logBox.Text.Length, 0);
            var liveLogScrollViewer = _liveLogScrollViewer ??= FindDescendant<ScrollViewer>(logBox);
            liveLogScrollViewer?.ChangeView(null, liveLogScrollViewer.ScrollableHeight, null, disableAnimation: true);
        });
    }

    /// <summary>
    /// CreatePageScrollViewer 작업을 수행합니다.
    /// </summary>
    private static ScrollViewer CreatePageScrollViewer()
    {
        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    /// <summary>
    /// CreateComboBox 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// GetTransportVisibilityPath 작업을 수행합니다.
    /// </summary>
    private static string GetTransportVisibilityPath(TransportKind kind)
    {
        return kind switch
        {
            TransportKind.Tcp => "Settings.IsTcpSelected",
            TransportKind.Udp => "Settings.IsUdpSelected",
            TransportKind.Multicast => "Settings.IsMulticastSelected",
            TransportKind.Serial => "Settings.IsSerialSelected",
            _ => throw new InvalidOperationException($"Unsupported transport selection: {kind}")
        };
    }

    /// <summary>
    /// CreateCheckBox 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateCommandButton 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// GetThemeBrush 작업을 수행합니다.
    /// </summary>
    private Brush GetThemeBrush(string key)
    {
        return GetTheme<Brush>(key);
    }

    /// <summary>
    /// GetThemeStyle 작업을 수행합니다.
    /// </summary>
    private Style GetThemeStyle(string key)
    {
        return GetTheme<Style>(key);
    }

    /// <summary>
    /// RegisterLocalizedText 작업을 수행합니다.
    /// </summary>
    private void RegisterLocalizedText(Action applyText)
    {
        _localizedTextUpdates.Add(applyText);
    }

    /// <summary>
    /// ApplyLocalizedText 작업을 수행합니다.
    /// </summary>
    private void ApplyLocalizedText()
    {
        foreach (var updateText in _localizedTextUpdates)
        {
            updateText();
        }
    }

    /// <summary>
    /// Bind 작업을 수행합니다.
    /// </summary>
    private void Bind(FrameworkElement element, DependencyProperty property, string path, BindingMode mode = BindingMode.OneWay, IValueConverter? converter = null)
    {
        element.SetBinding(property, new Binding
        {
            Path = new PropertyPath(path),
            Mode = mode,
            Converter = converter
        });
    }

    /// <summary>
    /// CreateSolid 작업을 수행합니다.
    /// </summary>
    private static SolidColorBrush CreateSolid(string hex)
    {
        var value = hex.TrimStart('#');
        return new SolidColorBrush(Windows.UI.Color.FromArgb(
            Convert.ToByte(value.Substring(0, 2), 16),
            Convert.ToByte(value.Substring(2, 2), 16),
            Convert.ToByte(value.Substring(4, 2), 16),
            Convert.ToByte(value.Substring(6, 2), 16)));
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        // WinUI 기본 컨트롤의 내부 템플릿 객체는 이름으로 바로 잡기 어려우므로
        // 필요한 경우에만 얕은 helper 하나로 visual tree를 재귀 탐색한다.
        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T typedChild)
            {
                return typedChild;
            }

            if (FindDescendant<T>(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}