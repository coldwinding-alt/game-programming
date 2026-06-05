// 固定分辨率画面适配器 / 让游戏始终保持 800x480 的像素风格画面，不管窗口或屏幕多大。自动计算黑边区域和缩放比例，保证画面不变形、不模糊，同时正确转换鼠标坐标到游戏内的像素位置。

using UnityEngine;
using UnityEngine.UI;

namespace mlp
{
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
        /// Hook up a source camera so its output renders at the fixed resolution.
        /// </summary>
        public void Attach(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            if (sourceCamera == camera && configured)
            {
                RefreshLayout(force: true);
                return;
            }

            DetachCurrentCamera();
            sourceCamera = camera;
            originalAllowHdr = sourceCamera.allowHDR;
            originalAllowMsaa = sourceCamera.allowMSAA;
            sourceCamera.allowHDR = false;
            sourceCamera.allowMSAA = false;

            EnsureOutputCamera();
            EnsureCanvas();
            EnsureRenderTexture();
            SetPresenterVisible(true);
            sourceCamera.targetTexture = renderTexture;
            configured = true;
            activePresenter = this;
            RefreshLayout(force: true);
        }

        /// <summary>
        /// Disconnect the current source camera and hide the presenter.
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
        /// Convert a screen-space position to the game's fixed-resolution pixel coordinates.
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
        /// Refresh the layout each frame in case the screen size changed.
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
        /// Clean up the render texture, canvas, and output camera when this component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            DetachCurrentCamera();
            if (renderTexture != null)
            {
                if (renderTexture.IsCreated())
                {
                    renderTexture.Release();
                }

                Destroy(renderTexture);
                renderTexture = null;
            }

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }

            if (outputCamera != null)
            {
                Destroy(outputCamera.gameObject);
                outputCamera = null;
            }

            if (activePresenter == this)
            {
                activePresenter = null;
            }
        }

        /// <summary>
        /// Create the UI canvas, background, and output RawImage if they don't exist yet.
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
        /// Create the orthographic camera that renders the UI canvas on top of the game.
        /// </summary>
        private void EnsureOutputCamera()
        {
            if (outputCamera != null)
            {
                return;
            }

            var cameraObject = new GameObject(CameraName);
            cameraObject.transform.SetParent(transform, false);
            ApplyPresenterLayer(cameraObject.transform);

            outputCamera = cameraObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.orthographicSize = 1f;
            outputCamera.nearClipPlane = 0.01f;
            outputCamera.farClipPlane = 10f;
            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = Color.black;
            outputCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            outputCamera.depth = short.MaxValue - 1;
            outputCamera.allowHDR = false;
            outputCamera.allowMSAA = false;
            outputCamera.transform.localPosition = new Vector3(0f, 0f, -5f);
        }

        /// <summary>
        /// Create the render texture at the game's fixed resolution with point filtering.
        /// </summary>
        private void EnsureRenderTexture()
        {
            if (renderTexture != null)
            {
                return;
            }

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
            outputImage.texture = renderTexture;
        }

        /// <summary>
        /// Recalculate the output size and screen rect to fit the window while keeping the aspect ratio.
        /// </summary>
        private void RefreshLayout(bool force)
        {
            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;

            var scale = Mathf.Min(
                screenWidth / (float)mlpConstants.DisplayW,
                screenHeight / (float)mlpConstants.DisplayH);
            var width = Mathf.Round(mlpConstants.DisplayW * scale);
            var height = Mathf.Round(mlpConstants.DisplayH * scale);
            outputRect.sizeDelta = new Vector2(width, height);
            outputScreenRect = new Rect(
                (screenWidth - width) * 0.5f,
                (screenHeight - height) * 0.5f,
                width,
                height);
        }

        /// <summary>
        /// Recursively set all children of a transform to the UI layer.
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
        /// Restore the source camera to its original settings and clear the reference.
        /// </summary>
        private void DetachCurrentCamera()
        {
            if (sourceCamera == null)
            {
                return;
            }

            sourceCamera.targetTexture = null;
            sourceCamera.allowHDR = originalAllowHdr;
            sourceCamera.allowMSAA = originalAllowMsaa;
            sourceCamera = null;
            configured = false;
        }

        /// <summary>
        /// Show or hide the presenter canvas and output camera.
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
        /// Map a screen position to game pixels, returning false if the position is outside the output area.
        /// </summary>
        private bool TryMapScreenToGamePixelInternal(Vector2 screenPosition, out Vector2 gamePixel)
        {
            if (!configured || outputRect == null || outputScreenRect.width <= 0f || outputScreenRect.height <= 0f)
            {
                gamePixel = default;
                return false;
            }

            if (!outputScreenRect.Contains(screenPosition))
            {
                gamePixel = default;
                return false;
            }

            var normalizedX = Mathf.Clamp01((screenPosition.x - outputScreenRect.xMin) / outputScreenRect.width);
            var normalizedY = Mathf.Clamp01((screenPosition.y - outputScreenRect.yMin) / outputScreenRect.height);
            var logicalHeight = mlpConstants.GameH / mlpConstants.RenderScale;
            gamePixel = new Vector2(
                normalizedX * mlpConstants.Width,
                (1f - normalizedY) * logicalHeight);
            return true;
        }
    }
}
