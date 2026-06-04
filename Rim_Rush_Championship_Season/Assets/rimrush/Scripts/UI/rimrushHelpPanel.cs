// 帮助面板（按键说明和游戏规则）
// 玩家按帮助按钮后弹出，有键盘操作和游戏规则两个页面。
// 键盘页面可以点选不同动作，女巫角色会播放对应的演示动画。

using TMPro;
using UnityEngine;

namespace rimrush
{
    public enum rimrushHelpPage
    {
        Keyboard,
        Rules
    }

    public enum rimrushHelpDemo
    {
        Move,
        Jump,
        Shoot,
        Pump,
        Dash,
        Steal,
        Block
    }

    public enum rimrushHelpButtonAction
    {
        Close,
        KeyboardTab,
        RulesTab,
        ReplayTutorial,
        DemoMove,
        DemoJump,
        DemoShoot,
        DemoPump,
        DemoDash,
        DemoSteal,
        DemoBlock,
        QuickTestToggle,
        QuickTestInfoToggle
    }

    [ExecuteAlways]
    public sealed class rimrushHelpPanel : MonoBehaviour
    {
        private const string PrefabResourcePath = "rimrush/Prefabs/UI/RimrushHelpPanel";
        private const int WitchCharacterId = 6;
        private const int WitchSortingOrderBase = 920;
        private const float DemoRepeatMove = 999f;
        private const float DemoRepeatJump = 0.72f;
        private const float DemoRepeatShoot = 0.9f;
        private const float DemoRepeatPump = 0.5f;
        private const float DemoRepeatDash = 0.55f;
        private const float DemoRepeatSteal = 0.82f;
        private const float DemoRepeatBlock = 0.55f;
#if UNITY_EDITOR
        private const string EditorWitchPreviewName = "WitchEditorPreview";
#endif

        private static rimrushHelpPanel activePanel;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject keyboardPageRoot;
        [SerializeField] private GameObject rulesPageRoot;
        [SerializeField] private rimrushHelpButton[] buttons;
        [SerializeField] private SpriteRenderer keyboardTabPlate;
        [SerializeField] private SpriteRenderer rulesTabPlate;
        [SerializeField] private TMP_Text keyboardTabText;
        [SerializeField] private TMP_Text rulesTabText;
        [SerializeField] private SpriteRenderer[] demoRowPlates;
        [SerializeField] private TMP_Text demoTitleText;
        [SerializeField] private TMP_Text demoDescriptionText;
        [SerializeField] private TMP_Text demoCoachText;
        [SerializeField] private Transform witchMount;
        [SerializeField] private SpriteRenderer witchSpotlight;
        [SerializeField] private rimrushHelpButton quickTestToggleButton;
        [SerializeField] private SpriteRenderer quickTestTogglePlate;
        [SerializeField] private TMP_Text quickTestToggleText;
        [SerializeField] private rimrushHelpButton quickTestInfoButton;
        [SerializeField] private GameObject quickTestInfoRoot;
        [SerializeField] private TMP_Text quickTestInfoText;

        private DBLiteArmature witchArmature;
        private rimrushHelpPage currentPage = rimrushHelpPage.Keyboard;
        private rimrushHelpDemo currentDemo = rimrushHelpDemo.Block;
        private bool initialized;
        private bool visible;
        private float panelTime;
        private float demoTimer;
        private bool demoToggle;
        private bool quickTestInfoVisible;
#if UNITY_EDITOR
        private GameObject editorWitchPreviewRoot;
#endif

        public bool IsVisible => visible;

        public static bool IsAnyOpen
        {
            get
            {
                var panel = activePanel != null ? activePanel : FindScenePanel();
                return panel != null && panel.visible;
            }
        }

        /// <summary>
        /// Open the help panel to the keyboard page. Creates one if none exists.
        /// </summary>
        public static void ShowKeyboardPage()
        {
            var panel = FindActivePanel(createFallback: true);
            if (panel != null)
            {
                panel.Show(rimrushHelpPage.Keyboard);
            }
        }

        /// <summary>
        /// Close whichever help panel is currently open.
        /// </summary>
        public static void HideActive()
        {
            var panel = activePanel != null ? activePanel : FindScenePanel();
            if (panel != null)
            {
                panel.Hide();
            }
        }

