// AI difficulty adjustment parameters AI "physical fitness"
// Adjust the computer opponent's performance according to the difficulty level (easy, normal, hard, hell): reaction speed, shooting accuracy, defensive enthusiasm, etc. The higher the difficulty, the more powerful the computer.

namespace mlp
{
    /// <summary>
    /// AI difficulty adjustment parameters: Define various behavioral distances and probabilities of AI under a certain difficulty level, such as defensive distance, tackling distance, sprinting distance, etc.

    /// </summary>
    public readonly struct mlpAIDifficultyTuningProfile
    {
        // ===== Defense parameters =====

        /// <summary>Defensive interference distance: The range within which the AI ​​will attempt to interfere/block shots when the opponent shoots.
        /// The larger the value → the wider the AI ​​defense coverage → the higher the difficulty. For example Easy=180, Hell=260. </summary>
        public readonly float DefenceContestDistance;

        /// <summary>Stealing distance from behind: When the AI ​​is behind the ball holder, the range within which it will attempt to steal.
        /// The larger the value → the easier it is for the AI ​​to steal from behind → the higher the difficulty. For example Easy=80, Hell=140. </summary>
        public readonly float StealBehindDistance;

        /// <summary>Stealing distance under the basket: When the AI ​​is near the basket, the range within which it will attempt to steal.
        /// The larger the value → the more aggressive the AI ​​will defend near the basket → the higher the difficulty. For example Easy=45, Hell=90. </summary>
        public readonly float StealBasketDistance;

        // ===== Sprint parameters =====

        /// <summary>Minimum distance for ball carrier super sprint: The minimum distance the AI ​​holding the ball must travel to trigger a super sprint.
        /// The smaller the value → the easier it is for the AI ​​to trigger sprint → the higher the difficulty. For example Easy=90, Hell=40. </summary>
        public readonly float HolderSuperDashMinDistance;

        /// <summary>Maximum super sprint distance of the ball holder: the upper limit of the effective range of the super sprint used by the ball holding AI.
        /// The larger the value → the farther the AI ​​sprint covers → the higher the difficulty. For example Easy=460, Hell=620. </summary>
        public readonly float HolderSuperDashMaxDistance;

        /// <summary>Free ball super sprint distance: The maximum distance the AI ​​can sprint to grab the ball when the ball is uncontrolled.
        /// The smaller the value → the earlier the AI ​​starts sprinting to grab the ball (the faster the reaction) → the higher the difficulty. For example Easy=120, Hell=60. </summary>
        public readonly float LooseBallSuperDashDistance;

        // ===== Offensive parameters =====


        /// <summary>Offensive pressure distance: When the AI attacks, the range within which it starts to exert pressure on the defender.
        /// The larger the value → the earlier the AI ​​starts to apply pressure when attacking → the higher the difficulty. For example Easy=140, Hell=240. </summary>
        public readonly float AttackPressureDistance;

        /// <summary>Offensive Super Sprint Distance: The maximum distance the AI ​​can launch an offensive super sprint to the basket.
        /// The smaller the value → the more the AI ​​prefers precision sprinting at close range (more efficient) → the higher the difficulty. For example Easy=260, Hell=150. </summary>
        public readonly float AttackSuperDashDistance;

        /// <summary>Maximum sprint block distance: The furthest distance the AI ​​attempts to block a shot with a sprint action.
        /// The larger the value → the wider the AI ​​blocking coverage → the higher the difficulty. For example Easy=180, Hell=280. </summary>
        public readonly float DashBlockRangeMaxDistance;

        // ===== Multiplier/bonus parameters (enabled only in high difficulty) =====

        /// <summary>Sprint Cooldown Multiplier: Scaling factor for the AI's cooldown after sprinting (based on the default cooldown).
        /// The smaller the value → the shorter the cooldown → the more frequently the AI ​​sprints → the higher the difficulty. Default=1.0, Hell=0.6. </summary>
        public readonly float DashCooldownMultiplier;

        /// <summary>Stealing range bonus: additional increment of AI stealing judgment distance (directly added to the base).
        /// The larger the value → the larger the AI ​​steal judgment range → the higher the difficulty. Default=0, Hell=20. </summary>
        public readonly float StealRangeBonus;

        /// <summary>Stun duration multiplier: The duration scaling factor when the AI ​​is stunned (based on the default stun time).
        /// The smaller the value → the faster the AI ​​recovery → the higher the difficulty. Default=1.0, Hell=0.65. </summary>
        public readonly float StunDurationMultiplier;

        // ===== Special skills/energy parameters (enabled only in high difficulty) =====

        /// <summary>The proportion of the sure-kill skills at the beginning: the percentage of the sure-kill skills owned by the AI ​​at the beginning of the game to the full slot.
        /// The larger the value → the AI ​​starts with more energy → the higher the difficulty. Default=0, Hell=0.55 (above half pipe at the beginning). </summary>
        public readonly float OpeningSuperChargeFraction;

        /// <summary>Special skill energy return ratio: After the AI ​​uses a special skill, the energy returned accounts for the percentage of consumption.
        /// The larger the value → the AI ​​can accumulate the next special move faster → the higher the difficulty. Default=0, Hell=0.35. </summary>
        public readonly float NativeSuperRefundFraction;

        /// <summary>Extra Super Sprint Cooldown (seconds): The additional cooldown time of AI Super Sprint on top of the base cooldown.
        /// The larger the value → the longer the AI ​​sprint interval → the lower the difficulty (this is a balance parameter that limits high-difficulty AI). Default=0, Hell=10. </summary>
        public readonly float BonusSuperDashCooldown;

        /// <summary>Extra shield cooldown (seconds): The additional cooldown time of the AI ​​shield skill on top of the base cooldown.
        /// The larger the value → the less frequently the AI ​​shield is used → the lower the difficulty (the same as the balance parameter). Default=0, Hell=24. </summary>
        public readonly float BonusShieldCooldown;

        /// <summary>Whether it has additional special skills: whether high-difficulty AI unlocks additional special skills.
        /// true → AI has more skill options → more difficult. Default=false, Hell=true. </summary>
        public readonly bool HasBonusSupers;

        /// <summary>
        /// Creates a new AI difficulty profile that contains all modifiers for a difficulty level.
        /// </summary>
        /// <param name="defenceContestDistance">The distance required for the AI ​​to interfere with shots when defending. </param>
        /// <param name="stealBehindDistance">The maximum distance behind the ball handler to attempt a steal. </param>
        /// <param name="stealBasketDistance">Maximum distance near the basket for attempted steals. </param>
        /// <param name="holderSuperDashMinDistance">The shortest distance the ball carrier must travel to use Super Dash. </param>
        /// <param name="holderSuperDashMaxDistance">The maximum range of the AI's super dash when holding the ball. </param>
        /// <param name="looseBallSuperDashDistance">The maximum distance the AI ​​can sprint to grab a free ball. </param>
        /// <param name="attackPressureDistance">The approach distance at which the AI ​​starts to apply pressure when attacking. </param>
        /// <param name="attackSuperDashDistance">The maximum distance for an offensive super dash to the basket. </param>
        /// <param name="dashBlockRangeMaxDistance">The maximum distance the AI ​​attempts to sprint to block. </param>
        /// <param name="dashCooldownMultiplier">Multiplier for dash cooldown (lower = faster dash). </param>
        /// <param name="stealRangeBonus">Extra range bonus for AI steals. </param>
        /// <param name="stunDurationMultiplier">The multiplier for the duration the AI ​​is stunned (lower = faster recovery). </param>
        /// <param name="openingSuperChargeFraction">The ratio of the amount of special kills the AI ​​has at the start of the match. </param>
        /// <param name="nativeSuperRefundFraction">The proportion of the amount of special skills returned by the AI ​​after using skills. </param>
        /// <param name="bonusSuperDashCooldown">Bonus cooldown for AI Super Dash in seconds. </param>
        /// <param name="bonusShieldCooldown">Bonus cooldown for AI shield skills in seconds. </param>
        /// <param name="hasBonusSupers">Whether this difficulty level gives the AI ​​additional special kill skills. </param>
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
    /// AI difficulty adjuster: Returns the corresponding adjustment parameters according to the selected difficulty level (easy/normal/hard/hell).
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
        /// Get the adjustment configuration for the specified difficulty level. If the difficulty is unknown, fall back to normal difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level to find (Easy, Normal, Hard, or Hell). </param>
        /// <returns>Matching AI difficulty adjustment configuration. </returns>
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
