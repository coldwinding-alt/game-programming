using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLGameBootstrap : MonoBehaviour
    {
        private enum BLBootstrapScreen
        {
            PlayerCount,
            MatchType,
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

        private readonly System.Collections.Generic.List<BLMenuButton> menuButtons = new System.Collections.Generic.List<BLMenuButton>();
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
        private const float MenuLogoScaleX = 0.78f;
        private const float MenuLogoScaleY = 0.68f;
        private const float TournamentBoardX = BLConstants.Width2;
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
        private const float TournamentAwardsBadgeY = 114f;
        private const float TournamentAwardsBannerY = 172f;
        private const float TournamentAwardsPodiumY = 382f;
        private const float TournamentAwardsBadgeSideOffsetX = 106f;
        private const float TournamentAwardsChampionX = BLConstants.Width2;
        private const float TournamentAwardsChampionY = 302f;
        private const float TournamentAwardsLeftX = BLConstants.Width2 - 106f;
        private const float TournamentAwardsLeftY = 316f;
        private const float TournamentAwardsRightX = BLConstants.Width2 + 106f;
        private const float TournamentAwardsRightY = 324f;
        private const float TournamentAwardsChampionScale = 0.84f;
        private const float TournamentAwardsSideScale = 0.8f;
        private const float TournamentAwardsArmatureScale = 0.82f;
        private const float TournamentAwardsArmatureYOffset = -18f;
        private const float TournamentAwardsPodiumScale = 0.98f;
        private const float TournamentAwardsCelebrationDelay = 0.66f;
        private const float MenuTopButtonY = 44f;
        private const float MenuMusicButtonX = 770f;
        private const float MenuHelpButtonX = 706f;
        private const float MenuTopButtonSize = 60f;
        private const float MenuTopIconPixels = 58f;
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
        private BLGameCore gameCore;
        private Camera mainCamera;
        private BLFixedResolutionPresenter fixedResolutionPresenter;
        private BLBootstrapScreen currentScreen;
        private BLParticipantMode pendingParticipantMode = BLParticipantMode.OnePlayer;
        private int quickCharacterId;
        private int trainingCharacterId;
        private int tournamentCharacterId;
        private int versusLeftCharacterId;
        private int versusRightCharacterId;
        private BLBallSelection quickBallSelection;
        private BLBallSelection trainingBallSelection;
        private BLBallSelection tournamentBallSelection;
        private BLBallSelection versusBallSelection;
        private float awardsElapsed;
        private bool awardsCelebrationTriggered;
        private DBLiteArmature awardsCelebrationPlayer;
        private string awardsCelebrationCupAnimation;
        private BLIconButton menuMusicButton;
        private BLIconButton menuHelpButton;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = BLConstants.GameH / (2f * BLConstants.PixelsPerUnit);
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = Color.black;
            fixedResolutionPresenter = GetComponent<BLFixedResolutionPresenter>();
            if (fixedResolutionPresenter == null)
            {
                fixedResolutionPresenter = gameObject.AddComponent<BLFixedResolutionPresenter>();
            }

            fixedResolutionPresenter.Attach(mainCamera);

            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform);

            var inventory = BLInventory.Instance;
            quickCharacterId = BLPlayersData.SanitizeCharacterId(inventory.SelectedQuickCharacterId);
            trainingCharacterId = BLPlayersData.SanitizeCharacterId(inventory.SelectedTrainingCharacterId, quickCharacterId);
            tournamentCharacterId = BLPlayersData.SanitizeCharacterId(inventory.SelectedTournamentCharacterId, quickCharacterId);
            quickBallSelection = inventory.SelectedQuickBallSelection;
            trainingBallSelection = inventory.SelectedTrainingBallSelection;
            tournamentBallSelection = inventory.SelectedTournamentBallSelection;
            versusBallSelection = inventory.SelectedVersusBallSelection;
            SeedTwoPlayerSelection();
            ShowPlayerCountMenu();
        }

        private void Update()
        {
            if (gameCore != null)
            {
                gameCore.Update(Time.deltaTime);
                if (gameCore.AdvanceFlowRequested)
                {
                    ClearRuntime();
                    BLInventory.Instance.AdvanceTournament();
                    ShowTournamentBracket();
                    return;
                }

                if (gameCore.ReturnToMenuRequested || Input.GetKeyDown(KeyCode.Escape))
                {
                    BLInventory.Instance.AbandonTournament();
                    ClearRuntime();
                    ShowPlayerCountMenu();
                }

                return;
            }

            UpdateTournamentAwardsSequence(Time.deltaTime);

            for (var i = 0; i < menuButtons.Count; i++)
            {
                var screenRoot = runtimeRoot;
                menuButtons[i].Update(mainCamera);
                if (screenRoot != runtimeRoot)
                {
                    break;
                }
            }

            if (runtimeRoot != null)
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

        private void HandleMenuEscape()
        {
            switch (currentScreen)
            {
                case BLBootstrapScreen.MatchType:
                    ShowPlayerCountMenu();
                    break;
                case BLBootstrapScreen.SinglePlayerSetup:
                case BLBootstrapScreen.TournamentSetup:
                    ShowMatchTypeMenu();
                    break;
                case BLBootstrapScreen.TwoPlayerSetup:
                case BLBootstrapScreen.TrainingSetup:
                    ShowPlayerCountMenu();
                    break;
                case BLBootstrapScreen.TournamentBracket:
                case BLBootstrapScreen.TournamentComplete:
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                    break;
                case BLBootstrapScreen.TournamentAwards:
                    ShowTournamentBracket();
                    break;
            }
        }

        private void ShowPlayerCountMenu()
        {
            currentScreen = BLBootstrapScreen.PlayerCount;
            BeginMenuScreen(true, false, "bg2blue0000");
            AddTitle("SELECT PLAYERS", 136f, 34, new Color32(0xD7, 0xF2, 0x4A, 0xFF));
            CreatePanel("PlayersPanel", BLConstants.Width2, 330f, 304f, 230f, 8, new Color(0.05f, 0.08f, 0.1f, 0.72f));

            var inventory = BLInventory.Instance;
            menuButtons.Add(new BLMenuButton("1 PLAYER", BLConstants.Width2, 274f, 228f, 52f, () =>
            {
                pendingParticipantMode = BLParticipantMode.OnePlayer;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowMatchTypeMenu();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("2 PLAYER", BLConstants.Width2, 334f, 228f, 52f, () =>
            {
                pendingParticipantMode = BLParticipantMode.TwoPlayers;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowTwoPlayerSetup();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("TRAINING", BLConstants.Width2, 394f, 228f, 52f, ShowTrainingSetup, runtimeRoot));
        }

        private void ShowMatchTypeMenu()
        {
            currentScreen = BLBootstrapScreen.MatchType;
            pendingParticipantMode = BLParticipantMode.OnePlayer;
            BeginMenuScreen(true, false, "bg10000");
            AddTitle("MATCH TYPE", 136f, 34, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            CreatePanel("ModePanel", BLConstants.Width2, 326f, 320f, 254f, 8, new Color(0.05f, 0.08f, 0.1f, 0.74f));

            menuButtons.Add(new BLMenuButton("TOURNAMENT", BLConstants.Width2, 312f, 246f, 52f, ShowTournamentSetup, runtimeRoot));
            menuButtons.Add(new BLMenuButton("QUICK MATCH", BLConstants.Width2, 372f, 246f, 52f, ShowSinglePlayerSetup, runtimeRoot));
            menuButtons.Add(new BLMenuButton("BACK", BLConstants.Width2, 432f, 200f, 46f, ShowPlayerCountMenu, runtimeRoot));
        }

        private void ShowSinglePlayerSetup()
        {
            currentScreen = BLBootstrapScreen.SinglePlayerSetup;
            pendingParticipantMode = BLParticipantMode.OnePlayer;
            BLInventory.Instance.SetParticipantMode(pendingParticipantMode);
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
                    quickBallSelection = BLBallCatalog.StepSelection(quickBallSelection, -1);
                    ShowSinglePlayerSetup();
                },
                () =>
                {
                    quickBallSelection = BLBallCatalog.StepSelection(quickBallSelection, 1);
                    ShowSinglePlayerSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new BLMenuButton(BLInventory.Instance.DifficultyLabel, 575f, 304f, 188f, 46f, () =>
            {
                BLInventory.Instance.ToggleDifficulty();
                ShowSinglePlayerSetup();
            }, runtimeRoot));
            if (BLInventory.Instance.Difficulty == BLAiDifficulty.Hell)
            {
                CreateHellDifficultyWarning(575f, 346f);
            }

            menuButtons.Add(new BLMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("PLAY", 660f, 452f, 150f, 42f, StartQuickMatchFlow, runtimeRoot));
        }

        private void ShowTrainingSetup()
        {
            currentScreen = BLBootstrapScreen.TrainingSetup;
            pendingParticipantMode = BLParticipantMode.Training;
            BLInventory.Instance.SetParticipantMode(pendingParticipantMode);
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
                    trainingBallSelection = BLBallCatalog.StepSelection(trainingBallSelection, -1);
                    ShowTrainingSetup();
                },
                () =>
                {
                    trainingBallSelection = BLBallCatalog.StepSelection(trainingBallSelection, 1);
                    ShowTrainingSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new BLMenuButton("BACK", 488f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("PLAY", 660f, 452f, 150f, 42f, StartTrainingFlow, runtimeRoot));
        }

        private void ShowTwoPlayerSetup()
        {
            currentScreen = BLBootstrapScreen.TwoPlayerSetup;
            pendingParticipantMode = BLParticipantMode.TwoPlayers;
            BLInventory.Instance.SetParticipantMode(pendingParticipantMode);
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

            BLRender.Text(
                "VersusLabel",
                "VS",
                BLConstants.Width2,
                284f,
                34,
                new Color32(0xFF, 0xA3, 0x00, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.CfCrackBold,
                outlineColor: Color.white,
                outlinePixels: 1.4f);

            CreatePanel("VersusBallPanel", BLConstants.Width2, TwoPlayerBallPanelY, TwoPlayerBallPanelWidth, TwoPlayerBallPanelHeight, 8, new Color(0.05f, 0.08f, 0.1f, 0.82f));
            CreateBallSelector(
                "VersusBall",
                BLConstants.Width2,
                versusBallSelection,
                () =>
                {
                    versusBallSelection = BLBallCatalog.StepSelection(versusBallSelection, -1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusBallSelection = BLBallCatalog.StepSelection(versusBallSelection, 1);
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

            menuButtons.Add(new BLMenuButton("BACK", 212f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("PLAY", 588f, 452f, 150f, 42f, StartTwoPlayerMatch, runtimeRoot));
        }

        private void ShowTournamentSetup()
        {
            currentScreen = BLBootstrapScreen.TournamentSetup;
            pendingParticipantMode = BLParticipantMode.OnePlayer;
            BLInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BLInventory.Instance.SetTournamentSelection(tournamentCharacterId);

            BeginMenuScreen(false, false, "bg2blue0000");
            AddTitle("TOURNAMENT", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

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
            BLRender.Text(
                "ModeFixed",
                "FORMAT: 8 PLAYER / 2 DIVISIONS",
                575f,
                186f,
                12,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.TournamentBody);
            CreateBallSelector(
                "TournamentBall",
                575f,
                tournamentBallSelection,
                () =>
                {
                    tournamentBallSelection = BLBallCatalog.StepSelection(tournamentBallSelection, -1);
                    ShowTournamentSetup();
                },
                () =>
                {
                    tournamentBallSelection = BLBallCatalog.StepSelection(tournamentBallSelection, 1);
                    ShowTournamentSetup();
                },
                OptionBallHeaderY,
                OptionBallPreviewY,
                OptionBallLabelY);

            menuButtons.Add(new BLMenuButton(BLInventory.Instance.DifficultyLabel, 575f, 304f, 188f, 46f, () =>
            {
                BLInventory.Instance.ToggleDifficulty();
                ShowTournamentSetup();
            }, runtimeRoot));
            if (BLInventory.Instance.Difficulty == BLAiDifficulty.Hell)
            {
                CreateHellDifficultyWarning(575f, 346f);
            }

            var enoughCharacters = BLPlayersData.GetActiveCharacterIds().Length >= 8;
            if (!enoughCharacters)
            {
                AddSubtitle("NEED 8 ENABLED CHARACTERS", 408f, 18);
            }

            menuButtons.Add(new BLMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            if (enoughCharacters)
            {
                menuButtons.Add(new BLMenuButton("NEXT", 660f, 452f, 150f, 42f, StartTournamentFlow, runtimeRoot));
            }
        }

        private void StartQuickMatchFlow()
        {
            var inventory = BLInventory.Instance;
            inventory.SetParticipantMode(BLParticipantMode.OnePlayer);
            inventory.SetQuickSelection(quickCharacterId);
            inventory.SetQuickBallSelection(quickBallSelection);
            inventory.StartQuickGame();
            StartGameplay();
        }

        private void StartTrainingFlow()
        {
            var inventory = BLInventory.Instance;
            inventory.SetParticipantMode(BLParticipantMode.Training);
            inventory.SetTrainingSelection(trainingCharacterId);
            inventory.SetTrainingBallSelection(trainingBallSelection);
            inventory.StartTraining();
            StartGameplay();
        }

        private void StartTournamentFlow()
        {
            var inventory = BLInventory.Instance;
            inventory.SetParticipantMode(BLParticipantMode.OnePlayer);
            inventory.SetTournamentSelection(tournamentCharacterId);
            inventory.SetTournamentBallSelection(tournamentBallSelection);
            if (!inventory.BeginTournament())
            {
                ShowTournamentSetup();
                return;
            }

            ShowTournamentBracket();
        }

        private void StartTournamentFinalsFlow()
        {
            BLInventory.Instance.BeginTournamentFinals();
            ShowTournamentBracket();
        }

        private void StartTwoPlayerMatch()
        {
            BLInventory.Instance.SetVersusBallSelection(versusBallSelection);
            BLInventory.Instance.StartTwoPlayerVersus(versusLeftCharacterId, versusRightCharacterId);
            StartGameplay();
        }

        private void ShowTournamentBracket()
        {
            var inventory = BLInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? BLBootstrapScreen.TournamentComplete : BLBootstrapScreen.TournamentBracket;
            var regularSeasonScreen = tournament.CurrentStage == BLTournamentStage.RegularSeason;
            var titleY = regularSeasonScreen ? 42f : 34f;
            var titleFontSize = regularSeasonScreen ? 32 : 30;
            var subtitleY = regularSeasonScreen ? 76f : 62f;
            var subtitleFontSize = regularSeasonScreen ? 18 : 14;

            var backgroundFrame = tournament.Completed || !regularSeasonScreen
                ? "bg10000"
                : "bg2blue0000";
            BeginMenuScreen(false, false, backgroundFrame);
            AddTitle(
                "TOURNAMENT",
                titleY,
                titleFontSize,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF));
            AddSubtitle(
                GetTournamentStatusText(tournament),
                subtitleY,
                subtitleFontSize);
            CreateTournamentBracketBoard(tournament);

            if (tournament.Completed)
            {
                menuButtons.Add(new BLMenuButton("MAIN MENU", 224f, 452f, 184f, 42f, () =>
                {
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new BLMenuButton("CEREMONY", 576f, 452f, 208f, 42f, ShowTournamentAwards, runtimeRoot));
            }
            else if (tournament.CurrentStage == BLTournamentStage.RegularSeason)
            {
                menuButtons.Add(new BLMenuButton("BACK", 172f, 452f, 150f, 42f, () =>
                {
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));

                if (tournament.RegularSeasonCompleted)
                {
                    menuButtons.Add(new BLMenuButton("START FINALS", 606f, 452f, 196f, 42f, StartTournamentFinalsFlow, runtimeRoot));
                }
                else
                {
                    menuButtons.Add(new BLMenuButton($"PLAY ROUND {tournament.CurrentRegularSeasonRoundIndex + 1}", 608f, 452f, 208f, 42f, StartGameplay, runtimeRoot));
                }
            }
            else
            {
                menuButtons.Add(new BLMenuButton("BACK", 172f, 452f, 150f, 42f, () =>
                {
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new BLMenuButton("PLAY", 628f, 452f, 150f, 42f, StartGameplay, runtimeRoot));
            }
        }

        private void ShowTournamentAwards()
        {
            var tournament = BLInventory.Instance.Tournament;
            if (!tournament.Completed)
            {
                ShowTournamentBracket();
                return;
            }

            currentScreen = BLBootstrapScreen.TournamentAwards;
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("AWARDS", 48f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));
            AddSubtitle("TOURNAMENT CEREMONY", 82f, 20);
            CreateTournamentAwardsScene(tournament);

            menuButtons.Add(new BLMenuButton("BRACKET", 220f, 452f, 180f, 42f, ShowTournamentBracket, runtimeRoot));
            menuButtons.Add(new BLMenuButton("MAIN MENU", 580f, 452f, 200f, 42f, () =>
            {
                BLInventory.Instance.AbandonTournament();
                ShowPlayerCountMenu();
            }, runtimeRoot));
        }

        private void StartGameplay()
        {
            ClearRuntime();
            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform).PlayMusic(BLAssets.Sounds.MenuMusic);
            menuButtons.Clear();
            gameCore = new BLGameBuilder().Build(runtimeRoot);
        }

        private void BeginMenuScreen(bool showLogo, bool showControls, string backgroundFrame)
        {
            ClearRuntime();
            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform).PlayMusic(BLAssets.Sounds.MenuMusic);

            BLRender.Sprite(
                "MenuBackground",
                BLAtlasCache.Instance.Interface,
                backgroundFrame,
                BLConstants.Width2,
                240f,
                0.5f,
                0.5f,
                0,
                runtimeRoot);

            if (showLogo)
            {
                var logoTexture = Resources.Load<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameLogo));
                if (logoTexture != null)
                {
                    var logo = BLRender.Image("Logo", logoTexture, BLConstants.Width2, 68f, 0.5f, 0.5f, 20, runtimeRoot);
                    logo.transform.localScale = new Vector3(
                        logo.transform.localScale.x * MenuLogoScaleX,
                        logo.transform.localScale.y * MenuLogoScaleY,
                        1f);
                }
            }

            menuMusicButton = new BLIconButton(
                "MenuMusicButton",
                MenuMusicButtonX,
                MenuTopButtonY,
                MenuTopButtonSize,
                MenuTopButtonSize,
                ToggleBackgroundMusic,
                runtimeRoot,
                32,
                MenuTopIconPixels,
                BLAssets.Images.ResourcePath(BLAssets.Images.MusicButtonOn),
                BLAssets.Images.ResourcePath(BLAssets.Images.MusicButtonOff));
            menuMusicButton.SetActiveIconIndex(GetMusicIconIndex());
            menuHelpButton = new BLIconButton(
                "MenuHelpButton",
                MenuHelpButtonX,
                MenuTopButtonY,
                MenuTopButtonSize,
                MenuTopButtonSize,
                NoOpAction,
                runtimeRoot,
                32,
                MenuTopIconPixels,
                BLAssets.Images.ResourcePath(BLAssets.Images.HelpButton));

            if (showControls)
            {
                BLRender.Text(
                    "Controls",
                    BLControlsData.MainMenuControlsText,
                    BLConstants.Width2,
                    492f,
                    11,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    30,
                    runtimeRoot,
                    BLTextStyle.TournamentBody);
            }

            menuButtons.Clear();
        }

        private void AddTitle(string title, float y, int fontSize, Color color)
        {
            BLRender.Text(
                $"{title}_Title",
                title,
                BLConstants.Width2,
                y,
                fontSize,
                color,
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.DisplayTitle);
        }

        private void AddSubtitle(string subtitle, float y, int fontSize = 18)
        {
            BLRender.Text(
                $"{subtitle}_Subtitle",
                subtitle,
                BLConstants.Width2,
                y,
                fontSize,
                Color.white,
                TextAnchor.MiddleCenter,
                19,
                runtimeRoot,
                BLTextStyle.Subtitle);
        }

        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            return CreatePanel(name, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            var panel = BLRender.Sprite(name, BLAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                BLConstants.UnitsPerPixel * width / 10f,
                BLConstants.UnitsPerPixel * height / 10f,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private void CreateOptionsPanel(string prefix, float centerX)
        {
            BLRender.Text(
                $"{prefix}_OptionTitle",
                "SETTINGS",
                centerX,
                160f,
                20,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.TournamentAccent);
        }

        private void CreateBallSelector(
            string key,
            float centerX,
            BLBallSelection selection,
            System.Action previousBallAction,
            System.Action nextBallAction,
            float headerY,
            float previewY,
            float labelY)
        {
            BLRender.Text(
                $"{key}_Header",
                "BALL",
                centerX,
                headerY,
                16,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.TournamentAccent);

            menuButtons.Add(new BLMenuButton("<", centerX - BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, previousBallAction, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", centerX + BallSelectorArrowOffsetX, previewY, BallSelectorArrowSize, BallSelectorArrowSize, nextBallAction, runtimeRoot));

            CreateBallPreview(
                $"{key}_Preview",
                BLBallCatalog.PreviewTheme(selection),
                centerX,
                previewY + 1f,
                BallPreviewPixels,
                19);

            BLRender.Text(
                $"{key}_Label",
                BLBallCatalog.Label(selection),
                centerX,
                labelY,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.TournamentBody);
        }

        private void CreateHellDifficultyWarning(float centerX, float y)
        {
            BLRender.Text(
                "HellDifficultyWarning",
                "UNFAIR CHALLENGE: CPU USES BONUS SUPERS",
                centerX,
                y,
                12,
                new Color32(0xFF, 0x9C, 0x32, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.TournamentAccent);
        }

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
            BLRender.Text(
                $"{key}_Header",
                header,
                centerX,
                SelectorHeaderY,
                header.Length <= 2 ? 26 : 18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLTextStyle.TournamentAccent);

            menuButtons.Add(new BLMenuButton("<", centerX - SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, previousCharacterAction, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", centerX + SelectorArrowOffsetX, SelectorArrowY, SelectorArrowSize, SelectorArrowSize, nextCharacterAction, runtimeRoot));

            CreatePreviewPlayer(characterId, centerX, previewY, previewScale);
            BLRender.Text(
                $"{key}_CharacterName",
                BLPlayersData.GetCharacterName(characterId),
                centerX,
                nameY,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLTextStyle.TournamentBody);
        }

        private void CreateBallPreview(string name, BLBallTheme theme, float x, float y, float targetPixels, int sortingOrder)
        {
            var sprite = BLGameplaySpriteLoader.LoadBallThemeSprite(theme, 0.5f, 0.5f) ??
                         BLAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);

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
            BLRender.ApplyPixelTransform(preview.transform, x, y, 0f, scale);
        }

        private void CreatePreviewPlayer(int characterId, float x, float y, float scale)
        {
            var previewScale = scale * PreviewScaleFactor * BLPlayersData.GetCharacterPreviewScaleMultiplier(characterId);
            var shadow = BLRender.Sprite("PreviewShadow", BLAtlasCache.Instance.Interface, "loginSelect0000", x, y + PreviewShadowYOffset, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= PreviewShadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.55f);

            var previewRoot = new GameObject($"Preview_{characterId}");
            previewRoot.transform.SetParent(runtimeRoot, false);
            BLRender.ApplyPixelTransform(previewRoot.transform, x, y, 0f, previewScale);

            var armature = BLPlayersData.BuildGameplayArmature($"PreviewArmature_{characterId}");
            if (armature == null)
            {
                return;
            }

            armature.transform.SetParent(previewRoot.transform, false);
            armature.transform.localPosition = new Vector3(0f, PreviewArmatureYOffset + BLPlayersData.GetCharacterPreviewOffsetY(characterId), 0f);
            armature.transform.localScale = new Vector3(PreviewArmatureScale, PreviewArmatureScale, 1f);
            BLPlayersData.ApplyCharacter(armature, characterId);
        }

        private void CreateTournamentBracketBoard(BLTournamentData tournament)
        {
            if (tournament.CurrentStage == BLTournamentStage.RegularSeason)
            {
                CreateTournamentRegularSeasonBoard(tournament);
            }
            else
            {
                CreateTournamentPlayoffBoard(tournament);
            }

            CreateTournamentSummaryPanel(tournament);
        }

        private void CreateTournamentRegularSeasonBoard(BLTournamentData tournament)
        {
            CreatePanel(
                "RegularSeasonBackdrop",
                BLConstants.Width2,
                236f,
                744f,
                302f,
                8,
                new Color(0.01f, 0.04f, 0.08f, 0.28f));

            CreateTournamentDivisionBoard("DivisionA", 222f, 242f, "DIV. A", tournament, 0);
            CreateTournamentDivisionBoard("DivisionB", 578f, 242f, "DIV. B", tournament, 1);
        }

        private void CreateTournamentDivisionBoard(string key, float x, float y, string title, BLTournamentData tournament, int divisionIndex)
        {
            const float boardWidth = 292f;
            const float boardHeight = 322f;
            const float rowHeight = 34f;
            const float rowSpacing = 42f;

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
                boardWidth - 74f,
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
                boardWidth - 98f,
                24f,
                11,
                new Color(0.15f, 0.67f, 0.82f, 0.92f));

            CreateStandingsAccentText($"{key}_HeaderRank", "#", x - 102f, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);
            CreateStandingsAccentText($"{key}_HeaderTeam", "TEAM", x - 52f, headerY + 1f, 12, Color.white, TextAnchor.MiddleLeft, 12);
            CreateStandingsAccentText($"{key}_HeaderW", "W", x + 58f, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);
            CreateStandingsAccentText($"{key}_HeaderL", "L", x + 90f, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);
            CreateStandingsAccentText($"{key}_HeaderPct", "PCT", x + 124f, headerY + 1f, 12, Color.white, TextAnchor.MiddleCenter, 12);

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
                    boardWidth - 110f,
                    rowHeight,
                    11,
                    rowTint);

                CreateStandingsAccentText(
                    $"{key}_Rank_{i}",
                    (i + 1).ToString(),
                    x - 102f,
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
                    x - 72f,
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
                    x - 48f,
                    rowY + 1f,
                    GetCompactFontSize(name, 13, 12, 11),
                    Color.white,
                    TextAnchor.MiddleLeft,
                    12);
                CreateStandingsAccentText(
                    $"{key}_W_{i}",
                    entry.Wins.ToString(),
                    x + 58f,
                    rowY + 1f,
                    14,
                    qualified ? new Color32(0xD7, 0xF2, 0x4A, 0xFF) : Color.white,
                    TextAnchor.MiddleCenter,
                    12);
                CreateStandingsAccentText(
                    $"{key}_L_{i}",
                    entry.Losses.ToString(),
                    x + 90f,
                    rowY + 1f,
                    14,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    12);
                CreateStandingsBodyText(
                    $"{key}_Pct_{i}",
                    FormatWinningPercentage(entry.Percentage),
                    x + 124f,
                    rowY + 1f,
                    12,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    12);

                if (qualified)
                {
                    CreateStandingsAccentText(
                        $"{key}_Qualified_{i}",
                        "Q",
                        x + 152f,
                        rowY + 1f,
                        12,
                        new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                        TextAnchor.MiddleCenter,
                        12);
                }
            }
        }

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
            return BLRender.Text(
                name,
                text,
                x,
                y,
                fontSize,
                color,
                anchor,
                sortingOrder,
                runtimeRoot,
                BLFontKind.RajdhaniSemiBold,
                outlineColor: new Color(0.02f, 0.03f, 0.08f, 0.9f),
                outlinePixels: fontSize >= 13 ? 0.7f : 0.58f);
        }

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
            return BLRender.Text(
                name,
                text,
                x,
                y,
                fontSize,
                color,
                anchor,
                sortingOrder,
                runtimeRoot,
                BLFontKind.RajdhaniBold,
                outlineColor: new Color(0.02f, 0.03f, 0.08f, 0.92f),
                outlinePixels: fontSize >= 14 ? 0.8f : 0.62f);
        }

        private void CreateTournamentPlayoffBoard(BLTournamentData tournament)
        {
            const float playoffBackdropY = 232f;
            const float playoffBackdropHeight = 292f;
            const float finalPanelY = 168f;
            const float semiPanelY = 238f;
            const float semiPanelOffsetX = 200f;
            const float placementPanelY = 324f;

            CreatePanel(
                "PlayoffBackdrop",
                BLConstants.Width2,
                playoffBackdropY,
                742f,
                playoffBackdropHeight,
                8,
                new Color(0.02f, 0.05f, 0.08f, 0.28f));

            var semiCurrent = !tournament.Completed && tournament.CurrentStage == BLTournamentStage.SemiFinal;
            CreateTournamentPlayoffMatchPanel(
                "SemiFinalLeft",
                BLConstants.Width2 - semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[0],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[0], tournament.PlayerCharacterId));
            CreateTournamentPlayoffMatchPanel(
                "SemiFinalRight",
                BLConstants.Width2 + semiPanelOffsetX,
                semiPanelY,
                "SEMIFINAL",
                tournament.SemiFinalResults[1],
                semiCurrent && MatchIncludesPlayer(tournament.SemiFinalResults[1], tournament.PlayerCharacterId));
            CreateTournamentPlayoffMatchPanel(
                "FinalMatch",
                BLConstants.Width2,
                finalPanelY,
                "FINAL",
                tournament.FinalResult,
                !tournament.Completed && tournament.CurrentStage == BLTournamentStage.Final);
            CreateTournamentPlayoffMatchPanel(
                "ThirdPlaceMatch",
                BLConstants.Width2,
                placementPanelY,
                "3RD PLACE MATCH",
                tournament.ThirdPlaceResult,
                !tournament.Completed && tournament.CurrentStage == BLTournamentStage.ThirdPlace);
        }

        private void CreateTournamentPlayoffMatchPanel(string key, float x, float y, string title, BLTournamentMatchResult match, bool current)
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

            BLRender.Text(
                $"{key}_Title",
                title,
                x,
                y - titleOffsetY,
                title.Length > 10 ? 13 : 15,
                current ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                15,
                runtimeRoot,
                BLTextStyle.TournamentAccent);

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
                BLRender.Text(
                    $"{key}_Versus",
                    "-",
                    x + scoreXOffset,
                    y,
                    18,
                    current ? new Color32(0xFF, 0xD6, 0x6D, 0xFF) : new Color32(0x42, 0xF1, 0xE6, 0xFF),
                    TextAnchor.MiddleCenter,
                    16,
                    runtimeRoot,
                    BLTextStyle.TournamentAccent);
            }
        }

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

        private void CreateTournamentSummaryPanel(BLTournamentData tournament)
        {
            var summaryCompleted = tournament.Completed;
            var summaryWidth = summaryCompleted ? 312f : 292f;
            var summaryHeight = summaryCompleted ? 72f : 50f;
            var summaryY = summaryCompleted ? TournamentSummaryY : TournamentSummaryY + 2f;
            CreateFramedPanel(
                "TournamentSummaryPanel",
                "btn_bg0000",
                BLConstants.Width2,
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
                BLRender.Text(
                    "ChampionLabel",
                    "CHAMPION",
                    312f,
                    summaryY - 12f,
                    12,
                    new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                    TextAnchor.MiddleLeft,
                    17,
                    runtimeRoot,
                    BLTextStyle.TournamentAccent);
                BLRender.Text(
                    "ChampionName",
                    CharacterNameOrTbd(tournament.ChampionCharacterId),
                    312f,
                    summaryY + 2f,
                    GetCompactFontSize(CharacterNameOrTbd(tournament.ChampionCharacterId), 15, 13, 11),
                    Color.white,
                    TextAnchor.MiddleLeft,
                    17,
                    runtimeRoot,
                    BLTextStyle.TournamentBody);
                BLRender.Text(
                    "PlacementLabel",
                    $"YOU FINISHED #{tournament.PlayerPlacement}",
                    312f,
                    summaryY + 18f,
                    11,
                    new Color32(0xFF, 0xD6, 0x6D, 0xFF),
                    TextAnchor.MiddleLeft,
                    17,
                    runtimeRoot,
                    BLTextStyle.TournamentAccent);
                return;
            }

            string headline;
            string detail;
            if (tournament.CurrentStage == BLTournamentStage.RegularSeason)
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
            else if (tournament.CurrentStage == BLTournamentStage.SemiFinal)
            {
                headline = "UP NEXT";
                detail = GetMatchupText(GetPlayerPlayoffMatch(tournament), "SEMIFINAL SET");
            }
            else if (tournament.CurrentStage == BLTournamentStage.ThirdPlace)
            {
                headline = "UP NEXT";
                detail = GetMatchupText(tournament.ThirdPlaceResult, "3RD PLACE MATCH");
            }
            else
            {
                headline = "UP NEXT";
                detail = GetMatchupText(tournament.FinalResult, "FINAL");
            }

            CreateStandingsAccentText(
                "TournamentSummaryHeadline",
                headline,
                BLConstants.Width2,
                summaryY - 8f,
                13,
                new Color32(0xD7, 0xF2, 0x4A, 0xFF),
                TextAnchor.MiddleCenter,
                17);
            CreateStandingsBodyText(
                "TournamentSummaryDetail",
                detail,
                BLConstants.Width2,
                summaryY + 10f,
                GetCompactFontSize(detail, 12, 11, 10),
                Color.white,
                TextAnchor.MiddleCenter,
                17);
        }

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

            var glow = BLRender.Sprite($"{key}_Glow", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            glow.transform.localScale *= glowScale;
            glow.GetComponent<SpriteRenderer>().color = glowColor;

            var badge = BLRender.Sprite($"{key}_Badge", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder + 1, parent);
            badge.transform.localScale *= badgeScale;
            badge.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.94f);

            if (characterId >= 0)
            {
                CreateTournamentPortrait($"{key}_Portrait", characterId, x, y + 1f, portraitPixels, sortingOrder + 2, parent);
            }
        }

        private static string FormatWinningPercentage(float value)
        {
            return value.ToString("0.000");
        }

        private static bool MatchIncludesPlayer(BLTournamentMatchResult match, int playerCharacterId)
        {
            return match != null && (match.LeftCharacterId == playerCharacterId || match.RightCharacterId == playerCharacterId);
        }

        private static BLTournamentMatchResult GetPlayerPlayoffMatch(BLTournamentData tournament)
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

        private static string GetMatchupText(BLTournamentMatchResult match, string fallback)
        {
            if (match == null || match.LeftCharacterId < 0 || match.RightCharacterId < 0)
            {
                return fallback;
            }

            return $"{CharacterNameOrTbd(match.LeftCharacterId)} VS {CharacterNameOrTbd(match.RightCharacterId)}";
        }

        private void CreateTournamentAwardsScene(BLTournamentData tournament)
        {
            ResetTournamentAwardsState();

            var placements = BuildTournamentAwardsPlacements(tournament);
            var playerPlacement = placements[Mathf.Clamp(tournament.PlayerPlacement - 1, 0, placements.Length - 1)];

            CreatePanel(
                "AwardsTopShade",
                BLConstants.Width2,
                110f,
                760f,
                132f,
                6,
                new Color(0.01f, 0.04f, 0.05f, 0.48f));
            CreatePanel(
                "AwardsCenterShade",
                BLConstants.Width2,
                258f,
                610f,
                248f,
                6,
                new Color(0.06f, 0.08f, 0.04f, 0.18f));
            CreatePanel(
                "AwardsBottomShade",
                BLConstants.Width2,
                396f,
                760f,
                156f,
                6,
                new Color(0.05f, 0.02f, 0.01f, 0.46f));

            var bannerGroup = CreateTournamentAwardsGroup("AwardsResultBanner");
            var bannerTint = playerPlacement.Placement == 1
                ? new Color(0.33f, 0.94f, 0.5f, 0.96f)
                : playerPlacement.Placement == 2
                    ? new Color(0.22f, 0.82f, 0.96f, 0.96f)
                    : new Color(0.98f, 0.62f, 0.22f, 0.96f);
            CreateFramedPanel(
                "AwardsResultBannerFrame",
                "btn_bg0000",
                BLConstants.Width2,
                TournamentAwardsBannerY,
                248f,
                52f,
                12,
                bannerTint,
                bannerGroup.transform);
            BLRender.Text(
                "AwardsResultBannerLabel",
                GetTournamentAwardsPlayerMessage(tournament),
                BLConstants.Width2,
                TournamentAwardsBannerY + 1f,
                17,
                Color.white,
                TextAnchor.MiddleCenter,
                13,
                bannerGroup.transform,
                BLTextStyle.TournamentAccent);
            RegisterTournamentAwardsAnimation(bannerGroup.transform, new Vector2(0f, 18f), 0.1f, 0.42f, 0.95f);

            var podiumGroup = CreateTournamentAwardsGroup("AwardsPodium");
            CreatePanel(
                "AwardsPodiumGlowCenter",
                TournamentAwardsChampionX,
                284f,
                212f,
                120f,
                8,
                new Color(0.32f, 0.72f, 0.18f, 0.18f),
                podiumGroup.transform);
            CreatePanel(
                "AwardsPodiumGlowLeft",
                TournamentAwardsLeftX,
                304f,
                152f,
                92f,
                8,
                new Color(0.18f, 0.72f, 0.82f, 0.14f),
                podiumGroup.transform);
            CreatePanel(
                "AwardsPodiumGlowRight",
                TournamentAwardsRightX,
                312f,
                152f,
                92f,
                8,
                new Color(0.96f, 0.52f, 0.12f, 0.14f),
                podiumGroup.transform);

            var tribune = BLRender.Sprite(
                "AwardsTribune",
                BLAtlasCache.Instance.Interface,
                "TribuneFinal0000",
                BLConstants.Width2,
                TournamentAwardsPodiumY,
                0.5f,
                0.5f,
                11,
                podiumGroup.transform);
            tribune.transform.localScale *= TournamentAwardsPodiumScale;
            tribune.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.98f);

            CreateTournamentAwardsLaneLabel("AwardsLaneSecond", placements[1], TournamentAwardsLeftX, 378f, false, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneChampion", placements[0], TournamentAwardsChampionX, 362f, true, podiumGroup.transform);
            CreateTournamentAwardsLaneLabel("AwardsLaneThird", placements[2], TournamentAwardsRightX, 388f, false, podiumGroup.transform);
            RegisterTournamentAwardsAnimation(podiumGroup.transform, new Vector2(0f, 24f), 0.14f, 0.52f, 0.97f);

            var badgeChampionGroup = CreateTournamentAwardsGroup("AwardsBadgeChampion");
            CreateTournamentAwardsBadge("AwardsBadgeChampion", placements[0], TournamentAwardsChampionX, TournamentAwardsBadgeY, true, badgeChampionGroup.transform);
            RegisterTournamentAwardsAnimation(badgeChampionGroup.transform, new Vector2(0f, -70f), 0.05f, 0.44f, 0.9f);

            var badgeSecondGroup = CreateTournamentAwardsGroup("AwardsBadgeSecond");
            CreateTournamentAwardsBadge("AwardsBadgeSecond", placements[1], TournamentAwardsChampionX - TournamentAwardsBadgeSideOffsetX, TournamentAwardsBadgeY, false, badgeSecondGroup.transform);
            RegisterTournamentAwardsAnimation(badgeSecondGroup.transform, new Vector2(0f, -78f), 0.12f, 0.44f, 0.9f);

            var badgeThirdGroup = CreateTournamentAwardsGroup("AwardsBadgeThird");
            CreateTournamentAwardsBadge("AwardsBadgeThird", placements[2], TournamentAwardsChampionX + TournamentAwardsBadgeSideOffsetX, TournamentAwardsBadgeY, false, badgeThirdGroup.transform);
            RegisterTournamentAwardsAnimation(badgeThirdGroup.transform, new Vector2(0f, -62f), 0.19f, 0.44f, 0.9f);

            CreateTournamentAwardsCharacterGroup(
                "AwardsSecondPlace",
                placements[1],
                TournamentAwardsLeftX,
                TournamentAwardsLeftY,
                TournamentAwardsSideScale,
                0.12f,
                0.38f,
                0.28f,
                0.28f);
            CreateTournamentAwardsCharacterGroup(
                "AwardsChampionPlace",
                placements[0],
                TournamentAwardsChampionX,
                TournamentAwardsChampionY,
                TournamentAwardsChampionScale,
                0.13f,
                0.48f,
                0.26f,
                0.2f);
            CreateTournamentAwardsCharacterGroup(
                "AwardsThirdPlace",
                placements[2],
                TournamentAwardsRightX,
                TournamentAwardsRightY,
                TournamentAwardsSideScale,
                0.11f,
                0.38f,
                0.24f,
                0.36f);
        }

        private GameObject CreateTournamentAwardsGroup(string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(runtimeRoot, false);
            return group;
        }

        private void CreateTournamentAwardsBadge(string key, TournamentAwardsPlacement placement, float x, float y, bool champion, Transform parent)
        {
            var glow = BLRender.Sprite($"{key}_Glow", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, 15, parent);
            glow.transform.localScale *= champion ? 0.56f : 0.46f;
            glow.GetComponent<SpriteRenderer>().color = placement.GlowColor;

            var badge = BLRender.Sprite($"{key}_Badge", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, 16, parent);
            badge.transform.localScale *= champion ? 0.42f : 0.38f;
            badge.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, champion ? 0.98f : 0.92f);

            CreateTournamentPortrait($"{key}_Portrait", placement.CharacterId, x, y + 1f, champion ? 46f : 40f, 17, parent);

            BLRender.Text(
                $"{key}_Rank",
                GetPlacementShortLabel(placement.Placement),
                x,
                y - (champion ? 44f : 40f),
                champion ? 16 : 14,
                placement.AccentColor,
                TextAnchor.MiddleCenter,
                18,
                parent,
                BLTextStyle.TournamentAccent);
        }

        private void CreateTournamentAwardsLaneLabel(string key, TournamentAwardsPlacement placement, float x, float y, bool champion, Transform parent)
        {
            var panelTint = champion
                ? new Color(0.26f, 0.88f, 0.42f, 0.96f)
                : placement.Placement == 2
                    ? new Color(0.2f, 0.72f, 0.88f, 0.94f)
                    : new Color(0.95f, 0.52f, 0.12f, 0.94f);
            CreateFramedPanel(
                $"{key}_Frame",
                "btn_bg0000",
                x,
                y,
                champion ? 172f : 152f,
                champion ? 38f : 34f,
                13,
                panelTint,
                parent);

            if (champion)
            {
                BLRender.Text(
                    $"{key}_Champion",
                    "CHAMPION",
                    x,
                    y - 24f,
                    13,
                    placement.AccentColor,
                    TextAnchor.MiddleCenter,
                    14,
                    parent,
                    BLTextStyle.TournamentAccent);
            }

            var name = CharacterNameOrTbd(placement.CharacterId);
            BLRender.Text(
                $"{key}_Name",
                name,
                x,
                y + 1f,
                GetCompactFontSize(name, champion ? 14 : 13, 12, 11),
                Color.white,
                TextAnchor.MiddleCenter,
                14,
                parent,
                BLTextStyle.TournamentBody);
        }

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
            var glow = BLRender.Sprite($"{key}_Aura", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y - 18f, 0.5f, 0.5f, 12, group.transform);
            glow.transform.localScale *= placement.Placement == 1 ? 0.82f : 0.66f;
            glow.GetComponent<SpriteRenderer>().color = new Color(placement.GlowColor.r, placement.GlowColor.g, placement.GlowColor.b, glowAlpha);

            var shadow = BLRender.Sprite($"{key}_Shadow", BLAtlasCache.Instance.Interface, "loginSelect0000", x, y + 24f, 0.5f, 0.5f, 13, group.transform);
            shadow.transform.localScale *= shadowScale;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, placement.IsPlayer ? 0.7f : 0.52f);

            var playerRoot = new GameObject($"{key}_Root");
            playerRoot.transform.SetParent(group.transform, false);
            var characterScale = scale * BLPlayersData.GetCharacterPreviewScaleMultiplier(placement.CharacterId);
            BLRender.ApplyPixelTransform(playerRoot.transform, x, y, z, characterScale);

            var armature = BLPlayersData.BuildGameplayArmature($"{key}_Armature");
            if (armature != null)
            {
                armature.transform.SetParent(playerRoot.transform, false);
                armature.transform.localPosition = new Vector3(
                    0f,
                    TournamentAwardsArmatureYOffset + BLPlayersData.GetCharacterPreviewOffsetY(placement.CharacterId) * 0.65f,
                    0f);
                armature.transform.localScale = new Vector3(TournamentAwardsArmatureScale, TournamentAwardsArmatureScale, 1f);
                BLPlayersData.ApplyCharacter(armature, placement.CharacterId);

                if (placement.IsPlayer)
                {
                    awardsCelebrationPlayer = armature;
                    awardsCelebrationCupAnimation = placement.CupAnimation;
                }
            }

            RegisterTournamentAwardsAnimation(group.transform, new Vector2(0f, 16f), landingDelay, 0.44f, 0.96f);
        }

        private TournamentAwardsPlacement[] BuildTournamentAwardsPlacements(BLTournamentData tournament)
        {
            return new[]
            {
                CreateTournamentAwardsPlacement(1, tournament.ChampionCharacterId, tournament.PlayerCharacterId),
                CreateTournamentAwardsPlacement(2, GetMatchLoserCharacterId(tournament.FinalResult), tournament.PlayerCharacterId),
                CreateTournamentAwardsPlacement(3, tournament.ThirdPlaceResult.WinnerCharacterId, tournament.PlayerCharacterId)
            };
        }

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

        private void UpdateTournamentAwardsSequence(float deltaTime)
        {
            if (currentScreen != BLBootstrapScreen.TournamentAwards || runtimeRoot == null)
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

        private static Vector3 PixelOffsetToLocal(Vector2 pixelOffset)
        {
            return new Vector3(pixelOffset.x * BLConstants.UnitsPerPixel, -pixelOffset.y * BLConstants.UnitsPerPixel, 0f);
        }

        private static float EaseOutBack01(float value)
        {
            value = Mathf.Clamp01(value);
            const float overshoot = 1.70158f;
            var shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        private static int GetMatchLoserCharacterId(BLTournamentMatchResult match)
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

        private static string GetTournamentAwardsPlayerMessage(BLTournamentData tournament)
        {
            return $"YOU FINISHED {GetPlacementShortLabel(tournament.PlayerPlacement)} PLACE";
        }

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

        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder)
        {
            return CreateTournamentPortrait(name, characterId, x, y, targetPixels, sortingOrder, runtimeRoot);
        }

        private GameObject CreateTournamentPortrait(string name, int characterId, float x, float y, float targetPixels, int sortingOrder, Transform parent)
        {
            var sprite = BLPlayersData.GetCharacterPortraitSprite(characterId);
            if (sprite == null)
            {
                return null;
            }

            var portrait = new GameObject(name);
            portrait.transform.SetParent(parent, false);
            var renderer = portrait.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            var targetSize = targetPixels * BLPlayersData.GetCharacterPortraitScaleMultiplier(characterId);
            var spritePixels = Mathf.Max(sprite.rect.width, sprite.rect.height);
            var scale = targetSize / Mathf.Max(1f, spritePixels);
            BLRender.ApplyPixelTransform(
                portrait.transform,
                x,
                y + BLPlayersData.GetCharacterPortraitOffsetY(characterId) * scale,
                0f,
                scale);
            return portrait;
        }

        private void CreateTournamentMiniBadge(string key, int characterId, float x, float y, int sortingOrder)
        {
            var glow = BLRender.Sprite($"{key}_Glow", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
            glow.transform.localScale *= 0.28f;
            glow.GetComponent<SpriteRenderer>().color = new Color(1f, 0.77f, 0.32f, 0.32f);

            var badge = BLRender.Sprite($"{key}_Badge", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder + 1, runtimeRoot);
            badge.transform.localScale *= 0.24f;
            badge.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.94f);

            CreateTournamentPortrait($"{key}_Portrait", characterId, x, y + 1f, 28f, sortingOrder + 2);
        }

        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            return CreateFramedPanel(name, frame, x, y, width, height, sortingOrder, tint, runtimeRoot);
        }

        private GameObject CreateFramedPanel(string name, string frame, float x, float y, float width, float height, int sortingOrder, Color tint, Transform parent)
        {
            var panel = BLRender.Sprite(name, BLAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = BLAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                var sourceWidth = Mathf.Max(1f, atlasFrame.SourceW);
                var sourceHeight = Mathf.Max(1f, atlasFrame.SourceH);
                panel.transform.localScale = new Vector3(
                    BLConstants.UnitsPerPixel * width / sourceWidth,
                    BLConstants.UnitsPerPixel * height / sourceHeight,
                    1f);
            }

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

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

        private void CreateElbowConnector(string name, float startX, float startY, float endX, float endY, bool highlighted)
        {
            var midX = (startX + endX) * 0.5f;
            CreateHorizontalConnector($"{name}_H1", startX, midX, startY, highlighted);
            CreateVerticalConnector($"{name}_V", midX, startY, endY, highlighted);
            CreateHorizontalConnector($"{name}_H2", midX, endX, endY, highlighted);
        }

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

        private void ResetTournamentAwardsState()
        {
            awardsAnimatedItems.Clear();
            awardsElapsed = 0f;
            awardsCelebrationTriggered = false;
            awardsCelebrationPlayer = null;
            awardsCelebrationCupAnimation = null;
        }

        private void SeedTwoPlayerSelection()
        {
            var match = BLInventory.Instance.MatchData;
            versusLeftCharacterId = BLPlayersData.SanitizeCharacterId(match.CharacterIds[0]);
            versusRightCharacterId = BLPlayersData.SanitizeCharacterId(match.CharacterIds[1], BLPlayersData.StepCharacterId(versusLeftCharacterId, 1));
        }

        private void ClearRuntime()
        {
            gameCore = null;
            menuButtons.Clear();
            menuMusicButton = null;
            menuHelpButton = null;
            ResetTournamentAwardsState();
            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot.gameObject);
                runtimeRoot = null;
            }
        }

        private static void ToggleBackgroundMusic()
        {
            BLAudio.Instance?.ToggleMusic();
        }

        private static void NoOpAction()
        {
        }

        private static int GetMusicIconIndex()
        {
            return BLAudio.Instance != null && BLAudio.Instance.MusicEnabled ? 0 : 1;
        }

        private static string GetTournamentStatusText(BLTournamentData tournament)
        {
            if (tournament.Completed)
            {
                if (tournament.PlayerPlacement == 1)
                {
                    return "CHAMPION";
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

            if (tournament.CurrentStage == BLTournamentStage.RegularSeason)
            {
                return tournament.RegularSeasonCompleted
                    ? "FINALS READY"
                    : $"ROUND {tournament.CurrentRegularSeasonRoundIndex + 1}";
            }

            if (tournament.CurrentStage == BLTournamentStage.SemiFinal)
            {
                return "FINAL FOUR";
            }

            if (tournament.CurrentStage == BLTournamentStage.ThirdPlace)
            {
                return "3RD PLACE MATCH";
            }

            return "GRAND FINAL";
        }

        private static string CharacterNameOrTbd(int characterId)
        {
            return characterId >= 0 ? BLPlayersData.GetCharacterName(characterId) : "TBD";
        }

        private static int WrapCharacter(int currentCharacterId, int direction)
        {
            return BLPlayersData.StepCharacterId(currentCharacterId, direction);
        }
    }
}
