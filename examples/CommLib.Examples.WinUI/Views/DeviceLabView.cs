using CommLib.Examples.WinUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CommLib.Examples.WinUI.Views;

public sealed class DeviceLabView : Grid
{
    public DeviceLabView(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContext = ViewModel;
        Children.Add(BuildContent());
    }

    public MainViewModel ViewModel { get; }

    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = CreateSolid("#FFF3F7FB")
        };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var layout = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16
        };

        layout.Children.Add(CreateHeroCard());
        layout.Children.Add(CreateSessionCard());
        layout.Children.Add(CreateTransportCard());
        layout.Children.Add(CreateComposerCard());
        layout.Children.Add(CreateLogCard());

        scrollViewer.Content = layout;
        root.Children.Add(scrollViewer);
        return root;
    }

    private UIElement CreateHeroCard()
    {
        var card = CreateCard("#FF0F4A6A", foregroundHex: "#FFFFFFFF");
        var stack = CreateVerticalStack(8);
        stack.Children.Add(new TextBlock
        {
            Text = "Device Lab",
            FontSize = 30,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FFFFFFFF")
        });

        stack.Children.Add(new TextBlock
        {
            Text = "TCP, UDP, Multicast, Serial transport sessions can be opened here with shared MVVM settings.",
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = CreateSolid("#FFD4E7F5")
        });

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

    private UIElement CreateSessionCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("Session Setup"));

        var transport = new ComboBox
        {
            DisplayMemberPath = "Label",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 40
        };
        Bind(transport, ItemsControl.ItemsSourceProperty, "Settings.TransportChoices");
        Bind(transport, Selector.SelectedItemProperty, "Settings.SelectedTransport", BindingMode.TwoWay);
        stack.Children.Add(CreateLabeledField("Transport", transport));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("Device Id", CreateTextBox("Settings.DeviceId")),
            CreateLabeledField("Display Name", CreateTextBox("Settings.DisplayName"))));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("Default Timeout (ms)", CreateTextBox("Settings.DefaultTimeoutMs")),
            CreateLabeledField("Max Pending Requests", CreateTextBox("Settings.MaxPendingRequests"))));

        card.Child = stack;
        return card;
    }

    private UIElement CreateTransportCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("Transport Settings"));

        stack.Children.Add(CreateTransportPanel("TCP", [
            CreateLabeledField("Host", CreateTextBox("Settings.TcpSettings.Host")),
            CreateLabeledField("Port", CreateTextBox("Settings.TcpSettings.Port")),
            CreateLabeledField("Connect Timeout (ms)", CreateTextBox("Settings.TcpSettings.ConnectTimeoutMs")),
            CreateLabeledField("Buffer Size", CreateTextBox("Settings.TcpSettings.BufferSize")),
            CreateCheckBox("Disable Nagle (NoDelay)", "Settings.TcpSettings.NoDelay")
        ]));

        stack.Children.Add(CreateTransportPanel("UDP", [
            CreateLabeledField("Local Port", CreateTextBox("Settings.UdpSettings.LocalPort")),
            CreateLabeledField("Remote Host", CreateTextBox("Settings.UdpSettings.RemoteHost")),
            CreateLabeledField("Remote Port", CreateTextBox("Settings.UdpSettings.RemotePort"))
        ]));

        stack.Children.Add(CreateTransportPanel("Multicast", [
            CreateLabeledField("Group Address", CreateTextBox("Settings.MulticastSettings.GroupAddress")),
            CreateLabeledField("Port", CreateTextBox("Settings.MulticastSettings.Port")),
            CreateLabeledField("TTL", CreateTextBox("Settings.MulticastSettings.Ttl")),
            CreateLabeledField("Local Interface", CreateTextBox("Settings.MulticastSettings.LocalInterface")),
            CreateCheckBox("Enable loopback", "Settings.MulticastSettings.Loopback")
        ]));

        stack.Children.Add(CreateTransportPanel("Serial", [
            CreateLabeledField("Port Name", CreateTextBox("Settings.SerialSettings.PortName")),
            CreateLabeledField("Baud Rate", CreateTextBox("Settings.SerialSettings.BaudRate")),
            CreateLabeledField("Data Bits", CreateTextBox("Settings.SerialSettings.DataBits")),
            CreateLabeledField("Parity", CreateComboBox("Settings.SerialSettings.ParityOptions", "Settings.SerialSettings.Parity")),
            CreateLabeledField("Stop Bits", CreateComboBox("Settings.SerialSettings.StopBitsOptions", "Settings.SerialSettings.StopBits")),
            CreateLabeledField("Turn Gap (ms)", CreateTextBox("Settings.SerialSettings.TurnGapMs")),
            CreateLabeledField("Read Buffer Size", CreateTextBox("Settings.SerialSettings.ReadBufferSize")),
            CreateLabeledField("Write Buffer Size", CreateTextBox("Settings.SerialSettings.WriteBufferSize")),
            CreateCheckBox("Use half-duplex timing", "Settings.SerialSettings.HalfDuplex")
        ]));

        card.Child = stack;
        return card;
    }

    private UIElement CreateComposerCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("Message Composer"));

        stack.Children.Add(CreateTwoColumnRow(
            CreateLabeledField("Message Id", CreateTextBox("Settings.OutboundMessageId")),
            CreateLabeledField("Body", CreateTextBox("Settings.OutboundBody", 120, true))));

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        buttons.Children.Add(CreateCommandButton("Connect", "ConnectCommand", "#FF0B6AA2", "#FFFFFFFF"));
        buttons.Children.Add(CreateCommandButton("Disconnect", "DisconnectCommand"));
        buttons.Children.Add(CreateCommandButton("Send", "SendCommand"));
        buttons.Children.Add(CreateCommandButton("Clear Log", "ClearLogCommand"));
        stack.Children.Add(buttons);

        card.Child = stack;
        return card;
    }

    private UIElement CreateLogCard()
    {
        var card = CreateCard();
        var stack = CreateVerticalStack(12);
        stack.Children.Add(CreateSectionTitle("Live Log"));

        var logBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 320,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Bind(logBox, TextBox.TextProperty, "LogText");
        stack.Children.Add(logBox);

        card.Child = stack;
        return card;
    }

    private Border CreateTransportPanel(string title, IEnumerable<UIElement> children)
    {
        var border = new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            BorderBrush = CreateSolid("#FFCAD8E6"),
            Background = CreateSolid("#FFF8FBFE")
        };

        var stack = CreateVerticalStack(10);
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FF12364F")
        });

        foreach (var child in children)
        {
            stack.Children.Add(child);
        }

        border.Child = stack;
        return border;
    }

    private Border CreateCard(string backgroundHex = "#FFFFFFFF", string foregroundHex = "#FF12364F")
    {
        return new Border
        {
            Padding = new Thickness(18),
            Background = CreateSolid(backgroundHex),
            BorderBrush = CreateSolid("#FFD5E2EE"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18)
        };
    }

    private TextBlock CreateSectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FF12364F")
        };
    }

    private StackPanel CreateVerticalStack(double spacing)
    {
        return new StackPanel { Spacing = spacing };
    }

    private FrameworkElement CreateLabeledField(string label, FrameworkElement input)
    {
        var stack = CreateVerticalStack(6);
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = CreateSolid("#FF4E6780")
        });
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
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Bind(textBox, TextBox.TextProperty, path, BindingMode.TwoWay);
        return textBox;
    }

    private ComboBox CreateComboBox(string itemsSourcePath, string selectedPath)
    {
        var comboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinHeight = 40
        };
        Bind(comboBox, ItemsControl.ItemsSourceProperty, itemsSourcePath);
        Bind(comboBox, Selector.SelectedItemProperty, selectedPath, BindingMode.TwoWay);
        return comboBox;
    }

    private CheckBox CreateCheckBox(string content, string path)
    {
        var checkBox = new CheckBox
        {
            Content = content
        };
        Bind(checkBox, ToggleButton.IsCheckedProperty, path, BindingMode.TwoWay);
        return checkBox;
    }

    private Button CreateCommandButton(string label, string commandPath, string backgroundHex = "#FFFFFFFF", string foregroundHex = "#FF12364F")
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(16, 10, 16, 10),
            Background = CreateSolid(backgroundHex),
            Foreground = CreateSolid(foregroundHex),
            BorderBrush = CreateSolid("#FFC2D3E1"),
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
