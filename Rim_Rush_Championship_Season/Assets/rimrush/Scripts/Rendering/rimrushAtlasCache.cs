// 图集、字体和材质缓存 / 加载并缓存游戏用到的纹理图集（把很多小图合成一张大图来提高性能）、字体和材质球。还提供渲染辅助工具，比如创建精灵、绘制文字、应用像素坐标变换。是整个游戏渲染系统的基础。

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushAtlasCache
    {
        private static rimrushAtlasCache instance;

        private readonly Dictionary<string, rimrushAtlas> atlases = new Dictionary<string, rimrushAtlas>();

        public static rimrushAtlasCache Instance => instance ?? (instance = new rimrushAtlasCache());

        public rimrushAtlas Gameplay => Get(rimrushAssets.Atlases.Gameplay);

        public rimrushAtlas Interface => Get(rimrushAssets.Atlases.Interface);

        public rimrushAtlas SkillFx => Get(rimrushAssets.Atlases.SkillFx);

        /// <summary>
        /// Get or create a cached atlas by its resource key.
        /// </summary>
        public rimrushAtlas Get(string atlasKey)
        {
            if (!atlases.TryGetValue(atlasKey, out var atlas))
            {
                atlas = new rimrushAtlas(atlasKey);
                atlases[atlasKey] = atlas;
            }

            return atlas;
        }
    }

    public sealed class rimrushAtlas
    {
        private readonly Texture2D texture;
        private readonly Dictionary<string, rimrushAtlasFrame> frames = new Dictionary<string, rimrushAtlasFrame>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Load a texture atlas and parse its sprite frame metadata from JSON.
        /// </summary>
        public rimrushAtlas(string atlasKey)
        {
            texture = Resources.Load<Texture2D>($"rimrush/Atlases/{atlasKey}");
            var jsonAsset = Resources.Load<TextAsset>($"rimrush/Atlases/{atlasKey}");
            if (texture == null || jsonAsset == null)
            {
                Debug.LogError($"Missing atlas {atlasKey}");
                return;
            }

            var root = rimrushJson.AsDict(rimrushJson.Parse(jsonAsset.text));
            var rawFrames = rimrushJson.Dict(root, "frames");
            if (rawFrames == null)
            {
                return;
            }

            foreach (var pair in rawFrames)
            {
                if (pair.Key == "meta")
                {
                    continue;
                }

                var frameDict = rimrushJson.AsDict(pair.Value);
                if (frameDict == null)
                {
                    continue;
                }

                frames[pair.Key] = rimrushAtlasFrame.FromJson(pair.Key, frameDict);
            }
        }

        /// <summary>
        /// Check whether the atlas contains a sprite frame with the given name.
        /// </summary>
        public bool HasFrame(string frameName)
        {
            return frames.ContainsKey(frameName);
        }

        /// <summary>
        /// Create or return a cached Sprite for the named atlas frame, using the given anchor point.
        /// </summary>
        public Sprite Sprite(string frameName, float anchorX = 0.5f, float anchorY = 0.5f)
        {
            if (texture == null || !frames.TryGetValue(frameName, out var frame))
            {
                Debug.LogWarning($"Missing atlas frame {frameName}");
                return null;
            }

            var cacheKey = $"{frameName}|{anchorX:0.###}|{anchorY:0.###}";
            if (spriteCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var rect = new Rect(frame.X, texture.height - frame.Y - frame.H, frame.W, frame.H);
            var pivot = frame.Pivot(anchorX, anchorY);
            var sprite = UnityEngine.Sprite.Create(texture, rect, pivot, 1f, 0, SpriteMeshType.FullRect);
            sprite.name = frameName;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Return the raw atlas frame metadata for the given frame name.
        /// </summary>
        public rimrushAtlasFrame Frame(string frameName)
        {
            frames.TryGetValue(frameName, out var frame);
            return frame;
        }
    }

    public sealed class rimrushAtlasFrame
    {
        public string Name;
        public float X;
        public float Y;
        public float W;
        public float H;
        public bool Trimmed;
        public float SourceX;
        public float SourceY;
        public float SourceW;
        public float SourceH;

        /// <summary>
        /// Parse atlas frame metadata from a JSON dictionary.
        /// </summary>
        public static rimrushAtlasFrame FromJson(string name, Dictionary<string, object> data)
        {
            var frame = rimrushJson.Dict(data, "frame");
            var spriteSource = rimrushJson.Dict(data, "spriteSourceSize");
            var source = rimrushJson.Dict(data, "sourceSize");
            return new rimrushAtlasFrame
            {
                Name = name,
                X = rimrushJson.Float(frame, "x"),
                Y = rimrushJson.Float(frame, "y"),
                W = rimrushJson.Float(frame, "w"),
                H = rimrushJson.Float(frame, "h"),
                Trimmed = rimrushJson.Bool(data, "trimmed"),
                SourceX = rimrushJson.Float(spriteSource, "x"),
                SourceY = rimrushJson.Float(spriteSource, "y"),
                SourceW = rimrushJson.Float(source, "w", rimrushJson.Float(frame, "w")),
                SourceH = rimrushJson.Float(source, "h", rimrushJson.Float(frame, "h"))
            };
        }

        /// <summary>
        /// Calculate the sprite pivot point from the anchor values and source frame dimensions.
        /// </summary>
        public Vector2 Pivot(float anchorX, float anchorY)
        {
            var pivotX = anchorX * SourceW - SourceX;
            var pivotYFromTop = anchorY * SourceH - SourceY;
            return new Vector2(
                W <= 0f ? 0.5f : pivotX / W,
                H <= 0f ? 0.5f : 1f - pivotYFromTop / H);
        }
    }

public enum rimrushFontKind
{
    Impact,
    Impact2,
    CfCrackBold,
    AgencyBold,
    RajdhaniSemiBold,
    RajdhaniBold,
    Griffy
}

public enum rimrushTextStyle
{
    HudName,
    HudScore,
    HudTimer,
    HudPopup,
    TournamentBody,
    TournamentAccent,
    ButtonLabel,
    LinkLabel,
    DisplayTitle,
    Subtitle,
    StoryScrollTitle,
    StoryScrollBody
}

    public static class rimrushFontCache
    {
        private static readonly Dictionary<rimrushFontKind, Font> ResourceFonts = new Dictionary<rimrushFontKind, Font>();
        private static readonly Dictionary<string, Font> FallbackFonts = new Dictionary<string, Font>();
        private static readonly HashSet<rimrushFontKind> MissingResourceWarnings = new HashSet<rimrushFontKind>();

        /// <summary>
        /// Return a Font for the given style, loading from Resources or falling back to OS fonts.
        /// </summary>
        public static Font Get(rimrushFontKind fontKind, int fontSize)
        {
            var resourceFont = GetResourceFont(fontKind);
            if (resourceFont != null)
            {
                return resourceFont;
            }

            var cacheKey = $"{fontKind}:{fontSize}";
            if (FallbackFonts.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            Font font;
            switch (fontKind)
            {
                case rimrushFontKind.RajdhaniSemiBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Rajdhani SemiBold", "Bahnschrift SemiBold", "Bahnschrift SemiCondensed", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.RajdhaniBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Rajdhani Bold", "Bahnschrift Bold", "Bahnschrift SemiBold", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.Griffy:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Griffy", "Bungee", "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.AgencyBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Barlow Condensed Bold", "Barlow Condensed", "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.CfCrackBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Bungee", "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.Impact2:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                default:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
            }

            PrepareFont(font);
            FallbackFonts[cacheKey] = font;
            return font;
        }

        /// <summary>
        /// Try to load the bundled font resource for a given font kind.
        /// </summary>
        private static Font GetResourceFont(rimrushFontKind fontKind)
        {
            if (ResourceFonts.TryGetValue(fontKind, out var cached))
            {
                return cached;
            }

            var font = Resources.Load<Font>(GetResourcePath(fontKind));
            if (font == null)
            {
                if (MissingResourceWarnings.Add(fontKind))
                {
                    Debug.LogWarning($"Missing bundled font resource for {fontKind}, falling back to OS fonts.");
                }

                return null;
            }

            PrepareFont(font);
            ResourceFonts[fontKind] = font;
            return font;
        }

        /// <summary>
        /// Return the Resources folder path for a bundled font.
        /// </summary>
        private static string GetResourcePath(rimrushFontKind fontKind)
        {
            switch (fontKind)
            {
                case rimrushFontKind.RajdhaniSemiBold:
                    return "rimrush/Fonts/Rajdhani-SemiBold";
                case rimrushFontKind.RajdhaniBold:
                    return "rimrush/Fonts/Rajdhani-Bold";
                case rimrushFontKind.Griffy:
                    return "rimrush/Fonts/Griffy-Regular";
                case rimrushFontKind.AgencyBold:
                    return "rimrush/Fonts/AgencyBold";
                case rimrushFontKind.CfCrackBold:
                    return "rimrush/Fonts/CfCrackBold";
                case rimrushFontKind.Impact2:
                    return "rimrush/Fonts/Impact2";
                default:
                    return "rimrush/Fonts/Impact";
            }
        }

        /// <summary>
        /// Set up font texture settings for crisp pixel-art rendering.
        /// </summary>
        private static void PrepareFont(Font font)
        {
            if (font == null || font.material == null || font.material.mainTexture == null)
            {
                return;
            }

            font.material.mainTexture.wrapMode = TextureWrapMode.Clamp;
            // Keep retro UI text crisp instead of letting font atlas sampling soften edges.
            font.material.mainTexture.filterMode = FilterMode.Point;
        }
    }

public static class rimrushFontMaterialCache
    {
        private const string OutlinedShaderName = "rimrush/TextMeshOutlined";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static bool missingShaderLogged;
        private static Shader outlinedShader;

        /// <summary>
        /// Return a Material with outline and shadow settings applied, using the custom outlined shader.
        /// </summary>
        public static Material Get(
            Font font,
            Color? outlineColor,
            float outlinePixels,
            Color? shadowColor,
            Vector2? shadowOffset,
            float rasterScale)
        {
            if (font == null || font.material == null)
            {
                return null;
            }

            var baseMaterial = font.material;
            var mainTexture = baseMaterial.mainTexture;
            if (mainTexture == null)
            {
                return baseMaterial;
            }

            var hasOutline = outlineColor.HasValue && outlineColor.Value.a > 0f && outlinePixels > 0f;
            var hasShadow = shadowColor.HasValue && shadowColor.Value.a > 0f;
            if (!hasOutline && !hasShadow)
            {
                return baseMaterial;
            }

            if (outlinedShader == null)
            {
                outlinedShader = Shader.Find(OutlinedShaderName);
            }

            if (outlinedShader == null)
            {
                if (!missingShaderLogged)
                {
                    Debug.LogWarning($"Could not find shader '{OutlinedShaderName}', using default font material.");
                    missingShaderLogged = true;
                }

                return baseMaterial;
            }

            var resolvedOutlineColor = hasOutline ? outlineColor.Value : Color.clear;
            var resolvedShadowColor = hasShadow ? shadowColor.Value : Color.clear;
            var resolvedShadowOffset = hasShadow ? shadowOffset ?? new Vector2(0f, 2f) : Vector2.zero;
            var outlineTexels = hasOutline ? outlinePixels * rasterScale : 0f;
            var shadowTexels = hasShadow ? resolvedShadowOffset * rasterScale : Vector2.zero;

            var cacheKey =
                $"{font.GetInstanceID()}:{mainTexture.GetInstanceID()}:{ColorKey(resolvedOutlineColor)}:{outlineTexels:0.###}:{ColorKey(resolvedShadowColor)}:{shadowTexels.x:0.###}:{shadowTexels.y:0.###}";
            if (!Materials.TryGetValue(cacheKey, out var material))
            {
                material = new Material(outlinedShader)
                {
                    name = $"{font.name}_Outlined",
                    hideFlags = HideFlags.HideAndDontSave,
                    renderQueue = baseMaterial.renderQueue
                };
                Materials[cacheKey] = material;
            }

            material.SetTexture("_MainTex", mainTexture);
            material.SetColor("_OutlineColor", resolvedOutlineColor);
            material.SetFloat("_OutlineWidth", outlineTexels);
            material.SetColor("_ShadowColor", resolvedShadowColor);
            material.SetVector("_ShadowOffset", new Vector4(shadowTexels.x, shadowTexels.y, 0f, 0f));
            return material;
        }

        /// <summary>
        /// Build a hex color string for material cache keys.
        /// </summary>
        private static string ColorKey(Color color)
        {
            var color32 = (Color32)color;
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
    }
}

public static class rimrushSharedMaterialCache
{
    private const string SpritesDefaultShaderName = "Sprites/Default";
    private static readonly Dictionary<int, Material> SpritesDefaultMaterials = new Dictionary<int, Material>();
    private static bool missingSpritesDefaultShaderLogged;
    private static Shader spritesDefaultShader;
    private static Material untexturedSpritesDefaultMaterial;

    /// <summary>
    /// Return a shared Sprites/Default material for the given texture.
    /// </summary>
    public static Material GetSpritesDefault(Texture mainTexture = null)
    {
        var shader = GetSpritesDefaultShader();
        if (shader == null)
        {
            return null;
        }

        if (mainTexture == null)
        {
            if (untexturedSpritesDefaultMaterial == null)
            {
                untexturedSpritesDefaultMaterial = CreateSharedMaterial(shader, "SpritesDefault_Shared", null);
            }

            return untexturedSpritesDefaultMaterial;
        }

        var textureKey = mainTexture.GetInstanceID();
        if (!SpritesDefaultMaterials.TryGetValue(textureKey, out var material))
        {
            material = CreateSharedMaterial(shader, $"{mainTexture.name}_SpritesDefault", mainTexture);
            SpritesDefaultMaterials[textureKey] = material;
        }

        return material;
    }

    /// <summary>
    /// Find and cache the Sprites/Default shader.
    /// </summary>
    private static Shader GetSpritesDefaultShader()
    {
        if (spritesDefaultShader == null)
        {
            spritesDefaultShader = Shader.Find(SpritesDefaultShaderName);
        }

        if (spritesDefaultShader == null && !missingSpritesDefaultShaderLogged)
        {
            Debug.LogWarning($"Could not find shader '{SpritesDefaultShaderName}', runtime shared sprite materials will be unavailable.");
            missingSpritesDefaultShaderLogged = true;
        }

        return spritesDefaultShader;
    }

    /// <summary>
    /// Create a new hide-and-dont-save material from the given shader and texture.
    /// </summary>
    private static Material CreateSharedMaterial(Shader shader, string name, Texture mainTexture)
    {
        var material = new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave
        };
        material.mainTexture = mainTexture;
        return material;
    }
}

internal enum rimrushTextRasterProfile
{
    UiSmall,
    UiMedium,
    Display,
    Score
}

internal struct rimrushResolvedTextStyle
{
    public rimrushFontKind FontKind;
    public Color? OutlineColor;
    public float OutlinePixels;
    public Color? ShadowColor;
    public Vector2? ShadowOffset;
    public rimrushTextRasterProfile RasterProfile;
}

internal static class rimrushTextStyles
{
    /// <summary>
    /// Return the resolved font, outline, and shadow settings for a text style and font size.
    /// </summary>
    public static rimrushResolvedTextStyle Resolve(rimrushTextStyle style, int fontSize)
    {
        switch (style)
        {
            case rimrushTextStyle.HudName:
                return Create(
                    rimrushFontKind.RajdhaniSemiBold,
                    rimrushTextRasterProfile.UiMedium,
                    new Color32(0x04, 0x1A, 0x22, 0xE0),
                    fontSize >= 18 ? 0.65f : 0.5f);
            case rimrushTextStyle.HudScore:
                return Create(
                    rimrushFontKind.RajdhaniBold,
                    rimrushTextRasterProfile.Score,
                    new Color32(0x41, 0x10, 0x00, 0xEA),
                    fontSize >= 34 ? 1.15f : 0.95f);
            case rimrushTextStyle.HudTimer:
                return Create(
                    rimrushFontKind.RajdhaniSemiBold,
                    rimrushTextRasterProfile.UiMedium,
                    new Color32(0x10, 0x27, 0x1A, 0xD8),
                    0.55f);
            case rimrushTextStyle.HudPopup:
                return Create(
                    rimrushFontKind.Griffy,
                    rimrushTextRasterProfile.Display,
                    new Color32(0x24, 0x05, 0x47, 0xE8),
                    fontSize >= 54 ? 1.2f : 0.95f);
            case rimrushTextStyle.TournamentBody:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiSmall);
            case rimrushTextStyle.TournamentAccent:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiMedium);
            case rimrushTextStyle.ButtonLabel:
                return Create(
                    rimrushFontKind.Impact2,
                    rimrushTextRasterProfile.UiMedium,
                    new Color(0f, 0f, 0f, 0.35f),
                    fontSize >= 28 ? 0.75f : 0.55f);
            case rimrushTextStyle.LinkLabel:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiMedium);
            case rimrushTextStyle.DisplayTitle:
                return Create(
                    rimrushFontKind.AgencyBold,
                    rimrushTextRasterProfile.Display,
                    new Color(0f, 0f, 0f, 0.72f),
                    fontSize >= 42 ? 0.95f : 0.8f);
            case rimrushTextStyle.Subtitle:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiMedium);
            case rimrushTextStyle.StoryScrollTitle:
                return Create(
                    rimrushFontKind.Griffy,
                    rimrushTextRasterProfile.Display,
                    new Color32(0x54, 0x2C, 0x12, 0x38),
                    fontSize >= 26 ? 0.42f : 0.3f);
            case rimrushTextStyle.StoryScrollBody:
                return Create(
                    rimrushFontKind.RajdhaniSemiBold,
                    rimrushTextRasterProfile.UiMedium,
                    new Color32(0x43, 0x26, 0x10, 0x18),
                    0.16f);
            default:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiSmall);
        }
    }

    /// <summary>
    /// Build a resolved text style struct from its component values.
    /// </summary>
    private static rimrushResolvedTextStyle Create(
        rimrushFontKind fontKind,
        rimrushTextRasterProfile rasterProfile,
        Color? outlineColor = null,
        float outlinePixels = 0f,
        Color? shadowColor = null,
        Vector2? shadowOffset = null)
    {
        return new rimrushResolvedTextStyle
        {
            FontKind = fontKind,
            OutlineColor = outlineColor,
            OutlinePixels = outlinePixels,
            ShadowColor = shadowColor,
            ShadowOffset = shadowOffset,
            RasterProfile = rasterProfile
        };
    }
}

