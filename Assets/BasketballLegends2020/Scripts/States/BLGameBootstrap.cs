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
            TournamentComplete
        }

        private readonly System.Collections.Generic.List<BLMenuButton> menuButtons = new System.Collections.Generic.List<BLMenuButton>();
        private Transform runtimeRoot;
        private BLGameCore gameCore;
        private Camera mainCamera;
        private BLBootstrapScreen currentScreen;
        private BLParticipantMode pendingParticipantMode = BLParticipantMode.OnePlayer;
        private int quickCharacterId;
        private int trainingCharacterId;
        private int tournamentCharacterId;
        private int versusLeftCharacterId;
        private int versusRightCharacterId;

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

            runtimeRoot = new GameObject("BL2020Runtime").transform;
            BLAudio.Create(transform);

            var inventory = BLInventory.Instance;
            quickCharacterId = BLPlayersData.SanitizeCharacterId(inventory.SelectedQuickCharacterId);
            trainingCharacterId = BLPlayersData.SanitizeCharacterId(inventory.SelectedTrainingCharacterId, quickCharacterId);
            tournamentCharacterId = BLPlayersData.SanitizeCharacterId(inventory.SelectedTournamentCharacterId, quickCharacterId);
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

            for (var i = 0; i < menuButtons.Count; i++)
            {
                var screenRoot = runtimeRoot;
                menuButtons[i].Update(mainCamera);
                if (screenRoot != runtimeRoot)
                {
                    break;
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
            menuButtons.Add(new BLMenuButton(BLInventory.Instance.DifficultyLabel, 575f, 270f, 188f, 46f, () =>
            {
                BLInventory.Instance.ToggleDifficulty();
                ShowSinglePlayerSetup();
            }, runtimeRoot));

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

            CreatePanel("TrainingPanel", BLConstants.Width2, 280f, 280f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreateCharacterSelector(
                "Training",
                "CHARACTER",
                BLConstants.Width2,
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
                3.9f,
                398f);

            menuButtons.Add(new BLMenuButton("BACK", 312f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("PLAY", 488f, 452f, 150f, 42f, StartTrainingFlow, runtimeRoot));
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
                outlinePixels: 1.8f);

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
                "FORMAT: 4 PLAYER KO",
                575f,
                216f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            menuButtons.Add(new BLMenuButton(BLInventory.Instance.DifficultyLabel, 575f, 270f, 188f, 46f, () =>
            {
                BLInventory.Instance.ToggleDifficulty();
                ShowTournamentSetup();
            }, runtimeRoot));

            var enoughCharacters = BLPlayersData.GetActiveCharacterIds().Length >= 4;
            if (!enoughCharacters)
            {
                AddSubtitle("NEED 4 ENABLED CHARACTERS", 408f, 18);
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
            inventory.StartQuickGame();
            StartGameplay();
        }

        private void StartTrainingFlow()
        {
            var inventory = BLInventory.Instance;
            inventory.SetParticipantMode(BLParticipantMode.Training);
            inventory.SetTrainingSelection(trainingCharacterId);
            inventory.StartTraining();
            StartGameplay();
        }

        private void StartTournamentFlow()
        {
            var inventory = BLInventory.Instance;
            inventory.SetParticipantMode(BLParticipantMode.OnePlayer);
            inventory.SetTournamentSelection(tournamentCharacterId);
            if (!inventory.BeginTournament())
            {
                ShowTournamentSetup();
                return;
            }

            ShowTournamentBracket();
        }

        private void StartTwoPlayerMatch()
        {
            BLInventory.Instance.StartTwoPlayerVersus(versusLeftCharacterId, versusRightCharacterId);
            StartGameplay();
        }

        private void ShowTournamentBracket()
        {
            var inventory = BLInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? BLBootstrapScreen.TournamentComplete : BLBootstrapScreen.TournamentBracket;

            BeginMenuScreen(false, false, tournament.Completed ? "bg10000" : "bg2blue0000");
            AddTitle("TOURNAMENT", 52f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));
            AddSubtitle(GetTournamentStatusText(tournament), 90f, 20);

            CreateBracketMatchCard("SemiFinal1", 240f, 182f, "SEMIFINAL 1", tournament.SemiFinalResults[0], !tournament.Completed && tournament.CurrentStage == BLTournamentStage.SemiFinal);
            CreateBracketMatchCard("SemiFinal2", 240f, 304f, "SEMIFINAL 2", tournament.SemiFinalResults[1], false);
            CreateBracketMatchCard("Final", 558f, 244f, "FINAL", tournament.FinalResult, !tournament.Completed && tournament.CurrentStage == BLTournamentStage.Final);

            if (tournament.Completed)
            {
                CreatePanel("ChampionPanel", BLConstants.Width2, 404f, 410f, 72f, 10, new Color(0.95f, 0.56f, 0.12f, 0.92f));
                BLRender.Text(
                    "ChampionLabel",
                    $"CHAMPION: {CharacterNameOrTbd(tournament.ChampionCharacterId)}",
                    BLConstants.Width2,
                    392f,
                    24,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    21,
                    runtimeRoot,
                    BLFontKind.CfCrackBold,
                    outlineColor: Color.black,
                    outlinePixels: 1.2f);
                BLRender.Text(
                    "PlacementLabel",
                    $"YOU FINISHED #{tournament.PlayerPlacement}",
                    BLConstants.Width2,
                    420f,
                    20,
                    new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                    TextAnchor.MiddleCenter,
                    21,
                    runtimeRoot,
                    BLFontKind.Impact2,
                    outlineColor: Color.black,
                    outlinePixels: 1f);

                menuButtons.Add(new BLMenuButton("MAIN MENU", BLConstants.Width2, 452f, 200f, 42f, () =>
                {
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
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
                var logoTexture = Resources.Load<Texture2D>("BL2020/Images/logo");
                if (logoTexture != null)
                {
                    var logo = BLRender.Image("Logo", logoTexture, BLConstants.Width2, 68f, 0.5f, 0.5f, 20, runtimeRoot);
                    logo.transform.localScale *= 0.78f;
                }
            }

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
                    BLFontKind.Impact2,
                    outlineColor: new Color(0f, 0f, 0f, 0.9f),
                    outlinePixels: 1f,
                    shadowColor: new Color(0f, 0f, 0f, 0.2f),
                    shadowOffset: new Vector2(1f, 1f));
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
                BLFontKind.CfCrackBold,
                outlineColor: new Color(0f, 0f, 0f, 0.92f),
                outlinePixels: 1.6f,
                shadowColor: new Color(0f, 0f, 0f, 0.28f),
                shadowOffset: new Vector2(1.5f, 1.5f));
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
                BLFontKind.Impact2,
                outlineColor: new Color(0f, 0f, 0f, 0.85f),
                outlinePixels: 1f);
        }

        private GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Color tint)
        {
            var panel = BLRender.Sprite(name, BLAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot);
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
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
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
                126f,
                header.Length <= 2 ? 26 : 18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            menuButtons.Add(new BLMenuButton("<", centerX - 88f, 252f, 42f, 42f, previousCharacterAction, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", centerX + 88f, 252f, 42f, 42f, nextCharacterAction, runtimeRoot));

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
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
        }

        private void CreatePreviewPlayer(int characterId, float x, float y, float scale)
        {
            var previewScale = scale * 0.7f;
            var shadow = BLRender.Sprite("PreviewShadow", BLAtlasCache.Instance.Interface, "loginSelect0000", x, y + 30f, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= 0.5f;
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
            armature.transform.localPosition = new Vector3(0f, -18f, 0f);
            armature.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            BLPlayersData.ApplyCharacter(armature, characterId);
        }

        private void CreateBracketMatchCard(string key, float x, float y, string title, BLTournamentMatchResult match, bool current)
        {
            var tint = current
                ? new Color(0.95f, 0.56f, 0.12f, 0.92f)
                : match.Completed
                    ? new Color(0.2f, 0.18f, 0.45f, 0.9f)
                    : new Color(0.08f, 0.12f, 0.18f, 0.88f);

            CreatePanel($"{key}_Panel", x, y, 254f, 94f, 10, tint);
            BLRender.Text(
                $"{key}_Title",
                title,
                x,
                y - 28f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            var leftName = CharacterNameOrTbd(match.LeftCharacterId);
            var rightName = CharacterNameOrTbd(match.RightCharacterId);
            BLRender.Text(
                $"{key}_Left",
                leftName,
                x,
                y - 6f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
            BLRender.Text(
                $"{key}_Center",
                match.Completed ? $"{match.LeftScore} - {match.RightScore}" : "VS",
                x,
                y + 18f,
                20,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.CfCrackBold,
                outlineColor: Color.black,
                outlinePixels: 1f);
            BLRender.Text(
                $"{key}_Right",
                rightName,
                x,
                y + 40f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            if (match.Completed)
            {
                BLRender.Text(
                    $"{key}_Winner",
                    $"WINNER: {CharacterNameOrTbd(match.WinnerCharacterId)}",
                    x,
                    y + 62f,
                    14,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    21,
                    runtimeRoot,
                    BLFontKind.Impact2,
                    outlineColor: Color.black,
                    outlinePixels: 1f);
            }
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
            if (runtimeRoot != null)
            {
                Destroy(runtimeRoot.gameObject);
                runtimeRoot = null;
            }
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

                return "SEMIFINALIST";
            }

            return tournament.CurrentStage == BLTournamentStage.Final ? "FINAL" : "SEMIFINAL";
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
