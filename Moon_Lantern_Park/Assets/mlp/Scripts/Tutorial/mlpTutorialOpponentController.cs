// Tutorial Mode Opponent Controller

// In the training level, the opponent will move to the designated position, jump, and shoot according to the script, and cooperate with the player to complete the exercise.
// During free battle, switch to normal AI control.

namespace mlp
{
    /// <summary>Tutorial opponent mode: script mode (practice with the player according to preset actions) or free battle mode (switch to normal AI). </summary>
    public enum mlpTutorialOpponentMode
    {
        Scripted,
        FreePlay
    }

    /// <summary>
    /// Tutorial opponent controller: During the tutorial exercises, control the opponent to move to a designated position, jump, and shoot according to the script, and cooperate with the player to complete the practice actions.
    /// </summary>
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
        /// Create a tutorial opponent controller. skillLevel is the four-level AI skill index (0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell).
        /// </summary>
        public mlpTutorialOpponentController(mlpPlayerObject player, int skillLevel)
        {
            // 1. Save a reference to the opponent’s player object

            this.player = player;
            // 2. Create a normal AI controller as a backup - when the tutorial enters the free battle phase, the opponent will switch to this AI to act autonomously
            //    0 is now a valid Easy index and can no longer be treated as an invalid value to fallback to a higher difficulty.
            var fallbackSkillIndex = UnityEngine.Mathf.Clamp(skillLevel, 0, mlpAISkillsData.MaxSkillIndex);
            fallbackController = mlpAIController.CreateForBrain(player, "B0", fallbackSkillIndex);
        }

        /// <summary>
        /// Switch between script mode (act according to tutorial instructions) and free battle mode (normal AI control).
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
        /// Called every frame. In the script mode, the opponent's behavior is controlled by the tutorial process, and in the free battle mode, the AI ​​makes decisions independently.
        /// </summary>
        public void UpdateController(float dt)
        {
            // 1. If it is a free battle mode, let the backup AI make decisions independently, and then synchronize its button status

            if (mode == mlpTutorialOpponentMode.FreePlay)
            {
                fallbackController.UpdateController(dt);
                SyncInputsFrom(fallbackController);
                return;
            }

            // 2. Script mode: First clear all buttons, and then let the tutorial process system fill in the actions that the opponent should do in this frame (move to the designated position, jump, shoot, etc.)

            SetFrameInputs(0, false, false, false, false, 0);
            player.GameCore.TutorialFlow?.PopulateOpponentInputs(player, this, dt);
        }

        /// <summary>
        /// Notify AI: The current player has the ball.

        /// </summary>
        public void BallInOwnHands(int holderPlayerNo)
        {
            fallbackController.BallInOwnHands(holderPlayerNo);
        }

        /// <summary>
        /// Notify the AI ​​that the opponent has the ball.

        /// </summary>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
            fallbackController.BallInOpponentsHands(holderPlayerNo);
        }

        /// <summary>
        /// Notify AI: The current player has just finished shooting.

        /// </summary>
        public void BallOwnShoot(int shooterPlayerNo)
        {
            fallbackController.BallOwnShoot(shooterPlayerNo);
        }

        /// <summary>
        /// Notify AI: The opponent has just completed a shot.
        /// </summary>
        public void BallOpponentShoot(int shooterPlayerNo)
        {
            fallbackController.BallOpponentShoot(shooterPlayerNo);
        }

        /// <summary>
        /// Notify AI: No one is currently holding the ball or shooting.

        /// </summary>
        public void BallOthers()
        {
            fallbackController.BallOthers();
        }

        /// <summary>
        /// Determine whether the player can currently press the action key.

        /// </summary>
        public bool ReadyForAction()
        {
            return mode == mlpTutorialOpponentMode.FreePlay
                ? fallbackController.ReadyForAction()
                : !CurrentAction;
        }

        /// <summary>
        /// Determines whether the player should release the guard/feint key.

        /// </summary>
        public bool ReleaseBlockOrPump(float dt)
        {
            return mode == mlpTutorialOpponentMode.FreePlay
                ? fallbackController.ReleaseBlockOrPump(dt)
                : !CurrentBlockOrPump;
        }

        /// <summary>
        /// Reset controller state at the start of a new round.

        /// </summary>
        public void Restart(int startSide)
        {
            fallbackController.Restart(startSide);
            SetFrameInputs(0, false, false, false, false, 0);
        }

        /// <summary>
        /// Called when the player lands.

        /// </summary>
        public void PlayerOnGround()
        {
            fallbackController.PlayerOnGround();
        }

        /// <summary>
        /// Called when the player's sprint ends.
        /// </summary>
        public void PlayerOnDashEnd()
        {
            fallbackController.PlayerOnDashEnd();
        }

        /// <summary>
        /// Called when a player successfully blocks a shot.

        /// </summary>
        public void PlayerOnBlock()
        {
            fallbackController.PlayerOnBlock();
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// Sets all key inputs for this frame (used by the tutorial system to script opponent behavior).

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
        /// Sync key state from another controller (used to synchronize AI decision results).
        /// </summary>
        private void SyncInputsFrom(IBLPlayerController controller)
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
