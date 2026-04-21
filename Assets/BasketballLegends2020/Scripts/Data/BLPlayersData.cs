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
        }

        private static readonly BLCharacterDefinition[] CharacterDefinitions =
        {
            new BLCharacterDefinition { DisplayName = "PUMPKIN", SkinIndex = 0, FormIndex = 0, SuperId = 3, Enabled = true },
            new BLCharacterDefinition { DisplayName = "FRANKENSTEIN", SkinIndex = 5, FormIndex = 2, SuperId = 0, Enabled = true },
            new BLCharacterDefinition { DisplayName = "MUMMY", SkinIndex = 8, FormIndex = 4, SuperId = 1, Enabled = true },
            new BLCharacterDefinition { DisplayName = "VAMPIRE", SkinIndex = 11, FormIndex = 6, SuperId = 2, Enabled = true },
            new BLCharacterDefinition { DisplayName = "CANDLEMAN", SkinIndex = 13, FormIndex = 8, SuperId = 3, Enabled = true },
            new BLCharacterDefinition { DisplayName = "SCARECROW", SkinIndex = 14, FormIndex = 9, SuperId = 0, Enabled = true },
            new BLCharacterDefinition { DisplayName = "WITCH", SkinIndex = 15, FormIndex = 10, SuperId = 2, Enabled = true },
            new BLCharacterDefinition { DisplayName = "BLACK CAT", SkinIndex = 16, FormIndex = 11, SuperId = 1, Enabled = true }
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

        public static void ApplyCharacter(DBLiteArmature armature, int characterId)
        {
            var definition = GetCharacterDefinition(characterId);
            SwitchPlayer(armature, definition.SkinIndex, definition.FormIndex);
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

        private static bool IsCharacterEnabled(int characterId)
        {
            return characterId >= 0
                && characterId < CharacterDefinitions.Length
                && CharacterDefinitions[characterId].Enabled;
        }
    }
}
