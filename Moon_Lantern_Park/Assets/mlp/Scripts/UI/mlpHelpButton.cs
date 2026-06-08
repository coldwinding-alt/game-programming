// 帮助面板里的可点击按钮
// 支持鼠标悬停变色和缩放效果，用于帮助面板的标签页切换和动作演示选择。

using TMPro;
using UnityEngine;

namespace mlp
{
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
        /// 游戏启动时初始化按钮碰撞区域并显示默认视觉状态。
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
            RefreshVisuals();
        }

        /// <summary>
        /// 按钮禁用时重置悬停和按下状态，避免按钮保持高亮显示。
        /// </summary>
        private void OnDisable()
        {
            pressed = false;
            hovered = false;
            RefreshVisuals();
        }

        /// <summary>
        /// 检测鼠标是否悬停在按钮上。当玩家在按钮上完成一次完整的点击释放时返回 true。需要由面板每帧调用。
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
        /// 设置按钮的选中（高亮）状态。用于标签页和演示按钮，让玩家能看到当前激活的是哪个。
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            EnsureInitialized();
            selected = isSelected;
            RefreshVisuals();
        }

        /// <summary>
        /// 根据 Inspector 中设置的中心点和尺寸构建像素碰撞区域。仅在按钮首次使用时执行一次。
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
        /// 根据按钮的悬停、选中或普通状态更新颜色和缩放。悬停时按钮略微放大，选中时使用不同的着色。
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
        /// 将鼠标屏幕坐标转换为游戏像素坐标。当鼠标在游戏窗口或摄像机视野之外时返回 false。
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
        /// 通过预制体构建器配置按钮。仅在 Unity 编辑器中创建或更新帮助面板预制体时使用。
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
