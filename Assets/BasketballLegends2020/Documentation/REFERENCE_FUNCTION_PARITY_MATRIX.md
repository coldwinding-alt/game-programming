# Reference Function Parity Matrix

Updated: 2026-04-18

This matrix tracks function-level parity work for the in-match core systems.  
Scope for this round: action timing, dash/steal/block-pump flow, AI strategy tree, hoop/score chain.

## Delay and Timing Core

| Reference (H5) | Unity mapping | Gap type | Priority | Current note |
| --- | --- | --- | --- | --- |
| `Delay` | `SimpleDelay` | migrated | P0 | Added and wired in AI timing paths. |
| `FullDelay` | `FullDelay` | migrated | P0 | Added `fixedDelay + random(range)` semantics. |
| `UseDelay` | `UseDelay` | migrated | P0 | Added fixed-delay activation for player dash cooldown. |
| `AIUseDelay` | `AIUseDelay` | migrated | P0 | Added `useIt/skipIt` with negative pre-wait semantics. |
| `NegativeDelay` | `NegativeDelay` | migrated | P1 | Added for parity completeness; not fully consumed yet. |

## PlayerObject Function Parity

| Reference (H5 `PlayerObject`) | Unity mapping | Gap type | Priority | Current note |
| --- | --- | --- | --- | --- |
| `resetVars` | `Restart` reset block in `BLPlayerObject` | partial | P0 | Main state flags aligned; super branches still missing. |
| `update` | `BLPlayerObject.Update` | partial | P0 | Main loop aligned with action/dash/block/pump gates; remaining super flows pending. |
| `makeDash` | `StartDash` + `UseDelay` gate | partial | P0 | Dash gate aligned; countdown pre-tick added to fix first-dash miss after start. |
| `endDash` | dash end block in `Update` | partial | P0 | Re-arms `dashDelay` on dash finish. |
| `makeSteal` | `BeginSteal` | partial | P0 | Uses action window and signal dispatch. |
| `tryToSteal` | `ResolveStealAttempt` + `BLGameCore.TryStealBall` | partial | P0 | Lane-based target scan aligned; deeper combo/landing windows pending. |
| `checkToBeStolen` | `CheckToBeStolen` | migrated | P0 | Front-lane directional check ported. |
| `getBeStolen` | `GetBeStolen` | partial | P0 | Supports no-ball stun and ball loose impulse; fine timing still tunable. |
| `makeBlockOrPump` | `BeginBlockOrPump` | partial | P0 | Ball holder enters pump path, defender enters block path. |
| `setBlock` | `UpdateBlockOrPump` (holding phase) | partial | P0 | Hold phase wired; body-shape mass parity still simplified in Unity physics. |
| `releaseBlockOrPump` | `UpdateBlockOrPump` release path | partial | P0 | End phase and timing mapped. |
| `unBlock` | block end cleanup in `UpdateBlockOrPump` | partial | P0 | Pickup restore and state reset mapped. |
| `makeThrow` | `MakeThrow` | partial | P0 | Throw type and `MatchProcessor.Shoot` context added. |
| `makeFloorThrow` | `BeginFloorThrow` + frame event | partial | P0 | Now driven by `throw_land` frame event with completion fallback. |
| `makeDunk` | `TryStartDunk` | partial | P0 | Zone checks and timings present; super/mega branches not in this round. |
| `endDunk` | `ReleaseDunkBall` + `UpdateDunk` end path | partial | P0 | Release now event-driven and tracked in score context. |
| `onFrameEvent` | `OnAnimationFrameEvent` | migrated | P0 | `throw/action/dunk` events wired via DBLite frame callback. |
| `onAnimationComplete` | `OnAnimationComplete` | migrated | P0 | `block/pump/throw/steal/dunk` complete callbacks wired. |

## AI Strategy Parity (Normal as reference baseline)

| Reference (H5 `BaseAIController/AIController/AIController2`) | Unity mapping | Gap type | Priority | Current note |
| --- | --- | --- | --- | --- |
| `processPlayerSignal` | `ProcessPlayerSignal` | partial | P0 | Added `startSteal/steal/jumpA/pump/dash/stun` chain. |
| `playerOnGround / playerOnDashEnd / playerOnBlock` | `PlayerOnGround / PlayerOnDashEnd / PlayerOnBlock` | partial | P0 | Lifecycle callbacks are now wired from player state transitions into AI controller. |
| `strategyDefence` | `StrategyDefence` | partial | P0 | Added spacing, steal delay gate, and contest jump timing. |
| `strategyDefence2` | `StrategyDefence2` (`BLAIController2` override) | partial | P1 | Specialized lane/steal behavior present; still being tuned. |
| `strategyBallFight` | `StrategyBallFight` | partial | P0 | Loose-ball and rebound-jump decisions migrated. |
| `strategyAttack` | `StrategyAttack` | partial | P0 | Attack/jump points, throw timing, dash decision path migrated. |
| `strategyJumpBall` | `StrategyJumpBall` | partial | P1 | Delay-driven jump added; needs scenario-specific hooks. |
| `strategyRebound` | `StrategyRebound` | partial | P0 | Rebound move/jump path added. |
| `tryToSteal` | `TryToSteal` | partial | P0 | Easy disables active steals, Normal uses chance windows. |
| `tryToAvoid` | `TryToAvoid` | partial | P1 | Jump/move/dash escape paths present. |
| `setAttackPoint` | `SetAttackPoint` | partial | P0 | Core attack point randomization and side mirroring migrated. |
| `moveInAttack` | `MoveInAttack` | partial | P0 | Jump-point then attack-point structure migrated. |

## Ball / Basket / MatchProcessor Parity

| Reference (H5) | Unity mapping | Gap type | Priority | Current note |
| --- | --- | --- | --- | --- |
| `BallObject.shoot` | `BLBallObject.Shoot` | partial | P0 | Context now records throw type into `BLMatchProcessor`. |
| `BallObject.dunk` | `BLBallObject.Dunk` | partial | P0 | Release and completion chain aligned with score context. |
| `BallObject.setState(\"block\", side)` | `BLBallObject.ApplyBlock` | partial | P0 | Block side + human source now recorded in match processor. |
| Basket upper/down sensors | `BLBallObject.ProcessScoreSensors` | partial | P0 | Upper-first score arming path now tracked and reported. |
| `MatchProcessor.shoot` | `BLMatchProcessor.Shoot` | migrated | P0 | Score context reset and shot source recorded. |
| `MatchProcessor.block` | `BLMatchProcessor.Block` | migrated | P0 | Block source side/human flag recorded. |
| `MatchProcessor.processSensor` | `BLMatchProcessor.ProcessSensor` | migrated | P0 | Upper/down ordering gate implemented. |
| `MatchProcessor.sendScore` | `BLMatchProcessor.ResolvePointsForScore` | partial | P0 | 3pt/2pt + block-owned 2pt branch implemented. |

## Remaining High-Risk Gaps (Next Rounds)

1. Player super branches: `makeMegaDunk/makeSuperDash/makeShield/makeAlleyOop`.
2. Full defence2 and rebound strategy probabilities per role profile.
3. Block body/mass behavior parity against original Nape setup.
4. Expanded DragonBones runtime parity (draw-order, color-frame, deeper event edge cases).
