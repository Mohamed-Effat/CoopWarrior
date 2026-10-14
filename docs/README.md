# Docs

| Path | What goes there |
| --- | --- |
| [architecture.md](architecture.md) | How the app works: threads, data flow, files on disk. |
| [testing.md](testing.md) | How to test against the real virtual pad, and what to be careful about. |
| [releasing.md](releasing.md) | How every release is made: the Release workflow on GitHub, step by step. |
| [release-notes/](release-notes/) | The notes players read on each release page, one file per version. |
| [decisions/](decisions/) | One file per design decision: the context, the choice, what was rejected. |
| [plans/](plans/) | Plans for features and larger changes, written before the work. Also the backlog. |
| [game-profiles/](game-profiles/) | How to research a game and write its `.kpad` profile, and notes per game. |
| [images/](images/) | The logo, animations and screenshots used in the main README. [images/README.md](images/README.md) describes each one in words. |

## Naming

- Decisions: `NNNN-short-title.md`, numbered in order. A decision is never edited to say something
  else; a later decision that replaces it says so and links back.
- Plans: `YYYY-MM-DD-short-title.md`. When the work is done, set the status at the top to Done
  and add what changed from the plan.
- Game notes: one file per game, named after the game, for example `dynasty-warriors-3.md`.

Each folder has a `_template.md` where a template helps.
