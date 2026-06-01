using UnityEngine;

namespace rimrush
{
    public sealed class rimrushTutorialFlow
    {
        private enum TutorialPhase
        {
            Intro,
            Active,
            Success,
            Outro
        }

        private enum TutorialStep
        {
            Dash,
            Shot,
            Steal,
            Block,
            Super
        }

        private const int TotalSteps = 5;
        private const float FullIntroDuration = 1.05f;
        private const float RetryIntroDuration = 0.66f;
        private const float SuccessPauseDuration = 0.92f;

        private readonly rimrushGameCore core;
        private readonly rimrushTutorialOverlay overlay;
        private readonly rimrushInventory inventory;

        private rimrushPlayerObject player;
        private rimrushPlayerObject opponent;
        private TutorialPhase phase;
        private TutorialStep currentStep;
        private float phaseTimer;
        private float activeTimer;
        private float slowMotionTimer;
        private bool awaitingApexScore;
        private bool shotAttempted;
        private bool opponentJumpIssued;
        private bool opponentShotIssued;

        public rimrushTutorialFlow(rimrushGameCore core)
        {
            this.core = core;
            overlay = rimrushTutorialOverlay.Active;
            inventory = rimrushInventory.Instance;
        }

        public bool FreezeGameplay => phase == TutorialPhase.Intro || phase == TutorialPhase.Success || phase == TutorialPhase.Outro;

        public float GameplayTimeScale =>
            currentStep == TutorialStep.Block && phase == TutorialPhase.Active && slowMotionTimer > 0f
                ? 0.48f
                : 1f;

        public void Start()
        {
            player = core.PlayersLeft.Count > 0 ? core.PlayersLeft[0] : null;
            opponent = core.PlayersRight.Count > 0 ? core.PlayersRight[0] : null;
            if (player == null || opponent == null || overlay == null)
            {
                return;
            }

            core.PlayerSignals.OnSignal += OnPlayerSignal;
            BeginStep(TutorialStep.Dash, true);
        }

        public void Shutdown()
        {
            core.PlayerSignals.OnSignal -= OnPlayerSignal;
            overlay?.Hide();
        }

        public void UpdateFrame(float dt)
        {
            if (overlay == null)
            {
                return;
            }

            if (slowMotionTimer > 0f)
            {
                slowMotionTimer = Mathf.Max(0f, slowMotionTimer - dt);
            }

            if (phase == TutorialPhase.Outro)
            {
                HandleOutroCommand();
                return;
            }

            UpdateFocus();
            if (phase == TutorialPhase.Intro || phase == TutorialPhase.Success)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    if (phase == TutorialPhase.Intro)
                    {
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
                case TutorialStep.Dash:
                    UpdateDashStep();
                    break;
                case TutorialStep.Shot:
                    UpdateShotStep();
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
            }
        }

        public void UpdateAfterGameplay(float dt)
        {
            if (phase != TutorialPhase.Active)
            {
                return;
            }

            UpdateFocus();
        }

        public void PopulateOpponentInputs(rimrushPlayerObject opponentPlayer, rimrushTutorialOpponentController controller, float dt)
        {
            if (controller == null || opponentPlayer == null || phase != TutorialPhase.Active)
            {
                return;
            }

            switch (currentStep)
            {
                case TutorialStep.Steal:
                    controller.SetFrameInputs(MoveTo(opponentPlayer.Position.x, 332f), false, false, false, false, 0);
                    break;

                case TutorialStep.Block:
                {
                    var move = 0;
                    var jump = false;
                    var action = false;
                    if (!opponentJumpIssued)
                    {
                        if (Mathf.Abs(opponentPlayer.Position.x - 612f) > 10f)
                        {
                            move = MoveTo(opponentPlayer.Position.x, 612f);
                        }
                        else if (activeTimer >= 0.55f)
                        {
                            jump = true;
                            opponentJumpIssued = true;
                        }
                    }
                    else if (!opponentPlayer.IsGrounded && !opponentShotIssued && opponentPlayer.CanThrow && opponentPlayer.Velocity.y < 40f)
                    {
                        action = true;
                        opponentShotIssued = true;
                        slowMotionTimer = Mathf.Max(slowMotionTimer, 0.85f);
                    }

                    controller.SetFrameInputs(move, jump, action, false, false, 0);
                    break;
                }
            }
        }

