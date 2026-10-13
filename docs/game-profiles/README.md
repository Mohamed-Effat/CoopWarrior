# Writing a game profile

A profile is a `.kpad` text file. The format is documented for players in `package/README.txt`;
`src/Core/GameProfile.cs` is the reader and writer. Start from `src/DefaultProfiles/Default.kpad`.

## Research a game

1. **Find the program that owns the game window.** The game filter compares the foreground window's
   process name (without `.exe`) to `GameProcess`.
   - Unreal Engine games: the folder has `<Name>.exe` (a launcher) and
     `<Name>\Binaries\Win64\<Name>-Win64-Shipping.exe`. The Shipping exe owns the window.
   - Unity games: the exe in the game folder usually owns the window.
   - If unsure, run the game and check Task Manager > Details, or the Dashboard's program list.
2. **Check how the game reads controllers.** An `InputConfig\*.vdf` or `steam_input_manifest.vdf`
   means Steam Input is involved; mention it in a `Note`, since it can merge both pads into Player 1.
3. **Look for saved keyboard bindings and display mode.**
   - Unreal: `%LOCALAPPDATA%\<Name>\Saved\Config\Windows\Input.ini` and `GameUserSettings.ini`
     (`FullscreenMode`: 0 exclusive fullscreen, 1 windowed fullscreen, 2 windowed).
   - Keep the pad's keys away from keys the game itself uses on the keyboard, or note it.
4. **Find out what the buttons do** from the game's controls screen, manual or store page.
   Write the action names in a `Note` only when confirmed; otherwise say they are assumed.

## Write the profile

- `Name` as the game calls itself; the file name can be shorter.
- `OnlyInGame = 1` when `GameProcess` is known.
- Turn on `MouseStick = Right` for games with a free camera; leave it `Off` otherwise.
- `LockCursor = 1` suits fullscreen games; in windowed games it keeps clicks inside the window.
- Use `Note =` lines for anything the player should know: assumed button meanings, Steam Input, modes
  where the camera does not move.

## Where it goes

- Shipped with the app: `src/DefaultProfiles/<file>.kpad`, then rebuild. It is copied into the
  player's `profiles\` on the next start (once).
- Only for this user: `%AppData%\CoopWarrior\profiles\<file>.kpad` (installed copy). It shows up on the next start,
  or the user can import it from the Profiles page.
- Research notes: `docs/game-profiles/<game>.md`.
