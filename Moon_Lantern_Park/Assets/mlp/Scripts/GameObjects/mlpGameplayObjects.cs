// 文件作用：游戏场景物体管理（球场、篮筐、篮球、传送特效、护盾、技能特效）
// 概括：创建和管理比赛中所有可见的游戏物体：球场背景和灯光、篮筐和篮网、篮球及其物理运动、传送门特效、护盾特效、技能激活特效。这个文件非常大，涵盖了比赛中大部分视觉元素的创建和更新逻辑。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    public static class mlpGameplaySpriteLoader
    {
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

    public sealed class mlpArenaObject
    {
        private const float ArenaLogicalWidth = 1398f;
        private const float ArenaLogicalHeight = 480f;

        public GameObject Graphic { get; }

        /// <summary>
        /// 创建球场背景精灵并缩放到逻辑比赛尺寸。
        /// </summary>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpArenaObject(Transform parent)
        {
            Graphic = new GameObject("ArenaObject");
            Graphic.transform.SetParent(parent, false);
            var renderer = Graphic.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.ArenaBackdrop,
                0f,
                0f,
                mlpAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000");
            renderer.sortingOrder = 0;
            mlpRender.ApplyPixelTransform(Graphic.transform, -299f, 0f);
            ApplyArenaLogicalScale(Graphic.transform, renderer.sprite);
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
    }

    public sealed class mlpBasketObject
    {
        private readonly List<LineRenderer> netLines = new List<LineRenderer>();
        private readonly int side;
        private GameObject graphic;
        private GameObject frontEar;
        private float netPulse;

        public int Side => side;
        public float Center { get; }
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
            graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(parent, false);
            mlpRender.ApplyPixelTransform(graphic.transform, Center, mlpObjectsData.BasketHeight, 0.05f);
            graphic.transform.localScale = new Vector3(mlpConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), mlpConstants.UnitsPerPixel, 1f);

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

            UpdateNetLines();
        }

        /// <summary>
        /// 重新定位十条篮网 LineRenderer 以模拟摆动的篮网网格。
        /// </summary>
        private void UpdateNetLines()
        {
            var left = Center - mlpObjectsData.BasketRadius + 2f;
            var right = Center + mlpObjectsData.BasketRadius - 2f;
            var middle = Center;
            var top = mlpObjectsData.BasketHeight + 3f;
            var sway = Mathf.Sin(Time.time * 18f) * 5f * netPulse;

            var pointsTop = new[]
            {
                new Vector2(left, top),
                new Vector2(middle, top),
                new Vector2(right, top)
            };
            var pointsMid = new[]
            {
                new Vector2(left + sway, top + 14f),
                new Vector2(middle - sway * 0.5f, top + 12f),
                new Vector2(right + sway, top + 14f)
            };
            var pointsBot = new[]
            {
                new Vector2(left - sway * 0.5f, top + 32f),
                new Vector2(middle + sway, top + 30f),
                new Vector2(right - sway * 0.5f, top + 32f)
            };

            SetLine(0, pointsTop[0], pointsMid[0]);
            SetLine(1, pointsMid[0], pointsBot[0]);
            SetLine(2, pointsTop[1], pointsMid[1]);
            SetLine(3, pointsMid[1], pointsBot[1]);
            SetLine(4, pointsTop[2], pointsMid[2]);
            SetLine(5, pointsMid[2], pointsBot[2]);
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

    public sealed class mlpBallObject
    {
        private const float MaxSubstepTravel = 8f;
        private const int MaxSubsteps = 8;
        private const float RimRestitution = 0.78f;
        private const float BackboardRestitution = 0.82f;
        private const float CollisionSoundCooldownDuration = 0.04f;
        private const float GuaranteedDunkScoreExtraX = 6f;

        private readonly GameObject graphic;
        private readonly GameObject shadow;
        private readonly mlpGameCore gameCore;
        private Vector2 previousPosition;
        private bool visibleNextFrame;
        private bool canScore;
        private bool upperSensorPassed;
        private bool guaranteedDunkScore;
        private bool tutorialGuaranteedScore;
        private int scoreArmedSide;
        private float pickupLockTimer;
        private float collisionSoundCooldown;
        private bool physicsRemoved;
        private mlpPlayerObject alleyOopPlayer;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Side;
        public string State = "up";
        public float LastShotX;
        public Vector2 PreviousPosition => previousPosition;
        public bool IsInGame => State != "inHands" && !physicsRemoved;
        public bool IsBlockable => State == "shooting";
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
            this.gameCore = gameCore;
            graphic = new GameObject("BallObject");
            graphic.transform.SetParent(parent, false);
            var graphicRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicRenderer.sprite = ResolveBallSprite();
            graphicRenderer.sortingOrder = 50;
            mlpRender.ApplyPixelTransform(graphic.transform, mlpConstants.Width2, mlpObjectsData.BallIndentYCenter, 0.2f);
            shadow = new GameObject("BallShadow");
            shadow.transform.SetParent(parent, false);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.PlayerShadowBall,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "ShadowMC0002");
            shadowRenderer.sortingOrder = 3;
            mlpRender.ApplyPixelTransform(shadow.transform, mlpConstants.Width2, mlpObjectsData.FloorY, 0.02f);
            shadow.transform.localScale *= 0.7f;
            Restart();
        }

        /// <summary>
        /// 将篮球重置到中场位置并赋予向上速度，为新一轮做好准备。
        /// </summary>
        public void Restart()
        {
            Position = new Vector2(mlpConstants.Width2, mlpObjectsData.BallIndentYCenter);
            previousPosition = Position;
            Velocity = new Vector2(0f, mlpObjectsData.BallUpVelocityY);
            State = "up";
            physicsRemoved = false;
            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            ResetScoring(false);
            Show();
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
            Side = side;
            Position = new Vector2(x, y);
            previousPosition = Position;
            LastShotX = x;
            var baseVelocity = CalcThrowVel(x, y, 0f);
            var distanceToBasket = side == 1 ? x : mlpConstants.Width - x;
            var runningDispersion = Mathf.Abs(playerVelocityX) / mlpObjectsData.PlayerMoveWithBall * 0.1f;
            var dispersion = CalcDispersion(distanceToBasket, y, runningDispersion, accuracy);
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
            Side = side;
            var basketX = side == 1 ? mlpObjectsData.BasketCenter : mlpObjectsData.BasketCenter2;
            Position = new Vector2(completed ? basketX + 17f * side : basketX, 170f);
            previousPosition = Position;
            LastShotX = Position.x;
            Velocity = completed ? new Vector2(-260f * side, 400f) : new Vector2(-550f * side, 400f);
            State = "dunk";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(true);
            guaranteedDunkScore = completed;
            if (completed)
            {
                // 完成的扣篮应在篮球释放后计为成功进球。
                // 预先激活上传感器可避免因粗略子步长导致的"先下后上"误判。
                upperSensorPassed = true;
                scoreArmedSide = side;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            pickupLockTimer = mlpObjectsData.DunkPickupLock;
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
            if (!IsInGame)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            if (Position.y >= mlpObjectsData.BallFloorY)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            var gravity = mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass;
            if (gravity <= 0.0001f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            var floorDelta = mlpObjectsData.BallFloorY - Position.y;
            var discriminant = Velocity.y * Velocity.y + 2f * gravity * floorDelta;
            if (discriminant <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            var timeToFloor = (-Velocity.y + Mathf.Sqrt(discriminant)) / gravity;
            if (timeToFloor <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

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
            var direction = Position.x >= blocker.Position.x ? 1f : -1f;
            Side = blocker.Side;
            previousPosition = Position;
            Velocity = new Vector2(
                direction * (280f + 100f * Random.value),
                -250f - 150f * Random.value);
            State = "block";
            physicsRemoved = false;
            alleyOopPlayer = null;
            // 保持当前投篮激活状态，使干净的盖帽后篮球仍能穿过
            // 原始篮筐时被传感器链计分。
            gameCore.MatchProcessor.Block(blocker.Side, blocker.IsHuman);
            Show();
            UpdateGraphic();
            mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
            gameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Block, blocker.Side, blocker.PlayerNo);
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
            Side = side;
            Position = new Vector2(x, y);
            previousPosition = Position;
            LastShotX = Position.x;
            Velocity = CalcVel(
                x,
                y,
                side == 1 ? mlpObjectsData.AlleyOopX : mlpConstants.Width - mlpObjectsData.AlleyOopX,
                mlpObjectsData.AlleyOopY,
                150f);
            State = "alleyOop";
            alleyOopPlayer = player;
            physicsRemoved = false;
            ResetScoring(false);
            Show();
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
            if (State == "inHands" || physicsRemoved)
            {
                return;
            }

            pickupLockTimer = Mathf.Max(0f, pickupLockTimer - dt);
            collisionSoundCooldown = Mathf.Max(0f, collisionSoundCooldown - dt);
            if (alleyOopPlayer != null && Velocity.y > 0f)
            {
                alleyOopPlayer.ContinueAlleyOop();
                alleyOopPlayer = null;
            }

            var minSubsteps = 1;
            if (State == "dunk")
            {
                minSubsteps = 5;
            }
            else if (State == "shooting" || State == "basket" || State == "block" || State == "alleyOop")
            {
                minSubsteps = 3;
            }

            var steps = Mathf.Clamp(
                Mathf.Max(minSubsteps, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(Velocity.x), Mathf.Abs(Velocity.y)) * dt / MaxSubstepTravel)),
                minSubsteps,
                MaxSubsteps);
            var stepDt = dt / steps;
            for (var i = 0; i < steps; i++)
            {
                previousPosition = Position;
                Velocity.y += mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass * stepDt;
                Position += Velocity * stepDt;

                gameCore.TryBlockBall();
                ResolveFloorBounce();
                ResolveWallBounce();
                if (State != "alleyOop")
                {
                    ResolveBasket(basketLeft, 1);
                    ResolveBasket(basketRight, -1);
                    gameCore.TryShieldBall();
                }

                TryResolveGuaranteedDunkScore(basketLeft, basketRight);
            }

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
            var glassTop = basket.Height + mlpObjectsData.GlassY;
            var glassBottom = glassTop + mlpObjectsData.GlassHeight;
            if (Position.y + mlpObjectsData.BallRadius < glassTop || Position.y - mlpObjectsData.BallRadius > glassBottom)
            {
                return;
            }

            if (basket.Side == -1)
            {
                var planeX = mlpObjectsData.GlassWidth;
                if (Velocity.x < 0f && Position.x - mlpObjectsData.BallRadius <= planeX)
                {
                    Position.x = planeX + mlpObjectsData.BallRadius;
                    Velocity.x = Mathf.Abs(Velocity.x) * BackboardRestitution;
                    Velocity.y *= 0.97f;
                    SetBasketState();
                    PlayBasketSound(2);
                }

                return;
            }

            var rightPlaneX = mlpConstants.Width - mlpObjectsData.GlassWidth;
            if (Velocity.x > 0f && Position.x + mlpObjectsData.BallRadius >= rightPlaneX)
            {
                Position.x = rightPlaneX - mlpObjectsData.BallRadius;
                Velocity.x = -Mathf.Abs(Velocity.x) * BackboardRestitution;
                Velocity.y *= 0.97f;
                SetBasketState();
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
            var combinedRadius = mlpObjectsData.BallRadius + mlpObjectsData.BasketPartRadius;
            var offset = Position - rimCenter;
            var distanceSquared = offset.sqrMagnitude;
            if (distanceSquared >= combinedRadius * combinedRadius)
            {
                return;
            }

            var distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSquared));
            var normal = distanceSquared > 0.0001f
                ? offset / distance
                : new Vector2(Mathf.Sign(Position.x - rimCenter.x), -1f).normalized;
            if (normal.sqrMagnitude < 0.1f)
            {
                normal = Vector2.up;
            }
            Position = rimCenter + normal * combinedRadius;

            var velocityIntoRim = Vector2.Dot(Velocity, normal);
            if (velocityIntoRim < 0f)
            {
                Velocity -= (1f + RimRestitution) * velocityIntoRim * normal;
                Velocity *= 0.985f;
            }

            SetBasketState();
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
            if (!canScore)
            {
                return;
            }

            if (scoreArmedSide != 0 && scoringSide != scoreArmedSide)
            {
                return;
            }

            if (TouchesSensor(previousPosition, Position, basket.Center, basket.Height + mlpObjectsData.SensorUp))
            {
                upperSensorPassed = true;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            if (!TouchesSensor(previousPosition, Position, basket.Center, basket.Height + mlpObjectsData.SensorDown))
            {
                return;
            }

            var matchProcessorReady = gameCore.MatchProcessor.ProcessSensor(1);
            if (matchProcessorReady || (guaranteedDunkScore && scoringSide == scoreArmedSide)
                || (tutorialGuaranteedScore && scoringSide == scoreArmedSide))
            {
                CommitScore(scoringSide);
            }
            else
            {
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
            if (!canScore || !guaranteedDunkScore || scoreArmedSide == 0)
            {
                return;
            }

            var armedBasket = scoreArmedSide == 1 ? basketLeft : basketRight;
            if (armedBasket == null)
            {
                return;
            }

            var minX = armedBasket.Center - mlpObjectsData.SensorHalf - mlpObjectsData.BallRadius - GuaranteedDunkScoreExtraX;
            var maxX = armedBasket.Center + mlpObjectsData.SensorHalf + mlpObjectsData.BallRadius + GuaranteedDunkScoreExtraX;
            var crossedDown = previousPosition.y <= armedBasket.Height + mlpObjectsData.SensorDown &&
                              Position.y >= armedBasket.Height + mlpObjectsData.SensorDown;
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

            if (!tutorialGuaranteedScore)
            {
                arc *= 1f + 0.1f * (Random.value <= 0.5f ? -1f : 1f) * Random.value;
            }
            arc = Mathf.Min(arc, 185f);
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
            if (accuracy <= -0.5f)
            {
                return 1f;
            }

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

            float distanceDispersion =
                distance <= 100f ? 0f :
                distance <= 200f ? 0.01f :
                distance <= 300f ? 0.02f :
                distance <= 400f ? 0.03f :
                distance <= 490f ? 0.04f :
                distance <= 540f ? 0.01f : 0.07f;

            var sign = Random.value < 0.5f ? -1f : 1f;
            var value = sign * (mlpObjectsData.Dispersion + vertical + distanceDispersion + accuracy + running) * Random.value;
            if (Mathf.Abs(value) <= 0.02f)
            {
                return 1f;
            }

            if (value < -0.08f)
            {
                return 2f;
            }

            if (value > 0.08f)
            {
                return 3f;
            }

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
            var themedSprite = mlpGameplaySpriteLoader.LoadBallThemeSprite(gameCore.MatchData.BallTheme, 0.5f, 0.5f);
            return themedSprite ?? mlpAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);
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

    public sealed class mlpTeleportFx
    {
        private enum TeleportPhase
        {
            Hidden,
            BlackExpand,
            BlackCollapse,
            WhiteFlash
        }

        private const float BlackExpandDuration = 0.06f;
        private const float BlackCollapseDuration = 0.07f;
        private const float BlackCollapseScaleDuration = 0.08f;
        private const float WhiteFlashDuration1 = 0.03f;
        private const float WhiteFlashDuration2 = 0.03f;
        private const float WhiteFlashDuration3 = 0.024f;
        private const float AnimationFps = 30f;

        private readonly GameObject graphic;
        private readonly Transform blackNode;
        private readonly SpriteRenderer blackRenderer;
        private readonly SpriteRenderer centerRenderer;
        private readonly SpriteRenderer whiteRenderer;
        private readonly SpriteRenderer animRenderer;
        private readonly Sprite[] frames;
        private readonly mlpCharacterSkillDefinition skillDefinition;
        private TeleportPhase phase = TeleportPhase.Hidden;
        private float phaseTime;

        /// <summary>
        /// 构建传送视觉特效，包括黑色扩展、中心点、动画帧和白色闪光。
        /// </summary>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpTeleportFx(Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            this.skillDefinition = skillDefinition;
            graphic = new GameObject("TeleportFx");
            graphic.transform.SetParent(parent, false);

            blackNode = new GameObject("TeleportBlack").transform;
            blackNode.SetParent(graphic.transform, false);
            blackRenderer = blackNode.gameObject.AddComponent<SpriteRenderer>();
            blackRenderer.sortingOrder = 74;
            blackRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport10000");

            var centerNode = new GameObject("TeleportCenter");
            centerNode.transform.SetParent(blackNode, false);
            centerRenderer = centerNode.AddComponent<SpriteRenderer>();
            centerRenderer.sortingOrder = 75;
            centerRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport20000");

            var animNode = new GameObject("TeleportAnim");
            animNode.transform.SetParent(graphic.transform, false);
            animRenderer = animNode.AddComponent<SpriteRenderer>();
            animRenderer.sortingOrder = 76;

            var whiteNode = new GameObject("TeleportWhite");
            whiteNode.transform.SetParent(graphic.transform, false);
            whiteRenderer = whiteNode.AddComponent<SpriteRenderer>();
            whiteRenderer.sortingOrder = 77;
            whiteRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            frames = new[]
            {
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30000"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30001"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30002"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30003")
            };

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
            if (phase == TeleportPhase.Hidden)
            {
                return;
            }

            phaseTime += dt;
            switch (phase)
            {
                case TeleportPhase.BlackExpand:
                    UpdateBlackExpand();
                    if (phaseTime >= BlackExpandDuration)
                    {
                        phase = TeleportPhase.BlackCollapse;
                        phaseTime = 0f;
                        animRenderer.enabled = true;
                    }
                    break;
                case TeleportPhase.BlackCollapse:
                    UpdateBlackCollapse();
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
                    UpdateWhiteFlash();
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
            blackRenderer.color = Color.Lerp(skillDefinition.SecondaryColor, Color.black, 0.22f);
            centerRenderer.color = skillDefinition.PrimaryColor;
            whiteRenderer.color = Color.Lerp(Color.white, skillDefinition.AccentColor, 0.42f);
            animRenderer.color = Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.AccentColor, 0.38f);
        }
    }

    public sealed class mlpShieldObject
    {
        private enum ShieldPhase
        {
            Hidden,
            Intro,
            Active,
            Fading
        }

        private const float IntroTime = 0.14f;
        private const float IntroDropTime = 0.12f;
        private const float IntroDropOffsetY = -600f;
        private const float IntroBlurScaleX = 1.08f;
        private const float IntroBlurScaleY = 1.16f;
        private const float ShowTime = 3f;
        private const float FadeTime = 0.5f;
        private const float AnimationFps = 30f;
        private const float GraphicXOffset = 23f;
        private const float GraphicYOffset = -62f;
        private const float CollisionRectTop = 30f;
        private const float CollisionRectWidth = 70f;
        private const float CollisionRectHeight = 10f;
        private const float CollisionRectLeftLeftSide = -23f;
        private const float CollisionRectLeftRightSide = -49f;
        private const float StartSpriteLocalX = 1f;

        private readonly int side;
        private readonly mlpBasketObject basket;
        private readonly GameObject graphic;
        private readonly SpriteRenderer blurRenderer;
        private readonly SpriteRenderer startRenderer;
        private readonly SpriteRenderer animRenderer;
        private readonly Sprite[] frames;
        private readonly mlpCharacterSkillDefinition skillDefinition;

        private ShieldPhase phase = ShieldPhase.Hidden;
        private float phaseTime;
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

            graphic = new GameObject(side == -1 ? "ShieldLeft" : "ShieldRight");
            graphic.transform.SetParent(parent, false);

            var shieldStartSprite = mlpAtlasCache.Instance.SkillFx.Sprite("ShieldMC0000");
            startRenderer = CreateRenderer("ShieldStart", 63, shieldStartSprite);
            blurRenderer = CreateRenderer("ShieldBlur", 64, shieldStartSprite);
            animRenderer = CreateRenderer("ShieldAnim", 65, null);

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
            phase = ShieldPhase.Intro;
            phaseTime = 0f;
            alpha = 1f;
            basket?.HideEar();
            graphic.SetActive(true);
            startRenderer.enabled = false;
            blurRenderer.enabled = true;
            animRenderer.enabled = false;
            blurRenderer.transform.localPosition = new Vector3(StartSpriteLocalX, IntroDropOffsetY, 0f);
            blurRenderer.transform.localScale = new Vector3(IntroBlurScaleX, IntroBlurScaleY, 1f);
            startRenderer.transform.localScale = Vector3.one;
            animRenderer.transform.localScale = Vector3.one;
            ApplyAlpha();
            UpdateGraphic();
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PShield);
        }

        /// <summary>
        /// 每帧推进护盾的入场、激活和消退阶段。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void Update(float dt)
        {
            if (phase == ShieldPhase.Hidden)
            {
                return;
            }

            phaseTime += dt;
            switch (phase)
            {
                case ShieldPhase.Intro:
                    UpdateIntro();
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
                    UpdateActive();
                    if (phaseTime >= AnimationDuration + ShowTime)
                    {
                        phase = ShieldPhase.Fading;
                        phaseTime = 0f;
                        animRenderer.enabled = false;
                        basket?.ShowEar();
                    }
                    break;
                case ShieldPhase.Fading:
                    alpha = 1f - Mathf.Clamp01(phaseTime / FadeTime);
                    ApplyAlpha();
                    if (phaseTime >= FadeTime)
                    {
                        Reset();
                        return;
                    }
                    break;
            }

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
            if (!IsBlocking || ball == null || ball.State == "score")
            {
                return false;
            }

            var origin = ShieldOrigin;
            var rectLeft = side == -1 ? CollisionRectLeftLeftSide : CollisionRectLeftRightSide;
            var minX = origin.x + rectLeft - mlpObjectsData.BallRadius;
            var maxX = minX + CollisionRectWidth + mlpObjectsData.BallRadius * 2f;
            var minY = origin.y + CollisionRectTop - mlpObjectsData.BallRadius;
            var maxY = minY + CollisionRectHeight + mlpObjectsData.BallRadius * 2f;
            if (!SweptPointIntersectsRect(ball.PreviousPosition, ball.Position, minX, maxX, minY, maxY))
            {
                return false;
            }

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

    public sealed class mlpPlayerSkillFx
    {
        private enum FxMode
        {
            Hidden,
            Buff,
            Burst,
            Dash
        }

        private readonly GameObject root;
        private readonly SpriteRenderer glowRenderer;
        private readonly SpriteRenderer coreRenderer;
        private readonly SpriteRenderer accentRenderer;
        private readonly mlpCharacterSkillDefinition baseSkillDefinition;
        private mlpCharacterSkillDefinition skillDefinition;
        private FxMode mode = FxMode.Hidden;
        private float timer;
        private float duration;

        public mlpPlayerSkillFx(Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            baseSkillDefinition = skillDefinition;
            this.skillDefinition = skillDefinition;
            DBLiteFactory.Instance.EnsureLoaded();

            root = new GameObject("PlayerSkillFx");
            root.transform.SetParent(parent, false);

            glowRenderer = CreateRenderer("Glow", 17, mlpAtlasCache.Instance.Interface.Sprite("EmblemsBg0000"));
            coreRenderer = CreateRenderer("Core", 18, null);
            accentRenderer = CreateRenderer("Accent", 19, null);

            ApplyTheme(skillDefinition);
            Stop();
        }

        public void ApplyTheme(mlpCharacterSkillDefinition definition)
        {
            skillDefinition = definition;
            var useCustomArt = UsesCustomFxArt(definition.SkillType);
            coreRenderer.sprite = LoadSkillSprite(definition.SkillType, false);
            accentRenderer.sprite = LoadSkillSprite(definition.SkillType, true);
            glowRenderer.color = WithAlpha(definition.PrimaryColor, useCustomArt ? 0.14f : 0.18f);
            coreRenderer.color = useCustomArt
                ? WithAlpha(Color.white, 0.72f)
                : WithAlpha(definition.PrimaryColor, 0.5f);
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
            if (mode == FxMode.Hidden)
            {
                root.SetActive(false);
                return;
            }

            timer += dt;
            if (timer >= duration)
            {
                Stop();
                return;
            }

            var shouldRender = visible || mode == FxMode.Dash;
            if (!shouldRender)
            {
                root.SetActive(false);
                return;
            }

            mlpRender.ApplyPixelTransform(root.transform, position.x, position.y + 30f, 0.08f, 1f);
            var rootScale = root.transform.localScale;
            rootScale.x = Mathf.Abs(rootScale.x) * Mathf.Sign(facingDirection);
            root.transform.localScale = rootScale;

            var t = timer / duration;
            ResetRendererLayout();

            if (UsesCustomFxArt(skillDefinition.SkillType))
            {
                UpdateCustomFx(t);
                root.SetActive(true);
                return;
            }

            switch (mode)
            {
                case FxMode.Buff:
                {
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
                case mlpCharacterSkillType.HarvestTime:
                    UpdateHarvestTimeFx(t);
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

        private void UpdateHarvestTimeFx(float t)
        {
            var fade = mode == FxMode.Burst ? 1f - t : 0.96f;
            var pulse = mode == FxMode.Burst
                ? Mathf.Lerp(0.76f, 1f, t)
                : 0.97f + Mathf.Sin(Time.time * 6f) * 0.035f;

            SetRendererPixelSize(glowRenderer, 96f * pulse, 34f * pulse);
            SetRendererPixelSize(coreRenderer, 62f * pulse, 78f * pulse);
            SetRendererPixelSize(accentRenderer, 110f * pulse, 46f * pulse);

            glowRenderer.transform.localPosition = new Vector3(0f, -1f, 0f);
            coreRenderer.transform.localPosition = new Vector3(28f, -26f, 0f);
            accentRenderer.transform.localPosition = new Vector3(0f, -4f, 0f);

            coreRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.1f) * 2f);
            accentRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 7f);

            glowRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.09f + Mathf.Sin(Time.time * 5f) * 0.012f);
            coreRenderer.color = WithAlpha(Color.white, 0.62f * fade);
            accentRenderer.color = WithAlpha(Color.white, 0.52f * fade);
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
                case mlpCharacterSkillType.HarvestTime:
                    return accent ? mlpAssets.Images.SkillFxImages.HarvestTimeAccent : mlpAssets.Images.SkillFxImages.HarvestTimeCore;
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
                case mlpCharacterSkillType.HarvestTime:
                    return "wind1";
                case mlpCharacterSkillType.HexGate:
                    return "dbanims/circle2";
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
                case mlpCharacterSkillType.HarvestTime:
                    return "fx_smoke_2";
                case mlpCharacterSkillType.HexGate:
                    return "fx_spl2_0";
                case mlpCharacterSkillType.BadLuck:
                    return "dbanims/eye34635";
                default:
                    return "fx_Blur_mol0";
            }
        }
    }

    public sealed class mlpPlayerObject
    {
        private const float GroundCollisionMass = 3f;
        private const float GroundBlockCollisionMass = 6f;
        private const float GroundCollisionSpeedEpsilon = 5f;
        private const float GraphicDepthBase = 0.12f;
        private const float ShadowDepthBase = 0.02f;
        private const float TeamDepthStep = 0.01f;
        private const float PlayerDepthStep = 0.0025f;
        private const float ShadowDepthBiasScale = 0.25f;
        private const float TutorialPutbackCatchWindowX = 190f;
        private const float TutorialPutbackCatchWindowY = 230f;
        private const float TutorialPutbackDunkYBonus = 96f;
        private const float TutorialPutbackMinBallY = mlpObjectsData.BasketHeight + 22f;
        private const float TutorialPutbackMaxBallVelocityY = 560f;
        private const float TutorialPutbackCompletionChance = 1f;
        private const float ReboundMagnetDefaultDuration = 1.55f;
        private const float ReboundMagnetCatchDistanceX = 52f;
        private const float ReboundMagnetCatchDistanceY = 72f;
        private const float ReboundMagnetMinSpeed = 560f;
        private const float ReboundMagnetMaxSpeed = 920f;
        private const float GuaranteedBlockHoldDuration = 0.22f;
        private const float GuaranteedBlockHorizontalOffset = 20f;
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

        private readonly GameObject graphic;
        private readonly GameObject shadow;
        private readonly SpriteRenderer shadowRenderer;
        private readonly Sprite defaultShadowSprite;
        private readonly Sprite activeSkillShadowSprite;
        private readonly DBLiteArmature armature;
        private readonly IBLPlayerController controller;
        private readonly int teamIndex;
        private readonly int characterId;
        private readonly int playerNo;
        private readonly float renderDepthBias;
        private readonly int skillLevel;
        private readonly mlpCharacterSkillDefinition skillDefinition;
        private readonly int superId;
        private readonly int brainSlot;
        private readonly mlpAIDifficultyTuningProfile aiDifficultyTuning;
        private readonly bool hellEnhanced;
        private readonly float accuracy;
        private readonly float chanceToCompleteDunk;
        private readonly float superCoolDown;
        private readonly float superDunkX;
        private readonly float superDunkEndX;
        private readonly float superDunkEndY;
        private readonly float[] superDashTargets = new float[2];
        private readonly UseDelay dashDelay;
        private readonly mlpEnergyBarView energyBar;
        private readonly mlpTeleportFx teleportFx;
        private readonly mlpShieldObject shield;
        private readonly mlpPlayerSkillFx skillFx;
        private readonly float hellBonusSuperDashCooldownDuration;
        private readonly float hellBonusShieldCooldownDuration;
        private readonly HashSet<int> superDashHits = new HashSet<int>();
        private float actionLatch;
        private string visualState = "";
        private float dashTimer;
        private int dashDirection;
        private int bufferedDashDirection;
        private float dashBufferTimer;
        private bool readyForDash;
        private bool canDoAction;
        private bool pendingGroundThrow;
        private bool pendingStealAction;
        private bool stealAnimationActive;
        private bool alleyOopPendingThrow;
        private bool isDunking;
        private bool dunkReleased;
        private float dunkTimer;
        private float dunkDuration;
        private Vector2 dunkStartPosition;
        private Vector2 dunkTargetPosition;
        private BlockPumpPhase blockPumpPhase;
        private bool blockPumpIsPump;
        private float blockPumpTimer;
        private bool blockPumpStartReady;
        private bool blockPumpEndReady;
        private float stealAttemptTimer = -1f;
        private float stealAnimationTimer = -1f;
        private float stunTimer;
        private float facingDirection;
        private float stealFacingDirection;
        private bool canTakeInHands;
        private bool canThrow;
        private bool attackJump;
        private float pointOfThrow;
        private bool jumpBlockActive;
        private bool needBlock;
        private bool readyForSuper;
        private bool isSuperShot;
        private bool removedFromPlay;
        private float superChargeTime;
        private float hellBonusSuperDashCooldownTimer;
        private float hellBonusShieldCooldownTimer;
        private float graphicScaleMultiplier = 1f;
        private SuperPhase superPhase;
        private float superTimer;
        private float superDuration;
        private Vector2 superStartPosition;
        private Vector2 superTargetPosition;
        private bool dashToRight;
        private bool dashTeammatePending;
        private mlpPlayerObject teamMate;
        private bool hellOpeningChargeApplied;
        private bool hellNativeSuperRefundPending;
        private bool guaranteedBlockPickupLocked;
        private bool scoreUpgradeActive;
        private bool scoreUpgradePendingShot;
        private bool tutorialPerfectShotPrimed;
        private bool tutorialPerfectDunkPrimed;
        private bool tutorialPutbackDunkPrimed;
        private float tutorialDunkCompletionChanceOverride = -1f;
        private float tutorialAirMotionTimeScale = 1f;
        private bool tutorialJumpBlockAssist;
        private float flatScoreBonusTimer;
        private int flatScoreBonusPoints;
        private float moveBuffTimer;
        private bool moveBuffScoreBonusAvailable;
        private float pendingScoreRefundFraction;
        private float pendingScoreRefundTimer;
        private float reboundMagnetTimer;

        public mlpGameCore GameCore { get; }
        public Vector2 Position;
        public Vector2 Velocity;
        public int Side { get; }
        public bool WithBall { get; private set; }
        public bool IsHuman { get; }
        public bool IsGrounded { get; private set; } = true;
        public float AttackTargetX => Side == -1 ? mlpObjectsData.BasketCenter2 : mlpObjectsData.BasketCenter;
        public bool IsDashing => dashTimer > 0f;
        public bool IsBlocking => blockPumpPhase == BlockPumpPhase.Holding && !blockPumpIsPump;
        public bool HasGroundBlockBody => IsBlocking && IsGrounded && !removedFromPlay && !isSuperShot && stunTimer <= 0f;
        public bool IsPumping => blockPumpPhase != BlockPumpPhase.None && blockPumpIsPump;
        public bool IsMoving => Mathf.Abs(Velocity.x) > 20f;
        public bool IsDunking => isDunking;
        public float FacingDirection => facingDirection;
        public bool CanTakeInHands => canTakeInHands && !WithBall && !removedFromPlay;
        public bool CanAct => actionLatch <= 0f && stunTimer <= 0f && !stealAnimationActive && !isDunking && !isSuperShot;
        public bool CanResolveGroundBlock => IsGrounded && !removedFromPlay && !isSuperShot && !isDunking && stunTimer <= 0f && !stealAnimationActive;
        public bool ReadyForDash => readyForDash && dashTimer <= 0f && !isSuperShot;
        public int PlayerNo => playerNo;
        public int SkillLevel => skillLevel;
        public int SuperId => superId;
        public int CharacterId => characterId;
        public mlpCharacterSkillType SkillType => skillDefinition.SkillType;
        public bool UsesPossessionSkill => skillDefinition.UsesPossessionSkill;
        public bool UsesDashSkill => skillDefinition.UsesDashSkill;
        public bool UsesShieldSkill => skillDefinition.UsesBasketShield;
        public bool UsesFreezeSkill => skillDefinition.UsesFreezeSkill;
        public bool UsesReboundMagnetSkill => skillDefinition.UsesReboundMagnetSkill;
        public bool UsesGuaranteedBlockSkill => skillDefinition.UsesGuaranteedBlockSkill;
        public bool ReadyForSuper => !isSuperShot && (readyForSuper || mlpQuickTestSettings.Enabled);
        public bool CanUseHellBonusSuperDash => hellEnhanced && (mlpQuickTestSettings.Enabled || hellBonusSuperDashCooldownTimer <= 0f);
        public bool CanUseHellBonusShield => hellEnhanced && shield != null && (mlpQuickTestSettings.Enabled || hellBonusShieldCooldownTimer <= 0f) && shield.CanActivate;
        public bool IsSuperShot => isSuperShot;
        public bool NeedBlock => needBlock;
        public bool CanThrow => canThrow;
        public IBLPlayerController Controller => controller;
        private bool UsesHighlightedSkillShadow => skillDefinition.SkillType == mlpCharacterSkillType.CarnivalJackpot && (scoreUpgradeActive || scoreUpgradePendingShot);
        private float EffectiveSuperCoolDown => mlpQuickTestSettings.Enabled ? 0f : superCoolDown;

        public void ApplyBonusSuperCharge(float amount)
        {
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                RefreshQuickTestSuperReady();
                return;
            }

            if (amount <= 0f || readyForSuper || isSuperShot)
            {
                return;
            }

            superChargeTime = Mathf.Min(cooldown, superChargeTime + amount);
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

        private void RefreshQuickTestSuperReady()
        {
            if (!mlpQuickTestSettings.Enabled || isSuperShot)
            {
                return;
            }

            readyForSuper = true;
            superChargeTime = superCoolDown;
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
        /// <param name="skillLevel">AI 技能等级数值（越高越难）</param>
        /// <param name="parent">用于挂载视觉子对象的父级 Transform</param>
        public mlpPlayerObject(mlpGameCore gameCore, int teamIndex, int characterId, int playerNo, string playerBrain, int skillLevel, Transform parent)
        {
            GameCore = gameCore;
            this.teamIndex = teamIndex;
            this.characterId = characterId;
            this.playerNo = playerNo;
            this.skillLevel = skillLevel;
            Side = teamIndex == 0 ? -1 : 1;
            renderDepthBias = teamIndex * TeamDepthStep + playerNo * PlayerDepthStep;
            IsHuman = !playerBrain.StartsWith("B") && !playerBrain.StartsWith("T");
            brainSlot = mlpControlsData.ParseControllerSlot(playerBrain);
            skillDefinition = mlpCharacterSkillsData.Get(characterId);
            superId = skillDefinition.IconSuperId;
            aiDifficultyTuning = mlpAIDifficultyTuning.Get(mlpInventory.Instance.Difficulty);
            hellEnhanced = !IsHuman && mlpInventory.Instance.Difficulty == mlpAiDifficulty.Hell;

            var profile = mlpAISkillsData.Get(skillLevel);
            accuracy = profile.Accuracy;
            chanceToCompleteDunk = profile.ChanceToCompleteDunk;
            superCoolDown = profile.CoolDown;
            dashDelay = new UseDelay(mlpObjectsData.DashDelay * (hellEnhanced ? aiDifficultyTuning.DashCooldownMultiplier : 1f));
            hellBonusSuperDashCooldownDuration = hellEnhanced
                ? skillLevel >= 11 ? aiDifficultyTuning.BonusSuperDashBossCooldown : aiDifficultyTuning.BonusSuperDashCooldown
                : 0f;
            hellBonusShieldCooldownDuration = hellEnhanced
                ? skillLevel >= 11 ? aiDifficultyTuning.BonusShieldBossCooldown : aiDifficultyTuning.BonusShieldCooldown
                : 0f;

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

            graphic = new GameObject($"Player_{teamIndex}_{playerNo}");
            graphic.transform.SetParent(parent, false);

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
            activeSkillShadowSprite = skillDefinition.SkillType == mlpCharacterSkillType.CarnivalJackpot
                ? mlpGameplaySpriteLoader.LoadGameplaySprite(
                    mlpAssets.Images.GameplayImages.PlayerShadowPrimaryRed,
                    0.5f,
                    0.5f,
                    mlpAtlasCache.Instance.Gameplay,
                    "ShadowMC0000") ?? defaultShadowSprite
                : defaultShadowSprite;
            shadowRenderer.sortingOrder = 2;

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
                armature.AnimationComplete += OnAnimationComplete;
                armature.FrameEvent += OnAnimationFrameEvent;
            }
            else
            {
                CreateFallbackAvatar();
            }

            controller = IsHuman
                ? new mlpKeyboardController(playerBrain)
                : playerBrain.Length > 0 && (playerBrain[0] == 'T' || playerBrain[0] == 't')
                    ? new mlpTutorialOpponentController(this, skillLevel)
                    : mlpAIController.CreateForBrain(this, playerBrain, skillLevel);

            energyBar = IsHuman ? new mlpEnergyBarView(parent, brainSlot, skillDefinition, superCoolDown) : null;
            teleportFx = (skillDefinition.UsesTeleportDunk || skillDefinition.UsesGuaranteedBlockSkill) ? new mlpTeleportFx(parent, skillDefinition) : null;
            shield = skillDefinition.UsesBasketShield || hellEnhanced
                ? new mlpShieldObject(Side, Side == -1 ? gameCore.BasketLeft : gameCore.BasketRight, parent, skillDefinition)
                : null;
            skillFx = new mlpPlayerSkillFx(parent, skillDefinition);

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
            WithBall = false;
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            readyForDash = false;
            dashDelay.Activate();
            canDoAction = true;
            pendingGroundThrow = false;
            pendingStealAction = false;
            stealAnimationActive = false;
            alleyOopPendingThrow = false;
            isDunking = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = 0f;
            blockPumpPhase = BlockPumpPhase.None;
            blockPumpIsPump = false;
            blockPumpTimer = 0f;
            blockPumpStartReady = false;
            blockPumpEndReady = false;
            stealAttemptTimer = -1f;
            stealAnimationTimer = -1f;
            stunTimer = 0f;
            actionLatch = 0f;
            facingDirection = -Side;
            stealFacingDirection = facingDirection;
            canTakeInHands = true;
            canThrow = true;
            attackJump = false;
            jumpBlockActive = false;
            needBlock = false;
            removedFromPlay = false;
            graphicScaleMultiplier = 1f;
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
            teleportFx?.Hide();
            shield?.Reset();
            skillFx?.Stop();

            var x = mlpConstants.Width2 + Side * (playerNo == 0 ? mlpObjectsData.PlayerIndentX : 200f);
            if (startSide == Side)
            {
                x = Side == -1 ? mlpObjectsData.IndentGeneralX : mlpConstants.Width - mlpObjectsData.IndentGeneralX;
            }

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
            teleportFx?.Update(dt);
            shield?.Update(dt);
            UpdateSkillTimers(dt);
            UpdateReboundMagnet(dt);
            skillFx?.Update(dt, Position, facingDirection, !removedFromPlay && graphicScaleMultiplier > 0.05f);
            hellBonusSuperDashCooldownTimer = Mathf.Max(0f, hellBonusSuperDashCooldownTimer - dt);
            hellBonusShieldCooldownTimer = Mathf.Max(0f, hellBonusShieldCooldownTimer - dt);
            RefreshQuickTestSuperReady();

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

            actionLatch -= dt;
            if (!canDoAction && !stealAnimationActive && controller.ReadyForAction())
            {
                canDoAction = true;
            }

            if (!readyForDash && dashDelay.Update(dt) == 1)
            {
                readyForDash = true;
            }

            if (isSuperShot)
            {
                UpdateSuper(dt);
                return;
            }

            if (isDunking)
            {
                UpdateDunk(dt);
                return;
            }

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

            if (stealAnimationActive)
            {
                UpdateStealAnimation(dt);
                return;
            }

            controller.UpdateController(dt);
            UpdateDashBuffer(dt);
            UpdateFacing();
            UpdateJumpBlockThreat();

            if (blockPumpPhase != BlockPumpPhase.None)
            {
                UpdateBlockOrPump(dt);
                return;
            }

            if (stealAttemptTimer >= 0f)
            {
                stealAttemptTimer -= dt;
                if (stealAttemptTimer <= 0f)
                {
                    ResolveStealAttempt();
                }
            }

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

                var dashInput = controller.CurrentDash != 0
                    ? controller.CurrentDash
                    : dashBufferTimer > 0f ? bufferedDashDirection : 0;
                if (dashInput != 0 && IsGrounded && readyForDash)
                {
                    StartDash(dashInput);
                }
            }

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

            if (dashTimer <= 0f && controller.CurrentBlockOrPump && IsGrounded && actionLatch <= 0f)
            {
                BeginBlockOrPump();
            }

            if (TryStartSuper(controller.CurrentSuper))
            {
                UpdateGraphic();
                return;
            }

            if (!IsGrounded)
            {
                Velocity.y += mlpObjectsData.Gravity.y * 3f * dt * tutorialAirMotionTimeScale;
            }

            var verticalDt = IsGrounded ? dt : dt * tutorialAirMotionTimeScale;
            Position += new Vector2(Velocity.x * dt, Velocity.y * verticalDt);
            Position.x = Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
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

            if (WithBall)
            {
                GameCore.Ball.TakeInHands(Side);
            }
            else
            {
                RestoreBallPickupIfReady();
            }

            UpdateGraphic();
        }

        /// <summary>
        /// 在赛前倒计时期间更新冷却时间和输入就绪状态，不移动玩家。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        public void TickPreMatch(float dt)
        {
            teleportFx?.Update(dt);
            shield?.Update(dt);

            if (actionLatch > 0f)
            {
                actionLatch -= dt;
            }

            if (!canDoAction && !stealAnimationActive && controller.ReadyForAction())
            {
                canDoAction = true;
            }

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
            WithBall = true;
            jumpBlockActive = false;
            canTakeInHands = false;
            canThrow = IsGrounded || IsUnderGlass();
            attackJump = false;
            if (IsUnderGlass())
            {
                pointOfThrow = Position.x;
            }

            CancelStealAnimation(false);
            stunTimer = 0f;
            if (!removedFromPlay)
            {
                GameCore.Ball.TakeInHands(Side);
            }

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
            Position = position;
            Velocity = Vector2.zero;
            pointOfThrow = Position.x;
            if (!Mathf.Approximately(facing, 0f))
            {
                facingDirection = Mathf.Sign(facing);
                stealFacingDirection = facingDirection;
            }

            UpdateGraphic();
        }

        /// <summary>
        /// 立即将超能力量表充满，使玩家可以激活技能。
        /// </summary>
        public void TutorialChargeSuper()
        {
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                readyForSuper = true;
                superChargeTime = superCoolDown;
                energyBar?.SetCharge(1f);
                return;
            }

            readyForSuper = true;
            superChargeTime = cooldown;
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
            if (!WithBall)
            {
                return;
            }

            WithBall = false;
            jumpBlockActive = false;
            canThrow = false;
            attackJump = false;
            CancelStealAnimation(false);
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
            if (!WithBall || GameCore.Ball == null)
            {
                return;
            }

            FreeBall();
            GameCore.Ball.DropFromFreeze(Position + new Vector2(0f, -45f));
        }

        /// <summary>
        /// 处理篮球变为自由状态：重置投掷/盖帽状态并通知控制器。
        /// </summary>
        public void NotifyBallLoose()
        {
            WithBall = false;
            CancelStealAnimation(false);
            stunTimer = 0f;
            canThrow = false;
            attackJump = false;
            canTakeInHands = true;
            jumpBlockActive = false;
            needBlock = false;
            controller.BallOthers();
        }

        /// <summary>
        /// 通知控制器有人拾取了篮球，更新进攻/防守策略。
        /// </summary>
        /// <param name="holderSide">拾取篮球的球员所在侧</param>
        /// <param name="holderPlayerNo">当前持球者的球员编号</param>
        public void NotifyBallInHands(int holderSide, int holderPlayerNo)
        {
            if (scoreUpgradePendingShot)
            {
                ClearScoreUpgrade();
            }

            if (holderSide == Side)
            {
                controller.BallInOwnHands(holderPlayerNo);
                needBlock = false;
            }
            else
            {
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
            if (scoreUpgradeActive && shotSide == Side && shooterPlayerNo == playerNo)
            {
                scoreUpgradeActive = false;
                scoreUpgradePendingShot = true;
            }

            if (shotSide == Side)
            {
                controller.BallOwnShoot(shooterPlayerNo);
                needBlock = false;
            }
            else
            {
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
            superDuration = skillDefinition.SkillType == mlpCharacterSkillType.HexGate ? 0.34f : 0.4f;
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
            if (duration <= 0f || removedFromPlay || isSuperShot || isDunking)
            {
                return;
            }

            DropHeldBallForFreeze();
            stunTimer = Mathf.Max(stunTimer, duration);
            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            pendingGroundThrow = false;
            pendingStealAction = false;
            CancelStealAnimation(false);
            blockPumpPhase = BlockPumpPhase.None;
            blockPumpTimer = 0f;
            jumpBlockActive = false;
            canDoAction = false;
            canTakeInHands = false;
            Velocity = Vector2.zero;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            PlayState("stun");
            skillFx?.PlayBurst(Mathf.Min(duration, 0.8f), freezeDefinition);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Stun, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PStunned, 0.9f);
            GameCore.ShowHudBonusNotice("FROZEN 2 SEC!", 0.95f);
        }

        public int ResolveScorePoints(int basePoints, out string scoreNotice)
        {
            scoreNotice = null;
            var resolvedPoints = basePoints;

            if (scoreUpgradePendingShot && resolvedPoints >= 2)
            {
                resolvedPoints = Mathf.Min(5, resolvedPoints + 2);
                scoreUpgradePendingShot = false;
                scoreNotice = skillDefinition.ScoreNotice;
                skillFx?.PlayBurst(0.5f);
            }

            if (moveBuffTimer > 0f && moveBuffScoreBonusAvailable && skillDefinition.FlatScoreBonus > 0)
            {
                resolvedPoints += skillDefinition.FlatScoreBonus;
                moveBuffScoreBonusAvailable = false;
                moveBuffTimer = 0f;
                scoreNotice = string.IsNullOrEmpty(scoreNotice) ? skillDefinition.ScoreNotice : scoreNotice;
                skillFx?.PlayBurst(0.45f);
            }

            if (flatScoreBonusTimer > 0f && flatScoreBonusPoints > 0)
            {
                resolvedPoints += flatScoreBonusPoints;
                flatScoreBonusPoints = 0;
                flatScoreBonusTimer = 0f;
                scoreNotice = string.IsNullOrEmpty(scoreNotice) ? skillDefinition.ScoreNotice : scoreNotice;
                skillFx?.PlayBurst(0.45f);
            }

            return resolvedPoints;
        }

        public void OnScoreConfirmed()
        {
            if (pendingScoreRefundTimer > 0f && pendingScoreRefundFraction > 0f)
            {
                GrantSuperChargeFraction(pendingScoreRefundFraction);
                pendingScoreRefundFraction = 0f;
                pendingScoreRefundTimer = 0f;
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
            if (!IsGrounded || stunTimer > 0f || removedFromPlay || isSuperShot)
            {
                return -1f;
            }

            if (thiefFacingScaleX >= 0f)
            {
                return Position.x >= thiefX && Position.x <= thiefX + stealDistance
                    ? Mathf.Abs(Position.x - thiefX)
                    : -1f;
            }

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
            if (removedFromPlay)
            {
                return false;
            }

            var hadBall = WithBall;
            WithBall = false;
            dashTimer = 0f;
            dashDirection = 0;
            CancelStealAnimation(false);
            pendingGroundThrow = false;
            canThrow = false;
            attackJump = false;
            var stunDuration = mlpObjectsData.StunDuration * (hellEnhanced ? aiDifficultyTuning.StunDurationMultiplier : 1f);
            stunTimer = Mathf.Max(stunTimer, stunDuration);
            canDoAction = false;
            jumpBlockActive = false;
            canTakeInHands = false;
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            PlayState("stun");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Stun, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PStunned, 0.9f);

            if (hadBall && applyBallSteal && GameCore.Ball != null)
            {
                var delta = Position.x - thiefX;
                var direction = delta > 0f ? 1 : -1;
                var distanceFactor = Mathf.Clamp01(Mathf.Abs(delta) / mlpObjectsData.StealDistance);
                GameCore.Ball.ApplySteal(Position + new Vector2(0f, -45f), distanceFactor, direction);
            }

            return hadBall;
        }

        /// <summary>
        /// 检测是否可以拾取自由球。
        /// </summary>
        /// <param name="ball">要检测的篮球对象</param>
        /// <returns>可拾取时返回距离平方；否则返回 -1。</returns>
        public float CheckLooseBallPickup(mlpBallObject ball)
        {
            if (ball == null || !ball.CanBeTakenInHands || !CanTakeInHands)
            {
                return -1f;
            }

            var delta = ball.Position - Position;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            if (absX > mlpObjectsData.BallPickupDistanceX || absY > mlpObjectsData.BallPickupDistanceY)
            {
                return -1f;
            }

            return delta.sqrMagnitude;
        }

        /// <summary>
        /// 尝试盖帽阻挡篮球。
        /// </summary>
        /// <param name="ball">要检测或影响的篮球对象</param>
        /// <returns>成功阻挡时返回 true；否则返回 false。</returns>
        public bool TryBlockBall(mlpBallObject ball)
        {
            if (!jumpBlockActive || ball == null || !ball.IsBlockable || ball.Side == Side || removedFromPlay || isSuperShot)
            {
                return false;
            }

            var start = ball.PreviousPosition;
            var end = ball.Position;
            if ((start.x - Position.x) * ball.Side <= 0f &&
                (end.x - Position.x) * ball.Side <= 0f)
            {
                return false;
            }

            var blockWidth = mlpObjectsData.JumpBlockWidth + (tutorialJumpBlockAssist ? 58f : 0f);
            var blockHeight = mlpObjectsData.JumpBlockHeight + (tutorialJumpBlockAssist ? 42f : 0f);
            var topBonus = tutorialJumpBlockAssist ? 18f : 0f;
            var bottomBonus = tutorialJumpBlockAssist ? 16f : 0f;
            var minX = Position.x - blockWidth * 0.5f - mlpObjectsData.BallRadius;
            var maxX = Position.x + blockWidth * 0.5f + mlpObjectsData.BallRadius;
            var minY = Position.y - blockHeight - mlpObjectsData.BallRadius - topBonus;
            var maxY = Position.y + mlpObjectsData.BallRadius + bottomBonus;
            if (!SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY))
            {
                return false;
            }

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
            RefreshQuickTestSuperReady();
            if (!pressed || !readyForSuper || GameCore.IsSuperShot)
            {
                return false;
            }

            if (skillDefinition.RequiresBallToCast && !WithBall)
            {
                return false;
            }

            if (skillDefinition.UsesDashSkill && (!IsGrounded || stunTimer > 0f || isDunking))
            {
                return false;
            }

            if (skillDefinition.UsesReboundMagnetSkill && !CanUseReboundMagnet())
            {
                return false;
            }

            if (skillDefinition.UsesGuaranteedBlockSkill && !CanUseGuaranteedBlock())
            {
                return false;
            }

            StartSuper(true);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Super, Side, playerNo);
            GameCore.ShowHudBonusNotice(skillDefinition.ActivateNotice, 0.95f);
            skillFx?.PlayBurst();

            switch (skillDefinition.SkillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    MakeSuperDash();
                    return true;
                case mlpCharacterSkillType.CarnivalJackpot:
                    MakeScoreUpgradeBuff();
                    return true;
                case mlpCharacterSkillType.GhostSail:
                    MakeShield();
                    return true;
                case mlpCharacterSkillType.BloodMoonBlink:
                    MakeAlleyOop();
                    return true;
                case mlpCharacterSkillType.WaxOverdrive:
                    MakeWaxOverdrive();
                    return true;
                case mlpCharacterSkillType.HarvestTime:
                    MakeScoreUpgradeBuff();
                    return true;
                case mlpCharacterSkillType.HexGate:
                    MakeAlleyOop();
                    return true;
                case mlpCharacterSkillType.BadLuck:
                    MakeFreeze();
                    return true;
                case mlpCharacterSkillType.ReboundMagnet:
                    MakeReboundMagnet();
                    return true;
                case mlpCharacterSkillType.SureBlock:
                    MakeGuaranteedBlock();
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
            if (!CanUseHellBonusSuperDash || GameCore.IsSuperShot || isSuperShot || removedFromPlay || !IsGrounded || stunTimer > 0f || isDunking)
            {
                return false;
            }

            StartSuper(false);
            MakeSuperDash();
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
            if (!CanUseHellBonusShield || GameCore.IsSuperShot || isSuperShot || removedFromPlay || stunTimer > 0f || isDunking)
            {
                return false;
            }

            StartSuper(false);
            MakeShield();
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
            isSuperShot = true;
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
            isSuperShot = false;
            GameCore.IsSuperShot = false;
            superPhase = SuperPhase.None;
            graphicScaleMultiplier = 1f;
            removedFromPlay = false;
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                readyForSuper = true;
                superChargeTime = superCoolDown;
                energyBar?.SetCharge(1f);
            }
            else if (hellNativeSuperRefundPending)
            {
                superChargeTime = Mathf.Min(cooldown, superChargeTime + cooldown * aiDifficultyTuning.NativeSuperRefundFraction);
                readyForSuper = superChargeTime >= cooldown;
                energyBar?.SetCharge(superChargeTime / cooldown);
            }

            hellNativeSuperRefundPending = false;
        }

        /// <summary>
        /// 执行超级扣篮。
        /// </summary>
        private void MakeMegaDunk()
        {
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            removedFromPlay = true;
            facingDirection = -Side;
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkX, mlpObjectsData.AlleyOopY);
            superTimer = 0f;
            superDuration = Mathf.Max(0.3f, Vector2.Distance(superStartPosition, superTargetPosition) / 700f / 1.3333f);
            superPhase = SuperPhase.MegaTravel;
            PlayState("megadunk");
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PMegaStart);
        }

        /// <summary>
        /// 继续超级扣篮的恢复阶段。
        /// </summary>
        private void ContinueSuperDunk()
        {
            superPhase = SuperPhase.MegaRecover;
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkEndX, superDunkEndY);
            superTimer = 0f;
            superDuration = 0.1f;
            PlayState("megadunk_end");
        }

        /// <summary>
        /// 结束超级扣篮。
        /// </summary>
        private void EndSuperDunk()
        {
            if (!isSuperShot)
            {
                return;
            }

            WithBall = false;
            canTakeInHands = true;
            canThrow = true;
            GameCore.MatchProcessor.Shoot(Side, IsHuman, 8, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, true);
            Position = new Vector2(superDunkEndX, superDunkEndY);
            Velocity = Vector2.zero;
            IsGrounded = true;
            PlayState("idle");
            EndSuper();
        }

        /// <summary>
        /// 释放护盾技能。
        /// </summary>
        private void MakeShield()
        {
            shield?.Activate();
            EndSuper();
        }

        private void MakeScoreUpgradeBuff()
        {
            scoreUpgradeActive = true;
            scoreUpgradePendingShot = false;
            skillFx?.PlayBuff(float.PositiveInfinity);
            EndSuper();
        }

        private void MakeWaxOverdrive()
        {
            moveBuffTimer = skillDefinition.EffectDuration;
            moveBuffScoreBonusAvailable = skillDefinition.FlatScoreBonus > 0;
            skillFx?.PlayBuff(skillDefinition.EffectDuration);
            EndSuper();
        }

        private void MakeFreeze()
        {
            var opponent = GameCore.FindClosestOpponent(this);
            if (opponent != null)
            {
                opponent.ApplyFreeze(skillDefinition.EffectDuration, skillDefinition);
            }

            skillFx?.PlayBurst(0.45f);
            EndSuper();
        }

        private void MakeReboundMagnet()
        {
            reboundMagnetTimer = skillDefinition.EffectDuration > 0f
                ? skillDefinition.EffectDuration
                : ReboundMagnetDefaultDuration;
            canTakeInHands = true;
            UpdateReboundMagnet(0f);
            EndSuper();
        }

        private void MakeGuaranteedBlock()
        {
            var ball = GameCore.Ball;
            if (!CanUseGuaranteedBlockBall(ball))
            {
                EndSuper();
                return;
            }

            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            WithBall = false;
            Velocity = Vector2.zero;
            dashTimer = 0f;
            blockPumpPhase = BlockPumpPhase.None;
            jumpBlockActive = false;
            Position = GetGuaranteedBlockPosition(ball);
            IsGrounded = Position.y >= mlpObjectsData.PlayerIndentY - 0.5f;
            facingDirection = ball.Position.x >= Position.x ? 1f : -1f;
            teleportFx?.StartPlay(Position.x, Position.y - GuaranteedBlockHandsOffsetY);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            PlayState("blockStart");
            ball.ApplyBlock(this);
            GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
            guaranteedBlockPickupLocked = !IsGrounded;
            superPhase = SuperPhase.GuaranteedBlockHold;
            superTimer = 0f;
            superDuration = GuaranteedBlockHoldDuration;
            actionLatch = Mathf.Max(actionLatch, GuaranteedBlockHoldDuration);
        }

        private Vector2 GetGuaranteedBlockPosition(mlpBallObject ball)
        {
            var targetX = Mathf.Clamp(
                ball.Position.x - ball.Side * GuaranteedBlockHorizontalOffset,
                20f,
                mlpConstants.Width - 20f);
            var targetY = Mathf.Clamp(
                ball.Position.y + GuaranteedBlockHandsOffsetY,
                mlpObjectsData.BasketHeight - 18f,
                mlpObjectsData.PlayerIndentY);
            return new Vector2(targetX, targetY);
        }

        private void FinishGuaranteedBlock()
        {
            Velocity = Vector2.zero;
            canDoAction = true;
            canThrow = true;
            if (Position.y >= mlpObjectsData.PlayerIndentY - 0.5f)
            {
                Position.y = mlpObjectsData.PlayerIndentY;
                IsGrounded = true;
            }

            guaranteedBlockPickupLocked = !IsGrounded;
            canTakeInHands = !WithBall && !guaranteedBlockPickupLocked;
            PlayState(IsGrounded ? "blockEnd" : "fly1");
            EndSuper();
        }

        /// <summary>
        /// 执行空接超能。
        /// </summary>
        private void MakeAlleyOop()
        {
            pendingScoreRefundFraction = skillDefinition.ScoreRefundFraction;
            pendingScoreRefundTimer = skillDefinition.ScoreRefundFraction > 0f ? 4f : 0f;
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            Velocity.x = 0f;
            facingDirection = AttackTargetX - Position.x >= 0f ? 1f : -1f;
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
            alleyOopPendingThrow = false;
            GameCore.Ball.AlleyOop(Side, Position.x - 20f * Side, Position.y - 30f, this);
            WithBall = false;
            canTakeInHands = false;
            canThrow = false;
            GameCore.NotifyBallOthers();
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
            Position = new Vector2(superDunkX, mlpObjectsData.AlleyOopY);
            facingDirection = -Side;
            graphicScaleMultiplier = 0f;
            if (armature != null)
            {
                visualState = "pumpEnd";
                armature.StopAtStart("pumpEnd");
            }

            teleportFx?.StartPlay(Position.x, Position.y);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            GameCore.Ball.RemoveFromPhysics();
            superPhase = SuperPhase.AlleyTeleportIn;
            superTimer = 0f;
            superDuration = skillDefinition.SkillType == mlpCharacterSkillType.HexGate ? 0.34f : 0.4f;
        }

        /// <summary>
        /// 执行超能冲刺。
        /// </summary>
        private void MakeSuperDash()
        {
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

            var currentX = Position.x;
            var dashPoint = WithBall
                ? 0
                : Side < 0
                    ? currentX < targetX ? 0 : 1
                    : currentX > targetX ? 0 : 1;

            dashToRight = Side < 0 ? dashPoint == 0 : dashPoint == 1;
            removedFromPlay = true;
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
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
            Position = superTargetPosition;
            Velocity = Vector2.zero;
            IsGrounded = true;
            canDoAction = true;
            canTakeInHands = !WithBall;
            canThrow = true;
            PlayState("md_end");
            EndSuper();
        }

        /// <summary>
        /// 更新超能状态。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        private void UpdateSuper(float dt)
        {
            switch (superPhase)
            {
                case SuperPhase.MegaTravel:
                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDunk();
                    }
                    break;
                case SuperPhase.MegaRecover:
                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    break;
                case SuperPhase.SuperDashTravel:
                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    UpdateSuperDashTravel();
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDash();
                    }
                    break;
                case SuperPhase.AlleyTeleportOut:
                    superTimer += dt;
                    graphicScaleMultiplier = Mathf.Clamp01(1f - superTimer / superDuration);
                    if (superTimer >= superDuration)
                    {
                        FinishAlleyTeleportOut();
                    }
                    break;
                case SuperPhase.AlleyTeleportIn:
                    superTimer += dt;
                    graphicScaleMultiplier = Mathf.Clamp01(superTimer / superDuration);
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDunk();
                    }
                    break;
                case SuperPhase.GuaranteedBlockHold:
                    superTimer += dt;
                    Velocity = Vector2.zero;
                    if (superTimer >= superDuration)
                    {
                        FinishGuaranteedBlock();
                    }
                    break;
            }

            UpdateGraphic();
        }

        /// <summary>
        /// 更新超能冲刺移动过程。
        /// </summary>
        private void UpdateSuperDashTravel()
        {
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
                    superDashHits.Add(opponentPlayer.PlayerNo);
                    if (opponentPlayer.GetBeStolen(currentX, false))
                    {
                        AcquireBallDuringSuperDash();
                    }
                }
            }

            var ball = GameCore.Ball;
            if (ball != null && ball.Position.y > mlpObjectsData.BasketHeight && !WithBall)
            {
                if (ball.IsInGame)
                {
                    if ((dashToRight && currentX > ball.Position.x) || (!dashToRight && currentX < ball.Position.x))
                    {
                        AcquireBallDuringSuperDash();
                    }
                }
                else if (dashTeammatePending && teamMate != null && ((dashToRight && currentX > teamMate.Position.x) || (!dashToRight && currentX < teamMate.Position.x)))
                {
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
            if (WithBall || GameCore.Ball == null)
            {
                return;
            }

            WithBall = true;
            canTakeInHands = false;
            attackJump = false;
            GameCore.Ball.TakeInHands(Side);
            GameCore.NotifyBallInHands(Side, playerNo);
            if (skillDefinition.SkillType == mlpCharacterSkillType.SoulReap && skillDefinition.FlatScoreBonus > 0)
            {
                flatScoreBonusPoints = Mathf.Max(flatScoreBonusPoints, skillDefinition.FlatScoreBonus);
                flatScoreBonusTimer = Mathf.Max(flatScoreBonusTimer, skillDefinition.BonusDuration);
                skillFx?.PlayBuff(Mathf.Min(skillDefinition.BonusDuration, 1.1f));
            }

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
            canDoAction = false;
            actionLatch = Mathf.Max(actionLatch, 0.35f);
            canThrow = false;
            attackJump = false;
            WithBall = false;

            if (TryStartDunk())
            {
                return;
            }

            canTakeInHands = IsGrounded;
            var releaseOffset = IsGrounded ? 20f : 35f;
            if (IsGrounded)
            {
                pointOfThrow = Position.x;
            }

            var releaseX = Position.x - Side * releaseOffset;
            var releaseY = Position.y - 50f;
            var throwType = (pointOfThrow - mlpObjectsData.ThreePointsDistance) * Side >= 0f ? 0 : 6;
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
            pendingGroundThrow = true;
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            actionLatch = Mathf.Max(actionLatch, 0.3f);
            Velocity.x = 0f;
            PlayState("throw_land");
        }

        /// <summary>
        /// 开始抢断。
        /// </summary>
        private void BeginSteal()
        {
            if (stealAnimationActive)
            {
                return;
            }

            canDoAction = false;
            pendingStealAction = true;
            stealAnimationActive = true;
            stealAttemptTimer = mlpObjectsData.StealFrameEventTime;
            stealAnimationTimer = mlpObjectsData.StealAnimationDuration;
            stealFacingDirection = facingDirection;
            facingDirection = stealFacingDirection;
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
            if (!pendingStealAction)
            {
                stealAttemptTimer = -1f;
                return;
            }

            stealAttemptTimer = -1f;
            pendingStealAction = false;
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Steal, Side, playerNo);
            if (GameCore.TryStealBall(this, stealFacingDirection))
            {
                actionLatch = Mathf.Max(actionLatch, 0.18f);
            }
        }

        /// <summary>
        /// 更新抢断动画。
        /// </summary>
        /// <param name="dt">帧间隔时间（秒）</param>
        private void UpdateStealAnimation(float dt)
        {
            facingDirection = stealFacingDirection;
            Velocity.x = 0f;

            if (stealAttemptTimer >= 0f)
            {
                stealAttemptTimer -= dt;
                if (stealAttemptTimer <= 0f)
                {
                    ResolveStealAttempt();
                }
            }

            stealAnimationTimer -= dt;
            if ((armature == null && stealAnimationTimer <= 0f) || stealAnimationTimer <= -0.2f)
            {
                FinishStealAnimation();
                return;
            }

            UpdateGraphic();
        }

        /// <summary>
        /// 结束抢断动画。
        /// </summary>
        private void FinishStealAnimation()
        {
            if (!stealAnimationActive)
            {
                return;
            }

            if (pendingStealAction)
            {
                ResolveStealAttempt();
            }

            stealAnimationActive = false;
            stealAnimationTimer = -1f;
            stealAttemptTimer = -1f;
            canTakeInHands = !WithBall && stunTimer <= 0f && !removedFromPlay;
            canDoAction = controller.ReadyForAction();
            actionLatch = Mathf.Max(actionLatch, 0f);
            PlayState(WithBall ? "idle_wb" : "idle");
        }

        /// <summary>
        /// 取消抢断动画。
        /// </summary>
        /// <param name="restorePickup">是否恢复拾取能力</param>
        private void CancelStealAnimation(bool restorePickup)
        {
            stealAnimationActive = false;
            pendingStealAction = false;
            stealAttemptTimer = -1f;
            stealAnimationTimer = -1f;
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
            if (visualState == state)
            {
                return;
            }

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
            armature?.StopAtStart(state);
        }

        /// <summary>
        /// 动画帧事件回调。
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="eventName">事件名称</param>
        private void OnAnimationFrameEvent(string animationName, string eventName)
        {
            armature?.RefreshPose();
            if (eventName == "throw")
            {
                if (alleyOopPendingThrow && WithBall)
                {
                    StartAlleyOop();
                    return;
                }

                if (pendingGroundThrow && WithBall)
                {
                    pendingGroundThrow = false;
                    MakeThrow();
                    return;
                }
            }

            if (eventName == "action" && pendingStealAction)
            {
                ResolveStealAttempt();
                return;
            }

            if (eventName == "mega" && isSuperShot)
            {
                EndSuperDunk();
                return;
            }

            if (eventName == "dunk" && isDunking)
            {
                ReleaseDunkBall();
            }
        }

        /// <summary>
        /// 动画完成回调。
        /// </summary>
        /// <param name="animationName">动画名称</param>
        private void OnAnimationComplete(string animationName)
        {
            switch (animationName)
            {
                case "blockStart":
                case "pumpStart":
                    blockPumpStartReady = true;
                    break;
                case "blockEnd":
                case "pumpEnd":
                    blockPumpEndReady = true;
                    break;
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
                case "steal":
                    FinishStealAnimation();
                    break;
                case "dunk1":
                case "dunk2":
                case "dunk3":
                    if (isDunking && !dunkReleased)
                    {
                        ReleaseDunkBall();
                    }
                    break;
                case "md_start":
                    PlayState("md_mid");
                    break;
                case "md_end":
                    PlayState(WithBall ? "idle_wb" : "idle");
                    break;
            }
        }

        private void UpdateSkillTimers(float dt)
        {
            if (flatScoreBonusTimer > 0f)
            {
                flatScoreBonusTimer = Mathf.Max(0f, flatScoreBonusTimer - dt);
                if (flatScoreBonusTimer <= 0f)
                {
                    flatScoreBonusPoints = 0;
                }
            }

            if (moveBuffTimer > 0f)
            {
                moveBuffTimer = Mathf.Max(0f, moveBuffTimer - dt);
                if (moveBuffTimer <= 0f)
                {
                    moveBuffScoreBonusAvailable = false;
                }
            }

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
            if (reboundMagnetTimer <= 0f)
            {
                return;
            }

            reboundMagnetTimer = Mathf.Max(0f, reboundMagnetTimer - dt);
            var ball = GameCore.Ball;
            if (WithBall || stunTimer > 0f || removedFromPlay || isDunking || !CanUseReboundMagnetBall(ball))
            {
                reboundMagnetTimer = 0f;
                return;
            }

            var catchPoint = Position + new Vector2(0f, -58f);
            var delta = catchPoint - ball.Position;
            if (Mathf.Abs(delta.x) <= ReboundMagnetCatchDistanceX &&
                Mathf.Abs(delta.y) <= ReboundMagnetCatchDistanceY)
            {
                reboundMagnetTimer = 0f;
                TakeBallInHands();
                canDoAction = false;
                actionLatch = Mathf.Max(actionLatch, 0.18f);
                GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
                return;
            }

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
            var baseSpeed = WithBall ? mlpObjectsData.PlayerMoveWithBall : mlpObjectsData.PlayerMove;
            return baseSpeed * (moveBuffTimer > 0f ? skillDefinition.MoveSpeedMultiplier : 1f);
        }

        private float GetDashSpeed()
        {
            var multiplier = moveBuffTimer > 0f ? Mathf.Lerp(1f, skillDefinition.MoveSpeedMultiplier, 0.65f) : 1f;
            return mlpObjectsData.PlayerMove * 1.7f * multiplier;
        }

        private float GetShotAccuracy()
        {
            if (tutorialPerfectShotPrimed)
            {
                tutorialPerfectShotPrimed = false;
                return -0.5f;
            }

            var resolvedAccuracy = accuracy;
            if (moveBuffTimer > 0f)
            {
                resolvedAccuracy += skillDefinition.AccuracyModifier;
            }

            return Mathf.Max(-0.05f, resolvedAccuracy);
        }

        private void GrantSuperChargeFraction(float fraction)
        {
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                RefreshQuickTestSuperReady();
                return;
            }

            if (fraction <= 0f)
            {
                return;
            }

            superChargeTime = Mathf.Min(cooldown, superChargeTime + cooldown * fraction);
            readyForSuper = superChargeTime >= cooldown;
            energyBar?.SetCharge(superChargeTime / cooldown);
        }

        /// <summary>
        /// 更新图形显示。
        /// </summary>
        private void UpdateGraphic()
        {
            var gameplayScale = mlpPlayersData.GetCharacterGameplayScaleMultiplier(characterId) * graphicScaleMultiplier;
            graphic.transform.position = mlpConstants.PixelToWorldSnapped(Position.x, Position.y, GraphicDepthBase + renderDepthBias);
            graphic.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * facingDirection * gameplayScale,
                mlpConstants.UnitsPerPixel * gameplayScale,
                1f);

            UpdateShadowAppearance();
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
            if (shadowRenderer == null)
            {
                return;
            }

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
            var body = new GameObject("FallbackBody");
            body.transform.SetParent(graphic.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.FallbackAvatar,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "BallClipMsg0000");
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
            dashDirection = direction;
            dashTimer = 0.14f;
            Velocity.x = GetDashSpeed() * direction;
            actionLatch = Mathf.Max(actionLatch, 0.15f);
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
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
            if (controller.CurrentDash != 0)
            {
                bufferedDashDirection = controller.CurrentDash;
                dashBufferTimer = mlpObjectsData.DashInputBuffer;
                return;
            }

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
            blockPumpIsPump = WithBall;
            blockPumpPhase = BlockPumpPhase.Starting;
            blockPumpTimer = blockPumpIsPump ? mlpObjectsData.PumpStartDuration : mlpObjectsData.BlockStartDuration;
            blockPumpStartReady = false;
            blockPumpEndReady = false;
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, blockPumpTimer);
            if (!blockPumpIsPump)
            {
                canTakeInHands = false;
            }
            else
            {
                GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Pump, Side, playerNo);
            }

            PlayState(blockPumpIsPump ? "pumpStart" : "blockStart");
        }

        /// <summary>
        /// 激活跳跃封盖。
        /// </summary>
        private void ActivateJumpBlock()
        {
            jumpBlockActive = true;
            canTakeInHands = false;
        }

        /// <summary>
        /// 判断是否应预备跳跃封盖。
        /// </summary>
        /// <returns>条件满足时返回 true；否则返回 false。</returns>
        private bool ShouldPrimeJumpBlock()
        {
            if (!needBlock)
            {
                return false;
            }

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
            Velocity.x = 0f;
            if (WithBall)
            {
                GameCore.Ball.TakeInHands(Side);
            }

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
            var dunkType = GetDunkType();
            if (dunkType == 0)
            {
                return false;
            }

            isDunking = true;
            canDoAction = false;
            canThrow = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = DunkDuration(dunkType);
            dunkStartPosition = Position;
            dunkTargetPosition = new Vector2(DunkTargetX(), mlpObjectsData.DunkY);
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            actionLatch = Mathf.Max(actionLatch, dunkDuration + 0.15f);
            canTakeInHands = false;
            PlayState("dunk" + dunkType);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Dunk, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        private bool TryTutorialPutbackDunk()
        {
            var ball = GameCore.Ball;
            if (!tutorialPutbackDunkPrimed || WithBall || IsGrounded || ball == null || !ball.IsInGame || removedFromPlay || isSuperShot || isDunking)
            {
                return false;
            }

            if (ball.State == "inHands" || ball.State == "score" || ball.State == "alleyOop" || !IsTutorialPutbackBallInWindow(ball))
            {
                return false;
            }

            var delta = ball.Position - Position;
            if (Mathf.Abs(delta.x) > TutorialPutbackCatchWindowX ||
                Mathf.Abs(delta.y) > TutorialPutbackCatchWindowY ||
                Position.y > mlpObjectsData.DunkZone2Y + TutorialPutbackDunkYBonus)
            {
                return false;
            }

            var dunkType = GetTutorialPutbackDunkType();
            if (dunkType == 0)
            {
                return false;
            }

            tutorialPutbackDunkPrimed = false;
            tutorialDunkCompletionChanceOverride = Mathf.Max(tutorialDunkCompletionChanceOverride, TutorialPutbackCompletionChance);
            ball.RemoveFromPhysics();
            isDunking = true;
            canDoAction = false;
            canThrow = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = DunkDuration(dunkType);
            dunkStartPosition = Position;
            dunkTargetPosition = new Vector2(DunkTargetX(), mlpObjectsData.DunkY);
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            actionLatch = Mathf.Max(actionLatch, dunkDuration + 0.15f);
            canTakeInHands = false;
            PlayState("dunk" + dunkType);
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

        /// <summary>
        /// 获取扣篮类型。
        /// </summary>
        /// <returns>计算得出的扣篮类型。</returns>
        private int GetDunkType()
        {
            var paintStart = Side == 1 ? mlpObjectsData.PaintStartX : mlpConstants.Width - mlpObjectsData.PaintMiddleX;
            var paintMiddle = Side == 1 ? mlpObjectsData.PaintMiddleX : mlpConstants.Width - mlpObjectsData.PaintStartX;
            var tutorialDunkYBonus = tutorialPerfectDunkPrimed ? 36f : 0f;
            if (Position.x >= paintStart && Position.x <= paintMiddle && Position.y <= mlpObjectsData.DunkZone1Y + tutorialDunkYBonus)
            {
                return 1 + Mathf.RoundToInt(2f * Random.value);
            }

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

        /// <summary>
        /// 更新扣篮状态。
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        private void UpdateDunk(float dt)
        {
            dunkTimer += dt;
            var t = dunkDuration > 0f ? Mathf.Clamp01(dunkTimer / dunkDuration) : 1f;
            Position = Vector2.Lerp(dunkStartPosition, dunkTargetPosition, Mathf.SmoothStep(0f, 1f, t));
            IsGrounded = false;
            UpdateGraphic();

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
            if (dunkReleased)
            {
                return;
            }

            dunkReleased = true;
            var completionChance = tutorialPerfectDunkPrimed
                ? 1f
                : tutorialDunkCompletionChanceOverride >= 0f
                    ? Mathf.Max(chanceToCompleteDunk, tutorialDunkCompletionChanceOverride)
                    : chanceToCompleteDunk;
            completionChance = Mathf.Clamp01(completionChance);
            tutorialPerfectDunkPrimed = false;
            tutorialDunkCompletionChanceOverride = -1f;
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
            if (canTakeInHands || stunTimer > 0f || stealAnimationActive || stealAttemptTimer >= 0f || actionLatch > 0f)
            {
                return;
            }

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
        /// </summary>
        /// <param name="p">裁剪平面的方向分量</param>
        /// <param name="q">裁剪平面的距离分量</param>
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
}
