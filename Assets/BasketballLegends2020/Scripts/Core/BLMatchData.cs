using UnityEngine;

namespace BasketballLegends2020
{
    public enum BLAiDifficulty
    {
        Easy,
        Normal
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
            if (MatchMode == 0)
            {
                Pb = new[] { new[] { "P0" }, new[] { "B0" } };
                Skills = new[] { new[] { 0 }, new[] { 3 } };
            }
            else
            {
                FillSecondPlayers();
                Pb = new[] { new[] { "P0", "B2" }, new[] { "B1", "B2" } };
                Skills = new[] { new[] { 0, 3 }, new[] { 3, 3 } };
            }

            RndForms();
        }

        public void StartPlayers2Match()
        {
            if (MatchMode == 0)
            {
                var left = Players[0].Length > 0 ? Players[0][0] : 0;
                var right = Players[1].Length > 0 ? Players[1][0] : 0;
                Players = new[] { new[] { left }, new[] { right } };
                Pb = new[] { new[] { "P1" }, new[] { "P2" } };
                Skills = new[] { new[] { 0 }, new[] { 0 } };
            }
            else if (MatchMode == 1)
            {
                FillSecondPlayers();
                Pb = new[] { new[] { "P1", "B2" }, new[] { "P2", "B2" } };
                Skills = new[] { new[] { 0, 4 }, new[] { 0, 4 } };
            }
            else
            {
                FillSecondPlayers();
                Pb = new[] { new[] { "P1", "P2" }, new[] { "B1", "B2" } };
                Skills = new[] { new[] { 0, 0 }, new[] { 4, 4 } };
            }

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
        public bool FirstRun = true;
        public bool FirstRun2 = true;
        public BLAiDifficulty Difficulty;

        private BLInventory()
        {
            GameMode = 1;
            MatchData = new BLMatchData(true);
            MatchData.MatchMode = 0;
            Difficulty = BLAiDifficulty.Normal;
        }

        public string DifficultyLabel => Difficulty == BLAiDifficulty.Easy ? "AI: EASY" : "AI: NORMAL";

        public void ToggleDifficulty()
        {
            Difficulty = Difficulty == BLAiDifficulty.Easy ? BLAiDifficulty.Normal : BLAiDifficulty.Easy;
        }

        public void StartQuickGame()
        {
            GameMode = 2;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch();
        }

        public void StartOnePlayer()
        {
            GameMode = 1;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch();
        }

        public void StartTwoPlayers()
        {
            GameMode = 4;
            MatchData.MatchMode = 0;
            if (MatchData.Teams[0] == 0)
            {
                MatchData.BaseInit();
            }
            MatchData.StartPlayers2Match();
        }

        public void StartTraining()
        {
            GameMode = 3;
            MatchData.StartTraining();
        }
    }
}
