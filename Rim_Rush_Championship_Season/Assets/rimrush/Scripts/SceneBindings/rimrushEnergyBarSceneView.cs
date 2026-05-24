using UnityEngine;

namespace rimrush
{
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
            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, false);
            var view = rootObject.AddComponent<rimrushEnergyBarSceneView>();
            view.root = rootObject;

            var background = new GameObject("Background");
            background.transform.SetParent(rootObject.transform, false);
            view.backgroundRenderer = background.AddComponent<SpriteRenderer>();

            var baseIcon = new GameObject("Base");
            baseIcon.transform.SetParent(rootObject.transform, false);
            view.baseRenderer = baseIcon.AddComponent<SpriteRenderer>();

            view.overlayView = rimrushRadialIconView.CreateRuntimeFallback("Overlay", rootObject.transform);

            var hintBackground = new GameObject("HintBackground");
            hintBackground.transform.SetParent(rootObject.transform, false);
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
                rootObject.transform,
                rimrushTextStyle.TournamentBody);
            return view;
        }
    }
}
