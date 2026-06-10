// 固定分辨率画面适配器 / 让游戏始终保持 800x480 的像素风格画面，不管窗口或屏幕多大。自动计算黑边区域和缩放比例，保证画面不变形、不模糊，同时正确转换鼠标坐标到游戏内的像素位置。

using UnityEngine;
using UnityEngine.UI;

namespace mlp
{
    /// <summary>
    /// 固定分辨率画面适配器：让游戏始终保持 800x480 的像素风格画面，不管窗口多大。自动计算黑边和缩放，保证画面不变形。
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
        /// 接入源相机，使其输出以固定分辨率渲染。
        /// </summary>
        public void Attach(Camera camera)
        {
            // 1. 如果传入的相机为空，直接退出
            if (camera == null)
            {
                return;
            }

            // 2. 如果已经接入了同一个相机，只需刷新布局即可
            if (sourceCamera == camera && configured)
            {
                RefreshLayout(force: true);
                return;
            }

            // 3. 断开之前连接的相机（如果有的话）
            DetachCurrentCamera();
            // 4. 保存源相机引用，关闭 HDR 和 MSAA（像素风格不需要这些特性）
            sourceCamera = camera;
            originalAllowHdr = sourceCamera.allowHDR;
            originalAllowMsaa = sourceCamera.allowMSAA;
            sourceCamera.allowHDR = false;
            sourceCamera.allowMSAA = false;

            // 5. 创建输出相机（用来在画面上方渲染 UI 画布）、UI 画布和渲染纹理
            EnsureOutputCamera();
            EnsureCanvas();
            EnsureRenderTexture();
            // 6. 显示呈现器，将源相机的渲染目标设为固定分辨率的渲染纹理
            SetPresenterVisible(true);
            sourceCamera.targetTexture = renderTexture;
            // 7. 标记为已配置，注册为全局活跃呈现器，强制刷新一次布局
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
            // 1. 恢复源相机的原始设置并断开连接
            DetachCurrentCamera();
            // 2. 释放并销毁渲染纹理（释放 GPU 内存）
            if (renderTexture != null)
            {
                if (renderTexture.IsCreated())
                {
                    renderTexture.Release();
                }

                Destroy(renderTexture);
                renderTexture = null;
            }

            // 3. 销毁 UI 画布
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }

            // 4. 销毁输出相机
            if (outputCamera != null)
            {
                Destroy(outputCamera.gameObject);
                outputCamera = null;
            }

            // 5. 清除全局活跃呈现器引用
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
            // 1. 如果输出相机已创建，直接返回
            if (outputCamera != null)
            {
                return;
            }

            // 2. 创建相机 GameObject，设置为 UI 层
            var cameraObject = new GameObject(CameraName);
            cameraObject.transform.SetParent(transform, false);
            ApplyPresenterLayer(cameraObject.transform);

            // 3. 配置为正交相机，只渲染 UI 层，深度最高确保在最上层
            outputCamera = cameraObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.orthographicSize = 1f;
            outputCamera.nearClipPlane = 0.01f;
            outputCamera.farClipPlane = 10f;
            outputCamera.clearFlags = CameraClearFlags.SolidColor;
            outputCamera.backgroundColor = Color.black;
            outputCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            outputCamera.depth = short.MaxValue - 1;
            // 4. 关闭 HDR 和 MSAA（像素风格不需要这些特性）
            outputCamera.allowHDR = false;
            outputCamera.allowMSAA = false;
            outputCamera.transform.localPosition = new Vector3(0f, 0f, -5f);
        }

        /// <summary>
        /// 以游戏固定分辨率和点过滤模式创建渲染纹理。
        /// </summary>
        private void EnsureRenderTexture()
        {
            // 1. 如果渲染纹理已创建，直接返回
            if (renderTexture != null)
            {
                return;
            }

            // 2. 创建固定分辨率（800x480）的渲染纹理，使用点过滤保持像素清晰
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
            // 3. 将渲染纹理赋给输出 UI 的 RawImage 显示
            outputImage.texture = renderTexture;
        }

        /// <summary>
        /// 重新计算输出尺寸和屏幕矩形，使其适配窗口并保持宽高比。
        /// </summary>
        private void RefreshLayout(bool force)
        {
            // 1. 获取当前屏幕尺寸，如果和上次相同且非强制刷新则跳过
            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            // 2. 记录当前屏幕尺寸，用于下次比较
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;

            // 3. 计算缩放比例：取宽高中较小的那个，确保游戏画面完整显示且不变形
            var scale = Mathf.Min(
                screenWidth / (float)mlpConstants.DisplayW,
                screenHeight / (float)mlpConstants.DisplayH);
            // 4. 计算输出区域的实际像素大小（按缩放后的整数像素对齐）
            var width = Mathf.Round(mlpConstants.DisplayW * scale);
            var height = Mathf.Round(mlpConstants.DisplayH * scale);
            // 5. 设置输出 UI 元素的大小
            outputRect.sizeDelta = new Vector2(width, height);
            // 6. 计算输出区域在屏幕上的矩形位置（居中显示，多余部分为黑边）
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
            // 1. 如果没有连接的源相机，直接返回
            if (sourceCamera == null)
            {
                return;
            }

            // 2. 清除源相机的渲染目标（不再输出到渲染纹理）
            sourceCamera.targetTexture = null;
            // 3. 恢复 HDR 和 MSAA 设置为连接前的原始值
            sourceCamera.allowHDR = originalAllowHdr;
            sourceCamera.allowMSAA = originalAllowMsaa;
            // 4. 清除引用，标记为未配置
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
            // 1. 检查呈现器是否已配置，输出区域是否有效
            if (!configured || outputRect == null || outputScreenRect.width <= 0f || outputScreenRect.height <= 0f)
            {
                gamePixel = default;
                return false;
            }

            // 2. 如果鼠标在输出区域（游戏画面）之外，返回 false（在黑边上）
            if (!outputScreenRect.Contains(screenPosition))
            {
                gamePixel = default;
                return false;
            }

            // 3. 将屏幕坐标归一化到 0-1 范围
            var normalizedX = Mathf.Clamp01((screenPosition.x - outputScreenRect.xMin) / outputScreenRect.width);
            var normalizedY = Mathf.Clamp01((screenPosition.y - outputScreenRect.yMin) / outputScreenRect.height);
            // 4. 转换为游戏逻辑像素坐标（Y 轴需要翻转，因为屏幕 Y 向上而游戏 Y 向下）
            var logicalHeight = mlpConstants.GameH / mlpConstants.RenderScale;
            gamePixel = new Vector2(
                normalizedX * mlpConstants.Width,
                (1f - normalizedY) * logicalHeight);
            return true;
        }
    }
}
