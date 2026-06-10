// 游戏核心逻辑（比赛主循环）
// 管理一场篮球比赛的全部流程：创建球场和球员、控制计时器、处理得分、判断胜负、暂停和恢复、教程模式。每帧由 Update 驱动，是整个游戏运行的核心。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏核心逻辑（比赛主循环）：管理一场篮球比赛的全部流程——创建球场和球员、控制计时器、处理得分、判断胜负、暂停和恢复。每帧由 Update 驱动，是整个游戏运行的核心。
    /// </summary>
    public sealed class mlpGameCore
    {
        private readonly Transform root;
        private readonly List<mlpPlayerObject> playersLeft = new List<mlpPlayerObject>();
        private readonly List<mlpPlayerObject> playersRight = new List<mlpPlayerObject>();
        private const float AdventureDoubleRimCycle = 12f;
        private const float AdventureDoubleRimActiveTime = 6f;
        private const float AdventureBloodMoonTimeScale = 1.14f;
        private const float AdventureFogWindForce = 30f;
        private mlpArenaObject arena;
        private mlpBasketObject basketLeft;
        private mlpBasketObject basketRight;
        private mlpHudView hud;
        private bool isPlaying;
        private bool isPaused;
        private bool isTraining;
        private float matchTime;
        private float endTime;
        private float restartDelay;
        private int restartSide;
        private bool preMatchCountdown;
        private bool pauseResumeCountdown;
        private bool waitingForBallAfterBuzzer;
        private float postMatchDelay;
        private int postMatchWinner;
        private bool overtimePending;
        private bool regularMatchTimeActive;
        private bool runtimeResourcesReleased;
        private mlpTutorialFlow tutorialFlow;
        private mlpAdventureLevelDefinition adventureLevel;
        private mlpAdventureMechanic lastAdventureCue = mlpAdventureMechanic.BasicDuel;
        private bool adventureCueWasActive;

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
        /// </summary>
        /// <param name="dt">自上一帧以来经过的时间（秒）。</param>
        public void Update(float dt)
        {
            // 1. 帮助面板打开时只更新 HUD 和教程，不处理游戏逻辑
            if (mlpHelpPanel.IsAnyOpen)
            {
                hud.Update(dt);
                tutorialFlow?.UpdateFrame(dt);
                return;
            }

            // 2. 检测暂停按键（P 或 Esc），在非赛后延迟且非结算界面时切换暂停
            if (!pauseResumeCountdown &&
                (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) &&
                postMatchDelay <= 0f &&
                !hud.IsPostMatchVisible)
            {
                HandlePauseCommand(mlpPauseCommand.Toggle);
            }

            // 3. 更新 HUD 和教程，处理暂停命令
            hud.Update(dt);
            tutorialFlow?.UpdateFrame(dt);
            HandlePauseCommand(hud.ConsumePauseCommand());
            if (mlpHelpPanel.IsAnyOpen)
            {
                return;
            }

            if (ReturnToMenuRequested)
            {
                return;
            }

            // 4. 暂停恢复倒计时：倒计时结束后取消暂停并吹哨
            if (pauseResumeCountdown)
            {
                pauseResumeCountdown = hud.UpdateCountdown(dt);
                if (!pauseResumeCountdown)
                {
                    hud.EndResumeCountdown();
                    isPaused = false;
                    if (!preMatchCountdown)
                    {
                        mlpAudio.Instance?.Play(mlpAssets.Sounds.MWhistle);
                    }
                }

                return;
            }

            // 5. 暂停状态直接跳过所有游戏逻辑
            if (isPaused)
            {
                return;
            }

            if (SyncQuickTestMatchTime())
            {
                return;
            }

            // 6. 教程冻结时暂停游戏物理（教程对话期间球和球员不动）
            if (tutorialFlow != null && tutorialFlow.FreezeGameplay)
            {
                return;
            }

            // 7. 计算实际游戏时间（可能被教程或冒险模式减速/加速）
            var gameplayDt = tutorialFlow != null ? dt * tutorialFlow.GameplayTimeScale : dt;
            gameplayDt *= GetAdventureGameplayTimeScale();
            UpdateAdventureMechanics(gameplayDt);

            // 8. 更新篮筐动画
            basketLeft.Update(gameplayDt);
            basketRight.Update(gameplayDt);

            // 9. 赛后延迟：等待指定时间后进入加时赛或显示结果
            if (postMatchDelay > 0f)
            {
                postMatchDelay -= gameplayDt;
                if (postMatchDelay <= 0f)
                {
                    ResolvePostMatchDelay();
                }
                return;
            }

            // 10. 结算界面显示中：点击后推进到下一阶段（锦标赛/冒险回到流程，普通模式回菜单）
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

            // 11. 赛前倒计时：更新球员冷却，倒计时结束后吹哨开球
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

            // 12. 得分后延迟：等待后重新开球
            if (restartDelay > 0f)
            {
                restartDelay -= gameplayDt;
                if (restartDelay <= 0f)
                {
                    RestartAfterScore();
                }
                return;
            }

            if (!isPlaying)
            {
                return;
            }

            // 13. 终场后等待篮球落地：球在空中时继续物理更新，落地后判定胜负
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

            // 14. 正常比赛进行中：更新篮球物理、所有球员、盖帽检测、捡球检测
            ApplyAdventureBallWind(gameplayDt);
            Ball.Update(gameplayDt, basketLeft, basketRight);
            foreach (var player in playersLeft)
            {
                player.Update(gameplayDt);
            }
            foreach (var player in playersRight)
            {
                player.Update(gameplayDt);
            }

            ResolvePlayerBlocking();
            TryBlockBall();
            TryPickupLooseBall();
            tutorialFlow?.UpdateAfterGameplay(dt);

            // 15. 更新比赛计时器，时间到则触发终场
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
        /// </summary>
        /// <param name="scoringSide">得分方所在半场（-1 = 左侧，1 = 右侧）。</param>
        public void OnBallScored(int scoringSide)
        {
            // 1. 防止重复触发得分（延迟期间不处理）
            if (restartDelay > 0f)
            {
                return;
            }

            // 2. 计算得分：根据投篮距离判断 2 分还是 3 分，冒险模式可能有加成
            var teamIndex = scoringSide == -1 ? 0 : 1;
            var fallbackPoints = IsThreePointer(scoringSide) ? 3 : 2;
            var points = MatchProcessor.ResolvePointsForScore(scoringSide, fallbackPoints);
            var adventureScoreNotice = ResolveAdventureScoreModifier(ref points);

            // 3. 查找投篮球员，应用其得分加成技能（如狂欢大奖）
            var scoringPlayer = FindPlayerBySideAndPlayerNo(MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            string scoreNotice = null;
            if (scoringPlayer != null)
            {
                points = scoringPlayer.ResolveScorePoints(points, out scoreNotice);
            }

            // 4. 更新比分和 HUD 显示
            MatchData.MatchScore[teamIndex] += points;
            hud.UpdateScore(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            if (MatchProcessor.ShotPlayerNo >= 0)
            {
                PlayerSignals.Dispatch(mlpPlayerSignalType.Score, MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            }
            hud.HideCountdown();
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
            if (waitingForBallAfterBuzzer)
            {
                restartDelay = 0f;
                return;
            }

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
                return;
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
            var direction = Mathf.Sin((matchTime + 0.7f) * 1.35f) >= 0f ? 1f : -1f;
            Ball.Velocity.x += direction * AdventureFogWindForce * dt;
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
