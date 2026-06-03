namespace rimrush
{
    public enum rimrushTutorialOpponentMode
    {
        Scripted,
        FreePlay
    }

    public sealed class rimrushTutorialOpponentController : IBLPlayerController
    {
        private readonly rimrushPlayerObject player;
        private readonly IBLPlayerController fallbackController;
        private rimrushTutorialOpponentMode mode = rimrushTutorialOpponentMode.Scripted;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        public rimrushTutorialOpponentController(rimrushPlayerObject player, int skillLevel)
        {
            this.player = player;
            fallbackController = rimrushAIController.CreateForBrain(player, "B0", skillLevel <= 0 ? 2 : skillLevel);
        }

        public void SetMode(rimrushTutorialOpponentMode nextMode)
        {
            mode = nextMode;
            if (mode == rimrushTutorialOpponentMode.Scripted)
            {
                SetFrameInputs(0, false, false, false, false, 0);
            }
        }

        public void UpdateController(float dt)
        {
            if (mode == rimrushTutorialOpponentMode.FreePlay)
            {
                fallbackController.UpdateController(dt);
                CopyInputsFrom(fallbackController);
                return;
            }

            SetFrameInputs(0, false, false, false, false, 0);
            player.GameCore.TutorialFlow?.PopulateOpponentInputs(player, this, dt);
        }

        public void BallInOwnHands(int holderPlayerNo)
        {
            fallbackController.BallInOwnHands(holderPlayerNo);
        }

        public void BallInOpponentsHands(int holderPlayerNo)
        {
            fallbackController.BallInOpponentsHands(holderPlayerNo);
        }

        public void BallOwnShoot(int shooterPlayerNo)
        {
            fallbackController.BallOwnShoot(shooterPlayerNo);
        }

        public void BallOpponentShoot(int shooterPlayerNo)
        {
            fallbackController.BallOpponentShoot(shooterPlayerNo);
        }

        public void BallOthers()
        {
            fallbackController.BallOthers();
        }

        public bool ReadyForAction()
        {
            return mode == rimrushTutorialOpponentMode.FreePlay
                ? fallbackController.ReadyForAction()
                : !CurrentAction;
        }

        public bool ReleaseBlockOrPump(float dt)
        {
            return mode == rimrushTutorialOpponentMode.FreePlay
                ? fallbackController.ReleaseBlockOrPump(dt)
                : !CurrentBlockOrPump;
        }

        public void Restart(int startSide)
        {
            fallbackController.Restart(startSide);
            SetFrameInputs(0, false, false, false, false, 0);
        }

        public void PlayerOnGround()
        {
            fallbackController.PlayerOnGround();
        }

        public void PlayerOnDashEnd()
        {
            fallbackController.PlayerOnDashEnd();
        }

        public void PlayerOnBlock()
        {
            fallbackController.PlayerOnBlock();
            CurrentBlockOrPump = false;
        }

        public void SetFrameInputs(int move, bool jump, bool action, bool blockOrPump, bool super, int dash)
        {
            CurrentMove = move;
            CurrentJump = jump;
            CurrentAction = action;
            CurrentBlockOrPump = blockOrPump;
            CurrentSuper = super;
            CurrentDash = dash;
        }

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
