# Game Programming Coursework Portfolio

This repository is a coursework portfolio for Game Programming. It contains the final assessment project, two in-class Unity practice projects, and supporting course activity notes. Each Unity project is kept in its own folder with separate `Assets`, `Packages`, and `ProjectSettings`, so each project should be opened independently in Unity Hub.

## Final Assessment Project

### [`Moon_Lantern_Park/`](Moon_Lantern_Park/)

**Moon Lantern Park is the final project submitted for assessment.** It is a Unity 2022 Halloween-themed 1v1 arcade basketball game with a complete playable flow, custom character skills, AI opponents, local two-player mode, training, tutorial support, adventure progression, tournament play, menus, HUD, audio feedback, and documented testing.

For assessment, start here:

- Full project README: [`Moon_Lantern_Park/README.md`](Moon_Lantern_Park/README.md)
- Test log: [`Moon_Lantern_Park/TEST_LOG.md`](Moon_Lantern_Park/TEST_LOG.md)
- Asset credits: [`Moon_Lantern_Park/ASSET_CREDITS.md`](Moon_Lantern_Park/ASSET_CREDITS.md)
- Supporting documentation: [`Moon_Lantern_Park/DOCS/`](Moon_Lantern_Park/DOCS/)

The game focuses on a compact side-view basketball match: players move, jump, shoot, steal, block, contest rebounds, and use character-specific super skills. The wider project includes the surrounding game structure needed for a presentable vertical slice: menu flow, mode selection, character and ball selection, match HUD, pause/results flow, tournament presentation, story/adventure screens, resource provenance, and smoke-test records.

## How To Run The Final Project

Prebuilt macOS release:

1. Open the latest GitHub Release for this repository.
2. Download `MoonLanternPark-macOS.zip`.
3. Unzip it on macOS and run `Moon Lantern Park.app`.
4. If macOS blocks the unsigned app on first launch, use Control-click > Open, or allow it from System Settings > Privacy & Security.

Unity editor run:

1. Install Unity Editor `2022.3.62f3c1`.
2. Open Unity Hub.
3. Choose `Add project from disk`.
4. Select the folder `Moon_Lantern_Park/`.
5. Open `Assets/Scenes/Main.unity`.
6. Press Play.

Do not open the repository root as a Unity project. The root folder is a portfolio container, not a Unity project.

## Moon Lantern Park Controls

Single-player, tutorial, and training:

| Action | Key |
| --- | --- |
| Move | `A` / `D` |
| Jump | `W` |
| Guard / fake | `S` |
| Shoot / steal / interact | `B` |
| Super skill | `N` |

Local two-player mode:

| Action | Player 1 | Player 2 |
| --- | --- | --- |
| Move | `A` / `D` | `Left Arrow` / `Right Arrow` |
| Jump | `W` | `Up Arrow` |
| Guard / fake | `S` | `Down Arrow` |
| Shoot / steal / interact | `B` | `L` |
| Super skill | `V` | `K` |

Additional match controls:

- Dash: double-tap left or right.
- Pause / resume: `P` or `Esc`.
- Return to menu: pause the match, then choose `MENU`.

More detailed mode descriptions, roster information, technical notes, and provenance records are in [`Moon_Lantern_Park/README.md`](Moon_Lantern_Park/README.md).

## Repository Contents

| Path | Role | What It Contains |
| --- | --- | --- |
| [`Moon_Lantern_Park/`](Moon_Lantern_Park/) | **Final assessment project** | The submitted Halloween arcade basketball game. Includes the main scene, gameplay code, UI/state flow, character roster, AI difficulty tuning, tournament/adventure/training/tutorial modes, runtime resources, art/audio tooling, tests, and documentation. |
| [`InteractiveSolarSystemKids/`](InteractiveSolarSystemKids/) | 3D in-class Unity practice | A child-friendly interactive solar system scene with the Sun, Earth, and Moon; selectable objects; camera focus; orbit/rotation motion; fact panels; lighting; materials; interaction scripts; and audio. |
| [`SpaceShooter2DImprovement/`](SpaceShooter2DImprovement/) | 2D in-class Unity practice | A 2D space-shooter improvement exercise. The project includes menu and gameplay scenes, player/enemy/projectile systems, health and damage logic, power-ups, HUD/UI, audio, feedback, a developer console resource, and an editor builder script used to assemble the practice project. |
| [`event-driven-posts/`](event-driven-posts/) | In-class activity notes | Markdown write-ups for class topics including Unity `InputAction` lifecycle, `MonoBehaviour` lifecycle, and `PlayerPrefs` persistence. These are supporting learning records, not the final submitted game. |
| [`in-class-docs/`](in-class-docs/) | In-class documentation | A project snapshot from the earlier development stage of the basketball project, used as a process record for design direction, planned modes, input, technical structure, and vertical-slice framing. |

## Other Unity Practice Projects

### [`InteractiveSolarSystemKids/`](InteractiveSolarSystemKids/)

This is the 3D in-class practice project. It demonstrates a small interactive educational scene rather than a full game submission. The scene lets the player click the Sun, Earth, or Moon, move the camera smoothly to the selected object, read a simple fact panel, and return to the main view.

Main reference file:

- [`InteractiveSolarSystemKids/README.md`](InteractiveSolarSystemKids/README.md)

### [`SpaceShooter2DImprovement/`](SpaceShooter2DImprovement/)

This is the 2D in-class practice and improvement project. It is organized as a Unity space shooter with separate menu and gameplay scenes. The exercise focuses on systems practice: scene setup, player control, shooting, enemies, collisions, health, power-ups, UI pages, feedback messages, audio cues, difficulty pacing, and replay/menu flow.

Useful entry points:

- `SpaceShooter2DImprovement/Assets/Scenes/MainMenu.unity`
- `SpaceShooter2DImprovement/Assets/Scenes/GameLevel.unity`
- `SpaceShooter2DImprovement/Assets/Editor/ImprovementProjectBuilder.cs`

## Documentation And Evidence

The repository is arranged to show both the final game and the development process behind it.

Moon Lantern Park includes:

- A detailed project README with gameplay, controls, modes, technical structure, testing, and credits.
- A current test log recording Unity version, entry scene, supported modes, verification dates, and known follow-up items.
- Asset and copyright documentation covering image, font, audio, and external-resource provenance.
- Local tooling for rebuilding selected art, animation, UI, and audio assets.

The class activity folders are included for context. They are separate from the final assessment project and should not be mistaken for the final submission.

## Unity Version

All Unity projects in this repository are set to:

```text
Unity Editor 2022.3.62f3c1
```

## Opening Projects Correctly

Open only one Unity project folder at a time:

```text
Moon_Lantern_Park
InteractiveSolarSystemKids
SpaceShooter2DImprovement
```

Unity-generated folders such as `Library/`, `Logs/`, `Temp/`, `Builds/`, and `UserSettings/` are local/generated data and are intentionally not treated as the main source content of the repository.

## Assessment Reading Order

For the clearest review path:

1. Open this root README to understand the repository layout.
2. Go to [`Moon_Lantern_Park/README.md`](Moon_Lantern_Park/README.md) for the final project details.
3. Run `Moon_Lantern_Park/Assets/Scenes/Main.unity` in Unity `2022.3.62f3c1`.
4. Check [`Moon_Lantern_Park/TEST_LOG.md`](Moon_Lantern_Park/TEST_LOG.md) and [`Moon_Lantern_Park/DOCS/`](Moon_Lantern_Park/DOCS/) for testing and provenance evidence.
