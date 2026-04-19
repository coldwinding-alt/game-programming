using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLGameBootstrap : MonoBehaviour
    {
        private enum BLBootstrapScreen
        {
            PlayerCount,
            MatchType,
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
        private int tournamentMatchMode;

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
            tournamentMatchMode = Mathf.Clamp(BLInventory.Instance.SelectedTournamentMatchMode, 0, 1);
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
            BeginMenuScreen(true, true, "bg2blue0000");
            AddTitle("SELECT PLAYERS", 118f, 52, new Color32(0xCD, 0xF0, 0x0F, 0xFF));
            AddSubtitle("Start with the player count, then pick tournament or quick match.", 160f);

            var inventory = BLInventory.Instance;
            menuButtons.Add(new BLMenuButton("1 PLAYER", BLConstants.Width2, 270f, 250f, 58f, () =>
            {
                pendingParticipantMode = BLParticipantMode.OnePlayer;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowMatchTypeMenu();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("2 PLAYER", BLConstants.Width2, 335f, 250f, 58f, () =>
            {
                pendingParticipantMode = BLParticipantMode.TwoPlayers;
                inventory.SetParticipantMode(pendingParticipantMode);
                ShowMatchTypeMenu();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("TRAINING", BLConstants.Width2, 400f, 250f, 58f, () =>
            {
                inventory.StartTraining();
                StartGameplay();
            }, runtimeRoot));
        }

        private void ShowMatchTypeMenu()
        {
            currentScreen = BLBootstrapScreen.MatchType;
            BeginMenuScreen(false, false, "bg10000");
            AddTitle("MATCH TYPE", 78f, 58, new Color32(0xCD, 0xF0, 0x0F, 0xFF));
            AddSubtitle(pendingParticipantMode == BLParticipantMode.TwoPlayers ? "2 PLAYERS" : "1 PLAYER", 126f, 24);

            CreatePanel("ModePanel", BLConstants.Width2, 256f, 360f, 240f, 8, new Color(0.05f, 0.08f, 0.1f, 0.82f));
            BLRender.Text(
                "ModeHint",
                pendingParticipantMode == BLParticipantMode.TwoPlayers
                    ? "Tournament runs as co-op 2v2.\nQuick match starts a random head-to-head game."
                    : "Tournament adds character select and standings.\nQuick match jumps straight into a random game.",
                BLConstants.Width2,
                205f,
                18,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: new Color(0f, 0f, 0f, 0.85f),
                outlinePixels: 1f);

            menuButtons.Add(new BLMenuButton("TOURNAMENT", BLConstants.Width2, 270f, 250f, 58f, ShowTournamentSetup, runtimeRoot));
            menuButtons.Add(new BLMenuButton("QUICK MATCH", BLConstants.Width2, 335f, 250f, 58f, () =>
            {
                var inventory = BLInventory.Instance;
                inventory.SetParticipantMode(pendingParticipantMode);
                inventory.StartQuickGame();
                StartGameplay();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton("BACK", BLConstants.Width2, 400f, 220f, 50f, ShowPlayerCountMenu, runtimeRoot));
        }

        private void ShowTournamentSetup()
        {
            currentScreen = BLBootstrapScreen.TournamentSetup;
            BeginMenuScreen(false, false, "bg2blue0000");
            AddTitle("TOURNAMENT", 72f, 62, new Color32(0xCD, 0xF0, 0x0F, 0xFF));
            AddSubtitle(pendingParticipantMode == BLParticipantMode.TwoPlayers ? "2 PLAYERS CO-OP" : "1 PLAYER", 120f, 22);

            BLInventory.Instance.SetParticipantMode(pendingParticipantMode);
            var resolvedMode = pendingParticipantMode == BLParticipantMode.TwoPlayers ? 2 : tournamentMatchMode;
            BLInventory.Instance.SetTournamentSelection(tournamentTeamId, tournamentPlayerIndex, resolvedMode);

            CreatePanel("SelectPanel", 220f, 262f, 260f, 250f, 8, new Color(0.05f, 0.08f, 0.1f, 0.82f));
            CreatePanel("OptionsPanel", 575f, 246f, 210f, 190f, 8, new Color(0.05f, 0.08f, 0.1f, 0.82f));

            BLRender.Text(
                "TeamLabel",
                "TEAM",
                220f,
                150f,
                22,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
            CreateEmblem(tournamentTeamId, 220f, 185f, 0.36f, 22);
            BLRender.Text(
                "TeamName",
                BLPlayersData.GetTeamName(tournamentTeamId),
                220f,
                214f,
                20,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            menuButtons.Add(new BLMenuButton("<", 130f, 184f, 42f, 42f, () =>
            {
                tournamentTeamId = WrapValue(tournamentTeamId - 1, 1, BLPlayersData.TeamsCount);
                ShowTournamentSetup();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", 310f, 184f, 42f, 42f, () =>
            {
                tournamentTeamId = WrapValue(tournamentTeamId + 1, 1, BLPlayersData.TeamsCount);
                ShowTournamentSetup();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton("<", 130f, 278f, 42f, 42f, () =>
            {
                tournamentPlayerIndex = WrapValue(tournamentPlayerIndex - 1, 0, BLPlayersData.TeamSize - 1);
                ShowTournamentSetup();
            }, runtimeRoot));
            menuButtons.Add(new BLMenuButton(">", 310f, 278f, 42f, 42f, () =>
            {
                tournamentPlayerIndex = WrapValue(tournamentPlayerIndex + 1, 0, BLPlayersData.TeamSize - 1);
                ShowTournamentSetup();
            }, runtimeRoot));

            CreatePreviewPlayer(tournamentTeamId, tournamentPlayerIndex, 220f, 303f, 4.4f);
            BLRender.Text(
                "PlayerName",
                BLPlayersData.GetPlayerName(tournamentTeamId, tournamentPlayerIndex),
                220f,
                392f,
                22,
                Color.white,
                TextAnchor.MiddleCenter,
                21,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);

            var teammateIndex = (tournamentPlayerIndex + 1) % BLPlayersData.TeamSize;
            var supportLine = pendingParticipantMode == BLParticipantMode.TwoPlayers
                ? $"TEAMMATE: {BLPlayersData.GetPlayerName(tournamentTeamId, teammateIndex)}"
                : resolvedMode == 1
                    ? $"AI TEAMMATE: {BLPlayersData.GetPlayerName(tournamentTeamId, teammateIndex)}"
                    : "SOLO MATCHUP";
            BLRender.Text(
                "SupportName",
                supportLine,
                220f,
                421f,
                16,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
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
                175f,
                26,
                new Color32(0xCD, 0xF0, 0x0F, 0xFF),
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: Color.black,
                outlinePixels: 1f);
            BLRender.Text(
                "OptionHint",
                "Four teams play three rounds.\nTop two finish as champion and runner-up.",
                575f,
                320f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                20,
                runtimeRoot,
                BLFontKind.Impact2,
                outlineColor: new Color(0f, 0f, 0f, 0.85f),
                outlinePixels: 1f);

            if (pendingParticipantMode == BLParticipantMode.OnePlayer)
            {
                menuButtons.Add(new BLMenuButton(
                    tournamentMatchMode == 0 ? "MODE: 1V1" : "MODE: 2V2",
                    575f,
                    220f,
                    180f,
                    48f,
                    () =>
                    {
                        tournamentMatchMode = tournamentMatchMode == 0 ? 1 : 0;
                        ShowTournamentSetup();
                    },
                    runtimeRoot));
            }
            else
            {
                BLRender.Text(
                    "ModeFixed",
                    "MODE: 2V2 CO-OP",
                    575f,
                    220f,
                    22,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    20,
                    runtimeRoot,
                    BLFontKind.Impact2,
                    outlineColor: Color.black,
                    outlinePixels: 1f);
            }

            menuButtons.Add(new BLMenuButton(BLInventory.Instance.DifficultyLabel, 575f, 270f, 180f, 48f, () =>
            {
                BLInventory.Instance.ToggleDifficulty();
                ShowTournamentSetup();
            }, runtimeRoot));

            menuButtons.Add(new BLMenuButton("BACK", 500f, 418f, 160f, 52f, ShowMatchTypeMenu, runtimeRoot));
            menuButtons.Add(new BLMenuButton("NEXT", 650f, 418f, 160f, 52f, StartTournamentFlow, runtimeRoot));
        }

        private void StartTournamentFlow()
        {
            var inventory = BLInventory.Instance;
            inventory.SetParticipantMode(pendingParticipantMode);
            inventory.SetTournamentSelection(
                tournamentTeamId,
                tournamentPlayerIndex,
                pendingParticipantMode == BLParticipantMode.TwoPlayers ? 2 : tournamentMatchMode);
            inventory.BeginTournament();
            ShowTournamentStandings();
        }

        private void ShowTournamentStandings()
        {
            var inventory = BLInventory.Instance;
            var tournament = inventory.Tournament;
            currentScreen = tournament.Completed ? BLBootstrapScreen.TournamentComplete : BLBootstrapScreen.TournamentStandings;

            BeginMenuScreen(false, false, tournament.Completed ? "bg10000" : "bg2blue0000");
            AddTitle("STANDINGS", 68f, 60, new Color32(0xCD, 0xF0, 0x0F, 0xFF));

            if (tournament.Completed)
            {
                AddSubtitle(
                    $"CHAMPION: {BLPlayersData.GetTeamName(tournament.GetChampionTeamId())}    RUNNER-UP: {BLPlayersData.GetTeamName(tournament.GetRunnerUpTeamId())}",
                    116f,
                    17);
            }
            else
            {
                AddSubtitle(
                    $"ROUND {tournament.CurrentRound + 1}/{tournament.TotalRounds}    NEXT: {BLPlayersData.GetTeamName(tournament.CurrentOpponentTeamId)}",
                    116f,
                    18);
            }

            CreatePanel("HeaderPanel", BLConstants.Width2, 155f, 530f, 34f, 10, new Color(0.08f, 0.14f, 0.18f, 0.92f));
            BLRender.Text("TeamHeader", "TEAM", 230f, 156f, 18, Color.white, TextAnchor.MiddleLeft, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
            BLRender.Text("WinsHeader", "W", 545f, 156f, 18, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
            BLRender.Text("LossHeader", "L", 605f, 156f, 18, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);
            BLRender.Text("PctHeader", "%", 670f, 156f, 18, Color.white, TextAnchor.MiddleCenter, 21, runtimeRoot, BLFontKind.Impact2, outlineColor: Color.black, outlinePixels: 1f);

            var sortedRecords = tournament.GetSortedRecords();
            for (var i = 0; i < sortedRecords.Count; i++)
            {
                var row = sortedRecords[i];
                var y = 205f + i * 58f;
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
                    448f,
                    26,
                    new Color32(0xFF, 0xA3, 0x00, 0xFF),
                    TextAnchor.MiddleCenter,
                    22,
                    runtimeRoot,
                    BLFontKind.Impact,
                    outlineColor: Color.white,
                    outlinePixels: 2f);

                menuButtons.Add(new BLMenuButton("MAIN MENU", BLConstants.Width2, 508f, 200f, 54f, () =>
                {
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
            }
            else
            {
                menuButtons.Add(new BLMenuButton("BACK", 215f, 508f, 170f, 54f, () =>
                {
                    BLInventory.Instance.AbandonTournament();
                    ShowPlayerCountMenu();
                }, runtimeRoot));
                menuButtons.Add(new BLMenuButton("PLAY", 585f, 508f, 170f, 54f, StartGameplay, runtimeRoot));
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
                    BLRender.Image("Logo", logoTexture, BLConstants.Width2, 72f, 0.5f, 0.5f, 20, runtimeRoot);
                }
            }

            if (showControls)
            {
                BLRender.Text(
                    "Controls",
                    BLControlsData.MainMenuControlsText,
                    BLConstants.Width2,
                    448f,
                    15,
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
                BLFontKind.Impact,
                outlineColor: Color.white,
                outlinePixels: 2f,
                shadowColor: new Color(0f, 0f, 0f, 0.35f),
                shadowOffset: new Vector2(2f, 2f));
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
            var shadow = BLRender.Sprite("PreviewShadow", BLAtlasCache.Instance.Interface, "loginSelect0000", x, y + 30f, 0.5f, 0.5f, 18, runtimeRoot);
            shadow.transform.localScale *= 0.6f;
            shadow.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.55f);

            var previewRoot = new GameObject($"Preview_{teamId}_{playerIndex}");
            previewRoot.transform.SetParent(runtimeRoot, false);
            BLRender.ApplyPixelTransform(previewRoot.transform, x, y, 0f, scale);

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
