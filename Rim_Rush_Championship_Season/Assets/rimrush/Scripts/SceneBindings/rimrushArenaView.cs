using UnityEngine;

namespace rimrush
{
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
}
