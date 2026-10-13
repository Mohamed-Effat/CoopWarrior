# Player number, a clearer Dashboard, and "profiles"

- Date: 2026-10-14
- Status: Done

## Goal

After the first test on the game PC, the user asked for four changes:

1. Choose which player the keyboard is, instead of always the next free one.
2. Make turning the keyboard controller on the main thing on the Dashboard, with a plain name.
3. Rename "game config" to "profile" everywhere: in the app, the code and the docs.
4. Show a green banner on the Dashboard when the driver is installed.

All text in the app was also rewritten in plain words.

## Approach

- **Player number.** `Engine.Start(player)` and `Engine.AddPad`, see
  [decisions/0007](../decisions/0007-player-number-spare-pads.md). The choice is stored as `Player` in
  `CoopWarrior.ini`, because it belongs to the PC and its controllers, not to a game.
  Changing it while the controller is on reconnects the pad.
- **Dashboard.** The main card holds a large "Turn keyboard into a controller" switch and says which
  player the keyboard is. The Player number card comes right after it, then the game profile.
- **Rename.** `GameConfig` became `GameProfile`, `ConfigStore` became `ProfileStore`, `ConfigsPage`
  became `ProfilesPage`, and `DefaultConfigs` became `DefaultProfiles`. On disk, `configs\` became
  `profiles\` and `ActiveConfig` became `ActiveProfile`. Existing data folders are moved on start.
  The `.kpad` extension and the setting names inside the files did not change.
- **Driver banner.** The driver bar is always shown: green when ViGEmBus is installed, yellow with
  Install driver when it is not.

## Steps

- [x] Player number in the engine, the store and the Dashboard
- [x] Dashboard layout and green driver banner
- [x] Rename in code, file names, docs and the player README
- [x] Plain-language pass over every page, the tutorial and the error messages
- [x] Tested on the dev PC: each player number lands in the right XInput slot, also when changed
      while on; a taken slot shows the warning

## Testing

- Dev PC: UI Automation picks each player number and turns the switch on; `[T]::Pads()` shows the slot.
  A second ViGEm client holding slot 0 checks the "taken" warning.
- Game PC: choose Player 1 and Player 2 in Dynasty Warriors 3 with the DualSense connected.

## Out of scope

- Stopping the game from also reading the keyboard and mouse for Player 1. It is in the backlog.
