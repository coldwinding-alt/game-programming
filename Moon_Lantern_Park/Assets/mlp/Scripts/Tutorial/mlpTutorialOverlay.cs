// 教程界面覆盖层
// 在教程模式下覆盖在游戏画面上方，显示操作提示、进度点、女巫角色讲解、技能演示动画和按键提示。教程完成后的结算界面也在这里管理。

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mlp
{
    public enum mlpTutorialOverlayCommand
    {
        None,
        SkipStep,
        ReturnToMenu,
        ReplayTutorial,
        StartTraining,
        StartQuickMatch
    }

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
        /// Register as the active overlay and build UI on first use.
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
        /// Clear the singleton reference when this overlay is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (activeOverlay == this)
            {
                activeOverlay = null;
            }
        }

        /// <summary>
        /// Tick pulse animations and fade out timed feedback messages.
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
        /// Keep the witch sprite drawn above the overlay each frame.
        /// </summary>
        private void LateUpdate()
        {
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// Show the opening screen before the first drill starts.
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
        /// Show a numbered drill step with progress dots and key prompts.
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
        /// Refresh the header text and key chips without changing the layout.
        /// </summary>
        public void UpdateCopy(string title, string subtitle, string goal, string narration, params string[] keys)
        {
            ShowInternal();
            var currentNarration = narratorText != null ? narratorText.text : string.Empty;
            SetCopy(stepText != null ? stepText.text : string.Empty, title, string.Empty, currentNarration, keys);
            SetGoal(string.Empty);
        }

        /// <summary>
        /// Dim everything outside the given rectangle to spotlight a UI area.
        /// </summary>
        public void SetFocusRect(float left, float top, float width, float height)
        {
            EnsureInitialized();
            var hasFocus = width > 0f && height > 0f;
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

            var clampedLeft = Mathf.Clamp(left, 0f, ReferenceWidth);
            var clampedTop = Mathf.Clamp(top, 0f, ReferenceHeight);
            var clampedWidth = Mathf.Clamp(width, 0f, ReferenceWidth - clampedLeft);
            var clampedHeight = Mathf.Clamp(height, 0f, ReferenceHeight - clampedTop);
            var right = clampedLeft + clampedWidth;
            var bottom = clampedTop + clampedHeight;

            SetTopLeftRect(maskTop, 0f, 0f, ReferenceWidth, clampedTop);
            SetTopLeftRect(maskBottom, 0f, bottom, ReferenceWidth, Mathf.Max(0f, ReferenceHeight - bottom));
            SetTopLeftRect(maskLeft, 0f, clampedTop, clampedLeft, clampedHeight);
            SetTopLeftRect(maskRight, right, clampedTop, Mathf.Max(0f, ReferenceWidth - right), clampedHeight);
            SetTopLeftRect(focusFrame, clampedLeft, clampedTop, clampedWidth, clampedHeight);
            SetTopLeftRect(focusGlow, clampedLeft - 16f, clampedTop - 16f, clampedWidth + 32f, clampedHeight + 32f);
        }

        /// <summary>
        /// Remove the focus spotlight so the full screen is visible again.
        /// </summary>
        public void ClearFocus()
        {
            SetFocusRect(0f, 0f, 0f, 0f);
        }

        /// <summary>
        /// Highlight a target zone the player should aim at.
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
        /// Show or hide the pulsing ring that marks the apex of a shot arc.
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
        /// Show or hide the energy pulse glow near the shooting lane.
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
        /// Draw a trail of dots showing the ball's predicted path.
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
        /// Show or hide the 2-point / 3-point scoring zone overlay.
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
        /// Flash a colored feedback message that fades after a short delay.
        /// </summary>
        public void ShowFeedback(string message, Color color, float duration = 1.35f)
        {
            EnsureInitialized();
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackTimer = duration;
        }

        /// <summary>
        /// Show the outro screen with replay, training, and menu choices.
        /// </summary>
        public void ShowOutro(string characterName, string skillName)
        {
            ShowInternal();
            SetProgress(9, 10);
            SetCopy("CLEAR", "READY TO PLAY", string.Empty, "Nice work. Choose your next run.", null);
            SetGoal(string.Empty);
            SetSkipVisible(false);
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetScoringGuide(0, false);
            SetTrajectory(null);
            if (outroRoot != null)
            {
                outroRoot.SetActive(true);
            }

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
        /// Return the pending button command and reset it to None.
        /// </summary>
        public mlpTutorialOverlayCommand ConsumeCommand()
        {
            var command = pendingCommand;
            pendingCommand = mlpTutorialOverlayCommand.None;
            return command;
        }

        /// <summary>
        /// Hide the entire tutorial overlay.
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
        /// Find the existing overlay in the scene or create a new runtime instance.
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
        /// Build the UI once, then just fix up the camera on later calls.
        /// </summary>
        private void EnsureInitialized()
        {
            if (initialized)
            {
                EnsureCanvasCamera();
                return;
            }

            initialized = true;
            EnsureSprites();
            EnsureEventSystem();
            BuildUi();
            EnsureCanvasCamera();
        }

        /// <summary>
        /// Activate the overlay root and reset timers and feedback text.
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
        /// Construct the entire tutorial overlay from code (no prefab).
        /// </summary>
        private void BuildUi()
        {
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = CanvasSortingOrder;
            gameObject.AddComponent<GraphicRaycaster>();
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            overlayRoot = CreateRootRect("OverlayRoot", transform);
            overlayRoot.gameObject.SetActive(false);

            var blurVeil = CreateImage("BlurVeil", overlayRoot, solidSprite, new Color(0.03f, 0.06f, 0.09f, 0.14f));
            StretchToParent(blurVeil.rectTransform);

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

            BuildScoringGuide();

            trajectoryRoot = CreateRootRect("TrajectoryRoot", overlayRoot);
            for (var i = 0; i < 7; i++)
            {
                trajectoryDots.Add(CreateImage($"TrajectoryDot{i}", trajectoryRoot, circleSprite, new Color(0.62f, 1f, 0.9f, 0.85f)));
            }

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

            var keysRoot = CreateRootRect("KeysRoot", overlayRoot);
            SetTopLeftRect(keysRoot, GuideLeft, GuideTop + GuideHeight + 8f, GuideWidth, KeyChipHeight);
            for (var i = 0; i < 5; i++)
            {
                var chip = CreateChip(keysRoot, i);
                keyChipRoots.Add(chip);
                var keyLabel = chip.transform.Find("KeyLabel") as RectTransform;
                keyChipLabels.Add(keyLabel != null ? keyLabel.GetComponent<TextMeshProUGUI>() : null);
            }

            feedbackText = CreateText("FeedbackText", overlayRoot, 154f, 176f, 492f, 26f, 18, new Color32(0x9A, 0xFF, 0xDD, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());
            skipButton = CreateButton("SkipStepButton", overlayRoot, 684f, 426f, 92f, 34f, "SKIP", new Color32(0x13, 0x1E, 0x30, 0xDD), new Color32(0xFF, 0xD5, 0x7C, 0xFF), mlpTutorialOverlayCommand.SkipStep);

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

            BuildWitchPreview(witchMount);
            SetSkipVisible(false);
            HideRuntimeMarkers();
        }

        /// <summary>
        /// Spawn a live witch character model inside the narrator panel.
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
        /// Use a static portrait sprite when the live model is unavailable.
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
        /// Update pulsing glow effects on the witch orb, header, and focus areas.
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
        /// Hide all focus, target, apex, energy, scoring, and trajectory markers.
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
        /// Update all header and narrator text labels, plus key chip display.
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
        /// Show or hide the goal line below the title.
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
        /// Show or hide the skip button.
        /// </summary>
        private void SetSkipVisible(bool active)
        {
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Light up progress dots to show which step the player is on.
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
        /// Show or hide each key prompt chip and set its label text.
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
        /// Build the 2-point / 3-point scoring zone labels and line marker.
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
        /// Color and label a single scoring zone as 2-point or 3-point.
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
        /// Assign the main camera to the canvas if one is not already set.
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
        /// Create an EventSystem if none exists in the scene.
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
        /// Generate the solid, circle, and ring sprites used by the overlay.
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
        /// Load and cache the witch character portrait sprite.
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
        /// Generate a small flat-color texture.
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
        /// Generate a soft-edged circle texture for glows and dots.
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
        /// Generate a ring texture with a hollow center for frame outlines.
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
        /// Wrap a texture into a named Sprite with center pivot.
        /// </summary>
        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            texture.name = name;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>
        /// Create a RectTransform that stretches to fill its parent.
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
        /// Create a semi-transparent dark panel used to dim areas outside the focus rect.
        /// </summary>
        private RectTransform CreateMask(string name)
        {
            var image = CreateImage(name, overlayRoot, solidSprite, new Color(0.02f, 0.04f, 0.08f, 0.52f));
            return image.rectTransform;
        }

        /// <summary>
        /// Create a dark card panel with a gold outline and drop shadow.
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
        /// Create a non-interactive Image element with the given sprite and color.
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
        /// Create a TextMeshPro label with outline at a fixed screen position.
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
        /// Create a "PRESS [key]" chip with a gold border for key prompts.
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
        /// Create a styled button that fires a tutorial command when clicked.
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
        /// Anchor a RectTransform so it stretches to fill its entire parent.
        /// </summary>
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Position and size a RectTransform anchored to the top-left corner.
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
        /// Position and size a RectTransform by its center point.
        /// </summary>
        private static void SetCenteredRect(RectTransform rect, float centerX, float centerY, float width, float height)
        {
            SetTopLeftRect(rect, centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }

        /// <summary>
        /// Show or hide a marker's GameObject.
        /// </summary>
        private static void SetMarkerActive(RectTransform rect, bool active)
        {
            if (rect != null)
            {
                rect.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Bump the witch sprite renderers above the overlay canvas.
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
        /// Hide the ball slots on the witch model so only the character shows.
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
        /// Load the Impact font used for large headings.
        /// </summary>
        private static TMP_FontAsset LoadTitleFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/Impact2 SDF") ?? TMP_Settings.defaultFontAsset;
        }

        /// <summary>
        /// Load the Agency Bold font used for buttons and key labels.
        /// </summary>
        private static TMP_FontAsset LoadButtonFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/AgencyBold SDF") ?? LoadTitleFont();
        }

        /// <summary>
        /// Load the Rajdhani Bold font used for body and narration text.
        /// </summary>
        private static TMP_FontAsset LoadBodyFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/RajdhaniBold SDF") ?? LoadTitleFont();
        }
    }
}
