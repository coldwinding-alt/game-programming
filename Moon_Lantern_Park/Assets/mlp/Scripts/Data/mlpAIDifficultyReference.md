# AI Difficulty Parameter Full Table (SkillsData + DifficultyTuning Combined)

> Corresponding scripts: `mlpAISkillsData.cs` (skill-level layer) + `mlpAIDifficultyTuning.cs` (physical-capability layer)
>
> Two-layer split: **SkillsData** = the AI's "brain" (probability / timing, decides whether it wants to do something), **DifficultyTuning** = the AI's "body" (distance / range, decides whether it can reach it)
>
> 38 parameters in total, covering 13 basketball behavior categories.

---

## 1. Shooting

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 1 | `Accuracy` | SkillsData | Numeric stacking: lower is more accurate, 0 = perfect accuracy | 0.14 | 0.12 | 0.04 | **0** |
| 2 | `Attack` | SkillsData | Delay timer: delay from jump to release (seconds); smaller is faster | 0.4 | 0.4 | 0.3 | **0.02** |
| 3 | `AttackAtOnce` | SkillsData | Probability roll: chance to shoot immediately after catching the ball | 20% | 30% | 60% | **100%** |
| 4 | `JumpThrow` | SkillsData | Probability roll: 1) someone behind jumps first 2) interrupt the jump when the opponent jumps | 30% | 40% | 100% | **100%** |
| 5 | `DefenceContestDistance` | DifficultyTuning | Distance: how close the AI must be to contest a shot | 180 | 180 | 220 | **260** |

## 2. Dunking

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 6 | `ChanceToCompleteDunk` | SkillsData | Probability roll: whether a dunk succeeds after takeoff | 40% | 45% | 70% | **98%** |

## 3. Stealing

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|------|--------|------|------|
| 7 | `MakeSteal` | SkillsData | Probability roll: after distance conditions are met, roll to decide whether to attempt a steal | 30% | 40% | 70% | **90%** |
| 8 | `DelaySteal` | SkillsData | Delay timer: cooldown interval between steals (seconds) | 3.0 | 2.5 | 1.0 | **0.55** |
| 9 | `StealBehindDistance` | DifficultyTuning | Distance: maximum distance to trigger a steal check when chasing from behind | 80 | 80 | 110 | **140** |
| 10 | `StealBasketDistance` | DifficultyTuning | Distance: maximum distance to trigger a steal when the opponent is near the basket | 45 | 45 | 65 | **90** |
| 11 | `StealRangeBonus` | DifficultyTuning | Numeric stacking: extra bonus to steal-check distance | 0 | 0 | 0 | **+20** |

> On Easy difficulty, stealing is completely disabled (`TryToSteal()` returns on the first line), so parameters 7-11 have no effect on Easy.

## 4. Blocking

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 12 | `MakeBlock` | SkillsData | Probability roll: chance to jump for a block when the opponent rushes in from behind | 20% | 30% | 50% | **100%** |
| 13 | `Defence` | SkillsData | Delay timer: delay after the opponent jumps before the AI jumps to contest (seconds) | 0.5 | 0.5 | 0.3 | **0.02** |
| 14 | `DashBlockRangeMaxDistance` | DifficultyTuning | Distance: maximum distance for dash block detection | 180 | 180 | 220 | **280** |

## 5. Rebounding

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 15 | `ChanceToRebound` | SkillsData | Probability roll: chance to jump for the ball when it is in the rebound area | 30% | 35% | 70% | **100%** |
| 16 | `ReboundFixed` | SkillsData | Delay timer: fixed wait time before rebound jump (seconds) | 0.35 | 0.3 | 0.2 | **0** |
| 17 | `ReboundRange` | SkillsData | Delay timer: random fluctuation range for rebound jump (seconds) | 0.1 | 0.1 | 0.1 | 0.1 |

## 6. Dash

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 18 | `MakeDash` | SkillsData | Probability roll: second-stage check after `ReactOnOpponent` passes | 100% | 100% | 100% | **100%** |
| 19 | `DelayDash` | SkillsData | Delay timer: cooldown interval between dash decisions (seconds) | 5.0 | 4.5 | 3.0 | **1.0** |
| 20 | `HolderSuperDashMinDistance` | DifficultyTuning | Distance: minimum running distance required to trigger a dash while holding the ball | 90 | 90 | 70 | **40** |
| 21 | `HolderSuperDashMaxDistance` | DifficultyTuning | Distance: upper bound of the effective range for ball-carrying dashes | 460 | 460 | 520 | **620** |
| 22 | `LooseBallSuperDashDistance` | DifficultyTuning | Distance: maximum distance for dashing to a loose ball | 120 | 120 | 90 | **60** |
| 23 | `AttackSuperDashDistance` | DifficultyTuning | Distance: maximum distance for an offensive dash toward the basket | 260 | 260 | 220 | **150** |
| 24 | `DashCooldownMultiplier` | DifficultyTuning | Multiplier: scale factor for dash cooldown time | 1.0 | 1.0 | 1.0 | **0.6** |
| 25 | `BonusSuperDashCooldown` | DifficultyTuning | Delay timer: extra dash cooldown (seconds), balancing parameter | 0 | 0 | 0 | **+10** |

