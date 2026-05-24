using UnityEngine;

namespace rimrush
{
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

            var armatureMountObject = new GameObject($"{name}_ArmatureMount");
            armatureMountObject.transform.SetParent(graphic.transform, false);
            armatureMountObject.transform.localPosition = rimrushConstants.SnapLocalPositionToScreenPixels(
                graphic.transform,
                new Vector3(0f, -35f, 0f));

            var fallback = new GameObject($"{name}_Fallback");
            fallback.transform.SetParent(graphic.transform, false);
            var fallbackSpriteRenderer = fallback.AddComponent<SpriteRenderer>();
            fallbackSpriteRenderer.sortingOrder = 20;
            fallbackSpriteRenderer.enabled = false;

            view.root = graphic.transform;
            view.shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            view.armatureMount = armatureMountObject.transform;
            view.fallbackRenderer = fallbackSpriteRenderer;
            return view;
        }
    }
}
