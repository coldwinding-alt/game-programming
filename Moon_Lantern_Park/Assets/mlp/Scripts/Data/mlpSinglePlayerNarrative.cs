// 单人模式叙事文案管理
// 集中管理月灯公园单人模式的所有命名和剧情文案，包括冒险模式和锦标赛模式的标题、副标题、剧情面板和结局文本。

namespace mlp
{
    /// <summary>
    /// 单人模式类型：冒险模式或锦标赛模式，用于区分不同模式的剧情文案。
    /// </summary>
    public enum mlpSinglePlayerNarrativeMode
    {
        Adventure,
        Tournament
    }

    /// <summary>
    /// 故事面板定义：描述一个剧情漫画页面的内容——标题、美术风格、图片、背景故事标题和正文。
    /// </summary>
    public sealed class mlpStoryPanelDefinition
    {
        public readonly string Caption;       // 面板标题，漫画页面上显示的文字标题
        public readonly string ArtDirection;  // 美术指导，描述这一页漫画应该怎么画（风格、构图等），给画师或 AI 生图用的提示词
        public readonly string ImageKey;      // 图片资源键名，用于从资源系统中查找对应的漫画图片
        public readonly string LoreTitle;     // 背景故事标题，面板下方可选显示的 Lore 文本的标题
        public readonly string LoreBody;      // 背景故事正文，面板下方可选显示的 Lore 文本内容（支持多行）

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
            // 1. 保存面板的基本信息：标题、美术指导、图片路径
            Caption = caption;
            ArtDirection = artDirection;
            ImageKey = imageKey;
            // 2. 如果没有传入背景故事标题，用空字符串代替（避免空引用错误）
            LoreTitle = loreTitle ?? string.Empty;
            // 3. 如果有背景故事正文（多行文本），用换行符拼接成一个字符串；否则设为空字符串
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

    /// <summary>
    /// 单人模式剧情定义：描述一个完整模式的标题、副标题、所有故事面板和结局文本。
    /// </summary>
    public sealed class mlpSinglePlayerModeDefinition
    {
        public readonly mlpSinglePlayerNarrativeMode Mode;       // 叙事模式类型（冒险或锦标赛），例：Adventure
        public readonly string ModeName;                         // 内部模式名称，用于程序逻辑中的标识，例："ADVENTURE MODE"
        public readonly string MenuTitle;                        // 菜单界面上显示的模式标题，例："ESCAPE MOON LANTERN"
        public readonly string Subtitle;                         // 菜单标题下方的副标题，简短描述模式目标，例："Collect every Lantern Sigil before dawn."
        public readonly string Objective;                        // 模式的完整目标描述，例："Win 1v1 duels, reclaim every Lantern Sigil, and reopen the park gates."
        public readonly string Tone;                             // 模式的叙事风格基调，例："Tense, adventurous, mysterious, but never grim."
        public readonly string GameplayWrapper;                  // 玩法结构描述，例："A park map links one Warden duel to the next."
        public readonly string WorldRole;                        // 该模式在游戏世界观中的角色定位，例："The night Moon Lantern Park locked its gates."
        public readonly mlpStoryPanelDefinition[] OpeningComic;  // 模式开始时播放的开场漫画面板数组，冒险模式有3页：公园开放→心灯熄灭→首次决斗

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

