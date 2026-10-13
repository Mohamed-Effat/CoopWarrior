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
            App.Config.Save();
            App.Store.SaveSettings();
        };

        ShowConfigName();
        App.Store.ActiveChanged += () =>
        {
            ShowConfigName();
            // Pages read the active config when they are created.
            if (ContentFrame.CurrentSourcePageType is Type page && page != typeof(ConfigsPage) && page != typeof(TutorialPage))
                ContentFrame.Navigate(page, null, new SuppressNavigationTransitionInfo());
        };

        // Bound keys would otherwise also press the focused control, e.g. Space flipping the on/off switch.
        ((UIElement)Content).PreviewKeyDown += (_, e) =>
        {
            Core.Engine engine = App.Engine;
            if (engine.Running && !engine.Paused && App.Config.Keys.Values.Any(k => k.Contains((int)e.Key)))
                e.Handled = true;
        };

        Nav.SelectedItem = App.Store.TutorialSeen ? Nav.MenuItems[0] : Nav.FooterMenuItems[0];
    }

    public void ShowConfigName() => ConfigText.Text = App.Config.Name;

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
            "Configs" => typeof(ConfigsPage),
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
