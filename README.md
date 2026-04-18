# Basketball Legends 2020 Unity Port

Unity 2022 coursework project focused on a faithful vertical-slice port of the 2020 browser game into an editor-friendly Unity project. The current deliverable keeps the migrated resource structure, data flow, and match feel close to the original reference package instead of rebuilding a loosely similar game from scratch.

## Current Playable Scope

- Main menu with `1 PLAYER`, `2 PLAYERS`, `QUICK MATCH`, `TRAINING`, and `AI: EASY / AI: NORMAL`.
- Direct boot into a playable match loop from the Unity editor.
- Player movement, jump, dash, shoot, steal, stun, loose-ball pickup, score reset, timer, overtime, and post-match overlay.
- Migrated atlases, logo, DragonBones data subset, and sound effects placed under Unity `Resources`.
- Hoop interaction rebuilt around rim contact, backboard contact, and upper/down score sensors.
- Local Windows build entry point included for continued testing outside the editor.

## Unity Version

- Unity Editor: `2022.3.62f3c1`

## How To Open And Run

1. Open this folder in Unity Hub with Unity `2022.3.62f3c1`.
2. Open `Assets/Scenes/Main.unity`.
3. Press Play.
4. The project uses `BL2020AutoBoot`, so the menu flow starts directly from the editor.

## Controls

- Player 1:
  - Move: `A` / `D`
  - Jump: `W`
  - Guard / fake: `S`
  - Action / shoot / steal: `B`
  - Super: `V`
- Player 2:
  - Move: `Left Arrow` / `Right Arrow`
  - Jump: `Up Arrow`
  - Guard / fake: `Down Arrow`
  - Action / shoot / steal: `L`
  - Super: `K`
- Match flow:
  - Dash: double-tap left or right
  - Return to menu: `Esc`

## Repository Guide

- `DOCS/SPRINT_PLAN.md` - sprint goals, delivered work, and remaining risks
- `TEST_LOG.md` - editor checks, playtest checks, and current follow-up items
- `ASSET_CREDITS.md` - source notes for migrated resource files
- `PROJECT_CONTEXT.md` - ongoing handoff summary for the current Unity port
- `Assets/BasketballLegends2020/Documentation/` - migration plan and migration status notes inside the Unity project

## Current Limitations

- Physics is still an approximation of the original browser-game physics model.
- DragonBones runtime support currently covers the gameplay subset needed for the current build, not the full original animation feature set.
- Some original screen flows are still simplified or unported.
- Block, pump fake, dunk, and fuller post-match behavior still need more parity work.
- Current reference assets are temporary for coursework delivery and are planned for later replacement/cleanup.

## Notes

- `Builds/`, `Library/`, `Logs/`, and other generated Unity folders are intentionally excluded from version control.
- The authoritative editor entry scene is `Assets/Scenes/Main.unity`.
