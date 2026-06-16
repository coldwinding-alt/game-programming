// Tutorial interface overlay prompt text/arrow/UI overlay
// In tutorial mode, it is overlaid on the top of the game screen to display operation tips, progress points, witch character explanations, skill demonstration animations and key press prompts. The settlement interface after the completion of the tutorial is also managed here.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace mlp
{
    /// <summary>Tutorial overlay commands: none, next step, display skill demonstration, display completion screen and other control commands. </summary>
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
    /// Tutorial interface overlay: Displays operation prompts, progress points, witch explanations, skill demonstrations and key prompts at the top of the game screen. After the tutorial is completed, the settlement interface is displayed.

    /// </summary>
    public sealed class mlpTutorialOverlay : MonoBehaviour
    {
        private const int WitchCharacterId = 6; // Witch character ID

        private const int CanvasSortingOrder = 1400; // Canvas rendering sorting hierarchy

        private const int WitchSortingOrderBase = 1425; // Witch Elf Sorting Hierarchy Baseline

        private const int ReferenceWidth = 800; // Reference resolution width

        private const int ReferenceHeight = 480; // Reference resolution height

        private const float GuideLeft = 122f; // Bootstrap card left border

        private const float GuideTop = 14f; // Bootstrap card upper border

        private const float GuideWidth = 476f; // Boot card width

        private const float GuideHeight = 100f; // Boot card height

        private const float KeyChipWidth = 76f; // Button label width

        private const float KeyChipHeight = 32f; // Button label height
        private const float KeyChipGap = 8f; // Button label spacing

        private const int ProgressDotCount = 10; // Number of progress points


        private static mlpTutorialOverlay activeOverlay; // The currently active overlay singleton

        private static Sprite solidSprite; // Solid color sprite cache

        private static Sprite circleSprite; // Circle sprite cache

        private static Sprite ringSprite; // Ring sprite cache

        private static Sprite witchPortraitSprite; // Witch Avatar Elf Cache


        private Canvas canvas; // canvas

        private RectTransform overlayRoot; // Overlay root node

        private RectTransform maskTop; // Top Vignetting Mask

        private RectTransform maskBottom; // Bottom Vignette Mask

        private RectTransform maskLeft; // Left vignetting mask

        private RectTransform maskRight; // Right vignetting mask

        private RectTransform focusFrame; // Focus on golden border

        private RectTransform focusGlow; // Focused peripheral glow

        private RectTransform targetZone; // Target area filling

        private RectTransform targetGlow; // Target area glows

        private RectTransform apexRing; // Pulse ring at the highest point of shooting

        private RectTransform energyPulse; // energy pulse light effect

        private RectTransform trajectoryRoot; // trajectory dot root node

        private GameObject scoringGuideRoot; // Score bootstrap root node

        private Image scoringLeftFill; // Fill left of scoring area

        private Image scoringRightFill; // Fill the right side of the scoring area

        private TextMeshProUGUI scoringLeftLabel; // Label on the left side of the scoring area
        private TextMeshProUGUI scoringRightLabel; // Label on the right side of the scoring area

        private TextMeshProUGUI scoringLineLabel; // score line label

        private readonly List<Image> trajectoryDots = new List<Image>(); // Track dot list

        private readonly List<Image> progressDots = new List<Image>(); // progress dot list

        private readonly List<GameObject> keyChipRoots = new List<GameObject>(); // Key label root node list

        private readonly List<TextMeshProUGUI> keyChipLabels = new List<TextMeshProUGUI>(); // Button label text list


        private GameObject headerRoot; // title bar root node

        private GameObject narratorRoot; // Explainer panel root node

        private GameObject outroRoot; // End screen root node

        private TextMeshProUGUI stepText; // step number text

        private TextMeshProUGUI titleText; // title text

        private TextMeshProUGUI subtitleText; // subtitle text

        private TextMeshProUGUI goalText; // Goal description text

        private TextMeshProUGUI narratorText; // explain text

        private TextMeshProUGUI feedbackText; // Feedback prompt text

        private TextMeshProUGUI outroTitleText; // End screen title text

        private TextMeshProUGUI outroBodyText; // End screen text

        private Image narratorOrb; // Commentator light ball

        private Image headerGlow; // title bar glow

        private Image targetFill; // Target area fill map

        private Image targetGlowImage; // target glow picture

        private Image energyPulseImage; // Energy Pulse Picture

        private Image apexRingImage; // Highest point ring picture

        private Button replayButton; // replay button
        private Button trainingButton; // training button

        private Button quickMatchButton; // Quick Match Button (Main Menu)

        private Button skipButton; // skip button


        private DBLiteArmature witchArmature; // Witch skeleton animation example

        private GameObject witchFallbackRoot; // Witch static avatar root node

        private RectTransform witchFallbackRect; // Witch static avatar rectangle

        private Vector3 witchBaseLocalPosition; // Witch animation base position

        private Vector3 witchBaseLocalScale = Vector3.one; // Witch animation basic scaling

        private Vector2 witchFallbackBasePosition; // Basic position of witch static avatar

        private bool initialized; // Has it been initialized?

        private bool visible; // visible or not

        private float feedbackTimer; // Feedback text remaining time

        private float visibleTime; // Visible cumulative time

        private float effectTime; // Special effects cumulative time

        private mlpTutorialOverlayCommand pendingCommand; // Pending button commands


        public static mlpTutorialOverlay Active => FindOrCreate();

        /// <summary>
        /// Sign up as an active tutorial overlay to build the UI on first use.

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
        /// Clear the singleton reference when the overlay is destroyed.

        /// </summary>
        private void OnDestroy()
        {
            if (activeOverlay == this)
            {
                activeOverlay = null;
            }
        }

        /// <summary>
        /// Drive the pulse animation and fade out the timed feedback message.

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
        /// Make sure the witch sprite is rendered on top of the overlay every frame.

        /// </summary>
        private void LateUpdate()
        {
            ApplyWitchSortingOrder();
        }

        /// <summary>
        /// Show the opening screen before the first exercise begins.

        /// </summary>
        public void ShowPrelude(string title, string subtitle, string narration, params string[] keys)
        {
            // 1. Activate overlay and reset timer

            ShowInternal();
            // 2. Hide progress points (the opening screen does not need to show progress)

            SetProgress(-1, 0);
            // 3. Set the opening title, explanation text and key prompts

            SetCopy("TUTORIAL", title, string.Empty, narration, keys);
            SetGoal(string.Empty);
            // 4. Show skip button
            SetSkipVisible(true);
            // 5. Clear all runtime visual effects (focus frame, target area, trajectory, etc.)

            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetScoringGuide(0, false);
            SetTrajectory(null);
            // 6. Hide the end screen

            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Displays numbered exercise steps with progress points and key prompts.

        /// </summary>
        public void ShowStep(int currentIndex, int total, string title, string subtitle, string goal, string narration, params string[] keys)
        {
            // 1. Activate overlay and reset timer

            ShowInternal();
            // 2. Light up the corresponding progress point (such as step 3 / 10 steps in total)

            SetProgress(currentIndex, total);
            // 3. Set step number, title, explanation text and key prompts

            SetCopy($"{currentIndex + 1}/{Mathf.Max(1, total)}", title, string.Empty, narration, keys);
            SetGoal(string.Empty);
            // 4. Show the skip button and hide the end screen

            SetSkipVisible(true);
            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Refresh the title text and key prompts without changing the layout structure.

        /// </summary>
        public void UpdateCopy(string title, string subtitle, string goal, string narration, params string[] keys)
        {
            // 1. Make sure the overlay is activated

            ShowInternal();
            // 2. Keep the current step number and explanation text, and only update the title and key prompts

            var currentNarration = narratorText != null ? narratorText.text : string.Empty;
            SetCopy(stepText != null ? stepText.text : string.Empty, title, string.Empty, currentNarration, keys);
            SetGoal(string.Empty);
        }

        /// <summary>
        /// Darkens everything outside the specified rectangular area to highlight the focused UI area.

        /// </summary>
        public void SetFocusRect(float left, float top, float width, float height)
        {
            // 1. Make sure the UI has been built

            EnsureInitialized();
            // 2. Determine whether focus is required (valid only when width and height are greater than 0)

            var hasFocus = width > 0f && height > 0f;
            // 3. Show or hide the four masks and focus frame according to whether focus is needed.

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

            // 4. Limit the focus area to the 800x480 frame

            var clampedLeft = Mathf.Clamp(left, 0f, ReferenceWidth);
            var clampedTop = Mathf.Clamp(top, 0f, ReferenceHeight);
            var clampedWidth = Mathf.Clamp(width, 0f, ReferenceWidth - clampedLeft);
            var clampedHeight = Mathf.Clamp(height, 0f, ReferenceHeight - clampedTop);
            var right = clampedLeft + clampedWidth;
            var bottom = clampedTop + clampedHeight;

            // 5. Calculate and set the positions of the four masks so that they form a "vignetting" effect around the focus area.

            SetTopLeftRect(maskTop, 0f, 0f, ReferenceWidth, clampedTop);
            SetTopLeftRect(maskBottom, 0f, bottom, ReferenceWidth, Mathf.Max(0f, ReferenceHeight - bottom));
            SetTopLeftRect(maskLeft, 0f, clampedTop, clampedLeft, clampedHeight);
            SetTopLeftRect(maskRight, right, clampedTop, Mathf.Max(0f, ReferenceWidth - right), clampedHeight);
            // 6. Set the position and size of the golden focus border and peripheral glow effect

            SetTopLeftRect(focusFrame, clampedLeft, clampedTop, clampedWidth, clampedHeight);
            SetTopLeftRect(focusGlow, clampedLeft - 16f, clampedTop - 16f, clampedWidth + 32f, clampedHeight + 32f);
        }

        /// <summary>
        /// Remove focus highlighting and restore full screen visibility.

        /// </summary>
        public void ClearFocus()
        {
            SetFocusRect(0f, 0f, 0f, 0f);
        }

        /// <summary>
        /// Highlight the target area that the player needs to aim at.

        /// </summary>
        public void SetTargetRect(float left, float top, float width, float height)
        {
            // 1. Make sure the UI has been built

            EnsureInitialized();
            // 2. Determine whether there is a valid target area (only displayed if the width and height are greater than 0)

            var hasTarget = width > 0f && height > 0f;
            SetMarkerActive(targetZone, hasTarget);
            SetMarkerActive(targetGlow, hasTarget);
            if (!hasTarget)
            {
                return;
            }

            // 3. Limit the target area to the 800x480 frame

            var clampedLeft = Mathf.Clamp(left, 0f, ReferenceWidth);
            var clampedTop = Mathf.Clamp(top, 0f, ReferenceHeight);
            var clampedWidth = Mathf.Clamp(width, 0f, ReferenceWidth - clampedLeft);
            var clampedHeight = Mathf.Clamp(height, 0f, ReferenceHeight - clampedTop);
            // 4. Set the position and size of the target area fill and peripheral glow

            SetTopLeftRect(targetZone, clampedLeft, clampedTop, clampedWidth, clampedHeight);
            SetCenteredRect(
                targetGlow,
                clampedLeft + clampedWidth * 0.5f,
                clampedTop + clampedHeight * 0.5f,
                clampedWidth + 44f,
                clampedHeight + 44f);
        }

        /// <summary>
        /// Shows or hides the impulse ring that marks the highest point of the shot's arc.

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
        /// Shows or hides the energy pulse light effect near the shooting lane.

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
        /// Draw a series of dots showing the predicted trajectory of the basketball.
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
        /// Show or hide the 2-point/3-point scoring area overlay.

        /// </summary>
        public void SetScoringGuide(int scoringSide, bool active)
        {
            // 1. Make sure the UI is built and check if the score boot root node exists

            EnsureInitialized();
            if (scoringGuideRoot == null)
            {
                return;
            }

            // 2. Show or hide the scoring area guide

            scoringGuideRoot.SetActive(active);
            if (!active)
            {
                return;
            }

            // 3. Depending on which side is the three partitions, color the areas on the left and right sides respectively.

            var leftIsThree = scoringSide == -1;
            SetScoringZone(scoringLeftFill, scoringLeftLabel, leftIsThree);
            SetScoringZone(scoringRightFill, scoringRightLabel, !leftIsThree);
            // 4. Set the middle dividing line label

            if (scoringLineLabel != null)
            {
                scoringLineLabel.text = "2PT / 3PT LINE";
            }
        }

        /// <summary>
        /// Displays a colored feedback message that automatically fades out after a delay.

        /// </summary>
        public void ShowFeedback(string message, Color color, float duration = 1.35f)
        {
            EnsureInitialized();
            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackTimer = duration;
        }

        /// <summary>
        /// Displays the end screen with options to replay, train, and return to the menu.

        /// </summary>
        public void ShowOutro(string characterName, string skillName)
        {
            // 1. Activate overlay and reset timer

            ShowInternal();
            // 2. Display the progress point of step 9/10 (all completed)

            SetProgress(9, 10);
            // 3. Set the title and explanation text of the end screen

            SetCopy("CLEAR", "READY TO PLAY", string.Empty, "Nice work. Choose your next run.", null);
            SetGoal(string.Empty);
            SetSkipVisible(false);
            // 4. Clear all runtime visual elements (focus boxes, score guides, track points, etc.)

            ClearFocus();
            SetTargetRect(0f, 0f, 0f, 0f);
            SetApexRing(Vector2.zero, 0f, false);
            SetEnergyPulse(false);
            SetScoringGuide(0, false);
            SetTrajectory(null);
            // 5. Show end screen card

            if (outroRoot != null)
            {
                outroRoot.SetActive(true);
            }

            // 6. Set the end screen title and character information

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
        /// Returns the pending button command and resets to None.

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
        /// Looks for an existing overlay instance in the scene and creates a new one at runtime if it does not exist.

        /// </summary>
        private static mlpTutorialOverlay FindOrCreate()
        {
            // 1. If there is already a cached active overlay, return directly

            if (activeOverlay != null)
            {
                return activeOverlay;
            }

            // 2. Try to find an existing tutorial overlay in the scene

            var existing = FindObjectOfType<mlpTutorialOverlay>();
            if (existing != null)
            {
                activeOverlay = existing;
                return existing;
            }

            // 3. There is no overlay in the scene: create a new runtime instance

            var root = new GameObject("MlpTutorialOverlayRuntime");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(root);
            }

            // 4. Add components and cache them as active overlays

            activeOverlay = root.AddComponent<mlpTutorialOverlay>();
            return activeOverlay;
        }

        /// <summary>
        /// The UI is built on the first call, subsequent calls only correct the camera settings.

        /// </summary>
        private void EnsureInitialized()
        {
            // 1. If it has already been initialized, just make sure the canvas is bound to the correct camera.

            if (initialized)
            {
                EnsureCanvasCamera();
                return;
            }

            // 2. Mark as initialized to prevent repeated construction

            initialized = true;
            // 3. Generate solid color, circular and ring texture sprites (basic materials for UI drawing)

            EnsureSprites();
            // 4. Make sure there is an event system in the scene (it won’t work without button clicks)
            EnsureEventSystem();
            // 5. Build the entire tutorial overlay UI in pure code (no reliance on prefabs)

            BuildUi();
            // 6. Make sure the canvas is bound to the correct camera

            EnsureCanvasCamera();
        }

        /// <summary>
        /// Activates the overlay root node, resetting the timer and feedback text.

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
        /// Build the entire tutorial overlay from pure code (no reliance on prefabs).

        /// </summary>
        private void BuildUi()
        {
            // 1. Create a canvas - a container for all UI elements, using camera rendering mode

            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = CanvasSortingOrder;
            gameObject.AddComponent<GraphicRaycaster>();
            // 2. Set the canvas scaler - let the UI automatically scale in different screen sizes, reference resolution 800x480

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 3. Create the overlay root node (the outermost container of all tutorial UIs), which is hidden by default

            overlayRoot = CreateRootRect("OverlayRoot", transform);
            overlayRoot.gameObject.SetActive(false);

            var blurVeil = CreateImage("BlurVeil", overlayRoot, solidSprite, new Color(0.03f, 0.06f, 0.09f, 0.14f));
            StretchToParent(blurVeil.rectTransform);

            // 4. Create four semi-transparent masks (top, bottom, left and right) to darken content outside the focus area

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

            // 5. Build 2-point/3-point scoring area guidance overlays

            BuildScoringGuide();

            // 6. Create 7 dots to display the basketball prediction trajectory

            trajectoryRoot = CreateRootRect("TrajectoryRoot", overlayRoot);
            for (var i = 0; i < 7; i++)
            {
                trajectoryDots.Add(CreateImage($"TrajectoryDot{i}", trajectoryRoot, circleSprite, new Color(0.62f, 1f, 0.9f, 0.85f)));
            }

            // 7. Build a guide card (dark panel + luminous title + step number + title text + subtitle + target text + progress point)

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

            // 8. Build a key prompt area (up to 5 small labels in the "Press [A]" style)

            var keysRoot = CreateRootRect("KeysRoot", overlayRoot);
            SetTopLeftRect(keysRoot, GuideLeft, GuideTop + GuideHeight + 8f, GuideWidth, KeyChipHeight);
            for (var i = 0; i < 5; i++)
            {
                var chip = CreateChip(keysRoot, i);
                keyChipRoots.Add(chip);
                var keyLabel = chip.transform.Find("KeyLabel") as RectTransform;
                keyChipLabels.Add(keyLabel != null ? keyLabel.GetComponent<TextMeshProUGUI>() : null);
            }

            // 9. Create feedback text (display prompts during practice, such as "Good dash") and skip buttons

            feedbackText = CreateText("FeedbackText", overlayRoot, 154f, 176f, 492f, 26f, 18, new Color32(0x9A, 0xFF, 0xDD, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());
            skipButton = CreateButton("SkipStepButton", overlayRoot, 684f, 426f, 92f, 34f, "SKIP", new Color32(0x13, 0x1E, 0x30, 0xDD), new Color32(0xFF, 0xD5, 0x7C, 0xFF), mlpTutorialOverlayCommand.SkipStep);

            // 10. Build the commentator panel (witch character avatar + "WITCH:" label + explanation text)

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

            // 11. Construct the end screen card ("WHERE NEXT?" title + three buttons of replay/training/menu)

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

            // 12. Generate a real-time model of the witch character in the commentator panel (for playing animation demonstrations)

            BuildWitchPreview(witchMount);
            // 13. The initial state hides the skip button and all runtime markers

            SetSkipVisible(false);
            HideRuntimeMarkers();
        }

        /// <summary>
        /// Generate a real-time witch character model in the narrator panel.

        /// </summary>
        private void BuildWitchPreview(RectTransform witchMount)
        {
            // 1. If the mount point does not exist or the model has been created, return directly

            if (witchMount == null || witchArmature != null || witchFallbackRoot != null)
            {
                return;
            }

            // 2. Try to create a skeletal animation real-time model of the witch character

            var builtArmature = mlpPlayersData.BuildGameplayArmature("TutorialNarratorWitch");
            if (builtArmature == null)
            {
                // 3. The real-time model creation failed and fell back to the static avatar sprite.

                BuildWitchPortraitFallback(witchMount);
                return;
            }

            try
            {
                // 4. Mount the model to the commentator panel and set the appropriate position and scale.

                witchArmature = builtArmature;
                witchArmature.transform.SetParent(witchMount, false);
                witchArmature.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                    witchMount,
                    new Vector3(0f, -14f, 0f));
                witchArmature.transform.localScale = new Vector3(
                    mlpConstants.PixelPerfectCharacterScale * 0.74f,
                    mlpConstants.PixelPerfectCharacterScale * 0.74f,
                    1f);
                // 5. Apply the appearance of the witch character (color, clothing), play the standby animation, and hide the basketball

                mlpPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
                witchArmature.Play("idle");
                HidePreviewBall(witchArmature);
                // 6. Record base position and scale (for subsequent pulse animation)
                witchBaseLocalPosition = witchArmature.transform.localPosition;
                witchBaseLocalScale = witchArmature.transform.localScale;
            }
            catch (System.Exception)
            {
                // 7. Clean up and fall back to static avatar when errors occur

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
        /// When live models are unavailable, static avatar sprites are used instead.

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
        /// Updated pulsing light effects for the Witch's orb, title bar, and focus area.

        /// </summary>
        private void AnimatePulse()
        {
            // 1. Calculate two sine waves: wave for pulse scaling/transparency, floatWave for floating up and down

            var wave = 0.5f + 0.5f * Mathf.Sin(effectTime * 4.6f);
            var floatWave = Mathf.Sin(effectTime * 3.1f);
            // 2. The commentator’s photosphere performs pulse scaling and transparency changes

            if (narratorOrb != null)
            {
                narratorOrb.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.06f);
                narratorOrb.color = new Color(0.33f, 1f, 0.88f, 0.1f + wave * 0.06f);
            }

            // 3. The real-time model or static avatar of the witch floats up and down slightly and zooms in and out to breathe.

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

            // 4. The title bar glows as a weak transparency pulse

            if (headerGlow != null)
            {
                headerGlow.color = new Color(0.33f, 1f, 0.88f, 0.08f + wave * 0.05f);
            }

            // 5. The focus frame emits light to make a zoom pulse

            if (focusGlow != null && focusGlow.gameObject.activeSelf)
            {
                focusGlow.localScale = Vector3.one * (1f + wave * 0.03f);
            }

            // 6. The target area glows for scaling and transparency pulses

            if (targetGlowImage != null && targetGlowImage.gameObject.activeSelf)
            {
                targetGlowImage.rectTransform.localScale = Vector3.one * (1f + wave * 0.05f);
                targetGlowImage.color = new Color(1f, 0.8f, 0.4f, 0.12f + wave * 0.08f);
            }

            // 7. Do scaling and transparency pulses on the highest point ring

            if (apexRingImage != null && apexRingImage.gameObject.activeSelf)
            {
                apexRingImage.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.08f);
                apexRingImage.color = new Color(1f, 0.84f, 0.46f, 0.72f + wave * 0.18f);
            }

            // 8. Energy pulses for scaling and transparency pulses

            if (energyPulseImage != null && energyPulseImage.gameObject.activeSelf)
            {
                energyPulseImage.rectTransform.localScale = Vector3.one * (0.98f + wave * 0.08f);
                energyPulseImage.color = new Color(0.53f, 1f, 0.86f, 0.34f + wave * 0.18f);
            }

            // 9. Prevent the scaling of the overlay root node from being accidentally modified

            if (overlayRoot != null)
            {
                overlayRoot.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hides all focus boxes, target zones, peak rings, energy pulses, score zones and track markers.

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
        /// Updated all title and narrator text labels, and keynote displays.

        /// </summary>
        private void SetCopy(string stepLabel, string title, string subtitle, string narration, IReadOnlyList<string> keys)
        {
            // 1. Set the step number label (such as "3/10" or "TUTORIAL")

            if (stepText != null)
            {
                stepText.text = stepLabel ?? string.Empty;
            }

            // 2. Set the main title text

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            // 3. Set the subtitle text (displayed only if it is not empty)

            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? string.Empty;
                subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }

            // 4. Set the explanation text of the commentator (witch)

            if (narratorText != null)
            {
                narratorText.text = narration ?? string.Empty;
            }

            // 5. Update key prompt labels

            SetKeys(keys);
        }

        /// <summary>
        /// Show or hide the goal description below the title.

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
        /// Light up the progress dot to show the player's current step.

        /// </summary>
        private void SetProgress(int currentIndex, int total)
        {
            // 1. Traverse all progress points

            for (var i = 0; i < progressDots.Count; i++)
            {
                var dot = progressDots[i];
                if (dot == null)
                {
                    continue;
                }

                // 2. Hide dots that exceed the total number of steps
                var active = i < total;
                dot.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                // 3. Completed steps are in cyan, current steps are in gold, and unreached steps are in dark blue.

                dot.color = i < currentIndex
                    ? new Color32(0x8A, 0xFF, 0xE1, 0xFF)
                    : i == currentIndex
                        ? new Color32(0xFF, 0xD5, 0x7C, 0xFF)
                        : new Color32(0x35, 0x4C, 0x70, 0xE8);
            }
        }

        /// <summary>
        /// Show or hide each key prompt label and set its text content.

        /// </summary>
        private void SetKeys(IReadOnlyList<string> keys)
        {
            // 1. Traverse all key prompt labels (up to 5)

            for (var i = 0; i < keyChipRoots.Count; i++)
            {
                // 2. Determine whether there is a valid button text that needs to be displayed at this position

                var visibleChip = keys != null && i < keys.Count && !string.IsNullOrEmpty(keys[i]);
                keyChipRoots[i].SetActive(visibleChip);
                // 3. Set the button text when there is content (such as "W", "B", "S")

                if (visibleChip && i < keyChipLabels.Count && keyChipLabels[i] != null)
                {
                    keyChipLabels[i].text = keys[i];
                }
            }
        }

        /// <summary>
        /// Construct 2-point/3-point scoring area labels and boundary markers.

        /// </summary>
        private void BuildScoringGuide()
        {
            // 1. Create the root node of the score guide

            scoringGuideRoot = CreateRootRect("ScoringGuideRoot", overlayRoot).gameObject;

            // 2. Create semi-transparent filling of the scoring area on the left and right sides (the left side defaults to a three-partition color, and the right side has a two-partition color)

            scoringLeftFill = CreateImage("ScoringLeftFill", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0x35, 0xD8, 0xB8, 0x24));
            SetTopLeftRect(scoringLeftFill.rectTransform, 44f, 336f, 346f, 58f);
            scoringRightFill = CreateImage("ScoringRightFill", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0xFF, 0xB9, 0x4D, 0x24));
            SetTopLeftRect(scoringRightFill.rectTransform, 410f, 336f, 346f, 58f);

            // 3. Create the dividing line between two and three zones (luminous layer + core line)

            var lineGlow = CreateImage("ScoringLineGlow", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0xFF, 0xD5, 0x7C, 0x66));
            SetTopLeftRect(lineGlow.rectTransform, mlpConstants.Width2 - 2f, 148f, 4f, 268f);
            var lineCore = CreateImage("ScoringLineCore", scoringGuideRoot.transform as RectTransform, solidSprite, new Color32(0xFF, 0xF4, 0xB2, 0xEE));
            SetTopLeftRect(lineCore.rectTransform, mlpConstants.Width2 - 0.5f, 148f, 1f, 268f);

            // 4. Create dividing line labels and text labels for the left and right areas ("3PT RANGE" / "2PT RANGE")

            scoringLineLabel = CreateText("ScoringLineLabel", scoringGuideRoot.transform as RectTransform, 330f, 314f, 140f, 18f, 13, new Color32(0xFF, 0xF4, 0xB2, 0xFF), TextAlignmentOptions.Center, LoadButtonFont());
            scoringLeftLabel = CreateText("ScoringLeftLabel", scoringGuideRoot.transform as RectTransform, 108f, 354f, 170f, 24f, 20, Color.white, TextAlignmentOptions.Center, LoadButtonFont());
            scoringRightLabel = CreateText("ScoringRightLabel", scoringGuideRoot.transform as RectTransform, 522f, 354f, 170f, 24f, 20, Color.white, TextAlignmentOptions.Center, LoadButtonFont());

            // 5. Hidden by default (shown only when needed for tutorials)

            scoringGuideRoot.SetActive(false);
        }

        /// <summary>
        /// Color and label individual scoring areas as 2-Division or 3-Division.

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
        /// If the canvas doesn't already have a camera set, assign the main camera to it.

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
        /// If there is no EventSystem in the scene, create one.

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
        /// Solid color, circle and ring sprites used to generate overlays.

        /// </summary>
        private static void EnsureSprites()
        {
            // 1. If all three elves have been created, return directly

            if (solidSprite != null && circleSprite != null && ringSprite != null)
            {
                return;
            }

            // 2. Create solid color texture sprites (for background panels and masks)

            solidSprite = CreateSprite(CreateSolidTexture(4, 4, Color.white), "TutorialSolid");
            // 3. Create circular texture sprites (used for light effects and dots) with soft edges and transitions

            circleSprite = CreateSprite(CreateCircleTexture(128, 0.96f), "TutorialCircle");
            // 4. Create a circular texture sprite (for border outline) with soft inner and outer edges

            ringSprite = CreateSprite(CreateRingTexture(160, 0.72f, 0.9f), "TutorialRing");
        }

        /// <summary>
        /// Loads and caches the witch character's avatar sprite.

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
        /// Generates a small sized solid color texture.

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
        /// Generates a soft-edged circular texture for use with light effects and dots.

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
        /// Generates a hollow ring texture for use in border outlines.

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
        /// Encapsulate the texture as a named sprite anchored at the center.
        /// </summary>
        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            texture.name = name;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>
        /// Create a RectTransform that stretches to fill the parent.

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
        /// Creates a semi-transparent dark mask panel that darkens areas outside the focused area.

        /// </summary>
        private RectTransform CreateMask(string name)
        {
            var image = CreateImage(name, overlayRoot, solidSprite, new Color(0.02f, 0.04f, 0.08f, 0.52f));
            return image.rectTransform;
        }

        /// <summary>
        /// Create a dark card panel with a gold border and shadow.

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
        /// Creates a non-interactive Image element using the specified sprite and color.

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
        /// Creates a stroked TextMeshPro text label at a specified screen location.

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
        /// Create a "Press [key]" prompt label with a gold border.

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
        /// Create a styled button that triggers a tutorial command when clicked.

        /// </summary>
        private Button CreateButton(string name, RectTransform parent, float left, float top, float width, float height, string label, Color background, Color textColor, mlpTutorialOverlayCommand command)
        {
            // 1. Create a button GameObject, including RectTransform, Image and Button components

            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            SetTopLeftRect(rect, left, top, width, height);

            // 2. Set the button background color and white thin border

            var image = root.GetComponent<Image>();
            image.sprite = solidSprite;
            image.color = background;
            var outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            outline.effectDistance = new Vector2(1f, -1f);

            // 3. Configure the interaction color of the button (normal, hover, pressed, disabled)

            var button = root.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.14f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(background.r, background.g, background.b, 0.35f);
            button.colors = colors;
            // 4. Bind click events: Set pending tutorial commands

            button.targetGraphic = image;
            button.onClick.AddListener(() => pendingCommand = command);

            // 5. Create button text labels

            var text = CreateText("Label", rect, 0f, 6f, width, height - 6f, 16, textColor, TextAlignmentOptions.Center, LoadButtonFont());
            text.text = label;
            return button;
        }

        /// <summary>
        /// Set the RectTransform anchor point so that it stretches to fill the entire parent.

        /// </summary>
        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Position and size the RectTransform with the upper left corner as the anchor point.

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
        /// Position and size the RectTransform based on the center point.

        /// </summary>
        private static void SetCenteredRect(RectTransform rect, float centerX, float centerY, float width, float height)
        {
            SetTopLeftRect(rect, centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }

        /// <summary>
        /// A GameObject that shows or hides markers.

        /// </summary>
        private static void SetMarkerActive(RectTransform rect, bool active)
        {
            if (rect != null)
            {
                rect.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Raise the witch sprite's rendering level above the overlay canvas.

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
        /// Hides the basketball slot on the Witch model, showing only the character itself.

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
        /// Load the Impact font for large titles.

        /// </summary>
        private static TMP_FontAsset LoadTitleFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/Impact2 SDF") ?? TMP_Settings.defaultFontAsset;
        }

        /// <summary>
        /// Loads the Agency Bold font for button and key labels.

        /// </summary>
        private static TMP_FontAsset LoadButtonFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/AgencyBold SDF") ?? LoadTitleFont();
        }

        /// <summary>
        /// Loads the Rajdhani Bold font for body and explanatory text.

        /// </summary>
        private static TMP_FontAsset LoadBodyFont()
        {
            return Resources.Load<TMP_FontAsset>("mlp/Fonts/TMP/RajdhaniBold SDF") ?? LoadTitleFont();
        }
    }
}