        /// <summary>
        /// Remember this panel as the active one when the game starts.
        /// </summary>
        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (activePanel == null || activePanel == this)
                {
                    activePanel = this;
                }

#if UNITY_EDITOR
                DestroyEditorWitchPreview();
#endif
                HideImmediate();
            }
#if UNITY_EDITOR
            else
            {
                ApplyEditorPreviewState();
            }
#endif
        }

        /// <summary>
        /// Update the editor preview when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyEditorPreviewState();
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Refresh the editor preview when a value changes in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall -= ApplyEditorPreviewState;
            UnityEditor.EditorApplication.delayCall += ApplyEditorPreviewState;
        }
#endif

        /// <summary>
        /// Clear the active panel reference when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (activePanel == this)
            {
                activePanel = null;
            }
        }

        /// <summary>
        /// Each frame: animate the panel, check buttons, update the demo, and handle Escape to close.
        /// </summary>
        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyEditorPreviewState();
                return;
            }
#endif
            if (!visible)
            {
                return;
            }

            panelTime += Time.unscaledDeltaTime;
            UpdatePanelEntrance();
            UpdateButtons();
            UpdateDemo(Time.unscaledDeltaTime);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        /// <summary>
        /// Make sure the witch character always renders on top of other sprites.
        /// </summary>
        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ApplyWitchSortingOrder(editorWitchPreviewRoot != null
                    ? editorWitchPreviewRoot.GetComponent<DBLiteArmature>()
                    : null);
                return;
            }
