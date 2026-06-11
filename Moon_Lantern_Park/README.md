# Moon Lantern Park

Moon Lantern Park is a Unity 2022 coursework game project: a Halloween-themed 1v1 arcade basketball game built around fast local matches, character-specific super skills, AI opponents, training, tutorial support, adventure progression, and a tournament flow.

The basketball match rhythm and some core court logic were designed with reference to the H5 web game `热血篮球赛` (`Hot-Blooded Basketball`), especially the idea of compact 1v1 arcade basketball with quick possessions, shooting, steals, blocks, rebounds, and immediate scoring feedback.

This project is not a Unity port of that game. The Unity codebase, runtime systems, menus, game-state flow, Halloween theme, character roster, skill design, game modes, UI presentation, asset pipeline, testing process, and overall concept direction were redesigned and implemented as an original coursework project. The reference was used for the basketball-play foundation only; the wider game identity and implementation are original to this project.

## Project Overview

The goal of Moon Lantern Park is to deliver a focused vertical slice of an arcade sports game: one small but complete experience that is playable, readable, and easy to demonstrate.

Players control Halloween characters in a side-view basketball court. Each match is built around simple keyboard controls, quick movement, loose-ball contests, timed scoring, and character skills that create different tactical moments.

Current playable content includes:

- `8` Halloween-themed playable characters
- Single-player matches against AI
- Local 2-player keyboard matches
- Tutorial and training flows
- Adventure-style single-player progression
- A `4`-character single-elimination tournament
- Halloween basketball skins
- Character portraits, skill icons, match UI, audio feedback, and menu presentation

## Core Gameplay

During a match, players can:

- Move, jump, and dash across the court
- Shoot, steal, defend, and contest loose balls
- Score through normal shots and skill-enhanced actions
- Use a charged super skill unique to each character
- Pause, resume, return to menu, and continue through post-match flow

The current roster is:

| Character | Super Skill |
| --- | --- |
| `REAPER` | `DASH STEAL` |
| `GHOST CLOWN` | `REBOUND MAGNET` |
| `SKULL PIRATE` | `HOOP SHIELD` |
| `VAMPIRE` | `BLINK DUNK` |
| `CANDLEMAN` | `SPEED BOOST` |
| `SCARECROW` | `NEXT SCORE +2` |
| `WITCH` | `FREEZE 2 SEC` |
| `BLACK CAT` | `SURE BLOCK` |

## Game Modes

| Mode | Description |
| --- | --- |
| `Quick Match` | Choose a character, ball skin, and AI difficulty for a fast single-player match. |
| `Adventure` | A single-player route with story presentation, map progression, themed opponents, and match results. |
| `Tournament` | A 4-character single-elimination bracket with standings, match progression, and awards presentation. |
| `2 Player` | Local keyboard versus mode for two human players. |
| `Training` | Solo practice without an active opponent. |
| `Tutorial` | Guided onboarding for movement, shooting, defense, and super-skill use. |

AI difficulty presets:

- `AI: EASY`
- `AI: NORMAL`
- `AI: HARD`
- `AI: HELL`

## Controls

Single-player, tutorial, and training:

| Action | Key |
| --- | --- |
| Move | `A` / `D` |
| Jump | `W` |
| Guard / fake | `S` |
| Shoot / steal / interact | `B` |
| Super skill | `N` |

Local 2-player:

| Action | Player 1 | Player 2 |
| --- | --- | --- |
| Move | `A` / `D` | `Left Arrow` / `Right Arrow` |
| Jump | `W` | `Up Arrow` |
| Guard / fake | `S` | `Down Arrow` |
| Shoot / steal / interact | `B` | `L` |
| Super skill | `V` | `K` |

Match flow:

- Dash: double-tap left or right
- Pause / resume: `P` or `Esc`
- Return to menu: pause the match, then choose `MENU`

## How To Run

