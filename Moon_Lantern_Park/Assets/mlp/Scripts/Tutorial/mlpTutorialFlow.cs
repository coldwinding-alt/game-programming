using UnityEngine;

// Tutorial Process Controller

// Manage the 10 practice steps of the entire novice tutorial: moving, sprinting, shooting, fake action, dunk, steal, block, tip-back, ultimate move, free play.

// Operation prompts will be displayed at each step, and the player will automatically proceed to the next step after completion.


namespace mlp
{
    /// <summary>
    /// Tutorial process controller: manages the 10 practice steps of the novice tutorial (moving, sprinting, shooting, feinting, dunking, stealing, blocking, cover-up dunking, ultimate moves, free battles), displaying operation prompts for each step, and automatically entering the next step after completion.

    /// </summary>
    public sealed class mlpTutorialFlow
    {
        private enum TutorialPhase
        {
            IntroFreeze,
            Active,
            SuccessPause,
            Outro
        }

        private enum TutorialStep
        {
            Opening,
            Move,
            Dash,
            Shot,
            Pump,
            Dunk,
            Steal,
            Block,
            Putback,
            Super,
            FreePlay
        }

        private enum ShotStage
        {
            Jump,
            PeakShot
        }

        private enum PumpStage
        {
            Fake,
            Finish
        }

        private const int TotalSteps = 10; // Total number of steps

        private const float FullIntroDuration = 0.78f; // Full introduction to freeze time

        private const float RetryIntroDuration = 0.45f; // Retry introduction freeze time

        private const float SuccessPauseDuration = 1.18f; // Successful pause duration

        private const float StepSettleDuration = 0.68f; // Step settlement waiting time

        private const float HintFeedbackDuration = 1.28f; // Prompt feedback display time

        private const float RetryFeedbackDuration = 1.25f; // Retry feedback display time

        private const float ActionFeedbackDuration = 1.08f; // Operation feedback display time

        private const float FreePlayReminderDelay = 6.8f; // Free battle reminder delayed

        private const float FreePlayScoreOutroDelay = 1.25f; // Ending delay after scoring in Free Battle

        private const float SuperCompletionDelay = 0.32f; // The shortest delay in completing the ultimate move

        private const float SuperFallbackCompletionDelay = 1.1f; // The completion of the ultimate move is delayed

        private const float ShotRetryWindow = 4.4f; // Shot retry window

        private const float ShotIdleRetryWindow = 6.8f; // Shooting idle retry window
        private const float PumpFinishWindow = 4.8f; // Fake action completion window

        private const float DunkRetryWindow = 3.7f; // Dunk retry window

        private const float DunkIdleRetryWindow = 6.2f; // Dunk idle retry window

        private const float StealCompletionMinDelay = 0.68f; // Minimum delay in tackling completion

        private const float StealCompletionMaxDelay = 1.08f; // Maximum delay in tackling completion

        private const float BlockDrillPlayerX = 360f; // Block practice player X coordinate

        private const float BlockDrillOpponentX = 430f; // Blocking practice opponent’s X coordinate

        private const float BlockDrillJumpDelay = 0.72f; // Blocking practice to delay opponent's take-off

        private const float BlockDrillRetryWindow = 4.2f; // Block practice retry window

        private const float BlockDrillOpponentAirTimeScale = 0.5f; // Blocking Practice Opponent Air Time Scaling

        private const float BlockShotCueVelocityY = -135f; // Block shot prompt Y speed threshold

        private const float BlockForcedShotDelay = 1.15f; // Blocking shot forces shot delay

        private const float BlockObserveDuration = 1.25f; // Observation time after successful block

        private const float BlockRetryDelay = 0.52f; // Block retry delay

        private const float PutbackRetryWindow = 3.7f; // Compensation deduction retry window

        private const float PutbackIdleRetryWindow = 6.2f; // Compensation for idle retry window

        private const float PutbackLooseBallPickupLock = 0.92f; // Locking time for picking up loose balls


        private readonly mlpGameCore core; // Game core quotes

        private readonly mlpTutorialOverlay overlay; // Tutorial Overlay Reference

        private readonly mlpInventory inventory; // Backpack archive quotes


        private mlpPlayerObject player; // player object

        private mlpPlayerObject opponent; // Opponent object

        private mlpTutorialOpponentController opponentController; // Rival Tutorial Controller

        private TutorialPhase phase; // current stage
        private TutorialStep currentStep; // current step

        private ShotStage shotStage; // Shooting sub-phase

        private PumpStage pumpStage; // feint sub-phase

        private float phaseTimer; // phase timer

        private float activeTimer; // Operation phase timer

        private bool stepHintShown; // Step prompts are shown

        private bool stepRetryHintShown; // Step retry prompt is shown

        private float moveStartX; // Move exercise starting X coordinate

        private bool movedLeft; // Moved left

        private bool movedRight; // Moved right

        private bool moveCompletionPending; // Move completed pending confirmation

        private float moveCompletedAt; // Move completion time

        private bool dashCompletionPending; // Sprint completed to be confirmed

        private float dashCompletedAt; // Sprint completion time

        private bool shotAttempted; // Shot attempted

        private float shotAttemptStartedAt; // Shot attempt start time

        private bool shotPeakValid; // The highest point of the shot is valid

        private bool shotScored; // field goal

        private bool pumpTriggered; // Fake action triggered

        private float pumpTriggeredAt; // Fake action trigger time

        private bool pumpShotAttempted; // Shot after a fake move

        private float pumpShotStartedAt; // Shooting start time after fake action

        private bool pumpBitePending; // The opponent was tricked into taking off and is pending for processing.

        private bool pumpJumpIssued; // The opponent has been tricked into taking off
        private bool dunkJumped; // Already taken off

        private bool dunkAttempted; // Dunk attempted

        private float dunkAttemptStartedAt; // Dunk attempt start time

        private bool dunkScored; // slam dunk

        private bool stealSuccessPending; // Successful steal to be confirmed

        private float stealSuccessAt; // Successful steal time

        private bool blockJumpIssued; // The opponent has taken off

        private bool blockShotIssued; // The opponent has taken a shot

        private float blockShotIssuedAt; // Opponent's action time

        private bool blockJumpPromptShown; // The take-off prompt is displayed

        private bool blockReleasePromptShown; // The action prompt has been displayed

        private bool blockSuccessPending; // Blocking success awaits confirmation

        private float blockSuccessAt; // Block success time

        private bool blockRetryPending; // Block retry pending confirmation

        private float blockRetryAt; // Block retry time

        private bool putbackJumped; // Already taken off

        private bool putbackWindowOpened; // The deduction window has been opened

        private bool putbackAttempted; // Tried to make up for the deduction

        private float putbackAttemptStartedAt; // Compensation attempt start time

        private bool putbackScored; // Point deduction

        private bool superTriggered; // The ultimate has been triggered

        private float superTriggeredAt; // Ultimate trigger time

        private bool freePlayReadyToEnd; // The free battle is ready to end

        private float freePlayScoredAt; // Free play scoring time


