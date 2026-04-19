using UnityEngine;

namespace BasketballLegends2020
{
    public readonly struct BLAISkillProfile
    {
        public readonly float Accuracy;
        public readonly float ChanceToCompleteDunk;
        public readonly float CoolDown;
        public readonly float JumpBall;
        public readonly float ChanceToRebound;
        public readonly float Attack;
        public readonly float AttackAtOnce;
        public readonly float AvoidSteal;
        public readonly float MakePump;
        public readonly float ReactOnOpponent;
        public readonly float MakeDash;
        public readonly float DelayDash;
        public readonly float Defence;
        public readonly float JumpThrow;
        public readonly float MakeSteal;
        public readonly float DelaySteal;
        public readonly float JumpPump;
        public readonly float MakeBlock;
        public readonly float ReboundFixed;
        public readonly float ReboundRange;
        public readonly float MoveDelay;

        public BLAISkillProfile(
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

    public static class BLAISkillsData
    {
        // Direct mapping of the original AISkillsData.botsSkills table.
        private static readonly BLAISkillProfile[] Profiles =
        {
            new BLAISkillProfile(
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
                moveDelay: 0f),
            new BLAISkillProfile(
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
            new BLAISkillProfile(
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
            new BLAISkillProfile(
                accuracy: 0.1f,
                chanceToCompleteDunk: 0.5f,
                coolDown: 35f,
                jumpBall: 0.45f,
                chanceToRebound: 0.4f,
                attack: 0.35f,
                attackAtOnce: 0.4f,
                avoidSteal: 0.4f,
                makePump: 0.5f,
                reactOnOpponent: 0.5f,
                makeDash: 1f,
                delayDash: 4f,
                defence: 0.45f,
                jumpThrow: 0.6f,
                makeSteal: 0.5f,
                delaySteal: 2f,
                jumpPump: 0.6f,
                makeBlock: 0.4f,
                reboundFixed: 0.25f,
                reboundRange: 0.1f,
                moveDelay: 0.06f),
            new BLAISkillProfile(
                accuracy: 0.08f,
                chanceToCompleteDunk: 0.6f,
                coolDown: 35f,
                jumpBall: 0.4f,
                chanceToRebound: 0.5f,
                attack: 0.3f,
                attackAtOnce: 0.5f,
                avoidSteal: 0.5f,
                makePump: 0.5f,
                reactOnOpponent: 0.6f,
                makeDash: 1f,
                delayDash: 3f,
                defence: 0.3f,
                jumpThrow: 0.8f,
                makeSteal: 0.6f,
                delaySteal: 1f,
                jumpPump: 0.5f,
                makeBlock: 0.5f,
                reboundFixed: 0.2f,
                reboundRange: 0.1f,
                moveDelay: 0.05f),
            new BLAISkillProfile(
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
            new BLAISkillProfile(
                accuracy: 0.03f,
                chanceToCompleteDunk: 0.75f,
                coolDown: 28f,
                jumpBall: 0.3f,
                chanceToRebound: 0.8f,
                attack: 0.2f,
                attackAtOnce: 0.7f,
                avoidSteal: 0.7f,
                makePump: 0.5f,
                reactOnOpponent: 0.7f,
                makeDash: 1f,
                delayDash: 2.5f,
                defence: 0.2f,
                jumpThrow: 1f,
                makeSteal: 0.8f,
                delaySteal: 1f,
                jumpPump: 0.4f,
                makeBlock: 0.5f,
                reboundFixed: 0.15f,
                reboundRange: 0.1f,
                moveDelay: 0.05f),
            new BLAISkillProfile(
                accuracy: 0.02f,
                chanceToCompleteDunk: 0.8f,
                coolDown: 28f,
                jumpBall: 0.2f,
                chanceToRebound: 0.9f,
                attack: 0.1f,
                attackAtOnce: 0.8f,
                avoidSteal: 0.8f,
                makePump: 0.5f,
                reactOnOpponent: 0.8f,
                makeDash: 1f,
                delayDash: 2f,
                defence: 0.1f,
                jumpThrow: 1f,
                makeSteal: 0.9f,
                delaySteal: 1f,
                jumpPump: 0.3f,
                makeBlock: 0.8f,
                reboundFixed: 0.1f,
                reboundRange: 0.1f,
                moveDelay: 0.05f),
            new BLAISkillProfile(
                accuracy: 0.01f,
                chanceToCompleteDunk: 0.9f,
                coolDown: 28f,
                jumpBall: 0.1f,
                chanceToRebound: 1f,
                attack: 0.05f,
                attackAtOnce: 0.9f,
                avoidSteal: 0.9f,
                makePump: 0.5f,
                reactOnOpponent: 0.9f,
                makeDash: 1f,
                delayDash: 2f,
                defence: 0.05f,
                jumpThrow: 1f,
                makeSteal: 0.9f,
                delaySteal: 1f,
                jumpPump: 0.2f,
                makeBlock: 1f,
                reboundFixed: 0f,
                reboundRange: 0.1f,
                moveDelay: 0.05f)
        };

        public static BLAISkillProfile Get(int skillIndex)
        {
            var index = Mathf.Clamp(skillIndex, 0, Profiles.Length - 1);
            return Profiles[index];
        }
    }
}
