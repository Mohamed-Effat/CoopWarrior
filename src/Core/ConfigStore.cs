namespace CoopWarrior.Core;

/// <summary>The game configs in the configs folder, which one is active, and the app's own settings.</summary>
public sealed class ConfigStore
{
    const string SettingsFileName = "CoopWarrior.ini";

    readonly List<GameConfig> configs = [];
    readonly HashSet<string> seenDefaults = new(StringComparer.OrdinalIgnoreCase);
    string activeFile = "";

    /// <summary>The folder that holds Start.bat.</summary>
    /// <remarks>
    /// Build.bat deletes and recreates the program folder, so user files live one level up.
    /// A build run from the source tree has no Start.bat above it and keeps everything next to the program.
    /// </remarks>
    public static string RootFolder { get; } = FindRoot();

    public static string ConfigsFolder => Path.Combine(RootFolder, "configs");

    static string SettingsPath => Path.Combine(RootFolder, SettingsFileName);

    static string DefaultsFolder => Path.Combine(AppContext.BaseDirectory, "DefaultConfigs");

    public IReadOnlyList<GameConfig> Configs => configs;

    public GameConfig Active { get; private set; } = new() { Name = "Default" };

    public bool TutorialSeen { get; set; }

    public event Action? ActiveChanged;

    public event Action? ListChanged;

    static string FindRoot()
    {
        string app = Path.GetFullPath(AppContext.BaseDirectory);
        string? parent = Path.GetDirectoryName(app.TrimEnd(Path.DirectorySeparatorChar));
        return parent != null && File.Exists(Path.Combine(parent, "Start.bat")) ? parent : app;
    }

    public static ConfigStore Load()
    {
        var s = new ConfigStore();
        s.ReadSettings();
        Directory.CreateDirectory(ConfigsFolder);
        s.InstallDefaults();
        s.Reload();
        s.Active = s.configs.FirstOrDefault(c => c.FileName.Equals(s.activeFile, StringComparison.OrdinalIgnoreCase))
                   ?? s.configs.FirstOrDefault(c => c.Name == "Default")
                   ?? s.configs.First();
        s.SaveSettings();
        return s;
    }

    void ReadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            foreach (string line in File.ReadAllLines(SettingsPath))
            {
                int eq = line.IndexOf('=');
                if (eq < 1 || line.TrimStart().StartsWith('#')) continue;
                string name = line[..eq].Trim();
                string val = line[(eq + 1)..].Trim();
                if (name == "ActiveConfig") activeFile = val;
                else if (name == "TutorialSeen") TutorialSeen = val == "1";
                else if (name == "InstalledDefaults")
                    foreach (string f in val.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) seenDefaults.Add(f);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void SaveSettings()
    {
        string text =
            "# Co-op Warrior settings. Game configs are in the configs folder.\r\n" +
            "ActiveConfig = " + Active.FileName + "\r\n" +
            "TutorialSeen = " + (TutorialSeen ? "1" : "0") + "\r\n" +
            "InstalledDefaults = " + string.Join(" | ", seenDefaults) + "\r\n";
        try { File.WriteAllText(SettingsPath, text); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>Copies each shipped config into the configs folder once.</summary>
    /// <remarks>A shipped config the user deleted is not copied back.</remarks>
    void InstallDefaults()
    {
        if (!Directory.Exists(DefaultsFolder)) return;
        foreach (string src in Directory.GetFiles(DefaultsFolder, "*" + GameConfig.Extension))
        {
            string file = Path.GetFileName(src);
            if (seenDefaults.Contains(file)) continue;
            string dest = Path.Combine(ConfigsFolder, file);
            try
            {
                if (!File.Exists(dest)) File.Copy(src, dest);
                seenDefaults.Add(file);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    void Reload()
    {
        configs.Clear();
        foreach (string f in Directory.GetFiles(ConfigsFolder, "*" + GameConfig.Extension))
        {
            try { configs.Add(GameConfig.Load(f)); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        if (configs.Count == 0)
        {
            var d = new GameConfig { Name = "Default", FilePath = Path.Combine(ConfigsFolder, "Default" + GameConfig.Extension) };
            d.Save();
            configs.Add(d);
        }
        configs.Sort((a, b) => a.Name == "Default" ? -1 : b.Name == "Default" ? 1 : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
    }

    public void Activate(GameConfig c)
    {
        Active = c;
        SaveSettings();
        ActiveChanged?.Invoke();
    }

    /// <summary>Turns a config name into a file name that does not exist yet.</summary>
    static string FreePath(string name, string? except = null)
    {
        char[] bad = Path.GetInvalidFileNameChars();
        string stem = new string(name.Select(ch => bad.Contains(ch) ? ' ' : ch).ToArray()).Trim().TrimEnd('.');
        if (stem.Length == 0) stem = "Config";
        if (stem.Length > 80) stem = stem[..80].TrimEnd();
        string path = Path.Combine(ConfigsFolder, stem + GameConfig.Extension);
        for (int i = 2; File.Exists(path) && !string.Equals(path, except, StringComparison.OrdinalIgnoreCase); i++)
            path = Path.Combine(ConfigsFolder, $"{stem} ({i}){GameConfig.Extension}");
        return path;
    }

    public GameConfig Add(GameConfig c)
    {
        c.FilePath = FreePath(c.Name);
        c.Save();
        Reload();
        ListChanged?.Invoke();
        return configs.First(x => x.FilePath == c.FilePath);
    }

    public GameConfig Create(string name) => Add(new GameConfig { Name = name });

    public GameConfig Duplicate(GameConfig c, string name) => Add(c.Clone(name));

    public void Rename(GameConfig c, string name)
    {
        string newPath = FreePath(name, c.FilePath);
        if (!string.Equals(newPath, c.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Move(c.FilePath, newPath); }
            catch (IOException) { return; }
            c.FilePath = newPath;
        }
        c.Name = name;
        c.Save();
        if (c == Active) SaveSettings();
        ListChanged?.Invoke();
        if (c == Active) ActiveChanged?.Invoke();
    }

    public void Delete(GameConfig c)
    {
        try { File.Delete(c.FilePath); }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }
        configs.Remove(c);
        if (configs.Count == 0) Reload();
        ListChanged?.Invoke();
        if (c == Active) Activate(configs[0]);
    }

    /// <summary>Copies a .kpad file into the configs folder. Returns null if it could not be read.</summary>
    public GameConfig? Import(string path)
    {
        GameConfig c;
        try { c = GameConfig.Load(path); }
        catch (Exception) { return null; }
        return Add(c);
    }

    public static void Export(GameConfig c, string path) => File.WriteAllText(path, c.ToText());
}
