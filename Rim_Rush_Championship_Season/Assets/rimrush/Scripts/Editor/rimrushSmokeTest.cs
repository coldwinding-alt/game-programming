// 自动化冒烟测试 / 在编辑器中运行一系列基本功能测试，检查游戏的核心系统是否正常工作：角色数据是否完整、技能配置是否正确、图集能否加载、音频能否播放、UI 能否创建等。用来在发布前快速发现明显的问题。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushSmokeTest
    {
        /// <summary>
        /// Run all smoke tests: validate resources, build a quick game, and report any errors.
        /// </summary>
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
                EnsureTextMeshProEssentialResources(errors);
                ValidateNativeMenuTextLayer(errors);
                ValidateSinglePlayerNarrativeDefinitions(errors);
                ValidateAdventureModeDefinitionsAndFlow(errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.PauseButton), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOn), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.MusicButtonOff), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.HelpButton), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.FramePanelLarge), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.FrameMatchCardIdle), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.FrameMatchCardActive), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.MenuButtonPlate), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.EnergyButtonPlate), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.EmblemOrb), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.AdventureTreasureMapBg), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.AwardsShowcasePanel), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.AwardsResultPlaque), errors);
                CheckResource<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.Ui.AwardsPodiumBase), errors);
                ValidateSkillIconAssets(errors);
                CheckResource<Texture2D>(rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Scoreboard), errors);
                CheckResource<Texture2D>(rimrushAssets.Hud.ResourcePath(rimrushAssets.Hud.Popup), errors);
                CheckResource<TextAsset>(rimrushAssets.Portraits.ResourcePath(rimrushAssets.Portraits.UiAtlas), errors);
                CheckResource<Texture2D>(rimrushAssets.Portraits.ResourcePath(rimrushAssets.Portraits.UiAtlas), errors);
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
                if (!ui.HasFrame("EmblemsBg0000") || !ui.HasFrame("loginSelect0000"))
                {
                    errors.Add("Interface atlas did not expose the expected active frame keys.");
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
                        if (rimrushPlayersData.GetCharacterPortraitSprite(characterId) == null)
                        {
                            errors.Add($"Missing UI portrait sprite for active character {characterId}.");
                        }

                        if (rimrushPlayersData.GetCharacterPortraitSprite(characterId, 28f) == null)
                        {
                            errors.Add($"Missing small UI portrait sprite variant for active character {characterId}.");
                        }
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

            Debug.Log("rimrush smoke test passed.");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Check that a resource exists at the given path and log an error if it's missing.
        /// </summary>
        private static void CheckResource<T>(string path, List<string> errors) where T : UnityEngine.Object
        {
            if (Resources.Load<T>(path) == null)
            {
                errors.Add($"Missing resource: {path}");
            }
        }

        /// <summary>
        /// Verify that the native TMP menu text layer can create headings and button labels.
        /// </summary>
        private static void ValidateNativeMenuTextLayer(List<string> errors)
        {
            var root = new GameObject("NativeMenuTextSmokeRoot");
            rimrushNativeMenuTextLayer layer = null;

            try
            {
                layer = new rimrushNativeMenuTextLayer(root.transform);
                layer.RefreshLayout(rimrushConstants.Width, 480);

                var heading = layer.CreateText(
                    "SmokeHeading",
                    "TOURNAMENT",
                    rimrushConstants.Width2,
                    80f,
                    20,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    rimrushTextStyle.TournamentAccent);
                if (heading == null || heading.font == null)
                {
                    errors.Add("Native menu TMP text layer could not create a heading with a font asset.");
                }

                var button = new rimrushMenuButton("PLAY", rimrushConstants.Width2, 440f, 150f, 42f, null, root.transform);
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
        /// Check that all single-player narrative text entries and mode definitions are present.
        /// </summary>
        private static void ValidateSinglePlayerNarrativeDefinitions(List<string> errors)
        {
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.ParkName, "park name", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.PumpkinHeartLantern, "core lantern", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.LanternSigil, "sigil", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.Warden, "warden", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.MidnightLockdownProtocol, "lockdown protocol", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.LanternChampion, "champion title", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.AdventurePreviewStatus, "adventure entry status", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.TournamentPreviewStatus, "tournament entry status", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.ComicReplayButton, "comic replay button", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.AdventureLinkToCup, "adventure to cup link", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.CupLinkToAdventure, "cup to adventure link", errors);

            ValidateNarrativeMode(rimrushSinglePlayerNarrative.Adventure, rimrushSinglePlayerNarrativeMode.Adventure, 3, errors);
            ValidateNarrativeMode(rimrushSinglePlayerNarrative.Tournament, rimrushSinglePlayerNarrativeMode.Tournament, 3, errors);

            if (!ReferenceEquals(
                    rimrushSinglePlayerNarrative.GetMode(rimrushSinglePlayerNarrativeMode.Adventure),
                    rimrushSinglePlayerNarrative.Adventure))
            {
                errors.Add("Single-player narrative lookup did not resolve Adventure mode.");
            }

            if (!ReferenceEquals(
                    rimrushSinglePlayerNarrative.GetMode(rimrushSinglePlayerNarrativeMode.Tournament),
                    rimrushSinglePlayerNarrative.Tournament))
            {
                errors.Add("Single-player narrative lookup did not resolve Tournament mode.");
            }
        }

        private static void ValidateNarrativeMode(
            rimrushSinglePlayerModeDefinition mode,
            rimrushSinglePlayerNarrativeMode expectedMode,
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
                    var resourcePath = rimrushAssets.Images.ResourcePath(panel.ImageKey);
                    var texture = Resources.Load<Texture2D>(resourcePath);
                    if (texture == null)
                    {
                        errors.Add($"Missing opening comic image resource: {resourcePath}");
                    }
                    else if (texture.width < 1280 || texture.height < 720)
                    {
                        errors.Add($"{expectedMode} opening comic panel {i + 1} should be high-resolution, got {texture.width}x{texture.height}.");
                    }

                    var assetPath = $"Assets/rimrush/Resources/rimrush/Images/{panel.ImageKey}.png";
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
            var levels = rimrushAdventureCatalog.AllLevels;
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

                if (level.OpponentSkill < 0 || level.OpponentSkill > rimrushAISkillsData.MaxSkillIndex)
                {
                    errors.Add($"Adventure level {i + 1} has out-of-range opponent skill {level.OpponentSkill}.");
                }
            }

            var adventure = new rimrushAdventureData();
            adventure.Create(0);
            if (!adventure.Active || adventure.Completed || !adventure.IsLevelUnlocked(0) || adventure.IsLevelUnlocked(1))
            {
                errors.Add("Adventure flow did not start with only the first level unlocked.");
            }

            if (!adventure.SelectLevel(0))
            {
                errors.Add("Adventure flow could not select the first unlocked level.");
            }

            var firstLevel = rimrushAdventureCatalog.GetLevel(0);
            var matchData = new rimrushMatchData(true);
            matchData.StartAdventureMatch(adventure);
            if (matchData.CharacterIds[1] != firstLevel.WardenCharacterId)
            {
                errors.Add("Adventure match did not use the first level Warden as opponent.");
            }

            AssertOpponentSkill(matchData, firstLevel.OpponentSkill, errors, "Adventure normal mapping");
            matchData.StartAdventureMatch(adventure, rimrushAiDifficulty.Easy);
            AssertOpponentSkill(matchData, Mathf.Max(0, firstLevel.OpponentSkill - 1), errors, "Adventure easy mapping");
            matchData.StartAdventureMatch(adventure, rimrushAiDifficulty.Hard);
            AssertOpponentSkill(matchData, Mathf.Min(rimrushAISkillsData.MaxSkillIndex, firstLevel.OpponentSkill + 2), errors, "Adventure hard mapping");
            matchData.StartAdventureMatch(adventure, rimrushAiDifficulty.Hell);
            AssertOpponentSkill(matchData, Mathf.Min(rimrushAISkillsData.MaxSkillIndex, firstLevel.OpponentSkill + 4), errors, "Adventure hell mapping");

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

            for (var i = 1; i < rimrushAdventureCatalog.LevelCount; i++)
            {
                adventure.SelectLevel(i);
                adventure.ApplyCurrentMatchResult(true);
            }

            if (!adventure.Completed || adventure.SigilsCollected != rimrushAdventureCatalog.LevelCount)
            {
                errors.Add("Adventure flow did not complete after all levels were won.");
            }

            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.GetTournamentStageTitle(new rimrushTournamentData()), "tournament stage title", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.GetTournamentStageDescription(new rimrushTournamentData()), "tournament stage description", errors);
            ValidateNarrativeTerm(rimrushSinglePlayerNarrative.GetTournamentPlacementEnding(1), "tournament champion ending", errors);
        }

        /// <summary>
        /// Make sure TMP essential resources (settings and shader) are imported in the project.
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
        /// Verify that all expected audio clips exist in the Resources folder.
        /// </summary>
        private static void CheckAudioResources(List<string> errors)
        {
            var audioPaths = new[]
            {
                "rimrush/Sound/whistle",
                "rimrush/Sound/teleport",
                "rimrush/Sound/swoosh",
                "rimrush/Sound/energy",
                "rimrush/Sound/stunned",
                "rimrush/Sound/clash",
                "rimrush/Sound/buzzer",
                "rimrush/Sound/rim_hit",
                "rimrush/Sound/mega_dunk",
                "rimrush/Sound/shield",
                "rimrush/Sound/ball_bounce",
                "rimrush/Sound/dash",
                "rimrush/Sound/super_dash",
                "rimrush/Sound/countdown",
                "rimrush/Sound/button",
                "rimrush/Sound/net",
                "rimrush/Sound/brick",
                "rimrush/Sound/basket",
                "rimrush/Sound/bgm",
            };

            for (var i = 0; i < audioPaths.Length; i++)
            {
                CheckResource<AudioClip>(audioPaths[i], errors);
            }
        }

        /// <summary>
        /// Check that runtime materials and meshes are shared and properly released.
        /// </summary>
        private static void ValidateRuntimeGraphicsResourceReuse(List<string> errors)
        {
            ValidateSharedRuntimeMaterials(errors);
            ValidateGameRuntimeMeshRelease(errors);
        }

        /// <summary>
        /// Check that the help panel prefab has a Replay Tutorial button.
        /// </summary>
        private static void ValidateHelpPanelTutorialEntry(List<string> errors)
        {
            var prefab = Resources.Load<rimrushHelpPanel>("rimrush/Prefabs/UI/RimrushHelpPanel");
            if (prefab == null)
            {
                errors.Add("Missing help panel prefab at Resources/rimrush/Prefabs/UI/RimrushHelpPanel.");
                return;
            }

            rimrushHelpPanel instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                var buttons = instance.GetComponentsInChildren<rimrushHelpButton>(true);
                var replayButtons = 0;
                for (var i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].Action == rimrushHelpButtonAction.ReplayTutorial)
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
        /// Verify that tutorial mode creates a flow, overlay, and cleans up focus markers properly.
        /// </summary>
        private static void ValidateTutorialModeBoot(List<string> errors)
        {
            var existingOverlay = UnityEngine.Object.FindObjectOfType<rimrushTutorialOverlay>();
            if (existingOverlay != null)
            {
                UnityEngine.Object.DestroyImmediate(existingOverlay.gameObject);
            }

            var runtimeRoot = new GameObject("TutorialSmokeRoot");
            rimrushGameCore tutorialCore = null;
            rimrushTutorialOverlay tutorialOverlay = null;

            try
            {
                rimrushAudio.Create(runtimeRoot.transform);
                rimrushInventory.Instance.SetTrainingSelection(0);
                rimrushInventory.Instance.SetTrainingBallSelection(rimrushBallSelection.ClassicOriginal);
                rimrushInventory.Instance.StartTutorial();
                tutorialCore = new rimrushGameBuilder().Build(runtimeRoot.transform);
                tutorialCore.Update(0.016f);

                if (tutorialCore.TutorialFlow == null)
                {
                    errors.Add("Tutorial mode did not create a tutorial flow.");
                }

                tutorialOverlay = UnityEngine.Object.FindObjectOfType<rimrushTutorialOverlay>();
                if (tutorialOverlay == null)
                {
                    errors.Add("Tutorial mode did not create the tutorial overlay.");
                    return;
                }

                var overlayRootField = typeof(rimrushTutorialOverlay).GetField("overlayRoot", BindingFlags.Instance | BindingFlags.NonPublic);
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

        private static void ValidateTutorialStepCleanup(rimrushGameCore tutorialCore, rimrushTutorialOverlay tutorialOverlay, List<string> errors)
        {
            if (tutorialCore?.TutorialFlow == null || tutorialOverlay == null)
            {
                return;
            }

            var beginSuperMethod = typeof(rimrushTutorialFlow).GetMethod("BeginSuper", BindingFlags.Instance | BindingFlags.NonPublic);
            var completeStepMethod = typeof(rimrushTutorialFlow).GetMethod("CompleteStep", BindingFlags.Instance | BindingFlags.NonPublic);
            var maskTopField = typeof(rimrushTutorialOverlay).GetField("maskTop", BindingFlags.Instance | BindingFlags.NonPublic);
            var focusFrameField = typeof(rimrushTutorialOverlay).GetField("focusFrame", BindingFlags.Instance | BindingFlags.NonPublic);

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
        /// Check that basket nets and radial icon meshes share materials instead of duplicating them.
        /// </summary>
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

                var maskTexture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(rimrushAssets.Images.SkillIcons.ScarecrowMask));
                if (maskTexture == null)
                {
                    errors.Add("Runtime graphics reuse test could not load the standalone skill mask texture.");
                }
                else
                {
                    new rimrushRadialIconMesh("EnergyFillSmokeA", maskTexture, 40f, 40f, 10, runtimeRoot.transform, 40f);
                    new rimrushRadialIconMesh("EnergyFillSmokeB", maskTexture, 72f, 40f, 10, runtimeRoot.transform, 40f);
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
        /// Verify that game shutdown releases the radial icon dynamic mesh.
        /// </summary>
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

        /// <summary>
        /// Verify that a blocked shot can still score after being deflected.
        /// </summary>
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

        /// <summary>
        /// Check that a ball texture is 36x36 with transparent corners and no white halo.
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
        /// Check that every character's skill icon and charge mask textures exist and meet quality requirements.
        /// </summary>
        private static void ValidateSkillIconAssets(List<string> errors)
        {
            var validatedImageKeys = new HashSet<string>();
            for (var characterId = 0; characterId < rimrushPlayersData.CharacterCount; characterId++)
            {
                var skillDefinition = rimrushCharacterSkillsData.Get(characterId);
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
        /// Validate a single skill icon texture: exists, is large enough, and has correct import settings.
        /// </summary>
        private static void ValidateSkillIconTexture(string imageKey, HashSet<string> validatedImageKeys, List<string> errors)
        {
            if (string.IsNullOrEmpty(imageKey) || !validatedImageKeys.Add(imageKey))
            {
                return;
            }

            var resourcePath = rimrushAssets.Images.ResourcePath(imageKey);
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
                $"Assets/rimrush/Resources/{resourcePath}.png",
                errors);
        }

        /// <summary>
        /// Check that a UI texture has mipmaps disabled, alpha transparency, and no compression.
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
        /// Check that platform-specific texture settings override defaults and stay uncompressed.
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
        /// Check that a corner pixel of the ball texture is transparent for clean rotation.
        /// </summary>
        private static void ValidateCornerAlpha(Texture2D texture, string resourcePath, int x, int y, List<string> errors)
        {
            if (texture.GetPixel(x, y).a > 0.03f)
            {
                errors.Add($"{resourcePath} corner ({x},{y}) should stay transparent for rotation.");
            }
        }

        /// <summary>
        /// Check that the ball fills the entire 36x36 canvas with no empty border.
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
        /// Check that semi-transparent pixels are not near-white, which would cause a visible halo.
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
        /// Verify that ball selection persists correctly across quick, training, versus, and tournament modes.
        /// </summary>
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

        /// <summary>
        /// Check that difficulty toggles cycle correctly and map to the expected AI skill levels.
        /// </summary>
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

        /// <summary>
        /// Run a full tournament at the given difficulty, verifying brackets, standings, and placements.
        /// </summary>
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

        /// <summary>
        /// Check that hard difficulty tournament rounds map to the expected escalating AI skills.
        /// </summary>
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

        /// <summary>
        /// Check that hell difficulty tournament rounds map to the expected maximum AI skills.
        /// </summary>
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

        /// <summary>
        /// Assert that the opponent's AI skill level matches the expected value.
        /// </summary>
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
