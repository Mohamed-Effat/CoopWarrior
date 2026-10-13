# Backlog

Open items. Move one into its own plan file when work starts.

## Not yet verified

- **Dynasty Warriors 3 on the game PC with a real controller as Player 1.** Check that each player gets
  their own character, which buttons do what (the shipped config assumes the classic layout), and
  whether the right stick turns the camera in every mode. If both pads drive Player 1, try disabling
  Steam Input for the game. Update `docs/game-configs/dynasty-warriors-3.md` with what is found.
- **Import and Export through the file dialogs.** The dialogs open and loading and saving work, but
  the full round trip through the dialog was not automated (the test filled the dialog's search box).
- **Real 150% display scaling.** WinUI scales by itself; only 125% was seen on screen.
- **Installing the driver from the app.** The driver was already installed on the dev PC.

## Ideas, not requested yet

- Rename the `.kpad` extension to match "Co-op Warrior" while still reading `.kpad`.
- A "restore shipped version" action for built-in configs.
