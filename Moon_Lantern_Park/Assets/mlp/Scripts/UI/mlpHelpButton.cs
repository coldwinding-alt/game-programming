// 帮助面板里的可点击按钮
// 支持鼠标悬停变色和缩放效果，用于帮助面板的标签页切换和动作演示选择。

using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 帮助面板按钮：支持鼠标悬停变色和缩放效果，用于帮助面板的标签页切换和动作演示选择。
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
            // 1. 保存按钮的动作类型（关闭、切换标签页、选择演示等）
            action = configuredAction;
            // 2. 保存按钮在屏幕上的中心位置和尺寸（用于鼠标碰撞检测）
            pixelCenter = center;
            pixelSize = size;
            // 3. 保存视觉元素引用和三种状态的颜色（普通、悬停、选中）
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
            // 4. 强制重新初始化碰撞区域并刷新显示
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
            // 1. 如果按钮被禁用或不在激活层级中，直接返回未点击
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            // 2. 确保碰撞区域已初始化
            EnsureInitialized();
            // 3. 获取鼠标在游戏像素坐标中的位置，判断是否在按钮区域内
            var inside = TryGetMousePixel(camera, out var pixel) && pixelRect.Contains(pixel);
            hovered = inside;
            // 4. 根据悬停状态更新按钮的颜色和缩放
            RefreshVisuals();

            // 5. 鼠标按下时记录"已按下"状态
            if (inside && Input.GetMouseButtonDown(0))
            {
                pressed = true;
            }

            // 6. 鼠标松开时：如果之前按下了且仍在按钮区域内，则算作一次有效点击
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
            // 1. 如果已经初始化过，直接返回
            if (initialized)
            {
                return;
            }

            // 2. 标记为已初始化
            initialized = true;
            // 3. 如果没有指定视觉根节点，使用按钮自身的 Transform
            visualRoot = visualRoot != null ? visualRoot : transform;
            // 4. 记录原始缩放值（悬停时会在此基础上放大）
            baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            // 5. 根据中心点和尺寸构建像素碰撞矩形（用于检测鼠标是否在按钮上）
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
            // 1. 如果尚未初始化，跳过
            if (!initialized)
            {
                return;
            }

            // 2. 悬停且未选中时略微放大按钮，否则恢复原始大小
            var scaleTarget = hovered && !selected ? hoverScale : 1f;
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale * scaleTarget;
            }

            // 3. 根据状态选择背景颜色：选中 > 悬停 > 普通
            var tint = selected ? selectedTint : hovered ? hoverTint : normalTint;
            // 4. 将背景颜色应用到所有需要着色的精灵渲染器
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

            // 5. 根据状态选择文字颜色：选中 > 悬停 > 普通
            var labelTint = selected ? selectedLabelTint : hovered ? hoverLabelTint : normalLabelTint;
            // 6. 将文字颜色应用到所有文字组件
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
