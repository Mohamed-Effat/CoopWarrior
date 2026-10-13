# 0002. Raw Input for mouse movement

- Date: 2026-10-13
- Status: Accepted

## Context

Mouse movement drives a stick (usually the camera). The game runs in front, so movement must be read
while Co-op Warrior is in the background, and it must keep working when the cursor reaches a screen edge.

## Decision

Register the mouse for Raw Input with `RIDEV_INPUTSINK` on a message-only window that the engine thread
creates and pumps. Movement is averaged over 30 ms and scaled to a stick tilt.

## Alternatives considered

- Polling `GetCursorPos` and taking differences: stops at screen edges and when a game confines the cursor.
- A low-level mouse hook (`WH_MOUSE_LL`): reports cursor positions, with the same edge problem, and adds
  input latency to the whole desktop if the hook thread stalls.

## Consequences

- Only one window per process can receive Raw Input for the mouse. Nothing else in the app uses it.
- Remote desktop and some tablets send absolute positions; `MouseInput` converts those to movement.
- Holding the cursor in the game window is a separate feature (`ClipCursor`); Raw Input does not need it.
