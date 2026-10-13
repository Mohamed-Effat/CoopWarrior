using Velopack;
using Velopack.Sources;

namespace CoopWarrior.Core;

public enum UpdateStatus { NotInstalled, Idle, Checking, UpToDate, Available, Downloading, Failed }

/// <summary>Finds, downloads and applies new versions published as GitHub Releases.</summary>
public static class Updater
{
    public const string RepoUrl = "https://github.com/Mohamed-Effat/CoopWarrior";

    /// <summary>When set to a folder of releases made by Release.ps1, used instead of GitHub,
    /// so a release can be tried before it is published.</summary>
    const string LocalSourceVariable = "COOPWARRIOR_UPDATE_SOURCE";

    static readonly UpdateManager Manager = new(CreateSource());
    static UpdateInfo? available;

    public static UpdateStatus Status { get; private set; } = Manager.IsInstalled ? UpdateStatus.Idle : UpdateStatus.NotInstalled;

    public static int DownloadPercent { get; private set; }

    public static string Error { get; private set; } = "";

    public static string? AvailableVersion => available?.TargetFullRelease.Version.ToString();

    /// <summary>False for a build run from source or the portable folder; only Setup.exe installs can update.</summary>
    public static bool IsInstalled => Manager.IsInstalled;

    public static string CurrentVersion =>
        Manager.CurrentVersion?.ToString() ?? typeof(Updater).Assembly.GetName().Version?.ToString(3) ?? "";

    /// <summary>Raised on the UI thread whenever <see cref="Status"/> or the download progress changes.</summary>
    public static event Action? Changed;

    static IUpdateSource CreateSource()
    {
        string? folder = Environment.GetEnvironmentVariable(LocalSourceVariable);
        return string.IsNullOrWhiteSpace(folder)
            ? new GithubSource(RepoUrl, null, false)
            : new SimpleFileSource(new DirectoryInfo(folder));
    }

    static void Set(UpdateStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }

    public static async Task CheckAsync()
    {
        if (!IsInstalled || Status is UpdateStatus.Checking or UpdateStatus.Downloading) return;
        Set(UpdateStatus.Checking);
        try
        {
            available = await Manager.CheckForUpdatesAsync();
            Set(available == null ? UpdateStatus.UpToDate : UpdateStatus.Available);
        }
        catch (Exception e)
        {
            Error = Describe(e);
            Set(UpdateStatus.Failed);
        }
    }

    static string Describe(Exception e) => e switch
    {
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.NotFound } => "There are no published versions on GitHub yet.",
        HttpRequestException or TaskCanceledException => "Could not reach GitHub. Check the internet connection.",
        _ => e.Message,
    };

    /// <summary>Downloads the available version, then closes the app and restarts into it.</summary>
    public static async Task UpdateAndRestartAsync(Action beforeRestart)
    {
        if (available == null || Status == UpdateStatus.Downloading) return;
        SynchronizationContext? ui = SynchronizationContext.Current;
        DownloadPercent = 0;
        Set(UpdateStatus.Downloading);
        try
        {
            await Manager.DownloadUpdatesAsync(available, p =>
            {
                DownloadPercent = p;
                ui?.Post(_ => Changed?.Invoke(), null);
            });
        }
        catch (Exception e)
        {
            Error = Describe(e);
            Set(UpdateStatus.Failed);
            return;
        }
        beforeRestart();
        Manager.ApplyUpdatesAndRestart(available);
    }
}
