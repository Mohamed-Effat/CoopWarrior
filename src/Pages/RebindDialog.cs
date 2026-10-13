using CoopWarrior.Controls;
using CoopWarrior.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace CoopWarrior.Pages;

/// <summary>Listens for one key or mouse button and asks whether it replaces or joins the current keys.</summary>
public sealed class RebindDialog : ContentDialog
{
    readonly Binding binding;
    readonly int[] current;
    readonly Grid zone;
    readonly Grid captureHost;
    readonly TextBlock listening;
    readonly TextBlock conflict;
    readonly Ellipse pulse;
    readonly bool[] held = new bool[256];
    DispatcherQueueTimer? timer;
    Storyboard? pulseAnim;
    int captured;

    public RebindDialog(Binding binding, int[] current)
    {
        this.binding = binding;
        this.current = current;

        Title = "Change " + binding.Label;
        PrimaryButtonText = "Replace";
        SecondaryButtonText = "Add as extra key";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.None;
        IsPrimaryButtonEnabled = false;
        IsSecondaryButtonEnabled = false;

        pulse = new Ellipse
        {
            Width = 64,
            Height = 64,
            Fill = Anim.Brush("AccentFillColorDefaultBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0,
        };
        listening = new TextBlock
        {
            Text = "Listening...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
        };
        captureHost = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        var frame = new Rectangle
        {
            RadiusX = 8,
            RadiusY = 8,
            StrokeThickness = 1.5,
            StrokeDashArray = [4, 3],
            Stroke = Anim.Brush("ControlStrongStrokeColorDefaultBrush"),
        };
        zone = new Grid
        {
            Height = 150,
            Background = Anim.Brush("SubtleFillColorSecondaryBrush"),
            CornerRadius = new CornerRadius(8),
            IsTabStop = true,
            UseSystemFocusVisuals = false,
        };
        zone.Children.Add(frame);
        zone.Children.Add(pulse);
        zone.Children.Add(listening);
        zone.Children.Add(captureHost);
        zone.PointerPressed += Zone_PointerPressed;
        zone.KeyDown += Zone_KeyDown;
        zone.PreviewKeyDown += Zone_KeyDown;

        var currentKeys = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        currentKeys.Children.Add(new TextBlock
        {
            Text = "Now:",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Anim.Brush("TextFillColorSecondaryBrush"),
            Margin = new Thickness(0, 0, 4, 0),
        });
        foreach (int vk in current) currentKeys.Children.Add(new Keycap(KeyNames.Label(vk), 12, 28));
        if (current.Length == 0) currentKeys.Children.Add(new TextBlock { Text = "not set", VerticalAlignment = VerticalAlignment.Center });

        conflict = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Anim.Brush("SystemFillColorCautionBrush"),
            Visibility = Visibility.Collapsed,
        };

        var root = new StackPanel { Spacing = 14, Width = 400 };
        root.Children.Add(new TextBlock
        {
            Text = "Press a key, or click inside the box with the mouse button you want. Esc cancels.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Anim.Brush("TextFillColorSecondaryBrush"),
        });
        root.Children.Add(zone);
        root.Children.Add(conflict);
        root.Children.Add(currentKeys);
        Content = root;

        Opened += (_, _) => StartListening();
        Closed += (_, _) => StopListening();
    }

    /// <summary>Shows the dialog. Returns the new key list, or null if cancelled.</summary>
    public async Task<int[]?> PickAsync()
    {
        ContentDialogResult r = await ShowAsync();
        if (captured == 0) return null;
        return r switch
        {
            ContentDialogResult.Primary => [captured],
            ContentDialogResult.Secondary => current.Contains(captured) ? current : [.. current, captured],
            _ => null,
        };
    }

    void StartListening()
    {
        for (int vk = 0; vk < 256; vk++) held[vk] = Engine.IsDown(vk);
        zone.Focus(FocusState.Programmatic);
        pulseAnim = Anim.Pulse(pulse);
        timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(15);
        timer.Tick += (_, _) => Poll();
        timer.Start();
    }

    void StopListening()
    {
        timer?.Stop();
        pulseAnim?.Stop();
    }

    /// <summary>Reads keys with GetAsyncKeyState, which also sees keys such as Alt, F10 and Tab
    /// that XAML would treat as navigation.</summary>
    void Poll()
    {
        for (int vk = 8; vk < 256; vk++)
        {
            bool down = Engine.IsDown(vk);
            bool wasHeld = held[vk];
            held[vk] = down;
            if (!down || wasHeld || Ignored(vk)) continue;
            Capture(vk);
            return;
        }
    }

    /// <summary>Esc closes the dialog. Left and right modifier codes repeat the generic Shift, Ctrl and Alt codes,
    /// and the Windows keys open the Start menu.</summary>
    static bool Ignored(int vk) => vk is 27 or 91 or 92 or (>= 160 and <= 165) or (>= 0x15 and <= 0x1A) or 0xE5 or 0xFF;

    void Zone_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var p = e.GetCurrentPoint(zone).Properties;
        int vk = p.IsLeftButtonPressed ? 1 : p.IsRightButtonPressed ? 2 : p.IsMiddleButtonPressed ? 4 :
                 p.IsXButton1Pressed ? 5 : p.IsXButton2Pressed ? 6 : 0;
        e.Handled = true;
        zone.Focus(FocusState.Pointer);
        if (vk != 0) Capture(vk);
    }

    /// <summary>Keeps Space, Enter, Tab and the arrows from pressing or moving between the dialog buttons.</summary>
    void Zone_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Escape) e.Handled = true;
    }

    void Capture(int vk)
    {
        captured = vk;
        listening.Visibility = Visibility.Collapsed;
        pulseAnim?.Stop();
        pulse.Opacity = 0;

        captureHost.Children.Clear();
        var cap = new Keycap(KeyNames.Label(vk), 20, 56) { Accent = true };
        captureHost.Children.Add(cap);
        Anim.SetScale(cap, 0.6);
        _ = Anim.Scale(cap, 1, 260, Anim.Spring);

        IsPrimaryButtonEnabled = true;
        IsSecondaryButtonEnabled = !current.Contains(vk);

        string[] others = GameConfig.Bindings.Concat(GameConfig.Hotkeys)
            .Where(b => b.Name != binding.Name && App.Config.Get(b.Name).Contains(vk))
            .Select(b => b.Label).ToArray();
        conflict.Text = others.Length == 0 ? "" : "This key is also used for: " + string.Join(", ", others) + ". Both will react.";
        conflict.Visibility = others.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
}