        /// <summary>
        /// Initialize the tutorial process and pass in the reference to the game core.
        /// </summary>
        public mlpTutorialFlow(mlpGameCore core)
        {
            // 1. Save the reference to the game core and use it later to operate the game scene

            this.core = core;
            // 2. Get the tutorial overlay (the UI panel that displays the operation prompts)

            overlay = mlpTutorialOverlay.Active;
            // 3. Get the backpack/archive instance (used to save the next step after the tutorial is completed)

            inventory = mlpInventory.Instance;
        }

        /// <summary>
        /// Returns true when the game needs to be paused to allow the player to read instructions.

        /// </summary>
        public bool FreezeGameplay => phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause;

        /// <summary>
        /// Normal speed is maintained throughout the tutorial.

        /// </summary>
        public float GameplayTimeScale => 1f;

        /// <summary>
        /// Start the tutorial: Find players and opponents, then display the opening screen.

        /// </summary>
        public void Start()
        {
            // 1. Find the player on the left (student) and the opponent on the right (witch) from the core of the game

            player = core.PlayersLeft.Count > 0 ? core.PlayersLeft[0] : null;
            opponent = core.PlayersRight.Count > 0 ? core.PlayersRight[0] : null;
            // 2. Obtain the opponent's tutorial controller (used to script the opponent's movements and practice)

            opponentController = opponent?.Controller as mlpTutorialOpponentController;
            // 3. If players, opponents or overlays are missing, exit directly (without starting the tutorial)

            if (player == null || opponent == null || overlay == null)
            {
                return;
            }

            // 4. Set the opponent to "script mode" - the tutorial system controls the opponent's movement and jumping

            opponentController?.SetMode(mlpTutorialOpponentMode.Scripted);
            // 5. Subscribe to player action signals (jumping, shooting, dunking, stealing, etc.) to determine whether the practice is completed.

            core.PlayerSignals.OnSignal += OnPlayerSignal;
            // 6. The opening guide screen is displayed and the tutorial officially begins.

            BeginOpening();
        }

        /// <summary>
        /// Clean up resources when leaving the tutorial.

        /// </summary>
        public void Shutdown()
        {
            core.PlayerSignals.OnSignal -= OnPlayerSignal;
            overlay?.Hide();
        }

        /// <summary>
        /// Main update loop: advances timers, checks exercise completion status, handles skipped instructions.

        /// </summary>
        public void UpdateFrame(float dt)
        {
            // 1. If the overlay or player does not exist, exit directly

            if (overlay == null || player == null)
            {
                return;
            }

            // 2. If the tutorial has entered the end screen, handle the button click of the end screen (replay, training, menu)

            if (phase == TutorialPhase.Outro)
            {
                UpdatePresentation();
                HandleOutroCommand();
                return;
            }

            // 3. Refresh overlay visual effects (score guide lines, etc.)

            UpdatePresentation();
            // 4. Check if the player pressed the "Skip" button, if so, jump to the next step

            if (HandleStepCommand())
            {
                return;
            }

            // 5. If you are currently in the "Introduction Freeze" or "Successful Pause" stage, countdown and wait.

            if (phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    // 5a. End of introduction freeze: If it is the opening scene, enter the first exercise; otherwise, start the operation phase

                    if (phase == TutorialPhase.IntroFreeze)
                    {
                        if (currentStep == TutorialStep.Opening)
                        {
                            BeginMove(true);
                            return;
                        }

                        phase = TutorialPhase.Active;
                        activeTimer = 0f;
                    }
                    else
                    {
                        // 5b. Successful pause ends: automatically enters the next exercise

                        AdvanceAfterSuccess();
                    }
                }

                return;
            }

            // 6. Exercise logic is only executed during the "operational phase"

            if (phase != TutorialPhase.Active)
            {
                return;
            }

            // 7. Accumulate the operation timer and call the corresponding detection method according to the current exercise step.

            activeTimer += dt;
            switch (currentStep)
            {
                case TutorialStep.Move:
                    UpdateMoveStep();
                    break;
                case TutorialStep.Dash:
                    UpdateDashStep();
                    break;
                case TutorialStep.Shot:
                    UpdateShotStep();
                    break;
                case TutorialStep.Pump:
                    UpdatePumpStep();
                    break;
                case TutorialStep.Dunk:
                    UpdateDunkStep();
                    break;
                case TutorialStep.Steal:
                    UpdateStealStep();
                    break;
                case TutorialStep.Block:
                    UpdateBlockStep();
                    break;
                case TutorialStep.Putback:
                    UpdatePutbackStep();
                    break;
                case TutorialStep.Super:
                    UpdateSuperStep();
                    break;
                case TutorialStep.FreePlay:
                    UpdateFreePlayStep();
                    break;
            }
        }

        /// <summary>
        /// The overlay visual effects are refreshed after each frame of game logic settlement.
        /// </summary>
        public void UpdateAfterGameplay(float dt)
        {
            // 1. If the player or overlay does not exist or the end screen has been entered, exit directly.

            if (player == null || overlay == null || phase == TutorialPhase.Outro)
            {
                return;
            }

            // 2. After the game logic is resolved, refresh the visual effects on the overlay (such as 2-point/3-point guide lines)

            UpdatePresentation();
        }

        /// <summary>
        /// Script your opponent's actions in practice (feints, steals, blocks).

        /// </summary>
        public void PopulateOpponentInputs(mlpPlayerObject opponentPlayer, mlpTutorialOpponentController controller, float dt)
        {
            // 1. If the controller does not exist or the tutorial is not in the operation stage, exit directly

            if (controller == null || opponentPlayer == null || phase != TutorialPhase.Active)
            {
                return;
            }

            // 2. Script the opponent’s behavior based on the current practice steps

            switch (currentStep)
            {
                case TutorialStep.Pump:
                {
                    // 2a. Fake action practice: The opponent walks to the designated position and is then tricked into jumping at the right time.

                    var move = MoveTo(opponentPlayer.Position.x, 420f);
                    var jump = false;
                    if (pumpBitePending && Mathf.Abs(opponentPlayer.Position.x - 420f) <= 14f && opponentPlayer.IsGrounded && !pumpJumpIssued)
                    {
                        jump = true;
                        pumpJumpIssued = true;
                        pumpBitePending = false;
                    }

                    controller.SetFrameInputs(move, jump, false, false, false, 0);
                    break;
                }

                case TutorialStep.Steal:
                    // 2b. Stealing practice: The opponent goes to the designated position and stands, waiting for the player to come and steal.

                    controller.SetFrameInputs(MoveTo(opponentPlayer.Position.x, 332f), false, false, false, false, 0);
                    break;

                case TutorialStep.Block:
                {
                    // 2c. Blocking practice: The opponent walks to the shooting position → takes off → shoots at the right time (with slow motion effect)

                    var move = 0;
                    var jump = false;
                    var action = false;
                    if (!blockJumpIssued)
                    {
                        // 2d. Keep walking before you reach the position, and take off when you reach it.

                        if (Mathf.Abs(opponentPlayer.Position.x - BlockDrillOpponentX) > 8f)
                        {
                            move = MoveTo(opponentPlayer.Position.x, BlockDrillOpponentX);
                        }
                        else if (activeTimer >= BlockDrillJumpDelay)
                        {
                            jump = true;
                            blockJumpIssued = true;
                        }
                    }
                    else if (!opponentPlayer.IsGrounded && !blockShotIssued && opponentPlayer.CanThrow)
                    {
                        // 2e. When in the air, wait until the player also takes off or the ball speed reaches the release point before shooting.

                        var playerHasJumped = player != null && !player.IsGrounded;
                        var shooterNearRelease = opponentPlayer.Velocity.y >= BlockShotCueVelocityY;
                        var forcedRelease = activeTimer >= BlockDrillJumpDelay + BlockForcedShotDelay || opponentPlayer.Velocity.y >= -28f;
                        if ((playerHasJumped && shooterNearRelease) || forcedRelease)
                        {
                            action = true;
                            blockShotIssued = true;
                            blockShotIssuedAt = activeTimer;
                        }
                    }

                    controller.SetFrameInputs(move, jump, action, false, false, 0);
                    break;
                }
            }
        }

