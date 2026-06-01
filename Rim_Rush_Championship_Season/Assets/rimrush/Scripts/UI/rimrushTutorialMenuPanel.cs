using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rimrush
{
    public enum rimrushTutorialMenuPage
    {
        Overview,
        Controls
    }

    public enum rimrushTutorialMenuCommand
    {
        None,
        Close,
        StartTutorial,
        StartTraining,
        PreviousCharacter,
        NextCharacter,
        PreviousBall,
        NextBall,
        OverviewTab,
        ControlsTab
    }

    [ExecuteAlways]
    public sealed class rimrushTutorialMenuPanel : MonoBehaviour
    {
        private const int WitchCharacterId = 6;
        private const int PreviewSortingOrderBase = 980;

        private static rimrushTutorialMenuPanel activePanel;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject overviewPageRoot;
        [SerializeField] private GameObject controlsPageRoot;
        [SerializeField] private Image overviewTabPlate;
        [SerializeField] private Image controlsTabPlate;
        [SerializeField] private TMP_Text overviewTabLabel;
        [SerializeField] private TMP_Text controlsTabLabel;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text ballNameText;
        [SerializeField] private TMP_Text modeTitleText;
        [SerializeField] private TMP_Text modeBodyText;
        [SerializeField] private Image ballPreviewImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button startTutorialButton;
        [SerializeField] private Button startTrainingButton;
        [SerializeField] private Button previousCharacterButton;
        [SerializeField] private Button nextCharacterButton;
        [SerializeField] private Button previousBallButton;
        [SerializeField] private Button nextBallButton;
        [SerializeField] private Button overviewTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Transform characterPreviewMount;
        [SerializeField] private Transform witchPreviewMount;
        [SerializeField] private Image panelGlow;

        private DBLiteArmature previewCharacterArmature;
        private DBLiteArmature previewWitchArmature;
        private bool visible;
        private bool initialized;
        private bool listenersBound;
        private float visibleTime;
        private int previewCharacterId;
        private rimrushBallSelection previewBallSelection;
        private rimrushTutorialMenuPage currentPage = rimrushTutorialMenuPage.Overview;
        private rimrushTutorialMenuCommand pendingCommand;

        public bool IsVisible => visible;
        public rimrushTutorialMenuPage CurrentPage => currentPage;

        public static bool IsAnyOpen
        {
            get
            {
                var panel = activePanel != null ? activePanel : FindScenePanel();
                return panel != null && panel.visible;
            }
        }

        public static void ShowOverview(int characterId, rimrushBallSelection ballSelection)
        {
            var panel = FindScenePanel();
            if (panel != null)
            {
                panel.Show(rimrushTutorialMenuPage.Overview, characterId, ballSelection);
            }
        }

        public static void ShowControls(int characterId, rimrushBallSelection ballSelection)
        {
            var panel = FindScenePanel();
            if (panel != null)
            {
                panel.Show(rimrushTutorialMenuPage.Controls, characterId, ballSelection);
            }
        }

        public static void HideActive()
        {
            var panel = activePanel != null ? activePanel : FindScenePanel();
            panel?.Hide();
        }

        public static rimrushTutorialMenuPage ActivePage
        {
            get
            {
                var panel = activePanel != null ? activePanel : FindScenePanel();
                return panel != null ? panel.currentPage : rimrushTutorialMenuPage.Overview;
            }
        }

        public static rimrushTutorialMenuCommand ConsumeActiveCommand()
        {
            var panel = activePanel != null ? activePanel : FindScenePanel();
            return panel != null ? panel.ConsumeCommand() : rimrushTutorialMenuCommand.None;
        }

        public static void RefreshActiveSelection(int characterId, rimrushBallSelection ballSelection)
        {
            var panel = activePanel != null ? activePanel : FindScenePanel();
            panel?.RefreshSelection(characterId, ballSelection);
        }

        private void Awake()
        {
            EnsureCanvasCamera();
            if (Application.isPlaying)
            {
                if (activePanel == null || activePanel == this)
                {
                    activePanel = this;
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

            visibleTime += Time.unscaledDeltaTime;
            UpdateEntrance();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                pendingCommand = rimrushTutorialMenuCommand.Close;
            }
        }

        private void LateUpdate()
        {
            ApplyPreviewSortingOrder(previewCharacterArmature);
            ApplyPreviewSortingOrder(previewWitchArmature);
        }

        public void Show(rimrushTutorialMenuPage page, int characterId, rimrushBallSelection ballSelection)
        {
            EnsureInitialized();
            previewCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
            previewBallSelection = ballSelection;
            visible = true;
            visibleTime = 0f;
            pendingCommand = rimrushTutorialMenuCommand.None;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            transform.localScale = Vector3.one * 0.985f;
            SetPage(page);
            RefreshSelection(previewCharacterId, previewBallSelection);
        }

        public void Hide()
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
        }

        private void HideImmediate()
        {
            visible = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private rimrushTutorialMenuCommand ConsumeCommand()
        {
            var command = pendingCommand;
            pendingCommand = rimrushTutorialMenuCommand.None;
            return command;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            panelRoot = panelRoot != null ? panelRoot : gameObject;
            BindButtons();
            BuildPreviewActors();
            SetPage(currentPage);
            RefreshSelection(previewCharacterId, previewBallSelection);
        }

        private void BindButtons()
        {
            if (listenersBound)
            {
                return;
            }

            listenersBound = true;
            BindButton(closeButton, rimrushTutorialMenuCommand.Close);
            BindButton(backButton, rimrushTutorialMenuCommand.Close);
            BindButton(startTutorialButton, rimrushTutorialMenuCommand.StartTutorial);
            BindButton(startTrainingButton, rimrushTutorialMenuCommand.StartTraining);
            BindButton(previousCharacterButton, rimrushTutorialMenuCommand.PreviousCharacter);
            BindButton(nextCharacterButton, rimrushTutorialMenuCommand.NextCharacter);
            BindButton(previousBallButton, rimrushTutorialMenuCommand.PreviousBall);
            BindButton(nextBallButton, rimrushTutorialMenuCommand.NextBall);
            BindButton(overviewTabButton, rimrushTutorialMenuCommand.OverviewTab);
            BindButton(controlsTabButton, rimrushTutorialMenuCommand.ControlsTab);
        }

        private void BindButton(Button button, rimrushTutorialMenuCommand command)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => pendingCommand = command);
        }

        private void BuildPreviewActors()
        {
            if (characterPreviewMount != null && previewCharacterArmature == null)
            {
                previewCharacterArmature = rimrushPlayersData.BuildGameplayArmature("TutorialMenuCharacterPreview");
                if (previewCharacterArmature != null)
                {
                    previewCharacterArmature.transform.SetParent(characterPreviewMount, false);
                    previewCharacterArmature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                        characterPreviewMount,
                        new Vector3(0f, -38f, 0f));
                    previewCharacterArmature.transform.localScale = new Vector3(
                        rimrushConstants.PixelPerfectCharacterScale * 1.12f,
                        rimrushConstants.PixelPerfectCharacterScale * 1.12f,
                        1f);
                    previewCharacterArmature.Play("idle");
                }
            }

            if (witchPreviewMount != null && previewWitchArmature == null)
            {
                previewWitchArmature = rimrushPlayersData.BuildGameplayArmature("TutorialMenuWitchPreview");
                if (previewWitchArmature != null)
                {
                    previewWitchArmature.transform.SetParent(witchPreviewMount, false);
                    previewWitchArmature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                        witchPreviewMount,
                        new Vector3(0f, -34f, 0f));
                    previewWitchArmature.transform.localScale = new Vector3(
                        rimrushConstants.PixelPerfectCharacterScale * 0.94f,
                        rimrushConstants.PixelPerfectCharacterScale * 0.94f,
                        1f);
                    rimrushPlayersData.ApplyCharacter(previewWitchArmature, WitchCharacterId);
                    HidePreviewBall(previewWitchArmature);
                    previewWitchArmature.StopAtStart("idle");
                }
            }
        }

        private void RefreshSelection(int characterId, rimrushBallSelection ballSelection)
        {
            previewCharacterId = rimrushPlayersData.SanitizeCharacterId(characterId);
            previewBallSelection = ballSelection;

            if (characterNameText != null)
            {
                characterNameText.text = rimrushPlayersData.GetCharacterName(previewCharacterId);
            }

            if (ballNameText != null)
            {
                ballNameText.text = rimrushBallCatalog.Label(previewBallSelection);
            }

            if (modeTitleText != null)
            {
                modeTitleText.text = currentPage == rimrushTutorialMenuPage.Overview
                    ? "LEARN THE FULL MATCH LOOP"
                    : "SHORTCUT CONTROLS";
            }

            if (modeBodyText != null)
            {
                modeBodyText.text = currentPage == rimrushTutorialMenuPage.Overview
                    ? "Dash, shoot, steal, block, and trigger your signature super in one guided run."
                    : "One screen. No wall of rules. Just the buttons you actually need in front of the examiner.";
            }

            if (ballPreviewImage != null)
            {
                ballPreviewImage.sprite = rimrushGameplaySpriteLoader.LoadBallThemeSprite(
                    rimrushBallCatalog.PreviewTheme(previewBallSelection),
                    0.5f,
                    0.5f);
                ballPreviewImage.enabled = ballPreviewImage.sprite != null;
            }

            if (previewCharacterArmature != null)
            {
                rimrushPlayersData.ApplyCharacter(previewCharacterArmature, previewCharacterId);
                HidePreviewBall(previewCharacterArmature);
                previewCharacterArmature.StopAtStart("idle");
            }
        }

        private void SetPage(rimrushTutorialMenuPage page)
        {
            currentPage = page;
            if (overviewPageRoot != null)
            {
                overviewPageRoot.SetActive(page == rimrushTutorialMenuPage.Overview);
            }

            if (controlsPageRoot != null)
            {
                controlsPageRoot.SetActive(page == rimrushTutorialMenuPage.Controls);
            }

            SetTabVisual(overviewTabPlate, overviewTabLabel, page == rimrushTutorialMenuPage.Overview);
            SetTabVisual(controlsTabPlate, controlsTabLabel, page == rimrushTutorialMenuPage.Controls);
            RefreshSelection(previewCharacterId, previewBallSelection);
        }

        private void UpdateEntrance()
        {
            var t = Mathf.Clamp01(visibleTime / 0.16f);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, eased);

            if (panelGlow != null)
            {
                var pulse = 0.9f + Mathf.Sin(Time.unscaledTime * 1.9f) * 0.08f;
                panelGlow.color = new Color(panelGlow.color.r, panelGlow.color.g, panelGlow.color.b, pulse);
            }
        }

        private static void SetTabVisual(Image plate, TMP_Text label, bool selected)
        {
            if (plate != null)
            {
                plate.color = selected
                    ? new Color32(0xFF, 0xC1, 0x4C, 0xFF)
                    : new Color32(0x22, 0x31, 0x50, 0xF0);
            }

            if (label != null)
            {
                label.color = selected
                    ? new Color32(0x1D, 0x13, 0x08, 0xFF)
                    : new Color32(0xF1, 0xF7, 0xFF, 0xFF);
            }
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

        private static rimrushTutorialMenuPanel FindScenePanel()
        {
            var panels = Resources.FindObjectsOfTypeAll<rimrushTutorialMenuPanel>();
            for (var i = 0; i < panels.Length; i++)
            {
                var panel = panels[i];
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    activePanel = panel;
                    return panel;
                }
            }

            return null;
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
            GameObject configuredPanelRoot,
            GameObject configuredOverviewPage,
            GameObject configuredControlsPage,
            Image configuredOverviewTabPlate,
            Image configuredControlsTabPlate,
            TMP_Text configuredOverviewTabLabel,
            TMP_Text configuredControlsTabLabel,
            TMP_Text configuredCharacterNameText,
            TMP_Text configuredBallNameText,
            TMP_Text configuredModeTitleText,
            TMP_Text configuredModeBodyText,
            Image configuredBallPreviewImage,
            Button configuredCloseButton,
            Button configuredBackButton,
            Button configuredStartTutorialButton,
            Button configuredStartTrainingButton,
            Button configuredPreviousCharacterButton,
            Button configuredNextCharacterButton,
            Button configuredPreviousBallButton,
            Button configuredNextBallButton,
            Button configuredOverviewTabButton,
            Button configuredControlsTabButton,
            Transform configuredCharacterPreviewMount,
            Transform configuredWitchPreviewMount,
            Image configuredPanelGlow)
        {
            panelRoot = configuredPanelRoot;
            overviewPageRoot = configuredOverviewPage;
            controlsPageRoot = configuredControlsPage;
            overviewTabPlate = configuredOverviewTabPlate;
            controlsTabPlate = configuredControlsTabPlate;
            overviewTabLabel = configuredOverviewTabLabel;
            controlsTabLabel = configuredControlsTabLabel;
            characterNameText = configuredCharacterNameText;
            ballNameText = configuredBallNameText;
            modeTitleText = configuredModeTitleText;
            modeBodyText = configuredModeBodyText;
            ballPreviewImage = configuredBallPreviewImage;
            closeButton = configuredCloseButton;
            backButton = configuredBackButton;
            startTutorialButton = configuredStartTutorialButton;
            startTrainingButton = configuredStartTrainingButton;
            previousCharacterButton = configuredPreviousCharacterButton;
            nextCharacterButton = configuredNextCharacterButton;
            previousBallButton = configuredPreviousBallButton;
            nextBallButton = configuredNextBallButton;
            overviewTabButton = configuredOverviewTabButton;
            controlsTabButton = configuredControlsTabButton;
            characterPreviewMount = configuredCharacterPreviewMount;
            witchPreviewMount = configuredWitchPreviewMount;
            panelGlow = configuredPanelGlow;
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
            panelRoot.SetActive(true);
            if (overviewPageRoot != null)
            {
                overviewPageRoot.SetActive(true);
            }

            if (controlsPageRoot != null)
            {
                controlsPageRoot.SetActive(false);
            }

            transform.localScale = Vector3.one;
            SetTabVisual(overviewTabPlate, overviewTabLabel, true);
            SetTabVisual(controlsTabPlate, controlsTabLabel, false);
            EnsureInitialized();
            RefreshSelection(previewCharacterId, previewBallSelection == 0 ? rimrushBallSelection.ClassicOriginal : previewBallSelection);
        }
#endif
    }
}
