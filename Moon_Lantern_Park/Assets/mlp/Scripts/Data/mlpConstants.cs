// Game constants and coordinate conversion tools
// Define basic values ​​such as court size, pixel ratio, and physical parameters. It also provides a conversion function between pixel coordinates and world coordinates to keep the picture pixel perfect at different resolutions.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Game constants and coordinate conversion tools: define basic values ​​such as court size, pixel ratio, physical parameters, etc., and also provide conversion functions between pixel coordinates and world coordinates.

    /// </summary>
    public static class mlpConstants
    {
        public const int Width = 800;           // Basic reference width (pixels), used for UI layout and other scenarios
        public const int Width2 = 400;          // Half the base width, used for horizontal centering calculations

        public const int GameW = 1066;          // Actual width of the game screen (pixels), width at 4:3 ratio

        public const int GameH = 640;           // The actual height of the game screen (pixels), height in 4:3 ratio

        public const int GameW2 = 533;          // Half the width of the game screen, used to center the coordinate origin (center of the pixel coordinate system)

        public const int GameH2 = 320;          // Half the height of the game screen, used to center the coordinate origin

        public const int DisplayW = 1066;       // Display area width (pixels), consistent with GameW

        public const int DisplayH = 640;        // Display area height (pixels), consistent with GameH

        public const int DisplayW2 = 533;       // Half the width of the display area

        public const int DisplayH2 = 320;       // Half the height of the display area


        public const float PreloaderTime = 0.325f;  // Preload screen duration (seconds)

        public const float Step = 0.025f;           // Fixed physics frame interval (seconds), which is a physics update frequency of 40 FPS

        public const float MatchTime = 60f;         // Normal length of a match (seconds)
        public const float OvertimeTime = 15f;      // Overtime duration (seconds)


        public const float RenderScale = 4f / 3f;   // Rendering scaling ratio, scaling factor from pixel coordinates to rendering coordinates (4:3 ratio)

        public const float PixelsPerUnit = 100f;     // The number of pixels corresponding to each unit in Unity controls the scaling accuracy of world coordinates
        public const float UnitsPerPixel = RenderScale / PixelsPerUnit;          // The number of Unity world units corresponding to each pixel, used for pixel → world coordinate conversion
        public const float PixelPerfectCharacterScale = 1f / RenderScale;        // Pixel-perfect scaling values ​​for character sprites, ensuring sprites are clear and not blurry on screen

        public static readonly int[] LimitsForAchievements =
        {
            20, 50, 5, 5, 20, 50, 500, 25, 25, 150, 150
        };

        /// <summary>
        /// Convert pixel coordinates to Unity world coordinates. Used to place game objects precisely at specified pixel locations on the screen.
        /// </summary>
        /// <param name="x">Horizontal pixel position. </param>
        /// <param name="y">Vertical pixel position (0 = top of screen). </param>
        /// <param name="z">Z-axis depth, used to control rendering ordering. </param>
        /// <returns>The corresponding Unity world space coordinates. </returns>
        public static Vector3 PixelToWorld(float x, float y, float z = 0f)
        {
            return new Vector3(
                (x * RenderScale - GameW2) / PixelsPerUnit,
                (GameH2 - y * RenderScale) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// Convert pixel coordinates to world coordinates and align to the nearest pixel boundary. Prevent sprites from appearing blurry by aligning them to the pixel grid.
        /// </summary>
        /// <param name="x">Horizontal pixel position. </param>
        /// <param name="y">Vertical pixel position (0 = top of screen). </param>
        /// <param name="z">Z-axis depth, used to control rendering ordering. </param>
        /// <returns>Aligned Unity world space coordinates. </returns>
        public static Vector3 PixelToWorldSnapped(float x, float y, float z = 0f)
        {
            return new Vector3(
                (Mathf.Round(x * RenderScale) - GameW2) / PixelsPerUnit,
                (GameH2 - Mathf.Round(y * RenderScale)) / PixelsPerUnit,
                z);
        }

        /// <summary>
        /// Align local coordinates to the nearest screen pixel to ensure that sub-objects remain sharp and not blurry.
        /// </summary>
        /// <param name="parent">Parent Transform that uses its world scale to calculate pixel bounds. </param>
        /// <param name="localPosition">The local coordinates to align to. </param>
        /// <returns>X and Y are aligned to the local coordinates of the nearest pixel. </returns>
        public static Vector3 SnapLocalPositionToScreenPixels(Transform parent, Vector3 localPosition)
        {
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            return new Vector3(
                SnapLocalAxisToScreenPixels(localPosition.x, parentScale.x),
                SnapLocalAxisToScreenPixels(localPosition.y, parentScale.y),
                localPosition.z);
        }

        /// <summary>
        /// Convert Unity world coordinates back to pixel coordinates. Is the inverse operation of PixelToWorld.
        /// </summary>
        /// <param name="world">Position in Unity world space. </param>
        /// <returns>The corresponding pixel coordinates. </returns>
        public static Vector2 WorldToPixel(Vector3 world)
        {
            return new Vector2(
                (world.x * PixelsPerUnit + GameW2) / RenderScale,
                (GameH2 - world.y * PixelsPerUnit) / RenderScale);
        }

        /// <summary>
        /// Aligns individual axis values ​​to the nearest pixel boundary based on the parent's world scale.
        /// </summary>
        /// <param name="localValue">The local spatial position value on an axis. </param>
        /// <param name="parentWorldScale">The parent's world scale value on the same axis. </param>
        /// <returns>Align to the value of the nearest pixel. </returns>
        private static float SnapLocalAxisToScreenPixels(float localValue, float parentWorldScale)
        {
            // 1. Calculate how many pixels each local unit contains (taking into account the scaling of the parent object)
            var pixelsPerLocalUnit = Mathf.Abs(parentWorldScale) * PixelsPerUnit;
            // 2. If the scaling is too small (close to zero), return the original value directly to avoid division by zero errors.

            if (pixelsPerLocalUnit <= 0.0001f)
            {
                return localValue;
            }

            // 3. Convert local coordinates to pixels, round to the nearest integer pixel, and then convert back

            return Mathf.Round(localValue * pixelsPerLocalUnit) / pixelsPerLocalUnit;
        }
    }
}
