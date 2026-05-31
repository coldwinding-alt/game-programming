// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushBuildTools 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushBuildTools
    {
        /// <summary>
        /// Executes Build Windows for the rimrushBuildTools workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
