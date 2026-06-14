# AI 难度参数全表（SkillsData + DifficultyTuning 融合）

> 对应脚本：`mlpAISkillsData.cs`（操作水平层）+ `mlpAIDifficultyTuning.cs`（身体素质层）
>
> 两层分离：**SkillsData** = AI 的"大脑"（概率/时机，决定想不想做），**DifficultyTuning** = AI 的"身体"（距离/范围，决定够不够得着）
>
> 共 38 个参数，覆盖 13 个篮球行为类别。

---

## 一、投篮 Shooting

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 1 | `Accuracy` | SkillsData | 数值叠加：越低越准，0=百发百中 | 0.14 | 0.12 | 0.04 | **0** |
| 2 | `Attack` | SkillsData | 延迟计时：起跳后到出手的延迟(秒)，越小越快 | 0.4 | 0.4 | 0.3 | **0.02** |
| 3 | `AttackAtOnce` | SkillsData | 概率掷骰：接球后立刻投篮的概率 | 20% | 30% | 60% | **100%** |
| 4 | `JumpThrow` | SkillsData | 概率掷骰：①身后有人抢先起跳 ②对手起跳时干扰起跳 | 30% | 40% | 100% | **100%** |
| 5 | `DefenceContestDistance` | DifficultyTuning | 距离：对手投篮时 AI 多远内会起跳干扰 | 180 | 180 | 220 | **260** |

## 二、扣篮 Dunking

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 6 | `ChanceToCompleteDunk` | SkillsData | 概率掷骰：扣篮出手后掷骰判定是否成功 | 40% | 45% | 70% | **98%** |

## 三、抢断 Stealing

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 7 | `MakeSteal` | SkillsData | 概率掷骰：距离条件满足后，掷骰决定是否出手 | 30% | 40% | 70% | **90%** |
| 8 | `DelaySteal` | SkillsData | 延迟计时：两次抢断之间的冷却间隔(秒) | 3.0 | 2.5 | 1.0 | **0.55** |
| 9 | `StealBehindDistance` | DifficultyTuning | 距离：身后追防时触发抢断判定的最大距离 | 80 | 80 | 110 | **140** |
| 10 | `StealBasketDistance` | DifficultyTuning | 距离：对手靠近篮筐时触发抢断的最大距离 | 45 | 45 | 65 | **90** |
| 11 | `StealRangeBonus` | DifficultyTuning | 数值叠加：抢断判定距离的额外加成 | 0 | 0 | 0 | **+20** |

> Easy 难度完全不抢断（`TryToSteal()` 第一行就 `return`），参数 7-11 对 Easy 实际无效。

## 四、盖帽/封盖 Blocking

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 12 | `MakeBlock` | SkillsData | 概率掷骰：对手从身后冲刺时起跳盖帽的概率 | 20% | 30% | 50% | **100%** |
| 13 | `Defence` | SkillsData | 延迟计时：对手起跳后 AI 起跳干扰的延迟(秒) | 0.5 | 0.5 | 0.3 | **0.02** |
| 14 | `DashBlockRangeMaxDistance` | DifficultyTuning | 距离：冲刺封盖的最远判定距离 | 180 | 180 | 220 | **280** |

## 五、篮板 Rebounding

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 15 | `ChanceToRebound` | SkillsData | 概率掷骰：球在篮板区域时起跳争抢的概率 | 30% | 35% | 70% | **100%** |
| 16 | `ReboundFixed` | SkillsData | 延迟计时：篮板起跳的固定等待时间(秒) | 0.35 | 0.3 | 0.2 | **0** |
| 17 | `ReboundRange` | SkillsData | 延迟计时：篮板起跳的随机浮动范围(秒) | 0.1 | 0.1 | 0.1 | 0.1 |

## 六、冲刺 Dash

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 18 | `MakeDash` | SkillsData | 概率掷骰：ReactOnOpponent 通过后的二级判定 | 100% | 100% | 100% | **100%** |
| 19 | `DelayDash` | SkillsData | 延迟计时：两次冲刺决策的冷却间隔(秒) | 5.0 | 4.5 | 3.0 | **1.0** |
| 20 | `HolderSuperDashMinDistance` | DifficultyTuning | 距离：持球冲刺触发所需的最短跑动距离 | 90 | 90 | 70 | **40** |
| 21 | `HolderSuperDashMaxDistance` | DifficultyTuning | 距离：持球冲刺的有效范围上限 | 460 | 460 | 520 | **620** |
| 22 | `LooseBallSuperDashDistance` | DifficultyTuning | 距离：自由球时冲刺抢球的最大距离 | 120 | 120 | 90 | **60** |
| 23 | `AttackSuperDashDistance` | DifficultyTuning | 距离：向篮筐进攻冲刺的最大距离 | 260 | 260 | 220 | **150** |
| 24 | `DashCooldownMultiplier` | DifficultyTuning | 倍率：冲刺冷却时间的缩放系数 | 1.0 | 1.0 | 1.0 | **0.6** |
| 25 | `BonusSuperDashCooldown` | DifficultyTuning | 延迟计时：额外冲刺冷却(秒)，平衡参数 | 0 | 0 | 0 | **+10** |

