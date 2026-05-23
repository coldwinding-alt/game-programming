using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushRuntimeContext
    {
        public Transform Root;
        public rimrushSceneBindings SceneBindings;
        public rimrushGameplayBindings GameplayBindings;
        public rimrushHudSceneView HudView;
    }

    public sealed class rimrushSceneBindings : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private rimrushFixedResolutionPresenter presenter;
        [SerializeField] private rimrushAudio audioComponent;
        [SerializeField] private Transform persistentRoot;
        [SerializeField] private Transform overlayRoot;
        [SerializeField] private rimrushMenuShellView menuShell;
        [SerializeField] private rimrushHudSceneView hudView;
        [SerializeField] private rimrushGameplayBindings gameplayBindings;

        public Camera MainCamera => mainCamera;
        public rimrushFixedResolutionPresenter Presenter => presenter;
        public rimrushAudio Audio => audioComponent;
        public Transform PersistentRoot => persistentRoot;
        public Transform OverlayRoot => overlayRoot;
        public rimrushMenuShellView MenuShell => menuShell;
        public rimrushHudSceneView HudView => hudView;
        public rimrushGameplayBindings GameplayBindings => gameplayBindings;

        public rimrushRuntimeContext CreateGameplayContext()
        {
            return new rimrushRuntimeContext
            {
                Root = gameplayBindings != null && gameplayBindings.Root != null
                    ? gameplayBindings.Root
                    : persistentRoot,
                SceneBindings = this,
                GameplayBindings = gameplayBindings,
                HudView = hudView != null ? hudView : gameplayBindings != null ? gameplayBindings.HudView : null
            };
        }

        public void ResolveMissingReferences()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (presenter == null)
            {
                presenter = GetComponent<rimrushFixedResolutionPresenter>();
            }

            if (audioComponent == null)
            {
                audioComponent = GetComponentInChildren<rimrushAudio>(true);
            }

            if (persistentRoot == null)
            {
                persistentRoot = transform;
            }

            if (menuShell == null)
            {
                menuShell = GetComponentInChildren<rimrushMenuShellView>(true);
            }

            if (hudView == null)
            {
                hudView = GetComponentInChildren<rimrushHudSceneView>(true);
            }

            if (gameplayBindings == null)
            {
                gameplayBindings = GetComponentInChildren<rimrushGameplayBindings>(true);
            }
        }
    }

    public sealed class rimrushGameplayBindings : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private rimrushArenaView arenaView;
        [SerializeField] private rimrushBasketView leftBasketView;
        [SerializeField] private rimrushBasketView rightBasketView;
        [SerializeField] private rimrushBallView ballView;
        [SerializeField] private rimrushPlayerView leftPlayerView;
        [SerializeField] private rimrushPlayerView rightPlayerView;
        [SerializeField] private Transform leftNeutralSpawn;
        [SerializeField] private Transform rightNeutralSpawn;
        [SerializeField] private Transform leftServeSpawn;
        [SerializeField] private Transform rightServeSpawn;
        [SerializeField] private rimrushEnergyBarSceneView energyBarSlot0;
        [SerializeField] private rimrushEnergyBarSceneView energyBarSlot1;
        [SerializeField] private rimrushEnergyBarSceneView energyBarSlot2;
        [SerializeField] private rimrushTeleportFxView leftTeleportFxView;
        [SerializeField] private rimrushTeleportFxView rightTeleportFxView;
        [SerializeField] private rimrushShieldView leftShieldView;
        [SerializeField] private rimrushShieldView rightShieldView;
        [SerializeField] private rimrushHudSceneView hudView;

        public Transform Root => root != null ? root : transform;
        public rimrushArenaView ArenaView => arenaView;
        public rimrushBasketView LeftBasketView => leftBasketView;
        public rimrushBasketView RightBasketView => rightBasketView;
        public rimrushBallView BallView => ballView;
        public rimrushHudSceneView HudView => hudView;

        public rimrushPlayerView GetPlayerView(int side)
        {
            return side == -1 ? leftPlayerView : rightPlayerView;
        }

        public Vector2 GetSpawnPosition(int side, bool serve)
        {
            var spawn = serve
                ? (side == -1 ? leftServeSpawn : rightServeSpawn)
                : (side == -1 ? leftNeutralSpawn : rightNeutralSpawn);
            if (spawn == null)
            {
                return new Vector2(rimrushConstants.Width2, rimrushObjectsData.PlayerIndentY);
            }

            return rimrushConstants.WorldToPixel(spawn.position);
        }

        public rimrushEnergyBarSceneView GetEnergyBarView(int controllerSlot)
        {
            switch (controllerSlot)
            {
                case 1:
                    return energyBarSlot1;
                case 2:
                    return energyBarSlot2;
                default:
                    return energyBarSlot0;
            }
        }

        public rimrushTeleportFxView GetTeleportFxView(int side)
        {
            return side == -1 ? leftTeleportFxView : rightTeleportFxView;
        }

        public rimrushShieldView GetShieldView(int side)
        {
            return side == -1 ? leftShieldView : rightShieldView;
        }
    }

    public sealed class rimrushArenaView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer graphicRenderer;

        public GameObject Root => gameObject;
        public SpriteRenderer GraphicRenderer => graphicRenderer;

        public static rimrushArenaView CreateRuntimeFallback(Transform parent)
        {
            var go = rimrushRender.Sprite(
                "ArenaObject",
                rimrushAtlasCache.Instance.Gameplay,
                "0bg_gameplay0000",
                -299f,
                0f,
                0f,
                0f,
                0,
                parent);
            var view = go.AddComponent<rimrushArenaView>();
            view.graphicRenderer = go.GetComponent<SpriteRenderer>();
            return view;
        }
    }

    public sealed class rimrushBasketView : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private SpriteRenderer basketRenderer;
        [SerializeField] private SpriteRenderer frontEarRenderer;
        [SerializeField] private LineRenderer[] netLines;

        public Transform Root => root != null ? root : transform;
        public SpriteRenderer BasketRenderer => basketRenderer;
        public SpriteRenderer FrontEarRenderer => frontEarRenderer;
        public IReadOnlyList<LineRenderer> NetLines => netLines;

        public static rimrushBasketView CreateRuntimeFallback(int side, Transform parent)
        {
            var container = new GameObject(side == -1 ? "BasketLeftRuntimeView" : "BasketRightRuntimeView");
            container.transform.SetParent(parent, false);
            var view = container.AddComponent<rimrushBasketView>();

            var center = side == -1 ? rimrushObjectsData.BasketCenter : rimrushObjectsData.BasketCenter2;
            var graphic = new GameObject(side == -1 ? "BasketLeft" : "BasketRight");
            graphic.transform.SetParent(container.transform, false);
            rimrushRender.ApplyPixelTransform(graphic.transform, center, rimrushObjectsData.BasketHeight, 0.05f);
            graphic.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f),
                rimrushConstants.UnitsPerPixel,
                1f);

            var basket = new GameObject("BasketGraphic");
            basket.transform.SetParent(graphic.transform, false);
            var basketRenderer = basket.AddComponent<SpriteRenderer>();
            basketRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("BasketGraphic0000", 0.7f, 0.93f);
            basketRenderer.sortingOrder = 4;

            var frontEar = new GameObject(side == -1 ? "FrontEarLeft" : "FrontEarRight");
            frontEar.transform.SetParent(container.transform, false);
            var frontEarRenderer = frontEar.AddComponent<SpriteRenderer>();
            frontEarRenderer.sprite = rimrushAtlasCache.Instance.Gameplay.Sprite("FrontEar0000", 0.5f, 0.5f);
            frontEarRenderer.sortingOrder = 60;
            rimrushRender.ApplyPixelTransform(frontEar.transform, center, rimrushObjectsData.BasketHeight);
            frontEar.transform.localScale = new Vector3(
                rimrushConstants.UnitsPerPixel * (side == -1 ? 1f : -1f),
                rimrushConstants.UnitsPerPixel,
                1f);

            var lineList = new List<LineRenderer>();
            for (var i = 0; i < 10; i++)
            {
                var lineObject = new GameObject($"NetLine{i}");
                lineObject.transform.SetParent(container.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.018f;
                line.endWidth = 0.018f;
                line.sharedMaterial = rimrushSharedMaterialCache.GetSpritesDefault(Texture2D.whiteTexture);
                line.startColor = Color.white;
                line.endColor = Color.white;
                line.sortingOrder = 55;
                lineList.Add(line);
            }

            view.root = graphic.transform;
            view.basketRenderer = basketRenderer;
            view.frontEarRenderer = frontEarRenderer;
            view.netLines = lineList.ToArray();
            return view;
        }
    }

    public sealed class rimrushBallView : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private SpriteRenderer graphicRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;

        public GameObject Container => gameObject;
        public Transform Root => root != null ? root : transform;
        public SpriteRenderer GraphicRenderer => graphicRenderer;
        public SpriteRenderer ShadowRenderer => shadowRenderer;

        public static rimrushBallView CreateRuntimeFallback(Transform parent)
        {
            var container = new GameObject("BallRuntimeView");
            container.transform.SetParent(parent, false);
            var view = container.AddComponent<rimrushBallView>();

            var graphic = new GameObject("BallObject");
            graphic.transform.SetParent(container.transform, false);
            var graphicRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicRenderer.sortingOrder = 50;
            rimrushRender.ApplyPixelTransform(graphic.transform, rimrushConstants.Width2, rimrushObjectsData.BallIndentYCenter, 0.2f);

            var shadow = rimrushRender.Sprite(
                "BallShadow",
                rimrushAtlasCache.Instance.Gameplay,
                "ShadowMC0002",
                rimrushConstants.Width2,
                rimrushObjectsData.FloorY,
                0.5f,
                0.5f,
                3,
                container.transform);
            shadow.transform.localScale *= 0.7f;

            view.root = graphic.transform;
            view.graphicRenderer = graphicRenderer;
            view.shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            return view;
        }
    }

    public sealed class rimrushPlayerView : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private Transform armatureMount;
        [SerializeField] private SpriteRenderer fallbackRenderer;

        public GameObject Container => gameObject;
        public Transform Root => root != null ? root : transform;
        public SpriteRenderer ShadowRenderer => shadowRenderer;
        public Transform ArmatureMount => armatureMount != null ? armatureMount : transform;
        public SpriteRenderer FallbackRenderer => fallbackRenderer;

        public void ClearRuntimeVisuals()
        {
            if (armatureMount != null)
            {
                for (var i = armatureMount.childCount - 1; i >= 0; i--)
                {
                    var child = armatureMount.GetChild(i);
                    if (Application.isPlaying)
                    {
                        Object.Destroy(child.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled = false;
                fallbackRenderer.sprite = null;
            }
        }

        public static rimrushPlayerView CreateRuntimeFallback(string name, int playerNo, Transform parent)
        {
            var container = new GameObject($"{name}RuntimeView");
            container.transform.SetParent(parent, false);
            var view = container.AddComponent<rimrushPlayerView>();

            var graphic = new GameObject(name);
            graphic.transform.SetParent(container.transform, false);

            var shadow = rimrushRender.Sprite(
                $"{name}_Shadow",
                rimrushAtlasCache.Instance.Gameplay,
                playerNo == 0 ? "ShadowMC0000" : "ShadowMC0001",
                0f,
                0f,
                0.5f,
                0.5f,
                2,
                container.transform);

            var armatureMount = new GameObject($"{name}_ArmatureMount");
            armatureMount.transform.SetParent(graphic.transform, false);
            armatureMount.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                graphic.transform,
                new Vector3(0f, -35f, 0f));

            var fallback = new GameObject($"{name}_Fallback");
            fallback.transform.SetParent(graphic.transform, false);
            var fallbackRenderer = fallback.AddComponent<SpriteRenderer>();
            fallbackRenderer.sortingOrder = 20;
            fallbackRenderer.enabled = false;

            view.root = graphic.transform;
            view.shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            view.armatureMount = armatureMount.transform;
            view.fallbackRenderer = fallbackRenderer;
            return view;
        }
    }

    public sealed class rimrushTeleportFxView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform blackNode;
        [SerializeField] private SpriteRenderer blackRenderer;
        [SerializeField] private SpriteRenderer centerRenderer;
        [SerializeField] private SpriteRenderer whiteRenderer;
        [SerializeField] private SpriteRenderer animRenderer;

        public GameObject Root => root != null ? root : gameObject;
        public Transform BlackNode => blackNode;
        public SpriteRenderer BlackRenderer => blackRenderer;
        public SpriteRenderer CenterRenderer => centerRenderer;
        public SpriteRenderer WhiteRenderer => whiteRenderer;
        public SpriteRenderer AnimRenderer => animRenderer;

        public static rimrushTeleportFxView CreateRuntimeFallback(Transform parent)
        {
            var graphic = new GameObject("TeleportFx");
            graphic.transform.SetParent(parent, false);
            var view = graphic.AddComponent<rimrushTeleportFxView>();
            view.root = graphic;

            var blackNode = new GameObject("TeleportBlack").transform;
            blackNode.SetParent(graphic.transform, false);
            var blackRenderer = blackNode.gameObject.AddComponent<SpriteRenderer>();
            blackRenderer.sortingOrder = 74;
            blackRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport10000");

            var centerNode = new GameObject("TeleportCenter");
            centerNode.transform.SetParent(blackNode, false);
            var centerRenderer = centerNode.AddComponent<SpriteRenderer>();
            centerRenderer.sortingOrder = 75;
            centerRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport20000");

            var animNode = new GameObject("TeleportAnim");
            animNode.transform.SetParent(graphic.transform, false);
            var animRenderer = animNode.AddComponent<SpriteRenderer>();
            animRenderer.sortingOrder = 76;

            var whiteNode = new GameObject("TeleportWhite");
            whiteNode.transform.SetParent(graphic.transform, false);
            var whiteRenderer = whiteNode.AddComponent<SpriteRenderer>();
            whiteRenderer.sortingOrder = 77;
            whiteRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            view.blackNode = blackNode;
            view.blackRenderer = blackRenderer;
            view.centerRenderer = centerRenderer;
            view.whiteRenderer = whiteRenderer;
            view.animRenderer = animRenderer;
            return view;
        }
    }

    public sealed class rimrushShieldView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private SpriteRenderer blurRenderer;
        [SerializeField] private SpriteRenderer startRenderer;
        [SerializeField] private SpriteRenderer animRenderer;

        public GameObject Root => root != null ? root : gameObject;
        public SpriteRenderer BlurRenderer => blurRenderer;
        public SpriteRenderer StartRenderer => startRenderer;
        public SpriteRenderer AnimRenderer => animRenderer;

        public static rimrushShieldView CreateRuntimeFallback(int side, Transform parent)
        {
            var graphic = new GameObject(side == -1 ? "ShieldLeft" : "ShieldRight");
            graphic.transform.SetParent(parent, false);
            var view = graphic.AddComponent<rimrushShieldView>();
            view.root = graphic;

            var shieldStartSprite = rimrushAtlasCache.Instance.SkillFx.Sprite("ShieldMC0000");
            view.startRenderer = CreateRenderer(graphic.transform, "ShieldStart", 63, shieldStartSprite);
            view.blurRenderer = CreateRenderer(graphic.transform, "ShieldBlur", 64, shieldStartSprite);
            view.animRenderer = CreateRenderer(graphic.transform, "ShieldAnim", 65, null);
            return view;
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, int sortingOrder, Sprite sprite)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.sprite = sprite;
            return renderer;
        }
    }

    public sealed class rimrushEnergyBarSceneView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private rimrushRadialIconView overlayView;
        [SerializeField] private SpriteRenderer hintBackgroundRenderer;
        [SerializeField] private TextMesh hintText;

        public GameObject Root => root != null ? root : gameObject;
        public SpriteRenderer BackgroundRenderer => backgroundRenderer;
        public SpriteRenderer BaseRenderer => baseRenderer;
        public rimrushRadialIconView OverlayView => overlayView;
        public SpriteRenderer HintBackgroundRenderer => hintBackgroundRenderer;
        public TextMesh HintText => hintText;

        public void SetVisible(bool isVisible)
        {
            Root.SetActive(isVisible);
        }

        public static rimrushEnergyBarSceneView CreateRuntimeFallback(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<rimrushEnergyBarSceneView>();
            view.root = root;
            var background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            view.backgroundRenderer = background.AddComponent<SpriteRenderer>();

            var baseIcon = new GameObject("Base");
            baseIcon.transform.SetParent(root.transform, false);
            view.baseRenderer = baseIcon.AddComponent<SpriteRenderer>();

            var overlay = rimrushRadialIconView.CreateRuntimeFallback("Overlay", root.transform);
            view.overlayView = overlay;

            var hintBackground = new GameObject("HintBackground");
            hintBackground.transform.SetParent(root.transform, false);
            view.hintBackgroundRenderer = hintBackground.AddComponent<SpriteRenderer>();

            view.hintText = rimrushRender.Text(
                $"{name}_Hint",
                string.Empty,
                0f,
                0f,
                16,
                Color.white,
                TextAnchor.MiddleCenter,
                87,
                root.transform,
                rimrushTextStyle.TournamentBody);
            return view;
        }
    }

    public sealed class rimrushRadialIconView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        public GameObject Root => root != null ? root : gameObject;
        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;

        public static rimrushRadialIconView CreateRuntimeFallback(string name, Transform parent)
        {
            var graphic = new GameObject(name);
            graphic.transform.SetParent(parent, false);
            var view = graphic.AddComponent<rimrushRadialIconView>();
            view.root = graphic;
            view.meshFilter = graphic.AddComponent<MeshFilter>();
            view.meshRenderer = graphic.AddComponent<MeshRenderer>();
            return view;
        }
    }
}
