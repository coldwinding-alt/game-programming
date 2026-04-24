using System.IO;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CreateInteractiveSolarSystemScene
{
    private const string ScenePath = "Assets/Scenes/InteractiveSolarSystem.unity";
    private const string MaterialFolder = "Assets/Materials";
    private const string AssetRoot = "Assets/SolarSystemAssets2026";

    [MenuItem("Tools/Create Interactive Solar System Scene")]
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialFolder);

        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Texture2D sunTexture = LoadTexture("Textures/SunTexture.jpg");
        Texture2D earthTexture = LoadTexture("Textures/EarthTexture.jpg");
        Texture2D moonTexture = LoadTexture("Textures/MoonTexture.jpg");
        Texture2D spaceTexture = LoadTexture("Textures/SpaceTexture.jpg");

        Material sunMaterial = CreateStandardMaterial("Sun_Material", sunTexture, Color.white, true);
        Material earthMaterial = CreateStandardMaterial("Earth_Material", earthTexture, Color.white, false);
        Material moonMaterial = CreateStandardMaterial("Moon_Material", moonTexture, Color.white, false);
        Material skyboxMaterial = CreateSkyboxMaterial(spaceTexture);

        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.13f);
        RenderSettings.reflectionIntensity = 0.25f;

        GameObject sun = CreatePlanet("Sun", Vector3.zero, Vector3.one * 1.4f, sunMaterial);
        GameObject earth = CreatePlanet("Earth", new Vector3(3.2f, 0f, 0f), Vector3.one * 0.62f, earthMaterial);
        GameObject moon = CreatePlanet("Moon", earth.transform.position + new Vector3(1.05f, 0f, 0f), Vector3.one * 0.28f, moonMaterial);
        moon.transform.SetParent(earth.transform, true);

        AddRotateAround(sun, sun.transform, 5);
        AddRotateAround(earth, earth.transform, 30);
        AddRotateAround(earth, sun.transform, 10);
        AddRotateAround(moon, moon.transform, 24);
        AddRotateAround(moon, earth.transform, 55);

        SolarSystemTarget earthTarget = earth.AddComponent<SolarSystemTarget>();
        earthTarget.displayName = "Earth";
        earthTarget.factText = "Earth is our home planet. It has air, oceans, and life.";
        earthTarget.cameraOffset = new Vector3(0f, 0.45f, -2.05f);

        SolarSystemTarget moonTarget = moon.AddComponent<SolarSystemTarget>();
        moonTarget.displayName = "Moon";
        moonTarget.factText = "The Moon travels around Earth. It shines because sunlight bounces off it.";
        moonTarget.cameraOffset = new Vector3(0f, 0.28f, -1.15f);
        moonTarget.pulseScaleMultiplier = 1.28f;

        AddLights(sun);
        AddAudio(sun, earth);

        Camera mainCamera = CreateMainCamera();
        CreateUi(mainCamera, out Text titleText, out Text factText, out Button backButton);

        GameObject controllerObject = new GameObject("GameController");
        SolarSystemExplorer explorer = controllerObject.AddComponent<SolarSystemExplorer>();
        explorer.mainCamera = mainCamera;
        explorer.titleText = titleText;
        explorer.factText = factText;
        explorer.backButton = backButton;
        explorer.cameraMoveSpeed = 4.5f;
        explorer.cameraTurnSpeed = 6.5f;

        PlayerSettings.companyName = "Game Programming";
        PlayerSettings.productName = "Interactive Solar System for Kids";
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Created " + ScenePath);
    }

    [MenuItem("Tools/Validate Interactive Solar System Scene")]
    public static void ValidateGeneratedScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject sun = RequireObject("Sun");
        GameObject earth = RequireObject("Earth");
        GameObject moon = RequireObject("Moon");
        GameObject cameraObject = RequireObject("Main Camera");
        GameObject controllerObject = RequireObject("GameController");
        RequireObject("Canvas");
        RequireObject("EventSystem");

        Require(sun.GetComponent<Renderer>() != null, "Sun has a renderer.");
        Require(sun.GetComponent<AudioSource>() != null, "Sun has an AudioSource.");
        Require(sun.GetComponentInChildren<Light>() != null, "Sun has a child light.");

        Require(earth.GetComponent<Collider>() != null, "Earth has a collider.");
        Require(earth.GetComponent<SolarSystemTarget>() != null, "Earth is clickable.");
        Require(earth.GetComponents<RotateAround>().Length >= 2, "Earth rotates and orbits.");
        Require(earth.GetComponent<AudioSource>() != null, "Earth has an AudioSource.");

        Require(moon.GetComponent<Collider>() != null, "Moon has a collider.");
        Require(moon.GetComponent<SolarSystemTarget>() != null, "Moon is clickable.");
        Require(moon.GetComponents<RotateAround>().Length >= 2, "Moon rotates and orbits.");

        SolarSystemExplorer explorer = controllerObject.GetComponent<SolarSystemExplorer>();
        Require(explorer != null, "GameController has SolarSystemExplorer.");
        Require(explorer.mainCamera != null, "Explorer has a camera reference.");
        Require(explorer.titleText != null, "Explorer has a title text reference.");
        Require(explorer.factText != null, "Explorer has a fact text reference.");
        Require(explorer.backButton != null, "Explorer has a Back button reference.");

        Require(cameraObject.GetComponent<Camera>() != null, "Main Camera has a Camera component.");
        Require(cameraObject.GetComponent<AudioListener>() != null, "Main Camera has an AudioListener.");

        Debug.Log("VALIDATION_OK: Interactive Solar System scene has required objects, clickable targets, camera UI, motion, lighting, materials, and audio.");
    }

    private static Texture2D LoadTexture(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetRoot + "/" + relativePath);
    }

    private static GameObject RequireObject(string name)
    {
        GameObject found = GameObject.Find(name);
        Require(found != null, "Missing GameObject: " + name);
        return found;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception("Validation failed: " + message);
        }
    }

    private static Material CreateStandardMaterial(string assetName, Texture2D texture, Color color, bool emission)
    {
        string path = MaterialFolder + "/" + assetName + ".mat";
        AssetDatabase.DeleteAsset(path);

        Material material = new Material(Shader.Find("Standard"));
        material.name = assetName;
        material.color = color;

        if (texture != null)
        {
            material.mainTexture = texture;
        }

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.62f, 0.18f) * 1.6f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CreateSkyboxMaterial(Texture2D texture)
    {
        string path = MaterialFolder + "/Space_Skybox.mat";
        AssetDatabase.DeleteAsset(path);

        Shader shader = Shader.Find("Skybox/Panoramic");
        Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
        material.name = "Space_Skybox";

        if (texture != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_Tint"))
        {
            material.SetColor("_Tint", Color.white);
        }

        if (material.HasProperty("_Exposure"))
        {
            material.SetFloat("_Exposure", 1.15f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject CreatePlanet(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = name;
        planet.transform.position = position;
        planet.transform.localScale = scale;

        Renderer renderer = planet.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        return planet;
    }

    private static void AddRotateAround(GameObject targetObject, Transform target, int speed)
    {
        RotateAround rotateAround = targetObject.AddComponent<RotateAround>();
        rotateAround.target = target;
        rotateAround.speed = speed;
    }

    private static void AddLights(GameObject sun)
    {
        GameObject directionalLightObject = new GameObject("Directional Light");
        Light directionalLight = directionalLightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 0.75f;
        directionalLightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

        GameObject sunLightObject = new GameObject("Sun Point Light");
        sunLightObject.transform.SetParent(sun.transform, false);
        sunLightObject.transform.localPosition = Vector3.zero;

        Light sunLight = sunLightObject.AddComponent<Light>();
        sunLight.type = LightType.Point;
        sunLight.range = 9.5f;
        sunLight.intensity = 2.35f;
        sunLight.color = new Color(1f, 0.82f, 0.48f);
    }

    private static void AddAudio(GameObject sun, GameObject earth)
    {
        AudioClip burning = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetRoot + "/Sounds/burning.aif");
        AudioClip droneHum = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetRoot + "/Sounds/dronehum.aif");

        AudioSource sunAudio = sun.AddComponent<AudioSource>();
        sunAudio.clip = burning;
        sunAudio.loop = true;
        sunAudio.playOnAwake = true;
        sunAudio.spatialBlend = 0f;
        sunAudio.volume = 0.28f;

        AudioSource earthAudio = earth.AddComponent<AudioSource>();
        earthAudio.clip = droneHum;
        earthAudio.loop = true;
        earthAudio.playOnAwake = true;
        earthAudio.spatialBlend = 1f;
        earthAudio.minDistance = 0.8f;
        earthAudio.maxDistance = 8f;
        earthAudio.volume = 0.32f;
    }

    private static Camera CreateMainCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 2.25f, -7.6f);
        cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.zero - cameraObject.transform.position, Vector3.up);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 54f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 120f;

        cameraObject.AddComponent<AudioListener>();

        return camera;
    }

    private static void CreateUi(Camera mainCamera, out Text titleText, out Text factText, out Button backButton)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Fact Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.025f, 0.04f, 0.78f);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.05f);
        panelRect.anchorMax = new Vector2(0.95f, 0.25f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", panelObject.transform, font, 30, FontStyle.Bold, TextAnchor.MiddleLeft);
        titleText.color = new Color(1f, 0.88f, 0.48f);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.04f, 0.58f);
        titleRect.anchorMax = new Vector2(0.74f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        factText = CreateText("Fact", panelObject.transform, font, 21, FontStyle.Normal, TextAnchor.UpperLeft);
        factText.color = Color.white;
        factText.horizontalOverflow = HorizontalWrapMode.Wrap;
        factText.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform factRect = factText.GetComponent<RectTransform>();
        factRect.anchorMin = new Vector2(0.04f, 0.12f);
        factRect.anchorMax = new Vector2(0.74f, 0.58f);
        factRect.offsetMin = Vector2.zero;
        factRect.offsetMax = Vector2.zero;

        backButton = CreateButton("Back Button", panelObject.transform, font, "Back");
        RectTransform buttonRect = backButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.79f, 0.2f);
        buttonRect.anchorMax = new Vector2(0.96f, 0.8f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Text CreateText(string name, Transform parent, Font font, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.raycastTarget = false;

        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.42f, 0.95f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.56f, 1f, 1f);
        colors.pressedColor = new Color(0.08f, 0.24f, 0.7f, 1f);
        button.colors = colors;

        Text buttonText = CreateText("Text", buttonObject.transform, font, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        buttonText.text = label;
        buttonText.color = Color.white;

        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }
}
