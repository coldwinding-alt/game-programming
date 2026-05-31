// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushConstants 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

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
        /// Executes Pixel To World for the rimrushConstants workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="z">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Vector3 PixelToWorld(float x, float y, float z = 0f)
        {
            return new Vector3(
                (x * RenderScale - GameW2) / PixelsPerUnit,
                (GameH2 - y * RenderScale) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// Executes Pixel To World Snapped for the rimrushConstants workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="z">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Vector3 PixelToWorldSnapped(float x, float y, float z = 0f)
        {
            return new Vector3(
                (Mathf.Round(x * RenderScale) - GameW2) / PixelsPerUnit,
                (GameH2 - Mathf.Round(y * RenderScale)) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// Executes Snap Local Position To Screen Pixels for the rimrushConstants workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        /// <param name="localPosition">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Vector3 SnapLocalPositionToScreenPixels(Transform parent, Vector3 localPosition)
        {
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(
                SnapLocalAxisToScreenPixels(localPosition.x, parentScale.x),
                SnapLocalAxisToScreenPixels(localPosition.y, parentScale.y),
                localPosition.z);
        }

        /// <summary>
        /// Executes World To Pixel for the rimrushConstants workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="world">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Vector2 WorldToPixel(Vector3 world)
        {
            return new Vector2(
                (world.x * PixelsPerUnit + GameW2) / RenderScale,
                (GameH2 - world.y * PixelsPerUnit) / RenderScale);
        }

        /// <summary>
        /// Executes Snap Local Axis To Screen Pixels for the rimrushConstants workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="localValue">Input value used by this step of the workflow.</param>
        /// <param name="parentWorldScale">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
