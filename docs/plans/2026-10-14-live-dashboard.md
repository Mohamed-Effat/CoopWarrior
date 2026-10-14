# Live keyboard and controllers on the Dashboard

- Date: 2026-10-14
- Status: Done

## Goal

The Dashboard shows what the README animations promise, with real data: a keyboard whose keys light up
as you press them, the keyboard's controller reacting next to it, and every other controller Windows
sees, each with its player number. The animations `demo-keys.gif` and `demo-turn-on.gif` can then be
recorded from the real Dashboard instead of from the How to use page.

## Approach

The main card on the Dashboard becomes:

```
Turn keyboard into a controller                       Off (switch)
One or two lines about the current state

[ keyboard ]                 [ keyboard's controller ]
                                  (Player 2 badge)
          Move up + Move right   (what the pressed keys do)
-----------------------------------------------------------
[ controller ]   [ controller ]   [ controller ]
 (Player 1)       (Player 3)       (Player 4)
```

- `Controls/KeyboardView` - draws every keyboard key bound to the sticks, face buttons, bumpers and
  triggers, at its place on a US QWERTY keyboard (see decisions/0008). A key lights up only while it is
  held and its action is down. `TutorialPage.ScenePlay` uses it too, so both pages draw the same keyboard.
- `Core/Engine` - records which bindings were down in the last report (`ActionDown`) and whether the
  keys are read right now (`Live`: on, not paused, and the game in front when the profile asks for it).
  Keys light up from this, so the keyboard picture never shows a key the controller did not react to.
- `Core/Gamepads` - reads the four XInput slots. Connected slots are read every tick; empty slots
  every 1.5 seconds, and at once when the keyboard controller turns on or off.
- `Pages/DashboardPage` - the new card. The keyboard's own slot (`Engine.UserIndex`) is left out of
  the other controllers, and a controller shows only after 300 ms in its slot: the spare pads that put the
  keyboard in its slot stay in XInput for about 40 ms after they are removed. Turning on pops the player badge in; a controller that connects slides in.
- `Controls/Keycap` - a label too long for a fixed-width key shrinks to fit instead of being cut off.

## Steps

- [x] Plan
- [x] `Engine.ActionDown`, `Engine.Live`
- [x] `Gamepads`
- [x] `KeyboardView`; `ScenePlay` uses it
- [x] Dashboard card
- [x] Test on this PC (see Testing). Done: off, on, badge, other controllers (`tools/test/Second-Pad.ps1`),
      tutorial, minimum size, held keys (W + D, and Y rebound to I) and pause. Light theme is not checked yet.
- [ ] New `demo-keys.gif` and `demo-turn-on.gif` and a new `dashboard.png`: moved to the backlog, they wait
      for the owner's approval.

## Testing

- Pad off: keyboard and controller dimmed, no keys light up, no badge. Other controllers still show.
- Pad on: badge shows the slot `[T]::Pads()` reports for the virtual pad. Each default key lights its
  key and moves the controller; the caption names the action. Mouse buttons light X, Y and B.
- F9 pauses: nothing lights up. A profile with "Only work while the game is in front": nothing lights
  up while the Co-op Warrior window is in front.
- A real controller (or `tools/test/Second-Pad.ps1`) shows below the line with its
  player number, moves live, and is not listed twice. Unplugging it removes it at once.
- The How to use page, step "Play with your keyboard", looks as before.
- Light and dark theme; the window at its minimum size.

## What changed from the plan

- Keys are drawn at their US QWERTY positions instead of one fixed place per action (decisions/0008),
  so a rebound key moves to where it is on the keyboard.
- The Dashboard draws every binding, the D-pad, Start, Back and the stick clicks included. The How to use
  page still draws only the 16 keys its animation presses.
- The Dashboard is wider than other pages (1200 instead of 1000), and the keyboard and controller grow
  with the window up to a set height.
- The "Other controllers" heading was dropped to fit the default window; the player badges say what they are.

## Out of scope

- Showing mouse movement on the Dashboard; the controller picture shows the stick it moves.
- Controllers that are not XInput devices, such as a DualSense without Steam Input. Windows gives them no
  XInput slot, so they do not appear.
