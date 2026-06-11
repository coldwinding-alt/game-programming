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
            public const string Gameplay = "gameplay";     // 比赛场景用的图集（球场、角色、篮球等素材合在一张大图里）
            public const string Interface = "interface";   // 菜单和 UI 界面用的图集（按钮、面板等素材）
            public const string SkillFx = "skillfx";       // 技能特效用的图集（技能释放时的光效等素材）
        }

        /// <summary>游戏图片资源的路径。</summary>
        public static class Images
        {
            private const string Root = "mlp/Images/";  // 图片资源在 Resources 文件夹下的根目录路径

            public const string GameLogo = "logo";                                          // 游戏主界面的 Logo 图片
            public const string MenuBackgroundHalloweenSpotlight = "menu_background_halloween_spotlight";  // 主菜单背景图（万圣节聚光灯风格）
            public const string MenuBackgroundMoonlitGym = "menu_background_moonlit_gym";    // 主菜单背景图（月光体育馆风格）
            public const string PauseButton = "pause_button";                               // 暂停按钮的图片
            public const string MusicButtonOn = "music_button_on";                          // 音乐开启状态的按钮图片（喇叭有声音图标）
            public const string MusicButtonOff = "music_button_off";                        // 音乐关闭状态的按钮图片（喇叭静音图标）
            public const string HelpButton = "help_button";                                 // 帮助按钮的图片

            /// <summary>UI 界面图片路径。</summary>
            public static class Ui
            {
                public const string FramePanelLarge = "UI/frame_panel_large";                   // 大尺寸的 UI 面板边框（用于弹窗、详情页等大区域）
                public const string FrameMatchCardIdle = "UI/frame_match_card_idle";            // 比赛卡片的默认状态（未选中时的外观）
                public const string FrameMatchCardActive = "UI/frame_match_card_active";        // 比赛卡片的激活状态（选中时高亮的外观）
                public const string MenuButtonPlate = "UI/menu_button_plate";                   // 菜单按钮的底板（按钮背后的装饰底图）
                public const string EnergyButtonPlate = "UI/energy_button_plate";               // 能量按钮的底板（技能能量相关的 UI 底图）
                public const string EmblemOrb = "UI/emblem_orb";                               // 徽章/标志的球形装饰元素
                public const string PanelFillSoft = "UI/panel_fill_soft";                       // 柔和填充面板（半透明背景遮罩，用于弹窗底层）
                public const string AdventureTreasureMapBg = "UI/adventure_treasure_map_bg";    // 冒险模式的宝藏地图背景图
                public const string AwardsShowcasePanel = "UI/awards_showcase_panel";           // 奖励展示面板（展示获得的奖品）
                public const string AwardsResultPlaque = "UI/awards_result_plaque";             // 比赛结果的铭牌/匾额装饰
                public const string AwardsPodiumBase = "UI/awards_podium_base";                 // 颁奖台的底座（比赛结束排名展示用）
            }

            /// <summary>剧情漫画图片路径。</summary>
            public static class Story
            {
                public const string AdventureComicPage01 = "Story/adventure_comic_page_01";    // 冒险模式剧情漫画第 1 页
                public const string AdventureComicPage02 = "Story/adventure_comic_page_02";    // 冒险模式剧情漫画第 2 页
                public const string AdventureComicPage03 = "Story/adventure_comic_page_03";    // 冒险模式剧情漫画第 3 页
                public const string TournamentComicPage01 = "Story/tournament_comic_page_01";  // 锦标赛模式剧情漫画第 1 页
                public const string TournamentComicPage02 = "Story/tournament_comic_page_02";  // 锦标赛模式剧情漫画第 2 页
                public const string TournamentComicPage03 = "Story/tournament_comic_page_03";  // 锦标赛模式剧情漫画第 3 页
            }

            /// <summary>技能特效图片路径。</summary>
            public static class SkillFxImages
            {
                public const string ReaperDashCore = "SkillFx/reaper_dash_core";       // 死神冲刺技能的核心光效（主体拖尾）
                public const string ReaperDashAccent = "SkillFx/reaper_dash_accent";   // 死神冲刺技能的装饰光效（边缘点缀）
                public const string BadLuckCore = "SkillFx/bad_luck_core";             // 厄运技能的核心光效（主体效果）
                public const string BadLuckAccent = "SkillFx/bad_luck_accent";         // 厄运技能的装饰光效（边缘点缀）
            }

            /// <summary>技能图标和充能遮罩图片路径。</summary>
            public static class SkillIcons
            {
                public const string Reaper = "SkillIcons/reaper_skill_icon";               // 死神（Reaper）角色的技能图标
                public const string ReaperMask = "SkillIcons/reaper_skill_charge_mask";    // 死神技能的充能遮罩（用于显示技能冷却/充能进度的黑白遮罩图）
                public const string GhostClown = "SkillIcons/ghost_clown_skill_icon";      // 幽灵小丑（Ghost Clown）角色的技能图标
                public const string GhostClownMask = "SkillIcons/ghost_clown_skill_charge_mask";  // 幽灵小丑技能的充能遮罩
                public const string SkullPirate = "SkillIcons/skull_pirate_skill_icon";    // 骷髅海盗（Skull Pirate）角色的技能图标
                public const string SkullPirateMask = "SkillIcons/skull_pirate_skill_charge_mask"; // 骷髅海盗技能的充能遮罩
                public const string Vampire = "SkillIcons/vampire_skill_icon";             // 吸血鬼（Vampire）角色的技能图标
                public const string VampireMask = "SkillIcons/vampire_skill_charge_mask";  // 吸血鬼技能的充能遮罩
                public const string Candleman = "SkillIcons/candleman_skill_icon";         // 蜡烛人（Candleman）角色的技能图标
                public const string CandlemanMask = "SkillIcons/candleman_skill_charge_mask";  // 蜡烛人技能的充能遮罩
                public const string Scarecrow = "SkillIcons/scarecrow_skill_icon";         // 稻草人（Scarecrow）角色的技能图标
                public const string ScarecrowMask = "SkillIcons/scarecrow_skill_charge_mask";  // 稻草人技能的充能遮罩
                public const string Witch = "SkillIcons/witch_skill_icon";                 // 女巫（Witch）角色的技能图标
                public const string WitchMask = "SkillIcons/witch_skill_charge_mask";      // 女巫技能的充能遮罩
                public const string BlackCat = "SkillIcons/black_cat_skill_icon";          // 黑猫（Black Cat）角色的技能图标
                public const string BlackCatMask = "SkillIcons/black_cat_skill_charge_mask";  // 黑猫技能的充能遮罩
            }

            /// <summary>
            /// 根据图片资源键名构建完整的加载路径。如果键名为空则返回 null。
            /// </summary>
            /// <param name="imageKey">简短的图片键名（例如 "logo" 或 "UI/panel_fill_soft"）。</param>
            /// <returns>完整的 Resources 路径，如果键名为空则返回 null。</returns>
            public static string ResourcePath(string imageKey)
            {
                // 拼接出 "mlp/Images/" + 简短键名，得到 Unity 可加载的完整 Resources 路径
                return string.IsNullOrEmpty(imageKey) ? null : $"{Root}{imageKey}";
            }

            /// <summary>比赛中使用的图片路径（球场、篮筐、篮球、角色动画图集等）。</summary>
            public static class GameplayImages
            {
                public const string ArenaBackdrop = "Gameplay/arena_halloween_backdrop";       // 比赛场地的万圣节主题背景图
                public const string BasketGraphic = "Gameplay/basket_halloween_rim";            // 篮筐的万圣节主题边缘图（篮筐铁环）
                public const string BasketFrontEar = "Gameplay/basket_halloween_front_ear";     // 篮筐前方的装饰部件（篮板耳朵细节，覆盖在篮筐前面）
                public const string PlayerShadowPrimary = "Gameplay/player_shadow_primary";     // 主队玩家的影子图片（默认颜色）
                public const string PlayerShadowPrimaryRed = "Gameplay/player_shadow_primary_red"; // 主队玩家的影子图片（红色变体）
                public const string PlayerShadowSecondary = "Gameplay/player_shadow_secondary"; // 客队玩家的影子图片
                public const string PlayerShadowBall = "Gameplay/player_shadow_ball";           // 篮球投篮时的影子图片
                public const string BallGhoulGreen = "Gameplay/ball_halloween_ghoul_green";     // 篮球皮肤：食尸鬼绿
                public const string BallPumpkinEmber = "Gameplay/ball_halloween_pumpkin_ember"; // 篮球皮肤：南瓜余烬
                public const string BallMoonlitViolet = "Gameplay/ball_halloween_moonlit_violet"; // 篮球皮肤：月光紫
                public const string BallJackOLantern = "Gameplay/ball_halloween_jack_o_lantern"; // 篮球皮肤：杰克南瓜灯
                public const string BallEvilEye = "Gameplay/ball_halloween_evil_eye";           // 篮球皮肤：邪恶之眼
                public const string BallCursed8Ball = "Gameplay/ball_halloween_cursed_8ball";   // 篮球皮肤：诅咒 8 号球
                public const string BallCandySwirl = "Gameplay/ball_halloween_candy_swirl";     // 篮球皮肤：糖果漩涡
                public const string FallbackAvatar = "Gameplay/player_fallback_avatar";         // 玩家默认备用头像（当角色没有专属头像时使用）
            }

            /// <summary>
            /// 根据篮球皮肤主题返回对应的图片路径。经典款篮球返回 null。
            /// </summary>
            /// <param name="theme">要查找的篮球皮肤主题。</param>
            /// <returns>对应篮球的 GameplayImages 键名，ClassicOriginal 返回 null。</returns>
            public static string BallTheme(mlpBallTheme theme)
            {
                // 根据篮球皮肤枚举返回对应的图片路径，经典款（默认球）不需要特殊图片所以返回 null
                return theme switch
                {
                    mlpBallTheme.ClassicOriginal => null,                                  // 经典原版篮球（使用默认渲染，无需额外图片）
                    mlpBallTheme.GhoulGreen => GameplayImages.BallGhoulGreen,              // 食尸鬼绿皮肤 → 对应图片路径
                    mlpBallTheme.PumpkinEmber => GameplayImages.BallPumpkinEmber,          // 南瓜余烬皮肤 → 对应图片路径
                    mlpBallTheme.MoonlitViolet => GameplayImages.BallMoonlitViolet,        // 月光紫皮肤 → 对应图片路径
                    mlpBallTheme.JackOLantern => GameplayImages.BallJackOLantern,          // 杰克南瓜灯皮肤 → 对应图片路径
                    mlpBallTheme.EvilEye => GameplayImages.BallEvilEye,                    // 邪恶之眼皮肤 → 对应图片路径
                    mlpBallTheme.Cursed8Ball => GameplayImages.BallCursed8Ball,            // 诅咒8号球皮肤 → 对应图片路径
                    mlpBallTheme.CandySwirl => GameplayImages.BallCandySwirl,              // 糖果漩涡皮肤 → 对应图片路径
                    _ => null                                                              // 未知皮肤 → 返回 null 走默认
                };
            }
        }

        /// <summary>HUD 界面元素（计分板、弹窗等）的资源路径。</summary>
        public static class Hud
        {
            private const string Root = "mlp/Hud/";    // HUD 资源在 Resources 文件夹下的根目录路径

            public const string Scoreboard = "scoreboard_halloween";  // 万圣节主题的计分板界面
            public const string Popup = "popup_halloween";            // 万圣节主题的弹窗界面

            /// <summary>
            /// 根据 HUD 元素键名构建完整的加载路径。如果键名为空则返回 null。
            /// </summary>
            /// <param name="hudKey">简短的 HUD 键名（例如 "scoreboard_halloween"）。</param>
            /// <returns>完整的 Resources 路径，如果键名为空则返回 null。</returns>
            public static string ResourcePath(string hudKey)
            {
                // 拼接出 "mlp/Hud/" + 简短键名，得到 Unity 可加载的完整 Resources 路径
                return string.IsNullOrEmpty(hudKey) ? null : $"{Root}{hudKey}";
            }
        }

        /// <summary>角色头像图片的资源路径。</summary>
        public static class Portraits
        {
            private const string Root = "mlp/Portraits/";  // 角色头像资源在 Resources 文件夹下的根目录路径

            public const string UiAtlas = "portraits_ui";  // 所有角色头像合在一起的 UI 图集（一张大图包含所有角色头像）

            /// <summary>
            /// 根据头像资源键名构建完整的加载路径。如果键名为空则返回 null。
            /// </summary>
            /// <param name="portraitKey">简短的头像键名（例如 "portraits_ui"）。</param>
            /// <returns>完整的 Resources 路径，如果键名为空则返回 null。</returns>
            public static string ResourcePath(string portraitKey)
            {
                // 拼接出 "mlp/Portraits/" + 简短键名，得到 Unity 可加载的完整 Resources 路径
                return string.IsNullOrEmpty(portraitKey) ? null : $"{Root}{portraitKey}";
            }
        }

        /// <summary>音效和背景音乐的资源名称。命名前缀：M=比赛 Match，P=玩家 Player，B=篮球 Ball。</summary>
        public static class Sounds
        {
            public const string MenuMusic = "bgm";        // 主菜单的背景音乐

            // M = Match（比赛相关音效）
            public const string MWhistle = "whistle";     // 裁判哨声（比赛开始/暂停）
            public const string MBuzzer = "buzzer";       // 蜂鸣器声（比赛结束/时间到）
            public const string MCountdown = "countdown"; // 倒计时提示音（3-2-1 倒数）

            // P = Player（玩家/角色操作音效）
            public const string PTeleport = "teleport";   // 瞬移音效（角色传送移动）
            public const string PSwoosh = "swoosh";       // 挥动音效（快速移动/投篮出手）
            public const string PEnergy = "energy";       // 能量音效（获取能量/充能完成）
            public const string PStunned = "stunned";     // 被眩晕音效（角色受到控制技能）
            public const string PMegaStart = "mega_dunk"; // 大招启动音效（发动强力灌篮技能）
            public const string PShield = "shield";       // 护盾音效（激活防护技能）
            public const string PDash = "dash";           // 冲刺音效（角色短距离突进）
            public const string PSuperDash = "super_dash"; // 超级冲刺音效（强化版冲刺技能）

            // B = Ball（篮球物理交互音效）
            public const string BSteel = "clash";         // 碰撞音效（球员之间身体对抗）
            public const string BRing = "rim_hit";        // 篮筐撞击音效（球打到篮筐边缘）
            public const string BBounce = "ball_bounce";  // 篮球弹跳音效（球落地弹起）
            public const string BNet = "net";             // 穿网音效（球穿过篮网）
            public const string BBrick = "brick";         // 打铁音效（球砸篮筐弹出，未命中）
            public const string BBasket = "basket";       // 命中音效（球成功投进篮筐）

            public const string Button = "button";        // 按钮点击音效（UI 按钮被按下）
        }
    }
}
