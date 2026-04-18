using System.Collections.Generic;
using UnityEngine;

namespace BasketballLegends2020
{
    public sealed class BLArenaObject
    {
        public GameObject Graphic { get; }

        public BLArenaObject(Transform parent)
        {
            Graphic = BLRender.Sprite(
                "ArenaObject",
                BLAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000",
                -299f,
                0f,
                0f,
                0f,
                0,
                parent);
        }
    }

    public sealed class BLBasketObject
    {
        private readonly List<LineRenderer> netLines = new List<LineRenderer>();
        private readonly int side;
        private GameObject graphic;
        private GameObject frontEar;
        private float netPulse;

        public int Side => side;
        public float Center { get; }
        public float Height => BLObjectsData.BasketHeight;

        public BLBasketObject(int side, Transform parent)
        {
            this.side = side;
            Center = side == -1 ? BLObjectsData.BasketCenter : BLObjectsData.BasketCenter2;
            CreateGraphic(parent);
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

        private void CreateGraphic(Transform parent)
        {
            graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(parent, false);
            BLRender.ApplyPixelTransform(graphic.transform, Center, BLObjectsData.BasketHeight, 0.05f);
            graphic.transform.localScale = new Vector3(BLConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), BLConstants.UnitsPerPixel, 1f);

            var basket = new GameObject("BasketGraphic");
            basket.transform.SetParent(graphic.transform, false);
            var renderer = basket.AddComponent<SpriteRenderer>();
            renderer.sprite = BLAtlasCache.Instance.Gameplay.Sprite("BasketGraphic0000", 0.7f, 0.93f);
            renderer.sortingOrder = 4;

            frontEar = BLRender.Sprite(
                side == -1 ? "FrontEarLeft" : "FrontEarRight",
                BLAtlasCache.Instance.Gameplay,
                "FrontEar0000",
                Center,
                BLObjectsData.BasketHeight,
                0.5f,
                0.5f,
                60,
                parent);
            frontEar.transform.localScale = new Vector3(BLConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), BLConstants.UnitsPerPixel, 1f);

            for (var i = 0; i < 10; i++)
            {
                var lineObject = new GameObject($"NetLine{i}");
                lineObject.transform.SetParent(parent, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.018f;
                line.endWidth = 0.018f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.white;
                line.endColor = Color.white;
                line.sortingOrder = 55;
                netLines.Add(line);
            }

            UpdateNetLines();
        }

        private void UpdateNetLines()
        {
            var left = Center - BLObjectsData.BasketRadius + 2f;
            var right = Center + BLObjectsData.BasketRadius - 2f;
            var middle = Center;
            var top = BLObjectsData.BasketHeight + 3f;
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
            netLines[index].SetPosition(0, BLConstants.PixelToWorld(a.x, a.y, 0.03f));
            netLines[index].SetPosition(1, BLConstants.PixelToWorld(b.x, b.y, 0.03f));
        }
    }

    public sealed class BLBallObject
    {
        private const float MaxSubstepTravel = 8f;
        private const int MaxSubsteps = 8;
        private const float RimRestitution = 0.78f;
        private const float BackboardRestitution = 0.82f;
        private const float CollisionSoundCooldownDuration = 0.04f;

        private readonly GameObject graphic;
        private readonly GameObject shadow;
        private readonly BLGameCore gameCore;
        private Vector2 previousPosition;
        private bool visibleNextFrame;
        private bool canScore;
        private bool upperSensorPassed;
        private float collisionSoundCooldown;

        public Vector2 Position;
        public Vector2 Velocity;
        public int Side;
        public string State = "up";
        public float LastShotX;
        public bool IsInGame => State != "inHands";
        public bool CanBeTakenInHands => State != "shooting" && State != "inHands" && State != "score";

        public BLBallObject(BLGameCore gameCore, Transform parent)
        {
            this.gameCore = gameCore;
            graphic = BLRender.Sprite("BallObject", BLAtlasCache.Instance.Gameplay, "BallMC0000", BLConstants.Width2, BLObjectsData.BallIndentYCenter, 0.5f, 0.5f, 50, parent);
            shadow = BLRender.Sprite("BallShadow", BLAtlasCache.Instance.Gameplay, "ShadowMC0002", BLConstants.Width2, BLObjectsData.FloorY, 0.5f, 0.5f, 3, parent);
            shadow.transform.localScale *= 0.7f;
            Restart();
        }

