// Game launcher and menu interface controller
// Manage all interfaces from the main menu to the start of the match: select 1 or 2 player mode, choose character and basketball skin, choose difficulty, enter adventure mode or tournament, display story comic, tournament matchup map and award screen. Also responsible for creating the camera and audio systems.

//  worldX = (x * 4/3 - 533) / 100
//  worldY = (320 - y * 4/3) / 100

using System.Text;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Game Launcher and Menu Controller: Manage all interfaces from the main menu to matches - selecting modes, characters, basketball skins, difficulty, and adventure/tournament flow control. Also responsible for creating the camera and audio systems.
    /// </summary>
    // ═══════════════════════════════════════════════════════════════════
    // Block 1: Class Definitions, Enumerations, Data Structures, and Constants

    // ═══════════════════════════════════════════════════════════════════
    public sealed class mlpGameBootstrap : MonoBehaviour
    {
        private static mlpGameBootstrap activeInstance;

        //A "map" of the entire script, listing all possible interfaces
        private enum mlpBootstrapScreen
        {
            PlayerCount,                 // Select the number of players (1 player/2 players)

            MatchType,                   // Select race type (Quick/Adventure/Tournament)

            SinglePlayerCharacterSetup,  // Select a character in single player mode
            StoryIntro,                  // Story comic cutscene

            AdventurePreview,            // Adventure mode trailer page

            AdventureMap,                // Adventure mode treasure map

            AdventureResult,             // Adventure level results (pass/fail)

            SinglePlayerSetup,           // Single player pre-match settings (ball selection, difficulty)

            TwoPlayerSetup,              // Pre-match setup for doubles
            TrainingSetup,               // Training mode settings

            TournamentSetup,             // Tournament settings

            TournamentBracket,           // Championship matchup chart

            TournamentComplete,          // tournament ends

            TournamentAwards             // Championship Awards Ceremony

        }

        // Ranking information for each player in the awards ceremony

        private sealed class TournamentAwardsPlacement
        {
            public int Placement;            // Ranking (1=champion, 2=runner-up, 3=third place)

            public int CharacterId;          // Role ID

            public bool IsPlayer;            // Is it a character controlled by the player?

            public string CupAnimation;      // Trophy animation name

            public Color AccentColor;        // Accent color (for ranking logos)

            public Color GlowColor;          // Glow effect color

        }

        // Definition of a single animation element in an awards ceremony (for entrance animation)

        private sealed class TournamentAwardsAnimatedItem
        {
            public Transform Root;           // Transform of animation target object

            public Vector3 TargetLocalPosition; // local position when animation ends

            public Vector3 TargetLocalScale;    // Local zoom at end of animation

            public Vector3 StartLocalOffset;    // Offset when animation starts (sliding in from far away)

            public float Delay;             // Animation delay playback time (seconds)

            public float Duration;          // Animation duration (seconds)

            public float StartScale = 1f;   // Zoom when animation starts

            public bool Fade = true;        // Is there a fade-in effect?

            public SpriteRenderer[] SpriteRenderers;  // Array of sprite renderers to fade in

            public Color[] SpriteBaseColors;          // The original color of the sprite (used to calculate transparency)

            public TextMesh[] TextMeshes;             // Text grid array that needs to be faded in
            public Color[] TextBaseColors;            // The original color of the text

        }

        // Display data for a single cell in the tournament standings

        private sealed class TournamentStandingsCellViewModel
        {
            public string TopText;           // Text above (e.g. match result "W" or "L")

            public string BottomText;        // Text below (e.g. score "21-18")

            public Color TopColor = Color.white;    // Text color above

            public Color BottomColor = Color.white; // Text color below

        }

        // Display data for individual rows of players in the tournament standings

        private sealed class TournamentStandingsRowViewModel
        {
            public int Seed;                 // seed ranking

            public int CharacterId;          // Role ID

            public bool IsPlayer;            // Is it a player

            public bool IsCurrent;           // Is it the player currently being viewed?

            public bool IsChampion;          // Is it a champion?

            public bool IsFinalist;          // Whether to enter the finals

            public int Wins;                 // Number of wins

            public int Losses;               // Number of negative games

            public string PercentageText;    // Win rate text (e.g. "75.0%")

            public string StatusText;        // Status text (such as "promotion", "elimination")

            public Color StatusColor;        // status text color

            public TournamentStandingsCellViewModel SemiCell = new TournamentStandingsCellViewModel();  // semi-final cells

            public TournamentStandingsCellViewModel FinalCell = new TournamentStandingsCellViewModel(); // final cell

        }

        // Menu button list (records all clickable buttons on the current interface)

        private readonly System.Collections.Generic.List<mlpMenuButton> menuButtons = new System.Collections.Generic.List<mlpMenuButton>();
        // Awards ceremony animation object list (used to drive the entrance animation when awarding awards)

        private readonly System.Collections.Generic.List<TournamentAwardsAnimatedItem> awardsAnimatedItems = new System.Collections.Generic.List<TournamentAwardsAnimatedItem>();

        // === Character/Ball Selector Layout Constants ===

        private const float SelectorHeaderY = 126f;          // Y coordinate of selector title

        private const float SelectorArrowY = 258f;           // Y coordinate of left and right arrows
        private const float SelectorArrowOffsetX = 74f;      // X offset of arrow from center

        private const float SelectorArrowSize = 36f;         // Arrow size


        // === Character preview layout constants ===

        private const float PreviewScaleFactor = 0.56f;      // Preview the overall zoom of the character

        private const float PreviewShadowYOffset = 24f;      // Y offset of shadow

        private const float PreviewShadowScale = 0.42f;      // Shadow scaling

        private const float PreviewArmatureYOffset = -24f;   // Y offset for skeletal animation

        private const float PreviewArmatureScale = 0.82f;    // Skeletal animation scaling


        // === Menu common layout constants ===

        private const float NativeUiAspect = mlpConstants.DisplayW / (float)mlpConstants.DisplayH; // Native UI aspect ratio

        private const float MenuLogoCenterY = 96f;           // Menu Logo Center Y coordinate

        private const float MenuLogoMaxWidth = 280f;         // Logo maximum width

        private const float MenuLogoMaxHeight = 188f;        // Logo maximum height

        // === Tournament Match Map - Panel Layout ===

        private const float TournamentBoardX = mlpConstants.Width2; // Center of the matchup panel X

        private const float TournamentBoardY = 250f;           // Matchup panel center Y

        private const float TournamentBoardScale = 0.92f;      // Overall scaling of the map


        // === Tournament Matchup - Contestant Entries ===

        private const float TournamentEntrantBadgeX = 252f;    // Participant Badge X Coordinate

        private const float TournamentEntrantNameX = 282f;     // Contestant name X coordinate

        private const float TournamentEntrantBadgeScale = 0.32f; // Badge scaling

        private const float TournamentEntrantGlowScale = 0.38f;  // Glow effect scaling

        private const float TournamentEntrantPortraitPixels = 36f; // Avatar pixel size


        // === Tournament Matchup - Match Panel ===
        private const float TournamentMatchPanelScale = 0.87f; // Match panel zoom

        private const float TournamentMatchWidth = 132f * TournamentMatchPanelScale;  // Match panel width

        private const float TournamentMatchHeight = 102f * TournamentMatchPanelScale; // Competition panel height

        private const float TournamentMatchHalfWidth = TournamentMatchWidth * 0.5f;   // Match panel half width

        private const float TournamentSemiPanelX = 383f;       // Semifinal Panel X

        private const float TournamentSemiTopY = 184f;        // First half semi-final Y

        private const float TournamentSemiBottomY = 314f;     // Lower half semi-finals Y

        private const float TournamentFinalPanelX = 521f;     // Finals Panel X

        private const float TournamentFinalPanelY = 249f;     // Final Panel Y

        private const float TournamentMatchRowOffset = 16f;   // Match panel inline offset

        private const float TournamentConnectorThickness = 6f; // Connection line thickness


        // === Tournament - Summary of Results ===

        private const float TournamentSummaryY = 402f;         // Summary area Y coordinate


        // === Championship – Standings ===

        private const float TournamentStandingsBoardX = 236f;  // Scoreboard Panel X

        private const float TournamentStandingsBoardY = 226f;  // Scoreboard panel Y

        private const float TournamentStandingsBoardScale = 0.72f; // Standings zoom

        private const float TournamentStandingsTitleY = 121f;  // Standings title Y

        private const float TournamentStandingsTableX = TournamentStandingsBoardX; // Form X

        private const float TournamentStandingsHeaderY = 154f; // Header Y

        private const float TournamentStandingsHeaderHeight = 24f; // Head height

        private const float TournamentStandingsRowStartY = 186f; // First line Y

        private const float TournamentStandingsRowSpacing = 34f; // line spacing

        private const float TournamentStandingsRowHeight = 30f; // row height
        private const float TournamentStandingsTableWidth = 248f; // Total table width

        private const float TournamentStandingsIndexWidth = 22f; // Rank column width

        private const float TournamentStandingsTeamWidth = 100f; // The team ranks wide

        private const float TournamentStandingsWinsWidth = 32f; // Win column width

        private const float TournamentStandingsLossesWidth = 32f; // Negative field column width

        private const float TournamentStandingsPctWidth = 62f; // Win rate column width

        private const float TournamentStandingsBadgeScale = 0.22f; // Standing Badge Zoom

        private const float TournamentStandingsGlowScale = 0.28f; // Scoreboard glow zoom

        private const float TournamentStandingsPortraitPixels = 28f; // Scoreboard avatar pixels


        // === Championship – Knockout Match Map ===

        private const float TournamentFinalsTitleX = 574f;     // Knockout Title X

        private const float TournamentFinalsTitleY = 120f;     // Knockout Title Y

        private const float TournamentBracketPanelScale = 0.9f; // Knockout Panel Zoom

        private const float TournamentBracketPanelWidth = 132f * TournamentBracketPanelScale;  // Panel width

        private const float TournamentBracketPanelHeight = 102f * TournamentBracketPanelScale; // Panel height

        private const float TournamentBracketFinalX = 574f;    // Final position X

        private const float TournamentBracketFinalY = 172f;    // Final position Y

        private const float TournamentBracketSemiLeftX = 450f; // Left half semi-finals X

        private const float TournamentBracketSemiRightX = 698f; // Right half semi-final X

        private const float TournamentBracketSemiY = 244f;    // Semi-final Y

        private const float TournamentBracketPlacementX = 574f; // Ranking display X

        private const float TournamentBracketPlacementY = 332f; // Ranking display Y

        private const float TournamentBracketRowOffset = 18f;  // row offset

        private const float TournamentBracketBadgeScale = 0.2f; // Knockout Badge Zoom
        private const float TournamentBracketGlowScale = 0.26f; // knockout glow zoom

        private const float TournamentBracketPortraitPixels = 24f; // Knockout avatar pixels


        // === Championship Award Ceremony Layout ===

        private const float TournamentAwardsShowcaseY = 244f;  // Exhibition area Y

        private const float TournamentAwardsShowcaseWidth = 560f; // Exhibition area width

        private const float TournamentAwardsShowcaseHeight = 322f; // High display area

        private const float TournamentAwardsPlaqueY = 132f;    // medal plaque Y

        private const float TournamentAwardsPodiumY = 376f;    // Podium Y

        private const float TournamentAwardsPodiumWidth = 428f; // podium width

        private const float TournamentAwardsPodiumHeight = 170f; // podium height

        private const float TournamentAwardsChampionX = mlpConstants.Width2; // Champion

        private const float TournamentAwardsChampionY = 296f;  // Champion Y

        private const float TournamentAwardsLeftX = mlpConstants.Width2 - 114f; // Left player X

        private const float TournamentAwardsLeftY = 314f;     // Player Y on the left

        private const float TournamentAwardsRightX = mlpConstants.Width2 + 114f; // Player on the right X

        private const float TournamentAwardsRightY = 320f;    // Player Y on the right

        private const float TournamentAwardsChampionScale = 0.82f; // Champion Zoom

        private const float TournamentAwardsSideScale = 0.78f; // Players on both sides zoom

        private const float TournamentAwardsArmatureScale = 0.82f; // Awards skeleton animation scaling

        private const float TournamentAwardsArmatureYOffset = -18f; // Award Bone Y Offset

        private const float TournamentAwardsCelebrationDelay = 0.66f; // Celebrate animation delay

        // === Menu top buttons (Music, Help) ===

        private const float MenuTopButtonY = 44f;             // Top button Y coordinate

        private const float MenuMusicButtonX = 770f;          // Music Button X
        private const float MenuHelpButtonX = 706f;           // Help button

        private const float MenuTopButtonSize = 60f;          // Top button size

        private const float MenuTopIconPixels = 58f;          // Top icon pixel size


        // === Quick test menu (for development and debugging) ===

        private const float QuickTestMenuLabelX = 666f;       // Test menu label X

        private const float QuickTestMenuControlY = 442f;     // Test menu control Y

        private const float QuickTestMenuToggleX = 706f;      // Test switch X

        private const float QuickTestMenuToggleWidth = 58f;   // Test switch width

        private const float QuickTestMenuToggleHeight = 34f;  // test switch high

        private const float QuickTestMenuInfoButtonX = 758f;  // Test information button X

        private const float QuickTestMenuInfoButtonSize = 32f; // Test info button size

        private const float QuickTestMenuInfoPanelX = 666f;   // Test Information Panel X

        private const float QuickTestMenuInfoPanelY = 356f;   // Test information panel Y

        private const float QuickTestMenuInfoPanelWidth = 220f; // Test information panel width

        private const float QuickTestMenuInfoPanelHeight = 94f; // Test information panel height

        // === Adventure Mode - Map Panel ===

        private const float AdventureMapPanelX = 306f;        // Map panel X

        private const float AdventureMapPanelY = 238f;        // Map panel Y

        private const float AdventureMapPanelWidth = 574f;    // Map panel width

        private const float AdventureMapPanelHeight = 348f;   // Map panel height

        private const float AdventureMechanicInfoY = 421f;    // How to play Y

        private const float AdventureMechanicInfoWidth = 540f; // Game instructions are wide

        private const float AdventureMechanicInfoHeight = 18f; // High gameplay instructions


        // === Adventure Mode - Level Poster ===
        private const float AdventurePosterX = 702f;          // Poster

        private const float AdventurePosterY = 270f;          // Poster Y

        private const float AdventurePosterWidth = 184f;      // Poster width

        private const float AdventurePosterHeight = 292f;     // Poster high


        // === Adventure Mode - Route Nodes ===

        private const float AdventureNodeWidth = 68f;         // node width

        private const float AdventureNodeHeight = 78f;        // node height


        // === Single player mode card selection (Adventure/Tournament two cards) ===

        private const float SinglePlayerModeCardY = 300f;     // Card Y

        private const float SinglePlayerModeCardWidth = 318f;  // card width

        private const float SinglePlayerModeCardHeight = 254f; // Card high

        private const float SinglePlayerModeLeftCardX = 232f;  // Left Card X (Adventure)

        private const float SinglePlayerModeRightCardX = 568f; // Right Card X (Tournament)


        // === Story Comic Panel ===

        private const float StoryPanelX = mlpConstants.Width2; // Comic Panel Center X

        private const float StoryPanelY = 260f;               // Comic panel Y

        private const float StoryPanelWidth = 610f;           // Comic panel width

        private const float StoryPanelHeight = 264f;          // comic panel height


        // === Story comic animation (movie-style playback effect) ===

        private const float StoryCinematicWidth = mlpConstants.Width; // Cinema mode wide

        private const float StoryCinematicHeight = 480f;      // Cinema mode high

        private const float StoryCinematicPageSeconds = 3.0f;  // Number of seconds per page

        private const float StoryCinematicFadeSeconds = 0.42f; // Page turn fade in and out seconds

        private const float StoryCinematicPanPixels = 14f;     // Amount of pixels to shift
        private const float StoryCinematicZoomAmount = 0.035f; // Zoom amount


        // === Story comic cutscene UI elements ===

        private const float StoryIntroTitleY = 28f;           // Title Y

        private const float StoryIntroCaptionY = 466f;        // Subtitle Y

        private const float StoryIntroPauseX = 54f;           // Pause button

        private const float StoryIntroPauseY = 456f;          // Pause button Y

        private const float StoryIntroSkipX = 748f;           // Skip button

        private const float StoryIntroSkipY = 456f;           // Skip button Y


        // === Story Legend Panel Button ===

        private const float StoryIntroLoreButtonX = 776f;     // Legend Button X

        private const float StoryIntroLoreButtonY = 234f;     // Legend Button Y

        private const float StoryIntroLoreOpenButtonX = StoryIntroLorePanelX;   // Open back button X

        private const float StoryIntroLoreOpenButtonY = StoryIntroLorePanelY + 108f; // Open back button Y

        private const float StoryIntroLoreButtonHitWidth = 112f;  // Button click area width

        private const float StoryIntroLoreButtonHitHeight = 80f;  // Button click area high

        private const float StoryIntroLoreIconOffsetX = 0f;   // Icon X Offset

        private const float StoryIntroLoreIconOffsetY = -18f;  // Icon Y Offset

        private const float StoryIntroLoreLabelOffsetX = 0f;  // Label X offset

        private const float StoryIntroLoreLabelOffsetY = 18f;  // Label Y offset

        private const float StoryIntroLoreOpenLabelOffsetX = 0f;  // Label X offset after opening

        private const float StoryIntroLoreOpenLabelOffsetY = 0f;  // Label Y offset after opening

        private const float StoryIntroLoreIconSize = 42f;     // icon size


        // === Story Legend Panel Layout ===

        private const float StoryIntroLorePanelX = 638f;      // Legend Panel X
        private const float StoryIntroLorePanelY = 236f;      // Legend Panel Y

        private const float StoryIntroLorePanelWidth = 228f;  // Legend panel width

        private const float StoryIntroLorePanelHeight = 304f; // Legend panel height


        // === Story Legend Panel Color ===

        private static readonly Color StoryIntroLoreClosedLabelColor = new Color(0.98f, 0.95f, 0.86f, 0.98f); // Label color when closed

        private static readonly Color StoryIntroLoreOpenLabelColor = new Color32(0x72, 0x43, 0x1B, 0xFF);     // Label color when opened

        private static readonly Color StoryIntroLorePageTagColor = new Color32(0x8B, 0x5B, 0x2B, 0xFF);      // Page label color


        // === Old menu background ===

        private const float LegacyMenuBackgroundWidth = 1398f; // old background wide

        private const float LegacyMenuBackgroundHeight = 480f; // old background high

        private const float LegacyTintPanelSourcePixels = 10f; // Old shading panel source pixels


        // === Ball Skin Selector Layout ===

        private const float OptionBallHeaderY = 208f;         // Ball option title Y

        private const float OptionBallPreviewY = 232f;        // ball preview Y

        private const float OptionBallLabelY = 260f;          // Ball name tag Y

        private const float BallSelectorArrowOffsetX = 68f;   // Ball Selection Arrow X Offset

        private const float BallSelectorArrowSize = 34f;      // Ball selection arrow size

        private const float BallPreviewPixels = 50f;          // Ball preview pixel size


        // === Two-player mode ball panel ===

        private const float TwoPlayerBallPanelY = 360f;       // Double ball panel Y

        private const float TwoPlayerBallHeaderY = 320f;      // Double ball title Y

        private const float TwoPlayerBallPreviewY = 356f;     // Double ball preview Y

        private const float TwoPlayerBallLabelY = 394f;       // Double ball tag Y

        private const float TwoPlayerBallPanelWidth = 168f;   // Double ball panel width
        private const float TwoPlayerBallPanelHeight = 148f;  // Double ball panel height


        // === Runtime core objects ===

        private Transform runtimeRoot;                       // The root node of the current interface (destroyed and rebuilt when switching interfaces)

        private mlpGameCore gameCore;                        // Game core controller (created when the game starts and destroyed when the game ends)

        private Camera mainCamera;                           // main camera

        private mlpFixedResolutionPresenter fixedResolutionPresenter; // Fixed resolution manager

        private mlpBootstrapScreen currentScreen;            // Current screen state


        // === Player selection status ===

        private mlpParticipantMode pendingParticipantMode = mlpParticipantMode.OnePlayer; // Player modes to be confirmed

        private int quickCharacterId;                        // Quick Match Selected Character ID

        private int trainingCharacterId;                     // Character ID selected in training mode

        private int tournamentCharacterId;                   // Tournament selected character ID

        private int versusLeftCharacterId;                   // Left player character ID in two-player mode

        private int versusRightCharacterId;                  // Player character ID on the right side of two-player mode

        private mlpBallSelection quickBallSelection;         // Ball skin selected for quick matches

        private mlpBallSelection trainingBallSelection;      // Ball skin selected in training mode

        private mlpBallSelection tournamentBallSelection;    // Tournament ball skin

        private mlpBallSelection versusBallSelection;        // Ball skin selected for two-player mode


        // === Award Ceremony Animation Status ===

        private float awardsElapsed;                         // The award animation has been played for a long time

        private bool awardsCelebrationTriggered;             // Whether the celebration animation has been triggered

        private DBLiteArmature awardsCelebrationPlayer;      // Character skeleton celebrating animation

        private string awardsCelebrationCupAnimation;        // Trophy animation name


        // === Menu top button ===

        private mlpIconButton menuMusicButton;               // Music switch button
        private mlpIconButton menuHelpButton;                // help button


        // === Quick test menu (for development and debugging) ===

        private mlpMenuButton quickTestMenuToggleButton;     // Test mode switch

        private mlpMenuButton quickTestMenuInfoButton;       // Test information button

        private GameObject quickTestMenuInfoRoot;            // Test the information panel root object

        private bool quickTestMenuInfoVisible;               // Test whether the information panel is visible


        // === Native UI and viewports ===

        private mlpNativeMenuTextLayer nativeMenuTextLayer;  // Native menu text layer

        private bool usingNativeUiPresentation;              // Whether to use native UI rendering

        private int viewportScreenWidth = -1;                // Viewport width cache

        private int viewportScreenHeight = -1;               // Viewport height cache


        // === Story comic cutscene status ===

        private mlpSinglePlayerNarrativeMode storyIntroMode = mlpSinglePlayerNarrativeMode.Adventure; // Current story type

        private int storyIntroPanelIndex;                    // Current comic page number

        private System.Action storyIntroContinueAction;      // Continuing callback after the comic ends

        private System.Action storyIntroCancelAction;        // Callback when comic canceled

        private mlpMenuButton storyIntroPauseButton;         // comic pause button

        private mlpMenuButton storyIntroLoreButton;          // Legend panel button

        private bool storyIntroPaused;                       // Is the comic paused?

        private bool storyIntroLoreOpen;                     // Whether the legend panel is open

        private bool storyIntroPauseBeforeLore;              // Whether it was paused before opening the legend

        private float storyIntroElapsed;                     // Comic play time

        private GameObject storyIntroImageObject;            // Current comic image object

        private Vector3 storyIntroImageBaseScale;            // Comic picture original scaling

        private SpriteRenderer storyIntroImageRenderer;      // Comic image renderer
        private Color storyIntroAccentColor = Color.white;   // comic accent color

        private GameObject storyIntroLoreRoot;               // Legend panel root object

        private GameObject storyIntroLoreArtRoot;            // legend illustration root object

        private GameObject storyIntroLoreLabelObject;        // Legend button label object

        private SpriteRenderer storyIntroLoreIconRenderer;   // Legend button icon renderer

        private readonly System.Collections.Generic.List<GameObject> storyIntroLoreTextObjects = new System.Collections.Generic.List<GameObject>(); // Legend text object list


        // === Adventure Mode Status ===

        private int adventureSelectedLevelIndex;             // The currently selected adventure level index

        private Texture2D adventureTreasureMapTexture;       // Treasure map texture cache


        /// <summary>
        /// When the game starts, it sets up the camera, audio system and displays the main menu.

        /// </summary>
        // ═══════════════════════════════════════════════════════════════
        // Block 2: Lifecycle Methods

        // Awake initializes camera/audio/archive, Update schedules game and menu logic every frame

        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// When the game starts, it sets up the camera, audio system and displays the main menu.

        /// </summary>
        private void Awake()
        {
            // 1. Save the singleton reference and obtain or create the main camera

            activeInstance = this;
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            // 2. Set the camera to orthographic mode (2D game) and adjust the size to fit the pixel resolution

            mainCamera.orthographic = true;     //Orthogonal mode, no perspective effect near large and far small

            mainCamera.orthographicSize = mlpConstants.GameH / (2f * mlpConstants.PixelsPerUnit);       //Calculate how much range the camera can see based on the game design resolution.

            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = Color.black;

            // 3. Get or add fixed resolution components (make sure the screen ratio is consistent)

            fixedResolutionPresenter = GetComponent<mlpFixedResolutionPresenter>();
            if (fixedResolutionPresenter == null)
            {
                fixedResolutionPresenter = gameObject.AddComponent<mlpFixedResolutionPresenter>();
            }

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            EnableNativeMenuPresentation();

            // 4. Create the runtime root node and audio system

            runtimeRoot = new GameObject("mlpRuntime").transform;
            mlpAudio.Create(transform);

            // 5. Read the last selected character and basketball skin from the archive

            var inventory = mlpInventory.Instance;
            quickCharacterId = mlpPlayersData.SanitizeCharacterId(inventory.SelectedQuickCharacterId);
            trainingCharacterId = mlpPlayersData.SanitizeCharacterId(inventory.SelectedTrainingCharacterId, quickCharacterId);
            tournamentCharacterId = mlpPlayersData.SanitizeCharacterId(inventory.SelectedTournamentCharacterId, quickCharacterId);
            quickBallSelection = inventory.SelectedQuickBallSelection;
            trainingBallSelection = inventory.SelectedTrainingBallSelection;
            tournamentBallSelection = inventory.SelectedTournamentBallSelection;
            versusBallSelection = inventory.SelectedVersusBallSelection;
            SeedTwoPlayerSelection();

            // 6. Display the main menu (select 1 person/2 people/Coach/Training)

            ShowPlayerCountMenu();
        }

        /// <summary>
        /// Unity life cycle callback: automatically called when this GameObject is destroyed.

        /// Responsible for cleaning up global singleton references and preventing other code from accessing destroyed objects (wild pointers);

        /// At the same time, the treasure map texture generated when running in adventure mode is released to avoid GPU memory leaks.

        /// </summary>
        private void OnDestroy()
        {
            // Clear the singleton reference: only set it to empty when the currently active instance is destroyed.
            // This prevents the new instance from being misunderstood after it has been replaced.

            if (activeInstance == this)
            {
                activeInstance = null;
            }

            // Unleashed treasure map textures for Adventure mode.

            // This texture is dynamically generated at runtime (not a project resource file) and will not be automatically recycled with the scene.

            // Destroy must be called manually to release GPU memory, otherwise memory leaks will occur.

            if (adventureTreasureMapTexture != null)
            {
                Destroy(adventureTreasureMapTexture);
                adventureTreasureMapTexture = null;
            }
        }

        /// <summary>
        /// Static entry method for launching tutorials from the help panel.

        /// Other scripts (such as the help panel's "Start Tutorial" button) do not need to hold a reference to mlpGameBootstrap,
        /// Directly calling this static method can trigger the tutorial process.
        /// </summary>
        /// <returns>Returns true if the tutorial is successfully started, false if no bootstrap instance is found. </returns>
        public static bool TryStartTutorialFromHelp()
        {
            // The cached singleton reference is used first; if it is empty (for example, the scene is rebuilt),
            // Then search in the scene through FindObjectOfType as a cover-up solution.

            var bootstrap = activeInstance != null ? activeInstance : FindObjectOfType<mlpGameBootstrap>();
            if (bootstrap == null)
            {
                return false;
            }

            // Call the instance method to actually execute the logic of entering the tutorial from the help panel.

            bootstrap.StartTutorialFromHelpPanel();
            return true;
        }

        /// <summary>
        /// Unity life cycle callback: automatically called once every frame.
        /// This is the "big scheduler" for the entire game - based on the current game state (in-match/menu/cutscenes)
        /// Distribute different processing logic. The overall judgment is divided into three levels:
        ///   Level 1: Is the game on? → Delegate to gameCore to update the game logic
        ///   Second level: Is the help panel open? → Pause menu interaction and only keep the help panel
        ///   The third layer: normal menu/cut scene → update button interaction, animation, key response

        /// </summary>
        private void Update()
        {
            // =====================================================================
            // The first level of judgment: Is the game going on?
            // gameCore is not null indicating that there is a game in progress (quick match/tournament/adventure/training, etc.)

            // =====================================================================
            if (gameCore != null)
                {
                // Delegate updates to gameCore, which handles core game logic such as physics, AI, timing, and scoring.

                // Time.deltaTime is the number of seconds elapsed from the previous frame to this frame, used for frame rate-independent timing.

                gameCore.Update(Time.deltaTime);

                // --- Check whether the game ends naturally (time is up/winner is determined) ---

                // AdvanceFlowRequested is a flag set by gameCore at the end of the game.

                if (gameCore.AdvanceFlowRequested)
                {
                    var inventory = mlpInventory.Instance;

                    // Adventure mode: clear the game scene and update the adventure progress (win to advance the level, lose to try again),
                    // Then the result interface of this level is displayed.

                    if (inventory.IsAdventureActive)
                    {
                        var playerWon = inventory.MatchData.WhoWins() < 0; // < 0 means the player wins

                        ClearRuntime();                           // Destroy all runtime objects of the match scene

                        inventory.AdvanceAdventure(playerWon);    // Update adventure save (advance/retry)

                        ShowAdventureResult(playerWon);           // Display the "Victory/Failure" result interface

                        return;
                    }

                    // Tournament Mode: Clear the game scene and advance the tournament map,

                    // Then the next round’s battle map interface is displayed.

                    ClearRuntime();
                    inventory.AdvanceTournament();                 // Advance the tournament to the next round

                    ShowTournamentBracket();                       // Show matchup map

                    return;
                }

                // --- Check whether the player actively pressed the return key to exit the game ---

                // ReturnToMenuRequested is the flag set by gameCore when the player requests to exit.

                if (gameCore.ReturnToMenuRequested)
                {
                    var inventory = mlpInventory.Instance;
                    // Remember what mode you were in before, because ClearRuntime will destroy gameCore.

                    // These statuses can no longer be queried afterward.

                    var tournamentWasActive = inventory.IsTournamentActive;
                    var adventureWasActive = inventory.IsAdventureActive;
                    ClearRuntime(); // Destroy the game scene


                    // Exit the adventure mode midway → return to the adventure map (you can choose other levels)

                    if (adventureWasActive)
                    {
                        ShowAdventureMap();
                        return;
                    }

                    // Exiting the tournament → Abandon the entire tournament and return to the main menu

                    if (tournamentWasActive)
                    {
                        inventory.AbandonTournament();
                        ShowPlayerCountMenu();
                        return;
                    }

                    // Training/tutorial mode exit → Check if there are any pending tutorial actions

                    if (HandlePendingTutorialAction())
                    {
                        return;
                    }

                    // Normal Match (Quick Match/Double) Exit → Return to Main Menu

                    ShowPlayerCountMenu();
                }

                // The game is still in progress, or the exit logic has just been processed, and the following menu logic will not be executed.

                return;
            }

            // Second level judgment: Is the help panel open?

            // The help panel is a modal dialog box that suspends all menu interactions when it is opened to prevent misoperations.

            var helpVisible = mlpHelpPanel.IsAnyOpen;
            // Hide the native text layer when the help panel is opened to avoid overlapping text

            nativeMenuTextLayer?.SetVisible(!helpVisible);

            if (helpVisible)
            {
                return; // While the help panel is open, no menu input is processed

            }

            // Third level judgment: normal menu/cutscene update

            // Updated tournament award ceremony animation sequences (character celebrations, trophy animations, etc.)

            UpdateTournamentAwardsSequence(Time.deltaTime);
            // Refresh the viewport size of the native menu text layer to adapt to window size changes

            RefreshNativeMenuViewport();

            // If you are currently in the story comic cutscene interface, update the comic animation (page turning, fade in and fade out, etc.).

            // UpdateStoryIntroCinematic returns true to indicate that the comic is still playing and subsequent menu logic is skipped.

            if (currentScreen == mlpBootstrapScreen.StoryIntro && UpdateStoryIntroCinematic(Time.deltaTime))
            {
                return;
            }

            // --- Update the interactions of all menu buttons in the current interface (hover highlighting, click response, etc.) ---

            // menuButtons is a list of all interactive buttons in the current interface.

            // Each button's Update detects mouse position and click status.

            for (var i = 0; i < menuButtons.Count; i++)
            {
                var screenRoot = runtimeRoot; // Record the root node before update

                menuButtons[i].Update(mainCamera);
                // If a button click causes an interface switch (runtimeRoot is rebuilt), stop traversing immediately.

                // Prevent errors from continuing to update old buttons that no longer exist.

                if (screenRoot != runtimeRoot)
                {
                    break;
                }
            }

            // --- Update the fixed buttons in the upper right corner (music switch, help button) ---

            // These buttons do not belong to a specific interface and are always displayed in the upper right corner (except for the story comic interface).
            if (runtimeRoot != null && currentScreen != mlpBootstrapScreen.StoryIntro)
            {
                // Switch the icon of the music button according to the current music playing status (play/pause)

                menuMusicButton?.SetActiveIconIndex(GetMusicIconIndex());
                var iconScreenRoot = runtimeRoot;
                menuMusicButton?.Update(mainCamera);
                // Also check whether the interface switches due to button clicks

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

            // --- Global keys: Escape key to return to the previous interface ---
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleMenuEscape();
            }
        }


//The third block: menu navigation. Generally speaking, this part is responsible for jumping between screens.

        /// <summary>
        /// Handle Escape key in various menu interfaces. Return to the previous interface or cancel the current operation.
        /// </summary>
        private void HandleMenuEscape()
        {
            // 1. Based on the current interface, decide where to return after pressing Escape.
            switch (currentScreen)
            {
                // 2. Competition type selection interface: Return to the character selection interface

                case mlpBootstrapScreen.MatchType:
                    ShowSinglePlayerCharacterSetup();
                    break;
                // 3. Story introduction interface: If the legend panel is open, close it, otherwise cancel the entire story introduction.

                case mlpBootstrapScreen.StoryIntro:
                    if (storyIntroLoreOpen)
                    {
                        SetStoryIntroLoreVisibility(false);
                        break;
                    }

                    CancelSinglePlayerStoryIntro();
                    break;
                // 4. Adventure/quick match/tournament setting interface: return to match type selection

                case mlpBootstrapScreen.AdventurePreview:
                case mlpBootstrapScreen.AdventureMap:
                case mlpBootstrapScreen.AdventureResult:
                case mlpBootstrapScreen.SinglePlayerSetup:
                case mlpBootstrapScreen.TournamentSetup:
                    ShowMatchTypeMenu();
                    break;
                // 5. Single-player character selection, double-player setting, and training setting interface: return to the main menu

                case mlpBootstrapScreen.SinglePlayerCharacterSetup:
                case mlpBootstrapScreen.TwoPlayerSetup:
                case mlpBootstrapScreen.TrainingSetup:
                    ShowPlayerCountMenu();
                    break;
                // 6. Tournament matchup or completion screen: abandon the tournament and return to the main menu

                case mlpBootstrapScreen.TournamentBracket:
                case mlpBootstrapScreen.TournamentComplete:
                    mlpInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                    break;
                // 7. Tournament awards interface: return to the matchup chart
                case mlpBootstrapScreen.TournamentAwards:
                    ShowTournamentBracket();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Block 3: Main Menu and Quick Test Controls

        // ShowPlayerCountMenu displays 1 person/2 persons/tutorial/training four entry buttons

        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Displays the main menu with buttons for Solo, Duo, Tutorial, and Training modes.

        /// </summary>
        private void ShowPlayerCountMenu()
        {
            // 1. Set the current interface status and initialize the menu (display logo, blue background)

            currentScreen = mlpBootstrapScreen.PlayerCount;
            BeginMenuScreen(true, false, "bg2blue0000");

            // 2. Create a translucent panel as a button container

            CreatePanel("PlayersPanel", mlpConstants.Width2, 336f, 304f, 286f, 8, new Color(0.05f, 0.08f, 0.1f, 0.72f));

            // 3. Add four menu buttons: 1-player mode, 2-player mode, tutorial, training

            var inventory = mlpInventory.Instance;
            // Create "1 PLAYER" button: text, X center, Y=246, width 228, height 52, execute lambda callback after clicking

            menuButtons.Add(new mlpMenuButton("1 PLAYER", mlpConstants.Width2, 246f, 228f, 52f, () =>
            {
                pendingParticipantMode = mlpParticipantMode.OnePlayer; // Set to single player mode

                inventory.SetParticipantMode(pendingParticipantMode);   // Applying single player mode to the inventory system

                ShowSinglePlayerCharacterSetup();                       // Display single player character selection interface

            }, runtimeRoot));

            menuButtons.Add(new mlpMenuButton("2 PLAYER", mlpConstants.Width2, 306f, 228f, 52f, () =>
            {
                pendingParticipantMode = mlpParticipantMode.TwoPlayers;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowTwoPlayerSetup();
            }, runtimeRoot));

            menuButtons.Add(new mlpMenuButton("TUTORIAL", mlpConstants.Width2, 366f, 228f, 52f, StartTutorialFlow, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("TRAINING", mlpConstants.Width2, 426f, 228f, 52f, ShowTrainingSetup, runtimeRoot));

            // 4. Create a switch control for quick test mode (for developer debugging)

            CreateQuickTestMenuControls();
        }

        // Create a "Quick Test" switch and information button at the bottom of the main menu (for developer debugging)

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

        // Toggle quick test mode on and off and refresh button text

        private void ToggleQuickTestModeFromMenu()
        {
            mlpQuickTestSettings.Enabled = !mlpQuickTestSettings.Enabled;
            RefreshQuickTestMenuToggle();
        }

        // Toggle display/hide of quick test information panel

        private void ToggleQuickTestInfoFromMenu()
        {
            SetQuickTestMenuInfoVisible(!quickTestMenuInfoVisible);
        }

        // Refresh the text of the quick test switch button (ON/OFF)

        private void RefreshQuickTestMenuToggle()
        {
            quickTestMenuToggleButton?.SetText(QuickTestMenuToggleLabel());
        }

        // Set the visibility of the quick test information panel

        private void SetQuickTestMenuInfoVisible(bool visible)
        {
            quickTestMenuInfoVisible = visible;
            if (quickTestMenuInfoRoot != null)
            {
                quickTestMenuInfoRoot.SetActive(visible);
            }
        }

        // Build the UI for a quick test information panel (title + description text)

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

        // Returns the current text label of the quick test switch

        private static string QuickTestMenuToggleLabel()
        {
            return mlpQuickTestSettings.Enabled ? "ON" : "OFF";
        }

        // ═══════════════════════════════════════════════════════════════
        // Block 4: Character Selection and Mode Selection

        // Select a character → select a mode (Adventure/Tournament), or go directly to Quick Match/Training/Double

        // ═══════════════════════════════════════════════════════════════

        // Display the single-player mode character selection interface (left and right arrows to switch characters, click NEXT to confirm)

        private void ShowSinglePlayerCharacterSetup()
        {
            // 1. Set the current interface to single-player character selection

            currentScreen = mlpBootstrapScreen.SinglePlayerCharacterSetup;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("SELECT CHARACTER", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            // 2. Create a centered character selection panel (with left and right arrows to switch characters)

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

        // Confirm the single-player character selection, save it to the archive and enter the mode selection interface.
        private void ConfirmSinglePlayerCharacter()
        {
            tournamentCharacterId = quickCharacterId;
            var inventory = mlpInventory.Instance;
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetTournamentSelection(tournamentCharacterId);
            ShowMatchTypeMenu();
        }

        /// <summary>
        /// Displays the mode selection screen, containing Adventure (Story Parkour) and Championship (Season Mode) cards.

        /// </summary>
        private void ShowMatchTypeMenu()
        {
            // 1. Set the current interface to mode selection

            currentScreen = mlpBootstrapScreen.MatchType;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            BeginMenuScreen(true, false, "bg10000");

            // 2. Card on the left: Adventure mode (story parkour, challenging guards step by step)

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

            // 3. Card on the right: Championship mode (seasonal system, 8-player knockout)

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
        // Block 5: Story Comic Cutscene

        // Play comics before entering adventure/tournament, support page turning, pause, legend panel and skip

        // ═══════════════════════════════════════════════════════════════

        // Showing the opening story comic for Adventure Mode

        private void ShowAdventureStoryIntro()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Adventure, ShowAdventureMap);
        }

        // Showing the opening story comic of Tournament Mode

        private void ShowTournamentStoryIntro()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Tournament, StartTournamentFlow);
        }

        private void ShowSinglePlayerStoryIntro(
            mlpSinglePlayerNarrativeMode mode,
            System.Action continueAction,
            System.Action cancelAction = null)
        {
            // 1. Save story mode type and completion callback

            storyIntroMode = mode;
            storyIntroPanelIndex = 0;
            storyIntroContinueAction = continueAction;
            storyIntroCancelAction = cancelAction ?? ShowMatchTypeMenu;
            // 2. Reset pause and legend panel status

            storyIntroPaused = false;
            storyIntroLoreOpen = false;
            storyIntroPauseBeforeLore = false;
            // 3. Display the first frame of the comic panel

            ShowSinglePlayerStoryIntroPanel();
        }

        // Display the current page of the story comic (picture, title, subtitles, pause/skip button)

        private void ShowSinglePlayerStoryIntroPanel()
        {
            // 1. Get the opening comic panel list of the current mode, if not, skip it directly

            var mode = mlpSinglePlayerNarrative.GetMode(storyIntroMode);
            var panels = mode.OpeningComic;
            if (panels == null || panels.Length == 0)
            {
                ContinueSinglePlayerStoryIntro();
                return;
            }

            // 2. Get the current panel data and select the accent color according to the mode (orange for adventure, blue for championship)

            storyIntroPanelIndex = Mathf.Clamp(storyIntroPanelIndex, 0, panels.Length - 1);
            var panel = panels[storyIntroPanelIndex];
            var isAdventure = storyIntroMode == mlpSinglePlayerNarrativeMode.Adventure;
            var accentColor = isAdventure
                ? new Color32(0xFF, 0xA6, 0x39, 0xFF)
                : new Color32(0x78, 0xE7, 0xFF, 0xFF);
            storyIntroAccentColor = accentColor;
            var backgroundFrame = isAdventure ? "bg10000" : "bg2blue0000";

            // 3. Initialize the menu interface and hide the music and help buttons (not required in comic mode)

            currentScreen = mlpBootstrapScreen.StoryIntro;
            BeginMenuScreen(false, false, backgroundFrame);
            menuMusicButton?.SetVisible(false);
            menuHelpButton?.SetVisible(false);
            storyIntroElapsed = 0f;
            // 3. Reset the pause and legend panel status related to the story introduction

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
            // 7. Update pause button text

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

        // Try to load the comic texture from Resources and display it. If the loading fails, false will be returned.

        private bool CreateStoryIntroComicImage(mlpStoryPanelDefinition panel)
        {
            // 1. Verify whether the panel data and image keys are valid

            if (panel == null || string.IsNullOrEmpty(panel.ImageKey))
            {
                return false;
            }

            // 2. Load comic textures from Resources, and return false if loading fails.

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(panel.ImageKey));
            if (texture == null)
            {
                return false;
            }

            // 3. Set the mapping mode to prevent seams from appearing on the edges

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            // 4. Create a comic image object in the center of the screen

            storyIntroImageObject = mlpRender.Image(
                "StoryIntroComicPage",
                texture,
                mlpConstants.Width2,
                StoryCinematicHeight * 0.5f,
                0.5f,
                0.5f,
                12,
                runtimeRoot);

            // 5. Calculate the scaling ratio so that the image covers the entire movie area

            var coverScale = Mathf.Max(
                StoryCinematicWidth / Mathf.Max(1f, texture.width),
                StoryCinematicHeight / Mathf.Max(1f, texture.height));
            storyIntroImageBaseScale = Vector3.one * mlpConstants.UnitsPerPixel * coverScale;
            storyIntroImageBaseScale.z = 1f;
            // 6. Get the renderer and set the initial transparency to 0 (it will fade in later)
            storyIntroImageRenderer = storyIntroImageObject.GetComponent<SpriteRenderer>();
            if (storyIntroImageRenderer != null)
            {
                storyIntroImageRenderer.color = new Color(1f, 1f, 1f, 0f);
            }

            // 7. Set initial position and zoom

            SetStoryIntroImageTransform(0f, 1f);
            return true;
        }

        // When comic textures fail to load, a solid color panel and text are used instead.

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

        // The movie-like playback effect (panning, zooming, fading in) of the comic is updated every frame, and the page turns automatically when the time comes.

        // Called by Update() when currentScreen == StoryIntro.

        // Returning true means that this page has finished playing and triggered page turning (the caller should skip the subsequent menu logic), false means that it is still playing or has been paused.

        private bool UpdateStoryIntroCinematic(float deltaTime)
        {
            // 1. The timer is not updated in the paused state, and the screen remains at the current frame.

            //    storyIntroPaused will be set to true when the user clicks the pause button or opens the legend panel.

            if (storyIntroPaused)
            {
                return false;
            }

            // 2. Accumulate elapsed time (Mathf.Max defends against negative deltaTime, such as when the window is out of focus and restored).

            storyIntroElapsed += Mathf.Max(0f, deltaTime);

            //    normalized: The playback progress of the current page, 0→1 linear mapping, controlled by StoryCinematicPageSeconds (3.0s).

            var normalized = Mathf.Clamp01(storyIntroElapsed / StoryCinematicPageSeconds);

            //    eased: SmoothStep easing (first slow, then slow, then fast in the middle) of normalized, used for panning and zooming,

            //    Made the Ken Burns effect start and stop softer to avoid sudden starts and stops.

            var eased = Mathf.SmoothStep(0f, 1f, normalized);

            //    fade: fade in progress, use shorter StoryCinematicFadeSeconds (0.42s) to make the picture appear quickly,

            //    Instead of waiting the full 3 seconds to become fully opaque.

            var fade = Mathf.Clamp01(storyIntroElapsed / StoryCinematicFadeSeconds);

            // 3. Update the visual representation of comic pictures.

            if (storyIntroImageObject != null)
            {
                //    Position (pan) and zoom (zoom in slowly) are driven by eased:

                //    - The panning direction is determined by the odd and even page numbers (even numbers go to the right, odd numbers go to the left), and the amplitude is ±StoryCinematicPanPixels(14px)

                //    - Zoom slowly increases from 1.0 to 1.0 + StoryCinematicZoomAmount(0.035), which is a 3.5% zoom

                //    The two work together to create the slow-pull-and-pull feel of a Ken Burns documentary.

                SetStoryIntroImageTransform(eased, 1f + StoryCinematicZoomAmount * eased);

                //    Transparency driven by fade SmoothStep fade-in: smooth transition from fully transparent to fully opaque for the first 0.42 seconds,

                //    Prevent images from being cut when turning pages.

                if (storyIntroImageRenderer != null)
                {
                    storyIntroImageRenderer.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, fade));
                }
            }

            // 4. When the dwell time expires (3.0 seconds), it will automatically turn to the next page.

            //    AdvanceSinglePlayerStoryIntro() will judge: if there are subsequent pages, the next page will be displayed.

            //    If it is the last page, the completion callback is executed (entering the adventure map or tournament settings).

            if (storyIntroElapsed >= StoryCinematicPageSeconds)
            {
                AdvanceSinglePlayerStoryIntro();
                return true;  // Tell the caller that the page has been turned and skip the menu button update in this frame.
            }

            return false;  // While still playing, the caller continues to execute the menu button interaction logic.

        }

        // Switch the pause/resume state of comics

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

        // Set the position (pan left and right + float up and down) and zoom of the comic image

        private void SetStoryIntroImageTransform(float normalized, float zoom)
        {
            // 1. If there is no image object, skip it

            if (storyIntroImageObject == null)
            {
                return;
            }

            // 2. Even-numbered frames pan to the right, and odd-numbered frames pan to the left (creating a sense of alternating motion)

            var direction = storyIntroPanelIndex % 2 == 0 ? 1f : -1f;
            var panX = Mathf.Lerp(-StoryCinematicPanPixels, StoryCinematicPanPixels, normalized) * direction;
            // 3. Use sine waves to float slightly up and down in the vertical direction.

            var panY = Mathf.Sin(normalized * Mathf.PI) * 4f;
            // 4. Set the world coordinate position of the image (adsorbed to the pixel grid)

            storyIntroImageObject.transform.position = mlpConstants.PixelToWorldSnapped(
                mlpConstants.Width2 + panX,
                StoryCinematicHeight * 0.5f + panY,
                0f);
            // 5. Apply scaling (base scale x magnification), keeping the Z axis at 1

            storyIntroImageObject.transform.localScale = storyIntroImageBaseScale * zoom;
            storyIntroImageObject.transform.localScale = new Vector3(
                storyIntroImageObject.transform.localScale.x,
                storyIntroImageObject.transform.localScale.y,
                1f);
        }

        // If the comic is turned to the previous page, if it is already the first page, the comic will be canceled.

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

        // Turn to the next page of the comic. If it is the last page, continue with the subsequent process.

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

        // Execute subsequent callbacks after the comic is finished (enter adventure map or tournament settings)

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

        // Cancel comic playback and execute cancellation callback (default returns to mode selection)

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

        // Replay the opening comic in Adventure Mode

        private void ShowAdventureComicReplay()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Adventure, ShowAdventureMap, ShowAdventureMap);
        }

        // Replay the opening comic of the tournament (triggered from the settings screen)

        private void ShowTournamentSetupComicReplay()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Tournament, ShowTournamentSetup, ShowTournamentSetup);
        }

        // Replay the opening comic of the tournament (triggered from the matchup screen)

        private void ShowTournamentBracketComicReplay()
        {
            ShowSinglePlayerStoryIntro(mlpSinglePlayerNarrativeMode.Tournament, ShowTournamentBracket, ShowTournamentBracket);
        }

        
        
        //Adventure trailer page

        private void ShowAdventurePreview()
        {
            // 1. Set the current interface to adventure preview and single-player mode

            currentScreen = mlpBootstrapScreen.AdventurePreview;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            // 2. Initialize the menu interface and display the adventure mode title and subtitle

            BeginMenuScreen(false, false, "bg10000");
            AddTitle(mlpSinglePlayerNarrative.Adventure.MenuTitle, 54f, 30, new Color32(0xFF, 0xB6, 0x45, 0xFF));
            AddSubtitle(mlpSinglePlayerNarrative.Adventure.Subtitle, 90f, 14);

            // 3. Create an information panel to display the status, goals and description text of the adventure mode

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

            // 4. Create bottom buttons: return and quick duel

            menuButtons.Add(new mlpMenuButton("BACK", 220f, 452f, 180f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new mlpMenuButton("QUICK DUEL", 580f, 452f, 200f, 42f, ShowSinglePlayerSetup, runtimeRoot));
        }

        // ═══════════════════════════════════════════════════════════════
        // Block 6: Adventure Mode

        // Treasure map, level nodes, route connections, level posters, difficulty selection and settlement interface

        // ═══════════════════════════════════════════════════════════════

        // Display the adventure mode treasure map interface (level nodes, routes, posters, difficulty selection)

        private void ShowAdventureMap()
        {
            // 1. Set the current interface as an adventure map
            currentScreen = mlpBootstrapScreen.AdventureMap;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(pendingParticipantMode);

            // 2. If the adventure has not started or the characters have been changed, start the adventure again

            var selectedAdventureCharacterId = mlpPlayersData.SanitizeCharacterId(quickCharacterId);
            if (!inventory.IsAdventureActive || inventory.Adventure.PlayerCharacterId != selectedAdventureCharacterId)
            {
                inventory.BeginAdventure(selectedAdventureCharacterId);
                adventureSelectedLevelIndex = inventory.Adventure.CurrentLevelIndex;
            }

            // 3. Make sure the selected level index is valid (the final settlement level will be displayed if completed, otherwise the current playable level will be displayed)

            var adventure = inventory.Adventure;
            if (adventure.Completed)
            {
                adventureSelectedLevelIndex = Mathf.Max(0, adventure.LastResolvedLevelIndex);
            }
            else if (!adventure.IsLevelUnlocked(adventureSelectedLevelIndex))
            {
                adventureSelectedLevelIndex = adventure.CurrentLevelIndex;
            }

            // 4. Initialize the menu, draw treasure map borders, route maps, mechanism descriptions, and level posters

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

        // Draw the outer frame of the treasure map (shadow, copper border, map texture, seal text)

        private void CreateAdventureTreasureMapFrame()
        {
            // 1. Draw the shadow layer of the map panel (to create a three-dimensional feel)

            CreatePanel(
                "AdventureMapDropShadow",
                AdventureMapPanelX + 7f,
                AdventureMapPanelY + 10f,
                AdventureMapPanelWidth + 30f,
                AdventureMapPanelHeight + 28f,
                7,
                new Color(0f, 0f, 0f, 0.3f));
            // 2. Draw a copper border

            CreatePanel(
                "AdventureMapCopperFrame",
                AdventureMapPanelX,
                AdventureMapPanelY,
                AdventureMapPanelWidth + 18f,
                AdventureMapPanelHeight + 18f,
                8,
                new Color(0.5f, 0.22f, 0.08f, 0.76f));

            // 3. Load the treasure map texture and display it. When the texture is unavailable, use a solid color panel to replace it.

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

            // 4. Add readability masks and inner shadow textures

            CreatePanel("AdventureMapReadabilityWash", AdventureMapPanelX - 28f, AdventureMapPanelY + 10f, AdventureMapPanelWidth - 168f, AdventureMapPanelHeight - 150f, 10, new Color(1f, 0.88f, 0.56f, 0.055f));
            CreatePanel("AdventureMapInnerShade", AdventureMapPanelX, AdventureMapPanelY + AdventureMapPanelHeight * 0.5f - 18f, AdventureMapPanelWidth - 68f, 7f, 10, new Color(0.13f, 0.06f, 0.02f, 0.24f));

            // 5. Display the "ESCAPE ROUTE" seal text in the lower left corner of the map

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

        // Display the mechanical description text of the current level at the bottom of the map (such as "Warden duels")

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

        // Get the treasure map texture: Prioritize the resource file, if not, programmatically generate an old paper texture at runtime

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

        // Create adventure mode route nodes and connecting lines (draw routes between levels)

        private void CreateAdventureRouteMap(mlpAdventureData adventure)
        {
            // 1. Draw connecting lines (route) between adjacent nodes

            var levels = mlpAdventureCatalog.AllLevels;
            for (var i = 1; i < levels.Length; i++)
            {
                var unlocked = adventure.IsLevelUnlocked(i);
                var previousAnchor = GetAdventureNodeRouteAnchor(i - 1);
                var currentAnchor = GetAdventureNodeRouteAnchor(i);
                CreateAdventureConnector(i, previousAnchor.x, previousAnchor.y, currentAnchor.x, currentAnchor.y, unlocked);
            }

            // 2. Draw each node (including avatar, status color, etc.) and add click buttons for unlocked nodes

            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                var unlocked = adventure.IsLevelUnlocked(i);
                var completed = adventure.IsLevelCompleted(i);
                var selected = i == adventureSelectedLevelIndex;
                CreateAdventureRouteNode(level, i, unlocked, completed, selected);

                // 3. Unlocked nodes with unfinished adventure: Create invisible click buttons

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

        // Draw a single adventure level node (avatar, border, glow, lock mask)

        private void CreateAdventureRouteNode(
            mlpAdventureLevelDefinition level,
            int index,
            bool unlocked,
            bool completed,
            bool selected)
        {
            // 1. Get the node position and select the corresponding color according to the status (completed/unlocked/locked)

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

            // 2. Draw node shadows

            CreatePanel($"AdventureNodeShadow_{index}", nodePosition.x + 4f, nodePosition.y + 7f, AdventureNodeWidth + 10f, AdventureNodeHeight + 12f, 13, new Color(0.04f, 0.02f, 0.01f, 0.34f));
            // 3. Draw a highlight halo on the selected node

            if (selected)
            {
                CreatePanel($"AdventureNodeSelectGlow_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth + 24f, AdventureNodeHeight + 24f, 14, new Color(1f, 0.8f, 0.28f, 0.28f));
            }

            // 4. Draw from outside to inside: outer frame, internal fill, avatar background, avatar halo

            CreatePanel($"AdventureNodePlate_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth, AdventureNodeHeight, 15, borderTint);
            CreatePanel($"AdventureNodeInset_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth - 8f, AdventureNodeHeight - 8f, 16, fillTint);
            CreatePanel($"AdventureNodePortraitBack_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 22f, AdventureNodeWidth - 22f, 17, portraitBackTint);
            CreatePanel($"AdventureNodePortraitGlow_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 12f, AdventureNodeWidth - 12f, 17, glowColor);
            // 5. Draw the guardian character’s avatar

            CreateTournamentPortrait(
                $"AdventureNodePortrait_{index}",
                level.WardenCharacterId,
                nodePosition.x,
                portraitY,
                selected ? 50f : 46f,
                18);
            // 6. Unlocked nodes are superimposed with dark masks

            if (!unlocked)
            {
                CreatePanel($"AdventureNodeLockShade_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 22f, AdventureNodeWidth - 22f, 19, new Color(0.08f, 0.09f, 0.11f, 0.42f));
            }
        }

        // Get the coordinate position of the adventure level node on the map

        private static Vector2 GetAdventureNodePosition(int index)
        {
            if (index < 0 || index >= mlpAdventureCatalog.LevelCount)
            {
                return new Vector2(AdventureMapPanelX, AdventureMapPanelY);
            }

            var level = mlpAdventureCatalog.GetLevel(index);
            return new Vector2(level.MapX, level.MapY);
        }

        // Get the route connection anchor point of the adventure node (moved slightly downward, more visually natural)
        private static Vector2 GetAdventureNodeRouteAnchor(int index)
        {
            var nodePosition = GetAdventureNodePosition(index);
            return new Vector2(nodePosition.x, nodePosition.y - 2f);
        }

        // Draw a connecting line between two adventure nodes (shading + route + dot)

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

        // Draw a slanted rectangular line segment (used to connect two nodes)

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

        // Draw isometric dot decoration on connecting lines

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

        // Draw detailed posters of adventure levels (guard portraits, status labels, ball skins, difficulty selection)

        private void CreateAdventureLevelPoster(mlpAdventureData adventure, int selectedLevelIndex)
        {
            // 1. Obtain level data and determine unlocking, completion and current level status

            var level = mlpAdventureCatalog.GetLevel(selectedLevelIndex);
            var unlocked = adventure.IsLevelUnlocked(selectedLevelIndex);
            var completed = adventure.IsLevelCompleted(selectedLevelIndex);
            var activeGate = selectedLevelIndex == adventure.CurrentLevelIndex && !completed && unlocked;
            // 2. Create a poster card border (special border style for active levels)

            CreateFramedPanel(
                "AdventurePosterFrame",
                activeGate ? "MatchBack0002" : "MatchBack0001",
                AdventurePosterX,
                AdventurePosterY,
                AdventurePosterWidth,
                AdventurePosterHeight,
                11,
                unlocked ? new Color(1f, 0.84f, 0.58f, 0.96f) : new Color(0.7f, 0.78f, 0.86f, 0.7f));
            // 3. Draw the interior shadows and decorative lines of the poster

            CreatePanel("AdventurePosterShade", AdventurePosterX, AdventurePosterY, AdventurePosterWidth - 28f, AdventurePosterHeight - 34f, 12, new Color(0.04f, 0.06f, 0.09f, 0.72f));
            CreatePanel("AdventurePosterAccent", AdventurePosterX, 137f, AdventurePosterWidth - 48f, 4f, 13, unlocked ? new Color(1f, 0.58f, 0.18f, 0.86f) : new Color(0.44f, 0.52f, 0.58f, 0.7f));
            // 4. Display level status labels (obtained/next level/open/locked)

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
            // 5. Display area name

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
            // 6. Draw the guardian avatar area (halo, border, internal filling)

            CreatePanel("AdventurePosterPortraitGlow", AdventurePosterX, 232f, 112f, 112f, 13, completed ? new Color(0.56f, 0.98f, 0.66f, 0.16f) : unlocked ? new Color(1f, 0.66f, 0.22f, 0.16f) : new Color(0.72f, 0.78f, 0.86f, 0.08f));
            CreatePanel("AdventurePosterPortraitFrame", AdventurePosterX, 232f, 90f, 96f, 14, unlocked ? new Color(0.98f, 0.84f, 0.56f, 0.94f) : new Color(0.68f, 0.72f, 0.78f, 0.84f));
            CreatePanel("AdventurePosterPortraitInset", AdventurePosterX, 232f, 80f, 86f, 15, unlocked ? new Color(0.98f, 0.94f, 0.86f, 0.96f) : new Color(0.7f, 0.72f, 0.74f, 0.9f));
            // 7. Draw the guardian’s avatar and name

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
            // 8. Draw a dividing line and then display the basketball skin selection area

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
            // 9. Show basketball preview and difficulty selection button

            CreateBallPreview(
                "AdventurePosterBallPreview",
                mlpBallCatalog.PreviewTheme(level.BallSelection),
                AdventurePosterX,
                346f,
                34f,
                14);
            CreateAdventureDifficultySelector();
        }

        // Create a difficulty toggle button at the bottom of the adventure poster

        private void CreateAdventureDifficultySelector()
        {
            menuButtons.Add(new mlpMenuButton(mlpInventory.Instance.DifficultyLabel, AdventurePosterX, 380f, 154f, 42f, () =>
            {
                mlpInventory.Instance.ToggleDifficulty();
                ShowAdventureMap();
            }, runtimeRoot));
        }

        // Start the adventure level: save your selection, initialize level data, and enter the game

        private void StartAdventureLevelFlow()
        {
            // Get backpack/inventory singleton

            var inventory = mlpInventory.Instance;
            // Save the currently selected character as a quick selection

            inventory.SetQuickSelection(quickCharacterId);
            // Try to start the adventure level (check whether the level is unlocked, whether the character is available, etc.)

            if (!inventory.StartAdventureLevel(adventureSelectedLevelIndex, quickCharacterId))
            {
                // The level cannot be started and returns to the adventure map interface.

                ShowAdventureMap();
                return;
            }

            // The level starts successfully and enters the gameplay.

            StartGameplay();
        }

        // Retry the current adventure level (triggered by clicking the RETRY button after failure)

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

        // Display the settlement interface of the adventure level (guard lines, victory or defeat results, continue/retry button)

        private void ShowAdventureResult(bool playerWon)

        {
            // 1. Get the level data and result lines just settled

            var adventure = mlpInventory.Instance.Adventure;
            var resolvedIndex = Mathf.Max(0, adventure.LastResolvedLevelIndex);
            var level = mlpAdventureCatalog.GetLevel(resolvedIndex);
            var resultSpeech = FormatAdventureResultSpeech(level.GetRandomResultLine(playerWon));
            adventureSelectedLevelIndex = playerWon && !adventure.Completed ? adventure.CurrentLevelIndex : resolvedIndex;

            // 2. Set different backgrounds and titles according to victory or defeat
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

        // Add quotation marks to the adventure result lines and wrap them automatically

        private static string FormatAdventureResultSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return "\"" + WrapMenuText(text.Trim(), 32) + "\"";
        }

        // Wrap automatically on word boundaries, no more than maxLineLength characters per line

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

        // Returns the short name of the adventure area (for UI with limited space)

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
        // Block 7: Competition Settings Interface

        // Pre-match settings for quick matches/trainings/duos/tournaments (characters, ball skins, difficulty)

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
            // 1. Draw the card background panel and decorative lines

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
            // 2. Display routing labels (such as "ADVENTURE", "TOURNAMENT")

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
            // 3. Display mode headline

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
            // 4. Display a subtitle (a sentence that attracts players)

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
            // 5. Display two lines of target description text

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

            // 6. Add action buttons at the bottom of the card

            menuButtons.Add(new mlpMenuButton(buttonText, centerX, 397f, 194f, 44f, action, runtimeRoot));
        }

        /// <summary>
        /// Displays the quick match settings interface, where players select their character, basketball skin, and difficulty.

        /// </summary>
        private void ShowSinglePlayerSetup()
        {
            // 1. Set the current interface to single player quick match settings

            currentScreen = mlpBootstrapScreen.SinglePlayerSetup;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("QUICK MATCH", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            // 2. Create the role selection panel on the left and the options panel on the right

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
        /// The training mode setting interface is displayed, where the player selects the character and basketball skin.

        /// </summary>
        private void ShowTrainingSetup()
        {
            // 1. Set the current interface to training mode settings

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
        /// The two-player setting interface is displayed. Two players select characters respectively and select basketball skins together.

        /// </summary>
        private void ShowTwoPlayerSetup()
        {
            // 1. Set the current interface to a two-player battle setting

            currentScreen = mlpBootstrapScreen.TwoPlayerSetup;
            pendingParticipantMode = mlpParticipantMode.TwoPlayers;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("2 PLAYERS MATCH", 58f, 30, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            // 2. Create two left and right character selection panels (P1 and P2), with VS displayed in the middle

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
        /// The tournament setting interface is displayed, and players select their characters before entering the battle map.

        /// </summary>
        private void ShowTournamentSetup()
        {
            // 1. Set the current interface to tournament character selection and save the selection to the archive.

            currentScreen = mlpBootstrapScreen.TournamentSetup;
            pendingParticipantMode = mlpParticipantMode.OnePlayer;
            mlpInventory.Instance.SetParticipantMode(pendingParticipantMode);
            mlpInventory.Instance.SetTournamentSelection(tournamentCharacterId);

            // 2. Initialize menu and display tournament title

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
        // Block 8: Game Start Process

        // Save menu selections to an archive, then call StartGameplay to create a game scene
        // ═══════════════════════════════════════════════════════════════

        /// Save your quick match selections and start the match.

        /// </summary>
        private void StartQuickMatchFlow()
        {
            // 1. Save player’s single player mode, character and basketball skin selections

            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(mlpParticipantMode.OnePlayer);
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetQuickBallSelection(quickBallSelection);
            // 2. Initialize Quick Match and enter the game

            inventory.StartQuickGame();
            StartGameplay();
        }

        /// <summary>
        /// Save training selections and start a training session.

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
        /// Save the training character selection and start the tutorial.

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

        // Start the tutorial from the help panel (if there is an ongoing tournament/adventure, abandon it first)

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
        /// Save your tournament character selection and start a tournament match.

        /// </summary>
        private void StartTournamentFlow()
        {
            // 1. Save your character and basketball skin selections and try to start the tournament

            var inventory = mlpInventory.Instance;
            inventory.SetParticipantMode(mlpParticipantMode.OnePlayer);
            inventory.SetTournamentSelection(tournamentCharacterId);
            inventory.SetTournamentBallSelection(tournamentBallSelection);

            // 2. If you fail to start the tournament (less than 8 characters), you will return to the setting interface.

            if (!inventory.BeginTournament())
            {
                ShowTournamentSetup();
                return;
            }

            // 3. If successful, the tournament matchup chart will be displayed.

            ShowTournamentBracket();
        }

        /// <summary>
        /// Open the keyboard operation help panel.

        /// </summary>
        private void ShowMenuControlsPanel()
        {
            mlpHelpPanel.ShowKeyboardPage();
        }

        /// <summary>
        /// Check if the player chooses to replay the tutorial, start training, or start a quick match after the tutorial ends.

        /// </summary>
        private bool HandlePendingTutorialAction()
        {
            // 1. Read the next action chosen by the player after the tutorial.

            var inventory = mlpInventory.Instance;
            var action = inventory.PendingTutorialNextAction;
            // 2. Clear the pending mark to prevent repeated triggering

            inventory.PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 3. Perform corresponding operations based on selection

            switch (action)
            {
                // 4. Replay the tutorial: Restart the tutorial process

                case mlpTutorialNextAction.ReplayTutorial:
                    StartTutorialFlow();
                    return true;
                // 5. Start training: enter training mode

                case mlpTutorialNextAction.StartTraining:
                    StartTrainingFlow();
                    return true;
                // 6. Start a quick match: synchronize your training selections to the quick match, and then start

                case mlpTutorialNextAction.StartQuickMatch:
                    quickCharacterId = trainingCharacterId;
                    quickBallSelection = trainingBallSelection;
                    StartQuickMatchFlow();
                    return true;
                // 7. There is no pending operation: return false to indicate that no jump is required

                default:
                    return false;
            }
        }

        /// <summary>
        /// Starts the finals phase of the tournament and displays the updated bracket map.

        /// </summary>
        private void StartTournamentFinalsFlow()
        {
            mlpInventory.Instance.BeginTournamentFinals();
            ShowTournamentBracket();
        }

        /// <summary>
        /// Save your two-player character selection and start a Versus match.

        /// </summary>
        private void StartTwoPlayerMatch()
        {
            // 1. Save the basketball skin selection and start the battle with the characters selected by the two players.

            mlpInventory.Instance.SetVersusBallSelection(versusBallSelection);
            mlpInventory.Instance.StartTwoPlayerVersus(versusLeftCharacterId, versusRightCharacterId);

            // 2. Clear the menu scene and create a competition scene

            StartGameplay();
        }

        /// <summary>
        // ═══════════════════════════════════════════════════════════════
        // Block 9: Championship brackets and awards
        // Regular season rankings, playoff matchups, awards ceremony animations and standings

        // ═══════════════════════════════════════════════════════════════

        /// Displays the tournament map interface, including all current matches and rankings.

        /// </summary>
        private void ShowTournamentBracket()
        {
            // 1. Obtain tournament data and determine the current stage (regular season/playoffs/completed)

            var inventory = mlpInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? mlpBootstrapScreen.TournamentComplete : mlpBootstrapScreen.TournamentBracket;
            var regularSeasonScreen = tournament.CurrentStage == mlpTournamentStage.RegularSeason;
            var titleY = regularSeasonScreen ? 44f : 34f;
            var titleFontSize = regularSeasonScreen ? 32 : 30;
            var subtitleY = regularSeasonScreen ? 72f : 62f;
            var subtitleFontSize = regularSeasonScreen ? 16 : 14;
            var statusText = GetTournamentStatusText(tournament);

            // 2. Select the background color according to the stage, initialize the menu and display the title and status subtitle

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

            // 3. Draw the matchup panel and season status banner

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
        /// Shows the end-of-season awards ceremony, including ranking ribbons and trophies.

        /// </summary>
        private void ShowTournamentAwards()
        {
            // 1. Return to the battle map interface when the tournament is not completed

            var tournament = mlpInventory.Instance.Tournament;
            if (!tournament.Completed)
            {
                ShowTournamentBracket();
                return;
            }

            // 2. Initialize the award ceremony interface and set the title color according to the player ranking

            currentScreen = mlpBootstrapScreen.TournamentAwards;
            BeginMenuScreen(false, false, "bg10000");
            AddTitle(mlpSinglePlayerNarrative.TournamentSeasonCompleteTitle, 52f, 28, GetTournamentAwardsAccentColor(tournament.PlayerPlacement));

            // 3. Create an awards scene (podium, trophy, character skeleton animation)

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
        // Block 10: Game Scene Construction and Camera Mode Switching

        // StartGameplay creates a game, and BeginMenuScreen initializes the menu interface.

        // ═══════════════════════════════════════════════════════════════

        /// Clear the menu scene and create a new race game object.

        /// </summary>
        private void StartGameplay()
        {
            // 1. Clear all GameObjects in the menu scene

            ClearRuntime();

            // 2. Switch the camera to pixel-perfect game rendering mode

            EnableGameplayPresentation();

            // 3. Create a new runtime root node, audio system, and clear the button list

            runtimeRoot = new GameObject("mlpRuntime").transform;
            mlpAudio.Create(transform).PlayMusic(mlpAssets.Sounds.MenuMusic);
            // 2. Clear menu buttons and various auxiliary object references

            menuButtons.Clear();

            // 4. Use the game builder to create game scenes (players, basketballs, hoops, courts, etc.)

            gameCore = new mlpGameBuilder().Build(runtimeRoot);
        }

        /// <summary>
        /// Set up a new menu interface, including background, optional logo and operation prompts.

        /// </summary>
        private void BeginMenuScreen(bool showLogo, bool showControls, string backgroundFrame)
        {
            // 1. Clear old scenes and switch the camera to native resolution menu mode (text is clearer)

            ClearRuntime();
            EnableNativeMenuPresentation();

            // 2. Create the runtime root node, native text layer, and audio system

            runtimeRoot = new GameObject("mlpRuntime").transform;
            nativeMenuTextLayer = new mlpNativeMenuTextLayer(runtimeRoot);
            nativeMenuTextLayer.RefreshLayout(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            mlpAudio.Create(transform).PlayMusic(mlpAssets.Sounds.MenuMusic);

            // 3. Create a background image (preferably use an independent background, otherwise use a universal background)

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

            // 4. If necessary, load and display the game logo (scaled to the maximum aspect ratio)

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

            // 5. Create the music switch and help button in the upper right corner

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

            // 6. If necessary, display operation prompt text at the bottom
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

            // 7. Clear the button list (each page will add its own button later)

            menuButtons.Clear();
        }

        /// <summary>
        /// Switch the camera to pixel-perfect gaming mode.

        /// </summary>
        private void EnableGameplayPresentation()
        {
            // 1. Turn off native UI mode and reset the viewport cache size

            usingNativeUiPresentation = false;
            viewportScreenWidth = -1;
            viewportScreenHeight = -1;
            // 2. If there is no camera, return directly.

            if (mainCamera == null)
            {
                return;
            }

            // 3. Set the camera to full screen and attach the pixel perfect resolution adapter

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            fixedResolutionPresenter?.Attach(mainCamera);
        }

        /// <summary>
        /// Switch the camera to native resolution menu mode to ensure text is clear.

        /// </summary>
        private void EnableNativeMenuPresentation()
        {
            // 1. Enable native UI mode and remove pixel perfect adapter

            usingNativeUiPresentation = true;
            fixedResolutionPresenter?.Detach();
            // 2. Force refresh the menu viewport to ensure clear text

            RefreshNativeMenuViewport(force: true);
        }

        /// <summary>
        /// In native menu mode, the camera viewport is recalculated when the window size changes.

        /// </summary>
        private void RefreshNativeMenuViewport(bool force = false)
        {
            // 1. Skip when not in native UI mode or without camera

            if (!usingNativeUiPresentation || mainCamera == null)
            {
                return;
            }

            // 2. Get the current screen size, skip if there is no change and no forced refresh

            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            if (!force && screenWidth == viewportScreenWidth && screenHeight == viewportScreenHeight)
            {
                return;
            }

            // 3. Cache the new size and refresh the native text layer layout

            viewportScreenWidth = screenWidth;
            viewportScreenHeight = screenHeight;
            nativeMenuTextLayer?.RefreshLayout(screenWidth, screenHeight);

            // 4. Calculate the screen aspect ratio and adjust the camera viewport to maintain the target ratio

            var screenAspect = screenWidth / (float)screenHeight;
            // 5. The aspect ratio just matches: full screen display

            if (Mathf.Abs(screenAspect - NativeUiAspect) <= 0.0001f)
            {
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            // 6. Wider screen: add black borders to the left and right, and display in the center

            if (screenAspect > NativeUiAspect)
            {
                var normalizedWidth = NativeUiAspect / screenAspect;
                mainCamera.rect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
                return;
            }

            // 7. The screen is taller: black borders are added up and down, and the display is centered.

            var normalizedHeight = screenAspect / NativeUiAspect;
            mainCamera.rect = new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Block 11: UI basic components (text, panel, button creation method)

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Place a large title tag at the top of the menu interface.

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
        /// Place a smaller subtitle label below the menu interface title.

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
        /// Create text labels on the menu interface, giving priority to using the native text layer.

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
            // 1. Determine the parent container of the text, which is hung under the menu root node by default.

            var resolvedParent = parent ?? runtimeRoot;
            // 2. If the native text layer (TextMeshPro) is supported and created in the native way, the text will be clearer.

            if (ShouldUseNativeMenuText(resolvedParent))
            {
                nativeMenuTextLayer?.CreateText(name, text, x, y, fontSize, color, anchor, style);
                return;
            }

            // 3. Otherwise, use the old TextMesh method to create text

            mlpRender.Text(name, text, x, y, fontSize, color, anchor, sortingOrder, resolvedParent, style);
        }

        // Create text objects that can be toggled on/off in the legend panel
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
            // 1. Create text objects based on available rendering methods (native or legacy)

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

            // 2. Add the text to the legend text list and set the visibility according to whether the legend panel is open or not.

            if (textObject != null)
            {
                storyIntroLoreTextObjects.Add(textObject);
                textObject.SetActive(storyIntroLoreOpen);
            }

            return textObject;
        }

        // Create text labels on legend panel buttons

        private GameObject CreateStoryIntroLoreButtonLabel(string text, float x, float y)
        {
            // 1. Prioritize using native text layer to create legend button labels

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

            // 2. When the native text layer is unavailable, use the old version of TextMesh to create it

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

        // Update the text content of the legend button label (compatible with native TMP and old TextMesh)

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

        // Update text color of legend button label

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

        // Update the position of buttons and art elements when the legend panel is opened/closed

        private void RefreshStoryIntroLoreButtonLayout(bool isVisible)
        {
            // 1. Depending on whether the legend panel is open, select the corresponding position and offset value

            var buttonX = isVisible ? StoryIntroLoreOpenButtonX : StoryIntroLoreButtonX;
            var buttonY = isVisible ? StoryIntroLoreOpenButtonY : StoryIntroLoreButtonY;
            var labelOffsetX = isVisible ? StoryIntroLoreOpenLabelOffsetX : StoryIntroLoreLabelOffsetX;
            var labelOffsetY = isVisible ? StoryIntroLoreOpenLabelOffsetY : StoryIntroLoreLabelOffsetY;
            // 2. Move the button to a new location

            storyIntroLoreButton?.SetPosition(buttonX, buttonY);
            // 3. Move the text label on the button to an offset position next to the button

            SetStoryIntroLoreLabelPosition(
                buttonX + labelOffsetX,
                buttonY + labelOffsetY);
            // 4. Move the art elements of the legend panel. The offset is based on the difference between the button and the initial position.

            SetStoryIntroLoreArtOffset(
                buttonX - StoryIntroLoreButtonX,
                buttonY - StoryIntroLoreButtonY);
        }

        // Set the screen position of the legend button label

        private void SetStoryIntroLoreLabelPosition(float x, float y)
        {
            // 1. Skip if there is no label object

            if (storyIntroLoreLabelObject == null)
            {
                return;
            }

            // 2. If it is native text (TMP), use pixel coordinates to set the UI position

            var nativeText = storyIntroLoreLabelObject.GetComponent<TMPro.TMP_Text>();
            if (nativeText != null)
            {
                mlpNativeMenuTextLayer.SetPixelPosition(nativeText.rectTransform, x, y);
                return;
            }

            // 3. Otherwise convert the pixel coordinates to world coordinates and snap to the pixel grid

            storyIntroLoreLabelObject.transform.position = mlpConstants.PixelToWorldSnapped(
                x,
                y,
                storyIntroLoreLabelObject.transform.position.z);
        }

        // Set the offset of the legend panel art element (move with the button)

        private void SetStoryIntroLoreArtOffset(float pixelOffsetX, float pixelOffsetY)
        {
            // 1. If there is no art root node, skip it.

            if (storyIntroLoreArtRoot == null)
            {
                return;
            }

            // 2. Convert both the origin and target offsets into world coordinates and calculate the relative offsets

            var origin = mlpConstants.PixelToWorldSnapped(0f, 0f);
            var offset = mlpConstants.PixelToWorldSnapped(pixelOffsetX, pixelOffsetY);
            // 3. Set the local position offset of the art element

            storyIntroLoreArtRoot.transform.localPosition = new Vector3(
                offset.x - origin.x,
                offset.y - origin.y,
                0f);
        }

        /// <summary>
        /// Returns true if the native text layer is active and can render text.

        /// </summary>
        private bool ShouldUseNativeMenuText(Transform parent)
        {
            return nativeMenuTextLayer != null && parent != null && nativeMenuTextLayer.Owns(parent);
        }

        /// <summary>
        /// Draw a semi-transparent dark panel rectangle on the menu interface.

        /// </summary>
        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            return CreatePanel(name, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        /// <summary>
        /// Draw a semi-transparent dark panel rectangle on the menu interface.

        /// </summary>
        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. First try to create a panel with independent textures (the image quality is better)

            var standalonePanel = TryCreateStandaloneTintPanel(name, x, y, width, height, sortingOrder, tint, parent);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            // 2. When independent textures are unavailable, use the color block elves in the atlas instead.
            var panel = mlpRender.Sprite(name, mlpAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            // 3. Scale the sprite according to the target width and height, and set the translucent color

            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / LegacyTintPanelSourcePixels,
                mlpConstants.UnitsPerPixel * height / LegacyTintPanelSourcePixels,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        // Try to use an independent texture as the menu background (the image quality is better), and return false if it fails.

        private bool TryCreateStandaloneMenuBackground(string backgroundFrame)
        {
            // 1. Parse the background frame name into the corresponding texture resource key

            var imageKey = ResolveStandaloneMenuBackgroundImage(backgroundFrame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return false;
            }

            // 2. Load the texture from Resources, and return false if the loading fails.

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return false;
            }

            // 3. Create a background image in the center of the screen

            var background = mlpRender.Image(
                "MenuBackground",
                texture,
                mlpConstants.Width2,
                240f,
                0.5f,
                0.5f,
                0,
                runtimeRoot);
            // 4. Scale background image to target size

            background.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * LegacyMenuBackgroundWidth / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * LegacyMenuBackgroundHeight / Mathf.Max(1f, texture.height),
                1f);
            return true;
        }

        // Map the background frame name to the corresponding independent texture resource key

        private static string ResolveStandaloneMenuBackgroundImage(string backgroundFrame)
        {
            return backgroundFrame switch
            {
                "bg10000" => mlpAssets.Images.MenuBackgroundHalloweenSpotlight,
                "bg2blue0000" => mlpAssets.Images.MenuBackgroundMoonlitGym,
                _ => null
            };
        }

        // Try to create a panel with an independent rounded corner panel map (the image quality is better), and return null if it fails.

        private static GameObject TryCreateStandaloneTintPanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. Load the rounded corner panel texture. If the loading fails, null will be returned.

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.PanelFillSoft));
            if (texture == null)
            {
                return null;
            }

            // 2. Create a panel image at the specified location

            var panel = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            // 3. Scale according to target width and height, and set translucent color

            panel.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        /// <summary>
        /// Basketball skin and difficulty selection panel used in the build settings interface.

        /// </summary>
        private void CreateOptionsPanel(string prefix, float centerX)
        {
            // 1. Display "SETTINGS" title text at the top of the panel

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
        /// Displays left and right arrows and a basketball preview, allowing the player to select a basketball skin.

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
            // 1. Display "BALL" title

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

            // 2. Create left and right arrow buttons to switch to the previous/next basketball skin

            menuButtons.Add(new mlpMenuButton("<", centerX - BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, previousBallAction, runtimeRoot));
            menuButtons.Add(new mlpMenuButton(">", centerX + BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, nextBallAction, runtimeRoot));

            // 3. Display a preview of the current basketball between the arrows

            CreateBallPreview(
                $"{key}_Preview",
                mlpBallCatalog.PreviewTheme(selection),
                centerX,
                previewY + 1f,
                BallPreviewPixels,
                19);

            // 4. Display the basketball name below the preview

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
        /// Display a warning message when selecting the highest difficulty.

        /// </summary>
        private void CreateHellDifficultyWarning(float centerX, float y)
        {
            // 1. Display hell difficulty warning text to inform players that the CPU will use additional super skills

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
        // Block 12: Character/Ball Selector and Preview

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Displays character avatar, left and right arrows, and name tag for selecting a fighting character.

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
            // 1. Display title text (such as "CHARACTER" or "P1")

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

            // 2. Create left and right arrow buttons to switch to the previous/next character
            menuButtons.Add(new mlpMenuButton("<", centerX - SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, previousCharacterAction, runtimeRoot));
            menuButtons.Add(new mlpMenuButton(">", centerX + SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, nextCharacterAction, runtimeRoot));

            // 3. Display character animation preview model

            CreatePreviewPlayer(key, characterId, centerX, previewY, previewScale);
            // 4. Display character skill icons and skill names

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
            // 5. Display character name

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

        // Draw character skill icons (light ball background + skill icon overlay)

        private void CreateCharacterSkillIcon(string name, mlpCharacterSkillDefinition skillDefinition, float x, float y, int sortingOrder)
        {
            const float orbPixels = 52f;
            const float iconPixels = 42f;
            // 1. Try to load an independent circular light ball background texture

            var orbTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.EmblemOrb));
            if (orbTexture != null)
            {
                // 2. Use independent textures to create a light sphere background and scale it to the target size

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
                // 3. When the independent texture is unavailable, use the light ball sprite in the album instead.

                var fallbackOrb = mlpRender.Sprite($"{name}_OrbFallback", mlpAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
                fallbackOrb.transform.localScale *= orbPixels / 150f;
            }

            // 4. If the character does not have independent skill icon art, this is the end

            if (!skillDefinition.HasStandaloneIconArt)
            {
                return;
            }

            // 5. Load and create the skill icon, superimposed on the light ball

            var iconPath = mlpAssets.Images.ResourcePath(skillDefinition.IconImageKey);
            mlpIconButton.CreateImageIcon(name, iconPath, x, y, sortingOrder + 1, iconPixels, runtimeRoot);
        }

        /// <summary>
        /// Renders a small preview of the selected basketball skin.

        /// </summary>
        private void CreateBallPreview(string name, mlpBallTheme theme, float x, float y, float targetPixels, int sortingOrder)
        {
            // 1. Load the basketball theme sprite. If loading fails, the default basketball sprite will be used.

            var sprite = mlpGameplaySpriteLoader.LoadBallThemeSprite(theme, 0.5f, 0.5f) ??
                         mlpAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);

            if (sprite == null)
            {
                return;
            }

            // 2. Create a new game object, add a sprite renderer and set the sprite and sorting levels

            var preview = new GameObject(name);
            preview.transform.SetParent(runtimeRoot, false);
            var renderer = preview.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // 3. Calculate the scaling ratio based on the target pixel size and apply the pixel alignment transformation

            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetPixels / Mathf.Max(1f, spritePixels);
            mlpRender.ApplyPixelTransform(preview.transform, x, y, 0f, scale);
        }

        /// <summary>
        /// Display real-time animated character models on the settings interface.

        /// </summary>
        private void CreatePreviewPlayer(string key, int characterId, float x, float y, float scale)
        {
            // 1. Calculate the preview zoom ratio (basic zoom x character-specific zoom ratio)

            var previewScale = scale * PreviewScaleFactor * mlpPlayersData.GetCharacterPreviewScaleMultiplier(characterId);
            // 2. Draw a semi-transparent shadow under the character’s feet

            var shadow = mlpRender.Sprite($"{key}_PreviewShadow", mlpAtlasCache.Instance.Interface, "loginSelect0000", x, y + PreviewShadowYOffset, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= PreviewShadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.55f);

            // 3. Create a preview root node and set the position and scale of pixel alignment

            var previewRoot = new GameObject($"{key}_Preview");
            previewRoot.transform.SetParent(runtimeRoot, false);
            mlpRender.ApplyPixelTransform(previewRoot.transform, x, y, 0f, previewScale);

            // 4. Build the character skeleton animation model, skip if failed

            var armature = mlpPlayersData.BuildGameplayArmature($"{key}_PreviewArmature");
            if (armature == null)
            {
                return;
            }

            // 5. Hang the skeleton under the preview root node, set the vertical offset and scaling, and apply the character appearance

            armature.transform.SetParent(previewRoot.transform, false);
            armature.transform.localPosition = new Vector3(0f, PreviewArmatureYOffset + mlpPlayersData.GetCharacterPreviewOffsetY(characterId), 0f);
            armature.transform.localScale = new Vector3(PreviewArmatureScale, PreviewArmatureScale, 1f);
            mlpPlayersData.ApplyCharacter(armature, characterId);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Block 13: Tournament matchup chart (regular season standings + playoff matchups)

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Draw complete tournament matchups, including season banners and playoff rounds.

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

        // Display the banner title of the current tournament stage at the top of the matchup map (such as "FINAL FOUR", "GRAND FINAL")

        private void CreateTournamentSeasonBanner(mlpTournamentData tournament)
        {
            // 1. Get the title text of the current event stage

            var title = mlpSinglePlayerNarrative.GetTournamentStageTitle(tournament);
            if (title == "DIVISIONS")
            {
                // 2. If it is the divisional stage, the banner will not be displayed and return directly.
                return;
            }

            // 3. Create a title background panel

            CreatePanel(
                "TournamentSeasonBannerPanel",
                mlpConstants.Width2,
                98f,
                204f,
                24f,
                18,
                new Color(0.03f, 0.06f, 0.1f, 0.74f));
            // 4. Display stage title text on the background (such as "regular season", "semi-finals", etc.)

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
        /// Draw a regular season standings table showing win and loss records.

        /// </summary>
        private void CreateTournamentRegularSeasonBoard(mlpTournamentData tournament)
        {
            // 1. Create an overall translucent background for the league table

            CreatePanel(
                "RegularSeasonBackdrop",
                mlpConstants.Width2,
                236f,
                744f,
                302f,
                8,
                new Color(0.01f, 0.04f, 0.08f, 0.28f));

            // 2. Draw the ranking list of teams in Division A on the left

            CreateTournamentDivisionBoard("DivisionA", 222f, 242f, "DIV. A", tournament, 0);
            // 3. Draw the ranking list of teams in Division B on the right

            CreateTournamentDivisionBoard("DivisionB", 578f, 242f, "DIV. B", tournament, 1);
        }

        /// <summary>
        /// Plots a division column from the season standings, containing team entries.

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
        /// Add a text label inside the ranking row cell.

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
        /// Add highlighted text labels to the ranking row cells.

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
        /// Draw a playoff matchup chart, including semifinal and final game panels.

        /// </summary>
        private void CreateTournamentPlayoffBoard(mlpTournamentData tournament)
        {
            const float playoffBackdropY = 232f;
            const float playoffBackdropHeight = 292f;
            const float finalPanelY = 168f;
            const float semiPanelY = 238f;
            const float semiPanelOffsetX = 200f;
            const float placementPanelY = 324f;

            // 1. Create the overall background panel for the playoffs

            CreatePanel(
                "PlayoffBackdrop",
                mlpConstants.Width2,
                playoffBackdropY,
                742f,
                playoffBackdropHeight,
                8,
                new Color(0.02f, 0.05f, 0.08f, 0.28f));

            // 2. Determine whether it is currently in the semi-finals stage (for highlighting)

            var semiCurrent = !tournament.Completed && tournament.CurrentStage == mlpTournamentStage.SemiFinal;
            // 3. Draw the semi-final game card on the left

            CreateTournamentPlayoffMatchPanel(
                "SemiFinalLeft",
                mlpConstants.Width2 - semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[0],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[0], tournament.PlayerCharacterId));
            // 4. Draw the semi-final match card on the right

            CreateTournamentPlayoffMatchPanel(
                "SemiFinalRight",
                mlpConstants.Width2 + semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[1],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[1], tournament.PlayerCharacterId));
            // 5. Draw the final game card

            CreateTournamentPlayoffMatchPanel(
                "FinalMatch",
                mlpConstants.Width2,
                finalPanelY,
                "FINAL",
                tournament.FinalResult,
                !tournament.Completed && tournament.CurrentStage == mlpTournamentStage.Final);
            // 6. Draw the third place game card

            CreateTournamentPlayoffMatchPanel(
                "ThirdPlaceMatch",
                mlpConstants.Width2,
                placementPanelY,
                "3RD PLACE MATCH",
                tournament.ThirdPlaceResult,
                !tournament.Completed && tournament.CurrentStage == mlpTournamentStage.ThirdPlace);
        }

        /// <summary>
        /// Draw a single playoff game card showing both player slots.

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

            // 1. Select the panel color according to the game status (currently in progress/completed/not started)

            var tint = current
                ? new Color(1f, 0.78f, 0.52f, 1f)
                : match.Completed
                    ? new Color(0.8f, 0.98f, 0.9f, 1f)
                    : new Color(0.76f, 0.82f, 0.9f, 0.9f);
            // 2. Select the border style (highlight border for the current game, normal border for other games)

            var frame = current ? "MatchBack0002" : "MatchBack0001";

            // 3. Create a bordered match card panel

            CreateFramedPanel($"{key}_Frame", frame, x, y, panelWidth, panelHeight, 13, tint);
            // 4. Draw a semi-transparent shadow layer inside the panel

            CreatePanel(
                $"{key}_Shade",
                x,
                y,
                panelWidth - 20f,
                panelHeight - 16f,
                14,
                new Color(0.03f, 0.05f, 0.08f, 0.3f));

            // 5. Display the competition stage title (such as "SEMIFINAL", "FINAL")

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

            // 6. Draw a horizontal dividing line to separate the two player areas

            CreatePanel($"{key}_DividerHorizontal", x, y, panelWidth - 28f, 2f, 15, new Color(0.3f, 0.86f, 0.9f, 0.46f));

            // 7. Draw the avatar, name and score of the player on the left
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
            // 8. Draw the avatar, name and score of the player on the right

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

            // 9. Display the battle separation mark when the match is not completed.

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
        /// Draw a player slot on the playoff game card, complete with avatar and name.

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
            // 1. Select the luminous color (gold/cyan/green) according to the victory or defeat and the game status.

            var glowColor = winner
                ? new Color(1f, 0.74f, 0.28f, 0.36f)
                : current
                    ? new Color(0.3f, 0.96f, 1f, 0.32f)
                    : new Color(0.24f, 0.94f, 0.78f, 0.24f);
            // 2. Draw player avatar badge (with luminous base)

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

            // 3. Show player name, winner highlighted in gold

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
            // 4. Display player scores, empty when the game is not over

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
        /// A summary of the results is drawn at the bottom of the matchup chart, showing player status.

        /// </summary>
        private void CreateTournamentSummaryPanel(mlpTournamentData tournament)
        {
            // 1. Adjust the panel size and position according to whether the event is over or not.

            var summaryCompleted = tournament.Completed;
            var summaryWidth = summaryCompleted ? 312f : 328f;
            var summaryHeight = summaryCompleted ? 72f : 40f;
            var summaryY = summaryCompleted ? TournamentSummaryY : TournamentSummaryY + 6f;
            // 2. Create a bordered summary panel

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

            // 3. Display champion information and player rankings when the event is over

            if (summaryCompleted)
            {
                // 4. Draw a small badge with the champion’s avatar

                CreateTournamentMiniBadge("ChampionBadge", tournament.ChampionCharacterId, 280f, summaryY, 16);
                // 5. Display "CHAMPION" label

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
                // 6. Display the name of the champion character

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
                // 7. Display the player's final ranking (such as "YOU FINISHED #3")

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

            // 8. When the event is in progress, prompt titles and details are generated based on the current stage.

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

            // 9. Display summary information text (such as "ROUND 3 - P1 VS P4")

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
        // Block 14: Tournament Tools Methods (Avatar, Badge, Summary Panel)

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Draw small label badges (e.g. wins, losses, seedings) on the bracket map.

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
            // 1. If no parent object is specified, it will be hung to the runtime root node by default.

            if (parent == null)
            {
                parent = runtimeRoot;
            }

            // 2. Calculate the pixel size of the badge base (take the maximum value of the avatar and zoom)

            const float legacyBadgePixels = 150f;
            var badgePixels = Mathf.Max(
                portraitPixels + 10f,
                Mathf.Max(legacyBadgePixels * badgeScale, legacyBadgePixels * glowScale * 0.82f));
            // 3. Mix the luminous color to generate the halo edge color

            var ringColor = new Color(
                Mathf.Clamp01(0.48f + glowColor.r * 0.55f),
                Mathf.Clamp01(0.48f + glowColor.g * 0.55f),
                Mathf.Clamp01(0.48f + glowColor.b * 0.55f),
                0.96f);
            // 4. Draw the base of the badge (including luminous circle and dark background)

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

            // 5. If the character ID is valid, superimpose the character avatar sprite
            if (characterId >= 0)
            {
                CreateTournamentPortrait($"{key}_Portrait", characterId, x, y + 1f, portraitPixels, sortingOrder + 3, parent);
            }
        }

        /// <summary>
        /// Convert the number of wins and total games to a percentage string, such as "75.0%".

        /// </summary>
        private static string FormatWinningPercentage(float value)
        {
            return value.ToString("0.000");
        }

        /// <summary>
        /// Returns true if any slot in the given match contains a player character.

        /// </summary>
        private static bool MatchIncludesPlayer(mlpTournamentMatchResult match, int playerCharacterId)
        {
            return match != null && (match.LeftCharacterId == playerCharacterId || match.RightCharacterId == playerCharacterId);
        }

        /// <summary>
        /// Find playoff games that include players.

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
        /// Returns a short label for the matchup, such as "P1 vs P3".

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
        // Block 15: Championship Awards Ceremony

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Build a complete award ceremony interface, including podium, trophy and banner.

        /// </summary>
        private void CreateTournamentAwardsScene(mlpTournamentData tournament)
        {
            // 1. Reset all awards animation status

            ResetTournamentAwardsState();

            // 2. Construct the top three ranking data and obtain the accent colors of the players’ corresponding rankings.

            var placements = BuildTournamentAwardsPlacements(tournament);
            var accentColor = GetTournamentAwardsAccentColor(tournament.PlayerPlacement);

            // 3. Create a display area group and load the display background image (replace it with a bordered panel if it fails)

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

            // 4. The entry slide-in animation of the registration display area

            RegisterTournamentAwardsAnimation(showcaseGroup.transform, new Vector2(0f, 10f), 0.04f, 0.42f, 0.96f);

            // 5. Create a result banner group and load the nameplate image (replace it with a panel if it fails)

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

            // 6. Display player result information (such as "CHAMPION", "RUNNER-UP")

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
            // 7. Display ending narrative text

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
            // 8. Admission slide-in animation for registration banner

            RegisterTournamentAwardsAnimation(bannerGroup.transform, new Vector2(0f, 14f), 0.1f, 0.42f, 0.95f);

            // 9. Create a podium group and load the base image (replace it with the album wizard if it fails)

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

            // 10. Draw podium name labels for second place, first place, and third place

            CreateTournamentAwardsLaneLabel("AwardsLaneSecond", placements[1], TournamentAwardsLeftX, 384f, false, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneChampion", placements[0], TournamentAwardsChampionX, 368f, true, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneThird", placements[2], TournamentAwardsRightX, 390f, false, podiumGroup.transform);
            // 11. Registration animation for podium group entrance

            RegisterTournamentAwardsAnimation(podiumGroup.transform, new Vector2(0f, 18f), 0.14f, 0.52f, 0.97f);

            // 12. Create the second character group (left position, smaller zoom)

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
            // 13. Create a champion character group (middle position, maximum zoom)

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
            // 14. Create the third character group (right position, smaller zoom)

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
        /// Draws a set of award items at the specified location, with labels.

        /// </summary>
        private GameObject CreateTournamentAwardsGroup(string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(runtimeRoot, false);
            return group;
        }

        /// <summary>
        /// Draw small badges used in award ceremonies.

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
        /// Draw track labels during awards ceremony.

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
        /// Draw a set of character portraits during an awards show.
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
            // 1. Create the parent object of the role group

            var group = CreateTournamentAwardsGroup(key);
            // 2. Load the luminous aperture image and make the player character slightly more transparent (replace it with the atlas sprite if it fails)

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

            // 3. Draw the shadow under the character’s feet to make the player character’s shadow more obvious

            var shadow = mlpRender.Sprite($"{key}_Shadow", mlpAtlasCache.Instance.Interface, "loginSelect0000", x, y + 24f, 0.5f, 0.5f, 13, group.transform);
            shadow.transform.localScale *= shadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, placement.IsPlayer ? 0.7f : 0.52f);

            // 4. Create the character root object, set the position and scale

            var playerRoot = new GameObject($"{key}_Root");
            playerRoot.transform.SetParent(group.transform, false);
            var characterScale = scale * mlpPlayersData.GetCharacterPreviewScaleMultiplier(placement.CharacterId);
            mlpRender.ApplyPixelTransform(playerRoot.transform, x, y, z, characterScale);

            // 5. Construct the character skeleton animation and bind it to the root object

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
        /// Create an ordered list of ranking data for award shows.

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
        /// Draw individual ranked entries in the awards ceremony.

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
        /// Add an item to the awards animation queue so that it slides in with delay.

        /// </summary>
        private void RegisterTournamentAwardsAnimation(Transform root, Vector2 startOffsetPixels, float delay, float duration, float startScale = 0.94f, bool fade = true)
        {
            if (root == null)
            {
                return;
            }

            // 1. Collect all elves and text components under this group

            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            var textMeshes = root.GetComponentsInChildren<TextMesh>(true);
            // 2. Create animation data items and record the target position and initial offset

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

            // 3. Record the original colors of all sprites and text

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                item.SpriteBaseColors[i] = spriteRenderers[i].color;
            }

            for (var i = 0; i < textMeshes.Length; i++)
            {
                item.TextBaseColors[i] = textMeshes[i].color;
            }

            // 4. Set the initial offset position, reduction ratio and transparency to zero

            root.localPosition = item.TargetLocalPosition + item.StartLocalOffset;
            root.localScale = item.TargetLocalScale * item.StartScale;
            ApplyTournamentAwardsAlpha(item, fade ? 0f : 1f);
            awardsAnimatedItems.Add(item);
        }

        /// <summary>
        /// The award entrance animation is updated every frame.

        /// </summary>
        private void UpdateTournamentAwardsSequence(float deltaTime)
        {
            if (currentScreen != mlpBootstrapScreen.TournamentAwards || runtimeRoot == null)
            {
                return;
            }

            // 1. Accumulated elapsed time

            awardsElapsed += deltaTime;
            // 2. Traverse all animation items and calculate the easing progress based on time

            for (var i = 0; i < awardsAnimatedItems.Count; i++)
            {
                var item = awardsAnimatedItems[i];
                if (item.Root == null)
                {
                    continue;
                }

                var normalized = Mathf.Clamp01((awardsElapsed - item.Delay) / item.Duration);
                var eased = EaseOutBack01(normalized);
                // 3. Interpolation updates position, scale and transparency

                item.Root.localPosition = item.TargetLocalPosition + Vector3.Lerp(item.StartLocalOffset, Vector3.zero, eased);
                item.Root.localScale = item.TargetLocalScale * Mathf.Lerp(item.StartScale, 1f, Mathf.SmoothStep(0f, 1f, normalized));
                ApplyTournamentAwardsAlpha(item, item.Fade ? normalized : 1f);
            }

            // 4. Trigger the championship celebration animation after delay

            if (!awardsCelebrationTriggered && awardsCelebrationPlayer != null && awardsElapsed >= TournamentAwardsCelebrationDelay)
            {
                awardsCelebrationTriggered = true;
                // 5. Play the "Happy" skeleton animation

                awardsCelebrationPlayer.Play("happiness");
                awardsCelebrationPlayer.RefreshPose();

                // 6. If there is a trophy skeleton, play the corresponding trophy animation.

                var cupArmature = awardsCelebrationPlayer.GetChildArmature("effects stun");
                if (cupArmature != null && !string.IsNullOrEmpty(awardsCelebrationCupAnimation))
                {
                    cupArmature.StopAtStart(awardsCelebrationCupAnimation);
                }
            }
        }

        /// <summary>
        /// Fade all award sprites and text to the specified transparency.

        /// </summary>
        private static void ApplyTournamentAwardsAlpha(TournamentAwardsAnimatedItem item, float alpha)
        {
            // 1. Limit transparency to 0-1 range

            alpha = Mathf.Clamp01(alpha);
            // 2. Traverse all elves and adjust the transparency proportionally

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

            // 3. Traverse all text and adjust the transparency proportionally

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
        /// Converts a pixel space offset to a local space offset relative to the parent Transform.
        /// </summary>
        private static Vector3 PixelOffsetToLocal(Vector2 pixelOffset)
        {
            return new Vector3(pixelOffset.x * mlpConstants.UnitsPerPixel, -pixelOffset.y * mlpConstants.UnitsPerPixel, 0f);
        }

        /// <summary>
        /// Apply overshoot buffering curve. The return value will briefly exceed 1 before falling back down.
        /// </summary>
        private static float EaseOutBack01(float value)
        {
            value = Mathf.Clamp01(value);
            const float overshoot = 1.70158f;
            var shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        /// <summary>
        /// Returns the character ID of the loser in the given match.

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
        /// Returns a congratulation or comfort message corresponding to the player's ranking.
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

        // Returns the corresponding accent color (gold/silver/bronze) based on ranking

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

        // Returns the color of the podium nameplate based on ranking and whether it is a player

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
        /// Returns a ranked short ordinal word, such as "1st", "2nd", "3rd".
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
        /// Draws the character avatar sprite at the given position and scale.

        /// </summary>
        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder)
        {
            return CreateTournamentPortrait(name, characterId, x, y, targetPixels, sortingOrder, runtimeRoot);
        }

        /// <summary>
        /// Draws the character avatar sprite at the given position and scale.

        /// </summary>
        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            // 1. Calculate the target size based on the character and obtain the avatar sprite

            var targetSize = targetPixels * mlpPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var sprite = mlpPlayersData.GetCharacterPortraitSprite(characterId, targetSize);
            if (sprite == null)
            {
                return null;
            }

            // 2. Create GameObject and add SpriteRenderer component

            var portrait = new GameObject(name);
            portrait.transform.SetParent(parent, false);
            var renderer = portrait.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // 3. Calculate the scaling ratio and apply pixel transformation (including character offset)

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
        /// Draw small avatar badges used in tournaments.

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
        /// Draw a bordered panel rectangle on the menu interface.
        /// </summary>
        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            // 1. Delegate to the overloaded version with parent object parameters and hang on the runtime root node by default

            return CreateFramedPanel(name, frame, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        /// <summary>
        /// Draw a bordered panel rectangle on the menu interface (parent object can be specified).

        /// </summary>
        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. Prioritize trying to create panels with independent textures (the image quality is better)

            var standalonePanel = TryCreateStandaloneFrame(name, frame, x, y, width, height, sortingOrder, tint, parent);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            // 2. When independent textures are not available, load sprites from the atlas

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

            // 3. Scale the sprite according to the target size and set the color

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        // Try to create a bordered panel with independent textures, and return null if failed (fallback to the atlas sprite)

        private static GameObject TryCreateStandaloneFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            // 1. Parse the border name into the corresponding texture resource key

            var imageKey = ResolveStandaloneFrameImage(frame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            // 2. Load texture from Resources, return null if failed

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            // 3. Create the image sprite, scale it to the target size and apply color

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
            // 1. Check whether the image resource key is valid

            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            // 2. Load texture from Resources, return null if failed

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            // 3. Determine the parent object and create the picture sprite

            var resolvedParent = parent ?? runtimeRoot;
            var image = mlpRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, resolvedParent);
            image.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                mlpConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            // 4. Scale to target size and apply color (default white)
            image.GetComponent<SpriteRenderer>().color = tint ?? Color.white;
            return image;
        }

        // Map border names to corresponding independent texture resource keys

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
        // Block 16: Story lore panels and comic cutscene aids

        // ═══════════════════════════════════════════════════════════════════

        // Create legend panels for story comics (scroll style, pole decoration, title and body text)

        private void CreateStoryIntroLorePanel(mlpStoryPanelDefinition panel)
        {
            // 1. Check whether the panel is valid and contains legendary content. If it is invalid, skip it.

            if (panel == null || !panel.HasLore)
            {
                return;
            }

            // 2. Calculate the screen position of icons and labels

            var loreIconX = StoryIntroLoreButtonX + StoryIntroLoreIconOffsetX;
            var loreIconY = StoryIntroLoreButtonY + StoryIntroLoreIconOffsetY;
            var loreLabelX = StoryIntroLoreButtonX + StoryIntroLoreLabelOffsetX;

            // 3. Create a clickable legend button (transparent background, no default label)

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
            // 4. Create custom label text next to the button

            storyIntroLoreLabelObject = CreateStoryIntroLoreButtonLabel(
                "lore",
                loreLabelX,
                StoryIntroLoreButtonY + StoryIntroLoreLabelOffsetY);

            // 5. Create art element root objects (icons and decorative lines, hidden when the panel is opened)

            storyIntroLoreArtRoot = new GameObject("StoryIntroLoreArt");
            storyIntroLoreArtRoot.transform.SetParent(runtimeRoot, false);
            // 6. Draw the background aperture of the legend icon

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

            // 7. Create the legendary content root object (panel, wooden pole, text, etc.)

            storyIntroLoreRoot = new GameObject("StoryIntroLoreRoot");
            storyIntroLoreRoot.transform.SetParent(runtimeRoot, false);
            var loreRootTransform = storyIntroLoreRoot.transform;
            // 8. Paint panel shadows and paper background

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
            // 9. Draw the upper and lower scroll wooden poles and four corner end caps

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

            // 10. Get story mode information, display page number, title and text

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

            // 11. Initialize to hidden state

            SetStoryIntroLoreVisibility(false);
        }

        // Toggle the legend panel on/off

        private void ToggleStoryIntroLore()
        {
            SetStoryIntroLoreVisibility(!storyIntroLoreOpen);
        }

        // Switch the display/hide of the legend panel, and simultaneously update the button position, label and animation pause state

        private void SetStoryIntroLoreVisibility(bool isVisible)
        {
            // 1. Save and pause the animation when opening, and restore the previous paused state when closing.

            if (isVisible)
            {
                storyIntroPauseBeforeLore = storyIntroPaused;
                storyIntroPaused = true;
            }
            else
            {
                storyIntroPaused = storyIntroPauseBeforeLore;
            }

            // 2. Update the legend panel open status mark

            storyIntroLoreOpen = isVisible;
            // 3. Show/hide content panel and art elements (both are mutually exclusive)

            if (storyIntroLoreRoot != null)
            {
                storyIntroLoreRoot.SetActive(isVisible);
            }

            if (storyIntroLoreArtRoot != null)
            {
                storyIntroLoreArtRoot.SetActive(!isVisible);
            }

            // 4. Traverse and switch the visibility of all legend text objects

            for (var i = 0; i < storyIntroLoreTextObjects.Count; i++)
            {
                if (storyIntroLoreTextObjects[i] != null)
                {
                    storyIntroLoreTextObjects[i].SetActive(isVisible);
                }
            }

            // 5. Refresh button position, label text and color

            RefreshStoryIntroLoreButtonLayout(isVisible);
            SetStoryIntroLoreButtonLabelText(isVisible ? "hide" : "lore");
            SetStoryIntroLoreButtonLabelColor(isVisible ? StoryIntroLoreOpenLabelColor : StoryIntroLoreClosedLabelColor);
            // 6. Switch icon color (warm color when open, accent color when closed)

            if (storyIntroLoreIconRenderer != null)
            {
                storyIntroLoreIconRenderer.color = isVisible
                    ? new Color(1f, 0.92f, 0.7f, 0.96f)
                    : new Color(storyIntroAccentColor.r, storyIntroAccentColor.g, storyIntroAccentColor.b, 0.84f);
            }

            // 7. Update the pause button text (display close when open, otherwise display pause/continue)

            RefreshStoryIntroPauseButtonLabel();
        }

        // Update the pause button text according to the current state (pause/resume/close)
        private void RefreshStoryIntroPauseButtonLabel()
        {
            if (storyIntroPauseButton == null)
            {
                return;
            }

            storyIntroPauseButton.SetText(storyIntroLoreOpen ? "close" : storyIntroPaused ? "resume" : "pause");
        }

        // ═══════════════════════════════════════════════════════════════════
        // Block 17: Drawing of connecting lines (connecting line segments of tournament matchups)

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Draw a horizontal line segment connecting the two matchup positions.

        /// </summary>
        private void CreateHorizontalConnector(string name, float startX, float endX, float y, bool highlighted, int sortingOrder = 10, float thickness = TournamentConnectorThickness)
        {
            // 1. Calculate the left end position and width

            var left = Mathf.Min(startX, endX);
            var width = Mathf.Abs(endX - startX);
            // 2. Draw a horizontal rectangular line segment, using orange when highlighting, otherwise using light cyan.

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
        /// Draw a vertical line segment connecting the two matchup positions.

        /// </summary>
        private void CreateVerticalConnector(string name, float x, float startY, float endY, bool highlighted, int sortingOrder = 10, float thickness = TournamentConnectorThickness)
        {
            // 1. Calculate top position and height

            var top = Mathf.Min(startY, endY);
            var height = Mathf.Abs(endY - startY);
            // 2. Draw a vertical rectangular line segment, using orange when highlighting, otherwise using light cyan

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
        /// Draw an L-shaped polyline connecting the two matchup positions.

        /// </summary>
        private void CreateElbowConnector(string name, float startX, float startY, float endX, float endY, bool highlighted)
        {
            // 1. Calculate the X coordinate of the midpoint

            var midX = (startX + endX) * 0.5f;
            // 2. Draw horizontal segments + vertical segments + horizontal segments to form an L-shaped polyline

            CreateHorizontalConnector($"{name}_H1", startX, midX, startY, highlighted);
            CreateVerticalConnector($"{name}_V", midX, startY, endY, highlighted);
            CreateHorizontalConnector($"{name}_H2", midX, endX, endY, highlighted);
        }

        /// <summary>
        /// Returns an appropriate font size based on the length of the text. The longer the text, the smaller the font size to prevent exceeding the available width.

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
        /// Clear all awards animation data to reconstruct the award ceremony.

        /// </summary>
        private void ResetTournamentAwardsState()
        {
            // 1. Clear the animation list, reset the timer and celebrate the trigger mark

            awardsAnimatedItems.Clear();
            awardsElapsed = 0f;
            awardsCelebrationTriggered = false;
            // 2. Clear the skeleton and trophy references of the celebration animation

            awardsCelebrationPlayer = null;
            awardsCelebrationCupAnimation = null;
        }

        /// <summary>
        /// Select a default character for two-player mode to ensure both players start with different characters.

        /// </summary>
        private void SeedTwoPlayerSelection()
        {
            // 1. Read the battle archive data

            var match = mlpInventory.Instance.MatchData;
            // 2. Initialize the player character on the left (verify ID validity)

            versusLeftCharacterId = mlpPlayersData.SanitizeCharacterId(match.CharacterIds[0]);
            // 3. Initialize the player character on the right to make sure it is different from the player character on the left

            versusRightCharacterId = mlpPlayersData.SanitizeCharacterId(match.CharacterIds[1], mlpPlayersData.StepCharacterId(versusLeftCharacterId, 1));
        }

        // ═══════════════════════════════════════════════════════════════════
        // Block 18: Cleanup and Miscellaneous Tools Methods

        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Destroy all menus and game objects, resetting the entire scene.

        /// </summary>
        private void ClearRuntime()
        {
            // 1. Close the game core and clear references

            gameCore?.Shutdown();
            gameCore = null;
            // 2. Clear menu buttons and various auxiliary object references

            menuButtons.Clear();
            menuMusicButton = null;
            menuHelpButton = null;
            quickTestMenuToggleButton = null;
            quickTestMenuInfoButton = null;
            quickTestMenuInfoRoot = null;
            quickTestMenuInfoVisible = false;
            // 3. Reset the pause and legend panel status related to the story introduction

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
            // 4. Release native text layer resources

            nativeMenuTextLayer?.Dispose();
            nativeMenuTextLayer = null;
            // 5. Reset the award animation status

            ResetTournamentAwardsState();
            // 6. Destroy all GameObjects under the runtime root node
            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot.gameObject);
                runtimeRoot = null;
            }
        }

        /// <summary>
        /// Toggle the mute status of background music (on or off).

        /// </summary>
        private static void ToggleBackgroundMusic()
        {
            mlpAudio.Instance?.ToggleMusic();
        }

        /// <summary>
        /// Empty stub callback, does not perform any operation.

        /// </summary>
        private static void NoOpAction()
        {
        }

        /// <summary>
        /// Returns the music icon index: 0 = playing, 1 = muted (used to toggle button icon).

        /// </summary>
        private static int GetMusicIconIndex()
        {
            return mlpAudio.Instance != null && mlpAudio.Instance.MusicEnabled ? 0 : 1;
        }

        /// <summary>
        /// Returns the tournament status text, such as "ROUND 3" or "GRAND FINAL", for use in map title display.

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
        /// Returns the character name, or "TBD" (to be determined) if the slot is empty.

        /// </summary>
        private static string CharacterNameOrTbd(int characterId)
        {
            return characterId >= 0 ? mlpPlayersData.GetCharacterName(characterId) : "TBD";
        }

        /// <summary>
        /// Switch to the next or previous character ID and automatically loop back to the beginning when the end is reached.

        /// </summary>
        private static int WrapCharacter(int currentCharacterId, int direction)
        {
            return mlpPlayersData.StepCharacterId(currentCharacterId, direction);
        }
    }
}
