using System;
using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLTournamentRecord
    {
        public int TeamId;
        public int Wins;
        public int Losses;
        public int PointsFor;
        public int PointsAgainst;

        public int GamesPlayed => Wins + Losses;

        public int PointDifferential => PointsFor - PointsAgainst;

        public float WinPercent => GamesPlayed <= 0 ? 0f : Wins / (float)GamesPlayed;

        public void Reset(int teamId)
        {
            TeamId = teamId;
            Wins = 0;
            Losses = 0;
            PointsFor = 0;
            PointsAgainst = 0;
        }
    }

    public sealed class BLTournamentData
    {
        private static readonly int[,] RoundFixtures =
        {
            { 0, 1, 2, 3 },
            { 0, 2, 1, 3 },
            { 0, 3, 1, 2 }
        };

        private readonly BLTournamentRecord[] records =
        {
            new BLTournamentRecord(),
            new BLTournamentRecord(),
            new BLTournamentRecord(),
            new BLTournamentRecord()
        };

        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public int GameMode { get; private set; }
        public int MatchMode { get; private set; }
        public int SelectedPlayer { get; private set; }
        public int SelectedTeammate { get; private set; }
        public BLAiDifficulty Difficulty { get; private set; }
        public int CurrentRound { get; private set; }
        public int[] TeamIds { get; } = new int[4];
        public IReadOnlyList<BLTournamentRecord> Records => records;

        public int TotalRounds => RoundFixtures.GetLength(0);

        public int PlayerTeamId => TeamIds[0];

        public int CurrentOpponentTeamId
        {
            get
            {
                var opponentIndex = GetOpponentIndexForRound(CurrentRound);
                return opponentIndex >= 0 ? TeamIds[opponentIndex] : 0;
            }
        }

        public void Reset()
        {
            Active = false;
            Completed = false;
            GameMode = 1;
            MatchMode = 0;
            SelectedPlayer = 0;
            SelectedTeammate = 1;
            Difficulty = BLAiDifficulty.Normal;
            CurrentRound = 0;
            Array.Clear(TeamIds, 0, TeamIds.Length);
            for (var i = 0; i < records.Length; i++)
            {
                records[i].Reset(0);
            }
        }

        public void Create(int playerTeamId, int selectedPlayer, int gameMode, int matchMode, BLAiDifficulty difficulty)
        {
            Reset();
            Active = true;
            GameMode = gameMode;
            MatchMode = matchMode;
            Difficulty = difficulty;
            SelectedPlayer = Mathf.Clamp(selectedPlayer, 0, BLPlayersData.TeamSize - 1);
            SelectedTeammate = (SelectedPlayer + 1) % BLPlayersData.TeamSize;
            TeamIds[0] = Mathf.Clamp(playerTeamId, 1, BLPlayersData.TeamsCount);

            var usedTeams = new HashSet<int> { TeamIds[0] };
            for (var i = 1; i < TeamIds.Length; i++)
            {
                var teamId = 1 + UnityEngine.Random.Range(0, BLPlayersData.TeamsCount);
                while (!usedTeams.Add(teamId))
                {
                    teamId = 1 + UnityEngine.Random.Range(0, BLPlayersData.TeamsCount);
                }

                TeamIds[i] = teamId;
            }

            for (var i = 0; i < records.Length; i++)
            {
                records[i].Reset(TeamIds[i]);
            }
        }

        public void ApplyRoundResult(int playerScore, int opponentScore)
        {
            if (!Active || Completed || CurrentRound >= TotalRounds)
            {
                return;
            }

            ApplyPlayerFixture(playerScore, opponentScore);
            SimulateCpuFixture(CurrentRound);

            CurrentRound++;
            if (CurrentRound >= TotalRounds)
            {
                Completed = true;
            }
        }

        public IReadOnlyList<BLTournamentRecord> GetSortedRecords()
        {
            var copy = new List<BLTournamentRecord>(records.Length);
            for (var i = 0; i < records.Length; i++)
            {
                copy.Add(new BLTournamentRecord
                {
                    TeamId = records[i].TeamId,
                    Wins = records[i].Wins,
                    Losses = records[i].Losses,
                    PointsFor = records[i].PointsFor,
                    PointsAgainst = records[i].PointsAgainst
                });
            }

            copy.Sort((left, right) =>
            {
                var winCompare = right.Wins.CompareTo(left.Wins);
                if (winCompare != 0)
                {
                    return winCompare;
                }

                var diffCompare = right.PointDifferential.CompareTo(left.PointDifferential);
                if (diffCompare != 0)
                {
                    return diffCompare;
                }

                var pointsCompare = right.PointsFor.CompareTo(left.PointsFor);
                if (pointsCompare != 0)
                {
                    return pointsCompare;
                }

                return left.TeamId.CompareTo(right.TeamId);
            });

            return copy;
        }

        public int GetPlayerPlacement()
        {
            var sorted = GetSortedRecords();
            for (var i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].TeamId == PlayerTeamId)
                {
                    return i + 1;
                }
            }

            return sorted.Count;
        }

        public int GetChampionTeamId()
        {
            var sorted = GetSortedRecords();
            return sorted.Count > 0 ? sorted[0].TeamId : 0;
        }

        public int GetRunnerUpTeamId()
        {
            var sorted = GetSortedRecords();
            return sorted.Count > 1 ? sorted[1].TeamId : 0;
        }

        private void ApplyPlayerFixture(int playerScore, int opponentScore)
        {
            var opponentIndex = GetOpponentIndexForRound(CurrentRound);
            if (opponentIndex < 0)
            {
                return;
            }

            ApplyMatchResult(0, playerScore, opponentIndex, opponentScore);
        }

        private void SimulateCpuFixture(int roundIndex)
        {
            var teamA = RoundFixtures[roundIndex, 2];
            var teamB = RoundFixtures[roundIndex, 3];
            var scoreA = 16 + UnityEngine.Random.Range(0, 15);
            var scoreB = 14 + UnityEngine.Random.Range(0, 15);
            if (scoreA == scoreB)
            {
                scoreA++;
            }

            ApplyMatchResult(teamA, scoreA, teamB, scoreB);
        }

        private void ApplyMatchResult(int teamAIndex, int scoreA, int teamBIndex, int scoreB)
        {
            if (teamAIndex < 0 || teamAIndex >= records.Length || teamBIndex < 0 || teamBIndex >= records.Length)
            {
                return;
            }

            if (scoreA == scoreB)
            {
                scoreA++;
            }

            var recordA = records[teamAIndex];
            var recordB = records[teamBIndex];
            recordA.PointsFor += scoreA;
            recordA.PointsAgainst += scoreB;
            recordB.PointsFor += scoreB;
            recordB.PointsAgainst += scoreA;

            if (scoreA > scoreB)
            {
                recordA.Wins++;
                recordB.Losses++;
            }
            else
            {
                recordB.Wins++;
                recordA.Losses++;
            }
        }

        private static int GetOpponentIndexForRound(int roundIndex)
        {
            if (roundIndex < 0 || roundIndex >= RoundFixtures.GetLength(0))
            {
                return -1;
            }

            return RoundFixtures[roundIndex, 1];
        }
    }
}
