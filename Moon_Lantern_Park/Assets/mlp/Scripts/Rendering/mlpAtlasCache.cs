// 图集、字体和材质缓存 / 加载并缓存游戏用到的纹理图集（把很多小图合成一张大图来提高性能）、字体和材质球。还提供渲染辅助工具，比如创建精灵、绘制文字、应用像素坐标变换。是整个游戏渲染系统的基础。

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace mlp
{
    public sealed class mlpAtlasCache
    {
        private static mlpAtlasCache instance;

        private readonly Dictionary<string, mlpAtlas> atlases = new Dictionary<string, mlpAtlas>();

        public static mlpAtlasCache Instance => instance ?? (instance = new mlpAtlasCache());

        public mlpAtlas Gameplay => Get(mlpAssets.Atlases.Gameplay);

        public mlpAtlas Interface => Get(mlpAssets.Atlases.Interface);

        public mlpAtlas SkillFx => Get(mlpAssets.Atlases.SkillFx);

        /// <summary>
        /// 根据资源键获取或创建缓存的图集。
        /// </summary>
        public mlpAtlas Get(string atlasKey)
        {
            if (!atlases.TryGetValue(atlasKey, out var atlas))
            {
                atlas = new mlpAtlas(atlasKey);
                atlases[atlasKey] = atlas;
            }

            return atlas;
        }
    }

    public sealed class mlpAtlas
    {
        private readonly Texture2D texture;
        private readonly Dictionary<string, mlpAtlasFrame> frames = new Dictionary<string, mlpAtlasFrame>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 加载纹理图集并从 JSON 解析其精灵帧元数据。
        /// </summary>
        public mlpAtlas(string atlasKey)
        {
            texture = Resources.Load<Texture2D>($"mlp/Atlases/{atlasKey}");
            var jsonAsset = Resources.Load<TextAsset>($"mlp/Atlases/{atlasKey}");
            if (texture == null || jsonAsset == null)
            {
                Debug.LogError($"Missing atlas {atlasKey}");
                return;
            }

            var root = mlpJson.AsDict(mlpJson.Parse(jsonAsset.text));
            var rawFrames = mlpJson.Dict(root, "frames");
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

                var frameDict = mlpJson.AsDict(pair.Value);
                if (frameDict == null)
                {
                    continue;
                }

                frames[pair.Key] = mlpAtlasFrame.FromJson(pair.Key, frameDict);
            }
        }

        /// <summary>
        /// 检查图集是否包含指定名称的精灵帧。
        /// </summary>
        public bool HasFrame(string frameName)
        {
            return frames.ContainsKey(frameName);
        }

        /// <summary>
        /// 为指定名称的图集帧创建或返回缓存的 Sprite，使用给定的锚点。
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
        /// 返回指定帧名称的原始图集帧元数据。
        /// </summary>
        public mlpAtlasFrame Frame(string frameName)
        {
            frames.TryGetValue(frameName, out var frame);
            return frame;
        }
    }

    public sealed class mlpAtlasFrame
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
        /// 从 JSON 字典解析图集帧元数据。
        /// </summary>
        public static mlpAtlasFrame FromJson(string name, Dictionary<string, object> data)
        {
            var frame = mlpJson.Dict(data, "frame");
            var spriteSource = mlpJson.Dict(data, "spriteSourceSize");
            var source = mlpJson.Dict(data, "sourceSize");
            return new mlpAtlasFrame
            {
                Name = name,
                X = mlpJson.Float(frame, "x"),
                Y = mlpJson.Float(frame, "y"),
                W = mlpJson.Float(frame, "w"),
                H = mlpJson.Float(frame, "h"),
                Trimmed = mlpJson.Bool(data, "trimmed"),
                SourceX = mlpJson.Float(spriteSource, "x"),
                SourceY = mlpJson.Float(spriteSource, "y"),
                SourceW = mlpJson.Float(source, "w", mlpJson.Float(frame, "w")),
                SourceH = mlpJson.Float(source, "h", mlpJson.Float(frame, "h"))
            };
        }

        /// <summary>
        /// 根据锚点值和源帧尺寸计算精灵的轴心点。
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

