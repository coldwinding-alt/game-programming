// 比赛配置数据和物品清单
// 定义一场比赛需要的配置信息：双方角色、难度、游戏模式。还包含 mlpInventory 类，用来保存玩家的进度、选择和解锁状态。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    public enum mlpAiDifficulty
    {
        Easy,
        Normal,
        Hard,
        Hell
    }

    public enum mlpParticipantMode
    {
        OnePlayer,
        TwoPlayers,
        Training,
        Tutorial
    }

    public enum mlpSessionMode
    {
        None,
        QuickMatch,
        Adventure,
        Tournament,
        Training,
        Tutorial
    }

    public enum mlpBallTheme
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

    public enum mlpBallSelection
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

    public static class mlpBallCatalog
    {
        private static readonly mlpBallSelection[] OrderedSelections =
        {
            mlpBallSelection.Random,
            mlpBallSelection.ClassicOriginal,
            mlpBallSelection.GhoulGreen,
            mlpBallSelection.PumpkinEmber,
            mlpBallSelection.MoonlitViolet,
            mlpBallSelection.JackOLantern,
            mlpBallSelection.EvilEye,
            mlpBallSelection.Cursed8Ball,
            mlpBallSelection.CandySwirl
        };

        private static readonly mlpBallTheme[] ConcreteThemes =
        {
            mlpBallTheme.ClassicOriginal,
            mlpBallTheme.GhoulGreen,
            mlpBallTheme.PumpkinEmber,
            mlpBallTheme.MoonlitViolet,
            mlpBallTheme.JackOLantern,
            mlpBallTheme.EvilEye,
            mlpBallTheme.Cursed8Ball,
            mlpBallTheme.CandySwirl
        };

        /// <summary>
        /// Moves to the next or previous ball selection in the ordered list, wrapping around at the ends.
        /// </summary>
        /// <param name="current">The currently selected ball skin.</param>
        /// <param name="direction">Positive to step forward, negative to step backward.</param>
        /// <returns>The next ball selection in the given direction.</returns>
        public static mlpBallSelection StepSelection(mlpBallSelection current, int direction)
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
        /// Converts a ball selection to a concrete theme. If "Random" is selected, picks a random non-random theme.
        /// </summary>
        /// <param name="selection">The ball selection to resolve.</param>
        /// <returns>The concrete ball theme to use for rendering.</returns>
        public static mlpBallTheme ResolveTheme(mlpBallSelection selection)
        {
            return selection == mlpBallSelection.Random
                ? ConcreteThemes[UnityEngine.Random.Range(0, ConcreteThemes.Length)]
                : ToTheme(selection);
        }

        /// <summary>
        /// Returns the theme to show in the UI preview. For "Random", always shows the classic theme as a placeholder.
        /// </summary>
        /// <param name="selection">The ball selection to preview.</param>
        /// <returns>The theme to display in the selection UI.</returns>
        public static mlpBallTheme PreviewTheme(mlpBallSelection selection)
        {
            return selection == mlpBallSelection.Random
                ? mlpBallTheme.ClassicOriginal
                : ToTheme(selection);
        }

        /// <summary>
        /// Directly maps a non-random ball selection to its corresponding ball theme.
        /// </summary>
        /// <param name="selection">The ball selection (must not be Random).</param>
        /// <returns>The matching ball theme.</returns>
        public static mlpBallTheme ToTheme(mlpBallSelection selection)
        {
            return selection switch
            {
                mlpBallSelection.GhoulGreen => mlpBallTheme.GhoulGreen,
                mlpBallSelection.PumpkinEmber => mlpBallTheme.PumpkinEmber,
                mlpBallSelection.MoonlitViolet => mlpBallTheme.MoonlitViolet,
                mlpBallSelection.JackOLantern => mlpBallTheme.JackOLantern,
                mlpBallSelection.EvilEye => mlpBallTheme.EvilEye,
                mlpBallSelection.Cursed8Ball => mlpBallTheme.Cursed8Ball,
                mlpBallSelection.CandySwirl => mlpBallTheme.CandySwirl,
                _ => mlpBallTheme.ClassicOriginal
            };
        }

        /// <summary>
        /// Returns the short display label for a ball selection (e.g. "GHOUL", "EMBER", "RANDOM").
        /// </summary>
        /// <param name="selection">The ball selection to get a label for.</param>
        /// <returns>A short uppercase string shown in the UI.</returns>
        public static string Label(mlpBallSelection selection)
        {
            return selection switch
            {
                mlpBallSelection.Random => "RANDOM",
                mlpBallSelection.GhoulGreen => "GHOUL",
                mlpBallSelection.PumpkinEmber => "EMBER",
                mlpBallSelection.MoonlitViolet => "VIOLET",
                mlpBallSelection.JackOLantern => "JACK",
                mlpBallSelection.EvilEye => "EYE",
                mlpBallSelection.Cursed8Ball => "8-BALL",
                mlpBallSelection.CandySwirl => "SWIRL",
                _ => "CLASSIC"
            };
        }
    }

    public sealed class mlpMatchData
    {
        public bool Restarted;
        public int FirstCharacterId;
        public int MatchMode;
        public mlpBallTheme BallTheme;
        public int[] CharacterIds = new int[2];
        public string[][] Pb = { new string[0], new string[0] };
        public int[][] Skills = { new int[0], new int[0] };
        public int[] MatchScore = { 0, 0 };

        /// <summary>
        /// Creates match data with default character IDs and a random ball theme.
        /// </summary>
        /// <param name="local">True for local play (uses character index 0 as default), false for networked.</param>
        public mlpMatchData(bool local)
        {
            FirstCharacterId = mlpPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
            RollBallTheme();
        }

        /// <summary>
        /// Resets both team character IDs back to their default values.
        /// </summary>
        public void ResetData()
        {
            CharacterIds = new[] { mlpPlayersData.SanitizeCharacterId(0), mlpPlayersData.SanitizeCharacterId(1, 0) };
        }

        /// <summary>
        /// Resets match mode, player brains, skills, and scores. Does not change character IDs or ball theme.
        /// </summary>
        public void ResetPartly()
        {
            MatchMode = 0;
            Pb = new[] { new string[0], new string[0] };
            Skills = new[] { new int[0], new int[0] };
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// Resets everything back to defaults: character IDs, brains, skills, scores, and match mode.
        /// </summary>
        public void ResetAll()
        {
            ResetData();
            ResetPartly();
        }

        /// <summary>
        /// Sets up default match configuration: both team characters, default brain strings, and default skill levels.
        /// </summary>
        public void BaseInit()
        {
            MatchMode = 0;
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(FirstCharacterId),
                mlpPlayersData.StepCharacterId(FirstCharacterId, 1)
            };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetQuickMatchOpponentSkill(mlpAiDifficulty.Normal) } };
        }

        /// <summary>
        /// Sets both team scores back to zero.
        /// </summary>
        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// Configures a quick match: picks a random opponent, sets brain strings for human vs AI, and resolves the ball theme.
        /// </summary>
        /// <param name="playerCharacterId">The player's chosen character ID.</param>
        /// <param name="difficulty">AI difficulty level.</param>
        /// <param name="ballSelection">Which ball skin to use (Random picks one at random).</param>
        public void StartQuickMatch(int playerCharacterId, mlpAiDifficulty difficulty, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var playerId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
            var excluded = new List<int> { playerId };
            var opponentId = mlpPlayersData.GetRandomCharacterId(excluded);
            var opponentSkill = GetQuickMatchOpponentSkill(difficulty);

            CharacterIds = new[] { playerId, opponentId };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        /// <summary>
        /// Configures a local 2-player versus match with two distinct characters and player-brain strings.
        /// </summary>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartQuickLocalVersusMatch(mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var left = mlpPlayersData.SanitizeCharacterId(0);
            var right = mlpPlayersData.StepCharacterId(left, 1);

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// Configures a training session with one player character and no opponent.
        /// </summary>
        /// <param name="characterId">The player's chosen character ID.</param>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartTraining(int characterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);

            CharacterIds = new[] { resolvedCharacterId, resolvedCharacterId };
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
        }

        /// <summary>
        /// Configures a tutorial match with a specific opponent character and tutorial brain string.
        /// </summary>
        /// <param name="characterId">The player's chosen character ID.</param>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartTutorial(int characterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
            var opponentCharacterId = mlpPlayersData.SanitizeCharacterId(7, resolvedCharacterId);

            CharacterIds = new[] { resolvedCharacterId, opponentCharacterId };
            Pb = new[] { new[] { "P0" }, new[] { "T0" } };
            Skills = new[] { new[] { 0 }, new[] { 2 } };
        }

        /// <summary>
        /// Starts a random match. Currently just delegates to StartQuickMatch.
        /// </summary>
        /// <param name="playerCharacterId">The player's chosen character ID.</param>
        /// <param name="difficulty">AI difficulty level.</param>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartRandomMatch(int playerCharacterId, mlpAiDifficulty difficulty, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            StartQuickMatch(playerCharacterId, difficulty, ballSelection);
        }

        /// <summary>
        /// Configures a 2-player local match using the currently stored character IDs.
        /// </summary>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartPlayers2Match(mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            var left = mlpPlayersData.SanitizeCharacterId(CharacterIds[0]);
            var right = mlpPlayersData.SanitizeCharacterId(CharacterIds[1], mlpPlayersData.StepCharacterId(left, 1));

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// Configures the next match in a tournament, using the tournament's current opponent and stage-based skill level.
        /// </summary>
        /// <param name="tournament">The active tournament data.</param>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartTournamentMatch(mlpTournamentData tournament, mlpBallSelection ballSelection = mlpBallSelection.Random)
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
                mlpPlayersData.SanitizeCharacterId(tournament.PlayerCharacterId),
                mlpPlayersData.SanitizeCharacterId(tournament.CurrentOpponentCharacterId)
            };

            var opponentSkill = GetTournamentOpponentSkill(tournament);

            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartAdventureMatch(mlpAdventureData adventure)
        {
            StartAdventureMatch(adventure, mlpAiDifficulty.Normal);
        }

        public void StartAdventureMatch(mlpAdventureData adventure, mlpAiDifficulty difficulty)
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
                mlpPlayersData.SanitizeCharacterId(adventure.PlayerCharacterId),
                mlpPlayersData.SanitizeCharacterId(level.WardenCharacterId)
            };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetAdventureOpponentSkill(level, difficulty) } };
        }

        /// <summary>
        /// Configures a 2-player match with specific character choices for each player.
        /// </summary>
        /// <param name="leftCharacterId">Character ID for the left-side player.</param>
        /// <param name="rightCharacterId">Character ID for the right-side player.</param>
        /// <param name="ballSelection">Which ball skin to use.</param>
        public void StartSelectedTwoPlayerMatch(int leftCharacterId, int rightCharacterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(leftCharacterId),
                mlpPlayersData.SanitizeCharacterId(rightCharacterId, leftCharacterId)
            };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// Compares both team scores to determine the winner.
        /// </summary>
        /// <returns>-1 if left team wins, 1 if right team wins, 0 if tied.</returns>
        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
        }

        /// <summary>
        /// Maps a difficulty level to a numeric AI skill index for quick matches.
        /// </summary>
        /// <param name="difficulty">The chosen AI difficulty.</param>
        /// <returns>A skill index (1 = easy, 2 = normal, 5 = hard, 10 = hell).</returns>
        private static int GetQuickMatchOpponentSkill(mlpAiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case mlpAiDifficulty.Easy:
                    return 1;
                case mlpAiDifficulty.Hell:
                    return 10;
                case mlpAiDifficulty.Hard:
                    return 5;
                default:
                    return 2;
            }
        }

        private static int GetAdventureOpponentSkill(mlpAdventureLevelDefinition level, mlpAiDifficulty difficulty)
        {
            var baseSkill = level != null ? level.OpponentSkill : 0;
            int adjustedSkill;
            switch (difficulty)
            {
                case mlpAiDifficulty.Easy:
                    adjustedSkill = baseSkill - 1;
                    break;
                case mlpAiDifficulty.Hard:
                    adjustedSkill = baseSkill + 2;
                    break;
                case mlpAiDifficulty.Hell:
                    adjustedSkill = baseSkill + 4;
                    break;
                default:
                    adjustedSkill = baseSkill;
                    break;
            }

            return Mathf.Clamp(adjustedSkill, 0, mlpAISkillsData.MaxSkillIndex);
        }

        /// <summary>
        /// Calculates the AI skill level for a tournament match based on the tournament's difficulty and current stage.
        /// Later rounds and higher difficulties give opponents higher skill levels.
        /// </summary>
        /// <param name="tournament">The active tournament data.</param>
        /// <returns>A clamped skill index appropriate for the current tournament stage.</returns>
        private static int GetTournamentOpponentSkill(mlpTournamentData tournament)
        {
            if (tournament == null)
            {
                return 0;
            }

            int skill;
            switch (tournament.Difficulty)
            {
                case mlpAiDifficulty.Easy:
                    skill = tournament.CurrentStage switch
                    {
                        mlpTournamentStage.SemiFinal => 3,
                        mlpTournamentStage.ThirdPlace => 3,
                        mlpTournamentStage.Final => 4,
                        _ => 2
                    };
                    break;
                case mlpAiDifficulty.Hard:
                    skill = tournament.CurrentStage switch
                    {
                        mlpTournamentStage.RegularSeason => 5 + Mathf.Clamp(tournament.CurrentRegularSeasonRoundIndex, 0, 2),
                        mlpTournamentStage.SemiFinal => 7,
                        mlpTournamentStage.ThirdPlace => 7,
                        mlpTournamentStage.Final => 8,
                        _ => 5
                    };
                    break;
                case mlpAiDifficulty.Hell:
                    skill = tournament.CurrentStage switch
                    {
                        mlpTournamentStage.RegularSeason => 8 + Mathf.Clamp(tournament.CurrentRegularSeasonRoundIndex, 0, 2),
                        mlpTournamentStage.SemiFinal => 10,
                        mlpTournamentStage.ThirdPlace => 10,
                        mlpTournamentStage.Final => 11,
                        _ => 8
                    };
                    break;
                default:
                    skill = tournament.CurrentStage switch
                    {
                        mlpTournamentStage.SemiFinal => 4,
                        mlpTournamentStage.ThirdPlace => 4,
                        mlpTournamentStage.Final => 5,
                        _ => 3
                    };
                    break;
            }

            return Mathf.Clamp(skill, 0, mlpAISkillsData.MaxSkillIndex);
        }

        /// <summary>
        /// Randomly picks a new ball theme for the current match.
        /// </summary>
        public void RollBallTheme()
        {
            BallTheme = mlpBallCatalog.ResolveTheme(mlpBallSelection.Random);
        }

        /// <summary>
        /// Converts a ball selection choice into a concrete ball theme and stores it.
        /// </summary>
        /// <param name="ballSelection">The chosen ball selection (may be Random).</param>
        private void ResolveBallSelection(mlpBallSelection ballSelection)
        {
            BallTheme = mlpBallCatalog.ResolveTheme(ballSelection);
        }
    }

    public sealed class mlpInventory
    {
        private static mlpInventory instance;

        public static mlpInventory Instance => instance ?? (instance = new mlpInventory());

        public int GameMode;
        public mlpMatchData MatchData;
        public mlpAdventureData Adventure;
        public mlpTournamentData Tournament;
        public bool FirstRun = true;
        public bool FirstRun2 = true;
        public bool MatchPrepared;
        public mlpAiDifficulty Difficulty;
        public mlpParticipantMode ParticipantMode;
        public mlpSessionMode SessionMode;
        public mlpTutorialNextAction PendingTutorialNextAction;
        public int SelectedQuickCharacterId;
        public int SelectedTournamentCharacterId;
        public int SelectedTrainingCharacterId;
        public mlpBallSelection SelectedQuickBallSelection;
        public mlpBallSelection SelectedTournamentBallSelection;
        public mlpBallSelection SelectedTrainingBallSelection;
        public mlpBallSelection SelectedVersusBallSelection;

        /// <summary>
        /// Initializes the global inventory with default game mode, difficulty, character selections, and ball selections.
        /// </summary>
        private mlpInventory()
        {
            GameMode = 1;
            ParticipantMode = mlpParticipantMode.OnePlayer;
            SessionMode = mlpSessionMode.None;
            MatchData = new mlpMatchData(true);
            Adventure = new mlpAdventureData();
            Tournament = new mlpTournamentData();
            MatchData.MatchMode = 0;
            Difficulty = mlpAiDifficulty.Normal;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            SelectedQuickCharacterId = MatchData.FirstCharacterId;
            SelectedTournamentCharacterId = MatchData.FirstCharacterId;
            SelectedTrainingCharacterId = MatchData.FirstCharacterId;
            SelectedQuickBallSelection = mlpBallSelection.ClassicOriginal;
            SelectedTournamentBallSelection = mlpBallSelection.ClassicOriginal;
            SelectedTrainingBallSelection = mlpBallSelection.ClassicOriginal;
            SelectedVersusBallSelection = mlpBallSelection.ClassicOriginal;
        }

        public string DifficultyLabel => Difficulty switch
        {
            mlpAiDifficulty.Easy => "AI: EASY",
            mlpAiDifficulty.Hard => "AI: HARD",
            mlpAiDifficulty.Hell => "AI: HELL",
            _ => "AI: NORMAL"
        };

        public bool IsTournamentActive => SessionMode == mlpSessionMode.Tournament && Tournament.Active;
        public bool IsAdventureActive => SessionMode == mlpSessionMode.Adventure && Adventure.Active;

        /// <summary>
        /// Cycles through difficulty levels in order: Easy -> Normal -> Hard -> Hell -> Easy.
        /// </summary>
        public void ToggleDifficulty()
        {
            Difficulty = Difficulty switch
            {
                mlpAiDifficulty.Easy => mlpAiDifficulty.Normal,
                mlpAiDifficulty.Normal => mlpAiDifficulty.Hard,
                mlpAiDifficulty.Hard => mlpAiDifficulty.Hell,
                _ => mlpAiDifficulty.Easy
            };
        }

        /// <summary>
        /// Sets whether this is a 1-player, 2-player, training, or tutorial session. Also updates the session mode accordingly.
        /// </summary>
        /// <param name="participantMode">The participant mode to set.</param>
        public void SetParticipantMode(mlpParticipantMode participantMode)
        {
            ParticipantMode = participantMode;
            if (participantMode == mlpParticipantMode.Training)
            {
                SessionMode = mlpSessionMode.Training;
            }
            else if (participantMode == mlpParticipantMode.Tutorial)
            {
                SessionMode = mlpSessionMode.Tutorial;
            }
        }

        /// <summary>
        /// Stores the player's chosen character for quick match mode.
        /// </summary>
        /// <param name="characterId">The character ID to use for quick matches.</param>
        public void SetQuickSelection(int characterId)
        {
            SelectedQuickCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Stores the player's chosen character for tournament mode.
        /// </summary>
        /// <param name="characterId">The character ID to use for tournaments.</param>
        public void SetTournamentSelection(int characterId)
        {
            SelectedTournamentCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Stores the player's chosen character for training mode.
        /// </summary>
        /// <param name="characterId">The character ID to use for training.</param>
        public void SetTrainingSelection(int characterId)
        {
            SelectedTrainingCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Stores the chosen ball skin for quick match mode.
        /// </summary>
        /// <param name="selection">The ball skin selection.</param>
        public void SetQuickBallSelection(mlpBallSelection selection)
        {
            SelectedQuickBallSelection = selection;
        }

        /// <summary>
        /// Stores the chosen ball skin for tournament mode.
        /// </summary>
        /// <param name="selection">The ball skin selection.</param>
        public void SetTournamentBallSelection(mlpBallSelection selection)
        {
            SelectedTournamentBallSelection = selection;
        }

        /// <summary>
        /// Stores the chosen ball skin for training mode.
        /// </summary>
        /// <param name="selection">The ball skin selection.</param>
        public void SetTrainingBallSelection(mlpBallSelection selection)
        {
            SelectedTrainingBallSelection = selection;
        }

        /// <summary>
        /// Stores the chosen ball skin for 2-player versus mode.
        /// </summary>
        /// <param name="selection">The ball skin selection.</param>
        public void SetVersusBallSelection(mlpBallSelection selection)
        {
            SelectedVersusBallSelection = selection;
        }

        /// <summary>
        /// Resets any active adventure/tournament, then prepares a quick match with the current settings.
        /// </summary>
        public void StartQuickGame()
        {
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = mlpParticipantMode.OnePlayer;
            GameMode = mlpGameModeIds.QuickMatch;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
        }

        /// <summary>
        /// Starts a single-player match from the main menu. Resets adventure/tournament and uses random opponent.
        /// </summary>
        public void StartOnePlayer()
        {
            ParticipantMode = mlpParticipantMode.OnePlayer;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            GameMode = mlpGameModeIds.RandomQuick;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Starts a 2-player local versus match from the main menu.
        /// </summary>
        public void StartTwoPlayers()
        {
            ParticipantMode = mlpParticipantMode.TwoPlayers;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            GameMode = mlpGameModeIds.TwoPlayers;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartPlayers2Match(SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Starts a 2-player match with each player's character explicitly chosen.
        /// </summary>
        /// <param name="leftCharacterId">Character ID for the left-side player.</param>
        /// <param name="rightCharacterId">Character ID for the right-side player.</param>
        public void StartTwoPlayerVersus(int leftCharacterId, int rightCharacterId)
        {
            ParticipantMode = mlpParticipantMode.TwoPlayers;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            GameMode = mlpGameModeIds.TwoPlayers;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.StartSelectedTwoPlayerMatch(leftCharacterId, rightCharacterId, SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Starts a training session with the currently selected training character and ball skin.
        /// </summary>
        public void StartTraining()
        {
            ParticipantMode = mlpParticipantMode.Training;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.Training;
            GameMode = mlpGameModeIds.Training;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.StartTraining(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Starts a tutorial session with the currently selected training character and ball skin.
        /// </summary>
        public void StartTutorial()
        {
            ParticipantMode = mlpParticipantMode.Tutorial;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.Tutorial;
            GameMode = mlpGameModeIds.Tutorial;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.StartTutorial(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// Creates a new tournament with the selected character and difficulty, then prepares the first match.
        /// </summary>
        /// <returns>True if the tournament was created successfully.</returns>
        public bool BeginTournament()
        {
            ParticipantMode = mlpParticipantMode.OnePlayer;
            Adventure.Reset();
            SessionMode = mlpSessionMode.Tournament;
            GameMode = mlpGameModeIds.RandomQuick;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
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
        /// Advances an active tournament to its finals stage and prepares the next match if one is pending.
        /// </summary>
        /// <returns>True if there is a pending finals match to play.</returns>
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
        /// Records the current match result and advances the tournament bracket. Prepares the next match if available.
        /// </summary>
        /// <returns>True if the tournament is now complete.</returns>
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
        /// Cancels the current tournament and clears all tournament state.
        /// </summary>
        public void AbandonTournament()
        {
            Tournament.Reset();
            SessionMode = mlpSessionMode.None;
            MatchPrepared = false;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
        }

        public void BeginAdventure(int playerCharacterId)
        {
            ParticipantMode = mlpParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = mlpSessionMode.Adventure;
            GameMode = mlpGameModeIds.RandomQuick;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
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
            SessionMode = mlpSessionMode.None;
            MatchPrepared = false;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
        }
    }
}
