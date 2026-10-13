using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoopWarrior.Core;

/// <summary>Turns mouse movement into a stick position, and can hold the cursor in the game window.</summary>
/// <remarks>Create, use and dispose it on one thread: Raw Input messages go to the thread that made the window.</remarks>
sealed unsafe partial class MouseInput : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct Point { public int X, Y; }

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateWindowEx(uint exStyle, string className, string? windowName, uint style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterRawInputDevices(ref RawInputDevice device, uint count, uint size);

    [LibraryImport("user32.dll")]
    private static partial uint GetRawInputData(IntPtr rawInput, uint command, byte* data, ref uint size, uint headerSize);

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PeekMessage(out Msg msg, IntPtr hwnd, uint min, uint max, uint remove);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static partial IntPtr DispatchMessage(ref Msg msg);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClipCursor(Rect* rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(IntPtr hwnd, out Rect rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClientToScreen(IntPtr hwnd, ref Point point);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW")]
    private static partial int GetClassName(IntPtr hwnd, char* name, int max);

    const uint WM_INPUT = 0x00FF;
    const uint PM_REMOVE = 1;
    const uint RIDEV_REMOVE = 0x1;
    const uint RIDEV_INPUTSINK = 0x100;
    const uint RID_INPUT = 0x10000003;
    const uint RIM_TYPEMOUSE = 0;
    const ushort MOUSE_MOVE_ABSOLUTE = 0x1;
    const ushort MOUSE_VIRTUAL_DESKTOP = 0x2;
    const int RawHeaderSize = 24;
    static readonly IntPtr HWND_MESSAGE = new(-3);

    /// <summary>Mouse speed is averaged over this window, so the stick returns to center this long after the mouse stops.</summary>
    const double WindowSeconds = 0.03;

    readonly IntPtr window;
    readonly Queue<(long time, int dx, int dy)> recent = new();
    long sumX, sumY;
    int pendingX, pendingY;
    int lastAbsX = -1, lastAbsY = -1;
    bool clipped;

    public MouseInput()
    {
        // A message-only window: it receives Raw Input but never appears on screen.
        window = CreateWindowEx(0, "Message", null, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (window == IntPtr.Zero) return;
        var device = new RawInputDevice { UsagePage = 0x01, Usage = 0x02, Flags = RIDEV_INPUTSINK, Target = window };
        if (!RegisterRawInputDevices(ref device, 1, (uint)sizeof(RawInputDevice)))
        {
            DestroyWindow(window);
            window = IntPtr.Zero;
        }
    }

    public bool Available => window != IntPtr.Zero;

    /// <summary>Reads the mouse movement since the last call.</summary>
    public void Pump()
    {
        if (window == IntPtr.Zero) return;
        while (PeekMessage(out Msg msg, IntPtr.Zero, 0, 0, PM_REMOVE))
        {
            if (msg.Message == WM_INPUT) Read(msg.LParam);
            DispatchMessage(ref msg);
        }
    }

    void Read(IntPtr handle)
    {
        byte* buffer = stackalloc byte[64];
        uint size = 64;
        uint got = GetRawInputData(handle, RID_INPUT, buffer, ref size, RawHeaderSize);
        if (got == uint.MaxValue || got < RawHeaderSize + 20) return;
        if (*(uint*)buffer != RIM_TYPEMOUSE) return;

        // RAWMOUSE follows the header: usFlags at 0, lLastX at 12, lLastY at 16.
        byte* mouse = buffer + RawHeaderSize;
        ushort flags = *(ushort*)mouse;
        int x = *(int*)(mouse + 12);
        int y = *(int*)(mouse + 16);

        if ((flags & MOUSE_MOVE_ABSOLUTE) == 0)
        {
            pendingX += x;
            pendingY += y;
            return;
        }

        // Remote desktop and some tablets send positions from 0 to 65535 instead of movement.
        bool virtualDesk = (flags & MOUSE_VIRTUAL_DESKTOP) != 0;
        int width = GetSystemMetrics(virtualDesk ? 78 : 0);
        int height = GetSystemMetrics(virtualDesk ? 79 : 1);
        if (lastAbsX >= 0)
        {
            pendingX += (int)((long)(x - lastAbsX) * width / 65535);
            pendingY += (int)((long)(y - lastAbsY) * height / 65535);
        }
        lastAbsX = x;
        lastAbsY = y;
    }

    /// <summary>The stick position for the mouse movement of the last few milliseconds, each axis -1 to 1, up positive.</summary>
    public (double x, double y) Stick(MouseSettings s, bool enabled)
    {
        long now = Stopwatch.GetTimestamp();
        int dx = pendingX, dy = pendingY;
        pendingX = pendingY = 0;
        if (!enabled)
        {
            recent.Clear();
            sumX = sumY = 0;
            return (0, 0);
        }

        recent.Enqueue((now, dx, dy));
        sumX += dx;
        sumY += dy;
        long oldest = now - (long)(WindowSeconds * Stopwatch.Frequency);
        while (recent.Count > 0 && recent.Peek().time < oldest)
        {
            var old = recent.Dequeue();
            sumX -= old.dx;
            sumY -= old.dy;
        }

        // Counts per second for a full tilt: 1200 at sensitivity 5.
        double full = 6000.0 / s.Sensitivity;
        double mx = sumX / WindowSeconds / full;
        double my = -sumY / WindowSeconds / full;
        if (s.InvertY) my = -my;
        double mag = Math.Sqrt(mx * mx + my * my);
        if (mag < 0.001) return (0, 0);
        double target = Math.Min(1, s.Deadzone + (1 - s.Deadzone) * Math.Min(1, mag));
        return (mx / mag * target, my / mag * target);
    }

    /// <summary>Holds the cursor in the bottom-right corner of the window in front, or lets it go.</summary>
    /// <remarks>Windows drops the clip whenever another window comes to the front, so this runs repeatedly.</remarks>
    public void HoldCursor(bool hold, IntPtr foreground)
    {
        if (hold && foreground != IntPtr.Zero && !IsShell(foreground) &&
            GetClientRect(foreground, out Rect rc) && rc.Right > 2 && rc.Bottom > 2)
        {
            var corner = new Point { X = rc.Right - 2, Y = rc.Bottom - 2 };
            ClientToScreen(foreground, ref corner);
            var clip = new Rect { Left = corner.X, Top = corner.Y, Right = corner.X + 1, Bottom = corner.Y + 1 };
            ClipCursor(&clip);
            clipped = true;
        }
        else if (clipped)
        {
            ClipCursor(null);
            clipped = false;
        }
    }

    static bool IsShell(IntPtr hwnd)
    {
        char* name = stackalloc char[64];
        int n = GetClassName(hwnd, name, 64);
        string cls = new(name, 0, n);
        return cls is "Shell_TrayWnd" or "Progman" or "WorkerW" or "Shell_SecondaryTrayWnd";
    }

    public void Dispose()
    {
        HoldCursor(false, IntPtr.Zero);
        if (window == IntPtr.Zero) return;
        var device = new RawInputDevice { UsagePage = 0x01, Usage = 0x02, Flags = RIDEV_REMOVE, Target = IntPtr.Zero };
        RegisterRawInputDevices(ref device, 1, (uint)sizeof(RawInputDevice));
        DestroyWindow(window);
    }
}
