using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ImprovementProjectBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameLevelScenePath = "Assets/Scenes/GameLevel.unity";
    private const string PickupPrefabPath = "Assets/Prefabs/Gameplay/PowerUpPickup.prefab";
    private const float WorldWidth = 18f;
    private const float WorldHeight = 10f;

    private static Font uiFont;
    private static Sprite buttonSprite;

    public static void BuildAll()
    {
        Directory.CreateDirectory("Assets/Prefabs/Gameplay");
        Directory.CreateDirectory("Assets/Scenes");
        AssetDatabase.Refresh();

        uiFont = LoadAsset<Font>("Assets/Art/UI Elements/Fonts/manaspc/manaspc.ttf");
        buttonSprite = LoadSprite("Assets/Art/UI Elements/Buttons/UIButton.png");
        CreatePickupPrefab();
        CreateMainMenuScene();
        CreateGameLevelScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameLevelScenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreatePickupPrefab()
    {
        Sprite sprite = LoadSprite("Assets/Art/Reticles/Reticle_Blue.png");
        GameObject pickupObject = new GameObject("PowerUpPickup");
        SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 3;
        CircleCollider2D collider = pickupObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;
        PowerUpPickup pickup = pickupObject.AddComponent<PowerUpPickup>();
        pickup.duration = 8f;
        pickup.healAmount = 1;
        pickup.scoreBonus = 5;
        pickup.pickupSound = LoadAsset<AudioClip>("Assets/Audio/Sound Effects/PauseMenu.wav");
        pickup.pickupEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/PlayerProjectileHit.prefab");
        TimedObjectDestroyer destroyer = pickupObject.AddComponent<TimedObjectDestroyer>();
        destroyer.lifetime = 14f;
        PrefabUtility.SaveAsPrefabAsset(pickupObject, PickupPrefabPath);
        Object.DestroyImmediate(pickupObject);
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";
        CreateEventSystem("MenuEventSystem");
        CreateCamera(new Vector3(0f, 0f, -10f), 6f);
        AudioSource menuAudio = CreateMusicSource("Menu Music", LoadAsset<AudioClip>("Assets/Audio/Music/Menu.wav"), 0.35f);

        Canvas canvas = CreateCanvas("Menu Canvas");
        CreateMenuBackground(canvas.transform);

        GameObject navigationObject = new GameObject("SceneNavigation");
        SceneNavigation navigation = navigationObject.AddComponent<SceneNavigation>();
        navigation.gameSceneName = "GameLevel";
        navigation.menuSceneName = "MainMenu";

        GameObject switcherObject = new GameObject("MenuPanelSwitcher");
        MenuPanelSwitcher switcher = switcherObject.AddComponent<MenuPanelSwitcher>();

        RectTransform mainPanel = CreatePanel(canvas.transform, "Main Panel", new Color(0.02f, 0.04f, 0.09f, 0.78f));
        switcher.mainPanel = mainPanel.gameObject;
        CreateText(mainPanel, "Title", "ASTRO DEFENDER", 56, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0f, 0f), new Vector2(760f, 90f), Color.white);
        CreateText(mainPanel, "Subtitle", "Defeat 15 enemies, collect power-ups, and survive the waves.", 24, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.71f), new Vector2(0.5f, 0.71f), Vector2.zero, new Vector2(860f, 60f), new Color(0.8f, 0.92f, 1f));

        Button newGameButton = CreateButton(mainPanel, "New Game Button", "New Game", new Vector2(0.5f, 0.55f));
        UnityEventTools.AddPersistentListener(newGameButton.onClick, navigation.StartNewGame);
        Button instructionsButton = CreateButton(mainPanel, "Instructions Button", "Instructions", new Vector2(0.5f, 0.43f));
        UnityEventTools.AddPersistentListener(instructionsButton.onClick, switcher.ShowInstructions);
        Button creditsButton = CreateButton(mainPanel, "Credits Button", "Credits", new Vector2(0.5f, 0.31f));
        UnityEventTools.AddPersistentListener(creditsButton.onClick, switcher.ShowCredits);
        Button exitButton = CreateButton(mainPanel, "Exit Button", "Exit", new Vector2(0.5f, 0.19f));
        UnityEventTools.AddPersistentListener(exitButton.onClick, navigation.ExitGame);

        RectTransform instructionsPanel = CreateInfoPanel(canvas.transform, "Instructions Panel",
            "INSTRUCTIONS",
            "Move: WASD or Arrow Keys\nAim: Mouse\nFire: Left Mouse Button or Space\n\nGoal: defeat 15 enemies before losing all lives.\nCollect power-ups for rapid fire, shield, repairs, and speed boosts.");
        switcher.instructionsPanel = instructionsPanel.gameObject;
        Button instructionsBack = CreateButton(instructionsPanel, "Back Button", "Back", new Vector2(0.5f, 0.16f));
        UnityEventTools.AddPersistentListener(instructionsBack.onClick, switcher.ShowMain);

        RectTransform creditsPanel = CreateInfoPanel(canvas.transform, "Credits Panel",
            "CREDITS",
            "Starter assets, sprites, sounds, effects, and font are from the provided 2D game asset package.\nFont license is included at Assets/Art/UI Elements/Fonts/manaspc/license.txt.\n\nChanges added for this assignment: menu flow, HUD, power-ups, difficulty scaling, feedback messages, and complete restart/menu flow.");
        switcher.creditsPanel = creditsPanel.gameObject;
        Button creditsBack = CreateButton(creditsPanel, "Back Button", "Back", new Vector2(0.5f, 0.16f));
        UnityEventTools.AddPersistentListener(creditsBack.onClick, switcher.ShowMain);

        menuAudio.Play();
        instructionsPanel.gameObject.SetActive(false);
        creditsPanel.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
    }

    private static void CreateGameLevelScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "GameLevel";
        CreateEventSystem("GameEventSystem");
        CreateCamera(new Vector3(0f, 0f, -10f), 6f);
        AudioSource gameAudio = CreateMusicSource("Level Music", LoadAsset<AudioClip>("Assets/Audio/Music/SongA.wav"), 0.24f);

        GameObject background = CreateWorldSprite("Background", LoadSprite("Assets/Art/Environment/Background/A_CompleteSpaceBackground.png"), Vector3.zero, new Vector3(3.1f, 3.1f, 1f), -10);
        background.transform.position = new Vector3(0f, 0f, 1f);
        CreateWorldSprite("Blue Planet", LoadSprite("Assets/Art/Environment/Planets/Big/BigBluePlanet.png"), new Vector3(-7.2f, 3.7f, 0f), new Vector3(0.8f, 0.8f, 1f), -3);
        CreateWorldSprite("Space Station", LoadSprite("Assets/Art/Environment/Space_Stations/Small_SpaceStation.png"), new Vector3(7.1f, -3.2f, 0f), new Vector3(0.75f, 0.75f, 1f), -2);

        GameObject projectileHolder = new GameObject("ProjectileHolder");
        GameObject enemyHolder = new GameObject("EnemyHolder");
        GameObject powerUpHolder = new GameObject("PowerUpHolder");

        GameObject player = CreatePlayer(projectileHolder.transform);
        GameObject gameManagerObject = new GameObject("GameManager");
        GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
        gameManager.player = player;
        gameManager.gameIsWinnable = true;
        gameManager.enemiesToDefeat = 15;
        gameManager.gameOverPageIndex = 0;
        gameManager.gameVictoryPageIndex = 1;
        gameManager.gameOverEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/Win&Lose/GameOverEffect.prefab");
        gameManager.victoryEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/Win&Lose/VictoryEffect.prefab");

        CreateArenaBounds();
        CreateEnemySpawners(player.transform, projectileHolder.transform, enemyHolder.transform);

        GameObject directorObject = new GameObject("DifficultyDirector");
        directorObject.AddComponent<DifficultyDirector>();

        PowerUpSpawner powerUpSpawner = powerUpHolder.AddComponent<PowerUpSpawner>();
        powerUpSpawner.pickupPrefab = LoadAsset<GameObject>(PickupPrefabPath);
        powerUpSpawner.pickupSprites = new[]
        {
            LoadSprite("Assets/Art/Reticles/Reticle_Blue.png"),
            LoadSprite("Assets/Art/Reticles/Reticle_Gold.png"),
            LoadSprite("Assets/Art/Reticles/Reticle4.png")
        };
        powerUpSpawner.pickupSound = LoadAsset<AudioClip>("Assets/Audio/Sound Effects/PauseMenu.wav");
        powerUpSpawner.pickupEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/PlayerProjectileHit.prefab");
        powerUpSpawner.center = Vector2.zero;
        powerUpSpawner.size = new Vector2(15.5f, 7.8f);
        powerUpSpawner.spawnDelayRange = new Vector2(12f, 16f);

        Canvas canvas = CreateCanvas("Game Canvas");
        CreateHud(canvas.transform, player);
        UIManager uiManager = CreateGamePages(canvas.transform, gameAudio);
        CreateFeedback(canvas.transform, gameAudio);
        uiManager.pauseAction = CreatePauseAction();

        gameAudio.Play();
        EditorSceneManager.SaveScene(scene, GameLevelScenePath);
    }

    private static GameObject CreatePlayer(Transform projectileHolder)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, -3.4f, 0f);
        SpriteRenderer spriteRenderer = player.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = LoadFirstSprite("Assets/Art/Player/Player Sprites.png");
        spriteRenderer.sortingOrder = 2;
        player.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;

        Controller controller = player.AddComponent<Controller>();
        controller.myRigidbody = body;
        controller.moveSpeed = 7.5f;
        controller.aimMode = Controller.AimModes.AimTowardsMouse;
        controller.movementMode = Controller.MovementModes.FreeRoam;
        controller.moveAction = CreateMoveAction();
        controller.lookAction = CreateLookAction();

        Health health = player.AddComponent<Health>();
        health.teamId = 0;
        health.defaultHealth = 3;
        health.maximumHealth = 3;
        health.currentHealth = 3;
        health.useLives = true;
        health.currentLives = 3;
        health.maximumLives = 3;
        health.invincibilityTime = 1.2f;
        health.hitEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/Player/PlayerHitEffect.prefab");
        health.deathEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/Player/PlayerDeathEffect.prefab");

        GameObject gunObject = new GameObject("PlayerGun");
        gunObject.transform.SetParent(player.transform);
        gunObject.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        gunObject.transform.localRotation = Quaternion.identity;
        ShootingController shootingController = gunObject.AddComponent<ShootingController>();
        shootingController.isPlayerControlled = true;
        shootingController.fireAction = CreateFireAction();
        shootingController.fireRate = 0.18f;
        shootingController.projectileSpread = 1f;
        shootingController.projectileHolder = projectileHolder;
        shootingController.projectilePrefab = LoadAsset<GameObject>("Assets/Prefabs/Projectiles/Player_Projectiles/Player_Projectile.prefab");
        shootingController.fireSound = LoadAsset<AudioClip>("Assets/Audio/Sound Effects/PlayerFire.wav");
        shootingController.fireEffect = LoadAsset<GameObject>("Assets/Prefabs/Effects/PlayerProjectileHitV2.prefab");

        PlayerPowerUpController powerUpController = player.AddComponent<PlayerPowerUpController>();
        powerUpController.movementController = controller;
        powerUpController.playerHealth = health;
        powerUpController.shootingControllers = new[] { shootingController };
        return player;
    }

    private static void CreateEnemySpawners(Transform player, Transform projectileHolder, Transform enemyHolder)
    {
        CreateSpawner("Straight Shooter Spawner", "Assets/Prefabs/Enemies/Spawners/EnemySpawnerStraight.prefab", new Vector3(-6f, 3.8f, 0f), player, projectileHolder, enemyHolder, 2.7f, 6f, 2.8f);
        CreateSpawner("Chaser Spawner", "Assets/Prefabs/Enemies/Spawners/EnemySpawnerChaser.prefab", new Vector3(6.1f, 3.6f, 0f), player, projectileHolder, enemyHolder, 4.1f, 4.8f, 2.4f);
        CreateSpawner("Diagonal Shooter Spawner", "Assets/Prefabs/Enemies/Spawners/EnemySpawnerDiagonal.prefab", new Vector3(0f, 4.3f, 0f), player, projectileHolder, enemyHolder, 3.5f, 7f, 1.8f);
    }

    private static void CreateSpawner(string name, string prefabPath, Vector3 position, Transform target, Transform projectileHolder, Transform parent, float delay, float rangeX, float rangeY)
    {
        GameObject prefab = LoadAsset<GameObject>(prefabPath);
        GameObject spawnerObject = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : new GameObject(name);
        spawnerObject.name = name;
        spawnerObject.transform.position = position;
        spawnerObject.transform.SetParent(parent);
        EnemySpawner spawner = spawnerObject.GetComponent<EnemySpawner>();
        if (spawner != null)
        {
            spawner.target = target;
            spawner.projectileHolder = projectileHolder;
            spawner.spawnInfinite = true;
            spawner.spawnDelay = delay;
            spawner.spawnRangeX = rangeX;
            spawner.spawnRangeY = rangeY;
        }
    }

    private static void CreateArenaBounds()
    {
        CreateWall("Top Boundary", new Vector2(0f, WorldHeight * 0.5f + 0.35f), new Vector2(WorldWidth, 0.5f));
        CreateWall("Bottom Boundary", new Vector2(0f, -WorldHeight * 0.5f - 0.35f), new Vector2(WorldWidth, 0.5f));
        CreateWall("Left Boundary", new Vector2(-WorldWidth * 0.5f - 0.35f, 0f), new Vector2(0.5f, WorldHeight));
        CreateWall("Right Boundary", new Vector2(WorldWidth * 0.5f + 0.35f, 0f), new Vector2(0.5f, WorldHeight));
    }

    private static void CreateWall(string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void CreateHud(Transform canvasTransform, GameObject player)
    {
        RectTransform hudPanel = CreatePanel(canvasTransform, "HUD", new Color(0.01f, 0.02f, 0.04f, 0.58f));
        hudPanel.anchorMin = new Vector2(0f, 1f);
        hudPanel.anchorMax = new Vector2(0f, 1f);
        hudPanel.pivot = new Vector2(0f, 1f);
        hudPanel.anchoredPosition = new Vector2(22f, -20f);
        hudPanel.sizeDelta = new Vector2(480f, 170f);

        Text scoreText = CreateText(hudPanel, "Score Text", "Score: 0", 23, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -20f), new Vector2(-48f, 34f), Color.white);
        Text livesText = CreateText(hudPanel, "Lives Text", "Lives: 3  Health: 3/3", 23, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -58f), new Vector2(-48f, 34f), new Color(0.8f, 1f, 0.85f));
        Text objectiveText = CreateText(hudPanel, "Objective Text", "Objective: Defeat 15 enemies  0/15", 21, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -96f), new Vector2(-48f, 34f), new Color(1f, 0.92f, 0.65f));
        Text powerUpText = CreateText(hudPanel, "PowerUp Text", "Power-up: none", 20, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -132f), new Vector2(-48f, 34f), new Color(0.72f, 0.92f, 1f));

        HUDController hudController = hudPanel.gameObject.AddComponent<HUDController>();
        hudController.scoreText = scoreText;
        hudController.livesText = livesText;
        hudController.objectiveText = objectiveText;
        hudController.powerUpText = powerUpText;
        hudController.playerHealth = player.GetComponent<Health>();
        hudController.playerPowerUpController = player.GetComponent<PlayerPowerUpController>();

        Text reminder = CreateText(canvasTransform, "Objective Reminder", "Defeat 15 enemies. Collect power-ups. Survive.", 24, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(760f, 44f), new Color(0.86f, 0.94f, 1f));
        reminder.fontStyle = FontStyle.Bold;
    }

    private static UIManager CreateGamePages(Transform canvasTransform, AudioSource audioSource)
    {
        GameObject uiManagerObject = new GameObject("UIManager");
        UIManager uiManager = uiManagerObject.AddComponent<UIManager>();
        uiManager.allowPause = true;
        uiManager.pausePageIndex = 2;
        uiManager.currentPage = 0;
        uiManager.defaultPage = 0;

        GameObject navigationObject = new GameObject("SceneNavigation");
        SceneNavigation navigation = navigationObject.AddComponent<SceneNavigation>();
        navigation.gameSceneName = "GameLevel";
        navigation.menuSceneName = "MainMenu";

        RectTransform gameOverPanel = CreateOverlayPage(canvasTransform, "Game Over Screen", "MISSION FAILED", "Your ship was destroyed. Try again and collect power-ups earlier.");
        Button retryFromGameOver = CreateButton(gameOverPanel, "Retry Button", "Retry", new Vector2(0.5f, 0.38f));
        UnityEventTools.AddPersistentListener(retryFromGameOver.onClick, navigation.RestartGame);
        Button menuFromGameOver = CreateButton(gameOverPanel, "Menu Button", "Main Menu", new Vector2(0.5f, 0.27f));
        UnityEventTools.AddPersistentListener(menuFromGameOver.onClick, navigation.ReturnToMenu);
        PanelAudioCue gameOverAudio = gameOverPanel.gameObject.AddComponent<PanelAudioCue>();
        gameOverAudio.audioSource = audioSource;
        gameOverAudio.clip = LoadAsset<AudioClip>("Assets/Audio/Sound Effects/GameOver.wav");

        RectTransform victoryPanel = CreateOverlayPage(canvasTransform, "Victory Screen", "SECTOR CLEAR", "You defeated 15 enemies and survived the attack wave.");
        Button retryFromVictory = CreateButton(victoryPanel, "Play Again Button", "Play Again", new Vector2(0.5f, 0.38f));
        UnityEventTools.AddPersistentListener(retryFromVictory.onClick, navigation.RestartGame);
        Button menuFromVictory = CreateButton(victoryPanel, "Menu Button", "Main Menu", new Vector2(0.5f, 0.27f));
        UnityEventTools.AddPersistentListener(menuFromVictory.onClick, navigation.ReturnToMenu);
        PanelAudioCue victoryAudio = victoryPanel.gameObject.AddComponent<PanelAudioCue>();
        victoryAudio.audioSource = audioSource;
        victoryAudio.clip = LoadAsset<AudioClip>("Assets/Audio/Sound Effects/GameWin.wav");

        RectTransform pausePanel = CreateOverlayPage(canvasTransform, "Pause Screen", "PAUSED", "Take a breath. The wave waits until you return.");
        Button resumeButton = CreateButton(pausePanel, "Resume Button", "Resume", new Vector2(0.5f, 0.43f));
        UnityEventTools.AddPersistentListener(resumeButton.onClick, uiManager.TogglePause);
        Button retryFromPause = CreateButton(pausePanel, "Retry Button", "Retry", new Vector2(0.5f, 0.32f));
        UnityEventTools.AddPersistentListener(retryFromPause.onClick, navigation.RestartGame);
        Button menuFromPause = CreateButton(pausePanel, "Menu Button", "Main Menu", new Vector2(0.5f, 0.21f));
        UnityEventTools.AddPersistentListener(menuFromPause.onClick, navigation.ReturnToMenu);

        UIPage gameOverPage = gameOverPanel.gameObject.AddComponent<UIPage>();
        gameOverPage.defaultSelected = retryFromGameOver.gameObject;
        UIPage victoryPage = victoryPanel.gameObject.AddComponent<UIPage>();
        victoryPage.defaultSelected = retryFromVictory.gameObject;
        UIPage pausePage = pausePanel.gameObject.AddComponent<UIPage>();
        pausePage.defaultSelected = resumeButton.gameObject;
        uiManager.pages = new List<UIPage> { gameOverPage, victoryPage, pausePage };

        gameOverPanel.gameObject.SetActive(false);
        victoryPanel.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        return uiManager;
    }

    private static void CreateFeedback(Transform canvasTransform, AudioSource audioSource)
    {
        Text messageText = CreateText(canvasTransform, "Feedback Message", "", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(860f, 60f), new Color(0.98f, 0.94f, 0.68f));
        messageText.fontStyle = FontStyle.Bold;
        FeedbackMessenger messenger = messageText.gameObject.AddComponent<FeedbackMessenger>();
        messenger.messageText = messageText;
        messenger.audioSource = audioSource;
        messenger.messageSound = LoadAsset<AudioClip>("Assets/Audio/Sound Effects/EnemyHit.wav");
    }

    private static RectTransform CreateInfoPanel(Transform parent, string name, string heading, string body)
    {
        RectTransform panel = CreatePanel(parent, name, new Color(0.02f, 0.04f, 0.09f, 0.88f));
        CreateText(panel, "Heading", heading, 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(760f, 70f), Color.white);
        CreateText(panel, "Body", body, 25, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 230f), new Color(0.86f, 0.94f, 1f));
        return panel;
    }

    private static RectTransform CreateOverlayPage(Transform parent, string name, string heading, string body)
    {
        RectTransform panel = CreatePanel(parent, name, new Color(0.01f, 0.02f, 0.04f, 0.87f));
        CreateText(panel, "Heading", heading, 50, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(760f, 78f), Color.white);
        CreateText(panel, "Body", body, 24, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.51f), new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(760f, 76f), new Color(0.85f, 0.94f, 1f));
        return panel;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateMenuBackground(Transform canvasTransform)
    {
        GameObject background = new GameObject("Background");
        background.transform.SetParent(canvasTransform, false);
        Image image = background.AddComponent<Image>();
        image.sprite = LoadSprite("Assets/Art/Environment/Background/B_CompleteSpaceBackground.png");
        image.color = new Color(0.72f, 0.8f, 1f, 1f);
        RectTransform rectTransform = background.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return rectTransform;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = new Color(0.12f, 0.42f, 0.68f, 0.95f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.42f, 0.68f, 0.95f);
        colors.highlightedColor = new Color(0.2f, 0.62f, 0.9f, 1f);
        colors.pressedColor = new Color(0.08f, 0.28f, 0.48f, 1f);
        colors.selectedColor = new Color(0.2f, 0.62f, 0.9f, 1f);
        button.colors = colors;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(330f, 72f);
        rectTransform.anchoredPosition = Vector2.zero;

        Text text = CreateText(rectTransform, "Label", label, 26, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
        text.fontStyle = FontStyle.Bold;
        return button;
    }

    private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.fontSize = size;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        if (uiFont != null)
        {
            uiText.font = uiFont;
        }

        RectTransform rectTransform = uiText.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = sizeDelta;
        return uiText;
    }

    private static GameObject CreateWorldSprite(string name, Sprite sprite, Vector3 position, Vector3 scale, int sortingOrder)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        SpriteRenderer spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = sortingOrder;
        return gameObject;
    }

    private static Camera CreateCamera(Vector3 position, float size)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = position;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.015f, 0.03f, 1f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static AudioSource CreateMusicSource(string name, AudioClip clip, float volume)
    {
        GameObject musicObject = new GameObject(name);
        AudioSource audioSource = musicObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = volume;
        return audioSource;
    }

    private static void CreateEventSystem(string name)
    {
        GameObject eventSystemObject = new GameObject(name);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static InputAction CreateMoveAction()
    {
        InputAction action = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        return action;
    }

    private static InputAction CreateLookAction()
    {
        InputAction action = new InputAction("Look", InputActionType.Value, "<Mouse>/position", expectedControlType: "Vector2");
        return action;
    }

    private static InputAction CreateFireAction()
    {
        InputAction action = new InputAction("Fire", InputActionType.Button);
        action.AddBinding("<Mouse>/leftButton");
        action.AddBinding("<Keyboard>/space");
        return action;
    }

    private static InputAction CreatePauseAction()
    {
        InputAction action = new InputAction("Pause", InputActionType.Button);
        action.AddBinding("<Keyboard>/escape");
        action.AddBinding("<Keyboard>/p");
        return action;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }
        return LoadFirstSprite(path);
    }

    private static Sprite LoadFirstSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }
        return null;
    }

    private static T LoadAsset<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
