CO-OP WARRIOR  -  keyboard + mouse as a second Xbox controller
===============================================================
For split-screen games that want two controllers when you only have one.
Portable: copy this whole folder to any 64-bit Windows 10 (1809 or newer)
or Windows 11 PC. Nothing else needs installing except the controller driver.


HOW TO USE
----------
1. Double-click Start.bat.
   The first time, the app opens on "How to use": a short animated
   walkthrough. You can open it again from the bottom of the left menu.
2. If the Dashboard shows "Driver not installed", click "Install driver"
   and say Yes to the Windows prompt (once per PC).
3. Plug in / turn on your real controller.
4. At the top of the Dashboard, pick the game config for your game
   (for example "DYNASTY WARRIORS 3: Complete Edition Remastered").
5. On the Dashboard, switch "Virtual controller" on. The dot next to the
   title turns green. The drawing of the controller moves when you press
   keys, so you can see it working.
6. Start the game and choose 2 players. The real pad is Player 1 and the
   keyboard pad is Player 2.
7. Press F12 (or switch it off) when done. The virtual pad disappears.

F9 pauses and resumes so you can type normally. While the virtual
controller is on, keys bound to it do not also press buttons in the
Co-op Warrior window.


GAME CONFIGS
------------
A game config holds everything for one game: its keys, its mouse settings,
the game's program name, and tips for that game. Co-op Warrior ships with:
  Default                                          - plain layout, any program
  DYNASTY WARRIORS 3: Complete Edition Remastered  - mouse turns the camera,
                                                     works only while the game
                                                     is in front
Open "Game configs" to switch, make a new one, duplicate, rename, delete,
import a .kpad file, or export one to share. Every change is saved at once.

Config files live in the configs folder next to Start.bat (created on the
first run), one .kpad file per game. They are plain text: open one in
Notepad to see every setting. A config file looks like this:

  Name = My game
  GameProcess = MyGame-Win64-Shipping   (program name, without .exe)
  OnlyInGame = 1                        (1 = ignore keys when the game is
                                         not in front)
  Note = A tip shown on the Dashboard (several Note lines are allowed)
  MoveUp = W                            (one line per controller button;
  A = Space, MButton                     several keys separated by commas)
  MouseStick = Right                    (Off, Left or Right)
  MouseSensitivity = 5                  (1 to 10)
  MouseDeadzone = 25                    (0 to 40, percent)
  MouseInvertY = 0
  MouseButtons = 1
  LockCursor = 1

Lines it does not know are skipped, and any setting left out keeps its
default. Keymap files from the first version of Co-op Warrior import too.
CoopWarrior.ini (next to Start.bat) remembers which config is in use.


CHANGING KEYS
-------------
Open "Key bindings" and click a row. Press the new key, or click inside
the box with a mouse button, then choose:
  Replace           - the new key replaces the old ones
  Add as extra key  - both keys work
"Reset all" restores the defaults. Keys are saved in the config in use.

Defaults: WASD move, arrow keys camera, Space = A, J / left click = X,
K / right click = Y, L / middle click = B, Q/E = LB/RB, Shift/Ctrl = LT/RT,
Enter = Start, Tab = Back, T F G H = D-pad.

"Only work while the game is in front" (Dashboard) limits the keyboard pad
to one program, so it ignores your keys when you alt-tab away. Pick the
game from the list while the game is running.


MOUSE
-----
On the Mouse page:
  Mouse moves a stick   - moving the mouse tilts a stick, like turning the
                          camera in a PC game. Choose the right stick
                          (camera) or the left stick (movement). Faster moves
                          tilt further; when the mouse stops, the stick
                          centers.
  Sensitivity           - how fast the camera turns for the same movement.
  Dead zone compensation - games ignore small stick tilts; this makes slow
                          mouse moves tilt past that point. Lower it if the
                          camera drifts, raise it if slow moves do nothing.
  Keep the cursor in the game window - holds the pointer in the corner of
                          the window in front so clicks stay in the game.
                          Switching to Co-op Warrior or pressing F9 frees it.
  Use mouse buttons     - the mouse buttons press the controller buttons
                          set on Key bindings.


IF SOMETHING GOES WRONG
-----------------------
Message about the driver     -> click "Install driver"; restart Windows if the
                                installer asks.
Game ignores the keyboard pad -> switch it on BEFORE launching the game.
Both pads move Player 1       -> turn off Steam Input for the game
                                (Steam > game > Properties > Controller), and
                                connect the real pad before switching the
                                keyboard pad on.
Keys also act in the game     -> the game still sees the real keyboard. Remove
                                its keyboard bindings in its own settings.
Start.bat says "app" missing  -> copy the whole folder again; the app folder
                                did not come along.
Antivirus warning             -> the program reads which keys are held down
                                (GetAsyncKeyState) to build the pad; some
                                antivirus tools dislike that.
Files from a downloaded zip   -> right-click the zip > Properties > Unblock
                                before extracting, or Windows may nag.


REMOVE
------
Delete the folder. To remove the driver: Settings > Apps > "Nefarius Virtual
Gamepad Emulation Bus Driver" > Uninstall.


FILES
-----
Start.bat              starts Co-op Warrior
configs\               your game configs (.kpad), made on the first run
CoopWarrior.ini    which config is in use, made on the first run
app\                   the program (WinUI 3 on .NET 9)
Install-Driver.bat     installs the driver without opening the app
ViGEmBusSetup_x64.msi  virtual gamepad driver (ViGEmBus, BSD-3). No longer
                       updated by its author, but works on current Windows.
LICENSE-vgamepad-MIT.txt  licence for ViGEmClient.dll (from the vgamepad
                       project)
