using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CommLib.Examples.WinUI.Styles;

public static class DeviceLabTheme
{
    public const string WindowBackgroundBrushKey = "WindowBackgroundBrush";
    public const string HeroPanelBrushKey = "HeroPanelBrush";
    public const string HeroForegroundBrushKey = "HeroForegroundBrush";
    public const string CardBackgroundBrushKey = "CardBackgroundBrush";
    public const string CardBorderBrushKey = "CardBorderBrush";
    public const string MutedForegroundBrushKey = "MutedForegroundBrush";
    public const string SectionForegroundBrushKey = "SectionForegroundBrush";
    public const string AccentBrushKey = "AccentBrush";
    public const string BadgeBackgroundBrushKey = "BadgeBackgroundBrush";
    public const string TransportPanelBrushKey = "TransportPanelBrush";
    public const string CardBorderStyleKey = "CardBorderStyle";
    public const string SectionTitleStyleKey = "SectionTitleStyle";
    public const string BodyTitleStyleKey = "BodyTitleStyle";
    public const string BodyCaptionStyleKey = "BodyCaptionStyle";
    public const string FieldLabelStyleKey = "FieldLabelStyle";
    public const string BadgeBorderStyleKey = "BadgeBorderStyle";
    public const string PrimaryButtonStyleKey = "PrimaryButtonStyle";
    public const string SecondaryButtonStyleKey = "SecondaryButtonStyle";
    public const string TextInputStyleKey = "TextInputStyle";
    public const string ComboInputStyleKey = "ComboInputStyle";
    public const string InlineToggleStyleKey = "InlineToggleStyle";
    public const string ActivityListStyleKey = "ActivityListStyle";

    public static ResourceDictionary Create()
    {
        var resources = new ResourceDictionary();

        resources[WindowBackgroundBrushKey] = CreateGradient("#FFF7FAFC", "#FFE8F0F7", "#FFDCE9F5");
        resources[HeroPanelBrushKey] = CreateGradient("#FF0A4F7D", "#FF124B72", "#FF184E77");
        resources[HeroForegroundBrushKey] = CreateSolid("#FFF8FBFD");
        resources[CardBackgroundBrushKey] = CreateSolid("#F7FFFFFF");
        resources[CardBorderBrushKey] = CreateSolid("#1E234A66");
        resources[MutedForegroundBrushKey] = CreateSolid("#FF4D6678");
        resources[SectionForegroundBrushKey] = CreateSolid("#FF0F2534");
        resources[AccentBrushKey] = CreateSolid("#FF0A6AA1");
        resources[BadgeBackgroundBrushKey] = CreateSolid("#140A6AA1");
        resources[TransportPanelBrushKey] = CreateSolid("#0F0A6AA1");

        resources[CardBorderStyleKey] = CreateCardBorderStyle();
        resources[SectionTitleStyleKey] = CreateSectionTitleStyle();
        resources[BodyTitleStyleKey] = CreateBodyTitleStyle();
        resources[BodyCaptionStyleKey] = CreateBodyCaptionStyle();
        resources[FieldLabelStyleKey] = CreateFieldLabelStyle();
        resources[BadgeBorderStyleKey] = CreateBadgeBorderStyle();
        resources[PrimaryButtonStyleKey] = CreatePrimaryButtonStyle();
        resources[SecondaryButtonStyleKey] = CreateSecondaryButtonStyle();
        resources[TextInputStyleKey] = CreateTextInputStyle();
        resources[ComboInputStyleKey] = CreateComboInputStyle();
        resources[InlineToggleStyleKey] = CreateInlineToggleStyle();
        resources[ActivityListStyleKey] = CreateActivityListStyle();

        return resources;
    }

    public static T Get<T>(FrameworkElement owner, string key) where T : class
    {
        return (T)owner.Resources[key];
    }

    private static Style CreateCardBorderStyle()
    {
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BackgroundProperty, CreateSolid("#F7FFFFFF")));
        style.Setters.Add(new Setter(Border.BorderBrushProperty, CreateSolid("#1E234A66")));
        style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(22)));
        style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(20)));
        return style;
    }

    private static Style CreateSectionTitleStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new FontFamily("Segoe UI Variable Display Semib")));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 22d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF0F2534")));
        return style;
    }

    private static Style CreateBodyTitleStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new FontFamily("Segoe UI Variable Display")));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 15d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF0F2534")));
        return style;
    }

    private static Style CreateBodyCaptionStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWholeWords));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF4D6678")));
        return style;
    }

    private static Style CreateFieldLabelStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF4D6678")));
        style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 0, 0, 6)));
        return style;
    }

    private static Style CreateBadgeBorderStyle()
    {
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BackgroundProperty, CreateSolid("#140A6AA1")));
        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(999)));
        style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10, 4, 10, 4)));
        return style;
    }

    private static Style CreatePrimaryButtonStyle()
    {
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FF0A6AA1")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#FF0A6AA1")));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(14)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FFFFFFFF")));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 10, 16, 10)));
        style.Setters.Add(new Setter(Control.MinHeightProperty, 42d));
        return style;
    }

    private static Style CreateSecondaryButtonStyle()
    {
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#EFFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#26364E60")));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(14)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF0A4F7D")));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 10, 16, 10)));
        style.Setters.Add(new Setter(Control.MinHeightProperty, 42d));
        return style;
    }

    private static Style CreateTextInputStyle()
    {
        var style = new Style(typeof(TextBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FCFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#2A355872")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(14)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF0F2534")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 9, 12, 9)));
        return style;
    }

    private static Style CreateComboInputStyle()
    {
        var style = new Style(typeof(ComboBox));
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FCFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#2A355872")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(14)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF0F2534")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        return style;
    }

    private static Style CreateInlineToggleStyle()
    {
        var style = new Style(typeof(CheckBox));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF0F2534")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2)));
        style.Setters.Add(new Setter(Control.MinHeightProperty, 32d));
        return style;
    }

    private static Style CreateActivityListStyle()
    {
        var style = new Style(typeof(ListView));
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#0AFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#14304A66")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(20)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12)));
        return style;
    }

    private static SolidColorBrush CreateSolid(string hex)
    {
        return new(ColorFromHex(hex));
    }

    private static LinearGradientBrush CreateGradient(string start, string middle, string end)
    {
        return new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
            GradientStops =
            {
                new GradientStop { Offset = 0, Color = ColorFromHex(start) },
                new GradientStop { Offset = 0.55, Color = ColorFromHex(middle) },
                new GradientStop { Offset = 1, Color = ColorFromHex(end) }
            }
        };
    }

    private static Color ColorFromHex(string hex)
    {
        var value = hex.TrimStart('#');
        if (value.Length != 8)
        {
            throw new InvalidOperationException($"Unsupported color format: {hex}");
        }

        return Color.FromArgb(
            Convert.ToByte(value.Substring(0, 2), 16),
            Convert.ToByte(value.Substring(2, 2), 16),
            Convert.ToByte(value.Substring(4, 2), 16),
            Convert.ToByte(value.Substring(6, 2), 16));
    }
}
