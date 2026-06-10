// 教程界面覆盖层
// 在教程模式下覆盖在游戏画面上方，显示操作提示、进度点、女巫角色讲解、技能演示动画和按键提示。教程完成后的结算界面也在这里管理。

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mlp
{
    /// <summary>教程覆盖层命令：无、下一步、显示技能演示、显示完成画面等控制指令。</summary>
    public enum mlpTutorialOverlayCommand
    {
        None,
        SkipStep,
        ReturnToMenu,
        ReplayTutorial,
        StartTraining,
        StartQuickMatch
    }

    /// <summary>
    /// 教程界面覆盖层：在游戏画面上方显示操作提示、进度点、女巫讲解、技能演示和按键提示，教程完成后显示结算界面。
    /// </summary>
    public sealed class mlpTutorialOverlay : MonoBehaviour
    {
        private const int WitchCharacterId = 6;
        private const int CanvasSortingOrder = 1400;
        private const int WitchSortingOrderBase = 1425;
        private const int ReferenceWidth = 800;
        private const int ReferenceHeight = 480;
        private const float GuideLeft = 122f;
        private const float GuideTop = 14f;
        private const float GuideWidth = 476f;
        private const float GuideHeight = 100f;
        private const float KeyChipWidth = 76f;
        private const float KeyChipHeight = 32f;
        private const float KeyChipGap = 8f;
        private const int ProgressDotCount = 10;

        private static mlpTutorialOverlay activeOverlay;
        private static Sprite solidSprite;
        private static Sprite circleSprite;
        private static Sprite ringSprite;
        private static Sprite witchPortraitSprite;

        private Canvas canvas;
        private RectTransform overlayRoot;
        private RectTransform maskTop;
        private RectTransform maskBottom;
        private RectTransform maskLeft;
        private RectTransform maskRight;
        private RectTransform focusFrame;
        private RectTransform focusGlow;
        private RectTransform targetZone;
        private RectTransform targetGlow;
        private RectTransform apexRing;
        private RectTransform energyPulse;
        private RectTransform trajectoryRoot;
        private GameObject scoringGuideRoot;
        private Image scoringLeftFill;
        private Image scoringRightFill;
        private TextMeshProUGUI scoringLeftLabel;
        private TextMeshProUGUI scoringRightLabel;
        private TextMeshProUGUI scoringLineLabel;
        private readonly List<Image> trajectoryDots = new List<Image>();
        private readonly List<Image> progressDots = new List<Image>();
        private readonly List<GameObject> keyChipRoots = new List<GameObject>();
        private readonly List<TextMeshProUGUI> keyChipLabels = new List<TextMeshProUGUI>();

        private GameObject headerRoot;
        private GameObject narratorRoot;
        private GameObject outroRoot;
        private TextMeshProUGUI stepText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI goalText;
        private TextMeshProUGUI narratorText;
        private TextMeshProUGUI feedbackText;
        private TextMeshProUGUI outroTitleText;
        private TextMeshProUGUI outroBodyText;
        private Image narratorOrb;
        private Image headerGlow;
        private Image targetFill;
        private Image targetGlowImage;
        private Image energyPulseImage;
        private Image apexRingImage;
        private Button replayButton;
        private Button trainingButton;
        private Button quickMatchButton;
        private Button skipButton;

        private DBLiteArmature witchArmature;
        private GameObject witchFallbackRoot;
        private RectTransform witchFallbackRect;
        private Vector3 witchBaseLocalPosition;
        private Vector3 witchBaseLocalScale = Vector3.one;
        private Vector2 witchFallbackBasePosition;
        private bool initialized;
        private bool visible;
        private float feedbackTimer;
        private float visibleTime;
        private float effectTime;
        private mlpTutorialOverlayCommand pendingCommand;

        public static mlpTutorialOverlay Active => FindOrCreate();

        /// <summary>
        /// 注册为活跃的教程覆盖层，首次使用时构建 UI。
        /// </summary>
        private void Awake()
        {
            if (activeOverlay == null || activeOverlay == this)
            {
                activeOverlay = this;
            }

            EnsureInitialized();
            Hide();
        }

        /// <summary>
        /// 覆盖层销毁时清除单例引用。
        /// </summary>
        private void OnDestroy()
        {
            if (activeOverlay == this)
            {
                activeOverlay = null;
            }
        }

        /// <summary>
        /// 驱动脉冲动画，淡出定时反馈消息。
        /// </summary>
        private void Update()
        {
            if (!visible)
            {
                return;
            }

            visibleTime += Time.unscaledDeltaTime;
            effectTime += Time.unscaledDeltaTime;
            AnimatePulse();

            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.unscaledDeltaTime;
                if (feedbackTimer <= 0f && feedbackText != null)
                {
                    feedbackText.text = string.Empty;
                }
            }
        }

        /// <summary>
        /// 每帧确保女巫精灵渲染在覆盖层上方。
        /// </summary>
        private void LateUpdate()
        {
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// 在第一个练习开始前显示开场画面。
        /// </summary>
        public void ShowPrelude(string title, string subtitle, string narration, params string[] keys)
        {
            ShowInternal();
            SetProgress(-1, 0);
            SetCopy("TUTORIAL", title, string.Empty, narration, keys);
            SetGoal(string.Empty);
            SetSkipVisible(true);
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetScoringGuide(0, false);
            SetTrajectory(null);
            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 显示带编号的练习步骤，包含进度点和按键提示。
        /// </summary>
        public void ShowStep(int currentIndex, int total, string title, string subtitle, string goal, string narration, params string[] keys)
        {
            ShowInternal();
            SetProgress(currentIndex, total);
            SetCopy($"{currentIndex + 1}/{Mathf.Max(1, total)}", title, string.Empty, narration, keys);
            SetGoal(string.Empty);
            SetSkipVisible(true);
            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 刷新标题文字和按键提示，不改变布局结构。
        /// </summary>
        public void UpdateCopy(string title, string subtitle, string goal, string narration, params string[] keys)
        {
            ShowInternal();
            var currentNarration = narratorText != null ? narratorText.text : string.Empty;
            SetCopy(stepText != null ? stepText.text : string.Empty, title, string.Empty, currentNarration, keys);
            SetGoal(string.Empty);
        }

        /// <summary>
        /// 将指定矩形区域外的所有内容变暗，以高亮聚焦 UI 区域。
        /// </summary>
        public void SetFocusRect(float left, float top, float width, float height)
        {
            // 1. 确保 UI 已经构建完成
            EnsureInitialized();
            // 2. 判断是否需要聚焦（宽高都大于 0 才有效）
            var hasFocus = width > 0f && height > 0f;
            // 3. 根据是否需要聚焦来显示或隐藏四块遮罩和聚焦框
            SetMarkerActive(maskTop, hasFocus);
            SetMarkerActive(maskBottom, hasFocus);
            SetMarkerActive(maskLeft, hasFocus);
            SetMarkerActive(maskRight, hasFocus);
            SetMarkerActive(focusFrame, hasFocus);
            SetMarkerActive(focusGlow, hasFocus);
            if (!hasFocus)
            {
                return;
            }

            // 4. 将聚焦区域限制在 800x480 画面范围内
            var clampedLeft = Mathf.Clamp(left, 0f, ReferenceWidth);
            var clampedTop = Mathf.Clamp(top, 0f, ReferenceHeight);
            var clampedWidth = Mathf.Clamp(width, 0f, ReferenceWidth - clampedLeft);
            var clampedHeight = Mathf.Clamp(height, 0f, ReferenceHeight - clampedTop);
            var right = clampedLeft + clampedWidth;
            var bottom = clampedTop + clampedHeight;

            // 5. 计算并设置四块遮罩的位置，让它们围绕聚焦区域形成"暗角"效果
            SetTopLeftRect(maskTop, 0f, 0f, ReferenceWidth, clampedTop);
            SetTopLeftRect(maskBottom, 0f, bottom, ReferenceWidth, Mathf.Max(0f, ReferenceHeight - bottom));
            SetTopLeftRect(maskLeft, 0f, clampedTop, clampedLeft, clampedHeight);
            SetTopLeftRect(maskRight, right, clampedTop, Mathf.Max(0f, ReferenceWidth - right), clampedHeight);
            // 6. 设置金色聚焦边框和外围发光效果的位置和大小
            SetTopLeftRect(focusFrame, clampedLeft, clampedTop, clampedWidth, clampedHeight);
            SetTopLeftRect(focusGlow, clampedLeft - 16f, clampedTop - 16f, clampedWidth + 32f, clampedHeight + 32f);
        }

        /// <summary>
        /// 移除聚焦高亮，恢复全屏可见。
        /// </summary>
        public void ClearFocus()
        {
            SetFocusRect(0f, 0f, 0f, 0f);
        }

        /// <summary>
        /// 高亮玩家需要瞄准的目标区域。
        /// </summary>
        public void SetTargetRect(float left, float top, float width, float height)
        {
            EnsureInitialized();
            var hasTarget = width > 0f && height > 0f;
            SetMarkerActive(targetZone, hasTarget);
            SetMarkerActive(targetGlow, hasTarget);
            if (!hasTarget)
            {
                return;
            }

            var clampedLeft = Mathf.Clamp(left, 0f, ReferenceWidth);
            var clampedTop = Mathf.Clamp(top, 0f, ReferenceHeight);
            var clampedWidth = Mathf.Clamp(width, 0f, ReferenceWidth - clampedLeft);
            var clampedHeight = Mathf.Clamp(height, 0f, ReferenceHeight - clampedTop);
            SetTopLeftRect(targetZone, clampedLeft, clampedTop, clampedWidth, clampedHeight);
            SetCenteredRect(
                targetGlow,
                clampedLeft + clampedWidth * 0.5f,
                clampedTop + clampedHeight * 0.5f,
                clampedWidth + 44f,
                clampedHeight + 44f);
        }

        /// <summary>
        /// 显示或隐藏标记投篮弧线最高点的脉冲环。
        /// </summary>
        public void SetApexRing(Vector2 center, float size, bool active)
        {
            EnsureInitialized();
            SetMarkerActive(apexRing, active && size > 0f);
            if (active && size > 0f)
            {
                SetCenteredRect(apexRing, center.x, center.y, size, size);
            }
        }

        /// <summary>
        /// 显示或隐藏投篮通道附近的能量脉冲光效。
        /// </summary>
        public void SetEnergyPulse(bool active)
        {
            EnsureInitialized();
            SetMarkerActive(energyPulse, active);
            if (active)
            {
                SetCenteredRect(energyPulse, 102f, 56f, 208f, 102f);
            }
        }

        /// <summary>
        /// 绘制一串圆点，显示篮球的预测轨迹。
        /// </summary>
        public void SetTrajectory(IReadOnlyList<Vector2> points)
        {
            EnsureInitialized();
            for (var i = 0; i < trajectoryDots.Count; i++)
            {
                var dot = trajectoryDots[i];
                var visible = points != null && i < points.Count;
                SetMarkerActive(dot.rectTransform, visible);
                if (!visible)
                {
                    continue;
                }

                var size = Mathf.Lerp(12f, 6f, i / Mathf.Max(1f, trajectoryDots.Count - 1f));
                SetCenteredRect(dot.rectTransform, points[i].x, points[i].y, size, size);
            }
        }

        /// <summary>
        /// 显示或隐藏 2 分 / 3 分得分区域覆盖层。
        /// </summary>
        public void SetScoringGuide(int scoringSide, bool active)
        {
            EnsureInitialized();
            if (scoringGuideRoot == null)
            {
                return;
            }

            scoringGuideRoot.SetActive(active);
            if (!active)
            {
                return;
            }

            var leftIsThree = scoringSide == -1;
            SetScoringZone(scoringLeftFill, scoringLeftLabel, leftIsThree);
            SetScoringZone(scoringRightFill, scoringRightLabel, !leftIsThree);
            if (scoringLineLabel != null)
            {
                scoringLineLabel.text = "2PT / 3PT LINE";
            }
        }

        /// <summary>
        /// 显示一条彩色反馈消息，延迟后自动淡出。
        /// </summary>
        public void ShowFeedback(string message, Color color, float duration = 1.35f)
        {
            EnsureInitialized();
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackTimer = duration;
        }

        /// <summary>
        /// 显示结束画面，包含重玩、训练和返回菜单等选项。
        /// </summary>
        public void ShowOutro(string characterName, string skillName)
        {
            // 1. 激活覆盖层并重置计时器
            ShowInternal();
            // 2. 显示第 9/10 步的进度点（全部完成）
            SetProgress(9, 10);
            // 3. 设置结束画面的标题和讲解文字
            SetCopy("CLEAR", "READY TO PLAY", string.Empty, "Nice work. Choose your next run.", null);
            SetGoal(string.Empty);
            SetSkipVisible(false);
            // 4. 清除所有运行时视觉元素（聚焦框、得分引导线、轨迹点等）
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetScoringGuide(0, false);
            SetTrajectory(null);
            // 5. 显示结束画面卡片
            if (outroRoot != null)
            {
                outroRoot.SetActive(true);
            }

            // 6. 设置结束画面标题和角色信息
            if (outroTitleText != null)
            {
                outroTitleText.text = "WHERE NEXT?";
            }

            if (outroBodyText != null)
            {
                outroBodyText.text = $"{characterName}\n{skillName}\nKeep the rhythm.";
            }
        }

        /// <summary>
        /// 返回待处理的按钮指令并重置为 None。
        /// </summary>
        public mlpTutorialOverlayCommand ConsumeCommand()
        {
            var command = pendingCommand;
            pendingCommand = mlpTutorialOverlayCommand.None;
            return command;
        }

        /// <summary>
        /// 隐藏整个教程覆盖层。
        /// </summary>
        public void Hide()
        {
            visible = false;
            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 查找场景中已有的覆盖层实例，若不存在则运行时创建一个新实例。
        /// </summary>
        private static mlpTutorialOverlay FindOrCreate()
        {
            if (activeOverlay != null)
            {
                return activeOverlay;
            }

            var existing = FindObjectOfType<mlpTutorialOverlay>();
            if (existing != null)
            {
                activeOverlay = existing;
                return existing;
            }

            var root = new GameObject("MlpTutorialOverlayRuntime");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(root);
            }

            activeOverlay = root.AddComponent<mlpTutorialOverlay>();
            return activeOverlay;
        }

        /// <summary>
        /// 首次调用时构建 UI，后续调用仅修正相机设置。
        /// </summary>
        private void EnsureInitialized()
        {
            // 1. 如果已经初始化过，只需确保画布绑定了正确的相机即可
            if (initialized)
            {
                EnsureCanvasCamera();
                return;
            }

            // 2. 标记为已初始化，防止重复构建
            initialized = true;
            // 3. 生成纯色、圆形和环形纹理精灵（UI 绘图的基本素材）
            EnsureSprites();
            // 4. 确保场景中有事件系统（没有按钮点击就没法工作）
            EnsureEventSystem();
            // 5. 用纯代码构建整个教程覆盖层 UI（不依赖预制体）
            BuildUi();
            // 6. 确保画布绑定了正确的相机
            EnsureCanvasCamera();
        }

        /// <summary>
        /// 激活覆盖层根节点，重置计时器和反馈文字。
        /// </summary>
        private void ShowInternal()
        {
            EnsureInitialized();
            visible = true;
            visibleTime = 0f;
            effectTime = 0f;
            pendingCommand = mlpTutorialOverlayCommand.None;
            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(true);
            }

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
        }

        /// <summary>
        /// 纯代码构建整个教程覆盖层（不依赖预制体）。
        /// </summary>
        private void BuildUi()
        {
            // 1. 创建画布（Canvas）——所有 UI 元素的容器，使用相机渲染模式
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = CanvasSortingOrder;
            gameObject.AddComponent<GraphicRaycaster>();
            // 2. 设置画布缩放器——让 UI 在不同屏幕大小下自动缩放，参考分辨率 800x480
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 3. 创建覆盖层根节点（所有教程 UI 的最外层容器），默认隐藏
            overlayRoot = CreateRootRect("OverlayRoot", transform);
            overlayRoot.gameObject.SetActive(false);

            var blurVeil = CreateImage("BlurVeil", overlayRoot, solidSprite, new Color(0.03f, 0.06f, 0.09f, 0.14f));
            StretchToParent(blurVeil.rectTransform);

            // 4. 创建四块半透明遮罩（上下左右），用来变暗聚焦区域外的内容
            maskTop = CreateMask("MaskTop");
            maskBottom = CreateMask("MaskBottom");
            maskLeft = CreateMask("MaskLeft");
            maskRight = CreateMask("MaskRight");

            focusGlow = CreateImage("FocusGlow", overlayRoot, ringSprite, new Color(0.35f, 1f, 0.86f, 0.18f)).rectTransform;
            focusFrame = CreateImage("FocusFrame", overlayRoot, ringSprite, new Color(1f, 0.84f, 0.42f, 0.94f)).rectTransform;
            apexRingImage = CreateImage("ApexRing", overlayRoot, ringSprite, new Color(1f, 0.84f, 0.46f, 0.95f));
            apexRing = apexRingImage.rectTransform;
            energyPulseImage = CreateImage("EnergyPulse", overlayRoot, ringSprite, new Color(0.53f, 1f, 0.86f, 0.82f));
            energyPulse = energyPulseImage.rectTransform;

            targetGlowImage = CreateImage("TargetGlow", overlayRoot, circleSprite, new Color(1f, 0.8f, 0.4f, 0.16f));
            targetGlow = targetGlowImage.rectTransform;
            targetFill = CreateImage("TargetZone", overlayRoot, solidSprite, new Color(1f, 0.82f, 0.44f, 0.16f));
            targetZone = targetFill.rectTransform;

            // 5. 构建 2 分 / 3 分得分区域引导覆盖层
            BuildScoringGuide();

            // 6. 创建 7 个圆点，用于显示篮球预测轨迹
            trajectoryRoot = CreateRootRect("TrajectoryRoot", overlayRoot);
            for (var i = 0; i < 7; i++)
            {
                trajectoryDots.Add(CreateImage($"TrajectoryDot{i}", trajectoryRoot, circleSprite, new Color(0.62f, 1f, 0.9f, 0.85f)));
            }

            // 7. 构建引导卡片（深色面板 + 发光标题 + 步骤编号 + 标题文字 + 副标题 + 目标文字 + 进度点）
            headerRoot = CreateCard("GuideRoot", overlayRoot, GuideLeft, GuideTop, GuideWidth, GuideHeight, 0.92f);
            headerGlow = CreateImage("HeaderGlow", headerRoot.transform as RectTransform, circleSprite, new Color(0.33f, 1f, 0.88f, 0.12f));
            SetCenteredRect(headerGlow.rectTransform, 198f, 48f, 248f, 78f);
            stepText = CreateText("StepText", headerRoot.transform as RectTransform, 212f, 10f, 76f, 17f, 12, new Color32(0x8A, 0xFF, 0xE1, 0xFF), TextAlignmentOptions.Left, LoadBodyFont());
            titleText = CreateText("TitleText", headerRoot.transform as RectTransform, 212f, 26f, 204f, 27f, 24, Color.white, TextAlignmentOptions.Left, LoadTitleFont());
            subtitleText = CreateText("SubtitleText", headerRoot.transform as RectTransform, 212f, 55f, 204f, 17f, 11, new Color32(0xD6, 0xE5, 0xF9, 0xFF), TextAlignmentOptions.Left, LoadBodyFont());
            goalText = CreateText("GoalText", headerRoot.transform as RectTransform, 212f, 74f, 188f, 15f, 11, new Color32(0xFF, 0xD5, 0x7C, 0xFF), TextAlignmentOptions.Left, LoadButtonFont());

            var dotsRoot = CreateRootRect("ProgressDots", headerRoot.transform);
            SetTopLeftRect(dotsRoot, 362f, 13f, 104f, 14f);
            for (var i = 0; i < ProgressDotCount; i++)
            {
                var dot = CreateImage($"ProgressDot{i}", dotsRoot, circleSprite, new Color32(0x35, 0x4C, 0x70, 0xE8));
                SetCenteredRect(dot.rectTransform, 7f + i * 10f, 7f, 6f, 6f);
                progressDots.Add(dot);
            }

            // 8. 构建按键提示区域（最多 5 个"按 [A]"样式的小标签）
            var keysRoot = CreateRootRect("KeysRoot", overlayRoot);
            SetTopLeftRect(keysRoot, GuideLeft, GuideTop + GuideHeight + 8f, GuideWidth, KeyChipHeight);
            for (var i = 0; i < 5; i++)
            {
                var chip = CreateChip(keysRoot, i);
                keyChipRoots.Add(chip);
                var keyLabel = chip.transform.Find("KeyLabel") as RectTransform;
                keyChipLabels.Add(keyLabel != null ? keyLabel.GetComponent<TextMeshProUGUI>() : null);
            }

            // 9. 创建反馈文字（练习过程中显示提示，如"Good dash"）和跳过按钮
            feedbackText = CreateText("FeedbackText", overlayRoot, 154f, 176f, 492f, 26f, 18, new Color32(0x9A, 0xFF, 0xDD, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());
            skipButton = CreateButton("SkipStepButton", overlayRoot, 684f, 426f, 92f, 34f, "SKIP", new Color32(0x13, 0x1E, 0x30, 0xDD), new Color32(0xFF, 0xD5, 0x7C, 0xFF), mlpTutorialOverlayCommand.SkipStep);

            // 10. 构建讲解员面板（女巫角色头像 + "WITCH:" 标签 + 讲解文字）
            narratorRoot = CreateRootRect("NarratorRoot", headerRoot.transform).gameObject;
            narratorOrb = CreateImage("NarratorOrb", narratorRoot.transform as RectTransform, circleSprite, new Color(0.33f, 1f, 0.88f, 0.14f));
            SetTopLeftRect(narratorRoot.transform as RectTransform, 0f, 0f, 196f, GuideHeight);
            SetCenteredRect(narratorOrb.rectTransform, 43f, 50f, 68f, 68f);
            var narratorLabel = CreateText("NarratorLabel", narratorRoot.transform as RectTransform, 82f, 14f, 86f, 15f, 11, new Color32(0xFF, 0xD6, 0x80, 0xFF), TextAlignmentOptions.Left, LoadButtonFont());
            narratorLabel.text = "WITCH:";
            narratorText = CreateText("NarratorText", narratorRoot.transform as RectTransform, 82f, 31f, 104f, 54f, 12, Color.white, TextAlignmentOptions.TopLeft, LoadBodyFont());
            narratorText.enableWordWrapping = true;
            narratorText.fontStyle = FontStyles.Bold;

            var witchMount = CreateRootRect("WitchMount", narratorRoot.transform);
            SetTopLeftRect(witchMount, 8f, 12f, 70f, 76f);

            // 11. 构建结束画面卡片（"WHERE NEXT?" 标题 + 重玩/训练/菜单三个按钮）
            outroRoot = CreateCard("OutroRoot", overlayRoot, 170f, 84f, 460f, 300f, 0.96f);
            outroRoot.SetActive(false);
            CreateImage("OutroAura", outroRoot.transform as RectTransform, circleSprite, new Color(1f, 0.72f, 0.35f, 0.12f));
            var auraRect = outroRoot.transform.Find("OutroAura") as RectTransform;
            if (auraRect != null)
            {
                SetCenteredRect(auraRect, 230f, 72f, 340f, 140f);
            }

            outroTitleText = CreateText("OutroTitle", outroRoot.transform as RectTransform, 42f, 28f, 376f, 30f, 30, Color.white, TextAlignmentOptions.Center, LoadTitleFont());
            outroBodyText = CreateText("OutroBody", outroRoot.transform as RectTransform, 42f, 70f, 376f, 72f, 15, new Color32(0xD6, 0xE5, 0xF9, 0xFF), TextAlignmentOptions.Center, LoadBodyFont());
            outroBodyText.enableWordWrapping = true;

            quickMatchButton = CreateButton("QuickMatchButton", outroRoot.transform as RectTransform, 124f, 146f, 212f, 38f, "MAIN MENU", new Color32(0xE8, 0xA4, 0x36, 0xFF), new Color32(0x19, 0x14, 0x0F, 0xFF), mlpTutorialOverlayCommand.ReturnToMenu);
            trainingButton = CreateButton("TrainingButton", outroRoot.transform as RectTransform, 124f, 198f, 212f, 38f, "FREE TRAINING", new Color32(0x26, 0x34, 0x4F, 0xFF), Color.white, mlpTutorialOverlayCommand.StartTraining);
            replayButton = CreateButton("ReplayButton", outroRoot.transform as RectTransform, 124f, 250f, 212f, 38f, "REPLAY TUTORIAL", new Color32(0x26, 0x34, 0x4F, 0xFF), Color.white, mlpTutorialOverlayCommand.ReplayTutorial);

            // 12. 在讲解员面板中生成女巫角色实时模型（用于播放动画演示）
            BuildWitchPreview(witchMount);
            // 13. 初始状态隐藏跳过按钮和所有运行时标记物
            SetSkipVisible(false);
            HideRuntimeMarkers();
        }

        /// <summary>
        /// 在讲解员面板中生成一个实时女巫角色模型。
        /// </summary>
        private void BuildWitchPreview(RectTransform witchMount)
        {
            if (witchMount == null || witchArmature != null || witchFallbackRoot != null)
            {
                return;
            }

            var builtArmature = mlpPlayersData.BuildGameplayArmature("TutorialNarratorWitch");
            if (builtArmature == null)
            {
                BuildWitchPortraitFallback(witchMount);
                return;
            }

            try
            {
                witchArmature = builtArmature;
                witchArmature.transform.SetParent(witchMount, false);
                witchArmature.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                    witchMount,
                    new Vector3(0f, -14f, 0f));
                witchArmature.transform.localScale = new Vector3(
                    mlpConstants.PixelPerfectCharacterScale * 0.74f,
                    mlpConstants.PixelPerfectCharacterScale * 0.74f,
                    1f);
                mlpPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
                witchArmature.Play("idle");
                HidePreviewBall(witchArmature);
                witchBaseLocalPosition = witchArmature.transform.localPosition;
                witchBaseLocalScale = witchArmature.transform.localScale;
            }
            catch (System.Exception)
            {
                if (builtArmature != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(builtArmature.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(builtArmature.gameObject);
                    }
                }

                witchArmature = null;
                BuildWitchPortraitFallback(witchMount);
            }
        }

        /// <summary>
        /// 当实时模型不可用时，使用静态头像精灵替代。
        /// </summary>
        private void BuildWitchPortraitFallback(RectTransform witchMount)
        {
            var sprite = LoadWitchPortraitSprite();
            if (sprite == null)
            {
                return;
            }

            var image = CreateImage("WitchFallbackPortrait", witchMount, sprite, Color.white);
            image.preserveAspect = true;
            witchFallbackRoot = image.gameObject;
            witchFallbackRect = image.rectTransform;
            SetTopLeftRect(witchFallbackRect, 4f, 0f, 62f, 74f);
            witchFallbackBasePosition = witchFallbackRect.anchoredPosition;
        }

        /// <summary>
        /// 更新女巫光球、标题栏和聚焦区域的脉冲光效。
        /// </summary>
        private void AnimatePulse()
        {
            var wave = 0.5f + 0.5f * Mathf.Sin(effectTime * 4.6f);
            var floatWave = Mathf.Sin(effectTime * 3.1f);
            if (narratorOrb != null)
            {
                narratorOrb.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.06f);
                narratorOrb.color = new Color(0.33f, 1f, 0.88f, 0.1f + wave * 0.06f);
            }

            if (witchArmature != null)
            {
                witchArmature.transform.localPosition = witchBaseLocalPosition + new Vector3(0f, floatWave * 1.3f, 0f);
                witchArmature.transform.localScale = witchBaseLocalScale * (1f + wave * 0.025f);
            }
            else if (witchFallbackRect != null)
            {
                witchFallbackRect.anchoredPosition = witchFallbackBasePosition + new Vector2(0f, floatWave * 1.6f);
                witchFallbackRect.localScale = Vector3.one * (1f + wave * 0.035f);
            }

            if (headerGlow != null)
            {
                headerGlow.color = new Color(0.33f, 1f, 0.88f, 0.08f + wave * 0.05f);
            }

            if (focusGlow != null && focusGlow.gameObject.activeSelf)
            {
                focusGlow.localScale = Vector3.one * (1f + wave * 0.03f);
            }

            if (targetGlowImage != null && targetGlowImage.gameObject.activeSelf)
            {
                targetGlowImage.rectTransform.localScale = Vector3.one * (1f + wave * 0.05f);
                targetGlowImage.color = new Color(1f, 0.8f, 0.4f, 0.12f + wave * 0.08f);
            }

            if (apexRingImage != null && apexRingImage.gameObject.activeSelf)
            {
                apexRingImage.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.08f);
                apexRingImage.color = new Color(1f, 0.84f, 0.46f, 0.72f + wave * 0.18f);
            }

            if (energyPulseImage != null && energyPulseImage.gameObject.activeSelf)
            {
                energyPulseImage.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.08f);
                energyPulseImage.color = new Color(0.53f, 1f, 0.86f, 0.34f + wave * 0.18f);
            }

            if (overlayRoot != null)
            {
                overlayRoot.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 隐藏所有聚焦框、目标区、最高点环、能量脉冲、得分区和轨迹标记。
        /// </summary>
        private void HideRuntimeMarkers()
        {
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetScoringGuide(0, false);
            SetTrajectory(null);
        }

        /// <summary>
        /// 更新所有标题和讲解员文字标签，以及按键提示显示。
        /// </summary>
        private void SetCopy(string stepLabel, string title, string subtitle, string narration, IReadOnlyList<string> keys)
        {
            if (stepText != null)
            {
                stepText.text = stepLabel ?? string.Empty;
            }

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? string.Empty;
                subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }

            if (narratorText != null)
            {
                narratorText.text = narration ?? string.Empty;
            }

            SetKeys(keys);
        }

        /// <summary>
        /// 显示或隐藏标题下方的目标说明。
        /// </summary>
        private void SetGoal(string goal)
        {
            if (goalText == null)
            {
                return;
            }

            goalText.text = goal ?? string.Empty;
            goalText.gameObject.SetActive(!string.IsNullOrEmpty(goal));
        }

        /// <summary>
        /// 显示或隐藏跳过按钮。
        /// </summary>
        private void SetSkipVisible(bool active)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 点亮进度点以显示玩家当前所在的步骤。
        /// </summary>
        private void SetProgress(int currentIndex, int total)
        {
            for (var i = 0; i < progressDots.Count; i++)
            {
                var dot = progressDots[i];
                if (dot == null)
                {
                    continue;
                }

                var active = i < total;
                dot.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                dot.color = i < currentIndex
                    ? new Color32(0x8A, 0xFF, 0xE1, 0xFF)
                    : i == currentIndex
                        ? new Color32(0xFF, 0xD5, 0x7C, 0xFF)
                        : new Color32(0x35, 0x4C, 0x70, 0xE8);
            }
        }

        /// <summary>
        /// 显示或隐藏各按键提示标签，并设置其文字内容。
        /// </summary>
        private void SetKeys(IReadOnlyList<string> keys)
        {
            for (var i = 0; i < keyChipRoots.Count; i++)
            {
                var visibleChip = keys != null && i < keys.Count && !string.IsNullOrEmpty(keys[i]);
                keyChipRoots[i].SetActive(visibleChip);
                if (visibleChip && i < keyChipLabels.Count && keyChipLabels[i] != null)
                {
                    keyChipLabels[i].text = keys[i];
                }
            }
        }

        /// <summary>
        /// 构建 2 分 / 3 分得分区域标签和分界线标记。
        /// </summary>
        private void BuildScoringGuide()
        {
            scoringGuideRoot = CreateRootRect("ScoringGuideRoot", overlayRoot).gameObject;

            scoringLeftFill = CreateImage("ScoringLeftFill", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0x35, 0xD8, 0xB8, 0x24));
            SetTopLeftRect(scoringLeftFill.rectTransform, 44f, 336f, 346f, 58f);
            scoringRightFill = CreateImage("ScoringRightFill", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0xFF, 0xB9, 0x4D, 0x24));
            SetTopLeftRect(scoringRightFill.rectTransform, 410f, 336f, 346f, 58f);

            var lineGlow = CreateImage("ScoringLineGlow", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0xFF, 0xD5, 0x7C, 0x66));
            SetTopLeftRect(lineGlow.rectTransform, mlpConstants.Width2 - 2f, 148f, 4f, 268f);
            var lineCore = CreateImage("ScoringLineCore", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0xFF, 0xF4, 0xB2, 0xEE));
            SetTopLeftRect(lineCore.rectTransform, mlpConstants.Width2 - 0.5f, 148f, 1f, 268f);

            scoringLineLabel = CreateText("ScoringLineLabel", scoringGuideRoot.transform as RectTransform, 330f, 314f, 140f, 18f, 13, new Color32(0xFF, 0xF4, 0xB2, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());
            scoringLeftLabel = CreateText("ScoringLeftLabel", scoringGuideRoot.transform as RectTransform, 108f, 354f, 170f, 24f, 20, Color.white, TextAlignmentOptions.Center, LoadButtonFont());
            scoringRightLabel = CreateText("ScoringRightLabel", scoringGuideRoot.transform as RectTransform, 522f, 354f, 170f, 24f, 20, Color.white, TextAlignmentOptions.Center, LoadButtonFont());

            scoringGuideRoot.SetActive(false);
        }

        /// <summary>
        /// 将单个得分区域着色并标记为 2 分区或 3 分区。
        /// </summary>
        private static void SetScoringZone(Image fill, TextMeshProUGUI label, bool isThreePoint)
        {
            if (fill != null)
            {
                fill.color = isThreePoint
                    ? new Color32(0x35, 0xD8, 0xB8, 0x28)
                    : new Color32(0xFF, 0xB9, 0x4D, 0x28);
            }

            if (label != null)
            {
                label.text = isThreePoint ? "3PT RANGE" : "2PT RANGE";
                label.color = isThreePoint
                    ? new Color32(0x8A, 0xFF, 0xE1, 0xFF)
                    : new Color32(0xFF, 0xD5, 0x7C, 0xFF);
            }
        }

        /// <summary>
        /// 如果画布尚未设置相机，则将主相机赋给它。
        /// </summary>
        private void EnsureCanvasCamera()
        {
            if (canvas == null)
            {
                return;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }

        /// <summary>
        /// 如果场景中没有 EventSystem，则创建一个。
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(eventSystem);
            }
        }

        /// <summary>
        /// 生成覆盖层使用的纯色、圆形和环形精灵。
        /// </summary>
        private static void EnsureSprites()
        {
            if (solidSprite != null && circleSprite != null && ringSprite != null)
            {
                return;
            }

            solidSprite = CreateSprite(CreateSolidTexture(4, 4, Color.white), "TutorialSolid");
            circleSprite = CreateSprite(CreateCircleTexture(128, 0.96f), "TutorialCircle");
            ringSprite = CreateSprite(CreateRingTexture(160, 0.72f, 0.9f), "TutorialRing");
        }

        /// <summary>
        /// 加载并缓存女巫角色的头像精灵。
        /// </summary>
        private static Sprite LoadWitchPortraitSprite()
        {
            if (witchPortraitSprite != null)
            {
                return witchPortraitSprite;
            }

            witchPortraitSprite = mlpPlayersData.GetCharacterPortraitSprite(WitchCharacterId, 96f);
            return witchPortraitSprite;
        }

        /// <summary>
        /// 生成一个小尺寸的纯色纹理。
        /// </summary>
        private static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 生成边缘柔和的圆形纹理，用于光效和圆点。
        /// </summary>
        private static Texture2D CreateCircleTexture(int size, float softness)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var normalized = distance / radius;
                    var alpha = Mathf.Pow(Mathf.Clamp01(1f - normalized), softness * 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 生成中空的环形纹理，用于边框轮廓。
        /// </summary>
        private static Texture2D CreateRingTexture(int size, float innerRadius, float outerSoftness)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    var outer = Mathf.Clamp01(1f - distance);
                    var inner = Mathf.Clamp01((distance - innerRadius) / Mathf.Max(0.001f, 1f - innerRadius));
                    var alpha = Mathf.Pow(outer, outerSoftness * 3f) * Mathf.Pow(inner, 0.45f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 将纹理封装为以中心为锚点的命名精灵。
        /// </summary>
        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            texture.name = name;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>
        /// 创建一个拉伸填满父级的 RectTransform。
        /// </summary>
        private static RectTransform CreateRootRect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// 创建半透明深色遮罩面板，用于变暗聚焦区域外的区域。
        /// </summary>
        private RectTransform CreateMask(string name)
        {
            var image = CreateImage(name, overlayRoot, solidSprite, new Color(0.02f, 0.04f, 0.08f, 0.52f));
            return image.rectTransform;
        }

        /// <summary>
        /// 创建带金色边框和阴影的深色卡片面板。
        /// </summary>
        private static GameObject CreateCard(string name, RectTransform parent, float left, float top, float width, float height, float alpha)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            SetTopLeftRect(rect, left, top, width, height);

            var background = root.AddComponent<Image>();
            background.sprite = solidSprite;
            background.color = new Color(0.05f, 0.09f, 0.15f, alpha);
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.75f, 0.35f, 0.28f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            var shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -3f);
            return root;
        }

        /// <summary>
        /// 创建一个不可交互的 Image 元素，使用指定的精灵和颜色。
        /// </summary>
        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 在指定屏幕位置创建带描边的 TextMeshPro 文本标签。
        /// </summary>
        private TextMeshProUGUI CreateText(string name, RectTransform parent, float left, float top, float width, float height, int fontSize, Color color, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            root.transform.SetParent(parent, false);
            var text = root.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(0x09, 0x10, 0x18, 0xFF);
            SetTopLeftRect(text.rectTransform, left, top, width, height);
            return text;
        }

        /// <summary>
        /// 创建带金色边框的"按下 [按键]"提示标签。
        /// </summary>
        private GameObject CreateChip(RectTransform parent, int index)
        {
            var root = new GameObject($"KeyChip{index}", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.sprite = solidSprite;
            image.color = new Color32(0x13, 0x1E, 0x30, 0xEE);
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.78f, 0.38f, 0.38f);
            outline.effectDistance = new Vector2(1.1f, -1.1f);
            SetTopLeftRect(root.GetComponent<RectTransform>(), index * (KeyChipWidth + KeyChipGap), 0f, KeyChipWidth, KeyChipHeight);

            var press = CreateText("PressLabel", root.GetComponent<RectTransform>(), 0f, 3f, KeyChipWidth, 10f, 8, new Color32(0xFF, 0xD5, 0x7C, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());
            press.text = "PRESS";
            var text = CreateText("KeyLabel", root.GetComponent<RectTransform>(), 0f, 13f, KeyChipWidth, 18f, 15, Color.white, TextAlignmentOptions.Center, LoadButtonFont());
            text.text = string.Empty;
            return root;
        }

        /// <summary>
        /// 创建一个样式化按钮，点击时触发教程指令。
        /// </summary>
        private Button CreateButton(string name, RectTransform parent, float left, float top, float width, float height, string label, Color background, Color textColor, mlpTutorialOverlayCommand command)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            SetTopLeftRect(rect, left, top, width, height);

            var image = root.GetComponent<Image>();
            image.sprite = solidSprite;
            image.color = background;
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            outline.effectDistance = new Vector2(1f, -1f);

            var button = root.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.14f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.35f);
            button.colors = colors;
            button.targetGraphic = image;
            button.onClick.AddListener(() => pendingCommand = command);

            var text = CreateText("Label", rect, 0f, 6f, width, height - 6f, 16, textColor, TextAlignmentOptions.Center, LoadButtonFont());
            text.text = label;
            return button;
        }

        /// <summary>
        /// 设置 RectTransform 锚点，使其拉伸填满整个父级。
        /// </summary>
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 以左上角为锚点定位和设置 RectTransform 的尺寸。
        /// </summary>
        private static void SetTopLeftRect(RectTransform rect, float left, float top, float width, float height)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// 以中心点为基准定位和设置 RectTransform 的尺寸。
        /// </summary>
        private static void SetCenteredRect(RectTransform rect, float centerX, float centerY, float width, float height)
        {
            SetTopLeftRect(rect, centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }

        /// <summary>
        /// 显示或隐藏标记物的 GameObject。
        /// </summary>
        private static void SetMarkerActive(RectTransform rect, bool active)
        {
            if (rect != null)
            {
                rect.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 将女巫精灵的渲染层级提升到覆盖层画布之上。
        /// </summary>
        private void ApplyWitchSortingOrder()
        {
            if (witchArmature == null)
            {
                return;
            }

            var renderers = witchArmature.GetComponentsInChildren<SpriteRenderer>(true);
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
        /// 隐藏女巫模型上的篮球插槽，只显示角色本身。
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
        /// 加载用于大标题的 Impact 字体。
        /// </summary>
        private static TMP_FontAsset LoadTitleFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/Impact2 SDF") ?? TMP_Settings.defaultFontAsset;
        }

        /// <summary>
        /// 加载用于按钮和按键标签的 Agency Bold 字体。
        /// </summary>
        private static TMP_FontAsset LoadButtonFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/AgencyBold SDF") ?? LoadTitleFont();
        }

        /// <summary>
        /// 加载用于正文和讲解文字的 Rajdhani Bold 字体。
        /// </summary>
        private static TMP_FontAsset LoadBodyFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/RajdhaniBold SDF") ?? LoadTitleFont();
        }
    }
}
