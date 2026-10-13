# Architecture

## The big picture

```
keyboard / mouse ──► Engine worker thread ──► ViGEmClient.dll ──► ViGEmBus driver ──► virtual Xbox 360 pad
   (GetAsyncKeyState,        builds a PadReport                                        (XInput slot 0-3)
    Raw Input)               every ~4 ms, sends on change
                                   ▲
UI thread (WinUI 3) ──── Engine.Apply(GameProfile) ─── active game profile (.kpad)
   reads Engine.Current ~30x a second for the live controller drawings
```

## Engine (`src/Core/Engine.cs`)

- `Start(player)` allocates a ViGEm client, connects to the bus and adds one X360 target. Failure messages
  are returned as strings for the Dashboard's error bar.
- Player number: Windows gives a new pad the lowest free XInput slot. To land in slot N, `AddPad` first
  adds spare pads until the lower free slots are filled, adds the real pad, then removes the spares.
  A pad keeps its slot after the pads below it are removed. If slot N is already taken by another
  controller, the pad takes the next free slot and the Dashboard says so.
  See [decisions/0007](decisions/0007-player-number-spare-pads.md).
- The worker thread (`CoopWarriorLoop`) runs with a 1 ms timer resolution and sleeps 4 ms per pass.
  Each pass:
  1. F12 (ExitKey) ends the loop; F9 (PauseKey) toggles pause on its rising edge.
  2. Every 100 ms: reads the foreground window, checks the game filter, updates the cursor hold,
     and asks ViGEm for the pad's XInput slot (shown on the Dashboard) until it has one.
  3. Pumps Raw Input and turns recent mouse movement into a stick tilt.
  4. Builds the report from the key bindings plus the mouse tilt; sends it only when it changed.
- Keys are read with `GetAsyncKeyState`, so they work while the game is in front. Opposite keys cancel;
  two direction keys give a diagonal scaled by 0.7071 so diagonal movement is not faster.
- Before the loop, a 1-unit nudge then a neutral report are sent. See
  [decisions/0003](decisions/0003-vigem-first-report-nudge.md).
- When the loop ends on F12, the UI timer calls `Stop()`, which removes the pad and frees the client.
  The driver also removes the pad if the process dies.
- Settings reach the engine through `Apply(GameProfile)`: the key dictionary and `MouseSettings` are
  swapped as whole objects (volatile fields), so the worker never sees a half-updated profile.

## Mouse (`src/Core/MouseInput.cs`)

- A message-only window registered for Raw Input with `RIDEV_INPUTSINK`, created on the engine thread
  and pumped by it. Raw Input keeps reporting movement when the cursor sits at a screen edge.
- Speed is averaged over the last 30 ms, so the stick centers 30 ms after the mouse stops.
  Full tilt is reached at `6000 / Sensitivity` counts per second (1200 at the default 5).
- Dead zone compensation: any nonzero tilt is raised to at least `Deadzone` (default 25%), because
  games ignore small stick tilts.
- Cursor hold: `ClipCursor` to a 1-pixel rectangle in the bottom-right of the foreground window's
  client area. Windows drops the clip on every foreground change, so it is re-applied every 100 ms.
  It is released while paused, while Co-op Warrior's own window is in front, and for shell windows.
- Mouse buttons are ordinary bindings (`LButton`, `RButton`, ...) read with `GetAsyncKeyState`;
  `MouseSettings.Buttons` turns them off.

## Game profiles (`src/Core/GameProfile.cs`, `ProfileStore.cs`)

- One `.kpad` text file per game: name, game process, filter, notes, every binding, mouse settings.
  The parser skips unknown lines and keeps defaults for missing ones, so hand-written files load.
- Files in the data folder (`ProfileStore.RootFolder`):
  - `profiles\*.kpad` - the user's profiles.
  - `CoopWarrior.ini` - active profile file name, player number (0 = next free), tutorial seen,
    update-check setting and time, and which shipped profiles were already installed
    (so a deleted shipped profile is not copied back).
- Version 2.0 used a `configs\` folder and an `ActiveConfig` key. On start, `configs\` is renamed to
  `profiles\` when `profiles\` does not exist yet, and `ActiveConfig` is still read.
- Shipped profiles are in `DefaultProfiles\*.kpad` next to the exe and are copied into `profiles\` once.
- The data folder is `%AppData%\CoopWarrior` for installed copies (updates replace the program folder),
  the folder holding `Start.bat` for portable copies, and the exe folder for builds run from source.

## Installer and updates (`src/Program.cs`, `src/Core/Updater.cs`)

- Velopack. `Program.Main` runs `VelopackApp.Build().Run()` before WinUI starts; Setup.exe and the
  updater launch the exe with hook arguments that are handled there.
- `Updater` wraps `UpdateManager` with a `GithubSource` for the public repo (or a local folder from
  `COOPWARRIOR_UPDATE_SOURCE`). States: NotInstalled, Idle, Checking, UpToDate, Available, Downloading, Failed.
- On start, installed copies check at most once every 20 hours unless turned off on the About page.
  An available version shows a banner above every page; Update and restart stops the virtual pad,
  saves settings, downloads (usually a small delta) and restarts into the new version.
- Release packaging and publishing: `tools/release/Release.ps1`, see [releasing.md](releasing.md).

## UI (`src/MainWindow.xaml`, `src/Pages`, `src/Controls`)

- PowerToys-style shell: custom title bar (status dot, active profile name), Mica on Windows 11 and
  Acrylic on Windows 10, `NavigationView` with Dashboard, Key bindings, Mouse, Profiles, and in the
  footer How to use and About.
- `MainWindow.Tick` fires every 33 ms; pages subscribe while loaded to show live state.
- While the pad is on and not paused, the window swallows key presses that are bound to the pad,
  so they drive only the controller and not the focused control.
- Pages read the active profile when created; switching profiles re-navigates to the current page.
- `ControllerView` draws a pad in code on a 400 x 300 canvas and animates sticks, triggers and buttons.
  The Dashboard, the Mouse page and the tutorial all use it.
- The tutorial (`TutorialPage`) plays one looping, cancellable scene per step on a 640 x 300 stage.

## Single instance

A named mutex (`KeyboardPad_single_instance_9f3b`, shared with version 1). A second launch brings the
existing window to the front and exits.