#endif
            if (visible)
            {
                ApplyWitchSortingOrder();
            }
        }

        /// <summary>
        /// Show the single help panel page.
        /// </summary>
        public void Show(rimrushHelpPage page)
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            visible = true;
            panelRoot.SetActive(true);
            panelTime = 0f;
            transform.localScale = Vector3.one * 0.985f;

            EnsureInitialized();
            SetPage(rimrushHelpPage.Keyboard);
            SetQuickTestInfoVisible(false);
            SelectDemo(rimrushHelpDemo.Block, forceRestart: true);
        }

        /// <summary>
        /// Hide the help panel. Plays a button sound by default.
        /// </summary>
        public void Hide(bool playSound = true)
        {
            if (!visible)
            {
                return;
            }

            visible = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (playSound)
            {
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.Button, 0.55f);
            }
        }

        /// <summary>
        /// Hide the panel right away without playing any sound (used on startup).
        /// </summary>
        private void HideImmediate()
        {
            visible = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Set up the witch model and pages the first time the panel is shown.
        /// </summary>
        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            HideLegacyTabs();
            EnsureQuickTestToggle();
            BuildWitchPreview();
            SetPage(currentPage);
            SelectDemo(currentDemo, forceRestart: true);
        }

        /// <summary>
        /// Create the witch character model so it can play demo animations.
        /// </summary>
        private void BuildWitchPreview()
        {
            if (witchMount == null || witchArmature != null)
            {
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                DestroyEditorWitchPreview();
            }
#endif
            witchArmature = rimrushPlayersData.BuildGameplayArmature("HelpWitchPreview");
            if (witchArmature == null)
            {
                return;
            }

            witchArmature.transform.SetParent(witchMount, false);
            witchArmature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                witchMount,
                new Vector3(0f, -35f, 0f));
            witchArmature.transform.localScale = new Vector3(
                rimrushConstants.PixelPerfectCharacterScale * 1.22f,
                rimrushConstants.PixelPerfectCharacterScale * 1.22f,
                1f);
            rimrushPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
            HidePreviewBall(witchArmature);
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// Animate the panel opening with a quick scale bounce and a subtle spotlight pulse.
        /// </summary>
        private void UpdatePanelEntrance()
        {
            var t = Mathf.Clamp01(panelTime / 0.12f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, eased);

            if (witchSpotlight != null)
            {
                var pulse = Mathf.Sin(Time.unscaledTime * 2.3f) * 0.025f;
                witchSpotlight.transform.localScale = Vector3.one * (1f + pulse);
            }
        }

        /// <summary>
        /// Check mouse clicks on every button each frame and route the hit to HandleButton.
        /// </summary>
        private void UpdateButtons()
        {
            var camera = Camera.main;
            if (buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null || !button.Tick(camera))
                {
                    continue;
                }

                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.Button, 0.75f);
                HandleButton(button.Action);
            }
        }

        /// <summary>
        /// Route a button click to the right action (close, switch tab, select demo, etc.).
        /// </summary>
        private void HandleButton(rimrushHelpButtonAction action)
        {
            switch (action)
            {
                case rimrushHelpButtonAction.Close:
                    Hide(playSound: false);
                    break;
                case rimrushHelpButtonAction.KeyboardTab:
                    SetPage(rimrushHelpPage.Keyboard);
                    break;
                case rimrushHelpButtonAction.RulesTab:
                    SetPage(rimrushHelpPage.Keyboard);
                    break;
                case rimrushHelpButtonAction.ReplayTutorial:
                    HandleReplayTutorialRequest();
                    break;
                case rimrushHelpButtonAction.DemoMove:
                    SelectDemo(rimrushHelpDemo.Move);
                    break;
                case rimrushHelpButtonAction.DemoJump:
                    SelectDemo(rimrushHelpDemo.Jump);
                    break;
                case rimrushHelpButtonAction.DemoShoot:
                    SelectDemo(rimrushHelpDemo.Shoot);
                    break;
                case rimrushHelpButtonAction.DemoPump:
                    SelectDemo(rimrushHelpDemo.Pump);
                    break;
                case rimrushHelpButtonAction.DemoDash:
                    SelectDemo(rimrushHelpDemo.Dash);
                    break;
                case rimrushHelpButtonAction.DemoSteal:
                    SelectDemo(rimrushHelpDemo.Steal);
                    break;
                case rimrushHelpButtonAction.DemoBlock:
                    SelectDemo(rimrushHelpDemo.Block);
                    break;
                case rimrushHelpButtonAction.QuickTestToggle:
                    rimrushQuickTestSettings.Enabled = !rimrushQuickTestSettings.Enabled;
                    RefreshQuickTestToggle();
                    UpdateDemoSelections();
                    break;
                case rimrushHelpButtonAction.QuickTestInfoToggle:
                    SetQuickTestInfoVisible(!quickTestInfoVisible);
                    UpdateDemoSelections();
                    break;
            }
        }

        private void HandleReplayTutorialRequest()
        {
            if (!rimrushGameBootstrap.TryStartTutorialFromHelp())
            {
                Debug.LogWarning("Could not find rimrushGameBootstrap to launch the tutorial from the help panel.");
                return;
            }

            Hide(playSound: false);
        }

        /// <summary>
        /// Keep the help panel on the single controls page.
        /// </summary>
        private void SetPage(rimrushHelpPage page)
        {
            currentPage = rimrushHelpPage.Keyboard;
            if (keyboardPageRoot != null)
            {
                keyboardPageRoot.SetActive(true);
            }

            if (rulesPageRoot != null)
            {
                rulesPageRoot.SetActive(false);
            }

            HideLegacyTabs();
            RefreshQuickTestToggle();

            if (buttons != null)
            {
                for (var i = 0; i < buttons.Length; i++)
                {
                    var button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    button.SetSelected(
                        button.Action == rimrushHelpButtonAction.QuickTestToggle && rimrushQuickTestSettings.Enabled ||
                        button.Action == rimrushHelpButtonAction.QuickTestInfoToggle && quickTestInfoVisible ||
                        IsDemoButtonSelected(button.Action));
                }
            }
        }

        /// <summary>
        /// Highlight the selected tab by changing its plate and text colors.
        /// </summary>
        private static void SetTabVisual(SpriteRenderer plate, TMP_Text label, bool selected)
        {
            if (plate != null)
            {
                plate.color = selected
                    ? new Color32(0xFF, 0xB3, 0x38, 0xFF)
                    : new Color32(0x28, 0x36, 0x52, 0xF2);
            }

            if (label != null)
            {
                label.color = selected
                    ? new Color32(0x24, 0x12, 0x07, 0xFF)
                    : new Color32(0xE9, 0xF3, 0xFF, 0xFF);
            }
        }

        private void HideLegacyTabs()
        {
            SetButtonRootActive(keyboardTabPlate, false);
            SetButtonRootActive(rulesTabPlate, false);
        }

        private void EnsureQuickTestToggle()
        {
            if (panelRoot == null || !Application.isPlaying)
            {
                return;
            }

            var tab = Resources.Load<Sprite>("rimrush/Help/help_tab");
            var card = Resources.Load<Sprite>("rimrush/Help/help_card");
            if (quickTestToggleButton == null)
            {
                var root = new GameObject("QuickTestToggle");
                root.transform.SetParent(panelRoot.transform, false);

                const float x = 604f;
                const float y = 58f;
                const float width = 160f;
                const float height = 34f;
                quickTestTogglePlate = AddRuntimeSprite("QuickTestTogglePlate", tab, x, y, 0.84f, width, height, 915, root.transform);
                quickTestToggleText = rimrushRender.TmpText(
                    "QuickTestToggleLabel",
                    string.Empty,
                    x,
                    y + 1f,
                    10,
                    new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                    TextAnchor.MiddleCenter,
                    935,
                    root.transform,
                    rimrushTextStyle.TournamentAccent);

                quickTestToggleButton = root.AddComponent<rimrushHelpButton>();
                quickTestToggleButton.Configure(
                    rimrushHelpButtonAction.QuickTestToggle,
                    new Vector2(x, y),
                    new Vector2(width, height),
                    root.transform,
                    new[] { quickTestTogglePlate },
                    new[] { quickTestToggleText },
                    new Color32(0x22, 0x30, 0x4C, 0xF2),
                    new Color32(0x36, 0x4C, 0x70, 0xFF),
                    new Color32(0x2D, 0xE6, 0xA3, 0xEE),
                    new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                    new Color32(0xFF, 0xD6, 0x6A, 0xFF),
                    new Color32(0x14, 0x1B, 0x25, 0xFF),
                    1.035f);
                AppendButton(quickTestToggleButton);
            }

            if (quickTestInfoButton == null)
            {
                var infoRoot = new GameObject("QuickTestInfoButton");
                infoRoot.transform.SetParent(panelRoot.transform, false);
                const float infoX = 704f;
                const float infoY = 58f;
                const float infoSize = 26f;
                var infoPlate = AddRuntimeSprite("QuickTestInfoButtonPlate", tab, infoX, infoY, 0.84f, infoSize, infoSize, 916, infoRoot.transform);
                var infoLabel = rimrushRender.TmpText(
                    "QuickTestInfoButtonLabel",
                    "?",
                    infoX,
                    infoY + 1f,
                    13,
                    new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                    TextAnchor.MiddleCenter,
                    936,
                    infoRoot.transform,
                    rimrushTextStyle.TournamentAccent);

                quickTestInfoButton = infoRoot.AddComponent<rimrushHelpButton>();
                quickTestInfoButton.Configure(
                    rimrushHelpButtonAction.QuickTestInfoToggle,
                    new Vector2(infoX, infoY),
                    new Vector2(infoSize, infoSize),
                    infoRoot.transform,
                    new[] { infoPlate },
                    new[] { infoLabel },
                    new Color32(0x22, 0x30, 0x4C, 0xF2),
                    new Color32(0x36, 0x4C, 0x70, 0xFF),
                    new Color32(0x2D, 0xE6, 0xA3, 0xEE),
                    new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                    new Color32(0xFF, 0xD6, 0x6A, 0xFF),
                    new Color32(0x14, 0x1B, 0x25, 0xFF),
                    1.035f);
                AppendButton(quickTestInfoButton);
            }

            if (quickTestInfoRoot == null)
            {
                quickTestInfoRoot = new GameObject("QuickTestInfoPanel");
                quickTestInfoRoot.transform.SetParent(panelRoot.transform, false);
                AddRuntimeSprite("QuickTestInfoPanelPlate", card, 596f, 113f, 0.842f, 292f, 72f, 912, quickTestInfoRoot.transform);
                quickTestInfoText = rimrushRender.TmpText(
                    "QuickTestInfoText",
                    QuickTestInfoCopy(),
                    466f,
                    113f,
                    9,
                    new Color32(0xF4, 0xF7, 0xFF, 0xFF),
                    TextAnchor.MiddleLeft,
                    936,
                    quickTestInfoRoot.transform,
                    rimrushTextStyle.TournamentBody);
            }

            SetQuickTestInfoVisible(false);
        }

        private void RefreshQuickTestToggle()
        {
            SetText(quickTestToggleText, rimrushQuickTestSettings.Enabled ? "FAST TEST: ON" : "FAST TEST: OFF");
            quickTestToggleButton?.SetSelected(rimrushQuickTestSettings.Enabled);
        }

        private void SetQuickTestInfoVisible(bool visible)
        {
            quickTestInfoVisible = visible;
            if (quickTestInfoRoot != null)
            {
                quickTestInfoRoot.SetActive(visible);
            }

            quickTestInfoButton?.SetSelected(visible);
        }

        private static string QuickTestInfoCopy()
        {
            return "DEV / REVIEW TEST\n15s matches + no skill cooldowns.\nQuickly try the full game flow.";
        }

        private void AppendButton(rimrushHelpButton button)
        {
            if (button == null)
            {
                return;
            }

            if (buttons == null)
            {
                buttons = new[] { button };
                return;
            }

            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == button)
                {
                    return;
                }
            }

            var length = buttons.Length;
            System.Array.Resize(ref buttons, length + 1);
            buttons[length] = button;
        }

        private static void SetButtonRootActive(SpriteRenderer plate, bool active)
        {
            if (plate == null)
            {
                return;
            }

            var root = plate.transform.parent != null ? plate.transform.parent.gameObject : plate.gameObject;
            if (root != null)
            {
                root.SetActive(active);
            }
        }

        private static SpriteRenderer AddRuntimeSprite(string name, Sprite sprite, float x, float y, float z, float width, float height, int sortingOrder, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            var spriteWidth = sprite != null ? Mathf.Max(1f, sprite.rect.width) : 1f;
            var spriteHeight = sprite != null ? Mathf.Max(1f, sprite.rect.height) : 1f;
            go.transform.position = rimrushConstants.PixelToWorldSnapped(x, y, z);
            go.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * width / spriteWidth,
                rimrushConstants.UnitsPerPixel * height / spriteHeight,
                1f);
            return renderer;
        }

        /// <summary>
        /// Choose which move to demonstrate (move, jump, shoot, pump, dash, steal, block).
        /// </summary>
        private void SelectDemo(rimrushHelpDemo demo, bool forceRestart = false)
        {
            if (!forceRestart && currentDemo == demo)
            {
                RestartDemoAnimation();
                return;
            }

            currentDemo = demo;
            demoTimer = 999f;
            demoToggle = false;
            UpdateDemoCopy();
            UpdateDemoSelections();
            RestartDemoAnimation();
        }

        /// <summary>
        /// Restart the demo animation when the timer runs out.
        /// </summary>
        private void UpdateDemo(float dt)
        {
            if (witchArmature == null || currentPage != rimrushHelpPage.Keyboard)
            {
                return;
            }

            demoTimer += dt;
            var repeat = DemoRepeatFor(currentDemo);
            if (demoTimer >= repeat)
            {
                RestartDemoAnimation();
            }
        }

        /// <summary>
        /// Play the witch animation for the currently selected demo.
        /// </summary>
        private void RestartDemoAnimation()
        {
            if (witchArmature == null)
            {
                return;
            }

            demoTimer = 0f;
            switch (currentDemo)
            {
                case rimrushHelpDemo.Move:
                    witchArmature.Play("run");
                    break;
                case rimrushHelpDemo.Jump:
                    witchArmature.Play("jump");
                    break;
                case rimrushHelpDemo.Shoot:
                    witchArmature.Play("throw_land");
                    break;
                case rimrushHelpDemo.Pump:
                    witchArmature.Play(demoToggle ? "pumpEnd" : "pumpStart");
                    demoToggle = !demoToggle;
                    break;
                case rimrushHelpDemo.Dash:
                    witchArmature.Play("dash");
                    break;
                case rimrushHelpDemo.Steal:
                    witchArmature.Play("steal");
                    break;
                case rimrushHelpDemo.Block:
                    witchArmature.Play(demoToggle ? "blockEnd" : "blockStart");
                    demoToggle = !demoToggle;
                    break;
            }

            HidePreviewBall(witchArmature);
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// Return how long (in seconds) each demo animation lasts before repeating.
        /// </summary>
        private static float DemoRepeatFor(rimrushHelpDemo demo)
        {
            switch (demo)
            {
                case rimrushHelpDemo.Move:
                    return DemoRepeatMove;
                case rimrushHelpDemo.Jump:
                    return DemoRepeatJump;
                case rimrushHelpDemo.Shoot:
                    return DemoRepeatShoot;
                case rimrushHelpDemo.Pump:
                    return DemoRepeatPump;
                case rimrushHelpDemo.Dash:
                    return DemoRepeatDash;
                case rimrushHelpDemo.Steal:
                    return DemoRepeatSteal;
                default:
                    return DemoRepeatBlock;
            }
        }

        /// <summary>
        /// Update the title, description, and coach tip text for the selected demo.
        /// </summary>
        private void UpdateDemoCopy()
        {
            SetText(demoTitleText, DemoTitle(currentDemo));
            SetText(demoDescriptionText, DemoDescription(currentDemo));
            SetText(demoCoachText, DemoCoachNote(currentDemo));
        }

        /// <summary>
        /// Return the display title for each demo type (e.g. "MOVE", "JUMP").
        /// </summary>
        private static string DemoTitle(rimrushHelpDemo demo)
        {
            switch (demo)
            {
                case rimrushHelpDemo.Move:
                    return "MOVE";
                case rimrushHelpDemo.Jump:
                    return "JUMP";
                case rimrushHelpDemo.Shoot:
                    return "ACTION: SHOOT";
                case rimrushHelpDemo.Pump:
                    return "DOWN: PUMP FAKE";
                case rimrushHelpDemo.Dash:
                    return "DOUBLE-TAP DASH";
                case rimrushHelpDemo.Steal:
                    return "ACTION: STEAL";
                default:
                    return "DOWN: BLOCK";
            }
        }

        /// <summary>
        /// Return the key instructions for each demo type.
        /// </summary>
        private static string DemoDescription(rimrushHelpDemo demo)
        {
            switch (demo)
            {
                case rimrushHelpDemo.Move:
                    return "Hold A / D.\nRelease to stop.";
                case rimrushHelpDemo.Jump:
                    return "Press W.\nJump for air shots.";
                case rimrushHelpDemo.Shoot:
                    return "Press B with the ball.\nRelease before landing.";
                case rimrushHelpDemo.Pump:
                    return "Hold S with the ball.\nUse it to fake the shot.";
                case rimrushHelpDemo.Dash:
                    return "Double-tap A or D.\nDash has a short cooldown.";
                case rimrushHelpDemo.Steal:
                    return "Press B near the dribbler.\nSteal from close range.";
                default:
                    return "Hold S to block.\nJump into the shot path.";
            }
        }

        /// <summary>
        /// Return a short coaching tip for each demo type.
        /// </summary>
        private static string DemoCoachNote(rimrushHelpDemo demo)
        {
            switch (demo)
            {
                case rimrushHelpDemo.Move:
                    return "Tip: stop before shooting.";
                case rimrushHelpDemo.Jump:
                    return "Tip: jump late.";
                case rimrushHelpDemo.Shoot:
                    return "Tip: release at the top.";
                case rimrushHelpDemo.Pump:
                    return "Tip: ball only.";
                case rimrushHelpDemo.Dash:
                    return "Tip: dash beats steals.";
                case rimrushHelpDemo.Steal:
                    return "Tip: stay in front.";
                default:
                    return "Tip: time the jump.";
            }
        }

        /// <summary>
        /// Highlight the button and row for the currently selected demo.
        /// </summary>
        private void UpdateDemoSelections()
        {
            if (demoRowPlates != null)
            {
                for (var i = 0; i < demoRowPlates.Length; i++)
                {
                    var plate = demoRowPlates[i];
                    if (plate == null)
                    {
                        continue;
                    }

                    var selected = i == (int)currentDemo;
                    plate.color = selected
                        ? new Color32(0x2D, 0xE6, 0xA3, 0xEE)
                        : new Color32(0x14, 0x1D, 0x31, 0xD8);
                }
            }

            if (buttons == null)
            {
                return;
            }

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button != null)
                {
                    button.SetSelected(
                        button.Action == rimrushHelpButtonAction.QuickTestToggle && rimrushQuickTestSettings.Enabled ||
                        button.Action == rimrushHelpButtonAction.QuickTestInfoToggle && quickTestInfoVisible ||
                        IsDemoButtonSelected(button.Action));
                }
            }
        }

        /// <summary>
        /// Check if a button matches the currently selected demo.
        /// </summary>
        private bool IsDemoButtonSelected(rimrushHelpButtonAction action)
        {
            return action == rimrushHelpButtonAction.DemoMove && currentDemo == rimrushHelpDemo.Move ||
                   action == rimrushHelpButtonAction.DemoJump && currentDemo == rimrushHelpDemo.Jump ||
                   action == rimrushHelpButtonAction.DemoShoot && currentDemo == rimrushHelpDemo.Shoot ||
                   action == rimrushHelpButtonAction.DemoPump && currentDemo == rimrushHelpDemo.Pump ||
                   action == rimrushHelpButtonAction.DemoDash && currentDemo == rimrushHelpDemo.Dash ||
                   action == rimrushHelpButtonAction.DemoSteal && currentDemo == rimrushHelpDemo.Steal ||
                   action == rimrushHelpButtonAction.DemoBlock && currentDemo == rimrushHelpDemo.Block;
        }

        /// <summary>
        /// Bump the witch's sorting order so she renders in front of the panel background.
        /// </summary>
        private void ApplyWitchSortingOrder()
        {
            ApplyWitchSortingOrder(witchArmature);
        }

        /// <summary>
        /// Bump every sprite on the given armature so it draws on top of other UI.
        /// </summary>
        private static void ApplyWitchSortingOrder(DBLiteArmature armature)
        {
            if (armature == null)
            {
                return;
            }

            var renderers = armature.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.sortingOrder < WitchSortingOrderBase)
                {
                    renderer.sortingOrder += WitchSortingOrderBase;
                }
            }
        }

        /// <summary>
        /// Hide the ball sprite from the witch preview so only the character is visible.
        /// </summary>
        private static void HidePreviewBall(DBLiteArmature armature)
        {
            if (armature == null)
            {
                return;
            }

            armature.SetSlotHidden("ball", true);
            armature.SetSlotHidden("ball_front", true);
        }

        /// <summary>
        /// Safely set text on a TextMeshPro label (handles null gracefully).
        /// </summary>
        private static void SetText(TMP_Text textMesh, string value)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.text = value ?? string.Empty;
            textMesh.ForceMeshUpdate();
        }

        /// <summary>
        /// Find the active help panel, or spawn one from the prefab if createFallback is true.
        /// </summary>
        private static rimrushHelpPanel FindActivePanel(bool createFallback)
        {
            if (activePanel != null)
            {
                return activePanel;
            }

            activePanel = FindScenePanel();
            if (activePanel != null || !createFallback)
            {
                return activePanel;
            }

            var prefab = Resources.Load<rimrushHelpPanel>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Missing help panel prefab at Resources/{PrefabResourcePath}.");
                return null;
            }

            activePanel = Object.Instantiate(prefab);
            activePanel.name = "RimrushHelpPanel_RuntimeFallback";
            return activePanel;
        }

        /// <summary>
        /// Look for an existing help panel already placed in the current scene.
        /// </summary>
        private static rimrushHelpPanel FindScenePanel()
        {
            var panels = Resources.FindObjectsOfTypeAll<rimrushHelpPanel>();
            for (var i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    return panel;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Wire up all serialized references from the prefab builder (editor only).
        /// </summary>
        public void EditorConfigure(
            GameObject panelRootObject,
            GameObject keyboardPage,
            GameObject rulesPage,
            rimrushHelpButton[] configuredButtons,
            SpriteRenderer keyboardTab,
            SpriteRenderer rulesTab,
            TMP_Text keyboardLabel,
            TMP_Text rulesLabel,
            SpriteRenderer[] demoRows,
            TMP_Text demoTitle,
            TMP_Text demoDescription,
            TMP_Text demoCoach,
            Transform configuredWitchMount,
            SpriteRenderer configuredWitchSpotlight,
            rimrushHelpButton configuredQuickTestToggleButton,
            SpriteRenderer configuredQuickTestTogglePlate,
            TMP_Text configuredQuickTestToggleText,
            rimrushHelpButton configuredQuickTestInfoButton,
            GameObject configuredQuickTestInfoRoot,
            TMP_Text configuredQuickTestInfoText)
        {
            panelRoot = panelRootObject;
            keyboardPageRoot = keyboardPage;
            rulesPageRoot = rulesPage;
            buttons = configuredButtons;
            keyboardTabPlate = keyboardTab;
            rulesTabPlate = rulesTab;
            keyboardTabText = keyboardLabel;
            rulesTabText = rulesLabel;
            demoRowPlates = demoRows;
            demoTitleText = demoTitle;
            demoDescriptionText = demoDescription;
            demoCoachText = demoCoach;
            witchMount = configuredWitchMount;
            witchSpotlight = configuredWitchSpotlight;
            quickTestToggleButton = configuredQuickTestToggleButton;
            quickTestTogglePlate = configuredQuickTestTogglePlate;
            quickTestToggleText = configuredQuickTestToggleText;
            quickTestInfoButton = configuredQuickTestInfoButton;
            quickTestInfoRoot = configuredQuickTestInfoRoot;
            quickTestInfoText = configuredQuickTestInfoText;
        }

        /// <summary>
        /// Show a live preview of the panel in the Unity editor (not at runtime).
        /// </summary>
        private void ApplyEditorPreviewState()
        {
            if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
            {
                return;
            }

            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            visible = false;
            currentPage = rimrushHelpPage.Keyboard;
            currentDemo = rimrushHelpDemo.Block;
            transform.localScale = Vector3.one;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (keyboardPageRoot != null)
            {
                keyboardPageRoot.SetActive(true);
            }

            if (rulesPageRoot != null)
            {
                rulesPageRoot.SetActive(false);
            }

            HideLegacyTabs();
            RefreshQuickTestToggle();
            SetQuickTestInfoVisible(false);
            UpdateDemoCopy();
            UpdateDemoSelections();
            EnsureEditorWitchPreview();
        }

        /// <summary>
        /// Create or reuse a witch preview for the editor scene view.
        /// </summary>
        private void EnsureEditorWitchPreview()
        {
            if (Application.isPlaying || witchMount == null || !gameObject.scene.IsValid())
            {
                return;
            }

            if (editorWitchPreviewRoot == null)
            {
                var existing = witchMount.Find(EditorWitchPreviewName);
                if (existing != null)
                {
                    editorWitchPreviewRoot = existing.gameObject;
                }
            }

            var armature = editorWitchPreviewRoot != null ? editorWitchPreviewRoot.GetComponent<DBLiteArmature>() : null;
            if (editorWitchPreviewRoot != null && armature != null)
            {
                editorWitchPreviewRoot.SetActive(true);
                HidePreviewBall(armature);
                ApplyWitchSortingOrder(armature);
                return;
            }

            DestroyEditorWitchPreview();
            armature = rimrushPlayersData.BuildGameplayArmature(EditorWitchPreviewName);
            if (armature == null)
            {
                return;
            }

            editorWitchPreviewRoot = armature.gameObject;
            SetEditorPreviewHideFlags(editorWitchPreviewRoot);
            editorWitchPreviewRoot.transform.SetParent(witchMount, false);
            editorWitchPreviewRoot.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                witchMount,
                new Vector3(0f, -35f, 0f));
            editorWitchPreviewRoot.transform.localScale = new Vector3(
                rimrushConstants.PixelPerfectCharacterScale * 1.22f,
                rimrushConstants.PixelPerfectCharacterScale * 1.22f,
                1f);
            rimrushPlayersData.ApplyCharacter(armature, WitchCharacterId);
            armature.StopAtStart("blockStart");
            HidePreviewBall(armature);
            ApplyWitchSortingOrder(armature);
        }

        /// <summary>
        /// Remove the editor-only witch preview object.
        /// </summary>
        private void DestroyEditorWitchPreview()
        {
            if (editorWitchPreviewRoot == null && witchMount != null)
            {
                var existing = witchMount.Find(EditorWitchPreviewName);
                if (existing != null)
                {
                    editorWitchPreviewRoot = existing.gameObject;
                }
            }

            if (editorWitchPreviewRoot == null)
            {
                return;
            }

            editorWitchPreviewRoot.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(editorWitchPreviewRoot);
            }
            else
            {
                DestroyImmediate(editorWitchPreviewRoot);
            }

            editorWitchPreviewRoot = null;
        }

        /// <summary>
        /// Mark editor preview objects so they are not saved into the scene or build.
        /// </summary>
        private static void SetEditorPreviewHideFlags(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            const HideFlags flags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                transforms[i].gameObject.hideFlags = flags;
            }
        }
#endif
    }
}
