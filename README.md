# Co-op Warrior

Turns a keyboard and mouse into a virtual Xbox 360 controller through the ViGEmBus driver,
so split-screen games get a second player. WinUI 3 on .NET 9, unpackaged and self-contained.

## Build

Needs the .NET 9 SDK.

```
Build.bat                                   -> dist\CoopWarrior
Build.bat "C:\Programs\CoopWarrior"     -> updates that folder in place
```

Build.bat publishes `src` into `<output>\app` and copies `package\*` next to it.
It leaves `configs\` and `CoopWarrior.ini` in the output folder untouched.

## Layout

| Path | What it is |
| --- | --- |
| `src\` | The app. `Core\` has the engine, Raw Input mouse, game configs; `Pages\` the UI. |
| `src\DefaultConfigs\` | Game configs shipped with the app, copied into `configs\` on first run. |
| `src\lib\ViGEmClient.dll` | ViGEm client library (MIT, from the vgamepad project). |
| `package\` | Files that sit next to `app\` in the portable folder: Start.bat, the driver installer, the user README. |
| `v1-winforms\` | The first version: a single WinForms file built with the Windows built-in C# compiler. |

## Game config files

One `.kpad` text file per game, `Setting = value` per line. `src\Core\GameConfig.cs` reads and
writes them; `package\README.txt` documents the format for users. Unknown settings are skipped,
and missing ones keep their defaults.
