// 文件作用：这个脚本负责本模块的核心逻辑与协作调度。
// 概括：rimrushAutoBoot 用来处理对应子系统的关键流程，先看这里能快速定位功能入口。

using UnityEngine;

namespace rimrush
{
    public static class rimrushAutoBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        /// <summary>
        /// Executes Boot for the rimrushAutoBoot workflow.
        /// This method coordinates related state updates so gameplay behavior stays consistent and predictable.
        /// </summary>
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
