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

        private void Awake()
        {
            EnsureInitialized();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            pressed = false;
            hovered = false;
            RefreshVisuals();
        }

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

        public void SetSelected(bool isSelected)
        {
            EnsureInitialized();
            selected = isSelected;
            RefreshVisuals();
        }

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
