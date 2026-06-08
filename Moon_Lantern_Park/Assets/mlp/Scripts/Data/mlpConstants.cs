// 游戏常量和坐标转换工具
// 定义球场尺寸、像素比例、物理参数等基础数值。还提供像素坐标和世界坐标之间的转换函数，让画面在不同分辨率下都保持像素完美。

using UnityEngine;

namespace mlp
{
    public static class mlpConstants
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
        /// 将像素坐标转换为 Unity 世界坐标。用于将游戏物体精确放置在屏幕的指定像素位置上。
        /// </summary>
        /// <param name="x">水平像素位置。</param>
        /// <param name="y">垂直像素位置（0 = 屏幕顶部）。</param>
        /// <param name="z">Z 轴深度，用于控制渲染排序。</param>
        /// <returns>对应的 Unity 世界空间坐标。</returns>
        public static Vector3 PixelToWorld(float x, float y, float z = 0f)
        {
            return new Vector3(
                (x * RenderScale - GameW2) / PixelsPerUnit,
                (GameH2 - y * RenderScale) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// 将像素坐标转换为世界坐标，并对齐到最近的像素边界。通过对齐像素网格，防止精灵出现模糊。
        /// </summary>
        /// <param name="x">水平像素位置。</param>
        /// <param name="y">垂直像素位置（0 = 屏幕顶部）。</param>
        /// <param name="z">Z 轴深度，用于控制渲染排序。</param>
        /// <returns>对齐后的 Unity 世界空间坐标。</returns>
        public static Vector3 PixelToWorldSnapped(float x, float y, float z = 0f)
        {
            return new Vector3(
                (Mathf.Round(x * RenderScale) - GameW2) / PixelsPerUnit,
                (GameH2 - Mathf.Round(y * RenderScale)) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// 将本地坐标对齐到最近的屏幕像素，确保子物体保持清晰不模糊。
        /// </summary>
        /// <param name="parent">父级 Transform，使用其世界缩放来计算像素边界。</param>
        /// <param name="localPosition">要对齐的本地坐标。</param>
        /// <returns>X 和 Y 已对齐到最近像素的本地坐标。</returns>
        public static Vector3 SnapLocalPositionToScreenPixels(Transform parent, Vector3 localPosition)
        {
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(
                SnapLocalAxisToScreenPixels(localPosition.x, parentScale.x),
                SnapLocalAxisToScreenPixels(localPosition.y, parentScale.y),
                localPosition.z);
        }

        /// <summary>
        /// 将 Unity 世界坐标反向转换为像素坐标。是 PixelToWorld 的逆运算。
        /// </summary>
        /// <param name="world">Unity 世界空间中的位置。</param>
        /// <returns>对应的像素坐标。</returns>
        public static Vector2 WorldToPixel(Vector3 world)
        {
            return new Vector2(
                (world.x * PixelsPerUnit + GameW2) / RenderScale,
                (GameH2 - world.y * PixelsPerUnit) / RenderScale);
        }

        /// <summary>
        /// 根据父级的世界缩放，将单个轴的值对齐到最近的像素边界。
        /// </summary>
        /// <param name="localValue">某个轴上的本地空间位置值。</param>
        /// <param name="parentWorldScale">父级在同一轴上的世界缩放值。</param>
        /// <returns>对齐到最近像素的值。</returns>
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
