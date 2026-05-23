# Golden Baseline

This package is the parity reference for the scene/prefab migration.

## Source of truth

- Primary runtime reference: the current repository with runtime-default bootstrap enabled
- Frozen backup reference: `../Rim_Rush_Championship_Season_RUNTIME_ORIGINAL_BACKUP`
- Main scene authority: `Assets/Scenes/Main.unity`

## Required artifacts

- `HOST_SCENE_HIERARCHY.md`: stage 1 host structure and component reference
- `KEY_LAYOUT_REFERENCE.md`: anchor positions, sizes, and scales for parity checks
- `SCREEN_CAPTURE_CHECKLIST.md`: required screenshots to capture before each default cutover
- `BEHAVIOR_CHECKLIST.md`: required manual parity checks

## Screenshot storage

Store captured reference PNGs in `DOCS/GoldenBaseline/screens/` with the exact filenames listed in `SCREEN_CAPTURE_CHECKLIST.md`.

## Usage

1. Keep the runtime-default path enabled while capturing the baseline.
2. Update screenshots only when the approved golden baseline changes.
3. Before each migration stage is switched on by default, compare the new path against this package.
