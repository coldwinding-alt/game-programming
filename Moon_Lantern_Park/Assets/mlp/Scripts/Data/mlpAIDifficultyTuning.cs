// AI 难度调节参数
// 根据难度等级（简单、普通、困难、地狱）调整电脑对手的表现：反应速度、投篮准确度、防守积极性等。难度越高，电脑越厉害。

namespace mlp
{
    /// <summary>
    /// AI 难度调节参数：定义某个难度等级下 AI 的各种行为距离和概率，比如防守距离、抢断距离、冲刺距离等。
    /// </summary>
    public readonly struct mlpAIDifficultyTuningProfile
    {
        public readonly float DefenceContestDistance;
        public readonly float StealBehindDistance;
        public readonly float StealBasketDistance;
        public readonly float HolderSuperDashMinDistance;
        public readonly float HolderSuperDashMaxDistance;
        public readonly float LooseBallSuperDashDistance;
        public readonly float AttackPressureDistance;
        public readonly float AttackSuperDashDistance;
        public readonly float DashBlockRangeMaxDistance;
        public readonly float DashCooldownMultiplier;
        public readonly float StealRangeBonus;
        public readonly float StunDurationMultiplier;
        public readonly float OpeningSuperChargeFraction;
        public readonly float NativeSuperRefundFraction;
        public readonly float BonusSuperDashCooldown;
        public readonly float BonusSuperDashBossCooldown;
        public readonly float BonusShieldCooldown;
        public readonly float BonusShieldBossCooldown;
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
        /// <param name="bonusSuperDashBossCooldown">Boss 级对手超级冲刺的额外冷却时间。</param>
        /// <param name="bonusShieldCooldown">AI 护盾技能的额外冷却时间（秒）。</param>
        /// <param name="bonusShieldBossCooldown">Boss 级对手护盾技能的额外冷却时间。</param>
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
            float bonusSuperDashBossCooldown = 0f,
            float bonusShieldCooldown = 0f,
            float bonusShieldBossCooldown = 0f,
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
            BonusSuperDashBossCooldown = bonusSuperDashBossCooldown;
            BonusShieldCooldown = bonusShieldCooldown;
            BonusShieldBossCooldown = bonusShieldBossCooldown;
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
            bonusSuperDashBossCooldown: 8f,
            bonusShieldCooldown: 24f,
            bonusShieldBossCooldown: 18f,
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
