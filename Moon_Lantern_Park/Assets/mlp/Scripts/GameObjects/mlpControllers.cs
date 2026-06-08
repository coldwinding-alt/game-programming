// 文件作用：玩家输入控制器（键盘和 AI）
// 概括：定义玩家用什么方式控制角色：键盘玩家 1 用 WASD 控制，键盘玩家 2 用方向键控制，AI 由电脑自动控制。每帧读取按键状态，告诉角色往哪移动、是否跳跃、是否投篮。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
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

    public sealed class mlpKeyboardController : IBLPlayerController
    {
        private readonly mlpControlProfile controls;
        private float lastLeftDown = -10f;
        private float lastRightDown = -10f;
        private float lastLeftUp = -10f;
        private float lastRightUp = -10f;

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
            controls = mlpControlsData.ProfileForBrain(brain);
        }

        /// <summary>
        /// 每帧读取键盘输入：移动、跳跃、投篮、盖帽、必杀技和冲刺双击。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void UpdateController(float dt)
        {
            CurrentMove = 0;
            CurrentDash = 0;
            var leftDown = controls.MoveLeftKey;
            var rightDown = controls.MoveRightKey;
            var currentTime = Time.time;

            if (Input.GetKeyUp(leftDown))
            {
                lastLeftUp = currentTime;
            }

            if (Input.GetKeyUp(rightDown))
            {
                lastRightUp = currentTime;
            }

            if (Input.GetKeyDown(leftDown))
            {
                // 同时接受快速连按和经典双击，降低冲刺操作的帧精度要求。
                if (currentTime - lastLeftDown <= mlpObjectsData.DashDoubleTapWindow
                    || currentTime - lastLeftUp <= mlpObjectsData.DashDoubleTapWindow)
                {
                    CurrentDash = -1;
                }

                lastLeftDown = currentTime;
            }

            if (Input.GetKeyDown(rightDown))
            {
                if (currentTime - lastRightDown <= mlpObjectsData.DashDoubleTapWindow
                    || currentTime - lastRightUp <= mlpObjectsData.DashDoubleTapWindow)
                {
                    CurrentDash = 1;
                }

                lastRightDown = currentTime;
            }

            if (Input.GetKey(leftDown))
            {
                CurrentMove--;
            }

            if (Input.GetKey(rightDown))
            {
                CurrentMove++;
            }

            CurrentJump = Input.GetKey(controls.JumpKey);
            CurrentAction = Input.GetKey(controls.ActionKey);
            CurrentBlockOrPump = Input.GetKey(controls.BlockKey);
            CurrentSuper = Input.GetKey(controls.SuperKey);
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
        }

        /// <summary>
        /// 键盘控制器无需操作；输入每帧都会读取。
        /// </summary>
        public void PlayerOnBlock()
        {
        }
    }

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
            this.player = player;
            difficulty = mlpInventory.Instance.Difficulty;
            tuning = mlpAIDifficultyTuning.Get(difficulty);
            profile = mlpAISkillsData.Get(skillLevel);
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
            playerNo = player.PlayerNo;
            player.GameCore.PlayerSignals.OnSignal += ProcessPlayerSignal;
            InitZones();
            ResetForRestart();
        }

        /// <summary>
        /// 运行完整的 AI 决策循环：根据球的状态选择策略，然后调用对应的策略方法。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public virtual void UpdateController(float dt)
        {
            EnsureRuntimeLinks();
            CurrentDash = 0;
            CurrentSuper = queuedSuperInput;
            queuedSuperInput = false;

            if (ball == null || opponents == null || opponents.Count == 0)
            {
                CurrentMove = 0;
                CurrentJump = false;
                CurrentAction = false;
                CurrentSuper = false;
                return;
            }

            var delayedJump = attackJumpDelay.Update(dt);
            if (delayedJump >= 0)
            {
                if (delayedJump == 1)
                {
                    CurrentMove = 0;
                    CurrentJump = true;
                    CurrentAction = false;
                }
                else
                {
                    CurrentMove = avoidStealMove;
                    CurrentJump = avoidStealJump;
                }

                return;
            }

            var holder = player.GameCore.FindBallHolder();
            if (player.WithBall)
            {
                if (strategy != 2)
                {
                    HandleBallInOwnHands();
                }

                StrategyAttack(dt);
            }
            else if (holder != null && holder.Side != player.Side)
            {
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
                var shotInFlight = ball.State == "shooting" || ball.State == "basket";
                if (shotInFlight)
                {
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
            if (opponent == null)
            {
                return;
            }

            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashAgainstHolder()))
            {
                return;
            }

            var stealState = stealDelay.Update(dt);
            var moveState = moveDelay.Update(dt);
            if (isPumped)
            {
                CurrentMove = 0;
            }
            else
            {
                var target = (opponent.Position.x - endPoint) * player.Side < 0f
                    ? endPoint
                    : opponent.IsGrounded
                        ? opponent.Position.x + player.Side * mlpObjectsData.OpponentDelta
                        : opponent.Position.x + player.Side * (mlpObjectsData.OpponentDelta - 10f);

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

                if (stealState == -1)
                {
                    TryToSteal();
                }
            }

            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseAbs(GetDefenceContestDistance());
            CurrentAction = stealState == 1;
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
            var ballX = GetTechnicalLooseBallTargetX();
            var offset = ballX - player.Position.x >= 0f ? 10f : -10f;
            CurrentMove = MoveTo(ballX + offset);
            CurrentJump = false;

            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashForBall()))
            {
                return;
            }

            if (ball.State != "bounce" && ball.State != "shooting")
            {
                if (ball.State == "basket")
                {
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
                    CurrentJump = Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
                }
            }

            CurrentAction = false;
        }

        /// <summary>
        /// 向攻击点移动，把握投篮起跳时机，并对附近的防守者做出反应。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        protected virtual void StrategyAttack(float dt)
        {
            if (!player.WithBall)
            {
                return;
            }

            if (player.UsesPossessionSkill && megaDunkDelay.Update(dt) == 1)
            {
                TriggerSuperInput();
                return;
            }

            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashInAttack()))
            {
                return;
            }

            if (avoidStealJump || avoidStealMove != 0)
            {
                CurrentMove = avoidStealMove;
                CurrentJump = avoidStealJump;
                return;
            }

            if (player.IsGrounded)
            {
                if (moveDelay.Update(dt) == -1)
                {
                    var move = MoveInAttack();
                    if (attackJump)
                    {
                        CurrentJump = true;
                        CurrentMove = move;
                    }
                    else if (IsAICloserForBasket())
                    {
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
                        CurrentJump = false;
                        CurrentDash = 0;
                        if (IsOpponentCloseBehind())
                        {
                            if (IsUnderOwnBasket())
                            {
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
                                CurrentMove = -player.Side;
                                moveDelay.Activate();
                            }
                        }
                        else
                        {
                            CurrentMove = -player.Side;
                        }
                    }

                    if (attackJump)
                    {
                        attack.Activate();
                        directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                    }
                }
            }
            else
            {
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
            if (player.UsesGuaranteedBlockSkill && player.ReadyForSuper && ball != null && ball.IsBlockable && ball.Side != player.Side)
            {
                TriggerSuperInput();
                return;
            }

            if (player.UsesReboundMagnetSkill && player.ReadyForSuper && ball != null && ball.State == "basket")
            {
                TriggerSuperInput();
                return;
            }

            if (TryUseDelayedSuperDash(dt, ball != null && ball.State == "basket" && ShouldUseSuperDashForBall()))
            {
                return;
            }

            var contestJump = queuedReboundJump && player.IsGrounded;
            if (contestJump)
            {
                queuedReboundJump = false;
            }

            CurrentJump = contestJump || (Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f);
            var targetReboundPoint = ShouldUseTechnicalPrediction() ? GetTechnicalReboundTargetX() : reboundPoint;
            CurrentMove = CurrentJump ? 0 : player.IsGrounded ? MoveTo(targetReboundPoint) : 0;
            CurrentAction = false;
        }

        /// <summary>
        /// 根据距离和难度配置，在靠近对手时进行抢断判定。
        /// </summary>
        protected void TryToSteal()
        {
            if (difficulty == mlpAiDifficulty.Easy || opponent == null || !opponent.IsGrounded)
            {
                return;
            }

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
            var canUseNativeSuperDash = player.UsesDashSkill && player.ReadyForSuper;
            var canUseHellBonusSuperDash = player.CanUseHellBonusSuperDash;
            if ((!canUseNativeSuperDash && !canUseHellBonusSuperDash) || !shouldUse)
            {
                superDashDelay.Reset();
                return false;
            }

            var state = superDashDelay.Update(dt);
            if (state == -1)
            {
                superDashDelay.Activate();
                return false;
            }

            if (state != 1)
            {
                return false;
            }

            if (canUseNativeSuperDash)
            {
                TriggerSuperInput();
                superDashDelay.Reset();
                return true;
            }

            if (player.TryUseHellBonusSuperDash())
            {
                superDashDelay.Reset();
                return true;
            }

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
            ResetBaseDelays();
            ResetCurrents();
            queuedReboundJump = false;
            strategy = 2;
            superDashDelay.Reset();

            var reboundZone = IsReboundInAttackZone();
            if (reboundZone == -1)
            {
                willAttackAtOnce = !player.IsGrounded;
                SetAttackPoint(150f, player.Position.x);
            }
            else if (reboundZone == 0)
            {
                willAttackAtOnce = !player.IsGrounded;
                var currentX = player.Position.x;
                SetAttackPoint(currentX, currentX);
            }
            else
            {
                SetAttackPoint(0f, 0f);
                willAttackAtOnce = !player.IsGrounded && Mathf.Abs(player.Position.x - attackPoint) < 50f;
            }
        }

        /// <summary>
        /// 初始化防守模式：识别对手持球者并清除延迟。
        /// </summary>
        protected void HandleBallInOpponentsHands()
        {
            strategy = 0;
            ResetBaseDelays();
            ResetCurrents();
            queuedReboundJump = false;
            willAttackAtOnce = false;
            isPumped = false;
            superDashDelay.Reset();
            opponent = player.GameCore.FindBallHolder(-player.Side);
        }

        /// <summary>
        /// 初始化争夺球模式：清除延迟并准备追逐球。
        /// </summary>
        protected void HandleBallOthers()
        {
            strategy = 1;
            ResetCurrents();
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
            if (signal == mlpPlayerSignalType.StartSteal)
            {
                PlayerStartSteal(side);
                return;
            }

            if (signal == mlpPlayerSignalType.Steal)
            {
                if (side == -player.Side)
                {
                    ResetAvoidSteal();
                }

                return;
            }

            if (signal == mlpPlayerSignalType.JumpA)
            {
                if (side == player.Side && signalPlayerNo == playerNo)
                {
                    ResetAvoidSteal();
                    attack.Activate();
                    directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                }
                else if (side == -player.Side)
                {
                    if (ShouldUsePerfectContestOnJump() || UnityEngine.Random.value <= profile.JumpThrow)
                    {
                        defenceDelay.Activate();
                    }
                }

                return;
            }

            if (signal == mlpPlayerSignalType.Pump)
            {
                if (side == -player.Side && player.CanAct && IsOpponentCloseBehind(90f))
                {
                    if (++pumpCount <= 3)
                    {
                        if (ShouldIgnorePumpFake())
                        {
                            return;
                        }

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

            if (signal == mlpPlayerSignalType.Dash)
            {
                if (side == player.Side)
                {
                    attack.Reset();
                }
                else if (strategy == 0 && player.CanAct && IsOpponentInRangeBehind(40f, GetDashBlockRangeMaxDistance()))
                {
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
            if (player.Side == 1)
            {
                attackZoneStart = mlpObjectsData.AttackZoneStart;
                attackZoneEnd = mlpObjectsData.AttackZoneEnd;
                dashZoneStart = mlpObjectsData.DashZoneStart;
                dashZoneEnd = mlpObjectsData.DashZoneEnd;
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
            strategy = 3;
            deltaDownTime = 0f;
            endPoint = baseEndPoint;
            ResetAllDelays();
            ResetCurrents();
            CurrentBlockOrPump = false;
            CurrentSuper = false;
            queuedSuperInput = false;
            isPumped = false;
            pumpCount = 0;
            queuedReboundJump = false;
            willAttackAtOnce = false;
            reboundPoint = reboundPointInDefence;
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
            if (!Mathf.Approximately(point, 0f))
            {
                attackPoint = point;
                jumpPoint = jump;
            }
            else
            {
                if (ShouldForceClutchThree())
                {
                    attackPoint = 500f + 24f * UnityEngine.Random.value;
                }
                else if (ShouldPreferSafeClutchTwo())
                {
                    attackPoint = 140f + 80f * UnityEngine.Random.value;
                }
                else if ((player.Position.x - 450f) * player.Side > 0f && UnityEngine.Random.value <= mlpObjectsData.ChanceForThree)
                {
                    attackPoint = 510f;
                }
                else if (UnityEngine.Random.value <= 0.7f)
                {
                    attackPoint = 120f + 200f * UnityEngine.Random.value;
                }
                else
                {
                    attackPoint = 320f + 160f * UnityEngine.Random.value;
                }

                jumpPoint = attackPoint <= 200f ? attackPoint + 100f : attackPoint;
            }

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
            var move = MoveTo(jumpPoint);
            if (move == 0)
            {
                attackJump = true;
                move = Mathf.Approximately(jumpPoint, attackPoint) ? 0 : MoveTo(attackPoint);
            }
            else
            {
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
