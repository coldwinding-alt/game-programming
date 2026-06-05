// 角色数据和动画管理
// 定义 8 个角色的属性（名字、头像、动画骨骼），负责加载角色模型、应用外观、播放动画。创建球员时都会来这里获取角色信息。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    public static class mlpPlayersData
    {
        private const int ActiveCharacterSkinCount = 8;
        private const float PortraitAtlasSourceScale = 4f;
        private const int PortraitCropPaddingPixels = 6;
        private const byte PortraitVisibleAlphaThreshold = 8;
        private const float GlobalCharacterModelScaleMultiplier = 1.08f;

        private sealed class mlpCharacterDefinition
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
        private static readonly Dictionary<string, Sprite> PortraitDisplaySprites = new Dictionary<string, Sprite>();

        private static readonly mlpCharacterDefinition[] CharacterDefinitions =
        {
            new mlpCharacterDefinition { DisplayName = "REAPER", SkinIndex = 0, FormIndex = 0, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_pumpkin", HeadOffsetX = 0.75f, HeadOffsetY = 9f, HeadScale = 1.02f, ModelScaleMultiplier = 1.08f, PreviewScaleMultiplier = 1f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 8f },
            new mlpCharacterDefinition { DisplayName = "GHOST CLOWN", SkinIndex = 1, FormIndex = 1, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_frankenstein", HeadOffsetX = 4.5f, HeadOffsetY = 1f, HeadScale = 1f, ModelScaleMultiplier = 1.06f, PreviewScaleMultiplier = 0.99f, PortraitScaleMultiplier = 0.98f, PortraitOffsetY = 9f },
            new mlpCharacterDefinition { DisplayName = "SKULL PIRATE", SkinIndex = 2, FormIndex = 2, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_mummy", HeadOffsetX = 1.5f, HeadOffsetY = 0f, HeadScale = 1.02f, ModelScaleMultiplier = 1.07f, PreviewScaleMultiplier = 1f, PreviewOffsetY = -2f, PortraitScaleMultiplier = 0.98f, PortraitOffsetY = 9f },
            new mlpCharacterDefinition { DisplayName = "VAMPIRE", SkinIndex = 3, FormIndex = 3, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_vampire", HeadOffsetY = -10.5f, HeadScale = 0.95f, PreviewScaleMultiplier = 0.96f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 12f },
            new mlpCharacterDefinition { DisplayName = "CANDLEMAN", SkinIndex = 4, FormIndex = 4, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_candle", HeadOffsetX = 2.75f, HeadOffsetY = 6f, HeadScale = 0.96f, PreviewScaleMultiplier = 0.94f, PortraitScaleMultiplier = 0.85f, PortraitOffsetY = -9f },
            new mlpCharacterDefinition { DisplayName = "SCARECROW", SkinIndex = 5, FormIndex = 5, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_scarecrow", HeadOffsetY = 7f, HeadScale = 1.05f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.05f, PortraitOffsetY = -10f },
            new mlpCharacterDefinition { DisplayName = "WITCH", SkinIndex = 6, FormIndex = 6, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_witch", HeadOffsetX = 3.5f, HeadOffsetY = 8f, HeadScale = 1.1f, PreviewScaleMultiplier = 0.98f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.12f, PortraitOffsetY = -9f },
            new mlpCharacterDefinition { DisplayName = "BLACK CAT", SkinIndex = 7, FormIndex = 7, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_blackcat", HeadOffsetX = 6f, HeadOffsetY = 7f, HeadScale = 0.99f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 1f, PortraitScaleMultiplier = 0.96f, PortraitOffsetY = -5f }
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
        /// Initialize the player character system. Called once at startup to prepare character data.
        /// </summary>
        public static void SetupPlayers()
        {
            // Active Halloween characters now use an explicit 8-character DragonBones set.
        }

        /// <summary>
        /// Load and build a DragonBones armature for use in gameplay.
        /// </summary>
        /// <param name="name">The armature name to build (e.g. a character skeleton name).</param>
        /// <returns>A new DBLiteArmature ready to animate.</returns>
        public static DBLiteArmature BuildGameplayArmature(string name)
        {
            DBLiteFactory.Instance.EnsureLoaded();
            return DBLiteFactory.Instance.BuildArmature("playerSmall", name);
        }

        /// <summary>
        /// Return an array of character IDs for all currently enabled characters.
        /// </summary>
        /// <returns>An array of enabled character indices.</returns>
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
        /// Make sure a character ID is valid and enabled. If not, return the fallback or the first enabled character.
        /// </summary>
        /// <param name="requestedCharacterId">The character ID the caller wants.</param>
        /// <param name="fallbackCharacterId">Backup ID to use if the requested one is disabled.</param>
        /// <returns>A valid, enabled character ID.</returns>
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
        /// Move to the next or previous enabled character in the list, wrapping around at the ends.
        /// </summary>
        /// <param name="currentCharacterId">The currently selected character ID.</param>
        /// <param name="direction">+1 for next, -1 for previous.</param>
        /// <returns>The ID of the next (or previous) enabled character.</returns>
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
        /// Return the display name for a character (e.g. "REAPER", "WITCH").
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The character's display name.</returns>
        public static string GetCharacterName(int characterId)
        {
            return GetCharacterDefinition(characterId).DisplayName;
        }

        /// <summary>
        /// Return the body form index for a character. Used to select the correct body animation.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The form index used for body animations.</returns>
        public static int GetCharacterFormIndex(int characterId)
        {
            return GetCharacterDefinition(characterId).FormIndex;
        }

        /// <summary>
        /// Return the super skill ID for a character. Maps to a mlpCharacterSkillType entry.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The super skill ID.</returns>
        public static int GetCharacterSuperId(int characterId)
        {
            return GetCharacterDefinition(characterId).SuperId;
        }

        /// <summary>
        /// Return the scale multiplier for a character's preview model in menus and selection screens.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The combined preview scale multiplier.</returns>
        public static float GetCharacterPreviewScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return definition.PreviewScaleMultiplier * GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// Return the scale multiplier for a character's model during actual gameplay.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The combined gameplay scale multiplier.</returns>
        public static float GetCharacterGameplayScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// Return the vertical offset for a character's preview model in menus.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The Y offset in pixels.</returns>
        public static float GetCharacterPreviewOffsetY(int characterId)
        {
            return GetCharacterDefinition(characterId).PreviewOffsetY;
        }

        /// <summary>
        /// Get the cropped portrait sprite for a character, creating and caching it if needed.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <param name="desiredMaxPixels">Optional max pixel size hint (currently unused).</param>
        /// <returns>A cropped portrait Sprite, or null if the atlas is missing.</returns>
        public static Sprite GetCharacterPortraitSprite(int characterId, float desiredMaxPixels = 0f)
        {
            var definition = GetCharacterDefinition(characterId);
            var baseSprite = GetPortraitBaseSprite(definition);
            if (baseSprite == null)
            {
                return null;
            }

            return GetOrCreatePortraitDisplaySprite(definition.PortraitSpriteName, baseSprite);
        }

        /// <summary>
        /// Return the scale multiplier used when displaying a character's portrait in the UI.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The portrait scale multiplier.</returns>
        public static float GetCharacterPortraitScaleMultiplier(int characterId)
        {
            return GetCharacterDefinition(characterId).PortraitScaleMultiplier;
        }

        /// <summary>
        /// Return the vertical offset for a character's portrait sprite in the UI. Adjusts based on sprite size if provided.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <param name="portraitSprite">Optional sprite to scale the offset relative to the base portrait size.</param>
        /// <returns>The Y offset in source-sprite pixels (scaled by atlas source scale).</returns>
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
        /// Apply a character's skin, form, and position tuning to an armature. Used when spawning a player.
        /// </summary>
        /// <param name="armature">The armature to configure.</param>
        /// <param name="characterId">The character index whose appearance to apply.</param>
        public static void ApplyCharacter(DBLiteArmature armature, int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            SwitchPlayer(armature, definition.SkinIndex, definition.FormIndex);
            ApplyCharacterTuning(armature, definition);
        }

        /// <summary>
        /// Pick a random enabled character, optionally excluding specific ones. Used for AI opponent selection.
        /// </summary>
        /// <param name="excludedCharacterIds">Character IDs to skip (e.g. the player's choice).</param>
        /// <returns>A random enabled character ID.</returns>
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
        /// Switch an armature's head, body, hands, and legs to match a skin and form. Plays the idle animation after.
        /// </summary>
        /// <param name="armature">The armature to update.</param>
        /// <param name="skinId">The skin index (0-7) that controls head, hands, and legs.</param>
        /// <param name="formId">The body form index that controls the body animation.</param>
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
        /// Look up the internal character definition by ID, sanitizing the ID first.
        /// </summary>
        /// <param name="characterId">The character index.</param>
        /// <returns>The matching mlpCharacterDefinition.</returns>
        private static mlpCharacterDefinition GetCharacterDefinition(int characterId)
        {
            return CharacterDefinitions[SanitizeCharacterId(characterId)];
        }

        /// <summary>
        /// Get the raw (uncropped) portrait sprite from the atlas for a character definition.
        /// </summary>
        /// <param name="definition">The character definition to look up.</param>
        /// <returns>The base sprite from the portrait atlas, or null if not found.</returns>
        private static Sprite GetPortraitBaseSprite(mlpCharacterDefinition definition)
        {
            var atlas = GetPortraitAtlas();
            return atlas?.Sprite(definition.PortraitSpriteName);
        }

        /// <summary>
        /// Load and cache the portrait texture atlas from Resources. Returns null if assets are missing.
        /// </summary>
        /// <returns>The cached DBLiteTextureAtlas, or null on load failure.</returns>
        private static DBLiteTextureAtlas GetPortraitAtlas()
        {
            if (portraitAtlas != null)
            {
                return portraitAtlas;
            }

            var portraitAtlasPath = mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas);
            var textureJsonAsset = Resources.Load<TextAsset>(portraitAtlasPath);
            var texture = Resources.Load<Texture2D>(portraitAtlasPath);
            if (textureJsonAsset == null || texture == null)
            {
                Debug.LogWarning("Missing UI portrait atlas resources.");
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            portraitAtlas = DBLiteTextureAtlas.Parse(mlpAssets.Portraits.UiAtlas, texture, textureJsonAsset.text);
            return portraitAtlas;
        }

        /// <summary>
        /// Get or create a cropped portrait sprite for UI display. Caches the result so it only crops once per character.
        /// </summary>
        /// <param name="portraitSpriteName">The sprite name used as the cache key.</param>
        /// <param name="baseSprite">The original uncropped atlas sprite to crop.</param>
        /// <returns>The cropped and cached portrait sprite.</returns>
        private static Sprite GetOrCreatePortraitDisplaySprite(string portraitSpriteName, Sprite baseSprite)
        {
            if (PortraitDisplaySprites.TryGetValue(portraitSpriteName, out var cached))
            {
                return cached;
            }

            var texture = baseSprite.texture;
            if (texture == null || !texture.isReadable)
            {
                Debug.LogWarning($"Portrait atlas texture must be readable to crop UI portraits: {portraitSpriteName}");
                return baseSprite;
            }

            var visibleRect = CalculatePortraitVisibleRect(texture, baseSprite.rect);
            if (visibleRect.width <= 0.0001f || visibleRect.height <= 0.0001f)
            {
                return baseSprite;
            }

            var baseCenter = baseSprite.rect.center;
            var pivot = new Vector2(
                Mathf.InverseLerp(visibleRect.xMin, visibleRect.xMax, baseCenter.x),
                Mathf.InverseLerp(visibleRect.yMin, visibleRect.yMax, baseCenter.y));
            var sprite = UnityEngine.Sprite.Create(
                texture,
                visibleRect,
                pivot,
                baseSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{portraitSpriteName}_ui_crop";

            PortraitDisplaySprites[portraitSpriteName] = sprite;
            return sprite;
        }

        /// <summary>
        /// Scan the portrait texture pixels to find the bounding box of all visible (non-transparent) pixels. Adds padding.
        /// </summary>
        /// <param name="sourceTexture">The texture containing the portrait pixels.</param>
        /// <param name="sourceRect">The original sprite rect within the texture.</param>
        /// <returns>A tight Rect around the visible pixels, with padding added.</returns>
        private static Rect CalculatePortraitVisibleRect(Texture2D sourceTexture, Rect sourceRect)
        {
            var xStart = Mathf.Clamp(Mathf.FloorToInt(sourceRect.xMin), 0, sourceTexture.width - 1);
            var xEnd = Mathf.Clamp(Mathf.CeilToInt(sourceRect.xMax), xStart + 1, sourceTexture.width);
            var yStart = Mathf.Clamp(Mathf.FloorToInt(sourceRect.yMin), 0, sourceTexture.height - 1);
            var yEnd = Mathf.Clamp(Mathf.CeilToInt(sourceRect.yMax), yStart + 1, sourceTexture.height);
            var pixels = sourceTexture.GetPixels32();
            var minX = xEnd;
            var maxX = xStart - 1;
            var minY = yEnd;
            var maxY = yStart - 1;

            for (var y = yStart; y < yEnd; y++)
            {
                var rowOffset = y * sourceTexture.width;
                for (var x = xStart; x < xEnd; x++)
                {
                    if (pixels[rowOffset + x].a <= PortraitVisibleAlphaThreshold)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return sourceRect;
            }

            minX = Mathf.Max(xStart, minX - PortraitCropPaddingPixels);
            maxX = Mathf.Min(xEnd - 1, maxX + PortraitCropPaddingPixels);
            minY = Mathf.Max(yStart, minY - PortraitCropPaddingPixels);
            maxY = Mathf.Min(yEnd - 1, maxY + PortraitCropPaddingPixels);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Adjust the head and body transforms on an armature to match a character's position and scale tuning.
        /// </summary>
        /// <param name="armature">The armature to tune.</param>
        /// <param name="definition">The character definition with offset and scale values.</param>
        private static void ApplyCharacterTuning(DBLiteArmature armature, mlpCharacterDefinition definition)
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
                head.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(head.transform.parent, headPosition);
                head.transform.localScale = new Vector3(definition.HeadScale, definition.HeadScale, 1f);
            }

            var body = armature.GetChildArmature("body");
            if (body != null)
            {
                var bodyPosition = body.transform.localPosition;
                bodyPosition.y = 0f;
                bodyPosition.z = 0f;
                body.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(body.transform.parent, bodyPosition);
                body.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Check whether a character ID is in range and marked as enabled.
        /// </summary>
        /// <param name="characterId">The character index to check.</param>
        /// <returns>True if the character exists and is enabled.</returns>
        private static bool IsCharacterEnabled(int characterId)
        {
            return characterId >= 0
                && characterId < CharacterDefinitions.Length
                && CharacterDefinitions[characterId].Enabled;
        }
    }
}
