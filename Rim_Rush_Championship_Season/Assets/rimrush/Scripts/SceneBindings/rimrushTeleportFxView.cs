using UnityEngine;

namespace rimrush
{
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

            var blackNodeTransform = new GameObject("TeleportBlack").transform;
            blackNodeTransform.SetParent(graphic.transform, false);
            var blackSpriteRenderer = blackNodeTransform.gameObject.AddComponent<SpriteRenderer>();
            blackSpriteRenderer.sortingOrder = 74;
            blackSpriteRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport10000");

            var centerNode = new GameObject("TeleportCenter");
            centerNode.transform.SetParent(blackNodeTransform, false);
            var centerSpriteRenderer = centerNode.AddComponent<SpriteRenderer>();
            centerSpriteRenderer.sortingOrder = 75;
            centerSpriteRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport20000");

            var animNode = new GameObject("TeleportAnim");
            animNode.transform.SetParent(graphic.transform, false);
            var animSpriteRenderer = animNode.AddComponent<SpriteRenderer>();
            animSpriteRenderer.sortingOrder = 76;

            var whiteNode = new GameObject("TeleportWhite");
            whiteNode.transform.SetParent(graphic.transform, false);
            var whiteSpriteRenderer = whiteNode.AddComponent<SpriteRenderer>();
            whiteSpriteRenderer.sortingOrder = 77;
            whiteSpriteRenderer.sprite = rimrushAtlasCache.Instance.SkillFx.Sprite("teleport40000");

            view.blackNode = blackNodeTransform;
            view.blackRenderer = blackSpriteRenderer;
            view.centerRenderer = centerSpriteRenderer;
            view.whiteRenderer = whiteSpriteRenderer;
            view.animRenderer = animSpriteRenderer;
            return view;
        }
    }
}
