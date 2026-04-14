using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CommLib.Examples.WinUI.Styles;

/// <summary>
/// DeviceLabTheme 타입입니다.
/// </summary>
public static class DeviceLabTheme
{
    // 지금 실제로 쓰는 것은 brush/text/border 위주의 "안전한 subset"이다.
    // 아래에 남아 있는 broader control style helper는 추후 검증 전까지는 설계 여지를 보존하는 용도다.
    /// <summary>
    /// SharedResources 값을 나타냅니다.
    /// </summary>
    private static readonly Lazy<ResourceDictionary> SharedResources = new(Create);

    /// <summary>
    /// WindowBackgroundBrushKey 상수입니다.
    /// </summary>
    public const string WindowBackgroundBrushKey = "WindowBackgroundBrush";
    /// <summary>
    /// HeroPanelBrushKey 상수입니다.
    /// </summary>
    public const string HeroPanelBrushKey = "HeroPanelBrush";
    /// <summary>
    /// HeroForegroundBrushKey 상수입니다.
    /// </summary>
    public const string HeroForegroundBrushKey = "HeroForegroundBrush";
    /// <summary>
    /// CardBackgroundBrushKey 상수입니다.
    /// </summary>
    public const string CardBackgroundBrushKey = "CardBackgroundBrush";
    /// <summary>
    /// CardBorderBrushKey 상수입니다.
    /// </summary>
    public const string CardBorderBrushKey = "CardBorderBrush";
    /// <summary>
    /// MutedForegroundBrushKey 상수입니다.
    /// </summary>
    public const string MutedForegroundBrushKey = "MutedForegroundBrush";
    /// <summary>
    /// SectionForegroundBrushKey 상수입니다.
    /// </summary>
    public const string SectionForegroundBrushKey = "SectionForegroundBrush";
    /// <summary>
    /// AccentBrushKey 상수입니다.
    /// </summary>
    public const string AccentBrushKey = "AccentBrush";
    /// <summary>
    /// BadgeBackgroundBrushKey 상수입니다.
    /// </summary>
    public const string BadgeBackgroundBrushKey = "BadgeBackgroundBrush";
    /// <summary>
    /// TransportPanelBrushKey 상수입니다.
    /// </summary>
    public const string TransportPanelBrushKey = "TransportPanelBrush";
    /// <summary>
    /// CardBorderStyleKey 상수입니다.
    /// </summary>
    public const string CardBorderStyleKey = "CardBorderStyle";
    /// <summary>
    /// SectionTitleStyleKey 상수입니다.
    /// </summary>
    public const string SectionTitleStyleKey = "SectionTitleStyle";
    /// <summary>
    /// BodyTitleStyleKey 상수입니다.
    /// </summary>
    public const string BodyTitleStyleKey = "BodyTitleStyle";
    /// <summary>
    /// BodyCaptionStyleKey 상수입니다.
    /// </summary>
    public const string BodyCaptionStyleKey = "BodyCaptionStyle";
    /// <summary>
    /// FieldLabelStyleKey 상수입니다.
    /// </summary>
    public const string FieldLabelStyleKey = "FieldLabelStyle";
    /// <summary>
    /// BadgeBorderStyleKey 상수입니다.
    /// </summary>
    public const string BadgeBorderStyleKey = "BadgeBorderStyle";
    /// <summary>
    /// PrimaryButtonStyleKey 상수입니다.
    /// </summary>
    public const string PrimaryButtonStyleKey = "PrimaryButtonStyle";
    /// <summary>
    /// SecondaryButtonStyleKey 상수입니다.
    /// </summary>
    public const string SecondaryButtonStyleKey = "SecondaryButtonStyle";
    /// <summary>
    /// TextInputStyleKey 상수입니다.
    /// </summary>
    public const string TextInputStyleKey = "TextInputStyle";
    /// <summary>
    /// ComboInputStyleKey 상수입니다.
    /// </summary>
    public const string ComboInputStyleKey = "ComboInputStyle";
    /// <summary>
    /// InlineToggleStyleKey 상수입니다.
    /// </summary>
    public const string InlineToggleStyleKey = "InlineToggleStyle";
    /// <summary>
    /// ActivityListStyleKey 상수입니다.
    /// </summary>
    public const string ActivityListStyleKey = "ActivityListStyle";

    /// <summary>
    /// Shared 값을 가져옵니다.
    /// </summary>
    public static ResourceDictionary Shared => SharedResources.Value;

