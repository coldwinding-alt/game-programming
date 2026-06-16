// Single-player mode narrative copy management
// Centrally manage all naming and plot copy for Moonlight Park's single-player mode, including titles, subtitles, plot panels, and ending text for Adventure and Tournament modes.


namespace mlp
{
    /// <summary>
    /// Single player mode type: adventure mode or tournament mode, used to distinguish the plot copy of different modes.

    /// </summary>
    public enum mlpSinglePlayerNarrativeMode
    {
        Adventure,
        Tournament
    }

    /// <summary>
    /// Story panel definition: Describes the content of a story comic page—title, art style, images, backstory title, and text.
    /// </summary>
    public sealed class mlpStoryPanelDefinition
    {
        public readonly string Caption;       // Panel title, the text title displayed on the comic page

        public readonly string ArtDirection;  // Art director, describing how this page of comics should be drawn (style, composition, etc.), and prompt words for the artist or AI to draw the drawings

        public readonly string ImageKey;      // Picture resource key name, used to find corresponding comic pictures from the resource system

        public readonly string LoreTitle;     // Backstory title, the title of the Lore text optionally displayed below the panel

        public readonly string LoreBody;      // Background story text, Lore text content optionally displayed below the panel (supports multiple lines)


        /// <summary>
        /// Create a story board with just the title and art direction, no images and no backstory.
        /// </summary>
        /// <param name="caption">Panel title text. </param>
        /// <param name="artDirection">The art direction description of the panel. </param>
        public mlpStoryPanelDefinition(string caption, string artDirection)
            : this(caption, artDirection, null, null, null)
        {
        }

        /// <summary>
        /// Create a story panel with a title, art direction, and image assets, but no backstory.
        /// </summary>
        /// <param name="caption">Panel title text. </param>
        /// <param name="artDirection">The art direction description of the panel. </param>
        /// <param name="imageKey">The resource key name of the panel image. </param>
        public mlpStoryPanelDefinition(string caption, string artDirection, string imageKey)
            : this(caption, artDirection, imageKey, null, null)
        {
        }

        /// <summary>
        /// Create a complete storyboard that includes a title, art direction, image assets, and optional backstory.
        /// </summary>
        /// <param name="caption">Panel title text. </param>
        /// <param name="artDirection">The art direction description of the panel. </param>
        /// <param name="imageKey">The resource key name of the panel image. </param>
        /// <param name="loreTitle">Backstory title, passing null means not to display the background story. </param>
        /// <param name="loreBodyLines">Backstory text, multi-line text will be spliced ​​with line breaks. </param>
        public mlpStoryPanelDefinition(
            string caption,
            string artDirection,
            string imageKey,
            string loreTitle,
            params string[] loreBodyLines)
        {
            // 1. Save the basic information of the panel: title, art direction, image path

            Caption = caption;
            ArtDirection = artDirection;
            ImageKey = imageKey;
            // 2. If no background story title is passed in, use an empty string instead (to avoid null reference errors)
            LoreTitle = loreTitle ?? string.Empty;
            // 3. If there is background story text (multi-line text), use line breaks to concatenate it into a string; otherwise, set it to an empty string

            LoreBody = loreBodyLines == null || loreBodyLines.Length == 0
                ? string.Empty
                : string.Join("\n", loreBodyLines);
        }

        /// <summary>
        /// Determines whether the panel contains displayable backstory (title or text).

        /// </summary>
        public bool HasLore
        {
            get { return !string.IsNullOrEmpty(LoreTitle) || !string.IsNullOrEmpty(LoreBody); }
        }
    }

    /// <summary>
    /// Single-player mode story definition: A title, subtitle, all story panels, and ending text that describe a complete mode.
    /// </summary>
    public sealed class mlpSinglePlayerModeDefinition
    {
        public readonly mlpSinglePlayerNarrativeMode Mode;       // Narrative mode type (Adventure or Tournament), example: Adventure

        public readonly string ModeName;                         // Internal mode name, used for identification in program logic, for example: "ADVENTURE MODE"

        public readonly string MenuTitle;                        // The mode title displayed on the menu interface, for example: "ESCAPE MOON LANTERN"

        public readonly string Subtitle;                         // A subtitle below the menu title that briefly describes the mode goal, for example: "Collect every Lantern Sigil before dawn."

        public readonly string Objective;                        // The complete goal description of the mode, for example: "Win 1v1 duels, reclaim every Lantern Sigil, and reopen the park gates."

