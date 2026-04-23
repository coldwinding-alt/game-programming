namespace BasketballLegends2020
{
    public static class BLAssets
    {
        public static class Atlases
        {
            public const string Gameplay = "gameplay";
            public const string Interface = "interface";
            public const string SkillFx = "skillfx";
        }

        public static class Images
        {
            public const string GameLogo = "logo";

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

            public static string BallTheme(BLBallTheme theme)
            {
                return theme switch
                {
                    BLBallTheme.GhoulGreen => GameplayImages.BallGhoulGreen,
                    BLBallTheme.PumpkinEmber => GameplayImages.BallPumpkinEmber,
                    BLBallTheme.MoonlitViolet => GameplayImages.BallMoonlitViolet,
                    BLBallTheme.JackOLantern => GameplayImages.BallJackOLantern,
                    BLBallTheme.EvilEye => GameplayImages.BallEvilEye,
                    BLBallTheme.Cursed8Ball => GameplayImages.BallCursed8Ball,
                    BLBallTheme.CandySwirl => GameplayImages.BallCandySwirl,
                    _ => GameplayImages.BallClassicOriginal
                };
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
