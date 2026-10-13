# 0007. Choosing the player number with spare pads

- Date: 2026-10-14
- Status: Accepted

## Context

Players want to choose whether the keyboard is Player 1, 2, 3 or 4. ViGEmBus has no way to ask for an
XInput slot: Windows gives a new pad the lowest free slot. Most games number players by XInput slot.

## Decision

To put the pad in slot N, `Engine.AddPad` adds spare X360 pads until the free slots below N are filled,
adds the real pad, then removes the spares. A pad keeps its slot after the pads below it are removed,
so the real pad stays in slot N. This was checked on the dev PC: with a spare in slot 0, the real pad
got slot 1 and still read as slot 1 after the spare was removed.

If slot N is already taken by another controller, the real pad takes the next free slot, and the
Dashboard says which player the keyboard became.

"Next free player" (the setting's value 0) adds the pad without spares.

## Alternatives considered

- **Keep the spares connected while the pad is on.** The game would see one or more idle controllers,
  and a game that numbers players by join order could still pick them up. Removing them right away
  leaves only the real pad.

## Consequences

- For a moment while turning on, Windows sees up to three extra controllers. A game that is already
  running may notice them connect and disconnect.
- The player number only decides the XInput slot. Controllers that do not use XInput slots, such as a
  PlayStation controller over Bluetooth, are numbered by the game or by Steam Input.
