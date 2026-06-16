// Fixed Resolution Graphics Adapter / Allows games to always maintain an 800x480 pixel style graphics, no matter the size of the window or screen. Automatically calculate the black border area and scaling ratio to ensure that the picture is not deformed or blurred, and at the same time, the mouse coordinates are correctly converted to the pixel position in the game.

using UnityEngine;
using UnityEngine.UI;

namespace mlp
{
    /// <summary>
    /// Fixed resolution graphics adapter: Let the game always maintain an 800x480 pixel style graphics, no matter how big the window is. Automatically calculate black borders and zoom to ensure the picture is not deformed.
    /// </summary>
    public sealed class mlpFixedResolutionPresenter : MonoBehaviour
    {
        private const string CanvasName = "mlpFixedResolutionCanvas";
        private const string CameraName = "mlpFixedResolutionCamera";
        private const string BackgroundName = "mlpFixedResolutionBackground";
        private const string OutputName = "mlpFixedResolutionOutput";
        private static mlpFixedResolutionPresenter activePresenter;

        private Camera sourceCamera;
        private Camera outputCamera;
        private Canvas canvas;
        private RectTransform outputRect;
        private RawImage outputImage;
        private RenderTexture renderTexture;
        private Rect outputScreenRect;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private bool originalAllowHdr;
        private bool originalAllowMsaa;
        private bool configured;

        public static bool HasActivePresenter => activePresenter != null && activePresenter.configured;

        /// <summary>
        /// Plug in a source camera so that its output is rendered at a fixed resolution.

        /// </summary>
        public void Attach(Camera camera)
        {
            // 1. If the incoming camera is empty, exit directly

            if (camera == null)
            {
                return;
            }

            // 2. If the same camera is already connected, just refresh the layout

            if (sourceCamera == camera && configured)
            {
                RefreshLayout(force: true);
                return;
            }

            // 3. Disconnect the previously connected camera (if any)

            DetachCurrentCamera();
            // 4. Save the source camera reference and turn off HDR and MSAA (these features are not required for Pixel Style)

            sourceCamera = camera;
            originalAllowHdr = sourceCamera.allowHDR;
            originalAllowMsaa = sourceCamera.allowMSAA;
            sourceCamera.allowHDR = false;
            sourceCamera.allowMSAA = false;

            // 5. Create an output camera (used to render the UI canvas on top of the screen), UI canvas and rendering texture
            EnsureOutputCamera();
            EnsureCanvas();
            EnsureRenderTexture();
            // 6. Display the renderer and set the source camera’s render target to a fixed-resolution render texture.

            SetPresenterVisible(true);
            sourceCamera.targetTexture = renderTexture;
            // 7. Mark as configured, register as a global active renderer, and force a layout refresh

            configured = true;
            activePresenter = this;
            RefreshLayout(force: true);
        }

        /// <summary>
        /// Disconnects the current source camera and hides the renderer.

        /// </summary>
        public void Detach()
        {
            DetachCurrentCamera();
            SetPresenterVisible(false);
            if (activePresenter == this)
            {
                activePresenter = null;
            }
        }

        /// <summary>
        /// Convert screen space coordinates to pixel coordinates at the game's fixed resolution.

        /// </summary>
        public static bool TryMapScreenToGamePixel(Vector2 screenPosition, out Vector2 gamePixel)
        {
            if (activePresenter != null)
            {
                return activePresenter.TryMapScreenToGamePixelInternal(screenPosition, out gamePixel);
            }

            gamePixel = default;
            return false;
        }

        /// <summary>
        /// Refresh the layout every frame in case the screen size changes.

        /// </summary>
        private void LateUpdate()
        {
            if (!configured)
            {
                return;
            }

            RefreshLayout(force: false);
        }

        /// <summary>
        /// Clean up the render texture, canvas, and output camera when the component is destroyed.

        /// </summary>
        private void OnDestroy()
        {
            // 1. Restore the original settings of the source camera and disconnect

            DetachCurrentCamera();
            // 2. Release and destroy rendering textures (release GPU memory)

            if (renderTexture != null)
            {
                if (renderTexture.IsCreated())
                {
                    renderTexture.Release();
                }

                Destroy(renderTexture);
                renderTexture = null;
            }

            // 3. Destroy the UI canvas

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }

            // 4. Destroy the output camera

            if (outputCamera != null)
            {
                Destroy(outputCamera.gameObject);
                outputCamera = null;
            }

            // 5. Clear global active renderer references

            if (activePresenter == this)
            {
                activePresenter = null;
            }
        }

        /// <summary>
        /// Builds the UI canvas, background, and output RawImage if not already created.
        /// </summary>
        private void EnsureCanvas()
        {
            if (canvas != null)
            {
                return;
            }

            var canvasObject = new GameObject(CanvasName);
            canvasObject.transform.SetParent(transform, false);
            ApplyPresenterLayer(canvasObject.transform);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = outputCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = short.MaxValue;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100f;

            canvasObject.AddComponent<GraphicRaycaster>();

            var backgroundObject = new GameObject(BackgroundName);
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            ApplyPresenterLayer(backgroundObject.transform);
            var backgroundRect = backgroundObject.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = Color.black;
            backgroundImage.raycastTarget = false;

            var outputObject = new GameObject(OutputName);
            outputObject.transform.SetParent(canvasObject.transform, false);
            ApplyPresenterLayer(outputObject.transform);
            outputRect = outputObject.AddComponent<RectTransform>();
            outputRect.anchorMin = new Vector2(0.5f, 0.5f);
            outputRect.anchorMax = new Vector2(0.5f, 0.5f);
            outputRect.pivot = new Vector2(0.5f, 0.5f);

            outputImage = outputObject.AddComponent<RawImage>();
            outputImage.raycastTarget = false;
        }

