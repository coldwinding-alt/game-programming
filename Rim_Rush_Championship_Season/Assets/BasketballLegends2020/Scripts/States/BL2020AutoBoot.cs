using UnityEngine;

namespace BasketballLegends2020
{
    public static class BL2020AutoBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<BLGameBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("BL2020Bootstrap");
            go.AddComponent<BLGameBootstrap>();
        }
    }
}
