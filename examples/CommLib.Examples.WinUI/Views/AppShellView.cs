using CommLib.Examples.WinUI.Models;
using CommLib.Examples.WinUI.Services;
using CommLib.Examples.WinUI.Styles;
using CommLib.Examples.WinUI.ViewModels;
using System.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace CommLib.Examples.WinUI.Views;

/// <summary>
/// AppShellView 타입입니다.
/// </summary>
public sealed class AppShellView : Grid
{
    /// <summary>
    /// _localizer 값을 나타냅니다.
    /// </summary>
    private readonly IAppLocalizer _localizer;
    /// <summary>
    /// _localizedTextUpdates 값을 나타냅니다.
    /// </summary>
    private readonly List<Action> _localizedTextUpdates = [];
    // 두 페이지를 계속 살아 있게 두는 dual-host 구조를 유지하면
    // 과거 WinUI blank-screen 성격의 리스크를 줄이면서도 가벼운 전환 애니메이션을 얹을 수 있다.
    /// <summary>
    /// _deviceLabHost 값을 나타냅니다.
    /// </summary>
    private Grid _deviceLabHost = null!;
    /// <summary>
    /// _settingsHost 값을 나타냅니다.
    /// </summary>
    private Grid _settingsHost = null!;
    /// <summary>
    /// _activePageKind 값을 나타냅니다.
    /// </summary>
    private ShellPageKind _activePageKind;
    /// <summary>
    /// _pendingPageKind 값을 나타냅니다.
    /// </summary>
    private ShellPageKind? _pendingPageKind;
    /// <summary>
    /// _isTransitionRunning 값을 나타냅니다.
    /// </summary>
    private bool _isTransitionRunning;

    /// <summary>
    /// <see cref="AppShellView"/>의 새 인스턴스를 초기화합니다.
    /// </summary>
    public AppShellView(
        ShellViewModel viewModel,
        DeviceLabView deviceLabView,
        SettingsView settingsView,
        IAppLocalizer localizer)
    {
        _localizer = localizer;
        ViewModel = viewModel;
        DeviceLabView = deviceLabView;
        SettingsView = settingsView;
        _activePageKind = ViewModel.SelectedPage.Kind;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContext = ViewModel;
        _localizer.LanguageChanged += OnLanguageChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Children.Add(BuildContent());
        ApplyActivePageState(_activePageKind);
        ApplyLocalizedText();
    }

    /// <summary>
    /// ViewModel 값을 가져옵니다.
    /// </summary>
    public ShellViewModel ViewModel { get; }

    /// <summary>
    /// DeviceLabView 값을 가져옵니다.
    /// </summary>
    public DeviceLabView DeviceLabView { get; }

    /// <summary>
    /// SettingsView 값을 가져옵니다.
    /// </summary>
    public SettingsView SettingsView { get; }

    /// <summary>
    /// OnLanguageChanged 작업을 수행합니다.
    /// </summary>
    private void OnLanguageChanged(object? sender, EventArgs args)
    {
        ApplyLocalizedText();
    }

