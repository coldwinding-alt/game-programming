using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushSmokeTest
    {
        public static void Run()
        {
            var errors = new List<string>();
            GameObject root = null;
            rimrushGameCore core = null;

            try
            {
                CheckResource<Texture2D>("rimrush/Atlases/gameplay", errors);
                CheckResource<TextAsset>("rimrush/Atlases/gameplay", errors);
                CheckResource<Texture2D>("rimrush/Atlases/interface", errors);
                CheckResource<TextAsset>("rimrush/Atlases/interface", errors);
                CheckResource<Texture2D>("rimrush/Atlases/skillfx", errors);
                CheckResource<TextAsset>("rimrush/Atlases/skillfx", errors);
                CheckResource<Font>("rimrush/Fonts/Impact", errors);
                CheckResource<Font>("rimrush/Fonts/Impact2", errors);
                CheckResource<Font>("rimrush/Fonts/CfCrackBold", errors);
                CheckResource<Font>("rimrush/Fonts/Rajdhani-SemiBold", errors);
                CheckResource<Font>("rimrush/Fonts/Rajdhani-Bold", errors);
                CheckResource<Font>("rimrush/Fonts/Griffy-Regular", errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.PauseButton), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton), errors);
                CheckResource<Texture2D>(rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Scoreboard), errors);
                CheckResource<Texture2D>(rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Popup), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallGhoulGreen), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallPumpkinEmber), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallMoonlitViolet), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallJackOLantern), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallEvilEye), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallCursed8Ball), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallCandySwirl), errors);
                CheckResource<TextAsset>("rimrush/DragonBones/sk2", errors);
                CheckResource<TextAsset>("rimrush/DragonBones/texture2", errors);
                CheckResource<Texture2D>("rimrush/DragonBones/texture2", errors);
                CheckAudioResources(errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallGhoulGreen), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_ghoul_green.png", errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallPumpkinEmber), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_pumpkin_ember.png", errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallMoonlitViolet), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_moonlit_violet.png", errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallJackOLantern), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_jack_o_lantern.png", errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallEvilEye), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_evil_eye.png", errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallCursed8Ball), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_cursed_8ball.png", errors);
                ValidateBallSpriteAsset(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.GameplayImages.BallCandySwirl), "Assets/rimrush/Resources/rimrush/Images/Gameplay/ball_halloween_candy_swirl.png", errors);

                if (Shader.Find("rimrush/TextMeshOutlined") == null)
                {
                    errors.Add("Could not find rimrush/TextMeshOutlined shader.");
                }

                var gameplay = rimrushAtlasCache.Instance.Gameplay;
                if (!gameplay.HasFrame("0bg_gameplay0000") || !gameplay.HasFrame("BallMC0000"))
                {
                    errors.Add("Gameplay atlas did not expose the expected frame keys.");
                }

                var ui = rimrushAtlasCache.Instance.Interface;
                if (!ui.HasFrame("icon_ball0000") || !ui.HasFrame("icon_ball20000"))
                {
                    errors.Add("Interface atlas did not expose expected skill UI frame keys.");
                }

                var skillFx = rimrushAtlasCache.Instance.SkillFx;
                if (!skillFx.HasFrame("ShieldMC20000") || !skillFx.HasFrame("teleport30000"))
                {
                    errors.Add("Skill FX atlas did not expose expected shield and teleport frame keys.");
                }

                DBLiteFactory.Instance.EnsureLoaded();
                var armature = rimrushPlayersData.BuildGameplayArmature("SmokePlayerSmall");
                if (armature == null)
                {
                    errors.Add("Could not build DragonBones playerSmall armature.");
                }
                else
                {
                    foreach (var characterId in rimrushPlayersData.GetActiveCharacterIds())
                    {
                        rimrushPlayersData.ApplyCharacter(armature, characterId);
                    }

                    UnityEngine.Object.DestroyImmediate(armature.gameObject);
                }

                var dragonBones = Resources.Load<TextAsset>("rimrush/DragonBones/sk2");
                if (dragonBones == null || !dragonBones.text.Contains("\"mega\""))
                {
                    errors.Add("DragonBones data did not expose the expected mega frame event.");
                }

                ValidateDifficultyCycleAndSkillMapping(errors);
                ValidateTournamentSeasonMode(errors, rimrushAiDifficulty.Normal, "normal");
                ValidateTournamentSeasonMode(errors, rimrushAiDifficulty.Hard, "hard");
                ValidateTournamentSeasonMode(errors, rimrushAiDifficulty.Hell, "hell");
                ValidateHardTournamentSkillMapping(errors);
                ValidateHellTournamentSkillMapping(errors);
                ValidateBallSelectionStateAndResolution(errors);
                ValidateRuntimeGraphicsResourceReuse(errors);

                root = new GameObject("SmokeRuntimeRoot");
                rimrushAudio.Create(root.transform);
                rimrushInventory.Instance.SetQuickSelection(0);
                rimrushInventory.Instance.SetQuickBallSelection(rimrushBallSelection.ClassicOriginal);
                rimrushInventory.Instance.StartQuickGame();
                core = new rimrushGameBuilder().Build(root.transform);
                core.Update(0.016f);
                ValidateBlockedShotScorePersistence(core, errors);
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            finally
            {
                core?.Shutdown();
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

            Debug.Log("rimrush smoke test passed.");
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
                "rimrush/Sound/2_M_Whistle",
                "rimrush/Sound/4_P_Teleport",
                "rimrush/Sound/5_P_Swoosh",
                "rimrush/Sound/6_P_Energy",
                "rimrush/Sound/7_P_Stunned",
                "rimrush/Sound/8_B_Steel",
                "rimrush/Sound/9_M_Buzzer",
                "rimrush/Sound/10_B_Ring",
                "rimrush/Sound/11_P_MegaStart",
                "rimrush/Sound/13_P_Shield",
                "rimrush/Sound/16_B_Bounce",
                "rimrush/Sound/17_P_Dash",
                "rimrush/Sound/18_P_SuperDash",
                "rimrush/Sound/19_M_Countdown",
                "rimrush/Sound/20_ButtonSnd",
                "rimrush/Sound/21_B_NET",
                "rimrush/Sound/22_B_Brick",
                "rimrush/Sound/23_B_Basket",
                "rimrush/Sound/24_TrackSnd",
            };

            for (var i = 0; i < audioPaths.Length; i++)
            {
                CheckResource<AudioClip>(audioPaths[i], errors);
            }
        }

        private static void ValidateRuntimeGraphicsResourceReuse(List<string> errors)
        {
            ValidateSharedRuntimeMaterials(errors);
            ValidateGameRuntimeMeshRelease(errors);
        }

        private static void ValidateSharedRuntimeMaterials(List<string> errors)
        {
            var runtimeRoot = new GameObject("GraphicsReuseSmokeRoot");
            try
            {
                new rimrushBasketObject(-1, runtimeRoot.transform);
                new rimrushBasketObject(1, runtimeRoot.transform);

                var lineRenderers = runtimeRoot.GetComponentsInChildren<LineRenderer>(true);
                if (lineRenderers.Length < 2)
                {
                    errors.Add("Runtime graphics reuse test could not find basket net line renderers.");
                }
                else
                {
                    var sharedLineMaterial = lineRenderers[0].sharedMaterial;
                    if (sharedLineMaterial == null)
                    {
                        errors.Add("Basket net line renderer did not receive a shared runtime material.");
                    }
                    else
                    {
                        for (var i = 1; i < lineRenderers.Length; i++)
                        {
                            if (lineRenderers[i].sharedMaterial != sharedLineMaterial)
                            {
                                errors.Add("Basket net line renderers are still allocating distinct runtime materials.");
                                break;
                            }
                        }
                    }
                }

                new rimrushRadialIconMesh("EnergyFillSmokeA", rimrushAtlasCache.Instance.Interface, "icon_ball20000", 40f, 40f, 10, runtimeRoot.transform);
                new rimrushRadialIconMesh("EnergyFillSmokeB", rimrushAtlasCache.Instance.Interface, "icon_ball20000", 72f, 40f, 10, runtimeRoot.transform);

                var meshRenderers = runtimeRoot.GetComponentsInChildren<MeshRenderer>(true);
                if (meshRenderers.Length < 2)
                {
                    errors.Add("Runtime graphics reuse test could not find radial icon mesh renderers.");
                }
                else if (meshRenderers[0].sharedMaterial == null || meshRenderers[1].sharedMaterial == null)
                {
                    errors.Add("Radial icon mesh renderer did not receive a shared runtime material.");
                }
                else if (meshRenderers[0].sharedMaterial != meshRenderers[1].sharedMaterial)
                {
                    errors.Add("Radial icon meshes are still allocating duplicate runtime materials for the same atlas texture.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
            }
        }

        private static void ValidateGameRuntimeMeshRelease(List<string> errors)
        {
            var runtimeRoot = new GameObject("GraphicsReleaseSmokeRoot");
            rimrushGameCore core = null;
            Mesh ownedMesh = null;
            MeshFilter meshFilter = null;

            try
            {
                rimrushAudio.Create(runtimeRoot.transform);
                rimrushInventory.Instance.SetQuickSelection(0);
                rimrushInventory.Instance.SetQuickBallSelection(rimrushBallSelection.ClassicOriginal);
                rimrushInventory.Instance.StartQuickGame();
                core = new rimrushGameBuilder().Build(runtimeRoot.transform);

                meshFilter = runtimeRoot.GetComponentInChildren<MeshFilter>(true);
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    errors.Add("Runtime graphics release test could not access the energy radial dynamic mesh.");
                    return;
                }

                ownedMesh = meshFilter.sharedMesh;
                core.Shutdown();

                if (meshFilter.sharedMesh != null)
                {
                    errors.Add("Game runtime shutdown did not detach the radial icon mesh from its MeshFilter.");
                }
            }
            finally
            {
                core?.Shutdown();
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
            }

            if (ownedMesh != null)
            {
                errors.Add("Game runtime shutdown did not release the radial icon dynamic mesh.");
            }
        }

        private static void ValidateBlockedShotScorePersistence(rimrushGameCore core, List<string> errors)
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

            var canScoreField = typeof(rimrushBallObject).GetField("canScore", BindingFlags.Instance | BindingFlags.NonPublic);
            var scoreArmedSideField = typeof(rimrushBallObject).GetField("scoreArmedSide", BindingFlags.Instance | BindingFlags.NonPublic);
            if (canScoreField == null || scoreArmedSideField == null)
            {
                errors.Add("Blocked-shot score regression test could not inspect rimrushBallObject scoring state.");
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
            var inventory = rimrushInventory.Instance;
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
                if (rimrushAssets.Images.BallTheme(rimrushBallTheme.ClassicOriginal) != null)
                {
                    errors.Add("Classic ball theme should stay atlas-backed and not resolve to an external image path.");
                }

                if (rimrushGameplaySpriteLoader.LoadBallThemeSprite(rimrushBallTheme.ClassicOriginal, 0.5f, 0.5f) != null)
                {
                    errors.Add("Classic ball theme unexpectedly resolved through the external ball sprite loader.");
                }

                inventory.SetQuickSelection(0);
                inventory.SetQuickBallSelection(rimrushBallSelection.GhoulGreen);
                inventory.Difficulty = rimrushAiDifficulty.Normal;
                inventory.StartQuickGame();
                if (inventory.MatchData.BallTheme != rimrushBallTheme.GhoulGreen)
                {
                    errors.Add("Quick Match did not use the quick-mode ball selection.");
                }

                inventory.SetTrainingSelection(1);
                inventory.SetTrainingBallSelection(rimrushBallSelection.EvilEye);
                inventory.StartTraining();
                if (inventory.MatchData.BallTheme != rimrushBallTheme.EvilEye)
                {
                    errors.Add("Training did not use the training-mode ball selection.");
                }

                inventory.SetVersusBallSelection(rimrushBallSelection.Cursed8Ball);
                inventory.StartTwoPlayerVersus(0, 1);
                if (inventory.MatchData.BallTheme != rimrushBallTheme.Cursed8Ball)
                {
                    errors.Add("2 Players did not use the versus-mode ball selection.");
                }

                inventory.SetTournamentSelection(0);
                inventory.SetTournamentBallSelection(rimrushBallSelection.CandySwirl);
                inventory.Difficulty = rimrushAiDifficulty.Normal;
                if (!inventory.BeginTournament())
                {
                    errors.Add("Tournament ball selection test could not create a tournament.");
                }
                else if (inventory.MatchData.BallTheme != rimrushBallTheme.CandySwirl)
                {
                    errors.Add("Tournament did not use the tournament-mode ball selection.");
                }

                var randomMatchData = new rimrushMatchData(true);
                var seenThemes = new HashSet<rimrushBallTheme>();
                UnityEngine.Random.InitState(24680);
                for (var i = 0; i < 16; i++)
                {
                    randomMatchData.StartQuickMatch(0, rimrushAiDifficulty.Normal, rimrushBallSelection.Random);
                    seenThemes.Add(randomMatchData.BallTheme);
                }

                if (seenThemes.Count < 2)
                {
                    errors.Add("Random ball selection did not reroll across repeated match starts.");
                }

                randomMatchData.StartQuickMatch(0, rimrushAiDifficulty.Normal, rimrushBallSelection.ClassicOriginal);
                if (randomMatchData.BallTheme != rimrushBallTheme.ClassicOriginal)
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
                inventory.Tournament = originalTournament ?? new rimrushTournamentData();
            }
        }

        private static void ValidateDifficultyCycleAndSkillMapping(List<string> errors)
        {
            var inventory = rimrushInventory.Instance;
            var originalDifficulty = inventory.Difficulty;

            try
            {
                inventory.Difficulty = rimrushAiDifficulty.Easy;
                if (inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty label did not render AI: EASY.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != rimrushAiDifficulty.Normal || inventory.DifficultyLabel != "AI: NORMAL")
                {
                    errors.Add("Difficulty toggle did not advance from Easy to Normal.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != rimrushAiDifficulty.Hard || inventory.DifficultyLabel != "AI: HARD")
                {
                    errors.Add("Difficulty toggle did not advance from Normal to Hard.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != rimrushAiDifficulty.Hell || inventory.DifficultyLabel != "AI: HELL")
                {
                    errors.Add("Difficulty toggle did not advance from Hard to Hell.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != rimrushAiDifficulty.Easy || inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty toggle did not cycle from Hell back to Easy.");
                }

                var matchData = new rimrushMatchData(true);
                matchData.StartQuickMatch(0, rimrushAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Quick Match hard mapping");

                matchData.StartRandomMatch(0, rimrushAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Random Match hard mapping");

                matchData.StartQuickMatch(0, rimrushAiDifficulty.Hell);
                AssertOpponentSkill(matchData, 10, errors, "Quick Match hell mapping");

                matchData.StartRandomMatch(0, rimrushAiDifficulty.Hell);
                AssertOpponentSkill(matchData, 10, errors, "Random Match hell mapping");
            }
            finally
            {
                inventory.Difficulty = originalDifficulty;
            }
        }

        private static void ValidateTournamentSeasonMode(List<string> errors, rimrushAiDifficulty difficulty, string difficultyLabel)
        {
            var tournament = new rimrushTournamentData();
            if (!tournament.Create(0, difficulty))
            {
                errors.Add($"Tournament season mode could not be created for {difficultyLabel} difficulty with 8 enabled characters.");
                return;
            }

            if (tournament.CurrentStage != rimrushTournamentStage.RegularSeason || !tournament.HasPendingPlayerMatch)
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

            if (!tournament.RegularSeasonCompleted || !tournament.PlayerQualifiedForPlayoffs || tournament.CurrentStage != rimrushTournamentStage.RegularSeason)
            {
                errors.Add($"Tournament regular season did not finish in standings-preview state after three player wins for {difficultyLabel} difficulty.");
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != rimrushTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament did not transition into a pending semifinal after finals start for {difficultyLabel} difficulty.");
            }

            tournament.ApplyCurrentMatchResult(32, 18);
            if (tournament.CurrentStage != rimrushTournamentStage.Final || !tournament.ThirdPlaceResult.Completed || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament semifinal resolution did not open the final and auto-resolve third place for {difficultyLabel} difficulty.");
            }

            tournament.ApplyCurrentMatchResult(29, 17);
            if (!tournament.Completed || tournament.CurrentStage != rimrushTournamentStage.Complete)
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
            var tournament = new rimrushTournamentData();
            if (!tournament.Create(0, rimrushAiDifficulty.Hard))
            {
                errors.Add("Hard tournament mapping test could not create a tournament.");
                return;
            }

            var matchData = new rimrushMatchData(true);
            var expectedRoundSkills = new[] { 5, 6, 7 };
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(matchData, expectedRoundSkills[round], errors, $"Hard tournament round {round + 1}");
                tournament.ApplyCurrentMatchResult(30 + round, 10 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != rimrushTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add("Hard tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 7, errors, "Hard tournament semifinal");

            tournament.ApplyCurrentMatchResult(32, 18);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 8, errors, "Hard tournament final");

            var thirdPlaceTournament = new rimrushTournamentData();
            if (!thirdPlaceTournament.Create(0, rimrushAiDifficulty.Hard))
            {
                errors.Add("Hard third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new rimrushMatchData(true);
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
            if (thirdPlaceTournament.CurrentStage != rimrushTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add("Hard tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 7, errors, "Hard tournament third-place match");
        }

        private static void ValidateHellTournamentSkillMapping(List<string> errors)
        {
            var tournament = new rimrushTournamentData();
            if (!tournament.Create(0, rimrushAiDifficulty.Hell))
            {
                errors.Add("Hell tournament mapping test could not create a tournament.");
                return;
            }

            var matchData = new rimrushMatchData(true);
            var expectedRoundSkills = new[] { 8, 9, 10 };
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(matchData, expectedRoundSkills[round], errors, $"Hell tournament round {round + 1}");
                tournament.ApplyCurrentMatchResult(34 + round, 18 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != rimrushTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add("Hell tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 10, errors, "Hell tournament semifinal");

            tournament.ApplyCurrentMatchResult(36, 22);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 11, errors, "Hell tournament final");

            var thirdPlaceTournament = new rimrushTournamentData();
            if (!thirdPlaceTournament.Create(0, rimrushAiDifficulty.Hell))
            {
                errors.Add("Hell third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new rimrushMatchData(true);
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
            if (thirdPlaceTournament.CurrentStage != rimrushTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add("Hell tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 10, errors, "Hell tournament third-place match");
        }

        private static void AssertOpponentSkill(rimrushMatchData matchData, int expectedSkill, List<string> errors, string context)
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

            if (actualSkill < 0 || actualSkill > rimrushAISkillsData.MaxSkillIndex)
            {
                errors.Add($"{context} produced out-of-range opponent skill {actualSkill}.");
            }
        }
    }
}
