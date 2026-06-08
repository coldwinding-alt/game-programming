// 自动化冒烟测试 / 在编辑器中运行一系列基本功能测试，检查游戏的核心系统是否正常工作：角色数据是否完整、技能配置是否正确、图集能否加载、音频能否播放、UI 能否创建等。用来在发布前快速发现明显的问题。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace mlp.EditorTools
{
    public static class mlpSmokeTest
    {
        /// <summary>
        /// 运行所有冒烟测试：验证资源、快速构建游戏并报告错误。
        /// </summary>
        public static void Run()
        {
            var errors = new List<string>();
            GameObject root = null;
            mlpGameCore core = null;

            try
            {
                CheckResource<Texture2D>("mlp/Atlases/gameplay", errors);
                CheckResource<TextAsset>("mlp/Atlases/gameplay", errors);
                CheckResource<Texture2D>("mlp/Atlases/interface", errors);
                CheckResource<TextAsset>("mlp/Atlases/interface", errors);
                CheckResource<Texture2D>("mlp/Atlases/skillfx", errors);
                CheckResource<TextAsset>("mlp/Atlases/skillfx", errors);
                CheckResource<Font>("mlp/Fonts/Impact", errors);
                CheckResource<Font>("mlp/Fonts/Impact2", errors);
                CheckResource<Font>("mlp/Fonts/CfCrackBold", errors);
                CheckResource<Font>("mlp/Fonts/Rajdhani-SemiBold", errors);
                CheckResource<Font>("mlp/Fonts/Rajdhani-Bold", errors);
                CheckResource<Font>("mlp/Fonts/Griffy-Regular", errors);
                EnsureTextMeshProEssentialResources(errors);
                ValidateNativeMenuTextLayer(errors);
                ValidateSinglePlayerNarrativeDefinitions(errors);
                ValidateAdventureModeDefinitionsAndFlow(errors);
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
                ValidateSkillIconAssets(errors);
                CheckResource<Texture2D>(mlpAssets.Hud.ResourcePath(mlpAssets.Hud.Scoreboard), errors);
                CheckResource<Texture2D>(mlpAssets.Hud.ResourcePath(mlpAssets.Hud.Popup), errors);
                CheckResource<TextAsset>(mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas), errors);
                CheckResource<Texture2D>(mlpAssets.Portraits.ResourcePath(mlpAssets.Portraits.UiAtlas), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallGhoulGreen), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallPumpkinEmber), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallMoonlitViolet), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallJackOLantern), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallEvilEye), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCursed8Ball), errors);
                CheckResource<Texture2D>(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCandySwirl), errors);
                CheckResource<TextAsset>("mlp/DragonBones/sk2", errors);
                CheckResource<TextAsset>("mlp/DragonBones/texture2", errors);
                CheckResource<Texture2D>("mlp/DragonBones/texture2", errors);
                CheckAudioResources(errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallGhoulGreen), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_ghoul_green.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallPumpkinEmber), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_pumpkin_ember.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallMoonlitViolet), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_moonlit_violet.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallJackOLantern), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_jack_o_lantern.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallEvilEye), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_evil_eye.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCursed8Ball), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_cursed_8ball.png", errors);
                ValidateBallSpriteAsset(mlpAssets.Images.ResourcePath(mlpAssets.Images.GameplayImages.BallCandySwirl), "Assets/mlp/Resources/mlp/Images/Gameplay/ball_halloween_candy_swirl.png", errors);

                if (Shader.Find("mlp/TextMeshOutlined") == null)
                {
                    errors.Add("Could not find mlp/TextMeshOutlined shader.");
                }

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

                DBLiteFactory.Instance.EnsureLoaded();
                var armature = mlpPlayersData.BuildGameplayArmature("SmokePlayerSmall");
                if (armature == null)
                {
                    errors.Add("Could not build DragonBones playerSmall armature.");
                }
                else
                {
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

                var dragonBones = Resources.Load<TextAsset>("mlp/DragonBones/sk2");
                if (dragonBones == null || !dragonBones.text.Contains("\"mega\""))
                {
                    errors.Add("DragonBones data did not expose the expected mega frame event.");
                }

                ValidateDifficultyCycleAndSkillMapping(errors);
                ValidateTournamentSeasonMode(errors, mlpAiDifficulty.Normal, "normal");
                ValidateTournamentSeasonMode(errors, mlpAiDifficulty.Hard, "hard");
                ValidateTournamentSeasonMode(errors, mlpAiDifficulty.Hell, "hell");
                ValidateHardTournamentSkillMapping(errors);
                ValidateHellTournamentSkillMapping(errors);
                ValidateBallSelectionStateAndResolution(errors);
                ValidateRuntimeGraphicsResourceReuse(errors);

                root = new GameObject("SmokeRuntimeRoot");
                mlpAudio.Create(root.transform);
                mlpInventory.Instance.SetQuickSelection(0);
                mlpInventory.Instance.SetQuickBallSelection(mlpBallSelection.ClassicOriginal);
                mlpInventory.Instance.StartQuickGame();
                core = new mlpGameBuilder().Build(root.transform);
                core.Update(0.016f);
                ValidateBlockedShotScorePersistence(core, errors);
                ValidateHelpPanelTutorialEntry(errors);
                ValidateTutorialModeBoot(errors);
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

            Debug.Log("mlp smoke test passed.");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 检查指定路径是否存在资源，若缺失则记录错误。
        /// </summary>
        private static void CheckResource<T>(string path, List<string> errors) where T : UnityEngine.Object
        {
            if (Resources.Load<T>(path) == null)
            {
                errors.Add($"Missing resource: {path}");
            }
        }

        /// <summary>
        /// 验证原生 TMP 菜单文本层能否创建标题和按钮标签。
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
        /// 检查所有单人模式叙述文本条目和模式定义是否存在。
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

                if (level.OpponentSkill < 0 || level.OpponentSkill > mlpAISkillsData.MaxSkillIndex)
                {
                    errors.Add($"Adventure level {i + 1} has out-of-range opponent skill {level.OpponentSkill}.");
                }
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

            AssertOpponentSkill(matchData, firstLevel.OpponentSkill, errors, "Adventure normal mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Easy);
            AssertOpponentSkill(matchData, Mathf.Max(0, firstLevel.OpponentSkill - 1), errors, "Adventure easy mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Hard);
            AssertOpponentSkill(matchData, Mathf.Min(mlpAISkillsData.MaxSkillIndex, firstLevel.OpponentSkill + 2), errors, "Adventure hard mapping");
            matchData.StartAdventureMatch(adventure, mlpAiDifficulty.Hell);
            AssertOpponentSkill(matchData, Mathf.Min(mlpAISkillsData.MaxSkillIndex, firstLevel.OpponentSkill + 4), errors, "Adventure hell mapping");

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

            ValidateNarrativeTerm(mlpSinglePlayerNarrative.GetTournamentStageTitle(new mlpTournamentData()), "tournament stage title", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.GetTournamentStageDescription(new mlpTournamentData()), "tournament stage description", errors);
            ValidateNarrativeTerm(mlpSinglePlayerNarrative.GetTournamentPlacementEnding(1), "tournament champion ending", errors);
        }

        /// <summary>
        /// 确保项目中已导入 TMP 基础资源（设置和着色器）。
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
        /// 验证 Resources 文件夹中是否存在所有预期的音频片段。
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
        /// 检查运行时材质和网格是否被共享并正确释放。
        /// </summary>
        private static void ValidateRuntimeGraphicsResourceReuse(List<string> errors)
        {
            ValidateSharedRuntimeMaterials(errors);
            ValidateGameRuntimeMeshRelease(errors);
        }

        /// <summary>
        /// 检查帮助面板预制体是否有重玩教程按钮。
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
        /// 验证教程模式能否创建流程、覆盖层，并正确清理聚焦标记。
        /// </summary>
        private static void ValidateTutorialModeBoot(List<string> errors)
        {
            var existingOverlay = UnityEngine.Object.FindObjectOfType<mlpTutorialOverlay>();
            if (existingOverlay != null)
            {
                UnityEngine.Object.DestroyImmediate(existingOverlay.gameObject);
            }

            var runtimeRoot = new GameObject("TutorialSmokeRoot");
            mlpGameCore tutorialCore = null;
            mlpTutorialOverlay tutorialOverlay = null;

            try
            {
                mlpAudio.Create(runtimeRoot.transform);
                mlpInventory.Instance.SetTrainingSelection(0);
                mlpInventory.Instance.SetTrainingBallSelection(mlpBallSelection.ClassicOriginal);
                mlpInventory.Instance.StartTutorial();
                tutorialCore = new mlpGameBuilder().Build(runtimeRoot.transform);
                tutorialCore.Update(0.016f);

                if (tutorialCore.TutorialFlow == null)
                {
                    errors.Add("Tutorial mode did not create a tutorial flow.");
                }

                tutorialOverlay = UnityEngine.Object.FindObjectOfType<mlpTutorialOverlay>();
                if (tutorialOverlay == null)
                {
                    errors.Add("Tutorial mode did not create the tutorial overlay.");
                    return;
                }

                var overlayRootField = typeof(mlpTutorialOverlay).GetField("overlayRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (overlayRootField == null || overlayRootField.GetValue(tutorialOverlay) == null)
                {
                    errors.Add("Tutorial overlay did not build its runtime UI root.");
                }

                ValidateTutorialStepCleanup(tutorialCore, tutorialOverlay, errors);
            }
            finally
            {
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
        /// 检查篮筐网和径向图标网格是否共享材质而非重复创建。
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
        /// 验证游戏关闭时是否释放径向图标的动态网格。
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
        /// 验证被盖帽的投篮在偏转后仍能得分。
        /// </summary>
        private static void ValidateBlockedShotScorePersistence(mlpGameCore core, List<string> errors)
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

            var canScoreField = typeof(mlpBallObject).GetField("canScore", BindingFlags.Instance | BindingFlags.NonPublic);
            var scoreArmedSideField = typeof(mlpBallObject).GetField("scoreArmedSide", BindingFlags.Instance | BindingFlags.NonPublic);
            if (canScoreField == null || scoreArmedSideField == null)
            {
                errors.Add("Blocked-shot score regression test could not inspect mlpBallObject scoring state.");
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

        /// <summary>
        /// 检查球体纹理是否为 36x36 且角落透明、无白色光晕。
        /// </summary>
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

        /// <summary>
        /// 检查每个角色的技能图标和充能遮罩纹理是否存在且符合质量要求。
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
        /// 验证单个技能图标纹理：是否存在、尺寸是否足够、导入设置是否正确。
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
        /// 检查 UI 纹理是否禁用了 mipmap、启用了 alpha 透明且未压缩。
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
        /// 检查平台特定的纹理设置是否覆盖了默认值并保持未压缩。
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
        /// 检查球体纹理的角落像素是否透明，以确保旋转时干净无残影。
        /// </summary>
        private static void ValidateCornerAlpha(Texture2D texture, string resourcePath, int x, int y, List<string> errors)
        {
            if (texture.GetPixel(x, y).a > 0.03f)
            {
                errors.Add($"{resourcePath} corner ({x},{y}) should stay transparent for rotation.");
            }
        }

        /// <summary>
        /// 检查球体是否填满整个 36x36 画布，没有空白边距。
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
        /// 检查半透明像素是否接近白色，这会导致可见的光晕。
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
        /// 验证球体选择在快速、训练、对战和锦标赛模式间是否正确保持。
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
        /// 检查难度切换是否正确循环并映射到预期的 AI 技能等级。
        /// </summary>
        private static void ValidateDifficultyCycleAndSkillMapping(List<string> errors)
        {
            var inventory = mlpInventory.Instance;
            var originalDifficulty = inventory.Difficulty;

            try
            {
                inventory.Difficulty = mlpAiDifficulty.Easy;
                if (inventory.DifficultyLabel != "AI: EASY")
                {
                    errors.Add("Difficulty label did not render AI: EASY.");
                }

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

                var matchData = new mlpMatchData(true);
                matchData.StartQuickMatch(0, mlpAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Quick Match hard mapping");

                matchData.StartRandomMatch(0, mlpAiDifficulty.Hard);
                AssertOpponentSkill(matchData, 5, errors, "Random Match hard mapping");

                matchData.StartQuickMatch(0, mlpAiDifficulty.Hell);
                AssertOpponentSkill(matchData, 10, errors, "Quick Match hell mapping");

                matchData.StartRandomMatch(0, mlpAiDifficulty.Hell);
                AssertOpponentSkill(matchData, 10, errors, "Random Match hell mapping");
            }
            finally
            {
                inventory.Difficulty = originalDifficulty;
            }
        }

        /// <summary>
        /// 在指定难度下运行完整锦标赛，验证对阵表、排名和名次。
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
        /// 检查困难难度的锦标赛轮次是否映射到预期的递增 AI 技能。
        /// </summary>
        private static void ValidateHardTournamentSkillMapping(List<string> errors)
        {
            var tournament = new mlpTournamentData();
            if (!tournament.Create(0, mlpAiDifficulty.Hard))
            {
                errors.Add("Hard tournament mapping test could not create a tournament.");
                return;
            }

            var matchData = new mlpMatchData(true);
            var expectedRoundSkills = new[] { 5, 6, 7 };
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(matchData, expectedRoundSkills[round], errors, $"Hard tournament round {round + 1}");
                tournament.ApplyCurrentMatchResult(30 + round, 10 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != mlpTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add("Hard tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 7, errors, "Hard tournament semifinal");

            tournament.ApplyCurrentMatchResult(32, 18);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 8, errors, "Hard tournament final");

            var thirdPlaceTournament = new mlpTournamentData();
            if (!thirdPlaceTournament.Create(0, mlpAiDifficulty.Hard))
            {
                errors.Add("Hard third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new mlpMatchData(true);
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
            if (thirdPlaceTournament.CurrentStage != mlpTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add("Hard tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 7, errors, "Hard tournament third-place match");
        }

        /// <summary>
        /// 检查地狱难度的锦标赛轮次是否映射到预期的最高 AI 技能。
        /// </summary>
        private static void ValidateHellTournamentSkillMapping(List<string> errors)
        {
            var tournament = new mlpTournamentData();
            if (!tournament.Create(0, mlpAiDifficulty.Hell))
            {
                errors.Add("Hell tournament mapping test could not create a tournament.");
                return;
            }

            var matchData = new mlpMatchData(true);
            var expectedRoundSkills = new[] { 8, 9, 10 };
            for (var round = 0; round < expectedRoundSkills.Length; round++)
            {
                matchData.StartTournamentMatch(tournament);
                AssertOpponentSkill(matchData, expectedRoundSkills[round], errors, $"Hell tournament round {round + 1}");
                tournament.ApplyCurrentMatchResult(34 + round, 18 + round);
            }

            tournament.BeginFinals();
            if (tournament.CurrentStage != mlpTournamentStage.SemiFinal || !tournament.HasPendingPlayerMatch)
            {
                errors.Add("Hell tournament did not open a pending semifinal after finals start.");
                return;
            }

            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 10, errors, "Hell tournament semifinal");

            tournament.ApplyCurrentMatchResult(36, 22);
            matchData.StartTournamentMatch(tournament);
            AssertOpponentSkill(matchData, 11, errors, "Hell tournament final");

            var thirdPlaceTournament = new mlpTournamentData();
            if (!thirdPlaceTournament.Create(0, mlpAiDifficulty.Hell))
            {
                errors.Add("Hell third-place mapping test could not create a tournament.");
                return;
            }

            var thirdPlaceMatchData = new mlpMatchData(true);
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
            if (thirdPlaceTournament.CurrentStage != mlpTournamentStage.ThirdPlace || !thirdPlaceTournament.HasPendingPlayerMatch)
            {
                errors.Add("Hell tournament did not route a semifinal loss into a pending third-place match.");
                return;
            }

            thirdPlaceMatchData.StartTournamentMatch(thirdPlaceTournament);
            AssertOpponentSkill(thirdPlaceMatchData, 10, errors, "Hell tournament third-place match");
        }

        /// <summary>
        /// 断言对手的 AI 技能等级与预期值匹配。
        /// </summary>
        private static void AssertOpponentSkill(mlpMatchData matchData, int expectedSkill, List<string> errors, string context)
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

            if (actualSkill < 0 || actualSkill > mlpAISkillsData.MaxSkillIndex)
            {
                errors.Add($"{context} produced out-of-range opponent skill {actualSkill}.");
            }
        }
    }
}
