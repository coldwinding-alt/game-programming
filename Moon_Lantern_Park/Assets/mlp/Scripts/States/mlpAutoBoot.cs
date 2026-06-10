// 游戏自动启动入口
// 游戏开始时自动运行，检查是否已经有启动器存在。如果没有就创建一个 mlpGameBootstrap 对象来初始化游戏。这个文件是游戏的启动起点。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 游戏自动启动入口：游戏开始时自动运行，检查是否已有启动器存在，如果没有就创建一个来初始化游戏。是游戏的启动起点。
    /// </summary>
    public static class mlpAutoBoot
    {
        /// <summary>
        /// 游戏启动时自动执行。如果场景中还没有启动器对象，则创建一个 mlpGameBootstrap 来完成初始化。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            // 1. 检查场景中是否已经存在游戏启动器（防止重复创建）
            if (Object.FindObjectOfType<mlpGameBootstrap>() != null)
            {
                return;
            }

            // 2. 如果没有启动器，创建一个新的 GameObject 并挂载 mlpGameBootstrap 组件来初始化游戏
            var go = new GameObject("mlpBootstrap");
            go.AddComponent<mlpGameBootstrap>();
        }
    }
}
