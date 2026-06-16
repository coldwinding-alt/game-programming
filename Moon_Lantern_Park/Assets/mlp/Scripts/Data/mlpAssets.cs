// Game resource path management
// Centrally manage the path names of all images, sound effects, fonts and other resources. When other codes need to load resources, they come here to find the correct path to avoid writing the wrong file name.

namespace mlp
{
    /// <summary>
    /// Game resource path manager: Centrally manage the path names of all pictures, sound effects, fonts and other resources to avoid writing wrong file names.
    /// </summary>
    public static class mlpAssets
    {
        /// <summary>The resource name of the texture atlas (large image collection). </summary>
        public static class Atlases
        {
            public const string Gameplay = "gameplay";     // Picture album for game scenes (court, characters, basketball and other materials combined into one large picture)
            public const string Interface = "interface";   // Atlases for menus and UI interfaces (buttons, panels, etc.)
            public const string SkillFx = "skillfx";       // Atlas for skill special effects (materials such as light effects when skills are released)
        }

        /// <summary>The path to the game image resource. </summary>
        public static class Images
        {
            private const string Root = "mlp/Images/";  // The root directory path of the image resource in the Resources folder


            public const string GameLogo = "logo";                                          // Logo image of the game’s main interface

            public const string MenuBackgroundHalloweenSpotlight = "menu_background_halloween_spotlight";  // Main menu background image (Halloween spotlight style)
            public const string MenuBackgroundMoonlitGym = "menu_background_moonlit_gym";    // Main menu background image (Moonlight Stadium style)

            public const string PauseButton = "pause_button";                               // image of pause button

            public const string MusicButtonOn = "music_button_on";                          // Button picture of music on state (speaker has sound icon)
            public const string MusicButtonOff = "music_button_off";                        // Button picture of music off state (speaker mute icon)
            public const string HelpButton = "help_button";                                 // image of help button

            /// <summary>UI interface image path. </summary>
            public static class Ui
            {
                public const string FramePanelLarge = "UI/frame_panel_large";                   // Large-sized UI panel border (for large areas such as pop-ups and details pages)

                public const string FrameMatchCardIdle = "UI/frame_match_card_idle";            // Default state of match cards (appearance when unselected)

                public const string FrameMatchCardActive = "UI/frame_match_card_active";        // The activation status of the match card (highlighted appearance when selected)

                public const string MenuButtonPlate = "UI/menu_button_plate";                   // The base plate of the menu button (the decorative base image behind the button)

                public const string EnergyButtonPlate = "UI/energy_button_plate";               // The base of the energy button (UI base map related to skill energy)

                public const string EmblemOrb = "UI/emblem_orb";                               // Spherical decorative elements for badges/logos
                public const string PanelFillSoft = "UI/panel_fill_soft";                       // Soft fill panel (semi-transparent background mask, used for the bottom layer of pop-up windows)

                public const string AdventureTreasureMapBg = "UI/adventure_treasure_map_bg";    // Adventure mode treasure map background image

                public const string AwardsShowcasePanel = "UI/awards_showcase_panel";           // Reward display panel (displays the prizes obtained)
                public const string AwardsResultPlaque = "UI/awards_result_plaque";             // Nameplate/plaque decoration of competition results
                public const string AwardsPodiumBase = "UI/awards_podium_base";                 // The base of the podium (used to display the ranking at the end of the game)
            }

            /// <summary>Path comic image path. </summary>
            public static class Story
            {
                public const string AdventureComicPage01 = "Story/adventure_comic_page_01";    // Adventure Mode Story Comics Page 1

                public const string AdventureComicPage02 = "Story/adventure_comic_page_02";    // Adventure Mode Story Comics Page 2

                public const string AdventureComicPage03 = "Story/adventure_comic_page_03";    // Adventure Mode Story Comics Page 3

                public const string TournamentComicPage01 = "Story/tournament_comic_page_01";  // Tournament Mode Story Comics Page 1

                public const string TournamentComicPage02 = "Story/tournament_comic_page_02";  // Tournament Mode Story Comics Page 2

                public const string TournamentComicPage03 = "Story/tournament_comic_page_03";  // Tournament Mode Story Comics Page 3
            }

            /// <summary>Skill effects image path. </summary>
            public static class SkillFxImages
            {
                public const string ReaperDashCore = "SkillFx/reaper_dash_core";       // The core light effect of Death Sprint skill (main body trailing)

                public const string ReaperDashAccent = "SkillFx/reaper_dash_accent";   // Decorative light effects for the Death Sprint skill (edge embellishment)
                public const string BadLuckCore = "SkillFx/bad_luck_core";             // The core light effect of the doom skill (main effect)
                public const string BadLuckAccent = "SkillFx/bad_luck_accent";         // Decorative light effects for Doom skills (edge ​​embellishments)
            }

            /// <summary>Skill icon and charge mask image paths. </summary>
            public static class SkillIcons
            {
                public const string Reaper = "SkillIcons/reaper_skill_icon";               // Skill icons for the Reaper character

