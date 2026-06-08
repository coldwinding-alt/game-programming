// 技能图标导入设置自动配置 / 当技能图标图片被导入 Unity 时，自动把图片设置成正确的格式：不开压缩、不开 mipmap、双线性过滤、Clamp 模式。保证技能图标在游戏中清晰不模糊。也可以通过菜单 Tools > Mlp > Art 手动批量应用到所有图标。

using System;
using UnityEditor;
using UnityEngine;

namespace mlp.EditorTools
{
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
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SkillIconsFolder.TrimEnd('/') });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || !IsSkillIconAsset(path))
                {
                    continue;
                }

                if (!ApplySettings(importer))
                {
                    continue;
                }

                importer.SaveAndReimport();
            }

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

            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.streamingMipmaps)
            {
                importer.streamingMipmaps = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (importer.maxTextureSize != RequiredMaxTextureSize)
            {
                importer.maxTextureSize = RequiredMaxTextureSize;
                changed = true;
            }

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
            var settings = importer.GetPlatformTextureSettings(platformName);
            var changed =
                !settings.overridden ||
                settings.maxTextureSize != RequiredMaxTextureSize ||
                settings.textureCompression != TextureImporterCompression.Uncompressed ||
                settings.compressionQuality != 100 ||
                settings.crunchedCompression;

            settings.name = platformName;
            settings.overridden = true;
            settings.maxTextureSize = RequiredMaxTextureSize;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
            return changed;
        }
    }
}
