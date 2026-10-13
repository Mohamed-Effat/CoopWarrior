using System.Globalization;

namespace CoopWarrior.Core;

/// <summary>The game profiles in the profiles folder, which one is active, and the app's own settings.</summary>
public sealed class ProfileStore
{
    const string SettingsFileName = "CoopWarrior.ini";

    readonly List<GameProfile> profiles = [];
    readonly HashSet<string> seenDefaults = new(StringComparer.OrdinalIgnoreCase);
    string activeFile = "";

    /// <summary>The folder that holds the profiles folder and CoopWarrior.ini.</summary>
    /// <remarks>
    /// Installed copies use %AppData%\CoopWarrior, because each update replaces the program folder.
    /// The portable folder from Build.bat uses the folder holding Start.bat, one level above the program,
    /// and a build run from the source tree keeps everything next to the program.
    /// </remarks>
    public static string RootFolder { get; } = FindRoot();

    public static string ProfilesFolder => Path.Combine(RootFolder, "profiles");

    /// <summary>The folder version 2.0 kept profiles in; it is renamed to <see cref="ProfilesFolder"/> on start.</summary>
    static string OldProfilesFolder => Path.Combine(RootFolder, "configs");

    static string SettingsPath => Path.Combine(RootFolder, SettingsFileName);

    static string DefaultsFolder => Path.Combine(AppContext.BaseDirectory, "DefaultProfiles");

    public IReadOnlyList<GameProfile> Profiles => profiles;

    public GameProfile Active { get; private set; } = new() { Name = "Default" };

    public bool TutorialSeen { get; set; }

    public bool CheckForUpdates { get; set; } = true;

    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

    /// <summary>The player number (1-4) the keyboard controller asks for, or 0 for the next free one.</summary>
    public int Player { get; set; }

    public event Action? ActiveChanged;

    public event Action? ListChanged;

    static string FindRoot()
    {
        if (Updater.IsInstalled)
        {
            string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoopWarrior");
            Directory.CreateDirectory(data);
            return data;
        }
        string app = Path.GetFullPath(AppContext.BaseDirectory);
        string? parent = Path.GetDirectoryName(app.TrimEnd(Path.DirectorySeparatorChar));
        return parent != null && File.Exists(Path.Combine(parent, "Start.bat")) ? parent : app;
    }

    public static ProfileStore Load()
    {
        var s = new ProfileStore();
        s.ReadSettings();
        MoveOldFolder();
        Directory.CreateDirectory(ProfilesFolder);
        s.InstallDefaults();
        s.Reload();
        s.Active = s.profiles.FirstOrDefault(p => p.FileName.Equals(s.activeFile, StringComparison.OrdinalIgnoreCase))
                   ?? s.profiles.FirstOrDefault(p => p.Name == "Default")
                   ?? s.profiles.First();
        s.SaveSettings();
        return s;
    }

