// Automated smoke testing / Run a series of basic functional tests in the editor to check whether the core system of the game is working properly: whether the character data is complete, whether the skill configuration is correct, whether the atlas can be loaded, whether the audio can be played, whether the UI can be created, etc. Used to quickly identify obvious issues before release.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace mlp.EditorTools
{
    /// <summary>
    /// Automated smoke testing: Run basic functional tests in the editor to check whether core systems such as character data, skill configuration, atlas, audio, and UI are normal.

    /// </summary>
    public static class mlpSmokeTest
    {
        private readonly struct DifficultySkillExpectation
        {
            public readonly mlpAiDifficulty Difficulty;
            public readonly int ExpectedSkillIndex;

            public DifficultySkillExpectation(mlpAiDifficulty difficulty, int expectedSkillIndex)
            {
                Difficulty = difficulty;
                ExpectedSkillIndex = expectedSkillIndex;
            }
        }

        private static void ValidateGuaranteedBlockTeleportsToShot(List<string> errors)
        {
            const float expectedBlockHorizontalOffset = 20f;
            const float expectedBlockHandsOffsetY = 64f;
            // 1. Save the original state and restore it after the test is completed

            var inventory = mlpInventory.Instance;
            var originalParticipantMode = inventory.ParticipantMode;
            var originalSessionMode = inventory.SessionMode;
            var originalGameMode = inventory.GameMode;
            var originalMatchPrepared = inventory.MatchPrepared;
            var originalPendingTutorialNextAction = inventory.PendingTutorialNextAction;
            var runtimeRoot = new GameObject("GuaranteedBlockRuntimeRoot");
            mlpGameCore core = null;

            try
            {
                // 2. Set to tutorial mode and create a match

                mlpAudio.Create(runtimeRoot.transform);
                inventory.ParticipantMode = mlpParticipantMode.Tutorial;
                inventory.SessionMode = mlpSessionMode.Tutorial;
                inventory.GameMode = mlpGameModeIds.Tutorial;
                inventory.PendingTutorialNextAction = mlpTutorialNextAction.None;
                inventory.MatchData.StartTutorial(7, mlpBallSelection.ClassicOriginal);
                inventory.MatchPrepared = true;
                core = new mlpGameBuilder().Build(runtimeRoot.transform);
                // 3. Confirm that both roles have been created

                if (core.PlayersLeft == null || core.PlayersLeft.Count == 0 || core.PlayersRight == null || core.PlayersRight.Count == 0)
                {
                    errors.Add("Guaranteed-block teleport test could not create both players.");
                    return;
                }

                // 4. Confirm that the character on the left has the "Must-Block" (Black Cat) skill

                var blocker = core.PlayersLeft[0];
                var shooter = core.PlayersRight[0];
                if (blocker.SkillType != mlpCharacterSkillType.SureBlock)
                {
                    errors.Add($"Guaranteed-block teleport test expected BLACK CAT on the left, got {blocker.SkillType}.");
                    return;
                }

                // 5. Place the shot blocker and shooter in designated positions, and fill the shot blocker with killing energy
                blocker.TutorialSnapTo(new Vector2(730f, mlpObjectsData.PlayerIndentY), -1f);
                shooter.TutorialSnapTo(new Vector2(560f, mlpObjectsData.PlayerIndentY), -1f);
                blocker.TutorialChargeSuper();
                // 6. The opponent initiates a shot

                core.MatchProcessor.Shoot(shooter.Side, false, 0, shooter.PlayerNo);
                core.Ball.Shoot(shooter.Side, 500f, 260f, 0f, 1f);
                core.NotifyPlayersBallShot(shooter.Side, shooter.PlayerNo);

                // 7. Calculate the expected location (next to the ball) the shot blocker should be teleported to

                var ballPosition = core.Ball.Position;
                var originalBlockerPosition = blocker.Position;
                var expectedPosition = new Vector2(
                    Mathf.Clamp(ballPosition.x - core.Ball.Side * expectedBlockHorizontalOffset, 20f, mlpConstants.Width - 20f),
                    Mathf.Clamp(ballPosition.y + expectedBlockHandsOffsetY, mlpObjectsData.BasketHeight - 18f, mlpObjectsData.PlayerIndentY));

                // 8. Trigger the nirvana and verify whether it is successfully activated

                if (!blocker.SuperShot())
                {
                    errors.Add("Guaranteed-block skill did not activate against a live opponent shot.");
                    return;
                }

                // 9. Verify that the ball immediately becomes blocked

                if (core.Ball.State != "block")
                {
                    errors.Add($"Guaranteed-block skill did not immediately block the shot. Ball state: {core.Ball.State}.");
                }

                // 10. Verify that the shot blocker is teleported to the correct location next to the ball

                if (Vector2.Distance(blocker.Position, expectedPosition) > 0.1f)
                {
                    errors.Add($"Guaranteed-block skill did not teleport beside the ball. Expected {expectedPosition}, got {blocker.Position}.");
                }

                // 11. Verify that the shot-blocker has indeed moved far enough (proves that the teleportation is effective)

                if (Mathf.Abs(blocker.Position.x - originalBlockerPosition.x) < 80f)
                {
                    errors.Add("Guaranteed-block skill did not move the blocker far enough to prove the teleport path ran.");
                }

                // 12. Advance for a short period of time to verify that the kill state has been released correctly.

                blocker.Update(0.26f);
                if (blocker.IsSuperShot || core.IsSuperShot)
                {
                    errors.Add("Guaranteed-block skill did not release its short hold state after the block.");
                }
            }
            finally
            {
                // 13. Clean up runtime objects and restore original state

                core?.Shutdown();
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
                inventory.ParticipantMode = originalParticipantMode;
                inventory.SessionMode = originalSessionMode;
                inventory.GameMode = originalGameMode;
                inventory.MatchPrepared = originalMatchPrepared;
                inventory.PendingTutorialNextAction = originalPendingTutorialNextAction;
            }
        }

        /// <summary>
        /// Run all smoke tests: verify assets, build your game quickly, and report bugs.

        /// </summary>
        private static void ValidateFogWindAdventureFeedback(List<string> errors)
        {
            const float simulateDuration = 0.6f;
            const float simulateStep = 0.02f;
            const float windDisplacementThreshold = 16f;
            const float shooterX = 172f;
            const float shotX = 228f;
            const float shotY = 218f;
            const float shotVelocityX = 240f;
            const float shotVelocityY = -280f;
            var inventory = mlpInventory.Instance;
            var originalRandomState = UnityEngine.Random.state;
            var originalMatchData = inventory.MatchData;
            var originalAdventure = inventory.Adventure;
            var originalTournament = inventory.Tournament;
            var originalDifficulty = inventory.Difficulty;
            var originalParticipantMode = inventory.ParticipantMode;
            var originalSessionMode = inventory.SessionMode;
            var originalGameMode = inventory.GameMode;
            var originalMatchPrepared = inventory.MatchPrepared;
            var originalPendingTutorialNextAction = inventory.PendingTutorialNextAction;
            var originalQuickCharacterId = inventory.SelectedQuickCharacterId;
            var originalQuickBallSelection = inventory.SelectedQuickBallSelection;
            GameObject fogRoot = null;
            GameObject clearRoot = null;
            mlpGameCore fogCore = null;
            mlpGameCore clearCore = null;

            try
            {
                UnityEngine.Random.InitState(24681357);
                mlpPlayersData.SetupPlayers();
                var playerCharacterId = 0;
                foreach (var characterId in mlpPlayersData.GetActiveCharacterIds())
                {
                    playerCharacterId = characterId;
                    break;
                }

                var fogWindLevel = FindAdventureLevel(mlpAdventureMechanic.FogWind);
                if (fogWindLevel == null)
                {
                    errors.Add("FogWind smoke test could not find the FogWind adventure level definition.");
                    return;
                }

                inventory.Difficulty = mlpAiDifficulty.Normal;
                inventory.PendingTutorialNextAction = mlpTutorialNextAction.None;
                inventory.BeginAdventure(playerCharacterId);
                while (inventory.Adventure.HighestUnlockedLevelIndex < fogWindLevel.Index && !inventory.Adventure.Completed)
                {
                    inventory.Adventure.ApplyCurrentMatchResult(true);
                }

                if (!inventory.StartAdventureLevel(fogWindLevel.Index, playerCharacterId))
                {
                    errors.Add("FogWind smoke test could not start the FogWind adventure level.");
                    return;
                }

                fogRoot = new GameObject("FogWindAdventureSmokeRoot");
                mlpAudio.Create(fogRoot.transform);
                fogCore = new mlpGameBuilder().Build(fogRoot.transform);
                if (!AdvanceToLivePlay(fogCore, "FogWind smoke test", errors))
                {
                    return;
                }

                var fogWindFxRoot = GetFogWindFxRoot(fogCore);
                if (fogWindFxRoot == null)
                {
                    errors.Add("FogWind smoke test could not find the arena wind FX root.");
                    return;
                }

                SimulateKnownAirborneShot(fogCore, shooterX, shotX, shotY, shotVelocityX, shotVelocityY, simulateDuration, simulateStep);
                if (!fogWindFxRoot.activeSelf)
                {
                    errors.Add("FogWind adventure did not activate the arena wind FX root.");
                }

                inventory.SetQuickSelection(playerCharacterId);
                inventory.SetQuickBallSelection(mlpBallSelection.ClassicOriginal);
                inventory.StartQuickGame();
                clearRoot = new GameObject("ClearWeatherSmokeRoot");
                mlpAudio.Create(clearRoot.transform);
                clearCore = new mlpGameBuilder().Build(clearRoot.transform);
                if (!AdvanceToLivePlay(clearCore, "Clear-weather smoke test", errors))
                {
                    return;
                }

                var clearWindFxRoot = GetFogWindFxRoot(clearCore);
                if (clearWindFxRoot == null)
                {
                    errors.Add("Clear-weather smoke test could not find the arena wind FX root.");
                    return;
                }

                SimulateKnownAirborneShot(clearCore, shooterX, shotX, shotY, shotVelocityX, shotVelocityY, simulateDuration, simulateStep);
                if (clearWindFxRoot.activeSelf)
                {
                    errors.Add("Non-FogWind match unexpectedly activated the arena wind FX root.");
                }

                var horizontalDrift = Mathf.Abs(fogCore.Ball.Position.x - clearCore.Ball.Position.x);
                if (horizontalDrift < windDisplacementThreshold)
                {
                    errors.Add($"FogWind shot drift was too small. Expected at least {windDisplacementThreshold}px, got {horizontalDrift:0.00}px.");
                }
            }
            finally
            {
                fogCore?.Shutdown();
                clearCore?.Shutdown();
                if (fogRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(fogRoot);
                }

                if (clearRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(clearRoot);
                }

                inventory.MatchData = originalMatchData;
                inventory.Adventure = originalAdventure;
                inventory.Tournament = originalTournament;
                inventory.Difficulty = originalDifficulty;
                inventory.ParticipantMode = originalParticipantMode;
                inventory.SessionMode = originalSessionMode;
                inventory.GameMode = originalGameMode;
                inventory.MatchPrepared = originalMatchPrepared;
                inventory.PendingTutorialNextAction = originalPendingTutorialNextAction;
                inventory.SelectedQuickCharacterId = originalQuickCharacterId;
                inventory.SelectedQuickBallSelection = originalQuickBallSelection;
                UnityEngine.Random.state = originalRandomState;
            }
        }

        private static mlpAdventureLevelDefinition FindAdventureLevel(mlpAdventureMechanic mechanic)
        {
            for (var i = 0; i < mlpAdventureCatalog.LevelCount; i++)
            {
                var level = mlpAdventureCatalog.GetLevel(i);
                if (level.Mechanic == mechanic)
                {
                    return level;
                }
            }

            return null;
        }

        private static bool AdvanceToLivePlay(mlpGameCore core, string context, List<string> errors)
        {
            if (core == null)
            {
                errors.Add($"{context} could not build a game runtime.");
                return false;
            }

            for (var i = 0; i < 360; i++)
            {
                core.Update(0.016f);
                if (TryGetPrivateField(core, "isPlaying", out bool isPlaying) && isPlaying)
                {
                    return true;
                }
            }

            errors.Add($"{context} did not reach live play.");
            return false;
        }

        private static void SimulateKnownAirborneShot(mlpGameCore core, float shooterX, float shotX, float shotY, float shotVelocityX, float shotVelocityY, float duration, float step)
        {
            if (core == null || core.Ball == null || core.PlayersLeft == null || core.PlayersLeft.Count == 0)
            {
                return;
            }

            PositionPlayersForShot(core, shooterX);
            var shooter = core.PlayersLeft[0];
            core.Ball.TutorialSnapTo(new Vector2(shotX, shotY));
            core.Ball.Side = shooter.Side;
            core.Ball.LastShotX = shotX;
            core.Ball.State = "shooting";
            core.Ball.Velocity = new Vector2(shotVelocityX * -shooter.Side, shotVelocityY);
            core.NotifyPlayersBallShot(shooter.Side, shooter.PlayerNo);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                var frameDt = Mathf.Min(step, duration - elapsed);
                core.Update(frameDt);
                elapsed += frameDt;
            }
        }

        private static void PositionPlayersForShot(mlpGameCore core, float shooterX)
        {
            var floorY = mlpObjectsData.PlayerIndentY;
            for (var i = 0; i < core.PlayersLeft.Count; i++)
            {
                var x = i == 0 ? shooterX : 92f - i * 20f;
                core.PlayersLeft[i].TutorialSnapTo(new Vector2(x, floorY), 1f);
            }

            for (var i = 0; i < core.PlayersRight.Count; i++)
            {
                core.PlayersRight[i].TutorialSnapTo(new Vector2(728f + i * 20f, floorY), -1f);
            }
        }

        private static GameObject GetFogWindFxRoot(mlpGameCore core)
        {
            if (core == null || !TryGetPrivateField(core, "arena", out mlpArenaObject arena) || arena == null)
            {
                return null;
            }

            return TryGetPrivateField(arena, "fogWindFxRoot", out GameObject fogWindFxRoot) ? fogWindFxRoot : null;
        }

        private static bool TryGetPrivateField<T>(object target, string fieldName, out T value)
        {
            value = default;
            if (target == null)
            {
                return false;
            }

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return false;
            }

            var rawValue = field.GetValue(target);
            if (rawValue == null)
            {
                return true;
            }

            if (rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return false;
        }

        public static void Run()
        {
            // 1. Initialize error collection list and temporary runtime objects

            var errors = new List<string>();
            GameObject root = null;
            mlpGameCore core = null;

            try
            {
                // ---- Phase 1: Check whether the core resource file exists ----

                // 2. Check the atlas resources (game interface, textures and configuration files of skill effects)
                CheckResource<Texture2D>("mlp/Atlases/gameplay", errors);
                CheckResource<TextAsset>("mlp/Atlases/gameplay", errors);
                CheckResource<Texture2D>("mlp/Atlases/interface", errors);
                CheckResource<TextAsset>("mlp/Atlases/interface", errors);
                CheckResource<Texture2D>("mlp/Atlases/skillfx", errors);
                CheckResource<TextAsset>("mlp/Atlases/skillfx", errors);
                // 3. Check if the font file exists

                CheckResource<Font>("mlp/Fonts/Impact", errors);
                CheckResource<Font>("mlp/Fonts/Impact2", errors);
                CheckResource<Font>("mlp/Fonts/CfCrackBold", errors);
                CheckResource<Font>("mlp/Fonts/Rajdhani-SemiBold", errors);
                CheckResource<Font>("mlp/Fonts/Rajdhani-Bold", errors);
                CheckResource<Font>("mlp/Fonts/Griffy-Regular", errors);
                // 4. Check TextMeshPro for necessary resources and menu text layers

                EnsureTextMeshProEssentialResources(errors);
                ValidateNativeMenuTextLayer(errors);
                // 5. Check single-player narrative text and adventure mode level definitions

                ValidateSinglePlayerNarrativeDefinitions(errors);
                ValidateAdventureModeDefinitionsAndFlow(errors);
                // 6. Check UI image resources (pause button, music button, help button, various panels, etc.)

                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.PauseButton), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.MusicButtonOn), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.MusicButtonOff), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.HelpButton), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.FramePanelLarge), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.FrameMatchCardIdle), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.FrameMatchCardActive), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.MenuButtonPlate), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.EnergyButtonPlate), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.EmblemOrb), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.AdventureTreasureMapBg), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.AwardsShowcasePanel), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.AwardsResultPlaque), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.Ui.AwardsPodiumBase), errors);
                // 7. Check each character’s skill icon resources

                ValidateSkillIconAssets(errors);
                // 8. Check the HUD (heads-up display) and portrait gallery resources

                CheckResource<Texture2D>(mlpAssets.Hud.ResourcePath(mlpAssets.Hud.Scoreboard), errors);
                CheckResource<Texture2D>(mlpAssets.Hud.ResourcePath(mlpAssets.Hud.Popup), errors);
                CheckResource<TextAsset>(mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas), errors);
                CheckResource<Texture2D>(mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas), errors);
                // 9. Check if the texture of the Halloween themed ball exists

                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallGhoulGreen), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallPumpkinEmber), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallMoonlitViolet), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallJackOLantern), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallEvilEye), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCursed8Ball), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCandySwirl), errors);
                // 10. Examine DragonBones skeletal animation data and textures

                CheckResource<TextAsset>("mlp/DragonBones/sk2", errors);
                CheckResource<TextAsset>("mlp/DragonBones/texture2", errors);
                CheckResource<Texture2D>("mlp/DragonBones/texture2", errors);
                // 11. Check all sound effects files

                CheckAudioResources(errors);
                // 12. Verify sphere texture quality (size, corner transparency, no white halo)

                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallGhoulGreen), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_ghoul_green.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallPumpkinEmber), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_pumpkin_ember.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallMoonlitViolet), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_moonlit_violet.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallJackOLantern), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_jack_o_lantern.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallEvilEye), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_evil_eye.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCursed8Ball), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_cursed_8ball.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCandySwirl), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_candy_swirl.png", errors);

                // ---- Phase 2: Verify the integrity of the atlas and animation system ----

                // 13. Check if custom shader exists

                if (Shader.Find("mlp/TextMeshOutlined") == null)
                {
                    errors.Add("Could not find mlp/TextMeshOutlined shader.");
                }

                // 14. Verify that the three atlases contain the expected keyframes

                var gameplay = mlpAtlasCache.Instance.Gameplay;
                if (!gameplay.HasFrame("0bg_gameplay0000") || !gameplay.HasFrame("BallMC0000"))
                {
                    errors.Add("Gameplay atlas did not expose the expected frame keys.");
                }

                var ui = mlpAtlasCache.Instance.Interface;
                if (!ui.HasFrame("EmblemsBg0000") || !ui.HasFrame("loginSelect0000"))
                {
                    errors.Add("Interface atlas did not expose the expected active frame keys.");
                }

                var skillFx = mlpAtlasCache.Instance.SkillFx;
                if (!skillFx.HasFrame("ShieldMC20000") || !skillFx.HasFrame("teleport30000"))
                {
                    errors.Add("Skill FX atlas did not expose expected shield and teleport frame keys.");
                }

                // 15. Verify whether the DragonBones skeleton animation can be constructed and dressed normally

                DBLiteFactory.Instance.EnsureLoaded();
                var armature = mlpPlayersData.BuildGameplayArmature("SmokePlayerSmall");
                if (armature == null)
                {
                    errors.Add("Could not build DragonBones playerSmall armature.");
                }
                else
                {
                    // 16. Traverse all characters, test dressing and avatar loading

                    foreach (var characterId in mlpPlayersData.GetActiveCharacterIds())
                    {
                        mlpPlayersData.ApplyCharacter(armature, characterId);
                        if (mlpPlayersData.GetCharacterPortraitSprite(characterId) == null)
                        {
                            errors.Add($"Missing UI portrait sprite for active character {characterId}.");
                        }

                        if (mlpPlayersData.GetCharacterPortraitSprite(characterId, 28f) == null)
                        {
                            errors.Add($"Missing small UI portrait sprite variant for active character {characterId}.");
                        }
                    }

                    UnityEngine.Object.DestroyImmediate(armature.gameObject);
                }

                // 17. Verify that DragonBones data contains expected frame events

                var dragonBones = Resources.Load<TextAsset>("mlp/DragonBones/sk2");
                if (dragonBones == null || !dragonBones.text.Contains("\"mega\""))
                {
                    errors.Add("DragonBones data did not expose the expected mega frame event.");
                }

                // ----The third stage: verify game logic----

                // 18. Test difficulty switching loop and AI skill mapping

                ValidateDifficultyCycleAndSkillMapping(errors);
                // 19. Run the full tournament process on multiple difficulties

                ValidateTournamentSeasonMode(errors, mlpAiDifficulty.Normal, "normal");
                ValidateTournamentSeasonMode(errors, mlpAiDifficulty.Hard, "hard");
                ValidateTournamentSeasonMode(errors, mlpAiDifficulty.Hell, "hell");
                ValidateFixedTournamentSkillMapping(errors);
                // 20. Verify that sphere skin selection maintains correctly between modes

                ValidateBallSelectionStateAndResolution(errors);
                // 21. Verify the sharing and release of materials and meshes at runtime

                ValidateRuntimeGraphicsResourceReuse(errors);
                // 22. Verify the transmission logic of the guaranteed-blocking skill

                ValidateGuaranteedBlockTeleportsToShot(errors);
                ValidateFogWindAdventureFeedback(errors);

                // ---- Phase 4: Build the game runtime and verify actual operation ----

                // 23. When creating a temporary game, quickly start a game and update a frame
                root = new GameObject("SmokeRuntimeRoot");
                mlpAudio.Create(root.transform);
                mlpInventory.Instance.SetQuickSelection(0);
                mlpInventory.Instance.SetQuickBallSelection(mlpBallSelection.ClassicOriginal);
                mlpInventory.Instance.StartQuickGame();
                core = new mlpGameBuilder().Build(root.transform);
                core.Update(0.016f);
                // 24. Verify that a blocked shot can still score.

                ValidateBlockedShotScorePersistence(core, errors);
                // 25. Tutorial entry to verify help panel prefab

                ValidateHelpPanelTutorialEntry(errors);
                // 26. Verify startup and cleanup of tutorial mode

                ValidateTutorialModeBoot(errors);
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            finally
            {
                // 27. Clean up runtime objects regardless of success or failure

                core?.Shutdown();
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            // 28. Output result: If there is an error, report it and exit with failure, otherwise exit with success

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Debug.LogError(error);
                }

                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("mlp smoke test passed.");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Check whether the resource exists in the specified path, and log an error if it is missing.

        /// </summary>
        private static void CheckResource<T>(string path, List<string> errors) where T : UnityEngine.Object
        {
            if (Resources.Load<T>(path) == null)
            {
                errors.Add($"Missing resource: {path}");
            }
        }

        /// <summary>
        /// Verify that native TMP menu text layer can create title and button labels.

        /// </summary>
        private static void ValidateNativeMenuTextLayer(List<string> errors)
        {
            var root = new GameObject("NativeMenuTextSmokeRoot");
            mlpNativeMenuTextLayer layer = null;

            try
            {
                layer = new mlpNativeMenuTextLayer(root.transform);
                layer.RefreshLayout(mlpConstants.Width, 480);

                var heading = layer.CreateText(
                    "SmokeHeading",
                    "TOURNAMENT",
                    mlpConstants.Width2,
                    80f,
                    20,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    mlpTextStyle.TournamentAccent);
                if (heading == null || heading.font == null)
                {
                    errors.Add("Native menu TMP text layer could not create a heading with a font asset.");
                }

                var button = new mlpMenuButton("PLAY", mlpConstants.Width2, 440f, 150f, 42f, null, root.transform);
                var tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
                var legacyTexts = root.GetComponentsInChildren<TextMesh>(true);
                if (tmpTexts.Length < 2)
                {
                    errors.Add("Native menu TMP text layer did not create the expected TMP heading and button label.");
                }

                if (legacyTexts.Length > 0)
                {
                    errors.Add("Native menu button labels still created legacy TextMesh components while the TMP menu layer was active.");
                }

                button.SetVisible(false);
            }
            finally
            {
                layer?.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Checks that all single player mode narrative text entries and mode definitions exist.

        /// </summary>
        private static void ValidateSinglePlayerNarrativeDefinitions(List<string> errors)
        {
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.ParkName, "park name", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.PumpkinHeartLantern, "core lantern", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.LanternSigil, "sigil", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.Warden, "warden", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.MidnightLockdownProtocol, "lockdown protocol", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.LanternChampion, "champion title", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.AdventurePreviewStatus, "adventure entry status", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.TournamentPreviewStatus, "tournament entry status", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.ComicReplayButton, "comic replay button", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.AdventureLinkToCup, "adventure to cup link", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.CupLinkToAdventure, "cup to adventure link", errors);

            ValidateNarrativeMode(mlpSinglePlayerNarrative.Adventure, mlpSinglePlayerNarrativeMode.Adventure, 3, errors);
            ValidateNarrativeMode(mlpSinglePlayerNarrative.Tournament, mlpSinglePlayerNarrativeMode.Tournament, 3, errors);

            if (!ReferenceEquals(
                    mlpSinglePlayerNarrative.GetMode(mlpSinglePlayerNarrativeMode.Adventure),
                    mlpSinglePlayerNarrative.Adventure))
            {
                errors.Add("Single-player narrative lookup did not resolve Adventure mode.");
            }

            if (!ReferenceEquals(
                    mlpSinglePlayerNarrative.GetMode(mlpSinglePlayerNarrativeMode.Tournament),
                    mlpSinglePlayerNarrative.Tournament))
            {
                errors.Add("Single-player narrative lookup did not resolve Tournament mode.");
            }
        }

        private static void ValidateNarrativeMode(
            mlpSinglePlayerModeDefinition mode,
            mlpSinglePlayerNarrativeMode expectedMode,
            int expectedComicPanels,
            List<string> errors)
        {
            if (mode == null)
            {
                errors.Add($"Single-player narrative mode {expectedMode} is missing.");
                return;
            }

            if (mode.Mode != expectedMode)
            {
                errors.Add($"Single-player narrative mode {expectedMode} has mismatched mode id {mode.Mode}.");
            }

            ValidateNarrativeTerm(mode.ModeName, $"{expectedMode} mode name", errors);
            ValidateNarrativeTerm(mode.MenuTitle, $"{expectedMode} menu title", errors);
            ValidateNarrativeTerm(mode.Subtitle, $"{expectedMode} subtitle", errors);
            ValidateNarrativeTerm(mode.Objective, $"{expectedMode} objective", errors);
            ValidateNarrativeTerm(mode.Tone, $"{expectedMode} tone", errors);
            ValidateNarrativeTerm(mode.GameplayWrapper, $"{expectedMode} gameplay wrapper", errors);
            ValidateNarrativeTerm(mode.WorldRole, $"{expectedMode} world role", errors);

            if (mode.OpeningComic == null || mode.OpeningComic.Length != expectedComicPanels)
            {
                errors.Add($"{expectedMode} opening comic should have {expectedComicPanels} panels.");
                return;
            }

            for (var i = 0; i < mode.OpeningComic.Length; i++)
            {
                var panel = mode.OpeningComic[i];
                if (panel == null)
                {
                    errors.Add($"{expectedMode} opening comic panel {i + 1} is missing.");
                    continue;
                }

                ValidateNarrativeTerm(panel.Caption, $"{expectedMode} panel {i + 1} caption", errors);
                ValidateNarrativeTerm(panel.ArtDirection, $"{expectedMode} panel {i + 1} art direction", errors);
                ValidateNarrativeTerm(panel.ImageKey, $"{expectedMode} panel {i + 1} image key", errors);
                if (!string.IsNullOrWhiteSpace(panel.ImageKey))
                {
                    var resourcePath = mlpAssets.Images.ResourcePath(panel.ImageKey);
                    var texture = Resources.Load<Texture2D>(resourcePath);
                    if (texture == null)
                    {
                        errors.Add($"Missing opening comic image resource: {resourcePath}");
                    }
                    else if (texture.width < 1280 || texture.height < 720)
                    {
                        errors.Add($"{expectedMode} opening comic panel {i + 1} should be high-resolution, got {texture.width}x{texture.height}.");
                    }

                    var assetPath = $"Assets/mlp/Resources/mlp/Images/{panel.ImageKey}.png";
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null)
                    {
                        errors.Add($"Opening comic image importer is missing: {assetPath}");
                    }
                    else
                    {
                        if (importer.mipmapEnabled)
                        {
                            errors.Add($"{expectedMode} opening comic panel {i + 1} should have mipmaps disabled for crisp fullscreen UI.");
                        }

                        if (importer.npotScale != TextureImporterNPOTScale.None)
                        {
                            errors.Add($"{expectedMode} opening comic panel {i + 1} should preserve non-power-of-two dimensions.");
                        }

                        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                        {
                            errors.Add($"{expectedMode} opening comic panel {i + 1} should use uncompressed import quality.");
                        }
                    }
                }
            }
        }

        private static void ValidateNarrativeTerm(string text, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                errors.Add($"Single-player narrative {label} is empty.");
            }
        }

        private static void ValidateAdventureModeDefinitionsAndFlow(List<string> errors)
        {
            var levels = mlpAdventureCatalog.AllLevels;
            if (levels == null || levels.Length != 8)
            {
                errors.Add("Adventure mode should define exactly 8 levels.");
                return;
            }

            var seenWardens = new HashSet<int>();
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                if (level == null)
                {
                    errors.Add($"Adventure level {i + 1} is missing.");
                    continue;
                }

                if (level.Index != i)
                {
                    errors.Add($"Adventure level {i + 1} has mismatched index {level.Index}.");
                }

                ValidateNarrativeTerm(level.AreaName, $"adventure level {i + 1} area", errors);
                ValidateNarrativeTerm(level.Mood, $"adventure level {i + 1} mood", errors);
                ValidateNarrativeTerm(level.MechanicTitle, $"adventure level {i + 1} mechanic title", errors);
                ValidateNarrativeTerm(level.MechanicSummary, $"adventure level {i + 1} mechanic summary", errors);
                ValidateNarrativeTerm(level.SceneDirection, $"adventure level {i + 1} scene direction", errors);
                ValidateNarrativeTerm(level.VictoryBeat, $"adventure level {i + 1} victory beat", errors);
                if (level.VictoryLines == null || level.VictoryLines.Length < 3)
                {
                    errors.Add($"Adventure level {i + 1} needs at least three victory result lines.");
                }
                else
                {
                    for (var lineIndex = 0; lineIndex < level.VictoryLines.Length; lineIndex++)
                    {
                        ValidateNarrativeTerm(
                            level.VictoryLines[lineIndex],
                            $"adventure level {i + 1} victory line {lineIndex + 1}",
                            errors);
                    }
                }

                if (level.DefeatLines == null || level.DefeatLines.Length < 3)
                {
                    errors.Add($"Adventure level {i + 1} needs at least three defeat result lines.");
                }
                else
                {
                    for (var lineIndex = 0; lineIndex < level.DefeatLines.Length; lineIndex++)
                    {
                        ValidateNarrativeTerm(
                            level.DefeatLines[lineIndex],
                            $"adventure level {i + 1} defeat line {lineIndex + 1}",
                            errors);
                    }
                }

                if (!seenWardens.Add(level.WardenCharacterId))
                {
                    errors.Add($"Adventure level {i + 1} repeats Warden character {level.WardenCharacterId}.");
                }

                if (level.RuleIcons == null || level.RuleIcons.Length < 2)
                {
                    errors.Add($"Adventure level {i + 1} needs at least two rule icons.");
                }

                // OpponentSkill is the basic strength field of the old version of adventure. It is not directly involved in AI skill calculation in the current fixed four-level mode.

                // Therefore, it is no longer required to fall within the four-level index range of 0..3, only the level structure and process verification are retained.

            }

            var adventure = new mlpAdventureData();
            adventure.Create(0);
            if (!adventure.Active || adventure.Completed || !adventure.IsLevelUnlocked(0) || adventure.IsLevelUnlocked(1))
            {
                errors.Add("Adventure flow did not start with only the first level unlocked.");
            }

            if (!adventure.SelectLevel(0))
            {
                errors.Add("Adventure flow could not select the first unlocked level.");
            }

            var firstLevel = mlpAdventureCatalog.GetLevel(0);
            var matchData = new mlpMatchData(true);
            matchData.StartAdventureMatch(adventure);
            if (matchData.CharacterIds[1] != firstLevel.WardenCharacterId)
            {
                errors.Add("Adventure match did not use the first level Warden as opponent.");
            }

            AssertOpponentSkill(matchData, mlpAISkillsData.NormalSkillIndex, errors, "Adventure default normal mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Easy);
            AssertOpponentSkill(matchData, mlpAISkillsData.EasySkillIndex, errors, "Adventure easy mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Normal);
            AssertOpponentSkill(matchData, mlpAISkillsData.NormalSkillIndex, errors, "Adventure normal mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Hard);
            AssertOpponentSkill(matchData, mlpAISkillsData.HardSkillIndex, errors, "Adventure hard mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Hell);
            AssertOpponentSkill(matchData, mlpAISkillsData.HellSkillIndex, errors, "Adventure hell mapping");

            adventure.ApplyCurrentMatchResult(true);
            if (!adventure.IsLevelCompleted(0) || !adventure.IsLevelUnlocked(1) || adventure.SigilsCollected != 1)
            {
                errors.Add("Adventure victory did not claim a Sigil and unlock the second level.");
            }

            if (!adventure.SelectLevel(1))
            {
                errors.Add("Adventure flow could not select the newly unlocked second level.");
            }

            adventure.ApplyCurrentMatchResult(false);
            if (adventure.IsLevelCompleted(1) || adventure.IsLevelUnlocked(2))
            {
                errors.Add("Adventure defeat should not complete a level or unlock the next route.");
            }

            for (var i = 1; i < mlpAdventureCatalog.LevelCount; i++)
            {
                adventure.SelectLevel(i);
                adventure.ApplyCurrentMatchResult(true);
            }

            if (!adventure.Completed || adventure.SigilsCollected != mlpAdventureCatalog.LevelCount)
            {
                errors.Add("Adventure flow did not complete after all levels were won.");
            }

            ValidateInventoryAdventurePlayerSelectionRefresh(errors);

            ValidateNarrativeTerm(mlpSinglePlayerNarrative.GetTournamentStageTitle(new mlpTournamentData()), "tournament stage title", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.GetTournamentStageDescription(new mlpTournamentData()), "tournament stage description", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.GetTournamentPlacementEnding(1), "tournament champion ending", errors);
        }

        /// <summary>
        /// Verifies adventure matches rebuild when the selected player character changes.
        /// </summary>
        private static void ValidateInventoryAdventurePlayerSelectionRefresh(List<string> errors)
        {
            var inventory = mlpInventory.Instance;
            var originalMatchData = inventory.MatchData;
            var originalAdventure = inventory.Adventure;
            var originalTournament = inventory.Tournament;
            var originalParticipantMode = inventory.ParticipantMode;
            var originalSessionMode = inventory.SessionMode;
            var originalGameMode = inventory.GameMode;
            var originalMatchPrepared = inventory.MatchPrepared;
            var originalDifficulty = inventory.Difficulty;
            var originalPendingTutorialNextAction = inventory.PendingTutorialNextAction;

            try
            {
                inventory.MatchData = new mlpMatchData(true);
                inventory.Adventure = new mlpAdventureData();
                inventory.Tournament = new mlpTournamentData();
                inventory.Difficulty = mlpAiDifficulty.Normal;
                inventory.BeginAdventure(0);

                const int selectedCharacterId = 3;
                if (!inventory.StartAdventureLevel(0, selectedCharacterId))
                {
                    errors.Add("Adventure player-selection refresh test could not start the first level.");
                    return;
                }

                if (inventory.Adventure.PlayerCharacterId != selectedCharacterId)
                {
                    errors.Add($"Adventure did not refresh its player character when selection changed. Expected {selectedCharacterId}, got {inventory.Adventure.PlayerCharacterId}.");
                }

                if (inventory.MatchData.CharacterIds[0] != selectedCharacterId)
                {
                    errors.Add($"Adventure match did not use the selected player character. Expected {selectedCharacterId}, got {inventory.MatchData.CharacterIds[0]}.");
                }
            }
            finally
            {
                inventory.MatchData = originalMatchData ?? new mlpMatchData(true);
                inventory.Adventure = originalAdventure ?? new mlpAdventureData();
                inventory.Tournament = originalTournament ?? new mlpTournamentData();
                inventory.ParticipantMode = originalParticipantMode;
                inventory.SessionMode = originalSessionMode;
                inventory.GameMode = originalGameMode;
                inventory.MatchPrepared = originalMatchPrepared;
                inventory.Difficulty = originalDifficulty;
                inventory.PendingTutorialNextAction = originalPendingTutorialNextAction;
            }
        }

        /// <summary>
        /// Ensures TextMesh Pro essential resources are present.
        /// </summary>
        private static void EnsureTextMeshProEssentialResources(List<string> errors)
        {
            var hasSettings = AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;
            var hasShader = Shader.Find("TextMeshPro/Mobile/Distance Field") != null;
            if (hasSettings && hasShader)
            {
                return;
            }

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (packageInfo == null)
            {
                errors.Add("Could not resolve the TextMeshPro package path needed to import essential resources.");
                return;
            }

            var packagePath = Path.Combine(packageInfo.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(packagePath))
            {
                errors.Add($"TMP essential resources package was missing at {packagePath}.");
                return;
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0 || Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                errors.Add("TMP essential resources could not be imported into the project.");
            }
        }

        /// <summary>
        /// Verify that all expected audio clips are present in the Resources folder.

        /// </summary>
        private static void CheckAudioResources(List<string> errors)
        {
            var audioPaths = new[]
            {
                "mlp/Sound/whistle",
                "mlp/Sound/teleport",
                "mlp/Sound/swoosh",
                "mlp/Sound/energy",
                "mlp/Sound/stunned",
                "mlp/Sound/clash",
                "mlp/Sound/buzzer",
                "mlp/Sound/rim_hit",
                "mlp/Sound/mega_dunk",
                "mlp/Sound/shield",
                "mlp/Sound/ball_bounce",
                "mlp/Sound/dash",
                "mlp/Sound/super_dash",
                "mlp/Sound/countdown",
                "mlp/Sound/button",
                "mlp/Sound/net",
                "mlp/Sound/brick",
                "mlp/Sound/basket",
                "mlp/Sound/bgm",
            };

            for (var i = 0; i < audioPaths.Length; i++)
            {
                CheckResource<AudioClip>(audioPaths[i], errors);
            }
        }

        /// <summary>
        /// Check that materials and meshes are shared and released correctly at runtime.

        /// </summary>
        private static void ValidateRuntimeGraphicsResourceReuse(List<string> errors)
        {
            ValidateSharedRuntimeMaterials(errors);
            ValidateGameRuntimeMeshRelease(errors);
        }

        /// <summary>
        /// Check the help panel prefab to see if there is a replay tutorial button.

        /// </summary>
        private static void ValidateHelpPanelTutorialEntry(List<string> errors)
        {
            var prefab = Resources.Load<mlpHelpPanel>("mlp/Prefabs/UI/MlpHelpPanel");
            if (prefab == null)
            {
                errors.Add("Missing help panel prefab at Resources/mlp/Prefabs/UI/MlpHelpPanel.");
                return;
            }

            mlpHelpPanel instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                var buttons = instance.GetComponentsInChildren<mlpHelpButton>(true);
                var replayButtons = 0;
                for (var i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].Action == mlpHelpButtonAction.ReplayTutorial)
                    {
                        replayButtons++;
                    }
                }

                if (replayButtons <= 0)
                {
                    errors.Add("Help panel no longer exposes a Replay Tutorial button.");
                }
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance.gameObject);
                }
            }
        }

        /// <summary>
        /// Verify that tutorial mode creates flows, overlays, and cleans focus markers correctly.

        /// </summary>
        private static void ValidateTutorialModeBoot(List<string> errors)
        {
            // 1. Clear the existing tutorial overlay in the scene (to avoid interfering with testing)

            var existingOverlay = UnityEngine.Object.FindObjectOfType<mlpTutorialOverlay>();
            if (existingOverlay != null)
            {
                UnityEngine.Object.DestroyImmediate(existingOverlay.gameObject);
            }

            // 2. Create a temporary runtime root object

            var runtimeRoot = new GameObject("TutorialSmokeRoot");
            mlpGameCore tutorialCore = null;
            mlpTutorialOverlay tutorialOverlay = null;

            try
            {
                // 3. Start tutorial mode and build the game runtime

                mlpAudio.Create(runtimeRoot.transform);
                mlpInventory.Instance.SetTrainingSelection(0);
                mlpInventory.Instance.SetTrainingBallSelection(mlpBallSelection.ClassicOriginal);
                mlpInventory.Instance.StartTutorial();
                tutorialCore = new mlpGameBuilder().Build(runtimeRoot.transform);
                tutorialCore.Update(0.016f);

                // 4. Verify whether the tutorial process object is created

                if (tutorialCore.TutorialFlow == null)
                {
                    errors.Add("Tutorial mode did not create a tutorial flow.");
                }

                // 5. Verify that the tutorial overlay UI exists

                tutorialOverlay = UnityEngine.Object.FindObjectOfType<mlpTutorialOverlay>();
                if (tutorialOverlay == null)
                {
                    errors.Add("Tutorial mode did not create the tutorial overlay.");
                    return;
                }

                // 6. Use reflection to check whether the UI root node of the overlay is built successfully.

                var overlayRootField = typeof(mlpTutorialOverlay).GetField("overlayRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (overlayRootField == null || overlayRootField.GetValue(tutorialOverlay) == null)
                {
                    errors.Add("Tutorial overlay did not build its runtime UI root.");
                }

                // 7. Verify that the focus mark is cleared correctly after completing the tutorial steps.

                ValidateTutorialStepCleanup(tutorialCore, tutorialOverlay, errors);
            }
            finally
            {
                // 8. Clean up all temporary objects

                tutorialCore?.Shutdown();
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
                if (tutorialOverlay != null)
                {
                    UnityEngine.Object.DestroyImmediate(tutorialOverlay.gameObject);
                }
            }
        }

        private static void ValidateTutorialStepCleanup(mlpGameCore tutorialCore, mlpTutorialOverlay tutorialOverlay, List<string> errors)
        {
            if (tutorialCore?.TutorialFlow == null || tutorialOverlay == null)
            {
                return;
            }

            var beginSuperMethod = typeof(mlpTutorialFlow).GetMethod("BeginSuper", BindingFlags.Instance | BindingFlags.NonPublic);
            var completeStepMethod = typeof(mlpTutorialFlow).GetMethod("CompleteStep", BindingFlags.Instance | BindingFlags.NonPublic);
            var maskTopField = typeof(mlpTutorialOverlay).GetField("maskTop", BindingFlags.Instance | BindingFlags.NonPublic);
            var focusFrameField = typeof(mlpTutorialOverlay).GetField("focusFrame", BindingFlags.Instance | BindingFlags.NonPublic);

            if (beginSuperMethod == null || completeStepMethod == null || maskTopField == null || focusFrameField == null)
            {
                errors.Add("Tutorial cleanup smoke test could not access private tutorial members.");
                return;
            }

            beginSuperMethod.Invoke(tutorialCore.TutorialFlow, new object[] { true });
            var maskTop = maskTopField.GetValue(tutorialOverlay) as RectTransform;
            var focusFrame = focusFrameField.GetValue(tutorialOverlay) as RectTransform;
            if (maskTop == null || focusFrame == null)
            {
                errors.Add("Tutorial cleanup smoke test could not read tutorial focus markers.");
                return;
            }

            if (maskTop.gameObject.activeSelf || focusFrame.gameObject.activeSelf)
            {
                errors.Add("Tutorial super step unexpectedly enabled the removed focus overlay.");
                return;
            }

            completeStepMethod.Invoke(tutorialCore.TutorialFlow, new object[] { "cleanup-check" });
            if (maskTop.gameObject.activeSelf || focusFrame.gameObject.activeSelf)
            {
                errors.Add("Tutorial step completion left the focus overlay active after success.");
            }
        }

        /// <summary>
        /// Check if basket mesh and radial icon mesh share materials instead of being duplicated.

        /// </summary>
        private static void ValidateSharedRuntimeMaterials(List<string> errors)
        {
            var runtimeRoot = new GameObject("GraphicsReuseSmokeRoot");
            try
            {
                new mlpBasketObject(-1, runtimeRoot.transform);
                new mlpBasketObject(1, runtimeRoot.transform);

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

                var maskTexture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.SkillIcons.ScarecrowMask));
                if (maskTexture == null)
                {
                    errors.Add("Runtime graphics reuse test could not load the standalone skill mask texture.");
                }
                else
                {
                    new mlpRadialIconMesh("EnergyFillSmokeA", maskTexture, 40f, 40f, 10, runtimeRoot.transform, 40f);
                    new mlpRadialIconMesh("EnergyFillSmokeB", maskTexture, 72f, 40f, 10, runtimeRoot.transform, 40f);
                }

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

        /// <summary>
        /// Verify that the dynamic grid of radial icons is released when the game is closed.
        /// </summary>
        private static void ValidateGameRuntimeMeshRelease(List<string> errors)
        {
            var runtimeRoot = new GameObject("GraphicsReleaseSmokeRoot");
            mlpGameCore core = null;
            Mesh ownedMesh = null;
            MeshFilter meshFilter = null;

            try
            {
                mlpAudio.Create(runtimeRoot.transform);
                mlpInventory.Instance.SetQuickSelection(0);
                mlpInventory.Instance.SetQuickBallSelection(mlpBallSelection.ClassicOriginal);
                mlpInventory.Instance.StartQuickGame();
                core = new mlpGameBuilder().Build(runtimeRoot.transform);

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

        /// <summary>
        /// Verify that a blocked shot can still score after being deflected.

        /// </summary>
        private static void ValidateBlockedShotScorePersistence(mlpGameCore core, List<string> errors)
        {
            // 1. Confirm that the runtime object is accessible

            if (core == null || core.Ball == null || core.PlayersRight == null || core.PlayersRight.Count == 0)
            {
                errors.Add("Blocked-shot score regression test could not access the runtime ball/blocker state.");
                return;
            }

            // 2. Simulate a shot and let the ball pass the first sensor (above the rim)

            core.MatchProcessor.Shoot(-1, true, 0);
            core.Ball.Shoot(-1, 240f, 260f, 0f, 1f);
            core.MatchProcessor.ProcessSensor(0);

            // 3. Block the ball once

            core.Ball.ApplyBlock(core.PlayersRight[0]);

            // 4. Reading the internal scoring state of the ball through reflection

            var canScoreField = typeof(mlpBallObject).GetField("canScore", BindingFlags.Instance | BindingFlags.NonPublic);
            var scoreArmedSideField = typeof(mlpBallObject).GetField("scoreArmedSide", BindingFlags.Instance | BindingFlags.NonPublic);
            if (canScoreField == null || scoreArmedSideField == null)
            {
                errors.Add("Blocked-shot score regression test could not inspect mlpBallObject scoring state.");
                return;
            }

            // 5. Verify that the ball remains eligible for scoring after being blocked

            var canScore = (bool)canScoreField.GetValue(core.Ball);
            var scoreArmedSide = (int)scoreArmedSideField.GetValue(core.Ball);
            if (!canScore)
            {
                errors.Add("Blocked shot unexpectedly lost its scoring eligibility before entering the basket.");
            }

            // 6. Verify that the scoring direction has not been changed after blocking the shot

            if (scoreArmedSide != -1)
            {
                errors.Add($"Blocked shot lost its original scoring side. Expected -1, got {scoreArmedSide}.");
            }

            // 7. Let the ball pass the second sensor (under the basket) to verify that the scoring process can be completed.

            if (!core.MatchProcessor.ProcessSensor(1))
            {
                errors.Add("Blocked shot did not preserve the upper-sensor progress needed to finish the made-basket chain.");
                return;
            }

            // 8. Verification that a blocked shot is correctly worth 2 points (not 3 points)

            var points = core.MatchProcessor.ResolvePointsForScore(-1, 3);
            if (points != 2)
            {
                errors.Add($"Blocked shot that still scored should resolve as 2 points, got {points}.");
            }
        }

        /// <summary>
        /// Check that the sphere texture is 36x36 with transparent corners and no white glow.

        /// </summary>
        private static void ValidateBallSpriteAsset(string resourcePath, string assetPath, List<string> errors)
        {
            // 1. Load the sphere texture and confirm that it exists

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                errors.Add($"Missing ball texture: {resourcePath}");
                return;
            }

            // 2. Check whether the texture size is 36x36 pixels

            if (texture.width != 36 || texture.height != 36)
            {
                errors.Add($"{resourcePath} expected 36x36, got {texture.width}x{texture.height}.");
            }

            // 3. Check whether the original image file exists

            if (!File.Exists(assetPath))
            {
                errors.Add($"Missing ball asset file: {assetPath}");
                return;
            }

            // 4. Load original image from disk for pixel-level quality check

            var inspector = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!inspector.LoadImage(File.ReadAllBytes(assetPath), false))
                {
                    errors.Add($"Could not decode ball asset file: {assetPath}");
                    return;
                }

                // 5. Check whether the pixels in the four corners are transparent (make sure there is no residual image when the ball rotates)

                ValidateCornerAlpha(inspector, resourcePath, 0, 0, errors);
                ValidateCornerAlpha(inspector, resourcePath, inspector.width - 1, 0, errors);
                ValidateCornerAlpha(inspector, resourcePath, 0, inspector.height - 1, errors);
                ValidateCornerAlpha(inspector, resourcePath, inspector.width - 1, inspector.height - 1, errors);
                // 6. Check whether the sphere fills the entire canvas (no extra blank margins)

                ValidateBallBounds(inspector, resourcePath, errors);
                // 7. Check for translucent white pixels (can cause white halo artifacts)

                ValidateNoWhiteHalo(inspector, resourcePath, errors);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inspector);
            }
        }

        /// <summary>
        /// Check that each character's skill icon and charge mask textures are present and meet quality requirements.

        /// </summary>
        private static void ValidateSkillIconAssets(List<string> errors)
        {
            var validatedImageKeys = new HashSet<string>();
            for (var characterId = 0; characterId < mlpPlayersData.CharacterCount; characterId++)
            {
                var skillDefinition = mlpCharacterSkillsData.Get(characterId);
                if (!skillDefinition.HasStandaloneIconArt)
                {
                    errors.Add($"Character {characterId} no longer provides standalone skill icon art.");
                    continue;
                }

                ValidateSkillIconTexture(skillDefinition.IconImageKey, validatedImageKeys, errors);
                ValidateSkillIconTexture(skillDefinition.ChargeMaskImageKey, validatedImageKeys, errors);
            }
        }

        /// <summary>
        /// Verify individual skill icon textures: exist, are of sufficient size, and have correct import settings.

        /// </summary>
        private static void ValidateSkillIconTexture(string imageKey, HashSet<string> validatedImageKeys, List<string> errors)
        {
            if (string.IsNullOrEmpty(imageKey) || !validatedImageKeys.Add(imageKey))
            {
                return;
            }

            var resourcePath = mlpAssets.Images.ResourcePath(imageKey);
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                errors.Add($"Missing skill icon texture: {resourcePath}");
                return;
            }

            if (texture.width < 512 || texture.height < 512)
            {
                errors.Add($"{resourcePath} should stay at least 512x512 for crisp HUD rendering, got {texture.width}x{texture.height}.");
            }

            ValidateStandaloneUiTextureImport(
                resourcePath,
                $"Assets/mlp/Resources/{resourcePath}.png",
                errors);
        }

        /// <summary>
        /// Checks that the UI texture has mipmap disabled, alpha transparency enabled, and uncompressed.

        /// </summary>
        private static void ValidateStandaloneUiTextureImport(string resourcePath, string assetPath, List<string> errors)
        {
            if (!File.Exists(assetPath))
            {
                errors.Add($"Missing standalone UI asset file: {assetPath}");
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                errors.Add($"Could not inspect texture importer for {assetPath}.");
                return;
            }

            if (importer.mipmapEnabled)
            {
                errors.Add($"{resourcePath} should disable mipmaps for sharp UI rendering.");
            }

            if (!importer.alphaIsTransparency)
            {
                errors.Add($"{resourcePath} should enable alpha transparency handling.");
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                errors.Add($"{resourcePath} should use bilinear filtering, got {importer.filterMode}.");
            }

            if (importer.maxTextureSize < 4096)
            {
                errors.Add($"{resourcePath} should keep max texture size at 4096, got {importer.maxTextureSize}.");
            }

            ValidatePlatformTextureSettings(resourcePath, importer.GetPlatformTextureSettings("DefaultTexturePlatform"), errors);
            ValidatePlatformTextureSettings(resourcePath, importer.GetPlatformTextureSettings("Standalone"), errors);
        }

        /// <summary>
        /// Check if platform-specific texture settings override the default and leave them uncompressed.

        /// </summary>
        private static void ValidatePlatformTextureSettings(string resourcePath, TextureImporterPlatformSettings settings, List<string> errors)
        {
            if (!settings.overridden)
            {
                errors.Add($"{resourcePath} should override {settings.name} import settings to avoid low-quality UI compression.");
            }

            if (settings.maxTextureSize < 4096)
            {
                errors.Add($"{resourcePath} should keep {settings.name} max texture size at 4096, got {settings.maxTextureSize}.");
            }

            if (settings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                errors.Add($"{resourcePath} should keep {settings.name} uncompressed for clean UI edges.");
            }
        }

        /// <summary>
        /// Check if the corner pixels of the sphere texture are transparent to ensure clean and no ghosting when rotating.

        /// </summary>
        private static void ValidateCornerAlpha(Texture2D texture, string resourcePath, int x, int y, List<string> errors)
        {
            if (texture.GetPixel(x, y).a > 0.03f)
            {
                errors.Add($"{resourcePath} corner ({x},{y}) should stay transparent for rotation.");
            }
        }

        /// <summary>
        /// Checks if the sphere fills the entire 36x36 canvas with no empty margins.

        /// </summary>
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

        /// <summary>
        /// Check whether translucent pixels are close to white, which would cause visible halos.
        /// </summary>
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

        /// <summary>
        /// Verify that sphere selection is maintained correctly between Quick, Training, Versus, and Championship modes.

        /// </summary>
        private static void ValidateBallSelectionStateAndResolution(List<string> errors)
        {
            var inventory = mlpInventory.Instance;
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
                if (mlpAssets.Images.BallTheme(mlpBallTheme.ClassicOriginal) != null)
                {
                    errors.Add("Classic ball theme should stay atlas-backed and not resolve to an external image path.");
                }

                if (mlpGameplaySpriteLoader.LoadBallThemeSprite(mlpBallTheme.ClassicOriginal, 0.5f, 0.5f) != null)
                {
                    errors.Add("Classic ball theme unexpectedly resolved through the external ball sprite loader.");
                }

                inventory.SetQuickSelection(0);
                inventory.SetQuickBallSelection(mlpBallSelection.GhoulGreen);
                inventory.Difficulty = mlpAiDifficulty.Normal;
                inventory.StartQuickGame();
                if (inventory.MatchData.BallTheme != mlpBallTheme.GhoulGreen)
                {
                    errors.Add("Quick Match did not use the quick-mode ball selection.");
                }

                inventory.SetTrainingSelection(1);
                inventory.SetTrainingBallSelection(mlpBallSelection.EvilEye);
                inventory.StartTraining();
                if (inventory.MatchData.BallTheme != mlpBallTheme.EvilEye)
                {
                    errors.Add("Training did not use the training-mode ball selection.");
                }

                inventory.SetVersusBallSelection(mlpBallSelection.Cursed8Ball);
                inventory.StartTwoPlayerVersus(0, 1);
                if (inventory.MatchData.BallTheme != mlpBallTheme.Cursed8Ball)
                {
                    errors.Add("2 Players did not use the versus-mode ball selection.");
                }

                inventory.SetTournamentSelection(0);
                inventory.SetTournamentBallSelection(mlpBallSelection.CandySwirl);
                inventory.Difficulty = mlpAiDifficulty.Normal;
                if (!inventory.BeginTournament())
                {
                    errors.Add("Tournament ball selection test could not create a tournament.");
                }
                else if (inventory.MatchData.BallTheme != mlpBallTheme.CandySwirl)
                {
                    errors.Add("Tournament did not use the tournament-mode ball selection.");
                }

                var randomMatchData = new mlpMatchData(true);
                var seenThemes = new HashSet<mlpBallTheme>();
                UnityEngine.Random.InitState(24680);
                for (var i = 0; i < 16; i++)
                {
                    randomMatchData.StartQuickMatch(0, mlpAiDifficulty.Normal, mlpBallSelection.Random);
                    seenThemes.Add(randomMatchData.BallTheme);
                }

                if (seenThemes.Count < 2)
                {
                    errors.Add("Random ball selection did not reroll across repeated match starts.");
                }

                randomMatchData.StartQuickMatch(0, mlpAiDifficulty.Normal, mlpBallSelection.ClassicOriginal);
                if (randomMatchData.BallTheme != mlpBallTheme.ClassicOriginal)
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
                inventory.Tournament = originalTournament ?? new mlpTournamentData();
            }
        }

        /// <summary>
        /// Check that difficulty toggles cycle correctly and map to the expected four-tier AI skill index.

        /// </summary>
        private static void ValidateDifficultyCycleAndSkillMapping(List<string> errors)
        {
            var inventory = mlpInventory.Instance;
            var originalDifficulty = inventory.Difficulty;

            try
            {
                // 1. Set to easy difficulty and verify whether the labels are displayed correctly

                inventory.Difficulty = mlpAiDifficulty.Easy;
                if (inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty label did not render AI: EASY.");
                }

                // 2. Switch the difficulty in turn and verify whether you press the Easy -> Normal -> Hard -> Hell -> Easy cycle

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != mlpAiDifficulty.Normal || inventory.DifficultyLabel != "AI: NORMAL")
                {
                    errors.Add("Difficulty toggle did not advance from Easy to Normal.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != mlpAiDifficulty.Hard || inventory.DifficultyLabel != "AI: HARD")
                {
                    errors.Add("Difficulty toggle did not advance from Normal to Hard.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != mlpAiDifficulty.Hell || inventory.DifficultyLabel != "AI: HELL")
                {
                    errors.Add("Difficulty toggle did not advance from Hard to Hell.");
                }

                inventory.ToggleDifficulty();
                if (inventory.Difficulty != mlpAiDifficulty.Easy || inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty toggle did not cycle from Hell back to Easy.");
                }

                // 3. Verify that both quick matches and random matches use fixed four-level difficulty indexes:

                //    Easy=0、Normal=1、Hard=2、Hell=3。
                var matchData = new mlpMatchData(true);
                var expectedSkills = new[]
                {
                    new DifficultySkillExpectation(mlpAiDifficulty.Easy, mlpAISkillsData.EasySkillIndex),
                    new DifficultySkillExpectation(mlpAiDifficulty.Normal, mlpAISkillsData.NormalSkillIndex),
                    new DifficultySkillExpectation(mlpAiDifficulty.Hard, mlpAISkillsData.HardSkillIndex),
                    new DifficultySkillExpectation(mlpAiDifficulty.Hell, mlpAISkillsData.HellSkillIndex)
                };

                if (mlpAISkillsData.MaxSkillIndex != mlpAISkillsData.HellSkillIndex)
                {
                    errors.Add($"AI skill table should expose exactly four fixed indexes, got max index {mlpAISkillsData.MaxSkillIndex}.");
                }

                for (var i = 0; i < expectedSkills.Length; i++)
                {
                    var expectation = expectedSkills[i];
                    matchData.StartQuickMatch(0, expectation.Difficulty);
                    AssertOpponentSkill(matchData, expectation.ExpectedSkillIndex, errors, $"Quick Match {expectation.Difficulty} fixed mapping");

                    matchData.StartRandomMatch(0, expectation.Difficulty);
                    AssertOpponentSkill(matchData, expectation.ExpectedSkillIndex, errors, $"Random Match {expectation.Difficulty} fixed mapping");
                }
            }
            finally
            {
                // 4. Restore original difficulty settings
                inventory.Difficulty = originalDifficulty;
            }
        }

        /// <summary>
        /// Run the full tournament on the specified difficulty to verify brackets, rankings and rankings.

        /// </summary>
        private static void ValidateTournamentSeasonMode(List<string> errors, mlpAiDifficulty difficulty, string difficultyLabel)
        {
            var tournament = new mlpTournamentData();
            if (!tournament.Create(0, difficulty))
            {
                errors.Add($"Tournament season mode could not be created for {difficultyLabel} difficulty with 8 enabled characters.");
                return;
            }

            if (tournament.CurrentStage != mlpTournamentStage.RegularSeason || !tournament.HasPendingPlayerMatch)
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

            if (!tournament.RegularSeasonCompleted || !tournament.PlayerQualifiedForPlayoffs || tournament.CurrentStage != mlpTournamentStage.RegularSeason)
            {
                errors.Add($"Tournament regular season did not finish in standings-preview state after three player wins for {difficultyLabel} difficulty.");
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != mlpTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament did not transition into a pending semifinal after finals start for {difficultyLabel} difficulty.");
            }

            tournament.ApplyCurrentMatchResult(32, 18);
            if (tournament.CurrentStage != mlpTournamentStage.Final || !tournament.ThirdPlaceResult.Completed || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"Tournament semifinal resolution did not open the final and auto-resolve third place for {difficultyLabel} difficulty.");
            }

            tournament.ApplyCurrentMatchResult(29, 17);
            if (!tournament.Completed || tournament.CurrentStage != mlpTournamentStage.Complete)
            {
                errors.Add($"Tournament did not complete after the final for {difficultyLabel} difficulty.");
            }

            if (tournament.ChampionCharacterId != tournament.PlayerCharacterId || tournament.PlayerPlacement != 1)
            {
                errors.Add($"Tournament final placement logic did not award the player the championship for {difficultyLabel} difficulty.");
            }
        }

        /// <summary>
        /// Check that tournaments always use a fixed AI skill index on four difficulty levels.

        /// </summary>
        private static void ValidateFixedTournamentSkillMapping(List<string> errors)
        {
            var expectations = new[]
            {
                new DifficultySkillExpectation(mlpAiDifficulty.Easy, mlpAISkillsData.EasySkillIndex),
                new DifficultySkillExpectation(mlpAiDifficulty.Normal, mlpAISkillsData.NormalSkillIndex),
                new DifficultySkillExpectation(mlpAiDifficulty.Hard, mlpAISkillsData.HardSkillIndex),
                new DifficultySkillExpectation(mlpAiDifficulty.Hell, mlpAISkillsData.HellSkillIndex)
            };

            for (var i = 0; i < expectations.Length; i++)
            {
                ValidateFixedTournamentSkillMappingForDifficulty(errors, expectations[i]);
            }
        }

        /// <summary>
        /// Checks whether a specific difficulty maintains the same skill index across the regular season, semi-finals, finals, and third-place matches of a tournament.
        /// </summary>
        private static void ValidateFixedTournamentSkillMappingForDifficulty(
            List<string> errors,
            DifficultySkillExpectation expectation)
        {
            var tournament = new mlpTournamentData();
            if (!tournament.Create(0, expectation.Difficulty))
            {
                errors.Add($"{expectation.Difficulty} tournament fixed mapping test could not create a tournament.");
                return;
            }

            var matchData = new mlpMatchData(true);
            for (var round = 0; round < 3; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(
                    matchData,
                    expectation.ExpectedSkillIndex,
                    errors,
                    $"{expectation.Difficulty} tournament fixed round {round + 1}");
                tournament.ApplyCurrentMatchResult(30 + round, 10 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != mlpTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add($"{expectation.Difficulty} tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, expectation.ExpectedSkillIndex, errors, $"{expectation.Difficulty} tournament fixed semifinal");

            tournament.ApplyCurrentMatchResult(32, 18);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, expectation.ExpectedSkillIndex, errors, $"{expectation.Difficulty} tournament fixed final");

            var thirdPlaceTournament = new mlpTournamentData();
            if (!thirdPlaceTournament.Create(0, expectation.Difficulty))
            {
                errors.Add($"{expectation.Difficulty} third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new mlpMatchData(true);
            for (var round = 0; round < 3; round++)
            {
                thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
                AssertOpponentSkill(
                    thirdPlaceMatchData,
                    expectation.ExpectedSkillIndex,
                    errors,
                    $"{expectation.Difficulty} tournament fixed third-place path round {round + 1}");
                thirdPlaceTournament.ApplyCurrentMatchResult(28 + round, 12 + round);
            }

            thirdPlaceTournament.BeginFinals();
            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(
                thirdPlaceMatchData,
                expectation.ExpectedSkillIndex,
                errors,
                $"{expectation.Difficulty} tournament fixed third-place path semifinal");

            thirdPlaceTournament.ApplyCurrentMatchResult(18, 24);
            if (thirdPlaceTournament.CurrentStage != mlpTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add($"{expectation.Difficulty} tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(
                thirdPlaceMatchData,
                expectation.ExpectedSkillIndex,
                errors,
                $"{expectation.Difficulty} tournament fixed third-place match");
        }

        /// <summary>
        /// Asserts that the opponent's Tier 4 AI skill index matches the expected value.
        /// </summary>
        private static void AssertOpponentSkill(mlpMatchData matchData, int expectedSkillIndex, List<string> errors, string context)
        {
            if (matchData.Skills == null || matchData.Skills.Length < 2 || matchData.Skills[1] == null || matchData.Skills[1].Length == 0)
            {
                errors.Add($"{context} did not produce an opponent skill entry.");
                return;
            }

            var actualSkill = matchData.Skills[1][0];
            if (actualSkill != expectedSkillIndex)
            {
                errors.Add($"{context} expected opponent skill index {expectedSkillIndex}, got {actualSkill}.");
            }

            if (actualSkill < 0 || actualSkill > mlpAISkillsData.MaxSkillIndex)
            {
                errors.Add($"{context} produced out-of-range opponent skill {actualSkill}.");
            }
        }
    }
}
