// 自动打包构建工具 / 在 Unity 编辑器里提供一键打包功能，自动把游戏构建到目标平台（Windows、Mac、Android 等）。省去手动设置构建选项的步骤。

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace mlp.EditorTools
{
    /// <summary>
    /// 自动打包构建工具：在 Unity 编辑器里提供一键打包功能，自动把游戏构建到目标平台。
    /// </summary>
    public static class mlpBuildTools
    {
        /// <summary>
        /// 使用构建设置中启用的场景，为 Windows 64 位平台构建游戏。
        /// </summary>
        public static void BuildWindows()
        {
            // 1. 从 Unity 构建设置中收集所有已启用的场景路径
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            // 2. 如果没有任何已启用的场景，报错并退出（退出码 1 表示失败）
            if (scenes.Length == 0)
            {
                Debug.LogError("No enabled scenes found for Windows build.");
                EditorApplication.Exit(1);
                return;
            }

            // 3. 设置输出路径，调用 Unity 的构建管线生成 Windows 64 位可执行文件
            var outputPath = "Builds/Windows/MoonLanternPark.exe";
            var report = BuildPipeline.BuildPlayer(
                scenes,
                outputPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);

            // 4. 检查构建结果，如果失败则报错并退出
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Moon Lantern Park Windows build failed: {report.summary.result}");
                EditorApplication.Exit(1);
                return;
            }

            // 5. 构建成功，输出日志并以退出码 0（成功）退出
            Debug.Log($"Moon Lantern Park Windows build passed: {outputPath}");
            EditorApplication.Exit(0);
        }
    }
}
