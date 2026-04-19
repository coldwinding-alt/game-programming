using UnityEngine;

namespace BasketballLegends2020
{
    public static class BLPlayersData
    {
        public const int TeamSize = 3;
        public const int PlayersCount = 60;
        public const int TeamsCount = 20;
        public static readonly string[] TeamNames =
        {
            "LAKERS",
            "WARRIORS",
            "BUCKS",
            "CELTICS",
            "ROCKETS",
            "76ERS",
            "THUNDER",
            "KNICKS",
            "MAVERICKS",
            "RAPTORS",
            "PELICANS",
            "HEAT",
            "MAGIC",
            "PACERS",
            "GRIZZLIES",
            "CLIPPERS",
            "NUGGETS",
            "JAZZ",
            "NETS",
            "BULLS"
        };

        private static readonly string[][] TeamPlayers =
        {
            new[] { "LEBRON JAMES", "ANTHONY DAVIS", "ALEX CARUSO" },
            new[] { "STEPH CURRY", "KLAY THOMPSON", "DRAYMOND GREEN" },
            new[] { "GIANNIS", "MIDDLETON", "BROOK LOPEZ" },
            new[] { "JAYSON TATUM", "KEMBA WALKER", "MARCUS SMART" },
            new[] { "JAMES HARDEN", "WESTBROOK", "P.J. TUCKER" },
            new[] { "JOEL EMBIID", "BEN SIMMONS", "TOBIAS HARRIS" },
            new[] { "CHRIS PAUL", "SHAI G-A", "STEVEN ADAMS" },
            new[] { "R.J. BARRETT", "JULIUS RANDLE", "BOBBY PORTIS" },
            new[] { "LUKA DONCIC", "PORZINGIS", "SETH CURRY" },
            new[] { "PASCAL SIAKAM", "FRED VANVLEET", "KYLE LOWRY" },
            new[] { "ZION", "JRUE HOLIDAY", "LONZO BALL" },
            new[] { "JIMMY BUTLER", "BAM ADEBAYO", "TYLER HERRO" },
            new[] { "VUCEVIC", "AARON GORDON", "EVAN FOURNIER" },
            new[] { "OLADIPO", "SABONIS", "BROGDON" },
            new[] { "JA MORANT", "VALANCIUNAS", "JAREN JACKSON" },
            new[] { "KAWHI", "PAUL GEORGE", "P. BEVERLEY" },
            new[] { "NIKOLA JOKIC", "JAMAL MURRAY", "WILL BARTON" },
            new[] { "D. MITCHELL", "JOE INGLES", "MIKE CONLEY" },
            new[] { "KEVIN DURANT", "KYRIE IRVING", "JARRETT ALLEN" },
            new[] { "MICHAEL JORDAN", "SCOTTIE PIPPEN", "DENNIS RODMAN" }
        };

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

        public static int SkinIndex(int teamId, int playerIndex)
        {
            return Mathf.Clamp(TeamSize * (teamId - 1) + playerIndex, 0, PlayersCount - 1);
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

        public static string GetTeamName(int teamId)
        {
            var index = Mathf.Clamp(teamId - 1, 0, TeamNames.Length - 1);
            return TeamNames[index];
        }

        public static string[] GetTeamPlayers(int teamId)
        {
            var index = Mathf.Clamp(teamId - 1, 0, TeamPlayers.Length - 1);
            return TeamPlayers[index];
        }

        public static string GetPlayerName(int teamId, int playerIndex)
        {
            var players = GetTeamPlayers(teamId);
            return players[Mathf.Clamp(playerIndex, 0, players.Length - 1)];
        }
    }
}