    /// <summary>
    /// Create 작업을 수행합니다.
    /// </summary>
    public static ResourceDictionary Create()
    {
        // 코드 기반 View들이 공통 look & feel을 쉽게 공유할 수 있도록
        // App 전역 merge 대신 지연 생성되는 ResourceDictionary 하나에 모아 둔다.
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

    public static T Get<T>(FrameworkElement owner, string key) where T : class
    {
        _ = owner;
        return (T)Shared[key];
    }

    /// <summary>
    /// CreateCardBorderStyle 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// CreateSectionTitleStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateSectionTitleStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 22d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF12364F")));
        return style;
    }

    /// <summary>
    /// CreateBodyTitleStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateBodyTitleStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF12364F")));
        return style;
    }

    /// <summary>
    /// CreateBodyCaptionStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateBodyCaptionStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13d));
        style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWholeWords));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF4D6678")));
        return style;
    }

    /// <summary>
    /// CreateFieldLabelStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateFieldLabelStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12d));
        style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, CreateSolid("#FF4D6678")));
        style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 0, 0, 6)));
        return style;
    }

    /// <summary>
    /// CreateBadgeBorderStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateBadgeBorderStyle()
    {
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BackgroundProperty, CreateSolid("#140A6AA1")));
        style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(999)));
        style.Setters.Add(new Setter(Border.PaddingProperty, new Thickness(10, 4, 10, 4)));
        return style;
    }

    /// <summary>
    /// CreatePrimaryButtonStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreatePrimaryButtonStyle()
    {
        var style = CreateBasedOnStyle(typeof(Button), "DefaultButtonStyle");
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FF0B6AA2")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#FF0B6AA2")));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(12)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FFFFFFFF")));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 10, 16, 10)));
        style.Setters.Add(new Setter(Control.MinHeightProperty, 40d));
        return style;
    }

    /// <summary>
    /// CreateSecondaryButtonStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateSecondaryButtonStyle()
    {
        var style = CreateBasedOnStyle(typeof(Button), "DefaultButtonStyle");
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FFFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#FFC2D3E1")));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(12)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF12364F")));
        style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(16, 10, 16, 10)));
        style.Setters.Add(new Setter(Control.MinHeightProperty, 40d));
        return style;
    }

    /// <summary>
    /// CreateTextInputStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateTextInputStyle()
    {
        var style = CreateBasedOnStyle(typeof(TextBox), "DefaultTextBoxStyle");
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FFFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#FFC2D3E1")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(12)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF12364F")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 9, 12, 9)));
        return style;
    }

    /// <summary>
    /// CreateComboInputStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateComboInputStyle()
    {
        var style = CreateBasedOnStyle(typeof(ComboBox), "DefaultComboBoxStyle");
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#FFFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#FFC2D3E1")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(12)));
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF12364F")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        return style;
    }

    /// <summary>
    /// CreateInlineToggleStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateInlineToggleStyle()
    {
        var style = CreateBasedOnStyle(typeof(CheckBox), "DefaultCheckBoxStyle");
        style.Setters.Add(new Setter(Control.ForegroundProperty, CreateSolid("#FF0F2534")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2)));
        style.Setters.Add(new Setter(Control.MinHeightProperty, 32d));
        return style;
    }

    /// <summary>
    /// CreateActivityListStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateActivityListStyle()
    {
        var style = CreateBasedOnStyle(typeof(ListView), "DefaultListViewStyle");
        style.Setters.Add(new Setter(Control.BackgroundProperty, CreateSolid("#0AFFFFFF")));
        style.Setters.Add(new Setter(Control.BorderBrushProperty, CreateSolid("#14304A66")));
        style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(20)));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12)));
        return style;
    }

    /// <summary>
    /// CreateSolid 작업을 수행합니다.
    /// </summary>
    private static SolidColorBrush CreateSolid(string hex)
    {
        return new(ColorFromHex(hex));
    }

    /// <summary>
    /// CreateBasedOnStyle 작업을 수행합니다.
    /// </summary>
    private static Style CreateBasedOnStyle(Type targetType, string defaultStyleKey)
    {
        return new Style(targetType)
        {
            BasedOn = (Style)Microsoft.UI.Xaml.Application.Current.Resources[defaultStyleKey]
        };
    }

    /// <summary>
    /// CreateGradient 작업을 수행합니다.
    /// </summary>
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

    /// <summary>
    /// ColorFromHex 작업을 수행합니다.
    /// </summary>
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