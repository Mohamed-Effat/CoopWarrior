using System.Runtime.InteropServices;
using CoopWarrior.Pages;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.UI;

namespace CoopWarrior;

public sealed partial class MainWindow : Window
{
    public const string WindowTitle = "Co-op Warrior";

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(IntPtr hWnd);

    readonly DispatcherQueueTimer timer;
    string lastStatus = "";

    /// <summary>Raised about 30 times a second so pages can show live state.</summary>
    public event Action? Tick;

    public MainWindow()
    {
        InitializeComponent();
        Title = WindowTitle;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        var root = (FrameworkElement)Content;
        root.ActualThemeChanged += (_, _) => ColorCaptionButtons();
        ColorCaptionButtons();
        SystemBackdrop = MicaController.IsSupported() ? new MicaBackdrop() : new DesktopAcrylicBackdrop();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "CoopWarrior.ico"));
        SizeAndCenter(1040, 780);

        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(33);
        timer.Tick += (_, _) => OnTick();
        timer.Start();

        Closed += (_, _) =>
        {
            timer.Stop();
            App.Engine.Stop();
            App.Profile.Save();
            App.Store.SaveSettings();
        };

        ShowProfileName();
        App.Store.ActiveChanged += () =>
        {
            ShowProfileName();
            // Pages read the active profile when they are created.
            if (ContentFrame.CurrentSourcePageType is Type page && page != typeof(ProfilesPage) && page != typeof(TutorialPage))
                ContentFrame.Navigate(page, null, new SuppressNavigationTransitionInfo());
        };

        // Bound keys would otherwise also press the focused control, e.g. Space flipping the on/off switch.
        ((UIElement)Content).PreviewKeyDown += (_, e) =>
        {
            Core.Engine engine = App.Engine;
            if (engine.Running && !engine.Paused && App.Profile.Keys.Values.Any(k => k.Contains((int)e.Key)))
                e.Handled = true;
        };

        Nav.SelectedItem = App.Store.TutorialSeen ? Nav.MenuItems[0] : Nav.FooterMenuItems[0];

        Core.Updater.Changed += ShowUpdateBar;
        _ = CheckForUpdatesOnStartAsync();
    }

    public void ShowProfileName() => ProfileText.Text = App.Profile.Name;

    /// <summary>Checks GitHub at most once every 20 hours, and only when the user has not turned checks off.</summary>
    static async Task CheckForUpdatesOnStartAsync()
    {
        Core.ProfileStore store = App.Store;
        if (!Core.Updater.IsInstalled || !store.CheckForUpdates) return;
        if (DateTime.UtcNow - store.LastUpdateCheck.ToUniversalTime() < TimeSpan.FromHours(20)) return;
        await Core.Updater.CheckAsync();
        if (Core.Updater.Status != Core.UpdateStatus.Failed)
        {
            store.LastUpdateCheck = DateTime.UtcNow;
            store.SaveSettings();
        }
    }

    void ShowUpdateBar()
    {
        switch (Core.Updater.Status)
        {
            case Core.UpdateStatus.Available:
                UpdateBar.Severity = InfoBarSeverity.Informational;
                UpdateBar.Title = "Update available";
                UpdateBar.Message = $"Co-op Warrior {Core.Updater.AvailableVersion} is ready to download. " +
                                    "Your profiles and settings are kept.";
                UpdateBarButton.IsEnabled = true;
                UpdateBar.IsOpen = true;
                break;
            case Core.UpdateStatus.Downloading:
                UpdateBar.Title = "Downloading the update";
                UpdateBar.Message = $"{Core.Updater.DownloadPercent}%. Co-op Warrior restarts when it is done.";
                UpdateBarButton.IsEnabled = false;
                UpdateBar.IsOpen = true;
                break;
            case Core.UpdateStatus.Failed when UpdateBar.IsOpen:
                UpdateBar.Severity = InfoBarSeverity.Error;
                UpdateBar.Title = "The update did not finish";
                UpdateBar.Message = Core.Updater.Error;
                UpdateBarButton.IsEnabled = true;
                break;
        }
    }

    void UpdateBarButton_Click(object sender, RoutedEventArgs e) => _ = UpdateNowAsync();

    /// <summary>Downloads and applies the available update; the virtual pad is removed before the restart.</summary>
    public static Task UpdateNowAsync() => Core.Updater.UpdateAndRestartAsync(() =>
    {
        App.Engine.Stop();
        App.Profile.Save();
        App.Store.SaveSettings();
    });

    /// <summary>Colors the minimize, maximize and close buttons to match the app theme.</summary>
    /// <remarks>With the content extended into the title bar, Windows 10 draws them in the system theme's colors,
    /// which disappear when the app and system themes differ.</remarks>
    void ColorCaptionButtons()
    {
        bool dark = ((FrameworkElement)Content).ActualTheme == ElementTheme.Dark;
        AppWindowTitleBar bar = AppWindow.TitleBar;
        bar.ButtonBackgroundColor = Colors.Transparent;
        bar.ButtonInactiveBackgroundColor = Colors.Transparent;
        bar.ButtonForegroundColor = dark ? Colors.White : Colors.Black;
        bar.ButtonInactiveForegroundColor = dark ? Color.FromArgb(255, 150, 150, 150) : Color.FromArgb(255, 110, 110, 110);
        bar.ButtonHoverBackgroundColor = dark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(18, 0, 0, 0);
        bar.ButtonHoverForegroundColor = dark ? Colors.White : Colors.Black;
        bar.ButtonPressedBackgroundColor = dark ? Color.FromArgb(14, 255, 255, 255) : Color.FromArgb(10, 0, 0, 0);
        bar.ButtonPressedForegroundColor = dark ? Colors.White : Colors.Black;
    }

    void SizeAndCenter(int width, int height)
    {
        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        double scale = GetDpiForWindow(hwnd) / 96.0;
        var size = new SizeInt32((int)(width * scale), (int)(height * scale));
        RectInt32 work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        size.Width = Math.Min(size.Width, work.Width);
        size.Height = Math.Min(size.Height, work.Height);
        AppWindow.MoveAndResize(new RectInt32(
            work.X + (work.Width - size.Width) / 2,
            work.Y + (work.Height - size.Height) / 2,
            size.Width, size.Height));
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.PreferredMinimumWidth = (int)(720 * scale);
            p.PreferredMinimumHeight = (int)(560 * scale);
        }
    }

    public void Navigate(string tag)
    {
        foreach (object item in Nav.MenuItems.Concat(Nav.FooterMenuItems))
        {
            if (item is NavigationViewItem n && (string)n.Tag == tag)
            {
                Nav.SelectedItem = n;
                return;
            }
        }
    }

    void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        Type page = (string)item.Tag switch
        {
            "Bindings" => typeof(BindingsPage),
            "Mouse" => typeof(MousePage),
            "Profiles" => typeof(ProfilesPage),
            "About" => typeof(AboutPage),
            "Tutorial" => typeof(TutorialPage),
            _ => typeof(DashboardPage),
        };
        if (ContentFrame.CurrentSourcePageType != page)
            ContentFrame.Navigate(page, null, new EntranceNavigationTransitionInfo());
    }

    void OnTick()
    {
        var engine = App.Engine;
        // The F12 hotkey ends the engine loop; Stop releases the virtual pad afterwards.
        if (!engine.Running) engine.Stop();
        string status = !engine.Running ? "Off" : engine.Paused ? "Paused" : "On";
        if (status != lastStatus)
        {
            lastStatus = status;
            StatusText.Text = status;
            StatusDot.Fill = (Brush)Application.Current.Resources[status switch
            {
                "On" => "SystemFillColorSuccessBrush",
                "Paused" => "SystemFillColorCautionBrush",
                _ => "ControlStrongFillColorDefaultBrush",
            }];
        }
        Tick?.Invoke();
    }
}
