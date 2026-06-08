// 键盘控制配置（玩家 1 和玩家 2 的按键映射）
// 定义两个玩家各自用哪些键盘按键来控制角色：移动、跳跃、投篮、假动作、扣篮、大招。游戏启动时根据这里设置的按键来读取玩家输入。

using UnityEngine;

namespace mlp
{
    public readonly struct mlpControlProfile
    {
        /// <summary>
        /// 存储一个玩家的完整键盘按键绑定和显示标签。
        /// </summary>
        /// <param name="controllerSlot">此配置所属的玩家槽位（0 = 单人，1 = 玩家 1，2 = 玩家 2）。</param>
        /// <param name="moveHint">UI 中显示的移动键提示文本（如 "A/D"）。</param>
        /// <param name="jumpHint">UI 中显示的跳跃键提示文本（如 "W"）。</param>
        /// <param name="blockHint">UI 中显示的防守键提示文本（如 "S"）。</param>
        /// <param name="actionHint">UI 中显示的投篮/操作键提示文本（如 "B"）。</param>
        /// <param name="superHint">UI 中显示的大招键提示文本（如 "N"）。</param>
        /// <param name="moveLeftKey">向左移动的 KeyCode。</param>
        /// <param name="moveRightKey">向右移动的 KeyCode。</param>
        /// <param name="jumpKey">跳跃的 KeyCode。</param>
        /// <param name="blockKey">防守的 KeyCode。</param>
        /// <param name="actionKey">投篮或执行主要操作的 KeyCode。</param>
        /// <param name="superKey">激活大招的 KeyCode。</param>
        public mlpControlProfile(
            int controllerSlot,
            string moveHint,
            string jumpHint,
            string blockHint,
            string actionHint,
            string superHint,
            KeyCode moveLeftKey,
            KeyCode moveRightKey,
            KeyCode jumpKey,
            KeyCode blockKey,
            KeyCode actionKey,
            KeyCode superKey)
        {
            ControllerSlot = controllerSlot;
            MoveHint = moveHint;
            JumpHint = jumpHint;
            BlockHint = blockHint;
            ActionHint = actionHint;
            SuperHint = superHint;
            MoveLeftKey = moveLeftKey;
            MoveRightKey = moveRightKey;
            JumpKey = jumpKey;
            BlockKey = blockKey;
            ActionKey = actionKey;
            SuperKey = superKey;
        }

        public int ControllerSlot { get; }
        public string MoveHint { get; }
        public string JumpHint { get; }
        public string BlockHint { get; }
        public string ActionHint { get; }
        public string SuperHint { get; }
        public KeyCode MoveLeftKey { get; }
        public KeyCode MoveRightKey { get; }
        public KeyCode JumpKey { get; }
        public KeyCode BlockKey { get; }
        public KeyCode ActionKey { get; }
        public KeyCode SuperKey { get; }
    }

    public static class mlpControlsData
    {
        private static readonly mlpControlProfile soloProfile = new mlpControlProfile(
            0,
            "A/D",
            "W",
            "S",
            "B",
            "N",
            KeyCode.A,
            KeyCode.D,
            KeyCode.W,
            KeyCode.S,
            KeyCode.B,
            KeyCode.N);

        private static readonly mlpControlProfile playerOneProfile = new mlpControlProfile(
            1,
            "A/D",
            "W",
            "S",
            "B",
            "V",
            KeyCode.A,
            KeyCode.D,
            KeyCode.W,
            KeyCode.S,
            KeyCode.B,
            KeyCode.V);

        private static readonly mlpControlProfile playerTwoProfile = new mlpControlProfile(
            2,
            "LEFT/RIGHT",
            "UP",
            "DOWN",
            "L",
            "K",
            KeyCode.LeftArrow,
            KeyCode.RightArrow,
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.L,
            KeyCode.K);

        public static string MainMenuControlsText =>
            $"1P/TUTORIAL/TRAINING  {soloProfile.MoveHint} MOVE  {soloProfile.JumpHint} JUMP  {soloProfile.BlockHint} BLOCK  {soloProfile.ActionHint} SHOOT  {soloProfile.SuperHint} SUPER\n" +
            $"2P  P1 {playerOneProfile.MoveHint} MOVE  {playerOneProfile.JumpHint} JUMP  {playerOneProfile.BlockHint} BLOCK  {playerOneProfile.ActionHint} SHOOT  {playerOneProfile.SuperHint} SUPER\n" +
            $"2P  P2 {playerTwoProfile.MoveHint} MOVE  {playerTwoProfile.JumpHint} JUMP  {playerTwoProfile.BlockHint} BLOCK  {playerTwoProfile.ActionHint} SHOOT  {playerTwoProfile.SuperHint} SUPER";

        /// <summary>
        /// 返回与给定脑标识字符串（如 "P0"、"B1"）匹配的控制配置。
        /// </summary>
        /// <param name="brain">脑字符串，其第二个字符编码了控制器槽位号。</param>
        /// <returns>匹配的控制配置。</returns>
        public static mlpControlProfile ProfileForBrain(string brain)
        {
            return ProfileForSlot(ParseControllerSlot(brain));
        }

        /// <summary>
        /// 返回指定玩家槽位的控制配置。
        /// </summary>
        /// <param name="controllerSlot">0 = 单人模式，1 = 双人模式中的玩家 1，2 = 玩家 2。</param>
        /// <returns>匹配槽位号的控制配置。</returns>
        public static mlpControlProfile ProfileForSlot(int controllerSlot)
        {
            switch (controllerSlot)
            {
                case 1:
                    return playerOneProfile;
                case 2:
                    return playerTwoProfile;
                default:
                    return soloProfile;
            }
        }

        /// <summary>
        /// 从脑标识字符串中提取控制器槽位号。读取脑字符串的第二个字符作为数字，并限制在 0-2 范围内。
        /// </summary>
        /// <param name="brain">脑字符串，如 "P0"、"P1" 或 "B2"。</param>
        /// <returns>解析出的槽位号（0、1 或 2），解析失败时返回 0。</returns>
        public static int ParseControllerSlot(string brain)
        {
            if (string.IsNullOrEmpty(brain) || brain.Length < 2)
            {
                return 0;
            }

            if (!int.TryParse(brain.Substring(1, 1), out var value))
            {
                return 0;
            }

            return Mathf.Clamp(value, 0, 2);
        }
    }
}
