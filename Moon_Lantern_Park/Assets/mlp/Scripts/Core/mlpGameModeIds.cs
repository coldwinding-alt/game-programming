// 游戏模式 ID 常量
// 定义各种游戏模式的编号：随机快速匹配、快速匹配、训练模式、双人对战、教程。用来区分当前正在玩的是哪种模式。

namespace mlp
{
    /// <summary>
    /// 游戏模式 ID 常量：定义各种游戏模式的编号（随机快速、快速匹配、训练、双人对战、教程），用来区分当前在玩哪种模式。
    /// </summary>
    public static class mlpGameModeIds
    {
        public const int RandomQuick = 1;
        public const int QuickMatch = 2;
        public const int Training = 3;
        public const int TwoPlayers = 4;
        public const int Tutorial = 5;
    }

}
