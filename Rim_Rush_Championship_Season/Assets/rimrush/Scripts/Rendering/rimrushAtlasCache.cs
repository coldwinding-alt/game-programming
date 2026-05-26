using System.Collections.Generic;
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
    DisplayTitle,
    Subtitle
}

    public static class rimrushFontCache
    {
        private static readonly Dictionary<rimrushFontKind, Font> ResourceFonts = new Dictionary<rimrushFontKind, Font>();
        private static readonly Dictionary<string, Font> FallbackFonts = new Dictionary<string, Font>();
        private static readonly HashSet<rimrushFontKind> MissingResourceWarnings = new HashSet<rimrushFontKind>();

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
                        new[] { "Griffy", "Impact", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.AgencyBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Agency FB", "Bahnschrift SemiCondensed", "Impact", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.CfCrackBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "CfCrackBold", "Arial Black", "Impact", "Arial" },
                        fontSize);
                    break;
                case rimrushFontKind.Impact2:
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

public static class rimrushSharedMaterialCache
{
    private const string SpritesDefaultShaderName = "Sprites/Default";
    private static readonly Dictionary<int, Material> SpritesDefaultMaterials = new Dictionary<int, Material>();
    private static bool missingSpritesDefaultShaderLogged;
    private static Shader spritesDefaultShader;
    private static Material untexturedSpritesDefaultMaterial;

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
            case rimrushTextStyle.DisplayTitle:
                return Create(
                    rimrushFontKind.AgencyBold,
                    rimrushTextRasterProfile.Display,
                    new Color(0f, 0f, 0f, 0.72f),
                    fontSize >= 42 ? 0.95f : 0.8f);
            case rimrushTextStyle.Subtitle:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiMedium);
            default:
                return Create(rimrushFontKind.Impact2, rimrushTextRasterProfile.UiSmall);
        }
    }

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

        public static void ApplyPixelTransform(Transform transform, float x, float y, float z = 0f, float scale = 1f, float rotationDegrees = 0f)
        {
            transform.position = rimrushConstants.PixelToWorldSnapped(x, y, z);
            var scaled = rimrushConstants.UnitsPerPixel * scale;
            transform.localScale = new Vector3(scaled, scaled, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
        }

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
