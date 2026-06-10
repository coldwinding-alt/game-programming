// AI 技能参数表
// 全游戏单人 AI 只保留 Easy / Normal / Hard / Hell 四档。每档对应一组固定参数，不再按赛制或关卡递增。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// AI 技能参数：定义某一档难度下 AI 的投篮命中率、扣篮成功率、冷却时间、抢断概率等数值。
    /// </summary>
    public readonly struct mlpAISkillProfile
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
    /// AI 技能参数表：集中管理 Easy、Normal、Hard、Hell 四档固定难度。
    /// 这里的 AI 难度索引只有 0、1、2、3 四个值：
    /// 0 = Easy，1 = Normal，2 = Hard，3 = Hell。
    /// 锦标赛轮次、冒险关卡编号和赛段进度都不会再产生额外的隐藏技能等级。
    /// </summary>
    public static class mlpAISkillsData
    {
        public const int EasySkillIndex = 0;
        public const int NormalSkillIndex = 1;
        public const int HardSkillIndex = 2;
        public const int HellSkillIndex = 3;

        // 人类玩家使用的基础手感参数。
        // 注意：这不是第五个 AI 难度档，只是为了保留玩家角色原本的投篮、扣篮和超能力冷却手感。
        // AI 的四档难度只读取下面的 Profiles 数组。
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

        // 当前 AI 难度的四档固定调参表。
        // 每个数组元素都直接对应一个可选难度档位，不再存在中间等级或隐藏递增等级。
        // 如果以后需要调手感，只改这四个档位本身，不要新增赛段/关卡偏移。
        private static readonly mlpAISkillProfile[] Profiles =
        {
            // Easy：保留基本移动和反应，但进攻、防守和扣篮稳定性都比较保守。
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
            // Normal：默认单人体验，比 Easy 更会抢篮板和补防，但仍给玩家留下稳定反应空间。
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
            // Hard：明显提高防守压迫、抢断和进攻执行力，但不会因为锦标赛后期或冒险后期继续上涨。
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
            // Hell：四档里的最高基础参数；Hell 专属额外强化仍由难度调校和控制器逻辑处理。
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
        /// 将玩家选择的四档难度转换为内部 AI 技能索引。
        /// 所有单人模式都应该先走这里，再把结果写入比赛数据。
        /// 这样快速赛、随机赛、锦标赛和冒险模式就不会各自维护一套难度规则。
        /// </summary>
        /// <param name="difficulty">玩家在菜单里选择的难度档位。</param>
        /// <returns>0 到 3 之间的 AI 技能索引。</returns>
        public static int GetSkillIndex(mlpAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case mlpAiDifficulty.Easy:
                    // Easy = 0：四档里最简单的一档，让 AI 保留基本行动能力，但进攻和防守更宽松。
                    return EasySkillIndex;
                case mlpAiDifficulty.Hard:
                    // Hard = 2：明显强于普通难度，但依然是固定档位，不会随赛段或关卡继续增加。
                    return HardSkillIndex;
                case mlpAiDifficulty.Hell:
                    // Hell = 3：四档里的最高基础技能值；Hell 额外强化在其他调校表里处理。
                    return HellSkillIndex;
                default:
                    // Normal = 1：默认难度。遇到未知枚举值时也回到普通，避免进入非法索引。
                    return NormalSkillIndex;
            }
        }

        /// <summary>
        /// 根据四档难度直接获取 AI 技能配置。
        /// </summary>
        /// <param name="difficulty">玩家选择的难度档位。</param>
        /// <returns>该难度档位的 AI 技能配置。</returns>
        public static mlpAISkillProfile Get(mlpAiDifficulty difficulty)
        {
            return Get(GetSkillIndex(difficulty));
        }

        /// <summary>
        /// 获取指定 AI 难度索引的技能配置。索引会被限制在 0 到 3 之间。
        /// </summary>
        /// <param name="skillIndex">四档固定难度索引：0 = Easy，1 = Normal，2 = Hard，3 = Hell。</param>
        /// <returns>该难度档位的 AI 技能配置。</returns>
        public static mlpAISkillProfile Get(int skillIndex)
        {
            // 正常情况下 skillIndex 应该来自 GetSkillIndex。
            // Clamp 只是防御旧存档、编辑器测试或外部调用传入非法值，避免数组越界导致比赛崩溃。
            var index = Mathf.Clamp(skillIndex, 0, Profiles.Length - 1);
            return Profiles[index];
        }

        /// <summary>
        /// 获取人类玩家的基础手感配置。
        /// </summary>
        /// <returns>玩家角色使用的投篮、扣篮和超能力冷却参数。</returns>
        public static mlpAISkillProfile GetHumanPlayerProfile()
        {
            return HumanPlayerProfile;
        }
    }
}
