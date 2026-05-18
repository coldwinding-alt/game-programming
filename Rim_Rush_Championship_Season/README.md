# Halloween Arcade Basketball

Unity 2022 coursework project for a Halloween-themed `1v1` arcade basketball game. The gameplay direction is inspired by the H5 web game `热血篮球赛`, especially its quick match rhythm, readable arcade basketball controls, and competitive skill feedback. This project should be presented as a student-built Unity coursework game inspired by that play style, not as a line-by-line rewrite or source-code port of the H5 game.

## Current Playable Scope

- Character-based roster with `8` playable characters:
  - `PUMPKIN`
  - `FRANKENSTEIN`
  - `MUMMY`
  - `VAMPIRE`
  - `CANDLEMAN`
  - `SCARECROW`
  - `WITCH`
  - `BLACK CAT`
- Main menu with `1 PLAYER`, `2 PLAYERS`, `QUICK MATCH`, `TRAINING`, and `AI: EASY / AI: NORMAL / AI: HARD`.
- Character-based match setup and a `4`-character single-elimination `TOURNAMENT` flow.
- Direct boot into a playable match loop from the Unity editor.
- Player movement, jump, dash, shoot, steal, stun, loose-ball pickup, score reset, timer, overtime, and post-match overlay.
- DragonBones-driven character pipeline, gameplay atlases, logo, and sound effects placed under Unity `Resources`.
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

- `DOCS/SPRINT_PLAN.md` - current development kanban, iteration focus, and delivery status
- `DOCS/PROJECT_FRAMING.md` - coursework-facing project positioning, inspiration notes, and agile management summary
- `TEST_LOG.md` - current verification log, smoke-test baseline, and known follow-up items
- `ASSET_CREDITS.md` - source notes for the active resource files used by the build
- `DOCS/COURSEWORK_ASSET_REVIEW.md` - coursework-facing asset review, audio source register, and current provenance risks
- `DOCS/FONT_PROVENANCE.md` - verified Google Fonts matches and the remaining runtime font follow-up list
- `DOCS/HALLOWEEN_ASSET_PROMPTS.md` - prompt references for future visual asset replacement work
- `DOCS/lecturer_asset_resources_guide.pdf` and `DOCS/评分标准.txt` - coursework delivery references

## Current Limitations

- Physics and collision feel still need more tuning for consistency across the full roster.
- DragonBones runtime support currently covers the gameplay subset needed for the current build, not every animation/runtime feature.
- Newer characters still need proportion and overlap polish in live gameplay.
- Tournament presentation, audio feedback, and delivery materials still need another pass before final submission.

## Notes

- `Builds/`, `Library/`, `Logs/`, `UserSettings/`, and other generated Unity folders are intentionally excluded from version control and can be regenerated when needed.
- The authoritative editor entry scene is `Assets/Scenes/Main.unity`.
