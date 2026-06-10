// 帮助面板（按键说明和游戏规则）
// 玩家按帮助按钮后弹出，有键盘操作和游戏规则两个页面。
// 键盘页面可以点选不同动作，女巫角色会播放对应的演示动画。

using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>帮助面板页面类型：键盘操作页面或游戏规则页面。</summary>
    public enum mlpHelpPage
    {
        Keyboard,
        Rules
    }

    /// <summary>帮助面板演示动作类型：移动、跳跃、投篮、假动作、扣篮、抢断、盖帽等可演示的操作。</summary>
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

    /// <summary>帮助面板按钮动作：切换页面、选择演示、关闭面板等按钮可执行的操作。</summary>
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
    /// 帮助面板：按帮助按钮后弹出的界面，有键盘操作和游戏规则两个页面，可以点选不同动作查看女巫角色的演示动画。
    /// </summary>
    public sealed class mlpHelpPanel : MonoBehaviour
    {
        private const string PrefabResourcePath = "mlp/Prefabs/UI/MlpHelpPanel";
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

        private static mlpHelpPanel activePanel;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject keyboardPageRoot;
        [SerializeField] private GameObject rulesPageRoot;
        [SerializeField] private mlpHelpButton[] buttons;
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
        [SerializeField] private mlpHelpButton quickTestToggleButton;
        [SerializeField] private SpriteRenderer quickTestTogglePlate;
        [SerializeField] private TMP_Text quickTestToggleText;
        [SerializeField] private mlpHelpButton quickTestInfoButton;
        [SerializeField] private GameObject quickTestInfoRoot;
        [SerializeField] private TMP_Text quickTestInfoText;

        private DBLiteArmature witchArmature;
        private mlpHelpPage currentPage = mlpHelpPage.Keyboard;
        private mlpHelpDemo currentDemo = mlpHelpDemo.Block;
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
        /// 打开帮助面板的键盘操作页面。如果没有面板则创建一个。
        /// </summary>
        public static void ShowKeyboardPage()
        {
            // 1. 查找已有的帮助面板实例，如果没有则从预制体创建一个新的
            var panel = FindActivePanel(createFallback: true);
            // 2. 如果找到了面板，显示键盘操作页面
            if (panel != null)
            {
                panel.Show(mlpHelpPage.Keyboard);
            }
        }

        /// <summary>
        /// 关闭当前打开的帮助面板。
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
        /// 游戏启动时将此面板记录为活动面板。
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
        /// 组件启用时更新编辑器预览。
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
        /// Inspector 中的值改变时刷新编辑器预览。
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
        /// 对象销毁时清除活动面板引用。
        /// </summary>
        private void OnDestroy()
        {
            if (activePanel == this)
            {
                activePanel = null;
            }
        }

        /// <summary>
        /// 每帧执行：面板动画、按钮检测、演示更新，以及 Escape 键关闭处理。
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
            // 1. 面板未打开时跳过所有更新
            if (!visible)
            {
                return;
            }

            // 2. 累计面板打开时间，用于播放入场动画
            panelTime += Time.unscaledDeltaTime;
            // 3. 更新面板的缩放入场动画和女巫聚光灯脉冲效果
            UpdatePanelEntrance();
            // 4. 检测所有按钮的鼠标悬停和点击
            UpdateButtons();
            // 5. 更新女巫演示动画（计时器到期时重新播放）
            UpdateDemo(Time.unscaledDeltaTime);

            // 6. 按 Escape 键关闭帮助面板
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }

        /// <summary>
        /// 确保女巫角色始终渲染在其他精灵之上。
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
        /// 显示帮助面板页面。
        /// </summary>
        public void Show(mlpHelpPage page)
        {
            // 1. 如果面板根节点未指定，使用当前 GameObject
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            // 2. 标记面板为可见，激活面板根节点
            visible = true;
            panelRoot.SetActive(true);
            // 3. 重置面板计时器（用于入场动画），设置略微缩小的初始缩放
            panelTime = 0f;
            transform.localScale = Vector3.one * 0.985f;

            // 4. 首次打开时构建女巫模型和 UI（后续调用会跳过）
            EnsureInitialized();
            // 5. 设置为键盘操作页面，隐藏信息面板
            SetPage(mlpHelpPage.Keyboard);
            SetQuickTestInfoVisible(false);
            // 6. 默认选中盖帽演示并重新播放动画
            SelectDemo(mlpHelpDemo.Block, forceRestart: true);
        }

        /// <summary>
        /// 隐藏帮助面板。默认播放按钮音效。
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
        /// 立即隐藏面板，不播放任何音效（用于启动时）。
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
        /// 首次显示面板时设置女巫模型和页面。
        /// </summary>
        private void EnsureInitialized()
        {
            // 1. 如果已经初始化过，直接返回
            if (initialized)
            {
                return;
            }

            // 2. 标记为已初始化
            initialized = true;
            // 3. 隐藏旧版标签页（现在只用键盘操作页面）
            HideLegacyTabs();
            // 4. 隐藏开发测试控件（普通玩家不需要看到）
            HideQuickTestControls();
            // 5. 创建女巫角色模型，用于播放动作演示动画
            BuildWitchPreview();
            // 6. 设置当前页面和选中的演示动作
            SetPage(currentPage);
            SelectDemo(currentDemo, forceRestart: true);
        }

        /// <summary>
        /// 创建女巫角色模型以便播放演示动画。
        /// </summary>
        private void BuildWitchPreview()
        {
            // 1. 如果挂载点不存在或已创建过女巫模型，直接返回
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
            // 2. 创建女巫角色的骨骼动画模型
            witchArmature = mlpPlayersData.BuildGameplayArmature("HelpWitchPreview");
            if (witchArmature == null)
            {
                return;
            }

            // 3. 将模型挂载到面板上的指定位置，设置合适的缩放比例
            witchArmature.transform.SetParent(witchMount, false);
            witchArmature.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                witchMount,
                new Vector3(0f, -35f, 0f));
            witchArmature.transform.localScale = new Vector3(
                mlpConstants.PixelPerfectCharacterScale * 1.22f,
                mlpConstants.PixelPerfectCharacterScale * 1.22f,
                1f);
            // 4. 应用女巫角色的外观（颜色、服装等），隐藏手中的篮球
            mlpPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
            HidePreviewBall(witchArmature);
            // 5. 提升女巫的渲染层级，确保她显示在面板背景之上
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// 面板打开动画，带有快速缩放弹跳和微妙的聚光灯脉冲效果。
        /// </summary>
        private void UpdatePanelEntrance()
        {
            // 1. 计算入场动画进度（0→1），使用缓出曲线（三次方）
            var t = Mathf.Clamp01(panelTime / 0.12f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            // 2. 面板从 0.985 倍缩放弹入到 1.0 倍
            transform.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, eased);

            // 3. 女巫聚光灯持续做微弱的呼吸脉冲动画
            if (witchSpotlight != null)
            {
                var pulse = Mathf.Sin(Time.unscaledTime * 2.3f) * 0.025f;
                witchSpotlight.transform.localScale = Vector3.one * (1f + pulse);
            }
        }

        /// <summary>
        /// 每帧检测所有按钮的鼠标点击，并将命中路由到 HandleButton。
        /// </summary>
        private void UpdateButtons()
        {
            // 1. 获取主相机（用于鼠标坐标转换）
            var camera = Camera.main;
            if (buttons == null)
            {
                return;
            }

            // 2. 遍历所有按钮，检测鼠标悬停和点击
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                // 3. Tick 返回 true 表示玩家完成了一次有效点击
                if (button == null || !button.Tick(camera))
                {
                    continue;
                }

                // 4. 播放按钮音效，将点击事件路由到对应的操作
                mlpAudio.Instance?.Play(mlpAssets.Sounds.Button, 0.75f);
                HandleButton(button.Action);
            }
        }

        /// <summary>
        /// 将按钮点击路由到正确的操作（关闭、切换标签页、选择演示等）。
        /// </summary>
        private void HandleButton(mlpHelpButtonAction action)
        {
            // 1. 根据按钮动作类型执行对应操作
            switch (action)
            {
                // 2. 关闭帮助面板（不播放额外音效，因为按钮音效已播放）
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
        /// 将帮助面板保持在单一操作说明页面。
        /// </summary>
        private void SetPage(mlpHelpPage page)
        {
            // 1. 固定使用键盘操作页面（规则页面已废弃）
            currentPage = mlpHelpPage.Keyboard;
            // 2. 显示键盘操作页面，隐藏游戏规则页面
            if (keyboardPageRoot != null)
            {
                keyboardPageRoot.SetActive(true);
            }

            if (rulesPageRoot != null)
            {
                rulesPageRoot.SetActive(false);
            }

            // 3. 隐藏旧版标签页按钮，刷新测试开关状态
            HideLegacyTabs();
            RefreshQuickTestToggle();

            // 4. 更新所有按钮的选中高亮状态（演示按钮、测试开关等）
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
        /// 通过改变标签页的底板和文字颜色来高亮选中的标签页。
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
            // 1. 非运行时或面板不存在时跳过
            if (panelRoot == null || !Application.isPlaying)
            {
                return;
            }

            // 2. 加载标签页和卡片精灵资源（用于创建按钮背景）
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
        /// 选择要演示的动作（移动、跳跃、投篮、虚晃、冲刺、抢断、盖帽）。
        /// </summary>
        private void SelectDemo(mlpHelpDemo demo, bool forceRestart = false)
        {
            // 1. 如果点击的是已选中的演示，只重新播放动画即可
            if (!forceRestart && currentDemo == demo)
            {
                RestartDemoAnimation();
                return;
            }

            // 2. 设置当前选中的演示类型
            currentDemo = demo;
            // 3. 重置演示计时器和交替播放标记（用于盖帽和假动作的两段式动画）
            demoTimer = 999f;
            demoToggle = false;
            // 4. 更新标题、描述和教练提示文字
            UpdateDemoCopy();
            // 5. 高亮当前选中的演示按钮行
            UpdateDemoSelections();
            // 6. 播放女巫的对应动作动画
            RestartDemoAnimation();
        }

        /// <summary>
        /// 计时器到期时重新开始演示动画。
        /// </summary>
        private void UpdateDemo(float dt)
        {
            // 1. 如果没有女巫模型或不在键盘页面，跳过
            if (witchArmature == null || currentPage != mlpHelpPage.Keyboard)
            {
                return;
            }

            // 2. 累加演示计时器
            demoTimer += dt;
            // 3. 获取当前演示动画的重复间隔（秒），到期则重新播放
            var repeat = DemoRepeatFor(currentDemo);
            if (demoTimer >= repeat)
            {
                RestartDemoAnimation();
            }
        }

        /// <summary>
        /// 播放当前选中演示对应的女巫动画。
        /// </summary>
        private void RestartDemoAnimation()
        {
            // 1. 如果没有女巫模型则跳过
            if (witchArmature == null)
            {
                return;
            }

            // 2. 重置演示计时器，开始计时
            demoTimer = 0f;
            // 3. 根据选中的演示类型播放对应的骨骼动画
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
                // 4. 假动作和盖帽使用两段交替动画（前摇 + 收招）
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

            // 5. 隐藏女巫手中的篮球，提升渲染层级确保显示在面板之上
            HidePreviewBall(witchArmature);
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// 返回每个演示动画重复前的持续时间（秒）。
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
        /// 更新所选演示的标题、描述和教练提示文字。
        /// </summary>
        private void UpdateDemoCopy()
        {
            SetText(demoTitleText, DemoTitle(currentDemo));
            SetText(demoDescriptionText, DemoDescription(currentDemo));
            SetText(demoCoachText, DemoCoachNote(currentDemo));
        }

        /// <summary>
        /// 返回每种演示类型的显示标题（如 "MOVE"、"JUMP"）。
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
        /// 返回每种演示类型的按键操作说明。
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
        /// 返回每种演示类型的简短教练提示。
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
        /// 高亮当前选中演示的按钮和行。
        /// </summary>
        private void UpdateDemoSelections()
        {
            // 1. 高亮当前选中的演示行背景（绿色 = 选中，深色 = 未选中）
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

            // 2. 更新所有按钮的选中状态（演示按钮和测试开关按钮）
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
        /// 检查按钮是否与当前选中的演示匹配。
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
        /// 提升女巫的排序层使其渲染在面板背景之前。
        /// </summary>
        private void ApplyWitchSortingOrder()
        {
            ApplyWitchSortingOrder(witchArmature);
        }

        /// <summary>
        /// 提升给定骨骼动画上所有精灵的排序层，使其绘制在其他 UI 之上。
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
        /// 隐藏女巫预览中的球精灵，只显示角色本身。
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
        /// 安全地设置 TextMeshPro 标签的文字（优雅处理 null）。
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
        /// 查找活动的帮助面板，如果 createFallback 为 true 则从预制体生成一个。
        /// </summary>
        private static mlpHelpPanel FindActivePanel(bool createFallback)
        {
            // 1. 如果已有缓存的活动面板，直接返回
            if (activePanel != null)
            {
                return activePanel;
            }

            // 2. 尝试在当前场景中查找已存在的帮助面板
            activePanel = FindScenePanel();
            if (activePanel != null || !createFallback)
            {
                return activePanel;
            }

            // 3. 场景中没有面板，且允许创建回退实例：从预制体加载
            var prefab = Resources.Load<mlpHelpPanel>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Missing help panel prefab at Resources/{PrefabResourcePath}.");
                return null;
            }

            // 4. 实例化预制体作为运行时回退面板
            activePanel = Object.Instantiate(prefab);
            activePanel.name = "MlpHelpPanel_RuntimeFallback";
            return activePanel;
        }

        /// <summary>
        /// 查找当前场景中已存在的帮助面板。
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
        /// 从预制体构建器连接所有序列化引用（仅编辑器）。
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
        /// 在 Unity 编辑器中显示面板的实时预览（非运行时）。
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
        /// 为编辑器场景视图创建或复用女巫预览。
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
        /// 移除仅编辑器使用的女巫预览对象。
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
        /// 标记编辑器预览对象，使其不会被保存到场景或构建中。
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
