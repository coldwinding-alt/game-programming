using UnityEngine;

namespace BasketballLegends2020
{
    public enum BLAiDifficulty
    {
        Easy,
        Normal
    }

    public enum BLParticipantMode
    {
        OnePlayer,
        TwoPlayers,
        Training
    }

    public enum BLSessionMode
    {
        None,
        QuickMatch,
        Tournament,
        Training
    }

    public sealed class BLMatchData
    {
        public bool Restarted;
        public int FirstTeam;
        public int MatchMode;
        public int[] Teams = new int[2];
        public int[][] Players = { new int[0], new int[0] };
        public string[][] Pb = { new string[0], new string[0] };
        public int[][] Skills = { new int[0], new int[0] };
        public int[] Forms = { 0, 1 };
        public int[] MatchScore = { 0, 0 };

        public BLMatchData(bool local)
        {
            FirstTeam = local ? 1 : 17;
            BaseInit();
            ResetPartly();
        }

        public void ResetData()
        {
            Teams = new int[2];
            Players = new[] { new int[0], new int[0] };
        }

        public void ResetPartly()
        {
            MatchMode = 0;
            Pb = new[] { new string[0], new string[0] };
            Skills = new[] { new int[0], new int[0] };
            Forms = new[] { 0, 1 };
            MatchScore = new[] { 0, 0 };
        }

        public void ResetAll()
        {
            ResetData();
            ResetPartly();
        }

        public void BaseInit()
        {
            MatchMode = 0;
            Teams = new[] { FirstTeam, 2 };
            Players = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        public void StartQuickMatch()
        {
            ResetAll();
            var left = 1 + Random.Range(0, BLPlayersData.TeamsCount);
            var right = 1 + Random.Range(0, BLPlayersData.TeamsCount);
            if (left == right)
            {
                right = right == BLPlayersData.TeamsCount ? right - 1 : right + 1;
            }

            Teams = new[] { left, right };
            Players[0] = new[] { Random.Range(0, BLPlayersData.TeamSize) };
            Players[1] = new[] { Random.Range(0, BLPlayersData.TeamSize) };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { 2 } };
            RndForms();
        }

        public void StartQuickLocalVersusMatch()
        {
            ResetAll();
            var left = 1 + Random.Range(0, BLPlayersData.TeamsCount);
            var right = 1 + Random.Range(0, BLPlayersData.TeamsCount);
            if (left == right)
            {
                right = right == BLPlayersData.TeamsCount ? right - 1 : right + 1;
            }

            Teams = new[] { left, right };
            Players[0] = new[] { Random.Range(0, BLPlayersData.TeamSize) };
            Players[1] = new[] { Random.Range(0, BLPlayersData.TeamSize) };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
            RndForms();
        }

        public void StartTraining()
        {
            ResetAll();
            Teams[0] = 1 + Random.Range(0, BLPlayersData.TeamsCount);
            Teams[1] = Teams[0];
            Players[0] = new[] { Random.Range(0, BLPlayersData.TeamSize) };
            Players[1] = new int[0];
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
            RndForms();
        }

        public void StartRandomMatch()
        {
            MatchMode = 0;
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { 3 } };

            RndForms();
        }

        public void StartPlayers2Match()
        {
            MatchMode = 0;
            var left = Players[0].Length > 0 ? Players[0][0] : 0;
            var right = Players[1].Length > 0 ? Players[1][0] : 0;
            Players = new[] { new[] { left }, new[] { right } };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };

