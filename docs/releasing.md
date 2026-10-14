# Releasing

Players install Co-op Warrior from `CoopWarrior-win-Setup.exe` and get later versions automatically
from GitHub Releases.

**Every release is built and published on GitHub by the Release workflow** (`.github/workflows/release.yml`).
It runs `tools/release/Release.ps1` on GitHub's Windows runner. Never publish from the dev PC:

- A release downloads the previous release (about 75 MB) and uploads about 240 MB. On GitHub's runner none of
  that touches the dev PC's connection; the dev PC only sends the command to start.
- Building every release on the same kind of machine keeps the update packages small. A delta between a
  PC-built and a runner-built version is larger, because the two builds differ more.
- `Release.ps1 -Publish` refuses to run outside GitHub Actions for these reasons.

Actions minutes on standard runners are free for public repositories, and GitHub sets no size or bandwidth
limit on release files (each file must be under 2 GiB).

Pushing commits does not update anyone; only a published release does. Publishing reaches every player, so
a release happens only when the owner asks for one.

## Make a release

1. **Pick the version** (`major.minor.patch`): patch for fixes, minor for features, major for changes that
   break profiles or settings.

2. **Write the release notes** in `docs/release-notes/<version>.md`. Players read them on the release page,
   so write for players: what they can do now, what changed in how the app behaves, then how to install.
   Copy the shape of the previous notes file. Without the file the release still goes out, with no notes.

3. **Set the version** in `src/CoopWarrior.csproj` (`<Version>`) to the same number. The build takes the
   version from the workflow input, but the csproj is what a build from source reports.

4. **Optional: test the update on this PC.** This builds locally and uploads nothing.
   ```powershell
   .\tools\release\Release.ps1 -Version 2.3.0
   $env:COOPWARRIOR_UPDATE_SOURCE = "$PWD\releases"
   & "$env:LOCALAPPDATA\CoopWarrior\current\CoopWarrior.exe"
   ```
   The installed copy offers the new version (About > Check for updates) and Update and restart brings it
   back as the new version with profiles and settings kept. When `releases\` also holds the previous
   version's full package, the build makes a delta and the update uses it; otherwise it uses the full package.
   Close every running copy first: the close button only hides the window, so stop the process.

5. **Commit and push** to `main`. The workflow builds what is on `main`.

6. **Run the workflow.** Actions tab > Release > Run workflow, enter the version. Or:
   ```powershell
   gh workflow run release.yml -f version=2.3.0                 # publishes when done
   gh workflow run release.yml -f version=2.3.0 -f draft=true   # leaves a draft to publish by hand
   gh run watch                                                 # follow it; about 3 minutes
   ```
   Use the draft option when the release should be looked over on GitHub before players get it, then
   press Publish on the Releases page.

7. **Check the release** at `https://github.com/Mohamed-Effat/CoopWarrior/releases`:
   ```powershell
   gh release view v2.3.0 --json isDraft,tagName,targetCommitish,assets --jq '{isDraft,tagName,targetCommitish,assets:[.assets[].name]}'
   ```
   - It is marked Latest (or Draft, if asked for).
   - It has `CoopWarrior-win-Setup.exe`, `CoopWarrior-<version>-full.nupkg`, `CoopWarrior-<version>-delta.nupkg`,
     `CoopWarrior-win-Portable.zip`, `RELEASES` and `releases.win.json`.
   - The tag `v<version>` points at the commit the workflow built, and the notes show on the page.

## When the workflow fails

- Read the log: `gh run view --log-failed`, or the run's page on the Actions tab.
- A failure during upload can leave a draft release with some files. Delete it before running again, or the
  next upload stops on the existing tag: `gh release delete v2.3.0 --yes`. A draft has no git tag yet;
  if a tag `v<version>` was created, delete it too: `git push origin :refs/tags/v2.3.0`.
- Fix the cause, push, and run the workflow again with the same version.

## Taking a release back

Installed copies update to the newest published release. To stop a bad one from spreading, publish a fixed
version with a higher number; do not reuse a version. Deleting a release also stops new updates to it, but
copies that already updated stay on it until a newer version is published.

## Building on the dev PC

For tests only (step 4 above), never to publish.

- .NET 9 SDK, and `dotnet tool install -g vpk --version 1.2.161`. `vpk`, the `Velopack` package in
  `src/CoopWarrior.csproj` and the vpk the workflow installs must all stay on the same version.
- `.\tools\release\Release.ps1 -Version X.Y.Z` writes to `releases\` (ignored by git):
  `CoopWarrior-win-Setup.exe`, `CoopWarrior-X.Y.Z-full.nupkg`, a delta package when an older version is in
  the folder, `CoopWarrior-win-Portable.zip`, and `releases.win.json`.
- `Build.bat` builds a portable folder (no installer, no automatic updates).

## What players see

- Install: Setup.exe shows a splash, installs to `%LocalAppData%\CoopWarrior` without an admin prompt,
  adds Start menu and desktop shortcuts and an entry in Settings > Apps, then starts the app.
- Data: profiles and `CoopWarrior.ini` live in `%AppData%\CoopWarrior` and survive updates and reinstalls.
- Updates: checked on start at most once every 20 hours (About page: turn off, or check now). A banner
  offers Update and restart; the delta download is usually small. The virtual pad is removed first.
- SmartScreen: the Setup.exe is not code-signed, so Windows may show "Windows protected your PC";
  More info > Run anyway.

## Notes

- The README's download links point to `releases/latest/download/CoopWarrior-win-Setup.exe`, so they only
  work while every release has an asset with exactly that name. Velopack names it from `--packId`.
- Velopack's unauthenticated GitHub checks are limited to 60 requests an hour per IP, far above one check a day.