    static void MoveOldFolder()
    {
        try
        {
            if (Directory.Exists(OldProfilesFolder) && !Directory.Exists(ProfilesFolder))
                Directory.Move(OldProfilesFolder, ProfilesFolder);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
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
                // Version 2.0 wrote ActiveConfig.
                if (name is "ActiveProfile" or "ActiveConfig") activeFile = val;
                else if (name == "TutorialSeen") TutorialSeen = val == "1";
                else if (name == "CheckForUpdates") CheckForUpdates = val != "0";
                else if (name == "Player" && int.TryParse(val, out int player)) Player = Math.Clamp(player, 0, 4);
                else if (name == "LastUpdateCheck" &&
                         DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime t))
                    LastUpdateCheck = t;
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
            "# Co-op Warrior settings. Game profiles are in the profiles folder.\r\n" +
            "ActiveProfile = " + Active.FileName + "\r\n" +
            "# Player: 1 to 4, or 0 for the next free player.\r\n" +
            "Player = " + Player.ToString(CultureInfo.InvariantCulture) + "\r\n" +
            "TutorialSeen = " + (TutorialSeen ? "1" : "0") + "\r\n" +
            "CheckForUpdates = " + (CheckForUpdates ? "1" : "0") + "\r\n" +
            "LastUpdateCheck = " + LastUpdateCheck.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) + "\r\n" +
            "InstalledDefaults = " + string.Join(" | ", seenDefaults) + "\r\n";
        try { File.WriteAllText(SettingsPath, text); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>Copies each shipped profile into the profiles folder once.</summary>
    /// <remarks>A shipped profile the user deleted is not copied back.</remarks>
    void InstallDefaults()
    {
        if (!Directory.Exists(DefaultsFolder)) return;
        foreach (string src in Directory.GetFiles(DefaultsFolder, "*" + GameProfile.Extension))
        {
            string file = Path.GetFileName(src);
            if (seenDefaults.Contains(file)) continue;
            string dest = Path.Combine(ProfilesFolder, file);
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
        profiles.Clear();
        foreach (string f in Directory.GetFiles(ProfilesFolder, "*" + GameProfile.Extension))
        {
            try { profiles.Add(GameProfile.Load(f)); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        if (profiles.Count == 0)
        {
            var d = new GameProfile { Name = "Default", FilePath = Path.Combine(ProfilesFolder, "Default" + GameProfile.Extension) };
            d.Save();
            profiles.Add(d);
        }
        profiles.Sort((a, b) => a.Name == "Default" ? -1 : b.Name == "Default" ? 1 : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
    }

    public void Activate(GameProfile p)
    {
        Active = p;
        SaveSettings();
        ActiveChanged?.Invoke();
    }

    /// <summary>Turns a profile name into a file name that does not exist yet.</summary>
    static string FreePath(string name, string? except = null)
    {
        char[] bad = Path.GetInvalidFileNameChars();
        string stem = new string(name.Select(ch => bad.Contains(ch) ? ' ' : ch).ToArray()).Trim().TrimEnd('.');
        if (stem.Length == 0) stem = "Profile";
        if (stem.Length > 80) stem = stem[..80].TrimEnd();
        string path = Path.Combine(ProfilesFolder, stem + GameProfile.Extension);
        for (int i = 2; File.Exists(path) && !string.Equals(path, except, StringComparison.OrdinalIgnoreCase); i++)
            path = Path.Combine(ProfilesFolder, $"{stem} ({i}){GameProfile.Extension}");
        return path;
    }

    public GameProfile Add(GameProfile p)
    {
        p.FilePath = FreePath(p.Name);
        p.Save();
        Reload();
        ListChanged?.Invoke();
        return profiles.First(x => x.FilePath == p.FilePath);
    }

    public GameProfile Create(string name) => Add(new GameProfile { Name = name });

    public GameProfile Duplicate(GameProfile p, string name) => Add(p.Clone(name));

    public void Rename(GameProfile p, string name)
    {
        string newPath = FreePath(name, p.FilePath);
        if (!string.Equals(newPath, p.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Move(p.FilePath, newPath); }
            catch (IOException) { return; }
            p.FilePath = newPath;
        }
        p.Name = name;
        p.Save();
        if (p == Active) SaveSettings();
        ListChanged?.Invoke();
        if (p == Active) ActiveChanged?.Invoke();
    }

    public void Delete(GameProfile p)
    {
        try { File.Delete(p.FilePath); }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }
        profiles.Remove(p);
        if (profiles.Count == 0) Reload();
        ListChanged?.Invoke();
        if (p == Active) Activate(profiles[0]);
    }

    /// <summary>Copies a .kpad file into the profiles folder. Returns null if it could not be read.</summary>
    public GameProfile? Import(string path)
    {
        GameProfile p;
        try { p = GameProfile.Load(path); }
        catch (Exception) { return null; }
        return Add(p);
    }

    public static void Export(GameProfile p, string path) => File.WriteAllText(path, p.ToText());
}
