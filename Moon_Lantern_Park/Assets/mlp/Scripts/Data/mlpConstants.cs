// 游戏常量和坐标转换工具
// 定义球场尺寸、像素比例、物理参数等基础数值。还提供像素坐标和世界坐标之间的转换函数，让画面在不同分辨率下都保持像素完美。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏常量和坐标转换工具：定义球场尺寸、像素比例、物理参数等基础数值，还提供像素坐标和世界坐标之间的转换函数。
    /// </summary>
    public static class mlpConstants
    {
        public const int Width = 800;           // 基础参考宽度（像素），用于 UI 布局等场景
        public const int Width2 = 400;          // 基础宽度的一半，用于水平居中计算
        public const int GameW = 1066;          // 游戏画面实际宽度（像素），4:3 比例下的宽度
        public const int GameH = 640;           // 游戏画面实际高度（像素），4:3 比例下的高度
        public const int GameW2 = 533;          // 游戏画面宽度的一半，用于坐标原点居中（像素坐标系中心）
        public const int GameH2 = 320;          // 游戏画面高度的一半，用于坐标原点居中
        public const int DisplayW = 1066;       // 显示区域宽度（像素），与 GameW 一致
        public const int DisplayH = 640;        // 显示区域高度（像素），与 GameH 一致
        public const int DisplayW2 = 533;       // 显示区域宽度的一半
        public const int DisplayH2 = 320;       // 显示区域高度的一半

        public const float PreloaderTime = 0.325f;  // 预加载画面持续时间（秒）
        public const float Step = 0.025f;           // 固定物理帧间隔（秒），即 40 FPS 的物理更新频率
        public const float MatchTime = 60f;         // 一场比赛的正常时长（秒）
        public const float OvertimeTime = 15f;      // 加时赛时长（秒）

        public const float RenderScale = 4f / 3f;   // 渲染缩放比，像素坐标到渲染坐标的缩放因子（4:3 比例）
        public const float PixelsPerUnit = 100f;     // Unity 中每单位对应的像素数，控制世界坐标的缩放精度
        public const float UnitsPerPixel = RenderScale / PixelsPerUnit;          // 每像素对应的 Unity 世界单位数，用于像素→世界坐标转换
        public const float PixelPerfectCharacterScale = 1f / RenderScale;        // 角色精灵的像素完美缩放值，确保精灵在屏幕上清晰不模糊

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
            // 1. 计算每个本地单位包含多少像素（考虑父物体的缩放）
            var pixelsPerLocalUnit = Mathf.Abs(parentWorldScale) * PixelsPerUnit;
            // 2. 如果缩放太小（接近零），直接返回原值，避免除以零的错误
            if (pixelsPerLocalUnit <= 0.0001f)
            {
                return localValue;
            }

            // 3. 将本地坐标换算成像素数，四舍五入到最近的整数像素，再换算回来
            return Mathf.Round(localValue * pixelsPerLocalUnit) / pixelsPerLocalUnit;
        }
    }
}
