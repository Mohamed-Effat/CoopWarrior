using CoopWarrior.Core;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Windows.UI;

namespace CoopWarrior.Controls;

/// <summary>An Xbox controller drawing that shows which buttons are pressed and where the sticks are.</summary>
public sealed class ControllerView : UserControl
{
    const double W = 400, H = 300;

    const string BodyPath =
        "M 120,80 L 280,80 C 330,80 360,100 375,150 C 390,200 400,255 370,275 C 345,292 315,282 300,258 " +
        "L 285,236 C 275,226 262,224 248,224 L 152,224 C 138,224 125,226 115,236 L 100,258 " +
        "C 85,282 55,292 30,275 C 0,255 10,200 25,150 C 40,100 70,80 120,80 Z";

    readonly Canvas canvas = new() { Width = W, Height = H };
    readonly List<(Shape shape, ushort mask, Brush? on)> buttons = [];
    readonly List<TextBlock> faceLetters = [];
    readonly (Ellipse cap, TranslateTransform t, double travel) left, right;
    readonly (Rectangle fill, double height) lt, rt;
    readonly Path body;
    PadReport shown;
    bool first = true;

    static readonly Color GreenA = Color.FromArgb(255, 16, 124, 16);
    static readonly Color RedB = Color.FromArgb(255, 209, 52, 56);
    static readonly Color BlueX = Color.FromArgb(255, 0, 120, 212);
    static readonly Color YellowY = Color.FromArgb(255, 234, 162, 0);

