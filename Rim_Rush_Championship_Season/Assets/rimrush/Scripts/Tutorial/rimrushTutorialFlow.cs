using UnityEngine;

namespace rimrush
{
    public sealed class rimrushTutorialFlow
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

        private const int TotalSteps = 10;
        private const float FullIntroDuration = 0.78f;
        private const float RetryIntroDuration = 0.45f;
        private const float SuccessPauseDuration = 1.18f;
        private const float StepSettleDuration = 0.68f;
        private const float HintFeedbackDuration = 1.28f;
        private const float RetryFeedbackDuration = 1.25f;
        private const float ActionFeedbackDuration = 1.08f;
        private const float FreePlayReminderDelay = 6.8f;
        private const float FreePlayScoreOutroDelay = 1.25f;
        private const float SuperCompletionDelay = 0.32f;
        private const float SuperFallbackCompletionDelay = 1.1f;
        private const float ShotRetryWindow = 4.4f;
        private const float ShotIdleRetryWindow = 6.8f;
        private const float PumpFinishWindow = 4.8f;
        private const float DunkRetryWindow = 3.7f;
        private const float DunkIdleRetryWindow = 6.2f;
        private const float StealCompletionMinDelay = 0.68f;
        private const float StealCompletionMaxDelay = 1.08f;
        private const float BlockDrillPlayerX = 360f;
        private const float BlockDrillOpponentX = 430f;
        private const float BlockDrillJumpDelay = 0.72f;
        private const float BlockDrillRetryWindow = 4.2f;
        private const float BlockDrillOpponentAirTimeScale = 0.5f;
        private const float BlockShotCueVelocityY = -135f;
        private const float BlockForcedShotDelay = 1.15f;
        private const float BlockObserveDuration = 1.25f;
        private const float BlockRetryDelay = 0.52f;
        private const float PutbackRetryWindow = 3.7f;
        private const float PutbackIdleRetryWindow = 6.2f;

        private readonly rimrushGameCore core;
        private readonly rimrushTutorialOverlay overlay;
        private readonly rimrushInventory inventory;

        private rimrushPlayerObject player;
        private rimrushPlayerObject opponent;
        private rimrushTutorialOpponentController opponentController;
        private TutorialPhase phase;
        private TutorialStep currentStep;
        private ShotStage shotStage;
        private PumpStage pumpStage;
        private float phaseTimer;
        private float activeTimer;
        private bool stepHintShown;
        private bool stepRetryHintShown;
        private float moveStartX;
        private bool movedLeft;
        private bool movedRight;
        private bool moveCompletionPending;
        private float moveCompletedAt;
        private bool dashCompletionPending;
        private float dashCompletedAt;
        private bool shotAttempted;
        private float shotAttemptStartedAt;
        private bool shotPeakValid;
        private bool shotScored;
        private bool pumpTriggered;
        private float pumpTriggeredAt;
        private bool pumpShotAttempted;
        private float pumpShotStartedAt;
        private bool pumpBitePending;
        private bool pumpJumpIssued;
        private bool dunkJumped;
        private bool dunkAttempted;
        private float dunkAttemptStartedAt;
        private bool dunkScored;
        private bool stealSuccessPending;
        private float stealSuccessAt;
        private bool blockJumpIssued;
        private bool blockShotIssued;
        private float blockShotIssuedAt;
        private bool blockJumpPromptShown;
        private bool blockReleasePromptShown;
        private bool blockSuccessPending;
        private float blockSuccessAt;
        private bool blockRetryPending;
        private float blockRetryAt;
        private bool putbackJumped;
        private bool putbackAttempted;
        private float putbackAttemptStartedAt;
        private bool putbackScored;
        private bool superTriggered;
        private float superTriggeredAt;
        private bool freePlayReadyToEnd;
        private float freePlayScoredAt;

        public rimrushTutorialFlow(rimrushGameCore core)
        {
            this.core = core;
            overlay = rimrushTutorialOverlay.Active;
            inventory = rimrushInventory.Instance;
        }

        public bool FreezeGameplay => phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause;

        public float GameplayTimeScale => 1f;

