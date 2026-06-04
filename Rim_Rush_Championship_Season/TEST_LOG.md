# Test Log

Updated: `2026-06-04`

## Current Baseline

- Unity Editor: `2022.3.62f3c1`
- Entry scene: `Assets/Scenes/Main.unity`
- Runtime boot: `rimrushAutoBoot`
- Playable roster: `8` custom Halloween characters
- Supported modes: `QUICK MATCH`, `2 PLAYERS`, `TRAINING`, `TOURNAMENT`

## Recent Verification

| Date | Check | Result | Notes |
| --- | --- | --- | --- |
| 2026-06-04 | Runtime font replacement and provenance hardening | Pass | Replaced the risky `Impact`, `Impact2`, `AgencyBold`, and `CfCrackBold` bundled font binaries with official Google Fonts copies (`Anton`, `Barlow Condensed Bold`, and `Bungee`), added local OFL copies under `DOCS/FontLicenses`, regenerated the relevant TMP font assets in a disposable validation workspace because the main project was already open in another Unity instance, and confirmed the updated build with `rimrush smoke test passed.` |
| 2026-06-04 | Release smoke after reaper refresh and menu/HUD polish (`rimrushSmokeTest.Run`) | Pass | Revalidated the current release candidate in Unity `2022.3.62f3c1` batch mode from a disposable validation workspace because the main project was already open in another editor instance; the renamed `REAPER` art assets, refreshed logo, single-player/menu updates, and HUD/native text changes all passed the repository smoke suite |
| 2026-05-26 | DragonBones animation smoothing fix | Pass | Corrected the custom DragonBones runtime so transform keyframes with missing `tweenEasing` or `tweenEasing: 0` are treated as linear interpolation instead of hard frame holds, which matches the DragonBones data format and smooths player motion significantly; re-ran `rimrushSmokeTest.Run` successfully in Unity batch mode |
| 2026-05-26 | Gameplay `Esc` pause remap | Pass | Removed the gameplay-level `Esc` shortcut that immediately bounced back to the player-count menu, routed `Esc` through the same pause toggle path as `P`, updated the README controls note, and re-ran `rimrushSmokeTest.Run` successfully in Unity batch mode |
| 2026-05-26 | TMP-native menu and tournament text pass (`rimrushSmokeTest.Run` in a temporary test workspace) | Pass | Added a screen-space native menu text layer backed by `TextMeshProUGUI`, switched menu/setup/bracket button labels and small tournament text off legacy `TextMesh`, imported the official TMP essential resources under `Assets/TextMesh Pro`, and verified the updated build with a batch smoke in a temporary test workspace because the main project was already open in another Unity instance |
| 2026-05-26 | Target-sized UI portrait variant pipeline | Partial pass | Changed the portrait rendering path so menu and tournament portrait slots now request size-specific cached sprite variants instead of runtime-minifying the full `portraits_ui.png` source every frame, and enabled `isReadable` on the portrait atlas so those small variants can be downsampled once with alpha-aware filtering; the open Unity editor completed script recompilation successfully, while the final in-editor visual pass still needs confirmation from the current session |
| 2026-05-26 | Pixel-art UI texture importer normalization | Partial pass | Unified the current menu/UI textures under `Resources/rimrush/Atlases`, `Hud`, and top-level `Images` to `Filter Mode = Point`, `Generate Mip Maps = Off`, and `Compression = None`, and also pinned their `nPOTScale` to `None` to avoid hidden resampling on non-power-of-two assets; the `.meta` audit matches the intended settings, while the final in-editor visual pass still needs confirmation from the open Unity session |
| 2026-05-26 | AI-regenerated tournament portrait atlas refresh | Partial pass | Rebuilt `Resources/rimrush/Portraits/portraits_ui.png` from an AI-redrawn `8`-portrait source sheet, repacked it onto a `2048x1024` power-of-two atlas, and pinned the importer to `nPOTScale: None` so the JSON crop coordinates remain stable; offline bbox comparison against the previous template stayed aligned, while a final in-editor visual pass is still needed in the open Unity session |
| 2026-05-26 | Dedicated UI portrait atlas split | Partial pass | Added `Resources/rimrush/Portraits/portraits_ui.png` and `portraits_ui.json` as a standalone `4x` upscaled portrait atlas sourced from the active DragonBones head frames, switched `rimrushPlayersData` to load UI portraits from the new atlas, and extended the smoke test to check the new resource chain; script compilation completed successfully, while the final in-editor visual pass still needs confirmation from the open Unity session |
| 2026-05-26 | Native-resolution menu and tournament presentation split | Pass | Menu, setup, bracket, and awards screens now switch the main camera to native-resolution rendering with aspect-fitted letterboxing, while gameplay still reattaches the fixed-resolution presenter before match boot; Unity editor recompilation completed successfully after importing `rimrushFixedResolutionPresenter.cs`, `rimrushGameBootstrap.cs`, and `rimrushHudView.cs` |
| 2026-05-18 | Repository history wording cleanup | Pass | Preserved the incremental branch history while removing the old history-only planning file from the cleaned public branches and replacing an outdated implementation commit title with neutral wording |
| 2026-05-18 | Collision audio provenance replacement pass | Pass | Replaced `10_B_Ring`, `16_B_Bounce`, `21_B_NET`, `22_B_Brick`, and `23_B_Basket` with locally generated `wav` assets from `Tools/Audio/generate_halloween_core_sfx.py`, while keeping the runtime resource keys stable |
| 2026-05-18 | Runtime font provenance verification pass | Partial pass | Verified `Rajdhani-Bold.ttf`, `Rajdhani-SemiBold.ttf`, and `Griffy-Regular.ttf` as exact Google Fonts upstream matches and documented them in `DOCS/FONT_PROVENANCE.md`; `Impact.ttf`, `Impact2.ttf`, `AgencyBold.ttf`, and `CfCrackBold.ttf` still need replacement or source records |
| 2026-05-08 | Coursework framing documentation audit | Pass | Updated README and framing notes so the project is described as an original Unity coursework build with fast arcade basketball pacing and a clear custom implementation scope |
| 2026-04-23 | Legacy DragonBones cleanup and active roster validation | Pass | Removed the unused duplicate `Players/sk/texture` DragonBones files from `Resources/rimrush/DragonBones`, confirmed the runtime now only loads `sk2/texture2`, and verified the active head/body/hand/leg armatures are all renamed to the 8 custom Halloween characters with no old player-name displays left in the live asset chain |
| 2026-04-22 | Core Halloween audio revision pass | Partial pass | Replaced `24_TrackSnd.ogg` with a more energetic public `CC0` Halloween action loop, retuned the generated core SFX toward a darker and less synthetic mix, and kept the resource keys stable; the only remaining smoke blocker at that point was the existing `ball_halloween_*` `32x32` validation issue, not the revised audio set |
| 2026-04-22 | Batch smoke after Halloween skill FX split (`rimrushSmokeTest.Run`) | Pass | New `Resources/rimrush/Atlases/skillfx` atlas resolved, `rimrushShieldObject` and `rimrushTeleportFx` loaded from the dedicated atlas, and the updated `texture2.png` supersheet still booted in batch mode |
| 2026-04-22 | Batch smoke after Halloween ball/hoop hookup (`rimrushSmokeTest.Run`) | Pass | New `Resources/rimrush/Images/Gameplay` textures resolved, gameplay booted in batch mode, and the match-scoped random ball theme code compiled successfully |
| 2026-04-22 | Batch smoke (`rimrushSmokeTest.Run`) | Pass | Project compiled and the current playable flow booted successfully after the latest roster/UI adjustments |
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

- Existing warning: `Assets/rimrush/Scripts/GameObjects/rimrushGameplayObjects.cs(176,22)` - `rimrushBallObject.upperSensorPassed` is assigned but currently unused.
