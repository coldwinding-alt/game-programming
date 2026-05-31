// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushAIDifficultyTuning 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

namespace rimrush
{
    public readonly struct rimrushAIDifficultyTuningProfile
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
        /// Executes rimrush AIDifficulty Tuning Profile for the rimrushAIDifficultyTuning workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="defenceContestDistance">Input value used by this step of the workflow.</param>
        /// <param name="stealBehindDistance">Input value used by this step of the workflow.</param>
        /// <param name="stealBasketDistance">Input value used by this step of the workflow.</param>
        /// <param name="holderSuperDashMinDistance">Input value used by this step of the workflow.</param>
        /// <param name="holderSuperDashMaxDistance">Input value used by this step of the workflow.</param>
        /// <param name="looseBallSuperDashDistance">Input value used by this step of the workflow.</param>
        /// <param name="attackPressureDistance">Input value used by this step of the workflow.</param>
        /// <param name="attackSuperDashDistance">Input value used by this step of the workflow.</param>
        /// <param name="dashBlockRangeMaxDistance">Input value used by this step of the workflow.</param>
        /// <param name="dashCooldownMultiplier">Input value used by this step of the workflow.</param>
        /// <param name="stealRangeBonus">Input value used by this step of the workflow.</param>
        /// <param name="stunDurationMultiplier">Input value used by this step of the workflow.</param>
        /// <param name="openingSuperChargeFraction">Input value used by this step of the workflow.</param>
        /// <param name="nativeSuperRefundFraction">Input value used by this step of the workflow.</param>
        /// <param name="bonusSuperDashCooldown">Input value used by this step of the workflow.</param>
        /// <param name="bonusSuperDashBossCooldown">Input value used by this step of the workflow.</param>
        /// <param name="bonusShieldCooldown">Input value used by this step of the workflow.</param>
        /// <param name="bonusShieldBossCooldown">Input value used by this step of the workflow.</param>
        /// <param name="hasBonusSupers">Input value used by this step of the workflow.</param>
        public rimrushAIDifficultyTuningProfile(
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

    public static class rimrushAIDifficultyTuning
    {
        private static readonly rimrushAIDifficultyTuningProfile Easy = new rimrushAIDifficultyTuningProfile(
            defenceContestDistance: 180f,
            stealBehindDistance: 80f,
            stealBasketDistance: 45f,
            holderSuperDashMinDistance: 90f,
            holderSuperDashMaxDistance: 460f,
            looseBallSuperDashDistance: 120f,
            attackPressureDistance: 140f,
            attackSuperDashDistance: 260f,
            dashBlockRangeMaxDistance: 180f);

        private static readonly rimrushAIDifficultyTuningProfile Normal = new rimrushAIDifficultyTuningProfile(
            defenceContestDistance: 180f,
            stealBehindDistance: 80f,
            stealBasketDistance: 45f,
            holderSuperDashMinDistance: 90f,
            holderSuperDashMaxDistance: 460f,
            looseBallSuperDashDistance: 120f,
            attackPressureDistance: 140f,
            attackSuperDashDistance: 260f,
            dashBlockRangeMaxDistance: 180f);

        private static readonly rimrushAIDifficultyTuningProfile Hard = new rimrushAIDifficultyTuningProfile(
            defenceContestDistance: 220f,
            stealBehindDistance: 110f,
            stealBasketDistance: 65f,
            holderSuperDashMinDistance: 70f,
            holderSuperDashMaxDistance: 520f,
            looseBallSuperDashDistance: 90f,
            attackPressureDistance: 180f,
            attackSuperDashDistance: 220f,
            dashBlockRangeMaxDistance: 220f);

        private static readonly rimrushAIDifficultyTuningProfile Hell = new rimrushAIDifficultyTuningProfile(
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
        /// Executes Get for the rimrushAIDifficultyTuning workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="difficulty">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static rimrushAIDifficultyTuningProfile Get(rimrushAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case rimrushAiDifficulty.Easy:
                    return Easy;
                case rimrushAiDifficulty.Hard:
                    return Hard;
                case rimrushAiDifficulty.Hell:
                    return Hell;
                default:
                    return Normal;
            }
        }
    }
}
