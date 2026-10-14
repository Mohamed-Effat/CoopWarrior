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
- **The live Dashboard in the light theme.**

## Make the README animations real

The animations in the README are recordings of the How to use page's drawn scenes, not of the app in use
([images/README.md](../images/README.md) describes each one). The aim is for each one to show a real page.
Where the real page cannot look like the animation yet, the page is changed to match it. An animation that
cannot be real moves to `docs/plans/reference/` with a note here, and the README shows a real recording in its place.
New recordings go into the README only after the owner approves them.

| Animation | Real counterpart | What is missing |
| --- | --- | --- |
| `demo-keys.gif` | The Dashboard: keyboard, controller and caption, all live. | Nothing. Record it (it needs key presses with the app in front). |
| `demo-turn-on.gif` | The Dashboard: the switch, the keyboard's player badge, and other controllers with theirs. | Nothing. Record it with a real controller, or a second virtual pad, as Player 1. |
| `demo-mouse.gif` | The Mouse page: its controller picture follows the mouse. | No mouse drawing and no caption. Showing the mouse movement there would match the animation. |
| `demo-change-key.gif` | The Key bindings page and its Change dialog. | Nothing in the app, as far as known; compare the real dialog with the animation, then record it. |
| `demo-split-screen.gif` | None: it shows a made-up game, not the app. | Cannot be real in the app. Move it to `reference/`. Replace it with a real split-screen game recorded on the game PC, or with the Dashboard showing two players. |

Also retake `dashboard.png`: it shows the Dashboard before the live keyboard.

## Ideas, not requested yet

- Keep bound keys and mouse input from reaching the game while the keyboard controller is on
  (low-level hooks that swallow them). Games that read the keyboard themselves give it to Player 1.
  Today a keyboard Player 1 gets both the game's keyboard controls and the pad, and a keyboard
  Player 2 also moves Player 1.

- Rename the `.kpad` extension to match "Co-op Warrior" while still reading `.kpad`.
- A "restore shipped version" action for built-in profiles.
