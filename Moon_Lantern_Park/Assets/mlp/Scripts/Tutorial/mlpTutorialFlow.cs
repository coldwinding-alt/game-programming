using UnityEngine;

// 教程流程控制器
// 管理整个新手教程的 10 个练习步骤：移动、冲刺、投篮、假动作、扣篮、抢断、盖帽、补扣、大招、自由对战。
// 每一步都会显示操作提示，等玩家完成后自动进入下一步。

namespace mlp
{
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
        private const float PutbackLooseBallPickupLock = 0.92f;

        private readonly mlpGameCore core;
        private readonly mlpTutorialOverlay overlay;
        private readonly mlpInventory inventory;

        private mlpPlayerObject player;
        private mlpPlayerObject opponent;
        private mlpTutorialOpponentController opponentController;
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
        private bool putbackWindowOpened;
        private bool putbackAttempted;
        private float putbackAttemptStartedAt;
        private bool putbackScored;
        private bool superTriggered;
        private float superTriggeredAt;
        private bool freePlayReadyToEnd;
        private float freePlayScoredAt;

        /// <summary>
        /// 初始化教程流程，传入游戏核心的引用。
        /// </summary>
        public mlpTutorialFlow(mlpGameCore core)
        {
            this.core = core;
            overlay = mlpTutorialOverlay.Active;
            inventory = mlpInventory.Instance;
        }

        /// <summary>
        /// 当游戏需要暂停让玩家阅读说明时返回 true。
        /// </summary>
        public bool FreezeGameplay => phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause;

        /// <summary>
        /// 教程期间始终保持正常速度。
        /// </summary>
        public float GameplayTimeScale => 1f;

        /// <summary>
        /// 开始教程：查找玩家和对手，然后显示开场画面。
        /// </summary>
        public void Start()
        {
            player = core.PlayersLeft.Count > 0 ? core.PlayersLeft[0] : null;
            opponent = core.PlayersRight.Count > 0 ? core.PlayersRight[0] : null;
            opponentController = opponent?.Controller as mlpTutorialOpponentController;
            if (player == null || opponent == null || overlay == null)
            {
                return;
            }

            opponentController?.SetMode(mlpTutorialOpponentMode.Scripted);
            core.PlayerSignals.OnSignal += OnPlayerSignal;
            BeginOpening();
        }

        /// <summary>
        /// 离开教程时清理资源。
        /// </summary>
        public void Shutdown()
        {
            core.PlayerSignals.OnSignal -= OnPlayerSignal;
            overlay?.Hide();
        }

        /// <summary>
        /// 主更新循环：推进计时器、检查练习完成状态、处理跳过指令。
        /// </summary>
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

        /// <summary>
        /// 每帧游戏逻辑结算后刷新覆盖层视觉效果。
        /// </summary>
        public void UpdateAfterGameplay(float dt)
        {
            if (player == null || overlay == null || phase == TutorialPhase.Outro)
            {
                return;
            }

            UpdatePresentation();
        }

        /// <summary>
        /// 在练习中脚本化对手的行为（假动作、抢断、盖帽）。
        /// </summary>
        public void PopulateOpponentInputs(mlpPlayerObject opponentPlayer, mlpTutorialOpponentController controller, float dt)
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

        /// <summary>
        /// 在第一个练习前显示"从这里开始"的开场引导。
        /// </summary>
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

