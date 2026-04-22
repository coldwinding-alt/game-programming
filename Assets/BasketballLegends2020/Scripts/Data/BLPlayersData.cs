using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public static class BLPlayersData
    {
        private const int DragonBonesSkinCount = 60;

        private sealed class BLCharacterDefinition
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
            public float PreviewScaleMultiplier = 1f;
            public float PreviewOffsetY;
            public float PortraitScaleMultiplier = 1f;
            public float PortraitOffsetY;
        }

        private static DBLiteTextureAtlas portraitAtlas;

        private static readonly BLCharacterDefinition[] CharacterDefinitions =
        {
            new BLCharacterDefinition { DisplayName = "PUMPKIN", SkinIndex = 0, FormIndex = 0, SuperId = 3, Enabled = true, PortraitSpriteName = "LeBron James0", HeadOffsetY = 0f, HeadScale = 1f, PreviewScaleMultiplier = 0.98f, PortraitScaleMultiplier = 1.02f },
            new BLCharacterDefinition { DisplayName = "FRANKENSTEIN", SkinIndex = 5, FormIndex = 2, SuperId = 0, Enabled = true, PortraitSpriteName = "Draymond Green0", HeadOffsetX = 4.5f, HeadOffsetY = -5.5f, HeadScale = 0.94f, PreviewScaleMultiplier = 0.96f, PortraitScaleMultiplier = 0.92f, PortraitOffsetY = 4f },
            new BLCharacterDefinition { DisplayName = "MUMMY", SkinIndex = 8, FormIndex = 4, SuperId = 1, Enabled = true, PortraitSpriteName = "Brook Lopez", HeadOffsetY = -7f, HeadScale = 0.97f, PreviewScaleMultiplier = 0.94f, PreviewOffsetY = -3f, PortraitScaleMultiplier = 1.02f, PortraitOffsetY = 4f },
            new BLCharacterDefinition { DisplayName = "VAMPIRE", SkinIndex = 11, FormIndex = 6, SuperId = 2, Enabled = true, PortraitSpriteName = "Marcus Smart0", HeadOffsetY = -6f, HeadScale = 0.98f, PreviewScaleMultiplier = 0.96f, PortraitScaleMultiplier = 1f, PortraitOffsetY = 4f },
            new BLCharacterDefinition { DisplayName = "CANDLEMAN", SkinIndex = 13, FormIndex = 8, SuperId = 3, Enabled = true, PortraitSpriteName = "custom_head_candle", HeadOffsetX = 2.75f, HeadOffsetY = -1.5f, HeadScale = 0.92f, PreviewScaleMultiplier = 0.94f, PortraitScaleMultiplier = 0.85f },
            new BLCharacterDefinition { DisplayName = "SCARECROW", SkinIndex = 14, FormIndex = 9, SuperId = 0, Enabled = true, PortraitSpriteName = "custom_head_scarecrow", HeadOffsetY = 0f, HeadScale = 1.03f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.05f, PortraitOffsetY = -6f },
            new BLCharacterDefinition { DisplayName = "WITCH", SkinIndex = 15, FormIndex = 10, SuperId = 2, Enabled = true, PortraitSpriteName = "custom_head_witch", HeadOffsetX = 3.5f, HeadOffsetY = -0.75f, HeadScale = 1.08f, PreviewScaleMultiplier = 0.98f, PreviewOffsetY = 2f, PortraitScaleMultiplier = 1.12f, PortraitOffsetY = -8f },
            new BLCharacterDefinition { DisplayName = "BLACK CAT", SkinIndex = 16, FormIndex = 11, SuperId = 1, Enabled = true, PortraitSpriteName = "custom_head_blackcat", HeadOffsetY = -0.5f, HeadScale = 0.96f, PreviewScaleMultiplier = 0.97f, PreviewOffsetY = 1f, PortraitScaleMultiplier = 0.96f, PortraitOffsetY = -4f }
        };

        private static readonly int[] Hands =
        {
            2, 2, 3, 2, 3, 1, 2, 2, 3, 2, 2, 4, 1, 2, 2, 1, 2, 2, 1, 1,
            3, 1, 1, 1, 3, 3, 2, 1, 2, 1, 1, 1, 2, 1, 1, 3, 3, 2, 2, 1,
            3, 2, 1, 3, 1, 2, 2, 1, 3, 2, 2, 1, 3, 1, 2, 2, 2, 1, 1, 1
        };

        private static readonly string[] Legs = new string[DragonBonesSkinCount];

        public static int CharacterCount => CharacterDefinitions.Length;

        public static void SetupPlayers()
        {
            if (Legs[0] != null)
            {
                return;
            }

            for (var i = 0; i < DragonBonesSkinCount; i++)
            {
                var stable = 1 + (i * 7 + 3) % 15;
                Legs[i] = "leg" + stable;
            }

            Hands[11] = 4;
            Hands[13] = 5;
            Hands[14] = 6;
            Hands[15] = 7;
            Hands[16] = 8;

            Legs[11] = "leg16";
            Legs[13] = "leg17";
            Legs[14] = "leg18";
            Legs[15] = "leg19";
            Legs[16] = "leg20";
        }

        public static DBLiteArmature BuildGameplayArmature(string name)
        {
            DBLiteFactory.Instance.EnsureLoaded();
            return DBLiteFactory.Instance.BuildArmature("playerSmall", name);
        }

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

        public static string GetCharacterName(int characterId)
        {
            return GetCharacterDefinition(characterId).DisplayName;
        }

        public static int GetCharacterFormIndex(int characterId)
        {
            return GetCharacterDefinition(characterId).FormIndex;
        }

        public static int GetCharacterSuperId(int characterId)
        {
            return GetCharacterDefinition(characterId).SuperId;
        }

        public static float GetCharacterPreviewScaleMultiplier(int characterId)
        {
            return GetCharacterDefinition(characterId).PreviewScaleMultiplier;
        }

        public static float GetCharacterPreviewOffsetY(int characterId)
        {
            return GetCharacterDefinition(characterId).PreviewOffsetY;
        }

        public static Sprite GetCharacterPortraitSprite(int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            var atlas = GetPortraitAtlas();
            return atlas?.Sprite(definition.PortraitSpriteName);
        }

        public static float GetCharacterPortraitScaleMultiplier(int characterId)
        {
            return GetCharacterDefinition(characterId).PortraitScaleMultiplier;
        }

        public static float GetCharacterPortraitOffsetY(int characterId)
        {
            return GetCharacterDefinition(characterId).PortraitOffsetY;
        }

        public static void ApplyCharacter(DBLiteArmature armature, int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            SwitchPlayer(armature, definition.SkinIndex, definition.FormIndex);
            ApplyCharacterTuning(armature, definition);
        }

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

        public static void SwitchPlayer(DBLiteArmature armature, int skinId, int formId)
        {
            if (armature == null)
            {
                return;
            }

            if (Legs[0] == null)
            {
                SetupPlayers();
            }

            skinId = Mathf.Clamp(skinId, 0, DragonBonesSkinCount - 1);
            formId = Mathf.Max(0, formId);

            var hand = Hands[Mathf.Clamp(skinId, 0, Hands.Length - 1)];
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

        private static BLCharacterDefinition GetCharacterDefinition(int characterId)
        {
            return CharacterDefinitions[SanitizeCharacterId(characterId)];
        }

        private static DBLiteTextureAtlas GetPortraitAtlas()
        {
            if (portraitAtlas != null)
            {
                return portraitAtlas;
            }

            var textureJsonAsset = Resources.Load<TextAsset>("BL2020/DragonBones/texture2");
            var texture = Resources.Load<Texture2D>("BL2020/DragonBones/texture2");
            if (textureJsonAsset == null || texture == null)
            {
                Debug.LogWarning("Missing DragonBones portrait atlas resources.");
                return null;
            }

            portraitAtlas = DBLiteTextureAtlas.Parse("texture2_portraits", texture, textureJsonAsset.text);
            return portraitAtlas;
        }

        private static void ApplyCharacterTuning(DBLiteArmature armature, BLCharacterDefinition definition)
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
                head.transform.localPosition = BLConstants.SnapLocalPositionToScreenPixels(head.transform.parent, headPosition);
                head.transform.localScale = new Vector3(definition.HeadScale, definition.HeadScale, 1f);
            }

            var body = armature.GetChildArmature("body");
            if (body != null)
            {
                var bodyPosition = body.transform.localPosition;
                bodyPosition.y = 0f;
                bodyPosition.z = 0f;
                body.transform.localPosition = BLConstants.SnapLocalPositionToScreenPixels(body.transform.parent, bodyPosition);
                body.transform.localScale = Vector3.one;
            }
        }

        private static bool IsCharacterEnabled(int characterId)
        {
            return characterId >= 0
                && characterId < CharacterDefinitions.Length
                && CharacterDefinitions[characterId].Enabled;
        }
    }
}