public static class rimrushRender
    {
        private const int PortraitBackplateTextureSize = 192;
        private static readonly Dictionary<string, Texture2D> PortraitBackplateTextures = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Create a GameObject with a SpriteRenderer displaying the given atlas frame.
        /// </summary>
        public static GameObject Sprite(string name, rimrushAtlas atlas, string frame, float x, float y, float anchorX, float anchorY, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = atlas.Sprite(frame, anchorX, anchorY);
            renderer.sortingOrder = sortingOrder;
            ApplyPixelTransform(go.transform, x, y);
            return go;
        }

        /// <summary>
        /// Create a GameObject with a SpriteRenderer displaying the given texture.
        /// </summary>
        public static GameObject Image(string name, Texture2D texture, float x, float y, float anchorX, float anchorY, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            var sprite = UnityEngine.Sprite.Create(texture, rect, new Vector2(anchorX, 1f - anchorY), 1f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            ApplyPixelTransform(go.transform, x, y);
            return go;
        }

        public static GameObject PortraitBackplate(
            string name,
            float x,
            float y,
            float diameterPixels,
            int sortingOrder,
            Transform parent,
            Color glowColor,
            Color fillColor,
            Color ringColor)
        {
            var root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            ApplyPixelTransform(root.transform, x, y);
            var safeDiameter = Mathf.Max(1f, diameterPixels);
            AddPortraitBackplateLayer(
                $"{name}_Glow",
                GetPortraitBackplateTexture("glow", 0f, 0.98f, 0.28f, true),
                safeDiameter * 1.22f,
                sortingOrder,
                root.transform,
                glowColor);
            AddPortraitBackplateLayer(
                $"{name}_Fill",
                GetPortraitBackplateTexture("fill", 0f, 0.86f, 0.025f, false),
                safeDiameter * 0.88f,
                sortingOrder + 1,
                root.transform,
                fillColor);
            AddPortraitBackplateLayer(
                $"{name}_Ring",
                GetPortraitBackplateTexture("ring", 0.78f, 0.96f, 0.02f, false),
                safeDiameter,
                sortingOrder + 2,
                root.transform,
                ringColor);
            AddPortraitBackplateLayer(
                $"{name}_InnerRing",
                GetPortraitBackplateTexture("inner_ring", 0.68f, 0.72f, 0.018f, false),
                safeDiameter * 0.92f,
                sortingOrder + 2,
                root.transform,
                new Color(0.55f, 1f, 0.95f, Mathf.Min(0.62f, ringColor.a * 0.62f)));
            return root;
        }

        private static void AddPortraitBackplateLayer(
            string name,
            Texture2D texture,
            float diameterPixels,
            int sortingOrder,
            Transform parent,
            Color tint)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = Vector3.zero;
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.localScale = new Vector3(
                diameterPixels / Mathf.Max(1f, texture.width),
                diameterPixels / Mathf.Max(1f, texture.height),
                1f);

            var sprite = UnityEngine.Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name;
            var renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint;
        }

        private static Texture2D GetPortraitBackplateTexture(string key, float innerRadius, float outerRadius, float softness, bool glow)
        {
            if (PortraitBackplateTextures.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var texture = new Texture2D(PortraitBackplateTextureSize, PortraitBackplateTextureSize, TextureFormat.RGBA32, false)
            {
                name = $"PortraitBackplate_{key}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[PortraitBackplateTextureSize * PortraitBackplateTextureSize];
            var center = (PortraitBackplateTextureSize - 1) * 0.5f;
            var radiusScale = Mathf.Max(1f, center);

            for (var y = 0; y < PortraitBackplateTextureSize; y++)
            {
                for (var x = 0; x < PortraitBackplateTextureSize; x++)
                {
                    var dx = (x - center) / radiusScale;
                    var dy = (y - center) / radiusScale;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var outerAlpha = 1f - Mathf.SmoothStep(outerRadius - softness, outerRadius, distance);
                    var innerAlpha = innerRadius <= 0f
                        ? 1f
                        : Mathf.SmoothStep(innerRadius, innerRadius + softness, distance);
                    var alpha = Mathf.Clamp01(outerAlpha * innerAlpha);
                    if (glow)
                    {
                        alpha *= Mathf.Clamp01(1f - distance / Mathf.Max(0.0001f, outerRadius));
                        alpha = Mathf.Pow(alpha, 1.55f);
                    }

                    pixels[y * PortraitBackplateTextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            PortraitBackplateTextures[key] = texture;
            return texture;
        }

    /// <summary>
    /// Create a TextMesh with the given style, position, and color.
    /// </summary>
    public static TextMesh Text(
        string name,
        string text,
            float x,
            float y,
            int fontSize,
            Color color,
            TextAnchor anchor,
        int sortingOrder,
        Transform parent,
        rimrushTextStyle style)
    {
        var resolvedStyle = rimrushTextStyles.Resolve(style, fontSize);
        return TextInternal(
            name,
            text,
            x,
            y,
            fontSize,
            color,
            anchor,
            sortingOrder,
            parent,
            resolvedStyle.FontKind,
            resolvedStyle.OutlineColor,
            resolvedStyle.OutlinePixels,
            resolvedStyle.ShadowColor,
            resolvedStyle.ShadowOffset,
            resolvedStyle.RasterProfile);
    }

    /// <summary>
    /// Create a TextMesh with explicit font and outline settings.
    /// </summary>
    public static TextMesh Text(
        string name,
        string text,
        float x,
        float y,
        int fontSize,
        Color color,
        TextAnchor anchor,
        int sortingOrder,
        Transform parent = null,
        rimrushFontKind fontKind = rimrushFontKind.Impact,
        Color? outlineColor = null,
        float outlinePixels = 0f,
        Color? shadowColor = null,
        Vector2? shadowOffset = null)
    {
        var rasterProfile = ResolveRawRasterProfile(fontSize, outlineColor, outlinePixels, shadowColor);
        return TextInternal(
            name,
            text,
            x,
            y,
            fontSize,
            color,
            anchor,
            sortingOrder,
            parent,
            fontKind,
            outlineColor,
            outlinePixels,
            shadowColor,
            shadowOffset,
            rasterProfile);
    }

    /// <summary>
    /// Build the TextMesh GameObject with font, material, and pixel transform.
    /// </summary>
    private static TextMesh TextInternal(
        string name,
        string text,
        float x,
        float y,
        int fontSize,
        Color color,
        TextAnchor anchor,
        int sortingOrder,
        Transform parent,
        rimrushFontKind fontKind,
        Color? outlineColor,
        float outlinePixels,
        Color? shadowColor,
        Vector2? shadowOffset,
        rimrushTextRasterProfile rasterProfile)
    {
        var go = new GameObject(name);
        if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = GetFontRasterSize(fontSize, rasterProfile);
        mesh.characterSize = Mathf.Max(0.1f, fontSize * 0.1f);
        mesh.anchor = anchor;
        mesh.alignment = AnchorToAlignment(anchor);
            mesh.color = color;
            mesh.font = rimrushFontCache.Get(fontKind, mesh.fontSize);
            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null)
            {
                mesh.font.RequestCharactersInTexture(text, mesh.fontSize, FontStyle.Normal);
                renderer.sharedMaterial = rimrushFontMaterialCache.Get(
                    mesh.font,
                    outlineColor,
                    outlinePixels,
                    shadowColor,
                    shadowOffset,
                    mesh.fontSize / (float)Mathf.Max(1, fontSize))
                    ?? mesh.font.material;
            }

            renderer.sortingOrder = sortingOrder;
        ApplyPixelTransform(go.transform, x, y);

        return mesh;
    }

    /// <summary>
    /// Create a TextMeshPro component with the given style, position, and color.
    /// </summary>
    public static TMP_Text TmpText(
        string name,
        string text,
        float x,
        float y,
        int fontSize,
        Color color,
        TextAnchor anchor,
        int sortingOrder,
        Transform parent,
        rimrushTextStyle style)
    {
        var resolvedStyle = rimrushTextStyles.Resolve(style, fontSize);
        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        var textComponent = go.AddComponent<TextMeshPro>();
        textComponent.text = text;
        textComponent.richText = false;
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.extraPadding = true;
        textComponent.alignment = AnchorToTmpAlignment(anchor);
        textComponent.color = color;
        var worldFontSize = fontSize * 10f;
        textComponent.fontSize = worldFontSize;
        textComponent.fontSizeMin = worldFontSize;
        textComponent.fontSizeMax = worldFontSize;
        textComponent.lineSpacing = -4f;

        var fontAsset = rimrushTmpFontCache.Get(resolvedStyle.FontKind);
        if (fontAsset != null)
        {
            textComponent.font = fontAsset;
            if (fontAsset.material != null)
            {
                textComponent.fontSharedMaterial = fontAsset.material;
            }
        }

        textComponent.outlineWidth = resolvedStyle.OutlineColor.HasValue && resolvedStyle.OutlinePixels > 0f
            ? Mathf.Clamp01(resolvedStyle.OutlinePixels * 0.08f)
            : 0f;
        textComponent.outlineColor = resolvedStyle.OutlineColor ?? Color.clear;
        textComponent.ForceMeshUpdate();

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
        }

        ApplyPixelTransform(go.transform, x, y);
        return textComponent;
    }

        /// <summary>
        /// Set a transform's world position from pixel coordinates, snapping to the screen pixel grid.
        /// </summary>
        public static void ApplyPixelTransform(Transform transform, float x, float y, float z = 0f, float scale = 1f, float rotationDegrees = 0f)
        {
            transform.position = rimrushConstants.PixelToWorldSnapped(x, y, z);
            var scaled = rimrushConstants.UnitsPerPixel * scale;
            transform.localScale = new Vector3(scaled, scaled, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
        }

    /// <summary>
    /// Pick the font raster profile based on font size and whether outline/shadow is used.
    /// </summary>
    private static rimrushTextRasterProfile ResolveRawRasterProfile(int fontSize, Color? outlineColor, float outlinePixels, Color? shadowColor)
    {
        var hasOutline = outlineColor.HasValue && outlineColor.Value.a > 0f && outlinePixels > 0f;
        var hasShadow = shadowColor.HasValue && shadowColor.Value.a > 0f;
        if (fontSize >= 40)
        {
            return rimrushTextRasterProfile.Display;
        }

        if (hasOutline || hasShadow)
        {
            return fontSize <= 18 ? rimrushTextRasterProfile.UiSmall : rimrushTextRasterProfile.UiMedium;
        }

        return fontSize <= 18 ? rimrushTextRasterProfile.UiSmall : rimrushTextRasterProfile.UiMedium;
    }

    /// <summary>
    /// Calculate the actual font texture raster size from the logical font size and raster profile.
    /// </summary>
    private static int GetFontRasterSize(int fontSize, rimrushTextRasterProfile rasterProfile)
    {
        float multiplier;
        switch (rasterProfile)
        {
            case rimrushTextRasterProfile.UiSmall:
                multiplier =
                    fontSize <= 12 ? 10f :
                    fontSize <= 18 ? 9f :
                    8f;
                break;
            case rimrushTextRasterProfile.UiMedium:
                multiplier =
                    fontSize <= 18 ? 8f :
                    fontSize <= 28 ? 7f :
                    6f;
                break;
            case rimrushTextRasterProfile.Score:
                multiplier =
                    fontSize <= 28 ? 7f :
                    fontSize <= 48 ? 6f :
                    5f;
                break;
            default:
                multiplier =
                    fontSize <= 24 ? 7f :
                    fontSize <= 36 ? 6f :
                    fontSize <= 64 ? 5f :
                    4f;
                break;
        }

        return Mathf.Clamp(Mathf.RoundToInt(fontSize * multiplier), 48, 512);
    }

        /// <summary>
        /// Convert a TextAnchor enum to the legacy TextMesh alignment enum.
        /// </summary>
        private static TextAlignment AnchorToAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                    return TextAlignment.Left;
                case TextAnchor.LowerRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                    return TextAlignment.Right;
                default:
                    return TextAlignment.Center;
            }
        }

        /// <summary>
        /// Convert a TextAnchor enum to TextMeshPro alignment options.
        /// </summary>
        private static TextAlignmentOptions AnchorToTmpAlignment(TextAnchor anchor)
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
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
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
}
