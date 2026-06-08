// 游戏自动启动入口
// 游戏开始时自动运行，检查是否已经有启动器存在。如果没有就创建一个 mlpGameBootstrap 对象来初始化游戏。这个文件是游戏的启动起点。

using UnityEngine;

namespace mlp
{
    public static class mlpAutoBoot
    {
        /// <summary>
        /// 游戏启动时自动执行。如果场景中还没有启动器对象，则创建一个 mlpGameBootstrap 来完成初始化。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<mlpGameBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("mlpBootstrap");
            go.AddComponent<mlpGameBootstrap>();
        }
    }
}