        public void Restart()
        {
            Position = new Vector2(BLConstants.Width2, BLObjectsData.BallIndentYCenter);
            previousPosition = Position;
            Velocity = new Vector2(0f, BLObjectsData.BallUpVelocityY);
            State = "up";
            ResetScoring(false);
            Show();
            UpdateGraphic();
        }

        public void TakeInHands(int side)
        {
            Side = side;
            State = "inHands";
            ResetScoring(false);
            graphic.SetActive(false);
            shadow.SetActive(false);
        }

        public void FromHands(Vector2 playerPosition, float direction)
        {
            Position = playerPosition;
            Velocity = new Vector2(150f * direction, -100f);
            State = "down";
            ResetScoring(false);
            Show();
        }

        public void Shoot(int side, float x, float y, float playerVelocityX, float accuracy)
        {
            Side = side;
            Position = new Vector2(x, y);
            LastShotX = x;
            var baseVelocity = CalcThrowVel(x, y, 0f);
            var distanceToBasket = side == 1 ? x : BLConstants.Width - x;
            var runningDispersion = Mathf.Abs(playerVelocityX) / BLObjectsData.PlayerMoveWithBall * 0.1f;
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
            ResetScoring(true);
            Show();
            BLAudio.Instance?.Play(BLAssets.Sounds.PSwoosh);
        }

        public void Dunk(int side, bool completed)
        {
            Side = side;
            var basketX = side == 1 ? BLObjectsData.BasketCenter : BLObjectsData.BasketCenter2;
            Position = new Vector2(completed ? basketX + 17f * side : basketX, 170f);
            Velocity = completed ? new Vector2(-260f * side, 400f) : new Vector2(-550f * side, 400f);
            State = "dunk";
            ResetScoring(true);
            Show();
        }

        public void ApplySteal(Vector2 playerPosition, float distanceFactor, int direction)
        {
            Position = playerPosition;
            Velocity = new Vector2(
                direction * (BLObjectsData.BallStealVelocityXBase + distanceFactor * BLObjectsData.BallStealVelocityXAdd),
                BLObjectsData.BallStealVelocityY);
            State = "steal";
            ResetScoring(false);
            Show();
            UpdateGraphic();
        }

        public void Update(float dt, BLBasketObject basketLeft, BLBasketObject basketRight)
        {
            if (State == "inHands")
            {
                return;
            }

            collisionSoundCooldown = Mathf.Max(0f, collisionSoundCooldown - dt);
            var steps = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(Mathf.Abs(Velocity.x), Mathf.Abs(Velocity.y)) * dt / MaxSubstepTravel),
                1,
                MaxSubsteps);
            var stepDt = dt / steps;
            for (var i = 0; i < steps; i++)
            {
                previousPosition = Position;
                Velocity.y += BLObjectsData.Gravity.y * BLObjectsData.BallGravMass * stepDt;
                Position += Velocity * stepDt;

                ResolveFloorBounce();
                ResolveWallBounce();
                ResolveBasket(basketLeft, 1);
                ResolveBasket(basketRight, -1);
            }

