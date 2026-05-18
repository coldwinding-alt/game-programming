using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLAtlasCache
    {
        private static BLAtlasCache instance;

        private readonly Dictionary<string, BLAtlas> atlases = new Dictionary<string, BLAtlas>();

        public static BLAtlasCache Instance => instance ?? (instance = new BLAtlasCache());

        public BLAtlas Gameplay => Get(BLAssets.Atlases.Gameplay);

        public BLAtlas Interface => Get(BLAssets.Atlases.Interface);

        public BLAtlas SkillFx => Get(BLAssets.Atlases.SkillFx);

        public BLAtlas Get(string atlasKey)
        {
            if (!atlases.TryGetValue(atlasKey, out var atlas))
            {
                atlas = new BLAtlas(atlasKey);
                atlases[atlasKey] = atlas;
            }

            return atlas;
        }
    }

    public sealed class BLAtlas
    {
        private readonly Texture2D texture;
        private readonly Dictionary<string, BLAtlasFrame> frames = new Dictionary<string, BLAtlasFrame>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public BLAtlas(string atlasKey)
        {
            texture = Resources.Load<Texture2D>($"BL2020/Atlases/{atlasKey}");
            var jsonAsset = Resources.Load<TextAsset>($"BL2020/Atlases/{atlasKey}");
            if (texture == null || jsonAsset == null)
            {
                Debug.LogError($"Missing atlas {atlasKey}");
                return;
            }

            var root = BLJson.AsDict(BLJson.Parse(jsonAsset.text));
            var rawFrames = BLJson.Dict(root, "frames");
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

                var frameDict = BLJson.AsDict(pair.Value);
                if (frameDict == null)
                {
                    continue;
                }

                frames[pair.Key] = BLAtlasFrame.FromJson(pair.Key, frameDict);
            }
        }

        public bool HasFrame(string frameName)
        {
            return frames.ContainsKey(frameName);
        }

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

        public BLAtlasFrame Frame(string frameName)
        {
            frames.TryGetValue(frameName, out var frame);
            return frame;
        }
    }

    public sealed class BLAtlasFrame
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

        public static BLAtlasFrame FromJson(string name, Dictionary<string, object> data)
        {
            var frame = BLJson.Dict(data, "frame");
            var spriteSource = BLJson.Dict(data, "spriteSourceSize");
            var source = BLJson.Dict(data, "sourceSize");
            return new BLAtlasFrame
            {
                Name = name,
                X = BLJson.Float(frame, "x"),
                Y = BLJson.Float(frame, "y"),
                W = BLJson.Float(frame, "w"),
                H = BLJson.Float(frame, "h"),
                Trimmed = BLJson.Bool(data, "trimmed"),
                SourceX = BLJson.Float(spriteSource, "x"),
                SourceY = BLJson.Float(spriteSource, "y"),
                SourceW = BLJson.Float(source, "w", BLJson.Float(frame, "w")),
                SourceH = BLJson.Float(source, "h", BLJson.Float(frame, "h"))
            };
        }

        public Vector2 Pivot(float anchorX, float anchorY)
        {
            var pivotX = anchorX * SourceW - SourceX;
            var pivotYFromTop = anchorY * SourceH - SourceY;
            return new Vector2(
                W <= 0f ? 0.5f : pivotX / W,
                H <= 0f ? 0.5f : 1f - pivotYFromTop / H);
        }
    }

public enum BLFontKind
{
    Impact,
    Impact2,
    CfCrackBold,
    AgencyBold,
    RajdhaniSemiBold,
    RajdhaniBold,
    Griffy
}

