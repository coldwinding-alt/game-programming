// 游戏核心逻辑（比赛主循环）
// 管理一场篮球比赛的全部流程：创建球场和球员、控制计时器、处理得分、判断胜负、暂停和恢复、教程模式。每帧由 Update 驱动，是整个游戏运行的核心。
//
// 比赛生命周期方法链：
//
//   StartMatch()            ← 开始新比赛（重置状态、显示倒计时）
//     ↓ (比赛进行中，Update 每帧驱动)
//   BeginEndOfTime()        ← 时间到，播放终场蜂鸣
//     ↓
//   FinalizeEndMatch()      ← 判定胜负（平局→加时赛，否则显示结果）
//     ↓
//   ResolvePostMatchDelay() ← 赛后延迟结束后   第九层
//     ├─ 平局 → StartOvertime()   ← 加时赛（更短计时器、重新倒计时）
//     └─ 有胜负 → 显示结算界面
//     ↓
//   (等待玩家点击)
//     ├─ 锦标赛/冒险 → AdvanceFlowRequested = true  ← 推进流程
//     └─ 普通模式 → ReturnToMenuRequested = true     ← 回主菜单

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏核心逻辑（比赛主循环）：管理一场篮球比赛的全部流程——创建球场和球员、控制计时器、处理得分、判断胜负、暂停和恢复。每帧由 Update 驱动，是整个游戏运行的核心。
    /// </summary>
    public sealed class mlpGameCore
    {
        private readonly Transform root;                                              // 所有游戏对象（球场、球员、球）挂载的父级 Transform
        private readonly List<mlpPlayerObject> playersLeft = new List<mlpPlayerObject>();  // 左侧队伍的球员列表
        private readonly List<mlpPlayerObject> playersRight = new List<mlpPlayerObject>(); // 右侧队伍的球员列表
        private const float AdventureDoubleRimCycle = 12f;                            // 冒险模式"双倍篮筐"机制的完整周期（秒）
        private const float AdventureDoubleRimActiveTime = 6f;                        // 冒险模式"双倍篮筐"每周期内生效的时长（秒）
        private const float AdventureBloodMoonTimeScale = 1.14f;                      // 冒险模式"血月"机制的游戏速度倍率（加速 14%）
        private const float AdventureFogWindForce = 120f;
        private const float AdventureFogWindMinMultiplier = 0.7f;
        private const float AdventureFogWindMaxMultiplier = 1f;
        private const float AdventureFogWindPhaseOffset = 0.7f;
        private const float AdventureFogWindFrequency = 1.35f;
        private mlpArenaObject arena;                                                 // 球场背景对象
        private mlpBasketObject basketLeft;                                           // 左侧篮筐
        private mlpBasketObject basketRight;                                          // 右侧篮筐
        private mlpHudView hud;                                                       // HUD 界面（比分板、计时器、消息提示、暂停遮罩）
        private bool isPlaying;                                                       // 比赛是否正在进行中（倒计时结束后为 true）
        private bool isPaused;                                                        // 是否处于暂停状态
        private bool isTraining;                                                      // 是否为训练模式或教程模式（无计时器限制）
        private float matchTime;                                                      // 当前比赛已进行的时间（秒）
        private float endTime;                                                        // 比赛结束时间（秒），训练模式为 99999
        private float restartDelay;                                                   // 得分后重新开球前的等待时间（秒），归零时开球
        private int restartSide;                                                      // 得分后获得球权的一方（-1=左侧，1=右侧）
        private bool preMatchCountdown;                                               // 是否处于赛前 3-2-1 倒计时阶段
        private bool pauseResumeCountdown;                                            // 是否处于暂停恢复的 3-2-1 倒计时阶段
        private bool waitingForBallAfterBuzzer;                                       // 终场蜂鸣后是否正在等待飞行中的篮球落地
        private float postMatchDelay;                                                 // 赛后延迟计时器（秒），用于在显示结果前留出缓冲时间
        private int postMatchWinner;                                                  // 赛后判定的获胜方（-1=左侧，1=右侧，0=平局/未定）
        private bool overtimePending;                                                 // 是否需要进入加时赛（平局时为 true）
        private bool regularMatchTimeActive;                                          // 是否为正式时长比赛（非加时、非训练）
        private bool runtimeResourcesReleased;                                        // 运行时资源是否已释放（防止重复释放）
        private mlpTutorialFlow tutorialFlow;                                         // 教程流程控制器（非教程模式为 null）
        private mlpAdventureLevelDefinition adventureLevel;                           // 当前冒险关卡定义（非冒险模式为 null）
        private mlpAdventureMechanic lastAdventureCue = mlpAdventureMechanic.BasicDuel; // 上一帧生效的冒险机制（用于检测机制切换）
        private bool adventureCueWasActive;                                           // 上一帧冒险机制是否处于激活状态（用于检测激活/失效变化）

        public mlpBallObject Ball { get; private set; }
        public mlpMatchData MatchData => mlpInventory.Instance.MatchData;
        public mlpPlayerSignalBus PlayerSignals { get; } = new mlpPlayerSignalBus();
        public mlpMatchProcessor MatchProcessor { get; } = new mlpMatchProcessor();
        public bool ReturnToMenuRequested { get; private set; }
        public bool AdvanceFlowRequested { get; private set; }
        public bool IsSuperShot { get; set; }
        public bool IsAlleyOop { get; set; }
        public float RemainingMatchTime => Mathf.Max(0f, endTime - matchTime);
        public IReadOnlyList<mlpPlayerObject> PlayersLeft => playersLeft;
        public IReadOnlyList<mlpPlayerObject> PlayersRight => playersRight;
        public mlpBasketObject BasketLeft => basketLeft;
        public mlpBasketObject BasketRight => basketRight;
        public mlpTutorialFlow TutorialFlow => tutorialFlow;

        /// <summary>
        /// 创建游戏核心，指定所有游戏对象（球场、球员、球）挂载的父级 Transform。
        /// </summary>
        /// <param name="root">所有生成的游戏对象的父级 Transform。</param>
        public mlpGameCore(Transform root)
        {
            this.root = root;
        }

        /// <summary>
        /// 初始化球场、篮筐、球、球员和 HUD，然后开始第一场比赛。教程模式下会创建教程流程。
        /// </summary>
        public void Start()
        {
            // 1. 加载球员角色数据
            mlpPlayersData.SetupPlayers();

            // 2. 创建球场背景、左右篮筐、篮球
            arena = new mlpArenaObject(root);
            basketLeft = new mlpBasketObject(-1, root);
            basketRight = new mlpBasketObject(1, root);
            Ball = new mlpBallObject(this, root);

            // 3. 创建比分和计时器的 HUD 界面
            hud = new mlpHudView(root, MatchData);

            // 4. 教程模式下创建教程流程控制器
            if (mlpInventory.Instance.GameMode == mlpGameModeIds.Tutorial)
            {
                tutorialFlow = new mlpTutorialFlow(this);
            }

            // 5. 创建所有球员，开始第一场比赛，启动教程
            BuildPlayers();
            StartMatch(true);
            tutorialFlow?.Start();
        }

        /// <summary>
        /// 释放所有球员和教程流程的运行时资源。可安全地多次调用。
        /// </summary>
        public void Shutdown()
        {
            // 1. 如果已经释放过资源，直接返回，防止重复释放
            if (runtimeResourcesReleased)
            {
                return;
            }

            // 2. 关闭教程流程（如果有的话）
            tutorialFlow?.Shutdown();
            // 3. 标记资源已释放
            runtimeResourcesReleased = true;
            // 4. 遍历左侧队伍，释放每个球员的运行时资源
            foreach (var player in playersLeft)
            {
                player.ReleaseRuntimeResources();
            }

            // 5. 遍历右侧队伍，释放每个球员的运行时资源
            foreach (var player in playersRight)
            {
                player.ReleaseRuntimeResources();
            }
        }

        /// <summary>
        /// 主游戏循环每帧调用。处理暂停输入、倒计时、球物理、球员更新、碰撞检测、计时器倒数和比赛结束逻辑。
        /// 整体采用"状态机 + 提前返回"的结构：每一层判断当前所处的比赛阶段，如果条件命中就执行对应逻辑后立即 return，
        /// 把后续更深层的状态判断"短路"掉。这样每一帧只会命中一个阶段，不会出现逻辑重叠。
        /// 阶段优先级从高到低依次为：
        ///   帮助面板 → 暂停输入 → HUD/教程更新 → 返回菜单请求 → 恢复倒计时 → 暂停 → 快速测试同步 → 教程冻结 →
        ///   篮筐动画 → 赛后延迟 → 结算界面 → 赛前倒计时 → 得分后延迟 → 比赛进行中 → 计时器
        /// </summary>
        /// <param name="dt">自上一帧以来经过的时间（秒），由 Unity 的 Time.deltaTime 传入。</param>
        public void Update(float dt)
        {
            // ── 第 1 层：帮助面板（模态对话框） ──────────────────────────────
            // 帮助面板打开时游戏进入"冻结"状态：暂停所有比赛物理和输入，
            // 但 HUD（计时器动画、消息淡出）和教程流程仍需正常刷新。
            if (mlpHelpPanel.IsAnyOpen)
            {
                hud.Update(dt);
                tutorialFlow?.UpdateFrame(dt);
                return;
            }

            // ── 第 2 层：暂停按键检测 ──────────────────────────────────────
            // 玩家按下 P 或 Esc 时切换暂停状态。以下情况不允许触发暂停：
            //   - pauseResumeCountdown：正在从暂停恢复的 3-2-1 倒计时中，不能打断
            //   - postMatchDelay > 0：赛后延迟中（等待显示结果），暂停会干扰流程
            //   - hud.IsPostMatchVisible：结算界面已显示，暂停无意义
            if (!pauseResumeCountdown &&
                (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) &&
                postMatchDelay <= 0f &&
                !hud.IsPostMatchVisible)
            {
                HandlePauseCommand(mlpPauseCommand.Toggle);
            }

            // ── 第 3 层：HUD 和教程更新 ──────────────────────────────────
            // HUD 每帧刷新：消息文字淡出、计时器动画、按钮悬停状态等。
            // 教程流程每帧刷新：对话气泡显示、步骤推进等。
            // hud.ConsumePauseCommand() 检查 HUD 上的暂停按钮是否被点击，
            // 如果被点击则返回对应的暂停命令（Toggle/Resume/Menu）。
            hud.Update(dt);
            tutorialFlow?.UpdateFrame(dt);
            HandlePauseCommand(hud.ConsumePauseCommand());

            // HUD 更新后再次检查帮助面板——因为 HUD 的按钮点击可能刚打开了帮助面板
            if (mlpHelpPanel.IsAnyOpen)
            {
                return;
            }

            // 玩家在结算界面或暂停菜单中请求返回主菜单，由外层 mlpGameBootstrap 处理
            if (ReturnToMenuRequested)
            {
                return;
            }

            // ── 第 4 层：暂停恢复倒计时 ────────────────────────────────────
            // 从暂停恢复时先显示 3-2-1 倒计时，倒计时结束后才真正取消暂停。
            // 这样玩家有准备时间，不会在毫无预警的情况下突然恢复比赛。
            // 倒计时由 HUD 驱动（UpdateCountdown 返回 false 表示倒计时结束）。
            if (pauseResumeCountdown)
            {
                pauseResumeCountdown = hud.UpdateCountdown(dt);
                if (!pauseResumeCountdown)
                {
                    hud.EndResumeCountdown();
                    isPaused = false;
                    // 赛前倒计时阶段恢复时不吹哨（因为赛前本身就有倒计时）
                    if (!preMatchCountdown)
                    {
                        mlpAudio.Instance?.Play(mlpAssets.Sounds.MWhistle);
                    }
                }

                return;
            }

            // ── 第 5 层：暂停状态 ──────────────────────────────────────────
            // 暂停中跳过所有游戏物理和逻辑，画面完全冻结。
            if (isPaused)
            {
                return;
            }

            // 快速测试模式下可能动态修改比赛时长，这里同步并检查是否需要触发终场
            if (SyncQuickTestMatchTime())
            {
                return;
            }

            // ── 第 6 层：教程冻结 ──────────────────────────────────────────
            // 教程对话框打开期间（FreezeGameplay = true），球和球员都不动，
            // 让玩家专注于阅读操作指引。只有 HUD 和教程流程继续刷新。
            if (tutorialFlow != null && tutorialFlow.FreezeGameplay)
            {
                return;
            }

            // ── 第 7 层：计算实际游戏时间 ──────────────────────────────────
            // gameplayDt 是经过时间缩放后的真实游戏时间：
            //   - 教程模式下可通过 GameplayTimeScale 减速（如 0.5x），让新手看清动作
            //   - 冒险模式的"血月"机制会加速 14%（1.14x），增加紧张感
            // UpdateAdventureMechanics 处理冒险模式的特殊机制激活/失效逻辑
            var gameplayDt = tutorialFlow != null ? dt * tutorialFlow.GameplayTimeScale : dt;
            gameplayDt *= GetAdventureGameplayTimeScale();
            UpdateAdventureMechanics(gameplayDt);

            // ── 第 8 层：篮筐动画 ──────────────────────────────────────────
            // 篮筐有弹性晃动动画（进球后网兜摇摆），需要每帧独立更新。
            // 篮筐动画不受比赛阶段影响，在赛后延迟期间也能看到余韵。
            basketLeft.Update(gameplayDt);
            basketRight.Update(gameplayDt);

            // ── 第 9 层：赛后延迟 ──────────────────────────────────────────
            // 比赛时间到或分出胜负后，不立即显示结算界面，而是等待一段短暂延迟
            // （约 1.15~1.2 秒），让"TIME!!!"或"OVERTIME"消息有时间展示。
            // 延迟结束后 ResolvePostMatchDelay 决定：平局→进入加时赛，否则→显示结算。
            if (postMatchDelay > 0f)
            {
                postMatchDelay -= gameplayDt;
                if (postMatchDelay <= 0f)
                {
                    ResolvePostMatchDelay();
                }
                return;
            }

            // ── 第 10 层：结算界面交互 ─────────────────────────────────────
            // 结算界面显示比赛结果（比分、胜负）。玩家点击鼠标/按回车/空格后：
            //   - 锦标赛/冒险模式：设置 AdvanceFlowRequested，由外层推进到下一关/下一轮
            //   - 普通模式：设置 ReturnToMenuRequested，返回主菜单
            if (hud.IsPostMatchVisible)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    var inventory = mlpInventory.Instance;
                    if (inventory.IsTournamentActive || inventory.IsAdventureActive)
                    {
                        AdvanceFlowRequested = true;
                    }
                    else
                    {
                        ReturnToMenuRequested = true;
                    }
                }
                return;
            }

            // ── 第 11 层：赛前倒计时 ──────────────────────────────────────
            // 新比赛开始时显示 "TIP OFF IN 3...2...1..." 倒计时。
            // 倒计时期间球员可以移动但不能投篮（TickPreMatch 更新技能冷却等）。
            // 倒计时结束后设置 isPlaying = true，吹哨开球，正式进入比赛。
            if (preMatchCountdown)
            {
                foreach (var player in playersLeft)
                {
                    player.TickPreMatch(gameplayDt);
                }
                foreach (var player in playersRight)
                {
                    player.TickPreMatch(gameplayDt);
                }

                preMatchCountdown = hud.UpdateCountdown(gameplayDt);
                if (!preMatchCountdown)
                {
                    isPlaying = true;
                    mlpAudio.Instance?.Play(mlpAssets.Sounds.MWhistle);
                }
                return;
            }

            // ── 第 12 层：得分后延迟 ──────────────────────────────────────
            // 一方得分后等待约 1.15 秒再重新开球。这段时间让进球动画和得分消息展示。
            // 延迟结束后 RestartAfterScore 将球权交给被得分的一方，重新开始比赛。
            if (restartDelay > 0f)
            {
                restartDelay -= gameplayDt;
                if (restartDelay <= 0f)
                {
                    RestartAfterScore();
                }
                return;
            }

            // isPlaying 为 false 表示比赛尚未正式开始（例如刚创建但还没吹哨）
            if (!isPlaying)
            {
                return;
            }

            // ── 第 13 层：终场后等待球落地 ────────────────────────────────
            // 终场蜂鸣器响后，如果球还在空中飞行（投篮弧线中），不能立即判定胜负。
            // 因为球可能还在飞向篮筐，有可能在蜂鸣后命中——这种情况应该算分。
            // 所以继续更新球的物理和盖帽检测，等球落地或入篮后再 FinalizeEndMatch。
            if (waitingForBallAfterBuzzer)
            {
                ApplyAdventureBallWind(gameplayDt);
                Ball.Update(gameplayDt, basketLeft, basketRight);
                TryBlockBall();
                if (HasWaitingBallResolved())
                {
                    FinalizeEndMatch();
                }
                return;
            }

            // ── 第 14 层：正常比赛进行（核心物理循环） ────────────────────
            // 这是比赛的主循环，每帧按顺序执行以下步骤：
            //
            // 14a. 冒险模式风力效果：迷雾风机制会向飞行中的球施加水平风力，
            //      用正弦函数计算风向，让球的飞行轨迹产生不可预测的偏移。
            ApplyAdventureBallWind(gameplayDt);

            // 14b. 篮球物理更新：包括重力、抛物线飞行、反弹、入篮检测等。
            //      传入左右篮筐用于碰撞检测（判断球是否穿过篮筐）。
            Ball.Update(gameplayDt, basketLeft, basketRight);

            // 14c. 球员更新：每个球员的 AI 决策、移动、动画状态机、技能冷却等。
            //      先更新左队再更新右队，顺序对结果无影响（纯逻辑更新）。
            foreach (var player in playersLeft)
            {
                player.Update(gameplayDt);
            }
            foreach (var player in playersRight)
            {
                player.Update(gameplayDt);
            }

            // 14d. 碰撞检测三连：
            //      ResolvePlayerBlocking — 两名主力球员物理重叠时推开（基于质量比例），
            //                             同时中断冲撞到防守球员的冲刺。
            //      TryBlockBall — 检查是否有球员能盖帽（球在可盖帽状态且球员在范围内）。
            //      TryPickupLooseBall — 检查是否有球员能捡起无人控制的球（最近的球员获得球权）。
            ResolvePlayerBlocking();
            TryBlockBall();
            TryPickupLooseBall();

            // 14e. 教程后处理：教程流程在游戏物理之后更新，用于检测玩家是否完成了操作目标。
            tutorialFlow?.UpdateAfterGameplay(dt);

            // ── 第 15 层：比赛计时器 ──────────────────────────────────────
            // 训练模式和超级投篮（SuperShot）不倒计时——训练无时间限制，超级投篮有独立计时。
            // 每帧累加比赛时间，更新 HUD 上的倒计时显示，时间到则触发终场流程
            // （BeginEndOfTime 会播放蜂鸣音，并处理球在空中的延迟判定）。
            if (!isTraining && !IsSuperShot)
            {
                matchTime += gameplayDt;
                hud.UpdateTimer(Mathf.Max(0f, endTime - matchTime));
                if (matchTime >= endTime)
                {
                    BeginEndOfTime();
                }
            }
        }

        /// <summary>
        /// 当球穿过篮筐时调用。计算得分、更新比分、在 HUD 上显示消息，并安排对方球队的重新开球。
        /// 这是得分的核心处理函数，由篮球碰撞检测模块在判定球入篮后回调。
        /// 注意 scoringSide 是"被得分方"的半场，不是得分方——球穿过左侧篮筐意味着右侧得分。
        /// </summary>
        /// <param name="scoringSide">被得分方的半场（-1 = 左侧篮筐被投进，1 = 右侧篮筐被投进）。</param>
        public void OnBallScored(int scoringSide)
        {
            // ── 第 1 步：防重复触发 ──────────────────────────────────────
            // restartDelay > 0 说明上一次得分后的延迟还没结束（约 1.15 秒）。
            // 这期间如果球又弹进了篮筐（比如球在篮筐边缘弹跳多次），不应该重复计分。
            if (restartDelay > 0f)
            {
                return;
            }

            // ── 第 2 步：计算基础得分 ────────────────────────────────────
            // teamIndex：得分方在数组中的索引（左队=0，右队=1）。
            // fallbackPoints：根据投篮位置判断是 2 分还是 3 分球。
            //   - IsThreePointer 检查投篮时球是否在三分线以外
            //   - 这只是"默认值"，后续可能被技能或冒险模式修改
            // ResolvePointsForScore：MatchProcessor 可能有额外的得分规则（如特殊投篮加成）
            // ResolveAdventureScoreModifier：冒险模式的特殊机制可能修改分数
            //   - "双倍篮筐"机制：分数翻倍
            //   - "丰收时刻"机制：分数 +1
            //   注意 ref points——直接修改原始变量，函数返回提示文字（如 "DOUBLE RIM 4!"）
            var teamIndex = scoringSide == -1 ? 0 : 1;
            var fallbackPoints = IsThreePointer(scoringSide) ? 3 : 2;
            var points = MatchProcessor.ResolvePointsForScore(scoringSide, fallbackPoints);
            var adventureScoreNotice = ResolveAdventureScoreModifier(ref points);

            // ── 第 3 步：查找投篮球员并应用其个人技能加成 ────────────────
            // MatchProcessor 记录了最后一次投篮的球员信息（哪一队、几号球员）。
            // 通过这些信息找到具体的球员对象，让他应用自己的得分技能。
            // 例如某些角色的"狂欢大奖"技能可能随机将得分翻倍。
            // ResolveScorePoints 返回修改后的分数，同时通过 out 参数输出技能提示文字。
            var scoringPlayer = FindPlayerBySideAndPlayerNo(MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            string scoreNotice = null;
            if (scoringPlayer != null)
            {
                points = scoringPlayer.ResolveScorePoints(points, out scoreNotice);
            }

            // ── 第 4 步：更新比分并同步到 HUD ────────────────────────────
            // 累加得分方的比分，然后刷新 HUD 上两边的比分显示。
            // Dispatch 发送得分信号——其他球员的 AI 可能会据此调整行为
            // （比如队友庆祝、对手沮丧等动画触发）。
            // HideCountdown 隐藏可能正在显示的倒计时（比如赛前倒计时被打断的情况）。
            MatchData.MatchScore[teamIndex] += points;
            hud.UpdateScore(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            if (MatchProcessor.ShotPlayerNo >= 0)
            {
                PlayerSignals.Dispatch(mlpPlayerSignalType.Score, MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            }
            hud.HideCountdown();

            // ── 第 5 步：在 HUD 上显示得分消息 ──────────────────────────
            // waitingForBallAfterBuzzer 为 true 表示这是终场蜂鸣后球才进的——
            // 这种情况下不显示得分消息，因为 "TIME!!!" 消息已经占位了。
            // 消息优先级：球员技能提示 > 冒险模式提示 > 高分通用提示 > 普通得分
            if (!waitingForBallAfterBuzzer)
            {
                if (!string.IsNullOrEmpty(scoreNotice))
                {
                    hud.ShowMessage($"{scoreNotice} {points}!", 1.28f, false);
                }
                else if (!string.IsNullOrEmpty(adventureScoreNotice))
                {
                    hud.ShowMessage(adventureScoreNotice, 1.28f, false);
                }
                else if (points >= 4)
                {
                    hud.ShowMessage($"{points} POINT", 1.2f, false);
                }
                else
                {
                    hud.ShowMessage(points == 3 ? "3 POINT" : "BASKET", 1.2f, false);
                }
            }

            // ── 第 6 步：播放篮筐和音效 ─────────────────────────────────
            // HitNet 让"被得分方"的篮筐播放网兜晃动动画。
            // 注意：球进了左侧篮筐（scoringSide==-1）→ 右侧篮筐晃动？
            // 不对——scoringSide 是被得分方的半场，球穿过的是该半场的篮筐，
            // 所以是该半场的篮筐播放网兜动画。逻辑上是 basketRight.HitNet() 对应右侧篮筐。
            // BBasket 是进球音效。
            if (scoringSide == -1)
            {
                basketRight.HitNet();
            }
            else
            {
                basketLeft.HitNet();
            }

            mlpAudio.Instance?.Play(mlpAssets.Sounds.BBasket);
            scoringPlayer?.OnScoreConfirmed();

            // ── 第 7 步：终场得分特殊处理 ────────────────────────────────
            // 如果是终场蜂鸣后球才进的，不需要安排重新开球（比赛已经结束了）。
            // 直接把 restartDelay 设为 0 并返回，让 Update 中的
            // waitingForBallAfterBuzzer 逻辑去调用 FinalizeEndMatch 判定胜负。
            if (waitingForBallAfterBuzzer)
            {
                restartDelay = 0f;
                return;
            }

            // ── 第 8 步：安排重新开球 ────────────────────────────────────
            // restartSide = -scoringSide：球权交给被得分的一方（对方进球后我方开球）。
            // restartDelay = 1.15 秒：进球后等待 1.15 秒再开球，
            //   这段时间让进球动画和得分消息展示完，Update 中会倒计时，
            //   归零后调用 RestartAfterScore 执行实际的开球操作。
            restartSide = -scoringSide;
            restartDelay = 1.15f;
        }

        /// <summary>
        /// 在双方队伍中搜索当前持球的球员。
        /// </summary>
        /// <param name="side">0 = 搜索双方，-1 = 仅搜索左侧队伍，1 = 仅搜索右侧队伍。</param>
        /// <returns>持球的球员，无人持球时返回 null。</returns>
        public mlpPlayerObject FindBallHolder(int side = 0)
        {
            foreach (var player in playersLeft)
            {
                if (player.WithBall && (side == 0 || player.Side == side))
                {
                    return player;
                }
            }

            foreach (var player in playersRight)
            {
                if (player.WithBall && (side == 0 || player.Side == side))
                {
                    return player;
                }
            }

            return null;
        }

        public mlpPlayerObject FindPlayerBySideAndPlayerNo(int side, int playerNo)
        {
            var team = side == -1 ? playersLeft : playersRight;
            for (var i = 0; i < team.Count; i++)
            {
                if (team[i] != null && team[i].PlayerNo == playerNo)
                {
                    return team[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 查找与给定球员直线距离最近的对手球员。
        /// </summary>
        /// <param name="source">用来测量距离的球员。</param>
        /// <returns>最近的对手，不存在对手时返回 null。</returns>
        public mlpPlayerObject FindClosestOpponent(mlpPlayerObject source)
        {
            // 1. 如果传入的球员为空，直接返回 null
            if (source == null)
            {
                return null;
            }

            // 2. 根据当前球员所在队伍，选择对手队伍列表
            var opponents = source.Side == -1 ? playersRight : playersLeft;
            // 3. 遍历对手队伍，用平方距离找最近的对手
            mlpPlayerObject closest = null;
            var bestDistance = float.MaxValue;
            foreach (var opponent in opponents)
            {
                var distance = (opponent.Position - source.Position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    closest = opponent;
                }
            }

            // 4. 返回最近的对手（如果没有对手则返回 null）
            return closest;
        }

        /// <summary>
        /// 通知所有球员当前哪位队友持球，以便他们调整 AI 行为。
        /// </summary>
        /// <param name="holderSide">持球球员所在队伍。</param>
        /// <param name="holderPlayerNo">持球球员在队伍中的索引。</param>
        public void NotifyBallInHands(int holderSide, int holderPlayerNo)
        {
            foreach (var player in playersLeft)
            {
                player.NotifyBallInHands(holderSide, holderPlayerNo);
            }

            foreach (var player in playersRight)
            {
                player.NotifyBallInHands(holderSide, holderPlayerNo);
            }
        }

        /// <summary>
        /// 通知所有球员有人投篮了，AI 球员可以做出反应（如移动到篮板位置）。
        /// </summary>
        /// <param name="shotSide">投篮球员所在队伍。</param>
        /// <param name="shooterPlayerNo">投篮球员在队伍中的索引。</param>
        public void NotifyPlayersBallShot(int shotSide, int shooterPlayerNo)
        {
            foreach (var player in playersLeft)
            {
                player.NotifyBallShot(shotSide, shooterPlayerNo);
            }

            foreach (var player in playersRight)
            {
                player.NotifyBallShot(shotSide, shooterPlayerNo);
            }
        }

        /// <summary>
        /// 通知所有球员球处于无人控制状态。
        /// </summary>
        public void NotifyBallOthers()
        {
            foreach (var player in playersLeft)
            {
                player.NotifyBallOthers();
            }

            foreach (var player in playersRight)
            {
                player.NotifyBallOthers();
            }
        }

        /// <summary>
        /// 返回同队的另一名球员。在 1v1 模式下始终返回 null，因为每队只有一名球员。
        /// </summary>
        /// <param name="side">所在队伍（-1 = 左侧，1 = 右侧）。</param>
        /// <param name="playerNo">查找队友的球员索引。</param>
        /// <returns>队友球员，不存在时返回 null。</returns>
        public mlpPlayerObject GetTeamMate(int side, int playerNo)
        {
            var team = side == -1 ? playersLeft : playersRight;
            for (var i = 0; i < team.Count; i++)
            {
                if (team[i].PlayerNo != playerNo)
                {
                    return team[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 返回指定半场队伍的当前比分。
        /// </summary>
        /// <param name="side">所在队伍（-1 = 左侧，1 = 右侧）。</param>
        /// <returns>该队伍的当前比分，数据缺失时返回 0。</returns>
        public int GetScoreForSide(int side)
        {
            var teamIndex = side == -1 ? 0 : 1;
            return MatchData.MatchScore != null && MatchData.MatchScore.Length > teamIndex
                ? MatchData.MatchScore[teamIndex]
                : 0;
        }

        /// <summary>
        /// 返回指定队伍领先（正数）或落后（负数）的分数。
        /// </summary>
        /// <param name="side">所在队伍（-1 = 左侧，1 = 右侧）。</param>
        /// <returns>分差（该队得分减去对手得分）。</returns>
        public int GetScoreLeadForSide(int side)
        {
            return GetScoreForSide(side) - GetScoreForSide(-side);
        }

        /// <summary>
        /// 检查球的最后一次投篮位置是否在三分线以外。
        /// </summary>
        /// <param name="shotSide">投篮方所在半场（-1 = 左侧，1 = 右侧）。</param>
        /// <returns>该投篮为三分球时返回 true。</returns>
        public bool IsCurrentShotThreePointer(int shotSide)
        {
            return Ball != null && IsThreePointer(shotSide);
        }

        /// <summary>
        /// 在 HUD 上显示一条文本消息，持续指定时长。
        /// </summary>
        /// <param name="message">要显示的文本。</param>
        /// <param name="duration">消息显示的持续时间（秒）。</param>
        public void ShowHudMessage(string message, float duration = 1.2f)
        {
            hud?.ShowMessage(message, duration);
        }

        /// <summary>
        /// 在 HUD 上显示一条较小的加成提示（用于得分加成和特殊事件）。
        /// </summary>
        /// <param name="message">要显示的加成文本。</param>
        /// <param name="duration">提示显示的持续时间（秒）。</param>
        public void ShowHudBonusNotice(string message, float duration = 0.9f)
        {
            hud?.ShowBonusNotice(message, duration);
        }

        /// <summary>
        /// 取消暂停并标记玩家希望返回主菜单。
        /// </summary>
        public void RequestReturnToMenu()
        {
            SetPaused(false);
            ReturnToMenuRequested = true;
        }

        /// <summary>
        /// 为教程练习场景重置场景状态。放置双方球员，可选择将球交给其中一方，如果无人持球则将球放在中间。
        /// </summary>
        /// <param name="playerPosition">玩家角色在球场上的位置。</param>
        /// <param name="opponentPosition">对手角色在球场上的位置。</param>
        /// <param name="givePlayerBall">是否在开始时将球交给玩家。</param>
        /// <param name="giveOpponentBall">是否在开始时将球交给对手。</param>
        /// <param name="playerFacing">玩家朝向（-1 = 左，1 = 右）。</param>
        /// <param name="opponentFacing">对手朝向。</param>
        public void TutorialResetScenario(
            Vector2 playerPosition,
            Vector2 opponentPosition,
            bool givePlayerBall,
            bool giveOpponentBall,
            float playerFacing,
            float opponentFacing)
        {
            // 1. 重置比赛处理器和特殊投篮标记
            MatchProcessor.Reset();
            IsSuperShot = false;
            IsAlleyOop = false;
            restartDelay = 0f;
            waitingForBallAfterBuzzer = false;

            // 2. 重置篮球和所有球员到初始状态
            Ball.Restart();
            foreach (var leftPlayer in playersLeft)
            {
                leftPlayer.Restart(0);
            }

            foreach (var rightPlayer in playersRight)
            {
                rightPlayer.Restart(0);
            }

            // 3. 如果缺少球员则提前返回
            if (playersLeft.Count == 0 || playersRight.Count == 0)
            {
                return;
            }

            // 4. 将玩家和对手分别移动到指定位置和朝向
            var left = playersLeft[0];
            var right = playersRight[0];
            left.TutorialSnapTo(playerPosition, playerFacing);
            right.TutorialSnapTo(opponentPosition, opponentFacing);

            // 5. 根据参数决定谁持球，或者把球放在两人中间
            if (givePlayerBall)
            {
                left.TakeBallInHands();
            }
            else if (giveOpponentBall)
            {
                right.TakeBallInHands();
            }
            else
            {
                Ball.TutorialSnapTo(new Vector2((playerPosition.x + opponentPosition.x) * 0.5f, mlpObjectsData.BallIndentYCenter));
                left.NotifyBallLoose();
                right.NotifyBallLoose();
            }
        }

        /// <summary>
        /// 尝试从对手手中抢断。检查对方队伍中所有在抢断范围内的对手，并从最近的对手处抢球。
        /// </summary>
        /// <param name="thief">尝试抢断的球员。</param>
        /// <param name="facingDirection">抢断者朝向（-1 = 左，1 = 右）。</param>
        /// <returns>已发起抢断尝试时返回 true（无论是否实际抢到球）。</returns>
        public bool TryStealBall(mlpPlayerObject thief, float facingDirection)
        {
            // 1. 前置检查：球权交接中、赛前倒计时、比赛未开始或超级投篮时不能抢断
            if (thief == null || restartDelay > 0f || preMatchCountdown || !isPlaying || IsSuperShot)
            {
                return false;
            }

            // 2. 确定对手队伍，计算抢断距离（基础距离 + 球员加成）
            var opponents = thief.Side == -1 ? playersRight : playersLeft;
            var stealDistance = mlpObjectsData.StealDistance + thief.GetStealDistanceBonus();
            // 3. 遍历对手，找到最近的可抢断目标
            mlpPlayerObject target = null;
            var bestDistance = float.MaxValue;
            foreach (var opponent in opponents)
            {
                var candidateDistance = opponent.CheckToBeStolen(thief.Position.x, facingDirection, stealDistance);
                if (candidateDistance >= 0f && candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    target = opponent;
                }
            }

            // 4. 没有可抢断的目标，返回失败
            if (target == null)
            {
                return false;
            }

            // 5. 对目标执行抢断，成功时发送信号并播放音效
            var stoleBall = target.GetBeStolen(thief.Position.x);
            if (stoleBall)
            {
                PlayerSignals.Dispatch(mlpPlayerSignalType.StealSuccess, thief.Side, thief.PlayerNo);
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel);
            }

            return true;
        }

        /// <summary>
        /// 检查两名主力球员是否发生物理重叠，并将他们推开。同时中断冲撞到防守球员的冲刺。
        /// </summary>
        private void ResolvePlayerBlocking()
        {
            // 1. 前置检查：需要双方都有球员，且不在超级投篮状态
            if (playersLeft.Count == 0 || playersRight.Count == 0 || IsSuperShot)
            {
                return;
            }

            // 2. 获取双方主力球员，确认都支持地面碰撞
            var left = playersLeft[0];
            var right = playersRight[0];
            if (left == null || right == null || !left.CanResolveGroundBlock || !right.CanResolveGroundBlock)
            {
                return;
            }

            // 3. 至少一方需要有碰撞体
            if (!left.HasGroundBlockBody && !right.HasGroundBlockBody)
            {
                return;
            }

            // 4. 计算 X 轴重叠量，没有重叠则跳过
            var deltaX = right.Position.x - left.Position.x;
            var overlapX = mlpObjectsData.BlockWidth - Mathf.Abs(deltaX);
            if (overlapX <= 0f)
            {
                return;
            }

            // 5. 计算 Y 轴重叠量，没有重叠则跳过
            var overlapY = mlpObjectsData.BlockHeight - Mathf.Abs(right.Position.y - left.Position.y);
            if (overlapY <= 0f)
            {
                return;
            }

            // 6. 检查是否有一方正在向对方移动，都不动则跳过
            var leftApproaching = left.IsMovingToward(right);
            var rightApproaching = right.IsMovingToward(left);
            if (!leftApproaching && !rightApproaching)
            {
                return;
            }

            // 7. 根据双方质量比例，将两人推开（质量大的移动少）
            var leftMass = left.GetCollisionMass();
            var rightMass = right.GetCollisionMass();
            var totalMass = Mathf.Max(0.001f, leftMass + rightMass);
            var separationSign = deltaX >= 0f ? 1f : -1f;
            left.ApplyHorizontalSeparation(-overlapX * (rightMass / totalMass) * separationSign);
            right.ApplyHorizontalSeparation(overlapX * (leftMass / totalMass) * separationSign);

            // 8. 如果一方正在冲刺并撞上有碰撞体的对方，中断冲刺
            if (left.HasGroundBlockBody && right.IsDashingInto(left))
            {
                right.InterruptDashByBlock();
            }

            if (right.HasGroundBlockBody && left.IsDashingInto(right))
            {
                left.InterruptDashByBlock();
            }
        }

        /// <summary>
        /// 检查球是否从三分线以外投出（相对于得分方的篮筐）。
        /// </summary>
        /// <param name="scoringSide">被得分方的篮筐所在半场。</param>
        /// <returns>该投篮为三分球时返回 true。</returns>
        private bool IsThreePointer(int scoringSide)
        {
            if (scoringSide == -1)
            {
                return Ball.LastShotX < mlpConstants.Width - mlpObjectsData.ThreePointsDistance;
            }

            return Ball.LastShotX > mlpObjectsData.ThreePointsDistance;
        }

        /// <summary>
        /// 根据当前比赛数据（角色 ID、脑字符串、技能等级）为双方队伍创建球员对象。训练模式只创建左侧队伍。
        /// </summary>
        private void BuildPlayers()
        {
            // 1. 读取比赛数据，确定每队人数（训练模式只有 1 队）
            var match = MatchData;
            const int playersPerTeam = 1;
            var teamCount = mlpInventory.Instance.GameMode == mlpGameModeIds.Training ? 1 : 2;

            // 2. 为每队创建球员：读取脑控制器标识（P=人类，B=AI，T=教程）和技能等级
            for (var teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                for (var playerNo = 0; playerNo < playersPerTeam; playerNo++)
                {
                    var brain = match.Pb[teamIndex].Length > playerNo ? match.Pb[teamIndex][playerNo] : (teamIndex == 0 ? "P0" : "B0");
                    var skill = match.Skills[teamIndex].Length > playerNo ? match.Skills[teamIndex][playerNo] : 0;

                    // 3. 创建球员对象并加入对应的队伍列表
                    var player = new mlpPlayerObject(
                        this,
                        teamIndex,
                        match.CharacterIds[teamIndex],
                        playerNo,
                        brain,
                        skill,
                        root);

                    if (teamIndex == 0)
                    {
                        playersLeft.Add(player);
                    }
                    else
                    {
                        playersRight.Add(player);
                    }
                }
            }
        }

        /// <summary>
        /// 为新比赛重置所有状态：比分、计时器、球位置、球员位置和 HUD 状态。非训练模式下显示赛前倒计时。
        /// </summary>
        /// <param name="regularTime">true 表示正常时长比赛，false 表示加时时长。</param>
        private void StartMatch(bool regularTime)
        {
            // 1. 读取游戏模式和冒险关卡信息
            var inventory = mlpInventory.Instance;
            var gameMode = inventory.GameMode;
            adventureLevel = inventory.IsAdventureActive ? inventory.Adventure.CurrentLevel : null;
            lastAdventureCue = mlpAdventureMechanic.BasicDuel;
            adventureCueWasActive = false;

            // 2. 设置比赛状态：训练/教程模式直接开始，其他模式显示倒计时
            isTraining = gameMode == mlpGameModeIds.Training || gameMode == mlpGameModeIds.Tutorial;
            isPlaying = isTraining;
            isPaused = false;
            matchTime = 0f;
            regularMatchTimeActive = regularTime && !isTraining;
            endTime = isTraining ? 99999f : mlpQuickTestSettings.GetMatchTime(regularTime);

            // 3. 重置比分和 HUD 显示
            MatchData.ResetScore();
            hud.UpdateScore(0, 0);
            hud.SetTimerVisible(!isTraining);
            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
            hud.HidePauseOverlay();

            // 4. 清除所有比赛中间状态
            postMatchDelay = 0f;
            postMatchWinner = 0;
            overtimePending = false;
            waitingForBallAfterBuzzer = false;
            IsSuperShot = false;
            IsAlleyOop = false;
            ReturnToMenuRequested = false;
            AdvanceFlowRequested = false;
            MatchProcessor.Reset();

            // 5. 重置所有球员位置，开始赛前倒计时
            Restart(0);
            preMatchCountdown = !isTraining;
            pauseResumeCountdown = false;
            if (preMatchCountdown)
            {
                hud.StartCountdown(3f, "TIP OFF IN");
                if (adventureLevel != null)
                {
                    hud.ShowMessage(adventureLevel.MechanicTitle, 1.25f, false);
                }
            }
            else
            {
                hud.ShowMessage(gameMode == mlpGameModeIds.Tutorial ? "TUTORIAL" : "TRAINING", 0.85f);
                mlpAudio.Instance?.Play(mlpAssets.Sounds.MWhistle);
            }
        }

        /// <summary>
        /// 处理暂停命令：切换暂停、带倒计时恢复、或返回主菜单。
        /// </summary>
        /// <param name="command">要执行的暂停操作（None、Toggle、Resume 或 Menu）。</param>
        private void HandlePauseCommand(mlpPauseCommand command)
        {
            // 1. 前置检查：无命令、正在倒计时、赛后延迟中或结算界面时忽略暂停操作
            if (command == mlpPauseCommand.None || pauseResumeCountdown || postMatchDelay > 0f || hud.IsPostMatchVisible)
            {
                return;
            }

            // 2. 根据命令类型执行对应的暂停操作
            switch (command)
            {
                case mlpPauseCommand.Toggle:
                    // 3. 切换暂停：已暂停则恢复，未暂停则暂停
                    if (isPaused)
                    {
                        BeginPauseResumeCountdown();
                    }
                    else
                    {
                        SetPaused(true);
                    }
                    break;
                case mlpPauseCommand.Resume:
                    // 4. 恢复：开始恢复倒计时
                    BeginPauseResumeCountdown();
                    break;
                case mlpPauseCommand.Menu:
                    // 5. 返回菜单：取消暂停并标记返回请求
                    SetPaused(false);
                    ReturnToMenuRequested = true;
                    break;
            }
        }

        /// <summary>
        /// 在游戏从暂停恢复前开始 3-2-1 倒计时。
        /// </summary>
        private void BeginPauseResumeCountdown()
        {
            if (!isPaused || pauseResumeCountdown)
            {
                return;
            }

            pauseResumeCountdown = true;
            hud.BeginResumeCountdown(3f);
        }

        /// <summary>
        /// 设置或清除暂停状态，并相应地显示/隐藏暂停覆盖层。
        /// </summary>
        /// <param name="paused">true 暂停，false 取消暂停。</param>
        private void SetPaused(bool paused)
        {
            // 1. 如果状态没有变化，只同步显示/隐藏暂停遮罩
            if (isPaused == paused)
            {
                if (paused)
                {
                    hud.ShowPauseOverlay();
                }
                else
                {
                    hud.HidePauseOverlay();
                }

                return;
            }

            // 2. 更新暂停状态
            isPaused = paused;
            if (paused)
            {
                // 3. 暂停：清除恢复倒计时，显示暂停遮罩
                pauseResumeCountdown = false;
                hud.ShowPauseOverlay();
            }
            else
            {
                // 4. 取消暂停：清除倒计时，隐藏暂停遮罩和恢复倒计时
                pauseResumeCountdown = false;
                hud.HidePauseOverlay();
                hud.EndResumeCountdown();
            }
        }

        /// <summary>
        /// 为新一轮进攻重置球和球员位置。训练模式下将球交给左侧球员，否则交给指定的一方（如果非零）。
        /// </summary>
        /// <param name="side">哪一方获得球权（-1 = 左侧，1 = 右侧，0 = 无人）。</param>
        private void Restart(int side)
        {
            // 1. 重置篮球和比赛处理器状态
            Ball.Restart();
            IsSuperShot = false;
            IsAlleyOop = false;
            MatchProcessor.Reset();
            // 2. 重置所有球员位置和状态
            foreach (var player in playersLeft)
            {
                player.Restart(side);
            }

            foreach (var player in playersRight)
            {
                player.Restart(side);
            }

            // 3. 分配球权：训练模式给左侧，正式比赛给指定一方
            if (isTraining)
            {
                Ball.TakeInHands(-1);
                playersLeft[0].TakeBallInHands();
            }
            else if (side != 0)
            {
                var receiver = side == -1 ? playersLeft[0] : playersRight[0];
                Ball.TakeInHands(side);
                receiver.TakeBallInHands();
            }
        }

        /// <summary>
        /// 得分后重新开始比赛。将球权交给被得分的一方。
        /// </summary>
        private void RestartAfterScore()
        {
            // 1. 重置比赛处理器
            MatchProcessor.Reset();
            // 2. 通知所有球员球已脱离控制
            foreach (var player in playersLeft)
            {
                player.NotifyBallLoose();
            }

            foreach (var player in playersRight)
            {
                player.NotifyBallLoose();
            }

            // 3. 以得分对方的球权重新开球
            Restart(restartSide);
        }

        /// <summary>
        /// 处理比赛时间结束时的蜂鸣器。如果球仍在飞行中，等待球落地后再决定获胜方。
        /// </summary>
        private void BeginEndOfTime()
        {
            // 1. 锁定比赛时间，播放终场蜂鸣音
            matchTime = endTime;
            restartDelay = 0f;
            preMatchCountdown = false;
            waitingForBallAfterBuzzer = false;
            mlpAudio.Instance?.Play(mlpAssets.Sounds.MBuzzer);
            hud.HideCountdown();
            hud.UpdateTimer(0f);

            // 2. 如果篮球还在空中飞行，等它落地后再判定胜负
            if (IsBallInGame())
            {
                waitingForBallAfterBuzzer = true;
                return;
            }

            // 3. 球已静止，直接判定比赛结果
            FinalizeEndMatch();
        }

        /// <summary>
        /// 确定获胜方（平局则进入加时赛），并显示相应的比赛结束消息。
        /// </summary>
        private void FinalizeEndMatch()
        {
            // 1. 停止比赛，判定胜负方
            isPlaying = false;
            waitingForBallAfterBuzzer = false;
            var winner = MatchData.WhoWins();

            // 2. 平局则进入加时赛，否则显示 "TIME!!!" 并延迟后显示结果
            if (winner == 0)
            {
                overtimePending = true;
                postMatchWinner = 0;
                postMatchDelay = 1.15f;
                hud.ShowMessage("OVERTIME", 1.05f);
                return;
            }

            overtimePending = false;
            postMatchWinner = winner;
            postMatchDelay = 1.2f;
            hud.ShowMessage("TIME!!!", 1.05f);
        }

        /// <summary>
        /// 赛后延迟结束后调用。进入加时赛或显示最终结果界面。
        /// </summary>
        private void ResolvePostMatchDelay()
        {
            // 1. 清除赛后延迟计时器
            postMatchDelay = 0f;
            // 2. 如果需要加时赛，启动加时赛
            if (overtimePending)
            {
                overtimePending = false;
                StartOvertime();
                return;
            }

            // 3. 有获胜方时，显示赛后结算界面
            if (postMatchWinner != 0)
            {
                hud.ShowPostMatch(postMatchWinner, MatchData.MatchScore[0], MatchData.MatchScore[1]);
            }
        }

        /// <summary>
        /// 为加时赛重置比赛，使用更短的计时器和新的赛前倒计时。
        /// </summary>
        private void StartOvertime()
        {
            // 1. 重置计时器为加时时长
            matchTime = 0f;
            endTime = mlpConstants.OvertimeTime;
            regularMatchTimeActive = false;
            waitingForBallAfterBuzzer = false;
            // 2. 清理 HUD 显示
            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
            // 3. 重置球员和球位置，开始赛前倒计时
            Restart(0);
            preMatchCountdown = true;
            pauseResumeCountdown = false;
            hud.StartCountdown(3f, "OVERTIME IN");
        }

        private float GetAdventureGameplayTimeScale()
        {
            if (adventureLevel == null || !isPlaying)
            {
                return 1f;
            }

            var mechanic = GetActiveAdventureMechanic();
            return mechanic == mlpAdventureMechanic.BloodMoon && IsAdventureMechanicCurrentlyActive(mechanic)
                ? AdventureBloodMoonTimeScale
                : 1f;
        }

        private bool SyncQuickTestMatchTime()
        {
            // 1. 非正式比赛或训练模式下不需要同步
            if (!regularMatchTimeActive || isTraining)
            {
                return false;
            }

            // 2. 如果快速测试设置中的比赛时长发生变化，同步更新
            var targetEndTime = mlpQuickTestSettings.GetMatchTime(true);
            if (!Mathf.Approximately(endTime, targetEndTime))
            {
                endTime = targetEndTime;
                hud.UpdateTimer(Mathf.Max(0f, endTime - matchTime));
            }

            // 3. 比赛未在进行中、倒计时中、等球落地或时间未到，不需要触发终场
            if (!isPlaying || preMatchCountdown || waitingForBallAfterBuzzer || matchTime < endTime)
            {
                return false;
            }

            // 4. 时间到了，触发终场
            BeginEndOfTime();
            return true;
        }

        private void UpdateAdventureMechanics(float dt)
        {
            // 1. 非冒险关卡、训练中或未比赛时跳过
            if (adventureLevel == null || isTraining || !isPlaying)
            {
                arena?.UpdateFogWindFx(false, 0f, 0f);
                return;
            }

            // 2. 获取当前生效的冒险机制，检查是否处于激活状态
            var mechanic = GetActiveAdventureMechanic();
            var active = IsAdventureMechanicCurrentlyActive(mechanic);
            // 3. 机制刚激活或切换时，在 HUD 上显示提示
            if (active && (!adventureCueWasActive || mechanic != lastAdventureCue))
            {
                hud.ShowMessage(GetAdventureMechanicCue(mechanic), 1.15f, false);
            }

            // 4. 记录当前机制状态，用于下一帧检测变化
            lastAdventureCue = mechanic;
            adventureCueWasActive = active;
            if (!active)
            {
                arena?.UpdateFogWindFx(false, 0f, 0f);
                return;
            }

            if (mechanic == mlpAdventureMechanic.FogWind)
            {
                var gustWave = GetAdventureFogWindWave();
                arena?.UpdateFogWindFx(true, gustWave, ResolveAdventureFogWindStrength(gustWave));
            }
            else
            {
                arena?.UpdateFogWindFx(false, 0f, 0f);
            }

            // 5. 根据机制类型执行特殊效果（如自动充能必杀技）
            switch (mechanic)
            {
                case mlpAdventureMechanic.CandyCharge:
                    ApplyAdventureBonusCharge(playersLeft, dt * 0.72f);
                    break;
                case mlpAdventureMechanic.CandleCircle:
                    ApplyAdventureBonusCharge(playersLeft, dt * 0.34f);
                    ApplyAdventureBonusCharge(playersRight, dt * 0.26f);
                    break;
            }
        }

        private float GetAdventureFogWindWave()
        {
            return Mathf.Sin((matchTime + AdventureFogWindPhaseOffset) * AdventureFogWindFrequency);
        }

        private static float ResolveAdventureFogWindStrength(float gustWave)
        {
            return Mathf.Lerp(AdventureFogWindMinMultiplier, AdventureFogWindMaxMultiplier, Mathf.Abs(gustWave));
        }

        private void ApplyAdventureBallWind(float dt)
        {
            // 1. 非冒险关卡或球不存在时跳过
            if (adventureLevel == null || Ball == null)
            {
                return;
            }

            // 2. 检查当前机制是否为迷雾风且处于激活状态
            var mechanic = GetActiveAdventureMechanic();
            if (mechanic != mlpAdventureMechanic.FogWind || !IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return;
            }

            // 3. 只对飞行中的球施加风力（投篮、入篮、盖帽、空中接力状态）
            if (Ball.State != "shooting" && Ball.State != "basket" && Ball.State != "block" && Ball.State != "alleyOop")
            {
                return;
            }

            // 4. 用正弦函数计算风向，对球施加水平风力
            var gustWave = GetAdventureFogWindWave();
            var direction = gustWave >= 0f ? 1f : -1f;
            var gustStrength = ResolveAdventureFogWindStrength(gustWave);
            Ball.Velocity.x += direction * AdventureFogWindForce * gustStrength * dt;
        }

        private string ResolveAdventureScoreModifier(ref int points)
        {
            // 1. 非冒险关卡直接返回
            if (adventureLevel == null)
            {
                return null;
            }

            // 2. 获取当前机制，未激活则不修改分数
            var mechanic = GetActiveAdventureMechanic();
            if (!IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return null;
            }

            // 3. 双倍篮筐：分数翻倍
            if (mechanic == mlpAdventureMechanic.DoubleHoop)
            {
                points *= 2;
                return $"DOUBLE RIM {points}!";
            }

            // 4. 丰收时刻：分数加 1
            if (mechanic == mlpAdventureMechanic.HarvestTime)
            {
                points += 1;
                return $"HARVEST +1 {points}!";
            }

            // 5. 其他机制不影响分数
            return null;
        }

        private mlpAdventureMechanic GetActiveAdventureMechanic()
        {
            // 1. 非冒险关卡返回默认的普通对决
            if (adventureLevel == null)
            {
                return mlpAdventureMechanic.BasicDuel;
            }

            // 2. 非混合机制关卡直接返回该关卡的机制
            if (adventureLevel.Mechanic != mlpAdventureMechanic.MoonLanternMix)
            {
                return adventureLevel.Mechanic;
            }

            // 3. 月灯混合关卡：每 10 秒轮换一次机制（充能->双倍->风->血月）
            var phase = Mathf.FloorToInt(matchTime / 10f) % 4;
            switch (phase)
            {
                case 0:
                    return mlpAdventureMechanic.CandyCharge;
                case 1:
                    return mlpAdventureMechanic.DoubleHoop;
                case 2:
                    return mlpAdventureMechanic.FogWind;
                default:
                    return mlpAdventureMechanic.BloodMoon;
            }
        }

        private bool IsAdventureMechanicCurrentlyActive(mlpAdventureMechanic mechanic)
        {
            switch (mechanic)
            {
                // 1. 普通对决：始终激活
                case mlpAdventureMechanic.BasicDuel:
                    return true;
                // 2. 双倍篮筐：混合关卡中始终激活，否则按周期激活（每 12 秒中前 6 秒生效）
                case mlpAdventureMechanic.DoubleHoop:
                    return (adventureLevel != null &&
                            adventureLevel.Mechanic == mlpAdventureMechanic.MoonLanternMix) ||
                           Mathf.Repeat(matchTime, AdventureDoubleRimCycle) < AdventureDoubleRimActiveTime;
                // 3. 血月：始终激活
                case mlpAdventureMechanic.BloodMoon:
                    return true;
                // 4. 丰收时刻：仅在最后 15 秒内激活
                case mlpAdventureMechanic.HarvestTime:
                    return RemainingMatchTime <= 15f;
                // 5. 其他机制（糖果充能、烛环、迷雾风等）：始终激活
                default:
                    return true;
            }
        }

        private static string GetAdventureMechanicCue(mlpAdventureMechanic mechanic)
        {
            switch (mechanic)
            {
                case mlpAdventureMechanic.CandyCharge:
                    return "CANDY CHARGE ACTIVE";
                case mlpAdventureMechanic.DoubleHoop:
                    return "DOUBLE RIM ACTIVE";
                case mlpAdventureMechanic.CandleCircle:
                    return "CANDLE RING ACTIVE";
                case mlpAdventureMechanic.FogWind:
                    return "FOG WIND ACTIVE";
                case mlpAdventureMechanic.BloodMoon:
                    return "BLOOD MOON ACTIVE";
                case mlpAdventureMechanic.HarvestTime:
                    return "HARVEST TIME ACTIVE";
                default:
                    return "WARDEN DUEL";
            }
        }

        private static void ApplyAdventureBonusCharge(IReadOnlyList<mlpPlayerObject> players, float amount)
        {
            if (players == null)
            {
                return;
            }

            for (var i = 0; i < players.Count; i++)
            {
                players[i]?.ApplyBonusSuperCharge(amount);
            }
        }

        /// <summary>
        /// 判断球是否正在飞行中（投篮、入篮、扣篮或被盖帽状态）。
        /// </summary>
        /// <returns>球仍在比赛中时返回 true。</returns>
        private bool IsBallInGame()
        {
            return Ball != null &&
                   (Ball.State == "shooting" ||
                    Ball.State == "basket" ||
                    Ball.State == "dunk" ||
                    Ball.State == "block");
        }

        /// <summary>
        /// 判断球在蜂鸣器响后是否已落地或入篮，以便完成比赛。
        /// </summary>
        /// <returns>球已稳定（弹跳、入篮或消失）时返回 true。</returns>
        private bool HasWaitingBallResolved()
        {
            return Ball == null || Ball.State == "bounce" || Ball.State == "score";
        }

        /// <summary>
        /// 检查是否有球员距离无人控制的球足够近。最近的球员将获得球权。
        /// </summary>
        private void TryPickupLooseBall()
        {
            // 1. 球不存在或不在可捡起状态时跳过
            if (Ball == null || !Ball.CanBeTakenInHands)
            {
                return;
            }

            // 2. 遍历左右两队，找到距离球最近的可捡球球员
            mlpPlayerObject picker = null;
            var bestDistance = float.MaxValue;
            foreach (var player in playersLeft)
            {
                var pickupDistance = player.CheckLooseBallPickup(Ball);
                if (pickupDistance >= 0f && pickupDistance < bestDistance)
                {
                    bestDistance = pickupDistance;
                    picker = player;
                }
            }

            foreach (var player in playersRight)
            {
                var pickupDistance = player.CheckLooseBallPickup(Ball);
                if (pickupDistance >= 0f && pickupDistance < bestDistance)
                {
                    bestDistance = pickupDistance;
                    picker = player;
                }
            }

            // 3. 没人能捡球则返回
            if (picker == null)
            {
                return;
            }

            // 4. 让最近的球员拿球，播放捡球音效
            picker.TakeBallInHands();
            mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel);
        }

        /// <summary>
        /// 检查是否有球员能在球处于可盖帽状态时进行盖帽。盖帽成功时返回 true。
        /// </summary>
        /// <returns>球被球员盖帽时返回 true。</returns>
        internal bool TryBlockBall()
        {
            if (Ball == null || !Ball.IsBlockable)
            {
                return false;
            }

            foreach (var player in playersLeft)
            {
                if (player.TryBlockBall(Ball))
                {
                    return true;
                }
            }

            foreach (var player in playersRight)
            {
                if (player.TryBlockBall(Ball))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否有球员能挡开（偏转）球。与盖帽类似但适用于不同的球状态。
        /// </summary>
        /// <returns>球被球员挡开时返回 true。</returns>
        internal bool TryShieldBall()
        {
            if (Ball == null)
            {
                return false;
            }

            foreach (var player in playersLeft)
            {
                if (player.TryShieldBall(Ball))
                {
                    return true;
                }
            }

            foreach (var player in playersRight)
            {
                if (player.TryShieldBall(Ball))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 游戏构建器：根据当前选择的游戏模式（快速比赛、锦标赛、冒险等）配置比赛数据并创建游戏核心实例。
    /// </summary>
    public sealed class mlpGameBuilder
    {
        /// <summary>
        /// 创建并启动一个新的游戏核心。在启动前根据当前游戏模式（快速比赛、锦标赛、冒险、训练、教程或双人对战）设置比赛数据。
        /// </summary>
        /// <param name="root">所有生成的游戏对象的父级 Transform。</param>
        /// <returns>初始化完成的游戏核心实例，可用于每帧更新。</returns>
        public mlpGameCore Build(Transform root)
        {
            // 1. 获取全局物品清单实例
            var inventory = mlpInventory.Instance;
            // 2. 如果比赛已经预先配置好，直接使用（跳过配置步骤）
            if (inventory.MatchPrepared)
            {
                inventory.MatchPrepared = false;
            }
            // 3. 训练模式：使用训练角色和球皮配置比赛
            else if (inventory.GameMode == mlpGameModeIds.Training)
            {
                inventory.MatchData.StartTraining(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            // 4. 教程模式：使用教程配置
            else if (inventory.GameMode == mlpGameModeIds.Tutorial)
            {
                inventory.MatchData.StartTutorial(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            // 5. 重新开始：保持现有配置，只重置比分
            else if (inventory.MatchData.Restarted)
            {
                inventory.MatchData.Restarted = false;
                inventory.MatchData.ResetScore();
            }
            // 6. 锦标赛模式：使用锦标赛当前对手配置
            else if (inventory.IsTournamentActive)
            {
                inventory.MatchData.StartTournamentMatch(inventory.Tournament, inventory.SelectedTournamentBallSelection);
            }
            // 7. 冒险模式：使用冒险关卡配置
            else if (inventory.IsAdventureActive)
            {
                inventory.MatchData.StartAdventureMatch(inventory.Adventure, inventory.Difficulty);
            }
            // 8. 快速比赛：使用选定角色和难度
            else if (inventory.GameMode == mlpGameModeIds.QuickMatch)
            {
                inventory.MatchData.StartQuickMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            // 9. 随机快速比赛：随机选对手
            else if (inventory.GameMode == mlpGameModeIds.RandomQuick)
            {
                inventory.MatchData.StartRandomMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            // 10. 双人对战：使用双方选定角色
            else if (inventory.GameMode == mlpGameModeIds.TwoPlayers)
            {
                inventory.MatchData.StartPlayers2Match(inventory.SelectedVersusBallSelection);
            }

            // 11. 创建游戏核心实例并启动比赛
            var core = new mlpGameCore(root);
            core.Start();
            return core;
        }
    }
}
