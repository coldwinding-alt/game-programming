// The game automatically starts the entrance
// The game starts automatically and checks to see if a launcher already exists. If not, create an mlpGameBootstrap object to initialize the game. This file is the starting point for starting the game.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Game automatic startup entrance: automatically runs when the game starts, checks whether a launcher already exists, and if not, creates one to initialize the game. It is the starting point of the game.
    /// </summary>
    public static class mlpAutoBoot
    {
        /// <summary>
        /// Automatically executed when the game starts. If there is no launcher object in the scene yet, create an mlpGameBootstrap to complete the initialization.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // 1. Check whether the game launcher already exists in the scene (to prevent repeated creation)
            if (Object.FindObjectOfType<mlpGameBootstrap>() != null)
            {
                return;
            }

            // 2. If there is no launcher, create a new GameObject and mount the mlpGameBootstrap component to initialize the game
            var go = new GameObject("mlpBootstrap");
            go.AddComponent<mlpGameBootstrap>();
        }
    }
}
