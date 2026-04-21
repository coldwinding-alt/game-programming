using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public enum BLTournamentStage
    {
        None,
        SemiFinal,
        Final,
        Complete
    }

    public sealed class BLTournamentMatchResult
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

    public sealed class BLTournamentData
    {
        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public BLAiDifficulty Difficulty { get; private set; }
        public BLTournamentStage CurrentStage { get; private set; }
        public int CurrentOpponentCharacterId { get; private set; } = -1;
        public int PlayerCharacterId { get; private set; } = -1;
        public int ChampionCharacterId { get; private set; } = -1;
        public int PlayerPlacement { get; private set; }
        public int[] EntrantCharacterIds { get; } = { -1, -1, -1, -1 };
        public BLTournamentMatchResult[] SemiFinalResults { get; } =
        {
            new BLTournamentMatchResult(),
            new BLTournamentMatchResult()
        };
        public BLTournamentMatchResult FinalResult { get; } = new BLTournamentMatchResult();

        public void Reset()
        {
            Active = false;
            Completed = false;
            Difficulty = BLAiDifficulty.Normal;
            CurrentStage = BLTournamentStage.None;
            CurrentOpponentCharacterId = -1;
            PlayerCharacterId = -1;
            ChampionCharacterId = -1;
            PlayerPlacement = 0;
            for (var i = 0; i < EntrantCharacterIds.Length; i++)
            {
                EntrantCharacterIds[i] = -1;
            }

            SemiFinalResults[0].Reset();
            SemiFinalResults[1].Reset();
            FinalResult.Reset();
        }

        public bool Create(int playerCharacterId, BLAiDifficulty difficulty)
        {
            Reset();
            var activeCharacters = BLPlayersData.GetActiveCharacterIds();
            if (activeCharacters.Length < 4)
            {
                return false;
            }

            Active = true;
            Difficulty = difficulty;
            PlayerCharacterId = BLPlayersData.SanitizeCharacterId(playerCharacterId);

            var availableOpponents = new List<int>(activeCharacters.Length - 1);
            for (var i = 0; i < activeCharacters.Length; i++)
            {
                if (activeCharacters[i] != PlayerCharacterId)
                {
                    availableOpponents.Add(activeCharacters[i]);
                }
            }

            Shuffle(availableOpponents);

            EntrantCharacterIds[0] = PlayerCharacterId;
            for (var i = 0; i < 3; i++)
            {
                EntrantCharacterIds[i + 1] = availableOpponents[i];
            }

            SemiFinalResults[0].Reset(EntrantCharacterIds[0], EntrantCharacterIds[1]);
            SemiFinalResults[1].Reset(EntrantCharacterIds[2], EntrantCharacterIds[3]);
            FinalResult.Reset();

            CurrentStage = BLTournamentStage.SemiFinal;
            CurrentOpponentCharacterId = EntrantCharacterIds[1];
            return true;
        }

        public void ApplyCurrentMatchResult(int playerScore, int opponentScore)
        {
            if (!Active || Completed)
            {
                return;
            }

            if (CurrentStage == BLTournamentStage.SemiFinal)
            {
                SemiFinalResults[0].Complete(playerScore, opponentScore);
                SimulateMatch(SemiFinalResults[1]);

                if (SemiFinalResults[0].WinnerCharacterId != PlayerCharacterId)
                {
                    FinalResult.Reset(SemiFinalResults[0].WinnerCharacterId, SemiFinalResults[1].WinnerCharacterId);
                    SimulateMatch(FinalResult);
                    ChampionCharacterId = FinalResult.WinnerCharacterId;
                    PlayerPlacement = 3;
                    CurrentOpponentCharacterId = -1;
                    CurrentStage = BLTournamentStage.Complete;
                    Completed = true;
                    return;
                }

                FinalResult.Reset(PlayerCharacterId, SemiFinalResults[1].WinnerCharacterId);
                CurrentOpponentCharacterId = FinalResult.RightCharacterId;
                CurrentStage = BLTournamentStage.Final;
                return;
            }

            if (CurrentStage == BLTournamentStage.Final)
            {
                FinalResult.Complete(playerScore, opponentScore);
                ChampionCharacterId = FinalResult.WinnerCharacterId;
                PlayerPlacement = ChampionCharacterId == PlayerCharacterId ? 1 : 2;
                CurrentOpponentCharacterId = -1;
                CurrentStage = BLTournamentStage.Complete;
                Completed = true;
            }
        }

        private static void SimulateMatch(BLTournamentMatchResult match)
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
