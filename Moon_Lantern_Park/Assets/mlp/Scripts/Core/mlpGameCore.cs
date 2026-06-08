// 游戏核心逻辑（比赛主循环）
// 管理一场篮球比赛的全部流程：创建球场和球员、控制计时器、处理得分、判断胜负、暂停和恢复、教程模式。每帧由 Update 驱动，是整个游戏运行的核心。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
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
            mlpPlayersData.SetupPlayers();
            arena = new mlpArenaObject(root);
            basketLeft = new mlpBasketObject(-1, root);
            basketRight = new mlpBasketObject(1, root);
            Ball = new mlpBallObject(this, root);
            hud = new mlpHudView(root, MatchData);
            if (mlpInventory.Instance.GameMode == mlpGameModeIds.Tutorial)
            {
                tutorialFlow = new mlpTutorialFlow(this);
            }

            BuildPlayers();
            StartMatch(true);
            tutorialFlow?.Start();
        }

        /// <summary>
        /// 释放所有球员和教程流程的运行时资源。可安全地多次调用。
        /// </summary>
        public void Shutdown()
        {
            if (runtimeResourcesReleased)
            {
                return;
            }

            tutorialFlow?.Shutdown();
            runtimeResourcesReleased = true;
            foreach (var player in playersLeft)
            {
                player.ReleaseRuntimeResources();
            }

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
            if (mlpHelpPanel.IsAnyOpen)
            {
                hud.Update(dt);
                tutorialFlow?.UpdateFrame(dt);
                return;
            }

            if (!pauseResumeCountdown &&
                (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) &&
                postMatchDelay <= 0f &&
                !hud.IsPostMatchVisible)
            {
                HandlePauseCommand(mlpPauseCommand.Toggle);
            }

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

            if (isPaused)
            {
                return;
            }

            if (SyncQuickTestMatchTime())
            {
                return;
            }

            if (tutorialFlow != null && tutorialFlow.FreezeGameplay)
            {
                return;
            }

            var gameplayDt = tutorialFlow != null ? dt * tutorialFlow.GameplayTimeScale : dt;
            gameplayDt *= GetAdventureGameplayTimeScale();
            UpdateAdventureMechanics(gameplayDt);

            basketLeft.Update(gameplayDt);
            basketRight.Update(gameplayDt);

            if (postMatchDelay > 0f)
            {
                postMatchDelay -= gameplayDt;
                if (postMatchDelay <= 0f)
                {
                    ResolvePostMatchDelay();
                }
                return;
            }

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
            if (restartDelay > 0f)
            {
                return;
            }

            var teamIndex = scoringSide == -1 ? 0 : 1;
            var fallbackPoints = IsThreePointer(scoringSide) ? 3 : 2;
            var points = MatchProcessor.ResolvePointsForScore(scoringSide, fallbackPoints);
            var adventureScoreNotice = ResolveAdventureScoreModifier(ref points);
            var scoringPlayer = FindPlayerBySideAndPlayerNo(MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            string scoreNotice = null;
            if (scoringPlayer != null)
            {
                points = scoringPlayer.ResolveScorePoints(points, out scoreNotice);
            }

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
            if (source == null)
            {
                return null;
            }

            var opponents = source.Side == -1 ? playersRight : playersLeft;
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
            MatchProcessor.Reset();
            IsSuperShot = false;
            IsAlleyOop = false;
            restartDelay = 0f;
            waitingForBallAfterBuzzer = false;

            Ball.Restart();
            foreach (var leftPlayer in playersLeft)
            {
                leftPlayer.Restart(0);
            }

            foreach (var rightPlayer in playersRight)
            {
                rightPlayer.Restart(0);
            }

            if (playersLeft.Count == 0 || playersRight.Count == 0)
            {
                return;
            }

            var left = playersLeft[0];
            var right = playersRight[0];
            left.TutorialSnapTo(playerPosition, playerFacing);
            right.TutorialSnapTo(opponentPosition, opponentFacing);

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
            if (thief == null || restartDelay > 0f || preMatchCountdown || !isPlaying || IsSuperShot)
            {
                return false;
            }

            var opponents = thief.Side == -1 ? playersRight : playersLeft;
            var stealDistance = mlpObjectsData.StealDistance + thief.GetStealDistanceBonus();
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

            if (target == null)
            {
                return false;
            }

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
            if (playersLeft.Count == 0 || playersRight.Count == 0 || IsSuperShot)
            {
                return;
            }

            var left = playersLeft[0];
            var right = playersRight[0];
            if (left == null || right == null || !left.CanResolveGroundBlock || !right.CanResolveGroundBlock)
            {
                return;
            }

            if (!left.HasGroundBlockBody && !right.HasGroundBlockBody)
            {
                return;
            }

            var deltaX = right.Position.x - left.Position.x;
            var overlapX = mlpObjectsData.BlockWidth - Mathf.Abs(deltaX);
            if (overlapX <= 0f)
            {
                return;
            }

            var overlapY = mlpObjectsData.BlockHeight - Mathf.Abs(right.Position.y - left.Position.y);
            if (overlapY <= 0f)
            {
                return;
            }

            var leftApproaching = left.IsMovingToward(right);
            var rightApproaching = right.IsMovingToward(left);
            if (!leftApproaching && !rightApproaching)
            {
                return;
            }

            var leftMass = left.GetCollisionMass();
            var rightMass = right.GetCollisionMass();
            var totalMass = Mathf.Max(0.001f, leftMass + rightMass);
            var separationSign = deltaX >= 0f ? 1f : -1f;
            left.ApplyHorizontalSeparation(-overlapX * (rightMass / totalMass) * separationSign);
            right.ApplyHorizontalSeparation(overlapX * (leftMass / totalMass) * separationSign);

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
            var match = MatchData;
            const int playersPerTeam = 1;
            var teamCount = mlpInventory.Instance.GameMode == mlpGameModeIds.Training ? 1 : 2;

            for (var teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                for (var playerNo = 0; playerNo < playersPerTeam; playerNo++)
                {
                    var brain = match.Pb[teamIndex].Length > playerNo ? match.Pb[teamIndex][playerNo] : (teamIndex == 0 ? "P0" : "B0");
                    var skill = match.Skills[teamIndex].Length > playerNo ? match.Skills[teamIndex][playerNo] : 0;
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
            var inventory = mlpInventory.Instance;
            var gameMode = inventory.GameMode;
            adventureLevel = inventory.IsAdventureActive ? inventory.Adventure.CurrentLevel : null;
            lastAdventureCue = mlpAdventureMechanic.BasicDuel;
            adventureCueWasActive = false;
            isTraining = gameMode == mlpGameModeIds.Training || gameMode == mlpGameModeIds.Tutorial;
            isPlaying = isTraining;
            isPaused = false;
            matchTime = 0f;
            regularMatchTimeActive = regularTime && !isTraining;
            endTime = isTraining ? 99999f : mlpQuickTestSettings.GetMatchTime(regularTime);
            MatchData.ResetScore();
            hud.UpdateScore(0, 0);
            hud.SetTimerVisible(!isTraining);
            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
            hud.HidePauseOverlay();
            postMatchDelay = 0f;
            postMatchWinner = 0;
            overtimePending = false;
            waitingForBallAfterBuzzer = false;
            IsSuperShot = false;
            IsAlleyOop = false;
            ReturnToMenuRequested = false;
            AdvanceFlowRequested = false;
            MatchProcessor.Reset();
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
            if (command == mlpPauseCommand.None || pauseResumeCountdown || postMatchDelay > 0f || hud.IsPostMatchVisible)
            {
                return;
            }

            switch (command)
            {
                case mlpPauseCommand.Toggle:
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
                    BeginPauseResumeCountdown();
                    break;
                case mlpPauseCommand.Menu:
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

            isPaused = paused;
            if (paused)
            {
                pauseResumeCountdown = false;
                hud.ShowPauseOverlay();
            }
            else
            {
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
            Ball.Restart();
            IsSuperShot = false;
            IsAlleyOop = false;
            MatchProcessor.Reset();
            foreach (var player in playersLeft)
            {
                player.Restart(side);
            }

            foreach (var player in playersRight)
            {
                player.Restart(side);
            }

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
            MatchProcessor.Reset();
            foreach (var player in playersLeft)
            {
                player.NotifyBallLoose();
            }

            foreach (var player in playersRight)
            {
                player.NotifyBallLoose();
            }

            Restart(restartSide);
        }

        /// <summary>
        /// 处理比赛时间结束时的蜂鸣器。如果球仍在飞行中，等待球落地后再决定获胜方。
        /// </summary>
        private void BeginEndOfTime()
        {
            matchTime = endTime;
            restartDelay = 0f;
            preMatchCountdown = false;
            waitingForBallAfterBuzzer = false;
            mlpAudio.Instance?.Play(mlpAssets.Sounds.MBuzzer);
            hud.HideCountdown();
            hud.UpdateTimer(0f);
            if (IsBallInGame())
            {
                waitingForBallAfterBuzzer = true;
                return;
            }

            FinalizeEndMatch();
        }

        /// <summary>
        /// 确定获胜方（平局则进入加时赛），并显示相应的比赛结束消息。
        /// </summary>
        private void FinalizeEndMatch()
        {
            isPlaying = false;
            waitingForBallAfterBuzzer = false;
            var winner = MatchData.WhoWins();
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
            postMatchDelay = 0f;
            if (overtimePending)
            {
                overtimePending = false;
                StartOvertime();
                return;
            }

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
            matchTime = 0f;
            endTime = mlpConstants.OvertimeTime;
            regularMatchTimeActive = false;
            waitingForBallAfterBuzzer = false;
            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
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
            if (!regularMatchTimeActive || isTraining)
            {
                return false;
            }

            var targetEndTime = mlpQuickTestSettings.GetMatchTime(true);
            if (!Mathf.Approximately(endTime, targetEndTime))
            {
                endTime = targetEndTime;
                hud.UpdateTimer(Mathf.Max(0f, endTime - matchTime));
            }

            if (!isPlaying || preMatchCountdown || waitingForBallAfterBuzzer || matchTime < endTime)
            {
                return false;
            }

            BeginEndOfTime();
            return true;
        }

        private void UpdateAdventureMechanics(float dt)
        {
            if (adventureLevel == null || isTraining || !isPlaying)
            {
                return;
            }

            var mechanic = GetActiveAdventureMechanic();
            var active = IsAdventureMechanicCurrentlyActive(mechanic);
            if (active && (!adventureCueWasActive || mechanic != lastAdventureCue))
            {
                hud.ShowMessage(GetAdventureMechanicCue(mechanic), 1.15f, false);
            }

            lastAdventureCue = mechanic;
            adventureCueWasActive = active;
            if (!active)
            {
                return;
            }

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
            if (adventureLevel == null || Ball == null)
            {
                return;
            }

            var mechanic = GetActiveAdventureMechanic();
            if (mechanic != mlpAdventureMechanic.FogWind || !IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return;
            }

            if (Ball.State != "shooting" && Ball.State != "basket" && Ball.State != "block" && Ball.State != "alleyOop")
            {
                return;
            }

            var direction = Mathf.Sin((matchTime + 0.7f) * 1.35f) >= 0f ? 1f : -1f;
            Ball.Velocity.x += direction * AdventureFogWindForce * dt;
        }

        private string ResolveAdventureScoreModifier(ref int points)
        {
            if (adventureLevel == null)
            {
                return null;
            }

            var mechanic = GetActiveAdventureMechanic();
            if (!IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return null;
            }

            if (mechanic == mlpAdventureMechanic.DoubleHoop)
            {
                points *= 2;
                return $"DOUBLE RIM {points}!";
            }

            if (mechanic == mlpAdventureMechanic.HarvestTime)
            {
                points += 1;
                return $"HARVEST +1 {points}!";
            }

            return null;
        }

        private mlpAdventureMechanic GetActiveAdventureMechanic()
        {
            if (adventureLevel == null)
            {
                return mlpAdventureMechanic.BasicDuel;
            }

            if (adventureLevel.Mechanic != mlpAdventureMechanic.MoonLanternMix)
            {
                return adventureLevel.Mechanic;
            }

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
                case mlpAdventureMechanic.BasicDuel:
                    return true;
                case mlpAdventureMechanic.DoubleHoop:
                    return (adventureLevel != null &&
                            adventureLevel.Mechanic == mlpAdventureMechanic.MoonLanternMix) ||
                           Mathf.Repeat(matchTime, AdventureDoubleRimCycle) < AdventureDoubleRimActiveTime;
                case mlpAdventureMechanic.BloodMoon:
                    return true;
                case mlpAdventureMechanic.HarvestTime:
                    return RemainingMatchTime <= 15f;
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
            if (Ball == null || !Ball.CanBeTakenInHands)
            {
                return;
            }

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

            if (picker == null)
            {
                return;
            }

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

    public sealed class mlpGameBuilder
    {
        /// <summary>
        /// 创建并启动一个新的游戏核心。在启动前根据当前游戏模式（快速比赛、锦标赛、冒险、训练、教程或双人对战）设置比赛数据。
        /// </summary>
        /// <param name="root">所有生成的游戏对象的父级 Transform。</param>
        /// <returns>初始化完成的游戏核心实例，可用于每帧更新。</returns>
        public mlpGameCore Build(Transform root)
        {
            var inventory = mlpInventory.Instance;
            if (inventory.MatchPrepared)
            {
                inventory.MatchPrepared = false;
            }
            else if (inventory.GameMode == mlpGameModeIds.Training)
            {
                inventory.MatchData.StartTraining(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            else if (inventory.GameMode == mlpGameModeIds.Tutorial)
            {
                inventory.MatchData.StartTutorial(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            else if (inventory.MatchData.Restarted)
            {
                inventory.MatchData.Restarted = false;
                inventory.MatchData.ResetScore();
            }
            else if (inventory.IsTournamentActive)
            {
                inventory.MatchData.StartTournamentMatch(inventory.Tournament, inventory.SelectedTournamentBallSelection);
            }
            else if (inventory.IsAdventureActive)
            {
                inventory.MatchData.StartAdventureMatch(inventory.Adventure, inventory.Difficulty);
            }
            else if (inventory.GameMode == mlpGameModeIds.QuickMatch)
            {
                inventory.MatchData.StartQuickMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            else if (inventory.GameMode == mlpGameModeIds.RandomQuick)
            {
                inventory.MatchData.StartRandomMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            else if (inventory.GameMode == mlpGameModeIds.TwoPlayers)
            {
                inventory.MatchData.StartPlayers2Match(inventory.SelectedVersusBallSelection);
            }

            var core = new mlpGameCore(root);
            core.Start();
            return core;
        }
    }
}
