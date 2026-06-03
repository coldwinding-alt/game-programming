// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushAssets 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

namespace rimrush
{
    public static class rimrushAssets
    {
        public static class Atlases
        {
            public const string Gameplay = "gameplay";
            public const string Interface = "interface";
            public const string SkillFx = "skillfx";
        }

        public static class Images
        {
            private const string Root = "rimrush/Images/";

            public const string GameLogo = "logo";
            public const string MenuBackgroundHalloweenSpotlight = "menu_background_halloween_spotlight";
            public const string MenuBackgroundMoonlitGym = "menu_background_moonlit_gym";
            public const string PauseButton = "pause_button";
            public const string MusicButtonOn = "music_button_on";
            public const string MusicButtonOff = "music_button_off";
            public const string HelpButton = "help_button";

            public static class Ui
            {
                public const string FramePanelLarge = "UI/frame_panel_large";
                public const string FrameMatchCardIdle = "UI/frame_match_card_idle";
                public const string FrameMatchCardActive = "UI/frame_match_card_active";
                public const string MenuButtonPlate = "UI/menu_button_plate";
                public const string EnergyButtonPlate = "UI/energy_button_plate";
                public const string EmblemOrb = "UI/emblem_orb";
                public const string PanelFillSoft = "UI/panel_fill_soft";
                public const string AwardsShowcasePanel = "UI/awards_showcase_panel";
                public const string AwardsResultPlaque = "UI/awards_result_plaque";
                public const string AwardsPodiumBase = "UI/awards_podium_base";
            }

            public static class Story
            {
                public const string AdventureComicPage01 = "Story/adventure_comic_page_01";
                public const string AdventureComicPage02 = "Story/adventure_comic_page_02";
                public const string AdventureComicPage03 = "Story/adventure_comic_page_03";
                public const string TournamentComicPage01 = "Story/tournament_comic_page_01";
                public const string TournamentComicPage02 = "Story/tournament_comic_page_02";
                public const string TournamentComicPage03 = "Story/tournament_comic_page_03";
            }

            public static class SkillFxImages
            {
                public const string ReaperDashCore = "SkillFx/reaper_dash_core";
                public const string ReaperDashAccent = "SkillFx/reaper_dash_accent";
                public const string BadLuckCore = "SkillFx/bad_luck_core";
                public const string BadLuckAccent = "SkillFx/bad_luck_accent";
                public const string HarvestTimeCore = "SkillFx/harvest_time_core";
                public const string HarvestTimeAccent = "SkillFx/harvest_time_accent";
            }

            public static class SkillIcons
            {
                public const string ReaperAcolyte = "SkillIcons/reaper_acolyte_skill_icon";
                public const string ReaperAcolyteMask = "SkillIcons/reaper_acolyte_skill_charge_mask";
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
            /// Executes Resource Path for the Images workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="imageKey">Input value used by this step of the workflow.</param>
            /// <returns>Result produced for downstream logic in the current frame.</returns>
            public static string ResourcePath(string imageKey)
            {
                return string.IsNullOrEmpty(imageKey) ? null : $"{Root}{imageKey}";
            }

            public static class GameplayImages
            {
                public const string ArenaBackdrop = "Gameplay/arena_halloween_backdrop";
                public const string BasketGraphic = "Gameplay/basket_halloween_rim";
                public const string BasketFrontEar = "Gameplay/basket_halloween_front_ear";
                public const string PlayerShadowPrimary = "Gameplay/player_shadow_primary";
                public const string PlayerShadowPrimaryRed = "Gameplay/player_shadow_primary_red";
                public const string PlayerShadowSecondary = "Gameplay/player_shadow_secondary";
                public const string PlayerShadowBall = "Gameplay/player_shadow_ball";
                public const string BallClassicOriginal = "Gameplay/ball_classic_original";
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
            /// Executes Ball Theme for the GameplayImages workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="theme">Input value used by this step of the workflow.</param>
            /// <returns>Result produced for downstream logic in the current frame.</returns>
            public static string BallTheme(rimrushBallTheme theme)
            {
                return theme switch
                {
                    rimrushBallTheme.ClassicOriginal => null,
                    rimrushBallTheme.GhoulGreen => GameplayImages.BallGhoulGreen,
                    rimrushBallTheme.PumpkinEmber => GameplayImages.BallPumpkinEmber,
                    rimrushBallTheme.MoonlitViolet => GameplayImages.BallMoonlitViolet,
                    rimrushBallTheme.JackOLantern => GameplayImages.BallJackOLantern,
                    rimrushBallTheme.EvilEye => GameplayImages.BallEvilEye,
                    rimrushBallTheme.Cursed8Ball => GameplayImages.BallCursed8Ball,
                    rimrushBallTheme.CandySwirl => GameplayImages.BallCandySwirl,
                    _ => null
                };
            }
        }

        public static class Hud
        {
            private const string Root = "rimrush/Hud/";

            public const string Scoreboard = "scoreboard_halloween";
            public const string Popup = "popup_halloween";

            /// <summary>
            /// Executes Resource Path for the Hud workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="hudKey">Input value used by this step of the workflow.</param>
            /// <returns>Result produced for downstream logic in the current frame.</returns>
            public static string ResourcePath(string hudKey)
            {
                return string.IsNullOrEmpty(hudKey) ? null : $"{Root}{hudKey}";
            }
        }

        public static class Portraits
        {
            private const string Root = "rimrush/Portraits/";

            public const string UiAtlas = "portraits_ui";

            /// <summary>
            /// Executes Resource Path for the Portraits workflow.
            /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
            /// </summary>
            /// <param name="portraitKey">Input value used by this step of the workflow.</param>
            /// <returns>Result produced for downstream logic in the current frame.</returns>
            public static string ResourcePath(string portraitKey)
            {
                return string.IsNullOrEmpty(portraitKey) ? null : $"{Root}{portraitKey}";
            }
        }

        public static class Sounds
        {
            public const string MenuMusic = "24_TrackSnd";
            public const string MWhistle = "2_M_Whistle";
            public const string MBuzzer = "9_M_Buzzer";
            public const string MCountdown = "19_M_Countdown";

            public const string PTeleport = "4_P_Teleport";
            public const string PSwoosh = "5_P_Swoosh";
            public const string PEnergy = "6_P_Energy";
            public const string PStunned = "7_P_Stunned";
            public const string PMegaStart = "11_P_MegaStart";
            public const string PShield = "13_P_Shield";
            public const string PDash = "17_P_Dash";
            public const string PSuperDash = "18_P_SuperDash";

            public const string BSteel = "8_B_Steel";
            public const string BRing = "10_B_Ring";
            public const string BBounce = "16_B_Bounce";
            public const string BNet = "21_B_NET";
            public const string BBrick = "22_B_Brick";
            public const string BBasket = "23_B_Basket";

            public const string Button = "20_ButtonSnd";
        }
    }
}
