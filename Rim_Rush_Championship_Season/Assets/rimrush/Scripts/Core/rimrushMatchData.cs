// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushMatchData 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

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
        Training,
        Tutorial
    }

    public enum rimrushSessionMode
    {
        None,
        QuickMatch,
        Adventure,
        Tournament,
        Training,
        Tutorial
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

        /// <summary>
        /// Executes Step Selection for the rimrushBallCatalog workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="current">Input value used by this step of the workflow.</param>
        /// <param name="direction">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Resolve Theme for the rimrushBallCatalog workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static rimrushBallTheme ResolveTheme(rimrushBallSelection selection)
        {
            return selection == rimrushBallSelection.Random
                ? ConcreteThemes[UnityEngine.Random.Range(0, ConcreteThemes.Length)]
                : ToTheme(selection);
        }

        /// <summary>
        /// Executes Preview Theme for the rimrushBallCatalog workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static rimrushBallTheme PreviewTheme(rimrushBallSelection selection)
        {
            return selection == rimrushBallSelection.Random
                ? rimrushBallTheme.ClassicOriginal
                : ToTheme(selection);
        }

        /// <summary>
        /// Executes To Theme for the rimrushBallCatalog workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Label for the rimrushBallCatalog workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes rimrush Match Data for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="local">Input value used by this step of the workflow.</param>
        public rimrushMatchData(bool local)
        {
            FirstCharacterId = rimrushPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
            RollBallTheme();
        }

        /// <summary>
        /// Executes Reset Data for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ResetData()
        {
            CharacterIds = new[] { rimrushPlayersData.SanitizeCharacterId(0), rimrushPlayersData.SanitizeCharacterId(1, 0) };
        }

        /// <summary>
        /// Executes Reset Partly for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ResetPartly()
        {
            MatchMode = 0;
            Pb = new[] { new string[0], new string[0] };
            Skills = new[] { new int[0], new int[0] };
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// Executes Reset All for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ResetAll()
        {
            ResetData();
            ResetPartly();
        }

        /// <summary>
        /// Executes Base Init for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
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

        /// <summary>
        /// Executes Reset Score for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// Executes Start Quick Match for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="playerCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="difficulty">Input value used by this step of the workflow.</param>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Start Quick Local Versus Match for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Start Training for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
        public void StartTraining(int characterId, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);

            CharacterIds = new[] { resolvedCharacterId, resolvedCharacterId };
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
        }

        /// <summary>
        /// Executes Start Tutorial for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
        public void StartTutorial(int characterId, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
            var opponentCharacterId = rimrushPlayersData.SanitizeCharacterId(7, resolvedCharacterId);

            CharacterIds = new[] { resolvedCharacterId, opponentCharacterId };
            Pb = new[] { new[] { "P0" }, new[] { "T0" } };
            Skills = new[] { new[] { 0 }, new[] { 2 } };
        }

        /// <summary>
        /// Executes Start Random Match for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="playerCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="difficulty">Input value used by this step of the workflow.</param>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
        public void StartRandomMatch(int playerCharacterId, rimrushAiDifficulty difficulty, rimrushBallSelection ballSelection = rimrushBallSelection.Random)
        {
            StartQuickMatch(playerCharacterId, difficulty, ballSelection);
        }

        /// <summary>
        /// Executes Start Players2 Match for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Start Tournament Match for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="tournament">Input value used by this step of the workflow.</param>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
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

        public void StartAdventureMatch(rimrushAdventureData adventure)
        {
            StartAdventureMatch(adventure, rimrushAiDifficulty.Normal);
        }

        public void StartAdventureMatch(rimrushAdventureData adventure, rimrushAiDifficulty difficulty)
        {
            ResetAll();
            if (adventure == null || !adventure.Active || adventure.Completed || !adventure.HasPendingPlayerMatch)
            {
                return;
            }

            var level = adventure.CurrentLevel;
            MatchMode = 0;
            ResolveBallSelection(level.BallSelection);
            CharacterIds = new[]
            {
                rimrushPlayersData.SanitizeCharacterId(adventure.PlayerCharacterId),
                rimrushPlayersData.SanitizeCharacterId(level.WardenCharacterId)
            };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetAdventureOpponentSkill(level, difficulty) } };
        }

        /// <summary>
        /// Executes Start Selected Two Player Match for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="leftCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="rightCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Who Wins for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
        }

        /// <summary>
        /// Executes Get Quick Match Opponent Skill for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="difficulty">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        private static int GetAdventureOpponentSkill(rimrushAdventureLevelDefinition level, rimrushAiDifficulty difficulty)
        {
            var baseSkill = level != null ? level.OpponentSkill : 0;
            int adjustedSkill;
            switch (difficulty)
            {
                case rimrushAiDifficulty.Easy:
                    adjustedSkill = baseSkill - 1;
                    break;
                case rimrushAiDifficulty.Hard:
                    adjustedSkill = baseSkill + 2;
                    break;
                case rimrushAiDifficulty.Hell:
                    adjustedSkill = baseSkill + 4;
                    break;
                default:
                    adjustedSkill = baseSkill;
                    break;
            }

            return Mathf.Clamp(adjustedSkill, 0, rimrushAISkillsData.MaxSkillIndex);
        }

        /// <summary>
        /// Executes Get Tournament Opponent Skill for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="tournament">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

        /// <summary>
        /// Executes Roll Ball Theme for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void RollBallTheme()
        {
            BallTheme = rimrushBallCatalog.ResolveTheme(rimrushBallSelection.Random);
        }

        /// <summary>
        /// Executes Resolve Ball Selection for the rimrushMatchData workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ballSelection">Input value used by this step of the workflow.</param>
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
        public rimrushAdventureData Adventure;
        public rimrushTournamentData Tournament;
        public bool FirstRun = true;
        public bool FirstRun2 = true;
        public bool MatchPrepared;
        public rimrushAiDifficulty Difficulty;
        public rimrushParticipantMode ParticipantMode;
        public rimrushSessionMode SessionMode;
        public rimrushTutorialNextAction PendingTutorialNextAction;
        public int SelectedQuickCharacterId;
        public int SelectedTournamentCharacterId;
        public int SelectedTrainingCharacterId;
        public rimrushBallSelection SelectedQuickBallSelection;
        public rimrushBallSelection SelectedTournamentBallSelection;
        public rimrushBallSelection SelectedTrainingBallSelection;
        public rimrushBallSelection SelectedVersusBallSelection;

        /// <summary>
        /// Executes rimrush Inventory for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private rimrushInventory()
        {
            GameMode = 1;
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            SessionMode = rimrushSessionMode.None;
            MatchData = new rimrushMatchData(true);
            Adventure = new rimrushAdventureData();
            Tournament = new rimrushTournamentData();
            MatchData.MatchMode = 0;
            Difficulty = rimrushAiDifficulty.Normal;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
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
        public bool IsAdventureActive => SessionMode == rimrushSessionMode.Adventure && Adventure.Active;

        /// <summary>
        /// Executes Toggle Difficulty for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
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

        /// <summary>
        /// Executes Set Participant Mode for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="participantMode">Input value used by this step of the workflow.</param>
        public void SetParticipantMode(rimrushParticipantMode participantMode)
        {
            ParticipantMode = participantMode;
            if (participantMode == rimrushParticipantMode.Training)
            {
                SessionMode = rimrushSessionMode.Training;
            }
            else if (participantMode == rimrushParticipantMode.Tutorial)
            {
                SessionMode = rimrushSessionMode.Tutorial;
            }
        }

        /// <summary>
        /// Executes Set Quick Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        public void SetQuickSelection(int characterId)
        {
            SelectedQuickCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Executes Set Tournament Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        public void SetTournamentSelection(int characterId)
        {
            SelectedTournamentCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Executes Set Training Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        public void SetTrainingSelection(int characterId)
        {
            SelectedTrainingCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Executes Set Quick Ball Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        public void SetQuickBallSelection(rimrushBallSelection selection)
        {
            SelectedQuickBallSelection = selection;
        }

        /// <summary>
        /// Executes Set Tournament Ball Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        public void SetTournamentBallSelection(rimrushBallSelection selection)
        {
            SelectedTournamentBallSelection = selection;
        }

        /// <summary>
        /// Executes Set Training Ball Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        public void SetTrainingBallSelection(rimrushBallSelection selection)
        {
            SelectedTrainingBallSelection = selection;
        }

        /// <summary>
        /// Executes Set Versus Ball Selection for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="selection">Input value used by this step of the workflow.</param>
        public void SetVersusBallSelection(rimrushBallSelection selection)
        {
            SelectedVersusBallSelection = selection;
        }

        /// <summary>
        /// Executes Start Quick Game for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void StartQuickGame()
        {
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            GameMode = rimrushGameModeIds.QuickMatch;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
        }

        /// <summary>
        /// Executes Start One Player for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void StartOnePlayer()
        {
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            GameMode = rimrushGameModeIds.RandomQuick;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Executes Start Two Players for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void StartTwoPlayers()
        {
            ParticipantMode = rimrushParticipantMode.TwoPlayers;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            GameMode = rimrushGameModeIds.TwoPlayers;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartPlayers2Match(SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Executes Start Two Player Versus for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="leftCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="rightCharacterId">Input value used by this step of the workflow.</param>
        public void StartTwoPlayerVersus(int leftCharacterId, int rightCharacterId)
        {
            ParticipantMode = rimrushParticipantMode.TwoPlayers;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = rimrushSessionMode.QuickMatch;
            GameMode = rimrushGameModeIds.TwoPlayers;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            MatchData.StartSelectedTwoPlayerMatch(leftCharacterId, rightCharacterId, SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Executes Start Training for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void StartTraining()
        {
            ParticipantMode = rimrushParticipantMode.Training;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = rimrushSessionMode.Training;
            GameMode = rimrushGameModeIds.Training;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            MatchData.StartTraining(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Executes Start Tutorial for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void StartTutorial()
        {
            ParticipantMode = rimrushParticipantMode.Tutorial;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = rimrushSessionMode.Tutorial;
            GameMode = rimrushGameModeIds.Tutorial;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            MatchData.StartTutorial(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Executes Begin Tournament for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool BeginTournament()
        {
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            Adventure.Reset();
            SessionMode = rimrushSessionMode.Tournament;
            GameMode = rimrushGameModeIds.RandomQuick;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
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

        /// <summary>
        /// Executes Begin Tournament Finals for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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

        /// <summary>
        /// Executes Advance Tournament for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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

        /// <summary>
        /// Executes Abandon Tournament for the rimrushInventory workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void AbandonTournament()
        {
            Tournament.Reset();
            SessionMode = rimrushSessionMode.None;
            MatchPrepared = false;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
        }

        public void BeginAdventure(int playerCharacterId)
        {
            ParticipantMode = rimrushParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = rimrushSessionMode.Adventure;
            GameMode = rimrushGameModeIds.RandomQuick;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
            Adventure.Create(playerCharacterId);
            MatchPrepared = false;
        }

        public bool StartAdventureLevel(int levelIndex, int playerCharacterId)
        {
            if (!IsAdventureActive || Adventure.Completed)
            {
                BeginAdventure(playerCharacterId);
            }

            if (!Adventure.SelectLevel(levelIndex))
            {
                return false;
            }

            MatchData.StartAdventureMatch(Adventure, Difficulty);
            MatchPrepared = true;
            return MatchData.Pb != null && MatchData.Pb.Length >= 2 && MatchData.Pb[1].Length > 0;
        }

        public bool RestartAdventureLevel()
        {
            if (!IsAdventureActive || Adventure.Completed || !Adventure.HasPendingPlayerMatch)
            {
                return false;
            }

            MatchData.StartAdventureMatch(Adventure, Difficulty);
            MatchPrepared = true;
            return true;
        }

        public void AdvanceAdventure(bool playerWon)
        {
            if (!IsAdventureActive)
            {
                return;
            }

            Adventure.ApplyCurrentMatchResult(playerWon);
            MatchPrepared = false;
        }

        public void AbandonAdventure()
        {
            Adventure.Reset();
            SessionMode = rimrushSessionMode.None;
            MatchPrepared = false;
            PendingTutorialNextAction = rimrushTutorialNextAction.None;
        }
    }
}
