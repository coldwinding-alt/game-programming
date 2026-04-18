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
    }

    public sealed class BLKeyboardController : IBLPlayerController
    {
        private readonly int playerIndex;
        private float lastLeftUp = -10f;
        private float lastRightUp = -10f;
        private const float DoubleTapRepeatWindow = 0.46f;

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

            if (Input.GetKeyDown(leftDown))
            {
                if (Time.time - lastLeftUp <= DoubleTapRepeatWindow)
                {
                    CurrentDash = -1;
                }
            }

            if (Input.GetKeyDown(rightDown))
            {
                if (Time.time - lastRightUp <= DoubleTapRepeatWindow)
                {
                    CurrentDash = 1;
                }
            }

            if (Input.GetKeyUp(leftDown))
            {
                lastLeftUp = Time.time;
            }

            if (Input.GetKeyUp(rightDown))
            {
                lastRightUp = Time.time;
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

        private KeyCode KeyDownLeft() => playerIndex == 0 ? KeyCode.A : KeyCode.LeftArrow;
        private KeyCode KeyDownRight() => playerIndex == 0 ? KeyCode.D : KeyCode.RightArrow;
        private KeyCode KeyJump() => playerIndex == 0 ? KeyCode.W : KeyCode.UpArrow;
        private KeyCode KeyBlock() => playerIndex == 0 ? KeyCode.S : KeyCode.DownArrow;
        private KeyCode KeyAction() => playerIndex == 0 ? KeyCode.B : KeyCode.L;
        private KeyCode KeySuper() => playerIndex == 0 ? KeyCode.V : KeyCode.K;
    }

    public sealed class BLAIController : IBLPlayerController
    {
        private readonly BLPlayerObject player;
        private readonly BLAiDifficulty difficulty;
        private float actionCooldown;
        private float jumpCooldown;
        private float dashDecisionCooldown;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        public BLAIController(BLPlayerObject player)
        {
            this.player = player;
            difficulty = BLInventory.Instance.Difficulty;
        }

        public void UpdateController(float dt)
        {
            actionCooldown -= dt;
            jumpCooldown -= dt;
            dashDecisionCooldown -= dt;
            CurrentDash = 0;
            CurrentSuper = false;
            CurrentBlockOrPump = false;
            CurrentAction = false;
            CurrentJump = false;

            var ball = player.GameCore.Ball;
            var ballHolder = player.GameCore.FindBallHolder();
            if (player.WithBall)
            {
                UpdateAttack();
            }
            else if (ballHolder != null && ballHolder.Side != player.Side)
            {
                UpdateDefence(ballHolder);
            }
            else if (ball != null && ball.IsInGame)
            {
                UpdateLooseBall(ball);
            }

            if (jumpCooldown <= 0f && ball != null && ball.IsInGame && ball.Position.y < player.Position.y - 65f && Mathf.Abs(ball.Position.x - player.Position.x) < 80f)
            {
                CurrentJump = true;
                jumpCooldown = 0.8f;
            }
        }

        public bool ReadyForAction() => actionCooldown <= 0.1f;

        public bool ReleaseBlockOrPump(float dt) => true;

        private void UpdateAttack()
        {
            var attackDx = player.AttackTargetX - player.Position.x;
            var attackDirection = attackDx >= 0f ? 1 : -1;
            CurrentMove = Mathf.Abs(attackDx) < 14f ? 0 : attackDirection;

            var defender = player.GameCore.FindClosestOpponent(player);
            if (defender != null)
            {
                var closeBehind = IsOpponentCloseBehindOnDrive(defender, defender.IsMoving ? 140f : 80f);
                if (player.IsGrounded && closeBehind && dashDecisionCooldown <= 0f)
                {
                    if (!defender.IsDashing && Random.value <= 0.42f)
                    {
                        CurrentDash = attackDirection;
                        dashDecisionCooldown = 0.75f + Random.value * 0.25f;
                    }
                    else if (jumpCooldown <= 0f && Random.value <= 0.14f)
                    {
                        CurrentJump = true;
                        jumpCooldown = 0.9f;
                        dashDecisionCooldown = 0.35f;
                    }
                }
            }

            if (actionCooldown <= 0f)
            {
                var nearBasket = Mathf.Abs(player.Position.x - player.AttackTargetX) < 210f;
                if (nearBasket || Random.value < 0.006f)
                {
                    CurrentAction = true;
                    actionCooldown = 0.8f + Random.value * 0.7f;
                }
            }
        }

        private void UpdateDefence(BLPlayerObject holder)
        {
            var desiredX = holder.Position.x + player.Side * 26f;
            var dx = desiredX - player.Position.x;
            CurrentMove = Mathf.Abs(dx) < 10f ? 0 : dx > 0f ? 1 : -1;

            if (difficulty == BLAiDifficulty.Easy)
            {
                UpdateEasyDefence(holder);
                return;
            }

            var holderInStealWindow = holder.IsGrounded && IsOpponentInStealLane(holder, holder.IsDashing ? 45f : 80f);
            if (actionCooldown <= 0f && holderInStealWindow)
            {
                var chance = IsOpponentCloseToBasket(holder, 45f) ? 0.52f : 0.34f;
                if (holder.IsDashing)
                {
                    chance *= 0.4f;
                }

                if (Random.value <= chance)
                {
                    CurrentAction = true;
                    actionCooldown = BLObjectsData.StealDuration + 0.12f + Random.value * 0.18f;
                }
                else
                {
                    actionCooldown = 0.12f + Random.value * 0.14f;
                }
            }
        }

        private void UpdateEasyDefence(BLPlayerObject holder)
        {
            if (jumpCooldown > 0f || !player.IsGrounded)
            {
                return;
            }

            var nearBasket = IsOpponentCloseToBasket(holder, 120f);
            var closeToShooter = Mathf.Abs(holder.Position.x - player.Position.x) <= 74f;
            var shotLane = Mathf.Abs(holder.Position.y - player.Position.y) <= 20f;
            if (nearBasket && closeToShooter && shotLane && Random.value <= 0.12f)
            {
                CurrentJump = true;
                jumpCooldown = 0.75f;
            }
        }

        private void UpdateLooseBall(BLBallObject ball)
        {
            var dx = ball.Position.x - player.Position.x;
            CurrentMove = Mathf.Abs(dx) < 12f ? 0 : dx > 0f ? 1 : -1;

            if (actionCooldown <= 0f &&
                Mathf.Abs(dx) < BLObjectsData.StealDistance &&
                Mathf.Abs(ball.Position.y - (player.Position.y - 35f)) < 95f)
            {
                CurrentAction = true;
                actionCooldown = 0.4f;
            }
        }

        private bool IsOpponentCloseBehindOnDrive(BLPlayerObject opponent, float distance)
        {
            var delta = (player.Position.x - opponent.Position.x) * -player.Side;
            return delta > 0f && delta <= distance;
        }

        private bool IsOpponentInStealLane(BLPlayerObject opponent, float distance)
        {
            var delta = (opponent.Position.x - player.Position.x) * -player.Side;
            return delta > 0f && delta <= distance;
        }

        private bool IsOpponentCloseToBasket(BLPlayerObject opponent, float distance)
        {
            var basketX = player.Side == -1 ? BLObjectsData.BasketCenter : BLObjectsData.BasketCenter2;
            return Mathf.Abs(opponent.Position.x - basketX) <= distance;
        }
    }
}
