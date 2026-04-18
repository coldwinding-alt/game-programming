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
        bool ReadyForAction();
        bool ReleaseBlockOrPump(float dt);
        void Restart(int startSide);
        void PlayerOnGround();
        void PlayerOnDashEnd();
        void PlayerOnBlock();
    }

    public sealed class BLKeyboardController : IBLPlayerController
    {
        private readonly int playerIndex;
        private float lastLeftUp = -10f;
        private float lastRightUp = -10f;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        public BLKeyboardController(int playerIndex)
        {
            this.playerIndex = playerIndex;
        }

        public void UpdateController(float dt)
        {
            CurrentMove = 0;
            CurrentDash = 0;
            var leftDown = KeyDownLeft();
            var rightDown = KeyDownRight();

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

            CurrentJump = Input.GetKey(KeyJump());
            CurrentAction = Input.GetKey(KeyAction());
            CurrentBlockOrPump = Input.GetKey(KeyBlock());
            CurrentSuper = Input.GetKey(KeySuper());
        }

        public bool ReadyForAction()
        {
            return !Input.GetKey(KeyAction());
        }

        public bool ReleaseBlockOrPump(float dt)
        {
            return !Input.GetKey(KeyBlock());
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

        private KeyCode KeyDownLeft() => playerIndex == 0 ? KeyCode.A : KeyCode.LeftArrow;
        private KeyCode KeyDownRight() => playerIndex == 0 ? KeyCode.D : KeyCode.RightArrow;
        private KeyCode KeyJump() => playerIndex == 0 ? KeyCode.W : KeyCode.UpArrow;
        private KeyCode KeyBlock() => playerIndex == 0 ? KeyCode.S : KeyCode.DownArrow;
        private KeyCode KeyAction() => playerIndex == 0 ? KeyCode.B : KeyCode.L;
        private KeyCode KeySuper() => playerIndex == 0 ? KeyCode.V : KeyCode.K;
    }

    public class BLAIController : BLBaseAIController
    {
        public BLAIController(BLPlayerObject player)
            : base(player)
        {
        }

        public static IBLPlayerController CreateForBrain(BLPlayerObject player, string brain)
        {
            var index = ParseBrainIndex(brain);
            return index == 1 ? new BLAIController2(player) : new BLAIController(player);
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
        public BLAIController2(BLPlayerObject player)
            : base(player)
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
            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseBehind(180f);
        }
    }

    public abstract class BLBaseAIController : IBLPlayerController
    {
        protected readonly BLPlayerObject player;
        protected readonly BLAiDifficulty difficulty;

        protected BLBallObject ball;
        protected List<BLPlayerObject> opponents;
        protected BLPlayerObject opponent;

        protected readonly SimpleDelay jumpBall = new SimpleDelay(BLObjectsData.IdealJumpBallJump);
        protected readonly FullDelay attack = new FullDelay(BLObjectsData.IdealAttackJump, 0.08f);
        protected readonly SimpleDelay attackJumpDelay = new SimpleDelay(0.5f);
        protected readonly AIUseDelay stealDelay = new AIUseDelay(0.1f, BLObjectsData.StealDuration + 0.12f);
        protected readonly SimpleDelay defenceDelay = new SimpleDelay(0.65f);
        protected readonly FullDelay blockDelay = new FullDelay(0f, 0.2f);
        protected readonly FullDelay reboundDelay = new FullDelay(0.55f, 0.2f);
        protected readonly FullDelay moveDelay = new FullDelay(0.18f, 0.05f);
        protected readonly AIUseDelay dashDecisionDelay = new AIUseDelay(0.1f, 0.24f);
        protected readonly FullDelay megaDunkDelay = new FullDelay(0.5f, 0.5f);

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
        protected int playerNo;
        protected bool initialized;
        protected float attackZoneStart;
        protected float attackZoneEnd;
        protected float dashZoneStart;
        protected float dashZoneEnd;
        protected const float DeltaDistance = 20f;
        protected const float DownTime = 5f;

        public int CurrentMove { get; protected set; }
        public bool CurrentJump { get; protected set; }
        public bool CurrentAction { get; protected set; }
        public bool CurrentBlockOrPump { get; protected set; }
        public bool CurrentSuper { get; protected set; }
        public int CurrentDash { get; protected set; }

        protected BLBaseAIController(BLPlayerObject player)
        {
            this.player = player;
            difficulty = BLInventory.Instance.Difficulty;
            playerNo = player.PlayerNo;
            player.GameCore.PlayerSignals.OnSignal += ProcessPlayerSignal;
            InitZones();
            ResetForRestart();
        }

        public virtual void UpdateController(float dt)
        {
            EnsureRuntimeLinks();
            ResetCurrents();

            if (ball == null || opponents == null || opponents.Count == 0)
            {
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
                    BallInOwnHands();
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
                        BallInOpponentsHands();
                    }

                    StrategyDefence(dt);
                }
            }
            else
            {
                if (strategy != 1)
                {
                    BallOthers();
                }

                if (ball.State == "shooting" || ball.State == "basket")
                {
                    strategy = 4;
                    StrategyRebound(dt);
                }
                else
                {
                    StrategyBallFight(dt);
                }
            }
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

            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseAbs(180f);
            CurrentAction = stealState == 1;
            if (!CurrentAction && !CurrentJump && CurrentMove == 0)
            {
                deltaDownTime += BLConstants.Step;
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
            var ballX = ball.Position.x;
            var offset = ballX - player.Position.x >= 0f ? 10f : -10f;
            CurrentMove = MoveTo(ballX + offset);
            CurrentJump = false;

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
                        CurrentJump = reboundState == 1 && IsBallInReboundZone();
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

            if (megaDunkDelay.Update(dt) == 1)
            {
                CurrentSuper = true;
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
                        attack.Activate();
                        directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                    }
                    else
                    {
                        CurrentMove = move;
                        if (IsOpponentCloseBehind(100f) && dashDecisionDelay.Update(dt) == -1)
                        {
                            if (player.IsDashing || difficulty == BLAiDifficulty.Easy)
                            {
                                dashDecisionDelay.SkipIt();
                            }
                            else
                            {
                                CurrentDash = -player.Side;
                                dashDecisionDelay.Activate();
                            }
                        }
                    }

                    moveDelay.Activate();
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
            CurrentJump = Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
            CurrentMove = CurrentJump ? 0 : player.IsGrounded ? MoveTo(reboundPoint) : 0;
        }

        protected void TryToSteal()
        {
            if (difficulty == BLAiDifficulty.Easy || opponent == null || !opponent.IsGrounded)
            {
                return;
            }

            if (IsOpponentCloseBehind(80f))
            {
                if (UnityEngine.Random.value <= 0.34f)
                {
                    stealDelay.Activate();
                }
                else
                {
                    stealDelay.SkipIt();
                }
            }
            else if (IsOpponentCloseToBasket(45f))
            {
                if (UnityEngine.Random.value <= 0.5f)
                {
                    stealDelay.Activate();
                }
                else
                {
                    stealDelay.SkipIt();
                }
            }
        }

        protected void BallInOwnHands()
        {
            strategy = 2;
            ResetBaseDelays();
            ResetCurrents();
            if (UnityEngine.Random.value <= BLObjectsData.ChanceForThree)
            {
                SetAttackPoint(510f, 510f);
            }
            else
            {
                SetAttackPoint(0f, 0f);
            }
        }

        protected void BallInOpponentsHands()
        {
            strategy = 0;
            ResetBaseDelays();
            ResetCurrents();
            isPumped = false;
            opponent = player.GameCore.FindBallHolder(-player.Side);
        }

        protected void BallOthers()
        {
            strategy = 1;
            ResetCurrents();
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
                else if (side == -player.Side && UnityEngine.Random.value <= 0.35f)
                {
                    defenceDelay.Activate();
                }

                return;
            }

            if (signal == BLPlayerSignalType.Pump)
            {
                if (side == -player.Side && player.CanAct && IsOpponentCloseBehind(90f))
                {
                    if (UnityEngine.Random.value <= 0.42f)
                    {
                        defenceDelay.Activate();
                        stealDelay.Reset();
                        CurrentMove = 0;
                        isPumped = true;
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
                else if (strategy == 0 && player.CanAct && IsOpponentInRangeBehind())
                {
                    if (UnityEngine.Random.value <= 0.55f)
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
                if (player.WithBall && player.IsGrounded && (IsOpponentCloseBehind(80f) || IsOpponentCloseBehind(140f)))
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
            if (UnityEngine.Random.value > 0.5f)
            {
                return;
            }

            var chance = UnityEngine.Random.value;
            if (chance <= 0.1f && player.IsDashing == false)
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
            CurrentSuper = false;
            ResetAllDelays();
            ResetCurrents();
            isPumped = false;
            pumpCount = 0;
            ResetAvoidSteal();
        }

        protected void ResetCurrents()
        {
            CurrentMove = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentDash = 0;
            CurrentBlockOrPump = false;
            CurrentSuper = false;
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
            blockDelay.Reset();
            reboundDelay.Reset();
            megaDunkDelay.Reset();
            ResetBaseDelays();
        }

        protected void ResetAvoidSteal()
        {
            avoidStealJump = false;
            avoidStealMove = 0;
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
                if ((player.Position.x - 450f) * player.Side > 0f && UnityEngine.Random.value <= BLObjectsData.ChanceForThree)
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
