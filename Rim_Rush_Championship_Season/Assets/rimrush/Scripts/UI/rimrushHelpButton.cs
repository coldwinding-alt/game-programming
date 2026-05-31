// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushHelpButton 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using TMPro;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushHelpButton : MonoBehaviour
    {
        [SerializeField] private rimrushHelpButtonAction action;
        [SerializeField] private Vector2 pixelCenter;
        [SerializeField] private Vector2 pixelSize;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer[] tintTargets;
        [SerializeField] private TMP_Text[] labelTargets;
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private Color hoverTint = Color.white;
        [SerializeField] private Color selectedTint = Color.white;
        [SerializeField] private Color normalLabelTint = Color.white;
        [SerializeField] private Color hoverLabelTint = Color.white;
        [SerializeField] private Color selectedLabelTint = Color.white;
        [SerializeField] private float hoverScale = 1.02f;

        private Rect pixelRect;
        private Vector3 baseScale = Vector3.one;
        private bool initialized;
        private bool pressed;
        private bool selected;
        private bool hovered;

        public rimrushHelpButtonAction Action => action;

        /// <summary>
        /// Executes Awake for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
            RefreshVisuals();
        }

        /// <summary>
        /// Executes On Disable for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void OnDisable()
        {
            pressed = false;
            hovered = false;
            RefreshVisuals();
        }

        /// <summary>
        /// Executes Tick for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="camera">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool Tick(Camera camera)
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            EnsureInitialized();
            var inside = TryGetMousePixel(camera, out var pixel) && pixelRect.Contains(pixel);
            hovered = inside;
            RefreshVisuals();

            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            if (!pressed || !Input.GetMouseButtonUp(0))
            {
                return false;
            }

            pressed = false;
            return inside;
        }

        /// <summary>
        /// Executes Set Selected for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="isSelected">Input value used by this step of the workflow.</param>
        public void SetSelected(bool isSelected)
        {
            EnsureInitialized();
            selected = isSelected;
            RefreshVisuals();
        }

        /// <summary>
        /// Executes Ensure Initialized for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            visualRoot = visualRoot != null ? visualRoot : transform;
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            pixelRect = new Rect(
                pixelCenter.x - pixelSize.x * 0.5f,
                pixelCenter.y - pixelSize.y * 0.5f,
                pixelSize.x,
                pixelSize.y);
        }

        /// <summary>
        /// Executes Refresh Visuals for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void RefreshVisuals()
        {
            if (!initialized)
            {
                return;
            }

            var scaleTarget = hovered && !selected ? hoverScale : 1f;
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale * scaleTarget;
            }

            var tint = selected ? selectedTint : hovered ? hoverTint : normalTint;
            if (tintTargets != null)
            {
                for (var i = 0; i < tintTargets.Length; i++)
                {
                    if (tintTargets[i] != null)
                    {
                        tintTargets[i].color = tint;
                    }
                }
            }

            var labelTint = selected ? selectedLabelTint : hovered ? hoverLabelTint : normalLabelTint;
            if (labelTargets != null)
            {
                for (var i = 0; i < labelTargets.Length; i++)
                {
                    if (labelTargets[i] != null)
                    {
                        labelTargets[i].color = labelTint;
                    }
                }
            }
        }

        /// <summary>
        /// Executes Try Get Mouse Pixel for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="camera">Input value used by this step of the workflow.</param>
        /// <param name="pixel">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private static bool TryGetMousePixel(Camera camera, out Vector2 pixel)
        {
            var mouse = Input.mousePosition;
            if (rimrushFixedResolutionPresenter.HasActivePresenter)
            {
                return rimrushFixedResolutionPresenter.TryMapScreenToGamePixel(mouse, out pixel);
            }

            if (camera == null)
            {
                pixel = default;
                return false;
            }

            var screenPoint = new Vector2(mouse.x, mouse.y);
            if (!camera.pixelRect.Contains(screenPoint))
            {
                pixel = default;
                return false;
            }

            var world = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -camera.transform.position.z));
            pixel = rimrushConstants.WorldToPixel(world);
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Executes Editor Configure for the rimrushHelpButton workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="configuredAction">Input value used by this step of the workflow.</param>
        /// <param name="center">Input value used by this step of the workflow.</param>
        /// <param name="size">Input value used by this step of the workflow.</param>
        /// <param name="configuredVisualRoot">Input value used by this step of the workflow.</param>
        /// <param name="configuredTintTargets">Input value used by this step of the workflow.</param>
        /// <param name="configuredLabelTargets">Input value used by this step of the workflow.</param>
        /// <param name="configuredNormalTint">Input value used by this step of the workflow.</param>
        /// <param name="configuredHoverTint">Input value used by this step of the workflow.</param>
        /// <param name="configuredSelectedTint">Input value used by this step of the workflow.</param>
        /// <param name="configuredNormalLabelTint">Input value used by this step of the workflow.</param>
        /// <param name="configuredHoverLabelTint">Input value used by this step of the workflow.</param>
        /// <param name="configuredSelectedLabelTint">Input value used by this step of the workflow.</param>
        /// <param name="configuredHoverScale">Input value used by this step of the workflow.</param>
        public void EditorConfigure(
            rimrushHelpButtonAction configuredAction,
            Vector2 center,
            Vector2 size,
            Transform configuredVisualRoot,
            SpriteRenderer[] configuredTintTargets,
            TMP_Text[] configuredLabelTargets,
            Color configuredNormalTint,
            Color configuredHoverTint,
            Color configuredSelectedTint,
            Color configuredNormalLabelTint,
            Color configuredHoverLabelTint,
            Color configuredSelectedLabelTint,
            float configuredHoverScale)
        {
            action = configuredAction;
            pixelCenter = center;
            pixelSize = size;
            visualRoot = configuredVisualRoot;
            tintTargets = configuredTintTargets;
            labelTargets = configuredLabelTargets;
            normalTint = configuredNormalTint;
            hoverTint = configuredHoverTint;
            selectedTint = configuredSelectedTint;
            normalLabelTint = configuredNormalLabelTint;
            hoverLabelTint = configuredHoverLabelTint;
            selectedLabelTint = configuredSelectedLabelTint;
            hoverScale = configuredHoverScale;
        }
#endif
    }
}
