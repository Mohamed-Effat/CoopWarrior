using System.Globalization;
using System.Text;

namespace CoopWarrior.Core;

public sealed record Binding(string Name, string Label, string Group, string Defaults);

public enum MouseStick { Off, Left, Right }

/// <summary>How mouse movement and mouse buttons drive the virtual pad.</summary>
/// <param name="Sensitivity">1 to 10. At 5, about 1200 mouse counts a second tilt the stick fully.</param>
/// <param name="Deadzone">0 to 0.4. The smallest tilt sent while the mouse moves, so slow moves clear the game's stick dead zone.</param>
public sealed record MouseSettings(
    MouseStick Stick = MouseStick.Off,
    double Sensitivity = 5,
    double Deadzone = 0.25,
    bool InvertY = false,
    bool Buttons = true,
    bool LockCursor = true);

/// <summary>One game's settings: key bindings, mouse options and the game filter, saved as a .kpad text file.</summary>
public sealed class GameProfile
{
    public const string Extension = ".kpad";

    public static readonly Binding[] Bindings =
    [
        new("MoveUp",    "Move up",         "Left stick",  "W"),
        new("MoveDown",  "Move down",       "Left stick",  "S"),
        new("MoveLeft",  "Move left",       "Left stick",  "A"),
        new("MoveRight", "Move right",      "Left stick",  "D"),
        new("CamUp",     "Camera up",       "Right stick", "Up"),
        new("CamDown",   "Camera down",     "Right stick", "Down"),
        new("CamLeft",   "Camera left",     "Right stick", "Left"),
        new("CamRight",  "Camera right",    "Right stick", "Right"),
        new("A",         "A",               "Face buttons", "Space"),
        new("B",         "B",               "Face buttons", "L, MButton"),
        new("X",         "X",               "Face buttons", "J, LButton"),
        new("Y",         "Y",               "Face buttons", "K, RButton"),
        new("LB",        "Left bumper",     "Bumpers and triggers", "Q"),
        new("RB",        "Right bumper",    "Bumpers and triggers", "E"),
        new("LT",        "Left trigger",    "Bumpers and triggers", "ShiftKey"),
        new("RT",        "Right trigger",   "Bumpers and triggers", "ControlKey"),
        new("DpadUp",    "D-pad up",        "D-pad", "T"),
        new("DpadDown",  "D-pad down",      "D-pad", "G"),
        new("DpadLeft",  "D-pad left",      "D-pad", "F"),
        new("DpadRight", "D-pad right",     "D-pad", "H"),
        new("L3",        "Left stick click",  "Stick clicks and menu", "Z"),
        new("R3",        "Right stick click", "Stick clicks and menu", "X"),
        new("Start",     "Start",           "Stick clicks and menu", "Return"),
        new("Back",      "Back / View",     "Stick clicks and menu", "Tab"),
    ];

    public static readonly Binding[] Hotkeys =
    [
        new("PauseKey", "Pause or resume", "Hotkeys", "F9"),
        new("ExitKey",  "Turn off",         "Hotkeys", "F12"),
    ];

    public string FilePath { get; set; } = "";
    public string Name { get; set; } = "New profile";
    public string GameProcess { get; set; } = "";
    public bool OnlyInGame { get; set; }
    public List<string> Notes { get; set; } = [];
    public Dictionary<string, int[]> Keys { get; private set; } = DefaultKeys();
    public MouseSettings Mouse { get; set; } = new();

    public string FileName => Path.GetFileName(FilePath);

    public static int[] ParseKeys(string text) =>
        text.Split(',').Select(KeyNames.Parse).Where(vk => vk > 0).Distinct().ToArray();

    public static Dictionary<string, int[]> DefaultKeys()
    {
        var m = new Dictionary<string, int[]>();
        foreach (Binding b in Bindings.Concat(Hotkeys)) m[b.Name] = ParseKeys(b.Defaults);
        return m;
    }

    public int[] Get(string name) => Keys.TryGetValue(name, out int[]? k) ? k : [];

    // Each change makes a new dictionary: the engine thread reads the old one without locking.
    public void Set(string name, int[] keys) => Keys = new Dictionary<string, int[]>(Keys) { [name] = keys };

    public void ResetKeys() => Keys = DefaultKeys();

    public GameProfile Clone(string name) => new()
    {
        Name = name,
        GameProcess = GameProcess,
        OnlyInGame = OnlyInGame,
        Notes = [.. Notes],
        Keys = new Dictionary<string, int[]>(Keys),
        Mouse = Mouse,
    };

