# Game Programming Coursework Portfolio

This repository contains my Game Programming coursework projects, in-class exercises, and development process notes. It is organised as a small Unity coursework portfolio: each Unity project lives in its own folder with its own `Assets`, `Packages`, and `ProjectSettings`, so the projects can be opened independently in Unity Hub.

## Main Project

### `Moon_Lantern_Park/`

`Moon Lantern Park` is the main coursework game in this repository. It is a Halloween-themed 1v1 arcade basketball game made in Unity 2022, with single-player AI matches, local 2-player play, training, tutorial support, adventure progression, tournament flow, character super skills, basketball skins, audio feedback, and a complete menu flow.

The basketball match pacing and part of the basic court logic were designed with reference to the H5 web game `热血篮球赛` (`Hot-Blooded Basketball`). The reference mainly relates to the compact 1v1 basketball foundation: fast possessions, quick attack and defence changes, shooting, stealing, blocking, rebounding, and immediate scoring feedback.

This project is not a Unity port of `热血篮球赛`. The Unity project structure, codebase, runtime systems, menu and state flow, Halloween theme, character concepts, skill system, adventure mode, tournament mode, UI presentation, asset workflow, and testing records were redesigned and implemented for this coursework project. The reference is limited to the basketball-match foundation; the wider game concept and implementation are original.

More details are available in:

```text
Moon_Lantern_Park/README.md
Moon_Lantern_Park/TEST_LOG.md
Moon_Lantern_Park/ASSET_CREDITS.md
Moon_Lantern_Park/DOCS/
```

## Other Coursework Projects And Exercises

### `InteractiveSolarSystemKids/`

A short Unity in-class assignment for an interactive solar system scene aimed at children. The scene includes the Sun, Earth, and Moon; selectable celestial objects; smooth camera focus; child-friendly information panels; orbit and rotation motion; materials; lighting; camera interaction; and audio.

More details are available in:

```text
InteractiveSolarSystemKids/README.md
```

### `SpaceShooter2DImprovement/`

A Unity 2D space-shooter exercise and improvement project used for systems practice and feature iteration during the course.

### `event-driven-posts/`

Course notes and short posts about event-driven programming, Unity input actions, MonoBehaviour lifecycle behaviour, and simple persistence patterns.

### `in-class-docs/`

In-class documentation, project snapshots, and process records.

## Unity Version

The Unity projects in this repository use:

```text
Unity Editor 2022.3.62f3c1
```

## How To Open A Project

1. Open Unity Hub.
2. Choose `Add project from disk`.
3. Select one project folder, for example:

```text
Moon_Lantern_Park
InteractiveSolarSystemKids
SpaceShooter2DImprovement
```

4. Open the entry scene listed in that project's README or project notes.
5. Press Play.

Do not open the repository root as a Unity project. Each Unity project should be opened from its own folder.

## Repository Structure

| Path | Purpose |
| --- | --- |
| `Moon_Lantern_Park/` | Main coursework game: Halloween 1v1 arcade basketball |
| `InteractiveSolarSystemKids/` | Child-friendly interactive solar system assignment |
| `SpaceShooter2DImprovement/` | 2D space-shooter practice and improvement project |
| `event-driven-posts/` | Course notes and topic write-ups |
| `in-class-docs/` | In-class documents, snapshots, and process notes |

## Development And Assessment Evidence

This repository is used to show the development process behind the coursework: concept planning, implementation, testing, debugging, iteration, documentation, and asset provenance.

The main project records evidence for:

- Game concept and design goals
- Unity project structure and technical implementation
- Input, collision, UI, audio, AI, animation, and game-state management
- Testing, debugging, and improvement
- Asset sources, copyright notes, and replacement records
- Coursework process documents and development snapshots

## Version Control Notes

Generated Unity folders and local build output are intentionally excluded from version control, including:

```text
Library/
Logs/
Temp/
Builds/
UserSettings/
```

Unity can recreate these folders when a project is opened. The repository keeps the source files, scripts, resource records, documentation, and testing evidence needed for review and continued development.
