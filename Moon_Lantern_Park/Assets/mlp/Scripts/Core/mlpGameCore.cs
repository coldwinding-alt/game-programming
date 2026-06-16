// Game core logic (main game loop)
// Manage the entire process of a basketball game: creating the court and players, controlling the timer, handling scores, determining the outcome, pausing and resuming, and tutorial mode. Each frame is driven by Update and is the core of the entire game.
//
// Competition life cycle method chain:

//
//   StartMatch() ← Start a new match (reset status, display countdown)
//     ↓ (The game is in progress, Update is driven every frame)

//   BeginEndOfTime() ← When time is up, play the final buzzer

//     ↓
//   FinalizeEndMatch() ← Determine the outcome (tie → overtime, otherwise the result will be displayed)
//     ↓
//   ResolvePostMatchDelay() ← After the post-match delay ends Level 9

//     ├─ Draw → StartOvertime() ← Overtime (shorter timer, restart countdown)

//     └─ There is a winner → Display the settlement interface

//     ↓
//   (Waiting for player to click)

//     ├─ Tournament/Adventure → AdvanceFlowRequested = true ← Advance Flow

//     └─ Normal mode → ReturnToMenuRequested = true ← Return to main menu

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// The core logic of the game (the main game loop): manages the entire process of a basketball game - creating the court and players, controlling the timer, processing scores, determining the outcome, timeout and recovery. Each frame is driven by Update and is the core of the entire game.
    /// </summary>
    public sealed class mlpGameCore
    {
        private readonly Transform root;                                              // The parent Transform on which all game objects (courts, players, balls) are mounted

        private readonly List<mlpPlayerObject> playersLeft = new List<mlpPlayerObject>();  // List of players on the left team
        private readonly List<mlpPlayerObject> playersRight = new List<mlpPlayerObject>(); // Player list of the team on the right

        private const float AdventureDoubleRimCycle = 12f;                            // Complete cycle time (seconds) of the "Double Basket" mechanism in Adventure Mode

        private const float AdventureDoubleRimActiveTime = 6f;                        // The duration (seconds) that "Double Basket" in Adventure Mode takes effect in each cycle
        private const float AdventureBloodMoonTimeScale = 1.14f;                      // Game speed multiplier for the Adventure Mode "Blood Moon" mechanic (14% acceleration)

        private const float AdventureFogWindForce = 120f;
        private const float AdventureFogWindMinMultiplier = 0.7f;
        private const float AdventureFogWindMaxMultiplier = 1f;
        private const float AdventureFogWindPhaseOffset = 0.7f;
        private const float AdventureFogWindFrequency = 1.35f;
        private mlpArenaObject arena;                                                 // Stadium background object

        private mlpBasketObject basketLeft;                                           // left basket

        private mlpBasketObject basketRight;                                          // right basket

        private mlpHudView hud;                                                       // HUD interface (score board, timer, message prompt, pause mask)

        private bool isPlaying;                                                       // Whether the game is in progress (true after the countdown ends)

        private bool isPaused;                                                        // Is it in paused state?

        private bool isTraining;                                                      // Whether it is training mode or tutorial mode (no timer limit)

        private float matchTime;                                                      // The time the current match has been played (seconds)

        private float endTime;                                                        // Game end time (seconds), training mode is 99999

        private float restartDelay;                                                   // Waiting time (seconds) before restarting the kick after scoring, kicking off when reset to zero

        private int restartSide;                                                      // The side that gets the ball after scoring (-1=left, 1=right)
        private bool preMatchCountdown;                                               // Are you in the pre-match 3-2-1 countdown phase?

        private bool pauseResumeCountdown;                                            // Are you in the 3-2-1 countdown phase of pause and recovery?

        private bool waitingForBallAfterBuzzer;                                       // Are you waiting for the flying basketball to land after the final buzzer?

        private float postMatchDelay;                                                 // Post-match delay timer (seconds) to allow buffering time before displaying results

        private int postMatchWinner;                                                  // The winner determined after the game (-1=left, 1=right, 0=tie/undecided)

        private bool overtimePending;                                                 // Whether to enter overtime (true in case of a draw)

        private bool regularMatchTimeActive;                                          // Whether it is a formal game (non-overtime, non-training)

        private bool runtimeResourcesReleased;                                        // Whether resources have been released during runtime (to prevent repeated release)

        private mlpTutorialFlow tutorialFlow;                                         // Tutorial process controller (null for non-tutorial mode)

        private mlpAdventureLevelDefinition adventureLevel;                           // Current adventure level definition (null for non-adventure mode)

        private mlpAdventureMechanic lastAdventureCue = mlpAdventureMechanic.BasicDuel; // The adventure mechanism in effect in the previous frame (used to detect mechanism switching)

        private bool adventureCueWasActive;                                           // Whether the adventure mechanism was active in the previous frame (used to detect activation/invalidation changes)

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
        /// Create the game core and specify the parent Transform to which all game objects (courts, players, balls) are mounted.
        /// </summary>
        /// <param name="root">The parent Transform of all generated game objects. </param>
        public mlpGameCore(Transform root)
        {
            this.root = root;
        }

        /// <summary>
        /// Initialize the court, hoops, balls, players, and HUD, then start your first game. Tutorial processes are created in tutorial mode.
        /// </summary>
        public void Start()
        {
            // 1. Load player role data

            mlpPlayersData.SetupPlayers();

            // 2. Create the court background, left and right baskets, and basketball

            arena = new mlpArenaObject(root);
            basketLeft = new mlpBasketObject(-1, root);
            basketRight = new mlpBasketObject(1, root);
            Ball = new mlpBallObject(this, root);

            // 3. Create HUD interface for score and timer
            hud = new mlpHudView(root, MatchData);

            // 4. Create a tutorial process controller in tutorial mode
            if (mlpInventory.Instance.GameMode == mlpGameModeIds.Tutorial)
            {
                tutorialFlow = new mlpTutorialFlow(this);
            }

            // 5. Create all players, start your first match, and start the tutorial
            BuildPlayers();
            StartMatch(true);
            tutorialFlow?.Start();
        }

        /// <summary>
        /// Releases runtime resources for all players and tutorial processes. Safe to call multiple times.
        /// </summary>
        public void Shutdown()
        {
            // 1. If the resource has been released, return directly to prevent repeated release.

            if (runtimeResourcesReleased)
            {
                return;
            }

            // 2. Close the tutorial process (if any)

            tutorialFlow?.Shutdown();
            // 3. Mark the resource as released
            runtimeResourcesReleased = true;
            // 4. Traverse the left team and release the runtime resources of each player
            foreach (var player in playersLeft)
            {
                player.ReleaseRuntimeResources();
            }

            // 5. Traverse the teams on the right and release the runtime resources of each player
            foreach (var player in playersRight)
            {
                player.ReleaseRuntimeResources();
            }
        }

        /// <summary>
        /// The main game loop is called every frame. Handles timeout input, countdown, ball physics, player updates, collision detection, timer countdown, and end-of-game logic.
        /// The overall structure adopts a "state machine + early return" structure: each layer determines the current game stage, and if the condition is hit, it executes the corresponding logic and returns immediately.
        /// "Short-circuit" subsequent deeper state judgments. In this way, each frame will only hit one stage, and there will be no logical overlap.
        /// The stage priorities from high to low are:
        ///   Help Panel → Pause Input → HUD/Tutorial Update → Return to Menu Request → Resume Countdown → Pause → Quick Test Sync → Tutorial Freeze →
        ///   Basket animation → Post-game delay → Settlement interface → Pre-game countdown → Post-score delay → Game in progress → Timer
        /// </summary>
        /// <param name="dt">The elapsed time (in seconds) since the previous frame, passed in by Unity's Time.deltaTime. </param>
        public void Update(float dt)
        {
            // ── Layer 1: Help panel (modal dialog box) ──────────────────────────────

            // The game enters a "freeze" state when the help panel is open: all match physics and input are suspended,

            // However, the HUD (timer animation, message fade-out) and tutorial flow still need to be refreshed normally.
            if (mlpHelpPanel.IsAnyOpen)
            {
                hud.Update(dt);
                tutorialFlow?.UpdateFrame(dt);
                return;
            }

            // ── Layer 2: Pause key detection ──────────────────────────────────────
            // Toggle pause state when player presses P or Esc. The following situations are not allowed to trigger a pause:
            //   - pauseResumeCountdown: The 3-2-1 countdown is in progress to resume from pause and cannot be interrupted.
            //   - postMatchDelay > 0: Post-match delay (waiting to display results), pausing will interfere with the process

            //   - hud.IsPostMatchVisible: The settlement interface has been displayed, and the pause is meaningless.

            if (!pauseResumeCountdown &&
                (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) &&
                postMatchDelay <= 0f &&
                !hud.IsPostMatchVisible)
            {
                HandlePauseCommand(mlpPauseCommand.Toggle);
            }

            // ── Tier 3: HUD and tutorial updates ─────────────────────────────────
            // HUD refreshes every frame: message text fade out, timer animation, button hover state, etc.

            // The tutorial process is refreshed every frame: dialogue bubble display, step advancement, etc.

            // hud.ConsumePauseCommand() checks whether the pause button on the HUD is clicked,

            // If clicked, return to the corresponding pause command (Toggle/Resume/Menu).

            hud.Update(dt);
            tutorialFlow?.UpdateFrame(dt);
            HandlePauseCommand(hud.ConsumePauseCommand());

            // Check the help panel again after the HUD is updated - as the HUD button click may have just opened the help panel

            if (mlpHelpPanel.IsAnyOpen)
            {
                return;
            }

            // The player requests to return to the main menu in the settlement interface or pause menu, which is handled by the outer mlpGameBootstrap
            if (ReturnToMenuRequested)
            {
                return;
            }

            // ──Level 4: Pause and resume countdown────────────────────────────────────

            // When resuming from pause, the 3-2-1 countdown is first displayed, and the pause is actually canceled after the countdown is over.

            // This way players have time to prepare and will not suddenly resume play without warning.

            // The countdown is driven by the HUD (UpdateCountdown returns false to indicate the end of the countdown).

            if (pauseResumeCountdown)
            {
                pauseResumeCountdown = hud.UpdateCountdown(dt);
                if (!pauseResumeCountdown)
                {
                    hud.EndResumeCountdown();
                    isPaused = false;
                    // The whistle will not be blown when the countdown phase resumes before the game (because there is a countdown before the game)

                    if (!preMatchCountdown)
                    {
                        mlpAudio.Instance?.Play(mlpAssets.Sounds.MWhistle);
                    }
                }

                return;
            }

            // ── Level 5: Suspended state ────────────────────────────────────────
            // All game physics and logic are skipped during pause, and the screen freezes completely.

            if (isPaused)
            {
                return;
            }

            // In the quick test mode, the game duration may be dynamically modified. Synchronize here and check whether the ending needs to be triggered.

            if (SyncQuickTestMatchTime())
            {
                return;
            }

            // ── Level 6: Tutorial Freeze ────────────────────────────────────────
            // While the tutorial dialog is open (FreezeGameplay = true), neither the ball nor the player moves,
            // Let players focus on reading the operating instructions. Only the HUD and tutorial flow continue to refresh.
            if (tutorialFlow != null && tutorialFlow.FreezeGameplay)
            {
                return;
            }

            // ── Layer 7: Calculate actual game time ─────────────────────────────────
            // gameplayDt is the real game time after time scaling:

            //   - GameplayTimeScale can be slowed down (such as 0.5x) in tutorial mode to allow novices to see the action clearly

            //   - The "Blood Moon" mechanism in Adventure Mode will be accelerated by 14% (1.14x), increasing the tension

            // UpdateAdventureMechanics handles adventure mode's special mechanism activation/deactivation logic

            var gameplayDt = tutorialFlow != null ? dt * tutorialFlow.GameplayTimeScale : dt;
            gameplayDt *= GetAdventureGameplayTimeScale();
            UpdateAdventureMechanics(gameplayDt);

            // ── Layer 8: Basket animation ───────────────────────────────────────

            // The basket has an elastic shaking animation (the net bag swings after a goal is scored), which needs to be updated independently for each frame.

            // Basket animations are not affected by game phases, and the aftereffects can also be seen during the post-game delay.

            basketLeft.Update(gameplayDt);
            basketRight.Update(gameplayDt);

            // ──Tier 9: Post-match delay────────────────────────────────────────

            // After the game time is up or the winner is determined, the settlement interface will not be displayed immediately, but will wait for a short delay.

            // (about 1.15~1.2 seconds), allowing time for the "TIME!!!" or "OVERTIME" message to be displayed.

            // After the delay ends ResolvePostMatchDelay decides: tie → enter overtime, otherwise → show settlement.

            if (postMatchDelay > 0f)
            {
                postMatchDelay -= gameplayDt;
                if (postMatchDelay <= 0f)
                {
                    ResolvePostMatchDelay();
                }
                return;
            }

            // ── Layer 10: Settlement interface interaction ────────────────────────────────────
            // The settlement interface displays the game results (score, victory or defeat). After the player clicks the mouse/presses Enter/Space:
            //   - Tournament/Adventure Mode: Set AdvanceFlowRequested to advance from the outer layer to the next level/next round

            //   - Normal mode: Set ReturnToMenuRequested to return to the main menu
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

            // ──Level 11: Pre-match countdown─────────────────────────────────────

            // A "TIP OFF IN 3...2...1..." countdown is displayed when a new match starts.

            // Players can move but cannot shoot during the countdown (TickPreMatch updates skill cooldowns, etc.).
            // After the countdown ends, set isPlaying = true, blow the whistle to kick off, and officially enter the game.
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

            // ──Level 12: Post-Score Delay ────────────────────────────────────
            // After a team scores, wait approximately 1.15 seconds before restarting the kick. This time allows goal animations and score messages to be displayed.
            // After the delay, RestartAfterScore returns the ball to the team that was scored and restarts the game.

            if (restartDelay > 0f)
            {
                restartDelay -= gameplayDt;
                if (restartDelay <= 0f)
                {
                    RestartAfterScore();
                }
                return;
            }

            // isPlaying is false indicating that the game has not officially started yet (for example, it has just been created but the whistle has not been blown yet)

            if (!isPlaying)
            {
                return;
            }

            // ──Level 13: Wait for the ball to land after the end of the game────────────────────────────────
            // After the final buzzer sounds, if the ball is still flying in the air (in the shooting arc), the winner cannot be determined immediately.

            // Because the ball may still be heading to the basket, it's possible that it could be hit after the buzzer - a situation that should count for a point.

            // So continue to update the ball's physics and block detection, and then FinalizeEndMatch after the ball hits the ground or enters the basket.

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

            // ── Level 14: Normal game progress (core physical cycle) ────────────────────

            // This is the main loop of the game, and each frame performs the following steps in sequence:

            //
            // 14a. Adventure mode wind effect: The mist wind mechanism will apply horizontal wind force to the ball in flight.

            //      Use a sine function to calculate the wind direction, causing the ball's flight path to deviate unpredictably.

            ApplyAdventureBallWind(gameplayDt);

            // 14b. Basketball physics update: including gravity, parabolic flight, rebound, basket detection, etc.

            //      The left and right baskets are passed in for collision detection (to determine whether the ball passes through the basket).

            Ball.Update(gameplayDt, basketLeft, basketRight);

            // 14c. Player updates: AI decisions, movements, animation state machines, skill cooldowns, etc. for each player.

            //      Update the left team first and then the right team. The order has no impact on the result (pure logical update).

            foreach (var player in playersLeft)
            {
                player.Update(gameplayDt);
            }
            foreach (var player in playersRight)
            {
                player.Update(gameplayDt);
            }

            // 14d. Collision detection triple:
            //      ResolvePlayerBlocking — push two main players apart when they physically overlap (based on mass ratio),

            //                             At the same time interrupting the rush into the defender.

            //      TryBlockBall — Checks if a player can block the ball (the ball is blockable and the player is within range).

            //      TryPickupLooseBall — Checks if a player can pick up an uncontrolled ball (nearest player gets the ball).

            ResolvePlayerBlocking();
            TryBlockBall();
            TryPickupLooseBall();

            // 14e. Tutorial post-processing: The tutorial process is updated after the game physics to detect whether the player has completed the operation goal.

            tutorialFlow?.UpdateAfterGameplay(dt);

            // ──Level 15: Match Timer──────────────────────────────────────
            // There is no countdown in training mode and SuperShot - there is no time limit for training, and there is an independent timer for SuperShot.

            // The game time is accumulated every frame, and the countdown display on the HUD is updated. When the time is up, the final process is triggered.

            // (BeginEndOfTime plays the buzzer and handles the delayed determination of the ball in the air).
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
        /// Called when the ball passes through the basket. Calculate scores, update scores, display messages on the HUD, and schedule restarts for the opposing team.
        /// This is the core processing function of scoring, and is called back by the basketball collision detection module after it determines that the ball has entered the basket.

        /// Note that scoringSide is the "scored" half of the court, not the scoring side - the ball passing through the left basket means a goal is scored on the right side.
        /// </summary>
        /// <param name="scoringSide">The scoring side's half (-1 = the left basket was scored, 1 = the right basket was scored). </param>
        public void OnBallScored(int scoringSide)
        {
            // ── Step 1: Prevent repeated triggering ────────────────────────────────────

            // restartDelay > 0 means that the delay after the last score has not ended yet (about 1.15 seconds).
            // If the ball bounces into the basket during this period (for example, the ball bounces multiple times on the edge of the basket), the score should not be repeated.

            if (restartDelay > 0f)
            {
                return;
            }

            // ── Step 2: Calculate base score ──────────────────────────────────

            // teamIndex: The index of the scoring team in the array (left team=0, right team=1).
            // fallbackPoints: Determine whether the shot is a 2-point or 3-point shot based on its position.

            //   - IsThreePointer checks whether the ball is outside the three-point line when shooting

            //   - This is just the "default value" and may be modified later by skills or adventure mode

            // ResolvePointsForScore: MatchProcessor may have additional scoring rules (such as special shot bonuses)

            // ResolveAdventureScoreModifier: Adventure mode’s special mechanics may modify the score

            //   - "Double basket" mechanism: double the score
            //   - "Harvest Hour" mechanism: Score +1

            //   Note ref points - modify the original variable directly, and the function returns the prompt text (such as "DOUBLE RIM 4!")

            var teamIndex = scoringSide == -1 ? 0 : 1;
            var fallbackPoints = IsThreePointer(scoringSide) ? 3 : 2;
            var points = MatchProcessor.ResolvePointsForScore(scoringSide, fallbackPoints);
            var adventureScoreNotice = ResolveAdventureScoreModifier(ref points);

            // ── Step 3: Find the shooting player and apply his personal skill bonus ────────────────

            // MatchProcessor records the player information (which team and player number) who took the last shot.

            // Use this information to find a specific player target and let him apply his scoring skills.

            // For example, the "Carnival Jackpot" skill of some characters may randomly double the score.

            // ResolveScorePoints returns the modified score and outputs the skill prompt text through the out parameter.

            var scoringPlayer = FindPlayerBySideAndPlayerNo(MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            string scoreNotice = null;
            if (scoringPlayer != null)
            {
                points = scoringPlayer.ResolveScorePoints(points, out scoreNotice);
            }

            // ── Step 4: Update score and sync to HUD ─────────────────────────────

            // Accumulate the score of the scoring team and then refresh the score display on both sides of the HUD.

            // Dispatch sends a score signal - other players' AI may adjust its behavior accordingly

            // (For example, animation triggers such as teammates celebrating, opponents being frustrated, etc.).

            // HideCountdown Hides a countdown that may be showing (for example, if the pre-match countdown is interrupted).
            MatchData.MatchScore[teamIndex] += points;
            hud.UpdateScore(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            if (MatchProcessor.ShotPlayerNo >= 0)
            {
                PlayerSignals.Dispatch(mlpPlayerSignalType.Score, MatchProcessor.ShotSide, MatchProcessor.ShotPlayerNo);
            }
            hud.HideCountdown();

            // ── Step 5: Display score message on HUD ───────────────────────────

            // waitingForBallAfterBuzzer is true, indicating that the ball was scored after the final buzzer——

            // The score message is not displayed in this case because the "TIME!!!" message is already taken.

            // Message priority: Player skill tips > Adventure mode tips > High score general tips > Normal scores

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

            // ── Step 6: Play basket and sound effects ────────────────────────────────

            // HitNet lets the basket of the "scored team" play a shaking animation of the net bag.
            // Note: The ball went into the left basket (scoringSide==-1) → the right basket shook?
            // No - the scoringSide is the half of the scoring team, and the ball passes through the basket in that half of the court.
            // So the net bag animation is played on the basket in that half. Logically, basketRight.HitNet() corresponds to the right basket.
            // BBasket is the goal sound effect.

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

            // ── Step 7: Special treatment for final score ───────────────────────────────

            // If the ball is scored after the final buzzer, there is no need to arrange for a restart (the game is already over).
            // Directly set restartDelay to 0 and return, so that the value in Update

            // waitingForBallAfterBuzzer logic calls FinalizeEndMatch to determine the outcome.

            if (waitingForBallAfterBuzzer)
            {
                restartDelay = 0f;
                return;
            }

            // ── Step 8: Arrange for re-kickoff ──────────────────────────────────

            // restartSide = -scoringSide: The ball is given to the team that scored (our team kicks off after the opponent scores).

            // restartDelay = 1.15 seconds: wait 1.15 seconds before kicking off after scoring a goal.

            //   During this time, the goal animation and score information will be displayed, and the update will count down.
            //   After zeroing, call RestartAfterScore to perform the actual kickoff operation.
            restartSide = -scoringSide;
            restartDelay = 1.15f;
        }

        /// <summary>
        /// Search both teams for the player currently holding the ball.
        /// </summary>
        /// <param name="side">0 = search both sides, -1 = search only the left side, 1 = search only the right side. </param>
        /// <returns>The player holding the ball, returns null when no one is holding the ball. </returns>
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
        /// Finds the opponent player closest to a given player in a straight line.
        /// </summary>
        /// <param name="source">The player used to measure distance. </param>
        /// <returns>The nearest opponent, returns null if there is no opponent. </returns>
        public mlpPlayerObject FindClosestOpponent(mlpPlayerObject source)
        {
            // 1. If the incoming player is empty, return null directly.

            if (source == null)
            {
                return null;
            }

            // 2. Select the opponent team list based on the current player’s team.
            var opponents = source.Side == -1 ? playersRight : playersLeft;
            // 3. Traverse the opponent team and find the nearest opponent using square distance

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

            // 4. Return the nearest opponent (returns null if there is no opponent)

            return closest;
        }

        /// <summary>
        /// Notifies all players which teammate is currently holding the ball so they can adjust AI behavior.
        /// </summary>
        /// <param name="holderSide">The team of the player holding the ball. </param>
        /// <param name="holderPlayerNo">The index of the player holding the ball in the team. </param>
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
        /// All players are notified that a shot has been taken, and the AI ​​players can react (e.g. move to rebound position).
        /// </summary>
        /// <param name="shotSide">The team of the shooting player. </param>
        /// <param name="shooterPlayerNo">The index of the shooting player in the team. </param>
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
        /// Notify all players that the ball is in uncontrolled possession.
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
        /// Returns another player from the same team. Always returns null in 1v1 mode since there is only one player per team.
        /// </summary>
        /// <param name="side">The team you are on (-1 = left, 1 = right). </param>
        /// <param name="playerNo">Find the player index of a teammate. </param>
        /// <returns>Team player, returns null if it does not exist. </returns>
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
        /// Returns the current score of the team at the specified half.
        /// </summary>
        /// <param name="side">The team you are on (-1 = left, 1 = right). </param>
        /// <returns>The current score of the team, 0 is returned if the data is missing. </returns>
        public int GetScoreForSide(int side)
        {
            var teamIndex = side == -1 ? 0 : 1;
            return MatchData.MatchScore != null && MatchData.MatchScore.Length > teamIndex
                ? MatchData.MatchScore[teamIndex]
                : 0;
        }

        /// <summary>
        /// Returns the number of points the specified team is ahead (positive) or behind (negative).
        /// </summary>
        /// <param name="side">The team you are on (-1 = left, 1 = right). </param>
        /// <returns>Point difference (the team's score minus the opponent's score). </returns>
        public int GetScoreLeadForSide(int side)
        {
            return GetScoreForSide(side) - GetScoreForSide(-side);
        }

        /// <summary>
        /// Check whether the last shot position of the ball is beyond the three-point line.
        /// </summary>
        /// <param name="shotSide">The shooting side's half of the court (-1 = left, 1 = right). </param>
        /// <returns>Returns true if the shot is a three-pointer. </returns>
        public bool IsCurrentShotThreePointer(int shotSide)
        {
            return Ball != null && IsThreePointer(shotSide);
        }

        /// <summary>
        /// Displays a text message on the HUD for the specified duration.
        /// </summary>
        /// <param name="message">Text to display. </param>
        /// <param name="duration">The duration (in seconds) for the message to be displayed. </param>
        public void ShowHudMessage(string message, float duration = 1.2f)
        {
            hud?.ShowMessage(message, duration);
        }

        /// <summary>
        /// Displays a smaller bonus tip on the HUD (for score bonuses and special events).
        /// </summary>
        /// <param name="message">The bonus text to display. </param>
        /// <param name="duration">The duration (seconds) for the prompt to be displayed. </param>
        public void ShowHudBonusNotice(string message, float duration = 0.9f)
        {
            hud?.ShowBonusNotice(message, duration);
        }

        /// <summary>
        /// Unpause and mark the player wishing to return to the main menu.
        /// </summary>
        public void RequestReturnToMenu()
        {
            SetPaused(false);
            ReturnToMenuRequested = true;
        }

        /// <summary>
        /// Reset scene state for tutorial practice scenes. Place the players on both sides, and you can choose to give the ball to one of them. If no one has the ball, the ball will be placed in the middle.
        /// </summary>
        /// <param name="playerPosition">The position of the player character on the court. </param>
        /// <param name="opponentPosition">The position of the opponent character on the court. </param>
        /// <param name="givePlayerBall">Whether the ball is given to the player at the start. </param>
        /// <param name="giveOpponentBall">Whether to give the ball to the opponent at the start. </param>
        /// <param name="playerFacing">Player facing (-1 = left, 1 = right). </param>
        /// <param name="opponentFacing">Opponent Facing. </param>
        public void TutorialResetScenario(
            Vector2 playerPosition,
            Vector2 opponentPosition,
            bool givePlayerBall,
            bool giveOpponentBall,
            float playerFacing,
            float opponentFacing)
        {
            // 1. Reset game processor and special shot markers

            MatchProcessor.Reset();
            IsSuperShot = false;
            IsAlleyOop = false;
            restartDelay = 0f;
            waitingForBallAfterBuzzer = false;

            // 2. Reset the basketball and all players to the initial state
            Ball.Restart();
            foreach (var leftPlayer in playersLeft)
            {
                leftPlayer.Restart(0);
            }

            foreach (var rightPlayer in playersRight)
            {
                rightPlayer.Restart(0);
            }

            // 3. Return early if players are missing

            if (playersLeft.Count == 0 || playersRight.Count == 0)
            {
                return;
            }

            // 4. Move the player and opponent to the designated position and direction respectively

            var left = playersLeft[0];
            var right = playersRight[0];
            left.TutorialSnapTo(playerPosition, playerFacing);
            right.TutorialSnapTo(opponentPosition, opponentFacing);

            // 5. Decide who has the ball based on parameters, or put the ball between them
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
        /// Try to steal from your opponent. Check all opponents on the opposing team who are within tackling range and steal the ball from the nearest opponent.
        /// </summary>
        /// <param name="thief">The player who attempted the tackle. </param>
        /// <param name="facingDirection">The direction the tackler is facing (-1 = left, 1 = right). </param>
        /// <returns>Returns true when a tackle attempt has been initiated (regardless of whether the ball was actually captured). </returns>
        public bool TryStealBall(mlpPlayerObject thief, float facingDirection)
        {
            // 1. Pre-inspection: No steals are allowed during the handover of the ball, during the pre-game countdown, before the game starts or during a super shot.

            if (thief == null || restartDelay > 0f || preMatchCountdown || !isPlaying || IsSuperShot)
            {
                return false;
            }

            // 2. Determine the opponent team and calculate the tackling distance (basic distance + player bonus)
            var opponents = thief.Side == -1 ? playersRight : playersLeft;
            var stealDistance = mlpObjectsData.StealDistance + thief.GetStealDistanceBonus();
            // 3. Traverse the opponent to find the nearest stealable target

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

            // 4. There is no target to steal, and failure is returned.

            if (target == null)
            {
                return false;
            }

            // 5. Perform a steal on the target, send a signal and play sound effects when successful
            var stoleBall = target.GetBeStolen(thief.Position.x);
            if (stoleBall)
            {
                PlayerSignals.Dispatch(mlpPlayerSignalType.StealSuccess, thief.Side, thief.PlayerNo);
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel);
            }

            return true;
        }

        /// <summary>
        /// Check if two main players physically overlap and push them away. At the same time interrupting the rush into the defender.
        /// </summary>
        private void ResolvePlayerBlocking()
        {
            // 1. Pre-check: both sides need to have players and are not in super shooting state

            if (playersLeft.Count == 0 || playersRight.Count == 0 || IsSuperShot)
            {
                return;
            }

            // 2. Obtain the main players from both sides and confirm that they support ground collision.
            var left = playersLeft[0];
            var right = playersRight[0];
            if (left == null || right == null || !left.CanResolveGroundBlock || !right.CanResolveGroundBlock)
            {
                return;
            }

            // 3. At least one party needs to have a collision body

            if (!left.HasGroundBlockBody && !right.HasGroundBlockBody)
            {
                return;
            }

            // 4. Calculate the X-axis overlap, skip if there is no overlap

            var deltaX = right.Position.x - left.Position.x;
            var overlapX = mlpObjectsData.BlockWidth - Mathf.Abs(deltaX);
            if (overlapX <= 0f)
            {
                return;
            }

            // 5. Calculate the Y-axis overlap, skip if there is no overlap

            var overlapY = mlpObjectsData.BlockHeight - Mathf.Abs(right.Position.y - left.Position.y);
            if (overlapY <= 0f)
            {
                return;
            }

            // 6. Check whether one party is moving towards the other party. If neither party is moving, skip it.

            var leftApproaching = left.IsMovingToward(right);
            var rightApproaching = right.IsMovingToward(left);
            if (!leftApproaching && !rightApproaching)
            {
                return;
            }

            // 7. Push the two people away according to the mass ratio of the two parties (the one with greater mass moves less)

            var leftMass = left.GetCollisionMass();
            var rightMass = right.GetCollisionMass();
            var totalMass = Mathf.Max(0.001f, leftMass + rightMass);
            var separationSign = deltaX >= 0f ? 1f : -1f;
            left.ApplyHorizontalSeparation(-overlapX * (rightMass / totalMass) * separationSign);
            right.ApplyHorizontalSeparation(overlapX * (leftMass / totalMass) * separationSign);

            // 8. If one party is sprinting and hits the opponent with a collision object, interrupt the sprint.
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
        /// Check to see if the ball was shot from beyond the three-point line (relative to the scoring team's basket).
        /// </summary>
        /// <param name="scoringSide">The half of the court where the scoring team's basket is located. </param>
        /// <returns>Returns true if the shot is a three-pointer. </returns>
        private bool IsThreePointer(int scoringSide)
        {
            if (scoringSide == -1)
            {
                return Ball.LastShotX < mlpConstants.Width - mlpObjectsData.ThreePointsDistance;
            }

            return Ball.LastShotX > mlpObjectsData.ThreePointsDistance;
        }

        /// <summary>
        /// Create player objects for both teams based on current match data (character ID, brain string, skill level). Training mode only creates the left team.
        /// </summary>
        private void BuildPlayers()
        {
            // 1. Read the game data and determine the number of people in each team (training mode only has 1 team)

            var match = MatchData;
            const int playersPerTeam = 1;
            var teamCount = mlpInventory.Instance.GameMode == mlpGameModeIds.Training ? 1 : 2;

            // 2. Create players for each team: read brain controller ID (P=human, B=AI, T=tutorial) and skill level

            for (var teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                for (var playerNo = 0; playerNo < playersPerTeam; playerNo++)
                {
                    var brain = match.Pb[teamIndex].Length > playerNo ? match.Pb[teamIndex][playerNo] : (teamIndex == 0 ? "P0" : "B0");
                    var skill = match.Skills[teamIndex].Length > playerNo ? match.Skills[teamIndex][playerNo] : 0;

                    // 3. Create player objects and add them to the corresponding team list
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
        /// Reset all status for a new match: score, timer, ball position, player position and HUD status. Displays pre-match countdown in non-training mode.
        /// </summary>
        /// <param name="regularTime">true indicates the normal length of the game, false indicates the overtime duration. </param>
        private void StartMatch(bool regularTime)
        {
            // 1. Read game mode and adventure level information
            var inventory = mlpInventory.Instance;
            var gameMode = inventory.GameMode;
            adventureLevel = inventory.IsAdventureActive ? inventory.Adventure.CurrentLevel : null;
            lastAdventureCue = mlpAdventureMechanic.BasicDuel;
            adventureCueWasActive = false;

            // 2. Set the game status: training/tutorial mode starts directly, other modes display countdown

            isTraining = gameMode == mlpGameModeIds.Training || gameMode == mlpGameModeIds.Tutorial;
            isPlaying = isTraining;
            isPaused = false;
            matchTime = 0f;
            regularMatchTimeActive = regularTime && !isTraining;
            endTime = isTraining ? 99999f : mlpQuickTestSettings.GetMatchTime(regularTime);

            // 3. Reset score and HUD display

            MatchData.ResetScore();
            hud.UpdateScore(0, 0);
            hud.SetTimerVisible(!isTraining);
            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
            hud.HidePauseOverlay();

            // 4. Clear all intermediate game states
            postMatchDelay = 0f;
            postMatchWinner = 0;
            overtimePending = false;
            waitingForBallAfterBuzzer = false;
            IsSuperShot = false;
            IsAlleyOop = false;
            ReturnToMenuRequested = false;
            AdvanceFlowRequested = false;
            MatchProcessor.Reset();

            // 5. Reset the positions of all players and start the countdown before the game.
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
        /// Handle pause commands: toggle pause, resume with countdown, or return to main menu.
        /// </summary>
        /// <param name="command">The pause action to perform (None, Toggle, Resume, or Menu). </param>
        private void HandlePauseCommand(mlpPauseCommand command)
        {
            // 1. Pre-check: Ignore the pause operation when there is no command, during countdown, post-match delay or settlement interface.

            if (command == mlpPauseCommand.None || pauseResumeCountdown || postMatchDelay > 0f || hud.IsPostMatchVisible)
            {
                return;
            }

            // 2. Execute the corresponding pause operation according to the command type

            switch (command)
            {
                case mlpPauseCommand.Toggle:
                    // 3. Switch pause: resume if paused, pause if not paused
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
                    // 4. Recovery: Start recovery countdown

                    BeginPauseResumeCountdown();
                    break;
                case mlpPauseCommand.Menu:
                    // 5. Back to menu: Unpause and mark return request

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
        /// <param name="paused">true to pause, false to cancel pause. </param>
        private void SetPaused(bool paused)
        {
            // 1. If the status does not change, only show/hide the pause mask synchronously
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

            // 2. Update pause status

            isPaused = paused;
            if (paused)
            {
                // 3. Pause: clear the resume countdown and display the pause mask

                pauseResumeCountdown = false;
                hud.ShowPauseOverlay();
            }
            else
            {
                // 4. Unpause: clear countdown, hide pause mask and resume countdown
                pauseResumeCountdown = false;
                hud.HidePauseOverlay();
                hud.EndResumeCountdown();
            }
        }

        /// <summary>
        /// Reset ball and player positions for a new attack. Give the ball to the player on the left in training mode, otherwise give it to the designated side (if non-zero).
        /// </summary>
        /// <param name="side">Which side has the ball (-1 = left, 1 = right, 0 = no one). </param>
        private void Restart(int side)
        {
            // 1. Reset basketball and game processor state
            Ball.Restart();
            IsSuperShot = false;
            IsAlleyOop = false;
            MatchProcessor.Reset();
            // 2. Reset all player positions and status
            foreach (var player in playersLeft)
            {
                player.Restart(side);
            }

            foreach (var player in playersRight)
            {
                player.Restart(side);
            }

            // 3. Distribute the ball: to the left side in training mode and to the designated side in official games.
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
        /// The game restarts after a score is scored. Give the ball to the team that scored.
        /// </summary>
        private void RestartAfterScore()
        {
            // 1. Reset the match processor

            MatchProcessor.Reset();
            // 2. Notify all players that the ball is out of control

            foreach (var player in playersLeft)
            {
                player.NotifyBallLoose();
            }

            foreach (var player in playersRight)
            {
                player.NotifyBallLoose();
            }

            // 3. Re-open the ball by scoring the opponent's ball
            Restart(restartSide);
        }

        /// <summary>
        /// Handle the buzzer at the end of game time. If the ball is still in flight, wait for the ball to hit the ground before determining the winner.
        /// </summary>
        private void BeginEndOfTime()
        {
            // 1. Lock the game time and play the final buzzer

            matchTime = endTime;
            restartDelay = 0f;
            preMatchCountdown = false;
            waitingForBallAfterBuzzer = false;
            mlpAudio.Instance?.Play(mlpAssets.Sounds.MBuzzer);
            hud.HideCountdown();
            hud.UpdateTimer(0f);

            // 2. If the basketball is still flying in the air, wait until it hits the ground before determining the outcome.
            if (IsBallInGame())
            {
                waitingForBallAfterBuzzer = true;
                return;
            }

            // 3. The ball has stopped and the result of the game is determined directly.

            FinalizeEndMatch();
        }

        /// <summary>
        /// Determine the winner (a tie will enter overtime) and display the corresponding end-of-game message.

        /// </summary>
        private void FinalizeEndMatch()
        {
            // 1. Stop the game and determine the winner
            isPlaying = false;
            waitingForBallAfterBuzzer = false;
            var winner = MatchData.WhoWins();

            // 2. If there is a tie, it will enter overtime, otherwise "TIME!!!" will be displayed and the result will be displayed after a delay.
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
        /// Called after the post-game delay has ended. Enter overtime or display the final results interface.
        /// </summary>
        private void ResolvePostMatchDelay()
        {
            // 1. Clear the post-match delay timer
            postMatchDelay = 0f;
            // 2. If overtime is needed, start overtime

            if (overtimePending)
            {
                overtimePending = false;
                StartOvertime();
                return;
            }

            // 3. When there is a winner, the post-match settlement interface will be displayed.

            if (postMatchWinner != 0)
            {
                hud.ShowPostMatch(postMatchWinner, MatchData.MatchScore[0], MatchData.MatchScore[1]);
            }
        }

        /// <summary>
        /// Reset games for overtime, with shorter timers and a new pre-match countdown.
        /// </summary>
        private void StartOvertime()
        {
            // 1. Reset timer to overtime duration

            matchTime = 0f;
            endTime = mlpConstants.OvertimeTime;
            regularMatchTimeActive = false;
            waitingForBallAfterBuzzer = false;
            // 2. Clean up HUD display

            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
            // 3. Reset player and ball positions and start pre-match countdown

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
            // 1. No synchronization is required in informal competition or training mode

            if (!regularMatchTimeActive || isTraining)
            {
                return false;
            }

            // 2. If the game duration in the quick test settings changes, update it synchronously

            var targetEndTime = mlpQuickTestSettings.GetMatchTime(true);
            if (!Mathf.Approximately(endTime, targetEndTime))
            {
                endTime = targetEndTime;
                hud.UpdateTimer(Mathf.Max(0f, endTime - matchTime));
            }

            // 3. If the game is not in progress, the countdown is in progress, the ball is waiting for the ball to hit the ground, or the time has not expired, there is no need to trigger the end game.

            if (!isPlaying || preMatchCountdown || waitingForBallAfterBuzzer || matchTime < endTime)
            {
                return false;
            }

            // 4. The time is up, trigger the ending

            BeginEndOfTime();
            return true;
        }

        private void UpdateAdventureMechanics(float dt)
        {
            // 1. Skip non-adventure levels, training or not competing

            if (adventureLevel == null || isTraining || !isPlaying)
            {
                arena?.UpdateFogWindFx(false, 0f, 0f);
                return;
            }

            // 2. Get the currently effective adventure mechanism and check whether it is activated

            var mechanic = GetActiveAdventureMechanic();
            var active = IsAdventureMechanicCurrentlyActive(mechanic);
            // 3. When the mechanism is first activated or switched, a prompt is displayed on the HUD

            if (active && (!adventureCueWasActive || mechanic != lastAdventureCue))
            {
                hud.ShowMessage(GetAdventureMechanicCue(mechanic), 1.15f, false);
            }

            // 4. Record the current mechanism status and use it to detect changes in the next frame

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

            // 5. Perform special effects based on mechanism type (such as automatic charging of special moves)

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
            // 1. Skip non-adventure levels or when the ball does not exist

            if (adventureLevel == null || Ball == null)
            {
                return;
            }

            // 2. Check whether the current mechanism is Mist Wind and is active

            var mechanic = GetActiveAdventureMechanic();
            if (mechanic != mlpAdventureMechanic.FogWind || !IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return;
            }

            // 3. Only apply wind force to the flying ball (shooting, basket, block, alley-oop status)

            if (Ball.State != "shooting" && Ball.State != "basket" && Ball.State != "block" && Ball.State != "alleyOop")
            {
                return;
            }

            // 4. Use the sine function to calculate the wind direction and apply horizontal wind force to the ball.

            var gustWave = GetAdventureFogWindWave();
            var direction = gustWave >= 0f ? 1f : -1f;
            var gustStrength = ResolveAdventureFogWindStrength(gustWave);
            Ball.Velocity.x += direction * AdventureFogWindForce * gustStrength * dt;
        }

        private string ResolveAdventureScoreModifier(ref int points)
        {
            // 1. Return directly to non-adventure levels

            if (adventureLevel == null)
            {
                return null;
            }

            // 2. Get the current mechanism. If it is not activated, the score will not be modified.

            var mechanic = GetActiveAdventureMechanic();
            if (!IsAdventureMechanicCurrentlyActive(mechanic))
            {
                return null;
            }

            // 3. Double basket: double the score

            if (mechanic == mlpAdventureMechanic.DoubleHoop)
            {
                points *= 2;
                return $"DOUBLE RIM {points}!";
            }

            // 4. Harvest time: Add 1 to the score

            if (mechanic == mlpAdventureMechanic.HarvestTime)
            {
                points += 1;
                return $"HARVEST +1 {points}!";
            }

            // 5. Other mechanisms do not affect scores

            return null;
        }

        private mlpAdventureMechanic GetActiveAdventureMechanic()
        {
            // 1. Non-adventure levels return to the default normal duel.

            if (adventureLevel == null)
            {
                return mlpAdventureMechanic.BasicDuel;
            }

            // 2. Non-mixed mechanism levels directly return to the mechanism of that level.

            if (adventureLevel.Mechanic != mlpAdventureMechanic.MoonLanternMix)
            {
                return adventureLevel.Mechanic;
            }

            // 3. Moon Lantern Mixed Level: The mechanism rotates every 10 seconds (Charge->Double->Wind->Blood Moon)
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
                // 1. Normal Duel: Always active

                case mlpAdventureMechanic.BasicDuel:
                    return true;
                // 2. Double basket: always activated in mixed levels, otherwise activated periodically (effective in the first 6 seconds of every 12 seconds)

                case mlpAdventureMechanic.DoubleHoop:
                    return (adventureLevel != null &&
                            adventureLevel.Mechanic == mlpAdventureMechanic.MoonLanternMix) ||
                           Mathf.Repeat(matchTime, AdventureDoubleRimCycle) < AdventureDoubleRimActiveTime;
                // 3. Blood Moon: Always active

                case mlpAdventureMechanic.BloodMoon:
                    return true;
                // 4. Harvest Moment: Activates only in the last 15 seconds

                case mlpAdventureMechanic.HarvestTime:
                    return RemainingMatchTime <= 15f;
                // 5. Other mechanisms (Candy Charge, Candle Ring, Mist Wind, etc.): Always activated

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
        /// Determine whether the ball is in flight (shooting, basket, dunk or blocked).
        /// </summary>
        /// <returns>Returns true if the ball is still in play. </returns>
        private bool IsBallInGame()
        {
            return Ball != null &&
                   (Ball.State == "shooting" ||
                    Ball.State == "basket" ||
                    Ball.State == "dunk" ||
                    Ball.State == "block");
        }

        /// <summary>
        /// Determine whether the ball has hit the ground or entered the basket after the buzzer sounds in order to complete the game.
        /// </summary>
        /// <returns>Returns true when the ball has stabilized (bounced, entered the basket, or disappeared). </returns>
        private bool HasWaitingBallResolved()
        {
            return Ball == null || Ball.State == "bounce" || Ball.State == "score";
        }

        /// <summary>
        /// Check to see if any player is close enough to the uncontrolled ball. The closest player will get the ball.
        /// </summary>
        private void TryPickupLooseBall()
        {
            // 1. Skip when the ball does not exist or cannot be picked up.

            if (Ball == null || !Ball.CanBeTakenInHands)
            {
                return;
            }

            // 2. Traverse the left and right teams and find the player closest to the ball who can pick up the ball.
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

            // 3. If no one can pick up the ball, return it
            if (picker == null)
            {
                return;
            }

            // 4. Let the nearest player pick up the ball and play the sound effect of picking up the ball.
            picker.TakeBallInHands();
            mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel);
        }

        /// <summary>
        /// Check to see if any player can block the ball while it is in a blockable condition. Returns true if the block is successful.
        /// </summary>
        /// <returns>Returns true when the ball is blocked by a player. </returns>
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
        /// Check to see if any player can deflect (deflect) the ball. Similar to Block but applies to different ball conditions.
        /// </summary>
        /// <returns>Returns true if the ball is deflected by a player. </returns>
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
    /// Game Builder: Configures match data and creates game core instances based on the currently selected game mode (Quick Match, Tournament, Adventure, etc.).
    /// </summary>
    public sealed class mlpGameBuilder
    {
        /// <summary>
        /// Create and start a new game core. Set match data based on the current game mode (Quick Match, Tournament, Adventure, Training, Tutorial, or Two Players) before launching.
        /// </summary>
        /// <param name="root">The parent Transform of all generated game objects. </param>
        /// <returns>The initialized game core instance can be used for every frame update. </returns>
        public mlpGameCore Build(Transform root)
        {
            // 1. Get the global item list instance

            var inventory = mlpInventory.Instance;
            // 2. If the competition has been pre-configured, use it directly (skip the configuration step)

            if (inventory.MatchPrepared)
            {
                inventory.MatchPrepared = false;
            }
            // 3. Training mode: use training characters and ball skins to configure the game
            else if (inventory.GameMode == mlpGameModeIds.Training)
            {
                inventory.MatchData.StartTraining(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            // 4. Tutorial mode: Use tutorial configuration

            else if (inventory.GameMode == mlpGameModeIds.Tutorial)
            {
                inventory.MatchData.StartTutorial(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
            }
            // 5. Start over: keep the existing configuration and only reset the score

            else if (inventory.MatchData.Restarted)
            {
                inventory.MatchData.Restarted = false;
                inventory.MatchData.ResetScore();
            }
            // 6. Tournament Mode: Use the current opponent configuration of the tournament

            else if (inventory.IsTournamentActive)
            {
                inventory.MatchData.StartTournamentMatch(inventory.Tournament, inventory.SelectedTournamentBallSelection);
            }
            // 7. Adventure mode: use adventure level configuration

            else if (inventory.IsAdventureActive)
            {
                inventory.MatchData.StartAdventureMatch(inventory.Adventure, inventory.Difficulty);
            }
            // 8. Quick Match: using selected character and difficulty

            else if (inventory.GameMode == mlpGameModeIds.QuickMatch)
            {
                inventory.MatchData.StartQuickMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            // 9. Random quick match: randomly select opponents
            else if (inventory.GameMode == mlpGameModeIds.RandomQuick)
            {
                inventory.MatchData.StartRandomMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            // 10. Two-player battle: use characters selected by both parties

            else if (inventory.GameMode == mlpGameModeIds.TwoPlayers)
            {
                inventory.MatchData.StartPlayers2Match(inventory.SelectedVersusBallSelection);
            }

            // 11. Create a game core instance and start the match

            var core = new mlpGameCore(root);
            core.Start();
            return core;
        }
    }
}
