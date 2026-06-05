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
        /// Create a keyboard controller that reads keys from the given control profile.
        /// </summary>
        /// <param name="brain">the controller brain identifier string</param>
        public mlpKeyboardController(string brain)
        {
            controls = mlpControlsData.ProfileForBrain(brain);
        }

        /// <summary>
        /// Read keyboard input each frame: movement, jump, action, block, super, and dash double-taps.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
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
                // Accept both quick re-presses and classic double taps so dash is less frame-perfect.
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
        /// Return true when the action key is released, preventing repeat throws while held.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool ReadyForAction()
        {
            return !Input.GetKey(controls.ActionKey);
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        /// <param name="holderPlayerNo">the player number of the current ball holder</param>
        public void BallInOwnHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        /// <param name="holderPlayerNo">the player number of the current ball holder</param>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        /// <param name="shooterPlayerNo">the player number of the player who shot</param>
        public void BallOwnShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        /// <param name="shooterPlayerNo">the player number of the player who shot</param>
        public void BallOpponentShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        public void BallOthers()
        {
        }

        /// <summary>
        /// Return true when the block key is released, ending the block/pump animation.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool ReleaseBlockOrPump(float dt)
        {
            return !Input.GetKey(controls.BlockKey);
        }

        /// <summary>
        /// Reset all input state and double-tap timers for a new round.
        /// </summary>
        /// <param name="startSide">the side the player starts on after a reset</param>
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
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        public void PlayerOnGround()
        {
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        public void PlayerOnDashEnd()
        {
        }

        /// <summary>
        /// No-op for keyboard controllers; input is read every frame regardless.
        /// </summary>
        public void PlayerOnBlock()
        {
        }
    }

    public class mlpAIController : mlpBaseAIController
    {
        /// <summary>
        /// Create an AI controller with default defence behaviour.
        /// </summary>
        /// <param name="player">the owning player object</param>
        /// <param name="skillLevel">the numeric AI skill level (higher is harder)</param>
        public mlpAIController(mlpPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// Factory method that returns the correct AI controller variant based on the brain identifier.
        /// </summary>
        /// <param name="player">the owning player object</param>
        /// <param name="brain">the controller brain identifier string</param>
        /// <param name="skillLevel">the numeric AI skill level (higher is harder)</param>
        /// <returns>The computed result.</returns>
        public static IBLPlayerController CreateForBrain(mlpPlayerObject player, string brain, int skillLevel)
        {
            var index = ParseBrainIndex(brain);
            return index == 1 ? new mlpAIController2(player, skillLevel) : new mlpAIController(player, skillLevel);
        }

        /// <summary>
        /// Return false; the default AI does not use the alternative defence style.
        /// </summary>
        /// <param name="holder">the ball holder to test against</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected override bool UseDefence2(mlpPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// Extract the numeric variant index from a brain string like 'B1' or 'B2'.
        /// </summary>
        /// <param name="brain">the controller brain identifier string</param>
        /// <returns>The computed result.</returns>
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
        /// Create an AI controller variant that uses an active steal-based defence style.
        /// </summary>
        /// <param name="player">the owning player object</param>
        /// <param name="skillLevel">the numeric AI skill level (higher is harder)</param>
        public mlpAIController2(mlpPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// Return true when the ball holder is an opponent, enabling the alternative defence strategy.
        /// </summary>
        /// <param name="holder">the ball holder to test against</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected override bool UseDefence2(mlpPlayerObject holder)
        {
            return holder != null && holder.PlayerNo != player.PlayerNo;
        }

        /// <summary>
        /// Chase the ball holder with steal timing and jump to contest close-range shots.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
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
        /// Initialize shared AI state: difficulty profile, decision timers, and attack/defend zones.
        /// </summary>
        /// <param name="player">the owning player object</param>
        /// <param name="skillLevel">the numeric AI skill level (higher is harder)</param>
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
        /// Run the full AI decision loop: choose a strategy based on ball state, then call the matching strategy method.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
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
        /// Switch to attack mode and optionally activate the mega-dunk super delay.
        /// </summary>
        /// <param name="holderPlayerNo">the player number of the current ball holder</param>
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
        /// Switch to defence mode and optionally use a freeze super if the opponent is close.
        /// </summary>
        /// <param name="holderPlayerNo">the player number of the current ball holder</param>
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
        /// Switch to rebound mode after a teammate shoots, positioning for an offensive rebound.
        /// </summary>
        /// <param name="shooterPlayerNo">the player number of the player who shot</param>
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
        /// Switch to rebound mode after an opponent shoots; optionally activate shield super.
        /// </summary>
        /// <param name="shooterPlayerNo">the player number of the player who shot</param>
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
        /// Switch to loose-ball mode, chasing the ball to pick it up.
        /// </summary>
        public virtual void BallOthers()
        {
            EnsureRuntimeLinks();
            HandleBallOthers();
        }

        /// <summary>
        /// Return true; AI controllers are always ready for actions.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public virtual bool ReadyForAction()
        {
            return true;
        }

        /// <summary>
        /// Return true when the block timer expires, ending the AI block attempt.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public virtual bool ReleaseBlockOrPump(float dt)
        {
            return blockDelay.Update(dt) == 1;
        }

        /// <summary>
        /// Reset all AI state for a new round.
        /// </summary>
        /// <param name="startSide">the side the player starts on after a reset</param>
        public virtual void Restart(int startSide)
        {
            ResetForRestart();
        }

        /// <summary>
        /// Reset pump state and optionally queue an immediate attack-jump if holding the ball.
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
        /// Adjust the attack point if the dash overshot the target.
        /// </summary>
        public virtual void PlayerOnDashEnd()
        {
            if ((player.Position.x - attackPoint) * player.Side < 0f)
            {
                attackPoint = player.Position.x - 10f * player.Side;
            }
        }

        /// <summary>
        /// Clear the block input and start the block cooldown timer.
        /// </summary>
        public virtual void PlayerOnBlock()
        {
            CurrentBlockOrPump = false;
            blockDelay.Activate();
        }

        /// <summary>
        /// Return false; subclasses override this to enable alternative defence strategies.
        /// </summary>
        /// <param name="holder">the ball holder to test against</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected virtual bool UseDefence2(mlpPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// Follow the ball holder, attempt steals at close range, and contest shots with timed jumps.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
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
        /// Delegate to the default defence strategy; subclasses override for custom behaviour.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
        protected virtual void StrategyDefence2(float dt)
        {
            StrategyDefence(dt);
        }

        /// <summary>
        /// Chase the loose ball and jump for rebounds when the ball is near the basket.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
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
        /// Move toward the attack point, time the shot jump, and react to nearby defenders.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
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
        /// Stand still and time the jump based on the jump-ball delay profile.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
        protected virtual void StrategyJumpBall(float dt)
        {
            CurrentMove = 0;
            CurrentJump = jumpBall.Update(dt) == 1;
            CurrentAction = false;
        }

        /// <summary>
        /// Position for a rebound and jump when the ball is in the rebound zone.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
        protected virtual void StrategyRebound(float dt)
        {
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
        /// Roll a steal attempt based on distance and difficulty profile when close to the opponent.
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
        /// Activate a super dash with a short delay if the condition and cooldown allow it.
        /// </summary>
        /// <param name="dt">delta time in seconds</param>
        /// <param name="shouldUse">true when the AI should attempt a super dash</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Set the super input flag and clear other inputs to activate the super skill.
        /// </summary>
        protected void TriggerSuperInput()
        {
            ResetCurrents();
            CurrentSuper = true;
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// Return true when the opponent holding the ball is in the ideal super-dash range.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return true when the loose ball is high enough and far enough to justify a super dash.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return true when an opponent is pressuring from behind or the basket is far enough for a super dash.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Initialize attack mode: set the attack point, clear delays, and prepare for the shot approach.
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
        /// Initialize defence mode: identify the opponent ball holder and clear delays.
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
        /// Initialize loose-ball mode: clear delays and prepare to chase the ball.
        /// </summary>
        protected void HandleBallOthers()
        {
            strategy = 1;
            ResetCurrents();
            queuedReboundJump = false;
            superDashDelay.Reset();
        }

        /// <summary>
        /// React to game signals (steal, jump, pump-fake, dash, stun) by adjusting AI inputs and timers.
        /// </summary>
        /// <param name="signal">the type of player event signal</param>
        /// <param name="side">the court side (-1 for left, 1 for right)</param>
        /// <param name="signalPlayerNo">the player number that triggered the signal</param>
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
        /// React to an opponent starting a steal by attempting to avoid it if holding the ball.
        /// </summary>
        /// <param name="side">the court side (-1 for left, 1 for right)</param>
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
        /// Attempt to dodge an incoming steal by dashing, jumping, or sidestepping.
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
        /// Return the distance threshold for contesting a shot, from the difficulty tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetDefenceContestDistance()
        {
            return tuning.DefenceContestDistance;
        }

        /// <summary>
        /// Return the distance threshold for stealing from behind, from the difficulty tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetStealBehindDistance()
        {
            return tuning.StealBehindDistance;
        }

        /// <summary>
        /// Return the distance threshold for stealing near the basket, from the difficulty tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetStealBasketDistance()
        {
            return tuning.StealBasketDistance;
        }

        /// <summary>
        /// Return the minimum distance to a ball holder for a super dash, from the tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetHolderSuperDashMinDistance()
        {
            return tuning.HolderSuperDashMinDistance;
        }

        /// <summary>
        /// Return the maximum distance to a ball holder for a super dash, from the tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetHolderSuperDashMaxDistance()
        {
            return tuning.HolderSuperDashMaxDistance;
        }

        /// <summary>
        /// Return the minimum ball distance for a loose-ball super dash, from the tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetLooseBallSuperDashDistance()
        {
            return tuning.LooseBallSuperDashDistance;
        }

        /// <summary>
        /// Return the behind-distance that triggers super dash escape in attack, from the tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetAttackPressureDistance()
        {
            return tuning.AttackPressureDistance;
        }

        /// <summary>
        /// Return the basket distance that justifies a super dash in attack, from the tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetAttackSuperDashDistance()
        {
            return tuning.AttackSuperDashDistance;
        }

        /// <summary>
        /// Return the maximum distance at which the AI will attempt a dash-block, from the tuning profile.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float GetDashBlockRangeMaxDistance()
        {
            return tuning.DashBlockRangeMaxDistance;
        }

        /// <summary>
        /// Activate a bonus shield if a human player takes a threatening shot on Hell difficulty.
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
        /// Calculate attack, dash, defence, and rebound positions based on the player's side and number.
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
        /// Lazily fetch the ball and opponent references from the game core on first use.
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
        /// Reset all AI state (strategy, delays, inputs, pump count) for a new round.
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
        /// Zero out all controller input flags (move, jump, action, dash).
        /// </summary>
        protected void ResetCurrents()
        {
            CurrentMove = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentDash = 0;
        }

        /// <summary>
        /// Reset the attack, defence, move, steal, and attack-jump delay timers.
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
        /// Reset all delay timers including dash-decision and super-dash timers.
        /// </summary>
        protected void ResetAllDelays()
        {
            dashDecisionDelay.Reset();
            superDashDelay.Reset();
            ResetBaseDelays();
        }

        /// <summary>
        /// Clear the steal-avoidance jump and move flags.
        /// </summary>
        protected void ResetAvoidSteal()
        {
            avoidStealJump = false;
            avoidStealMove = 0;
        }

        /// <summary>
        /// Search the opponent list for a player with the given number, or return null.
        /// </summary>
        /// <param name="targetPlayerNo">the player number to search for</param>
        /// <returns>The computed result.</returns>
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
        /// Choose the X position to shoot from, using distance-based randomisation and clutch-shot logic.
        /// </summary>
        /// <param name="point">the attack X position (0 means auto-select)</param>
        /// <param name="jump">the jump-point X position used to decide when to jump-shoot</param>
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
        /// Return true on Hell difficulty, enabling trajectory-based ball prediction.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldUseTechnicalPrediction()
        {
            return difficulty == mlpAiDifficulty.Hell;
        }

        /// <summary>
        /// Predict where a loose ball will land on Hell difficulty, or return the ball's current X.
        /// </summary>
        /// <returns>The computed result.</returns>
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
        /// Predict where a missed shot will land on Hell difficulty, or return the static rebound point.
        /// </summary>
        /// <returns>The computed result.</returns>
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
        /// Return true when the AI is losing late in the match and should force a three-pointer.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldForceClutchThree()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 12f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) <= -2;
        }

        /// <summary>
        /// Return true when the AI is winning late in the match and should take a safe two-pointer.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldPreferSafeClutchTwo()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 15f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) >= 3;
        }

        /// <summary>
        /// Return true on Hard or Hell difficulty, enabling late-game shot selection logic.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool SupportsClutchShotSelection()
        {
            return difficulty == mlpAiDifficulty.Hard || difficulty == mlpAiDifficulty.Hell;
        }

        /// <summary>
        /// Return true on Hell difficulty when the opponent is close and taking a threatening shot.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldUsePerfectContestOnJump()
        {
            return difficulty == mlpAiDifficulty.Hell &&
                   IsOpponentImmediateShotThreat() &&
                   IsOpponentCloseAbs(GetDefenceContestDistance() + 24f);
        }

        /// <summary>
        /// Return true on Hell difficulty when the opponent is unlikely to actually shoot.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return true when the opponent is close to their attack target or time is almost up.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return -1, 0, or 1 indicating whether the player is before, in, or past the attack zone.
        /// </summary>
        /// <returns>The computed result.</returns>
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
        /// Return -1, 0, or 1 indicating the direction to move toward the given X position.
        /// </summary>
        /// <param name="x">the horizontal coordinate in pixel space</param>
        /// <returns>The computed result.</returns>
        protected int MoveTo(float x)
        {
            var delta = player.Position.x - x;
            return Mathf.Abs(delta) <= DeltaDistance ? 0 : delta > 0f ? -1 : 1;
        }

        /// <summary>
        /// Decide whether to move toward the jump point or attack point, setting the attack-jump flag.
        /// </summary>
        /// <returns>The computed result.</returns>
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
        /// Return the horizontal distance from the player to the ball (positive means ball is to the left).
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float DeltaBallX()
        {
            return player.Position.x - ball.Position.x;
        }

        /// <summary>
        /// Return the vertical distance from the player to the ball.
        /// </summary>
        /// <returns>The computed result.</returns>
        protected float DeltaBallY()
        {
            return player.Position.y - ball.Position.y;
        }

        /// <summary>
        /// Return true when the ball is close horizontally and far vertically, indicating a rebound opportunity.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsBallInReboundZone()
        {
            return Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
        }

        /// <summary>
        /// Return true when the opponent is within a given distance behind the player.
        /// </summary>
        /// <param name="distance">the estimated distance to the target basket</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return true when the opponent is behind the player within a min-max distance range.
        /// </summary>
        /// <param name="min">the minimum distance threshold</param>
        /// <param name="max">the maximum distance threshold</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return true when the opponent is between the player and the basket, within a given distance.
        /// </summary>
        /// <param name="distance">the estimated distance to the target basket</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Return true when the AI player is closer to the opponent's basket than the opponent.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsAICloserForBasket()
        {
            if (opponent == null)
            {
                return false;
            }

            return (player.Position.x - opponent.Position.x) * player.Side < 0f;
        }

        /// <summary>
        /// Return true when the player is standing under their own basket.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsUnderOwnBasket()
        {
            return player.Side == 1 ? player.Position.x > 700f : player.Position.x < 100f;
        }

        /// <summary>
        /// Return true when the player's X position is within the configured dash zone.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool InDashingZone()
        {
            return player.Position.x >= dashZoneStart && player.Position.x <= dashZoneEnd;
        }

        /// <summary>
        /// Return true when the absolute horizontal distance to the opponent is within the threshold.
        /// </summary>
        /// <param name="distance">the estimated distance to the target basket</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsOpponentCloseAbs(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            return Mathf.Abs(player.Position.x - opponent.Position.x) <= distance;
        }

        /// <summary>
        /// Return true when the player is positioned in the opponent's half of the court.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsInAttackZone()
        {
            return player.Side == 1 ? player.Position.x < 600f : player.Position.x > 200f;
        }
    }
}
