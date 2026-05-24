using UnityEngine;

namespace rimrush
{
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
            var graphicSpriteRenderer = graphic.AddComponent<SpriteRenderer>();
            graphicSpriteRenderer.sortingOrder = 50;
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
            view.graphicRenderer = graphicSpriteRenderer;
            view.shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            return view;
        }
    }
}
