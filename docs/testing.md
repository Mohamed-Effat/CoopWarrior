# Testing

There are no unit tests. The app is tested against the real virtual pad: drive it, then read the
pad back through XInput. Scripts are in `tools/test/` (Windows PowerShell 5.1).

## Before you start

- **The user often works on this PC while tests run.** Scripts that inject keys or mouse movement send
  them to whatever window is in front, including the user's editor. Warn the user first, keep bursts
  short, and say when you are done.
- Prefer input-free checks: UI Automation (`UiAutomation.ps1`) clicks buttons and toggles switches
  without moving the pointer, and `[T]::Print` captures a window with `PrintWindow` even when it is covered.
- `[T]::Front` taps Alt to be allowed to bring a window forward. That Alt tap reaches the focused window
  and can swallow the next mouse click, so avoid it while a dialog in the app is listening for a key.
- Close the app before rebuilding; Build.bat refuses while it runs.

## Scripts

| Script | What it does |
| --- | --- |
| `PadTest.cs` | Helpers loaded with Add-Type: XInput readback (`[T]::Pads()`), key and mouse injection, window bounds, screenshots, `PrintWindow` captures. |
| `Load-PadTest.ps1` | Dot-source it to load `PadTest.cs` and make the process DPI aware, so coordinates are physical pixels. |
| `UiAutomation.ps1` | `Invoke-UI 'Name'` presses, selects or toggles the first matching element in the app; `Find-UI`, `Center-UI`. |
| `Test-KeyInputs.ps1` | Holds each default key (and key pairs for diagonals) and prints the pad state. Default config, pad on. |
| `Test-MouseLook.ps1` | Short bursts of relative mouse movement; prints the stick during and after. |
| `Capture-Frames.ps1` | Grabs several frames of the window into one contact sheet, to check animations. |

Typical session:

```powershell
. .\tools\test\Load-PadTest.ps1
. .\tools\test\UiAutomation.ps1
Invoke-UI 'Dashboard'
# The on/off switch has AutomationId PadToggle; see UiAutomation.ps1 for toggling by id.
[T]::Pads()                      # slot 0-3 state, or (none)
.\tools\test\Test-KeyInputs.ps1 -slot 0
```

## Checklist

Engine and pad
- [ ] Pad appears as "Controller (XBOX 360 For Windows)" in joy.cpl and reads centered right after turning on.
- [ ] Every default key moves the right axis or button; diagonals read 23169 on both axes; opposite keys cancel.
- [ ] Left and right Shift both give LT 255; left and right Ctrl give RT 255.
- [ ] F9 pauses (pad neutral, amber status) and resumes; F12 removes the pad and flips the switch off.
- [ ] Closing the window and killing the process both remove the pad.
- [ ] With the pad on and the switch focused, Space presses A and does not flip the switch.

Mouse
- [ ] With "Mouse moves a stick" on, movement tilts the chosen stick; faster tilts further; it centers within ~120 ms of stopping.
- [ ] Cursor hold: held in the bottom-right of another window in front; free while paused or while the app is in front.

Configs and UI
- [ ] First run installs the shipped configs into `configs\` and opens the tutorial.
- [ ] Use, New, Duplicate, Rename (with characters like `:` and `/`), Delete; `CoopWarrior.ini` keeps the active config.
- [ ] A version 1 `keymap.txt` loads as a config.
- [ ] Rebind dialog: key, Alt, Tab, mouse buttons inside the box, Esc cancels, Replace, Add as extra key.
- [ ] Light and dark theme; caption buttons visible in both.
- [ ] The published build starts (not just the `bin` build) - see decisions/0005.
