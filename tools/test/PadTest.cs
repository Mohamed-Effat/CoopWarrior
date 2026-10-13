using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Diagnostics;

public static class T
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XGamepad { public ushort wButtons; public byte bLeftTrigger; public byte bRightTrigger; public short sThumbLX; public short sThumbLY; public short sThumbRX; public short sThumbRY; }
    [StructLayout(LayoutKind.Sequential)]
    public struct XState { public uint dwPacketNumber; public XGamepad Gamepad; }
    [DllImport("xinput1_4.dll")] static extern uint XInputGetState(uint i, out XState s);

    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code, uint type);
    [DllImport("user32.dll")] static extern int GetDpiForWindow(IntPtr h);

    public static void Init() { SetProcessDPIAware(); }

    public static string Pads()
    {
        StringBuilder sb = new StringBuilder();
        for (uint i = 0; i < 4; i++)
        {
            XState s;
            uint e = XInputGetState(i, out s);
            if (e != 0) { sb.AppendLine("slot " + i + ": (none)"); continue; }
            XGamepad g = s.Gamepad;
            sb.AppendLine(string.Format("slot {0}: btn=0x{1:X4} LT={2} RT={3} LX={4} LY={5} RX={6} RY={7}", i, g.wButtons, g.bLeftTrigger, g.bRightTrigger, g.sThumbLX, g.sThumbLY, g.sThumbRX, g.sThumbRY));
        }
        return sb.ToString();
    }

    static bool Ext(byte vk) { return vk == 0x25 || vk == 0x26 || vk == 0x27 || vk == 0x28 || vk == 0xA3 || vk == 0x2D || vk == 0x2E; }
    public static void KeyDown(byte vk) { keybd_event(vk, (byte)MapVirtualKey(vk, 0), Ext(vk) ? 1u : 0u, UIntPtr.Zero); }
    public static void KeyUp(byte vk) { keybd_event(vk, (byte)MapVirtualKey(vk, 0), (Ext(vk) ? 1u : 0u) | 2u, UIntPtr.Zero); }
    public static void Tap(byte vk) { KeyDown(vk); Thread.Sleep(40); KeyUp(vk); }

    // 1=left 2=right 4=middle 5=x1 6=x2
    public static void MouseDown(int b) { MouseBtn(b, true); }
    public static void MouseUp(int b) { MouseBtn(b, false); }
    static void MouseBtn(int b, bool down)
    {
        uint f = 0, data = 0;
        if (b == 1) f = down ? 0x02u : 0x04u;
        else if (b == 2) f = down ? 0x08u : 0x10u;
        else if (b == 4) f = down ? 0x20u : 0x40u;
        else { f = down ? 0x80u : 0x100u; data = (uint)(b == 5 ? 1 : 2); }
        mouse_event(f, 0, 0, data, UIntPtr.Zero);
    }
    public static void Move(int x, int y) { SetCursorPos(x, y); }
    public static void MoveRel(int dx, int dy) { mouse_event(1, dx, dy, 0, UIntPtr.Zero); }
    [StructLayout(LayoutKind.Sequential)] struct PT { public int X, Y; }
    [DllImport("user32.dll")] static extern bool GetCursorPos(out PT p);
    public static string Cursor() { PT p; GetCursorPos(out p); return p.X + "," + p.Y; }
    public static void Click(int x, int y, int b) { SetCursorPos(x, y); Thread.Sleep(30); MouseDown(b); Thread.Sleep(40); MouseUp(b); }
    public static void DoubleClick(int x, int y) { SetCursorPos(x, y); Thread.Sleep(30); MouseDown(1); MouseUp(1); Thread.Sleep(60); MouseDown(1); MouseUp(1); }

    public static IntPtr Win(string proc)
    {
        foreach (Process p in Process.GetProcessesByName(proc)) { p.Refresh(); if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle; }
        return IntPtr.Zero;
    }
    public static string Rect(IntPtr h)
    {
        RECT r, c; GetWindowRect(h, out r); GetClientRect(h, out c);
        return string.Format("window {0},{1} {2}x{3}  client {4}x{5}", r.L, r.T, r.R - r.L, r.B - r.T, c.R, c.B);
    }
    public static int[] Bounds(IntPtr h) { RECT r; GetWindowRect(h, out r); return new int[] { r.L, r.T, r.R, r.B }; }
    public static void Front(IntPtr h) { keybd_event(0x12, 0, 0, UIntPtr.Zero); keybd_event(0x12, 0, 2, UIntPtr.Zero); SetForegroundWindow(h); }
    public static void Shot(IntPtr h, string path)
    {
        RECT r; GetWindowRect(h, out r);
        using (Bitmap b = new Bitmap(r.R - r.L, r.B - r.T))
        {
            using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(r.L, r.T, 0, 0, b.Size);
            b.Save(path, ImageFormat.Png);
        }
    }
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    public static Bitmap Capture(IntPtr h)
    {
        RECT r; GetWindowRect(h, out r);
        Bitmap b = new Bitmap(r.R - r.L, r.B - r.T);
        using (Graphics g = Graphics.FromImage(b))
        {
            IntPtr hdc = g.GetHdc();
            PrintWindow(h, hdc, 2);
            g.ReleaseHdc(hdc);
        }
        return b;
    }
    public static void Print(IntPtr h, string path) { using (Bitmap b = Capture(h)) b.Save(path, ImageFormat.Png); }
    public static void ShotScreen(string path, int x, int y, int w, int h)
    {
        using (Bitmap b = new Bitmap(w, h))
        {
            using (Graphics g = Graphics.FromImage(b)) g.CopyFromScreen(x, y, 0, 0, b.Size);
            b.Save(path, ImageFormat.Png);
        }
    }
}
