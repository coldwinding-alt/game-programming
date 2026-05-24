using UnityEngine;

namespace rimrush
{
    public enum rimrushSceneAuthoringFocus
    {
        Gameplay,
        Menu,
        Hud
    }

    [ExecuteAlways]
    public sealed class rimrushSceneAuthoringMode : MonoBehaviour
    {
        [SerializeField] private rimrushSceneBindings sceneBindings;
        [SerializeField] private rimrushSceneAuthoringFocus focus = rimrushSceneAuthoringFocus.Gameplay;
        [SerializeField] private bool showHudInGameplay = true;

        public void ApplyEditorState()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (sceneBindings == null)
            {
                sceneBindings = GetComponent<rimrushSceneBindings>();
                if (sceneBindings == null)
                {
                    sceneBindings = GetComponentInChildren<rimrushSceneBindings>(true);
                }
            }

            if (sceneBindings == null)
            {
                return;
            }

            sceneBindings.ResolveMissingReferences();

            var menuShell = sceneBindings.MenuShell;
            var gameplayBindings = sceneBindings.GameplayBindings;
            var hudView = sceneBindings.HudView;

            var showMenu = focus == rimrushSceneAuthoringFocus.Menu;
            var showGameplay = focus == rimrushSceneAuthoringFocus.Gameplay && gameplayBindings != null && gameplayBindings.Root != null;
            var showHud = focus == rimrushSceneAuthoringFocus.Hud || (focus == rimrushSceneAuthoringFocus.Gameplay && showHudInGameplay);

            if (menuShell != null)
            {
                menuShell.gameObject.SetActive(showMenu);
                if (!showMenu && menuShell.DynamicContentRoot != null)
                {
                    menuShell.DynamicContentRoot.gameObject.SetActive(false);
                }
            }

            if (gameplayBindings != null && gameplayBindings.Root != null)
            {
                gameplayBindings.Root.gameObject.SetActive(showGameplay);
            }

            if (hudView != null)
            {
                hudView.gameObject.SetActive(showHud);
            }
        }

        private void OnEnable()
        {
            ApplyEditorState();
        }

        private void OnValidate()
        {
            ApplyEditorState();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyEditorState();
            }
        }
    }
}
