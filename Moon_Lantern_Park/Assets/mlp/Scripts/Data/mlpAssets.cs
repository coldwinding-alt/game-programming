// 游戏资源路径管理
// 集中管理所有图片、音效、字体等资源的路径名。其他代码需要加载资源时，都来这里查找正确的路径，避免写错文件名。

namespace mlp
{
    /// <summary>
    /// 游戏资源路径管理器：集中管理所有图片、音效、字体等资源的路径名，避免写错文件名。
    /// </summary>
    public static class mlpAssets
    {
        /// <summary>纹理图集（大图合集）的资源名称。</summary>
        public static class Atlases
        {
            public const string Gameplay = "gameplay";
            public const string Interface = "interface";
            public const string SkillFx = "skillfx";
        }

        /// <summary>游戏图片资源的路径。</summary>
        public static class Images
        {
            private const string Root = "mlp/Images/";

            public const string GameLogo = "logo";
            public const string MenuBackgroundHalloweenSpotlight = "menu_background_halloween_spotlight";
            public const string MenuBackgroundMoonlitGym = "menu_background_moonlit_gym";
            public const string PauseButton = "pause_button";
            public const string MusicButtonOn = "music_button_on";
            public const string MusicButtonOff = "music_button_off";
            public const string HelpButton = "help_button";

            /// <summary>UI 界面图片路径。</summary>
            public static class Ui
            {
                public const string FramePanelLarge = "UI/frame_panel_large";
                public const string FrameMatchCardIdle = "UI/frame_match_card_idle";
                public const string FrameMatchCardActive = "UI/frame_match_card_active";
                public const string MenuButtonPlate = "UI/menu_button_plate";
                public const string EnergyButtonPlate = "UI/energy_button_plate";
                public const string EmblemOrb = "UI/emblem_orb";
                public const string PanelFillSoft = "UI/panel_fill_soft";
                public const string AdventureTreasureMapBg = "UI/adventure_treasure_map_bg";
                public const string AwardsShowcasePanel = "UI/awards_showcase_panel";
                public const string AwardsResultPlaque = "UI/awards_result_plaque";
                public const string AwardsPodiumBase = "UI/awards_podium_base";
            }

            /// <summary>剧情漫画图片路径。</summary>
            public static class Story
            {
                public const string AdventureComicPage01 = "Story/adventure_comic_page_01";
                public const string AdventureComicPage02 = "Story/adventure_comic_page_02";
                public const string AdventureComicPage03 = "Story/adventure_comic_page_03";
                public const string TournamentComicPage01 = "Story/tournament_comic_page_01";
                public const string TournamentComicPage02 = "Story/tournament_comic_page_02";
                public const string TournamentComicPage03 = "Story/tournament_comic_page_03";
            }

            /// <summary>技能特效图片路径。</summary>
            public static class SkillFxImages
            {
                public const string ReaperDashCore = "SkillFx/reaper_dash_core";
                public const string ReaperDashAccent = "SkillFx/reaper_dash_accent";
                public const string BadLuckCore = "SkillFx/bad_luck_core";
                public const string BadLuckAccent = "SkillFx/bad_luck_accent";
                public const string HarvestTimeCore = "SkillFx/harvest_time_core";
                public const string HarvestTimeAccent = "SkillFx/harvest_time_accent";
            }

            /// <summary>技能图标和充能遮罩图片路径。</summary>
            public static class SkillIcons
            {
                public const string Reaper = "SkillIcons/reaper_skill_icon";
                public const string ReaperMask = "SkillIcons/reaper_skill_charge_mask";
                public const string GhostClown = "SkillIcons/ghost_clown_skill_icon";
                public const string GhostClownMask = "SkillIcons/ghost_clown_skill_charge_mask";
                public const string SkullPirate = "SkillIcons/skull_pirate_skill_icon";
                public const string SkullPirateMask = "SkillIcons/skull_pirate_skill_charge_mask";
                public const string Vampire = "SkillIcons/vampire_skill_icon";
                public const string VampireMask = "SkillIcons/vampire_skill_charge_mask";
                public const string Candleman = "SkillIcons/candleman_skill_icon";
                public const string CandlemanMask = "SkillIcons/candleman_skill_charge_mask";
                public const string Scarecrow = "SkillIcons/scarecrow_skill_icon";
                public const string ScarecrowMask = "SkillIcons/scarecrow_skill_charge_mask";
                public const string Witch = "SkillIcons/witch_skill_icon";
                public const string WitchMask = "SkillIcons/witch_skill_charge_mask";
                public const string BlackCat = "SkillIcons/black_cat_skill_icon";
                public const string BlackCatMask = "SkillIcons/black_cat_skill_charge_mask";
            }

            /// <summary>
            /// 根据图片资源键名构建完整的加载路径。如果键名为空则返回 null。
            /// </summary>
            /// <param name="imageKey">简短的图片键名（例如 "logo" 或 "UI/panel_fill_soft"）。</param>
            /// <returns>完整的 Resources 路径，如果键名为空则返回 null。</returns>
            public static string ResourcePath(string imageKey)
            {
                return string.IsNullOrEmpty(imageKey) ? null : $"{Root}{imageKey}";
            }

