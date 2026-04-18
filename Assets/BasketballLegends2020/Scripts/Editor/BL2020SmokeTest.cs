using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BasketballLegends2020.EditorTools
{
    public static class BL2020SmokeTest
    {
        public static void Run()
        {
            var errors = new List<string>();
            GameObject root = null;

            try
            {
                CheckResource<Texture2D>("BL2020/Atlases/gameplay", errors);
                CheckResource<TextAsset>("BL2020/Atlases/gameplay", errors);
                CheckResource<Texture2D>("BL2020/Atlases/interface", errors);
                CheckResource<TextAsset>("BL2020/Atlases/interface", errors);
                CheckResource<TextAsset>("BL2020/DragonBones/sk2", errors);
                CheckResource<TextAsset>("BL2020/DragonBones/texture2", errors);
                CheckResource<Texture2D>("BL2020/DragonBones/texture2", errors);
                CheckResource<AudioClip>("BL2020/Sound/24_TrackSnd", errors);

                var gameplay = BLAtlasCache.Instance.Gameplay;
                if (!gameplay.HasFrame("0bg_gameplay0000") || !gameplay.HasFrame("BallMC0000"))
                {
                    errors.Add("Gameplay atlas did not expose expected original frame keys.");
                }

                DBLiteFactory.Instance.EnsureLoaded();
                var armature = BLPlayersData.BuildGameplayArmature("SmokePlayerSmall");
                if (armature == null)
                {
                    errors.Add("Could not build DragonBones playerSmall armature.");
                }
                else
                {
                    BLPlayersData.SwitchPlayer(armature, 0, 0);
                    UnityEngine.Object.DestroyImmediate(armature.gameObject);
                }

                root = new GameObject("SmokeRuntimeRoot");
                BLAudio.Create(root.transform);
                BLInventory.Instance.StartQuickGame();
                var core = new BLGameBuilder().Build(root.transform);
                core.Update(0.016f);
            }
            catch (Exception ex)
            {
                errors.Add(ex.ToString());
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Debug.LogError(error);
                }

                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("BL2020 smoke test passed.");
            EditorApplication.Exit(0);
        }

        private static void CheckResource<T>(string path, List<string> errors) where T : UnityEngine.Object
        {
            if (Resources.Load<T>(path) == null)
            {
                errors.Add($"Missing resource: {path}");
            }
        }
    }
}
