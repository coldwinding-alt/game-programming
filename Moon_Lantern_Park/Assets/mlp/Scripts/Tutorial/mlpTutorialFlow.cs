using UnityEngine;

// 教程流程控制器
// 管理整个新手教程的 10 个练习步骤：移动、冲刺、投篮、假动作、扣篮、抢断、盖帽、补扣、大招、自由对战。
// 每一步都会显示操作提示，等玩家完成后自动进入下一步。

namespace mlp
{
    /// <summary>
    /// 教程流程控制器：管理新手教程的 10 个练习步骤（移动、冲刺、投篮、假动作、扣篮、抢断、盖帽、补扣、大招、自由对战），每步显示操作提示，完成后自动进入下一步。
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
            // 1. 保存游戏核心的引用，后续用来操作比赛场景
            this.core = core;
            // 2. 获取教程覆盖层（显示操作提示的 UI 面板）
            overlay = mlpTutorialOverlay.Active;
            // 3. 获取背包/存档实例（用于保存教程完成后的下一步选择）
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
            // 1. 从游戏核心中找到左边的玩家（学员）和右边的对手（女巫）
            player = core.PlayersLeft.Count > 0 ? core.PlayersLeft[0] : null;
            opponent = core.PlayersRight.Count > 0 ? core.PlayersRight[0] : null;
            // 2. 获取对手的教程控制器（用来脚本化对手动作配合练习）
            opponentController = opponent?.Controller as mlpTutorialOpponentController;
            // 3. 如果缺少玩家、对手或覆盖层，直接退出（不启动教程）
            if (player == null || opponent == null || overlay == null)
            {
                return;
            }

            // 4. 将对手设为"脚本模式"——由教程系统控制对手移动和跳跃
            opponentController?.SetMode(mlpTutorialOpponentMode.Scripted);
            // 5. 订阅玩家动作信号（跳跃、投篮、扣篮、抢断等），用于判断练习是否完成
            core.PlayerSignals.OnSignal += OnPlayerSignal;
            // 6. 显示开场引导画面，教程正式开始
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
            // 1. 如果覆盖层或玩家不存在，直接退出
            if (overlay == null || player == null)
            {
                return;
            }

            // 2. 如果教程已进入结束画面，处理结束画面的按钮点击（重玩、训练、菜单）
            if (phase == TutorialPhase.Outro)
            {
                UpdatePresentation();
                HandleOutroCommand();
                return;
            }

            // 3. 刷新覆盖层视觉效果（得分引导线等）
            UpdatePresentation();
            // 4. 检查玩家是否按了"跳过"按钮，如果是则跳到下一步
            if (HandleStepCommand())
            {
                return;
            }

