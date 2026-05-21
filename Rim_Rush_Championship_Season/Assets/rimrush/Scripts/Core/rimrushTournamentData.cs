using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public enum rimrushTournamentStage
    {
        None,
        RegularSeason,
        SemiFinal,
        ThirdPlace,
        Final,
        Complete
    }

    public sealed class rimrushTournamentMatchResult
    {
        public int LeftCharacterId { get; private set; } = -1;
        public int RightCharacterId { get; private set; } = -1;
        public int LeftScore { get; private set; }
        public int RightScore { get; private set; }
        public int WinnerCharacterId { get; private set; } = -1;
        public bool Completed { get; private set; }

        public void Reset(int leftCharacterId = -1, int rightCharacterId = -1)
        {
            LeftCharacterId = leftCharacterId;
            RightCharacterId = rightCharacterId;
            LeftScore = 0;
            RightScore = 0;
            WinnerCharacterId = -1;
            Completed = false;
        }

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

    public sealed class rimrushTournamentStandingEntry
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

    public sealed class rimrushTournamentData
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
        public rimrushAiDifficulty Difficulty { get; private set; }
        public rimrushTournamentStage CurrentStage { get; private set; }
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

        public rimrushTournamentStandingEntry[][] DivisionStandings { get; } =
        {
            new[]
            {
                new rimrushTournamentStandingEntry(),
                new rimrushTournamentStandingEntry(),
                new rimrushTournamentStandingEntry(),
                new rimrushTournamentStandingEntry()
            },
            new[]
            {
                new rimrushTournamentStandingEntry(),
                new rimrushTournamentStandingEntry(),
                new rimrushTournamentStandingEntry(),
                new rimrushTournamentStandingEntry()
            }
        };

        public rimrushTournamentMatchResult[][] RegularSeasonRounds { get; } =
        {
            new[]
            {
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult()
            },
            new[]
            {
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult()
            },
            new[]
            {
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult(),
                new rimrushTournamentMatchResult()
            }
        };

        public rimrushTournamentMatchResult[] SemiFinalResults { get; } =
        {
            new rimrushTournamentMatchResult(),
            new rimrushTournamentMatchResult()
        };

        public rimrushTournamentMatchResult ThirdPlaceResult { get; } = new rimrushTournamentMatchResult();
        public rimrushTournamentMatchResult FinalResult { get; } = new rimrushTournamentMatchResult();

        public void Reset()
        {
            Active = false;
            Completed = false;
            RegularSeasonCompleted = false;
            PlayerQualifiedForPlayoffs = false;
            Difficulty = rimrushAiDifficulty.Normal;
            CurrentStage = rimrushTournamentStage.None;
            CurrentOpponentCharacterId = -1;
            PlayerCharacterId = -1;
            ChampionCharacterId = -1;
            PlayerPlacement = 0;
            CurrentRegularSeasonRoundIndex = 0;

            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    DivisionEntrantCharacterIds[division][slot] = -1;
                    DivisionStandings[division][slot].Reset(-1, division, slot);
                }
            }

            for (var round = 0; round < RegularSeasonRoundCount; round++)
            {
                for (var matchIndex = 0; matchIndex < MatchesPerRegularSeasonRound; matchIndex++)
                {
                    RegularSeasonRounds[round][matchIndex].Reset();
                }
            }

            SemiFinalResults[0].Reset();
            SemiFinalResults[1].Reset();
            ThirdPlaceResult.Reset();
            FinalResult.Reset();
        }

        public bool Create(int playerCharacterId, rimrushAiDifficulty difficulty)
        {
            Reset();

            var activeCharacters = rimrushPlayersData.GetActiveCharacterIds();
            if (activeCharacters.Length < DivisionCount * TeamsPerDivision)
            {
                return false;
            }

            Active = true;
            Difficulty = difficulty;
            PlayerCharacterId = rimrushPlayersData.SanitizeCharacterId(playerCharacterId);

            var availableOpponents = new List<int>(activeCharacters.Length - 1);
            for (var i = 0; i < activeCharacters.Length; i++)
            {
                if (activeCharacters[i] != PlayerCharacterId)
                {
                    availableOpponents.Add(activeCharacters[i]);
                }
            }

            Shuffle(availableOpponents);

            DivisionEntrantCharacterIds[0][0] = PlayerCharacterId;
            for (var slot = 1; slot < TeamsPerDivision; slot++)
            {
                DivisionEntrantCharacterIds[0][slot] = availableOpponents[slot - 1];
            }

            for (var slot = 0; slot < TeamsPerDivision; slot++)
            {
                DivisionEntrantCharacterIds[1][slot] = availableOpponents[slot + TeamsPerDivision - 1];
            }

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

            BuildRegularSeasonSchedule();

            CurrentStage = rimrushTournamentStage.RegularSeason;
            CurrentRegularSeasonRoundIndex = 0;
            CurrentOpponentCharacterId = GetPlayerOpponentForRound(CurrentRegularSeasonRoundIndex);
            return true;
        }

        public void BeginFinals()
        {
            if (!Active || Completed || !RegularSeasonCompleted || !PlayerQualifiedForPlayoffs)
            {
                return;
            }

            if (CurrentStage != rimrushTournamentStage.RegularSeason)
            {
                return;
            }

            CurrentStage = rimrushTournamentStage.SemiFinal;
            CurrentOpponentCharacterId = GetPlayerOpponentForMatchSet(SemiFinalResults);
        }

        public void ApplyCurrentMatchResult(int playerScore, int opponentScore)
        {
            if (!Active || Completed)
            {
                return;
            }

            switch (CurrentStage)
            {
                case rimrushTournamentStage.RegularSeason:
                    ApplyRegularSeasonResult(playerScore, opponentScore);
                    break;
                case rimrushTournamentStage.SemiFinal:
                    ApplySemiFinalResult(playerScore, opponentScore);
                    break;
                case rimrushTournamentStage.ThirdPlace:
                    ApplyPlacementResult(ThirdPlaceResult, playerScore, opponentScore);
                    break;
                case rimrushTournamentStage.Final:
                    ApplyPlacementResult(FinalResult, playerScore, opponentScore);
                    break;
            }
        }

        public rimrushTournamentStandingEntry[] GetDivisionStandings(int divisionIndex)
        {
            if (divisionIndex < 0 || divisionIndex >= DivisionCount)
            {
                return new rimrushTournamentStandingEntry[0];
            }

            var standings = new rimrushTournamentStandingEntry[TeamsPerDivision];
            for (var i = 0; i < TeamsPerDivision; i++)
            {
                standings[i] = DivisionStandings[divisionIndex][i];
            }

            System.Array.Sort(standings, CompareDivisionStandings);
            return standings;
        }

        private void ApplyRegularSeasonResult(int playerScore, int opponentScore)
        {
            if (RegularSeasonCompleted || CurrentRegularSeasonRoundIndex < 0 || CurrentRegularSeasonRoundIndex >= RegularSeasonRoundCount)
            {
                return;
            }

            var playerMatch = GetPlayerMatchForRound(CurrentRegularSeasonRoundIndex);
            if (playerMatch == null || playerMatch.Completed)
            {
                return;
            }

            playerMatch.Complete(playerScore, opponentScore);
            ApplyStandingUpdate(playerMatch);

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

            CurrentRegularSeasonRoundIndex++;
            if (CurrentRegularSeasonRoundIndex < RegularSeasonRoundCount)
            {
                CurrentOpponentCharacterId = GetPlayerOpponentForRound(CurrentRegularSeasonRoundIndex);
                return;
            }

            RegularSeasonCompleted = true;
            CurrentOpponentCharacterId = -1;
            BuildPlayoffBracket();

            if (!PlayerQualifiedForPlayoffs)
            {
                SimulateEntirePlayoffs();
                FinalizeTournament();
            }
        }

        private void ApplySemiFinalResult(int playerScore, int opponentScore)
        {
            var playerSemi = GetPlayerMatchFromSet(SemiFinalResults);
            if (playerSemi == null || playerSemi.Completed)
            {
                return;
            }

            playerSemi.Complete(playerScore, opponentScore);

            for (var i = 0; i < SemiFinalResults.Length; i++)
            {
                if (SemiFinalResults[i].Completed)
                {
                    continue;
                }

                SimulateMatch(SemiFinalResults[i]);
            }

            ConfigurePlacementMatchesFromSemiFinals();
            if (playerSemi.WinnerCharacterId == PlayerCharacterId)
            {
                SimulateMatch(ThirdPlaceResult);
                CurrentStage = rimrushTournamentStage.Final;
                CurrentOpponentCharacterId = GetOpponentCharacterId(FinalResult, PlayerCharacterId);
                return;
            }

            SimulateMatch(FinalResult);
            CurrentStage = rimrushTournamentStage.ThirdPlace;
            CurrentOpponentCharacterId = GetOpponentCharacterId(ThirdPlaceResult, PlayerCharacterId);
        }

        private void ApplyPlacementResult(rimrushTournamentMatchResult match, int playerScore, int opponentScore)
        {
            if (match == null || match.Completed)
            {
                return;
            }

            match.Complete(playerScore, opponentScore);
            FinalizeTournament();
        }

        private void FinalizeTournament()
        {
            if (!FinalResult.Completed || !ThirdPlaceResult.Completed)
            {
                return;
            }

            ChampionCharacterId = FinalResult.WinnerCharacterId;
            PlayerPlacement = ResolvePlayerPlacement();
            CurrentOpponentCharacterId = -1;
            CurrentStage = rimrushTournamentStage.Complete;
            Completed = true;
        }

        private int ResolvePlayerPlacement()
        {
            var champion = FinalResult.WinnerCharacterId;
            var runnerUp = GetMatchLoserCharacterId(FinalResult);
            var third = ThirdPlaceResult.WinnerCharacterId;
            var fourth = GetMatchLoserCharacterId(ThirdPlaceResult);

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

            var nonPlayoffEntries = new List<rimrushTournamentStandingEntry>(4);
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

            nonPlayoffEntries.Sort(CompareOverallStandings);
            for (var i = 0; i < nonPlayoffEntries.Count; i++)
            {
                if (nonPlayoffEntries[i].CharacterId == PlayerCharacterId)
                {
                    return 5 + i;
                }
            }

            return 8;
        }

        private void BuildRegularSeasonSchedule()
        {
            for (var round = 0; round < RegularSeasonRoundCount; round++)
            {
                for (var division = 0; division < DivisionCount; division++)
                {
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

        private void BuildPlayoffBracket()
        {
            var divisionA = GetDivisionStandings(0);
            var divisionB = GetDivisionStandings(1);

            SemiFinalResults[0].Reset(divisionA[0].CharacterId, divisionB[1].CharacterId);
            SemiFinalResults[1].Reset(divisionB[0].CharacterId, divisionA[1].CharacterId);
            ThirdPlaceResult.Reset();
            FinalResult.Reset();

            PlayerQualifiedForPlayoffs =
                SemiFinalResults[0].LeftCharacterId == PlayerCharacterId ||
                SemiFinalResults[0].RightCharacterId == PlayerCharacterId ||
                SemiFinalResults[1].LeftCharacterId == PlayerCharacterId ||
                SemiFinalResults[1].RightCharacterId == PlayerCharacterId;
        }

        private void ConfigurePlacementMatchesFromSemiFinals()
        {
            FinalResult.Reset(
                SemiFinalResults[0].WinnerCharacterId,
                SemiFinalResults[1].WinnerCharacterId);
            ThirdPlaceResult.Reset(
                GetMatchLoserCharacterId(SemiFinalResults[0]),
                GetMatchLoserCharacterId(SemiFinalResults[1]));
        }

        private void SimulateEntirePlayoffs()
        {
            for (var i = 0; i < SemiFinalResults.Length; i++)
            {
                SimulateMatch(SemiFinalResults[i]);
            }

            ConfigurePlacementMatchesFromSemiFinals();
            SimulateMatch(ThirdPlaceResult);
            SimulateMatch(FinalResult);
        }

        private rimrushTournamentMatchResult GetPlayerMatchForRound(int roundIndex)
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

        private rimrushTournamentMatchResult GetPlayerMatchFromSet(rimrushTournamentMatchResult[] matches)
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

        private int GetPlayerOpponentForRound(int roundIndex)
        {
            return GetOpponentCharacterId(GetPlayerMatchForRound(roundIndex), PlayerCharacterId);
        }

        private int GetPlayerOpponentForMatchSet(rimrushTournamentMatchResult[] matches)
        {
            return GetOpponentCharacterId(GetPlayerMatchFromSet(matches), PlayerCharacterId);
        }

        private void ApplyStandingUpdate(rimrushTournamentMatchResult match)
        {
            var leftEntry = GetStandingEntry(match.LeftCharacterId);
            var rightEntry = GetStandingEntry(match.RightCharacterId);
            if (leftEntry == null || rightEntry == null)
            {
                return;
            }

            leftEntry.ApplyResult(match.LeftScore, match.RightScore);
            rightEntry.ApplyResult(match.RightScore, match.LeftScore);
        }

        private rimrushTournamentStandingEntry GetStandingEntry(int characterId)
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

        private static int CompareDivisionStandings(rimrushTournamentStandingEntry left, rimrushTournamentStandingEntry right)
        {
            var winCompare = right.Wins.CompareTo(left.Wins);
            if (winCompare != 0)
            {
                return winCompare;
            }

            var diffCompare = right.PointDiff.CompareTo(left.PointDiff);
            if (diffCompare != 0)
            {
                return diffCompare;
            }

            var pointsCompare = right.PointsFor.CompareTo(left.PointsFor);
            if (pointsCompare != 0)
            {
                return pointsCompare;
            }

            return left.DivisionSlot.CompareTo(right.DivisionSlot);
        }

        private static int CompareOverallStandings(rimrushTournamentStandingEntry left, rimrushTournamentStandingEntry right)
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

        private static int GetOpponentCharacterId(rimrushTournamentMatchResult match, int characterId)
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

        private static int GetMatchLoserCharacterId(rimrushTournamentMatchResult match)
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

        private static void SimulateMatch(rimrushTournamentMatchResult match)
        {
            if (match == null || match.Completed)
            {
                return;
            }

            var leftScore = 16 + Random.Range(0, 15);
            var rightScore = 14 + Random.Range(0, 15);
            match.Complete(leftScore, rightScore);
        }

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
