using System.Collections.Generic;
using UnityEngine;

namespace rimrush
{
    public sealed class rimrushIconButtonView : MonoBehaviour
    {
        [SerializeField] private rimrushMenuButtonView buttonView;
        [SerializeField] private GameObject[] icons;

        public GameObject Root => gameObject;
        public rimrushMenuButtonView ButtonView => buttonView;
        public IReadOnlyList<GameObject> Icons => icons;

        public GameObject GetIcon(int index)
        {
            if (icons == null || index < 0 || index >= icons.Length)
            {
                return null;
            }

            return icons[index];
        }

        public static rimrushIconButtonView CreateRuntimeFallback(string name, Transform parent, params string[] resourcePaths)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<rimrushIconButtonView>();
            view.buttonView = rimrushMenuButtonView.CreateRuntimeFallback($"{name}_Button", root.transform);

            var iconList = new List<GameObject>();
            for (var i = 0; i < resourcePaths.Length; i++)
            {
                var texture = Resources.Load<Texture2D>(resourcePaths[i]);
                if (texture == null)
                {
                    continue;
                }

                var icon = rimrushRender.Image($"{name}_Icon{i}", texture, 0f, 0f, 0.5f, 0.5f, 60, root.transform);
                iconList.Add(icon);
            }

            view.icons = iconList.ToArray();
            return view;
        }
    }
}
