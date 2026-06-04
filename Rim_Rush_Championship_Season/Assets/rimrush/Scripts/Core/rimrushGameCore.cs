// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushGameCore 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushGameCore
    {
        private readonly Transform root;
        private readonly List<rimrushPlayerObject> playersLeft = new List<rimrushPlayerObject>();
        private readonly List<rimrushPlayerObject> playersRight = new List<rimrushPlayerObject>();
        private rimrushArenaObject arena;
        private rimrushBasketObject basketLeft;
        private rimrushBasketObject basketRight;
        private rimrushHudView hud;
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
        private bool runtimeResourcesReleased;
        private rimrushTutorialFlow tutorialFlow;
        private rimrushAdventureLevelDefinition adventureLevel;
        private rimrushAdventureMechanic lastAdventureCue = rimrushAdventureMechanic.BasicDuel;
        private bool adventureCueWasActive;

        public rimrushBallObject Ball { get; private set; }
        public rimrushMatchData MatchData => rimrushInventory.Instance.MatchData;
        public rimrushPlayerSignalBus PlayerSignals { get; } = new rimrushPlayerSignalBus();
        public rimrushMatchProcessor MatchProcessor { get; } = new rimrushMatchProcessor();
        public bool ReturnToMenuRequested { get; private set; }
        public bool AdvanceFlowRequested { get; private set; }
        public bool IsSuperShot { get; set; }
        public bool IsAlleyOop { get; set; }
        public float RemainingMatchTime => Mathf.Max(0f, endTime - matchTime);
        public IReadOnlyList<rimrushPlayerObject> PlayersLeft => playersLeft;
        public IReadOnlyList<rimrushPlayerObject> PlayersRight => playersRight;
        public rimrushBasketObject BasketLeft => basketLeft;
        public rimrushBasketObject BasketRight => basketRight;
        public rimrushTutorialFlow TutorialFlow => tutorialFlow;

        /// <summary>
        /// Executes rimrush Game Core for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="root">Input value used by this step of the workflow.</param>
        public rimrushGameCore(Transform root)
        {
            this.root = root;
        }

        /// <summary>
        /// Executes Start for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void Start()
        {
            rimrushPlayersData.SetupPlayers();
            arena = new rimrushArenaObject(root);
            basketLeft = new rimrushBasketObject(-1, root);
            basketRight = new rimrushBasketObject(1, root);
            Ball = new rimrushBallObject(this, root);
            hud = new rimrushHudView(root, MatchData);
            if (rimrushInventory.Instance.GameMode == rimrushGameModeIds.Tutorial)
            {
                tutorialFlow = new rimrushTutorialFlow(this);
            }

            BuildPlayers();
            StartMatch(true);
            tutorialFlow?.Start();
        }

        /// <summary>
        /// Executes Shutdown for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Update for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        public void Update(float dt)
        {
            if (rimrushHelpPanel.IsAnyOpen)
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
                HandlePauseCommand(rimrushPauseCommand.Toggle);
            }

            hud.Update(dt);
            tutorialFlow?.UpdateFrame(dt);
            HandlePauseCommand(hud.ConsumePauseCommand());
            if (rimrushHelpPanel.IsAnyOpen)
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
                        rimrushAudio.Instance?.Play(rimrushAssets.Sounds.MWhistle);
                    }
                }

                return;
            }

            if (isPaused)
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
                    var inventory = rimrushInventory.Instance;
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
                    rimrushAudio.Instance?.Play(rimrushAssets.Sounds.MWhistle);
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
        /// Executes On Ball Scored for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="scoringSide">Input value used by this step of the workflow.</param>
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
                PlayerSignals.Dispatch(rimrushPlayerSignalType.Score, MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
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

            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BBasket);
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
        /// Executes Find Ball Holder for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public rimrushPlayerObject FindBallHolder(int side = 0)
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

        public rimrushPlayerObject FindPlayerBySideAndPlayerNo(int side, int playerNo)
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
        /// Executes Find Closest Opponent for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="source">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public rimrushPlayerObject FindClosestOpponent(rimrushPlayerObject source)
        {
            if (source == null)
            {
                return null;
            }

            var opponents = source.Side == -1 ? playersRight : playersLeft;
            rimrushPlayerObject closest = null;
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
        /// Executes Notify Ball In Hands for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holderSide">Input value used by this step of the workflow.</param>
        /// <param name="holderPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Notify Players Ball Shot for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shotSide">Input value used by this step of the workflow.</param>
        /// <param name="shooterPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Notify Ball Others for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Get Team Mate for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="playerNo">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public rimrushPlayerObject GetTeamMate(int side, int playerNo)
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
        /// Executes Get Score For Side for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public int GetScoreForSide(int side)
        {
            var teamIndex = side == -1 ? 0 : 1;
            return MatchData.MatchScore != null && MatchData.MatchScore.Length > teamIndex
                ? MatchData.MatchScore[teamIndex]
                : 0;
        }

        /// <summary>
        /// Executes Get Score Lead For Side for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public int GetScoreLeadForSide(int side)
        {
            return GetScoreForSide(side) - GetScoreForSide(-side);
        }

        /// <summary>
        /// Executes Is Current Shot Three Pointer for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shotSide">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool IsCurrentShotThreePointer(int shotSide)
        {
            return Ball != null && IsThreePointer(shotSide);
        }

        /// <summary>
        /// Executes Show Hud Message for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        public void ShowHudMessage(string message, float duration = 1.2f)
        {
            hud?.ShowMessage(message, duration);
        }

        /// <summary>
        /// Executes Show Hud Bonus Notice for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="message">Input value used by this step of the workflow.</param>
        /// <param name="duration">Input value used by this step of the workflow.</param>
        public void ShowHudBonusNotice(string message, float duration = 0.9f)
        {
            hud?.ShowBonusNotice(message, duration);
        }

        /// <summary>
        /// Executes Request Return To Menu for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void RequestReturnToMenu()
        {
            SetPaused(false);
            ReturnToMenuRequested = true;
        }

        /// <summary>
        /// Executes Tutorial Reset Scenario for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="playerPosition">Input value used by this step of the workflow.</param>
        /// <param name="opponentPosition">Input value used by this step of the workflow.</param>
        /// <param name="givePlayerBall">Input value used by this step of the workflow.</param>
        /// <param name="giveOpponentBall">Input value used by this step of the workflow.</param>
        /// <param name="playerFacing">Input value used by this step of the workflow.</param>
        /// <param name="opponentFacing">Input value used by this step of the workflow.</param>
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
                Ball.TutorialSnapTo(new Vector2((playerPosition.x + opponentPosition.x) * 0.5f, rimrushObjectsData.BallIndentYCenter));
                left.NotifyBallLoose();
                right.NotifyBallLoose();
            }
        }

        /// <summary>
        /// Executes Try Steal Ball for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="thief">Input value used by this step of the workflow.</param>
        /// <param name="facingDirection">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryStealBall(rimrushPlayerObject thief, float facingDirection)
        {
            if (thief == null || restartDelay > 0f || preMatchCountdown || !isPlaying || IsSuperShot)
            {
                return false;
            }

            var opponents = thief.Side == -1 ? playersRight : playersLeft;
            var stealDistance = rimrushObjectsData.StealDistance + thief.GetStealDistanceBonus();
            rimrushPlayerObject target = null;
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
                PlayerSignals.Dispatch(rimrushPlayerSignalType.StealSuccess, thief.Side, thief.PlayerNo);
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BSteel);
            }

            return true;
        }

        /// <summary>
        /// Executes Resolve Player Blocking for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            var overlapX = rimrushObjectsData.BlockWidth - Mathf.Abs(deltaX);
            if (overlapX <= 0f)
            {
                return;
            }

            var overlapY = rimrushObjectsData.BlockHeight - Mathf.Abs(right.Position.y - left.Position.y);
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
        /// Executes Is Three Pointer for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="scoringSide">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private bool IsThreePointer(int scoringSide)
        {
            if (scoringSide == -1)
            {
                return Ball.LastShotX < rimrushConstants.Width - rimrushObjectsData.ThreePointsDistance;
            }

            return Ball.LastShotX > rimrushObjectsData.ThreePointsDistance;
        }

        /// <summary>
        /// Executes Build Players for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void BuildPlayers()
        {
            var match = MatchData;
            const int playersPerTeam = 1;
            var teamCount = rimrushInventory.Instance.GameMode == rimrushGameModeIds.Training ? 1 : 2;

            for (var teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                for (var playerNo = 0; playerNo < playersPerTeam; playerNo++)
                {
                    var brain = match.Pb[teamIndex].Length > playerNo ? match.Pb[teamIndex][playerNo] : (teamIndex == 0 ? "P0" : "B0");
                    var skill = match.Skills[teamIndex].Length > playerNo ? match.Skills[teamIndex][playerNo] : 0;
                    var player = new rimrushPlayerObject(
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
        /// Executes Start Match for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="regularTime">Input value used by this step of the workflow.</param>
        private void StartMatch(bool regularTime)
        {
            var inventory = rimrushInventory.Instance;
            var gameMode = inventory.GameMode;
            adventureLevel = inventory.IsAdventureActive ? inventory.Adventure.CurrentLevel : null;
            lastAdventureCue = rimrushAdventureMechanic.BasicDuel;
            adventureCueWasActive = false;
            isTraining = gameMode == rimrushGameModeIds.Training || gameMode == rimrushGameModeIds.Tutorial;
            isPlaying = isTraining;
            isPaused = false;
            matchTime = 0f;
            endTime = isTraining ? 99999f : (regularTime ? rimrushConstants.MatchTime : rimrushConstants.OvertimeTime);
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
                hud.ShowMessage(gameMode == rimrushGameModeIds.Tutorial ? "TUTORIAL" : "TRAINING", 0.85f);
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.MWhistle);
            }
        }

        /// <summary>
        /// Executes Handle Pause Command for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="command">Input value used by this step of the workflow.</param>
        private void HandlePauseCommand(rimrushPauseCommand command)
        {
            if (command == rimrushPauseCommand.None || pauseResumeCountdown || postMatchDelay > 0f || hud.IsPostMatchVisible)
            {
                return;
            }

            switch (command)
            {
                case rimrushPauseCommand.Toggle:
                    if (isPaused)
                    {
                        BeginPauseResumeCountdown();
                    }
                    else
                    {
                        SetPaused(true);
                    }
                    break;
                case rimrushPauseCommand.Resume:
                    BeginPauseResumeCountdown();
                    break;
                case rimrushPauseCommand.Menu:
                    SetPaused(false);
                    ReturnToMenuRequested = true;
                    break;
            }
        }

        /// <summary>
        /// Executes Begin Pause Resume Countdown for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Set Paused for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="paused">Input value used by this step of the workflow.</param>
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
        /// Executes Restart for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
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
        /// Executes Restart After Score for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Begin End Of Time for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void BeginEndOfTime()
        {
            matchTime = endTime;
            restartDelay = 0f;
            preMatchCountdown = false;
            waitingForBallAfterBuzzer = false;
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.MBuzzer);
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
        /// Executes Finalize End Match for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Resolve Post Match Delay for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Start Overtime for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void StartOvertime()
        {
            matchTime = 0f;
            endTime = rimrushConstants.OvertimeTime;
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
            return mechanic == rimrushAdventureMechanic.BloodMoon && IsAdventureMechanicCurrentlyActive(mechanic)
                ? 1.14f
                : 1f;
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
                case rimrushAdventureMechanic.CandyCharge:
                    ApplyAdventureBonusCharge(playersLeft, dt * 0.72f);
                    break;
                case rimrushAdventureMechanic.CandleCircle:
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
            if (mechanic != rimrushAdventureMechanic.FogWind || !IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return;
            }

            if (Ball.State != "shooting" && Ball.State != "basket" && Ball.State != "block" && Ball.State != "alleyOop")
            {
                return;
            }

            var direction = Mathf.Sin((matchTime + 0.7f) * 1.35f) >= 0f ? 1f : -1f;
            Ball.Velocity.x += direction * 42f * dt;
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

            if (mechanic == rimrushAdventureMechanic.DoubleHoop)
            {
                points *= 2;
                return $"DOUBLE RIM {points}!";
            }

            if (mechanic == rimrushAdventureMechanic.HarvestTime)
            {
                points += 1;
                return $"HARVEST +1 {points}!";
            }

            return null;
        }

        private rimrushAdventureMechanic GetActiveAdventureMechanic()
        {
            if (adventureLevel == null)
            {
                return rimrushAdventureMechanic.BasicDuel;
            }

            if (adventureLevel.Mechanic != rimrushAdventureMechanic.MoonLanternMix)
            {
                return adventureLevel.Mechanic;
            }

            var phase = Mathf.FloorToInt(matchTime / 10f) % 4;
            switch (phase)
            {
                case 0:
                    return rimrushAdventureMechanic.CandyCharge;
                case 1:
                    return rimrushAdventureMechanic.DoubleHoop;
                case 2:
                    return rimrushAdventureMechanic.FogWind;
                default:
                    return rimrushAdventureMechanic.BloodMoon;
            }
        }

        private bool IsAdventureMechanicCurrentlyActive(rimrushAdventureMechanic mechanic)
        {
            switch (mechanic)
            {
                case rimrushAdventureMechanic.BasicDuel:
                    return true;
                case rimrushAdventureMechanic.DoubleHoop:
                    return Mathf.Repeat(matchTime, 18f) < 7f;
                case rimrushAdventureMechanic.BloodMoon:
                    return Mathf.Repeat(matchTime, 20f) < 8f;
                case rimrushAdventureMechanic.HarvestTime:
                    return RemainingMatchTime <= 15f;
                default:
                    return true;
            }
        }

        private static string GetAdventureMechanicCue(rimrushAdventureMechanic mechanic)
        {
            switch (mechanic)
            {
                case rimrushAdventureMechanic.CandyCharge:
                    return "CANDY CHARGE";
                case rimrushAdventureMechanic.DoubleHoop:
                    return "DOUBLE RIM";
                case rimrushAdventureMechanic.CandleCircle:
                    return "CANDLE RING";
                case rimrushAdventureMechanic.FogWind:
                    return "FOG WIND";
                case rimrushAdventureMechanic.BloodMoon:
                    return "BLOOD MOON";
                case rimrushAdventureMechanic.HarvestTime:
                    return "HARVEST TIME";
                default:
                    return "WARDEN DUEL";
            }
        }

        private static void ApplyAdventureBonusCharge(IReadOnlyList<rimrushPlayerObject> players, float amount)
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
        /// Executes Is Ball In Game for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private bool IsBallInGame()
        {
            return Ball != null &&
                   (Ball.State == "shooting" ||
                    Ball.State == "basket" ||
                    Ball.State == "dunk" ||
                    Ball.State == "block");
        }

        /// <summary>
        /// Executes Has Waiting Ball Resolved for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private bool HasWaitingBallResolved()
        {
            return Ball == null || Ball.State == "bounce" || Ball.State == "score";
        }

        /// <summary>
        /// Executes Try Pickup Loose Ball for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void TryPickupLooseBall()
        {
            if (Ball == null || !Ball.CanBeTakenInHands)
            {
                return;
            }

            rimrushPlayerObject picker = null;
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
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BSteel);
        }

        /// <summary>
        /// Executes Try Block Ball for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Try Shield Ball for the rimrushGameCore workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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

    public sealed class rimrushGameBuilder
    {
        /// <summary>
        /// Executes Build for the rimrushGameBuilder workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="root">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public rimrushGameCore Build(Transform root)
        {
            var inventory = rimrushInventory.Instance;
            if (inventory.MatchPrepared)
            {
                inventory.MatchPrepared = false;
            }
            else if (inventory.GameMode == rimrushGameModeIds.Training)
            {
                inventory.MatchData.StartTraining(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            else if (inventory.GameMode == rimrushGameModeIds.Tutorial)
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
            else if (inventory.GameMode == rimrushGameModeIds.QuickMatch)
            {
                inventory.MatchData.StartQuickMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            else if (inventory.GameMode == rimrushGameModeIds.RandomQuick)
            {
                inventory.MatchData.StartRandomMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            else if (inventory.GameMode == rimrushGameModeIds.TwoPlayers)
            {
                inventory.MatchData.StartPlayers2Match(inventory.SelectedVersusBallSelection);
            }

            var core = new rimrushGameCore(root);
            core.Start();
            return core;
        }
    }
}
