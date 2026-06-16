// Text rendering layer for menu interface

// Use TextMeshPro to draw menu text on the screen and automatically scale it according to the screen size to ensure that the text is clear and beautiful at different resolutions.


using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace mlp
{
    /// <summary>
    /// Menu text rendering layer: Use TextMeshPro to draw menu text on the screen and automatically scale it according to the screen size to ensure that the text is clear and beautiful.
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
        /// Create a full-screen canvas and use TextMeshPro to draw menu text. The canvas uses a fixed reference resolution of 800x480 to ensure consistent text placement across screens.
        /// </summary>
        public mlpNativeMenuTextLayer(Transform menuRoot)
        {
            // 1. Save the reference to the menu root node (then determine whether a certain text belongs to this layer)

            this.menuRoot = menuRoot;

            // 2. Create a canvas - use screen overlay mode to always display on top
            var canvasObject = new GameObject("mlpNativeMenuTextLayer");
            canvasObject.transform.SetParent(menuRoot, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            // 3. Set the canvas scaler to fixed pixel size mode (no automatic scaling, manually controlled by RefreshLayout)

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            // 4. Create the viewport root node - all text elements are placed under this node, the reference size is 800x480

            viewportRoot = new GameObject("ViewportRoot").AddComponent<RectTransform>();
            viewportRoot.SetParent(canvas.transform, false);
            viewportRoot.anchorMin = new Vector2(0.5f, 0.5f);
            viewportRoot.anchorMax = new Vector2(0.5f, 0.5f);
            viewportRoot.pivot = new Vector2(0.5f, 0.5f);
            viewportRoot.sizeDelta = ReferenceSize;
            viewportRoot.localScale = Vector3.one;
            viewportRoot.anchoredPosition = Vector2.zero;

            // 5. Register yourself as the currently active text layer (there is only one globally)
            Active = this;
        }

        /// <summary>
        /// Checks whether the given Transform belongs to this text layer's menu root node. Used to determine whether a text element was created by this layer.
        /// </summary>
        public bool Owns(Transform parent)
        {
            return parent != null && parent == menuRoot;
        }

        /// <summary>
        /// Recalculate canvas scaling when screen size changes, keeping the 800x480 reference layout centered and correctly sized.

        /// </summary>
        public void RefreshLayout(int screenWidth, int screenHeight)
        {
            // 1. If the viewport root node does not exist, return directly
            if (viewportRoot == null)
            {
                return;
            }

            // 2. Make sure the screen size is at least 1 pixel

            var width = Mathf.Max(1, screenWidth);
            var height = Mathf.Max(1, screenHeight);
            // 3. Calculate the scaling ratio: take the smaller value of width and center to ensure that the 800x480 reference layout is fully displayed

            var scale = Mathf.Min(width / ReferenceWidth, height / ReferenceHeight);
            // 4. Keep the reference size unchanged and only adjust the scaling ratio to make the text the same size on different screens.
            viewportRoot.sizeDelta = ReferenceSize;
            viewportRoot.localScale = new Vector3(scale, scale, 1f);
            viewportRoot.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Show or hide the entire text layer. No menu text is drawn when hidden.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Creates a new TextMeshPro text element at the specified pixel position. Return the TMP_Text component for subsequent updates of text content. The x/y coordinates are based on the 800x480 reference coordinate system.
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
            // 1. Create a new GameObject and add a RectTransform, mount it under the root node of the viewport
            var textObject = new GameObject(name);
            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.SetParent(viewportRoot, false);
            // 2. Set the anchor point as the center and set the pivot according to the alignment (such as upper left corner, center, etc.)

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = AnchorToPivot(anchor);
            // 3. Convert pixel coordinates (the upper left corner is the origin) to canvas coordinates (the center is the origin)

            rectTransform.anchoredPosition = PixelToViewportPosition(x, y);
            // 4. Estimate the required height based on the number of lines of text

            rectTransform.sizeDelta = new Vector2(ReferenceWidth, EstimateHeight(text, fontSize));

            // 5. Add TextMeshPro text component and configure basic properties
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

            // 6. Apply font styles (font family, stroke, etc.)
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
        /// Destroy the canvas and all text elements. Called when leaving the menu.
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
        /// Apply font styles (font family, stroke, etc.) to text components. Use different fonts for different styles, such as Impact for titles and Rajdhani for body text.
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
        /// Convert pixel coordinates (0,0 = top left corner) to canvas anchor coordinates (based on center point).

        /// </summary>
        private static Vector2 PixelToViewportPosition(float x, float y)
        {
            return new Vector2(
                x - ReferenceWidth * 0.5f,
                ReferenceHeight * 0.5f - y);
        }

        /// <summary>
        /// Estimate the required height of the text element based on the number of lines and font size.
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
        /// Convert a TextAnchor (such as UpperLeft, MiddleCenter) to a RectTransform's pivot vector.

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
        /// Convert a TextAnchor to the corresponding TextMeshPro alignment option.
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

    /// <summary>TextMeshPro font cache: internally used font resource cache to avoid loading the same font repeatedly. </summary>
    internal static class mlpTmpFontCache
    {
        private static readonly Dictionary<mlpFontKind, TMP_FontAsset> FontAssets = new Dictionary<mlpFontKind, TMP_FontAsset>();

        /// <summary>
        /// Gets the cached TextMeshPro font resource for the specified font type. If not already cached, attempts to load a prepackaged SDF font, or dynamically create it from a source font.
        /// </summary>
        public static TMP_FontAsset Get(mlpFontKind fontKind)
        {
            // 1. Check whether the TMP font resource of this font type already exists in the cache
            if (FontAssets.TryGetValue(fontKind, out var cached) && cached != null)
            {
                return cached;
            }

            // 2. Prioritize loading of prepackaged SDF font resources (best performance)

            var bundledFontAsset = Resources.Load<TMP_FontAsset>(GetBundledFontAssetPath(fontKind));
            if (bundledFontAsset != null)
            {
                FontAssets[fontKind] = bundledFontAsset;
                return bundledFontAsset;
            }

            // 3. Prepackaged resources are missing and TMP font resources are dynamically created from system fonts.

            var sourceFont = mlpFontCache.Get(fontKind, 96);
            if (sourceFont == null)
            {
                return null;
            }

            // 4. Generate SDF font atlas from source fonts using TextMeshPro’s API

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

            // 5. Cache and return

            FontAssets[fontKind] = fontAsset;
            return fontAsset;
        }

        /// <summary>
        /// Returns the Resources path to the prebuilt TMP font resource corresponding to the specified font type.
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
