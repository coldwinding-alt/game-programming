# Side-by-Side Parity Checklist (Match Core)

Updated: 2026-04-18

Validation method:

1. Run the same scene context on H5 reference and Unity build.
2. Use the same input script/window where possible.
3. Record result by three dimensions:
   - behavior
   - timing window
   - trigger condition

## Core Cases

| Case | Reference result | Unity result | Deviation | Next action |
| --- | --- | --- | --- | --- |
| Dash first available window after countdown | `UseDelay(DASH_DELAY)` warmup happens before active play window | Added pre-match delay warmup tick (`TickPreMatch`) so first legal dash no longer waits extra 1s after whistle | Needs live AB verification | Capture two synchronized recordings and compare first successful dash frame. |
| Steal action window (`steal -> action`) | Steal resolves on frame event, not overlap instant | Frame-event trigger wired through DBLite `FRAME_EVENT` + fallback | Small risk remains on fallback path | Keep frame-event path as default and reduce fallback dependence in next pass. |
| Ground throw (`throw_land -> throw`) | Ball release on throw frame event | Frame-event trigger wired and fallback retained | Mostly aligned | Verify with 10 repeated throw tests at 30fps capture. |
| Dunk release + score | Dunk release on `dunk` event, score through sensor chain | Event-driven release + `MatchProcessor` sensor/throw context | Still sensitive to contact substep edge cases | Continue rim/contact parity pass and log remaining false negatives. |
| Defence steal cadence (Normal) | Delay-driven steals with spacing and probability | `strategyDefence` now delay-gated with one-sample steal action decision | Better than overlap-steal behavior, still tunable | Compare steal attempt frequency over fixed 60s scenarios. |
| Easy difficulty defensive behavior | No active steal pressure, mostly spacing + contest | Easy branch disables active steal trigger in AI | aligned with coursework display target | Keep as constrained subset of Normal. |
| Pump fake defensive reaction | Defender can bite/pause with probability | Pump signal path and defence reaction branch migrated | Timing/probability still rough | Tune chance and delay table against reference profile. |
| Upper/down sensor score ordering | Must pass upper before down to score | `BLMatchProcessor.ProcessSensor` chain active | aligned in logic | Continue physics contact-order stabilization. |

## Regression Guard Cases

| Guard | Pass criteria |
| --- | --- |
| Visual dunk made but no score | Must not reproduce in normal play loops. |
| Visual shot swish but no score | Must not reproduce when upper/down order is valid. |
| First dash ignored | First valid double tap after countdown should trigger dash. |
| Opponent steals on body overlap without window | Must not happen in Normal strategy. |
| Empty-hand stun missing | Forward-lane steal on grounded target should still apply stun. |
