using UnityEngine;

namespace rimrush
{
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
