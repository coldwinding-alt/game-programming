# Test Log

Updated: `2026-05-18`

## Current Baseline

- Unity Editor: `2022.3.62f3c1`
- Entry scene: `Assets/Scenes/Main.unity`
- Runtime boot: `BL2020AutoBoot`
- Playable roster: `8` custom Halloween characters
- Supported modes: `QUICK MATCH`, `2 PLAYERS`, `TRAINING`, `TOURNAMENT`

## Recent Verification

| Date | Check | Result | Notes |
| --- | --- | --- | --- |
| 2026-05-18 | Repository history wording cleanup | Pass | Preserved the incremental branch history while removing the old history-only planning file from the cleaned public branches and replacing an outdated implementation commit title with neutral wording |
| 2026-05-18 | Collision audio provenance replacement pass | Pass | Replaced `10_B_Ring`, `16_B_Bounce`, `21_B_NET`, `22_B_Brick`, and `23_B_Basket` with locally generated `wav` assets from `Tools/Audio/generate_halloween_core_sfx.py`, while keeping the runtime resource keys stable |
| 2026-05-18 | Runtime font provenance verification pass | Partial pass | Verified `Rajdhani-Bold.ttf`, `Rajdhani-SemiBold.ttf`, and `Griffy-Regular.ttf` as exact Google Fonts upstream matches and documented them in `DOCS/FONT_PROVENANCE.md`; `Impact.ttf`, `Impact2.ttf`, `AgencyBold.ttf`, and `CfCrackBold.ttf` still need replacement or source records |
| 2026-05-08 | Coursework framing documentation audit | Pass | Updated README, sprint plan, and asset-credit wording so the project is described as gameplay-inspired by the H5 web game `热血篮球赛`, not as a line-by-line rewrite or source-code port |
| 2026-04-23 | Legacy DragonBones cleanup and active roster validation | Pass | Removed the unused duplicate `Players/sk/texture` DragonBones files from `Resources/BL2020/DragonBones`, confirmed the runtime now only loads `sk2/texture2`, and verified the active head/body/hand/leg armatures are all renamed to the 8 custom Halloween characters with no old player-name displays left in the live asset chain |
| 2026-04-22 | Core Halloween audio revision pass | Partial pass | Replaced `24_TrackSnd.ogg` with a more energetic public `CC0` Halloween action loop, retuned the generated core SFX toward a darker and less synthetic mix, and kept the resource keys stable; the only remaining smoke blocker at that point was the existing `ball_halloween_*` `32x32` validation issue, not the revised audio set |
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
- Run a quick in-editor multi-match visual pass to confirm all configured Halloween ball themes appear cleanly during gameplay.
- Run one longer manual playtest loop before final submission packaging.

## Known Non-Blocking Issue

- Existing warning: `Assets/BasketballLegends2020/Scripts/GameObjects/BLGameplayObjects.cs(176,22)` - `BLBallObject.upperSensorPassed` is assigned but currently unused.
