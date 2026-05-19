# Project Snapshot — 2026-05-19

## What Is This Game

Rim Rush: Championship Season is a **2D side-scrolling arcade basketball 1v1 game**, built in Unity 2022 LTS with a Halloween theme.

- Gameplay is inspired by the H5 web game *热血篮球赛*: fast-paced competition, highly readable controls, direct feedback
- Side-view perspective on a horizontal court with a basket at each end; players move horizontally and jump vertically
- Fixed resolution of 1066×640, orthographic camera with point filtering, pixel-art style
- All game objects are procedurally constructed via C# code at runtime — no prefabs, single scene `Main.unity`

## What Players Do

Players control a Halloween character and compete in a 60-second 1v1 basketball match against an opponent. The goal is to **score more points than the opponent**.

Core actions:

| Action | Description |
|---|---|
| Move | Horizontal left/right movement (15% slower when holding the ball) |
| Jump | Vertical jump for rebounds, blocks, and shooting off the ground |
| Shoot | Release the ball with a physics-based parabolic arc |
| Dunk | Close-range slam dunk after jumping, 90% base success rate |
| Alley-oop | A high-arc special shot |
| Dash | Double-tap a direction for a quick burst forward; can be interrupted by blocks |
| Steal | Attempt to strip the ball when near the opponent |
| Block / Pump Fake | Defensive button — block stops dash attacks; releasing triggers a pump fake to bait the opponent into jumping |
| Super Ability | Charges over time, four types: Super Dunk, Super Dash, Shield, Teleport |

Scoring: The ball must pass through an upper sensor then a lower sensor inside the basket to register a score — 2 points (close range) or 3 points (far range). A tied game goes to 15-second overtime.

## Game Modes

| Mode | Description |
|---|---|
| QUICK MATCH | Single player vs AI, 4 difficulty levels (Easy / Normal / Hard / Hell) |
| 2 PLAYERS | Local 1v1, P1: WASD+B/V, P2: Arrow keys+L/K |
| TRAINING | Unlimited time practice, no opponent timer pressure |
| TOURNAMENT | 8-character bracket: 2-division round-robin regular season, semifinals, 3rd-place match, grand final, with an awards podium |

## Character Roster

8 Halloween-themed characters, each with a unique super ability:

PUMPKIN · FRANKENSTEIN · MUMMY · VAMPIRE · CANDLEMAN · SCARECROW · WITCH · BLACK CAT

Character animations are driven by a custom lightweight DragonBones runtime (`DBLiteRuntime`) that reads JSON skeleton data and texture atlases at runtime.

## Vertical Slice

A vertical slice is a self-contained, complete, playable fragment of the game that demonstrates the core experience of the final product. The vertical slice for this project is:

> **A full QUICK MATCH session: Menu → Character Select → Tip-off → 60-second match → Scoring → Results → Return to Menu.**

This slice covers:

- Menu navigation and UI interaction
- Character selection flow
- Core match gameplay loop (move, jump, shoot, dunk, steal, block, super abilities)
- AI opponent behavior (4 difficulty levels)
- Timer and scoring system
- Real-time HUD display
- Results screen and return flow

When this path works end-to-end with a complete experience, the core value of the game is delivered. TOURNAMENT, 2-player mode, and additional characters are all extensions built on top of this slice.

## Development Snapshot

| Dimension | Detail |
|---|---|
| Genre | 2D side-scrolling arcade basketball 1v1 |
| Perspective | 2D side-view, horizontal court, baskets at both ends |
| Engine | Unity 2022.3.62f3c1 (LTS), pure C# |
| Resolution | Fixed 1066×640, orthographic camera + point filtering (pixel-art style) |
| Scene | Single scene `Main.unity`, all objects procedurally generated at runtime, no prefabs |
| Character Animation | Custom lightweight DragonBones runtime, parses JSON skeleton + texture atlas |
| Character Roster | 8 Halloween-themed characters, each with a unique super ability |
| Game Modes | Quick Match, 2 Players, Training, 8-player Tournament |
| Core Gameplay | 60-second 1v1: shoot / dunk / alley-oop / dash / steal / block / pump fake / super abilities |
| AI System | 5 behavioral strategies x 12 skill tiers x 21 tunable parameters, 4 difficulty levels |
| Input | Local keyboard multiplayer: P1 WASD+B/V, P2 Arrow keys+L/K |
| Asset Management | Unity Resources folder runtime loading, atlas JSON+PNG, 18 SFX + 1 BGM |
| Build / Test | Editor build tools + batch-mode smoke tests |
| Documentation | Sprint kanban (K-01 to K-42), asset credits register, font provenance, test log |
