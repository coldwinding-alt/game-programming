// 教程结束后玩家的下一步选择
// 教程完成后，玩家可以选择重玩教程、进入自由训练或开始快速匹配。

namespace mlp
{
    /// <summary>教程结束后玩家的下一步选择：无、重玩教程、进入训练、开始快速匹配。</summary>
    public enum mlpTutorialNextAction
    {
        None,
        ReplayTutorial,
        StartTraining,
        StartQuickMatch
    }
}
