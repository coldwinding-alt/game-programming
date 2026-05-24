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
}