> `MakeDash` 四档都是 100%，实际差异由 `DelayDash` 和 `DashCooldownMultiplier` 体现。

## 七、防守反应 Defence Reaction

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 26 | `ReactOnOpponent` | SkillsData | 概率掷骰：身后有防守者时做出反应（变向或冲刺） | 30% | 40% | 60% | **100%** |

## 八、闪避 Avoid Steal

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 27 | `AvoidSteal` | SkillsData | 概率掷骰：对手抢断时尝试闪避（冲刺/跳跃/侧移） | 20% | 30% | 60% | **95%** |

## 九、假动作 Pump Fake

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 28 | `MakePump` | SkillsData | 概率掷骰：进攻时做假动作的概率（死代码，未接入） | 50% | 50% | 50% | 50% |
| 29 | `JumpPump` | SkillsData | 概率掷骰：被对手假动作骗到而起跳的概率 | 80% | 70% | 50% | **10%** |

## 十、跳球 Jump Ball

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 30 | `JumpBall` | SkillsData | 延迟计时：跳球起跳时机，值越小越早起跳 | 0.45 | 0.45 | 0.4 | **0.1** |

## 十一、移动 Movement

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 31 | `MoveDelay` | SkillsData | 延迟计时：攻防移动决策间隔(秒)，越小越敏捷 | 0.1 | 0.08 | 0.05 | **0.02** |
| 32 | `AttackPressureDistance` | DifficultyTuning | 距离：进攻时开始对防守方施压的距离 | 140 | 140 | 180 | **240** |

## 十二、超能力/必杀技 Super/Ultimate

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 33 | `CoolDown` | SkillsData | 延迟计时：超能力充能时间(秒) | 48 | 48 | 28 | **14** |
| 34 | `OpeningSuperChargeFraction` | DifficultyTuning | 比例：开局拥有的必杀技能量比例 | 0 | 0 | 0 | **55%** |
| 35 | `NativeSuperRefundFraction` | DifficultyTuning | 比例：使用必杀技后返还的能量比例 | 0 | 0 | 0 | **35%** |
| 36 | `HasBonusSupers` | DifficultyTuning | 布尔：是否解锁额外必杀技 | ❌ | ❌ | ❌ | **✅** |

## 十三、生存/韧性 Survivability

| # | 参数 | 来源 | 机制 | Easy | Normal | Hard | Hell |
|---|------|------|------|------|--------|------|------|
| 37 | `StunDurationMultiplier` | DifficultyTuning | 倍率：被眩晕持续时间缩放 | 1.0 | 1.0 | 1.0 | **0.65** |
| 38 | `BonusShieldCooldown` | DifficultyTuning | 延迟计时：护盾技能的额外冷却(秒)，平衡参数 | 0 | 0 | 0 | **+24** |

---

## 四种机制说明

| 机制 | 原理 | 典型参数 |
|------|------|----------|
| **概率掷骰** | 条件满足后 `Random.value < 参数` 决定是否执行 | `MakeSteal`、`ChanceToCompleteDunk` |
| **延迟计时** | 传入各种 Delay 计时器，控制动作间隔/反应延迟 | `Attack`、`Defence`、`DelaySteal` |
| **距离判定** | 控制 AI 行为判定的空间范围 | `StealBehindDistance`、`DefenceContestDistance` |
| **倍率叠加** | 对基础值的缩放系数或直接加减 | `StunDurationMultiplier`、`StealRangeBonus` |

> 实际行为都是**"计时器门控 + 概率掷骰"的组合模式**，没有单机制决定的行为。

---

## 四档难度设计思路总结

| | Easy | Normal | Hard | Hell |
|---|---|---|---|---|
| **投篮** | 偏差大、出手慢 | 稍好 | 很准、很快 | 百发百中 |
| **扣篮** | 40% 成功率 | 45% | 70% | 98% |
| **抢断** | 完全不抢 | 温和 | 积极 | 疯狂（距离+概率双高） |
| **篮板** | 反应慢、概率低 | 一般 | 积极 | 100% 必抢、0 延迟 |
| **冲刺** | 冷却长、范围小 | 同 Easy | 更快更远 | 冷却减半、范围最大 |
| **假动作** | 最容易被骗(80%) | 容易被骗(70%) | 一半概率(50%) | 几乎不吃晃(10%) |
| **必杀技** | 48s 冷却、无加成 | 同 Easy | 28s 冷却 | 14s + 开局半管 + 返还35% + 额外技能 |
| **韧性** | 标准眩晕 | 同 Easy | 同 Easy | 眩晕缩短35%、护盾冷却加长(平衡) |

**核心规律：**

- Easy → Normal 主要靠 SkillsData 的概率和延迟拉开差距（DifficultyTuning 完全相同）
- Hard 两层同时加码
- Hell 独享 11 个专属"作弊"参数：`Accuracy=0`、`StealRangeBonus`、`DashCooldownMultiplier`、`StunDurationMultiplier`、`OpeningSuperChargeFraction`、`NativeSuperRefundFraction`、`HasBonusSupers`、`BonusSuperDashCooldown`、`BonusShieldCooldown`、`JumpPump=0.1`、`MoveDelay=0.02`
