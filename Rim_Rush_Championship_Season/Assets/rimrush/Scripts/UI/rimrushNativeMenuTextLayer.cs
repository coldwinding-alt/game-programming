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

        public bool Owns(Transform parent)
        {
            return parent != null && parent == menuRoot;
        }

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

        private static Vector2 PixelToViewportPosition(float x, float y)
        {
            return new Vector2(
                x - ReferenceWidth * 0.5f,
                ReferenceHeight * 0.5f - y);
        }

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

        public static TMP_FontAsset Get(rimrushFontKind fontKind)
        {
            if (FontAssets.TryGetValue(fontKind, out var cached) && cached != null)
            {
                return cached;
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
    }
}
