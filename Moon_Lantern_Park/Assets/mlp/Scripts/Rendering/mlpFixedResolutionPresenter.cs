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
        /// 接入源相机，使其输出以固定分辨率渲染。
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
        /// 断开当前源相机并隐藏呈现器。
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
        /// 将屏幕空间坐标转换为游戏固定分辨率的像素坐标。
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
        /// 每帧刷新布局，以防屏幕尺寸发生变化。
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
        /// 组件销毁时清理渲染纹理、画布和输出相机。
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
        /// 如果尚未创建，则构建 UI 画布、背景和输出 RawImage。
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
        /// 创建正交相机，用于在游戏画面上方渲染 UI 画布。
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
        /// 以游戏固定分辨率和点过滤模式创建渲染纹理。
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
        /// 重新计算输出尺寸和屏幕矩形，使其适配窗口并保持宽高比。
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
        /// 递归地将 Transform 的所有子物体设置到 UI 层。
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
        /// 将源相机恢复到原始设置并清除引用。
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
        /// 显示或隐藏呈现器画布和输出相机。
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
        /// 将屏幕坐标映射到游戏像素，如果位置在输出区域外则返回 false。
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
