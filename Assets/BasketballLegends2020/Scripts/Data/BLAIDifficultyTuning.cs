namespace BasketballLegends2020
{
    public readonly struct BLAIDifficultyTuningProfile
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

        public BLAIDifficultyTuningProfile(
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

    public static class BLAIDifficultyTuning
    {
        private static readonly BLAIDifficultyTuningProfile Easy = new BLAIDifficultyTuningProfile(
            defenceContestDistance: 180f,
            stealBehindDistance: 80f,
            stealBasketDistance: 45f,
            holderSuperDashMinDistance: 90f,
            holderSuperDashMaxDistance: 460f,
            looseBallSuperDashDistance: 120f,
            attackPressureDistance: 140f,
            attackSuperDashDistance: 260f,
            dashBlockRangeMaxDistance: 180f);

        private static readonly BLAIDifficultyTuningProfile Normal = new BLAIDifficultyTuningProfile(
            defenceContestDistance: 180f,
            stealBehindDistance: 80f,
            stealBasketDistance: 45f,
            holderSuperDashMinDistance: 90f,
            holderSuperDashMaxDistance: 460f,
            looseBallSuperDashDistance: 120f,
            attackPressureDistance: 140f,
            attackSuperDashDistance: 260f,
            dashBlockRangeMaxDistance: 180f);

        private static readonly BLAIDifficultyTuningProfile Hard = new BLAIDifficultyTuningProfile(
            defenceContestDistance: 220f,
            stealBehindDistance: 110f,
            stealBasketDistance: 65f,
            holderSuperDashMinDistance: 70f,
            holderSuperDashMaxDistance: 520f,
            looseBallSuperDashDistance: 90f,
            attackPressureDistance: 180f,
            attackSuperDashDistance: 220f,
            dashBlockRangeMaxDistance: 220f);

        private static readonly BLAIDifficultyTuningProfile Hell = new BLAIDifficultyTuningProfile(
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

        public static BLAIDifficultyTuningProfile Get(BLAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BLAiDifficulty.Easy:
                    return Easy;
                case BLAiDifficulty.Hard:
                    return Hard;
                case BLAiDifficulty.Hell:
                    return Hell;
                default:
                    return Normal;
            }
        }
    }
}