            RndForms();
        }

        public void StartTournamentMatch(BLTournamentData tournament)
        {
            ResetAll();
            if (tournament == null || !tournament.Active)
            {
                return;
            }

            MatchMode = 0;
            Teams = new[] { tournament.PlayerTeamId, tournament.CurrentOpponentTeamId };
            var opponentSkill = Mathf.Clamp(
                2 + tournament.CurrentRound + (tournament.Difficulty == BLAiDifficulty.Normal ? 1 : 0),
                0,
                8);
            var playerOne = Mathf.Clamp(tournament.SelectedPlayer, 0, BLPlayersData.TeamSize - 1);
            var opponentOne = Random.Range(0, BLPlayersData.TeamSize);
            Players = new[] { new[] { playerOne }, new[] { opponentOne } };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };

            RndForms();
        }

        public void StartSelectedTwoPlayerMatch(int leftTeamId, int leftPlayerIndex, int rightTeamId, int rightPlayerIndex)
        {
            ResetAll();
            MatchMode = 0;
            Teams = new[]
            {
                Mathf.Clamp(leftTeamId, 1, BLPlayersData.TeamsCount),
                Mathf.Clamp(rightTeamId, 1, BLPlayersData.TeamsCount)
            };
            Players = new[]
            {
                new[] { Mathf.Clamp(leftPlayerIndex, 0, BLPlayersData.TeamSize - 1) },
                new[] { Mathf.Clamp(rightPlayerIndex, 0, BLPlayersData.TeamSize - 1) }
            };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
            RndForms();
        }

        public void FillSecondPlayers()
        {
            if (Players[0].Length != 2)
            {
                Players[0] = new[] { Players[0][0], Players[0][0] == 0 ? 1 : 0 };
            }

            if (Players[1].Length != 2)
            {
                Players[1] = new[] { Players[1][0], Players[1][0] == 0 ? 1 : 0 };
            }
        }

        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
        }

        private void RndForms()
        {
            Forms = new[] { 0, 1 };
        }
    }

    public sealed class BLInventory
    {
        private static BLInventory instance;

        public static BLInventory Instance => instance ?? (instance = new BLInventory());

        public int GameMode;
        public BLMatchData MatchData;
        public BLTournamentData Tournament;
        public bool FirstRun = true;
        public bool FirstRun2 = true;
        public bool MatchPrepared;
        public BLAiDifficulty Difficulty;
        public BLParticipantMode ParticipantMode;
        public BLSessionMode SessionMode;
        public int SelectedTournamentTeamId;
        public int SelectedTournamentPlayerIndex;
        public int SelectedTournamentMatchMode;

        private BLInventory()
        {
            GameMode = 1;
            ParticipantMode = BLParticipantMode.OnePlayer;
            SessionMode = BLSessionMode.None;
            MatchData = new BLMatchData(true);
            Tournament = new BLTournamentData();
            MatchData.MatchMode = 0;
            Difficulty = BLAiDifficulty.Normal;
            SelectedTournamentTeamId = MatchData.FirstTeam;
            SelectedTournamentPlayerIndex = 0;
            SelectedTournamentMatchMode = 0;
        }

        public string DifficultyLabel => Difficulty == BLAiDifficulty.Easy ? "AI: EASY" : "AI: NORMAL";

        public bool IsTournamentActive => SessionMode == BLSessionMode.Tournament && Tournament.Active;

        public void ToggleDifficulty()
        {
            Difficulty = Difficulty == BLAiDifficulty.Easy ? BLAiDifficulty.Normal : BLAiDifficulty.Easy;
        }

        public void SetParticipantMode(BLParticipantMode participantMode)
        {
            ParticipantMode = participantMode;
            if (participantMode == BLParticipantMode.Training)
            {
                SessionMode = BLSessionMode.Training;
            }
        }

        public void SetTournamentSelection(int teamId, int playerIndex, int matchMode)
        {
            SelectedTournamentTeamId = Mathf.Clamp(teamId, 1, BLPlayersData.TeamsCount);
            SelectedTournamentPlayerIndex = Mathf.Clamp(playerIndex, 0, BLPlayersData.TeamSize - 1);
            SelectedTournamentMatchMode = Mathf.Clamp(matchMode, 0, 0);
        }

        public void StartQuickGame()
        {
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            MatchPrepared = true;
            if (ParticipantMode == BLParticipantMode.TwoPlayers)
            {
                GameMode = 4;
                MatchData.MatchMode = 0;
                MatchData.StartQuickLocalVersusMatch();
                return;
            }

            ParticipantMode = BLParticipantMode.OnePlayer;
            GameMode = 2;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch();
        }

        public void StartOnePlayer()
        {
            ParticipantMode = BLParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 1;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch();
            MatchPrepared = true;
        }

        public void StartTwoPlayers()
        {
            ParticipantMode = BLParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.MatchMode = 0;
            if (MatchData.Teams[0] == 0)
            {
                MatchData.BaseInit();
            }

            MatchData.StartPlayers2Match();
            MatchPrepared = true;
        }

        public void StartTwoPlayerVersus(int leftTeamId, int leftPlayerIndex, int rightTeamId, int rightPlayerIndex)
        {
            ParticipantMode = BLParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.StartSelectedTwoPlayerMatch(leftTeamId, leftPlayerIndex, rightTeamId, rightPlayerIndex);
            MatchPrepared = true;
        }

        public void StartTraining()
        {
            ParticipantMode = BLParticipantMode.Training;
            Tournament.Reset();
            SessionMode = BLSessionMode.Training;
            GameMode = 3;
            MatchData.StartTraining();
            MatchPrepared = true;
        }

        public void BeginTournament()
        {
            ParticipantMode = BLParticipantMode.OnePlayer;
            SessionMode = BLSessionMode.Tournament;
            GameMode = 1;
            var resolvedMatchMode = 0;
            Tournament.Create(
                SelectedTournamentTeamId,
                SelectedTournamentPlayerIndex,
                GameMode,
                resolvedMatchMode,
                Difficulty);
            MatchData.StartTournamentMatch(Tournament);
            MatchPrepared = true;
        }

        public bool AdvanceTournament()
        {
            if (!IsTournamentActive)
            {
                return false;
            }

            Tournament.ApplyRoundResult(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            MatchPrepared = false;
            if (!Tournament.Completed)
            {
                MatchData.StartTournamentMatch(Tournament);
                MatchPrepared = true;
            }

            return Tournament.Completed;
        }

        public void AbandonTournament()
        {
            Tournament.Reset();
            SessionMode = BLSessionMode.None;
            MatchPrepared = false;
        }
    }
}