        public void Start()
        {
            player = core.PlayersLeft.Count > 0 ? core.PlayersLeft[0] : null;
            opponent = core.PlayersRight.Count > 0 ? core.PlayersRight[0] : null;
            opponentController = opponent?.Controller as rimrushTutorialOpponentController;
            if (player == null || opponent == null || overlay == null)
            {
                return;
            }

            opponentController?.SetMode(rimrushTutorialOpponentMode.Scripted);
            core.PlayerSignals.OnSignal += OnPlayerSignal;
            BeginOpening();
        }

        public void Shutdown()
        {
            core.PlayerSignals.OnSignal -= OnPlayerSignal;
            overlay?.Hide();
        }

        public void UpdateFrame(float dt)
        {
            if (overlay == null || player == null)
            {
                return;
            }

            if (phase == TutorialPhase.Outro)
            {
                UpdatePresentation();
                HandleOutroCommand();
                return;
            }

            UpdatePresentation();
            if (HandleStepCommand())
            {
                return;
            }

            if (phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
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
                        AdvanceAfterSuccess();
                    }
                }

                return;
            }

            if (phase != TutorialPhase.Active)
            {
                return;
            }

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

        public void UpdateAfterGameplay(float dt)
        {
            if (player == null || overlay == null || phase == TutorialPhase.Outro)
            {
                return;
            }

            UpdatePresentation();
        }

        public void PopulateOpponentInputs(rimrushPlayerObject opponentPlayer, rimrushTutorialOpponentController controller, float dt)
        {
            if (controller == null || opponentPlayer == null || phase != TutorialPhase.Active)
            {
                return;
            }

            switch (currentStep)
            {
                case TutorialStep.Pump:
                {
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
                    controller.SetFrameInputs(MoveTo(opponentPlayer.Position.x, 332f), false, false, false, false, 0);
                    break;

                case TutorialStep.Block:
                {
                    var move = 0;
                    var jump = false;
                    var action = false;
                    if (!blockJumpIssued)
                    {
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

        private void BeginOpening()
        {
            currentStep = TutorialStep.Opening;
            phase = TutorialPhase.IntroFreeze;
            phaseTimer = 1.8f;
            activeTimer = 0f;
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

        private void BeginMove(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Move;
            moveStartX = 360f;
            movedLeft = false;
            movedRight = false;
            moveCompletionPending = false;
            moveCompletedAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(moveStartX, rimrushObjectsData.PlayerIndentY),
                new Vector2(652f, rimrushObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
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

        private void BeginDash(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Dash;
            dashCompletionPending = false;
            dashCompletedAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(286f, rimrushObjectsData.PlayerIndentY),
                new Vector2(652f, rimrushObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
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

        private void BeginShot(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Shot;
            shotStage = ShotStage.Jump;
            shotAttempted = false;
            shotAttemptStartedAt = 0f;
            shotPeakValid = false;
            shotScored = false;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(552f, rimrushObjectsData.PlayerIndentY),
                new Vector2(654f, rimrushObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            player.TutorialPrimePerfectShot();
            overlay.ShowStep(
                2,
                TotalSteps,
                "JUMP SHOT",
                string.Empty,
                string.Empty,
                "Jump first. Press B at the top.",
                "W",
                "B");
        }

        private void BeginPump(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Pump;
            pumpStage = PumpStage.Fake;
            pumpTriggered = false;
            pumpTriggeredAt = 0f;
            pumpShotAttempted = false;
            pumpShotStartedAt = 0f;
            pumpBitePending = false;
            pumpJumpIssued = false;
            PrepareScriptedStep();
            core.Ball.TutorialClearGuaranteedScore();
            core.TutorialResetScenario(
                new Vector2(276f, rimrushObjectsData.PlayerIndentY),
                new Vector2(426f, rimrushObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
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

        private void BeginDunk(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Dunk;
            dunkJumped = false;
            dunkAttempted = false;
            dunkAttemptStartedAt = 0f;
            dunkScored = false;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(642f, rimrushObjectsData.PlayerIndentY),
                new Vector2(516f, rimrushObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            player.TutorialPrimePerfectDunk();
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

        private void BeginSteal(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Steal;
            stealSuccessPending = false;
            stealSuccessAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(286f, rimrushObjectsData.PlayerIndentY),
                new Vector2(518f, rimrushObjectsData.PlayerIndentY),
                false,
                true,
                1f,
                -1f);
            overlay.ShowStep(
                5,
                TotalSteps,
                "STEAL",
                string.Empty,
                string.Empty,
                "Get close, then press B.",
                "B");
        }

        private void BeginBlock(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Block;
            blockJumpIssued = false;
            blockShotIssued = false;
            blockShotIssuedAt = 0f;
            blockJumpPromptShown = false;
            blockReleasePromptShown = false;
            blockSuccessPending = false;
            blockSuccessAt = 0f;
            blockRetryPending = false;
            blockRetryAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(BlockDrillPlayerX, rimrushObjectsData.PlayerIndentY),
                new Vector2(BlockDrillOpponentX, rimrushObjectsData.PlayerIndentY),
                false,
                true,
                -1f,
                -1f);
            opponent.TutorialSetAirMotionTimeScale(BlockDrillOpponentAirTimeScale);
            player.TutorialSetJumpBlockAssist(true);
            overlay.ShowStep(
                6,
                TotalSteps,
                "BLOCK",
                string.Empty,
                string.Empty,
                "Read the slow shot. Press W late.",
                "W");
        }

        private void BeginPutback(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Putback;
            putbackJumped = false;
            putbackAttempted = false;
            putbackAttemptStartedAt = 0f;
            putbackScored = false;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(666f, rimrushObjectsData.PlayerIndentY),
                new Vector2(516f, rimrushObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            core.Ball.TutorialLaunchPutbackBounce(player.Side, 7.5f);
            player.TutorialPrimePutbackDunk();
            overlay.ShowStep(
                7,
                TotalSteps,
                "PUTBACK",
                string.Empty,
                string.Empty,
                "Read the rim bounce. Press B near it.",
                "W",
                "B");
        }

        private void BeginSuper(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Super;
            superTriggered = false;
            superTriggeredAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(242f, rimrushObjectsData.PlayerIndentY),
                new Vector2(654f, rimrushObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            player.TutorialChargeSuper();
            overlay.ShowStep(
                8,
                TotalSteps,
                "SUPER",
                string.Empty,
                string.Empty,
                "Energy is full. Press N.",
                "N");
        }

        private void BeginFreePlay(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.FreePlay;
            freePlayReadyToEnd = false;
            freePlayScoredAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(238f, rimrushObjectsData.PlayerIndentY),
                new Vector2(606f, rimrushObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            opponentController?.SetMode(rimrushTutorialOpponentMode.FreePlay);
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
            ClearOverlayHighlights();
        }

        private void ResetStep(bool fullIntro)
        {
            phase = TutorialPhase.IntroFreeze;
            phaseTimer = fullIntro ? FullIntroDuration : RetryIntroDuration;
            activeTimer = 0f;
            stepHintShown = false;
            stepRetryHintShown = false;
            superTriggered = false;
            superTriggeredAt = 0f;
            player?.TutorialSetJumpBlockAssist(false);
        }

        private void PrepareScriptedStep()
        {
            opponentController?.SetMode(rimrushTutorialOpponentMode.Scripted);
            ClearOverlayHighlights();
        }

        private void ClearOverlayHighlights()
        {
            overlay.ClearFocus();
            overlay.SetApexRing(Vector2.zero, 0f, false);
            overlay.SetEnergyPulse(false);
            overlay.SetTargetRect(0f, 0f, 0f, 0f);
            overlay.SetTrajectory(null);
        }

        private void UpdateMoveStep()
        {
            if (player.Position.x <= moveStartX - 42f)
            {
                movedLeft = true;
            }

            if (player.Position.x >= moveStartX + 42f)
            {
                movedRight = true;
            }

            if (!moveCompletionPending && movedLeft && movedRight && activeTimer >= 2.2f)
            {
                moveCompletionPending = true;
                moveCompletedAt = activeTimer;
                overlay.ShowFeedback("Good. Keep that court feel.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                return;
            }

            if (moveCompletionPending)
            {
                if (activeTimer - moveCompletedAt >= StepSettleDuration)
                {
                    CompleteStep("Left and right are yours.");
                }

                return;
            }

            if (activeTimer >= 3.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Try both A and D.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (activeTimer >= 7.4f && !stepRetryHintShown)
            {
                stepRetryHintShown = true;
                overlay.ShowFeedback("Move left once, then right once.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateDashStep()
        {
            if (dashCompletionPending)
            {
                if (activeTimer - dashCompletedAt >= StepSettleDuration)
                {
                    CompleteStep("Dash gives you the burst.");
                }

                return;
            }

            if (activeTimer >= 3.2f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Tap A twice, or D twice.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (activeTimer >= 6.6f)
            {
                BeginDash(false);
                overlay.ShowFeedback("Two quick taps. Same direction.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateShotStep()
        {
            if (shotScored)
            {
                return;
            }

            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(shotStage == ShotStage.Jump ? "W first. B at the top." : "Highest point has best accuracy.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (shotAttempted && activeTimer - shotAttemptStartedAt >= ShotRetryWindow)
            {
                BeginShot(false);
                overlay.ShowFeedback("Good try. Jump and release once more.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            if (!shotAttempted && activeTimer >= ShotIdleRetryWindow)
            {
                BeginShot(false);
                overlay.ShowFeedback("Jump, then B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdatePumpStep()
        {
            if (!pumpTriggered)
            {
                if (activeTimer >= 2.4f && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Hold S with the ball.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }

                if (activeTimer >= 5.2f)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Sell the fake first.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                }

                return;
            }

            if (pumpShotAttempted)
            {
                if (activeTimer - pumpShotStartedAt >= ShotRetryWindow)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Good try. Fake, release, then finish the shot.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                }

                return;
            }

            if (activeTimer - pumpTriggeredAt >= 2.2f && !stepRetryHintShown)
            {
                stepRetryHintShown = true;
                overlay.ShowFeedback("Let go, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (activeTimer - pumpTriggeredAt >= PumpFinishWindow)
            {
                BeginPump(false);
                overlay.ShowFeedback("Fake, then punish.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateDunkStep()
        {
            if (dunkScored)
            {
                return;
            }

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

            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("W, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (dunkAttempted && activeTimer - dunkAttemptStartedAt >= DunkRetryWindow)
            {
                BeginDunk(false);
                overlay.ShowFeedback("Good try. Press B higher in the paint.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            if (!dunkAttempted && activeTimer >= DunkIdleRetryWindow)
            {
                BeginDunk(false);
                overlay.ShowFeedback("Jump near rim, then B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateStealStep()
        {
            if (stealSuccessPending)
            {
                var elapsed = activeTimer - stealSuccessAt;
                if (elapsed >= StealCompletionMinDelay && (player.WithBall || elapsed >= StealCompletionMaxDelay))
                {
                    CompleteStep("Nice. Defense can start the offense.");
                }

                return;
            }

            if (opponent != null && opponent.Position.x <= 292f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Get closer before you swipe.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Get close.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (activeTimer >= 4.6f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Closer, then B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateBlockStep()
        {
            if (blockSuccessPending)
            {
                if (activeTimer - blockSuccessAt >= BlockObserveDuration)
                {
                    CompleteStep("Good block. You changed the shot.");
                }

                return;
            }

            if (blockRetryPending)
            {
                if (activeTimer >= blockRetryAt)
                {
                    BeginBlock(false);
                }

                return;
            }

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

            if (activeTimer >= 2.8f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Wait for the release, then W.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (blockShotIssued && activeTimer - blockShotIssuedAt >= BlockDrillRetryWindow)
            {
                ScheduleBlockRetry("No block yet. Jump on the W cue.");
            }
        }

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

        private void UpdatePutbackStep()
        {
            if (putbackScored)
            {
                return;
            }

            if (player != null && !player.IsGrounded && !putbackJumped)
            {
                putbackJumped = true;
                overlay.UpdateCopy(
                    "PRESS B",
                    "Near the ball.",
                    "FINISH PUTBACK",
                    "Press B by rebound.",
                    "B");
                overlay.ShowFeedback("Press B now.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
            }

            if (activeTimer >= 2.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Jump, then B by ball.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            if (putbackAttempted && activeTimer - putbackAttemptStartedAt >= PutbackRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Good try. Meet the ball earlier.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            if (!putbackAttempted && activeTimer >= PutbackIdleRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("W, then B near ball.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateSuperStep()
        {
            if (superTriggered)
            {
                var elapsed = activeTimer - superTriggeredAt;
                if ((!player.IsSuperShot && elapsed >= SuperCompletionDelay) ||
                    elapsed >= SuperFallbackCompletionDelay)
                {
                    CompleteStep("Super landed.");
                }

                return;
            }

            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Press N.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), HintFeedbackDuration);
            }

            if (activeTimer >= 4.4f)
            {
                BeginSuper(false);
                overlay.ShowFeedback("Do not leave the super hidden.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        private void UpdateFreePlayStep()
        {
            if (!freePlayReadyToEnd)
            {
                if (activeTimer >= FreePlayReminderDelay && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Score one basket to finish.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }

                return;
            }

            if (activeTimer - freePlayScoredAt < FreePlayScoreOutroDelay)
            {
                return;
            }

            BeginOutro();
        }

        private void CompleteStep(string message)
        {
            phase = TutorialPhase.SuccessPause;
            phaseTimer = SuccessPauseDuration;
            overlay.ShowFeedback(message, new Color32(0x9A, 0xFF, 0xDD, 0xFF), SuccessPauseDuration);
            ClearOverlayHighlights();
        }

        private void BeginOutro()
        {
            phase = TutorialPhase.Outro;
            overlay.ShowOutro(rimrushPlayersData.GetCharacterName(player.CharacterId), rimrushCharacterSkillsData.Get(player.CharacterId).SkillName);
        }

        private void AdvanceAfterSuccess()
        {
            switch (currentStep)
            {
                case TutorialStep.Move:
                    BeginDash(true);
                    break;
                case TutorialStep.Dash:
                    BeginShot(true);
                    break;
                case TutorialStep.Shot:
                    BeginPump(true);
                    break;
                case TutorialStep.Pump:
                    BeginDunk(true);
                    break;
                case TutorialStep.Dunk:
                    BeginSteal(true);
                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);
                    break;
                case TutorialStep.Block:
                    BeginPutback(true);
                    break;
                case TutorialStep.Putback:
                    BeginSuper(true);
                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);
                    break;
                default:
                    BeginOutro();
                    break;
            }
        }

        private bool HandleStepCommand()
        {
            var command = overlay.ConsumeCommand();
            if (command == rimrushTutorialOverlayCommand.None)
            {
                return false;
            }

            if (command == rimrushTutorialOverlayCommand.SkipStep)
            {
                SkipCurrentStep();
                return true;
            }

            return false;
        }

        private void SkipCurrentStep()
        {
            switch (currentStep)
            {
                case TutorialStep.Opening:
                    BeginMove(true);
                    break;
                case TutorialStep.Move:
                    BeginDash(true);
                    break;
                case TutorialStep.Dash:
                    BeginShot(true);
                    break;
                case TutorialStep.Shot:
                    BeginPump(true);
                    break;
                case TutorialStep.Pump:
                    BeginDunk(true);
                    break;
                case TutorialStep.Dunk:
                    BeginSteal(true);
                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);
                    break;
                case TutorialStep.Block:
                    BeginPutback(true);
                    break;
                case TutorialStep.Putback:
                    BeginSuper(true);
                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);
                    break;
                default:
                    BeginOutro();
                    break;
            }
        }

        private void OnPlayerSignal(rimrushPlayerSignalType signal, int side, int playerNo)
        {
            if (player == null || phase != TutorialPhase.Active)
            {
                return;
            }

            if (currentStep == TutorialStep.Block &&
                signal == rimrushPlayerSignalType.Score &&
                opponent != null &&
                side == opponent.Side &&
                playerNo == opponent.PlayerNo)
            {
                ScheduleBlockRetry("Try again. Jump on the release cue.");
                return;
            }

            if (side != player.Side || playerNo != player.PlayerNo)
            {
                return;
            }

            switch (signal)
            {
                case rimrushPlayerSignalType.Dash:
                    if (currentStep == TutorialStep.Dash && !dashCompletionPending)
                    {
                        dashCompletionPending = true;
                        dashCompletedAt = activeTimer;
                        overlay.ShowFeedback("Good dash. Feel the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.JumpA:
                    if (currentStep == TutorialStep.Shot)
                    {
                        shotStage = ShotStage.PeakShot;
                        overlay.UpdateCopy(
                            "TOP RELEASE",
                            "Best accuracy.",
                            "SHOOT AT PEAK",
                            "Press B near highest point.",
                            "B");
                        overlay.ShowFeedback("Highest point has best accuracy.", new Color32(0xFF, 0xD1, 0x76, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.Shoot:
                    if (currentStep == TutorialStep.Shot)
                    {
                        shotAttemptStartedAt = activeTimer;
                        shotPeakValid = shotStage == ShotStage.PeakShot && !player.IsGrounded;
                        shotAttempted = shotPeakValid;
                        overlay.ShowFeedback(
                            shotPeakValid
                                ? Mathf.Abs(player.Velocity.y) <= 140f ? "Peak release. Best accuracy." : "Good release. Top is best."
                                : "Jump first, then B.",
                            shotPeakValid ? new Color32(0x9A, 0xFF, 0xDD, 0xFF) : new Color32(0xFF, 0xD1, 0x76, 0xFF),
                            ActionFeedbackDuration);
                    }
                    else if (currentStep == TutorialStep.Pump && pumpTriggered)
                    {
                        pumpShotAttempted = true;
                        pumpShotStartedAt = activeTimer;
                        overlay.ShowFeedback(
                            player.IsGrounded ? "Good release." : "That counts.",
                            new Color32(0x9A, 0xFF, 0xDD, 0xFF),
                            ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.Pump:
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

                case rimrushPlayerSignalType.Dunk:
                    if (currentStep == TutorialStep.Dunk)
                    {
                        dunkAttempted = true;
                        dunkAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Strong take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.StealSuccess:
                    if (currentStep == TutorialStep.Steal)
                    {
                        stealSuccessPending = true;
                        stealSuccessAt = activeTimer;
                        overlay.ShowFeedback("Nice steal.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.Block:
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

                case rimrushPlayerSignalType.PutbackDunk:
                    if (currentStep == TutorialStep.Putback)
                    {
                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("There it is. Finish the rebound.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.Super:
                    if (currentStep == TutorialStep.Super && !superTriggered)
                    {
                        superTriggered = true;
                        superTriggeredAt = activeTimer;
                        overlay.ShowFeedback("Skill live. Watch the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case rimrushPlayerSignalType.Score:
                    if (currentStep == TutorialStep.Shot && shotAttempted)
                    {
                        shotScored = true;
                        CompleteStep(shotPeakValid ? "Good rhythm." : "Shot made.");
                    }
                    else if (currentStep == TutorialStep.Pump && pumpShotAttempted)
                    {
                        CompleteStep("Fake worked.");
                    }
                    else if (currentStep == TutorialStep.Dunk && dunkAttempted)
                    {
                        dunkScored = true;
                        CompleteStep("Dunk made.");
                    }
                    else if (currentStep == TutorialStep.Putback && putbackAttempted)
                    {
                        putbackScored = true;
                        CompleteStep("Putback made.");
                    }
                    else if (currentStep == TutorialStep.FreePlay)
                    {
                        freePlayReadyToEnd = true;
                        freePlayScoredAt = activeTimer;
                        overlay.ShowFeedback("Basket. Nice finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), FreePlayScoreOutroDelay);
                    }
                    break;
            }
        }

        private void HandleOutroCommand()
        {
            var command = overlay.ConsumeCommand();
            if (command == rimrushTutorialOverlayCommand.None)
            {
                return;
            }

            inventory.PendingTutorialNextAction = command switch
            {
                rimrushTutorialOverlayCommand.ReturnToMenu => rimrushTutorialNextAction.None,
                rimrushTutorialOverlayCommand.ReplayTutorial => rimrushTutorialNextAction.ReplayTutorial,
                rimrushTutorialOverlayCommand.StartTraining => rimrushTutorialNextAction.StartTraining,
                rimrushTutorialOverlayCommand.StartQuickMatch => rimrushTutorialNextAction.StartQuickMatch,
                _ => rimrushTutorialNextAction.None
            };

            core.RequestReturnToMenu();
        }

        private void UpdatePresentation()
        {
            if (overlay == null || player == null)
            {
                return;
            }

            ClearOverlayHighlights();
        }

        private bool IsBallStillLive()
        {
            return core.Ball != null &&
                   (core.Ball.State == "shooting" ||
                    core.Ball.State == "basket" ||
                    core.Ball.State == "block" ||
                    core.Ball.State == "dunk" ||
                    core.Ball.State == "alleyOop");
        }

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
