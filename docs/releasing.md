# Releasing

Players install Co-op Warrior from `CoopWarrior-win-Setup.exe` and get later versions automatically
from GitHub Releases. A release is built and uploaded by `tools/release/Release.ps1` (Velopack).

## One-time setup

- .NET 9 SDK.
- `dotnet tool install -g vpk --version 1.2.161` - keep it on the same version as the `Velopack` package in
  `src/CoopWarrior.csproj`.
- `gh auth login` as the repo owner. The script reads the token with `gh auth token`.

## Make a release

1. Pick the next version (`major.minor.patch`). Patch for fixes, minor for features.
   The version comes from the script argument, not the csproj.
2. Build and try it locally:
   ```powershell
   .\tools\release\Release.ps1 -Version 2.1.0
   ```
   Output in `releases\`: `CoopWarrior-win-Setup.exe`, `CoopWarrior-2.1.0-full.nupkg`, a delta package when
   an older version is in the folder, `CoopWarrior-win-Portable.zip`, and `releases.win.json`.
3. Test an update before publishing: start an installed copy with the update source pointed at the folder.
   ```powershell
   $env:COOPWARRIOR_UPDATE_SOURCE = "$PWD\releases"
   & "$env:LOCALAPPDATA\CoopWarrior\current\CoopWarrior.exe"
   ```
   It finds the new version on start (or About > Check for updates) and offers Update and restart.
4. Publish:
   ```powershell
   .\tools\release\Release.ps1 -Version 2.1.0 -Publish -Notes notes.md
   ```
   This downloads the latest published release (for a delta package), packs, and creates the GitHub
   Release `v2.1.0` with all files. Installed copies see it on their next check.

Pushing commits does not update anyone; only a published release does.

## What players see

- Install: Setup.exe shows a splash, installs to `%LocalAppData%\CoopWarrior` without an admin prompt,
  adds Start menu and desktop shortcuts and an entry in Settings > Apps, then starts the app.
- Data: profiles and `CoopWarrior.ini` live in `%AppData%\CoopWarrior` and survive updates and reinstalls.
- Updates: checked on start at most once every 20 hours (About page: turn off, or check now). A banner
  offers Update and restart; the delta download is usually small. The virtual pad is removed first.
- SmartScreen: the Setup.exe is not code-signed, so Windows may show "Windows protected your PC";
  More info > Run anyway.

## Notes

- `Build.bat` still builds a portable folder (no installer, no automatic updates) for local testing.
- Velopack's unauthenticated GitHub checks are limited to 60 requests an hour per IP, far above one check a day.