    public static GameProfile Load(string path)
    {
        var c = Parse(File.ReadAllLines(path));
        c.FilePath = path;
        if (c.Name.Length == 0) c.Name = Path.GetFileNameWithoutExtension(path);
        return c;
    }

    /// <summary>Reads the .kpad format. Unknown names and keys are skipped so hand-written files load.</summary>
    /// <remarks>Version 1 keymap.txt files are a subset of this format and load too.</remarks>
    public static GameProfile Parse(IEnumerable<string> lines)
    {
        var c = new GameProfile { Name = "" };
        var keys = DefaultKeys();
        var mouse = new MouseSettings();
        bool onlySet = false;
        foreach (string line in lines)
        {
            string t = line.Trim();
            if (t.Length == 0 || t.StartsWith('#')) continue;
            int eq = t.IndexOf('=');
            if (eq < 1) continue;
            string name = t[..eq].Trim();
            string val = t[(eq + 1)..].Trim();
            switch (name.ToLowerInvariant())
            {
                case "name": c.Name = val; break;
                case "gameprocess": c.GameProcess = StripExe(val); break;
                case "onlyingame": c.OnlyInGame = Bool(val); onlySet = true; break;
                case "note": c.Notes.Add(val); break;
                case "mousestick":
                    mouse = mouse with { Stick = Enum.TryParse(val, true, out MouseStick s) ? s : MouseStick.Off };
                    break;
                case "mousesensitivity": mouse = mouse with { Sensitivity = Math.Clamp(Num(val, 5), 1, 10) }; break;
                case "mousedeadzone": mouse = mouse with { Deadzone = Math.Clamp(Num(val, 25), 0, 40) / 100 }; break;
                case "mouseinverty": mouse = mouse with { InvertY = Bool(val) }; break;
                case "mousebuttons": mouse = mouse with { Buttons = Bool(val) }; break;
                case "lockcursor": mouse = mouse with { LockCursor = Bool(val) }; break;
                default:
                    string? key = keys.Keys.FirstOrDefault(k => k.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (key != null) keys[key] = ParseKeys(val);
                    break;
            }
        }
        c.Keys = keys;
        c.Mouse = mouse;
        if (!onlySet) c.OnlyInGame = c.GameProcess.Length > 0;
        return c;
    }

    static bool Bool(string v) => v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase);

    static double Num(string v, double fallback) =>
        double.TryParse(v.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : fallback;

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Co-op Warrior game profile. Edit it in the app, or by hand in any text editor.");
        sb.AppendLine("# Lines starting with # are ignored. Format:  Setting = value");
        sb.AppendLine("# Key names: A-Z, D0-D9, Space, Return, Tab, Back, Up, Down, Left, Right, ShiftKey, ControlKey,");
        sb.AppendLine("#   Menu (Alt), F1-F12, LButton, RButton, MButton, XButton1, XButton2. Separate several keys with commas.");
        sb.AppendLine();
        sb.AppendLine("Name = " + Name);
        sb.AppendLine("GameProcess = " + GameProcess);
        sb.AppendLine("OnlyInGame = " + (OnlyInGame ? "1" : "0"));
        foreach (string n in Notes) sb.AppendLine("Note = " + n);
        sb.AppendLine();
        sb.AppendLine("# Controller buttons");
        foreach (Binding b in Bindings) sb.AppendLine(b.Name + " = " + string.Join(", ", Get(b.Name).Select(KeyNames.Name)));
        sb.AppendLine();
        sb.AppendLine("# Hotkeys (work while the game is in front)");
        foreach (Binding b in Hotkeys) sb.AppendLine(b.Name + " = " + string.Join(", ", Get(b.Name).Select(KeyNames.Name)));
        sb.AppendLine();
        sb.AppendLine("# Mouse. MouseStick: Off, Left or Right. Sensitivity 1-10. Deadzone 0-40 (percent).");
        sb.AppendLine("MouseStick = " + Mouse.Stick);
        sb.AppendLine("MouseSensitivity = " + Mouse.Sensitivity.ToString("0.#", CultureInfo.InvariantCulture));
        sb.AppendLine("MouseDeadzone = " + Math.Round(Mouse.Deadzone * 100).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("MouseInvertY = " + (Mouse.InvertY ? "1" : "0"));
        sb.AppendLine("MouseButtons = " + (Mouse.Buttons ? "1" : "0"));
        sb.AppendLine("LockCursor = " + (Mouse.LockCursor ? "1" : "0"));
        return sb.ToString();
    }

    public void Save()
    {
        if (FilePath.Length == 0) return;
        try { File.WriteAllText(FilePath, ToText()); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static string StripExe(string name)
    {
        name = name.Trim();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }
}
