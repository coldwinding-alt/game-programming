using System;
using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
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

    public sealed class BLKeyboardController : IBLPlayerController
    {
        private readonly BLControlProfile controls;
        private float lastLeftUp = -10f;
        private float lastRightUp = -10f;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        public BLKeyboardController(string brain)
        {
            controls = BLControlsData.ProfileForBrain(brain);
        }

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

            if (Input.GetKeyDown(leftDown) && Time.time - lastLeftUp <= BLObjectsData.DashDoubleTapWindow)
            {
                CurrentDash = -1;
            }

            if (Input.GetKeyDown(rightDown) && Time.time - lastRightUp <= BLObjectsData.DashDoubleTapWindow)
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

        public bool ReadyForAction()
        {
            return !Input.GetKey(controls.ActionKey);
        }

        public void BallInOwnHands(int holderPlayerNo)
        {
        }

        public void BallInOpponentsHands(int holderPlayerNo)
        {
        }

        public void BallOwnShoot(int shooterPlayerNo)
        {
        }

        public void BallOpponentShoot(int shooterPlayerNo)
        {
        }

        public void BallOthers()
        {
        }

        public bool ReleaseBlockOrPump(float dt)
        {
            return !Input.GetKey(controls.BlockKey);
        }

        public void Restart(int startSide)
        {
        }

        public void PlayerOnGround()
        {
        }

        public void PlayerOnDashEnd()
        {
        }

        public void PlayerOnBlock()
        {
        }
    }

    public class BLAIController : BLBaseAIController
    {
        public BLAIController(BLPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        public static IBLPlayerController CreateForBrain(BLPlayerObject player, string brain, int skillLevel)
        {
            var index = ParseBrainIndex(brain);
            return index == 1 ? new BLAIController2(player, skillLevel) : new BLAIController(player, skillLevel);
        }

        protected override bool UseDefence2(BLPlayerObject holder)
        {
            return false;
        }

        private static int ParseBrainIndex(string brain)
        {
            if (string.IsNullOrEmpty(brain) || brain.Length < 2 || !brain.StartsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return int.TryParse(brain.Substring(1, 1), out var value) ? value : 0;
        }
    }

    public sealed class BLAIController2 : BLBaseAIController
    {
        public BLAIController2(BLPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        protected override bool UseDefence2(BLPlayerObject holder)
        {
            return holder != null && holder.PlayerNo != player.PlayerNo;
        }

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

    public abstract class BLBaseAIController : IBLPlayerController
    {
        protected readonly BLPlayerObject player;
        protected readonly BLAiDifficulty difficulty;
        protected readonly BLAIDifficultyTuningProfile tuning;

        protected BLBallObject ball;
        protected List<BLPlayerObject> opponents;
        protected BLPlayerObject opponent;
        protected readonly BLAISkillProfile profile;

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

        protected BLBaseAIController(BLPlayerObject player, int skillLevel)
        {
            this.player = player;
            difficulty = BLInventory.Instance.Difficulty;
            tuning = BLAIDifficultyTuning.Get(difficulty);
            profile = BLAISkillsData.Get(skillLevel);
            jumpBall = new NegativeDelay(BLObjectsData.IdealJumpBallJump, profile.JumpBall);
            attack = new FullDelay(BLObjectsData.IdealAttackJump, profile.Attack);
            attackJumpDelay = new SimpleDelay(profile.AttackAtOnce);
            stealDelay = new AIUseDelay(0.1f, BLObjectsData.StealDuration + profile.DelaySteal);
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

        public virtual void BallInOwnHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            if ((player.SuperId == 0 || player.SuperId == 2) && player.ReadyForSuper)
            {
                megaDunkDelay.Activate();
            }

            HandleBallInOwnHands();
        }

        public virtual void BallInOpponentsHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(holderPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            HandleBallInOpponentsHands();
        }

        public virtual void BallOwnShoot(int shooterPlayerNo)
        {
            EnsureRuntimeLinks();
            ResetCurrents();
            queuedReboundJump = false;
            strategy = 4;
            reboundPoint = reboundPointInAttack;
            superDashDelay.Reset();
        }

        public virtual void BallOpponentShoot(int shooterPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(shooterPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            if (player.SuperId == 1 && player.ReadyForSuper)
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

        public virtual void BallOthers()
        {
            EnsureRuntimeLinks();
            HandleBallOthers();
        }

        public virtual bool ReadyForAction()
        {
            return true;
        }

        public virtual bool ReleaseBlockOrPump(float dt)
        {
            return blockDelay.Update(dt) == 1;
        }

        public virtual void Restart(int startSide)
        {
            ResetForRestart();
        }

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

        public virtual void PlayerOnDashEnd()
        {
            if ((player.Position.x - attackPoint) * player.Side < 0f)
            {
                attackPoint = player.Position.x - 10f * player.Side;
            }
        }

        public virtual void PlayerOnBlock()
        {
            CurrentBlockOrPump = false;
            blockDelay.Activate();
        }

        protected virtual bool UseDefence2(BLPlayerObject holder)
        {
            return false;
        }

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
                        ? opponent.Position.x + player.Side * BLObjectsData.OpponentDelta
                        : opponent.Position.x + player.Side * (BLObjectsData.OpponentDelta - 10f);

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
                    CurrentMove = MoveTo(opponent.Position.x + player.Side * (BLObjectsData.OpponentDelta - 10f));
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
                    endPoint = player.Side == 1 ? 0f : BLConstants.Width;
                    deltaDownTime = 0f;
                }
            }
            else
            {
                deltaDownTime = 0f;
            }
        }

        protected virtual void StrategyDefence2(float dt)
        {
            StrategyDefence(dt);
        }

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

        protected virtual void StrategyAttack(float dt)
        {
            if (!player.WithBall)
            {
                return;
            }

            if ((player.SuperId == 0 || player.SuperId == 2) && megaDunkDelay.Update(dt) == 1)
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
                                if (player.ReadyForDash && difficulty != BLAiDifficulty.Easy)
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
                                if (player.ReadyForDash && InDashingZone() && UnityEngine.Random.value <= profile.MakeDash && difficulty != BLAiDifficulty.Easy)
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

        protected virtual void StrategyJumpBall(float dt)
        {
            CurrentMove = 0;
            CurrentJump = jumpBall.Update(dt) == 1;
            CurrentAction = false;
        }

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

        protected void TryToSteal()
        {
            if (difficulty == BLAiDifficulty.Easy || opponent == null || !opponent.IsGrounded)
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

        protected bool TryUseDelayedSuperDash(float dt, bool shouldUse)
        {
            var canUseNativeSuperDash = player.SuperId == 3 && player.ReadyForSuper;
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

        protected void TriggerSuperInput()
        {
            ResetCurrents();
            CurrentSuper = true;
            CurrentBlockOrPump = false;
        }

        protected bool ShouldUseSuperDashAgainstHolder()
        {
            if (opponent == null || !opponent.WithBall || !player.IsGrounded)
            {
                return false;
            }

            var distance = Mathf.Abs(player.Position.x - opponent.Position.x);
            return distance >= GetHolderSuperDashMinDistance() && distance <= GetHolderSuperDashMaxDistance();
        }

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

            return ball.Position.y > BLObjectsData.BasketHeight &&
                   Mathf.Abs(DeltaBallX()) >= GetLooseBallSuperDashDistance();
        }

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

        protected void HandleBallOthers()
        {
            strategy = 1;
            ResetCurrents();
            queuedReboundJump = false;
            superDashDelay.Reset();
        }

        protected void ProcessPlayerSignal(BLPlayerSignalType signal, int side, int signalPlayerNo)
        {
            if (signal == BLPlayerSignalType.StartSteal)
            {
                PlayerStartSteal(side);
                return;
            }

            if (signal == BLPlayerSignalType.Steal)
            {
                if (side == -player.Side)
                {
                    ResetAvoidSteal();
                }

                return;
            }

            if (signal == BLPlayerSignalType.JumpA)
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

            if (signal == BLPlayerSignalType.Pump)
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

            if (signal == BLPlayerSignalType.Dash)
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

            if (signal == BLPlayerSignalType.Stun && side == player.Side)
            {
                ResetAllDelays();
            }
        }

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

        protected float GetDefenceContestDistance()
        {
            return tuning.DefenceContestDistance;
        }

        protected float GetStealBehindDistance()
        {
            return tuning.StealBehindDistance;
        }

        protected float GetStealBasketDistance()
        {
            return tuning.StealBasketDistance;
        }

        protected float GetHolderSuperDashMinDistance()
        {
            return tuning.HolderSuperDashMinDistance;
        }

        protected float GetHolderSuperDashMaxDistance()
        {
            return tuning.HolderSuperDashMaxDistance;
        }

        protected float GetLooseBallSuperDashDistance()
        {
            return tuning.LooseBallSuperDashDistance;
        }

        protected float GetAttackPressureDistance()
        {
            return tuning.AttackPressureDistance;
        }

        protected float GetAttackSuperDashDistance()
        {
            return tuning.AttackSuperDashDistance;
        }

        protected float GetDashBlockRangeMaxDistance()
        {
            return tuning.DashBlockRangeMaxDistance;
        }

        protected void TryUseHellBonusShieldAgainstHumanShot()
        {
            if (difficulty != BLAiDifficulty.Hell || opponent == null || !opponent.IsHuman)
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

        protected void InitZones()
        {
            if (player.Side == 1)
            {
                attackZoneStart = BLObjectsData.AttackZoneStart;
                attackZoneEnd = BLObjectsData.AttackZoneEnd;
                dashZoneStart = BLObjectsData.DashZoneStart;
                dashZoneEnd = BLObjectsData.DashZoneEnd;
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
                attackZoneStart = BLConstants.Width - BLObjectsData.AttackZoneEnd;
                attackZoneEnd = BLConstants.Width - BLObjectsData.AttackZoneStart;
                dashZoneStart = BLConstants.Width - BLObjectsData.DashZoneEnd;
                dashZoneEnd = BLConstants.Width - BLObjectsData.DashZoneStart;
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

            defensePoint = player.Side == -1 ? BLObjectsData.DefensePoint : BLConstants.Width - BLObjectsData.DefensePoint;
            endPoint = baseEndPoint;
        }

        protected void EnsureRuntimeLinks()
        {
            if (initialized)
            {
                return;
            }

            ball = player.GameCore.Ball;
            opponents = player.Side == -1
                ? new List<BLPlayerObject>(player.GameCore.PlayersRight)
                : new List<BLPlayerObject>(player.GameCore.PlayersLeft);
            opponent = opponents.Count > 0 ? opponents[0] : null;
            initialized = true;
        }

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

        protected void ResetCurrents()
        {
            CurrentMove = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentDash = 0;
        }

        protected void ResetBaseDelays()
        {
            attackJumpDelay.Reset();
            attack.Reset();
            defenceDelay.Reset();
            moveDelay.Reset();
            stealDelay.Reset();
            ResetAvoidSteal();
        }

        protected void ResetAllDelays()
        {
            dashDecisionDelay.Reset();
            superDashDelay.Reset();
            ResetBaseDelays();
        }

        protected void ResetAvoidSteal()
        {
            avoidStealJump = false;
            avoidStealMove = 0;
        }

        protected BLPlayerObject FindOpponentByPlayerNo(int targetPlayerNo)
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
                else if ((player.Position.x - 450f) * player.Side > 0f && UnityEngine.Random.value <= BLObjectsData.ChanceForThree)
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
                attackPoint = BLConstants.Width - attackPoint;
                jumpPoint = BLConstants.Width - jumpPoint;
            }
        }

        protected bool ShouldUseTechnicalPrediction()
        {
            return difficulty == BLAiDifficulty.Hell;
        }

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

        protected bool ShouldForceClutchThree()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 12f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) <= -2;
        }

        protected bool ShouldPreferSafeClutchTwo()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 15f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) >= 3;
        }

        protected bool SupportsClutchShotSelection()
        {
            return difficulty == BLAiDifficulty.Hard || difficulty == BLAiDifficulty.Hell;
        }

        protected bool ShouldUsePerfectContestOnJump()
        {
            return difficulty == BLAiDifficulty.Hell &&
                   IsOpponentImmediateShotThreat() &&
                   IsOpponentCloseAbs(GetDefenceContestDistance() + 24f);
        }

        protected bool ShouldIgnorePumpFake()
        {
            if (difficulty != BLAiDifficulty.Hell)
            {
                return false;
            }

            if (pumpCount > 1)
            {
                return false;
            }

            return !IsOpponentImmediateShotThreat();
        }

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

        protected int MoveTo(float x)
        {
            var delta = player.Position.x - x;
            return Mathf.Abs(delta) <= DeltaDistance ? 0 : delta > 0f ? -1 : 1;
        }

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

        protected float DeltaBallX()
        {
            return player.Position.x - ball.Position.x;
        }

        protected float DeltaBallY()
        {
            return player.Position.y - ball.Position.y;
        }

        protected bool IsBallInReboundZone()
        {
            return Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
        }

        protected bool IsOpponentCloseBehind(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta > 0f && delta <= distance;
        }

        protected bool IsOpponentInRangeBehind(float min = 40f, float max = 180f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta > 0f && delta >= min && delta <= max;
        }

        protected bool IsOpponentCloseToBasket(float distance = 30f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta < 0f && delta + distance >= 0f;
        }

        protected bool IsAICloserForBasket()
        {
            if (opponent == null)
            {
                return false;
            }

            return (player.Position.x - opponent.Position.x) * player.Side < 0f;
        }

        protected bool IsUnderOwnBasket()
        {
            return player.Side == 1 ? player.Position.x > 700f : player.Position.x < 100f;
        }

        protected bool InDashingZone()
        {
            return player.Position.x >= dashZoneStart && player.Position.x <= dashZoneEnd;
        }

        protected bool IsOpponentCloseAbs(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            return Mathf.Abs(player.Position.x - opponent.Position.x) <= distance;
        }

        protected bool IsInAttackZone()
        {
            return player.Side == 1 ? player.Position.x < 600f : player.Position.x > 200f;
        }
    }
}
