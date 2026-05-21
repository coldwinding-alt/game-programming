using UnityEngine;

namespace rimrush
{
    public static class rimrushAutoBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<rimrushGameBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("rimrushBootstrap");
            go.AddComponent<rimrushGameBootstrap>();
        }
    }
}
