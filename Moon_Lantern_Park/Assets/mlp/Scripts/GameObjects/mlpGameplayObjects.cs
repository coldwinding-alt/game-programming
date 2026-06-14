// 文件作用：游戏场景物体管理（球场、篮筐、篮球、传送特效、护盾、技能特效）
// 概括：创建和管理比赛中所有可见的游戏物体：球场背景和灯光、篮筐和篮网、篮球及其物理运动、传送门特效、护盾特效、技能激活特效。这个文件非常大，涵盖了比赛中大部分视觉元素的创建和更新逻辑。

// 类名                说明
// ──────────────────────────────────────────────
// mlpArenaObject       球场对象 — 管理场地视觉、雾风效果 (FogWind)、边界碰撞
// mlpBasketObject      篮筐对象 — 篮筐动画、网兜摆动、传感器碰撞(进球检测)
// mlpBallObject        篮球对象 — 投篮飞行、弹跳、被抢断/被盖帽后的物理、入篮检测
// mlpTeleportFx        传送特效 — 瞬移类技能的视觉粒子效果
// mlpShieldObject      护盾对象 — 篮筐护盾技能，可阻挡飞来的篮球
// mlpPlayerSkillFx     技能光效 — 球员技能的发光/粒子视觉反馈
// mlpPlayerObject      球员对象 — 核心类，管理球员全部状态（移动、跳跃、投篮、扣篮、抢断、盖帽、AI、技能等）


