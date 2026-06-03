using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rimrush
{
    public enum rimrushTutorialOverlayCommand
    {
        None,
        ReturnToMenu,
        ReplayTutorial,
        StartTraining,
        StartQuickMatch
    }

    public sealed class rimrushTutorialOverlay : MonoBehaviour
    {
        private const int WitchCharacterId = 6;
        private const int CanvasSortingOrder = 1400;
        private const int WitchSortingOrderBase = 1260;
        private const int ReferenceWidth = 800;
        private const int ReferenceHeight = 480;

        private static rimrushTutorialOverlay activeOverlay;
        private static Sprite solidSprite;
        private static Sprite circleSprite;
        private static Sprite ringSprite;
        private static Sprite witchIconSprite;

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

        private DBLiteArmature witchArmature;
        private GameObject witchFallbackRoot;
        private bool initialized;
        private bool visible;
        private float feedbackTimer;
        private float visibleTime;
        private float effectTime;
        private rimrushTutorialOverlayCommand pendingCommand;

        public static rimrushTutorialOverlay Active => FindOrCreate();

        private void Awake()
        {
            if (activeOverlay == null || activeOverlay == this)
            {
                activeOverlay = this;
            }

            EnsureInitialized();
            Hide();
        }

        private void OnDestroy()
        {
            if (activeOverlay == this)
            {
                activeOverlay = null;
            }
        }

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

        private void LateUpdate()
        {
            ApplyWitchSortingOrder();
        }

        public void ShowPrelude(string title, string subtitle, string narration, params string[] keys)
        {
            ShowInternal();
            SetProgress(-1, 0);
            SetCopy("WITCH GUIDE", title, subtitle, narration, keys);
            SetGoal(string.Empty);
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetTrajectory(null);
            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }
        }

        public void ShowStep(int currentIndex, int total, string title, string subtitle, string goal, string narration, params string[] keys)
        {
            ShowInternal();
            SetProgress(currentIndex, total);
            SetCopy($"STEP {currentIndex + 1}/{Mathf.Max(1, total)}", title, subtitle, narration, keys);
            SetGoal(goal);
            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }
        }

        public void UpdateCopy(string title, string subtitle, string goal, string narration, params string[] keys)
        {
            ShowInternal();
            SetCopy(stepText != null ? stepText.text : string.Empty, title, subtitle, narration, keys);
            SetGoal(goal);
        }

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

        public void ClearFocus()
        {
            SetFocusRect(0f, 0f, 0f, 0f);
        }

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

        public void SetApexRing(Vector2 center, float size, bool active)
        {
            EnsureInitialized();
            SetMarkerActive(apexRing, active && size > 0f);
            if (active && size > 0f)
            {
                SetCenteredRect(apexRing, center.x, center.y, size, size);
            }
        }

        public void SetEnergyPulse(bool active)
        {
            EnsureInitialized();
            SetMarkerActive(energyPulse, active);
            if (active)
            {
                SetCenteredRect(energyPulse, 102f, 56f, 208f, 102f);
            }
        }

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

        public void ShowFeedback(string message, Color color, float duration = 0.9f)
        {
            EnsureInitialized();
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackTimer = duration;
        }

        public void ShowOutro(string characterName, string skillName)
        {
            ShowInternal();
            SetProgress(7, 7);
            SetCopy("TUTORIAL CLEAR", "YOU HAVE THE TOOLKIT", "Pace. Timing. Defense. Super.", "Take that feel into the next mode.", null);
            SetGoal(string.Empty);
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
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
                outroBodyText.text = $"{characterName}\n{skillName}\nMove, dash, shoot, fake, steal, block, super.";
            }
        }

        public rimrushTutorialOverlayCommand ConsumeCommand()
        {
            var command = pendingCommand;
            pendingCommand = rimrushTutorialOverlayCommand.None;
            return command;
        }

        public void Hide()
        {
            visible = false;
            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(false);
            }
        }

        private static rimrushTutorialOverlay FindOrCreate()
        {
            if (activeOverlay != null)
            {
                return activeOverlay;
            }

            var existing = FindObjectOfType<rimrushTutorialOverlay>();
            if (existing != null)
            {
                activeOverlay = existing;
                return existing;
            }

            var root = new GameObject("RimrushTutorialOverlayRuntime");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(root);
            }

            activeOverlay = root.AddComponent<rimrushTutorialOverlay>();
            return activeOverlay;
        }

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

        private void ShowInternal()
        {
            EnsureInitialized();
            visible = true;
            visibleTime = 0f;
            effectTime = 0f;
            pendingCommand = rimrushTutorialOverlayCommand.None;
            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(true);
            }

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }
        }

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

            trajectoryRoot = CreateRootRect("TrajectoryRoot", overlayRoot);
            for (var i = 0; i < 7; i++)
            {
                trajectoryDots.Add(CreateImage($"TrajectoryDot{i}", trajectoryRoot, circleSprite, new Color(0.62f, 1f, 0.9f, 0.85f)));
            }

            headerRoot = CreateCard("HeaderRoot", overlayRoot, 222f, 18f, 356f, 104f, 0.92f);
            headerGlow = CreateImage("HeaderGlow", headerRoot.transform as RectTransform, circleSprite, new Color(0.33f, 1f, 0.88f, 0.12f));
            SetCenteredRect(headerGlow.rectTransform, 178f, 42f, 280f, 76f);
            stepText = CreateText("StepText", headerRoot.transform as RectTransform, 24f, 12f, 150f, 22f, 14, new Color32(0x8A, 0xFF, 0xE1, 0xFF), TextAlignmentOptions.Left, LoadBodyFont());
            titleText = CreateText("TitleText", headerRoot.transform as RectTransform, 24f, 34f, 308f, 26f, 26, Color.white, TextAlignmentOptions.Left, LoadTitleFont());
            subtitleText = CreateText("SubtitleText", headerRoot.transform as RectTransform, 24f, 62f, 308f, 20f, 13, new Color32(0xD6, 0xE5, 0xF9, 0xFF), TextAlignmentOptions.Left, LoadBodyFont());
            goalText = CreateText("GoalText", headerRoot.transform as RectTransform, 24f, 82f, 260f, 16f, 12, new Color32(0xFF, 0xD5, 0x7C, 0xFF), TextAlignmentOptions.Left, LoadButtonFont());

            var dotsRoot = CreateRootRect("ProgressDots", headerRoot.transform);
            SetTopLeftRect(dotsRoot, 248f, 80f, 94f, 18f);
            for (var i = 0; i < 7; i++)
            {
                var dot = CreateImage($"ProgressDot{i}", dotsRoot, circleSprite, new Color32(0x35, 0x4C, 0x70, 0xE8));
                SetCenteredRect(dot.rectTransform, 12f + i * 13f, 9f, 8f, 8f);
                progressDots.Add(dot);
            }

            var keysRoot = CreateRootRect("KeysRoot", overlayRoot);
            SetTopLeftRect(keysRoot, 222f, 126f, 356f, 34f);
            for (var i = 0; i < 5; i++)
            {
                var chip = CreateChip(keysRoot, i);
                keyChipRoots.Add(chip);
                keyChipLabels.Add(chip.GetComponentInChildren<TextMeshProUGUI>());
            }

            feedbackText = CreateText("FeedbackText", overlayRoot, 204f, 170f, 392f, 26f, 18, new Color32(0x9A, 0xFF, 0xDD, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());

            narratorRoot = CreateCard("NarratorRoot", overlayRoot, 580f, 18f, 198f, 104f, 0.88f);
            narratorOrb = CreateImage("NarratorOrb", narratorRoot.transform as RectTransform, circleSprite, new Color(0.33f, 1f, 0.88f, 0.14f));
            SetCenteredRect(narratorOrb.rectTransform, 48f, 52f, 72f, 72f);
            var narratorLabel = CreateText("NarratorLabel", narratorRoot.transform as RectTransform, 84f, 16f, 94f, 16f, 12, new Color32(0xFF, 0xD6, 0x80, 0xFF), TextAlignmentOptions.Left, LoadButtonFont());
            narratorLabel.text = "WITCH";
            narratorText = CreateText("NarratorText", narratorRoot.transform as RectTransform, 84f, 34f, 102f, 48f, 13, Color.white, TextAlignmentOptions.TopLeft, LoadBodyFont());
            narratorText.enableWordWrapping = true;
            narratorText.fontStyle = FontStyles.Bold;

            var witchMount = CreateRootRect("WitchMount", narratorRoot.transform);
            SetTopLeftRect(witchMount, 10f, 12f, 72f, 76f);

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

            quickMatchButton = CreateButton("QuickMatchButton", outroRoot.transform as RectTransform, 124f, 146f, 212f, 38f, "MAIN MENU", new Color32(0xE8, 0xA4, 0x36, 0xFF), new Color32(0x19, 0x14, 0x0F, 0xFF), rimrushTutorialOverlayCommand.ReturnToMenu);
            trainingButton = CreateButton("TrainingButton", outroRoot.transform as RectTransform, 124f, 198f, 212f, 38f, "FREE TRAINING", new Color32(0x26, 0x34, 0x4F, 0xFF), Color.white, rimrushTutorialOverlayCommand.StartTraining);
            replayButton = CreateButton("ReplayButton", outroRoot.transform as RectTransform, 124f, 250f, 212f, 38f, "REPLAY TUTORIAL", new Color32(0x26, 0x34, 0x4F, 0xFF), Color.white, rimrushTutorialOverlayCommand.ReplayTutorial);

            BuildWitchPreview(witchMount);
            HideRuntimeMarkers();
        }

        private void BuildWitchPreview(RectTransform witchMount)
        {
            if (witchMount == null || witchArmature != null || witchFallbackRoot != null)
            {
                return;
            }

            var builtArmature = rimrushPlayersData.BuildGameplayArmature("TutorialNarratorWitch");
            if (builtArmature == null)
            {
                BuildWitchIconFallback(witchMount);
                return;
            }

            try
            {
                witchArmature = builtArmature;
                witchArmature.transform.SetParent(witchMount, false);
                witchArmature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                    witchMount,
                    new Vector3(0f, -18f, 0f));
                witchArmature.transform.localScale = new Vector3(
                    rimrushConstants.PixelPerfectCharacterScale * 0.88f,
                    rimrushConstants.PixelPerfectCharacterScale * 0.88f,
                    1f);
                rimrushPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
                witchArmature.StopAtStart("idle");
                HidePreviewBall(witchArmature);
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
                BuildWitchIconFallback(witchMount);
            }
        }

        private void BuildWitchIconFallback(RectTransform witchMount)
        {
            var sprite = LoadWitchIconSprite();
            if (sprite == null)
            {
                return;
            }

            var image = CreateImage("WitchFallbackIcon", witchMount, sprite, Color.white);
            image.preserveAspect = true;
            witchFallbackRoot = image.gameObject;
            SetTopLeftRect(image.rectTransform, 4f, 4f, 62f, 62f);
        }

        private void AnimatePulse()
        {
            var wave = 0.5f + 0.5f * Mathf.Sin(effectTime * 4.6f);
            if (narratorOrb != null)
            {
                narratorOrb.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.06f);
                narratorOrb.color = new Color(0.33f, 1f, 0.88f, 0.1f + wave * 0.06f);
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

        private void HideRuntimeMarkers()
        {
            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetTrajectory(null);
        }

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
            }

            if (narratorText != null)
            {
                narratorText.text = narration ?? string.Empty;
            }

            SetKeys(keys);
        }

        private void SetGoal(string goal)
        {
            if (goalText == null)
            {
                return;
            }

            goalText.text = goal ?? string.Empty;
            goalText.gameObject.SetActive(!string.IsNullOrEmpty(goal));
        }

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

        private static Sprite LoadWitchIconSprite()
        {
            if (witchIconSprite != null)
            {
                return witchIconSprite;
            }

            var resourcePath = rimrushAssets.Images.ResourcePath(rimrushAssets.Images.SkillIcons.Witch);
            witchIconSprite = Resources.Load<Sprite>(resourcePath);
            if (witchIconSprite != null)
            {
                return witchIconSprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            witchIconSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            witchIconSprite.name = "TutorialWitchFallbackIcon";
            return witchIconSprite;
        }

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

        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            texture.name = name;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

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

        private RectTransform CreateMask(string name)
        {
            var image = CreateImage(name, overlayRoot, solidSprite, new Color(0.02f, 0.04f, 0.08f, 0.52f));
            return image.rectTransform;
        }

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
            SetTopLeftRect(root.GetComponent<RectTransform>(), 0f + index * 72f, 0f, 62f, 28f);

            var text = CreateText("Label", root.GetComponent<RectTransform>(), 0f, 2f, 62f, 24f, 14, Color.white, TextAlignmentOptions.Center, LoadButtonFont());
            text.text = string.Empty;
            return root;
        }

        private Button CreateButton(string name, RectTransform parent, float left, float top, float width, float height, string label, Color background, Color textColor, rimrushTutorialOverlayCommand command)
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

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

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

        private static void SetCenteredRect(RectTransform rect, float centerX, float centerY, float width, float height)
        {
            SetTopLeftRect(rect, centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }

        private static void SetMarkerActive(RectTransform rect, bool active)
        {
            if (rect != null)
            {
                rect.gameObject.SetActive(active);
            }
        }

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

        private static void HidePreviewBall(DBLiteArmature armature)
        {
            if (armature == null)
            {
                return;
            }

            armature.SetSlotHidden("ball", true);
            armature.SetSlotHidden("ball_front", true);
        }

        private static TMP_FontAsset LoadTitleFont()
        {
            return Resources.Load<TMP_FontAsset>("rimrush/Fonts/TMP/Impact2 SDF") ?? TMP_Settings.defaultFontAsset;
        }

        private static TMP_FontAsset LoadButtonFont()
        {
            return Resources.Load<TMP_FontAsset>("rimrush/Fonts/TMP/AgencyBold SDF") ?? LoadTitleFont();
        }

        private static TMP_FontAsset LoadBodyFont()
        {
            return Resources.Load<TMP_FontAsset>("rimrush/Fonts/TMP/RajdhaniBold SDF") ?? LoadTitleFont();
        }
    }
}
