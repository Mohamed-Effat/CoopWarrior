using System.Diagnostics;
using CoopWarrior.Controls;
using CoopWarrior.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoopWarrior.Pages;

public sealed partial class DashboardPage : Page
{
    bool syncing;
    bool installing;
    int lastDriverCheck;
    string lastState = "";

    public DashboardPage()
    {
        InitializeComponent();

        syncing = true;
        ProfileBox.ItemsSource = App.Store.Profiles.ToList();
        ProfileBox.SelectedItem = App.Profile;
        NotesBar.Message = string.Join("\n\n", App.Profile.Notes);
        NotesBar.IsOpen = App.Profile.Notes.Count > 0;
        OnlyToggle.IsOn = App.Profile.OnlyInGame;
        ProcBox.Text = App.Profile.GameProcess;
        FilterExpander.IsExpanded = App.Profile.OnlyInGame;
        PadToggle.IsOn = App.Engine.Running;
        PlayerBox.SelectedIndex = App.Store.Player;
        syncing = false;

        foreach (int vk in App.Profile.Get("PauseKey")) PauseKeys.Children.Add(new Keycap(KeyNames.Label(vk)));
        foreach (int vk in App.Profile.Get("ExitKey")) StopKeys.Children.Add(new Keycap(KeyNames.Label(vk)));

        ShowDriver(Driver.Installed());
        lastDriverCheck = Environment.TickCount;

        Loaded += (_, _) => { if (App.Window != null) App.Window.Tick += OnTick; OnTick(); };
        Unloaded += (_, _) => { if (App.Window != null) App.Window.Tick -= OnTick; };
    }

    void ShowDriver(bool installed, string? problem = null)
    {
        DriverBar.Severity = installed ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        DriverBar.Title = installed ? "The controller driver is installed" : "The controller driver is not installed";
        DriverBar.Message = installed
            ? "ViGEmBus is ready on this PC."
            : problem ?? "Co-op Warrior needs the free ViGEmBus driver to turn your keyboard into a controller. You install it once on each PC.";
        InstallButton.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
    }

    void OnTick()
    {
        Engine engine = App.Engine;
        if (PadToggle.IsOn != engine.Running)
        {
            syncing = true;
            PadToggle.IsOn = engine.Running;
            syncing = false;
        }

        Pad.Show(engine.Running ? engine.Current : default);
        Pad.Opacity = engine.Running && !engine.Paused ? 1.0 : 0.5;

        string state = !engine.Running ? "off" : engine.Paused ? "paused" : "on";
        int player = engine.UserIndex is >= 0 and < 4 ? engine.UserIndex + 1 : 0;
        string key = state + player + App.Store.Player + App.Profile.OnlyInGame;
        if (key != lastState)
        {
            lastState = key;
            ShowState(state, player);
        }

        if (unchecked(Environment.TickCount - lastDriverCheck) > 1500)
        {
            lastDriverCheck = Environment.TickCount;
            if (!installing) ShowDriver(Driver.Installed());
        }
    }

    void ShowState(string state, int player)
    {
        string pauseKeys = string.Join(" or ", App.Profile.Get("PauseKey").Select(KeyNames.Label));
        Headline.Text = state switch
        {
            "on" when player > 0 => $"Your keyboard is Player {player}",
            "on" => "Your keyboard is a controller",
            "paused" => "Paused",
            _ => "",
        };
        Headline.Visibility = Headline.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        Detail.Text = state switch
        {
            "on" when App.Profile.OnlyInGame =>
                "This profile only works while the game is in front, so the picture stays still until you switch to the game.",
            "on" => "Press your keys and watch the controller picture move.",
            "paused" => $"The controller stays connected but ignores your keys. Press {pauseKeys} to resume.",
            _ => "When it is on, Windows sees your keyboard and mouse as an Xbox controller, so another player can join split-screen games.",
        };

        int wanted = App.Store.Player;
        bool moved = state != "off" && wanted > 0 && player > 0 && player != wanted;
        if (moved)
        {
            PlayerBar.Title = $"Your keyboard is Player {player}, not Player {wanted}";
            PlayerBar.Message = $"Another controller is already Player {wanted}. To free that number, disconnect the other controller, " +
                                "then turn the keyboard controller off and on again.";
        }
        PlayerBar.IsOpen = moved;
    }

    void PadToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        ErrorBar.IsOpen = false;
        if (!PadToggle.IsOn)
        {
            App.Engine.Stop();
            return;
        }
        TurnOn();
    }

    void TurnOn()
    {
        App.Engine.Apply(App.Profile);
        string? err = App.Engine.Start(App.Store.Player);
        if (err == null) return;
        ErrorBar.Message = err;
        ErrorBar.IsOpen = true;
        syncing = true;
        PadToggle.IsOn = false;
        syncing = false;
    }

    void PlayerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || PlayerBox.SelectedIndex < 0) return;
        App.Store.Player = PlayerBox.SelectedIndex;
        App.Store.SaveSettings();
        // The player number is chosen when the controller connects.
        if (App.Engine.Running)
        {
            App.Engine.Stop();
            TurnOn();
        }
        lastState = "";
    }

    async void Install_Click(object sender, RoutedEventArgs e)
    {
        installing = true;
        InstallButton.IsEnabled = false;
        DriverBar.Severity = InfoBarSeverity.Informational;
        DriverBar.Title = "Installing the controller driver";
        DriverBar.Message = "Click Yes when Windows asks for permission. This takes a few seconds.";
        string? err = await Driver.InstallAsync();
        installing = false;
        InstallButton.IsEnabled = true;
        bool installed = err == null && Driver.Installed();
        ShowDriver(installed, err ?? "The installer finished, but the driver is not ready yet. Restart Windows and try again.");
        if (installed) DriverBar.Message = "ViGEmBus is ready on this PC. You can turn your keyboard into a controller now.";
    }

    void OnlyToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (syncing) return;
        if (OnlyToggle.IsOn) FilterExpander.IsExpanded = true;
        ApplyGameSetting();
    }

    void ProcBox_DropDownOpened(object sender, object e)
    {
        string current = ProcBox.Text;
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Process p in Process.GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero && p.Id != Environment.ProcessId) names.Add(p.ProcessName);
            }
            catch (Exception) { }
            finally { p.Dispose(); }
        }
        ProcBox.ItemsSource = names.ToList();
        ProcBox.Text = current;
    }

    void ProcBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
    {
        ProcBox.Text = args.Text;
        ApplyGameSetting();
    }

    void ProcBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProcBox.SelectedItem is string s)
        {
            ProcBox.Text = s;
            ApplyGameSetting();
        }
    }

    void ApplyGameSetting()
    {
        App.Profile.GameProcess = GameProfile.StripExe(ProcBox.Text ?? "");
        App.Profile.OnlyInGame = OnlyToggle.IsOn;
        App.ProfileChanged();
    }

    void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || ProfileBox.SelectedItem is not GameProfile p || p == App.Profile) return;
        App.Store.Activate(p);
    }

    void Tutorial_Click(object sender, RoutedEventArgs e) => App.Window?.Navigate("Tutorial");

    void JoyCpl_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("control.exe", "joy.cpl") { UseShellExecute = true }); }
        catch (Exception) { }
    }
}