using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 比赛精灵加载器：从图集中加载篮球、球场、篮筐等比赛用的图片精灵，带缓存避免重复加载。
    /// </summary>
    public static class mlpGameplaySpriteLoader
    {
        // 精灵缓存：按资源路径和锚点缓存已创建的 Sprite，避免重复加载。
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 加载主题篮球的精灵，如果该主题没有专属资源则回退到默认图集。
        /// </summary>
        /// <param name="theme">要查找的篮球视觉主题</param>
        /// <param name="anchorX">精灵水平锚点，范围 0-1</param>
        /// <param name="anchorY">精灵垂直锚点，范围 0-1</param>
        /// <returns>加载到的精灵，未找到时返回 null。</returns>
        public static Sprite LoadBallThemeSprite(mlpBallTheme theme, float anchorX, float anchorY)
        {
            var resourcePath = mlpAssets.Images.BallTheme(theme);
            return string.IsNullOrEmpty(resourcePath)
                ? null
                : LoadGameplaySprite(resourcePath, anchorX, anchorY);
        }

        public static Sprite LoadMatchBallSprite(mlpBallTheme theme, float anchorX, float anchorY)
        {
            return LoadBallThemeSprite(theme, anchorX, anchorY) ??
                   mlpAtlasCache.Instance.Gameplay.Sprite("BallMC0000", anchorX, anchorY);
        }

        /// <summary>
        /// 从 Resources 加载游戏精灵，直接资源路径缺失时回退到图集查找。
        /// </summary>
        /// <param name="resourcePath">Images 文件夹下的资源路径</param>
        /// <param name="anchorX">精灵水平锚点，范围 0-1</param>
        /// <param name="anchorY">精灵垂直锚点，范围 0-1</param>
        /// <param name="fallbackAtlas">直接资源路径缺失时搜索的图集</param>
        /// <param name="fallbackFrame">用作回退精灵的图集帧名称</param>
        /// <returns>加载到的精灵，未找到时返回 null。</returns>
        public static Sprite LoadGameplaySprite(
            string resourcePath,
            float anchorX,
            float anchorY,
            mlpAtlas fallbackAtlas = null,
            string fallbackFrame = null)
        {
            var direct = LoadImageSprite(resourcePath, anchorX, anchorY);
            if (direct != null)
            {
                return direct;
            }

            return fallbackAtlas != null && !string.IsNullOrEmpty(fallbackFrame)
                ? fallbackAtlas.Sprite(fallbackFrame, anchorX, anchorY)
                : null;
        }

        /// <summary>
        /// 从 Resources 加载纹理并创建 Sprite，缓存结果以便重复请求时立即返回。
        /// </summary>
        /// <param name="resourcePath">Images 文件夹下的资源路径</param>
        /// <param name="anchorX">精灵水平锚点，范围 0-1</param>
        /// <param name="anchorY">精灵垂直锚点，范围 0-1</param>
        /// <returns>创建的精灵，加载失败时返回 null。</returns>
        public static Sprite LoadImageSprite(string resourcePath, float anchorX, float anchorY)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            var cacheKey = $"{resourcePath}|{anchorX:0.###}|{anchorY:0.###}";
            if (SpriteCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var texture = Resources.Load<Texture2D>(mlpAssets.Images.ResourcePath(resourcePath));
            if (texture == null)
            {
                return null;
            }

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            // 使主题篮球在比赛中旋转时保持与原始图集篮球边界对齐。
            var sprite = Sprite.Create(texture, rect, new Vector2(anchorX, 1f - anchorY), 1f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// 球场对象：创建和管理比赛场地的背景图片、灯光效果和视觉装饰。
    /// </summary>
    public sealed class mlpArenaObject
    {
        // 球场逻辑宽度：用于把背景图缩放到比赛使用的逻辑尺寸。
        private const float ArenaLogicalWidth = 1398f;
        // 球场逻辑高度：用于把背景图缩放到比赛使用的逻辑尺寸。
        private const float ArenaLogicalHeight = 480f;
        // 背景风雾骨骼动画资源名。
        private const string FogWindArmatureName = "dbanims/backwind_01";
        // 三层风雾特效的基础位置。
        private static readonly Vector2[] FogWindLayerPositions =
        {
            new Vector2(168f, 172f),
            new Vector2(408f, 136f),
            new Vector2(652f, 184f)
        };
        // 三层风雾特效的基础缩放。
        private static readonly float[] FogWindLayerScales = { 1.12f, 1.3f, 1.06f };
        // 三层风雾特效的渲染层级。
        private static readonly int[] FogWindLayerSortingOrders = { 1, 2, 3 };
        // 三层风雾特效的透明度偏移。
        private static readonly float[] FogWindLayerAlphaBiases = { 0.82f, 1f, 0.9f };

        private sealed class FogWindLayer
        {
            // 该层风雾的基础位置。
            public Vector2 BasePosition;
            // 该层风雾的根节点。
            public GameObject Root;
            // 该层风雾的骨骼动画对象。
            public DBLiteArmature Armature;
            // 该层风雾的基础缩放。
            public float BaseScale;
            // 该层风雾的渲染顺序基准。
            public int SortingOrder;
            // 该层风雾的透明度偏移。
            public float AlphaBias;
        }

        // 风雾特效根节点。
        private readonly GameObject fogWindFxRoot;
        // 三层风雾实例数组。
        private readonly FogWindLayer[] fogWindLayers = new FogWindLayer[FogWindLayerPositions.Length];

        // 球场根对象，外部可直接挂接到场景中。
        public GameObject Graphic { get; }

        /// <summary>
        /// 创建球场背景精灵并缩放到逻辑比赛尺寸。
        /// </summary>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpArenaObject(Transform parent)
        {
            // 1. 创建球场背景 GameObject 并挂到场景父节点下
            Graphic = new GameObject("ArenaObject");
            Graphic.transform.SetParent(parent, false);

            // 2. 添加 SpriteRenderer 并加载球场背景精灵（优先独立资源，回退图集帧）
            var renderer = Graphic.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.ArenaBackdrop,
                0f,
                0f,
                mlpAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000");

            // 3. 设置渲染排序层级（0 = 最底层，所有其他物体都在其上方绘制）
            renderer.sortingOrder = 0;

            // 4. 将球场定位到像素对齐的世界坐标（左侧偏移 -299 像素，Y = 0）
            mlpRender.ApplyPixelTransform(Graphic.transform, -299f, 0f);

            // 5. 按逻辑比赛尺寸（1398×480）缩放，使高清素材与旧版图集帧保持一致
            ApplyArenaLogicalScale(Graphic.transform, renderer.sprite);

            fogWindFxRoot = new GameObject("ArenaFogWindFx");
            fogWindFxRoot.transform.SetParent(parent, false);
            fogWindFxRoot.SetActive(false);
            CreateFogWindLayers();
        }

        internal void UpdateFogWindFx(bool active, float signedWave, float gustStrength)
        {
            if (fogWindFxRoot == null)
            {
                return;
            }

            if (!active)
            {
                if (fogWindFxRoot.activeSelf)
                {
                    fogWindFxRoot.SetActive(false);
                }

                return;
            }

            if (!fogWindFxRoot.activeSelf)
            {
                fogWindFxRoot.SetActive(true);
            }

            var clampedStrength = Mathf.Clamp01(gustStrength);
            for (var i = 0; i < fogWindLayers.Length; i++)
            {
                ApplyFogWindLayer(fogWindLayers[i], i, signedWave, clampedStrength);
            }
        }

        /// <summary>
        /// 将高分辨率独立球场素材保持在与旧版图集帧相同的逻辑比赛尺寸。
        /// </summary>
        /// <param name="transform">要缩放的 Transform</param>
        /// <param name="sprite">像素尺寸驱动缩放的精灵</param>
        private static void ApplyArenaLogicalScale(Transform transform, Sprite sprite)
        {
            if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
            {
                return;
            }

            var baseScale = mlpConstants.UnitsPerPixel;
            transform.localScale = new Vector3(
                baseScale * ArenaLogicalWidth / sprite.rect.width,
                baseScale * ArenaLogicalHeight / sprite.rect.height,
                1f);
        }

        private void CreateFogWindLayers()
        {
            for (var i = 0; i < fogWindLayers.Length; i++)
            {
                var layer = new FogWindLayer
                {
                    BasePosition = FogWindLayerPositions[i],
                    BaseScale = FogWindLayerScales[i],
                    SortingOrder = FogWindLayerSortingOrders[i],
                    AlphaBias = FogWindLayerAlphaBiases[i]
                };

                layer.Root = new GameObject($"FogWindLayer_{i}");
                layer.Root.transform.SetParent(fogWindFxRoot.transform, false);
                mlpRender.ApplyPixelTransform(layer.Root.transform, layer.BasePosition.x, layer.BasePosition.y, 0.01f + i * 0.001f);

                layer.Armature = DBLiteFactory.Instance.BuildArmature(FogWindArmatureName, $"FogWindArmature_{i}");
                if (layer.Armature != null)
                {
                    layer.Armature.transform.SetParent(layer.Root.transform, false);
                    layer.Armature.transform.localPosition = Vector3.zero;
                    layer.Armature.transform.localScale = new Vector3(
                        mlpConstants.PixelPerfectCharacterScale * layer.BaseScale,
                        mlpConstants.PixelPerfectCharacterScale * layer.BaseScale,
                        1f);
                }

                fogWindLayers[i] = layer;
            }
        }

        private static void ApplyFogWindLayer(FogWindLayer layer, int layerIndex, float signedWave, float gustStrength)
        {
            if (layer?.Root == null)
            {
                return;
            }

            var direction = signedWave >= 0f ? 1f : -1f;
            var waveAbs = Mathf.Abs(signedWave);
            var swayX = signedWave * (16f + layerIndex * 4f);
            var swayY = Mathf.Sin(Time.time * (1.9f + layerIndex * 0.21f) + layerIndex * 0.7f) * (2f + gustStrength * 4f);
            mlpRender.ApplyPixelTransform(
                layer.Root.transform,
                layer.BasePosition.x + swayX,
                layer.BasePosition.y + swayY,
                0.01f + layerIndex * 0.001f);

            if (layer.Armature == null)
            {
                return;
            }

            var widthPulse = 0.94f + gustStrength * 0.18f + waveAbs * 0.06f;
            var heightPulse = 0.9f + gustStrength * 0.12f;
            layer.Armature.transform.localScale = new Vector3(
                mlpConstants.PixelPerfectCharacterScale * layer.BaseScale * widthPulse * direction,
                mlpConstants.PixelPerfectCharacterScale * layer.BaseScale * heightPulse,
                1f);

            ApplyFogWindRenderers(layer.Armature, layer.SortingOrder, (0.2f + gustStrength * 0.42f + waveAbs * 0.08f) * layer.AlphaBias);
        }

        private static void ApplyFogWindRenderers(DBLiteArmature armature, int sortingOrderBase, float alpha)
        {
            if (armature == null)
            {
                return;
            }

            var renderers = armature.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var minSortingOrder = int.MaxValue;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.sortingOrder < minSortingOrder)
                {
                    minSortingOrder = renderer.sortingOrder;
                }
            }

            if (minSortingOrder == int.MaxValue)
            {
                minSortingOrder = sortingOrderBase;
            }

            var sortingOffset = sortingOrderBase - minSortingOrder;
            var tint = new Color(0.84f, 0.95f, 1f, Mathf.Clamp01(alpha));
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.sortingOrder += sortingOffset;
                renderer.color = tint;
                renderer.enabled = tint.a > 0.001f;
            }
        }
    }

    /// <summary>
    /// 篮筐对象：管理篮筐的物理位置、篮网动画、得分传感器（检测球是否穿过篮筐）。
    /// </summary>
    public sealed class mlpBasketObject
    {
        // 篮网线条集合，用于模拟篮网摆动。
        private readonly List<LineRenderer> netLines = new List<LineRenderer>();
        // 篮筐所在球场侧，-1 表示左侧，1 表示右侧。
        private readonly int side;
        // 篮筐根节点。
        private GameObject graphic;
        // 篮筐前沿遮挡层，用来挡住扣篮时的篮球。
        private GameObject frontEar;
        // 篮网摆动脉冲强度。
        private float netPulse;

        // 球场侧，-1 左侧，1 右侧。
        public int Side => side;
        // 篮筐中心 X 坐标。
        public float Center { get; }
        // 篮筐高度，直接读取场景数据。
        public float Height => mlpObjectsData.BasketHeight;

        /// <summary>
        /// 创建球场一侧的篮筐，包括篮圈、前沿耳和篮网。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpBasketObject(int side, Transform parent)
        {
            this.side = side;
            Center = side == -1 ? mlpObjectsData.BasketCenter : mlpObjectsData.BasketCenter2;
            CreateGraphic(parent);
        }

        /// <summary>
        /// 每帧更新篮网动画，使摆动随时间衰减。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void Update(float dt)
        {
            netPulse = Mathf.Max(0f, netPulse - dt * 2f);
            UpdateNetLines();
        }

        /// <summary>
        /// 触发篮网摆动脉冲，使篮球穿过时篮网产生波纹效果。
        /// </summary>
        public void HitNet()
        {
            netPulse = 1f;
        }

        /// <summary>
        /// 隐藏前沿耳图形，避免扣篮时遮挡篮球。
        /// </summary>
        public void HideEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(false);
            }
        }

        /// <summary>
        /// 扣篮或特殊技能完成后恢复前沿耳图形。
        /// </summary>
        public void ShowEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(true);
            }
        }

        /// <summary>
        /// 构建篮筐的所有视觉子对象：篮圈精灵、前沿耳覆盖层和十条 LineRenderer 篮网线。
        /// </summary>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        private void CreateGraphic(Transform parent)
        {
            // 1. 创建篮筐根节点并挂到场景父节点下
            graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(parent, false);

            // 2. 将篮筐定位到像素对齐的世界坐标（篮筐中心 X，篮筐高度 Y）
            mlpRender.ApplyPixelTransform(graphic.transform, Center, mlpObjectsData.BasketHeight, 0.05f);

            // 3. 设置缩放（像素完美），右侧篮筐水平翻转
            graphic.transform.localScale = new Vector3(mlpConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), mlpConstants.UnitsPerPixel, 1f);

            // 4. 创建篮圈精灵（锚点 0.7, 0.93 使篮圈对齐篮筐前沿）
            var basket = new GameObject("BasketGraphic");
            basket.transform.SetParent(graphic.transform, false);
            var renderer = basket.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.BasketGraphic,
                0.7f,
                0.93f,
                mlpAtlasCache.Instance.Gameplay,
                "BasketGraphic0000");
            renderer.sortingOrder = 4;

            // 5. 创建前沿耳覆盖层（排序层级 60，遮挡扣篮时的篮球，需要时可隐藏）
            frontEar = new GameObject(side == -1 ? "FrontEarLeft" : "FrontEarRight");
            frontEar.transform.SetParent(parent, false);
            var frontEarRenderer = frontEar.AddComponent<SpriteRenderer>();
            frontEarRenderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.BasketFrontEar,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "FrontEar0000");
            frontEarRenderer.sortingOrder = 60;
            mlpRender.ApplyPixelTransform(frontEar.transform, Center, mlpObjectsData.BasketHeight);
            frontEar.transform.localScale = new Vector3(mlpConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), mlpConstants.UnitsPerPixel, 1f);

            // 6. 创建十条 LineRenderer 篮网线（模拟网格状篮网结构）
            for (var i = 0; i < 10; i++)
            {
                var lineObject = new GameObject($"NetLine{i}");
                lineObject.transform.SetParent(parent, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.018f;
                line.endWidth = 0.018f;
                line.sharedMaterial = mlpSharedMaterialCache.GetSpritesDefault();
                line.startColor = Color.white;
                line.endColor = Color.white;
                line.sortingOrder = 55;
                netLines.Add(line);
            }

            // 7. 初始化篮网线条位置（3 行 × 10 条线段构成网格）
            UpdateNetLines();
        }

        /// <summary>
        /// 重新定位十条篮网 LineRenderer 以模拟摆动的篮网网格。
        /// </summary>
        private void UpdateNetLines()
        {
            // 1. 计算篮网左右边界和中心位置
            var left = Center - mlpObjectsData.BasketRadius + 2f;
            var right = Center + mlpObjectsData.BasketRadius - 2f;
            var middle = Center;
            // 2. 计算篮网顶部起始高度
            var top = mlpObjectsData.BasketHeight + 3f;
            // 3. 计算篮网的摇摆偏移量（随时间和脉冲强度变化）
            var sway = Mathf.Sin(Time.time * 18f) * 5f * netPulse;

            // 4. 定义篮网顶部三个节点的位置
            var pointsTop = new[]
            {
                new Vector2(left, top),
                new Vector2(middle, top),
                new Vector2(right, top)
            };
            // 5. 定义篮网中部三个节点的位置（带摇摆效果）
            var pointsMid = new[]
            {
                new Vector2(left + sway, top + 14f),
                new Vector2(middle - sway * 0.5f, top + 12f),
                new Vector2(right + sway, top + 14f)
            };
            // 6. 定义篮网底部三个节点的位置（带反向摇摆效果）
            var pointsBot = new[]
            {
                new Vector2(left - sway * 0.5f, top + 32f),
                new Vector2(middle + sway, top + 30f),
                new Vector2(right - sway * 0.5f, top + 32f)
            };

            // 7. 设置左侧篮网线条（顶部到中部、中部到底部）
            SetLine(0, pointsTop[0], pointsMid[0]);
            SetLine(1, pointsMid[0], pointsBot[0]);
            // 8. 设置中间篮网线条
            SetLine(2, pointsTop[1], pointsMid[1]);
            SetLine(3, pointsMid[1], pointsBot[1]);
            // 9. 设置右侧篮网线条
            SetLine(4, pointsTop[2], pointsMid[2]);
            SetLine(5, pointsMid[2], pointsBot[2]);
            // 10. 设置横向连接线条（中部和底部）
            SetLine(6, pointsMid[0], pointsMid[1]);
            SetLine(7, pointsMid[2], pointsMid[1]);
            SetLine(8, pointsBot[0], pointsBot[1]);
            SetLine(9, pointsBot[2], pointsBot[1]);
        }

        /// <summary>
        /// 设置单条篮网 LineRenderer 的起始和结束世界坐标。
        /// </summary>
        /// <param name="index">篮网线条渲染器索引（0-9）</param>
        /// <param name="a">线条第一个端点</param>
        /// <param name="b">线条第二个端点</param>
        private void SetLine(int index, Vector2 a, Vector2 b)
        {
            netLines[index].SetPosition(0, mlpConstants.PixelToWorld(a.x, a.y, 0.03f));
            netLines[index].SetPosition(1, mlpConstants.PixelToWorld(b.x, b.y, 0.03f));
        }
    }

    /// <summary>
    /// 篮球对象：管理篮球的全部状态——被持球、投篮飞行、弹跳、入篮、扣篮、被盖帽等，以及物理运动和碰撞检测。
    /// </summary>
    public sealed class mlpBallObject
    {
        // 单次物理子步允许的最大移动距离，避免篮球高速穿透碰撞体。
        private const float MaxSubstepTravel = 8f;
        // 每帧最多拆分成的物理子步数量。
        private const int MaxSubsteps = 8;
        // 篮圈碰撞反弹系数。
        private const float RimRestitution = 0.78f;
        // 篮板碰撞反弹系数。
        private const float BackboardRestitution = 0.82f;
        // 碰撞音效冷却时间，避免连续播放过于密集。
        private const float CollisionSoundCooldownDuration = 0.04f;
        // 保障扣篮得分时额外放宽的 X 容差。
        private const float GuaranteedDunkScoreExtraX = 6f;

        // 篮球视觉节点。
        private readonly GameObject graphic;
        // 篮球阴影节点。
        private readonly GameObject shadow;
        // 游戏核心引用，用于通知计分、抢断等全局逻辑。
        private readonly mlpGameCore gameCore;
        // 上一帧的球位置。
        private Vector2 previousPosition;
        // 下一帧是否仍保持可见。
        private bool visibleNextFrame;
        // 当前是否允许进入得分判定流程。
        private bool canScore;
        // 是否已经穿过上方得分传感器。
        private bool upperSensorPassed;
        // 是否启用必进扣篮得分判定。
        private bool guaranteedDunkScore;
        // 教程中的必进得分标记。
        private bool tutorialGuaranteedScore;
        // 当前已经激活得分判定的进攻方。
        private int scoreArmedSide;
        // 拾球锁定倒计时。
        private float pickupLockTimer;
        // 碰撞音效冷却计时。
        private float collisionSoundCooldown;
        // 是否已从物理系统中移除。
        private bool physicsRemoved;
        // 空接关联的球员对象。
        private mlpPlayerObject alleyOopPlayer;

        // 球的位置。
        public Vector2 Position;
        // 球的速度。
        public Vector2 Velocity;
        // 球所属的场地侧。
        public int Side;
        // 球当前状态字符串，例如 up、inHands、shooting。
        public string State = "up";
        // 最近一次出手时记录的 X 坐标。
        public float LastShotX;
        // 上一帧位置，只读暴露给外部使用。
        public Vector2 PreviousPosition => previousPosition;
        // 是否仍处于比赛物理流程中。
        public bool IsInGame => State != "inHands" && !physicsRemoved;
        // 是否处于可被盖帽的飞行状态。
        public bool IsBlockable => State == "shooting";
        // 是否允许被球员捡起或接住。
        public bool CanBeTakenInHands =>
            pickupLockTimer <= 0f &&
            !physicsRemoved &&
            State != "shooting" &&
            State != "alleyOop" &&
            State != "inHands" &&
            State != "score";

        /// <summary>
        /// 创建篮球精灵及其阴影，然后重置到起始位置。
        /// </summary>
        /// <param name="gameCore">中央游戏逻辑协调器</param>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpBallObject(mlpGameCore gameCore, Transform parent)
        {
            // 1. 保存中央游戏逻辑协调器引用
            this.gameCore = gameCore;

            // 2. 创建篮球精灵 GameObject 并挂到场景父节点下
            graphic = new GameObject("BallObject");
            graphic.transform.SetParent(parent, false);

            // 3. 添加 SpriteRenderer，加载主题篮球精灵（如有）否则回退默认图集帧
            var graphicRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicRenderer.sprite = ResolveBallSprite();
            graphicRenderer.sortingOrder = 50;

            // 4. 将篮球定位到中场起始位置（屏幕中心 X，篮球起始高度 Y）
            mlpRender.ApplyPixelTransform(graphic.transform, mlpConstants.Width2, mlpObjectsData.BallIndentYCenter, 0.2f);

            // 5. 创建篮球阴影 GameObject 并挂到场景父节点下
            shadow = new GameObject("BallShadow");
            shadow.transform.SetParent(parent, false);

            // 6. 加载篮球专用阴影精灵并设置排序层级（3 = 地面层）
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.PlayerShadowBall,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "ShadowMC0002");
            shadowRenderer.sortingOrder = 3;

            // 7. 将阴影定位到地面 Y 坐标，缩放为 0.7 倍使其比球员阴影更小
            mlpRender.ApplyPixelTransform(shadow.transform, mlpConstants.Width2, mlpObjectsData.FloorY, 0.02f);
            shadow.transform.localScale *= 0.7f;

            // 8. 重置篮球状态到中场起始位置，准备新一轮比赛
            Restart();
        }

        /// <summary>
        /// 将篮球重置到中场位置并赋予向上速度，为新一轮做好准备。
        /// </summary>
        public void Restart()
        {
            // 1. 将篮球位置重置到球场中央
            Position = new Vector2(mlpConstants.Width2, mlpObjectsData.BallIndentYCenter);
            // 2. 记录上一帧位置用于物理计算
            previousPosition = Position;
            // 3. 给篮球一个向上的初始速度
            Velocity = new Vector2(0f, mlpObjectsData.BallUpVelocityY);
            // 4. 设置篮球状态为"上升中"
            State = "up";
            // 5. 标记物理未被移除
            physicsRemoved = false;
            // 6. 清除空接相关数据
            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            // 7. 重置得分状态
            ResetScoring(false);
            // 8. 显示篮球和阴影
            Show();
            // 9. 更新篮球视觉位置
            UpdateGraphic();
        }

        /// <summary>
        /// 将篮球瞬间放置在指定位置且速度为零，用于教程中的脚本化时刻。
        /// </summary>
        /// <param name="position">世界坐标</param>
        public void TutorialSnapTo(Vector2 position)
        {
            Position = position;
            previousPosition = position;
            Velocity = Vector2.zero;
            State = "down";
            physicsRemoved = false;
            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            ResetScoring(false);
            Show();
            UpdateGraphic();
        }

        public void TutorialLaunchRebound(Vector2 position, Vector2 velocity, float pickupLock)
        {
            Position = position;
            previousPosition = position;
            Velocity = velocity;
            State = "basket";
            physicsRemoved = false;
            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            ResetScoring(false);
            pickupLockTimer = Mathf.Max(0f, pickupLock);
            Show();
            UpdateGraphic();
            gameCore.NotifyBallOthers();
        }

        public void TutorialLaunchPutbackBounce(int scoringSide, float pickupLock)
        {
            var side = scoringSide == 1 ? 1 : -1;
            var basketX = side == 1 ? mlpObjectsData.BasketCenter : mlpObjectsData.BasketCenter2;
            var nearRimX = basketX + mlpObjectsData.BasketRadius * side;
            Side = side;
            TutorialLaunchRebound(
                new Vector2(nearRimX, mlpObjectsData.BasketHeight - mlpObjectsData.BallRadius - 2f),
                new Vector2(42f * side, 360f),
                pickupLock);
        }

        /// <summary>
        /// 将篮球放入球员手中，隐藏篮球和阴影精灵。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        public void TakeInHands(int side)
        {
            Side = side;
            previousPosition = Position;
            State = "inHands";
            physicsRemoved = false;
            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            ResetScoring(false);
            graphic.SetActive(false);
            shadow.SetActive(false);
        }

        /// <summary>
        /// 以略微向前向下的速度将篮球从球员手中释放。
        /// </summary>
        /// <param name="playerPosition">球员当前的世界坐标</param>
        /// <param name="direction">移动或投掷方向（-1 或 1）</param>
        public void FromHands(Vector2 playerPosition, float direction)
        {
            Position = playerPosition;
            previousPosition = Position;
            Velocity = new Vector2(150f * direction, -100f);
            State = "down";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(false);
            Show();
            gameCore.NotifyBallOthers();
        }

        public void DropFromFreeze(Vector2 playerPosition)
        {
            Position = playerPosition;
            previousPosition = Position;
            Velocity = Vector2.zero;
            State = "down";
            physicsRemoved = false;
            alleyOopPlayer = null;
            pickupLockTimer = CalcPickupLockUntilFloor(playerPosition.y);
            ResetScoring(false);
            Show();
            UpdateGraphic();
            gameCore.NotifyBallOthers();
        }

        /// <summary>
        /// 以抛物线物理将篮球投向篮筐，根据距离和移动速度应用精度偏移。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        /// <param name="x">像素空间中的水平坐标</param>
        /// <param name="y">像素空间中的垂直坐标</param>
        /// <param name="playerVelocityX">投手出手时的水平速度</param>
        /// <param name="accuracy">投篮精度修正值（越低越准）</param>
        public void Shoot(int side, float x, float y, float playerVelocityX, float accuracy)
        {
            // 1. 记录投篮侧、出手位置和上一次投篮 X 坐标
            Side = side;
            Position = new Vector2(x, y);
            previousPosition = Position;
            LastShotX = x;

            // 2. 计算精确投篮的基准抛物线速度（无偏移时命中篮筐中心）
            var baseVelocity = CalcThrowVel(x, y, 0f);

            // 3. 根据距离、高度、跑动速度和精度计算偏移系数
            var distanceToBasket = side == 1 ? x : mlpConstants.Width - x;
            var runningDispersion = Mathf.Abs(playerVelocityX) / mlpObjectsData.PlayerMoveWithBall * 0.1f;
            var dispersion = CalcDispersion(distanceToBasket, y, runningDispersion, accuracy);

            // 4. 根据偏移系数决定最终速度：轻微偏移缩放 X 分量，严重偏移左右偏转
            if (dispersion < 2f)
            {
                Velocity = new Vector2(baseVelocity.x * dispersion, baseVelocity.y);
            }
            else if (Mathf.Approximately(dispersion, 2f))
            {
                Velocity = CalcThrowVel(x, y, 30f * side);
            }
            else
            {
                Velocity = CalcThrowVel(x, y, 30f * -side);
            }

            // 5. 切换到投篮状态，激活得分传感器，显示篮球并播放出手音效
            State = "shooting";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(true);
            scoreArmedSide = side;
            Show();
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh);
        }

        /// <summary>
        /// 播放篮球扣篮轨迹动画；扣篮完成时预先激活得分传感器以便立即计分。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        /// <param name="completed">扣篮动画顺利结束时为 true</param>
        public void Dunk(int side, bool completed)
        {
            // 1. 记录扣篮侧和篮筐中心 X 坐标
            Side = side;
            var basketX = side == 1 ? mlpObjectsData.BasketCenter : mlpObjectsData.BasketCenter2;

            // 2. 设置篮球位置：完成时偏移 17 像素，未完成时在篮筐正上方
            Position = new Vector2(completed ? basketX + 17f * side : basketX, 170f);
            previousPosition = Position;
            LastShotX = Position.x;

            // 3. 设置弹出速度：完成时轻弹，未完成时大力弹飞
            Velocity = completed ? new Vector2(-260f * side, 400f) : new Vector2(-550f * side, 400f);

            // 4. 切换到扣篮状态，激活得分传感器
            State = "dunk";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(true);

            // 5. 标记保证得分（完成的扣篮必定进球）
            guaranteedDunkScore = completed;
            if (completed)
            {
                // 6. 预先激活上传感器，避免子步长检测时"先下后上"误判导致不得分
                upperSensorPassed = true;
                scoreArmedSide = side;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            // 7. 设置拾取锁定计时器，防止扣篮后立即被抢球
            pickupLockTimer = mlpObjectsData.DunkPickupLock;

            // 8. 显示篮球精灵
            Show();
        }

        /// <summary>
        /// 抢断后将篮球击飞，赋予其抢断方向的速度。
        /// </summary>
        /// <param name="playerPosition">球员当前的世界坐标</param>
        /// <param name="distanceFactor">基于抢断距离的倍率</param>
        /// <param name="direction">移动或投掷方向（-1 或 1）</param>
        public void ApplySteal(Vector2 playerPosition, float distanceFactor, int direction)
        {
            Position = playerPosition;
            previousPosition = Position;
            Velocity = new Vector2(
                direction * (mlpObjectsData.BallStealVelocityXBase + distanceFactor * mlpObjectsData.BallStealVelocityXAdd),
                mlpObjectsData.BallStealVelocityY);
            State = "steal";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(false);
            Show();
            UpdateGraphic();
            gameCore.NotifyBallOthers();
        }

        /// <summary>
        /// 通过求解抛物线轨迹方程的落地时间，估算篮球将落在地面的位置。
        /// </summary>
        /// <returns>预测的落地 X 坐标。</returns>
        public float PredictFloorLandingX()
        {
            // 1. 如果篮球不在游戏中，直接返回当前位置（限制在场地内）
            if (!IsInGame)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 2. 如果篮球已经在地面或以下，直接返回当前位置
            if (Position.y >= mlpObjectsData.BallFloorY)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 3. 计算篮球受到的重力加速度
            var gravity = mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass;
            if (gravity <= 0.0001f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 4. 计算篮球到地面的高度差
            var floorDelta = mlpObjectsData.BallFloorY - Position.y;
            // 5. 用抛物线公式求判别式（用于解落地时间）
            var discriminant = Velocity.y * Velocity.y + 2f * gravity * floorDelta;
            if (discriminant <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 6. 解出篮球落地所需的时间
            var timeToFloor = (-Velocity.y + Mathf.Sqrt(discriminant)) / gravity;
            if (timeToFloor <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 7. 根据水平速度和落地时间预测落地的X坐标
            return Mathf.Clamp(Position.x + Velocity.x * timeToFloor, 20f, mlpConstants.Width - 20f);
        }

        private static float CalcPickupLockUntilFloor(float startY)
        {
            var floorDelta = Mathf.Max(0f, mlpObjectsData.BallFloorY - startY);
            var gravity = mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass;
            if (floorDelta <= 0.01f || gravity <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Sqrt(2f * floorDelta / gravity);
        }

        /// <summary>
        /// 盖帽后将篮球弹开，使其方向相对于盖帽者反转。
        /// </summary>
        /// <param name="blocker">执行盖帽的球员</param>
        public void ApplyBlock(mlpPlayerObject blocker)
        {
            // 1. 根据篮球和盖帽者的相对位置确定弹开方向
            var direction = Position.x >= blocker.Position.x ? 1f : -1f;
            // 2. 将篮球归属设为盖帽者一方
            Side = blocker.Side;
            // 3. 记录当前位置用于物理计算
            previousPosition = Position;
            // 4. 给篮球一个随机的弹开速度（向侧上方）
            Velocity = new Vector2(
                direction * (280f + 100f * Random.value),
                -250f - 150f * Random.value);
            // 5. 设置篮球状态为"被盖帽"
            State = "block";
            // 6. 标记物理未被移除
            physicsRemoved = false;
            // 7. 清除空接球员引用
            alleyOopPlayer = null;
            // 8. 保持当前投篮激活状态，使干净的盖帽后篮球仍能穿过
            // 原始篮筐时被传感器链计分。
            gameCore.MatchProcessor.Block(blocker.Side, blocker.IsHuman);
            // 9. 显示篮球
            Show();
            // 10. 更新篮球视觉位置
            UpdateGraphic();
            // 11. 播放盖帽音效
            mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
            // 12. 通知系统盖帽事件
            gameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Block, blocker.Side, blocker.PlayerNo);
            // 13. 通知其他玩家篮球状态变化
            gameCore.NotifyBallOthers();
        }

        /// <summary>
        /// 将篮球沿空接弧线投向指定接球点，由空接球员引用跟踪。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        /// <param name="x">像素空间中的水平坐标</param>
        /// <param name="y">像素空间中的垂直坐标</param>
        /// <param name="player">拥有空接的球员对象</param>
        public void AlleyOop(int side, float x, float y, mlpPlayerObject player)
        {
            // 1. 设置篮球所属阵营
            Side = side;
            // 2. 将篮球放到起始位置
            Position = new Vector2(x, y);
            // 3. 记录上一帧位置
            previousPosition = Position;
            // 4. 记录投篮出手位置的X坐标
            LastShotX = Position.x;
            // 5. 计算空接弧线的速度向量（目标是空接接球点）
            Velocity = CalcVel(
                x,
                y,
                side == 1 ? mlpObjectsData.AlleyOopX : mlpConstants.Width - mlpObjectsData.AlleyOopX,
                mlpObjectsData.AlleyOopY,
                150f);
            // 6. 设置篮球状态为"空接中"
            State = "alleyOop";
            // 7. 记录空接球员引用
            alleyOopPlayer = player;
            // 8. 标记物理未被移除
            physicsRemoved = false;
            // 9. 重置得分状态
            ResetScoring(false);
            // 10. 显示篮球
            Show();
            // 11. 标记当前为空接状态
            gameCore.IsAlleyOop = true;
        }

        /// <summary>
        /// 隐藏篮球并停止其物理模拟，用于过场动画或场景转换期间。
        /// </summary>
        public void RemoveFromPhysics()
        {
            physicsRemoved = true;
            gameCore.IsAlleyOop = false;
            graphic.SetActive(false);
            shadow.SetActive(false);
        }

        /// <summary>
        /// 在篮球被移除后重新启用其物理和可见性。
        /// </summary>
        public void ReturnToPhysics()
        {
            physicsRemoved = false;
            gameCore.IsAlleyOop = false;
            Show();
        }

        /// <summary>
        /// 将篮球从护盾技能弹开，带随机弹跳效果。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        public void OnShieldCollision(int side)
        {
            if (State == "score")
            {
                return;
            }

            Side = side;
            previousPosition = Position;
            Velocity = new Vector2(-side * (200f + 100f * Random.value), -200f - 100f * Random.value);
            State = "basket";
            mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
        }

        /// <summary>
        /// 每帧推进篮球物理：应用重力，对地面/墙壁/篮筐/护盾进行子步碰撞检测，并更新精灵位置。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        /// <param name="basketLeft">左侧篮筐对象</param>
        /// <param name="basketRight">右侧篮筐对象</param>
        public void Update(float dt, mlpBasketObject basketLeft, mlpBasketObject basketRight)
        {
            // 1. 持球状态或物理已移除时跳过更新
            if (State == "inHands" || physicsRemoved)
            {
                return;
            }

            // 2. 递减拾取锁定和碰撞音效冷却计时器
            pickupLockTimer = Mathf.Max(0f, pickupLockTimer - dt);
            collisionSoundCooldown = Mathf.Max(0f, collisionSoundCooldown - dt);

            // 3. 空接弧线到达最高点后，通知球员继续空接传送
            if (alleyOopPlayer != null && Velocity.y > 0f)
            {
                alleyOopPlayer.ContinueAlleyOop();
                alleyOopPlayer = null;
            }

            // 4. 根据篮球状态确定最少子步数（扣篮 5 步，投篮/弹跳/盖帽/空接 3 步）
            var minSubsteps = 1;
            if (State == "dunk")
            {
                minSubsteps = 5;
            }
            else if (State == "shooting" || State == "basket" || State == "block" || State == "alleyOop")
            {
                minSubsteps = 3;
            }

            // 5. 根据速度计算子步数，确保每步移动不超过 8 像素（防穿透），上限 8 步
            var steps = Mathf.Clamp(
                Mathf.Max(minSubsteps, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(Velocity.x), Mathf.Abs(Velocity.y)) * dt / MaxSubstepTravel)),
                minSubsteps,
                MaxSubsteps);
            var stepDt = dt / steps;

            // 6. 逐子步推进物理：重力 → 移动 → 碰撞检测 → 得分检测
            for (var i = 0; i < steps; i++)
            {
                // 6a. 记录上一帧位置（用于碰撞扫描和得分传感器检测）
                previousPosition = Position;

                // 6b. 应用重力并更新位置
                Velocity.y += mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass * stepDt;
                Position += Velocity * stepDt;

                // 6c. 检测球员盖帽
                gameCore.TryBlockBall();

                // 6d. 地面反弹和墙壁反弹
                ResolveFloorBounce();
                ResolveWallBounce();

                // 6e. 篮筐碰撞（篮板、篮圈、得分传感器）和护盾碰撞（空接时不检测）
                if (State != "alleyOop")
                {
                    ResolveBasket(basketLeft, 1);
                    ResolveBasket(basketRight, -1);
                    gameCore.TryShieldBall();
                }

                // 6f. 保证扣篮得分检测（篮球接近篮筐时直接判定进球）
                TryResolveGuaranteedDunkScore(basketLeft, basketRight);
            }

            // 7. 将篮球和阴影精灵移动到当前物理位置
            UpdateGraphic();
        }

        /// <summary>
        /// 使篮球从地面反弹并衰减水平速度。
        /// </summary>
        private void ResolveFloorBounce()
        {
            if (Position.y > mlpObjectsData.BallFloorY)
            {
                Position.y = mlpObjectsData.BallFloorY;
                if (Velocity.y > 0f)
                {
                    Velocity.y = mlpObjectsData.BallBounce;
                    Velocity.x *= 0.86f;
                    State = "bounce";
                    mlpAudio.Instance?.Play(mlpAssets.Sounds.BBounce);
                }
            }
        }

        /// <summary>
        /// 使篮球从左右墙壁边界反弹。
        /// </summary>
        private void ResolveWallBounce()
        {
            if (Position.x < 5f || Position.x > mlpConstants.Width - 5f)
            {
                Position.x = Mathf.Clamp(Position.x, 5f, mlpConstants.Width - 5f);
                Velocity.x *= -0.75f;
            }
        }

        /// <summary>
        /// 检测一侧篮筐的所有碰撞：篮板、左篮圈、右篮圈和得分传感器。
        /// </summary>
        /// <param name="basket">要检测碰撞的篮筐对象</param>
        /// <param name="scoringSide">篮球穿过篮筐时得分的一侧</param>
        private void ResolveBasket(mlpBasketObject basket, int scoringSide)
        {
            if (basket == null)
            {
                return;
            }

            ResolveBackboardCollision(basket);
            ResolveRimCollision(new Vector2(basket.Center - mlpObjectsData.BasketRadius, basket.Height), basket);
            ResolveRimCollision(new Vector2(basket.Center + mlpObjectsData.BasketRadius, basket.Height), basket);
            ProcessScoreSensors(basket, scoringSide);
        }

        /// <summary>
        /// 当篮球到达篮板玻璃区域时使其从篮板玻璃平面反射。
        /// </summary>
        /// <param name="basket">要检测碰撞的篮筐对象</param>
        private void ResolveBackboardCollision(mlpBasketObject basket)
        {
            // 1. 计算篮板玻璃区域的上下边界
            var glassTop = basket.Height + mlpObjectsData.GlassY;
            var glassBottom = glassTop + mlpObjectsData.GlassHeight;
            // 2. 如果篮球不在篮板玻璃的垂直范围内，直接返回
            if (Position.y + mlpObjectsData.BallRadius < glassTop || Position.y - mlpObjectsData.BallRadius > glassBottom)
            {
                return;
            }

            // 3. 处理左侧篮筐的篮板碰撞
            if (basket.Side == -1)
            {
                var planeX = mlpObjectsData.GlassWidth;
                // 4. 检测篮球是否碰到左侧篮板平面
                if (Velocity.x < 0f && Position.x - mlpObjectsData.BallRadius <= planeX)
                {
                    // 5. 将篮球推离篮板表面
                    Position.x = planeX + mlpObjectsData.BallRadius;
                    // 6. 反弹水平速度并乘以恢复系数
                    Velocity.x = Mathf.Abs(Velocity.x) * BackboardRestitution;
                    // 7. 稍微减缓垂直速度模拟摩擦
                    Velocity.y *= 0.97f;
                    // 8. 设置篮球为篮筐区域状态
                    SetBasketState();
                    // 9. 播放篮板碰撞音效
                    PlayBasketSound(2);
                }

                return;
            }

            // 10. 处理右侧篮筐的篮板碰撞
            var rightPlaneX = mlpConstants.Width - mlpObjectsData.GlassWidth;
            // 11. 检测篮球是否碰到右侧篮板平面
            if (Velocity.x > 0f && Position.x + mlpObjectsData.BallRadius >= rightPlaneX)
            {
                // 12. 将篮球推离篮板表面
                Position.x = rightPlaneX - mlpObjectsData.BallRadius;
                // 13. 反弹水平速度并乘以恢复系数
                Velocity.x = -Mathf.Abs(Velocity.x) * BackboardRestitution;
                // 14. 稍微减缓垂直速度模拟摩擦
                Velocity.y *= 0.97f;
                // 15. 设置篮球为篮筐区域状态
                SetBasketState();
                // 16. 播放篮板碰撞音效
                PlayBasketSound(2);
            }
        }

        /// <summary>
        /// 将篮球推出篮圈碰撞圆并对速度应用弹性恢复系数。
        /// </summary>
        /// <param name="rimCenter">像素空间中篮圈碰撞圆的中心</param>
        /// <param name="basket">要检测碰撞的篮筐对象</param>
        private void ResolveRimCollision(Vector2 rimCenter, mlpBasketObject basket)
        {
            // 1. 计算篮球和篮圈的半径之和
            var combinedRadius = mlpObjectsData.BallRadius + mlpObjectsData.BasketPartRadius;
            // 2. 计算篮球中心到篮圈中心的方向向量
            var offset = Position - rimCenter;
            // 3. 计算距离的平方（避免开方运算）
            var distanceSquared = offset.sqrMagnitude;
            // 4. 如果距离大于两半径之和，没有碰撞，直接返回
            if (distanceSquared >= combinedRadius * combinedRadius)
            {
                return;
            }

            // 5. 计算实际距离和碰撞法线方向
            var distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSquared));
            var normal = distanceSquared > 0.0001f
                ? offset / distance
                : new Vector2(Mathf.Sign(Position.x - rimCenter.x), -1f).normalized;
            if (normal.sqrMagnitude < 0.1f)
            {
                normal = Vector2.up;
            }
            // 6. 将篮球推到篮圈碰撞圆的边缘
            Position = rimCenter + normal * combinedRadius;

            // 7. 计算篮球速度在碰撞法线方向上的分量
            var velocityIntoRim = Vector2.Dot(Velocity, normal);
            // 8. 如果篮球正在向篮圈内部移动，则反弹
            if (velocityIntoRim < 0f)
            {
                // 9. 沿法线方向反弹速度并应用弹性系数
                Velocity -= (1f + RimRestitution) * velocityIntoRim * normal;
                // 10. 稍微减缓整体速度模拟能量损失
                Velocity *= 0.985f;
            }

            // 11. 设置篮球为篮筐区域状态
            SetBasketState();
            // 12. 如果篮球在篮筐下方则播放篮圈碰撞音效
            if (Position.y <= basket.Height - 2f)
            {
                PlayBasketSound(1);
            }
        }

        /// <summary>
        /// 检测篮球是否按正确顺序穿过上下得分传感器以计为进球。
        /// </summary>
        /// <param name="basket">要检测碰撞的篮筐对象</param>
        /// <param name="scoringSide">篮球穿过篮筐时得分的一侧</param>
        private void ProcessScoreSensors(mlpBasketObject basket, int scoringSide)
        {
            // 1. 检查篮球当前是否可以得分
            if (!canScore)
            {
                return;
            }

            // 2. 如果已锁定得分阵营且不是当前阵营，直接返回
            if (scoreArmedSide != 0 && scoringSide != scoreArmedSide)
            {
                return;
            }

            // 3. 检测篮球是否穿过得分传感器上方（上半部分）
            if (TouchesSensor(previousPosition, Position, basket.Center, basket.Height + mlpObjectsData.SensorUp))
            {
                upperSensorPassed = true;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            // 4. 检测篮球是否穿过得分传感器下方（下半部分）
            if (!TouchesSensor(previousPosition, Position, basket.Center, basket.Height + mlpObjectsData.SensorDown))
            {
                return;
            }

            // 5. 通知比赛处理器下传感器被触发
            var matchProcessorReady = gameCore.MatchProcessor.ProcessSensor(1);
            // 6. 如果比赛处理器确认有效、或是保证扣篮、或是教程保证得分，则确认得分
            if (matchProcessorReady || (guaranteedDunkScore && scoringSide == scoreArmedSide)
                || (tutorialGuaranteedScore && scoringSide == scoreArmedSide))
            {
                CommitScore(scoringSide);
            }
            else
            {
                // 7. 否则取消本次得分尝试
                CancelScoreAttempt();
            }
        }

        /// <summary>
        /// 在保证扣篮期间，如果篮球足够接近篮筐中心则立即得分。
        /// </summary>
        /// <param name="basketLeft">左侧篮筐对象</param>
        /// <param name="basketRight">右侧篮筐对象</param>
        private void TryResolveGuaranteedDunkScore(mlpBasketObject basketLeft, mlpBasketObject basketRight)
        {
            // 1. 检查是否可以得分、是否是保证扣篮模式、是否已锁定得分阵营
            if (!canScore || !guaranteedDunkScore || scoreArmedSide == 0)
            {
                return;
            }

            // 2. 根据得分阵营选择对应的篮筐
            var armedBasket = scoreArmedSide == 1 ? basketLeft : basketRight;
            if (armedBasket == null)
            {
                return;
            }

            // 3. 计算篮筐下方得分区域的水平范围（带额外容差）
            var minX = armedBasket.Center - mlpObjectsData.SensorHalf - mlpObjectsData.BallRadius - GuaranteedDunkScoreExtraX;
            var maxX = armedBasket.Center + mlpObjectsData.SensorHalf + mlpObjectsData.BallRadius + GuaranteedDunkScoreExtraX;
            // 4. 检测篮球是否刚刚穿过得分传感器的下边界
            var crossedDown = previousPosition.y <= armedBasket.Height + mlpObjectsData.SensorDown &&
                              Position.y >= armedBasket.Height + mlpObjectsData.SensorDown;
            // 5. 如果篮球在得分区域内（穿过下边界或已低于传感器区域），则确认得分
            if ((crossedDown || Position.y >= armedBasket.Height + mlpObjectsData.SensorDown + mlpObjectsData.SensorHeight) &&
                Position.x >= minX &&
                Position.x <= maxX)
            {
                CommitScore(scoreArmedSide);
            }
        }

        /// <summary>
        /// 标记篮球已得分并通知比赛处理器。
        /// </summary>
        /// <param name="scoringSide">篮球穿过篮筐时得分的一侧</param>
        private void CommitScore(int scoringSide)
        {
            CancelScoreAttempt();
            State = "score";
            PlayBasketSound(0);
            gameCore.OnBallScored(scoringSide);
        }

        /// <summary>
        /// 重置所有得分追踪标志，使篮球可以尝试下一次投篮。
        /// </summary>
        private void CancelScoreAttempt()
        {
            canScore = false;
            upperSensorPassed = false;
            guaranteedDunkScore = false;
            tutorialGuaranteedScore = false;
            scoreArmedSide = 0;
        }

        public void TutorialSetGuaranteedScore()
        {
            tutorialGuaranteedScore = true;
        }

        public void TutorialClearGuaranteedScore()
        {
            tutorialGuaranteedScore = false;
        }

        /// <summary>
        /// 测试篮球扫掠轨迹是否与矩形得分传感器区域相交。
        /// </summary>
        /// <param name="start">篮球轨迹的起始位置</param>
        /// <param name="end">篮球轨迹的结束位置</param>
        /// <param name="centerX">得分传感器的水平中心</param>
        /// <param name="topY">得分传感器的顶部 Y 坐标</param>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        private static bool TouchesSensor(Vector2 start, Vector2 end, float centerX, float topY)
        {
            var minX = centerX - mlpObjectsData.SensorHalf - mlpObjectsData.BallRadius;
            var maxX = centerX + mlpObjectsData.SensorHalf + mlpObjectsData.BallRadius;
            var minY = topY - mlpObjectsData.BallRadius;
            var maxY = topY + mlpObjectsData.SensorHeight + mlpObjectsData.BallRadius;
            return SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY);
        }

        /// <summary>
        /// 使用 Cohen-Sutherland 线段裁剪算法检测移动点是否穿过矩形。
        /// </summary>
        /// <param name="start">轨迹的起始位置</param>
        /// <param name="end">轨迹的结束位置</param>
        /// <param name="minX">测试矩形的左边界</param>
        /// <param name="maxX">测试矩形的右边界</param>
        /// <param name="minY">测试矩形的下边界</param>
        /// <param name="maxY">测试矩形的上边界</param>
        /// <returns>线段与矩形相交时返回 true；否则返回 false。</returns>
        private static bool SweptPointIntersectsRect(Vector2 start, Vector2 end, float minX, float maxX, float minY, float maxY)
        {
            if (PointInsideRect(start, minX, maxX, minY, maxY) || PointInsideRect(end, minX, maxX, minY, maxY))
            {
                return true;
            }

            var direction = end - start;
            var tMin = 0f;
            var tMax = 1f;
            return ClipSegment(-direction.x, start.x - minX, ref tMin, ref tMax) &&
                   ClipSegment(direction.x, maxX - start.x, ref tMin, ref tMax) &&
                   ClipSegment(-direction.y, start.y - minY, ref tMin, ref tMax) &&
                   ClipSegment(direction.y, maxY - start.y, ref tMin, ref tMax);
        }

        /// <summary>
        /// 当二维点位于给定轴对齐矩形内部时返回 true。
        /// </summary>
        /// <param name="point">要检测的二维点</param>
        /// <param name="minX">测试矩形的左边界</param>
        /// <param name="maxX">测试矩形的右边界</param>
        /// <param name="minY">测试矩形的下边界</param>
        /// <param name="maxY">测试矩形的上边界</param>
        /// <returns>点在矩形内部时返回 true；否则返回 false。</returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// 对线段的一个分量进行 Cohen-Sutherland 裁剪的平板边界裁剪。
        /// </summary>
        /// <param name="p">裁剪平板的方向分量</param>
        /// <param name="q">裁剪平板的距离分量</param>
        /// <param name="tMin">当前最小参数裁剪值</param>
        /// <param name="tMax">当前最大参数裁剪值</param>
        /// <returns>线段未被完全裁剪时返回 true；否则返回 false。</returns>
        private static bool ClipSegment(float p, float q, ref float tMin, ref float tMax)
        {
            if (Mathf.Approximately(p, 0f))
            {
                return q >= 0f;
            }

            var ratio = q / p;
            if (p < 0f)
            {
                if (ratio > tMax)
                {
                    return false;
                }

                if (ratio > tMin)
                {
                    tMin = ratio;
                }
            }
            else
            {
                if (ratio < tMin)
                {
                    return false;
                }

                if (ratio < tMax)
                {
                    tMax = ratio;
                }
            }

            return true;
        }

        /// <summary>
        /// 将篮球状态切换为"basket"，除非已经处于得分状态。
        /// </summary>
        private void SetBasketState()
        {
            if (State != "score")
            {
                State = "basket";
            }
        }

        /// <summary>
        /// 播放相应的篮筐音效（入网声、篮圈撞击声或篮板声），快速碰撞时带冷却时间。
        /// </summary>
        /// <param name="type">碰撞音效类型（0=入网声，1=篮圈撞击，2=篮板）</param>
        private void PlayBasketSound(int type)
        {
            if (type != 0 && collisionSoundCooldown > 0f)
            {
                return;
            }

            if (type == 0)
            {
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BNet);
                return;
            }

            collisionSoundCooldown = CollisionSoundCooldownDuration;
            if (type == 1)
            {
                var velocityMagnitude = Velocity.magnitude;
                var volume = velocityMagnitude > 300f ? 1f : velocityMagnitude / 300f * 0.8f;
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BRing, Mathf.Clamp(volume, 0.1f, 1f));
            }
            else
            {
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BBasket);
            }
        }

        /// <summary>
        /// 清除所有得分激活标志，并可选择性地为下一次投篮激活得分传感器。
        /// </summary>
        /// <param name="armed">为 true 时激活得分传感器以准备下一次投篮</param>
        private void ResetScoring(bool armed)
        {
            canScore = armed;
            upperSensorPassed = false;
            guaranteedDunkScore = false;
            scoreArmedSide = 0;
            collisionSoundCooldown = 0f;
        }

        /// <summary>
        /// 计算将篮球从给定位置抛物线投向目标篮筐所需的发射速度。
        /// </summary>
        /// <param name="x">像素空间中的水平坐标</param>
        /// <param name="y">像素空间中的垂直坐标</param>
        /// <param name="offset">叠加到篮筐中心的水平偏移</param>
        /// <returns>计算得到的发射速度向量。</returns>
        private Vector2 CalcThrowVel(float x, float y, float offset)
        {
            // 1. 根据篮球所属阵营确定目标篮筐位置
            float targetX;
            float distance;
            if (Side == 1)
            {
                targetX = mlpObjectsData.BasketCenter + offset;
                distance = x;
            }
            else
            {
                targetX = mlpObjectsData.BasketCenter2 + offset;
                distance = mlpConstants.Width - x;
            }

            // 2. 根据到篮筐的距离选择抛物线弧高
            float arc;
            if (distance <= 150f)
            {
                arc = 70f;
            }
            else if (distance <= 250f)
            {
                arc = 100f;
            }
            else if (distance <= 350f)
            {
                arc = 0.3f * distance + 40f;
            }
            else if (distance <= 540f)
            {
                arc = 150f;
            }
            else
            {
                arc = 130f;
            }

            // 3. 非教程模式下给弧高加一点随机变化模拟手感
            if (!tutorialGuaranteedScore)
            {
                arc *= 1f + 0.1f * (Random.value <= 0.5f ? -1f : 1f) * Random.value;
            }
            // 4. 限制弧高不超过最大值
            arc = Mathf.Min(arc, 185f);
            // 5. 用抛物线公式计算出手速度向量
            return CalcVel(x, y, targetX, mlpObjectsData.BasketHeight, arc);
        }

        /// <summary>
        /// 求解抛物线轨迹，产生从 (x,y) 以给定弧高到达目标的速度。
        /// </summary>
        /// <param name="x">像素空间中的水平坐标</param>
        /// <param name="y">像素空间中的垂直坐标</param>
        /// <param name="targetX">目标的水平位置</param>
        /// <param name="targetY">目标的垂直位置</param>
        /// <param name="arc">出手点上方的最高弧高</param>
        /// <returns>计算得到的速度向量。</returns>
        private Vector2 CalcVel(float x, float y, float targetX, float targetY, float arc)
        {
            var gravity = mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass;
            var offsetY = y - (targetY - arc);
            var vy = -Mathf.Sqrt(Mathf.Max(0.01f, 2f * gravity * offsetY));
            var upTime = -vy / gravity;
            var downTime = Mathf.Sqrt(2f * arc / gravity);
            return new Vector2((targetX - x) / (upTime + downTime) * 1.035f, vy);
        }

        /// <summary>
        /// 根据距离、高度、跑动速度和球员精度随机生成投篮偏移值。
        /// </summary>
        /// <param name="distance">到目标篮筐的估算距离</param>
        /// <param name="y">像素空间中的垂直坐标</param>
        /// <param name="running">球员移动速度带来的额外偏移</param>
        /// <param name="accuracy">投篮精度修正值（越低越准）</param>
        /// <returns>计算得到的偏移系数。</returns>
        private float CalcDispersion(float distance, float y, float running, float accuracy)
        {
            // 1. 如果精度极高（负值），直接返回完美命中
            if (accuracy <= -0.5f)
            {
                return 1f;
            }

            // 2. 根据出手高度计算垂直方向的偏移量
            float vertical;
            if (y < 235f)
            {
                vertical = 0f;
            }
            else if (y >= 295f)
            {
                vertical = mlpObjectsData.VerticalDispersion;
            }
            else
            {
                vertical = (1f - (295f - y) / 60f) * mlpObjectsData.VerticalDispersion;
            }

            // 3. 根据到篮筐的距离查找对应的离散度等级
            float distanceDispersion =
                distance <= 100f ? 0f :
                distance <= 200f ? 0.01f :
                distance <= 300f ? 0.02f :
                distance <= 400f ? 0.03f :
                distance <= 490f ? 0.04f :
                distance <= 540f ? 0.01f : 0.07f;

            // 4. 随机选择偏移方向（左或右），计算总偏移值
            var sign = Random.value < 0.5f ? -1f : 1f;
            var value = sign * (mlpObjectsData.Dispersion + vertical + distanceDispersion + accuracy + running) * Random.value;
            // 5. 如果偏移很小，视为完美命中
            if (Mathf.Abs(value) <= 0.02f)
            {
                return 1f;
            }

            // 6. 偏移过大（偏左），返回特殊值2表示严重偏左
            if (value < -0.08f)
            {
                return 2f;
            }

            // 7. 偏移过大（偏右），返回特殊值3表示严重偏右
            if (value > 0.08f)
            {
                return 3f;
            }

            // 8. 正常偏移范围，返回1+偏移值作为速度缩放系数
            return 1f + value;
        }

        /// <summary>
        /// 标记篮球在下一次图形更新时变为可见。
        /// </summary>
        private void Show()
        {
            visibleNextFrame = true;
        }

        /// <summary>
        /// 选择主题篮球精灵（如有），否则回退到默认图集篮球帧。
        /// </summary>
        /// <returns>解析到的篮球精灵。</returns>
        private Sprite ResolveBallSprite()
        {
            return mlpGameplaySpriteLoader.LoadMatchBallSprite(gameCore.MatchData.BallTheme, 0.5f, 0.5f);
        }

        /// <summary>
        /// 每帧将篮球和阴影 GameObject 移动到当前物理位置。
        /// </summary>
        private void UpdateGraphic()
        {
            if (visibleNextFrame)
            {
                visibleNextFrame = false;
                graphic.SetActive(true);
                shadow.SetActive(true);
            }

            mlpRender.ApplyPixelTransform(graphic.transform, Position.x, Position.y, 0.2f, 1f, -Position.x * 0.1f);
            var shadowY = mlpObjectsData.FloorY + 3f;
            mlpRender.ApplyPixelTransform(shadow.transform, Position.x, shadowY, 0.01f, Mathf.Clamp01(1f - (shadowY - Position.y) / 420f) * 0.7f);
        }
    }

    /// <summary>
    /// 传送特效：角色使用传送技能时播放的视觉特效，包括闪烁、消失和出现动画。
    /// </summary>
    public sealed class mlpTeleportFx
    {
        private enum TeleportPhase
        {
            Hidden,
            BlackExpand,
            BlackCollapse,
            WhiteFlash
        }

        // 黑色扩展阶段持续时间。
        private const float BlackExpandDuration = 0.06f;
        // 黑色收缩阶段持续时间。
        private const float BlackCollapseDuration = 0.07f;
        // 黑色收缩时的缩放过渡时长。
        private const float BlackCollapseScaleDuration = 0.08f;
        // 白色闪光第一段时长。
        private const float WhiteFlashDuration1 = 0.03f;
        // 白色闪光第二段时长。
        private const float WhiteFlashDuration2 = 0.03f;
        // 白色闪光第三段时长。
        private const float WhiteFlashDuration3 = 0.024f;
        // 传送动画播放帧率。
        private const float AnimationFps = 30f;

        // 传送特效根节点。
        private readonly GameObject graphic;
        // 黑色扩展圆环节点。
        private readonly Transform blackNode;
        // 黑色扩展圆环渲染器。
        private readonly SpriteRenderer blackRenderer;
        // 中心圆点渲染器。
        private readonly SpriteRenderer centerRenderer;
        // 白色闪光层渲染器。
        private readonly SpriteRenderer whiteRenderer;
        // 传送动画帧渲染器。
        private readonly SpriteRenderer animRenderer;
        // 传送动画帧数组。
        private readonly Sprite[] frames;
        // 对应角色的技能定义，用于切换特效主题。
        private readonly mlpCharacterSkillDefinition skillDefinition;
        // 当前特效阶段。
        private TeleportPhase phase = TeleportPhase.Hidden;
        // 当前阶段已用时间。
        private float phaseTime;

        /// <summary>
        /// 构建传送视觉特效，包括黑色扩展、中心点、动画帧和白色闪光。
        /// </summary>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpTeleportFx(Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            this.skillDefinition = skillDefinition;

            // 1. 创建传送特效的根 GameObject，挂到父节点下
            graphic = new GameObject("TeleportFx");
            graphic.transform.SetParent(parent, false);

            // 2. 创建黑色扩展圆环：用于传送开始时的黑色扩散效果
            blackNode = new GameObject("TeleportBlack").transform;
            blackNode.SetParent(graphic.transform, false);
            blackRenderer = blackNode.gameObject.AddComponent<SpriteRenderer>();
            blackRenderer.sortingOrder = 74;
            blackRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport10000");

            // 3. 创建中心白点：挂在黑色圆环内部，作为传送的视觉中心
            var centerNode = new GameObject("TeleportCenter");
            centerNode.transform.SetParent(blackNode, false);
            centerRenderer = centerNode.AddComponent<SpriteRenderer>();
            centerRenderer.sortingOrder = 75;
            centerRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport20000");

            // 4. 创建动画帧播放器：用于播放传送过程中的逐帧动画
            var animNode = new GameObject("TeleportAnim");
            animNode.transform.SetParent(graphic.transform, false);
            animRenderer = animNode.AddComponent<SpriteRenderer>();
            animRenderer.sortingOrder = 76;

            // 5. 创建白色闪光层：传送结束时的白色闪烁效果
            var whiteNode = new GameObject("TeleportWhite");
            whiteNode.transform.SetParent(graphic.transform, false);
            whiteRenderer = whiteNode.AddComponent<SpriteRenderer>();
            whiteRenderer.sortingOrder = 77;
            whiteRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            // 6. 加载传送动画的 4 帧精灵图
            frames = new[]
            {
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30000"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30001"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30002"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30003")
            };

            // 7. 根据角色技能配色调整特效颜色，然后隐藏
            ApplyTheme();
            Hide();
        }

        /// <summary>
        /// 在指定世界坐标处开始传送动画序列。
        /// </summary>
        /// <param name="x">像素空间中的水平坐标</param>
        /// <param name="y">像素空间中的垂直坐标</param>
        public void StartPlay(float x, float y)
        {
            phase = TeleportPhase.BlackExpand;
            phaseTime = 0f;
            graphic.SetActive(true);
            mlpRender.ApplyPixelTransform(graphic.transform, x, y, 0.16f, 1f);
            blackRenderer.enabled = true;
            centerRenderer.enabled = true;
            whiteRenderer.enabled = false;
            animRenderer.enabled = false;
            animRenderer.sprite = null;
            blackNode.localScale = new Vector3(0.1f, 0.1f, 1f);
            blackNode.localRotation = Quaternion.identity;
            whiteRenderer.transform.localScale = new Vector3(0.06f, 0.02f, 1f);
            whiteRenderer.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 每帧推进传送动画的状态机。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void Update(float dt)
        {
            // 1. 如果特效处于隐藏状态，直接返回
            if (phase == TeleportPhase.Hidden)
            {
                return;
            }

            // 2. 累加当前阶段的运行时间
            phaseTime += dt;
            // 3. 根据当前阶段执行对应的动画更新
            switch (phase)
            {
                case TeleportPhase.BlackExpand:
                    // 4. 更新黑色圆环扩展动画
                    UpdateBlackExpand();
                    // 5. 扩展时间结束后切换到收缩阶段
                    if (phaseTime >= BlackExpandDuration)
                    {
                        phase = TeleportPhase.BlackCollapse;
                        phaseTime = 0f;
                        animRenderer.enabled = true;
                    }
                    break;
                case TeleportPhase.BlackCollapse:
                    // 6. 更新黑色圆环收缩动画
                    UpdateBlackCollapse();
                    // 7. 收缩时间结束后切换到白色闪光阶段
                    if (phaseTime >= BlackCollapseDuration)
                    {
                        phase = TeleportPhase.WhiteFlash;
                        phaseTime = 0f;
                        blackRenderer.enabled = false;
                        centerRenderer.enabled = false;
                        animRenderer.enabled = false;
                        whiteRenderer.enabled = true;
                    }
                    break;
                case TeleportPhase.WhiteFlash:
                    // 8. 更新白色闪光动画
                    UpdateWhiteFlash();
                    // 9. 闪光结束后隐藏整个特效
                    if (phaseTime >= WhiteFlashDuration1 + WhiteFlashDuration2 + WhiteFlashDuration3)
                    {
                        Hide();
                    }
                    break;
            }
        }

        /// <summary>
        /// 立即停止传送特效并隐藏所有渲染器。
        /// </summary>
        public void Hide()
        {
            phase = TeleportPhase.Hidden;
            phaseTime = 0f;
            blackRenderer.enabled = false;
            centerRenderer.enabled = false;
            whiteRenderer.enabled = false;
            animRenderer.enabled = false;
            animRenderer.sprite = null;
            graphic.SetActive(false);
        }

        /// <summary>
        /// 在传送第一阶段将黑色圆圈从小扩展到中等大小。
        /// </summary>
        private void UpdateBlackExpand()
        {
            var t = Mathf.Clamp01(phaseTime / BlackExpandDuration);
            var scale = Mathf.Lerp(0.1f, 0.78f, t);
            blackNode.localScale = new Vector3(scale, scale, 1f);
            blackNode.localRotation = Quaternion.Euler(0f, 0f, -180f * t);
        }

        /// <summary>
        /// 旋转的同时收缩黑色圆圈，然后显示动画帧。
        /// </summary>
        private void UpdateBlackCollapse()
        {
            var scaleT = Mathf.Clamp01(phaseTime / BlackCollapseScaleDuration);
            var scale = Mathf.Lerp(1f, 0f, EaseInBack(scaleT));
            blackNode.localScale = new Vector3(scale, scale, 1f);

            var rotateT = Mathf.Clamp01(phaseTime / BlackCollapseDuration);
            blackNode.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(180f, 360f, rotateT));

            var frameIndex = Mathf.Clamp(Mathf.FloorToInt(phaseTime * AnimationFps), 0, frames.Length - 1);
            animRenderer.sprite = frames[frameIndex];
        }

        /// <summary>
        /// 播放三阶段白色闪光：先扩展然后收缩至消失。
        /// </summary>
        private void UpdateWhiteFlash()
        {
            var time1 = WhiteFlashDuration1;
            var time2 = WhiteFlashDuration2;
            var total12 = time1 + time2;
            Vector2 scale;
            if (phaseTime <= time1)
            {
                scale = Vector2.Lerp(new Vector2(0.06f, 0.02f), new Vector2(0.46f, 0.42f), phaseTime / time1);
            }
            else if (phaseTime <= total12)
            {
                scale = Vector2.Lerp(new Vector2(0.46f, 0.42f), new Vector2(0.06f, 0.82f), (phaseTime - time1) / time2);
            }
            else
            {
                scale = Vector2.Lerp(new Vector2(0.06f, 0.82f), new Vector2(0.02f, 0.05f), (phaseTime - total12) / WhiteFlashDuration3);
            }

            whiteRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        /// <summary>
        /// 应用带轻微过冲的 ease-in-back 曲线，产生弹性动画效果。
        /// </summary>
        /// <param name="t">归一化进度值（0 到 1）</param>
        /// <returns>计算得到的缓动值。</returns>
        private static float EaseInBack(float t)
        {
            const float overshoot = 1.70158f;
            return (overshoot + 1f) * t * t * t - overshoot * t * t;
        }

        private void ApplyTheme()
        {
            // 1. 将黑色圆环设为角色次要颜色偏黑的混合色
            blackRenderer.color = Color.Lerp(skillDefinition.SecondaryColor, Color.black, 0.22f);
            // 2. 中心白点使用角色主色调
            centerRenderer.color = skillDefinition.PrimaryColor;
            // 3. 白色闪光层使用白色和强调色的混合
            whiteRenderer.color = Color.Lerp(Color.white, skillDefinition.AccentColor, 0.42f);
            // 4. 动画帧层使用主色调和强调色的混合色
            animRenderer.color = Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.AccentColor, 0.38f);
        }
    }

    /// <summary>
    /// 护盾特效对象：角色激活护盾技能时显示的防护罩，可以偏转飞来的篮球。
    /// </summary>
    public sealed class mlpShieldObject
    {
        private enum ShieldPhase
        {
            Hidden,
            Intro,
            Active,
            Fading
        }

        // 护盾入场总时长。
        private const float IntroTime = 0.14f;
        // 护盾下落入场持续时间。
        private const float IntroDropTime = 0.12f;
        // 护盾入场时的初始下落偏移。
        private const float IntroDropOffsetY = -600f;
        // 入场模糊层的横向缩放。
        private const float IntroBlurScaleX = 1.08f;
        // 入场模糊层的纵向缩放。
        private const float IntroBlurScaleY = 1.16f;
        // 护盾展示停留时间。
        private const float ShowTime = 3f;
        // 护盾渐隐时间。
        private const float FadeTime = 0.5f;
        // 动画播放帧率。
        private const float AnimationFps = 30f;
        // 护盾图形的 X 偏移。
        private const float GraphicXOffset = 23f;
        // 护盾图形的 Y 偏移。
        private const float GraphicYOffset = -62f;
        // 碰撞矩形顶部位置。
        private const float CollisionRectTop = 30f;
        // 碰撞矩形宽度。
        private const float CollisionRectWidth = 70f;
        // 碰撞矩形高度。
        private const float CollisionRectHeight = 10f;
        // 左侧篮筐对应的碰撞矩形左边界偏移。
        private const float CollisionRectLeftLeftSide = -23f;
        // 右侧篮筐对应的碰撞矩形左边界偏移。
        private const float CollisionRectLeftRightSide = -49f;
        // 起始提示精灵的局部 X 偏移。
        private const float StartSpriteLocalX = 1f;

        // 球场侧，-1 左侧，1 右侧。
        private readonly int side;
        // 关联的篮筐对象，用于碰撞检测。
        private readonly mlpBasketObject basket;
        // 护盾根节点。
        private readonly GameObject graphic;
        // 模糊层渲染器。
        private readonly SpriteRenderer blurRenderer;
        // 起始提示渲染器。
        private readonly SpriteRenderer startRenderer;
        // 动画帧渲染器。
        private readonly SpriteRenderer animRenderer;
        // 护盾动画帧数组。
        private readonly Sprite[] frames;
        // 对应角色的技能定义，用于切换护盾主题。
        private readonly mlpCharacterSkillDefinition skillDefinition;

        // 当前护盾阶段。
        private ShieldPhase phase = ShieldPhase.Hidden;
        // 当前阶段已用时间。
        private float phaseTime;
        // 当前护盾透明度。
        private float alpha = 1f;

        /// <summary>
        /// 创建球场一侧的护盾技能视觉特效。
        /// </summary>
        /// <param name="side">球场侧（-1 为左侧，1 为右侧）</param>
        /// <param name="basket">要检测碰撞的篮筐对象</param>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpShieldObject(int side, mlpBasketObject basket, Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            this.side = side;
            this.basket = basket;
            this.skillDefinition = skillDefinition;

            // 1. 创建护盾根节点，根据所在侧命名为 ShieldLeft 或 ShieldRight
            graphic = new GameObject(side == -1 ? "ShieldLeft" : "ShieldRight");
            graphic.transform.SetParent(parent, false);

            // 2. 创建三层渲染器：起始帧（静止）、模糊帧（入场动画）、逐帧动画层
            var shieldStartSprite = mlpAtlasCache.Instance.SkillFx.Sprite("ShieldMC0000");
            startRenderer = CreateRenderer("ShieldStart", 63, shieldStartSprite);
            blurRenderer = CreateRenderer("ShieldBlur", 64, shieldStartSprite);
            animRenderer = CreateRenderer("ShieldAnim", 65, null);

            // 3. 加载护盾动画的 21 帧精灵图（逐帧播放的展开动画）
            frames = new Sprite[21];
            for (var i = 0; i < frames.Length; i++)
            {
                var frameName = $"ShieldMC2{i:0000}";
                var frame = mlpAtlasCache.Instance.SkillFx.Frame(frameName);
                if (frame != null && frame.W > 0f && frame.H > 0f)
                {
                    frames[i] = mlpAtlasCache.Instance.SkillFx.Sprite(frameName);
                }
            }

            // 4. 初始隐藏，应用角色配色主题
            graphic.SetActive(false);
            ApplyTheme();
        }

        public bool IsBlocking => phase == ShieldPhase.Active && phaseTime < AnimationDuration + ShowTime;
        public bool CanActivate => phase == ShieldPhase.Hidden;

        /// <summary>
        /// 开始护盾入场动画：从上方滑入模糊精灵并播放护盾音效。
        /// </summary>
        public void Activate()
        {
            // 1. 设置护盾为入场阶段，重置计时器
            phase = ShieldPhase.Intro;
            phaseTime = 0f;
            alpha = 1f;
            // 2. 隐藏篮筐前沿耳图形，为护盾腾出空间
            basket?.HideEar();
            // 3. 显示护盾根节点
            graphic.SetActive(true);
            // 4. 入场时只显示模糊层，隐藏其他层
            startRenderer.enabled = false;
            blurRenderer.enabled = true;
            animRenderer.enabled = false;
            // 5. 将模糊精灵放到起始位置（上方），并设置放大比例
            blurRenderer.transform.localPosition = new Vector3(StartSpriteLocalX, IntroDropOffsetY, 0f);
            blurRenderer.transform.localScale = new Vector3(IntroBlurScaleX, IntroBlurScaleY, 1f);
            startRenderer.transform.localScale = Vector3.one;
            animRenderer.transform.localScale = Vector3.one;
            // 6. 应用透明度并更新位置
            ApplyAlpha();
            UpdateGraphic();
            // 7. 播放护盾激活音效
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PShield);
        }

        /// <summary>
        /// 每帧推进护盾的入场、激活和消退阶段。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void Update(float dt)
        {
            // 1. 如果护盾处于隐藏状态，直接返回
            if (phase == ShieldPhase.Hidden)
            {
                return;
            }

            // 2. 累加当前阶段的运行时间
            phaseTime += dt;
            // 3. 根据当前阶段执行对应的动画更新
            switch (phase)
            {
                case ShieldPhase.Intro:
                    // 4. 更新入场动画（模糊精灵从上方落下）
                    UpdateIntro();
                    // 5. 入场时间结束后切换到激活阶段
                    if (phaseTime >= IntroTime)
                    {
                        phase = ShieldPhase.Active;
                        phaseTime = 0f;
                        blurRenderer.enabled = false;
                        startRenderer.enabled = true;
                        animRenderer.enabled = true;
                    }
                    break;
                case ShieldPhase.Active:
                    // 6. 更新激活阶段动画（护盾展开动画播放）
                    UpdateActive();
                    // 7. 动画播放加展示时间结束后切换到消退阶段
                    if (phaseTime >= AnimationDuration + ShowTime)
                    {
                        phase = ShieldPhase.Fading;
                        phaseTime = 0f;
                        animRenderer.enabled = false;
                        basket?.ShowEar();
                    }
                    break;
                case ShieldPhase.Fading:
                    // 8. 计算消退透明度并应用
                    alpha = 1f - Mathf.Clamp01(phaseTime / FadeTime);
                    ApplyAlpha();
                    // 9. 消退完成后重置护盾
                    if (phaseTime >= FadeTime)
                    {
                        Reset();
                        return;
                    }
                    break;
            }

            // 10. 更新护盾图形位置
            UpdateGraphic();
        }

        /// <summary>
        /// 立即隐藏护盾并恢复篮筐前沿耳图形。
        /// </summary>
        public void Reset()
        {
            phase = ShieldPhase.Hidden;
            phaseTime = 0f;
            alpha = 1f;
            startRenderer.enabled = false;
            blurRenderer.enabled = false;
            animRenderer.enabled = false;
            animRenderer.sprite = null;
            graphic.SetActive(false);
            basket?.ShowEar();
        }

        /// <summary>
        /// 检测篮球是否与护盾碰撞矩形重叠，如果是则将其弹开。
        /// </summary>
        /// <param name="ball">要检测或影响的篮球对象</param>
        /// <returns>成功阻挡篮球时返回 true；否则返回 false。</returns>
        public bool TryBlockBall(mlpBallObject ball)
        {
            // 1. 检查护盾是否在激活状态、篮球是否存在、篮球是否正在得分
            if (!IsBlocking || ball == null || ball.State == "score")
            {
                return false;
            }

            // 2. 获取护盾的原点位置
            var origin = ShieldOrigin;
            // 3. 根据所在侧选择碰撞矩形的左边距
            var rectLeft = side == -1 ? CollisionRectLeftLeftSide : CollisionRectLeftRightSide;
            // 4. 计算碰撞矩形的四条边界（考虑篮球半径的扩展）
            var minX = origin.x + rectLeft - mlpObjectsData.BallRadius;
            var maxX = minX + CollisionRectWidth + mlpObjectsData.BallRadius * 2f;
            var minY = origin.y + CollisionRectTop - mlpObjectsData.BallRadius;
            var maxY = minY + CollisionRectHeight + mlpObjectsData.BallRadius * 2f;
            // 5. 用扫掠检测判断篮球轨迹是否穿过护盾碰撞矩形
            if (!SweptPointIntersectsRect(ball.PreviousPosition, ball.Position, minX, maxX, minY, maxY))
            {
                return false;
            }

            // 6. 触发篮球被护盾弹开的效果
            ball.OnShieldCollision(side);
            return true;
        }

        /// <summary>
        /// 每帧将护盾图形定位到篮筐原点，根据所在侧水平翻转。
        /// </summary>
        private void UpdateGraphic()
        {
            if (!graphic.activeSelf)
            {
                return;
            }

            var origin = ShieldOrigin;
            mlpRender.ApplyPixelTransform(graphic.transform, origin.x, origin.y, 0.15f, 1f);
            var localScale = graphic.transform.localScale;
            localScale.x = side == 1 ? -Mathf.Abs(localScale.x) : Mathf.Abs(localScale.x);
            graphic.transform.localScale = localScale;
        }

        private Vector2 ShieldOrigin => new Vector2(
            basket.Center + side * GraphicXOffset,
            basket.Height + GraphicYOffset);

        private float AnimationDuration => frames.Length / AnimationFps;

        /// <summary>
        /// 使用 ease-out-back 曲线播放模糊精灵从上方落下的动画。
        /// </summary>
        private void UpdateIntro()
        {
            var t = Mathf.Clamp01(phaseTime / IntroDropTime);
            blurRenderer.transform.localPosition = new Vector3(
                StartSpriteLocalX,
                Mathf.Lerp(IntroDropOffsetY, 0f, EaseOutBack(t)),
                0f);
            blurRenderer.transform.localScale = new Vector3(
                Mathf.Lerp(IntroBlurScaleX, 1f, t),
                Mathf.Lerp(IntroBlurScaleY, 1f, t),
                1f);
        }

        /// <summary>
        /// 按配置的帧率循环播放护盾动画帧。
        /// </summary>
        private void UpdateActive()
        {
            startRenderer.enabled = true;
            startRenderer.transform.localPosition = new Vector3(StartSpriteLocalX, 0f, 0f);

            var frameIndex = Mathf.FloorToInt(phaseTime * AnimationFps);
            if (frameIndex >= 0 && frameIndex < frames.Length && frames[frameIndex] != null)
            {
                animRenderer.enabled = true;
                animRenderer.sprite = frames[frameIndex];
            }
            else
            {
                animRenderer.enabled = false;
                animRenderer.sprite = null;
            }
        }

        /// <summary>
        /// 使用当前淡出透明度和技能主题颜色为所有护盾渲染器着色。
        /// </summary>
        private void ApplyAlpha()
        {
            startRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.AccentColor, 0.3f), alpha);
            blurRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, Color.white, 0.18f), alpha * 0.85f);
            animRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.SecondaryColor, 0.22f), alpha);
        }

        /// <summary>
        /// 创建带有 SpriteRenderer 的子 GameObject，使用指定的排序层级。
        /// </summary>
        /// <param name="name">子 GameObject 的名称</param>
        /// <param name="sortingOrder">精灵排序层级，决定绘制优先级</param>
        /// <param name="sprite">像素尺寸驱动缩放的精灵</param>
        /// <returns>创建的 SpriteRenderer 组件。</returns>
        private SpriteRenderer CreateRenderer(string name, int sortingOrder, Sprite sprite)
        {
            var child = new GameObject(name);
            child.transform.SetParent(graphic.transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.sprite = sprite;
            renderer.enabled = false;
            return renderer;
        }

        /// <summary>
        /// 应用带轻微过冲的 ease-out-back 曲线，产生弹性动画效果。
        /// </summary>
        /// <param name="t">归一化进度值（0 到 1）</param>
        /// <returns>计算得到的缓动值。</returns>
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            var x = t - 1f;
            return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
        }

        private void ApplyTheme()
        {
            // 1. 应用当前透明度到所有护盾渲染器
            ApplyAlpha();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// <summary>
        /// 使用 Cohen-Sutherland 线段裁剪算法检测移动点是否穿过矩形。
        /// </summary>
        /// <param name="start">轨迹的起始位置</param>
        /// <param name="end">轨迹的结束位置</param>
        /// <param name="minX">测试矩形的左边界</param>
        /// <param name="maxX">测试矩形的右边界</param>
        /// <param name="minY">测试矩形的下边界</param>
        /// <param name="maxY">测试矩形的上边界</param>
        /// <returns>线段与矩形相交时返回 true；否则返回 false。</returns>
        private static bool SweptPointIntersectsRect(Vector2 start, Vector2 end, float minX, float maxX, float minY, float maxY)
        {
            if (PointInsideRect(start, minX, maxX, minY, maxY) || PointInsideRect(end, minX, maxX, minY, maxY))
            {
                return true;
            }

            var direction = end - start;
            var tMin = 0f;
            var tMax = 1f;
            return ClipSegment(-direction.x, start.x - minX, ref tMin, ref tMax) &&
                   ClipSegment(direction.x, maxX - start.x, ref tMin, ref tMax) &&
                   ClipSegment(-direction.y, start.y - minY, ref tMin, ref tMax) &&
                   ClipSegment(direction.y, maxY - start.y, ref tMin, ref tMax);
        }

        /// <summary>
        /// 当二维点位于给定轴对齐矩形内部时返回 true。
        /// </summary>
        /// <param name="point">要检测的二维点</param>
        /// <param name="minX">测试矩形的左边界</param>
        /// <param name="maxX">测试矩形的右边界</param>
        /// <param name="minY">测试矩形的下边界</param>
        /// <param name="maxY">测试矩形的上边界</param>
        /// <returns>点在矩形内部时返回 true；否则返回 false。</returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// 对线段的一个分量进行 Cohen-Sutherland 裁剪的平板边界裁剪。
        /// </summary>
        /// <param name="p">裁剪平板的方向分量</param>
        /// <param name="q">裁剪平板的距离分量</param>
        /// <param name="tMin">当前最小参数裁剪值</param>
        /// <param name="tMax">当前最大参数裁剪值</param>
        /// <returns>线段未被完全裁剪时返回 true；否则返回 false。</returns>
        private static bool ClipSegment(float p, float q, ref float tMin, ref float tMax)
        {
            if (Mathf.Approximately(p, 0f))
            {
                return q >= 0f;
            }

            var ratio = q / p;
            if (p < 0f)
            {
                if (ratio > tMax)
                {
                    return false;
                }

                if (ratio > tMin)
                {
                    tMin = ratio;
                }
            }
            else
            {
                if (ratio < tMin)
                {
                    return false;
                }

                if (ratio < tMax)
                {
                    tMax = ratio;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 角色技能特效：角色激活专属技能时播放的视觉特效（闪光、粒子、图标等）。
    /// </summary>
    public sealed class mlpPlayerSkillFx
    {
        private enum FxMode
        {
            Hidden,
            Buff,
            Burst,
            Dash
        }

        // 技能特效根节点。
        private readonly GameObject root;
        // 外圈发光渲染器。
        private readonly SpriteRenderer glowRenderer;
        // 核心渲染器。
        private readonly SpriteRenderer coreRenderer;
        // 强调色渲染器。
        private readonly SpriteRenderer accentRenderer;
        // 基础技能定义，用于恢复默认主题。
        private readonly mlpCharacterSkillDefinition baseSkillDefinition;
        // 当前生效的技能定义。
        private mlpCharacterSkillDefinition skillDefinition;
        // 当前特效模式。
        private FxMode mode = FxMode.Hidden;
        // 当前特效已运行时间。
        private float timer;
        // 当前特效持续时间。
        private float duration;

        public mlpPlayerSkillFx(Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            // 1. 保存技能定义（基础定义和当前定义，用于切换后恢复）
            baseSkillDefinition = skillDefinition;
            this.skillDefinition = skillDefinition;
            DBLiteFactory.Instance.EnsureLoaded();

            // 2. 创建特效根节点，挂到父节点下
            root = new GameObject("PlayerSkillFx");
            root.transform.SetParent(parent, false);

            // 3. 创建三层渲染器：光晕背景、核心图标、强调色装饰
            glowRenderer = CreateRenderer("Glow", 17, mlpAtlasCache.Instance.Interface.Sprite("EmblemsBg0000"));
            coreRenderer = CreateRenderer("Core", 18, null);
            accentRenderer = CreateRenderer("Accent", 19, null);

            // 4. 根据角色技能设置颜色和图标，然后停止播放（隐藏状态）
            ApplyTheme(skillDefinition);
            Stop();
        }

        public void ApplyTheme(mlpCharacterSkillDefinition definition)
        {
            // 1. 保存技能定义引用
            skillDefinition = definition;
            // 2. 检查是否使用自定义特效美术资源
            var useCustomArt = UsesCustomFxArt(definition.SkillType);
            // 3. 加载核心图标和强调装饰的精灵图
            coreRenderer.sprite = LoadSkillSprite(definition.SkillType, false);
            accentRenderer.sprite = LoadSkillSprite(definition.SkillType, true);
            // 4. 设置光晕背景颜色（自定义美术用更淡的透明度）
            glowRenderer.color = WithAlpha(definition.PrimaryColor, useCustomArt ? 0.14f : 0.18f);
            // 5. 设置核心图标颜色
            coreRenderer.color = useCustomArt
                ? WithAlpha(Color.white, 0.72f)
                : WithAlpha(definition.PrimaryColor, 0.5f);
            // 6. 设置强调装饰颜色
            accentRenderer.color = useCustomArt
                ? WithAlpha(Color.white, 0.62f)
                : WithAlpha(definition.AccentColor, 0.58f);
        }

        public void PlayBuff(float effectDuration)
        {
            BeginMode(FxMode.Buff, effectDuration);
        }

        public void PlayBurst(float effectDuration = 0.42f)
        {
            BeginMode(FxMode.Burst, effectDuration);
        }

        public void PlayBurst(float effectDuration, mlpCharacterSkillDefinition definition)
        {
            ApplyTheme(definition);
            PlayBurst(effectDuration);
        }

        public void PlayDash(float effectDuration)
        {
            BeginMode(FxMode.Dash, effectDuration);
        }

        public void Stop()
        {
            mode = FxMode.Hidden;
            timer = 0f;
            duration = 0f;
            root.SetActive(false);
            if (skillDefinition.SkillType != baseSkillDefinition.SkillType ||
                skillDefinition.SkillName != baseSkillDefinition.SkillName)
            {
                ApplyTheme(baseSkillDefinition);
            }
        }

        public void Update(float dt, Vector2 position, float facingDirection, bool visible)
        {
            // 1. 如果特效处于隐藏状态，关闭显示并返回
            if (mode == FxMode.Hidden)
            {
                root.SetActive(false);
                return;
            }

            // 2. 累加计时器，超过持续时间则停止特效
            timer += dt;
            if (timer >= duration)
            {
                Stop();
                return;
            }

            // 3. 检查是否应该渲染（角色可见或正在冲刺）
            var shouldRender = visible || mode == FxMode.Dash;
            if (!shouldRender)
            {
                root.SetActive(false);
                return;
            }

            // 4. 将特效定位到角色上方，根据朝向翻转
            mlpRender.ApplyPixelTransform(root.transform, position.x, position.y + 30f, 0.08f, 1f);
            var rootScale = root.transform.localScale;
            rootScale.x = Mathf.Abs(rootScale.x) * Mathf.Sign(facingDirection);
            root.transform.localScale = rootScale;

            // 5. 计算动画进度比例并重置渲染器布局
            var t = timer / duration;
            ResetRendererLayout();

            // 6. 如果技能使用自定义特效美术，走单独的更新逻辑
            if (UsesCustomFxArt(skillDefinition.SkillType))
            {
                UpdateCustomFx(t);
                root.SetActive(true);
                return;
            }

            // 7. 根据特效模式更新三层渲染器的大小、颜色和旋转
            switch (mode)
            {
                case FxMode.Buff:
                {
                    // 8. 增益模式：三层都做呼吸脉冲动画
                    var pulse = 0.92f + Mathf.Sin(Time.time * 10f) * 0.06f;
                    glowRenderer.transform.localScale = Vector3.one * pulse * 0.24f;
                    coreRenderer.transform.localScale = new Vector3(0.18f, 0.12f, 1f) * (0.98f + Mathf.Sin(Time.time * 11f) * 0.04f);
                    accentRenderer.transform.localScale = new Vector3(0.3f, 0.08f, 1f) * (0.98f + Mathf.Sin(Time.time * 12f) * 0.04f);
                    glowRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.13f + Mathf.Sin(Time.time * 7f) * 0.015f);
                    coreRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.34f);
                    accentRenderer.color = WithAlpha(skillDefinition.AccentColor, 0.42f);
                    accentRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 48f);
                    break;
                }
                case FxMode.Burst:
                {
                    // 9. 爆发模式：三层从小变大并逐渐淡出
                    glowRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.32f, t);
                    coreRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.22f, t);
                    accentRenderer.transform.localScale = new Vector3(Mathf.Lerp(0.12f, 0.36f, t), Mathf.Lerp(0.04f, 0.12f, t), 1f);
                    glowRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.26f * (1f - t));
                    coreRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.46f * (1f - t));
                    accentRenderer.color = WithAlpha(skillDefinition.AccentColor, 0.58f * (1f - t));
                    accentRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -120f * t);
                    break;
                }
                case FxMode.Dash:
                {
                    // 9. 冲刺模式：水平拉伸效果，中间最宽，两头窄
                    var stretch = Mathf.Lerp(0.88f, 1.18f, Mathf.Sin(t * Mathf.PI));
                    glowRenderer.transform.localScale = new Vector3(0.34f * stretch, 0.12f, 1f);
                    coreRenderer.transform.localScale = new Vector3(0.46f * stretch, 0.1f, 1f);
                    accentRenderer.transform.localScale = new Vector3(0.72f * stretch, 0.06f, 1f);
                    glowRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.1f);
                    coreRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.34f);
                    accentRenderer.color = WithAlpha(skillDefinition.AccentColor, 0.48f * (1f - t * 0.5f));
                    accentRenderer.transform.localRotation = Quaternion.identity;
                    break;
                }
            }

            // 10. 显示特效根节点
            root.SetActive(true);
        }

        private SpriteRenderer CreateRenderer(string name, int sortingOrder, Sprite sprite)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.sprite = sprite;
            return renderer;
        }

        private void ResetRendererLayout()
        {
            glowRenderer.enabled = true;
            coreRenderer.enabled = true;
            accentRenderer.enabled = true;
            glowRenderer.transform.localPosition = Vector3.zero;
            glowRenderer.transform.localRotation = Quaternion.identity;
            coreRenderer.transform.localPosition = Vector3.zero;
            coreRenderer.transform.localRotation = Quaternion.identity;
            accentRenderer.transform.localPosition = Vector3.zero;
            accentRenderer.transform.localRotation = Quaternion.identity;
        }

        private void BeginMode(FxMode fxMode, float effectDuration)
        {
            mode = fxMode;
            timer = 0f;
            duration = Mathf.Max(0.01f, effectDuration);
            root.SetActive(false);
        }

        private void UpdateCustomFx(float t)
        {
            switch (skillDefinition.SkillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    UpdateSoulReapFx(t);
                    break;
                case mlpCharacterSkillType.BadLuck:
                    UpdateFreezeFx(t);
                    break;
            }
        }

        private void UpdateSoulReapFx(float t)
        {
            var fade = mode == FxMode.Burst ? 1f - t : 1f - t * 0.18f;
            var stretch = mode == FxMode.Burst
                ? Mathf.Lerp(0.72f, 1f, t)
                : Mathf.Lerp(0.92f, 1.08f, Mathf.Sin(t * Mathf.PI));

            glowRenderer.enabled = false;
            SetRendererPixelSize(coreRenderer, 150f * stretch, 44f);
            accentRenderer.enabled = false;

            coreRenderer.transform.localPosition = new Vector3(18f, -4f, 0f);

            coreRenderer.color = WithAlpha(Color.white, 0.68f * fade);
        }

        private void UpdateFreezeFx(float t)
        {
            var fade = mode == FxMode.Burst ? 1f - t : 0.92f;
            var pulse = mode == FxMode.Burst
                ? Mathf.Lerp(0.78f, 1.02f, t)
                : 0.98f + Mathf.Sin(Time.time * 8f) * 0.04f;

            SetRendererPixelSize(glowRenderer, 72f * pulse, 72f * pulse);
            SetRendererPixelSize(coreRenderer, 82f * pulse, 82f * pulse);
            SetRendererPixelSize(accentRenderer, 96f * pulse, 96f * pulse);

            glowRenderer.transform.localPosition = new Vector3(0f, -14f, 0f);
            coreRenderer.transform.localPosition = new Vector3(0f, -15f, 0f);
            accentRenderer.transform.localPosition = new Vector3(0f, -15f, 0f);

            coreRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 5f);
            accentRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 10f);

            glowRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.1f + Mathf.Sin(Time.time * 6f) * 0.012f);
            coreRenderer.color = WithAlpha(Color.white, 0.6f * fade);
            accentRenderer.color = WithAlpha(Color.white, 0.66f * fade);
        }


        private static void SetRendererPixelSize(SpriteRenderer renderer, float widthPixels, float heightPixels)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            var rect = renderer.sprite.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            renderer.transform.localScale = new Vector3(widthPixels / rect.width, heightPixels / rect.height, 1f);
        }

        private static bool UsesCustomFxArt(mlpCharacterSkillType skillType)
        {
            return !string.IsNullOrEmpty(GetCustomImageKey(skillType, false));
        }

        private static Sprite LoadSkillSprite(mlpCharacterSkillType skillType, bool accent)
        {
            var customImageKey = GetCustomImageKey(skillType, accent);
            if (!string.IsNullOrEmpty(customImageKey))
            {
                var customSprite = mlpGameplaySpriteLoader.LoadImageSprite(customImageKey, 0.5f, 0.5f);
                if (customSprite != null)
                {
                    return customSprite;
                }
            }

            var legacySpriteName = accent ? GetAccentSpriteName(skillType) : GetCoreSpriteName(skillType);
            return DBLiteFactory.Instance.GetTextureSprite(legacySpriteName);
        }

        private static string GetCustomImageKey(mlpCharacterSkillType skillType, bool accent)
        {
            switch (skillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    return accent ? mlpAssets.Images.SkillFxImages.ReaperDashAccent : mlpAssets.Images.SkillFxImages.ReaperDashCore;
                case mlpCharacterSkillType.BadLuck:
                    return accent ? mlpAssets.Images.SkillFxImages.BadLuckAccent : mlpAssets.Images.SkillFxImages.BadLuckCore;
                default:
                    return null;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static string GetCoreSpriteName(mlpCharacterSkillType skillType)
        {
            switch (skillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    return "fx_smoke_7";
                case mlpCharacterSkillType.CarnivalJackpot:
                    return "dbanims/circle3";
                case mlpCharacterSkillType.GhostSail:
                    return "fx_smoke_4";
                case mlpCharacterSkillType.BloodMoonBlink:
                    return "fx_smoke_0";
                case mlpCharacterSkillType.WaxOverdrive:
                    return "fx_fire_2";
                case mlpCharacterSkillType.BadLuck:
                    return "fx_smoke_6";
                default:
                    return "fx_smoke_1";
            }
        }

        private static string GetAccentSpriteName(mlpCharacterSkillType skillType)
        {
            switch (skillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    return "fx_spl_0";
                case mlpCharacterSkillType.CarnivalJackpot:
                    return "fx_Blur_mol2";
                case mlpCharacterSkillType.GhostSail:
                    return "fx_Blur_mol1";
                case mlpCharacterSkillType.BloodMoonBlink:
                    return "fx_spl2_0";
                case mlpCharacterSkillType.WaxOverdrive:
                    return "fx_Blur_mol4";
                case mlpCharacterSkillType.BadLuck:
                    return "dbanims/eye34635";
                default:
                    return "fx_Blur_mol0";
            }
        }
    }

    /// <summary>
    /// 球员对象：管理一个球员的全部状态——移动、跳跃、投篮、扣篮、防守、技能、动画、AI 控制等。是比赛中最重要的游戏对象。
    /// </summary>
    public sealed class mlpPlayerObject
    {
        // 地面碰撞时用于按质量比例推开的球员质量。
        private const float GroundCollisionMass = 3f;
        // 地面盖帽碰撞时使用的较大虚拟质量。
        private const float GroundBlockCollisionMass = 6f;
        // 地面碰撞速度阈值，低于该值时视作静止处理。
        private const float GroundCollisionSpeedEpsilon = 5f;
        // 球员主图形的基础渲染深度。
        private const float GraphicDepthBase = 0.12f;
        // 球员阴影的基础渲染深度。
        private const float ShadowDepthBase = 0.02f;
        // 同队不同球员之间的渲染深度步长。
        private const float TeamDepthStep = 0.01f;
        // 同队内不同球员编号之间的细微渲染深度步长。
        private const float PlayerDepthStep = 0.0025f;
        // 阴影渲染深度偏移的缩放系数。
        private const float ShadowDepthBiasScale = 0.25f;
        // 教程补扣的接球 X 容差范围。
        private const float TutorialPutbackCatchWindowX = 190f;
        // 教程补扣的接球 Y 容差范围。
        private const float TutorialPutbackCatchWindowY = 230f;
        // 教程补扣允许扣篮的额外 Y 高度。
        private const float TutorialPutbackDunkYBonus = 96f;
        // 教程补扣要求的篮球最低高度。
        private const float TutorialPutbackMinBallY = mlpObjectsData.BasketHeight + 22f;
        // 教程补扣要求的篮球最高竖直速度。
        private const float TutorialPutbackMaxBallVelocityY = 560f;
        // 教程补扣默认成功率。
        private const float TutorialPutbackCompletionChance = 1f;
        // 篮板磁铁的默认持续时间。
        private const float ReboundMagnetDefaultDuration = 1.55f;
        // 篮板磁铁的水平吸附距离。
        private const float ReboundMagnetCatchDistanceX = 52f;
        // 篮板磁铁的垂直吸附距离。
        private const float ReboundMagnetCatchDistanceY = 72f;
        // 篮板磁铁吸球所需的最小速度。
        private const float ReboundMagnetMinSpeed = 560f;
        // 篮板磁铁吸球的最大速度上限。
        private const float ReboundMagnetMaxSpeed = 920f;
        // 必定盖帽后的悬停持续时间。
        private const float GuaranteedBlockHoldDuration = 0.22f;
        // 必定盖帽时角色位置的水平偏移。
        private const float GuaranteedBlockHorizontalOffset = 20f;
        // 必定盖帽时手部碰撞点的垂直偏移。
        private const float GuaranteedBlockHandsOffsetY = 64f;

        private enum BlockPumpPhase
        {
            None,
            Starting,
            Holding,
            Ending
        }

        private enum SuperPhase
        {
            None,
            MegaTravel,
            MegaRecover,
            SuperDashTravel,
            AlleyTeleportOut,
            AlleyTeleportIn,
            GuaranteedBlockHold
        }

        // 球员主视觉根节点。
        private readonly GameObject graphic;
        // 球员阴影根节点。
        private readonly GameObject shadow;
        // 球员阴影渲染器。
        private readonly SpriteRenderer shadowRenderer;
        // 默认阴影精灵。
        private readonly Sprite defaultShadowSprite;
        // 技能激活时使用的阴影精灵。
        private readonly Sprite activeSkillShadowSprite;
        // 球员骨骼动画对象。
        private readonly DBLiteArmature armature;
        // 球员控制器接口。
        private readonly IBLPlayerController controller;
        // 所属队伍索引。
        private readonly int teamIndex;
        // 角色 ID。
        private readonly int characterId;
        // 队内球员编号。
        private readonly int playerNo;
        // 渲染深度偏移，用于同队球员错层显示。
        private readonly float renderDepthBias;
        // 技能等级。
        private readonly int skillLevel;
        // 角色技能定义。
        private readonly mlpCharacterSkillDefinition skillDefinition;
        // 超能力 ID。
        private readonly int superId;
        // AI 难度槽位。
        private readonly int brainSlot;
        // AI 难度调参配置。
        private readonly mlpAIDifficultyTuningProfile aiDifficultyTuning;
        // 是否为地狱难度强化 AI。
        private readonly bool hellEnhanced;
        // 投篮精度基础值。
        private readonly float accuracy;
        // 扣篮成功率基础值。
        private readonly float chanceToCompleteDunk;
        // 超能力冷却时间。
        private readonly float superCoolDown;
        // 超级扣篮的目标 X 坐标。
        private readonly float superDunkX;
        // 超级扣篮落地结束时的 X 坐标。
        private readonly float superDunkEndX;
        // 超级扣篮落地结束时的 Y 坐标。
        private readonly float superDunkEndY;
        // 超级冲刺的两个目标 X 坐标。
        private readonly float[] superDashTargets = new float[2];
        // 冲刺输入/冷却延迟控制器。
        private readonly UseDelay dashDelay;
        // 能量条 UI 视图。
        private readonly mlpEnergyBarView energyBar;
        // 传送特效对象。
        private readonly mlpTeleportFx teleportFx;
        // 护盾特效对象。
        private readonly mlpShieldObject shield;
        // 技能特效对象。
        private readonly mlpPlayerSkillFx skillFx;
        // 地狱难度下超冲刺的额外冷却时长。
        private readonly float hellBonusSuperDashCooldownDuration;
        // 地狱难度下护盾的额外冷却时长。
        private readonly float hellBonusShieldCooldownDuration;
        // 超级冲刺过程中已命中的对手编号集合。
        private readonly HashSet<int> superDashHits = new HashSet<int>();
        // 操作锁定计时器。
        private float actionLatch;
        // 当前动画状态名。
        private string visualState = "";
        // 冲刺计时器。
        private float dashTimer;
        // 当前冲刺方向。
        private int dashDirection;
        // 缓存的冲刺方向。
        private int bufferedDashDirection;
        // 冲刺输入缓冲计时器。
        private float dashBufferTimer;
        // 是否准备好发起冲刺。
        private bool readyForDash;
        // 是否允许执行普通动作。
        private bool canDoAction;
        // 是否存在待执行的地面出手。
        private bool pendingGroundThrow;
        // 是否存在待执行的抢断动作。
        private bool pendingStealAction;
        // 抢断动画是否正在播放。
        private bool stealAnimationActive;
        // 空接是否等待出手。
        private bool alleyOopPendingThrow;
        // 是否处于扣篮状态。
        private bool isDunking;
        // 扣篮球是否已释放。
        private bool dunkReleased;
        // 扣篮计时器。
        private float dunkTimer;
        // 扣篮总时长。
        private float dunkDuration;
        // 扣篮球释放时刻。
        private float dunkReleaseTime;
        // 扣篮时是否隐藏持球插槽。
        private bool dunkBallSlotsHidden;
        // 扣篮起始位置。
        private Vector2 dunkStartPosition;
        // 扣篮目标位置。
        private Vector2 dunkTargetPosition;
        // 盖帽/假动作当前阶段。
        private BlockPumpPhase blockPumpPhase;
        // 当前动作是否是假动作而不是盖帽。
        private bool blockPumpIsPump;
        // 盖帽/假动作阶段计时器。
        private float blockPumpTimer;
        // 起始动画是否已准备好切换阶段。
        private bool blockPumpStartReady;
        // 结束动画是否已准备好切换阶段。
        private bool blockPumpEndReady;
        // 抢断判定倒计时。
        private float stealAttemptTimer = -1f;
        // 抢断动画剩余时间。
        private float stealAnimationTimer = -1f;
        // 眩晕剩余时间。
        private float stunTimer;
        // 当前面向方向。
        private float facingDirection;
        // 抢断开始时锁定的面向方向。
        private float stealFacingDirection;
        // 是否允许接球/捡球。
        private bool canTakeInHands;
        // 是否允许出手投篮。
        private bool canThrow;
        // 是否处于持球起跳攻击状态。
        private bool attackJump;
        // 最近一次出手位置 X。
        private float pointOfThrow;
        // 是否启用跳跃盖帽判定。
        private bool jumpBlockActive;
        // 当前是否需要准备盖帽。
        private bool needBlock;
        // 超能力是否已经准备好。
        private bool readyForSuper;
        // 是否正在执行超级技能。
        private bool isSuperShot;
        // 是否已暂时移出比赛。
        private bool removedFromPlay;
        // 当前超能充能时间。
        private float superChargeTime;
        // 地狱额外超冲刺冷却计时。
        private float hellBonusSuperDashCooldownTimer;
        // 地狱额外护盾冷却计时。
        private float hellBonusShieldCooldownTimer;
        // 主视觉缩放倍数。
        private float graphicScaleMultiplier = 1f;
        // 当前超能力阶段。
        private SuperPhase superPhase;
        // 超能力阶段计时器。
        private float superTimer;
        // 超能力阶段总时长。
        private float superDuration;
        // 超能力移动起点。
        private Vector2 superStartPosition;
        // 超能力移动目标点。
        private Vector2 superTargetPosition;
        // 超级冲刺方向是否向右。
        private bool dashToRight;
        // 超级冲刺后是否还要处理队友接球。
        private bool dashTeammatePending;
        // 当前队友引用。
        private mlpPlayerObject teamMate;
        // 地狱难度开局充能是否已发放。
        private bool hellOpeningChargeApplied;
        // 是否等待返还原生超级能量。
        private bool hellNativeSuperRefundPending;
        // 必定盖帽后是否暂时锁定拾球。
        private bool guaranteedBlockPickupLocked;
        // 得分升级是否已激活。
        private bool scoreUpgradeActive;
        // 得分升级是否等待当前出手结算。
        private bool scoreUpgradePendingShot;
        // 教程完美投篮是否已预备。
        private bool tutorialPerfectShotPrimed;
        // 教程完美扣篮是否已预备。
        private bool tutorialPerfectDunkPrimed;
        // 教程补扣是否已预备。
        private bool tutorialPutbackDunkPrimed;
        // 教程扣篮成功率覆盖值。
        private float tutorialDunkCompletionChanceOverride = -1f;
        // 教程空中运动时间倍率。
        private float tutorialAirMotionTimeScale = 1f;
        // 教程跳跃盖帽辅助是否开启。
        private bool tutorialJumpBlockAssist;
        // 固定得分加成剩余时间。
        private float flatScoreBonusTimer;
        // 固定得分加成点数。
        private int flatScoreBonusPoints;
        // 移动增益剩余时间。
        private float moveBuffTimer;
        // 移动增益是否仍可提供额外得分奖励。
        private bool moveBuffScoreBonusAvailable;
        // 待返还的超级能量比例。
        private float pendingScoreRefundFraction;
        // 得分返还倒计时。
        private float pendingScoreRefundTimer;
        // 篮板磁铁剩余时间。
        private float reboundMagnetTimer;

        // 游戏核心引用。
        public mlpGameCore GameCore { get; }
        // 球员当前位置。
        public Vector2 Position;
        // 球员当前速度。
        public Vector2 Velocity;
        // 球员所在场地侧。
        public int Side { get; }
        // 当前是否持球。
        public bool WithBall { get; private set; }
        // 是否为人类玩家。
        public bool IsHuman { get; }
        // 当前是否在地面上。
        public bool IsGrounded { get; private set; } = true;
        // 当前进攻目标 X 坐标。
        public float AttackTargetX => Side == -1 ? mlpObjectsData.BasketCenter2 : mlpObjectsData.BasketCenter;
        // 是否正在冲刺。
        public bool IsDashing => dashTimer > 0f;
        // 是否处于盖帽保持阶段。
        public bool IsBlocking => blockPumpPhase == BlockPumpPhase.Holding && !blockPumpIsPump;
        // 是否具备地面盖帽碰撞体。
        public bool HasGroundBlockBody => IsBlocking && IsGrounded && !removedFromPlay && !isSuperShot && stunTimer <= 0f;
        // 是否处于假动作阶段。
        public bool IsPumping => blockPumpPhase != BlockPumpPhase.None && blockPumpIsPump;
        // 是否正在移动。
        public bool IsMoving => Mathf.Abs(Velocity.x) > 20f;
        // 是否正在扣篮。
        public bool IsDunking => isDunking;
        // 当前面向方向。
        public float FacingDirection => facingDirection;
        // 是否允许接球/捡球。
        public bool CanTakeInHands => canTakeInHands && !WithBall && !removedFromPlay;
        // 是否允许执行一般动作。
        public bool CanAct => actionLatch <= 0f && stunTimer <= 0f && !stealAnimationActive && !isDunking && !isSuperShot;
        // 是否满足结算地面盖帽的条件。
        public bool CanResolveGroundBlock => IsGrounded && !removedFromPlay && !isSuperShot && !isDunking && stunTimer <= 0f && !stealAnimationActive;
        // 是否准备好冲刺。
        public bool ReadyForDash => readyForDash && dashTimer <= 0f && !isSuperShot;
        // 队内球员编号。
        public int PlayerNo => playerNo;
        // 技能等级。
        public int SkillLevel => skillLevel;
        // 超能力 ID。
        public int SuperId => superId;
        // 角色 ID。
        public int CharacterId => characterId;
        // 技能类型。
        public mlpCharacterSkillType SkillType => skillDefinition.SkillType;
        // 是否使用控球类技能。
        public bool UsesPossessionSkill => skillDefinition.UsesPossessionSkill;
        // 是否使用冲刺类技能。
        public bool UsesDashSkill => skillDefinition.UsesDashSkill;
        // 是否使用护盾类技能。
        public bool UsesShieldSkill => skillDefinition.UsesBasketShield;
        // 是否使用冻结类技能。
        public bool UsesFreezeSkill => skillDefinition.UsesFreezeSkill;
        // 是否使用篮板磁铁技能。
        public bool UsesReboundMagnetSkill => skillDefinition.UsesReboundMagnetSkill;
        // 是否使用必定盖帽技能。
        public bool UsesGuaranteedBlockSkill => skillDefinition.UsesGuaranteedBlockSkill;
        // 是否准备好释放超能力。
        public bool ReadyForSuper => !isSuperShot && (readyForSuper || mlpQuickTestSettings.Enabled);
        // 是否还能使用地狱额外超冲刺。
        public bool CanUseHellBonusSuperDash => hellEnhanced && (mlpQuickTestSettings.Enabled || hellBonusSuperDashCooldownTimer <= 0f);
        // 是否还能使用地狱额外护盾。
        public bool CanUseHellBonusShield => hellEnhanced && shield != null && (mlpQuickTestSettings.Enabled || hellBonusShieldCooldownTimer <= 0f) && shield.CanActivate;
        // 是否正在执行超能出手。
        public bool IsSuperShot => isSuperShot;
        // 当前是否需要防守盖帽。
        public bool NeedBlock => needBlock;
        // 是否允许出手。
        public bool CanThrow => canThrow;
        // 球员控制器引用。
        public IBLPlayerController Controller => controller;
        // 是否使用高亮技能阴影。
        private bool UsesHighlightedSkillShadow => skillDefinition.SkillType == mlpCharacterSkillType.CarnivalJackpot && (scoreUpgradeActive || scoreUpgradePendingShot);
        // 实际生效的超能力冷却时间。
        private float EffectiveSuperCoolDown => mlpQuickTestSettings.Enabled ? 0f : superCoolDown;

        public void ApplyBonusSuperCharge(float amount)
        {
            // 1. 获取超级技能的冷却时间
            var cooldown = EffectiveSuperCoolDown;
            // 2. 如果冷却时间为0（快速测试模式），直接刷新
            if (cooldown <= 0f)
            {
                RefreshQuickTestSuperReady();
                return;
            }

            // 3. 如果充能无效或已准备好或正在使用超级技能，直接返回
            if (amount <= 0f || readyForSuper || isSuperShot)
            {
                return;
            }

            // 4. 增加充能时间（不超过冷却时间上限）
            superChargeTime = Mathf.Min(cooldown, superChargeTime + amount);
            // 5. 更新能量条UI显示
            energyBar?.SetCharge(superChargeTime / cooldown);
            // 6. 如果充能已满，标记超级技能就绪
            if (superChargeTime >= cooldown)
            {
                readyForSuper = true;
                // 7. 人类玩家播放充能完成音效
                if (IsHuman)
                {
                    mlpAudio.Instance?.Play(mlpAssets.Sounds.PEnergy);
                }
            }
        }

        private void RefreshQuickTestSuperReady()
        {
            // 1. 如果未启用快速测试模式或正在使用超级技能，直接返回
            if (!mlpQuickTestSettings.Enabled || isSuperShot)
            {
                return;
            }

            // 2. 标记超级技能就绪
            readyForSuper = true;
            // 3. 将充能时间设为满值
            superChargeTime = superCoolDown;
            // 4. 将能量条UI设为满格
            energyBar?.SetCharge(1f);
        }

        /// <summary>
        /// 设置玩家角色，包括精灵、阴影、骨骼动画、控制器和技能特效。
        /// </summary>
        /// <param name="gameCore">中央游戏逻辑协调器</param>
        /// <param name="teamIndex">队伍索引（0 为左侧，1 为右侧）</param>
        /// <param name="characterId">用于查找技能定义的角色标识符</param>
        /// <param name="playerNo">队伍内的玩家编号（0 或 1）</param>
        /// <param name="playerBrain">决定控制器类型的脑字符串</param>
        /// <param name="skillLevel">AI 四档技能索引（0 = Easy，1 = Normal，2 = Hard，3 = Hell）；人类玩家保留基础手感配置。</param>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpPlayerObject(mlpGameCore gameCore, int teamIndex, int characterId, int playerNo, string playerBrain, int skillLevel, Transform parent)
        {
            // 1. 保存基本身份信息：所属队伍、角色编号、球员编号、技能等级
            GameCore = gameCore;
            this.teamIndex = teamIndex;
            this.characterId = characterId;
            this.playerNo = playerNo;
            this.skillLevel = skillLevel;
            Side = teamIndex == 0 ? -1 : 1;
            renderDepthBias = teamIndex * TeamDepthStep + playerNo * PlayerDepthStep;

            // 2. 判断是人类玩家还是 AI，解析控制器按键槽位
            IsHuman = !playerBrain.StartsWith("B") && !playerBrain.StartsWith("T");
            brainSlot = mlpControlsData.ParseControllerSlot(playerBrain);

            // 3. 加载角色技能定义和超能力 ID，获取 AI 难度参数
            skillDefinition = mlpCharacterSkillsData.Get(characterId);
            superId = skillDefinition.IconSuperId;
            aiDifficultyTuning = mlpAIDifficultyTuning.Get(mlpInventory.Instance.Difficulty);
            hellEnhanced = !IsHuman && mlpInventory.Instance.Difficulty == mlpAiDifficulty.Hell;

            // 4. 读取投篮、扣篮和超能力冷却参数
            //    - 人类玩家使用固定的基础手感，避免 AI 难度切换影响玩家自己的操作体验
            //    - AI 使用四档技能索引，索引只允许 Easy/Normal/Hard/Hell 四种含义
            var profile = IsHuman
                ? mlpAISkillsData.GetHumanPlayerProfile()
                : mlpAISkillsData.Get(skillLevel);
            accuracy = profile.Accuracy;
            chanceToCompleteDunk = profile.ChanceToCompleteDunk;
            superCoolDown = profile.CoolDown;
            dashDelay = new UseDelay(mlpObjectsData.DashDelay * (hellEnhanced ? aiDifficultyTuning.DashCooldownMultiplier : 1f));

            // 5. Hell 难度额外加成：超能力冲刺和护盾的冷却时间
            //    当前难度只保留 Easy/Normal/Hard/Hell 四档，Hell 已经是最高档，
            //    所以不再按技能数值拆出额外的隐藏对手档位。
            hellBonusSuperDashCooldownDuration = hellEnhanced
                ? aiDifficultyTuning.BonusSuperDashCooldown
                : 0f;
            hellBonusShieldCooldownDuration = hellEnhanced
                ? aiDifficultyTuning.BonusShieldCooldown
                : 0f;

            // 6. 根据所在侧设置超能力扣篮位置和冲刺目标坐标
            if (Side == 1)
            {
                superDunkX = mlpObjectsData.AlleyOopX;
                superDashTargets[0] = mlpObjectsData.SuperDashX1;
                superDashTargets[1] = mlpObjectsData.SuperDashX2 + 130f;
            }
            else
            {
                superDunkX = mlpConstants.Width - mlpObjectsData.AlleyOopX;
                superDashTargets[0] = mlpObjectsData.SuperDashX2;
                superDashTargets[1] = mlpObjectsData.SuperDashX1 - 130f;
            }
            superDunkEndX = DunkTargetX() + 20f * Side;
            superDunkEndY = mlpObjectsData.DunkY + 30f;

            // 7. 创建球员角色 GameObject（用于承载骨骼动画）
            graphic = new GameObject($"Player_{teamIndex}_{playerNo}");
            graphic.transform.SetParent(parent, false);

            // 8. 创建球员阴影：加载阴影精灵图，设置渲染层级
            shadow = new GameObject($"PlayerShadow_{teamIndex}_{playerNo}");
            shadow.transform.SetParent(parent, false);
            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            defaultShadowSprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                playerNo == 0
                    ? mlpAssets.Images.GameplayImages.PlayerShadowPrimary
                    : mlpAssets.Images.GameplayImages.PlayerShadowSecondary,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                playerNo == 0 ? "ShadowMC0000" : "ShadowMC0001");
            shadowRenderer.sprite = defaultShadowSprite;
            // 狂欢节大奖技能使用红色阴影，其他角色用默认阴影
            activeSkillShadowSprite = skillDefinition.SkillType == mlpCharacterSkillType.CarnivalJackpot
                ? mlpGameplaySpriteLoader.LoadGameplaySprite(
                    mlpAssets.Images.GameplayImages.PlayerShadowPrimaryRed,
                    0.5f,
                    0.5f,
                    mlpAtlasCache.Instance.Gameplay,
                    "ShadowMC0000") ?? defaultShadowSprite
                : defaultShadowSprite;
            shadowRenderer.sortingOrder = 2;

            // 9. 创建骨骼动画（armature），设置像素对齐位置和缩放，应用角色外观
            armature = mlpPlayersData.BuildGameplayArmature($"playerSmall_{teamIndex}_{playerNo}");
            if (armature != null)
            {
                armature.transform.SetParent(graphic.transform, false);
                armature.transform.localPosition = mlpConstants.SnapLocalPositionToScreenPixels(
                    graphic.transform,
                    new Vector3(0f, -35f, 0f));
                armature.transform.localScale = new Vector3(
                    mlpConstants.PixelPerfectCharacterScale,
                    mlpConstants.PixelPerfectCharacterScale,
                    1f);
                mlpPlayersData.ApplyCharacter(armature, characterId);
                // 10. 订阅动画完成和帧事件回调（用于扣篮释放、技能触发等）
                armature.AnimationComplete += OnAnimationComplete;
                armature.FrameEvent += OnAnimationFrameEvent;
            }
            else
            {
                CreateFallbackAvatar();
            }

            // 11. 创建控制器：人类用键盘，AI 用对应的脑决策器，教程用教程控制器
            controller = IsHuman
                ? new mlpKeyboardController(playerBrain)
                : playerBrain.Length > 0 && (playerBrain[0] == 'T' || playerBrain[0] == 't')
                    ? new mlpTutorialOpponentController(this, skillLevel)
                    : mlpAIController.CreateForBrain(this, playerBrain, skillLevel);

            // 12. 创建技能相关视觉组件：能量条（仅人类）、传送特效、护盾、技能光效
            energyBar = IsHuman ? new mlpEnergyBarView(parent, brainSlot, skillDefinition, superCoolDown) : null;
            teleportFx = (skillDefinition.UsesTeleportDunk || skillDefinition.UsesGuaranteedBlockSkill) ? new mlpTeleportFx(parent, skillDefinition) : null;
            shield = skillDefinition.UsesBasketShield || hellEnhanced
                ? new mlpShieldObject(Side, Side == -1 ? gameCore.BasketLeft : gameCore.BasketRight, parent, skillDefinition)
                : null;
            skillFx = new mlpPlayerSkillFx(parent, skillDefinition);

            // 13. 重置所有状态到初始值（位置、冲刺、眩晕、扣篮等）
            Restart(0);
        }

        /// <summary>
        /// 比赛结束时清理仅在运行时使用的资源（如能量条）。
        /// </summary>
        public void ReleaseRuntimeResources()
        {
            energyBar?.ReleaseRuntimeResources();
        }

        /// <summary>
        /// 为新一轮重置所有玩家状态：位置、冲刺、眩晕、扣篮、超能力和技能计时器。
        /// </summary>
        /// <param name="startSide">重置后玩家所在的起始侧</param>
        public void Restart(int startSide)
        {
            // 1. 重置持球和移动状态
            WithBall = false;
            Velocity = Vector2.zero;

            // 2. 重置冲刺相关状态（计时器、方向、缓冲输入）
            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            readyForDash = false;
            dashDelay.Activate();
            canDoAction = true;

            // 3. 重置投篮和抢断标记
            pendingGroundThrow = false;
            pendingStealAction = false;
            stealAnimationActive = false;
            alleyOopPendingThrow = false;

            // 4. 重置扣篮状态（计时器、释放标记、球槽可见性）
            isDunking = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = 0f;
            dunkReleaseTime = 0f;
            SetDunkBallSlotsHidden(false);

            // 5. 重置盖帽动画阶段
            blockPumpPhase = BlockPumpPhase.None;
            blockPumpIsPump = false;
            blockPumpTimer = 0f;
            blockPumpStartReady = false;
            blockPumpEndReady = false;

            // 6. 重置抢断计时器和眩晕
            stealAttemptTimer = -1f;
            stealAnimationTimer = -1f;
            stunTimer = 0f;
            actionLatch = 0f;

            // 7. 重置朝向和操作许可
            facingDirection = -Side;
            stealFacingDirection = facingDirection;
            canTakeInHands = true;
            canThrow = true;
            attackJump = false;
            jumpBlockActive = false;
            needBlock = false;
            removedFromPlay = false;
            graphicScaleMultiplier = 1f;

            // 8. 重置超能力阶段和冲刺状态
            superPhase = SuperPhase.None;
            superTimer = 0f;
            superDuration = 0f;
            dashToRight = false;
            dashTeammatePending = false;
            teamMate = GameCore.GetTeamMate(Side, playerNo);
            superDashHits.Clear();
            isSuperShot = false;
            hellNativeSuperRefundPending = false;
            guaranteedBlockPickupLocked = false;

            // 9. 重置教程和特殊得分加成标记
            scoreUpgradeActive = false;
            scoreUpgradePendingShot = false;
            tutorialPerfectShotPrimed = false;
            tutorialPerfectDunkPrimed = false;
            tutorialPutbackDunkPrimed = false;
            tutorialDunkCompletionChanceOverride = -1f;
            tutorialAirMotionTimeScale = 1f;
            tutorialJumpBlockAssist = false;
            flatScoreBonusTimer = 0f;
            flatScoreBonusPoints = 0;
            moveBuffTimer = 0f;
            moveBuffScoreBonusAvailable = false;
            pendingScoreRefundFraction = 0f;
            pendingScoreRefundTimer = 0f;
            reboundMagnetTimer = 0f;
            GameCore.IsSuperShot = false;

            // 10. 隐藏所有技能特效
            teleportFx?.Hide();
            shield?.Reset();
            skillFx?.Stop();

            // 11. 根据所在侧和起始位置计算球员的初始 X 坐标
            var x = mlpConstants.Width2 + Side * (playerNo == 0 ? mlpObjectsData.PlayerIndentX : 200f);
            if (startSide == Side)
            {
                x = Side == -1 ? mlpObjectsData.IndentGeneralX : mlpConstants.Width - mlpObjectsData.IndentGeneralX;
            }

            // 12. 设置初始位置、落地标记、超能力充能（地狱难度开局自带部分充能）
            Position = new Vector2(x, mlpObjectsData.PlayerIndentY);
            pointOfThrow = Position.x;
            IsGrounded = true;
            var cooldown = EffectiveSuperCoolDown;
            if (!hellOpeningChargeApplied && hellEnhanced && cooldown > 0f)
            {
                superChargeTime = Mathf.Max(superChargeTime, cooldown * aiDifficultyTuning.OpeningSuperChargeFraction);
                hellOpeningChargeApplied = true;
            }

            if (cooldown <= 0f)
            {
                readyForSuper = true;
                superChargeTime = superCoolDown;
            }
            else
            {
                superChargeTime = Mathf.Clamp(superChargeTime, 0f, cooldown);
                readyForSuper = superChargeTime >= cooldown;
            }

            PlayState("idle");
            controller.Restart(startSide);
            energyBar?.SetCharge(cooldown <= 0f ? 1f : superChargeTime / cooldown);
            UpdateGraphic();
        }

        /// <summary>
        /// 运行完整的玩家更新循环：输入、物理、冲刺、跳跃、投掷、抢断、盖帽和动画。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void Update(float dt)
        {
            // 1. 更新所有技能特效（传送、护盾、技能计时器、篮板磁铁、技能光效）
            teleportFx?.Update(dt);
            shield?.Update(dt);
            UpdateSkillTimers(dt);
            UpdateReboundMagnet(dt);
            skillFx?.Update(dt, Position, facingDirection, !removedFromPlay && graphicScaleMultiplier > 0.05f);
            hellBonusSuperDashCooldownTimer = Mathf.Max(0f, hellBonusSuperDashCooldownTimer - dt);
            hellBonusShieldCooldownTimer = Mathf.Max(0f, hellBonusShieldCooldownTimer - dt);
            RefreshQuickTestSuperReady();

            // 2. 超能力充能：每帧累加充能时间，充满后标记就绪并播放音效
            var cooldown = EffectiveSuperCoolDown;
            if (!readyForSuper && !isSuperShot && cooldown > 0f)
            {
                superChargeTime = Mathf.Min(cooldown, superChargeTime + dt);
                energyBar?.SetCharge(superChargeTime / cooldown);
                if (superChargeTime >= cooldown)
                {
                    readyForSuper = true;
                    if (IsHuman)
                    {
                        mlpAudio.Instance?.Play(mlpAssets.Sounds.PEnergy);
                    }
                }
            }

            // 3. 递减动作冷却计时器，当控制器准备好时恢复动作许可
            actionLatch -= dt;
            if (!canDoAction && !stealAnimationActive && controller.ReadyForAction())
            {
                canDoAction = true;
            }

            // 4. 冲刺冷却计时：冷却结束后标记可以再次冲刺
            if (!readyForDash && dashDelay.Update(dt) == 1)
            {
                readyForDash = true;
            }

            // 5. 如果正在播放超能力动画，进入超能力专用更新流程
            if (isSuperShot)
            {
                UpdateSuper(dt);
                return;
            }

            // 6. 如果正在扣篮，进入扣篮专用更新流程
            if (isDunking)
            {
                UpdateDunk(dt);
                return;
            }

            // 7. 眩晕状态：清空所有操作输入，禁止移动，倒计时结束后恢复
            if (stunTimer > 0f)
            {
                stunTimer -= dt;
                stealAttemptTimer = -1f;
                dashTimer = 0f;
                dashDirection = 0;
                bufferedDashDirection = 0;
                dashBufferTimer = 0f;
                pendingGroundThrow = false;
                pendingStealAction = false;
                stealAnimationActive = false;
                stealAnimationTimer = -1f;
                blockPumpPhase = BlockPumpPhase.None;
                blockPumpTimer = 0f;
                jumpBlockActive = false;
                canTakeInHands = false;
                Velocity = Vector2.zero;
                if (stunTimer <= 0f)
                {
                    stunTimer = 0f;
                    canTakeInHands = true;
                    PlayState(WithBall ? "idle_wb" : "idle");
                }

                UpdateGraphic();
                return;
            }

            // 8. 如果正在播放抢断动画，走抢断动画专用流程
            if (stealAnimationActive)
            {
                UpdateStealAnimation(dt);
                return;
            }

            // 9. 读取玩家输入、更新冲刺缓冲、面朝方向、起跳盖帽威胁
            controller.UpdateController(dt);
            UpdateDashBuffer(dt);
            UpdateFacing();
            UpdateJumpBlockThreat();

            // 10. 如果正在盖帽或假动作，走盖帽/假动作专用流程
            if (blockPumpPhase != BlockPumpPhase.None)
            {
                UpdateBlockOrPump(dt);
                return;
            }

            // 11. 抢断倒计时：计时结束后结算抢断结果
            if (stealAttemptTimer >= 0f)
            {
                stealAttemptTimer -= dt;
                if (stealAttemptTimer <= 0f)
                {
                    ResolveStealAttempt();
                }
            }

            // 12. 水平移动：冲刺中按冲刺速度移动，否则按普通移动速度
            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                Velocity.x = GetDashSpeed() * dashDirection;
                PlayState("dash");
                if (dashTimer <= 0f)
                {
                    dashTimer = 0f;
                    dashDirection = 0;
                    readyForDash = false;
                    dashDelay.Activate();
                    controller.PlayerOnDashEnd();
                }
            }
            else
            {
                var moveSpeed = GetMoveSpeed();
                Velocity.x = controller.CurrentMove * moveSpeed;

                // 12a. 检测冲刺输入（直接输入或缓冲输入），满足条件则启动冲刺
                var dashInput = controller.CurrentDash != 0
                    ? controller.CurrentDash
                    : dashBufferTimer > 0f ? bufferedDashDirection : 0;
                if (dashInput != 0 && IsGrounded && readyForDash)
                {
                    StartDash(dashInput);
                }
            }

            // 13. 跳跃输入处理：无球时尝试起跳盖帽，有球时起跳投篮
            if (dashTimer <= 0f && controller.CurrentJump && IsGrounded)
            {
                if (!WithBall && ShouldPrimeJumpBlock())
                {
                    ActivateJumpBlock();
                }

                attackJump = WithBall;
                if (WithBall)
                {
                    pointOfThrow = Position.x;
                }

                Velocity.y = mlpObjectsData.PlayerJump;
                IsGrounded = false;
                canThrow = WithBall;
                PlayState(WithBall ? "jump_wb" : "jump");
                if (WithBall)
                {
                    GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.JumpA, Side, playerNo);
                }
            }

            // 14. 动作键输入处理：有球时投篮，无球时抢断（教程模式有补扣特殊逻辑）
            if (dashTimer <= 0f && controller.CurrentAction && actionLatch <= 0f && canDoAction)
            {
                if (WithBall)
                {
                    if (canThrow && IsGrounded)
                    {
                        BeginFloorThrow();
                    }
                    else if (canThrow)
                    {
                        MakeThrow();
                    }
                }
                else
                {
                    if (TryTutorialPutbackDunk())
                    {
                        UpdateGraphic();
                        return;
                    }

                    if (IsGrounded)
                    {
                        BeginSteal();
                    }
                }
            }

            // 15. 盖帽/假动作键输入：在地面且冷却结束时触发
            if (dashTimer <= 0f && controller.CurrentBlockOrPump && IsGrounded && actionLatch <= 0f)
            {
                BeginBlockOrPump();
            }

            // 16. 超能力输入：满足条件时触发超能力（扣篮、冲刺、传送等）
            if (TryStartSuper(controller.CurrentSuper))
            {
                UpdateGraphic();
                return;
            }

            // 17. 应用重力（空中时），然后更新位置并限制在场地边界内
            if (!IsGrounded)
            {
                Velocity.y += mlpObjectsData.Gravity.y * 3f * dt * tutorialAirMotionTimeScale;
            }

            var verticalDt = IsGrounded ? dt : dt * tutorialAirMotionTimeScale;
            Position += new Vector2(Velocity.x * dt, Velocity.y * verticalDt);
            Position.x = Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);

            // 18. 落地检测：到达地面高度时重置跳跃状态，有球跳跃投篮，无球恢复接球能力
            if (Position.y >= mlpObjectsData.PlayerIndentY)
            {
                Position.y = mlpObjectsData.PlayerIndentY;
                Velocity.y = 0f;
                if (!IsGrounded)
                {
                    jumpBlockActive = false;
                    if (WithBall && attackJump)
                    {
                        MakeThrow();
                        canTakeInHands = true;
                    }
                    else
                    {
                        canThrow = true;
                        controller.PlayerOnGround();
                        guaranteedBlockPickupLocked = false;
                        if (!WithBall)
                        {
                            canTakeInHands = true;
                        }
                    }

                    IsGrounded = true;
                    PlayState(WithBall ? "landing_wb" : "landing");
                }
            }

            // 19. 播放地面动画：根据速度选择跑步或待机动画
            if (IsGrounded && dashTimer <= 0f && actionLatch <= 0f && !stealAnimationActive)
            {
                if (Mathf.Abs(Velocity.x) > 5f)
                {
                    PlayState(WithBall ? "run_wb" : "run");
                }
                else
                {
                    PlayState(WithBall ? "idle_wb" : "idle");
                }
            }

            // 20. 持球时让篮球跟随球员，否则恢复篮球接球检测
            if (WithBall)
            {
                GameCore.Ball.TakeInHands(Side);
            }
            else
            {
                RestoreBallPickupIfReady();
            }

            // 21. 将球员和阴影精灵移动到当前物理位置
            UpdateGraphic();
        }

        /// <summary>
        /// 在赛前倒计时期间更新冷却时间和输入就绪状态，不移动玩家。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void TickPreMatch(float dt)
        {
            // 1. 更新传送特效和护盾特效的动画
            teleportFx?.Update(dt);
            shield?.Update(dt);

            // 2. 减少动作锁定计时器
            if (actionLatch > 0f)
            {
                actionLatch -= dt;
            }

            // 3. 如果控制器已就绪且未在偷球动画中，解锁动作
            if (!canDoAction && !stealAnimationActive && controller.ReadyForAction())
            {
                canDoAction = true;
            }

            // 4. 如果冲刺延迟结束，标记冲刺就绪
            if (!readyForDash && dashDelay.Update(dt) == 1)
            {
                readyForDash = true;
            }
        }

        /// <summary>
        /// 拾取篮球，更新控制器信号，并播放持球动画。
        /// </summary>
        public void TakeBallInHands()
        {
            // 1. 标记球员持球，重置盖帽和接球状态
            WithBall = true;
            jumpBlockActive = false;
            canTakeInHands = false;
            // 2. 只有在地面或篮筐下方才能投篮
            canThrow = IsGrounded || IsUnderGlass();
            attackJump = false;
            // 3. 如果在篮筐下方，记录出手位置
            if (IsUnderGlass())
            {
                pointOfThrow = Position.x;
            }

            // 4. 取消偷球动画并清除眩晕计时器
            CancelStealAnimation(false);
            stunTimer = 0f;
            // 5. 通知篮球对象进入持球状态
            if (!removedFromPlay)
            {
                GameCore.Ball.TakeInHands(Side);
            }

            // 6. 通知游戏核心并播放持球动画
            GameCore.NotifyBallInHands(Side, playerNo);
            PlayState(IsGrounded ? "idle_wb" : "fly1_wb");
        }

        /// <summary>
        /// 将玩家瞬间放置在指定位置和朝向，用于教程中的脚本化时刻。
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <param name="facing">朝向方向（-1 或 1）</param>
        public void TutorialSnapTo(Vector2 position, float facing)
        {
            // 1. 将球员瞬间移动到指定位置，速度归零
            Position = position;
            Velocity = Vector2.zero;
            // 2. 更新出手位置记录
            pointOfThrow = Position.x;
            // 3. 如果指定了朝向，更新球员面向方向
            if (!Mathf.Approximately(facing, 0f))
            {
                facingDirection = Mathf.Sign(facing);
                stealFacingDirection = facingDirection;
            }

            // 4. 更新球员图形位置
            UpdateGraphic();
        }

        /// <summary>
        /// 立即将超能力量表充满，使玩家可以激活技能。
        /// </summary>
        public void TutorialChargeSuper()
        {
            // 1. 获取超级技能的冷却时间
            var cooldown = EffectiveSuperCoolDown;
            // 2. 如果冷却时间为0，直接用默认值充满
            if (cooldown <= 0f)
            {
                readyForSuper = true;
                superChargeTime = superCoolDown;
                energyBar?.SetCharge(1f);
                return;
            }

            // 3. 将充能时间设为满值，标记超级技能就绪
            readyForSuper = true;
            superChargeTime = cooldown;
            // 4. 更新能量条UI为满格
            energyBar?.SetCharge(1f);
        }

        /// <summary>
        /// 启用教程完美投篮标志，使下一次投篮完全精准。
        /// </summary>
        public void TutorialPrimePerfectShot()
        {
            tutorialPerfectShotPrimed = true;
        }

        public void TutorialPrimePerfectDunk()
        {
            tutorialPerfectDunkPrimed = true;
            tutorialDunkCompletionChanceOverride = -1f;
        }

        public void TutorialPrimePutbackDunk()
        {
            tutorialPutbackDunkPrimed = true;
            tutorialDunkCompletionChanceOverride = Mathf.Max(tutorialDunkCompletionChanceOverride, TutorialPutbackCompletionChance);
        }

        public bool IsTutorialPutbackBallInWindow(mlpBallObject ball)
        {
            var reboundState = ball != null && (ball.State == "basket" || ball.State == "bounce");
            return ball != null &&
                   ball.IsInGame &&
                   reboundState &&
                   ball.Position.y >= TutorialPutbackMinBallY &&
                   ball.Velocity.y <= TutorialPutbackMaxBallVelocityY;
        }

        public void TutorialSetAirMotionTimeScale(float scale)
        {
            tutorialAirMotionTimeScale = Mathf.Clamp(scale, 0.35f, 1f);
        }

        public void TutorialSetJumpBlockAssist(bool active)
        {
            tutorialJumpBlockAssist = active;
        }

        /// <summary>
        /// 从该球员手中释放篮球但不投掷，重置投掷相关状态。
        /// </summary>
        public void FreeBall()
        {
            // 1. 如果球员没有持球，直接返回
            if (!WithBall)
            {
                return;
            }

            // 2. 重置持球和相关战斗状态
            WithBall = false;
            jumpBlockActive = false;
            canThrow = false;
            attackJump = false;
            // 3. 取消偷球动画
            CancelStealAnimation(false);
            // 4. 根据是否在地面播放对应的待机动画
            if (IsGrounded)
            {
                PlayState("idle");
            }
            else
            {
                canDoAction = false;
                PlayState("fly1");
            }
        }

        private void DropHeldBallForFreeze()
        {
            // 1. 如果球员没有持球或篮球不存在，直接返回
            if (!WithBall || GameCore.Ball == null)
            {
                return;
            }

            // 2. 释放篮球控制权
            FreeBall();
            // 3. 将篮球从球员位置下方放下（冻结时掉落效果）
            GameCore.Ball.DropFromFreeze(Position + new Vector2(0f, -45f));
        }

        /// <summary>
        /// 处理篮球变为自由状态：重置投掷/盖帽状态并通知控制器。
        /// </summary>
        public void NotifyBallLoose()
        {
            // 1. 标记球员不再持球
            WithBall = false;
            // 2. 取消偷球动画并清除眩晕
            CancelStealAnimation(false);
            stunTimer = 0f;
            // 3. 重置投掷和攻击跳起状态
            canThrow = false;
            attackJump = false;
            // 4. 允许重新捡球
            canTakeInHands = true;
            // 5. 重置盖帽状态
            jumpBlockActive = false;
            needBlock = false;
            // 6. 通知控制器篮球已不在手中
            controller.BallOthers();
        }

        /// <summary>
        /// 通知控制器有人拾取了篮球，更新进攻/防守策略。
        /// </summary>
        /// <param name="holderSide">拾取篮球的球员所在侧</param>
        /// <param name="holderPlayerNo">当前持球者的球员编号</param>
        public void NotifyBallInHands(int holderSide, int holderPlayerNo)
        {
            // 1. 如果有待处理的得分升级，清除它
            if (scoreUpgradePendingShot)
            {
                ClearScoreUpgrade();
            }

            // 2. 根据持球者是否是己方，通知控制器不同策略
            if (holderSide == Side)
            {
                // 3. 己方持球：切换到进攻策略
                controller.BallInOwnHands(holderPlayerNo);
                needBlock = false;
            }
            else
            {
                // 4. 对方持球：切换到防守策略，准备盖帽
                controller.BallInOpponentsHands(holderPlayerNo);
                needBlock = true;
            }
        }

        /// <summary>
        /// 通知控制器有人投篮了，更新篮板和盖帽策略。
        /// </summary>
        /// <param name="shotSide">投篮球员所在侧</param>
        /// <param name="shooterPlayerNo">投篮球员的编号</param>
        public void NotifyBallShot(int shotSide, int shooterPlayerNo)
        {
            // 1. 如果自己有得分升级且是自己投篮，标记等待确认
            if (scoreUpgradeActive && shotSide == Side && shooterPlayerNo == playerNo)
            {
                scoreUpgradeActive = false;
                scoreUpgradePendingShot = true;
            }

            // 2. 根据投篮方是否是己方，通知控制器不同策略
            if (shotSide == Side)
            {
                // 3. 己方投篮：切换到己方出手策略
                controller.BallOwnShoot(shooterPlayerNo);
                needBlock = false;
            }
            else
            {
                // 4. 对方投篮：切换到对方出手策略，准备盖帽
                controller.BallOpponentShoot(shooterPlayerNo);
                needBlock = true;
            }
        }

        /// <summary>
        /// 通知控制器篮球处于中立状态（未持球，未投篮）。
        /// </summary>
        public void NotifyBallOthers()
        {
            needBlock = false;
            jumpBlockActive = false;
            controller.BallOthers();
        }

        /// <summary>
        /// 尝试在玩家满足条件时激活超能技能。
        /// </summary>
        /// <returns>成功激活时返回 true；否则返回 false。</returns>
        public bool SuperShot()
        {
            return TryStartSuper(true);
        }

        /// <summary>
        /// 篮球到达接球点后开始空接超能的传送出场阶段。
        /// </summary>
        public void ContinueAlleyOop()
        {
            if (!isSuperShot || superPhase != SuperPhase.None)
            {
                return;
            }

            teleportFx?.StartPlay(Position.x, Position.y);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            removedFromPlay = true;
            superPhase = SuperPhase.AlleyTeleportOut;
            superTimer = 0f;
            superDuration = 0.4f;
        }

        /// <summary>
        /// 检测玩家的护盾技能是否能阻挡给定的篮球。
        /// </summary>
        /// <param name="ball">要检测或影响的篮球对象</param>
        /// <returns>成功阻挡时返回 true；否则返回 false。</returns>
        public bool TryShieldBall(mlpBallObject ball)
        {
            return shield != null && shield.TryBlockBall(ball);
        }

        public void ApplyFreeze(float duration, mlpCharacterSkillDefinition freezeDefinition)
        {
            // 1. 如果持续时间无效或球员不在比赛中或正在超级技能/扣篮，直接返回
            if (duration <= 0f || removedFromPlay || isSuperShot || isDunking)
            {
                return;
            }

            // 2. 如果持球则掉落篮球
            DropHeldBallForFreeze();
            // 3. 设置眩晕计时器（取较大值避免缩短已有眩晕）
            stunTimer = Mathf.Max(stunTimer, duration);
            // 4. 重置冲刺相关状态
            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            // 5. 重置待处理的动作和偷球状态
            pendingGroundThrow = false;
            pendingStealAction = false;
            CancelStealAnimation(false);
            // 6. 重置盖帽蓄力状态
            blockPumpPhase = BlockPumpPhase.None;
            blockPumpTimer = 0f;
            jumpBlockActive = false;
            // 7. 禁止所有动作和捡球
            canDoAction = false;
            canTakeInHands = false;
            // 8. 停止移动
            Velocity = Vector2.zero;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            // 9. 播放眩晕动画
            PlayState("stun");
            // 10. 播放冰冻特效、音效并显示提示
            skillFx?.PlayBurst(Mathf.Min(duration, 0.8f), freezeDefinition);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Stun, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PStunned, 0.9f);
            GameCore.ShowHudBonusNotice("FROZEN 2 SEC!", 0.95f);
        }

        public int ResolveScorePoints(int basePoints, out string scoreNotice)
        {
            // 1. 初始化输出参数和基础得分
            scoreNotice = null;
            var resolvedPoints = basePoints;

            // 2. 如果有待确认的得分升级，额外加2分（最多5分）
            if (scoreUpgradePendingShot && resolvedPoints >= 2)
            {
                resolvedPoints = Mathf.Min(5, resolvedPoints + 2);
                scoreUpgradePendingShot = false;
                scoreNotice = skillDefinition.ScoreNotice;
                skillFx?.PlayBurst(0.5f);
            }

            // 3. 如果移动增益激活且有额外得分加成，叠加加成
            if (moveBuffTimer > 0f && moveBuffScoreBonusAvailable && skillDefinition.FlatScoreBonus > 0)
            {
                resolvedPoints += skillDefinition.FlatScoreBonus;
                moveBuffScoreBonusAvailable = false;
                moveBuffTimer = 0f;
                scoreNotice = string.IsNullOrEmpty(scoreNotice) ? skillDefinition.ScoreNotice : scoreNotice;
                skillFx?.PlayBurst(0.45f);
            }

            // 4. 如果有临时得分加成效果，叠加加成
            if (flatScoreBonusTimer > 0f && flatScoreBonusPoints > 0)
            {
                resolvedPoints += flatScoreBonusPoints;
                flatScoreBonusPoints = 0;
                flatScoreBonusTimer = 0f;
                scoreNotice = string.IsNullOrEmpty(scoreNotice) ? skillDefinition.ScoreNotice : scoreNotice;
                skillFx?.PlayBurst(0.45f);
            }

            // 5. 返回最终计算的得分
            return resolvedPoints;
        }

        public void OnScoreConfirmed()
        {
            // 1. 如果有待确认的得分超级能量返还，立即发放
            if (pendingScoreRefundTimer > 0f && pendingScoreRefundFraction > 0f)
            {
                // 2. 按比例返还超级能量
                GrantSuperChargeFraction(pendingScoreRefundFraction);
                // 3. 清除返还标记
                pendingScoreRefundFraction = 0f;
                pendingScoreRefundTimer = 0f;
                // 4. 显示返还提示
                GameCore.ShowHudBonusNotice("SUPER REFUND!", 0.95f);
            }
        }

        /// <summary>
        /// 返回地狱难度增强提供的额外抢断范围。
        /// </summary>
        /// <returns>额外抢断距离。</returns>
        public float GetStealDistanceBonus()
        {
            return hellEnhanced ? aiDifficultyTuning.StealRangeBonus : 0f;
        }

        /// <summary>
        /// 返回地面级玩家间碰撞使用的物理质量。
        /// </summary>
        /// <returns>碰撞物理质量值。</returns>
        public float GetCollisionMass()
        {
            return HasGroundBlockBody ? GroundBlockCollisionMass : GroundCollisionMass;
        }

        /// <summary>
        /// 当该玩家正朝另一名球员的位置移动时返回 true。
        /// </summary>
        /// <param name="other">要检测移动方向的另一名球员</param>
        /// <returns>正在朝对方移动时返回 true；否则返回 false。</returns>
        public bool IsMovingToward(mlpPlayerObject other)
        {
            if (other == null)
            {
                return false;
            }

            var delta = other.Position.x - Position.x;
            if (Mathf.Abs(delta) <= 0.01f)
            {
                return Mathf.Abs(Velocity.x) > GroundCollisionSpeedEpsilon;
            }

            return Velocity.x * Mathf.Sign(delta) > GroundCollisionSpeedEpsilon;
        }

        /// <summary>
        /// 检测是否正在冲刺冲向另一名球员。
        /// </summary>
        /// <param name="other">要检测冲刺方向的另一名球员</param>
        /// <returns>正在冲刺冲向对方时返回 true；否则返回 false。</returns>
        public bool IsDashingInto(mlpPlayerObject other)
        {
            if (!IsDashing || other == null)
            {
                return false;
            }

            var delta = other.Position.x - Position.x;
            if (Mathf.Abs(delta) <= 0.01f)
            {
                return true;
            }

            return dashDirection * Mathf.Sign(delta) > 0f;
        }

        /// <summary>
        /// 因盖帽而中断冲刺。
        /// </summary>
        public void InterruptDashByBlock()
        {
            if (!IsDashing)
            {
                return;
            }

            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            readyForDash = false;
            Velocity.x = 0f;
            dashDelay.Activate();
            controller.PlayerOnDashEnd();

            if (IsGrounded && blockPumpPhase == BlockPumpPhase.None && !stealAnimationActive && stunTimer <= 0f && !isDunking && !isSuperShot)
            {
                PlayState(WithBall ? "idle_wb" : "idle");
            }

            UpdateGraphic();
        }

        /// <summary>
        /// 应用水平分离力。
        /// </summary>
        /// <param name="delta">分离偏移量</param>
        public void ApplyHorizontalSeparation(float delta)
        {
            if (Mathf.Abs(delta) <= 0.001f)
            {
                return;
            }

            Position.x = Mathf.Clamp(Position.x + delta, 20f, mlpConstants.Width - 20f);
            UpdateGraphic();
        }

        /// <summary>
        /// 被抢断时的处理。
        /// </summary>
        public void OnStolen()
        {
            GetBeStolen(Position.x, false);
        }

        /// <summary>
        /// 检测是否可被抢断。
        /// </summary>
        /// <param name="thiefX">抢断者的 X 坐标</param>
        /// <param name="thiefFacingScaleX">抢断者的朝向 X 缩放</param>
        /// <param name="stealDistance">the steal distance</param>
        /// <returns>The computed result.</returns>
        public float CheckToBeStolen(float thiefX, float thiefFacingScaleX, float stealDistance)
        {
            // 1. 如果不在地面、正在眩晕、已退出比赛或正在超级技能，返回-1表示不可偷
            if (!IsGrounded || stunTimer > 0f || removedFromPlay || isSuperShot)
            {
                return -1f;
            }

            // 2. 偷球者面朝右方时，检查持球者是否在其前方偷球范围内
            if (thiefFacingScaleX >= 0f)
            {
                return Position.x >= thiefX && Position.x <= thiefX + stealDistance
                    ? Mathf.Abs(Position.x - thiefX)
                    : -1f;
            }

            // 3. 偷球者面朝左方时，检查持球者是否在其前方偷球范围内
            return Position.x >= thiefX - stealDistance && Position.x <= thiefX
                ? Mathf.Abs(Position.x - thiefX)
                : -1f;
        }

        /// <summary>
        /// 执行被抢断的逻辑。
        /// </summary>
        /// <param name="thiefX">抢断者的 X 坐标</param>
        /// <param name="applyBallSteal">是否应用篮球抢断效果</param>
        /// <returns>之前持球时返回 true；否则返回 false。</returns>
        public bool GetBeStolen(float thiefX, bool applyBallSteal = true)
        {
            // 1. 如果已退出比赛，直接返回
            if (removedFromPlay)
            {
                return false;
            }

            // 2. 记录之前是否持球，然后释放篮球
            var hadBall = WithBall;
            WithBall = false;
            // 3. 重置冲刺和偷球相关状态
            dashTimer = 0f;
            dashDirection = 0;
            CancelStealAnimation(false);
            // 4. 重置投掷和攻击状态
            pendingGroundThrow = false;
            canThrow = false;
            attackJump = false;
            // 5. 计算眩晕时间（地狱难度会延长）
            var stunDuration = mlpObjectsData.StunDuration * (hellEnhanced ? aiDifficultyTuning.StunDurationMultiplier : 1f);
            stunTimer = Mathf.Max(stunTimer, stunDuration);
            // 6. 禁止所有动作
            canDoAction = false;
            jumpBlockActive = false;
            canTakeInHands = false;
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            // 7. 播放眩晕动画和音效
            PlayState("stun");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Stun, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PStunned, 0.9f);

            // 8. 如果之前持球且需要应用偷球效果，将篮球弹开
            if (hadBall && applyBallSteal && GameCore.Ball != null)
            {
                var delta = Position.x - thiefX;
                var direction = delta > 0f ? 1 : -1;
                var distanceFactor = Mathf.Clamp01(Mathf.Abs(delta) / mlpObjectsData.StealDistance);
                GameCore.Ball.ApplySteal(Position + new Vector2(0f, -45f), distanceFactor, direction);
            }

            // 9. 返回之前是否持球
            return hadBall;
        }

        /// <summary>
        /// 检测是否可以拾取自由球。
        /// </summary>
        /// <param name="ball">要检测的篮球对象</param>
        /// <returns>可拾取时返回距离平方；否则返回 -1。</returns>
        public float CheckLooseBallPickup(mlpBallObject ball)
        {
            // 1. 检查篮球是否可被捡起且球员是否可以捡球
            if (ball == null || !ball.CanBeTakenInHands || !CanTakeInHands)
            {
                return -1f;
            }

            // 2. 计算球员和篮球之间的偏移量
            var delta = ball.Position - Position;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            // 3. 如果水平或垂直距离超出捡球范围，返回-1
            if (absX > mlpObjectsData.BallPickupDistanceX || absY > mlpObjectsData.BallPickupDistanceY)
            {
                return -1f;
            }

            // 4. 返回距离的平方（越小越近，用于选择最近的球员）
            return delta.sqrMagnitude;
        }

        /// <summary>
        /// 尝试盖帽阻挡篮球。
        /// </summary>
        /// <param name="ball">要检测或影响的篮球对象</param>
        /// <returns>成功阻挡时返回 true；否则返回 false。</returns>
        public bool TryBlockBall(mlpBallObject ball)
        {
            // 1. 检查是否在盖帽状态、篮球是否可被盖、是否是对方的球
            if (!jumpBlockActive || ball == null || !ball.IsBlockable || ball.Side == Side || removedFromPlay || isSuperShot)
            {
                return false;
            }

            // 2. 获取篮球的运动轨迹（上一帧到当前帧）
            var start = ball.PreviousPosition;
            var end = ball.Position;
            // 3. 如果篮球轨迹完全在球员身后，无法盖帽
            if ((start.x - Position.x) * ball.Side <= 0f &&
                (end.x - Position.x) * ball.Side <= 0f)
            {
                return false;
            }

            // 4. 计算盖帽碰撞区域（教程模式下增大范围辅助玩家）
            var blockWidth = mlpObjectsData.JumpBlockWidth + (tutorialJumpBlockAssist ? 58f : 0f);
            var blockHeight = mlpObjectsData.JumpBlockHeight + (tutorialJumpBlockAssist ? 42f : 0f);
            var topBonus = tutorialJumpBlockAssist ? 18f : 0f;
            var bottomBonus = tutorialJumpBlockAssist ? 16f : 0f;
            // 5. 计算碰撞矩形的四条边界（考虑篮球半径）
            var minX = Position.x - blockWidth * 0.5f - mlpObjectsData.BallRadius;
            var maxX = Position.x + blockWidth * 0.5f + mlpObjectsData.BallRadius;
            var minY = Position.y - blockHeight - mlpObjectsData.BallRadius - topBonus;
            var maxY = Position.y + mlpObjectsData.BallRadius + bottomBonus;
            // 6. 用扫掠检测判断篮球轨迹是否穿过盖帽碰撞矩形
            if (!SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY))
            {
                return false;
            }

            // 7. 触发盖帽效果，将篮球弹开
            ball.ApplyBlock(this);
            return true;
        }

        /// <summary>
        /// 尝试启动超能技能。
        /// </summary>
        /// <param name="pressed">是否按下超能键</param>
        /// <returns>成功启动时返回 true；否则返回 false。</returns>
        private bool TryStartSuper(bool pressed)
        {
            // 1. 检查是否满足释放超能力的条件：按下按键、充能完毕、没有其他超能力在播放
            RefreshQuickTestSuperReady();
            if (!pressed || !readyForSuper || GameCore.IsSuperShot)
            {
                return false;
            }

            // 2. 部分技能需要持球才能释放
            if (skillDefinition.RequiresBallToCast && !WithBall)
            {
                return false;
            }

            // 3. 冲刺类技能需要在地面且不在眩晕/扣篮中
            if (skillDefinition.UsesDashSkill && (!IsGrounded || stunTimer > 0f || isDunking))
            {
                return false;
            }

            // 4. 篮板磁铁和必中盖帽有各自的使用条件检查
            if (skillDefinition.UsesReboundMagnetSkill && !CanUseReboundMagnet())
            {
                return false;
            }
            if (skillDefinition.UsesGuaranteedBlockSkill && !CanUseGuaranteedBlock())
            {
                return false;
            }

            // 5. 进入超能力状态，发送信号，显示提示文字，播放爆发特效
            StartSuper(true);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Super, Side, playerNo);
            GameCore.ShowHudBonusNotice(skillDefinition.ActivateNotice, 0.95f);
            skillFx?.PlayBurst();

            // 6. 根据技能类型执行对应的超能力效果
            switch (skillDefinition.SkillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    MakeSuperDash();        // 灵魂收割：冲刺穿过对手抢球
                    return true;
                case mlpCharacterSkillType.CarnivalJackpot:
                    MakeScoreUpgradeBuff(); // 狂欢大奖：下次得分加成
                    return true;
                case mlpCharacterSkillType.GhostSail:
                    MakeShield();           // 幽灵帆：在篮筐前生成护盾
                    return true;
                case mlpCharacterSkillType.BloodMoonBlink:
                    MakeAlleyOop();         // 血月闪烁：传送到篮下空接扣篮
                    return true;
                case mlpCharacterSkillType.WaxOverdrive:
                    MakeWaxOverdrive();     // 蜡像过载：特殊移动效果
                    return true;
                case mlpCharacterSkillType.BadLuck:
                    MakeFreeze();           // 厄运：冻结对手球员
                    return true;
                case mlpCharacterSkillType.ReboundMagnet:
                    MakeReboundMagnet();    // 篮板磁铁：自动吸引篮板球
                    return true;
                case mlpCharacterSkillType.SureBlock:
                    MakeGuaranteedBlock();  // 必中盖帽：必定成功盖帽
                    return true;
            }

            EndSuper();
            return false;
        }

        /// <summary>
        /// 尝试使用地狱加成超能冲刺。
        /// </summary>
        /// <returns>成功使用时返回 true；否则返回 false。</returns>
        public bool TryUseHellBonusSuperDash()
        {
            // 1. 检查是否满足使用条件（不在超能状态、未移除比赛、在地面、未被眩晕、未扣篮）
            if (!CanUseHellBonusSuperDash || GameCore.IsSuperShot || isSuperShot || removedFromPlay || !IsGrounded || stunTimer > 0f || isDunking)
            {
                return false;
            }

            // 2. 激活超能状态（不消耗原生充能），执行超级冲刺
            StartSuper(false);
            MakeSuperDash();

            // 3. 设置冷却计时器，显示提示文字
            hellBonusSuperDashCooldownTimer = mlpQuickTestSettings.Enabled ? 0f : hellBonusSuperDashCooldownDuration;
            GameCore.ShowHudBonusNotice("HELL DASH!", 0.9f);
            return true;
        }

        /// <summary>
        /// 尝试使用地狱加成护盾。
        /// </summary>
        /// <returns>成功使用时返回 true；否则返回 false。</returns>
        public bool TryUseHellBonusShield()
        {
            // 1. 检查是否满足使用条件（不在超能状态、未移除比赛、未被眩晕、未扣篮）
            if (!CanUseHellBonusShield || GameCore.IsSuperShot || isSuperShot || removedFromPlay || stunTimer > 0f || isDunking)
            {
                return false;
            }

            // 2. 激活超能状态（不消耗原生充能），执行护盾技能
            StartSuper(false);
            MakeShield();

            // 3. 设置冷却计时器，显示提示文字
            hellBonusShieldCooldownTimer = mlpQuickTestSettings.Enabled ? 0f : hellBonusShieldCooldownDuration;
            GameCore.ShowHudBonusNotice("HELL SHIELD!", 0.95f);
            return true;
        }

        /// <summary>
        /// 启动超能状态。
        /// </summary>
        /// <param name="consumeNativeCharge">是否消耗原生充能</param>
        private void StartSuper(bool consumeNativeCharge)
        {
            // 1. 标记进入超能状态
            isSuperShot = true;

            // 2. 如果消耗原生充能，清零能量条并标记可能需要退还
            if (consumeNativeCharge)
            {
                readyForSuper = false;
                superChargeTime = 0f;
                energyBar?.SetCharge(0f);
                hellNativeSuperRefundPending = !mlpQuickTestSettings.Enabled && hellEnhanced && aiDifficultyTuning.NativeSuperRefundFraction > 0f;
            }
            else
            {
                hellNativeSuperRefundPending = false;
            }

            // 3. 通知全局进入超能模式，取消当前进行中的动作
            GameCore.IsSuperShot = true;
            pendingGroundThrow = false;
            CancelStealAnimation(false);
            jumpBlockActive = false;
            blockPumpPhase = BlockPumpPhase.None;
            dashTimer = 0f;
        }

        /// <summary>
        /// 结束超能状态。
        /// </summary>
        private void EndSuper()
        {
            // 1. 清除超能状态标记，恢复缩放，允许再次参与比赛
            isSuperShot = false;
            GameCore.IsSuperShot = false;
            superPhase = SuperPhase.None;
            graphicScaleMultiplier = 1f;
            removedFromPlay = false;

            // 2. 处理冷却与能量恢复逻辑
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                // 2a. 无冷却时立即充满能量条
                readyForSuper = true;
                superChargeTime = superCoolDown;
                energyBar?.SetCharge(1f);
            }
            else if (hellNativeSuperRefundPending)
            {
                // 2b. 地狱增强模式下退还部分充能
                superChargeTime = Mathf.Min(cooldown, superChargeTime + cooldown * aiDifficultyTuning.NativeSuperRefundFraction);
                readyForSuper = superChargeTime >= cooldown;
                energyBar?.SetCharge(superChargeTime / cooldown);
            }

            // 3. 清除退还标记
            hellNativeSuperRefundPending = false;
        }

        /// <summary>
        /// 执行超级扣篮。
        /// </summary>
        private void MakeMegaDunk()
        {
            // 1. 锁定所有操作，将球员从比赛中移除，面朝自家篮筐
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            removedFromPlay = true;
            facingDirection = -Side;

            // 2. 计算飞行起点和篮下目标点，设置飞行持续时间
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkX, mlpObjectsData.AlleyOopY);
            superTimer = 0f;
            superDuration = Mathf.Max(0.3f, Vector2.Distance(superStartPosition, superTargetPosition) / 700f / 1.3333f);

            // 3. 进入超级扣篮飞行阶段，播放动画和音效
            superPhase = SuperPhase.MegaTravel;
            PlayState("megadunk");
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PMegaStart);
        }

        /// <summary>
        /// 继续超级扣篮的恢复阶段。
        /// </summary>
        private void ContinueSuperDunk()
        {
            // 1. 切换到超级扣篮恢复阶段
            superPhase = SuperPhase.MegaRecover;

            // 2. 设置从当前位置到落地点的插值参数
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkEndX, superDunkEndY);
            superTimer = 0f;
            superDuration = 0.1f;

            // 3. 播放落地动画
            PlayState("megadunk_end");
        }

        /// <summary>
        /// 结束超级扣篮。
        /// </summary>
        private void EndSuperDunk()
        {
            // 1. 如果不在超能状态则直接返回（防止重复调用）
            if (!isSuperShot)
            {
                return;
            }

            // 2. 释放球权，允许拾球和投篮
            WithBall = false;
            canTakeInHands = true;
            canThrow = true;

            // 3. 通知比赛处理器执行必中投篮，让篮球进入篮筐
            GameCore.MatchProcessor.Shoot(Side, IsHuman, 8, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, true);

            // 4. 将球员放置到落地点，重置速度和地面状态
            Position = new Vector2(superDunkEndX, superDunkEndY);
            Velocity = Vector2.zero;
            IsGrounded = true;

            // 5. 播放待机动画，结束超能状态
            PlayState("idle");
            EndSuper();
        }

        /// <summary>
        /// 释放护盾技能。
        /// </summary>
        private void MakeShield()
        {
            // 1. 激活护盾对象
            shield?.Activate();

            // 2. 结束超能状态
            EndSuper();
        }

        private void MakeScoreUpgradeBuff()
        {
            // 1. 激活得分手加成，播放增益特效
            scoreUpgradeActive = true;
            scoreUpgradePendingShot = false;
            skillFx?.PlayBuff(float.PositiveInfinity);

            // 2. 结束超能状态
            EndSuper();
        }

        private void MakeWaxOverdrive()
        {
            // 1. 设置移动加速持续时间，记录是否有额外得分加成
            moveBuffTimer = skillDefinition.EffectDuration;
            moveBuffScoreBonusAvailable = skillDefinition.FlatScoreBonus > 0;

            // 2. 播放增益特效，结束超能状态
            skillFx?.PlayBuff(skillDefinition.EffectDuration);
            EndSuper();
        }

        private void MakeFreeze()
        {
            // 1. 找到最近的对手，对其施加冰冻效果
            var opponent = GameCore.FindClosestOpponent(this);
            if (opponent != null)
            {
                opponent.ApplyFreeze(skillDefinition.EffectDuration, skillDefinition);
            }

            // 2. 播放爆发特效，结束超能状态
            skillFx?.PlayBurst(0.45f);
            EndSuper();
        }

        private void MakeReboundMagnet()
        {
            // 1. 设置篮板磁铁持续时间（优先使用技能定义，否则用默认值）
            reboundMagnetTimer = skillDefinition.EffectDuration > 0f
                ? skillDefinition.EffectDuration
                : ReboundMagnetDefaultDuration;

            // 2. 允许拾球，立即执行一次磁铁更新，结束超能状态
            canTakeInHands = true;
            UpdateReboundMagnet(0f);
            EndSuper();
        }

        private void MakeGuaranteedBlock()
        {
            // 1. 检查篮球是否可以被盖帽，不满足则直接结束超能
            var ball = GameCore.Ball;
            if (!CanUseGuaranteedBlockBall(ball))
            {
                EndSuper();
                return;
            }

            // 2. 锁定所有操作，停止移动，取消其他进行中的动作
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            WithBall = false;
            Velocity = Vector2.zero;
            dashTimer = 0f;
            blockPumpPhase = BlockPumpPhase.None;
            jumpBlockActive = false;

            // 3. 将球员传送到篮球旁边的盖帽位置
            Position = GetGuaranteedBlockPosition(ball);
            IsGrounded = Position.y >= mlpObjectsData.PlayerIndentY - 0.5f;
            facingDirection = ball.Position.x >= Position.x ? 1f : -1f;

            // 4. 播放传送特效和音效，执行盖帽判定
            teleportFx?.StartPlay(Position.x, Position.y - GuaranteedBlockHandsOffsetY);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            PlayState("blockStart");
            ball.ApplyBlock(this);

            // 5. 显示提示文字，进入盖帽悬停阶段
            GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
            guaranteedBlockPickupLocked = !IsGrounded;
            superPhase = SuperPhase.GuaranteedBlockHold;
            superTimer = 0f;
            superDuration = GuaranteedBlockHoldDuration;
            actionLatch = Mathf.Max(actionLatch, GuaranteedBlockHoldDuration);
        }

        private Vector2 GetGuaranteedBlockPosition(mlpBallObject ball)
        {
            // 1. 计算水平位置：在篮球前方偏移，限制在场地范围内
            var targetX = Mathf.Clamp(
                ball.Position.x - ball.Side * GuaranteedBlockHorizontalOffset,
                20f,
                mlpConstants.Width - 20f);

            // 2. 计算垂直位置：在篮球上方偏移，限制在篮筐和地面之间
            var targetY = Mathf.Clamp(
                ball.Position.y + GuaranteedBlockHandsOffsetY,
                mlpObjectsData.BasketHeight - 18f,
                mlpObjectsData.PlayerIndentY);

            return new Vector2(targetX, targetY);
        }

        private void FinishGuaranteedBlock()
        {
            // 1. 停止移动，恢复操作能力
            Velocity = Vector2.zero;
            canDoAction = true;
            canThrow = true;

            // 2. 如果接近地面则落到地面
            if (Position.y >= mlpObjectsData.PlayerIndentY - 0.5f)
            {
                Position.y = mlpObjectsData.PlayerIndentY;
                IsGrounded = true;
            }

            // 3. 根据是否在地面决定拾球能力和落地动画
            guaranteedBlockPickupLocked = !IsGrounded;
            canTakeInHands = !WithBall && !guaranteedBlockPickupLocked;
            PlayState(IsGrounded ? "blockEnd" : "fly1");

            // 4. 结束超能状态
            EndSuper();
        }

        /// <summary>
        /// 执行空接超能。
        /// </summary>
        private void MakeAlleyOop()
        {
            // 1. 设置得分退费比例（部分技能投丢后退还能量）
            pendingScoreRefundFraction = skillDefinition.ScoreRefundFraction;
            pendingScoreRefundTimer = skillDefinition.ScoreRefundFraction > 0f ? 4f : 0f;

            // 2. 锁定操作，停止水平移动，面朝进攻方向
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            Velocity.x = 0f;
            facingDirection = AttackTargetX - Position.x >= 0f ? 1f : -1f;

            // 3. 在地面先播放投篮动画，动画结束后再空接；在空中直接空接
            if (IsGrounded)
            {
                alleyOopPendingThrow = true;
                PlayState("throw_land");
            }
            else
            {
                StartAlleyOop();
            }
        }

        /// <summary>
        /// 开始空接投掷。
        /// </summary>
        private void StartAlleyOop()
        {
            // 1. 清除待执行标记，让篮球进入空接飞行状态
            alleyOopPendingThrow = false;
            GameCore.Ball.AlleyOop(Side, Position.x - 20f * Side, Position.y - 30f, this);

            // 2. 释放球权，锁定拾球和投篮
            WithBall = false;
            canTakeInHands = false;
            canThrow = false;

            // 3. 通知其他球员球已出手
            GameCore.NotifyBallOthers();

            // 4. 在空中时播放飞行动画
            if (!IsGrounded)
            {
                PlayState("fly1");
            }
        }

        /// <summary>
        /// 完成空接传送出场。
        /// </summary>
        private void FinishAlleyTeleportOut()
        {
            // 1. 将球员移动到扣篮起始位置，面朝篮筐，隐藏角色图形
            Position = new Vector2(superDunkX, mlpObjectsData.AlleyOopY);
            facingDirection = -Side;
            graphicScaleMultiplier = 0f;

            // 2. 重置骨骼动画姿态
            if (armature != null)
            {
                visualState = "pumpEnd";
                SetAnimationPlaybackSpeed(visualState);
                armature.StopAtStart("pumpEnd");
            }

            // 3. 播放传送特效和音效，将篮球从物理模拟中移除
            teleportFx?.StartPlay(Position.x, Position.y);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            GameCore.Ball.RemoveFromPhysics();

            // 4. 切换到传送入场阶段
            superPhase = SuperPhase.AlleyTeleportIn;
            superTimer = 0f;
            superDuration = 0.4f;
        }

        /// <summary>
        /// 执行超能冲刺。
        /// </summary>
        private void MakeSuperDash()
        {
            // 1. 找到持球对手的位置作为冲刺目标
            var targetX = -1f;
            var opponents = Side == -1 ? GameCore.PlayersRight : GameCore.PlayersLeft;
            superDashHits.Clear();
            for (var i = 0; i < opponents.Count; i++)
            {
                if (opponents[i].WithBall)
                {
                    targetX = opponents[i].Position.x;
                }
            }

            // 2. 没有持球对手时，追踪篮球位置或队友位置
            teamMate = GameCore.GetTeamMate(Side, playerNo);
            dashTeammatePending = false;
            if (targetX < 0f)
            {
                if (GameCore.Ball != null && GameCore.Ball.IsInGame)
                {
                    targetX = GameCore.Ball.Position.x;
                }

                if (teamMate != null)
                {
                    if (teamMate.WithBall)
                    {
                        targetX = teamMate.Position.x;
                    }

                    dashTeammatePending = true;
                }
            }

            if (targetX < 0f)
            {
                targetX = AttackTargetX;
            }

            // 3. 根据当前位置和目标位置选择冲刺终点（两个预设点之一）
            var currentX = Position.x;
            var dashPoint = WithBall
                ? 0
                : Side < 0
                    ? currentX < targetX ? 0 : 1
                    : currentX > targetX ? 0 : 1;

            // 4. 设置冲刺方向，将球员从比赛中移除（不可被碰撞）
            dashToRight = Side < 0 ? dashPoint == 0 : dashPoint == 1;
            removedFromPlay = true;
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;

            // 5. 设置冲刺起点、终点、持续时间，进入冲刺阶段
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDashTargets[dashPoint], mlpObjectsData.SuperDashY);
            superTimer = 0f;
            superDuration = Mathf.Max(0.1f, Vector2.Distance(superStartPosition, superTargetPosition) / 600f / 1.3333f);
            superPhase = SuperPhase.SuperDashTravel;
            skillFx?.PlayDash(superDuration);
            PlayState("md_start");
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSuperDash);
        }

        /// <summary>
        /// 继续超能冲刺。
        /// </summary>
        private void ContinueSuperDash()
        {
            // 1. 将球员放置到冲刺终点，停止移动并落地
            Position = superTargetPosition;
            Velocity = Vector2.zero;
            IsGrounded = true;

            // 2. 恢复操作能力（如果没有持球则允许拾球）
            canDoAction = true;
            canTakeInHands = !WithBall;
            canThrow = true;

            // 3. 播放落地动画，结束超能状态
            PlayState("md_end");
            EndSuper();
        }

        /// <summary>
        /// 更新超能状态。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        private void UpdateSuper(float dt)
        {
            // 1. 根据当前超能阶段执行不同的更新逻辑
            switch (superPhase)
            {
                case SuperPhase.MegaTravel:
                    // 1a. 超级扣篮飞行阶段：插值移动球员到目标点
                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDunk();
                    }
                    break;
                case SuperPhase.MegaRecover:
                    // 1b. 超级扣篮恢复阶段：插值移动球员到落地点
                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    break;
                case SuperPhase.SuperDashTravel:
                    // 1c. 超能冲刺飞行阶段：插值移动并检测碰撞
                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    UpdateSuperDashTravel();
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDash();
                    }
                    break;
                case SuperPhase.AlleyTeleportOut:
                    // 1d. 空接传送出场阶段：逐渐缩小角色图形
                    superTimer += dt;
                    graphicScaleMultiplier = Mathf.Clamp01(1f - superTimer / superDuration);
                    if (superTimer >= superDuration)
                    {
                        FinishAlleyTeleportOut();
                    }
                    break;
                case SuperPhase.AlleyTeleportIn:
                    // 1e. 空接传送入场阶段：逐渐放大角色图形
                    superTimer += dt;
                    graphicScaleMultiplier = Mathf.Clamp01(superTimer / superDuration);
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDunk();
                    }
                    break;
                case SuperPhase.GuaranteedBlockHold:
                    // 1f. 必定盖帽悬停阶段：保持球员静止
                    superTimer += dt;
                    Velocity = Vector2.zero;
                    if (superTimer >= superDuration)
                    {
                        FinishGuaranteedBlock();
                    }
                    break;
            }

            // 2. 更新角色图形显示
            UpdateGraphic();
        }

        /// <summary>
        /// 更新超能冲刺移动过程。
        /// </summary>
        private void UpdateSuperDashTravel()
        {
            // 1. 遍历对手，检测冲刺碰撞（距离小于40像素时触发抢断）
            var opponents = Side == -1 ? GameCore.PlayersRight : GameCore.PlayersLeft;
            var currentX = Position.x;
            for (var i = 0; i < opponents.Count; i++)
            {
                var opponentPlayer = opponents[i];
                if (opponentPlayer == null || superDashHits.Contains(opponentPlayer.PlayerNo) || opponentPlayer.IsDunking)
                {
                    continue;
                }

                if (Mathf.Abs(currentX - opponentPlayer.Position.x) < 40f)
                {
                    // 1a. 记录已碰撞的对手（防止重复触发），尝试抢断
                    superDashHits.Add(opponentPlayer.PlayerNo);
                    if (opponentPlayer.GetBeStolen(currentX, false))
                    {
                        AcquireBallDuringSuperDash();
                    }
                }
            }

            // 2. 检测是否经过空中的篮球或队友的球
            var ball = GameCore.Ball;
            if (ball != null && ball.Position.y > mlpObjectsData.BasketHeight && !WithBall)
            {
                if (ball.IsInGame)
                {
                    // 2a. 经过篮球时直接拾取
                    if ((dashToRight && currentX > ball.Position.x) || (!dashToRight && currentX < ball.Position.x))
                    {
                        AcquireBallDuringSuperDash();
                    }
                }
                else if (dashTeammatePending && teamMate != null && ((dashToRight && currentX > teamMate.Position.x) || (!dashToRight && currentX < teamMate.Position.x)))
                {
                    // 2b. 经过持球队友时，强制释放队友的球并拾取
                    dashTeammatePending = false;
                    if (teamMate.WithBall)
                    {
                        teamMate.FreeBall();
                        AcquireBallDuringSuperDash();
                    }
                }
            }
        }

        /// <summary>
        /// 在超能冲刺期间获取篮球。
        /// </summary>
        private void AcquireBallDuringSuperDash()
        {
            // 1. 如果已经持球或篮球不存在则跳过
            if (WithBall || GameCore.Ball == null)
            {
                return;
            }

            // 2. 将篮球收入手中，通知其他球员
            WithBall = true;
            canTakeInHands = false;
            attackJump = false;
            GameCore.Ball.TakeInHands(Side);
            GameCore.NotifyBallInHands(Side, playerNo);

            // 3. 如果是灵魂收割技能，激活额外得分加成
            if (skillDefinition.SkillType == mlpCharacterSkillType.SoulReap && skillDefinition.FlatScoreBonus > 0)
            {
                flatScoreBonusPoints = Mathf.Max(flatScoreBonusPoints, skillDefinition.FlatScoreBonus);
                flatScoreBonusTimer = Mathf.Max(flatScoreBonusTimer, skillDefinition.BonusDuration);
                skillFx?.PlayBuff(Mathf.Min(skillDefinition.BonusDuration, 1.1f));
            }

            // 4. 灵魂收割技能显示提示文字
            if (skillDefinition.SkillType == mlpCharacterSkillType.SoulReap)
            {
                GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
            }
        }

        /// <summary>
        /// 执行投篮。
        /// </summary>
        private void MakeThrow()
        {
            // 1. 锁定动作，防止连续投篮
            canDoAction = false;
            actionLatch = Mathf.Max(actionLatch, 0.35f);
            canThrow = false;
            attackJump = false;
            WithBall = false;

            // 2. 尝试触发扣篮（如果在篮下且满足条件），成功则直接返回
            if (TryStartDunk())
            {
                return;
            }

            // 3. 计算篮球出手位置：地面偏移 20px，空中偏移 35px
            canTakeInHands = IsGrounded;
            var releaseOffset = IsGrounded ? 20f : 35f;
            if (IsGrounded)
            {
                pointOfThrow = Position.x;
            }

            var releaseX = Position.x - Side * releaseOffset;
            var releaseY = Position.y - 50f;

            // 4. 判断是三分球还是两分球（根据出手位置与三分线的距离）
            var throwType = (pointOfThrow - mlpObjectsData.ThreePointsDistance) * Side >= 0f ? 0 : 6;

            // 5. 记录投篮数据、发送信号、让篮球飞向篮筐
            GameCore.MatchProcessor.Shoot(Side, IsHuman, throwType, playerNo);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Shoot, Side, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Shoot(Side, releaseX, releaseY, Velocity.x, GetShotAccuracy());
            PlayState(IsGrounded ? "throw_land" : "fly1");
        }

        /// <summary>
        /// 开始地面投篮。
        /// </summary>
        private void BeginFloorThrow()
        {
            // 1. 标记待执行地面投篮
            pendingGroundThrow = true;

            // 2. 锁定所有操作，防止重复投篮
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            actionLatch = Mathf.Max(actionLatch, 0.3f);

            // 3. 停止水平移动，播放投篮预备动画
            Velocity.x = 0f;
            PlayState("throw_land");
        }

        /// <summary>
        /// 开始抢断。
        /// </summary>
        private void BeginSteal()
        {
            // 1. 防止重复触发抢断
            if (stealAnimationActive)
            {
                return;
            }

            // 2. 锁定操作，启动抢断动画和计时器
            canDoAction = false;
            pendingStealAction = true;
            stealAnimationActive = true;
            stealAttemptTimer = mlpObjectsData.StealFrameEventTime;   // 实际判定时间点
            stealAnimationTimer = mlpObjectsData.StealAnimationDuration; // 动画总时长

            // 3. 记录并锁定朝向（抢断时面朝对手方向）
            stealFacingDirection = facingDirection;
            facingDirection = stealFacingDirection;

            // 4. 停止移动，播放抢断动画，播放音效
            actionLatch = Mathf.Max(actionLatch, mlpObjectsData.StealAnimationDuration);
            canTakeInHands = false;
            Velocity.x = 0f;
            PlayState("steal");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.StartSteal, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh, 0.7f);
        }

        /// <summary>
        /// 结算抢断尝试。
        /// </summary>
        private void ResolveStealAttempt()
        {
            // 1. 如果没有待执行的抢断动作，只清除计时器
            if (!pendingStealAction)
            {
                stealAttemptTimer = -1f;
                return;
            }

            // 2. 清除抢断判定标记和计时器
            stealAttemptTimer = -1f;
            pendingStealAction = false;

            // 3. 发送抢断信号，尝试从对手手中抢球
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Steal, Side, playerNo);
            if (GameCore.TryStealBall(this, stealFacingDirection))
            {
                // 4. 抢断成功时增加短暂操作锁定
                actionLatch = Mathf.Max(actionLatch, 0.18f);
            }
        }

        /// <summary>
        /// 更新抢断动画。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        private void UpdateStealAnimation(float dt)
        {
            // 1. 锁定朝向和水平移动
            facingDirection = stealFacingDirection;
            Velocity.x = 0f;

            // 2. 等待抢断判定时间点到达后执行判定
            if (stealAttemptTimer >= 0f)
            {
                stealAttemptTimer -= dt;
                if (stealAttemptTimer <= 0f)
                {
                    ResolveStealAttempt();          //判定触发点
                }
            }

            // 3. 动画计时结束后完成抢断动画
            stealAnimationTimer -= dt;
            if ((armature == null && stealAnimationTimer <= 0f) || stealAnimationTimer <= -0.2f)
            {
                FinishStealAnimation();
                return;
            }

            // 4. 更新角色图形
            UpdateGraphic();
        }

        /// <summary>
        /// 结束抢断动画。
        /// </summary>
        private void FinishStealAnimation()
        {
            // 1. 如果抢断动画未激活则跳过
            if (!stealAnimationActive)
            {
                return;
            }

            // 2. 如果还有未结算的抢断判定，先执行判定
            if (pendingStealAction)
            {
                ResolveStealAttempt();
            }

            // 3. 清除抢断动画状态和计时器
            stealAnimationActive = false;
            stealAnimationTimer = -1f;
            stealAttemptTimer = -1f;

            // 4. 恢复拾球和操作能力
            canTakeInHands = !WithBall && stunTimer <= 0f && !removedFromPlay;
            canDoAction = controller.ReadyForAction();
            actionLatch = Mathf.Max(actionLatch, 0f);

            // 5. 恢复待机动画
            PlayState(WithBall ? "idle_wb" : "idle");
        }

        /// <summary>
        /// 取消抢断动画。
        /// </summary>
        /// <param name="restorePickup">是否恢复拾取能力</param>
        private void CancelStealAnimation(bool restorePickup)
        {
            // 1. 清除抢断动画状态和所有计时器
            stealAnimationActive = false;
            pendingStealAction = false;
            stealAttemptTimer = -1f;
            stealAnimationTimer = -1f;

            // 2. 如果允许恢复拾球且当前可以拾球，则重新启用拾球能力
            if (restorePickup && !WithBall && stunTimer <= 0f && !removedFromPlay)
            {
                canTakeInHands = true;
            }
        }

        /// <summary>
        /// 更新朝向。
        /// </summary>
        private void UpdateFacing()
        {
            // 1. 确定面朝目标：默认朝进攻方向，防守时朝持球者或篮球
            var ballHolder = GameCore.FindBallHolder();
            var faceTarget = AttackTargetX;
            if (!WithBall && ballHolder != null && ballHolder.Side != Side)
            {
                faceTarget = ballHolder.Position.x;
            }
            else if (!WithBall && GameCore.Ball != null)
            {
                faceTarget = GameCore.Ball.Position.x;
            }

            // 2. 当目标与当前位置有足够距离时，更新面朝方向
            var delta = faceTarget - Position.x;
            if (Mathf.Abs(delta) > 0.5f)
            {
                facingDirection = delta >= 0f ? 1f : -1f;
            }
        }

        /// <summary>
        /// 播放指定的动画状态。
        /// </summary>
        /// <param name="state">动画状态名称</param>
        private void PlayState(string state)
        {
            // 1. 非扣篮动画时恢复球槽显示
            if (!IsDunkAnimationState(state))
            {
                SetDunkBallSlotsHidden(false);
            }

            // 2. 设置动画播放速度
            SetAnimationPlaybackSpeed(state);

            // 3. 如果已是当前状态则跳过（避免重复播放）
            if (visualState == state)
            {
                return;
            }

            // 4. 更新状态并播放骨骼动画
            visualState = state;
            armature?.Play(state);
        }

        /// <summary>
        /// 将动画状态设置到起始帧。
        /// </summary>
        /// <param name="state">动画状态名称</param>
        private void SetStateAtStart(string state)
        {
            visualState = state;
            if (!IsDunkAnimationState(state))
            {
                SetDunkBallSlotsHidden(false);
            }

            SetAnimationPlaybackSpeed(state);
            armature?.StopAtStart(state);
        }

        private void SetDunkBallSlotsHidden(bool hidden)
        {
            // 1. 如果状态没有变化则跳过
            if (dunkBallSlotsHidden == hidden)
            {
                return;
            }

            // 2. 更新球槽隐藏状态
            dunkBallSlotsHidden = hidden;
            if (armature == null)
            {
                return;
            }

            // 3. 控制前景和背景两个球槽的显示/隐藏
            armature.SetSlotHidden("ball", hidden);
            armature.SetSlotHidden("ball_front", hidden);
        }

        private void SetAnimationPlaybackSpeed(string state)
        {
            // 1. 如果骨骼动画存在，根据动画状态设置对应的播放速度
            if (armature != null)
            {
                armature.PlaybackSpeed = AnimationPlaybackSpeed(state);
            }
        }

        private static float AnimationPlaybackSpeed(string state)
        {
            if (state == "dunk1")
            {
                return mlpObjectsData.Dunk1AnimationSpeed;
            }

            if (state == "dunk2")
            {
                return mlpObjectsData.Dunk2AnimationSpeed;
            }

            if (state == "dunk3")
            {
                return mlpObjectsData.Dunk3AnimationSpeed;
            }

            return 1f;
        }

        private static bool IsDunkAnimationState(string state)
        {
            return state == "dunk1" || state == "dunk2" || state == "dunk3";
        }

        /// <summary>
        /// 动画帧事件回调：骨骼动画播放到特定帧时触发，用于同步游戏逻辑（投篮出手、抢断判定、扣篮释放等）。
        /// </summary>
        /// <param name="animationName">播放中的动画名称</param>
        /// <param name="eventName">动画编辑器中设置的事件标记名称</param>
        private void OnAnimationFrameEvent(string animationName, string eventName)
        {
            // 1. 立即刷新骨骼姿态，确保视觉位置与事件时机同步
            armature?.RefreshPose();

            // 2. "throw" 事件：投篮出手帧 → 篮球离开球员手的时机
            if (eventName == "throw")
            {
                // 2a. 如果有待执行的空接投篮，先执行空接（把球抛向空中让队友扣）
                if (alleyOopPendingThrow && WithBall)
                {
                    StartAlleyOop();
                    return;
                }

                // 2b. 如果有待执行的地面投篮，执行普通投篮出手
                if (pendingGroundThrow && WithBall)
                {
                    pendingGroundThrow = false;
                    MakeThrow();
                    return;
                }
            }

            // 3. "action" 事件：抢断动作帧 → 手伸出到最远位置时判定是否碰到球
            if (eventName == "action" && pendingStealAction)
            {
                ResolveStealAttempt();
                return;
            }

            // 4. "mega" 事件：超级扣篮结束帧 → 扣篮动画播完，结束超能力状态
            if (eventName == "mega" && isSuperShot)
            {
                EndSuperDunk();
                return;
            }

            // 5. "dunk" 事件：扣篮释放帧 → 球进入篮筐的时机，判定是否扣进
            if (eventName == "dunk" && isDunking)
            {
                ReleaseDunkBall();
            }
        }

        /// <summary>
        /// 动画完成回调。
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <summary>
        /// 动画完成回调：某个动画播放完毕时触发，用于衔接下一个动画或恢复默认状态。
        /// </summary>
        /// <param name="animationName">播放完毕的动画名称</param>
        private void OnAnimationComplete(string animationName)
        {
            switch (animationName)
            {
                // 1. 盖帽/假动作起跳动画完成 → 标记可以进入下一阶段
                case "blockStart":
                case "pumpStart":
                    blockPumpStartReady = true;
                    break;

                // 2. 盖帽/假动作落地动画完成 → 标记可以恢复自由操作
                case "blockEnd":
                case "pumpEnd":
                    blockPumpEndReady = true;
                    break;

                // 3. 地面投篮预备动画完成 → 执行实际投篮出手（先检查空接）
                case "throw_land":
                    if (alleyOopPendingThrow && WithBall)
                    {
                        StartAlleyOop();
                    }
                    else if (pendingGroundThrow && WithBall)
                    {
                        pendingGroundThrow = false;
                        MakeThrow();
                    }
                    break;

                // 4. 抢断动画完成 → 结束抢断状态，恢复移动
                case "steal":
                    FinishStealAnimation();
                    break;

                // 5. 扣篮动画完成 → 如果球还没释放，补发释放（保底机制）
                case "dunk1":
                case "dunk2":
                case "dunk3":
                    if (isDunking && !dunkReleased)
                    {
                        ReleaseDunkBall();
                    }
                    break;

                // 6. 超能力冲刺起飞动画完成 → 切换到空中滑行动画
                case "md_start":
                    PlayState("md_mid");
                    break;

                // 7. 超能力冲刺落地动画完成 → 恢复待机动画
                case "md_end":
                    PlayState(WithBall ? "idle_wb" : "idle");
                    break;
            }
        }

        private void UpdateSkillTimers(float dt)
        {
            // 1. 更新额外得分加成计时器（到时清零加成点数）
            if (flatScoreBonusTimer > 0f)
            {
                flatScoreBonusTimer = Mathf.Max(0f, flatScoreBonusTimer - dt);
                if (flatScoreBonusTimer <= 0f)
                {
                    flatScoreBonusPoints = 0;
                }
            }

            // 2. 更新移动加速计时器（到时清除得分加成标志）
            if (moveBuffTimer > 0f)
            {
                moveBuffTimer = Mathf.Max(0f, moveBuffTimer - dt);
                if (moveBuffTimer <= 0f)
                {
                    moveBuffScoreBonusAvailable = false;
                }
            }

            // 3. 更新得分退费计时器（到时清除退费比例）
            if (pendingScoreRefundTimer > 0f)
            {
                pendingScoreRefundTimer = Mathf.Max(0f, pendingScoreRefundTimer - dt);
                if (pendingScoreRefundTimer <= 0f)
                {
                    pendingScoreRefundFraction = 0f;
                }
            }
        }

        private bool CanUseReboundMagnet()
        {
            return !WithBall &&
                   stunTimer <= 0f &&
                   !removedFromPlay &&
                   !isDunking &&
                   CanUseReboundMagnetBall(GameCore.Ball);
        }

        private bool CanUseGuaranteedBlock()
        {
            return stunTimer <= 0f &&
                   !removedFromPlay &&
                   !isDunking &&
                   CanUseGuaranteedBlockBall(GameCore.Ball);
        }

        private bool CanUseGuaranteedBlockBall(mlpBallObject ball)
        {
            return ball != null && ball.IsBlockable && ball.Side != Side;
        }

        private bool CanUseReboundMagnetBall(mlpBallObject ball)
        {
            if (ball == null || !ball.IsInGame)
            {
                return false;
            }

            return ball.State == "basket" ||
                   ball.State == "block" ||
                   ball.State == "bounce" ||
                   ball.State == "steal";
        }

        private void UpdateReboundMagnet(float dt)
        {
            // 1. 如果磁铁未激活则跳过
            if (reboundMagnetTimer <= 0f)
            {
                return;
            }

            // 2. 递减磁铁持续时间
            reboundMagnetTimer = Mathf.Max(0f, reboundMagnetTimer - dt);

            // 3. 如果球员不可用则立即关闭磁铁
            var ball = GameCore.Ball;
            if (WithBall || stunTimer > 0f || removedFromPlay || isDunking || !CanUseReboundMagnetBall(ball))
            {
                reboundMagnetTimer = 0f;
                return;
            }

            // 4. 计算接球点与篮球的距离
            var catchPoint = Position + new Vector2(0f, -58f);
            var delta = catchPoint - ball.Position;
            if (Mathf.Abs(delta.x) <= ReboundMagnetCatchDistanceX &&
                Mathf.Abs(delta.y) <= ReboundMagnetCatchDistanceY)
            {
                // 4a. 篮球进入接球范围，拾取篮球并显示提示
                reboundMagnetTimer = 0f;
                TakeBallInHands();
                canDoAction = false;
                actionLatch = Mathf.Max(actionLatch, 0.18f);
                GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
                return;
            }

            // 5. 将篮球速度朝球员方向吸引（距离越远速度越快）
            var distance = Mathf.Max(1f, delta.magnitude);
            var speed = Mathf.Lerp(
                ReboundMagnetMinSpeed,
                ReboundMagnetMaxSpeed,
                Mathf.Clamp01(distance / 420f));
            ball.Side = Side;
            ball.Velocity = delta / distance * speed;
        }

        private float GetMoveSpeed()
        {
            // 1. 根据是否持球选择基础速度，有移动加速时乘以加速倍率
            var baseSpeed = WithBall ? mlpObjectsData.PlayerMoveWithBall : mlpObjectsData.PlayerMove;
            return baseSpeed * (moveBuffTimer > 0f ? skillDefinition.MoveSpeedMultiplier : 1f);
        }

        private float GetDashSpeed()
        {
            // 1. 计算冲刺速度：基础移动速度的1.7倍，有移动加速时额外加成
            var multiplier = moveBuffTimer > 0f ? Mathf.Lerp(1f, skillDefinition.MoveSpeedMultiplier, 0.65f) : 1f;
            return mlpObjectsData.PlayerMove * 1.7f * multiplier;
        }

        private float GetShotAccuracy()
        {
            // 1. 教程完美投篮模式下直接返回高精度值
            if (tutorialPerfectShotPrimed)
            {
                tutorialPerfectShotPrimed = false;
                return -0.5f;
            }

            // 2. 基础精度加上移动加速的精度修正值
            var resolvedAccuracy = accuracy;
            if (moveBuffTimer > 0f)
            {
                resolvedAccuracy += skillDefinition.AccuracyModifier;
            }

            return Mathf.Max(-0.05f, resolvedAccuracy);
        }

        private void GrantSuperChargeFraction(float fraction)
        {
            // 1. 获取超能冷却时间，无冷却时直接刷新就绪状态
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                RefreshQuickTestSuperReady();
                return;
            }

            // 2. 无效比例则跳过
            if (fraction <= 0f)
            {
                return;
            }

            // 3. 增加充能进度，更新能量条显示
            superChargeTime = Mathf.Min(cooldown, superChargeTime + cooldown * fraction);
            readyForSuper = superChargeTime >= cooldown;
            energyBar?.SetCharge(superChargeTime / cooldown);
        }

        /// <summary>
        /// 更新图形显示。
        /// </summary>
        private void UpdateGraphic()
        {
            // 1. 计算角色缩放（角色专属缩放 × 通用缩放倍率），面朝方向由 scaleX 的正负控制
            var gameplayScale = mlpPlayersData.GetCharacterGameplayScaleMultiplier(characterId) * graphicScaleMultiplier;
            graphic.transform.position = mlpConstants.PixelToWorldSnapped(Position.x, Position.y, GraphicDepthBase + renderDepthBias);
            graphic.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * facingDirection * gameplayScale,
                mlpConstants.UnitsPerPixel * gameplayScale,
                1f);

            // 2. 更新阴影外观（技能激活时可能切换为红色阴影）
            UpdateShadowAppearance();

            // 3. 显示/隐藏阴影，并根据球员高度缩放阴影大小（越高越小）
            var showShadow = !removedFromPlay && graphicScaleMultiplier > 0.05f;
            shadow.SetActive(showShadow);
            if (showShadow)
            {
                var shadowScale = Mathf.Clamp01(1f - (mlpObjectsData.PlayerIndentY - Position.y) / 300f);
                mlpRender.ApplyPixelTransform(
                    shadow.transform,
                    Position.x,
                    mlpObjectsData.FloorY + 6f,
                    ShadowDepthBase + renderDepthBias * ShadowDepthBiasScale,
                    Mathf.Max(0.2f, shadowScale));
            }
        }

        private void UpdateShadowAppearance()
        {
            // 1. 如果没有渲染器则跳过
            if (shadowRenderer == null)
            {
                return;
            }

            // 2. 根据技能激活状态选择高亮或默认阴影贴图
            var targetSprite = UsesHighlightedSkillShadow ? activeSkillShadowSprite : defaultShadowSprite;
            if (targetSprite != null && shadowRenderer.sprite != targetSprite)
            {
                shadowRenderer.sprite = targetSprite;
            }
        }

        private void ClearScoreUpgrade()
        {
            scoreUpgradeActive = false;
            scoreUpgradePendingShot = false;
            skillFx?.Stop();
        }

        /// <summary>
        /// 创建备用角色形象。
        /// </summary>
        private void CreateFallbackAvatar()
        {
            // 1. 创建一个新游戏对象作为备用角色身体
            var body = new GameObject("FallbackBody");
            // 2. 挂载到角色图形下，添加精灵渲染器
            body.transform.SetParent(graphic.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.FallbackAvatar,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "BallClipMsg0000");
            // 3. 根据队伍设置颜色，调整排序和位置缩放
            renderer.color = teamIndex == 0 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            renderer.sortingOrder = 20;
            body.transform.localPosition = new Vector3(0f, -80f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
        }

        /// <summary>
        /// 开始冲刺。
        /// </summary>
        /// <param name="direction">移动或投掷方向（-1 或 1）</param>
        private void StartDash(int direction)
        {
            // 1. 设置冲刺方向和持续时间
            dashDirection = direction;
            dashTimer = 0.14f;
            // 2. 设置冲刺速度，清除输入缓冲
            Velocity.x = GetDashSpeed() * direction;
            actionLatch = Mathf.Max(actionLatch, 0.15f);
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;

            // 3. 播放冲刺动画和音效，发送冲刺信号
            PlayState("dash");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Dash, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PDash);
        }

        /// <summary>
        /// 更新冲刺缓冲。
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        private void UpdateDashBuffer(float dt)
        {
            // 1. 如果有新输入，记录方向和缓冲时间
            if (controller.CurrentDash != 0)
            {
                bufferedDashDirection = controller.CurrentDash;
                dashBufferTimer = mlpObjectsData.DashInputBuffer;
                return;
            }

            // 2. 缓冲期结束后清除记录的方向
            if (dashBufferTimer <= 0f)
            {
                return;
            }

            dashBufferTimer -= dt;
            if (dashBufferTimer <= 0f)
            {
                dashBufferTimer = 0f;
                bufferedDashDirection = 0;
            }
        }

        /// <summary>
        /// 开始防守或假动作。
        /// </summary>
        private void BeginBlockOrPump()
        {
            // 1. 判断是假动作（持球时）还是盖帽（无球时）
            blockPumpIsPump = WithBall;
            blockPumpPhase = BlockPumpPhase.Starting;
            blockPumpTimer = blockPumpIsPump ? mlpObjectsData.PumpStartDuration : mlpObjectsData.BlockStartDuration;
            blockPumpStartReady = false;
            blockPumpEndReady = false;

            // 2. 停止水平移动，锁定操作时间
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, blockPumpTimer);
            // 3. 盖帽时禁止拾球，假动作时发送假动作信号
            if (!blockPumpIsPump)
            {
                canTakeInHands = false;
            }
            else
            {
                GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Pump, Side, playerNo);
            }

            // 4. 播放对应的起跳动画
            PlayState(blockPumpIsPump ? "pumpStart" : "blockStart");
        }

        /// <summary>
        /// 激活跳跃封盖。
        /// </summary>
        private void ActivateJumpBlock()
        {
            // 1. 激活跳跃封盖碰撞检测，禁止拾球
            jumpBlockActive = true;
            canTakeInHands = false;
        }

        /// <summary>
        /// 判断是否应预备跳跃封盖。
        /// </summary>
        /// <returns>条件满足时返回 true；否则返回 false。</returns>
        private bool ShouldPrimeJumpBlock()
        {
            // 1. 如果不需要盖帽则直接返回
            if (!needBlock)
            {
                return false;
            }

            // 2. 检查持球者是否是对手，或篮球是否正在飞向篮筐
            var holder = GameCore.FindBallHolder();
            if (holder != null)
            {
                return holder.Side != Side;
            }

            var ball = GameCore.Ball;
            return ball != null && ball.State == "shooting" && ball.Side != Side;
        }

        /// <summary>
        /// 更新跳跃封盖威胁状态。
        /// </summary>
        private void UpdateJumpBlockThreat()
        {
            // 1. 如果正在盖帽但条件不再满足，则关闭盖帽碰撞
            if (jumpBlockActive && !ShouldPrimeJumpBlock())
            {
                jumpBlockActive = false;
            }
        }

        /// <summary>
        /// 更新防守或假动作状态。
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        private void UpdateBlockOrPump(float dt)
        {
            // 1. 停止水平移动，持球时保持球在手中
            Velocity.x = 0f;
            if (WithBall)
            {
                GameCore.Ball.TakeInHands(Side);
            }

            // 2. 起跳阶段：等待动画完成或计时器结束，切换到保持阶段
            if (blockPumpPhase == BlockPumpPhase.Starting)
            {
                blockPumpTimer -= dt;
                if (blockPumpStartReady || blockPumpTimer <= 0f)
                {
                    blockPumpPhase = BlockPumpPhase.Holding;
                    blockPumpTimer = 0f;
                    blockPumpStartReady = false;
                    actionLatch = 0f;
                    if (!blockPumpIsPump)
                    {
                        controller.PlayerOnBlock();
                    }
                }

                UpdateGraphic();
                return;
            }

            // 3. 保持阶段：等待玩家松开按键，切换到落地阶段
            if (blockPumpPhase == BlockPumpPhase.Holding)
            {
                if (controller.ReleaseBlockOrPump(dt))
                {
                    blockPumpPhase = BlockPumpPhase.Ending;
                    blockPumpTimer = blockPumpIsPump ? mlpObjectsData.PumpEndDuration : mlpObjectsData.BlockEndDuration;
                    blockPumpEndReady = false;
                    actionLatch = Mathf.Max(actionLatch, blockPumpTimer);
                    if (!blockPumpIsPump)
                    {
                        canTakeInHands = true;
                    }

                    PlayState(blockPumpIsPump ? "pumpEnd" : "blockEnd");
                }

                UpdateGraphic();
                return;
            }

            // 4. 落地阶段：等待动画完成或计时器结束，恢复待机状态
            blockPumpTimer -= dt;
            if (blockPumpEndReady || blockPumpTimer <= 0f)
            {
                blockPumpPhase = BlockPumpPhase.None;
                blockPumpTimer = 0f;
                blockPumpEndReady = false;
                canTakeInHands = true;
                actionLatch = 0f;
                PlayState(WithBall ? "idle_wb" : "idle");
            }

            UpdateGraphic();
        }

        /// <summary>
        /// 尝试开始扣篮。
        /// </summary>
        /// <returns>操作成功时返回 true；否则返回 false。</returns>
        private bool TryStartDunk()
        {
            // 1. 获取扣篮类型（不在扣篮区域则返回0）
            var dunkType = GetDunkType();
            if (dunkType == 0)
            {
                return false;
            }

            // 2. 进入扣篮状态，发送扣篮信号，播放音效
            BeginDunkState(dunkType);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Dunk, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        private bool TryTutorialPutbackDunk()
        {
            // 1. 检查是否满足教程补扣条件（已预备、不在地面、篮球存在等）
            var ball = GameCore.Ball;
            if (!tutorialPutbackDunkPrimed || WithBall || IsGrounded || ball == null || !ball.IsInGame || removedFromPlay || isSuperShot || isDunking)
            {
                return false;
            }

            // 2. 检查篮球状态是否允许补扣
            if (ball.State == "inHands" || ball.State == "score" || ball.State == "alleyOop" || !IsTutorialPutbackBallInWindow(ball))
            {
                return false;
            }

            // 3. 检查球员与篮球的距离是否在补扣范围内
            var delta = ball.Position - Position;
            if (Mathf.Abs(delta.x) > TutorialPutbackCatchWindowX ||
                Mathf.Abs(delta.y) > TutorialPutbackCatchWindowY ||
                Position.y > mlpObjectsData.DunkZone2Y + TutorialPutbackDunkYBonus)
            {
                return false;
            }

            // 4. 获取教程补扣类型，设置高完成率
            var dunkType = GetTutorialPutbackDunkType();
            if (dunkType == 0)
            {
                return false;
            }

            // 5. 清除预备标记，从物理中移除篮球，进入扣篮状态
            tutorialPutbackDunkPrimed = false;
            tutorialDunkCompletionChanceOverride = Mathf.Max(tutorialDunkCompletionChanceOverride, TutorialPutbackCompletionChance);
            ball.RemoveFromPhysics();
            BeginDunkState(dunkType);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.PutbackDunk, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        private int GetTutorialPutbackDunkType()
        {
            var normalDunkType = GetDunkType();
            if (normalDunkType != 0)
            {
                return normalDunkType;
            }

            var paintStart = Side == 1 ? mlpObjectsData.PaintStartX : mlpConstants.Width - mlpObjectsData.PaintMiddleX;
            var paintMiddle = Side == 1 ? mlpObjectsData.PaintMiddleX : mlpConstants.Width - mlpObjectsData.PaintStartX;
            if (Position.x >= paintStart &&
                Position.x <= paintMiddle &&
                Position.y <= mlpObjectsData.DunkZone2Y + TutorialPutbackDunkYBonus)
            {
                return 1 + Mathf.RoundToInt(2f * Random.value);
            }

            if ((Position.x - paintStart) * Side < 0f &&
                Position.y <= mlpObjectsData.DunkZone2Y + TutorialPutbackDunkYBonus)
            {
                return 1;
            }

            return 0;
        }

        private void BeginDunkState(int dunkType)
        {
            // 1. 标记进入扣篮状态，锁定操作
            isDunking = true;
            canDoAction = false;
            canThrow = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = DunkTravelDuration(dunkType);
            dunkReleaseTime = DunkReleaseTime(dunkType);
            // 2. 记录起跳位置和篮下目标点，停止移动
            dunkStartPosition = Position;
            dunkTargetPosition = new Vector2(DunkTargetX(), mlpObjectsData.DunkY);
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            actionLatch = Mathf.Max(actionLatch, DunkActionLockDuration(dunkType));
            // 3. 禁止拾球，显示球槽，播放对应类型的扣篮动画
            canTakeInHands = false;
            SetDunkBallSlotsHidden(false);
            PlayState("dunk" + dunkType);
        }

        /// <summary>
        /// 获取扣篮类型。
        /// </summary>
        /// <returns>计算得出的扣篮类型。</returns>
        private int GetDunkType()
        {
            // 1. 计算油漆区（篮下区域）的左右边界
            var paintStart = Side == 1 ? mlpObjectsData.PaintStartX : mlpConstants.Width - mlpObjectsData.PaintMiddleX;
            var paintMiddle = Side == 1 ? mlpObjectsData.PaintMiddleX : mlpConstants.Width - mlpObjectsData.PaintStartX;
            var tutorialDunkYBonus = tutorialPerfectDunkPrimed ? 36f : 0f;
            // 2. 在油漆区深处且高度足够时，随机返回扣篮类型2或3
            if (Position.x >= paintStart && Position.x <= paintMiddle && Position.y <= mlpObjectsData.DunkZone1Y + tutorialDunkYBonus)
            {
                return 1 + Mathf.RoundToInt(2f * Random.value);
            }

            // 3. 在油漆区边缘且高度足够时，返回基础扣篮类型1
            if ((Position.x - paintStart) * Side < 0f && Position.y <= mlpObjectsData.DunkZone2Y + tutorialDunkYBonus)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// 获取扣篮目标 X 坐标。
        /// </summary>
        /// <returns>计算得出的目标 X 坐标。</returns>
        private float DunkTargetX()
        {
            return Side == 1 ? mlpObjectsData.DunkX : mlpConstants.Width - mlpObjectsData.DunkX;
        }

        /// <summary>
        /// 获取扣篮持续时间。
        /// </summary>
        /// <param name="dunkType">扣篮类型</param>
        /// <returns>计算得出的持续时间。</returns>
        private static float DunkDuration(int dunkType)
        {
            return dunkType == 2
                ? mlpObjectsData.Dunk2Duration
                : dunkType == 3
                    ? mlpObjectsData.Dunk3Duration
                    : mlpObjectsData.Dunk1Duration;
        }

        private static float DunkTravelDuration(int dunkType)
        {
            return dunkType == 2
                ? mlpObjectsData.Dunk2TravelDuration
                : dunkType == 3
                    ? mlpObjectsData.Dunk3TravelDuration
                    : mlpObjectsData.Dunk1TravelDuration;
        }

        private static float DunkReleaseTime(int dunkType)
        {
            var eventTime = dunkType == 2
                ? mlpObjectsData.Dunk2ReleaseTime
                : dunkType == 3
                    ? mlpObjectsData.Dunk3ReleaseTime
                    : mlpObjectsData.Dunk1ReleaseTime;
            return eventTime / DunkAnimationSpeed(dunkType);
        }

        private static float DunkAnimationSpeed(int dunkType)
        {
            return dunkType == 2
                ? mlpObjectsData.Dunk2AnimationSpeed
                : dunkType == 3
                    ? mlpObjectsData.Dunk3AnimationSpeed
                    : mlpObjectsData.Dunk1AnimationSpeed;
        }

        private static float DunkActionLockDuration(int dunkType)
        {
            var animationDuration = DunkDuration(dunkType) / DunkAnimationSpeed(dunkType);
            return Mathf.Max(DunkTravelDuration(dunkType), animationDuration) + 0.12f;
        }

        private static float DunkTravelEase(float t)
        {
            t = Mathf.Clamp01(t);
            var inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        /// <summary>
        /// 更新扣篮状态。
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        private void UpdateDunk(float dt)
        {
            // 1. 推进扣篮计时器，计算进度（0~1）
            dunkTimer += dt;
            var t = dunkDuration > 0f ? Mathf.Clamp01(dunkTimer / dunkDuration) : 1f;

            // 2. 用缓动函数插值球员位置（从起跳点飞向篮下目标点）
            Position = Vector2.Lerp(dunkStartPosition, dunkTargetPosition, DunkTravelEase(t));
            IsGrounded = false;

            // 3. 到达释放时间点时，释放篮球（判定是否扣进）
            if (!dunkReleased && dunkTimer >= dunkReleaseTime)
            {
                ReleaseDunkBall();
            }

            // 4. 更新球员精灵位置
            UpdateGraphic();

            // 5. 动画未结束则继续，结束后重置扣篮状态
            if (t < 1f)
            {
                return;
            }

            isDunking = false;
            Position = dunkTargetPosition;
            Velocity = Vector2.zero;
            if (!dunkReleased)
            {
                ReleaseDunkBall();
            }
        }

        /// <summary>
        /// 释放扣篮篮球。
        /// </summary>
        private void ReleaseDunkBall()
        {
            // 1. 防止重复释放
            if (dunkReleased)
            {
                return;
            }

            // 2. 标记已释放，隐藏扣篮时手中的球槽
            dunkReleased = true;
            SetDunkBallSlotsHidden(true);

            // 3. 计算扣篮成功概率（教程完美扣篮必定成功，否则按 AI 技能等级概率）
            var completionChance = tutorialPerfectDunkPrimed
                ? 1f
                : tutorialDunkCompletionChanceOverride >= 0f
                    ? Mathf.Max(chanceToCompleteDunk, tutorialDunkCompletionChanceOverride)
                    : chanceToCompleteDunk;
            completionChance = Mathf.Clamp01(completionChance);
            tutorialPerfectDunkPrimed = false;
            tutorialDunkCompletionChanceOverride = -1f;

            // 4. 随机判定扣篮是否成功，通知比赛处理器和篮球对象
            var completed = Random.value <= completionChance;
            GameCore.MatchProcessor.Shoot(Side, IsHuman, completed ? 1 : 9, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, completed);
            if (!completed)
            {
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BBrick);
            }
        }

        /// <summary>
        /// 条件满足时恢复拾球能力。
        /// </summary>
        private void RestoreBallPickupIfReady()
        {
            // 1. 如果已可拾球或处于不可拾球状态则跳过
            if (canTakeInHands || stunTimer > 0f || stealAnimationActive || stealAttemptTimer >= 0f || actionLatch > 0f)
            {
                return;
            }

            // 2. 如果必定盖帽后拾球被锁定，落地后解除锁定
            if (guaranteedBlockPickupLocked)
            {
                if (!IsGrounded)
                {
                    return;
                }

                guaranteedBlockPickupLocked = false;
            }

            canTakeInHands = true;
        }

        /// <summary>
        /// 判断是否在篮板下方。
        /// </summary>
        /// <returns>在篮板下方时返回 true；否则返回 false。</returns>
        private bool IsUnderGlass()
        {
            return Side == -1
                ? Position.x > mlpConstants.Width - 200f && Position.x < mlpConstants.Width - 100f
                : Position.x > 100f && Position.x < 200f;
        }

        /// <summary>
        /// 判断扫掠点线段是否与矩形相交。
        /// </summary>
        /// <param name="start">球轨迹的起点</param>
        /// <param name="end">球轨迹的终点</param>
        /// <param name="minX">测试矩形的左边界</param>
        /// <param name="maxX">测试矩形的右边界</param>
        /// <param name="minY">测试矩形的下边界</param>
        /// <param name="maxY">测试矩形的上边界</param>
        /// <returns>相交时返回 true；否则返回 false。</returns>
        private static bool SweptPointIntersectsRect(Vector2 start, Vector2 end, float minX, float maxX, float minY, float maxY)
        {
            // 1. 如果起点或终点在矩形内则直接相交
            if (PointInsideRect(start, minX, maxX, minY, maxY) || PointInsideRect(end, minX, maxX, minY, maxY))
            {
                return true;
            }

            // 2. 用 Cohen-Sutherland 裁剪算法检测线段与矩形是否相交
            var direction = end - start;
            var tMin = 0f;
            var tMax = 1f;
            return ClipSegment(-direction.x, start.x - minX, ref tMin, ref tMax) &&
                   ClipSegment(direction.x, maxX - start.x, ref tMin, ref tMax) &&
                   ClipSegment(-direction.y, start.y - minY, ref tMin, ref tMax) &&
                   ClipSegment(direction.y, maxY - start.y, ref tMin, ref tMax);
        }

        /// <summary>
        /// 判断点是否在矩形内。
        /// </summary>
        /// <param name="point">待检测的点</param>
        /// <param name="minX">测试矩形的左边界</param>
        /// <param name="maxX">测试矩形的右边界</param>
        /// <param name="minY">测试矩形的下边界</param>
        /// <param name="maxY">测试矩形的上边界</param>
        /// <returns>点在矩形内时返回 true；否则返回 false。</returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// 裁剪线段（Cohen-Sutherland 算法）。
        /// 用于判断扫掠轨迹线段是否穿过矩形区域。
        /// </summary>
        /// <param name="p">裁剪平面的方向分量</param>
        /// <param name="q">裁剪平面的距离分量</param>
        /// <param name="tMin">当前最小参数裁剪值</param>
        /// <param name="tMax">当前最大参数裁剪值</param>
        /// <returns>线段未被完全裁剪时返回 true；否则返回 false。</returns>
        private static bool ClipSegment(float p, float q, ref float tMin, ref float tMax)
        {
            // 1. 方向分量为零时，线段平行于裁剪边界，检查是否在内侧
            if (Mathf.Approximately(p, 0f))
            {
                return q >= 0f;
            }

            // 2. 计算交点参数（距离/方向）
            var ratio = q / p;
            if (p < 0f)
            {
                // 3. 从负方向进入：更新 tMin（进入点），超出范围则不相交
                if (ratio > tMax)
                {
                    return false;
                }

                if (ratio > tMin)
                {
                    tMin = ratio;
                }
            }
            else
            {
                // 4. 从正方向进入：更新 tMax（离开点），超出范围则不相交
                if (ratio < tMin)
                {
                    return false;
                }

                if (ratio < tMax)
                {
                    tMax = ratio;
                }
            }

            return true;
        }
    }
}
