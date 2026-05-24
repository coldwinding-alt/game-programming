# Scene/Prefab Migration Test Log

Updated: `2026-05-24`

## 2026-05-23

### Runtime-default stabilization

- Scope: restore the original runtime-created menu and gameplay path as the default execution path
- Unity: `2022.3.62f3c1`
- Result: pass
- Notes:
  - `rimrushGameBootstrap` keeps `preferSceneMenuShell` and `preferSceneGameplayBindings` disabled by default
  - scene-owned bindings remain available for staged migration work, but no longer take over the menu or gameplay path by default

### Validation clone smoke

- Scope: verify the stabilized runtime-default build in an isolated clone while the main project may still be open in the editor
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSmokeTest.Run`
- Result: pass
- Notes:
  - log ended with `rimrush smoke test passed.`
  - warning noise from edit-mode `Destroy` calls inside DragonBones was observed, but it did not fail the smoke run

### Stage 0/1 tooling alignment

- Scope: align migration tooling, baseline docs, and smoke coverage with the host-only Stage 0/1 plan
- Unity: `2022.3.62f3c1`
- Result: pass
- Notes:
  - added `rimrush/Stage 1/Prepare Main Scene Hosts`
  - added `rimrush/Baseline/Export Stage 0 Baseline Package`
  - updated smoke validation to require host objects and golden baseline artifacts instead of prototype `MenuShell` / `GameplayRoot` objects
  - validated `Main.unity` now contains only the Stage 1 host objects: `Main Camera`, `rimrushBootstrap`, `PersistentRoot`, `OverlayRoot`, and `rimrushAudio`
  - validated the exported `DOCS/GoldenBaseline/` package and a passing `rimrushSmokeTest.Run` in an isolated validation clone

## 2026-05-24

### Stage 2 menu/HUD authoring build

- Scope: author native `MenuShell` and `HudSceneRoot` objects directly into `Assets/Scenes/Main.unity` while keeping the runtime-created menu/gameplay path as the default
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSceneMigrationTools.PrepareMenuAndHudSceneViews`
- Result: pass
- Notes:
  - captured the current runtime menu pages into a scene-owned `PageCatalog`
  - added authored `Page_PlayerCount`, `Page_MatchType`, `Page_QuickSetup`, `Page_TrainingSetup`, `Page_TwoPlayerSetup`, `Page_TournamentSetup`, `Page_TournamentBracket`, and `Page_TournamentAwards`
  - added `rimrushMenuAuthoringPreview` and `rimrushHudAuthoringPreview`
  - kept `preferSceneMenuShell`, `preferSceneHudView`, and `preferSceneGameplayBindings` disabled by default

### Stage 2 validation clone smoke

- Scope: confirm the authored Stage 2 scene survives a fresh project reload and still passes the existing automated smoke coverage
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSmokeTest.Run`
- Result: pass
- Notes:
  - log ended with `rimrush smoke test passed.`
  - updated smoke validation now checks the authored menu page catalog, HUD bindings, preview components, and the disabled default cutover flags

### Stage 3 gameplay scene authoring build

- Scope: author native `GameplayRoot`, `ArenaObject`, `BasketLeft`, `BasketRight`, `BallObject`, and four gameplay spawn anchors directly into `Assets/Scenes/Main.unity` while keeping the runtime-created gameplay path as the default
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSceneMigrationTools.PrepareGameplaySceneViews`
- Result: pass
- Notes:
  - added `rimrushSceneAuthoringMode` so edit-time focus defaults to gameplay authoring instead of menu preview
  - added authored gameplay bindings that reference the shared Stage 2 `HudSceneRoot`
  - moved scene-owned gameplay view components into dedicated script files so Unity can serialize the authored basket, ball, arena, and gameplay binding components reliably
  - kept `preferSceneGameplayBindings` disabled by default

### Stage 3 validation clone smoke

- Scope: confirm the authored Stage 3 gameplay scene survives a fresh project reload, keeps runtime gameplay as the default, and passes the updated automated smoke coverage
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSceneMigrationTools.PrepareGameplaySceneViewsAndRunSmoke`
- Result: pass
- Notes:
  - log ended with `rimrush smoke test passed.`
  - follow-up `DumpGameplaySceneBindings` confirmed `ArenaObject`, `BasketLeft`, `BasketRight`, `BallObject`, and all Stage 3 bindings deserialize correctly after a fresh Unity reload
  - gameplay authoring remains edit-time only; runtime play still uses the original runtime-created gameplay path until manual parity is approved

### Stage 4 player/FX scene authoring build

- Scope: extend authored `GameplayRoot` with native player containers, animation mounts, controller energy bars, teleport FX, and shield FX while keeping runtime gameplay as the default
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSceneMigrationTools.PreparePlayerSceneViews`
- Result: pass
- Notes:
  - authored `LeftPlayerView`, `RightPlayerView`, `EnergyBarSlot0`, `EnergyBarSlot1`, `EnergyBarSlot2`, `LeftTeleportFxView`, `RightTeleportFxView`, `LeftShieldView`, and `RightShieldView` directly into `Main.unity`
  - kept `preferSceneGameplayBindings` disabled by default
  - retained the shared Stage 2 `HudSceneRoot` instead of creating a duplicate HUD under `GameplayRoot`

### Stage 4 validation smoke

- Scope: confirm the authored Stage 4 gameplay scene survives a fresh project reload, exposes the full player/FX bindings, and still keeps runtime gameplay as the default path
- Unity: `2022.3.62f3c1`
- Command: `rimrush.EditorTools.rimrushSceneMigrationTools.PreparePlayerSceneViewsAndRunSmoke`
- Result: pass
- Notes:
  - log ended with `rimrush smoke test passed.`
  - follow-up `DumpGameplaySceneBindings` confirmed the authored player, energy bar, teleport, and shield view bindings deserialize correctly after a fresh Unity reload
  - gameplay authoring remains edit-time only; runtime play still uses the original runtime-created gameplay path until manual parity is approved

## Outstanding Manual Checks

- Reopen `Assets/Scenes/Main.unity` after Stage 1 preparation and confirm the host objects are visible in `Scene view`.
- Capture the runtime-default golden screenshots listed in `DOCS/GoldenBaseline/SCREEN_CAPTURE_CHECKLIST.md`.
- Confirm `Quick Match`, `Training`, `2 Players`, and `Tournament` still match the current runtime baseline before Stage 2 begins.
- Reopen `Assets/Scenes/Main.unity`, leave `rimrushSceneAuthoringMode` on `Gameplay`, and confirm the authored court, baskets, ball, and four spawn anchors are visible and draggable in `Scene view`.
- Reopen `Assets/Scenes/Main.unity`, leave `rimrushSceneAuthoringMode` on `Gameplay`, and confirm the authored player containers, energy bars, teleport FX, and shield FX are visible and draggable in `Scene view`.
- Run the Stage 3 gameplay parity checklist in Play Mode before enabling `preferSceneGameplayBindings`.
- Run the Stage 4 player visual and gameplay-FX parity checklist in Play Mode before enabling `preferSceneGameplayBindings`.
