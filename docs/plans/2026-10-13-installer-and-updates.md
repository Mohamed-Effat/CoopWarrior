# Single-file installer and automatic updates

- Date: 2026-10-13
- Status: Draft

## Goal

Players download one `CoopWarrior-win-Setup.exe` that installs everything, with no admin prompt
except the one-time controller driver. The installed app finds new versions on GitHub Releases and
updates itself, keeping the player's configs.

## Decisions already made

- Engine: Velopack (MIT, `Velopack` NuGet 1.2.x and the matching `vpk` tool).
- Update source: GitHub Releases of a public repo, `github.com/Mohamed-Effat/CoopWarrior`.
- Install: per-user. Velopack installs under `%LocalAppData%\CoopWarrior`; user data moves to
  `%AppData%\CoopWarrior` so updates never touch it.

## Approach

### App changes

- **Custom entry point.** Velopack's hooks must run before WinUI starts, so add `Program.cs` with
  `[STAThread] Main` that calls `VelopackApp.Build()...Run()` first, then starts the WinUI `App`.
  Set `DISABLE_XAML_GENERATED_MAIN` in the project.
- **User data location.** `ConfigStore.RootFolder` becomes `%AppData%\CoopWarrior` for installed
  copies (configs, `CoopWarrior.ini`). A build run from `src\bin` keeps using its own folder.
- **Driver.** Ship `ViGEmBusSetup_x64.msi` inside the app. On first run (`OnFirstRun`), the Dashboard
  opens with the driver step if the driver is missing. The existing Install driver button runs the MSI.
- **Updates UI.**
  - About page gets an Updates section: current version, Check for updates, status, Update and restart.
  - On start (at most once a day) the app checks quietly; if a version is available, the Dashboard
    shows an InfoBar "Version X is ready" with Update and restart.
  - A setting "Check for updates automatically", on by default, stored in `CoopWarrior.ini`.
  - Updating while the virtual pad is on: stop the pad first, then apply and restart.
- **Single instance and updates.** Velopack restarts the app after updating; the mutex is released on exit,
  so the new process starts normally.

### Release process

- `tools/release/Release.ps1 -Version 2.1.0`:
  1. `dotnet publish src -c Release -p:Version=2.1.0 -o publish`
  2. `vpk download github` (previous release, so delta packages can be made)
  3. `vpk pack --packId CoopWarrior --packVersion 2.1.0 --packDir publish --mainExe CoopWarrior.exe
     --packTitle "Co-op Warrior" --packAuthors "Mohamed Effat" --icon ...`
  4. `vpk upload github --repoUrl ... --publish --releaseName "Co-op Warrior 2.1.0" --tag v2.1.0`,
     token from `gh auth token`.
- `docs/releasing.md` explains the steps, version numbering, and what players see.
- `Build.bat` stays for local test builds of a portable folder.

### Repo goes public first

- Create `Mohamed-Effat/CoopWarrior` (public) and push `main`.
- Before the first push, remove personal details from the docs: machine names, user names and network
  paths.
- Add the ViGEmBus license text (BSD-3 requires keeping its notice when redistributing the MSI).
- Add a license for Co-op Warrior's own code (to be chosen) and a GitHub-facing README.

## Steps

- [ ] Scrub personal paths from docs; add ViGEmBus license; choose and add the project license
- [ ] Create the public GitHub repo and push
- [ ] `Program.cs` entry point with Velopack; data folder moves to `%AppData%`
- [ ] Ship the driver MSI inside the app; first-run driver step
- [ ] Updates section on About; startup check and Dashboard InfoBar; setting to turn checks off
- [ ] `Release.ps1` and `docs/releasing.md`; install `vpk` (`dotnet tool install -g vpk`)
- [ ] Release 2.0.0 (first installer), install it from the Setup.exe on this PC
- [ ] Release 2.0.1 with a visible change; confirm the installed 2.0.0 finds it, updates and keeps configs
- [ ] Move the player's existing configs from `C:\Programs\CoopWarrior\configs` to `%AppData%`
- [ ] Update CLAUDE.md, architecture.md, the player README

## Testing

- Fresh install on this PC from the downloaded Setup.exe: shortcut, uninstall entry, first-run tutorial,
  driver step when the driver is missing (check on a PC without ViGEmBus).
- Update from 2.0.0 to 2.0.1: delta download, restart, configs and active config kept.
- Update while the pad is on: pad removed cleanly, comes back after restart only when switched on.
- Uninstall: app and shortcuts removed; `%AppData%\CoopWarrior` kept (configs survive a reinstall).

## Out of scope

- **Code signing.** An unsigned Setup.exe downloaded from the internet triggers SmartScreen
  ("Windows protected your PC" > More info > Run anyway). A signing certificate costs money; revisit later.
- Machine-wide (Program Files) installs, ARM64 builds, winget publishing.
- Removing the ViGEmBus driver on uninstall (other apps may use it).
