using System.Diagnostics;
using CoopWarrior.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CoopWarrior.Pages;

public sealed partial class AboutPage : Page
{
    bool loading = true;

    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = "Version " + Updater.CurrentVersion;
        AutoToggle.IsOn = App.Store.CheckForUpdates;
        loading = false;
        ShowUpdateState();

        Updater.Changed += ShowUpdateState;
        Unloaded += (_, _) => Updater.Changed -= ShowUpdateState;
    }

    void ShowUpdateState()
    {
        UpdateRing.IsActive = Updater.Status is UpdateStatus.Checking or UpdateStatus.Downloading;
        UpdateButton.IsEnabled = Updater.Status is not (UpdateStatus.Checking or UpdateStatus.Downloading or UpdateStatus.NotInstalled);
        UpdateButton.Content = Updater.Status == UpdateStatus.Available ? "Update and restart" : "Check for updates";
        AutoCard.IsEnabled = Updater.IsInstalled;

        UpdateCard.Description = Updater.Status switch
        {
            UpdateStatus.NotInstalled => "Automatic updates only work when Co-op Warrior was installed with its Setup program.",
            UpdateStatus.Checking => "Checking GitHub for a new version...",
            UpdateStatus.UpToDate => $"You have the latest version ({Updater.CurrentVersion}).",
            UpdateStatus.Available => $"Version {Updater.AvailableVersion} is available. Your profiles and settings are kept.",
            UpdateStatus.Downloading => $"Downloading version {Updater.AvailableVersion}: {Updater.DownloadPercent}%. Co-op Warrior restarts when it is done.",
            UpdateStatus.Failed => "Could not check for updates. " + Updater.Error,
            _ => $"You have version {Updater.CurrentVersion}.",
        };
    }

    void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (Updater.Status == UpdateStatus.Available) _ = MainWindow.UpdateNowAsync();
        else _ = CheckAsync();
    }

    static async Task CheckAsync()
    {
        await Updater.CheckAsync();
        if (Updater.Status == UpdateStatus.Failed) return;
        App.Store.LastUpdateCheck = DateTime.UtcNow;
        App.Store.SaveSettings();
    }

    void AutoToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        App.Store.CheckForUpdates = AutoToggle.IsOn;
        App.Store.SaveSettings();
    }

    void License_Click(object sender, RoutedEventArgs e)
    {
        foreach (string file in new[] { "LICENSE-EXCEPTION.txt", "THIRD-PARTY-NOTICES.txt" })
        {
            string path = Path.Combine(AppContext.BaseDirectory, file);
            if (!File.Exists(path)) continue;
            try { Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true }); }
            catch (Exception) { }
        }
    }
}
