// 游戏启动器和菜单界面控制器
// 管理从主菜单到比赛开始的所有界面：选择 1 人或 2 人模式、选择角色和篮球皮肤、选择难度、进入冒险模式或锦标赛、显示故事漫画、锦标赛对阵图和颁奖画面。也负责创建摄像机和音频系统。

//  worldX = (x * 4/3 - 533) / 100
//  worldY = (320 - y * 4/3) / 100

using System.Text;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏启动器和菜单控制器：管理从主菜单到比赛的所有界面——选择模式、角色、篮球皮肤、难度，以及冒险/锦标赛的流程控制。也负责创建摄像机和音频系统。
    /// </summary>
    // ═══════════════════════════════════════════════════════════════════
    // 第 1 块：类定义、枚举、数据结构和常量
    // ═══════════════════════════════════════════════════════════════════
    public sealed class mlpGameBootstrap : MonoBehaviour
    {
        private static mlpGameBootstrap activeInstance;

        //整个脚本的"地图"，列出了所有可能出现的界面
        private enum mlpBootstrapScreen
        {
            PlayerCount,                 // 选择玩家人数（1人/2人）
            MatchType,                   // 选择比赛类型（快速/冒险/锦标赛）
            SinglePlayerCharacterSetup,  // 单人模式选择角色
            StoryIntro,                  // 故事漫画过场
            AdventurePreview,            // 冒险模式预告页
            AdventureMap,                // 冒险模式藏宝地图
            AdventureResult,             // 冒险关卡结果（过关/失败）
            SinglePlayerSetup,           // 单人赛前设置（选球、难度）
            TwoPlayerSetup,              // 双人赛前设置
            TrainingSetup,               // 训练模式设置
            TournamentSetup,             // 锦标赛设置
            TournamentBracket,           // 锦标赛对阵图
            TournamentComplete,          // 锦标赛结束
            TournamentAwards             // 锦标赛颁奖典礼
        }

        // 颁奖典礼中每个选手的排名信息
        private sealed class TournamentAwardsPlacement
        {
            public int Placement;            // 名次（1=冠军，2=亚军，3=季军）
            public int CharacterId;          // 角色 ID
            public bool IsPlayer;            // 是否是玩家操控的角色
            public string CupAnimation;      // 奖杯动画名称
            public Color AccentColor;        // 强调色（用于排名标识）
            public Color GlowColor;          // 发光效果颜色
        }

        // 颁奖典礼中单个动画元素的定义（用于入场动画）
        private sealed class TournamentAwardsAnimatedItem
        {
            public Transform Root;           // 动画目标对象的 Transform
            public Vector3 TargetLocalPosition; // 动画结束时的本地位置
            public Vector3 TargetLocalScale;    // 动画结束时的本地缩放
            public Vector3 StartLocalOffset;    // 动画开始时的偏移量（从远处滑入）
            public float Delay;             // 动画延迟播放时间（秒）
            public float Duration;          // 动画持续时间（秒）
            public float StartScale = 1f;   // 动画开始时的缩放
            public bool Fade = true;        // 是否有淡入效果
            public SpriteRenderer[] SpriteRenderers;  // 需要淡入的精灵渲染器数组
            public Color[] SpriteBaseColors;          // 精灵的原始颜色（用于计算透明度）
            public TextMesh[] TextMeshes;             // 需要淡入的文字网格数组
            public Color[] TextBaseColors;            // 文字的原始颜色
        }

        // 锦标赛积分榜中单个单元格的显示数据
        private sealed class TournamentStandingsCellViewModel
        {
            public string TopText;           // 上方文字（如比赛结果 "W" 或 "L"）
            public string BottomText;        // 下方文字（如比分 "21-18"）
            public Color TopColor = Color.white;    // 上方文字颜色
            public Color BottomColor = Color.white; // 下方文字颜色
        }

        // 锦标赛积分榜中单行选手的显示数据
        private sealed class TournamentStandingsRowViewModel
        {
            public int Seed;                 // 种子排名
            public int CharacterId;          // 角色 ID
            public bool IsPlayer;            // 是否是玩家
            public bool IsCurrent;           // 是否是当前正在查看的选手
            public bool IsChampion;          // 是否是冠军
            public bool IsFinalist;          // 是否进入决赛
            public int Wins;                 // 胜场数
            public int Losses;               // 负场数
            public string PercentageText;    // 胜率文字（如 "75.0%"）
            public string StatusText;        // 状态文字（如 "晋级"、"淘汰"）
            public Color StatusColor;        // 状态文字颜色
            public TournamentStandingsCellViewModel SemiCell = new TournamentStandingsCellViewModel();  // 半决赛单元格
            public TournamentStandingsCellViewModel FinalCell = new TournamentStandingsCellViewModel(); // 决赛单元格
        }

        // 菜单按钮列表（记录当前界面上所有可点击的按钮）
        private readonly System.Collections.Generic.List<mlpMenuButton> menuButtons = new System.Collections.Generic.List<mlpMenuButton>();
        // 颁奖典礼动画对象列表（用于驱动颁奖时的入场动画）
        private readonly System.Collections.Generic.List<TournamentAwardsAnimatedItem> awardsAnimatedItems = new System.Collections.Generic.List<TournamentAwardsAnimatedItem>();

        // === 角色/球选择器布局常量 ===
        private const float SelectorHeaderY = 126f;          // 选择器标题的 Y 坐标
        private const float SelectorArrowY = 258f;           // 左右箭头的 Y 坐标
        private const float SelectorArrowOffsetX = 74f;      // 箭头距离中心的 X 偏移
        private const float SelectorArrowSize = 36f;         // 箭头大小

        // === 角色预览布局常量 ===
        private const float PreviewScaleFactor = 0.56f;      // 预览角色整体缩放
        private const float PreviewShadowYOffset = 24f;      // 阴影的 Y 偏移
        private const float PreviewShadowScale = 0.42f;      // 阴影缩放
        private const float PreviewArmatureYOffset = -24f;   // 骨骼动画的 Y 偏移
        private const float PreviewArmatureScale = 0.82f;    // 骨骼动画缩放

        // === 菜单通用布局常量 ===
        private const float NativeUiAspect = mlpConstants.DisplayW / (float)mlpConstants.DisplayH; // 原生 UI 宽高比
        private const float MenuLogoCenterY = 96f;           // 菜单 Logo 中心 Y 坐标
        private const float MenuLogoMaxWidth = 280f;         // Logo 最大宽度
        private const float MenuLogoMaxHeight = 188f;        // Logo 最大高度
        // === 锦标赛对阵图——面板布局 ===
        private const float TournamentBoardX = mlpConstants.Width2; // 对阵图面板中心 X
        private const float TournamentBoardY = 250f;           // 对阵图面板中心 Y
        private const float TournamentBoardScale = 0.92f;      // 对阵图整体缩放

        // === 锦标赛对阵图——参赛者条目 ===
        private const float TournamentEntrantBadgeX = 252f;    // 参赛者徽章 X 坐标
        private const float TournamentEntrantNameX = 282f;     // 参赛者名字 X 坐标
        private const float TournamentEntrantBadgeScale = 0.32f; // 徽章缩放
        private const float TournamentEntrantGlowScale = 0.38f;  // 发光效果缩放
        private const float TournamentEntrantPortraitPixels = 36f; // 头像像素大小

        // === 锦标赛对阵图——比赛面板 ===
        private const float TournamentMatchPanelScale = 0.87f; // 比赛面板缩放
        private const float TournamentMatchWidth = 132f * TournamentMatchPanelScale;  // 比赛面板宽度
        private const float TournamentMatchHeight = 102f * TournamentMatchPanelScale; // 比赛面板高度
        private const float TournamentMatchHalfWidth = TournamentMatchWidth * 0.5f;   // 比赛面板半宽
        private const float TournamentSemiPanelX = 383f;       // 半决赛面板 X
        private const float TournamentSemiTopY = 184f;        // 上半区半决赛 Y
        private const float TournamentSemiBottomY = 314f;     // 下半区半决赛 Y
        private const float TournamentFinalPanelX = 521f;     // 决赛面板 X
        private const float TournamentFinalPanelY = 249f;     // 决赛面板 Y
        private const float TournamentMatchRowOffset = 16f;   // 比赛面板内行偏移
        private const float TournamentConnectorThickness = 6f; // 连接线粗细

        // === 锦标赛——战绩汇总 ===
        private const float TournamentSummaryY = 402f;         // 汇总区域 Y 坐标

        // === 锦标赛——积分榜 ===
        private const float TournamentStandingsBoardX = 236f;  // 积分榜面板 X
        private const float TournamentStandingsBoardY = 226f;  // 积分榜面板 Y
        private const float TournamentStandingsBoardScale = 0.72f; // 积分榜缩放
        private const float TournamentStandingsTitleY = 121f;  // 积分榜标题 Y
        private const float TournamentStandingsTableX = TournamentStandingsBoardX; // 表格 X
        private const float TournamentStandingsHeaderY = 154f; // 表头 Y
        private const float TournamentStandingsHeaderHeight = 24f; // 表头高度
        private const float TournamentStandingsRowStartY = 186f; // 第一行 Y
        private const float TournamentStandingsRowSpacing = 34f; // 行间距
        private const float TournamentStandingsRowHeight = 30f; // 行高
        private const float TournamentStandingsTableWidth = 248f; // 表格总宽度
        private const float TournamentStandingsIndexWidth = 22f; // 排名列宽
        private const float TournamentStandingsTeamWidth = 100f; // 队名列宽
        private const float TournamentStandingsWinsWidth = 32f; // 胜场列宽
        private const float TournamentStandingsLossesWidth = 32f; // 负场列宽
        private const float TournamentStandingsPctWidth = 62f; // 胜率列宽
        private const float TournamentStandingsBadgeScale = 0.22f; // 积分榜徽章缩放
        private const float TournamentStandingsGlowScale = 0.28f; // 积分榜发光缩放
        private const float TournamentStandingsPortraitPixels = 28f; // 积分榜头像像素

        // === 锦标赛——淘汰赛对阵图 ===
        private const float TournamentFinalsTitleX = 574f;     // 淘汰赛标题 X
        private const float TournamentFinalsTitleY = 120f;     // 淘汰赛标题 Y
        private const float TournamentBracketPanelScale = 0.9f; // 淘汰赛面板缩放
        private const float TournamentBracketPanelWidth = 132f * TournamentBracketPanelScale;  // 面板宽
        private const float TournamentBracketPanelHeight = 102f * TournamentBracketPanelScale; // 面板高
        private const float TournamentBracketFinalX = 574f;    // 决赛位置 X
        private const float TournamentBracketFinalY = 172f;    // 决赛位置 Y
        private const float TournamentBracketSemiLeftX = 450f; // 左半区半决赛 X
        private const float TournamentBracketSemiRightX = 698f; // 右半区半决赛 X
        private const float TournamentBracketSemiY = 244f;    // 半决赛 Y
        private const float TournamentBracketPlacementX = 574f; // 排名显示 X
        private const float TournamentBracketPlacementY = 332f; // 排名显示 Y
        private const float TournamentBracketRowOffset = 18f;  // 行偏移
        private const float TournamentBracketBadgeScale = 0.2f; // 淘汰赛徽章缩放
        private const float TournamentBracketGlowScale = 0.26f; // 淘汰赛发光缩放
        private const float TournamentBracketPortraitPixels = 24f; // 淘汰赛头像像素

        // === 锦标赛颁奖典礼布局 ===
        private const float TournamentAwardsShowcaseY = 244f;  // 展示区 Y
        private const float TournamentAwardsShowcaseWidth = 560f; // 展示区宽
        private const float TournamentAwardsShowcaseHeight = 322f; // 展示区高
        private const float TournamentAwardsPlaqueY = 132f;    // 奖牌匾 Y
        private const float TournamentAwardsPodiumY = 376f;    // 领奖台 Y
        private const float TournamentAwardsPodiumWidth = 428f; // 领奖台宽
        private const float TournamentAwardsPodiumHeight = 170f; // 领奖台高
        private const float TournamentAwardsChampionX = mlpConstants.Width2; // 冠军 X
        private const float TournamentAwardsChampionY = 296f;  // 冠军 Y
        private const float TournamentAwardsLeftX = mlpConstants.Width2 - 114f; // 左侧选手 X
        private const float TournamentAwardsLeftY = 314f;     // 左侧选手 Y
        private const float TournamentAwardsRightX = mlpConstants.Width2 + 114f; // 右侧选手 X
        private const float TournamentAwardsRightY = 320f;    // 右侧选手 Y
        private const float TournamentAwardsChampionScale = 0.82f; // 冠军缩放
        private const float TournamentAwardsSideScale = 0.78f; // 两侧选手缩放
        private const float TournamentAwardsArmatureScale = 0.82f; // 颁奖骨骼动画缩放
        private const float TournamentAwardsArmatureYOffset = -18f; // 颁奖骨骼 Y 偏移
        private const float TournamentAwardsCelebrationDelay = 0.66f; // 庆祝动画延迟
        // === 菜单顶部按钮（音乐、帮助） ===
        private const float MenuTopButtonY = 44f;             // 顶部按钮 Y 坐标
        private const float MenuMusicButtonX = 770f;          // 音乐按钮 X
        private const float MenuHelpButtonX = 706f;           // 帮助按钮 X
        private const float MenuTopButtonSize = 60f;          // 顶部按钮大小
        private const float MenuTopIconPixels = 58f;          // 顶部图标像素大小

        // === 快速测试菜单（开发调试用） ===
        private const float QuickTestMenuLabelX = 666f;       // 测试菜单标签 X
        private const float QuickTestMenuControlY = 442f;     // 测试菜单控件 Y
        private const float QuickTestMenuToggleX = 706f;      // 测试开关 X
        private const float QuickTestMenuToggleWidth = 58f;   // 测试开关宽
        private const float QuickTestMenuToggleHeight = 34f;  // 测试开关高
        private const float QuickTestMenuInfoButtonX = 758f;  // 测试信息按钮 X
        private const float QuickTestMenuInfoButtonSize = 32f; // 测试信息按钮大小
        private const float QuickTestMenuInfoPanelX = 666f;   // 测试信息面板 X
        private const float QuickTestMenuInfoPanelY = 356f;   // 测试信息面板 Y
        private const float QuickTestMenuInfoPanelWidth = 220f; // 测试信息面板宽
        private const float QuickTestMenuInfoPanelHeight = 94f; // 测试信息面板高
        // === 冒险模式——地图面板 ===
        private const float AdventureMapPanelX = 306f;        // 地图面板 X
        private const float AdventureMapPanelY = 238f;        // 地图面板 Y
        private const float AdventureMapPanelWidth = 574f;    // 地图面板宽
        private const float AdventureMapPanelHeight = 348f;   // 地图面板高
        private const float AdventureMechanicInfoY = 421f;    // 玩法说明 Y
        private const float AdventureMechanicInfoWidth = 540f; // 玩法说明宽
        private const float AdventureMechanicInfoHeight = 18f; // 玩法说明高

        // === 冒险模式——关卡海报 ===
        private const float AdventurePosterX = 702f;          // 海报 X
        private const float AdventurePosterY = 270f;          // 海报 Y
        private const float AdventurePosterWidth = 184f;      // 海报宽
        private const float AdventurePosterHeight = 292f;     // 海报高

        // === 冒险模式——路线节点 ===
        private const float AdventureNodeWidth = 68f;         // 节点宽
        private const float AdventureNodeHeight = 78f;        // 节点高

        // === 单人模式卡片选择（冒险/锦标赛两张卡片） ===
        private const float SinglePlayerModeCardY = 300f;     // 卡片 Y
        private const float SinglePlayerModeCardWidth = 318f;  // 卡片宽
        private const float SinglePlayerModeCardHeight = 254f; // 卡片高
        private const float SinglePlayerModeLeftCardX = 232f;  // 左卡片 X（冒险）
        private const float SinglePlayerModeRightCardX = 568f; // 右卡片 X（锦标赛）

        // === 故事漫画面板 ===
        private const float StoryPanelX = mlpConstants.Width2; // 漫画面板中心 X
        private const float StoryPanelY = 260f;               // 漫画面板 Y
        private const float StoryPanelWidth = 610f;           // 漫画面板宽
        private const float StoryPanelHeight = 264f;          // 漫画面板高

        // === 故事漫画动画（电影式播放效果） ===
        private const float StoryCinematicWidth = mlpConstants.Width; // 影院模式宽
        private const float StoryCinematicHeight = 480f;      // 影院模式高
        private const float StoryCinematicPageSeconds = 3.0f;  // 每页停留秒数
        private const float StoryCinematicFadeSeconds = 0.42f; // 翻页淡入淡出秒数
        private const float StoryCinematicPanPixels = 14f;     // 平移像素量
        private const float StoryCinematicZoomAmount = 0.035f; // 缩放量

        // === 故事漫画过场 UI 元素 ===
        private const float StoryIntroTitleY = 28f;           // 标题 Y
        private const float StoryIntroCaptionY = 466f;        // 字幕 Y
        private const float StoryIntroPauseX = 54f;           // 暂停按钮 X
        private const float StoryIntroPauseY = 456f;          // 暂停按钮 Y
        private const float StoryIntroSkipX = 748f;           // 跳过按钮 X
        private const float StoryIntroSkipY = 456f;           // 跳过按钮 Y

        // === 故事传说面板按钮 ===
        private const float StoryIntroLoreButtonX = 776f;     // 传说按钮 X
        private const float StoryIntroLoreButtonY = 234f;     // 传说按钮 Y
        private const float StoryIntroLoreOpenButtonX = StoryIntroLorePanelX;   // 打开后按钮 X
        private const float StoryIntroLoreOpenButtonY = StoryIntroLorePanelY + 108f; // 打开后按钮 Y
        private const float StoryIntroLoreButtonHitWidth = 112f;  // 按钮点击区域宽
        private const float StoryIntroLoreButtonHitHeight = 80f;  // 按钮点击区域高
        private const float StoryIntroLoreIconOffsetX = 0f;   // 图标 X 偏移
        private const float StoryIntroLoreIconOffsetY = -18f;  // 图标 Y 偏移
        private const float StoryIntroLoreLabelOffsetX = 0f;  // 标签 X 偏移
        private const float StoryIntroLoreLabelOffsetY = 18f;  // 标签 Y 偏移
        private const float StoryIntroLoreOpenLabelOffsetX = 0f;  // 打开后标签 X 偏移
        private const float StoryIntroLoreOpenLabelOffsetY = 0f;  // 打开后标签 Y 偏移
        private const float StoryIntroLoreIconSize = 42f;     // 图标大小

        // === 故事传说面板布局 ===
        private const float StoryIntroLorePanelX = 638f;      // 传说面板 X
        private const float StoryIntroLorePanelY = 236f;      // 传说面板 Y
        private const float StoryIntroLorePanelWidth = 228f;  // 传说面板宽
        private const float StoryIntroLorePanelHeight = 304f; // 传说面板高

        // === 故事传说面板颜色 ===
        private static readonly Color StoryIntroLoreClosedLabelColor = new Color(0.98f, 0.95f, 0.86f, 0.98f); // 关闭时标签颜色
        private static readonly Color StoryIntroLoreOpenLabelColor = new Color32(0x72, 0x43, 0x1B, 0xFF);     // 打开时标签颜色
        private static readonly Color StoryIntroLorePageTagColor = new Color32(0x8B, 0x5B, 0x2B, 0xFF);      // 页码标签颜色

        // === 旧版菜单背景 ===
        private const float LegacyMenuBackgroundWidth = 1398f; // 旧背景宽
        private const float LegacyMenuBackgroundHeight = 480f; // 旧背景高
        private const float LegacyTintPanelSourcePixels = 10f; // 旧着色面板源像素

        // === 球皮肤选择器布局 ===
        private const float OptionBallHeaderY = 208f;         // 球选项标题 Y
        private const float OptionBallPreviewY = 232f;        // 球预览 Y
        private const float OptionBallLabelY = 260f;          // 球名称标签 Y
        private const float BallSelectorArrowOffsetX = 68f;   // 球选择箭头 X 偏移
        private const float BallSelectorArrowSize = 34f;      // 球选择箭头大小
        private const float BallPreviewPixels = 50f;          // 球预览像素大小

        // === 双人模式球面板 ===
        private const float TwoPlayerBallPanelY = 360f;       // 双人球面板 Y
        private const float TwoPlayerBallHeaderY = 320f;      // 双人球标题 Y
        private const float TwoPlayerBallPreviewY = 356f;     // 双人球预览 Y
        private const float TwoPlayerBallLabelY = 394f;       // 双人球标签 Y
        private const float TwoPlayerBallPanelWidth = 168f;   // 双人球面板宽
        private const float TwoPlayerBallPanelHeight = 148f;  // 双人球面板高

        // === 运行时核心对象 ===
        private Transform runtimeRoot;                       // 当前界面的根节点（切换界面时销毁重建）
        private mlpGameCore gameCore;                        // 比赛核心控制器（比赛开始时创建，结束时销毁）
        private Camera mainCamera;                           // 主摄像机
        private mlpFixedResolutionPresenter fixedResolutionPresenter; // 固定分辨率管理器
        private mlpBootstrapScreen currentScreen;            // 当前所处的屏幕状态

        // === 玩家选择状态 ===
        private mlpParticipantMode pendingParticipantMode = mlpParticipantMode.OnePlayer; // 待确认的玩家模式
        private int quickCharacterId;                        // 快速比赛选的角色 ID
        private int trainingCharacterId;                     // 训练模式选的角色 ID
        private int tournamentCharacterId;                   // 锦标赛选的角色 ID
        private int versusLeftCharacterId;                   // 双人模式左侧玩家角色 ID
        private int versusRightCharacterId;                  // 双人模式右侧玩家角色 ID
        private mlpBallSelection quickBallSelection;         // 快速比赛选的球皮肤
        private mlpBallSelection trainingBallSelection;      // 训练模式选的球皮肤
        private mlpBallSelection tournamentBallSelection;    // 锦标赛选的球皮肤
        private mlpBallSelection versusBallSelection;        // 双人模式选的球皮肤

        // === 颁奖典礼动画状态 ===
        private float awardsElapsed;                         // 颁奖动画已播放时间
        private bool awardsCelebrationTriggered;             // 庆祝动画是否已触发
        private DBLiteArmature awardsCelebrationPlayer;      // 庆祝动画的角色骨骼
        private string awardsCelebrationCupAnimation;        // 奖杯动画名称

        // === 菜单顶部按钮 ===
        private mlpIconButton menuMusicButton;               // 音乐开关按钮
        private mlpIconButton menuHelpButton;                // 帮助按钮

        // === 快速测试菜单（开发调试用） ===
        private mlpMenuButton quickTestMenuToggleButton;     // 测试模式开关
        private mlpMenuButton quickTestMenuInfoButton;       // 测试信息按钮
        private GameObject quickTestMenuInfoRoot;            // 测试信息面板根对象
        private bool quickTestMenuInfoVisible;               // 测试信息面板是否可见

        // === 原生 UI 和视口 ===
        private mlpNativeMenuTextLayer nativeMenuTextLayer;  // 原生菜单文字层
        private bool usingNativeUiPresentation;              // 是否使用原生 UI 渲染
        private int viewportScreenWidth = -1;                // 视口宽度缓存
        private int viewportScreenHeight = -1;               // 视口高度缓存

        // === 故事漫画过场状态 ===
        private mlpSinglePlayerNarrativeMode storyIntroMode = mlpSinglePlayerNarrativeMode.Adventure; // 当前故事类型
        private int storyIntroPanelIndex;                    // 当前漫画页码
        private System.Action storyIntroContinueAction;      // 漫画结束后的继续回调
        private System.Action storyIntroCancelAction;        // 漫画取消时的回调
        private mlpMenuButton storyIntroPauseButton;         // 漫画暂停按钮
        private mlpMenuButton storyIntroLoreButton;          // 传说面板按钮
        private bool storyIntroPaused;                       // 漫画是否暂停
        private bool storyIntroLoreOpen;                     // 传说面板是否打开
        private bool storyIntroPauseBeforeLore;              // 打开传说前是否已暂停
        private float storyIntroElapsed;                     // 漫画已播放时间
        private GameObject storyIntroImageObject;            // 当前漫画图片对象
        private Vector3 storyIntroImageBaseScale;            // 漫画图片原始缩放
        private SpriteRenderer storyIntroImageRenderer;      // 漫画图片渲染器
        private Color storyIntroAccentColor = Color.white;   // 漫画强调色
        private GameObject storyIntroLoreRoot;               // 传说面板根对象
        private GameObject storyIntroLoreArtRoot;            // 传说插画根对象
        private GameObject storyIntroLoreLabelObject;        // 传说按钮标签对象
        private SpriteRenderer storyIntroLoreIconRenderer;   // 传说按钮图标渲染器
        private readonly System.Collections.Generic.List<GameObject> storyIntroLoreTextObjects = new System.Collections.Generic.List<GameObject>(); // 传说文字对象列表

        // === 冒险模式状态 ===
        private int adventureSelectedLevelIndex;             // 当前选中的冒险关卡索引
        private Texture2D adventureTreasureMapTexture;       // 藏宝地图纹理缓存

        /// <summary>
        /// 游戏启动时设置摄像机、音频系统并显示主菜单。
        /// </summary>
        // ═══════════════════════════════════════════════════════════════
        // 第 2 块：生命周期方法
        // Awake 初始化摄像机/音频/存档，Update 每帧调度比赛和菜单逻辑
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 游戏启动时设置摄像机、音频系统并显示主菜单。
        /// </summary>
        private void Awake()
        {
            // 1. 保存单例引用，获取或创建主摄像机
            activeInstance = this;
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            // 2. 设置摄像机为正交模式（2D 游戏），调整大小以适配像素分辨率
            mainCamera.orthographic = true;     //正交模式，没有近大远小的透视效果
            mainCamera.orthographicSize = mlpConstants.GameH / (2f * mlpConstants.PixelsPerUnit);       //根据游戏设计分辨率算出摄像机能看到多大的范围。
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = Color.black;

            // 3. 获取或添加固定分辨率组件（确保画面比例一致）
            fixedResolutionPresenter = GetComponent<mlpFixedResolutionPresenter>();
            if (fixedResolutionPresenter == null)
            {
                fixedResolutionPresenter = gameObject.AddComponent<mlpFixedResolutionPresenter>();
            }

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            EnableNativeMenuPresentation();

            // 4. 创建运行时根节点和音频系统
            runtimeRoot = new GameObject("mlpRuntime").transform;
            mlpAudio.Create(transform);

            // 5. 从存档中读取上次选择的角色和篮球皮肤
            var inventory = mlpInventory.Instance;
            quickCharacterId = mlpPlayersData.SanitizeCharacterId(inventory.SelectedQuickCharacterId);
            trainingCharacterId = mlpPlayersData.SanitizeCharacterId(inventory.SelectedTrainingCharacterId, quickCharacterId);
            tournamentCharacterId = mlpPlayersData.SanitizeCharacterId(inventory.SelectedTournamentCharacterId, quickCharacterId);
            quickBallSelection = inventory.SelectedQuickBallSelection;
            trainingBallSelection = inventory.SelectedTrainingBallSelection;
            tournamentBallSelection = inventory.SelectedTournamentBallSelection;
            versusBallSelection = inventory.SelectedVersusBallSelection;
            SeedTwoPlayerSelection();

            // 6. 显示主菜单（选择 1 人 / 2 人 / 教练 / 训练）
            ShowPlayerCountMenu();
        }

        /// <summary>
        /// Unity 生命周期回调：当此 GameObject 被销毁时自动调用。
        /// 负责清理全局单例引用，防止其他代码访问已销毁的对象（野指针）；
        /// 同时释放冒险模式运行时生成的藏宝地图纹理，避免 GPU 内存泄漏。
        /// </summary>
        private void OnDestroy()
        {
            // 清除单例引用：只有当销毁的确实是当前活跃实例时才置空，
            // 避免新的实例已经替换进来后被误清。
            if (activeInstance == this)
            {
                activeInstance = null;
            }

            // 释放冒险模式的藏宝地图纹理。
            // 该纹理是运行时动态生成的（不是项目资源文件），不会随场景自动回收，
            // 必须手动调用 Destroy 释放 GPU 显存，否则会内存泄漏。
            if (adventureTreasureMapTexture != null)
            {
                Destroy(adventureTreasureMapTexture);
                adventureTreasureMapTexture = null;
            }
        }

        /// <summary>
        /// 从帮助面板启动教程的静态入口方法。
        /// 其他脚本（如帮助面板的"开始教程"按钮）不需要持有 mlpGameBootstrap 的引用，
        /// 直接调用这个静态方法即可触发教程流程。
        /// </summary>
        /// <returns>成功启动教程返回 true，找不到 bootstrap 实例返回 false。</returns>
        public static bool TryStartTutorialFromHelp()
        {
            // 优先使用缓存的单例引用；如果为空（比如场景重建了），
            // 则通过 FindObjectOfType 在场景中搜索，作为兜底方案。
            var bootstrap = activeInstance != null ? activeInstance : FindObjectOfType<mlpGameBootstrap>();
            if (bootstrap == null)
            {
                return false;
            }

            // 调用实例方法，实际执行从帮助面板进入教程的逻辑。
            bootstrap.StartTutorialFromHelpPanel();
            return true;
        }

        /// <summary>
        /// Unity 生命周期回调：每帧自动调用一次。
        /// 这是整个游戏的"大调度器"——根据当前游戏状态（比赛中 / 菜单 / 过场动画）
        /// 分发不同的处理逻辑。整体分三层判断：
        ///   第一层：比赛是否在进行？ → 委托给 gameCore 更新比赛逻辑
        ///   第二层：帮助面板是否打开？ → 暂停菜单交互，只保留帮助面板
        ///   第三层：正常菜单/过场 → 更新按钮交互、动画、按键响应
        /// </summary>
        private void Update()
        {
            // =====================================================================
            // 第一层判断：比赛是否在进行？
            // gameCore 不为 null 说明有一场比赛正在进行（快速比赛/锦标赛/冒险/训练等）
            // =====================================================================
            if (gameCore != null)
                {
                // 将更新委托给 gameCore，它会处理物理、AI、计时、得分等比赛核心逻辑。
                // Time.deltaTime 是上一帧到这一帧经过的秒数，用于帧率无关的计时。
                gameCore.Update(Time.deltaTime);

                // --- 检查比赛是否自然结束（时间到/分出胜负） ---
                // AdvanceFlowRequested 是 gameCore 在比赛结束时设置的标志位。
                if (gameCore.AdvanceFlowRequested)
                {
                    var inventory = mlpInventory.Instance;

                    // 冒险模式：清除比赛场景，更新冒险进度（赢了推进关卡，输了重试），
                    // 然后显示本关的结果界面。
                    if (inventory.IsAdventureActive)
                    {
                        var playerWon = inventory.MatchData.WhoWins() < 0; // < 0 表示玩家赢
                        ClearRuntime();                           // 销毁比赛场景的所有运行时对象
                        inventory.AdvanceAdventure(playerWon);    // 更新冒险存档（推进/重试）
                        ShowAdventureResult(playerWon);           // 显示"胜利/失败"结果界面
                        return;
                    }

                    // 锦标赛模式：清除比赛场景，推进锦标赛对阵图，
                    // 然后显示下一轮的对阵图界面。
                    ClearRuntime();
                    inventory.AdvanceTournament();                 // 推进锦标赛到下一轮
                    ShowTournamentBracket();                       // 显示对阵图
                    return;
                }

                // --- 检查玩家是否主动按了返回键退出比赛 ---
                // ReturnToMenuRequested 是 gameCore 在玩家请求退出时设置的标志位。
                if (gameCore.ReturnToMenuRequested)
                {
                    var inventory = mlpInventory.Instance;
                    // 先记住之前在什么模式，因为 ClearRuntime 会销毁 gameCore，
                    // 之后就无法再查询这些状态了。
                    var tournamentWasActive = inventory.IsTournamentActive;
                    var adventureWasActive = inventory.IsAdventureActive;
                    ClearRuntime(); // 销毁比赛场景

                    // 冒险模式中途退出 → 回到冒险地图（可以选择其他关卡）
                    if (adventureWasActive)
                    {
                        ShowAdventureMap();
                        return;
                    }

                    // 锦标赛中途退出 → 放弃整个锦标赛，回到主菜单
                    if (tournamentWasActive)
                    {
                        inventory.AbandonTournament();
                        ShowPlayerCountMenu();
                        return;
                    }

                    // 训练/教程模式退出 → 检查是否有待处理的教程动作
                    if (HandlePendingTutorialAction())
                    {
                        return;
                    }

                    // 普通比赛（快速比赛/双人）退出 → 回到主菜单
                    ShowPlayerCountMenu();
                }

                // 比赛还在进行中，或者刚处理完退出逻辑，不执行下面的菜单逻辑
                return;
            }

            // 第二层判断：帮助面板是否打开？
            // 帮助面板是模态对话框，打开时暂停所有菜单交互，防止误操作。
            var helpVisible = mlpHelpPanel.IsAnyOpen;
            // 帮助面板打开时隐藏原生文字层，避免文字重叠
            nativeMenuTextLayer?.SetVisible(!helpVisible);

            if (helpVisible)
            {
                return; // 帮助面板打开期间，不处理任何菜单输入
            }

            // 第三层判断：正常菜单/过场动画更新

            // 更新锦标赛颁奖典礼的动画序列（角色庆祝、奖杯动画等）
            UpdateTournamentAwardsSequence(Time.deltaTime);
            // 刷新原生菜单文字层的视口尺寸，适配窗口大小变化
            RefreshNativeMenuViewport();

            // 如果当前在故事漫画过场界面，更新漫画动画（翻页、淡入淡出等）。
            // UpdateStoryIntroCinematic 返回 true 表示漫画还在播放中，跳过后续菜单逻辑。
            if (currentScreen == mlpBootstrapScreen.StoryIntro && UpdateStoryIntroCinematic(Time.deltaTime))
            {
                return;
            }

            // --- 更新当前界面所有菜单按钮的交互（悬停高亮、点击响应等） ---
            // menuButtons 是当前界面所有可交互按钮的列表。
            // 每个按钮的 Update 会检测鼠标位置和点击状态。
            for (var i = 0; i < menuButtons.Count; i++)
            {
                var screenRoot = runtimeRoot; // 记录更新前的根节点
                menuButtons[i].Update(mainCamera);
                // 如果按钮点击导致了界面切换（runtimeRoot 被重建），立即停止遍历，
                // 防止继续更新已经不存在的旧按钮而报错。
                if (screenRoot != runtimeRoot)
                {
                    break;
                }
            }

            // --- 更新右上角的固定按钮（音乐开关、帮助按钮） ---
            // 这些按钮不属于某个具体界面，始终显示在右上角（故事漫画界面除外）。
            if (runtimeRoot != null && currentScreen != mlpBootstrapScreen.StoryIntro)
            {
                // 根据当前音乐播放状态切换音乐按钮的图标（播放/暂停）
                menuMusicButton?.SetActiveIconIndex(GetMusicIconIndex());
                var iconScreenRoot = runtimeRoot;
                menuMusicButton?.Update(mainCamera);
                // 同样检查界面是否因按钮点击而切换
                if (iconScreenRoot != runtimeRoot)
                {
                    return;
                }

                menuHelpButton?.Update(mainCamera);
                if (iconScreenRoot != runtimeRoot)
                {
                    return;
                }
            }

            // --- 全局按键：Escape 键返回上一界面 ---
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleMenuEscape();
            }
        }


