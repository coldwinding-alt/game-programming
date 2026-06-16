// AI skill parameter table AI "operation level"
// The single-player AI in the entire game has four levels: Easy/Normal/Hard/Hell. Each gear corresponds to a set of fixed parameters.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// AI skill parameters: Define the AI's shooting percentage, dunk success rate, cooling time, steal probability and other values ​​under a certain level of difficulty.
    /// </summary>
    public readonly struct mlpAISkillProfile
    {
        // Shooting accuracy. [Numerical Superposition] Insert the CalcDispersion() formula to calculate the flight deviation of the ball. The lower the ball, the more accurate it is. 0 = 100% hit.
        public readonly float Accuracy;
        // Dunk success rate. [Probability roll] After a dunk is made, Random.value < this value will be considered a dunk, otherwise the dunk will be missed.
        public readonly float ChanceToCompleteDunk;
        // Super power cooldown time. [Delay Timing] The number of seconds required for charging, dt is accumulated every frame, and super powers can be released only after it is full.
        public readonly float CoolDown;
        // Jump ball reaction speed. [Delay Timing] Pass in the NegativeDelay timer to control the timing of jumping. The smaller the value, the earlier the AI ​​will jump.
        public readonly float JumpBall;
        // Rebound probability. [Probability roll] When the ball is in the backboard area, it will only take off if Random.value < this value (it needs to be judged by the position and timer first)
        public readonly float ChanceToRebound;
        // Shot release is delayed. [Delay Timing] Pass in the FullDelay timer to control the delay between AI taking off and taking action. The smaller it is, the faster it will take action.
        public readonly float Attack;
        // Catch and shoot probability. [Probability roll] After landing and holding the ball, if Random.value < this value, you will immediately take off and shoot. Otherwise, dribble first and then make a decision.
        public readonly float AttackAtOnce;
        // Ability to dodge steals. [Probability roll] When the opponent initiates a steal, if Random.value < this value, the AI ​​will try to dodge (sprint/jump/sideways move)
        public readonly float AvoidSteal;
        // Probability of false action. [Unused] The original plan was to control the probability of AI making false moves when attacking. The current code is not connected and is a dead code.
        public readonly float MakePump;
        // Defensive reaction probability. [Probability roll] There is a defender behind you when attacking. If Random.value < this value, the AI ​​will react (change direction or sprint), otherwise it will ignore it and continue to rush.
        public readonly float ReactOnOpponent;
        // Dash dodge probability. [Probability roll] Secondary judgment after ReactOnOpponent passes. If Random.value < this value, use sprint to dodge, otherwise it will only change direction.
        public readonly float MakeDash;
        // Sprint cooldown interval. [Delay Timing] Pass in AIUseDelay to control the cooling seconds between two sprint decisions. The shorter the time, the more frequent the sprints will be.
        public readonly float DelayDash;
        // Defensive interference delay. [Delay Timing] Pass in SimpleDelay to control the delay of AI jump interference after the opponent jumps to shoot. The smaller the delay, the faster the reaction.
        public readonly float Defence;
        // Jump shot interference probability. [Probability Roll] Two purposes: (1) When holding the ball, if there is someone behind you, decide whether to take off first to shoot; (2) When the opponent takes off, decide whether to take off to interfere
        public readonly float JumpThrow;
        // Steal attempt probability. [Probability roll] Random.value < this value initiates a steal when close to the ball holder; probability × 1.5 when the opponent is close to the basket
        public readonly float MakeSteal;
        // Tackle cooldown interval. [Delay Timing] is added to the basic steal action duration to control the number of cooling seconds between two steal attempts.
        public readonly float DelaySteal;
        // The probability of being fooled by a fake move. [Probability roll] When the opponent makes a fake move, if Random.value < this value, the AI ​​will be tricked into jumping (the higher the value, the easier it is to be fooled)
        public readonly float JumpPump;
        // Block attempt probability. [Probability roll] When the opponent sprints from behind, if Random.value < this value, the AI ​​will try to take off and block the shot.
        public readonly float MakeBlock;
        // Fixed delay in rebounding. [Delay Timing] Pass in FullDelay as a fixed delay component to control the basic waiting time of the rebound timing.
        public readonly float ReboundFixed;
        // The backboard has a random floating range. [Delay Timing] Pass in FullDelay as a random floating component, which together with ReboundFixed determines the delay window for the rebound to take off.
        public readonly float ReboundRange;
        // Move decision interval. [Delay Timing] Pass in FullDelay to control the interval between the AI’s offensive and defensive movement decisions. The smaller the value, the more agile the AI ​​will move.
        public readonly float MoveDelay;

        public mlpAISkillProfile(
            float accuracy,
            float chanceToCompleteDunk,
            float coolDown,
            float jumpBall,
            float chanceToRebound,
            float attack,
            float attackAtOnce,
            float avoidSteal,
            float makePump,
            float reactOnOpponent,
            float makeDash,
            float delayDash,
            float defence,
            float jumpThrow,
            float makeSteal,
            float delaySteal,
            float jumpPump,
            float makeBlock,
            float reboundFixed,
            float reboundRange,
            float moveDelay)
        {
            Accuracy = accuracy;
            ChanceToCompleteDunk = chanceToCompleteDunk;
            CoolDown = coolDown;
            JumpBall = jumpBall;
            ChanceToRebound = chanceToRebound;
            Attack = attack;
            AttackAtOnce = attackAtOnce;
            AvoidSteal = avoidSteal;
            MakePump = makePump;
            ReactOnOpponent = reactOnOpponent;
            MakeDash = makeDash;
            DelayDash = delayDash;
            Defence = defence;
            JumpThrow = jumpThrow;
            MakeSteal = makeSteal;
            DelaySteal = delaySteal;
            JumpPump = jumpPump;
            MakeBlock = makeBlock;
            ReboundFixed = reboundFixed;
            ReboundRange = reboundRange;
            MoveDelay = moveDelay;
        }
    }

    /// <summary>
    /// AI skill parameter table: centralized management of four fixed difficulty levels: Easy, Normal, Hard, and Hell.

    /// The AI difficulty index here only has four values: 0, 1, 2, and 3:

    /// 0 = Easy，1 = Normal，2 = Hard，3 = Hell。
    /// Tournament rounds, adventure level numbers, and stage progression no longer generate additional hidden skill levels.

    /// </summary>
    public static class mlpAISkillsData
    {
        public const int EasySkillIndex = 0;
        public const int NormalSkillIndex = 1;
        public const int HardSkillIndex = 2;
        public const int HellSkillIndex = 3;

        // Basic feel parameters used by human players.

        // Note: This is not the fifth AI difficulty level, it is just to retain the original shooting, dunking and super power cooling feel of the player character.

        // The four difficulty levels of AI only read the Profiles array below.

        private static readonly mlpAISkillProfile HumanPlayerProfile = new mlpAISkillProfile(
            accuracy: 0.01f,
            chanceToCompleteDunk: 0.9f,
            coolDown: 18f,
            jumpBall: 0f,
            chanceToRebound: 0f,
            attack: 0f,
            attackAtOnce: 0f,
            avoidSteal: 0f,
            makePump: 0.5f,
            reactOnOpponent: 0f,
            makeDash: 0f,
            delayDash: 0f,
            defence: 0f,
            jumpThrow: 0f,
            makeSteal: 0f,
            delaySteal: 0f,
            jumpPump: 0f,
            makeBlock: 0f,
            reboundFixed: 0f,
            reboundRange: 0f,
            moveDelay: 0f);

        // Four levels of fixed tuning parameters for the current AI difficulty.

        // Each array element corresponds directly to a selectable difficulty level, and there are no longer intermediate levels or hidden increasing levels.

        // If you need to adjust the feel in the future, only change the four gears themselves and do not add stage/level offsets.

        private static readonly mlpAISkillProfile[] Profiles =
        {
            // Easy: Retains basic movements and reactions, but is more conservative in terms of offense, defense and dunk stability.

            new mlpAISkillProfile(
                accuracy: 0.14f,
                chanceToCompleteDunk: 0.4f,
                coolDown: 48f,
                jumpBall: 0.45f,
                chanceToRebound: 0.3f,
                attack: 0.4f,
                attackAtOnce: 0.2f,
                avoidSteal: 0.2f,
                makePump: 0.5f,
                reactOnOpponent: 0.3f,
                makeDash: 1f,
                delayDash: 5f,
                defence: 0.5f,
                jumpThrow: 0.3f,
                makeSteal: 0.3f,
                delaySteal: 3f,
                jumpPump: 0.8f,
                makeBlock: 0.2f,
                reboundFixed: 0.35f,
                reboundRange: 0.1f,
                moveDelay: 0.1f),
            // Normal: The default single-player experience, which is better at rebounding and defense than Easy, but still leaves room for players to react stably.

            new mlpAISkillProfile(
                accuracy: 0.12f,
                chanceToCompleteDunk: 0.45f,
                coolDown: 48f,
                jumpBall: 0.45f,
                chanceToRebound: 0.35f,
                attack: 0.4f,
                attackAtOnce: 0.3f,
                avoidSteal: 0.3f,
                makePump: 0.5f,
                reactOnOpponent: 0.4f,
                makeDash: 1f,
                delayDash: 4.5f,
                defence: 0.5f,
                jumpThrow: 0.4f,
                makeSteal: 0.4f,
                delaySteal: 2.5f,
                jumpPump: 0.7f,
                makeBlock: 0.3f,
                reboundFixed: 0.3f,
                reboundRange: 0.1f,
                moveDelay: 0.08f),
            // Hard: Significantly improves defensive pressure, steals and offensive execution, but will not continue to increase in the late stages of the tournament or adventure.
            new mlpAISkillProfile(
                accuracy: 0.04f,
                chanceToCompleteDunk: 0.7f,
                coolDown: 28f,
                jumpBall: 0.4f,
                chanceToRebound: 0.7f,
                attack: 0.3f,
                attackAtOnce: 0.6f,
                avoidSteal: 0.6f,
                makePump: 0.5f,
                reactOnOpponent: 0.6f,
                makeDash: 1f,
                delayDash: 3f,
                defence: 0.3f,
                jumpThrow: 1f,
                makeSteal: 0.7f,
                delaySteal: 1f,
                jumpPump: 0.5f,
                makeBlock: 0.5f,
                reboundFixed: 0.2f,
                reboundRange: 0.1f,
                moveDelay: 0.05f),
            // Hell: The highest basic parameters among the four levels; Hell’s exclusive additional enhancements are still handled by difficulty adjustment and controller logic.

            new mlpAISkillProfile(
                accuracy: 0f,
                chanceToCompleteDunk: 0.98f,
                coolDown: 14f,
                jumpBall: 0.1f,
                chanceToRebound: 1f,
                attack: 0.02f,
                attackAtOnce: 1f,
                avoidSteal: 0.95f,
                makePump: 0.5f,
                reactOnOpponent: 1f,
                makeDash: 1f,
                delayDash: 1f,
                defence: 0.02f,
                jumpThrow: 1f,
                makeSteal: 0.9f,
                delaySteal: 0.55f,
                jumpPump: 0.1f,
                makeBlock: 1f,
                reboundFixed: 0f,
                reboundRange: 0.1f,
                moveDelay: 0.02f)
        };

        public static int MaxSkillIndex => Profiles.Length - 1;

        /// <summary>
        /// Converts the four difficulty levels selected by the player into an internal AI skill index.

        /// All single player modes should go here first before writing the results to the match data.
        /// This way Quick Play, Random Play, Tournament and Adventure Mode won't each maintain their own set of difficulty rules.
        /// </summary>
        /// <param name="difficulty">The difficulty level selected by the player in the menu. </param>
        /// <returns>AI skill index between 0 and 3. </returns>
        public static int GetSkillIndex(mlpAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case mlpAiDifficulty.Easy:
                    // Easy = 0: The easiest level among the four levels, allowing the AI ​​to retain basic mobility, but with more relaxed attack and defense.

                    return EasySkillIndex;
                case mlpAiDifficulty.Hard:
                    // Hard = 2: It is obviously stronger than the normal difficulty, but it is still a fixed level and will not continue to increase with stages or levels.

                    return HardSkillIndex;
                case mlpAiDifficulty.Hell:
                    // Hell = 3: The highest basic skill value among the four levels; Hell’s additional enhancements are handled in other tuning tables.
                    return HellSkillIndex;
                default:
                    // Normal = 1: Default difficulty. It also returns to normal when encountering an unknown enumeration value to avoid entering illegal indexes.
                    return NormalSkillIndex;
            }
        }

        /// <summary>
        /// Directly obtain AI skill configuration according to four levels of difficulty.
        /// </summary>
        /// <param name="difficulty">The difficulty level selected by the player. </param>
        /// <returns>AI skill configuration for this difficulty level. </returns>
        public static mlpAISkillProfile Get(mlpAiDifficulty difficulty)
        {
            return Get(GetSkillIndex(difficulty));
        }

        /// <summary>
        /// Gets the skill configuration for the specified AI difficulty index. Indexes will be limited to between 0 and 3.
        /// </summary>
        /// <param name="skillIndex">Four levels of fixed difficulty index: 0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell. </param>
        /// <returns>AI skill configuration for this difficulty level. </returns>
        public static mlpAISkillProfile Get(int skillIndex)
        {
            // Normally skillIndex should come from GetSkillIndex.

            // Clamp only prevents illegal values ​​from being passed in by old saves, editor tests, or external calls, and prevents the game from crashing due to array out-of-bounds.

            var index = Mathf.Clamp(skillIndex, 0, Profiles.Length - 1);
            return Profiles[index];
        }

        /// <summary>
        /// Obtain the basic feel configuration of human players.
        /// </summary>
        /// <returns>The shooting, dunking and super power cooling parameters used by the player character. </returns>
        public static mlpAISkillProfile GetHumanPlayerProfile()
        {
            return HumanPlayerProfile;
        }
    }
}
