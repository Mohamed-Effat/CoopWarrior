# 0003. Nudge the pad before the first neutral report

- Date: 2026-10-13
- Status: Accepted

## Context

Right after `vigem_target_add`, XInput reported the new pad's sticks off-center
(LX -3356, LY -1869, RX -3255, RY -848, the same on every run) until the first key press.
Sending a neutral report at startup did not help, and neither did re-sending it every 500 ms.
A report that differed from all-zero did fix it.

## Decision

Before the loop, send a report with `LeftX = 1`, then a neutral report.

## Alternatives considered

- Re-sending the neutral report periodically: tested, had no effect. ViGEmBus appears to drop a report
  equal to the all-zero state it starts with.

## Consequences

The 1-unit tilt lasts one report and is far below any game's dead zone.
