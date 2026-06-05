// 教程模式的对手控制器
// 训练关卡里，对手会按照脚本移动到指定位置、跳跃、投篮，配合玩家完成练习。
// 自由对战时，切换成普通 AI 控制。

namespace mlp
{
    public enum mlpTutorialOpponentMode
    {
        Scripted,
        FreePlay
    }

    public sealed class mlpTutorialOpponentController : IBLPlayerController
    {
        private readonly mlpPlayerObject player;
        private readonly IBLPlayerController fallbackController;
        private mlpTutorialOpponentMode mode = mlpTutorialOpponentMode.Scripted;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        /// <summary>
        /// Create the controller. skillLevel sets how smart the AI is during free play.
        /// </summary>
        public mlpTutorialOpponentController(mlpPlayerObject player, int skillLevel)
        {
            this.player = player;
            fallbackController = mlpAIController.CreateForBrain(player, "B0", skillLevel <= 0 ? 2 : skillLevel);
        }

        /// <summary>
        /// Switch between scripted (follow tutorial instructions) and free play (normal AI).
        /// </summary>
        public void SetMode(mlpTutorialOpponentMode nextMode)
        {
            mode = nextMode;
            if (mode == mlpTutorialOpponentMode.Scripted)
            {
                SetFrameInputs(0, false, false, false, false, 0);
            }
        }

        /// <summary>
        /// Called each frame. In scripted mode the tutorial tells the opponent what to do.
        /// In free play the AI decides on its own.
        /// </summary>
        public void UpdateController(float dt)
        {
            if (mode == mlpTutorialOpponentMode.FreePlay)
            {
                fallbackController.UpdateController(dt);
                CopyInputsFrom(fallbackController);
                return;
            }

            SetFrameInputs(0, false, false, false, false, 0);
            player.GameCore.TutorialFlow?.PopulateOpponentInputs(player, this, dt);
        }

        /// <summary>
        /// Tell the AI that this player is now holding the ball.
        /// </summary>
        public void BallInOwnHands(int holderPlayerNo)
        {
            fallbackController.BallInOwnHands(holderPlayerNo);
        }

        /// <summary>
        /// Tell the AI that the opponent has the ball.
        /// </summary>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
            fallbackController.BallInOpponentsHands(holderPlayerNo);
        }

        /// <summary>
        /// Tell the AI that this player just took a shot.
        /// </summary>
        public void BallOwnShoot(int shooterPlayerNo)
        {
            fallbackController.BallOwnShoot(shooterPlayerNo);
        }

        /// <summary>
        /// Tell the AI that the opponent just took a shot.
        /// </summary>
        public void BallOpponentShoot(int shooterPlayerNo)
        {
            fallbackController.BallOpponentShoot(shooterPlayerNo);
        }

        /// <summary>
        /// Tell the AI that nobody is holding or shooting the ball right now.
        /// </summary>
        public void BallOthers()
        {
            fallbackController.BallOthers();
        }

        /// <summary>
        /// Can the player press the action button right now?
        /// </summary>
        public bool ReadyForAction()
        {
            return mode == mlpTutorialOpponentMode.FreePlay
                ? fallbackController.ReadyForAction()
                : !CurrentAction;
        }

        /// <summary>
        /// Should the player let go of the block/pump button?
        /// </summary>
        public bool ReleaseBlockOrPump(float dt)
        {
            return mode == mlpTutorialOpponentMode.FreePlay
                ? fallbackController.ReleaseBlockOrPump(dt)
                : !CurrentBlockOrPump;
        }

        /// <summary>
        /// Reset the controller when a new round starts.
        /// </summary>
        public void Restart(int startSide)
        {
            fallbackController.Restart(startSide);
            SetFrameInputs(0, false, false, false, false, 0);
        }

        /// <summary>
        /// Called when the player lands on the ground.
        /// </summary>
        public void PlayerOnGround()
        {
            fallbackController.PlayerOnGround();
        }

        /// <summary>
        /// Called when the player finishes a dash.
        /// </summary>
        public void PlayerOnDashEnd()
        {
            fallbackController.PlayerOnDashEnd();
        }

        /// <summary>
        /// Called when the player blocks a shot.
        /// </summary>
        public void PlayerOnBlock()
        {
            fallbackController.PlayerOnBlock();
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// Set all button inputs for this frame (used by the tutorial to script the opponent).
        /// </summary>
        public void SetFrameInputs(int move, bool jump, bool action, bool blockOrPump, bool super, int dash)
        {
            CurrentMove = move;
            CurrentJump = jump;
            CurrentAction = action;
            CurrentBlockOrPump = blockOrPump;
            CurrentSuper = super;
            CurrentDash = dash;
        }

        /// <summary>
        /// Copy button states from another controller (used to mirror AI decisions).
        /// </summary>
        private void CopyInputsFrom(IBLPlayerController controller)
        {
            CurrentMove = controller.CurrentMove;
            CurrentJump = controller.CurrentJump;
            CurrentAction = controller.CurrentAction;
            CurrentBlockOrPump = controller.CurrentBlockOrPump;
            CurrentSuper = controller.CurrentSuper;
            CurrentDash = controller.CurrentDash;
        }
    }
}
