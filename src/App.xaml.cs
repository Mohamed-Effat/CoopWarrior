using System.Runtime.InteropServices;
using CoopWarrior.Core;
using Microsoft.UI.Xaml;

namespace CoopWarrior;

public partial class App : Application
{
    // Version 1 used the same name, so the two versions never drive a pad at the same time.
    const string MutexName = "KeyboardPad_single_instance_9f3b";

    static Mutex? instanceMutex;

    public static ProfileStore Store { get; private set; } = null!;
    public static Engine Engine { get; } = new();

    /// <summary>The active game profile.</summary>
    public static GameProfile Profile => Store.Active;

    /// <summary>Turns the keyboard controller on with the active profile and player number.</summary>
    /// <returns>Null on success, otherwise a message for the user.</returns>
    public static string? TurnOn()
    {
        Engine.Apply(Profile);
        return Engine.Start(Store.Player);
    }

    /// <summary>Saves the active profile and passes its settings to the engine.</summary>
    public static void ProfileChanged()
    {
        Profile.Save();
        Engine.Apply(Profile);
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

        Store = ProfileStore.Load();
        Engine.Apply(Profile);
        Store.ActiveChanged += () => Engine.Apply(Profile);
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

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    public static void ActivateExistingWindow(string title)
    {
        IntPtr h = FindWindow(null, title);
        if (h == IntPtr.Zero) return;
        // The window is hidden while the app sits in the notification area.
        if (!IsWindowVisible(h)) ShowWindow(h, 5);
        if (IsIconic(h)) ShowWindow(h, 9);
        SetForegroundWindow(h);
    }
}
