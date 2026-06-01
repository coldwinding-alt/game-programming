using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rimrush
{
    public enum rimrushTutorialOverlayCommand
    {
        None,
        ReplayTutorial,
        StartTraining,
        StartQuickMatch
    }

    [ExecuteAlways]
    public sealed class rimrushTutorialOverlay : MonoBehaviour
    {
        private const int WitchCharacterId = 6;
        private const int PreviewSortingOrderBase = 990;
        private const string PrefabResourcePath = "rimrush/Prefabs/UI/RimrushTutorialOverlay";

        private static rimrushTutorialOverlay activeOverlay;

        [SerializeField] private bool editorPreviewVisible;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text footerHintText;
        [SerializeField] private Image[] progressDots;
        [SerializeField] private GameObject[] keyChipRoots;
        [SerializeField] private TMP_Text[] keyChipLabels;
        [SerializeField] private RectTransform maskTop;
        [SerializeField] private RectTransform maskBottom;
        [SerializeField] private RectTransform maskLeft;
        [SerializeField] private RectTransform maskRight;
        [SerializeField] private RectTransform focusFrame;
        [SerializeField] private RectTransform focusGlow;
        [SerializeField] private Image narratorGlow;
        [SerializeField] private Image boardGlow;
        [SerializeField] private Transform witchMount;
        [SerializeField] private GameObject outroRoot;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button trainingButton;
        [SerializeField] private Button quickMatchButton;

        private DBLiteArmature witchArmature;
        private bool initialized;
        private bool listenersBound;
        private bool visible;
        private float visibleTime;
        private float feedbackTimer;
        private rimrushTutorialOverlayCommand pendingCommand;

        public bool IsVisible => visible;

        public static rimrushTutorialOverlay Active
        {
            get
            {
                return FindActiveOverlay(true);
            }
        }

        private void Awake()
        {
            EnsureCanvasCamera();
            if (Application.isPlaying)
            {
                if (activeOverlay == null || activeOverlay == this)
                {
                    activeOverlay = this;
                }

                HideImmediate();
            }
            else
            {
                ApplyEditorPreviewState();
            }
        }

        private void OnEnable()
        {
            EnsureCanvasCamera();
            if (!Application.isPlaying)
            {
                ApplyEditorPreviewState();
            }
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
            if (activeOverlay == this)
            {
                activeOverlay = null;
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

            visibleTime += Time.unscaledDeltaTime;
            UpdatePresentation();

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
            ApplyPreviewSortingOrder(witchArmature);
        }

        public void BeginTutorial()
        {
            EnsureInitialized();
            visible = true;
            visibleTime = 0f;
            pendingCommand = rimrushTutorialOverlayCommand.None;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(true);
            }

            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            transform.localScale = Vector3.one * 0.99f;
        }

        public void Hide()
        {
            if (!visible)
            {
                return;
            }

            visible = false;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        public void ShowStep(int index, int total, string title, string body, string tip, string footerHint, string[] keys)
        {
            BeginTutorial();
            SetText(stepCounterText, $"STEP {index + 1} / {Mathf.Max(1, total)}");
            SetText(titleText, title);
            SetText(bodyText, body);
            SetText(tipText, tip);
            SetText(footerHintText, footerHint);
            SetKeyTexts(keys);
            SetProgress(index, total);
        }

        public void SetFocusRect(float left, float top, float width, float height)
        {
            EnsureInitialized();
            var clampedLeft = Mathf.Clamp(left, 0f, rimrushConstants.Width);
            var clampedTop = Mathf.Clamp(top, 0f, rimrushConstants.GameH / rimrushConstants.RenderScale);
            var clampedWidth = Mathf.Clamp(width, 0f, rimrushConstants.Width - clampedLeft);
            var clampedHeight = Mathf.Clamp(height, 0f, 480f - clampedTop);
            var right = clampedLeft + clampedWidth;
            var bottom = clampedTop + clampedHeight;

            SetTopLeftRect(maskTop, 0f, 0f, 800f, clampedTop);
            SetTopLeftRect(maskBottom, 0f, bottom, 800f, Mathf.Max(0f, 480f - bottom));
            SetTopLeftRect(maskLeft, 0f, clampedTop, clampedLeft, clampedHeight);
            SetTopLeftRect(maskRight, right, clampedTop, Mathf.Max(0f, 800f - right), clampedHeight);
            SetTopLeftRect(focusFrame, clampedLeft, clampedTop, clampedWidth, clampedHeight);
            SetTopLeftRect(focusGlow, clampedLeft - 10f, clampedTop - 10f, clampedWidth + 20f, clampedHeight + 20f);
        }

        public void ClearFocus()
        {
            SetFocusRect(0f, 0f, 0f, 0f);
        }

        public void ShowFeedback(string message, Color color, float duration = 0.9f)
        {
            if (feedbackText == null)
            {
                return;
            }

            feedbackText.text = message ?? string.Empty;
            feedbackText.color = color;
            feedbackTimer = duration;
        }

        public void ShowOutro()
        {
            EnsureInitialized();
            if (outroRoot != null)
            {
                outroRoot.SetActive(true);
            }

            SetText(stepCounterText, "TUTORIAL COMPLETE");
            SetText(titleText, "YOU'RE READY TO IMPRESS");
            SetText(bodyText, "The examiner just saw movement, timing, defense, and character skills in one clean flow.");
            SetText(tipText, "Pick the next mode and keep that momentum.");
            SetText(footerHintText, string.Empty);
            SetKeyTexts(null);
            ShowFeedback(string.Empty, Color.white, 0f);
        }

        public rimrushTutorialOverlayCommand ConsumeCommand()
        {
            var command = pendingCommand;
            pendingCommand = rimrushTutorialOverlayCommand.None;
            return command;
        }

        private void HideImmediate()
        {
            visible = false;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            overlayRoot = overlayRoot != null ? overlayRoot : gameObject;
            BindButtons();
            BuildWitchPreview();
        }

        private void BindButtons()
        {
            if (listenersBound)
            {
                return;
            }

            listenersBound = true;
            BindButton(replayButton, rimrushTutorialOverlayCommand.ReplayTutorial);
            BindButton(trainingButton, rimrushTutorialOverlayCommand.StartTraining);
            BindButton(quickMatchButton, rimrushTutorialOverlayCommand.StartQuickMatch);
        }

        private void BindButton(Button button, rimrushTutorialOverlayCommand command)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => pendingCommand = command);
        }

        private void BuildWitchPreview()
        {
            if (witchMount == null || witchArmature != null)
            {
                return;
            }

            witchArmature = rimrushPlayersData.BuildGameplayArmature("TutorialOverlayWitchPreview");
            if (witchArmature == null)
            {
                return;
            }

            witchArmature.transform.SetParent(witchMount, false);
            witchArmature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                witchMount,
                new Vector3(0f, -36f, 0f));
            witchArmature.transform.localScale = new Vector3(
                rimrushConstants.PixelPerfectCharacterScale * 1.02f,
                rimrushConstants.PixelPerfectCharacterScale * 1.02f,
                1f);
            rimrushPlayersData.ApplyCharacter(witchArmature, WitchCharacterId);
            witchArmature.StopAtStart("idle");
            HidePreviewBall(witchArmature);
        }

        private void UpdatePresentation()
        {
            var t = Mathf.Clamp01(visibleTime / 0.14f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.99f, 1f, eased);

            if (boardGlow != null)
            {
                var glowAlpha = 0.58f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.08f;
                boardGlow.color = new Color(boardGlow.color.r, boardGlow.color.g, boardGlow.color.b, glowAlpha);
            }

            if (narratorGlow != null)
            {
                var narratorAlpha = 0.66f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.1f;
                narratorGlow.color = new Color(narratorGlow.color.r, narratorGlow.color.g, narratorGlow.color.b, narratorAlpha);
            }
        }

        private void SetKeyTexts(string[] keys)
        {
            var chipCount = keyChipRoots != null ? keyChipRoots.Length : 0;
            for (var i = 0; i < chipCount; i++)
            {
                var hasKey = keys != null && i < keys.Length && !string.IsNullOrEmpty(keys[i]);
                if (keyChipRoots[i] != null)
                {
                    keyChipRoots[i].SetActive(hasKey);
                }

                if (hasKey && keyChipLabels != null && i < keyChipLabels.Length && keyChipLabels[i] != null)
                {
                    keyChipLabels[i].text = keys[i];
                }
            }
        }

        private void SetProgress(int currentIndex, int total)
        {
            if (progressDots == null)
            {
                return;
            }

            for (var i = 0; i < progressDots.Length; i++)
            {
                var active = i < total;
                if (progressDots[i] != null)
                {
                    progressDots[i].gameObject.SetActive(active);
                    progressDots[i].color = !active
                        ? new Color32(0x19, 0x28, 0x43, 0xD0)
                        : i < currentIndex
                            ? new Color32(0x56, 0xFF, 0xC8, 0xFF)
                            : i == currentIndex
                                ? new Color32(0xFF, 0xC5, 0x55, 0xFF)
                                : new Color32(0x42, 0x5F, 0x87, 0xFF);
                }
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value ?? string.Empty;
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

        private static void ApplyPreviewSortingOrder(DBLiteArmature armature)
        {
            if (armature == null)
            {
                return;
            }

            var renderers = armature.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.sortingOrder < PreviewSortingOrderBase)
                {
                    renderer.sortingOrder += PreviewSortingOrderBase;
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

        private static rimrushTutorialOverlay FindSceneOverlay()
        {
            var overlays = Resources.FindObjectsOfTypeAll<rimrushTutorialOverlay>();
            for (var i = 0; i < overlays.Length; i++)
            {
                var overlay = overlays[i];
                if (overlay != null && overlay.gameObject.scene.IsValid())
                {
                    activeOverlay = overlay;
                    return overlay;
                }
            }

            return null;
        }

        private static rimrushTutorialOverlay FindActiveOverlay(bool createFallback)
        {
            if (activeOverlay != null)
            {
                return activeOverlay;
            }

            activeOverlay = FindSceneOverlay();
            if (activeOverlay != null || !createFallback)
            {
                return activeOverlay;
            }

            var prefab = Resources.Load<rimrushTutorialOverlay>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Missing tutorial overlay prefab at Resources/{PrefabResourcePath}.");
                return null;
            }

            activeOverlay = Object.Instantiate(prefab);
            activeOverlay.name = "RimrushTutorialOverlay_RuntimeFallback";
            return activeOverlay;
        }

        private void EnsureCanvasCamera()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            GameObject configuredOverlayRoot,
            TMP_Text configuredStepCounterText,
            TMP_Text configuredTitleText,
            TMP_Text configuredBodyText,
            TMP_Text configuredTipText,
            TMP_Text configuredFeedbackText,
            TMP_Text configuredFooterHintText,
            Image[] configuredProgressDots,
            GameObject[] configuredKeyChipRoots,
            TMP_Text[] configuredKeyChipLabels,
            RectTransform configuredMaskTop,
            RectTransform configuredMaskBottom,
            RectTransform configuredMaskLeft,
            RectTransform configuredMaskRight,
            RectTransform configuredFocusFrame,
            RectTransform configuredFocusGlow,
            Image configuredNarratorGlow,
            Image configuredBoardGlow,
            Transform configuredWitchMount,
            GameObject configuredOutroRoot,
            Button configuredReplayButton,
            Button configuredTrainingButton,
            Button configuredQuickMatchButton)
        {
            overlayRoot = configuredOverlayRoot;
            stepCounterText = configuredStepCounterText;
            titleText = configuredTitleText;
            bodyText = configuredBodyText;
            tipText = configuredTipText;
            feedbackText = configuredFeedbackText;
            footerHintText = configuredFooterHintText;
            progressDots = configuredProgressDots;
            keyChipRoots = configuredKeyChipRoots;
            keyChipLabels = configuredKeyChipLabels;
            maskTop = configuredMaskTop;
            maskBottom = configuredMaskBottom;
            maskLeft = configuredMaskLeft;
            maskRight = configuredMaskRight;
            focusFrame = configuredFocusFrame;
            focusGlow = configuredFocusGlow;
            narratorGlow = configuredNarratorGlow;
            boardGlow = configuredBoardGlow;
            witchMount = configuredWitchMount;
            outroRoot = configuredOutroRoot;
            replayButton = configuredReplayButton;
            trainingButton = configuredTrainingButton;
            quickMatchButton = configuredQuickMatchButton;
        }

        private void ApplyEditorPreviewState()
        {
            if (this == null || Application.isPlaying || !gameObject.scene.IsValid())
            {
                return;
            }

            if (overlayRoot == null)
            {
                overlayRoot = gameObject;
            }

            visible = false;
            overlayRoot.SetActive(editorPreviewVisible);
            if (!editorPreviewVisible)
            {
                transform.localScale = Vector3.one;
                return;
            }

            if (outroRoot != null)
            {
                outroRoot.SetActive(false);
            }

            EnsureInitialized();
            ShowStep(
                1,
                5,
                "HIT THE SHOT AT THE APEX",
                "Jump first, then release the ball at the top of your rise.",
                "The judge should see timing, not button mashing.",
                "Tutorial overlay preview",
                new[] { "W", "B" });
            SetFocusRect(290f, 112f, 228f, 188f);
            transform.localScale = Vector3.one;
        }
#endif
    }
}