        /// <summary>
        /// 练习 1：教玩家移动。玩家需要向左走过一个标记点，再向右走过另一个标记点。显示"A"和"D"按键提示。
        /// </summary>
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
                new Vector2(moveStartX, mlpObjectsData.PlayerIndentY),
                new Vector2(652f, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 练习 2：教玩家冲刺。玩家需要快速双击 A 或 D 来触发速度爆发。显示双击按键提示。
        /// </summary>
        private void BeginDash(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Dash;
            dashCompletionPending = false;
            dashCompletedAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(286f, mlpObjectsData.PlayerIndentY),
                new Vector2(652f, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 练习 3：教玩家投篮。玩家需要先跳跃（W），然后在跳跃最高点按 B 以获得最佳命中率。同时显示 2 分/3 分得分线。
        /// </summary>
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
                new Vector2(552f, mlpObjectsData.PlayerIndentY),
                new Vector2(654f, mlpObjectsData.PlayerIndentY),
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
                "Jump spot decides points.\nNear rim=2PT. Behind line=3PT.",
                "W",
                "B");
            overlay.SetScoringGuide(player.Side, true);
        }

        /// <summary>
        /// 练习 4：假动作后投篮。
        /// </summary>
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
                new Vector2(344f, mlpObjectsData.PlayerIndentY),
                new Vector2(426f, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 练习 5：在篮筐附近起跳扣篮。
        /// </summary>
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
                new Vector2(642f, mlpObjectsData.PlayerIndentY),
                new Vector2(516f, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 练习 6：靠近对手并抢断。
        /// </summary>
        private void BeginSteal(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Steal;
            stealSuccessPending = false;
            stealSuccessAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(286f, mlpObjectsData.PlayerIndentY),
                new Vector2(518f, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 练习 7：把握跳跃时机来盖帽。
        /// </summary>
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
                new Vector2(BlockDrillPlayerX, mlpObjectsData.PlayerIndentY),
                new Vector2(BlockDrillOpponentX, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 练习 8：抢到篮板球并补篮得分。
        /// </summary>
        private void BeginPutback(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Putback;
            putbackJumped = false;
            putbackWindowOpened = false;
            putbackAttempted = false;
            putbackAttemptStartedAt = 0f;
            putbackScored = false;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(666f, mlpObjectsData.PlayerIndentY),
                new Vector2(516f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            core.Ball.TutorialLaunchPutbackBounce(player.Side, PutbackLooseBallPickupLock);
            player.TutorialPrimePutbackDunk();
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
        /// 练习 9：使用大招技能投篮。
        /// </summary>
        private void BeginSuper(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.Super;
            superTriggered = false;
            superTriggeredAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(242f, mlpObjectsData.PlayerIndentY),
                new Vector2(654f, mlpObjectsData.PlayerIndentY),
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

        /// <summary>
        /// 最后一步：自由对战——投进一球即可完成教程。
        /// </summary>
        private void BeginFreePlay(bool fullIntro)
        {
            ResetStep(fullIntro);
            currentStep = TutorialStep.FreePlay;
            freePlayReadyToEnd = false;
            freePlayScoredAt = 0f;
            PrepareScriptedStep();
            core.TutorialResetScenario(
                new Vector2(238f, mlpObjectsData.PlayerIndentY),
                new Vector2(606f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            opponentController?.SetMode(mlpTutorialOpponentMode.FreePlay);
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

        /// <summary>
        /// 重置所有步骤状态并启动介绍冻结计时器。
        /// </summary>
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

        /// <summary>
        /// 将对手设为脚本模式并清除旧的覆盖层视觉元素。
        /// </summary>
        private void PrepareScriptedStep()
        {
            opponentController?.SetMode(mlpTutorialOpponentMode.Scripted);
            ClearOverlayHighlights();
        }

        /// <summary>
        /// 移除覆盖层上的所有聚焦框、光环、引导线和轨迹点。
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
        /// 检查玩家是否完成了左右移动。如果卡住则显示提示。
        /// </summary>
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

        /// <summary>
        /// 检查玩家是否完成了冲刺。如果卡住太久则自动重试。
        /// </summary>
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

        /// <summary>
        /// 检查玩家是否投篮得分。如果尝试失败则重置。
        /// </summary>
        private void UpdateShotStep()
        {
            if (shotScored)
            {
                return;
            }

            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(shotStage == ShotStage.Jump ? "W, then B. Watch the 2PT/3PT line." : "Highest point has best accuracy.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
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

        /// <summary>
        /// 检查玩家是否完成了假动作并投篮得分。如果卡住则重置。
        /// </summary>
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
                if (pumpJumpIssued && !IsBallStillLive())
                {
                    CompleteStep("Fake worked.");
                    return;
                }

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

        /// <summary>
        /// 检查玩家是否扣篮。在空中时显示"按 B"提示。
        /// </summary>
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

        /// <summary>
        /// 检查玩家是否抢断成功。如果对手漂移太远则重新开始。
        /// </summary>
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

        /// <summary>
        /// 检查玩家是否盖帽成功。在练习过程中显示时机提示。
        /// </summary>
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

        /// <summary>
        /// 在短暂延迟后排队重试盖帽练习。
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
        /// 检查玩家是否抢到篮板并补篮得分。
        /// </summary>
        private void UpdatePutbackStep()
        {
            if (putbackScored)
            {
                return;
            }

            var ball = core.Ball;
            var putbackWindowLive = player != null && player.IsTutorialPutbackBallInWindow(ball);
            if (putbackWindowLive && !putbackWindowOpened)
            {
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

            if (player != null && !player.IsGrounded && !putbackJumped)
            {
                putbackJumped = true;
                if (!putbackWindowOpened)
                {
                    overlay.ShowFeedback("Track the rebound, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }
            }

            if (activeTimer >= 2.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(
                    putbackWindowOpened ? "Ball is live. W + B." : "Jump into the rebound, then B.",
                    new Color32(0xFF, 0xD1, 0x76, 0xFF),
                    HintFeedbackDuration);
            }

            if (putbackWindowOpened && !putbackAttempted && !putbackWindowLive)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Missed the rebound. Attack it earlier.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            if (putbackAttempted && activeTimer - putbackAttemptStartedAt >= PutbackRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Good read. Try the putback again.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            if (!putbackAttempted && activeTimer >= PutbackIdleRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Jump into the rebound, then press B.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
            }
        }

        /// <summary>
        /// 检查玩家是否使用了大招技能投篮。
        /// </summary>
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

        /// <summary>
        /// 自由对战：等待玩家投进一球。
        /// </summary>
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

        /// <summary>
        /// 标记当前步骤完成并显示成功消息。
        /// </summary>
        private void CompleteStep(string message)
        {
            phase = TutorialPhase.SuccessPause;
            phaseTimer = SuccessPauseDuration;
            overlay.ShowFeedback(message, new Color32(0x9A, 0xFF, 0xDD, 0xFF), SuccessPauseDuration);
            ClearOverlayHighlights();
        }

        /// <summary>
        /// 显示带有"接下来去哪？"选项的结束画面。
        /// </summary>
        private void BeginOutro()
        {
            phase = TutorialPhase.Outro;
            overlay.ShowOutro(mlpPlayersData.GetCharacterName(player.CharacterId), mlpCharacterSkillsData.Get(player.CharacterId).SkillName);
        }

        /// <summary>
        /// 成功暂停结束后进入下一个练习。
        /// </summary>
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

        /// <summary>
        /// 检查覆盖层是否发送了指令（如跳过）。
        /// </summary>
        private bool HandleStepCommand()
        {
            var command = overlay.ConsumeCommand();
            if (command == mlpTutorialOverlayCommand.None)
            {
                return false;
            }

            if (command == mlpTutorialOverlayCommand.SkipStep)
            {
                SkipCurrentStep();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 玩家按下跳过时跳转到下一步。
        /// </summary>
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

        /// <summary>
        /// 监听玩家动作（冲刺、投篮、扣篮、抢断、盖帽、得分等）。
        /// </summary>
        private void OnPlayerSignal(mlpPlayerSignalType signal, int side, int playerNo)
        {
            if (player == null || phase != TutorialPhase.Active)
            {
                return;
            }

            if (currentStep == TutorialStep.Block &&
                signal == mlpPlayerSignalType.Score &&
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
                case mlpPlayerSignalType.Dash:
                    if (currentStep == TutorialStep.Dash && !dashCompletionPending)
                    {
                        dashCompletionPending = true;
                        dashCompletedAt = activeTimer;
                        overlay.ShowFeedback("Good dash. Feel the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.JumpA:
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
                    if (currentStep == TutorialStep.Shot)
                    {
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
                    if (currentStep == TutorialStep.Dunk)
                    {
                        dunkAttempted = true;
                        dunkAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Strong take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    else if (currentStep == TutorialStep.Putback)
                    {
                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Putback take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.StealSuccess:
                    if (currentStep == TutorialStep.Steal)
                    {
                        stealSuccessPending = true;
                        stealSuccessAt = activeTimer;
                        overlay.ShowFeedback("Nice steal.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Block:
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
                    if (currentStep == TutorialStep.Putback)
                    {
                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("There it is. Finish the rebound.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Super:
                    if (currentStep == TutorialStep.Super && !superTriggered)
                    {
                        superTriggered = true;
                        superTriggeredAt = activeTimer;
                        overlay.ShowFeedback("Skill live. Watch the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Score:
                    if (currentStep == TutorialStep.Shot && shotAttempted)
                    {
                        shotScored = true;
                        CompleteStep(core.MatchProcessor.ThrowType == 0 ? "Good rhythm. 3PT." : "Good rhythm. 2PT.");
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

        /// <summary>
        /// 处理结束画面上的按钮点击（重玩、训练、菜单）。
        /// </summary>
        private void HandleOutroCommand()
        {
            var command = overlay.ConsumeCommand();
            if (command == mlpTutorialOverlayCommand.None)
            {
                return;
            }

            inventory.PendingTutorialNextAction = command switch
            {
                mlpTutorialOverlayCommand.ReturnToMenu => mlpTutorialNextAction.None,
                mlpTutorialOverlayCommand.ReplayTutorial => mlpTutorialNextAction.ReplayTutorial,
                mlpTutorialOverlayCommand.StartTraining => mlpTutorialNextAction.StartTraining,
                mlpTutorialOverlayCommand.StartQuickMatch => mlpTutorialNextAction.StartQuickMatch,
                _ => mlpTutorialNextAction.None
            };

            core.RequestReturnToMenu();
        }

        /// <summary>
        /// 刷新覆盖层视觉效果（得分引导等）。
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
        /// 判断篮球是否在空中（投篮、扣篮、盖帽等状态）。
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
        /// 返回 -1（向左）、1（向右）或 0（已足够接近），用于向目标 X 位置移动。
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
