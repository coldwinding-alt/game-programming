// 自动打包构建工具 / 在 Unity 编辑器里提供一键打包功能，自动把游戏构建到目标平台（Windows、Mac、Android 等）。省去手动设置构建选项的步骤。

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushBuildTools
    {
        /// <summary>
        /// Build the game for Windows 64-bit using the scenes enabled in Build Settings.
        /// </summary>
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("No enabled scenes found for Windows build.");
                EditorApplication.Exit(1);
                return;
            }

            var outputPath = "Builds/Windows/rimrush.exe";
            var report = BuildPipeline.BuildPlayer(
                scenes,
                outputPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"rimrush Windows build failed: {report.summary.result}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"rimrush Windows build passed: {outputPath}");
            EditorApplication.Exit(0);
        }
    }
}
