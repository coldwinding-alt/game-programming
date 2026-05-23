using UnityEngine;

namespace rimrush
{
    public sealed class rimrushMenuButtonView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private TextMesh label;

        public GameObject Root => gameObject;
        public SpriteRenderer BackgroundRenderer => backgroundRenderer;
        public TextMesh Label => label;

        public static rimrushMenuButtonView CreateRuntimeFallback(string name, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<rimrushMenuButtonView>();
            var background = new GameObject("Background");
            background.transform.SetParent(root.transform, false);
            var bg = background.AddComponent<SpriteRenderer>();
            bg.sprite = rimrushAtlasCache.Instance.Interface.Sprite("btn_bg0000", 0.5f, 0.5f);
            bg.sortingOrder = 50;
            view.backgroundRenderer = bg;

            var labelMesh = rimrushRender.Text(
                $"{name}_Label",
                string.Empty,
                0f,
                0f,
                24,
                Color.white,
                TextAnchor.MiddleCenter,
                80,
                root.transform,
                rimrushTextStyle.ButtonLabel);
            view.label = labelMesh;
            return view;
        }
    }
}