        private void BeginStep(TutorialStep step, bool fullIntro)
        {
            currentStep = step;
            phase = TutorialPhase.Intro;
            phaseTimer = fullIntro ? FullIntroDuration : RetryIntroDuration;
            activeTimer = 0f;
            slowMotionTimer = 0f;
            awaitingApexScore = false;
            shotAttempted = false;
            opponentJumpIssued = false;
            opponentShotIssued = false;

            switch (step)
            {
                case TutorialStep.Dash:
                    core.TutorialResetScenario(new Vector2(132f, 356f), new Vector2(654f, 356f), false, false, 1f, -1f);
                    overlay.ShowStep(
                        0,
                        TotalSteps,
                        "DOUBLE-TAP TO DASH",
                        "Burst forward with a quick double tap. This instantly tells the examiner the game has speed.",
                        "Two taps beats one long hold.",
                        "Dash into the glowing lane.",
                        new[] { "D x2" });
                    break;

                case TutorialStep.Shot:
                    core.TutorialResetScenario(new Vector2(220f, 356f), new Vector2(654f, 356f), true, false, 1f, -1f);
                    overlay.ShowStep(
                        1,
                        TotalSteps,
                        "JUMP, THEN SHOOT AT THE PEAK",
                        "Air releases read cleaner and feel more skillful. We want the ball leaving your hands at the top.",
                        "Highest point = best-looking shot rhythm.",
                        "Score one clean apex shot.",
                        new[] { "W", "B" });
                    break;

                case TutorialStep.Steal:
                    core.TutorialResetScenario(new Vector2(284f, 356f), new Vector2(504f, 356f), false, true, 1f, -1f);
                    overlay.ShowStep(
                        2,
                        TotalSteps,
                        "PRESSURE THE DRIBBLER",
                        "Without the ball, your action button becomes a steal attempt. Step in and rip it loose.",
                        "Close range matters more than mashing.",
                        "Strip the ball from the carrier.",
                        new[] { "B" });
                    break;

                case TutorialStep.Block:
                    core.TutorialResetScenario(new Vector2(470f, 356f), new Vector2(608f, 356f), false, true, -1f, -1f);
                    overlay.ShowStep(
                        3,
                        TotalSteps,
                        "BLOCK THE SHOT PATH",
                        "A great block is not standing still. Meet the release in the air and swat the flight line.",
                        "Jump into the path, not after the ball is gone.",
                        "Time one clean rejection.",
                        new[] { "W" });
                    break;

                case TutorialStep.Super:
                    core.TutorialResetScenario(new Vector2(246f, 356f), new Vector2(654f, 356f), true, false, 1f, -1f);
                    player?.TutorialChargeSuper();
                    overlay.ShowStep(
                        4,
                        TotalSteps,
                        "UNLEASH YOUR SIGNATURE SUPER",
                        "Every character has a headline move. Use it here so the examiner immediately sees depth beyond normal shooting.",
                        "Your solo tutorial key is N.",
                        "Spend the full energy charge now.",
                        new[] { "N" });
                    break;
            }

            UpdateFocus();
        }

        private void UpdateDashStep()
        {
            if (player != null && player.IsDashing && player.Position.x >= 264f)
            {
                CompleteStep("Good. The game already feels faster.");
                return;
            }

            if (activeTimer > 3.2f && Mathf.FloorToInt(activeTimer * 10f) % 25 == 0)
            {
                overlay.ShowFeedback("Double-tap. Do not just hold the key.", new Color32(0xFF, 0xC0, 0x6A, 0xFF), 0.9f);
            }
        }

        private void UpdateShotStep()
        {
            if (!shotAttempted)
            {
                return;
            }

            if (activeTimer >= 4.5f)
            {
                overlay.ShowFeedback("Try again. Jump first, then release higher.", new Color32(0xFF, 0xC0, 0x6A, 0xFF), 1f);
                BeginStep(TutorialStep.Shot, false);
            }
        }

        private void UpdateStealStep()
        {
            if (opponent != null && opponent.Position.x <= 314f)
            {
                overlay.ShowFeedback("Close the gap before you swipe.", new Color32(0xFF, 0xC0, 0x6A, 0xFF), 1f);
                BeginStep(TutorialStep.Steal, false);
                return;
            }

            if (activeTimer >= 4.8f)
            {
                overlay.ShowFeedback("Step right into the dribble, then press B.", new Color32(0xFF, 0xC0, 0x6A, 0xFF), 1f);
                BeginStep(TutorialStep.Steal, false);
            }
        }

        private void UpdateBlockStep()
        {
            if (opponentShotIssued && activeTimer >= 4.6f)
            {
                overlay.ShowFeedback("Read the arc earlier and jump into it.", new Color32(0xFF, 0xC0, 0x6A, 0xFF), 1f);
                BeginStep(TutorialStep.Block, false);
            }
        }

