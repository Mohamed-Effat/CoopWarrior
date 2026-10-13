using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using CoopWarrior.Core;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace CoopWarrior.Pages;

public sealed partial class ProfilesPage : Page
{
    public ProfilesPage()
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
        foreach (GameProfile p in App.Store.Profiles) List.Children.Add(Card(p));
    }

    static string Describe(GameProfile p)
    {
        var parts = new List<string>
        {
            p.GameProcess.Length > 0 ? "Game: " + p.GameProcess + ".exe" + (p.OnlyInGame ? " (only while it is in front)" : "") : "Works in any program",
            p.Mouse.Stick switch
            {
                MouseStick.Right => "the mouse turns the camera",
                MouseStick.Left => "the mouse moves your player",
                _ => "keyboard only",
            },
        };
        return string.Join("  ·  ", parts) + "\n" + p.FileName;
    }

    SettingsCard Card(GameProfile p)
    {
        bool active = p == App.Profile;
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
            use.Click += (_, _) => App.Store.Activate(p);
            right.Children.Add(use);
        }

        var more = new Button { Content = new FontIcon { Glyph = "", FontSize = 14 }, Padding = new Thickness(8) };
        ToolTipService.SetToolTip(more, "More");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(more, "More options for " + p.Name);
        var menu = new MenuFlyout();
        menu.Items.Add(Item("Rename", "", async () => await Rename(p)));
        menu.Items.Add(Item("Make a copy", "", async () => await Duplicate(p)));
        menu.Items.Add(Item("Export to a file...", "", async () => await Export(p)));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(Item("Delete", "", async () => await Delete(p)));
        more.Flyout = menu;
        right.Children.Add(more);

        var card = new SettingsCard
        {
            Header = p.Name,
            Description = Describe(p),
            HeaderIcon = new FontIcon { Glyph = active ? "" : "" },
            Content = right,
            IsClickEnabled = !active,
        };
        if (!active) card.Click += (_, _) => App.Store.Activate(p);
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
        string? name = await AskName("New profile", "", "Create");
        if (name == null) return;
        GameProfile p = App.Store.Create(name);
        App.Store.Activate(p);
        Show($"Created \"{p.Name}\" with the default keys, and it is now in use. Choose its game on the Dashboard and its keys on the Key bindings page.");
    }

    async Task Rename(GameProfile p)
    {
        string? name = await AskName("Rename profile", p.Name, "Rename");
        if (name == null || name == p.Name) return;
        App.Store.Rename(p, name);
    }

    async Task Duplicate(GameProfile p)
    {
        string? name = await AskName("Make a copy", p.Name + " copy", "Make copy");
        if (name == null) return;
        GameProfile copy = App.Store.Duplicate(p, name);
        Show($"Made a copy called \"{copy.Name}\".");
    }

    async Task Delete(GameProfile p)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Delete \"{p.Name}\"?",
            Content = "Its file is deleted from the profiles folder. If you want to keep a copy, export it first.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        App.Store.Delete(p);
    }

    async Task Export(GameProfile p)
    {
        try
        {
            var picker = new FileSavePicker { SuggestedFileName = Path.GetFileNameWithoutExtension(p.FilePath) };
            picker.FileTypeChoices.Add("Co-op Warrior profile", [GameProfile.Extension]);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Window));
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            ProfileStore.Export(p, file.Path);
            Show($"Saved \"{p.Name}\" to {file.Path}");
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
            picker.FileTypeFilter.Add(GameProfile.Extension);
            picker.FileTypeFilter.Add(".txt");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Window));
            var file = await picker.PickSingleFileAsync();
            if (file == null) return;
            path = file.Path;
        }
        catch (Exception ex)
        {
            Show("Could not open the file window: " + ex.Message, InfoBarSeverity.Error);
            return;
        }
        GameProfile? p = App.Store.Import(path);
        if (p == null)
        {
            Show("Could not read that file.", InfoBarSeverity.Error);
            return;
        }
        App.Store.Activate(p);
        Show($"Imported \"{p.Name}\", and it is now in use.");
    }

    void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"\"{ProfileStore.ProfilesFolder}\"") { UseShellExecute = true }); }
        catch (Exception) { }
    }
}
