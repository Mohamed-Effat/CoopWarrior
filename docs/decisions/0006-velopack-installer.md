# 0006. Velopack for the installer and updates

- Date: 2026-10-13
- Status: Accepted

## Context

The portable folder had about 500 files. The user wanted one installer file with everything inside, and
an easy way for players to get new versions.

## Decision

Velopack (MIT). `vpk pack` produces `CoopWarrior-win-Setup.exe` (about 79 MB) and update packages;
the app checks GitHub Releases of the public repo with `GithubSource`, downloads, and restarts into the
new version. Per-user install under `%LocalAppData%\CoopWarrior`, no admin prompt. User data moved to
`%AppData%\CoopWarrior` because each update replaces the program folder.

## Alternatives considered

- WiX MSI and bootstrapper with our own updater, as PowerToys does: full control, but more code, and
  every update downloads the whole installer.
- A fully custom installer and updater: the most code to build and keep working (replacing a running
  app, failed updates, rollback).

## Consequences

- `Program.Main` replaces the XAML-generated entry point (`DISABLE_XAML_GENERATED_MAIN`) so
  `VelopackApp.Build().Run()` runs before WinUI starts.
- Delta updates are small: 2.0.0 to 2.0.1 was 0.21 MB.
- The ViGEmBus driver still needs its own admin prompt; its MSI ships inside the app and the
  Dashboard installs it.
- `COOPWARRIOR_UPDATE_SOURCE` points the updater at a local folder to test a release before publishing.
