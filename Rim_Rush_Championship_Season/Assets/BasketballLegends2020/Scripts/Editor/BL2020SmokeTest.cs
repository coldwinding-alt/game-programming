using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.PauseButton), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.MusicButtonOn), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.MusicButtonOff), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.HelpButton), errors);
                CheckResource<Texture2D>(BLAssets.Hud.ResourcePath(BLAssets.Hud.Scoreboard), errors);
                CheckResource<Texture2D>(BLAssets.Hud.ResourcePath(BLAssets.Hud.Popup), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallGhoulGreen), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallPumpkinEmber), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallMoonlitViolet), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallJackOLantern), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallEvilEye), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallCursed8Ball), errors);
                CheckResource<Texture2D>(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallCandySwirl), errors);
                CheckResource<TextAsset>("BL2020/DragonBones/sk2", errors);
                CheckResource<TextAsset>("BL2020/DragonBones/texture2", errors);
                CheckResource<Texture2D>("BL2020/DragonBones/texture2", errors);
                CheckAudioResources(errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallGhoulGreen), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_ghoul_green.png", errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallPumpkinEmber), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_pumpkin_ember.png", errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallMoonlitViolet), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_moonlit_violet.png", errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallJackOLantern), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_jack_o_lantern.png", errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallEvilEye), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_evil_eye.png", errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallCursed8Ball), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_cursed_8ball.png", errors);
                ValidateBallSpriteAsset(BLAssets.Images.ResourcePath(BLAssets.Images.GameplayImages.BallCandySwirl), "Assets/BasketballLegends2020/Resources/BL2020/Images/Gameplay/ball_halloween_candy_swirl.png", errors);

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
                ValidateTournamentSeasonMode(errors, BLAiDifficulty.Hell, "hell");
                ValidateHardTournamentSkillMapping(errors);
                ValidateHellTournamentSkillMapping(errors);
                ValidateBallSelectionStateAndResolution(errors);

                root = new GameObject("SmokeRuntimeRoot");
                BLAudio.Create(root.transform);
                BLInventory.Instance.SetQuickSelection(0);
                BLInventory.Instance.SetQuickBallSelection(BLBallSelection.ClassicOriginal);
                BLInventory.Instance.StartQuickGame();
                var core = new BLGameBuilder().Build(root.transform);
                core.Update(0.016f);
                ValidateBlockedShotScorePersistence(core, errors);
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

        private static void ValidateBlockedShotScorePersistence(BLGameCore core, List<string> errors)
        {
            if (core == null || core.Ball == null || core.PlayersRight == null || core.PlayersRight.Count == 0)
            {
                errors.Add("Blocked-shot score regression test could not access the runtime ball/blocker state.");
                return;
            }

            core.MatchProcessor.Shoot(-1, true, 0);
            core.Ball.Shoot(-1, 240f, 260f, 0f, 1f);
            core.MatchProcessor.ProcessSensor(0);

            core.Ball.ApplyBlock(core.PlayersRight[0]);

            var canScoreField = typeof(BLBallObject).GetField("canScore", BindingFlags.Instance | BindingFlags.NonPublic);
            var scoreArmedSideField = typeof(BLBallObject).GetField("scoreArmedSide", BindingFlags.Instance | BindingFlags.NonPublic);
            if (canScoreField == null || scoreArmedSideField == null)
            {
                errors.Add("Blocked-shot score regression test could not inspect BLBallObject scoring state.");
                return;
            }

            var canScore = (bool)canScoreField.GetValue(core.Ball);
            var scoreArmedSide = (int)scoreArmedSideField.GetValue(core.Ball);
            if (!canScore)
            {
                errors.Add("Blocked shot unexpectedly lost its scoring eligibility before entering the basket.");
            }

            if (scoreArmedSide != -1)
            {
                errors.Add($"Blocked shot lost its original scoring side. Expected -1, got {scoreArmedSide}.");
            }

            if (!core.MatchProcessor.ProcessSensor(1))
            {
                errors.Add("Blocked shot did not preserve the upper-sensor progress needed to finish the made-basket chain.");
                return;
            }

            var points = core.MatchProcessor.ResolvePointsForScore(-1, 3);
            if (points != 2)
            {
                errors.Add($"Blocked shot that still scored should resolve as 2 points, got {points}.");
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
                ValidateBallBounds(inspector, resourcePath, errors);
                ValidateNoWhiteHalo(inspector, resourcePath, errors);
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

        private static void ValidateBallBounds(Texture2D texture, string resourcePath, List<string> errors)
        {
            var minX = texture.width;
            var minY = texture.height;
            var maxX = -1;
            var maxY = -1;

            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    if (texture.GetPixel(x, y).a <= 0.03f)
                    {
                        continue;
                    }

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (minX != 0 || minY != 0 || maxX != texture.width - 1 || maxY != texture.height - 1)
            {
                errors.Add($"{resourcePath} expected a full-footprint ball within the 36x36 canvas, got x={minX}..{maxX}, y={minY}..{maxY}.");
            }
        }

        private static void ValidateNoWhiteHalo(Texture2D texture, string resourcePath, List<string> errors)
        {
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a <= 0f || pixel.a >= 0.999f)
                    {
                        continue;
                    }

                    if (pixel.r > 0.92f && pixel.g > 0.92f && pixel.b > 0.92f)
                    {
                        errors.Add($"{resourcePath} has a near-white semi-transparent edge pixel at ({x},{y}); this can reintroduce a white halo in Unity.");
                        return;
                    }
                }
            }
        }

        private static void ValidateBallSelectionStateAndResolution(List<string> errors)
        {
            var inventory = BLInventory.Instance;
            var originalRandomState = UnityEngine.Random.state;
            var originalQuickBall = inventory.SelectedQuickBallSelection;
            var originalTournamentBall = inventory.SelectedTournamentBallSelection;
            var originalTrainingBall = inventory.SelectedTrainingBallSelection;
            var originalVersusBall = inventory.SelectedVersusBallSelection;
            var originalQuickCharacter = inventory.SelectedQuickCharacterId;
            var originalTournamentCharacter = inventory.SelectedTournamentCharacterId;
            var originalTrainingCharacter = inventory.SelectedTrainingCharacterId;
            var originalDifficulty = inventory.Difficulty;
            var originalParticipantMode = inventory.ParticipantMode;
            var originalSessionMode = inventory.SessionMode;
            var originalGameMode = inventory.GameMode;
            var originalMatchPrepared = inventory.MatchPrepared;
            var originalTournament = inventory.Tournament;

            try
            {
                if (BLAssets.Images.BallTheme(BLBallTheme.ClassicOriginal) != null)
                {
                    errors.Add("Classic ball theme should stay atlas-backed and not resolve to an external image path.");
                }

                if (BLGameplaySpriteLoader.LoadBallThemeSprite(BLBallTheme.ClassicOriginal, 0.5f, 0.5f) != null)
                {
                    errors.Add("Classic ball theme unexpectedly resolved through the external ball sprite loader.");
                }

                inventory.SetQuickSelection(0);
                inventory.SetQuickBallSelection(BLBallSelection.GhoulGreen);
                inventory.Difficulty = BLAiDifficulty.Normal;
                inventory.StartQuickGame();
                if (inventory.MatchData.BallTheme != BLBallTheme.GhoulGreen)
                {
                    errors.Add("Quick Match did not use the quick-mode ball selection.");
                }

                inventory.SetTrainingSelection(1);
                inventory.SetTrainingBallSelection(BLBallSelection.EvilEye);
                inventory.StartTraining();
                if (inventory.MatchData.BallTheme != BLBallTheme.EvilEye)
                {
                    errors.Add("Training did not use the training-mode ball selection.");
                }

                inventory.SetVersusBallSelection(BLBallSelection.Cursed8Ball);
                inventory.StartTwoPlayerVersus(0, 1);
                if (inventory.MatchData.BallTheme != BLBallTheme.Cursed8Ball)
                {
                    errors.Add("2 Players did not use the versus-mode ball selection.");
                }

                inventory.SetTournamentSelection(0);
                inventory.SetTournamentBallSelection(BLBallSelection.CandySwirl);
                inventory.Difficulty = BLAiDifficulty.Normal;
                if (!inventory.BeginTournament())
                {
                    errors.Add("Tournament ball selection test could not create a tournament.");
                }
                else if (inventory.MatchData.BallTheme != BLBallTheme.CandySwirl)
                {
                    errors.Add("Tournament did not use the tournament-mode ball selection.");
                }

                var randomMatchData = new BLMatchData(true);
                var seenThemes = new HashSet<BLBallTheme>();
                UnityEngine.Random.InitState(24680);
                for (var i = 0; i < 16; i++)
                {
                    randomMatchData.StartQuickMatch(0, BLAiDifficulty.Normal, BLBallSelection.Random);
                    seenThemes.Add(randomMatchData.BallTheme);
                }

                if (seenThemes.Count < 2)
                {
                    errors.Add("Random ball selection did not reroll across repeated match starts.");
                }

                randomMatchData.StartQuickMatch(0, BLAiDifficulty.Normal, BLBallSelection.ClassicOriginal);
                if (randomMatchData.BallTheme != BLBallTheme.ClassicOriginal)
                {
                    errors.Add("Classic ball selection did not resolve to the classic ball theme.");
                }
            }
            finally
            {
                UnityEngine.Random.state = originalRandomState;
                inventory.SelectedQuickBallSelection = originalQuickBall;
                inventory.SelectedTournamentBallSelection = originalTournamentBall;
                inventory.SelectedTrainingBallSelection = originalTrainingBall;
                inventory.SelectedVersusBallSelection = originalVersusBall;
                inventory.SelectedQuickCharacterId = originalQuickCharacter;
                inventory.SelectedTournamentCharacterId = originalTournamentCharacter;
                inventory.SelectedTrainingCharacterId = originalTrainingCharacter;
                inventory.Difficulty = originalDifficulty;
                inventory.ParticipantMode = originalParticipantMode;
                inventory.SessionMode = originalSessionMode;
                inventory.GameMode = originalGameMode;
                inventory.MatchPrepared = originalMatchPrepared;
                inventory.Tournament = originalTournament ?? new BLTournamentData();
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
                if (inventory.Difficulty != BLAiDifficulty.Hell || inventory.DifficultyLabel != "AI: HELL")
                {
                    errors.Add("Difficulty toggle did not advance from Hard to Hell.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != BLAiDifficulty.Easy || inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty toggle did not cycle from Hell back to Easy.");
                }

                var matchData = new BLMatchData(true);
                matchData.StartQuickMatch(0, BLAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Quick Match hard mapping");

                matchData.StartRandomMatch(0, BLAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Random Match hard mapping");

                matchData.StartQuickMatch(0, BLAiDifficulty.Hell);
                AssertOpponentSkill(matchData, 10, errors, "Quick Match hell mapping");

                matchData.StartRandomMatch(0, BLAiDifficulty.Hell);
                AssertOpponentSkill(matchData, 10, errors, "Random Match hell mapping");
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

        private static void ValidateHellTournamentSkillMapping(List<string> errors)
        {
            var tournament = new BLTournamentData();
            if (!tournament.Create(0, BLAiDifficulty.Hell))
            {
                errors.Add("Hell tournament mapping test could not create a tournament.");
                return;
            }

            var matchData = new BLMatchData(true);
            var expectedRoundSkills = new[] { 8, 9, 10 };
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(matchData, expectedRoundSkills[round], errors, $"Hell tournament round {round + 1}");
                tournament.ApplyCurrentMatchResult(34 + round, 18 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != BLTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add("Hell tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 10, errors, "Hell tournament semifinal");

            tournament.ApplyCurrentMatchResult(36, 22);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 11, errors, "Hell tournament final");

            var thirdPlaceTournament = new BLTournamentData();
            if (!thirdPlaceTournament.Create(0, BLAiDifficulty.Hell))
            {
                errors.Add("Hell third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new BLMatchData(true);
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
                AssertOpponentSkill(thirdPlaceMatchData, expectedRoundSkills[round], errors, $"Hell tournament third-place path round {round + 1}");
                thirdPlaceTournament.ApplyCurrentMatchResult(31 + round, 16 + round);
            }

            thirdPlaceTournament.BeginFinals();
            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 10, errors, "Hell tournament third-place path semifinal");

            thirdPlaceTournament.ApplyCurrentMatchResult(21, 27);
            if (thirdPlaceTournament.CurrentStage != BLTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add("Hell tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 10, errors, "Hell tournament third-place match");
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

            if (actualSkill < 0 || actualSkill > BLAISkillsData.MaxSkillIndex)
            {
                errors.Add($"{context} produced out-of-range opponent skill {actualSkill}.");
            }
        }
    }
}