public enum BLTextStyle
{
    HudName,
    HudScore,
    HudTimer,
    HudPopup,
    TournamentBody,
    TournamentAccent,
    ButtonLabel,
    DisplayTitle,
    Subtitle
}

    public static class BLFontCache
    {
        private static readonly Dictionary<BLFontKind, Font> ResourceFonts = new Dictionary<BLFontKind, Font>();
        private static readonly Dictionary<string, Font> FallbackFonts = new Dictionary<string, Font>();
        private static readonly HashSet<BLFontKind> MissingResourceWarnings = new HashSet<BLFontKind>();

        public static Font Get(BLFontKind fontKind, int fontSize)
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
                case BLFontKind.RajdhaniSemiBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Rajdhani SemiBold", "Bahnschrift SemiBold", "Bahnschrift SemiCondensed", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case BLFontKind.RajdhaniBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Rajdhani Bold", "Bahnschrift Bold", "Bahnschrift SemiBold", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case BLFontKind.Griffy:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Griffy", "Impact", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case BLFontKind.AgencyBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Agency FB", "Bahnschrift SemiCondensed", "Impact", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case BLFontKind.CfCrackBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "CfCrackBold", "Arial Black", "Impact", "Arial" },
                        fontSize);
                    break;
                case BLFontKind.Impact2:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Impact2", "Impact", "Arial Black", "Arial" },
                        fontSize);
                    break;
                default:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Impact", "Arial Black", "Arial" },
                        fontSize);
                    break;
            }

            PrepareFont(font);
            FallbackFonts[cacheKey] = font;
            return font;
        }

        private static Font GetResourceFont(BLFontKind fontKind)
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

        private static string GetResourcePath(BLFontKind fontKind)
        {
            switch (fontKind)
            {
                case BLFontKind.RajdhaniSemiBold:
                    return "BL2020/Fonts/Rajdhani-SemiBold";
                case BLFontKind.RajdhaniBold:
                    return "BL2020/Fonts/Rajdhani-Bold";
                case BLFontKind.Griffy:
                    return "BL2020/Fonts/Griffy-Regular";
                case BLFontKind.AgencyBold:
                    return "BL2020/Fonts/AgencyBold";
                case BLFontKind.CfCrackBold:
                    return "BL2020/Fonts/CfCrackBold";
                case BLFontKind.Impact2:
                    return "BL2020/Fonts/Impact2";
                default:
                    return "BL2020/Fonts/Impact";
            }
        }

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

public static class BLFontMaterialCache
    {
        private const string OutlinedShaderName = "BasketballLegends2020/TextMeshOutlined";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static bool missingShaderLogged;
        private static Shader outlinedShader;

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

        private static string ColorKey(Color color)
        {
            var color32 = (Color32)color;
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
    }
}

internal enum BLTextRasterProfile
{
    UiSmall,
    UiMedium,
    Display,
    Score
}

internal struct BLResolvedTextStyle
{
    public BLFontKind FontKind;
    public Color? OutlineColor;
    public float OutlinePixels;
    public Color? ShadowColor;
    public Vector2? ShadowOffset;
    public BLTextRasterProfile RasterProfile;
}

internal static class BLTextStyles
{
    public static BLResolvedTextStyle Resolve(BLTextStyle style, int fontSize)
    {
        switch (style)
        {
            case BLTextStyle.HudName:
                return Create(
                    BLFontKind.RajdhaniSemiBold,
                    BLTextRasterProfile.UiMedium,
                    new Color32(0x04, 0x1A, 0x22, 0xE0),
                    fontSize >= 18 ? 0.65f : 0.5f);
            case BLTextStyle.HudScore:
                return Create(
                    BLFontKind.RajdhaniBold,
                    BLTextRasterProfile.Score,
                    new Color32(0x41, 0x10, 0x00, 0xEA),
                    fontSize >= 34 ? 1.15f : 0.95f);
            case BLTextStyle.HudTimer:
                return Create(
                    BLFontKind.RajdhaniSemiBold,
                    BLTextRasterProfile.UiMedium,
                    new Color32(0x10, 0x27, 0x1A, 0xD8),
                    0.55f);
            case BLTextStyle.HudPopup:
                return Create(
                    BLFontKind.Griffy,
                    BLTextRasterProfile.Display,
                    new Color32(0x24, 0x05, 0x47, 0xE8),
                    fontSize >= 54 ? 1.2f : 0.95f);
            case BLTextStyle.TournamentBody:
                return Create(BLFontKind.Impact2, BLTextRasterProfile.UiSmall);
            case BLTextStyle.TournamentAccent:
                return Create(BLFontKind.Impact2, BLTextRasterProfile.UiMedium);
            case BLTextStyle.ButtonLabel:
                return Create(
                    BLFontKind.Impact2,
                    BLTextRasterProfile.UiMedium,
                    new Color(0f, 0f, 0f, 0.35f),
                    fontSize >= 28 ? 0.75f : 0.55f);
            case BLTextStyle.DisplayTitle:
                return Create(
                    BLFontKind.AgencyBold,
                    BLTextRasterProfile.Display,
                    new Color(0f, 0f, 0f, 0.72f),
                    fontSize >= 42 ? 0.95f : 0.8f);
            case BLTextStyle.Subtitle:
                return Create(BLFontKind.Impact2, BLTextRasterProfile.UiMedium);
            default:
                return Create(BLFontKind.Impact2, BLTextRasterProfile.UiSmall);
        }
    }

