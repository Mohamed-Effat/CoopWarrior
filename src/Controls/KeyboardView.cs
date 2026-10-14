using CoopWarrior.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CoopWarrior.Controls;

/// <summary>A keyboard drawing of the keys a profile binds to controller buttons,
/// each where it sits on a US QWERTY keyboard.</summary>
/// <remarks>
/// Only bound keys are drawn. The main keys, the arrow and navigation keys, and the number pad are each
/// trimmed to the keys in use and placed side by side, so the drawing stays compact.
/// </remarks>
public sealed class KeyboardView : UserControl
{
    /// <summary>The distance from one key to the next, in drawing units.</summary>
    const double Pitch = 44;
    const double Gap = 6;
    const double BlockGap = 0.5;

    enum Block { Main, Nav, Pad }

    /// <summary>Key positions on a US (ANSI) keyboard, in keys: x and y from the top-left key of the block.</summary>
    static readonly Dictionary<int, (Block block, double x, double y, double w, double h)> Positions = BuildPositions();

    sealed record Key(int Vk, Keycap Cap, List<string> Actions);

    readonly Canvas canvas = new();
    readonly List<Key> keys = [];

    public KeyboardView()
    {
        IsHitTestVisible = false;
        Content = new Viewbox { Child = canvas, Stretch = Stretch.Uniform };
    }

    static Dictionary<int, (Block, double, double, double, double)> BuildPositions()
    {
        var p = new Dictionary<int, (Block, double, double, double, double)>();
        void Row(Block b, double y, double x, string vks)
        {
            foreach (char c in vks)
            {
                p[c] = (b, x, y, 1, 1);
                x++;
            }
        }
        void At(int vk, Block b, double x, double y, double w = 1, double h = 1) => p[vk] = (b, x, y, w, h);

        At(27, Block.Main, 0, -1.5);
        for (int i = 0; i < 12; i++) At(112 + i, Block.Main, 2 + i + i / 4 * 0.5, -1.5);

        At(192, Block.Main, 0, 0);
        Row(Block.Main, 0, 1, "1234567890");
        At(189, Block.Main, 11, 0); At(187, Block.Main, 12, 0); At(8, Block.Main, 13, 0, 2);

        At(9, Block.Main, 0, 1, 1.5);
        Row(Block.Main, 1, 1.5, "QWERTYUIOP");
        At(219, Block.Main, 11.5, 1); At(221, Block.Main, 12.5, 1); At(220, Block.Main, 13.5, 1, 1.5);

        At(20, Block.Main, 0, 2, 1.75);
        Row(Block.Main, 2, 1.75, "ASDFGHJKL");
        At(186, Block.Main, 10.75, 2); At(222, Block.Main, 11.75, 2); At(13, Block.Main, 12.75, 2, 2.25);

        At(16, Block.Main, 0, 3, 2.25); At(160, Block.Main, 0, 3, 2.25);
        Row(Block.Main, 3, 2.25, "ZXCVBNM");
        At(188, Block.Main, 9.25, 3); At(190, Block.Main, 10.25, 3); At(191, Block.Main, 11.25, 3);
        At(161, Block.Main, 12.25, 3, 2.75);

        At(17, Block.Main, 0, 4, 1.25); At(162, Block.Main, 0, 4, 1.25);
        At(91, Block.Main, 1.25, 4, 1.25);
        At(18, Block.Main, 2.5, 4, 1.25); At(164, Block.Main, 2.5, 4, 1.25);
        At(32, Block.Main, 3.75, 4, 6.25);
        At(165, Block.Main, 10, 4, 1.25); At(92, Block.Main, 11.25, 4, 1.25);
        At(93, Block.Main, 12.5, 4, 1.25); At(163, Block.Main, 13.75, 4, 1.25);

        At(44, Block.Nav, 0, -1.5); At(145, Block.Nav, 1, -1.5); At(19, Block.Nav, 2, -1.5);
        At(45, Block.Nav, 0, 0); At(36, Block.Nav, 1, 0); At(33, Block.Nav, 2, 0);
        At(46, Block.Nav, 0, 1); At(35, Block.Nav, 1, 1); At(34, Block.Nav, 2, 1);
        At(38, Block.Nav, 1, 3);
        At(37, Block.Nav, 0, 4); At(40, Block.Nav, 1, 4); At(39, Block.Nav, 2, 4);

        At(144, Block.Pad, 0, 0); At(111, Block.Pad, 1, 0); At(106, Block.Pad, 2, 0); At(109, Block.Pad, 3, 0);
        At(103, Block.Pad, 0, 1); At(104, Block.Pad, 1, 1); At(105, Block.Pad, 2, 1); At(107, Block.Pad, 3, 1, 1, 2);
        At(100, Block.Pad, 0, 2); At(101, Block.Pad, 1, 2); At(102, Block.Pad, 2, 2);
        At(97, Block.Pad, 0, 3); At(98, Block.Pad, 1, 3); At(99, Block.Pad, 2, 3);
        At(96, Block.Pad, 0, 4, 2); At(110, Block.Pad, 2, 4);
        return p;
    }

