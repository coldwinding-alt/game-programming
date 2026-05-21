using System;
using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public enum rimrushAiDifficulty
    {
        Easy,
        Normal,
        Hard,
        Hell
    }

    public enum rimrushParticipantMode
    {
        OnePlayer,
        TwoPlayers,
        Training
    }

    public enum rimrushSessionMode
    {
        None,
        QuickMatch,
        Tournament,
        Training
    }

    public enum rimrushBallTheme
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

    public enum rimrushBallSelection
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

    public static class rimrushBallCatalog
    {
        private static readonly rimrushBallSelection[] OrderedSelections =
        {
            rimrushBallSelection.Random,
            rimrushBallSelection.ClassicOriginal,
            rimrushBallSelection.GhoulGreen,
            rimrushBallSelection.PumpkinEmber,
            rimrushBallSelection.MoonlitViolet,
            rimrushBallSelection.JackOLantern,
            rimrushBallSelection.EvilEye,
            rimrushBallSelection.Cursed8Ball,
            rimrushBallSelection.CandySwirl
        };

        private static readonly rimrushBallTheme[] ConcreteThemes =
        {
            rimrushBallTheme.ClassicOriginal,
            rimrushBallTheme.GhoulGreen,
            rimrushBallTheme.PumpkinEmber,
            rimrushBallTheme.MoonlitViolet,
            rimrushBallTheme.JackOLantern,
            rimrushBallTheme.EvilEye,
            rimrushBallTheme.Cursed8Ball,
            rimrushBallTheme.CandySwirl
        };

        public static rimrushBallSelection StepSelection(rimrushBallSelection current, int direction)
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

        public static rimrushBallTheme ResolveTheme(rimrushBallSelection selection)
        {
            return selection == rimrushBallSelection.Random
                ? ConcreteThemes[UnityEngine.Random.Range(0, ConcreteThemes.Length)]
                : ToTheme(selection);
        }

        public static rimrushBallTheme PreviewTheme(rimrushBallSelection selection)
        {
            return selection == rimrushBallSelection.Random
                ? rimrushBallTheme.ClassicOriginal
                : ToTheme(selection);
        }

        public static rimrushBallTheme ToTheme(rimrushBallSelection selection)
        {
            return selection switch
            {
                rimrushBallSelection.GhoulGreen => rimrushBallTheme.GhoulGreen,
                rimrushBallSelection.PumpkinEmber => rimrushBallTheme.PumpkinEmber,
                rimrushBallSelection.MoonlitViolet => rimrushBallTheme.MoonlitViolet,
                rimrushBallSelection.JackOLantern => rimrushBallTheme.JackOLantern,
                rimrushBallSelection.EvilEye => rimrushBallTheme.EvilEye,
                rimrushBallSelection.Cursed8Ball => rimrushBallTheme.Cursed8Ball,
                rimrushBallSelection.CandySwirl => rimrushBallTheme.CandySwirl,
                _ => rimrushBallTheme.ClassicOriginal
            };
        }

        public static string Label(rimrushBallSelection selection)
        {
            return selection switch
            {
                rimrushBallSelection.Random => "RANDOM",
                rimrushBallSelection.GhoulGreen => "GHOUL",
                rimrushBallSelection.PumpkinEmber => "EMBER",
                rimrushBallSelection.MoonlitViolet => "VIOLET",
                rimrushBallSelection.JackOLantern => "JACK",
                rimrushBallSelection.EvilEye => "EYE",
                rimrushBallSelection.Cursed8Ball => "8-BALL",
                rimrushBallSelection.CandySwirl => "SWIRL",
                _ => "CLASSIC"
            };
        }
    }

    public sealed class rimrushMatchData
    {
        public bool Restarted;
        public int FirstCharacterId;
        public int MatchMode;
        public rimrushBallTheme BallTheme;
        public int[] CharacterIds = new int[2];
        public string[][] Pb = { new string[0], new string[0] };
        public int[][] Skills = { new int[0], new int[0] };
        public int[] MatchScore = { 0, 0 };

        public rimrushMatchData(bool local)
        {
            FirstCharacterId = rimrushPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
            RollBallTheme();
        }

        public void ResetData()
        {
            CharacterIds = new[] { rimrushPlayersData.SanitizeCharacterId(0), rimrushPlayersData.SanitizeCharacterId(1, 0) };
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
                rimrushPlayersData.SanitizeCharacterId(FirstCharacterId),
                rimrushPlayersData.StepCharacterId(FirstCharacterId, 1)
            };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetQuickMatchOpponentSkill(rimrushAiDifficulty.Normal) } };
        }

        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        public void StartQuickMatch(int playerCharacterId, rimrushAiDifficulty difficulty, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var playerId = rimrushPlayersData.SanitizeCharacterId(playerCharacterId);
            var excluded = new List<int> { playerId };
            var opponentId = rimrushPlayersData.GetRandomCharacterId(excluded);
            var opponentSkill = GetQuickMatchOpponentSkill(difficulty);

            CharacterIds = new[] { playerId, opponentId };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartQuickLocalVersusMatch(rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var left = rimrushPlayersData.SanitizeCharacterId(0);
            var right = rimrushPlayersData.StepCharacterId(left, 1);

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void StartTraining(int characterId, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);

            CharacterIds = new[] { resolvedCharacterId, resolvedCharacterId };
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
        }

        public void StartRandomMatch(int playerCharacterId, rimrushAiDifficulty difficulty, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            StartQuickMatch(playerCharacterId, difficulty, ballSelection);
        }

        public void StartPlayers2Match(rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            var left = rimrushPlayersData.SanitizeCharacterId(CharacterIds[0]);
            var right = rimrushPlayersData.SanitizeCharacterId(CharacterIds[1], rimrushPlayersData.StepCharacterId(left, 1));

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public void StartTournamentMatch(rimrushTournamentData tournament, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
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
                rimrushPlayersData.SanitizeCharacterId(tournament.PlayerCharacterId),
                rimrushPlayersData.SanitizeCharacterId(tournament.CurrentOpponentCharacterId)
            };

            var opponentSkill = GetTournamentOpponentSkill(tournament);

            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartSelectedTwoPlayerMatch(int leftCharacterId, int rightCharacterId, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            ResetAll();
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            CharacterIds = new[]
            {
                rimrushPlayersData.SanitizeCharacterId(leftCharacterId),
                rimrushPlayersData.SanitizeCharacterId(rightCharacterId, leftCharacterId)
            };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
        }

        private static int GetQuickMatchOpponentSkill(rimrushAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case rimrushAiDifficulty.Easy:
                    return 1;
                case rimrushAiDifficulty.Hell:
                    return 10;
                case rimrushAiDifficulty.Hard:
                    return 5;
                default:
                    return 2;
            }
        }

        private static int GetTournamentOpponentSkill(rimrushTournamentData tournament)
        {
            if (tournament == null)
            {
                return 0;
            }

            int skill;
            switch (tournament.Difficulty)
            {
                case rimrushAiDifficulty.Easy:
                    skill = tournament.CurrentStage switch
                    {
                        rimrushTournamentStage.SemiFinal => 3,
                        rimrushTournamentStage.ThirdPlace => 3,
                        rimrushTournamentStage.Final => 4,
                        _ => 2
                    };
                    break;
                case rimrushAiDifficulty.Hard:
                    skill = tournament.CurrentStage switch
                    {
                        rimrushTournamentStage.RegularSeason => 5 + Mathf.Clamp(tournament.CurrentRegularSeasonRoundIndex, 0, 2),
                        rimrushTournamentStage.SemiFinal => 7,
                        rimrushTournamentStage.ThirdPlace => 7,
                        rimrushTournamentStage.Final => 8,
                        _ => 5
                    };
                    break;
                case rimrushAiDifficulty.Hell:
                    skill = tournament.CurrentStage switch
                    {
                        rimrushTournamentStage.RegularSeason => 8 + Mathf.Clamp(tournament.CurrentRegularSeasonRoundIndex, 0, 2),
                        rimrushTournamentStage.SemiFinal => 10,
                        rimrushTournamentStage.ThirdPlace => 10,
                        rimrushTournamentStage.Final => 11,
                        _ => 8
                    };
                    break;
                default:
                    skill = tournament.CurrentStage switch
                    {
                        rimrushTournamentStage.SemiFinal => 4,
                        rimrushTournamentStage.ThirdPlace => 4,
                        rimrushTournamentStage.Final => 5,
                        _ => 3
                    };
                    break;
            }

            return Mathf.Clamp(skill, 0, rimrushAISkillsData.MaxSkillIndex);
        }

        public void RollBallTheme()
        {
            BallTheme = rimrushBallCatalog.ResolveTheme(rimrushBallSelection.Random);
        }

        private void ResolveBallSelection(rimrushBallSelection ballSelection)
        {
            BallTheme = rimrushBallCatalog.ResolveTheme(ballSelection);
        }
    }

    public sealed class rimrushInventory
    {
        private static rimrushInventory instance;

        public static rimrushInventory Instance => instance ?? (instance = new rimrushInventory());

        public int GameMode;
        public rimrushMatchData MatchData;
        public rimrushTournamentData Tournament;
        public bool FirstRun = true;
        public bool FirstRun2 = true;
        public bool MatchPrepared;
        public rimrushAiDifficulty Difficulty;
        public rimrushParticipantMode ParticipantMode;
        public rimrushSessionMode SessionMode;
        public int SelectedQuickCharacterId;
        public int SelectedTournamentCharacterId;
        public int SelectedTrainingCharacterId;
        public rimrushBallSelection SelectedQuickBallSelection;
        public rimrushBallSelection SelectedTournamentBallSelection;
        public rimrushBallSelection SelectedTrainingBallSelection;
        public rimrushBallSelection SelectedVersusBallSelection;

        private rimrushInventory()
        {
            GameMode = 1;
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            SessionMode = rimrushSessionMode.None;
            MatchData = new rimrushMatchData(true);
            Tournament = new rimrushTournamentData();
            MatchData.MatchMode = 0;
            Difficulty = rimrushAiDifficulty.Normal;
            SelectedQuickCharacterId = MatchData.FirstCharacterId;
            SelectedTournamentCharacterId = MatchData.FirstCharacterId;
            SelectedTrainingCharacterId = MatchData.FirstCharacterId;
            SelectedQuickBallSelection = rimrushBallSelection.ClassicOriginal;
            SelectedTournamentBallSelection = rimrushBallSelection.ClassicOriginal;
            SelectedTrainingBallSelection = rimrushBallSelection.ClassicOriginal;
            SelectedVersusBallSelection = rimrushBallSelection.ClassicOriginal;
        }

        public string DifficultyLabel => Difficulty switch
        {
            rimrushAiDifficulty.Easy => "AI: EASY",
            rimrushAiDifficulty.Hard => "AI: HARD",
            rimrushAiDifficulty.Hell => "AI: HELL",
            _ => "AI: NORMAL"
        };

        public bool IsTournamentActive => SessionMode == rimrushSessionMode.Tournament && Tournament.Active;

        public void ToggleDifficulty()
        {
            Difficulty = Difficulty switch
            {
                rimrushAiDifficulty.Easy => rimrushAiDifficulty.Normal,
                rimrushAiDifficulty.Normal => rimrushAiDifficulty.Hard,
                rimrushAiDifficulty.Hard => rimrushAiDifficulty.Hell,
                _ => rimrushAiDifficulty.Easy
            };
        }

        public void SetParticipantMode(rimrushParticipantMode participantMode)
        {
            ParticipantMode = participantMode;
            if (participantMode == rimrushParticipantMode.Training)
            {
                SessionMode = rimrushSessionMode.Training;
            }
        }

        public void SetQuickSelection(int characterId)
        {
            SelectedQuickCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
        }

        public void SetTournamentSelection(int characterId)
        {
            SelectedTournamentCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
        }

        public void SetTrainingSelection(int characterId)
        {
            SelectedTrainingCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
        }

        public void SetQuickBallSelection(rimrushBallSelection selection)
        {
            SelectedQuickBallSelection = selection;
        }

        public void SetTournamentBallSelection(rimrushBallSelection selection)
        {
            SelectedTournamentBallSelection = selection;
        }

        public void SetTrainingBallSelection(rimrushBallSelection selection)
        {
            SelectedTrainingBallSelection = selection;
        }

        public void SetVersusBallSelection(rimrushBallSelection selection)
        {
            SelectedVersusBallSelection = selection;
        }

        public void StartQuickGame()
        {
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            GameMode = 2;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
        }

        public void StartOnePlayer()
        {
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            GameMode = 1;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
            MatchPrepared = true;
        }

        public void StartTwoPlayers()
        {
            ParticipantMode = rimrushParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.MatchMode = 0;
            MatchData.StartPlayers2Match(SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        public void StartTwoPlayerVersus(int leftCharacterId, int rightCharacterId)
        {
            ParticipantMode = rimrushParticipantMode.TwoPlayers;
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            GameMode = 4;
            MatchData.StartSelectedTwoPlayerMatch(leftCharacterId, rightCharacterId, SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        public void StartTraining()
        {
            ParticipantMode = rimrushParticipantMode.Training;
            Tournament.Reset();
            SessionMode = rimrushSessionMode.Training;
            GameMode = 3;
            MatchData.StartTraining(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        public bool BeginTournament()
        {
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            SessionMode = rimrushSessionMode.Tournament;
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
            SessionMode = rimrushSessionMode.None;
            MatchPrepared = false;
        }
    }
}
