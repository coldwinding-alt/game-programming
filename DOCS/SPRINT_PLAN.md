# Sprint Plan

This coursework project is organized as a documented vertical-slice port. The aim is to keep the repository readable, the Unity project runnable, and each major pass visible through focused delivery steps.

## Product Goal

Build a Unity 2022 vertical slice that faithfully ports the core structure, assets, and gameplay loop of the 2020 browser release into a maintainable Unity project.

## Sprint 1 - Repository And Documentation Setup

Status: completed.

Definition of done:

- The project is in a clean repository branch.
- Unity-generated folders and local reference folders are excluded from version control.
- The repository includes a README, sprint plan, test log, asset credits, and handoff notes.

Delivered:

- Repository branch setup for the current Unity port.
- Unity-safe `.gitignore`.
- Root documentation for running, testing, and tracking the project.

Remaining risks:

- Documentation must stay in sync as the gameplay port continues to change.

## Sprint 2 - Unity Baseline And Asset Migration

Status: completed.

Definition of done:

- The project opens in Unity `2022.3.62f3c1`.
- `Assets/Scenes/Main.unity` is available as the editor entry scene.
- Core migrated atlases, animation data, logo, and sound files are accessible through Unity `Resources`.
- Project settings and package files are committed.

Delivered:

- Unity project skeleton and boot scene.
- Migrated resource package under `Assets/BasketballLegends2020/Resources/BL2020`.
- Atlas loading, JSON loading, and lightweight animation runtime support.

Remaining risks:

- Current resource files are temporary for coursework delivery and still need later cleanup/replacement.
- Animation support is still a subset of the full original runtime behavior.

## Sprint 3 - Gameplay Fidelity Migration

Status: active and playable.

Definition of done:

- Menu, HUD, player, ball, basket, and match flow are playable from the editor.
- Movement, jump, dash, steal, loose-ball pickup, scoring, and overtime behave close to the reference release.
- The computer opponent is functional on both `EASY` and `NORMAL`.

Delivered:

- Game bootstrap, match loop, HUD, controllers, and gameplay objects.
- Text readability improvements and countdown cleanup.
- Steal/stun rework, contact-style loose-ball pickup, and hoop scoring sensor logic.
- Rim and backboard contact improvements to better align visible makes with actual scoring.

Remaining risks:

- Block, pump fake, dunk, and fuller defensive timing still need more parity work.
- Physics remains an approximation rather than a full one-to-one port of the original browser implementation.

## Sprint 4 - Testing And Final Polish

Status: in progress.

Definition of done:

- The repository clearly explains how to open and run the project.
- Core play checks are documented.
- The project remains buildable and editable in Unity 2022.
- Remaining gaps are documented for the next cleanup pass.

Delivered:

- Current test log with editor, match loop, and gameplay checks.
- Build entry point and migration status notes inside the Unity project.
- Known-limitations list for future cleanup and replacement work.

Remaining risks:

- More playtesting is still needed on longer matches and the unfinished screen flows.
- Final resource replacement is still pending.
