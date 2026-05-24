# Scene/Prefab Migration Tracker

Updated: `2026-05-24`

## Goal

Move the project from runtime-created presentation objects to `scene/prefab-owned` views with preattached scripts, while keeping gameplay, flow, and rendering parity with the current playable baseline.

## Guardrails

- The playable default path stays on the original runtime-created menu and gameplay flow until a migrated subsystem passes parity checks.
- Stage tools may generate temporary scaffolds for analysis, but final shipping views must be authored as native Unity scene objects or prefabs.
- Only one migration issue may be actively implemented at a time.
- Any behavior regression discovered during migration is tracked separately instead of being folded into the migration card.

## Stage Map

| GitHub Issue | Stage | Scope | Default Path | Status | Evidence |
| --- | --- | --- | --- | --- | --- |
| `#22` | Stage 1 | Scene host objects only: `rimrushGameBootstrap`, camera, presenter, audio, persistent roots | Runtime menu/gameplay | Testing | `rimrush/Stage 1/Prepare Main Scene Hosts`, `rimrushSmokeTest.Run` |
| `#23` | Stage 2 | Native menu shell and HUD shell views | Runtime menu/gameplay | Testing | `rimrush/Stage 2/Prepare Menu and HUD Scene Views`, `rimrushSmokeTest.Run` |
| `#24` | Stage 3 | Native court, basket, and ball views | Runtime gameplay | Testing | `rimrush/Stage 3/Prepare Gameplay Scene Views`, `rimrushSmokeTest.Run` |
| `#25` | Stage 4 | Native player view, animation mounts, energy bars, and gameplay FX anchors | Runtime gameplay | Testing | `rimrush/Stage 4/Prepare Player Scene Views`, `rimrushSmokeTest.Run` |
| `#26` | Stage 5 | View/logic responsibility split for player, ball, and basket | Scene/prefab-driven | Backlog | Architecture verification |
| `#27` | Stage 6 | Remove obsolete runtime creation branches | Scene/prefab-driven | Backlog | Final loop regression |
| `#28` | Final Regression | Cross-mode regression and architecture handoff | Stage-dependent | Backlog | Full manual and smoke coverage |

## Current Baseline

- `Main.unity` should expose the persistent host objects directly in `Scene view`.
- The runtime-created menu and gameplay path remains the default until Stage 2 and Stage 3 are individually approved.
- `Main.unity` now also authors player containers, energy bars, teleport FX, and shield FX inside `GameplayRoot`, while runtime gameplay still remains the default path.
- The golden parity reference lives in `DOCS/GoldenBaseline/`.
- The original runtime implementation is frozen in `../Rim_Rush_Championship_Season_RUNTIME_ORIGINAL_BACKUP`.

## Stage 0 Deliverables

- `DOCS/GoldenBaseline/README.md`
- `DOCS/GoldenBaseline/HOST_SCENE_HIERARCHY.md`
- `DOCS/GoldenBaseline/KEY_LAYOUT_REFERENCE.md`
- `DOCS/GoldenBaseline/SCREEN_CAPTURE_CHECKLIST.md`
- `DOCS/GoldenBaseline/BEHAVIOR_CHECKLIST.md`

## Stage 1 Deliverables

- `Main.unity` contains only the always-on host objects needed before gameplay or menu visuals are instantiated.
- `rimrushSceneBindings` serializes the camera, presenter, audio, `PersistentRoot`, and `OverlayRoot`.
- `rimrushGameBootstrap` keeps `preferSceneMenuShell` and `preferSceneGameplayBindings` off by default.
- `rimrushAutoBoot` remains a fallback only.

## Stage 2 Deliverables

- `Main.unity` contains authored `MenuShell` and `HudSceneRoot` objects under `OverlayRoot`.
- `MenuShell` serializes a `PageCatalog` with the authored menu page roots:
  - `Page_PlayerCount`
  - `Page_MatchType`
  - `Page_QuickSetup`
  - `Page_TrainingSetup`
  - `Page_TwoPlayerSetup`
  - `Page_TournamentSetup`
  - `Page_TournamentBracket`
  - `Page_TournamentAwards`
- `HudSceneRoot` serializes the authored HUD shell bindings and preview helper.
- `rimrushGameBootstrap` keeps `preferSceneMenuShell`, `preferSceneHudView`, and `preferSceneGameplayBindings` off by default until manual parity is approved.

## Stage 3 Deliverables

- `Main.unity` contains an authored `GameplayRoot` under `PersistentRoot`.
- `GameplayRoot` serializes `rimrushGameplayBindings` with:
  - `ArenaObject`
  - `BasketLeft`
  - `BasketRight`
  - `BallObject`
  - `LeftNeutralSpawn`
  - `RightNeutralSpawn`
  - `LeftServeSpawn`
  - `RightServeSpawn`
- `rimrushSceneAuthoringMode` defaults edit-time authoring focus to `Gameplay`, while keeping the runtime gameplay cutover disabled.
- `rimrushGameplayBindings` references the shared Stage 2 `HudSceneRoot` instead of creating a duplicate HUD.
- `rimrushGameBootstrap` still keeps `preferSceneGameplayBindings` off by default until manual gameplay parity is approved.

## Stage 4 Deliverables

- `Main.unity` extends the authored `GameplayRoot` with:
  - `LeftPlayerView`
  - `RightPlayerView`
  - `EnergyBarSlot0`
  - `EnergyBarSlot1`
  - `EnergyBarSlot2`
  - `LeftTeleportFxView`
  - `RightTeleportFxView`
  - `LeftShieldView`
  - `RightShieldView`
- `rimrushGameplayBindings` serializes authored references for:
  - left and right player view containers
  - controller energy bar scene views
  - teleport FX scene views
  - shield FX scene views
- `Main.unity` should open in `Gameplay` authoring focus with the full authored court, baskets, ball, player containers, energy bars, and gameplay FX visible in `Scene view`.
- `rimrushGameBootstrap` still keeps `preferSceneGameplayBindings` off by default until manual gameplay parity is approved with the authored player path included.

## Verification Workflow

1. Compile the project.
2. Run `rimrush.EditorTools.rimrushSceneMigrationTools.PrepareMainSceneHosts`.
3. Run `rimrush.EditorTools.rimrushBaselineCaptureTools.ExportStage0BaselinePackage`.
4. Run `rimrush.EditorTools.rimrushSceneMigrationTools.PrepareMenuAndHudSceneViews`.
5. Run `rimrush.EditorTools.rimrushSceneMigrationTools.PrepareGameplaySceneViews`.
6. Run `rimrush.EditorTools.rimrushSceneMigrationTools.PreparePlayerSceneViews`.
7. Run `rimrush.EditorTools.rimrushSmokeTest.Run`.
8. Record the result in `TEST_LOG.md` and `DOCS/TEST_LOG.md`.

## Next Cutover

The next authored implementation target is Stage 5, but the default-path cutover remains blocked until the Stage 2, Stage 3, and Stage 4 scene-owned views are manually compared against the golden baseline in Play Mode and approved for parity.
