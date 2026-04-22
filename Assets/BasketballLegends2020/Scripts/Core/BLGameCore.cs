using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLGameCore
    {
        private readonly Transform root;
        private readonly List<BLPlayerObject> playersLeft = new List<BLPlayerObject>();
        private readonly List<BLPlayerObject> playersRight = new List<BLPlayerObject>();
        private BLArenaObject arena;
        private BLBasketObject basketLeft;
        private BLBasketObject basketRight;
        private BLHudView hud;
        private bool isPlaying;
        private bool isPaused;
        private bool isTraining;
        private float matchTime;
        private float endTime;
        private float restartDelay;
        private int restartSide;
        private bool preMatchCountdown;
        private bool waitingForBallAfterBuzzer;
        private float postMatchDelay;
        private int postMatchWinner;
        private bool overtimePending;

        public BLBallObject Ball { get; private set; }
        public BLMatchData MatchData => BLInventory.Instance.MatchData;
        public BLPlayerSignalBus PlayerSignals { get; } = new BLPlayerSignalBus();
        public BLMatchProcessor MatchProcessor { get; } = new BLMatchProcessor();
        public bool ReturnToMenuRequested { get; private set; }
        public bool AdvanceFlowRequested { get; private set; }
        public bool IsSuperShot { get; set; }
        public bool IsAlleyOop { get; set; }
        public IReadOnlyList<BLPlayerObject> PlayersLeft => playersLeft;
        public IReadOnlyList<BLPlayerObject> PlayersRight => playersRight;
        public BLBasketObject BasketLeft => basketLeft;
        public BLBasketObject BasketRight => basketRight;

        public BLGameCore(Transform root)
        {
            this.root = root;
        }

        public void Start()
        {
            BLPlayersData.SetupPlayers();
            arena = new BLArenaObject(root);
            basketLeft = new BLBasketObject(-1, root);
            basketRight = new BLBasketObject(1, root);
            Ball = new BLBallObject(this, root);
            hud = new BLHudView(root, MatchData);

            BuildPlayers();
            StartMatch(true);
        }

        public void Update(float dt)
        {
            if (Input.GetKeyDown(KeyCode.P) && postMatchDelay <= 0f && !hud.IsPostMatchVisible)
            {
                HandlePauseCommand(BLPauseCommand.Toggle);
            }

            hud.Update(dt);
            HandlePauseCommand(hud.ConsumePauseCommand());
            if (ReturnToMenuRequested)
            {
                return;
            }

            if (isPaused)
            {
                return;
            }

            basketLeft.Update(dt);
            basketRight.Update(dt);

            if (postMatchDelay > 0f)
            {
                postMatchDelay -= dt;
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
                    if (BLInventory.Instance.IsTournamentActive)
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
                    player.TickPreMatch(dt);
                }

                foreach (var player in playersRight)
                {
                    player.TickPreMatch(dt);
                }

                preMatchCountdown = hud.UpdateCountdown(dt);
                if (!preMatchCountdown)
                {
                    isPlaying = true;
                    BLAudio.Instance?.Play(BLAssets.Sounds.MWhistle);
                }

                return;
            }

            if (restartDelay > 0f)
            {
                restartDelay -= dt;
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
                Ball.Update(dt, basketLeft, basketRight);
                TryBlockBall();
                if (HasWaitingBallResolved())
                {
                    FinalizeEndMatch();
                }

                return;
            }

            Ball.Update(dt, basketLeft, basketRight);
            foreach (var player in playersLeft)
            {
                player.Update(dt);
            }

            foreach (var player in playersRight)
            {
                player.Update(dt);
            }

            ResolvePlayerBlocking();
            TryBlockBall();
            TryPickupLooseBall();

            if (!isTraining && !IsSuperShot)
            {
                matchTime += dt;
                hud.UpdateTimer(Mathf.Max(0f, endTime - matchTime));
                if (matchTime >= endTime)
                {
                    BeginEndOfTime();
                }
            }
        }

        public void OnBallScored(int scoringSide)
        {
            if (restartDelay > 0f)
            {
                return;
            }

            var teamIndex = scoringSide == -1 ? 0 : 1;
            var fallbackPoints = IsThreePointer(scoringSide) ? 3 : 2;
            var points = MatchProcessor.ResolvePointsForScore(scoringSide, fallbackPoints);
            MatchData.MatchScore[teamIndex] += points;
            hud.UpdateScore(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            hud.HideCountdown();
            if (!waitingForBallAfterBuzzer)
            {
                hud.ShowMessage(points == 3 ? "3 POINTS!" : "BASKET!");
            }

            if (scoringSide == -1)
            {
                basketRight.HitNet();
            }
            else
            {
                basketLeft.HitNet();
            }

            BLAudio.Instance?.Play(BLAssets.Sounds.BBasket);
            if (waitingForBallAfterBuzzer)
            {
                restartDelay = 0f;
                return;
            }

            restartSide = -scoringSide;
            restartDelay = 1.15f;
        }

        public BLPlayerObject FindBallHolder(int side = 0)
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

        public BLPlayerObject FindClosestOpponent(BLPlayerObject source)
        {
            if (source == null)
            {
                return null;
            }

            var opponents = source.Side == -1 ? playersRight : playersLeft;
            BLPlayerObject closest = null;
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

        public BLPlayerObject GetTeamMate(int side, int playerNo)
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

        public bool TryStealBall(BLPlayerObject thief, float facingDirection)
        {
            if (thief == null || restartDelay > 0f || preMatchCountdown || !isPlaying || IsSuperShot)
            {
                return false;
            }

            var opponents = thief.Side == -1 ? playersRight : playersLeft;
            BLPlayerObject target = null;
            var bestDistance = float.MaxValue;
            foreach (var opponent in opponents)
            {
                var candidateDistance = opponent.CheckToBeStolen(thief.Position.x, facingDirection);
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
                BLAudio.Instance?.Play(BLAssets.Sounds.BSteel);
            }

            return true;
        }

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
            var overlapX = BLObjectsData.BlockWidth - Mathf.Abs(deltaX);
            if (overlapX <= 0f)
            {
                return;
            }

            var overlapY = BLObjectsData.BlockHeight - Mathf.Abs(right.Position.y - left.Position.y);
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

        private bool IsThreePointer(int scoringSide)
        {
            if (scoringSide == -1)
            {
                return Ball.LastShotX < BLConstants.Width - BLObjectsData.ThreePointsDistance;
            }

            return Ball.LastShotX > BLObjectsData.ThreePointsDistance;
        }

        private void BuildPlayers()
        {
            var match = MatchData;
            const int playersPerTeam = 1;
            var teamCount = BLInventory.Instance.GameMode == 3 ? 1 : 2;

            for (var teamIndex = 0; teamIndex < teamCount; teamIndex++)
            {
                for (var playerNo = 0; playerNo < playersPerTeam; playerNo++)
                {
                    var brain = match.Pb[teamIndex].Length > playerNo ? match.Pb[teamIndex][playerNo] : (teamIndex == 0 ? "P0" : "B0");
                    var skill = match.Skills[teamIndex].Length > playerNo ? match.Skills[teamIndex][playerNo] : 0;
                    var player = new BLPlayerObject(
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

        private void StartMatch(bool regularTime)
        {
            isTraining = BLInventory.Instance.GameMode == 3;
            isPlaying = isTraining;
            isPaused = false;
            matchTime = 0f;
            endTime = isTraining ? 99999f : (regularTime ? BLConstants.MatchTime : BLConstants.OvertimeTime);
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
            if (preMatchCountdown)
            {
                hud.StartCountdown(3f);
            }
            else
            {
                hud.ShowMessage("TRAINING", 0.85f);
                BLAudio.Instance?.Play(BLAssets.Sounds.MWhistle);
            }
        }

        private void HandlePauseCommand(BLPauseCommand command)
        {
            if (command == BLPauseCommand.None || postMatchDelay > 0f || hud.IsPostMatchVisible)
            {
                return;
            }

            switch (command)
            {
                case BLPauseCommand.Toggle:
                    SetPaused(!isPaused);
                    break;
                case BLPauseCommand.Resume:
                    SetPaused(false);
                    break;
                case BLPauseCommand.Menu:
                    SetPaused(false);
                    ReturnToMenuRequested = true;
                    break;
            }
        }

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
                hud.ShowPauseOverlay();
            }
            else
            {
                hud.HidePauseOverlay();
                hud.ShowMessage("GO!!!", 0.8f);
            }
        }

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

        private void BeginEndOfTime()
        {
            matchTime = endTime;
            restartDelay = 0f;
            preMatchCountdown = false;
            waitingForBallAfterBuzzer = false;
            BLAudio.Instance?.Play(BLAssets.Sounds.MBuzzer);
            hud.HideCountdown();
            hud.UpdateTimer(0f);
            if (IsBallInGame())
            {
                waitingForBallAfterBuzzer = true;
                return;
            }

            FinalizeEndMatch();
        }

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

        private void StartOvertime()
        {
            matchTime = 0f;
            endTime = BLConstants.OvertimeTime;
            waitingForBallAfterBuzzer = false;
            hud.UpdateTimer(endTime);
            hud.HideCountdown();
            hud.HideMessage();
            hud.HidePostMatch();
            Restart(0);
            preMatchCountdown = true;
            hud.StartCountdown(3f);
        }

        private bool IsBallInGame()
        {
            return Ball != null &&
                   (Ball.State == "shooting" ||
                    Ball.State == "basket" ||
                    Ball.State == "dunk" ||
                    Ball.State == "block");
        }

        private bool HasWaitingBallResolved()
        {
            return Ball == null || Ball.State == "bounce" || Ball.State == "score";
        }

        private void TryPickupLooseBall()
        {
            if (Ball == null || !Ball.CanBeTakenInHands)
            {
                return;
            }

            BLPlayerObject picker = null;
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
            BLAudio.Instance?.Play(BLAssets.Sounds.BSteel);
        }

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

    public sealed class BLGameBuilder
    {
        public BLGameCore Build(Transform root)
        {
            var inventory = BLInventory.Instance;
            if (inventory.MatchPrepared)
            {
                inventory.MatchPrepared = false;
            }
            else if (inventory.GameMode == 3)
            {
                inventory.MatchData.StartTraining(inventory.SelectedTrainingCharacterId, inventory.SelectedTrainingBallSelection);
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
            else if (inventory.GameMode == 2)
            {
                inventory.MatchData.StartQuickMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            else if (inventory.GameMode == 1)
            {
                inventory.MatchData.StartRandomMatch(inventory.SelectedQuickCharacterId, inventory.Difficulty, inventory.SelectedQuickBallSelection);
            }
            else if (inventory.GameMode == 4)
            {
                inventory.MatchData.StartPlayers2Match(inventory.SelectedVersusBallSelection);
            }

            var core = new BLGameCore(root);
            core.Start();
            return core;
        }
    }
}
