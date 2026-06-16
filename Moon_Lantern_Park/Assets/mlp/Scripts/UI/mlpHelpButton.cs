// The clickable button
//  in the help panel supports mouse hover discoloration and zoom effects, and is used for tab switching and action demonstration selection in the help panel.

using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Help panel button: supports mouse-over discoloration and zoom effects, used for tab switching and action demonstration selection of the help panel.
    /// </summary>
    public sealed class mlpHelpButton : MonoBehaviour
    {
        [SerializeField] private mlpHelpButtonAction action;
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

        public mlpHelpButtonAction Action => action;

        public void Configure(
            mlpHelpButtonAction configuredAction,
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
            // 1. Save the action type of the button (close, switch tabs, select presentation, etc.)
            action = configuredAction;
            // 2. Save the center position and size of the button on the screen (for mouse collision detection)
            pixelCenter = center;
            pixelSize = size;
            // 3. Save the visual element reference and the color of the three states (normal, hover, selected)
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
            // 4. Force reinitialization of the collision area and refresh the display
            initialized = false;
            EnsureInitialized();
            RefreshVisuals();
        }

        /// <summary>
        /// Initialize the button collision area and display the default visual state when the game starts.
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
            RefreshVisuals();
        }

        /// <summary>
        /// Resets the hover and pressed states when the button is disabled, preventing the button from remaining highlighted.
        /// </summary>
        private void OnDisable()
        {
            pressed = false;
            hovered = false;
            RefreshVisuals();
        }

        /// <summary>
        /// Detects whether the mouse is hovering over the button. Returns true when the player completes a full click release on the button. Needs to be called by the panel every frame.
        /// </summary>
        public bool Tick(Camera camera)
        {
            // 1. If the button is disabled or not in the activation level, return unclicked directly
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            // 2. Make sure the collision area has been initialized
            EnsureInitialized();
            // 3. Get the position of the mouse in the game pixel coordinates and determine whether it is within the button area
            var inside = TryGetMousePixel(camera, out var pixel) && pixelRect.Contains(pixel);
            hovered = inside;
            // 4. Update the color and scale of the button based on the hover state
            RefreshVisuals();

            // 5. Record the "pressed" state when the mouse is pressed
            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            // 6. When the mouse is released: If it was previously pressed and is still within the button area, it counts as a valid click
            if (!pressed || !Input.GetMouseButtonUp(0))
            {
                return false;
            }

            pressed = false;
            return inside;
        }

        /// <summary>
        /// Set the selected (highlighted) state of the button. Used for tabs and demo buttons so players can see which one is currently active.
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            EnsureInitialized();
            selected = isSelected;
            RefreshVisuals();
        }

        /// <summary>
        /// Constructs a pixel collision area based on the center point and dimensions set in the Inspector. Executed only once when the button is first used.
        /// </summary>
        private void EnsureInitialized()
        {
            // 1. If it has been initialized, directly return
            if (initialized)
            {
                return;
            }

            // 2. Mark as initialized
            initialized = true;
            // 3. If no visual root node is specified, use the button's own Transform
            visualRoot = visualRoot != null ? visualRoot : transform;
            // 4. Record the original scale value (it will be enlarged on this basis when hovering)
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            // 5. Construct a pixel collision rectangle based on the center point and size (used to detect whether the mouse is on the button)
            pixelRect = new Rect(
                pixelCenter.x - pixelSize.x * 0.5f,
                pixelCenter.y - pixelSize.y * 0.5f,
                pixelSize.x,
                pixelSize.y);
        }

        /// <summary>
        /// Update the color and scale according to the hover, selected or normal state of the button. The button enlarges slightly when hovered and uses a different coloring when selected.
        /// </summary>
        private void RefreshVisuals()
        {
            // 1. If not initialized yet, skip
            if (!initialized)
            {
                return;
            }

            // 2. Slightly enlarge button when hovered and unselected, otherwise return to original size
            var scaleTarget = hovered && !selected ? hoverScale : 1f;
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale * scaleTarget;
            }

            // 3. Select background color based on state: Selected > Hover > Normal
            var tint = selected ? selectedTint : hovered ? hoverTint : normalTint;
            // 4. Apply background color to all sprite renderers that need shading
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

            // 5. Select text color based on state: Selected > Hover > Normal
            var labelTint = selected ? selectedLabelTint : hovered ? hoverLabelTint : normalLabelTint;
            // 6. Apply text color to all text components
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
        /// Convert mouse screen coordinates to game pixel coordinates. Returns false when the mouse is outside the game window or camera field of view.
        /// </summary>
        private static bool TryGetMousePixel(Camera camera, out Vector2 pixel)
        {
            var mouse = Input.mousePosition;
            if (mlpFixedResolutionPresenter.HasActivePresenter)
            {
                return mlpFixedResolutionPresenter.TryMapScreenToGamePixel(mouse, out pixel);
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
            pixel = mlpConstants.WorldToPixel(world);
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Configure buttons via the prefab builder. Only used when creating or updating a help panel prefab in the Unity Editor.
        /// </summary>
        public void EditorConfigure(
            mlpHelpButtonAction configuredAction,
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