1. Install Unity Editor `2022.3.62f3c1`.
2. Open this repository folder in Unity Hub.
3. Open `Assets/Scenes/Main.unity`.
4. Press Play.

The project uses `mlpAutoBoot`, so the game enters the main menu flow directly from the Unity editor.

## Unity And Package Information

- Unity Editor: `2022.3.62f3c1`
- Main scene: `Assets/Scenes/Main.unity`
- Runtime boot: `mlpAutoBoot`
- Main Unity packages:
  - `com.unity.2d.sprite`
  - `com.unity.textmeshpro`
  - `com.unity.ugui`
  - `com.unity.modules.audio`

## Repository Structure

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/Main.unity` | Main Unity entry scene |
| `Assets/mlp/Scripts/Core` | Match data, controls, audio, game loop, tournament data, and core runtime systems |
| `Assets/mlp/Scripts/GameObjects` | Runtime players, ball, baskets, controllers, gameplay objects, and visual effects |
| `Assets/mlp/Scripts/States` | Menu bootstrap, mode routing, scene boot, UI flow, and presentation state |
| `Assets/mlp/Scripts/Data` | Character roster, skills, AI tuning, constants, asset keys, adventure data, and story data |
| `Assets/mlp/Scripts/Tutorial` | Tutorial flow, tutorial overlay, and tutorial opponent behavior |
| `Assets/mlp/Scripts/DragonBonesLite` | Lightweight animation runtime used by the current character pipeline |
| `Assets/mlp/Resources/mlp` | Runtime-loaded atlases, images, portraits, fonts, audio, prefabs, and animation data |
| `ArtSource` | Source art used to rebuild selected runtime assets |
| `Tools` | Helper scripts for rebuilding art, animation, UI assets, skill icons, and generated audio |
| `DOCS` | Coursework notes, asset provenance, copyright notes, font records, and submission references |

Generated Unity folders such as `Library/`, `Logs/`, `UserSettings/`, and local build outputs are intentionally excluded from version control.

## Development And Technical Focus

The project demonstrates:

- A complete playable Unity vertical slice rather than disconnected prototypes
- Custom game-state flow from menu to match to post-match screens
- Keyboard input profiles for solo, tutorial, training, and local 2-player play
- Character-specific skill definitions and feedback
- AI difficulty tuning for single-player modes
- Collision, scoring, ball possession, shot, rebound, and hoop interaction logic
- Runtime UI, HUD, menu, tournament, tutorial, and adventure presentation
- Audio feedback for menu actions, collisions, scoring, movement, and skills
- A lightweight animation/runtime asset pipeline suited to the project scope

## Testing And Process Evidence

The project includes a Unity editor smoke-test suite:

```text
Assets/mlp/Scripts/Editor/mlpSmokeTest.cs
```

The current verification history, tested features, and follow-up notes are recorded in:

```text
TEST_LOG.md
```

This repository is also used as the development record for the coursework project, showing planning, implementation, testing, cleanup, and iteration over time.

## Credits And Asset Provenance

Runtime assets and source notes are documented in:

```text
ASSET_CREDITS.md
```

Additional copyright and provenance records are kept in:

```text
DOCS/FONT_PROVENANCE.md
DOCS/FONTS_COPYRIGHT.md
DOCS/AUDIO_COPYRIGHT.md
```

The project also contains local tooling for regenerating or rebuilding selected assets:

```text
Tools/Art/rebuild_runtime_dragonbones_skeleton.py
Tools/Art/rebuild_active_character_assets.py
Tools/Art/rebuild_character_refresh_assets.py
Tools/Art/build_skill_icon_assets.py
Tools/Art/generate_awards_ui_assets.py
Tools/Audio/generate_halloween_core_sfx.py
```

## Coursework Context

Moon Lantern Park is maintained as a game programming coursework submission and development record. The repository is organised so the project can be opened, run, inspected, tested, and assessed with clear links between the game concept, implementation decisions, testing evidence, and asset provenance.