public enum mlpFontKind
{
    Impact,
    Impact2,
    CfCrackBold,
    AgencyBold,
    RajdhaniSemiBold,
    RajdhaniBold,
    Griffy
}

public enum mlpTextStyle
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

    public static class mlpFontCache
    {
        private static readonly Dictionary<mlpFontKind, Font> ResourceFonts = new Dictionary<mlpFontKind, Font>();
        private static readonly Dictionary<string, Font> FallbackFonts = new Dictionary<string, Font>();
        private static readonly HashSet<mlpFontKind> MissingResourceWarnings = new HashSet<mlpFontKind>();

        /// <summary>
        /// 返回指定样式的字体，优先从 Resources 加载，否则回退到系统字体。
        /// </summary>
        public static Font Get(mlpFontKind fontKind, int fontSize)
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
                case mlpFontKind.RajdhaniSemiBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Rajdhani SemiBold", "Bahnschrift SemiBold", "Bahnschrift SemiCondensed", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case mlpFontKind.RajdhaniBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Rajdhani Bold", "Bahnschrift Bold", "Bahnschrift SemiBold", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case mlpFontKind.Griffy:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Griffy", "Bungee", "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case mlpFontKind.AgencyBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Barlow Condensed Bold", "Barlow Condensed", "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case mlpFontKind.CfCrackBold:
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Bungee", "Anton", "Arial Black", "Arial" },
                        fontSize);
                    break;
                case mlpFontKind.Impact2:
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
        /// 尝试加载指定字体类型的内置资源。
        /// </summary>
        private static Font GetResourceFont(mlpFontKind fontKind)
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
        /// 返回内置字体的 Resources 文件夹路径。
        /// </summary>
        private static string GetResourcePath(mlpFontKind fontKind)
        {
            switch (fontKind)
            {
                case mlpFontKind.RajdhaniSemiBold:
                    return "mlp/Fonts/Rajdhani-SemiBold";
                case mlpFontKind.RajdhaniBold:
                    return "mlp/Fonts/Rajdhani-Bold";
                case mlpFontKind.Griffy:
                    return "mlp/Fonts/Griffy-Regular";
                case mlpFontKind.AgencyBold:
                    return "mlp/Fonts/AgencyBold";
                case mlpFontKind.CfCrackBold:
                    return "mlp/Fonts/CfCrackBold";
                case mlpFontKind.Impact2:
                    return "mlp/Fonts/Impact2";
                default:
                    return "mlp/Fonts/Impact";
            }
        }

        /// <summary>
        /// 设置字体纹理参数，确保像素风格渲染清晰。
        /// </summary>
        private static void PrepareFont(Font font)
        {
            if (font == null || font.material == null || font.material.mainTexture == null)
            {
                return;
            }

            font.material.mainTexture.wrapMode = TextureWrapMode.Clamp;
            // 保持复古 UI 文字清晰，避免字体图集采样导致边缘模糊。
            font.material.mainTexture.filterMode = FilterMode.Point;
        }
    }

public static class mlpFontMaterialCache
    {
        private const string OutlinedShaderName = "mlp/TextMeshOutlined";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static bool missingShaderLogged;
        private static Shader outlinedShader;

        /// <summary>
        /// 返回应用了描边和阴影设置的材质，使用自定义描边着色器。
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
        /// 构建用于材质缓存键的十六进制颜色字符串。
        /// </summary>
        private static string ColorKey(Color color)
        {
            var color32 = (Color32)color;
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
    }
}

public static class mlpSharedMaterialCache
{
    private const string SpritesDefaultShaderName = "Sprites/Default";
    private static readonly Dictionary<int, Material> SpritesDefaultMaterials = new Dictionary<int, Material>();
    private static bool missingSpritesDefaultShaderLogged;
    private static Shader spritesDefaultShader;
    private static Material untexturedSpritesDefaultMaterial;

