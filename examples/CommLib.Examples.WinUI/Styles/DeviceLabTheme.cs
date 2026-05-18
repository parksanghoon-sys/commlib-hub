using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CommLib.Examples.WinUI.Styles;

public static class DeviceLabTheme
{
    private static readonly Lazy<ResourceDictionary> SharedResources = new(Create);

    public const string WindowBackgroundBrushKey = "WindowBackgroundBrush";
    public const string HeroPanelBrushKey = "HeroPanelBrush";
    public const string HeroForegroundBrushKey = "HeroForegroundBrush";
    public const string CardBackgroundBrushKey = "CardBackgroundBrush";
    public const string CardBorderBrushKey = "CardBorderBrush";
    public const string MutedForegroundBrushKey = "MutedForegroundBrush";
    public const string SectionForegroundBrushKey = "SectionForegroundBrush";
    public const string AccentBrushKey = "AccentBrush";
    public const string TransportPanelBrushKey = "TransportPanelBrush";
    public const string CardBorderStyleKey = "CardBorderStyle";
    public const string SectionTitleStyleKey = "SectionTitleStyle";
    public const string BodyTitleStyleKey = "BodyTitleStyle";
    public const string BodyCaptionStyleKey = "BodyCaptionStyle";
    public const string FieldLabelStyleKey = "FieldLabelStyle";

    public static ResourceDictionary Shared => SharedResources.Value;

    public static ResourceDictionary Create()
    {
        // Keep only the resources that are actively consumed by the code-built views.
        var resources = new ResourceDictionary();

        resources[WindowBackgroundBrushKey] = CreateSolid("#FFF3F7FB");
        resources[HeroPanelBrushKey] = CreateSolid("#FF123B5A");
        resources[HeroForegroundBrushKey] = CreateSolid("#FFFFFFFF");
        resources[CardBackgroundBrushKey] = CreateSolid("#FFFFFFFF");
        resources[CardBorderBrushKey] = CreateSolid("#FFD5E2EE");
        resources[MutedForegroundBrushKey] = CreateSolid("#FF4E6780");
        resources[SectionForegroundBrushKey] = CreateSolid("#FF12364F");
        resources[AccentBrushKey] = CreateSolid("#FF0B6AA2");
        resources[TransportPanelBrushKey] = CreateSolid("#FFF8FBFE");

        resources[CardBorderStyleKey] = CreateCardBorderStyle();
        resources[SectionTitleStyleKey] = CreateSectionTitleStyle();
        resources[BodyTitleStyleKey] = CreateBodyTitleStyle();
        resources[BodyCaptionStyleKey] = CreateBodyCaptionStyle();
        resources[FieldLabelStyleKey] = CreateFieldLabelStyle();

        return resources;
    }

    public static T Get<T>(string key) where T : class
    {
        return (T)Shared[key];
    }

    private static Style CreateCardBorderStyle()
    {
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BackgroundProperty, CreateSolid("#FFFFFFFF")));
        style.Setters.Add(new Setter(Border.BorderBrushProperty, CreateSolid("#FFD5E2EE")));
        style.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(18)));
        style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(18)));
        return style;
    }

    private static Style CreateSectionTitleStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 22d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF12364F")));
        return style;
    }

    private static Style CreateBodyTitleStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF12364F")));
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

    private static SolidColorBrush CreateSolid(string hex)
    {
        return new(ColorFromHex(hex));
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