        private void UpdateSuperStep()
        {
            if (player != null && !player.ReadyForSuper)
            {
                return;
            }

            if (activeTimer > 3f && Mathf.FloorToInt(activeTimer * 10f) % 20 == 0)
            {
                overlay.ShowFeedback("Energy is full. Press N now.", new Color32(0x9E, 0xFF, 0xDA, 0xFF), 0.7f);
            }
        }

        private void CompleteStep(string message)
        {
            phase = TutorialPhase.Success;
            phaseTimer = SuccessPauseDuration;
            overlay.ShowFeedback(message, new Color32(0x98, 0xFF, 0xD8, 0xFF), SuccessPauseDuration);
        }

        private void AdvanceAfterSuccess()
        {
            switch (currentStep)
            {
                case TutorialStep.Dash:
                    BeginStep(TutorialStep.Shot, true);
                    break;
                case TutorialStep.Shot:
                    BeginStep(TutorialStep.Steal, true);
                    break;
                case TutorialStep.Steal:
                    BeginStep(TutorialStep.Block, true);
                    break;
                case TutorialStep.Block:
                    BeginStep(TutorialStep.Super, true);
                    break;
                default:
                    phase = TutorialPhase.Outro;
                    overlay.ShowOutro();
                    break;
            }
        }

        private void OnPlayerSignal(rimrushPlayerSignalType signal, int side, int playerNo)
        {
            if (player == null || phase != TutorialPhase.Active)
            {
                return;
            }

            if (signal == rimrushPlayerSignalType.Score &&
                currentStep == TutorialStep.Block &&
                side == opponent.Side &&
                playerNo == opponent.PlayerNo)
            {
                overlay.ShowFeedback("You were late. Meet the ball higher.", new Color32(0xFF, 0xC0, 0x6A, 0xFF), 1f);
                BeginStep(TutorialStep.Block, false);
                return;
            }

            if (side != player.Side || playerNo != player.PlayerNo)
            {
                return;
            }

            switch (signal)
            {
                case rimrushPlayerSignalType.Shoot:
                    if (currentStep == TutorialStep.Shot)
                    {
                        shotAttempted = true;
                        awaitingApexScore = !player.IsGrounded && Mathf.Abs(player.Velocity.y) <= 42f;
                        overlay.ShowFeedback(
                            awaitingApexScore ? "That release looked clean." : "A little higher. Release at the peak.",
                            awaitingApexScore ? new Color32(0x98, 0xFF, 0xD8, 0xFF) : new Color32(0xFF, 0xC0, 0x6A, 0xFF),
                            0.85f);
                    }
                    break;

                case rimrushPlayerSignalType.StealSuccess:
                    if (currentStep == TutorialStep.Steal)
                    {
                        CompleteStep("Nice. Defense can create offense here.");
                    }
                    break;

                case rimrushPlayerSignalType.Block:
                    if (currentStep == TutorialStep.Block)
                    {
                        CompleteStep("Perfect. That proves the game has defense.");
                    }
                    break;

                case rimrushPlayerSignalType.Super:
                    if (currentStep == TutorialStep.Super)
                    {
                        CompleteStep("Exactly. Your character identity is obvious now.");
                    }
                    break;

                case rimrushPlayerSignalType.Score:
                    if (currentStep == TutorialStep.Shot && awaitingApexScore)
                    {
                        CompleteStep("Beautiful. That is the shot you want them to remember.");
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

        private void UpdateFocus()
        {
            if (overlay == null || player == null)
            {
                return;
            }

            switch (currentStep)
            {
                case TutorialStep.Dash:
                    overlay.SetFocusRect(96f, 238f, 248f, 124f);
                    break;

                case TutorialStep.Shot:
                    overlay.SetFocusRect(136f, 110f, 276f, 248f);
                    break;

                case TutorialStep.Steal:
                    if (opponent != null)
                    {
                        overlay.SetFocusRect(opponent.Position.x - 74f, 212f, 192f, 118f);
                    }
                    break;

                case TutorialStep.Block:
                    overlay.SetFocusRect(434f, 102f, 244f, 236f);
                    break;

                case TutorialStep.Super:
                    overlay.SetFocusRect(activeTimer < 0.8f ? 14f : player.Position.x - 80f, activeTimer < 0.8f ? 12f : 188f, activeTimer < 0.8f ? 182f : 200f, activeTimer < 0.8f ? 94f : 146f);
                    break;
            }
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
