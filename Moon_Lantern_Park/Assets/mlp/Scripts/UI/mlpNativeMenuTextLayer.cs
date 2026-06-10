// 菜单界面的文字渲染层
// 用 TextMeshPro 在屏幕上绘制菜单文字，自动根据屏幕大小缩放，保证文字在不同分辨率下都清晰好看。

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace mlp
{
    /// <summary>
    /// 菜单文字渲染层：用 TextMeshPro 在屏幕上绘制菜单文字，自动根据屏幕大小缩放，保证文字清晰好看。
    /// </summary>
    public sealed class mlpNativeMenuTextLayer
    {
        private const float ReferenceWidth = mlpConstants.Width;
        private const float ReferenceHeight = 480f;
        private static readonly Vector2 ReferenceSize = new Vector2(ReferenceWidth, ReferenceHeight);

        private readonly Transform menuRoot;
        private readonly Canvas canvas;
        private readonly RectTransform viewportRoot;

        public static mlpNativeMenuTextLayer Active { get; private set; }

        /// <summary>
        /// 创建一个全屏画布，使用 TextMeshPro 绘制菜单文字。画布采用固定的 800x480 参考分辨率，确保文字位置在不同屏幕上保持一致。
        /// </summary>
        public mlpNativeMenuTextLayer(Transform menuRoot)
        {
            // 1. 保存菜单根节点的引用（后续判断某个文字是否属于该层）
            this.menuRoot = menuRoot;

            // 2. 创建画布——使用屏幕覆盖模式，始终显示在最上层
            var canvasObject = new GameObject("mlpNativeMenuTextLayer");
            canvasObject.transform.SetParent(menuRoot, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            // 3. 设置画布缩放器为固定像素大小模式（不自动缩放，由 RefreshLayout 手动控制）
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            // 4. 创建视口根节点——所有文字元素都放在这个节点下，参考尺寸 800x480
            viewportRoot = new GameObject("ViewportRoot").AddComponent<RectTransform>();
            viewportRoot.SetParent(canvas.transform, false);
            viewportRoot.anchorMin = new Vector2(0.5f, 0.5f);
            viewportRoot.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRoot.pivot = new Vector2(0.5f, 0.5f);
            viewportRoot.sizeDelta = ReferenceSize;
            viewportRoot.localScale = Vector3.one;
            viewportRoot.anchoredPosition = Vector2.zero;

            // 5. 将自己注册为当前活跃的文字层（全局只有一个）
            Active = this;
        }

        /// <summary>
        /// 检查给定的 Transform 是否属于此文字层的菜单根节点。用于判断某个文字元素是否由该层创建。
        /// </summary>
        public bool Owns(Transform parent)
        {
            return parent != null && parent == menuRoot;
        }

        /// <summary>
        /// 屏幕尺寸变化时重新计算画布缩放，使 800x480 参考布局保持居中和正确大小。
        /// </summary>
        public void RefreshLayout(int screenWidth, int screenHeight)
        {
            // 1. 如果视口根节点不存在，直接返回
            if (viewportRoot == null)
            {
                return;
            }

            // 2. 确保屏幕尺寸至少为 1 像素
            var width = Mathf.Max(1, screenWidth);
            var height = Mathf.Max(1, screenHeight);
            // 3. 计算缩放比例：取宽高中较小值，确保 800x480 的参考布局完整显示
            var scale = Mathf.Min(width / ReferenceWidth, height / ReferenceHeight);
            // 4. 保持参考尺寸不变，只调整缩放比例，使文字在不同屏幕上大小一致
            viewportRoot.sizeDelta = ReferenceSize;
            viewportRoot.localScale = new Vector3(scale, scale, 1f);
            viewportRoot.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 显示或隐藏整个文字层。隐藏时不绘制任何菜单文字。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 在指定像素位置创建一个新的 TextMeshPro 文字元素。返回 TMP_Text 组件以便后续更新文字内容。x/y 坐标基于 800x480 参考坐标系。
        /// </summary>
        public TMP_Text CreateText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            mlpTextStyle style)
        {
            // 1. 创建新的 GameObject 并添加 RectTransform，挂载到视口根节点下
            var textObject = new GameObject(name);
            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.SetParent(viewportRoot, false);
            // 2. 设置锚点为中心，根据对齐方式设置 pivot（如左上角、居中等）
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = AnchorToPivot(anchor);
            // 3. 将像素坐标（左上角为原点）转换为画布坐标（中心为原点）
            rectTransform.anchoredPosition = PixelToViewportPosition(x, y);
            // 4. 根据文字行数估算所需高度
            rectTransform.sizeDelta = new Vector2(ReferenceWidth, EstimateHeight(text, fontSize));

            // 5. 添加 TextMeshPro 文字组件，配置基础属性
            var textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.raycastTarget = false;
            textComponent.richText = false;
            textComponent.enableWordWrapping = false;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            textComponent.extraPadding = true;
            textComponent.alignment = AnchorToAlignment(anchor);
            textComponent.color = color;
            textComponent.fontSize = fontSize;
            textComponent.text = text;

            // 6. 应用字体样式（字体族、描边等）
            ApplyStyle(textComponent, style, fontSize);
            return textComponent;
        }

        public static void SetPixelPosition(RectTransform rectTransform, float x, float y)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = PixelToViewportPosition(x, y);
        }

        /// <summary>
        /// 销毁画布及所有文字元素。在离开菜单时调用。
        /// </summary>
        public void Dispose()
        {
            if (Active == this)
            {
                Active = null;
            }

            if (canvas != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(canvas.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }
        }

        /// <summary>
        /// 为文字组件应用字体样式（字体族、描边等）。不同样式使用不同字体，例如标题用 Impact，正文用 Rajdhani。
        /// </summary>
        private static void ApplyStyle(TMP_Text textComponent, mlpTextStyle style, int fontSize)
        {
            var resolvedStyle = mlpTextStyles.Resolve(style, fontSize);
            var fontAsset = mlpTmpFontCache.Get(resolvedStyle.FontKind);
            if (fontAsset != null)
            {
                textComponent.font = fontAsset;
            }

            textComponent.outlineWidth = resolvedStyle.OutlineColor.HasValue && resolvedStyle.OutlinePixels > 0f
                ? Mathf.Clamp01(resolvedStyle.OutlinePixels * 0.14f)
                : 0f;
            textComponent.outlineColor = resolvedStyle.OutlineColor ?? Color.clear;
        }

        /// <summary>
        /// 将像素坐标（0,0 = 左上角）转换为画布锚点坐标（基于中心点）。
        /// </summary>
        private static Vector2 PixelToViewportPosition(float x, float y)
        {
            return new Vector2(
                x - ReferenceWidth * 0.5f,
                ReferenceHeight * 0.5f - y);
        }

        /// <summary>
        /// 根据行数和字号估算文字元素所需的高度。
        /// </summary>
        private static float EstimateHeight(string text, int fontSize)
        {
            var lineCount = 1;
            if (!string.IsNullOrEmpty(text))
            {
                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                    {
                        lineCount++;
                    }
                }
            }

            return Mathf.Max(fontSize * 2.2f, fontSize * 1.45f * lineCount + 8f);
        }

        /// <summary>
        /// 将 TextAnchor（如 UpperLeft、MiddleCenter）转换为 RectTransform 的 pivot 向量。
        /// </summary>
        private static Vector2 AnchorToPivot(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return new Vector2(0f, 1f);
                case TextAnchor.UpperCenter:
                    return new Vector2(0.5f, 1f);
                case TextAnchor.UpperRight:
                    return new Vector2(1f, 1f);
                case TextAnchor.MiddleLeft:
                    return new Vector2(0f, 0.5f);
                case TextAnchor.MiddleRight:
                    return new Vector2(1f, 0.5f);
                case TextAnchor.LowerLeft:
                    return new Vector2(0f, 0f);
                case TextAnchor.LowerCenter:
                    return new Vector2(0.5f, 0f);
                case TextAnchor.LowerRight:
                    return new Vector2(1f, 0f);
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// 将 TextAnchor 转换为对应的 TextMeshPro 对齐选项。
        /// </summary>
        private static TextAlignmentOptions AnchorToAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }
    }

    /// <summary>TextMeshPro 字体缓存：内部使用的字体资源缓存，避免重复加载同一字体。</summary>
    internal static class mlpTmpFontCache
    {
        private static readonly Dictionary<mlpFontKind, TMP_FontAsset> FontAssets = new Dictionary<mlpFontKind, TMP_FontAsset>();

        /// <summary>
        /// 获取指定字体类型的缓存 TextMeshPro 字体资源。若尚未缓存，则尝试加载预打包的 SDF 字体，或从源字体动态创建。
        /// </summary>
        public static TMP_FontAsset Get(mlpFontKind fontKind)
        {
            if (FontAssets.TryGetValue(fontKind, out var cached) && cached != null)
            {
                return cached;
            }

            var bundledFontAsset = Resources.Load<TMP_FontAsset>(GetBundledFontAssetPath(fontKind));
            if (bundledFontAsset != null)
            {
                FontAssets[fontKind] = bundledFontAsset;
                return bundledFontAsset;
            }

            var sourceFont = mlpFontCache.Get(fontKind, 96);
            if (sourceFont == null)
            {
                return null;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                96,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset != null)
            {
                fontAsset.name = $"{sourceFont.name}_TMP";
                fontAsset.hideFlags = HideFlags.HideAndDontSave;
            }

            FontAssets[fontKind] = fontAsset;
            return fontAsset;
        }

        /// <summary>
        /// 返回与指定字体类型对应的预构建 TMP 字体资源的 Resources 路径。
        /// </summary>
        private static string GetBundledFontAssetPath(mlpFontKind fontKind)
        {
            switch (fontKind)
            {
                case mlpFontKind.AgencyBold:
                    return "mlp/Fonts/TMP/AgencyBold SDF";
                case mlpFontKind.CfCrackBold:
                    return "mlp/Fonts/TMP/CfCrackBold SDF";
                case mlpFontKind.Griffy:
                    return "mlp/Fonts/TMP/Griffy-Regular SDF";
                case mlpFontKind.Impact2:
                    return "mlp/Fonts/TMP/Impact2 SDF";
                case mlpFontKind.RajdhaniBold:
                    return "mlp/Fonts/TMP/Rajdhani-Bold SDF";
                case mlpFontKind.RajdhaniSemiBold:
                    return "mlp/Fonts/TMP/Rajdhani-SemiBold SDF";
                default:
                    return "mlp/Fonts/TMP/Impact SDF";
            }
        }
    }
}
