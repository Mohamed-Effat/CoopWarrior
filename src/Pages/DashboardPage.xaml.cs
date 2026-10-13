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
        ConfigBox.ItemsSource = App.Store.Configs.ToList();
        ConfigBox.SelectedItem = App.Config;
        NotesBar.Message = string.Join("\n\n", App.Config.Notes);
        NotesBar.IsOpen = App.Config.Notes.Count > 0;
        OnlyToggle.IsOn = App.Config.OnlyInGame;
        ProcBox.Text = App.Config.GameProcess;
        FilterExpander.IsExpanded = App.Config.OnlyInGame;
        PadToggle.IsOn = App.Engine.Running;
        syncing = false;

        foreach (int vk in App.Config.Get("PauseKey")) PauseKeys.Children.Add(new Keycap(KeyNames.Label(vk)));
        foreach (int vk in App.Config.Get("ExitKey")) StopKeys.Children.Add(new Keycap(KeyNames.Label(vk)));

        DriverBar.IsOpen = !Driver.Installed();
        lastDriverCheck = Environment.TickCount;

        Loaded += (_, _) => { if (App.Window != null) App.Window.Tick += OnTick; OnTick(); };
        Unloaded += (_, _) => { if (App.Window != null) App.Window.Tick -= OnTick; };
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
        int slot = engine.UserIndex;
        string slotText = slot is >= 0 and < 4 ? $"Windows sees it as controller {slot + 1}" : "";
        if (state + slotText != lastState)
        {
            lastState = state + slotText;
            Headline.Text = state switch
            {
                "on" => "Your keyboard is a controller",
                "paused" => "Paused",
                _ => "The virtual controller is off",
            };
            Detail.Text = state switch
            {
                "on" => "Press your keys and watch the controller react. In the game, the keyboard is the next free player.",
                "paused" => "The controller stays connected but ignores your keys. Press the pause hotkey again to resume.",
                _ => "Turn it on and your keyboard and mouse become a second Xbox controller, ready for split-screen games.",
            };
            SlotText.Text = slotText;
            SlotPanel.Visibility = state != "off" && slotText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        if (unchecked(Environment.TickCount - lastDriverCheck) > 1500)
        {
            lastDriverCheck = Environment.TickCount;
            if (!installing) DriverBar.IsOpen = !Driver.Installed();
        }
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

        App.Engine.Apply(App.Config);
        string? err = App.Engine.Start();
        if (err != null)
        {
            ErrorBar.Message = err;
            ErrorBar.IsOpen = true;
            syncing = true;
            PadToggle.IsOn = false;
            syncing = false;
        }
    }

    async void Install_Click(object sender, RoutedEventArgs e)
    {
        installing = true;
        InstallButton.IsEnabled = false;
        DriverBar.Severity = InfoBarSeverity.Informational;
        DriverBar.Title = "Installing the driver";
        DriverBar.Message = "Approve the Windows permission prompt. This takes a few seconds.";
        string? err = await Driver.InstallAsync();
        installing = false;
        InstallButton.IsEnabled = true;
        if (err == null && Driver.Installed())
        {
            DriverBar.Severity = InfoBarSeverity.Success;
            DriverBar.Title = "Driver installed";
            DriverBar.Message = "You can turn on the virtual controller now.";
            DriverBar.ActionButton = null;
            DriverBar.IsClosable = true;
            return;
        }
        DriverBar.Severity = InfoBarSeverity.Warning;
        DriverBar.Title = "Driver not installed";
        DriverBar.Message = err ?? "The installer finished, but the driver is not there yet. Restart Windows and try again.";
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
        App.Config.GameProcess = GameConfig.StripExe(ProcBox.Text ?? "");
        App.Config.OnlyInGame = OnlyToggle.IsOn;
        App.ConfigChanged();
    }

    void ConfigBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (syncing || ConfigBox.SelectedItem is not GameConfig c || c == App.Config) return;
        App.Store.Activate(c);
    }

    void Tutorial_Click(object sender, RoutedEventArgs e) => App.Window?.Navigate("Tutorial");

    void JoyCpl_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("control.exe", "joy.cpl") { UseShellExecute = true }); }
        catch (Exception) { }
    }
}
