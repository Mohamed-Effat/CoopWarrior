using System.Runtime.InteropServices;

namespace CoopWarrior.Core;

/// <summary>Reads the controllers in the four XInput slots, the keyboard controller's own slot included.</summary>
public sealed partial class Gamepads
{
    /// <remarks>XINPUT_GAMEPAD has the same layout as <see cref="PadReport"/>.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    struct XInputState
    {
        public uint PacketNumber;
        public PadReport Pad;
    }

    [LibraryImport("xinput1_4.dll")] private static partial uint XInputGetState(uint userIndex, out XInputState state);

    public const int Slots = 4;

    readonly bool[] connected = new bool[Slots];
    readonly PadReport[] pads = new PadReport[Slots];
    int nextScan = Environment.TickCount;

    public bool Connected(int slot) => connected[slot];

    public PadReport State(int slot) => pads[slot];

    /// <summary>Reads the connected slots, and the empty ones when a scan is due or <paramref name="scanNow"/> is set.</summary>
    /// <remarks>For performance, Microsoft's XInput documentation advises against reading an empty slot every frame
    /// and suggests checking for new controllers every few seconds.</remarks>
    public void Poll(bool scanNow = false)
    {
        bool scan = scanNow || unchecked(Environment.TickCount - nextScan) >= 0;
        if (scan) nextScan = unchecked(Environment.TickCount + 1500);
        for (int i = 0; i < Slots; i++)
        {
            if (!connected[i] && !scan) continue;
            connected[i] = XInputGetState((uint)i, out XInputState s) == 0;
            pads[i] = connected[i] ? s.Pad : default;
        }
    }
}