            /// <summary>比赛中使用的图片路径（球场、篮筐、篮球、角色动画图集等）。</summary>
            public static class GameplayImages
            {
                public const string ArenaBackdrop = "Gameplay/arena_halloween_backdrop";
                public const string BasketGraphic = "Gameplay/basket_halloween_rim";
                public const string BasketFrontEar = "Gameplay/basket_halloween_front_ear";
                public const string PlayerShadowPrimary = "Gameplay/player_shadow_primary";
                public const string PlayerShadowPrimaryRed = "Gameplay/player_shadow_primary_red";
                public const string PlayerShadowSecondary = "Gameplay/player_shadow_secondary";
                public const string PlayerShadowBall = "Gameplay/player_shadow_ball";
                public const string BallGhoulGreen = "Gameplay/ball_halloween_ghoul_green";
                public const string BallPumpkinEmber = "Gameplay/ball_halloween_pumpkin_ember";
                public const string BallMoonlitViolet = "Gameplay/ball_halloween_moonlit_violet";
                public const string BallJackOLantern = "Gameplay/ball_halloween_jack_o_lantern";
                public const string BallEvilEye = "Gameplay/ball_halloween_evil_eye";
                public const string BallCursed8Ball = "Gameplay/ball_halloween_cursed_8ball";
                public const string BallCandySwirl = "Gameplay/ball_halloween_candy_swirl";
                public const string FallbackAvatar = "Gameplay/player_fallback_avatar";
            }

            /// <summary>
            /// 根据篮球皮肤主题返回对应的图片路径。经典款篮球返回 null。
            /// </summary>
            /// <param name="theme">要查找的篮球皮肤主题。</param>
            /// <returns>对应篮球的 GameplayImages 键名，ClassicOriginal 返回 null。</returns>
            public static string BallTheme(mlpBallTheme theme)
            {
                return theme switch
                {
                    mlpBallTheme.ClassicOriginal => null,
                    mlpBallTheme.GhoulGreen => GameplayImages.BallGhoulGreen,
                    mlpBallTheme.PumpkinEmber => GameplayImages.BallPumpkinEmber,
                    mlpBallTheme.MoonlitViolet => GameplayImages.BallMoonlitViolet,
                    mlpBallTheme.JackOLantern => GameplayImages.BallJackOLantern,
                    mlpBallTheme.EvilEye => GameplayImages.BallEvilEye,
                    mlpBallTheme.Cursed8Ball => GameplayImages.BallCursed8Ball,
                    mlpBallTheme.CandySwirl => GameplayImages.BallCandySwirl,
                    _ => null
                };
            }
        }

        /// <summary>HUD 界面元素（计分板、弹窗等）的资源路径。</summary>
        public static class Hud
        {
            private const string Root = "mlp/Hud/";

            public const string Scoreboard = "scoreboard_halloween";
            public const string Popup = "popup_halloween";

            /// <summary>
            /// 根据 HUD 元素键名构建完整的加载路径。如果键名为空则返回 null。
            /// </summary>
            /// <param name="hudKey">简短的 HUD 键名（例如 "scoreboard_halloween"）。</param>
            /// <returns>完整的 Resources 路径，如果键名为空则返回 null。</returns>
            public static string ResourcePath(string hudKey)
            {
                return string.IsNullOrEmpty(hudKey) ? null : $"{Root}{hudKey}";
            }
        }

        /// <summary>角色头像图片的资源路径。</summary>
        public static class Portraits
        {
            private const string Root = "mlp/Portraits/";

            public const string UiAtlas = "portraits_ui";

            /// <summary>
            /// 根据头像资源键名构建完整的加载路径。如果键名为空则返回 null。
            /// </summary>
            /// <param name="portraitKey">简短的头像键名（例如 "portraits_ui"）。</param>
            /// <returns>完整的 Resources 路径，如果键名为空则返回 null。</returns>
            public static string ResourcePath(string portraitKey)
            {
                return string.IsNullOrEmpty(portraitKey) ? null : $"{Root}{portraitKey}";
            }
        }

        /// <summary>音效和背景音乐的资源名称。</summary>
        public static class Sounds
        {
            public const string MenuMusic = "bgm";
            public const string MWhistle = "whistle";
            public const string MBuzzer = "buzzer";
            public const string MCountdown = "countdown";

            public const string PTeleport = "teleport";
            public const string PSwoosh = "swoosh";
            public const string PEnergy = "energy";
            public const string PStunned = "stunned";
            public const string PMegaStart = "mega_dunk";
            public const string PShield = "shield";
            public const string PDash = "dash";
            public const string PSuperDash = "super_dash";

            public const string BSteel = "clash";
            public const string BRing = "rim_hit";
            public const string BBounce = "ball_bounce";
            public const string BNet = "net";
            public const string BBrick = "brick";
            public const string BBasket = "basket";

            public const string Button = "button";
        }
    }
}
