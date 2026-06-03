using System.Collections.Generic;
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
            MoveDash,
            Shot,
            Pump,
            Steal,
            Block,
            Super,
            FreePlay
        }

        private enum MoveDashStage
        {
            Move,
            Dash
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

        private const int TotalSteps = 7;
        private const float FullIntroDuration = 0.95f;
        private const float RetryIntroDuration = 0.72f;
        private const float SuccessPauseDuration = 0.84f;
        private const float FreePlayDuration = 8.4f;

        private readonly rimrushGameCore core;
        private readonly rimrushTutorialOverlay overlay;
        private readonly rimrushInventory inventory;

        private rimrushPlayerObject player;
        private rimrushPlayerObject opponent;
        private rimrushTutorialOpponentController opponentController;
        private TutorialPhase phase;
        private TutorialStep currentStep;
        private MoveDashStage moveDashStage;
        private ShotStage shotStage;
        private PumpStage pumpStage;
        private float phaseTimer;
        private float activeTimer;
        private float slowMotionTimer;
        private float slowMotionScale = 1f;
        private bool stepHintShown;
        private bool stepRetryHintShown;
        private bool shotAttempted;
        private float shotAttemptStartedAt;
        private bool shotPeakValid;
        private bool shotScored;
        private bool pumpTriggered;
        private bool pumpBitePending;
        private bool pumpJumpIssued;
        private bool blockJumpIssued;
        private bool blockShotIssued;
        private bool blockAssistApplied;
        private bool blockSlowMotionConsumed;
        private bool freePlayReadyToEnd;

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

            if (slowMotionTimer > 0f)
            {
                slowMotionTimer = Mathf.Max(0f, slowMotionTimer - dt);
            }

            if (phase == TutorialPhase.Outro)
            {
                UpdatePresentation();
                HandleOutroCommand();
                return;
            }

            UpdatePresentation();
            if (phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    if (phase == TutorialPhase.IntroFreeze)
                    {
                        if (currentStep == TutorialStep.Opening)
                        {
                            BeginMoveDash(true);
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
                case TutorialStep.MoveDash:
                    UpdateMoveDashStep();
                    break;
                case TutorialStep.Shot:
                    UpdateShotStep();
                    break;
                case TutorialStep.Pump:
                    UpdatePumpStep();
                    break;
                case TutorialStep.Steal:
                    UpdateStealStep();
                    break;
                case TutorialStep.Block:
                    UpdateBlockStep();
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
                        if (Mathf.Abs(opponentPlayer.Position.x - 612f) > 10f)
                        {
                            move = MoveTo(opponentPlayer.Position.x, 612f);
                        }
                        else if (activeTimer >= 0.52f)
                        {
                            jump = true;
                            blockJumpIssued = true;
                        }
                    }
                    else if (!opponentPlayer.IsGrounded && !blockShotIssued && opponentPlayer.CanThrow && opponentPlayer.Velocity.y >= -28f)
                    {
                        action = true;
                        blockShotIssued = true;
                        blockSlowMotionConsumed = true;
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
            slowMotionTimer = 0f;
            overlay.ShowPrelude(
                "LEARN THE FULL GAME",
                "Seven safe drills, then a live round.",
                "Follow the checklist once. Each action shows why the game is fun.",
                "A / D",
                "W",
                "S",
                "B",
                "N");
        }

        private void BeginMoveDash(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.MoveDash;
            moveDashStage = MoveDashStage.Move;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(118f, rimrushObjectsData.PlayerIndentY),
                new Vector2(652f, rimrushObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            overlay.ShowStep(
                0,
                TotalSteps,
                "A / D MOVE",
                "Hold D to create space.",
                "MOVE RIGHT",
                "Footwork sets up every shot, steal, and block.",
                "A / D");
            overlay.SetFocusRect(84f, 228f, 256f, 152f);
            overlay.SetTargetRect(222f, 350f, 76f, 22f);
        }

        private void BeginDashPrompt()
        {
            phase = TutorialPhase.IntroFreeze;
            phaseTimer = 0.82f;
            activeTimer = 0f;
            moveDashStage = MoveDashStage.Dash;
            overlay.UpdateCopy(
                "D, D DASH",
                "Burst through the lane.",
                "CROSS THE LANE FAST",
                "Dash is the game's pace change. Tap twice quickly.",
                "D",
                "D");
            overlay.SetFocusRect(152f, 224f, 332f, 160f);
            overlay.SetTargetRect(336f, 344f, 112f, 30f);
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
                1,
                TotalSteps,
                "W THEN B",
                "Jump, then release in the air.",
                "MAKE ONE JUMP SHOT",
                "The top is best, but any air release teaches the rhythm.",
                "W",
                "B");
            overlay.SetFocusRect(486f, 92f, 262f, 282f);
            overlay.SetTargetRect(696f, 222f, 58f, 34f);
        }

        private void BeginPump(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Pump;
            pumpStage = PumpStage.Fake;
            pumpTriggered = false;
            pumpBitePending = false;
            pumpJumpIssued = false;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(276f, rimrushObjectsData.PlayerIndentY),
                new Vector2(426f, rimrushObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            overlay.ShowStep(
                2,
                TotalSteps,
                "HOLD S FAKE",
                "Make the defender jump.",
                "BAIT, THEN SHOOT",
                "Hold S to sell it. Let go and press B.",
                "S",
                "B");
            overlay.SetFocusRect(216f, 208f, 262f, 170f);
        }

        private void BeginSteal(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Steal;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(286f, rimrushObjectsData.PlayerIndentY),
                new Vector2(518f, rimrushObjectsData.PlayerIndentY),
                false,
                true,
                1f,
                -1f);
            overlay.ShowStep(
                3,
                TotalSteps,
                "GET CLOSE + B",
                "Take the ball back.",
                "STEAL ONE DRIBBLE",
                "Defense starts by closing the space.",
                "B");
            overlay.SetFocusRect(236f, 216f, 334f, 160f);
        }

        private void BeginBlock(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Block;
            blockJumpIssued = false;
            blockShotIssued = false;
            blockAssistApplied = false;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(206f, rimrushObjectsData.PlayerIndentY),
                new Vector2(612f, rimrushObjectsData.PlayerIndentY),
                false,
                true,
                -1f,
                -1f);
            overlay.ShowStep(
                4,
                TotalSteps,
                "W TO CONTEST",
                "Jump before the shot leaves.",
                "BLOCK OR ALTER ONE SHOT",
                "Watch the shooter rise, then meet the release early.",
                "W");
            overlay.SetFocusRect(166f, 94f, 300f, 270f);
            UpdateBlockTrajectory();
        }

        private void BeginSuper(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Super;
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
                5,
                TotalSteps,
                "N SUPER",
                "Trigger your signature move.",
                "USE THE CHARACTER SKILL",
                "Supers are the character identity moment.",
                "N");
            overlay.SetFocusRect(10f, 12f, 184f, 88f);
            overlay.SetEnergyPulse(true);
        }

        private void BeginFreePlay(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.FreePlay;
            freePlayReadyToEnd = false;
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
                6,
                TotalSteps,
                "SHORT LIVE ROUND",
                "Use the whole toolkit.",
                "PLAY ONE POSSESSION",
                "Move, dash, shoot, fake, steal, block, or super. Make it yours.",
                "A / D",
                "W",
                "S",
                "B",
                "N");
            overlay.ClearFocus();
            overlay.SetTargetRect(0f, 0f, 0f, 0f);
            overlay.SetApexRing(Vector2.zero, 0f, false);
            overlay.SetEnergyPulse(false);
            overlay.SetTrajectory(null);
        }

        private void ResetStep(bool fullIntro)
        {
            phase = TutorialPhase.IntroFreeze;
            phaseTimer = fullIntro ? FullIntroDuration : RetryIntroDuration;
            activeTimer = 0f;
            slowMotionTimer = 0f;
            slowMotionScale = 1f;
            stepHintShown = false;
            stepRetryHintShown = false;
        }

        private void PrepareScriptedStep()
        {
            opponentController?.SetMode(rimrushTutorialOpponentMode.Scripted);
            overlay.SetApexRing(Vector2.zero, 0f, false);
            overlay.SetEnergyPulse(false);
            overlay.SetTargetRect(0f, 0f, 0f, 0f);
            overlay.SetTrajectory(null);
        }

        private void UpdateMoveDashStep()
        {
            if (moveDashStage == MoveDashStage.Move)
            {
                if (player.Position.x >= 222f)
                {
                    BeginDashPrompt();
                    return;
                }

                if (activeTimer >= 2.2f && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Hold D until the drill advances.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
                }

                if (activeTimer >= 5.2f)
                {
                    BeginMoveDash(false);
                    overlay.ShowFeedback("Start with a simple move to the right.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
                }

                return;
            }

            if (player.IsDashing && player.Position.x >= 340f)
            {
                CompleteStep("Good. The game already feels faster.");
                return;
            }

            if (activeTimer >= 2.1f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Double-tap D.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
            }

            if (activeTimer >= 4.8f)
            {
                BeginMoveDash(false);
                overlay.ShowFeedback("Two taps. Not one hold.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
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
                overlay.ShowFeedback(shotStage == ShotStage.Jump ? "Press W first, then B while airborne." : "Press B before landing.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
            }

            if (shotAttempted && activeTimer - shotAttemptStartedAt >= 3.4f)
            {
                BeginShot(false);
                overlay.ShowFeedback("Good try. Jump and release once more.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
                return;
            }

            if (!shotAttempted && activeTimer >= 7f)
            {
                BeginShot(false);
                overlay.ShowFeedback("Jump first, then shoot before landing.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
            }
        }

        private void UpdatePumpStep()
        {
            if (!pumpTriggered)
            {
                if (activeTimer >= 2.4f && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Hold S with the ball.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
                }

                if (activeTimer >= 5.2f)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Sell the fake first.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
                }

                return;
            }

            if (activeTimer >= 2.6f && !stepRetryHintShown)
            {
                stepRetryHintShown = true;
                overlay.ShowFeedback("Let go of S, then press B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
            }

            if (activeTimer >= 5.6f)
            {
                BeginPump(false);
                overlay.ShowFeedback("Fake, then punish.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
            }
        }

        private void UpdateStealStep()
        {
            if (opponent != null && opponent.Position.x <= 292f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Get closer before you swipe.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
                return;
            }

            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Step into the dribble.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
            }

            if (activeTimer >= 5.4f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Move closer first, then press B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
            }
        }

        private void UpdateBlockStep()
        {
            UpdateBlockTrajectory();
            if (TryResolveTutorialBlockAssist())
            {
                return;
            }

            if (activeTimer >= 2.8f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Jump into the arc.", new Color32(0xFF, 0xD1, 0x76, 0xFF), 0.95f);
            }

            if (blockShotIssued && activeTimer >= 6.4f)
            {
                BeginBlock(false);
                overlay.ShowFeedback("Jump as the shooter rises, not after the ball is gone.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
            }
        }

        private void UpdateSuperStep()
        {
            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Energy is full. Press N.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), 0.95f);
            }

            if (activeTimer >= 5.4f)
            {
                BeginSuper(false);
                overlay.ShowFeedback("Do not leave the super hidden.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
            }
        }

        private void UpdateFreePlayStep()
        {
            if (!freePlayReadyToEnd && activeTimer >= 2.6f)
            {
                freePlayReadyToEnd = true;
            }

            if (activeTimer < FreePlayDuration)
            {
                return;
            }

            if (core.Ball != null && !freePlayReadyToEnd && IsBallStillLive())
            {
                return;
            }

            phase = TutorialPhase.Outro;
            overlay.ShowOutro(rimrushPlayersData.GetCharacterName(player.CharacterId), rimrushCharacterSkillsData.Get(player.CharacterId).SkillName);
        }

        private void CompleteStep(string message)
        {
            phase = TutorialPhase.SuccessPause;
            phaseTimer = SuccessPauseDuration;
            overlay.ShowFeedback(message, new Color32(0x9A, 0xFF, 0xDD, 0xFF), SuccessPauseDuration);
            overlay.SetApexRing(Vector2.zero, 0f, false);
            overlay.SetEnergyPulse(false);
            overlay.SetTrajectory(null);
        }

        private void AdvanceAfterSuccess()
        {
            switch (currentStep)
            {
                case TutorialStep.MoveDash:
                    BeginShot(true);
                    break;
                case TutorialStep.Shot:
                    BeginPump(true);
                    break;
                case TutorialStep.Pump:
                    BeginSteal(true);
                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);
                    break;
                case TutorialStep.Block:
                    BeginSuper(true);
                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);
                    break;
                default:
                    phase = TutorialPhase.Outro;
                    overlay.ShowOutro(rimrushPlayersData.GetCharacterName(player.CharacterId), rimrushCharacterSkillsData.Get(player.CharacterId).SkillName);
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
                BeginBlock(false);
                overlay.ShowFeedback("Too late. Jump sooner.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), 1f);
                return;
            }

            if (side != player.Side || playerNo != player.PlayerNo)
            {
                return;
            }

            switch (signal)
            {
                case rimrushPlayerSignalType.JumpA:
                    if (currentStep == TutorialStep.Shot)
                    {
                        shotStage = ShotStage.PeakShot;
                        slowMotionTimer = 0.66f;
                        slowMotionScale = 0.62f;
                        overlay.UpdateCopy(
                            "PEAK + B",
                            "Release inside the ring.",
                            "MAKE THE SHOT",
                            "Top of the jump. Then B.",
                            "B");
                    }
                    break;

                case rimrushPlayerSignalType.Shoot:
                    if (currentStep == TutorialStep.Shot)
                    {
                        shotAttempted = true;
                        shotPeakValid = !player.IsGrounded && Mathf.Abs(player.Velocity.y) <= 62f;
                        overlay.ShowFeedback(
                            shotPeakValid ? "That release looked clean." : "A little higher.",
                            shotPeakValid ? new Color32(0x9A, 0xFF, 0xDD, 0xFF) : new Color32(0xFF, 0xD1, 0x76, 0xFF),
                            0.9f);
                    }
                    else if (currentStep == TutorialStep.Pump && pumpTriggered && opponent != null && !opponent.IsGrounded)
                    {
                        CompleteStep("Perfect. The fake created the window.");
                    }
                    break;

                case rimrushPlayerSignalType.Pump:
                    if (currentStep == TutorialStep.Pump && pumpStage == PumpStage.Fake)
                    {
                        pumpTriggered = true;
                        pumpStage = PumpStage.Finish;
                        pumpBitePending = true;
                        slowMotionTimer = 0.5f;
                        slowMotionScale = 0.58f;
                        overlay.UpdateCopy(
                            "LET GO + B",
                            "Punish the jump.",
                            "TAKE THE OPEN LOOK",
                            "Release S. Then B.",
                            "S",
                            "B");
                        overlay.ShowFeedback("He bit.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), 0.9f);
                    }
                    break;

                case rimrushPlayerSignalType.StealSuccess:
                    if (currentStep == TutorialStep.Steal)
                    {
                        CompleteStep("Nice. Defense can start the offense.");
                    }
                    break;

                case rimrushPlayerSignalType.Block:
                    if (currentStep == TutorialStep.Block)
                    {
                        CompleteStep("Exactly. Now they can see the defense.");
                    }
                    break;

                case rimrushPlayerSignalType.Super:
                    if (currentStep == TutorialStep.Super)
                    {
                        CompleteStep("That is the character identity moment.");
                    }
                    break;

                case rimrushPlayerSignalType.Score:
                    if (currentStep == TutorialStep.Shot && shotPeakValid)
                    {
                        shotScored = true;
                        CompleteStep("Beautiful. That is the rhythm to remember.");
                    }
                    else if (currentStep == TutorialStep.FreePlay && activeTimer >= 2f)
                    {
                        freePlayReadyToEnd = true;
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

            switch (currentStep)
            {
                case TutorialStep.MoveDash:
                    overlay.SetApexRing(Vector2.zero, 0f, false);
                    overlay.SetEnergyPulse(false);
                    overlay.SetTrajectory(null);
                    break;

                case TutorialStep.Shot:
                    overlay.SetEnergyPulse(false);
                    overlay.SetTrajectory(null);
                    break;

                case TutorialStep.Pump:
                {
                    overlay.SetApexRing(Vector2.zero, 0f, false);
                    overlay.SetEnergyPulse(false);
                    overlay.SetTrajectory(null);
                    var left = Mathf.Min(player.Position.x, opponent != null ? opponent.Position.x : player.Position.x) - 68f;
                    var right = Mathf.Max(player.Position.x, opponent != null ? opponent.Position.x : player.Position.x) + 68f;
                    overlay.SetFocusRect(left, 204f, right - left, 176f);
                    break;
                }

                case TutorialStep.Steal:
                    if (opponent != null)
                    {
                        var left = Mathf.Min(player.Position.x, opponent.Position.x) - 72f;
                        var right = Mathf.Max(player.Position.x, opponent.Position.x) + 72f;
                        overlay.SetFocusRect(left, 210f, right - left, 170f);
                    }

                    overlay.SetApexRing(Vector2.zero, 0f, false);
                    overlay.SetEnergyPulse(false);
                    overlay.SetTrajectory(null);
                    break;

                case TutorialStep.Block:
                    overlay.SetApexRing(Vector2.zero, 0f, false);
                    overlay.SetEnergyPulse(false);
                    break;

                case TutorialStep.Super:
                    overlay.SetApexRing(Vector2.zero, 0f, false);
                    overlay.SetTrajectory(null);
                    overlay.SetEnergyPulse(true);
                    if (phase == TutorialPhase.Active)
                    {
                        overlay.SetFocusRect(player.Position.x - 90f, 176f, 210f, 170f);
                    }
                    break;

                case TutorialStep.FreePlay:
                    overlay.ClearFocus();
                    overlay.SetApexRing(Vector2.zero, 0f, false);
                    overlay.SetEnergyPulse(false);
                    overlay.SetTrajectory(null);
                    break;
            }
        }

        private void UpdateBlockTrajectory()
        {
            if (overlay == null || player == null || opponent == null || currentStep != TutorialStep.Block)
            {
                return;
            }

            var start = new Vector2(opponent.Position.x - 12f, opponent.Position.y - 120f);
            var end = new Vector2(player.Position.x + 4f, 138f);
            var points = new List<Vector2>(7);
            for (var i = 0; i < 7; i++)
            {
                var t = i / 6f;
                var x = Mathf.Lerp(start.x, end.x, t);
                var y = Mathf.Lerp(start.y, end.y, t) - Mathf.Sin(t * Mathf.PI) * 58f;
                points.Add(new Vector2(x, y));
            }

            overlay.SetTrajectory(points);
        }

        private bool TryResolveTutorialBlockAssist()
        {
            if (blockAssistApplied || core.Ball == null || player == null || player.IsGrounded)
            {
                return false;
            }

            var ball = core.Ball;
            if (ball.State != "shooting" || ball.Side == player.Side)
            {
                return false;
            }

            var nearPath = Mathf.Abs(ball.Position.x - player.Position.x) <= 74f;
            var inVerticalLane = ball.Position.y >= player.Position.y - 180f &&
                                 ball.Position.y <= player.Position.y + 42f;
            if (!nearPath || !inVerticalLane)
            {
                return false;
            }

            blockAssistApplied = true;
            ball.ApplyBlock(player);
            return true;
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