        /// <summary>
        /// Displays the "Start Here" opening guide before the first exercise.

        /// </summary>
        private void BeginOpening()
        {
            // 1. Set the current step as "Opening" and enter the introduction freezing stage
            currentStep = TutorialStep.Opening;
            phase = TutorialPhase.IntroFreeze;
            phaseTimer = 1.8f;
            activeTimer = 0f;
            // 2. Display the opening guide screen and list all operation button instructions
            overlay.ShowPrelude(
                "START HERE",
                string.Empty,
                "One drill at a time. Press the shown keys.",
                "A / D",
                "W",
                "S",
                "B",
                "N");
        }

        /// <summary>
        /// Exercise 1: Teach players to move. Players need to walk to the left through a marked point, and then to the right through another marked point. Displays "A" and "D" key prompts.
        /// </summary>
        private void BeginMove(bool fullIntro)
        {
            // 1. Reset step status and set to "Move" exercise
            ResetStep(fullIntro);
            currentStep = TutorialStep.Move;
            // 2. Record the player's starting position and initialize the left and right movement completion flag.

            moveStartX = 360f;
            movedLeft = false;
            movedRight = false;
            moveCompletionPending = false;
            moveCompletedAt = 0f;
            // 3. Set the opponent to script mode and clear old overlays

            PrepareScriptedStep();
            // 4. Reset the game scene: the player is in the middle, the opponent is far away, and the player holds the ball
            core.TutorialResetScenario(
                new Vector2(moveStartX, mlpObjectsData.PlayerIndentY),
                new Vector2(652f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            // 5. Display operation tips: move left and right
            overlay.ShowStep(
                0,
                TotalSteps,
                "MOVE",
                string.Empty,
                string.Empty,
                "Move left, then right.",
                "A",
                "D");
        }

        /// <summary>
        /// Exercise 2: Teach players to sprint. Players need to quickly double-tap A or D to trigger a burst of speed. Show double-click key prompt.
        /// </summary>
        private void BeginDash(bool fullIntro)
        {
            // 1. Reset step status and set to "Sprint" exercise
            ResetStep(fullIntro);
            currentStep = TutorialStep.Dash;
            dashCompletionPending = false;
            dashCompletedAt = 0f;
            // 2. Prepare scripted scenarios

            PrepareScriptedStep();
            // 3. Reset the scene: the player is on the left and the opponent is far away

            core.TutorialResetScenario(
                new Vector2(286f, mlpObjectsData.PlayerIndentY),
                new Vector2(652f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            // 4. Display operation tips: quickly double-click A or D to sprint
            overlay.ShowStep(
                1,
                TotalSteps,
                "DASH",
                string.Empty,
                string.Empty,
                "Double-tap one side to dash.",
                "A",
                "A",
                "D",
                "D");
        }

        /// <summary>
        /// Exercise 3: Teach players to shoot. The player needs to jump (W) first and then press B at the highest point of the jump for the best chance of hitting. Also shown is the 2-point/3-point score line.
        /// </summary>
        private void BeginShot(bool fullIntro)
        {
            // 1. Reset step status and set to "shooting" exercise

            ResetStep(fullIntro);
            currentStep = TutorialStep.Shot;
            // 2. Initialize the shooting phase (jump first), try flag and score flag
            shotStage = ShotStage.Jump;
            shotAttempted = false;
            shotAttemptStartedAt = 0f;
            shotPeakValid = false;
            shotScored = false;
            // 3. Prepare scripted scenarios

            PrepareScriptedStep();
            // 4. Reset scene: The player is near the three-point line, the opponent is far away, and the player holds the ball

            core.TutorialResetScenario(
                new Vector2(552f, mlpObjectsData.PlayerIndentY),
                new Vector2(654f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 5. Preset the perfect shooting timing (lowering the difficulty to make it easier for players to succeed)
            player.TutorialPrimePerfectShot();
            // 6. Display operation tips: shoot at the highest point after jumping, the position determines 2 points or 3 points

            overlay.ShowStep(
                2,
                TotalSteps,
                "JUMP SHOT",
                string.Empty,
                string.Empty,
                "Jump spot decides points.\nNear rim=2PT. Behind line=3PT.",
                "W",
                "B");
            // 7. Display 2-point/3-point scoring lines on the court

            overlay.SetScoringGuide(player.Side, true);
        }

        /// <summary>
        /// Exercise 4: Fake the shot.

        /// </summary>
        private void BeginPump(bool fullIntro)
        {
            // 1. Reset step status and set to "Fake" exercise

            ResetStep(fullIntro);
            currentStep = TutorialStep.Pump;
            // 2. Initialize the feint phase and all related flags

            pumpStage = PumpStage.Fake;
            pumpTriggered = false;
            pumpTriggeredAt = 0f;
            pumpShotAttempted = false;
            pumpShotStartedAt = 0f;
            pumpBitePending = false;
            pumpJumpIssued = false;
            // 3. Prepare scripted scenarios

            PrepareScriptedStep();
            // 4. Clear the last guaranteed score setting and reset the scene
            core.Ball.TutorialClearGuaranteedScore();
            core.TutorialResetScenario(
                new Vector2(344f, mlpObjectsData.PlayerIndentY),
                new Vector2(426f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 5. Display operation tips: press and hold S to make a fake move, release it and shoot

            overlay.ShowStep(
                3,
                TotalSteps,
                "PUMP FAKE",
                string.Empty,
                string.Empty,
                "Hold S to fake. Release, then shoot.",
                "S",
                "B");
        }

        /// <summary>
        /// Exercise 5: Take off and dunk near the rim.

        /// </summary>
        private void BeginDunk(bool fullIntro)
        {
            // 1. Reset step status and set to "slam dunk" exercise

            ResetStep(fullIntro);
            currentStep = TutorialStep.Dunk;
            // 2. Initialize dunk related flags

            dunkJumped = false;
            dunkAttempted = false;
            dunkAttemptStartedAt = 0f;
            dunkScored = false;
            // 3. Prepare scripted scenarios

            PrepareScriptedStep();
            // 4. Reset scene: The player is near the basket, the opponent is far away, and the player holds the ball

            core.TutorialResetScenario(
                new Vector2(642f, mlpObjectsData.PlayerIndentY),
                new Vector2(516f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 5. Preset perfect dunk timing (reduce difficulty)

            player.TutorialPrimePerfectDunk();
            // 6. Display operation tips: press B after taking off near the basket to dunk.

            overlay.ShowStep(
                4,
                TotalSteps,
                "DUNK",
                string.Empty,
                string.Empty,
                "Jump near the rim. Press B.",
                "W",
                "B");
        }

        /// <summary>
        /// Exercise 6: Get close to your opponent and steal.

        /// </summary>
        private void BeginSteal(bool fullIntro)
        {
            // 1. Reset step status and set to "steal" practice

            ResetStep(fullIntro);
            currentStep = TutorialStep.Steal;
            stealSuccessPending = false;
            stealSuccessAt = 0f;
            // 2. Prepare scripted scenarios

            PrepareScriptedStep();
            // 3. Reset the scene: the player is on the left and the opponent holds the ball to the right of the center.

            core.TutorialResetScenario(
                new Vector2(286f, mlpObjectsData.PlayerIndentY),
                new Vector2(518f, mlpObjectsData.PlayerIndentY),
                false,
                true,
                1f,
                -1f);
            // 4. Display operation tips: press B after getting close to the opponent to steal

            overlay.ShowStep(
                5,
                TotalSteps,
                "STEAL",
                string.Empty,
                string.Empty,
                "Get close, then press B.",
                "B");
        }

        /// <summary>
        /// Exercise 7: Timing your jump to block shots.
        /// </summary>
        private void BeginBlock(bool fullIntro)
        {
            // 1. Reset step status and set to "Block" exercise

            ResetStep(fullIntro);
            currentStep = TutorialStep.Block;
            // 2. Initialize all flags of the block practice (opponent jump, shot, success, retry, etc.)

            blockJumpIssued = false;
            blockShotIssued = false;
            blockShotIssuedAt = 0f;
            blockJumpPromptShown = false;
            blockReleasePromptShown = false;
            blockSuccessPending = false;
            blockSuccessAt = 0f;
            blockRetryPending = false;
            blockRetryAt = 0f;
            // 3. Prepare scripted scenarios

            PrepareScriptedStep();
            // 4. Reset scene: Player and opponent stand close to each other, opponent holds the ball

            core.TutorialResetScenario(
                new Vector2(BlockDrillPlayerX, mlpObjectsData.PlayerIndentY),
                new Vector2(BlockDrillOpponentX, mlpObjectsData.PlayerIndentY),
                false,
                true,
                -1f,
                -1f);
            // 5. Slow down the opponent’s movement speed in the air (slow-motion effect makes it easier for players to see the timing of shooting)

            opponent.TutorialSetAirMotionTimeScale(BlockDrillOpponentAirTimeScale);
            // 6. Turn on block assist (increase block judgment range)

            player.TutorialSetJumpBlockAssist(true);
            // 7. Display operation tips: observe the slow-motion shot, press W when shooting to take off and block the shot

            overlay.ShowStep(
                6,
                TotalSteps,
                "BLOCK",
                string.Empty,
                string.Empty,
                "Read the slow shot. Press W late.",
                "W");
        }

        /// <summary>
        /// Exercise 8: Rebound and tip-in.

        /// </summary>
        private void BeginPutback(bool fullIntro)
        {
            // 1. Reset the step status and set it to "compensation deduction" exercise

            ResetStep(fullIntro);
            currentStep = TutorialStep.Putback;
            // 2. Initialize subsidy related flags

            putbackJumped = false;
            putbackWindowOpened = false;
            putbackAttempted = false;
            putbackAttemptStartedAt = 0f;
            putbackScored = false;
            // 3. Prepare scripted scenarios

            PrepareScriptedStep();
            // 4. Reset scene: The player is on the right side of the basket, the opponent is far away, and no one is holding the ball.

            core.TutorialResetScenario(
                new Vector2(666f, mlpObjectsData.PlayerIndentY),
                new Vector2(516f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            // 5. Launch a rebound ball (simulating the rebound after shooting)

            core.Ball.TutorialLaunchPutbackBounce(player.Side, PutbackLooseBallPickupLock);
            // 6. Default perfect timing for compensation (reduces difficulty)

            player.TutorialPrimePutbackDunk();
            // 7. Display operation tips: rush for the rebound, press B after taking off to make up for the dunk.

            overlay.ShowStep(
                7,
                TotalSteps,
                "PUTBACK",
                string.Empty,
                string.Empty,
                "Crash the rebound. Jump, then B.",
                "W",
                "B");
        }

        /// <summary>
        /// Exercise 9: Shoot using your ultimate skill.

        /// </summary>
        private void BeginSuper(bool fullIntro)
        {
            // 1. Reset the step status and set it to "Ultimate Move" practice

            ResetStep(fullIntro);
            currentStep = TutorialStep.Super;
            superTriggered = false;
            superTriggeredAt = 0f;
            // 2. Prepare scripted scenarios

            PrepareScriptedStep();
            // 3. Reset scene: The player holds the ball on the left side and the opponent is far away

            core.TutorialResetScenario(
                new Vector2(242f, mlpObjectsData.PlayerIndentY),
                new Vector2(654f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 4. Fill the player’s ultimate energy with energy

            player.TutorialChargeSuper();
            // 5. The operation prompt is displayed: the energy is full, press N to release the ultimate move

            overlay.ShowStep(
                8,
                TotalSteps,
                "SUPER",
                string.Empty,
                string.Empty,
                "Energy is full. Press N.",
                "N");
        }

        /// <summary>
        /// The final step: Free Battle - Score one goal to complete the tutorial.

        /// </summary>
        private void BeginFreePlay(bool fullIntro)
        {
            // 1. Reset step status and set to "Free Battle" exercise

            ResetStep(fullIntro);
            currentStep = TutorialStep.FreePlay;
            freePlayReadyToEnd = false;
            freePlayScoredAt = 0f;
            // 2. Prepare scripted scenarios

            PrepareScriptedStep();
            // 3. Reset scene: player holds the ball, opponent is far away

            core.TutorialResetScenario(
                new Vector2(238f, mlpObjectsData.PlayerIndentY),
                new Vector2(606f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 4. Switch the opponent to free battle mode (defend and attack normally, no longer script mode)

            opponentController?.SetMode(mlpTutorialOpponentMode.FreePlay);
            // 5. Display operation tips: complete the tutorial by scoring one ball

            overlay.ShowStep(
                9,
                TotalSteps,
                "LIVE PLAY",
                string.Empty,
                string.Empty,
                "Score one basket to finish.",
                "A / D",
                "W",
                "S",
                "B",
                "N");
            // 6. Clear all visual aids (guiding lines, etc.) on the overlay to allow players to play freely
            ClearOverlayHighlights();
        }

        /// <summary>
        /// Resets all step states and starts the intro freeze timer.

        /// </summary>
        private void ResetStep(bool fullIntro)
        {
            // 1. Enter the introduction freeze stage (pause the game to let players read the instructions)

            phase = TutorialPhase.IntroFreeze;
            // 2. Use a longer freezing time when entering for the first time, and use a shorter time when retrying.

            phaseTimer = fullIntro ? FullIntroDuration : RetryIntroDuration;
            // 3. Reset operation timer and prompt display flag

            activeTimer = 0f;
            stepHintShown = false;
            stepRetryHintShown = false;
            // 4. Reset ultimate move status

            superTriggered = false;
            superTriggeredAt = 0f;
            // 5. Turn off the blocking assist (turn it on individually as needed for each step)
            player?.TutorialSetJumpBlockAssist(false);
        }

        /// <summary>
        /// Set the opponent to script mode and clear the old overlay visual elements.
        /// </summary>
        private void PrepareScriptedStep()
        {
            opponentController?.SetMode(mlpTutorialOpponentMode.Scripted);
            ClearOverlayHighlights();
        }

        /// <summary>
        /// Removes all focus boxes, halos, guides, and track points on the overlay.
        /// </summary>
        private void ClearOverlayHighlights()
        {
            overlay.ClearFocus();
            overlay.SetApexRing(Vector2.zero, 0f, false);
            overlay.SetEnergyPulse(false);
            overlay.SetTargetRect(0f, 0f, 0f, 0f);
            overlay.SetScoringGuide(0, false);
            overlay.SetTrajectory(null);
        }

        /// <summary>
        /// Checks if the player has completed moving left or right. Display a prompt if stuck.
        /// </summary>
        private void UpdateMoveStep()
        {
            // 1. Detect whether the player has moved a sufficient distance to the left

            if (player.Position.x <= moveStartX - 42f)
            {
                movedLeft = true;
            }

            // 2. Detect whether the player has moved a sufficient distance to the right

            if (player.Position.x >= moveStartX + 42f)
            {
                movedRight = true;
            }

            // 3. Both left and right have been moved and exceeded the minimum time → mark completed and display success feedback
            if (!moveCompletionPending && movedLeft && movedRight && activeTimer >= 2.2f)
            {
                moveCompletionPending = true;
                moveCompletedAt = activeTimer;
                overlay.ShowFeedback("Good. Keep that court feel.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                return;
            }

            // 4. After completion, wait for a short period of time before proceeding to the next step (let the player see the success prompt)

            if (moveCompletionPending)
            {
                if (activeTimer - moveCompletedAt >= StepSettleDuration)
                {
                    CompleteStep("Left and right are yours.");
                }

                return;
            }

            // 5. Not completed for more than 3.4 seconds → display the first prompt

            if (activeTimer >= 3.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Try both A and D.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 6. Not completed within 7.4 seconds → Display more detailed prompts
            if (activeTimer >= 7.4f && !stepRetryHintShown)
            {
                stepRetryHintShown = true;
                overlay.ShowFeedback("Move left once, then right once.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Checks if the player has completed the sprint. Automatically retry if stuck for too long.
        /// </summary>
        private void UpdateDashStep()
        {
            // 1. If the sprint has been completed, wait for the settlement time before proceeding to the next step.

            if (dashCompletionPending)
            {
                if (activeTimer - dashCompletedAt >= StepSettleDuration)
                {
                    CompleteStep("Dash gives you the burst.");
                }

                return;
            }

            // 2. Not sprinting for more than 3.2 seconds → Display prompt
            if (activeTimer >= 3.2f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Tap A twice, or D twice.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 3. No sprint for more than 6.6 seconds → Automatically reset the exercise and display more detailed prompts
            if (activeTimer >= 6.6f)
            {
                BeginDash(false);
                overlay.ShowFeedback("Two quick taps. Same direction.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Check if the player scores a field goal. Reset if attempt fails.
        /// </summary>
        private void UpdateShotStep()
        {
            // 1. If a field goal has been scored, no further detection will be performed.
            if (shotScored)
            {
                return;
            }

            // 2. More than 2.6 seconds → Display corresponding prompts according to the current stage (in the jumping stage, it prompts to jump first, in the air stage, it prompts to take action at the highest point)

            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(shotStage == ShotStage.Jump ? "W, then B. Watch the 2PT/3PT line." : "Highest point has best accuracy.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 3. Attempted to shoot but missed in timeout → Reset practice

            if (shotAttempted && activeTimer - shotAttemptStartedAt >= ShotRetryWindow)
            {
                BeginShot(false);
                overlay.ShowFeedback("Good try. Jump and release once more.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 4. Haven’t tried shooting for a long time → Reset practice and display prompts
            if (!shotAttempted && activeTimer >= ShotIdleRetryWindow)
            {
                BeginShot(false);
                overlay.ShowFeedback("Jump, then B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Check if the player completed the fake and scored the shot. Reset if stuck.
        /// </summary>
        private void UpdatePumpStep()
        {
            // Phase 1: Wait for the player to make a fake move

            if (!pumpTriggered)
            {
                // 1. No fake action has been performed for more than 2.4 seconds → Display prompt
                if (activeTimer >= 2.4f && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Hold S with the ball.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }

                // 2. No fake action for more than 5.2 seconds → Reset the exercise

                if (activeTimer >= 5.2f)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Sell the fake first.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                }

                return;
            }

            // Phase 2: The fake action has been triggered, waiting for the player to shoot

            if (pumpShotAttempted)
            {
                // 3. The opponent has been tricked into taking off and the ball is not in the air (has been shot) → Practice completed
                if (pumpJumpIssued && !IsBallStillLive())
                {
                    CompleteStep("Fake worked.");
                    return;
                }

                // 4. Shooting timeout not completed → Reset practice

                if (activeTimer - pumpShotStartedAt >= ShotRetryWindow)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Good try. Fake, release, then finish the shot.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                }

                return;
            }

            // Phase 3: The fake action has been triggered but the shot has not yet been made

            // 5. Failure to shoot after more than 2.2 seconds after the fake move → Display the "shoot after letting go" prompt
            if (activeTimer - pumpTriggeredAt >= 2.2f && !stepRetryHintShown)
            {
                stepRetryHintShown = true;
                overlay.ShowFeedback("Let go, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 6. Timeout after feint → Reset exercise
            if (activeTimer - pumpTriggeredAt >= PumpFinishWindow)
            {
                BeginPump(false);
                overlay.ShowFeedback("Fake, then punish.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Check if the player dunked the ball. Displays the "Press B" prompt while in the air.
        /// </summary>
        private void UpdateDunkStep()
        {
            // 1. If you have already scored a dunk, no more testing will be done.
            if (dunkScored)
            {
                return;
            }

            // 2. After the player jumps, the prompt is updated to "Press B to complete the dunk."

            if (player != null && !player.IsGrounded && !dunkJumped)
            {
                dunkJumped = true;
                overlay.UpdateCopy(
                    "PRESS B",
                    "Near the rim.",
                    "FINISH DUNK",
                    "Press B in the air.",
                    "B");
                overlay.ShowFeedback("Press B now.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
            }

            // 3. No dunk for more than 2.5 seconds → Display prompt

            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("W, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 4. Dunk was attempted but timed out → Reset practice
            if (dunkAttempted && activeTimer - dunkAttemptStartedAt >= DunkRetryWindow)
            {
                BeginDunk(false);
                overlay.ShowFeedback("Good try. Press B higher in the paint.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 5. Haven’t tried dunking for a long time → Reset practice
            if (!dunkAttempted && activeTimer >= DunkIdleRetryWindow)
            {
                BeginDunk(false);
                overlay.ShowFeedback("Jump near rim, then B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Check if the player successfully tackled the ball. Start over if your opponent drifts too far.
        /// </summary>
        private void UpdateStealStep()
        {
            // 1. If the steal is successful, wait for the player to get the ball or time out to complete the steps.
            if (stealSuccessPending)
            {
                var elapsed = activeTimer - stealSuccessAt;
                if (elapsed >= StealCompletionMinDelay && (player.WithBall || elapsed >= StealCompletionMaxDelay))
                {
                    CompleteStep("Nice. Defense can start the offense.");
                }

                return;
            }

            // 2. If the opponent drifts too far (not in a reasonable position), reset the practice

            if (opponent != null && opponent.Position.x <= 292f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Get closer before you swipe.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 3. No steal for more than 2.6 seconds → Display the "Close to Opponent" prompt

            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Get close.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 4. No steal for more than 4.6 seconds → Reset practice
            if (activeTimer >= 4.6f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Closer, then B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Check if the player blocks the shot successfully. Display timing cues during practice.
        /// </summary>
        private void UpdateBlockStep()
        {
            // 1. If the shot is blocked successfully, wait for the observation time and then complete the steps.

            if (blockSuccessPending)
            {
                if (activeTimer - blockSuccessAt >= BlockObserveDuration)
                {
                    CompleteStep("Good block. You changed the shot.");
                }

                return;
            }

            // 2. If you are waiting for a retry, restart the blocking practice after the time is up.
            if (blockRetryPending)
            {
                if (activeTimer >= blockRetryAt)
                {
                    BeginBlock(false);
                }

                return;
            }

            // 3. The opponent has taken off but has not yet taken action → Update prompt to "Ready to take off"

            if (blockJumpIssued && !blockShotIssued && !blockJumpPromptShown)
            {
                blockJumpPromptShown = true;
                overlay.UpdateCopy(
                    "GET READY",
                    "Shooter is rising.",
                    "TIME THE JUMP",
                    "Press W near release.",
                    "W");
                overlay.ShowFeedback("Read the slow rise.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 4. When the opponent is about to release (the ball speed is close to the release point) → Update prompt to "Press W now"

            if (blockJumpIssued &&
                !blockShotIssued &&
                !blockReleasePromptShown &&
                opponent != null &&
                !opponent.IsGrounded &&
                opponent.Velocity.y >= BlockShotCueVelocityY)
            {
                blockReleasePromptShown = true;
                overlay.UpdateCopy(
                    "PRESS W",
                    "Shot is leaving.",
                    "MEET THE BALL",
                    "Jump into the release.",
                    "W");
                overlay.ShowFeedback("W now.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
            }

            // 5. More than 2.8 seconds → Display general prompt

            if (activeTimer >= 2.8f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Wait for the release, then W.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 6. The opponent timed out and failed to block the shot → Arrange a retry

            if (blockShotIssued && activeTimer - blockShotIssuedAt >= BlockDrillRetryWindow)
            {
                ScheduleBlockRetry("No block yet. Jump on the W cue.");
            }
        }

        /// <summary>
        /// Queue up and try the blocking drill again after a short delay.

        /// </summary>
        private void ScheduleBlockRetry(string message)
        {
            if (blockRetryPending || blockSuccessPending)
            {
                return;
            }

            blockRetryPending = true;
            blockRetryAt = activeTimer + BlockRetryDelay;
            overlay.ShowFeedback(message, new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
        }

        /// <summary>
        /// Check if the player grabbed the rebound and scored a tip-in.

        /// </summary>
        private void UpdatePutbackStep()
        {
            // 1. If a tip-in has been scored, no further detection will be performed.

            if (putbackScored)
            {
                return;
            }

            // 2. Detect whether the ball enters the "recovery window" (the ball is near the basket and the player can reach it)

            var ball = core.Ball;
            var putbackWindowLive = player != null && player.IsTutorialPutbackBallInWindow(ball);
            if (putbackWindowLive && !putbackWindowOpened)
            {
                // 3. The window opens for the first time → The perfect compensation time is preset, and the update prompt is "Start compensation now"

                putbackWindowOpened = true;
                player.TutorialPrimePutbackDunk();
                overlay.UpdateCopy(
                    "ATTACK THE REBOUND",
                    "Loose ball near the rim.",
                    "W + B NOW",
                    "Catch it high and dunk.",
                    "W",
                    "B");
                overlay.ShowFeedback("Now. Meet it above the rim.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
            }

            // 4. If the window has not been opened after the player takes off, the "Track rebound" prompt will be displayed.

            if (player != null && !player.IsGrounded && !putbackJumped)
            {
                putbackJumped = true;
                if (!putbackWindowOpened)
                {
                    overlay.ShowFeedback("Track the rebound, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }
            }

            // 5. More than 2.4 seconds → Display prompt

            if (activeTimer >= 2.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(
                    putbackWindowOpened ? "Ball is live. W + B." : "Jump into the rebound, then B.",
                    new Color32(0xFF, 0xD1, 0x76, 0xFF),
                    HintFeedbackDuration);
            }

            // 6. The window is open but missed (the ball is no longer in the window position) → Reset practice

            if (putbackWindowOpened && !putbackAttempted && !putbackWindowLive)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Missed the rebound. Attack it earlier.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 7. Tried tip-in but timed out → Reset practice

            if (putbackAttempted && activeTimer - putbackAttemptStartedAt >= PutbackRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Good read. Try the putback again.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 8. Haven’t tried a tip-in for a long time → Reset practice

            if (!putbackAttempted && activeTimer >= PutbackIdleRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Jump into the rebound, then press B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Check whether the player has used the ultimate skill to shoot.

        /// </summary>
        private void UpdateSuperStep()
        {
            // 1. If the ultimate move has been triggered, wait until the ultimate move animation ends to complete the steps

            if (superTriggered)
            {
                var elapsed = activeTimer - superTriggeredAt;
                // 1a. The ultimate shot has ended (no longer in the super shooting state) and the minimum delay has passed, or the pocket time has exceeded

                if ((!player.IsSuperShot && elapsed >= SuperCompletionDelay) ||
                    elapsed >= SuperFallbackCompletionDelay)
                {
                    CompleteStep("Super landed.");
                }

                return;
            }

            // 2. The ultimate move has not been pressed for more than 2.5 seconds → a prompt will be displayed

            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Press N.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), HintFeedbackDuration);
            }

            // 3. Failure to press the ultimate move for more than 4.4 seconds → Reset practice

            if (activeTimer >= 4.4f)
            {
                BeginSuper(false);
                overlay.ShowFeedback("Do not leave the super hidden.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// Free Battle: Wait for the player to score a goal.

        /// </summary>
        private void UpdateFreePlayStep()
        {
            // 1. If you haven’t scored yet

            if (!freePlayReadyToEnd)
            {
                // 2. The reminder delay time is exceeded → the prompt "Score a goal to complete the tutorial" is displayed.

                if (activeTimer >= FreePlayReminderDelay && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Score one basket to finish.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }

                return;
            }

            // 3. After scoring, wait for a short delay before entering the end screen.

            if (activeTimer - freePlayScoredAt < FreePlayScoreOutroDelay)
            {
                return;
            }

            // 4. Delayed end → Display the end screen of tutorial completion
            BeginOutro();
        }

        /// <summary>
        /// Marks the current step complete and displays a success message.

        /// </summary>
        private void CompleteStep(string message)
        {
            phase = TutorialPhase.SuccessPause;
            phaseTimer = SuccessPauseDuration;
            overlay.ShowFeedback(message, new Color32(0x9A, 0xFF, 0xDD, 0xFF), SuccessPauseDuration);
            ClearOverlayHighlights();
        }

        /// <summary>
        /// Shows the end screen with the "Where to go next?" option.

        /// </summary>
        private void BeginOutro()
        {
            phase = TutorialPhase.Outro;
            overlay.ShowOutro(mlpPlayersData.GetCharacterName(player.CharacterId), mlpCharacterSkillsData.Get(player.CharacterId).SkillName);
        }

        /// <summary>
        /// After a successful pause, proceed to the next exercise.

        /// </summary>
        private void AdvanceAfterSuccess()
        {
            // 1. Automatically enter the next exercise based on the steps just completed (advance in sequence according to the tutorial sequence)

            switch (currentStep)
            {
                case TutorialStep.Move:
                    BeginDash(true);       // Move → Dash

                    break;
                case TutorialStep.Dash:
                    BeginShot(true);       // sprint → shoot

                    break;
                case TutorialStep.Shot:
                    BeginPump(true);       // Shoot → Fake

                    break;
                case TutorialStep.Pump:
                    BeginDunk(true);       // Feint → Dunk

                    break;
                case TutorialStep.Dunk:
                    BeginSteal(true);      // Dunk → Steal

                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);      // Steal → Block

                    break;
                case TutorialStep.Block:
                    BeginPutback(true);    // Block → Makeup

                    break;
                case TutorialStep.Putback:
                    BeginSuper(true);      // Compensation deduction → Big move

                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);   // Ultimate move → Free battle

                    break;
                default:
                    BeginOutro();          // Other cases → end screen

                    break;
            }
        }

        /// <summary>
        /// Check if the overlay has sent instructions (such as skipping).

        /// </summary>
        private bool HandleStepCommand()
        {
            // 1. Read button instructions on the overlay

            var command = overlay.ConsumeCommand();
            // 2. If there is no instruction, return false and continue the normal process.

            if (command == mlpTutorialOverlayCommand.None)
            {
                return false;
            }

            // 3. If it is a "skip" instruction, jump to the next exercise step

            if (command == mlpTutorialOverlayCommand.SkipStep)
            {
                SkipCurrentStep();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Jump to the next step when the player presses skip.

        /// </summary>
        private void SkipCurrentStep()
        {
            // 1. The player presses the skip button → proceeds directly to the next exercise step (same as normal sequence)

            switch (currentStep)
            {
                case TutorialStep.Opening:
                    BeginMove(true);       // Opening → Move

                    break;
                case TutorialStep.Move:
                    BeginDash(true);       // Move → Dash

                    break;
                case TutorialStep.Dash:
                    BeginShot(true);       // sprint → shoot

                    break;
                case TutorialStep.Shot:
                    BeginPump(true);       // Shoot → Fake

                    break;
                case TutorialStep.Pump:
                    BeginDunk(true);       // Feint → Dunk

                    break;
                case TutorialStep.Dunk:
                    BeginSteal(true);      // Dunk → Steal

                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);      // Steal → Block

                    break;
                case TutorialStep.Block:
                    BeginPutback(true);    // Block → Makeup

                    break;
                case TutorialStep.Putback:
                    BeginSuper(true);      // Compensation deduction → Big move

                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);   // Ultimate move → Free battle

                    break;
                default:
                    BeginOutro();          // Other cases → end screen

                    break;
            }
        }

        /// <summary>
        /// Monitor player actions (sprinting, shooting, dunking, stealing, blocking, scoring, etc.).

        /// </summary>
        private void OnPlayerSignal(mlpPlayerSignalType signal, int side, int playerNo)
        {
            // 1. If the player does not exist or the tutorial is not in the operation phase, ignore all signals

            if (player == null || phase != TutorialPhase.Active)
            {
                return;
            }

            // 2. Special treatment: The opponent scores during the blocking practice → Arrange a retry (the player did not block)
            if (currentStep == TutorialStep.Block &&
                signal == mlpPlayerSignalType.Score &&
                opponent != null &&
                side == opponent.Side &&
                playerNo == opponent.PlayerNo)
            {
                ScheduleBlockRetry("Try again. Jump on the release cue.");
                return;
            }

            // 3. Only process the player's own signals (ignore the opponent's signals)

            if (side != player.Side || playerNo != player.PlayerNo)
            {
                return;
            }

            // 4. Process according to signal type

            switch (signal)
            {
                case mlpPlayerSignalType.Dash:
                    // 4a. Sprint signal: During sprint practice, mark sprint completion

                    if (currentStep == TutorialStep.Dash && !dashCompletionPending)
                    {
                        dashCompletionPending = true;
                        dashCompletedAt = activeTimer;
                        overlay.ShowFeedback("Good dash. Feel the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.JumpA:
                    // 4b. Take-off signal: In shooting practice, the update prompt is "shoot at the highest point"

                    if (currentStep == TutorialStep.Shot)
                    {
                        shotStage = ShotStage.PeakShot;
                        overlay.UpdateCopy(
                            "TOP RELEASE",
                            "Best accuracy.",
                            "SHOOT AT PEAK",
                            "B at the top.\nJump spot decides 2PT or 3PT.",
                            "B");
                        overlay.ShowFeedback("Highest point has best accuracy.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Shoot:
                    // 4c. Shot signal

                    if (currentStep == TutorialStep.Shot)
                    {
                        // 4c-i. Shooting practice: record the timing of the shot and determine whether it is shot at the highest point

                        shotAttemptStartedAt = activeTimer;
                        shotPeakValid = shotStage == ShotStage.PeakShot && !player.IsGrounded;
                        shotAttempted = shotPeakValid;
                        var shotValueText = core.MatchProcessor.ThrowType == 0 ? "3PT range." : "2PT range.";
                        var shotFeedback = Mathf.Abs(player.Velocity.y) <= 140f
                            ? $"{shotValueText} Peak release."
                            : $"{shotValueText} Top is best.";
                        overlay.ShowFeedback(
                            shotPeakValid ? shotFeedback : "Jump first, then B.",
                            shotPeakValid ? new Color32(0x9A, 0xFF, 0xDD, 0xFF) : new Color32(0xFF, 0xD1, 0x76, 0xFF),
                            ActionFeedbackDuration);
                    }
                    else if (currentStep == TutorialStep.Pump && pumpTriggered)
                    {
                        // 4c-ii. Feint practice: shoot after a fake move and give different feedback according to whether the opponent has been deceived.

                        pumpShotAttempted = true;
                        pumpShotStartedAt = activeTimer;
                        if (pumpJumpIssued)
                        {
                            overlay.ShowFeedback(
                                player.IsGrounded ? "Good release. Watch it finish." : "That counts. Watch it finish.",
                                new Color32(0x9A, 0xFF, 0xDD, 0xFF),
                                ActionFeedbackDuration);
                        }
                        else
                        {
                            overlay.ShowFeedback("Make him leave his feet first.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
                        }
                    }
                    break;

                case mlpPlayerSignalType.Pump:
                    // 4d. Feint signal: In the feint exercise, the player performs a feint → let the opponent be tricked into jumping and set a guaranteed score

                    if (currentStep == TutorialStep.Pump && pumpStage == PumpStage.Fake)
                    {
                        pumpTriggered = true;
                        pumpTriggeredAt = activeTimer;
                        pumpStage = PumpStage.Finish;
                        pumpBitePending = true;
                        player.TutorialPrimePerfectShot();
                        core.Ball.TutorialSetGuaranteedScore();
                        overlay.UpdateCopy(
                            "RELEASE + B",
                            "Defender jumped.",
                            "SHOOT NOW",
                            "Let go, then B.",
                            "S",
                            "B");
                        overlay.ShowFeedback("He bit.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Dunk:
                    // 4e. Dunk Signal: Mark dunk attempt

                    if (currentStep == TutorialStep.Dunk)
                    {
                        // 4e-i. Dunk Drill: Record Dunk Attempts

                        dunkAttempted = true;
                        dunkAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Strong take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    else if (currentStep == TutorialStep.Putback)
                    {
                        // 4e-ii. Make-up practice: record make-up attempts

                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Putback take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.StealSuccess:
                    // 4f. Signal of successful steal: Mark the successful steal and wait for confirmation.

                    if (currentStep == TutorialStep.Steal)
                    {
                        stealSuccessPending = true;
                        stealSuccessAt = activeTimer;
                        overlay.ShowFeedback("Nice steal.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Block:
                    // 4g. Block success signal: Mark the block successfully, cancel the retry, and display the observation prompt.

                    if (currentStep == TutorialStep.Block && !blockSuccessPending)
                    {
                        blockSuccessPending = true;
                        blockSuccessAt = activeTimer;
                        blockRetryPending = false;
                        overlay.UpdateCopy(
                            "BLOCKED",
                            "Watch the ball.",
                            "SHOT CHANGED",
                            "That slow read became a block.",
                            "W");
                        overlay.ShowFeedback("Blocked. Watch the result.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), BlockObserveDuration);
                    }
                    break;

                case mlpPlayerSignalType.PutbackDunk:
                    // 4h. Compensation signal: mark compensation attempt

                    if (currentStep == TutorialStep.Putback)
                    {
                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("There it is. Finish the rebound.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Super:
                    // 4i. Ultimate move signal: mark that the ultimate move has been triggered

                    if (currentStep == TutorialStep.Super && !superTriggered)
                    {
                        superTriggered = true;
                        superTriggeredAt = activeTimer;
                        overlay.ShowFeedback("Skill live. Watch the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Score:
                    // 4j. Score signal: Complete the corresponding exercises according to the current exercise steps.

                    if (currentStep == TutorialStep.Shot && shotAttempted)
                    {
                        // 4j-i. Shooting practice score → Complete (divided into 2 points/3 points)

                        shotScored = true;
                        CompleteStep(core.MatchProcessor.ThrowType == 0 ? "Good rhythm. 3PT." : "Good rhythm. 2PT.");
                    }
                    else if (currentStep == TutorialStep.Dunk && dunkAttempted)
                    {
                        // 4j-ii. Dunk → Complete

                        dunkScored = true;
                        CompleteStep("Dunk made.");
                    }
                    else if (currentStep == TutorialStep.Putback && putbackAttempted)
                    {
                        // 4j-iii. Tip-up → Complete

                        putbackScored = true;
                        CompleteStep("Putback made.");
                    }
                    else if (currentStep == TutorialStep.FreePlay)
                    {
                        // 4j-iv. Free battle score → Mark preparation end

                        freePlayReadyToEnd = true;
                        freePlayScoredAt = activeTimer;
                        overlay.ShowFeedback("Basket. Nice finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), FreePlayScoreOutroDelay);
                    }
                    break;
            }
        }

        /// <summary>
        /// Handle button clicks on end screens (replay, training, menu).

        /// </summary>
        private void HandleOutroCommand()
        {
            // 1. Read the button instructions on the completion screen

            var command = overlay.ConsumeCommand();
            // 2. If no button is clicked, exit directly.

            if (command == mlpTutorialOverlayCommand.None)
            {
                return;
            }

            // 3. Set where to go next based on the button clicked
            inventory.PendingTutorialNextAction = command switch
            {
                mlpTutorialOverlayCommand.ReturnToMenu => mlpTutorialNextAction.None,
                mlpTutorialOverlayCommand.ReplayTutorial => mlpTutorialNextAction.ReplayTutorial,
                mlpTutorialOverlayCommand.StartTraining => mlpTutorialNextAction.StartTraining,
                mlpTutorialOverlayCommand.StartQuickMatch => mlpTutorialNextAction.StartQuickMatch,
                _ => mlpTutorialNextAction.None
            };

            // 4. Request to return to the main menu (the menu will jump to the corresponding page according to PendingTutorialNextAction)

            core.RequestReturnToMenu();
        }

        /// <summary>
        /// Refresh overlay visuals (score guides, etc.).

        /// </summary>
        private void UpdatePresentation()
        {
            if (overlay == null || player == null)
            {
                return;
            }

            ClearOverlayHighlights();
            if (currentStep == TutorialStep.Shot && phase != TutorialPhase.SuccessPause && phase != TutorialPhase.Outro)
            {
                overlay.SetScoringGuide(player.Side, true);
            }
        }

        /// <summary>
        /// Determine whether the basketball is in the air (shooting, dunking, blocking, etc.).

        /// </summary>
        private bool IsBallStillLive()
        {
            return core.Ball != null &&
                   (core.Ball.State == "shooting" ||
                    core.Ball.State == "basket" ||
                    core.Ball.State == "block" ||
                    core.Ball.State == "dunk" ||
                    core.Ball.State == "alleyOop");
        }

        /// <summary>
        /// Returns -1 (left), 1 (right), or 0 (close enough) for movement toward the target X position.

        /// </summary>
        private static int MoveTo(float currentX, float targetX)
        {
            var delta = targetX - currentX;
            if (Mathf.Abs(delta) <= 8f)
            {
                return 0;
            }

            return delta > 0f ? 1 : -1;
        }
    }
}
