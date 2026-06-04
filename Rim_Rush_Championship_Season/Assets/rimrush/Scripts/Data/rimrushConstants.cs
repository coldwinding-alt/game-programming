// 游戏常量和坐标转换工具
// 定义球场尺寸、像素比例、物理参数等基础数值。还提供像素坐标和世界坐标之间的转换函数，让画面在不同分辨率下都保持像素完美。

using UnityEngine;

namespace rimrush
{
    public static class rimrushConstants
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

        /// <summary>
        /// Convert a position from pixel coordinates to Unity world coordinates. Used to place game objects at exact pixel positions on screen.
        /// </summary>
        /// <param name="x">Horizontal pixel position.</param>
        /// <param name="y">Vertical pixel position (0 = top of the screen).</param>
        /// <param name="z">Z depth for sorting order.</param>
        /// <returns>The equivalent position in Unity world space.</returns>
        public static Vector3 PixelToWorld(float x, float y, float z = 0f)
        {
            return new Vector3(
                (x * RenderScale - GameW2) / PixelsPerUnit,
                (GameH2 - y * RenderScale) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// Convert pixel coordinates to world coordinates, snapping to the nearest pixel. This prevents blurry sprites by keeping everything aligned to the pixel grid.
        /// </summary>
        /// <param name="x">Horizontal pixel position.</param>
        /// <param name="y">Vertical pixel position (0 = top of the screen).</param>
        /// <param name="z">Z depth for sorting order.</param>
        /// <returns>The snapped position in Unity world space.</returns>
        public static Vector3 PixelToWorldSnapped(float x, float y, float z = 0f)
        {
            return new Vector3(
                (Mathf.Round(x * RenderScale) - GameW2) / PixelsPerUnit,
                (GameH2 - Mathf.Round(y * RenderScale)) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// Snap a local position to the nearest screen pixel so child objects stay sharp.
        /// </summary>
        /// <param name="parent">The parent transform whose world scale is used to calculate pixel boundaries.</param>
        /// <param name="localPosition">The local position to snap.</param>
        /// <returns>The local position with X and Y snapped to the nearest pixel.</returns>
        public static Vector3 SnapLocalPositionToScreenPixels(Transform parent, Vector3 localPosition)
        {
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(
                SnapLocalAxisToScreenPixels(localPosition.x, parentScale.x),
                SnapLocalAxisToScreenPixels(localPosition.y, parentScale.y),
                localPosition.z);
        }

        /// <summary>
        /// Convert a Unity world position back to pixel coordinates. The reverse of PixelToWorld.
        /// </summary>
        /// <param name="world">A position in Unity world space.</param>
        /// <returns>The equivalent pixel coordinates.</returns>
        public static Vector2 WorldToPixel(Vector3 world)
        {
            return new Vector2(
                (world.x * PixelsPerUnit + GameW2) / RenderScale,
                (GameH2 - world.y * PixelsPerUnit) / RenderScale);
        }

        /// <summary>
        /// Snap a single axis value to the nearest pixel boundary based on the parent's world scale.
        /// </summary>
        /// <param name="localValue">The local-space position value on one axis.</param>
        /// <param name="parentWorldScale">The parent's world scale on that same axis.</param>
        /// <returns>The value snapped to the nearest pixel.</returns>
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
