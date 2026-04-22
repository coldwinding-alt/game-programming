using UnityEngine;

namespace BasketballLegends2020
{
    public static class BLConstants
    {
        public const int Width = 800;
        public const int Width2 = 400;
        public const int GameW = 1066;
        public const int GameH = 640;
        public const int GameW2 = 533;
        public const int GameH2 = 320;
        public const int DisplayW = 1066;
        public const int DisplayH = 640;
        public const int DisplayW2 = 533;
        public const int DisplayH2 = 320;

        public const float PreloaderTime = 0.325f;
        public const float Step = 0.025f;
        public const float MatchTime = 60f;
        public const float OvertimeTime = 15f;

        public const float RenderScale = 4f / 3f;
        public const float PixelsPerUnit = 100f;
        public const float UnitsPerPixel = RenderScale / PixelsPerUnit;
        public const float PixelPerfectCharacterScale = 1f / RenderScale;

        public static readonly int[] LimitsForAchievements =
        {
            20, 50, 5, 5, 20, 50, 500, 25, 25, 150, 150
        };

        public static Vector3 PixelToWorld(float x, float y, float z = 0f)
        {
            return new Vector3(
                (x * RenderScale - GameW2) / PixelsPerUnit,
                (GameH2 - y * RenderScale) / PixelsPerUnit,
                z);
        }

        public static Vector3 PixelToWorldSnapped(float x, float y, float z = 0f)
        {
            return new Vector3(
                (Mathf.Round(x * RenderScale) - GameW2) / PixelsPerUnit,
                (GameH2 - Mathf.Round(y * RenderScale)) / PixelsPerUnit,
                z);
        }

        public static Vector3 SnapLocalPositionToScreenPixels(Transform parent, Vector3 localPosition)
        {
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(
                SnapLocalAxisToScreenPixels(localPosition.x, parentScale.x),
                SnapLocalAxisToScreenPixels(localPosition.y, parentScale.y),
                localPosition.z);
        }

        public static Vector2 WorldToPixel(Vector3 world)
        {
            return new Vector2(
                (world.x * PixelsPerUnit + GameW2) / RenderScale,
                (GameH2 - world.y * PixelsPerUnit) / RenderScale);
        }

        private static float SnapLocalAxisToScreenPixels(float localValue, float parentWorldScale)
        {
            var pixelsPerLocalUnit = Mathf.Abs(parentWorldScale) * PixelsPerUnit;
            if (pixelsPerLocalUnit <= 0.0001f)
            {
                return localValue;
            }

            return Mathf.Round(localValue * pixelsPerLocalUnit) / pixelsPerLocalUnit;
        }
    }
}
