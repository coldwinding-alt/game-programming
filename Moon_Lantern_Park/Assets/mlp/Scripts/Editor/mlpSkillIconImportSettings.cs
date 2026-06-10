// 技能图标导入设置自动配置 / 当技能图标图片被导入 Unity 时，自动把图片设置成正确的格式：不开压缩、不开 mipmap、双线性过滤、Clamp 模式。保证技能图标在游戏中清晰不模糊。也可以通过菜单 Tools > Mlp > Art 手动批量应用到所有图标。

using System;
using UnityEditor;
using UnityEngine;

namespace mlp.EditorTools
{
    /// <summary>
    /// 技能图标导入设置：当技能图标图片被导入 Unity 时，自动设置正确的格式（不压缩、不开 mipmap、双线性过滤），保证图标清晰不模糊。
    /// </summary>
    public sealed class mlpSkillIconImportSettings : AssetPostprocessor
    {
        private const string SkillIconsFolder = "Assets/mlp/Resources/mlp/Images/SkillIcons/";
        private const int RequiredMaxTextureSize = 4096;

        /// <summary>
        /// Unity 在导入纹理前自动调用。如果纹理是技能图标，则应用正确的导入设置。
        /// </summary>
        private void OnPreprocessTexture()
        {
            if (assetImporter is TextureImporter importer && IsSkillIconAsset(assetPath))
            {
                ApplySettings(importer);
            }
        }

        /// <summary>
        /// 菜单项：查找所有技能图标纹理并为每个应用正确的导入设置。
        /// </summary>
        [MenuItem("Tools/Mlp/Art/Apply Skill Icon Import Settings")]
        private static void ApplyToAllSkillIcons()
        {
            // 1. 在 SkillIcons 文件夹中搜索所有纹理资源
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SkillIconsFolder.TrimEnd('/') });
            for (var i = 0; i < guids.Length; i++)
            {
                // 2. 获取资源路径，确认是 SkillIcons 文件夹内的 PNG 文件
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || !IsSkillIconAsset(path))
                {
                    continue;
                }

                // 3. 应用导入设置，如果设置没有变化则跳过
                if (!ApplySettings(importer))
                {
                    continue;
                }

                // 4. 保存设置并重新导入该纹理
                importer.SaveAndReimport();
            }

            // 5. 刷新资源数据库
            AssetDatabase.Refresh();
            Debug.Log("Applied standalone skill icon import settings.");
        }

        /// <summary>
        /// 检查给定的资源路径是否指向 SkillIcons 文件夹内的 PNG 文件。
        /// </summary>
        /// <param name="path">要检查的资源路径。</param>
        /// <returns>如果路径是 SkillIcons 文件夹内的 PNG 文件则返回 true。</returns>
        private static bool IsSkillIconAsset(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith(SkillIconsFolder, StringComparison.OrdinalIgnoreCase) &&
                   path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将所需的导入设置应用到纹理导入器。
        /// </summary>
        /// <param name="importer">要配置的纹理导入器。</param>
        /// <returns>如果有任何设置被更改则返回 true。</returns>
        private static bool ApplySettings(TextureImporter importer)
        {
            var changed = false;

            // 1. 纹理类型设为默认（不做精灵图集等特殊处理）
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            // 2. 启用 alpha 透明处理（保留半透明边缘）
            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            // 3. 关闭 mipmap（UI 图标不需要多级缩略图，否则会模糊）
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            // 4. 关闭流式 mipmap 加载
            if (importer.streamingMipmaps)
            {
                importer.streamingMipmaps = false;
                changed = true;
            }

            // 5. 使用双线性过滤（在缩放时保持平滑但不模糊）
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            // 6. 纹理包裹模式设为 Clamp（边缘不会重复拉伸）
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            // 7. 最大纹理尺寸设为 4096（保证高清图标不被压缩缩小）
            if (importer.maxTextureSize != RequiredMaxTextureSize)
            {
                importer.maxTextureSize = RequiredMaxTextureSize;
                changed = true;
            }

            // 8. 为默认平台和独立构建平台分别应用不压缩设置
            if (ApplyPlatformSettings(importer, "DefaultTexturePlatform"))
            {
                changed = true;
            }

            if (ApplyPlatformSettings(importer, "Standalone"))
            {
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// 应用平台特定的纹理设置（不压缩，不使用 crunch 压缩）。
        /// </summary>
        /// <param name="importer">要配置的纹理导入器。</param>
        /// <param name="platformName">平台名称（如 "DefaultTexturePlatform" 或 "Standalone"）。</param>
        /// <returns>如果有任何设置被更改则返回 true。</returns>
        private static bool ApplyPlatformSettings(TextureImporter importer, string platformName)
        {
            // 1. 读取该平台当前的纹理导入设置
            var settings = importer.GetPlatformTextureSettings(platformName);
            // 2. 判断是否需要修改（未覆盖、尺寸不对、有压缩等都需要改）
            var changed =
                !settings.overridden ||
                settings.maxTextureSize != RequiredMaxTextureSize ||
                settings.textureCompression != TextureImporterCompression.Uncompressed ||
                settings.compressionQuality != 100 ||
                settings.crunchedCompression;

            // 3. 强制覆盖平台默认设置，关闭所有压缩（保持图标原始清晰度）
            settings.name = platformName;
            settings.overridden = true;
            settings.maxTextureSize = RequiredMaxTextureSize;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            // 4. 写入修改后的设置
            importer.SetPlatformTextureSettings(settings);
            return changed;
        }
    }
}
