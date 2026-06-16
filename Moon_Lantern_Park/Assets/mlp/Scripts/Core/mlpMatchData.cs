// Match configuration data and item list
// Define the configuration information required for a game: roles of both sides, difficulty, and game mode. Also includes the mlpInventory class, which saves the player's progress, selections, and unlock status.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// AI difficulty level: The whole game only retains four levels: Easy, Normal, Hard, and Hell.
    /// These enumeration values ​​only represent the difficulty level selected by the player, and no longer represent incremental progress such as tournament rounds, adventure level numbers, etc.
    /// The specific AI skill index that will be converted is determined by mlpMatchData.GetOpponentSkillForDifficulty.
    /// </summary>
    public enum mlpAiDifficulty
    {
        /// <summary>Simple: for new players and practice, with the lowest AI skill value. </summary>
        Easy,

        /// <summary>Normal: The default difficulty, suitable for standard experience. </summary>
        Normal,

        /// <summary>Difficulty: The AI ​​is more proactive and stable, but it will not continue to get stronger from stage to stage. </summary>
        Hard,

        /// <summary>Hell: The highest difficulty, Hell’s exclusive enhancements are still retained, but the basic skill values ​​are fixed. </summary>
        Hell
    }

    /// <summary>
    /// Participation modes: single, duo, training, tutorial. Used to distinguish how many people are playing and for what purpose.
    /// </summary>
    public enum mlpParticipantMode
    {
        OnePlayer,
        TwoPlayers,
        Training,
        Tutorial
    }

    /// <summary>
    /// Session Mode: The type of game currently being played (None, Quick Match, Adventure, Tournament, Training, Tutorial).

    /// </summary>
    public enum mlpSessionMode
    {
        None,
        QuickMatch,
        Adventure,
        Tournament,
        Training,
        Tutorial
    }

    /// <summary>
    /// Basketball skin themes: Classic, ghost green, pumpkin ember, moonlight purple and other basketballs with different appearances.
    /// </summary>
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

    /// <summary>
    /// Basketball Skin Selection: Contains the "Random" option and all specific skins for the selection interface in the menu.
    /// </summary>
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

    /// <summary>
    /// Basketball skin directory: manages the switching, parsing and label display of all basketball skins. Convert the options on the selection interface into actual skins.
    /// </summary>
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
        /// Switch the ball selection forward or backward in the ordered list, automatically looping back to the beginning when the end is reached.
        /// </summary>
        /// <param name="current">Currently selected ball cover. </param>
        /// <param name="direction">Positive number means switching backward, negative number means switching forward. </param>
        /// <returns>The next ball cover option in the specified direction. </returns>
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
        /// Convert ball skin options to specific themes. If "Random" is selected, a non-random topic is selected at random.
        /// </summary>
        /// <param name="selection">The ball skin options to parse. </param>
        /// <returns>The specific ball skin theme used for rendering. </returns>
        public static mlpBallTheme ResolveTheme(mlpBallSelection selection)
        {
            return selection == mlpBallSelection.Random
                ? ConcreteThemes[UnityEngine.Random.Range(0, ConcreteThemes.Length)]
                : ToTheme(selection);
        }

        /// <summary>
        /// Returns the theme displayed in the UI preview. When "Random" is selected, the classic theme is always displayed as a placeholder.
        /// </summary>
        /// <param name="selection">Surface options to preview. </param>
        /// <returns>The theme displayed in the selection interface. </returns>
        public static mlpBallTheme PreviewTheme(mlpBallSelection selection)
        {
            return selection == mlpBallSelection.Random
                ? mlpBallTheme.ClassicOriginal
                : ToTheme(selection);
        }

        /// <summary>
        /// Directly map non-random ball cover options to corresponding ball cover themes.
        /// </summary>
        /// <param name="selection">Ball skin option (cannot be Random). </param>
        /// <returns>Matching ball skin theme. </returns>
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
        /// Returns the short label text for the cover option (e.g. "GHOUL", "EMBER", "RANDOM").
        /// </summary>
        /// <param name="selection">The ball skin option to get the label. </param>
        /// <returns>A short uppercase string displayed in the UI. </returns>
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

    /// <summary>
    /// Game configuration data: stores all settings for a game - characters of both sides, difficulty, brain control method, skill level, score. Different modes (Quick, Training, Championship, etc.) are configured in different ways.
    /// </summary>
    public sealed class mlpMatchData
    {
        public bool Restarted;                          // Whether to start again

        public int FirstCharacterId;                    // The character id controlled by the player
        public int MatchMode;                           // Competition mode

        public mlpBallTheme BallTheme;                  // Ball theme

        public int[] CharacterIds = new int[2];         // Character IDs of both parties [0]=Player 1, [1]=Player 2
        public string[][] Pb = { new string[0], new string[0] };  // Control type of both parties, P (Player) - human player control B (Bot) - AI/computer control T (Tutorial) - tutorial mode

        public int[][] Skills = { new int[0], new int[0] };       // Difficulty level for both sides

        public int[] MatchScore = { 0, 0 };             // Score of both sides

        /// <summary>
        /// Create game data, initialize character ID and random ball theme by default.
        /// </summary>
        /// <param name="local">Pass true for local games (character index 0 is used by default), and false for network connections. </param>
        public mlpMatchData(bool local)
        {
            FirstCharacterId = mlpPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
            RollBallTheme();
        }

        /// <summary>
        /// Resets both teams' character IDs to default values.
        /// </summary>
        public void ResetData()
        {
            CharacterIds = new[] { mlpPlayersData.SanitizeCharacterId(0), mlpPlayersData.SanitizeCharacterId(1, 0) };
        }

        /// <summary>
        /// Reset match mode, player brain strings, skill levels and scores. Character IDs and ball themes will not be changed.
        /// </summary>
        public void ResetPartly()
        {
            MatchMode = 0;
            Pb = new[] { new string[0], new string[0] };
            Skills = new[] { new int[0], new int[0] };
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// Resets all data to default: character ID, brain string, skill level, score, and match mode.

        /// </summary>
        public void ResetAll()
        {
            ResetData();
            ResetPartly();
        }

        /// <summary>
        /// Set the default match configuration: both team characters, default brain strings, and default skill levels.
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
        /// Resets both teams' scores to zero.

        /// </summary>
        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

///The XCLF:Start* methods all follow the same pattern: ResetAll → Set Character → Set Brain Control → Set Skill.



        /// <summary>
        /// Configure a quick match: randomly select opponents, set brain strings for human vs. machine play, and parse the ball theme.
        /// </summary>
        /// <param name="playerCharacterId">The character ID selected by the player. </param>
        /// <param name="difficulty">AI difficulty level. </param>
        /// <param name="ballSelection">The ball cover used (Random means randomly selected). </param>
        public void StartQuickMatch(int playerCharacterId, mlpAiDifficulty difficulty, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            // 1. Reset all game data and analyze the ball skin theme

            ResetAll();
            ResolveBallSelection(ballSelection);
            // 2. Verify the player character and randomly select a different opponent

            var playerId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
            var excluded = new List<int> { playerId };
            var opponentId = mlpPlayersData.GetRandomCharacterId(excluded);
            // 3. Obtain the opponent’s AI skill level based on difficulty
            var opponentSkill = GetQuickMatchOpponentSkill(difficulty);

            // 4. Configure the roles, brain control methods and skills of both parties
            CharacterIds = new[] { playerId, opponentId };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        /// <summary>
        /// Configure a local two-player match using two different characters and player brain strings.
        /// </summary>
        /// <param name="ballSelection">The ball cover used. </param>
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
        /// Configure a training match with only player characters and no opponents.
        /// </summary>
        /// <param name="characterId">The character ID selected by the player. </param>
        /// <param name="ballSelection">The ball cover used. </param>
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
        /// Configure a tutorial match, using the specified opponent character and tutorial-specific brain strings.
        /// </summary>
        /// <param name="characterId">The character ID selected by the player. </param>
        /// <param name="ballSelection">The ball cover used. </param>
        public void StartTutorial(int characterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
            var opponentCharacterId = mlpPlayersData.SanitizeCharacterId(7, resolvedCharacterId);

            CharacterIds = new[] { resolvedCharacterId, opponentCharacterId };
            Pb = new[] { new[] { "P0" }, new[] { "T0" } };
            Skills = new[] { new[] { 0 }, new[] { mlpAISkillsData.NormalSkillIndex } };
        }

        /// <summary>
        /// Start a random match. Currently it is delegated directly to the StartQuickMatch method.
        /// </summary>
        /// <param name="playerCharacterId">The character ID selected by the player. </param>
        /// <param name="difficulty">AI difficulty level. </param>
        /// <param name="ballSelection">The ball cover used. </param>
        public void StartRandomMatch(int playerCharacterId, mlpAiDifficulty difficulty, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            StartQuickMatch(playerCharacterId, difficulty, ballSelection);
        }

        /// <summary>
        /// Configure a local two-player match using the currently stored character ID.

        /// </summary>
        /// <param name="ballSelection">The ball cover used. </param>
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
        /// Configure the next match in the tournament, using the tournament's current opponent and four fixed difficulty levels.
        /// </summary>
        /// <param name="tournament">Data for the currently ongoing tournament. </param>
        /// <param name="ballSelection">The ball cover used. </param>
        public void StartTournamentMatch(mlpTournamentData tournament, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            // 1. Reset all game data

            ResetAll();
            // 2. Return directly when the tournament is invalid, completed or there are no games to be played.
            if (tournament == null || !tournament.Active || tournament.Completed || !tournament.HasPendingPlayerMatch)
            {
                return;
            }

            // 3. Analyze the ball skin and set the character IDs of both parties

            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(tournament.PlayerCharacterId),
                mlpPlayersData.SanitizeCharacterId(tournament.CurrentOpponentCharacterId)
            };

            // 4. Calculate opponent skills based on tournament difficulty

            var opponentSkill = GetTournamentOpponentSkill(tournament);

            // 5. Configure brain control mode and skill level
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartAdventureMatch(mlpAdventureData adventure)
        {
            StartAdventureMatch(adventure, mlpAiDifficulty.Normal);
        }

        /// <summary>
        /// Configure the next game in Adventure Mode.
        /// </summary>
        /// <param name="adventure">Currently ongoing adventure data. </param>
        /// <param name="difficulty">Fixed four levels of AI difficulty selected by the player. </param>
        public void StartAdventureMatch(mlpAdventureData adventure, mlpAiDifficulty difficulty)
        {
            // 1. Reset all game data

            ResetAll();
            // 2. Return directly when the adventure is invalid, completed or there are no games to be played.

            if (adventure == null || !adventure.Active || adventure.Completed || !adventure.HasPendingPlayerMatch)
            {
                return;
            }

            // 3. Get the current level definition and parse the ball skin specified by the level.

            var level = adventure.CurrentLevel;
            MatchMode = 0;
            ResolveBallSelection(level.BallSelection);
            // 4. Set character IDs for players and guardians
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(adventure.PlayerCharacterId),
                mlpPlayersData.SanitizeCharacterId(level.WardenCharacterId)
            };
            // 5. Configure brain control method and opponent skill level
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetAdventureOpponentSkill(level, difficulty) } };
        }

        /// <summary>
        /// Configure a two-player match with assigned roles for each player.
        /// </summary>
        /// <param name="leftCharacterId">The character ID of the left player. </param>
        /// <param name="rightCharacterId">The character ID of the player on the right. </param>
        /// <param name="ballSelection">The ball cover used. </param>
        public void StartSelectedTwoPlayerMatch(int leftCharacterId, int rightCharacterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            // 1. Reset data and analyze the ball skin

            ResetAll();
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            // 2. Verify the role IDs of both parties (make sure there are no duplicates)
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(leftCharacterId),
                mlpPlayersData.SanitizeCharacterId(rightCharacterId, leftCharacterId)
            };
            // 3. Both parties are human players (P1 and P2) and have no AI skills.
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// The scores are compared to determine the winner.
        /// </summary>
        /// <returns>Returns -1 for a win on the left, 1 for a win on the right, and 0 for a draw. </returns>
        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
        }

        /// <summary>
        /// Mapping the four levels of difficulty selected by the player into AI skill indexes.
        /// </summary>
        /// <param name="difficulty">The chosen AI difficulty. </param>
        /// <returns>Four-level fixed skill index: 0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell. </returns>
        private static int GetOpponentSkillForDifficulty(mlpAiDifficulty difficulty)
        {
            // This is the only entrance to the AI ​​skill index for all single-player modes.

            // The return value will only be four fixed gears: 0, 1, 2, and 3:

            // 0 = Easy: Basic actions intact, but reactions, offense, and defense looser.
            // 1 = Normal: Default experience, suitable for standard match intensity.

            // 2 = Hard: More aggressive and stable, but does not continue to increase from level to level or stage to stage.

            // 3 = Hell: The highest base strength among the four levels; additional enhancements exclusive to Hell are still handled by difficulty adjustment.

            // Do not add separate round/level offsets to Quick Play, Random Play, Adventure, or Tournaments.

            // Otherwise, it will become a hidden multi-level difficulty again, which will conflict with the current fixed four-level design.

            return mlpAISkillsData.GetSkillIndex(difficulty);
        }

        /// <summary>
        /// Quick races also use a unified four-level difficulty mapping.
        /// </summary>
        private static int GetQuickMatchOpponentSkill(mlpAiDifficulty difficulty)
        {
            return GetOpponentSkillForDifficulty(difficulty);
        }

        private static int GetAdventureOpponentSkill(mlpAdventureLevelDefinition level, mlpAiDifficulty difficulty)
        {
            // 1. Adventure levels retain special rules and plots, but AI skills are uniformly determined by difficulty

            // 2. Level.OpponentSkill will no longer be read and will no longer increase with level progress.

            return GetOpponentSkillForDifficulty(difficulty);
        }

        /// <summary>
        /// AI skill levels are calculated based on four fixed difficulty levels selected by the tournament.
        /// </summary>
        /// <param name="tournament">Data for the currently ongoing tournament. </param>
        /// <returns>The skill index corresponding to this difficulty is fixed (limited by range). </returns>
        private static int GetTournamentOpponentSkill(mlpTournamentData tournament)
        {
            // 1. When the tournament data is empty, the default skill 0 is returned.

            if (tournament == null)
            {
                return 0;
            }

            // 2. The tournament retains the format process (regular season, semi-finals, finals, etc.), but AI skills are uniformly determined by difficulty
            // 3. The stage only determines the matchup and ranking, and does not affect the AI skill value.

            // 4. Keep the same difficulty selected by the player from the first round to the finals

            return GetOpponentSkillForDifficulty(tournament.Difficulty);
        }

        /// <summary>
        /// Randomly select a new ball theme for the current game.
        /// </summary>
        public void RollBallTheme()
        {
            BallTheme = mlpBallCatalog.ResolveTheme(mlpBallSelection.Random);
        }

        /// <summary>
        /// Convert cover options to specific cover themes and save.
        /// </summary>
        /// <param name="ballSelection">Selected ball cover (can be Random). </param>
        private void ResolveBallSelection(mlpBallSelection ballSelection)
        {
            BallTheme = mlpBallCatalog.ResolveTheme(ballSelection);
        }
    }

    // Global inventory (single case): Saves all player choices and progress - current game mode, selected characters and basketball skins, difficulty, adventure/tournament progress status. It is the central storage of the entire game state.
    public sealed class mlpInventory
    {
        // A private static reference to a singleton instance that stores the unique global instance.

        private static mlpInventory instance;

        // Expose static access points: Create an instance through lazy loading using the ?? operator during the first call, and subsequent calls directly return the existing instance to ensure global uniqueness.

        public static mlpInventory Instance => instance ?? (instance = new mlpInventory());

        // Game mode ID: 1=random quick match, 2=quick match, 3=training, 4=two player battle, 5=tutorial.

        public int GameMode;
        // Detailed data of the current game, including participating characters, scores, timing and other information.

        public mlpMatchData MatchData;
        // Progress and level data of adventure mode, manage level unlocking and victory and defeat records.

        public mlpAdventureData Adventure;
        // Tournament brackets and schedule data, managing knockout stages and opponents.

        public mlpTournamentData Tournament;
        // Whether the game is started for the first time is used to control the first startup boot process.

        public bool FirstRun = true;
        // Whether the tutorial is run for the first time is used to control the boot prompts within the tutorial.

        public bool FirstRun2 = true;
        // Whether the competition data has been configured. If it is true, the scene can be loaded directly to start the competition.

        public bool MatchPrepared;
        // AI difficulty level: Easy=easy, Normal=normal, Hard=difficult, Hell=Hell.

        public mlpAiDifficulty Difficulty;
        // Participation mode: OnePlayer=single player, TwoPlayers=two players, Training=training, Tutorial=tutorial.

        public mlpParticipantMode ParticipantMode;
        // Current session type: None, QuickMatch, Adventure, Tournament, Training, Tutorial.

        public mlpSessionMode SessionMode;
        // Next operations to be performed after the tutorial ends: None=None, ReplayTutorial=Replay the tutorial, StartTraining=Enter training, StartQuickMatch=Start quick matching.

        public mlpTutorialNextAction PendingTutorialNextAction;
        // The character ID selected by the player in quick match mode.

        public int SelectedQuickCharacterId;
        // The character ID selected by the player in tournament mode.

        public int SelectedTournamentCharacterId;
        // The character ID selected by the player in training mode.

        public int SelectedTrainingCharacterId;
        // The ball skin theme selected in the quick match mode: ClassicOriginal=Classic Original, GhoulGreen=Ghost Green, PumpkinEmber=Pumpkin Ember, MoonlitViolet=Moonlight Violet, JackOLantern=Pumpkin O'Lantern, EvilEye=Evil Eye, Cursed8Ball=Curse No. 8 Ball, CandySwirl=Candy Swirl.

        public mlpBallSelection SelectedQuickBallSelection;
        // The selected ball cover theme in tournament mode.

        public mlpBallSelection SelectedTournamentBallSelection;
        // The ball skin theme selected in training mode.

        public mlpBallSelection SelectedTrainingBallSelection;
        // The ball skin theme selected in two-player mode.

        public mlpBallSelection SelectedVersusBallSelection;

        // Initialize the global inventory and set the default game mode, difficulty, character selection and ball cover selection.

        private mlpInventory()
        {
            // 1. Set default game mode and participation mode

            GameMode = 1;
            ParticipantMode = mlpParticipantMode.OnePlayer;
            SessionMode = mlpSessionMode.None;
            // 2. Create game data, adventure data and tournament data instances

            MatchData = new mlpMatchData(true);
            Adventure = new mlpAdventureData();
            Tournament = new mlpTournamentData();
            // 3. Set default difficulty and tutorial status
            MatchData.MatchMode = 0;
            Difficulty = mlpAiDifficulty.Normal;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 4. The first character is selected by default in each mode.

            SelectedQuickCharacterId = MatchData.FirstCharacterId;
            SelectedTournamentCharacterId = MatchData.FirstCharacterId;
            SelectedTrainingCharacterId = MatchData.FirstCharacterId;
            // 5. Each mode uses classic ball skin by default

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

        // Cycle through difficulty levels in sequence: Easy -> Normal -> Hard -> Hell -> Easy.
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
        /// Set whether the current mode is single player, double player, training or tutorial, and update the corresponding session mode at the same time.
        /// </summary>
        /// <param name="participantMode">The participation mode to set. </param>
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
        /// Saves the character selected by the player in Quick Match mode.
        /// </summary>
        /// <param name="characterId">The character ID used for quick matches. </param>
        public void SetQuickSelection(int characterId)
        {
            SelectedQuickCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Saves the character selected by the player in tournament mode.
        /// </summary>
        /// <param name="characterId">The character ID used in the tournament. </param>
        public void SetTournamentSelection(int characterId)
        {
            SelectedTournamentCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Saves the character selected by the player in training mode.
        /// </summary>
        /// <param name="characterId">The character ID used in training mode. </param>
        public void SetTrainingSelection(int characterId)
        {
            SelectedTrainingCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// Saves the ball cover selected in Quick Play mode.
        /// </summary>
        /// <param name="selection">Ball skin options. </param>
        public void SetQuickBallSelection(mlpBallSelection selection)
        {
            SelectedQuickBallSelection = selection;
        }

        /// <summary>
        /// Saves the ball cover selected in tournament mode.

        /// </summary>
        /// <param name="selection">Ball skin options. </param>
        public void SetTournamentBallSelection(mlpBallSelection selection)
        {
            SelectedTournamentBallSelection = selection;
        }

        /// <summary>
        /// Save the ball cover selected in training mode.
        /// </summary>
        /// <param name="selection">Ball skin options. </param>
        public void SetTrainingBallSelection(mlpBallSelection selection)
        {
            SelectedTrainingBallSelection = selection;
        }

        /// <summary>
        /// Save the ball skin selected in two-player mode.

        /// </summary>
        /// <param name="selection">Ball skin options. </param>
        public void SetVersusBallSelection(mlpBallSelection selection)
        {
            SelectedVersusBallSelection = selection;
        }

        /// <summary>
        /// Reset the currently ongoing adventure/tournament and prepare for a quick match using the current settings.

        /// </summary>
        public void StartQuickGame()
        {
            // 1. Reset adventure and tournament status
            Adventure.Reset();
            Tournament.Reset();
            // 2. Set the session to quick match mode

            SessionMode = mlpSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = mlpParticipantMode.OnePlayer;
            GameMode = mlpGameModeIds.QuickMatch;
            // 3. Clear tutorial to-do status

            PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 4. Configure match data
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
        }

        /// <summary>
        /// Start a single player match from the main menu. Reset adventure/tournament state to use random opponents.
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
        /// Start a local two-player match from the main menu.

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
        /// Start a two-player match with clearly assigned roles for each player.
        /// </summary>
        /// <param name="leftCharacterId">The character ID of the left player. </param>
        /// <param name="rightCharacterId">The character ID of the player on the right. </param>
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
        /// Start a training match using the currently selected training character and ball cover.

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
        /// Start a tutorial using the currently selected training character and ball skin.

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
        /// Create a new tournament with your selected character and difficulty and prepare for your first match.
        /// </summary>
        /// <returns>Returns true if the tournament is successfully created. </returns>
        public bool BeginTournament()
        {
            // 1. Set up single player mode, reset the adventure, and switch to a tournament session

            ParticipantMode = mlpParticipantMode.OnePlayer;
            Adventure.Reset();
            SessionMode = mlpSessionMode.Tournament;
            GameMode = mlpGameModeIds.RandomQuick;
            // 2. Clear tutorial to-do status
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 3. Create a tournament and return if it fails.

            if (!Tournament.Create(SelectedTournamentCharacterId, Difficulty))
            {
                MatchPrepared = false;
                return false;
            }

            // 4. Configure the first game when the game is to be played

            MatchPrepared = false;
            if (Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            return true;
        }

        /// <summary>
        /// Advance ongoing tournaments to the finals and prepare for the next one if there are pending games.
        /// </summary>
        /// <returns>Returns true if there is a final match to be played. </returns>
        public bool BeginTournamentFinals()
        {
            // 1. Return failure when the tournament is not activated

            if (!IsTournamentActive)
            {
                return false;
            }

            // 2. Advance the tournament to the final stage
            Tournament.BeginFinals();
            // 3. Configure the game when the game is to be played
            MatchPrepared = false;
            if (Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            // 4. Return whether there are any finals to be played
            return Tournament.HasPendingPlayerMatch;
        }

        /// <summary>
        /// Record current match results and advance tournament brackets. Prepare for the next game if there is one.
        /// </summary>
        /// <returns>Returns true if the tournament has ended. </returns>
        public bool AdvanceTournament()
        {
            // 1. Return failure when the tournament is not activated

            if (!IsTournamentActive)
            {
                return false;
            }

            // 2. Submit current match results to the tournament

            Tournament.ApplyCurrentMatchResult(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            // 3. When it is not completed and there is a next game, configure the next game
            MatchPrepared = false;
            if (!Tournament.Completed && Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            // 4. Return whether the tournament is completed

            return Tournament.Completed;
        }

        /// <summary>
        /// Cancels the current tournament and clears all tournament status.

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
            // 1. Verify role ID

            var resolvedPlayerCharacterId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
            // 2. If the adventure is not active, completed or the characters do not match, restart the adventure

            if (!IsAdventureActive || Adventure.Completed || Adventure.PlayerCharacterId != resolvedPlayerCharacterId)
            {
                BeginAdventure(resolvedPlayerCharacterId);
            }

            // 3. Select a level and return if failed.

            if (!Adventure.SelectLevel(levelIndex))
            {
                return false;
            }

            // 4. Configure adventure match data and mark ready
            MatchData.StartAdventureMatch(Adventure, Difficulty);
            MatchPrepared = true;
            // 5. Verify match data integrity

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
