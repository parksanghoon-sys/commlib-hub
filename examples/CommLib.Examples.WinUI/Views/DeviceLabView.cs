using CommLib.Examples.WinUI.Converters;
using CommLib.Examples.WinUI.Styles;
using CommLib.Examples.WinUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace CommLib.Examples.WinUI.Views;

public sealed class DeviceLabView : UserControl
{
    private readonly ResourceDictionary _resources = DeviceLabTheme.Create();
    private readonly DataTemplate _transportChoiceTemplate;
    private readonly DataTemplate _logEntryTemplate;

    public DeviceLabView(MainViewModel viewModel)
    {
        ViewModel = viewModel;

        _resources["BoolToVisibilityConverter"] = new BooleanToVisibilityConverter();
        _resources["LogSeverityToBrushConverter"] = new LogSeverityToBrushConverter();

        _transportChoiceTemplate = CreateDataTemplate(
            """
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <StackPanel Spacing="2">
                    <TextBlock FontFamily="Segoe UI Variable Display"
                               FontWeight="SemiBold"
                               Text="{Binding Label}" />
                    <TextBlock Style="{StaticResource BodyCaptionStyle}"
                               Text="{Binding Subtitle}" />
                </StackPanel>
            </DataTemplate>
            """);

        _logEntryTemplate = CreateDataTemplate(
            """
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Border Margin="0,0,0,10"
                        Padding="14"
                        Background="#F6FFFFFF"
                        BorderBrush="{Binding Severity, Converter={StaticResource LogSeverityToBrushConverter}}"
                        BorderThickness="1"
                        CornerRadius="16">
                    <Border.Transitions>
                        <TransitionCollection>
                            <EntranceThemeTransition FromVerticalOffset="12" />
                        </TransitionCollection>
                    </Border.Transitions>

                    <Grid ColumnSpacing="12">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>

                        <Border Grid.Column="0"
                                Padding="10,4"
                                Background="{Binding Severity, Converter={StaticResource LogSeverityToBrushConverter}}"
                                CornerRadius="999">
                            <TextBlock Foreground="White"
                                       FontSize="11"
                                       FontWeight="SemiBold"
                                       Text="{Binding SeverityText}" />
                        </Border>

                        <StackPanel Grid.Column="1" Spacing="4">
                            <TextBlock Style="{StaticResource BodyTitleStyle}" Text="{Binding Title}" />
                            <TextBlock Style="{StaticResource BodyCaptionStyle}" Text="{Binding Message}" />
                        </StackPanel>

                        <TextBlock Grid.Column="2"
                                   Foreground="{StaticResource MutedForegroundBrush}"
                                   Text="{Binding TimestampText}" />
                    </Grid>
                </Border>
            </DataTemplate>
            """);

        Content = BuildContent();
        DataContext = ViewModel;
    }

    public MainViewModel ViewModel { get; }

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

