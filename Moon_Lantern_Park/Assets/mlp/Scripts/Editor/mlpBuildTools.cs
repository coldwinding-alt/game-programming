// Automatic packaging and building tool for Moon Lantern Park.

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace mlp.EditorTools
{
    /// <summary>
    /// Provides one-click and command-line builds for the supported desktop platforms.
    /// </summary>
    public static class mlpBuildTools
    {
        [MenuItem("Moon Lantern Park/Build/Windows 64-bit")]
        public static void BuildWindows()
        {
            RunBuild(
                BuildTarget.StandaloneWindows64,
                "Builds/Windows/MoonLanternPark.exe",
                "Windows");
        }

        [MenuItem("Moon Lantern Park/Build/macOS")]
        public static void BuildMacOS()
        {
            RunBuild(
                BuildTarget.StandaloneOSX,
                "Builds/macOS/Moon Lantern Park.app",
                "macOS");
        }

        private static void RunBuild(BuildTarget target, string outputPath, string label)
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError($"No enabled scenes found for {label} build.");
                EditorApplication.Exit(1);
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogError($"{label} build target is not installed for this Unity editor.");
                EditorApplication.Exit(1);
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target);

            var report = BuildPipeline.BuildPlayer(scenes, outputPath, target, BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Moon Lantern Park {label} build failed: {report.summary.result}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"Moon Lantern Park {label} build passed: {outputPath}");
            EditorApplication.Exit(0);
        }
    }
}
