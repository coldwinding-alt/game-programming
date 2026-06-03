// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushPlayersData 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public static class rimrushPlayersData
    {
        private const int ActiveCharacterSkinCount = 8;
        private const float PortraitAtlasSourceScale = 4f;
        private const int PortraitVariantSampleGrid = 4;
        private const float GlobalCharacterModelScaleMultiplier = 1.08f;

        private sealed class rimrushCharacterDefinition
        {
            public string DisplayName;
            public int SkinIndex;
            public int FormIndex;
            public int SuperId;
            public bool Enabled;
            public string PortraitSpriteName;
            public float HeadOffsetX;
            public float HeadOffsetY;
            public float HeadScale = 1f;
            public float ModelScaleMultiplier = 1f;
            public float PreviewScaleMultiplier = 1f;
            public float PreviewOffsetY;
            public float PortraitScaleMultiplier = 1f;
            // Portrait offsets are expressed in source-sprite pixels so they can scale with each UI slot size.
            public float PortraitOffsetY;
        }

        private static DBLiteTextureAtlas portraitAtlas;
        private static readonly Dictionary<string, Sprite> PortraitVariantSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> PortraitVariantTextures = new Dictionary<string, Texture2D>();

        private static readonly rimrushCharacterDefinition[] CharacterDefinitions =
        {
            new rimrushCharacterDefinition { DisplayName = "REAPER ACOLYTE", SkinIndex = 0, FormIndex = 0, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_pumpkin", HeadOffsetX = 0.75f, HeadOffsetY = 9f, HeadScale = 1.02f, ModelScaleMultiplier = 1.08f, PreviewScaleMultiplier = 1f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 8f },
            new rimrushCharacterDefinition { DisplayName = "GHOST CLOWN", SkinIndex = 1, FormIndex = 1, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_frankenstein", HeadOffsetX = 4.5f, HeadOffsetY = 1f, HeadScale = 1f, ModelScaleMultiplier = 1.06f, PreviewScaleMultiplier = 0.99f, PortraitScaleMultiplier = 0.98f, PortraitOffsetY = 9f },
            new rimrushCharacterDefinition { DisplayName = "SKULL PIRATE", SkinIndex = 2, FormIndex = 2, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_mummy", HeadOffsetX = 1.5f, HeadOffsetY = 0f, HeadScale = 1.02f, ModelScaleMultiplier = 1.07f, PreviewScaleMultiplier = 1f, PreviewOffsetY = -2f, PortraitScaleMultiplier = 0.98f, PortraitOffsetY = 9f },
            new rimrushCharacterDefinition { DisplayName = "VAMPIRE", SkinIndex = 3, FormIndex = 3, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_vampire", HeadOffsetY = -10.5f, HeadScale = 0.95f, PreviewScaleMultiplier = 0.96f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 12f },
            new rimrushCharacterDefinition { DisplayName = "CANDLEMAN", SkinIndex = 4, FormIndex = 4, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_candle", HeadOffsetX = 2.75f, HeadOffsetY = 6f, HeadScale = 0.96f, PreviewScaleMultiplier = 0.94f, PortraitScaleMultiplier = 0.85f, PortraitOffsetY = -9f },
            new rimrushCharacterDefinition { DisplayName = "SCARECROW", SkinIndex = 5, FormIndex = 5, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_scarecrow", HeadOffsetY = 7f, HeadScale = 1.05f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.05f, PortraitOffsetY = -10f },
            new rimrushCharacterDefinition { DisplayName = "WITCH", SkinIndex = 6, FormIndex = 6, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_witch", HeadOffsetX = 3.5f, HeadOffsetY = 8f, HeadScale = 1.1f, PreviewScaleMultiplier = 0.98f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.12f, PortraitOffsetY = -9f },
            new rimrushCharacterDefinition { DisplayName = "BLACK CAT", SkinIndex = 7, FormIndex = 7, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_blackcat", HeadOffsetX = 6f, HeadOffsetY = 7f, HeadScale = 0.99f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 1f, PortraitScaleMultiplier = 0.96f, PortraitOffsetY = -5f }
        };

        private static readonly int[] Hands = { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static readonly string[] Legs =
        {
            "leg1",
            "leg2",
            "leg3",
            "leg4",
            "leg5",
            "leg6",
            "leg7",
            "leg8"
        };

        public static int CharacterCount => CharacterDefinitions.Length;

        /// <summary>
        /// Executes Setup Players for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public static void SetupPlayers()
        {
            // Active Halloween characters now use an explicit 8-character DragonBones set.
        }

        /// <summary>
        /// Executes Build Gameplay Armature for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static DBLiteArmature BuildGameplayArmature(string name)
        {
            DBLiteFactory.Instance.EnsureLoaded();
            return DBLiteFactory.Instance.BuildArmature("playerSmall", name);
        }

        /// <summary>
        /// Executes Get Active Character Ids for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int[] GetActiveCharacterIds()
        {
            var active = new List<int>(CharacterDefinitions.Length);
            for (var i = 0; i < CharacterDefinitions.Length; i++)
            {
                if (CharacterDefinitions[i].Enabled)
                {
                    active.Add(i);
                }
            }

            return active.ToArray();
        }

        /// <summary>
        /// Executes Sanitize Character Id for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="requestedCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="fallbackCharacterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int SanitizeCharacterId(int requestedCharacterId, int fallbackCharacterId = 0)
        {
            if (IsCharacterEnabled(requestedCharacterId))
            {
                return requestedCharacterId;
            }

            if (IsCharacterEnabled(fallbackCharacterId))
            {
                return fallbackCharacterId;
            }

            var active = GetActiveCharacterIds();
            return active.Length > 0 ? active[0] : 0;
        }

        /// <summary>
        /// Executes Step Character Id for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="currentCharacterId">Input value used by this step of the workflow.</param>
        /// <param name="direction">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int StepCharacterId(int currentCharacterId, int direction)
        {
            var active = GetActiveCharacterIds();
            if (active.Length == 0)
            {
                return 0;
            }

            var currentIndex = 0;
            for (var i = 0; i < active.Length; i++)
            {
                if (active[i] == currentCharacterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            var nextIndex = (currentIndex + direction) % active.Length;
            if (nextIndex < 0)
            {
                nextIndex += active.Length;
            }

            return active[nextIndex];
        }

        /// <summary>
        /// Executes Get Character Name for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static string GetCharacterName(int characterId)
        {
            return GetCharacterDefinition(characterId).DisplayName;
        }

        /// <summary>
        /// Executes Get Character Form Index for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int GetCharacterFormIndex(int characterId)
        {
            return GetCharacterDefinition(characterId).FormIndex;
        }

        /// <summary>
        /// Executes Get Character Super Id for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int GetCharacterSuperId(int characterId)
        {
            return GetCharacterDefinition(characterId).SuperId;
        }

        /// <summary>
        /// Executes Get Character Preview Scale Multiplier for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static float GetCharacterPreviewScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return definition.PreviewScaleMultiplier * GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// Executes Get Character Gameplay Scale Multiplier for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static float GetCharacterGameplayScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// Executes Get Character Preview Offset Y for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static float GetCharacterPreviewOffsetY(int characterId)
        {
            return GetCharacterDefinition(characterId).PreviewOffsetY;
        }

        /// <summary>
        /// Executes Get Character Portrait Sprite for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <param name="desiredMaxPixels">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Sprite GetCharacterPortraitSprite(int characterId, float desiredMaxPixels = 0f)
        {
            var definition = GetCharacterDefinition(characterId);
            var baseSprite = GetPortraitBaseSprite(definition);
            if (baseSprite == null)
            {
                return null;
            }

            var requestedMaxPixels = Mathf.RoundToInt(desiredMaxPixels);
            if (requestedMaxPixels <= 0)
            {
                return baseSprite;
            }

            var baseMaxPixels = Mathf.RoundToInt(Mathf.Max(baseSprite.rect.width, baseSprite.rect.height));
            if (requestedMaxPixels >= baseMaxPixels)
            {
                return baseSprite;
            }

            return GetOrCreatePortraitVariantSprite(definition.PortraitSpriteName, baseSprite, requestedMaxPixels);
        }

        /// <summary>
        /// Executes Get Character Portrait Scale Multiplier for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static float GetCharacterPortraitScaleMultiplier(int characterId)
        {
            return GetCharacterDefinition(characterId).PortraitScaleMultiplier;
        }

        /// <summary>
        /// Executes Get Character Portrait Offset Y for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <param name="portraitSprite">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static float GetCharacterPortraitOffsetY(int characterId, Sprite portraitSprite = null)
        {
            var definition = GetCharacterDefinition(characterId);
            var baseOffset = definition.PortraitOffsetY * PortraitAtlasSourceScale;
            if (portraitSprite == null)
            {
                return baseOffset;
            }

            var baseSprite = GetPortraitBaseSprite(definition);
            if (baseSprite == null)
            {
                return baseOffset;
            }

            var baseMaxPixels = Mathf.Max(baseSprite.rect.width, baseSprite.rect.height);
            var spriteMaxPixels = Mathf.Max(portraitSprite.rect.width, portraitSprite.rect.height);
            if (baseMaxPixels <= 0.0001f || spriteMaxPixels <= 0.0001f)
            {
                return baseOffset;
            }

            return baseOffset * (spriteMaxPixels / baseMaxPixels);
        }

        /// <summary>
        /// Executes Apply Character for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="armature">Input value used by this step of the workflow.</param>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        public static void ApplyCharacter(DBLiteArmature armature, int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            SwitchPlayer(armature, definition.SkinIndex, definition.FormIndex);
            ApplyCharacterTuning(armature, definition);
        }

        /// <summary>
        /// Executes Get Random Character Id for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="excludedCharacterIds">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static int GetRandomCharacterId(IList<int> excludedCharacterIds = null)
        {
            var candidates = new List<int>(CharacterDefinitions.Length);
            for (var i = 0; i < CharacterDefinitions.Length; i++)
            {
                if (!CharacterDefinitions[i].Enabled)
                {
                    continue;
                }

                if (excludedCharacterIds != null && excludedCharacterIds.Contains(i))
                {
                    continue;
                }

                candidates.Add(i);
            }

            if (candidates.Count == 0)
            {
                return SanitizeCharacterId(0);
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Executes Switch Player for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="armature">Input value used by this step of the workflow.</param>
        /// <param name="skinId">Input value used by this step of the workflow.</param>
        /// <param name="formId">Input value used by this step of the workflow.</param>
        public static void SwitchPlayer(DBLiteArmature armature, int skinId, int formId)
        {
            if (armature == null)
            {
                return;
            }

            skinId = Mathf.Clamp(skinId, 0, ActiveCharacterSkinCount - 1);
            formId = Mathf.Max(0, formId);

            var hand = Hands[skinId];
            var leg = Legs[skinId];

            armature.GetChildArmature("head")?.Play("head" + (skinId + 1));
            armature.GetChildArmature("body")?.Play("body" + (formId + 1));
            armature.GetChildArmature("left hand")?.Play("hand" + hand);
            armature.GetChildArmature("right hand")?.Play("hand" + hand);
            armature.GetChildArmature("dighand")?.Play("hand" + hand);
            armature.GetChildArmature("left leg")?.Play(leg);
            armature.GetChildArmature("right leg")?.Play(leg);
            armature.GetChildArmature("digleg")?.Play(leg);
            armature.Play("idle");
        }

        /// <summary>
        /// Executes Get Character Definition for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static rimrushCharacterDefinition GetCharacterDefinition(int characterId)
        {
            return CharacterDefinitions[SanitizeCharacterId(characterId)];
        }

        /// <summary>
        /// Executes Get Portrait Base Sprite for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="definition">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static Sprite GetPortraitBaseSprite(rimrushCharacterDefinition definition)
        {
            var atlas = GetPortraitAtlas();
            return atlas?.Sprite(definition.PortraitSpriteName);
        }

        /// <summary>
        /// Executes Get Portrait Atlas for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static DBLiteTextureAtlas GetPortraitAtlas()
        {
            if (portraitAtlas != null)
            {
                return portraitAtlas;
            }

            var portraitAtlasPath = rimrushAssets.Portraits.ResourcePath(rimrushAssets.Portraits.UiAtlas);
            var textureJsonAsset = Resources.Load<TextAsset>(portraitAtlasPath);
            var texture = Resources.Load<Texture2D>(portraitAtlasPath);
            if (textureJsonAsset == null || texture == null)
            {
                Debug.LogWarning("Missing UI portrait atlas resources.");
                return null;
            }

            portraitAtlas = DBLiteTextureAtlas.Parse(rimrushAssets.Portraits.UiAtlas, texture, textureJsonAsset.text);
            return portraitAtlas;
        }

        /// <summary>
        /// Executes Get Or Create Portrait Variant Sprite for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="portraitSpriteName">Input value used by this step of the workflow.</param>
        /// <param name="baseSprite">Input value used by this step of the workflow.</param>
        /// <param name="maxPixels">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static Sprite GetOrCreatePortraitVariantSprite(string portraitSpriteName, Sprite baseSprite, int maxPixels)
        {
            var safeMaxPixels = Mathf.Max(1, maxPixels);
            var cacheKey = $"{portraitSpriteName}@{safeMaxPixels}";
            if (PortraitVariantSprites.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var texture = baseSprite.texture;
            if (texture == null || !texture.isReadable)
            {
                Debug.LogWarning($"Portrait atlas texture must be readable to build portrait variants: {portraitSpriteName}");
                return baseSprite;
            }

            var variantTexture = BuildPortraitVariantTexture(texture, baseSprite.rect, safeMaxPixels, cacheKey);
            if (variantTexture == null)
            {
                return baseSprite;
            }

            var sprite = UnityEngine.Sprite.Create(
                variantTexture,
                new Rect(0f, 0f, variantTexture.width, variantTexture.height),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = cacheKey;

            PortraitVariantTextures[cacheKey] = variantTexture;
            PortraitVariantSprites[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>
        /// Executes Build Portrait Variant Texture for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="sourceTexture">Input value used by this step of the workflow.</param>
        /// <param name="sourceRect">Input value used by this step of the workflow.</param>
        /// <param name="maxPixels">Input value used by this step of the workflow.</param>
        /// <param name="textureName">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static Texture2D BuildPortraitVariantTexture(Texture2D sourceTexture, Rect sourceRect, int maxPixels, string textureName)
        {
            var sourceMaxPixels = Mathf.Max(sourceRect.width, sourceRect.height);
            if (sourceMaxPixels <= 0.0001f)
            {
                return null;
            }

            var scale = Mathf.Min(1f, maxPixels / sourceMaxPixels);
            var width = Mathf.Max(1, Mathf.RoundToInt(sourceRect.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(sourceRect.height * scale));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels32(DownsamplePortraitPixels(sourceTexture, sourceRect, width, height));
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>
        /// Executes Downsample Portrait Pixels for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="sourceTexture">Input value used by this step of the workflow.</param>
        /// <param name="sourceRect">Input value used by this step of the workflow.</param>
        /// <param name="width">Input value used by this step of the workflow.</param>
        /// <param name="height">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static Color32[] DownsamplePortraitPixels(Texture2D sourceTexture, Rect sourceRect, int width, int height)
        {
            var output = new Color32[width * height];
            var sampleCount = PortraitVariantSampleGrid * PortraitVariantSampleGrid;
            var inverseSampleCount = 1f / sampleCount;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var accumulatedAlpha = 0f;
                    var accumulatedRed = 0f;
                    var accumulatedGreen = 0f;
                    var accumulatedBlue = 0f;

                    for (var sampleY = 0; sampleY < PortraitVariantSampleGrid; sampleY++)
                    {
                        var v = (y + (sampleY + 0.5f) / PortraitVariantSampleGrid) / height;
                        for (var sampleX = 0; sampleX < PortraitVariantSampleGrid; sampleX++)
                        {
                            var u = (x + (sampleX + 0.5f) / PortraitVariantSampleGrid) / width;
                            var sourceU = (sourceRect.x + u * sourceRect.width) / sourceTexture.width;
                            var sourceV = (sourceRect.y + v * sourceRect.height) / sourceTexture.height;
                            var color = sourceTexture.GetPixelBilinear(sourceU, sourceV);
                            accumulatedAlpha += color.a;
                            accumulatedRed += color.r * color.a;
                            accumulatedGreen += color.g * color.a;
                            accumulatedBlue += color.b * color.a;
                        }
                    }

                    var alpha = accumulatedAlpha * inverseSampleCount;
                    var index = y * width + x;
                    if (alpha <= 0.0001f)
                    {
                        output[index] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    var normalizedAlpha = Mathf.Clamp01(alpha);
                    var rgbDivisor = Mathf.Max(accumulatedAlpha, 0.0001f);
                    output[index] = new Color(
                        Mathf.Clamp01(accumulatedRed / rgbDivisor),
                        Mathf.Clamp01(accumulatedGreen / rgbDivisor),
                        Mathf.Clamp01(accumulatedBlue / rgbDivisor),
                        normalizedAlpha);
                }
            }

            return output;
        }

        /// <summary>
        /// Executes Apply Character Tuning for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="armature">Input value used by this step of the workflow.</param>
        /// <param name="definition">Input value used by this step of the workflow.</param>
        private static void ApplyCharacterTuning(DBLiteArmature armature, rimrushCharacterDefinition definition)
        {
            if (armature == null)
            {
                return;
            }

            var head = armature.GetChildArmature("head");
            if (head != null)
            {
                var headPosition = head.transform.localPosition;
                headPosition.x = definition.HeadOffsetX;
                headPosition.y = definition.HeadOffsetY;
                headPosition.z = 0f;
                head.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(head.transform.parent, headPosition);
                head.transform.localScale = new Vector3(definition.HeadScale, definition.HeadScale, 1f);
            }

            var body = armature.GetChildArmature("body");
            if (body != null)
            {
                var bodyPosition = body.transform.localPosition;
                bodyPosition.y = 0f;
                bodyPosition.z = 0f;
                body.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(body.transform.parent, bodyPosition);
                body.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Executes Is Character Enabled for the rimrushCharacterDefinition workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private static bool IsCharacterEnabled(int characterId)
        {
            return characterId >= 0
                && characterId < CharacterDefinitions.Length
                && CharacterDefinitions[characterId].Enabled;
        }
    }
}
