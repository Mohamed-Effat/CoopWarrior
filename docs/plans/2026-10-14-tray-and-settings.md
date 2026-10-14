# Notification area (tray) and a Settings page

- Date: 2026-10-14
- Status: Done

## Goal

Minimizing or closing the window puts Co-op Warrior in the notification area next to the clock, so the
keyboard controller keeps working while the window is out of the way. A new Settings page turns each
behavior on or off.

## Approach

- `TrayIcon` (new, `src\TrayIcon.cs`) - the notification area icon through `Shell_NotifyIcon`. It subclasses
  the main window (`SetWindowSubclass`) to receive the icon's clicks, `WM_SIZE` (minimize) and
  `TaskbarCreated` (Explorer restarted, so the icon is added again). Left click opens the window.
  Right click shows a menu: Open Co-op Warrior, Turn keyboard into a controller (checked while on), Exit.
- `MainWindow` - shows the icon while either setting is on. Minimize hides the window when
  "Minimize to the notification area" is on. The close button (`AppWindow.Closing`) hides it when
  "Close to the notification area" is on; Exit from the menu quits. The first time the close button hides
  the window, a notification says the app is still running. The icon's tooltip shows On, Off or Paused.
- `App.TurnOn` - turning on with the profile and player number, shared by the Dashboard and the menu.
- `Native.ActivateExistingWindow` - a second start also shows a window that is hidden in the tray.
- `ProfileStore` - `MinimizeToTray` and `CloseToTray` (both on by default) and `TrayHintShown`, in CoopWarrior.ini.
- `Pages/SettingsPage` (new) - a "Window" group with the two switches. Footer of the navigation, between
  How to use and About.
- Text that says closing the app turns the controller off: README.md, the How to use page.

## Steps

- [x] Settings in `ProfileStore`
- [x] `TrayIcon`
- [x] `MainWindow`, `App.TurnOn`, `Native`
- [x] Settings page and navigation
- [x] Texts
- [x] Test. Done on this PC: minimize and close to the tray, the tray click (simulated through its message),
      a second start, the menu (opened through its message), close with the setting off. A real click on the icon,
      the first-close notification and an Explorer restart are in the backlog under Not yet verified.

## Testing

- Minimize: the window disappears from the taskbar; the icon's left click brings it back where it was.
- Close: the window hides, the notification shows once; the pad stays on. Exit from the menu removes the pad and quits.
- With both settings off, minimize and close work as before and there is no icon.
- The menu's switch turns the pad on and off; F12 still turns it off. The tooltip follows.
- Starting Co-op Warrior again while it is in the tray shows the window.
- Restarting Explorer brings the icon back.

## Out of scope

- Starting with Windows, and starting minimized.
