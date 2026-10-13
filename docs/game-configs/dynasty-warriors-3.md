# DYNASTY WARRIORS 3: Complete Edition Remastered

Config: `src/DefaultConfigs/Dynasty Warriors 3 Complete Edition Remastered.kpad` (ships with the app).

## Where things are

- Game folder: wherever Steam installed it, named `DYNASTY WARRIORS 3 - CE Remastered`.
- User data: `%LOCALAPPDATA%\DW3CE_RE` (settings) and `%LOCALAPPDATA%\KoeiTecmo\DW3CE_RE` (saves).

## Findings (2026-10-13, from the files)

- Unreal Engine game. `DW3CE_RE.exe` is the launcher; `DW3CE_RE\Binaries\Win64\DW3CE_RE-Win64-Shipping.exe`
  owns the window, so `GameProcess = DW3CE_RE-Win64-Shipping`.
- `InputConfig\config_controller_xbox360.vdf` is a Steam Input layout by KoeiTecmo that maps every Xbox
  button and both sticks and triggers to themselves. The game reads a standard XInput pad, and Steam Input
  is involved by default.
- `GameUserSettings.ini`: `FullscreenMode=0` (exclusive fullscreen), 1920 x 1080, 60 fps limit.
  No saved keyboard bindings were found, so the game uses its defaults.
- `Saved\Config\CrashReportClient\UECC-Windows-*` folders appear once per launch; they are not crash
  reports. There was no `Crashes` folder.

## Assumptions in the config (not confirmed in game)

- Face buttons follow the classic Dynasty Warriors layout: X normal attack, Y charge attack,
  B Musou, A jump. Mapped to left click / J, right click / K, middle click / L, Space.
- The right stick turns the camera. The mouse is set to drive it with the cursor held in the corner.

## To check on the game PC

See [the backlog](../plans/backlog.md).
