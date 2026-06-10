// 教程模式的对手控制器
// 训练关卡里，对手会按照脚本移动到指定位置、跳跃、投篮，配合玩家完成练习。
// 自由对战时，切换成普通 AI 控制。

namespace mlp
{
    /// <summary>教程对手模式：脚本模式（按预设动作配合玩家练习）或自由对战模式（切换成普通 AI）。</summary>
    public enum mlpTutorialOpponentMode
    {
        Scripted,
        FreePlay
    }

    /// <summary>
    /// 教程对手控制器：在教程练习中按脚本控制对手移动到指定位置、跳跃、投篮，配合玩家完成练习动作。
    /// </summary>
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
        /// 创建教程对手控制器。skillLevel 是四档 AI 技能索引（0 = Easy，1 = Normal，2 = Hard，3 = Hell）。
        /// </summary>
        public mlpTutorialOpponentController(mlpPlayerObject player, int skillLevel)
        {
            // 1. 保存对手玩家对象的引用
            this.player = player;
            // 2. 创建一个普通 AI 控制器作为后备——当教程进入自由对战阶段时，对手会切换成这个 AI 来自主行动
            //    0 现在是合法的 Easy 索引，不能再当成无效值回退到更高难度。
            var fallbackSkillIndex = UnityEngine.Mathf.Clamp(skillLevel, 0, mlpAISkillsData.MaxSkillIndex);
            fallbackController = mlpAIController.CreateForBrain(player, "B0", fallbackSkillIndex);
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
            // 1. 如果是自由对战模式，让后备 AI 自主决策，然后同步它的按键状态
            if (mode == mlpTutorialOpponentMode.FreePlay)
            {
                fallbackController.UpdateController(dt);
                SyncInputsFrom(fallbackController);
                return;
            }

            // 2. 脚本模式：先清空所有按键，然后让教程流程系统填充本帧对手该做的动作（移动到指定位置、跳跃、投篮等）
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