    /// <summary>
    /// 单人模式剧情文案管理器：存储冒险模式和锦标赛模式的所有剧情文本，供 UI 显示使用。
    /// </summary>
    public static class mlpSinglePlayerNarrative
    {
        // ── 世界观术语（出现在漫画 Lore、剧情文本、HUD 等多处） ──
        public const string ParkName = "MOON LANTERN PARK";                // 公园名称，出现在开场漫画和各处剧情描述中
        public const string PumpkinHeartLantern = "PUMPKIN HEART LANTERN"; // 南瓜心灯，公园核心道具，出现在漫画 Lore 和结局文本中
        public const string LanternSigil = "LANTERN SIGIL";                // 灯印记（单数），击败 Warden 后获得的收集物
        public const string LanternSigils = "LANTERN SIGILS";              // 灯印记（复数），用于菜单副标题等需要复数形式的场景
        public const string Warden = "WARDEN";                             // 守护者（单数），冒险模式中的对手角色
        public const string Wardens = "WARDENS";                           // 守护者（复数），锦标赛中作为明星选手回归
        public const string MidnightLockdownProtocol = "MIDNIGHT LOCKDOWN PROTOCOL"; // 午夜封锁协议，公园自动封锁的机制名称
        public const string LanternChampion = "LANTERN CHAMPION";          // 灯冠军，锦标赛冠军头衔，出现在 HUD 和颁奖界面

        // ── 模式选择菜单 UI ──
        public const string AdventureMenuTitle = "ESCAPE MOON LANTERN";    // 冒险模式菜单标题，显示在模式选择页面和开场漫画标题栏
        public const string AdventureSubtitle = "Collect every Lantern Sigil before dawn."; // 冒险模式副标题，显示在菜单标题下方
        public const string TournamentMenuTitle = "MOON LANTERN CUP";      // 锦标赛菜单标题，显示在模式选择页面和各锦标赛界面
        public const string TournamentSubtitle = "Win the season and become the Lantern Champion."; // 锦标赛副标题，显示在菜单标题下方
        public const string TournamentResultSubtitle = "MOON LANTERN RESULT"; // 锦标赛比赛结果页面副标题，出现在每场比赛结算界面
        public const string TournamentSeasonCompleteTitle = "SEASON COMPLETE"; // 赛季完成标题，显示在颁奖界面顶部
        public const string TournamentFormatLine = "8 PLAYERS / 2 DIVISIONS"; // 锦标赛赛制说明，显示在锦标赛设置页面
        public const string AdventurePreviewStatus = "PARK MAP PLAYABLE";  // 冒险模式入口状态标签，显示在模式选择页面表示可游玩
        public const string TournamentPreviewStatus = "FULL SEASON PLAYABLE"; // 锦标赛入口状态标签，显示在模式选择页面表示可游玩
        public const string TournamentSeasonBanner = "PUBLIC CHAMPIONSHIP SEASON"; // 锦标赛赛季横幅标题，显示在锦标赛主页顶部
        public const string TournamentSeasonHook = "A restored park turns its hidden ritual into an annual Halloween cup."; // 赛季简介描述，显示在锦标赛页面
        public const string ComicReplayButton = "READ COMIC";              // 漫画重播按钮文字，显示在冒险和锦标赛菜单页面供玩家回顾漫画
        public const string AdventureLinkToCup = "The public Cup later turns this lockdown ritual into a season."; // 冒险→锦标赛关联文案，暗示两个模式的故事联系
        public const string CupLinkToAdventure = "The season quietly honors the night the park gates locked."; // 锦标赛→冒险关联文案，暗示两个模式的故事联系

        //两个模式定义实例
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
            // 1. 如果锦标赛数据为空，返回默认的赛季横幅标题
            if (tournament == null)
            {
                return TournamentSeasonBanner;
            }

            // 2. 如果锦标赛已结束，显示颁奖台标题
            if (tournament.Completed)
            {
                return "AWARDS PODIUM";
            }

            // 3. 根据锦标赛当前阶段返回对应的标题
            switch (tournament.CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
                    // 常规赛阶段：根据是否打完显示"分区赛"或"四强赛"
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
            // 1. 如果锦标赛数据为空，返回默认的赛季简介
            if (tournament == null)
            {
                return TournamentSeasonHook;
            }

            // 2. 如果锦标赛已结束，返回结局描述
            if (tournament.Completed)
            {
                return "The Cup renews the Heart Lantern, just as the first escape once did.";
            }

            // 3. 根据当前阶段返回对应的风味文本描述
            switch (tournament.CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
                    // 常规赛阶段：根据进度返回不同描述
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
