// 帮助面板里的可点击按钮
// 支持鼠标悬停变色和缩放效果，用于帮助面板的标签页切换和动作演示选择。

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

        public void Configure(
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
            initialized = false;
            EnsureInitialized();
            RefreshVisuals();
        }

        /// <summary>
        /// Set up the button hitbox and show the default visual state when the game starts.
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
            RefreshVisuals();
        }

        /// <summary>
        /// Reset hover and press state when the button is turned off, so it doesn't stay highlighted.
        /// </summary>
        private void OnDisable()
        {
            pressed = false;
            hovered = false;
            RefreshVisuals();
        }

        /// <summary>
        /// Check if the mouse is hovering over this button. Returns true if the player
        /// clicked and released on it (a full press). Call this every frame from the panel.
        /// </summary>
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
        /// Mark this button as selected (highlighted) or not. Used for tabs and demo buttons
        /// so the player can see which one is currently active.
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            EnsureInitialized();
            selected = isSelected;
            RefreshVisuals();
        }

        /// <summary>
        /// Build the pixel hitbox from the center and size values set in the inspector.
        /// Only runs once, the first time the button is used.
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
        /// Update the button's color and scale based on whether it's hovered, selected, or normal.
        /// Hover makes it slightly bigger, selected uses a different tint color.
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
        /// Convert the mouse screen position to game pixel coordinates.
        /// Returns false if the mouse is outside the game window or camera view.
        /// </summary>
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
        /// Set up the button from the prefab builder. Only used in the Unity Editor
        /// when creating or updating the help panel prefab.
        /// </summary>
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
            Configure(
                configuredAction,
                center,
                size,
                configuredVisualRoot,
                configuredTintTargets,
                configuredLabelTargets,
                configuredNormalTint,
                configuredHoverTint,
                configuredSelectedTint,
                configuredNormalLabelTint,
                configuredHoverLabelTint,
                configuredSelectedLabelTint,
                configuredHoverScale);
        }
#endif
    }
}