> `MakeDash` is 100% across all four difficulties; the actual difference comes from `DelayDash` and `DashCooldownMultiplier`.

## 7. Defence Reaction

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 26 | `ReactOnOpponent` | SkillsData | Probability roll: react when there is a defender behind you (change direction or dash) | 30% | 40% | 60% | **100%** |

## 8. Avoid Steal

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 27 | `AvoidSteal` | SkillsData | Probability roll: try to evade when the opponent attempts a steal (dash / jump / sidestep) | 20% | 30% | 60% | **95%** |

## 9. Pump Fake

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 28 | `MakePump` | SkillsData | Probability roll: chance to use a pump fake on offense (dead code, not wired up) | 50% | 50% | 50% | 50% |
| 29 | `JumpPump` | SkillsData | Probability roll: chance to be fooled by an opponent's pump fake and jump | 80% | 70% | 50% | **10%** |

## 10. Jump Ball

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 30 | `JumpBall` | SkillsData | Delay timer: jump-ball takeoff timing; smaller values jump earlier | 0.45 | 0.45 | 0.4 | **0.1** |

## 11. Movement

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 31 | `MoveDelay` | SkillsData | Delay timer: interval between offense/defense movement decisions (seconds); smaller is more agile | 0.1 | 0.08 | 0.05 | **0.02** |
| 32 | `AttackPressureDistance` | DifficultyTuning | Distance: how far out the AI starts pressuring on offense | 140 | 140 | 180 | **240** |

## 12. Super/Ultimate

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 33 | `CoolDown` | SkillsData | Delay timer: super-charge time (seconds) | 48 | 48 | 28 | **14** |
| 34 | `OpeningSuperChargeFraction` | DifficultyTuning | Ratio: amount of ultimate energy available at the start | 0 | 0 | 0 | **55%** |
| 35 | `NativeSuperRefundFraction` | DifficultyTuning | Ratio: energy refunded after using an ultimate | 0 | 0 | 0 | **35%** |
| 36 | `HasBonusSupers` | DifficultyTuning | Boolean: whether extra ultimates are unlocked | ❌ | ❌ | ❌ | **✅** |

## 13. Survivability

| # | Parameter | Source | Mechanism | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 37 | `StunDurationMultiplier` | DifficultyTuning | Multiplier: stun duration scaling | 1.0 | 1.0 | 1.0 | **0.65** |
| 38 | `BonusShieldCooldown` | DifficultyTuning | Delay timer: extra cooldown for the shield skill (seconds), balancing parameter | 0 | 0 | 0 | **+24** |

---

## Mechanism Types

| Mechanism | Principle | Typical Parameters |
|------|------|----------|
| **Probability roll** | After conditions are met, `Random.value < parameter` decides whether it executes | `MakeSteal`, `ChanceToCompleteDunk` |
| **Delay timer** | Various delay timers control action intervals / reaction latency | `Attack`, `Defence`, `DelaySteal` |
| **Distance check** | Controls the spatial range used for AI behavior checks | `StealBehindDistance`, `DefenceContestDistance` |
| **Multiplier stacking** | Scale factor or direct addition/subtraction applied to a base value | `StunDurationMultiplier`, `StealRangeBonus` |

> In practice, all behaviors are a **"timer gate + probability roll" composite pattern**. No behavior is decided by a single mechanism alone.

---

## Difficulty Design Summary

| | Easy | Normal | Hard | Hell |
|---|---|---|---|---|
| **Shooting** | Large deviation, slow release | Slightly better | Very accurate, very fast | Perfect accuracy |
| **Dunking** | 40% success rate | 45% | 70% | 98% |
| **Stealing** | No steals at all | Gentle | Aggressive | Insane (high distance + high probability) |
| **Rebounding** | Slow reaction, low probability | Average | Aggressive | 100% must-rebound, 0 delay |
| **Dash** | Long cooldown, short range | Same as Easy | Faster and farther | Half cooldown, maximum range |
| **Pump Fake** | Easiest to fool (80%) | Easy to fool (70%) | 50/50 | Almost never fooled (10%) |
| **Ultimate** | 48s cooldown, no bonus | Same as Easy | 28s cooldown | 14s + half at start + 35% refund + extra skill |
| **Survivability** | Standard stun | Same as Easy | Same as Easy | Stun shortened by 35%, shield cooldown increased (for balance) |

**Core pattern:**

- Easy -> Normal relies mainly on SkillsData probabilities and delays to create the gap (DifficultyTuning is identical)
- Hard increases both layers at the same time
- Hell gets 11 exclusive "cheat" parameters: `Accuracy=0`, `StealRangeBonus`, `DashCooldownMultiplier`, `StunDurationMultiplier`, `OpeningSuperChargeFraction`, `NativeSuperRefundFraction`, `HasBonusSupers`, `BonusSuperDashCooldown`, `BonusShieldCooldown`, `JumpPump=0.1`, `MoveDelay=0.02`
