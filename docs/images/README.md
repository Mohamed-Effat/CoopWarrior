# Images

What each image in this folder shows, in words, so you can understand it without opening it.
All images use the app's dark theme with the purple accent color. "Accent" below means that purple.

Every GIF loops and is 720 x 349 pixels. Each one is a recording of an animated scene from the
How to use page (`src/Pages/TutorialPage.xaml.cs`), not of the app in use. The scene method is named for each.
[plans/backlog.md](../plans/backlog.md#make-the-readme-animations-real) tracks replacing them with recordings of the real pages.

## The controller drawing

Several images use the same Xbox controller outline (`src/Controls/ControllerView.cs`): a dark grey pad
with LT and RT triggers on top, LB and RB bumpers under them, a left stick at upper left, a D-pad and a
right stick lower down, Back and Start pills and a round Guide button in the middle, and four face
buttons on the right (Y yellow at the top, B red at the right, A green at the bottom, X blue at the left).
When a button is pressed it fills: face buttons with their own color, everything else with the accent.
A stick that is tilted turns accent and moves off center. A pulled trigger fills with the accent from the bottom up.

## Animations

### demo-keys.gif

Keys light up and the controller reacts. 86 frames, 16.4 seconds. Scene: `ScenePlay`.

- Left: a keyboard drawn as keycaps, laid out like the default keys. Row 1: Q W E. Row 2: A S D, then
  J K L. Then a wide Shift, then Ctrl and a wide Space. Bottom right: the arrow keys (up above left, down, right).
  The app now draws each key at its real QWERTY position (decisions/0008), so the arrow keys sit to the
  right of J K L and Space starts further right; this recording predates that.
- Right: the controller drawing. Bottom center: a one-line caption in bold white.
- Each step holds keys for 0.75 seconds: the keycaps turn accent and the controller shows the result.
  1. W, A, S, D one at a time: the left stick moves up, left, down, right. Caption "Move with the left stick".
  2. W and D together: the left stick moves up and right. "Hold two keys to move diagonally".
  3. Up arrow, then left arrow: the right stick moves. "Arrow keys turn the camera".
  4. Space: A turns green. "Jump or confirm with A".
  5. J, then K: X turns blue, then Y yellow. "Attack with X and Y".
  6. L: B turns red. "B goes back".
  7. Q, then E: LB, then RB. "Bumpers".
  8. Shift, then Ctrl: LT, then RT fill. "Triggers".

### demo-turn-on.gif

Turning on the switch adds a second controller as Player 2. 44 frames, 6.8 seconds. Scene: `SceneToggle`.

- Top: a dark card with a gamepad icon, the text "Turn keyboard into a controller", "Off" and a switch.
- Below left: a controller with a purple "Player 1" badge, standing for a real controller already connected.
- A mouse cursor moves from the bottom right to the switch and clicks it (a ripple ring). The switch turns
  accent and the text says "On".
- A second controller springs up from below on the right, and a green "Player 2" badge with a keyboard
  icon pops in under it.
- The cursor goes back to the corner. Player 2's left stick tilts up, then right. Player 1 presses A (green).

### demo-mouse.gif

Moving the mouse tilts the right stick. 44 frames, 5.6 seconds. Scene: `SceneMouse`.

- Left: a computer mouse drawn as an outline with a purple scroll wheel. Right: the controller drawing.
  Bottom center: a caption.
- The mouse slides right and the right stick tilts right: "Slide right: the camera turns right".
- It stops and the stick returns to the center: "Stop: the stick goes back to the center".
- It moves left quickly and the stick tilts all the way left: "Move faster: the stick tilts all the way".
- It stops again, then moves up and the stick tilts up: "Up and down work too". It stops, fades out and back.

### demo-change-key.gif

Changing the key for Move up from W to P. 72 frames, 7.2 seconds. Scene: `SceneRebind`.

- Top: a card row with an up arrow, "Move up", a W keycap and a pencil icon.
- The cursor clicks the row. A dialog titled "Change Move up" opens. It has a dashed box with "Listening..."
  over a pulsing purple circle, and the buttons Replace (accent), Add as extra key and Cancel.
- On the right, a P keycap labeled "You press" appears and is pressed. The dashed box now shows a large
  purple P keycap.
- The cursor clicks Replace. The dialog closes and the row's key changes from W to P, flashing accent once.

### demo-split-screen.gif

A game going from a menu to split screen. 39 frames, 7.2 seconds. Scene: `SceneSplitScreen`.

- A grey monitor drawing with a dark navy screen. On the screen: a menu with "1 PLAYER" highlighted in
  orange and "2 PLAYERS" under it.
- The highlight moves to "2 PLAYERS" and blinks twice. The menu fades out, a white line grows down the
  middle, and the screen splits: left half blue with "P1" and a gamepad icon, right half green with
  "P2" and a keyboard icon. It holds for about 2.5 seconds, then fades back to the menu.
- This is a made-up game, drawn by the app. It does not show Co-op Warrior itself.

## Screenshots

Real screenshots of the app, 1000 x 750 pixels, window at its default size, profile
"DYNASTY WARRIORS 3: Complete Edition Remastered" in use. The title bar shows the app name, an On or Off
status chip and the profile name. A navigation pane on the left lists Dashboard, Key bindings, Mouse,
Profiles, and at the bottom How to use and About.

### dashboard.png

The Dashboard before the live keyboard (see [plans/2026-10-14-live-dashboard.md](../plans/2026-10-14-live-dashboard.md)).
A green banner "The controller driver is installed". A large card with the controller drawing on the
left and, on the right, "Turn keyboard into a controller", an enlarged switch set to On,
"Your keyboard is Player 1", and a note that the profile only works while the game is in front.
Below: the "Player number" card set to "Next free player", the "Game profile" card, and the start of
a purple "Tips for this game" box.

### bindings.png

The Key bindings page with the switch on. A "Reset all" button at the top right, and the text "Click a row
to change its key...". Groups "Left stick" (Move up W, Move down S, Move left A, Move right D) and
"Right stick" (Camera up, Camera down, with arrow keycaps). Each row has an arrow icon, the action name,
its keycaps and a pencil icon.

### mouse.png

The Mouse page. A card with the controller drawing and "Look around with the mouse": "Move your mouse and
watch the stick...". Below, "Mouse moves a stick" is On and expanded: Stick "Right stick (camera)",
Sensitivity 5 on a slider, Smallest tilt 25% on a slider, and "Invert up and down" Off.

### profiles.png

The Profiles page with the switch off. Buttons "New profile" (accent), "Import" and an export icon.
Two profile rows: "Default" (works in any program, keyboard only, with a Use button) and
"DYNASTY WARRIORS 3: Complete Edition Remastered" (its game program, the mouse turns the camera,
marked "In use").

## Other

- `logo.png` - the app icon, 256 x 256: a white gamepad outline (a plus-shaped D-pad on the left, two
  round buttons on the right) on a blue rounded square.
