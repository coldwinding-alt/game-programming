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

## Outstanding Manual Checks

- Reopen `Assets/Scenes/Main.unity` after Stage 1 preparation and confirm the host objects are visible in `Scene view`.
- Capture the runtime-default golden screenshots listed in `DOCS/GoldenBaseline/SCREEN_CAPTURE_CHECKLIST.md`.
- Confirm `Quick Match`, `Training`, `2 Players`, and `Tournament` still match the current runtime baseline before Stage 2 begins.
