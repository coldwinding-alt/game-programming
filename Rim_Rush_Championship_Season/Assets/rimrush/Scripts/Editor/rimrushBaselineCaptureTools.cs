using System;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushBaselineCaptureTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string DocsRoot = "DOCS/GoldenBaseline";

        [MenuItem("rimrush/Baseline/Export Stage 0 Baseline Package")]
        public static void ExportStage0BaselinePackage()
        {
            Directory.CreateDirectory(DocsRoot);
            Directory.CreateDirectory(Path.Combine(DocsRoot, "screens"));

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = GameObject.Find("rimrushBootstrap");
            var mainCamera = GameObject.Find("Main Camera");

            File.WriteAllText(Path.Combine(DocsRoot, "README.md"), BuildReadme(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(DocsRoot, "HOST_SCENE_HIERARCHY.md"), BuildHostHierarchy(scene, mainCamera, bootstrap), Encoding.UTF8);
            File.WriteAllText(Path.Combine(DocsRoot, "KEY_LAYOUT_REFERENCE.md"), BuildLayoutReference(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(DocsRoot, "SCREEN_CAPTURE_CHECKLIST.md"), BuildScreenCaptureChecklist(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(DocsRoot, "BEHAVIOR_CHECKLIST.md"), BuildBehaviorChecklist(), Encoding.UTF8);

            var gitKeep = Path.Combine(DocsRoot, "screens", ".gitkeep");
            if (!File.Exists(gitKeep))
            {
                File.WriteAllText(gitKeep, string.Empty, Encoding.UTF8);
            }

            AssetDatabase.Refresh();
            Debug.Log("Exported Stage 0 baseline package.");
        }

        private static string BuildReadme()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Golden Baseline");
            builder.AppendLine();
            builder.AppendLine("This package is the parity reference for the scene/prefab migration.");
            builder.AppendLine();
            builder.AppendLine("## Source of truth");
            builder.AppendLine();
            builder.AppendLine("- Primary runtime reference: the current repository with runtime-default bootstrap enabled");
            builder.AppendLine("- Frozen backup reference: `../Rim_Rush_Championship_Season_RUNTIME_ORIGINAL_BACKUP`");
            builder.AppendLine("- Main scene authority: `Assets/Scenes/Main.unity`");
            builder.AppendLine();
            builder.AppendLine("## Required artifacts");
            builder.AppendLine();
            builder.AppendLine("- `HOST_SCENE_HIERARCHY.md`: stage 1 host structure and component reference");
            builder.AppendLine("- `KEY_LAYOUT_REFERENCE.md`: anchor positions, sizes, and scales for parity checks");
            builder.AppendLine("- `SCREEN_CAPTURE_CHECKLIST.md`: required screenshots to capture before each default cutover");
            builder.AppendLine("- `BEHAVIOR_CHECKLIST.md`: required manual parity checks");
            builder.AppendLine();
            builder.AppendLine("## Screenshot storage");
            builder.AppendLine();
            builder.AppendLine("Store captured reference PNGs in `DOCS/GoldenBaseline/screens/` with the exact filenames listed in `SCREEN_CAPTURE_CHECKLIST.md`.");
            builder.AppendLine();
            builder.AppendLine("## Usage");
            builder.AppendLine();
            builder.AppendLine("1. Keep the runtime-default path enabled while capturing the baseline.");
            builder.AppendLine("2. Update screenshots only when the approved golden baseline changes.");
            builder.AppendLine("3. Before each migration stage is switched on by default, compare the new path against this package.");
            return builder.ToString();
        }

        private static string BuildHostHierarchy(UnityEngine.SceneManagement.Scene scene, GameObject mainCamera, GameObject bootstrap)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Host Scene Hierarchy");
            builder.AppendLine();
            builder.AppendLine($"- Scene: `{scene.path}`");
            builder.AppendLine($"- Captured: `{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC`");
            builder.AppendLine();

            if (mainCamera != null)
            {
                builder.AppendLine("## Main Camera");
                builder.AppendLine();
                AppendTree(mainCamera.transform, builder, 0);
                builder.AppendLine();
            }

            if (bootstrap != null)
            {
                builder.AppendLine("## Bootstrap Root");
                builder.AppendLine();
                AppendTree(bootstrap.transform, builder, 0);
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine("## Bootstrap Root");
                builder.AppendLine();
                builder.AppendLine("`rimrushBootstrap` not found in the scene.");
            }

            return builder.ToString();
        }

        private static string BuildLayoutReference()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Key Layout Reference");
            builder.AppendLine();
            builder.AppendLine($"- UI logical width: `{rimrushConstants.Width}`");
            builder.AppendLine("- UI logical height: `480`");
            builder.AppendLine($"- Render size: `{rimrushConstants.GameW} x {rimrushConstants.GameH}`");
            builder.AppendLine($"- Pixels per unit: `{rimrushConstants.PixelsPerUnit}`");
            builder.AppendLine($"- Render scale: `{rimrushConstants.RenderScale}`");
            builder.AppendLine();

            builder.AppendLine("## Menu Shell");
            builder.AppendLine();
            builder.AppendLine("| Element | X | Y | Width | Height | Extra |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
            builder.AppendLine($"| Background center | {rimrushConstants.Width2} | 240 | 800 | 480 | frame driven |");
            builder.AppendLine($"| Logo center | {rimrushConstants.Width2} | 68 | - | - | scale `{ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuLogoScaleX")}` x `{ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuLogoScaleY")}` |");
            builder.AppendLine($"| Music button | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuMusicButtonX")} | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopButtonY")} | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopButtonSize")} | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopButtonSize")} | icon target `{ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopIconPixels")}` |");
            builder.AppendLine($"| Help button | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuHelpButtonX")} | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopButtonY")} | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopButtonSize")} | {ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopButtonSize")} | icon target `{ReadPrivateConst<float>(typeof(rimrushGameBootstrap), "MenuTopIconPixels")}` |");
            builder.AppendLine();

            builder.AppendLine("## HUD");
            builder.AppendLine();
            builder.AppendLine("| Element | X | Y | Width | Height | Extra |");
            builder.AppendLine("| --- | ---: | ---: | ---: | ---: | --- |");
            builder.AppendLine($"| Scoreboard center | {ReadPrivateConst<float>(typeof(rimrushHudView), "ScoreboardCenterX")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "ScoreboardCenterY")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "ScoreboardTargetWidth")} | - | backdrop width target |");
            builder.AppendLine($"| Timer | {ReadPrivateConst<float>(typeof(rimrushHudView), "ScoreboardCenterX")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TimerY")} | - | - | text |");
            builder.AppendLine($"| Pause button | {ReadPrivateConst<float>(typeof(rimrushHudView), "PauseButtonX")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonY")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonSize")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonSize")} | icon target `{ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightIconPixels")}` |");
            builder.AppendLine($"| Music button | {ReadPrivateConst<float>(typeof(rimrushHudView), "MusicButtonX")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonY")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonSize")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonSize")} | icon target `{ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightIconPixels")}` |");
            builder.AppendLine($"| Help button | {ReadPrivateConst<float>(typeof(rimrushHudView), "HelpButtonX")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonY")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonSize")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightButtonSize")} | icon target `{ReadPrivateConst<float>(typeof(rimrushHudView), "TopRightIconPixels")}` |");
            builder.AppendLine($"| Countdown center | {rimrushConstants.Width2} | {ReadPrivateConst<float>(typeof(rimrushHudView), "CountdownY")} | 360 | - | popup width target |");
            builder.AppendLine($"| Message popup center | {rimrushConstants.Width2} | {ReadPrivateConst<float>(typeof(rimrushHudView), "PopupCenterY")} | {ReadPrivateConst<float>(typeof(rimrushHudView), "PopupBackdropWidth")} | - | popup width target |");
            builder.AppendLine();

            builder.AppendLine("## Gameplay Anchors");
            builder.AppendLine();
            builder.AppendLine("| Element | X | Y | Extra |");
            builder.AppendLine("| --- | ---: | ---: | --- |");
            builder.AppendLine($"| Left basket center | {rimrushObjectsData.BasketCenter} | {rimrushObjectsData.BasketHeight} | radius `{rimrushObjectsData.BasketRadius}` |");
            builder.AppendLine($"| Right basket center | {rimrushObjectsData.BasketCenter2} | {rimrushObjectsData.BasketHeight} | radius `{rimrushObjectsData.BasketRadius}` |");
            builder.AppendLine($"| Left neutral spawn | {rimrushConstants.Width2 - rimrushObjectsData.PlayerIndentX} | {rimrushObjectsData.PlayerIndentY} | player restart without serve |");
            builder.AppendLine($"| Right neutral spawn | {rimrushConstants.Width2 + rimrushObjectsData.PlayerIndentX} | {rimrushObjectsData.PlayerIndentY} | player restart without serve |");
            builder.AppendLine($"| Left serve spawn | {rimrushObjectsData.IndentGeneralX} | {rimrushObjectsData.PlayerIndentY} | player restart after opponent score |");
            builder.AppendLine($"| Right serve spawn | {rimrushConstants.Width - rimrushObjectsData.IndentGeneralX} | {rimrushObjectsData.PlayerIndentY} | player restart after opponent score |");
            builder.AppendLine($"| Floor Y | - | {rimrushObjectsData.FloorY} | player floor |");
            builder.AppendLine($"| Ball floor Y | - | {rimrushObjectsData.BallFloorY} | ball rest height |");
            builder.AppendLine($"| Ball pickup center Y | - | {rimrushObjectsData.BallIndentYPlayer} | carried ball baseline |");
            return builder.ToString();
        }

        private static string BuildScreenCaptureChecklist()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Screen Capture Checklist");
            builder.AppendLine();
            builder.AppendLine("Capture each screen from the runtime-default baseline before enabling a new scene/prefab path by default.");
            builder.AppendLine();
            builder.AppendLine("- [ ] `player-count.png`");
            builder.AppendLine("- [ ] `match-type.png`");
            builder.AppendLine("- [ ] `quick-match-setup.png`");
            builder.AppendLine("- [ ] `training-setup.png`");
            builder.AppendLine("- [ ] `two-player-setup.png`");
            builder.AppendLine("- [ ] `tournament-setup.png`");
            builder.AppendLine("- [ ] `tournament-bracket.png`");
            builder.AppendLine("- [ ] `tournament-awards.png`");
            builder.AppendLine("- [ ] `gameplay-tipoff.png`");
            builder.AppendLine("- [ ] `gameplay-live-hud.png`");
            builder.AppendLine("- [ ] `pause-overlay.png`");
            builder.AppendLine("- [ ] `post-match.png`");
            return builder.ToString();
        }

        private static string BuildBehaviorChecklist()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Behavior Checklist");
            builder.AppendLine();
            builder.AppendLine("Use this list for manual parity checks before switching a migrated subsystem on by default.");
            builder.AppendLine();
            builder.AppendLine("## Menu and Flow");
            builder.AppendLine();
            builder.AppendLine("- [ ] Player Count menu opens on boot");
            builder.AppendLine("- [ ] Match Type menu opens and returns correctly");
            builder.AppendLine("- [ ] Quick Match setup updates character, difficulty, and ball selection correctly");
            builder.AppendLine("- [ ] Training setup updates character and ball selection correctly");
            builder.AppendLine("- [ ] Two Players setup updates both characters and ball selection correctly");
            builder.AppendLine("- [ ] Tournament setup, bracket, and awards screens match the runtime baseline");
            builder.AppendLine();
            builder.AppendLine("## HUD and Pause");
            builder.AppendLine();
            builder.AppendLine("- [ ] Scoreboard portraits, names, scores, and timer match the baseline layout");
            builder.AppendLine("- [ ] Countdown, popup messages, bonus notices, and post-match prompts match the baseline");
            builder.AppendLine("- [ ] Pause overlay layout, portraits, score board, and buttons match the baseline");
            builder.AppendLine("- [ ] Top-right pause, music, and help buttons retain hover and click behavior");
            builder.AppendLine();
            builder.AppendLine("## Gameplay");
            builder.AppendLine();
            builder.AppendLine("- [ ] Court, baskets, and ball appear at the same positions and sorting layers");
            builder.AppendLine("- [ ] Tipoff, restart, and serve positions match the baseline");
            builder.AppendLine("- [ ] Ball bounce, basket sensor scoring, and rim collisions behave the same");
            builder.AppendLine("- [ ] Player movement, jump, pickup, steal, block, and super abilities behave the same");
            builder.AppendLine("- [ ] Tournament advancement, return to menu, and repeated match loops behave the same");
            return builder.ToString();
        }

        private static void AppendTree(Transform node, StringBuilder builder, int depth)
        {
            if (node == null)
            {
                return;
            }

            var indent = new string(' ', depth * 2);
            builder.Append(indent);
            builder.Append("- `");
            builder.Append(node.name);
            builder.Append("`");
            builder.Append(node.gameObject.activeSelf ? " (active)" : " (inactive)");

            var components = node.GetComponents<Component>();
            if (components.Length > 1)
            {
                builder.Append(" - ");
                for (var i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        continue;
                    }

                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(components[i].GetType().Name);
                }
            }

            builder.AppendLine();

            for (var i = 0; i < node.childCount; i++)
            {
                AppendTree(node.GetChild(i), builder, depth + 1);
            }
        }

        private static T ReadPrivateConst<T>(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(type.FullName, fieldName);
            }

            return (T)Convert.ChangeType(field.GetRawConstantValue(), typeof(T));
        }
    }
}
