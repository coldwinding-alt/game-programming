using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace rimrush.EditorTools
{
    public static class rimrushBuildTools
    {
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
