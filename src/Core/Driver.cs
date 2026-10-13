using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;

namespace CoopWarrior.Core;

/// <summary>Checks for and installs the ViGEmBus driver.</summary>
public static class Driver
{
    const string MsiName = "ViGEmBusSetup_x64.msi";

    public static bool Installed()
    {
        try
        {
            using RegistryKey? k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ViGEmBus");
            return k != null;
        }
        catch (Exception) { return false; }
    }

    /// <summary>Finds the driver installer.</summary>
    /// <remarks>It ships in the folder above the program folder.</remarks>
    static string? FindMsi()
    {
        string dir = AppContext.BaseDirectory;
        foreach (string candidate in new[] { Path.Combine(dir, MsiName), Path.Combine(dir, "..", MsiName) })
            if (File.Exists(candidate)) return Path.GetFullPath(candidate);
        return null;
    }

    /// <summary>Runs the installer elevated. Returns null on success, otherwise a message for the user.</summary>
    public static async Task<string?> InstallAsync()
    {
        string? msi = FindMsi();
        if (msi == null) return MsiName + " is missing from the Co-op Warrior folder.";
        try
        {
            var psi = new ProcessStartInfo("msiexec.exe", $"/i \"{msi}\" /passive /norestart")
            {
                UseShellExecute = true,
                Verb = "runas",
            };
            using Process? p = Process.Start(psi);
            if (p == null) return "Could not start the driver installer.";
            await p.WaitForExitAsync();
            // 3010: success, restart required.
            if (p.ExitCode != 0 && p.ExitCode != 3010) return $"The driver installer stopped with error code {p.ExitCode}.";
            return null;
        }
        catch (Win32Exception e) when (e.NativeErrorCode == 1223)
        {
            return "The driver was not installed, because Windows did not get permission. Click Install driver again and click Yes.";
        }
        catch (Exception e)
        {
            return "Could not start the driver installer: " + e.Message;
        }
    }
}
