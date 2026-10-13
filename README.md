# Co-op Warrior

Play local split-screen games with one controller and a keyboard. Co-op Warrior turns your keyboard and
mouse into a second Xbox controller, so the games that need two pads see two pads.

- Live controller preview, PowerToys-style settings, light and dark theme
- Choose which player the keyboard is (Player 1 to 4)
- Rebind any button to keys or mouse buttons; mouse movement can drive the camera stick
- Game profiles (`.kpad` files): one per game, shareable, with a profile for
  DYNASTY WARRIORS 3: Complete Edition Remastered included
- Pause (F9) and turn-off (F12) hotkeys that work while the game is in front
- Installs per user without admin rights and updates itself

## Install

Download `CoopWarrior-win-Setup.exe` from the [latest release](https://github.com/Mohamed-Effat/CoopWarrior/releases/latest)
and run it. Windows 10 (1809 or newer) or Windows 11, 64-bit.

The first start walks you through the rest, including the one-time install of the free
[ViGEmBus](https://github.com/nefarius/ViGEmBus) virtual gamepad driver, which needs one admin prompt.

The installer is not code-signed yet, so Windows SmartScreen may warn you: choose More info > Run anyway.

## Build from source

Needs the .NET 9 SDK.

```powershell
dotnet build src -c Release                        # compile check
.\tools\release\Release.ps1 -Version 2.0.1         # installer and update packages in releases\
Build.bat                                          # portable folder in dist\CoopWarrior (no auto-update)
```

See [docs/releasing.md](docs/releasing.md) for publishing a release, and [docs/](docs/README.md) for how
the app works.

## License

Co-op Warrior is free software under the [GNU General Public License v3](LICENSE), with an
[additional permission](LICENSE-EXCEPTION.txt) to combine it with Microsoft's Windows App SDK, WinUI and .NET
libraries. Bundled third-party software and its licenses are listed in
[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

Made by Mohamed Effat.
