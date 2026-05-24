using UnityEngine;

namespace rimrush
{
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
}
