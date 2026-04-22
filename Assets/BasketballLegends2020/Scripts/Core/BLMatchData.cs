using System;
using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public enum BLAiDifficulty
    {
        Easy,
        Normal,
        Hard
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

    public enum BLBallTheme
    {
        ClassicOriginal,
        GhoulGreen,
        PumpkinEmber,
        MoonlitViolet,
        JackOLantern,
        EvilEye,
        Cursed8Ball,
        CandySwirl
    }

    public enum BLBallSelection
    {
        Random,
        ClassicOriginal,
        GhoulGreen,
        PumpkinEmber,
        MoonlitViolet,
        JackOLantern,
        EvilEye,
        Cursed8Ball,
        CandySwirl
    }

    public static class BLBallCatalog
    {
        private static readonly BLBallSelection[] OrderedSelections =
        {
            BLBallSelection.Random,
            BLBallSelection.ClassicOriginal,
            BLBallSelection.GhoulGreen,
            BLBallSelection.PumpkinEmber,
            BLBallSelection.MoonlitViolet,
            BLBallSelection.JackOLantern,
            BLBallSelection.EvilEye,
            BLBallSelection.Cursed8Ball,
            BLBallSelection.CandySwirl
        };

        private static readonly BLBallTheme[] ConcreteThemes =
        {
            BLBallTheme.ClassicOriginal,
            BLBallTheme.GhoulGreen,
            BLBallTheme.PumpkinEmber,
            BLBallTheme.MoonlitViolet,
            BLBallTheme.JackOLantern,
            BLBallTheme.EvilEye,
            BLBallTheme.Cursed8Ball,
            BLBallTheme.CandySwirl
        };

        public static BLBallSelection StepSelection(BLBallSelection current, int direction)
        {
            var index = Array.IndexOf(OrderedSelections, current);
            if (index < 0)
            {
                index = 0;
            }

            index += direction >= 0 ? 1 : -1;
            if (index < 0)
            {
                index = OrderedSelections.Length - 1;
            }
            else if (index >= OrderedSelections.Length)
            {
                index = 0;
            }

            return OrderedSelections[index];
        }

        public static BLBallTheme ResolveTheme(BLBallSelection selection)
        {
            return selection == BLBallSelection.Random
                ? ConcreteThemes[UnityEngine.Random.Range(0, ConcreteThemes.Length)]
                : ToTheme(selection);
        }

        public static BLBallTheme PreviewTheme(BLBallSelection selection)
        {
            return selection == BLBallSelection.Random
                ? BLBallTheme.ClassicOriginal
                : ToTheme(selection);
        }

        public static BLBallTheme ToTheme(BLBallSelection selection)
        {
            return selection switch
            {
                BLBallSelection.GhoulGreen => BLBallTheme.GhoulGreen,
                BLBallSelection.PumpkinEmber => BLBallTheme.PumpkinEmber,
                BLBallSelection.MoonlitViolet => BLBallTheme.MoonlitViolet,
                BLBallSelection.JackOLantern => BLBallTheme.JackOLantern,
                BLBallSelection.EvilEye => BLBallTheme.EvilEye,
                BLBallSelection.Cursed8Ball => BLBallTheme.Cursed8Ball,
                BLBallSelection.CandySwirl => BLBallTheme.CandySwirl,
                _ => BLBallTheme.ClassicOriginal
            };
        }

        public static string Label(BLBallSelection selection)
        {
            return selection switch
            {
                BLBallSelection.Random => "RANDOM",
                BLBallSelection.GhoulGreen => "GHOUL",
                BLBallSelection.PumpkinEmber => "EMBER",
                BLBallSelection.MoonlitViolet => "VIOLET",
                BLBallSelection.JackOLantern => "JACK",
                BLBallSelection.EvilEye => "EYE",
                BLBallSelection.Cursed8Ball => "8-BALL",
                BLBallSelection.CandySwirl => "SWIRL",
                _ => "CLASSIC"
            };
        }
    }

    public sealed class BLMatchData
    {
        public bool Restarted;
        public int FirstCharacterId;
        public int MatchMode;
        public BLBallTheme BallTheme;
        public int[] CharacterIds = new int[2];
        public string[][] Pb = { new string[0], new string[0] };
        public int[][] Skills = { new int[0], new int[0] };
        public int[] MatchScore = { 0, 0 };

        public BLMatchData(bool local)
        {
            FirstCharacterId = BLPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
            RollBallTheme();
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
            Skills = new[] { new[] { 0 }, new[] { GetQuickMatchOpponentSkill(BLAiDifficulty.Normal) } };
        }

        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        public void StartQuickMatch(int playerCharacterId, BLAiDifficulty difficulty, BLBallSelection ballSelection = BLBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var playerId = BLPlayersData.SanitizeCharacterId(playerCharacterId);
            var excluded = new List<int> { playerId };
            var opponentId = BLPlayersData.GetRandomCharacterId(excluded);
            var opponentSkill = GetQuickMatchOpponentSkill(difficulty);

            CharacterIds = new[] { playerId, opponentId };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartQuickLocalVersusMatch(BLBallSelection ballSelection = BLBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var left = BLPlayersData.SanitizeCharacterId(0);
            var right = BLPlayersData.StepCharacterId(left, 1);

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void StartTraining(int characterId, BLBallSelection ballSelection = BLBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = BLPlayersData.SanitizeCharacterId(characterId);

            CharacterIds = new[] { resolvedCharacterId, resolvedCharacterId };
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
        }

        public void StartRandomMatch(int playerCharacterId, BLAiDifficulty difficulty, BLBallSelection ballSelection = BLBallSelection.Random)
        {
            StartQuickMatch(playerCharacterId, difficulty, ballSelection);
        }

        public void StartPlayers2Match(BLBallSelection ballSelection = BLBallSelection.Random)
        {
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            var left = BLPlayersData.SanitizeCharacterId(CharacterIds[0]);
            var right = BLPlayersData.SanitizeCharacterId(CharacterIds[1], BLPlayersData.StepCharacterId(left, 1));

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void StartTournamentMatch(BLTournamentData tournament, BLBallSelection ballSelection = BLBallSelection.Random)
        {
            ResetAll();
            if (tournament == null || !tournament.Active || tournament.Completed || !tournament.HasPendingPlayerMatch)
            {
                return;
            }

            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            CharacterIds = new[]
            {
                BLPlayersData.SanitizeCharacterId(tournament.PlayerCharacterId),
                BLPlayersData.SanitizeCharacterId(tournament.CurrentOpponentCharacterId)
            };

            var opponentSkill = GetTournamentOpponentSkill(tournament);

            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartSelectedTwoPlayerMatch(int leftCharacterId, int rightCharacterId, BLBallSelection ballSelection = BLBallSelection.Random)
        {
            ResetAll();
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
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

        private static int GetQuickMatchOpponentSkill(BLAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BLAiDifficulty.Easy:
                    return 1;
                case BLAiDifficulty.Hard:
                    return 5;
                default:
                    return 2;
            }
        }

        private static int GetTournamentOpponentSkill(BLTournamentData tournament)
        {
            if (tournament == null)
            {
                return 0;
            }

            int skill;
            switch (tournament.Difficulty)
            {
                case BLAiDifficulty.Easy:
                    skill = tournament.CurrentStage switch
                    {
                        BLTournamentStage.SemiFinal => 3,
                        BLTournamentStage.ThirdPlace => 3,
                        BLTournamentStage.Final => 4,
                        _ => 2
                    };
                    break;
                case BLAiDifficulty.Hard:
                    skill = tournament.CurrentStage switch
                    {
                        BLTournamentStage.RegularSeason => 5 + Mathf.Clamp(tournament.CurrentRegularSeasonRoundIndex, 0, 2),
                        BLTournamentStage.SemiFinal => 7,
                        BLTournamentStage.ThirdPlace => 7,
                        BLTournamentStage.Final => 8,
                        _ => 5
                    };
                    break;
                default:
                    skill = tournament.CurrentStage switch
                    {
                        BLTournamentStage.SemiFinal => 4,
                        BLTournamentStage.ThirdPlace => 4,
                        BLTournamentStage.Final => 5,
                        _ => 3
                    };
                    break;
            }

            return Mathf.Clamp(skill, 0, 8);
        }

        public void RollBallTheme()
        {
            BallTheme = BLBallCatalog.ResolveTheme(BLBallSelection.Random);
        }

        private void ResolveBallSelection(BLBallSelection ballSelection)
        {
            BallTheme = BLBallCatalog.ResolveTheme(ballSelection);
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
        public BLBallSelection SelectedQuickBallSelection;
        public BLBallSelection SelectedTournamentBallSelection;
        public BLBallSelection SelectedTrainingBallSelection;
        public BLBallSelection SelectedVersusBallSelection;

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
            SelectedQuickBallSelection = BLBallSelection.ClassicOriginal;
            SelectedTournamentBallSelection = BLBallSelection.ClassicOriginal;
            SelectedTrainingBallSelection = BLBallSelection.ClassicOriginal;
            SelectedVersusBallSelection = BLBallSelection.ClassicOriginal;
        }

        public string DifficultyLabel => Difficulty switch
        {
            BLAiDifficulty.Easy => "AI: EASY",
            BLAiDifficulty.Hard => "AI: HARD",
            _ => "AI: NORMAL"
        };

        public bool IsTournamentActive => SessionMode == BLSessionMode.Tournament && Tournament.Active;

        public void ToggleDifficulty()
        {
            Difficulty = Difficulty switch
            {
                BLAiDifficulty.Easy => BLAiDifficulty.Normal,
                BLAiDifficulty.Normal => BLAiDifficulty.Hard,
                _ => BLAiDifficulty.Easy
            };
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

        public void SetQuickBallSelection(BLBallSelection selection)
        {
            SelectedQuickBallSelection = selection;
        }

        public void SetTournamentBallSelection(BLBallSelection selection)
        {
            SelectedTournamentBallSelection = selection;
        }

        public void SetTrainingBallSelection(BLBallSelection selection)
        {
            SelectedTrainingBallSelection = selection;
        }

        public void SetVersusBallSelection(BLBallSelection selection)
        {
            SelectedVersusBallSelection = selection;
        }

        public void StartQuickGame()
        {
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = BLParticipantMode.OnePlayer;
            GameMode = 2;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
        }

        public void StartOnePlayer()
        {
            ParticipantMode = BLParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 1;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
            MatchPrepared = true;
        }

        public void StartTwoPlayers()
        {
            ParticipantMode = BLParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.MatchMode = 0;
            MatchData.StartPlayers2Match(SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        public void StartTwoPlayerVersus(int leftCharacterId, int rightCharacterId)
        {
            ParticipantMode = BLParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = BLSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.StartSelectedTwoPlayerMatch(leftCharacterId, rightCharacterId, SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        public void StartTraining()
        {
            ParticipantMode = BLParticipantMode.Training;
            Tournament.Reset();
            SessionMode = BLSessionMode.Training;
            GameMode = 3;
            MatchData.StartTraining(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
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

            MatchPrepared = false;
            if (Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            return true;
        }

        public bool BeginTournamentFinals()
        {
            if (!IsTournamentActive)
            {
                return false;
            }

            Tournament.BeginFinals();
            MatchPrepared = false;
            if (Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            return Tournament.HasPendingPlayerMatch;
        }

        public bool AdvanceTournament()
        {
            if (!IsTournamentActive)
            {
                return false;
            }

            Tournament.ApplyCurrentMatchResult(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            MatchPrepared = false;
            if (!Tournament.Completed && Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
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
