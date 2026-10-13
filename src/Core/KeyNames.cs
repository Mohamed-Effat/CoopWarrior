namespace CoopWarrior.Core;

/// <summary>Converts Windows virtual-key codes to keymap.txt names and to labels shown in the window.</summary>
/// <remarks>
/// The names are the WinForms <c>Keys</c> enum names used by keymap.txt files written by version 1,
/// so those files keep loading.
/// </remarks>
public static class KeyNames
{
    static readonly Dictionary<int, string> Names = new();
    static readonly Dictionary<string, int> Codes = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<int, string> Labels = new()
    {
        [1] = "Mouse left", [2] = "Mouse right", [4] = "Mouse middle", [5] = "Mouse side 1", [6] = "Mouse side 2",
        [8] = "Backspace", [9] = "Tab", [13] = "Enter", [16] = "Shift", [17] = "Ctrl", [18] = "Alt",
        [19] = "Pause", [20] = "Caps Lock", [27] = "Esc", [32] = "Space",
        [33] = "Page Up", [34] = "Page Down", [35] = "End", [36] = "Home",
        [37] = "←", [38] = "↑", [39] = "→", [40] = "↓",
        [44] = "Print Screen", [45] = "Insert", [46] = "Delete", [91] = "Win", [92] = "Win", [93] = "Menu",
        [106] = "Num *", [107] = "Num +", [109] = "Num -", [110] = "Num .", [111] = "Num /",
        [144] = "Num Lock", [145] = "Scroll Lock",
        [186] = ";", [187] = "=", [188] = ",", [189] = "-", [190] = ".", [191] = "/", [192] = "`",
        [219] = "[", [220] = "\\", [221] = "]", [222] = "'", [226] = "\\",
    };

    static KeyNames()
    {
        void Add(int vk, string name, params string[] aliases)
        {
            Names[vk] = name;
            Codes[name] = vk;
            foreach (string a in aliases) Codes[a] = vk;
        }

        Add(1, "LButton"); Add(2, "RButton"); Add(4, "MButton"); Add(5, "XButton1"); Add(6, "XButton2");
        Add(8, "Back", "Backspace"); Add(9, "Tab"); Add(12, "Clear"); Add(13, "Return", "Enter");
        Add(16, "ShiftKey", "Shift"); Add(17, "ControlKey", "Control", "Ctrl"); Add(18, "Menu", "Alt");
        Add(19, "Pause"); Add(20, "Capital", "CapsLock"); Add(27, "Escape", "Esc"); Add(32, "Space");
        Add(33, "PageUp", "Prior"); Add(34, "PageDown", "Next"); Add(35, "End"); Add(36, "Home");
        Add(37, "Left"); Add(38, "Up"); Add(39, "Right"); Add(40, "Down");
        Add(44, "PrintScreen", "Snapshot"); Add(45, "Insert"); Add(46, "Delete");
        for (int i = 0; i <= 9; i++) Add(48 + i, "D" + i);
        for (char c = 'A'; c <= 'Z'; c++) Add(c, c.ToString());
        Add(91, "LWin"); Add(92, "RWin"); Add(93, "Apps");
        for (int i = 0; i <= 9; i++) Add(96 + i, "NumPad" + i);
        Add(106, "Multiply"); Add(107, "Add"); Add(108, "Separator"); Add(109, "Subtract");
        Add(110, "Decimal"); Add(111, "Divide");
        for (int i = 1; i <= 24; i++) Add(111 + i, "F" + i);
        Add(144, "NumLock"); Add(145, "Scroll");
        Add(160, "LShiftKey"); Add(161, "RShiftKey"); Add(162, "LControlKey"); Add(163, "RControlKey");
        Add(164, "LMenu"); Add(165, "RMenu");
        Add(186, "Oem1", "OemSemicolon"); Add(187, "Oemplus"); Add(188, "Oemcomma"); Add(189, "OemMinus");
        Add(190, "OemPeriod"); Add(191, "OemQuestion", "Oem2"); Add(192, "Oemtilde", "Oem3");
        Add(219, "OemOpenBrackets", "Oem4"); Add(220, "OemPipe", "Oem5"); Add(221, "OemCloseBrackets", "Oem6");
        Add(222, "OemQuotes", "Oem7"); Add(226, "OemBackslash", "Oem102");
    }

    public static bool IsMouse(int vk) => vk is >= 1 and <= 6 and not 3;

    public static string Name(int vk) => Names.TryGetValue(vk, out string? n) ? n : "VK" + vk;

    public static int Parse(string name)
    {
        name = name.Trim();
        if (Codes.TryGetValue(name, out int vk)) return vk;
        if (name.StartsWith("VK", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(name.AsSpan(2), out vk) && vk is > 0 and < 256) return vk;
        return 0;
    }

    public static string Label(int vk)
    {
        if (Labels.TryGetValue(vk, out string? l)) return l;
        if (vk is >= 48 and <= 57) return ((char)vk).ToString();
        if (vk is >= 96 and <= 105) return "Num " + (vk - 96);
        if (vk is >= 160 and <= 165) return vk < 162 ? "Shift" : vk < 164 ? "Ctrl" : "Alt";
        return Name(vk);
    }
}
