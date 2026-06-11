// AI 难度调节参数  AI 的"身体素质"
// 根据难度等级（简单、普通、困难、地狱）调整电脑对手的表现：反应速度、投篮准确度、防守积极性等。难度越高，电脑越厉害。

namespace mlp
{
    /// <summary>
    /// AI 难度调节参数：定义某个难度等级下 AI 的各种行为距离和概率，比如防守距离、抢断距离、冲刺距离等。
    /// </summary>
    public readonly struct mlpAIDifficultyTuningProfile
    {
        // ===== 防守类参数 =====

        /// <summary>防守干扰距离：AI 在对手投篮时，多大范围内会尝试干扰/封盖。
        /// 值越大 → AI 防守覆盖范围越广 → 难度越高。例如 Easy=180, Hell=260。</summary>
        public readonly float DefenceContestDistance;

        /// <summary>身后抢断距离：AI 在持球人身后时，多大范围内会尝试抢断。
        /// 值越大 → AI 从背后抢断越容易 → 难度越高。例如 Easy=80, Hell=140。</summary>
        public readonly float StealBehindDistance;

        /// <summary>篮下抢断距离：AI 在篮筐附近时，多大范围内会尝试抢断。
        /// 值越大 → AI 在篮筐附近防守越积极 → 难度越高。例如 Easy=45, Hell=90。</summary>
        public readonly float StealBasketDistance;

        // ===== 冲刺类参数 =====

        /// <summary>持球人超级冲刺最短距离：持球的 AI 必须跑过至少多远才能触发超级冲刺。
        /// 值越小 → AI 越容易触发冲刺 → 难度越高。例如 Easy=90, Hell=40。</summary>
        public readonly float HolderSuperDashMinDistance;

        /// <summary>持球人超级冲刺最大距离：持球 AI 使用超级冲刺的有效范围上限。
        /// 值越大 → AI 冲刺覆盖范围越远 → 难度越高。例如 Easy=460, Hell=620。</summary>
        public readonly float HolderSuperDashMaxDistance;

        /// <summary>自由球超级冲刺距离：球处于无人控制状态时，AI 冲刺抢球的最大距离。
        /// 值越小 → AI 越早开始冲刺抢球（反应越快）→ 难度越高。例如 Easy=120, Hell=60。</summary>
        public readonly float LooseBallSuperDashDistance;

        // ===== 进攻类参数 =====

        /// <summary>进攻施压距离：AI 进攻时，多大范围内开始对防守方施加压力。
        /// 值越大 → AI 进攻时越早开始施压 → 难度越高。例如 Easy=140, Hell=240。</summary>
        public readonly float AttackPressureDistance;

        /// <summary>进攻超级冲刺距离：AI 向篮筐发起进攻性超级冲刺的最大距离。
        /// 值越小 → AI 越倾向于近距离精确冲刺（更高效）→ 难度越高。例如 Easy=260, Hell=150。</summary>
        public readonly float AttackSuperDashDistance;

        /// <summary>冲刺封盖最远距离：AI 尝试用冲刺动作封盖投篮的最远距离。
        /// 值越大 → AI 封盖覆盖范围越广 → 难度越高。例如 Easy=180, Hell=280。</summary>
        public readonly float DashBlockRangeMaxDistance;

        // ===== 倍率/加成类参数（仅高难度启用） =====

        /// <summary>冲刺冷却倍率：AI 冲刺后冷却时间的缩放系数（基于默认冷却时间）。
        /// 值越小 → 冷却越短 → AI 冲刺越频繁 → 难度越高。默认=1.0，Hell=0.6。</summary>
        public readonly float DashCooldownMultiplier;

        /// <summary>抢断范围加成：AI 抢断判定距离的额外增量（直接加到基础上）。
        /// 值越大 → AI 抢断判定范围越大 → 难度越高。默认=0，Hell=20。</summary>
        public readonly float StealRangeBonus;

        /// <summary>眩晕持续时间倍率：AI 被眩晕时的持续时间缩放系数（基于默认眩晕时间）。
        /// 值越小 → AI 恢复越快 → 难度越高。默认=1.0，Hell=0.65。</summary>
        public readonly float StunDurationMultiplier;

        // ===== 必杀技/能量类参数（仅高难度启用） =====

        /// <summary>开局必杀技能量比例：比赛开始时 AI 拥有的必杀技能量占满槽的百分比。
        /// 值越大 → AI 开局就有更多能量 → 难度越高。默认=0，Hell=0.55（开局半管以上）。</summary>
        public readonly float OpeningSuperChargeFraction;

        /// <summary>必杀技能量返还比例：AI 使用必杀技后，返还的能量占消耗量的百分比。
        /// 值越大 → AI 能更快攒出下一个必杀技 → 难度越高。默认=0，Hell=0.35。</summary>
        public readonly float NativeSuperRefundFraction;

        /// <summary>额外超级冲刺冷却（秒）：AI 超级冲刺在基础冷却之上额外增加的冷却时间。
        /// 值越大 → AI 冲刺间隔越长 → 难度越低（此为限制高难度 AI 的平衡参数）。默认=0，Hell=10。</summary>
        public readonly float BonusSuperDashCooldown;

        /// <summary>额外护盾冷却（秒）：AI 护盾技能在基础冷却之上额外增加的冷却时间。
        /// 值越大 → AI 护盾使用频率越低 → 难度越低（同为平衡参数）。默认=0，Hell=24。</summary>
        public readonly float BonusShieldCooldown;

        /// <summary>是否拥有额外必杀技：高难度 AI 是否解锁额外的必杀技能力。
        /// true → AI 拥有更多技能选项 → 难度更高。默认=false，Hell=true。</summary>
        public readonly bool HasBonusSupers;

        /// <summary>
        /// 创建一个新的 AI 难度配置，包含某个难度等级的所有调节数值。
        /// </summary>
        /// <param name="defenceContestDistance">防守时 AI 干扰投篮所需的距离。</param>
        /// <param name="stealBehindDistance">在持球人身后尝试抢断的最大距离。</param>
        /// <param name="stealBasketDistance">在篮筐附近尝试抢断的最大距离。</param>
        /// <param name="holderSuperDashMinDistance">持球人必须跑过的最短距离才能使用超级冲刺。</param>
        /// <param name="holderSuperDashMaxDistance">持球状态下 AI 使用超级冲刺的最大范围。</param>
        /// <param name="looseBallSuperDashDistance">AI 冲刺抢夺自由球的最大距离。</param>
        /// <param name="attackPressureDistance">进攻时 AI 开始施压的接近距离。</param>
        /// <param name="attackSuperDashDistance">向篮筐发起进攻性超级冲刺的最大距离。</param>
        /// <param name="dashBlockRangeMaxDistance">AI 尝试冲刺封盖的最远距离。</param>
        /// <param name="dashCooldownMultiplier">冲刺冷却时间的倍率（越低 = 冲刺越快）。</param>
        /// <param name="stealRangeBonus">AI 抢断的额外距离加成。</param>
        /// <param name="stunDurationMultiplier">AI 被眩晕持续时间的倍率（越低 = 恢复越快）。</param>
        /// <param name="openingSuperChargeFraction">比赛开始时 AI 拥有的必杀技能量比例。</param>
        /// <param name="nativeSuperRefundFraction">AI 使用技能后返还的必杀技能量比例。</param>
        /// <param name="bonusSuperDashCooldown">AI 超级冲刺的额外冷却时间（秒）。</param>
        /// <param name="bonusShieldCooldown">AI 护盾技能的额外冷却时间（秒）。</param>
        /// <param name="hasBonusSupers">该难度等级是否赋予 AI 额外的必杀技能力。</param>
        public mlpAIDifficultyTuningProfile(
            float defenceContestDistance,
            float stealBehindDistance,
            float stealBasketDistance,
            float holderSuperDashMinDistance,
            float holderSuperDashMaxDistance,
            float looseBallSuperDashDistance,
            float attackPressureDistance,
            float attackSuperDashDistance,
            float dashBlockRangeMaxDistance,
            float dashCooldownMultiplier = 1f,
            float stealRangeBonus = 0f,
            float stunDurationMultiplier = 1f,
            float openingSuperChargeFraction = 0f,
            float nativeSuperRefundFraction = 0f,
            float bonusSuperDashCooldown = 0f,
            float bonusShieldCooldown = 0f,
            bool hasBonusSupers = false)
        {
            DefenceContestDistance = defenceContestDistance;
            StealBehindDistance = stealBehindDistance;
            StealBasketDistance = stealBasketDistance;
            HolderSuperDashMinDistance = holderSuperDashMinDistance;
            HolderSuperDashMaxDistance = holderSuperDashMaxDistance;
            LooseBallSuperDashDistance = looseBallSuperDashDistance;
            AttackPressureDistance = attackPressureDistance;
            AttackSuperDashDistance = attackSuperDashDistance;
            DashBlockRangeMaxDistance = dashBlockRangeMaxDistance;
            DashCooldownMultiplier = dashCooldownMultiplier;
            StealRangeBonus = stealRangeBonus;
            StunDurationMultiplier = stunDurationMultiplier;
            OpeningSuperChargeFraction = openingSuperChargeFraction;
            NativeSuperRefundFraction = nativeSuperRefundFraction;
            BonusSuperDashCooldown = bonusSuperDashCooldown;
            BonusShieldCooldown = bonusShieldCooldown;
            HasBonusSupers = hasBonusSupers;
        }
    }

