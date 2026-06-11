// AI 技能参数表  AI 的"操作水平"
// 全游戏单人 AI 有 Easy / Normal / Hard / Hell 四档。每档对应一组固定参数。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// AI 技能参数：定义某一档难度下 AI 的投篮命中率、扣篮成功率、冷却时间、抢断概率等数值。
    /// </summary>
    public readonly struct mlpAISkillProfile
    {
        // 投篮精度。【数值叠加】塞入 CalcDispersion() 公式计算球的飞行偏差，越低越准，0 = 百发百中
        public readonly float Accuracy;
        // 扣篮成功率。【概率掷骰】扣篮出手后 Random.value < 此值才算扣进，否则扣飞
        public readonly float ChanceToCompleteDunk;
        // 超能力冷却时间。【延迟计时】充能所需秒数，每帧累加 dt，充满后才能释放超能力
        public readonly float CoolDown;
        // 跳球反应速度。【延迟计时】传入 NegativeDelay 计时器控制起跳时机，值越小 AI 越早起跳
        public readonly float JumpBall;
        // 篮板争抢概率。【概率掷骰】球在篮板区域时，Random.value < 此值才起跳（还需先通过位置和计时器判断）
        public readonly float ChanceToRebound;
        // 投篮出手延迟。【延迟计时】传入 FullDelay 计时器，控制 AI 起跳后到出手之间的延迟，越小出手越快
        public readonly float Attack;
        // 接球即投概率。【概率掷骰】落地持球后，Random.value < 此值则立刻起跳投篮，否则先运球再决策
        public readonly float AttackAtOnce;
        // 闪避抢断能力。【概率掷骰】对手发起抢断时，Random.value < 此值则 AI 尝试闪避（冲刺/跳跃/侧移）
        public readonly float AvoidSteal;
        // 假动作概率。【未使用】原计划控制 AI 进攻时做假动作的概率，当前代码未接入，属于死代码
        public readonly float MakePump;
        // 防守反应概率。【概率掷骰】进攻时身后有防守者，Random.value < 此值则 AI 做出反应（变向或冲刺），否则无视继续冲
        public readonly float ReactOnOpponent;
        // 冲刺闪避概率。【概率掷骰】ReactOnOpponent 通过后的二级判定，Random.value < 此值则用冲刺闪避，否则只变向
        public readonly float MakeDash;
        // 冲刺冷却间隔。【延迟计时】传入 AIUseDelay，控制两次冲刺决策之间的冷却秒数，越短冲刺越频繁
        public readonly float DelayDash;
        // 防守干扰延迟。【延迟计时】传入 SimpleDelay，控制对手起跳投篮后 AI 起跳干扰的延迟，越小反应越快
        public readonly float Defence;
        // 跳投干扰概率。【概率掷骰】两个用途：(1) 持球时身后有人，决定是否抢先起跳投篮；(2) 对手起跳时，决定是否起跳干扰
        public readonly float JumpThrow;
        // 抢断尝试概率。【概率掷骰】靠近持球者时 Random.value < 此值发起抢断；对手靠近篮筐时概率 ×1.5
        public readonly float MakeSteal;
        // 抢断冷却间隔。【延迟计时】加到基础抢断动作时长上，控制两次抢断尝试之间的冷却秒数
        public readonly float DelaySteal;
        // 被假动作骗到的概率。【概率掷骰】对手做假动作时，Random.value < 此值则 AI 被骗起跳（值越高越容易上当）
        public readonly float JumpPump;
        // 盖帽尝试概率。【概率掷骰】对手从身后冲刺时，Random.value < 此值则 AI 尝试起跳盖帽
        public readonly float MakeBlock;
        // 篮板固定延迟。【延迟计时】传入 FullDelay 作为固定延迟分量，控制篮板起跳时机的基础等待时间
        public readonly float ReboundFixed;
        // 篮板随机浮动范围。【延迟计时】传入 FullDelay 作为随机浮动分量，和 ReboundFixed 共同决定篮板起跳的延迟窗口
        public readonly float ReboundRange;
        // 移动决策间隔。【延迟计时】传入 FullDelay，控制 AI 攻防移动决策之间的间隔，越小 AI 移动越敏捷
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