        public readonly string Tone;                             // The tone of the mode's narrative style, for example: "Tense, adventurous, mysterious, but never grim."
        public readonly string GameplayWrapper;                  // Description of gameplay structure, for example: "A park map links one Warden duel to the next."

        public readonly string WorldRole;                        // The role of this mode in the game world view, for example: "The night Moon Lantern Park locked its gates."

        public readonly mlpStoryPanelDefinition[] OpeningComic;  // The array of opening comic panels played when the mode starts. The adventure mode has 3 pages: Park opening → Heart light goes out → First duel

        /// <summary>
        /// Create a single-player mode definition, including all narrative and presentation data.
        /// </summary>
        /// <param name="mode">Narrative mode type. </param>
        /// <param name="modeName">Internal mode name. </param>
        /// <param name="menuTitle">The title displayed on the mode selection menu. </param>
        /// <param name="subtitle">The subtitle displayed below the menu title. </param>
        /// <param name="objective">Description of the pattern objective. </param>
        /// <param name="tone">Tone guide for pattern presentation style. </param>
        /// <param name="gameplayWrapper">Description of gameplay structure. </param>
        /// <param name="worldRole">The role of this mode in the game world narrative. </param>
        /// <param name="openingComic">The story panel displayed at the beginning of the mode. </param>
        public mlpSinglePlayerModeDefinition(
            mlpSinglePlayerNarrativeMode mode,
            string modeName,
            string menuTitle,
            string subtitle,
            string objective,
            string tone,
            string gameplayWrapper,
            string worldRole,
            mlpStoryPanelDefinition[] openingComic)
        {
            Mode = mode;
            ModeName = modeName;
            MenuTitle = menuTitle;
            Subtitle = subtitle;
            Objective = objective;
            Tone = tone;
            GameplayWrapper = gameplayWrapper;
            WorldRole = worldRole;
            OpeningComic = openingComic ?? new mlpStoryPanelDefinition[0];
        }
    }

    /// <summary>
    /// Single-player mode story text manager: stores all story text for adventure mode and tournament mode for UI display.

    /// </summary>
    public static class mlpSinglePlayerNarrative
    {
        // ── World view terms (appearing in comic Lore, plot text, HUD, etc.) ──
        public const string ParkName = "MOON LANTERN PARK";                // The name of the park appears in the opening comics and various plot descriptions

        public const string PumpkinHeartLantern = "PUMPKIN HEART LANTERN"; // Pumpkin heart lantern, the core prop of the park, appears in the comic Lore and the ending text

        public const string LanternSigil = "LANTERN SIGIL";                // Lamp Sigil (odd number), a collectible obtained after defeating the Warden
        public const string LanternSigils = "LANTERN SIGILS";              // Lamp mark (plural), used for menu subtitles and other scenes that require plural forms

        public const string Warden = "WARDEN";                             // Guardian (singular), an opponent character in Adventure Mode

        public const string Wardens = "WARDENS";                           // Guardians (plural), returning as star players in tournaments

        public const string MidnightLockdownProtocol = "MIDNIGHT LOCKDOWN PROTOCOL"; // Midnight Lockout Protocol, the name of the park's automatic lockout mechanism

        public const string LanternChampion = "LANTERN CHAMPION";          // Lamp Champion, the championship title, appears on the HUD and awards screen


        // ── Mode selection menu UI ──

        public const string AdventureMenuTitle = "ESCAPE MOON LANTERN";    // Adventure mode menu title, displayed on the mode selection page and opening comic title bar

        public const string AdventureSubtitle = "Collect every Lantern Sigil before dawn."; // Adventure mode subtitle, displayed below the menu title

        public const string TournamentMenuTitle = "MOON LANTERN CUP";      // Tournament menu title, displayed on the mode selection page and each tournament interface

        public const string TournamentSubtitle = "Win the season and become the Lantern Champion."; // Tournament subtitle, displayed below the menu title

        public const string TournamentResultSubtitle = "MOON LANTERN RESULT"; // The subtitle of the tournament results page, which appears on the settlement screen of each game.

        public const string TournamentSeasonCompleteTitle = "SEASON COMPLETE"; // Season completion title, displayed at the top of the awards interface
        public const string TournamentFormatLine = "8 PLAYERS / 2 DIVISIONS"; // Tournament format description, displayed on the tournament settings page