                public const string ReaperMask = "SkillIcons/reaper_skill_charge_mask";    // Reaper skill charging mask (black and white mask used to display skill cooling/charging progress)

                public const string GhostClown = "SkillIcons/ghost_clown_skill_icon";      // Ghost Clown character's skill icons

                public const string GhostClownMask = "SkillIcons/ghost_clown_skill_charge_mask";  // Ghost clown skill charge mask

                public const string SkullPirate = "SkillIcons/skull_pirate_skill_icon";    // Skill icons for the Skull Pirate character

                public const string SkullPirateMask = "SkillIcons/skull_pirate_skill_charge_mask"; // Skeleton Pirate Ability Charge Mask

                public const string Vampire = "SkillIcons/vampire_skill_icon";             // Vampire character skill icons

                public const string VampireMask = "SkillIcons/vampire_skill_charge_mask";  // Vampire Ability Charge Mask

                public const string Candleman = "SkillIcons/candleman_skill_icon";         // Skill icons for the Candleman character

                public const string CandlemanMask = "SkillIcons/candleman_skill_charge_mask";  // Candleman skill charge mask

                public const string Scarecrow = "SkillIcons/scarecrow_skill_icon";         // Scarecrow character skill icons

                public const string ScarecrowMask = "SkillIcons/scarecrow_skill_charge_mask";  // Scarecrow skill charge mask
                public const string Witch = "SkillIcons/witch_skill_icon";                 // Witch character skill icons

                public const string WitchMask = "SkillIcons/witch_skill_charge_mask";      // Charging Mask for Witch Skills

                public const string BlackCat = "SkillIcons/black_cat_skill_icon";          // Skill icons for Black Cat characters
                public const string BlackCatMask = "SkillIcons/black_cat_skill_charge_mask";  // Black cat skill charge mask
            }

            /// <summary>
            /// Build a complete loading path based on the image resource key name. Returns null if the key name is empty.
            /// </summary>
            /// <param name="imageKey">A short image key name (such as "logo" or "UI/panel_fill_soft"). </param>
            /// <returns>The complete Resources path, or null if the key name is empty. </returns>
            public static string ResourcePath(string imageKey)
            {
                // Splice out "mlp/Images/" + short key name to get the complete Resources path that Unity can load
                return string.IsNullOrEmpty(imageKey) ? null : $"{Root}{imageKey}";
            }

            /// <summary>The image path used in the game (court, hoop, basketball, character animation album, etc.). </summary>
            public static class GameplayImages
            {
                public const string ArenaBackdrop = "Gameplay/arena_halloween_backdrop";       // Halloween themed background image for the competition venue

                public const string BasketGraphic = "Gameplay/basket_halloween_rim";            // Halloween themed rim graphics for baskets (basket hoops)

                public const string BasketFrontEar = "Gameplay/basket_halloween_front_ear";     // Decorative parts on the front of the basket (backboard ear details, covering the front of the basket)
                public const string PlayerShadowPrimary = "Gameplay/player_shadow_primary";     // Shadow image of the home team player (default color)

                public const string PlayerShadowPrimaryRed = "Gameplay/player_shadow_primary_red"; // Shadow image of the home team player (red variant)

                public const string PlayerShadowSecondary = "Gameplay/player_shadow_secondary"; // Shadow pictures of visiting team players

                public const string PlayerShadowBall = "Gameplay/player_shadow_ball";           // Shadow picture of basketball shooting

                public const string BallGhoulGreen = "Gameplay/ball_halloween_ghoul_green";     // Basketball skin: Ghoul green

                public const string BallPumpkinEmber = "Gameplay/ball_halloween_pumpkin_ember"; // Basketball Skin: Pumpkin Ember

                public const string BallMoonlitViolet = "Gameplay/ball_halloween_moonlit_violet"; // Basketball skin: moonlight purple

                public const string BallJackOLantern = "Gameplay/ball_halloween_jack_o_lantern"; // Basketball Skin: Jack-O-Lantern

                public const string BallEvilEye = "Gameplay/ball_halloween_evil_eye";           // Basketball Skin: Evil Eye

                public const string BallCursed8Ball = "Gameplay/ball_halloween_cursed_8ball";   // Basketball Skin: Cursed 8-Ball

                public const string BallCandySwirl = "Gameplay/ball_halloween_candy_swirl";     // Basketball Skin: Candy Swirl

                public const string FallbackAvatar = "Gameplay/player_fallback_avatar";         // The player's default backup avatar (used when the character does not have an exclusive avatar)
            }

