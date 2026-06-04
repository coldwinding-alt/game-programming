// 游戏启动器和菜单界面控制器
// 管理从主菜单到比赛开始的所有界面：选择 1 人或 2 人模式、选择角色和篮球皮肤、选择难度、进入冒险模式或锦标赛、显示故事漫画、锦标赛对阵图和颁奖画面。也负责创建摄像机和音频系统。

using System.Text;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushGameBootstrap : MonoBehaviour
    {
        private static rimrushGameBootstrap activeInstance;

        private enum rimrushBootstrapScreen
        {
            PlayerCount,
            MatchType,
            SinglePlayerCharacterSetup,
            StoryIntro,
            AdventurePreview,
            AdventureMap,
            AdventureResult,
            SinglePlayerSetup,
            TwoPlayerSetup,
            TrainingSetup,
            TournamentSetup,
            TournamentBracket,
            TournamentComplete,
            TournamentAwards
        }

        private sealed class TournamentAwardsPlacement
        {
            public int Placement;
            public int CharacterId;
            public bool IsPlayer;
            public string CupAnimation;
            public Color AccentColor;
            public Color GlowColor;
        }

        private sealed class TournamentAwardsAnimatedItem
        {
            public Transform Root;
            public Vector3 TargetLocalPosition;
            public Vector3 TargetLocalScale;
            public Vector3 StartLocalOffset;
            public float Delay;
            public float Duration;
            public float StartScale = 1f;
            public bool Fade = true;
            public SpriteRenderer[] SpriteRenderers;
            public Color[] SpriteBaseColors;
            public TextMesh[] TextMeshes;
            public Color[] TextBaseColors;
        }

        private sealed class TournamentStandingsCellViewModel
        {
            public string TopText;
            public string BottomText;
            public Color TopColor = Color.white;
            public Color BottomColor = Color.white;
        }

        private sealed class TournamentStandingsRowViewModel
        {
            public int Seed;
            public int CharacterId;
            public bool IsPlayer;
            public bool IsCurrent;
            public bool IsChampion;
            public bool IsFinalist;
            public int Wins;
            public int Losses;
            public string PercentageText;
            public string StatusText;
            public Color StatusColor;
            public TournamentStandingsCellViewModel SemiCell = new TournamentStandingsCellViewModel();
            public TournamentStandingsCellViewModel FinalCell = new TournamentStandingsCellViewModel();
        }

        private readonly System.Collections.Generic.List<rimrushMenuButton> menuButtons = new System.Collections.Generic.List<rimrushMenuButton>();
        private readonly System.Collections.Generic.List<TournamentAwardsAnimatedItem> awardsAnimatedItems = new System.Collections.Generic.List<TournamentAwardsAnimatedItem>();
        private const float SelectorHeaderY = 126f;
        private const float SelectorArrowY = 258f;
        private const float SelectorArrowOffsetX = 74f;
        private const float SelectorArrowSize = 36f;
        private const float PreviewScaleFactor = 0.56f;
        private const float PreviewShadowYOffset = 24f;
        private const float PreviewShadowScale = 0.42f;
        private const float PreviewArmatureYOffset = -24f;
        private const float PreviewArmatureScale = 0.82f;
        private const float NativeUiAspect = rimrushConstants.DisplayW / (float)rimrushConstants.DisplayH;
        private const float MenuLogoCenterY = 96f;
        private const float MenuLogoMaxWidth = 280f;
        private const float MenuLogoMaxHeight = 188f;
        private const float TournamentBoardX = rimrushConstants.Width2;
        private const float TournamentBoardY = 250f;
        private const float TournamentBoardScale = 0.92f;
        private const float TournamentEntrantBadgeX = 252f;
        private const float TournamentEntrantNameX = 282f;
        private const float TournamentEntrantBadgeScale = 0.32f;
        private const float TournamentEntrantGlowScale = 0.38f;
        private const float TournamentEntrantPortraitPixels = 36f;
        private const float TournamentMatchPanelScale = 0.87f;
        private const float TournamentMatchWidth = 132f * TournamentMatchPanelScale;
        private const float TournamentMatchHeight = 102f * TournamentMatchPanelScale;
        private const float TournamentMatchHalfWidth = TournamentMatchWidth * 0.5f;
        private const float TournamentSemiPanelX = 383f;
        private const float TournamentSemiTopY = 184f;
        private const float TournamentSemiBottomY = 314f;
        private const float TournamentFinalPanelX = 521f;
        private const float TournamentFinalPanelY = 249f;
        private const float TournamentMatchRowOffset = 16f;
        private const float TournamentConnectorThickness = 6f;
        private const float TournamentSummaryY = 402f;
        private const float TournamentStandingsBoardX = 236f;
        private const float TournamentStandingsBoardY = 226f;
        private const float TournamentStandingsBoardScale = 0.72f;
        private const float TournamentStandingsTitleY = 121f;
        private const float TournamentStandingsTableX = TournamentStandingsBoardX;
        private const float TournamentStandingsHeaderY = 154f;
        private const float TournamentStandingsHeaderHeight = 24f;
        private const float TournamentStandingsRowStartY = 186f;
        private const float TournamentStandingsRowSpacing = 34f;
        private const float TournamentStandingsRowHeight = 30f;
        private const float TournamentStandingsTableWidth = 248f;
        private const float TournamentStandingsIndexWidth = 22f;
        private const float TournamentStandingsTeamWidth = 100f;
        private const float TournamentStandingsWinsWidth = 32f;
        private const float TournamentStandingsLossesWidth = 32f;
        private const float TournamentStandingsPctWidth = 62f;
        private const float TournamentStandingsBadgeScale = 0.22f;
        private const float TournamentStandingsGlowScale = 0.28f;
        private const float TournamentStandingsPortraitPixels = 28f;
        private const float TournamentFinalsTitleX = 574f;
        private const float TournamentFinalsTitleY = 120f;
        private const float TournamentBracketPanelScale = 0.9f;
        private const float TournamentBracketPanelWidth = 132f * TournamentBracketPanelScale;
        private const float TournamentBracketPanelHeight = 102f * TournamentBracketPanelScale;
        private const float TournamentBracketFinalX = 574f;
        private const float TournamentBracketFinalY = 172f;
        private const float TournamentBracketSemiLeftX = 450f;
        private const float TournamentBracketSemiRightX = 698f;
        private const float TournamentBracketSemiY = 244f;
        private const float TournamentBracketPlacementX = 574f;
        private const float TournamentBracketPlacementY = 332f;
        private const float TournamentBracketRowOffset = 18f;
        private const float TournamentBracketBadgeScale = 0.2f;
        private const float TournamentBracketGlowScale = 0.26f;
        private const float TournamentBracketPortraitPixels = 24f;
        private const float TournamentAwardsShowcaseY = 244f;
        private const float TournamentAwardsShowcaseWidth = 560f;
        private const float TournamentAwardsShowcaseHeight = 322f;
        private const float TournamentAwardsPlaqueY = 132f;
        private const float TournamentAwardsPodiumY = 376f;
        private const float TournamentAwardsPodiumWidth = 428f;
        private const float TournamentAwardsPodiumHeight = 170f;
        private const float TournamentAwardsChampionX = rimrushConstants.Width2;
        private const float TournamentAwardsChampionY = 296f;
        private const float TournamentAwardsLeftX = rimrushConstants.Width2 - 114f;
        private const float TournamentAwardsLeftY = 314f;
        private const float TournamentAwardsRightX = rimrushConstants.Width2 + 114f;
        private const float TournamentAwardsRightY = 320f;
        private const float TournamentAwardsChampionScale = 0.82f;
        private const float TournamentAwardsSideScale = 0.78f;
        private const float TournamentAwardsArmatureScale = 0.82f;
        private const float TournamentAwardsArmatureYOffset = -18f;
        private const float TournamentAwardsCelebrationDelay = 0.66f;
        private const float MenuTopButtonY = 44f;
        private const float MenuMusicButtonX = 770f;
        private const float MenuHelpButtonX = 706f;
        private const float MenuTopButtonSize = 60f;
        private const float MenuTopIconPixels = 58f;
        private const float AdventureMapPanelX = 306f;
        private const float AdventureMapPanelY = 238f;
        private const float AdventureMapPanelWidth = 574f;
        private const float AdventureMapPanelHeight = 348f;
        private const float AdventurePosterX = 702f;
        private const float AdventurePosterY = 270f;
        private const float AdventurePosterWidth = 184f;
        private const float AdventurePosterHeight = 292f;
        private const float AdventureNodeWidth = 68f;
        private const float AdventureNodeHeight = 78f;
        private const float SinglePlayerModeCardY = 300f;
        private const float SinglePlayerModeCardWidth = 318f;
        private const float SinglePlayerModeCardHeight = 254f;
        private const float SinglePlayerModeLeftCardX = 232f;
        private const float SinglePlayerModeRightCardX = 568f;
        private const float StoryPanelX = rimrushConstants.Width2;
        private const float StoryPanelY = 260f;
        private const float StoryPanelWidth = 610f;
        private const float StoryPanelHeight = 264f;
        private const float StoryCinematicWidth = rimrushConstants.Width;
        private const float StoryCinematicHeight = 480f;
        private const float StoryCinematicPageSeconds = 3.0f;
        private const float StoryCinematicFadeSeconds = 0.42f;
        private const float StoryCinematicPanPixels = 14f;
        private const float StoryCinematicZoomAmount = 0.035f;
        private const float StoryIntroTitleY = 28f;
        private const float StoryIntroCaptionY = 466f;
        private const float StoryIntroPauseX = 54f;
        private const float StoryIntroPauseY = 456f;
        private const float StoryIntroSkipX = 748f;
        private const float StoryIntroSkipY = 456f;
        private const float StoryIntroLoreButtonX = 776f;
        private const float StoryIntroLoreButtonY = 234f;
        private const float StoryIntroLoreOpenButtonX = StoryIntroLorePanelX;
        private const float StoryIntroLoreOpenButtonY = StoryIntroLorePanelY + 108f;
        private const float StoryIntroLoreButtonHitWidth = 112f;
        private const float StoryIntroLoreButtonHitHeight = 80f;
        private const float StoryIntroLoreIconOffsetX = 0f;
        private const float StoryIntroLoreIconOffsetY = -18f;
        private const float StoryIntroLoreLabelOffsetX = 0f;
        private const float StoryIntroLoreLabelOffsetY = 18f;
        private const float StoryIntroLoreOpenLabelOffsetX = 0f;
        private const float StoryIntroLoreOpenLabelOffsetY = 0f;
        private const float StoryIntroLoreIconSize = 42f;
        private const float StoryIntroLorePanelX = 638f;
        private const float StoryIntroLorePanelY = 236f;
        private const float StoryIntroLorePanelWidth = 228f;
        private const float StoryIntroLorePanelHeight = 304f;
        private static readonly Color StoryIntroLoreClosedLabelColor = new Color(0.98f, 0.95f, 0.86f, 0.98f);
        private static readonly Color StoryIntroLoreOpenLabelColor = new Color32(0x72, 0x43, 0x1B, 0xFF);
        private static readonly Color StoryIntroLorePageTagColor = new Color32(0x8B, 0x5B, 0x2B, 0xFF);
        private const float LegacyMenuBackgroundWidth = 1398f;
        private const float LegacyMenuBackgroundHeight = 480f;
        private const float LegacyTintPanelSourcePixels = 10f;
        private const float OptionBallHeaderY = 208f;
        private const float OptionBallPreviewY = 232f;
        private const float OptionBallLabelY = 260f;
        private const float BallSelectorArrowOffsetX = 68f;
        private const float BallSelectorArrowSize = 34f;
        private const float BallPreviewPixels = 50f;
        private const float TwoPlayerBallPanelY = 360f;
        private const float TwoPlayerBallHeaderY = 320f;
        private const float TwoPlayerBallPreviewY = 356f;
        private const float TwoPlayerBallLabelY = 394f;
        private const float TwoPlayerBallPanelWidth = 168f;
        private const float TwoPlayerBallPanelHeight = 148f;
        private Transform runtimeRoot;
        private rimrushGameCore gameCore;
        private Camera mainCamera;
        private rimrushFixedResolutionPresenter fixedResolutionPresenter;
        private rimrushBootstrapScreen currentScreen;
        private rimrushParticipantMode pendingParticipantMode = rimrushParticipantMode.OnePlayer;
        private int quickCharacterId;
        private int trainingCharacterId;
        private int tournamentCharacterId;
        private int versusLeftCharacterId;
        private int versusRightCharacterId;
        private rimrushBallSelection quickBallSelection;
        private rimrushBallSelection trainingBallSelection;
        private rimrushBallSelection tournamentBallSelection;
        private rimrushBallSelection versusBallSelection;
        private float awardsElapsed;
        private bool awardsCelebrationTriggered;
        private DBLiteArmature awardsCelebrationPlayer;
        private string awardsCelebrationCupAnimation;
        private rimrushIconButton menuMusicButton;
        private rimrushIconButton menuHelpButton;
        private rimrushNativeMenuTextLayer nativeMenuTextLayer;
        private bool usingNativeUiPresentation;
        private int viewportScreenWidth = -1;
        private int viewportScreenHeight = -1;
        private rimrushSinglePlayerNarrativeMode storyIntroMode = rimrushSinglePlayerNarrativeMode.Adventure;
        private int storyIntroPanelIndex;
        private System.Action storyIntroContinueAction;
        private System.Action storyIntroCancelAction;
        private rimrushMenuButton storyIntroPauseButton;
        private rimrushMenuButton storyIntroLoreButton;
        private bool storyIntroPaused;
        private bool storyIntroLoreOpen;
        private bool storyIntroPauseBeforeLore;
        private float storyIntroElapsed;
        private GameObject storyIntroImageObject;
        private Vector3 storyIntroImageBaseScale;
        private SpriteRenderer storyIntroImageRenderer;
        private Color storyIntroAccentColor = Color.white;
        private GameObject storyIntroLoreRoot;
        private GameObject storyIntroLoreArtRoot;
        private GameObject storyIntroLoreLabelObject;
        private SpriteRenderer storyIntroLoreIconRenderer;
        private readonly System.Collections.Generic.List<GameObject> storyIntroLoreTextObjects = new System.Collections.Generic.List<GameObject>();
        private int adventureSelectedLevelIndex;
        private Texture2D adventureTreasureMapTexture;

        /// <summary>
        /// Set up the camera, audio, and show the main menu when the game starts.
        /// </summary>
        private void Awake()
        {
            activeInstance = this;
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = rimrushConstants.GameH / (2f * rimrushConstants.PixelsPerUnit);
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = Color.black;
            fixedResolutionPresenter = GetComponent<rimrushFixedResolutionPresenter>();
            if (fixedResolutionPresenter == null)
            {
                fixedResolutionPresenter = gameObject.AddComponent<rimrushFixedResolutionPresenter>();
            }

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            EnableNativeMenuPresentation();

            runtimeRoot = new GameObject("rimrushRuntime").transform;
            rimrushAudio.Create(transform);

            var inventory = rimrushInventory.Instance;
            quickCharacterId = rimrushPlayersData.SanitizeCharacterId(inventory.SelectedQuickCharacterId);
            trainingCharacterId = rimrushPlayersData.SanitizeCharacterId(inventory.SelectedTrainingCharacterId, quickCharacterId);
            tournamentCharacterId = rimrushPlayersData.SanitizeCharacterId(inventory.SelectedTournamentCharacterId, quickCharacterId);
            quickBallSelection = inventory.SelectedQuickBallSelection;
            trainingBallSelection = inventory.SelectedTrainingBallSelection;
            tournamentBallSelection = inventory.SelectedTournamentBallSelection;
            versusBallSelection = inventory.SelectedVersusBallSelection;
            SeedTwoPlayerSelection();
            ShowPlayerCountMenu();
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }

            if (adventureTreasureMapTexture != null)
            {
                Destroy(adventureTreasureMapTexture);
                adventureTreasureMapTexture = null;
            }
        }

        public static bool TryStartTutorialFromHelp()
        {
            var bootstrap = activeInstance != null ? activeInstance : FindObjectOfType<rimrushGameBootstrap>();
            if (bootstrap == null)
            {
                return false;
            }

            bootstrap.StartTutorialFromHelpPanel();
            return true;
        }

        /// <summary>
        /// Run each frame: update the game if a match is active, or handle menu input if we are on a menu screen.
        /// </summary>
        private void Update()
        {
            if (gameCore != null)
            {
                gameCore.Update(Time.deltaTime);
                if (gameCore.AdvanceFlowRequested)
                {
                    var inventory = rimrushInventory.Instance;
                    if (inventory.IsAdventureActive)
                    {
                        var playerWon = inventory.MatchData.WhoWins() < 0;
                        ClearRuntime();
                        inventory.AdvanceAdventure(playerWon);
                        ShowAdventureResult(playerWon);
                        return;
                    }

                    ClearRuntime();
                    inventory.AdvanceTournament();
                    ShowTournamentBracket();
                    return;
                }

                if (gameCore.ReturnToMenuRequested)
                {
                    var inventory = rimrushInventory.Instance;
                    var tournamentWasActive = inventory.IsTournamentActive;
                    var adventureWasActive = inventory.IsAdventureActive;
                    ClearRuntime();
                    if (adventureWasActive)
                    {
                        ShowAdventureMap();
                        return;
                    }

                    if (tournamentWasActive)
                    {
                        inventory.AbandonTournament();
                        ShowPlayerCountMenu();
                        return;
                    }

                    if (HandlePendingTutorialAction())
                    {
                        return;
                    }

                    ShowPlayerCountMenu();
                }

                return;
            }

            var helpVisible = rimrushHelpPanel.IsAnyOpen;
            nativeMenuTextLayer?.SetVisible(!helpVisible);

            if (helpVisible)
            {
                return;
            }

            UpdateTournamentAwardsSequence(Time.deltaTime);
            RefreshNativeMenuViewport();

            if (currentScreen == rimrushBootstrapScreen.StoryIntro && UpdateStoryIntroCinematic(Time.deltaTime))
            {
                return;
            }

            for (var i = 0; i < menuButtons.Count; i++)
            {
                var screenRoot = runtimeRoot;
                menuButtons[i].Update(mainCamera);
                if (screenRoot != runtimeRoot)
                {
                    break;
                }
            }

            if (runtimeRoot != null && currentScreen != rimrushBootstrapScreen.StoryIntro)
            {
                menuMusicButton?.SetActiveIconIndex(GetMusicIconIndex());
                var iconScreenRoot = runtimeRoot;
                menuMusicButton?.Update(mainCamera);
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

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleMenuEscape();
            }
        }

        /// <summary>
        /// Handle the Escape key on each menu screen. Goes back to the previous screen or cancels the current action.
        /// </summary>
        private void HandleMenuEscape()
        {
            switch (currentScreen)
            {
                case rimrushBootstrapScreen.MatchType:
                    ShowSinglePlayerCharacterSetup();
                    break;
                case rimrushBootstrapScreen.StoryIntro:
                    if (storyIntroLoreOpen)
                    {
                        SetStoryIntroLoreVisibility(false);
                        break;
                    }

                    CancelSinglePlayerStoryIntro();
                    break;
                case rimrushBootstrapScreen.AdventurePreview:
                case rimrushBootstrapScreen.AdventureMap:
                case rimrushBootstrapScreen.AdventureResult:
                case rimrushBootstrapScreen.SinglePlayerSetup:
                case rimrushBootstrapScreen.TournamentSetup:
                    ShowMatchTypeMenu();
                    break;
                case rimrushBootstrapScreen.SinglePlayerCharacterSetup:
                case rimrushBootstrapScreen.TwoPlayerSetup:
                case rimrushBootstrapScreen.TrainingSetup:
                    ShowPlayerCountMenu();
                    break;
                case rimrushBootstrapScreen.TournamentBracket:
                case rimrushBootstrapScreen.TournamentComplete:
                    rimrushInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                    break;
                case rimrushBootstrapScreen.TournamentAwards:
                    ShowTournamentBracket();
                    break;
            }
        }

        /// <summary>
        /// Show the main menu with buttons for 1 Player, 2 Player, Tutorial, and Training.
        /// </summary>
        private void ShowPlayerCountMenu()
        {
            currentScreen = rimrushBootstrapScreen.PlayerCount;
            BeginMenuScreen(true, false, "bg2blue0000");
            CreatePanel("PlayersPanel", rimrushConstants.Width2, 336f, 304f, 286f, 8, new Color(0.05f, 0.08f, 0.1f, 0.72f));

            var inventory = rimrushInventory.Instance;
            menuButtons.Add(new rimrushMenuButton("1 PLAYER", rimrushConstants.Width2, 246f, 228f, 52f, () =>
            {
                pendingParticipantMode = rimrushParticipantMode.OnePlayer;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowSinglePlayerCharacterSetup();
            }, runtimeRoot));

            menuButtons.Add(new rimrushMenuButton("2 PLAYER", rimrushConstants.Width2, 306f, 228f, 52f, () =>
            {
                pendingParticipantMode = rimrushParticipantMode.TwoPlayers;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowTwoPlayerSetup();
            }, runtimeRoot));

            menuButtons.Add(new rimrushMenuButton("TUTORIAL", rimrushConstants.Width2, 366f, 228f, 52f, StartTutorialFlow, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("TRAINING", rimrushConstants.Width2, 426f, 228f, 52f, ShowTrainingSetup, runtimeRoot));
        }

        private void ShowSinglePlayerCharacterSetup()
        {
            currentScreen = rimrushBootstrapScreen.SinglePlayerCharacterSetup;
            pendingParticipantMode = rimrushParticipantMode.OnePlayer;
            rimrushInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("SELECT CHARACTER", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            CreatePanel("SinglePlayerCharacterPanel", rimrushConstants.Width2, 280f, 260f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreateCharacterSelector(
                "SinglePlayer",
                "CHARACTER",
                rimrushConstants.Width2,
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

            menuButtons.Add(new rimrushMenuButton("BACK", 312f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("NEXT", 488f, 452f, 150f, 42f, ConfirmSinglePlayerCharacter, runtimeRoot));
        }

        private void ConfirmSinglePlayerCharacter()
        {
            tournamentCharacterId = quickCharacterId;
            var inventory = rimrushInventory.Instance;
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetTournamentSelection(tournamentCharacterId);
            ShowMatchTypeMenu();
        }

        /// <summary>
        /// Show the mode choice screen with Adventure (Story Run) and Tournament (Season Run) cards.
        /// </summary>
        private void ShowMatchTypeMenu()
        {
            currentScreen = rimrushBootstrapScreen.MatchType;
            pendingParticipantMode = rimrushParticipantMode.OnePlayer;
            BeginMenuScreen(true, false, "bg10000");
            CreateSinglePlayerModeCard(
                "Adventure",
                rimrushSinglePlayerNarrative.Adventure,
                SinglePlayerModeLeftCardX,
                new Color(0.06f, 0.10f, 0.11f, 0.92f),
                new Color32(0xFF, 0x9F, 0x32, 0xFF),
                "STORY RUN",
                "Warden duels. Sigils. Escape.",
                "Follow the park map",
                "and reopen the gates.",
                "START ROUTE",
                ShowAdventureStoryIntro);

            CreateSinglePlayerModeCard(
                "Tournament",
                rimrushSinglePlayerNarrative.Tournament,
                SinglePlayerModeRightCardX,
                new Color(0.05f, 0.07f, 0.14f, 0.92f),
                new Color32(0x78, 0xE7, 0xFF, 0xFF),
                "SEASON RUN",
                "Divisions. Finals. Trophy.",
                "Beat the bracket",
                "and claim the Cup.",
                "ENTER CUP",
                ShowTournamentStoryIntro);

            menuButtons.Add(new rimrushMenuButton("BACK", rimrushConstants.Width2, 442f, 180f, 42f, ShowSinglePlayerCharacterSetup, runtimeRoot));
        }

        private void ShowAdventureStoryIntro()
        {
            ShowSinglePlayerStoryIntro(rimrushSinglePlayerNarrativeMode.Adventure, ShowAdventureMap);
        }

        private void ShowTournamentStoryIntro()
        {
            ShowSinglePlayerStoryIntro(rimrushSinglePlayerNarrativeMode.Tournament, StartTournamentFlow);
        }

        private void ShowSinglePlayerStoryIntro(
            rimrushSinglePlayerNarrativeMode mode,
            System.Action continueAction,
            System.Action cancelAction = null)
        {
            storyIntroMode = mode;
            storyIntroPanelIndex = 0;
            storyIntroContinueAction = continueAction;
            storyIntroCancelAction = cancelAction ?? ShowMatchTypeMenu;
            storyIntroPaused = false;
            storyIntroLoreOpen = false;
            storyIntroPauseBeforeLore = false;
            ShowSinglePlayerStoryIntroPanel();
        }

        private void ShowSinglePlayerStoryIntroPanel()
        {
            var mode = rimrushSinglePlayerNarrative.GetMode(storyIntroMode);
            var panels = mode.OpeningComic;
            if (panels == null || panels.Length == 0)
            {
                ContinueSinglePlayerStoryIntro();
                return;
            }

            storyIntroPanelIndex = Mathf.Clamp(storyIntroPanelIndex, 0, panels.Length - 1);
            var panel = panels[storyIntroPanelIndex];
            var isAdventure = storyIntroMode == rimrushSinglePlayerNarrativeMode.Adventure;
            var accentColor = isAdventure
                ? new Color32(0xFF, 0xA6, 0x39, 0xFF)
                : new Color32(0x78, 0xE7, 0xFF, 0xFF);
            storyIntroAccentColor = accentColor;
            var backgroundFrame = isAdventure ? "bg10000" : "bg2blue0000";

            currentScreen = rimrushBootstrapScreen.StoryIntro;
            BeginMenuScreen(false, false, backgroundFrame);
            menuMusicButton?.SetVisible(false);
            menuHelpButton?.SetVisible(false);
            storyIntroElapsed = 0f;
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
                rimrushConstants.Width2,
                StoryIntroTitleY,
                24,
                accentColor,
                TextAnchor.MiddleCenter,
                24,
                rimrushTextStyle.TournamentAccent);
            CreateMenuText(
                "StoryIntroCaption",
                panel.Caption,
                StoryPanelX,
                StoryIntroCaptionY,
                14,
                Color.white,
                TextAnchor.MiddleCenter,
                24,
                rimrushTextStyle.TournamentBody);
            CreateStoryIntroLorePanel(panel);

            storyIntroPauseButton = new rimrushMenuButton(
                "pause",
                StoryIntroPauseX,
                StoryIntroPauseY,
                86f,
                26f,
                ToggleStoryIntroPause,
                runtimeRoot,
                26,
                rimrushTextStyle.LinkLabel);
            storyIntroPauseButton.SetBackgroundVisible(false);
            menuButtons.Add(storyIntroPauseButton);
            RefreshStoryIntroPauseButtonLabel();
            CreatePanel("StoryIntroPauseUnderline", StoryIntroPauseX, StoryIntroPauseY + 13f, 56f, 2f, 24, accentColor);

            var skipButton = new rimrushMenuButton(
                "skip",
                StoryIntroSkipX,
                StoryIntroSkipY,
                72f,
                26f,
                ContinueSinglePlayerStoryIntro,
                runtimeRoot,
                26,
                rimrushTextStyle.LinkLabel);
            skipButton.SetBackgroundVisible(false);
            menuButtons.Add(skipButton);
            CreatePanel("StoryIntroSkipUnderline", StoryIntroSkipX, StoryIntroSkipY + 13f, 31f, 2f, 24, accentColor);
        }

        private bool CreateStoryIntroComicImage(rimrushStoryPanelDefinition panel)
        {
            if (panel == null || string.IsNullOrEmpty(panel.ImageKey))
            {
                return false;
            }

            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(panel.ImageKey));
            if (texture == null)
            {
                return false;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            storyIntroImageObject = rimrushRender.Image(
                "StoryIntroComicPage",
                texture,
                rimrushConstants.Width2,
                StoryCinematicHeight * 0.5f,
                0.5f,
                0.5f,
                12,
                runtimeRoot);

            var coverScale = Mathf.Max(
                StoryCinematicWidth / Mathf.Max(1f, texture.width),
                StoryCinematicHeight / Mathf.Max(1f, texture.height));
            storyIntroImageBaseScale = Vector3.one * rimrushConstants.UnitsPerPixel * coverScale;
            storyIntroImageBaseScale.z = 1f;
            storyIntroImageRenderer = storyIntroImageObject.GetComponent<SpriteRenderer>();
            if (storyIntroImageRenderer != null)
            {
                storyIntroImageRenderer.color = new Color(1f, 1f, 1f, 0f);
            }

            SetStoryIntroImageTransform(0f, 1f);
            return true;
        }

        private void CreateStoryIntroFallbackPanel(rimrushStoryPanelDefinition panel, Color accentColor)
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
                rimrushTextStyle.TournamentBody);
        }

        private bool UpdateStoryIntroCinematic(float deltaTime)
        {
            if (storyIntroPaused)
            {
                return false;
            }

            storyIntroElapsed += Mathf.Max(0f, deltaTime);
            var normalized = Mathf.Clamp01(storyIntroElapsed / StoryCinematicPageSeconds);
            var eased = Mathf.SmoothStep(0f, 1f, normalized);
            var fade = Mathf.Clamp01(storyIntroElapsed / StoryCinematicFadeSeconds);
            if (storyIntroImageObject != null)
            {
                SetStoryIntroImageTransform(eased, 1f + StoryCinematicZoomAmount * eased);

                if (storyIntroImageRenderer != null)
                {
                    storyIntroImageRenderer.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, fade));
                }
            }

            if (storyIntroElapsed >= StoryCinematicPageSeconds)
            {
                AdvanceSinglePlayerStoryIntro();
                return true;
            }

            return false;
        }

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

        private void SetStoryIntroImageTransform(float normalized, float zoom)
        {
            if (storyIntroImageObject == null)
            {
                return;
            }

            var direction = storyIntroPanelIndex % 2 == 0 ? 1f : -1f;
            var panX = Mathf.Lerp(-StoryCinematicPanPixels, StoryCinematicPanPixels, normalized) * direction;
            var panY = Mathf.Sin(normalized * Mathf.PI) * 4f;
            storyIntroImageObject.transform.position = rimrushConstants.PixelToWorldSnapped(
                rimrushConstants.Width2 + panX,
                StoryCinematicHeight * 0.5f + panY,
                0f);
            storyIntroImageObject.transform.localScale = storyIntroImageBaseScale * zoom;
            storyIntroImageObject.transform.localScale = new Vector3(
                storyIntroImageObject.transform.localScale.x,
                storyIntroImageObject.transform.localScale.y,
                1f);
        }

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

        private void AdvanceSinglePlayerStoryIntro()
        {
            var mode = rimrushSinglePlayerNarrative.GetMode(storyIntroMode);
            var panelCount = mode.OpeningComic != null ? mode.OpeningComic.Length : 0;
            if (storyIntroPanelIndex < panelCount - 1)
            {
                storyIntroPanelIndex++;
                ShowSinglePlayerStoryIntroPanel();
                return;
            }

            ContinueSinglePlayerStoryIntro();
        }

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

        private void ShowAdventureComicReplay()
        {
            ShowSinglePlayerStoryIntro(rimrushSinglePlayerNarrativeMode.Adventure, ShowAdventureMap, ShowAdventureMap);
        }

        private void ShowTournamentSetupComicReplay()
        {
            ShowSinglePlayerStoryIntro(rimrushSinglePlayerNarrativeMode.Tournament, ShowTournamentSetup, ShowTournamentSetup);
        }

        private void ShowTournamentBracketComicReplay()
        {
            ShowSinglePlayerStoryIntro(rimrushSinglePlayerNarrativeMode.Tournament, ShowTournamentBracket, ShowTournamentBracket);
        }

        private void ShowAdventurePreview()
        {
            currentScreen = rimrushBootstrapScreen.AdventurePreview;
            pendingParticipantMode = rimrushParticipantMode.OnePlayer;
            rimrushInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle(rimrushSinglePlayerNarrative.Adventure.MenuTitle, 54f, 30, new Color32(0xFF, 0xB6, 0x45, 0xFF));
            AddSubtitle(rimrushSinglePlayerNarrative.Adventure.Subtitle, 90f, 14);

            CreatePanel("AdventurePreviewPanel", rimrushConstants.Width2, 274f, 590f, 276f, 8, new Color(0.04f, 0.07f, 0.1f, 0.84f));
            CreatePanel("AdventurePreviewAccent", rimrushConstants.Width2, 149f, 520f, 8f, 9, new Color(1f, 0.55f, 0.18f, 0.92f));
            CreateMenuText(
                "AdventurePreviewStatus",
                rimrushSinglePlayerNarrative.AdventurePreviewStatus,
                rimrushConstants.Width2,
                188f,
                18,
                new Color32(0xFF, 0xCF, 0x75, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);
            CreateMenuText(
                "AdventurePreviewObjective",
                "Goal: win 1v1 duels, collect Lantern Sigils, and escape before dawn.",
                rimrushConstants.Width2,
                236f,
                13,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);
            CreateMenuText(
                "AdventurePreviewLoop",
                "Next build step: park map, locked routes, and the first Warden gate.",
                rimrushConstants.Width2,
                276f,
                13,
                new Color32(0xCC, 0xE5, 0xF0, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);
            CreateMenuText(
                "AdventurePreviewSafeRoute",
                "Use Quick Duel only as a temporary 1v1 practice path until Adventure flow lands.",
                rimrushConstants.Width2,
                316f,
                12,
                new Color32(0xFF, 0xD8, 0x9E, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);

            menuButtons.Add(new rimrushMenuButton("BACK", 220f, 452f, 180f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("QUICK DUEL", 580f, 452f, 200f, 42f, ShowSinglePlayerSetup, runtimeRoot));
        }

        private void ShowAdventureMap()
        {
            currentScreen = rimrushBootstrapScreen.AdventureMap;
            pendingParticipantMode = rimrushParticipantMode.OnePlayer;
            var inventory = rimrushInventory.Instance;
            inventory.SetParticipantMode(pendingParticipantMode);
            if (!inventory.IsAdventureActive)
            {
                inventory.BeginAdventure(quickCharacterId);
            }

            var adventure = inventory.Adventure;
            if (adventure.Completed)
            {
                adventureSelectedLevelIndex = Mathf.Max(0, adventure.LastResolvedLevelIndex);
            }
            else if (!adventure.IsLevelUnlocked(adventureSelectedLevelIndex))
            {
                adventureSelectedLevelIndex = adventure.CurrentLevelIndex;
            }

            BeginMenuScreen(false, false, "bg10000");

            CreateAdventureTreasureMapFrame();
            CreateAdventureRouteMap(adventure);
            CreateAdventureLevelPoster(adventure, adventureSelectedLevelIndex);
            if (adventure.Completed)
            {
                menuButtons.Add(new rimrushMenuButton("MAIN MENU", 250f, 452f, 176f, 42f, ShowPlayerCountMenu, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton(rimrushSinglePlayerNarrative.ComicReplayButton, 550f, 452f, 176f, 42f, ShowAdventureComicReplay, runtimeRoot));
                return;
            }

            menuButtons.Add(new rimrushMenuButton("BACK", 124f, 452f, 132f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton(rimrushSinglePlayerNarrative.ComicReplayButton, 400f, 452f, 160f, 42f, ShowAdventureComicReplay, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("PLAY LEVEL", 656f, 452f, 188f, 42f, StartAdventureLevelFlow, runtimeRoot));
        }

        private void CreateAdventureTreasureMapFrame()
        {
            CreatePanel(
                "AdventureMapDropShadow",
                AdventureMapPanelX + 7f,
                AdventureMapPanelY + 10f,
                AdventureMapPanelWidth + 30f,
                AdventureMapPanelHeight + 28f,
                7,
                new Color(0f, 0f, 0f, 0.3f));
            CreatePanel(
                "AdventureMapCopperFrame",
                AdventureMapPanelX,
                AdventureMapPanelY,
                AdventureMapPanelWidth + 18f,
                AdventureMapPanelHeight + 18f,
                8,
                new Color(0.5f, 0.22f, 0.08f, 0.76f));

            var texture = GetAdventureTreasureMapTexture();
            if (texture != null)
            {
                var map = rimrushRender.Image(
                    "AdventureTreasureMap",
                    texture,
                    AdventureMapPanelX,
                    AdventureMapPanelY,
                    0.5f,
                    0.5f,
                    9,
                    runtimeRoot);
                map.transform.localScale = new Vector3(
                    rimrushConstants.UnitsPerPixel * AdventureMapPanelWidth / Mathf.Max(1f, texture.width),
                    rimrushConstants.UnitsPerPixel * AdventureMapPanelHeight / Mathf.Max(1f, texture.height),
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

            CreatePanel("AdventureMapReadabilityWash", AdventureMapPanelX - 28f, AdventureMapPanelY + 10f, AdventureMapPanelWidth - 168f, AdventureMapPanelHeight - 150f, 10, new Color(1f, 0.88f, 0.56f, 0.055f));
            CreatePanel("AdventureMapInnerShade", AdventureMapPanelX, AdventureMapPanelY + AdventureMapPanelHeight * 0.5f - 18f, AdventureMapPanelWidth - 68f, 7f, 10, new Color(0.13f, 0.06f, 0.02f, 0.24f));

            CreateMenuText(
                "AdventureMapStamp",
                "ESCAPE ROUTE",
                AdventureMapPanelX - AdventureMapPanelWidth * 0.5f + 92f,
                AdventureMapPanelY - AdventureMapPanelHeight * 0.5f + 30f,
                12,
                new Color32(0x39, 0x1E, 0x0B, 0xFF),
                TextAnchor.MiddleCenter,
                18,
                rimrushTextStyle.TournamentAccent);
        }

        private Texture2D GetAdventureTreasureMapTexture()
        {
            var assetTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.AdventureTreasureMapBg));
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

        private void CreateAdventureRouteMap(rimrushAdventureData adventure)
        {
            var levels = rimrushAdventureCatalog.AllLevels;
            for (var i = 1; i < levels.Length; i++)
            {
                var unlocked = adventure.IsLevelUnlocked(i);
                var previousAnchor = GetAdventureNodeRouteAnchor(i - 1);
                var currentAnchor = GetAdventureNodeRouteAnchor(i);
                CreateAdventureConnector(i, previousAnchor.x, previousAnchor.y, currentAnchor.x, currentAnchor.y, unlocked);
            }

            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                var unlocked = adventure.IsLevelUnlocked(i);
                var completed = adventure.IsLevelCompleted(i);
                var selected = i == adventureSelectedLevelIndex;
                CreateAdventureRouteNode(level, i, unlocked, completed, selected);

                if (unlocked && !adventure.Completed)
                {
                    var capturedIndex = i;
                    var nodePosition = GetAdventureNodePosition(i);
                    var nodeButton = new rimrushMenuButton($"LV{i + 1:00}", nodePosition.x, nodePosition.y, AdventureNodeWidth + 8f, AdventureNodeHeight + 8f, () =>
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

        private void CreateAdventureRouteNode(
            rimrushAdventureLevelDefinition level,
            int index,
            bool unlocked,
            bool completed,
            bool selected)
        {
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

            CreatePanel($"AdventureNodeShadow_{index}", nodePosition.x + 4f, nodePosition.y + 7f, AdventureNodeWidth + 10f, AdventureNodeHeight + 12f, 13, new Color(0.04f, 0.02f, 0.01f, 0.34f));
            if (selected)
            {
                CreatePanel($"AdventureNodeSelectGlow_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth + 24f, AdventureNodeHeight + 24f, 14, new Color(1f, 0.8f, 0.28f, 0.28f));
            }

            CreatePanel($"AdventureNodePlate_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth, AdventureNodeHeight, 15, borderTint);
            CreatePanel($"AdventureNodeInset_{index}", nodePosition.x, nodePosition.y, AdventureNodeWidth - 8f, AdventureNodeHeight - 8f, 16, fillTint);
            CreatePanel($"AdventureNodePortraitBack_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 22f, AdventureNodeWidth - 22f, 17, portraitBackTint);
            CreatePanel($"AdventureNodePortraitGlow_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 12f, AdventureNodeWidth - 12f, 17, glowColor);
            CreateTournamentPortrait(
                $"AdventureNodePortrait_{index}",
                level.WardenCharacterId,
                nodePosition.x,
                portraitY,
                selected ? 50f : 46f,
                18);
            if (!unlocked)
            {
                CreatePanel($"AdventureNodeLockShade_{index}", nodePosition.x, portraitY, AdventureNodeWidth - 22f, AdventureNodeWidth - 22f, 19, new Color(0.08f, 0.09f, 0.11f, 0.42f));
            }
        }

        private static Vector2 GetAdventureNodePosition(int index)
        {
            if (index < 0 || index >= rimrushAdventureCatalog.LevelCount)
            {
                return new Vector2(AdventureMapPanelX, AdventureMapPanelY);
            }

            var level = rimrushAdventureCatalog.GetLevel(index);
            return new Vector2(level.MapX, level.MapY);
        }

        private static Vector2 GetAdventureNodeRouteAnchor(int index)
        {
            var nodePosition = GetAdventureNodePosition(index);
            return new Vector2(nodePosition.x, nodePosition.y - 2f);
        }

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

        private void CreateAdventureLevelPoster(rimrushAdventureData adventure, int selectedLevelIndex)
        {
            var level = rimrushAdventureCatalog.GetLevel(selectedLevelIndex);
            var unlocked = adventure.IsLevelUnlocked(selectedLevelIndex);
            var completed = adventure.IsLevelCompleted(selectedLevelIndex);
            var activeGate = selectedLevelIndex == adventure.CurrentLevelIndex && !completed && unlocked;
            CreateFramedPanel(
                "AdventurePosterFrame",
                activeGate ? "MatchBack0002" : "MatchBack0001",
                AdventurePosterX,
                AdventurePosterY,
                AdventurePosterWidth,
                AdventurePosterHeight,
                11,
                unlocked ? new Color(1f, 0.84f, 0.58f, 0.96f) : new Color(0.7f, 0.78f, 0.86f, 0.7f));
            CreatePanel("AdventurePosterShade", AdventurePosterX, AdventurePosterY, AdventurePosterWidth - 28f, AdventurePosterHeight - 34f, 12, new Color(0.04f, 0.06f, 0.09f, 0.72f));
            CreatePanel("AdventurePosterAccent", AdventurePosterX, 137f, AdventurePosterWidth - 48f, 4f, 13, unlocked ? new Color(1f, 0.58f, 0.18f, 0.86f) : new Color(0.44f, 0.52f, 0.58f, 0.7f));
            CreateMenuText(
                "AdventurePosterStatus",
                completed ? "CLAIMED" : activeGate ? "NEXT GATE" : unlocked ? "OPEN" : "LOCKED",
                AdventurePosterX,
                152f,
                11,
                completed ? new Color32(0x8F, 0xFF, 0x8B, 0xFF) : unlocked ? new Color32(0xD7, 0xF2, 0x4A, 0xFF) : new Color32(0xB6, 0xC1, 0xCC, 0xFF),
                TextAnchor.MiddleCenter,
                13,
                rimrushTextStyle.TournamentAccent);
            CreateMenuText(
                "AdventurePosterArea",
                level.AreaName,
                AdventurePosterX,
                178f,
                GetCompactFontSize(level.AreaName, 15, 12, 10),
                Color.white,
                TextAnchor.MiddleCenter,
                13,
                rimrushTextStyle.TournamentBody);
            CreatePanel("AdventurePosterPortraitGlow", AdventurePosterX, 232f, 112f, 112f, 13, completed ? new Color(0.56f, 0.98f, 0.66f, 0.16f) : unlocked ? new Color(1f, 0.66f, 0.22f, 0.16f) : new Color(0.72f, 0.78f, 0.86f, 0.08f));
            CreatePanel("AdventurePosterPortraitFrame", AdventurePosterX, 232f, 90f, 96f, 14, unlocked ? new Color(0.98f, 0.84f, 0.56f, 0.94f) : new Color(0.68f, 0.72f, 0.78f, 0.84f));
            CreatePanel("AdventurePosterPortraitInset", AdventurePosterX, 232f, 80f, 86f, 15, unlocked ? new Color(0.98f, 0.94f, 0.86f, 0.96f) : new Color(0.7f, 0.72f, 0.74f, 0.9f));
            CreateTournamentPortrait(
                "AdventurePosterWardenBadge",
                level.WardenCharacterId,
                AdventurePosterX,
                228f,
                70f,
                16);
            var wardenName = rimrushPlayersData.GetCharacterName(level.WardenCharacterId);
            CreateMenuText(
                "AdventurePosterWarden",
                wardenName,
                AdventurePosterX,
                292f,
                GetCompactFontSize(wardenName, 14, 12, 11),
                Color.white,
                TextAnchor.MiddleCenter,
                13,
                rimrushTextStyle.TournamentBody);
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
                rimrushTextStyle.TournamentAccent);
            CreatePanel(
                "AdventurePosterBallGlow",
                AdventurePosterX,
                346f,
                44f,
                44f,
                13,
                completed ? new Color(0.56f, 0.98f, 0.66f, 0.12f) : unlocked ? new Color(1f, 0.64f, 0.22f, 0.12f) : new Color(0.72f, 0.78f, 0.86f, 0.08f));
            CreateBallPreview(
                "AdventurePosterBallPreview",
                rimrushBallCatalog.PreviewTheme(level.BallSelection),
                AdventurePosterX,
                346f,
                34f,
                14);
            CreateAdventureDifficultySelector();
        }

        private void CreateAdventureDifficultySelector()
        {
            menuButtons.Add(new rimrushMenuButton(rimrushInventory.Instance.DifficultyLabel, AdventurePosterX, 380f, 154f, 42f, () =>
            {
                rimrushInventory.Instance.ToggleDifficulty();
                ShowAdventureMap();
            }, runtimeRoot));
        }

        private void StartAdventureLevelFlow()
        {
            var inventory = rimrushInventory.Instance;
            inventory.SetQuickSelection(quickCharacterId);
            if (!inventory.StartAdventureLevel(adventureSelectedLevelIndex, quickCharacterId))
            {
                ShowAdventureMap();
                return;
            }

            StartGameplay();
        }

        private void RetryAdventureLevelFlow()
        {
            var inventory = rimrushInventory.Instance;
            if (!inventory.RestartAdventureLevel())
            {
                ShowAdventureMap();
                return;
            }

            StartGameplay();
        }

        private void ShowAdventureResult(bool playerWon)
        {
            var adventure = rimrushInventory.Instance.Adventure;
            var resolvedIndex = Mathf.Max(0, adventure.LastResolvedLevelIndex);
            var level = rimrushAdventureCatalog.GetLevel(resolvedIndex);
            var resultSpeech = FormatAdventureResultSpeech(level.GetRandomResultLine(playerWon));
            adventureSelectedLevelIndex = playerWon && !adventure.Completed ? adventure.CurrentLevelIndex : resolvedIndex;

            currentScreen = rimrushBootstrapScreen.AdventureResult;
            BeginMenuScreen(false, false, playerWon ? "bg10000" : "bg2blue0000");
            var title = playerWon
                ? adventure.Completed ? "PARK GATE OPEN" : "SIGIL CLAIMED"
                : "WARDEN HOLDS";
            AddTitle(title, 54f, 31, playerWon ? new Color32(0xFF, 0xC7, 0x56, 0xFF) : new Color32(0xDB, 0xE4, 0xF1, 0xFF));
            AddSubtitle(level.AreaName, 90f, 14);

            CreatePanel("AdventureResultPanel", rimrushConstants.Width2, 266f, 560f, 188f, 8, new Color(0.03f, 0.06f, 0.09f, 0.86f));
            CreatePanel("AdventureResultAccent", rimrushConstants.Width2, 177f, 504f, 8f, 9, playerWon ? new Color(1f, 0.65f, 0.24f, 0.92f) : new Color(0.54f, 0.72f, 0.82f, 0.72f));
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
                rimrushPlayersData.GetCharacterName(level.WardenCharacterId),
                292f,
                232f,
                GetCompactFontSize(rimrushPlayersData.GetCharacterName(level.WardenCharacterId), 18, 15, 13),
                Color.white,
                TextAnchor.MiddleLeft,
                10,
                rimrushTextStyle.TournamentBody);
            CreatePanel(
                "AdventureResultDialoguePlate",
                rimrushConstants.Width2,
                314f,
                458f,
                84f,
                9,
                playerWon ? new Color(0.15f, 0.11f, 0.05f, 0.38f) : new Color(0.08f, 0.11f, 0.15f, 0.42f));
            CreateMenuText(
                "AdventureResultStory",
                resultSpeech,
                rimrushConstants.Width2,
                314f,
                GetCompactFontSize(resultSpeech, 15, 14, 12),
                new Color32(0xE8, 0xF1, 0xF7, 0xFF),
                TextAnchor.MiddleCenter,
                10,
                rimrushTextStyle.TournamentBody);

            if (playerWon)
            {
                menuButtons.Add(new rimrushMenuButton("MAP", 220f, 452f, 180f, 42f, ShowAdventureMap, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton(adventure.Completed ? "MAIN MENU" : "CONTINUE", 580f, 452f, 200f, 42f, adventure.Completed ? ShowPlayerCountMenu : ShowAdventureMap, runtimeRoot));
            }
            else
            {
                menuButtons.Add(new rimrushMenuButton("MAP", 220f, 452f, 180f, 42f, ShowAdventureMap, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton("RETRY", 580f, 452f, 200f, 42f, RetryAdventureLevelFlow, runtimeRoot));
            }
        }

        private static string FormatAdventureResultSpeech(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return "\"" + WrapMenuText(text.Trim(), 32) + "\"";
        }

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

        private void CreateSinglePlayerModeCard(
            string key,
            rimrushSinglePlayerModeDefinition mode,
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
            CreateMenuText(
                $"{key}_ModeLabel",
                routeLabel,
                centerX,
                206f,
                13,
                accentColor,
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);
            CreateMenuText(
                $"{key}_ModeTitle",
                mode.MenuTitle,
                centerX,
                274f,
                GetCompactFontSize(mode.MenuTitle, 21, 19, 17),
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);
            CreateMenuText(
                $"{key}_ModeSubtitle",
                hookLine,
                centerX,
                309f,
                13,
                new Color32(0xF2, 0xFA, 0xFA, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);
            CreateMenuText(
                $"{key}_ModeObjectiveOne",
                objectiveLineOne,
                centerX,
                346f,
                12,
                new Color32(0xD9, 0xEA, 0xEF, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);
            CreateMenuText(
                $"{key}_ModeObjectiveTwo",
                objectiveLineTwo,
                centerX,
                366f,
                12,
                new Color32(0xD9, 0xEA, 0xEF, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);

            menuButtons.Add(new rimrushMenuButton(buttonText, centerX, 397f, 194f, 44f, action, runtimeRoot));
        }

        /// <summary>
        /// Show the quick match setup screen where the player picks a character, ball skin, and difficulty.
        /// </summary>
        private void ShowSinglePlayerSetup()
        {
            currentScreen = rimrushBootstrapScreen.SinglePlayerSetup;
            pendingParticipantMode = rimrushParticipantMode.OnePlayer;
            rimrushInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("QUICK MATCH", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

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
                    quickBallSelection = rimrushBallCatalog.StepSelection(quickBallSelection, -1);
                    ShowSinglePlayerSetup();
                },
                () =>
                {
                    quickBallSelection = rimrushBallCatalog.StepSelection(quickBallSelection, 1);
                    ShowSinglePlayerSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new rimrushMenuButton(rimrushInventory.Instance.DifficultyLabel, 575f, 304f, 188f, 46f, () =>
            {
                rimrushInventory.Instance.ToggleDifficulty();
                ShowSinglePlayerSetup();
            }, runtimeRoot));
            if (rimrushInventory.Instance.Difficulty == rimrushAiDifficulty.Hell)
            {
                CreateHellDifficultyWarning(575f, 346f);
            }

            menuButtons.Add(new rimrushMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("PLAY", 660f, 452f, 150f, 42f, StartQuickMatchFlow, runtimeRoot));
        }

        /// <summary>
        /// Show the training mode setup screen where the player picks a character and ball skin.
        /// </summary>
        private void ShowTrainingSetup()
        {
            currentScreen = rimrushBootstrapScreen.TrainingSetup;
            pendingParticipantMode = rimrushParticipantMode.Training;
            rimrushInventory.Instance.SetParticipantMode(pendingParticipantMode);
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
                    trainingBallSelection = rimrushBallCatalog.StepSelection(trainingBallSelection, -1);
                    ShowTrainingSetup();
                },
                () =>
                {
                    trainingBallSelection = rimrushBallCatalog.StepSelection(trainingBallSelection, 1);
                    ShowTrainingSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new rimrushMenuButton("BACK", 488f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("PLAY", 660f, 452f, 150f, 42f, StartTrainingFlow, runtimeRoot));
        }

        /// <summary>
        /// Show the two-player setup screen where both players pick their characters and a shared ball skin.
        /// </summary>
        private void ShowTwoPlayerSetup()
        {
            currentScreen = rimrushBootstrapScreen.TwoPlayerSetup;
            pendingParticipantMode = rimrushParticipantMode.TwoPlayers;
            rimrushInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("2 PLAYERS MATCH", 58f, 30, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

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

            rimrushRender.Text(
                "VersusLabel",
                "VS",
                rimrushConstants.Width2,
                284f,
                34,
                new Color32(0xFF, 0xA3, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                rimrushFontKind.CfCrackBold,
                outlineColor: Color.white,
                outlinePixels: 1.4f);

            CreatePanel("VersusBallPanel", rimrushConstants.Width2, TwoPlayerBallPanelY, TwoPlayerBallPanelWidth, TwoPlayerBallPanelHeight, 8, new Color(0.05f, 0.08f, 0.1f, 0.82f));
            CreateBallSelector(
                "VersusBall",
                rimrushConstants.Width2,
                versusBallSelection,
                () =>
                {
                    versusBallSelection = rimrushBallCatalog.StepSelection(versusBallSelection, -1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusBallSelection = rimrushBallCatalog.StepSelection(versusBallSelection, 1);
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

            menuButtons.Add(new rimrushMenuButton("BACK", 212f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("PLAY", 588f, 452f, 150f, 42f, StartTwoPlayerMatch, runtimeRoot));
        }

        /// <summary>
        /// Show the tournament setup screen where the player picks a character before entering the bracket.
        /// </summary>
        private void ShowTournamentSetup()
        {
            currentScreen = rimrushBootstrapScreen.TournamentSetup;
            pendingParticipantMode = rimrushParticipantMode.OnePlayer;
            rimrushInventory.Instance.SetParticipantMode(pendingParticipantMode);
            rimrushInventory.Instance.SetTournamentSelection(tournamentCharacterId);

            BeginMenuScreen(false, false, "bg2blue0000");
            AddTitle(rimrushSinglePlayerNarrative.Tournament.MenuTitle, 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

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
                rimrushSinglePlayerNarrative.TournamentFormatLine,
                575f,
                186f,
                12,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);
            CreateBallSelector(
                "TournamentBall",
                575f,
                tournamentBallSelection,
                () =>
                {
                    tournamentBallSelection = rimrushBallCatalog.StepSelection(tournamentBallSelection, -1);
                    ShowTournamentSetup();
                },
                () =>
                {
                    tournamentBallSelection = rimrushBallCatalog.StepSelection(tournamentBallSelection, 1);
                    ShowTournamentSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new rimrushMenuButton(rimrushInventory.Instance.DifficultyLabel, 575f, 304f, 188f, 46f, () =>
            {
                rimrushInventory.Instance.ToggleDifficulty();
                ShowTournamentSetup();
            }, runtimeRoot));
            if (rimrushInventory.Instance.Difficulty == rimrushAiDifficulty.Hell)
            {
                CreateHellDifficultyWarning(575f, 346f);
            }

            var enoughCharacters = rimrushPlayersData.GetActiveCharacterIds().Length >= 8;
            if (!enoughCharacters)
            {
                AddSubtitle("NEED 8 ENABLED CHARACTERS", 408f, 18);
            }

            menuButtons.Add(new rimrushMenuButton(rimrushSinglePlayerNarrative.ComicReplayButton, 310f, 452f, 170f, 42f, ShowTournamentSetupComicReplay, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            if (enoughCharacters)
            {
                menuButtons.Add(new rimrushMenuButton("NEXT", 660f, 452f, 150f, 42f, StartTournamentFlow, runtimeRoot));
            }
        }

        /// <summary>
        /// Save the quick match choices and launch the match.
        /// </summary>
        private void StartQuickMatchFlow()
        {
            var inventory = rimrushInventory.Instance;
            inventory.SetParticipantMode(rimrushParticipantMode.OnePlayer);
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetQuickBallSelection(quickBallSelection);
            inventory.StartQuickGame();
            StartGameplay();
        }

        /// <summary>
        /// Save the training choices and launch the training session.
        /// </summary>
        private void StartTrainingFlow()
        {
            var inventory = rimrushInventory.Instance;
            inventory.SetParticipantMode(rimrushParticipantMode.Training);
            inventory.SetTrainingSelection(trainingCharacterId);
            inventory.SetTrainingBallSelection(trainingBallSelection);
            inventory.StartTraining();
            StartGameplay();
        }

        /// <summary>
        /// Save the training character selection and launch the tutorial.
        /// </summary>
        private void StartTutorialFlow()
        {
            var inventory = rimrushInventory.Instance;
            inventory.SetParticipantMode(rimrushParticipantMode.Tutorial);
            inventory.SetTrainingSelection(trainingCharacterId);
            inventory.SetTrainingBallSelection(trainingBallSelection);
            inventory.StartTutorial();
            StartGameplay();
        }

        private void StartTutorialFromHelpPanel()
        {
            if (rimrushInventory.Instance.IsTournamentActive)
            {
                rimrushInventory.Instance.AbandonTournament();
            }
            else if (rimrushInventory.Instance.IsAdventureActive)
            {
                rimrushInventory.Instance.AbandonAdventure();
            }

            StartTutorialFlow();
        }

        /// <summary>
        /// Save the tournament character selection and start the tournament bracket.
        /// </summary>
        private void StartTournamentFlow()
        {
            var inventory = rimrushInventory.Instance;
            inventory.SetParticipantMode(rimrushParticipantMode.OnePlayer);
            inventory.SetTournamentSelection(tournamentCharacterId);
            inventory.SetTournamentBallSelection(tournamentBallSelection);
            if (!inventory.BeginTournament())
            {
                ShowTournamentSetup();
                return;
            }

            ShowTournamentBracket();
        }

        /// <summary>
        /// Open the keyboard controls help panel.
        /// </summary>
        private void ShowMenuControlsPanel()
        {
            rimrushHelpPanel.ShowKeyboardPage();
        }

        /// <summary>
        /// Check if the player chose to replay the tutorial, start training, or start a quick match from the tutorial outro.
        /// </summary>
        private bool HandlePendingTutorialAction()
        {
            var inventory = rimrushInventory.Instance;
            var action = inventory.PendingTutorialNextAction;
            inventory.PendingTutorialNextAction = rimrushTutorialNextAction.None;
            switch (action)
            {
                case rimrushTutorialNextAction.ReplayTutorial:
                    StartTutorialFlow();
                    return true;
                case rimrushTutorialNextAction.StartTraining:
                    StartTrainingFlow();
                    return true;
                case rimrushTutorialNextAction.StartQuickMatch:
                    quickCharacterId = trainingCharacterId;
                    quickBallSelection = trainingBallSelection;
                    StartQuickMatchFlow();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Begin the tournament finals stage and show the updated bracket.
        /// </summary>
        private void StartTournamentFinalsFlow()
        {
            rimrushInventory.Instance.BeginTournamentFinals();
            ShowTournamentBracket();
        }

        /// <summary>
        /// Save the two-player character choices and launch the versus match.
        /// </summary>
        private void StartTwoPlayerMatch()
        {
            rimrushInventory.Instance.SetVersusBallSelection(versusBallSelection);
            rimrushInventory.Instance.StartTwoPlayerVersus(versusLeftCharacterId, versusRightCharacterId);
            StartGameplay();
        }

        /// <summary>
        /// Show the tournament bracket screen with all current matches and standings.
        /// </summary>
        private void ShowTournamentBracket()
        {
            var inventory = rimrushInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? rimrushBootstrapScreen.TournamentComplete : rimrushBootstrapScreen.TournamentBracket;
            var regularSeasonScreen = tournament.CurrentStage == rimrushTournamentStage.RegularSeason;
            var titleY = regularSeasonScreen ? 44f : 34f;
            var titleFontSize = regularSeasonScreen ? 32 : 30;
            var subtitleY = regularSeasonScreen ? 72f : 62f;
            var subtitleFontSize = regularSeasonScreen ? 16 : 14;
            var statusText = GetTournamentStatusText(tournament);

            var backgroundFrame = tournament.Completed || !regularSeasonScreen
                ? "bg10000"
                : "bg2blue0000";
            BeginMenuScreen(false, false, backgroundFrame);
            AddTitle(
                rimrushSinglePlayerNarrative.Tournament.MenuTitle,
                titleY,
                titleFontSize,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF));
            CreateMenuText(
                $"{statusText}_Subtitle",
                statusText,
                rimrushConstants.Width2,
                subtitleY,
                subtitleFontSize,
                regularSeasonScreen ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : Color.white,
                TextAnchor.MiddleCenter,
                19,
                rimrushTextStyle.Subtitle);
            CreateTournamentBracketBoard(tournament);
            CreateTournamentSeasonBanner(tournament);

            if (tournament.Completed)
            {
                menuButtons.Add(new rimrushMenuButton("MAIN MENU", 156f, 452f, 164f, 42f, () =>
                {
                    rimrushInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton(rimrushSinglePlayerNarrative.ComicReplayButton, 400f, 452f, 176f, 42f, ShowTournamentBracketComicReplay, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton("CEREMONY", 640f, 452f, 190f, 42f, ShowTournamentAwards, runtimeRoot));
            }
            else if (tournament.CurrentStage == rimrushTournamentStage.RegularSeason)
            {
                menuButtons.Add(new rimrushMenuButton("BACK", 142f, 452f, 138f, 42f, () =>
                {
                    rimrushInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));

                if (tournament.RegularSeasonCompleted)
                {
                    menuButtons.Add(new rimrushMenuButton("START FINALS", 634f, 452f, 190f, 42f, StartTournamentFinalsFlow, runtimeRoot));
                }
                else
                {
                    menuButtons.Add(new rimrushMenuButton("PLAY", 634f, 452f, 190f, 42f, StartGameplay, runtimeRoot));
                }
            }
            else
            {
                menuButtons.Add(new rimrushMenuButton("BACK", 142f, 452f, 138f, 42f, () =>
                {
                    rimrushInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton(rimrushSinglePlayerNarrative.ComicReplayButton, 400f, 452f, 176f, 42f, ShowTournamentBracketComicReplay, runtimeRoot));
                menuButtons.Add(new rimrushMenuButton("PLAY", 640f, 452f, 150f, 42f, StartGameplay, runtimeRoot));
            }
        }

        /// <summary>
        /// Show the end-of-season awards ceremony with placement ribbons and trophies.
        /// </summary>
        private void ShowTournamentAwards()
        {
            var tournament = rimrushInventory.Instance.Tournament;
            if (!tournament.Completed)
            {
                ShowTournamentBracket();
                return;
            }

            currentScreen = rimrushBootstrapScreen.TournamentAwards;
            BeginMenuScreen(false, false, "bg10000");
            AddTitle(rimrushSinglePlayerNarrative.TournamentSeasonCompleteTitle, 52f, 28, GetTournamentAwardsAccentColor(tournament.PlayerPlacement));
            CreateTournamentAwardsScene(tournament);

            menuButtons.Add(new rimrushMenuButton("BRACKET", 220f, 452f, 180f, 42f, ShowTournamentBracket, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton("MAIN MENU", 580f, 452f, 200f, 42f, () =>
            {
                rimrushInventory.Instance.AbandonTournament();
                ShowPlayerCountMenu();
            }, runtimeRoot));
        }

        /// <summary>
        /// Clear the menu scene and create a new match game object.
        /// </summary>
        private void StartGameplay()
        {
            ClearRuntime();
            EnableGameplayPresentation();
            runtimeRoot = new GameObject("rimrushRuntime").transform;
            rimrushAudio.Create(transform).PlayMusic(rimrushAssets.Sounds.MenuMusic);
            menuButtons.Clear();
            gameCore = new rimrushGameBuilder().Build(runtimeRoot);
        }

        /// <summary>
        /// Set up a fresh menu screen with background, optional logo, and controls hint.
        /// </summary>
        private void BeginMenuScreen(bool showLogo, bool showControls, string backgroundFrame)
        {
            ClearRuntime();
            EnableNativeMenuPresentation();
            runtimeRoot = new GameObject("rimrushRuntime").transform;
            nativeMenuTextLayer = new rimrushNativeMenuTextLayer(runtimeRoot);
            nativeMenuTextLayer.RefreshLayout(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            rimrushAudio.Create(transform).PlayMusic(rimrushAssets.Sounds.MenuMusic);

            if (!TryCreateStandaloneMenuBackground(backgroundFrame))
            {
                rimrushRender.Sprite(
                    "MenuBackground",
                    rimrushAtlasCache.Instance.Interface,
                    backgroundFrame,
                    rimrushConstants.Width2,
                    240f,
                    0.5f,
                    0.5f,
                    0,
                    runtimeRoot);
            }

            if (showLogo)
            {
                var logoTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameLogo));
                if (logoTexture != null)
                {
                    var logoScale = Mathf.Min(
                        MenuLogoMaxWidth / Mathf.Max(1f, logoTexture.width),
                        MenuLogoMaxHeight / Mathf.Max(1f, logoTexture.height));
                    var logo = rimrushRender.Image("Logo", logoTexture, rimrushConstants.Width2, MenuLogoCenterY, 0.5f, 0.5f, 20, runtimeRoot);
                    var logoWorldScale = rimrushConstants.UnitsPerPixel * logoScale;
                    logo.transform.localScale = new Vector3(logoWorldScale, logoWorldScale, 1f);
                }
            }

            menuMusicButton = new rimrushIconButton(
                "MenuMusicButton",
                MenuMusicButtonX,
                MenuTopButtonY,
                MenuTopButtonSize,
                MenuTopButtonSize,
                ToggleBackgroundMusic,
                runtimeRoot,
                32,
                MenuTopIconPixels,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn),
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff));
            menuMusicButton.SetActiveIconIndex(GetMusicIconIndex());
            menuHelpButton = new rimrushIconButton(
                "MenuHelpButton",
                MenuHelpButtonX,
                MenuTopButtonY,
                MenuTopButtonSize,
                MenuTopButtonSize,
                ShowMenuControlsPanel,
                runtimeRoot,
                32,
                MenuTopIconPixels,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton));

            if (showControls)
            {
                CreateMenuText(
                    "Controls",
                    rimrushControlsData.MainMenuControlsText,
                    rimrushConstants.Width2,
                    492f,
                    11,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    30,
                    rimrushTextStyle.TournamentBody);
            }

            menuButtons.Clear();
        }

        /// <summary>
        /// Switch the camera to pixel-perfect gameplay mode.
        /// </summary>
        private void EnableGameplayPresentation()
        {
            usingNativeUiPresentation = false;
            viewportScreenWidth = -1;
            viewportScreenHeight = -1;
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            fixedResolutionPresenter?.Attach(mainCamera);
        }

        /// <summary>
        /// Switch the camera to native-resolution menu mode so text stays sharp.
        /// </summary>
        private void EnableNativeMenuPresentation()
        {
            usingNativeUiPresentation = true;
            fixedResolutionPresenter?.Detach();
            RefreshNativeMenuViewport(force: true);
        }

        /// <summary>
        /// Recalculate the camera viewport when the window is resized in native menu mode.
        /// </summary>
        private void RefreshNativeMenuViewport(bool force = false)
        {
            if (!usingNativeUiPresentation || mainCamera == null)
            {
                return;
            }

            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            if (!force && screenWidth == viewportScreenWidth && screenHeight == viewportScreenHeight)
            {
                return;
            }

            viewportScreenWidth = screenWidth;
            viewportScreenHeight = screenHeight;
            nativeMenuTextLayer?.RefreshLayout(screenWidth, screenHeight);

            var screenAspect = screenWidth / (float)screenHeight;
            if (Mathf.Abs(screenAspect - NativeUiAspect) <= 0.0001f)
            {
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (screenAspect > NativeUiAspect)
            {
                var normalizedWidth = NativeUiAspect / screenAspect;
                mainCamera.rect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
                return;
            }

            var normalizedHeight = screenAspect / NativeUiAspect;
            mainCamera.rect = new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
        }

        /// <summary>
        /// Place a large title label at the top of a menu screen.
        /// </summary>
        private void AddTitle(string title, float y, int fontSize, Color color)
        {
            CreateMenuText(
                $"{title}_Title",
                title,
                rimrushConstants.Width2,
                y,
                fontSize,
                color,
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.DisplayTitle);
        }

        /// <summary>
        /// Place a smaller subtitle label below the title on a menu screen.
        /// </summary>
        private void AddSubtitle(string subtitle, float y, int fontSize = 18)
        {
            CreateMenuText(
                $"{subtitle}_Subtitle",
                subtitle,
                rimrushConstants.Width2,
                y,
                fontSize,
                Color.white,
                TextAnchor.MiddleCenter,
                19,
                rimrushTextStyle.Subtitle);
        }

        /// <summary>
        /// Create a text label on a menu screen using the native text layer when available.
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
            rimrushTextStyle style,
            Transform parent = null)
        {
            var resolvedParent = parent ?? runtimeRoot;
            if (ShouldUseNativeMenuText(resolvedParent))
            {
                nativeMenuTextLayer?.CreateText(name, text, x, y, fontSize, color, anchor, style);
                return;
            }

            rimrushRender.Text(name, text, x, y, fontSize, color, anchor, sortingOrder, resolvedParent, style);
        }

        private GameObject CreateToggleableStoryIntroText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            int sortingOrder,
            rimrushTextStyle style)
        {
            GameObject textObject = null;
            if (ShouldUseNativeMenuText(runtimeRoot))
            {
                var nativeText = nativeMenuTextLayer?.CreateText(name, text, x, y, fontSize, color, anchor, style);
                textObject = nativeText != null ? nativeText.gameObject : null;
            }
            else
            {
                var legacyText = rimrushRender.Text(name, text, x, y, fontSize, color, anchor, sortingOrder, runtimeRoot, style);
                textObject = legacyText != null ? legacyText.gameObject : null;
            }

            if (textObject != null)
            {
                storyIntroLoreTextObjects.Add(textObject);
                textObject.SetActive(storyIntroLoreOpen);
            }

            return textObject;
        }

        private GameObject CreateStoryIntroLoreButtonLabel(string text, float x, float y)
        {
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
                    rimrushTextStyle.TournamentAccent);
                return nativeText != null ? nativeText.gameObject : null;
            }

            var legacyText = rimrushRender.Text(
                "StoryIntroLoreButtonLabel",
                text,
                x,
                y,
                18,
                StoryIntroLoreClosedLabelColor,
                TextAnchor.MiddleCenter,
                56,
                runtimeRoot,
                rimrushTextStyle.TournamentAccent);
            return legacyText != null ? legacyText.gameObject : null;
        }

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

        private void RefreshStoryIntroLoreButtonLayout(bool isVisible)
        {
            var buttonX = isVisible ? StoryIntroLoreOpenButtonX : StoryIntroLoreButtonX;
            var buttonY = isVisible ? StoryIntroLoreOpenButtonY : StoryIntroLoreButtonY;
            var labelOffsetX = isVisible ? StoryIntroLoreOpenLabelOffsetX : StoryIntroLoreLabelOffsetX;
            var labelOffsetY = isVisible ? StoryIntroLoreOpenLabelOffsetY : StoryIntroLoreLabelOffsetY;
            storyIntroLoreButton?.SetPosition(buttonX, buttonY);
            SetStoryIntroLoreLabelPosition(
                buttonX + labelOffsetX,
                buttonY + labelOffsetY);
            SetStoryIntroLoreArtOffset(
                buttonX - StoryIntroLoreButtonX,
                buttonY - StoryIntroLoreButtonY);
        }

        private void SetStoryIntroLoreLabelPosition(float x, float y)
        {
            if (storyIntroLoreLabelObject == null)
            {
                return;
            }

            var nativeText = storyIntroLoreLabelObject.GetComponent<TMPro.TMP_Text>();
            if (nativeText != null)
            {
                rimrushNativeMenuTextLayer.SetPixelPosition(nativeText.rectTransform, x, y);
                return;
            }

            storyIntroLoreLabelObject.transform.position = rimrushConstants.PixelToWorldSnapped(
                x,
                y,
                storyIntroLoreLabelObject.transform.position.z);
        }

        private void SetStoryIntroLoreArtOffset(float pixelOffsetX, float pixelOffsetY)
        {
            if (storyIntroLoreArtRoot == null)
            {
                return;
            }

            var origin = rimrushConstants.PixelToWorldSnapped(0f, 0f);
            var offset = rimrushConstants.PixelToWorldSnapped(pixelOffsetX, pixelOffsetY);
            storyIntroLoreArtRoot.transform.localPosition = new Vector3(
                offset.x - origin.x,
                offset.y - origin.y,
                0f);
        }

        /// <summary>
        /// Return true if the native text layer is active and can render text.
        /// </summary>
        private bool ShouldUseNativeMenuText(Transform parent)
        {
            return nativeMenuTextLayer != null && parent != null && nativeMenuTextLayer.Owns(parent);
        }

        /// <summary>
        /// Draw a semi-transparent dark panel rectangle on a menu screen.
        /// </summary>
        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            return CreatePanel(name, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        /// <summary>
        /// Draw a semi-transparent dark panel rectangle on a menu screen.
        /// </summary>
        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            var standalonePanel = TryCreateStandaloneTintPanel(name, x, y, width, height, sortingOrder, tint, parent);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            var panel = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / LegacyTintPanelSourcePixels,
                rimrushConstants.UnitsPerPixel * height / LegacyTintPanelSourcePixels,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private bool TryCreateStandaloneMenuBackground(string backgroundFrame)
        {
            var imageKey = ResolveStandaloneMenuBackgroundImage(backgroundFrame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return false;
            }

            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return false;
            }

            var background = rimrushRender.Image(
                "MenuBackground",
                texture,
                rimrushConstants.Width2,
                240f,
                0.5f,
                0.5f,
                0,
                runtimeRoot);
            background.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * LegacyMenuBackgroundWidth / Mathf.Max(1f, texture.width),
                rimrushConstants.UnitsPerPixel * LegacyMenuBackgroundHeight / Mathf.Max(1f, texture.height),
                1f);
            return true;
        }

        private static string ResolveStandaloneMenuBackgroundImage(string backgroundFrame)
        {
            return backgroundFrame switch
            {
                "bg10000" => rimrushAssets.Images.MenuBackgroundHalloweenSpotlight,
                "bg2blue0000" => rimrushAssets.Images.MenuBackgroundMoonlitGym,
                _ => null
            };
        }

        private static GameObject TryCreateStandaloneTintPanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.PanelFillSoft));
            if (texture == null)
            {
                return null;
            }

            var panel = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                rimrushConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        /// <summary>
        /// Build the ball skin and difficulty selector panel used on setup screens.
        /// </summary>
        private void CreateOptionsPanel(string prefix, float centerX)
        {
            CreateMenuText(
                $"{prefix}_OptionTitle",
                "SETTINGS",
                centerX,
                160f,
                20,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Show left/right arrows and a ball preview so the player can pick a ball skin.
        /// </summary>
        private void CreateBallSelector(
            string key,
            float centerX,
            rimrushBallSelection selection,
            System.Action previousBallAction,
            System.Action nextBallAction,
            float headerY,
            float previewY,
            float labelY)
        {
            CreateMenuText(
                $"{key}_Header",
                "BALL",
                centerX,
                headerY,
                16,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);

            menuButtons.Add(new rimrushMenuButton("<", centerX - BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, previousBallAction, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton(">", centerX + BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, nextBallAction, runtimeRoot));

            CreateBallPreview(
                $"{key}_Preview",
                rimrushBallCatalog.PreviewTheme(selection),
                centerX,
                previewY + 1f,
                BallPreviewPixels,
                19);

            CreateMenuText(
                $"{key}_Label",
                rimrushBallCatalog.Label(selection),
                centerX,
                labelY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentBody);
        }

        /// <summary>
        /// Display a warning message when the hardest difficulty is selected.
        /// </summary>
        private void CreateHellDifficultyWarning(float centerX, float y)
        {
            CreateMenuText(
                "HellDifficultyWarning",
                "UNFAIR CHALLENGE: CPU USES BONUS SUPERS",
                centerX,
                y,
                12,
                new Color32(0xFF, 0x9C, 0x32, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Show a character portrait with left/right arrows and name label for picking a fighter.
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
            CreateMenuText(
                $"{key}_Header",
                header,
                centerX,
                SelectorHeaderY,
                header.Length <= 2 ? 26 : 18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                rimrushTextStyle.TournamentAccent);

            menuButtons.Add(new rimrushMenuButton("<", centerX - SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, previousCharacterAction, runtimeRoot));
            menuButtons.Add(new rimrushMenuButton(">", centerX + SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, nextCharacterAction, runtimeRoot));

            CreatePreviewPlayer(key, characterId, centerX, previewY, previewScale);
            var skillDefinition = rimrushCharacterSkillsData.Get(characterId);
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
                rimrushTextStyle.TournamentAccent);
            CreateMenuText(
                $"{key}_CharacterName",
                rimrushPlayersData.GetCharacterName(characterId),
                centerX,
                nameY,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                rimrushTextStyle.TournamentBody);
        }

        private void CreateCharacterSkillIcon(string name, rimrushCharacterSkillDefinition skillDefinition, float x, float y, int sortingOrder)
        {
            const float orbPixels = 52f;
            const float iconPixels = 42f;
            var orbTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.EmblemOrb));
            if (orbTexture != null)
            {
                var orb = rimrushRender.Image($"{name}_Orb", orbTexture, x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
                orb.transform.localScale = new Vector3(
                    rimrushConstants.UnitsPerPixel * orbPixels / Mathf.Max(1f, orbTexture.width),
                    rimrushConstants.UnitsPerPixel * orbPixels / Mathf.Max(1f, orbTexture.height),
                    1f);
                var orbRenderer = orb.GetComponent<SpriteRenderer>();
                if (orbRenderer != null)
                {
                    orbRenderer.color = new Color(1f, 1f, 1f, 0.92f);
                }
            }
            else
            {
                var fallbackOrb = rimrushRender.Sprite($"{name}_OrbFallback", rimrushAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
                fallbackOrb.transform.localScale *= orbPixels / 150f;
            }

            if (!skillDefinition.HasStandaloneIconArt)
            {
                return;
            }

            var iconPath = rimrushAssets.Images.ResourcePath(skillDefinition.IconImageKey);
            rimrushIconButton.CreateImageIcon(name, iconPath, x, y, sortingOrder + 1, iconPixels, runtimeRoot);
        }

        /// <summary>
        /// Render a small preview of the selected ball skin.
        /// </summary>
        private void CreateBallPreview(string name, rimrushBallTheme theme, float x, float y, float targetPixels, int sortingOrder)
        {
            var sprite = rimrushGameplaySpriteLoader.LoadBallThemeSprite(theme, 0.5f, 0.5f) ??
                         rimrushAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);

            if (sprite == null)
            {
                return;
            }

            var preview = new GameObject(name);
            preview.transform.SetParent(runtimeRoot, false);
            var renderer = preview.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetPixels / Mathf.Max(1f, spritePixels);
            rimrushRender.ApplyPixelTransform(preview.transform, x, y, 0f, scale);
        }

        /// <summary>
        /// Show a live animated character model on a setup screen.
        /// </summary>
        private void CreatePreviewPlayer(string key, int characterId, float x, float y, float scale)
        {
            var previewScale = scale * PreviewScaleFactor * rimrushPlayersData.GetCharacterPreviewScaleMultiplier(characterId);
            var shadow = rimrushRender.Sprite($"{key}_PreviewShadow", rimrushAtlasCache.Instance.Interface, "loginSelect0000", x, y + PreviewShadowYOffset, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= PreviewShadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.55f);

            var previewRoot = new GameObject($"{key}_Preview");
            previewRoot.transform.SetParent(runtimeRoot, false);
            rimrushRender.ApplyPixelTransform(previewRoot.transform, x, y, 0f, previewScale);

            var armature = rimrushPlayersData.BuildGameplayArmature($"{key}_PreviewArmature");
            if (armature == null)
            {
                return;
            }

            armature.transform.SetParent(previewRoot.transform, false);
            armature.transform.localPosition = new Vector3(0f, PreviewArmatureYOffset + rimrushPlayersData.GetCharacterPreviewOffsetY(characterId), 0f);
            armature.transform.localScale = new Vector3(PreviewArmatureScale, PreviewArmatureScale, 1f);
            rimrushPlayersData.ApplyCharacter(armature, characterId);
        }

        /// <summary>
        /// Draw the full tournament bracket with season banners and playoff rounds.
        /// </summary>
        private void CreateTournamentBracketBoard(rimrushTournamentData tournament)
        {
            if (tournament.CurrentStage == rimrushTournamentStage.RegularSeason)
            {
                CreateTournamentRegularSeasonBoard(tournament);
            }
            else
            {
                CreateTournamentPlayoffBoard(tournament);
            }

            CreateTournamentSummaryPanel(tournament);
        }

        private void CreateTournamentSeasonBanner(rimrushTournamentData tournament)
        {
            var title = rimrushSinglePlayerNarrative.GetTournamentStageTitle(tournament);
            if (title == "DIVISIONS")
            {
                return;
            }

            CreatePanel(
                "TournamentSeasonBannerPanel",
                rimrushConstants.Width2,
                98f,
                204f,
                24f,
                18,
                new Color(0.03f, 0.06f, 0.1f, 0.74f));
            CreateMenuText(
                "TournamentSeasonBannerTitle",
                title,
                rimrushConstants.Width2,
                98f,
                11,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                19,
                rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Draw the regular season standings table with win/loss records.
        /// </summary>
        private void CreateTournamentRegularSeasonBoard(rimrushTournamentData tournament)
        {
            CreatePanel(
                "RegularSeasonBackdrop",
                rimrushConstants.Width2,
                236f,
                744f,
                302f,
                8,
                new Color(0.01f, 0.04f, 0.08f, 0.28f));

            CreateTournamentDivisionBoard("DivisionA", 222f, 242f, "DIV. A", tournament, 0);
            CreateTournamentDivisionBoard("DivisionB", 578f, 242f, "DIV. B", tournament, 1);
        }

        /// <summary>
        /// Draw one division column in the season standings with team entries.
        /// </summary>
        private void CreateTournamentDivisionBoard(string key, float x, float y, string title, rimrushTournamentData tournament, int divisionIndex)
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
        /// Add a text label inside a standings row cell.
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
                CreateMenuText(name, text, x, y, fontSize, color, anchor, sortingOrder, rimrushTextStyle.TournamentBody);
                return null;
            }

            return rimrushRender.Text(
                name,
                text,
                x,
                y,
                fontSize,
                color,
                anchor,
                sortingOrder,
                runtimeRoot,
                rimrushFontKind.RajdhaniSemiBold,
                outlineColor: new Color(0.02f, 0.03f, 0.08f, 0.9f),
                outlinePixels: fontSize >= 13 ? 0.7f : 0.58f);
        }

        /// <summary>
        /// Add a highlighted text label inside a standings row cell.
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
                CreateMenuText(name, text, x, y, fontSize, color, anchor, sortingOrder, rimrushTextStyle.TournamentAccent);
                return null;
            }

            return rimrushRender.Text(
                name,
                text,
                x,
                y,
                fontSize,
                color,
                anchor,
                sortingOrder,
                runtimeRoot,
                rimrushFontKind.RajdhaniBold,
                outlineColor: new Color(0.02f, 0.03f, 0.08f, 0.92f),
                outlinePixels: fontSize >= 14 ? 0.8f : 0.62f);
        }

        /// <summary>
        /// Draw the playoff bracket with semifinal and final match panels.
        /// </summary>
        private void CreateTournamentPlayoffBoard(rimrushTournamentData tournament)
        {
            const float playoffBackdropY = 232f;
            const float playoffBackdropHeight = 292f;
            const float finalPanelY = 168f;
            const float semiPanelY = 238f;
            const float semiPanelOffsetX = 200f;
            const float placementPanelY = 324f;

            CreatePanel(
                "PlayoffBackdrop",
                rimrushConstants.Width2,
                playoffBackdropY,
                742f,
                playoffBackdropHeight,
                8,
                new Color(0.02f, 0.05f, 0.08f, 0.28f));

            var semiCurrent = !tournament.Completed && tournament.CurrentStage == rimrushTournamentStage.SemiFinal;
            CreateTournamentPlayoffMatchPanel(
                "SemiFinalLeft",
                rimrushConstants.Width2 - semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[0],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[0], tournament.PlayerCharacterId));
            CreateTournamentPlayoffMatchPanel(
                "SemiFinalRight",
                rimrushConstants.Width2 + semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[1],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[1], tournament.PlayerCharacterId));
            CreateTournamentPlayoffMatchPanel(
                "FinalMatch",
                rimrushConstants.Width2,
                finalPanelY,
                "FINAL",
                tournament.FinalResult,
                !tournament.Completed && tournament.CurrentStage == rimrushTournamentStage.Final);
            CreateTournamentPlayoffMatchPanel(
                "ThirdPlaceMatch",
                rimrushConstants.Width2,
                placementPanelY,
                "3RD PLACE MATCH",
                tournament.ThirdPlaceResult,
                !tournament.Completed && tournament.CurrentStage == rimrushTournamentStage.ThirdPlace);
        }

        /// <summary>
        /// Draw a single playoff match card showing two player slots.
        /// </summary>
        private void CreateTournamentPlayoffMatchPanel(string key, float x, float y, string title, rimrushTournamentMatchResult match, bool current)
        {
            const float panelWidth = 180f;
            const float panelHeight = 96f;
            const float badgeOffsetX = 58f;
            const float nameXOffset = 24f;
            const float scoreXOffset = 72f;
            const float rowOffset = 18f;
            const float titleOffsetY = 58f;

            var tint = current
                ? new Color(1f, 0.78f, 0.52f, 1f)
                : match.Completed
                    ? new Color(0.8f, 0.98f, 0.9f, 1f)
                    : new Color(0.76f, 0.82f, 0.9f, 0.9f);
            var frame = current ? "MatchBack0002" : "MatchBack0001";

            CreateFramedPanel($"{key}_Frame", frame, x, y, panelWidth, panelHeight, 13, tint);
            CreatePanel(
                $"{key}_Shade",
                x,
                y,
                panelWidth - 20f,
                panelHeight - 16f,
                14,
                new Color(0.03f, 0.05f, 0.08f, 0.3f));

            CreateMenuText(
                $"{key}_Title",
                title,
                x,
                y - titleOffsetY,
                title.Length > 10 ? 13 : 15,
                current ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                15,
                rimrushTextStyle.TournamentAccent);

            CreatePanel($"{key}_DividerHorizontal", x, y, panelWidth - 28f, 2f, 15, new Color(0.3f, 0.86f, 0.9f, 0.46f));

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
                    rimrushTextStyle.TournamentAccent);
            }
        }

        /// <summary>
        /// Draw one player slot inside a playoff match card with portrait and name.
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
            var glowColor = winner
                ? new Color(1f, 0.74f, 0.28f, 0.36f)
                : current
                    ? new Color(0.3f, 0.96f, 1f, 0.32f)
                    : new Color(0.24f, 0.94f, 0.78f, 0.24f);
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
        /// Draw the results summary at the bottom of the bracket with player status.
        /// </summary>
        private void CreateTournamentSummaryPanel(rimrushTournamentData tournament)
        {
            var summaryCompleted = tournament.Completed;
            var summaryWidth = summaryCompleted ? 312f : 328f;
            var summaryHeight = summaryCompleted ? 72f : 40f;
            var summaryY = summaryCompleted ? TournamentSummaryY : TournamentSummaryY + 6f;
            CreateFramedPanel(
                "TournamentSummaryPanel",
                "btn_bg0000",
                rimrushConstants.Width2,
                summaryY,
                summaryWidth,
                summaryHeight,
                15,
                summaryCompleted
                    ? new Color(0.2f, 0.78f, 0.88f, 0.96f)
                    : new Color(0.22f, 0.86f, 0.94f, 0.94f));

            if (summaryCompleted)
            {
                CreateTournamentMiniBadge("ChampionBadge", tournament.ChampionCharacterId, 280f, summaryY, 16);
                CreateMenuText(
                    "ChampionLabel",
                    "CHAMPION",
                    312f,
                    summaryY - 12f,
                    12,
                    new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                    TextAnchor.MiddleLeft,
                    17,
                    rimrushTextStyle.TournamentAccent);
                CreateMenuText(
                    "ChampionName",
                    CharacterNameOrTbd(tournament.ChampionCharacterId),
                    312f,
                    summaryY + 2f,
                    GetCompactFontSize(CharacterNameOrTbd(tournament.ChampionCharacterId), 15, 13, 11),
                    Color.white,
                    TextAnchor.MiddleLeft,
                    17,
                    rimrushTextStyle.TournamentBody);
                CreateMenuText(
                    "PlacementLabel",
                    $"YOU FINISHED #{tournament.PlayerPlacement}",
                    312f,
                    summaryY + 18f,
                    11,
                    new Color32(0xFF, 0xD6, 0x6D, 0xFF),
                    TextAnchor.MiddleLeft,
                    17,
                    rimrushTextStyle.TournamentAccent);
                return;
            }

            string headline;
            string detail;
            if (tournament.CurrentStage == rimrushTournamentStage.RegularSeason)
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
            else if (tournament.CurrentStage == rimrushTournamentStage.SemiFinal)
            {
                headline = "UP NEXT";
                detail = GetMatchupText(GetPlayerPlayoffMatch(tournament), "SEMIFINAL SET");
            }
            else if (tournament.CurrentStage == rimrushTournamentStage.ThirdPlace)
            {
                headline = "UP NEXT";
                detail = GetMatchupText(tournament.ThirdPlaceResult, "3RD PLACE MATCH");
            }
            else
            {
                headline = "UP NEXT";
                detail = GetMatchupText(tournament.FinalResult, "FINAL");
            }

            CreateStandingsBodyText(
                "TournamentSummaryDetail",
                $"{headline} - {detail}",
                rimrushConstants.Width2,
                summaryY + 2f,
                GetCompactFontSize($"{headline} - {detail}", 12, 11, 10),
                Color.white,
                TextAnchor.MiddleCenter,
                17);
        }

        /// <summary>
        /// Draw a small labeled badge (e.g. WIN, LOSS, SEED) on the bracket.
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
            if (parent == null)
            {
                parent = runtimeRoot;
            }

            const float legacyBadgePixels = 150f;
            var badgePixels = Mathf.Max(
                portraitPixels + 10f,
                Mathf.Max(legacyBadgePixels * badgeScale, legacyBadgePixels * glowScale * 0.82f));
            var ringColor = new Color(
                Mathf.Clamp01(0.48f + glowColor.r * 0.55f),
                Mathf.Clamp01(0.48f + glowColor.g * 0.55f),
                Mathf.Clamp01(0.48f + glowColor.b * 0.55f),
                0.96f);
            rimrushRender.PortraitBackplate(
                $"{key}_Badge",
                x,
                y,
                badgePixels,
                sortingOrder,
                parent,
                glowColor,
                new Color(0.01f, 0.025f, 0.06f, characterId >= 0 ? 0.94f : 0.62f),
                ringColor);

            if (characterId >= 0)
            {
                CreateTournamentPortrait($"{key}_Portrait", characterId, x, y + 1f, portraitPixels, sortingOrder + 3, parent);
            }
        }

        /// <summary>
        /// Convert a win count and total games into a percentage string like "75.0%".
        /// </summary>
        private static string FormatWinningPercentage(float value)
        {
            return value.ToString("0.000");
        }

        /// <summary>
        /// Return true if the given match has the player character in either slot.
        /// </summary>
        private static bool MatchIncludesPlayer(rimrushTournamentMatchResult match, int playerCharacterId)
        {
            return match != null && (match.LeftCharacterId == playerCharacterId || match.RightCharacterId == playerCharacterId);
        }

        /// <summary>
        /// Find which playoff match contains the player.
        /// </summary>
        private static rimrushTournamentMatchResult GetPlayerPlayoffMatch(rimrushTournamentData tournament)
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
        /// Return a short label like "P1 vs P3" for a bracket match.
        /// </summary>
        private static string GetMatchupText(rimrushTournamentMatchResult match, string fallback)
        {
            if (match == null || match.LeftCharacterId < 0 || match.RightCharacterId < 0)
            {
                return fallback;
            }

            return $"{CharacterNameOrTbd(match.LeftCharacterId)} VS {CharacterNameOrTbd(match.RightCharacterId)}";
        }

        /// <summary>
        /// Build the full awards ceremony screen with podiums, trophies, and banners.
        /// </summary>
        private void CreateTournamentAwardsScene(rimrushTournamentData tournament)
        {
            ResetTournamentAwardsState();

            var placements = BuildTournamentAwardsPlacements(tournament);
            var accentColor = GetTournamentAwardsAccentColor(tournament.PlayerPlacement);

            var showcaseGroup = CreateTournamentAwardsGroup("AwardsShowcase");
            var showcase = CreateStandaloneImage(
                "AwardsShowcasePanel",
                rimrushAssets.Images.Ui.AwardsShowcasePanel,
                rimrushConstants.Width2,
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
                    rimrushConstants.Width2,
                    TournamentAwardsShowcaseY,
                    TournamentAwardsShowcaseWidth,
                    TournamentAwardsShowcaseHeight,
                    7,
                    new Color(1f, 1f, 1f, 0.74f),
                    showcaseGroup.transform);
            }

            RegisterTournamentAwardsAnimation(showcaseGroup.transform, new Vector2(0f, 10f), 0.04f, 0.42f, 0.96f);

            var bannerGroup = CreateTournamentAwardsGroup("AwardsResultBanner");
            var plaque = CreateStandaloneImage(
                "AwardsResultPlaque",
                rimrushAssets.Images.Ui.AwardsResultPlaque,
                rimrushConstants.Width2,
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
                    rimrushConstants.Width2,
                    TournamentAwardsPlaqueY,
                    312f,
                    66f,
                    12,
                    new Color(0.12f, 0.22f, 0.32f, 0.92f),
                    bannerGroup.transform);
            }

            CreateMenuText(
                "AwardsResultBannerLabel",
                GetTournamentAwardsPlayerMessage(tournament),
                rimrushConstants.Width2,
                TournamentAwardsPlaqueY,
                18,
                accentColor,
                TextAnchor.MiddleCenter,
                13,
                rimrushTextStyle.TournamentAccent,
                bannerGroup.transform);
            CreateMenuText(
                "AwardsResultEndingLine",
                rimrushSinglePlayerNarrative.GetTournamentPlacementEnding(tournament.PlayerPlacement),
                rimrushConstants.Width2,
                TournamentAwardsPlaqueY + 42f,
                10,
                new Color32(0xE0, 0xEC, 0xF4, 0xFF),
                TextAnchor.MiddleCenter,
                13,
                rimrushTextStyle.TournamentBody,
                bannerGroup.transform);
            RegisterTournamentAwardsAnimation(bannerGroup.transform, new Vector2(0f, 14f), 0.1f, 0.42f, 0.95f);

            var podiumGroup = CreateTournamentAwardsGroup("AwardsPodium");
            var podiumBase = CreateStandaloneImage(
                "AwardsPodiumBase",
                rimrushAssets.Images.Ui.AwardsPodiumBase,
                rimrushConstants.Width2,
                TournamentAwardsPodiumY,
                TournamentAwardsPodiumWidth,
                TournamentAwardsPodiumHeight,
                11,
                podiumGroup.transform);
            if (podiumBase == null)
            {
                var tribune = rimrushRender.Sprite(
                    "AwardsTribuneFallback",
                    rimrushAtlasCache.Instance.Interface,
                    "TribuneFinal0000",
                    rimrushConstants.Width2,
                    TournamentAwardsPodiumY + 4f,
                    0.5f,
                    0.5f,
                    11,
                    podiumGroup.transform);
                tribune.transform.localScale *= 0.98f;
                tribune.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.98f);
            }

            CreateTournamentAwardsLaneLabel("AwardsLaneSecond", placements[1], TournamentAwardsLeftX, 384f, false, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneChampion", placements[0], TournamentAwardsChampionX, 368f, true, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneThird", placements[2], TournamentAwardsRightX, 390f, false, podiumGroup.transform);
            RegisterTournamentAwardsAnimation(podiumGroup.transform, new Vector2(0f, 18f), 0.14f, 0.52f, 0.97f);

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
        /// Draw a group of awards items at a given position with a label.
        /// </summary>
        private GameObject CreateTournamentAwardsGroup(string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(runtimeRoot, false);
            return group;
        }

        /// <summary>
        /// Draw a small badge used in the awards ceremony.
        /// </summary>
        private void CreateTournamentAwardsBadge(string key, TournamentAwardsPlacement placement, float x, float y, bool champion, Transform parent)
        {
            rimrushRender.PortraitBackplate(
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

            rimrushRender.Text(
                $"{key}_Rank",
                GetPlacementShortLabel(placement.Placement),
                x,
                y - (champion ? 44f : 40f),
                champion ? 16 : 14,
                placement.AccentColor,
                TextAnchor.MiddleCenter,
                18,
                parent,
                rimrushTextStyle.TournamentAccent);
        }

        /// <summary>
        /// Draw a lane label in the awards ceremony.
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
            rimrushRender.Text(
                $"{key}_Name",
                name,
                x,
                y + 1f,
                GetCompactFontSize(name, champion ? 14 : 13, 12, 11),
                Color.white,
                TextAnchor.MiddleCenter,
                14,
                parent,
                rimrushTextStyle.TournamentBody);
        }

        /// <summary>
        /// Draw a group of character portraits in the awards ceremony.
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
            var group = CreateTournamentAwardsGroup(key);
            var glow = CreateStandaloneImage(
                $"{key}_Aura",
                rimrushAssets.Images.Ui.EmblemOrb,
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
                glow = rimrushRender.Sprite($"{key}_Aura", rimrushAtlasCache.Instance.Interface, "EmblemsBg0000", x, y - 18f, 0.5f, 0.5f, 12, group.transform);
                glow.transform.localScale *= placement.Placement == 1 ? 0.82f : 0.66f;
                glow.GetComponent<SpriteRenderer>().color = new Color(placement.GlowColor.r, placement.GlowColor.g, placement.GlowColor.b, glowAlpha);
            }

            var shadow = rimrushRender.Sprite($"{key}_Shadow", rimrushAtlasCache.Instance.Interface, "loginSelect0000", x, y + 24f, 0.5f, 0.5f, 13, group.transform);
            shadow.transform.localScale *= shadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, placement.IsPlayer ? 0.7f : 0.52f);

            var playerRoot = new GameObject($"{key}_Root");
            playerRoot.transform.SetParent(group.transform, false);
            var characterScale = scale * rimrushPlayersData.GetCharacterPreviewScaleMultiplier(placement.CharacterId);
            rimrushRender.ApplyPixelTransform(playerRoot.transform, x, y, z, characterScale);

            var armature = rimrushPlayersData.BuildGameplayArmature($"{key}_Armature");
            if (armature != null)
            {
                armature.transform.SetParent(playerRoot.transform, false);
                armature.transform.localPosition = new Vector3(
                    0f,
                    TournamentAwardsArmatureYOffset + rimrushPlayersData.GetCharacterPreviewOffsetY(placement.CharacterId) * 0.65f,
                    0f);
                armature.transform.localScale = new Vector3(TournamentAwardsArmatureScale, TournamentAwardsArmatureScale, 1f);
                rimrushPlayersData.ApplyCharacter(armature, placement.CharacterId);

                if (placement.IsPlayer)
                {
                    awardsCelebrationPlayer = armature;
                    awardsCelebrationCupAnimation = placement.CupAnimation;
                }
            }

            RegisterTournamentAwardsAnimation(group.transform, new Vector2(0f, 12f), landingDelay, 0.44f, 0.96f);
        }

        /// <summary>
        /// Create the ordered list of placement data for the awards ceremony.
        /// </summary>
        private TournamentAwardsPlacement[] BuildTournamentAwardsPlacements(rimrushTournamentData tournament)
        {
            return new[]
            {
                CreateTournamentAwardsPlacement(1, tournament.ChampionCharacterId, tournament.PlayerCharacterId),
                CreateTournamentAwardsPlacement(2, GetMatchLoserCharacterId(tournament.FinalResult), tournament.PlayerCharacterId),
                CreateTournamentAwardsPlacement(3, tournament.ThirdPlaceResult.WinnerCharacterId, tournament.PlayerCharacterId)
            };
        }

        /// <summary>
        /// Draw a single placement entry in the awards ceremony.
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
        /// Add an item to the awards animation queue so it slides in with a delay.
        /// </summary>
        private void RegisterTournamentAwardsAnimation(Transform root, Vector2 startOffsetPixels, float delay, float duration, float startScale = 0.94f, bool fade = true)
        {
            if (root == null)
            {
                return;
            }

            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            var textMeshes = root.GetComponentsInChildren<TextMesh>(true);
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

            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                item.SpriteBaseColors[i] = spriteRenderers[i].color;
            }

            for (var i = 0; i < textMeshes.Length; i++)
            {
                item.TextBaseColors[i] = textMeshes[i].color;
            }

            root.localPosition = item.TargetLocalPosition + item.StartLocalOffset;
            root.localScale = item.TargetLocalScale * item.StartScale;
            ApplyTournamentAwardsAlpha(item, fade ? 0f : 1f);
            awardsAnimatedItems.Add(item);
        }

        /// <summary>
        /// Tick the awards entrance animations each frame.
        /// </summary>
        private void UpdateTournamentAwardsSequence(float deltaTime)
        {
            if (currentScreen != rimrushBootstrapScreen.TournamentAwards || runtimeRoot == null)
            {
                return;
            }

            awardsElapsed += deltaTime;
            for (var i = 0; i < awardsAnimatedItems.Count; i++)
            {
                var item = awardsAnimatedItems[i];
                if (item.Root == null)
                {
                    continue;
                }

                var normalized = Mathf.Clamp01((awardsElapsed - item.Delay) / item.Duration);
                var eased = EaseOutBack01(normalized);
                item.Root.localPosition = item.TargetLocalPosition + Vector3.Lerp(item.StartLocalOffset, Vector3.zero, eased);
                item.Root.localScale = item.TargetLocalScale * Mathf.Lerp(item.StartScale, 1f, Mathf.SmoothStep(0f, 1f, normalized));
                ApplyTournamentAwardsAlpha(item, item.Fade ? normalized : 1f);
            }

            if (!awardsCelebrationTriggered && awardsCelebrationPlayer != null && awardsElapsed >= TournamentAwardsCelebrationDelay)
            {
                awardsCelebrationTriggered = true;
                awardsCelebrationPlayer.Play("happiness");
                awardsCelebrationPlayer.RefreshPose();

                var cupArmature = awardsCelebrationPlayer.GetChildArmature("effects stun");
                if (cupArmature != null && !string.IsNullOrEmpty(awardsCelebrationCupAnimation))
                {
                    cupArmature.StopAtStart(awardsCelebrationCupAnimation);
                }
            }
        }

        /// <summary>
        /// Fade all awards sprites and text to a given opacity.
        /// </summary>
        private static void ApplyTournamentAwardsAlpha(TournamentAwardsAnimatedItem item, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
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
        /// Convert a pixel-space offset to a local-space offset relative to a parent transform.
        /// </summary>
        private static Vector3 PixelOffsetToLocal(Vector2 pixelOffset)
        {
            return new Vector3(pixelOffset.x * rimrushConstants.UnitsPerPixel, -pixelOffset.y * rimrushConstants.UnitsPerPixel, 0f);
        }

        /// <summary>
        /// Apply an overshoot ease curve. Returns values that briefly exceed 1 before settling.
        /// </summary>
        private static float EaseOutBack01(float value)
        {
            value = Mathf.Clamp01(value);
            const float overshoot = 1.70158f;
            var shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        /// <summary>
        /// Return the character id of the loser in a given match.
        /// </summary>
        private static int GetMatchLoserCharacterId(rimrushTournamentMatchResult match)
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
        /// Return the congratulatory or consolation message for the player placement.
        /// </summary>
        private static string GetTournamentAwardsPlayerMessage(rimrushTournamentData tournament)
        {
            switch (tournament.PlayerPlacement)
            {
                case 1:
                    return rimrushSinglePlayerNarrative.LanternChampion;
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
        /// Return a short ordinal like "1st", "2nd", "3rd" for a placement number.
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
        /// Draw a character portrait sprite at the given position and scale.
        /// </summary>
        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder)
        {
            return CreateTournamentPortrait(name, characterId, x, y, targetPixels, sortingOrder, runtimeRoot);
        }

        /// <summary>
        /// Draw a character portrait sprite at the given position and scale.
        /// </summary>
        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            var targetSize = targetPixels * rimrushPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var sprite = rimrushPlayersData.GetCharacterPortraitSprite(characterId, targetSize);
            if (sprite == null)
            {
                return null;
            }

            var portrait = new GameObject(name);
            portrait.transform.SetParent(parent, false);
            var renderer = portrait.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetSize / Mathf.Max(1f, spritePixels);
            rimrushRender.ApplyPixelTransform(
                portrait.transform,
                x,
                y + rimrushPlayersData.GetCharacterPortraitOffsetY(characterId, sprite) * scale,
                0f,
                scale);
            return portrait;
        }

        /// <summary>
        /// Draw a small badge used in tournament displays.
        /// </summary>
        private void CreateTournamentMiniBadge(string key, int characterId, float x, float y, int sortingOrder)
        {
            rimrushRender.PortraitBackplate(
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
        /// Draw a bordered panel rectangle on a menu screen.
        /// </summary>
        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            return CreateFramedPanel(name, frame, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        /// <summary>
        /// Draw a bordered panel rectangle on a menu screen.
        /// </summary>
        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            var standalonePanel = TryCreateStandaloneFrame(name, frame, x, y, width, height, sortingOrder, tint, parent);
            if (standalonePanel != null)
            {
                return standalonePanel;
            }

            var panel = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = rimrushAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                var sourceWidth = Mathf.Max(1f, atlasFrame.SourceW);
                var sourceHeight = Mathf.Max(1f, atlasFrame.SourceH);
                panel.transform.localScale = new Vector3(
                    rimrushConstants.UnitsPerPixel * width / sourceWidth,
                    rimrushConstants.UnitsPerPixel * height / sourceHeight,
                    1f);
            }

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static GameObject TryCreateStandaloneFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            var imageKey = ResolveStandaloneFrameImage(frame);
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            var panel = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                rimrushConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
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
            if (string.IsNullOrEmpty(imageKey))
            {
                return null;
            }

            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(imageKey));
            if (texture == null)
            {
                return null;
            }

            var resolvedParent = parent ?? runtimeRoot;
            var image = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, resolvedParent);
            image.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / Mathf.Max(1f, texture.width),
                rimrushConstants.UnitsPerPixel * height / Mathf.Max(1f, texture.height),
                1f);
            image.GetComponent<SpriteRenderer>().color = tint ?? Color.white;
            return image;
        }

        private static string ResolveStandaloneFrameImage(string frame)
        {
            return frame switch
            {
                "0bg100000" => rimrushAssets.Images.Ui.FramePanelLarge,
                "MatchBack0001" => rimrushAssets.Images.Ui.FrameMatchCardIdle,
                "MatchBack0002" => rimrushAssets.Images.Ui.FrameMatchCardActive,
                "btn_bg0000" => rimrushAssets.Images.Ui.MenuButtonPlate,
                _ => null
            };
        }

        private void CreateStoryIntroLorePanel(rimrushStoryPanelDefinition panel)
        {
            if (panel == null || !panel.HasLore)
            {
                return;
            }

            var loreIconX = StoryIntroLoreButtonX + StoryIntroLoreIconOffsetX;
            var loreIconY = StoryIntroLoreButtonY + StoryIntroLoreIconOffsetY;
            var loreLabelX = StoryIntroLoreButtonX + StoryIntroLoreLabelOffsetX;

            storyIntroLoreButton = new rimrushMenuButton(
                "lore",
                StoryIntroLoreButtonX,
                StoryIntroLoreButtonY,
                StoryIntroLoreButtonHitWidth,
                StoryIntroLoreButtonHitHeight,
                ToggleStoryIntroLore,
                runtimeRoot,
                26,
                rimrushTextStyle.TournamentAccent);
            storyIntroLoreButton.SetBackgroundVisible(false);
            storyIntroLoreButton.SetLabelVisible(false);
            menuButtons.Add(storyIntroLoreButton);
            storyIntroLoreLabelObject = CreateStoryIntroLoreButtonLabel(
                "lore",
                loreLabelX,
                StoryIntroLoreButtonY + StoryIntroLoreLabelOffsetY);

            storyIntroLoreArtRoot = new GameObject("StoryIntroLoreArt");
            storyIntroLoreArtRoot.transform.SetParent(runtimeRoot, false);
            storyIntroLoreIconRenderer = CreateStandaloneImage(
                    "StoryIntroLoreIcon",
                    rimrushAssets.Images.Ui.EmblemOrb,
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

            storyIntroLoreRoot = new GameObject("StoryIntroLoreRoot");
            storyIntroLoreRoot.transform.SetParent(runtimeRoot, false);
            var loreRootTransform = storyIntroLoreRoot.transform;
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
                rimrushAssets.Images.Ui.PanelFillSoft,
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

            var mode = rimrushSinglePlayerNarrative.GetMode(storyIntroMode);
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
                rimrushTextStyle.TournamentAccent);
            CreateToggleableStoryIntroText(
                "StoryIntroLoreTitle",
                panel.LoreTitle,
                StoryIntroLorePanelX,
                StoryIntroLorePanelY - 82f,
                24,
                new Color32(0x4B, 0x26, 0x11, 0xFF),
                TextAnchor.MiddleCenter,
                30,
                rimrushTextStyle.StoryScrollTitle);
            CreateToggleableStoryIntroText(
                "StoryIntroLoreBody",
                panel.LoreBody,
                StoryIntroLorePanelX - 86f,
                StoryIntroLorePanelY - 42f,
                14,
                new Color32(0x43, 0x2A, 0x16, 0xFF),
                TextAnchor.UpperLeft,
                30,
                rimrushTextStyle.StoryScrollBody);

            SetStoryIntroLoreVisibility(false);
        }

        private void ToggleStoryIntroLore()
        {
            SetStoryIntroLoreVisibility(!storyIntroLoreOpen);
        }

        private void SetStoryIntroLoreVisibility(bool isVisible)
        {
            if (isVisible)
            {
                storyIntroPauseBeforeLore = storyIntroPaused;
                storyIntroPaused = true;
            }
            else
            {
                storyIntroPaused = storyIntroPauseBeforeLore;
            }

            storyIntroLoreOpen = isVisible;
            if (storyIntroLoreRoot != null)
            {
                storyIntroLoreRoot.SetActive(isVisible);
            }

            if (storyIntroLoreArtRoot != null)
            {
                storyIntroLoreArtRoot.SetActive(!isVisible);
            }

            for (var i = 0; i < storyIntroLoreTextObjects.Count; i++)
            {
                if (storyIntroLoreTextObjects[i] != null)
                {
                    storyIntroLoreTextObjects[i].SetActive(isVisible);
                }
            }

            RefreshStoryIntroLoreButtonLayout(isVisible);
            SetStoryIntroLoreButtonLabelText(isVisible ? "hide" : "lore");
            SetStoryIntroLoreButtonLabelColor(isVisible ? StoryIntroLoreOpenLabelColor : StoryIntroLoreClosedLabelColor);
            if (storyIntroLoreIconRenderer != null)
            {
                storyIntroLoreIconRenderer.color = isVisible
                    ? new Color(1f, 0.92f, 0.7f, 0.96f)
                    : new Color(storyIntroAccentColor.r, storyIntroAccentColor.g, storyIntroAccentColor.b, 0.84f);
            }

            RefreshStoryIntroPauseButtonLabel();
        }

        private void RefreshStoryIntroPauseButtonLabel()
        {
            if (storyIntroPauseButton == null)
            {
                return;
            }

            storyIntroPauseButton.SetText(storyIntroLoreOpen ? "close" : storyIntroPaused ? "resume" : "pause");
        }

        /// <summary>
        /// Draw a horizontal line connecting two bracket positions.
        /// </summary>
        private void CreateHorizontalConnector(string name, float startX, float endX, float y, bool highlighted, int sortingOrder = 10, float thickness = TournamentConnectorThickness)
        {
            var left = Mathf.Min(startX, endX);
            var width = Mathf.Abs(endX - startX);
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
        /// Draw a vertical line connecting two bracket positions.
        /// </summary>
        private void CreateVerticalConnector(string name, float x, float startY, float endY, bool highlighted, int sortingOrder = 10, float thickness = TournamentConnectorThickness)
        {
            var top = Mathf.Min(startY, endY);
            var height = Mathf.Abs(endY - startY);
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
        /// Draw an L-shaped line connecting two bracket positions.
        /// </summary>
        private void CreateElbowConnector(string name, float startX, float startY, float endX, float endY, bool highlighted)
        {
            var midX = (startX + endX) * 0.5f;
            CreateHorizontalConnector($"{name}_H1", startX, midX, startY, highlighted);
            CreateVerticalConnector($"{name}_V", midX, startY, endY, highlighted);
            CreateHorizontalConnector($"{name}_H2", midX, endX, endY, highlighted);
        }

        /// <summary>
        /// Return a smaller font size if the text is too long for the available width.
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
        /// Clear all awards animation data so the ceremony can be rebuilt.
        /// </summary>
        private void ResetTournamentAwardsState()
        {
            awardsAnimatedItems.Clear();
            awardsElapsed = 0f;
            awardsCelebrationTriggered = false;
            awardsCelebrationPlayer = null;
            awardsCelebrationCupAnimation = null;
        }

        /// <summary>
        /// Pick default characters for the two-player setup so both sides start with different fighters.
        /// </summary>
        private void SeedTwoPlayerSelection()
        {
            var match = rimrushInventory.Instance.MatchData;
            versusLeftCharacterId = rimrushPlayersData.SanitizeCharacterId(match.CharacterIds[0]);
            versusRightCharacterId = rimrushPlayersData.SanitizeCharacterId(match.CharacterIds[1], rimrushPlayersData.StepCharacterId(versusLeftCharacterId, 1));
        }

        /// <summary>
        /// Destroy all menu or gameplay objects and reset the scene.
        /// </summary>
        private void ClearRuntime()
        {
            gameCore?.Shutdown();
            gameCore = null;
            menuButtons.Clear();
            menuMusicButton = null;
            menuHelpButton = null;
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
            nativeMenuTextLayer?.Dispose();
            nativeMenuTextLayer = null;
            ResetTournamentAwardsState();
            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot.gameObject);
                runtimeRoot = null;
            }
        }

        /// <summary>
        /// Mute or unmute the background music.
        /// </summary>
        private static void ToggleBackgroundMusic()
        {
            rimrushAudio.Instance?.ToggleMusic();
        }

        /// <summary>
        /// Empty placeholder callback that does nothing.
        /// </summary>
        private static void NoOpAction()
        {
        }

        /// <summary>
        /// Return 0 if music is playing, 1 if muted (used to pick the right button icon).
        /// </summary>
        private static int GetMusicIconIndex()
        {
            return rimrushAudio.Instance != null && rimrushAudio.Instance.MusicEnabled ? 0 : 1;
        }

        /// <summary>
        /// Return a status line like "SEASON RUN / ROUND 3" for the bracket header.
        /// </summary>
        private static string GetTournamentStatusText(rimrushTournamentData tournament)
        {
            if (tournament.Completed)
            {
                if (tournament.PlayerPlacement == 1)
                {
                    return rimrushSinglePlayerNarrative.LanternChampion;
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

            if (tournament.CurrentStage == rimrushTournamentStage.RegularSeason)
            {
                return tournament.RegularSeasonCompleted
                    ? "FINALS READY"
                    : $"ROUND {tournament.CurrentRegularSeasonRoundIndex + 1}";
            }

            if (tournament.CurrentStage == rimrushTournamentStage.SemiFinal)
            {
                return "FINAL FOUR";
            }

            if (tournament.CurrentStage == rimrushTournamentStage.ThirdPlace)
            {
                return "3RD PLACE MATCH";
            }

            return "GRAND FINAL";
        }

        /// <summary>
        /// Return the character name, or "TBD" if the slot is empty.
        /// </summary>
        private static string CharacterNameOrTbd(int characterId)
        {
            return characterId >= 0 ? rimrushPlayersData.GetCharacterName(characterId) : "TBD";
        }

        /// <summary>
        /// Move to the next or previous character id, wrapping around the full roster.
        /// </summary>
        private static int WrapCharacter(int currentCharacterId, int direction)
        {
            return rimrushPlayersData.StepCharacterId(currentCharacterId, direction);
        }
    }
}
