using UnityEngine;

namespace rimrush
{
    public readonly struct rimrushControlProfile
    {
        public rimrushControlProfile(
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

    public static class rimrushControlsData
    {
        private static readonly rimrushControlProfile soloProfile = new rimrushControlProfile(
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

        private static readonly rimrushControlProfile playerOneProfile = new rimrushControlProfile(
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

        private static readonly rimrushControlProfile playerTwoProfile = new rimrushControlProfile(
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
            $"1P/TRAINING  {soloProfile.MoveHint} MOVE  {soloProfile.JumpHint} JUMP  {soloProfile.BlockHint} BLOCK  {soloProfile.ActionHint} SHOOT  {soloProfile.SuperHint} SUPER\n" +
            $"2P  P1 {playerOneProfile.MoveHint} MOVE  {playerOneProfile.JumpHint} JUMP  {playerOneProfile.BlockHint} BLOCK  {playerOneProfile.ActionHint} SHOOT  {playerOneProfile.SuperHint} SUPER\n" +
            $"2P  P2 {playerTwoProfile.MoveHint} MOVE  {playerTwoProfile.JumpHint} JUMP  {playerTwoProfile.BlockHint} BLOCK  {playerTwoProfile.ActionHint} SHOOT  {playerTwoProfile.SuperHint} SUPER";

        public static rimrushControlProfile ProfileForBrain(string brain)
        {
            return ProfileForSlot(ParseControllerSlot(brain));
        }

        public static rimrushControlProfile ProfileForSlot(int controllerSlot)
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
