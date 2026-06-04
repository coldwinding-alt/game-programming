// 菜单界面的文字渲染层
// 用 TextMeshPro 在屏幕上绘制菜单文字，自动根据屏幕大小缩放，保证文字在不同分辨率下都清晰好看。

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace rimrush
{
    public sealed class rimrushNativeMenuTextLayer
    {
        private const float ReferenceWidth = rimrushConstants.Width;
        private const float ReferenceHeight = 480f;
        private static readonly Vector2 ReferenceSize = new Vector2(ReferenceWidth, ReferenceHeight);

        private readonly Transform menuRoot;
        private readonly Canvas canvas;
        private readonly RectTransform viewportRoot;

        public static rimrushNativeMenuTextLayer Active { get; private set; }

        /// <summary>
        /// Create a full-screen canvas for drawing menu text using TextMeshPro.
        /// The canvas uses a fixed 800x480 reference resolution so text positions stay consistent.
        /// </summary>
        public rimrushNativeMenuTextLayer(Transform menuRoot)
        {
            this.menuRoot = menuRoot;

            var canvasObject = new GameObject("rimrushNativeMenuTextLayer");
            canvasObject.transform.SetParent(menuRoot, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            viewportRoot = new GameObject("ViewportRoot").AddComponent<RectTransform>();
            viewportRoot.SetParent(canvas.transform, false);
            viewportRoot.anchorMin = new Vector2(0.5f, 0.5f);
            viewportRoot.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRoot.pivot = new Vector2(0.5f, 0.5f);
            viewportRoot.sizeDelta = ReferenceSize;
            viewportRoot.localScale = Vector3.one;
            viewportRoot.anchoredPosition = Vector2.zero;

            Active = this;
        }

        /// <summary>
        /// Check if the given transform belongs to this text layer's menu root.
        /// Used to figure out if a text element was created by this layer.
        /// </summary>
        public bool Owns(Transform parent)
        {
            return parent != null && parent == menuRoot;
        }

        /// <summary>
        /// Recalculate the canvas scale when the screen size changes.
        /// This keeps the 800x480 reference layout centered and properly sized.
        /// </summary>
        public void RefreshLayout(int screenWidth, int screenHeight)
        {
            if (viewportRoot == null)
            {
                return;
            }

            var width = Mathf.Max(1, screenWidth);
            var height = Mathf.Max(1, screenHeight);
            var scale = Mathf.Min(width / ReferenceWidth, height / ReferenceHeight);
            viewportRoot.sizeDelta = ReferenceSize;
            viewportRoot.localScale = new Vector3(scale, scale, 1f);
            viewportRoot.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Show or hide the entire text layer. When hidden, no menu text is drawn.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Create a new TextMeshPro text element at the given pixel position.
        /// Returns the TMP_Text component so you can update the text later.
        /// x/y are in the 800x480 reference coordinate space.
        /// </summary>
        public TMP_Text CreateText(
            string name,
            string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
            rimrushTextStyle style)
        {
            var textObject = new GameObject(name);
            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.SetParent(viewportRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = AnchorToPivot(anchor);
            rectTransform.anchoredPosition = PixelToViewportPosition(x, y);
            rectTransform.sizeDelta = new Vector2(ReferenceWidth, EstimateHeight(text, fontSize));

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
        /// Destroy the canvas and all text elements. Call this when leaving the menu.
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
        /// Apply a font style (font family, outline, etc) to a text component.
        /// Different styles use different fonts — for example, titles use Impact, body text uses Rajdhani.
        /// </summary>
        private static void ApplyStyle(TMP_Text textComponent, rimrushTextStyle style, int fontSize)
        {
            var resolvedStyle = rimrushTextStyles.Resolve(style, fontSize);
            var fontAsset = rimrushTmpFontCache.Get(resolvedStyle.FontKind);
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
        /// Convert pixel coordinates (0,0 = top-left) to the canvas anchored position (center-based).
        /// </summary>
        private static Vector2 PixelToViewportPosition(float x, float y)
        {
            return new Vector2(
                x - ReferenceWidth * 0.5f,
                ReferenceHeight * 0.5f - y);
        }

        /// <summary>
        /// Guess how tall a text element needs to be based on the number of lines and font size.
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
        /// Convert a TextAnchor (like UpperLeft, MiddleCenter) to a pivot vector for the RectTransform.
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
        /// Convert a TextAnchor to the matching TextMeshPro alignment option.
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

    internal static class rimrushTmpFontCache
    {
        private static readonly Dictionary<rimrushFontKind, TMP_FontAsset> FontAssets = new Dictionary<rimrushFontKind, TMP_FontAsset>();

        /// <summary>
        /// Get a cached TextMeshPro font asset for the given font kind.
        /// If not cached yet, tries to load a bundled SDF font, or creates one from the source font.
        /// </summary>
        public static TMP_FontAsset Get(rimrushFontKind fontKind)
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

            var sourceFont = rimrushFontCache.Get(fontKind, 96);
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
        /// Return the Resources path for the pre-built TMP font asset that matches the given font kind.
        /// </summary>
        private static string GetBundledFontAssetPath(rimrushFontKind fontKind)
        {
            switch (fontKind)
            {
                case rimrushFontKind.AgencyBold:
                    return "rimrush/Fonts/TMP/AgencyBold SDF";
                case rimrushFontKind.CfCrackBold:
                    return "rimrush/Fonts/TMP/CfCrackBold SDF";
                case rimrushFontKind.Griffy:
                    return "rimrush/Fonts/TMP/Griffy-Regular SDF";
                case rimrushFontKind.Impact2:
                    return "rimrush/Fonts/TMP/Impact2 SDF";
                case rimrushFontKind.RajdhaniBold:
                    return "rimrush/Fonts/TMP/Rajdhani-Bold SDF";
                case rimrushFontKind.RajdhaniSemiBold:
                    return "rimrush/Fonts/TMP/Rajdhani-SemiBold SDF";
                default:
                    return "rimrush/Fonts/TMP/Impact SDF";
            }
        }
    }
}
