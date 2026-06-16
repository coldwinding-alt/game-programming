using System.Collections;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Keeps the standalone player presentation friendly for demos without changing game rendering scale.
    /// The fixed-resolution presenter still owns aspect-ratio preservation and pixel clarity.
    /// </summary>
    public sealed class mlpDisplayModeManager : MonoBehaviour
    {
        private const int PreferredWindowWidth = 1280;
        private const int PreferredWindowHeight = 768;

        private void Start()
        {
            if (Application.isEditor || Application.isBatchMode)
            {
                return;
            }

            StartCoroutine(ApplyInitialWindowMode());
        }

        private void Update()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                ToggleFullscreen();
            }
        }

        private static void ToggleFullscreen()
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed || !Screen.fullScreen)
            {
                RequestFullscreenWindow();
                return;
            }

            Screen.SetResolution(
                PreferredWindowWidth,
                PreferredWindowHeight,
                FullScreenMode.Windowed);
        }

        private static void RequestWindowedMode()
        {
            Screen.SetResolution(
                PreferredWindowWidth,
                PreferredWindowHeight,
                FullScreenMode.Windowed);
        }

        private static IEnumerator ApplyInitialWindowMode()
        {
            RequestWindowedMode();
            yield return null;
            RequestWindowedMode();
            yield return null;
            RequestWindowedMode();
        }

        private static void RequestFullscreenWindow()
        {
            var width = Mathf.Max(mlpConstants.DisplayW, Display.main.systemWidth);
            var height = Mathf.Max(mlpConstants.DisplayH, Display.main.systemHeight);
            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        }
    }
}
