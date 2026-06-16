// Keyboard control configuration (key mapping for Player 1 and Player 2)
// Define which keyboard keys each player uses to control the character: move, jump, shoot, feint, dunk, ultimate move. When the game starts, player input is read based on the keys set here.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Keyboard control configuration: Stores a player's complete key bindings (movement, jump, defense, shooting, ultimate) and UI display labels.
    /// </summary>
    public readonly struct mlpControlProfile
    {
        /// <summary>
        /// Stores a player's complete keyboard keybindings and display labels.
        /// </summary>
        /// <param name="controllerSlot">The player slot this configuration belongs to (0 = solo, 1 = player 1, 2 = player 2). </param>
        /// <param name="moveHint">Move key hint text displayed in the UI (such as "A/D"). </param>
        /// <param name="jumpHint">The jump key hint text displayed in the UI (such as "W"). </param>
        /// <param name="blockHint">The defense key hint text displayed in the UI (such as "S"). </param>
        /// <param name="actionHint">The shooting/action key hint text displayed in the UI (such as "B"). </param>
        /// <param name="superHint">Ultimate key hint text displayed in the UI (such as "N"). </param>
        /// <param name="moveLeftKey">KeyCode for moving left. </param>
        /// <param name="moveRightKey">KeyCode to move to the right. </param>
        /// <param name="jumpKey">Jump KeyCode. </param>
        /// <param name="blockKey">The defending KeyCode. </param>
        /// <param name="actionKey">KeyCode for shooting the ball or performing the main action. </param>
        /// <param name="superKey">KeyCode that activates the ultimate move. </param>
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

    /// <summary>
    /// Control configuration manager: Three sets of button configurations are preset for single player, player 1, and player 2, and the corresponding button mapping is returned according to the player number.
    /// </summary>
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
        /// Returns the control configuration matching the given brain identification string (e.g. "P0", "B1").
        /// </summary>
        /// <param name="brain">Brain string, the second character encodes the controller slot number. </param>
        /// <returns> Matching control configuration. </returns>
        public static mlpControlProfile ProfileForBrain(string brain)
        {
            return ProfileForSlot(ParseControllerSlot(brain));
        }

        /// <summary>
        /// Returns the control configuration for the specified player slot.
        /// </summary>
        /// <param name="controllerSlot">0 = Solo mode, 1 = Player 1 in Duo mode, 2 = Player 2. </param>
        /// <returns>Control configuration matching the slot number. </returns>
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
        /// Extract the controller slot number from the brain identification string. Read the second character of the brain string as a number, limited to the range 0-2.
        /// </summary>
        /// <param name="brain">Brain string, such as "P0", "P1" or "B2". </param>
        /// <returns>The parsed slot number (0, 1 or 2), 0 will be returned if the parsing fails. </returns>
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
