using System;
using UnityEditor;
using UnityEngine;

namespace rimrush.EditorTools
{
    public sealed class rimrushSkillIconImportSettings : AssetPostprocessor
    {
        private const string SkillIconsFolder = "Assets/rimrush/Resources/rimrush/Images/SkillIcons/";
        private const int RequiredMaxTextureSize = 4096;

        private void OnPreprocessTexture()
        {
            if (assetImporter is TextureImporter importer && IsSkillIconAsset(assetPath))
            {
                ApplySettings(importer);
            }
        }

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

        private static bool IsSkillIconAsset(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   path.StartsWith(SkillIconsFolder, StringComparison.OrdinalIgnoreCase) &&
                   path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

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
