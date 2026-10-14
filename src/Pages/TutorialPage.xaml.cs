using CoopWarrior.Controls;
using CoopWarrior.Core;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace CoopWarrior.Pages;

/// <summary>The animated walkthrough. Each step plays a small looping scene on a 640 x 300 stage.</summary>
public sealed partial class TutorialPage : Page
{
    sealed record Step(string Title, string Body, Func<CancellationToken, Task> Play);

    readonly Step[] steps;
    int index = -1;
    CancellationTokenSource? cts;
    Path? cursor;

    public TutorialPage()
    {
        InitializeComponent();
        Stage.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, 640, 300) };

        steps =
        [
            new("Install the controller driver (once on each PC)",
                "Co-op Warrior needs the free ViGEmBus driver. If the Dashboard shows a yellow message saying the driver is not installed, " +
                "click Install driver. When Windows asks for permission, click Yes. When the driver is installed, the message turns green.",
                SceneDriver),
            new("Choose your player number",
                "Your keyboard can be any player from 1 to 4. Choose the number under Player number on the Dashboard. " +
                "If you choose Next free player, the keyboard takes the first free number. In that case, connect your real controller first " +
                "if it should be Player 1.",
                SceneRealPad),
            new("Turn your keyboard into a controller",
                "On the Dashboard, turn on \"Turn keyboard into a controller\". Windows now sees one more controller: your keyboard and mouse. " +
                "The dot at the top of the window is green while it is on.",
                SceneToggle),
            new("Pick the profile for your game",
                "A profile holds the keys and mouse settings for one game. Choose your game under Game profile on the Dashboard. " +
                "On the Profiles page you can also make new profiles, and import or export profile files.",
                SceneProfiles),
            new("Start the game and choose two players",
                "Start the game after your keyboard is a controller. Then choose a two-player or split-screen mode. " +
                $"Each player presses Start on their own controller to join. On the keyboard, Start is {Keys("Start")}.",
                SceneSplitScreen),
            new("Play with your keyboard",
                "W A S D move and the arrow keys turn the camera. Space is A, and J, K and L are X, Y and B. " +
                "Q and E are the bumpers, and Shift and Ctrl are the triggers. Your mouse buttons work too.",
                ScenePlay),
            new("Look around with the mouse (optional)",
                "On the Mouse page, turn on \"Mouse moves a stick\". The mouse then turns the camera like in a PC game. " +
                "The faster you move the mouse, the further the stick tilts. You can change the sensitivity on the same page.",
                SceneMouse),
            new("Change any key",
                "Open the Key bindings page and click a row. Press the new key, or click with a mouse button, then choose Replace. " +
                "To keep the old key as well, choose Add as extra key.",
                SceneRebind),
            new("Pause and turn off with hotkeys",
                $"{Keys("PauseKey")} pauses the keyboard controller so you can type, and pressing it again resumes. {Keys("ExitKey")} turns it off. " +
                "Both hotkeys work while the game is in front. Closing Co-op Warrior also turns the keyboard controller off.",
                SceneHotkeys),
        ];
        Pips.NumberOfPages = steps.Length;

        Loaded += (_, _) => Show(0);
        Unloaded += (_, _) => cts?.Cancel();
    }

    static string Keys(string name) => string.Join(" or ", App.Profile.Get(name).Select(KeyNames.Label));

    static string Key(string name, string fallback)
    {
        int[] k = App.Profile.Get(name);
        return k.Length > 0 ? KeyNames.Label(k[0]) : fallback;
    }

    // ---------- navigation ----------

    async void Show(int i)
    {
        if (i == index || i < 0 || i >= steps.Length) return;
        bool forward = i > index;
        index = i;
        cts?.Cancel();
        cts = new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        Pips.SelectedPageIndex = i;
        BackButton.IsEnabled = i > 0;
        NextButton.Content = i == steps.Length - 1 ? "Get started" : "Next";
        StepNumber.Text = $"STEP {i + 1} OF {steps.Length}";
        StepTitle.Text = steps[i].Title;
        StepBody.Text = steps[i].Body;

        await Anim.Fade(Stage, 0, 120);
        if (ct.IsCancellationRequested) return;
        Stage.Children.Clear();
        cursor = null;
        Anim.Translate(Stage).X = forward ? 30 : -30;
        _ = Anim.Fade(Stage, 1, 220);
        _ = Anim.To(Anim.Translate(Stage), "X", 0, 320);

        try
        {
            await steps[i].Play(ct);
        }
        catch (OperationCanceledException) { }
    }

    void Back_Click(object sender, RoutedEventArgs e) => Show(index - 1);

    void Next_Click(object sender, RoutedEventArgs e)
    {
        if (index < steps.Length - 1)
        {
            Show(index + 1);
            return;
        }
        App.Store.TutorialSeen = true;
        App.Store.SaveSettings();
        App.Window?.Navigate("Dashboard");
    }

    void Pips_SelectedIndexChanged(PipsPager sender, PipsPagerSelectedIndexChangedEventArgs args) => Show(sender.SelectedPageIndex);

    // ---------- stage helpers ----------

    T Add<T>(T e, double x, double y) where T : UIElement
    {
        Canvas.SetLeft(e, x);
        Canvas.SetTop(e, y);
        Stage.Children.Add(e);
        return e;
    }

    static TextBlock Text(string s, double size = 14, bool bold = false, string brush = "TextFillColorPrimaryBrush") => new()
    {
        Text = s,
        FontSize = size,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Foreground = Anim.Brush(brush),
    };

    static FontIcon Glyph(string glyph, double size = 16, string brush = "TextFillColorPrimaryBrush") => new()
    {
        Glyph = glyph,
        FontSize = size,
        Foreground = Anim.Brush(brush),
    };

    static Border Card(double w, double h, UIElement? child = null, string bg = "SolidBackgroundFillColorSecondaryBrush") => new()
    {
        Width = w,
        Height = h,
        Child = child,
        Background = Anim.Brush(bg),
        BorderBrush = Anim.Brush("CardStrokeColorDefaultBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
    };

    static Border FakeButton(string label, bool accent = false) => new()
    {
        Padding = new Thickness(14, 6, 14, 6),
        CornerRadius = new CornerRadius(4),
        Background = Anim.Brush(accent ? "AccentFillColorDefaultBrush" : "ControlFillColorDefaultBrush"),
        BorderBrush = Anim.Brush(accent ? "AccentControlElevationBorderBrush" : "ControlElevationBorderBrush"),
        BorderThickness = new Thickness(1),
        Child = Text(label, 13, false, accent ? "TextOnAccentFillColorPrimaryBrush" : "TextFillColorPrimaryBrush"),
    };

    static Border Badge(string label, string brush = "AccentFillColorDefaultBrush") => new()
    {
        Padding = new Thickness(10, 3, 10, 4),
        CornerRadius = new CornerRadius(11),
        Background = Anim.Brush(brush),
        Child = Text(label, 12, true, "TextOnAccentFillColorPrimaryBrush"),
    };

    /// <summary>A small window frame with a title row; the returned grid is its body.</summary>
    Grid MockWindow(double x, double y, double w, double h, string title)
    {
        var body = new Grid { Margin = new Thickness(14, 0, 14, 12) };
        var layout = new Grid { RowDefinitions = { new RowDefinition { Height = new GridLength(34) }, new RowDefinition() } };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        titleRow.Children.Add(Glyph("", 12, "AccentTextFillColorPrimaryBrush"));
        titleRow.Children.Add(Text(title, 12));
        layout.Children.Add(titleRow);
        Grid.SetRow(body, 1);
        layout.Children.Add(body);
        Add(Card(w, h, layout, "SolidBackgroundFillColorBaseBrush"), x, y);
        return body;
    }

    Path Cursor()
    {
        if (cursor != null) return cursor;
        cursor = new Path
        {
            Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M0,0 L0,19 L5,14.5 L8.5,22 L11.5,20.8 L8,13.5 L14.5,13.5 Z"),
            Fill = new SolidColorBrush(Colors.White),
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 1.2,
            RenderTransform = new TranslateTransform { X = 600, Y = 280 },
        };
        Canvas.SetZIndex(cursor, 100);
        Stage.Children.Add(cursor);
        return cursor;
    }

    void PlaceCursor(double x, double y)
    {
        var t = Anim.Translate(Cursor());
        t.X = x;
        t.Y = y;
    }

    Task MoveCursor(double x, double y, int ms = 650) => Anim.Move(Cursor(), x, y, ms);

    /// <summary>Plays a click at the cursor tip: a ripple ring and a quick press.</summary>
    async Task Click(CancellationToken ct)
    {
        var t = Anim.Translate(Cursor());
        var ring = new Ellipse { Width = 26, Height = 26, Stroke = Anim.Brush("AccentFillColorDefaultBrush"), StrokeThickness = 2.5, IsHitTestVisible = false };
        Canvas.SetZIndex(ring, 99);
        Add(ring, t.X - 13, t.Y - 13);
        Anim.SetScale(ring, 0.3);
        _ = Anim.Scale(ring, 1.4, 380);
        _ = Anim.Fade(ring, 0, 380);
        await Anim.To(t, "Y", t.Y + 2, 60);
        await Anim.To(t, "Y", t.Y - 2, 80);
        await Task.Delay(300, ct);
        Stage.Children.Remove(ring);
    }

    /// <summary>Center of an element in stage coordinates, once it has been laid out.</summary>
    Point StageCenter(FrameworkElement e)
    {
        var p = e.TransformToVisual(Stage).TransformPoint(new Windows.Foundation.Point(e.ActualWidth / 2, e.ActualHeight / 2));
        return new Point(p.X, p.Y);
    }

    readonly record struct Point(double X, double Y);

    // ---------- scenes ----------

    async Task SceneDriver(CancellationToken ct)
    {
        Grid body = MockWindow(130, 28, 380, 230, "Co-op Warrior");

        var bar = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 10, 12, 10), VerticalAlignment = VerticalAlignment.Top };
        var barRow = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition(), new ColumnDefinition { Width = GridLength.Auto } } };
        var icon = Glyph("", 16);
        var message = Text("The controller driver is not installed", 13, true);
        message.VerticalAlignment = VerticalAlignment.Center;
        message.TextWrapping = TextWrapping.Wrap;
        var ring = new ProgressRing { Width = 18, Height = 18, IsActive = false };
        var install = FakeButton("Install driver", accent: true);
        Grid.SetColumn(message, 1);
        Grid.SetColumn(install, 2);
        Grid.SetColumn(ring, 2);
        barRow.Children.Add(icon);
        barRow.Children.Add(message);
        barRow.Children.Add(install);
        barRow.Children.Add(ring);
        bar.Child = barRow;
        body.Children.Add(bar);

        var placeholder = new StackPanel { Spacing = 10, Margin = new Thickness(0, 64, 0, 0) };
        foreach (double w in new[] { 300.0, 240, 270 })
            placeholder.Children.Add(new Border { Width = w, Height = 12, CornerRadius = new CornerRadius(6), HorizontalAlignment = HorizontalAlignment.Left, Background = Anim.Brush("ControlFillColorSecondaryBrush") });
        body.Children.Add(placeholder);

        var uacBody = new StackPanel { Spacing = 10, Margin = new Thickness(16) };
        var uacTitle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        uacTitle.Children.Add(Glyph("", 18, "AccentTextFillColorPrimaryBrush"));
        uacTitle.Children.Add(Text("Allow changes to this PC?", 13, true));
        uacBody.Children.Add(uacTitle);
        uacBody.Children.Add(Text("ViGEmBus driver installer", 12, false, "TextFillColorSecondaryBrush"));
        var uacButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        var yes = FakeButton("Yes", accent: true);
        uacButtons.Children.Add(yes);
        uacButtons.Children.Add(FakeButton("No"));
        uacBody.Children.Add(uacButtons);
        var uac = Add(Card(250, 128, uacBody, "SolidBackgroundFillColorTertiaryBrush"), 195, 120);
        Canvas.SetZIndex(uac, 50);

        while (true)
        {
            bar.Background = Anim.Brush("SystemFillColorCautionBackgroundBrush");
            icon.Glyph = "";
            icon.Foreground = Anim.Brush("SystemFillColorCautionBrush");
            message.Text = "The controller driver is not installed";
            install.Visibility = Visibility.Visible;
            ring.IsActive = false;
            uac.Opacity = 0;
            uac.IsHitTestVisible = false;
            PlaceCursor(560, 270);
            await Task.Delay(700, ct);

            Point p = StageCenter(install);
            await MoveCursor(p.X, p.Y);
            await Click(ct);
            install.Visibility = Visibility.Collapsed;
            ring.IsActive = true;
            message.Text = "Installing the controller driver...";
            bar.Background = Anim.Brush("SystemFillColorAttentionBackgroundBrush");
            icon.Glyph = "";
            icon.Foreground = Anim.Brush("AccentTextFillColorPrimaryBrush");
            await Task.Delay(500, ct);

            Anim.SetScale(uac, 0.9);
            _ = Anim.Scale(uac, 1, 250, Anim.Spring);
            await Anim.Fade(uac, 1, 200);
            await Task.Delay(500, ct);
            p = StageCenter(yes);
            await MoveCursor(p.X, p.Y);
            await Click(ct);
            await Anim.Fade(uac, 0, 180);
            await Task.Delay(1100, ct);

            ring.IsActive = false;
            bar.Background = Anim.Brush("SystemFillColorSuccessBackgroundBrush");
            icon.Glyph = "";
            icon.Foreground = Anim.Brush("SystemFillColorSuccessBrush");
            message.Text = "The controller driver is installed";
            Anim.SetScale(icon, 0.4);
            _ = Anim.Scale(icon, 1, 300, Anim.Spring);
            await MoveCursor(560, 270, 900);
            await Task.Delay(1600, ct);
        }
    }

    async Task SceneRealPad(CancellationToken ct)
    {
        var pad = Add(new ControllerView { Width = 240, Height = 180 }, 200, 52);
        var badge = Add(Badge("Player 1"), 282, 248);

        var arcs = new List<Path>();
        for (int i = 0; i < 3; i++)
        {
            double r = 14 + i * 12;
            var arc = new Path
            {
                Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), $"M {-r * 0.7},{-r * 0.7} A {r},{r} 0 0 1 {r * 0.7},{-r * 0.7}"),
                Stroke = Anim.Brush("AccentFillColorDefaultBrush"),
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0,
            };
            arcs.Add(Add(arc, 320, 96));
        }

        while (true)
        {
            pad.Opacity = 0;
            badge.Opacity = 0;
            pad.Show(default);
            Anim.Translate(pad).X = -260;
            await Task.Delay(300, ct);

            _ = Anim.Fade(pad, 1, 400);
            await Anim.To(Anim.Translate(pad), "X", 0, 650, Anim.EaseOut);

            for (int round = 0; round < 2; round++)
            {
                for (int i = 0; i < arcs.Count; i++)
                {
                    _ = Anim.Fade(arcs[i], 1, 150);
                    await Task.Delay(150, ct);
                }
                await Task.Delay(250, ct);
                foreach (Path a in arcs) _ = Anim.Fade(a, 0, 250);
                await Task.Delay(300, ct);
            }

            Anim.SetScale(badge, 0.5);
            _ = Anim.Fade(badge, 1, 200);
            await Anim.Scale(badge, 1, 300, Anim.Spring);

            pad.Show(new PadReport { Buttons = PadButtons.A });
            await Task.Delay(300, ct);
            pad.Show(default);
            await Task.Delay(2000, ct);

            _ = Anim.Fade(badge, 0, 250);
            await Anim.Fade(pad, 0, 300);
        }
    }

    async Task SceneToggle(CancellationToken ct)
    {
        var row = new Grid { ColumnSpacing = 12, Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(Glyph("", 18));
        var label = Text("Turn keyboard into a controller", 14);
        label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(label, 1);
        row.Children.Add(label);

        var track = new Border { Width = 40, Height = 20, CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1) };
        var knob = new Ellipse { Width = 12, Height = 12, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(4, 0, 0, 0) };
        track.Child = knob;
        var stateText = Text("Off", 13, false, "TextFillColorSecondaryBrush");
        stateText.VerticalAlignment = VerticalAlignment.Center;
        var toggle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        toggle.Children.Add(stateText);
        toggle.Children.Add(track);
        Grid.SetColumn(toggle, 2);
        row.Children.Add(toggle);
        Add(Card(420, 58, row), 110, 18);

        var p1 = Add(new ControllerView { Width = 180, Height = 135 }, 120, 100);
        Add(Badge("Player 1"), 172, 246);
        var p2 = Add(new ControllerView { Width = 180, Height = 135 }, 340, 100);
        var p2Badge = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        p2Badge.Children.Add(Badge("Player 2", "SystemFillColorSuccessBrush"));
        p2Badge.Children.Add(Glyph("", 16, "TextFillColorSecondaryBrush"));
        Add(p2Badge, 380, 246);

        while (true)
        {
            track.Background = Anim.Brush("ControlAltFillColorSecondaryBrush");
            track.BorderBrush = Anim.Brush("ControlStrongStrokeColorDefaultBrush");
            knob.Fill = Anim.Brush("ControlStrongFillColorDefaultBrush");
            Anim.Translate(knob).X = 0;
            stateText.Text = "Off";
            p2.Opacity = 0;
            p2Badge.Opacity = 0;
            Anim.Translate(p2).Y = 40;
            PlaceCursor(600, 280);
            await Task.Delay(700, ct);

            Point p = StageCenter(track);
            await MoveCursor(p.X, p.Y);
            await Click(ct);
            track.Background = Anim.Brush("AccentFillColorDefaultBrush");
            track.BorderBrush = Anim.Brush("AccentFillColorDefaultBrush");
            knob.Fill = Anim.Brush("TextOnAccentFillColorPrimaryBrush");
            stateText.Text = "On";
            await Anim.To(Anim.Translate(knob), "X", 20, 180);

            _ = Anim.Fade(p2, 1, 350);
            await Anim.To(Anim.Translate(p2), "Y", 0, 450, Anim.Spring);
            Anim.SetScale(p2Badge, 0.5);
            _ = Anim.Fade(p2Badge, 1, 200);
            await Anim.Scale(p2Badge, 1, 300, Anim.Spring);
            await MoveCursor(600, 280, 800);

            p2.Show(new PadReport { LeftY = 32767 });
            await Task.Delay(500, ct);
            p2.Show(new PadReport { LeftX = 32767 });
            await Task.Delay(500, ct);
            p2.Show(default);
            p1.Show(new PadReport { Buttons = PadButtons.A });
            await Task.Delay(300, ct);
            p1.Show(default);
            await Task.Delay(1800, ct);
        }
    }

    async Task SceneProfiles(CancellationToken ct)
    {
        var title = Text("", 12, false, "TextFillColorSecondaryBrush");
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(Glyph("", 12, "AccentTextFillColorPrimaryBrush"));
        titleRow.Children.Add(Text("Co-op Warrior", 12));
        titleRow.Children.Add(title);
        Add(titleRow, 112, 14);

        string[] names = ["Default", "DYNASTY WARRIORS 3: Complete Edition Remastered", "My other game"];
        var useButtons = new List<Border>();
        var badges = new List<StackPanel>();
        for (int i = 0; i < names.Length; i++)
        {
            var row = new Grid { ColumnSpacing = 12, Margin = new Thickness(14, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(Glyph("", 16));
            var name = Text(names[i], 13);
            name.VerticalAlignment = VerticalAlignment.Center;
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(name, 1);
            row.Children.Add(name);

            var use = FakeButton("Use");
            var badge = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            badge.Children.Add(Glyph("", 13, "AccentTextFillColorPrimaryBrush"));
            badge.Children.Add(Text("In use", 13, true));
            Grid.SetColumn(use, 2);
            Grid.SetColumn(badge, 2);
            row.Children.Add(use);
            row.Children.Add(badge);
            useButtons.Add(use);
            badges.Add(badge);
            Add(Card(440, 54, row), 100, 50 + i * 64);
        }

        void SetActive(int active)
        {
            for (int i = 0; i < names.Length; i++)
            {
                useButtons[i].Visibility = i == active ? Visibility.Collapsed : Visibility.Visible;
                badges[i].Visibility = i == active ? Visibility.Visible : Visibility.Collapsed;
            }
            title.Text = "·  " + names[active];
        }

        while (true)
        {
            SetActive(0);
            PlaceCursor(600, 280);
            await Task.Delay(800, ct);
            Point p = StageCenter(useButtons[1]);
            await MoveCursor(p.X, p.Y);
            await Click(ct);
            SetActive(1);
            Anim.SetScale(badges[1], 0.5);
            _ = Anim.Scale(badges[1], 1, 300, Anim.Spring);
            Anim.SetScale(titleRow, 1.08);
            _ = Anim.Scale(titleRow, 1, 300);
            await MoveCursor(600, 280, 800);
            await Task.Delay(2200, ct);
        }
    }

    async Task SceneMouse(CancellationToken ct)
    {
        var body = new Border
        {
            Width = 72,
            Height = 112,
            CornerRadius = new CornerRadius(36, 36, 32, 32),
            Background = Anim.Brush("SolidBackgroundFillColorQuarternaryBrush"),
            BorderBrush = Anim.Brush("ControlStrongStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(2),
        };
        var parts = new Grid();
        parts.Children.Add(new Rectangle { Width = 2, Height = 44, VerticalAlignment = VerticalAlignment.Top, Fill = Anim.Brush("ControlStrongStrokeColorDefaultBrush") });
        parts.Children.Add(new Rectangle { Width = 8, Height = 18, RadiusX = 4, RadiusY = 4, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 12, 0, 0), Fill = Anim.Brush("AccentFillColorDefaultBrush") });
        body.Child = parts;
        var mouse = Add(body, 120, 80);

        var pad = Add(new ControllerView { Width = 270, Height = 202 }, 340, 30);
        var caption = Add(Text("", 14, true), 0, 252);
        caption.Width = 640;
        caption.TextAlignment = TextAlignment.Center;

        // Each step: how far the mouse moves, how long it takes, the stick tilt meanwhile, and the caption.
        (double dx, double dy, int ms, short rx, short ry, string text)[] script =
        [
            (70, 0, 500, 16000, 0, "Slide right: the camera turns right"),
            (0, 0, 700, 0, 0, "Stop: the stick goes back to the center"),
            (-110, 0, 220, -32767, 0, "Move faster: the stick tilts all the way"),
            (0, 0, 700, 0, 0, "Stop: the stick goes back to the center"),
            (20, -50, 500, 0, 20000, "Up and down work too"),
            (0, 0, 900, 0, 0, "Stop: the stick goes back to the center"),
        ];

        while (true)
        {
            Anim.Translate(mouse).X = 0;
            Anim.Translate(mouse).Y = 0;
            pad.Show(default);
            await Anim.Fade(mouse, 1, 250);
            await Task.Delay(400, ct);
            double x = 0, y = 0;
            foreach (var (dx, dy, ms, rx, ry, text) in script)
            {
                if (caption.Text != text)
                {
                    await Anim.Fade(caption, 0, 100);
                    caption.Text = text;
                    _ = Anim.Fade(caption, 1, 150);
                }
                pad.Show(new PadReport { RightX = rx, RightY = ry });
                if (dx != 0 || dy != 0)
                {
                    x += dx;
                    y += dy;
                    await Anim.Move(mouse, x, y, ms);
                    pad.Show(default);
                }
                else
                {
                    await Task.Delay(ms, ct);
                }
            }
            await Anim.Fade(mouse, 0, 250);
        }
    }

    async Task SceneSplitScreen(CancellationToken ct)
    {
        var screen = new Grid { Width = 380, Height = 214, CornerRadius = new CornerRadius(6), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 24, 28, 38)) };
        var monitor = new Border
        {
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(10),
            Background = Anim.Brush("ControlStrongFillColorDefaultBrush"),
            Child = screen,
        };
        Add(monitor, 130, 12);
        Add(new Rectangle { Width = 70, Height = 24, Fill = Anim.Brush("ControlStrongFillColorDefaultBrush") }, 285, 242);
        Add(new Rectangle { Width = 150, Height = 8, RadiusX = 4, RadiusY = 4, Fill = Anim.Brush("ControlStrongFillColorDefaultBrush") }, 245, 264);

        var menu = new Grid { Width = 180, Height = 90, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var highlight = new Border { Height = 34, CornerRadius = new CornerRadius(4), VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 160, 30)) };
        menu.Children.Add(highlight);
        var items = new StackPanel { Spacing = 14, Padding = new Thickness(0, 7, 0, 0) };
        foreach (string s in new[] { "1 PLAYER", "2 PLAYERS" })
        {
            var t = Text(s, 15, true);
            t.Foreground = new SolidColorBrush(Colors.White);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            items.Children.Add(t);
        }
        menu.Children.Add(items);
        screen.Children.Add(menu);

        var halves = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition() }, Opacity = 0 };
        var left = new Grid { Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 70, 130)) };
        var right = new Grid { Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 110, 60)) };
        Grid.SetColumn(right, 1);
        left.Children.Add(PlayerLabel("P1", ""));
        right.Children.Add(PlayerLabel("P2", ""));
        halves.Children.Add(left);
        halves.Children.Add(right);
        screen.Children.Add(halves);
        var divider = new Rectangle { Width = 3, Fill = new SolidColorBrush(Colors.White), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 0 };
        screen.Children.Add(divider);

        while (true)
        {
            menu.Opacity = 1;
            halves.Opacity = 0;
            divider.Height = 0;
            Anim.Translate(highlight).Y = 0;
            await Task.Delay(900, ct);
            await Anim.To(Anim.Translate(highlight), "Y", 42, 350);
            await Task.Delay(400, ct);
            for (int i = 0; i < 2; i++)
            {
                await Anim.Fade(highlight, 0.4, 120);
                await Anim.Fade(highlight, 1, 120);
            }
            await Anim.Fade(menu, 0, 200);
            await Anim.To(divider, "Height", 214, 350);
            await Anim.Fade(halves, 1, 300);
            await Task.Delay(2600, ct);
            _ = Anim.Fade(divider, 0, 200);
            await Anim.Fade(halves, 0, 300);
            divider.Opacity = 1;
        }

        static UIElement PlayerLabel(string text, string glyph)
        {
            var s = new StackPanel { Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var t = new TextBlock { Text = text, FontSize = 30, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Colors.White), HorizontalAlignment = HorizontalAlignment.Center };
            var g = new FontIcon { Glyph = glyph, FontSize = 22, Foreground = new SolidColorBrush(Colors.White) };
            s.Children.Add(t);
            s.Children.Add(g);
            return s;
        }
    }

    async Task ScenePlay(CancellationToken ct)
    {
        var keyboard = Add(new KeyboardView { Width = 400, Height = 190 }, 10, 30);
        keyboard.ShowKeys(App.Profile,
            ["MoveUp", "MoveDown", "MoveLeft", "MoveRight", "CamUp", "CamDown", "CamLeft", "CamRight", "A", "B", "X", "Y", "LB", "RB", "LT", "RT"]);

        var pad = Add(new ControllerView { Width = 210, Height = 158 }, 420, 46);
        var caption = Add(Text("", 14, true), 0, 252);
        caption.Width = 640;
        caption.TextAlignment = TextAlignment.Center;

        (string[] keys, PadReport report, string text)[] script =
        [
            (["MoveUp"], new PadReport { LeftY = 32767 }, "Move with the left stick"),
            (["MoveLeft"], new PadReport { LeftX = -32767 }, "Move with the left stick"),
            (["MoveDown"], new PadReport { LeftY = -32767 }, "Move with the left stick"),
            (["MoveRight"], new PadReport { LeftX = 32767 }, "Move with the left stick"),
            (["MoveUp", "MoveRight"], new PadReport { LeftX = 23169, LeftY = 23169 }, "Hold two keys to move diagonally"),
            (["CamUp"], new PadReport { RightY = 32767 }, "Arrow keys turn the camera"),
            (["CamLeft"], new PadReport { RightX = -32767 }, "Arrow keys turn the camera"),
            (["A"], new PadReport { Buttons = PadButtons.A }, "Jump or confirm with A"),
            (["X"], new PadReport { Buttons = PadButtons.X }, "Attack with X and Y"),
            (["Y"], new PadReport { Buttons = PadButtons.Y }, "Attack with X and Y"),
            (["B"], new PadReport { Buttons = PadButtons.B }, "B goes back"),
            (["LB"], new PadReport { Buttons = PadButtons.LB }, "Bumpers"),
            (["RB"], new PadReport { Buttons = PadButtons.RB }, "Bumpers"),
            (["LT"], new PadReport { LeftTrigger = 255 }, "Triggers"),
            (["RT"], new PadReport { RightTrigger = 255 }, "Triggers"),
        ];

        while (true)
        {
            foreach (var (keys, report, text) in script)
            {
                if (caption.Text != text)
                {
                    await Anim.Fade(caption, 0, 100);
                    caption.Text = text;
                    _ = Anim.Fade(caption, 1, 150);
                }
                foreach (string k in keys) keyboard.SetPressed(k, true);
                pad.Show(report);
                await Task.Delay(750, ct);
                foreach (string k in keys) keyboard.SetPressed(k, false);
                pad.Show(default);
                await Task.Delay(260, ct);
            }
            await Task.Delay(600, ct);
        }
    }

    async Task SceneRebind(CancellationToken ct)
    {
        string oldKey = Key("MoveUp", "W");
        string newKey = oldKey == "P" ? "O" : "P";

        var row = new Grid { ColumnSpacing = 14, Margin = new Thickness(16, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(Glyph("", 16));
        var label = Text("Move up", 14);
        label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(label, 1);
        row.Children.Add(label);
        var cardKey = new Keycap(oldKey, 13, 32);
        Grid.SetColumn(cardKey, 2);
        row.Children.Add(cardKey);
        var pencil = Glyph("", 14, "TextFillColorSecondaryBrush");
        Grid.SetColumn(pencil, 3);
        row.Children.Add(pencil);
        var card = Add(Card(440, 56, row), 100, 20);

        var dialogBody = new StackPanel { Spacing = 10, Margin = new Thickness(18) };
        dialogBody.Children.Add(Text("Change Move up", 15, true));
        var zone = new Grid { Height = 82, CornerRadius = new CornerRadius(6), Background = Anim.Brush("SubtleFillColorSecondaryBrush") };
        zone.Children.Add(new Rectangle { RadiusX = 6, RadiusY = 6, StrokeDashArray = [4, 3], StrokeThickness = 1.2, Stroke = Anim.Brush("ControlStrongStrokeColorDefaultBrush") });
        var pulse = new Ellipse { Width = 44, Height = 44, Fill = Anim.Brush("AccentFillColorDefaultBrush"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0 };
        zone.Children.Add(pulse);
        var listening = Text("Listening...", 13, true);
        listening.HorizontalAlignment = HorizontalAlignment.Center;
        listening.VerticalAlignment = VerticalAlignment.Center;
        zone.Children.Add(listening);
        var bigKey = new Keycap(newKey, 18, 46) { Accent = true, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        zone.Children.Add(bigKey);
        dialogBody.Children.Add(zone);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
        var replace = FakeButton("Replace", accent: true);
        buttons.Children.Add(replace);
        buttons.Children.Add(FakeButton("Add as extra key"));
        buttons.Children.Add(FakeButton("Cancel"));
        dialogBody.Children.Add(buttons);
        var dialog = Add(Card(340, 196, dialogBody, "SolidBackgroundFillColorTertiaryBrush"), 150, 90);
        Canvas.SetZIndex(dialog, 50);

        var physical = Add(new Keycap(newKey, 16, 40), 560, 200);
        var hand = Add(Text("You press", 12, false, "TextFillColorSecondaryBrush"), 548, 250);

        while (true)
        {
            cardKey.Label = oldKey;
            dialog.Opacity = 0;
            physical.Opacity = 0;
            hand.Opacity = 0;
            bigKey.Opacity = 0;
            listening.Opacity = 1;
            PlaceCursor(600, 120);
            await Task.Delay(700, ct);

            Point p = StageCenter(card);
            await MoveCursor(p.X + 120, p.Y);
            await Click(ct);
            Anim.SetScale(dialog, 0.92);
            _ = Anim.Scale(dialog, 1, 260, Anim.Spring);
            await Anim.Fade(dialog, 1, 200);
            var pulseAnim = Anim.Pulse(pulse);
            await MoveCursor(600, 120, 600);
            await Task.Delay(900, ct);

            _ = Anim.Fade(physical, 1, 200);
            await Anim.Fade(hand, 1, 200);
            physical.IsPressed = true;
            await Task.Delay(180, ct);
            pulseAnim.Stop();
            pulse.Opacity = 0;
            listening.Opacity = 0;
            bigKey.Opacity = 1;
            Anim.SetScale(bigKey, 0.6);
            _ = Anim.Scale(bigKey, 1, 300, Anim.Spring);
            await Task.Delay(250, ct);
            physical.IsPressed = false;
            await Task.Delay(700, ct);

            p = StageCenter(replace);
            await MoveCursor(p.X, p.Y);
            await Click(ct);
            _ = Anim.Fade(physical, 0, 200);
            _ = Anim.Fade(hand, 0, 200);
            await Anim.Fade(dialog, 0, 180);
            cardKey.Label = newKey;
            cardKey.Accent = true;
            Anim.SetScale(cardKey, 0.6);
            await Anim.Scale(cardKey, 1, 300, Anim.Spring);
            await Task.Delay(500, ct);
            cardKey.Accent = false;
            await MoveCursor(600, 120, 700);
            await Task.Delay(1600, ct);
        }
    }

    async Task SceneHotkeys(CancellationToken ct)
    {
        var dot = new Ellipse { Width = 12, Height = 12, VerticalAlignment = VerticalAlignment.Center };
        var label = Text("On", 16, true);
        var chipRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        chipRow.Children.Add(dot);
        chipRow.Children.Add(label);
        var chip = new Border { CornerRadius = new CornerRadius(18), Padding = new Thickness(16, 8, 18, 8), Background = Anim.Brush("ControlFillColorSecondaryBrush"), Child = chipRow };
        Add(chip, 70, 40);

        var pad = Add(new ControllerView { Width = 260, Height = 195 }, 340, 28);
        var pause = Add(new Keycap(Key("PauseKey", "F9"), 18, 54), 70, 150);
        var stop = Add(new Keycap(Key("ExitKey", "F12"), 18, 54), 170, 150);
        Add(Text("pause", 12, false, "TextFillColorSecondaryBrush"), 80, 214);
        Add(Text("stop", 12, false, "TextFillColorSecondaryBrush"), 184, 214);

        async Task Press(Keycap k)
        {
            k.IsPressed = true;
            await Task.Delay(220, ct);
            k.IsPressed = false;
        }

        void State(string text, string brush, double padOpacity)
        {
            label.Text = text;
            dot.Fill = Anim.Brush(brush);
            _ = Anim.Fade(pad, padOpacity, 250);
            Anim.SetScale(chip, 0.9);
            _ = Anim.Scale(chip, 1, 250, Anim.Spring);
        }

        while (true)
        {
            State("On", "SystemFillColorSuccessBrush", 1);
            pad.Show(new PadReport { LeftY = 32767 });
            await Task.Delay(900, ct);
            pad.Show(default);
            await Task.Delay(300, ct);

            await Press(pause);
            State("Paused", "SystemFillColorCautionBrush", 0.45);
            await Task.Delay(1500, ct);

            await Press(pause);
            State("On", "SystemFillColorSuccessBrush", 1);
            pad.Show(new PadReport { Buttons = PadButtons.A });
            await Task.Delay(400, ct);
            pad.Show(default);
            await Task.Delay(900, ct);

            await Press(stop);
            State("Off", "ControlStrongFillColorDefaultBrush", 0);
            await Task.Delay(2000, ct);
        }
    }
}
