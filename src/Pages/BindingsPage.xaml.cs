using CommunityToolkit.WinUI.Controls;
using CoopWarrior.Controls;
using CoopWarrior.Core;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CoopWarrior.Pages;

public sealed partial class BindingsPage : Page
{
    readonly Dictionary<string, StackPanel> keyPanels = [];

    public BindingsPage()
    {
        InitializeComponent();

        foreach (var group in GameProfile.Bindings.Concat(GameProfile.Hotkeys).GroupBy(b => b.Group))
        {
            Groups.Children.Add(new TextBlock { Text = group.Key, Style = (Style)Application.Current.Resources["GroupHeaderStyle"] });
            foreach (Binding b in group) Groups.Children.Add(MakeCard(b));
        }
        RefreshAll();

        Loaded += (_, _) => { if (App.Window != null) App.Window.Tick += OnTick; };
        Unloaded += (_, _) => { if (App.Window != null) App.Window.Tick -= OnTick; };
    }

    SettingsCard MakeCard(Binding b)
    {
        var keys = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        keyPanels[b.Name] = keys;
        var card = new SettingsCard
        {
            Header = b.Label,
            HeaderIcon = Icon(b.Name),
            Content = keys,
            IsClickEnabled = true,
            ActionIcon = new FontIcon { Glyph = "" },
            ActionIconToolTip = "Change",
        };
        if (b.Group == "Hotkeys") card.Description = "Also works while the game is in front.";
        card.Click += async (_, _) => await Edit(b);
        return card;
    }

    static IconElement Icon(string name)
    {
        string? glyph = name switch
        {
            "MoveUp" or "CamUp" or "DpadUp" => "",
            "MoveDown" or "CamDown" or "DpadDown" => "",
            "MoveLeft" or "CamLeft" or "DpadLeft" => "",
            "MoveRight" or "CamRight" or "DpadRight" => "",
            "PauseKey" => "",
            "ExitKey" => "",
            "Start" => "",
            "Back" => "",
            _ => null,
        };
        if (glyph != null) return new FontIcon { Glyph = glyph };

        Color? color = name switch
        {
            "A" => Color.FromArgb(255, 16, 124, 16),
            "B" => Color.FromArgb(255, 209, 52, 56),
            "X" => Color.FromArgb(255, 0, 120, 212),
            "Y" => Color.FromArgb(255, 234, 162, 0),
            _ => null,
        };
        var icon = new FontIcon
        {
            Glyph = name,
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.Bold,
            FontSize = name.Length > 1 ? 12 : 18,
        };
        if (color != null) icon.Foreground = new SolidColorBrush(color.Value);
        return icon;
    }

    void Refresh(string name)
    {
        StackPanel panel = keyPanels[name];
        panel.Children.Clear();
        int[] codes = App.Profile.Get(name);
        if (codes.Length == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Not set",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Anim.Brush("TextFillColorTertiaryBrush"),
            });
            return;
        }
        foreach (int vk in codes)
        {
            var cap = new Keycap(KeyNames.Label(vk)) { Tag = vk };
            if (KeyNames.IsMouse(vk) && !App.Profile.Mouse.Buttons)
            {
                cap.Opacity = 0.4;
                ToolTipService.SetToolTip(cap, "Mouse buttons are turned off on the Mouse page");
            }
            panel.Children.Add(cap);
        }
    }

    void RefreshAll()
    {
        foreach (string name in keyPanels.Keys) Refresh(name);
    }

    void OnTick()
    {
        foreach (StackPanel panel in keyPanels.Values)
            foreach (UIElement child in panel.Children)
                if (child is Keycap k && k.Tag is int vk) k.IsPressed = Engine.IsDown(vk);
    }

    async Task Edit(Binding b)
    {
        var dialog = new RebindDialog(b, App.Profile.Get(b.Name)) { XamlRoot = XamlRoot };
        int[]? result = await dialog.PickAsync();
        if (result == null) return;
        App.Profile.Set(b.Name, result);
        App.ProfileChanged();
        Refresh(b.Name);
    }

    async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Reset all keys?",
            Content = "Every controller button and hotkey in this profile goes back to its default key.",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;
        App.Profile.ResetKeys();
        App.ProfileChanged();
        RefreshAll();
    }
}
