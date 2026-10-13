using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using CoopWarrior.Core;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace CoopWarrior.Pages;

public sealed partial class ConfigsPage : Page
{
    public ConfigsPage()
    {
        InitializeComponent();
        Build();
        App.Store.ListChanged += Build;
        App.Store.ActiveChanged += Build;
        Unloaded += (_, _) =>
        {
            App.Store.ListChanged -= Build;
            App.Store.ActiveChanged -= Build;
        };
    }

    void Build()
    {
        List.Children.Clear();
        foreach (GameConfig c in App.Store.Configs) List.Children.Add(Card(c));
    }

    static string Describe(GameConfig c)
    {
        var parts = new List<string>
        {
            c.GameProcess.Length > 0 ? "Game: " + c.GameProcess + ".exe" + (c.OnlyInGame ? " (only while in front)" : "") : "Any program",
            c.Mouse.Stick switch
            {
                MouseStick.Right => "mouse turns the camera",
                MouseStick.Left => "mouse moves",
                _ => "keyboard only",
            },
        };
        return string.Join("  ·  ", parts) + "\n" + c.FileName;
    }

    SettingsCard Card(GameConfig c)
    {
        bool active = c == App.Config;
        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        if (active)
        {
            var badge = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            badge.Children.Add(new FontIcon { Glyph = "", FontSize = 14, Foreground = Controls.Anim.Brush("AccentTextFillColorPrimaryBrush") });
            badge.Children.Add(new TextBlock { Text = "In use", FontWeight = FontWeights.SemiBold });
            right.Children.Add(badge);
        }
        else
        {
            var use = new Button { Content = "Use" };
            use.Click += (_, _) => App.Store.Activate(c);
            right.Children.Add(use);
        }

        var more = new Button { Content = new FontIcon { Glyph = "", FontSize = 14 }, Padding = new Thickness(8) };
        ToolTipService.SetToolTip(more, "More");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(more, "More options for " + c.Name);
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Rename", "", async () => await Rename(c)));
        menu.Items.Add(Item("Duplicate", "", async () => await Duplicate(c)));
        menu.Items.Add(Item("Export...", "", async () => await Export(c)));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Delete", "", async () => await Delete(c)));
        more.Flyout = menu;
        right.Children.Add(more);

        var card = new SettingsCard
        {
            Header = c.Name,
            Description = Describe(c),
            HeaderIcon = new FontIcon { Glyph = active ? "" : "" },
            Content = right,
            IsClickEnabled = !active,
        };
        if (!active) card.Click += (_, _) => App.Store.Activate(c);
        return card;
    }

    static MenuFlyoutItem Item(string text, string glyph, Func<Task> action)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph } };
        item.Click += async (_, _) => await action();
        return item;
    }

    void Show(string message, InfoBarSeverity severity = InfoBarSeverity.Success)
    {
        MessageBar.Severity = severity;
        MessageBar.Message = message;
        MessageBar.IsOpen = true;
    }

    async Task<string?> AskName(string title, string initial, string button)
    {
        var box = new TextBox { Text = initial, PlaceholderText = "For example: Dynasty Warriors 3", SelectionStart = 0, SelectionLength = initial.Length };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = box,
            PrimaryButtonText = button,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        box.TextChanged += (_, _) => dialog.IsPrimaryButtonEnabled = box.Text.Trim().Length > 0;
        dialog.IsPrimaryButtonEnabled = initial.Trim().Length > 0;
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return null;
        return box.Text.Trim();
    }

    async void New_Click(object sender, RoutedEventArgs e)
    {
        string? name = await AskName("New game config", "", "Create");
        if (name == null) return;
        GameConfig c = App.Store.Create(name);
        App.Store.Activate(c);
        Show($"Created \"{c.Name}\" with the default keys and switched to it. Set its game on the Dashboard and its keys on Key bindings.");
    }

    async Task Rename(GameConfig c)
    {
        string? name = await AskName("Rename config", c.Name, "Rename");
        if (name == null || name == c.Name) return;
        App.Store.Rename(c, name);
    }

    async Task Duplicate(GameConfig c)
    {
        string? name = await AskName("Duplicate config", c.Name + " copy", "Duplicate");
        if (name == null) return;
        GameConfig copy = App.Store.Duplicate(c, name);
        Show($"Made \"{copy.Name}\".");
    }

    async Task Delete(GameConfig c)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Delete \"{c.Name}\"?",
            Content = "Its file is deleted from the configs folder. Export it first if you want to keep a copy.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        App.Store.Delete(c);
    }

    async Task Export(GameConfig c)
    {
        try
        {
            var picker = new FileSavePicker { SuggestedFileName = Path.GetFileNameWithoutExtension(c.FilePath) };
            picker.FileTypeChoices.Add("Co-op Warrior game config", [GameConfig.Extension]);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Window));
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            ConfigStore.Export(c, file.Path);
            Show($"Saved \"{c.Name}\" to {file.Path}");
        }
        catch (Exception ex)
        {
            Show("Could not save the file: " + ex.Message, InfoBarSeverity.Error);
        }
    }

    async void Import_Click(object sender, RoutedEventArgs e)
    {
        string path;
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(GameConfig.Extension);
            picker.FileTypeFilter.Add(".txt");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Window));
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            path = file.Path;
        }
        catch (Exception ex)
        {
            Show("Could not open the file picker: " + ex.Message, InfoBarSeverity.Error);
            return;
        }
        GameConfig? c = App.Store.Import(path);
        if (c == null)
        {
            Show("Could not read that file.", InfoBarSeverity.Error);
            return;
        }
        App.Store.Activate(c);
        Show($"Imported \"{c.Name}\" and switched to it.");
    }

    void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"\"{ConfigStore.ConfigsFolder}\"") { UseShellExecute = true }); }
        catch (Exception) { }
    }
}