            // 5. 如果当前处于"介绍冻结"或"成功暂停"阶段，倒计时等待
            if (phase == TutorialPhase.IntroFreeze || phase == TutorialPhase.SuccessPause)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    // 5a. 介绍冻结结束：如果是开场画面，进入第一个练习；否则开始操作阶段
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
                        // 5b. 成功暂停结束：自动进入下一个练习
                        AdvanceAfterSuccess();
                    }
                }

                return;
            }

            // 6. 只有在"操作阶段"才执行练习逻辑
            if (phase != TutorialPhase.Active)
            {
                return;
            }

            // 7. 累加操作计时器，根据当前练习步骤调用对应的检测方法
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
            // 1. 如果玩家、覆盖层不存在或已进入结束画面，直接退出
            if (player == null || overlay == null || phase == TutorialPhase.Outro)
            {
                return;
            }

            // 2. 在游戏逻辑结算后，刷新覆盖层上的视觉效果（如 2 分/3 分引导线）
            UpdatePresentation();
        }

        /// <summary>
        /// 在练习中脚本化对手的行为（假动作、抢断、盖帽）。
        /// </summary>
        public void PopulateOpponentInputs(mlpPlayerObject opponentPlayer, mlpTutorialOpponentController controller, float dt)
        {
            // 1. 如果控制器不存在或教程不在操作阶段，直接退出
            if (controller == null || opponentPlayer == null || phase != TutorialPhase.Active)
            {
                return;
            }

            // 2. 根据当前练习步骤，脚本化对手的行为
            switch (currentStep)
            {
                case TutorialStep.Pump:
                {
                    // 2a. 假动作练习：对手走到指定位置，然后在合适时机被骗起跳
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
                    // 2b. 抢断练习：对手走到指定位置站好，等玩家来抢
                    controller.SetFrameInputs(MoveTo(opponentPlayer.Position.x, 332f), false, false, false, false, 0);
                    break;

                case TutorialStep.Block:
                {
                    // 2c. 盖帽练习：对手走到投篮位置 → 起跳 → 在合适时机出手（带慢动作效果）
                    var move = 0;
                    var jump = false;
                    var action = false;
                    if (!blockJumpIssued)
                    {
                        // 2d. 还没走到位置就继续走，到了就起跳
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
                        // 2e. 在空中时，等玩家也起跳或者球速到达释放点时才出手投篮
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
            // 1. 设置当前步骤为"开场"，进入介绍冻结阶段
            currentStep = TutorialStep.Opening;
            phase = TutorialPhase.IntroFreeze;
            phaseTimer = 1.8f;
            activeTimer = 0f;
            // 2. 显示开场引导画面，列出所有操作按键说明
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
            // 1. 重置步骤状态并设置为"移动"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Move;
            // 2. 记录玩家起始位置，初始化左右移动完成标志
            moveStartX = 360f;
            movedLeft = false;
            movedRight = false;
            moveCompletionPending = false;
            moveCompletedAt = 0f;
            // 3. 将对手设为脚本模式，清除旧的覆盖层
            PrepareScriptedStep();
            // 4. 重置比赛场景：玩家在中间，对手在远处，玩家持球
            core.TutorialResetScenario(
                new Vector2(moveStartX, mlpObjectsData.PlayerIndentY),
                new Vector2(652f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            // 5. 显示操作提示：左右移动
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
            // 1. 重置步骤状态并设置为"冲刺"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Dash;
            dashCompletionPending = false;
            dashCompletedAt = 0f;
            // 2. 准备脚本化场景
            PrepareScriptedStep();
            // 3. 重置场景：玩家在偏左位置，对手在远处
            core.TutorialResetScenario(
                new Vector2(286f, mlpObjectsData.PlayerIndentY),
                new Vector2(652f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            // 4. 显示操作提示：快速双击 A 或 D 来冲刺
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
            // 1. 重置步骤状态并设置为"投篮"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Shot;
            // 2. 初始化投篮阶段（先跳跃）、尝试标志和得分标志
            shotStage = ShotStage.Jump;
            shotAttempted = false;
            shotAttemptStartedAt = 0f;
            shotPeakValid = false;
            shotScored = false;
            // 3. 准备脚本化场景
            PrepareScriptedStep();
            // 4. 重置场景：玩家在三分线附近，对手在远处，玩家持球
            core.TutorialResetScenario(
                new Vector2(552f, mlpObjectsData.PlayerIndentY),
                new Vector2(654f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 5. 预设完美投篮时机（降低难度让玩家更容易成功）
            player.TutorialPrimePerfectShot();
            // 6. 显示操作提示：跳跃后在最高点投篮，位置决定 2 分或 3 分
            overlay.ShowStep(
                2,
                TotalSteps,
                "JUMP SHOT",
                string.Empty,
                string.Empty,
                "Jump spot decides points.\nNear rim=2PT. Behind line=3PT.",
                "W",
                "B");
            // 7. 在场地上显示 2 分/3 分得分引导线
            overlay.SetScoringGuide(player.Side, true);
        }

        /// <summary>
        /// 练习 4：假动作后投篮。
        /// </summary>
        private void BeginPump(bool fullIntro)
        {
            // 1. 重置步骤状态并设置为"假动作"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Pump;
            // 2. 初始化假动作阶段和所有相关标志
            pumpStage = PumpStage.Fake;
            pumpTriggered = false;
            pumpTriggeredAt = 0f;
            pumpShotAttempted = false;
            pumpShotStartedAt = 0f;
            pumpBitePending = false;
            pumpJumpIssued = false;
            // 3. 准备脚本化场景
            PrepareScriptedStep();
            // 4. 清除上一次的保底得分设置，重置场景
            core.Ball.TutorialClearGuaranteedScore();
            core.TutorialResetScenario(
                new Vector2(344f, mlpObjectsData.PlayerIndentY),
                new Vector2(426f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 5. 显示操作提示：按住 S 做假动作，松开后投篮
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
            // 1. 重置步骤状态并设置为"扣篮"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Dunk;
            // 2. 初始化扣篮相关标志
            dunkJumped = false;
            dunkAttempted = false;
            dunkAttemptStartedAt = 0f;
            dunkScored = false;
            // 3. 准备脚本化场景
            PrepareScriptedStep();
            // 4. 重置场景：玩家在篮筐附近，对手在远处，玩家持球
            core.TutorialResetScenario(
                new Vector2(642f, mlpObjectsData.PlayerIndentY),
                new Vector2(516f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 5. 预设完美扣篮时机（降低难度）
            player.TutorialPrimePerfectDunk();
            // 6. 显示操作提示：在篮筐附近起跳后按 B 扣篮
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
            // 1. 重置步骤状态并设置为"抢断"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Steal;
            stealSuccessPending = false;
            stealSuccessAt = 0f;
            // 2. 准备脚本化场景
            PrepareScriptedStep();
            // 3. 重置场景：玩家在左侧，对手持球在中间偏右
            core.TutorialResetScenario(
                new Vector2(286f, mlpObjectsData.PlayerIndentY),
                new Vector2(518f, mlpObjectsData.PlayerIndentY),
                false,
                true,
                1f,
                -1f);
            // 4. 显示操作提示：靠近对手后按 B 抢断
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
            // 1. 重置步骤状态并设置为"盖帽"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Block;
            // 2. 初始化盖帽练习的所有标志（对手跳跃、出手、成功、重试等）
            blockJumpIssued = false;
            blockShotIssued = false;
            blockShotIssuedAt = 0f;
            blockJumpPromptShown = false;
            blockReleasePromptShown = false;
            blockSuccessPending = false;
            blockSuccessAt = 0f;
            blockRetryPending = false;
            blockRetryAt = 0f;
            // 3. 准备脚本化场景
            PrepareScriptedStep();
            // 4. 重置场景：玩家和对手紧挨着站位，对手持球
            core.TutorialResetScenario(
                new Vector2(BlockDrillPlayerX, mlpObjectsData.PlayerIndentY),
                new Vector2(BlockDrillOpponentX, mlpObjectsData.PlayerIndentY),
                false,
                true,
                -1f,
                -1f);
            // 5. 减慢对手空中运动速度（慢动作效果，方便玩家看清投篮时机）
            opponent.TutorialSetAirMotionTimeScale(BlockDrillOpponentAirTimeScale);
            // 6. 开启盖帽辅助（增加盖帽判定范围）
            player.TutorialSetJumpBlockAssist(true);
            // 7. 显示操作提示：观察慢动作投篮，在出手时按 W 起跳盖帽
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
            // 1. 重置步骤状态并设置为"补扣"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Putback;
            // 2. 初始化补扣相关标志
            putbackJumped = false;
            putbackWindowOpened = false;
            putbackAttempted = false;
            putbackAttemptStartedAt = 0f;
            putbackScored = false;
            // 3. 准备脚本化场景
            PrepareScriptedStep();
            // 4. 重置场景：玩家在篮筐右侧，对手在远处，无人持球
            core.TutorialResetScenario(
                new Vector2(666f, mlpObjectsData.PlayerIndentY),
                new Vector2(516f, mlpObjectsData.PlayerIndentY),
                false,
                false,
                1f,
                -1f);
            // 5. 发射一个篮板弹跳球（模拟投篮打铁后的篮板球）
            core.Ball.TutorialLaunchPutbackBounce(player.Side, PutbackLooseBallPickupLock);
            // 6. 预设完美补扣时机（降低难度）
            player.TutorialPrimePutbackDunk();
            // 7. 显示操作提示：冲抢篮板，起跳后按 B 补扣
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
            // 1. 重置步骤状态并设置为"大招"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.Super;
            superTriggered = false;
            superTriggeredAt = 0f;
            // 2. 准备脚本化场景
            PrepareScriptedStep();
            // 3. 重置场景：玩家在左侧持球，对手在远处
            core.TutorialResetScenario(
                new Vector2(242f, mlpObjectsData.PlayerIndentY),
                new Vector2(654f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 4. 将玩家大招能量充满
            player.TutorialChargeSuper();
            // 5. 显示操作提示：能量已满，按 N 释放大招
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
            // 1. 重置步骤状态并设置为"自由对战"练习
            ResetStep(fullIntro);
            currentStep = TutorialStep.FreePlay;
            freePlayReadyToEnd = false;
            freePlayScoredAt = 0f;
            // 2. 准备脚本化场景
            PrepareScriptedStep();
            // 3. 重置场景：玩家持球，对手在远处
            core.TutorialResetScenario(
                new Vector2(238f, mlpObjectsData.PlayerIndentY),
                new Vector2(606f, mlpObjectsData.PlayerIndentY),
                true,
                false,
                1f,
                -1f);
            // 4. 将对手切换到自由对战模式（会正常防守和进攻，不再是脚本模式）
            opponentController?.SetMode(mlpTutorialOpponentMode.FreePlay);
            // 5. 显示操作提示：投进一球即可完成教程
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
            // 6. 清除覆盖层上的所有视觉辅助（引导线等），让玩家自由发挥
            ClearOverlayHighlights();
        }

        /// <summary>
        /// 重置所有步骤状态并启动介绍冻结计时器。
        /// </summary>
        private void ResetStep(bool fullIntro)
        {
            // 1. 进入介绍冻结阶段（暂停游戏让玩家阅读说明）
            phase = TutorialPhase.IntroFreeze;
            // 2. 首次进入用较长冻结时间，重试时用较短时间
            phaseTimer = fullIntro ? FullIntroDuration : RetryIntroDuration;
            // 3. 重置操作计时器和提示显示标志
            activeTimer = 0f;
            stepHintShown = false;
            stepRetryHintShown = false;
            // 4. 重置大招状态
            superTriggered = false;
            superTriggeredAt = 0f;
            // 5. 关闭盖帽辅助（每个步骤按需单独开启）
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
            // 1. 检测玩家是否向左移动了足够距离
            if (player.Position.x <= moveStartX - 42f)
            {
                movedLeft = true;
            }

            // 2. 检测玩家是否向右移动了足够距离
            if (player.Position.x >= moveStartX + 42f)
            {
                movedRight = true;
            }

            // 3. 左右都移动过且超过最短时间 → 标记完成，显示成功反馈
            if (!moveCompletionPending && movedLeft && movedRight && activeTimer >= 2.2f)
            {
                moveCompletionPending = true;
                moveCompletedAt = activeTimer;
                overlay.ShowFeedback("Good. Keep that court feel.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                return;
            }

            // 4. 完成后等待一小段时间再进入下一步（让玩家看到成功提示）
            if (moveCompletionPending)
            {
                if (activeTimer - moveCompletedAt >= StepSettleDuration)
                {
                    CompleteStep("Left and right are yours.");
                }

                return;
            }

            // 5. 超过 3.4 秒还没完成 → 显示第一个提示
            if (activeTimer >= 3.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Try both A and D.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 6. 超过 7.4 秒还没完成 → 显示更详细的提示
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
            // 1. 如果冲刺已完成，等待结算时间后进入下一步
            if (dashCompletionPending)
            {
                if (activeTimer - dashCompletedAt >= StepSettleDuration)
                {
                    CompleteStep("Dash gives you the burst.");
                }

                return;
            }

            // 2. 超过 3.2 秒还没冲刺 → 显示提示
            if (activeTimer >= 3.2f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Tap A twice, or D twice.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 3. 超过 6.6 秒还没冲刺 → 自动重置练习并显示更详细的提示
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
            // 1. 如果已经投篮得分，不再检测
            if (shotScored)
            {
                return;
            }

            // 2. 超过 2.6 秒 → 根据当前阶段显示对应提示（跳跃阶段提示先跳，空中阶段提示最高点出手）
            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(shotStage == ShotStage.Jump ? "W, then B. Watch the 2PT/3PT line." : "Highest point has best accuracy.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 3. 已尝试投篮但超时未命中 → 重置练习
            if (shotAttempted && activeTimer - shotAttemptStartedAt >= ShotRetryWindow)
            {
                BeginShot(false);
                overlay.ShowFeedback("Good try. Jump and release once more.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 4. 长时间没尝试投篮 → 重置练习并显示提示
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
            // 阶段一：等待玩家做假动作
            if (!pumpTriggered)
            {
                // 1. 超过 2.4 秒还没做假动作 → 显示提示
                if (activeTimer >= 2.4f && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Hold S with the ball.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }

                // 2. 超过 5.2 秒还没做假动作 → 重置练习
                if (activeTimer >= 5.2f)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Sell the fake first.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                }

                return;
            }

            // 阶段二：假动作已触发，等待玩家投篮
            if (pumpShotAttempted)
            {
                // 3. 对手已被骗起跳且球不在空中（已出手） → 练习完成
                if (pumpJumpIssued && !IsBallStillLive())
                {
                    CompleteStep("Fake worked.");
                    return;
                }

                // 4. 投篮超时未完成 → 重置练习
                if (activeTimer - pumpShotStartedAt >= ShotRetryWindow)
                {
                    BeginPump(false);
                    overlay.ShowFeedback("Good try. Fake, release, then finish the shot.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                }

                return;
            }

            // 阶段三：假动作已触发但还没投篮
            // 5. 假动作后超过 2.2 秒还没投篮 → 显示"松手后投篮"提示
            if (activeTimer - pumpTriggeredAt >= 2.2f && !stepRetryHintShown)
            {
                stepRetryHintShown = true;
                overlay.ShowFeedback("Let go, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 6. 假动作后超时 → 重置练习
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
            // 1. 如果已经扣篮得分，不再检测
            if (dunkScored)
            {
                return;
            }

            // 2. 玩家跳起后更新提示为"按 B 完成扣篮"
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

            // 3. 超过 2.5 秒还没扣篮 → 显示提示
            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("W, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 4. 已尝试扣篮但超时 → 重置练习
            if (dunkAttempted && activeTimer - dunkAttemptStartedAt >= DunkRetryWindow)
            {
                BeginDunk(false);
                overlay.ShowFeedback("Good try. Press B higher in the paint.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 5. 长时间没尝试扣篮 → 重置练习
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
            // 1. 如果抢断已成功，等待玩家拿到球或超时后完成步骤
            if (stealSuccessPending)
            {
                var elapsed = activeTimer - stealSuccessAt;
                if (elapsed >= StealCompletionMinDelay && (player.WithBall || elapsed >= StealCompletionMaxDelay))
                {
                    CompleteStep("Nice. Defense can start the offense.");
                }

                return;
            }

            // 2. 如果对手漂移太远（不在合理位置），重置练习
            if (opponent != null && opponent.Position.x <= 292f)
            {
                BeginSteal(false);
                overlay.ShowFeedback("Get closer before you swipe.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 3. 超过 2.6 秒还没抢断 → 显示"靠近对手"提示
            if (activeTimer >= 2.6f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Get close.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 4. 超过 4.6 秒还没抢断 → 重置练习
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
            // 1. 如果盖帽已成功，等待观察时间后完成步骤
            if (blockSuccessPending)
            {
                if (activeTimer - blockSuccessAt >= BlockObserveDuration)
                {
                    CompleteStep("Good block. You changed the shot.");
                }

                return;
            }

            // 2. 如果正在等待重试，到时间后重新开始盖帽练习
            if (blockRetryPending)
            {
                if (activeTimer >= blockRetryAt)
                {
                    BeginBlock(false);
                }

                return;
            }

            // 3. 对手已起跳但还没出手 → 更新提示为"准备起跳"
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

            // 4. 对手快出手时（球速接近释放点） → 更新提示为"现在按 W"
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

            // 5. 超过 2.8 秒 → 显示通用提示
            if (activeTimer >= 2.8f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Wait for the release, then W.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
            }

            // 6. 对手出手后超时未盖帽 → 安排重试
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
            // 1. 如果已经补篮得分，不再检测
            if (putbackScored)
            {
                return;
            }

            // 2. 检测球是否进入"补扣窗口"（球在篮筐附近且玩家可以够到）
            var ball = core.Ball;
            var putbackWindowLive = player != null && player.IsTutorialPutbackBallInWindow(ball);
            if (putbackWindowLive && !putbackWindowOpened)
            {
                // 3. 窗口首次打开 → 预设完美补扣时机，更新提示为"现在起跳补扣"
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

            // 4. 玩家起跳后如果窗口还没打开，显示"追踪篮板球"提示
            if (player != null && !player.IsGrounded && !putbackJumped)
            {
                putbackJumped = true;
                if (!putbackWindowOpened)
                {
                    overlay.ShowFeedback("Track the rebound, then B.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }
            }

            // 5. 超过 2.4 秒 → 显示提示
            if (activeTimer >= 2.4f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback(
                    putbackWindowOpened ? "Ball is live. W + B." : "Jump into the rebound, then B.",
                    new Color32(0xFF, 0xD1, 0x76, 0xFF),
                    HintFeedbackDuration);
            }

            // 6. 窗口已打开但错过了（球已不在窗口位置）→ 重置练习
            if (putbackWindowOpened && !putbackAttempted && !putbackWindowLive)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Missed the rebound. Attack it earlier.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 7. 已尝试补篮但超时 → 重置练习
            if (putbackAttempted && activeTimer - putbackAttemptStartedAt >= PutbackRetryWindow)
            {
                BeginPutback(false);
                overlay.ShowFeedback("Good read. Try the putback again.", new Color32(0xFF, 0xB6, 0x6B, 0xFF), RetryFeedbackDuration);
                return;
            }

            // 8. 长时间没尝试补篮 → 重置练习
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
            // 1. 如果大招已触发，等待大招动画结束后完成步骤
            if (superTriggered)
            {
                var elapsed = activeTimer - superTriggeredAt;
                // 1a. 大招投篮已结束（不再是超级投篮状态）且过了最短延迟，或者超过兜底时间
                if ((!player.IsSuperShot && elapsed >= SuperCompletionDelay) ||
                    elapsed >= SuperFallbackCompletionDelay)
                {
                    CompleteStep("Super landed.");
                }

                return;
            }

            // 2. 超过 2.5 秒还没按大招 → 显示提示
            if (activeTimer >= 2.5f && !stepHintShown)
            {
                stepHintShown = true;
                overlay.ShowFeedback("Press N.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), HintFeedbackDuration);
            }

            // 3. 超过 4.4 秒还没按大招 → 重置练习
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
            // 1. 如果还没得分
            if (!freePlayReadyToEnd)
            {
                // 2. 超过提醒延迟时间 → 显示"投进一球完成教程"提示
                if (activeTimer >= FreePlayReminderDelay && !stepHintShown)
                {
                    stepHintShown = true;
                    overlay.ShowFeedback("Score one basket to finish.", new Color32(0xFF, 0xD1, 0x76, 0xFF), HintFeedbackDuration);
                }

                return;
            }

            // 3. 已得分，等待短暂延迟后进入结束画面
            if (activeTimer - freePlayScoredAt < FreePlayScoreOutroDelay)
            {
                return;
            }

            // 4. 延迟结束 → 显示教程完成的结束画面
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
            // 1. 根据刚完成的步骤，自动进入下一个练习（按教程顺序依次推进）
            switch (currentStep)
            {
                case TutorialStep.Move:
                    BeginDash(true);       // 移动 → 冲刺
                    break;
                case TutorialStep.Dash:
                    BeginShot(true);       // 冲刺 → 投篮
                    break;
                case TutorialStep.Shot:
                    BeginPump(true);       // 投篮 → 假动作
                    break;
                case TutorialStep.Pump:
                    BeginDunk(true);       // 假动作 → 扣篮
                    break;
                case TutorialStep.Dunk:
                    BeginSteal(true);      // 扣篮 → 抢断
                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);      // 抢断 → 盖帽
                    break;
                case TutorialStep.Block:
                    BeginPutback(true);    // 盖帽 → 补扣
                    break;
                case TutorialStep.Putback:
                    BeginSuper(true);      // 补扣 → 大招
                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);   // 大招 → 自由对战
                    break;
                default:
                    BeginOutro();          // 其他情况 → 结束画面
                    break;
            }
        }

        /// <summary>
        /// 检查覆盖层是否发送了指令（如跳过）。
        /// </summary>
        private bool HandleStepCommand()
        {
            // 1. 读取覆盖层上的按钮指令
            var command = overlay.ConsumeCommand();
            // 2. 没有指令，返回 false 继续正常流程
            if (command == mlpTutorialOverlayCommand.None)
            {
                return false;
            }

            // 3. 如果是"跳过"指令，跳到下一个练习步骤
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
            // 1. 玩家按下跳过按钮 → 直接进入下一个练习步骤（与正常顺序相同）
            switch (currentStep)
            {
                case TutorialStep.Opening:
                    BeginMove(true);       // 开场 → 移动
                    break;
                case TutorialStep.Move:
                    BeginDash(true);       // 移动 → 冲刺
                    break;
                case TutorialStep.Dash:
                    BeginShot(true);       // 冲刺 → 投篮
                    break;
                case TutorialStep.Shot:
                    BeginPump(true);       // 投篮 → 假动作
                    break;
                case TutorialStep.Pump:
                    BeginDunk(true);       // 假动作 → 扣篮
                    break;
                case TutorialStep.Dunk:
                    BeginSteal(true);      // 扣篮 → 抢断
                    break;
                case TutorialStep.Steal:
                    BeginBlock(true);      // 抢断 → 盖帽
                    break;
                case TutorialStep.Block:
                    BeginPutback(true);    // 盖帽 → 补扣
                    break;
                case TutorialStep.Putback:
                    BeginSuper(true);      // 补扣 → 大招
                    break;
                case TutorialStep.Super:
                    BeginFreePlay(true);   // 大招 → 自由对战
                    break;
                default:
                    BeginOutro();          // 其他情况 → 结束画面
                    break;
            }
        }

        /// <summary>
        /// 监听玩家动作（冲刺、投篮、扣篮、抢断、盖帽、得分等）。
        /// </summary>
        private void OnPlayerSignal(mlpPlayerSignalType signal, int side, int playerNo)
        {
            // 1. 如果玩家不存在或教程不在操作阶段，忽略所有信号
            if (player == null || phase != TutorialPhase.Active)
            {
                return;
            }

            // 2. 特殊处理：盖帽练习中对手得分 → 安排重试（玩家没盖住）
            if (currentStep == TutorialStep.Block &&
                signal == mlpPlayerSignalType.Score &&
                opponent != null &&
                side == opponent.Side &&
                playerNo == opponent.PlayerNo)
            {
                ScheduleBlockRetry("Try again. Jump on the release cue.");
                return;
            }

            // 3. 只处理玩家自己的信号（忽略对手的信号）
            if (side != player.Side || playerNo != player.PlayerNo)
            {
                return;
            }

            // 4. 根据信号类型处理
            switch (signal)
            {
                case mlpPlayerSignalType.Dash:
                    // 4a. 冲刺信号：在冲刺练习中，标记冲刺完成
                    if (currentStep == TutorialStep.Dash && !dashCompletionPending)
                    {
                        dashCompletionPending = true;
                        dashCompletedAt = activeTimer;
                        overlay.ShowFeedback("Good dash. Feel the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.JumpA:
                    // 4b. 起跳信号：在投篮练习中，更新提示为"在最高点出手"
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
                    // 4c. 投篮信号
                    if (currentStep == TutorialStep.Shot)
                    {
                        // 4c-i. 投篮练习：记录投篮时机，判断是否在最高点出手
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
                        // 4c-ii. 假动作练习：假动作后投篮，根据对手是否被骗给出不同反馈
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
                    // 4d. 假动作信号：在假动作练习中，玩家做了假动作 → 让对手被骗起跳，设置保底得分
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
                    // 4e. 扣篮信号：标记扣篮尝试
                    if (currentStep == TutorialStep.Dunk)
                    {
                        // 4e-i. 扣篮练习：记录扣篮尝试
                        dunkAttempted = true;
                        dunkAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Strong take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    else if (currentStep == TutorialStep.Putback)
                    {
                        // 4e-ii. 补扣练习：记录补扣尝试
                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("Putback take. Watch it finish.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.StealSuccess:
                    // 4f. 抢断成功信号：标记抢断成功，等待确认
                    if (currentStep == TutorialStep.Steal)
                    {
                        stealSuccessPending = true;
                        stealSuccessAt = activeTimer;
                        overlay.ShowFeedback("Nice steal.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Block:
                    // 4g. 盖帽成功信号：标记盖帽成功，取消重试，显示观察提示
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
                    // 4h. 补扣信号：标记补扣尝试
                    if (currentStep == TutorialStep.Putback)
                    {
                        putbackAttempted = true;
                        putbackAttemptStartedAt = activeTimer;
                        overlay.ShowFeedback("There it is. Finish the rebound.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Super:
                    // 4i. 大招信号：标记大招已触发
                    if (currentStep == TutorialStep.Super && !superTriggered)
                    {
                        superTriggered = true;
                        superTriggeredAt = activeTimer;
                        overlay.ShowFeedback("Skill live. Watch the burst.", new Color32(0x9A, 0xFF, 0xDD, 0xFF), ActionFeedbackDuration);
                    }
                    break;

                case mlpPlayerSignalType.Score:
                    // 4j. 得分信号：根据当前练习步骤完成对应练习
                    if (currentStep == TutorialStep.Shot && shotAttempted)
                    {
                        // 4j-i. 投篮练习得分 → 完成（区分 2 分/3 分）
                        shotScored = true;
                        CompleteStep(core.MatchProcessor.ThrowType == 0 ? "Good rhythm. 3PT." : "Good rhythm. 2PT.");
                    }
                    else if (currentStep == TutorialStep.Dunk && dunkAttempted)
                    {
                        // 4j-ii. 扣篮得分 → 完成
                        dunkScored = true;
                        CompleteStep("Dunk made.");
                    }
                    else if (currentStep == TutorialStep.Putback && putbackAttempted)
                    {
                        // 4j-iii. 补篮得分 → 完成
                        putbackScored = true;
                        CompleteStep("Putback made.");
                    }
                    else if (currentStep == TutorialStep.FreePlay)
                    {
                        // 4j-iv. 自由对战得分 → 标记准备结束
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
            // 1. 读取结束画面上的按钮指令
            var command = overlay.ConsumeCommand();
            // 2. 没有按钮被点击，直接退出
            if (command == mlpTutorialOverlayCommand.None)
            {
                return;
            }

            // 3. 根据点击的按钮，设置下一步要去的地方
            inventory.PendingTutorialNextAction = command switch
            {
                mlpTutorialOverlayCommand.ReturnToMenu => mlpTutorialNextAction.None,
                mlpTutorialOverlayCommand.ReplayTutorial => mlpTutorialNextAction.ReplayTutorial,
                mlpTutorialOverlayCommand.StartTraining => mlpTutorialNextAction.StartTraining,
                mlpTutorialOverlayCommand.StartQuickMatch => mlpTutorialNextAction.StartQuickMatch,
                _ => mlpTutorialNextAction.None
            };

            // 4. 请求返回主菜单（在菜单中会根据 PendingTutorialNextAction 跳转到对应页面）
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
