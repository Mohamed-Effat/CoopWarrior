# Backlog

Open items. Move one into its own plan file when work starts.

## Not yet verified

- **Dynasty Warriors 3 button meanings.** Two-player works (see the game notes), but which buttons do
  what (the shipped profile assumes the classic layout) and whether the right stick turns the camera in
  every mode are not confirmed.
- **Player number with a PlayStation controller.** The player number sets the XInput slot. A DualSense
  over Bluetooth does not take an XInput slot, so whether "Player 2" makes the game put it first is untested.
- **Import and Export through the file dialogs.** The dialogs open and loading and saving work, but
  the full round trip through the dialog was not automated (the test filled the dialog's search box).
- **Real 150% display scaling.** WinUI scales by itself; only 125% was seen on screen.
- **Installing the driver from the app.** The driver was already installed on the dev PC.

## Ideas, not requested yet

- Keep bound keys and mouse input from reaching the game while the keyboard controller is on
  (low-level hooks that swallow them). Games that read the keyboard themselves give it to Player 1.
  Today a keyboard Player 1 gets both the game's keyboard controls and the pad, and a keyboard
  Player 2 also moves Player 1.

- Rename the `.kpad` extension to match "Co-op Warrior" while still reading `.kpad`.
- A "restore shipped version" action for built-in profiles.
