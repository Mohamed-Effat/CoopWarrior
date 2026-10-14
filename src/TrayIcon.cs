using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CoopWarrior;

/// <summary>Co-op Warrior's icon in the notification area, with a menu on right click.</summary>
/// <remarks>
/// It subclasses the main window to receive the icon's mouse messages. The same hook reports when the window
/// is minimized, and adds the icon again after Explorer restarts, which removes every notification area icon.
/// </remarks>
sealed unsafe partial class TrayIcon : IDisposable
{
    /// <summary>One line of the right-click menu. A null item in the menu list is a separator.</summary>
    public sealed record MenuItem(string Text, bool Checked, Action Run);

    const uint CallbackMessage = 0x8000 + 0x43;
    const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2, NIM_SETVERSION = 4;
    const uint NIF_MESSAGE = 0x1, NIF_ICON = 0x2, NIF_TIP = 0x4, NIF_INFO = 0x10, NIF_SHOWTIP = 0x80;
    const uint NIIF_USER = 0x4;
    const uint NOTIFYICON_VERSION_4 = 4;
    const uint NIN_SELECT = 0x400, NIN_KEYSELECT = 0x401, WM_CONTEXTMENU = 0x7B;
    const uint WM_SIZE = 0x5, SIZE_MINIMIZED = 1;
    const uint MF_STRING = 0, MF_CHECKED = 0x8, MF_SEPARATOR = 0x800;
    const uint TPM_RIGHTBUTTON = 0x2, TPM_NONOTIFY = 0x80, TPM_RETURNCMD = 0x100;

    [StructLayout(LayoutKind.Sequential)]
    struct NotifyIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        public fixed char Tip[128];
        public uint State;
        public uint StateMask;
        public fixed char Info[256];
        public uint Version;
        public fixed char InfoTitle[64];
        public uint InfoFlags;
        public Guid Item;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Point
    {
        public int X, Y;
    }

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [LibraryImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowSubclass(IntPtr hWnd, delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, UIntPtr, UIntPtr, IntPtr> proc, UIntPtr id, UIntPtr data);

    [LibraryImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveWindowSubclass(IntPtr hWnd, delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, UIntPtr, UIntPtr, IntPtr> proc, UIntPtr id);

    [LibraryImport("comctl32.dll")]
    private static partial IntPtr DefSubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint RegisterWindowMessage(string name);

    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr LoadImage(IntPtr instance, string name, uint type, int cx, int cy, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr icon);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    private static partial IntPtr CreatePopupMenu();

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string? text);

    [LibraryImport("user32.dll")]
    private static partial int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hWnd, IntPtr tpm);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyMenu(IntPtr menu);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out Point p);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    static readonly uint TaskbarCreated = RegisterWindowMessage("TaskbarCreated");
    static TrayIcon? current;

    readonly IntPtr window;
    readonly IntPtr icon;
    string tip;
    bool visible;

    /// <summary>The icon was clicked, or chosen with the keyboard.</summary>
    public event Action? Clicked;

    /// <summary>The window was minimized. Raised from inside the window's WM_SIZE message.</summary>
    public event Action? Minimized;

    /// <summary>Builds the right-click menu each time it opens.</summary>
    public Func<IReadOnlyList<MenuItem?>>? Menu { get; set; }

    public TrayIcon(IntPtr window, string iconPath, string tip)
    {
        this.window = window;
        this.tip = tip;
        const uint IMAGE_ICON = 1, LR_LOADFROMFILE = 0x10;
        const int SM_CXSMICON = 49, SM_CYSMICON = 50;
        icon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, GetSystemMetrics(SM_CXSMICON), GetSystemMetrics(SM_CYSMICON), LR_LOADFROMFILE);
        current = this;
        SetWindowSubclass(window, &SubclassProc, (UIntPtr)1, UIntPtr.Zero);
    }

    public bool Visible
    {
        get => visible;
        set
        {
            if (value == visible) return;
            visible = value;
            if (value) Add();
            else
            {
                NotifyIconData d = Data(0);
                Shell_NotifyIcon(NIM_DELETE, ref d);
            }
        }
    }

    public void SetTip(string text)
    {
        tip = text;
        if (!visible) return;
        NotifyIconData d = Data(NIF_TIP | NIF_SHOWTIP);
        Shell_NotifyIcon(NIM_MODIFY, ref d);
    }

    /// <summary>Shows a Windows notification from the icon.</summary>
    public void Notify(string title, string text)
    {
        if (!visible) return;
        NotifyIconData d = Data(NIF_INFO);
        Copy(text, d.Info, 256);
        Copy(title, d.InfoTitle, 64);
        d.InfoFlags = NIIF_USER;
        Shell_NotifyIcon(NIM_MODIFY, ref d);
    }

    void Add()
    {
        NotifyIconData d = Data(NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP);
        Shell_NotifyIcon(NIM_ADD, ref d);
        d.Version = NOTIFYICON_VERSION_4;
        Shell_NotifyIcon(NIM_SETVERSION, ref d);
    }

    NotifyIconData Data(uint flags)
    {
        var d = new NotifyIconData
        {
            Size = (uint)sizeof(NotifyIconData),
            Window = window,
            Id = 1,
            Flags = flags,
            CallbackMessage = CallbackMessage,
            Icon = icon,
        };
        Copy(tip, d.Tip, 128);
        return d;
    }

    static void Copy(string text, char* dest, int size)
    {
        int n = Math.Min(text.Length, size - 1);
        for (int i = 0; i < n; i++) dest[i] = text[i];
        dest[n] = '\0';
    }

    void ShowMenu()
    {
        IReadOnlyList<MenuItem?> items = Menu?.Invoke() ?? [];
        IntPtr menu = CreatePopupMenu();
        for (int i = 0; i < items.Count; i++)
        {
            MenuItem? item = items[i];
            if (item == null) AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            else AppendMenu(menu, MF_STRING | (item.Checked ? MF_CHECKED : 0), (UIntPtr)(i + 1), item.Text);
        }
        GetCursorPos(out Point p);
        // Without the window in front and the message after it, the menu stays open when the user clicks elsewhere
        // (see the remarks on TrackPopupMenu).
        SetForegroundWindow(window);
        int chosen = TrackPopupMenuEx(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_NONOTIFY, p.X, p.Y, window, IntPtr.Zero);
        PostMessage(window, 0, IntPtr.Zero, IntPtr.Zero);
        DestroyMenu(menu);
        if (chosen > 0) items[chosen - 1]?.Run();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    static IntPtr SubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
    {
        // An exception must not unwind into the native window procedure.
        try
        {
            TrayIcon? t = current;
            if (t != null)
            {
                if (msg == CallbackMessage)
                {
                    uint ev = (uint)(lParam.ToInt64() & 0xFFFF);
                    if (ev is NIN_SELECT or NIN_KEYSELECT) t.Clicked?.Invoke();
                    else if (ev == WM_CONTEXTMENU) t.ShowMenu();
                    return 0;
                }
                if (msg == TaskbarCreated && t.visible) t.Add();
                if (msg == WM_SIZE && wParam == (IntPtr)SIZE_MINIMIZED) t.Minimized?.Invoke();
            }
        }
        catch (Exception) { }
        return DefSubclassProc(hWnd, msg, wParam, lParam);
    }


    public void Dispose()
    {
        Visible = false;
        RemoveWindowSubclass(window, &SubclassProc, (UIntPtr)1);
        if (icon != IntPtr.Zero) DestroyIcon(icon);
        if (current == this) current = null;
    }
}
