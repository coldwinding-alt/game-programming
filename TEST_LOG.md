# Test Log

Updated: `2026-04-22`

## Current Baseline

- Unity Editor: `2022.3.62f3c1`
- Entry scene: `Assets/Scenes/Main.unity`
- Runtime boot: `BL2020AutoBoot`
- Playable roster: `8` custom Halloween characters
- Supported modes: `QUICK MATCH`, `2 PLAYERS`, `TRAINING`, `TOURNAMENT`

## Recent Verification

| Date | Check | Result | Notes |
| --- | --- | --- | --- |
| 2026-04-22 | Batch smoke after Halloween skill FX split (`BL2020SmokeTest.Run`) | Pass | New `Resources/BL2020/Atlases/skillfx` atlas resolved, `BLShieldObject` and `BLTeleportFx` loaded from the dedicated atlas, and the updated `texture2.png` supersheet still booted in batch mode |
| 2026-04-22 | Batch smoke after Halloween ball/hoop hookup (`BL2020SmokeTest.Run`) | Pass | New `Resources/BL2020/Images/Gameplay` textures resolved, gameplay booted in batch mode, and the match-scoped random ball theme code compiled successfully |
| 2026-04-22 | Batch smoke (`BL2020SmokeTest.Run`) | Pass | Project compiled and the current playable flow booted successfully after the latest roster/UI adjustments |
| 2026-04-22 | Character-based selection flow | Pass | All player setup flows now read from the custom character roster instead of team/player combinations |
| 2026-04-22 | Tournament bracket flow | Pass | Tournament remains a `4`-character single-elimination structure in the current build |
| 2026-04-22 | Character preview consistency | Pass | Preview scale and head/body offsets were tuned to reduce large height differences across the roster |
| 2026-04-22 | Match HUD portrait slots | Pass | Scoreboard portrait windows now render active character head sprites instead of staying empty |

## Current Follow-Up Items

- Do a full in-editor visual pass on the `8` playable characters and tighten any remaining head/body overlap issues.
- Continue polishing menu spacing and tournament presentation so the overall UI reads as one cohesive game.
- Run a quick in-editor multi-match visual pass to confirm all `3` Halloween ball themes appear and that the front rim overlay still covers the ball cleanly on both baskets.
- Run one longer manual playtest loop before final submission packaging.

## Known Non-Blocking Issue

- Existing warning: `Assets/BasketballLegends2020/Scripts/GameObjects/BLGameplayObjects.cs(176,22)` - `BLBallObject.upperSensorPassed` is assigned but currently unused.