    /// <summary>
    /// 返回指定纹理的共享 Sprites/Default 材质。
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
    /// 查找并缓存 Sprites/Default 着色器。
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
    /// 使用给定的着色器和纹理创建一个新的隐藏且不保存的材质。
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

internal enum mlpTextRasterProfile
{
    UiSmall,
    UiMedium,
    Display,
    Score
}

internal struct mlpResolvedTextStyle
{
    public mlpFontKind FontKind;
    public Color? OutlineColor;
    public float OutlinePixels;
    public Color? ShadowColor;
    public Vector2? ShadowOffset;
    public mlpTextRasterProfile RasterProfile;
}

internal static class mlpTextStyles
{
    /// <summary>
    /// 返回指定文本样式和字号的字体、描边和阴影设置。
    /// </summary>
    public static mlpResolvedTextStyle Resolve(mlpTextStyle style, int fontSize)
    {
        switch (style)
        {
            case mlpTextStyle.HudName:
                return Create(
                    mlpFontKind.RajdhaniSemiBold,
                    mlpTextRasterProfile.UiMedium,
                    new Color32(0x04, 0x1A, 0x22, 0xE0),
                    fontSize >= 18 ? 0.65f : 0.5f);
            case mlpTextStyle.HudScore:
                return Create(
                    mlpFontKind.RajdhaniBold,
                    mlpTextRasterProfile.Score,
                    new Color32(0x41, 0x10, 0x00, 0xEA),
                    fontSize >= 34 ? 1.15f : 0.95f);
            case mlpTextStyle.HudTimer:
                return Create(
                    mlpFontKind.RajdhaniSemiBold,
                    mlpTextRasterProfile.UiMedium,
                    new Color32(0x10, 0x27, 0x1A, 0xD8),
                    0.55f);
            case mlpTextStyle.HudPopup:
                return Create(
                    mlpFontKind.Griffy,
                    mlpTextRasterProfile.Display,
                    new Color32(0x24, 0x05, 0x47, 0xE8),
                    fontSize >= 54 ? 1.2f : 0.95f);
            case mlpTextStyle.TournamentBody:
                return Create(mlpFontKind.Impact2, mlpTextRasterProfile.UiSmall);
            case mlpTextStyle.TournamentAccent:
                return Create(mlpFontKind.Impact2, mlpTextRasterProfile.UiMedium);
            case mlpTextStyle.ButtonLabel:
                return Create(
                    mlpFontKind.Impact2,
                    mlpTextRasterProfile.UiMedium,
                    new Color(0f, 0f, 0f, 0.35f),
                    fontSize >= 28 ? 0.75f : 0.55f);
            case mlpTextStyle.LinkLabel:
                return Create(mlpFontKind.Impact2, mlpTextRasterProfile.UiMedium);
            case mlpTextStyle.DisplayTitle:
                return Create(
                    mlpFontKind.AgencyBold,
                    mlpTextRasterProfile.Display,
                    new Color(0f, 0f, 0f, 0.72f),
                    fontSize >= 42 ? 0.95f : 0.8f);
            case mlpTextStyle.Subtitle:
                return Create(mlpFontKind.Impact2, mlpTextRasterProfile.UiMedium);
            case mlpTextStyle.StoryScrollTitle:
                return Create(
                    mlpFontKind.Griffy,
                    mlpTextRasterProfile.Display,
                    new Color32(0x54, 0x2C, 0x12, 0x38),
                    fontSize >= 26 ? 0.42f : 0.3f);
            case mlpTextStyle.StoryScrollBody:
                return Create(
                    mlpFontKind.RajdhaniSemiBold,
                    mlpTextRasterProfile.UiMedium,
                    new Color32(0x43, 0x26, 0x10, 0x18),
                    0.16f);
            default:
                return Create(mlpFontKind.Impact2, mlpTextRasterProfile.UiSmall);
        }
    }

