// Character data and animation management
// Define the attributes of 8 characters (name, avatar, animation skeleton), and be responsible for loading character models, applying appearance, and playing animation. When creating a player, you will come here to get character information.

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Character data manager: defines the attributes of 8 characters (name, avatar, animation skeleton), and is responsible for loading character models, applying appearance, and playing animations.
    /// </summary>
    public static class mlpPlayersData
    {
        private const int ActiveCharacterSkinCount = 8;              // Total number of character skins (8 characters in total)

        private const float PortraitAtlasSourceScale = 4f;           // Source scaling factor for the avatar atlas, used to convert offsets into pixel units

        private const int PortraitCropPaddingPixels = 6;             // Padding (in pixels) left around the visible area when cropping the avatar
        private const byte PortraitVisibleAlphaThreshold = 8;        // Pixel transparency threshold, below which the value is considered transparent (0-255)

        private const float GlobalCharacterModelScaleMultiplier = 1.08f; // Global character model scaling multiplier, all characters will be multiplied by this value


        private sealed class mlpCharacterDefinition
        {
            public string DisplayName;                  // Role display name (e.g. "REAPER", "WITCH")

            public int SkinIndex;                       // Skin index (0-7), determines the animated appearance of the head, hands, and legs

            public int FormIndex;                       // Body shape index, determines the style of body animation

            public int SuperId;                         // Special skill ID, corresponding to the skill type in mlpCharacterSkillType
            public bool Enabled;                        // Whether to enable this role (false means it will be hidden in the selection interface)

            public string PortraitSpriteName;           // The name of the elf whose avatar is in the album

            public float HeadOffsetX;                   // X-axis offset of head relative to body (pixels)

            public float HeadOffsetY;                   // Y-axis offset of head relative to body (pixels)

            public float HeadScale = 1f;                // Head scaling factor (1 is original size)

            public float ModelScaleMultiplier = 1f;     // The overall scaling factor of the model (will be multiplied by the global scaling factor)

            public float PreviewScaleMultiplier = 1f;   // Model zoom factor in preview interface (menu/casting)

            public float PreviewOffsetY;                // Y-axis offset of the model in the preview interface (pixels)

            public float PortraitScaleMultiplier = 1f;  // The zoom factor when the avatar is displayed in the UI

            public float PortraitOffsetY;               // The Y-axis offset of the avatar in the UI (source sprite pixel unit, multiplied by the atlas scaling factor)

        }

        private static DBLiteTextureAtlas portraitAtlas;                                                // Cached avatar texture atlas (reused after first load)

        private static readonly Dictionary<string, Sprite> PortraitDisplaySprites = new Dictionary<string, Sprite>(); // Cache of cropped avatar sprites (indexed by sprite name to avoid repeated cropping)

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

        private static readonly int[] Hands = { 1, 2, 3, 4, 5, 6, 7, 8 }; // Hand animation number corresponding to each character (used for DragonBones changing hands)

        private static readonly string[] Legs =                            // The leg animation name corresponding to each character (used for DragonBones leg replacement)

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

        public static int CharacterCount => CharacterDefinitions.Length; // Total number of roles (read-only attribute)

        /// <summary>
        /// Initialize the player character system. Called once when the game starts, all character data is ready.
        /// </summary>
        public static void SetupPlayers()
        {
            // The current Halloween characters use the explicit 8-character DragonBones skeleton set.
        }

        /// <summary>
        /// Load and build a DragonBones skeleton for use in combat. The returned skeleton can be animated directly.
        /// </summary>
        /// <param name="name">The name of the skeleton to build (e.g. character bone name). </param>
        /// <returns>The completed DBLiteArmature instance can be used for animation playback. </returns>
        public static DBLiteArmature BuildGameplayArmature(string name)
        {
            DBLiteFactory.Instance.EnsureLoaded();
            return DBLiteFactory.Instance.BuildArmature("playerSmall", name);
        }

        /// <summary>
        /// Gets an array of IDs for all enabled roles. Used for character selection interface or random assignment of characters.
        /// </summary>
        /// <returns>An array of indices of enabled roles. </returns>
        public static int[] GetActiveCharacterIds()
        {
            // 1. Create a temporary list to store the numbers of all enabled roles

            var active = new List<int>(CharacterDefinitions.Length);
            // 2. Traverse all role definitions and add the enabled role numbers to the list
            for (var i = 0; i < CharacterDefinitions.Length; i++)
            {
                if (CharacterDefinitions[i].Enabled)
                {
                    active.Add(i);
                }
            }

            // 3. Convert the list into an array and return it
            return active.ToArray();
        }

        /// <summary>
        /// Verify that the role ID is valid and enabled. If the requested role is not available, an alternate role or the first available role is returned.
        /// </summary>
        /// <param name="requestedCharacterId">The character ID desired by the caller. </param>
        /// <param name="fallbackCharacterId">The fallback character ID to use when the requested character is disabled. </param>
        /// <returns>A valid and enabled role ID. </returns>
        public static int SanitizeCharacterId(int requestedCharacterId, int fallbackCharacterId = 0)
        {
            // 1. The requested role is valid and enabled, return directly
            if (IsCharacterEnabled(requestedCharacterId))
            {
                return requestedCharacterId;
            }

            // 2. Requested unavailable, try alternate role

            if (IsCharacterEnabled(fallbackCharacterId))
            {
                return fallbackCharacterId;
            }

            // 3. Backup is also unavailable, return to the first enabled role

            var active = GetActiveCharacterIds();
            return active.Length > 0 ? active[0] : 0;
        }

        /// <summary>
        /// Switch to the next or previous enabled character in the list, automatically looping to the beginning when the end is reached.
        /// </summary>
        /// <param name="currentCharacterId">The currently selected character ID. </param>
        /// <param name="direction">+1 means next, -1 means previous. </param>
        /// <returns>The ID of the next (or previous) enabled role. </returns>
        public static int StepCharacterId(int currentCharacterId, int direction)
        {
            // 1. Get a numbered list of all enabled roles

            var active = GetActiveCharacterIds();
            if (active.Length == 0)
            {
                return 0;
            }

            // 2. Find the position (index) of the current character in the list

            var currentIndex = 0;
            for (var i = 0; i < active.Length; i++)
            {
                if (active[i] == currentCharacterId)
                {
                    currentIndex = i;
                    break;
                }
            }

            // 3. Calculate the next position based on the direction (+1 next, -1 previous), and loop back when it exceeds the range.
            var nextIndex = (currentIndex + direction) % active.Length;
            if (nextIndex < 0)
            {
                nextIndex += active.Length;
            }

            // 4. Return the number of the next character
            return active[nextIndex];
        }

        /// <summary>
        /// Get the display name of the role, such as "REAPER", "WITCH", etc.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>The display name of the role. </returns>
        public static string GetCharacterName(int characterId)
        {
            return GetCharacterDefinition(characterId).DisplayName;
        }

        /// <summary>
        /// Gets the character's body shape index, used to select the correct body animation.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>The body shape index used for body animation. </returns>
        public static int GetCharacterFormIndex(int characterId)
        {
            return GetCharacterDefinition(characterId).FormIndex;
        }

        /// <summary>
        /// Get the character's special move ID, corresponding to the skill type in mlpCharacterSkillType.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>Nirvana ID. </returns>
        public static int GetCharacterSuperId(int characterId)
        {
            return GetCharacterDefinition(characterId).SuperId;
        }

        /// <summary>
        /// Gets the zoom factor of the character preview model, which is used for model display in menus and character selection interfaces.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>Comprehensive preview zoom factor. </returns>
        public static float GetCharacterPreviewScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return definition.PreviewScaleMultiplier * GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// Get the character's model scaling factor in actual combat.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>Actual comprehensive scaling factor. </returns>
        public static float GetCharacterGameplayScaleMultiplier(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            return GlobalCharacterModelScaleMultiplier * definition.ModelScaleMultiplier;
        }

        /// <summary>
        /// Gets the vertical offset of the character preview model in the menu.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>Y-axis offset (pixels). </returns>
        public static float GetCharacterPreviewOffsetY(int characterId)
        {
            return GetCharacterDefinition(characterId).PreviewOffsetY;
        }

        /// <summary>
        /// Get the character's cropped avatar sprite, which will be automatically created and cached when called for the first time.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <param name="desiredMaxPixels">Optional maximum pixel size hint (currently not used). </param>
        /// <returns>The cropped avatar sprite, or null if the atlas is missing. </returns>
        public static Sprite GetCharacterPortraitSprite(int characterId, float desiredMaxPixels = 0f)
        {
            // 1. Get the character definition and original avatar sprite
            var definition = GetCharacterDefinition(characterId);
            var baseSprite = GetPortraitBaseSprite(definition);
            // 2. If the avatar cannot be found in the album, null will be returned.

            if (baseSprite == null)
            {
                return null;
            }

            // 3. Obtain or create the cropped avatar sprite (it will be automatically cropped and cached for the first time)

            return GetOrCreatePortraitDisplaySprite(definition.PortraitSpriteName, baseSprite);
        }

        /// <summary>
        /// Gets the zoom factor of the character's avatar when displayed in the UI.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>Avatar zoom multiple. </returns>
        public static float GetCharacterPortraitScaleMultiplier(int characterId)
        {
            return GetCharacterDefinition(characterId).PortraitScaleMultiplier;
        }

        /// <summary>
        /// Gets the vertical offset of the character's avatar sprite in the UI. If a sprite is provided, the offset is automatically adjusted based on the sprite size.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <param name="portraitSprite">Optional sprite used to scale offset relative to base avatar size. </param>
        /// <returns>Y-axis offset (source sprite pixel units, multiplied by the atlas source scaling factor). </returns>
        public static float GetCharacterPortraitOffsetY(int characterId, Sprite portraitSprite = null)
        {
            // 1. Obtain the character definition and calculate the basic offset (multiply the atlas zoom factor to convert to pixel units)

            var definition = GetCharacterDefinition(characterId);
            var baseOffset = definition.PortraitOffsetY * PortraitAtlasSourceScale;
            // 2. If no reference sprite is provided, the base offset is returned directly.
            if (portraitSprite == null)
            {
                return baseOffset;
            }

            // 3. Get the original avatar sprite in the atlas

            var baseSprite = GetPortraitBaseSprite(definition);
            if (baseSprite == null)
            {
                return baseOffset;
            }

            // 4. Compare the size of the original sprite and the reference sprite, and scale the offset proportionally

            var baseMaxPixels = Mathf.Max(baseSprite.rect.width, baseSprite.rect.height);
            var spriteMaxPixels = Mathf.Max(portraitSprite.rect.width, portraitSprite.rect.height);
            if (baseMaxPixels <= 0.0001f || spriteMaxPixels <= 0.0001f)
            {
                return baseOffset;
            }

            // 5. Scale the offset using the size ratio of the reference sprite to the original sprite
            return baseOffset * (spriteMaxPixels / baseMaxPixels);
        }

        /// <summary>
        /// Apply your character's skin, size, and position adjustments to the skeleton. Called when a player is spawned.
        /// </summary>
        /// <param name="armature">The armature to configure. </param>
        /// <param name="characterId">The character index to which the appearance is to be applied. </param>
        public static void ApplyCharacter(DBLiteArmature armature, int characterId)
        {
            // 1. Get role definition

            var definition = GetCharacterDefinition(characterId);
            // 2. Switch skin and body animation

            SwitchPlayer(armature, definition.SkinIndex, definition.FormIndex);
            // 3. Adjust the position and scaling of the head and body
            ApplyCharacterTuning(armature, definition);
        }

        /// <summary>
        /// Excludes specified roles by selecting an enabled role at random. Used for AI opponent selection.
        /// </summary>
        /// <param name="excludedCharacterIds">List of character IDs to exclude (e.g. characters the player has selected). </param>
        /// <returns>A randomly selected enabled character ID. </returns>
        public static int GetRandomCharacterId(IList<int> excludedCharacterIds = null)
        {
            // 1. Create a candidate list and filter out all roles that are "enabled" and "not in the excluded list"

            var candidates = new List<int>(CharacterDefinitions.Length);
            for (var i = 0; i < CharacterDefinitions.Length; i++)
            {
                // Skip disabled characters

                if (!CharacterDefinitions[i].Enabled)
                {
                    continue;
                }

                // Skip characters that need to be excluded (for example, the player has already chosen this character)
                if (excludedCharacterIds != null && excludedCharacterIds.Contains(i))
                {
                    continue;
                }

                candidates.Add(i);
            }

            // 2. If no candidate role is available, return a safe default value
            if (candidates.Count == 0)
            {
                return SanitizeCharacterId(0);
            }

            // 3. Randomly select one from the candidate list and return it
            return candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// Switch the skeleton's head, body, hands, and legs to match the specified skin and body type. The standby animation will automatically play after the switch is completed.
        /// </summary>
        /// <param name="armature">The armature to update. </param>
        /// <param name="skinId">Skin index (0-7), controls the appearance of the head, hands and legs. </param>
        /// <param name="formId">Body index, control body animation. </param>
        public static void SwitchPlayer(DBLiteArmature armature, int skinId, int formId)
        {
            if (armature == null)
            {
                return;
            }

            // 1. Limit the skin number and body number to the valid range

            skinId = Mathf.Clamp(skinId, 0, ActiveCharacterSkinCount - 1);
            formId = Mathf.Max(0, formId);

            // 2. Find the corresponding hand and leg animation names based on the skin number

            var hand = Hands[skinId];
            var leg = Legs[skinId];

            // 3. Switch the animations of the head, body, left hand, right hand, and digging hand respectively.
            armature.GetChildArmature("head")?.Play("head" + (skinId + 1));
            armature.GetChildArmature("body")?.Play("body" + (formId + 1));
            armature.GetChildArmature("left hand")?.Play("hand" + hand);
            armature.GetChildArmature("right hand")?.Play("hand" + hand);
            armature.GetChildArmature("dighand")?.Play("hand" + hand);
            // 4. Switch the animation of the left leg, right leg and digging leg

            armature.GetChildArmature("left leg")?.Play(leg);
            armature.GetChildArmature("right leg")?.Play(leg);
            armature.GetChildArmature("digleg")?.Play(leg);
            // 5. Play the standby animation to make the character stand.

            armature.Play("idle");
        }

        /// <summary>
        /// When looking up internal role definitions based on ID, the validity of the ID will be verified first.
        /// </summary>
        /// <param name="characterId">Character index. </param>
        /// <returns>The matching mlpCharacterDefinition instance. </returns>
        private static mlpCharacterDefinition GetCharacterDefinition(int characterId)
        {
            return CharacterDefinitions[SanitizeCharacterId(characterId)];
        }

        /// <summary>
        /// Get the character's original (uncropped) avatar sprite from the gallery.
        /// </summary>
        /// <param name="definition">The role definition to find. </param>
        /// <returns>The base sprite in the avatar atlas, or null if not found. </returns>
        private static Sprite GetPortraitBaseSprite(mlpCharacterDefinition definition)
        {
            var atlas = GetPortraitAtlas();
            return atlas?.Sprite(definition.PortraitSpriteName);
        }

        /// <summary>
        /// Load and cache the avatar texture atlas from the Resources folder. Returns null if the resource is missing.
        /// </summary>
        /// <returns>The cached DBLiteTextureAtlas instance, returns null if loading fails. </returns>
        private static DBLiteTextureAtlas GetPortraitAtlas()
        {
            // 1. If the avatar atlas has been loaded, the cached result will be returned directly.

            if (portraitAtlas != null)
            {
                return portraitAtlas;
            }

            // 2. Splice the resource path of the image atlas, load the texture and JSON configuration file

            var portraitAtlasPath = mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas);
            var textureJsonAsset = Resources.Load<TextAsset>(portraitAtlasPath);
            var texture = Resources.Load<Texture2D>(portraitAtlasPath);
            // 3. If the resource file is missing, output a warning and return empty
            if (textureJsonAsset == null || texture == null)
            {
                Debug.LogWarning("Missing UI portrait atlas resources.");
                return null;
            }

            // 4. Set the filtering mode of the texture to "Point Sampling" (to keep the pixel style clear), and the wrapping mode to "Clamping" (edges are not repeated)
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            // 5. Parse the JSON configuration, combine the texture and configuration into an atlas object, and cache it
            portraitAtlas = DBLiteTextureAtlas.Parse(mlpAssets.Portraits.UiAtlas, texture, textureJsonAsset.text);
            return portraitAtlas;
        }

        /// <summary>
        /// Gets or creates a cropped avatar sprite for UI display. The results are cached and each character is only cropped once.
        /// </summary>
        /// <param name="portraitSpriteName">The name of the sprite to use as the cache key. </param>
        /// <param name="baseSprite">The original uncropped atlas sprite to be cropped. </param>
        /// <returns>The cropped and cached avatar sprite. </returns>
        private static Sprite GetOrCreatePortraitDisplaySprite(string portraitSpriteName, Sprite baseSprite)
        {
            // 1. If the avatar has been cropped and cached, directly return the cached result.

            if (PortraitDisplaySprites.TryGetValue(portraitSpriteName, out var cached))
            {
                return cached;
            }

            // 2. Get the texture where the avatar is located and confirm that the texture can read pixel data

            var texture = baseSprite.texture;
            if (texture == null || !texture.isReadable)
            {
                Debug.LogWarning($"Portrait atlas texture must be readable to crop UI portraits: {portraitSpriteName}");
                return baseSprite;
            }

            // 3. Scan the pixels and calculate the bounding box of the visible area of the avatar.

            var visibleRect = CalculatePortraitVisibleRect(texture, baseSprite.rect);
            if (visibleRect.width <= 0.0001f || visibleRect.height <= 0.0001f)
            {
                return baseSprite;
            }

            // 4. Calculate the center point (anchor point) of the cropped sprite and keep it in its original position

            var baseCenter = baseSprite.rect.center;
            var pivot = new Vector2(
                Mathf.InverseLerp(visibleRect.xMin, visibleRect.xMax, baseCenter.x),
                Mathf.InverseLerp(visibleRect.yMin, visibleRect.yMax, baseCenter.y));
            // 5. Create a new sprite containing only the visible area

            var sprite = UnityEngine.Sprite.Create(
                texture,
                visibleRect,
                pivot,
                baseSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{portraitSpriteName}_ui_crop";

            // 6. Store the cropped sprite in the cache and use it directly next time
            PortraitDisplaySprites[portraitSpriteName] = sprite;
            return sprite;
        }

        /// <summary>
        /// Scan the pixels of the avatar texture, find the bounding box of all visible (non-transparent) pixels, and add padding.
        /// </summary>
        /// <param name="sourceTexture">Texture containing avatar pixels. </param>
        /// <param name="sourceRect">The rectangular area of ​​the original sprite in the texture. </param>
        /// <returns>A compact rectangle surrounding the visible pixels, with padding added. </returns>
        private static Rect CalculatePortraitVisibleRect(Texture2D sourceTexture, Rect sourceRect)
        {
            // 1. Determine the pixel range to be scanned (the rectangular area of the original sprite in the texture)

            var xStart = Mathf.Clamp(Mathf.FloorToInt(sourceRect.xMin), 0, sourceTexture.width - 1);
            var xEnd = Mathf.Clamp(Mathf.CeilToInt(sourceRect.xMax), xStart + 1, sourceTexture.width);
            var yStart = Mathf.Clamp(Mathf.FloorToInt(sourceRect.yMin), 0, sourceTexture.height - 1);
            var yEnd = Mathf.Clamp(Mathf.CeilToInt(sourceRect.yMax), yStart + 1, sourceTexture.height);
            // 2. Read the color data of all pixels in the texture at once
            var pixels = sourceTexture.GetPixels32();
            // 3. Initialize the boundary value to the "least likely" initial state to facilitate subsequent updates using Min/Max

            var minX = xEnd;
            var maxX = xStart - 1;
            var minY = yEnd;
            var maxY = yStart - 1;

            // 4. Scan each pixel row by row and column by column, and find the leftmost, rightmost, topmost, and bottommost positions of all opaque pixels.

            for (var y = yStart; y < yEnd; y++)
            {
                var rowOffset = y * sourceTexture.width;
                for (var x = xStart; x < xEnd; x++)
                {
                    // Skip transparent and semi-transparent pixels (those with transparency below the threshold are considered invisible)

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

            // 5. If no visible pixels are found, return directly to the original area

            if (maxX < minX || maxY < minY)
            {
                return sourceRect;
            }

            // 6. Add some padding (white space) around the visible area to prevent cropping too tightly

            minX = Mathf.Max(xStart, minX - PortraitCropPaddingPixels);
            maxX = Mathf.Min(xEnd - 1, maxX + PortraitCropPaddingPixels);
            minY = Mathf.Max(yStart, minY - PortraitCropPaddingPixels);
            maxY = Mathf.Min(yEnd - 1, maxY + PortraitCropPaddingPixels);
            // 7. Return the final cropping rectangle (x, y, width, height)
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Adjust the head and body transformations on the skeleton to match the character's position and scale settings.
        /// </summary>
        /// <param name="armature">The armature to adjust. </param>
        /// <param name="definition">Character definition containing offset and scale values. </param>
        private static void ApplyCharacterTuning(DBLiteArmature armature, mlpCharacterDefinition definition)
        {
            if (armature == null)
            {
                return;
            }

            // 1. Adjust the position and size of the head so that it aligns to the pixel grid (to prevent blurring)

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

            // 2. Adjust the position of the body (reset Y and Z to 0), also aligning to the pixel grid

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
        /// Check that the role ID is within a valid range and has been marked enabled.
        /// </summary>
        /// <param name="characterId">The character index to check. </param>
        /// <returns>Returns true if the role exists and is enabled. </returns>
        private static bool IsCharacterEnabled(int characterId)
        {
            return characterId >= 0
                && characterId < CharacterDefinitions.Length
                && CharacterDefinitions[characterId].Enabled;
        }
    }
}
