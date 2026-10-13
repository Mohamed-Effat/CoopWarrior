# Architecture

## The big picture

```
keyboard / mouse ──► Engine worker thread ──► ViGEmClient.dll ──► ViGEmBus driver ──► virtual Xbox 360 pad
   (GetAsyncKeyState,        builds a PadReport                                        (XInput slot 0-3)
    Raw Input)               every ~4 ms, sends on change
                                   ▲
UI thread (WinUI 3) ──── Engine.Apply(GameConfig) ─── active game config (.kpad)
   reads Engine.Current ~30x a second for the live controller drawings
```

## Engine (`src/Core/Engine.cs`)

- `Start()` allocates a ViGEm client, connects to the bus and adds one X360 target. Failure messages
  are returned as strings for the Dashboard's error bar.
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
- Settings reach the engine through `Apply(GameConfig)`: the key dictionary and `MouseSettings` are
  swapped as whole objects (volatile fields), so the worker never sees a half-updated config.

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

## Game configs (`src/Core/GameConfig.cs`, `ConfigStore.cs`)

- One `.kpad` text file per game: name, game process, filter, notes, every binding, mouse settings.
  The parser skips unknown lines and keeps defaults for missing ones, so hand-written files load.
- Files on disk, relative to the folder holding `Start.bat`:
  - `configs\*.kpad` - the user's configs.
  - `CoopWarrior.ini` - active config file name, tutorial seen, and which shipped configs were
    already installed (so a deleted shipped config is not copied back).
  - `app\DefaultConfigs\*.kpad` - shipped configs, copied into `configs\` on first run.
- `ConfigStore.RootFolder` is the parent of `app\` when it holds `Start.bat`, otherwise the exe folder.
  That keeps user files outside `app\`, which Build.bat deletes and recreates.

## UI (`src/MainWindow.xaml`, `src/Pages`, `src/Controls`)

- PowerToys-style shell: custom title bar (status dot, active config name), Mica on Windows 11 and
  Acrylic on Windows 10, `NavigationView` with Dashboard, Key bindings, Mouse, Game configs, and in the
  footer How to use and About.
- `MainWindow.Tick` fires every 33 ms; pages subscribe while loaded to show live state.
- While the pad is on and not paused, the window swallows key presses that are bound to the pad,
  so they drive only the controller and not the focused control.
- Pages read the active config when created; switching configs re-navigates to the current page.
- `ControllerView` draws a pad in code on a 400 x 300 canvas and animates sticks, triggers and buttons.
  The Dashboard, the Mouse page and the tutorial all use it.
- The tutorial (`TutorialPage`) plays one looping, cancellable scene per step on a 640 x 300 stage.

## Single instance

A named mutex (`KeyboardPad_single_instance_9f3b`, shared with version 1). A second launch brings the
existing window to the front and exits.
