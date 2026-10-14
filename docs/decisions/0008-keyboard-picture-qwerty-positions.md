# 0008. Keys drawn at their QWERTY positions

- Date: 2026-10-14
- Status: Accepted

## Context

The Dashboard and the How to use page draw the keys bound to the sticks, face buttons, bumpers and
triggers (`Controls/KeyboardView`). Players rebind keys, so the drawing has to show any key, and it has
to stay small enough to sit next to the controller drawing.

## Decision

Each bound key is drawn where it sits on a US (ANSI) keyboard: its row, its offset in the row and its
width (Shift 2.25 keys, Space 6.25, and so on). Only bound keys are drawn. The keyboard is split into
three blocks (main keys, arrow and navigation keys, number pad). Each block is trimmed to the keys in use,
and the blocks sit side by side half a key apart. Keys with no known position go in a row underneath.

With the default keys, this gives Q W E above A S D, J K L in the same row as A S D, Shift and
Ctrl and Space below, and the arrow keys to the right.

## Alternatives considered

- **One fixed place per action** (W's place for Move up, and so on). It matched the default keys, but a
  rebound key stayed in the old key's place: binding Y to I left I in the A S D row, where K was.
- **The whole keyboard, with unbound keys faded.** True to scale, but about 22 keys wide; next to the
  controller drawing the keys became too small to read.

## Consequences

- The drawing changes size with the bindings. Its width follows the keys in use, so a layout that spreads
  across the keyboard is drawn smaller.
- Layouts other than US QWERTY (AZERTY, QWERTZ) are drawn by virtual-key code, so a key appears where the
  US key with that code sits.
- Mouse buttons are not drawn. The controller drawing still shows what they do.
