using UnityEngine;

namespace BasketballLegends2020
{
    public static class BLPlayersData
    {
        public const int TeamSize = 3;
        public const int PlayersCount = 60;
        public const int TeamsCount = 20;

        private static readonly int[] Hands =
        {
            2, 2, 3, 2, 3, 1, 2, 2, 3, 2, 2, 1, 1, 2, 2, 1, 2, 2, 1, 1,
            3, 1, 1, 1, 3, 3, 2, 1, 2, 1, 1, 1, 2, 1, 1, 3, 3, 2, 2, 1,
            3, 2, 1, 3, 1, 2, 2, 1, 3, 2, 2, 1, 3, 1, 2, 2, 2, 1, 1, 1
        };

        private static readonly string[] Legs = new string[PlayersCount];

        public static void SetupPlayers()
        {
            for (var i = 0; i < PlayersCount; i++)
            {
                var stable = 1 + (i * 7 + 3) % 15;
                Legs[i] = "leg" + stable;
            }
        }

        public static DBLiteArmature BuildGameplayArmature(string name)
        {
            DBLiteFactory.Instance.EnsureLoaded();
            return DBLiteFactory.Instance.BuildArmature("playerSmall", name);
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

            skinId = Mathf.Clamp(skinId, 0, PlayersCount - 1);
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
    }
}
