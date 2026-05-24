using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushSceneMigrationTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("rimrush/Stage 1/Prepare Main Scene Hosts")]
        public static void PrepareMainSceneHosts()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyByNames(
                "rimrushBootstrap",
                "rimrushSceneRoot",
                "rimrushRuntime",
                "MenuShell",
                "GameplayRoot",
                "HudSceneRoot",
                "PersistentRoot",
                "OverlayRoot",
                "rimrushAudio");

            var mainCamera = EnsureMainCamera();
            var bootstrapObject = new GameObject("rimrushBootstrap");
            var bootstrap = bootstrapObject.AddComponent<rimrushGameBootstrap>();
            var presenter = bootstrapObject.AddComponent<rimrushFixedResolutionPresenter>();
            var sceneBindings = bootstrapObject.AddComponent<rimrushSceneBindings>();

            var persistentRoot = new GameObject("PersistentRoot").transform;
            persistentRoot.SetParent(bootstrapObject.transform, false);
            var overlayRoot = new GameObject("OverlayRoot").transform;
            overlayRoot.SetParent(persistentRoot, false);

            var audio = CreateAudioHost(persistentRoot);
            rimrushAudio.Create(persistentRoot);

            Assign(sceneBindings, "mainCamera", mainCamera);
            Assign(sceneBindings, "presenter", presenter);
            Assign(sceneBindings, "audioComponent", audio);
            Assign(sceneBindings, "persistentRoot", persistentRoot);
            Assign(sceneBindings, "overlayRoot", overlayRoot);
            Assign(sceneBindings, "menuShell", null);
            Assign(sceneBindings, "gameplayBindings", null);
            Assign(bootstrap, "sceneBindings", sceneBindings);

            EditorUtility.SetDirty(mainCamera.gameObject);
            EditorUtility.SetDirty(bootstrapObject);
            EditorUtility.SetDirty(sceneBindings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("rimrush/Experimental/Prototype Full Scene Views (Do Not Use)")]
        public static void MigrateMainScene()
        {
            rimrushPlayersData.SetupPlayers();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyByNames(
                "rimrushBootstrap",
                "rimrushSceneRoot",
                "rimrushRuntime",
                "MenuShell",
                "GameplayRoot",
                "HudSceneRoot",
                "PersistentRoot",
                "OverlayRoot",
                "rimrushAudio");

            var mainCamera = EnsureMainCamera();
            var bootstrapObject = new GameObject("rimrushBootstrap");
            var bootstrap = bootstrapObject.AddComponent<rimrushGameBootstrap>();
            var presenter = bootstrapObject.AddComponent<rimrushFixedResolutionPresenter>();
            var sceneBindings = bootstrapObject.AddComponent<rimrushSceneBindings>();

            var persistentRoot = new GameObject("PersistentRoot").transform;
            persistentRoot.SetParent(bootstrapObject.transform, false);
            var overlayRoot = new GameObject("OverlayRoot").transform;
            overlayRoot.SetParent(persistentRoot, false);

            var audio = CreateAudioHost(persistentRoot);
            rimrushAudio.Create(persistentRoot);

            var menuShell = CreateMenuShell(persistentRoot);
            menuShell.gameObject.SetActive(false);
            var gameplayBindings = CreateGameplayBindings(persistentRoot);
            gameplayBindings.gameObject.SetActive(false);

            Assign(sceneBindings, "mainCamera", mainCamera);
            Assign(sceneBindings, "presenter", presenter);
            Assign(sceneBindings, "audioComponent", audio);
            Assign(sceneBindings, "persistentRoot", persistentRoot);
            Assign(sceneBindings, "overlayRoot", overlayRoot);
            Assign(sceneBindings, "menuShell", menuShell);
            Assign(sceneBindings, "gameplayBindings", gameplayBindings);
            Assign(bootstrap, "sceneBindings", sceneBindings);

            EditorUtility.SetDirty(mainCamera.gameObject);
            EditorUtility.SetDirty(bootstrapObject);
            EditorUtility.SetDirty(sceneBindings);
            EditorUtility.SetDirty(gameplayBindings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("rimrush/Stage 2/Prepare Menu and HUD Scene Views")]
        public static void PrepareMenuAndHudSceneViews()
        {
            rimrushPlayersData.SetupPlayers();
            PrepareMainSceneHosts();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = GameObject.Find("rimrushBootstrap")?.GetComponent<rimrushGameBootstrap>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("Could not find rimrushBootstrap after preparing Stage 1 hosts.");
            }

            var sceneBindings = bootstrap.GetComponent<rimrushSceneBindings>();
            if (sceneBindings == null)
            {
                throw new InvalidOperationException("Could not find rimrushSceneBindings on rimrushBootstrap.");
            }

            sceneBindings.ResolveMissingReferences();
            var overlayRoot = sceneBindings.OverlayRoot;
            if (overlayRoot == null)
            {
                throw new InvalidOperationException("Scene bindings did not expose OverlayRoot.");
            }

            var menuShell = CreateMenuShell(overlayRoot);
            var hudView = CreateHudSceneView(overlayRoot);
            var menuPreview = CreateOrGetComponent<rimrushMenuAuthoringPreview>(menuShell.gameObject);
            var hudPreview = CreateOrGetComponent<rimrushHudAuthoringPreview>(hudView.gameObject);

            Assign(sceneBindings, "menuShell", menuShell);
            Assign(sceneBindings, "hudView", hudView);
            Assign(sceneBindings, "gameplayBindings", null);
            Assign(bootstrap, "sceneBindings", sceneBindings);
            Assign(bootstrap, "preferSceneMenuShell", false);
            Assign(bootstrap, "preferSceneHudView", false);
            Assign(bootstrap, "preferSceneGameplayBindings", false);

            ResetAuthoringInventoryState();
            Invoke(bootstrap, "Awake");
            BuildMenuAuthoringPages(bootstrap, menuShell);

            Assign(menuPreview, "menuShell", menuShell);
            Assign(hudPreview, "hudView", hudView);

            if (menuShell.DynamicContentRoot != null)
            {
                menuShell.DynamicContentRoot.gameObject.SetActive(false);
            }

            menuShell.gameObject.SetActive(true);
            hudView.gameObject.SetActive(true);
            overlayRoot.gameObject.SetActive(true);
            sceneBindings.ResolveMissingReferences();

            EditorUtility.SetDirty(bootstrap.gameObject);
            EditorUtility.SetDirty(sceneBindings);
            EditorUtility.SetDirty(menuShell);
            EditorUtility.SetDirty(hudView);
            EditorUtility.SetDirty(menuPreview);
            EditorUtility.SetDirty(hudPreview);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("rimrush/Stage 3/Prepare Gameplay Scene Views")]
        public static void PrepareGameplaySceneViews()
        {
            rimrushPlayersData.SetupPlayers();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = GameObject.Find("rimrushBootstrap")?.GetComponent<rimrushGameBootstrap>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("Could not find rimrushBootstrap in Main.unity.");
            }

            var sceneBindings = bootstrap.GetComponent<rimrushSceneBindings>();
            if (sceneBindings == null)
            {
                throw new InvalidOperationException("Could not find rimrushSceneBindings on rimrushBootstrap.");
            }

            sceneBindings.ResolveMissingReferences();
            if (sceneBindings.PersistentRoot == null)
            {
                throw new InvalidOperationException("Scene bindings did not expose PersistentRoot.");
            }

            DestroyExistingGameplayRoot(sceneBindings);

            var gameplayBindings = CreateStage3GameplayBindings(sceneBindings.PersistentRoot, sceneBindings.HudView);
            gameplayBindings.Root.SetSiblingIndex(0);

            Assign(sceneBindings, "gameplayBindings", gameplayBindings);
            Assign(bootstrap, "sceneBindings", sceneBindings);
            Assign(bootstrap, "preferSceneGameplayBindings", false);

            if (sceneBindings.HudView != null)
            {
                var hudPreview = sceneBindings.HudView.GetComponent<rimrushHudAuthoringPreview>();
                if (hudPreview != null)
                {
                    Assign(hudPreview, "previewState", rimrushHudPreviewState.Live);
                }
            }

            var authoringMode = CreateOrGetComponent<rimrushSceneAuthoringMode>(bootstrap.gameObject);
            Assign(authoringMode, "sceneBindings", sceneBindings);
            Assign(authoringMode, "focus", rimrushSceneAuthoringFocus.Gameplay);
            Assign(authoringMode, "showHudInGameplay", true);
            authoringMode.ApplyEditorState();

            EditorUtility.SetDirty(bootstrap.gameObject);
            EditorUtility.SetDirty(sceneBindings);
            EditorUtility.SetDirty(gameplayBindings);
            EditorUtility.SetDirty(authoringMode);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("rimrush/Stage 4/Prepare Player Scene Views")]
        public static void PreparePlayerSceneViews()
        {
            rimrushPlayersData.SetupPlayers();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = GameObject.Find("rimrushBootstrap")?.GetComponent<rimrushGameBootstrap>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException("Could not find rimrushBootstrap in Main.unity.");
            }

            var sceneBindings = bootstrap.GetComponent<rimrushSceneBindings>();
            if (sceneBindings == null)
            {
                throw new InvalidOperationException("Could not find rimrushSceneBindings on rimrushBootstrap.");
            }

            sceneBindings.ResolveMissingReferences();
            if (sceneBindings.PersistentRoot == null)
            {
                throw new InvalidOperationException("Scene bindings did not expose PersistentRoot.");
            }

            DestroyExistingGameplayRoot(sceneBindings);

            var gameplayBindings = CreateStage4GameplayBindings(sceneBindings.PersistentRoot, sceneBindings.HudView);
            gameplayBindings.Root.SetSiblingIndex(0);

            Assign(sceneBindings, "gameplayBindings", gameplayBindings);
            Assign(bootstrap, "sceneBindings", sceneBindings);
            Assign(bootstrap, "preferSceneGameplayBindings", false);

            if (sceneBindings.HudView != null)
            {
                var hudPreview = sceneBindings.HudView.GetComponent<rimrushHudAuthoringPreview>();
                if (hudPreview != null)
                {
                    Assign(hudPreview, "previewState", rimrushHudPreviewState.Live);
                }
            }

            var authoringMode = CreateOrGetComponent<rimrushSceneAuthoringMode>(bootstrap.gameObject);
            Assign(authoringMode, "sceneBindings", sceneBindings);
            Assign(authoringMode, "focus", rimrushSceneAuthoringFocus.Gameplay);
            Assign(authoringMode, "showHudInGameplay", true);
            authoringMode.ApplyEditorState();

            EditorUtility.SetDirty(bootstrap.gameObject);
            EditorUtility.SetDirty(sceneBindings);
            EditorUtility.SetDirty(gameplayBindings);
            EditorUtility.SetDirty(authoringMode);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void PrepareGameplaySceneViewsAndRunSmoke()
        {
            PrepareGameplaySceneViews();
            rimrushSmokeTest.Run();
        }

        public static void PreparePlayerSceneViewsAndRunSmoke()
        {
            PreparePlayerSceneViews();
            rimrushSmokeTest.Run();
        }

        public static void DumpGameplaySceneBindings()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = GameObject.Find("rimrushBootstrap")?.GetComponent<rimrushGameBootstrap>();
            var sceneBindings = bootstrap != null ? bootstrap.GetComponent<rimrushSceneBindings>() : null;
            sceneBindings?.ResolveMissingReferences();

            var gameplayBindings = sceneBindings != null ? sceneBindings.GameplayBindings : null;
            Debug.Log($"GameplayBindings scene: {scene.path}");
            Debug.Log($"GameplayBindings component: {DescribeUnityObject(gameplayBindings)}");
            Debug.Log($"SceneBindings serialized gameplayBindings: {DescribeSerializedReference(sceneBindings, "gameplayBindings")}");
            Debug.Log($"GameplayBindings serialized arenaView: {DescribeSerializedReference(gameplayBindings, "arenaView")}");
            Debug.Log($"GameplayBindings serialized leftBasketView: {DescribeSerializedReference(gameplayBindings, "leftBasketView")}");
            Debug.Log($"GameplayBindings serialized rightBasketView: {DescribeSerializedReference(gameplayBindings, "rightBasketView")}");
            Debug.Log($"GameplayBindings serialized ballView: {DescribeSerializedReference(gameplayBindings, "ballView")}");
            Debug.Log($"ArenaObject component: {DescribeUnityObject(GameObject.Find("ArenaObject")?.GetComponent<rimrushArenaView>())}");
            Debug.Log($"BasketLeft component: {DescribeUnityObject(GameObject.Find("BasketLeft")?.GetComponent<rimrushBasketView>())}");
            Debug.Log($"BasketRight component: {DescribeUnityObject(GameObject.Find("BasketRight")?.GetComponent<rimrushBasketView>())}");
            Debug.Log($"BallObject component: {DescribeUnityObject(GameObject.Find("BallObject")?.GetComponent<rimrushBallView>())}");
            Debug.Log($"ArenaView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.ArenaView : null)}, GraphicRenderer: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.ArenaView != null ? gameplayBindings.ArenaView.GraphicRenderer : null)}");
            Debug.Log($"LeftBasketView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.LeftBasketView : null)}, Root: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.LeftBasketView != null ? gameplayBindings.LeftBasketView.Root : null)}, BasketRenderer: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.LeftBasketView != null ? gameplayBindings.LeftBasketView.BasketRenderer : null)}, FrontEar: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.LeftBasketView != null ? gameplayBindings.LeftBasketView.FrontEarRenderer : null)}, NetLines: {DescribeNetLines(gameplayBindings != null ? gameplayBindings.LeftBasketView : null)}");
            Debug.Log($"RightBasketView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.RightBasketView : null)}, Root: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.RightBasketView != null ? gameplayBindings.RightBasketView.Root : null)}, BasketRenderer: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.RightBasketView != null ? gameplayBindings.RightBasketView.BasketRenderer : null)}, FrontEar: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.RightBasketView != null ? gameplayBindings.RightBasketView.FrontEarRenderer : null)}, NetLines: {DescribeNetLines(gameplayBindings != null ? gameplayBindings.RightBasketView : null)}");
            Debug.Log($"BallView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.BallView : null)}, Root: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.BallView != null ? gameplayBindings.BallView.Root : null)}, GraphicRenderer: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.BallView != null ? gameplayBindings.BallView.GraphicRenderer : null)}, ShadowRenderer: {DescribeUnityObject(gameplayBindings != null && gameplayBindings.BallView != null ? gameplayBindings.BallView.ShadowRenderer : null)}");
            Debug.Log($"LeftPlayerView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetPlayerView(-1) : null)}");
            Debug.Log($"RightPlayerView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetPlayerView(1) : null)}");
            Debug.Log($"EnergyBarSlot0: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetEnergyBarView(0) : null)}");
            Debug.Log($"EnergyBarSlot1: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetEnergyBarView(1) : null)}");
            Debug.Log($"EnergyBarSlot2: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetEnergyBarView(2) : null)}");
            Debug.Log($"LeftTeleportFxView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetTeleportFxView(-1) : null)}");
            Debug.Log($"RightTeleportFxView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetTeleportFxView(1) : null)}");
            Debug.Log($"LeftShieldView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetShieldView(-1) : null)}");
            Debug.Log($"RightShieldView: {DescribeUnityObject(gameplayBindings != null ? gameplayBindings.GetShieldView(1) : null)}");
            EditorApplication.Exit(0);
        }

        private static void BuildMenuAuthoringPages(rimrushGameBootstrap bootstrap, rimrushMenuShellView menuShell)
        {
            if (bootstrap == null || menuShell == null)
            {
                return;
            }

            var pageCatalogObject = new GameObject("PageCatalog");
            pageCatalogObject.transform.SetParent(menuShell.transform, false);
            var pageCatalog = pageCatalogObject.transform;
            var pages = new List<rimrushMenuPageView>();

            pages.Add(CaptureStandardMenuPage(
                bootstrap,
                pageCatalog,
                "ShowPlayerCountMenu",
                "Page_PlayerCount",
                rimrushMenuPageKind.PlayerCount,
                "bg2blue0000",
                true));
            pages.Add(CaptureStandardMenuPage(
                bootstrap,
                pageCatalog,
                "ShowMatchTypeMenu",
                "Page_MatchType",
                rimrushMenuPageKind.MatchType,
                "bg10000",
                true));
            pages.Add(CaptureStandardMenuPage(
                bootstrap,
                pageCatalog,
                "ShowSinglePlayerSetup",
                "Page_QuickSetup",
                rimrushMenuPageKind.QuickSetup,
                "bg10000",
                false));
            pages.Add(CaptureStandardMenuPage(
                bootstrap,
                pageCatalog,
                "ShowTrainingSetup",
                "Page_TrainingSetup",
                rimrushMenuPageKind.TrainingSetup,
                "bg2blue0000",
                false));
            pages.Add(CaptureStandardMenuPage(
                bootstrap,
                pageCatalog,
                "ShowTwoPlayerSetup",
                "Page_TwoPlayerSetup",
                rimrushMenuPageKind.TwoPlayerSetup,
                "bg10000",
                false));
            pages.Add(CaptureStandardMenuPage(
                bootstrap,
                pageCatalog,
                "ShowTournamentSetup",
                "Page_TournamentSetup",
                rimrushMenuPageKind.TournamentSetup,
                "bg2blue0000",
                false));
            pages.Add(BuildTournamentBracketPage(bootstrap, pageCatalog));
            pages.Add(BuildTournamentAwardsPage(bootstrap, pageCatalog));

            Assign(menuShell, "pageCatalog", pageCatalog);
            Assign(menuShell, "pages", pages.ToArray());
        }

        private static rimrushMenuPageView CaptureStandardMenuPage(
            rimrushGameBootstrap bootstrap,
            Transform pageCatalog,
            string methodName,
            string pageName,
            rimrushMenuPageKind pageKind,
            string backgroundFrame,
            bool showLogo)
        {
            Invoke(bootstrap, methodName);
            var pageRoot = CaptureRuntimePageRoot(bootstrap, pageCatalog, pageName);
            return FinalizeMenuPage(pageRoot, pageKind, backgroundFrame, showLogo);
        }

        private static rimrushMenuPageView BuildTournamentBracketPage(rimrushGameBootstrap bootstrap, Transform pageCatalog)
        {
            var pageRootObject = new GameObject("Page_TournamentBracket");
            pageRootObject.transform.SetParent(pageCatalog, false);

            ConfigureTournamentRegularSeasonState();
            Invoke(bootstrap, "ShowTournamentBracket");
            var regularSeasonRoot = CaptureRuntimePageRoot(bootstrap, pageRootObject.transform, "RegularSeasonBoardRoot");

            ConfigureTournamentPlayoffState();
            Invoke(bootstrap, "ShowTournamentBracket");
            var playoffRoot = CaptureRuntimePageRoot(bootstrap, pageRootObject.transform, "PlayoffBoardRoot");

            ConfigureTournamentCompletedState();
            Invoke(bootstrap, "ShowTournamentBracket");
            var completedRoot = CaptureRuntimePageRoot(bootstrap, pageRootObject.transform, "CompletedBoardRoot");

            var bracketView = CreateOrGetComponent<rimrushTournamentBracketSceneView>(pageRootObject);
            Assign(bracketView, "regularSeasonBoardRoot", regularSeasonRoot);
            Assign(bracketView, "playoffBoardRoot", playoffRoot);
            Assign(bracketView, "completedBoardRoot", completedRoot);
            Assign(bracketView, "previewState", rimrushTournamentBracketPreviewState.RegularSeason);
            bracketView.ApplyPreviewState();

            var pageView = FinalizeMenuPage(pageRootObject, rimrushMenuPageKind.TournamentBracket, "bg2blue0000", false);
            Assign(pageView, "tournamentBracketView", bracketView);
            return pageView;
        }

        private static rimrushMenuPageView BuildTournamentAwardsPage(rimrushGameBootstrap bootstrap, Transform pageCatalog)
        {
            ConfigureTournamentCompletedState();
            Invoke(bootstrap, "ShowTournamentAwards");
            Invoke(bootstrap, "UpdateTournamentAwardsSequence", 10f);

            var pageRoot = CaptureRuntimePageRoot(bootstrap, pageCatalog, "Page_TournamentAwards");
            var awardsView = CreateOrGetComponent<rimrushTournamentAwardsSceneView>(pageRoot);
            Assign(awardsView, "root", pageRoot);

            var pageView = FinalizeMenuPage(pageRoot, rimrushMenuPageKind.TournamentAwards, "bg10000", false);
            Assign(pageView, "tournamentAwardsView", awardsView);
            return pageView;
        }

        private static GameObject CaptureRuntimePageRoot(rimrushGameBootstrap bootstrap, Transform parent, string pageName)
        {
            var runtimeRoot = GetField<Transform>(bootstrap, "runtimeRoot");
            if (runtimeRoot == null)
            {
                throw new InvalidOperationException($"Bootstrap did not expose runtimeRoot while capturing {pageName}.");
            }

            runtimeRoot.name = pageName;
            runtimeRoot.SetParent(parent, false);
            SetField(bootstrap, "runtimeRoot", null);
            SetField(bootstrap, "runtimeRootOwned", false);

            var pageRoot = runtimeRoot.gameObject;
            StripMenuChrome(pageRoot);
            ConvertRuntimeButtonsToSceneViews(pageRoot.transform);
            return pageRoot;
        }

        private static rimrushMenuPageView FinalizeMenuPage(
            GameObject pageRoot,
            rimrushMenuPageKind pageKind,
            string backgroundFrame,
            bool showLogo)
        {
            var pageView = CreateOrGetComponent<rimrushMenuPageView>(pageRoot);
            Assign(pageView, "pageKind", pageKind);
            Assign(pageView, "backgroundFrame", backgroundFrame);
            Assign(pageView, "showLogo", showLogo);
            Assign(pageView, "root", pageRoot);
            Assign(pageView, "characterSelectors", BuildCharacterSelectors(pageRoot, pageKind));
            Assign(pageView, "ballSelectors", BuildBallSelectors(pageRoot, pageKind));
            pageRoot.SetActive(true);
            return pageView;
        }

        private static rimrushCharacterSelectorView[] BuildCharacterSelectors(GameObject pageRoot, rimrushMenuPageKind pageKind)
        {
            switch (pageKind)
            {
                case rimrushMenuPageKind.QuickSetup:
                    return new[]
                    {
                        CreateCharacterSelectorBinding(pageRoot.transform, "QuickCharacterSelectorView", "Quick_Header", "Quick_CharacterName", 220f, 306f)
                    };
                case rimrushMenuPageKind.TrainingSetup:
                    return new[]
                    {
                        CreateCharacterSelectorBinding(pageRoot.transform, "TrainingCharacterSelectorView", "Training_Header", "Training_CharacterName", 220f, 306f)
                    };
                case rimrushMenuPageKind.TwoPlayerSetup:
                    return new[]
                    {
                        CreateCharacterSelectorBinding(pageRoot.transform, "P1CharacterSelectorView", "P1_Header", "P1_CharacterName", 214f, 308f),
                        CreateCharacterSelectorBinding(pageRoot.transform, "P2CharacterSelectorView", "P2_Header", "P2_CharacterName", 586f, 308f)
                    };
                case rimrushMenuPageKind.TournamentSetup:
                    return new[]
                    {
                        CreateCharacterSelectorBinding(pageRoot.transform, "TournamentCharacterSelectorView", "Tournament_Header", "Tournament_CharacterName", 220f, 306f)
                    };
                default:
                    return new rimrushCharacterSelectorView[0];
            }
        }

        private static rimrushBallSelectorView[] BuildBallSelectors(GameObject pageRoot, rimrushMenuPageKind pageKind)
        {
            switch (pageKind)
            {
                case rimrushMenuPageKind.QuickSetup:
                    return new[]
                    {
                        CreateBallSelectorBinding(pageRoot.transform, "QuickBallSelectorView", "QuickBall_Header", "QuickBall_Label", "QuickBall_Preview", 575f, 232f)
                    };
                case rimrushMenuPageKind.TrainingSetup:
                    return new[]
                    {
                        CreateBallSelectorBinding(pageRoot.transform, "TrainingBallSelectorView", "TrainingBall_Header", "TrainingBall_Label", "TrainingBall_Preview", 575f, 232f)
                    };
                case rimrushMenuPageKind.TwoPlayerSetup:
                    return new[]
                    {
                        CreateBallSelectorBinding(pageRoot.transform, "VersusBallSelectorView", "VersusBall_Header", "VersusBall_Label", "VersusBall_Preview", 400f, 356f)
                    };
                case rimrushMenuPageKind.TournamentSetup:
                    return new[]
                    {
                        CreateBallSelectorBinding(pageRoot.transform, "TournamentBallSelectorView", "TournamentBall_Header", "TournamentBall_Label", "TournamentBall_Preview", 575f, 232f)
                    };
                default:
                    return new rimrushBallSelectorView[0];
            }
        }

        private static rimrushCharacterSelectorView CreateCharacterSelectorBinding(
            Transform pageRoot,
            string viewName,
            string headerName,
            string nameTextName,
            float centerX,
            float previewY)
        {
            var viewObject = new GameObject(viewName);
            viewObject.transform.SetParent(pageRoot, false);
            var view = viewObject.AddComponent<rimrushCharacterSelectorView>();

            Assign(view, "headerText", FindTextByName(pageRoot, headerName));
            Assign(view, "previousButtonView", FindNearestButtonView(pageRoot, "<", centerX - 74f, 258f));
            Assign(view, "nextButtonView", FindNearestButtonView(pageRoot, ">", centerX + 74f, 258f));
            Assign(view, "shadowRenderer", FindNearestSpriteRendererByName(pageRoot, "PreviewShadow", centerX, previewY + 24f));
            Assign(view, "previewMount", FindNearestTransformByPrefix(pageRoot, "Preview_", centerX, previewY));
            Assign(view, "nameText", FindTextByName(pageRoot, nameTextName));
            return view;
        }

        private static rimrushBallSelectorView CreateBallSelectorBinding(
            Transform pageRoot,
            string viewName,
            string headerName,
            string labelName,
            string previewName,
            float centerX,
            float previewY)
        {
            var viewObject = new GameObject(viewName);
            viewObject.transform.SetParent(pageRoot, false);
            var view = viewObject.AddComponent<rimrushBallSelectorView>();

            Assign(view, "headerText", FindTextByName(pageRoot, headerName));
            Assign(view, "previousButtonView", FindNearestButtonView(pageRoot, "<", centerX - 68f, previewY));
            Assign(view, "nextButtonView", FindNearestButtonView(pageRoot, ">", centerX + 68f, previewY));
            Assign(view, "previewRenderer", FindSpriteRendererByName(pageRoot, previewName));
            Assign(view, "labelText", FindTextByName(pageRoot, labelName));
            return view;
        }

        private static void StripMenuChrome(GameObject pageRoot)
        {
            var transforms = pageRoot.GetComponentsInChildren<Transform>(true);
            for (var i = transforms.Length - 1; i >= 0; i--)
            {
                var current = transforms[i];
                if (current == null || current == pageRoot.transform)
                {
                    continue;
                }

                var name = current.gameObject.name;
                if (name == "MenuBackground" || name == "Logo" || name.StartsWith("MenuMusicButton_", StringComparison.Ordinal) || name.StartsWith("MenuHelpButton_", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(current.gameObject);
                    continue;
                }

                if ((name == "Button_" || name == "ButtonText_") && IsTopChromeButton(current.position))
                {
                    UnityEngine.Object.DestroyImmediate(current.gameObject);
                }
            }
        }

        private static bool IsTopChromeButton(Vector3 worldPosition)
        {
            var pixel = rimrushConstants.WorldToPixel(worldPosition);
            return Mathf.Abs(pixel.y - 44f) <= 8f &&
                   (Mathf.Abs(pixel.x - 770f) <= 16f || Mathf.Abs(pixel.x - 706f) <= 16f);
        }

        private static void ConvertRuntimeButtonsToSceneViews(Transform root)
        {
            var labels = new List<TextMesh>();
            var buttons = new List<SpriteRenderer>();
            var allText = root.GetComponentsInChildren<TextMesh>(true);
            for (var i = 0; i < allText.Length; i++)
            {
                if (allText[i] != null && allText[i].gameObject.name.StartsWith("ButtonText_", StringComparison.Ordinal))
                {
                    labels.Add(allText[i]);
                }
            }

            var allRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].gameObject.name.StartsWith("Button_", StringComparison.Ordinal))
                {
                    buttons.Add(allRenderers[i]);
                }
            }

            var usedLabels = new HashSet<TextMesh>();
            for (var i = 0; i < buttons.Count; i++)
            {
                var backgroundRenderer = buttons[i];
                if (backgroundRenderer == null || backgroundRenderer.GetComponent<rimrushMenuButtonView>() != null || backgroundRenderer.transform.parent == null)
                {
                    continue;
                }

                var parent = backgroundRenderer.transform.parent;
                var siblingIndex = backgroundRenderer.transform.GetSiblingIndex();
                var wrapper = new GameObject($"{backgroundRenderer.gameObject.name}_View");
                wrapper.transform.SetParent(parent, false);
                wrapper.transform.SetSiblingIndex(siblingIndex);
                wrapper.transform.position = backgroundRenderer.transform.position;
                wrapper.transform.rotation = backgroundRenderer.transform.rotation;
                wrapper.transform.localScale = Vector3.one;

                backgroundRenderer.transform.SetParent(wrapper.transform, true);
                var label = FindClosestLabel(labels, usedLabels, backgroundRenderer.transform.position);
                if (label != null)
                {
                    label.transform.SetParent(wrapper.transform, true);
                    usedLabels.Add(label);
                }

                var view = wrapper.AddComponent<rimrushMenuButtonView>();
                Assign(view, "backgroundRenderer", backgroundRenderer);
                Assign(view, "label", label);
            }
        }

        private static TextMesh FindClosestLabel(List<TextMesh> labels, HashSet<TextMesh> usedLabels, Vector3 targetPosition)
        {
            TextMesh best = null;
            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null || usedLabels.Contains(label))
                {
                    continue;
                }

                var distance = (label.transform.position - targetPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = label;
                }
            }

            return best;
        }

        private static rimrushMenuButtonView FindNearestButtonView(Transform root, string labelText, float x, float y)
        {
            var views = root.GetComponentsInChildren<rimrushMenuButtonView>(true);
            rimrushMenuButtonView best = null;
            var bestDistance = float.PositiveInfinity;
            var target = new Vector2(x, y);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null || view.Label == null || !string.Equals(view.Label.text, labelText, StringComparison.Ordinal))
                {
                    continue;
                }

                var pixel = rimrushConstants.WorldToPixel(view.Root.transform.position);
                var distance = (pixel - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = view;
                }
            }

            return best;
        }

        private static TextMesh FindTextByName(Transform root, string name)
        {
            var texts = root.GetComponentsInChildren<TextMesh>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].gameObject.name == name)
                {
                    return texts[i];
                }
            }

            return null;
        }

        private static SpriteRenderer FindSpriteRendererByName(Transform root, string name)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].gameObject.name == name)
                {
                    return renderers[i];
                }
            }

            return null;
        }

        private static Transform FindNearestTransformByPrefix(Transform root, string prefix, float x, float y)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            Transform best = null;
            var bestDistance = float.PositiveInfinity;
            var target = new Vector2(x, y);
            for (var i = 0; i < transforms.Length; i++)
            {
                var current = transforms[i];
                if (current == null || !current.gameObject.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var pixel = rimrushConstants.WorldToPixel(current.position);
                var distance = (pixel - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = current;
                }
            }

            return best;
        }

        private static SpriteRenderer FindNearestSpriteRendererByName(Transform root, string name, float x, float y)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer best = null;
            var bestDistance = float.PositiveInfinity;
            var target = new Vector2(x, y);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.gameObject.name != name)
                {
                    continue;
                }

                var pixel = rimrushConstants.WorldToPixel(renderer.transform.position);
                var distance = (pixel - target).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = renderer;
                }
            }

            return best;
        }

        private static void ResetAuthoringInventoryState()
        {
            var inventory = rimrushInventory.Instance;
            inventory.AbandonTournament();
            inventory.Difficulty = rimrushAiDifficulty.Normal;
            inventory.SetQuickSelection(0);
            inventory.SetQuickBallSelection(rimrushBallSelection.ClassicOriginal);
            inventory.SetTrainingSelection(0);
            inventory.SetTrainingBallSelection(rimrushBallSelection.ClassicOriginal);
            inventory.SetTournamentSelection(0);
            inventory.SetTournamentBallSelection(rimrushBallSelection.ClassicOriginal);
            inventory.SetVersusBallSelection(rimrushBallSelection.ClassicOriginal);
        }

        private static void ConfigureTournamentRegularSeasonState()
        {
            ResetAuthoringInventoryState();
            var inventory = rimrushInventory.Instance;
            inventory.BeginTournament();
        }

        private static void ConfigureTournamentPlayoffState()
        {
            ConfigureTournamentRegularSeasonState();
            var tournament = rimrushInventory.Instance.Tournament;
            tournament.ApplyCurrentMatchResult(28, 16);
            tournament.ApplyCurrentMatchResult(30, 17);
            tournament.ApplyCurrentMatchResult(31, 19);
            rimrushInventory.Instance.BeginTournamentFinals();
        }

        private static void ConfigureTournamentCompletedState()
        {
            ConfigureTournamentPlayoffState();
            var tournament = rimrushInventory.Instance.Tournament;
            tournament.ApplyCurrentMatchResult(33, 21);
            tournament.ApplyCurrentMatchResult(34, 22);
        }

        private static T CreateOrGetComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            return (T)field.GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }

        private static object Invoke(object target, string methodName, params object[] parameters)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().FullName, methodName);
            }

            return method.Invoke(target, parameters);
        }

        private static Camera EnsureMainCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = GameObject.Find("Main Camera");
                if (cameraObject == null)
                {
                    cameraObject = new GameObject("Main Camera");
                }

                camera = cameraObject.GetComponent<Camera>();
                if (camera == null)
                {
                    camera = cameraObject.AddComponent<Camera>();
                }
            }

            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = rimrushConstants.GameH / (2f * rimrushConstants.PixelsPerUnit);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = Color.black;
            return camera;
        }

        private static rimrushAudio CreateAudioHost(Transform parent)
        {
            var audioObject = new GameObject("rimrushAudio");
            audioObject.transform.SetParent(parent, false);
            var audio = audioObject.AddComponent<rimrushAudio>();
            audioObject.AddComponent<AudioSource>();
            audioObject.AddComponent<AudioSource>();
            return audio;
        }

        private static rimrushMenuShellView CreateMenuShell(Transform parent)
        {
            var shellObject = new GameObject("MenuShell");
            shellObject.transform.SetParent(parent, false);
            var shell = shellObject.AddComponent<rimrushMenuShellView>();

            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(shellObject.transform, false);
            var backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sortingOrder = 0;
            backgroundRenderer.sprite = rimrushAtlasCache.Instance.Interface.Sprite("bg2blue0000", 0.5f, 0.5f);
            rimrushRender.ApplyPixelTransform(backgroundObject.transform, rimrushConstants.Width2, 240f, 0f);

            var logoObject = new GameObject("Logo");
            logoObject.transform.SetParent(shellObject.transform, false);
            var logoRenderer = logoObject.AddComponent<SpriteRenderer>();
            var logoTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameLogo));
            if (logoTexture != null)
            {
                logoRenderer.sprite = Sprite.Create(
                    logoTexture,
                    new Rect(0f, 0f, logoTexture.width, logoTexture.height),
                    new Vector2(0.5f, 0.5f),
                    1f);
            }

            logoRenderer.sortingOrder = 20;
            rimrushRender.ApplyPixelTransform(logoObject.transform, rimrushConstants.Width2, 68f, 0f);
            logoObject.transform.localScale = new Vector3(0.78f, 0.68f, 1f);

            var buttonPoolRoot = new GameObject("ButtonPool");
            buttonPoolRoot.transform.SetParent(shellObject.transform, false);
            var buttonPool = new rimrushMenuButtonView[18];
            for (var i = 0; i < buttonPool.Length; i++)
            {
                var buttonView = rimrushMenuButtonView.CreateRuntimeFallback($"MenuButton{i}", buttonPoolRoot.transform);
                _ = new rimrushMenuButton(buttonView, string.Empty, rimrushConstants.Width2, 240f, 200f, 44f, () => { });
                buttonView.Root.SetActive(false);
                buttonPool[i] = buttonView;
            }

            var topButtonsRoot = new GameObject("TopButtons");
            topButtonsRoot.transform.SetParent(shellObject.transform, false);
            var musicButton = rimrushIconButtonView.CreateRuntimeFallback(
                "MenuMusicButtonView",
                topButtonsRoot.transform,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn),
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff));
            _ = new rimrushIconButton(
                musicButton,
                "MenuMusicButton",
                770f,
                44f,
                60f,
                60f,
                () => { },
                32,
                58f,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn),
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff));

            var helpButton = rimrushIconButtonView.CreateRuntimeFallback(
                "MenuHelpButtonView",
                topButtonsRoot.transform,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton));
            _ = new rimrushIconButton(
                helpButton,
                "MenuHelpButton",
                706f,
                44f,
                60f,
                60f,
                () => { },
                32,
                58f,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton));

            var dynamicContentRoot = new GameObject("DynamicContentRoot").transform;
            dynamicContentRoot.SetParent(shellObject.transform, false);

            Assign(shell, "backgroundRenderer", backgroundRenderer);
            Assign(shell, "logoRenderer", logoRenderer);
            Assign(shell, "dynamicContentRoot", dynamicContentRoot);
            Assign(shell, "buttonPool", buttonPool);
            Assign(shell, "musicButton", musicButton);
            Assign(shell, "helpButton", helpButton);
            return shell;
        }

        private static rimrushGameplayBindings CreateGameplayBindings(Transform parent)
        {
            var gameplayObject = new GameObject("GameplayRoot");
            gameplayObject.transform.SetParent(parent, false);
            var gameplayBindings = gameplayObject.AddComponent<rimrushGameplayBindings>();

            var arenaView = rimrushArenaView.CreateRuntimeFallback(gameplayObject.transform);
            var leftBasketView = rimrushBasketView.CreateRuntimeFallback(-1, gameplayObject.transform);
            var rightBasketView = rimrushBasketView.CreateRuntimeFallback(1, gameplayObject.transform);
            var ballView = rimrushBallView.CreateRuntimeFallback(gameplayObject.transform);
            var leftPlayerView = rimrushPlayerView.CreateRuntimeFallback("LeftPlayer", 0, gameplayObject.transform);
            var rightPlayerView = rimrushPlayerView.CreateRuntimeFallback("RightPlayer", 0, gameplayObject.transform);

            var leftNeutralSpawn = CreateAnchor("LeftNeutralSpawn", rimrushConstants.Width2 - rimrushObjectsData.PlayerIndentX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);
            var rightNeutralSpawn = CreateAnchor("RightNeutralSpawn", rimrushConstants.Width2 + rimrushObjectsData.PlayerIndentX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);
            var leftServeSpawn = CreateAnchor("LeftServeSpawn", rimrushObjectsData.IndentGeneralX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);
            var rightServeSpawn = CreateAnchor("RightServeSpawn", rimrushConstants.Width - rimrushObjectsData.IndentGeneralX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);

            var energyBarSlot0 = CreateEnergyBarView("EnergyBarSlot0", gameplayObject.transform, 45f);
            var energyBarSlot1 = CreateEnergyBarView("EnergyBarSlot1", gameplayObject.transform, 185f);
            var energyBarSlot2 = CreateEnergyBarView("EnergyBarSlot2", gameplayObject.transform, 614f);
            _ = new rimrushEnergyBarView(energyBarSlot0, 0, 0, 1f);
            _ = new rimrushEnergyBarView(energyBarSlot1, 1, 0, 1f);
            _ = new rimrushEnergyBarView(energyBarSlot2, 2, 0, 1f);

            var leftTeleportFxView = rimrushTeleportFxView.CreateRuntimeFallback(gameplayObject.transform);
            leftTeleportFxView.gameObject.name = "LeftTeleportFxView";
            var rightTeleportFxView = rimrushTeleportFxView.CreateRuntimeFallback(gameplayObject.transform);
            rightTeleportFxView.gameObject.name = "RightTeleportFxView";
            var leftShieldView = rimrushShieldView.CreateRuntimeFallback(-1, gameplayObject.transform);
            leftShieldView.gameObject.name = "LeftShieldView";
            var rightShieldView = rimrushShieldView.CreateRuntimeFallback(1, gameplayObject.transform);
            rightShieldView.gameObject.name = "RightShieldView";

            var hudView = CreateHudSceneView(gameplayObject.transform);

            Assign(gameplayBindings, "root", gameplayObject.transform);
            Assign(gameplayBindings, "arenaView", arenaView);
            Assign(gameplayBindings, "leftBasketView", leftBasketView);
            Assign(gameplayBindings, "rightBasketView", rightBasketView);
            Assign(gameplayBindings, "ballView", ballView);
            Assign(gameplayBindings, "leftPlayerView", leftPlayerView);
            Assign(gameplayBindings, "rightPlayerView", rightPlayerView);
            Assign(gameplayBindings, "leftNeutralSpawn", leftNeutralSpawn);
            Assign(gameplayBindings, "rightNeutralSpawn", rightNeutralSpawn);
            Assign(gameplayBindings, "leftServeSpawn", leftServeSpawn);
            Assign(gameplayBindings, "rightServeSpawn", rightServeSpawn);
            Assign(gameplayBindings, "energyBarSlot0", energyBarSlot0);
            Assign(gameplayBindings, "energyBarSlot1", energyBarSlot1);
            Assign(gameplayBindings, "energyBarSlot2", energyBarSlot2);
            Assign(gameplayBindings, "leftTeleportFxView", leftTeleportFxView);
            Assign(gameplayBindings, "rightTeleportFxView", rightTeleportFxView);
            Assign(gameplayBindings, "leftShieldView", leftShieldView);
            Assign(gameplayBindings, "rightShieldView", rightShieldView);
            Assign(gameplayBindings, "hudView", hudView);

            return gameplayBindings;
        }

        private static rimrushGameplayBindings CreateStage3GameplayBindings(Transform parent, rimrushHudSceneView sharedHudView)
        {
            var gameplayObject = new GameObject("GameplayRoot");
            gameplayObject.transform.SetParent(parent, false);
            var gameplayBindings = gameplayObject.AddComponent<rimrushGameplayBindings>();

            var arenaView = CreateStage3ArenaView(gameplayObject.transform);
            var leftBasketView = CreateStage3BasketView("BasketLeft", -1, gameplayObject.transform);
            var rightBasketView = CreateStage3BasketView("BasketRight", 1, gameplayObject.transform);
            var ballView = CreateStage3BallView(gameplayObject.transform);

            var leftNeutralSpawn = CreateAnchor("LeftNeutralSpawn", rimrushConstants.Width2 - rimrushObjectsData.PlayerIndentX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);
            var rightNeutralSpawn = CreateAnchor("RightNeutralSpawn", rimrushConstants.Width2 + rimrushObjectsData.PlayerIndentX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);
            var leftServeSpawn = CreateAnchor("LeftServeSpawn", rimrushObjectsData.IndentGeneralX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);
            var rightServeSpawn = CreateAnchor("RightServeSpawn", rimrushConstants.Width - rimrushObjectsData.IndentGeneralX, rimrushObjectsData.PlayerIndentY, gameplayObject.transform);

            Assign(gameplayBindings, "root", gameplayObject.transform);
            Assign(gameplayBindings, "arenaView", arenaView);
            Assign(gameplayBindings, "leftBasketView", leftBasketView);
            Assign(gameplayBindings, "rightBasketView", rightBasketView);
            Assign(gameplayBindings, "ballView", ballView);
            Assign(gameplayBindings, "leftPlayerView", null);
            Assign(gameplayBindings, "rightPlayerView", null);
            Assign(gameplayBindings, "leftNeutralSpawn", leftNeutralSpawn);
            Assign(gameplayBindings, "rightNeutralSpawn", rightNeutralSpawn);
            Assign(gameplayBindings, "leftServeSpawn", leftServeSpawn);
            Assign(gameplayBindings, "rightServeSpawn", rightServeSpawn);
            Assign(gameplayBindings, "energyBarSlot0", null);
            Assign(gameplayBindings, "energyBarSlot1", null);
            Assign(gameplayBindings, "energyBarSlot2", null);
            Assign(gameplayBindings, "leftTeleportFxView", null);
            Assign(gameplayBindings, "rightTeleportFxView", null);
            Assign(gameplayBindings, "leftShieldView", null);
            Assign(gameplayBindings, "rightShieldView", null);
            Assign(gameplayBindings, "hudView", sharedHudView);

            return gameplayBindings;
        }

        private static rimrushGameplayBindings CreateStage4GameplayBindings(Transform parent, rimrushHudSceneView sharedHudView)
        {
            var gameplayBindings = CreateStage3GameplayBindings(parent, sharedHudView);
            var root = gameplayBindings.Root;

            var leftNeutralSpawn = root.Find("LeftNeutralSpawn");
            var rightNeutralSpawn = root.Find("RightNeutralSpawn");

            var leftPlayerPosition = leftNeutralSpawn != null
                ? rimrushConstants.WorldToPixel(leftNeutralSpawn.position)
                : new Vector2(rimrushConstants.Width2 - rimrushObjectsData.PlayerIndentX, rimrushObjectsData.PlayerIndentY);
            var rightPlayerPosition = rightNeutralSpawn != null
                ? rimrushConstants.WorldToPixel(rightNeutralSpawn.position)
                : new Vector2(rimrushConstants.Width2 + rimrushObjectsData.PlayerIndentX, rimrushObjectsData.PlayerIndentY);

            var leftPlayerView = CreateStage4PlayerView("LeftPlayerView", 0, -1, root, leftPlayerPosition);
            var rightPlayerView = CreateStage4PlayerView("RightPlayerView", 0, 1, root, rightPlayerPosition);

            var energyBarSlot0 = CreateEnergyBarView("EnergyBarSlot0", root, 45f);
            var energyBarSlot1 = CreateEnergyBarView("EnergyBarSlot1", root, 185f);
            var energyBarSlot2 = CreateEnergyBarView("EnergyBarSlot2", root, 614f);
            _ = new rimrushEnergyBarView(energyBarSlot0, 0, 0, 1f);
            _ = new rimrushEnergyBarView(energyBarSlot1, 1, 1, 1f);
            _ = new rimrushEnergyBarView(energyBarSlot2, 2, 2, 1f);

            var leftTeleportFxView = CreateStage4TeleportFxView("LeftTeleportFxView", root, leftPlayerPosition + new Vector2(0f, -36f));
            var rightTeleportFxView = CreateStage4TeleportFxView("RightTeleportFxView", root, rightPlayerPosition + new Vector2(0f, -36f));

            var leftShieldView = CreateStage4ShieldView("LeftShieldView", -1, root);
            var rightShieldView = CreateStage4ShieldView("RightShieldView", 1, root);

            Assign(gameplayBindings, "leftPlayerView", leftPlayerView);
            Assign(gameplayBindings, "rightPlayerView", rightPlayerView);
            Assign(gameplayBindings, "energyBarSlot0", energyBarSlot0);
            Assign(gameplayBindings, "energyBarSlot1", energyBarSlot1);
            Assign(gameplayBindings, "energyBarSlot2", energyBarSlot2);
            Assign(gameplayBindings, "leftTeleportFxView", leftTeleportFxView);
            Assign(gameplayBindings, "rightTeleportFxView", rightTeleportFxView);
            Assign(gameplayBindings, "leftShieldView", leftShieldView);
            Assign(gameplayBindings, "rightShieldView", rightShieldView);
            return gameplayBindings;
        }

        private static rimrushArenaView CreateStage3ArenaView(Transform parent)
        {
            var arenaObject = rimrushRender.Sprite(
                "ArenaObject",
                rimrushAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000",
                -299f,
                0f,
                0f,
                0f,
                0,
                parent);
            var arenaView = arenaObject.AddComponent<rimrushArenaView>();
            Assign(arenaView, "graphicRenderer", arenaObject.GetComponent<SpriteRenderer>());
            return arenaView;
        }

        private static rimrushBasketView CreateStage3BasketView(string rootName, int side, Transform parent)
        {
            var container = new GameObject(rootName);
            container.transform.SetParent(parent, false);
            var basketView = container.AddComponent<rimrushBasketView>();

            var center = side == -1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2;

            var graphicRoot = new GameObject("Root");
            graphicRoot.transform.SetParent(container.transform, false);
            rimrushRender.ApplyPixelTransform(graphicRoot.transform, center, rimrushObjectsData.BasketHeight, 0.05f);
            graphicRoot.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f),
                rimrushConstants.UnitsPerPixel,
                1f);

            var basketGraphic = new GameObject("BasketGraphic");
            basketGraphic.transform.SetParent(graphicRoot.transform, false);
            var basketRenderer = basketGraphic.AddComponent<SpriteRenderer>();
            basketRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BasketGraphic0000", 0.7f, 0.93f);
            basketRenderer.sortingOrder = 4;

            var frontEar = new GameObject("FrontEar");
            frontEar.transform.SetParent(container.transform, false);
            var frontEarRenderer = frontEar.AddComponent<SpriteRenderer>();
            frontEarRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("FrontEar0000", 0.5f, 0.5f);
            frontEarRenderer.sortingOrder = 60;
            rimrushRender.ApplyPixelTransform(frontEar.transform, center, rimrushObjectsData.BasketHeight, 0f);
            frontEar.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f),
                rimrushConstants.UnitsPerPixel,
                1f);

            var netLines = new LineRenderer[10];
            for (var i = 0; i < netLines.Length; i++)
            {
                var lineObject = new GameObject($"NetLine{i}");
                lineObject.transform.SetParent(container.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.018f;
                line.endWidth = 0.018f;
                line.sharedMaterial = rimrushSharedMaterialCache.GetSpritesDefault(Texture2D.whiteTexture);
                line.startColor = Color.white;
                line.endColor = Color.white;
                line.sortingOrder = 55;
                netLines[i] = line;
            }

            Assign(basketView, "root", graphicRoot.transform);
            Assign(basketView, "basketRenderer", basketRenderer);
            Assign(basketView, "frontEarRenderer", frontEarRenderer);
            Assign(basketView, "netLines", netLines);
            return basketView;
        }

        private static rimrushBallView CreateStage3BallView(Transform parent)
        {
            var container = new GameObject("BallObject");
            container.transform.SetParent(parent, false);
            var ballView = container.AddComponent<rimrushBallView>();

            var graphic = new GameObject("Graphic");
            graphic.transform.SetParent(container.transform, false);
            var graphicRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);
            graphicRenderer.sortingOrder = 50;
            rimrushRender.ApplyPixelTransform(graphic.transform, rimrushConstants.Width2, rimrushObjectsData.BallIndentYCenter, 0.2f);

            var shadow = rimrushRender.Sprite(
                "BallShadow",
                rimrushAtlasCache.Instance.Gameplay,
                "ShadowMC0002",
                rimrushConstants.Width2,
                rimrushObjectsData.FloorY,
                0.5f,
                0.5f,
                3,
                container.transform);
            shadow.transform.localScale *= 0.7f;

            Assign(ballView, "root", graphic.transform);
            Assign(ballView, "graphicRenderer", graphicRenderer);
            Assign(ballView, "shadowRenderer", shadow.GetComponent<SpriteRenderer>());
            return ballView;
        }

        private static rimrushPlayerView CreateStage4PlayerView(string name, int playerNo, int side, Transform parent, Vector2 pixelPosition)
        {
            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            var playerView = container.AddComponent<rimrushPlayerView>();

            var rootObject = new GameObject("Root");
            rootObject.transform.SetParent(container.transform, false);

            var shadowObject = rimrushRender.Sprite(
                "Shadow",
                rimrushAtlasCache.Instance.Gameplay,
                playerNo == 0 ? "ShadowMC0000" : "ShadowMC0001",
                0f,
                0f,
                0.5f,
                0.5f,
                2,
                container.transform);
            var shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();

            var armatureMount = new GameObject("ArmatureMount").transform;
            armatureMount.SetParent(rootObject.transform, false);
            armatureMount.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                rootObject.transform,
                new Vector3(0f, -35f, 0f));

            var fallbackObject = new GameObject("Fallback");
            fallbackObject.transform.SetParent(rootObject.transform, false);
            var fallbackRenderer = fallbackObject.AddComponent<SpriteRenderer>();
            fallbackRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BallClipMsg0000", 0.5f, 0.5f);
            fallbackRenderer.color = side == -1 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            fallbackRenderer.sortingOrder = 20;
            fallbackRenderer.enabled = true;
            fallbackRenderer.transform.localPosition = new Vector3(0f, -80f, 0f);
            fallbackRenderer.transform.localScale = new Vector3(1.2f, 1.8f, 1f);

            ApplyPlayerPreviewPose(rootObject.transform, shadowRenderer.transform, side, playerNo, pixelPosition);

            Assign(playerView, "root", rootObject.transform);
            Assign(playerView, "shadowRenderer", shadowRenderer);
            Assign(playerView, "armatureMount", armatureMount);
            Assign(playerView, "fallbackRenderer", fallbackRenderer);
            return playerView;
        }

        private static rimrushTeleportFxView CreateStage4TeleportFxView(string name, Transform parent, Vector2 pixelPosition)
        {
            var view = rimrushTeleportFxView.CreateRuntimeFallback(parent);
            view.gameObject.name = name;
            view.Root.SetActive(true);
            rimrushRender.ApplyPixelTransform(view.Root.transform, pixelPosition.x, pixelPosition.y, 0.16f, 0.72f);
            if (view.BlackNode != null)
            {
                view.BlackNode.localScale = new Vector3(0.55f, 0.55f, 1f);
                view.BlackNode.localRotation = Quaternion.identity;
            }

            if (view.BlackRenderer != null)
            {
                view.BlackRenderer.enabled = true;
            }

            if (view.CenterRenderer != null)
            {
                view.CenterRenderer.enabled = true;
            }

            if (view.WhiteRenderer != null)
            {
                view.WhiteRenderer.enabled = false;
            }

            if (view.AnimRenderer != null)
            {
                view.AnimRenderer.enabled = false;
                view.AnimRenderer.sprite = null;
            }

            return view;
        }

        private static rimrushShieldView CreateStage4ShieldView(string name, int side, Transform parent)
        {
            var view = rimrushShieldView.CreateRuntimeFallback(side, parent);
            view.gameObject.name = name;
            var x = (side == -1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2) + side * 23f;
            var y = rimrushObjectsData.BasketHeight - 62f;
            rimrushRender.ApplyPixelTransform(view.Root.transform, x, y, 0.15f, 1f);
            var localScale = view.Root.transform.localScale;
            localScale.x = side == 1 ? -Mathf.Abs(localScale.x) : Mathf.Abs(localScale.x);
            view.Root.transform.localScale = localScale;

            if (view.BlurRenderer != null)
            {
                view.BlurRenderer.enabled = true;
                view.BlurRenderer.color = new Color(1f, 1f, 1f, 0.82f);
            }

            if (view.StartRenderer != null)
            {
                view.StartRenderer.enabled = true;
                view.StartRenderer.color = Color.white;
            }

            if (view.AnimRenderer != null)
            {
                view.AnimRenderer.enabled = false;
                view.AnimRenderer.sprite = null;
            }

            return view;
        }

        private static void ApplyPlayerPreviewPose(Transform root, Transform shadow, int side, int playerNo, Vector2 pixelPosition)
        {
            rimrushRender.ApplyPixelTransform(root, pixelPosition.x, pixelPosition.y, 0.12f + playerNo * 0.01f);
            root.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * -side,
                rimrushConstants.UnitsPerPixel,
                1f);

            if (shadow != null)
            {
                shadow.gameObject.SetActive(true);
                rimrushRender.ApplyPixelTransform(shadow, pixelPosition.x, rimrushObjectsData.FloorY + 6f, 0.02f, 1f);
            }
        }

        private static rimrushHudSceneView CreateHudSceneView(Transform parent)
        {
            var hudRoot = new GameObject("HudSceneRoot");
            hudRoot.transform.SetParent(parent, false);
            var sceneView = hudRoot.AddComponent<rimrushHudSceneView>();

            var scoreboardBackdrop = CreateHudImage("ScoreboardBackdrop", rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Scoreboard), 400f, 88f, 360f, 80, hudRoot.transform);
            if (scoreboardBackdrop == null)
            {
                scoreboardBackdrop = rimrushRender.Sprite("ScoreboardBackdropFallback", rimrushAtlasCache.Instance.Gameplay, "infoPanel0000", rimrushConstants.Width2, 60f, 0.5f, 0.5f, 80, hudRoot.transform);
            }

            var leftPortraitAura = CreateAura("LeftPortraitAura", 291f, 74f, 0.235f, 81, hudRoot.transform);
            var rightPortraitAura = CreateAura("RightPortraitAura", 509f, 74f, 0.235f, 81, hudRoot.transform);
            var leftPortraitRenderer = CreatePortraitRenderer("LeftPortrait", 83, hudRoot.transform);
            rimrushRender.ApplyPixelTransform(leftPortraitRenderer.transform, 291f, 74f, 0f);
            var rightPortraitRenderer = CreatePortraitRenderer("RightPortrait", 83, hudRoot.transform);
            rimrushRender.ApplyPixelTransform(rightPortraitRenderer.transform, 509f, 74f, 0f);

            var leftNameText = rimrushRender.Text("LeftName", string.Empty, 254f, 66f, 18, Color.white, TextAnchor.MiddleRight, 85, hudRoot.transform, rimrushTextStyle.HudName);
            var rightNameText = rimrushRender.Text("RightName", string.Empty, 546f, 66f, 18, Color.white, TextAnchor.MiddleLeft, 85, hudRoot.transform, rimrushTextStyle.HudName);
            var scoreColor = new Color32(0xFF, 0xA7, 0x22, 0xFF);
            var leftScoreText = rimrushRender.Text("LeftScore", "0", 370f, 68f, 34, scoreColor, TextAnchor.MiddleCenter, 86, hudRoot.transform, rimrushTextStyle.HudScore);
            var rightScoreText = rimrushRender.Text("RightScore", "0", 430f, 68f, 34, scoreColor, TextAnchor.MiddleCenter, 86, hudRoot.transform, rimrushTextStyle.HudScore);
            var timerText = rimrushRender.Text("Timer", "1:00", 400f, 110f, 18, new Color32(0xC6, 0xFF, 0x33, 0xFF), TextAnchor.MiddleCenter, 87, hudRoot.transform, rimrushTextStyle.HudTimer);

            var pauseButtonView = rimrushMenuButtonView.CreateRuntimeFallback("PauseButtonView", hudRoot.transform);
            _ = new rimrushMenuButton(pauseButtonView, string.Empty, 770f, 44f, 60f, 60f, () => { });
            var pauseButtonIcon = CreatePauseIcon("PauseButtonIcon", 770f, 44f, 82, 58f, hudRoot.transform);

            var musicButtonView = rimrushIconButtonView.CreateRuntimeFallback(
                "HudMusicButtonView",
                hudRoot.transform,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn),
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff));
            _ = new rimrushIconButton(
                musicButtonView,
                "HudMusicButton",
                706f,
                44f,
                60f,
                60f,
                () => { },
                82,
                58f,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn),
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff));

            var helpButtonView = rimrushIconButtonView.CreateRuntimeFallback(
                "HudHelpButtonView",
                hudRoot.transform,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton));
            _ = new rimrushIconButton(
                helpButtonView,
                "HudHelpButton",
                642f,
                44f,
                60f,
                60f,
                () => { },
                82,
                58f,
                rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton));

            var countdownBackdrop = CreateHudImage("CountdownBackdrop", rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Popup), rimrushConstants.Width2, 176f, 360f, 119, hudRoot.transform);
            var countdownCaptionText = rimrushRender.Text("CountdownCaption", string.Empty, rimrushConstants.Width2, 144f, 16, new Color32(0xC8, 0xFF, 0x55, 0xFF), TextAnchor.MiddleCenter, 120, hudRoot.transform, rimrushTextStyle.TournamentAccent);
            var countdownText = rimrushRender.Text("Countdown", string.Empty, rimrushConstants.Width2, 172f, 58, new Color32(0xFF, 0xB8, 0x2E, 0xFF), TextAnchor.MiddleCenter, 120, hudRoot.transform, rimrushTextStyle.HudPopup);

            var messageRoot = CreateAnchor("MessageRoot", rimrushConstants.Width2, 236f, hudRoot.transform).gameObject;
            var messageBackdrop = CreateHudImage("MessageBackdrop", rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Popup), rimrushConstants.Width2, 237f, 432f, 118, hudRoot.transform);
            if (messageBackdrop != null)
            {
                messageBackdrop.transform.SetParent(messageRoot.transform, true);
            }

            var messageText = rimrushRender.Text("Message", string.Empty, rimrushConstants.Width2, 238f, 56, new Color32(0x8B, 0x2D, 0xFF, 0xFF), TextAnchor.MiddleCenter, 120, hudRoot.transform, rimrushTextStyle.HudPopup);
            messageText.transform.SetParent(messageRoot.transform, true);

            var bonusNoticeRoot = CreateAnchor("BonusNoticeRoot", 676f, 142f, hudRoot.transform).gameObject;
            var bonusNoticeText = rimrushRender.Text("BonusNotice", string.Empty, 676f, 142f, 16, new Color32(0xFF, 0x7A, 0x39, 0xFF), TextAnchor.MiddleCenter, 119, hudRoot.transform, rimrushTextStyle.TournamentAccent);
            bonusNoticeText.transform.SetParent(bonusNoticeRoot.transform, true);

            var postMatchRoot = new GameObject("PostMatchRoot");
            postMatchRoot.transform.SetParent(hudRoot.transform, false);
            var postMatchTitleText = rimrushRender.Text("PostMatchTitle", string.Empty, rimrushConstants.Width2, 188f, 40, new Color32(0xFF, 0x9C, 0x12, 0xFF), TextAnchor.MiddleCenter, 130, hudRoot.transform, rimrushTextStyle.HudPopup);
            postMatchTitleText.transform.SetParent(postMatchRoot.transform, true);
            var postMatchScoreText = rimrushRender.Text("PostMatchScore", string.Empty, rimrushConstants.Width2, 236f, 24, Color.white, TextAnchor.MiddleCenter, 130, hudRoot.transform, rimrushTextStyle.HudScore);
            postMatchScoreText.transform.SetParent(postMatchRoot.transform, true);
            var postMatchPromptText = rimrushRender.Text("PostMatchPrompt", string.Empty, rimrushConstants.Width2, 276f, 18, new Color32(0xCD, 0xF0, 0x0F, 0xFF), TextAnchor.MiddleCenter, 130, hudRoot.transform, rimrushTextStyle.TournamentAccent);
            postMatchPromptText.transform.SetParent(postMatchRoot.transform, true);

            var pauseOverlayRoot = new GameObject("PauseOverlayRoot");
            pauseOverlayRoot.transform.SetParent(hudRoot.transform, false);
            var pauseShade = CreatePanel("PauseShade", rimrushConstants.Width2, 240f, 800f, 480f, 140, pauseOverlayRoot.transform, new Color(0.01f, 0.03f, 0.05f, 0.78f));
            CreatePanel("PauseTopGlow", rimrushConstants.Width2, 96f, 760f, 104f, 141, pauseOverlayRoot.transform, new Color(0.22f, 0.86f, 0.94f, 0.12f));
            CreatePanel("PauseBottomGlow", rimrushConstants.Width2, 388f, 760f, 132f, 141, pauseOverlayRoot.transform, new Color(0.56f, 0.22f, 0.94f, 0.1f));
            var pausePanel = CreatePanel("PausePanel", rimrushConstants.Width2, 240f, 582f, 308f, 142, pauseOverlayRoot.transform, new Color(0.05f, 0.08f, 0.12f, 0.9f));
            CreateFrame("PauseFrame", "MatchBack0002", rimrushConstants.Width2, 240f, 632f, 332f, 143, pauseOverlayRoot.transform, new Color(0.9f, 0.98f, 1f, 0.96f));
            CreatePanel("PauseBoardTint", rimrushConstants.Width2, 214f, 206f, 72f, 144, pauseOverlayRoot.transform, new Color(0.02f, 0.04f, 0.09f, 0.4f));
            CreateAura("PauseLeftPortraitAura", 230f, 192f, 0.46f, 146, pauseOverlayRoot.transform);
            CreateAura("PauseRightPortraitAura", 570f, 192f, 0.46f, 146, pauseOverlayRoot.transform);
            var pauseLeftPortraitRenderer = CreatePortraitRenderer("PauseLeftPortrait", 147, pauseOverlayRoot.transform);
            rimrushRender.ApplyPixelTransform(pauseLeftPortraitRenderer.transform, 230f, 192f, 0f);
            var pauseRightPortraitRenderer = CreatePortraitRenderer("PauseRightPortrait", 147, pauseOverlayRoot.transform);
            rimrushRender.ApplyPixelTransform(pauseRightPortraitRenderer.transform, 570f, 192f, 0f);
            var pauseTitleText = rimrushRender.Text("PauseTitle", "GAME PAUSED", rimrushConstants.Width2, 100f, 28, new Color32(0xC8, 0xFF, 0x55, 0xFF), TextAnchor.MiddleCenter, 146, pauseOverlayRoot.transform, rimrushTextStyle.DisplayTitle);
            var pauseLeftNameText = rimrushRender.Text("PauseLeftName", string.Empty, 230f, 292f, 16, Color.white, TextAnchor.MiddleCenter, 146, pauseOverlayRoot.transform, rimrushFontKind.RajdhaniBold, outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f), outlinePixels: 0.68f);
            var pauseRightNameText = rimrushRender.Text("PauseRightName", string.Empty, 570f, 292f, 16, Color.white, TextAnchor.MiddleCenter, 146, pauseOverlayRoot.transform, rimrushFontKind.RajdhaniBold, outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.9f), outlinePixels: 0.68f);
            var pauseLeftScoreText = rimrushRender.Text("PauseLeftScore", "0", 353f, 180f, 42, new Color32(0xFF, 0xC2, 0x42, 0xFF), TextAnchor.MiddleCenter, 146, pauseOverlayRoot.transform, rimrushFontKind.CfCrackBold, outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f), outlinePixels: 1.4f);
            var pauseScoreDividerText = rimrushRender.Text("PauseScoreDivider", ":", rimrushConstants.Width2, 179f, 32, new Color32(0x8F, 0xFF, 0xF8, 0xFF), TextAnchor.MiddleCenter, 146, pauseOverlayRoot.transform, rimrushFontKind.CfCrackBold, outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.95f), outlinePixels: 1f);
            var pauseRightScoreText = rimrushRender.Text("PauseRightScore", "0", 447f, 180f, 42, new Color32(0xFF, 0xC2, 0x42, 0xFF), TextAnchor.MiddleCenter, 146, pauseOverlayRoot.transform, rimrushFontKind.CfCrackBold, outlineColor: new Color(0.12f, 0.04f, 0f, 0.95f), outlinePixels: 1.4f);
            var pauseScoreText = rimrushRender.Text("PauseMeta", string.Empty, rimrushConstants.Width2, 320f, 15, new Color32(0xCC, 0xF6, 0xFF, 0xFF), TextAnchor.MiddleCenter, 145, pauseOverlayRoot.transform, rimrushFontKind.RajdhaniBold, outlineColor: new Color(0.03f, 0.05f, 0.1f, 0.88f), outlinePixels: 0.62f);
            var pauseMenuButtonView = rimrushMenuButtonView.CreateRuntimeFallback("PauseMenuButtonView", pauseOverlayRoot.transform);
            _ = new rimrushMenuButton(pauseMenuButtonView, "MENU", 304f, 372f, 156f, 40f, () => { }, 147);
            var pauseResumeButtonView = rimrushMenuButtonView.CreateRuntimeFallback("PauseResumeButtonView", pauseOverlayRoot.transform);
            _ = new rimrushMenuButton(pauseResumeButtonView, "RESUME", 496f, 372f, 188f, 40f, () => { }, 147);

            Assign(sceneView, "scoreboardBackdrop", scoreboardBackdrop);
            Assign(sceneView, "leftPortraitAura", leftPortraitAura);
            Assign(sceneView, "rightPortraitAura", rightPortraitAura);
            Assign(sceneView, "leftPortraitRenderer", leftPortraitRenderer);
            Assign(sceneView, "rightPortraitRenderer", rightPortraitRenderer);
            Assign(sceneView, "leftNameText", leftNameText);
            Assign(sceneView, "rightNameText", rightNameText);
            Assign(sceneView, "leftScoreText", leftScoreText);
            Assign(sceneView, "rightScoreText", rightScoreText);
            Assign(sceneView, "timerText", timerText);
            Assign(sceneView, "pauseButtonView", pauseButtonView);
            Assign(sceneView, "pauseButtonIcon", pauseButtonIcon);
            Assign(sceneView, "musicButtonView", musicButtonView);
            Assign(sceneView, "helpButtonView", helpButtonView);
            Assign(sceneView, "countdownBackdrop", countdownBackdrop);
            Assign(sceneView, "countdownCaptionText", countdownCaptionText);
            Assign(sceneView, "countdownText", countdownText);
            Assign(sceneView, "messageRoot", messageRoot);
            Assign(sceneView, "messageText", messageText);
            Assign(sceneView, "bonusNoticeRoot", bonusNoticeRoot);
            Assign(sceneView, "bonusNoticeText", bonusNoticeText);
            Assign(sceneView, "postMatchRoot", postMatchRoot);
            Assign(sceneView, "postMatchTitleText", postMatchTitleText);
            Assign(sceneView, "postMatchScoreText", postMatchScoreText);
            Assign(sceneView, "postMatchPromptText", postMatchPromptText);
            Assign(sceneView, "pauseOverlayRoot", pauseOverlayRoot);
            Assign(sceneView, "pauseShade", pauseShade);
            Assign(sceneView, "pausePanel", pausePanel);
            Assign(sceneView, "pauseTitleText", pauseTitleText);
            Assign(sceneView, "pauseScoreText", pauseScoreText);
            Assign(sceneView, "pauseLeftNameText", pauseLeftNameText);
            Assign(sceneView, "pauseRightNameText", pauseRightNameText);
            Assign(sceneView, "pauseLeftScoreText", pauseLeftScoreText);
            Assign(sceneView, "pauseRightScoreText", pauseRightScoreText);
            Assign(sceneView, "pauseScoreDividerText", pauseScoreDividerText);
            Assign(sceneView, "pauseLeftPortraitRenderer", pauseLeftPortraitRenderer);
            Assign(sceneView, "pauseRightPortraitRenderer", pauseRightPortraitRenderer);
            Assign(sceneView, "pauseMenuButtonView", pauseMenuButtonView);
            Assign(sceneView, "pauseResumeButtonView", pauseResumeButtonView);

            _ = new rimrushHudView(hudRoot.transform, new rimrushMatchData(true), sceneView);
            return sceneView;
        }

        private static rimrushEnergyBarSceneView CreateEnergyBarView(string name, Transform parent, float x)
        {
            var view = rimrushEnergyBarSceneView.CreateRuntimeFallback(name, parent);
            rimrushRender.ApplyPixelTransform(view.Root.transform, x, 45f, 0f);
            rimrushRender.ApplyPixelTransform(view.OverlayView.Root.transform, x, 45f, 0.13f);
            rimrushRender.ApplyPixelTransform(view.HintBackgroundRenderer.transform, x - 30f, 75f, 0f);
            rimrushRender.ApplyPixelTransform(view.HintText.transform, x - 30f, 77f, 0f);
            return view;
        }

        private static Transform CreateAnchor(string name, float x, float y, Transform parent)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.position = rimrushConstants.PixelToWorldSnapped(x, y);
            return anchor;
        }

        private static void DestroyExistingGameplayRoot(rimrushSceneBindings sceneBindings)
        {
            if (sceneBindings != null && sceneBindings.GameplayBindings != null)
            {
                var root = sceneBindings.GameplayBindings.Root;
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root.gameObject);
                }
            }

            var byName = GameObject.Find("GameplayRoot");
            if (byName != null)
            {
                UnityEngine.Object.DestroyImmediate(byName);
            }
        }

        private static SpriteRenderer CreatePortraitRenderer(string name, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static SpriteRenderer CreateAura(string name, float x, float y, float scale, int sortingOrder, Transform parent)
        {
            var aura = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, "EmblemsBg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            aura.transform.localScale *= scale;
            var renderer = aura.GetComponent<SpriteRenderer>();
            renderer.color = new Color32(0x46, 0xFF, 0xF0, 0x95);
            return renderer;
        }

        private static GameObject CreateHudImage(string name, string resourcePath, float x, float y, float targetWidth, int sortingOrder, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            var image = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
            image.transform.localScale *= targetWidth / Mathf.Max(1f, texture.width);
            return image;
        }

        private static GameObject CreatePauseIcon(string name, float x, float y, int sortingOrder, float targetPixels, Transform parent)
        {
            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.PauseButton));
            if (texture != null)
            {
                var icon = rimrushRender.Image(name, texture, x, y, 0.5f, 0.5f, sortingOrder, parent);
                icon.transform.localScale *= targetPixels / Mathf.Max(1f, Mathf.Max(texture.width, texture.height));
                return icon;
            }

            var fallback = rimrushRender.Sprite($"{name}Fallback", rimrushAtlasCache.Instance.Gameplay, "InGamePauseButton0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            fallback.transform.localScale *= 1.2f;
            return fallback;
        }

        private static GameObject CreatePanel(string name, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var panel = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, "bg0000", x, y, 0.5f, 0.5f, sortingOrder, parent);
            panel.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / 10f,
                rimrushConstants.UnitsPerPixel * height / 10f,
                1f);
            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static GameObject CreateFrame(string name, string frame, float x, float y, float width, float height, int sortingOrder, Transform parent, Color tint)
        {
            var panel = rimrushRender.Sprite(name, rimrushAtlasCache.Instance.Interface, frame, x, y, 0.5f, 0.5f, sortingOrder, parent);
            var atlasFrame = rimrushAtlasCache.Instance.Interface.Frame(frame);
            if (atlasFrame != null)
            {
                panel.transform.localScale = new Vector3(
                    rimrushConstants.UnitsPerPixel * width / Mathf.Max(1f, atlasFrame.SourceW),
                    rimrushConstants.UnitsPerPixel * height / Mathf.Max(1f, atlasFrame.SourceH),
                    1f);
            }

            panel.GetComponent<SpriteRenderer>().color = tint;
            return panel;
        }

        private static void DestroyByNames(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return;
            }

            var targets = Resources.FindObjectsOfTypeAll<GameObject>();
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null)
                {
                    continue;
                }

                for (var nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    if (target.name != names[nameIndex])
                    {
                        continue;
                    }

                    if (EditorUtility.IsPersistent(target))
                    {
                        break;
                    }

                    UnityEngine.Object.DestroyImmediate(target);
                    break;
                }
            }
        }

        private static void Assign(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }

        private static string DescribeSerializedReference(UnityEngine.Object target, string fieldName)
        {
            if (target == null)
            {
                return "<target:null>";
            }

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                return "<missing-property>";
            }

            return DescribeUnityObject(property.objectReferenceValue);
        }

        private static string DescribeNetLines(rimrushBasketView basketView)
        {
            if (basketView == null)
            {
                return "<basket:null>";
            }

            var netLines = basketView.NetLines;
            if (netLines == null)
            {
                return "<net-lines:null>";
            }

            return netLines.Count.ToString();
        }

        private static string DescribeUnityObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return "null";
            }

            return $"{value.name} ({value.GetType().Name})";
        }
    }
}
