// 文件作用：玩家输入控制器（键盘和 AI）
// 概括：定义玩家用什么方式控制角色：键盘玩家 1 用 WASD 控制，键盘玩家 2 用方向键控制，AI 由电脑自动控制。每帧读取按键状态，告诉角色往哪移动、是否跳跃、是否投篮。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 玩家控制器接口：定义控制角色的统一方式（移动、跳跃、投篮、防守、大招）。键盘和 AI 都实现这个接口。
    /// </summary>
    public interface IBLPlayerController
    {
        int CurrentMove { get; }
        bool CurrentJump { get; }
        bool CurrentAction { get; }
        bool CurrentBlockOrPump { get; }
        bool CurrentSuper { get; }
        int CurrentDash { get; }
        void UpdateController(float dt);
        void BallInOwnHands(int holderPlayerNo);
        void BallInOpponentsHands(int holderPlayerNo);
        void BallOwnShoot(int shooterPlayerNo);
        void BallOpponentShoot(int shooterPlayerNo);
        void BallOthers();
        bool ReadyForAction();
        bool ReleaseBlockOrPump(float dt);
        void Restart(int startSide);
        void PlayerOnGround();
        void PlayerOnDashEnd();
        void PlayerOnBlock();
    }

    /// <summary>
    /// 键盘控制器：读取键盘按键输入来控制角色。根据配置的按键映射，每帧检测按键状态并转换为角色动作。
    /// </summary>
    public sealed class mlpKeyboardController : IBLPlayerController
    {
        private readonly mlpControlProfile controls;
        private float lastLeftDown = -10f;
        private float lastRightDown = -10f;
        private float lastLeftUp = -10f;
        private float lastRightUp = -10f;
        private int pendingDashDirection;
        private float pendingDashTimer;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        /// <summary>
        /// 创建一个键盘控制器，从给定的控制配置中读取按键。
        /// </summary>
        /// <param name="brain">控制器标识字符串</param>
        public mlpKeyboardController(string brain)
        {
            // 1. 根据控制器标识（如 "KB1" 或 "KB2"）加载对应的按键配置（WASD 或方向键）
            controls = mlpControlsData.ProfileForBrain(brain);
        }

        /// <summary>
        /// 每帧读取键盘输入：移动、跳跃、投篮、盖帽、必杀技和冲刺双击。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void UpdateController(float dt)
        {
            // 1. 重置移动和冲刺状态
            CurrentMove = 0;
            CurrentDash = 0;
            // 2. 如果有正在缓冲的冲刺指令，递减计时器并在有效期内执行冲刺
            if (pendingDashTimer > 0f)
            {
                pendingDashTimer = Mathf.Max(0f, pendingDashTimer - dt);
                if (pendingDashTimer > 0f)
                {
                    CurrentDash = pendingDashDirection;
                }
                else
                {
                    pendingDashDirection = 0;
                }
            }

            // 3. 读取配置中的左移和右移按键
            var leftDown = controls.MoveLeftKey;
            var rightDown = controls.MoveRightKey;
            var currentTime = Time.time;

            // 4. 记录按键松开的时间（用于双击冲刺检测）
            if (Input.GetKeyUp(leftDown))
            {
                lastLeftUp = currentTime;
            }

            if (Input.GetKeyUp(rightDown))
            {
                lastRightUp = currentTime;
            }

            // 5. 检测左移键按下：如果在短时间内连按了两次（按下-按下 或 松开-按下），触发向左冲刺
            if (Input.GetKeyDown(leftDown))
            {
                if (currentTime - lastLeftDown <= mlpObjectsData.DashDoubleTapWindow
                    || currentTime - lastLeftUp <= mlpObjectsData.DashDoubleTapWindow)
                {
                    QueueDash(-1);
                }

                lastLeftDown = currentTime;
            }

            // 6. 检测右移键按下：同理，双击触发向右冲刺
            if (Input.GetKeyDown(rightDown))
            {
                if (currentTime - lastRightDown <= mlpObjectsData.DashDoubleTapWindow
                    || currentTime - lastRightUp <= mlpObjectsData.DashDoubleTapWindow)
                {
                    QueueDash(1);
                }

                lastRightDown = currentTime;
            }

            // 7. 按住左移键时移动方向 -1，按住右移键时 +1（可叠加，同时按时为 0）
            if (Input.GetKey(leftDown))
            {
                CurrentMove--;
            }

            if (Input.GetKey(rightDown))
            {
                CurrentMove++;
            }

            // 8. 读取跳跃、动作（投篮/抢断）、防守/假动作、大招按键的当前状态
            CurrentJump = Input.GetKey(controls.JumpKey);
            CurrentAction = Input.GetKey(controls.ActionKey);
            CurrentBlockOrPump = Input.GetKey(controls.BlockKey);
            CurrentSuper = Input.GetKey(controls.SuperKey);
        }

        private void QueueDash(int direction)
        {
            pendingDashDirection = direction;
            pendingDashTimer = mlpObjectsData.DashInputBuffer;
            CurrentDash = direction;
        }

        /// <summary>
        /// 当动作键释放时返回 true，防止按住时重复投篮。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        public bool ReadyForAction()
        {
            return !Input.GetKey(controls.ActionKey);
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        /// <param name="holderPlayerNo">当前持球者的玩家编号</param>
        public void BallInOwnHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        /// <param name="holderPlayerNo">当前持球者的玩家编号</param>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        /// <param name="shooterPlayerNo">投篮者的玩家编号</param>
        public void BallOwnShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        /// <param name="shooterPlayerNo">投篮者的玩家编号</param>
        public void BallOpponentShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        public void BallOthers()
        {
        }

        /// <summary>
        /// 当盖帽键释放时返回 true，结束盖帽/虚晃动画。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        public bool ReleaseBlockOrPump(float dt)
        {
            return !Input.GetKey(controls.BlockKey);
        }

        /// <summary>
        /// 重置所有输入状态和双击计时器，准备新回合。
        /// </summary>
        /// <param name="startSide">重置后玩家的初始位置方向</param>
        public void Restart(int startSide)
        {
            lastLeftDown = -10f;
            lastRightDown = -10f;
            lastLeftUp = -10f;
            lastRightUp = -10f;
            pendingDashDirection = 0;
            pendingDashTimer = 0f;
            CurrentMove = 0;
            CurrentDash = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentBlockOrPump = false;
            CurrentSuper = false;
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        public void PlayerOnGround()
        {
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        public void PlayerOnDashEnd()
        {
            pendingDashDirection = 0;
            pendingDashTimer = 0f;
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        public void PlayerOnBlock()
        {
        }
    }

    /// <summary>
    /// AI 控制器（基础版）：让电脑自动控制角色，根据比赛情况决定移动、投篮、防守等行为。
    /// </summary>
    public class mlpAIController : mlpBaseAIController
    {
        /// <summary>
        /// 创建一个具有默认防守行为的 AI 控制器。
        /// </summary>
        /// <param name="player">所属的玩家对象</param>
        /// <param name="skillLevel">AI 技能等级数值（越高越难）</param>
        public mlpAIController(mlpPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// 工厂方法，根据控制器标识返回正确的 AI 控制器变体。
        /// </summary>
        /// <param name="player">所属的玩家对象</param>
        /// <param name="brain">控制器标识字符串</param>
        /// <param name="skillLevel">AI 技能等级数值（越高越难）</param>
        /// <returns>创建的控制器实例。</returns>
        public static IBLPlayerController CreateForBrain(mlpPlayerObject player, string brain, int skillLevel)
        {
            var index = ParseBrainIndex(brain);
            return index == 1 ? new mlpAIController2(player, skillLevel) : new mlpAIController(player, skillLevel);
        }

        /// <summary>
        /// 返回 false；默认 AI 不使用替代防守风格。
        /// </summary>
        /// <param name="holder">待测试的持球者</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected override bool UseDefence2(mlpPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// 从类似 'B1' 或 'B2' 的控制器标识字符串中提取数字变体索引。
        /// </summary>
        /// <param name="brain">控制器标识字符串</param>
        /// <returns>解析出的索引数值。</returns>
        private static int ParseBrainIndex(string brain)
        {
            if (string.IsNullOrEmpty(brain) || brain.Length < 2 || !brain.StartsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return int.TryParse(brain.Substring(1, 1), out var value) ? value : 0;
        }
    }

    /// <summary>
    /// AI 控制器（高级版）：比基础版更聪明的 AI，用于教程模式的对手，有更复杂的行为决策。
    /// </summary>
    public sealed class mlpAIController2 : mlpBaseAIController
    {
        /// <summary>
        /// 创建一个使用主动抢断防守风格的 AI 控制器变体。
        /// </summary>
        /// <param name="player">所属的玩家对象</param>
        /// <param name="skillLevel">AI 技能等级数值（越高越难）</param>
        public mlpAIController2(mlpPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// 当持球者是对手时返回 true，启用替代防守策略。
        /// </summary>
        /// <param name="holder">待测试的持球者</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected override bool UseDefence2(mlpPlayerObject holder)
        {
            return holder != null && holder.PlayerNo != player.PlayerNo;
        }

        /// <summary>
        /// 以抢断时机追逐持球者，并在近距离起跳干扰投篮。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected override void StrategyDefence2(float dt)
        {
            if (opponent == null)
            {
                return;
            }

            CurrentMove = MoveTo(defensePoint);
            var stealState = stealDelay.Update(dt);
            if (stealState == -1)
            {
                TryToSteal();
            }

            CurrentAction = stealState == 1;
            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseBehind(GetDefenceContestDistance());
        }
    }

    /// <summary>
    /// AI 控制器基类：实现 AI 共有的行为逻辑（如计时器管理、状态判断），具体决策由子类实现。
    /// </summary>
    public abstract class mlpBaseAIController : IBLPlayerController
    {
        protected readonly mlpPlayerObject player;
        protected readonly mlpAiDifficulty difficulty;
        protected readonly mlpAIDifficultyTuningProfile tuning;

        protected mlpBallObject ball;
        protected List<mlpPlayerObject> opponents;
        protected mlpPlayerObject opponent;
        protected readonly mlpAISkillProfile profile;

        protected readonly NegativeDelay jumpBall;
        protected readonly FullDelay attack;
        protected readonly SimpleDelay attackJumpDelay;
        protected readonly AIUseDelay stealDelay;
        protected readonly SimpleDelay defenceDelay;
        protected readonly FullDelay blockDelay;
        protected readonly FullDelay reboundDelay;
        protected readonly FullDelay moveDelay;
        protected readonly AIUseDelay dashDecisionDelay;
        protected readonly FullDelay megaDunkDelay;
        protected readonly FullDelay superDashDelay;

        protected int strategy;
        protected float attackPoint;
        protected float jumpPoint;
        protected float reboundPoint;
        protected float reboundPointInAttack;
        protected float reboundPointInDefence;
        protected float baseEndPoint;
        protected float endPoint;
        protected float defensePoint;
        protected float deltaDownTime;
        protected float directionToFly;
        protected bool attackJump;
        protected bool avoidStealJump;
        protected int avoidStealMove;
        protected bool isPumped;
        protected int pumpCount;
        protected bool queuedReboundJump;
        protected int playerNo;
        protected bool initialized;
        protected float attackZoneStart;
        protected float attackZoneEnd;
        protected float dashZoneStart;
        protected float dashZoneEnd;
        protected bool willAttackAtOnce;
        protected bool queuedSuperInput;
        protected const float DeltaDistance = 20f;
        protected const float DownTime = 5f;

        public int CurrentMove { get; protected set; }
        public bool CurrentJump { get; protected set; }
        public bool CurrentAction { get; protected set; }
        public bool CurrentBlockOrPump { get; protected set; }
        public bool CurrentSuper { get; protected set; }
        public int CurrentDash { get; protected set; }

        /// <summary>
        /// 初始化共享的 AI 状态：难度配置、决策计时器和进攻/防守区域。
        /// </summary>
        /// <param name="player">所属的玩家对象</param>
        /// <param name="skillLevel">AI 技能等级数值（越高越难）</param>
        protected mlpBaseAIController(mlpPlayerObject player, int skillLevel)
        {
            // 1. 保存玩家对象引用和编号
            this.player = player;
            // 2. 读取当前难度设置（简单/普通/困难/地狱）和对应的调校参数
            difficulty = mlpInventory.Instance.Difficulty;
            tuning = mlpAIDifficultyTuning.Get(difficulty);
            // 3. 根据技能等级加载 AI 技能配置（控制投篮时机、抢断概率等）
            profile = mlpAISkillsData.Get(skillLevel);
            // 4. 初始化各种决策延迟计时器（控制 AI 不要每帧都做决策，模拟人类反应时间）
            jumpBall = new NegativeDelay(mlpObjectsData.IdealJumpBallJump, profile.JumpBall);
            attack = new FullDelay(mlpObjectsData.IdealAttackJump, profile.Attack);
            attackJumpDelay = new SimpleDelay(profile.AttackAtOnce);
            stealDelay = new AIUseDelay(0.1f, mlpObjectsData.StealDuration + profile.DelaySteal);
            defenceDelay = new SimpleDelay(profile.Defence);
            blockDelay = new FullDelay(0f, 0.2f);
            reboundDelay = new FullDelay(profile.ReboundRange, profile.ReboundFixed);
            moveDelay = new FullDelay(profile.MoveDelay, 0.05f);
            dashDecisionDelay = new AIUseDelay(0.1f, profile.DelayDash);
            megaDunkDelay = new FullDelay(0.5f, 0.5f);
            superDashDelay = new FullDelay(0.5f, 0.5f);
            // 5. 记录玩家编号，订阅游戏信号（对手跳跃、抢断、虚晃等事件）
            playerNo = player.PlayerNo;
            player.GameCore.PlayerSignals.OnSignal += ProcessPlayerSignal;
            // 6. 计算进攻/防守/篮板位置区域，重置所有状态
            InitZones();
            ResetForRestart();
        }

        /// <summary>
        /// 运行完整的 AI 决策循环：根据球的状态选择策略，然后调用对应的策略方法。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public virtual void UpdateController(float dt)
        {
            // 1. 首次运行时获取球和对手的引用
            EnsureRuntimeLinks();
            // 2. 重置冲刺，应用上一帧排队的大招输入
            CurrentDash = 0;
            CurrentSuper = queuedSuperInput;
            queuedSuperInput = false;

            // 3. 如果球或对手引用丢失，清空所有输入
            if (ball == null || opponents == null || opponents.Count == 0)
            {
                CurrentMove = 0;
                CurrentJump = false;
                CurrentAction = false;
                CurrentSuper = false;
                return;
            }

            // 4. 处理"快速起跳"延迟计时器——用于落地后立即起跳投篮的场景
            var delayedJump = attackJumpDelay.Update(dt);
            if (delayedJump >= 0)
            {
                if (delayedJump == 1)
                {
                    // 计时器到期：执行跳跃
                    CurrentMove = 0;
                    CurrentJump = true;
                    CurrentAction = false;
                }
                else
                {
                    // 计时器未到期：保持躲避抢断的移动和跳跃状态
                    CurrentMove = avoidStealMove;
                    CurrentJump = avoidStealJump;
                }

                return;
            }

            // 5. 根据球的状态选择 AI 策略
            var holder = player.GameCore.FindBallHolder();
            if (player.WithBall)
            {
                // 5a. 自己持球 → 进攻策略：向投篮点移动，在合适时机起跳投篮
                if (strategy != 2)
                {
                    HandleBallInOwnHands();
                }

                StrategyAttack(dt);
            }
            else if (holder != null && holder.Side != player.Side)
            {
                // 5b. 对手持球 → 防守策略：跟随对手，尝试抢断或干扰投篮
                opponent = holder;
                if (UseDefence2(holder))
                {
                    strategy = 5;
                    StrategyDefence2(dt);
                }
                else
                {
                    if (strategy != 0)
                    {
                        HandleBallInOpponentsHands();
                    }

                    StrategyDefence(dt);
                }
            }
            else
            {
                // 5c. 无人持球
                var shotInFlight = ball.State == "shooting" || ball.State == "basket";
                if (shotInFlight)
                {
                    // 球在空中（投篮/打板）→ 篮板策略：抢占篮板位置
                    if (strategy != 4)
                    {
                        if (ball.Side == player.Side)
                        {
                            BallOwnShoot(0);
                        }
                        else
                        {
                            BallOpponentShoot(0);
                        }
                    }

                    StrategyRebound(dt);
                }
                else
                {
                    // 球在地上弹跳 → 争球策略：追逐球并尝试捡起
                    if (strategy != 1)
                    {
                        HandleBallOthers();
                    }

                    StrategyBallFight(dt);
                }
            }
        }

        /// <summary>
        /// 切换到进攻模式，可选激活超级扣篮延迟。
        /// </summary>
        /// <param name="holderPlayerNo">当前持球者的玩家编号</param>
        public virtual void BallInOwnHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            if (player.UsesPossessionSkill && player.ReadyForSuper)
            {
                megaDunkDelay.Activate();
            }

            HandleBallInOwnHands();
        }

        /// <summary>
        /// 切换到防守模式，当对手靠近时可选使用冰冻必杀。
        /// </summary>
        /// <param name="holderPlayerNo">当前持球者的玩家编号</param>
        public virtual void BallInOpponentsHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(holderPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            if (player.UsesFreezeSkill && player.ReadyForSuper && opponent != null && Mathf.Abs(player.Position.x - opponent.Position.x) <= 220f)
            {
                player.SuperShot();
            }

            HandleBallInOpponentsHands();
        }

        /// <summary>
        /// 队友投篮后切换到篮板模式，为进攻篮板做好位置准备。
        /// </summary>
        /// <param name="shooterPlayerNo">投篮者的玩家编号</param>
        public virtual void BallOwnShoot(int shooterPlayerNo)
        {
            EnsureRuntimeLinks();
            ResetCurrents();
            queuedReboundJump = false;
            strategy = 4;
            reboundPoint = reboundPointInAttack;
            superDashDelay.Reset();
        }

        /// <summary>
        /// 对手投篮后切换到篮板模式；可选激活护盾必杀。
        /// </summary>
        /// <param name="shooterPlayerNo">投篮者的玩家编号</param>
        public virtual void BallOpponentShoot(int shooterPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(shooterPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            if (player.UsesShieldSkill && player.ReadyForSuper)
            {
                player.SuperShot();
            }

            ResetCurrents();
            strategy = 4;
            reboundPoint = reboundPointInDefence;
            superDashDelay.Reset();
            TryUseHellBonusShieldAgainstHumanShot();
            queuedReboundJump = opponent != null &&
                                opponent.IsGrounded &&
                                IsOpponentCloseBehind(120f) &&
                                UnityEngine.Random.value <= profile.JumpThrow;
        }

        /// <summary>
        /// 切换到争夺球模式，追逐球并尝试捡起。
        /// </summary>
        public virtual void BallOthers()
        {
            EnsureRuntimeLinks();
            HandleBallOthers();
        }

        /// <summary>
        /// 始终返回 true；AI 控制器随时准备执行动作。
        /// </summary>
        /// <returns>始终返回 true。</returns>
        public virtual bool ReadyForAction()
        {
            return true;
        }

        /// <summary>
        /// 当盖帽计时器到期时返回 true，结束 AI 的盖帽尝试。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        public virtual bool ReleaseBlockOrPump(float dt)
        {
            return blockDelay.Update(dt) == 1;
        }

        /// <summary>
        /// 为新回合重置所有 AI 状态。
        /// </summary>
        /// <param name="startSide">重置后玩家的初始位置方向</param>
        public virtual void Restart(int startSide)
        {
            ResetForRestart();
        }

        /// <summary>
        /// 重置虚晃状态，如果持球则可选立即排队攻击跳跃。
        /// </summary>
        public virtual void PlayerOnGround()
        {
            isPumped = false;
            pumpCount = 0;
            if (player.WithBall && willAttackAtOnce)
            {
                ResetCurrents();
                attackJumpDelay.Activate();
            }
        }

        /// <summary>
        /// 如果冲刺超过目标位置，调整攻击点。
        /// </summary>
        public virtual void PlayerOnDashEnd()
        {
            if ((player.Position.x - attackPoint) * player.Side < 0f)
            {
                attackPoint = player.Position.x - 10f * player.Side;
            }
        }

        /// <summary>
        /// 清除盖帽输入并启动盖帽冷却计时器。
        /// </summary>
        public virtual void PlayerOnBlock()
        {
            CurrentBlockOrPump = false;
            blockDelay.Activate();
        }

        /// <summary>
        /// 返回 false；子类可重写此方法以启用替代防守策略。
        /// </summary>
        /// <param name="holder">待测试的持球者</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected virtual bool UseDefence2(mlpPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// 跟随持球者，在近距离尝试抢断，并用定时跳跃干扰投篮。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyDefence(float dt)
        {
            // 1. 如果没有对手信息，直接退出
            if (opponent == null)
            {
                return;
            }

            // 2. 尝试使用超级冲刺拦截持球对手，如果成功则本帧不再做其他操作
            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashAgainstHolder()))
            {
                return;
            }

            // 3. 更新抢断和移动延迟计时器
            var stealState = stealDelay.Update(dt);
            var moveState = moveDelay.Update(dt);
            // 4. 如果被假动作骗到（"被晃飞"），则停下不动
            if (isPumped)
            {
                CurrentMove = 0;
            }
            else
            {
                // 5. 计算防守目标位置：如果对手超出防守边界就守住边界，否则紧跟对手身后一段距离
                var target = (opponent.Position.x - endPoint) * player.Side < 0f
                    ? endPoint
                    : opponent.IsGrounded
                        ? opponent.Position.x + player.Side * mlpObjectsData.OpponentDelta
                        : opponent.Position.x + player.Side * (mlpObjectsData.OpponentDelta - 10f);

                // 6. 站在地上时按移动延迟节奏跟防，跳起时则直接跟踪对手
                if (player.IsGrounded)
                {
                    if (moveState == -1)
                    {
                        CurrentMove = MoveTo(target);
                        moveDelay.Activate();
                    }
                }
                else
                {
                    CurrentMove = MoveTo(opponent.Position.x + player.Side * (mlpObjectsData.OpponentDelta - 10f));
                }

                // 7. 当抢断计时器到期时，尝试抢断
                if (stealState == -1)
                {
                    TryToSteal();
                }
            }

            // 8. 当盖帽计时器到期且对手在身边时起跳干扰投篮
            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseAbs(GetDefenceContestDistance());
            // 9. 抢断计时器激活时执行抢断动作
            CurrentAction = stealState == 1;
            // 10. 如果长时间没有动作（站着不动），将防守边界重置到场地另一侧，防止消极防守
            if (!CurrentAction && !CurrentJump && CurrentMove == 0)
            {
                deltaDownTime += dt;
                if (deltaDownTime >= DownTime)
                {
                    endPoint = player.Side == 1 ? 0f : mlpConstants.Width;
                    deltaDownTime = 0f;
                }
            }
            else
            {
                deltaDownTime = 0f;
            }
        }

        /// <summary>
        /// 委托给默认防守策略；子类可重写以实现自定义行为。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyDefence2(float dt)
        {
            StrategyDefence(dt);
        }

        /// <summary>
        /// 追逐自由球，当球靠近篮筐时起跳争抢篮板。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyBallFight(float dt)
        {
            // 1. 获取自由球的目标 X 坐标（地狱难度会预测落点），并向球的方向移动（带一点偏移避免撞到球）
            var ballX = GetTechnicalLooseBallTargetX();
            var offset = ballX - player.Position.x >= 0f ? 10f : -10f;
            CurrentMove = MoveTo(ballX + offset);
            CurrentJump = false;

            // 2. 如果球在空中且距离够远，尝试使用超级冲刺抢先捡球
            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashForBall()))
            {
                return;
            }

            // 3. 根据球的状态决定是否起跳
            if (ball.State != "bounce" && ball.State != "shooting")
            {
                if (ball.State == "basket")
                {
                    // 3a. 球在篮筐附近弹跳 → 争抢篮板：使用篮板延迟计时器，在篮板区域起跳
                    var reboundState = reboundDelay.Update(dt);
                    if (reboundState == -1 && IsBallInReboundZone())
                    {
                        reboundDelay.Activate();
                    }
                    else
                    {
                        CurrentJump = reboundState == 1 && UnityEngine.Random.value < profile.ChanceToRebound && IsBallInReboundZone();
                    }
                }
                else
                {
                    // 3b. 球在其他状态（如抢断中）→ 水平距离近且垂直距离远时起跳
                    CurrentJump = Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
                }
            }

            // 4. 争球状态下不执行投篮/抢断等动作
            CurrentAction = false;
        }

        /// <summary>
        /// 向攻击点移动，把握投篮起跳时机，并对附近的防守者做出反应。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyAttack(float dt)
        {
            // 1. 如果没有持球，直接退出
            if (!player.WithBall)
            {
                return;
            }

            // 2. 如果拥有扣篮类大招且计时器到期，触发大招投篮
            if (player.UsesPossessionSkill && megaDunkDelay.Update(dt) == 1)
            {
                TriggerSuperInput();
                return;
            }

            // 3. 尝试使用超级冲刺甩开防守者
            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashInAttack()))
            {
                return;
            }

            // 4. 如果正在执行躲避抢断的动作（跳起或侧移），优先完成躲避
            if (avoidStealJump || avoidStealMove != 0)
            {
                CurrentMove = avoidStealMove;
                CurrentJump = avoidStealJump;
                return;
            }

            // 5. 站在地面上时的进攻逻辑
            if (player.IsGrounded)
            {
                // 5a. 移动延迟计时器到期时做一次决策
                if (moveDelay.Update(dt) == -1)
                {
                    // 5b. 向跳跃点/攻击点移动，并判断是否应该起跳投篮
                    var move = MoveInAttack();
                    if (attackJump)
                    {
                        // 5c. 到达跳跃点 → 起跳投篮
                        CurrentJump = true;
                        CurrentMove = move;
                    }
                    else if (IsAICloserForBasket())
                    {
                        // 5d. AI 比对手更靠近篮筐 → 可以直接上篮
                        if (move == -player.Side)
                        {
                            CurrentMove = -player.Side;
                            CurrentJump = false;
                        }
                        else
                        {
                            CurrentMove = move;
                            CurrentJump = true;
                        }
                    }
                    else
                    {
                        // 5e. 对手挡在前面 → 处理各种被防守的情况
                        CurrentJump = false;
                        CurrentDash = 0;
                        if (IsOpponentCloseBehind())
                        {
                            // 5f. 对手从后面追上来施压
                            if (IsUnderOwnBasket())
                            {
                                // 5g. 在自家篮筐下 → 用冲刺甩开（非简单难度）
                                if (player.ReadyForDash && difficulty != mlpAiDifficulty.Easy)
                                {
                                    CurrentDash = -player.Side;
                                    CurrentMove = 0;
                                }
                                else
                                {
                                    CurrentMove = UnityEngine.Random.value <= 0.5f ? -player.Side : 0;
                                    moveDelay.Activate();
                                }
                            }
                            else if (UnityEngine.Random.value <= profile.ReactOnOpponent)
                            {
                                // 5h. 有一定概率对背后防守做出反应
                                CurrentJump = false;
                                if (player.ReadyForDash && InDashingZone() && UnityEngine.Random.value <= profile.MakeDash && difficulty != mlpAiDifficulty.Easy)
                                {
                                    CurrentDash = -player.Side;
                                }
                                else
                                {
                                    CurrentMove = UnityEngine.Random.value <= 0.5f ? 0 : player.Side;
                                    moveDelay.Activate();
                                }
                            }
                            else
                            {
                                // 5i. 没反应过来 → 继续向篮筐方向移动
                                CurrentMove = -player.Side;
                                moveDelay.Activate();
                            }
                        }
                        else
                        {
                            // 5j. 对手不在后面 → 安全地向篮筐推进
                            CurrentMove = -player.Side;
                        }
                    }

                    // 5k. 如果已决定起跳投篮，激活攻击计时器并计算飞行方向
                    if (attackJump)
                    {
                        attack.Activate();
                        directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                    }
                }
            }
            else
            {
                // 6. 在空中时：保持飞行方向，攻击计时器到期时执行投篮
                CurrentMove = (player.Position.x - attackPoint) * directionToFly > 0f ? Mathf.RoundToInt(directionToFly) : 0;
                CurrentJump = false;
                CurrentAction = attack.Update(dt) == 1;
            }
        }

        /// <summary>
        /// 站定不动，根据跳球延迟配置把握起跳时机。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyJumpBall(float dt)
        {
            CurrentMove = 0;
            CurrentJump = jumpBall.Update(dt) == 1;
            CurrentAction = false;
        }

        /// <summary>
        /// 为篮板调整位置，当球进入篮板区域时起跳。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyRebound(float dt)
        {
            // 1. 如果拥有"必定盖帽"大招且对手投出可盖帽的球，触发大招
            if (player.UsesGuaranteedBlockSkill && player.ReadyForSuper && ball != null && ball.IsBlockable && ball.Side != player.Side)
            {
                TriggerSuperInput();
                return;
            }

            // 2. 如果拥有"篮板磁铁"大招且球在篮筐附近弹跳，触发大招
            if (player.UsesReboundMagnetSkill && player.ReadyForSuper && ball != null && ball.State == "basket")
            {
                TriggerSuperInput();
                return;
            }

            // 3. 如果球在篮筐附近弹跳，尝试超级冲刺抢占篮板位置
            if (TryUseDelayedSuperDash(dt, ball != null && ball.State == "basket" && ShouldUseSuperDashForBall()))
            {
                return;
            }

            // 4. 处理"提前排队的起跳"指令（比如对手投篮时提前起跳干扰）
            var contestJump = queuedReboundJump && player.IsGrounded;
            if (contestJump)
            {
                queuedReboundJump = false;
            }

            // 5. 当球在身边（水平近、垂直远）或者有排队起跳时才起跳
            CurrentJump = contestJump || (Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f);
            // 6. 地狱难度使用弹道预测落点，其他难度用固定篮板位置
            var targetReboundPoint = ShouldUseTechnicalPrediction() ? GetTechnicalReboundTargetX() : reboundPoint;
            // 7. 起跳时不动，落地后向篮板位置移动
            CurrentMove = CurrentJump ? 0 : player.IsGrounded ? MoveTo(targetReboundPoint) : 0;
            // 8. 篮板阶段不执行投篮/抢断动作
            CurrentAction = false;
        }

        /// <summary>
        /// 根据距离和难度配置，在靠近对手时进行抢断判定。
        /// </summary>
        protected void TryToSteal()
        {
            // 1. 简单难度不抢断；对手为空或在空中时也不抢断
            if (difficulty == mlpAiDifficulty.Easy || opponent == null || !opponent.IsGrounded)
            {
                return;
            }

            // 2. 如果从后面接近对手，按技能概率决定是否发起抢断
            if (IsOpponentCloseBehind(GetStealBehindDistance()))
            {
                if (UnityEngine.Random.value <= profile.MakeSteal)
                {
                    stealDelay.Activate();
                }
                else
                {
                    stealDelay.SkipIt();
                }
            }
            // 3. 如果对手在篮筐附近（更危险），抢断概率提高到 1.5 倍
            else if (IsOpponentCloseToBasket(GetStealBasketDistance()))
            {
                if (UnityEngine.Random.value <= 1.5f * profile.MakeSteal)
                {
                    stealDelay.Activate();
                }
                else
                {
                    stealDelay.SkipIt();
                }
            }
        }

        /// <summary>
        /// 在条件和冷却允许的情况下，以短暂延迟激活超级冲刺。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        /// <param name="shouldUse">当 AI 应该尝试超级冲刺时为 true</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool TryUseDelayedSuperDash(float dt, bool shouldUse)
        {
            // 1. 检查是否有可用的超级冲刺（角色自带或地狱难度奖励），以及当前场景是否值得使用
            var canUseNativeSuperDash = player.UsesDashSkill && player.ReadyForSuper;
            var canUseHellBonusSuperDash = player.CanUseHellBonusSuperDash;
            // 2. 如果没有可用的超级冲刺或者场景不需要，重置计时器并返回
            if ((!canUseNativeSuperDash && !canUseHellBonusSuperDash) || !shouldUse)
            {
                superDashDelay.Reset();
                return false;
            }

            // 3. 更新超级冲刺延迟计时器
            var state = superDashDelay.Update(dt);
            // 4. 计时器首次激活（-1 表示刚开始计时）
            if (state == -1)
            {
                superDashDelay.Activate();
                return false;
            }

            // 5. 计时器还没到期，继续等待
            if (state != 1)
            {
                return false;
            }

            // 6. 计时器到期 → 使用角色自带的超级冲刺
            if (canUseNativeSuperDash)
            {
                TriggerSuperInput();
                superDashDelay.Reset();
                return true;
            }

            // 7. 尝试使用地狱难度奖励的超级冲刺
            if (player.TryUseHellBonusSuperDash())
            {
                superDashDelay.Reset();
                return true;
            }

            // 8. 两种都失败了，重置计时器
            superDashDelay.Reset();
            return false;
        }

        /// <summary>
        /// 设置必杀输入标志并清除其他输入以激活必杀技。
        /// </summary>
        protected void TriggerSuperInput()
        {
            ResetCurrents();
            CurrentSuper = true;
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// 当持球对手处于理想的超级冲刺范围内时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldUseSuperDashAgainstHolder()
        {
            if (opponent == null || !opponent.WithBall || !player.IsGrounded)
            {
                return false;
            }

            var distance = Mathf.Abs(player.Position.x - opponent.Position.x);
            return distance >= GetHolderSuperDashMinDistance() && distance <= GetHolderSuperDashMaxDistance();
        }

        /// <summary>
        /// 当自由球足够高且足够远，值得使用超级冲刺时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldUseSuperDashForBall()
        {
            if (ball == null || !ball.IsInGame || !player.IsGrounded)
            {
                return false;
            }

            if (ball.State == "shooting" || ball.State == "score" || ball.State == "alleyOop")
            {
                return false;
            }

            return ball.Position.y > mlpObjectsData.BasketHeight &&
                   Mathf.Abs(DeltaBallX()) >= GetLooseBallSuperDashDistance();
        }

        /// <summary>
        /// 当对手从后方施压或篮筐距离足够远值得超级冲刺时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldUseSuperDashInAttack()
        {
            if (!player.WithBall || !player.IsGrounded)
            {
                return false;
            }

            if (opponent != null && IsOpponentCloseBehind(GetAttackPressureDistance()))
            {
                return true;
            }

            return Mathf.Abs(player.Position.x - player.AttackTargetX) >= GetAttackSuperDashDistance() &&
                   InDashingZone();
        }

        /// <summary>
        /// 初始化进攻模式：设置攻击点、清除延迟、准备投篮路线。
        /// </summary>
        protected void HandleBallInOwnHands()
        {
            // 1. 重置所有延迟计时器和输入状态，切换到进攻策略（策略编号 2）
            ResetBaseDelays();
            ResetCurrents();
            queuedReboundJump = false;
            strategy = 2;
            superDashDelay.Reset();

            // 2. 根据球员在场上的位置，决定投篮目标点
            var reboundZone = IsReboundInAttackZone();
            if (reboundZone == -1)
            {
                // 2a. 在进攻区域之前（离篮筐远）→ 设定一个近筐攻击点，如果在空中则立即准备投篮
                willAttackAtOnce = !player.IsGrounded;
                SetAttackPoint(150f, player.Position.x);
            }
            else if (reboundZone == 0)
            {
                // 2b. 在进攻区域内 → 在当前位置投篮，如果在空中则立即投
                willAttackAtOnce = !player.IsGrounded;
                var currentX = player.Position.x;
                SetAttackPoint(currentX, currentX);
            }
            else
            {
                // 2c. 在进攻区域之后（已深入对方半场）→ 自动选择最佳投篮点
                SetAttackPoint(0f, 0f);
                willAttackAtOnce = !player.IsGrounded && Mathf.Abs(player.Position.x - attackPoint) < 50f;
            }
        }

        /// <summary>
        /// 初始化防守模式：识别对手持球者并清除延迟。
        /// </summary>
        protected void HandleBallInOpponentsHands()
        {
            // 1. 切换到防守策略（编号 0）
            strategy = 0;
            // 2. 重置所有延迟计时器和当前输入
            ResetBaseDelays();
            ResetCurrents();
            // 3. 清除篮板、进攻、假动作等残留状态
            queuedReboundJump = false;
            willAttackAtOnce = false;
            isPumped = false;
            superDashDelay.Reset();
            // 4. 找到当前持球的对手作为防守目标
            opponent = player.GameCore.FindBallHolder(-player.Side);
        }

        /// <summary>
        /// 初始化争夺球模式：清除延迟并准备追逐球。
        /// </summary>
        protected void HandleBallOthers()
        {
            // 1. 切换到争球策略（编号 1）
            strategy = 1;
            // 2. 重置当前输入
            ResetCurrents();
            // 3. 清除排队篮板起跳和超级冲刺延迟
            queuedReboundJump = false;
            superDashDelay.Reset();
        }

        /// <summary>
        /// 响应游戏信号（抢断、跳跃、虚晃、冲刺、眩晕），调整 AI 输入和计时器。
        /// </summary>
        /// <param name="signal">玩家事件信号类型</param>
        /// <param name="side">场地方向（-1 为左侧，1 为右侧）</param>
        /// <param name="signalPlayerNo">触发信号的玩家编号</param>
        protected void ProcessPlayerSignal(mlpPlayerSignalType signal, int side, int signalPlayerNo)
        {
            // 1. 收到"开始抢断"信号 → 处理抢断反应（躲避或跟进）
            if (signal == mlpPlayerSignalType.StartSteal)
            {
                PlayerStartSteal(side);
                return;
            }

            // 2. 收到"抢断完成"信号 → 如果是对手的抢断，清除躲避状态
            if (signal == mlpPlayerSignalType.Steal)
            {
                if (side == -player.Side)
                {
                    ResetAvoidSteal();
                }

                return;
            }

            // 3. 收到"起跳"信号
            if (signal == mlpPlayerSignalType.JumpA)
            {
                if (side == player.Side && signalPlayerNo == playerNo)
                {
                    // 3a. 自己起跳 → 清除躲避状态，激活攻击计时器，记录飞行方向
                    ResetAvoidSteal();
                    attack.Activate();
                    directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                }
                else if (side == -player.Side)
                {
                    // 3b. 对手起跳 → 根据难度和概率决定是否跟着起跳干扰投篮
                    if (ShouldUsePerfectContestOnJump() || UnityEngine.Random.value <= profile.JumpThrow)
                    {
                        defenceDelay.Activate();
                    }
                }

                return;
            }

            // 4. 收到"假动作"信号 → 对手做假动作时可能被骗起跳
            if (signal == mlpPlayerSignalType.Pump)
            {
                if (side == -player.Side && player.CanAct && IsOpponentCloseBehind(90f))
                {
                    // 4a. 限制最多被骗 3 次
                    if (++pumpCount <= 3)
                    {
                        // 4b. 地狱难度下可能识破假动作，不被骗
                        if (ShouldIgnorePumpFake())
                        {
                            return;
                        }

                        // 4c. 按概率被骗：起跳防守并停下移动，标记为"被晃飞"
                        if (UnityEngine.Random.value <= profile.JumpPump)
                        {
                            defenceDelay.Activate();
                            stealDelay.Reset();
                            CurrentMove = 0;
                            isPumped = true;
                        }
                    }
                }

                return;
            }

            // 5. 收到"冲刺"信号
            if (signal == mlpPlayerSignalType.Dash)
            {
                if (side == player.Side)
                {
                    // 5a. 队友冲刺 → 重置攻击计时器（队友在跑，重新规划）
                    attack.Reset();
                }
                else if (strategy == 0 && player.CanAct && IsOpponentInRangeBehind(40f, GetDashBlockRangeMaxDistance()))
                {
                    // 5b. 对手在身后冲刺 → 按概率尝试盖帽
                    if (UnityEngine.Random.value <= profile.MakeBlock)
                    {
                        ResetCurrents();
                        ResetAllDelays();
                        CurrentBlockOrPump = true;
                        blockDelay.Activate();
                    }
                }

                return;
            }

            // 6. 收到"眩晕"信号 → 如果是自己被眩晕，重置所有计时器
            if (signal == mlpPlayerSignalType.Stun && side == player.Side)
            {
                ResetAllDelays();
            }
        }

        /// <summary>
        /// 当对手开始抢断时做出反应，如果持球则尝试躲避。
        /// </summary>
        /// <param name="side">场地方向（-1 为左侧，1 为右侧）</param>
        protected void PlayerStartSteal(int side)
        {
            if (side == -player.Side)
            {
                if (player.WithBall && player.IsGrounded && (IsOpponentCloseBehind(80f) || (IsOpponentCloseBehind(140f) && opponent != null && opponent.IsMoving)))
                {
                    TryToAvoid();
                }
            }
            else
            {
                stealDelay.UseIt();
            }
        }

        /// <summary>
        /// 通过冲刺、跳跃或侧移来躲避即将到来的抢断。
        /// </summary>
        protected void TryToAvoid()
        {
            if (UnityEngine.Random.value > profile.AvoidSteal || player.Position.x > 600f)
            {
                return;
            }

            var chance = UnityEngine.Random.value;
            if (chance <= 0.1f && player.ReadyForDash)
            {
                CurrentDash = -player.Side;
                return;
            }

            if (chance <= 0.4f && IsInAttackZone())
            {
                avoidStealJump = true;
                moveDelay.Reset();
                return;
            }

            avoidStealMove = player.Side;
        }

        /// <summary>
        /// 返回干扰投篮的距离阈值，来自难度调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetDefenceContestDistance()
        {
            return tuning.DefenceContestDistance;
        }

        /// <summary>
        /// 返回从背后抢断的距离阈值，来自难度调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetStealBehindDistance()
        {
            return tuning.StealBehindDistance;
        }

        /// <summary>
        /// 返回篮筐附近抢断的距离阈值，来自难度调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetStealBasketDistance()
        {
            return tuning.StealBasketDistance;
        }

        /// <summary>
        /// 返回超级冲刺所需的最小持球者距离，来自调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetHolderSuperDashMinDistance()
        {
            return tuning.HolderSuperDashMinDistance;
        }

        /// <summary>
        /// 返回超级冲刺所需的最大持球者距离，来自调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetHolderSuperDashMaxDistance()
        {
            return tuning.HolderSuperDashMaxDistance;
        }

        /// <summary>
        /// 返回自由球超级冲刺所需的最小球距，来自调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetLooseBallSuperDashDistance()
        {
            return tuning.LooseBallSuperDashDistance;
        }

        /// <summary>
        /// 返回进攻中触发超级冲刺逃脱的后方距离阈值，来自调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetAttackPressureDistance()
        {
            return tuning.AttackPressureDistance;
        }

        /// <summary>
        /// 返回进攻中值得使用超级冲刺的篮筐距离，来自调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetAttackSuperDashDistance()
        {
            return tuning.AttackSuperDashDistance;
        }

        /// <summary>
        /// 返回 AI 尝试冲刺盖帽的最大距离，来自调校配置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetDashBlockRangeMaxDistance()
        {
            return tuning.DashBlockRangeMaxDistance;
        }

        /// <summary>
        /// 在地狱难度下，当人类玩家投出威胁性投篮时激活额外护盾。
        /// </summary>
        protected void TryUseHellBonusShieldAgainstHumanShot()
        {
            if (difficulty != mlpAiDifficulty.Hell || opponent == null || !opponent.IsHuman)
            {
                return;
            }

            var threateningShot =
                player.GameCore.IsCurrentShotThreePointer(opponent.Side) ||
                player.GameCore.RemainingMatchTime <= 15f ||
                player.GameCore.GetScoreLeadForSide(opponent.Side) >= 4;

            if (threateningShot)
            {
                player.TryUseHellBonusShield();
            }
        }

        /// <summary>
        /// 根据玩家的位置方向和编号计算进攻、冲刺、防守和篮板位置。
        /// </summary>
        protected void InitZones()
        {
            // 1. 根据玩家所在阵营（左=1，右=-1）设置进攻和冲刺区域的边界
            if (player.Side == 1)
            {
                // 1a. 右侧阵营：直接使用配置中的区域值
                attackZoneStart = mlpObjectsData.AttackZoneStart;
                attackZoneEnd = mlpObjectsData.AttackZoneEnd;
                dashZoneStart = mlpObjectsData.DashZoneStart;
                dashZoneEnd = mlpObjectsData.DashZoneEnd;
                // 1b. 根据球员编号（0=主力，1=辅助）设置不同的防守/篮板位置
                if (playerNo == 0)
                {
                    baseEndPoint = 280f;
                    reboundPointInAttack = 190f;
                    reboundPointInDefence = 610f;
                }
                else
                {
                    baseEndPoint = 400f;
                    reboundPointInAttack = 150f;
                    reboundPointInDefence = 680f;
                }
            }
            else
            {
                // 1c. 左侧阵营：将坐标镜像翻转（场地关于中心对称）
                attackZoneStart = mlpConstants.Width - mlpObjectsData.AttackZoneEnd;
                attackZoneEnd = mlpConstants.Width - mlpObjectsData.AttackZoneStart;
                dashZoneStart = mlpConstants.Width - mlpObjectsData.DashZoneEnd;
                dashZoneEnd = mlpConstants.Width - mlpObjectsData.DashZoneStart;
                if (playerNo == 0)
                {
                    baseEndPoint = 580f;
                    reboundPointInAttack = 610f;
                    reboundPointInDefence = 190f;
                }
                else
                {
                    baseEndPoint = 400f;
                    reboundPointInAttack = 650f;
                    reboundPointInDefence = 120f;
                }
            }

            // 2. 设置防守站位点和默认终点位置
            defensePoint = player.Side == -1 ? mlpObjectsData.DefensePoint : mlpConstants.Width - mlpObjectsData.DefensePoint;
            endPoint = baseEndPoint;
        }

        /// <summary>
        /// 首次使用时从游戏核心延迟获取球和对手的引用。
        /// </summary>
        protected void EnsureRuntimeLinks()
        {
            if (initialized)
            {
                return;
            }

            ball = player.GameCore.Ball;
            opponents = player.Side == -1
                ? new List<mlpPlayerObject>(player.GameCore.PlayersRight)
                : new List<mlpPlayerObject>(player.GameCore.PlayersLeft);
            opponent = opponents.Count > 0 ? opponents[0] : null;
            initialized = true;
        }

        /// <summary>
        /// 为新回合重置所有 AI 状态（策略、延迟、输入、虚晃计数）。
        /// </summary>
        protected void ResetForRestart()
        {
            // 1. 设置默认策略为"跳球"（编号 3），重置无聊等待计时
            strategy = 3;
            deltaDownTime = 0f;
            // 2. 恢复默认防守终点位置
            endPoint = baseEndPoint;
            // 3. 重置所有延迟计时器和当前输入
            ResetAllDelays();
            ResetCurrents();
            // 4. 清除盖帽、大招、排队大招等状态
            CurrentBlockOrPump = false;
            CurrentSuper = false;
            queuedSuperInput = false;
            // 5. 清除假动作被骗状态和计数
            isPumped = false;
            pumpCount = 0;
            // 6. 清除排队篮板起跳和立即进攻标志
            queuedReboundJump = false;
            willAttackAtOnce = false;
            // 7. 篮板位置默认为防守篮板位置
            reboundPoint = reboundPointInDefence;
            // 8. 清除抢断躲避状态
            ResetAvoidSteal();
        }

        /// <summary>
        /// 将所有控制器输入标志归零（移动、跳跃、动作、冲刺）。
        /// </summary>
        protected void ResetCurrents()
        {
            CurrentMove = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentDash = 0;
        }

        /// <summary>
        /// 重置进攻、防守、移动、抢断和攻击跳跃的延迟计时器。
        /// </summary>
        protected void ResetBaseDelays()
        {
            attackJumpDelay.Reset();
            attack.Reset();
            defenceDelay.Reset();
            moveDelay.Reset();
            stealDelay.Reset();
            ResetAvoidSteal();
        }

        /// <summary>
        /// 重置所有延迟计时器，包括冲刺决策和超级冲刺计时器。
        /// </summary>
        protected void ResetAllDelays()
        {
            dashDecisionDelay.Reset();
            superDashDelay.Reset();
            ResetBaseDelays();
        }

        /// <summary>
        /// 清除抢断躲避的跳跃和移动标志。
        /// </summary>
        protected void ResetAvoidSteal()
        {
            avoidStealJump = false;
            avoidStealMove = 0;
        }

        /// <summary>
        /// 在对手列表中搜索指定编号的玩家，未找到则返回 null。
        /// </summary>
        /// <param name="targetPlayerNo">要搜索的玩家编号</param>
        /// <returns>搜索结果。</returns>
        protected mlpPlayerObject FindOpponentByPlayerNo(int targetPlayerNo)
        {
            if (opponents == null)
            {
                return null;
            }

            for (var i = 0; i < opponents.Count; i++)
            {
                if (opponents[i] != null && opponents[i].PlayerNo == targetPlayerNo)
                {
                    return opponents[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 选择投篮的 X 位置，使用基于距离的随机化和关键球逻辑。
        /// </summary>
        /// <param name="point">攻击 X 位置（0 表示自动选择）</param>
        /// <param name="jump">跳跃点 X 位置，用于决定何时起跳投篮</param>
        protected void SetAttackPoint(float point, float jump)
        {
            // 1. 如果调用方指定了具体的攻击点（非 0），直接使用
            if (!Mathf.Approximately(point, 0f))
            {
                attackPoint = point;
                jumpPoint = jump;
            }
            else
            {
                // 2. 没有指定攻击点 → 根据比赛情况和概率自动选择投篮位置
                if (ShouldForceClutchThree())
                {
                    // 2a. 比赛快结束且落后 → 强制选择三分线位置
                    attackPoint = 500f + 24f * UnityEngine.Random.value;
                }
                else if (ShouldPreferSafeClutchTwo())
                {
                    // 2b. 比赛快结束且领先 → 选择安全的近距离两分位置
                    attackPoint = 140f + 80f * UnityEngine.Random.value;
                }
                else if ((player.Position.x - 450f) * player.Side > 0f && UnityEngine.Random.value <= mlpObjectsData.ChanceForThree)
                {
                    // 2c. 已经在对方半场且随机命中 → 尝试三分
                    attackPoint = 510f;
                }
                else if (UnityEngine.Random.value <= 0.7f)
                {
                    // 2d. 大概率选择中距离投篮位置
                    attackPoint = 120f + 200f * UnityEngine.Random.value;
                }
                else
                {
                    // 2e. 小概率选择远距离中投位置
                    attackPoint = 320f + 160f * UnityEngine.Random.value;
                }

                // 3. 跳跃点：近距离投篮需要先跳到更靠近篮筐的位置再出手
                jumpPoint = attackPoint <= 200f ? attackPoint + 100f : attackPoint;
            }

            // 4. 左侧阵营需要将 X 坐标镜像翻转
            if (player.Side == -1)
            {
                attackPoint = mlpConstants.Width - attackPoint;
                jumpPoint = mlpConstants.Width - jumpPoint;
            }
        }

        /// <summary>
        /// 在地狱难度下返回 true，启用基于弹道的球路预测。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldUseTechnicalPrediction()
        {
            return difficulty == mlpAiDifficulty.Hell;
        }

        /// <summary>
        /// 在地狱难度下预测自由球的落点，否则返回球当前的 X 坐标。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetTechnicalLooseBallTargetX()
        {
            if (!ShouldUseTechnicalPrediction() || ball == null)
            {
                return ball != null ? ball.Position.x : player.Position.x;
            }

            if (ball.State == "bounce" || ball.State == "steal")
            {
                return ball.Position.x;
            }

            return ball.PredictFloorLandingX();
        }

        /// <summary>
        /// 在地狱难度下预测投失球的落点，否则返回固定的篮板位置。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float GetTechnicalReboundTargetX()
        {
            if (!ShouldUseTechnicalPrediction() || ball == null)
            {
                return reboundPoint;
            }

            if (ball.State == "shooting" || ball.State == "basket" || ball.State == "block" || ball.State == "dunk" || ball.State == "alleyOop")
            {
                return ball.PredictFloorLandingX();
            }

            return reboundPoint;
        }

        /// <summary>
        /// 当 AI 在比赛末段落后时返回 true，应强制投三分球。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldForceClutchThree()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 12f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) <= -2;
        }

        /// <summary>
        /// 当 AI 在比赛末段领先时返回 true，应选择安全的两分球。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldPreferSafeClutchTwo()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 15f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) >= 3;
        }

        /// <summary>
        /// 在困难或地狱难度下返回 true，启用末节投篮选择逻辑。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool SupportsClutchShotSelection()
        {
            return difficulty == mlpAiDifficulty.Hard || difficulty == mlpAiDifficulty.Hell;
        }

        /// <summary>
        /// 在地狱难度下，当对手靠近且投出威胁性投篮时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldUsePerfectContestOnJump()
        {
            return difficulty == mlpAiDifficulty.Hell &&
                   IsOpponentImmediateShotThreat() &&
                   IsOpponentCloseAbs(GetDefenceContestDistance() + 24f);
        }

        /// <summary>
        /// 在地狱难度下，当对手不太可能真正投篮时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool ShouldIgnorePumpFake()
        {
            if (difficulty != mlpAiDifficulty.Hell)
            {
                return false;
            }

            if (pumpCount > 1)
            {
                return false;
            }

            return !IsOpponentImmediateShotThreat();
        }

        /// <summary>
        /// 当对手接近其攻击目标或比赛时间即将结束时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsOpponentImmediateShotThreat()
        {
            if (opponent == null || !opponent.WithBall)
            {
                return false;
            }

            var distanceToTargetBasket = Mathf.Abs(opponent.Position.x - opponent.AttackTargetX);
            if (distanceToTargetBasket <= 220f)
            {
                return true;
            }

            return player.GameCore.RemainingMatchTime <= 8f &&
                   player.GameCore.GetScoreLeadForSide(opponent.Side) <= 0;
        }

        /// <summary>
        /// 返回 -1、0 或 1，表示玩家在进攻区域之前、之内还是之后。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected int IsReboundInAttackZone()
        {
            var playerX = player.Position.x;
            var zone = 0;
            if ((playerX - attackZoneStart) * player.Side <= 0f)
            {
                zone = -1;
            }
            else if ((playerX - attackZoneEnd) * player.Side >= 0f)
            {
                zone = 1;
            }

            return zone;
        }

        /// <summary>
        /// 返回 -1、0 或 1，表示朝给定 X 位置移动的方向。
        /// </summary>
        /// <param name="x">像素空间中的水平坐标</param>
        /// <returns>计算结果。</returns>
        protected int MoveTo(float x)
        {
            var delta = player.Position.x - x;
            return Mathf.Abs(delta) <= DeltaDistance ? 0 : delta > 0f ? -1 : 1;
        }

        /// <summary>
        /// 决定向跳跃点还是攻击点移动，同时设置攻击跳跃标志。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected int MoveInAttack()
        {
            // 1. 先尝试向跳跃点移动
            var move = MoveTo(jumpPoint);
            if (move == 0)
            {
                // 2. 已到达跳跃点 → 标记"可以起跳投篮"，再判断是否需要继续走向攻击点
                attackJump = true;
                move = Mathf.Approximately(jumpPoint, attackPoint) ? 0 : MoveTo(attackPoint);
            }
            else
            {
                // 3. 还没到跳跃点 → 标记"不要跳"，向攻击点移动（先走大致方向）
                attackJump = false;
                move = MoveTo(attackPoint);
            }

            return move;
        }

        /// <summary>
        /// 返回玩家到球的水平距离（正值表示球在玩家左侧）。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float DeltaBallX()
        {
            return player.Position.x - ball.Position.x;
        }

        /// <summary>
        /// 返回玩家到球的垂直距离。
        /// </summary>
        /// <returns>计算结果。</returns>
        protected float DeltaBallY()
        {
            return player.Position.y - ball.Position.y;
        }

        /// <summary>
        /// 当球水平距离近而垂直距离远时返回 true，表示篮板机会。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsBallInReboundZone()
        {
            return Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
        }

        /// <summary>
        /// 当对手在玩家后方指定距离内时返回 true。
        /// </summary>
        /// <param name="distance">到目标篮筐的估计距离</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsOpponentCloseBehind(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta > 0f && delta <= distance;
        }

        /// <summary>
        /// 当对手在玩家后方且处于最小-最大距离范围内时返回 true。
        /// </summary>
        /// <param name="min">最小距离阈值</param>
        /// <param name="max">最大距离阈值</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsOpponentInRangeBehind(float min = 40f, float max = 180f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta > 0f && delta >= min && delta <= max;
        }

        /// <summary>
        /// 当对手在玩家和篮筐之间，且在指定距离内时返回 true。
        /// </summary>
        /// <param name="distance">到目标篮筐的估计距离</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsOpponentCloseToBasket(float distance = 30f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta < 0f && delta + distance >= 0f;
        }

        /// <summary>
        /// 当 AI 玩家比对手更靠近对方篮筐时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsAICloserForBasket()
        {
            if (opponent == null)
            {
                return false;
            }

            return (player.Position.x - opponent.Position.x) * player.Side < 0f;
        }

        /// <summary>
        /// 当玩家站在自己篮筐下方时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsUnderOwnBasket()
        {
            return player.Side == 1 ? player.Position.x > 700f : player.Position.x < 100f;
        }

        /// <summary>
        /// 当玩家的 X 位置处于配置的冲刺区域内时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool InDashingZone()
        {
            return player.Position.x >= dashZoneStart && player.Position.x <= dashZoneEnd;
        }

        /// <summary>
        /// 当与对手的绝对水平距离在阈值内时返回 true。
        /// </summary>
        /// <param name="distance">到目标篮筐的估计距离</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsOpponentCloseAbs(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            return Mathf.Abs(player.Position.x - opponent.Position.x) <= distance;
        }

        /// <summary>
        /// 当玩家位于对手半场时返回 true。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        protected bool IsInAttackZone()
        {
            return player.Side == 1 ? player.Position.x < 600f : player.Position.x > 200f;
        }
    }
}
