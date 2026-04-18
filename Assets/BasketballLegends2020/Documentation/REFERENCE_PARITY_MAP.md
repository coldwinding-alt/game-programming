# Reference Parity Map

Updated: 2026-04-18

This document tracks the code-level migration path from the Basketball Legends 2020 H5 reference into the Unity coursework build. The goal is to keep future work tied to concrete reference functions instead of tuning by feel only.

## Core Module Mapping

| Reference H5 area | Unity area | Current status | Next parity work |
| --- | --- | --- | --- |
| `ObjectsData` constants | `BLObjectsData.cs` | Main court, ball, basket, player, dash, steal, stun, dunk, block, and pump constants are mirrored where used. | Continue adding missing super-shot, shield, and exact AI delay-table constants. |
| `BallObject.shoot / calcThrowVel / calcDispersion` | `BLBallObject.Shoot / CalcThrowVel / CalcDispersion` | Shot arc and dispersion follow the reference formula structure. | Keep tuning against Nape gravity/material behavior and original player accuracy tables. |
| `BallObject.dunk` | `BLBallObject.Dunk` | Ball placement, velocity, completed/missed dunk behavior, and scoring arming now follow the reference values. | Add richer net/rim response and stat/event handling. |
| Basket glass/rings/sensors | `BLBasketObject`, `BLBallObject.ResolveBasket` | Uses glass collision, two rim contacts, and upper/down sensor scoring order. | Improve material response to match Nape contacts more closely. |
| Player double-tap input | `BLKeyboardController` | Uses a 300 ms same-direction keydown window like the reference `doubleTapRate`. | Add mobile/general controller parity only if needed. |
| `PlayerObject.makeThrow / makeDunk / endDunk` | `BLPlayerObject.MakeThrow / TryStartDunk / UpdateDunk` | Airborne shot now checks dunk zones, moves to `DUNK_X/DUNK_Y`, then releases the ball through `Ball.Dunk`. | Add DragonBones frame-event timing and more exact animation-complete callbacks. |
| `PlayerObject.makeSteal / checkToBeStolen / getBeStolen` | `BLPlayerObject.BeginSteal / CheckToBeStolen / GetBeStolen` | Forward steal lane, empty-hand stun, ball knock-free velocity, and stun timing are aligned. | Add landing and throw-frame interaction windows. |
| `PlayerObject.makeBlockOrPump / setBlock / releaseBlockOrPump / unBlock` | `BLPlayerObject.BeginBlockOrPump / UpdateBlockOrPump / TryBlockBall` | Holding the action key now starts pump fake with the ball and block without the ball, uses original animation durations, disables hand pickup during block, and resolves a block sensor-style ball deflection. | Replace approximation with exact DragonBones event callbacks and swept collision against the original block shape. |
| `AIController.strategyDefence / tryToSteal` | `BLAIController.UpdateDefence` | Defence now keeps the original-style `OPPONENT_DELTA` spacing, reacts to pump fake, contests airborne shots, uses block holds, and avoids instant overlap steals. | Port `FullDelay`, `Delay`, and original chance tables instead of current Unity timers. |
| `AIController.strategyAttack / moveInAttack` | `BLAIController.UpdateAttack / SetAttackPoint` | Attack now chooses an attack point and jump point, moves into the lane, can pump against close defenders, jumps at the original-style takeoff point, and triggers dunk/shot decisions in the air. | Port rebound, defence2, and more exact close-defender dash/avoidance branches. |
| `AIController` strategy modes | `BLAIController` | Current Unity AI has attack, defence, loose-ball, easy/normal difficulty, dash avoidance, block/pump reactions, and dunk attempt hooks. | Port the remaining original strategy methods one by one: ball fight, jump ball, rebound, defence2. |
| HUD countdown/message | `BLHudView` | Countdown cleanup follows the reference hide-after-final-step behavior. | Continue menu/post-match screen parity. |

## Function-Level Checklist

- Function-level migration checklist is maintained in:
  - `Assets/BasketballLegends2020/Documentation/REFERENCE_FUNCTION_PARITY_MATRIX.md`
- Side-by-side runtime verification checklist is maintained in:
  - `Assets/BasketballLegends2020/Documentation/PARITY_SIDE_BY_SIDE_CHECKLIST.md`
- Acceptance dimensions for each function row:
  - behavior parity
  - trigger/timing parity
  - window/condition parity

## Current Reference Anchors

- Dash: reference `doubleTapRate = 300`, controller flags `isbtnADouble`, `isbtnDDouble`, `isbtnLeftDouble`, `isbtnRightDouble`.
- Dunk zones: `DUNK_ZONE1_Y = 280`, `DUNK_ZONE2_Y = 300`, `DUNK_X = 100`, `DUNK_Y = 180`.
- Dunk timing: reference `makeDunk` uses `520`, `350`, and `480` ms animation paths divided by `1.3333`.
- Dunk ball release: reference `BallObject.dunk` places the ball at basket center plus/minus `17` on success, uses velocity `(-260 * side, 400)`, and missed dunks use basket center with velocity `(-550 * side, 400)`.
- Block/pump input: reference `makeBlockOrPump` uses the same action input; with ball it enters `pumpStart`, without ball it enters `blockStart`.
- Block/pump animation timings from `sk2.json`: `blockStart` 3 frames, `blockEnd` 5 frames, `pumpStart` 4 frames, `pumpEnd` 4 frames at 30 fps.
- Block collision: reference `onBallBlock` only blocks when the ball is in front of the block shape relative to the ball side, then calls `BallObject.setState("block", blocker.SIDE)`.
- AI defence spacing: reference uses `ObjectsData.OPPONENT_DELTA = 60` around the live ball holder before deciding steal, block, or jump contest.
- AI attack planning: reference chooses `attackPoint`, derives `jumpPoint`, then decides jump/throw/pump based on delay objects and defender position.
- AI source strategy names to port next: `strategyDefence`, `strategyBallFight`, `strategyAttack`, `strategyJumpBall`, `strategyRebound`, and `strategyDefence2`.
- New runtime parity hooks now active:
  - delay family (`FullDelay/UseDelay/AIUseDelay/SimpleDelay/NegativeDelay`)
  - DragonBones-lite frame event + animation complete callbacks
  - `MatchProcessor` style score context (`shoot/block/sensor chain`)

## Current Priority

1. Replace the current block/pump approximation with exact frame-event and animation-complete behavior.
2. Move AI timers from Unity-side cooldowns toward the original `Delay` / `FullDelay` strategy system.
3. Continue physics parity for ball/rim/backboard/block contacts.
4. Keep each migration step playable, documented, and buildable in Unity 2022.
