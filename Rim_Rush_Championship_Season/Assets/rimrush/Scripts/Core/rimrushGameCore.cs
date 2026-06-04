// 游戏核心逻辑（比赛主循环）
// 管理一场篮球比赛的全部流程：创建球场和球员、控制计时器、处理得分、判断胜负、暂停和恢复、教程模式。每帧由 Update 驱动，是整个游戏运行的核心。

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
        private bool regularMatchTimeActive;
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
        /// Creates the game core with a parent transform where all game objects (arena, players, ball) will be attached.
        /// </summary>
        /// <param name="root">The parent transform for all spawned game objects.</param>
        public rimrushGameCore(Transform root)
        {
            this.root = root;
        }

        /// <summary>
        /// Initializes the arena, baskets, ball, players, and HUD, then starts the first match. Creates the tutorial flow if in tutorial mode.
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
        /// Releases runtime resources for all players and the tutorial flow. Safe to call multiple times.
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
        /// Main game loop tick. Handles pause input, countdowns, ball physics, player updates, collisions,
        /// timer countdown, and end-of-match logic. Call this every frame with the delta time.
        /// </summary>
        /// <param name="dt">Elapsed time in seconds since the last frame.</param>
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
        /// Called when the ball goes through a basket. Calculates points, updates the score,
        /// shows a message on the HUD, and schedules a restart for the other team.
        /// </summary>
        /// <param name="scoringSide">Which side scored (-1 = left, 1 = right).</param>
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
        /// Searches both teams for the player currently holding the ball.
        /// </summary>
        /// <param name="side">0 = search both sides, -1 = left team only, 1 = right team only.</param>
        /// <returns>The player holding the ball, or null if nobody has it.</returns>
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
        /// Finds the opponent player that is closest to the given player by straight-line distance.
        /// </summary>
        /// <param name="source">The player to measure distance from.</param>
        /// <returns>The nearest opponent, or null if no opponents exist.</returns>
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
        /// Tells all players which teammate currently has the ball, so they can adjust their AI behavior.
        /// </summary>
        /// <param name="holderSide">Which team the ball holder is on.</param>
        /// <param name="holderPlayerNo">Index of the ball holder within their team.</param>
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
        /// Tells all players that a shot was taken, so AI players can react (e.g. move to rebound position).
        /// </summary>
        /// <param name="shotSide">Which team took the shot.</param>
        /// <param name="shooterPlayerNo">Index of the shooting player within their team.</param>
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
        /// Tells all players that the ball is loose and nobody is holding it.
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
        /// Returns the other player on the same team. In 1v1 this is always null since each team has one player.
        /// </summary>
        /// <param name="side">Which team (-1 = left, 1 = right).</param>
        /// <param name="playerNo">Index of the player looking for a teammate.</param>
        /// <returns>The teammate, or null if there is none.</returns>
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
        /// Returns the current score for the given side of the court.
        /// </summary>
        /// <param name="side">Which team (-1 = left, 1 = right).</param>
        /// <returns>The team's current score, or 0 if data is missing.</returns>
        public int GetScoreForSide(int side)
        {
            var teamIndex = side == -1 ? 0 : 1;
            return MatchData.MatchScore != null && MatchData.MatchScore.Length > teamIndex
                ? MatchData.MatchScore[teamIndex]
                : 0;
        }

        /// <summary>
        /// Returns how many points ahead (positive) or behind (negative) the given team is.
        /// </summary>
        /// <param name="side">Which team (-1 = left, 1 = right).</param>
        /// <returns>The score difference (this team's score minus the opponent's).</returns>
        public int GetScoreLeadForSide(int side)
        {
            return GetScoreForSide(side) - GetScoreForSide(-side);
        }

        /// <summary>
        /// Checks whether the ball's last shot position is beyond the three-point line.
        /// </summary>
        /// <param name="shotSide">Which side took the shot (-1 = left, 1 = right).</param>
        /// <returns>True if the shot was a three-pointer.</returns>
        public bool IsCurrentShotThreePointer(int shotSide)
        {
            return Ball != null && IsThreePointer(shotSide);
        }

        /// <summary>
        /// Displays a text message on the HUD for the given duration.
        /// </summary>
        /// <param name="message">The text to show.</param>
        /// <param name="duration">How long the message stays visible in seconds.</param>
        public void ShowHudMessage(string message, float duration = 1.2f)
        {
            hud?.ShowMessage(message, duration);
        }

        /// <summary>
        /// Displays a smaller bonus notice on the HUD (used for score modifiers and special events).
        /// </summary>
        /// <param name="message">The bonus text to show.</param>
        /// <param name="duration">How long the notice stays visible in seconds.</param>
        public void ShowHudBonusNotice(string message, float duration = 0.9f)
        {
            hud?.ShowBonusNotice(message, duration);
        }

        /// <summary>
        /// Unpauses the game and flags that the player wants to go back to the main menu.
        /// </summary>
        public void RequestReturnToMenu()
        {
            SetPaused(false);
            ReturnToMenuRequested = true;
        }

        /// <summary>
        /// Resets the scene for a tutorial practice scenario. Positions both players, optionally gives one the ball,
        /// and places the ball loose in the middle if nobody gets it.
        /// </summary>
        /// <param name="playerPosition">Where to place the player character on the court.</param>
        /// <param name="opponentPosition">Where to place the opponent character on the court.</param>
        /// <param name="givePlayerBall">True to start the player with the ball.</param>
        /// <param name="giveOpponentBall">True to start the opponent with the ball.</param>
        /// <param name="playerFacing">Which direction the player faces (-1 = left, 1 = right).</param>
        /// <param name="opponentFacing">Which direction the opponent faces.</param>
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
        /// Attempts to steal the ball from an opponent. Checks all opponents on the other team
        /// within steal range and takes the ball from the closest one.
        /// </summary>
        /// <param name="thief">The player attempting the steal.</param>
        /// <param name="facingDirection">Which direction the thief is facing (-1 = left, 1 = right).</param>
        /// <returns>True if the steal attempt was made (whether or not the ball was actually taken).</returns>
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
        /// Checks if the two main players are physically overlapping and pushes them apart.
        /// Also interrupts dashes that run into a blocking player.
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
        /// Checks whether the ball was shot from beyond the three-point line relative to the scoring basket.
        /// </summary>
        /// <param name="scoringSide">Which side's basket was scored on.</param>
        /// <returns>True if the shot was a three-pointer.</returns>
        private bool IsThreePointer(int scoringSide)
        {
            if (scoringSide == -1)
            {
                return Ball.LastShotX < rimrushConstants.Width - rimrushObjectsData.ThreePointsDistance;
            }

            return Ball.LastShotX > rimrushObjectsData.ThreePointsDistance;
        }

        /// <summary>
        /// Creates player objects for both teams based on the current match data (character IDs, brain strings, skill levels).
        /// Training mode only creates the left team.
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
        /// Resets everything for a new match: scores, timers, ball position, player positions, and HUD state.
        /// Shows a pre-match countdown in non-training modes.
        /// </summary>
        /// <param name="regularTime">True for a normal-length match, false for overtime length.</param>
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
            regularMatchTimeActive = regularTime && !isTraining;
            endTime = isTraining ? 99999f : rimrushQuickTestSettings.GetMatchTime(regularTime);
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
        /// Processes a pause command: toggling pause, resuming with countdown, or returning to the menu.
        /// </summary>
        /// <param name="command">The pause action to perform (None, Toggle, Resume, or Menu).</param>
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
        /// Starts a 3-2-1 countdown before the game resumes from pause.
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
        /// Sets or clears the pause state and shows/hides the pause overlay accordingly.
        /// </summary>
        /// <param name="paused">True to pause, false to unpause.</param>
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
        /// Resets ball and player positions for a new play. In training mode, gives the ball to the left player.
        /// Otherwise gives it to the specified side if nonzero.
        /// </summary>
        /// <param name="side">Which side gets the ball (-1 = left, 1 = right, 0 = nobody).</param>
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
        /// Restarts play after a score. Gives the ball to the team that was scored on.
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
        /// Handles the buzzer when the match clock runs out. If the ball is still in flight, waits for it to land before deciding the winner.
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
        /// Determines the winner (or overtime if tied) and shows the appropriate end-of-match message.
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
        /// Called after the post-match delay expires. Either starts overtime or shows the final results screen.
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
        /// Resets the match for an overtime period with a shorter clock and a new pre-match countdown.
        /// </summary>
        private void StartOvertime()
        {
            matchTime = 0f;
            endTime = rimrushConstants.OvertimeTime;
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
            return mechanic == rimrushAdventureMechanic.BloodMoon && IsAdventureMechanicCurrentlyActive(mechanic)
                ? 1.14f
                : 1f;
        }

        private bool SyncQuickTestMatchTime()
        {
            if (!regularMatchTimeActive || isTraining)
            {
                return false;
            }

            var targetEndTime = rimrushQuickTestSettings.GetMatchTime(true);
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
        /// Returns true if the ball is currently in flight (shooting, scoring, dunking, or being blocked).
        /// </summary>
        /// <returns>True if the ball is still actively in play.</returns>
        private bool IsBallInGame()
        {
            return Ball != null &&
                   (Ball.State == "shooting" ||
                    Ball.State == "basket" ||
                    Ball.State == "dunk" ||
                    Ball.State == "block");
        }

        /// <summary>
        /// Returns true if the ball has landed or scored after the buzzer, so the match can be finalized.
        /// </summary>
        /// <returns>True if the ball has settled (bounced, scored, or is gone).</returns>
        private bool HasWaitingBallResolved()
        {
            return Ball == null || Ball.State == "bounce" || Ball.State == "score";
        }

        /// <summary>
        /// Checks if any player is close enough to pick up a loose ball. The closest player gets it.
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
        /// Checks if any player can block the ball while it is in a blockable state. Returns true if a block occurred.
        /// </summary>
        /// <returns>True if the ball was blocked by a player.</returns>
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
        /// Checks if any player can shield (deflect) the ball. Similar to blocking but for different ball states.
        /// </summary>
        /// <returns>True if the ball was shielded by a player.</returns>
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
        /// Creates and starts a new game core. Sets up the match data based on the current game mode
        /// (quick match, tournament, adventure, training, tutorial, or 2-player) before launching.
        /// </summary>
        /// <param name="root">The parent transform for all spawned game objects.</param>
        /// <returns>A fully initialized game core ready to be updated each frame.</returns>
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
