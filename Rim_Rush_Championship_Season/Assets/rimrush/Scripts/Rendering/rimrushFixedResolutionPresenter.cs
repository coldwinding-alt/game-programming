// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushFixedResolutionPresenter 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using UnityEngine;
using UnityEngine.UI;

namespace rimrush
{
    public sealed class rimrushFixedResolutionPresenter : MonoBehaviour
    {
        private const string CanvasName = "rimrushFixedResolutionCanvas";
        private const string CameraName = "rimrushFixedResolutionCamera";
        private const string BackgroundName = "rimrushFixedResolutionBackground";
        private const string OutputName = "rimrushFixedResolutionOutput";
        private static rimrushFixedResolutionPresenter activePresenter;

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
        /// Executes Attach for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="camera">Input value used by this step of the workflow.</param>
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
        /// Executes Detach for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Try Map Screen To Game Pixel for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="screenPosition">Input value used by this step of the workflow.</param>
        /// <param name="gamePixel">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Late Update for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes On Destroy for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Ensure Canvas for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Ensure Output Camera for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Ensure Render Texture for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void EnsureRenderTexture()
        {
            if (renderTexture != null)
            {
                return;
            }

            renderTexture = new RenderTexture(rimrushConstants.DisplayW, rimrushConstants.DisplayH, 24, RenderTextureFormat.ARGB32)
            {
                name = "rimrushFixedResolutionRT",
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
        /// Executes Refresh Layout for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="force">Input value used by this step of the workflow.</param>
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
                screenWidth / (float)rimrushConstants.DisplayW,
                screenHeight / (float)rimrushConstants.DisplayH);
            var width = Mathf.Round(rimrushConstants.DisplayW * scale);
            var height = Mathf.Round(rimrushConstants.DisplayH * scale);
            outputRect.sizeDelta = new Vector2(width, height);
            outputScreenRect = new Rect(
                (screenWidth - width) * 0.5f,
                (screenHeight - height) * 0.5f,
                width,
                height);
        }

        /// <summary>
        /// Executes Apply Presenter Layer for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="root">Input value used by this step of the workflow.</param>
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
        /// Executes Detach Current Camera for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Set Presenter Visible for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="visible">Input value used by this step of the workflow.</param>
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
        /// Executes Try Map Screen To Game Pixel Internal for the rimrushFixedResolutionPresenter workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="screenPosition">Input value used by this step of the workflow.</param>
        /// <param name="gamePixel">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            var logicalHeight = rimrushConstants.GameH / rimrushConstants.RenderScale;
            gamePixel = new Vector2(
                normalizedX * rimrushConstants.Width,
                (1f - normalizedY) * logicalHeight);
            return true;
        }
    }
}
