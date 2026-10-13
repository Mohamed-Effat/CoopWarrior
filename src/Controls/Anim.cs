using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace CoopWarrior.Controls;

/// <summary>Small awaitable wrappers around Storyboard animations.</summary>
public static class Anim
{
    public static readonly EasingFunctionBase EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };
    public static readonly EasingFunctionBase EaseInOut = new CubicEase { EasingMode = EasingMode.EaseInOut };
    public static readonly EasingFunctionBase Spring = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };

    public static Brush Brush(string key) => (Brush)Application.Current.Resources[key];

    /// <summary>Animates a double property such as Opacity, or X on a transform.</summary>
    public static Task To(DependencyObject target, string property, double to, int ms, EasingFunctionBase? ease = null)
    {
        var tcs = new TaskCompletionSource();
        var a = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromMilliseconds(ms),
            EasingFunction = ease ?? EaseOut,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(a, target);
        Storyboard.SetTargetProperty(a, property);
        var sb = new Storyboard();
        sb.Children.Add(a);
        sb.Completed += (_, _) => tcs.TrySetResult();
        sb.Begin();
        return tcs.Task;
    }

    public static Task Fade(UIElement e, double to, int ms = 250) => To(e, "Opacity", to, ms);

    /// <summary>Moves an element that has a TranslateTransform as its RenderTransform.</summary>
    public static Task Move(UIElement e, double x, double y, int ms, EasingFunctionBase? ease = null)
    {
        var t = Translate(e);
        return Task.WhenAll(To(t, "X", x, ms, ease ?? EaseInOut), To(t, "Y", y, ms, ease ?? EaseInOut));
    }

    public static TranslateTransform Translate(UIElement e)
    {
        if (e.RenderTransform is TranslateTransform t) return t;
        if (e.RenderTransform is TransformGroup g)
            foreach (Transform child in g.Children)
                if (child is TranslateTransform gt) return gt;
        t = new TranslateTransform();
        e.RenderTransform = t;
        return t;
    }

    /// <summary>Scales an element about its center.</summary>
    public static Task Scale(UIElement e, double to, int ms, EasingFunctionBase? ease = null)
    {
        e.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        if (e.RenderTransform is not ScaleTransform s)
        {
            s = new ScaleTransform();
            e.RenderTransform = s;
        }
        return Task.WhenAll(To(s, "ScaleX", to, ms, ease), To(s, "ScaleY", to, ms, ease));
    }

    public static void SetScale(UIElement e, double value)
    {
        e.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        e.RenderTransform = new ScaleTransform { ScaleX = value, ScaleY = value };
    }

    /// <summary>Starts a looping pulse: the element grows and fades out, then repeats.</summary>
    public static Storyboard Pulse(UIElement e, double toScale = 1.6, int ms = 1400)
    {
        e.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var s = new ScaleTransform();
        e.RenderTransform = s;
        var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        foreach (string p in new[] { "ScaleX", "ScaleY" })
        {
            var a = new DoubleAnimation { From = 1, To = toScale, Duration = TimeSpan.FromMilliseconds(ms), EasingFunction = EaseOut };
            Storyboard.SetTarget(a, s);
            Storyboard.SetTargetProperty(a, p);
            sb.Children.Add(a);
        }
        var f = new DoubleAnimation { From = 0.7, To = 0, Duration = TimeSpan.FromMilliseconds(ms), EasingFunction = EaseOut };
        Storyboard.SetTarget(f, e);
        Storyboard.SetTargetProperty(f, "Opacity");
        sb.Children.Add(f);
        sb.Begin();
        return sb;
    }

    public static Task Delay(int ms, CancellationToken ct) => Task.Delay(ms, ct);
}
