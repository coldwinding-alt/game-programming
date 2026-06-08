// 教程模式的对手控制器
// 训练关卡里，对手会按照脚本移动到指定位置、跳跃、投篮，配合玩家完成练习。
// 自由对战时，切换成普通 AI 控制。

namespace mlp
{
    public enum mlpTutorialOpponentMode
    {
        Scripted,
        FreePlay
    }

    public sealed class mlpTutorialOpponentController : IBLPlayerController
    {
        private readonly mlpPlayerObject player;
        private readonly IBLPlayerController fallbackController;
        private mlpTutorialOpponentMode mode = mlpTutorialOpponentMode.Scripted;

        public int CurrentMove { get; private set; }
        public bool CurrentJump { get; private set; }
        public bool CurrentAction { get; private set; }
        public bool CurrentBlockOrPump { get; private set; }
        public bool CurrentSuper { get; private set; }
        public int CurrentDash { get; private set; }

        /// <summary>
        /// 创建教程对手控制器。skillLevel 控制自由对战时 AI 的智能程度。
        /// </summary>
        public mlpTutorialOpponentController(mlpPlayerObject player, int skillLevel)
        {
            this.player = player;
            fallbackController = mlpAIController.CreateForBrain(player, "B0", skillLevel <= 0 ? 2 : skillLevel);
        }

        /// <summary>
        /// 切换脚本模式（按教程指令行动）和自由对战模式（普通 AI 控制）。
        /// </summary>
        public void SetMode(mlpTutorialOpponentMode nextMode)
        {
            mode = nextMode;
            if (mode == mlpTutorialOpponentMode.Scripted)
            {
                SetFrameInputs(0, false, false, false, false, 0);
            }
        }

        /// <summary>
        /// 每帧调用。脚本模式下由教程流程控制对手的行为，自由对战模式下由 AI 自主决策。
        /// </summary>
        public void UpdateController(float dt)
        {
            if (mode == mlpTutorialOpponentMode.FreePlay)
            {
                fallbackController.UpdateController(dt);
                SyncInputsFrom(fallbackController);
                return;
            }

            SetFrameInputs(0, false, false, false, false, 0);
            player.GameCore.TutorialFlow?.PopulateOpponentInputs(player, this, dt);
        }

        /// <summary>
        /// 通知 AI：当前玩家已持球。
        /// </summary>
        public void BallInOwnHands(int holderPlayerNo)
        {
            fallbackController.BallInOwnHands(holderPlayerNo);
        }

        /// <summary>
        /// 通知 AI：对手已持球。
        /// </summary>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
            fallbackController.BallInOpponentsHands(holderPlayerNo);
        }

        /// <summary>
        /// 通知 AI：当前玩家刚完成投篮。
        /// </summary>
        public void BallOwnShoot(int shooterPlayerNo)
        {
            fallbackController.BallOwnShoot(shooterPlayerNo);
        }

        /// <summary>
        /// 通知 AI：对手刚完成投篮。
        /// </summary>
        public void BallOpponentShoot(int shooterPlayerNo)
        {
            fallbackController.BallOpponentShoot(shooterPlayerNo);
        }

        /// <summary>
        /// 通知 AI：当前无人持球或投篮。
        /// </summary>
        public void BallOthers()
        {
            fallbackController.BallOthers();
        }

        /// <summary>
        /// 判断玩家当前是否可以按下动作键。
        /// </summary>
        public bool ReadyForAction()
        {
            return mode == mlpTutorialOpponentMode.FreePlay
                ? fallbackController.ReadyForAction()
                : !CurrentAction;
        }

        /// <summary>
        /// 判断玩家是否应该松开防守/假动作键。
        /// </summary>
        public bool ReleaseBlockOrPump(float dt)
        {
            return mode == mlpTutorialOpponentMode.FreePlay
                ? fallbackController.ReleaseBlockOrPump(dt)
                : !CurrentBlockOrPump;
        }

        /// <summary>
        /// 新回合开始时重置控制器状态。
        /// </summary>
        public void Restart(int startSide)
        {
            fallbackController.Restart(startSide);
            SetFrameInputs(0, false, false, false, false, 0);
        }

        /// <summary>
        /// 玩家落地时调用。
        /// </summary>
        public void PlayerOnGround()
        {
            fallbackController.PlayerOnGround();
        }

        /// <summary>
        /// 玩家冲刺结束时调用。
        /// </summary>
        public void PlayerOnDashEnd()
        {
            fallbackController.PlayerOnDashEnd();
        }

        /// <summary>
        /// 玩家成功盖帽时调用。
        /// </summary>
        public void PlayerOnBlock()
        {
            fallbackController.PlayerOnBlock();
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// 设置本帧的所有按键输入（由教程系统用来脚本化对手行为）。
        /// </summary>
        public void SetFrameInputs(int move, bool jump, bool action, bool blockOrPump, bool super, int dash)
        {
            CurrentMove = move;
            CurrentJump = jump;
            CurrentAction = action;
            CurrentBlockOrPump = blockOrPump;
            CurrentSuper = super;
            CurrentDash = dash;
        }

        /// <summary>
        /// 从另一个控制器同步按键状态（用于同步 AI 决策结果）。
        /// </summary>
        private void SyncInputsFrom(IBLPlayerController controller)
        {
            CurrentMove = controller.CurrentMove;
            CurrentJump = controller.CurrentJump;
            CurrentAction = controller.CurrentAction;
            CurrentBlockOrPump = controller.CurrentBlockOrPump;
            CurrentSuper = controller.CurrentSuper;
            CurrentDash = controller.CurrentDash;
        }
    }
}
