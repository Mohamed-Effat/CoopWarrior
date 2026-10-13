using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CoopWarrior.Controls;

/// <summary>A key shown as a keyboard keycap, in the style PowerToys uses for shortcuts.</summary>
public sealed class Keycap : UserControl
{
    readonly Border border;
    readonly TextBlock text;
    readonly TranslateTransform shift = new();
    bool pressed;
    bool accent;

    public Keycap(string label, double fontSize = 14, double minWidth = 34)
    {
        text = new TextBlock
        {
            Text = label,
            FontSize = fontSize,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextLineBounds = TextLineBounds.Tight,
        };
        border = new Border
        {
            Child = text,
            MinWidth = minWidth,
            MinHeight = minWidth - 2,
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(5),
            BorderThickness = new Thickness(1, 1, 1, 3),
            RenderTransform = shift,
        };
        Content = border;
        ApplyLook();
        ActualThemeChanged += (_, _) => ApplyLook();
    }

    public string Label
    {
        get => text.Text;
        set => text.Text = value;
    }

    public bool Accent
    {
        get => accent;
        set
        {
            accent = value;
            ApplyLook();
        }
    }

    public bool IsPressed
    {
        get => pressed;
        set
        {
            if (pressed == value) return;
            pressed = value;
            ApplyLook();
            _ = Anim.To(shift, "Y", value ? 2 : 0, 70);
        }
    }

    void ApplyLook()
    {
        bool lit = pressed || Accent;
        border.Background = Anim.Brush(lit ? "AccentFillColorDefaultBrush" : "ControlFillColorDefaultBrush");
        border.BorderBrush = Anim.Brush(lit ? "AccentControlElevationBorderBrush" : "ControlElevationBorderBrush");
        text.Foreground = Anim.Brush(lit ? "TextOnAccentFillColorPrimaryBrush" : "TextFillColorPrimaryBrush");
    }
}
