// Tournament format data and match management/Complete process of managing a 4-player tournament: randomization, semi-finals, finals, recording the results of each game, calculating rankings and results. It is also responsible for saving and loading tournament progress, allowing players to quit midway and come back to continue.

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Championship stage: None, regular season, semi-finals, third and fourth place finals, finals, completed. Identifies the stage of the tournament.
    /// </summary>
    public enum mlpTournamentStage
    {
        None,
        RegularSeason,
        SemiFinal,
        ThirdPlace,
        Final,
        Complete
    }

    /// <summary>
    /// Championship single match results: Record both sides' roles, scores and winners. In the event of a draw, the left side automatically wins.
    /// </summary>
    public sealed class mlpTournamentMatchResult
    {
        public int LeftCharacterId { get; private set; } = -1;
        public int RightCharacterId { get; private set; } = -1;
        public int LeftScore { get; private set; }
        public int RightScore { get; private set; }
        public int WinnerCharacterId { get; private set; } = -1;
        public bool Completed { get; private set; }

        /// <summary>
        /// Reset the results of this match and optionally specify the participating roles of both sides.

        /// </summary>
        public void Reset(int leftCharacterId = -1, int rightCharacterId = -1)
        {
            LeftCharacterId = leftCharacterId;
            RightCharacterId = rightCharacterId;
            LeftScore = 0;
            RightScore = 0;
            WinnerCharacterId = -1;
            Completed = false;
        }

        /// <summary>
        /// The final score is recorded and the winner is determined; in the event of a tie, the left side wins.
        /// </summary>
        public void Complete(int leftScore, int rightScore)
        {
            if (LeftCharacterId < 0 || RightCharacterId < 0)
            {
                return;
            }

            if (leftScore == rightScore)
            {
                leftScore++;
            }

            LeftScore = leftScore;
            RightScore = rightScore;
            WinnerCharacterId = leftScore > rightScore ? LeftCharacterId : RightCharacterId;
            Completed = true;
        }
    }

    /// <summary>
    /// Tournament ranking record: Record the number of wins and losses, total points and points difference of a certain character in the division for ranking.

    /// </summary>
    public sealed class mlpTournamentStandingEntry
    {
        public int CharacterId { get; private set; } = -1;
        public int DivisionIndex { get; private set; } = -1;
        public int DivisionSlot { get; private set; } = -1;
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int PointsFor { get; private set; }
        public int PointsAgainst { get; private set; }
        public int GamesPlayed => Wins + Losses;
        public int PointDiff => PointsFor - PointsAgainst;
        public float Percentage => GamesPlayed > 0 ? Wins / (float)GamesPlayed : 0f;

        /// <summary>
        /// Reset this ranking record and assign it to the specified role, division and position.

        /// </summary>
        public void Reset(int characterId, int divisionIndex, int divisionSlot)
        {
            CharacterId = characterId;
            DivisionIndex = divisionIndex;
            DivisionSlot = divisionSlot;
            Wins = 0;
            Losses = 0;
            PointsFor = 0;
            PointsAgainst = 0;
        }

        /// <summary>
        /// Add the results of a game to this ranking record, updating wins, losses and total points.
        /// </summary>
        public void ApplyResult(int scored, int allowed)
        {
            PointsFor += scored;
            PointsAgainst += allowed;
            if (scored > allowed)
            {
                Wins++;
            }
            else
            {
                Losses++;
            }
        }
    }

    /// <summary>
    /// Complete tournament data: Manage the entire process of an 8-player tournament - random division into two zones, 3 rounds of regular season, semi-finals, finals, recording of all game results, calculation of rankings. Supports saving and restoring progress.
    /// </summary>
    public sealed class mlpTournamentData
    {
        private const int DivisionCount = 2;
        private const int TeamsPerDivision = 4;
        private const int RegularSeasonRoundCount = 3;
        private const int MatchesPerRegularSeasonRound = 4;

        private static readonly int[,,] RoundRobinPairings =
        {
            { { 0, 3 }, { 1, 2 } },
            { { 0, 2 }, { 3, 1 } },
            { { 0, 1 }, { 2, 3 } }
        };

        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public bool RegularSeasonCompleted { get; private set; }
        public bool PlayerQualifiedForPlayoffs { get; private set; }
        public mlpAiDifficulty Difficulty { get; private set; }
        public mlpTournamentStage CurrentStage { get; private set; }
        public int CurrentOpponentCharacterId { get; private set; } = -1;
        public int PlayerCharacterId { get; private set; } = -1;
        public int ChampionCharacterId { get; private set; } = -1;
        public int PlayerPlacement { get; private set; }
        public int CurrentRegularSeasonRoundIndex { get; private set; }
        public bool HasPendingPlayerMatch => Active && !Completed && CurrentOpponentCharacterId >= 0;

        public int[][] DivisionEntrantCharacterIds { get; } =
        {
            new[] { -1, -1, -1, -1 },
            new[] { -1, -1, -1, -1 }
        };

        public mlpTournamentStandingEntry[][] DivisionStandings { get; } =
        {
            new[]
            {
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry()
            },
            new[]
            {
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry()
            }
        };

        public mlpTournamentMatchResult[][] RegularSeasonRounds { get; } =
        {
            new[]
            {
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult()
            },
            new[]
            {
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult()
            },
            new[]
            {
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult()
            }
        };

        public mlpTournamentMatchResult[] SemiFinalResults { get; } =
        {
            new mlpTournamentMatchResult(),
            new mlpTournamentMatchResult()
        };

        public mlpTournamentMatchResult ThirdPlaceResult { get; } = new mlpTournamentMatchResult();
        public mlpTournamentMatchResult FinalResult { get; } = new mlpTournamentMatchResult();

        /// <summary>
        /// Clears all tournament status, resetting every game result and ranking record.

        /// </summary>
        public void Reset()
        {
            // 1. Reset all basic status markers and current stage information
            Active = false;
            Completed = false;
            RegularSeasonCompleted = false;
            PlayerQualifiedForPlayoffs = false;
            Difficulty = mlpAiDifficulty.Normal;
            CurrentStage = mlpTournamentStage.None;
            CurrentOpponentCharacterId = -1;
            PlayerCharacterId = -1;
            ChampionCharacterId = -1;
            PlayerPlacement = 0;
            CurrentRegularSeasonRoundIndex = 0;

            // 2. Traverse the two partitions and reset all contestant IDs and ranking records

            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    DivisionEntrantCharacterIds[division][slot] = -1;
                    DivisionStandings[division][slot].Reset(-1, division, slot);
                }
            }

            // 3. Traverse all regular season rounds and reset the results of each game

            for (var round = 0; round < RegularSeasonRoundCount; round++)
            {
                for (var matchIndex = 0; matchIndex < MatchesPerRegularSeasonRound; matchIndex++)
                {
                    RegularSeasonRounds[round][matchIndex].Reset();
                }
            }

            // 4. Reset all playoff game results
            SemiFinalResults[0].Reset();
            SemiFinalResults[1].Reset();
            ThirdPlaceResult.Reset();
            FinalResult.Reset();
        }

        /// <summary>
        /// Create a new tournament with specified player characters and difficulty, dividing participants into two divisions.

        /// </summary>
        public bool Create(int playerCharacterId, mlpAiDifficulty difficulty)
        {
            // 1. Reset all data and check if there are enough characters (at least 8)

            Reset();
            var activeCharacters = mlpPlayersData.GetActiveCharacterIds();
            if (activeCharacters.Length < DivisionCount * TeamsPerDivision)
            {
                return false;
            }

            // 2. Set the basic information of the tournament: activation status, difficulty, player role

            Active = true;
            Difficulty = difficulty;
            PlayerCharacterId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);

            // 3. Randomly select 7 opponents from the remaining characters and shuffle them.

            var availableOpponents = new List<int>(activeCharacters.Length - 1);
            for (var i = 0; i < activeCharacters.Length; i++)
            {
                if (activeCharacters[i] != PlayerCharacterId)
                {
                    availableOpponents.Add(activeCharacters[i]);
                }
            }
            Shuffle(availableOpponents);

            // 4. Assigned to two areas: Area A contains players + 3 opponents, Area B contains 4 opponents

            DivisionEntrantCharacterIds[0][0] = PlayerCharacterId;
            for (var slot = 1; slot < TeamsPerDivision; slot++)
            {
                DivisionEntrantCharacterIds[0][slot] = availableOpponents[slot - 1];
            }
            for (var slot = 0; slot < TeamsPerDivision; slot++)
            {
                DivisionEntrantCharacterIds[1][slot] = availableOpponents[slot + TeamsPerDivision - 1];
            }

            // 5. Initialize the ranking records of each area
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    DivisionStandings[division][slot].Reset(
                        DivisionEntrantCharacterIds[division][slot],
                        division,
                        slot);
                }
            }

            // 6. Generate regular season schedule and enter the first round

            BuildRegularSeasonSchedule();
            CurrentStage = mlpTournamentStage.RegularSeason;
            CurrentRegularSeasonRoundIndex = 0;
            CurrentOpponentCharacterId = GetPlayerOpponentForRound(CurrentRegularSeasonRoundIndex);
            return true;
        }

        /// <summary>
        /// Transition from the regular season stage to the semi-finals playoff stage.

        /// </summary>
        public void BeginFinals()
        {
            if (!Active || Completed || !RegularSeasonCompleted || !PlayerQualifiedForPlayoffs)
            {
                return;
            }

            if (CurrentStage != mlpTournamentStage.RegularSeason)
            {
                return;
            }

            CurrentStage = mlpTournamentStage.SemiFinal;
            CurrentOpponentCharacterId = GetPlayerOpponentForMatchSet(SemiFinalResults);
        }

        /// <summary>
        /// Record player results and advance to the next stage of the tournament.

        /// </summary>
        public void ApplyCurrentMatchResult(int playerScore, int opponentScore)
        {
            if (!Active || Completed)
            {
                return;
            }

            switch (CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
                    ApplyRegularSeasonResult(playerScore, opponentScore);
                    break;
                case mlpTournamentStage.SemiFinal:
                    ApplySemiFinalResult(playerScore, opponentScore);
                    break;
                case mlpTournamentStage.ThirdPlace:
                    ApplyPlacementResult(ThirdPlaceResult, playerScore, opponentScore);
                    break;
                case mlpTournamentStage.Final:
                    ApplyPlacementResult(FinalResult, playerScore, opponentScore);
                    break;
            }
        }

        /// <summary>
        /// Returns the rankings for the specified division, sorted by wins, point differential, and total points.

        /// </summary>
        public mlpTournamentStandingEntry[] GetDivisionStandings(int divisionIndex)
        {
            if (divisionIndex < 0 || divisionIndex >= DivisionCount)
            {
                return new mlpTournamentStandingEntry[0];
            }

            var standings = new mlpTournamentStandingEntry[TeamsPerDivision];
            for (var i = 0; i < TeamsPerDivision; i++)
            {
                standings[i] = DivisionStandings[divisionIndex][i];
            }

            System.Array.Sort(standings, CompareDivisionStandings);
            return standings;
        }

        /// <summary>
        /// Record the players' regular season game results, simulate the remaining games of this round, and advance to the next round or playoffs.

        /// </summary>
        private void ApplyRegularSeasonResult(int playerScore, int opponentScore)
        {
            // 1. Pre-check: Skip when the regular season is over or the round index is invalid

            if (RegularSeasonCompleted || CurrentRegularSeasonRoundIndex < 0 || CurrentRegularSeasonRoundIndex >= RegularSeasonRoundCount)
            {
                return;
            }

            // 2. Find the player’s current round of games and confirm that they are not completed.

            var playerMatch = GetPlayerMatchForRound(CurrentRegularSeasonRoundIndex);
            if (playerMatch == null || playerMatch.Completed)
            {
                return;
            }

            // 3. Record player competition results and update the rankings of both parties

            playerMatch.Complete(playerScore, opponentScore);
            ApplyStandingUpdate(playerMatch);

            // 4. Simulate other games in this round and update the rankings

            var roundMatches = RegularSeasonRounds[CurrentRegularSeasonRoundIndex];
            for (var i = 0; i < roundMatches.Length; i++)
            {
                if (roundMatches[i] == playerMatch || roundMatches[i].Completed)
                {
                    continue;
                }

                SimulateMatch(roundMatches[i]);
                ApplyStandingUpdate(roundMatches[i]);
            }

            // 5. Advance to the next round, and if there are remaining rounds, set the next opponent.

            CurrentRegularSeasonRoundIndex++;
            if (CurrentRegularSeasonRoundIndex < RegularSeasonRoundCount)
            {
                CurrentOpponentCharacterId = GetPlayerOpponentForRound(CurrentRegularSeasonRoundIndex);
                return;
            }

            // 6. The regular season is over and the playoffs are set up.

            RegularSeasonCompleted = true;
            CurrentOpponentCharacterId = -1;
            BuildPlayoffBracket();

            // 7. Automatically simulate all playoffs when the player fails to advance

            if (!PlayerQualifiedForPlayoffs)
            {
                SimulateEntirePlayoffs();
                FinalizeTournament();
            }
        }

        /// <summary>
        /// Record the player's semi-final results, simulate another semi-final, and arrange the final and third-place finals.

        /// </summary>
        private void ApplySemiFinalResult(int playerScore, int opponentScore)
        {
            // 1. Find the player's semi-finals and confirm that they are not completed.

            var playerSemi = GetPlayerMatchFromSet(SemiFinalResults);
            if (playerSemi == null || playerSemi.Completed)
            {
                return;
            }

            // 2. Record player semi-final results

            playerSemi.Complete(playerScore, opponentScore);

            // 3. Simulate another semi-final

            for (var i = 0; i < SemiFinalResults.Length; i++)
            {
                if (SemiFinalResults[i].Completed)
                {
                    continue;
                }

                SimulateMatch(SemiFinalResults[i]);
            }

            // 4. Arrange the finals and the third and fourth place finals based on the results of the semi-finals

            ConfigurePlacementMatchesFromSemiFinals();
            // 5. Players who win will advance to the finals, and players who lose will advance to the third or fourth place finals.

            if (playerSemi.WinnerCharacterId == PlayerCharacterId)
            {
                SimulateMatch(ThirdPlaceResult);
                CurrentStage = mlpTournamentStage.Final;
                CurrentOpponentCharacterId = GetOpponentCharacterId(FinalResult, PlayerCharacterId);
                return;
            }

            SimulateMatch(FinalResult);
            CurrentStage = mlpTournamentStage.ThirdPlace;
            CurrentOpponentCharacterId = GetOpponentCharacterId(ThirdPlaceResult, PlayerCharacterId);
        }

        /// <summary>
        /// Record the results of the placing match (third or fourth place final or final) and complete the tournament.

        /// </summary>
        private void ApplyPlacementResult(mlpTournamentMatchResult match, int playerScore, int opponentScore)
        {
            if (match == null || match.Completed)
            {
                return;
            }

            match.Complete(playerScore, opponentScore);
            FinalizeTournament();
        }

        /// <summary>
        /// When the finals and the third and fourth place finals are completed, the champion and player rankings will be determined.

        /// </summary>
        private void FinalizeTournament()
        {
            // 1. The finals and the third and fourth place finals must be completed to settle the settlement.

            if (!FinalResult.Completed || !ThirdPlaceResult.Completed)
            {
                return;
            }

            // 2. Determine the champion and calculate the final ranking of players

            ChampionCharacterId = FinalResult.WinnerCharacterId;
            PlayerPlacement = ResolvePlayerPlacement();
            // 3. Clear the opponents and mark the tournament complete

            CurrentOpponentCharacterId = -1;
            CurrentStage = mlpTournamentStage.Complete;
            Completed = true;
        }

        /// <summary>
        /// A player's final ranking (1st to 8th) is determined based on playoff results and regular season standings.
        /// </summary>
        private int ResolvePlayerPlacement()
        {
            // 1. Get the top four character IDs

            var champion = FinalResult.WinnerCharacterId;
            var runnerUp = GetMatchLoserCharacterId(FinalResult);
            var third = ThirdPlaceResult.WinnerCharacterId;
            var fourth = GetMatchLoserCharacterId(ThirdPlaceResult);

            // 2. Check if the player is the champion, runner-up, third runner-up or fourth place

            if (PlayerCharacterId == champion)
            {
                return 1;
            }

            if (PlayerCharacterId == runnerUp)
            {
                return 2;
            }

            if (PlayerCharacterId == third)
            {
                return 3;
            }

            if (PlayerCharacterId == fourth)
            {
                return 4;
            }

            // 3. Teams that did not advance to the playoffs will be ranked 5th-8th according to ranking.

            var nonPlayoffEntries = new List<mlpTournamentStandingEntry>(4);
            var playoffCharacters = new HashSet<int> { champion, runnerUp, third, fourth };
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    var entry = DivisionStandings[division][slot];
                    if (entry.CharacterId >= 0 && !playoffCharacters.Contains(entry.CharacterId))
                    {
                        nonPlayoffEntries.Add(entry);
                    }
                }
            }

            // 4. Find the player position after sorting

            nonPlayoffEntries.Sort(CompareOverallStandings);
            for (var i = 0; i < nonPlayoffEntries.Count; i++)
            {
                if (nonPlayoffEntries[i].CharacterId == PlayerCharacterId)
                {
                    return 5 + i;
                }
            }

            // 5. Return to 8th place

            return 8;
        }

        /// <summary>
        /// Generate a round-robin schedule for three regular season rounds, covering both divisions.

        /// </summary>
        private void BuildRegularSeasonSchedule()
        {
            // 1. Go through each round

            for (var round = 0; round < RegularSeasonRoundCount; round++)
            {
                // 2. Traverse each partition

                for (var division = 0; division < DivisionCount; division++)
                {
                    // 3. There are 2 games per region in each round, and the two teams are read from the round-robin matchmaking table.

                    for (var pair = 0; pair < 2; pair++)
                    {
                        var matchIndex = division * 2 + pair;
                        var leftSlot = RoundRobinPairings[round, pair, 0];
                        var rightSlot = RoundRobinPairings[round, pair, 1];
                        RegularSeasonRounds[round][matchIndex].Reset(
                            DivisionEntrantCharacterIds[division][leftSlot],
                            DivisionEntrantCharacterIds[division][rightSlot]);
                    }
                }
            }
        }

        /// <summary>
        /// The top two teams from each division are paired up for a semi-final matchup, and players are checked to see if they advance.

        /// </summary>
        private void BuildPlayoffBracket()
        {
            // 1. Get the rankings of two partitions

            var divisionA = GetDivisionStandings(0);
            var divisionB = GetDivisionStandings(1);

            // 2. Cross-matching: No. 1 in Zone A vs No. 2 in Zone B, No. 1 in Zone B vs No. 2 in Zone A

            SemiFinalResults[0].Reset(divisionA[0].CharacterId, divisionB[1].CharacterId);
            SemiFinalResults[1].Reset(divisionB[0].CharacterId, divisionA[1].CharacterId);
            ThirdPlaceResult.Reset();
            FinalResult.Reset();

            // 3. Check if the player is in the semi-final match

            PlayerQualifiedForPlayoffs =
                SemiFinalResults[0].LeftCharacterId == PlayerCharacterId ||
                SemiFinalResults[0].RightCharacterId == PlayerCharacterId ||
                SemiFinalResults[1].LeftCharacterId == PlayerCharacterId ||
                SemiFinalResults[1].RightCharacterId == PlayerCharacterId;
        }

        /// <summary>
        /// The finals and third-place finals are arranged based on the winners and losers of the semi-finals.

        /// </summary>
        private void ConfigurePlacementMatchesFromSemiFinals()
        {
            FinalResult.Reset(
                SemiFinalResults[0].WinnerCharacterId,
                SemiFinalResults[1].WinnerCharacterId);
            ThirdPlaceResult.Reset(
                GetMatchLoserCharacterId(SemiFinalResults[0]),
                GetMatchLoserCharacterId(SemiFinalResults[1]));
        }

        /// <summary>
        /// Automatically simulates all remaining playoff games when a player does not advance.

        /// </summary>
        private void SimulateEntirePlayoffs()
        {
            // 1. Simulate two semi-finals

            for (var i = 0; i < SemiFinalResults.Length; i++)
            {
                SimulateMatch(SemiFinalResults[i]);
            }

            // 2. Arrange the finals and the third and fourth place finals based on the results of the semi-finals

            ConfigurePlacementMatchesFromSemiFinals();
            // 3. Simulate the third and fourth place finals and finals

            SimulateMatch(ThirdPlaceResult);
            SimulateMatch(FinalResult);
        }

        /// <summary>
        /// Finds and returns the player's games during the specified regular season round, or null if not found.

        /// </summary>
        private mlpTournamentMatchResult GetPlayerMatchForRound(int roundIndex)
        {
            if (roundIndex < 0 || roundIndex >= RegularSeasonRounds.Length)
            {
                return null;
            }

            var matches = RegularSeasonRounds[roundIndex];
            for (var i = 0; i < matches.Length; i++)
            {
                if (matches[i].LeftCharacterId == PlayerCharacterId || matches[i].RightCharacterId == PlayerCharacterId)
                {
                    return matches[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Finds and returns the player's matches from the match results array, or null if not found.

        /// </summary>
        private mlpTournamentMatchResult GetPlayerMatchFromSet(mlpTournamentMatchResult[] matches)
        {
            if (matches == null)
            {
                return null;
            }

            for (var i = 0; i < matches.Length; i++)
            {
                if (matches[i].LeftCharacterId == PlayerCharacterId || matches[i].RightCharacterId == PlayerCharacterId)
                {
                    return matches[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the character ID of the player's opponent in the specified regular season round.

        /// </summary>
        private int GetPlayerOpponentForRound(int roundIndex)
        {
            return GetOpponentCharacterId(GetPlayerMatchForRound(roundIndex), PlayerCharacterId);
        }

        /// <summary>
        /// Get the player's opponent's character ID from the match results array.

        /// </summary>
        private int GetPlayerOpponentForMatchSet(mlpTournamentMatchResult[] matches)
        {
            return GetOpponentCharacterId(GetPlayerMatchFromSet(matches), PlayerCharacterId);
        }

        /// <summary>
        /// The ranking records of both teams are updated based on the results of the completed games.

        /// </summary>
        private void ApplyStandingUpdate(mlpTournamentMatchResult match)
        {
            // 1. Find the ranking records of both parties
            var leftEntry = GetStandingEntry(match.LeftCharacterId);
            var rightEntry = GetStandingEntry(match.RightCharacterId);
            if (leftEntry == null || rightEntry == null)
            {
                return;
            }

            // 2. Update the winning and losing games and scores of both sides respectively.

            leftEntry.ApplyResult(match.LeftScore, match.RightScore);
            rightEntry.ApplyResult(match.RightScore, match.LeftScore);
        }

        /// <summary>
        /// Find and return the ranking record of the specified role, or return null if not found.

        /// </summary>
        private mlpTournamentStandingEntry GetStandingEntry(int characterId)
        {
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    var entry = DivisionStandings[division][slot];
                    if (entry.CharacterId == characterId)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Sort both conference standings records by wins, point differential, total points and position index.

        /// </summary>
        private static int CompareDivisionStandings(mlpTournamentStandingEntry left, mlpTournamentStandingEntry right)
        {
            // 1. First compare the number of wins (the one with more wins will be ranked first)

            var winCompare = right.Wins.CompareTo(left.Wins);
            if (winCompare != 0)
            {
                return winCompare;
            }

            // 2. Compare the points difference when the wins are the same (higher ranked first)

            var diffCompare = right.PointDiff.CompareTo(left.PointDiff);
            if (diffCompare != 0)
            {
                return diffCompare;
            }

            // 3. If the points difference is the same, compare the total points (the one with the higher score will be ranked first)

            var pointsCompare = right.PointsFor.CompareTo(left.PointsFor);
            if (pointsCompare != 0)
            {
                return pointsCompare;
            }

            // 4. Sort by initial position when all indicators are the same

            return left.DivisionSlot.CompareTo(right.DivisionSlot);
        }

        /// <summary>
        /// Two overall ranking records sorted by partition rank, partition index, and position.

        /// </summary>
        private static int CompareOverallStandings(mlpTournamentStandingEntry left, mlpTournamentStandingEntry right)
        {
            var compare = CompareDivisionStandings(left, right);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.DivisionIndex.CompareTo(right.DivisionIndex);
            if (compare != 0)
            {
                return compare;
            }

            return left.DivisionSlot.CompareTo(right.DivisionSlot);
        }

        /// <summary>
        /// Returns the character ID of the opponent in this match based on the character ID of the known contestant.

        /// </summary>
        private static int GetOpponentCharacterId(mlpTournamentMatchResult match, int characterId)
        {
            if (match == null)
            {
                return -1;
            }

            if (match.LeftCharacterId == characterId)
            {
                return match.RightCharacterId;
            }

            if (match.RightCharacterId == characterId)
            {
                return match.LeftCharacterId;
            }

            return -1;
        }

        /// <summary>
        /// Returns the character ID of the loser in a completed match.

        /// </summary>
        private static int GetMatchLoserCharacterId(mlpTournamentMatchResult match)
        {
            if (match == null || !match.Completed)
            {
                return -1;
            }

            if (match.WinnerCharacterId == match.LeftCharacterId)
            {
                return match.RightCharacterId;
            }

            if (match.WinnerCharacterId == match.RightCharacterId)
            {
                return match.LeftCharacterId;
            }

            return -1;
        }

        /// <summary>
        /// Random scores are automatically generated for matches not played and marked as completed.

        /// </summary>
        private static void SimulateMatch(mlpTournamentMatchResult match)
        {
            if (match == null || match.Completed)
            {
                return;
            }

            var leftScore = 16 + Random.Range(0, 15);
            var rightScore = 14 + Random.Range(0, 15);
            match.Complete(leftScore, rightScore);
        }

        /// <summary>
        /// Randomly shuffle a list of integers using the Fisher-Yates algorithm.
        /// </summary>
        private static void Shuffle(List<int> values)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                var temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }
    }
}
