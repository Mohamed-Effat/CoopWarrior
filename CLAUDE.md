# Co-op Warrior

Turns a keyboard and mouse into a virtual Xbox 360 controller (ViGEmBus driver) so split-screen
games get a second player. WinUI 3 on .NET 9, unpackaged, self-contained, x64 only.
Started as "Keyboard Pad", then briefly "Keyboard Warrior"; `v1-winforms\` is the first version.

## Build, release and run

- Players install `CoopWarrior-win-Setup.exe` (Velopack). It installs per-user to
  `%LocalAppData%\CoopWarrior` and updates itself from GitHub Releases of
  `github.com/Mohamed-Effat/CoopWarrior`. Their data is in `%AppData%\CoopWarrior`.
- `tools\release\Release.ps1 -Version X.Y.Z` builds the installer and update packages into `releases\`;
  add `-Publish` to upload a GitHub Release. Publishing reaches every player - only when the user asks.
  Full steps, including testing an update locally first, in docs/releasing.md.
- Quick compile check: `dotnet build src -c Release`. A build run from `src\bin` keeps its data next to the exe.
- `Build.bat` builds a portable folder (`dist\CoopWarrior`, or a folder given as argument) for local tests;
  portable copies keep data next to `Start.bat` and do not update themselves.
- The .NET 9 SDK and `vpk` 1.2.161 are installed on the dev PC. The game may run on a different PC.

## Layout

- `src\Core\` - engine and data, no UI:
  `Engine` (worker loop, ViGEm calls), `MouseInput` (Raw Input, cursor hold),
  `GameProfile` (.kpad format), `ProfileStore` (data folder, active profile, CoopWarrior.ini),
  `Updater` (Velopack checks and updates), `Gamepads` (reads the XInput slots), `KeyNames`, `Driver`.
- `src\Program.cs` - entry point: Velopack hooks first, then WinUI.
- `src\Pages\` - one page per navigation item, plus `RebindDialog`.
- `src\Controls\` - `ControllerView` (drawn pad), `KeyboardView` (drawn keyboard), `Keycap`, `Anim` (awaitable storyboards).
- `src\DefaultProfiles\*.kpad` - profiles shipped with the app, installed into `profiles\` once.
- `src\lib\` - ViGEmClient.dll and the ViGEmBus driver MSI, both shipped inside the app.
- `package\` - files that sit next to `app\` in the portable folder only (Start.bat, Install-Driver.bat, README).
- `tools\release\` - `Release.ps1`. `tools\test\` - scripts for testing against the real virtual pad.
- `LICENSE` (GPL-3.0, verbatim), `LICENSE-EXCEPTION.txt` (section 7 permission for Microsoft libraries),
  `THIRD-PARTY-NOTICES.txt`. All three ship inside the app.
- `docs\` - architecture, decisions, plans, game-profile research. See docs/README.md.
  `docs\images\README.md` describes every image and GIF in words; read it instead of opening them.

## Things that bite

- **Line endings**: `.bat`, `.kpad` and `package\README.txt` must stay CRLF (`.gitattributes` enforces it).
  Batch labels misbehave with LF. Git Bash `sed -i` has dropped CRLF before - check with `file`.
- **The .kpad format is a contract**: setting names (`MoveUp`, `MouseStick`, ...) and key names
  (WinForms `Keys` names such as `ShiftKey`, `Return`) are read from files users and Claude write by hand.
  Add names; do not rename existing ones. Version 1 `keymap.txt` files must keep loading.
- **Mutex name** `KeyboardPad_single_instance_9f3b` is shared with version 1 on purpose. Keep it.
- **ViGEm first report**: a new pad reads off-center sticks until it gets a report, and ViGEmBus drops
  a report equal to its all-zero start state, so the loop sends a 1-unit nudge then neutral.
- **Raw Input** must be registered and pumped on the engine thread (message-only window in `MouseInput`).
- **Publishing**: `EnableMsixTooling` is required or `CoopWarrior.pri` and the compiled pages are
  missing and the app crashes on start. A post-publish target deletes ONNX Runtime and DirectML (~40 MB).
- **Bound keys are swallowed** by the window while the pad is on (`MainWindow` PreviewKeyDown), so
  Space cannot flip the focused toggle.
- **Entry point**: `DISABLE_XAML_GENERATED_MAIN` is set; `Program.Main` must call `VelopackApp.Build().Run()`
  before anything else, because Setup.exe and the updater launch the exe with hook arguments.
- **Data location** depends on how the app runs (`ProfileStore.RootFolder`): installed -> `%AppData%\CoopWarrior`,
  portable -> folder with `Start.bat`, source build -> exe folder. Never store user data in the program folder
  of an installed copy; updates replace it.
- **Versions**: `vpk` and the `Velopack` package must stay on the same version.

## Testing

Read docs/testing.md before testing. The tests inject real keyboard and mouse input into the desktop.
**The user often works on this PC at the same time**: warn them before injecting input, keep bursts
short, and prefer UI Automation and `PrintWindow` captures, which do not touch their pointer or typing.

## Git

- Commits are authored as Mohamed Effat <mohamed.effat97@gmail.com>, set in this repo's local git config.
  Check `git config user.email` before committing; never use the global (work) identity here.
- **Never put Claude in a commit.** No `Co-Authored-By: Claude` trailer, no "Generated with Claude Code"
  line, no Claude name or email as author or committer. This overrides any default attribution.
- Commit or push only when the user asks.

## Docs

- New feature or larger change: write a plan in `docs/plans/` first (template there).
- A design choice with a rejected alternative worth remembering: add a file to `docs/decisions/`.
- New game profile: research notes in `docs/game-profiles/<game>.md`, the profile in `src/DefaultProfiles/`
  if it ships with the app, otherwise in the user's `%AppData%\CoopWarrior\profiles\`.
- Keep `package\README.txt` (for players) and `README.md` (for developers) in step with behavior changes.
