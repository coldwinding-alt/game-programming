// 单人模式叙事文案管理
// 集中管理月灯公园单人模式的所有命名和剧情文案，包括冒险模式和锦标赛模式的标题、副标题、剧情面板和结局文本。

namespace mlp
{
    public enum mlpSinglePlayerNarrativeMode
    {
        Adventure,
        Tournament
    }

    public sealed class mlpStoryPanelDefinition
    {
        public readonly string Caption;
        public readonly string ArtDirection;
        public readonly string ImageKey;
        public readonly string LoreTitle;
        public readonly string LoreBody;

        /// <summary>
        /// 创建一个仅有标题和美术指导的故事面板，不包含图片和背景故事。
        /// </summary>
        /// <param name="caption">面板标题文本。</param>
        /// <param name="artDirection">面板的美术指导说明。</param>
        public mlpStoryPanelDefinition(string caption, string artDirection)
            : this(caption, artDirection, null, null, null)
        {
        }

        /// <summary>
        /// 创建一个带有标题、美术指导和图片资源的故事面板，不包含背景故事。
        /// </summary>
        /// <param name="caption">面板标题文本。</param>
        /// <param name="artDirection">面板的美术指导说明。</param>
        /// <param name="imageKey">面板图片的资源键名。</param>
        public mlpStoryPanelDefinition(string caption, string artDirection, string imageKey)
            : this(caption, artDirection, imageKey, null, null)
        {
        }

        /// <summary>
        /// 创建一个包含标题、美术指导、图片资源和可选背景故事的完整故事面板。
        /// </summary>
        /// <param name="caption">面板标题文本。</param>
        /// <param name="artDirection">面板的美术指导说明。</param>
        /// <param name="imageKey">面板图片的资源键名。</param>
        /// <param name="loreTitle">背景故事标题，传 null 表示不显示背景故事。</param>
        /// <param name="loreBodyLines">背景故事正文，多行文本会用换行符拼接。</param>
        public mlpStoryPanelDefinition(
            string caption,
            string artDirection,
            string imageKey,
            string loreTitle,
            params string[] loreBodyLines)
        {
            Caption = caption;
            ArtDirection = artDirection;
            ImageKey = imageKey;
            LoreTitle = loreTitle ?? string.Empty;
            LoreBody = loreBodyLines == null || loreBodyLines.Length == 0
                ? string.Empty
                : string.Join("\n", loreBodyLines);
        }

        /// <summary>
        /// 判断该面板是否包含可显示的背景故事（标题或正文）。
        /// </summary>
        public bool HasLore
        {
            get { return !string.IsNullOrEmpty(LoreTitle) || !string.IsNullOrEmpty(LoreBody); }
        }
    }

    public sealed class mlpSinglePlayerModeDefinition
    {
        public readonly mlpSinglePlayerNarrativeMode Mode;
        public readonly string ModeName;
        public readonly string MenuTitle;
        public readonly string Subtitle;
        public readonly string Objective;
        public readonly string Tone;
        public readonly string GameplayWrapper;
        public readonly string WorldRole;
        public readonly mlpStoryPanelDefinition[] OpeningComic;

        /// <summary>
        /// 创建单人模式定义，包含所有叙事和展示数据。
        /// </summary>
        /// <param name="mode">叙事模式类型。</param>
        /// <param name="modeName">内部模式名称。</param>
        /// <param name="menuTitle">模式选择菜单上显示的标题。</param>
        /// <param name="subtitle">菜单标题下方显示的副标题。</param>
        /// <param name="objective">模式目标的描述。</param>
        /// <param name="tone">模式呈现风格的基调指南。</param>
        /// <param name="gameplayWrapper">玩法结构的描述。</param>
        /// <param name="worldRole">该模式在游戏世界叙事中的角色定位。</param>
        /// <param name="openingComic">模式开始时展示的剧情面板。</param>
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

    public static class mlpSinglePlayerNarrative
    {
        public const string ParkName = "MOON LANTERN PARK";
        public const string PumpkinHeartLantern = "PUMPKIN HEART LANTERN";
        public const string LanternSigil = "LANTERN SIGIL";
        public const string LanternSigils = "LANTERN SIGILS";
        public const string Warden = "WARDEN";
        public const string Wardens = "WARDENS";
        public const string MidnightLockdownProtocol = "MIDNIGHT LOCKDOWN PROTOCOL";
        public const string LanternChampion = "LANTERN CHAMPION";

        public const string AdventureMenuTitle = "ESCAPE MOON LANTERN";
        public const string AdventureSubtitle = "Collect every Lantern Sigil before dawn.";
        public const string TournamentMenuTitle = "MOON LANTERN CUP";
        public const string TournamentSubtitle = "Win the season and become the Lantern Champion.";
        public const string TournamentResultSubtitle = "MOON LANTERN RESULT";
        public const string TournamentSeasonCompleteTitle = "SEASON COMPLETE";
        public const string TournamentFormatLine = "8 PLAYERS / 2 DIVISIONS";
        public const string AdventurePreviewStatus = "PARK MAP PLAYABLE";
        public const string TournamentPreviewStatus = "FULL SEASON PLAYABLE";
        public const string TournamentSeasonBanner = "PUBLIC CHAMPIONSHIP SEASON";
        public const string TournamentSeasonHook = "A restored park turns its hidden ritual into an annual Halloween cup.";
        public const string ComicReplayButton = "READ COMIC";
        public const string AdventureLinkToCup = "The public Cup later turns this lockdown ritual into a season.";
        public const string CupLinkToAdventure = "The season quietly honors the night the park gates locked.";

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
        /// 根据叙事模式类型返回对应的模式定义。
        /// </summary>
        /// <param name="mode">要查找的叙事模式。</param>
        /// <returns>冒险模式或锦标赛模式的定义。</returns>
        public static mlpSinglePlayerModeDefinition GetMode(mlpSinglePlayerNarrativeMode mode)
        {
            return mode == mlpSinglePlayerNarrativeMode.Adventure ? Adventure : Tournament;
        }

        /// <summary>
        /// 获取当前锦标赛阶段的显示标题。
        /// </summary>
        /// <param name="tournament">要读取阶段信息的锦标赛数据。</param>
        /// <returns>显示标题，如 "DIVISIONS"、"FINAL FOUR" 或 "GRAND FINAL"。</returns>
        public static string GetTournamentStageTitle(mlpTournamentData tournament)
        {
            if (tournament == null)
            {
                return TournamentSeasonBanner;
            }

            if (tournament.Completed)
            {
                return "AWARDS PODIUM";
            }

            switch (tournament.CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
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
        /// 获取当前锦标赛阶段的叙事描述文本。
        /// </summary>
        /// <param name="tournament">要读取阶段信息的锦标赛数据。</param>
        /// <returns>与当前阶段匹配的风味文本描述。</returns>
        public static string GetTournamentStageDescription(mlpTournamentData tournament)
        {
            if (tournament == null)
            {
                return TournamentSeasonHook;
            }

            if (tournament.Completed)
            {
                return "The Cup renews the Heart Lantern, just as the first escape once did.";
            }

            switch (tournament.CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
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
        /// 根据锦标赛最终排名返回结局叙事文本。
        /// </summary>
        /// <param name="placement">玩家的最终排名（1 = 冠军）。</param>
        /// <returns>与排名匹配的结局描述。</returns>
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
