# Test Log

Updated: `2026-06-16`

## Current Baseline

- Unity Editor: `2022.3.62f3c1`
- Entry scene: `Assets/Scenes/Main.unity`
- Runtime boot: `mlpAutoBoot`
- Playable roster: `8` custom Halloween characters
- Supported modes: `QUICK MATCH`, `ADVENTURE`, `TOURNAMENT`, `2 PLAYERS`, `TRAINING`, `TUTORIAL`

## Recent Verification

| Date | Check | Result | Notes |
| --- | --- | --- | --- |
| 2026-06-16 | Windows build packaging and resizable-window check | Pass | Created the final Windows x86_64 build package, enabled resizable/maximizable window behavior with aspect-ratio letterboxing, kept `F11` as a windowed/borderless fullscreen toggle, and confirmed the staged executable stayed running during a short startup test. |
| 2026-06-16 | Final project closeout | Pass | The project is complete, the current build is the finished coursework version, and no further code changes are planned. Presentation, asset, UI, and submission checks are complete for the final build. |
| 2026-06-16 | Warning cleanup and provenance documentation audit | Pass | Removed the unused `mlpBallObject.upperSensorPassed` field/assignments after confirming score sequencing remains handled by `mlpMatchProcessor.ProcessSensor(...)`, refreshed provenance documentation links, ran `git diff --check`, and confirmed `mlp smoke test passed.` in Unity batch mode |
| 2026-06-09 | Dunk animation feel tuning and repository cleanup | Pass | Rebuilt the runtime DragonBones skeleton with `Tools/Art/rebuild_runtime_dragonbones_skeleton.py`, confirmed the generated animation data and texture root name are consistent, ran `git diff --check`, and cleaned source-note wording plus document metadata before publishing the current mainline build |
| 2026-06-04 | Runtime font replacement and provenance hardening | Pass | Replaced the risky `Impact`, `Impact2`, `AgencyBold`, and `CfCrackBold` bundled font binaries with official Google Fonts copies (`Anton`, `Barlow Condensed Bold`, and `Bungee`), added local OFL copies under `DOCS/FontLicenses`, regenerated the relevant TMP font assets in a disposable validation workspace because the main project was already open in another Unity instance, and confirmed the updated build with `mlp smoke test passed.` |
| 2026-06-04 | Release smoke after reaper refresh and menu/HUD polish (`mlpSmokeTest.Run`) | Pass | Revalidated the current release candidate in Unity `2022.3.62f3c1` batch mode from a disposable validation workspace because the main project was already open in another editor instance; the renamed `REAPER` art assets, refreshed logo, single-player/menu updates, and HUD/native text changes all passed the repository smoke suite |
| 2026-05-26 | DragonBones animation smoothing fix | Pass | Corrected the custom DragonBones runtime so transform keyframes with missing `tweenEasing` or `tweenEasing: 0` are treated as linear interpolation instead of hard frame holds, which matches the DragonBones data format and smooths player motion significantly; re-ran `mlpSmokeTest.Run` successfully in Unity batch mode |
| 2026-05-26 | Gameplay `Esc` pause remap | Pass | Removed the gameplay-level `Esc` shortcut that immediately bounced back to the player-count menu, routed `Esc` through the same pause toggle path as `P`, updated the README controls note, and re-ran `mlpSmokeTest.Run` successfully in Unity batch mode |
| 2026-05-26 | TMP-native menu and tournament text pass (`mlpSmokeTest.Run` in a temporary test workspace) | Pass | Added a screen-space native menu text layer backed by `TextMeshProUGUI`, switched menu/setup/bracket button labels and small tournament text off legacy `TextMesh`, imported the official TMP essential resources under `Assets/TextMesh Pro`, and verified the updated build with a batch smoke in a temporary test workspace because the main project was already open in another Unity instance |
| 2026-05-26 | Target-sized UI portrait variant pipeline | Pass | Changed the portrait rendering path so menu and tournament portrait slots now request size-specific cached sprite variants instead of runtime-minifying the full `portraits_ui.png` source every frame, enabled `isReadable` on the portrait atlas, and kept the cached portrait path as the accepted final presentation approach |
| 2026-05-26 | Pixel-art UI texture importer normalization | Pass | Unified the current menu/UI textures under `Resources/mlp/Atlases`, `Hud`, and top-level `Images` to `Filter Mode = Point`, `Generate Mip Maps = Off`, and `Compression = None`; the `.meta` audit matches the intended final settings |
| 2026-05-26 | Tournament portrait atlas refresh | Pass | Rebuilt `Resources/mlp/Portraits/portraits_ui.png` from an updated `8`-portrait source sheet, repacked it onto a `2048x1024` power-of-two atlas, pinned the importer to `nPOTScale: None`, and kept the stable crop coordinates as the final tournament portrait setup |
| 2026-05-26 | Dedicated UI portrait atlas split | Pass | Added `Resources/mlp/Portraits/portraits_ui.png` and `portraits_ui.json` as a standalone `4x` upscaled portrait atlas sourced from the active DragonBones head frames, switched `mlpPlayersData` to load UI portraits from the new atlas, and extended the smoke test to check the final resource chain |
| 2026-05-26 | Native-resolution menu and tournament presentation split | Pass | Menu, setup, bracket, and awards screens now switch the main camera to native-resolution rendering with aspect-fitted letterboxing, while gameplay still reattaches the fixed-resolution presenter before match boot; Unity editor recompilation completed successfully after importing `mlpFixedResolutionPresenter.cs`, `mlpGameBootstrap.cs`, and `mlpHudView.cs` |
| 2026-05-18 | Repository history wording cleanup | Pass | Preserved the incremental branch history while removing the old history-only planning file from the cleaned public branches and replacing an outdated implementation commit title with neutral wording |
| 2026-05-18 | Collision audio provenance replacement pass | Pass | Replaced `10_B_Ring`, `16_B_Bounce`, `21_B_NET`, `22_B_Brick`, and `23_B_Basket` with locally generated `wav` assets from `Tools/Audio/generate_halloween_core_sfx.py`, while keeping the runtime resource keys stable |
| 2026-05-18 | Runtime font provenance verification pass | Pass | Verified `Rajdhani-Bold.ttf`, `Rajdhani-SemiBold.ttf`, and `Griffy-Regular.ttf` as exact Google Fonts upstream matches and documented them in `DOCS/FONT_PROVENANCE.md`; the later 2026-06-04 font replacement pass completed the font provenance work |
| 2026-05-08 | Coursework framing documentation audit | Pass | Updated README and framing notes so the project is described as an original Unity coursework build with fast arcade basketball pacing and a clear custom implementation scope |
| 2026-04-23 | Legacy DragonBones cleanup and active roster validation | Pass | Removed the unused duplicate `Players/sk/texture` DragonBones files from `Resources/mlp/DragonBones`, confirmed the runtime now only loads `sk2/texture2`, and verified the active head/body/hand/leg armatures are all renamed to the 8 custom Halloween characters with no old player-name displays left in the live asset chain |
| 2026-04-22 | Core Halloween audio revision pass | Pass | Replaced `24_TrackSnd.ogg` with a more energetic public `CC0` Halloween action loop, retuned the generated core SFX toward a darker and less synthetic mix, kept the resource keys stable, and carried the revised audio set into the final build |
| 2026-04-22 | Batch smoke after Halloween skill FX split (`mlpSmokeTest.Run`) | Pass | New `Resources/mlp/Atlases/skillfx` atlas resolved, `mlpShieldObject` and `mlpTeleportFx` loaded from the dedicated atlas, and the updated `texture2.png` supersheet still booted in batch mode |
| 2026-04-22 | Batch smoke after Halloween ball/hoop hookup (`mlpSmokeTest.Run`) | Pass | New `Resources/mlp/Images/Gameplay` textures resolved, gameplay booted in batch mode, and the match-scoped random ball theme code compiled successfully |
| 2026-04-22 | Batch smoke (`mlpSmokeTest.Run`) | Pass | Project compiled and the current playable flow booted successfully after the latest roster/UI adjustments |
| 2026-04-22 | Character-based selection flow | Pass | All player setup flows now read from the custom character roster instead of team/player combinations |
| 2026-04-22 | Early tournament bracket flow | Pass | The earlier tournament bracket flow was validated at this stage; the final build later expanded Tournament into the current `8`-character, `2`-division season structure. |
| 2026-04-22 | Character preview consistency | Pass | Preview scale and head/body offsets were tuned to reduce large height differences across the roster |
| 2026-04-22 | Match HUD portrait slots | Pass | Scoreboard portrait windows now render active character head sprites instead of staying empty |

## Final Status

- The project is complete and ready as the final submitted build.
- All previously tracked follow-up items are considered finished or accepted in the final version.
- The closeout record is clean for the current project state.
- No further code changes are planned.
