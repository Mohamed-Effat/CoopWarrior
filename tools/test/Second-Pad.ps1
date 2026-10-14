# Connects one more virtual Xbox 360 pad for a while, standing in for a real controller.
# It takes the lowest free XInput slot and cycles: left stick up, A, right stick right, LT, neutral.
param([int]$Seconds = 10)
$ErrorActionPreference = 'Stop'
$dll = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\..\src\lib\ViGEmClient.dll')).Path
if (-not ('SecondPad' -as [type])) {
  Add-Type @'
using System; using System.Runtime.InteropServices; using System.Threading;
public static class SecondPad {
  [StructLayout(LayoutKind.Sequential)] public struct Report { public ushort Buttons; public byte LT, RT; public short LX, LY, RX, RY; }
  [DllImport("kernel32.dll")] static extern IntPtr LoadLibrary(string path);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr vigem_alloc();
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_connect(IntPtr c);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_disconnect(IntPtr c);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_free(IntPtr c);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr vigem_target_x360_alloc();
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_add(IntPtr c, IntPtr t);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_remove(IntPtr c, IntPtr t);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_target_free(IntPtr t);
  [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_x360_update(IntPtr c, IntPtr t, Report r);
  public static void Run(string dll, int ms) {
    LoadLibrary(dll);
    IntPtr c = vigem_alloc();
    if (vigem_connect(c) != 0x20000000) throw new Exception("ViGEmBus is not installed or not running.");
    IntPtr t = vigem_target_x360_alloc();
    vigem_target_add(c, t);
    try {
      // A new pad reads off-center sticks until it gets a report, and ViGEmBus drops one equal to all zeros.
      Report nudge = new Report(); nudge.LX = 1;
      vigem_target_x360_update(c, t, nudge);
      var steps = new Report[5];
      steps[0].LY = 32767; steps[1].Buttons = 0x1000; steps[2].RX = 32767; steps[3].LT = 255;
      for (int i = 0; i * 700 < ms; i++) { vigem_target_x360_update(c, t, steps[i % 5]); Thread.Sleep(700); }
    } finally {
      vigem_target_remove(c, t); vigem_target_free(t); vigem_disconnect(c); vigem_free(c);
    }
  }
}
'@
}
[SecondPad]::Run($dll, $Seconds * 1000)
