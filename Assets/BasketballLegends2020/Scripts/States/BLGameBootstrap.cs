using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLGameBootstrap : MonoBehaviour
    {
        private enum BLBootstrapScreen
        {
            PlayerCount,
            MatchType,
            TwoPlayerSetup,
            TournamentSetup,
            TournamentStandings,
            TournamentComplete
        }

        private readonly System.Collections.Generic.List<BLMenuButton> menuButtons = new System.Collections.Generic.List<BLMenuButton>();
        private Transform runtimeRoot;
        private BLGameCore gameCore;
        private Camera mainCamera;
        private BLBootstrapScreen currentScreen;
        private BLParticipantMode pendingParticipantMode = BLParticipantMode.OnePlayer;
        private int tournamentTeamId = 1;
        private int tournamentPlayerIndex;
        private int versusLeftTeamId = 1;
        private int versusLeftPlayerIndex;
        private int versusRightTeamId = 2;
        private int versusRightPlayerIndex;

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
            tournamentTeamId = Mathf.Clamp(BLInventory.Instance.SelectedTournamentTeamId, 1, BLPlayersData.TeamsCount);
            tournamentPlayerIndex = Mathf.Clamp(BLInventory.Instance.SelectedTournamentPlayerIndex, 0, BLPlayersData.TeamSize - 1);
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
                    ShowTournamentStandings();
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
                case BLBootstrapScreen.TwoPlayerSetup:
                    ShowPlayerCountMenu();
                    break;
                case BLBootstrapScreen.TournamentSetup:
                    ShowMatchTypeMenu();
                    break;
                case BLBootstrapScreen.TournamentStandings:
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

            menuButtons.Add(new BLMenuButton("TRAINING", BLConstants.Width2, 394f, 228f, 52f, () =>
            {
                inventory.StartTraining();
                StartGameplay();
            }, runtimeRoot));
        }

        private void ShowMatchTypeMenu()
        {
            currentScreen = BLBootstrapScreen.MatchType;
            pendingParticipantMode = BLParticipantMode.OnePlayer;
            BeginMenuScreen(true, false, "bg10000");
            AddTitle("MATCH TYPE", 136f, 34, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            CreatePanel("ModePanel", BLConstants.Width2, 326f, 320f, 254f, 8, new Color(0.05f, 0.08f, 0.1f, 0.74f));

            menuButtons.Add(new BLMenuButton("TOURNAMENT", BLConstants.Width2, 312f, 246f, 52f, ShowTournamentSetup, runtimeRoot));
            menuButtons.Add(new BLMenuButton("QUICK MATCH", BLConstants.Width2, 372f, 246f, 52f, () =>
            {
                var inventory = BLInventory.Instance;
                inventory.SetParticipantMode(pendingParticipantMode);
                inventory.StartQuickGame();
                StartGameplay();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton("BACK", BLConstants.Width2, 432f, 200f, 46f, ShowPlayerCountMenu, runtimeRoot));
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

            CreateVersusSelector(
                "P1",
                214f,
                versusLeftTeamId,
                versusLeftPlayerIndex,
                () =>
                {
                    versusLeftTeamId = WrapValue(versusLeftTeamId - 1, 1, BLPlayersData.TeamsCount);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusLeftTeamId = WrapValue(versusLeftTeamId + 1, 1, BLPlayersData.TeamsCount);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusLeftPlayerIndex = WrapValue(versusLeftPlayerIndex - 1, 0, BLPlayersData.TeamSize - 1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusLeftPlayerIndex = WrapValue(versusLeftPlayerIndex + 1, 0, BLPlayersData.TeamSize - 1);
                    ShowTwoPlayerSetup();
                });

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

            CreateVersusSelector(
                "P2",
                586f,
                versusRightTeamId,
                versusRightPlayerIndex,
                () =>
                {
                    versusRightTeamId = WrapValue(versusRightTeamId - 1, 1, BLPlayersData.TeamsCount);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusRightTeamId = WrapValue(versusRightTeamId + 1, 1, BLPlayersData.TeamsCount);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusRightPlayerIndex = WrapValue(versusRightPlayerIndex - 1, 0, BLPlayersData.TeamSize - 1);
                    ShowTwoPlayerSetup();
                },
                () =>
                {
                    versusRightPlayerIndex = WrapValue(versusRightPlayerIndex + 1, 0, BLPlayersData.TeamSize - 1);
                    ShowTwoPlayerSetup();
                });

            menuButtons.Add(new BLMenuButton("BACK", 212f, 452f, 150f, 42f, ShowPlayerCountMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("PLAY", 588f, 452f, 150f, 42f, StartTwoPlayerMatch, runtimeRoot));
        }

        private void ShowTournamentSetup()
        {
            currentScreen = BLBootstrapScreen.TournamentSetup;
            BeginMenuScreen(false, false, "bg2blue0000");
            AddTitle("TOURNAMENT", 54f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            pendingParticipantMode = BLParticipantMode.OnePlayer;
            BLInventory.Instance.SetParticipantMode(pendingParticipantMode);
            BLInventory.Instance.SetTournamentSelection(tournamentTeamId, tournamentPlayerIndex, 0);

            CreatePanel("SelectPanel", 220f, 280f, 260f, 278f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));
            CreatePanel("OptionsPanel", 575f, 278f, 228f, 214f, 8, new Color(0.05f, 0.08f, 0.1f, 0.8f));

            BLRender.Text(
                "TeamLabel",
                "TEAM",
                220f,
                130f,
                18,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
            CreateEmblem(tournamentTeamId, 220f, 164f, 0.32f, 22);
            BLRender.Text(
                "TeamName",
                BLPlayersData.GetTeamName(tournamentTeamId),
                220f,
                190f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            menuButtons.Add(new BLMenuButton("<", 132f, 164f, 42f, 42f, () =>
            {
                tournamentTeamId = WrapValue(tournamentTeamId - 1, 1, BLPlayersData.TeamsCount);
                ShowTournamentSetup();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", 308f, 164f, 42f, 42f, () =>
            {
                tournamentTeamId = WrapValue(tournamentTeamId + 1, 1, BLPlayersData.TeamsCount);
                ShowTournamentSetup();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton("<", 132f, 252f, 42f, 42f, () =>
            {
                tournamentPlayerIndex = WrapValue(tournamentPlayerIndex - 1, 0, BLPlayersData.TeamSize - 1);
                ShowTournamentSetup();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", 308f, 252f, 42f, 42f, () =>
            {
                tournamentPlayerIndex = WrapValue(tournamentPlayerIndex + 1, 0, BLPlayersData.TeamSize - 1);
                ShowTournamentSetup();
            }, runtimeRoot));

            CreatePreviewPlayer(tournamentTeamId, tournamentPlayerIndex, 220f, 306f, 3.85f);
            BLRender.Text(
                "PlayerName",
                BLPlayersData.GetPlayerName(tournamentTeamId, tournamentPlayerIndex),
                220f,
                398f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            BLRender.Text(
                "OptionTitle",
                "SETTINGS",
                575f,
                160f,
                20,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
            BLRender.Text(
                "ModeFixed",
                "MODE: 1V1",
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

            menuButtons.Add(new BLMenuButton("BACK", 488f, 452f, 150f, 42f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("NEXT", 660f, 452f, 150f, 42f, StartTournamentFlow, runtimeRoot));
        }

        private void StartTournamentFlow()
        {
            var inventory = BLInventory.Instance;
            pendingParticipantMode = BLParticipantMode.OnePlayer;
            inventory.SetParticipantMode(pendingParticipantMode);
            inventory.SetTournamentSelection(tournamentTeamId, tournamentPlayerIndex, 0);
            inventory.BeginTournament();
            ShowTournamentStandings();
        }

        private void StartTwoPlayerMatch()
        {
            BLInventory.Instance.StartTwoPlayerVersus(
                versusLeftTeamId,
                versusLeftPlayerIndex,
                versusRightTeamId,
                versusRightPlayerIndex);
            StartGameplay();
        }

        private void ShowTournamentStandings()
        {
            var inventory = BLInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? BLBootstrapScreen.TournamentComplete : BLBootstrapScreen.TournamentStandings;

            BeginMenuScreen(false, false, tournament.Completed ? "bg10000" : "bg2blue0000");
            AddTitle("STANDINGS", 52f, 36, new Color32(0xD7, 0xF2, 0x4A, 0xFF));

            CreatePanel("HeaderPanel", BLConstants.Width2, 126f, 530f, 34f, 10, new Color(0.08f, 0.14f, 0.18f, 0.92f));
            BLRender.Text("TeamHeader", "TEAM", 230f, 127f, 16, Color.white, TextAnchor.MiddleLeft, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
            BLRender.Text("WinsHeader", "W", 545f, 127f, 16, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
            BLRender.Text("LossHeader", "L", 605f, 127f, 16, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
            BLRender.Text("PctHeader", "%", 670f, 127f, 16, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);

            var sortedRecords = tournament.GetSortedRecords();
            for (var i = 0; i < sortedRecords.Count; i++)
            {
                var row = sortedRecords[i];
                var y = 171f + i * 55f;
                var tint = row.TeamId == tournament.PlayerTeamId
                    ? new Color(0.95f, 0.56f, 0.12f, 0.92f)
                    : new Color(0.2f, 0.18f, 0.45f, 0.88f);
                CreatePanel($"StandingRow_{i}", BLConstants.Width2, y, 530f, 46f, 10, tint);
                BLRender.Text($"Rank_{i}", (i + 1).ToString(), 162f, y + 2f, 20, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.CfCrackBold, outlineColor: Color.black, outlinePixels: 1f);
                CreateEmblem(row.TeamId, 222f, y, 0.18f, 22);
                BLRender.Text($"Team_{i}", BLPlayersData.GetTeamName(row.TeamId), 270f, y + 2f, 18, Color.white, TextAnchor.MiddleLeft, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
                BLRender.Text($"Wins_{i}", row.Wins.ToString(), 545f, y + 2f, 20, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.CfCrackBold, outlineColor: Color.black, outlinePixels: 1f);
                BLRender.Text($"Losses_{i}", row.Losses.ToString(), 605f, y + 2f, 20, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.CfCrackBold, outlineColor: Color.black, outlinePixels: 1f);
                BLRender.Text($"Pct_{i}", Mathf.RoundToInt(row.WinPercent * 100f).ToString(), 670f, y + 2f, 20, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.CfCrackBold, outlineColor: Color.black, outlinePixels: 1f);
            }

            if (tournament.Completed)
            {
                var placement = tournament.GetPlayerPlacement();
                BLRender.Text(
                    "PlacementSummary",
                    $"YOU FINISHED #{placement}",
                    BLConstants.Width2,
                    410f,
                    24,
                    new Color32(0xFF, 0xA3, 0x00, 0xFF),
                    TextAnchor.MiddleCenter,
                    22,
                    runtimeRoot,
                    BLFontKind.Impact,
                    outlineColor: Color.white,
                    outlinePixels: 2f);

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

        private void CreateEmblem(int teamId, float x, float y, float scale, int sortingOrder)
        {
            BLRender.Sprite($"EmblemBg_{teamId}_{x}_{y}", BLAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder - 1, runtimeRoot).transform.localScale *= scale + 0.05f;
            var frame = $"Emblems00{(teamId - 1 < 10 ? "0" : string.Empty)}{teamId - 1}";
            BLRender.Sprite($"Emblem_{teamId}_{x}_{y}", BLAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, runtimeRoot).transform.localScale *= scale;
        }

        private void CreatePreviewPlayer(int teamId, int playerIndex, float x, float y, float scale)
        {
            var previewScale = scale * 0.7f;
            var shadow = BLRender.Sprite("PreviewShadow", BLAtlasCache.Instance.Interface, "loginSelect0000", x, y + 30f, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= 0.5f;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.55f);

            var previewRoot = new GameObject($"Preview_{teamId}_{playerIndex}");
            previewRoot.transform.SetParent(runtimeRoot, false);
            BLRender.ApplyPixelTransform(previewRoot.transform, x, y, 0f, previewScale);

            var armature = BLPlayersData.BuildGameplayArmature($"PreviewArmature_{teamId}_{playerIndex}");
            if (armature == null)
            {
                return;
            }

            armature.transform.SetParent(previewRoot.transform, false);
            armature.transform.localPosition = new Vector3(0f, -18f, 0f);
            armature.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            BLPlayersData.SwitchPlayer(
                armature,
                BLPlayersData.SkinIndex(teamId, playerIndex),
                2 * (teamId - 1));
        }

        private void CreateVersusSelector(
            string label,
            float centerX,
            int teamId,
            int playerIndex,
            System.Action previousTeamAction,
            System.Action nextTeamAction,
            System.Action previousPlayerAction,
            System.Action nextPlayerAction)
        {
            BLRender.Text(
                $"{label}_Header",
                label,
                centerX,
                126f,
                26,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
            CreateEmblem(teamId, centerX, 164f, 0.28f, 22);
            BLRender.Text(
                $"{label}_TeamName",
                BLPlayersData.GetTeamName(teamId),
                centerX,
                192f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            menuButtons.Add(new BLMenuButton("<", centerX - 88f, 164f, 42f, 42f, previousTeamAction, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", centerX + 88f, 164f, 42f, 42f, nextTeamAction, runtimeRoot));
            menuButtons.Add(new BLMenuButton("<", centerX - 88f, 252f, 42f, 42f, previousPlayerAction, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", centerX + 88f, 252f, 42f, 42f, nextPlayerAction, runtimeRoot));

            CreatePreviewPlayer(teamId, playerIndex, centerX, 308f, 3.8f);
            BLRender.Text(
                $"{label}_PlayerName",
                BLPlayersData.GetPlayerName(teamId, playerIndex),
                centerX,
                420f,
                17,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
        }

        private void SeedTwoPlayerSelection()
        {
            var match = BLInventory.Instance.MatchData;
            versusLeftTeamId = Mathf.Clamp(match.Teams[0] > 0 ? match.Teams[0] : tournamentTeamId, 1, BLPlayersData.TeamsCount);
            versusRightTeamId = Mathf.Clamp(match.Teams[1] > 0 ? match.Teams[1] : WrapValue(versusLeftTeamId + 1, 1, BLPlayersData.TeamsCount), 1, BLPlayersData.TeamsCount);
            versusLeftPlayerIndex = ReadPlayerIndex(match.Players[0]);
            versusRightPlayerIndex = ReadPlayerIndex(match.Players[1]);
        }

        private static int ReadPlayerIndex(int[] players)
        {
            return players != null && players.Length > 0
                ? Mathf.Clamp(players[0], 0, BLPlayersData.TeamSize - 1)
                : 0;
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

        private static int WrapValue(int value, int min, int max)
        {
            if (value < min)
            {
                return max;
            }

            if (value > max)
            {
                return min;
            }

            return value;
        }
    }
}