            /// <summary>
            /// Return the corresponding image path according to the basketball skin theme. Classic basketball returns null.
            /// </summary>
            /// <param name="theme">The basketball skin theme to find. </param>
            /// <returns>The GameplayImages key name corresponding to basketball, ClassicOriginal returns null. </returns>
            public static string BallTheme(mlpBallTheme theme)
            {
                // Returns the corresponding image path according to the basketball skin enumeration. The classic model (default ball) does not require special images, so null is returned.

                return theme switch
                {
                    mlpBallTheme.ClassicOriginal => null,                                  // Classic original basketball (using default rendering, no additional images required)

                    mlpBallTheme.GhoulGreen => GameplayImages.BallGhoulGreen,              // Ghoul green skin → corresponding image path
                    mlpBallTheme.PumpkinEmber => GameplayImages.BallPumpkinEmber,          // Pumpkin Ember Skin → Corresponding image path

                    mlpBallTheme.MoonlitViolet => GameplayImages.BallMoonlitViolet,        // Moonlight Purple Skin → Corresponding picture path

                    mlpBallTheme.JackOLantern => GameplayImages.BallJackOLantern,          // Jack-o-lantern skin → corresponding image path

                    mlpBallTheme.EvilEye => GameplayImages.BallEvilEye,                    // Evil eye skin → corresponding image path

                    mlpBallTheme.Cursed8Ball => GameplayImages.BallCursed8Ball,            // Cursed No. 8 Ball Skin → Corresponding image path

                    mlpBallTheme.CandySwirl => GameplayImages.BallCandySwirl,              // Candy Swirl Skin → Corresponding image path
                    _ => null                                                              // Unknown skin → return null and use default
                };
            }
        }

        /// <summary>The resource path of HUD interface elements (scoreboard, pop-up window, etc.). </summary>
        public static class Hud
        {
            private const string Root = "mlp/Hud/";    // The root directory path of the HUD resource in the Resources folder

            public const string Scoreboard = "scoreboard_halloween";  // Halloween themed scoreboard interface
            public const string Popup = "popup_halloween";            // Halloween themed pop-up interface

            /// <summary>
            /// Build the complete load path based on the HUD element key. Returns null if the key name is empty.
            /// </summary>
            /// <param name="hudKey">Short HUD key name (e.g. "scoreboard_halloween"). </param>
            /// <returns>The complete Resources path, or null if the key name is empty. </returns>
            public static string ResourcePath(string hudKey)
            {
                // Splice out "mlp/Hud/" + short key name to get the complete Resources path that Unity can load
                return string.IsNullOrEmpty(hudKey) ? null : $"{Root}{hudKey}";
            }
        }

        /// <summary>The resource path of the character avatar image. </summary>
        public static class Portraits
        {
            private const string Root = "mlp/Portraits/";  // The root directory path of the character avatar resource in the Resources folder

            public const string UiAtlas = "portraits_ui";  // A UI gallery of all character avatars combined (one large image contains all character avatars)

            /// <summary>
            /// Build a complete loading path based on the avatar resource key name. Returns null if the key name is empty.
            /// </summary>
            /// <param name="portraitKey">Short avatar key name (e.g. "portraits_ui"). </param>
            /// <returns>The complete Resources path, or null if the key name is empty. </returns>
            public static string ResourcePath(string portraitKey)
            {
                // Splice out "mlp/Portraits/" + short key name to get the complete Resources path that Unity can load
                return string.IsNullOrEmpty(portraitKey) ? null : $"{Root}{portraitKey}";
            }
        }

        /// <summary>The resource name of sound effects and background music. Naming prefix: M=Match, P=Player, B=Basketball Ball. </summary>
        public static class Sounds
        {
            public const string MenuMusic = "bgm";        // Main menu background music


            // M = Match (match related sound effects)

            public const string MWhistle = "whistle";     // Referee whistle (game start/pause)
            public const string MBuzzer = "buzzer";       // Buzzer sound (game ends/time is up)

            public const string MCountdown = "countdown"; // Countdown sound (3-2-1 countdown)


            // P = Player (player/character operation sound effect)

            public const string PTeleport = "teleport";   // Teleportation sound effects (character teleportation movement)

            public const string PSwoosh = "swoosh";       // Swing sound effect (fast movement/shooting shot)

            public const string PEnergy = "energy";       // Energy sound effects (energy gained/charge completed)
            public const string PStunned = "stunned";     // Being stunned sound effect (the character is controlled by a skill)

            public const string PMegaStart = "mega_dunk"; // Ultimate move activation sound effect (launching powerful slam dunk skill)

            public const string PShield = "shield";       // Shield sound effect (activates protective skills)

            public const string PDash = "dash";           // Sprint sound effect (character dashes forward in a short distance)

            public const string PSuperDash = "super_dash"; // Super sprint sound effect (enhanced version of sprint skill)


            // B = Ball (basketball physics interactive sound effect)

            public const string BSteel = "clash";         // Collision sound effects (physical confrontation between players)

            public const string BRing = "rim_hit";        // Basket impact sound effect (the ball hits the edge of the basket)

            public const string BBounce = "ball_bounce";  // Basketball bouncing sound effect (the ball bounces after it hits the ground)

            public const string BNet = "net";             // Net passing sound effect (the ball passes through the net)

            public const string BBrick = "brick";         // Blacksmithing sound effect (the ball hits the basket and pops out, but misses)

            public const string BBasket = "basket";       // Hit sound effect (the ball is successfully thrown into the basket)

            public const string Button = "button";        // Button click sound (UI button pressed)
        }
    }
}