    /// <summary>Draws every keyboard key the profile binds to the given actions, or to all of them. Mouse buttons are left out.</summary>
    public void ShowKeys(GameProfile profile, IEnumerable<string>? actions = null)
    {
        keys.Clear();
        canvas.Children.Clear();
        var byVk = new Dictionary<int, Key>();
        foreach (string action in actions ?? GameProfile.Bindings.Select(b => b.Name))
        {
            foreach (int vk in profile.Get(action))
            {
                if (KeyNames.IsMouse(vk)) continue;
                if (!byVk.TryGetValue(vk, out Key? key))
                {
                    key = new Key(vk, new Keycap(KeyNames.Label(vk), 16, Pitch - Gap), []);
                    byVk[vk] = key;
                    keys.Add(key);
                }
                key.Actions.Add(action);
            }
        }

        var placed = new List<(Key key, double x, double y, double w, double h)>();
        var unknown = new List<Key>();
        double left = 0;
        foreach (Block block in Enum.GetValues<Block>())
        {
            var inBlock = keys.Where(k => Positions.TryGetValue(k.Vk, out var p) && p.block == block)
                              .Select(k => (key: k, pos: Positions[k.Vk])).ToList();
            if (inBlock.Count == 0) continue;
            double minX = inBlock.Min(k => k.pos.x);
            foreach (var (key, pos) in inBlock) placed.Add((key, left + pos.x - minX, pos.y, pos.w, pos.h));
            left = placed.Max(k => k.x + k.w) + BlockGap;
        }
        unknown.AddRange(keys.Where(k => !Positions.ContainsKey(k.Vk)));
        if (unknown.Count > 0)
        {
            double y = placed.Count > 0 ? placed.Max(k => k.y + k.h) + 0.25 : 0;
            for (int i = 0; i < unknown.Count; i++) placed.Add((unknown[i], i * 1.5, y, 1.5, 1));
        }
        if (placed.Count == 0)
        {
            canvas.Width = canvas.Height = 1;
            return;
        }

        double top = placed.Min(k => k.y);
        foreach (var (key, x, y, w, h) in placed)
        {
            key.Cap.Width = w * Pitch - Gap;
            key.Cap.Height = h * Pitch - Gap;
            Canvas.SetLeft(key.Cap, x * Pitch);
            Canvas.SetTop(key.Cap, (y - top) * Pitch);
            canvas.Children.Add(key.Cap);
        }
        canvas.Width = placed.Max(k => k.x + k.w) * Pitch - Gap;
        // Room for a pressed key, which moves down by 2.
        canvas.Height = placed.Max(k => k.y + k.h - top) * Pitch - Gap + 2;
    }

    /// <summary>Lights every key bound to the action.</summary>
    public void SetPressed(string action, bool pressed)
    {
        foreach (Key k in keys)
            if (k.Actions.Contains(action)) k.Cap.IsPressed = pressed;
    }

    /// <summary>Lights each key that is held down while one of its actions is down.</summary>
    /// <remarks>An action can have several keys; only the ones actually held light up.</remarks>
    public void ShowPressed(Func<string, bool> actionDown)
    {
        foreach (Key k in keys) k.Cap.IsPressed = k.Actions.Any(actionDown) && Engine.IsDown(k.Vk);
    }
}
