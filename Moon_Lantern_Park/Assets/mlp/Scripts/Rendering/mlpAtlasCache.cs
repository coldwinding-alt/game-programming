// Atlas, font and material cache / Load and cache the texture atlas used by the game (combining many small images into one large image to improve performance), fonts and material balls. Rendering auxiliary tools are also provided, such as creating sprites, drawing text, and applying pixel coordinate transformations. It is the basis of the entire game rendering system.

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Atlas cache manager (single case): loads and caches texture atlases, fonts and materials, and provides rendering tools such as creating sprites and drawing text.
    /// </summary>
    public sealed class mlpAtlasCache
    {
        private static mlpAtlasCache instance;

        private readonly Dictionary<string, mlpAtlas> atlases = new Dictionary<string, mlpAtlas>();

        public static mlpAtlasCache Instance => instance ?? (instance = new mlpAtlasCache());

        public mlpAtlas Gameplay => Get(mlpAssets.Atlases.Gameplay);

        public mlpAtlas Interface => Get(mlpAssets.Atlases.Interface);

        public mlpAtlas SkillFx => Get(mlpAssets.Atlases.SkillFx);

        /// <summary>
        /// Get or create a cached atlas based on a resource key.
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

    /// <summary>
    /// Texture atlas: A large image containing many small images, each of which can be cut out through sub-texture information. More efficient than loading many small images individually.
    /// </summary>
    public sealed class mlpAtlas
    {
        private readonly Texture2D texture;
        private readonly Dictionary<string, mlpAtlasFrame> frames = new Dictionary<string, mlpAtlasFrame>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Loads a texture atlas and parses its sprite frame metadata from JSON.

        /// </summary>
        public mlpAtlas(string atlasKey)
        {
            // 1. Load the large texture image of the atlas from the Resources folder

            texture = Resources.Load<Texture2D>($"mlp/Atlases/{atlasKey}");
            // 2. Load the JSON file with the same name (recording the position and size of each small picture in the large picture)

            var jsonAsset = Resources.Load<TextAsset>($"mlp/Atlases/{atlasKey}");
            // 3. If the image or JSON is missing, an error will be reported and exit.

            if (texture == null || jsonAsset == null)
            {
                Debug.LogError($"Missing atlas {atlasKey}");
                return;
            }

            // 4. Parse JSON and retrieve the frame information of all small images

            var root = mlpJson.AsDict(mlpJson.Parse(jsonAsset.text));
            var rawFrames = mlpJson.Dict(root, "frames");
            if (rawFrames == null)
            {
                return;
            }

            // 5. Traverse each frame, skip the "meta" field, and store the position information of each small picture in the dictionary.

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
        /// Checks whether the atlas contains a sprite frame with the specified name.

        /// </summary>
        public bool HasFrame(string frameName)
        {
            return frames.ContainsKey(frameName);
        }

        /// <summary>
        /// Creates or returns a cached Sprite for the specified gallery frame, using the given anchor.

        /// </summary>
        public Sprite Sprite(string frameName, float anchorX = 0.5f, float anchorY = 0.5f)
        {
            // 1. Check whether the texture and frame name exist. If they do not exist, a warning will be issued and null will be returned.

            if (texture == null || !frames.TryGetValue(frameName, out var frame))
            {
                Debug.LogWarning($"Missing atlas frame {frameName}");
                return null;
            }

            // 2. Use the frame name and anchor point to form a cache key. If it has been cached, return it directly.

            var cacheKey = $"{frameName}|{anchorX:0.###}|{anchorY:0.###}";
            if (spriteCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // 3. Cut out the rectangular area of the small image from the large image (note that the Y axis needs to be flipped, because the texture coordinates and the atlas coordinates are in opposite directions)

            var rect = new Rect(frame.X, texture.height - frame.Y - frame.H, frame.W, frame.H);
            // 4. Calculate the pivot point of the elf based on the anchor point (control the rotation and alignment center of the elf)

            var pivot = frame.Pivot(anchorX, anchorY);
            // 5. Create Unity Sprite object and store it in cache

            var sprite = UnityEngine.Sprite.Create(texture, rect, pivot, 1f, 0, SpriteMeshType.FullRect);
            sprite.name = frameName;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Returns the original atlas frame metadata for the specified frame name.

        /// </summary>
        public mlpAtlasFrame Frame(string frameName)
        {
            frames.TryGetValue(frameName, out var frame);
            return frame;
        }
    }

    /// <summary>
    /// Atlas frame information: records the position, size and anchor point of a small picture in the large picture for cropping and display.

    /// </summary>
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
        /// Parse gallery frame metadata from a JSON dictionary.

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
        /// Calculate the sprite's pivot point based on the anchor point value and source frame dimensions.

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

    /// <summary>
    /// Font caching: Load and cache TextMeshPro font resources to avoid repeated loading.

    /// </summary>
    public static class mlpFontCache
    {
        private static readonly Dictionary<mlpFontKind, Font> ResourceFonts = new Dictionary<mlpFontKind, Font>();
        private static readonly Dictionary<string, Font> FallbackFonts = new Dictionary<string, Font>();
        private static readonly HashSet<mlpFontKind> MissingResourceWarnings = new HashSet<mlpFontKind>();

        /// <summary>
        /// Returns the font of the specified style, loading it from Resources first, otherwise falling back to the system font.

        /// </summary>
        public static Font Get(mlpFontKind fontKind, int fontSize)
        {
            // 1. Prioritize trying to load built-in font resources from Resources

            var resourceFont = GetResourceFont(fontKind);
            if (resourceFont != null)
            {
                return resourceFont;
            }

            // 2. Check whether there is already a fallback font cache of the same type and size

            var cacheKey = $"{fontKind}:{fontSize}";
            if (FallbackFonts.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // 3. Built-in resources are missing, dynamic fonts are created from the system font list according to the font type (with priority fallback)

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

            // 4. Set font texture parameters (point filtering, clamping mode), and return after caching

            PrepareFont(font);
            FallbackFonts[cacheKey] = font;
            return font;
        }

        /// <summary>
        /// Attempts to load a built-in resource for the specified font type.
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
        /// Returns the Resources folder path for built-in fonts.

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
        /// Set font texture parameters to ensure clear pixel style rendering.

        /// </summary>
        private static void PrepareFont(Font font)
        {
            if (font == null || font.material == null || font.material.mainTexture == null)
            {
                return;
            }

            font.material.mainTexture.wrapMode = TextureWrapMode.Clamp;
            // Keep retro UI text clear and avoid blurry edges caused by font atlas sampling.

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
        /// Returns the material with stroke and shadow settings applied, using a custom stroke shader.

        /// </summary>
        public static Material Get(
            Font font,
            Color? outlineColor,
            float outlinePixels,
            Color? shadowColor,
            Vector2? shadowOffset,
            float rasterScale)
        {
            // 1. Return null when the font or material is empty.

            if (font == null || font.material == null)
            {
                return null;
            }

            // 2. Get the basic material and texture of the font

            var baseMaterial = font.material;
            var mainTexture = baseMaterial.mainTexture;
            if (mainTexture == null)
            {
                return baseMaterial;
            }

            // 3. Determine whether stroke or shadow is needed. If neither is needed, return directly to the basic material.

            var hasOutline = outlineColor.HasValue && outlineColor.Value.a > 0f && outlinePixels > 0f;
            var hasShadow = shadowColor.HasValue && shadowColor.Value.a > 0f;
            if (!hasOutline && !hasShadow)
            {
                return baseMaterial;
            }

            // 4. Find the custom stroke shader (once for first use)

            if (outlinedShader == null)
            {
                outlinedShader = Shader.Find(OutlinedShaderName);
            }

            // 5. When the shader cannot be found, fall back to the basic material and output a warning.

            if (outlinedShader == null)
            {
                if (!missingShaderLogged)
                {
                    Debug.LogWarning($"Could not find shader '{OutlinedShaderName}', using default font material.");
                    missingShaderLogged = true;
                }

                return baseMaterial;
            }

            // 6. Convert stroke and shadow parameters to texture pixel units

            var resolvedOutlineColor = hasOutline ? outlineColor.Value : Color.clear;
            var resolvedShadowColor = hasShadow ? shadowColor.Value : Color.clear;
            var resolvedShadowOffset = hasShadow ? shadowOffset ?? new Vector2(0f, 2f) : Vector2.zero;
            var outlineTexels = hasOutline ? outlinePixels * rasterScale : 0f;
            var shadowTexels = hasShadow ? resolvedShadowOffset * rasterScale : Vector2.zero;

            // 7. Generate cache keys with all parameters and find or create material instances

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

            // 8. Set the texture, stroke color/width, shadow color/offset of the material

            material.SetTexture("_MainTex", mainTexture);
            material.SetColor("_OutlineColor", resolvedOutlineColor);
            material.SetFloat("_OutlineWidth", outlineTexels);
            material.SetColor("_ShadowColor", resolvedShadowColor);
            material.SetVector("_ShadowOffset", new Vector4(shadowTexels.x, shadowTexels.y, 0f, 0f));
            return material;
        }

        /// <summary>
        /// Constructs a hexadecimal color string used for material cache keys.

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
    /// Returns the shared Sprites/Default material for the specified texture.

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
    /// Find and cache Sprites/Default shaders.

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
    /// Creates a new hidden and unsaved material using the given shader and texture.

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
    /// Returns the font, stroke, and shading settings for the specified text style and size.

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
    /// Builds a parsed text style structure from each component value.

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
        /// Creates a GameObject with a SpriteRenderer that displays the specified atlas frame.

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
        /// Creates a GameObject with a SpriteRenderer displaying the specified texture.

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
            // 1. Create a root node and position it at the specified pixel coordinates

            var root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            ApplyPixelTransform(root.transform, x, y);
            // 2. Make sure the diameter is at least 1 pixel to prevent division by zero

            var safeDiameter = Mathf.Max(1f, diameterPixels);
            // 3. Create four layers of concentric circle effects: outer halo (maximum, translucent), fill layer, outer ring, inner ring

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
            // 4. The inner ring is slightly smaller than the outer ring, and the transparency is 62% of the outer ring (to create a sense of layering)
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
            // 1. Check whether there is already a texture with the same name in the cache

            if (PortraitBackplateTextures.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // 2. Create square texture (RGBA32 format, bilinear filtering, clamping mode)

            var texture = new Texture2D(PortraitBackplateTextureSize, PortraitBackplateTextureSize, TextureFormat.RGBA32, false)
            {
                name = $"PortraitBackplate_{key}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[PortraitBackplateTextureSize * PortraitBackplateTextureSize];
            var center = (PortraitBackplateTextureSize - 1) * 0.5f;
            var radiusScale = Mathf.Max(1f, center);

            // 3. Calculate the distance to the center of the circle pixel by pixel and generate the alpha value of the radial gradient.

            for (var y = 0; y < PortraitBackplateTextureSize; y++)
            {
                for (var x = 0; x < PortraitBackplateTextureSize; x++)
                {
                    var dx = (x - center) / radiusScale;
                    var dy = (y - center) / radiusScale;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    // 4. Use SmoothStep on the outer edge to achieve a soft transition.

                    var outerAlpha = 1f - Mathf.SmoothStep(outerRadius - softness, outerRadius, distance);
                    // 5. Inner edges (if any) create a hollow effect

                    var innerAlpha = innerRadius <= 0f
                        ? 1f
                        : Mathf.SmoothStep(innerRadius, innerRadius + softness, distance);
                    var alpha = Mathf.Clamp01(outerAlpha * innerAlpha);
                    // 6. Halo mode additionally performs distance attenuation and gamma compression to create a soft glowing effect.

                    if (glow)
                    {
                        alpha *= Mathf.Clamp01(1f - distance / Mathf.Max(0.0001f, outerRadius));
                        alpha = Mathf.Pow(alpha, 1.55f);
                    }

                    pixels[y * PortraitBackplateTextureSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            // 7. Upload pixel data to GPU and cache textures

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            PortraitBackplateTextures[key] = texture;
            return texture;
        }

    /// <summary>
    /// Creates a TextMesh using the specified style, position, and color.

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
    /// Create a TextMesh with explicit font and stroke settings.

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
    /// Build a TextMesh GameObject with fonts, materials, and pixel transforms.

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
        // 1. Create GameObject and mount it to the parent node

        var go = new GameObject(name);
        if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

        // 2. Add the TextMesh component and set the text content, font size, alignment and color

        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = GetFontRasterSize(fontSize, rasterProfile);
        mesh.characterSize = Mathf.Max(0.1f, fontSize * 0.1f);
        mesh.anchor = anchor;
        mesh.alignment = AnchorToAlignment(anchor);
            mesh.color = color;
            // 3. Load the font and request character rendering (make sure the font texture contains the required characters)

            mesh.font = mlpFontCache.Get(fontKind, mesh.fontSize);
            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null)
            {
                mesh.font.RequestCharactersInTexture(text, mesh.fontSize, FontStyle.Normal);
                // 4. Get the material with stroke/shadow effect. If not needed, use the font default material.

                renderer.sharedMaterial = mlpFontMaterialCache.Get(
                    mesh.font,
                    outlineColor,
                    outlinePixels,
                    shadowColor,
                    shadowOffset,
                    mesh.fontSize / (float)Mathf.Max(1, fontSize))
                    ?? mesh.font.material;
            }

            // 5. Set the rendering sorting level and apply pixel coordinate transformation

            renderer.sortingOrder = sortingOrder;
        ApplyPixelTransform(go.transform, x, y);

        return mesh;
    }

    /// <summary>
    /// Creates a TextMeshPro component using the specified style, position, and color.

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
        // 1. Parse the text style and obtain the font type and stroke settings

        var resolvedStyle = mlpTextStyles.Resolve(style, fontSize);
        // 2. Create GameObject and mount it to the parent node

        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        // 3. Add the TextMeshPro component and configure basic properties (disable rich text, automatic word wrapping and overflow cropping)

        var textComponent = go.AddComponent<TextMeshPro>();
        textComponent.text = text;
        textComponent.richText = false;
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.extraPadding = true;
        textComponent.alignment = AnchorToTmpAlignment(anchor);
        textComponent.color = color;
        // 4. Set the font size (multiply by 10 to convert to world units), and fix the minimum and maximum font sizes to be consistent

        var worldFontSize = fontSize * 10f;
        textComponent.fontSize = worldFontSize;
        textComponent.fontSizeMin = worldFontSize;
        textComponent.fontSizeMax = worldFontSize;
        textComponent.lineSpacing = -4f;

        // 5. Load TextMeshPro font resources and apply

        var fontAsset = mlpTmpFontCache.Get(resolvedStyle.FontKind);
        if (fontAsset != null)
        {
            textComponent.font = fontAsset;
            if (fontAsset.material != null)
            {
                textComponent.fontSharedMaterial = fontAsset.material;
            }
        }

        // 6. Set the stroke width and color (width is 0 when there is no stroke)

        textComponent.outlineWidth = resolvedStyle.OutlineColor.HasValue && resolvedStyle.OutlinePixels > 0f
            ? Mathf.Clamp01(resolvedStyle.OutlinePixels * 0.08f)
            : 0f;
        textComponent.outlineColor = resolvedStyle.OutlineColor ?? Color.clear;
        // 7. Force refresh the grid, set the rendering sorting level, and apply pixel coordinate transformation

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
        /// Sets the transformed world position based on pixel coordinates, aligned to the screen pixel grid.
        /// </summary>
        public static void ApplyPixelTransform(Transform transform, float x, float y, float z = 0f, float scale = 1f, float rotationDegrees = 0f)
        {
            transform.position = mlpConstants.PixelToWorldSnapped(x, y, z);
            var scaled = mlpConstants.UnitsPerPixel * scale;
            transform.localScale = new Vector3(scaled, scaled, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
        }

    /// <summary>
    /// Select a font rasterization configuration based on font size and whether to use strokes/shadows.

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
    /// Calculates the actual font texture raster size based on the logical font size and rasterization configuration.

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
        /// Convert TextAnchor enumeration to legacy TextMesh alignment enumeration.
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
        /// Convert TextAnchor enumeration to TextMeshPro alignment options.
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
