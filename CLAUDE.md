# Co-op Warrior

Turns a keyboard and mouse into a virtual Xbox 360 controller (ViGEmBus driver) so split-screen
games get a second player. WinUI 3 on .NET 9, unpackaged, self-contained, x64 only.
Started as "Keyboard Pad", then briefly "Keyboard Warrior"; `v1-winforms\` is the first version.

## Build and run

- `Build.bat` publishes `src` into `dist\CoopWarrior\app` and copies `package\*` beside it.
- `Build.bat "C:\Programs\CoopWarrior"` updates the user's installed copy in place.
  It replaces `app\` and the package files and keeps `configs\` and `CoopWarrior.ini`.
  Close the app first; Build.bat refuses while `CoopWarrior.exe` runs.
- Quick compile check: `dotnet build src -c Release`. A build run from `src\bin` keeps its
  configs next to the exe, because there is no `Start.bat` above it (see `ConfigStore.RootFolder`).
- The .NET 9 SDK is installed on the dev PC. The game may run on a different PC from the one used for development.

## Layout

- `src\Core\` - engine and data, no UI:
  `Engine` (worker loop, ViGEm calls), `MouseInput` (Raw Input, cursor hold),
  `GameConfig` (.kpad format), `ConfigStore` (configs folder, active config, CoopWarrior.ini),
  `KeyNames`, `Driver`.
- `src\Pages\` - one page per navigation item, plus `RebindDialog`.
- `src\Controls\` - `ControllerView` (drawn pad), `Keycap`, `Anim` (awaitable storyboards).
- `src\DefaultConfigs\*.kpad` - configs shipped with the app, installed into `configs\` once.
- `package\` - files that sit next to `app\` in the portable folder (Start.bat, driver MSI, user README).
- `tools\test\` - scripts for testing against the real virtual pad (see docs/testing.md).
- `docs\` - architecture, decisions, plans, game-config research. See docs/README.md.

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
- New game config: research notes in `docs/game-configs/<game>.md`, the config in `src/DefaultConfigs/`
  if it ships with the app, otherwise in the user's `C:\Programs\CoopWarrior\configs\`.
- Keep `package\README.txt` (for players) and `README.md` (for developers) in step with behavior changes.