        public const string AdventurePreviewStatus = "PARK MAP PLAYABLE";  // Adventure mode entrance status label, displayed on the mode selection page to indicate playability

        public const string TournamentPreviewStatus = "FULL SEASON PLAYABLE"; // Tournament entrance status label, displayed on the mode selection page to indicate playability

        public const string TournamentSeasonBanner = "PUBLIC CHAMPIONSHIP SEASON"; // Tournament season banner title, displayed at the top of the tournament homepage

        public const string TournamentSeasonHook = "A restored park turns its hidden ritual into an annual Halloween cup."; // Season profile description, displayed on the tournament page

        public const string ComicReplayButton = "READ COMIC";              // Comic replay button text, displayed on adventure and tournament menu pages for players to review the comic
        public const string AdventureLinkToCup = "The public Cup later turns this lockdown ritual into a season."; // Adventure→Tournament related copywriting, hinting at the story connection between the two modes

        public const string CupLinkToAdventure = "The season quietly honors the night the park gates locked."; // Tournament→Adventure related copywriting, hinting at the story connection between the two modes


        //Two schema definition examples
        public static readonly mlpSinglePlayerModeDefinition Adventure =
            new mlpSinglePlayerModeDefinition(
                mlpSinglePlayerNarrativeMode.Adventure,
                "ADVENTURE MODE",
                AdventureMenuTitle,
                AdventureSubtitle,
                "Win 1v1 duels, reclaim every Lantern Sigil, and reopen the park gates.",
                "Tense, adventurous, mysterious, but never grim.",
                "A park map links one Warden duel to the next.",
                "The night Moon Lantern Park locked its gates.",
                new[]
                {
                    new mlpStoryPanelDefinition(
                        "Moon Lantern Park opens on Halloween night.",
                        "Fullscreen comic page: neon gates, pumpkin lights, and a court flaring to life.",
                        mlpAssets.Images.Story.AdventureComicPage01,
                        "THE PARK AWAKENS",
                        "Halloween wakes Moon Lantern",
                        "Park in one sweep of neon.",
                        "Crowd noise, court lights, and",
                        "pumpkin fire all feed the Heart",
                        "Lantern above the dome."),
                    new mlpStoryPanelDefinition(
                        "The Heart Lantern fails and the park locks itself shut.",
                        "Fullscreen comic page: the dome flickers, gates chain shut, and Sigils scatter.",
                        mlpAssets.Images.Story.AdventureComicPage02,
                        "MIDNIGHT LOCKDOWN",
                        "Then the Heart Lantern stutters.",
                        "Chains drop across the gates,",
                        "the protocol wakes, and every",
                        "Lantern Sigil is thrown into a",
                        "different Warden district."),
                    new mlpStoryPanelDefinition(
                        "Win the duel, take the Sigil, and keep moving.",
                        "Fullscreen comic page: the first Warden duel begins and the escape route lights up.",
                        mlpAssets.Images.Story.AdventureComicPage03,
                        "THE FIRST RULE",
                        "The park leaves only one way",
                        "forward: beat each Warden in a",
                        "1v1 duel, reclaim the Sigil,",
                        "and stay ahead of dawn before",
                        "the route goes dark for good.")
                });

        public static readonly mlpSinglePlayerModeDefinition Tournament =
            new mlpSinglePlayerModeDefinition(
                mlpSinglePlayerNarrativeMode.Tournament,
                "TOURNAMENT MODE",
                TournamentMenuTitle,
                TournamentSubtitle,
                "Survive the divisions, reach the finals, and claim the Moon Lantern Cup.",
                "Loud, competitive, ceremonial, and full of Halloween showmanship.",
                "Divisions lead into the final four, then the grand final.",
                "One year later, the park turns its secret ritual into a public championship.",
                new[]
                {
                    new mlpStoryPanelDefinition(
                        "One year later, Moon Lantern Park reopens.",
                        "Fullscreen comic page: crowds return to a brighter Halloween basketball park.",
                        mlpAssets.Images.Story.TournamentComicPage01,
                        "THE LIGHTS RETURN",
                        "A year later, the locked park",
                        "reopens brighter than ever.",
                        "What used to be a hidden ritual",
                        "is now promoted as the city's",
                        "biggest Halloween night event."),
                    new mlpStoryPanelDefinition(
                        "The Wardens return as star players.",
                        "Fullscreen comic page: trophies, abstract brackets, and Warden athletes under spotlights.",
                        mlpAssets.Images.Story.TournamentComicPage02,
                        "WARDENS ON STAGE",
                        "The old Wardens step back in",
                        "under spotlights as division",
                        "stars. Every public match still",
                        "quietly keeps the restored Heart",
                        "Lantern burning overhead."),
                    new mlpStoryPanelDefinition(
                        "Your season begins under the main dome.",
                        "Fullscreen comic page: the trophy, Heart Lantern, and first match ignite the dome.",
                        mlpAssets.Images.Story.TournamentComicPage03,
                        "CHASE THE CUP",
                        "Now you enter the dome as the",
                        "next challenger. Win the season,",
                        "lift the Moon Lantern Cup, and",
                        "earn the right to guard the",
                        "park in front of the whole city.")
                });

