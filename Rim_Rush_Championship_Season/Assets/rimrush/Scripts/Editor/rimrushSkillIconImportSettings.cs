// 技能图标导入设置自动配置 / 当技能图标图片被导入 Unity 时，自动把图片设置成正确的格式：不开压缩、不开 mipmap、双线性过滤、Clamp 模式。保证技能图标在游戏中清晰不模糊。也可以通过菜单 Tools > Rimrush > Art 手动批量应用到所有图标。

using System;
using UnityEditor;
using UnityEngine;

namespace rimrush.EditorTools
{
    public sealed class rimrushSkillIconImportSettings : AssetPostprocessor
    {
        private const string SkillIconsFolder = "Assets/rimrush/Resources/rimrush/Images/SkillIcons/";
        private const int RequiredMaxTextureSize = 4096;

        /// <summary>
        /// Called automatically by Unity before a texture is imported.
        /// If the texture is a skill icon, applies the correct import settings.
        /// </summary>
        private void OnPreprocessTexture()
        {
            if (assetImporter is TextureImporter importer && IsSkillIconAsset(assetPath))
            {
                ApplySettings(importer);
            }
        }

        /// <summary>
        /// Menu item: find all skill icon textures and apply the correct import settings to each one.
        /// </summary>
        [MenuItem("Tools/Rimrush/Art/Apply Skill Icon Import Settings")]
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
        /// Check if the given asset path points to a PNG inside the SkillIcons folder.
        /// </summary>
        /// <param name="path">The asset path to check.</param>
        /// <returns>True if the path is a PNG inside the SkillIcons folder.</returns>
        private static bool IsSkillIconAsset(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith(SkillIconsFolder, StringComparison.OrdinalIgnoreCase) &&
                   path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Apply the required import settings to a texture importer.
        /// </summary>
        /// <param name="importer">The texture importer to configure.</param>
        /// <returns>True if any setting was changed.</returns>
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
        /// Apply platform-specific texture settings (uncompressed, no crunching).
        /// </summary>
        /// <param name="importer">The texture importer to configure.</param>
        /// <param name="platformName">The platform name (e.g. "DefaultTexturePlatform" or "Standalone").</param>
        /// <returns>True if any setting was changed.</returns>
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
