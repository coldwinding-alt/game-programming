// Help panel (key descriptions and game rules)

// After the player presses the help button, a pop-up will appear with two pages: keyboard operation and game rules.

// You can click on different actions on the keyboard page, and the witch character will play the corresponding demonstration animation.

using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>Help panel page type: keyboard operation page or game rules page. </summary>
    public enum mlpHelpPage
    {
        Keyboard,
        Rules
    }

    /// <summary>The help panel demonstrates action types: moving, jumping, shooting, fake action, dunking, stealing, blocking and other demonstrable operations. </summary>
    public enum mlpHelpDemo
    {
        Move,
        Jump,
        Shoot,
        Pump,
        Dash,
        Steal,
        Block
    }

    /// <summary>Help panel button actions: Switch pages, select demos, close panels, etc. The operations that buttons can perform. </summary>
    public enum mlpHelpButtonAction
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
    /// <summary>
    /// Help panel: The interface that pops up after pressing the help button has two pages: keyboard operation and game rules. You can click on different actions to view the demonstration animation of the witch character.

    /// </summary>
    public sealed class mlpHelpPanel : MonoBehaviour
    {
        private const string PrefabResourcePath = "mlp/Prefabs/UI/MlpHelpPanel"; // Help panel prefab resource path

        private const int WitchCharacterId = 6; // Witch character ID

        private const int WitchSortingOrderBase = 920; // Witch rendering sorting level base
        private const float DemoRepeatMove = 999f; // Mobile demo repeat interval (seconds)

        private const float DemoRepeatJump = 0.72f; // Jump presentation repeat interval (seconds)

        private const float DemoRepeatShoot = 0.9f; // Shooting demonstration repetition interval (seconds)

        private const float DemoRepeatPump = 0.5f; // Fake action demonstration repeat interval (seconds)

        private const float DemoRepeatDash = 0.55f; // Sprint demo repeat interval (seconds)

        private const float DemoRepeatSteal = 0.82f; // Tackle demo repeat interval (seconds)
        private const float DemoRepeatBlock = 0.55f; // Block demonstration repeat interval (seconds)

#if UNITY_EDITOR
        private const string EditorWitchPreviewName = "WitchEditorPreview"; // Editor witch preview object name

#endif

        private static mlpHelpPanel activePanel; // The currently active help panel instance


        [SerializeField] private GameObject panelRoot; // Panel root node

        [SerializeField] private GameObject keyboardPageRoot; // Keyboard operation page root node

        [SerializeField] private GameObject rulesPageRoot; // Game rules page root node

        [SerializeField] private mlpHelpButton[] buttons; // Array of all buttons on the panel

        [SerializeField] private SpriteRenderer keyboardTabPlate; // Keyboard tab base wizard

        [SerializeField] private SpriteRenderer rulesTabPlate; // Rules tab floor wizard

        [SerializeField] private TMP_Text keyboardTabText; // Keyboard tab text

        [SerializeField] private TMP_Text rulesTabText; // Rules tab text

        [SerializeField] private SpriteRenderer[] demoRowPlates; // Demo row background floor sprite array
        [SerializeField] private TMP_Text demoTitleText; // Demo title text

        [SerializeField] private TMP_Text demoDescriptionText; // Demo description text

        [SerializeField] private TMP_Text demoCoachText; // Presentation Coach Prompt Text

        [SerializeField] private Transform witchMount; // Witch model mount point

        [SerializeField] private SpriteRenderer witchSpotlight; // witch spotlight elf

        [SerializeField] private mlpHelpButton quickTestToggleButton; // Quick test switch button

        [SerializeField] private SpriteRenderer quickTestTogglePlate; // Quick test switch backplane wizard

        [SerializeField] private TMP_Text quickTestToggleText; // Quick test switch text

        [SerializeField] private mlpHelpButton quickTestInfoButton; // Quick test information button

        [SerializeField] private GameObject quickTestInfoRoot; // Quickly test the information panel root node

        [SerializeField] private TMP_Text quickTestInfoText; // Quick test message text


        private DBLiteArmature witchArmature; // Witch skeleton animation example

        private mlpHelpPage currentPage = mlpHelpPage.Keyboard; // The type of page currently displayed
        private mlpHelpDemo currentDemo = mlpHelpDemo.Block; // Currently selected demonstration action

        private bool initialized; // Has initialization been completed?

        private bool visible; // Is the panel visible?

        private float panelTime; // Cumulative time of panel opening (used for entrance animation)

        private float demoTimer; // Presentation animation timer

        private bool demoToggle; // Demonstration animation alternation mark (for two-stage animation switching)
        private bool quickTestInfoVisible; // Quickly test whether the information panel is visible

#if UNITY_EDITOR
        private GameObject editorWitchPreviewRoot; // Editor witch preview object root node

#endif

        public bool IsVisible => visible; // Whether the panel is visible (public read-only)

        public static bool IsAnyOpen // Are there any help panels open?
        {
            get
            {
                var panel = activePanel != null ? activePanel : FindScenePanel();
                return panel != null && panel.visible;
            }
        }

        /// <summary>
        /// Open the keyboard operation page of the help panel. If there is no panel, create one.
        /// </summary>
        public static void ShowKeyboardPage()
        {
            // 1. Find an existing help panel instance, if not create a new one from the prefab
            var panel = FindActivePanel(createFallback: true);
            // 2. If the panel is found, display the keyboard operation page

            if (panel != null)
            {
                panel.Show(mlpHelpPage.Keyboard);
            }
        }

        /// <summary>
        /// Close the currently open help panel.

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
        /// This panel is recorded as the active panel when the game starts.

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
        /// Updates the editor preview when the component is enabled.

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
        /// Refresh the editor preview when the value in the Inspector changes.

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
        /// The active panel reference is cleared when the object is destroyed.

        /// </summary>
        private void OnDestroy()
        {
            if (activePanel == this)
            {
                activePanel = null;
            }
        }

        /// <summary>
        /// Performed per frame: panel animation, button detection, presentation updates, and Escape key close handling.

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
            // 1. Skip all updates when the panel is not open

            if (!visible)
            {
                return;
            }

            // 2. The accumulated panel opening time is used to play the entrance animation.

            panelTime += Time.unscaledDeltaTime;
            // 3. Update the panel’s zoom entrance animation and witch spotlight pulse effect

            UpdatePanelEntrance();
            // 4. Detect mouseovers and clicks on all buttons

            UpdateButtons();
            // 5. Update the witch demonstration animation (replay when the timer expires)
            UpdateDemo(Time.unscaledDeltaTime);

            // 6. Press the Escape key to close the help panel

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
        /// Display the help panel page.

        /// </summary>
        public void Show(mlpHelpPage page)
        {
            // 1. If the panel root node is not specified, use the current GameObject

            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            // 2. Mark the panel as visible and activate the panel root node

            visible = true;
            panelRoot.SetActive(true);
            // 3. Reset the panel timer (used for the entrance animation) and set a slightly smaller initial zoom
            panelTime = 0f;
            transform.localScale = Vector3.one * 0.985f;

            // 4. Build the witch model and UI when opening for the first time (will be skipped in subsequent calls)

            EnsureInitialized();
            // 5. Set as keyboard operation page and hide the information panel

            SetPage(mlpHelpPage.Keyboard);
            SetQuickTestInfoVisible(false);
            // 6. The block demo is selected by default and the animation is replayed
            SelectDemo(mlpHelpDemo.Block, forceRestart: true);
        }

        /// <summary>
        /// Hide the help panel. Play button sound by default.
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
                mlpAudio.Instance?.Play(mlpAssets.Sounds.Button, 0.55f);
            }
        }

        /// <summary>
        /// Immediately hides the panel without playing any sound effects (for use on startup).

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
        /// Set up the witch model and page when the panel is first shown.
        /// </summary>
        private void EnsureInitialized()
        {
            // 1. If it has been initialized, return directly

            if (initialized)
            {
                return;
            }

            // 2. Mark as initialized

            initialized = true;
            // 3. Hide the old tab page (now only use the keyboard to operate the page)

            HideLegacyTabs();
            // 4. Hide development and testing controls (ordinary players don’t need to see them)

            HideQuickTestControls();
            // 5. Create a witch character model for playing action demonstration animations

            BuildWitchPreview();
            // 6. Set the current page and selected demonstration action

            SetPage(currentPage);
            SelectDemo(currentDemo, forceRestart: true);
        }

        /// <summary>
        /// Create a witch character model to play the demo animation.

        /// </summary>
        private void BuildWitchPreview()
        {
            // 1. If the mount point does not exist or the witch model has been created, return directly

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
            // 2. Create a skeletal animation model of the witch character

            witchArmature = mlpPlayersData.BuildGameplayArmature("HelpWitchPreview");
            if (witchArmature == null)
            {
                return;
            }

            // 3. Mount the model to the specified position on the panel and set the appropriate scaling ratio

            witchArmature.transform.SetParent(witchMount, false);
            witchArmature.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                witchMount,
                new Vector3(0f, -35f, 0f));
            witchArmature.transform.localScale = new Vector3(
                mlpConstants.PixelPerfectCharacterScale * 1.22f,
                mlpConstants.PixelPerfectCharacterScale * 1.22f,
                1f);
            // 4. Apply the appearance of the witch character (color, clothing, etc.) to hide the basketball in your hand

            mlpPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
            HidePreviewBall(witchArmature);
            // 5. Increase the rendering level of the witch to ensure that she appears above the panel background

            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// Panel opening animation with quick zoom bounce and subtle spotlight pulsing effect.

        /// </summary>
        private void UpdatePanelEntrance()
        {
            // 1. Calculate the progress of the entry animation (0→1), using the ease-out curve (cubic)

            var t = Mathf.Clamp01(panelTime / 0.12f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            // 2. The panel pops up from 0.985x zoom to 1.0x

            transform.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, eased);

            // 3. The witch spotlight continues to do weak breathing pulse animations

            if (witchSpotlight != null)
            {
                var pulse = Mathf.Sin(Time.unscaledTime * 2.3f) * 0.025f;
                witchSpotlight.transform.localScale = Vector3.one * (1f + pulse);
            }
        }

        /// <summary>
        /// Detect mouse clicks on all buttons every frame and route hits to HandleButton.

        /// </summary>
        private void UpdateButtons()
        {
            // 1. Get the main camera (for mouse coordinate conversion)

            var camera = Camera.main;
            if (buttons == null)
            {
                return;
            }

            // 2. Traverse all buttons and detect mouse hovers and clicks

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                // 3. Tick returns true indicating that the player has completed a valid click

                if (button == null || !button.Tick(camera))
                {
                    continue;
                }

                // 4. Play the button sound effect and route the click event to the corresponding operation

                mlpAudio.Instance?.Play(mlpAssets.Sounds.Button, 0.75f);
                HandleButton(button.Action);
            }
        }

        /// <summary>
        /// Route button clicks to the correct action (close, switch tabs, select demo, etc.).

        /// </summary>
        private void HandleButton(mlpHelpButtonAction action)
        {
            // 1. Perform corresponding operations according to the button action type

            switch (action)
            {
                // 2. Close the help panel (no additional sound effects are played because the button sound effects are already played)
                case mlpHelpButtonAction.Close:
                    Hide(playSound: false);
                    break;
                case mlpHelpButtonAction.KeyboardTab:
                    SetPage(mlpHelpPage.Keyboard);
                    break;
                case mlpHelpButtonAction.RulesTab:
                    SetPage(mlpHelpPage.Keyboard);
                    break;
                case mlpHelpButtonAction.ReplayTutorial:
                    HandleReplayTutorialRequest();
                    break;
                case mlpHelpButtonAction.DemoMove:
                    SelectDemo(mlpHelpDemo.Move);
                    break;
                case mlpHelpButtonAction.DemoJump:
                    SelectDemo(mlpHelpDemo.Jump);
                    break;
                case mlpHelpButtonAction.DemoShoot:
                    SelectDemo(mlpHelpDemo.Shoot);
                    break;
                case mlpHelpButtonAction.DemoPump:
                    SelectDemo(mlpHelpDemo.Pump);
                    break;
                case mlpHelpButtonAction.DemoDash:
                    SelectDemo(mlpHelpDemo.Dash);
                    break;
                case mlpHelpButtonAction.DemoSteal:
                    SelectDemo(mlpHelpDemo.Steal);
                    break;
                case mlpHelpButtonAction.DemoBlock:
                    SelectDemo(mlpHelpDemo.Block);
                    break;
                case mlpHelpButtonAction.QuickTestToggle:
                    mlpQuickTestSettings.Enabled = !mlpQuickTestSettings.Enabled;
                    RefreshQuickTestToggle();
                    UpdateDemoSelections();
                    break;
                case mlpHelpButtonAction.QuickTestInfoToggle:
                    SetQuickTestInfoVisible(!quickTestInfoVisible);
                    UpdateDemoSelections();
                    break;
            }
        }

        private void HandleReplayTutorialRequest()
        {
            if (!mlpGameBootstrap.TryStartTutorialFromHelp())
            {
                Debug.LogWarning("Could not find mlpGameBootstrap to launch the tutorial from the help panel.");
                return;
            }

            Hide(playSound: false);
        }

        /// <summary>
        /// Keep help panels to a single page of instructions.

        /// </summary>
        private void SetPage(mlpHelpPage page)
        {
            // 1. Fixed use of keyboard operation page (the rules page has been abandoned)

            currentPage = mlpHelpPage.Keyboard;
            // 2. Display the keyboard operation page and hide the game rules page

            if (keyboardPageRoot != null)
            {
                keyboardPageRoot.SetActive(true);
            }

            if (rulesPageRoot != null)
            {
                rulesPageRoot.SetActive(false);
            }

            // 3. Hide the old tab button and refresh the test switch status

            HideLegacyTabs();
            RefreshQuickTestToggle();

            // 4. Update the selected and highlighted status of all buttons (demo button, test switch, etc.)

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
                        button.Action == mlpHelpButtonAction.QuickTestToggle && mlpQuickTestSettings.Enabled ||
                        button.Action == mlpHelpButtonAction.QuickTestInfoToggle && quickTestInfoVisible ||
                        IsDemoButtonSelected(button.Action));
                }
            }
        }

        /// <summary>
        /// Highlight the selected tab by changing the tab's background and text color.

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

        private void HideQuickTestControls()
        {
            if (quickTestToggleButton != null)
            {
                quickTestToggleButton.gameObject.SetActive(false);
            }

            if (quickTestInfoButton != null)
            {
                quickTestInfoButton.gameObject.SetActive(false);
            }

            if (quickTestInfoRoot != null)
            {
                quickTestInfoRoot.SetActive(false);
            }

            quickTestInfoVisible = false;
        }

        private void EnsureQuickTestToggle()
        {
            // 1. Skip when not running or when the panel does not exist

            if (panelRoot == null || !Application.isPlaying)
            {
                return;
            }

            // 2. Load tab and card sprite resources (used to create button backgrounds)

            var tab = Resources.Load<Sprite>("mlp/Help/help_tab");
            var card = Resources.Load<Sprite>("mlp/Help/help_card");
            if (quickTestToggleButton == null)
            {
                var root = new GameObject("QuickTestToggle");
                root.transform.SetParent(panelRoot.transform, false);

                const float x = 604f;
                const float y = 58f;
                const float width = 160f;
                const float height = 34f;
                quickTestTogglePlate = AddRuntimeSprite("QuickTestTogglePlate", tab, x, y, 0.84f, width, height, 915, root.transform);
                quickTestToggleText = mlpRender.TmpText(
                    "QuickTestToggleLabel",
                    string.Empty,
                    x,
                    y + 1f,
                    10,
                    new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                    TextAnchor.MiddleCenter,
                    935,
                    root.transform,
                    mlpTextStyle.TournamentAccent);

                quickTestToggleButton = root.AddComponent<mlpHelpButton>();
                quickTestToggleButton.Configure(
                    mlpHelpButtonAction.QuickTestToggle,
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
                var infoLabel = mlpRender.TmpText(
                    "QuickTestInfoButtonLabel",
                    "?",
                    infoX,
                    infoY + 1f,
                    13,
                    new Color32(0xE9, 0xF3, 0xFF, 0xFF),
                    TextAnchor.MiddleCenter,
                    936,
                    infoRoot.transform,
                    mlpTextStyle.TournamentAccent);

                quickTestInfoButton = infoRoot.AddComponent<mlpHelpButton>();
                quickTestInfoButton.Configure(
                    mlpHelpButtonAction.QuickTestInfoToggle,
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
                quickTestInfoText = mlpRender.TmpText(
                    "QuickTestInfoText",
                    QuickTestInfoCopy(),
                    466f,
                    113f,
                    9,
                    new Color32(0xF4, 0xF7, 0xFF, 0xFF),
                    TextAnchor.MiddleLeft,
                    936,
                    quickTestInfoRoot.transform,
                    mlpTextStyle.TournamentBody);
            }

            SetQuickTestInfoVisible(false);
        }

        private void RefreshQuickTestToggle()
        {
            SetText(quickTestToggleText, mlpQuickTestSettings.Enabled ? "ON" : "OFF");
            quickTestToggleButton?.SetSelected(mlpQuickTestSettings.Enabled);
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

        private void AppendButton(mlpHelpButton button)
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
            go.transform.position = mlpConstants.PixelToWorldSnapped(x, y, z);
            go.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * width / spriteWidth,
                mlpConstants.UnitsPerPixel * height / spriteHeight,
                1f);
            return renderer;
        }

        /// <summary>
        /// Select the action you want to demonstrate (move, jump, shoot, feint, sprint, steal, block).

        /// </summary>
        private void SelectDemo(mlpHelpDemo demo, bool forceRestart = false)
        {
            // 1. If you click on the selected demo, just replay the animation.

            if (!forceRestart && currentDemo == demo)
            {
                RestartDemoAnimation();
                return;
            }

            // 2. Set the currently selected presentation type

            currentDemo = demo;
            // 3. Reset demo timer and alternate play markers (two-part animation for blocks and feints)

            demoTimer = 999f;
            demoToggle = false;
            // 4. Update title, description and coaching prompt text

            UpdateDemoCopy();
            // 5. Highlight the currently selected demo button row

            UpdateDemoSelections();
            // 6. Play the corresponding action animation of the witch

            RestartDemoAnimation();
        }

        /// <summary>
        /// Restarts the presentation animation when the timer expires.

        /// </summary>
        private void UpdateDemo(float dt)
        {
            // 1. If there is no witch model or not on the keyboard page, skip

            if (witchArmature == null || currentPage != mlpHelpPage.Keyboard)
            {
                return;
            }

            // 2. Cumulative presentation timer

            demoTimer += dt;
            // 3. Get the repetition interval (seconds) of the current demonstration animation, and replay it when it expires.

            var repeat = DemoRepeatFor(currentDemo);
            if (demoTimer >= repeat)
            {
                RestartDemoAnimation();
            }
        }

        /// <summary>
        /// Play the witch animation corresponding to the currently selected demo.

        /// </summary>
        private void RestartDemoAnimation()
        {
            // 1. Skip if there is no witch model

            if (witchArmature == null)
            {
                return;
            }

            // 2. Reset the demo timer and start counting

            demoTimer = 0f;
            // 3. Play the corresponding skeletal animation according to the selected demonstration type

            switch (currentDemo)
            {
                case mlpHelpDemo.Move:
                    witchArmature.Play("run");
                    break;
                case mlpHelpDemo.Jump:
                    witchArmature.Play("jump");
                    break;
                case mlpHelpDemo.Shoot:
                    witchArmature.Play("throw_land");
                    break;
                // 4. Use two alternating animations for feints and blocks (forward + closing)
                case mlpHelpDemo.Pump:
                    witchArmature.Play(demoToggle ? "pumpEnd" : "pumpStart");
                    demoToggle = !demoToggle;
                    break;
                case mlpHelpDemo.Dash:
                    witchArmature.Play("dash");
                    break;
                case mlpHelpDemo.Steal:
                    witchArmature.Play("steal");
                    break;
                case mlpHelpDemo.Block:
                    witchArmature.Play(demoToggle ? "blockEnd" : "blockStart");
                    demoToggle = !demoToggle;
                    break;
            }

            // 5. Hide the basketball in the witch’s hand and increase the rendering level to ensure it is displayed on the panel.

            HidePreviewBall(witchArmature);
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// Returns the duration in seconds before each presentation animation repeats.

        /// </summary>
        private static float DemoRepeatFor(mlpHelpDemo demo)
        {
            switch (demo)
            {
                case mlpHelpDemo.Move:
                    return DemoRepeatMove;
                case mlpHelpDemo.Jump:
                    return DemoRepeatJump;
                case mlpHelpDemo.Shoot:
                    return DemoRepeatShoot;
                case mlpHelpDemo.Pump:
                    return DemoRepeatPump;
                case mlpHelpDemo.Dash:
                    return DemoRepeatDash;
                case mlpHelpDemo.Steal:
                    return DemoRepeatSteal;
                default:
                    return DemoRepeatBlock;
            }
        }

        /// <summary>
        /// Updates the selected demo's title, description, and coach tip text.

        /// </summary>
        private void UpdateDemoCopy()
        {
            SetText(demoTitleText, DemoTitle(currentDemo));
            SetText(demoDescriptionText, DemoDescription(currentDemo));
            SetText(demoCoachText, DemoCoachNote(currentDemo));
        }

        /// <summary>
        /// Returns the display title for each demo type (e.g. "MOVE", "JUMP").

        /// </summary>
        private static string DemoTitle(mlpHelpDemo demo)
        {
            switch (demo)
            {
                case mlpHelpDemo.Move:
                    return "MOVE";
                case mlpHelpDemo.Jump:
                    return "JUMP";
                case mlpHelpDemo.Shoot:
                    return "ACTION: SHOOT";
                case mlpHelpDemo.Pump:
                    return "DOWN: PUMP FAKE";
                case mlpHelpDemo.Dash:
                    return "DOUBLE-TAP DASH";
                case mlpHelpDemo.Steal:
                    return "ACTION: STEAL";
                default:
                    return "DOWN: BLOCK";
            }
        }

        /// <summary>
        /// Returns keystroke instructions for each presentation type.

        /// </summary>
        private static string DemoDescription(mlpHelpDemo demo)
        {
            switch (demo)
            {
                case mlpHelpDemo.Move:
                    return "Hold A / D.\nRelease to stop.";
                case mlpHelpDemo.Jump:
                    return "Press W.\nJump for air shots.";
                case mlpHelpDemo.Shoot:
                    return "Press B with the ball.\nRelease before landing.";
                case mlpHelpDemo.Pump:
                    return "Hold S with the ball.\nUse it to fake the shot.";
                case mlpHelpDemo.Dash:
                    return "Double-tap A or D.\nDash has a short cooldown.";
                case mlpHelpDemo.Steal:
                    return "Press B near the dribbler.\nSteal from close range.";
                default:
                    return "Hold S to block.\nJump into the shot path.";
            }
        }

        /// <summary>
        /// Returns brief coaching tips for each presentation type.

        /// </summary>
        private static string DemoCoachNote(mlpHelpDemo demo)
        {
            switch (demo)
            {
                case mlpHelpDemo.Move:
                    return "Tip: stop before shooting.";
                case mlpHelpDemo.Jump:
                    return "Tip: jump late.";
                case mlpHelpDemo.Shoot:
                    return "Tip: release at the top.";
                case mlpHelpDemo.Pump:
                    return "Tip: ball only.";
                case mlpHelpDemo.Dash:
                    return "Tip: dash beats steals.";
                case mlpHelpDemo.Steal:
                    return "Tip: stay in front.";
                default:
                    return "Tip: time the jump.";
            }
        }

        /// <summary>
        /// Highlight the buttons and rows of the currently selected demo.

        /// </summary>
        private void UpdateDemoSelections()
        {
            // 1. Highlight the background of the currently selected demo row (green = selected, dark = unselected)

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

            // 2. Update the selected status of all buttons (demo button and test switch button)

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
                        button.Action == mlpHelpButtonAction.QuickTestToggle && mlpQuickTestSettings.Enabled ||
                        button.Action == mlpHelpButtonAction.QuickTestInfoToggle && quickTestInfoVisible ||
                        IsDemoButtonSelected(button.Action));
                }
            }
        }

        /// <summary>
        /// Checks if the button matches the currently selected demo.

        /// </summary>
        private bool IsDemoButtonSelected(mlpHelpButtonAction action)
        {
            return action == mlpHelpButtonAction.DemoMove && currentDemo == mlpHelpDemo.Move ||
                   action == mlpHelpButtonAction.DemoJump && currentDemo == mlpHelpDemo.Jump ||
                   action == mlpHelpButtonAction.DemoShoot && currentDemo == mlpHelpDemo.Shoot ||
                   action == mlpHelpButtonAction.DemoPump && currentDemo == mlpHelpDemo.Pump ||
                   action == mlpHelpButtonAction.DemoDash && currentDemo == mlpHelpDemo.Dash ||
                   action == mlpHelpButtonAction.DemoSteal && currentDemo == mlpHelpDemo.Steal ||
                   action == mlpHelpButtonAction.DemoBlock && currentDemo == mlpHelpDemo.Block;
        }

        /// <summary>
        /// Raise the witch's sorting layer so that it renders in front of the panel background.

        /// </summary>
        private void ApplyWitchSortingOrder()
        {
            ApplyWitchSortingOrder(witchArmature);
        }

        /// <summary>
        /// Raise the ordering layer of all sprites on the given skeletal animation so that they are drawn on top of other UI.

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
        /// Hides the ball sprite in the Witch preview, showing only the character itself.

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
        /// Safely set the text of a TextMeshPro label (handles null gracefully).

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
        /// Finds the active help panel and generates one from a prefab if createFallback is true.

        /// </summary>
        private static mlpHelpPanel FindActivePanel(bool createFallback)
        {
            // 1. If there is already a cached active panel, return directly

            if (activePanel != null)
            {
                return activePanel;
            }

            // 2. Try to find an existing help panel in the current scene

            activePanel = FindScenePanel();
            if (activePanel != null || !createFallback)
            {
                return activePanel;
            }

            // 3. There are no panels in the scene and fallback instances are allowed to be created: Load from prefab

            var prefab = Resources.Load<mlpHelpPanel>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Missing help panel prefab at Resources/{PrefabResourcePath}.");
                return null;
            }

            // 4. Instantiate the prefab as a runtime fallback panel

            activePanel = Object.Instantiate(prefab);
            activePanel.name = "MlpHelpPanel_RuntimeFallback";
            return activePanel;
        }

        /// <summary>
        /// Find help panels that already exist in the current scene.

        /// </summary>
        private static mlpHelpPanel FindScenePanel()
        {
            var panels = Resources.FindObjectsOfTypeAll<mlpHelpPanel>();
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
        /// Wire all serialization references from the prefab builder (Editor only).

        /// </summary>
        public void EditorConfigure(
            GameObject panelRootObject,
            GameObject keyboardPage,
            GameObject rulesPage,
            mlpHelpButton[] configuredButtons,
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
            mlpHelpButton configuredQuickTestToggleButton,
            SpriteRenderer configuredQuickTestTogglePlate,
            TMP_Text configuredQuickTestToggleText,
            mlpHelpButton configuredQuickTestInfoButton,
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
        /// Shows a live preview of the panel in the Unity Editor (not runtime).

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
            currentPage = mlpHelpPage.Keyboard;
            currentDemo = mlpHelpDemo.Block;
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
        /// Create or reuse witch previews for editor scene views.

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
            armature = mlpPlayersData.BuildGameplayArmature(EditorWitchPreviewName);
            if (armature == null)
            {
                return;
            }

            editorWitchPreviewRoot = armature.gameObject;
            SetEditorPreviewHideFlags(editorWitchPreviewRoot);
            editorWitchPreviewRoot.transform.SetParent(witchMount, false);
            editorWitchPreviewRoot.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                witchMount,
                new Vector3(0f, -35f, 0f));
            editorWitchPreviewRoot.transform.localScale = new Vector3(
                mlpConstants.PixelPerfectCharacterScale * 1.22f,
                mlpConstants.PixelPerfectCharacterScale * 1.22f,
                1f);
            mlpPlayersData.ApplyCharacter(armature, WitchCharacterId);
            armature.StopAtStart("blockStart");
            HidePreviewBall(armature);
            ApplyWitchSortingOrder(armature);
        }

        /// <summary>
        /// Removed witch preview object used only by editor.
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
        /// Flag the editor preview object so that it will not be saved to the scene or build.

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
