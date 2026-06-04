// AI 难度调节参数
// 根据难度等级（简单、普通、困难、地狱）调整电脑对手的表现：反应速度、投篮准确度、防守积极性等。难度越高，电脑越厉害。

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
        /// Create a new AI difficulty profile with all tuning values for one difficulty level.
        /// </summary>
        /// <param name="defenceContestDistance">How close the AI needs to be to contest a shot on defence.</param>
        /// <param name="stealBehindDistance">Max distance for a steal attempt when behind the ball carrier.</param>
        /// <param name="stealBasketDistance">Max distance for a steal attempt near the basket.</param>
        /// <param name="holderSuperDashMinDistance">Minimum distance the ball carrier must travel before a super dash is allowed.</param>
        /// <param name="holderSuperDashMaxDistance">Maximum range the AI will use a super dash while holding the ball.</param>
        /// <param name="looseBallSuperDashDistance">How far the AI will super dash to grab a loose ball.</param>
        /// <param name="attackPressureDistance">How close the AI gets before it starts pressuring on offence.</param>
        /// <param name="attackSuperDashDistance">Max distance for an offensive super dash toward the basket.</param>
        /// <param name="dashBlockRangeMaxDistance">How close the AI must be to attempt a dash block.</param>
        /// <param name="dashCooldownMultiplier">Multiplier on dash cooldown time (lower = faster dashes).</param>
        /// <param name="stealRangeBonus">Extra range added to the AI's steal attempts.</param>
        /// <param name="stunDurationMultiplier">Multiplier on how long the AI stays stunned (lower = recovers faster).</param>
        /// <param name="openingSuperChargeFraction">How much super meter the AI starts with at the beginning of a match.</param>
        /// <param name="nativeSuperRefundFraction">Fraction of super meter refunded after the AI uses a skill.</param>
        /// <param name="bonusSuperDashCooldown">Extra cooldown (in seconds) added to the AI's super dash.</param>
        /// <param name="bonusSuperDashBossCooldown">Extra super dash cooldown used in boss-level encounters.</param>
        /// <param name="bonusShieldCooldown">Extra cooldown (in seconds) added to the AI's shield skill.</param>
        /// <param name="bonusShieldBossCooldown">Extra shield cooldown used in boss-level encounters.</param>
        /// <param name="hasBonusSupers">Whether this difficulty level grants the AI bonus super abilities.</param>
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
        /// Return the tuning profile for the given difficulty level. Falls back to Normal if unknown.
        /// </summary>
        /// <param name="difficulty">The difficulty level to look up (Easy, Normal, Hard, or Hell).</param>
        /// <returns>The matching AI difficulty tuning profile.</returns>
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