    /// <summary>
    /// AI 难度调节器：根据选择的难度等级（简单/普通/困难/地狱）返回对应的调节参数。
    /// </summary>
    public static class mlpAIDifficultyTuning
    {
        private static readonly mlpAIDifficultyTuningProfile Easy = new mlpAIDifficultyTuningProfile(
            defenceContestDistance: 180f,
            stealBehindDistance: 80f,
            stealBasketDistance: 45f,
            holderSuperDashMinDistance: 90f,
            holderSuperDashMaxDistance: 460f,
            looseBallSuperDashDistance: 120f,
            attackPressureDistance: 140f,
            attackSuperDashDistance: 260f,
            dashBlockRangeMaxDistance: 180f);

        private static readonly mlpAIDifficultyTuningProfile Normal = new mlpAIDifficultyTuningProfile(
            defenceContestDistance: 180f,
            stealBehindDistance: 80f,
            stealBasketDistance: 45f,
            holderSuperDashMinDistance: 90f,
            holderSuperDashMaxDistance: 460f,
            looseBallSuperDashDistance: 120f,
            attackPressureDistance: 140f,
            attackSuperDashDistance: 260f,
            dashBlockRangeMaxDistance: 180f);

        private static readonly mlpAIDifficultyTuningProfile Hard = new mlpAIDifficultyTuningProfile(
            defenceContestDistance: 220f,
            stealBehindDistance: 110f,
            stealBasketDistance: 65f,
            holderSuperDashMinDistance: 70f,
            holderSuperDashMaxDistance: 520f,
            looseBallSuperDashDistance: 90f,
            attackPressureDistance: 180f,
            attackSuperDashDistance: 220f,
            dashBlockRangeMaxDistance: 220f);

