using System.Runtime.InteropServices;
using CoopWarrior.Core;
using Microsoft.UI.Xaml;

namespace CoopWarrior;

public partial class App : Application
{
    // Version 1 used the same name, so the two versions never drive a pad at the same time.
    const string MutexName = "KeyboardPad_single_instance_9f3b";

    static Mutex? instanceMutex;

    public static ConfigStore Store { get; private set; } = null!;
    public static Engine Engine { get; } = new();

    /// <summary>The active game config.</summary>
    public static GameConfig Config => Store.Active;

    /// <summary>Saves the active config and passes its settings to the engine.</summary>
    public static void ConfigChanged()
    {
        Config.Save();
        Engine.Apply(Config);
    }
    public static MainWindow? Window { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        instanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Native.ActivateExistingWindow(MainWindow.WindowTitle);
            Exit();
            return;
        }

        Store = ConfigStore.Load();
        Engine.Apply(Config);
        Store.ActiveChanged += () => Engine.Apply(Config);
        Window = new MainWindow();
        Window.Activate();
    }
}

static partial class Native
{
    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr FindWindow(string? className, string windowName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int cmd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hWnd);

    public static void ActivateExistingWindow(string title)
    {
        IntPtr h = FindWindow(null, title);
        if (h == IntPtr.Zero) return;
        if (IsIconic(h)) ShowWindow(h, 9);
        SetForegroundWindow(h);
    }
}
