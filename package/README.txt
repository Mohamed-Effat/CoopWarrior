CO-OP WARRIOR  -  use your keyboard and mouse as an Xbox controller
====================================================================
For split-screen games that need one more controller than you have.

This is the PORTABLE copy. You can copy this whole folder to any 64-bit
Windows 10 (1809 or newer) or Windows 11 PC. It does not update itself.
For automatic updates, install CoopWarrior-win-Setup.exe from
https://github.com/Mohamed-Effat/CoopWarrior/releases instead.


HOW TO USE
----------
1. Double-click Start.bat.
   The first time, the app opens on "How to use", a short animated guide.
   You can open it again from the bottom of the left menu.
2. If the Dashboard shows a yellow message saying the controller driver is
   not installed, click "Install driver". When Windows asks for permission,
   click Yes. You do this once on each PC. When the driver is installed,
   the message turns green.
3. Under "Player number", choose which player your keyboard should be.
   If you choose "Next free player", the keyboard takes the first free
   number. In that case, connect your real controller first if it should
   be Player 1.
4. Under "Game profile", pick the profile for your game
   (for example "DYNASTY WARRIORS 3: Complete Edition Remastered").
5. Turn on "Turn keyboard into a controller" at the top of the Dashboard.
   The dot at the top of the window turns green. The keys you press light
   up and the controller picture moves, so you can see that it works.
   Under it, the Dashboard shows the other controllers Windows sees, each
   with its player number.
6. Start the game and choose two players.
7. When you are done, press F12 or turn the switch off. The keyboard
   controller disappears from Windows.

Closing or minimizing the window does not quit Co-op Warrior: it keeps
running next to the clock, and the keyboard controller keeps working. To
quit, right-click the Co-op Warrior icon next to the clock and choose Exit.
You can change this on the Settings page.

F9 pauses and resumes, so you can type normally in between. While the
keyboard controller is on, the keys it uses do not also press buttons in
the Co-op Warrior window.


PROFILES
--------
A profile holds everything for one game:
  - its keys
  - its mouse settings
  - the game's program name
  - tips for that game
Co-op Warrior comes with two profiles:
  Default                                          - the standard keys, works
                                                     in any program
  DYNASTY WARRIORS 3: Complete Edition Remastered  - the mouse turns the
                                                     camera, and the keys only
                                                     work while the game is
                                                     in front
On the "Profiles" page you can switch profiles, make a new one, make a
copy, rename, delete, import a .kpad file, or export one to share.
Every change is saved straight away.

Profiles are files in the profiles folder next to Start.bat. The folder is
made on the first run, with one .kpad file per game. The files are plain
text, so you can open one in Notepad to see every setting. A profile file
looks like this:

  Name = My game
  GameProcess = MyGame-Win64-Shipping   (program name, without .exe)
  OnlyInGame = 1                        (1 = keys only work while the game
                                         is in front)
  Note = A tip shown on the Dashboard (you can have several Note lines)
  MoveUp = W                            (one line per controller button;
  A = Space, MButton                     separate several keys with commas)
  MouseStick = Right                    (Off, Left or Right)
  MouseSensitivity = 5                  (1 to 10)
  MouseDeadzone = 25                    (0 to 40, percent)
  MouseInvertY = 0
  MouseButtons = 1
  LockCursor = 1

Lines Co-op Warrior does not know are skipped. A setting you leave out
keeps its default. Keymap files from the first version of Co-op Warrior
can be imported too.
CoopWarrior.ini (next to Start.bat) remembers which profile is in use and
which player number you chose.


CHANGING KEYS
-------------
Open "Key bindings" and click a row. Press the new key, or click inside
the box with a mouse button. Then choose:
  Replace           - the new key replaces the old ones
  Add as extra key  - both keys work
"Reset all" puts back the default keys. Keys are saved in the profile in use.

Default keys: W A S D move, the arrow keys turn the camera, Space = A,
J or left click = X, K or right click = Y, L or middle click = B,
Q and E = LB and RB, Shift and Ctrl = LT and RT, Enter = Start, Tab = Back,
T F G H = D-pad.

"Only work while the game is in front" on the Dashboard makes the keyboard
controller work in one program only. When you switch to another window,
your keys work as normal keys again. Start the game first, then pick it
from the list.


MOUSE
-----
On the Mouse page:
  Mouse moves a stick   - moving the mouse tilts a stick, so you can turn
                          the camera like in a PC game. Choose the right
                          stick (camera) or the left stick (movement).
                          Moving faster tilts the stick further. When the
                          mouse stops, the stick goes back to the center.
  Sensitivity           - how fast the camera turns when you move the mouse
                          the same distance.
  Smallest tilt         - games ignore a stick that is tilted only a little.
                          This makes even slow mouse moves tilt the stick far
                          enough. Lower it if the camera keeps moving on its
                          own. Raise it if slow mouse moves do nothing.
  Keep the mouse pointer in the game window
                        - holds the pointer in the corner of the game window,
                          so your clicks stay in the game. The pointer is
                          free again when you press F9 or switch to
                          Co-op Warrior.
  Use mouse buttons     - the mouse buttons press the controller buttons you
                          choose on the Key bindings page.


IF SOMETHING GOES WRONG
-----------------------
A message about the driver
  -> Click "Install driver". If the installer asks you to restart Windows,
     restart.
The game ignores the keyboard controller
  -> Turn it on BEFORE you start the game.
Your keyboard is not the player number you chose
  -> Another controller already has that number. Disconnect the other
     controller, then turn the keyboard controller off and on again.
Both controllers move the same player
  -> Turn off Steam Input for the game: in Steam, right-click the game >
     Properties > Controller > Disable Steam Input.
Keys also do things in the game by themselves
  -> The game also reads the real keyboard and mouse. In the game's own
     settings, remove its keyboard controls, or change Co-op Warrior's keys
     to keys the game does not use.
Start.bat says the "app" folder is missing
  -> Copy the whole folder again. The app folder did not come along.
An antivirus warning
  -> Co-op Warrior reads which keys are held down (GetAsyncKeyState) to
     work the controller. Some antivirus programs do not like that.
     Malwarebytes may flag ViGEmBusSetup_x64.msi as "Malware.AI". That is a
     guess by its machine-learning scanner, not a known threat: the file is
     the official ViGEmBus driver installer, signed by Nefarius Software
     Solutions. Allow it or restore it from quarantine. Once the driver is
     installed, Co-op Warrior works without that file.
Windows warns about files from a downloaded zip
  -> Right-click the zip > Properties > Unblock, then extract it again.


REMOVE
------
Delete the folder. To remove the driver: Settings > Apps > "Nefarius Virtual
Gamepad Emulation Bus Driver" > Uninstall.


FILES
-----
Start.bat              starts Co-op Warrior
profiles\              your game profiles (.kpad), made on the first run
CoopWarrior.ini        your settings, made on the first run
app\                   the program (WinUI 3 on .NET 9), including the
                       ViGEmBus driver installer and the license files
                       (LICENSE, LICENSE-EXCEPTION.txt, THIRD-PARTY-NOTICES.txt)
Install-Driver.bat     installs the driver without opening the app
