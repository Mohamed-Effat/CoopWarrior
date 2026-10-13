# 0004. Game configs as .kpad text files

- Date: 2026-10-13
- Status: Accepted

## Context

Each game needs its own keys, mouse settings and game filter. The user wants to import and export them,
and to ask Claude to write a config for a new game.

## Decision

One plain-text file per game, `Setting = value` per line, `#` comments, repeated `Note =` lines for tips.
Key names are the WinForms `Keys` names version 1 used (`ShiftKey`, `Return`, `LButton`), plus aliases
such as `Shift`, `Enter`, `Alt`. Unknown lines are skipped and missing settings keep their defaults.
Files live in `configs\` next to `Start.bat`. The extension `.kpad` is kept from the "Keyboard Pad" name
so existing files keep working.

## Alternatives considered

- JSON: stricter and harder to write by hand; the version 1 `keymap.txt` format was already line-based.
- One settings file with all games inside: harder to share one game's setup.

## Consequences

- Setting and key names are a contract with files on disk. Add new names; do not rename old ones.
- Version 1 `keymap.txt` files are a subset of the format and import directly.
- Comments in a file are not preserved when the app saves it.
