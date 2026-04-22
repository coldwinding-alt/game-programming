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
        CfCrackBold
    }

    public sealed class BLTextSync : MonoBehaviour
    {
        public TextMesh Source;
        public TextMesh[] Mirrors;
        private MeshRenderer sourceRenderer;
        private readonly List<MeshRenderer> mirrorRenderers = new List<MeshRenderer>();

        private void Awake()
        {
            sourceRenderer = GetComponent<MeshRenderer>();
            mirrorRenderers.Clear();
            if (Mirrors == null)
            {
                return;
            }

            for (var i = 0; i < Mirrors.Length; i++)
            {
                if (Mirrors[i] != null)
                {
                    mirrorRenderers.Add(Mirrors[i].GetComponent<MeshRenderer>());
                }
            }
        }

        private void LateUpdate()
        {
            if (Source == null || Mirrors == null)
            {
                return;
            }

            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponent<MeshRenderer>();
            }

            var visible = !string.IsNullOrEmpty(Source.text);
            if (sourceRenderer != null)
            {
                sourceRenderer.enabled = visible;
                if (Source.font != null)
                {
                    sourceRenderer.sharedMaterial = Source.font.material;
                }
            }

            for (var i = 0; i < Mirrors.Length; i++)
            {
                var mirror = Mirrors[i];
                if (mirror == null)
                {
                    continue;
                }

                mirror.text = Source.text;
                mirror.font = Source.font;
                mirror.fontSize = Source.fontSize;
                mirror.characterSize = Source.characterSize;
                mirror.anchor = Source.anchor;
                mirror.alignment = Source.alignment;

                var renderer = mirror.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = visible;
                    if (mirror.font != null)
                    {
                        renderer.sharedMaterial = mirror.font.material;
                    }
                }
            }
        }
    }

    public static class BLFontCache
    {
        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        public static Font Get(BLFontKind fontKind, int fontSize)
        {
            var cacheKey = $"{fontKind}:{fontSize}";
            if (Fonts.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            Font font;
            switch (fontKind)
            {
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

            Fonts[cacheKey] = font;
            return font;
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
            Transform parent = null,
            BLFontKind fontKind = BLFontKind.Impact,
            Color? outlineColor = null,
            float outlinePixels = 0f,
            Color? shadowColor = null,
            Vector2? shadowOffset = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = Mathf.Clamp(fontSize * 4, 32, 256);
            mesh.characterSize = Mathf.Max(0.1f, fontSize * 0.1f);
            mesh.anchor = anchor;
            mesh.alignment = AnchorToAlignment(anchor);
            mesh.color = color;
            mesh.font = BLFontCache.Get(fontKind, mesh.fontSize);
            var renderer = go.GetComponent<MeshRenderer>();
            if (mesh.font != null)
            {
                renderer.sharedMaterial = mesh.font.material;
            }

            renderer.sortingOrder = sortingOrder;
            ApplyPixelTransform(go.transform, x, y);

            var mirrors = new List<TextMesh>();
            if (shadowColor.HasValue && shadowColor.Value.a > 0f)
            {
                mirrors.Add(CreateMirror(go.transform, "Shadow", sortingOrder - 2, shadowColor.Value, shadowOffset ?? new Vector2(0f, 2f)));
            }

            if (outlineColor.HasValue && outlinePixels > 0f && outlineColor.Value.a > 0f)
            {
                var outline = Mathf.Max(1f, outlinePixels * 0.5f);
                mirrors.Add(CreateMirror(go.transform, "OutlineL", sortingOrder - 1, outlineColor.Value, new Vector2(-outline, 0f)));
                mirrors.Add(CreateMirror(go.transform, "OutlineR", sortingOrder - 1, outlineColor.Value, new Vector2(outline, 0f)));
                mirrors.Add(CreateMirror(go.transform, "OutlineU", sortingOrder - 1, outlineColor.Value, new Vector2(0f, -outline)));
                mirrors.Add(CreateMirror(go.transform, "OutlineD", sortingOrder - 1, outlineColor.Value, new Vector2(0f, outline)));
            }

            if (mirrors.Count > 0)
            {
                var sync = go.AddComponent<BLTextSync>();
                sync.Source = mesh;
                sync.Mirrors = mirrors.ToArray();
            }

            return mesh;
        }

        public static void ApplyPixelTransform(Transform transform, float x, float y, float z = 0f, float scale = 1f, float rotationDegrees = 0f)
        {
            transform.position = BLConstants.PixelToWorld(x, y, z);
            var scaled = BLConstants.UnitsPerPixel * scale;
            transform.localScale = new Vector3(scaled, scaled, 1f);
            transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);
        }

        private static TextMesh CreateMirror(Transform parent, string name, int sortingOrder, Color color, Vector2 offsetPixels)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(offsetPixels.x, -offsetPixels.y, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
            return mesh;
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
