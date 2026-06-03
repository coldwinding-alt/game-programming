// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushGameplayObjects 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public static class rimrushGameplaySpriteLoader
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Executes Load Ball Theme Sprite for the rimrushGameplaySpriteLoader workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="theme">Input value used by this step of the workflow.</param>
        /// <param name="anchorX">Input value used by this step of the workflow.</param>
        /// <param name="anchorY">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Sprite LoadBallThemeSprite(rimrushBallTheme theme, float anchorX, float anchorY)
        {
            var resourcePath = rimrushAssets.Images.BallTheme(theme);
            return string.IsNullOrEmpty(resourcePath)
                ? null
                : LoadGameplaySprite(resourcePath, anchorX, anchorY);
        }

        /// <summary>
        /// Executes Load Gameplay Sprite for the rimrushGameplaySpriteLoader workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="resourcePath">Input value used by this step of the workflow.</param>
        /// <param name="anchorX">Input value used by this step of the workflow.</param>
        /// <param name="anchorY">Input value used by this step of the workflow.</param>
        /// <param name="fallbackAtlas">Input value used by this step of the workflow.</param>
        /// <param name="fallbackFrame">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public static Sprite LoadGameplaySprite(
            string resourcePath,
            float anchorX,
            float anchorY,
            rimrushAtlas fallbackAtlas = null,
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
        /// Executes Load Image Sprite for the rimrushGameplaySpriteLoader workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="resourcePath">Input value used by this step of the workflow.</param>
        /// <param name="anchorX">Input value used by this step of the workflow.</param>
        /// <param name="anchorY">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

            var texture = Resources.Load<Texture2D>(rimrushAssets.Images.ResourcePath(resourcePath));
            if (texture == null)
            {
                return null;
            }

            var rect = new Rect(0f, 0f, texture.width, texture.height);
            // Keep the themed ball aligned to the original atlas ball bounds when it rotates in gameplay.
            var sprite = Sprite.Create(texture, rect, new Vector2(anchorX, 1f - anchorY), 1f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }
    }

    public sealed class rimrushArenaObject
    {
        private const float ArenaLogicalWidth = 1398f;
        private const float ArenaLogicalHeight = 480f;

        public GameObject Graphic { get; }

        /// <summary>
        /// Executes rimrush Arena Object for the rimrushArenaObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushArenaObject(Transform parent)
        {
            Graphic = new GameObject("ArenaObject");
            Graphic.transform.SetParent(parent, false);
            var renderer = Graphic.AddComponent<SpriteRenderer>();
            renderer.sprite = rimrushGameplaySpriteLoader.LoadGameplaySprite(
                rimrushAssets.Images.GameplayImages.ArenaBackdrop,
                0f,
                0f,
                rimrushAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000");
            renderer.sortingOrder = 0;
            rimrushRender.ApplyPixelTransform(Graphic.transform, -299f, 0f);
            ApplyArenaLogicalScale(Graphic.transform, renderer.sprite);
        }

        /// <summary>
        /// Keeps high-resolution standalone arena art at the same logical gameplay size as the legacy atlas frame.
        /// </summary>
        /// <param name="transform">Input value used by this step of the workflow.</param>
        /// <param name="sprite">Input value used by this step of the workflow.</param>
        private static void ApplyArenaLogicalScale(Transform transform, Sprite sprite)
        {
            if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
            {
                return;
            }

            var baseScale = rimrushConstants.UnitsPerPixel;
            transform.localScale = new Vector3(
                baseScale * ArenaLogicalWidth / sprite.rect.width,
                baseScale * ArenaLogicalHeight / sprite.rect.height,
                1f);
        }
    }

    public sealed class rimrushBasketObject
    {
        private readonly List<LineRenderer> netLines = new List<LineRenderer>();
        private readonly int side;
        private GameObject graphic;
        private GameObject frontEar;
        private float netPulse;

        public int Side => side;
        public float Center { get; }
        public float Height => rimrushObjectsData.BasketHeight;

        /// <summary>
        /// Executes rimrush Basket Object for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushBasketObject(int side, Transform parent)
        {
            this.side = side;
            Center = side == -1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2;
            CreateGraphic(parent);
        }

        /// <summary>
        /// Executes Update for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        public void Update(float dt)
        {
            netPulse = Mathf.Max(0f, netPulse - dt * 2f);
            UpdateNetLines();
        }

        /// <summary>
        /// Executes Hit Net for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void HitNet()
        {
            netPulse = 1f;
        }

        /// <summary>
        /// Executes Hide Ear for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void HideEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(false);
            }
        }

        /// <summary>
        /// Executes Show Ear for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ShowEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(true);
            }
        }

        /// <summary>
        /// Executes Create Graphic for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        private void CreateGraphic(Transform parent)
        {
            graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(parent, false);
            rimrushRender.ApplyPixelTransform(graphic.transform, Center, rimrushObjectsData.BasketHeight, 0.05f);
            graphic.transform.localScale = new Vector3(rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), rimrushConstants.UnitsPerPixel, 1f);

            var basket = new GameObject("BasketGraphic");
            basket.transform.SetParent(graphic.transform, false);
            var renderer = basket.AddComponent<SpriteRenderer>();
            renderer.sprite = rimrushGameplaySpriteLoader.LoadGameplaySprite(
                rimrushAssets.Images.GameplayImages.BasketGraphic,
                0.7f,
                0.93f,
                rimrushAtlasCache.Instance.Gameplay,
                "BasketGraphic0000");
            renderer.sortingOrder = 4;

            frontEar = new GameObject(side == -1 ? "FrontEarLeft" : "FrontEarRight");
            frontEar.transform.SetParent(parent, false);
            var frontEarRenderer = frontEar.AddComponent<SpriteRenderer>();
            frontEarRenderer.sprite = rimrushGameplaySpriteLoader.LoadGameplaySprite(
                rimrushAssets.Images.GameplayImages.BasketFrontEar,
                0.5f,
                0.5f,
                rimrushAtlasCache.Instance.Gameplay,
                "FrontEar0000");
            frontEarRenderer.sortingOrder = 60;
            rimrushRender.ApplyPixelTransform(frontEar.transform, Center, rimrushObjectsData.BasketHeight);
            frontEar.transform.localScale = new Vector3(rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), rimrushConstants.UnitsPerPixel, 1f);

            for (var i = 0; i < 10; i++)
            {
                var lineObject = new GameObject($"NetLine{i}");
                lineObject.transform.SetParent(parent, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.018f;
                line.endWidth = 0.018f;
                line.sharedMaterial = rimrushSharedMaterialCache.GetSpritesDefault();
                line.startColor = Color.white;
                line.endColor = Color.white;
                line.sortingOrder = 55;
                netLines.Add(line);
            }

            UpdateNetLines();
        }

        /// <summary>
        /// Executes Update Net Lines for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateNetLines()
        {
            var left = Center - rimrushObjectsData.BasketRadius + 2f;
            var right = Center + rimrushObjectsData.BasketRadius - 2f;
            var middle = Center;
            var top = rimrushObjectsData.BasketHeight + 3f;
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
        /// Executes Set Line for the rimrushBasketObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="index">Input value used by this step of the workflow.</param>
        /// <param name="a">Input value used by this step of the workflow.</param>
        /// <param name="b">Input value used by this step of the workflow.</param>
        private void SetLine(int index, Vector2 a, Vector2 b)
        {
            netLines[index].SetPosition(0, rimrushConstants.PixelToWorld(a.x, a.y, 0.03f));
            netLines[index].SetPosition(1, rimrushConstants.PixelToWorld(b.x, b.y, 0.03f));
        }
    }

    public sealed class rimrushBallObject
    {
        private const float MaxSubstepTravel = 8f;
        private const int MaxSubsteps = 8;
        private const float RimRestitution = 0.78f;
        private const float BackboardRestitution = 0.82f;
        private const float CollisionSoundCooldownDuration = 0.04f;
        private const float GuaranteedDunkScoreExtraX = 6f;

        private readonly GameObject graphic;
        private readonly GameObject shadow;
        private readonly rimrushGameCore gameCore;
        private Vector2 previousPosition;
        private bool visibleNextFrame;
        private bool canScore;
        private bool upperSensorPassed;
        private bool guaranteedDunkScore;
        private int scoreArmedSide;
        private float pickupLockTimer;
        private float collisionSoundCooldown;
        private bool physicsRemoved;
        private rimrushPlayerObject alleyOopPlayer;

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
        /// Executes rimrush Ball Object for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="gameCore">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushBallObject(rimrushGameCore gameCore, Transform parent)
        {
            this.gameCore = gameCore;
            graphic = new GameObject("BallObject");
            graphic.transform.SetParent(parent, false);
            var graphicRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicRenderer.sprite = ResolveBallSprite();
            graphicRenderer.sortingOrder = 50;
            rimrushRender.ApplyPixelTransform(graphic.transform, rimrushConstants.Width2, rimrushObjectsData.BallIndentYCenter, 0.2f);
            shadow = new GameObject("BallShadow");
            shadow.transform.SetParent(parent, false);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = rimrushGameplaySpriteLoader.LoadGameplaySprite(
                rimrushAssets.Images.GameplayImages.PlayerShadowBall,
                0.5f,
                0.5f,
                rimrushAtlasCache.Instance.Gameplay,
                "ShadowMC0002");
            shadowRenderer.sortingOrder = 3;
            rimrushRender.ApplyPixelTransform(shadow.transform, rimrushConstants.Width2, rimrushObjectsData.FloorY, 0.02f);
            shadow.transform.localScale *= 0.7f;
            Restart();
        }

        /// <summary>
        /// Executes Restart for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void Restart()
        {
            Position = new Vector2(rimrushConstants.Width2, rimrushObjectsData.BallIndentYCenter);
            previousPosition = Position;
            Velocity = new Vector2(0f, rimrushObjectsData.BallUpVelocityY);
            State = "up";
            physicsRemoved = false;
            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            ResetScoring(false);
            Show();
            UpdateGraphic();
        }

        /// <summary>
        /// Executes Tutorial Snap To for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="position">Input value used by this step of the workflow.</param>
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

        /// <summary>
        /// Executes Take In Hands for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
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
        /// Executes From Hands for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="playerPosition">Input value used by this step of the workflow.</param>
        /// <param name="direction">Input value used by this step of the workflow.</param>
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
        /// Executes Shoot for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="playerVelocityX">Input value used by this step of the workflow.</param>
        /// <param name="accuracy">Input value used by this step of the workflow.</param>
        public void Shoot(int side, float x, float y, float playerVelocityX, float accuracy)
        {
            Side = side;
            Position = new Vector2(x, y);
            previousPosition = Position;
            LastShotX = x;
            var baseVelocity = CalcThrowVel(x, y, 0f);
            var distanceToBasket = side == 1 ? x : rimrushConstants.Width - x;
            var runningDispersion = Mathf.Abs(playerVelocityX) / rimrushObjectsData.PlayerMoveWithBall * 0.1f;
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
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSwoosh);
        }

        /// <summary>
        /// Executes Dunk for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="completed">Input value used by this step of the workflow.</param>
        public void Dunk(int side, bool completed)
        {
            Side = side;
            var basketX = side == 1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2;
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
                // Completed dunks should resolve as made baskets once the ball is released.
                // Priming the upper sensor avoids false "down-first" misses from coarse substeps.
                upperSensorPassed = true;
                scoreArmedSide = side;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            pickupLockTimer = rimrushObjectsData.DunkPickupLock;
            Show();
        }

        /// <summary>
        /// Executes Apply Steal for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="playerPosition">Input value used by this step of the workflow.</param>
        /// <param name="distanceFactor">Input value used by this step of the workflow.</param>
        /// <param name="direction">Input value used by this step of the workflow.</param>
        public void ApplySteal(Vector2 playerPosition, float distanceFactor, int direction)
        {
            Position = playerPosition;
            previousPosition = Position;
            Velocity = new Vector2(
                direction * (rimrushObjectsData.BallStealVelocityXBase + distanceFactor * rimrushObjectsData.BallStealVelocityXAdd),
                rimrushObjectsData.BallStealVelocityY);
            State = "steal";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(false);
            Show();
            UpdateGraphic();
            gameCore.NotifyBallOthers();
        }

        /// <summary>
        /// Executes Predict Floor Landing X for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public float PredictFloorLandingX()
        {
            if (!IsInGame)
            {
                return Mathf.Clamp(Position.x, 20f, rimrushConstants.Width - 20f);
            }

            if (Position.y >= rimrushObjectsData.BallFloorY)
            {
                return Mathf.Clamp(Position.x, 20f, rimrushConstants.Width - 20f);
            }

            var gravity = rimrushObjectsData.Gravity.y * rimrushObjectsData.BallGravMass;
            if (gravity <= 0.0001f)
            {
                return Mathf.Clamp(Position.x, 20f, rimrushConstants.Width - 20f);
            }

            var floorDelta = rimrushObjectsData.BallFloorY - Position.y;
            var discriminant = Velocity.y * Velocity.y + 2f * gravity * floorDelta;
            if (discriminant <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, rimrushConstants.Width - 20f);
            }

            var timeToFloor = (-Velocity.y + Mathf.Sqrt(discriminant)) / gravity;
            if (timeToFloor <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, rimrushConstants.Width - 20f);
            }

            return Mathf.Clamp(Position.x + Velocity.x * timeToFloor, 20f, rimrushConstants.Width - 20f);
        }

        private static float CalcPickupLockUntilFloor(float startY)
        {
            var floorDelta = Mathf.Max(0f, rimrushObjectsData.BallFloorY - startY);
            var gravity = rimrushObjectsData.Gravity.y * rimrushObjectsData.BallGravMass;
            if (floorDelta <= 0.01f || gravity <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Sqrt(2f * floorDelta / gravity);
        }

        /// <summary>
        /// Executes Apply Block for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="blocker">Input value used by this step of the workflow.</param>
        public void ApplyBlock(rimrushPlayerObject blocker)
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
            // Keep the current shot armed so a clean block that still drops
            // through the original hoop can be scored by the sensor chain.
            gameCore.MatchProcessor.Block(blocker.Side, blocker.IsHuman);
            Show();
            UpdateGraphic();
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BSteel, 0.85f);
            gameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Block, blocker.Side, blocker.PlayerNo);
            gameCore.NotifyBallOthers();
        }

        /// <summary>
        /// Executes Alley Oop for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="player">Input value used by this step of the workflow.</param>
        public void AlleyOop(int side, float x, float y, rimrushPlayerObject player)
        {
            Side = side;
            Position = new Vector2(x, y);
            previousPosition = Position;
            LastShotX = Position.x;
            Velocity = CalcVel(
                x,
                y,
                side == 1 ? rimrushObjectsData.AlleyOopX : rimrushConstants.Width - rimrushObjectsData.AlleyOopX,
                rimrushObjectsData.AlleyOopY,
                150f);
            State = "alleyOop";
            alleyOopPlayer = player;
            physicsRemoved = false;
            ResetScoring(false);
            Show();
            gameCore.IsAlleyOop = true;
        }

        /// <summary>
        /// Executes Remove From Physics for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void RemoveFromPhysics()
        {
            physicsRemoved = true;
            gameCore.IsAlleyOop = false;
            graphic.SetActive(false);
            shadow.SetActive(false);
        }

        /// <summary>
        /// Executes Return To Physics for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ReturnToPhysics()
        {
            physicsRemoved = false;
            gameCore.IsAlleyOop = false;
            Show();
        }

        /// <summary>
        /// Executes On Shield Collision for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
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
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BSteel, 0.85f);
        }

        /// <summary>
        /// Executes Update for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        /// <param name="basketLeft">Input value used by this step of the workflow.</param>
        /// <param name="basketRight">Input value used by this step of the workflow.</param>
        public void Update(float dt, rimrushBasketObject basketLeft, rimrushBasketObject basketRight)
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
                Velocity.y += rimrushObjectsData.Gravity.y * rimrushObjectsData.BallGravMass * stepDt;
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
        /// Executes Resolve Floor Bounce for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void ResolveFloorBounce()
        {
            if (Position.y > rimrushObjectsData.BallFloorY)
            {
                Position.y = rimrushObjectsData.BallFloorY;
                if (Velocity.y > 0f)
                {
                    Velocity.y = rimrushObjectsData.BallBounce;
                    Velocity.x *= 0.86f;
                    State = "bounce";
                    rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BBounce);
                }
            }
        }

        /// <summary>
        /// Executes Resolve Wall Bounce for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void ResolveWallBounce()
        {
            if (Position.x < 5f || Position.x > rimrushConstants.Width - 5f)
            {
                Position.x = Mathf.Clamp(Position.x, 5f, rimrushConstants.Width - 5f);
                Velocity.x *= -0.75f;
            }
        }

        /// <summary>
        /// Executes Resolve Basket for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="basket">Input value used by this step of the workflow.</param>
        /// <param name="scoringSide">Input value used by this step of the workflow.</param>
        private void ResolveBasket(rimrushBasketObject basket, int scoringSide)
        {
            if (basket == null)
            {
                return;
            }

            ResolveBackboardCollision(basket);
            ResolveRimCollision(new Vector2(basket.Center - rimrushObjectsData.BasketRadius, basket.Height), basket);
            ResolveRimCollision(new Vector2(basket.Center + rimrushObjectsData.BasketRadius, basket.Height), basket);
            ProcessScoreSensors(basket, scoringSide);
        }

        /// <summary>
        /// Executes Resolve Backboard Collision for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="basket">Input value used by this step of the workflow.</param>
        private void ResolveBackboardCollision(rimrushBasketObject basket)
        {
            var glassTop = basket.Height + rimrushObjectsData.GlassY;
            var glassBottom = glassTop + rimrushObjectsData.GlassHeight;
            if (Position.y + rimrushObjectsData.BallRadius < glassTop || Position.y - rimrushObjectsData.BallRadius > glassBottom)
            {
                return;
            }

            if (basket.Side == -1)
            {
                var planeX = rimrushObjectsData.GlassWidth;
                if (Velocity.x < 0f && Position.x - rimrushObjectsData.BallRadius <= planeX)
                {
                    Position.x = planeX + rimrushObjectsData.BallRadius;
                    Velocity.x = Mathf.Abs(Velocity.x) * BackboardRestitution;
                    Velocity.y *= 0.97f;
                    SetBasketState();
                    PlayBasketSound(2);
                }

                return;
            }

            var rightPlaneX = rimrushConstants.Width - rimrushObjectsData.GlassWidth;
            if (Velocity.x > 0f && Position.x + rimrushObjectsData.BallRadius >= rightPlaneX)
            {
                Position.x = rightPlaneX - rimrushObjectsData.BallRadius;
                Velocity.x = -Mathf.Abs(Velocity.x) * BackboardRestitution;
                Velocity.y *= 0.97f;
                SetBasketState();
                PlayBasketSound(2);
            }
        }

        /// <summary>
        /// Executes Resolve Rim Collision for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="rimCenter">Input value used by this step of the workflow.</param>
        /// <param name="basket">Input value used by this step of the workflow.</param>
        private void ResolveRimCollision(Vector2 rimCenter, rimrushBasketObject basket)
        {
            var combinedRadius = rimrushObjectsData.BallRadius + rimrushObjectsData.BasketPartRadius;
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
        /// Executes Process Score Sensors for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="basket">Input value used by this step of the workflow.</param>
        /// <param name="scoringSide">Input value used by this step of the workflow.</param>
        private void ProcessScoreSensors(rimrushBasketObject basket, int scoringSide)
        {
            if (!canScore)
            {
                return;
            }

            if (scoreArmedSide != 0 && scoringSide != scoreArmedSide)
            {
                return;
            }

            if (TouchesSensor(previousPosition, Position, basket.Center, basket.Height + rimrushObjectsData.SensorUp))
            {
                upperSensorPassed = true;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            if (!TouchesSensor(previousPosition, Position, basket.Center, basket.Height + rimrushObjectsData.SensorDown))
            {
                return;
            }

            var matchProcessorReady = gameCore.MatchProcessor.ProcessSensor(1);
            if (matchProcessorReady || (guaranteedDunkScore && scoringSide == scoreArmedSide))
            {
                CommitScore(scoringSide);
            }
            else
            {
                CancelScoreAttempt();
            }
        }

        /// <summary>
        /// Executes Try Resolve Guaranteed Dunk Score for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="basketLeft">Input value used by this step of the workflow.</param>
        /// <param name="basketRight">Input value used by this step of the workflow.</param>
        private void TryResolveGuaranteedDunkScore(rimrushBasketObject basketLeft, rimrushBasketObject basketRight)
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

            var minX = armedBasket.Center - rimrushObjectsData.SensorHalf - rimrushObjectsData.BallRadius - GuaranteedDunkScoreExtraX;
            var maxX = armedBasket.Center + rimrushObjectsData.SensorHalf + rimrushObjectsData.BallRadius + GuaranteedDunkScoreExtraX;
            var crossedDown = previousPosition.y <= armedBasket.Height + rimrushObjectsData.SensorDown &&
                              Position.y >= armedBasket.Height + rimrushObjectsData.SensorDown;
            if ((crossedDown || Position.y >= armedBasket.Height + rimrushObjectsData.SensorDown + rimrushObjectsData.SensorHeight) &&
                Position.x >= minX &&
                Position.x <= maxX)
            {
                CommitScore(scoreArmedSide);
            }
        }

        /// <summary>
        /// Executes Commit Score for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="scoringSide">Input value used by this step of the workflow.</param>
        private void CommitScore(int scoringSide)
        {
            CancelScoreAttempt();
            State = "score";
            PlayBasketSound(0);
            gameCore.OnBallScored(scoringSide);
        }

        /// <summary>
        /// Executes Cancel Score Attempt for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void CancelScoreAttempt()
        {
            canScore = false;
            upperSensorPassed = false;
            guaranteedDunkScore = false;
            scoreArmedSide = 0;
        }

        /// <summary>
        /// Executes Touches Sensor for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="start">Input value used by this step of the workflow.</param>
        /// <param name="end">Input value used by this step of the workflow.</param>
        /// <param name="centerX">Input value used by this step of the workflow.</param>
        /// <param name="topY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private static bool TouchesSensor(Vector2 start, Vector2 end, float centerX, float topY)
        {
            var minX = centerX - rimrushObjectsData.SensorHalf - rimrushObjectsData.BallRadius;
            var maxX = centerX + rimrushObjectsData.SensorHalf + rimrushObjectsData.BallRadius;
            var minY = topY - rimrushObjectsData.BallRadius;
            var maxY = topY + rimrushObjectsData.SensorHeight + rimrushObjectsData.BallRadius;
            return SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY);
        }

        /// <summary>
        /// Executes Swept Point Intersects Rect for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="start">Input value used by this step of the workflow.</param>
        /// <param name="end">Input value used by this step of the workflow.</param>
        /// <param name="minX">Input value used by this step of the workflow.</param>
        /// <param name="maxX">Input value used by this step of the workflow.</param>
        /// <param name="minY">Input value used by this step of the workflow.</param>
        /// <param name="maxY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Point Inside Rect for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="point">Input value used by this step of the workflow.</param>
        /// <param name="minX">Input value used by this step of the workflow.</param>
        /// <param name="maxX">Input value used by this step of the workflow.</param>
        /// <param name="minY">Input value used by this step of the workflow.</param>
        /// <param name="maxY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// Executes Clip Segment for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="p">Input value used by this step of the workflow.</param>
        /// <param name="q">Input value used by this step of the workflow.</param>
        /// <param name="tMin">Input value used by this step of the workflow.</param>
        /// <param name="tMax">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Set Basket State for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void SetBasketState()
        {
            if (State != "score")
            {
                State = "basket";
            }
        }

        /// <summary>
        /// Executes Play Basket Sound for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="type">Input value used by this step of the workflow.</param>
        private void PlayBasketSound(int type)
        {
            if (type != 0 && collisionSoundCooldown > 0f)
            {
                return;
            }

            if (type == 0)
            {
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BNet);
                return;
            }

            collisionSoundCooldown = CollisionSoundCooldownDuration;
            if (type == 1)
            {
                var velocityMagnitude = Velocity.magnitude;
                var volume = velocityMagnitude > 300f ? 1f : velocityMagnitude / 300f * 0.8f;
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BRing, Mathf.Clamp(volume, 0.1f, 1f));
            }
            else
            {
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BBasket);
            }
        }

        /// <summary>
        /// Executes Reset Scoring for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="armed">Input value used by this step of the workflow.</param>
        private void ResetScoring(bool armed)
        {
            canScore = armed;
            upperSensorPassed = false;
            guaranteedDunkScore = false;
            scoreArmedSide = 0;
            collisionSoundCooldown = 0f;
        }

        /// <summary>
        /// Executes Calc Throw Vel for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="offset">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private Vector2 CalcThrowVel(float x, float y, float offset)
        {
            float targetX;
            float distance;
            if (Side == 1)
            {
                targetX = rimrushObjectsData.BasketCenter + offset;
                distance = x;
            }
            else
            {
                targetX = rimrushObjectsData.BasketCenter2 + offset;
                distance = rimrushConstants.Width - x;
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

            arc *= 1f + 0.1f * (Random.value <= 0.5f ? -1f : 1f) * Random.value;
            arc = Mathf.Min(arc, 185f);
            return CalcVel(x, y, targetX, rimrushObjectsData.BasketHeight, arc);
        }

        /// <summary>
        /// Executes Calc Vel for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="targetX">Input value used by this step of the workflow.</param>
        /// <param name="targetY">Input value used by this step of the workflow.</param>
        /// <param name="arc">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private Vector2 CalcVel(float x, float y, float targetX, float targetY, float arc)
        {
            var gravity = rimrushObjectsData.Gravity.y * rimrushObjectsData.BallGravMass;
            var offsetY = y - (targetY - arc);
            var vy = -Mathf.Sqrt(Mathf.Max(0.01f, 2f * gravity * offsetY));
            var upTime = -vy / gravity;
            var downTime = Mathf.Sqrt(2f * arc / gravity);
            return new Vector2((targetX - x) / (upTime + downTime) * 1.035f, vy);
        }

        /// <summary>
        /// Executes Calc Dispersion for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="distance">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        /// <param name="running">Input value used by this step of the workflow.</param>
        /// <param name="accuracy">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
                vertical = rimrushObjectsData.VerticalDispersion;
            }
            else
            {
                vertical = (1f - (295f - y) / 60f) * rimrushObjectsData.VerticalDispersion;
            }

            float distanceDispersion =
                distance <= 100f ? 0f :
                distance <= 200f ? 0.01f :
                distance <= 300f ? 0.02f :
                distance <= 400f ? 0.03f :
                distance <= 490f ? 0.04f :
                distance <= 540f ? 0.01f : 0.07f;

            var sign = Random.value < 0.5f ? -1f : 1f;
            var value = sign * (rimrushObjectsData.Dispersion + vertical + distanceDispersion + accuracy + running) * Random.value;
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
        /// Executes Show for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void Show()
        {
            visibleNextFrame = true;
        }

        /// <summary>
        /// Executes Resolve Ball Sprite for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private Sprite ResolveBallSprite()
        {
            var themedSprite = rimrushGameplaySpriteLoader.LoadBallThemeSprite(gameCore.MatchData.BallTheme, 0.5f, 0.5f);
            return themedSprite ?? rimrushAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);
        }

        /// <summary>
        /// Executes Update Graphic for the rimrushBallObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateGraphic()
        {
            if (visibleNextFrame)
            {
                visibleNextFrame = false;
                graphic.SetActive(true);
                shadow.SetActive(true);
            }

            rimrushRender.ApplyPixelTransform(graphic.transform, Position.x, Position.y, 0.2f, 1f, -Position.x * 0.1f);
            var shadowY = rimrushObjectsData.FloorY + 3f;
            rimrushRender.ApplyPixelTransform(shadow.transform, Position.x, shadowY, 0.01f, Mathf.Clamp01(1f - (shadowY - Position.y) / 420f) * 0.7f);
        }
    }

    public sealed class rimrushTeleportFx
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
        private readonly rimrushCharacterSkillDefinition skillDefinition;
        private TeleportPhase phase = TeleportPhase.Hidden;
        private float phaseTime;

        /// <summary>
        /// Executes rimrush Teleport Fx for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushTeleportFx(Transform parent, rimrushCharacterSkillDefinition skillDefinition)
        {
            this.skillDefinition = skillDefinition;
            graphic = new GameObject("TeleportFx");
            graphic.transform.SetParent(parent, false);

            blackNode = new GameObject("TeleportBlack").transform;
            blackNode.SetParent(graphic.transform, false);
            blackRenderer = blackNode.gameObject.AddComponent<SpriteRenderer>();
            blackRenderer.sortingOrder = 74;
            blackRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport10000");

            var centerNode = new GameObject("TeleportCenter");
            centerNode.transform.SetParent(blackNode, false);
            centerRenderer = centerNode.AddComponent<SpriteRenderer>();
            centerRenderer.sortingOrder = 75;
            centerRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport20000");

            var animNode = new GameObject("TeleportAnim");
            animNode.transform.SetParent(graphic.transform, false);
            animRenderer = animNode.AddComponent<SpriteRenderer>();
            animRenderer.sortingOrder = 76;

            var whiteNode = new GameObject("TeleportWhite");
            whiteNode.transform.SetParent(graphic.transform, false);
            whiteRenderer = whiteNode.AddComponent<SpriteRenderer>();
            whiteRenderer.sortingOrder = 77;
            whiteRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            frames = new[]
            {
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30000"),
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30001"),
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30002"),
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30003")
            };

            ApplyTheme();
            Hide();
        }

        /// <summary>
        /// Executes Start Play for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="x">Input value used by this step of the workflow.</param>
        /// <param name="y">Input value used by this step of the workflow.</param>
        public void StartPlay(float x, float y)
        {
            phase = TeleportPhase.BlackExpand;
            phaseTime = 0f;
            graphic.SetActive(true);
            rimrushRender.ApplyPixelTransform(graphic.transform, x, y, 0.16f, 1f);
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
        /// Executes Update for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Hide for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Update Black Expand for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateBlackExpand()
        {
            var t = Mathf.Clamp01(phaseTime / BlackExpandDuration);
            var scale = Mathf.Lerp(0.1f, 0.78f, t);
            blackNode.localScale = new Vector3(scale, scale, 1f);
            blackNode.localRotation = Quaternion.Euler(0f, 0f, -180f * t);
        }

        /// <summary>
        /// Executes Update Black Collapse for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Update White Flash for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Ease In Back for the rimrushTeleportFx workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="t">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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

    public sealed class rimrushShieldObject
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
        private readonly rimrushBasketObject basket;
        private readonly GameObject graphic;
        private readonly SpriteRenderer blurRenderer;
        private readonly SpriteRenderer startRenderer;
        private readonly SpriteRenderer animRenderer;
        private readonly Sprite[] frames;
        private readonly rimrushCharacterSkillDefinition skillDefinition;

        private ShieldPhase phase = ShieldPhase.Hidden;
        private float phaseTime;
        private float alpha = 1f;

        /// <summary>
        /// Executes rimrush Shield Object for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="side">Input value used by this step of the workflow.</param>
        /// <param name="basket">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushShieldObject(int side, rimrushBasketObject basket, Transform parent, rimrushCharacterSkillDefinition skillDefinition)
        {
            this.side = side;
            this.basket = basket;
            this.skillDefinition = skillDefinition;

            graphic = new GameObject(side == -1 ? "ShieldLeft" : "ShieldRight");
            graphic.transform.SetParent(parent, false);

            var shieldStartSprite = rimrushAtlasCache.Instance.SkillFx.Sprite("ShieldMC0000");
            startRenderer = CreateRenderer("ShieldStart", 63, shieldStartSprite);
            blurRenderer = CreateRenderer("ShieldBlur", 64, shieldStartSprite);
            animRenderer = CreateRenderer("ShieldAnim", 65, null);

            frames = new Sprite[21];
            for (var i = 0; i < frames.Length; i++)
            {
                var frameName = $"ShieldMC2{i:0000}";
                var frame = rimrushAtlasCache.Instance.SkillFx.Frame(frameName);
                if (frame != null && frame.W > 0f && frame.H > 0f)
                {
                    frames[i] = rimrushAtlasCache.Instance.SkillFx.Sprite(frameName);
                }
            }

            graphic.SetActive(false);
            ApplyTheme();
        }

        public bool IsBlocking => phase == ShieldPhase.Active && phaseTime < AnimationDuration + ShowTime;
        public bool CanActivate => phase == ShieldPhase.Hidden;

        /// <summary>
        /// Executes Activate for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PShield);
        }

        /// <summary>
        /// Executes Update for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Reset for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Try Block Ball for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ball">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryBlockBall(rimrushBallObject ball)
        {
            if (!IsBlocking || ball == null || ball.State == "score")
            {
                return false;
            }

            var origin = ShieldOrigin;
            var rectLeft = side == -1 ? CollisionRectLeftLeftSide : CollisionRectLeftRightSide;
            var minX = origin.x + rectLeft - rimrushObjectsData.BallRadius;
            var maxX = minX + CollisionRectWidth + rimrushObjectsData.BallRadius * 2f;
            var minY = origin.y + CollisionRectTop - rimrushObjectsData.BallRadius;
            var maxY = minY + CollisionRectHeight + rimrushObjectsData.BallRadius * 2f;
            if (!SweptPointIntersectsRect(ball.PreviousPosition, ball.Position, minX, maxX, minY, maxY))
            {
                return false;
            }

            ball.OnShieldCollision(side);
            return true;
        }

        /// <summary>
        /// Executes Update Graphic for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateGraphic()
        {
            if (!graphic.activeSelf)
            {
                return;
            }

            var origin = ShieldOrigin;
            rimrushRender.ApplyPixelTransform(graphic.transform, origin.x, origin.y, 0.15f, 1f);
            var localScale = graphic.transform.localScale;
            localScale.x = side == 1 ? -Mathf.Abs(localScale.x) : Mathf.Abs(localScale.x);
            graphic.transform.localScale = localScale;
        }

        private Vector2 ShieldOrigin => new Vector2(
            basket.Center + side * GraphicXOffset,
            basket.Height + GraphicYOffset);

        private float AnimationDuration => frames.Length / AnimationFps;

        /// <summary>
        /// Executes Update Intro for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Update Active for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Apply Alpha for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void ApplyAlpha()
        {
            startRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.AccentColor, 0.3f), alpha);
            blurRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, Color.white, 0.18f), alpha * 0.85f);
            animRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.SecondaryColor, 0.22f), alpha);
        }

        /// <summary>
        /// Executes Create Renderer for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="name">Input value used by this step of the workflow.</param>
        /// <param name="sortingOrder">Input value used by this step of the workflow.</param>
        /// <param name="sprite">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Ease Out Back for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="t">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Swept Point Intersects Rect for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="start">Input value used by this step of the workflow.</param>
        /// <param name="end">Input value used by this step of the workflow.</param>
        /// <param name="minX">Input value used by this step of the workflow.</param>
        /// <param name="maxX">Input value used by this step of the workflow.</param>
        /// <param name="minY">Input value used by this step of the workflow.</param>
        /// <param name="maxY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Point Inside Rect for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="point">Input value used by this step of the workflow.</param>
        /// <param name="minX">Input value used by this step of the workflow.</param>
        /// <param name="maxX">Input value used by this step of the workflow.</param>
        /// <param name="minY">Input value used by this step of the workflow.</param>
        /// <param name="maxY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// Executes Clip Segment for the rimrushShieldObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="p">Input value used by this step of the workflow.</param>
        /// <param name="q">Input value used by this step of the workflow.</param>
        /// <param name="tMin">Input value used by this step of the workflow.</param>
        /// <param name="tMax">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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

    public sealed class rimrushPlayerSkillFx
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
        private readonly rimrushCharacterSkillDefinition baseSkillDefinition;
        private rimrushCharacterSkillDefinition skillDefinition;
        private FxMode mode = FxMode.Hidden;
        private float timer;
        private float duration;

        public rimrushPlayerSkillFx(Transform parent, rimrushCharacterSkillDefinition skillDefinition)
        {
            baseSkillDefinition = skillDefinition;
            this.skillDefinition = skillDefinition;
            DBLiteFactory.Instance.EnsureLoaded();

            root = new GameObject("PlayerSkillFx");
            root.transform.SetParent(parent, false);

            glowRenderer = CreateRenderer("Glow", 17, rimrushAtlasCache.Instance.Interface.Sprite("EmblemsBg0000"));
            coreRenderer = CreateRenderer("Core", 18, null);
            accentRenderer = CreateRenderer("Accent", 19, null);

            ApplyTheme(skillDefinition);
            Stop();
        }

        public void ApplyTheme(rimrushCharacterSkillDefinition definition)
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
            mode = FxMode.Buff;
            timer = 0f;
            duration = Mathf.Max(0.01f, effectDuration);
            root.SetActive(true);
        }

        public void PlayBurst(float effectDuration = 0.42f)
        {
            mode = FxMode.Burst;
            timer = 0f;
            duration = Mathf.Max(0.01f, effectDuration);
            root.SetActive(true);
        }

        public void PlayBurst(float effectDuration, rimrushCharacterSkillDefinition definition)
        {
            ApplyTheme(definition);
            PlayBurst(effectDuration);
        }

        public void PlayDash(float effectDuration)
        {
            mode = FxMode.Dash;
            timer = 0f;
            duration = Mathf.Max(0.01f, effectDuration);
            root.SetActive(true);
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

            if (!visible)
            {
                root.SetActive(false);
                return;
            }

            root.SetActive(true);
            rimrushRender.ApplyPixelTransform(root.transform, position.x, position.y + 30f, 0.08f, 1f);
            var rootScale = root.transform.localScale;
            rootScale.x = Mathf.Abs(rootScale.x) * Mathf.Sign(facingDirection);
            root.transform.localScale = rootScale;

            var t = timer / duration;
            ResetRendererLayout();

            if (UsesCustomFxArt(skillDefinition.SkillType))
            {
                UpdateCustomFx(t);
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
            glowRenderer.transform.localPosition = Vector3.zero;
            glowRenderer.transform.localRotation = Quaternion.identity;
            coreRenderer.transform.localPosition = Vector3.zero;
            coreRenderer.transform.localRotation = Quaternion.identity;
            accentRenderer.transform.localPosition = Vector3.zero;
            accentRenderer.transform.localRotation = Quaternion.identity;
        }

        private void UpdateCustomFx(float t)
        {
            switch (skillDefinition.SkillType)
            {
                case rimrushCharacterSkillType.SoulReap:
                    UpdateSoulReapFx(t);
                    break;
                case rimrushCharacterSkillType.BadLuck:
                    UpdateFreezeFx(t);
                    break;
                case rimrushCharacterSkillType.HarvestTime:
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

            SetRendererPixelSize(glowRenderer, 118f * stretch, 34f);
            SetRendererPixelSize(coreRenderer, 150f * stretch, 44f);
            SetRendererPixelSize(accentRenderer, 162f * stretch, 50f);

            glowRenderer.transform.localPosition = new Vector3(8f, -3f, 0f);
            coreRenderer.transform.localPosition = new Vector3(18f, -4f, 0f);
            accentRenderer.transform.localPosition = new Vector3(24f, -5f, 0f);

            glowRenderer.color = WithAlpha(skillDefinition.PrimaryColor, 0.1f + Mathf.Sin(Time.time * 12f) * 0.015f);
            coreRenderer.color = WithAlpha(Color.white, 0.68f * fade);
            accentRenderer.color = WithAlpha(Color.white, 0.5f * fade);
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

        private static bool UsesCustomFxArt(rimrushCharacterSkillType skillType)
        {
            return !string.IsNullOrEmpty(GetCustomImageKey(skillType, false));
        }

        private static Sprite LoadSkillSprite(rimrushCharacterSkillType skillType, bool accent)
        {
            var customImageKey = GetCustomImageKey(skillType, accent);
            if (!string.IsNullOrEmpty(customImageKey))
            {
                var customSprite = rimrushGameplaySpriteLoader.LoadImageSprite(customImageKey, 0.5f, 0.5f);
                if (customSprite != null)
                {
                    return customSprite;
                }
            }

            var legacySpriteName = accent ? GetAccentSpriteName(skillType) : GetCoreSpriteName(skillType);
            return DBLiteFactory.Instance.GetTextureSprite(legacySpriteName);
        }

        private static string GetCustomImageKey(rimrushCharacterSkillType skillType, bool accent)
        {
            switch (skillType)
            {
                case rimrushCharacterSkillType.SoulReap:
                    return accent ? rimrushAssets.Images.SkillFxImages.ReaperDashAccent : rimrushAssets.Images.SkillFxImages.ReaperDashCore;
                case rimrushCharacterSkillType.BadLuck:
                    return accent ? rimrushAssets.Images.SkillFxImages.BadLuckAccent : rimrushAssets.Images.SkillFxImages.BadLuckCore;
                case rimrushCharacterSkillType.HarvestTime:
                    return accent ? rimrushAssets.Images.SkillFxImages.HarvestTimeAccent : rimrushAssets.Images.SkillFxImages.HarvestTimeCore;
                default:
                    return null;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static string GetCoreSpriteName(rimrushCharacterSkillType skillType)
        {
            switch (skillType)
            {
                case rimrushCharacterSkillType.SoulReap:
                    return "fx_smoke_7";
                case rimrushCharacterSkillType.CarnivalJackpot:
                    return "dbanims/circle3";
                case rimrushCharacterSkillType.GhostSail:
                    return "fx_smoke_4";
                case rimrushCharacterSkillType.BloodMoonBlink:
                    return "fx_smoke_0";
                case rimrushCharacterSkillType.WaxOverdrive:
                    return "fx_fire_2";
                case rimrushCharacterSkillType.HarvestTime:
                    return "wind1";
                case rimrushCharacterSkillType.HexGate:
                    return "dbanims/circle2";
                case rimrushCharacterSkillType.BadLuck:
                    return "fx_smoke_6";
                default:
                    return "fx_smoke_1";
            }
        }

        private static string GetAccentSpriteName(rimrushCharacterSkillType skillType)
        {
            switch (skillType)
            {
                case rimrushCharacterSkillType.SoulReap:
                    return "fx_spl_0";
                case rimrushCharacterSkillType.CarnivalJackpot:
                    return "fx_Blur_mol2";
                case rimrushCharacterSkillType.GhostSail:
                    return "fx_Blur_mol1";
                case rimrushCharacterSkillType.BloodMoonBlink:
                    return "fx_spl2_0";
                case rimrushCharacterSkillType.WaxOverdrive:
                    return "fx_Blur_mol4";
                case rimrushCharacterSkillType.HarvestTime:
                    return "fx_smoke_2";
                case rimrushCharacterSkillType.HexGate:
                    return "fx_spl2_0";
                case rimrushCharacterSkillType.BadLuck:
                    return "dbanims/eye34635";
                default:
                    return "fx_Blur_mol0";
            }
        }
    }

    public sealed class rimrushPlayerObject
    {
        private const float GroundCollisionMass = 3f;
        private const float GroundBlockCollisionMass = 6f;
        private const float GroundCollisionSpeedEpsilon = 5f;
        private const float GraphicDepthBase = 0.12f;
        private const float ShadowDepthBase = 0.02f;
        private const float TeamDepthStep = 0.01f;
        private const float PlayerDepthStep = 0.0025f;
        private const float ShadowDepthBiasScale = 0.25f;

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
            AlleyTeleportIn
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
        private readonly rimrushCharacterSkillDefinition skillDefinition;
        private readonly int superId;
        private readonly int brainSlot;
        private readonly rimrushAIDifficultyTuningProfile aiDifficultyTuning;
        private readonly bool hellEnhanced;
        private readonly float accuracy;
        private readonly float chanceToCompleteDunk;
        private readonly float superCoolDown;
        private readonly float superDunkX;
        private readonly float superDunkEndX;
        private readonly float superDunkEndY;
        private readonly float[] superDashTargets = new float[2];
        private readonly UseDelay dashDelay;
        private readonly rimrushEnergyBarView energyBar;
        private readonly rimrushTeleportFx teleportFx;
        private readonly rimrushShieldObject shield;
        private readonly rimrushPlayerSkillFx skillFx;
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
        private rimrushPlayerObject teamMate;
        private bool hellOpeningChargeApplied;
        private bool hellNativeSuperRefundPending;
        private bool scoreUpgradeActive;
        private bool scoreUpgradePendingShot;
        private bool tutorialPerfectShotPrimed;
        private bool tutorialPerfectDunkPrimed;
        private bool tutorialPutbackDunkPrimed;
        private float tutorialAirMotionTimeScale = 1f;
        private bool tutorialJumpBlockAssist;
        private float flatScoreBonusTimer;
        private int flatScoreBonusPoints;
        private float moveBuffTimer;
        private bool moveBuffScoreBonusAvailable;
        private float pendingScoreRefundFraction;
        private float pendingScoreRefundTimer;

        public rimrushGameCore GameCore { get; }
        public Vector2 Position;
        public Vector2 Velocity;
        public int Side { get; }
        public bool WithBall { get; private set; }
        public bool IsHuman { get; }
        public bool IsGrounded { get; private set; } = true;
        public float AttackTargetX => Side == -1 ? rimrushObjectsData.BasketCenter2 : rimrushObjectsData.BasketCenter;
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
        public rimrushCharacterSkillType SkillType => skillDefinition.SkillType;
        public bool UsesPossessionSkill => skillDefinition.UsesPossessionSkill;
        public bool UsesDashSkill => skillDefinition.UsesDashSkill;
        public bool UsesShieldSkill => skillDefinition.UsesBasketShield;
        public bool UsesFreezeSkill => skillDefinition.UsesFreezeSkill;
        public bool ReadyForSuper => readyForSuper;
        public bool CanUseHellBonusSuperDash => hellEnhanced && hellBonusSuperDashCooldownTimer <= 0f;
        public bool CanUseHellBonusShield => hellEnhanced && shield != null && hellBonusShieldCooldownTimer <= 0f && shield.CanActivate;
        public bool IsSuperShot => isSuperShot;
        public bool NeedBlock => needBlock;
        public bool CanThrow => canThrow;
        public IBLPlayerController Controller => controller;
        private bool UsesHighlightedSkillShadow => skillDefinition.SkillType == rimrushCharacterSkillType.CarnivalJackpot && (scoreUpgradeActive || scoreUpgradePendingShot);

        /// <summary>
        /// Executes rimrush Player Object for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="gameCore">Input value used by this step of the workflow.</param>
        /// <param name="teamIndex">Input value used by this step of the workflow.</param>
        /// <param name="characterId">Input value used by this step of the workflow.</param>
        /// <param name="playerNo">Input value used by this step of the workflow.</param>
        /// <param name="playerBrain">Input value used by this step of the workflow.</param>
        /// <param name="skillLevel">Input value used by this step of the workflow.</param>
        /// <param name="parent">Input value used by this step of the workflow.</param>
        public rimrushPlayerObject(rimrushGameCore gameCore, int teamIndex, int characterId, int playerNo, string playerBrain, int skillLevel, Transform parent)
        {
            GameCore = gameCore;
            this.teamIndex = teamIndex;
            this.characterId = characterId;
            this.playerNo = playerNo;
            this.skillLevel = skillLevel;
            Side = teamIndex == 0 ? -1 : 1;
            renderDepthBias = teamIndex * TeamDepthStep + playerNo * PlayerDepthStep;
            IsHuman = !playerBrain.StartsWith("B") && !playerBrain.StartsWith("T");
            brainSlot = rimrushControlsData.ParseControllerSlot(playerBrain);
            skillDefinition = rimrushCharacterSkillsData.Get(characterId);
            superId = skillDefinition.IconSuperId;
            aiDifficultyTuning = rimrushAIDifficultyTuning.Get(rimrushInventory.Instance.Difficulty);
            hellEnhanced = !IsHuman && rimrushInventory.Instance.Difficulty == rimrushAiDifficulty.Hell;

            var profile = rimrushAISkillsData.Get(skillLevel);
            accuracy = profile.Accuracy;
            chanceToCompleteDunk = profile.ChanceToCompleteDunk;
            superCoolDown = profile.CoolDown;
            dashDelay = new UseDelay(rimrushObjectsData.DashDelay * (hellEnhanced ? aiDifficultyTuning.DashCooldownMultiplier : 1f));
            hellBonusSuperDashCooldownDuration = hellEnhanced
                ? skillLevel >= 11 ? aiDifficultyTuning.BonusSuperDashBossCooldown : aiDifficultyTuning.BonusSuperDashCooldown
                : 0f;
            hellBonusShieldCooldownDuration = hellEnhanced
                ? skillLevel >= 11 ? aiDifficultyTuning.BonusShieldBossCooldown : aiDifficultyTuning.BonusShieldCooldown
                : 0f;

            if (Side == 1)
            {
                superDunkX = rimrushObjectsData.AlleyOopX;
                superDashTargets[0] = rimrushObjectsData.SuperDashX1;
                superDashTargets[1] = rimrushObjectsData.SuperDashX2 + 130f;
            }
            else
            {
                superDunkX = rimrushConstants.Width - rimrushObjectsData.AlleyOopX;
                superDashTargets[0] = rimrushObjectsData.SuperDashX2;
                superDashTargets[1] = rimrushObjectsData.SuperDashX1 - 130f;
            }

            superDunkEndX = DunkTargetX() + 20f * Side;
            superDunkEndY = rimrushObjectsData.DunkY + 30f;

            graphic = new GameObject($"Player_{teamIndex}_{playerNo}");
            graphic.transform.SetParent(parent, false);

            shadow = new GameObject($"PlayerShadow_{teamIndex}_{playerNo}");
            shadow.transform.SetParent(parent, false);
            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            defaultShadowSprite = rimrushGameplaySpriteLoader.LoadGameplaySprite(
                playerNo == 0
                    ? rimrushAssets.Images.GameplayImages.PlayerShadowPrimary
                    : rimrushAssets.Images.GameplayImages.PlayerShadowSecondary,
                0.5f,
                0.5f,
                rimrushAtlasCache.Instance.Gameplay,
                playerNo == 0 ? "ShadowMC0000" : "ShadowMC0001");
            shadowRenderer.sprite = defaultShadowSprite;
            activeSkillShadowSprite = skillDefinition.SkillType == rimrushCharacterSkillType.CarnivalJackpot
                ? rimrushGameplaySpriteLoader.LoadGameplaySprite(
                    rimrushAssets.Images.GameplayImages.PlayerShadowPrimaryRed,
                    0.5f,
                    0.5f,
                    rimrushAtlasCache.Instance.Gameplay,
                    "ShadowMC0000") ?? defaultShadowSprite
                : defaultShadowSprite;
            shadowRenderer.sortingOrder = 2;

            armature = rimrushPlayersData.BuildGameplayArmature($"playerSmall_{teamIndex}_{playerNo}");
            if (armature != null)
            {
                armature.transform.SetParent(graphic.transform, false);
                armature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                    graphic.transform,
                    new Vector3(0f, -35f, 0f));
                armature.transform.localScale = new Vector3(
                    rimrushConstants.PixelPerfectCharacterScale,
                    rimrushConstants.PixelPerfectCharacterScale,
                    1f);
                rimrushPlayersData.ApplyCharacter(armature, characterId);
                armature.AnimationComplete += OnAnimationComplete;
                armature.FrameEvent += OnAnimationFrameEvent;
            }
            else
            {
                CreateFallbackAvatar();
            }

            controller = IsHuman
                ? new rimrushKeyboardController(playerBrain)
                : playerBrain.Length > 0 && (playerBrain[0] == 'T' || playerBrain[0] == 't')
                    ? new rimrushTutorialOpponentController(this, skillLevel)
                    : rimrushAIController.CreateForBrain(this, playerBrain, skillLevel);

            energyBar = IsHuman ? new rimrushEnergyBarView(parent, brainSlot, skillDefinition, superCoolDown) : null;
            teleportFx = skillDefinition.UsesTeleportDunk ? new rimrushTeleportFx(parent, skillDefinition) : null;
            shield = skillDefinition.UsesBasketShield || hellEnhanced
                ? new rimrushShieldObject(Side, Side == -1 ? gameCore.BasketLeft : gameCore.BasketRight, parent, skillDefinition)
                : null;
            skillFx = new rimrushPlayerSkillFx(parent, skillDefinition);

            Restart(0);
        }

        /// <summary>
        /// Executes Release Runtime Resources for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ReleaseRuntimeResources()
        {
            energyBar?.ReleaseRuntimeResources();
        }

        /// <summary>
        /// Executes Restart for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="startSide">Input value used by this step of the workflow.</param>
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
            scoreUpgradeActive = false;
            scoreUpgradePendingShot = false;
            tutorialPerfectShotPrimed = false;
            tutorialPerfectDunkPrimed = false;
            tutorialPutbackDunkPrimed = false;
            tutorialAirMotionTimeScale = 1f;
            tutorialJumpBlockAssist = false;
            flatScoreBonusTimer = 0f;
            flatScoreBonusPoints = 0;
            moveBuffTimer = 0f;
            moveBuffScoreBonusAvailable = false;
            pendingScoreRefundFraction = 0f;
            pendingScoreRefundTimer = 0f;
            GameCore.IsSuperShot = false;
            teleportFx?.Hide();
            shield?.Reset();
            skillFx?.Stop();

            var x = rimrushConstants.Width2 + Side * (playerNo == 0 ? rimrushObjectsData.PlayerIndentX : 200f);
            if (startSide == Side)
            {
                x = Side == -1 ? rimrushObjectsData.IndentGeneralX : rimrushConstants.Width - rimrushObjectsData.IndentGeneralX;
            }

            Position = new Vector2(x, rimrushObjectsData.PlayerIndentY);
            pointOfThrow = Position.x;
            IsGrounded = true;
            if (!hellOpeningChargeApplied && hellEnhanced && superCoolDown > 0f)
            {
                superChargeTime = Mathf.Max(superChargeTime, superCoolDown * aiDifficultyTuning.OpeningSuperChargeFraction);
                hellOpeningChargeApplied = true;
            }

            if (superCoolDown <= 0f)
            {
                readyForSuper = true;
                superChargeTime = 0f;
            }
            else
            {
                superChargeTime = Mathf.Clamp(superChargeTime, 0f, superCoolDown);
                readyForSuper = superChargeTime >= superCoolDown;
            }

            PlayState("idle");
            controller.Restart(startSide);
            energyBar?.SetCharge(superCoolDown <= 0f ? 1f : superChargeTime / superCoolDown);
            UpdateGraphic();
        }

        /// <summary>
        /// Executes Update for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        public void Update(float dt)
        {
            teleportFx?.Update(dt);
            shield?.Update(dt);
            UpdateSkillTimers(dt);
            skillFx?.Update(dt, Position, facingDirection, !removedFromPlay && graphicScaleMultiplier > 0.05f);
            hellBonusSuperDashCooldownTimer = Mathf.Max(0f, hellBonusSuperDashCooldownTimer - dt);
            hellBonusShieldCooldownTimer = Mathf.Max(0f, hellBonusShieldCooldownTimer - dt);

            if (!readyForSuper && !isSuperShot && superCoolDown > 0f)
            {
                superChargeTime = Mathf.Min(superCoolDown, superChargeTime + dt);
                energyBar?.SetCharge(superChargeTime / superCoolDown);
                if (superChargeTime >= superCoolDown)
                {
                    readyForSuper = true;
                    if (IsHuman)
                    {
                        rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PEnergy);
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

                Velocity.y = rimrushObjectsData.PlayerJump;
                IsGrounded = false;
                canThrow = WithBall;
                PlayState(WithBall ? "jump_wb" : "jump");
                if (WithBall)
                {
                    GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.JumpA, Side, playerNo);
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
                Velocity.y += rimrushObjectsData.Gravity.y * 3f * dt * tutorialAirMotionTimeScale;
            }

            var verticalDt = IsGrounded ? dt : dt * tutorialAirMotionTimeScale;
            Position += new Vector2(Velocity.x * dt, Velocity.y * verticalDt);
            Position.x = Mathf.Clamp(Position.x, 20f, rimrushConstants.Width - 20f);
            if (Position.y >= rimrushObjectsData.PlayerIndentY)
            {
                Position.y = rimrushObjectsData.PlayerIndentY;
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
        /// Executes Tick Pre Match for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Take Ball In Hands for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Tutorial Snap To for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="position">Input value used by this step of the workflow.</param>
        /// <param name="facing">Input value used by this step of the workflow.</param>
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
        /// Executes Tutorial Charge Super for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void TutorialChargeSuper()
        {
            if (superCoolDown <= 0f)
            {
                readyForSuper = true;
                energyBar?.SetCharge(1f);
                return;
            }

            readyForSuper = true;
            superChargeTime = superCoolDown;
            energyBar?.SetCharge(1f);
        }

        /// <summary>
        /// Executes Tutorial Prime Perfect Shot for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void TutorialPrimePerfectShot()
        {
            tutorialPerfectShotPrimed = true;
        }

        public void TutorialPrimePerfectDunk()
        {
            tutorialPerfectDunkPrimed = true;
        }

        public void TutorialPrimePutbackDunk()
        {
            tutorialPutbackDunkPrimed = true;
            tutorialPerfectDunkPrimed = true;
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
        /// Executes Free Ball for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Notify Ball Loose for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Notify Ball In Hands for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="holderSide">Input value used by this step of the workflow.</param>
        /// <param name="holderPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Notify Ball Shot for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="shotSide">Input value used by this step of the workflow.</param>
        /// <param name="shooterPlayerNo">Input value used by this step of the workflow.</param>
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
        /// Executes Notify Ball Others for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void NotifyBallOthers()
        {
            needBlock = false;
            jumpBlockActive = false;
            controller.BallOthers();
        }

        /// <summary>
        /// Executes Super Shot for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool SuperShot()
        {
            return TryStartSuper(true);
        }

        /// <summary>
        /// Executes Continue Alley Oop for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void ContinueAlleyOop()
        {
            if (!isSuperShot || superPhase != SuperPhase.None)
            {
                return;
            }

            teleportFx?.StartPlay(Position.x, Position.y);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PTeleport);
            removedFromPlay = true;
            superPhase = SuperPhase.AlleyTeleportOut;
            superTimer = 0f;
            superDuration = skillDefinition.SkillType == rimrushCharacterSkillType.HexGate ? 0.34f : 0.4f;
        }

        /// <summary>
        /// Executes Try Shield Ball for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ball">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryShieldBall(rimrushBallObject ball)
        {
            return shield != null && shield.TryBlockBall(ball);
        }

        public void ApplyFreeze(float duration, rimrushCharacterSkillDefinition freezeDefinition)
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
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Stun, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PStunned, 0.9f);
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
        /// Executes Get Steal Distance Bonus for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public float GetStealDistanceBonus()
        {
            return hellEnhanced ? aiDifficultyTuning.StealRangeBonus : 0f;
        }

        /// <summary>
        /// Executes Get Collision Mass for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public float GetCollisionMass()
        {
            return HasGroundBlockBody ? GroundBlockCollisionMass : GroundCollisionMass;
        }

        /// <summary>
        /// Executes Is Moving Toward for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="other">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool IsMovingToward(rimrushPlayerObject other)
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
        /// Executes Is Dashing Into for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="other">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool IsDashingInto(rimrushPlayerObject other)
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
        /// Executes Interrupt Dash By Block for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Apply Horizontal Separation for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="delta">Input value used by this step of the workflow.</param>
        public void ApplyHorizontalSeparation(float delta)
        {
            if (Mathf.Abs(delta) <= 0.001f)
            {
                return;
            }

            Position.x = Mathf.Clamp(Position.x + delta, 20f, rimrushConstants.Width - 20f);
            UpdateGraphic();
        }

        /// <summary>
        /// Executes On Stolen for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        public void OnStolen()
        {
            GetBeStolen(Position.x, false);
        }

        /// <summary>
        /// Executes Check To Be Stolen for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="thiefX">Input value used by this step of the workflow.</param>
        /// <param name="thiefFacingScaleX">Input value used by this step of the workflow.</param>
        /// <param name="stealDistance">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
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
        /// Executes Get Be Stolen for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="thiefX">Input value used by this step of the workflow.</param>
        /// <param name="applyBallSteal">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            var stunDuration = rimrushObjectsData.StunDuration * (hellEnhanced ? aiDifficultyTuning.StunDurationMultiplier : 1f);
            stunTimer = Mathf.Max(stunTimer, stunDuration);
            canDoAction = false;
            jumpBlockActive = false;
            canTakeInHands = false;
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            PlayState("stun");
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Stun, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PStunned, 0.9f);

            if (hadBall && applyBallSteal && GameCore.Ball != null)
            {
                var delta = Position.x - thiefX;
                var direction = delta > 0f ? 1 : -1;
                var distanceFactor = Mathf.Clamp01(Mathf.Abs(delta) / rimrushObjectsData.StealDistance);
                GameCore.Ball.ApplySteal(Position + new Vector2(0f, -45f), distanceFactor, direction);
            }

            return hadBall;
        }

        /// <summary>
        /// Executes Check Loose Ball Pickup for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ball">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        public float CheckLooseBallPickup(rimrushBallObject ball)
        {
            if (ball == null || !ball.CanBeTakenInHands || !CanTakeInHands)
            {
                return -1f;
            }

            var delta = ball.Position - Position;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            if (absX > rimrushObjectsData.BallPickupDistanceX || absY > rimrushObjectsData.BallPickupDistanceY)
            {
                return -1f;
            }

            return delta.sqrMagnitude;
        }

        /// <summary>
        /// Executes Try Block Ball for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="ball">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryBlockBall(rimrushBallObject ball)
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

            var blockWidth = rimrushObjectsData.JumpBlockWidth + (tutorialJumpBlockAssist ? 58f : 0f);
            var blockHeight = rimrushObjectsData.JumpBlockHeight + (tutorialJumpBlockAssist ? 42f : 0f);
            var topBonus = tutorialJumpBlockAssist ? 18f : 0f;
            var bottomBonus = tutorialJumpBlockAssist ? 16f : 0f;
            var minX = Position.x - blockWidth * 0.5f - rimrushObjectsData.BallRadius;
            var maxX = Position.x + blockWidth * 0.5f + rimrushObjectsData.BallRadius;
            var minY = Position.y - blockHeight - rimrushObjectsData.BallRadius - topBonus;
            var maxY = Position.y + rimrushObjectsData.BallRadius + bottomBonus;
            if (!SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY))
            {
                return false;
            }

            ball.ApplyBlock(this);
            return true;
        }

        /// <summary>
        /// Executes Try Start Super for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="pressed">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private bool TryStartSuper(bool pressed)
        {
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

            StartSuper(true);
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Super, Side, playerNo);
            GameCore.ShowHudBonusNotice(skillDefinition.ActivateNotice, 0.95f);
            skillFx?.PlayBurst();

            switch (skillDefinition.SkillType)
            {
                case rimrushCharacterSkillType.SoulReap:
                    MakeSuperDash();
                    return true;
                case rimrushCharacterSkillType.CarnivalJackpot:
                    MakeScoreUpgradeBuff();
                    return true;
                case rimrushCharacterSkillType.GhostSail:
                    MakeShield();
                    return true;
                case rimrushCharacterSkillType.BloodMoonBlink:
                    MakeAlleyOop();
                    return true;
                case rimrushCharacterSkillType.WaxOverdrive:
                    MakeWaxOverdrive();
                    return true;
                case rimrushCharacterSkillType.HarvestTime:
                    MakeScoreUpgradeBuff();
                    return true;
                case rimrushCharacterSkillType.HexGate:
                    MakeAlleyOop();
                    return true;
                case rimrushCharacterSkillType.BadLuck:
                    MakeFreeze();
                    return true;
            }

            EndSuper();

            return false;
        }

        /// <summary>
        /// Executes Try Use Hell Bonus Super Dash for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryUseHellBonusSuperDash()
        {
            if (!CanUseHellBonusSuperDash || GameCore.IsSuperShot || isSuperShot || removedFromPlay || !IsGrounded || stunTimer > 0f || isDunking)
            {
                return false;
            }

            StartSuper(false);
            MakeSuperDash();
            hellBonusSuperDashCooldownTimer = hellBonusSuperDashCooldownDuration;
            GameCore.ShowHudBonusNotice("HELL DASH!", 0.9f);
            return true;
        }

        /// <summary>
        /// Executes Try Use Hell Bonus Shield for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        public bool TryUseHellBonusShield()
        {
            if (!CanUseHellBonusShield || GameCore.IsSuperShot || isSuperShot || removedFromPlay || stunTimer > 0f || isDunking)
            {
                return false;
            }

            StartSuper(false);
            MakeShield();
            hellBonusShieldCooldownTimer = hellBonusShieldCooldownDuration;
            GameCore.ShowHudBonusNotice("HELL SHIELD!", 0.95f);
            return true;
        }

        /// <summary>
        /// Executes Start Super for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="consumeNativeCharge">Input value used by this step of the workflow.</param>
        private void StartSuper(bool consumeNativeCharge)
        {
            isSuperShot = true;
            if (consumeNativeCharge)
            {
                readyForSuper = false;
                superChargeTime = 0f;
                energyBar?.SetCharge(0f);
                hellNativeSuperRefundPending = hellEnhanced && aiDifficultyTuning.NativeSuperRefundFraction > 0f;
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
        /// Executes End Super for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void EndSuper()
        {
            isSuperShot = false;
            GameCore.IsSuperShot = false;
            superPhase = SuperPhase.None;
            graphicScaleMultiplier = 1f;
            removedFromPlay = false;
            if (hellNativeSuperRefundPending && superCoolDown > 0f)
            {
                superChargeTime = Mathf.Min(superCoolDown, superChargeTime + superCoolDown * aiDifficultyTuning.NativeSuperRefundFraction);
                readyForSuper = superChargeTime >= superCoolDown;
                energyBar?.SetCharge(superChargeTime / superCoolDown);
            }

            hellNativeSuperRefundPending = false;
        }

        /// <summary>
        /// Executes Make Mega Dunk for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void MakeMegaDunk()
        {
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            removedFromPlay = true;
            facingDirection = -Side;
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkX, rimrushObjectsData.AlleyOopY);
            superTimer = 0f;
            superDuration = Mathf.Max(0.3f, Vector2.Distance(superStartPosition, superTargetPosition) / 700f / 1.3333f);
            superPhase = SuperPhase.MegaTravel;
            PlayState("megadunk");
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PMegaStart);
        }

        /// <summary>
        /// Executes Continue Super Dunk for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes End Super Dunk for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Make Shield for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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

        /// <summary>
        /// Executes Make Alley Oop for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Start Alley Oop for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Finish Alley Teleport Out for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void FinishAlleyTeleportOut()
        {
            Position = new Vector2(superDunkX, rimrushObjectsData.AlleyOopY);
            facingDirection = -Side;
            graphicScaleMultiplier = 0f;
            if (armature != null)
            {
                visualState = "pumpEnd";
                armature.StopAtStart("pumpEnd");
            }

            teleportFx?.StartPlay(Position.x, Position.y);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PTeleport);
            GameCore.Ball.RemoveFromPhysics();
            superPhase = SuperPhase.AlleyTeleportIn;
            superTimer = 0f;
            superDuration = skillDefinition.SkillType == rimrushCharacterSkillType.HexGate ? 0.34f : 0.4f;
        }

        /// <summary>
        /// Executes Make Super Dash for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            superTargetPosition = new Vector2(superDashTargets[dashPoint], rimrushObjectsData.SuperDashY);
            superTimer = 0f;
            superDuration = Mathf.Max(0.1f, Vector2.Distance(superStartPosition, superTargetPosition) / 600f / 1.3333f);
            superPhase = SuperPhase.SuperDashTravel;
            skillFx?.PlayDash(superDuration);
            PlayState("md_start");
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSuperDash);
        }

        /// <summary>
        /// Executes Continue Super Dash for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Update Super for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
            }

            UpdateGraphic();
        }

        /// <summary>
        /// Executes Update Super Dash Travel for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            if (ball != null && ball.Position.y > rimrushObjectsData.BasketHeight && !WithBall)
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
        /// Executes Acquire Ball During Super Dash for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            if (skillDefinition.SkillType == rimrushCharacterSkillType.SoulReap && skillDefinition.FlatScoreBonus > 0)
            {
                flatScoreBonusPoints = Mathf.Max(flatScoreBonusPoints, skillDefinition.FlatScoreBonus);
                flatScoreBonusTimer = Mathf.Max(flatScoreBonusTimer, skillDefinition.BonusDuration);
                skillFx?.PlayBuff(Mathf.Min(skillDefinition.BonusDuration, 1.1f));
            }

            if (skillDefinition.SkillType == rimrushCharacterSkillType.SoulReap)
            {
                GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
            }
        }

        /// <summary>
        /// Executes Make Throw for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            var throwType = (pointOfThrow - rimrushObjectsData.ThreePointsDistance) * Side >= 0f ? 0 : 6;
            GameCore.MatchProcessor.Shoot(Side, IsHuman, throwType, playerNo);
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Shoot, Side, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Shoot(Side, releaseX, releaseY, Velocity.x, GetShotAccuracy());
            PlayState(IsGrounded ? "throw_land" : "fly1");
        }

        /// <summary>
        /// Executes Begin Floor Throw for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Begin Steal for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            stealAttemptTimer = rimrushObjectsData.StealFrameEventTime;
            stealAnimationTimer = rimrushObjectsData.StealAnimationDuration;
            stealFacingDirection = facingDirection;
            facingDirection = stealFacingDirection;
            actionLatch = Mathf.Max(actionLatch, rimrushObjectsData.StealAnimationDuration);
            canTakeInHands = false;
            Velocity.x = 0f;
            PlayState("steal");
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.StartSteal, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSwoosh, 0.7f);
        }

        /// <summary>
        /// Executes Resolve Steal Attempt for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Steal, Side, playerNo);
            if (GameCore.TryStealBall(this, stealFacingDirection))
            {
                actionLatch = Mathf.Max(actionLatch, 0.18f);
            }
        }

        /// <summary>
        /// Executes Update Steal Animation for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Finish Steal Animation for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Cancel Steal Animation for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="restorePickup">Input value used by this step of the workflow.</param>
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
        /// Executes Update Facing for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
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
        /// Executes Play State for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="state">Input value used by this step of the workflow.</param>
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
        /// Executes Set State At Start for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="state">Input value used by this step of the workflow.</param>
        private void SetStateAtStart(string state)
        {
            visualState = state;
            armature?.StopAtStart(state);
        }

        /// <summary>
        /// Executes On Animation Frame Event for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="animationName">Input value used by this step of the workflow.</param>
        /// <param name="eventName">Input value used by this step of the workflow.</param>
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
        /// Executes On Animation Complete for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="animationName">Input value used by this step of the workflow.</param>
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

        private float GetMoveSpeed()
        {
            var baseSpeed = WithBall ? rimrushObjectsData.PlayerMoveWithBall : rimrushObjectsData.PlayerMove;
            return baseSpeed * (moveBuffTimer > 0f ? skillDefinition.MoveSpeedMultiplier : 1f);
        }

        private float GetDashSpeed()
        {
            var multiplier = moveBuffTimer > 0f ? Mathf.Lerp(1f, skillDefinition.MoveSpeedMultiplier, 0.65f) : 1f;
            return rimrushObjectsData.PlayerMove * 1.7f * multiplier;
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
            if (fraction <= 0f || superCoolDown <= 0f)
            {
                return;
            }

            superChargeTime = Mathf.Min(superCoolDown, superChargeTime + superCoolDown * fraction);
            readyForSuper = superChargeTime >= superCoolDown;
            energyBar?.SetCharge(superChargeTime / superCoolDown);
        }

        /// <summary>
        /// Executes Update Graphic for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateGraphic()
        {
            var gameplayScale = rimrushPlayersData.GetCharacterGameplayScaleMultiplier(characterId) * graphicScaleMultiplier;
            graphic.transform.position = rimrushConstants.PixelToWorldSnapped(Position.x, Position.y, GraphicDepthBase + renderDepthBias);
            graphic.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * facingDirection * gameplayScale,
                rimrushConstants.UnitsPerPixel * gameplayScale,
                1f);

            UpdateShadowAppearance();
            var showShadow = !removedFromPlay && graphicScaleMultiplier > 0.05f;
            shadow.SetActive(showShadow);
            if (showShadow)
            {
                var shadowScale = Mathf.Clamp01(1f - (rimrushObjectsData.PlayerIndentY - Position.y) / 300f);
                rimrushRender.ApplyPixelTransform(
                    shadow.transform,
                    Position.x,
                    rimrushObjectsData.FloorY + 6f,
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
        /// Executes Create Fallback Avatar for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void CreateFallbackAvatar()
        {
            var body = new GameObject("FallbackBody");
            body.transform.SetParent(graphic.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = rimrushGameplaySpriteLoader.LoadGameplaySprite(
                rimrushAssets.Images.GameplayImages.FallbackAvatar,
                0.5f,
                0.5f,
                rimrushAtlasCache.Instance.Gameplay,
                "BallClipMsg0000");
            renderer.color = teamIndex == 0 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            renderer.sortingOrder = 20;
            body.transform.localPosition = new Vector3(0f, -80f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
        }

        /// <summary>
        /// Executes Start Dash for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="direction">Input value used by this step of the workflow.</param>
        private void StartDash(int direction)
        {
            dashDirection = direction;
            dashTimer = 0.14f;
            Velocity.x = GetDashSpeed() * direction;
            actionLatch = Mathf.Max(actionLatch, 0.15f);
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            PlayState("dash");
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Dash, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PDash);
        }

        /// <summary>
        /// Executes Update Dash Buffer for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
        private void UpdateDashBuffer(float dt)
        {
            if (controller.CurrentDash != 0)
            {
                bufferedDashDirection = controller.CurrentDash;
                dashBufferTimer = rimrushObjectsData.DashInputBuffer;
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
        /// Executes Begin Block Or Pump for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void BeginBlockOrPump()
        {
            blockPumpIsPump = WithBall;
            blockPumpPhase = BlockPumpPhase.Starting;
            blockPumpTimer = blockPumpIsPump ? rimrushObjectsData.PumpStartDuration : rimrushObjectsData.BlockStartDuration;
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
                GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Pump, Side, playerNo);
            }

            PlayState(blockPumpIsPump ? "pumpStart" : "blockStart");
        }

        /// <summary>
        /// Executes Activate Jump Block for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void ActivateJumpBlock()
        {
            jumpBlockActive = true;
            canTakeInHands = false;
        }

        /// <summary>
        /// Executes Should Prime Jump Block for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Update Jump Block Threat for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void UpdateJumpBlockThreat()
        {
            if (jumpBlockActive && !ShouldPrimeJumpBlock())
            {
                jumpBlockActive = false;
            }
        }

        /// <summary>
        /// Executes Update Block Or Pump for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
                    blockPumpTimer = blockPumpIsPump ? rimrushObjectsData.PumpEndDuration : rimrushObjectsData.BlockEndDuration;
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
        /// Executes Try Start Dunk for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
            dunkTargetPosition = new Vector2(DunkTargetX(), rimrushObjectsData.DunkY);
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            actionLatch = Mathf.Max(actionLatch, dunkDuration + 0.15f);
            canTakeInHands = false;
            PlayState("dunk" + dunkType);
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Dunk, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        private bool TryTutorialPutbackDunk()
        {
            var ball = GameCore.Ball;
            if (!tutorialPutbackDunkPrimed || WithBall || IsGrounded || ball == null || !ball.IsInGame || removedFromPlay || isSuperShot || isDunking)
            {
                return false;
            }

            if (ball.State == "inHands" || ball.State == "score" || ball.State == "alleyOop")
            {
                return false;
            }

            var delta = ball.Position - Position;
            if (Mathf.Abs(delta.x) > 96f || Mathf.Abs(delta.y) > 120f || Position.y > rimrushObjectsData.DunkZone2Y + 36f)
            {
                return false;
            }

            tutorialPutbackDunkPrimed = false;
            tutorialPerfectDunkPrimed = true;
            isDunking = true;
            canDoAction = false;
            canThrow = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = rimrushObjectsData.Dunk1Duration;
            dunkStartPosition = Position;
            dunkTargetPosition = new Vector2(DunkTargetX(), rimrushObjectsData.DunkY);
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            actionLatch = Mathf.Max(actionLatch, dunkDuration + 0.15f);
            canTakeInHands = false;
            PlayState("dunk1");
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.PutbackDunk, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        /// <summary>
        /// Executes Get Dunk Type for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private int GetDunkType()
        {
            var paintStart = Side == 1 ? rimrushObjectsData.PaintStartX : rimrushConstants.Width - rimrushObjectsData.PaintMiddleX;
            var paintMiddle = Side == 1 ? rimrushObjectsData.PaintMiddleX : rimrushConstants.Width - rimrushObjectsData.PaintStartX;
            var tutorialDunkYBonus = tutorialPerfectDunkPrimed ? 36f : 0f;
            if (Position.x >= paintStart && Position.x <= paintMiddle && Position.y <= rimrushObjectsData.DunkZone1Y + tutorialDunkYBonus)
            {
                return 1 + Mathf.RoundToInt(2f * Random.value);
            }

            if ((Position.x - paintStart) * Side < 0f && Position.y <= rimrushObjectsData.DunkZone2Y + tutorialDunkYBonus)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Executes Dunk Target X for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private float DunkTargetX()
        {
            return Side == 1 ? rimrushObjectsData.DunkX : rimrushConstants.Width - rimrushObjectsData.DunkX;
        }

        /// <summary>
        /// Executes Dunk Duration for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dunkType">Input value used by this step of the workflow.</param>
        /// <returns>Result produced for downstream logic in the current frame.</returns>
        private static float DunkDuration(int dunkType)
        {
            return dunkType == 2
                ? rimrushObjectsData.Dunk2Duration
                : dunkType == 3
                    ? rimrushObjectsData.Dunk3Duration
                    : rimrushObjectsData.Dunk1Duration;
        }

        /// <summary>
        /// Executes Update Dunk for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="dt">Input value used by this step of the workflow.</param>
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
        /// Executes Release Dunk Ball for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void ReleaseDunkBall()
        {
            if (dunkReleased)
            {
                return;
            }

            dunkReleased = true;
            var completionChance = tutorialPerfectDunkPrimed ? 1f : chanceToCompleteDunk;
            tutorialPerfectDunkPrimed = false;
            var completed = Random.value <= completionChance;
            GameCore.MatchProcessor.Shoot(Side, IsHuman, completed ? 1 : 9, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, completed);
            if (!completed)
            {
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BBrick);
            }
        }

        /// <summary>
        /// Executes Restore Ball Pickup If Ready for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        private void RestoreBallPickupIfReady()
        {
            if (canTakeInHands || stunTimer > 0f || stealAnimationActive || stealAttemptTimer >= 0f || actionLatch > 0f)
            {
                return;
            }

            canTakeInHands = true;
        }

        /// <summary>
        /// Executes Is Under Glass for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private bool IsUnderGlass()
        {
            return Side == -1
                ? Position.x > rimrushConstants.Width - 200f && Position.x < rimrushConstants.Width - 100f
                : Position.x > 100f && Position.x < 200f;
        }

        /// <summary>
        /// Executes Swept Point Intersects Rect for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="start">Input value used by this step of the workflow.</param>
        /// <param name="end">Input value used by this step of the workflow.</param>
        /// <param name="minX">Input value used by this step of the workflow.</param>
        /// <param name="maxX">Input value used by this step of the workflow.</param>
        /// <param name="minY">Input value used by this step of the workflow.</param>
        /// <param name="maxY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
        /// Executes Point Inside Rect for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="point">Input value used by this step of the workflow.</param>
        /// <param name="minX">Input value used by this step of the workflow.</param>
        /// <param name="maxX">Input value used by this step of the workflow.</param>
        /// <param name="minY">Input value used by this step of the workflow.</param>
        /// <param name="maxY">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// Executes Clip Segment for the rimrushPlayerObject workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
        /// <param name="p">Input value used by this step of the workflow.</param>
        /// <param name="q">Input value used by this step of the workflow.</param>
        /// <param name="tMin">Input value used by this step of the workflow.</param>
        /// <param name="tMax">Input value used by this step of the workflow.</param>
        /// <returns>True when the requested operation succeeds; otherwise false.</returns>
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