    private static BLResolvedTextStyle Create(
        BLFontKind fontKind,
        BLTextRasterProfile rasterProfile,
        Color? outlineColor = null,
        float outlinePixels = 0f,
        Color? shadowColor = null,
        Vector2? shadowOffset = null)
    {
        return new BLResolvedTextStyle
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

public static class BLRender
{
        public static GameObject Sprite(string name, BLAtlas atlas, string frame, float x, float y, float anchorX, float anchorY, int sortingOrder, Transform parent = null)
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
        BLTextStyle style)
    {
        var resolvedStyle = BLTextStyles.Resolve(style, fontSize);
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
        BLFontKind fontKind = BLFontKind.Impact,
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
        BLFontKind fontKind,
        Color? outlineColor,
        float outlinePixels,
        Color? shadowColor,
        Vector2? shadowOffset,
        BLTextRasterProfile rasterProfile)
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
            mesh.font = BLFontCache.Get(fontKind, mesh.fontSize);
            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null)
            {
                mesh.font.RequestCharactersInTexture(text, mesh.fontSize, FontStyle.Normal);
                renderer.sharedMaterial = BLFontMaterialCache.Get(
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

        public static void ApplyPixelTransform(Transform transform, float x, float y, float z = 0f, float scale = 1f, float rotationDegrees = 0f)
        {
            transform.position = BLConstants.PixelToWorldSnapped(x, y, z);
            var scaled = BLConstants.UnitsPerPixel * scale;
            transform.localScale = new Vector3(scaled, scaled, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
        }

    private static BLTextRasterProfile ResolveRawRasterProfile(int fontSize, Color? outlineColor, float outlinePixels, Color? shadowColor)
    {
        var hasOutline = outlineColor.HasValue && outlineColor.Value.a > 0f && outlinePixels > 0f;
        var hasShadow = shadowColor.HasValue && shadowColor.Value.a > 0f;
        if (fontSize >= 40)
        {
            return BLTextRasterProfile.Display;
        }

        if (hasOutline || hasShadow)
        {
            return fontSize <= 18 ? BLTextRasterProfile.UiSmall : BLTextRasterProfile.UiMedium;
        }

        return fontSize <= 18 ? BLTextRasterProfile.UiSmall : BLTextRasterProfile.UiMedium;
    }

    private static int GetFontRasterSize(int fontSize, BLTextRasterProfile rasterProfile)
    {
        float multiplier;
        switch (rasterProfile)
        {
            case BLTextRasterProfile.UiSmall:
                multiplier =
                    fontSize <= 12 ? 10f :
                    fontSize <= 18 ? 9f :
                    8f;
                break;
            case BLTextRasterProfile.UiMedium:
                multiplier =
                    fontSize <= 18 ? 8f :
                    fontSize <= 28 ? 7f :
                    6f;
                break;
            case BLTextRasterProfile.Score:
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
    }
}