//第三块：菜单导航    总的来说这部分负责屏幕之间的跳转

        /// <summary>
        /// 在各菜单界面处理 Escape 键。返回上一界面或取消当前操作。
        /// </summary>
        private void HandleMenuEscape()
        {
            // 1. 根据当前所在界面，决定按 Escape 后返回哪里
            switch (currentScreen)
            {
                // 2. 比赛类型选择界面：返回角色选择界面
                case mlpBootstrapScreen.MatchType:
                    ShowSinglePlayerCharacterSetup();
                    break;
                // 3. 故事介绍界面：如果传说面板开着就关闭它，否则取消整个故事介绍
                case mlpBootstrapScreen.StoryIntro:
                    if (storyIntroLoreOpen)
                    {
                        SetStoryIntroLoreVisibility(false);
                        break;
                    }

                    CancelSinglePlayerStoryIntro();
                    break;
                // 4. 冒险/快速比赛/锦标赛设置界面：返回比赛类型选择
                case mlpBootstrapScreen.AdventurePreview:
                case mlpBootstrapScreen.AdventureMap:
                case mlpBootstrapScreen.AdventureResult:
                case mlpBootstrapScreen.SinglePlayerSetup:
                case mlpBootstrapScreen.TournamentSetup:
                    ShowMatchTypeMenu();
                    break;
                // 5. 单人角色选择、双人设置、训练设置界面：返回主菜单
                case mlpBootstrapScreen.SinglePlayerCharacterSetup:
                case mlpBootstrapScreen.TwoPlayerSetup:
                case mlpBootstrapScreen.TrainingSetup:
                    ShowPlayerCountMenu();
                    break;
                // 6. 锦标赛对阵图或完成界面：放弃锦标赛并返回主菜单
                case mlpBootstrapScreen.TournamentBracket:
                case mlpBootstrapScreen.TournamentComplete:
                    mlpInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                    break;
                // 7. 锦标赛颁奖界面：返回对阵图
                case mlpBootstrapScreen.TournamentAwards:
                    ShowTournamentBracket();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 第 3 块：主菜单和快速测试控件
        // ShowPlayerCountMenu 显示 1人/2人/教程/训练 四个入口按钮
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 显示主菜单，包含单人、双人、教程和训练模式的按钮。
        /// </summary>
        private void ShowPlayerCountMenu()
        {
            // 1. 设置当前界面状态，初始化菜单（显示 logo，蓝色背景）
            currentScreen = mlpBootstrapScreen.PlayerCount;
            BeginMenuScreen(true, false, "bg2blue0000");

            // 2. 创建半透明面板作为按钮容器
            CreatePanel("PlayersPanel", mlpConstants.Width2, 336f, 304f, 286f, 8, new Color(0.05f, 0.08f, 0.1f, 0.72f));

            // 3. 添加四个菜单按钮：1 人模式、2 人模式、教程、训练
            var inventory = mlpInventory.Instance;
            // 创建"1 PLAYER"按钮：文字、X居中、Y=246、宽228、高52，点击后执行lambda回调
            menuButtons.Add(new mlpMenuButton("1 PLAYER", mlpConstants.Width2, 246f, 228f, 52f, () =>
            {
                pendingParticipantMode = mlpParticipantMode.OnePlayer; // 设置为单人模式
                inventory.SetParticipantMode(pendingParticipantMode);   // 将单人模式应用到库存系统
                ShowSinglePlayerCharacterSetup();                       // 显示单人角色选择界面
            }, runtimeRoot));

            menuButtons.Add(new mlpMenuButton("2 PLAYER", mlpConstants.Width2, 306f, 228f, 52f, () =>
            {
                pendingParticipantMode = mlpParticipantMode.TwoPlayers;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowTwoPlayerSetup();
            }, runtimeRoot));

            menuButtons.Add(new mlpMenuButton("TUTORIAL", mlpConstants.Width2, 366f, 228f, 52f, StartTutorialFlow, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("TRAINING", mlpConstants.Width2, 426f, 228f, 52f, ShowTrainingSetup, runtimeRoot));

            // 4. 创建快速测试模式的开关控件（开发者调试用）
            CreateQuickTestMenuControls();
        }

        // 在主菜单底部创建"快速测试"开关和信息按钮（开发者调试用）
        private void CreateQuickTestMenuControls()
        {
            CreateMenuText(
                "QuickTestMenuLabel",
                "FAST TEST",
                QuickTestMenuLabelX,
                QuickTestMenuControlY + 1f,
                12,
                new Color32(0xFF, 0xD2, 0x75, 0xFF),
                TextAnchor.MiddleRight,
                55,
                mlpTextStyle.TournamentAccent);

            quickTestMenuToggleButton = new mlpMenuButton(
                QuickTestMenuToggleLabel(),
                QuickTestMenuToggleX,
                QuickTestMenuControlY,
                QuickTestMenuToggleWidth,
                QuickTestMenuToggleHeight,
                ToggleQuickTestModeFromMenu,
                runtimeRoot,
                54,
                mlpTextStyle.TournamentAccent);
            menuButtons.Add(quickTestMenuToggleButton);

            quickTestMenuInfoButton = new mlpMenuButton(
                "?",
                QuickTestMenuInfoButtonX,
                QuickTestMenuControlY,
                QuickTestMenuInfoButtonSize,
                QuickTestMenuInfoButtonSize,
                ToggleQuickTestInfoFromMenu,
                runtimeRoot,
                54,
                mlpTextStyle.TournamentAccent);
            menuButtons.Add(quickTestMenuInfoButton);

            BuildQuickTestInfoPanel();
            SetQuickTestMenuInfoVisible(false);
        }

        // 切换快速测试模式的开关状态并刷新按钮文字
        private void ToggleQuickTestModeFromMenu()
        {
            mlpQuickTestSettings.Enabled = !mlpQuickTestSettings.Enabled;
            RefreshQuickTestMenuToggle();
        }

        // 切换快速测试信息面板的显示/隐藏
        private void ToggleQuickTestInfoFromMenu()
        {
            SetQuickTestMenuInfoVisible(!quickTestMenuInfoVisible);
        }

        // 刷新快速测试开关按钮的文字（ON/OFF）
        private void RefreshQuickTestMenuToggle()
        {
            quickTestMenuToggleButton?.SetText(QuickTestMenuToggleLabel());
        }

        // 设置快速测试信息面板的可见性
        private void SetQuickTestMenuInfoVisible(bool visible)
        {
            quickTestMenuInfoVisible = visible;
            if (quickTestMenuInfoRoot != null)
            {
                quickTestMenuInfoRoot.SetActive(visible);
            }
        }

        // 构建快速测试信息面板的 UI（标题 + 说明文字）
        private void BuildQuickTestInfoPanel()
        {
            quickTestMenuInfoRoot = new GameObject("QuickTestMenuInfoPanel");
            quickTestMenuInfoRoot.transform.SetParent(runtimeRoot, false);
            CreatePanel(
                "QuickTestMenuInfoPanelFill",
                QuickTestMenuInfoPanelX,
                QuickTestMenuInfoPanelY,
                QuickTestMenuInfoPanelWidth,
                QuickTestMenuInfoPanelHeight,
                58,
                new Color(0.06f, 0.09f, 0.12f, 0.9f),
                quickTestMenuInfoRoot.transform);
            mlpRender.Text(
                "QuickTestMenuInfoTitle",
                "FAST TEST MODE",
                QuickTestMenuInfoPanelX - QuickTestMenuInfoPanelWidth * 0.5f + 14f,
                QuickTestMenuInfoPanelY - 27f,
                12,
                new Color32(0xFF, 0xD2, 0x75, 0xFF),
                TextAnchor.MiddleLeft,
                86,
                quickTestMenuInfoRoot.transform,
                mlpTextStyle.TournamentAccent);
            mlpRender.Text(
                "QuickTestMenuInfoBody",
                "15s matches.\nSkills recharge instantly.\nUse it for quick review.",
                QuickTestMenuInfoPanelX - QuickTestMenuInfoPanelWidth * 0.5f + 14f,
                QuickTestMenuInfoPanelY + 15f,
                10,
                new Color32(0xF4, 0xF7, 0xFF, 0xFF),
                TextAnchor.MiddleLeft,
                86,
                quickTestMenuInfoRoot.transform,
                mlpTextStyle.TournamentBody);
        }

        // 返回快速测试开关的当前文字标签
        private static string QuickTestMenuToggleLabel()
        {
            return mlpQuickTestSettings.Enabled ? "ON" : "OFF";
        }

        // ═══════════════════════════════════════════════════════════════
        // 第 4 块：角色选择和模式选择
        // 选角色 → 选模式（冒险/锦标赛），或直接进入快速比赛/训练/双人
        // ═══════════════════════════════════════════════════════════════

        // 显示单人模式角色选择界面（左右箭头切换角色，点 NEXT 确认）
        private void ShowSinglePlayerCharacterSetup()
        {
            // 1. 设置当前界面为单人角色选择
            currentScreen = mlpBootstrapScreen.SinglePlayerCharacterSetup;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("SELECT CHARACTER", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            // 2. 创建居中的角色选择面板（带左右箭头切换角色）
            CreatePanel("SinglePlayerCharacterPanel", mlpConstants.Width2, 280f, 260f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreateCharacterSelector(
                "SinglePlayer",
                "CHARACTER",
                mlpConstants.Width2,
                quickCharacterId,
                () =>
                {
                    quickCharacterId = WrapCharacter(quickCharacterId, -1);
                    ShowSinglePlayerCharacterSetup();
                },
                () =>
                {
                    quickCharacterId = WrapCharacter(quickCharacterId, 1);
                    ShowSinglePlayerCharacterSetup();
                },
                306f,
                3.85f,
                398f);

            //back button
            menuButtons.Add(new mlpMenuButton("BACK", 312f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));

            //next button
            menuButtons.Add(new mlpMenuButton("NEXT", 488f, 452f, 150f, 42f, ConfirmSinglePlayerCharacter, runtimeRoot));
        }

        // 确认单人角色选择，保存到存档后进入模式选择界面
        private void ConfirmSinglePlayerCharacter()
        {
            tournamentCharacterId = quickCharacterId;
            var inventory = mlpInventory.Instance;
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetTournamentSelection(tournamentCharacterId);
            ShowMatchTypeMenu();
        }

        /// <summary>
        /// 显示模式选择界面，包含冒险（故事跑酷）和锦标赛（赛季模式）卡片。
        /// </summary>
        private void ShowMatchTypeMenu()
        {
            // 1. 设置当前界面为模式选择
            currentScreen = mlpBootstrapScreen.MatchType;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            BeginMenuScreen(true, false, "bg10000");

            // 2. 左侧卡片：冒险模式（故事跑酷，逐关挑战守卫）
            CreateSinglePlayerModeCard(
                "Adventure",
                mlpSinglePlayerNarrative.Adventure,
                SinglePlayerModeLeftCardX,
                new Color(0.06f, 0.10f, 0.11f, 0.92f),
                new Color32(0xFF, 0x9F, 0x32, 0xFF),
                "STORY RUN",
                "Warden duels. Sigils. Escape.",
                "Follow the park map",
                "and reopen the gates.",
                "START ROUTE",
                ShowAdventureStoryIntro);

            // 3. 右侧卡片：锦标赛模式（赛季制，8 人淘汰赛）
            CreateSinglePlayerModeCard(
                "Tournament",
                mlpSinglePlayerNarrative.Tournament,
                SinglePlayerModeRightCardX,
                new Color(0.05f, 0.07f, 0.14f, 0.92f),
                new Color32(0x78, 0xE7, 0xFF, 0xFF),
                "SEASON RUN",
                "Divisions. Finals. Trophy.",
                "Beat the bracket",
                "and claim the Cup.",
                "ENTER CUP",
                ShowTournamentStoryIntro);

            menuButtons.Add(new mlpMenuButton("BACK", mlpConstants.Width2, 442f, 180f, 42f, ShowSinglePlayerCharacterSetup, runtimeRoot));
        }



        // ═══════════════════════════════════════════════════════════════
        // 第 5 块：故事漫画过场
        // 进入冒险/锦标赛前播放漫画，支持翻页、暂停、传说面板和跳过
        // ═══════════════════════════════════════════════════════════════

        // 显示冒险模式的开场故事漫画
        private void ShowAdventureStoryIntro()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Adventure, ShowAdventureMap);
        }

        // 显示锦标赛模式的开场故事漫画
        private void ShowTournamentStoryIntro()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Tournament, StartTournamentFlow);
        }

        private void ShowSinglePlayerStoryIntro(
            mlpSinglePlayerNarrativeMode mode,
            System.Action continueAction,
            System.Action cancelAction = null)
        {
            // 1. 保存故事模式类型和完成后的回调
            storyIntroMode = mode;
            storyIntroPanelIndex = 0;
            storyIntroContinueAction = continueAction;
            storyIntroCancelAction = cancelAction ?? ShowMatchTypeMenu;
            // 2. 重置暂停和传说面板状态
            storyIntroPaused = false;
            storyIntroLoreOpen = false;
            storyIntroPauseBeforeLore = false;
            // 3. 显示第一帧漫画面板
            ShowSinglePlayerStoryIntroPanel();
        }

        // 显示故事漫画的当前页（图片、标题、字幕、暂停/跳过按钮）
        private void ShowSinglePlayerStoryIntroPanel()
        {
            // 1. 获取当前模式的开场漫画面板列表，没有则直接跳过
            var mode = mlpSinglePlayerNarrative.GetMode(storyIntroMode);
            var panels = mode.OpeningComic;
            if (panels == null || panels.Length == 0)
            {
                ContinueSinglePlayerStoryIntro();
                return;
            }

            // 2. 获取当前面板数据，根据模式选择强调色（冒险用橙色，锦标赛用蓝色）
            storyIntroPanelIndex = Mathf.Clamp(storyIntroPanelIndex, 0, panels.Length - 1);
            var panel = panels[storyIntroPanelIndex];
            var isAdventure = storyIntroMode == mlpSinglePlayerNarrativeMode.Adventure;
            var accentColor = isAdventure
                ? new Color32(0xFF, 0xA6, 0x39, 0xFF)
                : new Color32(0x78, 0xE7, 0xFF, 0xFF);
            storyIntroAccentColor = accentColor;
            var backgroundFrame = isAdventure ? "bg10000" : "bg2blue0000";

            // 3. 初始化菜单界面，隐藏音乐和帮助按钮（漫画模式不需要）
            currentScreen = mlpBootstrapScreen.StoryIntro;
            BeginMenuScreen(false, false, backgroundFrame);
            menuMusicButton?.SetVisible(false);
            menuHelpButton?.SetVisible(false);
            storyIntroElapsed = 0f;
            // 3. 重置故事介绍相关的暂停和传说面板状态
            storyIntroPauseButton = null;
            storyIntroLoreButton = null;
            storyIntroLoreOpen = false;
            storyIntroPauseBeforeLore = false;
            storyIntroLoreRoot = null;
            storyIntroLoreArtRoot = null;
            storyIntroLoreLabelObject = null;
            storyIntroLoreIconRenderer = null;
            storyIntroLoreTextObjects.Clear();
            storyIntroImageObject = null;
            storyIntroImageRenderer = null;
            storyIntroImageBaseScale = Vector3.one;

            if (!CreateStoryIntroComicImage(panel))
            {
                CreateStoryIntroFallbackPanel(panel, accentColor);
            }

            CreateMenuText(
                "StoryIntroTitle",
                mode.MenuTitle,
                mlpConstants.Width2,
                StoryIntroTitleY,
                24,
                accentColor,
                TextAnchor.MiddleCenter,
                24,
                mlpTextStyle.TournamentAccent);
            CreateMenuText(
                "StoryIntroCaption",
                panel.Caption,
                StoryPanelX,
                StoryIntroCaptionY,
                14,
                Color.white,
                TextAnchor.MiddleCenter,
                24,
                mlpTextStyle.TournamentBody);
            CreateStoryIntroLorePanel(panel);

            storyIntroPauseButton = new mlpMenuButton(
                "pause",
                StoryIntroPauseX,
                StoryIntroPauseY,
                86f,
                26f,
                ToggleStoryIntroPause,
                runtimeRoot,
                26,
                mlpTextStyle.LinkLabel);
            storyIntroPauseButton.SetBackgroundVisible(false);
            menuButtons.Add(storyIntroPauseButton);
            // 7. 更新暂停按钮文字
            RefreshStoryIntroPauseButtonLabel();
            CreatePanel("StoryIntroPauseUnderline", StoryIntroPauseX, StoryIntroPauseY + 13f, 56f, 2f, 24, accentColor);

            var skipButton = new mlpMenuButton(
                "skip",
                StoryIntroSkipX,
                StoryIntroSkipY,
                72f,
                26f,
                ContinueSinglePlayerStoryIntro,
                runtimeRoot,
                26,
                mlpTextStyle.LinkLabel);
            skipButton.SetBackgroundVisible(false);
            menuButtons.Add(skipButton);
            CreatePanel("StoryIntroSkipUnderline", StoryIntroSkipX, StoryIntroSkipY + 13f, 31f, 2f, 24, accentColor);
        }

        // 尝试从 Resources 加载漫画贴图并显示，加载失败返回 false
        private bool CreateStoryIntroComicImage(mlpStoryPanelDefinition panel)
        {
            // 1. 验证面板数据和图片键是否有效
            if (panel == null || string.IsNullOrEmpty(panel.ImageKey))
            {
                return false;
            }

            // 2. 从 Resources 加载漫画贴图，加载失败返回 false
            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(panel.ImageKey));
            if (texture == null)
            {
                return false;
            }

            // 3. 设置贴图模式，防止边缘出现接缝
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            // 4. 在屏幕中心创建漫画图片对象
            storyIntroImageObject = mlpRender.Image(
                "StoryIntroComicPage",
                texture,
                mlpConstants.Width2,
                StoryCinematicHeight * 0.5f,
                0.5f,
                0.5f,
                12,
                runtimeRoot);

            // 5. 计算缩放比例使图片覆盖整个电影区域
            var coverScale = Mathf.Max(
                StoryCinematicWidth / Mathf.Max(1f, texture.width),
                StoryCinematicHeight / Mathf.Max(1f, texture.height));
            storyIntroImageBaseScale = Vector3.one * mlpConstants.UnitsPerPixel * coverScale;
            storyIntroImageBaseScale.z = 1f;
            // 6. 获取渲染器并将初始透明度设为 0（后续会渐入显示）
            storyIntroImageRenderer = storyIntroImageObject.GetComponent<SpriteRenderer>();
            if (storyIntroImageRenderer != null)
            {
                storyIntroImageRenderer.color = new Color(1f, 1f, 1f, 0f);
            }

            // 7. 设置初始位置和缩放
            SetStoryIntroImageTransform(0f, 1f);
            return true;
        }

        // 漫画贴图加载失败时，用纯色面板和文字作为替代显示
        private void CreateStoryIntroFallbackPanel(mlpStoryPanelDefinition panel, Color accentColor)
        {
            CreatePanel("StoryIntroBackdrop", StoryPanelX, StoryPanelY, StoryPanelWidth, StoryPanelHeight, 8, new Color(0.03f, 0.05f, 0.08f, 0.86f));
            CreatePanel("StoryIntroImageSlot", StoryPanelX, 214f, StoryPanelWidth - 72f, 150f, 9, new Color(0.08f, 0.12f, 0.16f, 0.94f));
            CreatePanel("StoryIntroImageAccent", StoryPanelX, 139f, StoryPanelWidth - 92f, 8f, 10, accentColor);
            CreateMenuText(
                "StoryIntroArtDirectionFallback",
                panel != null ? panel.ArtDirection : "Comic page art is loading.",
                StoryPanelX,
                244f,
                12,
                new Color32(0xC8, 0xDD, 0xE8, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
        }

        // 每帧更新漫画的电影式播放效果（平移、缩放、淡入），到时间自动翻页。
        // 由 Update() 在 currentScreen == StoryIntro 时调用。
        // 返回 true 表示本页已播完并触发了翻页（调用方应跳过后续菜单逻辑），false 表示仍在播放中或已暂停。
        private bool UpdateStoryIntroCinematic(float deltaTime)
        {
            // 1. 暂停状态下不更新计时器，画面静止在当前帧。
            //    用户点击 pause 按钮或打开传说面板时 storyIntroPaused 会被设为 true。
            if (storyIntroPaused)
            {
                return false;
            }

            // 2. 累加经过时间（Mathf.Max 防御负值 deltaTime，例如窗口失焦恢复时）。
            storyIntroElapsed += Mathf.Max(0f, deltaTime);

            //    normalized：当前页的播放进度，0→1 线性映射，受 StoryCinematicPageSeconds（3.0s）控制。
            var normalized = Mathf.Clamp01(storyIntroElapsed / StoryCinematicPageSeconds);

            //    eased：对 normalized 做 SmoothStep 缓动（先慢后慢、中间快），用于平移和缩放，
            //    使 Ken Burns 效果的起止更柔和，避免突然启停。
            var eased = Mathf.SmoothStep(0f, 1f, normalized);

            //    fade：淡入进度，用更短的 StoryCinematicFadeSeconds（0.42s）让图片快速出现，
            //    而非等完整 3 秒才完全不透明。
            var fade = Mathf.Clamp01(storyIntroElapsed / StoryCinematicFadeSeconds);

            // 3. 更新漫画图片的视觉表现。
            if (storyIntroImageObject != null)
            {
                //    位置（平移）和缩放（缓慢放大）由 eased 驱动：
                //    - 平移方向由页码奇偶决定（偶数向右、奇数向左），幅度 ±StoryCinematicPanPixels(14px)
                //    - 缩放从 1.0 缓慢增长到 1.0 + StoryCinematicZoomAmount(0.035)，即放大 3.5%
                //    两者共同营造 Ken Burns 纪录片式的镜头缓慢推拉感。
                SetStoryIntroImageTransform(eased, 1f + StoryCinematicZoomAmount * eased);

                //    透明度由 fade 驱动 SmoothStep 淡入：前 0.42 秒从全透明平滑过渡到完全不透明，
                //    避免翻页时图片硬切出现。
                if (storyIntroImageRenderer != null)
                {
                    storyIntroImageRenderer.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, fade));
                }
            }

            // 4. 停留时间耗尽（3.0 秒），自动翻到下一页。
            //    AdvanceSinglePlayerStoryIntro() 会判断：还有后续页则显示下一页，
            //    已经是最后一页则执行完成回调（进入冒险地图或锦标赛设置）。
            if (storyIntroElapsed >= StoryCinematicPageSeconds)
            {
                AdvanceSinglePlayerStoryIntro();
                return true;  // 告诉调用方已翻页，本帧跳过菜单按钮更新
            }

            return false;  // 还在播放中，调用方继续执行菜单按钮交互逻辑
        }

        // 切换漫画的暂停/继续状态
        private void ToggleStoryIntroPause()
        {
            if (storyIntroLoreOpen)
            {
                SetStoryIntroLoreVisibility(false);
                return;
            }

            storyIntroPaused = !storyIntroPaused;
            RefreshStoryIntroPauseButtonLabel();
        }

        // 设置漫画图片的位置（左右平移+上下浮动）和缩放
        private void SetStoryIntroImageTransform(float normalized, float zoom)
        {
            // 1. 没有图片对象则跳过
            if (storyIntroImageObject == null)
            {
                return;
            }

            // 2. 偶数帧向右平移，奇数帧向左平移（营造交替运动感）
            var direction = storyIntroPanelIndex % 2 == 0 ? 1f : -1f;
            var panX = Mathf.Lerp(-StoryCinematicPanPixels, StoryCinematicPanPixels, normalized) * direction;
            // 3. 垂直方向用正弦波做轻微上下浮动
            var panY = Mathf.Sin(normalized * Mathf.PI) * 4f;
            // 4. 设置图片的世界坐标位置（吸附到像素网格）
            storyIntroImageObject.transform.position = mlpConstants.PixelToWorldSnapped(
                mlpConstants.Width2 + panX,
                StoryCinematicHeight * 0.5f + panY,
                0f);
            // 5. 应用缩放（基础缩放 x 放大倍率），保持 Z 轴为 1
            storyIntroImageObject.transform.localScale = storyIntroImageBaseScale * zoom;
            storyIntroImageObject.transform.localScale = new Vector3(
                storyIntroImageObject.transform.localScale.x,
                storyIntroImageObject.transform.localScale.y,
                1f);
        }

        // 漫画翻到上一页，已经是第一页则取消漫画
        private void BackSinglePlayerStoryIntro()
        {
            if (storyIntroPanelIndex > 0)
            {
                storyIntroPanelIndex--;
                ShowSinglePlayerStoryIntroPanel();
                return;
            }

            CancelSinglePlayerStoryIntro();
        }

        // 漫画翻到下一页，已经是最后一页则继续后续流程
        private void AdvanceSinglePlayerStoryIntro()
        {
            var mode = mlpSinglePlayerNarrative.GetMode(storyIntroMode);
            var panelCount = mode.OpeningComic != null ? mode.OpeningComic.Length : 0;
            if (storyIntroPanelIndex < panelCount - 1)
            {
                storyIntroPanelIndex++;
                ShowSinglePlayerStoryIntroPanel();
                return;
            }

            ContinueSinglePlayerStoryIntro();
        }

        // 漫画播完后执行后续回调（进入冒险地图或锦标赛设置）
        private void ContinueSinglePlayerStoryIntro()
        {
            var continueAction = storyIntroContinueAction;
            storyIntroContinueAction = null;
            storyIntroCancelAction = null;
            if (continueAction != null)
            {
                continueAction();
                return;
            }

            ShowMatchTypeMenu();
        }

        // 取消漫画播放，执行取消回调（默认回到模式选择）
        private void CancelSinglePlayerStoryIntro()
        {
            var cancelAction = storyIntroCancelAction;
            storyIntroContinueAction = null;
            storyIntroCancelAction = null;
            if (cancelAction != null)
            {
                cancelAction();
                return;
            }

            ShowMatchTypeMenu();
        }

        // 重播冒险模式的开场漫画
        private void ShowAdventureComicReplay()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Adventure, ShowAdventureMap, ShowAdventureMap);
        }

        // 重播锦标赛的开场漫画（从设置界面触发）
        private void ShowTournamentSetupComicReplay()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Tournament, ShowTournamentSetup, ShowTournamentSetup);
        }

        // 重播锦标赛的开场漫画（从对阵图界面触发）
        private void ShowTournamentBracketComicReplay()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Tournament, ShowTournamentBracket, ShowTournamentBracket);
        }

        
        
        //冒险预告页
        private void ShowAdventurePreview()
        {
            // 1. 设置当前界面为冒险预览，设为单人模式
            currentScreen = mlpBootstrapScreen.AdventurePreview;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            // 2. 初始化菜单界面，显示冒险模式标题和副标题
            BeginMenuScreen(false, false, "bg10000");
            AddTitle(mlpSinglePlayerNarrative.Adventure.MenuTitle, 54f, 30, new Color32(0xFF, 0xB6, 0x45, 0xFF));
            AddSubtitle(mlpSinglePlayerNarrative.Adventure.Subtitle, 90f, 14);

            // 3. 创建信息面板，显示冒险模式的状态、目标和说明文字
            CreatePanel("AdventurePreviewPanel", mlpConstants.Width2, 274f, 590f, 276f, 8, new Color(0.04f, 0.07f, 0.1f, 0.84f));
            CreatePanel("AdventurePreviewAccent", mlpConstants.Width2, 149f, 520f, 8f, 9, new Color(1f, 0.55f, 0.18f, 0.92f));
            CreateMenuText(
                "AdventurePreviewStatus",
                mlpSinglePlayerNarrative.AdventurePreviewStatus,
                mlpConstants.Width2,
                188f,
                18,
                new Color32(0xFF, 0xCF, 0x75, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);
            CreateMenuText(
                "AdventurePreviewObjective",
                "Goal: win 1v1 duels, collect Lantern Sigils, and escape before dawn.",
                mlpConstants.Width2,
                236f,
                13,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
            CreateMenuText(
                "AdventurePreviewLoop",
                "Next build step: park map, locked routes, and the first Warden gate.",
                mlpConstants.Width2,
                276f,
                13,
                new Color32(0xCC, 0xE5, 0xF0, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
            CreateMenuText(
                "AdventurePreviewSafeRoute",
                "Use Quick Duel only as a temporary 1v1 practice path until Adventure flow lands.",
                mlpConstants.Width2,
                316f,
                12,
                new Color32(0xFF, 0xD8, 0x9E, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);

            // 4. 创建底部按钮：返回和快速决斗
            menuButtons.Add(new mlpMenuButton("BACK", 220f, 452f, 180f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("QUICK DUEL", 580f, 452f, 200f, 42f, ShowSinglePlayerSetup, runtimeRoot));
        }

        // ═══════════════════════════════════════════════════════════════
        // 第 6 块：冒险模式
        // 藏宝地图、关卡节点、路线连接、关卡海报、难度选择和结算界面
        // ═══════════════════════════════════════════════════════════════

        // 显示冒险模式的藏宝地图界面（关卡节点、路线、海报、难度选择）
        private void ShowAdventureMap()
        {
            // 1. 设置当前界面为冒险地图
            currentScreen = mlpBootstrapScreen.AdventureMap;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(pendingParticipantMode);

            // 2. 如果冒险未开始或角色更换了，重新开始冒险
            var selectedAdventureCharacterId = mlpPlayersData.SanitizeCharacterId(quickCharacterId);
            if (!inventory.IsAdventureActive || inventory.Adventure.PlayerCharacterId != selectedAdventureCharacterId)
            {
                inventory.BeginAdventure(selectedAdventureCharacterId);
                adventureSelectedLevelIndex = inventory.Adventure.CurrentLevelIndex;
            }

            // 3. 确保选中的关卡索引有效（已完成时显示最后结算关，否则显示当前可玩关）
            var adventure = inventory.Adventure;
            if (adventure.Completed)
            {
                adventureSelectedLevelIndex = Mathf.Max(0, adventure.LastResolvedLevelIndex);
            }
            else if (!adventure.IsLevelUnlocked(adventureSelectedLevelIndex))
            {
                adventureSelectedLevelIndex = adventure.CurrentLevelIndex;
            }

            // 4. 初始化菜单，绘制藏宝图边框、路线图、机制说明、关卡海报
            BeginMenuScreen(false, false, "bg10000");
            CreateAdventureTreasureMapFrame();
            CreateAdventureRouteMap(adventure);
            CreateAdventureMechanicInfo(adventureSelectedLevelIndex);
            CreateAdventureLevelPoster(adventure, adventureSelectedLevelIndex);
            if (adventure.Completed)
            {
                menuButtons.Add(new mlpMenuButton("MAIN MENU", 250f, 452f, 176f, 42f, ShowPlayerCountMenu, runtimeRoot));
                menuButtons.Add(new mlpMenuButton(mlpSinglePlayerNarrative.ComicReplayButton, 550f, 452f, 176f, 42f, ShowAdventureComicReplay, runtimeRoot));
                return;
            }

            menuButtons.Add(new mlpMenuButton("BACK", 124f, 452f, 132f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new mlpMenuButton(mlpSinglePlayerNarrative.ComicReplayButton, 400f, 452f, 160f, 42f, ShowAdventureComicReplay, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("PLAY LEVEL", 656f, 452f, 188f, 42f, StartAdventureLevelFlow, runtimeRoot));
        }

        // 绘制藏宝地图的外框（阴影、铜色边框、地图贴图、印章文字）
        private void CreateAdventureTreasureMapFrame()
        {
            // 1. 绘制地图面板的阴影层（营造立体感）
            CreatePanel(
                "AdventureMapDropShadow",
                AdventureMapPanelX + 7f,
                AdventureMapPanelY + 10f,
                AdventureMapPanelWidth + 30f,
                AdventureMapPanelHeight + 28f,
                7,
                new Color(0f, 0f, 0f, 0.3f));
            // 2. 绘制铜色边框
            CreatePanel(
                "AdventureMapCopperFrame",
                AdventureMapPanelX,
                AdventureMapPanelY,
                AdventureMapPanelWidth + 18f,
                AdventureMapPanelHeight + 18f,
                8,
                new Color(0.5f, 0.22f, 0.08f, 0.76f));

            // 3. 加载藏宝图贴图并显示，贴图不可用时用纯色面板替代
            var texture = GetAdventureTreasureMapTexture();
            if (texture != null)
            {
                var map = mlpRender.Image(
                    "AdventureTreasureMap",
                    texture,
                    AdventureMapPanelX,
                    AdventureMapPanelY,
                    0.5f,
                    0.5f,
                    9,
                    runtimeRoot);
                map.transform.localScale = new Vector3(
                    mlpConstants.UnitsPerPixel * AdventureMapPanelWidth / Mathf.Max(1f, texture.width),
                    mlpConstants.UnitsPerPixel * AdventureMapPanelHeight / Mathf.Max(1f, texture.height),
                    1f);
            }
            else
            {
                CreatePanel(
                    "AdventureTreasureMapFallback",
                    AdventureMapPanelX,
                    AdventureMapPanelY,
                    AdventureMapPanelWidth,
                    AdventureMapPanelHeight,
                    9,
                    new Color(0.64f, 0.42f, 0.2f, 0.94f));
            }

            // 4. 添加可读性遮罩和内部阴影纹理
            CreatePanel("AdventureMapReadabilityWash", AdventureMapPanelX - 28f, AdventureMapPanelY + 10f, AdventureMapPanelWidth - 168f, AdventureMapPanelHeight - 150f, 10, new Color(1f, 0.88f, 0.56f, 0.055f));
            CreatePanel("AdventureMapInnerShade", AdventureMapPanelX, AdventureMapPanelY + AdventureMapPanelHeight * 0.5f - 18f, AdventureMapPanelWidth - 68f, 7f, 10, new Color(0.13f, 0.06f, 0.02f, 0.24f));

            // 5. 在地图左下角显示 "ESCAPE ROUTE" 印章文字
            CreateMenuText(
                "AdventureMapStamp",
                "ESCAPE ROUTE",
                AdventureMapPanelX - AdventureMapPanelWidth * 0.5f + 92f,
                AdventureMapPanelY - AdventureMapPanelHeight * 0.5f + 30f,
                12,
                new Color32(0x39, 0x1E, 0x0B, 0xFF),
                TextAnchor.MiddleCenter,
                18,
                mlpTextStyle.TournamentAccent);
        }

        // 在地图底部显示当前关卡的机制说明文字（如"Warden duels"）
        private void CreateAdventureMechanicInfo(int selectedLevelIndex)
        {
            var level = mlpAdventureCatalog.GetLevel(selectedLevelIndex);
            var mechanicLine = $"{level.MechanicTitle} - {level.MechanicSummary}";
            CreatePanel(
                "AdventureMechanicInfoShadow",
                AdventureMapPanelX + 2f,
                AdventureMechanicInfoY + 3f,
                AdventureMechanicInfoWidth,
                AdventureMechanicInfoHeight,
                20,
                new Color(0.02f, 0.01f, 0f, 0.34f));
            CreatePanel(
                "AdventureMechanicInfoPanel",
                AdventureMapPanelX,
                AdventureMechanicInfoY,
                AdventureMechanicInfoWidth,
                AdventureMechanicInfoHeight,
                21,
                new Color(0.11f, 0.05f, 0.02f, 0.62f));
            CreateMenuText(
                "AdventureMechanicInfoText",
                mechanicLine,
                AdventureMapPanelX,
                AdventureMechanicInfoY,
                GetCompactFontSize(mechanicLine, 12, 11, 10),
                new Color32(0xFF, 0xE2, 0xA0, 0xFF),
                TextAnchor.MiddleCenter,
                22,
                mlpTextStyle.TournamentBody);
        }

        // 获取藏宝地图贴图：优先用资源文件，没有则运行时程序化生成一张旧纸纹理
        private Texture2D GetAdventureTreasureMapTexture()
        {
            var assetTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.AdventureTreasureMapBg));
            if (assetTexture != null)
            {
                return assetTexture;
            }

            if (adventureTreasureMapTexture != null)
            {
                return adventureTreasureMapTexture;
            }

            const int width = 512;
            const int height = 304;
            var pixels = new Color32[width * height];
            var darkPaper = new Color(0.48f, 0.26f, 0.1f, 1f);
            var midPaper = new Color(0.72f, 0.48f, 0.22f, 1f);
            var lightPaper = new Color(0.94f, 0.74f, 0.42f, 1f);
            var burnColor = new Color(0.12f, 0.04f, 0.01f, 1f);
            var stainColor = new Color(0.25f, 0.1f, 0.02f, 1f);

            for (var y = 0; y < height; y++)
            {
                var ny = y / (float)(height - 1);
                for (var x = 0; x < width; x++)
                {
                    var nx = x / (float)(width - 1);
                    var grainA = Mathf.PerlinNoise(nx * 10.7f + 1.4f, ny * 8.9f + 6.2f);
                    var grainB = Mathf.PerlinNoise(nx * 41.5f + 2.8f, ny * 37.6f + 4.1f);
                    var grain = (grainA - 0.5f) * 0.18f + (grainB - 0.5f) * 0.08f;
                    var paperMix = Mathf.Clamp01(0.62f + grain);
                    var color = Color.Lerp(midPaper, lightPaper, paperMix);

                    var centerDx = nx - 0.5f;
                    var centerDy = ny - 0.5f;
                    var vignette = Mathf.Clamp01((centerDx * centerDx + centerDy * centerDy) * 1.35f);
                    color = Color.Lerp(color, darkPaper, vignette * 0.18f);

                    var edgePixels = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    var edge01 = Mathf.Clamp01(edgePixels / 35f);
                    color = Color.Lerp(burnColor, color, edge01);

                    var foldV = 1f - Mathf.Clamp01(Mathf.Abs(nx - 0.47f) / 0.018f);
                    var foldH = 1f - Mathf.Clamp01(Mathf.Abs(ny - 0.53f) / 0.014f);
                    color = Color.Lerp(color, lightPaper, foldV * 0.06f);
                    color = Color.Lerp(color, stainColor, foldH * 0.05f);

                    var stainA = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(nx, ny), new Vector2(0.18f, 0.75f)) / 0.2f);
                    var stainB = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(nx, ny), new Vector2(0.78f, 0.24f)) / 0.18f);
                    var moonWash = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(nx, ny), new Vector2(0.72f, 0.2f)) / 0.3f);
                    var contourNoise = Mathf.PerlinNoise(nx * 18.5f + 9.4f, ny * 16.8f + 3.6f);
                    var contourInk = 1f - Mathf.Clamp01(Mathf.Abs(contourNoise - 0.5f) / 0.035f);
                    color = Color.Lerp(color, stainColor, Mathf.SmoothStep(0f, 1f, stainA) * 0.12f);
                    color = Color.Lerp(color, darkPaper, Mathf.SmoothStep(0f, 1f, stainB) * 0.1f);
                    color = Color.Lerp(color, lightPaper, moonWash * 0.08f);
                    color = Color.Lerp(color, stainColor, contourInk * 0.018f * edge01);

                    pixels[y * width + x] = color;
                }
            }

            adventureTreasureMapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "AdventureTreasureMapRuntime",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            adventureTreasureMapTexture.SetPixels32(pixels);
            adventureTreasureMapTexture.Apply(false, true);
            return adventureTreasureMapTexture;
        }

        // 创建冒险模式的路线节点和连接线（画出关卡之间的路线）
        private void CreateAdventureRouteMap(mlpAdventureData adventure)
        {
            // 1. 绘制相邻节点之间的连接线（路线）
            var levels = mlpAdventureCatalog.AllLevels;
            for (var i = 1; i < levels.Length; i++)
            {
                var unlocked = adventure.IsLevelUnlocked(i);
                var previousAnchor = GetAdventureNodeRouteAnchor(i - 1);
                var currentAnchor = GetAdventureNodeRouteAnchor(i);
                CreateAdventureConnector(i, previousAnchor.x, previousAnchor.y, currentAnchor.x, currentAnchor.y, unlocked);
            }

            // 2. 绘制每个节点（包含头像、状态颜色等），并为已解锁节点添加点击按钮
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                var unlocked = adventure.IsLevelUnlocked(i);
                var completed = adventure.IsLevelCompleted(i);
                var selected = i == adventureSelectedLevelIndex;
                CreateAdventureRouteNode(level, i, unlocked, completed, selected);

                // 3. 已解锁且冒险未完成的节点：创建不可见的点击按钮
                if (unlocked && !adventure.Completed)
                {
                    var capturedIndex = i;
                    var nodePosition = GetAdventureNodePosition(i);
                    var nodeButton = new mlpMenuButton($"LV{i + 1:00}", nodePosition.x, nodePosition.y, AdventureNodeWidth + 8f, AdventureNodeHeight + 8f, () =>
                    {
                        adventureSelectedLevelIndex = capturedIndex;
                        ShowAdventureMap();
                    }, runtimeRoot);
                    nodeButton.SetBackgroundVisible(false);
                    nodeButton.SetLabelVisible(false);
                    menuButtons.Add(nodeButton);
                }
            }
        }

        // 绘制单个冒险关卡节点（头像、边框、发光、锁定遮罩）
        private void CreateAdventureRouteNode(
            mlpAdventureLevelDefinition level,
            int index,
            bool unlocked,
            bool completed,
            bool selected)
        {
            // 1. 获取节点位置，根据状态（完成/解锁/锁定）选择对应的颜色
            var nodePosition = GetAdventureNodePosition(index);
            var portraitY = nodePosition.y - 2f;
            var glowColor = completed
                ? new Color(0.46f, 0.95f, 0.54f, 0.3f)
                : unlocked
                    ? new Color(1f, 0.68f, 0.24f, 0.34f)
                    : new Color(0.18f, 0.12f, 0.08f, 0.22f);
            var borderTint = completed
                ? new Color(0.79f, 0.97f, 0.82f, 0.9f)
                : unlocked
                    ? new Color(0.98f, 0.84f, 0.56f, 0.94f)
                    : new Color(0.62f, 0.58f, 0.52f, 0.82f);
            var fillTint = completed
                ? new Color(0.9f, 0.97f, 0.86f, 0.88f)
                : unlocked
                    ? new Color(0.97f, 0.86f, 0.66f, 0.92f)
                    : new Color(0.6f, 0.56f, 0.48f, 0.86f);
            var portraitBackTint = unlocked
                ? new Color(0.98f, 0.95f, 0.86f, 0.98f)
                : new Color(0.66f, 0.62f, 0.56f, 0.96f);

            // 2. 绘制节点阴影
            CreatePanel($"AdventureNodeShadow_{index}", nodePosition.x + 4f, nodePosition.y + 7f, AdventureNodeWidth + 10f, AdventureNodeHeight + 12f, 13, new Color(0.04f, 0.02f, 0.01f, 0.34f));
            // 3. 选中的节点绘制高亮光晕
            if (selected)
            {
                CreatePanel($"AdventureNodeSelectGlow_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth + 24f, AdventureNodeHeight + 24f, 14, new Color(1f, 0.8f, 0.28f, 0.28f));
            }

            // 4. 从外到内绘制：外框、内部填充、头像背景、头像光晕
            CreatePanel($"AdventureNodePlate_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth, AdventureNodeHeight, 15, borderTint);
            CreatePanel($"AdventureNodeInset_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth - 8f, AdventureNodeHeight - 8f, 16, fillTint);
            CreatePanel($"AdventureNodePortraitBack_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 22f, AdventureNodeWidth - 22f, 17, portraitBackTint);
            CreatePanel($"AdventureNodePortraitGlow_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 12f, AdventureNodeWidth - 12f, 17, glowColor);
            // 5. 绘制守卫者角色头像
            CreateTournamentPortrait(
                $"AdventureNodePortrait_{index}",
                level.WardenCharacterId,
                nodePosition.x,
                portraitY,
                selected ? 50f : 46f,
                18);
            // 6. 未解锁的节点叠加暗色遮罩
            if (!unlocked)
            {
                CreatePanel($"AdventureNodeLockShade_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 22f, AdventureNodeWidth - 22f, 19, new Color(0.08f, 0.09f, 0.11f, 0.42f));
            }
        }

        // 获取冒险关卡节点在地图上的坐标位置
        private static Vector2 GetAdventureNodePosition(int index)
        {
            if (index < 0 || index >= mlpAdventureCatalog.LevelCount)
            {
                return new Vector2(AdventureMapPanelX, AdventureMapPanelY);
            }

            var level = mlpAdventureCatalog.GetLevel(index);
            return new Vector2(level.MapX, level.MapY);
        }

        // 获取冒险节点的路线连接锚点（略微下移，视觉上更自然）
        private static Vector2 GetAdventureNodeRouteAnchor(int index)
        {
            var nodePosition = GetAdventureNodePosition(index);
            return new Vector2(nodePosition.x, nodePosition.y - 2f);
        }

        // 绘制两个冒险节点之间的连接线（阴影+路线+圆点）
        private void CreateAdventureConnector(int index, float startX, float startY, float endX, float endY, bool unlocked)
        {
            CreateAdventureTrailSegment($"AdventureTrailShadow_{index}", startX, startY, endX, endY, 14f, new Color(0.1f, 0.05f, 0.02f, 0.28f), 10);
            CreateAdventureTrailSegment(
                $"AdventureTrail_{index}",
                startX,
                startY,
                endX,
                endY,
                unlocked ? 8f : 6f,
                unlocked ? new Color(0.84f, 0.38f, 0.14f, 0.76f) : new Color(0.34f, 0.24f, 0.14f, 0.4f),
                11);
            CreateAdventureTrailDots(index, startX, startY, endX, endY, unlocked);
        }

        // 绘制一段倾斜的矩形线段（用于连接两个节点）
        private void CreateAdventureTrailSegment(string name, float startX, float startY, float endX, float endY, float thickness, Color color, int sortingOrder)
        {
            var dx = endX - startX;
            var dy = endY - startY;
            var length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length <= 1f)
            {
                return;
            }

            var segment = CreatePanel(name, (startX + endX) * 0.5f, (startY + endY) * 0.5f, length, thickness, sortingOrder, color);
            segment.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg);
        }

        // 在连接线上绘制等距圆点装饰
        private void CreateAdventureTrailDots(int index, float startX, float startY, float endX, float endY, bool unlocked)
        {
            var dotColor = unlocked
                ? new Color(1f, 0.88f, 0.54f, 0.7f)
                : new Color(0.26f, 0.18f, 0.12f, 0.32f);
            for (var i = 1; i <= 5; i++)
            {
                var t = i / 6f;
                var x = Mathf.Lerp(startX, endX, t);
                var y = Mathf.Lerp(startY, endY, t);
                CreatePanel($"AdventureTrailDot_{index}_{i}", x, y, unlocked ? 8f : 6f, unlocked ? 8f : 6f, 12, dotColor);
            }
        }

        // 绘制冒险关卡的详情海报（守卫头像、状态标签、球皮肤、难度选择）
        private void CreateAdventureLevelPoster(mlpAdventureData adventure, int selectedLevelIndex)
        {
            // 1. 获取关卡数据，判断解锁、完成和当前关卡状态
            var level = mlpAdventureCatalog.GetLevel(selectedLevelIndex);
            var unlocked = adventure.IsLevelUnlocked(selectedLevelIndex);
            var completed = adventure.IsLevelCompleted(selectedLevelIndex);
            var activeGate = selectedLevelIndex == adventure.CurrentLevelIndex && !completed && unlocked;
            // 2. 创建海报卡片边框（活跃关卡用特殊边框样式）
            CreateFramedPanel(
                "AdventurePosterFrame",
                activeGate ? "MatchBack0002" : "MatchBack0001",
                AdventurePosterX,
                AdventurePosterY,
                AdventurePosterWidth,
                AdventurePosterHeight,
                11,
                unlocked ? new Color(1f, 0.84f, 0.58f, 0.96f) : new Color(0.7f, 0.78f, 0.86f, 0.7f));
            // 3. 绘制海报内部阴影和装饰线条
            CreatePanel("AdventurePosterShade", AdventurePosterX, AdventurePosterY, AdventurePosterWidth - 28f, AdventurePosterHeight - 34f, 12, new Color(0.04f, 0.06f, 0.09f, 0.72f));
            CreatePanel("AdventurePosterAccent", AdventurePosterX, 137f, AdventurePosterWidth - 48f, 4f, 13, unlocked ? new Color(1f, 0.58f, 0.18f, 0.86f) : new Color(0.44f, 0.52f, 0.58f, 0.7f));
            // 4. 显示关卡状态标签（已获得/下一关/开放/锁定）
            CreateMenuText(
                "AdventurePosterStatus",
                completed ? "CLAIMED" : activeGate ? "NEXT GATE" : unlocked ? "OPEN" : "LOCKED",
                AdventurePosterX,
                152f,
                11,
                completed ? new Color32(0x8F, 0xFF, 0x8B, 0xFF) : unlocked ? new Color32(0xD7, 0xF2, 0x4A, 0xFF) : new Color32(0xB6, 0xC1, 0xCC, 0xFF),
                TextAnchor.MiddleCenter,
                13,
                mlpTextStyle.TournamentAccent);
            // 5. 显示区域名称
            CreateMenuText(
                "AdventurePosterArea",
                level.AreaName,
                AdventurePosterX,
                178f,
                GetCompactFontSize(level.AreaName, 15, 12, 10),
                Color.white,
                TextAnchor.MiddleCenter,
                13,
                mlpTextStyle.TournamentBody);
            // 6. 绘制守卫者头像区域（光晕、边框、内部填充）
            CreatePanel("AdventurePosterPortraitGlow", AdventurePosterX, 232f, 112f, 112f, 13, completed ? new Color(0.56f, 0.98f, 0.66f, 0.16f) : unlocked ? new Color(1f, 0.66f, 0.22f, 0.16f) : new Color(0.72f, 0.78f, 0.86f, 0.08f));
            CreatePanel("AdventurePosterPortraitFrame", AdventurePosterX, 232f, 90f, 96f, 14, unlocked ? new Color(0.98f, 0.84f, 0.56f, 0.94f) : new Color(0.68f, 0.72f, 0.78f, 0.84f));
            CreatePanel("AdventurePosterPortraitInset", AdventurePosterX, 232f, 80f, 86f, 15, unlocked ? new Color(0.98f, 0.94f, 0.86f, 0.96f) : new Color(0.7f, 0.72f, 0.74f, 0.9f));
            // 7. 绘制守卫者头像和名称
            CreateTournamentPortrait(
                "AdventurePosterWardenBadge",
                level.WardenCharacterId,
                AdventurePosterX,
                228f,
                70f,
                16);
            var wardenName = mlpPlayersData.GetCharacterName(level.WardenCharacterId);
            CreateMenuText(
                "AdventurePosterWarden",
                wardenName,
                AdventurePosterX,
                292f,
                GetCompactFontSize(wardenName, 14, 12, 11),
                Color.white,
                TextAnchor.MiddleCenter,
                13,
                mlpTextStyle.TournamentBody);
            // 8. 绘制分隔线，然后显示篮球皮肤选择区域
            CreatePanel("AdventurePosterDivider", AdventurePosterX, 306f, AdventurePosterWidth - 68f, 2f, 13, new Color(1f, 0.63f, 0.22f, 0.28f));
            CreateMenuText(
                "AdventurePosterBallHeader",
                "BALL",
                AdventurePosterX,
                324f,
                11,
                unlocked ? new Color32(0xCD, 0xF0, 0x0F, 0xFF) : new Color32(0x9E, 0xAA, 0xB6, 0xFF),
                TextAnchor.MiddleCenter,
                14,
                mlpTextStyle.TournamentAccent);
            CreatePanel(
                "AdventurePosterBallGlow",
                AdventurePosterX,
                346f,
                44f,
                44f,
                13,
                completed ? new Color(0.56f, 0.98f, 0.66f, 0.12f) : unlocked ? new Color(1f, 0.64f, 0.22f, 0.12f) : new Color(0.72f, 0.78f, 0.86f, 0.08f));
            // 9. 显示篮球预览和难度选择按钮
            CreateBallPreview(
                "AdventurePosterBallPreview",
                mlpBallCatalog.PreviewTheme(level.BallSelection),
                AdventurePosterX,
                346f,
                34f,
                14);
            CreateAdventureDifficultySelector();
        }

        // 在冒险海报底部创建难度切换按钮
        private void CreateAdventureDifficultySelector()
        {
            menuButtons.Add(new mlpMenuButton(mlpInventory.Instance.DifficultyLabel, AdventurePosterX, 380f, 154f, 42f, () =>
            {
                mlpInventory.Instance.ToggleDifficulty();
                ShowAdventureMap();
            }, runtimeRoot));
        }

        // 开始冒险关卡：保存选择，初始化关卡数据，进入比赛
        private void StartAdventureLevelFlow()
        {
            // 获取背包/库存单例
            var inventory = mlpInventory.Instance;
            // 将当前选择的角色保存为快捷选择
            inventory.SetQuickSelection(quickCharacterId);
            // 尝试启动冒险关卡（校验关卡是否解锁、角色是否可用等）
            if (!inventory.StartAdventureLevel(adventureSelectedLevelIndex, quickCharacterId))
            {
                // 关卡无法开始，退回冒险地图界面
                ShowAdventureMap();
                return;
            }

            // 关卡启动成功，进入游戏玩法
            StartGameplay();
        }

        // 重试当前冒险关卡（失败后点击 RETRY 按钮触发）
        private void RetryAdventureLevelFlow()
        {
            var inventory = mlpInventory.Instance;
            if (!inventory.RestartAdventureLevel())
            {
                ShowAdventureMap();
                return;
            }

            StartGameplay();
        }

        // 显示冒险关卡的结算界面（守卫台词、胜负结果、继续/重试按钮）
        private void ShowAdventureResult(bool playerWon)

        {
            // 1. 获取刚才结算的关卡数据和结果台词
            var adventure = mlpInventory.Instance.Adventure;
            var resolvedIndex = Mathf.Max(0, adventure.LastResolvedLevelIndex);
            var level = mlpAdventureCatalog.GetLevel(resolvedIndex);
            var resultSpeech = FormatAdventureResultSpeech(level.GetRandomResultLine(playerWon));
            adventureSelectedLevelIndex = playerWon && !adventure.Completed ? adventure.CurrentLevelIndex : resolvedIndex;

            // 2. 根据胜负设置不同背景和标题
            currentScreen = mlpBootstrapScreen.AdventureResult;
            BeginMenuScreen(false, false, playerWon ? "bg10000" : "bg2blue0000");
            var title = playerWon
                ? adventure.Completed ? "PARK GATE OPEN" : "SIGIL CLAIMED"
                : "WARDEN HOLDS";
            AddTitle(title, 54f, 31, playerWon ? new Color32(0xFF, 0xC7, 0x56, 0xFF) : new Color32(0xDB, 0xE4, 0xF1, 0xFF));
            AddSubtitle(level.AreaName, 90f, 14);

            CreatePanel("AdventureResultPanel", mlpConstants.Width2, 266f, 560f, 188f, 8, new Color(0.03f, 0.06f, 0.09f, 0.86f));
            CreatePanel("AdventureResultAccent", mlpConstants.Width2, 177f, 504f, 8f, 9, playerWon ? new Color(1f, 0.65f, 0.24f, 0.92f) : new Color(0.54f, 0.72f, 0.82f, 0.72f));
            CreateTournamentBadge(
                "AdventureResultWarden",
                level.WardenCharacterId,
                246f,
                232f,
                10,
                0.48f,
                0.39f,
                44f,
                playerWon ? new Color(1f, 0.67f, 0.24f, 0.38f) : new Color(0.5f, 0.7f, 0.9f, 0.26f));
            CreateMenuText(
                "AdventureResultWardenName",
                mlpPlayersData.GetCharacterName(level.WardenCharacterId),
                292f,
                232f,
                GetCompactFontSize(mlpPlayersData.GetCharacterName(level.WardenCharacterId), 18, 15, 13),
                Color.white,
                TextAnchor.MiddleLeft,
                10,
                mlpTextStyle.TournamentBody);
            CreatePanel(
                "AdventureResultDialoguePlate",
                mlpConstants.Width2,
                314f,
                458f,
                84f,
                9,
                playerWon ? new Color(0.15f, 0.11f, 0.05f, 0.38f) : new Color(0.08f, 0.11f, 0.15f, 0.42f));
            CreateMenuText(
                "AdventureResultStory",
                resultSpeech,
                mlpConstants.Width2,
                314f,
                GetCompactFontSize(resultSpeech, 15, 14, 12),
                new Color32(0xE8, 0xF1, 0xF7, 0xFF),
                TextAnchor.MiddleCenter,
                10,
                mlpTextStyle.TournamentBody);

            if (playerWon)
            {
                menuButtons.Add(new mlpMenuButton("MAP", 220f, 452f, 180f, 42f, ShowAdventureMap, runtimeRoot));
                menuButtons.Add(new mlpMenuButton(adventure.Completed ? "MAIN MENU" : "CONTINUE", 580f, 452f, 200f, 42f, adventure.Completed ? ShowPlayerCountMenu : ShowAdventureMap, runtimeRoot));
            }
            else
            {
                menuButtons.Add(new mlpMenuButton("MAP", 220f, 452f, 180f, 42f, ShowAdventureMap, runtimeRoot));
                menuButtons.Add(new mlpMenuButton("RETRY", 580f, 452f, 200f, 42f, RetryAdventureLevelFlow, runtimeRoot));
            }
        }

        // 给冒险结果台词加上引号并自动换行
        private static string FormatAdventureResultSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return "\"" + WrapMenuText(text.Trim(), 32) + "\"";
        }

        // 按单词边界自动换行，每行不超过 maxLineLength 个字符
        private static string WrapMenuText(string text, int maxLineLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLineLength)
            {
                return text;
            }

            var words = text.Split(' ');
            var builder = new StringBuilder(text.Length + 8);
            var currentLineLength = 0;

            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (string.IsNullOrEmpty(word))
                {
                    continue;
                }

                if (currentLineLength > 0 && currentLineLength + 1 + word.Length > maxLineLength)
                {
                    builder.Append('\n');
                    builder.Append(word);
                    currentLineLength = word.Length;
                    continue;
                }

                if (currentLineLength > 0)
                {
                    builder.Append(' ');
                    currentLineLength += 1;
                }

                builder.Append(word);
                currentLineLength += word.Length;
            }

            return builder.ToString();
        }

        // 返回冒险区域的简短名称（用于空间有限的 UI）
        private static string ShortAdventureAreaName(string areaName)
        {
            if (string.IsNullOrEmpty(areaName))
            {
                return string.Empty;
            }

            switch (areaName)
            {
                case "PUMPKIN GATEWAY":
                    return "GATEWAY";
                case "CANDY ARCH STREET":
                    return "CANDY ARCH";
                case "LAUGHING MIRROR HOUSE":
                    return "MIRROR";
                case "CANDLE HALL":
                    return "CANDLE";
                case "FOG DOCK":
                    return "FOG DOCK";
                case "BLOOD MOON TERRACE":
                    return "BLOOD MOON";
                case "CLOCKTOWER GRAVEYARD":
                    return "GRAVEYARD";
                case "MOON LANTERN DOME":
                    return "DOME";
                default:
                    return areaName;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 第 7 块：比赛设置界面
        // 快速比赛/训练/双人/锦标赛的赛前设置（角色、球皮肤、难度）
        // ═══════════════════════════════════════════════════════════════

        private void CreateSinglePlayerModeCard(
            string key,
            mlpSinglePlayerModeDefinition mode,
            float centerX,
            Color panelTint,
            Color accentColor,
            string routeLabel,
            string hookLine,
            string objectiveLineOne,
            string objectiveLineTwo,
            string buttonText,
            System.Action action)
        {
            // 1. 绘制卡片背景面板和装饰线条
            CreatePanel(
                $"{key}_ModeCard",
                centerX,
                SinglePlayerModeCardY,
                SinglePlayerModeCardWidth,
                SinglePlayerModeCardHeight,
                8,
                panelTint);
            CreatePanel(
                $"{key}_ModeCardAccent",
                centerX,
                218f,
                SinglePlayerModeCardWidth - 48f,
                3f,
                10,
                new Color(accentColor.r, accentColor.g, accentColor.b, 0.7f));
            // 2. 显示路由标签（如 "ADVENTURE"、"TOURNAMENT"）
            CreateMenuText(
                $"{key}_ModeLabel",
                routeLabel,
                centerX,
                206f,
                13,
                accentColor,
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);
            // 3. 显示模式大标题
            CreateMenuText(
                $"{key}_ModeTitle",
                mode.MenuTitle,
                centerX,
                274f,
                GetCompactFontSize(mode.MenuTitle, 21, 19, 17),
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);
            // 4. 显示副标题（吸引玩家的一句话）
            CreateMenuText(
                $"{key}_ModeSubtitle",
                hookLine,
                centerX,
                309f,
                13,
                new Color32(0xF2, 0xFA, 0xFA, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
            // 5. 显示两行目标说明文字
            CreateMenuText(
                $"{key}_ModeObjectiveOne",
                objectiveLineOne,
                centerX,
                346f,
                12,
                new Color32(0xD9, 0xEA, 0xEF, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
            CreateMenuText(
                $"{key}_ModeObjectiveTwo",
                objectiveLineTwo,
                centerX,
                366f,
                12,
                new Color32(0xD9, 0xEA, 0xEF, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);

            // 6. 在卡片底部添加操作按钮
            menuButtons.Add(new mlpMenuButton(buttonText, centerX, 397f, 194f, 44f, action, runtimeRoot));
        }

        /// <summary>
        /// 显示快速比赛设置界面，玩家在此选择角色、篮球皮肤和难度。
        /// </summary>
        private void ShowSinglePlayerSetup()
        {
            // 1. 设置当前界面为单人快速比赛设置
            currentScreen = mlpBootstrapScreen.SinglePlayerSetup;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("QUICK MATCH", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            // 2. 创建左侧角色选择面板和右侧选项面板
            CreatePanel("CharacterPanel", 220f, 280f, 260f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreatePanel("OptionsPanel", 575f, 278f, 228f, 214f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreateCharacterSelector(
                "Quick",
                "CHARACTER",
                220f,
                quickCharacterId,
                () =>
                {
                    quickCharacterId = WrapCharacter(quickCharacterId, -1);
                    ShowSinglePlayerSetup();
                },
                () =>
                {
                    quickCharacterId = WrapCharacter(quickCharacterId, 1);
                    ShowSinglePlayerSetup();
                },
                306f,
                3.85f,
                398f);

            CreateOptionsPanel("Quick", 575f);
            CreateBallSelector(
                "QuickBall",
                575f,
                quickBallSelection,
                () =>
                {
                    quickBallSelection = mlpBallCatalog.StepSelection(quickBallSelection, -1);
                    ShowSinglePlayerSetup();
                },
                () =>
                {
                    quickBallSelection = mlpBallCatalog.StepSelection(quickBallSelection, 1);
                    ShowSinglePlayerSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new mlpMenuButton(mlpInventory.Instance.DifficultyLabel, 575f, 304f, 188f, 46f, () =>
            {
                mlpInventory.Instance.ToggleDifficulty();
                ShowSinglePlayerSetup();
            }, runtimeRoot));
            if (mlpInventory.Instance.Difficulty == mlpAiDifficulty.Hell)
            {
                CreateHellDifficultyWarning(575f, 346f);
            }

            menuButtons.Add(new mlpMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("PLAY", 660f, 452f, 150f, 42f, StartQuickMatchFlow, runtimeRoot));
        }

        /// <summary>
        /// 显示训练模式设置界面，玩家在此选择角色和篮球皮肤。
        /// </summary>
        private void ShowTrainingSetup()
        {
            // 1. 设置当前界面为训练模式设置
            currentScreen = mlpBootstrapScreen.TrainingSetup;
            pendingParticipantMode = mlpParticipantMode.Training;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg2blue0000");
            AddTitle("TRAINING", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            CreatePanel("TrainingCharacterPanel", 220f, 280f, 260f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreatePanel("TrainingOptionsPanel", 575f, 278f, 228f, 214f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreateCharacterSelector(
                "Training",
                "CHARACTER",
                220f,
                trainingCharacterId,
                () =>
                {
                    trainingCharacterId = WrapCharacter(trainingCharacterId, -1);
                    ShowTrainingSetup();
                },
                () =>
                {
                    trainingCharacterId = WrapCharacter(trainingCharacterId, 1);
                    ShowTrainingSetup();
                },
                306f,
                3.85f,
                398f);

            CreateOptionsPanel("Training", 575f);
            CreateBallSelector(
                "TrainingBall",
                575f,
                trainingBallSelection,
                () =>
                {
                    trainingBallSelection = mlpBallCatalog.StepSelection(trainingBallSelection, -1);
                    ShowTrainingSetup();
                },
                () =>
                {
                    trainingBallSelection = mlpBallCatalog.StepSelection(trainingBallSelection, 1);
                    ShowTrainingSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new mlpMenuButton("BACK", 488f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("PLAY", 660f, 452f, 150f, 42f, StartTrainingFlow, runtimeRoot));
        }

        /// <summary>
        /// 显示双人设置界面，两名玩家分别选择角色并共选篮球皮肤。
        /// </summary>
        private void ShowTwoPlayerSetup()
        {
            // 1. 设置当前界面为双人对战设置
            currentScreen = mlpBootstrapScreen.TwoPlayerSetup;
            pendingParticipantMode = mlpParticipantMode.TwoPlayers;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("2 PLAYERS MATCH", 58f, 30, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            // 2. 创建左右两个角色选择面板（P1 和 P2），中间显示 VS
            CreatePanel("P1Panel", 214f, 286f, 250f, 308f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreatePanel("P2Panel", 586f, 286f, 250f, 308f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));

            CreateCharacterSelector(
                "P1",
                "P1",
                214f,
                versusLeftCharacterId,
                () =>
                {
                    versusLeftCharacterId = WrapCharacter(versusLeftCharacterId, -1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusLeftCharacterId = WrapCharacter(versusLeftCharacterId, 1);
                    ShowTwoPlayerSetup();
                },
                308f,
                3.8f,
                420f);

            mlpRender.Text(
                "VersusLabel",
                "VS",
                mlpConstants.Width2,
                284f,
                34,
                new Color32(0xFF, 0xA3, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                mlpFontKind.CfCrackBold,
                outlineColor: Color.white,
                outlinePixels: 1.4f);

            CreatePanel("VersusBallPanel", mlpConstants.Width2, TwoPlayerBallPanelY, TwoPlayerBallPanelWidth, TwoPlayerBallPanelHeight, 8, new Color(0.05f, 0.08f, 0.1f, 0.82f));
            CreateBallSelector(
                "VersusBall",
                mlpConstants.Width2,
                versusBallSelection,
                () =>
                {
                    versusBallSelection = mlpBallCatalog.StepSelection(versusBallSelection, -1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusBallSelection = mlpBallCatalog.StepSelection(versusBallSelection, 1);
                    ShowTwoPlayerSetup();
                },
                TwoPlayerBallHeaderY,
                TwoPlayerBallPreviewY,
                TwoPlayerBallLabelY);

            CreateCharacterSelector(
                "P2",
                "P2",
                586f,
                versusRightCharacterId,
                () =>
                {
                    versusRightCharacterId = WrapCharacter(versusRightCharacterId, -1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusRightCharacterId = WrapCharacter(versusRightCharacterId, 1);
                    ShowTwoPlayerSetup();
                },
                308f,
                3.8f,
                420f);

            menuButtons.Add(new mlpMenuButton("BACK", 212f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("PLAY", 588f, 452f, 150f, 42f, StartTwoPlayerMatch, runtimeRoot));
        }

        /// <summary>
        /// 显示锦标赛设置界面，玩家在进入对阵图前选择角色。
        /// </summary>
        private void ShowTournamentSetup()
        {
            // 1. 设置当前界面为锦标赛角色选择，保存选择到存档
            currentScreen = mlpBootstrapScreen.TournamentSetup;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            mlpInventory.Instance.SetTournamentSelection(tournamentCharacterId);

            // 2. 初始化菜单，显示锦标赛标题
            BeginMenuScreen(false, false, "bg2blue0000");
            AddTitle(mlpSinglePlayerNarrative.Tournament.MenuTitle, 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            CreatePanel("SelectPanel", 220f, 280f, 260f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreatePanel("OptionsPanel", 575f, 278f, 228f, 214f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreateCharacterSelector(
                "Tournament",
                "CHARACTER",
                220f,
                tournamentCharacterId,
                () =>
                {
                    tournamentCharacterId = WrapCharacter(tournamentCharacterId, -1);
                    ShowTournamentSetup();
                },
                () =>
                {
                    tournamentCharacterId = WrapCharacter(tournamentCharacterId, 1);
                    ShowTournamentSetup();
                },
                306f,
                3.85f,
                398f);

            CreateOptionsPanel("Tournament", 575f);
            CreateMenuText(
                "ModeFixed",
                mlpSinglePlayerNarrative.TournamentFormatLine,
                575f,
                186f,
                12,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
            CreateBallSelector(
                "TournamentBall",
                575f,
                tournamentBallSelection,
                () =>
                {
                    tournamentBallSelection = mlpBallCatalog.StepSelection(tournamentBallSelection, -1);
                    ShowTournamentSetup();
                },
                () =>
                {
                    tournamentBallSelection = mlpBallCatalog.StepSelection(tournamentBallSelection, 1);
                    ShowTournamentSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new mlpMenuButton(mlpInventory.Instance.DifficultyLabel, 575f, 304f, 188f, 46f, () =>
            {
                mlpInventory.Instance.ToggleDifficulty();
                ShowTournamentSetup();
            }, runtimeRoot));
            if (mlpInventory.Instance.Difficulty == mlpAiDifficulty.Hell)
            {
                CreateHellDifficultyWarning(575f, 346f);
            }

            var enoughCharacters = mlpPlayersData.GetActiveCharacterIds().Length >= 8;
            if (!enoughCharacters)
            {
                AddSubtitle("NEED 8 ENABLED CHARACTERS", 408f, 18);
            }

            menuButtons.Add(new mlpMenuButton(mlpSinglePlayerNarrative.ComicReplayButton, 310f, 452f, 170f, 42f, ShowTournamentSetupComicReplay, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            if (enoughCharacters)
            {
                menuButtons.Add(new mlpMenuButton("NEXT", 660f, 452f, 150f, 42f, StartTournamentFlow, runtimeRoot));
            }
        }

        /// <summary>
        // ═══════════════════════════════════════════════════════════════
        // 第 8 块：比赛启动流程
        // 将菜单选择保存到存档，然后调用 StartGameplay 创建比赛场景
        // ═══════════════════════════════════════════════════════════════

        /// 保存快速比赛的选择并启动比赛。
        /// </summary>
        private void StartQuickMatchFlow()
        {
            // 1. 保存玩家的单人模式、角色和篮球皮肤选择
            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(mlpParticipantMode.OnePlayer);
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetQuickBallSelection(quickBallSelection);
            // 2. 初始化快速比赛并进入游戏
            inventory.StartQuickGame();
            StartGameplay();
        }

        /// <summary>
        /// 保存训练选择并启动训练会话。
        /// </summary>
        private void StartTrainingFlow()
        {
            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(mlpParticipantMode.Training);
            inventory.SetTrainingSelection(trainingCharacterId);
            inventory.SetTrainingBallSelection(trainingBallSelection);
            inventory.StartTraining();
            StartGameplay();
        }

        /// <summary>
        /// 保存训练角色选择并启动教程。
        /// </summary>
        private void StartTutorialFlow()
        {
            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(mlpParticipantMode.Tutorial);
            inventory.SetTrainingSelection(trainingCharacterId);
            inventory.SetTrainingBallSelection(trainingBallSelection);
            inventory.StartTutorial();
            StartGameplay();
        }

        // 从帮助面板启动教程（如果有进行中的锦标赛/冒险，先放弃）
        private void StartTutorialFromHelpPanel()
        {
            if (mlpInventory.Instance.IsTournamentActive)
            {
                mlpInventory.Instance.AbandonTournament();
            }
            else if (mlpInventory.Instance.IsAdventureActive)
            {
                mlpInventory.Instance.AbandonAdventure();
            }

            StartTutorialFlow();
        }

        /// <summary>
        /// 保存锦标赛角色选择并开始锦标赛对阵。
        /// </summary>
        private void StartTournamentFlow()
        {
            // 1. 保存角色和篮球皮肤选择，尝试开始锦标赛
            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(mlpParticipantMode.OnePlayer);
            inventory.SetTournamentSelection(tournamentCharacterId);
            inventory.SetTournamentBallSelection(tournamentBallSelection);

            // 2. 开始锦标赛失败（角色不足 8 个）则回到设置界面
            if (!inventory.BeginTournament())
            {
                ShowTournamentSetup();
                return;
            }

            // 3. 成功则显示锦标赛对阵图
            ShowTournamentBracket();
        }

        /// <summary>
        /// 打开键盘操作帮助面板。
        /// </summary>
        private void ShowMenuControlsPanel()
        {
            mlpHelpPanel.ShowKeyboardPage();
        }

        /// <summary>
        /// 检查玩家在教程结束后选择重玩教程、开始训练还是开始快速比赛。
        /// </summary>
        private bool HandlePendingTutorialAction()
        {
            // 1. 读取教程结束后玩家选择的下一步操作
            var inventory = mlpInventory.Instance;
            var action = inventory.PendingTutorialNextAction;
            // 2. 清除待处理标记，防止重复触发
            inventory.PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 3. 根据选择执行对应操作
            switch (action)
            {
                // 4. 重玩教程：重新启动教程流程
                case mlpTutorialNextAction.ReplayTutorial:
                    StartTutorialFlow();
                    return true;
                // 5. 开始训练：进入训练模式
                case mlpTutorialNextAction.StartTraining:
                    StartTrainingFlow();
                    return true;
                // 6. 开始快速比赛：把训练时的选择同步到快速比赛，然后开始
                case mlpTutorialNextAction.StartQuickMatch:
                    quickCharacterId = trainingCharacterId;
                    quickBallSelection = trainingBallSelection;
                    StartQuickMatchFlow();
                    return true;
                // 7. 没有待处理操作：返回 false 表示不需要跳转
                default:
                    return false;
            }
        }

        /// <summary>
        /// 开始锦标赛决赛阶段并显示更新后的对阵图。
        /// </summary>
        private void StartTournamentFinalsFlow()
        {
            mlpInventory.Instance.BeginTournamentFinals();
            ShowTournamentBracket();
        }

        /// <summary>
        /// 保存双人角色选择并启动对战比赛。
        /// </summary>
        private void StartTwoPlayerMatch()
        {
            // 1. 保存篮球皮肤选择，用两名玩家选择的角色开始对战
            mlpInventory.Instance.SetVersusBallSelection(versusBallSelection);
            mlpInventory.Instance.StartTwoPlayerVersus(versusLeftCharacterId, versusRightCharacterId);

            // 2. 清除菜单场景，创建比赛场景
            StartGameplay();
        }

        /// <summary>
        // ═══════════════════════════════════════════════════════════════
        // 第 9 块：锦标赛对阵图和颁奖
        // 常规赛排名表、季后赛对阵图、颁奖典礼动画和积分榜
        // ═══════════════════════════════════════════════════════════════

        /// 显示锦标赛对阵图界面，包含所有当前比赛和排名。
        /// </summary>
        private void ShowTournamentBracket()
        {
            // 1. 获取锦标赛数据，判断当前阶段（常规赛/季后赛/已完成）
            var inventory = mlpInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? mlpBootstrapScreen.TournamentComplete : mlpBootstrapScreen.TournamentBracket;
            var regularSeasonScreen = tournament.CurrentStage == mlpTournamentStage.RegularSeason;
            var titleY = regularSeasonScreen ? 44f : 34f;
            var titleFontSize = regularSeasonScreen ? 32 : 30;
            var subtitleY = regularSeasonScreen ? 72f : 62f;
            var subtitleFontSize = regularSeasonScreen ? 16 : 14;
            var statusText = GetTournamentStatusText(tournament);

            // 2. 根据阶段选择背景色，初始化菜单并显示标题和状态副标题
            var backgroundFrame = tournament.Completed || !regularSeasonScreen
                ? "bg10000"
                : "bg2blue0000";
            BeginMenuScreen(false, false, backgroundFrame);
            AddTitle(
                mlpSinglePlayerNarrative.Tournament.MenuTitle,
                titleY,
                titleFontSize,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF));
            CreateMenuText(
                $"{statusText}_Subtitle",
                statusText,
                mlpConstants.Width2,
                subtitleY,
                subtitleFontSize,
                regularSeasonScreen ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : Color.white,
                TextAnchor.MiddleCenter,
                19,
                mlpTextStyle.Subtitle);

            // 3. 绘制对阵图面板和赛季状态横幅
            CreateTournamentBracketBoard(tournament);
            CreateTournamentSeasonBanner(tournament);

            if (tournament.Completed)
            {
                menuButtons.Add(new mlpMenuButton("MAIN MENU", 156f, 452f, 164f, 42f, () =>
                {
                    mlpInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new mlpMenuButton(mlpSinglePlayerNarrative.ComicReplayButton, 400f, 452f, 176f, 42f, ShowTournamentBracketComicReplay, runtimeRoot));
                menuButtons.Add(new mlpMenuButton("CEREMONY", 640f, 452f, 190f, 42f, ShowTournamentAwards, runtimeRoot));
            }
            else if (tournament.CurrentStage == mlpTournamentStage.RegularSeason)
            {
                menuButtons.Add(new mlpMenuButton("BACK", 142f, 452f, 138f, 42f, () =>
                {
                    mlpInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));

                if (tournament.RegularSeasonCompleted)
                {
                    menuButtons.Add(new mlpMenuButton("START FINALS", 634f, 452f, 190f, 42f, StartTournamentFinalsFlow, runtimeRoot));
                }
                else
                {
                    menuButtons.Add(new mlpMenuButton("PLAY", 634f, 452f, 190f, 42f, StartGameplay, runtimeRoot));
                }
            }
            else
            {
                menuButtons.Add(new mlpMenuButton("BACK", 142f, 452f, 138f, 42f, () =>
                {
                    mlpInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new mlpMenuButton(mlpSinglePlayerNarrative.ComicReplayButton, 400f, 452f, 176f, 42f, ShowTournamentBracketComicReplay, runtimeRoot));
                menuButtons.Add(new mlpMenuButton("PLAY", 640f, 452f, 150f, 42f, StartGameplay, runtimeRoot));
            }
        }

        /// <summary>
        /// 显示赛季末颁奖典礼，包含名次绶带和奖杯。
        /// </summary>
        private void ShowTournamentAwards()
        {
            // 1. 锦标赛未完成时回到对阵图界面
            var tournament = mlpInventory.Instance.Tournament;
            if (!tournament.Completed)
            {
                ShowTournamentBracket();
                return;
            }

            // 2. 初始化颁奖典礼界面，根据玩家名次设置标题颜色
            currentScreen = mlpBootstrapScreen.TournamentAwards;
            BeginMenuScreen(false, false, "bg10000");
            AddTitle(mlpSinglePlayerNarrative.TournamentSeasonCompleteTitle, 52f, 28, GetTournamentAwardsAccentColor(tournament.PlayerPlacement));

            // 3. 创建颁奖场景（领奖台、奖杯、角色骨骼动画）
            CreateTournamentAwardsScene(tournament);

            menuButtons.Add(new mlpMenuButton("BRACKET", 220f, 452f, 180f, 42f, ShowTournamentBracket, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("MAIN MENU", 580f, 452f, 200f, 42f, () =>
            {
                mlpInventory.Instance.AbandonTournament();
                ShowPlayerCountMenu();
            }, runtimeRoot));
        }

        /// <summary>
        // ═══════════════════════════════════════════════════════════════
        // 第 10 块：比赛场景构建和摄像机模式切换
        // StartGameplay 创建比赛，BeginMenuScreen 初始化菜单界面
        // ═══════════════════════════════════════════════════════════════

        /// 清除菜单场景并创建新的比赛游戏对象。
        /// </summary>
        private void StartGameplay()
        {
            // 1. 清除菜单场景的所有 GameObject
            ClearRuntime();

            // 2. 切换摄像机到像素完美的游戏渲染模式
            EnableGameplayPresentation();

            // 3. 创建新的运行时根节点、音频系统，清空按钮列表
            runtimeRoot = new GameObject("mlpRuntime").transform;
            mlpAudio.Create(transform).PlayMusic(mlpAssets.Sounds.MenuMusic);
            // 2. 清空菜单按钮和各类辅助对象引用
            menuButtons.Clear();

            // 4. 用游戏构建器创建比赛场景（球员、篮球、篮筐、球场等）
            gameCore = new mlpGameBuilder().Build(runtimeRoot);
        }

        /// <summary>
        /// 设置全新的菜单界面，包含背景、可选的标志和操作提示。
        /// </summary>
        private void BeginMenuScreen(bool showLogo, bool showControls, string backgroundFrame)
        {
            // 1. 清除旧场景，切换摄像机到原生分辨率菜单模式（文字更清晰）
            ClearRuntime();
            EnableNativeMenuPresentation();

            // 2. 创建运行时根节点、原生文字层、音频系统
            runtimeRoot = new GameObject("mlpRuntime").transform;
            nativeMenuTextLayer = new mlpNativeMenuTextLayer(runtimeRoot);
            nativeMenuTextLayer.RefreshLayout(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            mlpAudio.Create(transform).PlayMusic(mlpAssets.Sounds.MenuMusic);

            // 3. 创建背景图（优先使用独立背景，否则用通用背景）
            if (!TryCreateStandaloneMenuBackground(backgroundFrame))
            {
                mlpRender.Sprite(
                    "MenuBackground",
                    mlpAtlasCache.Instance.Interface,
                    backgroundFrame,
                    mlpConstants.Width2,
                    240f,
                    0.5f,
                    0.5f,
                    0,
                    runtimeRoot);
            }

            // 4. 如果需要，加载并显示游戏 logo（按最大宽高比缩放）
            if (showLogo)
            {
                var logoTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameLogo));
                if (logoTexture != null)
                {
                    var logoScale = Mathf.Min(
                        MenuLogoMaxWidth / Mathf.Max(1f, logoTexture.width),
                        MenuLogoMaxHeight / Mathf.Max(1f, logoTexture.height));
                    var logo = mlpRender.Image("Logo", logoTexture, mlpConstants.Width2, MenuLogoCenterY, 0.5f, 0.5f, 20, runtimeRoot);
                    var logoWorldScale = mlpConstants.UnitsPerPixel * logoScale;
                    logo.transform.localScale = new Vector3(logoWorldScale, logoWorldScale, 1f);
                }
            }

            // 5. 创建右上角的音乐开关和帮助按钮
            menuMusicButton = new mlpIconButton(
                "MenuMusicButton",
                MenuMusicButtonX,
                MenuTopButtonY,
                MenuTopButtonSize,
                MenuTopButtonSize,
                ToggleBackgroundMusic,
                runtimeRoot,
                32,
                MenuTopIconPixels,
                mlpAssets.Images.ResourcePath(mlpAssets.Images.MusicButtonOn),
                mlpAssets.Images.ResourcePath(mlpAssets.Images.MusicButtonOff));
            menuMusicButton.SetActiveIconIndex(GetMusicIconIndex());
            menuHelpButton = new mlpIconButton(
                "MenuHelpButton",
                MenuHelpButtonX,
                MenuTopButtonY,
                MenuTopButtonSize,
                MenuTopButtonSize,
                ShowMenuControlsPanel,
                runtimeRoot,
                32,
                MenuTopIconPixels,
                mlpAssets.Images.ResourcePath(mlpAssets.Images.HelpButton));

            // 6. 如果需要，在底部显示操作提示文字
            if (showControls)
            {
                CreateMenuText(
                    "Controls",
                    mlpControlsData.MainMenuControlsText,
                    mlpConstants.Width2,
                    492f,
                    11,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    30,
                    mlpTextStyle.TournamentBody);
            }

            // 7. 清空按钮列表（各页面会在之后添加自己的按钮）
            menuButtons.Clear();
        }

        /// <summary>
        /// 将摄像机切换到像素完美的游戏模式。
        /// </summary>
        private void EnableGameplayPresentation()
        {
            // 1. 关闭原生 UI 模式，重置视口缓存尺寸
            usingNativeUiPresentation = false;
            viewportScreenWidth = -1;
            viewportScreenHeight = -1;
            // 2. 没有摄像机则直接返回
            if (mainCamera == null)
            {
                return;
            }

            // 3. 设置摄像机为全屏，附加像素完美分辨率适配器
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            fixedResolutionPresenter?.Attach(mainCamera);
        }

        /// <summary>
        /// 将摄像机切换到原生分辨率菜单模式，确保文字清晰。
        /// </summary>
        private void EnableNativeMenuPresentation()
        {
            // 1. 开启原生 UI 模式，移除像素完美适配器
            usingNativeUiPresentation = true;
            fixedResolutionPresenter?.Detach();
            // 2. 强制刷新菜单视口，确保文字清晰
            RefreshNativeMenuViewport(force: true);
        }

        /// <summary>
        /// 在原生菜单模式下，窗口大小变化时重新计算摄像机视口。
        /// </summary>
        private void RefreshNativeMenuViewport(bool force = false)
        {
            // 1. 不在原生 UI 模式或没有摄像机时跳过
            if (!usingNativeUiPresentation || mainCamera == null)
            {
                return;
            }

            // 2. 获取当前屏幕尺寸，如果没变化且非强制刷新则跳过
            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            if (!force && screenWidth == viewportScreenWidth && screenHeight == viewportScreenHeight)
            {
                return;
            }

            // 3. 缓存新尺寸，刷新原生文字层布局
            viewportScreenWidth = screenWidth;
            viewportScreenHeight = screenHeight;
            nativeMenuTextLayer?.RefreshLayout(screenWidth, screenHeight);

            // 4. 计算屏幕宽高比，调整摄像机视口保持目标比例
            var screenAspect = screenWidth / (float)screenHeight;
            // 5. 宽高比刚好匹配：全屏显示
            if (Mathf.Abs(screenAspect - NativeUiAspect) <= 0.0001f)
            {
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            // 6. 屏幕更宽：左右加黑边，居中显示
            if (screenAspect > NativeUiAspect)
            {
                var normalizedWidth = NativeUiAspect / screenAspect;
                mainCamera.rect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
                return;
            }

            // 7. 屏幕更高：上下加黑边，居中显示
            var normalizedHeight = screenAspect / NativeUiAspect;
            mainCamera.rect = new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 11 块：UI 基础组件（文字、面板、按钮创建方法）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 在菜单界面顶部放置大标题标签。
        /// </summary>
        private void AddTitle(string title, float y, int fontSize, Color color)
        {
            CreateMenuText(
                $"{title}_Title",
                title,
                mlpConstants.Width2,
                y,
                fontSize,
                color,
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.DisplayTitle);
        }

        /// <summary>
        /// 在菜单界面标题下方放置较小的副标题标签。
        /// </summary>
        private void AddSubtitle(string subtitle, float y, int fontSize = 18)
        {
            CreateMenuText(
                $"{subtitle}_Subtitle",
                subtitle,
                mlpConstants.Width2,
                y,
                fontSize,
                Color.white,
                TextAnchor.MiddleCenter,
                19,
                mlpTextStyle.Subtitle);
        }

        /// <summary>
        /// 在菜单界面上创建文字标签，优先使用原生文字层。
        /// </summary>
        private void CreateMenuText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            int sortingOrder,
            mlpTextStyle style,
            Transform parent = null)
        {
            // 1. 确定文字的父容器，默认挂在菜单根节点下
            var resolvedParent = parent ?? runtimeRoot;
            // 2. 如果支持原生文字层（TextMeshPro），用原生方式创建，文字更清晰
            if (ShouldUseNativeMenuText(resolvedParent))
            {
                nativeMenuTextLayer?.CreateText(name, text, x, y, fontSize, color, anchor, style);
                return;
            }

            // 3. 否则用旧版 TextMesh 方式创建文字
            mlpRender.Text(name, text, x, y, fontSize, color, anchor, sortingOrder, resolvedParent, style);
        }

        // 创建可在传说面板中切换显示/隐藏的文字对象
        private GameObject CreateToggleableStoryIntroText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            int sortingOrder,
            mlpTextStyle style)
        {
            // 1. 根据可用的渲染方式（原生或旧版）创建文字对象
            GameObject textObject = null;
            if (ShouldUseNativeMenuText(runtimeRoot))
            {
                var nativeText = nativeMenuTextLayer?.CreateText(name, text, x, y, fontSize, color, anchor, style);
                textObject = nativeText != null ? nativeText.gameObject : null;
            }
            else
            {
                var legacyText = mlpRender.Text(name, text, x, y, fontSize, color, anchor, sortingOrder, runtimeRoot, style);
                textObject = legacyText != null ? legacyText.gameObject : null;
            }

            // 2. 把文字加入传说文本列表，并根据传说面板是否打开设置可见性
            if (textObject != null)
            {
                storyIntroLoreTextObjects.Add(textObject);
                textObject.SetActive(storyIntroLoreOpen);
            }

            return textObject;
        }

        // 创建传说面板按钮上的文字标签
        private GameObject CreateStoryIntroLoreButtonLabel(string text, float x, float y)
        {
            // 1. 优先使用原生文字层创建传说按钮标签
            if (ShouldUseNativeMenuText(runtimeRoot))
            {
                var nativeText = nativeMenuTextLayer?.CreateText(
                    "StoryIntroLoreButtonLabel",
                    text,
                    x,
                    y,
                    18,
                    StoryIntroLoreClosedLabelColor,
                    TextAnchor.MiddleCenter,
                    mlpTextStyle.TournamentAccent);
                return nativeText != null ? nativeText.gameObject : null;
            }

            // 2. 原生文字层不可用时，用旧版 TextMesh 创建
            var legacyText = mlpRender.Text(
                "StoryIntroLoreButtonLabel",
                text,
                x,
                y,
                18,
                StoryIntroLoreClosedLabelColor,
                TextAnchor.MiddleCenter,
                56,
                runtimeRoot,
                mlpTextStyle.TournamentAccent);
            return legacyText != null ? legacyText.gameObject : null;
        }

        // 更新传说按钮标签的文字内容（兼容原生TMP和旧版TextMesh）
        private void SetStoryIntroLoreButtonLabelText(string text)
        {
            if (storyIntroLoreLabelObject == null)
            {
                return;
            }

            var nativeText = storyIntroLoreLabelObject.GetComponent<TMPro.TMP_Text>();
            if (nativeText != null)
            {
                nativeText.text = text;
                return;
            }

            var legacyText = storyIntroLoreLabelObject.GetComponent<TextMesh>();
            if (legacyText != null)
            {
                legacyText.text = text;
            }
        }

        // 更新传说按钮标签的文字颜色
        private void SetStoryIntroLoreButtonLabelColor(Color color)
        {
            if (storyIntroLoreLabelObject == null)
            {
                return;
            }

            var nativeText = storyIntroLoreLabelObject.GetComponent<TMPro.TMP_Text>();
            if (nativeText != null)
            {
                nativeText.color = color;
                return;
            }

            var legacyText = storyIntroLoreLabelObject.GetComponent<TextMesh>();
            if (legacyText != null)
            {
                legacyText.color = color;
            }
        }

        // 传说面板打开/关闭时，更新按钮和美术元素的位置
        private void RefreshStoryIntroLoreButtonLayout(bool isVisible)
        {
            // 1. 根据传说面板是否打开，选择对应的位置和偏移值
            var buttonX = isVisible ? StoryIntroLoreOpenButtonX : StoryIntroLoreButtonX;
            var buttonY = isVisible ? StoryIntroLoreOpenButtonY : StoryIntroLoreButtonY;
            var labelOffsetX = isVisible ? StoryIntroLoreOpenLabelOffsetX : StoryIntroLoreLabelOffsetX;
            var labelOffsetY = isVisible ? StoryIntroLoreOpenLabelOffsetY : StoryIntroLoreLabelOffsetY;
            // 2. 移动按钮到新位置
            storyIntroLoreButton?.SetPosition(buttonX, buttonY);
            // 3. 移动按钮上的文字标签到按钮旁边的偏移位置
            SetStoryIntroLoreLabelPosition(
                buttonX + labelOffsetX,
                buttonY + labelOffsetY);
            // 4. 移动传说面板的美术元素，偏移量基于按钮与初始位置的差值
            SetStoryIntroLoreArtOffset(
                buttonX - StoryIntroLoreButtonX,
                buttonY - StoryIntroLoreButtonY);
        }

        // 设置传说按钮标签的屏幕位置
        private void SetStoryIntroLoreLabelPosition(float x, float y)
        {
            // 1. 没有标签对象则跳过
            if (storyIntroLoreLabelObject == null)
            {
                return;
            }

            // 2. 如果是原生文字（TMP），用像素坐标设置 UI 位置
            var nativeText = storyIntroLoreLabelObject.GetComponent<TMPro.TMP_Text>();
            if (nativeText != null)
            {
                mlpNativeMenuTextLayer.SetPixelPosition(nativeText.rectTransform, x, y);
                return;
            }

            // 3. 否则将像素坐标转换为世界坐标并吸附到像素网格
            storyIntroLoreLabelObject.transform.position = mlpConstants.PixelToWorldSnapped(
                x,
                y,
                storyIntroLoreLabelObject.transform.position.z);
        }

        // 设置传说面板美术元素的偏移量（跟随按钮移动）
        private void SetStoryIntroLoreArtOffset(float pixelOffsetX, float pixelOffsetY)
        {
            // 1. 没有美术根节点则跳过
            if (storyIntroLoreArtRoot == null)
            {
                return;
            }

            // 2. 将原点和目标偏移都转换为世界坐标，计算相对偏移量
            var origin = mlpConstants.PixelToWorldSnapped(0f, 0f);
            var offset = mlpConstants.PixelToWorldSnapped(pixelOffsetX, pixelOffsetY);
            // 3. 设置美术元素的本地位置偏移
            storyIntroLoreArtRoot.transform.localPosition = new Vector3(
                offset.x - origin.x,
                offset.y - origin.y,
                0f);
        }

        /// <summary>
        /// 如果原生文字层处于活动状态且能渲染文字则返回 true。
        /// </summary>
        private bool ShouldUseNativeMenuText(Transform parent)
        {
            return nativeMenuTextLayer != null && parent != null && nativeMenuTextLayer.Owns(parent);
        }

        /// <summary>
        /// 在菜单界面上绘制半透明深色面板矩形。
        /// </summary>
        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            return CreatePanel(name, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        /// <summary>
        /// 在菜单界面上绘制半透明深色面板矩形。
        /// </summary>
        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. 先尝试用独立贴图创建面板（画质更好）
            var standalonePanel = TryCreateStandaloneTintPanel(name, x, y, width, height, sortingOrder, tint, parent);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            // 2. 独立贴图不可用时，用图集中的色块精灵替代
            var panel = mlpRender.Sprite(name, mlpAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            // 3. 根据目标宽高缩放精灵，并设置半透明颜色
            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / LegacyTintPanelSourcePixels,
                mlpConstants.UnitsPerPixel * height / LegacyTintPanelSourcePixels,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        // 尝试用独立贴图作为菜单背景（画质更好），失败返回 false
        private bool TryCreateStandaloneMenuBackground(string backgroundFrame)
        {
            // 1. 将背景帧名称解析为对应的贴图资源键
            var imageKey = ResolveStandaloneMenuBackgroundImage(backgroundFrame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return false;
            }

            // 2. 从 Resources 加载贴图，加载失败则返回 false
            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return false;
            }

            // 3. 在屏幕中心创建背景图片
            var background = mlpRender.Image(
                "MenuBackground",
                texture,
                mlpConstants.Width2,
                240f,
                0.5f,
                0.5f,
                0,
                runtimeRoot);
            // 4. 按目标尺寸缩放背景图片
            background.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * LegacyMenuBackgroundWidth / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * LegacyMenuBackgroundHeight / Mathf.Max(1f, texture.height),
                1f);
            return true;
        }

        // 将背景帧名称映射为对应的独立贴图资源键
        private static string ResolveStandaloneMenuBackgroundImage(string backgroundFrame)
        {
            return backgroundFrame switch
            {
                "bg10000" => mlpAssets.Images.MenuBackgroundHalloweenSpotlight,
                "bg2blue0000" => mlpAssets.Images.MenuBackgroundMoonlitGym,
                _ => null
            };
        }

        // 尝试用独立的圆角面板贴图创建面板（画质更好），失败返回 null
        private static GameObject TryCreateStandaloneTintPanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. 加载圆角面板贴图，加载失败则返回 null
            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.PanelFillSoft));
            if (texture == null)
            {
                return null;
            }

            // 2. 在指定位置创建面板图片
            var panel = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            // 3. 按目标宽高缩放，并设置半透明颜色
            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        /// <summary>
        /// 构建设置界面使用的篮球皮肤和难度选择面板。
        /// </summary>
        private void CreateOptionsPanel(string prefix, float centerX)
        {
            // 1. 在面板顶部显示 "SETTINGS" 标题文字
            CreateMenuText(
                $"{prefix}_OptionTitle",
                "SETTINGS",
                centerX,
                160f,
                20,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);
        }

        /// <summary>
        /// 显示左右箭头和篮球预览，让玩家选择篮球皮肤。
        /// </summary>
        private void CreateBallSelector(
            string key,
            float centerX,
            mlpBallSelection selection,
            System.Action previousBallAction,
            System.Action nextBallAction,
            float headerY,
            float previewY,
            float labelY)
        {
            // 1. 显示 "BALL" 标题
            CreateMenuText(
                $"{key}_Header",
                "BALL",
                centerX,
                headerY,
                16,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);

            // 2. 创建左右箭头按钮，用于切换上一个/下一个篮球皮肤
            menuButtons.Add(new mlpMenuButton("<", centerX - BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, previousBallAction, runtimeRoot));
            menuButtons.Add(new mlpMenuButton(">", centerX + BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, nextBallAction, runtimeRoot));

            // 3. 在箭头之间显示当前篮球的预览图
            CreateBallPreview(
                $"{key}_Preview",
                mlpBallCatalog.PreviewTheme(selection),
                centerX,
                previewY + 1f,
                BallPreviewPixels,
                19);

            // 4. 在预览下方显示篮球名称
            CreateMenuText(
                $"{key}_Label",
                mlpBallCatalog.Label(selection),
                centerX,
                labelY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentBody);
        }

        /// <summary>
        /// 选择最高难度时显示警告信息。
        /// </summary>
        private void CreateHellDifficultyWarning(float centerX, float y)
        {
            // 1. 显示地狱难度警告文字，告知玩家 CPU 会使用额外超级技能
            CreateMenuText(
                "HellDifficultyWarning",
                "UNFAIR CHALLENGE: CPU USES BONUS SUPERS",
                centerX,
                y,
                12,
                new Color32(0xFF, 0x9C, 0x32, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 12 块：角色/球选择器和预览
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 显示角色头像、左右箭头和名称标签，用于选择格斗角色。
        /// </summary>
        private void CreateCharacterSelector(
            string key,
            string header,
            float centerX,
            int characterId,
            System.Action previousCharacterAction,
            System.Action nextCharacterAction,
            float previewY,
            float previewScale,
            float nameY)
        {
            // 1. 显示标题文字（如 "CHARACTER" 或 "P1"）
            CreateMenuText(
                $"{key}_Header",
                header,
                centerX,
                SelectorHeaderY,
                header.Length <= 2 ? 26 : 18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                mlpTextStyle.TournamentAccent);

            // 2. 创建左右箭头按钮，用于切换上一个/下一个角色
            menuButtons.Add(new mlpMenuButton("<", centerX - SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, previousCharacterAction, runtimeRoot));
            menuButtons.Add(new mlpMenuButton(">", centerX + SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, nextCharacterAction, runtimeRoot));

            // 3. 显示角色动画预览模型
            CreatePreviewPlayer(key, characterId, centerX, previewY, previewScale);
            // 4. 显示角色技能图标和技能名称
            var skillDefinition = mlpCharacterSkillsData.Get(characterId);
            CreateCharacterSkillIcon($"{key}_SkillIcon", skillDefinition, centerX - 82f, nameY - 18f, 21);
            CreateMenuText(
                $"{key}_SkillName",
                skillDefinition.SkillName,
                centerX - 48f,
                nameY - 20f,
                GetCompactFontSize(skillDefinition.SkillName, 12, 11, 10),
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleLeft,
                21,
                mlpTextStyle.TournamentAccent);
            // 5. 显示角色名称
            CreateMenuText(
                $"{key}_CharacterName",
                mlpPlayersData.GetCharacterName(characterId),
                centerX,
                nameY,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                mlpTextStyle.TournamentBody);
        }

        // 绘制角色技能图标（光球背景 + 技能图标叠加）
        private void CreateCharacterSkillIcon(string name, mlpCharacterSkillDefinition skillDefinition, float x, float y, int sortingOrder)
        {
            const float orbPixels = 52f;
            const float iconPixels = 42f;
            // 1. 尝试加载独立的圆形光球背景贴图
            var orbTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.EmblemOrb));
            if (orbTexture != null)
            {
                // 2. 用独立贴图创建光球背景，缩放到目标尺寸
                var orb = mlpRender.Image($"{name}_Orb", orbTexture, x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
                orb.transform.localScale = new Vector3(
                    mlpConstants.UnitsPerPixel * orbPixels / Mathf.Max(1f, orbTexture.width),
                    mlpConstants.UnitsPerPixel * orbPixels / Mathf.Max(1f, orbTexture.height),
                    1f);
                var orbRenderer = orb.GetComponent<SpriteRenderer>();
                if (orbRenderer != null)
                {
                    orbRenderer.color = new Color(1f, 1f, 1f, 0.92f);
                }
            }
            else
            {
                // 3. 独立贴图不可用时，用图集中的光球精灵替代
                var fallbackOrb = mlpRender.Sprite($"{name}_OrbFallback", mlpAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
                fallbackOrb.transform.localScale *= orbPixels / 150f;
            }

            // 4. 如果角色没有独立技能图标美术，到此结束
            if (!skillDefinition.HasStandaloneIconArt)
            {
                return;
            }

            // 5. 加载并创建技能图标，叠在光球上方
            var iconPath = mlpAssets.Images.ResourcePath(skillDefinition.IconImageKey);
            mlpIconButton.CreateImageIcon(name, iconPath, x, y, sortingOrder + 1, iconPixels, runtimeRoot);
        }

        /// <summary>
        /// 渲染所选篮球皮肤的小型预览。
        /// </summary>
        private void CreateBallPreview(string name, mlpBallTheme theme, float x, float y, float targetPixels, int sortingOrder)
        {
            // 1. 加载篮球主题精灵，加载失败则使用默认篮球精灵
            var sprite = mlpGameplaySpriteLoader.LoadBallThemeSprite(theme, 0.5f, 0.5f) ??
                         mlpAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);

            if (sprite == null)
            {
                return;
            }

            // 2. 创建新的游戏对象，添加精灵渲染器并设置精灵和排序层级
            var preview = new GameObject(name);
            preview.transform.SetParent(runtimeRoot, false);
            var renderer = preview.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // 3. 根据目标像素尺寸计算缩放比例，应用像素对齐变换
            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetPixels / Mathf.Max(1f, spritePixels);
            mlpRender.ApplyPixelTransform(preview.transform, x, y, 0f, scale);
        }

        /// <summary>
        /// 在设置界面上显示实时动画角色模型。
        /// </summary>
        private void CreatePreviewPlayer(string key, int characterId, float x, float y, float scale)
        {
            // 1. 计算预览缩放比例（基础缩放 x 角色专属缩放倍率）
            var previewScale = scale * PreviewScaleFactor * mlpPlayersData.GetCharacterPreviewScaleMultiplier(characterId);
            // 2. 在角色脚下绘制半透明阴影
            var shadow = mlpRender.Sprite($"{key}_PreviewShadow", mlpAtlasCache.Instance.Interface, "loginSelect0000", x, y + PreviewShadowYOffset, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= PreviewShadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.55f);

            // 3. 创建预览根节点，设置像素对齐的位置和缩放
            var previewRoot = new GameObject($"{key}_Preview");
            previewRoot.transform.SetParent(runtimeRoot, false);
            mlpRender.ApplyPixelTransform(previewRoot.transform, x, y, 0f, previewScale);

            // 4. 构建角色骨骼动画模型，失败则跳过
            var armature = mlpPlayersData.BuildGameplayArmature($"{key}_PreviewArmature");
            if (armature == null)
            {
                return;
            }

            // 5. 将骨骼挂到预览根节点下，设置垂直偏移和缩放，应用角色外观
            armature.transform.SetParent(previewRoot.transform, false);
            armature.transform.localPosition = new Vector3(0f, PreviewArmatureYOffset + mlpPlayersData.GetCharacterPreviewOffsetY(characterId), 0f);
            armature.transform.localScale = new Vector3(PreviewArmatureScale, PreviewArmatureScale, 1f);
            mlpPlayersData.ApplyCharacter(armature, characterId);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 13 块：锦标赛对阵图（常规赛排名 + 季后赛对阵）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 绘制完整的锦标赛对阵图，包含赛季横幅和季后赛轮次。
        /// </summary>
        private void CreateTournamentBracketBoard(mlpTournamentData tournament)
        {
            if (tournament.CurrentStage == mlpTournamentStage.RegularSeason)
            {
                CreateTournamentRegularSeasonBoard(tournament);
            }
            else
            {
                CreateTournamentPlayoffBoard(tournament);
            }

            CreateTournamentSummaryPanel(tournament);
        }

        // 在对阵图顶部显示当前赛事阶段的横幅标题（如"FINAL FOUR"、"GRAND FINAL"）
        private void CreateTournamentSeasonBanner(mlpTournamentData tournament)
        {
            // 1. 获取当前赛事阶段的标题文字
            var title = mlpSinglePlayerNarrative.GetTournamentStageTitle(tournament);
            if (title == "DIVISIONS")
            {
                // 2. 如果是分区赛阶段则不显示横幅，直接返回
                return;
            }

            // 3. 创建标题背景面板
            CreatePanel(
                "TournamentSeasonBannerPanel",
                mlpConstants.Width2,
                98f,
                204f,
                24f,
                18,
                new Color(0.03f, 0.06f, 0.1f, 0.74f));
            // 4. 在背景上显示阶段标题文字（如"常规赛"、"半决赛"等）
            CreateMenuText(
                "TournamentSeasonBannerTitle",
                title,
                mlpConstants.Width2,
                98f,
                11,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                19,
                mlpTextStyle.TournamentAccent);
        }

        /// <summary>
        /// 绘制常规赛排名表，显示胜负记录。
        /// </summary>
        private void CreateTournamentRegularSeasonBoard(mlpTournamentData tournament)
        {
            // 1. 创建排名表的整体半透明背景
            CreatePanel(
                "RegularSeasonBackdrop",
                mlpConstants.Width2,
                236f,
                744f,
                302f,
                8,
                new Color(0.01f, 0.04f, 0.08f, 0.28f));

            // 2. 左侧绘制 A 分区的队伍排名列表
            CreateTournamentDivisionBoard("DivisionA", 222f, 242f, "DIV. A", tournament, 0);
            // 3. 右侧绘制 B 分区的队伍排名列表
            CreateTournamentDivisionBoard("DivisionB", 578f, 242f, "DIV. B", tournament, 1);
        }

        /// <summary>
        /// 绘制赛季排名中的一个分区列，包含队伍条目。
        /// </summary>
        private void CreateTournamentDivisionBoard(string key, float x, float y, string title, mlpTournamentData tournament, int divisionIndex)
        {
            const float boardWidth = 316f;
            const float boardHeight = 322f;
            const float rowHeight = 34f;
            const float rowSpacing = 42f;
            const float rankXOffset = -111f;
            const float badgeXOffset = -76f;
            const float nameXOffset = -52f;
            const float winsXOffset = 70f;
            const float lossesXOffset = 104f;
            const float qualifiedXOffset = 122f;

            CreateFramedPanel(
                $"{key}_Frame",
                "0bg100000",
                x,
                y,
                boardWidth,
                boardHeight,
                9,
                Color.white);
            CreatePanel(
                $"{key}_Shade",
                x,
                y + 3f,
                boardWidth - 62f,
                boardHeight - 92f,
                10,
                new Color(0.02f, 0.05f, 0.07f, 0.36f));

            CreateStandingsAccentText(
                $"{key}_Title",
                title,
                x,
                y - 124f,
                19,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                11);

            var headerY = y - 86f;
            CreatePanel(
                $"{key}_Header",
                x,
                headerY,
                boardWidth - 68f,
                24f,
                11,
                new Color(0.15f, 0.67f, 0.82f, 0.92f));

            CreateStandingsAccentText($"{key}_HeaderRank", "#", x + rankXOffset, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);
            CreateStandingsAccentText($"{key}_HeaderTeam", "TEAM", x + nameXOffset, headerY + 1f, 12, Color.white, TextAnchor.MiddleLeft, 12);
            CreateStandingsAccentText($"{key}_HeaderW", "W", x + winsXOffset, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);
            CreateStandingsAccentText($"{key}_HeaderL", "L", x + lossesXOffset, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);

            var standings = tournament.GetDivisionStandings(divisionIndex);
            for (var i = 0; i < standings.Length; i++)
            {
                var entry = standings[i];
                var rowY = y - 46f + i * rowSpacing;
                var isPlayer = entry.CharacterId == tournament.PlayerCharacterId;
                var isCurrentOpponent = !tournament.RegularSeasonCompleted && entry.CharacterId == tournament.CurrentOpponentCharacterId;
                var qualified = tournament.RegularSeasonCompleted && i < 2;

                var rowTint = new Color(0.34f, 0.2f, 0.58f, 0.92f);
                if (qualified)
                {
                    rowTint = new Color(0.12f, 0.56f, 0.42f, 0.94f);
                }

                if (isCurrentOpponent)
                {
                    rowTint = new Color(0.16f, 0.44f, 0.72f, 0.94f);
                }

                if (isPlayer)
                {
                    rowTint = new Color(0.94f, 0.58f, 0.16f, 0.96f);
                }

                CreatePanel(
                    $"{key}_Row_{i}",
                    x,
                    rowY,
                    boardWidth - 68f,
                    rowHeight,
                    11,
                    rowTint);

                CreateStandingsAccentText(
                    $"{key}_Rank_{i}",
                    (i + 1).ToString(),
                    x + rankXOffset,
                    rowY + 1f,
                    16,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    12);

                var glowColor = qualified
                    ? new Color(0.3f, 1f, 0.64f, 0.34f)
                    : isPlayer
                        ? new Color(1f, 0.74f, 0.32f, 0.34f)
                        : new Color(0.3f, 0.96f, 1f, 0.26f);
                CreateTournamentBadge(
                    $"{key}_Badge_{i}",
                    entry.CharacterId,
                    x + badgeXOffset,
                    rowY,
                    12,
                    TournamentStandingsGlowScale,
                    TournamentStandingsBadgeScale,
                    TournamentStandingsPortraitPixels,
                    glowColor);

                var name = CharacterNameOrTbd(entry.CharacterId);
                CreateStandingsBodyText(
                    $"{key}_Name_{i}",
                    name,
                    x + nameXOffset,
                    rowY + 1f,
                    GetCompactFontSize(name, 13, 12, 11),
                    Color.white,
                    TextAnchor.MiddleLeft,
                    12);
                CreateStandingsAccentText(
                    $"{key}_W_{i}",
                    entry.Wins.ToString(),
                    x + winsXOffset,
                    rowY + 1f,
                    14,
                    qualified ? new Color32(0xD7, 0xF2, 0x4A, 0xFF) : Color.white,
                    TextAnchor.MiddleCenter,
                    12);
                CreateStandingsAccentText(
                    $"{key}_L_{i}",
                    entry.Losses.ToString(),
                    x + lossesXOffset,
                    rowY + 1f,
                    14,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    12);

                if (qualified)
                {
                    CreateStandingsAccentText(
                        $"{key}_Qualified_{i}",
                        "Q",
                        x + qualifiedXOffset,
                        rowY + 1f,
                        12,
                        new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                        TextAnchor.MiddleCenter,
                        12);
                }
            }
        }

        /// <summary>
        /// 在排名行单元格内添加文字标签。
        /// </summary>
        private TextMesh CreateStandingsBodyText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            int sortingOrder)
        {
            if (ShouldUseNativeMenuText(runtimeRoot))
            {
                CreateMenuText(name, text, x, y, fontSize, color, anchor, sortingOrder, mlpTextStyle.TournamentBody);
                return null;
            }

            return mlpRender.Text(
                name,
                text,
                x,
                y,
                fontSize,
                color,
                anchor,
                sortingOrder,
                runtimeRoot,
                mlpFontKind.RajdhaniSemiBold,
                outlineColor: new Color(0.02f, 0.03f, 0.08f, 0.9f),
                outlinePixels: fontSize >= 13 ? 0.7f : 0.58f);
        }

        /// <summary>
        /// 在排名行单元格内添加高亮文字标签。
        /// </summary>
        private TextMesh CreateStandingsAccentText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            int sortingOrder)
        {
            if (ShouldUseNativeMenuText(runtimeRoot))
            {
                CreateMenuText(name, text, x, y, fontSize, color, anchor, sortingOrder, mlpTextStyle.TournamentAccent);
                return null;
            }

            return mlpRender.Text(
                name,
                text,
                x,
                y,
                fontSize,
                color,
                anchor,
                sortingOrder,
                runtimeRoot,
                mlpFontKind.RajdhaniBold,
                outlineColor: new Color(0.02f, 0.03f, 0.08f, 0.92f),
                outlinePixels: fontSize >= 14 ? 0.8f : 0.62f);
        }

        /// <summary>
        /// 绘制季后赛对阵图，包含半决赛和决赛比赛面板。
        /// </summary>
        private void CreateTournamentPlayoffBoard(mlpTournamentData tournament)
        {
            const float playoffBackdropY = 232f;
            const float playoffBackdropHeight = 292f;
            const float finalPanelY = 168f;
            const float semiPanelY = 238f;
            const float semiPanelOffsetX = 200f;
            const float placementPanelY = 324f;

            // 1. 创建季后赛整体背景面板
            CreatePanel(
                "PlayoffBackdrop",
                mlpConstants.Width2,
                playoffBackdropY,
                742f,
                playoffBackdropHeight,
                8,
                new Color(0.02f, 0.05f, 0.08f, 0.28f));

            // 2. 判断当前是否处于半决赛阶段（用于高亮显示）
            var semiCurrent = !tournament.Completed && tournament.CurrentStage == mlpTournamentStage.SemiFinal;
            // 3. 绘制左侧半决赛比赛卡片
            CreateTournamentPlayoffMatchPanel(
                "SemiFinalLeft",
                mlpConstants.Width2 - semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[0],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[0], tournament.PlayerCharacterId));
            // 4. 绘制右侧半决赛比赛卡片
            CreateTournamentPlayoffMatchPanel(
                "SemiFinalRight",
                mlpConstants.Width2 + semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[1],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[1], tournament.PlayerCharacterId));
            // 5. 绘制决赛比赛卡片
            CreateTournamentPlayoffMatchPanel(
                "FinalMatch",
                mlpConstants.Width2,
                finalPanelY,
                "FINAL",
                tournament.FinalResult,
                !tournament.Completed && tournament.CurrentStage == mlpTournamentStage.Final);
            // 6. 绘制季军赛比赛卡片
            CreateTournamentPlayoffMatchPanel(
                "ThirdPlaceMatch",
                mlpConstants.Width2,
                placementPanelY,
                "3RD PLACE MATCH",
                tournament.ThirdPlaceResult,
                !tournament.Completed && tournament.CurrentStage == mlpTournamentStage.ThirdPlace);
        }

        /// <summary>
        /// 绘制单个季后赛比赛卡片，显示两个玩家槽位。
        /// </summary>
        private void CreateTournamentPlayoffMatchPanel(string key, float x, float y, string title, mlpTournamentMatchResult match, bool current)
        {
            const float panelWidth = 180f;
            const float panelHeight = 96f;
            const float badgeOffsetX = 58f;
            const float nameXOffset = 24f;
            const float scoreXOffset = 72f;
            const float rowOffset = 18f;
            const float titleOffsetY = 58f;

            // 1. 根据比赛状态选择面板颜色（当前进行中/已完成/未开始）
            var tint = current
                ? new Color(1f, 0.78f, 0.52f, 1f)
                : match.Completed
                    ? new Color(0.8f, 0.98f, 0.9f, 1f)
                    : new Color(0.76f, 0.82f, 0.9f, 0.9f);
            // 2. 选择边框样式（当前比赛用高亮边框，其他用普通边框）
            var frame = current ? "MatchBack0002" : "MatchBack0001";

            // 3. 创建带边框的比赛卡片面板
            CreateFramedPanel($"{key}_Frame", frame, x, y, panelWidth, panelHeight, 13, tint);
            // 4. 在面板内绘制半透明阴影层
            CreatePanel(
                $"{key}_Shade",
                x,
                y,
                panelWidth - 20f,
                panelHeight - 16f,
                14,
                new Color(0.03f, 0.05f, 0.08f, 0.3f));

            // 5. 显示比赛阶段标题（如"SEMIFINAL"、"FINAL"）
            CreateMenuText(
                $"{key}_Title",
                title,
                x,
                y - titleOffsetY,
                title.Length > 10 ? 13 : 15,
                current ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                15,
                mlpTextStyle.TournamentAccent);

            // 6. 绘制水平分隔线，将两个选手区域隔开
            CreatePanel($"{key}_DividerHorizontal", x, y, panelWidth - 28f, 2f, 15, new Color(0.3f, 0.86f, 0.9f, 0.46f));

            // 7. 绘制左侧选手的头像、名称和得分
            var leftRowY = y - rowOffset;
            var rightRowY = y + rowOffset;
            CreateTournamentPlayoffEntry(
                $"{key}_Left",
                match.LeftCharacterId,
                match.Completed && match.WinnerCharacterId == match.LeftCharacterId,
                x - badgeOffsetX,
                leftRowY,
                x - nameXOffset,
                x + scoreXOffset,
                match.LeftScore,
                match.Completed,
                current);
            // 8. 绘制右侧选手的头像、名称和得分
            CreateTournamentPlayoffEntry(
                $"{key}_Right",
                match.RightCharacterId,
                match.Completed && match.WinnerCharacterId == match.RightCharacterId,
                x - badgeOffsetX,
                rightRowY,
                x - nameXOffset,
                x + scoreXOffset,
                match.RightScore,
                match.Completed,
                current);

            // 9. 比赛未完成时显示对战分隔标记
            if (!match.Completed)
            {
                CreateMenuText(
                    $"{key}_Versus",
                    "-",
                    x + scoreXOffset,
                    y,
                    18,
                    current ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : new Color32(0x42, 0xF1, 0xE6, 0xFF),
                    TextAnchor.MiddleCenter,
                    16,
                    mlpTextStyle.TournamentAccent);
            }
        }

        /// <summary>
        /// 在季后赛比赛卡片中绘制一个玩家槽位，包含头像和名称。
        /// </summary>
        private void CreateTournamentPlayoffEntry(
            string key,
            int characterId,
            bool winner,
            float badgeX,
            float y,
            float nameX,
            float scoreX,
            int score,
            bool showScore,
            bool current)
        {
            // 1. 根据胜负和比赛状态选择发光颜色（金色/青色/绿色）
            var glowColor = winner
                ? new Color(1f, 0.74f, 0.28f, 0.36f)
                : current
                    ? new Color(0.3f, 0.96f, 1f, 0.32f)
                    : new Color(0.24f, 0.94f, 0.78f, 0.24f);
            // 2. 绘制选手头像徽章（带发光底座）
            CreateTournamentBadge(
                key,
                characterId,
                badgeX,
                y,
                16,
                TournamentBracketGlowScale,
                TournamentBracketBadgeScale,
                TournamentBracketPortraitPixels,
                glowColor);

            // 3. 显示选手名称，获胜者用金色高亮
            var name = CharacterNameOrTbd(characterId);
            CreateStandingsBodyText(
                $"{key}_Name",
                name,
                nameX,
                y,
                GetCompactFontSize(name, 10, 9, 8),
                winner ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : Color.white,
                TextAnchor.MiddleLeft,
                17);
            // 4. 显示选手得分，比赛未结束时为空
            CreateStandingsAccentText(
                $"{key}_Score",
                showScore ? score.ToString() : string.Empty,
                scoreX,
                y,
                14,
                winner ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleRight,
                17);
        }

        /// <summary>
        /// 在对阵图底部绘制结果摘要，显示玩家状态。
        /// </summary>
        private void CreateTournamentSummaryPanel(mlpTournamentData tournament)
        {
            // 1. 根据赛事是否结束，调整面板尺寸和位置
            var summaryCompleted = tournament.Completed;
            var summaryWidth = summaryCompleted ? 312f : 328f;
            var summaryHeight = summaryCompleted ? 72f : 40f;
            var summaryY = summaryCompleted ? TournamentSummaryY : TournamentSummaryY + 6f;
            // 2. 创建带边框的摘要面板
            CreateFramedPanel(
                "TournamentSummaryPanel",
                "btn_bg0000",
                mlpConstants.Width2,
                summaryY,
                summaryWidth,
                summaryHeight,
                15,
                summaryCompleted
                    ? new Color(0.2f, 0.78f, 0.88f, 0.96f)
                    : new Color(0.22f, 0.86f, 0.94f, 0.94f));

            // 3. 赛事已结束时显示冠军信息和玩家排名
            if (summaryCompleted)
            {
                // 4. 绘制冠军头像小型徽章
                CreateTournamentMiniBadge("ChampionBadge", tournament.ChampionCharacterId, 280f, summaryY, 16);
                // 5. 显示"CHAMPION"标签
                CreateMenuText(
                    "ChampionLabel",
                    "CHAMPION",
                    312f,
                    summaryY - 12f,
                    12,
                    new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                    TextAnchor.MiddleLeft,
                    17,
                    mlpTextStyle.TournamentAccent);
                // 6. 显示冠军角色名称
                CreateMenuText(
                    "ChampionName",
                    CharacterNameOrTbd(tournament.ChampionCharacterId),
                    312f,
                    summaryY + 2f,
                    GetCompactFontSize(CharacterNameOrTbd(tournament.ChampionCharacterId), 15, 13, 11),
                    Color.white,
                    TextAnchor.MiddleLeft,
                    17,
                    mlpTextStyle.TournamentBody);
                // 7. 显示玩家最终排名（如"YOU FINISHED #3"）
                CreateMenuText(
                    "PlacementLabel",
                    $"YOU FINISHED #{tournament.PlayerPlacement}",
                    312f,
                    summaryY + 18f,
                    11,
                    new Color32(0xFF, 0xD6, 0x6D, 0xFF),
                    TextAnchor.MiddleLeft,
                    17,
                    mlpTextStyle.TournamentAccent);
                return;
            }

            // 8. 赛事进行中时，根据当前阶段生成提示标题和详情
            string headline;
            string detail;
            if (tournament.CurrentStage == mlpTournamentStage.RegularSeason)
            {
                if (tournament.RegularSeasonCompleted)
                {
                    headline = "READY";
                    detail = GetMatchupText(GetPlayerPlayoffMatch(tournament), "START FINALS");
                }
                else
                {
                    headline = $"ROUND {tournament.CurrentRegularSeasonRoundIndex + 1}";
                    detail = $"{CharacterNameOrTbd(tournament.PlayerCharacterId)} VS {CharacterNameOrTbd(tournament.CurrentOpponentCharacterId)}";
                }
            }
            else if (tournament.CurrentStage == mlpTournamentStage.SemiFinal)
            {
                headline = "UP NEXT";
                detail = GetMatchupText(GetPlayerPlayoffMatch(tournament), "SEMIFINAL SET");
            }
            else if (tournament.CurrentStage == mlpTournamentStage.ThirdPlace)
            {
                headline = "UP NEXT";
                detail = GetMatchupText(tournament.ThirdPlaceResult, "3RD PLACE MATCH");
            }
            else
            {
                headline = "UP NEXT";
                detail = GetMatchupText(tournament.FinalResult, "FINAL");
            }

            // 9. 显示摘要信息文字（如"ROUND 3 - P1 VS P4"）
            CreateStandingsBodyText(
                "TournamentSummaryDetail",
                $"{headline} - {detail}",
                mlpConstants.Width2,
                summaryY + 2f,
                GetCompactFontSize($"{headline} - {detail}", 12, 11, 10),
                Color.white,
                TextAnchor.MiddleCenter,
                17);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 14 块：锦标赛工具方法（头像、徽章、摘要面板）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 在对阵图上绘制小型标签徽章（如胜利、失败、种子排名）。
        /// </summary>
        private void CreateTournamentBadge(
            string key,
            int characterId,
            float x,
            float y,
            int sortingOrder,
            float glowScale,
            float badgeScale,
            float portraitPixels,
            Color glowColor,
            Transform parent = null)
        {
            // 1. 如果没有指定父对象，默认挂到运行时根节点下
            if (parent == null)
            {
                parent = runtimeRoot;
            }

            // 2. 计算徽章底座的像素尺寸（取头像和缩放的最大值）
            const float legacyBadgePixels = 150f;
            var badgePixels = Mathf.Max(
                portraitPixels + 10f,
                Mathf.Max(legacyBadgePixels * badgeScale, legacyBadgePixels * glowScale * 0.82f));
            // 3. 在发光颜色基础上混合生成光环边缘颜色
            var ringColor = new Color(
                Mathf.Clamp01(0.48f + glowColor.r * 0.55f),
                Mathf.Clamp01(0.48f + glowColor.g * 0.55f),
                Mathf.Clamp01(0.48f + glowColor.b * 0.55f),
                0.96f);
            // 4. 绘制徽章底座（含发光圈和深色背景）
            mlpRender.PortraitBackplate(
                $"{key}_Badge",
                x,
                y,
                badgePixels,
                sortingOrder,
                parent,
                glowColor,
                new Color(0.01f, 0.025f, 0.06f, characterId >= 0 ? 0.94f : 0.62f),
                ringColor);

            // 5. 如果角色 ID 有效，叠加上角色头像精灵
            if (characterId >= 0)
            {
                CreateTournamentPortrait($"{key}_Portrait", characterId, x, y + 1f, portraitPixels, sortingOrder + 3, parent);
            }
        }

        /// <summary>
        /// 将胜场数和总场数转换为百分比字符串，如 "75.0%"。
        /// </summary>
        private static string FormatWinningPercentage(float value)
        {
            return value.ToString("0.000");
        }

        /// <summary>
        /// 如果给定比赛中任一槽位包含玩家角色则返回 true。
        /// </summary>
        private static bool MatchIncludesPlayer(mlpTournamentMatchResult match, int playerCharacterId)
        {
            return match != null && (match.LeftCharacterId == playerCharacterId || match.RightCharacterId == playerCharacterId);
        }

        /// <summary>
        /// 查找包含玩家的季后赛比赛。
        /// </summary>
        private static mlpTournamentMatchResult GetPlayerPlayoffMatch(mlpTournamentData tournament)
        {
            if (MatchIncludesPlayer(tournament.SemiFinalResults[0], tournament.PlayerCharacterId))
            {
                return tournament.SemiFinalResults[0];
            }

            if (MatchIncludesPlayer(tournament.SemiFinalResults[1], tournament.PlayerCharacterId))
            {
                return tournament.SemiFinalResults[1];
            }

            return null;
        }

        /// <summary>
        /// 返回对阵比赛的简短标签，如 "P1 vs P3"。
        /// </summary>
        private static string GetMatchupText(mlpTournamentMatchResult match, string fallback)
        {
            if (match == null || match.LeftCharacterId < 0 || match.RightCharacterId < 0)
            {
                return fallback;
            }

            return $"{CharacterNameOrTbd(match.LeftCharacterId)} VS {CharacterNameOrTbd(match.RightCharacterId)}";
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 15 块：锦标赛颁奖典礼
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 构建完整的颁奖典礼界面，包含领奖台、奖杯和横幅。
        /// </summary>
        private void CreateTournamentAwardsScene(mlpTournamentData tournament)
        {
            // 1. 重置所有颁奖动画状态
            ResetTournamentAwardsState();

            // 2. 构建前三名排名数据，获取玩家对应名次的强调色
            var placements = BuildTournamentAwardsPlacements(tournament);
            var accentColor = GetTournamentAwardsAccentColor(tournament.PlayerPlacement);

            // 3. 创建展示区组，加载展示背景图（失败时用带边框面板替代）
            var showcaseGroup = CreateTournamentAwardsGroup("AwardsShowcase");
            var showcase = CreateStandaloneImage(
                "AwardsShowcasePanel",
                mlpAssets.Images.Ui.AwardsShowcasePanel,
                mlpConstants.Width2,
                TournamentAwardsShowcaseY,
                TournamentAwardsShowcaseWidth,
                TournamentAwardsShowcaseHeight,
                7,
                showcaseGroup.transform,
                new Color(1f, 1f, 1f, 0.9f));
            if (showcase == null)
            {
                CreateFramedPanel(
                    "AwardsShowcaseFallback",
                    "0bg100000",
                    mlpConstants.Width2,
                    TournamentAwardsShowcaseY,
                    TournamentAwardsShowcaseWidth,
                    TournamentAwardsShowcaseHeight,
                    7,
                    new Color(1f, 1f, 1f, 0.74f),
                    showcaseGroup.transform);
            }

            // 4. 注册展示区的入场滑入动画
            RegisterTournamentAwardsAnimation(showcaseGroup.transform, new Vector2(0f, 10f), 0.04f, 0.42f, 0.96f);

            // 5. 创建结果横幅组，加载铭牌图（失败时用面板替代）
            var bannerGroup = CreateTournamentAwardsGroup("AwardsResultBanner");
            var plaque = CreateStandaloneImage(
                "AwardsResultPlaque",
                mlpAssets.Images.Ui.AwardsResultPlaque,
                mlpConstants.Width2,
                TournamentAwardsPlaqueY,
                312f,
                66f,
                12,
                bannerGroup.transform);
            if (plaque == null)
            {
                CreateFramedPanel(
                    "AwardsResultPlaqueFallback",
                    "btn_bg0000",
                    mlpConstants.Width2,
                    TournamentAwardsPlaqueY,
                    312f,
                    66f,
                    12,
                    new Color(0.12f, 0.22f, 0.32f, 0.92f),
                    bannerGroup.transform);
            }

            // 6. 显示玩家结果信息（如"CHAMPION"、"RUNNER-UP"）
            CreateMenuText(
                "AwardsResultBannerLabel",
                GetTournamentAwardsPlayerMessage(tournament),
                mlpConstants.Width2,
                TournamentAwardsPlaqueY,
                18,
                accentColor,
                TextAnchor.MiddleCenter,
                13,
                mlpTextStyle.TournamentAccent,
                bannerGroup.transform);
            // 7. 显示结局叙事文字
            CreateMenuText(
                "AwardsResultEndingLine",
                mlpSinglePlayerNarrative.GetTournamentPlacementEnding(tournament.PlayerPlacement),
                mlpConstants.Width2,
                TournamentAwardsPlaqueY + 42f,
                10,
                new Color32(0xE0, 0xEC, 0xF4, 0xFF),
                TextAnchor.MiddleCenter,
                13,
                mlpTextStyle.TournamentBody,
                bannerGroup.transform);
            // 8. 注册横幅的入场滑入动画
            RegisterTournamentAwardsAnimation(bannerGroup.transform, new Vector2(0f, 14f), 0.1f, 0.42f, 0.95f);

            // 9. 创建领奖台组，加载底座图（失败时用图集精灵替代）
            var podiumGroup = CreateTournamentAwardsGroup("AwardsPodium");
            var podiumBase = CreateStandaloneImage(
                "AwardsPodiumBase",
                mlpAssets.Images.Ui.AwardsPodiumBase,
                mlpConstants.Width2,
                TournamentAwardsPodiumY,
                TournamentAwardsPodiumWidth,
                TournamentAwardsPodiumHeight,
                11,
                podiumGroup.transform);
            if (podiumBase == null)
            {
                var tribune = mlpRender.Sprite(
                    "AwardsTribuneFallback",
                    mlpAtlasCache.Instance.Interface,
                    "TribuneFinal0000",
                    mlpConstants.Width2,
                    TournamentAwardsPodiumY + 4f,
                    0.5f,
                    0.5f,
                    11,
                    podiumGroup.transform);
                tribune.transform.localScale *= 0.98f;
                tribune.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.98f);
            }

            // 10. 绘制第二名、冠军、第三名的领奖台名称标签
            CreateTournamentAwardsLaneLabel("AwardsLaneSecond", placements[1], TournamentAwardsLeftX, 384f, false, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneChampion", placements[0], TournamentAwardsChampionX, 368f, true, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneThird", placements[2], TournamentAwardsRightX, 390f, false, podiumGroup.transform);
            // 11. 注册领奖台组的入场动画
            RegisterTournamentAwardsAnimation(podiumGroup.transform, new Vector2(0f, 18f), 0.14f, 0.52f, 0.97f);

            // 12. 创建第二名角色组（左侧位置，较小缩放）
            CreateTournamentAwardsCharacterGroup(
                "AwardsSecondPlace",
                placements[1],
                TournamentAwardsLeftX,
                TournamentAwardsLeftY,
                TournamentAwardsSideScale,
                0.12f,
                0.38f,
                0.24f,
                0.28f);
            // 13. 创建冠军角色组（中间位置，最大缩放）
            CreateTournamentAwardsCharacterGroup(
                "AwardsChampionPlace",
                placements[0],
                TournamentAwardsChampionX,
                TournamentAwardsChampionY,
                TournamentAwardsChampionScale,
                0.13f,
                0.48f,
                0.28f,
                0.2f);
            // 14. 创建第三名角色组（右侧位置，较小缩放）
            CreateTournamentAwardsCharacterGroup(
                "AwardsThirdPlace",
                placements[2],
                TournamentAwardsRightX,
                TournamentAwardsRightY,
                TournamentAwardsSideScale,
                0.11f,
                0.38f,
                0.22f,
                0.36f);
        }

        /// <summary>
        /// 在指定位置绘制一组颁奖项目，带有标签。
        /// </summary>
        private GameObject CreateTournamentAwardsGroup(string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(runtimeRoot, false);
            return group;
        }

        /// <summary>
        /// 绘制颁奖典礼中使用的小型徽章。
        /// </summary>
        private void CreateTournamentAwardsBadge(string key, TournamentAwardsPlacement placement, float x, float y, bool champion, Transform parent)
        {
            mlpRender.PortraitBackplate(
                $"{key}_Badge",
                x,
                y,
                champion ? 64f : 58f,
                14,
                parent,
                placement.GlowColor,
                new Color(0.01f, 0.025f, 0.06f, champion ? 0.96f : 0.9f),
                new Color(placement.AccentColor.r, placement.AccentColor.g, placement.AccentColor.b, champion ? 0.98f : 0.9f));

            CreateTournamentPortrait($"{key}_Portrait", placement.CharacterId, x, y + 1f, champion ? 46f : 40f, 17, parent);

            mlpRender.Text(
                $"{key}_Rank",
                GetPlacementShortLabel(placement.Placement),
                x,
                y - (champion ? 44f : 40f),
                champion ? 16 : 14,
                placement.AccentColor,
                TextAnchor.MiddleCenter,
                18,
                parent,
                mlpTextStyle.TournamentAccent);
        }

        /// <summary>
        /// 在颁奖典礼中绘制赛道标签。
        /// </summary>
        private void CreateTournamentAwardsLaneLabel(string key, TournamentAwardsPlacement placement, float x, float y, bool champion, Transform parent)
        {
            var panelTint = GetTournamentAwardsPlateTint(placement, champion);
            var panel = CreateFramedPanel(
                $"{key}_Frame",
                "btn_bg0000",
                x,
                y,
                champion ? 176f : 146f,
                champion ? 36f : 32f,
                13,
                panelTint,
                parent);
            if (panel != null && placement.IsPlayer)
            {
                panel.transform.localScale *= 1.015f;
            }

            var name = CharacterNameOrTbd(placement.CharacterId);
            mlpRender.Text(
                $"{key}_Name",
                name,
                x,
                y + 1f,
                GetCompactFontSize(name, champion ? 14 : 13, 12, 11),
                Color.white,
                TextAnchor.MiddleCenter,
                14,
                parent,
                mlpTextStyle.TournamentBody);
        }

        /// <summary>
        /// 在颁奖典礼中绘制一组角色肖像。
        /// </summary>
        private void CreateTournamentAwardsCharacterGroup(
            string key,
            TournamentAwardsPlacement placement,
            float x,
            float y,
            float scale,
            float z,
            float shadowScale,
            float glowAlpha,
            float landingDelay)
        {
            // 1. 创建角色组的父对象
            var group = CreateTournamentAwardsGroup(key);
            // 2. 加载发光光圈图片，玩家角色透明度略高（失败时用图集精灵替代）
            var glow = CreateStandaloneImage(
                $"{key}_Aura",
                mlpAssets.Images.Ui.EmblemOrb,
                x,
                y - 18f,
                placement.Placement == 1 ? 144f : 116f,
                placement.Placement == 1 ? 144f : 116f,
                12,
                group.transform,
                new Color(
                    placement.GlowColor.r,
                    placement.GlowColor.g,
                    placement.GlowColor.b,
                    placement.IsPlayer ? glowAlpha + 0.06f : glowAlpha));
            if (glow == null)
            {
                glow = mlpRender.Sprite($"{key}_Aura", mlpAtlasCache.Instance.Interface, "EmblemsBg0000", x, y - 18f, 0.5f, 0.5f, 12, group.transform);
                glow.transform.localScale *= placement.Placement == 1 ? 0.82f : 0.66f;
                glow.GetComponent<SpriteRenderer>().color = new Color(placement.GlowColor.r, placement.GlowColor.g, placement.GlowColor.b, glowAlpha);
            }

            // 3. 绘制角色脚下阴影，玩家角色阴影更明显
            var shadow = mlpRender.Sprite($"{key}_Shadow", mlpAtlasCache.Instance.Interface, "loginSelect0000", x, y + 24f, 0.5f, 0.5f, 13, group.transform);
            shadow.transform.localScale *= shadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, placement.IsPlayer ? 0.7f : 0.52f);

            // 4. 创建角色根对象，设置位置和缩放
            var playerRoot = new GameObject($"{key}_Root");
            playerRoot.transform.SetParent(group.transform, false);
            var characterScale = scale * mlpPlayersData.GetCharacterPreviewScaleMultiplier(placement.CharacterId);
            mlpRender.ApplyPixelTransform(playerRoot.transform, x, y, z, characterScale);

            // 5. 构建角色骨骼动画，绑定到根对象
            var armature = mlpPlayersData.BuildGameplayArmature($"{key}_Armature");
            if (armature != null)
            {
                armature.transform.SetParent(playerRoot.transform, false);
                armature.transform.localPosition = new Vector3(
                    0f,
                    TournamentAwardsArmatureYOffset + mlpPlayersData.GetCharacterPreviewOffsetY(placement.CharacterId) * 0.65f,
                    0f);
                armature.transform.localScale = new Vector3(TournamentAwardsArmatureScale, TournamentAwardsArmatureScale, 1f);
                mlpPlayersData.ApplyCharacter(armature, placement.CharacterId);

                if (placement.IsPlayer)
                {
                    awardsCelebrationPlayer = armature;
                    awardsCelebrationCupAnimation = placement.CupAnimation;
                }
            }

            RegisterTournamentAwardsAnimation(group.transform, new Vector2(0f, 12f), landingDelay, 0.44f, 0.96f);
        }

        /// <summary>
        /// 创建颁奖典礼的排名数据有序列表。
        /// </summary>
        private TournamentAwardsPlacement[] BuildTournamentAwardsPlacements(mlpTournamentData tournament)
        {
            return new[]
            {
                CreateTournamentAwardsPlacement(1, tournament.ChampionCharacterId, tournament.PlayerCharacterId),
                CreateTournamentAwardsPlacement(2, GetMatchLoserCharacterId(tournament.FinalResult), tournament.PlayerCharacterId),
                CreateTournamentAwardsPlacement(3, tournament.ThirdPlaceResult.WinnerCharacterId, tournament.PlayerCharacterId)
            };
        }

        /// <summary>
        /// 在颁奖典礼中绘制单个排名条目。
        /// </summary>
        private static TournamentAwardsPlacement CreateTournamentAwardsPlacement(int placement, int characterId, int playerCharacterId)
        {
            Color accentColor = placement == 1
                ? new Color32(0xD7, 0xF2, 0x4A, 0xFF)
                : placement == 2
                    ? new Color32(0x9D, 0xF5, 0xFF, 0xFF)
                    : new Color32(0xFF, 0xB1, 0x48, 0xFF);
            var glowColor = placement == 1
                ? new Color(0.52f, 1f, 0.18f, 0.26f)
                : placement == 2
                    ? new Color(0.38f, 0.94f, 1f, 0.22f)
                    : new Color(1f, 0.58f, 0.14f, 0.22f);

            return new TournamentAwardsPlacement
            {
                Placement = placement,
                CharacterId = characterId,
                IsPlayer = characterId == playerCharacterId,
                CupAnimation = placement == 1 ? "cup1" : placement == 2 ? "cup2" : "cup3",
                AccentColor = accentColor,
                GlowColor = glowColor
            };
        }

        /// <summary>
        /// 将项目添加到颁奖动画队列，使其延迟滑入。
        /// </summary>
        private void RegisterTournamentAwardsAnimation(Transform root, Vector2 startOffsetPixels, float delay, float duration, float startScale = 0.94f, bool fade = true)
        {
            if (root == null)
            {
                return;
            }

            // 1. 收集该组下所有精灵和文字组件
            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            var textMeshes = root.GetComponentsInChildren<TextMesh>(true);
            // 2. 创建动画数据项，记录目标位置和初始偏移
            var item = new TournamentAwardsAnimatedItem
            {
                Root = root,
                TargetLocalPosition = root.localPosition,
                TargetLocalScale = root.localScale,
                StartLocalOffset = PixelOffsetToLocal(startOffsetPixels),
                Delay = Mathf.Max(0f, delay),
                Duration = Mathf.Max(0.01f, duration),
                StartScale = Mathf.Max(0.01f, startScale),
                Fade = fade,
                SpriteRenderers = spriteRenderers,
                SpriteBaseColors = new Color[spriteRenderers.Length],
                TextMeshes = textMeshes,
                TextBaseColors = new Color[textMeshes.Length]
            };

            // 3. 记录所有精灵和文字的原始颜色
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                item.SpriteBaseColors[i] = spriteRenderers[i].color;
            }

            for (var i = 0; i < textMeshes.Length; i++)
            {
                item.TextBaseColors[i] = textMeshes[i].color;
            }

            // 4. 设置初始偏移位置、缩小比例和透明度为零
            root.localPosition = item.TargetLocalPosition + item.StartLocalOffset;
            root.localScale = item.TargetLocalScale * item.StartScale;
            ApplyTournamentAwardsAlpha(item, fade ? 0f : 1f);
            awardsAnimatedItems.Add(item);
        }

        /// <summary>
        /// 每帧更新颁奖入场动画。
        /// </summary>
        private void UpdateTournamentAwardsSequence(float deltaTime)
        {
            if (currentScreen != mlpBootstrapScreen.TournamentAwards || runtimeRoot == null)
            {
                return;
            }

            // 1. 累计已用时间
            awardsElapsed += deltaTime;
            // 2. 遍历所有动画项，根据时间计算缓动进度
            for (var i = 0; i < awardsAnimatedItems.Count; i++)
            {
                var item = awardsAnimatedItems[i];
                if (item.Root == null)
                {
                    continue;
                }

                var normalized = Mathf.Clamp01((awardsElapsed - item.Delay) / item.Duration);
                var eased = EaseOutBack01(normalized);
                // 3. 插值更新位置、缩放和透明度
                item.Root.localPosition = item.TargetLocalPosition + Vector3.Lerp(item.StartLocalOffset, Vector3.zero, eased);
                item.Root.localScale = item.TargetLocalScale * Mathf.Lerp(item.StartScale, 1f, Mathf.SmoothStep(0f, 1f, normalized));
                ApplyTournamentAwardsAlpha(item, item.Fade ? normalized : 1f);
            }

            // 4. 延迟后触发冠军庆祝动画
            if (!awardsCelebrationTriggered && awardsCelebrationPlayer != null && awardsElapsed >= TournamentAwardsCelebrationDelay)
            {
                awardsCelebrationTriggered = true;
                // 5. 播放"开心"骨骼动画
                awardsCelebrationPlayer.Play("happiness");
                awardsCelebrationPlayer.RefreshPose();

                // 6. 如果有奖杯子骨骼，播放对应的奖杯动画
                var cupArmature = awardsCelebrationPlayer.GetChildArmature("effects stun");
                if (cupArmature != null && !string.IsNullOrEmpty(awardsCelebrationCupAnimation))
                {
                    cupArmature.StopAtStart(awardsCelebrationCupAnimation);
                }
            }
        }

        /// <summary>
        /// 将所有颁奖精灵和文字淡入到指定透明度。
        /// </summary>
        private static void ApplyTournamentAwardsAlpha(TournamentAwardsAnimatedItem item, float alpha)
        {
            // 1. 将透明度限制在 0-1 范围
            alpha = Mathf.Clamp01(alpha);
            // 2. 遍历所有精灵，按比例调整透明度
            if (item.SpriteRenderers != null)
            {
                for (var i = 0; i < item.SpriteRenderers.Length; i++)
                {
                    if (item.SpriteRenderers[i] == null)
                    {
                        continue;
                    }

                    var baseColor = item.SpriteBaseColors[i];
                    item.SpriteRenderers[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
                }
            }

            // 3. 遍历所有文字，按比例调整透明度
            if (item.TextMeshes != null)
            {
                for (var i = 0; i < item.TextMeshes.Length; i++)
                {
                    if (item.TextMeshes[i] == null)
                    {
                        continue;
                    }

                    var baseColor = item.TextBaseColors[i];
                    item.TextMeshes[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
                }
            }
        }

        /// <summary>
        /// 将像素空间的偏移量转换为相对于父 Transform 的本地空间偏移量。
        /// </summary>
        private static Vector3 PixelOffsetToLocal(Vector2 pixelOffset)
        {
            return new Vector3(pixelOffset.x * mlpConstants.UnitsPerPixel, -pixelOffset.y * mlpConstants.UnitsPerPixel, 0f);
        }

        /// <summary>
        /// 应用过冲缓动曲线。返回值会短暂超过 1 后再回落。
        /// </summary>
        private static float EaseOutBack01(float value)
        {
            value = Mathf.Clamp01(value);
            const float overshoot = 1.70158f;
            var shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        /// <summary>
        /// 返回给定比赛中失败者的角色 ID。
        /// </summary>
        private static int GetMatchLoserCharacterId(mlpTournamentMatchResult match)
        {
            if (match == null || !match.Completed)
            {
                return -1;
            }

            if (match.WinnerCharacterId == match.LeftCharacterId)
            {
                return match.RightCharacterId;
            }

            if (match.WinnerCharacterId == match.RightCharacterId)
            {
                return match.LeftCharacterId;
            }

            return -1;
        }

        /// <summary>
        /// 返回玩家排名对应的祝贺或安慰信息。
        /// </summary>
        private static string GetTournamentAwardsPlayerMessage(mlpTournamentData tournament)
        {
            switch (tournament.PlayerPlacement)
            {
                case 1:
                    return mlpSinglePlayerNarrative.LanternChampion;
                case 2:
                    return "RUNNER-UP";
                case 3:
                    return "THIRD PLACE";
                case 4:
                    return "FOURTH PLACE";
                default:
                    return $"{Mathf.Max(tournament.PlayerPlacement, 1)}TH PLACE";
            }
        }

        // 根据排名返回对应的强调色（金/银/铜）
        private static Color GetTournamentAwardsAccentColor(int placement)
        {
            switch (placement)
            {
                case 1:
                    return new Color32(0xD7, 0xF2, 0x4A, 0xFF);
                case 2:
                    return new Color32(0x9D, 0xF5, 0xFF, 0xFF);
                case 3:
                    return new Color32(0xFF, 0xB1, 0x48, 0xFF);
                default:
                    return new Color32(0xD8, 0xE5, 0xF6, 0xFF);
            }
        }

        // 根据排名和是否为玩家返回领奖台铭牌的颜色
        private static Color GetTournamentAwardsPlateTint(TournamentAwardsPlacement placement, bool champion)
        {
            if (placement.IsPlayer)
            {
                return champion
                    ? new Color(0.26f, 0.84f, 0.38f, 0.96f)
                    : placement.Placement == 2
                        ? new Color(0.2f, 0.72f, 0.88f, 0.94f)
                        : new Color(0.95f, 0.52f, 0.12f, 0.94f);
            }

            return champion
                ? new Color(0.12f, 0.34f, 0.24f, 0.9f)
                : placement.Placement == 2
                    ? new Color(0.08f, 0.22f, 0.32f, 0.9f)
                    : new Color(0.22f, 0.15f, 0.06f, 0.9f);
        }

        /// <summary>
        /// 返回排名的简短序数词，如 "1st"、"2nd"、"3rd"。
        /// </summary>
        private static string GetPlacementShortLabel(int placement)
        {
            switch (placement)
            {
                case 1:
                    return "1ST";
                case 2:
                    return "2ND";
                case 3:
                    return "3RD";
                default:
                    return $"{Mathf.Max(placement, 0)}TH";
            }
        }

        /// <summary>
        /// 在给定位置和缩放下绘制角色头像精灵。
        /// </summary>
        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder)
        {
            return CreateTournamentPortrait(name, characterId, x, y, targetPixels, sortingOrder, runtimeRoot);
        }

        /// <summary>
        /// 在给定位置和缩放下绘制角色头像精灵。
        /// </summary>
        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            // 1. 根据角色计算目标尺寸，获取头像精灵
            var targetSize = targetPixels * mlpPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var sprite = mlpPlayersData.GetCharacterPortraitSprite(characterId, targetSize);
            if (sprite == null)
            {
                return null;
            }

            // 2. 创建 GameObject 并添加 SpriteRenderer 组件
            var portrait = new GameObject(name);
            portrait.transform.SetParent(parent, false);
            var renderer = portrait.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // 3. 计算缩放比例，应用像素变换（含角色偏移）
            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetSize / Mathf.Max(1f, spritePixels);
            mlpRender.ApplyPixelTransform(
                portrait.transform,
                x,
                y + mlpPlayersData.GetCharacterPortraitOffsetY(characterId, sprite) * scale,
                0f,
                scale);
            return portrait;
        }

        /// <summary>
        /// 绘制锦标赛中使用的小型头像徽章。
        /// </summary>
        private void CreateTournamentMiniBadge(string key, int characterId, float x, float y, int sortingOrder)
        {
            mlpRender.PortraitBackplate(
                $"{key}_Badge",
                x,
                y,
                38f,
                sortingOrder,
                runtimeRoot,
                new Color(1f, 0.77f, 0.32f, 0.32f),
                new Color(0.01f, 0.025f, 0.06f, 0.94f),
                new Color(1f, 0.9f, 0.55f, 0.96f));

            CreateTournamentPortrait($"{key}_Portrait", characterId, x, y + 1f, 28f, sortingOrder + 3);
        }

        /// <summary>
        /// 在菜单界面上绘制一个带边框的面板矩形。
        /// </summary>
        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            // 1. 委托到带父对象参数的重载版本，默认挂在运行时根节点
            return CreateFramedPanel(name, frame, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        /// <summary>
        /// 在菜单界面上绘制一个带边框的面板矩形（可指定父对象）。
        /// </summary>
        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. 优先尝试用独立纹理创建面板（画质更好）
            var standalonePanel = TryCreateStandaloneFrame(name, frame, x, y, width, height, sortingOrder, tint, parent);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            // 2. 独立纹理不可用时，从图集加载精灵
            var panel = mlpRender.Sprite(name, mlpAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = mlpAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                var sourceWidth = Mathf.Max(1f, atlasFrame.SourceW);
                var sourceHeight = Mathf.Max(1f, atlasFrame.SourceH);
                panel.transform.localScale = new Vector3(
                    mlpConstants.UnitsPerPixel * width / sourceWidth,
                    mlpConstants.UnitsPerPixel * height / sourceHeight,
                    1f);
            }

            // 3. 按目标尺寸缩放精灵，并设置颜色
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        // 尝试用独立贴图创建带边框的面板，失败返回 null（回退到图集精灵）
        private static GameObject TryCreateStandaloneFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. 将边框名称解析为对应的纹理资源键
            var imageKey = ResolveStandaloneFrameImage(frame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            // 2. 从 Resources 加载纹理，失败则返回 null
            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            // 3. 创建图片精灵，缩放到目标尺寸并应用颜色
            var panel = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private GameObject CreateStandaloneImage(
            string name,
            string imageKey,
            float x,
            float y,
            float width,
            float height,
            int sortingOrder,
            Transform parent = null,
            Color? tint = null)
        {
            // 1. 检查图片资源键是否有效
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            // 2. 从 Resources 加载纹理，失败则返回 null
            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            // 3. 确定父对象，创建图片精灵
            var resolvedParent = parent ?? runtimeRoot;
            var image = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, resolvedParent);
            image.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            // 4. 缩放到目标尺寸，应用颜色（默认白色）
            image.GetComponent<SpriteRenderer>().color = tint ?? Color.white;
            return image;
        }

        // 将边框名称映射为对应的独立贴图资源键
        private static string ResolveStandaloneFrameImage(string frame)
        {
            return frame switch
            {
                "0bg100000" => mlpAssets.Images.Ui.FramePanelLarge,
                "MatchBack0001" => mlpAssets.Images.Ui.FrameMatchCardIdle,
                "MatchBack0002" => mlpAssets.Images.Ui.FrameMatchCardActive,
                "btn_bg0000" => mlpAssets.Images.Ui.MenuButtonPlate,
                _ => null
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 16 块：故事传说面板和漫画过场辅助
        // ═══════════════════════════════════════════════════════════════════

        // 创建故事漫画的传说面板（卷轴样式、木杆装饰、标题和正文文字）
        private void CreateStoryIntroLorePanel(mlpStoryPanelDefinition panel)
        {
            // 1. 检查面板是否有效且包含传说内容，无效则跳过
            if (panel == null || !panel.HasLore)
            {
                return;
            }

            // 2. 计算图标和标签的屏幕位置
            var loreIconX = StoryIntroLoreButtonX + StoryIntroLoreIconOffsetX;
            var loreIconY = StoryIntroLoreButtonY + StoryIntroLoreIconOffsetY;
            var loreLabelX = StoryIntroLoreButtonX + StoryIntroLoreLabelOffsetX;

            // 3. 创建可点击的传说按钮（透明背景，无默认标签）
            storyIntroLoreButton = new mlpMenuButton(
                "lore",
                StoryIntroLoreButtonX,
                StoryIntroLoreButtonY,
                StoryIntroLoreButtonHitWidth,
                StoryIntroLoreButtonHitHeight,
                ToggleStoryIntroLore,
                runtimeRoot,
                26,
                mlpTextStyle.TournamentAccent);
            storyIntroLoreButton.SetBackgroundVisible(false);
            storyIntroLoreButton.SetLabelVisible(false);
            menuButtons.Add(storyIntroLoreButton);
            // 4. 创建按钮旁边的自定义标签文字
            storyIntroLoreLabelObject = CreateStoryIntroLoreButtonLabel(
                "lore",
                loreLabelX,
                StoryIntroLoreButtonY + StoryIntroLoreLabelOffsetY);

            // 5. 创建美术元素根对象（图标和装饰线，面板打开时隐藏）
            storyIntroLoreArtRoot = new GameObject("StoryIntroLoreArt");
            storyIntroLoreArtRoot.transform.SetParent(runtimeRoot, false);
            // 6. 绘制传说图标背景光圈
            storyIntroLoreIconRenderer = CreateStandaloneImage(
                    "StoryIntroLoreIcon",
                    mlpAssets.Images.Ui.EmblemOrb,
                    loreIconX,
                    loreIconY,
                    StoryIntroLoreIconSize,
                    StoryIntroLoreIconSize,
                    23,
                    storyIntroLoreArtRoot.transform,
                    new Color(storyIntroAccentColor.r, storyIntroAccentColor.g, storyIntroAccentColor.b, 0.84f))
                ?.GetComponent<SpriteRenderer>();
            CreatePanel(
                "StoryIntroLoreGlyph",
                loreIconX,
                loreIconY - 2f,
                18f,
                13f,
                24,
                new Color(0.96f, 0.89f, 0.74f, 0.92f),
                storyIntroLoreArtRoot.transform);
            CreatePanel(
                "StoryIntroLoreGlyphLine1",
                loreIconX,
                loreIconY - 5f,
                10f,
                2f,
                25,
                new Color(0.42f, 0.25f, 0.10f, 0.9f),
                storyIntroLoreArtRoot.transform);
            CreatePanel(
                "StoryIntroLoreGlyphLine2",
                loreIconX,
                loreIconY - 2f,
                11f,
                2f,
                25,
                new Color(0.42f, 0.25f, 0.10f, 0.9f),
                storyIntroLoreArtRoot.transform);
            CreatePanel(
                "StoryIntroLoreGlyphLine3",
                loreIconX,
                loreIconY + 1f,
                9f,
                2f,
                25,
                new Color(0.42f, 0.25f, 0.10f, 0.9f),
                storyIntroLoreArtRoot.transform);

            // 7. 创建传说内容根对象（面板、木杆、文字等）
            storyIntroLoreRoot = new GameObject("StoryIntroLoreRoot");
            storyIntroLoreRoot.transform.SetParent(runtimeRoot, false);
            var loreRootTransform = storyIntroLoreRoot.transform;
            // 8. 绘制面板阴影和纸张背景
            CreatePanel(
                "StoryIntroLoreShadow",
                StoryIntroLorePanelX + 6f,
                StoryIntroLorePanelY + 6f,
                StoryIntroLorePanelWidth + 8f,
                StoryIntroLorePanelHeight + 10f,
                24,
                new Color(0.06f, 0.04f, 0.02f, 0.34f),
                loreRootTransform);
            CreateStandaloneImage(
                "StoryIntroLorePaper",
                mlpAssets.Images.Ui.PanelFillSoft,
                StoryIntroLorePanelX,
                StoryIntroLorePanelY,
                StoryIntroLorePanelWidth,
                StoryIntroLorePanelHeight,
                25,
                loreRootTransform,
                new Color(0.98f, 0.92f, 0.78f, 0.98f));
            CreatePanel(
                "StoryIntroLoreWarmWash",
                StoryIntroLorePanelX,
                StoryIntroLorePanelY,
                StoryIntroLorePanelWidth - 14f,
                StoryIntroLorePanelHeight - 18f,
                26,
                new Color(0.63f, 0.41f, 0.18f, 0.08f),
                loreRootTransform);
            // 9. 绘制上下卷轴木杆和四角端盖
            CreatePanel(
                "StoryIntroLoreRodTop",
                StoryIntroLorePanelX,
                StoryIntroLorePanelY - 134f,
                162f,
                10f,
                27,
                new Color(0.48f, 0.26f, 0.10f, 0.98f),
                loreRootTransform);
            CreatePanel(
                "StoryIntroLoreRodBottom",
                StoryIntroLorePanelX,
                StoryIntroLorePanelY + 134f,
                162f,
                10f,
                27,
                new Color(0.48f, 0.26f, 0.10f, 0.98f),
                loreRootTransform);
            CreatePanel(
                "StoryIntroLoreRodTopLeftCap",
                StoryIntroLorePanelX - 90f,
                StoryIntroLorePanelY - 134f,
                14f,
                14f,
                28,
                new Color(0.64f, 0.39f, 0.18f, 1f),
                loreRootTransform);
            CreatePanel(
                "StoryIntroLoreRodTopRightCap",
                StoryIntroLorePanelX + 90f,
                StoryIntroLorePanelY - 134f,
                14f,
                14f,
                28,
                new Color(0.64f, 0.39f, 0.18f, 1f),
                loreRootTransform);
            CreatePanel(
                "StoryIntroLoreRodBottomLeftCap",
                StoryIntroLorePanelX - 90f,
                StoryIntroLorePanelY + 134f,
                14f,
                14f,
                28,
                new Color(0.64f, 0.39f, 0.18f, 1f),
                loreRootTransform);
            CreatePanel(
                "StoryIntroLoreRodBottomRightCap",
                StoryIntroLorePanelX + 90f,
                StoryIntroLorePanelY + 134f,
                14f,
                14f,
                28,
                new Color(0.64f, 0.39f, 0.18f, 1f),
                loreRootTransform);
            CreatePanel(
                "StoryIntroLoreAccentLine",
                StoryIntroLorePanelX,
                StoryIntroLorePanelY - 108f,
                156f,
                4f,
                29,
                new Color(storyIntroAccentColor.r, storyIntroAccentColor.g, storyIntroAccentColor.b, 0.85f),
                loreRootTransform);

            // 10. 获取故事模式信息，显示页码、标题和正文
            var mode = mlpSinglePlayerNarrative.GetMode(storyIntroMode);
            var panelCount = mode.OpeningComic != null ? mode.OpeningComic.Length : 0;
            CreateToggleableStoryIntroText(
                "StoryIntroLorePageTag",
                $"PAGE {storyIntroPanelIndex + 1}/{Mathf.Max(1, panelCount)}",
                StoryIntroLorePanelX,
                StoryIntroLorePanelY - 118f,
                11,
                StoryIntroLorePageTagColor,
                TextAnchor.MiddleCenter,
                30,
                mlpTextStyle.TournamentAccent);
            CreateToggleableStoryIntroText(
                "StoryIntroLoreTitle",
                panel.LoreTitle,
                StoryIntroLorePanelX,
                StoryIntroLorePanelY - 82f,
                24,
                new Color32(0x4B, 0x26, 0x11, 0xFF),
                TextAnchor.MiddleCenter,
                30,
                mlpTextStyle.StoryScrollTitle);
            CreateToggleableStoryIntroText(
                "StoryIntroLoreBody",
                panel.LoreBody,
                StoryIntroLorePanelX - 86f,
                StoryIntroLorePanelY - 42f,
                14,
                new Color32(0x43, 0x2A, 0x16, 0xFF),
                TextAnchor.UpperLeft,
                30,
                mlpTextStyle.StoryScrollBody);

            // 11. 初始化为隐藏状态
            SetStoryIntroLoreVisibility(false);
        }

        // 切换传说面板的打开/关闭
        private void ToggleStoryIntroLore()
        {
            SetStoryIntroLoreVisibility(!storyIntroLoreOpen);
        }

        // 切换传说面板的显示/隐藏，同步更新按钮位置、标签和动画暂停状态
        private void SetStoryIntroLoreVisibility(bool isVisible)
        {
            // 1. 打开时保存并暂停动画，关闭时恢复之前的暂停状态
            if (isVisible)
            {
                storyIntroPauseBeforeLore = storyIntroPaused;
                storyIntroPaused = true;
            }
            else
            {
                storyIntroPaused = storyIntroPauseBeforeLore;
            }

            // 2. 更新传说面板打开状态标记
            storyIntroLoreOpen = isVisible;
            // 3. 显示/隐藏内容面板和美术元素（两者互斥）
            if (storyIntroLoreRoot != null)
            {
                storyIntroLoreRoot.SetActive(isVisible);
            }

            if (storyIntroLoreArtRoot != null)
            {
                storyIntroLoreArtRoot.SetActive(!isVisible);
            }

            // 4. 遍历切换所有传说文字对象的可见性
            for (var i = 0; i < storyIntroLoreTextObjects.Count; i++)
            {
                if (storyIntroLoreTextObjects[i] != null)
                {
                    storyIntroLoreTextObjects[i].SetActive(isVisible);
                }
            }

            // 5. 刷新按钮位置、标签文字和颜色
            RefreshStoryIntroLoreButtonLayout(isVisible);
            SetStoryIntroLoreButtonLabelText(isVisible ? "hide" : "lore");
            SetStoryIntroLoreButtonLabelColor(isVisible ? StoryIntroLoreOpenLabelColor : StoryIntroLoreClosedLabelColor);
            // 6. 切换图标颜色（打开时用暖色，关闭时用强调色）
            if (storyIntroLoreIconRenderer != null)
            {
                storyIntroLoreIconRenderer.color = isVisible
                    ? new Color(1f, 0.92f, 0.7f, 0.96f)
                    : new Color(storyIntroAccentColor.r, storyIntroAccentColor.g, storyIntroAccentColor.b, 0.84f);
            }

            // 7. 更新暂停按钮文字（打开时显示关闭，否则显示暂停/继续）
            RefreshStoryIntroPauseButtonLabel();
        }

        // 根据当前状态更新暂停按钮文字（pause/resume/close）
        private void RefreshStoryIntroPauseButtonLabel()
        {
            if (storyIntroPauseButton == null)
            {
                return;
            }

            storyIntroPauseButton.SetText(storyIntroLoreOpen ? "close" : storyIntroPaused ? "resume" : "pause");
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 17 块：连接线绘制（锦标赛对阵图的连接线段）
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 绘制连接两个对阵位置的水平线段。
        /// </summary>
        private void CreateHorizontalConnector(string name, float startX, float endX, float y, bool highlighted, int sortingOrder = 10, float thickness = TournamentConnectorThickness)
        {
            // 1. 计算左端位置和宽度
            var left = Mathf.Min(startX, endX);
            var width = Mathf.Abs(endX - startX);
            // 2. 绘制水平矩形线段，高亮时用橙色，否则用淡青色
            CreatePanel(
                name,
                left + width * 0.5f,
                y,
                width,
                thickness,
                sortingOrder,
                highlighted
                    ? new Color(1f, 0.64f, 0.15f, 0.85f)
                    : new Color(0.32f, 0.9f, 0.88f, 0.28f));
        }

        /// <summary>
        /// 绘制连接两个对阵位置的垂直线段。
        /// </summary>
        private void CreateVerticalConnector(string name, float x, float startY, float endY, bool highlighted, int sortingOrder = 10, float thickness = TournamentConnectorThickness)
        {
            // 1. 计算顶部位置和高度
            var top = Mathf.Min(startY, endY);
            var height = Mathf.Abs(endY - startY);
            // 2. 绘制垂直矩形线段，高亮时用橙色，否则用淡青色
            CreatePanel(
                name,
                x,
                top + height * 0.5f,
                thickness,
                height,
                sortingOrder,
                highlighted
                    ? new Color(1f, 0.64f, 0.15f, 0.85f)
                    : new Color(0.32f, 0.9f, 0.88f, 0.28f));
        }

        /// <summary>
        /// 绘制连接两个对阵位置的 L 形折线。
        /// </summary>
        private void CreateElbowConnector(string name, float startX, float startY, float endX, float endY, bool highlighted)
        {
            // 1. 计算中点 X 坐标
            var midX = (startX + endX) * 0.5f;
            // 2. 绘制水平段 + 垂直段 + 水平段，组成 L 形折线
            CreateHorizontalConnector($"{name}_H1", startX, midX, startY, highlighted);
            CreateVerticalConnector($"{name}_V", midX, startY, endY, highlighted);
            CreateHorizontalConnector($"{name}_H2", midX, endX, endY, highlighted);
        }

        /// <summary>
        /// 根据文本长度返回合适的字号，文本越长字号越小，防止超出可用宽度。
        /// </summary>
        private static int GetCompactFontSize(string text, int shortSize, int mediumSize, int longSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return shortSize;
            }

            if (text.Length <= 10)
            {
                return shortSize;
            }

            if (text.Length <= 14)
            {
                return mediumSize;
            }

            return longSize;
        }

        /// <summary>
        /// 清除所有颁奖动画数据，以便重新构建颁奖仪式。
        /// </summary>
        private void ResetTournamentAwardsState()
        {
            // 1. 清空动画列表，重置计时器和庆祝触发标记
            awardsAnimatedItems.Clear();
            awardsElapsed = 0f;
            awardsCelebrationTriggered = false;
            // 2. 清空庆祝动画的骨骼和奖杯引用
            awardsCelebrationPlayer = null;
            awardsCelebrationCupAnimation = null;
        }

        /// <summary>
        /// 为双人模式选择默认角色，确保双方使用不同的角色开局。
        /// </summary>
        private void SeedTwoPlayerSelection()
        {
            // 1. 读取对战存档数据
            var match = mlpInventory.Instance.MatchData;
            // 2. 初始化左侧玩家角色（校验 ID 合法性）
            versusLeftCharacterId = mlpPlayersData.SanitizeCharacterId(match.CharacterIds[0]);
            // 3. 初始化右侧玩家角色，确保与左侧不同
            versusRightCharacterId = mlpPlayersData.SanitizeCharacterId(match.CharacterIds[1], mlpPlayersData.StepCharacterId(versusLeftCharacterId, 1));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 第 18 块：清理和杂项工具方法
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// 销毁所有菜单和游戏对象，重置整个场景。
        /// </summary>
        private void ClearRuntime()
        {
            // 1. 关闭游戏核心并清空引用
            gameCore?.Shutdown();
            gameCore = null;
            // 2. 清空菜单按钮和各类辅助对象引用
            menuButtons.Clear();
            menuMusicButton = null;
            menuHelpButton = null;
            quickTestMenuToggleButton = null;
            quickTestMenuInfoButton = null;
            quickTestMenuInfoRoot = null;
            quickTestMenuInfoVisible = false;
            // 3. 重置故事介绍相关的暂停和传说面板状态
            storyIntroPauseButton = null;
            storyIntroLoreButton = null;
            storyIntroPaused = false;
            storyIntroLoreOpen = false;
            storyIntroPauseBeforeLore = false;
            storyIntroLoreRoot = null;
            storyIntroLoreArtRoot = null;
            storyIntroLoreLabelObject = null;
            storyIntroLoreIconRenderer = null;
            storyIntroLoreTextObjects.Clear();
            // 4. 释放原生文字图层资源
            nativeMenuTextLayer?.Dispose();
            nativeMenuTextLayer = null;
            // 5. 重置颁奖动画状态
            ResetTournamentAwardsState();
            // 6. 销毁运行时根节点下所有 GameObject
            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot.gameObject);
                runtimeRoot = null;
            }
        }

        /// <summary>
        /// 切换背景音乐的静音状态（开启或关闭）。
        /// </summary>
        private static void ToggleBackgroundMusic()
        {
            mlpAudio.Instance?.ToggleMusic();
        }

        /// <summary>
        /// 空占位回调，不执行任何操作。
        /// </summary>
        private static void NoOpAction()
        {
        }

        /// <summary>
        /// 返回音乐图标索引：0 = 正在播放，1 = 已静音（用于切换按钮图标）。
        /// </summary>
        private static int GetMusicIconIndex()
        {
            return mlpAudio.Instance != null && mlpAudio.Instance.MusicEnabled ? 0 : 1;
        }

        /// <summary>
        /// 返回锦标赛状态文字，如 "ROUND 3" 或 "GRAND FINAL"，用于对阵图标题显示。
        /// </summary>
        private static string GetTournamentStatusText(mlpTournamentData tournament)
        {
            if (tournament.Completed)
            {
                if (tournament.PlayerPlacement == 1)
                {
                    return mlpSinglePlayerNarrative.LanternChampion;
                }

                if (tournament.PlayerPlacement == 2)
                {
                    return "RUNNER-UP";
                }

                if (tournament.PlayerPlacement == 3)
                {
                    return "THIRD PLACE";
                }

                if (tournament.PlayerPlacement == 4)
                {
                    return "FOURTH PLACE";
                }

                return $"FINISHED #{tournament.PlayerPlacement}";
            }

            if (tournament.CurrentStage == mlpTournamentStage.RegularSeason)
            {
                return tournament.RegularSeasonCompleted
                    ? "FINALS READY"
                    : $"ROUND {tournament.CurrentRegularSeasonRoundIndex + 1}";
            }

            if (tournament.CurrentStage == mlpTournamentStage.SemiFinal)
            {
                return "FINAL FOUR";
            }

            if (tournament.CurrentStage == mlpTournamentStage.ThirdPlace)
            {
                return "3RD PLACE MATCH";
            }

            return "GRAND FINAL";
        }

        /// <summary>
        /// 返回角色名称，如果槽位为空则返回 "TBD"（待定）。
        /// </summary>
        private static string CharacterNameOrTbd(int characterId)
        {
            return characterId >= 0 ? mlpPlayersData.GetCharacterName(characterId) : "TBD";
        }

        /// <summary>
        /// 切换到下一个或上一个角色 ID，到达末尾时自动循环回到开头。
        /// </summary>
        private static int WrapCharacter(int currentCharacterId, int direction)
        {
            return mlpPlayersData.StepCharacterId(currentCharacterId, direction);
        }
    }
}