        /// <summary>
        /// Returns the corresponding mode definition based on the narrative mode type.
        /// </summary>
        /// <param name="mode">The narrative mode to look for. </param>
        /// <returns>Definition of adventure mode or tournament mode. </returns>
        public static mlpSinglePlayerModeDefinition GetMode(mlpSinglePlayerNarrativeMode mode)
        {
            return mode == mlpSinglePlayerNarrativeMode.Adventure ? Adventure : Tournament;
        }

        /// <summary>
        /// Get the display title of the current tournament stage.
        /// </summary>
        /// <param name="tournament">Tournament data to read stage information from. </param>
        /// <returns>Display title, such as "DIVISIONS", "FINAL FOUR" or "GRAND FINAL". </returns>
        public static string GetTournamentStageTitle(mlpTournamentData tournament)
        {
            // 1. If the tournament data is empty, return the default season banner title

            if (tournament == null)
            {
                return TournamentSeasonBanner;
            }

            // 2. If the championship has ended, show the podium title

            if (tournament.Completed)
            {
                return "AWARDS PODIUM";
            }

            // 3. Return the corresponding title based on the current stage of the tournament
            switch (tournament.CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
                    // Regular season stage: "Divisional Tournament" or "Final Four" will be displayed depending on whether the game is completed or not.
                    return tournament.RegularSeasonCompleted ? "FINAL FOUR" : "DIVISIONS";
                case mlpTournamentStage.SemiFinal:
                    return "FINAL FOUR";
                case mlpTournamentStage.ThirdPlace:
                    return "3RD PLACE";
                case mlpTournamentStage.Final:
                    return "GRAND FINAL";
                default:
                    return TournamentSeasonBanner;
            }
        }

        /// <summary>
        /// Gets the narrative description text for the current tournament stage.
        /// </summary>
        /// <param name="tournament">Tournament data to read stage information from. </param>
        /// <returns>The flavor text description matching the current stage. </returns>
        public static string GetTournamentStageDescription(mlpTournamentData tournament)
        {
            // 1. If the tournament data is empty, return to the default season introduction

            if (tournament == null)
            {
                return TournamentSeasonHook;
            }

            // 2. If the tournament has ended, return the ending description

            if (tournament.Completed)
            {
                return "The Cup renews the Heart Lantern, just as the first escape once did.";
            }

            // 3. Return the corresponding flavor text description according to the current stage
            switch (tournament.CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
                    // Regular season stage: Return different descriptions based on progress
                    return tournament.RegularSeasonCompleted
                        ? "The top four step into the dome lights once reserved for Wardens."
                        : "Win division rounds and keep the Heart Lantern bright.";
                case mlpTournamentStage.SemiFinal:
                    return "The former Wardens enter like star players under the restored dome.";
                case mlpTournamentStage.ThirdPlace:
                    return "A final placement duel decides who leaves a name in the park.";
                case mlpTournamentStage.Final:
                    return "The winner publicly guards the same Heart Lantern rescued on lockdown night.";
                default:
                    return TournamentSeasonHook;
            }
        }

        /// <summary>
        /// Returns the ending narrative text based on the final ranking of the tournament.
        /// </summary>
        /// <param name="placement">The player's final ranking (1 = champion). </param>
        /// <returns>An ending description that matches the ranking. </returns>
        public static string GetTournamentPlacementEnding(int placement)
        {
            switch (placement)
            {
                case 1:
                    return "You become the new public guardian of the Pumpkin Heart Lantern.";
                case 2:
                    return "The main stage recognizes you, even without the champion crown.";
                case 3:
                    return "Your name stays on the park board for next year's challengers.";
                default:
                    return "The Cup ends with a clear invitation to return next season.";
            }
        }
    }
}