    /// <summary>
    /// 根据各组件值构建已解析的文本样式结构体。
    /// </summary>
    private static mlpResolvedTextStyle Create(
        mlpFontKind fontKind,
        mlpTextRasterProfile rasterProfile,
        Color? outlineColor = null,
        float outlinePixels = 0f,
        Color? shadowColor = null,
        Vector2? shadowOffset = null)
    {
        return new mlpResolvedTextStyle
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

public static class mlpRender
    {
        private const int PortraitBackplateTextureSize = 192;
        private static readonly Dictionary<string, Texture2D> PortraitBackplateTextures = new Dictionary<string, Texture2D>();

        /// <summary>
        /// 创建一个带有 SpriteRenderer 的 GameObject，显示指定的图集帧。
        /// </summary>
        public static GameObject Sprite(string name, mlpAtlas atlas, string frame, float x, float y, float anchorX, float anchorY, int sortingOrder, Transform parent = null)
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
        /// 创建一个带有 SpriteRenderer 的 GameObject，显示指定的纹理。
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
    /// 使用指定的样式、位置和颜色创建 TextMesh。
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
        mlpTextStyle style)
    {
        var resolvedStyle = mlpTextStyles.Resolve(style, fontSize);
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
    /// 使用明确的字体和描边设置创建 TextMesh。
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
        mlpFontKind fontKind = mlpFontKind.Impact,
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
    /// 构建带有字体、材质和像素变换的 TextMesh GameObject。
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
        mlpFontKind fontKind,
        Color? outlineColor,
        float outlinePixels,
        Color? shadowColor,
        Vector2? shadowOffset,
        mlpTextRasterProfile rasterProfile)
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
            mesh.font = mlpFontCache.Get(fontKind, mesh.fontSize);
            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null)
            {
                mesh.font.RequestCharactersInTexture(text, mesh.fontSize, FontStyle.Normal);
                renderer.sharedMaterial = mlpFontMaterialCache.Get(
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
    /// 使用指定的样式、位置和颜色创建 TextMeshPro 组件。
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
        mlpTextStyle style)
    {
        var resolvedStyle = mlpTextStyles.Resolve(style, fontSize);
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

        var fontAsset = mlpTmpFontCache.Get(resolvedStyle.FontKind);
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
        /// 根据像素坐标设置变换的世界位置，对齐到屏幕像素网格。
        /// </summary>
        public static void ApplyPixelTransform(Transform transform, float x, float y, float z = 0f, float scale = 1f, float rotationDegrees = 0f)
        {
            transform.position = mlpConstants.PixelToWorldSnapped(x, y, z);
            var scaled = mlpConstants.UnitsPerPixel * scale;
            transform.localScale = new Vector3(scaled, scaled, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
        }

    /// <summary>
    /// 根据字号和是否使用描边/阴影选择字体光栅化配置。
    /// </summary>
    private static mlpTextRasterProfile ResolveRawRasterProfile(int fontSize, Color? outlineColor, float outlinePixels, Color? shadowColor)
    {
        var hasOutline = outlineColor.HasValue && outlineColor.Value.a > 0f && outlinePixels > 0f;
        var hasShadow = shadowColor.HasValue && shadowColor.Value.a > 0f;
        if (fontSize >= 40)
        {
            return mlpTextRasterProfile.Display;
        }

        if (hasOutline || hasShadow)
        {
            return fontSize <= 18 ? mlpTextRasterProfile.UiSmall : mlpTextRasterProfile.UiMedium;
        }

        return fontSize <= 18 ? mlpTextRasterProfile.UiSmall : mlpTextRasterProfile.UiMedium;
    }

    /// <summary>
    /// 根据逻辑字号和光栅化配置计算实际的字体纹理光栅尺寸。
    /// </summary>
    private static int GetFontRasterSize(int fontSize, mlpTextRasterProfile rasterProfile)
    {
        float multiplier;
        switch (rasterProfile)
        {
            case mlpTextRasterProfile.UiSmall:
                multiplier =
                    fontSize <= 12 ? 10f :
                    fontSize <= 18 ? 9f :
                    8f;
                break;
            case mlpTextRasterProfile.UiMedium:
                multiplier =
                    fontSize <= 18 ? 8f :
                    fontSize <= 28 ? 7f :
                    6f;
                break;
            case mlpTextRasterProfile.Score:
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
        /// 将 TextAnchor 枚举转换为旧版 TextMesh 对齐枚举。
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
        /// 将 TextAnchor 枚举转换为 TextMeshPro 对齐选项。
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
