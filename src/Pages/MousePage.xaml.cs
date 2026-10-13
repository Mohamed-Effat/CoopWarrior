using CoopWarrior.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoopWarrior.Pages;

public sealed partial class MousePage : Page
{
    readonly DispatcherQueueTimer saveTimer;
    bool loading = true;
    string lastHint = "";

    public MousePage()
    {
        InitializeComponent();

        MouseSettings m = App.Config.Mouse;
        MoveToggle.IsOn = m.Stick != MouseStick.Off;
        StickBox.SelectedIndex = m.Stick == MouseStick.Left ? 1 : 0;
        SensitivitySlider.Value = m.Sensitivity;
        DeadzoneSlider.Value = Math.Round(m.Deadzone * 100);
        InvertToggle.IsOn = m.InvertY;
        LockToggle.IsOn = m.LockCursor;
        ButtonsToggle.IsOn = m.Buttons;
        MoveExpander.IsExpanded = MoveToggle.IsOn;
        ShowValues();
        loading = false;

        // Sliders change many times a second while dragged; the file is written once they settle.
        saveTimer = DispatcherQueue.CreateTimer();
        saveTimer.Interval = TimeSpan.FromMilliseconds(400);
        saveTimer.IsRepeating = false;
        saveTimer.Tick += (_, _) => App.Config.Save();

        Loaded += (_, _) => { if (App.Window != null) App.Window.Tick += OnTick; OnTick(); };
        Unloaded += (_, _) =>
        {
            if (App.Window != null) App.Window.Tick -= OnTick;
            if (saveTimer.IsRunning)
            {
                saveTimer.Stop();
                App.Config.Save();
            }
        };
    }

    void ShowValues()
    {
        SensitivityText.Text = SensitivitySlider.Value.ToString("0.#");
        DeadzoneText.Text = DeadzoneSlider.Value.ToString("0") + "%";
    }

    void Changed(object sender, object e)
    {
        if (loading) return;
        ShowValues();
        if (ReferenceEquals(sender, MoveToggle) && MoveToggle.IsOn) MoveExpander.IsExpanded = true;
        App.Config.Mouse = new MouseSettings(
            MoveToggle.IsOn ? (StickBox.SelectedIndex == 1 ? MouseStick.Left : MouseStick.Right) : MouseStick.Off,
            SensitivitySlider.Value,
            DeadzoneSlider.Value / 100,
            InvertToggle.IsOn,
            ButtonsToggle.IsOn,
            LockToggle.IsOn);
        App.Engine.Apply(App.Config);
        saveTimer.Stop();
        saveTimer.Start();
    }

    void OnTick()
    {
        Engine engine = App.Engine;
        Pad.Show(engine.Running ? engine.Current : default);
        Pad.Opacity = engine.Running && !engine.Paused ? 1.0 : 0.5;
        UnavailableBar.IsOpen = engine.Running && !engine.MouseAvailable;

        string hint = !engine.Running ? "Turn on the virtual controller on the Dashboard, then move your mouse here to try these settings."
            : !MoveToggle.IsOn ? "Switch on \"Mouse moves a stick\" below, then move your mouse."
            : "Move your mouse and watch the stick. The faster you move, the further it tilts.";
        if (hint != lastHint) HeroDetail.Text = lastHint = hint;
    }

    void Bindings_Click(object sender, RoutedEventArgs e) => App.Window?.Navigate("Bindings");
}
