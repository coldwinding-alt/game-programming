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
        DemoMove,
        DemoJump,
        DemoShoot,
        DemoPump,
        DemoDash,
        DemoSteal,
        DemoBlock
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

        private DBLiteArmature witchArmature;
        private rimrushHelpPage currentPage = rimrushHelpPage.Keyboard;
        private rimrushHelpDemo currentDemo = rimrushHelpDemo.Block;
        private bool initialized;
        private bool visible;
        private float panelTime;
        private float demoTimer;
        private bool demoToggle;
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

        public static void ShowKeyboardPage()
        {
            var panel = FindActivePanel(createFallback: true);
            if (panel != null)
            {
                panel.Show(rimrushHelpPage.Keyboard);
            }
        }

        public static void HideActive()
        {
            var panel = activePanel != null ? activePanel : FindScenePanel();
            if (panel != null)
            {
                panel.Hide();
            }
        }

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

        private void OnDestroy()
        {
            if (activePanel == this)
            {
                activePanel = null;
            }
        }

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
            SetPage(page);
            SelectDemo(rimrushHelpDemo.Block, forceRestart: true);
        }

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

        private void HideImmediate()
        {
            visible = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            BuildWitchPreview();
            SetPage(currentPage);
            SelectDemo(currentDemo, forceRestart: true);
        }

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
                    SetPage(rimrushHelpPage.Rules);
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
            }
        }

        private void SetPage(rimrushHelpPage page)
        {
            currentPage = page;
            if (keyboardPageRoot != null)
            {
                keyboardPageRoot.SetActive(page == rimrushHelpPage.Keyboard);
            }

            if (rulesPageRoot != null)
            {
                rulesPageRoot.SetActive(page == rimrushHelpPage.Rules);
            }

            SetTabVisual(keyboardTabPlate, keyboardTabText, page == rimrushHelpPage.Keyboard);
            SetTabVisual(rulesTabPlate, rulesTabText, page == rimrushHelpPage.Rules);

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
                        button.Action == rimrushHelpButtonAction.KeyboardTab && page == rimrushHelpPage.Keyboard ||
                        button.Action == rimrushHelpButtonAction.RulesTab && page == rimrushHelpPage.Rules ||
                        IsDemoButtonSelected(button.Action));
                }
            }
        }

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

        private void UpdateDemoCopy()
        {
            SetText(demoTitleText, DemoTitle(currentDemo));
            SetText(demoDescriptionText, DemoDescription(currentDemo));
            SetText(demoCoachText, DemoCoachNote(currentDemo));
        }

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

        private static string DemoDescription(rimrushHelpDemo demo)
        {
            switch (demo)
            {
                case rimrushHelpDemo.Move:
                    return "Hold A/D or LEFT/RIGHT.\nRelease to stop.";
                case rimrushHelpDemo.Jump:
                    return "Press W or UP.\nJump for air shots.";
                case rimrushHelpDemo.Shoot:
                    return "Press ACTION with the ball.\nAir shots release before landing.";
                case rimrushHelpDemo.Pump:
                    return "Hold S or DOWN with the ball.\nUse it to fake the shot.";
                case rimrushHelpDemo.Dash:
                    return "Double-tap left or right.\nDash has a short cooldown.";
                case rimrushHelpDemo.Steal:
                    return "Press ACTION near the dribbler.\nSteal from close range.";
                default:
                    return "Hold S or DOWN to block.\nJump into the shot path.";
            }
        }

        private static string DemoCoachNote(rimrushHelpDemo demo)
        {
            switch (demo)
            {
                case rimrushHelpDemo.Move:
                    return "Tip: stop before shooting.";
                case rimrushHelpDemo.Jump:
                    return "Tip: time your jump.";
                case rimrushHelpDemo.Shoot:
                    return "Tip: release at the top.";
                case rimrushHelpDemo.Pump:
                    return "Tip: pump only works with the ball.";
                case rimrushHelpDemo.Dash:
                    return "Tip: dash beats steals.";
                case rimrushHelpDemo.Steal:
                    return "Tip: steal from the front.";
                default:
                    return "Tip: use DOWN to block.";
            }
        }

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
                    button.SetSelected(IsDemoButtonSelected(button.Action));
                }
            }
        }

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

        private void ApplyWitchSortingOrder()
        {
            ApplyWitchSortingOrder(witchArmature);
        }

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

        private static void HidePreviewBall(DBLiteArmature armature)
        {
            if (armature == null)
            {
                return;
            }

            armature.SetSlotHidden("ball", true);
            armature.SetSlotHidden("ball_front", true);
        }

        private static void SetText(TMP_Text textMesh, string value)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.text = value ?? string.Empty;
            textMesh.ForceMeshUpdate();
        }

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
            SpriteRenderer configuredWitchSpotlight)
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
        }

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

            SetTabVisual(keyboardTabPlate, keyboardTabText, true);
            SetTabVisual(rulesTabPlate, rulesTabText, false);
            UpdateDemoCopy();
            UpdateDemoSelections();
            EnsureEditorWitchPreview();
        }

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
