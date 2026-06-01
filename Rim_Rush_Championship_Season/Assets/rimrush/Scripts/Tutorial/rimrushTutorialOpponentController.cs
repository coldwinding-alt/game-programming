namespace rimrush
{
    public sealed class rimrushTutorialOpponentController : IBLPlayerController
    {
        private readonly rimrushPlayerObject player;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        public rimrushTutorialOpponentController(rimrushPlayerObject player)
        {
            this.player = player;
        }

        public void UpdateController(float dt)
        {
            SetFrameInputs(0, false, false, false, false, 0);
            player.GameCore.TutorialFlow?.PopulateOpponentInputs(player, this, dt);
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

        public bool ReadyForAction()
        {
            return !CurrentAction;
        }

        public bool ReleaseBlockOrPump(float dt)
        {
            return !CurrentBlockOrPump;
        }

        public void Restart(int startSide)
        {
            SetFrameInputs(0, false, false, false, false, 0);
        }

        public void PlayerOnGround()
        {
        }

        public void PlayerOnDashEnd()
        {
        }

        public void PlayerOnBlock()
        {
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
    }
}
