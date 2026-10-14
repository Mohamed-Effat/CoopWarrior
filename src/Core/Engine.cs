using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoopWarrior.Core;

[StructLayout(LayoutKind.Sequential)]
public struct PadReport
{
    public ushort Buttons;
    public byte LeftTrigger;
    public byte RightTrigger;
    public short LeftX;
    public short LeftY;
    public short RightX;
    public short RightY;
}

public static class PadButtons
{
    public const ushort DpadUp = 0x0001, DpadDown = 0x0002, DpadLeft = 0x0004, DpadRight = 0x0008;
    public const ushort Start = 0x0010, Back = 0x0020, L3 = 0x0040, R3 = 0x0080;
    public const ushort LB = 0x0100, RB = 0x0200, A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;
}

/// <summary>Reads the keyboard and mouse and feeds a virtual Xbox 360 controller through ViGEmBus.</summary>
public sealed partial class Engine
{
    [LibraryImport("user32.dll")] private static partial short GetAsyncKeyState(int vKey);
    [LibraryImport("user32.dll")] private static partial IntPtr GetForegroundWindow();
    [LibraryImport("user32.dll")] private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [LibraryImport("winmm.dll")] private static partial uint timeBeginPeriod(uint ms);
    [LibraryImport("winmm.dll")] private static partial uint timeEndPeriod(uint ms);

    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr vigem_alloc();
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_free(IntPtr client);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_connect(IntPtr client);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_disconnect(IntPtr client);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr vigem_target_x360_alloc();
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_target_free(IntPtr target);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_add(IntPtr client, IntPtr target);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_remove(IntPtr client, IntPtr target);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_x360_update(IntPtr client, IntPtr target, PadReport report);
    [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_x360_get_user_index(IntPtr client, IntPtr target, out uint index);

    const uint VIGEM_OK = 0x20000000;

    static readonly Dictionary<string, int> ActionBits =
        GameProfile.Bindings.Select((b, i) => (b.Name, i)).ToDictionary(p => p.Name, p => p.i);

    volatile Dictionary<string, int[]> map = GameProfile.DefaultKeys();
    volatile MouseSettings mouse = new();
    volatile bool running;
    volatile bool paused;
    volatile string gameProcess = "";
    volatile int userIndex = -1;
    volatile bool mouseAvailable = true;
    volatile bool live;
    volatile uint actionsDown;
    readonly Lock reportLock = new();
    PadReport current;

    IntPtr client;
    IntPtr pad;
    bool added;
    Thread? worker;

    public bool Running => running;
    public bool Paused => paused;

    /// <summary>The XInput slot (0-3) Windows gave the virtual pad, or -1 until it is known.</summary>
    public int UserIndex => userIndex;

    /// <summary>False when Windows refused the Raw Input registration, so mouse movement cannot drive a stick.</summary>
    public bool MouseAvailable => mouseAvailable;

    public PadReport Current { get { lock (reportLock) return current; } }

    /// <summary>True while keys drive the pad: on, not paused, and the game in front when the profile asks for it.</summary>
    public bool Live => running && live;

    /// <summary>Whether a binding's keys were down when the last report was built.</summary>
    /// <remarks>Always false while not <see cref="Live"/>.</remarks>
    public bool ActionDown(string name) => Live && ActionBits.TryGetValue(name, out int bit) && (actionsDown & (1u << bit)) != 0;

    /// <summary>Takes the keys, game setting and mouse settings from a game profile. Safe while running.</summary>
    public void Apply(GameProfile p)
    {
        map = p.Keys;
        mouse = p.Mouse;
        gameProcess = p.OnlyInGame ? p.GameProcess : "";
    }

    public static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    public bool Down(string name)
    {
        if (!map.TryGetValue(name, out int[]? codes)) return false;
        bool mouseButtons = mouse.Buttons;
        foreach (int vk in codes)
        {
            if (!mouseButtons && KeyNames.IsMouse(vk)) continue;
            if (IsDown(vk)) return true;
        }
        return false;
    }

    /// <summary>Starts the virtual pad. Returns null on success, otherwise a message for the user.</summary>
    /// <param name="player">1 to 4 to ask for that XInput slot, or 0 for the next free one.</param>
    public string? Start(int player = 0)
    {
        if (running) return null;
        try
        {
            client = vigem_alloc();
            if (client == IntPtr.Zero) return "The controller library did not start.";

            uint err = vigem_connect(client);
            if (err != VIGEM_OK)
            {
                Cleanup();
                return "The controller driver (ViGEmBus) is not installed or is not running. " +
                       "Install it from the Dashboard and try again. If the driver installer asks you to restart Windows, restart first.";
            }

            string? addError = AddPad(player);
            if (addError != null)
            {
                Cleanup();
                return addError;
            }
        }
        catch (DllNotFoundException)
        {
            Cleanup();
            return "ViGEmClient.dll is missing from the program folder.";
        }
        catch (Exception e)
        {
            Cleanup();
            return "Could not turn on: " + e.Message;
        }

        timeBeginPeriod(1);
        paused = false;
        running = true;
        worker = new Thread(Loop) { IsBackground = true, Name = "CoopWarriorLoop" };
        worker.Start();
        return null;
    }

    /// <summary>Adds the pad, first filling lower free slots with spare pads so it lands in the requested slot.</summary>
    /// <remarks>
    /// Windows gives a new pad the lowest free XInput slot, and a pad keeps its slot until it is removed,
    /// so the spares can be removed again as soon as the pad has its slot.
    /// When the requested slot is already taken, the pad takes the next free one.
    /// </remarks>
    string? AddPad(int player)
    {
        var spares = new List<IntPtr>();
        try
        {
            while (true)
            {
                IntPtr target = vigem_target_x360_alloc();
                uint err = vigem_target_add(client, target);
                if (err != VIGEM_OK)
                {
                    vigem_target_free(target);
                    return $"Windows would not create the controller (code 0x{err:X}).";
                }
                int slot = WaitForSlot(target);
                if (player == 0 || slot < 0 || slot >= player - 1 || spares.Count == 3)
                {
                    pad = target;
                    added = true;
                    userIndex = slot;
                    return null;
                }
                spares.Add(target);
            }
        }
        finally
        {
            foreach (IntPtr spare in spares)
            {
                vigem_target_remove(client, spare);
                vigem_target_free(spare);
            }
        }
    }

    /// <summary>Returns the XInput slot Windows gave a pad, or -1 when it is not known within a second.</summary>
    int WaitForSlot(IntPtr target)
    {
        try
        {
            for (int i = 0; i < 50; i++)
            {
                if (vigem_target_x360_get_user_index(client, target, out uint idx) == VIGEM_OK) return (int)idx;
                Thread.Sleep(20);
            }
        }
        catch (EntryPointNotFoundException) { }
        return -1;
    }

    public void Stop()
    {
        running = false;
        Thread? w = worker;
        worker = null;
        if (w == null) return;
        if (w.IsAlive && w != Thread.CurrentThread) w.Join(1000);
        Cleanup();
        timeEndPeriod(1);
        lock (reportLock) current = default;
    }

    public void TogglePause() => paused = !paused;

    void Cleanup()
    {
        if (client != IntPtr.Zero)
        {
            try
            {
                if (added) vigem_target_remove(client, pad);
                if (pad != IntPtr.Zero) vigem_target_free(pad);
                vigem_disconnect(client);
                vigem_free(client);
            }
            catch (Exception) { }
        }
        added = false;
        pad = IntPtr.Zero;
        client = IntPtr.Zero;
        userIndex = -1;
    }

    static short Axis(bool positive, bool negative, bool diagonal)
    {
        int dir = (positive ? 1 : 0) - (negative ? 1 : 0);
        return (short)(dir * 32767 * (diagonal ? 0.7071 : 1.0));
    }

    static bool GameActive(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        IntPtr h = GetForegroundWindow();
        if (h == IntPtr.Zero) return false;
        GetWindowThreadProcessId(h, out uint pid);
        try
        {
            using Process p = Process.GetProcessById((int)pid);
            return string.Equals(p.ProcessName, name, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) { return false; }
    }

    /// <summary>Adds the mouse tilt to a stick the keys already set, keeping the result inside the stick's circle.</summary>
    static (short x, short y) Combine(short kx, short ky, double mx, double my)
    {
        double x = kx / 32767.0 + mx, y = ky / 32767.0 + my;
        double mag = Math.Sqrt(x * x + y * y);
        if (mag > 1) { x /= mag; y /= mag; }
        return ((short)Math.Round(x * 32767), (short)Math.Round(y * 32767));
    }

    PadReport Build(double mx, double my, MouseStick stick)
    {
        uint down = 0;
        bool On(string name)
        {
            if (!Down(name)) return false;
            down |= 1u << ActionBits[name];
            return true;
        }

        var r = new PadReport();
        ushort b = 0;
        if (On("DpadUp")) b |= PadButtons.DpadUp;
        if (On("DpadDown")) b |= PadButtons.DpadDown;
        if (On("DpadLeft")) b |= PadButtons.DpadLeft;
        if (On("DpadRight")) b |= PadButtons.DpadRight;
        if (On("Start")) b |= PadButtons.Start;
        if (On("Back")) b |= PadButtons.Back;
        if (On("L3")) b |= PadButtons.L3;
        if (On("R3")) b |= PadButtons.R3;
        if (On("LB")) b |= PadButtons.LB;
        if (On("RB")) b |= PadButtons.RB;
        if (On("A")) b |= PadButtons.A;
        if (On("B")) b |= PadButtons.B;
        if (On("X")) b |= PadButtons.X;
        if (On("Y")) b |= PadButtons.Y;
        r.Buttons = b;
        r.LeftTrigger = On("LT") ? (byte)255 : (byte)0;
        r.RightTrigger = On("RT") ? (byte)255 : (byte)0;

        bool mu = On("MoveUp"), md = On("MoveDown"), ml = On("MoveLeft"), mr = On("MoveRight");
        bool moveDiag = (mu != md) && (ml != mr);
        r.LeftX = Axis(mr, ml, moveDiag);
        r.LeftY = Axis(mu, md, moveDiag);

        bool cu = On("CamUp"), cd = On("CamDown"), cl = On("CamLeft"), cr = On("CamRight");
        bool camDiag = (cu != cd) && (cl != cr);
        r.RightX = Axis(cr, cl, camDiag);
        r.RightY = Axis(cu, cd, camDiag);

        if (stick == MouseStick.Left) (r.LeftX, r.LeftY) = Combine(r.LeftX, r.LeftY, mx, my);
        else if (stick == MouseStick.Right) (r.RightX, r.RightY) = Combine(r.RightX, r.RightY, mx, my);
        actionsDown = down;
        return r;
    }

    void Loop()
    {
        var neutral = new PadReport();
        PadReport last = neutral;
        bool pauseHeld = false;
        bool active = true;
        bool ownWindowInFront = false;
        IntPtr foreground = IntPtr.Zero;
        int nextCheck = Environment.TickCount;
        using var mouseInput = new MouseInput();
        mouseAvailable = mouseInput.Available;

        // A new pad reads off-center sticks in XInput until it gets a report,
        // and ViGEmBus ignores a report equal to the all-zero one it starts with.
        PadReport nudge = neutral;
        nudge.LeftX = 1;
        vigem_target_x360_update(client, pad, nudge);
        vigem_target_x360_update(client, pad, neutral);

        while (running)
        {
            if (Down("ExitKey"))
            {
                running = false;
                break;
            }

            bool pauseNow = Down("PauseKey");
            if (pauseNow && !pauseHeld) paused = !paused;
            pauseHeld = pauseNow;

            MouseSettings ms = mouse;
            int now = Environment.TickCount;
            if (unchecked(now - nextCheck) >= 0)
            {
                foreground = GetForegroundWindow();
                GetWindowThreadProcessId(foreground, out uint fgPid);
                ownWindowInFront = fgPid == (uint)Environment.ProcessId;
                active = GameActive(gameProcess);
                // The Co-op Warrior window is left free so its controls stay usable.
                mouseInput.HoldCursor(ms.Stick != MouseStick.Off && ms.LockCursor && !paused && active && !ownWindowInFront, foreground);
                if (userIndex < 0)
                {
                    try
                    {
                        if (vigem_target_x360_get_user_index(client, pad, out uint idx) == VIGEM_OK) userIndex = (int)idx;
                    }
                    catch (EntryPointNotFoundException) { userIndex = int.MaxValue; }
                }
                nextCheck = unchecked(now + 100);
            }

            mouseInput.Pump();
            live = !paused && active;
            (double mx, double my) = mouseInput.Stick(ms, live && ms.Stick != MouseStick.Off);
            PadReport cur = live ? Build(mx, my, ms.Stick) : neutral;
            if (!cur.Equals(last))
            {
                vigem_target_x360_update(client, pad, cur);
                last = cur;
                lock (reportLock) current = cur;
            }
            Thread.Sleep(4);
        }
    }
}
