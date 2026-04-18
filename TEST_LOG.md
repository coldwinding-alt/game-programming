# Test Log

## 2026-04-18 - Repository Setup And Unity Baseline

Environment:

- Unity `2022.3.62f3c1`
- Windows desktop workflow
- Branch: `feature/bl2020-faithful-port`

Completed checks:

| Check | Result | Notes |
| --- | --- | --- |
| Repository import layout | Pass | Added `Assets`, `Packages`, `ProjectSettings`, `.gitignore`, and project documentation without Unity cache or build folders |
| Unity project structure | Pass | Project opens as a standard Unity 2022 project with `Assets/Scenes/Main.unity` enabled in build settings |
| Editor boot scene | Pass | `Main.unity` is the active editor entry scene for the current project |
| Runtime bootstrap | Pass | `BL2020AutoBoot` starts the menu flow directly from Play Mode |
| Documentation coverage | Pass | README, sprint plan, test log, asset credits, migration notes, and project context are included in the repository |

## 2026-04-18 - Match Loop And Playability Checks

Completed checks:

| Check | Result | Notes |
| --- | --- | --- |
| Main menu flow | Pass | Menu exposes `1 PLAYER`, `2 PLAYERS`, `QUICK MATCH`, `TRAINING`, and difficulty toggle |
| Match start sequence | Pass | Countdown and center text enter the match correctly from the current boot flow |
| Scoreboard and timer | Pass | HUD shows score and timer during play and handles overtime state |
| Difficulty toggle | Pass | `AI: EASY / AI: NORMAL` changes the computer opponent behavior from the menu |
| Match end flow | Pass | Match reaches time-out and shows a post-match overlay rather than leaving the match in an unusable state |

## 2026-04-18 - Gameplay Fidelity Checks

Completed checks:

| Check | Result | Notes |
| --- | --- | --- |
| Steal lane and stun timing | Pass | Steal interaction follows a forward-lane check and applies a longer stun window closer to the reference behavior |
| Loose-ball pickup | Pass | Pickup uses a contact-style hand window instead of depending on the steal button |
| Countdown cleanup | Pass | `GO!!!` is explicitly hidden after the intro sequence so stale text does not remain onscreen |
| Rim and backboard contact | Pass | Hoop contact now resolves against left/right rim circles and a backboard collision region |
| Score confirmation | Pass | Points are awarded through upper/down score sensor ordering rather than a single coarse hoop check |

## Follow-Up Items

- Continue tuning block, pump fake, dunk, and rebound behavior.
- Continue tightening menu flow and post-match screen parity.
- Replace temporary reference resource files in a later cleanup pass.