            UpdateGraphic();
        }

        private void ResolveFloorBounce()
        {
            if (Position.y > BLObjectsData.BallFloorY)
            {
                Position.y = BLObjectsData.BallFloorY;
                if (Velocity.y > 0f)
                {
                    Velocity.y = BLObjectsData.BallBounce;
                    Velocity.x *= 0.86f;
                    State = "bounce";
                    BLAudio.Instance?.Play(BLAssets.Sounds.BBounce);
                }
            }
        }

        private void ResolveWallBounce()
        {
            if (Position.x < 5f || Position.x > BLConstants.Width - 5f)
            {
                Position.x = Mathf.Clamp(Position.x, 5f, BLConstants.Width - 5f);
                Velocity.x *= -0.75f;
            }
        }

        private void ResolveBasket(BLBasketObject basket, int scoringSide)
        {
            if (basket == null)
            {
                return;
            }

            ResolveBackboardCollision(basket);
            ResolveRimCollision(new Vector2(basket.Center - BLObjectsData.BasketRadius, basket.Height), basket);
            ResolveRimCollision(new Vector2(basket.Center + BLObjectsData.BasketRadius, basket.Height), basket);
            ProcessScoreSensors(basket, scoringSide);
        }

        private void ResolveBackboardCollision(BLBasketObject basket)
        {
            var glassTop = basket.Height + BLObjectsData.GlassY;
            var glassBottom = glassTop + BLObjectsData.GlassHeight;
            if (Position.y + BLObjectsData.BallRadius < glassTop || Position.y - BLObjectsData.BallRadius > glassBottom)
            {
                return;
            }

            if (basket.Side == -1)
            {
                var planeX = BLObjectsData.GlassWidth;
                if (Velocity.x < 0f && Position.x - BLObjectsData.BallRadius <= planeX)
                {
                    Position.x = planeX + BLObjectsData.BallRadius;
                    Velocity.x = Mathf.Abs(Velocity.x) * BackboardRestitution;
                    Velocity.y *= 0.97f;
                    SetBasketState();
                    PlayBasketSound(2);
                }

                return;
            }

            var rightPlaneX = BLConstants.Width - BLObjectsData.GlassWidth;
            if (Velocity.x > 0f && Position.x + BLObjectsData.BallRadius >= rightPlaneX)
            {
                Position.x = rightPlaneX - BLObjectsData.BallRadius;
                Velocity.x = -Mathf.Abs(Velocity.x) * BackboardRestitution;
                Velocity.y *= 0.97f;
                SetBasketState();
                PlayBasketSound(2);
            }
        }

        private void ResolveRimCollision(Vector2 rimCenter, BLBasketObject basket)
        {
            var combinedRadius = BLObjectsData.BallRadius + BLObjectsData.BasketPartRadius;
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

        private void ProcessScoreSensors(BLBasketObject basket, int scoringSide)
        {
            if (!canScore)
            {
                return;
            }

            if (TouchesSensor(previousPosition, Position, basket.Center, basket.Height + BLObjectsData.SensorUp))
            {
                upperSensorPassed = true;
            }

            if (!TouchesSensor(previousPosition, Position, basket.Center, basket.Height + BLObjectsData.SensorDown))
            {
                return;
            }

            if (upperSensorPassed)
            {
                canScore = false;
                upperSensorPassed = false;
                State = "score";
                PlayBasketSound(0);
                gameCore.OnBallScored(scoringSide);
            }
            else
            {
                canScore = false;
            }
        }

        private static bool TouchesSensor(Vector2 start, Vector2 end, float centerX, float topY)
        {
            var minX = centerX - BLObjectsData.SensorHalf - BLObjectsData.BallRadius;
            var maxX = centerX + BLObjectsData.SensorHalf + BLObjectsData.BallRadius;
            var minY = topY - BLObjectsData.BallRadius;
            var maxY = topY + BLObjectsData.SensorHeight + BLObjectsData.BallRadius;
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
                BLAudio.Instance?.Play(BLAssets.Sounds.BNet);
                return;
            }

            collisionSoundCooldown = CollisionSoundCooldownDuration;
            if (type == 1)
            {
                var velocityMagnitude = Velocity.magnitude;
                var volume = velocityMagnitude > 300f ? 1f : velocityMagnitude / 300f * 0.8f;
                BLAudio.Instance?.Play(BLAssets.Sounds.BRing, Mathf.Clamp(volume, 0.1f, 1f));
            }
            else
            {
                BLAudio.Instance?.Play(BLAssets.Sounds.BBasket);
            }
        }

        private void ResetScoring(bool armed)
        {
            canScore = armed;
            upperSensorPassed = false;
            collisionSoundCooldown = 0f;
        }

        private Vector2 CalcThrowVel(float x, float y, float offset)
        {
            float targetX;
            float distance;
            if (Side == 1)
            {
                targetX = BLObjectsData.BasketCenter + offset;
                distance = x;
            }
            else
            {
                targetX = BLObjectsData.BasketCenter2 + offset;
                distance = BLConstants.Width - x;
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
            return CalcVel(x, y, targetX, BLObjectsData.BasketHeight, arc);
        }

        private Vector2 CalcVel(float x, float y, float targetX, float targetY, float arc)
        {
            var gravity = BLObjectsData.Gravity.y * BLObjectsData.BallGravMass;
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
                vertical = BLObjectsData.VerticalDispersion;
            }
            else
            {
                vertical = (1f - (295f - y) / 60f) * BLObjectsData.VerticalDispersion;
            }

            float distanceDispersion =
                distance <= 100f ? 0f :
                distance <= 200f ? 0.01f :
                distance <= 300f ? 0.02f :
                distance <= 400f ? 0.03f :
                distance <= 490f ? 0.04f :
                distance <= 540f ? 0.01f : 0.07f;

            var sign = Random.value < 0.5f ? -1f : 1f;
            var value = sign * (BLObjectsData.Dispersion + vertical + distanceDispersion + accuracy + running) * Random.value;
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

        private void UpdateGraphic()
        {
            if (visibleNextFrame)
            {
                visibleNextFrame = false;
                graphic.SetActive(true);
                shadow.SetActive(true);
            }

            BLRender.ApplyPixelTransform(graphic.transform, Position.x, Position.y, 0.2f, 1f, -Position.x * 0.1f);
            var shadowY = BLObjectsData.FloorY + 3f;
            BLRender.ApplyPixelTransform(shadow.transform, Position.x, shadowY, 0.01f, Mathf.Clamp01(1f - (shadowY - Position.y) / 420f) * 0.7f);
        }
    }

    public sealed class BLPlayerObject
    {
        private readonly GameObject graphic;
        private readonly GameObject shadow;
        private readonly DBLiteArmature armature;
        private readonly IBLPlayerController controller;
        private readonly int teamIndex;
        private readonly int playerNo;
        private float actionLatch;
        private string visualState = "";
        private float dashTimer;
        private float dashCooldown = BLObjectsData.DashDelay;
        private int dashDirection;
        private float stealAttemptTimer = -1f;
        private float stunTimer;
        private float facingDirection;
        private float stealFacingDirection;
        private bool canTakeInHands;

        public BLGameCore GameCore { get; }
        public Vector2 Position;
        public Vector2 Velocity;
        public int Side { get; }
        public bool WithBall { get; private set; }
        public bool IsHuman { get; }
        public bool IsGrounded { get; private set; } = true;
        public float AttackTargetX => Side == -1 ? BLObjectsData.BasketCenter2 : BLObjectsData.BasketCenter;
        public bool IsDashing => dashTimer > 0f;
        public bool IsMoving => Mathf.Abs(Velocity.x) > 20f;
        public float FacingDirection => facingDirection;
        public bool CanTakeInHands => canTakeInHands && !WithBall;

        public BLPlayerObject(BLGameCore gameCore, int teamIndex, int team, int player, int form, int playerNo, string playerBrain, Transform parent)
        {
            GameCore = gameCore;
            this.teamIndex = teamIndex;
            this.playerNo = playerNo;
            Side = teamIndex == 0 ? -1 : 1;
            IsHuman = !playerBrain.StartsWith("B");

            graphic = new GameObject($"Player_{teamIndex}_{playerNo}");
            graphic.transform.SetParent(parent, false);

            shadow = BLRender.Sprite($"PlayerShadow_{teamIndex}_{playerNo}", BLAtlasCache.Instance.Gameplay, playerNo == 0 ? "ShadowMC0000" : "ShadowMC0001", 0f, 0f, 0.5f, 0.5f, 2, parent);

            armature = BLPlayersData.BuildGameplayArmature($"playerSmall_{teamIndex}_{playerNo}");
            if (armature != null)
            {
                armature.transform.SetParent(graphic.transform, false);
                armature.transform.localPosition = new Vector3(0f, -35f, 0f);
                armature.transform.localScale = new Vector3(0.73f, 0.73f, 1f);
                BLPlayersData.SwitchPlayer(armature, BLPlayersData.TeamSize * (team - 1) + player, 2 * (team - 1) + form);
            }
            else
            {
                CreateFallbackAvatar();
            }

            controller = IsHuman
                ? new BLKeyboardController(playerBrain == "P2" ? 1 : 0)
                : new BLAIController(this);

            Restart(0);
        }

        public void Restart(int startSide)
        {
            WithBall = false;
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            dashCooldown = BLObjectsData.DashDelay;
            stealAttemptTimer = -1f;
            stunTimer = 0f;
            facingDirection = -Side;
            stealFacingDirection = facingDirection;
            canTakeInHands = true;
            var x = BLConstants.Width2 + Side * (playerNo == 0 ? BLObjectsData.PlayerIndentX : 200f);
            if (startSide == Side)
            {
                x = Side == -1 ? BLObjectsData.IndentGeneralX : BLConstants.Width - BLObjectsData.IndentGeneralX;
            }

            Position = new Vector2(x, BLObjectsData.PlayerIndentY);
            IsGrounded = true;
            PlayState("idle");
            UpdateGraphic();
        }

        public void Update(float dt)
        {
            actionLatch -= dt;
            dashCooldown -= dt;

            if (stunTimer > 0f)
            {
                stunTimer -= dt;
                stealAttemptTimer = -1f;
                dashTimer = 0f;
                dashDirection = 0;
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

            controller.UpdateController(dt);
            UpdateFacing();

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
                Velocity.x = BLObjectsData.PlayerMove * 1.7f * dashDirection;
                PlayState("dash");
                if (dashTimer <= 0f)
                {
                    dashTimer = 0f;
                    dashDirection = 0;
                    dashCooldown = BLObjectsData.DashDelay;
                }
            }
            else
            {
                var moveSpeed = WithBall ? BLObjectsData.PlayerMoveWithBall : BLObjectsData.PlayerMove;
                Velocity.x = controller.CurrentMove * moveSpeed;

                if (controller.CurrentDash != 0 && IsGrounded && dashCooldown <= 0f)
                {
                    StartDash(controller.CurrentDash);
                }
            }

            if (dashTimer <= 0f && controller.CurrentJump && IsGrounded)
            {
                Velocity.y = BLObjectsData.PlayerJump;
                IsGrounded = false;
                PlayState(WithBall ? "jump_wb" : "jump");
            }

            if (dashTimer <= 0f && controller.CurrentAction && actionLatch <= 0f)
            {
                if (WithBall)
                {
                    MakeThrow();
                    actionLatch = 0.35f;
                }
                else
                {
                    BeginSteal();
                }
            }

            if (!IsGrounded)
            {
                Velocity.y += BLObjectsData.Gravity.y * 3f * dt;
            }

            Position += Velocity * dt;
            Position.x = Mathf.Clamp(Position.x, 20f, BLConstants.Width - 20f);
            if (Position.y >= BLObjectsData.PlayerIndentY)
            {
                Position.y = BLObjectsData.PlayerIndentY;
                Velocity.y = 0f;
                if (!IsGrounded)
                {
                    IsGrounded = true;
                    if (!WithBall)
                    {
                        canTakeInHands = true;
                    }
                    PlayState(WithBall ? "landing_wb" : "landing");
                }
            }

            if (IsGrounded && dashTimer <= 0f && actionLatch <= 0f)
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

        public void TakeBallInHands()
        {
            WithBall = true;
            canTakeInHands = false;
            stealAttemptTimer = -1f;
            stunTimer = 0f;
            GameCore.Ball.TakeInHands(Side);
            PlayState(IsGrounded ? "idle_wb" : "fly1_wb");
        }

        public void FreeBall()
        {
            if (!WithBall)
            {
                return;
            }

            WithBall = false;
            canTakeInHands = actionLatch <= 0f && stunTimer <= 0f;
            GameCore.Ball.FromHands(Position + new Vector2(0f, -45f), Mathf.Sign(graphic.transform.localScale.x));
        }

        public void NotifyBallLoose()
        {
            WithBall = false;
            stealAttemptTimer = -1f;
            stunTimer = 0f;
            canTakeInHands = true;
        }

        public void OnStolen()
        {
            GetBeStolen(Position.x, false);
        }

        public float CheckToBeStolen(float thiefX, float thiefFacingScaleX)
        {
            if (!IsGrounded || stunTimer > 0f)
            {
                return -1f;
            }

            if (thiefFacingScaleX >= 0f)
            {
                return Position.x >= thiefX && Position.x <= thiefX + BLObjectsData.StealDistance
                    ? Mathf.Abs(Position.x - thiefX)
                    : -1f;
            }

            return Position.x >= thiefX - BLObjectsData.StealDistance && Position.x <= thiefX
                ? Mathf.Abs(Position.x - thiefX)
                : -1f;
        }

        public bool GetBeStolen(float thiefX, bool applyBallSteal = true)
        {
            var hadBall = WithBall;
            WithBall = false;
            dashTimer = 0f;
            dashDirection = 0;
            stealAttemptTimer = -1f;
            stunTimer = Mathf.Max(stunTimer, BLObjectsData.StunDuration);
            canTakeInHands = false;
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            PlayState("stun");
            BLAudio.Instance?.Play(BLAssets.Sounds.PStunned, 0.9f);

            if (hadBall && applyBallSteal && GameCore.Ball != null)
            {
                var delta = Position.x - thiefX;
                var direction = delta > 0f ? 1 : -1;
                var distanceFactor = Mathf.Clamp01(Mathf.Abs(delta) / BLObjectsData.StealDistance);
                GameCore.Ball.ApplySteal(Position + new Vector2(0f, -45f), distanceFactor, direction);
            }

            return hadBall;
        }

        private void MakeThrow()
        {
            WithBall = false;
            canTakeInHands = IsGrounded;
            var releaseX = Position.x - Side * 20f;
            var releaseY = Position.y - 55f;
            GameCore.Ball.Shoot(Side, releaseX, releaseY, Velocity.x, 0f);
            PlayState(IsGrounded ? "throw_land" : "fly1");
        }

        private void BeginSteal()
        {
            stealAttemptTimer = 0.06f;
            stealFacingDirection = facingDirection;
            actionLatch = Mathf.Max(actionLatch, BLObjectsData.StealDuration);
            canTakeInHands = false;
            PlayState("steal");
            BLAudio.Instance?.Play(BLAssets.Sounds.PSwoosh, 0.7f);
        }

        private void ResolveStealAttempt()
        {
            stealAttemptTimer = -1f;
            if (GameCore.TryStealBall(this, stealFacingDirection))
            {
                actionLatch = Mathf.Max(actionLatch, 0.18f);
                return;
            }
        }

        public float CheckLooseBallPickup(BLBallObject ball)
        {
            if (ball == null || !ball.CanBeTakenInHands || !CanTakeInHands)
            {
                return -1f;
            }

            var delta = ball.Position - Position;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            if (absX > BLObjectsData.BallPickupDistanceX || absY > BLObjectsData.BallPickupDistanceY)
            {
                return -1f;
            }

            return delta.sqrMagnitude;
        }

        private void UpdateFacing()
        {
            var ballHolder = GameCore.FindBallHolder();
            var faceTarget = AttackTargetX;
            if (WithBall)
            {
                faceTarget = AttackTargetX;
            }
            else if (ballHolder != null && ballHolder.Side != Side)
            {
                faceTarget = ballHolder.Position.x;
            }
            else if (GameCore.Ball != null)
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

        private void UpdateGraphic()
        {
            graphic.transform.position = BLConstants.PixelToWorld(Position.x, Position.y, 0.12f + playerNo * 0.01f);
            graphic.transform.localScale = new Vector3(BLConstants.UnitsPerPixel * facingDirection, BLConstants.UnitsPerPixel, 1f);

            var shadowScale = Mathf.Clamp01(1f - (BLObjectsData.PlayerIndentY - Position.y) / 300f);
            BLRender.ApplyPixelTransform(shadow.transform, Position.x, BLObjectsData.FloorY + 6f, 0.02f, Mathf.Max(0.2f, shadowScale));
        }

        private void CreateFallbackAvatar()
        {
            var body = new GameObject("FallbackBody");
            body.transform.SetParent(graphic.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = BLAtlasCache.Instance.Gameplay.Sprite("BallClipMsg0000", 0.5f, 0.5f);
            renderer.color = teamIndex == 0 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            renderer.sortingOrder = 20;
            body.transform.localPosition = new Vector3(0f, -80f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
        }

        private void StartDash(int direction)
        {
            dashDirection = direction;
            dashTimer = 0.14f;
            Velocity.x = BLObjectsData.PlayerMove * 1.7f * direction;
            actionLatch = Mathf.Max(actionLatch, 0.15f);
            PlayState("dash");
            BLAudio.Instance?.Play(BLAssets.Sounds.PDash);
        }

        private void RestoreBallPickupIfReady()
        {
            if (canTakeInHands || stunTimer > 0f || stealAttemptTimer >= 0f || actionLatch > 0f)
            {
                return;
            }

            canTakeInHands = true;
        }
    }
}