        /// <summary>
        /// Create an orthographic camera that renders the UI canvas above the game screen.

        /// </summary>
        private void EnsureOutputCamera()
        {
            // 1. If the output camera has been created, return directly

            if (outputCamera != null)
            {
                return;
            }

            // 2. Create a camera GameObject and set it as the UI layer

            var cameraObject = new GameObject(CameraName);
            cameraObject.transform.SetParent(transform, false);
            ApplyPresenterLayer(cameraObject.transform);

            // 3. Configure it as an orthographic camera, which only renders the UI layer. The highest depth must be on the top layer.

            outputCamera = cameraObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.orthographicSize = 1f;
            outputCamera.nearClipPlane = 0.01f;
            outputCamera.farClipPlane = 10f;
            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = Color.black;
            outputCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            outputCamera.depth = short.MaxValue - 1;
            // 4. Turn off HDR and MSAA (these features are not required for pixel style)

            outputCamera.allowHDR = false;
            outputCamera.allowMSAA = false;
            outputCamera.transform.localPosition = new Vector3(0f, 0f, -5f);
        }

        /// <summary>
        /// Create render textures at game fixed resolution and point filtering mode.

        /// </summary>
        private void EnsureRenderTexture()
        {
            // 1. If the rendering texture has been created, return directly

            if (renderTexture != null)
            {
                return;
            }

            // 2. Create a fixed resolution (800x480) rendering texture and use point filtering to keep the pixels clear

            renderTexture = new RenderTexture(mlpConstants.DisplayW, mlpConstants.DisplayH, 24, RenderTextureFormat.ARGB32)
            {
                name = "mlpFixedResolutionRT",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1
            };
            renderTexture.Create();
            // 3. Assign the rendering texture to the RawImage display of the output UI

            outputImage.texture = renderTexture;
        }

        /// <summary>
        /// Recalculate the output size and screen rectangle so that it fits the window and maintains the aspect ratio.

        /// </summary>
        private void RefreshLayout(bool force)
        {
            // 1. Get the current screen size, skip if it is the same as last time and there is no forced refresh

            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            // 2. Record the current screen size for next comparison

            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;

            // 3. Calculate the scaling ratio: Take the smaller one of width and height to ensure that the game screen is fully displayed and not deformed.

            var scale = Mathf.Min(
                screenWidth / (float)mlpConstants.DisplayW,
                screenHeight / (float)mlpConstants.DisplayH);
            // 4. Calculate the actual pixel size of the output area (aligned by scaled integer pixels)

            var width = Mathf.Round(mlpConstants.DisplayW * scale);
            var height = Mathf.Round(mlpConstants.DisplayH * scale);
            // 5. Set the size of output UI elements

            outputRect.sizeDelta = new Vector2(width, height);
            // 6. Calculate the rectangular position of the output area on the screen (displayed in the center, and the excess part is a black border)

            outputScreenRect = new Rect(
                (screenWidth - width) * 0.5f,
                (screenHeight - height) * 0.5f,
                width,
                height);
        }

        /// <summary>
        /// Recursively sets all children of the Transform to the UI layer.

        /// </summary>
        private static void ApplyPresenterLayer(Transform root)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                return;
            }

            root.gameObject.layer = uiLayer;
            for (var i = 0; i < root.childCount; i++)
            {
                ApplyPresenterLayer(root.GetChild(i));
            }
        }

        /// <summary>
        /// Restore the source camera to its original settings and clear references.

        /// </summary>
        private void DetachCurrentCamera()
        {
            // 1. If there is no connected source camera, return directly

            if (sourceCamera == null)
            {
                return;
            }

            // 2. Clear the render target of the source camera (no longer output to the render texture)

            sourceCamera.targetTexture = null;
            // 3. Restore HDR and MSAA settings to their original values before connection

            sourceCamera.allowHDR = originalAllowHdr;
            sourceCamera.allowMSAA = originalAllowMsaa;
            // 4. Clear the reference and mark it as unconfigured

            sourceCamera = null;
            configured = false;
        }

        /// <summary>
        /// Show or hide the render canvas and output camera.

        /// </summary>
        private void SetPresenterVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }

            if (outputCamera != null)
            {
                outputCamera.enabled = visible;
            }
        }

        /// <summary>
        /// Maps screen coordinates to game pixels, returning false if the position is outside the output area.
        /// </summary>
        private bool TryMapScreenToGamePixelInternal(Vector2 screenPosition, out Vector2 gamePixel)
        {
            // 1. Check whether the renderer has been configured and whether the output area is valid

            if (!configured || outputRect == null || outputScreenRect.width <= 0f || outputScreenRect.height <= 0f)
            {
                gamePixel = default;
                return false;
            }

            // 2. If the mouse is outside the output area (game screen), return false (on the black border)

            if (!outputScreenRect.Contains(screenPosition))
            {
                gamePixel = default;
                return false;
            }

            // 3. Normalize screen coordinates to the 0-1 range
            var normalizedX = Mathf.Clamp01((screenPosition.x - outputScreenRect.xMin) / outputScreenRect.width);
            var normalizedY = Mathf.Clamp01((screenPosition.y - outputScreenRect.yMin) / outputScreenRect.height);
            // 4. Convert to game logical pixel coordinates (Y axis needs to be flipped because screen Y is up and game Y is down)
            var logicalHeight = mlpConstants.GameH / mlpConstants.RenderScale;
            gamePixel = new Vector2(
                normalizedX * mlpConstants.Width,
                (1f - normalizedY) * logicalHeight);
            return true;
        }
    }
}