    public ControllerView()
    {
        IsHitTestVisible = false;

        lt = Trigger(85, "LT");
        rt = Trigger(265, "RT");
        Bumper(68, "LB", PadButtons.LB);
        Bumper(242, "RB", PadButtons.RB);

        body = new Path
        {
            Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), BodyPath),
            StrokeThickness = 2,
        };
        canvas.Children.Add(body);

        left = Stick(125, 140, 27, 17, 13, PadButtons.L3);
        right = Stick(240, 192, 23, 15, 11, PadButtons.R3);

        Face(285, 166, "A", GreenA, PadButtons.A);
        Face(311, 140, "B", RedB, PadButtons.B);
        Face(259, 140, "X", BlueX, PadButtons.X);
        Face(285, 114, "Y", YellowY, PadButtons.Y);

        Dpad(165, 192);

        Pill(178, 140, PadButtons.Back);
        Pill(222, 140, PadButtons.Start);

        var guide = new Ellipse { Width = 26, Height = 26, StrokeThickness = 2 };
        guide.Tag = "guide";
        Place(guide, 200 - 13, 108 - 13);
        canvas.Children.Add(guide);

        Content = new Viewbox { Child = canvas, Stretch = Stretch.Uniform };
        ApplyTheme();
        ActualThemeChanged += (_, _) => ApplyTheme();
    }

    static void Place(UIElement e, double x, double y)
    {
        Canvas.SetLeft(e, x);
        Canvas.SetTop(e, y);
    }

    Brush Neutral => Anim.Brush("SolidBackgroundFillColorBaseBrush");
    Brush Accent => Anim.Brush("AccentFillColorDefaultBrush");

    (Rectangle, double) Trigger(double x, string label)
    {
        var frame = new Rectangle { Width = 50, Height = 46, RadiusX = 10, RadiusY = 10, StrokeThickness = 2, Tag = "frame" };
        Place(frame, x, 24);
        canvas.Children.Add(frame);
        var fill = new Rectangle { Width = 46, Height = 0, RadiusX = 8, RadiusY = 8 };
        Place(fill, x + 2, 68);
        canvas.Children.Add(fill);
        AddLabel(label, x, 30, 50, 13);
        return (fill, 42);
    }

    void Bumper(double x, string label, ushort mask)
    {
        var r = new Rectangle { Width = 90, Height = 24, RadiusX = 11, RadiusY = 11, StrokeThickness = 2 };
        Place(r, x, 66);
        canvas.Children.Add(r);
        buttons.Add((r, mask, null));
        AddLabel(label, x, 67, 90, 11);
    }

    void AddLabel(string text, double x, double y, double width, double size)
    {
        var t = new TextBlock
        {
            Text = text,
            Width = width,
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Tag = "label",
        };
        Place(t, x, y);
        canvas.Children.Add(t);
    }

    (Ellipse, TranslateTransform, double) Stick(double cx, double cy, double well, double cap, double travel, ushort click)
    {
        var w = new Ellipse { Width = well * 2, Height = well * 2, StrokeThickness = 2, Tag = "well" };
        Place(w, cx - well, cy - well);
        canvas.Children.Add(w);
        var t = new TranslateTransform();
        var c = new Ellipse { Width = cap * 2, Height = cap * 2, StrokeThickness = 2, RenderTransform = t };
        Place(c, cx - cap, cy - cap);
        canvas.Children.Add(c);
        buttons.Add((c, click, null));
        return (c, t, travel);
    }

    void Face(double cx, double cy, string letter, Color color, ushort mask)
    {
        const double r = 12;
        var e = new Ellipse { Width = r * 2, Height = r * 2, StrokeThickness = 2 };
        Place(e, cx - r, cy - r);
        canvas.Children.Add(e);
        buttons.Add((e, mask, new SolidColorBrush(color)));
        var t = new TextBlock
        {
            Text = letter,
            Width = r * 2,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(color),
            Tag = color,
        };
        Place(t, cx - r, cy - 9);
        canvas.Children.Add(t);
        faceLetters.Add(t);
    }

    void Dpad(double cx, double cy)
    {
        const double a = 14;
        var center = new Rectangle { Width = a, Height = a, Tag = "dpadcenter" };
        Place(center, cx - a / 2, cy - a / 2);
        canvas.Children.Add(center);
        AddArm(cx - a / 2, cy - a * 1.5, PadButtons.DpadUp);
        AddArm(cx - a / 2, cy + a / 2, PadButtons.DpadDown);
        AddArm(cx - a * 1.5, cy - a / 2, PadButtons.DpadLeft);
        AddArm(cx + a / 2, cy - a / 2, PadButtons.DpadRight);

        void AddArm(double x, double y, ushort mask)
        {
            var r = new Rectangle { Width = a, Height = a, RadiusX = 3, RadiusY = 3, StrokeThickness = 1.5 };
            Place(r, x, y);
            canvas.Children.Add(r);
            buttons.Add((r, mask, null));
        }
    }

    void Pill(double cx, double cy, ushort mask)
    {
        var r = new Rectangle { Width = 18, Height = 11, RadiusX = 5.5, RadiusY = 5.5, StrokeThickness = 1.5 };
        Place(r, cx - 9, cy - 5.5);
        canvas.Children.Add(r);
        buttons.Add((r, mask, null));
    }

    void ApplyTheme()
    {
        Brush stroke = Anim.Brush("ControlStrongStrokeColorDefaultBrush");
        Brush text = Anim.Brush("TextFillColorSecondaryBrush");
        body.Fill = Anim.Brush("SolidBackgroundFillColorQuarternaryBrush");
        body.Stroke = stroke;
        foreach (UIElement child in canvas.Children)
        {
            switch (child)
            {
                case Shape s when s.Tag is "well" or "frame" or "guide":
                    s.Fill = Neutral;
                    s.Stroke = stroke;
                    break;
                case Shape s when s.Tag is "dpadcenter":
                    s.Fill = Anim.Brush("ControlStrongFillColorDisabledBrush");
                    break;
                case Shape s:
                    s.Stroke = stroke;
                    break;
                case TextBlock t when t.Tag is "label":
                    t.Foreground = text;
                    break;
            }
        }
        lt.fill.Fill = Accent;
        rt.fill.Fill = Accent;
        first = true;
        Show(shown);
    }

    /// <summary>Draws the given controller state, animating the sticks and buttons toward it.</summary>
    public void Show(PadReport r)
    {
        bool animate = !first;
        first = false;
        if (animate && r.Equals(shown)) return;
        PadReport before = shown;
        shown = r;

        foreach (var (shape, mask, on) in buttons)
        {
            bool down = (r.Buttons & mask) != 0;
            bool wasDown = (before.Buttons & mask) != 0;
            if (animate && down == wasDown) continue;
            shape.Fill = down ? on ?? Accent : Neutral;
        }
        foreach (TextBlock t in faceLetters)
        {
            ushort mask = t.Text switch { "A" => PadButtons.A, "B" => PadButtons.B, "X" => PadButtons.X, _ => PadButtons.Y };
            t.Foreground = (r.Buttons & mask) != 0 ? new SolidColorBrush(Colors.White) : new SolidColorBrush((Color)t.Tag);
        }

        MoveStick(left, r.LeftX, r.LeftY, animate);
        MoveStick(right, r.RightX, r.RightY, animate);
        SetTrigger(lt, r.LeftTrigger, animate);
        SetTrigger(rt, r.RightTrigger, animate);
    }

    void MoveStick((Ellipse cap, TranslateTransform t, double travel) s, short x, short y, bool animate)
    {
        double tx = x / 32767.0 * s.travel;
        double ty = -y / 32767.0 * s.travel;
        bool moved = x != 0 || y != 0;
        if (!buttonsPressed(s.cap)) s.cap.Fill = moved ? Accent : Neutral;
        if (animate)
        {
            _ = Anim.To(s.t, "X", tx, 90);
            _ = Anim.To(s.t, "Y", ty, 90);
        }
        else
        {
            s.t.X = tx;
            s.t.Y = ty;
        }

        bool buttonsPressed(Ellipse cap)
        {
            foreach (var b in buttons)
                if (b.shape == cap) return (shown.Buttons & b.mask) != 0;
            return false;
        }
    }

    static void SetTrigger((Rectangle fill, double height) t, byte value, bool animate)
    {
        double h = value / 255.0 * t.height;
        double top = 68 - h;
        if (animate)
        {
            _ = Anim.To(t.fill, "Height", h, 90);
            _ = Anim.To(t.fill, "(Canvas.Top)", top, 90);
        }
        else
        {
            t.fill.Height = h;
            Canvas.SetTop(t.fill, top);
        }
    }
}
