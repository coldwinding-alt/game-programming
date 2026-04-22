using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BasketballLegends2020.EditorTools
{
    public static class BL2020SmokeTest
    {
        public static void Run()
        {
            var errors = new List<string>();
            GameObject root = null;

            try
            {
                CheckResource<Texture2D>("BL2020/Atlases/gameplay", errors);
                CheckResource<TextAsset>("BL2020/Atlases/gameplay", errors);
                CheckResource<Texture2D>("BL2020/Atlases/interface", errors);
                CheckResource<TextAsset>("BL2020/Atlases/interface", errors);
                CheckResource<Texture2D>("BL2020/Atlases/skillfx", errors);
                CheckResource<TextAsset>("BL2020/Atlases/skillfx", errors);
                CheckResource<Font>("BL2020/Fonts/Impact", errors);
                CheckResource<Font>("BL2020/Fonts/Impact2", errors);
                CheckResource<Font>("BL2020/Fonts/CfCrackBold", errors);
                CheckResource<Font>("BL2020/Fonts/Rajdhani-SemiBold", errors);
                CheckResource<Font>("BL2020/Fonts/Rajdhani-Bold", errors);
                CheckResource<Font>("BL2020/Fonts/Griffy-Regular", errors);
                CheckResource<Texture2D>("BL2020/Hud/scoreboard_halloween", errors);
                CheckResource<Texture2D>("BL2020/Hud/popup_halloween", errors);
                CheckResource<Texture2D>("BL2020/Images/Gameplay/ball_halloween_ghoul_green", errors);
                CheckResource<Texture2D>("BL2020/Images/Gameplay/ball_halloween_pumpkin_ember", errors);
                CheckResource<Texture2D>("BL2020/Images/Gameplay/ball_halloween_moonlit_violet", errors);
                CheckResource<TextAsset>("BL2020/DragonBones/sk2", errors);
                CheckResource<TextAsset>("BL2020/DragonBones/texture2", errors);
                CheckResource<Texture2D>("BL2020/DragonBones/texture2", errors);
                CheckAudioResources(errors);
                ValidateBallSpriteAsset("BL2020/Images/Gameplay/ball_halloween_ghoul_green", "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_ghoul_green.png", errors);
                ValidateBallSpriteAsset("BL2020/Images/Gameplay/ball_halloween_pumpkin_ember", "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_pumpkin_ember.png", errors);
                ValidateBallSpriteAsset("BL2020/Images/Gameplay/ball_halloween_moonlit_violet", "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_moonlit_violet.png", errors);

                if (Shader.Find("BasketballLegends2020/TextMeshOutlined") == null)
                {
                    errors.Add("Could not find BasketballLegends2020/TextMeshOutlined shader.");
                }

                var gameplay = BLAtlasCache.Instance.Gameplay;
                if (!gameplay.HasFrame("0bg_gameplay0000") || !gameplay.HasFrame("BallMC0000"))
                {
                    errors.Add("Gameplay atlas did not expose the expected frame keys.");
                }

                var ui = BLAtlasCache.Instance.Interface;
                if (!ui.HasFrame("icon_ball0000") || !ui.HasFrame("icon_ball20000"))
                {
                    errors.Add("Interface atlas did not expose expected skill UI frame keys.");
                }

                var skillFx = BLAtlasCache.Instance.SkillFx;
                if (!skillFx.HasFrame("ShieldMC20000") || !skillFx.HasFrame("teleport30000"))
                {
                    errors.Add("Skill FX atlas did not expose expected shield and teleport frame keys.");
                }

                DBLiteFactory.Instance.EnsureLoaded();
                var armature = BLPlayersData.BuildGameplayArmature("SmokePlayerSmall");
                if (armature == null)
                {
                    errors.Add("Could not build DragonBones playerSmall armature.");
                }
                else
                {
                    foreach (var characterId in BLPlayersData.GetActiveCharacterIds())
                    {
                        BLPlayersData.ApplyCharacter(armature, characterId);
                    }

                    UnityEngine.Object.DestroyImmediate(armature.gameObject);
                }

                var dragonBones = Resources.Load<TextAsset>("BL2020/DragonBones/sk2");
                if (dragonBones == null || !dragonBones.text.Contains("\"mega\""))
                {
                    errors.Add("DragonBones data did not expose the expected mega frame event.");
                }

                ValidateDifficultyCycleAndSkillMapping(errors);
                ValidateTournamentSeasonMode(errors, BLAiDifficulty.Normal, "normal");
                ValidateTournamentSeasonMode(errors, BLAiDifficulty.Hard, "hard");
                ValidateHardTournamentSkillMapping(errors);

                root = new GameObject("SmokeRuntimeRoot");
                BLAudio.Create(root.transform);
                BLInventory.Instance.SetQuickSelection(0);
                BLInventory.Instance.StartQuickGame();
                var core = new BLGameBuilder().Build(root.transform);
                core.Update(0.016f);
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Debug.LogError(error);
                }

                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("BL2020 smoke test passed.");
            EditorApplication.Exit(0);
        }

        private static void CheckResource<T>(string path, List<string> errors) where T : UnityEngine.Object
        {
            if (Resources.Load<T>(path) == null)
            {
                errors.Add($"Missing resource: {path}");
            }
        }

        private static void CheckAudioResources(List<string> errors)
        {
            var audioPaths = new[]
            {
                "BL2020/Sound/2_M_Whistle",
                "BL2020/Sound/4_P_Teleport",
                "BL2020/Sound/5_P_Swoosh",
                "BL2020/Sound/6_P_Energy",
                "BL2020/Sound/7_P_Stunned",
                "BL2020/Sound/8_B_Steel",
                "BL2020/Sound/9_M_Buzzer",
                "BL2020/Sound/10_B_Ring",
                "BL2020/Sound/11_P_MegaStart",
                "BL2020/Sound/13_P_Shield",
                "BL2020/Sound/16_B_Bounce",
                "BL2020/Sound/17_P_Dash",
                "BL2020/Sound/18_P_SuperDash",
                "BL2020/Sound/19_M_Countdown",
                "BL2020/Sound/20_ButtonSnd",
                "BL2020/Sound/21_B_NET",
                "BL2020/Sound/22_B_Brick",
                "BL2020/Sound/23_B_Basket",
                "BL2020/Sound/24_TrackSnd",
            };

            for (var i = 0; i < audioPaths.Length; i++)
            {
                CheckResource<AudioClip>(audioPaths[i], errors);
            }
        }

        private static void ValidateBallSpriteAsset(string resourcePath, string assetPath, List<string> errors)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                errors.Add($"Missing ball texture: {resourcePath}");
                return;
            }

            if (texture.width != 36 || texture.height != 36)
            {
                errors.Add($"{resourcePath} expected 36x36, got {texture.width}x{texture.height}.");
            }

            if (!File.Exists(assetPath))
            {
                errors.Add($"Missing ball asset file: {assetPath}");
                return;
            }

            var inspector = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!inspector.LoadImage(File.ReadAllBytes(assetPath), false))
                {
                    errors.Add($"Could not decode ball asset file: {assetPath}");
                    return;
                }

                ValidateCornerAlpha(inspector, resourcePath, 0, 0, errors);
                ValidateCornerAlpha(inspector, resourcePath, inspector.width - 1, 0, errors);
                ValidateCornerAlpha(inspector, resourcePath, 0, inspector.height - 1, errors);
                ValidateCornerAlpha(inspector, resourcePath, inspector.width - 1, inspector.height - 1, errors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inspector);
            }
        }

        private static void ValidateCornerAlpha(Texture2D texture, string resourcePath, int x, int y, List<string> errors)
        {
            if (texture.GetPixel(x, y).a > 0.03f)
            {
                errors.Add($"{resourcePath} corner ({x},{y}) should stay transparent for rotation.");
            }
        }

        private static void ValidateDifficultyCycleAndSkillMapping(List<string> errors)
        {
            var inventory = BLInventory.Instance;
            var originalDifficulty = inventory.Difficulty;

            try
            {
                inventory.Difficulty = BLAiDifficulty.Easy;
                if (inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty label did not render AI: EASY.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != BLAiDifficulty.Normal || inventory.DifficultyLabel != "AI: NORMAL")
                {
                    errors.Add("Difficulty toggle did not advance from Easy to Normal.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != BLAiDifficulty.Hard || inventory.DifficultyLabel != "AI: HARD")
                {
                    errors.Add("Difficulty toggle did not advance from Normal to Hard.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != BLAiDifficulty.Easy || inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty toggle did not cycle from Hard back to Easy.");
                }

                var matchData = new BLMatchData(true);
                matchData.StartQuickMatch(0, BLAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Quick Match hard mapping");

                matchData.StartRandomMatch(0, BLAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Random Match hard mapping");
            }
            finally
            {
                inventory.Difficulty = originalDifficulty;
            }
        }

        private static void ValidateTournamentSeasonMode(List<string> errors, BLAiDifficulty difficulty, string difficultyLabel)
        {
            var tournament = new BLTournamentData();
            if (!tournament.Create(0, difficulty))
            {
                errors.Add($"Tournament season mode could not be created for {difficultyLabel} difficulty with 8 enabled characters.");
                return;
            }

            if (tournament.CurrentStage != BLTournamentStage.RegularSeason || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament did not start in regular season with a pending player match for {difficultyLabel} difficulty.");
            }

            var seenCharacters = new HashSet<int>();
            for (var division = 0; division < tournament.DivisionEntrantCharacterIds.Length; division++)
            {
                for (var slot = 0; slot < tournament.DivisionEntrantCharacterIds[division].Length; slot++)
                {
                    var characterId = tournament.DivisionEntrantCharacterIds[division][slot];
                    if (!seenCharacters.Add(characterId))
                    {
                        errors.Add($"Tournament assigned duplicate characters across divisions for {difficultyLabel} difficulty.");
                    }
                }
            }

            if (tournament.DivisionEntrantCharacterIds[0][0] != tournament.PlayerCharacterId)
            {
                errors.Add($"Tournament player was not placed into division A slot 0 for {difficultyLabel} difficulty.");
            }

            for (var round = 0; round < 3; round++)
            {
                tournament.ApplyCurrentMatchResult(30 + round, 10 + round);
            }

            if (!tournament.RegularSeasonCompleted || !tournament.PlayerQualifiedForPlayoffs || tournament.CurrentStage != BLTournamentStage.RegularSeason)
            {
                errors.Add($"Tournament regular season did not finish in standings-preview state after three player wins for {difficultyLabel} difficulty.");
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != BLTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament did not transition into a pending semifinal after finals start for {difficultyLabel} difficulty.");
            }

            tournament.ApplyCurrentMatchResult(32, 18);
            if (tournament.CurrentStage != BLTournamentStage.Final || !tournament.ThirdPlaceResult.Completed || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament semifinal resolution did not open the final and auto-resolve third place for {difficultyLabel} difficulty.");
            }

            tournament.ApplyCurrentMatchResult(29, 17);
            if (!tournament.Completed || tournament.CurrentStage != BLTournamentStage.Complete)
            {
                errors.Add($"Tournament did not complete after the final for {difficultyLabel} difficulty.");
            }

            if (tournament.ChampionCharacterId != tournament.PlayerCharacterId || tournament.PlayerPlacement != 1)
            {
                errors.Add($"Tournament final placement logic did not award the player the championship for {difficultyLabel} difficulty.");
            }
        }

        private static void ValidateHardTournamentSkillMapping(List<string> errors)
        {
            var tournament = new BLTournamentData();
            if (!tournament.Create(0, BLAiDifficulty.Hard))
            {
                errors.Add("Hard tournament mapping test could not create a tournament.");
                return;
            }

            var matchData = new BLMatchData(true);
            var expectedRoundSkills = new[] { 5, 6, 7 };
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(matchData, expectedRoundSkills[round], errors, $"Hard tournament round {round + 1}");
                tournament.ApplyCurrentMatchResult(30 + round, 10 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != BLTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add("Hard tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 7, errors, "Hard tournament semifinal");

            tournament.ApplyCurrentMatchResult(32, 18);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 8, errors, "Hard tournament final");

            var thirdPlaceTournament = new BLTournamentData();
            if (!thirdPlaceTournament.Create(0, BLAiDifficulty.Hard))
            {
                errors.Add("Hard third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new BLMatchData(true);
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
                AssertOpponentSkill(thirdPlaceMatchData, expectedRoundSkills[round], errors, $"Hard tournament third-place path round {round + 1}");
                thirdPlaceTournament.ApplyCurrentMatchResult(28 + round, 12 + round);
            }

            thirdPlaceTournament.BeginFinals();
            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 7, errors, "Hard tournament third-place path semifinal");

            thirdPlaceTournament.ApplyCurrentMatchResult(18, 24);
            if (thirdPlaceTournament.CurrentStage != BLTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add("Hard tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 7, errors, "Hard tournament third-place match");
        }

        private static void AssertOpponentSkill(BLMatchData matchData, int expectedSkill, List<string> errors, string context)
        {
            if (matchData.Skills == null || matchData.Skills.Length < 2 || matchData.Skills[1] == null || matchData.Skills[1].Length == 0)
            {
                errors.Add($"{context} did not produce an opponent skill entry.");
                return;
            }

            var actualSkill = matchData.Skills[1][0];
            if (actualSkill != expectedSkill)
            {
                errors.Add($"{context} expected opponent skill {expectedSkill}, got {actualSkill}.");
            }

            if (actualSkill < 0 || actualSkill > 8)
            {
                errors.Add($"{context} produced out-of-range opponent skill {actualSkill}.");
            }
        }
    }
}
