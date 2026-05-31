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
            public const string PauseButton = "pause_button";
            public const string MusicButtonOn = "music_button_on";
            public const string MusicButtonOff = "music_button_off";
            public const string HelpButton = "help_button";

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
                public const string BallClassicOriginal = null;
                public const string BallGhoulGreen = "Gameplay/ball_halloween_ghoul_green";
                public const string BallPumpkinEmber = "Gameplay/ball_halloween_pumpkin_ember";
                public const string BallMoonlitViolet = "Gameplay/ball_halloween_moonlit_violet";
                public const string BallJackOLantern = "Gameplay/ball_halloween_jack_o_lantern";
                public const string BallEvilEye = "Gameplay/ball_halloween_evil_eye";
                public const string BallCursed8Ball = "Gameplay/ball_halloween_cursed_8ball";
                public const string BallCandySwirl = "Gameplay/ball_halloween_candy_swirl";
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
                    rimrushBallTheme.GhoulGreen => GameplayImages.BallGhoulGreen,
                    rimrushBallTheme.PumpkinEmber => GameplayImages.BallPumpkinEmber,
                    rimrushBallTheme.MoonlitViolet => GameplayImages.BallMoonlitViolet,
                    rimrushBallTheme.JackOLantern => GameplayImages.BallJackOLantern,
                    rimrushBallTheme.EvilEye => GameplayImages.BallEvilEye,
                    rimrushBallTheme.Cursed8Ball => GameplayImages.BallCursed8Ball,
                    rimrushBallTheme.CandySwirl => GameplayImages.BallCandySwirl,
                    _ => GameplayImages.BallClassicOriginal
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
