using System.Collections.Generic;
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
        public int FirstCharacterId;
        public int MatchMode;
        public int[] CharacterIds = new int[2];
        public string[][] Pb = { new string[0], new string[0] };
        public int[][] Skills = { new int[0], new int[0] };
        public int[] MatchScore = { 0, 0 };

        public BLMatchData(bool local)
        {
            FirstCharacterId = BLPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
        }

        public void ResetData()
        {
            CharacterIds = new[] { BLPlayersData.SanitizeCharacterId(0), BLPlayersData.SanitizeCharacterId(1, 0) };
        }

        public void ResetPartly()
        {
            MatchMode = 0;
            Pb = new[] { new string[0], new string[0] };
            Skills = new[] { new int[0], new int[0] };
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
            CharacterIds = new[]
            {
                BLPlayersData.SanitizeCharacterId(FirstCharacterId),
                BLPlayersData.StepCharacterId(FirstCharacterId, 1)
            };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { 2 } };
        }

        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        public void StartQuickMatch(int playerCharacterId, BLAiDifficulty difficulty)
        {
            ResetAll();
            var playerId = BLPlayersData.SanitizeCharacterId(playerCharacterId);
            var excluded = new List<int> { playerId };
            var opponentId = BLPlayersData.GetRandomCharacterId(excluded);
            var opponentSkill = difficulty == BLAiDifficulty.Easy ? 1 : 2;

            CharacterIds = new[] { playerId, opponentId };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartQuickLocalVersusMatch()
        {
            ResetAll();
            var left = BLPlayersData.SanitizeCharacterId(0);
            var right = BLPlayersData.StepCharacterId(left, 1);

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void StartTraining(int characterId)
        {
            ResetAll();
            var resolvedCharacterId = BLPlayersData.SanitizeCharacterId(characterId);

            CharacterIds = new[] { resolvedCharacterId, resolvedCharacterId };
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
        }

        public void StartRandomMatch(int playerCharacterId, BLAiDifficulty difficulty)
        {
            StartQuickMatch(playerCharacterId, difficulty);
        }

        public void StartPlayers2Match()
        {
            MatchMode = 0;
            var left = BLPlayersData.SanitizeCharacterId(CharacterIds[0]);
            var right = BLPlayersData.SanitizeCharacterId(CharacterIds[1], BLPlayersData.StepCharacterId(left, 1));

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void StartTournamentMatch(BLTournamentData tournament)
        {
            ResetAll();
            if (tournament == null || !tournament.Active || tournament.Completed)
            {
                return;
            }

            MatchMode = 0;
            CharacterIds = new[]
            {
                BLPlayersData.SanitizeCharacterId(tournament.PlayerCharacterId),
                BLPlayersData.SanitizeCharacterId(tournament.CurrentOpponentCharacterId)
            };

            var opponentSkillBase = tournament.CurrentStage == BLTournamentStage.Final ? 3 : 2;
            var opponentSkill = Mathf.Clamp(
                opponentSkillBase + (tournament.Difficulty == BLAiDifficulty.Normal ? 1 : 0),
                0,
                8);

            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartSelectedTwoPlayerMatch(int leftCharacterId, int rightCharacterId)
        {
            ResetAll();
            MatchMode = 0;
            CharacterIds = new[]
            {
                BLPlayersData.SanitizeCharacterId(leftCharacterId),
                BLPlayersData.SanitizeCharacterId(rightCharacterId, leftCharacterId)
            };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
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
        public int SelectedQuickCharacterId;
        public int SelectedTournamentCharacterId;
        public int SelectedTrainingCharacterId;

        private BLInventory()
        {
            GameMode = 1;
            ParticipantMode = BLParticipantMode.OnePlayer;
            SessionMode = BLSessionMode.None;
            MatchData = new BLMatchData(true);
            Tournament = new BLTournamentData();
            MatchData.MatchMode = 0;
            Difficulty = BLAiDifficulty.Normal;
            SelectedQuickCharacterId = MatchData.FirstCharacterId;
            SelectedTournamentCharacterId = MatchData.FirstCharacterId;
            SelectedTrainingCharacterId = MatchData.FirstCharacterId;
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

        public void SetQuickSelection(int characterId)
        {
            SelectedQuickCharacterId = BLPlayersData.SanitizeCharacterId(characterId);
        }

        public void SetTournamentSelection(int characterId)
        {
            SelectedTournamentCharacterId = BLPlayersData.SanitizeCharacterId(characterId);
        }

        public void SetTrainingSelection(int characterId)
        {
            SelectedTrainingCharacterId = BLPlayersData.SanitizeCharacterId(characterId);
        }

        public void StartQuickGame()
        {
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = BLParticipantMode.OnePlayer;
            GameMode = 2;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty);
        }

        public void StartOnePlayer()
        {
            ParticipantMode = BLParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 1;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch(SelectedQuickCharacterId, Difficulty);
            MatchPrepared = true;
        }

        public void StartTwoPlayers()
        {
            ParticipantMode = BLParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.MatchMode = 0;
            MatchData.StartPlayers2Match();
            MatchPrepared = true;
        }

        public void StartTwoPlayerVersus(int leftCharacterId, int rightCharacterId)
        {
            ParticipantMode = BLParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.StartSelectedTwoPlayerMatch(leftCharacterId, rightCharacterId);
            MatchPrepared = true;
        }

        public void StartTraining()
        {
            ParticipantMode = BLParticipantMode.Training;
            Tournament.Reset();
            SessionMode = BLSessionMode.Training;
            GameMode = 3;
            MatchData.StartTraining(SelectedTrainingCharacterId);
            MatchPrepared = true;
        }

        public bool BeginTournament()
        {
            ParticipantMode = BLParticipantMode.OnePlayer;
            SessionMode = BLSessionMode.Tournament;
            GameMode = 1;
            if (!Tournament.Create(SelectedTournamentCharacterId, Difficulty))
            {
                MatchPrepared = false;
                return false;
            }

            MatchData.StartTournamentMatch(Tournament);
            MatchPrepared = true;
            return true;
        }

        public bool AdvanceTournament()
        {
            if (!IsTournamentActive)
            {
                return false;
            }

            Tournament.ApplyCurrentMatchResult(MatchData.MatchScore[0], MatchData.MatchScore[1]);
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
