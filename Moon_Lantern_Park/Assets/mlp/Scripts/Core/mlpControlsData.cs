// 键盘控制配置（玩家 1 和玩家 2 的按键映射）
// 定义两个玩家各自用哪些键盘按键来控制角色：移动、跳跃、投篮、假动作、扣篮、大招。游戏启动时根据这里设置的按键来读取玩家输入。

using UnityEngine;

namespace mlp
{
    public readonly struct mlpControlProfile
    {
        /// <summary>
        /// Stores one player's full set of keyboard bindings and display labels.
        /// </summary>
        /// <param name="controllerSlot">Which player slot this profile belongs to (0 = solo, 1 = player 1, 2 = player 2).</param>
        /// <param name="moveHint">Text label shown in the UI for the move keys (e.g. "A/D").</param>
        /// <param name="jumpHint">Text label shown in the UI for the jump key (e.g. "W").</param>
        /// <param name="blockHint">Text label shown in the UI for the block key (e.g. "S").</param>
        /// <param name="actionHint">Text label shown in the UI for the shoot / action key (e.g. "B").</param>
        /// <param name="superHint">Text label shown in the UI for the super move key (e.g. "N").</param>
        /// <param name="moveLeftKey">KeyCode for moving left.</param>
        /// <param name="moveRightKey">KeyCode for moving right.</param>
        /// <param name="jumpKey">KeyCode for jumping.</param>
        /// <param name="blockKey">KeyCode for blocking.</param>
        /// <param name="actionKey">KeyCode for shooting or performing the main action.</param>
        /// <param name="superKey">KeyCode for activating the super move.</param>
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
        /// Returns the control profile that matches the given brain identifier string (e.g. "P0", "B1").
        /// </summary>
        /// <param name="brain">The brain string whose second character encodes the controller slot number.</param>
        /// <returns>The matching control profile for that brain.</returns>
        public static mlpControlProfile ProfileForBrain(string brain)
        {
            return ProfileForSlot(ParseControllerSlot(brain));
        }

        /// <summary>
        /// Returns the control profile for the given player slot number.
        /// </summary>
        /// <param name="controllerSlot">0 = solo play, 1 = player 1 in 2-player mode, 2 = player 2.</param>
        /// <returns>The control profile with the matching slot number.</returns>
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
        /// Extracts the controller slot number from a brain identifier string.
        /// Reads the second character of the brain string as a digit and clamps it to 0-2.
        /// </summary>
        /// <param name="brain">A brain string like "P0", "P1", or "B2".</param>
        /// <returns>The parsed slot number (0, 1, or 2), or 0 if parsing fails.</returns>
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