        var layout = new Grid
        {
            MaxWidth = 1580,
            Padding = new Thickness(28),
            RowSpacing = 18
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        layout.Children.Add(BuildHeroCard());

        var bodyGrid = new Grid
        {
            ColumnSpacing = 18
        };
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(520) });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(bodyGrid, 1);

        var leftColumn = new StackPanel { Spacing = 18 };
        leftColumn.Children.Add(BuildSessionSetupCard());
        leftColumn.Children.Add(BuildTransportSettingsCard());
        Grid.SetColumn(leftColumn, 0);

        var rightColumn = new StackPanel { Spacing = 18 };
        rightColumn.Children.Add(BuildComposerCard());
        rightColumn.Children.Add(BuildLiveActivityCard());
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
        var card = CreateCard(fromVerticalOffset: 48);
        card.Background = GetBrush(DeviceLabTheme.HeroPanelBrushKey);
        card.BorderBrush = CreateSolid("#25FFFFFF");

        var grid = new Grid { ColumnSpacing = 18 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI Variable Display Semib"),
            FontSize = 34,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey),
            Text = "CommLib Device Lab"
        });
        content.Children.Add(new TextBlock
        {
            MaxWidth = 760,
            FontSize = 15,
            Foreground = CreateSolid("#E8F8FBFD"),
            TextWrapping = TextWrapping.WrapWholeWords,
            Text = "A WinUI transport bench built with strict MVVM and DI. Connect over TCP, UDP, multicast, or serial, send MessageModel frames, and watch live traffic in a structured event stream."
        });

        var badgeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        badgeRow.Children.Add(CreateBadge("RuntimePolicyText", true));
        badgeRow.Children.Add(CreateBadge("ProtocolBadgeText", true));
        badgeRow.Children.Add(CreateBadge("SerializerBadgeText", true));
        content.Children.Add(badgeRow);

        var statusCard = new Border
        {
            MinWidth = 280,
            Padding = new Thickness(16),
            Background = CreateSolid("#18FFFFFF"),
            BorderBrush = CreateSolid("#14FFFFFF"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20)
        };

        var statusStack = new StackPanel { Spacing = 10 };
        var statusHeader = new StackPanel
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
        statusHeader.Children.Add(progress);

        var labelStack = new StackPanel();
        labelStack.Children.Add(new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#CCF8FBFD"),
            Text = "SESSION STATUS"
        });
        var statusText = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey)
        };
        Bind(statusText, TextBlock.TextProperty, "StatusText");
        labelStack.Children.Add(statusText);

        statusHeader.Children.Add(labelStack);
        statusStack.Children.Add(statusHeader);

        var statusDetail = new TextBlock
        {
            Foreground = CreateSolid("#DDF8FBFD"),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        Bind(statusDetail, TextBlock.TextProperty, "StatusDetail");
        statusStack.Children.Add(statusDetail);
        statusCard.Child = statusStack;

        grid.Children.Add(content);
        Grid.SetColumn(statusCard, 1);
        grid.Children.Add(statusCard);

        card.Child = grid;
        return card;
    }

    private UIElement BuildSessionSetupCard()
    {
        var card = CreateCard(fromHorizontalOffset: -32);
        var content = new StackPanel { Spacing = 16 };

        content.Children.Add(CreateCardHeader(
            "Session Setup",
            "Define the session identity, timeout policy, and transport family before opening a line."));

        content.Children.Add(CreateFieldLabel("Transport"));

        var transportCombo = CreateComboBox();
        transportCombo.ItemTemplate = _transportChoiceTemplate;
        Bind(transportCombo, ItemsControl.ItemsSourceProperty, "TransportChoices");
        Bind(transportCombo, Selector.SelectedItemProperty, "SelectedTransport", BindingMode.TwoWay);
        content.Children.Add(transportCombo);

        var grid = CreateTwoColumnFormGrid();
        AddToGrid(grid, CreateLabeledInput("Device Id", CreateTextBox("DeviceId")), 0, 0);
        AddToGrid(grid, CreateLabeledInput("Display Name", CreateTextBox("DisplayName")), 0, 1);
        AddToGrid(grid, CreateLabeledInput("Default Timeout (ms)", CreateTextBox("DefaultTimeoutMs")), 1, 0);
        AddToGrid(grid, CreateLabeledInput("Max Pending Requests", CreateTextBox("MaxPendingRequests")), 1, 1);
        content.Children.Add(grid);

        card.Child = content;
        return card;
    }

    private UIElement BuildTransportSettingsCard()
    {
        var card = CreateCard(fromHorizontalOffset: -12);
        var content = new StackPanel { Spacing = 16 };

        content.Children.Add(CreateBoundHeader("SelectedTransportTitle", "SelectedTransportSubtitle"));

        content.Children.Add(CreateTcpPanel());
        content.Children.Add(CreateUdpPanel());
        content.Children.Add(CreateMulticastPanel());
        content.Children.Add(CreateSerialPanel());

        card.Child = content;
        return card;
    }

    private UIElement BuildComposerCard()
    {
        var card = CreateCard(fromHorizontalOffset: 24);
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(CreateCardHeader(
            "Message Composer",
            "Create an outbound message and send it over the active session. The peer side must speak LengthPrefixed plus AutoBinary framing."));

        var messageGrid = new Grid { ColumnSpacing = 12 };
        messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        messageGrid.Children.Add(CreateLabeledInput("Message Id", CreateTextBox("OutboundMessageId")));
        var bodyInput = CreateLabeledInput("Body", CreateTextBox("OutboundBody", height: 140, acceptsReturn: true));
        Grid.SetColumn(bodyInput, 1);
        messageGrid.Children.Add(bodyInput);
        content.Children.Add(messageGrid);

        var buttonGrid = new Grid { ColumnSpacing = 12 };
        for (var index = 0; index < 4; index++)
        {
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        AddButton(buttonGrid, 0, "Connect", "ConnectCommand", DeviceLabTheme.PrimaryButtonStyleKey);
        AddButton(buttonGrid, 1, "Disconnect", "DisconnectCommand", DeviceLabTheme.SecondaryButtonStyleKey);
        AddButton(buttonGrid, 2, "Send", "SendCommand", DeviceLabTheme.SecondaryButtonStyleKey);
        AddButton(buttonGrid, 3, "Clear Log", "ClearLogCommand", DeviceLabTheme.SecondaryButtonStyleKey);
        content.Children.Add(buttonGrid);

        var badgeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        badgeRow.Children.Add(CreateBadge("ProtocolBadgeText"));
        badgeRow.Children.Add(CreateBadge("SerializerBadgeText"));
        badgeRow.Children.Add(CreateBadge(staticText: "Strict MVVM"));
        content.Children.Add(badgeRow);

        card.Child = content;
        return card;
    }

    private UIElement BuildLiveActivityCard()
    {
        var card = CreateCard(fromHorizontalOffset: 42);
        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(CreateCardHeader(
            "Live Activity",
            "All connection lifecycle events and inbound traffic are appended here from the session service."));

        var logList = new ListView
        {
            Height = 780,
            IsItemClickEnabled = false,
            SelectionMode = ListViewSelectionMode.None,
            ItemTemplate = _logEntryTemplate,
            Style = GetStyle(DeviceLabTheme.ActivityListStyleKey)
        };
        Bind(logList, ItemsControl.ItemsSourceProperty, "Logs");
        content.Children.Add(logList);

        card.Child = content;
        return card;
    }

    private UIElement CreateTcpPanel()
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddToGrid(grid, CreateLabeledInput("Host", CreateTextBox("TcpSettings.Host")), 0, 0);
        AddToGrid(grid, CreateLabeledInput("Port", CreateTextBox("TcpSettings.Port")), 0, 1);
        AddToGrid(grid, CreateLabeledInput("Connect Timeout (ms)", CreateTextBox("TcpSettings.ConnectTimeoutMs")), 1, 0);
        AddToGrid(grid, CreateLabeledInput("Buffer Size", CreateTextBox("TcpSettings.BufferSize")), 1, 1);

        var noDelay = CreateCheckBox("Disable Nagle (NoDelay)", "TcpSettings.NoDelay");
        Grid.SetRow(noDelay, 2);
        Grid.SetColumnSpan(noDelay, 2);
        grid.Children.Add(noDelay);

        return CreateTransportPanel("IsTcpSelected", grid);
    }

    private UIElement CreateUdpPanel()
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        AddToGrid(grid, CreateLabeledInput("Local Port", CreateTextBox("UdpSettings.LocalPort")), 0, 0);
        AddToGrid(grid, CreateLabeledInput("Remote Host", CreateTextBox("UdpSettings.RemoteHost")), 0, 1);
        AddToGrid(grid, CreateLabeledInput("Remote Port", CreateTextBox("UdpSettings.RemotePort")), 0, 2);
        return CreateTransportPanel("IsUdpSelected", grid);
    }

    private UIElement CreateMulticastPanel()
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddToGrid(grid, CreateLabeledInput("Group Address", CreateTextBox("MulticastSettings.GroupAddress")), 0, 0);
        AddToGrid(grid, CreateLabeledInput("Port", CreateTextBox("MulticastSettings.Port")), 0, 1);
        AddToGrid(grid, CreateLabeledInput("TTL", CreateTextBox("MulticastSettings.Ttl")), 0, 2);
        AddToGrid(grid, CreateLabeledInput("Local Interface (optional)", CreateTextBox("MulticastSettings.LocalInterface")), 1, 0);

        var loopback = CreateCheckBox("Enable loopback", "MulticastSettings.Loopback");
        loopback.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(loopback, 1);
        Grid.SetColumn(loopback, 1);
        Grid.SetColumnSpan(loopback, 2);
        grid.Children.Add(loopback);

        return CreateTransportPanel("IsMulticastSelected", grid);
    }

    private UIElement CreateSerialPanel()
    {
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddToGrid(grid, CreateLabeledInput("Port Name", CreateTextBox("SerialSettings.PortName")), 0, 0);
        AddToGrid(grid, CreateLabeledInput("Baud Rate", CreateTextBox("SerialSettings.BaudRate")), 0, 1);
        AddToGrid(grid, CreateLabeledInput("Data Bits", CreateTextBox("SerialSettings.DataBits")), 0, 2);
        AddToGrid(grid, CreateLabeledInput("Parity", CreateComboBox("SerialSettings.ParityOptions", "SerialSettings.Parity")), 1, 0);
        AddToGrid(grid, CreateLabeledInput("Stop Bits", CreateComboBox("SerialSettings.StopBitsOptions", "SerialSettings.StopBits")), 1, 1);
        AddToGrid(grid, CreateLabeledInput("Turn Gap (ms)", CreateTextBox("SerialSettings.TurnGapMs")), 1, 2);
        AddToGrid(grid, CreateLabeledInput("Read Buffer Size", CreateTextBox("SerialSettings.ReadBufferSize")), 2, 0);
        AddToGrid(grid, CreateLabeledInput("Write Buffer Size", CreateTextBox("SerialSettings.WriteBufferSize")), 2, 1);

        var halfDuplex = CreateCheckBox("Use half-duplex timing", "SerialSettings.HalfDuplex");
        halfDuplex.VerticalAlignment = VerticalAlignment.Bottom;
        Grid.SetRow(halfDuplex, 2);
        Grid.SetColumn(halfDuplex, 2);
        grid.Children.Add(halfDuplex);

        return CreateTransportPanel("IsSerialSelected", grid);
    }

    private Border CreateTransportPanel(string visibilityPath, FrameworkElement content)
    {
        var panel = new Border
        {
            Padding = new Thickness(16),
            Background = GetBrush(DeviceLabTheme.TransportPanelBrushKey),
            CornerRadius = new CornerRadius(18),
            Child = content
        };
        panel.Transitions = CreateTransitions(fromVerticalOffset: 18);
        Bind(panel, UIElement.VisibilityProperty, visibilityPath, converter: GetConverter<BooleanToVisibilityConverter>("BoolToVisibilityConverter"));
        return panel;
    }

    private Border CreateCard(double fromHorizontalOffset = 0, double fromVerticalOffset = 0)
    {
        var card = new Border
        {
            Background = GetBrush(DeviceLabTheme.CardBackgroundBrushKey),
            BorderBrush = GetBrush(DeviceLabTheme.CardBorderBrushKey),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(20)
        };
        card.Transitions = CreateTransitions(fromHorizontalOffset, fromVerticalOffset);
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

    private UIElement CreateBoundHeader(string titlePath, string captionPath)
    {
        var stack = new StackPanel { Spacing = 4 };
        var title = new TextBlock
        {
            Style = GetStyle(DeviceLabTheme.SectionTitleStyleKey)
        };
        Bind(title, TextBlock.TextProperty, titlePath);

        var caption = new TextBlock
        {
            Style = GetStyle(DeviceLabTheme.BodyCaptionStyleKey)
        };
        Bind(caption, TextBlock.TextProperty, captionPath);

        stack.Children.Add(title);
        stack.Children.Add(caption);
        return stack;
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

    private Border CreateBadge(string? path = null, bool hero = false, string? staticText = null)
    {
        var badge = new Border
        {
            Style = GetStyle(DeviceLabTheme.BadgeBorderStyleKey)
        };

        if (hero)
        {
            badge.Background = CreateSolid("#1FFFFFFF");
        }

        var text = new TextBlock();
        if (hero)
        {
            text.Foreground = GetBrush(DeviceLabTheme.HeroForegroundBrushKey);
        }

        if (staticText is not null)
        {
            text.Text = staticText;
        }
        else if (path is not null)
        {
            Bind(text, TextBlock.TextProperty, path);
        }

        badge.Child = text;
        return badge;
    }

    private static DataTemplate CreateDataTemplate(string xaml)
    {
        return (DataTemplate)XamlReader.Load(xaml);
    }

    private static Grid CreateTwoColumnFormGrid()
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        return grid;
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