        private static readonly mlpAIDifficultyTuningProfile Hell = new mlpAIDifficultyTuningProfile(
            defenceContestDistance: 260f,
            stealBehindDistance: 140f,
            stealBasketDistance: 90f,
            holderSuperDashMinDistance: 40f,
            holderSuperDashMaxDistance: 620f,
            looseBallSuperDashDistance: 60f,
            attackPressureDistance: 240f,
            attackSuperDashDistance: 150f,
            dashBlockRangeMaxDistance: 280f,
            dashCooldownMultiplier: 0.6f,
            stealRangeBonus: 20f,
            stunDurationMultiplier: 0.65f,
            openingSuperChargeFraction: 0.55f,
            nativeSuperRefundFraction: 0.35f,
            bonusSuperDashCooldown: 10f,
            bonusShieldCooldown: 24f,
            hasBonusSupers: true);

        /// <summary>
        /// 获取指定难度等级的调节配置。如果难度未知，回退到普通难度。
        /// </summary>
        /// <param name="difficulty">要查找的难度等级（Easy、Normal、Hard 或 Hell）。</param>
        /// <returns>匹配的 AI 难度调节配置。</returns>
        public static mlpAIDifficultyTuningProfile Get(mlpAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case mlpAiDifficulty.Easy:
                    return Easy;
                case mlpAiDifficulty.Hard:
                    return Hard;
                case mlpAiDifficulty.Hell:
                    return Hell;
                default:
                    return Normal;
            }
        }
    }
}
