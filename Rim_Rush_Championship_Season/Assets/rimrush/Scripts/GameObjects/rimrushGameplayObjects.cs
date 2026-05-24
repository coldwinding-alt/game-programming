using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public static class rimrushGameplaySpriteLoader
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Sprite LoadBallThemeSprite(rimrushBallTheme theme, float anchorX, float anchorY)
        {
            return LoadImageSprite(rimrushAssets.Images.BallTheme(theme), anchorX, anchorY);
        }

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
        public GameObject Graphic { get; }

        public rimrushArenaObject(Transform parent)
            : this(parent, null)
        {
        }

        public rimrushArenaObject(Transform parent, rimrushArenaView view)
        {
            if (view == null || view.GraphicRenderer == null)
            {
                view = rimrushArenaView.CreateRuntimeFallback(parent);
            }

            view.GraphicRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("0bg_gameplay0000", 0f, 0f);
            view.GraphicRenderer.sortingOrder = 0;
            rimrushRender.ApplyPixelTransform(view.GraphicRenderer.transform, -299f, 0f, view.GraphicRenderer.transform.localPosition.z);
            Graphic = view.GraphicRenderer.gameObject;
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

        public rimrushBasketObject(int side, Transform parent)
            : this(side, parent, null)
        {
        }

        public rimrushBasketObject(int side, Transform parent, rimrushBasketView view)
        {
            this.side = side;
            Center = side == -1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2;
            CreateGraphic(parent, view);
        }

        public void Update(float dt)
        {
            netPulse = Mathf.Max(0f, netPulse - dt * 2f);
            UpdateNetLines();
        }

        public void HitNet()
        {
            netPulse = 1f;
        }

        public void HideEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(false);
            }
        }

        public void ShowEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(true);
            }
        }

        private void CreateGraphic(Transform parent, rimrushBasketView view)
        {
            if (view == null || view.BasketRenderer == null || view.FrontEarRenderer == null || view.NetLines == null || view.NetLines.Count < 10)
            {
                view = rimrushBasketView.CreateRuntimeFallback(side, parent);
            }

            graphic = view.Root.gameObject;
            rimrushRender.ApplyPixelTransform(graphic.transform, Center, rimrushObjectsData.BasketHeight, 0.05f);
            graphic.transform.localScale = new Vector3(rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), rimrushConstants.UnitsPerPixel, 1f);

            view.BasketRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BasketGraphic0000", 0.7f, 0.93f);
            view.BasketRenderer.sortingOrder = 4;

            frontEar = view.FrontEarRenderer.gameObject;
            view.FrontEarRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("FrontEar0000", 0.5f, 0.5f);
            view.FrontEarRenderer.sortingOrder = 60;
            rimrushRender.ApplyPixelTransform(frontEar.transform, Center, rimrushObjectsData.BasketHeight, frontEar.transform.localPosition.z);
            frontEar.transform.localScale = new Vector3(rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), rimrushConstants.UnitsPerPixel, 1f);

            netLines.Clear();
            for (var i = 0; i < view.NetLines.Count; i++)
            {
                var line = view.NetLines[i];
                if (line == null)
                {
                    continue;
                }

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

        public rimrushBallObject(rimrushGameCore gameCore, Transform parent)
            : this(gameCore, parent, null)
        {
        }

        public rimrushBallObject(rimrushGameCore gameCore, Transform parent, rimrushBallView view)
        {
            this.gameCore = gameCore;
            if (view == null || view.GraphicRenderer == null || view.ShadowRenderer == null)
            {
                view = rimrushBallView.CreateRuntimeFallback(parent);
            }

            graphic = view.Root.gameObject;
            view.GraphicRenderer.sprite = ResolveBallSprite();
            view.GraphicRenderer.sortingOrder = 50;
            rimrushRender.ApplyPixelTransform(graphic.transform, rimrushConstants.Width2, rimrushObjectsData.BallIndentYCenter, 0.2f);
            shadow = view.ShadowRenderer.gameObject;
            view.ShadowRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("ShadowMC0002", 0.5f, 0.5f);
            view.ShadowRenderer.sortingOrder = 3;
            shadow.transform.localScale = Vector3.one * 0.7f;
            Restart();
        }

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
            gameCore.NotifyBallOthers();
        }

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

        public void RemoveFromPhysics()
        {
            physicsRemoved = true;
            gameCore.IsAlleyOop = false;
            graphic.SetActive(false);
            shadow.SetActive(false);
        }

        public void ReturnToPhysics()
        {
            physicsRemoved = false;
            gameCore.IsAlleyOop = false;
            Show();
        }

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

        private void ResolveWallBounce()
        {
            if (Position.x < 5f || Position.x > rimrushConstants.Width - 5f)
            {
                Position.x = Mathf.Clamp(Position.x, 5f, rimrushConstants.Width - 5f);
                Velocity.x *= -0.75f;
            }
        }

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

        private void CommitScore(int scoringSide)
        {
            CancelScoreAttempt();
            State = "score";
            PlayBasketSound(0);
            gameCore.OnBallScored(scoringSide);
        }

        private void CancelScoreAttempt()
        {
            canScore = false;
            upperSensorPassed = false;
            guaranteedDunkScore = false;
            scoreArmedSide = 0;
        }

        private static bool TouchesSensor(Vector2 start, Vector2 end, float centerX, float topY)
        {
            var minX = centerX - rimrushObjectsData.SensorHalf - rimrushObjectsData.BallRadius;
            var maxX = centerX + rimrushObjectsData.SensorHalf + rimrushObjectsData.BallRadius;
            var minY = topY - rimrushObjectsData.BallRadius;
            var maxY = topY + rimrushObjectsData.SensorHeight + rimrushObjectsData.BallRadius;
            return SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY);
        }

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

        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

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

        private void SetBasketState()
        {
            if (State != "score")
            {
                State = "basket";
            }
        }

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

        private void ResetScoring(bool armed)
        {
            canScore = armed;
            upperSensorPassed = false;
            guaranteedDunkScore = false;
            scoreArmedSide = 0;
            collisionSoundCooldown = 0f;
        }

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

        private Vector2 CalcVel(float x, float y, float targetX, float targetY, float arc)
        {
            var gravity = rimrushObjectsData.Gravity.y * rimrushObjectsData.BallGravMass;
            var offsetY = y - (targetY - arc);
            var vy = -Mathf.Sqrt(Mathf.Max(0.01f, 2f * gravity * offsetY));
            var upTime = -vy / gravity;
            var downTime = Mathf.Sqrt(2f * arc / gravity);
            return new Vector2((targetX - x) / (upTime + downTime) * 1.035f, vy);
        }

        private float CalcDispersion(float distance, float y, float running, float accuracy)
        {
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

        private void Show()
        {
            visibleNextFrame = true;
        }

        private Sprite ResolveBallSprite()
        {
            var themedSprite = rimrushGameplaySpriteLoader.LoadBallThemeSprite(gameCore.MatchData.BallTheme, 0.5f, 0.5f);
            return themedSprite ?? rimrushAtlasCache.Instance.Gameplay.Sprite("BallMC0000", 0.5f, 0.5f);
        }

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
        private TeleportPhase phase = TeleportPhase.Hidden;
        private float phaseTime;

        public rimrushTeleportFx(Transform parent)
            : this(parent, null)
        {
        }

        public rimrushTeleportFx(Transform parent, rimrushTeleportFxView view)
        {
            if (view == null || view.Root == null || view.BlackNode == null || view.BlackRenderer == null || view.CenterRenderer == null || view.WhiteRenderer == null || view.AnimRenderer == null)
            {
                view = rimrushTeleportFxView.CreateRuntimeFallback(parent);
            }

            graphic = view.Root;
            blackNode = view.BlackNode;
            blackRenderer = view.BlackRenderer;
            centerRenderer = view.CenterRenderer;
            whiteRenderer = view.WhiteRenderer;
            animRenderer = view.AnimRenderer;
            blackRenderer.sortingOrder = 74;
            blackRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport10000");
            centerRenderer.sortingOrder = 75;
            centerRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport20000");
            animRenderer.sortingOrder = 76;
            whiteRenderer.sortingOrder = 77;
            whiteRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            frames = new[]
            {
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30000"),
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30001"),
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30002"),
                rimrushAtlasCache.Instance.SkillFx.Sprite("teleport30003")
            };

            Hide();
        }

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
            blackNode.localScale = new Vector3(0.12f, 0.12f, 1f);
            blackNode.localRotation = Quaternion.identity;
            whiteRenderer.transform.localScale = new Vector3(0.086f, 0.027f, 1f);
            whiteRenderer.transform.localRotation = Quaternion.identity;
        }

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

        private void UpdateBlackExpand()
        {
            var t = Mathf.Clamp01(phaseTime / BlackExpandDuration);
            var scale = Mathf.Lerp(0.12f, 1f, t);
            blackNode.localScale = new Vector3(scale, scale, 1f);
            blackNode.localRotation = Quaternion.Euler(0f, 0f, -180f * t);
        }

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

        private void UpdateWhiteFlash()
        {
            var time1 = WhiteFlashDuration1;
            var time2 = WhiteFlashDuration2;
            var total12 = time1 + time2;
            Vector2 scale;
            if (phaseTime <= time1)
            {
                scale = Vector2.Lerp(new Vector2(0.086f, 0.027f), new Vector2(0.658f, 0.596f), phaseTime / time1);
            }
            else if (phaseTime <= total12)
            {
                scale = Vector2.Lerp(new Vector2(0.658f, 0.596f), new Vector2(0.084f, 1.258f), (phaseTime - time1) / time2);
            }
            else
            {
                scale = Vector2.Lerp(new Vector2(0.084f, 1.258f), new Vector2(0.028f, 0.072f), (phaseTime - total12) / WhiteFlashDuration3);
            }

            whiteRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        private static float EaseInBack(float t)
        {
            const float overshoot = 1.70158f;
            return (overshoot + 1f) * t * t * t - overshoot * t * t;
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

        private ShieldPhase phase = ShieldPhase.Hidden;
        private float phaseTime;
        private float alpha = 1f;

        public rimrushShieldObject(int side, rimrushBasketObject basket, Transform parent)
            : this(side, basket, parent, null)
        {
        }

        public rimrushShieldObject(int side, rimrushBasketObject basket, Transform parent, rimrushShieldView view)
        {
            this.side = side;
            this.basket = basket;

            if (view == null || view.Root == null || view.BlurRenderer == null || view.StartRenderer == null || view.AnimRenderer == null)
            {
                view = rimrushShieldView.CreateRuntimeFallback(side, parent);
            }

            graphic = view.Root;

            var shieldStartSprite = rimrushAtlasCache.Instance.SkillFx.Sprite("ShieldMC0000");
            startRenderer = view.StartRenderer;
            blurRenderer = view.BlurRenderer;
            animRenderer = view.AnimRenderer;
            startRenderer.sortingOrder = 63;
            startRenderer.sprite = shieldStartSprite;
            blurRenderer.sortingOrder = 64;
            blurRenderer.sprite = shieldStartSprite;
            animRenderer.sortingOrder = 65;
            animRenderer.sprite = null;

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
        }

        public bool IsBlocking => phase == ShieldPhase.Active && phaseTime < AnimationDuration + ShowTime;
        public bool CanActivate => phase == ShieldPhase.Hidden;

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

        private void ApplyAlpha()
        {
            startRenderer.color = new Color(1f, 1f, 1f, alpha);
            blurRenderer.color = new Color(1f, 1f, 1f, alpha * 0.85f);
            animRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            var x = t - 1f;
            return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
        }

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

        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

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

    public sealed class rimrushPlayerObject
    {
        private const float GroundCollisionMass = 3f;
        private const float GroundBlockCollisionMass = 6f;
        private const float GroundCollisionSpeedEpsilon = 5f;

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
        private readonly DBLiteArmature armature;
        private readonly rimrushGameplayBindings gameplayBindings;
        private readonly rimrushPlayerView playerView;
        private readonly Transform armatureMount;
        private readonly SpriteRenderer fallbackRenderer;
        private readonly IBLPlayerController controller;
        private readonly int teamIndex;
        private readonly int playerNo;
        private readonly int skillLevel;
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
        public bool ReadyForSuper => readyForSuper;
        public bool CanUseHellBonusSuperDash => hellEnhanced && hellBonusSuperDashCooldownTimer <= 0f;
        public bool CanUseHellBonusShield => hellEnhanced && shield != null && hellBonusShieldCooldownTimer <= 0f && shield.CanActivate;
        public bool IsSuperShot => isSuperShot;
        public bool NeedBlock => needBlock;
        public bool CanThrow => canThrow;

        public rimrushPlayerObject(rimrushGameCore gameCore, int teamIndex, int characterId, int playerNo, string playerBrain, int skillLevel, Transform parent)
            : this(gameCore, teamIndex, characterId, playerNo, playerBrain, skillLevel, parent, null, null, null, null, null)
        {
        }

        public rimrushPlayerObject(
            rimrushGameCore gameCore,
            int teamIndex,
            int characterId,
            int playerNo,
            string playerBrain,
            int skillLevel,
            Transform parent,
            rimrushGameplayBindings gameplayBindings,
            rimrushPlayerView playerView,
            rimrushEnergyBarSceneView energyBarView,
            rimrushTeleportFxView teleportFxView,
            rimrushShieldView shieldView)
        {
            GameCore = gameCore;
            this.teamIndex = teamIndex;
            this.playerNo = playerNo;
            this.skillLevel = skillLevel;
            this.gameplayBindings = gameplayBindings;
            Side = teamIndex == 0 ? -1 : 1;
            IsHuman = !playerBrain.StartsWith("B");
            brainSlot = rimrushControlsData.ParseControllerSlot(playerBrain);
            superId = rimrushPlayersData.GetCharacterSuperId(characterId);
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

            this.playerView = playerView;
            if (this.playerView == null || this.playerView.Root == null || this.playerView.ShadowRenderer == null)
            {
                this.playerView = rimrushPlayerView.CreateRuntimeFallback($"Player_{teamIndex}_{playerNo}", playerNo, parent);
            }

            this.playerView.ClearRuntimeVisuals();
            graphic = this.playerView.Root.gameObject;
            shadow = this.playerView.ShadowRenderer.gameObject;
            this.playerView.ShadowRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite(playerNo == 0 ? "ShadowMC0000" : "ShadowMC0001", 0.5f, 0.5f);
            this.playerView.ShadowRenderer.sortingOrder = 2;
            armatureMount = this.playerView.ArmatureMount;
            fallbackRenderer = this.playerView.FallbackRenderer;

            armature = rimrushPlayersData.BuildGameplayArmature($"playerSmall_{teamIndex}_{playerNo}");
            if (armature != null)
            {
                armature.transform.SetParent(armatureMount != null ? armatureMount : graphic.transform, false);
                armature.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                    armatureMount != null ? armatureMount : graphic.transform,
                    Vector3.zero);
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
                : rimrushAIController.CreateForBrain(this, playerBrain, skillLevel);

            energyBar = IsHuman
                ? (energyBarView != null
                    ? new rimrushEnergyBarView(energyBarView, brainSlot, superId, superCoolDown)
                    : new rimrushEnergyBarView(parent, brainSlot, superId, superCoolDown))
                : null;
            teleportFx = superId == 0 || superId == 2
                ? new rimrushTeleportFx(parent, teleportFxView)
                : null;
            shield = superId == 1 || hellEnhanced
                ? new rimrushShieldObject(Side, Side == -1 ? gameCore.BasketLeft : gameCore.BasketRight, parent, shieldView)
                : null;

            Restart(0);
        }

        public void ReleaseRuntimeResources()
        {
            energyBar?.ReleaseRuntimeResources();
            playerView?.ClearRuntimeVisuals();
        }

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
            GameCore.IsSuperShot = false;
            teleportFx?.Hide();
            shield?.Reset();

            var x = rimrushConstants.Width2 + Side * (playerNo == 0 ? rimrushObjectsData.PlayerIndentX : 200f);
            var y = rimrushObjectsData.PlayerIndentY;
            if (gameplayBindings != null)
            {
                var spawn = gameplayBindings.GetSpawnPosition(Side, startSide == Side);
                x = spawn.x;
                y = spawn.y;
            }
            else if (startSide == Side)
            {
                x = Side == -1 ? rimrushObjectsData.IndentGeneralX : rimrushConstants.Width - rimrushObjectsData.IndentGeneralX;
            }

            Position = new Vector2(x, y);
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

        public void Update(float dt)
        {
            teleportFx?.Update(dt);
            shield?.Update(dt);
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
                Velocity.x = rimrushObjectsData.PlayerMove * 1.7f * dashDirection;
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
                var moveSpeed = WithBall ? rimrushObjectsData.PlayerMoveWithBall : rimrushObjectsData.PlayerMove;
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
                Velocity.y += rimrushObjectsData.Gravity.y * 3f * dt;
            }

            Position += Velocity * dt;
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

        public void NotifyBallInHands(int holderSide, int holderPlayerNo)
        {
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

        public void NotifyBallShot(int shotSide, int shooterPlayerNo)
        {
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

        public void NotifyBallOthers()
        {
            needBlock = false;
            jumpBlockActive = false;
            controller.BallOthers();
        }

        public bool SuperShot()
        {
            return TryStartSuper(true);
        }

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
            superDuration = 0.4f;
        }

        public bool TryShieldBall(rimrushBallObject ball)
        {
            return shield != null && shield.TryBlockBall(ball);
        }

        public float GetStealDistanceBonus()
        {
            return hellEnhanced ? aiDifficultyTuning.StealRangeBonus : 0f;
        }

        public float GetCollisionMass()
        {
            return HasGroundBlockBody ? GroundBlockCollisionMass : GroundCollisionMass;
        }

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

        public void ApplyHorizontalSeparation(float delta)
        {
            if (Mathf.Abs(delta) <= 0.001f)
            {
                return;
            }

            Position.x = Mathf.Clamp(Position.x + delta, 20f, rimrushConstants.Width - 20f);
            UpdateGraphic();
        }

        public void OnStolen()
        {
            GetBeStolen(Position.x, false);
        }

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

            var minX = Position.x - rimrushObjectsData.JumpBlockWidth * 0.5f - rimrushObjectsData.BallRadius;
            var maxX = Position.x + rimrushObjectsData.JumpBlockWidth * 0.5f + rimrushObjectsData.BallRadius;
            var minY = Position.y - rimrushObjectsData.JumpBlockHeight - rimrushObjectsData.BallRadius;
            var maxY = Position.y + rimrushObjectsData.BallRadius;
            if (!SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY))
            {
                return false;
            }

            ball.ApplyBlock(this);
            return true;
        }

        private bool TryStartSuper(bool pressed)
        {
            if (!pressed || !readyForSuper || GameCore.IsSuperShot)
            {
                return false;
            }

            if (superId == 0)
            {
                if (!WithBall)
                {
                    return false;
                }

                StartSuper(true);
                MakeMegaDunk();
                return true;
            }

            if (superId == 1)
            {
                StartSuper(true);
                MakeShield();
                return true;
            }

            if (superId == 2)
            {
                if (!WithBall)
                {
                    return false;
                }

                StartSuper(true);
                MakeAlleyOop();
                return true;
            }

            if (superId == 3)
            {
                StartSuper(true);
                MakeSuperDash();
                return true;
            }

            return false;
        }

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

        private void ContinueSuperDunk()
        {
            superPhase = SuperPhase.MegaRecover;
            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkEndX, superDunkEndY);
            superTimer = 0f;
            superDuration = 0.1f;
            PlayState("megadunk_end");
        }

        private void EndSuperDunk()
        {
            if (!isSuperShot)
            {
                return;
            }

            WithBall = false;
            canTakeInHands = true;
            canThrow = true;
            GameCore.MatchProcessor.Shoot(Side, IsHuman, superId == 0 ? 7 : 8);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, true);
            Position = new Vector2(superDunkEndX, superDunkEndY);
            Velocity = Vector2.zero;
            IsGrounded = true;
            PlayState("idle");
            EndSuper();
        }

        private void MakeShield()
        {
            shield?.Activate();
            EndSuper();
        }

        private void MakeAlleyOop()
        {
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
            superDuration = 0.4f;
        }

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
            PlayState("md_start");
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSuperDash);
        }

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
        }

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
            GameCore.MatchProcessor.Shoot(Side, IsHuman, throwType);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Shoot(Side, releaseX, releaseY, Velocity.x, accuracy);
            PlayState(IsGrounded ? "throw_land" : "fly1");
        }

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

        private void PlayState(string state)
        {
            if (visualState == state)
            {
                return;
            }

            visualState = state;
            armature?.Play(state);
        }

        private void SetStateAtStart(string state)
        {
            visualState = state;
            armature?.StopAtStart(state);
        }

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

        private void UpdateGraphic()
        {
            graphic.transform.position = rimrushConstants.PixelToWorldSnapped(Position.x, Position.y, 0.12f + playerNo * 0.01f);
            graphic.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * facingDirection * graphicScaleMultiplier,
                rimrushConstants.UnitsPerPixel * graphicScaleMultiplier,
                1f);

            var showShadow = !removedFromPlay && graphicScaleMultiplier > 0.05f;
            shadow.SetActive(showShadow);
            if (showShadow)
            {
                var shadowScale = Mathf.Clamp01(1f - (rimrushObjectsData.PlayerIndentY - Position.y) / 300f);
                rimrushRender.ApplyPixelTransform(shadow.transform, Position.x, rimrushObjectsData.FloorY + 6f, 0.02f, Mathf.Max(0.2f, shadowScale));
            }
        }

        private void CreateFallbackAvatar()
        {
            if (fallbackRenderer != null)
            {
                fallbackRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BallClipMsg0000", 0.5f, 0.5f);
                fallbackRenderer.color = teamIndex == 0 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
                fallbackRenderer.sortingOrder = 20;
                fallbackRenderer.enabled = true;
                fallbackRenderer.transform.localPosition = new Vector3(0f, -80f, 0f);
                fallbackRenderer.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
                return;
            }

            var body = new GameObject("FallbackBody");
            body.transform.SetParent(graphic.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BallClipMsg0000", 0.5f, 0.5f);
            renderer.color = teamIndex == 0 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            renderer.sortingOrder = 20;
            body.transform.localPosition = new Vector3(0f, -80f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
        }

        private void StartDash(int direction)
        {
            dashDirection = direction;
            dashTimer = 0.14f;
            Velocity.x = rimrushObjectsData.PlayerMove * 1.7f * direction;
            actionLatch = Mathf.Max(actionLatch, 0.15f);
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            PlayState("dash");
            GameCore.PlayerSignals.Dispatch(rimrushPlayerSignalType.Dash, Side, playerNo);
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PDash);
        }

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

        private void ActivateJumpBlock()
        {
            jumpBlockActive = true;
            canTakeInHands = false;
        }

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

        private void UpdateJumpBlockThreat()
        {
            if (jumpBlockActive && !ShouldPrimeJumpBlock())
            {
                jumpBlockActive = false;
            }
        }

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
            rimrushAudio.Instance?.Play(rimrushAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        private int GetDunkType()
        {
            var paintStart = Side == 1 ? rimrushObjectsData.PaintStartX : rimrushConstants.Width - rimrushObjectsData.PaintMiddleX;
            var paintMiddle = Side == 1 ? rimrushObjectsData.PaintMiddleX : rimrushConstants.Width - rimrushObjectsData.PaintStartX;
            if (Position.x >= paintStart && Position.x <= paintMiddle && Position.y <= rimrushObjectsData.DunkZone1Y)
            {
                return 1 + Mathf.RoundToInt(2f * Random.value);
            }

            if ((Position.x - paintStart) * Side < 0f && Position.y <= rimrushObjectsData.DunkZone2Y)
            {
                return 1;
            }

            return 0;
        }

        private float DunkTargetX()
        {
            return Side == 1 ? rimrushObjectsData.DunkX : rimrushConstants.Width - rimrushObjectsData.DunkX;
        }

        private static float DunkDuration(int dunkType)
        {
            return dunkType == 2
                ? rimrushObjectsData.Dunk2Duration
                : dunkType == 3
                    ? rimrushObjectsData.Dunk3Duration
                    : rimrushObjectsData.Dunk1Duration;
        }

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

        private void ReleaseDunkBall()
        {
            if (dunkReleased)
            {
                return;
            }

            dunkReleased = true;
            var completed = Random.value <= chanceToCompleteDunk;
            GameCore.MatchProcessor.Shoot(Side, IsHuman, completed ? 1 : 9);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, completed);
            if (!completed)
            {
                rimrushAudio.Instance?.Play(rimrushAssets.Sounds.BBrick);
            }
        }

        private void RestoreBallPickupIfReady()
        {
            if (canTakeInHands || stunTimer > 0f || stealAnimationActive || stealAttemptTimer >= 0f || actionLatch > 0f)
            {
                return;
            }

            canTakeInHands = true;
        }

        private bool IsUnderGlass()
        {
            return Side == -1
                ? Position.x > rimrushConstants.Width - 200f && Position.x < rimrushConstants.Width - 100f
                : Position.x > 100f && Position.x < 200f;
        }

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

        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

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
