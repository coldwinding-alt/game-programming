// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushControllers 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace rimrush
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

    public sealed class rimrushKeyboardController : IBLPlayerController
    {
        private readonly rimrushControlProfile controls;
        private float lastLeftUp = -10f;
        private float lastRightUp = -10f;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        /// <summary>
        /// Executes rimrush Keyboard Controller for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="brain">Input value used by this step of the workflow.</param>
        public rimrushKeyboardController(string brain)
        {
            controls = rimrushControlsData.ProfileForBrain(brain);
        }

        /// <summary>
        /// Executes Update Controller for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        public void UpdateController(float dt)
        {
            CurrentMove = 0;
            CurrentDash = 0;
            var leftDown = controls.MoveLeftKey;
            var rightDown = controls.MoveRightKey;

            if (Input.GetKeyUp(leftDown))
            {
                lastLeftUp = Time.time;
            }

            if (Input.GetKeyUp(rightDown))
            {
                lastRightUp = Time.time;
            }

            if (Input.GetKeyDown(leftDown) && Time.time - lastLeftUp <= rimrushObjectsData.DashDoubleTapWindow)
            {
                CurrentDash = -1;
            }

            if (Input.GetKeyDown(rightDown) && Time.time - lastRightUp <= rimrushObjectsData.DashDoubleTapWindow)
            {
                CurrentDash = 1;
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
        /// Executes Ready For Action for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool ReadyForAction()
        {
            return !Input.GetKey(controls.ActionKey);
        }

        /// <summary>
        /// Executes Ball In Own Hands for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holderPlayerNo">Input value used by this step of the workflow.</param>
        public void BallInOwnHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// Executes Ball In Opponents Hands for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holderPlayerNo">Input value used by this step of the workflow.</param>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// Executes Ball Own Shoot for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shooterPlayerNo">Input value used by this step of the workflow.</param>
        public void BallOwnShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// Executes Ball Opponent Shoot for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shooterPlayerNo">Input value used by this step of the workflow.</param>
        public void BallOpponentShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// Executes Ball Others for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void BallOthers()
        {
        }

        /// <summary>
        /// Executes Release Block Or Pump for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool ReleaseBlockOrPump(float dt)
        {
            return !Input.GetKey(controls.BlockKey);
        }

        /// <summary>
        /// Executes Restart for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="startSide">Input value used by this step of the workflow.</param>
        public void Restart(int startSide)
        {
        }

        /// <summary>
        /// Executes Player On Ground for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void PlayerOnGround()
        {
        }

        /// <summary>
        /// Executes Player On Dash End for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void PlayerOnDashEnd()
        {
        }

        /// <summary>
        /// Executes Player On Block for the rimrushKeyboardController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void PlayerOnBlock()
        {
        }
    }

    public class rimrushAIController : rimrushBaseAIController
    {
        /// <summary>
        /// Executes rimrush AIController for the rimrushAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="player">Input value used by this step of the workflow.</param>
        /// <param name="skillLevel">Input value used by this step of the workflow.</param>
        public rimrushAIController(rimrushPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// Executes Create For Brain for the rimrushAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="player">Input value used by this step of the workflow.</param>
        /// <param name="brain">Input value used by this step of the workflow.</param>
        /// <param name="skillLevel">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static IBLPlayerController CreateForBrain(rimrushPlayerObject player, string brain, int skillLevel)
        {
            var index = ParseBrainIndex(brain);
            return index == 1 ? new rimrushAIController2(player, skillLevel) : new rimrushAIController(player, skillLevel);
        }

        /// <summary>
        /// Executes Use Defence2 for the rimrushAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holder">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected override bool UseDefence2(rimrushPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// Executes Parse Brain Index for the rimrushAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="brain">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static int ParseBrainIndex(string brain)
        {
            if (string.IsNullOrEmpty(brain) || brain.Length < 2 || !brain.StartsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return int.TryParse(brain.Substring(1, 1), out var value) ? value : 0;
        }
    }

    public sealed class rimrushAIController2 : rimrushBaseAIController
    {
        /// <summary>
        /// Executes rimrush AIController2 for the rimrushAIController2 workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="player">Input value used by this step of the workflow.</param>
        /// <param name="skillLevel">Input value used by this step of the workflow.</param>
        public rimrushAIController2(rimrushPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// Executes Use Defence2 for the rimrushAIController2 workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holder">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected override bool UseDefence2(rimrushPlayerObject holder)
        {
            return holder != null && holder.PlayerNo != player.PlayerNo;
        }

        /// <summary>
        /// Executes Strategy Defence2 for the rimrushAIController2 workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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

    public abstract class rimrushBaseAIController : IBLPlayerController
    {
        protected readonly rimrushPlayerObject player;
        protected readonly rimrushAiDifficulty difficulty;
        protected readonly rimrushAIDifficultyTuningProfile tuning;

        protected rimrushBallObject ball;
        protected List<rimrushPlayerObject> opponents;
        protected rimrushPlayerObject opponent;
        protected readonly rimrushAISkillProfile profile;

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
        /// Executes rimrush Base AIController for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="player">Input value used by this step of the workflow.</param>
        /// <param name="skillLevel">Input value used by this step of the workflow.</param>
        protected rimrushBaseAIController(rimrushPlayerObject player, int skillLevel)
        {
            this.player = player;
            difficulty = rimrushInventory.Instance.Difficulty;
            tuning = rimrushAIDifficultyTuning.Get(difficulty);
            profile = rimrushAISkillsData.Get(skillLevel);
            jumpBall = new NegativeDelay(rimrushObjectsData.IdealJumpBallJump, profile.JumpBall);
            attack = new FullDelay(rimrushObjectsData.IdealAttackJump, profile.Attack);
            attackJumpDelay = new SimpleDelay(profile.AttackAtOnce);
            stealDelay = new AIUseDelay(0.1f, rimrushObjectsData.StealDuration + profile.DelaySteal);
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
        /// Executes Update Controller for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Ball In Own Hands for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holderPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Ball In Opponents Hands for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holderPlayerNo">Input value used by this step of the workflow.</param>
        public virtual void BallInOpponentsHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(holderPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            if (player.UsesCurseSkill && player.ReadyForSuper && opponent != null && Mathf.Abs(player.Position.x - opponent.Position.x) <= 220f)
            {
                player.SuperShot();
            }

            HandleBallInOpponentsHands();
        }

        /// <summary>
        /// Executes Ball Own Shoot for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shooterPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Ball Opponent Shoot for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shooterPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Ball Others for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public virtual void BallOthers()
        {
            EnsureRuntimeLinks();
            HandleBallOthers();
        }

        /// <summary>
        /// Executes Ready For Action for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public virtual bool ReadyForAction()
        {
            return true;
        }

        /// <summary>
        /// Executes Release Block Or Pump for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public virtual bool ReleaseBlockOrPump(float dt)
        {
            return blockDelay.Update(dt) == 1;
        }

        /// <summary>
        /// Executes Restart for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="startSide">Input value used by this step of the workflow.</param>
        public virtual void Restart(int startSide)
        {
            ResetForRestart();
        }

        /// <summary>
        /// Executes Player On Ground for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Player On Dash End for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public virtual void PlayerOnDashEnd()
        {
            if ((player.Position.x - attackPoint) * player.Side < 0f)
            {
                attackPoint = player.Position.x - 10f * player.Side;
            }
        }

        /// <summary>
        /// Executes Player On Block for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public virtual void PlayerOnBlock()
        {
            CurrentBlockOrPump = false;
            blockDelay.Activate();
        }

        /// <summary>
        /// Executes Use Defence2 for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holder">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected virtual bool UseDefence2(rimrushPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// Executes Strategy Defence for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
                        ? opponent.Position.x + player.Side * rimrushObjectsData.OpponentDelta
                        : opponent.Position.x + player.Side * (rimrushObjectsData.OpponentDelta - 10f);

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
                    CurrentMove = MoveTo(opponent.Position.x + player.Side * (rimrushObjectsData.OpponentDelta - 10f));
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
                    endPoint = player.Side == 1 ? 0f : rimrushConstants.Width;
                    deltaDownTime = 0f;
                }
            }
            else
            {
                deltaDownTime = 0f;
            }
        }

        /// <summary>
        /// Executes Strategy Defence2 for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        protected virtual void StrategyDefence2(float dt)
        {
            StrategyDefence(dt);
        }

        /// <summary>
        /// Executes Strategy Ball Fight for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Strategy Attack for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
                                if (player.ReadyForDash && difficulty != rimrushAiDifficulty.Easy)
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
                                if (player.ReadyForDash && InDashingZone() && UnityEngine.Random.value <= profile.MakeDash && difficulty != rimrushAiDifficulty.Easy)
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
        /// Executes Strategy Jump Ball for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        protected virtual void StrategyJumpBall(float dt)
        {
            CurrentMove = 0;
            CurrentJump = jumpBall.Update(dt) == 1;
            CurrentAction = false;
        }

        /// <summary>
        /// Executes Strategy Rebound for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Try To Steal for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void TryToSteal()
        {
            if (difficulty == rimrushAiDifficulty.Easy || opponent == null || !opponent.IsGrounded)
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
        /// Executes Try Use Delayed Super Dash for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <param name="shouldUse">Input value used by this step of the workflow.</param>
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
        /// Executes Trigger Super Input for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void TriggerSuperInput()
        {
            ResetCurrents();
            CurrentSuper = true;
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// Executes Should Use Super Dash Against Holder for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Should Use Super Dash For Ball for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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

            return ball.Position.y > rimrushObjectsData.BasketHeight &&
                   Mathf.Abs(DeltaBallX()) >= GetLooseBallSuperDashDistance();
        }

        /// <summary>
        /// Executes Should Use Super Dash In Attack for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Handle Ball In Own Hands for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Handle Ball In Opponents Hands for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Handle Ball Others for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void HandleBallOthers()
        {
            strategy = 1;
            ResetCurrents();
            queuedReboundJump = false;
            superDashDelay.Reset();
        }

        /// <summary>
        /// Executes Process Player Signal for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="signal">Input value used by this step of the workflow.</param>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="signalPlayerNo">Input value used by this step of the workflow.</param>
        protected void ProcessPlayerSignal(rimrushPlayerSignalType signal, int side, int signalPlayerNo)
        {
            if (signal == rimrushPlayerSignalType.StartSteal)
            {
                PlayerStartSteal(side);
                return;
            }

            if (signal == rimrushPlayerSignalType.Steal)
            {
                if (side == -player.Side)
                {
                    ResetAvoidSteal();
                }

                return;
            }

            if (signal == rimrushPlayerSignalType.JumpA)
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

            if (signal == rimrushPlayerSignalType.Pump)
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

            if (signal == rimrushPlayerSignalType.Dash)
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

            if (signal == rimrushPlayerSignalType.Stun && side == player.Side)
            {
                ResetAllDelays();
            }
        }

        /// <summary>
        /// Executes Player Start Steal for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
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
        /// Executes Try To Avoid for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Get Defence Contest Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetDefenceContestDistance()
        {
            return tuning.DefenceContestDistance;
        }

        /// <summary>
        /// Executes Get Steal Behind Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetStealBehindDistance()
        {
            return tuning.StealBehindDistance;
        }

        /// <summary>
        /// Executes Get Steal Basket Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetStealBasketDistance()
        {
            return tuning.StealBasketDistance;
        }

        /// <summary>
        /// Executes Get Holder Super Dash Min Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetHolderSuperDashMinDistance()
        {
            return tuning.HolderSuperDashMinDistance;
        }

        /// <summary>
        /// Executes Get Holder Super Dash Max Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetHolderSuperDashMaxDistance()
        {
            return tuning.HolderSuperDashMaxDistance;
        }

        /// <summary>
        /// Executes Get Loose Ball Super Dash Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetLooseBallSuperDashDistance()
        {
            return tuning.LooseBallSuperDashDistance;
        }

        /// <summary>
        /// Executes Get Attack Pressure Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetAttackPressureDistance()
        {
            return tuning.AttackPressureDistance;
        }

        /// <summary>
        /// Executes Get Attack Super Dash Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetAttackSuperDashDistance()
        {
            return tuning.AttackSuperDashDistance;
        }

        /// <summary>
        /// Executes Get Dash Block Range Max Distance for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float GetDashBlockRangeMaxDistance()
        {
            return tuning.DashBlockRangeMaxDistance;
        }

        /// <summary>
        /// Executes Try Use Hell Bonus Shield Against Human Shot for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void TryUseHellBonusShieldAgainstHumanShot()
        {
            if (difficulty != rimrushAiDifficulty.Hell || opponent == null || !opponent.IsHuman)
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
        /// Executes Init Zones for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void InitZones()
        {
            if (player.Side == 1)
            {
                attackZoneStart = rimrushObjectsData.AttackZoneStart;
                attackZoneEnd = rimrushObjectsData.AttackZoneEnd;
                dashZoneStart = rimrushObjectsData.DashZoneStart;
                dashZoneEnd = rimrushObjectsData.DashZoneEnd;
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
                attackZoneStart = rimrushConstants.Width - rimrushObjectsData.AttackZoneEnd;
                attackZoneEnd = rimrushConstants.Width - rimrushObjectsData.AttackZoneStart;
                dashZoneStart = rimrushConstants.Width - rimrushObjectsData.DashZoneEnd;
                dashZoneEnd = rimrushConstants.Width - rimrushObjectsData.DashZoneStart;
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

            defensePoint = player.Side == -1 ? rimrushObjectsData.DefensePoint : rimrushConstants.Width - rimrushObjectsData.DefensePoint;
            endPoint = baseEndPoint;
        }

        /// <summary>
        /// Executes Ensure Runtime Links for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void EnsureRuntimeLinks()
        {
            if (initialized)
            {
                return;
            }

            ball = player.GameCore.Ball;
            opponents = player.Side == -1
                ? new List<rimrushPlayerObject>(player.GameCore.PlayersRight)
                : new List<rimrushPlayerObject>(player.GameCore.PlayersLeft);
            opponent = opponents.Count > 0 ? opponents[0] : null;
            initialized = true;
        }

        /// <summary>
        /// Executes Reset For Restart for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Reset Currents for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void ResetCurrents()
        {
            CurrentMove = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentDash = 0;
        }

        /// <summary>
        /// Executes Reset Base Delays for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Reset All Delays for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void ResetAllDelays()
        {
            dashDecisionDelay.Reset();
            superDashDelay.Reset();
            ResetBaseDelays();
        }

        /// <summary>
        /// Executes Reset Avoid Steal for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        protected void ResetAvoidSteal()
        {
            avoidStealJump = false;
            avoidStealMove = 0;
        }

        /// <summary>
        /// Executes Find Opponent By Player No for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="targetPlayerNo">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected rimrushPlayerObject FindOpponentByPlayerNo(int targetPlayerNo)
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
        /// Executes Set Attack Point for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="point">Input value used by this step of the workflow.</param>
        /// <param name="jump">Input value used by this step of the workflow.</param>
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
                else if ((player.Position.x - 450f) * player.Side > 0f && UnityEngine.Random.value <= rimrushObjectsData.ChanceForThree)
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
                attackPoint = rimrushConstants.Width - attackPoint;
                jumpPoint = rimrushConstants.Width - jumpPoint;
            }
        }

        /// <summary>
        /// Executes Should Use Technical Prediction for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldUseTechnicalPrediction()
        {
            return difficulty == rimrushAiDifficulty.Hell;
        }

        /// <summary>
        /// Executes Get Technical Loose Ball Target X for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Get Technical Rebound Target X for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Should Force Clutch Three for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldForceClutchThree()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 12f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) <= -2;
        }

        /// <summary>
        /// Executes Should Prefer Safe Clutch Two for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldPreferSafeClutchTwo()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 15f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) >= 3;
        }

        /// <summary>
        /// Executes Supports Clutch Shot Selection for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool SupportsClutchShotSelection()
        {
            return difficulty == rimrushAiDifficulty.Hard || difficulty == rimrushAiDifficulty.Hell;
        }

        /// <summary>
        /// Executes Should Use Perfect Contest On Jump for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldUsePerfectContestOnJump()
        {
            return difficulty == rimrushAiDifficulty.Hell &&
                   IsOpponentImmediateShotThreat() &&
                   IsOpponentCloseAbs(GetDefenceContestDistance() + 24f);
        }

        /// <summary>
        /// Executes Should Ignore Pump Fake for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool ShouldIgnorePumpFake()
        {
            if (difficulty != rimrushAiDifficulty.Hell)
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
        /// Executes Is Opponent Immediate Shot Threat for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Is Rebound In Attack Zone for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Move To for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected int MoveTo(float x)
        {
            var delta = player.Position.x - x;
            return Mathf.Abs(delta) <= DeltaDistance ? 0 : delta > 0f ? -1 : 1;
        }

        /// <summary>
        /// Executes Move In Attack for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Delta Ball X for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float DeltaBallX()
        {
            return player.Position.x - ball.Position.x;
        }

        /// <summary>
        /// Executes Delta Ball Y for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        protected float DeltaBallY()
        {
            return player.Position.y - ball.Position.y;
        }

        /// <summary>
        /// Executes Is Ball In Rebound Zone for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsBallInReboundZone()
        {
            return Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
        }

        /// <summary>
        /// Executes Is Opponent Close Behind for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="distance">Input value used by this step of the workflow.</param>
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
        /// Executes Is Opponent In Range Behind for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="min">Input value used by this step of the workflow.</param>
        /// <param name="max">Input value used by this step of the workflow.</param>
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
        /// Executes Is Opponent Close To Basket for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="distance">Input value used by this step of the workflow.</param>
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
        /// Executes Is AICloser For Basket for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Is Under Own Basket for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsUnderOwnBasket()
        {
            return player.Side == 1 ? player.Position.x > 700f : player.Position.x < 100f;
        }

        /// <summary>
        /// Executes In Dashing Zone for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool InDashingZone()
        {
            return player.Position.x >= dashZoneStart && player.Position.x <= dashZoneEnd;
        }

        /// <summary>
        /// Executes Is Opponent Close Abs for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="distance">Input value used by this step of the workflow.</param>
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
        /// Executes Is In Attack Zone for the rimrushBaseAIController workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        protected bool IsInAttackZone()
        {
            return player.Side == 1 ? player.Position.x < 600f : player.Position.x > 200f;
        }
    }
}