    /// <summary>
    /// OnViewModelPropertyChanged 작업을 수행합니다.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ShellViewModel.SelectedPage))
        {
            QueuePageTransition(ViewModel.SelectedPage.Kind);
        }
    }

    /// <summary>
    /// BuildContent 작업을 수행합니다.
    /// </summary>
    private FrameworkElement BuildContent()
    {
        var root = new Grid
        {
            Background = GetThemeBrush(DeviceLabTheme.WindowBackgroundBrushKey)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Border
        {
            Margin = new Thickness(20, 20, 20, 0),
            Padding = new Thickness(20),
            Background = GetThemeBrush(DeviceLabTheme.HeroPanelBrushKey),
            CornerRadius = new CornerRadius(20)
        };

        var headerGrid = new Grid { ColumnSpacing = 16 };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleStack = new StackPanel { Spacing = 4 };
        var appTitle = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetThemeBrush(DeviceLabTheme.HeroForegroundBrushKey)
        };
        RegisterLocalizedText(() => appTitle.Text = _localizer.Get("shell.appTitle"));
        titleStack.Children.Add(appTitle);

        var pageTitle = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetThemeBrush(DeviceLabTheme.HeroForegroundBrushKey)
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
        nav.Children.Add(CreateHeaderButton("shell.nav.deviceLab", "OpenDeviceLabPageCommand"));
        nav.Children.Add(CreateHeaderButton("shell.nav.settings", "OpenSettingsPageCommand"));
        Grid.SetColumn(nav, 1);
        headerGrid.Children.Add(nav);

        header.Child = headerGrid;
        root.Children.Add(header);

        var contentHost = new Grid
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(contentHost, 1);

        _deviceLabHost = CreatePageHost(DeviceLabView);
        _settingsHost = CreatePageHost(SettingsView);

        contentHost.Children.Add(_deviceLabHost);
        contentHost.Children.Add(_settingsHost);
        root.Children.Add(contentHost);
        return root;
    }

    /// <summary>
    /// QueuePageTransition 작업을 수행합니다.
    /// </summary>
    private void QueuePageTransition(ShellPageKind targetPageKind)
    {
        // 전환 도중에 탭을 연속으로 눌러도 마지막 요청만 반영되게 해서
        // 애니메이션 겹침이나 host 상태 꼬임을 막는다.
        _pendingPageKind = targetPageKind;
        if (_isTransitionRunning)
        {
            return;
        }

        _ = ProcessQueuedTransitionsAsync();
    }

    /// <summary>
    /// ProcessQueuedTransitionsAsync 작업을 수행합니다.
    /// </summary>
    private async Task ProcessQueuedTransitionsAsync()
    {
        _isTransitionRunning = true;

        try
        {
            while (_pendingPageKind is ShellPageKind nextPageKind)
            {
                _pendingPageKind = null;
                if (nextPageKind == _activePageKind)
                {
                    continue;
                }

                await TransitionToPageAsync(nextPageKind).ConfigureAwait(true);
            }
        }
        finally
        {
            _isTransitionRunning = false;
        }
    }

    /// <summary>
    /// TransitionToPageAsync 작업을 수행합니다.
    /// </summary>
    private async Task TransitionToPageAsync(ShellPageKind nextPageKind)
    {
        var outgoingHost = GetPageHost(_activePageKind);
        var incomingHost = GetPageHost(nextPageKind);
        // 방향값은 "설정으로 가는지 / 디바이스 랩으로 돌아오는지"만 표현한다.
        // 실제 애니메이션은 이 부호만 써서 slide 방향을 반전한다.
        var direction = nextPageKind == ShellPageKind.Settings ? 1 : -1;

        PrepareIncomingHost(incomingHost, direction);
        PrepareOutgoingHost(outgoingHost);

        SetHostZIndex(outgoingHost, 0);
        SetHostZIndex(incomingHost, 1);

        await RunTransitionAsync(outgoingHost, incomingHost, direction).ConfigureAwait(true);

        CompleteTransition(outgoingHost, incomingHost);
        _activePageKind = nextPageKind;
    }

    /// <summary>
    /// ApplyActivePageState 작업을 수행합니다.
    /// </summary>
    private void ApplyActivePageState(ShellPageKind activePageKind)
    {
        ApplyHostState(_deviceLabHost, activePageKind == ShellPageKind.DeviceLab);
        ApplyHostState(_settingsHost, activePageKind == ShellPageKind.Settings);
    }

    /// <summary>
    /// ApplyHostState 작업을 수행합니다.
    /// </summary>
    private static void ApplyHostState(Grid host, bool isVisible)
    {
        host.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        host.Opacity = isVisible ? 1 : 0;
        GetPageTransform(host).X = 0;
        SetHostZIndex(host, isVisible ? 1 : 0);
    }

    /// <summary>
    /// CreatePageHost 작업을 수행합니다.
    /// </summary>
    private static Grid CreatePageHost(FrameworkElement content)
    {
        // View를 매번 다시 만들지 않고 host 안에 유지해야
        // 입력 상태, 스크롤 위치, 바인딩된 객체 수명이 예측 가능하게 남는다.
        var host = new Grid
        {
            Visibility = Visibility.Collapsed,
            Opacity = 0,
            RenderTransform = new TranslateTransform()
        };
        host.Children.Add(content);
        return host;
    }

    /// <summary>
    /// GetPageHost 작업을 수행합니다.
    /// </summary>
    private Grid GetPageHost(ShellPageKind pageKind)
    {
        return pageKind switch
        {
            ShellPageKind.DeviceLab => _deviceLabHost,
            ShellPageKind.Settings => _settingsHost,
            _ => throw new InvalidOperationException($"Unsupported page kind: {pageKind}")
        };
    }

    /// <summary>
    /// PrepareOutgoingHost 작업을 수행합니다.
    /// </summary>
    private static void PrepareOutgoingHost(Grid host)
    {
        host.Visibility = Visibility.Visible;
        host.Opacity = 1;
        GetPageTransform(host).X = 0;
    }

    /// <summary>
    /// PrepareIncomingHost 작업을 수행합니다.
    /// </summary>
    private static void PrepareIncomingHost(Grid host, int direction)
    {
        host.Visibility = Visibility.Visible;
        host.Opacity = 0;
        GetPageTransform(host).X = 32 * direction;
    }

    /// <summary>
    /// CompleteTransition 작업을 수행합니다.
    /// </summary>
    private static void CompleteTransition(Grid outgoingHost, Grid incomingHost)
    {
        outgoingHost.Visibility = Visibility.Collapsed;
        outgoingHost.Opacity = 0;
        GetPageTransform(outgoingHost).X = 0;

        incomingHost.Visibility = Visibility.Visible;
        incomingHost.Opacity = 1;
        GetPageTransform(incomingHost).X = 0;
    }

    /// <summary>
    /// RunTransitionAsync 작업을 수행합니다.
    /// </summary>
    private static Task RunTransitionAsync(Grid outgoingHost, Grid incomingHost, int direction)
    {
        var storyboard = new Storyboard();
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        storyboard.Children.Add(CreateOpacityAnimation(outgoingHost, 1, 0, easing));
        storyboard.Children.Add(CreateOpacityAnimation(incomingHost, 0, 1, easing));
        storyboard.Children.Add(CreateTranslateAnimation(GetPageTransform(outgoingHost), 0, -32 * direction, easing));
        storyboard.Children.Add(CreateTranslateAnimation(GetPageTransform(incomingHost), 32 * direction, 0, easing));

        // Storyboard는 awaitable이 아니므로 완료 시점을 Task로 감싸서
        // 상위 전환 큐가 "정확히 한 번의 전환이 끝난 뒤" 다음 요청을 처리하게 만든다.
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += (_, _) => completion.TrySetResult(null);
        storyboard.Begin();
        return completion.Task;
    }

    /// <summary>
    /// CreateOpacityAnimation 작업을 수행합니다.
    /// </summary>
    private static DoubleAnimation CreateOpacityAnimation(UIElement target, double from, double to, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = easing
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Opacity");
        return animation;
    }

    /// <summary>
    /// CreateTranslateAnimation 작업을 수행합니다.
    /// </summary>
    private static DoubleAnimation CreateTranslateAnimation(TranslateTransform transform, double from, double to, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, "X");
        return animation;
    }

    /// <summary>
    /// GetPageTransform 작업을 수행합니다.
    /// </summary>
    private static TranslateTransform GetPageTransform(Grid host)
    {
        return (TranslateTransform)host.RenderTransform;
    }

    /// <summary>
    /// SetHostZIndex 작업을 수행합니다.
    /// </summary>
    private static void SetHostZIndex(Grid host, int zIndex)
    {
        host.SetValue(Canvas.ZIndexProperty, zIndex);
    }

    /// <summary>
    /// CreateHeaderButton 작업을 수행합니다.
    /// </summary>
    private Button CreateHeaderButton(string labelKey, string commandPath)
    {
        var button = new Button
        {
            MinWidth = 120,
            Padding = new Thickness(16, 10, 16, 10),
            CornerRadius = new CornerRadius(12),
            Background = CreateSolid("#1FFFFFFF"),
            Foreground = GetThemeBrush(DeviceLabTheme.HeroForegroundBrushKey),
            BorderBrush = CreateSolid("#33FFFFFF"),
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
}