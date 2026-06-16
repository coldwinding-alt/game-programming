// File function: Game scene object management (court, basket, basketball, teleportation effects, shields, skill effects)
// Summary: Create and manage all visible game objects in the game: court background and lighting, basket and net, basketball and its physical movement, portal effects, shield effects, skill activation effects. This file is quite large and covers the creation and update logic for most of the visual elements in the game.

// Class name Description

// ──────────────────────────────────────────────
// mlpArenaObject court object — manages court visuals, fog and wind effects (FogWind), boundary collisions
// mlpBasketObject Basket object — Basket animation, net bag swing, sensor collision (goal detection)

// mlpBallObject Basketball object - shot flight, bounce, physics after being stolen/blocked, basket detection

// mlpTeleportFx teleportation effects — visual particle effects for teleportation skills
// mlpShieldObject shield object — Basket shield skill, which can block flying basketballs

// mlpPlayerSkillFx Skill Lighting — glow/particle visual feedback for player skills

// mlpPlayerObject player object - core class, manages all player states (movement, jumping, shooting, dunking, stealing, blocking, AI, skills, etc.)



using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Game sprite loader: Load picture sprites for basketball, court, basket and other games from the atlas, with caching to avoid repeated loading.

    /// </summary>
    public static class mlpGameplaySpriteLoader
    {
        // Sprite cache: Cache the created Sprite by resource path and anchor point to avoid repeated loading.

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Load the sprite of the theme basketball. If the theme does not have exclusive resources, it will fall back to the default album.
        /// </summary>
        /// <param name="theme">Basketball visual theme to find</param>

        /// <param name="anchorX">Elf horizontal anchor point, range 0-1</param>

        /// <param name="anchorY">Sprite vertical anchor point, range 0-1</param>
        /// <returns>The loaded sprite, returns null if not found. </returns>
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
        /// Load the game sprite from Resources, and fall back to the atlas search when the direct resource path is missing.

        /// </summary>
        /// <param name="resourcePath">Resource path under the Images folder</param>
        /// <param name="anchorX">Elf horizontal anchor point, range 0-1</param>

        /// <param name="anchorY">Sprite vertical anchor point, range 0-1</param>
        /// <param name="fallbackAtlas">Atlas searched when direct resource path is missing</param>

        /// <param name="fallbackFrame">Atlas frame name used as fallback sprite</param>

        /// <returns>The loaded sprite, returns null if not found. </returns>
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
        /// Loads textures from Resources and creates Sprites, caching the results for immediate return on repeated requests.
        /// </summary>
        /// <param name="resourcePath">Resource path under the Images folder</param>
        /// <param name="anchorX">Elf horizontal anchor point, range 0-1</param>

        /// <param name="anchorY">Sprite vertical anchor point, range 0-1</param>
        /// <returns>The created sprite, returns null if loading fails. </returns>
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
            // Causes the themed basketball to stay aligned with the original album basketball borders when rotated during the game.

            var sprite = Sprite.Create(texture, rect, new Vector2(anchorX, 1f - anchorY), 1f, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// Stadium Objects: Create and manage background images, lighting effects and visual decorations for playing fields.
    /// </summary>
    public sealed class mlpArenaObject
    {
        // Stadium logical width: Used to scale the background image to the logical size used in the game.

        private const float ArenaLogicalWidth = 1398f;
        // Stadium logical height: used to scale the background image to the logical size used in the game.

        private const float ArenaLogicalHeight = 480f;
        // Background wind and fog skeleton animation resource name.

        private const string FogWindArmatureName = "dbanims/backwind_01";
        // The basic position of the three-layer wind and fog special effects.

        private static readonly Vector2[] FogWindLayerPositions =
        {
            new Vector2(168f, 172f),
            new Vector2(408f, 136f),
            new Vector2(652f, 184f)
        };
        // Basic scaling of three layers of wind and fog effects.

        private static readonly float[] FogWindLayerScales = { 1.12f, 1.3f, 1.06f };
        // Rendering levels for three layers of wind and fog effects.

        private static readonly int[] FogWindLayerSortingOrders = { 1, 2, 3 };
        // Transparency offset for three-layer wind and fog effects.

        private static readonly float[] FogWindLayerAlphaBiases = { 0.82f, 1f, 0.9f };

        private sealed class FogWindLayer
        {
            // The base position of this layer of wind and mist.

            public Vector2 BasePosition;
            // The root node of this layer of wind and fog.

            public GameObject Root;
            // The skeletal animation object of this layer of wind and fog.

            public DBLiteArmature Armature;
            // The base scaling of this layer of wind and fog.

            public float BaseScale;
            // The rendering order benchmark for this layer of wind and fog.

            public int SortingOrder;
            // The transparency of this layer of wind and fog is offset.

            public float AlphaBias;
        }

        // Wind and fog special effect root node.

        private readonly GameObject fogWindFxRoot;
        // Array of three-layer wind and fog instances.

        private readonly FogWindLayer[] fogWindLayers = new FogWindLayer[FogWindLayerPositions.Length];

        // The stadium root object can be directly connected to the scene externally.

        public GameObject Graphic { get; }

        /// <summary>
        /// Create a pitch background sprite and scale it to logical match dimensions.

        /// </summary>
        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        public mlpArenaObject(Transform parent)
        {
            // 1. Create the stadium background GameObject and hang it under the scene parent node

            Graphic = new GameObject("ArenaObject");
            Graphic.transform.SetParent(parent, false);

            // 2. Add SpriteRenderer and load the stadium background sprite (prioritize independent resources and fall back to album frames)

            var renderer = Graphic.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.ArenaBackdrop,
                0f,
                0f,
                mlpAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000");

            // 3. Set the rendering ordering level (0 = lowest layer, all other objects are drawn above it)

            renderer.sortingOrder = 0;

            // 4. Position the court to pixel-aligned world coordinates (left offset -299 pixels, Y = 0)

            mlpRender.ApplyPixelTransform(Graphic.transform, -299f, 0f);

            // 5. Scale according to the logical competition size (1398×480) to make the high-definition material consistent with the old version of the atlas frame

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
        /// Keeps high-resolution individual pitch footage at the same logical match size as legacy atlas frames.
        /// </summary>
        /// <param name="transform">Transform to scale</param>

        /// <param name="sprite">Pixel size driven scaling of sprites</param>

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
    /// Basket object: manages the physical position of the basket, the animation of the basket, and the scoring sensor (which detects whether the ball passes through the basket).

    /// </summary>
    public sealed class mlpBasketObject
    {
        // A collection of net lines used to simulate the swing of the net.

        private readonly List<LineRenderer> netLines = new List<LineRenderer>();
        // The side of the court where the basket is located, -1 means left, 1 means right.

        private readonly int side;
        // Basket root node.

        private GameObject graphic;
        // The front shielding layer of the basket is used to block the basketball during dunking.

        private GameObject frontEar;
        // Nets swing pulse intensity.

        private float netPulse;

        // Court side, -1 left, 1 right.

        public int Side => side;
        // Basket center X coordinate.

        public float Center { get; }
        // Basket height, read scene data directly.

        public float Height => mlpObjectsData.BasketHeight;

        /// <summary>
        /// Create the court-side hoop, including the hoop, front lugs, and net.

        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        public mlpBasketObject(int side, Transform parent)
        {
            this.side = side;
            Center = side == -1 ? mlpObjectsData.BasketCenter : mlpObjectsData.BasketCenter2;
            CreateGraphic(parent);
        }

        /// <summary>
        /// The net animation is updated every frame so that the swing decays over time.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public void Update(float dt)
        {
            netPulse = Mathf.Max(0f, netPulse - dt * 2f);
            UpdateNetLines();
        }

        /// <summary>
        /// Trigger the net swing pulse, causing the net to produce a ripple effect when the basketball passes through it.

        /// </summary>
        public void HitNet()
        {
            netPulse = 1f;
        }

        /// <summary>
        /// Hides the front lug graphic to avoid blocking the basketball when dunking.

        /// </summary>
        public void HideEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(false);
            }
        }

        /// <summary>
        /// The leading edge ear pattern is restored after a dunk or special skill is completed.

        /// </summary>
        public void ShowEar()
        {
            if (frontEar != null)
            {
                frontEar.SetActive(true);
            }
        }

        /// <summary>
        /// Constructs all the visual sub-objects of the basket: the hoop sprite, the leading edge ear overlay, and the ten LineRenderer net lines.

        /// </summary>
        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        private void CreateGraphic(Transform parent)
        {
            // 1. Create the basket root node and hang it under the scene parent node

            graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(parent, false);

            // 2. Position the basket to pixel-aligned world coordinates (basket center X, basket height Y)

            mlpRender.ApplyPixelTransform(graphic.transform, Center, mlpObjectsData.BasketHeight, 0.05f);

            // 3. Set zoom (pixel perfect) and flip the right basket horizontally

            graphic.transform.localScale = new Vector3(mlpConstants.UnitsPerPixel * (side == -1 ? 1f : -1f), mlpConstants.UnitsPerPixel, 1f);

            // 4. Create the hoop sprite (anchor points 0.7, 0.93 to align the hoop with the front edge of the basket)

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

            // 5. Create the leading edge ear overlay (sort level 60, covering the basketball when dunking, can be hidden if needed)

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

            // 6. Create ten LineRenderer net lines (simulating a grid-like net structure)
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

            // 7. Initialize the basket line position (3 rows × 10 line segments constitute a grid)

            UpdateNetLines();
        }

        /// <summary>
        /// Reposition the Ten Nets LineRenderer to simulate a swinging net mesh.

        /// </summary>
        private void UpdateNetLines()
        {
            // 1. Calculate the left and right boundaries and center position of the basket

            var left = Center - mlpObjectsData.BasketRadius + 2f;
            var right = Center + mlpObjectsData.BasketRadius - 2f;
            var middle = Center;
            // 2. Calculate the starting height of the top of the net

            var top = mlpObjectsData.BasketHeight + 3f;
            // 3. Calculate the swing offset of the net (varies with time and pulse intensity)

            var sway = Mathf.Sin(Time.time * 18f) * 5f * netPulse;

            // 4. Define the positions of the three nodes at the top of the net

            var pointsTop = new[]
            {
                new Vector2(left, top),
                new Vector2(middle, top),
                new Vector2(right, top)
            };
            // 5. Define the positions of the three nodes in the middle of the basket (with swing effect)

            var pointsMid = new[]
            {
                new Vector2(left + sway, top + 14f),
                new Vector2(middle - sway * 0.5f, top + 12f),
                new Vector2(right + sway, top + 14f)
            };
            // 6. Define the positions of the three nodes at the bottom of the net (with reverse swing effect)

            var pointsBot = new[]
            {
                new Vector2(left - sway * 0.5f, top + 32f),
                new Vector2(middle + sway, top + 30f),
                new Vector2(right - sway * 0.5f, top + 32f)
            };

            // 7. Set the left basket net lines (top to middle, middle to bottom)

            SetLine(0, pointsTop[0], pointsMid[0]);
            SetLine(1, pointsMid[0], pointsBot[0]);
            // 8. Set the middle net line

            SetLine(2, pointsTop[1], pointsMid[1]);
            SetLine(3, pointsMid[1], pointsBot[1]);
            // 9. Set the basketball net lines on the right side

            SetLine(4, pointsTop[2], pointsMid[2]);
            SetLine(5, pointsMid[2], pointsBot[2]);
            // 10. Set horizontal connecting lines (middle and bottom)

            SetLine(6, pointsMid[0], pointsMid[1]);
            SetLine(7, pointsMid[2], pointsMid[1]);
            SetLine(8, pointsBot[0], pointsBot[1]);
            SetLine(9, pointsBot[2], pointsBot[1]);
        }

        /// <summary>
        /// Set the start and end world coordinates of a single Nets LineRenderer.

        /// </summary>
        /// <param name="index">Nets line renderer index (0-9)</param>

        /// <param name="a">The first endpoint of the line</param>

        /// <param name="b">Second endpoint of line</param>

        private void SetLine(int index, Vector2 a, Vector2 b)
        {
            netLines[index].SetPosition(0, mlpConstants.PixelToWorld(a.x, a.y, 0.03f));
            netLines[index].SetPosition(1, mlpConstants.PixelToWorld(b.x, b.y, 0.03f));
        }
    }

    /// <summary>
    /// Basketball object: manages all states of the basketball - being held, shooting, bouncing, entering the basket, dunking, being blocked, etc., as well as physical movement and collision detection.

    /// </summary>
    public sealed class mlpBallObject
    {
        // The maximum movement distance allowed in a single physical sub-step to prevent the basketball from penetrating the collision body at high speed.

        private const float MaxSubstepTravel = 8f;
        // The maximum number of physical sub-steps that each frame can be divided into.

        private const int MaxSubsteps = 8;
        // Collision rebound coefficient of basketball hoop.

        private const float RimRestitution = 0.78f;
        // Backboard collision rebound coefficient.

        private const float BackboardRestitution = 0.82f;
        // Collision sound effect cooling time to avoid too intensive continuous playback.

        private const float CollisionSoundCooldownDuration = 0.04f;
        // Extra relaxed X tolerance when scoring dunks.

        private const float GuaranteedDunkScoreExtraX = 6f;

        // Basketball visual node.
        private readonly GameObject graphic;
        // Basketball shadow node.

        private readonly GameObject shadow;
        // Game core reference, used to inform scoring, steals and other global logic.

        private readonly mlpGameCore gameCore;
        // The ball position from the previous frame.

        private Vector2 previousPosition;
        // Whether it will remain visible for the next frame.

        private bool visibleNextFrame;
        // Whether it is currently allowed to enter the score determination process.

        private bool canScore;
        // Whether to enable the must-go dunk scoring determination.

        private bool guaranteedDunkScore;
        // A must-score marker in the tutorial.

        private bool tutorialGuaranteedScore;
        // The attacking team that currently has the scoring decision activated.

        private int scoreArmedSide;
        // Pick up ball lock countdown.

        private float pickupLockTimer;
        // Collision sound effect cooldown timer.

        private float collisionSoundCooldown;
        // Has been removed from the physical system.

        private bool physicsRemoved;
        // The player object associated with the alley-oop.

        private mlpPlayerObject alleyOopPlayer;

        // ball position.

        public Vector2 Position;
        // The speed of the ball.

        public Vector2 Velocity;
        // The side of the court where the ball belongs.

        public int Side;
        // Current ball status string, such as up, inHands, shooting.

        public string State = "up";
        // The X coordinate recorded during the most recent shot.

        public float LastShotX;
        // Previous frame position, read-only exposed to external use.

        public Vector2 PreviousPosition => previousPosition;
        // Are you still in the physical flow of the game?

        public bool IsInGame => State != "inHands" && !physicsRemoved;
        // Whether it is in a flight state that can be blocked.

        public bool IsBlockable => State == "shooting";
        // Whether it is allowed to be picked up or caught by players.

        public bool CanBeTakenInHands =>
            pickupLockTimer <= 0f &&
            !physicsRemoved &&
            State != "shooting" &&
            State != "alleyOop" &&
            State != "inHands" &&
            State != "score";

        /// <summary>
        /// Create the basketball sprite and its shadow, then reset it to the starting position.

        /// </summary>
        /// <param name="gameCore">Central game logic coordinator</param>

        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        public mlpBallObject(mlpGameCore gameCore, Transform parent)
        {
            // 1. Save the central game logic coordinator reference
            this.gameCore = gameCore;

            // 2. Create the basketball elf GameObject and hang it under the scene parent node

            graphic = new GameObject("BallObject");
            graphic.transform.SetParent(parent, false);

            // 3. Add SpriteRenderer and load the theme basketball sprite (if any), otherwise fall back to the default album frame

            var graphicRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicRenderer.sprite = ResolveBallSprite();
            graphicRenderer.sortingOrder = 50;

            // 4. Position the basketball to the starting position of the midcourt (screen center X, basketball starting height Y)

            mlpRender.ApplyPixelTransform(graphic.transform, mlpConstants.Width2, mlpObjectsData.BallIndentYCenter, 0.2f);

            // 5. Create a basketball shadow GameObject and hang it under the scene parent node

            shadow = new GameObject("BallShadow");
            shadow.transform.SetParent(parent, false);

            // 6. Load the basketball-specific shadow sprite and set the sorting level (3 = ground layer)

            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.PlayerShadowBall,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "ShadowMC0002");
            shadowRenderer.sortingOrder = 3;

            // 7. Position the shadow to the ground Y coordinate and scale it to 0.7x to make it smaller than the player shadow

            mlpRender.ApplyPixelTransform(shadow.transform, mlpConstants.Width2, mlpObjectsData.FloorY, 0.02f);
            shadow.transform.localScale *= 0.7f;

            // 8. Reset the basketball state to the starting position of midfield and prepare for a new round of competition.

            Restart();
        }

        /// <summary>
        /// Resets the basketball to midcourt position and imparts upward speed, ready for another round.

        /// </summary>
        public void Restart()
        {
            // 1. Reset the basketball position to the center of the court

            Position = new Vector2(mlpConstants.Width2, mlpObjectsData.BallIndentYCenter);
            // 2. Record the position of the previous frame for physical calculation

            previousPosition = Position;
            // 3. Give the basketball an upward initial speed

            Velocity = new Vector2(0f, mlpObjectsData.BallUpVelocityY);
            // 4. Set the basketball status to "rising"

            State = "up";
            // 5. The marked physical has not been removed

            physicsRemoved = false;
            // 6. Clear air-connect related data

            alleyOopPlayer = null;
            gameCore.IsAlleyOop = false;
            // 7. Reset score status

            ResetScoring(false);
            // 8. Show basketball and shadow

            Show();
            // 9. Update basketball visual position

            UpdateGraphic();
        }

        /// <summary>
        /// Momentarily places the basketball at the specified location with zero velocity, for scripted moments in the tutorial.

        /// </summary>
        /// <param name="position">World coordinates</param>

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
        /// Place the basketball into the player's hand, hiding the basketball and the shadow sprite.

        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

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
        /// Release the basketball from the player's hands with a slight forward and downward motion.

        /// </summary>
        /// <param name="playerPosition">Player's current world coordinates</param>

        /// <param name="direction">Movement or throwing direction (-1 or 1)</param>

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
        /// Throw the basketball toward the hoop with parabolic physics, applying precision offsets based on distance and movement speed.
        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

        /// <param name="x">Horizontal coordinates in pixel space</param>

        /// <param name="y">Vertical coordinate in pixel space</param>

        /// <param name="playerVelocityX">Horizontal velocity when the pitcher releases the ball</param>

        /// <param name="accuracy">Shooting accuracy correction value (the lower, the more accurate)</param>

        public void Shoot(int side, float x, float y, float playerVelocityX, float accuracy)
        {
            // 1. Record the shooting side, release position and X coordinate of the last shot

            Side = side;
            Position = new Vector2(x, y);
            previousPosition = Position;
            LastShotX = x;

            // 2. Calculate the baseline parabolic velocity of an accurate shot (hitting the center of the basket without offset)

            var baseVelocity = CalcThrowVel(x, y, 0f);

            // 3. Calculate the offset coefficient based on distance, height, running speed and accuracy

            var distanceToBasket = side == 1 ? x : mlpConstants.Width - x;
            var runningDispersion = Mathf.Abs(playerVelocityX) / mlpObjectsData.PlayerMoveWithBall * 0.1f;
            var dispersion = CalcDispersion(distanceToBasket, y, runningDispersion, accuracy);

            // 4. Determine the final speed according to the offset coefficient: slight offset scales the X component, severe offset left and right deflection

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

            // 5. Switch to shooting state, activate the score sensor, display the basketball and play the shot sound effect

            State = "shooting";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(true);
            scoreArmedSide = side;
            Show();
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh);
        }

        /// <summary>
        /// Play a basketball dunk trajectory animation; pre-activate the scoring sensor for immediate scoring when the dunk is completed.

        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

        /// <param name="completed">True when the dunk animation ends successfully</param>

        public void Dunk(int side, bool completed)
        {
            // 1. Record the X coordinates of the dunk side and the center of the basket

            Side = side;
            var basketX = side == 1 ? mlpObjectsData.BasketCenter : mlpObjectsData.BasketCenter2;

            // 2. Set the position of the basketball: offset by 17 pixels when completed, directly above the basket when not completed
            Position = new Vector2(completed ? basketX + 17f * side : basketX, 170f);
            previousPosition = Position;
            LastShotX = Position.x;

            // 3. Set the pop-up speed: flick when completed, bounce hard when not completed

            Velocity = completed ? new Vector2(-260f * side, 400f) : new Vector2(-550f * side, 400f);

            // 4. Switch to dunk state and activate the score sensor

            State = "dunk";
            physicsRemoved = false;
            alleyOopPlayer = null;
            ResetScoring(true);

            // 5. Marking guarantees points (completed dunks must score)

            guaranteedDunkScore = completed;
            if (completed)
            {
                // 6. Pre-activate the upper sensor to avoid misjudgment of "first down and then up" during sub-step detection, resulting in no points.

                scoreArmedSide = side;
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            // 7. Set the pickup lock timer to prevent the ball from being grabbed immediately after dunking.

            pickupLockTimer = mlpObjectsData.DunkPickupLock;

            // 8. Display Basketball Wizard
            Show();
        }

        /// <summary>
        /// After stealing, knock the basketball away to give it speed in the direction of the steal.

        /// </summary>
        /// <param name="playerPosition">Player's current world coordinates</param>

        /// <param name="distanceFactor">Multiple based on tackling distance</param>

        /// <param name="direction">Movement or throwing direction (-1 or 1)</param>

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
        /// Estimate where the basketball will land on the ground by solving the parabolic trajectory equation for its landing time.
        /// </summary>
        /// <returns>Predicted landing X coordinate. </returns>
        public float PredictFloorLandingX()
        {
            // 1. If the basketball is not in the game, return directly to the current position (limited to the court)

            if (!IsInGame)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 2. If the basketball is already on the ground or below, return directly to the current position
            if (Position.y >= mlpObjectsData.BallFloorY)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 3. Calculate the acceleration due to gravity on the basketball

            var gravity = mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass;
            if (gravity <= 0.0001f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 4. Calculate the height difference between the basketball and the ground

            var floorDelta = mlpObjectsData.BallFloorY - Position.y;
            // 5. Use the parabola formula to find the discriminant (used to solve for landing time)

            var discriminant = Velocity.y * Velocity.y + 2f * gravity * floorDelta;
            if (discriminant <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 6. Find the time it takes for the basketball to hit the ground

            var timeToFloor = (-Velocity.y + Mathf.Sqrt(discriminant)) / gravity;
            if (timeToFloor <= 0f)
            {
                return Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);
            }

            // 7. Predict the X coordinate of landing based on horizontal speed and landing time

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
        /// After a shot is blocked, the basketball is bounced so that its direction is reversed relative to the person who blocked the shot.

        /// </summary>
        /// <param name="blocker">The player who blocks the shot</param>

        public void ApplyBlock(mlpPlayerObject blocker)
        {
            // 1. Determine the bounce direction based on the relative positions of the basketball and the shot blocker

            var direction = Position.x >= blocker.Position.x ? 1f : -1f;
            // 2. Set the basketball ownership to the side of the shot blocker

            Side = blocker.Side;
            // 3. Record the current position for physical calculations

            previousPosition = Position;
            // 4. Give the basketball a random bounce speed (towards the side and up)

            Velocity = new Vector2(
                direction * (280f + 100f * Random.value),
                -250f - 150f * Random.value);
            // 5. Set the basketball status to "Blocked"

            State = "block";
            // 6. The marked physical has not been removed

            physicsRemoved = false;
            // 7. Clear lob player references

            alleyOopPlayer = null;
            // 8. Keep the current shot activated so that the basketball can still pass through after a clean block.

            // The original basket was scored by the sensor chain.

            gameCore.MatchProcessor.Block(blocker.Side, blocker.IsHuman);
            // 9. Show Basketball

            Show();
            // 10. Update basketball visual position

            UpdateGraphic();
            // 11. Play the blocking sound effect

            mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
            // 12. Notify the system of blocking events

            gameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Block, blocker.Side, blocker.PlayerNo);
            // 13. Notify other players of basketball status changes

            gameCore.NotifyBallOthers();
        }

        /// <summary>
        /// Throw the basketball along the alley-oop arc to the designated receiving point, which is tracked by the alley-receiver player.

        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

        /// <param name="x">Horizontal coordinates in pixel space</param>

        /// <param name="y">Vertical coordinate in pixel space</param>

        /// <param name="player">Player object with lob</param>

        public void AlleyOop(int side, float x, float y, mlpPlayerObject player)
        {
            // 1. Set the basketball camp
            Side = side;
            // 2. Place the basketball in the starting position

            Position = new Vector2(x, y);
            // 3. Record the position of the previous frame

            previousPosition = Position;
            // 4. Record the X coordinate of the shooting position

            LastShotX = Position.x;
            // 5. Calculate the speed vector of the alley-oop arc (the target is the alley-oop ball point)

            Velocity = CalcVel(
                x,
                y,
                side == 1 ? mlpObjectsData.AlleyOopX : mlpConstants.Width - mlpObjectsData.AlleyOopX,
                mlpObjectsData.AlleyOopY,
                150f);
            // 6. Set the basketball status to "Alley-oop"

            State = "alleyOop";
            // 7. Record lob player references

            alleyOopPlayer = player;
            // 8. The marked physical has not been removed

            physicsRemoved = false;
            // 9. Reset score status

            ResetScoring(false);
            // 10. Show basketball

            Show();
            // 11. Mark the current air-connect status

            gameCore.IsAlleyOop = true;
        }

        /// <summary>
        /// Hides the basketball and stops its physics simulation, for use during cutscenes or scene transitions.

        /// </summary>
        public void RemoveFromPhysics()
        {
            physicsRemoved = true;
            gameCore.IsAlleyOop = false;
            graphic.SetActive(false);
            shadow.SetActive(false);
        }

        /// <summary>
        /// Re-enabled basketball physics and visibility after being removed.

        /// </summary>
        public void ReturnToPhysics()
        {
            physicsRemoved = false;
            gameCore.IsAlleyOop = false;
            Show();
        }

        /// <summary>
        /// Bounce the basketball away from the shield skill with a random bouncing effect.

        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

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
        /// Advance basketball physics every frame: apply gravity, perform sub-step collision detection on the ground/wall/basket/shield, and update sprite positions.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        /// <param name="basketLeft">Left basket object</param>

        /// <param name="basketRight">Right basket object</param>

        public void Update(float dt, mlpBasketObject basketLeft, mlpBasketObject basketRight)
        {
            // 1. Skip update when ball holding state or physics has been removed

            if (State == "inHands" || physicsRemoved)
            {
                return;
            }

            // 2. Decrement the pickup lock and collision sound effect cooling timer

            pickupLockTimer = Mathf.Max(0f, pickupLockTimer - dt);
            collisionSoundCooldown = Mathf.Max(0f, collisionSoundCooldown - dt);

            // 3. After the alley-oop arc reaches the highest point, notify the players to continue the alley-oop transmission.

            if (alleyOopPlayer != null && Velocity.y > 0f)
            {
                alleyOopPlayer.ContinueAlleyOop();
                alleyOopPlayer = null;
            }

            // 4. Determine the minimum number of sub-steps according to the basketball state (5 steps for dunking, 3 steps for shooting/bounce/block/alley-oop)

            var minSubsteps = 1;
            if (State == "dunk")
            {
                minSubsteps = 5;
            }
            else if (State == "shooting" || State == "basket" || State == "block" || State == "alleyOop")
            {
                minSubsteps = 3;
            }

            // 5. Calculate the number of sub-steps based on the speed to ensure that each step does not move more than 8 pixels (anti-penetration), with an upper limit of 8 steps

            var steps = Mathf.Clamp(
                Mathf.Max(minSubsteps, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(Velocity.x), Mathf.Abs(Velocity.y)) * dt / MaxSubstepTravel)),
                minSubsteps,
                MaxSubsteps);
            var stepDt = dt / steps;

            // 6. Advance physics step by step: gravity → movement → collision detection → score detection

            for (var i = 0; i < steps; i++)
            {
                // 6a. Record the previous frame position (for collision scanning and scoring sensor detection)

                previousPosition = Position;

                // 6b. Apply gravity and update position
                Velocity.y += mlpObjectsData.Gravity.y * mlpObjectsData.BallGravMass * stepDt;
                Position += Velocity * stepDt;

                // 6c. Detect player blocks

                gameCore.TryBlockBall();

                // 6d. Floor bounce and wall bounce

                ResolveWallBounce();
                ResolveFloorBounce();

                // 6e. Basket collision (backboard, rim, scoring sensor) and shield collision (not detected during alley-oop)

                if (State != "alleyOop")
                {
                    ResolveBasket(basketLeft, 1);
                    ResolveBasket(basketRight, -1);
                    gameCore.TryShieldBall();
                }

                // 6f. Guaranteed dunk score detection (directly determines a goal when the basketball is close to the basket)

                TryResolveGuaranteedDunkScore(basketLeft, basketRight);
            }

            // 7. Move the basketball and shadow sprite to the current physical location

            UpdateGraphic();
        }

        /// <summary>
        /// Causes the basketball to bounce off the ground and attenuate horizontal velocity.

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
        /// Make the basketball bounce off the left and right wall boundaries.

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
        /// Detects all collisions with one side of the basket: backboard, left rim, right rim and scoring sensor.

        /// </summary>
        /// <param name="basket">The basket object to detect collision</param>

        /// <param name="scoringSide">The side on which the basketball is scored when it passes through the hoop</param>

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
        /// When the basketball reaches the backboard glass area it reflects from the backboard glass plane.

        /// </summary>
        /// <param name="basket">The basket object to detect collision</param>

        private void ResolveBackboardCollision(mlpBasketObject basket)
        {
            // 1. Calculate the upper and lower boundaries of the backboard glass area

            var glassTop = basket.Height + mlpObjectsData.GlassY;
            var glassBottom = glassTop + mlpObjectsData.GlassHeight;
            // 2. If the basketball is not within the vertical range of the backboard glass, return directly

            if (Position.y + mlpObjectsData.BallRadius < glassTop || Position.y - mlpObjectsData.BallRadius > glassBottom)
            {
                return;
            }

            // 3. Handle the backboard collision on the left basket

            if (basket.Side == -1)
            {
                var planeX = mlpObjectsData.GlassWidth;
                // 4. Check whether the basketball hits the left backboard surface

                if (Velocity.x < 0f && Position.x - mlpObjectsData.BallRadius <= planeX)
                {
                    // 5. Push the basketball away from the backboard surface

                    Position.x = planeX + mlpObjectsData.BallRadius;
                    // 6. Rebound horizontal velocity and multiply by restitution coefficient

                    Velocity.x = Mathf.Abs(Velocity.x) * BackboardRestitution;
                    // 7. Slightly slow down the vertical speed to simulate friction

                    Velocity.y *= 0.97f;
                    // 8. Set the basketball to the basket area status

                    SetBasketState();
                    // 9. Play backboard collision sound effects

                    PlayBasketSound(2);
                }

                return;
            }

            // 10. Handle backboard collision on the right side of the basket

            var rightPlaneX = mlpConstants.Width - mlpObjectsData.GlassWidth;
            // 11. Check whether the basketball touches the backboard plane on the right side

            if (Velocity.x > 0f && Position.x + mlpObjectsData.BallRadius >= rightPlaneX)
            {
                // 12. Push the basketball away from the backboard surface

                Position.x = rightPlaneX - mlpObjectsData.BallRadius;
                // 13. Rebound horizontal velocity multiplied by restitution coefficient
                Velocity.x = -Mathf.Abs(Velocity.x) * BackboardRestitution;
                // 14. Slightly slow down the vertical speed to simulate friction

                Velocity.y *= 0.97f;
                // 15. Set basketball as basket area status

                SetBasketState();
                // 16. Play backboard collision sound effects

                PlayBasketSound(2);
            }
        }

        /// <summary>
        /// Push the basketball out of the hoop into a collision circle and apply an elastic recovery coefficient to the speed.

        /// </summary>
        /// <param name="rimCenter">The center of the basketball hoop collision circle in pixel space</param>

        /// <param name="basket">The basket object to detect collision</param>

        private void ResolveRimCollision(Vector2 rimCenter, mlpBasketObject basket)
        {
            // 1. Calculate the sum of the radii of the basketball and the hoop

            var combinedRadius = mlpObjectsData.BallRadius + mlpObjectsData.BasketPartRadius;
            // 2. Calculate the direction vector from the center of the basketball to the center of the hoop

            var offset = Position - rimCenter;
            // 3. Calculate the square of the distance (avoiding the square root operation)

            var distanceSquared = offset.sqrMagnitude;
            // 4. If the distance is greater than the sum of the two radii, there is no collision and returns directly

            if (distanceSquared >= combinedRadius * combinedRadius)
            {
                return;
            }

            // 5. Calculate the actual distance and collision normal direction

            var distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSquared));
            var normal = distanceSquared > 0.0001f
                ? offset / distance
                : new Vector2(Mathf.Sign(Position.x - rimCenter.x), -1f).normalized;
            if (normal.sqrMagnitude < 0.1f)
            {
                normal = Vector2.up;
            }
            // 6. Push the basketball to the edge of the hoop collision circle

            Position = rimCenter + normal * combinedRadius;

            // 7. Calculate the component of the basketball’s velocity in the direction of the collision normal

            var velocityIntoRim = Vector2.Dot(Velocity, normal);
            // 8. If the basketball is moving toward the inside of the hoop, bounce

            if (velocityIntoRim < 0f)
            {
                // 9. Bounce velocity along normal direction and apply elastic coefficient

                Velocity -= (1f + RimRestitution) * velocityIntoRim * normal;
                // 10. Slightly slow down the overall speed to simulate energy loss

                Velocity *= 0.985f;
            }

            // 11. Set basketball as basket area status

            SetBasketState();
            // 12. If the basketball is under the basket, the ring collision sound effect will be played.

            if (Position.y <= basket.Height - 2f)
            {
                PlayBasketSound(1);
            }
        }

        /// <summary>
        /// Detect whether the basketball passes through the upper and lower scoring sensors in the correct order to count as a goal.

        /// </summary>
        /// <param name="basket">The basket object to detect collision</param>

        /// <param name="scoringSide">The side on which the basketball is scored when it passes through the hoop</param>

        private void ProcessScoreSensors(mlpBasketObject basket, int scoringSide)
        {
            // 1. Check whether the basketball can currently be scored

            if (!canScore)
            {
                return;
            }

            // 2. If the scoring camp has been locked and is not the current camp, return directly

            if (scoreArmedSide != 0 && scoringSide != scoreArmedSide)
            {
                return;
            }

            // 3. Detect whether the basketball passes through the top of the scoring sensor (upper part)

            if (TouchesSensor(previousPosition, Position, basket.Center, basket.Height + mlpObjectsData.SensorUp))
            {
                gameCore.MatchProcessor.ProcessSensor(0);
            }

            // 4. Detect whether the basketball passes under the scoring sensor (lower part)

            if (!TouchesSensor(previousPosition, Position, basket.Center, basket.Height + mlpObjectsData.SensorDown))
            {
                return;
            }

            // 5. Notify the game processor that the sensor is triggered

            var matchProcessorReady = gameCore.MatchProcessor.ProcessSensor(1);
            // 6. If the game processor confirms that it is valid, or the dunk is guaranteed, or the tutorial guarantees the score, then confirm the score.
            if (matchProcessorReady || (guaranteedDunkScore && scoringSide == scoreArmedSide)
                || (tutorialGuaranteedScore && scoringSide == scoreArmedSide))
            {
                CommitScore(scoringSide);
            }
            else
            {
                // 7. Otherwise, this scoring attempt will be cancelled.

                CancelScoreAttempt();
            }
        }

        /// <summary>
        /// During the guaranteed dunk period, if the basketball is close enough to the center of the hoop, a point is scored immediately.

        /// </summary>
        /// <param name="basketLeft">Left basket object</param>

        /// <param name="basketRight">Right basket object</param>

        private void TryResolveGuaranteedDunkScore(mlpBasketObject basketLeft, mlpBasketObject basketRight)
        {
            // 1. Check whether you can score, whether it is a guaranteed dunk mode, and whether the scoring camp has been locked.

            if (!canScore || !guaranteedDunkScore || scoreArmedSide == 0)
            {
                return;
            }

            // 2. Select the corresponding basket according to the scoring camp

            var armedBasket = scoreArmedSide == 1 ? basketLeft : basketRight;
            if (armedBasket == null)
            {
                return;
            }

            // 3. Calculate the horizontal range of the scoring area below the basket (with additional tolerance)

            var minX = armedBasket.Center - mlpObjectsData.SensorHalf - mlpObjectsData.BallRadius - GuaranteedDunkScoreExtraX;
            var maxX = armedBasket.Center + mlpObjectsData.SensorHalf + mlpObjectsData.BallRadius + GuaranteedDunkScoreExtraX;
            // 4. Detect whether the basketball has just passed through the lower boundary of the scoring sensor

            var crossedDown = previousPosition.y <= armedBasket.Height + mlpObjectsData.SensorDown &&
                              Position.y >= armedBasket.Height + mlpObjectsData.SensorDown;
            // 5. If the basketball is within the scoring area (crosses the lower boundary or is below the sensor area), confirm the score

            if ((crossedDown || Position.y >= armedBasket.Height + mlpObjectsData.SensorDown + mlpObjectsData.SensorHeight) &&
                Position.x >= minX &&
                Position.x <= maxX)
            {
                CommitScore(scoreArmedSide);
            }
        }

        /// <summary>
        /// Marks the basketball as scored and notifies the game processor.

        /// </summary>
        /// <param name="scoringSide">The side on which the basketball is scored when it passes through the hoop</param>

        private void CommitScore(int scoringSide)
        {
            CancelScoreAttempt();
            State = "score";
            PlayBasketSound(0);
            gameCore.OnBallScored(scoringSide);
        }

        /// <summary>
        /// Resets all score tracking flags so the basketball can attempt its next shot.

        /// </summary>
        private void CancelScoreAttempt()
        {
            canScore = false;
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
        /// Tests whether the basketball sweep trajectory intersects the rectangular scoring sensor area.

        /// </summary>
        /// <param name="start">The starting position of the basketball trajectory</param>

        /// <param name="end">End position of basketball trajectory</param>
        /// <param name="centerX">Horizontal center of the scoring sensor</param>

        /// <param name="topY">Top Y coordinate of the scoring sensor</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        private static bool TouchesSensor(Vector2 start, Vector2 end, float centerX, float topY)
        {
            var minX = centerX - mlpObjectsData.SensorHalf - mlpObjectsData.BallRadius;
            var maxX = centerX + mlpObjectsData.SensorHalf + mlpObjectsData.BallRadius;
            var minY = topY - mlpObjectsData.BallRadius;
            var maxY = topY + mlpObjectsData.SensorHeight + mlpObjectsData.BallRadius;
            return SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY);
        }

        /// <summary>
        /// Use the Cohen-Sutherland segment clipping algorithm to detect whether a moving point crosses a rectangle.

        /// </summary>
        /// <param name="start">The starting position of the trajectory</param>

        /// <param name="end">The end position of the trajectory</param>

        /// <param name="minX">Test the left edge of the rectangle</param>
        /// <param name="maxX">Test the right edge of the rectangle</param>

        /// <param name="minY">Test the lower boundary of the rectangle</param>

        /// <param name="maxY">Test the upper boundary of the rectangle</param>
        /// <returns>Returns true when the line segment intersects the rectangle; otherwise returns false. </returns>
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
        /// Returns true when a 2D point is inside the given axis-aligned rectangle.

        /// </summary>
        /// <param name="point">Two-dimensional point to be detected</param>
        /// <param name="minX">Test the left edge of the rectangle</param>
        /// <param name="maxX">Test the right edge of the rectangle</param>

        /// <param name="minY">Test the lower boundary of the rectangle</param>

        /// <param name="maxY">Test the upper boundary of the rectangle</param>
        /// <returns>Returns true if the point is inside the rectangle; otherwise returns false. </returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// Slab boundary clipping with Cohen-Sutherland clipping of one component of the line segment.

        /// </summary>
        /// <param name="p">Direction component of clipping plate</param>
        /// <param name="q">Distance component of clipping plate</param>

        /// <param name="tMin">Current minimum parameter clipping value</param>

        /// <param name="tMax">Current maximum parameter clipping value</param>
        /// <returns>Returns true if the line segment is not completely clipped; otherwise returns false. </returns>
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
        /// Toggles basketball status to "basket" unless already scoring.

        /// </summary>
        private void SetBasketState()
        {
            if (State != "score")
            {
                State = "basket";
            }
        }

        /// <summary>
        /// Play the corresponding basket sound effects (the sound of the net, the sound of the rim impact or the sound of the backboard), with a cooling time for rapid collisions.
        /// </summary>
        /// <param name="type">Collision sound effect type (0=net sound, 1=rim impact, 2=backboard)</param>

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
        /// Clears all score activation flags and optionally activates the score sensor for the next shot.

        /// </summary>
        /// <param name="armed">When true, activate the scoring sensor to prepare for the next shot</param>
        private void ResetScoring(bool armed)
        {
            canScore = armed;
            guaranteedDunkScore = false;
            scoreArmedSide = 0;
            collisionSoundCooldown = 0f;
        }

        /// <summary>
        /// Calculate the launch speed required to throw a basketball parabola from a given position to the target basket.
        /// </summary>
        /// <param name="x">Horizontal coordinates in pixel space</param>

        /// <param name="y">Vertical coordinate in pixel space</param>

        /// <param name="offset">Horizontal offset superimposed to the center of the basket</param>
        /// <returns>The calculated launch velocity vector. </returns>
        private Vector2 CalcThrowVel(float x, float y, float offset)
        {
            // 1. Determine the target basket position according to the basketball team.

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

            // 2. Select the parabolic arc height based on the distance to the basket

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

            // 3. In non-tutorial mode, add a little random variation to the arc height to simulate the feel.
            if (!tutorialGuaranteedScore)
            {
                arc *= 1f + 0.1f * (Random.value <= 0.5f ? -1f : 1f) * Random.value;
            }
            // 4. Limit the arc height to not exceed the maximum value

            arc = Mathf.Min(arc, 185f);
            // 5. Calculate the hand speed vector using the parabolic formula

            return CalcVel(x, y, targetX, mlpObjectsData.BasketHeight, arc);
        }

        /// <summary>
        /// Solve for the parabolic trajectory that yields the velocity from (x,y) to the target at the given arc height.

        /// </summary>
        /// <param name="x">Horizontal coordinates in pixel space</param>

        /// <param name="y">Vertical coordinate in pixel space</param>

        /// <param name="targetX">The horizontal position of the target</param>

        /// <param name="targetY">Vertical position of target</param>

        /// <param name="arc">The highest arc height above the release point</param>
        /// <returns>The calculated velocity vector. </returns>
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
        /// Shot offset values ​​are randomly generated based on distance, height, running speed and player accuracy.

        /// </summary>
        /// <param name="distance">Estimated distance to target basket</param>
        /// <param name="y">Vertical coordinate in pixel space</param>

        /// <param name="running">Additional offset caused by player movement speed</param>
        /// <param name="accuracy">Shooting accuracy correction value (the lower, the more accurate)</param>

        /// <returns>The calculated offset coefficient. </returns>
        private float CalcDispersion(float distance, float y, float running, float accuracy)
        {
            // 1. If the accuracy is extremely high (negative value), a perfect hit will be returned directly
            if (accuracy <= -0.5f)
            {
                return 1f;
            }

            // 2. Calculate the vertical offset based on the release height

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

            // 3. Find the corresponding dispersion level based on the distance to the basket

            float distanceDispersion =
                distance <= 100f ? 0f :
                distance <= 200f ? 0.01f :
                distance <= 300f ? 0.02f :
                distance <= 400f ? 0.03f :
                distance <= 490f ? 0.04f :
                distance <= 540f ? 0.01f : 0.07f;

            // 4. Randomly select the offset direction (left or right) and calculate the total offset value

            var sign = Random.value < 0.5f ? -1f : 1f;
            var value = sign * (mlpObjectsData.Dispersion + vertical + distanceDispersion + accuracy + running) * Random.value;
            // 5. If the offset is small, it is considered a perfect hit

            if (Mathf.Abs(value) <= 0.02f)
            {
                return 1f;
            }

            // 6. The offset is too large (left), and the special value 2 is returned to indicate a serious left deviation.

            if (value < -0.08f)
            {
                return 2f;
            }

            // 7. The offset is too large (to the right), and the special value 3 is returned to indicate a serious deviation to the right.
            if (value > 0.08f)
            {
                return 3f;
            }

            // 8. Normal offset range, return 1+offset value as speed scaling factor

            return 1f + value;
        }

        /// <summary>
        /// The marked basketball becomes visible on the next graphics update.

        /// </summary>
        private void Show()
        {
            visibleNextFrame = true;
        }

        /// <summary>
        /// Select the theme basketball sprite (if available), otherwise fall back to the default gallery basketball frame.
        /// </summary>
        /// <returns>The parsed basketball sprite. </returns>
        private Sprite ResolveBallSprite()
        {
            return mlpGameplaySpriteLoader.LoadMatchBallSprite(gameCore.MatchData.BallTheme, 0.5f, 0.5f);
        }

        /// <summary>
        /// Moves the basketball and shadow GameObject to the current physical location every frame.

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
    /// Teleportation effects: visual effects played when a character uses teleportation skills, including flashing, disappearing and appearing animations.
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

        // Black expansion phase duration.

        private const float BlackExpandDuration = 0.06f;
        // Black contraction phase duration.

        private const float BlackCollapseDuration = 0.07f;
        // The duration of the zoom transition when black shrinks.

        private const float BlackCollapseScaleDuration = 0.08f;
        // The first white flash duration.

        private const float WhiteFlashDuration1 = 0.03f;
        // The second white flash duration.

        private const float WhiteFlashDuration2 = 0.03f;
        // The third duration of white flash.
        private const float WhiteFlashDuration3 = 0.024f;
        // Transfer animation playback frame rate.

        private const float AnimationFps = 30f;

        // Transport special effects root node.

        private readonly GameObject graphic;
        // Black expanded circle point.

        private readonly Transform blackNode;
        // Black expanding ring renderer.

        private readonly SpriteRenderer blackRenderer;
        // Center dot renderer.

        private readonly SpriteRenderer centerRenderer;
        // White glitter layer renderer.

        private readonly SpriteRenderer whiteRenderer;
        // Deliver animation frames to the renderer.

        private readonly SpriteRenderer animRenderer;
        // Transfers an array of animation frames.

        private readonly Sprite[] frames;
        // The skill definition of the corresponding character is used to switch the special effects theme.

        private readonly mlpCharacterSkillDefinition skillDefinition;
        // The current special effects stage.

        private TeleportPhase phase = TeleportPhase.Hidden;
        // The elapsed time of the current stage.

        private float phaseTime;

        /// <summary>
        /// Build teleportation visual effects, including black extensions, center points, animated frames, and white flashes.

        /// </summary>
        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        public mlpTeleportFx(Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            this.skillDefinition = skillDefinition;

            // 1. Create the root GameObject that transmits special effects and hang it under the parent node

            graphic = new GameObject("TeleportFx");
            graphic.transform.SetParent(parent, false);

            // 2. Create a black expansion ring: used for the black diffusion effect at the beginning of the transmission

            blackNode = new GameObject("TeleportBlack").transform;
            blackNode.SetParent(graphic.transform, false);
            blackRenderer = blackNode.gameObject.AddComponent<SpriteRenderer>();
            blackRenderer.sortingOrder = 74;
            blackRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport10000");

            // 3. Create a central white point: hang inside the black ring as the visual center of the transmission

            var centerNode = new GameObject("TeleportCenter");
            centerNode.transform.SetParent(blackNode, false);
            centerRenderer = centerNode.AddComponent<SpriteRenderer>();
            centerRenderer.sortingOrder = 75;
            centerRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport20000");

            // 4. Create animation frame player: used to play frame-by-frame animation during transmission

            var animNode = new GameObject("TeleportAnim");
            animNode.transform.SetParent(graphic.transform, false);
            animRenderer = animNode.AddComponent<SpriteRenderer>();
            animRenderer.sortingOrder = 76;

            // 5. Create a white flash layer: the white flash effect at the end of the transfer

            var whiteNode = new GameObject("TeleportWhite");
            whiteNode.transform.SetParent(graphic.transform, false);
            whiteRenderer = whiteNode.AddComponent<SpriteRenderer>();
            whiteRenderer.sortingOrder = 77;
            whiteRenderer.sprite = mlpAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            // 6. Load the 4-frame sprite of the teleportation animation

            frames = new[]
            {
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30000"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30001"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30002"),
                mlpAtlasCache.Instance.SkillFx.Sprite("teleport30003")
            };

            // 7. Adjust the color of the special effects according to the color of the character's skills and then hide it

            ApplyTheme();
            Hide();
        }

        /// <summary>
        /// Begins the teleportation animation sequence at the specified world coordinates.

        /// </summary>
        /// <param name="x">Horizontal coordinates in pixel space</param>

        /// <param name="y">Vertical coordinate in pixel space</param>

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
        /// Each frame advances the state machine of the transport animation.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public void Update(float dt)
        {
            // 1. If the special effect is hidden, return directly

            if (phase == TeleportPhase.Hidden)
            {
                return;
            }

            // 2. Accumulate the running time of the current stage

            phaseTime += dt;
            // 3. Execute the corresponding animation update according to the current stage
            switch (phase)
            {
                case TeleportPhase.BlackExpand:
                    // 4. Update the black ring expansion animation

                    UpdateBlackExpand();
                    // 5. After the expansion time is over, switch to the contraction phase

                    if (phaseTime >= BlackExpandDuration)
                    {
                        phase = TeleportPhase.BlackCollapse;
                        phaseTime = 0f;
                        animRenderer.enabled = true;
                    }
                    break;
                case TeleportPhase.BlackCollapse:
                    // 6. Update the black ring shrinking animation

                    UpdateBlackCollapse();
                    // 7. After the contraction time is over, switch to the white flash stage

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
                    // 8. Update white flash animation

                    UpdateWhiteFlash();
                    // 9. Hide the entire special effect after the flash ends

                    if (phaseTime >= WhiteFlashDuration1 + WhiteFlashDuration2 + WhiteFlashDuration3)
                    {
                        Hide();
                    }
                    break;
            }
        }

        /// <summary>
        /// Immediately stops delivering effects and hides all renderers.

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
        /// Expand the black circle from small to medium in the first phase of teleportation.

        /// </summary>
        private void UpdateBlackExpand()
        {
            var t = Mathf.Clamp01(phaseTime / BlackExpandDuration);
            var scale = Mathf.Lerp(0.1f, 0.78f, t);
            blackNode.localScale = new Vector3(scale, scale, 1f);
            blackNode.localRotation = Quaternion.Euler(0f, 0f, -180f * t);
        }

        /// <summary>
        /// Shrink the black circle while rotating, and then display the animation frame.

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
        /// Plays a three-stage white flash: first expanding and then contracting until it disappears.

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
        /// Apply an ease-in-back curve with a slight overshoot to produce a springy animation effect.

        /// </summary>
        /// <param name="t">Normalized progress value (0 to 1)</param>
        /// <returns>The calculated easing value. </returns>
        private static float EaseInBack(float t)
        {
            const float overshoot = 1.70158f;
            return (overshoot + 1f) * t * t * t - overshoot * t * t;
        }

        private void ApplyTheme()
        {
            // 1. Set the black ring to a darker blend color of the character’s secondary color

            blackRenderer.color = Color.Lerp(skillDefinition.SecondaryColor, Color.black, 0.22f);
            // 2. Use the main color of the character for the central white point
            centerRenderer.color = skillDefinition.PrimaryColor;
            // 3. Use a mix of white and accent colors for the white glitter layer

            whiteRenderer.color = Color.Lerp(Color.white, skillDefinition.AccentColor, 0.42f);
            // 4. Use a mixture of main and accent colors for the animation frame layer.

            animRenderer.color = Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.AccentColor, 0.38f);
        }
    }

    /// <summary>
    /// Shield special effect object: The shield displayed when the character activates the shield skill, which can deflect flying basketballs.
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

        // Total shield admission time.

        private const float IntroTime = 0.14f;
        // Shield down duration.

        private const float IntroDropTime = 0.12f;
        // Initial drop offset on shield entry.

        private const float IntroDropOffsetY = -600f;
        // Horizontal scaling of the admission blur layer.

        private const float IntroBlurScaleX = 1.08f;
        // Vertical scaling of the admission blur layer.

        private const float IntroBlurScaleY = 1.16f;
        // Shield display dwell time.
        private const float ShowTime = 3f;
        // Shield fade time.

        private const float FadeTime = 0.5f;
        // Animation playback frame rate.

        private const float AnimationFps = 30f;
        // The X offset of the shield graphic.

        private const float GraphicXOffset = 23f;
        // Y offset of the shield graphic.

        private const float GraphicYOffset = -62f;
        // The top position of the collision rectangle.

        private const float CollisionRectTop = 30f;
        // Collision rectangle width.

        private const float CollisionRectWidth = 70f;
        // The height of the collision rectangle.

        private const float CollisionRectHeight = 10f;
        // The left boundary of the collision rectangle corresponding to the left basket is offset.

        private const float CollisionRectLeftLeftSide = -23f;
        // The left boundary of the collision rectangle corresponding to the right basket is offset.

        private const float CollisionRectLeftRightSide = -49f;
        // The local X offset of the starting tip sprite.

        private const float StartSpriteLocalX = 1f;

        // Court side, -1 left, 1 right.

        private readonly int side;
        // The associated basket object, used for collision detection.

        private readonly mlpBasketObject basket;
        // Shield root node.

        private readonly GameObject graphic;
        // Blur layer renderer.

        private readonly SpriteRenderer blurRenderer;
        // Start prompt renderer.

        private readonly SpriteRenderer startRenderer;
        // Animation frame renderer.

        private readonly SpriteRenderer animRenderer;
        // Array of shield animation frames.

        private readonly Sprite[] frames;
        // The skill definition of the corresponding character is used to switch the shield theme.

        private readonly mlpCharacterSkillDefinition skillDefinition;

        // Current shield stage.

        private ShieldPhase phase = ShieldPhase.Hidden;
        // The elapsed time of the current stage.

        private float phaseTime;
        // Current shield transparency.

        private float alpha = 1f;

        /// <summary>
        /// Create a shield skill VFX on one side of the pitch.

        /// </summary>
        /// <param name="side">Court side (-1 is left, 1 is right)</param>

        /// <param name="basket">The basket object to detect collision</param>

        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        public mlpShieldObject(int side, mlpBasketObject basket, Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            this.side = side;
            this.basket = basket;
            this.skillDefinition = skillDefinition;

            // 1. Create a shield root node and name it ShieldLeft or ShieldRight according to the side it is on.

            graphic = new GameObject(side == -1 ? "ShieldLeft" : "ShieldRight");
            graphic.transform.SetParent(parent, false);

            // 2. Create a three-layer renderer: starting frame (still), blur frame (entrance animation), frame-by-frame animation layer

            var shieldStartSprite = mlpAtlasCache.Instance.SkillFx.Sprite("ShieldMC0000");
            startRenderer = CreateRenderer("ShieldStart", 63, shieldStartSprite);
            blurRenderer = CreateRenderer("ShieldBlur", 64, shieldStartSprite);
            animRenderer = CreateRenderer("ShieldAnim", 65, null);

            // 3. Load 21 frames of sprites for the shield animation (expand animation played frame by frame)

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

            // 4. Initial hiding, applying character color theme
            graphic.SetActive(false);
            ApplyTheme();
        }

        public bool IsBlocking => phase == ShieldPhase.Active && phaseTime < AnimationDuration + ShowTime;
        public bool CanActivate => phase == ShieldPhase.Hidden;

        /// <summary>
        /// Start the shield entry animation: slide in the fuzzy sprite from above and play the shield sound effect.

        /// </summary>
        public void Activate()
        {
            // 1. Set the shield to the entry stage and reset the timer

            phase = ShieldPhase.Intro;
            phaseTime = 0f;
            alpha = 1f;
            // 2. Hide the ear pattern on the front edge of the basket to make room for the shield

            basket?.HideEar();
            // 3. Display the shield root node

            graphic.SetActive(true);
            // 4. Only the blur layer is displayed when entering, and other layers are hidden.

            startRenderer.enabled = false;
            blurRenderer.enabled = true;
            animRenderer.enabled = false;
            // 5. Place the blur sprite at the starting position (top) and set the magnification ratio

            blurRenderer.transform.localPosition = new Vector3(StartSpriteLocalX, IntroDropOffsetY, 0f);
            blurRenderer.transform.localScale = new Vector3(IntroBlurScaleX, IntroBlurScaleY, 1f);
            startRenderer.transform.localScale = Vector3.one;
            animRenderer.transform.localScale = Vector3.one;
            // 6. Apply transparency and update position

            ApplyAlpha();
            UpdateGraphic();
            // 7. Play the shield activation sound effect

            mlpAudio.Instance?.Play(mlpAssets.Sounds.PShield);
        }

        /// <summary>
        /// Each frame advances the shield's entry, activation, and deactivation phases.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public void Update(float dt)
        {
            // 1. If the shield is hidden, return directly

            if (phase == ShieldPhase.Hidden)
            {
                return;
            }

            // 2. Accumulate the running time of the current stage

            phaseTime += dt;
            // 3. Execute the corresponding animation update according to the current stage
            switch (phase)
            {
                case ShieldPhase.Intro:
                    // 4. Update the entrance animation (fuzzy sprite falling from above)

                    UpdateIntro();
                    // 5. After the admission time ends, switch to the activation stage
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
                    // 6. Update the activation phase animation (shield expansion animation playback)

                    UpdateActive();
                    // 7. After the animation playback and display time ends, switch to the fading stage

                    if (phaseTime >= AnimationDuration + ShowTime)
                    {
                        phase = ShieldPhase.Fading;
                        phaseTime = 0f;
                        animRenderer.enabled = false;
                        basket?.ShowEar();
                    }
                    break;
                case ShieldPhase.Fading:
                    // 8. Calculate fading transparency and apply

                    alpha = 1f - Mathf.Clamp01(phaseTime / FadeTime);
                    ApplyAlpha();
                    // 9. Reset the shield after fading is completed

                    if (phaseTime >= FadeTime)
                    {
                        Reset();
                        return;
                    }
                    break;
            }

            // 10. Update shield graphic position

            UpdateGraphic();
        }

        /// <summary>
        /// Instantly hides the shield and restores the front lug graphics on the rim.
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
        /// Checks if the basketball overlaps the shield collision rectangle and bounces it if so.
        /// </summary>
        /// <param name="ball">The basketball object to be detected or affected</param>
        /// <returns>Returns true if the basketball is successfully blocked; otherwise returns false. </returns>
        public bool TryBlockBall(mlpBallObject ball)
        {
            // 1. Check whether the shield is activated, whether the basketball exists, and whether the basketball is scoring.

            if (!IsBlocking || ball == null || ball.State == "score")
            {
                return false;
            }

            // 2. Get the origin position of the shield

            var origin = ShieldOrigin;
            // 3. Select the left margin of the collision rectangle according to the side it is on.
            var rectLeft = side == -1 ? CollisionRectLeftLeftSide : CollisionRectLeftRightSide;
            // 4. Calculate the four boundaries of the collision rectangle (considering the expansion of the basketball radius)

            var minX = origin.x + rectLeft - mlpObjectsData.BallRadius;
            var maxX = minX + CollisionRectWidth + mlpObjectsData.BallRadius * 2f;
            var minY = origin.y + CollisionRectTop - mlpObjectsData.BallRadius;
            var maxY = minY + CollisionRectHeight + mlpObjectsData.BallRadius * 2f;
            // 5. Use sweep detection to determine whether the basketball trajectory passes through the shield collision rectangle

            if (!SweptPointIntersectsRect(ball.PreviousPosition, ball.Position, minX, maxX, minY, maxY))
            {
                return false;
            }

            // 6. Trigger the effect of the basketball being bounced by the shield

            ball.OnShieldCollision(side);
            return true;
        }

        /// <summary>
        /// Each frame positions the shield graphic to the origin of the basket and flips it horizontally based on the side it is on.

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
        /// Use an ease-out-back curve to animate the blurred sprite falling from above.

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
        /// Loops the shield animation frames at the configured frame rate.
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
        /// Colorizes all shield renderers using the current fade transparency and skill theme colors.

        /// </summary>
        private void ApplyAlpha()
        {
            startRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.AccentColor, 0.3f), alpha);
            blurRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, Color.white, 0.18f), alpha * 0.85f);
            animRenderer.color = WithAlpha(Color.Lerp(skillDefinition.PrimaryColor, skillDefinition.SecondaryColor, 0.22f), alpha);
        }

        /// <summary>
        /// Creates a child GameObject with a SpriteRenderer, using the specified sorting level.

        /// </summary>
        /// <param name="name">The name of the child GameObject</param>
        /// <param name="sortingOrder">The wizard sorting level determines the drawing priority</param>
        /// <param name="sprite">Pixel size driven scaling of sprites</param>

        /// <returns>The created SpriteRenderer component. </returns>
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
        /// Apply an ease-out-back curve with a slight overshoot to produce a springy animation effect.
        /// </summary>
        /// <param name="t">Normalized progress value (0 to 1)</param>
        /// <returns>The calculated easing value. </returns>
        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            var x = t - 1f;
            return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
        }

        private void ApplyTheme()
        {
            // 1. Apply current transparency to all shield renderers

            ApplyAlpha();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// <summary>
        /// Use the Cohen-Sutherland segment clipping algorithm to detect whether a moving point crosses a rectangle.

        /// </summary>
        /// <param name="start">The starting position of the trajectory</param>

        /// <param name="end">The end position of the trajectory</param>

        /// <param name="minX">Test the left edge of the rectangle</param>
        /// <param name="maxX">Test the right edge of the rectangle</param>

        /// <param name="minY">Test the lower boundary of the rectangle</param>

        /// <param name="maxY">Test the upper boundary of the rectangle</param>
        /// <returns>Returns true when the line segment intersects the rectangle; otherwise returns false. </returns>
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
        /// Returns true when a 2D point is inside the given axis-aligned rectangle.

        /// </summary>
        /// <param name="point">Two-dimensional point to be detected</param>
        /// <param name="minX">Test the left edge of the rectangle</param>
        /// <param name="maxX">Test the right edge of the rectangle</param>

        /// <param name="minY">Test the lower boundary of the rectangle</param>

        /// <param name="maxY">Test the upper boundary of the rectangle</param>
        /// <returns>Returns true if the point is inside the rectangle; otherwise returns false. </returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// Slab boundary clipping with Cohen-Sutherland clipping of one component of the line segment.

        /// </summary>
        /// <param name="p">Direction component of clipping plate</param>
        /// <param name="q">Distance component of clipping plate</param>

        /// <param name="tMin">Current minimum parameter clipping value</param>

        /// <param name="tMax">Current maximum parameter clipping value</param>
        /// <returns>Returns true if the line segment is not completely clipped; otherwise returns false. </returns>
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
    /// Character skill special effects: visual special effects (flash, particles, icons, etc.) played when the character activates exclusive skills.

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

        // Skill special effects root node.

        private readonly GameObject root;
        // Outer glow renderer.

        private readonly SpriteRenderer glowRenderer;
        // Core renderer.

        private readonly SpriteRenderer coreRenderer;
        // Accent color renderer.

        private readonly SpriteRenderer accentRenderer;
        // Basic skill definition, used to restore the default theme.

        private readonly mlpCharacterSkillDefinition baseSkillDefinition;
        // The currently active skill definition.

        private mlpCharacterSkillDefinition skillDefinition;
        // Current special effects mode.

        private FxMode mode = FxMode.Hidden;
        // The current running time of the special effect.

        private float timer;
        // The current effect duration.

        private float duration;

        public mlpPlayerSkillFx(Transform parent, mlpCharacterSkillDefinition skillDefinition)
        {
            // 1. Save skill definition (basic definition and current definition, for restoration after switching)
            baseSkillDefinition = skillDefinition;
            this.skillDefinition = skillDefinition;
            DBLiteFactory.Instance.EnsureLoaded();

            // 2. Create a special effect root node and hang it under the parent node

            root = new GameObject("PlayerSkillFx");
            root.transform.SetParent(parent, false);

            // 3. Create a three-layer renderer: halo background, core icon, accent color decoration

            glowRenderer = CreateRenderer("Glow", 17, mlpAtlasCache.Instance.Interface.Sprite("EmblemsBg0000"));
            coreRenderer = CreateRenderer("Core", 18, null);
            accentRenderer = CreateRenderer("Accent", 19, null);

            // 4. Set the color and icon according to the character's skills, and then stop playing (hidden state)

            ApplyTheme(skillDefinition);
            Stop();
        }

        public void ApplyTheme(mlpCharacterSkillDefinition definition)
        {
            // 1. Save skill definition reference

            skillDefinition = definition;
            // 2. Check whether custom special effects art resources are used

            var useCustomArt = UsesCustomFxArt(definition.SkillType);
            // 3. Load core icons and sprites that emphasize decoration

            coreRenderer.sprite = LoadSkillSprite(definition.SkillType, false);
            accentRenderer.sprite = LoadSkillSprite(definition.SkillType, true);
            // 4. Set the halo background color (use lighter transparency for custom art)

            glowRenderer.color = WithAlpha(definition.PrimaryColor, useCustomArt ? 0.14f : 0.18f);
            // 5. Set core icon color

            coreRenderer.color = useCustomArt
                ? WithAlpha(Color.white, 0.72f)
                : WithAlpha(definition.PrimaryColor, 0.5f);
            // 6. Set accent colors

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
            // 1. If the special effect is hidden, turn off the display and return

            if (mode == FxMode.Hidden)
            {
                root.SetActive(false);
                return;
            }

            // 2. Accumulation timer, if the duration exceeds, the special effect will be stopped.

            timer += dt;
            if (timer >= duration)
            {
                Stop();
                return;
            }

            // 3. Check if it should render (character is visible or sprinting)
            var shouldRender = visible || mode == FxMode.Dash;
            if (!shouldRender)
            {
                root.SetActive(false);
                return;
            }

            // 4. Position the special effect above the character and flip it according to the direction.

            mlpRender.ApplyPixelTransform(root.transform, position.x, position.y + 30f, 0.08f, 1f);
            var rootScale = root.transform.localScale;
            rootScale.x = Mathf.Abs(rootScale.x) * Mathf.Sign(facingDirection);
            root.transform.localScale = rootScale;

            // 5. Calculate animation progress ratio and reset renderer layout

            var t = timer / duration;
            ResetRendererLayout();

            // 6. If the skill uses custom special effects art, use separate update logic.

            if (UsesCustomFxArt(skillDefinition.SkillType))
            {
                UpdateCustomFx(t);
                root.SetActive(true);
                return;
            }

            // 7. Update the size, color and rotation of the three-layer renderer according to the special effects mode

            switch (mode)
            {
                case FxMode.Buff:
                {
                    // 8. Gain mode: breathing pulse animation on all three layers

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
                    // 9. Burst mode: three layers grow from small to large and gradually fade out
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
                    // 9. Sprint mode: horizontal stretching effect, widest in the middle and narrow at both ends
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

            // 10. Display special effects root node
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
    /// Player object: Manage all the status of a player - movement, jumping, shooting, dunking, defense, skills, animation, AI control, etc. It is the most important game object in the game.
    /// </summary>
    public sealed class mlpPlayerObject
    {
        // The mass of the player used to push away in proportion to their mass during ground collision.

        private const float GroundCollisionMass = 3f;
        // Larger virtual mass to use when ground blocks collide.

        private const float GroundBlockCollisionMass = 6f;
        // Ground collision speed threshold. Below this value, the vehicle is considered stationary.
        private const float GroundCollisionSpeedEpsilon = 5f;
        // The base rendering depth of the player's main graphic.

        private const float GraphicDepthBase = 0.12f;
        // Base rendering depth of player shadows.

        private const float ShadowDepthBase = 0.02f;
        // Rendering depth steps between different players on the same team.

        private const float TeamDepthStep = 0.01f;
        // Subtle rendering depth steps between different player numbers within the same team.

        private const float PlayerDepthStep = 0.0025f;
        // Scaling factor for shadow rendering depth offset.

        private const float ShadowDepthBiasScale = 0.25f;
        // Catch X tolerance range for tutorial dunks.

        private const float TutorialPutbackCatchWindowX = 190f;
        // Catch Y tolerance range for tutorial padding.

        private const float TutorialPutbackCatchWindowY = 230f;
        // Tutorial back-up dunks allow extra Y height for dunks.

        private const float TutorialPutbackDunkYBonus = 96f;
        // The minimum height of the basketball required for the tutorial to make up for dunks.

        private const float TutorialPutbackMinBallY = mlpObjectsData.BasketHeight + 22f;
        // The maximum vertical speed of the basketball required for tutorial make-up dunks.

        private const float TutorialPutbackMaxBallVelocityY = 560f;
        // The default success rate of tutorial compensation deduction.

        private const float TutorialPutbackCompletionChance = 1f;
        // Default duration for backboard magnets.

        private const float ReboundMagnetDefaultDuration = 1.55f;
        // The horizontal adsorption distance of the backboard magnet.

        private const float ReboundMagnetCatchDistanceX = 52f;
        // The vertical adsorption distance of the backboard magnet.

        private const float ReboundMagnetCatchDistanceY = 72f;
        // The minimum speed required for a backboard magnet to attract the ball.

        private const float ReboundMagnetMinSpeed = 560f;
        // The maximum speed of the backboard magnet to attract the ball.

        private const float ReboundMagnetMaxSpeed = 920f;
        // Hover duration after a certain block.

        private const float GuaranteedBlockHoldDuration = 0.22f;
        // The horizontal offset of the character's position when blocking a shot.

        private const float GuaranteedBlockHorizontalOffset = 20f;
        // Vertical offset of the hand collision point when blocking a shot.

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

        // The player's main visual root node.

        private readonly GameObject graphic;
        // Player shadow root node.

        private readonly GameObject shadow;
        // Player shadow renderer.

        private readonly SpriteRenderer shadowRenderer;
        // Default shadow sprite.

        private readonly Sprite defaultShadowSprite;
        // The shadow sprite used when the skill is activated.
        private readonly Sprite activeSkillShadowSprite;
        // Player skeleton animation object.

        private readonly DBLiteArmature armature;
        // Player controller interface.

        private readonly IBLPlayerController controller;
        // Team index.

        private readonly int teamIndex;
        // Role ID.

        private readonly int characterId;
        // Team player number.

        private readonly int playerNo;
        // Rendering depth offset for staggered display of players on the same team.

        private readonly float renderDepthBias;
        // Skill level.

        private readonly int skillLevel;
        // Role skill definitions.

        private readonly mlpCharacterSkillDefinition skillDefinition;
        // Superpower ID.

        private readonly int superId;
        // AI difficulty slots.

        private readonly int brainSlot;
        // AI difficulty parameter adjustment configuration.

        private readonly mlpAIDifficultyTuningProfile aiDifficultyTuning;
        // Whether to strengthen the AI ​​for Hell difficulty.

        private readonly bool hellEnhanced;
        // Basis value of shooting accuracy.

        private readonly float accuracy;
        // Basic dunk success rate.

        private readonly float chanceToCompleteDunk;
        // Super power cooldown time.

        private readonly float superCoolDown;
        // Super Dunk's target X coordinate.

        private readonly float superDunkX;
        // The X coordinate of the end of the super dunk landing.

        private readonly float superDunkEndX;
        // The Y coordinate of the end of the super dunk landing.

        private readonly float superDunkEndY;
        // X-coordinates of the two targets for Super Sprint.

        private readonly float[] superDashTargets = new float[2];
        // Sprint input/cooldown delay controller.

        private readonly UseDelay dashDelay;
        // Energy bar UI view.

        private readonly mlpEnergyBarView energyBar;
        // Send special effects objects.

        private readonly mlpTeleportFx teleportFx;
        // Shield effect object.

        private readonly mlpShieldObject shield;
        // Skill special effect object.
        private readonly mlpPlayerSkillFx skillFx;
        // Extra cooldown for Hyper Sprint on Hell difficulty.

        private readonly float hellBonusSuperDashCooldownDuration;
        // Additional cooldown for shields on Hell difficulty.

        private readonly float hellBonusShieldCooldownDuration;
        // A collection of opponent numbers that have been hit during the super sprint.

        private readonly HashSet<int> superDashHits = new HashSet<int>();
        // Operation lock timer.

        private float actionLatch;
        // The name of the current animation state.

        private string visualState = "";
        // Sprint timer.

        private float dashTimer;
        // Current sprint direction.

        private int dashDirection;
        // The cached sprint direction.

        private int bufferedDashDirection;
        // Sprint input buffer timer.

        private float dashBufferTimer;
        // Are you ready to launch a sprint?

        private bool readyForDash;
        // Whether to allow normal actions.

        private bool canDoAction;
        // Whether there is a ground shot to be executed.

        private bool pendingGroundThrow;
        // Whether there is a steal action to be performed.

        private bool pendingStealAction;
        // Whether the tackling animation is playing.

        private bool stealAnimationActive;
        // Whether the alley-oop is waiting to be released.

        private bool alleyOopPendingThrow;
        // Is it in dunk mode?

        private bool isDunking;
        // Whether dunk basketball has been released.

        private bool dunkReleased;
        // Dunk timer.

        private float dunkTimer;
        // Total dunk duration.

        private float dunkDuration;
        // A dunking basketball release moment.

        private float dunkReleaseTime;
        // Whether to hide the ball slot when dunking.

        private bool dunkBallSlotsHidden;
        // Dunk starting position.

        private Vector2 dunkStartPosition;
        // Dunk target location.

        private Vector2 dunkTargetPosition;
        // Current phase of block/feint.
        private BlockPumpPhase blockPumpPhase;
        // Whether the current action is a fake action rather than a block.

        private bool blockPumpIsPump;
        // Block/Fake phase timer.

        private float blockPumpTimer;
        // Whether the starting animation is ready to switch stages.

        private bool blockPumpStartReady;
        // Whether the end animation is ready to switch stages.

        private bool blockPumpEndReady;
        // Countdown to steal decision.

        private float stealAttemptTimer = -1f;
        // The remaining time of the steal animation.

        private float stealAnimationTimer = -1f;
        // The remaining time of dizziness.

        private float stunTimer;
        // Current facing direction.

        private float facingDirection;
        // The locked facing direction when the snap begins.

        private float stealFacingDirection;
        // Whether catching/picking up the ball is allowed.

        private bool canTakeInHands;
        // Whether shots are allowed.

        private bool canThrow;
        // Whether it is in a jumping attack state with the ball in hand.

        private bool attackJump;
        // The most recent shot position is X.

        private float pointOfThrow;
        // Whether to enable jump block judgment.

        private bool jumpBlockActive;
        // Whether you need to prepare to block shots at the moment.

        private bool needBlock;
        // Are superpowers ready?

        private bool readyForSuper;
        // Whether a super skill is being performed.

        private bool isSuperShot;
        // Has been temporarily removed from the match.

        private bool removedFromPlay;
        // Current super charging time.

        private float superChargeTime;
        // Hell extra super sprint cooldown timer.

        private float hellBonusSuperDashCooldownTimer;
        // Infernal extra shield cooldown timer.

        private float hellBonusShieldCooldownTimer;
        // Main visual zoom factor.

        private float graphicScaleMultiplier = 1f;
        // Current superpower stage.

        private SuperPhase superPhase;
        // Superpower phase timer.
        private float superTimer;
        // The total duration of the super power stage.

        private float superDuration;
        // Super power mobile starting point.

        private Vector2 superStartPosition;
        // Super power to move target point.

        private Vector2 superTargetPosition;
        // Whether the super sprint direction is to the right.

        private bool dashToRight;
        // Do you still have to deal with teammates receiving the ball after super sprinting?

        private bool dashTeammatePending;
        // Current teammate reference.

        private mlpPlayerObject teamMate;
        // Whether the starting charge of Hell difficulty has been released.

        private bool hellOpeningChargeApplied;
        // Whether to wait for the return of native super energy.

        private bool hellNativeSuperRefundPending;
        // Whether to temporarily lock the ball after blocking the shot.

        private bool guaranteedBlockPickupLocked;
        // Whether the score upgrade is activated.

        private bool scoreUpgradeActive;
        // Whether the score upgrade waits for the current shot to be settled.

        private bool scoreUpgradePendingShot;
        // Tutorial perfect shot is ready.

        private bool tutorialPerfectShotPrimed;
        // Tutorial on whether the perfect dunk is ready.

        private bool tutorialPerfectDunkPrimed;
        // Whether the tutorial reimbursement deduction has been prepared.

        private bool tutorialPutbackDunkPrimed;
        // Tutorial dunk success rate coverage value.

        private float tutorialDunkCompletionChanceOverride = -1f;
        // Tutorial air movement time multiplier.

        private float tutorialAirMotionTimeScale = 1f;
        // Whether tutorial jump block assist is enabled.

        private bool tutorialJumpBlockAssist;
        // Fixed score bonus for remaining time.

        private float flatScoreBonusTimer;
        // Fixed score bonus points.

        private int flatScoreBonusPoints;
        // Movement buff remaining time.

        private float moveBuffTimer;
        // Whether movement buffs still provide extra score bonuses.

        private bool moveBuffScoreBonusAvailable;
        // The proportion of super energy to be returned.

        private float pendingScoreRefundFraction;
        // Score return countdown.

        private float pendingScoreRefundTimer;
        // Backboard magnet time remaining.
        private float reboundMagnetTimer;

        // Game core quotes.

        public mlpGameCore GameCore { get; }
        // The player's current position.

        public Vector2 Position;
        // The player's current speed.

        public Vector2 Velocity;
        // The side of the field the player is on.

        public int Side { get; }
        // Whether you are currently holding the ball.

        public bool WithBall { get; private set; }
        // Whether it is a human player.

        public bool IsHuman { get; }
        // Whether it is currently on the ground.

        public bool IsGrounded { get; private set; } = true;
        // The X coordinate of the current attack target.

        public float AttackTargetX => Side == -1 ? mlpObjectsData.BasketCenter2 : mlpObjectsData.BasketCenter;
        // Whether you are sprinting or not.

        public bool IsDashing => dashTimer > 0f;
        // Whether it is in the blocking stage.

        public bool IsBlocking => blockPumpPhase == BlockPumpPhase.Holding && !blockPumpIsPump;
        // Whether to have ground cap collision body.

        public bool HasGroundBlockBody => IsBlocking && IsGrounded && !removedFromPlay && !isSuperShot && stunTimer <= 0f;
        // Is it in the feint stage?

        public bool IsPumping => blockPumpPhase != BlockPumpPhase.None && blockPumpIsPump;
        // is moving.

        public bool IsMoving => Mathf.Abs(Velocity.x) > 20f;
        // Is it dunking?

        public bool IsDunking => isDunking;
        // Current facing direction.

        public float FacingDirection => facingDirection;
        // Whether catching/picking up the ball is allowed.

        public bool CanTakeInHands => canTakeInHands && !WithBall && !removedFromPlay;
        // Whether general actions are allowed.

        public bool CanAct => actionLatch <= 0f && stunTimer <= 0f && !stealAnimationActive && !isDunking && !isSuperShot;
        // Whether the conditions for settling ground blocks are met.

        public bool CanResolveGroundBlock => IsGrounded && !removedFromPlay && !isSuperShot && !isDunking && stunTimer <= 0f && !stealAnimationActive;
        // Are you ready to sprint?

        public bool ReadyForDash => readyForDash && dashTimer <= 0f && !isSuperShot;
        // Team player number.

        public int PlayerNo => playerNo;
        // Skill level.

        public int SkillLevel => skillLevel;
        // Superpower ID.

        public int SuperId => superId;
        // Role ID.

        public int CharacterId => characterId;
        // Skill type.

        public mlpCharacterSkillType SkillType => skillDefinition.SkillType;
        // Whether to use ball control skills.

        public bool UsesPossessionSkill => skillDefinition.UsesPossessionSkill;
        // Whether to use sprint skills.

        public bool UsesDashSkill => skillDefinition.UsesDashSkill;
        // Whether to use shield skills.

        public bool UsesShieldSkill => skillDefinition.UsesBasketShield;
        // Whether to use freezing skills.

        public bool UsesFreezeSkill => skillDefinition.UsesFreezeSkill;
        // Whether to use the Backboard Magnet skill.

        public bool UsesReboundMagnetSkill => skillDefinition.UsesReboundMagnetSkill;
        // Whether to use certain shot-blocking skills.
        public bool UsesGuaranteedBlockSkill => skillDefinition.UsesGuaranteedBlockSkill;
        // Are you ready to unleash your superpowers?

        public bool ReadyForSuper => !isSuperShot && (readyForSuper || mlpQuickTestSettings.Enabled);
        // Is it still possible to use Hell Extra Super Dash.

        public bool CanUseHellBonusSuperDash => hellEnhanced && (mlpQuickTestSettings.Enabled || hellBonusSuperDashCooldownTimer <= 0f);
        // Is it possible to use the extra shield of hell.

        public bool CanUseHellBonusShield => hellEnhanced && shield != null && (mlpQuickTestSettings.Enabled || hellBonusShieldCooldownTimer <= 0f) && shield.CanActivate;
        // Whether a super shot is being executed.

        public bool IsSuperShot => isSuperShot;
        // Whether a defensive block is currently required.

        public bool NeedBlock => needBlock;
        // Is it allowed to take action?

        public bool CanThrow => canThrow;
        // Player controller reference.

        public IBLPlayerController Controller => controller;
        // Whether to use highlight skill shadows.

        private bool UsesHighlightedSkillShadow => skillDefinition.SkillType == mlpCharacterSkillType.CarnivalJackpot && (scoreUpgradeActive || scoreUpgradePendingShot);
        // The actual superpower cooldown time.

        private float EffectiveSuperCoolDown => mlpQuickTestSettings.Enabled ? 0f : superCoolDown;

        public void ApplyBonusSuperCharge(float amount)
        {
            // 1. Get the cooldown time of super skills

            var cooldown = EffectiveSuperCoolDown;
            // 2. If the cooling time is 0 (quick test mode), refresh directly

            if (cooldown <= 0f)
            {
                RefreshQuickTestSuperReady();
                return;
            }

            // 3. If the charge is invalid or the super skill is ready or in use, return directly.

            if (amount <= 0f || readyForSuper || isSuperShot)
            {
                return;
            }

            // 4. Increase the charging time (not exceeding the upper limit of the cooling time)

            superChargeTime = Mathf.Min(cooldown, superChargeTime + amount);
            // 5. Update the energy bar UI display

            energyBar?.SetCharge(superChargeTime / cooldown);
            // 6. If the charge is full, mark the super skill as ready

            if (superChargeTime >= cooldown)
            {
                readyForSuper = true;
                // 7. Human players play the charging completion sound effect

                if (IsHuman)
                {
                    mlpAudio.Instance?.Play(mlpAssets.Sounds.PEnergy);
                }
            }
        }

        private void RefreshQuickTestSuperReady()
        {
            // 1. If quick test mode is not enabled or super skills are being used, return directly

            if (!mlpQuickTestSettings.Enabled || isSuperShot)
            {
                return;
            }

            // 2. Mark super skill ready

            readyForSuper = true;
            // 3. Set the charging time to full value

            superChargeTime = superCoolDown;
            // 4. Set the energy bar UI to full

            energyBar?.SetCharge(1f);
        }

        /// <summary>
        /// Set up the player character, including sprites, shadows, skeletal animation, controllers and skill effects.

        /// </summary>
        /// <param name="gameCore">Central game logic coordinator</param>

        /// <param name="teamIndex">Team index (0 is left, 1 is right)</param>

        /// <param name="characterId">Character identifier used to find skill definitions</param>

        /// <param name="playerNo">Player number in the team (0 or 1)</param>
        /// <param name="playerBrain">Brain string that determines the controller type</param>
        /// <param name="skillLevel">AI four-level skill index (0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell); human players retain the basic feel configuration. </param>
        /// <param name="parent">The parent Transform used to mount visual child objects</param>

        public mlpPlayerObject(mlpGameCore gameCore, int teamIndex, int characterId, int playerNo, string playerBrain, int skillLevel, Transform parent)
        {
            // 1. Save basic identity information: team affiliation, role number, player number, skill level
            GameCore = gameCore;
            this.teamIndex = teamIndex;
            this.characterId = characterId;
            this.playerNo = playerNo;
            this.skillLevel = skillLevel;
            Side = teamIndex == 0 ? -1 : 1;
            renderDepthBias = teamIndex * TeamDepthStep + playerNo * PlayerDepthStep;

            // 2. Determine whether it is a human player or AI, and analyze the controller key slots

            IsHuman = !playerBrain.StartsWith("B") && !playerBrain.StartsWith("T");
            brainSlot = mlpControlsData.ParseControllerSlot(playerBrain);

            // 3. Load the character skill definition and superpower ID to obtain the AI difficulty parameters

            skillDefinition = mlpCharacterSkillsData.Get(characterId);
            superId = skillDefinition.IconSuperId;
            aiDifficultyTuning = mlpAIDifficultyTuning.Get(mlpInventory.Instance.Difficulty);
            hellEnhanced = !IsHuman && mlpInventory.Instance.Difficulty == mlpAiDifficulty.Hell;

            // 4. Read shooting, dunk and super power cooling parameters
            //    - Human players use a fixed basic feel to avoid AI difficulty switching affecting the player's own operating experience

            //    - AI uses a four-level skill index, and the index only allows four meanings: Easy/Normal/Hard/Hell

            var profile = IsHuman
                ? mlpAISkillsData.GetHumanPlayerProfile()
                : mlpAISkillsData.Get(skillLevel);
            accuracy = profile.Accuracy;
            chanceToCompleteDunk = profile.ChanceToCompleteDunk;
            superCoolDown = profile.CoolDown;
            dashDelay = new UseDelay(mlpObjectsData.DashDelay * (hellEnhanced ? aiDifficultyTuning.DashCooldownMultiplier : 1f));

            // 5. Hell difficulty bonus: super sprint and shield cooldown

            //    Currently, there are only four levels of difficulty: Easy/Normal/Hard/Hell, and Hell is already the highest level.

            //    Therefore, additional hidden opponent gears are no longer revealed based on skill values.

            hellBonusSuperDashCooldownDuration = hellEnhanced
                ? aiDifficultyTuning.BonusSuperDashCooldown
                : 0f;
            hellBonusShieldCooldownDuration = hellEnhanced
                ? aiDifficultyTuning.BonusShieldCooldown
                : 0f;

            // 6. Set the super dunk position and sprint target coordinates according to the side you are on.
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

            // 7. Create player character GameObject (used to host skeletal animation)

            graphic = new GameObject($"Player_{teamIndex}_{playerNo}");
            graphic.transform.SetParent(parent, false);

            // 8. Create player shadows: load the shadow sprite map and set the rendering level

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
            // The carnival jackpot skill uses a red shade, and other characters use the default shade.

            activeSkillShadowSprite = skillDefinition.SkillType == mlpCharacterSkillType.CarnivalJackpot
                ? mlpGameplaySpriteLoader.LoadGameplaySprite(
                    mlpAssets.Images.GameplayImages.PlayerShadowPrimaryRed,
                    0.5f,
                    0.5f,
                    mlpAtlasCache.Instance.Gameplay,
                    "ShadowMC0000") ?? defaultShadowSprite
                : defaultShadowSprite;
            shadowRenderer.sortingOrder = 2;

            // 9. Create skeletal animation (armature), set pixel alignment and scaling, and apply character appearance

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
                // 10. Subscribe to animation completion and frame event callbacks (for dunk release, skill triggering, etc.)

                armature.AnimationComplete += OnAnimationComplete;
                armature.FrameEvent += OnAnimationFrameEvent;
            }
            else
            {
                CreateFallbackAvatar();
            }

            // 11. Create a controller: humans use the keyboard, AI uses the corresponding brain decision maker, and tutorials use the tutorial controller

            controller = IsHuman
                ? new mlpKeyboardController(playerBrain)
                : playerBrain.Length > 0 && (playerBrain[0] == 'T' || playerBrain[0] == 't')
                    ? new mlpTutorialOpponentController(this, skillLevel)
                    : mlpAIController.CreateForBrain(this, playerBrain, skillLevel);

            // 12. Create skill-related visual components: energy bar (humans only), teleportation effects, shields, skill light effects

            energyBar = IsHuman ? new mlpEnergyBarView(parent, brainSlot, skillDefinition, superCoolDown) : null;
            teleportFx = (skillDefinition.UsesTeleportDunk || skillDefinition.UsesGuaranteedBlockSkill) ? new mlpTeleportFx(parent, skillDefinition) : null;
            shield = skillDefinition.UsesBasketShield || hellEnhanced
                ? new mlpShieldObject(Side, Side == -1 ? gameCore.BasketLeft : gameCore.BasketRight, parent, skillDefinition)
                : null;
            skillFx = new mlpPlayerSkillFx(parent, skillDefinition);

            // 13. Reset all states to initial values (position, sprint, stun, dunk, etc.)

            Restart(0);
        }

        /// <summary>
        /// Clean up at the end of a match resources that were only used during runtime (such as energy bars).

        /// </summary>
        public void ReleaseRuntimeResources()
        {
            energyBar?.ReleaseRuntimeResources();
        }

        /// <summary>
        /// Resets all player states for the new round: position, sprint, stun, dunk, superpower and skill timers.

        /// </summary>
        /// <param name="startSide">The starting side where the player is after reset</param>

        public void Restart(int startSide)
        {
            // 1. Reset ball holding and movement status
            WithBall = false;
            Velocity = Vector2.zero;

            // 2. Reset sprint related status (timer, direction, buffer input)

            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            readyForDash = false;
            dashDelay.Activate();
            canDoAction = true;

            // 3. Reset shot and steal marks

            pendingGroundThrow = false;
            pendingStealAction = false;
            stealAnimationActive = false;
            alleyOopPendingThrow = false;

            // 4. Reset dunk status (timer, release mark, ball slot visibility)

            isDunking = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = 0f;
            dunkReleaseTime = 0f;
            SetDunkBallSlotsHidden(false);

            // 5. Reset the blocking animation stage

            blockPumpPhase = BlockPumpPhase.None;
            blockPumpIsPump = false;
            blockPumpTimer = 0f;
            blockPumpStartReady = false;
            blockPumpEndReady = false;

            // 6. Reset steal timer and stun

            stealAttemptTimer = -1f;
            stealAnimationTimer = -1f;
            stunTimer = 0f;
            actionLatch = 0f;

            // 7. Reset orientation and operation permissions

            facingDirection = -Side;
            stealFacingDirection = facingDirection;
            canTakeInHands = true;
            canThrow = true;
            attackJump = false;
            jumpBlockActive = false;
            needBlock = false;
            removedFromPlay = false;
            graphicScaleMultiplier = 1f;

            // 8. Reset super power stage and sprint status

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

            // 9. Reset tutorial and special score bonus markers

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

            // 10. Hide all skill effects

            teleportFx?.Hide();
            shield?.Reset();
            skillFx?.Stop();

            // 11. Calculate the player's initial X coordinate based on side and starting position

            var x = mlpConstants.Width2 + Side * (playerNo == 0 ? mlpObjectsData.PlayerIndentX : 200f);
            if (startSide == Side)
            {
                x = Side == -1 ? mlpObjectsData.IndentGeneralX : mlpConstants.Width - mlpObjectsData.IndentGeneralX;
            }

            // 12. Set the initial position, landing mark, and super power charging (the hell difficulty starts with some charging)

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
        /// Runs the full player update loop: inputs, physics, sprinting, jumping, throwing, tackling, blocking, and animations.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public void Update(float dt)
        {
            // 1. Update all skill effects (teleport, shield, skill timer, rebound magnet, skill light effect)

            teleportFx?.Update(dt);
            shield?.Update(dt);
            UpdateSkillTimers(dt);
            UpdateReboundMagnet(dt);
            skillFx?.Update(dt, Position, facingDirection, !removedFromPlay && graphicScaleMultiplier > 0.05f);
            hellBonusSuperDashCooldownTimer = Mathf.Max(0f, hellBonusSuperDashCooldownTimer - dt);
            hellBonusShieldCooldownTimer = Mathf.Max(0f, hellBonusShieldCooldownTimer - dt);
            RefreshQuickTestSuperReady();

            // 2. Super power charging: The charging time is accumulated every frame. When full, the mark is ready and the sound effect is played.

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

            // 3. Decrement the action cooling timer and restore action permission when the controller is ready

            actionLatch -= dt;
            if (!canDoAction && !stealAnimationActive && controller.ReadyForAction())
            {
                canDoAction = true;
            }

            // 4. Sprint cooldown timer: After the cooldown, the marker can sprint again

            if (!readyForDash && dashDelay.Update(dt) == 1)
            {
                readyForDash = true;
            }

            // 5. If a superpower animation is playing, enter the superpower-specific update process.

            if (isSuperShot)
            {
                UpdateSuper(dt);
                return;
            }

            // 6. If you are dunking, enter the dunk-specific update process.

            if (isDunking)
            {
                UpdateDunk(dt);
                return;
            }

            // 7. Dizziness state: Clear all operation inputs, prohibit movement, and resume after the countdown is over.

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

            // 8. If a steal animation is being played, go through the special process for the steal animation.

            if (stealAnimationActive)
            {
                UpdateStealAnimation(dt);
                return;
            }

            // 9. Read player input, update sprint buffer, facing direction, and take-off block threat

            controller.UpdateController(dt);
            UpdateDashBuffer(dt);
            UpdateFacing();
            UpdateJumpBlockThreat();

            // 10. If you are blocking a shot or feinting, follow the special process for blocking or feinting.

            if (blockPumpPhase != BlockPumpPhase.None)
            {
                UpdateBlockOrPump(dt);
                return;
            }

            // 11. Countdown to steal: The steal result will be calculated after the timer ends.

            if (stealAttemptTimer >= 0f)
            {
                stealAttemptTimer -= dt;
                if (stealAttemptTimer <= 0f)
                {
                    ResolveStealAttempt();
                }
            }

            // 12. Horizontal movement: move at sprint speed during sprinting, otherwise move at normal movement speed
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

                // 12a. Detect sprint input (direct input or buffered input), and start sprint if the conditions are met

                var dashInput = controller.CurrentDash != 0
                    ? controller.CurrentDash
                    : dashBufferTimer > 0f ? bufferedDashDirection : 0;
                if (dashInput != 0 && IsGrounded && readyForDash)
                {
                    StartDash(dashInput);
                }
            }

            // 13. Jump input processing: try to jump to block shots when there is no ball, and jump to shoot when there is the ball.

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

            // 14. Action key input processing: shooting when there is the ball, stealing when there is no ball (tutorial mode has special logic for make-up dunks)

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

            // 15. Block/feint key input: Triggered when the ground is on the ground and the cooldown ends

            if (dashTimer <= 0f && controller.CurrentBlockOrPump && IsGrounded && actionLatch <= 0f)
            {
                BeginBlockOrPump();
            }

            // 16. Super power input: Super powers (dunk, sprint, teleport, etc.) are triggered when conditions are met.

            if (TryStartSuper(controller.CurrentSuper))
            {
                UpdateGraphic();
                return;
            }

            // 17. Apply gravity (when airborne), then update position and constrain within field boundaries

            if (!IsGrounded)
            {
                Velocity.y += mlpObjectsData.Gravity.y * 3f * dt * tutorialAirMotionTimeScale;
            }

            var verticalDt = IsGrounded ? dt : dt * tutorialAirMotionTimeScale;
            Position += new Vector2(Velocity.x * dt, Velocity.y * verticalDt);
            Position.x = Mathf.Clamp(Position.x, 20f, mlpConstants.Width - 20f);

            // 18. Landing detection: Reset the jumping state when reaching the height of the ground, jump and shoot with the ball, and restore the ability to catch the ball without the ball.

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

            // 19. Play ground animation: choose running or standby animation according to speed

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

            // 20. Let the basketball follow the player when holding the ball, otherwise the basketball catch detection will be resumed

            if (WithBall)
            {
                GameCore.Ball.TakeInHands(Side);
            }
            else
            {
                RestoreBallPickupIfReady();
            }

            // 21. Move players and shadow sprites to their current physical locations

            UpdateGraphic();
        }

        /// <summary>
        /// Updates cooldown and input readiness status during pre-match countdown without moving players.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public void TickPreMatch(float dt)
        {
            // 1. Update the animations of teleportation effects and shield effects

            teleportFx?.Update(dt);
            shield?.Update(dt);

            // 2. Reduce action lock timer

            if (actionLatch > 0f)
            {
                actionLatch -= dt;
            }

            // 3. If the controller is ready and not in the ball-stealing animation, unlock the action

            if (!canDoAction && !stealAnimationActive && controller.ReadyForAction())
            {
                canDoAction = true;
            }

            // 4. If sprint ends late, mark sprint ready

            if (!readyForDash && dashDelay.Update(dt) == 1)
            {
                readyForDash = true;
            }
        }

        /// <summary>
        /// Pick up the basketball, update the controller signal, and play the ball-holding animation.

        /// </summary>
        public void TakeBallInHands()
        {
            // 1. Mark the player holding the ball and reset the blocking and catching status

            WithBall = true;
            jumpBlockActive = false;
            canTakeInHands = false;
            // 2. Shooting is only allowed on the ground or under the basket

            canThrow = IsGrounded || IsUnderGlass();
            attackJump = false;
            // 3. If under the basket, record the shot position

            if (IsUnderGlass())
            {
                pointOfThrow = Position.x;
            }

            // 4. Cancel the ball-stealing animation and clear the stun timer

            CancelStealAnimation(false);
            stunTimer = 0f;
            // 5. Notify the basketball object to enter the ball-holding state

            if (!removedFromPlay)
            {
                GameCore.Ball.TakeInHands(Side);
            }

            // 6. Notify the game core and play the ball holding animation

            GameCore.NotifyBallInHands(Side, playerNo);
            PlayState(IsGrounded ? "idle_wb" : "fly1_wb");
        }

        /// <summary>
        /// Instantly places the player at a specified location and orientation for scripted moments in the tutorial.

        /// </summary>
        /// <param name="position">World coordinates</param>

        /// <param name="facing">Facing direction (-1 or 1)</param>
        public void TutorialSnapTo(Vector2 position, float facing)
        {
            // 1. Instantly move the player to the designated position and return the speed to zero.

            Position = position;
            Velocity = Vector2.zero;
            // 2. Update shooting position record

            pointOfThrow = Position.x;
            // 3. If heading is specified, update the player's facing direction

            if (!Mathf.Approximately(facing, 0f))
            {
                facingDirection = Mathf.Sign(facing);
                stealFacingDirection = facingDirection;
            }

            // 4. Update player graphic position

            UpdateGraphic();
        }

        /// <summary>
        /// Instantly fills the Super Power Gauge, allowing the player to activate skills.

        /// </summary>
        public void TutorialChargeSuper()
        {
            // 1. Get the cooldown time of super skills

            var cooldown = EffectiveSuperCoolDown;
            // 2. If the cooling time is 0, directly fill it with the default value.

            if (cooldown <= 0f)
            {
                readyForSuper = true;
                superChargeTime = superCoolDown;
                energyBar?.SetCharge(1f);
                return;
            }

            // 3. Set the charging time to full value and mark the super skill as ready.

            readyForSuper = true;
            superChargeTime = cooldown;
            // 4. Update the energy bar UI to be full.

            energyBar?.SetCharge(1f);
        }

        /// <summary>
        /// Enable the Tutorial Perfect Shot flag to make your next shot completely accurate.

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
        /// Releases the basketball from the player's hands without throwing it, resetting throw-related states.

        /// </summary>
        public void FreeBall()
        {
            // 1. If the player is not holding the ball, return directly

            if (!WithBall)
            {
                return;
            }

            // 2. Reset ball holding and related combat status

            WithBall = false;
            jumpBlockActive = false;
            canThrow = false;
            attackJump = false;
            // 3. Cancel the ball-stealing animation

            CancelStealAnimation(false);
            // 4. Play the corresponding standby animation based on whether it is on the ground

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
            // 1. If the player is not holding the ball or the basketball does not exist, return directly

            if (!WithBall || GameCore.Ball == null)
            {
                return;
            }

            // 2. Release control of the basketball

            FreeBall();
            // 3. Put the basketball down from under the player's position (dropping effect when frozen)

            GameCore.Ball.DropFromFreeze(Position + new Vector2(0f, -45f));
        }

        /// <summary>
        /// Handle basketball to free state: reset throwing/blocking state and notify controller.

        /// </summary>
        public void NotifyBallLoose()
        {
            // 1. Mark player no longer holding the ball

            WithBall = false;
            // 2. Cancel the ball-stealing animation and clear the stun

            CancelStealAnimation(false);
            stunTimer = 0f;
            // 3. Reset throwing and attack jump status

            canThrow = false;
            attackJump = false;
            // 4. Allow the ball to be picked up again

            canTakeInHands = true;
            // 5. Reset blocking status

            jumpBlockActive = false;
            needBlock = false;
            // 6. Notify the controller that the basketball is no longer in hand
            controller.BallOthers();
        }

        /// <summary>
        /// Notify the controller that someone picked up the basketball and update the offensive/defensive strategy.

        /// </summary>
        /// <param name="holderSide">The side of the player who picked up the basketball</param>

        /// <param name="holderPlayerNo">The player number of the current ball holder</param>

        public void NotifyBallInHands(int holderSide, int holderPlayerNo)
        {
            // 1. If there is a pending score upgrade, clear it

            if (scoreUpgradePendingShot)
            {
                ClearScoreUpgrade();
            }

            // 2. Notify the controller of different strategies depending on whether the ball holder is your own team

            if (holderSide == Side)
            {
                // 3. Your own team has the ball: switch to offensive strategy

                controller.BallInOwnHands(holderPlayerNo);
                needBlock = false;
            }
            else
            {
                // 4. The opponent holds the ball: switch to defensive strategy and prepare to block shots

                controller.BallInOpponentsHands(holderPlayerNo);
                needBlock = true;
            }
        }

        /// <summary>
        /// Notify the controller that someone has taken a shot, and update rebounding and shot-blocking strategies.

        /// </summary>
        /// <param name="shotSide">The side of the shooting player</param>

        /// <param name="shooterPlayerNo">The shooting player's number</param>

        public void NotifyBallShot(int shotSide, int shooterPlayerNo)
        {
            // 1. If you have a score upgrade and you shoot the ball yourself, mark it for confirmation.

            if (scoreUpgradeActive && shotSide == Side && shooterPlayerNo == playerNo)
            {
                scoreUpgradeActive = false;
                scoreUpgradePendingShot = true;
            }

            // 2. Notify the controller of different strategies depending on whether the shooting team is your own.
            if (shotSide == Side)
            {
                // 3. Own shot: switch to your own shooting strategy

                controller.BallOwnShoot(shooterPlayerNo);
                needBlock = false;
            }
            else
            {
                // 4. Opponent's shot: switch to the opponent's shot strategy and prepare to block shots

                controller.BallOpponentShoot(shooterPlayerNo);
                needBlock = true;
            }
        }

        /// <summary>
        /// Notifies the controller that the basketball is in a neutral state (not holding the ball, not shooting).
        /// </summary>
        public void NotifyBallOthers()
        {
            needBlock = false;
            jumpBlockActive = false;
            controller.BallOthers();
        }

        /// <summary>
        /// Try to activate super skills when the player meets the conditions.
        /// </summary>
        /// <returns>Returns true if activated successfully; otherwise returns false. </returns>
        public bool SuperShot()
        {
            return TryStartSuper(true);
        }

        /// <summary>
        /// After the basketball reaches the receiving point, the alley-oop super transfer phase begins.
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
        /// Tests whether the player's shield ability can block a given basketball.
        /// </summary>
        /// <param name="ball">The basketball object to be detected or affected</param>
        /// <returns>Returns true if blocked successfully; otherwise returns false. </returns>
        public bool TryShieldBall(mlpBallObject ball)
        {
            return shield != null && shield.TryBlockBall(ball);
        }

        public void ApplyFreeze(float duration, mlpCharacterSkillDefinition freezeDefinition)
        {
            // 1. If the duration is invalid or the player is not in the game or super skill/dunk, return directly
            if (duration <= 0f || removedFromPlay || isSuperShot || isDunking)
            {
                return;
            }

            // 2. If you hold the ball, drop the basketball

            DropHeldBallForFreeze();
            // 3. Set the dizziness timer (take a larger value to avoid shortening the existing dizziness)

            stunTimer = Mathf.Max(stunTimer, duration);
            // 4. Reset sprint related status
            dashTimer = 0f;
            dashDirection = 0;
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;
            // 5. Reset pending actions and ball stealing status

            pendingGroundThrow = false;
            pendingStealAction = false;
            CancelStealAnimation(false);
            // 6. Reset the block charging state

            blockPumpPhase = BlockPumpPhase.None;
            blockPumpTimer = 0f;
            jumpBlockActive = false;
            // 7. All movements and ball picking are prohibited

            canDoAction = false;
            canTakeInHands = false;
            // 8. Stop moving

            Velocity = Vector2.zero;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            // 9. Play dizziness animation

            PlayState("stun");
            // 10. Play freezing effects, sound effects and display prompts

            skillFx?.PlayBurst(Mathf.Min(duration, 0.8f), freezeDefinition);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Stun, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PStunned, 0.9f);
            GameCore.ShowHudBonusNotice("FROZEN 2 SEC!", 0.95f);
        }

        public int ResolveScorePoints(int basePoints, out string scoreNotice)
        {
            // 1. Initialize output parameters and basic scores

            scoreNotice = null;
            var resolvedPoints = basePoints;

            // 2. If there is a pending score upgrade, add an additional 2 points (up to a maximum of 5 points)

            if (scoreUpgradePendingShot && resolvedPoints >= 2)
            {
                resolvedPoints = Mathf.Min(5, resolvedPoints + 2);
                scoreUpgradePendingShot = false;
                scoreNotice = skillDefinition.ScoreNotice;
                skillFx?.PlayBurst(0.5f);
            }

            // 3. If the movement buff is activated and there is an additional score bonus, the bonus will be stacked

            if (moveBuffTimer > 0f && moveBuffScoreBonusAvailable && skillDefinition.FlatScoreBonus > 0)
            {
                resolvedPoints += skillDefinition.FlatScoreBonus;
                moveBuffScoreBonusAvailable = false;
                moveBuffTimer = 0f;
                scoreNotice = string.IsNullOrEmpty(scoreNotice) ? skillDefinition.ScoreNotice : scoreNotice;
                skillFx?.PlayBurst(0.45f);
            }

            // 4. If there is a temporary score bonus effect, the bonus will be superimposed

            if (flatScoreBonusTimer > 0f && flatScoreBonusPoints > 0)
            {
                resolvedPoints += flatScoreBonusPoints;
                flatScoreBonusPoints = 0;
                flatScoreBonusTimer = 0f;
                scoreNotice = string.IsNullOrEmpty(scoreNotice) ? skillDefinition.ScoreNotice : scoreNotice;
                skillFx?.PlayBurst(0.45f);
            }

            // 5. Return the final calculated score

            return resolvedPoints;
        }

        public void OnScoreConfirmed()
        {
            // 1. If there is a pending confirmed score super energy return, it will be issued immediately
            if (pendingScoreRefundTimer > 0f && pendingScoreRefundFraction > 0f)
            {
                // 2. Proportional return of super energy

                GrantSuperChargeFraction(pendingScoreRefundFraction);
                // 3. Clear the return mark

                pendingScoreRefundFraction = 0f;
                pendingScoreRefundTimer = 0f;
                // 4. Display return prompt
                GameCore.ShowHudBonusNotice("SUPER REFUND!", 0.95f);
            }
        }

        /// <summary>
        /// Returns the extra steal range provided by the Hell difficulty enhancement.
        /// </summary>
        /// <returns>Extra tackling distance. </returns>
        public float GetStealDistanceBonus()
        {
            return hellEnhanced ? aiDifficultyTuning.StealRangeBonus : 0f;
        }

        /// <summary>
        /// Returns the physics mass used for ground-level player-to-player collisions.
        /// </summary>
        /// <returns>Collision physics mass value. </returns>
        public float GetCollisionMass()
        {
            return HasGroundBlockBody ? GroundBlockCollisionMass : GroundCollisionMass;
        }

        /// <summary>
        /// Returns true when the player is moving towards another player's position.

        /// </summary>
        /// <param name="other">Another player whose direction of movement is to be detected</param>
        /// <returns>Returns true when moving towards the other party; otherwise returns false. </returns>
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
        /// Detects if a player is sprinting towards another player.

        /// </summary>
        /// <param name="other">Another player to detect sprinting direction</param>
        /// <returns>Returns true when sprinting towards the opponent; otherwise returns false. </returns>
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
        /// The sprint was interrupted due to a blocked shot.

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
        /// Apply horizontal separation force.
        /// </summary>
        /// <param name="delta">Detached offset</param>

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
        /// Handling when intercepted.

        /// </summary>
        public void OnStolen()
        {
            GetBeStolen(Position.x, false);
        }

        /// <summary>
        /// Check whether it can be intercepted.
        /// </summary>
        /// <param name="thiefX">The X coordinate of the stealer</param>

        /// <param name="thiefFacingScaleX">The tackler's facing X scale</param>

        /// <param name="stealDistance">the steal distance</param>
        /// <returns>The computed result.</returns>
        public float CheckToBeStolen(float thiefX, float thiefFacingScaleX, float stealDistance)
        {
            // 1. If you are not on the ground, are dazed, have exited the game, or are using super skills, -1 will be returned to indicate that you cannot steal.

            if (!IsGrounded || stunTimer > 0f || removedFromPlay || isSuperShot)
            {
                return -1f;
            }

            // 2. When the person who steals the ball faces to the right, check whether the ball holder is within the range of stealing the ball in front of him.

            if (thiefFacingScaleX >= 0f)
            {
                return Position.x >= thiefX && Position.x <= thiefX + stealDistance
                    ? Mathf.Abs(Position.x - thiefX)
                    : -1f;
            }

            // 3. When the ball-stealer faces left, check whether the ball-carrier is within the ball-stealing range in front of him or her.

            return Position.x >= thiefX - stealDistance && Position.x <= thiefX
                ? Mathf.Abs(Position.x - thiefX)
                : -1f;
        }

        /// <summary>
        /// Execute the intercepted logic.
        /// </summary>
        /// <param name="thiefX">The X coordinate of the stealer</param>

        /// <param name="applyBallSteal">Whether to apply the basketball steal effect</param>
        /// <returns>Returns true if the ball was previously held; otherwise returns false. </returns>
        public bool GetBeStolen(float thiefX, bool applyBallSteal = true)
        {
            // 1. If you have exited the game, return directly
            if (removedFromPlay)
            {
                return false;
            }

            // 2. Record whether the ball was held before, and then release the basketball

            var hadBall = WithBall;
            WithBall = false;
            // 3. Reset the status related to sprinting and stealing the ball

            dashTimer = 0f;
            dashDirection = 0;
            CancelStealAnimation(false);
            // 4. Reset throwing and attack status
            pendingGroundThrow = false;
            canThrow = false;
            attackJump = false;
            // 5. Calculate the stun time (the hell difficulty will be extended)

            var stunDuration = mlpObjectsData.StunDuration * (hellEnhanced ? aiDifficultyTuning.StunDurationMultiplier : 1f);
            stunTimer = Mathf.Max(stunTimer, stunDuration);
            // 6. All actions are prohibited

            canDoAction = false;
            jumpBlockActive = false;
            canTakeInHands = false;
            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, stunTimer);
            // 7. Play dizziness animation and sound effects

            PlayState("stun");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Stun, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PStunned, 0.9f);

            // 8. If the ball was previously held and the stealing effect needs to be applied, bounce the basketball away

            if (hadBall && applyBallSteal && GameCore.Ball != null)
            {
                var delta = Position.x - thiefX;
                var direction = delta > 0f ? 1 : -1;
                var distanceFactor = Mathf.Clamp01(Mathf.Abs(delta) / mlpObjectsData.StealDistance);
                GameCore.Ball.ApplySteal(Position + new Vector2(0f, -45f), distanceFactor, direction);
            }

            // 9. Whether to hold the ball before returning

            return hadBall;
        }

        /// <summary>
        /// Checks if a free ball can be picked up.
        /// </summary>
        /// <param name="ball">Basketball object to be detected</param>
        /// <returns>Returns the squared distance if pickable; otherwise returns -1. </returns>
        public float CheckLooseBallPickup(mlpBallObject ball)
        {
            // 1. Check if the basketball can be picked up and if the player can pick it up
            if (ball == null || !ball.CanBeTakenInHands || !CanTakeInHands)
            {
                return -1f;
            }

            // 2. Calculate the offset between the player and the basketball

            var delta = ball.Position - Position;
            var absX = Mathf.Abs(delta.x);
            var absY = Mathf.Abs(delta.y);
            // 3. If the horizontal or vertical distance exceeds the ball pickup range, return -1

            if (absX > mlpObjectsData.BallPickupDistanceX || absY > mlpObjectsData.BallPickupDistanceY)
            {
                return -1f;
            }

            // 4. Return the square of the distance (the smaller the closer, used to select the closest player)
            return delta.sqrMagnitude;
        }

        /// <summary>
        /// Try to block the basketball.

        /// </summary>
        /// <param name="ball">The basketball object to be detected or affected</param>
        /// <returns>Returns true if blocked successfully; otherwise returns false. </returns>
        public bool TryBlockBall(mlpBallObject ball)
        {
            // 1. Check whether it is in blocking state, whether the basketball can be blocked, and whether it is the opponent's ball.

            if (!jumpBlockActive || ball == null || !ball.IsBlockable || ball.Side == Side || removedFromPlay || isSuperShot)
            {
                return false;
            }

            // 2. Get the movement trajectory of the basketball (from the previous frame to the current frame)

            var start = ball.PreviousPosition;
            var end = ball.Position;
            // 3. If the trajectory of the basketball is completely behind the player, the shot cannot be blocked.

            if ((start.x - Position.x) * ball.Side <= 0f &&
                (end.x - Position.x) * ball.Side <= 0f)
            {
                return false;
            }

            // 4. Calculate the block collision area (increase the range to assist players in tutorial mode)

            var blockWidth = mlpObjectsData.JumpBlockWidth + (tutorialJumpBlockAssist ? 58f : 0f);
            var blockHeight = mlpObjectsData.JumpBlockHeight + (tutorialJumpBlockAssist ? 42f : 0f);
            var topBonus = tutorialJumpBlockAssist ? 18f : 0f;
            var bottomBonus = tutorialJumpBlockAssist ? 16f : 0f;
            // 5. Calculate the four boundaries of the collision rectangle (considering the basketball radius)
            var minX = Position.x - blockWidth * 0.5f - mlpObjectsData.BallRadius;
            var maxX = Position.x + blockWidth * 0.5f + mlpObjectsData.BallRadius;
            var minY = Position.y - blockHeight - mlpObjectsData.BallRadius - topBonus;
            var maxY = Position.y + mlpObjectsData.BallRadius + bottomBonus;
            // 6. Use sweep detection to determine whether the basketball trajectory passes through the block collision rectangle

            if (!SweptPointIntersectsRect(start, end, minX, maxX, minY, maxY))
            {
                return false;
            }

            // 7. Trigger the blocking effect and bounce the basketball away

            ball.ApplyBlock(this);
            return true;
        }

        /// <summary>
        /// Try to activate super skills.
        /// </summary>
        /// <param name="pressed">Whether to press the super key</param>
        /// <returns>Returns true if started successfully; otherwise returns false. </returns>
        private bool TryStartSuper(bool pressed)
        {
            // 1. Check whether the conditions for releasing superpowers are met: press the button, charging is complete, and no other superpowers are playing.
            RefreshQuickTestSuperReady();
            if (!pressed || !readyForSuper || GameCore.IsSuperShot)
            {
                return false;
            }

            // 2. Some skills require holding the ball to release.

            if (skillDefinition.RequiresBallToCast && !WithBall)
            {
                return false;
            }

            // 3. Sprint skills need to be on the ground and not during stun/dunk.

            if (skillDefinition.UsesDashSkill && (!IsGrounded || stunTimer > 0f || isDunking))
            {
                return false;
            }

            // 4. Backboard magnets and guaranteed blocks have their own conditions of use.

            if (skillDefinition.UsesReboundMagnetSkill && !CanUseReboundMagnet())
            {
                return false;
            }
            if (skillDefinition.UsesGuaranteedBlockSkill && !CanUseGuaranteedBlock())
            {
                return false;
            }

            // 5. Enter the super power state, send signals, display prompt text, and play burst special effects

            StartSuper(true);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Super, Side, playerNo);
            GameCore.ShowHudBonusNotice(skillDefinition.ActivateNotice, 0.95f);
            skillFx?.PlayBurst();

            // 6. Execute the corresponding super power effect according to the skill type

            switch (skillDefinition.SkillType)
            {
                case mlpCharacterSkillType.SoulReap:
                    MakeSuperDash();        // Soul Harvest: Sprint through the opponent to grab the ball
                    return true;
                case mlpCharacterSkillType.CarnivalJackpot:
                    MakeScoreUpgradeBuff(); // Carnival jackpot: next time score bonus

                    return true;
                case mlpCharacterSkillType.GhostSail:
                    MakeShield();           // Ghost Sail: Generates a shield in front of the basket

                    return true;
                case mlpCharacterSkillType.BloodMoonBlink:
                    MakeAlleyOop();         // Blood Moon Flash: Teleport to the basket for an alley-oop dunk

                    return true;
                case mlpCharacterSkillType.WaxOverdrive:
                    MakeWaxOverdrive();     // Wax Overload: special movement effects

                    return true;
                case mlpCharacterSkillType.BadLuck:
                    MakeFreeze();           // Doom: Freezes opponent players

                    return true;
                case mlpCharacterSkillType.ReboundMagnet:
                    MakeReboundMagnet();    // Backboard Magnet: Automatically attract rebound balls
                    return true;
                case mlpCharacterSkillType.SureBlock:
                    MakeGuaranteedBlock();  // Must hit the block: Must successfully block the shot
                    return true;
            }

            EndSuper();
            return false;
        }

        /// <summary>
        /// Try using Hell's bonus super dash.
        /// </summary>
        /// <returns>Returns true if used successfully; otherwise returns false. </returns>
        public bool TryUseHellBonusSuperDash()
        {
            // 1. Check whether the usage conditions are met (not in super state, not removed from the game, on the ground, not stunned, not dunked)

            if (!CanUseHellBonusSuperDash || GameCore.IsSuperShot || isSuperShot || removedFromPlay || !IsGrounded || stunTimer > 0f || isDunking)
            {
                return false;
            }

            // 2. Activate the super state (does not consume native charge) and perform super sprint

            StartSuper(false);
            MakeSuperDash();

            // 3. Set cooling timer and display prompt text
            hellBonusSuperDashCooldownTimer = mlpQuickTestSettings.Enabled ? 0f : hellBonusSuperDashCooldownDuration;
            GameCore.ShowHudBonusNotice("HELL DASH!", 0.9f);
            return true;
        }

        /// <summary>
        /// Try using Infernal Bonus Shield.

        /// </summary>
        /// <returns>Returns true if used successfully; otherwise returns false. </returns>
        public bool TryUseHellBonusShield()
        {
            // 1. Check whether the usage conditions are met (not in super state, not removed from the game, not stunned, not dunked)

            if (!CanUseHellBonusShield || GameCore.IsSuperShot || isSuperShot || removedFromPlay || stunTimer > 0f || isDunking)
            {
                return false;
            }

            // 2. Activate super state (does not consume native charge) and perform shield skills

            StartSuper(false);
            MakeShield();

            // 3. Set cooling timer and display prompt text
            hellBonusShieldCooldownTimer = mlpQuickTestSettings.Enabled ? 0f : hellBonusShieldCooldownDuration;
            GameCore.ShowHudBonusNotice("HELL SHIELD!", 0.95f);
            return true;
        }

        /// <summary>
        /// Activate super state.

        /// </summary>
        /// <param name="consumeNativeCharge">Whether to consume native charge</param>

        private void StartSuper(bool consumeNativeCharge)
        {
            // 1. Mark entering the super state

            isSuperShot = true;

            // 2. If native charge is consumed, clear the energy bar and mark that it may need to be returned

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

            // 3. Notify the whole world to enter super mode and cancel the current action in progress.

            GameCore.IsSuperShot = true;
            pendingGroundThrow = false;
            CancelStealAnimation(false);
            jumpBlockActive = false;
            blockPumpPhase = BlockPumpPhase.None;
            dashTimer = 0f;
        }

        /// <summary>
        /// End the super state.

        /// </summary>
        private void EndSuper()
        {
            // 1. Clear the super status mark, restore scaling, and allow participation in the game again

            isSuperShot = false;
            GameCore.IsSuperShot = false;
            superPhase = SuperPhase.None;
            graphicScaleMultiplier = 1f;
            removedFromPlay = false;

            // 2. Handle cooling and energy recovery logic

            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                // 2a. Fill the energy bar immediately without cooling
                readyForSuper = true;
                superChargeTime = superCoolDown;
                energyBar?.SetCharge(1f);
            }
            else if (hellNativeSuperRefundPending)
            {
                // 2b. Refund part of the charge in hell enhancement mode

                superChargeTime = Mathf.Min(cooldown, superChargeTime + cooldown * aiDifficultyTuning.NativeSuperRefundFraction);
                readyForSuper = superChargeTime >= cooldown;
                energyBar?.SetCharge(superChargeTime / cooldown);
            }

            // 3. Clear the return flag

            hellNativeSuperRefundPending = false;
        }

        /// <summary>
        /// Perform a super dunk.

        /// </summary>
        private void MakeMegaDunk()
        {
            // 1. Lock all actions, remove player from the game, face their own basket

            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            removedFromPlay = true;
            facingDirection = -Side;

            // 2. Calculate the flight starting point and the target point under the basket, and set the flight duration

            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkX, mlpObjectsData.AlleyOopY);
            superTimer = 0f;
            superDuration = Mathf.Max(0.3f, Vector2.Distance(superStartPosition, superTargetPosition) / 700f / 1.3333f);

            // 3. Enter the super dunk flight stage, play animation and sound effects

            superPhase = SuperPhase.MegaTravel;
            PlayState("megadunk");
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PMegaStart);
        }

        /// <summary>
        /// Continue the recovery phase of Super Dunk.

        /// </summary>
        private void ContinueSuperDunk()
        {
            // 1. Switch to super dunk recovery phase

            superPhase = SuperPhase.MegaRecover;

            // 2. Set the interpolation parameters from the current position to the landing point

            superStartPosition = Position;
            superTargetPosition = new Vector2(superDunkEndX, superDunkEndY);
            superTimer = 0f;
            superDuration = 0.1f;

            // 3. Play landing animation

            PlayState("megadunk_end");
        }

        /// <summary>
        /// Ending super dunk.

        /// </summary>
        private void EndSuperDunk()
        {
            // 1. If it is not in the super state, return directly (to prevent repeated calls)

            if (!isSuperShot)
            {
                return;
            }

            // 2. Release the ball and allow pickup and shooting

            WithBall = false;
            canTakeInHands = true;
            canThrow = true;

            // 3. Notify the game processor to execute a sure shot and let the basketball enter the basket

            GameCore.MatchProcessor.Shoot(Side, IsHuman, 8, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Dunk(Side, true);

            // 4. Place the player to the landing point and reset the speed and ground state

            Position = new Vector2(superDunkEndX, superDunkEndY);
            Velocity = Vector2.zero;
            IsGrounded = true;

            // 5. Play the standby animation and end the super state

            PlayState("idle");
            EndSuper();
        }

        /// <summary>
        /// Release shield skills.

        /// </summary>
        private void MakeShield()
        {
            // 1. Activate the shield object

            shield?.Activate();

            // 2. End the super state

            EndSuper();
        }

        private void MakeScoreUpgradeBuff()
        {
            // 1. Activate scorer bonus and play gain effects

            scoreUpgradeActive = true;
            scoreUpgradePendingShot = false;
            skillFx?.PlayBuff(float.PositiveInfinity);

            // 2. End the super state

            EndSuper();
        }

        private void MakeWaxOverdrive()
        {
            // 1. Set the movement acceleration duration and record whether there is an additional score bonus

            moveBuffTimer = skillDefinition.EffectDuration;
            moveBuffScoreBonusAvailable = skillDefinition.FlatScoreBonus > 0;

            // 2. Play the gain effect and end the super state

            skillFx?.PlayBuff(skillDefinition.EffectDuration);
            EndSuper();
        }

        private void MakeFreeze()
        {
            // 1. Find the nearest opponent and apply a freezing effect to it

            var opponent = GameCore.FindClosestOpponent(this);
            if (opponent != null)
            {
                opponent.ApplyFreeze(skillDefinition.EffectDuration, skillDefinition);
            }

            // 2. Play the burst special effects to end the super state
            skillFx?.PlayBurst(0.45f);
            EndSuper();
        }

        private void MakeReboundMagnet()
        {
            // 1. Set the rebound magnet duration (priority is given to skill definition, otherwise the default value is used)

            reboundMagnetTimer = skillDefinition.EffectDuration > 0f
                ? skillDefinition.EffectDuration
                : ReboundMagnetDefaultDuration;

            // 2. Allow the ball to be picked up, immediately perform a magnet update, and end the super state.

            canTakeInHands = true;
            UpdateReboundMagnet(0f);
            EndSuper();
        }

        private void MakeGuaranteedBlock()
        {
            // 1. Check whether the basketball can be blocked. If not satisfied, end the super power directly.

            var ball = GameCore.Ball;
            if (!CanUseGuaranteedBlockBall(ball))
            {
                EndSuper();
                return;
            }

            // 2. Lock all operations, stop moving, and cancel other ongoing actions.

            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            WithBall = false;
            Velocity = Vector2.zero;
            dashTimer = 0f;
            blockPumpPhase = BlockPumpPhase.None;
            jumpBlockActive = false;

            // 3. Teleport the player to the block position next to the basketball

            Position = GetGuaranteedBlockPosition(ball);
            IsGrounded = Position.y >= mlpObjectsData.PlayerIndentY - 0.5f;
            facingDirection = ball.Position.x >= Position.x ? 1f : -1f;

            // 4. Play the teleportation special effects and sound effects, and perform block judgment

            teleportFx?.StartPlay(Position.x, Position.y - GuaranteedBlockHandsOffsetY);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            PlayState("blockStart");
            ball.ApplyBlock(this);

            // 5. Display the prompt text and enter the block hovering stage.

            GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
            guaranteedBlockPickupLocked = !IsGrounded;
            superPhase = SuperPhase.GuaranteedBlockHold;
            superTimer = 0f;
            superDuration = GuaranteedBlockHoldDuration;
            actionLatch = Mathf.Max(actionLatch, GuaranteedBlockHoldDuration);
        }

        private Vector2 GetGuaranteedBlockPosition(mlpBallObject ball)
        {
            // 1. Calculate the horizontal position: offset in front of the basketball, limited to the court range

            var targetX = Mathf.Clamp(
                ball.Position.x - ball.Side * GuaranteedBlockHorizontalOffset,
                20f,
                mlpConstants.Width - 20f);

            // 2. Calculate vertical position: offset above basketball, constrained between basket and ground

            var targetY = Mathf.Clamp(
                ball.Position.y + GuaranteedBlockHandsOffsetY,
                mlpObjectsData.BasketHeight - 18f,
                mlpObjectsData.PlayerIndentY);

            return new Vector2(targetX, targetY);
        }

        private void FinishGuaranteedBlock()
        {
            // 1. Stop moving and restore operational capabilities

            Velocity = Vector2.zero;
            canDoAction = true;
            canThrow = true;

            // 2. If close to the ground, fall to the ground

            if (Position.y >= mlpObjectsData.PlayerIndentY - 0.5f)
            {
                Position.y = mlpObjectsData.PlayerIndentY;
                IsGrounded = true;
            }

            // 3. Determine the ability to pick up the ball and the landing animation based on whether it is on the ground.

            guaranteedBlockPickupLocked = !IsGrounded;
            canTakeInHands = !WithBall && !guaranteedBlockPickupLocked;
            PlayState(IsGrounded ? "blockEnd" : "fly1");

            // 4. End the super state

            EndSuper();
        }

        /// <summary>
        /// Perform alley-oop super.

        /// </summary>
        private void MakeAlleyOop()
        {
            // 1. Set the score refund ratio (energy will be refunded after some skills are lost)

            pendingScoreRefundFraction = skillDefinition.ScoreRefundFraction;
            pendingScoreRefundTimer = skillDefinition.ScoreRefundFraction > 0f ? 4f : 0f;

            // 2. Lock the operation, stop horizontal movement, and face the direction of attack

            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            Velocity.x = 0f;
            facingDirection = AttackTargetX - Position.x >= 0f ? 1f : -1f;

            // 3. Play the shooting animation on the ground first, then alley-oop after the animation ends; directly alley-oop in the air

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
        /// Start throwing alley-oops.

        /// </summary>
        private void StartAlleyOop()
        {
            // 1. Clear the pending execution mark and let the basketball enter the alley-oop flight state

            alleyOopPendingThrow = false;
            GameCore.Ball.AlleyOop(Side, Position.x - 20f * Side, Position.y - 30f, this);

            // 2. Release the ball, lock in pickups and shots

            WithBall = false;
            canTakeInHands = false;
            canThrow = false;

            // 3. Notify other players that the ball has been released

            GameCore.NotifyBallOthers();

            // 4. Play flight animation while in the air

            if (!IsGrounded)
            {
                PlayState("fly1");
            }
        }

        /// <summary>
        /// Completed the air-oop transmission.

        /// </summary>
        private void FinishAlleyTeleportOut()
        {
            // 1. Move the player to the starting position for dunking, facing the basket, hiding the character graphics
            Position = new Vector2(superDunkX, mlpObjectsData.AlleyOopY);
            facingDirection = -Side;
            graphicScaleMultiplier = 0f;

            // 2. Reset skeletal animation posture

            if (armature != null)
            {
                visualState = "pumpEnd";
                SetAnimationPlaybackSpeed(visualState);
                armature.StopAtStart("pumpEnd");
            }

            // 3. Play teleportation effects and sound effects to remove the basketball from the physics simulation

            teleportFx?.StartPlay(Position.x, Position.y);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PTeleport);
            GameCore.Ball.RemoveFromPhysics();

            // 4. Switch to the teleportation stage

            superPhase = SuperPhase.AlleyTeleportIn;
            superTimer = 0f;
            superDuration = 0.4f;
        }

        /// <summary>
        /// Perform a super dash.

        /// </summary>
        private void MakeSuperDash()
        {
            // 1. Find the position of the opponent holding the ball as a sprint target

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

            // 2. Track the position of the basketball or teammates when there is no opponent holding the ball.

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

            // 3. Select the sprint end point (one of two preset points) based on the current position and target position.

            var currentX = Position.x;
            var dashPoint = WithBall
                ? 0
                : Side < 0
                    ? currentX < targetX ? 0 : 1
                    : currentX > targetX ? 0 : 1;

            // 4. Set the sprint direction to remove the player from the game (cannot be collided)

            dashToRight = Side < 0 ? dashPoint == 0 : dashPoint == 1;
            removedFromPlay = true;
            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;

            // 5. Set the sprint start point, end point, and duration and enter the sprint phase

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
        /// Continue to super sprint.

        /// </summary>
        private void ContinueSuperDash()
        {
            // 1. Place the player to the end of the sprint, stop moving and land on the ground

            Position = superTargetPosition;
            Velocity = Vector2.zero;
            IsGrounded = true;

            // 2. Restore operational ability (allowed to pick up the ball if not holding the ball)

            canDoAction = true;
            canTakeInHands = !WithBall;
            canThrow = true;

            // 3. Play the landing animation to end the super state

            PlayState("md_end");
            EndSuper();
        }

        /// <summary>
        /// Update super status.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        private void UpdateSuper(float dt)
        {
            // 1. Execute different update logic according to the current super power stage

            switch (superPhase)
            {
                case SuperPhase.MegaTravel:
                    // 1a. Super dunk flight phase: interpolation moves the player to the target point

                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDunk();
                    }
                    break;
                case SuperPhase.MegaRecover:
                    // 1b. Super dunk recovery phase: interpolation moves the player to the landing point

                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    break;
                case SuperPhase.SuperDashTravel:
                    // 1c. Super sprint flight phase: interpolate movement and detect collisions

                    superTimer += dt;
                    Position = Vector2.Lerp(superStartPosition, superTargetPosition, Mathf.Clamp01(superTimer / superDuration));
                    UpdateSuperDashTravel();
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDash();
                    }
                    break;
                case SuperPhase.AlleyTeleportOut:
                    // 1d. All-air transfer stage: gradually shrink the character graphics

                    superTimer += dt;
                    graphicScaleMultiplier = Mathf.Clamp01(1f - superTimer / superDuration);
                    if (superTimer >= superDuration)
                    {
                        FinishAlleyTeleportOut();
                    }
                    break;
                case SuperPhase.AlleyTeleportIn:
                    // 1e. All-air transfer entry stage: gradually enlarge the character graphics

                    superTimer += dt;
                    graphicScaleMultiplier = Mathf.Clamp01(superTimer / superDuration);
                    if (superTimer >= superDuration)
                    {
                        ContinueSuperDunk();
                    }
                    break;
                case SuperPhase.GuaranteedBlockHold:
                    // 1f. Must block the hover phase: keep the player still

                    superTimer += dt;
                    Velocity = Vector2.zero;
                    if (superTimer >= superDuration)
                    {
                        FinishGuaranteedBlock();
                    }
                    break;
            }

            // 2. Update character graphic display

            UpdateGraphic();
        }

        /// <summary>
        /// Updated Super Sprint movement process.

        /// </summary>
        private void UpdateSuperDashTravel()
        {
            // 1. Traverse the opponent and detect sprint collision (stealing is triggered when the distance is less than 40 pixels)
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
                    // 1a. Record the collided opponent (to prevent repeated triggering) and try to steal

                    superDashHits.Add(opponentPlayer.PlayerNo);
                    if (opponentPlayer.GetBeStolen(currentX, false))
                    {
                        AcquireBallDuringSuperDash();
                    }
                }
            }

            // 2. Detect whether a basketball or teammate’s ball passes through the air

            var ball = GameCore.Ball;
            if (ball != null && ball.Position.y > mlpObjectsData.BasketHeight && !WithBall)
            {
                if (ball.IsInGame)
                {
                    // 2a. Pick up the basketball directly when passing it

                    if ((dashToRight && currentX > ball.Position.x) || (!dashToRight && currentX < ball.Position.x))
                    {
                        AcquireBallDuringSuperDash();
                    }
                }
                else if (dashTeammatePending && teamMate != null && ((dashToRight && currentX > teamMate.Position.x) || (!dashToRight && currentX < teamMate.Position.x)))
                {
                    // 2b. When passing by a teammate holding the ball, force the teammate’s ball to be released and picked up.

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
        /// Get the basketball during Super Dash.

        /// </summary>
        private void AcquireBallDuringSuperDash()
        {
            // 1. Skip if the ball is already held or the basketball does not exist

            if (WithBall || GameCore.Ball == null)
            {
                return;
            }

            // 2. Take the basketball into your hands and notify other players

            WithBall = true;
            canTakeInHands = false;
            attackJump = false;
            GameCore.Ball.TakeInHands(Side);
            GameCore.NotifyBallInHands(Side, playerNo);

            // 3. If it is a soul harvesting skill, activate the extra score bonus

            if (skillDefinition.SkillType == mlpCharacterSkillType.SoulReap && skillDefinition.FlatScoreBonus > 0)
            {
                flatScoreBonusPoints = Mathf.Max(flatScoreBonusPoints, skillDefinition.FlatScoreBonus);
                flatScoreBonusTimer = Mathf.Max(flatScoreBonusTimer, skillDefinition.BonusDuration);
                skillFx?.PlayBuff(Mathf.Min(skillDefinition.BonusDuration, 1.1f));
            }

            // 4. Display prompt text for Soul Harvest skill

            if (skillDefinition.SkillType == mlpCharacterSkillType.SoulReap)
            {
                GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
            }
        }

        /// <summary>
        /// Execute the shot.

        /// </summary>
        private void MakeThrow()
        {
            // 1. Lock the action to prevent continuous shooting

            canDoAction = false;
            actionLatch = Mathf.Max(actionLatch, 0.35f);
            canThrow = false;
            attackJump = false;
            WithBall = false;

            // 2. Try to trigger a dunk (if it is under the basket and meets the conditions), and return directly if successful.

            if (TryStartDunk())
            {
                return;
            }

            // 3. Calculate the basketball shot position: ground offset 20px, air offset 35px

            canTakeInHands = IsGrounded;
            var releaseOffset = IsGrounded ? 20f : 35f;
            if (IsGrounded)
            {
                pointOfThrow = Position.x;
            }

            var releaseX = Position.x - Side * releaseOffset;
            var releaseY = Position.y - 50f;

            // 4. Determine whether it is a three-pointer or a two-pointer (based on the distance between the shot position and the three-point line)

            var throwType = (pointOfThrow - mlpObjectsData.ThreePointsDistance) * Side >= 0f ? 0 : 6;

            // 5. Record shooting data, send signals, and let the basketball fly to the basket

            GameCore.MatchProcessor.Shoot(Side, IsHuman, throwType, playerNo);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Shoot, Side, playerNo);
            GameCore.NotifyPlayersBallShot(Side, playerNo);
            GameCore.Ball.Shoot(Side, releaseX, releaseY, Velocity.x, GetShotAccuracy());
            PlayState(IsGrounded ? "throw_land" : "fly1");
        }

        /// <summary>
        /// Start shooting from the ground.

        /// </summary>
        private void BeginFloorThrow()
        {
            // 1. Mark the ground shot to be executed

            pendingGroundThrow = true;

            // 2. Lock all operations to prevent repeated shots

            canDoAction = false;
            canTakeInHands = false;
            canThrow = false;
            actionLatch = Mathf.Max(actionLatch, 0.3f);

            // 3. Stop horizontal movement and play shooting preparation animation

            Velocity.x = 0f;
            PlayState("throw_land");
        }

        /// <summary>
        /// Start stealing.

        /// </summary>
        private void BeginSteal()
        {
            // 1. Prevent repeated triggering of steals

            if (stealAnimationActive)
            {
                return;
            }

            // 2. Lock the operation and start the tackling animation and timer

            canDoAction = false;
            pendingStealAction = true;
            stealAnimationActive = true;
            stealAttemptTimer = mlpObjectsData.StealFrameEventTime;   // Actual judgment time point

            stealAnimationTimer = mlpObjectsData.StealAnimationDuration; // Total animation duration

            // 3. Record and lock the direction (facing the opponent's direction when stealing)

            stealFacingDirection = facingDirection;
            facingDirection = stealFacingDirection;

            // 4. Stop moving, play the tackling animation, and play the sound effects

            actionLatch = Mathf.Max(actionLatch, mlpObjectsData.StealAnimationDuration);
            canTakeInHands = false;
            Velocity.x = 0f;
            PlayState("steal");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.StartSteal, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh, 0.7f);
        }

        /// <summary>
        /// Settlement steal attempt.

        /// </summary>
        private void ResolveStealAttempt()
        {
            // 1. If there are no pending steals, only clear the timer

            if (!pendingStealAction)
            {
                stealAttemptTimer = -1f;
                return;
            }

            // 2. Clear the steal judgment flag and timer

            stealAttemptTimer = -1f;
            pendingStealAction = false;

            // 3. Send a steal signal and try to steal the ball from the opponent

            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Steal, Side, playerNo);
            if (GameCore.TryStealBall(this, stealFacingDirection))
            {
                // 4. Added short-term operation lock when the steal is successful.

                actionLatch = Mathf.Max(actionLatch, 0.18f);
            }
        }

        /// <summary>
        /// Updated tackling animation.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        private void UpdateStealAnimation(float dt)
        {
            // 1. Lock orientation and horizontal movement

            facingDirection = stealFacingDirection;
            Velocity.x = 0f;

            // 2. Wait for the steal judgment time to arrive and then execute the judgment.

            if (stealAttemptTimer >= 0f)
            {
                stealAttemptTimer -= dt;
                if (stealAttemptTimer <= 0f)
                {
                    ResolveStealAttempt();          //Determine trigger point

                }
            }

            // 3. Complete the steal animation after the animation timer ends.

            stealAnimationTimer -= dt;
            if ((armature == null && stealAnimationTimer <= 0f) || stealAnimationTimer <= -0.2f)
            {
                FinishStealAnimation();
                return;
            }

            // 4. Update character graphics

            UpdateGraphic();
        }

        /// <summary>
        /// End steal animation.

        /// </summary>
        private void FinishStealAnimation()
        {
            // 1. Skip if the tackling animation is not activated

            if (!stealAnimationActive)
            {
                return;
            }

            // 2. If there are still unsettled steal judgments, execute the judgment first

            if (pendingStealAction)
            {
                ResolveStealAttempt();
            }

            // 3. Clear steal animation status and timer

            stealAnimationActive = false;
            stealAnimationTimer = -1f;
            stealAttemptTimer = -1f;

            // 4. Restore the ability to pick up the ball and operate it

            canTakeInHands = !WithBall && stunTimer <= 0f && !removedFromPlay;
            canDoAction = controller.ReadyForAction();
            actionLatch = Mathf.Max(actionLatch, 0f);

            // 5. Restore standby animation

            PlayState(WithBall ? "idle_wb" : "idle");
        }

        /// <summary>
        /// Cancel the steal animation.

        /// </summary>
        /// <param name="restorePickup">Whether to restore the pickup ability</param>

        private void CancelStealAnimation(bool restorePickup)
        {
            // 1. Clear steal animation status and all timers

            stealAnimationActive = false;
            pendingStealAction = false;
            stealAttemptTimer = -1f;
            stealAnimationTimer = -1f;

            // 2. If ball pickup is allowed to be resumed and ball pickup is currently possible, re-enable the ability to pick up balls.

            if (restorePickup && !WithBall && stunTimer <= 0f && !removedFromPlay)
            {
                canTakeInHands = true;
            }
        }

        /// <summary>
        /// Update orientation.
        /// </summary>
        private void UpdateFacing()
        {
            // 1. Determine the direction of facing the target: the default is towards the offensive direction, when defending, towards the ball carrier or basketball

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

            // 2. When the target is far enough away from the current position, update the facing direction

            var delta = faceTarget - Position.x;
            if (Mathf.Abs(delta) > 0.5f)
            {
                facingDirection = delta >= 0f ? 1f : -1f;
            }
        }

        /// <summary>
        /// Play the specified animation state.

        /// </summary>
        /// <param name="state">Animation state name</param>

        private void PlayState(string state)
        {
            // 1. Restore ball slot display during non-dunk animation

            if (!IsDunkAnimationState(state))
            {
                SetDunkBallSlotsHidden(false);
            }

            // 2. Set animation playback speed

            SetAnimationPlaybackSpeed(state);

            // 3. If it is already the current state, skip it (to avoid repeated playback)

            if (visualState == state)
            {
                return;
            }

            // 4. Update status and play skeletal animation

            visualState = state;
            armature?.Play(state);
        }

        /// <summary>
        /// Set the animation state to the starting frame.

        /// </summary>
        /// <param name="state">Animation state name</param>

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
            // 1. Skip if there is no change in status

            if (dunkBallSlotsHidden == hidden)
            {
                return;
            }

            // 2. Update the hidden state of the ball slot

            dunkBallSlotsHidden = hidden;
            if (armature == null)
            {
                return;
            }

            // 3. Control the display/hide of the foreground and background ball slots

            armature.SetSlotHidden("ball", hidden);
            armature.SetSlotHidden("ball_front", hidden);
        }

        private void SetAnimationPlaybackSpeed(string state)
        {
            // 1. If skeletal animation exists, set the corresponding playback speed according to the animation status.

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
        /// Animation frame event callback: Triggered when the skeletal animation plays to a specific frame, used to synchronize game logic (shooting, steal determination, dunk release, etc.).

        /// </summary>
        /// <param name="animationName">The name of the animation being played</param>

        /// <param name="eventName">The event tag name set in the animation editor</param>

        private void OnAnimationFrameEvent(string animationName, string eventName)
        {
            // 1. Immediately refresh the skeletal posture to ensure that the visual position is synchronized with the event timing

            armature?.RefreshPose();

            // 2. "throw" event: shot release frame → the time when the basketball leaves the player's hand

            if (eventName == "throw")
            {
                // 2a. If there is an alley-oop shot to be executed, execute the alley-oop first (throw the ball into the air for a teammate to dunk)

                if (alleyOopPendingThrow && WithBall)
                {
                    StartAlleyOop();
                    return;
                }

                // 2b. If there is a ground shot pending, perform a normal shot attempt.

                if (pendingGroundThrow && WithBall)
                {
                    pendingGroundThrow = false;
                    MakeThrow();
                    return;
                }
            }

            // 3. "action" event: steal action frame → determine whether the hand touches the ball when it reaches the farthest position

            if (eventName == "action" && pendingStealAction)
            {
                ResolveStealAttempt();
                return;
            }

            // 4. "mega" event: super dunk end frame → the dunk animation ends and the super power state ends

            if (eventName == "mega" && isSuperShot)
            {
                EndSuperDunk();
                return;
            }

            // 5. "dunk" event: dunk release frame → the time when the ball enters the basket to determine whether it is dunked

            if (eventName == "dunk" && isDunking)
            {
                ReleaseDunkBall();
            }
        }

        /// <summary>
        /// Animation completion callback.
        /// </summary>
        /// <param name="animationName">Animation name</param>

        /// <summary>
        /// Animation completion callback: Triggered when an animation has finished playing, used to connect to the next animation or restore the default state.

        /// </summary>
        /// <param name="animationName">The name of the completed animation</param>

        private void OnAnimationComplete(string animationName)
        {
            switch (animationName)
            {
                // 1. The block/fake action take-off animation is completed → Mark can enter the next stage

                case "blockStart":
                case "pumpStart":
                    blockPumpStartReady = true;
                    break;

                // 2. Block/fake landing animation completed → mark can resume free operation

                case "blockEnd":
                case "pumpEnd":
                    blockPumpEndReady = true;
                    break;

                // 3. The ground shot preparation animation is completed → execute the actual shot (check the alley-oop first)

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

                // 4. The steal animation is completed → end the steal state and resume movement

                case "steal":
                    FinishStealAnimation();
                    break;

                // 5. The dunk animation is completed → If the ball has not been released, it will be released (guaranteed mechanism)

                case "dunk1":
                case "dunk2":
                case "dunk3":
                    if (isDunking && !dunkReleased)
                    {
                        ReleaseDunkBall();
                    }
                    break;

                // 6. Super power sprint take-off animation completed → switch to air gliding animation

                case "md_start":
                    PlayState("md_mid");
                    break;

                // 7. Super power sprint landing animation completed → Resume standby animation

                case "md_end":
                    PlayState(WithBall ? "idle_wb" : "idle");
                    break;
            }
        }

        private void UpdateSkillTimers(float dt)
        {
            // 1. Update the extra score bonus timer (the bonus points will be cleared when the time comes)

            if (flatScoreBonusTimer > 0f)
            {
                flatScoreBonusTimer = Mathf.Max(0f, flatScoreBonusTimer - dt);
                if (flatScoreBonusTimer <= 0f)
                {
                    flatScoreBonusPoints = 0;
                }
            }

            // 2. Update the movement acceleration timer (clear the score bonus flag when it expires)

            if (moveBuffTimer > 0f)
            {
                moveBuffTimer = Mathf.Max(0f, moveBuffTimer - dt);
                if (moveBuffTimer <= 0f)
                {
                    moveBuffScoreBonusAvailable = false;
                }
            }

            // 3. Update the score refund timer (clear the refund ratio when the time comes)

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
            // 1. Skip if magnet is not active

            if (reboundMagnetTimer <= 0f)
            {
                return;
            }

            // 2. Decrement magnet duration

            reboundMagnetTimer = Mathf.Max(0f, reboundMagnetTimer - dt);

            // 3. Immediately turn off the magnet if the player is unavailable

            var ball = GameCore.Ball;
            if (WithBall || stunTimer > 0f || removedFromPlay || isDunking || !CanUseReboundMagnetBall(ball))
            {
                reboundMagnetTimer = 0f;
                return;
            }

            // 4. Calculate the distance between the catching point and the basketball

            var catchPoint = Position + new Vector2(0f, -58f);
            var delta = catchPoint - ball.Position;
            if (Mathf.Abs(delta.x) <= ReboundMagnetCatchDistanceX &&
                Mathf.Abs(delta.y) <= ReboundMagnetCatchDistanceY)
            {
                // 4a. The basketball enters the catching range, pick up the basketball and display a prompt

                reboundMagnetTimer = 0f;
                TakeBallInHands();
                canDoAction = false;
                actionLatch = Mathf.Max(actionLatch, 0.18f);
                GameCore.ShowHudBonusNotice(skillDefinition.ScoreNotice, 0.95f);
                mlpAudio.Instance?.Play(mlpAssets.Sounds.BSteel, 0.85f);
                return;
            }

            // 5. Attract the speed of the basketball towards the player (the farther away, the faster)

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
            // 1. Select the base speed according to whether you are holding the ball, and multiply it by the acceleration multiplier when there is movement acceleration.

            var baseSpeed = WithBall ? mlpObjectsData.PlayerMoveWithBall : mlpObjectsData.PlayerMove;
            return baseSpeed * (moveBuffTimer > 0f ? skillDefinition.MoveSpeedMultiplier : 1f);
        }

        private float GetDashSpeed()
        {
            // 1. Calculate sprint speed: 1.7 times the basic movement speed, with additional bonus when there is movement acceleration

            var multiplier = moveBuffTimer > 0f ? Mathf.Lerp(1f, skillDefinition.MoveSpeedMultiplier, 0.65f) : 1f;
            return mlpObjectsData.PlayerMove * 1.7f * multiplier;
        }

        private float GetShotAccuracy()
        {
            // 1. Directly return high-precision values in the tutorial perfect shooting mode

            if (tutorialPerfectShotPrimed)
            {
                tutorialPerfectShotPrimed = false;
                return -0.5f;
            }

            // 2. Basic accuracy plus accuracy correction value of movement acceleration

            var resolvedAccuracy = accuracy;
            if (moveBuffTimer > 0f)
            {
                resolvedAccuracy += skillDefinition.AccuracyModifier;
            }

            return Mathf.Max(-0.05f, resolvedAccuracy);
        }

        private void GrantSuperChargeFraction(float fraction)
        {
            // 1. Get the super cooling time, and refresh the ready status directly when there is no cooling time.
            var cooldown = EffectiveSuperCoolDown;
            if (cooldown <= 0f)
            {
                RefreshQuickTestSuperReady();
                return;
            }

            // 2. If the ratio is invalid, skip it.

            if (fraction <= 0f)
            {
                return;
            }

            // 3. Increase charging progress and update energy bar display

            superChargeTime = Mathf.Min(cooldown, superChargeTime + cooldown * fraction);
            readyForSuper = superChargeTime >= cooldown;
            energyBar?.SetCharge(superChargeTime / cooldown);
        }

        /// <summary>
        /// Update the graphical display.

        /// </summary>
        private void UpdateGraphic()
        {
            // 1. Calculate the character scaling (character-specific scaling × universal scaling ratio), and the facing direction is controlled by the positive and negative values of scaleX

            var gameplayScale = mlpPlayersData.GetCharacterGameplayScaleMultiplier(characterId) * graphicScaleMultiplier;
            graphic.transform.position = mlpConstants.PixelToWorldSnapped(Position.x, Position.y, GraphicDepthBase + renderDepthBias);
            graphic.transform.localScale = new Vector3(
                mlpConstants.UnitsPerPixel * facingDirection * gameplayScale,
                mlpConstants.UnitsPerPixel * gameplayScale,
                1f);

            // 2. Update shadow appearance (may switch to red shadow when skill is activated)

            UpdateShadowAppearance();

            // 3. Show/hide shadows and scale shadow size based on player height (the taller, the smaller)

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
            // 1. Skip if there is no renderer

            if (shadowRenderer == null)
            {
                return;
            }

            // 2. Select highlight or default shadow map according to skill activation status

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
        /// Create alternate character avatars.

        /// </summary>
        private void CreateFallbackAvatar()
        {
            // 1. Create a new game object as an alternate character body

            var body = new GameObject("FallbackBody");
            // 2. Mount it under the character graphics and add the sprite renderer

            body.transform.SetParent(graphic.transform, false);
            var renderer = body.AddComponent<SpriteRenderer>();
            renderer.sprite = mlpGameplaySpriteLoader.LoadGameplaySprite(
                mlpAssets.Images.GameplayImages.FallbackAvatar,
                0.5f,
                0.5f,
                mlpAtlasCache.Instance.Gameplay,
                "BallClipMsg0000");
            // 3. Set colors, adjust sorting and position scaling according to the team

            renderer.color = teamIndex == 0 ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.2f, 0.45f, 1f);
            renderer.sortingOrder = 20;
            body.transform.localPosition = new Vector3(0f, -80f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.8f, 1f);
        }

        /// <summary>
        /// Start sprinting.

        /// </summary>
        /// <param name="direction">Movement or throwing direction (-1 or 1)</param>

        private void StartDash(int direction)
        {
            // 1. Set sprint direction and duration

            dashDirection = direction;
            dashTimer = 0.14f;
            // 2. Set sprint speed and clear input buffer

            Velocity.x = GetDashSpeed() * direction;
            actionLatch = Mathf.Max(actionLatch, 0.15f);
            bufferedDashDirection = 0;
            dashBufferTimer = 0f;

            // 3. Play sprint animation and sound effects, and send sprint signals

            PlayState("dash");
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Dash, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PDash);
        }

        /// <summary>
        /// Update sprint buffer.

        /// </summary>
        /// <param name="dt">Time increment (seconds)</param>

        private void UpdateDashBuffer(float dt)
        {
            // 1. If there is new input, record the direction and buffering time

            if (controller.CurrentDash != 0)
            {
                bufferedDashDirection = controller.CurrentDash;
                dashBufferTimer = mlpObjectsData.DashInputBuffer;
                return;
            }

            // 2. Direction of clearing records after the buffer period ends

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
        /// Start playing defense or feinting.

        /// </summary>
        private void BeginBlockOrPump()
        {
            // 1. Determine whether it is a fake (when holding the ball) or a block (without the ball)

            blockPumpIsPump = WithBall;
            blockPumpPhase = BlockPumpPhase.Starting;
            blockPumpTimer = blockPumpIsPump ? mlpObjectsData.PumpStartDuration : mlpObjectsData.BlockStartDuration;
            blockPumpStartReady = false;
            blockPumpEndReady = false;

            // 2. Stop horizontal movement and lock the operation time

            Velocity.x = 0f;
            actionLatch = Mathf.Max(actionLatch, blockPumpTimer);
            // 3. It is prohibited to pick up the ball when blocking a shot, and send a fake action signal when making a fake move.
            if (!blockPumpIsPump)
            {
                canTakeInHands = false;
            }
            else
            {
                GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Pump, Side, playerNo);
            }

            // 4. Play the corresponding jumping animation

            PlayState(blockPumpIsPump ? "pumpStart" : "blockStart");
        }

        /// <summary>
        /// Activate jump block.

        /// </summary>
        private void ActivateJumpBlock()
        {
            // 1. Activate jump block collision detection and prohibit picking up the ball.
            jumpBlockActive = true;
            canTakeInHands = false;
        }

        /// <summary>
        /// Determine whether to prepare for a jump block.
        /// </summary>
        /// <returns>Returns true when the condition is met; otherwise returns false. </returns>
        private bool ShouldPrimeJumpBlock()
        {
            // 1. If there is no need to block the shot, return directly
            if (!needBlock)
            {
                return false;
            }

            // 2. Check if the ball carrier is an opponent or if the basketball is flying toward the basket

            var holder = GameCore.FindBallHolder();
            if (holder != null)
            {
                return holder.Side != Side;
            }

            var ball = GameCore.Ball;
            return ball != null && ball.State == "shooting" && ball.Side != Side;
        }

        /// <summary>
        /// Updated jump blocking threat status.

        /// </summary>
        private void UpdateJumpBlockThreat()
        {
            // 1. If a shot is being blocked but the conditions are no longer met, turn off blocking collision

            if (jumpBlockActive && !ShouldPrimeJumpBlock())
            {
                jumpBlockActive = false;
            }
        }

        /// <summary>
        /// Update defense or feint status.

        /// </summary>
        /// <param name="dt">Time increment (seconds)</param>

        private void UpdateBlockOrPump(float dt)
        {
            // 1. Stop moving horizontally and keep the ball in your hands while holding the ball

            Velocity.x = 0f;
            if (WithBall)
            {
                GameCore.Ball.TakeInHands(Side);
            }

            // 2. Jumping phase: wait for the animation to complete or the timer to end, and switch to the holding phase.
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

            // 3. Holding phase: wait for the player to release the button and switch to the landing phase

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

            // 4. Landing stage: wait for the animation to complete or the timer to end, and then return to standby state

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
        /// Try to start dunking.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        private bool TryStartDunk()
        {
            // 1. Get the dunk type (returns 0 if it is not in the dunk area)

            var dunkType = GetDunkType();
            if (dunkType == 0)
            {
                return false;
            }

            // 2. Enter dunk state, send dunk signal and play sound effects

            BeginDunkState(dunkType);
            GameCore.PlayerSignals.Dispatch(mlpPlayerSignalType.Dunk, Side, playerNo);
            mlpAudio.Instance?.Play(mlpAssets.Sounds.PSwoosh, 0.8f);
            return true;
        }

        private bool TryTutorialPutbackDunk()
        {
            // 1. Check whether the tutorial compensation conditions are met (prepared, not on the ground, basketball exists, etc.)

            var ball = GameCore.Ball;
            if (!tutorialPutbackDunkPrimed || WithBall || IsGrounded || ball == null || !ball.IsInGame || removedFromPlay || isSuperShot || isDunking)
            {
                return false;
            }

            // 2. Check whether the basketball status allows back-up deductions

            if (ball.State == "inHands" || ball.State == "score" || ball.State == "alleyOop" || !IsTutorialPutbackBallInWindow(ball))
            {
                return false;
            }

            // 3. Check whether the distance between the player and the basketball is within the compensation range

            var delta = ball.Position - Position;
            if (Mathf.Abs(delta.x) > TutorialPutbackCatchWindowX ||
                Mathf.Abs(delta.y) > TutorialPutbackCatchWindowY ||
                Position.y > mlpObjectsData.DunkZone2Y + TutorialPutbackDunkYBonus)
            {
                return false;
            }

            // 4. Get the tutorial rebate type and set a high completion rate

            var dunkType = GetTutorialPutbackDunkType();
            if (dunkType == 0)
            {
                return false;
            }

            // 5. Clear the reserve mark, remove the basketball from physics, and enter dunk state

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
            // 1. Mark the dunk state and lock the operation

            isDunking = true;
            canDoAction = false;
            canThrow = false;
            dunkReleased = false;
            dunkTimer = 0f;
            dunkDuration = DunkTravelDuration(dunkType);
            dunkReleaseTime = DunkReleaseTime(dunkType);
            // 2. Record the starting position and target point under the basket, and stop moving
            dunkStartPosition = Position;
            dunkTargetPosition = new Vector2(DunkTargetX(), mlpObjectsData.DunkY);
            Velocity = Vector2.zero;
            dashTimer = 0f;
            dashDirection = 0;
            actionLatch = Mathf.Max(actionLatch, DunkActionLockDuration(dunkType));
            // 3. It is forbidden to pick up the ball, display the ball slot, and play the corresponding type of dunk animation.
            canTakeInHands = false;
            SetDunkBallSlotsHidden(false);
            PlayState("dunk" + dunkType);
        }

        /// <summary>
        /// Get dunk type.
        /// </summary>
        /// <returns>The calculated dunk type. </returns>
        private int GetDunkType()
        {
            // 1. Calculate the left and right boundaries of the paint area (area under the basket)

            var paintStart = Side == 1 ? mlpObjectsData.PaintStartX : mlpConstants.Width - mlpObjectsData.PaintMiddleX;
            var paintMiddle = Side == 1 ? mlpObjectsData.PaintMiddleX : mlpConstants.Width - mlpObjectsData.PaintStartX;
            var tutorialDunkYBonus = tutorialPerfectDunkPrimed ? 36f : 0f;
            // 2. When deep in the paint area and the height is sufficient, dunk type 2 or 3 will be returned randomly.

            if (Position.x >= paintStart && Position.x <= paintMiddle && Position.y <= mlpObjectsData.DunkZone1Y + tutorialDunkYBonus)
            {
                return 1 + Mathf.RoundToInt(2f * Random.value);
            }

            // 3. When at the edge of the paint area and the height is sufficient, return to basic dunk type 1
            if ((Position.x - paintStart) * Side < 0f && Position.y <= mlpObjectsData.DunkZone2Y + tutorialDunkYBonus)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Get the dunk target X coordinate.
        /// </summary>
        /// <returns>The calculated X coordinate of the target. </returns>
        private float DunkTargetX()
        {
            return Side == 1 ? mlpObjectsData.DunkX : mlpConstants.Width - mlpObjectsData.DunkX;
        }

        /// <summary>
        /// Get dunk duration.
        /// </summary>
        /// <param name="dunkType">Dunk type</param>
        /// <returns>The calculated duration. </returns>
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
        /// Update dunk status.
        /// </summary>
        /// <param name="dt">Time increment (seconds)</param>

        private void UpdateDunk(float dt)
        {
            // 1. Advance the dunk timer and calculate the progress (0~1)

            dunkTimer += dt;
            var t = dunkDuration > 0f ? Mathf.Clamp01(dunkTimer / dunkDuration) : 1f;

            // 2. Use the easing function to interpolate the player's position (flying from the starting point to the target point under the basket)

            Position = Vector2.Lerp(dunkStartPosition, dunkTargetPosition, DunkTravelEase(t));
            IsGrounded = false;

            // 3. When the release time point is reached, release the basketball (determine whether to dunk in)

            if (!dunkReleased && dunkTimer >= dunkReleaseTime)
            {
                ReleaseDunkBall();
            }

            // 4. Update player sprite position

            UpdateGraphic();

            // 5. The animation will continue before it ends, and the dunk state will be reset after it ends.

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
        /// Unleash the dunk basketball.

        /// </summary>
        private void ReleaseDunkBall()
        {
            // 1. Prevent repeated release

            if (dunkReleased)
            {
                return;
            }

            // 2. The mark is released, hiding the ball slot in the hand when dunking

            dunkReleased = true;
            SetDunkBallSlotsHidden(true);

            // 3. Calculate the probability of successful dunk (a perfect dunk in the tutorial must be successful, otherwise the probability will be based on AI skill level)

            var completionChance = tutorialPerfectDunkPrimed
                ? 1f
                : tutorialDunkCompletionChanceOverride >= 0f
                    ? Mathf.Max(chanceToCompleteDunk, tutorialDunkCompletionChanceOverride)
                    : chanceToCompleteDunk;
            completionChance = Mathf.Clamp01(completionChance);
            tutorialPerfectDunkPrimed = false;
            tutorialDunkCompletionChanceOverride = -1f;

            // 4. Randomly determine whether the dunk is successful and notify the game processor and basketball object

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
        /// The ability to pick up the ball is restored when the conditions are met.

        /// </summary>
        private void RestoreBallPickupIfReady()
        {
            // 1. Skip if the ball can already be picked up or is in a state where the ball cannot be picked up.
            if (canTakeInHands || stunTimer > 0f || stealAnimationActive || stealAttemptTimer >= 0f || actionLatch > 0f)
            {
                return;
            }

            // 2. If the ball is locked after a certain block, it will be unlocked after landing.
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
        /// Determine if it is under the backboard.
        /// </summary>
        /// <returns>Returns true if it is under the backboard; otherwise returns false. </returns>
        private bool IsUnderGlass()
        {
            return Side == -1
                ? Position.x > mlpConstants.Width - 200f && Position.x < mlpConstants.Width - 100f
                : Position.x > 100f && Position.x < 200f;
        }

        /// <summary>
        /// Determine whether the swept point line segment intersects the rectangle.

        /// </summary>
        /// <param name="start">Start point of ball trajectory</param>

        /// <param name="end">End point of ball trajectory</param>
        /// <param name="minX">Test the left edge of the rectangle</param>
        /// <param name="maxX">Test the right edge of the rectangle</param>

        /// <param name="minY">Test the lower boundary of the rectangle</param>

        /// <param name="maxY">Test the upper boundary of the rectangle</param>
        /// <returns>Returns true if they intersect; otherwise returns false. </returns>
        private static bool SweptPointIntersectsRect(Vector2 start, Vector2 end, float minX, float maxX, float minY, float maxY)
        {
            // 1. If the starting point or end point is within the rectangle, it intersects directly

            if (PointInsideRect(start, minX, maxX, minY, maxY) || PointInsideRect(end, minX, maxX, minY, maxY))
            {
                return true;
            }

            // 2. Use the Cohen-Sutherland clipping algorithm to detect whether the line segment and the rectangle intersect.
            var direction = end - start;
            var tMin = 0f;
            var tMax = 1f;
            return ClipSegment(-direction.x, start.x - minX, ref tMin, ref tMax) &&
                   ClipSegment(direction.x, maxX - start.x, ref tMin, ref tMax) &&
                   ClipSegment(-direction.y, start.y - minY, ref tMin, ref tMax) &&
                   ClipSegment(direction.y, maxY - start.y, ref tMin, ref tMax);
        }

        /// <summary>
        /// Determine whether the point is within the rectangle.
        /// </summary>
        /// <param name="point">Point to be detected</param>
        /// <param name="minX">Test the left edge of the rectangle</param>
        /// <param name="maxX">Test the right edge of the rectangle</param>

        /// <param name="minY">Test the lower boundary of the rectangle</param>

        /// <param name="maxY">Test the upper boundary of the rectangle</param>
        /// <returns>Returns true if the point is within the rectangle; otherwise returns false. </returns>
        private static bool PointInsideRect(Vector2 point, float minX, float maxX, float minY, float maxY)
        {
            return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
        }

        /// <summary>
        /// Clipping line segments (Cohen-Sutherland algorithm).

        /// Used to determine whether the sweep trajectory segment passes through the rectangular area.

        /// </summary>
        /// <param name="p">Direction component of the clipping plane</param>

        /// <param name="q">Distance component of clipping plane</param>

        /// <param name="tMin">Current minimum parameter clipping value</param>

        /// <param name="tMax">Current maximum parameter clipping value</param>
        /// <returns>Returns true if the line segment is not completely clipped; otherwise returns false. </returns>
        private static bool ClipSegment(float p, float q, ref float tMin, ref float tMax)
        {
            // 1. When the direction component is zero, the line segment is parallel to the clipping boundary, check whether it is on the inside

            if (Mathf.Approximately(p, 0f))
            {
                return q >= 0f;
            }

            // 2. Calculate intersection parameters (distance/direction)
            var ratio = q / p;
            if (p < 0f)
            {
                // 3. Enter from the negative direction: update tMin (entry point), if it exceeds the range, it will not intersect.
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
                // 4. Enter from the positive direction: update tMax (leaving point), if it exceeds the range, it will not intersect.
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
