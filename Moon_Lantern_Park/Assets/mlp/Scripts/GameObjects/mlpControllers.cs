// File function: Player input controller (keyboard and AI)
// Summary: Define how the player controls the character: keyboard player 1 uses WASD, keyboard player 2 uses the arrow keys, and the AI ​​is automatically controlled by the computer. The key status is read every frame and tells the character where to move, whether to jump, and whether to shoot.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Player Controller Interface: Defines a unified way to control the character (movement, jumping, shooting, defense, ultimate move). Both the keyboard and the AI ​​implement this interface.
    /// </summary>
    public interface IBLPlayerController
    {
        /// <summary>-1 move left / 0 stay still / +1 move right</summary>

        int CurrentMove { get; }
        /// <summary>Whether to jump</summary>
        bool CurrentJump { get; }
        /// <summary>Shoot (when holding the ball) or steal (when defending)</summary>

        bool CurrentAction { get; }
        /// <summary>Block or feint</summary>

        bool CurrentBlockOrPump { get; }
        /// <summary>Unleash the ultimate move</summary>
        bool CurrentSuper { get; }
        /// <summary>Sprint direction (-1 left / +1 right / 0 none)</summary>

        int CurrentDash { get; }
        /// <summary>Called every frame: read input or run AI decision, update 6 output attributes</summary>

        void UpdateController(float dt);
        /// <summary>Our team gets the ball: switch to attack mode</summary>

        void BallInOwnHands(int holderPlayerNo);
        /// <summary>The opponent gets the ball: switch to defensive mode</summary>

        void BallInOpponentsHands(int holderPlayerNo);
        /// <summary>Your own shot: switch to offensive rebounding mode</summary>

        void BallOwnShoot(int shooterPlayerNo);
        /// <summary>Opponent's shot: switch to defensive rebounding mode</summary>
        void BallOpponentShoot(int shooterPlayerNo);
        /// <summary>The ball is uncontrolled (in contention/bouncing): switch to contention mode</summary>

        void BallOthers();
        /// <summary>Are you ready to perform actions (keyboard release to prevent bursts, AI is always true)</summary>

        bool ReadyForAction();
        /// <summary>Whether to release the cap key/timer expires and end the cap animation</summary>

        bool ReleaseBlockOrPump(float dt);
        /// <summary>New round starts: reset all status</summary>

        void Restart(int startSide);
        /// <summary>Character landing: handling feint reset and quick take-off</summary>

        void PlayerOnGround();
        /// <summary>End of sprint: adjust attack point, clear sprint buffer</summary>
        void PlayerOnDashEnd();
        /// <summary>Block completed: reset the blocking status and start the cooling timer</summary>
        void PlayerOnBlock();
    }

    /// <summary>
    /// Keyboard controller: Read keyboard key input to control the character. According to the configured key mapping, the key status is detected every frame and converted into character actions.
    /// </summary>
    public sealed class mlpKeyboardController : IBLPlayerController
    {
        // Current controller button configuration (WASD or direction keys)
        private readonly mlpControlProfile controls;
        // The time when the left button was last pressed, initially -10 to prevent false triggering at the start of the game

        private float lastLeftDown = -10f;
        // The time when the right button was last pressed

        private float lastRightDown = -10f;
        // The time when the left button was last released (for double-click release-press detection)
        private float lastLeftUp = -10f;
        // The time when the right button was last released

        private float lastRightUp = -10f;
        // Sprint direction in queue: -1 for left dash, +1 for right dash, 0 for none

        private int pendingDashDirection;
        // Sprint buffer timer, when it is greater than 0, the sprint signal will continue to be output.

        private float pendingDashTimer;

        // Current moving direction output: -1 for left movement, 0 for stationary, +1 for right movement

        public int CurrentMove { get; private set; }
        // Current jump output

        public bool CurrentJump { get; private set; }
        // Current action output: shooting when holding the ball, stealing when defending

        public bool CurrentAction { get; private set; }
        // Current block/fake output

        public bool CurrentBlockOrPump { get; private set; }
        // Current ultimate output

        public bool CurrentSuper { get; private set; }
        // Current sprint direction output: -1 for left dash, 0 for none, +1 for right dash

        public int CurrentDash { get; private set; }

        /// <summary>
        /// Creates a keyboard controller that reads keystrokes from the given control configuration.

        /// </summary>
        /// <param name="brain">Controller identification string</param>

        public mlpKeyboardController(string brain)
        {
            // 1. Load the corresponding button configuration (WASD or direction keys) according to the controller identification (such as "KB1" or "KB2")
            controls = mlpControlsData.ProfileForBrain(brain);
        }

        /// <summary>
        /// Keyboard input is read every frame: move, jump, shoot, block, special move, and sprint double tap.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public void UpdateController(float dt)
        {
            // 1. Reset movement and sprint status

            CurrentMove = 0;
            CurrentDash = 0;
            // 2. If there are sprint instructions being buffered, decrement the timer and execute the sprint within the validity period

            if (pendingDashTimer > 0f)
            {
                pendingDashTimer = Mathf.Max(0f, pendingDashTimer - dt);
                if (pendingDashTimer > 0f)
                {
                    CurrentDash = pendingDashDirection;
                }
                else
                {
                    pendingDashDirection = 0;
                }
            }

            // 3. Read the left and right keys in the configuration

            var leftDown = controls.MoveLeftKey;
            var rightDown = controls.MoveRightKey;
            var currentTime = Time.time;

            // 4. Record the time when the key is released (for double-click sprint detection)

            if (Input.GetKeyUp(leftDown))
            {
                lastLeftUp = currentTime;
            }

            if (Input.GetKeyUp(rightDown))
            {
                lastRightUp = currentTime;
            }

            // 5. Detect left shift key press: If pressed twice in a short period of time (press-press or release-press), trigger left sprint

            if (Input.GetKeyDown(leftDown))
            {
                if (currentTime - lastLeftDown <= mlpObjectsData.DashDoubleTapWindow
                    || currentTime - lastLeftUp <= mlpObjectsData.DashDoubleTapWindow)
                {
                    QueueDash(-1);
                }

                lastLeftDown = currentTime;
            }

            // 6. Detect when the right shift key is pressed: In the same way, double-click to trigger a sprint to the right.

            if (Input.GetKeyDown(rightDown))
            {
                if (currentTime - lastRightDown <= mlpObjectsData.DashDoubleTapWindow
                    || currentTime - lastRightUp <= mlpObjectsData.DashDoubleTapWindow)
                {
                    QueueDash(1);
                }

                lastRightDown = currentTime;
            }

            // 7. When holding down the left shift key, the movement direction is -1, and when holding down the right shift key, the movement direction is +1 (can be superimposed, and pressed simultaneously is 0)

            if (Input.GetKey(leftDown))
            {
                CurrentMove--;
            }

            if (Input.GetKey(rightDown))
            {
                CurrentMove++;
            }

            // 8. Read the current status of jump, action (shooting/stealing), defense/feint, and ultimate button

            CurrentJump = Input.GetKey(controls.JumpKey);
            CurrentAction = Input.GetKey(controls.ActionKey);
            CurrentBlockOrPump = Input.GetKey(controls.BlockKey);
            CurrentSuper = Input.GetKey(controls.SuperKey);
        }

        // Buffer the sprint command: Set the direction and buffer timing so that the sprint signal lasts for a few frames instead of being instantaneous

        private void QueueDash(int direction)
        {
            pendingDashDirection = direction;
            pendingDashTimer = mlpObjectsData.DashInputBuffer;
            CurrentDash = direction;
        }

        /// <summary>
        /// Returns true when the action key is released, preventing repeated shots while held down.
        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        public bool ReadyForAction()
        {
            return !Input.GetKey(controls.ActionKey);
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        /// <param name="holderPlayerNo">The player number of the current ball holder</param>
        public void BallInOwnHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        /// <param name="holderPlayerNo">The player number of the current ball holder</param>
        public void BallInOpponentsHands(int holderPlayerNo)
        {
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        /// <param name="shooterPlayerNo">The player number of the shooter</param>

        public void BallOwnShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        /// <param name="shooterPlayerNo">The player number of the shooter</param>

        public void BallOpponentShoot(int shooterPlayerNo)
        {
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        public void BallOthers()
        {
        }

        /// <summary>
        /// Returns true when the cap key is released, ending the cap/feint animation.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        public bool ReleaseBlockOrPump(float dt)
        {
            return !Input.GetKey(controls.BlockKey);
        }

        /// <summary>
        /// Reset all input states and double-click timers to prepare for a new round.
        /// </summary>
        /// <param name="startSide">The player's initial position and direction after reset</param>

        public void Restart(int startSide)
        {
            lastLeftDown = -10f;
            lastRightDown = -10f;
            lastLeftUp = -10f;
            lastRightUp = -10f;
            pendingDashDirection = 0;
            pendingDashTimer = 0f;
            CurrentMove = 0;
            CurrentDash = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentBlockOrPump = false;
            CurrentSuper = false;
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        public void PlayerOnGround()
        {
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        public void PlayerOnDashEnd()
        {
            pendingDashDirection = 0;
            pendingDashTimer = 0f;
        }

        /// <summary>
        /// No action is required from the keyboard controller; input is read every frame.

        /// </summary>
        public void PlayerOnBlock()
        {
        }
    }

    /// <summary>
    /// AI Controller (Basic Edition): Let the computer automatically control the character and decide movement, shooting, defense and other behaviors according to the game situation.

    /// </summary>
    public class mlpAIController : mlpBaseAIController
    {
        /// <summary>
        /// Create an AI controller with default defensive behavior.
        /// </summary>
        /// <param name="player">The player object it belongs to</param>
        /// <param name="skillLevel">AI four-level skill index (0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell). </param>
        public mlpAIController(mlpPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// Factory method that returns the correct AI controller variant based on the controller ID.
        /// </summary>
        /// <param name="player">The player object it belongs to</param>
        /// <param name="brain">Controller identification string</param>

        /// <param name="skillLevel">AI four-level skill index (0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell). </param>
        /// <returns>The controller instance created. </returns>
        public static IBLPlayerController CreateForBrain(mlpPlayerObject player, string brain, int skillLevel)
        {
            var index = ParseBrainIndex(brain);
            return index == 1 ? new mlpAIController2(player, skillLevel) : new mlpAIController(player, skillLevel);
        }

        /// <summary>
        /// Returns false; the default AI does not use alternative defensive styles.

        /// </summary>
        /// <param name="holder">Ball carrier to be tested</param>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected override bool UseDefence2(mlpPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// Extracts the numeric variant index from the controller identification string like 'B1' or 'B2'.
        /// </summary>
        /// <param name="brain">Controller identification string</param>

        /// <returns>The parsed index value. </returns>
        private static int ParseBrainIndex(string brain)
        {
            if (string.IsNullOrEmpty(brain) || brain.Length < 2 || !brain.StartsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            return int.TryParse(brain.Substring(1, 1), out var value) ? value : 0;
        }
    }

    /// <summary>
    /// AI Controller (Premium): Smarter AI than the base version, used for tutorial mode opponents, with more complex behavioral decisions.
    /// </summary>
    public sealed class mlpAIController2 : mlpBaseAIController
    {
        /// <summary>
        /// Create an AI controller variant that uses an active tackling defensive style.

        /// </summary>
        /// <param name="player">The player object it belongs to</param>
        /// <param name="skillLevel">AI four-level skill index (0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell). </param>
        public mlpAIController2(mlpPlayerObject player, int skillLevel)
            : base(player, skillLevel)
        {
        }

        /// <summary>
        /// Returns true when the ball carrier is the opponent, enabling alternative defensive strategies.

        /// </summary>
        /// <param name="holder">Ball carrier to be tested</param>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected override bool UseDefence2(mlpPlayerObject holder)
        {
            return holder != null && holder.PlayerNo != player.PlayerNo;
        }

        /// <summary>
        /// Chasing the ball handler with steal timing and taking off from close range to interfere with shots.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected override void StrategyDefence2(float dt)
        {
            if (opponent == null)
            {
                return;
            }

            CurrentMove = MoveTo(defensePoint);
            var stealState = stealDelay.Update(dt);
            if (stealState == -1)
            {
                TryToSteal();
            }

            CurrentAction = stealState == 1;
            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseBehind(GetDefenceContestDistance());
        }
    }

    /// <summary>
    /// AI controller base class: implements common behavioral logic of AI (such as timer management, status judgment), and specific decisions are implemented by subclasses.

    /// </summary>
    public abstract class mlpBaseAIController : IBLPlayerController
    {
        // The player object reference to which it belongs

        protected readonly mlpPlayerObject player;
        // Current game difficulty (Easy/Normal/Hard/Hell)
        protected readonly mlpAiDifficulty difficulty;
        // Difficulty adjustment parameters (defensive distance, tackling distance, sprint range, etc.)

        protected readonly mlpAIDifficultyTuningProfile tuning;

        // Ball object reference in game

        protected mlpBallObject ball;
        // Opponent player list

        protected List<mlpPlayerObject> opponents;
        // Main opponents currently targeted

        protected mlpPlayerObject opponent;
        // AI skill configuration (probability and reaction rhythm parameters)

        protected readonly mlpAISkillProfile profile;

        // Jump ball delay timer: control the timing of the jump ball

        protected readonly NegativeDelay jumpBall;
        // Attack Delay Timer: Controls shot release timing (combines range and fixed delay)

        protected readonly FullDelay attack;
        // Quick takeoff delay timer: Delay for taking off immediately after landing

        protected readonly SimpleDelay attackJumpDelay;
        // Steal delay timer: controls steal cooldown and usage time

        protected readonly AIUseDelay stealDelay;
        // Defense delay timer: Control the timing of defense interference when taking off

        protected readonly SimpleDelay defenceDelay;
        // Block delay timer: the cooldown time of the block action

        protected readonly FullDelay blockDelay;
        // Rebound delay timer: Timing control when jumping to grab rebounds
        protected readonly FullDelay reboundDelay;
        // Movement delay timer: controls the frequency of movement decisions (not decisions every frame)

        protected readonly FullDelay moveDelay;
        // Sprint Decision Delay Timer: Cooldown Control Used by Sprints

        protected readonly AIUseDelay dashDecisionDelay;
        // Super dunk delay timer: when to use the ultimate dunk

        protected readonly FullDelay megaDunkDelay;
        // Super Sprint Delay Timer: When to use the ultimate sprint

        protected readonly FullDelay superDashDelay;

        // Current strategy number: 0=defense, 1=scramble, 2=offense, 3=jump ball, 4=rebound, 5=aggressive defense

        protected int strategy;
        // Attack target X coordinate (shooting release position)

        protected float attackPoint;
        // X coordinate of the starting point (jump towards the attack point after arriving)

        protected float jumpPoint;
        // Current rebound target X coordinate

        protected float reboundPoint;
        // Offensive rebound position

        protected float reboundPointInAttack;
        // Defensive rebound position

        protected float reboundPointInDefence;
        // Defensive end base position (assigned based on player number)

        protected float baseEndPoint;
        // Current defensive end position (may be dynamically adjusted due to long periods of immobility)

        protected float endPoint;
        // Defensive station point X coordinate

        protected float defensePoint;
        // Accumulated time of no action: reset the defensive boundary when exceeding the threshold to prevent passive defense

        protected float deltaDownTime;
        // Shooting direction: -1 to fly left, +1 to fly right

        protected float directionToFly;
        // Whether you have reached the starting point and can take off for shooting

        protected bool attackJump;
        // Whether to jump to avoid tackles

        protected bool avoidStealJump;
        // Side movement direction when avoiding a tackle: -1 left, 0 none, +1 right

        protected int avoidStealMove;
        // Was he deceived by his opponent's fake moves (being swayed away)?

        protected bool isPumped;
        // The number of times you were deceived by feints this round (maximum 3 times)

        protected int pumpCount;
        // Whether you have lined up a rebound to take off (prepare in advance when the opponent shoots)

        protected bool queuedReboundJump;
        // Player number: 0=main player, 1=auxiliary

        protected int playerNo;
        // Whether the first run-time reference acquisition has been completed

        protected bool initialized;
        // Attack area starting X coordinate
        protected float attackZoneStart;
        // Attack area end X coordinate

        protected float attackZoneEnd;
        // Start X coordinate of sprint area

        protected float dashZoneStart;
        // Sprint area end X coordinate

        protected float dashZoneEnd;
        // Whether to take off and shoot immediately after landing (at the moment of catching the ball or landing)

        protected bool willAttackAtOnce;
        // Whether the ultimate move input is queued (effective in the next frame)

        protected bool queuedSuperInput;
        // Dead zone distance for movement determination: When the distance to the target is less than this value, it is deemed to have been reached.

        protected const float DeltaDistance = 20f;
        // No action timeout threshold: Reset the defensive boundary after this number of seconds

        protected const float DownTime = 5f;

        // Current moving direction output: -1 for left movement, 0 for stationary, +1 for right movement

        public int CurrentMove { get; protected set; }
        // Current jump output

        public bool CurrentJump { get; protected set; }
        // Current action output: shooting when holding the ball, stealing when defending

        public bool CurrentAction { get; protected set; }
        // Current block/fake output

        public bool CurrentBlockOrPump { get; protected set; }
        // Current ultimate output

        public bool CurrentSuper { get; protected set; }
        // Current sprint direction output: -1 for left dash, 0 for none, +1 for right dash

        public int CurrentDash { get; protected set; }

        /// <summary>
        /// Initialize shared AI state: difficulty configuration, decision timer, and attack/defense zones.

        /// </summary>
        /// <param name="player">The player object it belongs to</param>
        /// <param name="skillLevel">AI four-level skill index (0 = Easy, 1 = Normal, 2 = Hard, 3 = Hell). </param>
        protected mlpBaseAIController(mlpPlayerObject player, int skillLevel)
        {
            // 1. Save player object reference and number

            this.player = player;
            // 2. Read the current difficulty setting (easy/normal/hard/hell) and the corresponding adjustment parameters

            difficulty = mlpInventory.Instance.Difficulty;
            tuning = mlpAIDifficultyTuning.Get(difficulty);
            // 3. Load AI skill configuration according to the four-level skill index (control shooting timing, steal probability, etc.)

            profile = mlpAISkillsData.Get(skillLevel);
            // 4. Initialize various decision delay timers (control AI not to make decisions every frame, simulate human reaction time)

            jumpBall = new NegativeDelay(mlpObjectsData.IdealJumpBallJump, profile.JumpBall);
            attack = new FullDelay(mlpObjectsData.IdealAttackJump, profile.Attack);
            attackJumpDelay = new SimpleDelay(profile.AttackAtOnce);
            stealDelay = new AIUseDelay(0.1f, mlpObjectsData.StealDuration + profile.DelaySteal);
            defenceDelay = new SimpleDelay(profile.Defence);
            blockDelay = new FullDelay(0f, 0.2f);
            reboundDelay = new FullDelay(profile.ReboundRange, profile.ReboundFixed);
            moveDelay = new FullDelay(profile.MoveDelay, 0.05f);
            dashDecisionDelay = new AIUseDelay(0.1f, profile.DelayDash);
            megaDunkDelay = new FullDelay(0.5f, 0.5f);
            superDashDelay = new FullDelay(0.5f, 0.5f);
            // 5. Record player numbers and subscribe to game signals (opponent jumps, steals, feints, etc.)

            playerNo = player.PlayerNo;
            player.GameCore.PlayerSignals.OnSignal += ProcessPlayerSignal;
            // 6. Calculate the offensive/defensive/rebounding position areas and reset all states

            InitZones();
            ResetForRestart();
        }

        /// <summary>
        /// Run a complete AI decision loop: select a strategy based on the state of the ball, and then call the corresponding strategy method.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        public virtual void UpdateController(float dt)
        {
            // 1. Get references to ball and opponent on first run

            EnsureRuntimeLinks();
            // 2. Reset sprint and apply the ultimate input queued in the previous frame

            CurrentDash = 0;
            CurrentSuper = queuedSuperInput;
            queuedSuperInput = false;

            // 3. If ball or opponent reference is lost, clear all inputs

            if (ball == null || opponents == null || opponents.Count == 0)
            {
                CurrentMove = 0;
                CurrentJump = false;
                CurrentAction = false;
                CurrentSuper = false;
                return;
            }

            // 4. Handle the "quick takeoff" delay timer - used in scenarios where you take off and shoot immediately after landing.

            var delayedJump = attackJumpDelay.Update(dt);
            if (delayedJump >= 0)
            {
                if (delayedJump == 1)
                {
                    // Timer expires: Execute jump

                    CurrentMove = 0;
                    CurrentJump = true;
                    CurrentAction = false;
                }
                else
                {
                    // The timer has not expired: keep moving and jumping to avoid tackles

                    CurrentMove = avoidStealMove;
                    CurrentJump = avoidStealJump;
                }

                return;
            }

            // 5. Choose AI strategy based on the state of the ball

            var holder = player.GameCore.FindBallHolder();
            if (player.WithBall)
            {
                // 5a. Hold the ball by yourself → Offensive strategy: move towards the shooting point and take off to shoot at the right time

                if (strategy != 2)
                {
                    HandleBallInOwnHands();
                }

                StrategyAttack(dt);
            }
            else if (holder != null && holder.Side != player.Side)
            {
                // 5b. The opponent has the ball → Defensive strategy: follow the opponent and try to steal or interfere with the shot
                opponent = holder;
                if (UseDefence2(holder))
                {
                    strategy = 5;
                    StrategyDefence2(dt);
                }
                else
                {
                    if (strategy != 0)
                    {
                        HandleBallInOpponentsHands();
                    }

                    StrategyDefence(dt);
                }
            }
            else
            {
                // 5c. No one has the ball

                var shotInFlight = ball.State == "shooting" || ball.State == "basket";
                if (shotInFlight)
                {
                    // The ball is in the air (shooting/rebounding) → Rebounding strategy: Seize the rebounding position

                    if (strategy != 4)
                    {
                        if (ball.Side == player.Side)
                        {
                            BallOwnShoot(0);
                        }
                        else
                        {
                            BallOpponentShoot(0);
                        }
                    }

                    StrategyRebound(dt);
                }
                else
                {
                    // The ball bounces on the ground → Faceoff Strategy: Chase the ball and try to pick it up

                    if (strategy != 1)
                    {
                        HandleBallOthers();
                    }

                    StrategyBallFight(dt);
                }
            }
        }

        /// <summary>
        /// Switch to offensive mode and optionally activate Super Dunk Delay.

        /// </summary>
        /// <param name="holderPlayerNo">The player number of the current ball holder</param>
        public virtual void BallInOwnHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            if (player.UsesPossessionSkill && player.ReadyForSuper)
            {
                megaDunkDelay.Activate();
            }

            HandleBallInOwnHands();
        }

        /// <summary>
        /// Switch to defensive mode and optionally use Freezing Kill when the opponent is close.

        /// </summary>
        /// <param name="holderPlayerNo">The player number of the current ball holder</param>
        public virtual void BallInOpponentsHands(int holderPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(holderPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            if (player.UsesFreezeSkill && player.ReadyForSuper && opponent != null && Mathf.Abs(player.Position.x - opponent.Position.x) <= 220f)
            {
                player.SuperShot();
            }

            HandleBallInOpponentsHands();
        }

        /// <summary>
        /// After a teammate shoots, switch to rebounding mode and prepare your position for the offensive rebound.
        /// </summary>
        /// <param name="shooterPlayerNo">The player number of the shooter</param>

        public virtual void BallOwnShoot(int shooterPlayerNo)
        {
            EnsureRuntimeLinks();
            ResetCurrents();
            queuedReboundJump = false;
            strategy = 4;
            reboundPoint = reboundPointInAttack;
            superDashDelay.Reset();
        }

        /// <summary>
        /// Switch to rebounding mode after the opponent shoots; optionally activate the shield to kill.

        /// </summary>
        /// <param name="shooterPlayerNo">The player number of the shooter</param>

        public virtual void BallOpponentShoot(int shooterPlayerNo)
        {
            EnsureRuntimeLinks();
            opponent = FindOpponentByPlayerNo(shooterPlayerNo) ?? player.GameCore.FindBallHolder(-player.Side) ?? opponent;
            if (player.UsesShieldSkill && player.ReadyForSuper)
            {
                player.SuperShot();
            }

            ResetCurrents();
            strategy = 4;
            reboundPoint = reboundPointInDefence;
            superDashDelay.Reset();
            TryUseHellBonusShieldAgainstHumanShot();
            queuedReboundJump = opponent != null &&
                                opponent.IsGrounded &&
                                IsOpponentCloseBehind(120f) &&
                                UnityEngine.Random.value <= profile.JumpThrow;
        }

        /// <summary>
        /// Switch to scramble ball mode, chase the ball and try to pick it up.

        /// </summary>
        public virtual void BallOthers()
        {
            EnsureRuntimeLinks();
            HandleBallOthers();
        }

        /// <summary>
        /// Always returns true; the AI ​​controller is ready to perform actions.
        /// </summary>
        /// <returns>Always returns true. </returns>
        public virtual bool ReadyForAction()
        {
            return true;
        }

        /// <summary>
        /// Returns true when the block timer expires, ending the AI's block attempt.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        public virtual bool ReleaseBlockOrPump(float dt)
        {
            return blockDelay.Update(dt) == 1;
        }

        /// <summary>
        /// Reset all AI states for the new turn.
        /// </summary>
        /// <param name="startSide">The player's initial position and direction after reset</param>

        public virtual void Restart(int startSide)
        {
            ResetForRestart();
        }

        /// <summary>
        /// Reset the feint state. If you hold the ball, you can choose to immediately queue up to attack and jump.

        /// </summary>
        public virtual void PlayerOnGround()
        {
            isPumped = false;
            pumpCount = 0;
            if (player.WithBall && willAttackAtOnce)
            {
                ResetCurrents();
                attackJumpDelay.Activate();
            }
        }

        /// <summary>
        /// If the sprint exceeds the target position, adjust the attack point.

        /// </summary>
        public virtual void PlayerOnDashEnd()
        {
            if ((player.Position.x - attackPoint) * player.Side < 0f)
            {
                attackPoint = player.Position.x - 10f * player.Side;
            }
        }

        /// <summary>
        /// Clears the cap input and starts the cap cooldown timer.

        /// </summary>
        public virtual void PlayerOnBlock()
        {
            CurrentBlockOrPump = false;
            blockDelay.Activate();
        }

        /// <summary>
        /// Returns false; subclasses can override this method to enable alternative defensive strategies.

        /// </summary>
        /// <param name="holder">Ball carrier to be tested</param>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected virtual bool UseDefence2(mlpPlayerObject holder)
        {
            return false;
        }

        /// <summary>
        /// Follow the ball handler, attempt steals at close range, and disrupt shots with timed jumps.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected virtual void StrategyDefence(float dt)
        {
            // 1. If there is no opponent information, exit directly

            if (opponent == null)
            {
                return;
            }

            // 2. Try to use super sprint to intercept the opponent holding the ball. If successful, no other operations will be performed in this frame.

            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashAgainstHolder()))
            {
                return;
            }

            // 3. Updated tackling and movement delay timers

            var stealState = stealDelay.Update(dt);
            var moveState = moveDelay.Update(dt);
            // 4. If you are fooled by a fake move ("swayed"), stop still.

            if (isPumped)
            {
                CurrentMove = 0;
            }
            else
            {
                // 5. Calculate the defensive target position: if the opponent exceeds the defensive boundary, hold the boundary, otherwise follow the opponent a certain distance behind

                var target = (opponent.Position.x - endPoint) * player.Side < 0f
                    ? endPoint
                    : opponent.IsGrounded
                        ? opponent.Position.x + player.Side * mlpObjectsData.OpponentDelta
                        : opponent.Position.x + player.Side * (mlpObjectsData.OpponentDelta - 10f);

                // 6. When standing on the ground, follow the movement delay rhythm and follow the opponent directly when jumping.

                if (player.IsGrounded)
                {
                    if (moveState == -1)
                    {
                        CurrentMove = MoveTo(target);
                        moveDelay.Activate();
                    }
                }
                else
                {
                    CurrentMove = MoveTo(opponent.Position.x + player.Side * (mlpObjectsData.OpponentDelta - 10f));
                }

                // 7. When the steal timer expires, attempt a steal
                if (stealState == -1)
                {
                    TryToSteal();
                }
            }

            // 8. When the block timer expires and the opponent is nearby, jump to interfere with the shot.

            CurrentJump = defenceDelay.Update(dt) == 1 && IsOpponentCloseAbs(GetDefenceContestDistance());
            // 9. Perform a steal when the steal timer is activated.

            CurrentAction = stealState == 1;
            // 10. If there is no action for a long time (standing still), reset the defensive boundary to the other side of the field to prevent passive defense

            if (!CurrentAction && !CurrentJump && CurrentMove == 0)
            {
                deltaDownTime += dt;
                if (deltaDownTime >= DownTime)
                {
                    endPoint = player.Side == 1 ? 0f : mlpConstants.Width;
                    deltaDownTime = 0f;
                }
            }
            else
            {
                deltaDownTime = 0f;
            }
        }

        /// <summary>
        /// Delegate to the default defense strategy; subclasses can override to implement custom behavior.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected virtual void StrategyDefence2(float dt)
        {
            StrategyDefence(dt);
        }

        /// <summary>
        /// Chase the free ball and jump for rebounds as the ball approaches the basket.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected virtual void StrategyBallFight(float dt)
        {
            // 1. Get the target X coordinate of the free ball (Hell difficulty will predict the landing point), and move in the direction of the ball (with a little offset to avoid hitting the ball)

            var ballX = GetTechnicalLooseBallTargetX();
            var offset = ballX - player.Position.x >= 0f ? 10f : -10f;
            CurrentMove = MoveTo(ballX + offset);
            CurrentJump = false;

            // 2. If the ball is in the air and far enough away, try to use super sprint to pick it up first

            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashForBall()))
            {
                return;
            }

            // 3. Determine whether to take off based on the state of the ball

            if (ball.State != "bounce" && ball.State != "shooting")
            {
                if (ball.State == "basket")
                {
                    // 3a. The ball bounces near the basket → Fight for rebounds: Use the rebound delay timer and take off in the rebound area

                    var reboundState = reboundDelay.Update(dt);
                    if (reboundState == -1 && IsBallInReboundZone())
                    {
                        reboundDelay.Activate();
                    }
                    else
                    {
                        CurrentJump = reboundState == 1 && UnityEngine.Random.value < profile.ChanceToRebound && IsBallInReboundZone();
                    }
                }
                else
                {
                    // 3b. The ball is in other states (such as stealing) → takes off when the horizontal distance is short and the vertical distance is long.

                    CurrentJump = Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
                }
            }

            // 4. Do not perform shooting/stealing and other actions while scrambling for the ball.

            CurrentAction = false;
        }

        /// <summary>
        /// Move toward the point of attack, time your shot and react to nearby defenders.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected virtual void StrategyAttack(float dt)
        {
            // 1. If you don’t have the ball, exit directly

            if (!player.WithBall)
            {
                return;
            }

            // 2. If you have a dunk-type ultimate move and the timer expires, trigger the ultimate move to shoot.

            if (player.UsesPossessionSkill && megaDunkDelay.Update(dt) == 1)
            {
                TriggerSuperInput();
                return;
            }

            // 3. Try to use super sprint to throw off the defender

            if (TryUseDelayedSuperDash(dt, ShouldUseSuperDashInAttack()))
            {
                return;
            }

            // 4. If you are performing an action to dodge a tackle (jumping or moving sideways), give priority to completing the dodge.

            if (avoidStealJump || avoidStealMove != 0)
            {
                CurrentMove = avoidStealMove;
                CurrentJump = avoidStealJump;
                return;
            }

            // 5. Offensive logic when standing on the ground

            if (player.IsGrounded)
            {
                // 5a. Make a decision when the movement delay timer expires

                if (moveDelay.Update(dt) == -1)
                {
                    // 5b. Move towards the jump point/attack point and determine whether you should take off and shoot.

                    var move = MoveInAttack();
                    if (attackJump)
                    {
                        // 5c. Reach the jumping point → take off and shoot

                        CurrentJump = true;
                        CurrentMove = move;
                    }
                    else if (IsAICloserForBasket())
                    {
                        // 5d. AI is closer to the basket than the opponent → can lay up directly

                        if (move == -player.Side)
                        {
                            CurrentMove = -player.Side;
                            CurrentJump = false;
                        }
                        else
                        {
                            CurrentMove = move;
                            CurrentJump = true;
                        }
                    }
                    else
                    {
                        // 5e. Opponent blocks in front → Handle various defensive situations

                        CurrentJump = false;
                        CurrentDash = 0;
                        if (IsOpponentCloseBehind())
                        {
                            // 5f. The opponent catches up from behind and applies pressure.

                            if (IsUnderOwnBasket())
                            {
                                // 5g. Under your own basket → Use sprint to get rid of it (not easy difficulty)
                                if (player.ReadyForDash && difficulty != mlpAiDifficulty.Easy)
                                {
                                    CurrentDash = -player.Side;
                                    CurrentMove = 0;
                                }
                                else
                                {
                                    CurrentMove = UnityEngine.Random.value <= 0.5f ? -player.Side : 0;
                                    moveDelay.Activate();
                                }
                            }
                            else if (UnityEngine.Random.value <= profile.ReactOnOpponent)
                            {
                                // 5h. There is a certain probability of reacting to the defense behind

                                CurrentJump = false;
                                if (player.ReadyForDash && InDashingZone() && UnityEngine.Random.value <= profile.MakeDash && difficulty != mlpAiDifficulty.Easy)
                                {
                                    CurrentDash = -player.Side;
                                }
                                else
                                {
                                    CurrentMove = UnityEngine.Random.value <= 0.5f ? 0 : player.Side;
                                    moveDelay.Activate();
                                }
                            }
                            else
                            {
                                // 5i. Didn’t react → Continue moving towards the basket

                                CurrentMove = -player.Side;
                                moveDelay.Activate();
                            }
                        }
                        else
                        {
                            // 5j. Opponent not behind → Safely advance to basket

                            CurrentMove = -player.Side;
                        }
                    }

                    // 5k. If a jump shot has been decided, activate the attack timer and calculate the flight direction

                    if (attackJump)
                    {
                        attack.Activate();
                        directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                    }
                }
            }
            else
            {
                // 6. While in the air: maintain flight direction and shoot when the attack timer expires

                CurrentMove = (player.Position.x - attackPoint) * directionToFly > 0f ? Mathf.RoundToInt(directionToFly) : 0;
                CurrentJump = false;
                CurrentAction = attack.Update(dt) == 1;
            }
        }

        /// <summary>
        /// Stand still and grasp the timing of the jump according to the jump ball delay configuration.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected virtual void StrategyJumpBall(float dt)
        {
            CurrentMove = 0;
            CurrentJump = jumpBall.Update(dt) == 1;
            CurrentAction = false;
        }

        /// <summary>
        /// Position the backboard to take off when the ball enters the backboard area.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        protected virtual void StrategyRebound(float dt)
        {
            // 1. If you have the "guaranteed block" ultimate move and the opponent throws a ball that can be blocked, the ultimate move will be triggered.

            if (player.UsesGuaranteedBlockSkill && player.ReadyForSuper && ball != null && ball.IsBlockable && ball.Side != player.Side)
            {
                TriggerSuperInput();
                return;
            }

            // 2. If you have the "Backboard Magnet" ultimate move and the ball bounces near the basket, the ultimate move will be triggered

            if (player.UsesReboundMagnetSkill && player.ReadyForSuper && ball != null && ball.State == "basket")
            {
                TriggerSuperInput();
                return;
            }

            // 3. If the ball bounces near the rim, try to super sprint to get on the rebound

            if (TryUseDelayedSuperDash(dt, ball != null && ball.State == "basket" && ShouldUseSuperDashForBall()))
            {
                return;
            }

            // 4. Process the "queued jump in advance" command (such as early jump interference when the opponent shoots)

            var contestJump = queuedReboundJump && player.IsGrounded;
            if (contestJump)
            {
                queuedReboundJump = false;
            }

            // 5. Only take off when the ball is around you (horizontally close, vertically far) or there is a queue to take off.

            CurrentJump = contestJump || (Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f);
            // 6. Hell difficulty uses ballistics to predict the impact point, other difficulties use fixed rebound positions.

            var targetReboundPoint = ShouldUseTechnicalPrediction() ? GetTechnicalReboundTargetX() : reboundPoint;
            // 7. Stay still when taking off, then move toward the backboard after landing.

            CurrentMove = CurrentJump ? 0 : player.IsGrounded ? MoveTo(targetReboundPoint) : 0;
            // 8. Do not perform shooting/stealing actions during the rebound phase

            CurrentAction = false;
        }

        /// <summary>
        /// According to the distance and difficulty configuration, steal judgment is made when approaching the opponent.

        /// </summary>
        protected void TryToSteal()
        {
            // 1. No steals on easy difficulty; no steals when the opponent is empty or in the air

            if (difficulty == mlpAiDifficulty.Easy || opponent == null || !opponent.IsGrounded)
            {
                return;
            }

            // 2. If you approach the opponent from behind, decide whether to initiate a steal based on skill probability.

            if (IsOpponentCloseBehind(GetStealBehindDistance()))
            {
                if (UnityEngine.Random.value <= profile.MakeSteal)
                {
                    stealDelay.Activate();
                }
                else
                {
                    stealDelay.SkipIt();
                }
            }
            // 3. If the opponent is near the basket (more dangerous), the steal probability increases to 1.5 times

            else if (IsOpponentCloseToBasket(GetStealBasketDistance()))
            {
                if (UnityEngine.Random.value <= 1.5f * profile.MakeSteal)
                {
                    stealDelay.Activate();
                }
                else
                {
                    stealDelay.SkipIt();
                }
            }
        }

        /// <summary>
        /// Activates Super Sprint with a short delay, conditions and cooldown permitting.

        /// </summary>
        /// <param name="dt">Frame interval time (seconds)</param>

        /// <param name="shouldUse">True when the AI should attempt a super sprint</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool TryUseDelayedSuperDash(float dt, bool shouldUse)
        {
            // 1. Check if there is a super sprint available (either with the character or as a hell difficulty reward) and if the current scenario is worth using

            var canUseNativeSuperDash = player.UsesDashSkill && player.ReadyForSuper;
            var canUseHellBonusSuperDash = player.CanUseHellBonusSuperDash;
            // 2. If there is no super sprint available or the scene does not require it, reset the timer and return

            if ((!canUseNativeSuperDash && !canUseHellBonusSuperDash) || !shouldUse)
            {
                superDashDelay.Reset();
                return false;
            }

            // 3. Update the super sprint delay timer
            var state = superDashDelay.Update(dt);
            // 4. The timer is activated for the first time (-1 means the timer has just started)

            if (state == -1)
            {
                superDashDelay.Activate();
                return false;
            }

            // 5. The timer has not expired yet, continue to wait.

            if (state != 1)
            {
                return false;
            }

            // 6. The timer expires → Use the character’s own super sprint

            if (canUseNativeSuperDash)
            {
                TriggerSuperInput();
                superDashDelay.Reset();
                return true;
            }

            // 7. Try using the Hell Difficulty Bonus Super Sprint

            if (player.TryUseHellBonusSuperDash())
            {
                superDashDelay.Reset();
                return true;
            }

            // 8. If both fail, reset the timer

            superDashDelay.Reset();
            return false;
        }

        /// <summary>
        /// Set the kill input flag and clear other inputs to activate the kill move.

        /// </summary>
        protected void TriggerSuperInput()
        {
            ResetCurrents();
            CurrentSuper = true;
            CurrentBlockOrPump = false;
        }

        /// <summary>
        /// Returns true when the opponent with the ball is within ideal super rush range.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldUseSuperDashAgainstHolder()
        {
            if (opponent == null || !opponent.WithBall || !player.IsGrounded)
            {
                return false;
            }

            var distance = Mathf.Abs(player.Position.x - opponent.Position.x);
            return distance >= GetHolderSuperDashMinDistance() && distance <= GetHolderSuperDashMaxDistance();
        }

        /// <summary>
        /// Returns true when the free ball is high enough and far enough to merit using Super Sprint.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldUseSuperDashForBall()
        {
            if (ball == null || !ball.IsInGame || !player.IsGrounded)
            {
                return false;
            }

            if (ball.State == "shooting" || ball.State == "score" || ball.State == "alleyOop")
            {
                return false;
            }

            return ball.Position.y > mlpObjectsData.BasketHeight &&
                   Mathf.Abs(DeltaBallX()) >= GetLooseBallSuperDashDistance();
        }

        /// <summary>
        /// Returns true when the opponent is applying pressure from behind or the basket is far enough away to merit a super rush.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldUseSuperDashInAttack()
        {
            if (!player.WithBall || !player.IsGrounded)
            {
                return false;
            }

            if (opponent != null && IsOpponentCloseBehind(GetAttackPressureDistance()))
            {
                return true;
            }

            return Mathf.Abs(player.Position.x - player.AttackTargetX) >= GetAttackSuperDashDistance() &&
                   InDashingZone();
        }

        /// <summary>
        /// Initialize the attack mode: set the attack point, clear the delay, and prepare the shooting route.

        /// </summary>
        protected void HandleBallInOwnHands()
        {
            // 1. Reset all delay timers and input states and switch to the offensive strategy (strategy number 2)

            ResetBaseDelays();
            ResetCurrents();
            queuedReboundJump = false;
            strategy = 2;
            superDashDelay.Reset();

            // 2. Determine the shooting target point based on the player’s position on the court

            var reboundZone = IsReboundInAttackZone();
            if (reboundZone == -1)
            {
                // 2a. Before the offensive zone (far away from the basket) → Set an attack point near the basket, and if it is in the air, prepare to shoot immediately

                willAttackAtOnce = !player.IsGrounded;
                SetAttackPoint(150f, player.Position.x);
            }
            else if (reboundZone == 0)
            {
                // 2b. In the offensive zone → Shoot from your current position, or immediately if in the air

                willAttackAtOnce = !player.IsGrounded;
                var currentX = player.Position.x;
                SetAttackPoint(currentX, currentX);
            }
            else
            {
                // 2c. Behind the offensive zone (deep into the opponent's half) → automatically select the best shooting point

                SetAttackPoint(0f, 0f);
                willAttackAtOnce = !player.IsGrounded && Mathf.Abs(player.Position.x - attackPoint) < 50f;
            }
        }

        /// <summary>
        /// Initialize defensive mode: identify the opponent's ball carrier and clear delays.

        /// </summary>
        protected void HandleBallInOpponentsHands()
        {
            // 1. Switch to defensive strategy (number 0)

            strategy = 0;
            // 2. Reset all delay timers and current inputs

            ResetBaseDelays();
            ResetCurrents();
            // 3. Clear the remaining status of rebounds, offenses, fake actions, etc.

            queuedReboundJump = false;
            willAttackAtOnce = false;
            isPumped = false;
            superDashDelay.Reset();
            // 4. Find the opponent currently holding the ball as a defensive target

            opponent = player.GameCore.FindBallHolder(-player.Side);
        }

        /// <summary>
        /// Initialize scramble ball mode: Clear lag and get ready to chase the ball.

        /// </summary>
        protected void HandleBallOthers()
        {
            // 1. Switch to scrum strategy (number 1)

            strategy = 1;
            // 2. Reset current input

            ResetCurrents();
            // 3. Clear queue rebound jump and super sprint delays
            queuedReboundJump = false;
            superDashDelay.Reset();
        }

        /// <summary>
        /// Respond to game signals (tackle, jump, feint, dash, stun), adjust AI inputs and timers.

        /// </summary>
        /// <param name="signal">Player event signal type</param>

        /// <param name="side">Field direction (-1 is left, 1 is right)</param>

        /// <param name="signalPlayerNo">The player number that triggered the signal</param>

        protected void ProcessPlayerSignal(mlpPlayerSignalType signal, int side, int signalPlayerNo)
        {
            // 1. Receive the "start tackling" signal → handle the tackling response (dodge or follow up)

            if (signal == mlpPlayerSignalType.StartSteal)
            {
                PlayerStartSteal(side);
                return;
            }

            // 2. Receive the "steal completed" signal → If it is an opponent's steal, clear the avoidance state

            if (signal == mlpPlayerSignalType.Steal)
            {
                if (side == -player.Side)
                {
                    ResetAvoidSteal();
                }

                return;
            }

            // 3. Receive "take off" signal

            if (signal == mlpPlayerSignalType.JumpA)
            {
                if (side == player.Side && signalPlayerNo == playerNo)
                {
                    // 3a. Take off by yourself → clear the avoidance state, activate the attack timer, and record the flight direction

                    ResetAvoidSteal();
                    attack.Activate();
                    directionToFly = player.Position.x - attackPoint >= 0f ? -1f : 1f;
                }
                else if (side == -player.Side)
                {
                    // 3b. The opponent takes off → decide whether to follow the takeoff to interfere with the shot based on difficulty and probability

                    if (ShouldUsePerfectContestOnJump() || UnityEngine.Random.value <= profile.JumpThrow)
                    {
                        defenceDelay.Activate();
                    }
                }

                return;
            }

            // 4. Receive a "feint" signal → Your opponent may be tricked into jumping when making a fake move

            if (signal == mlpPlayerSignalType.Pump)
            {
                if (side == -player.Side && player.CanAct && IsOpponentCloseBehind(90f))
                {
                    // 4a. Limit being cheated up to 3 times

                    if (++pumpCount <= 3)
                    {
                        // 4b. In Hell difficulty, it is possible to see through fake moves and not be deceived.
                        if (ShouldIgnorePumpFake())
                        {
                            return;
                        }

                        // 4c. Being cheated according to probability: jump to defend and stop moving, marked as "being shaken"

                        if (UnityEngine.Random.value <= profile.JumpPump)
                        {
                            defenceDelay.Activate();
                            stealDelay.Reset();
                            CurrentMove = 0;
                            isPumped = true;
                        }
                    }
                }

                return;
            }

            // 5. Receive "Sprint" signal

            if (signal == mlpPlayerSignalType.Dash)
            {
                if (side == player.Side)
                {
                    // 5a. Teammate sprints → Reset attack timer (teammate is running, re-plan)

                    attack.Reset();
                }
                else if (strategy == 0 && player.CanAct && IsOpponentInRangeBehind(40f, GetDashBlockRangeMaxDistance()))
                {
                    // 5b. The opponent is sprinting behind → try to block the shot based on probability

                    if (UnityEngine.Random.value <= profile.MakeBlock)
                    {
                        ResetCurrents();
                        ResetAllDelays();
                        CurrentBlockOrPump = true;
                        blockDelay.Activate();
                    }
                }

                return;
            }

            // 6. Receive a "stun" signal → If you are stunned, reset all timers

            if (signal == mlpPlayerSignalType.Stun && side == player.Side)
            {
                ResetAllDelays();
            }
        }

        /// <summary>
        /// React when the opponent starts to tackle and try to dodge if you have the ball.
        /// </summary>
        /// <param name="side">Field direction (-1 is left, 1 is right)</param>

        protected void PlayerStartSteal(int side)
        {
            if (side == -player.Side)
            {
                if (player.WithBall && player.IsGrounded && (IsOpponentCloseBehind(80f) || (IsOpponentCloseBehind(140f) && opponent != null && opponent.IsMoving)))
                {
                    TryToAvoid();
                }
            }
            else
            {
                stealDelay.UseIt();
            }
        }

        /// <summary>
        /// Dodge incoming tackles by sprinting, jumping or moving sideways.
        /// </summary>
        protected void TryToAvoid()
        {
            if (UnityEngine.Random.value > profile.AvoidSteal || player.Position.x > 600f)
            {
                return;
            }

            var chance = UnityEngine.Random.value;
            if (chance <= 0.1f && player.ReadyForDash)
            {
                CurrentDash = -player.Side;
                return;
            }

            if (chance <= 0.4f && IsInAttackZone())
            {
                avoidStealJump = true;
                moveDelay.Reset();
                return;
            }

            avoidStealMove = player.Side;
        }

        /// <summary>
        /// Returns the distance threshold for interference shots, from the difficulty tuning configuration.
        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetDefenceContestDistance()
        {
            return tuning.DefenceContestDistance;
        }

        /// <summary>
        /// Returns the distance threshold for tackling from behind, from difficulty tuning configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetStealBehindDistance()
        {
            return tuning.StealBehindDistance;
        }

        /// <summary>
        /// Returns the distance threshold for steals near the basket, from the difficulty adjustment configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetStealBasketDistance()
        {
            return tuning.StealBasketDistance;
        }

        /// <summary>
        /// Minimum ball carrier distance required to return super sprint, from tuning configuration.
        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetHolderSuperDashMinDistance()
        {
            return tuning.HolderSuperDashMinDistance;
        }

        /// <summary>
        /// Returns the maximum ball carrier distance required to super sprint, from the tuning configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetHolderSuperDashMaxDistance()
        {
            return tuning.HolderSuperDashMaxDistance;
        }

        /// <summary>
        /// Minimum ball distance required to return free ball super sprint, from tuning configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetLooseBallSuperDashDistance()
        {
            return tuning.LooseBallSuperDashDistance;
        }

        /// <summary>
        /// Returns the rear distance threshold for triggering a super sprint escape during an attack, from the tuning configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetAttackPressureDistance()
        {
            return tuning.AttackPressureDistance;
        }

        /// <summary>
        /// The distance to the rim that is worth using Super Sprint in the return offense comes from the tuning configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetAttackSuperDashDistance()
        {
            return tuning.AttackSuperDashDistance;
        }

        /// <summary>
        /// Returns the maximum distance the AI ​​will attempt to sprint a block, from the tuning configuration.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetDashBlockRangeMaxDistance()
        {
            return tuning.DashBlockRangeMaxDistance;
        }

        /// <summary>
        /// On Hell difficulty, extra shields are activated when human players take threatening shots.

        /// </summary>
        protected void TryUseHellBonusShieldAgainstHumanShot()
        {
            if (difficulty != mlpAiDifficulty.Hell || opponent == null || !opponent.IsHuman)
            {
                return;
            }

            var threateningShot =
                player.GameCore.IsCurrentShotThreePointer(opponent.Side) ||
                player.GameCore.RemainingMatchTime <= 15f ||
                player.GameCore.GetScoreLeadForSide(opponent.Side) >= 4;

            if (threateningShot)
            {
                player.TryUseHellBonusShield();
            }
        }

        /// <summary>
        /// Calculate offensive, sprinting, defensive and rebounding positions based on player position direction and number.

        /// </summary>
        protected void InitZones()
        {
            // 1. Set the boundaries of the attack and sprint areas according to the player's camp (left=1, right=-1)

            if (player.Side == 1)
            {
                // 1a. Right camp: directly use the area value in the configuration

                attackZoneStart = mlpObjectsData.AttackZoneStart;
                attackZoneEnd = mlpObjectsData.AttackZoneEnd;
                dashZoneStart = mlpObjectsData.DashZoneStart;
                dashZoneEnd = mlpObjectsData.DashZoneEnd;
                // 1b. Set different defensive/rebounding positions according to player number (0=main player, 1=auxiliary)

                if (playerNo == 0)
                {
                    baseEndPoint = 280f;
                    reboundPointInAttack = 190f;
                    reboundPointInDefence = 610f;
                }
                else
                {
                    baseEndPoint = 400f;
                    reboundPointInAttack = 150f;
                    reboundPointInDefence = 680f;
                }
            }
            else
            {
                // 1c. Left camp: Mirror the coordinates (the field is symmetrical about the center)

                attackZoneStart = mlpConstants.Width - mlpObjectsData.AttackZoneEnd;
                attackZoneEnd = mlpConstants.Width - mlpObjectsData.AttackZoneStart;
                dashZoneStart = mlpConstants.Width - mlpObjectsData.DashZoneEnd;
                dashZoneEnd = mlpConstants.Width - mlpObjectsData.DashZoneStart;
                if (playerNo == 0)
                {
                    baseEndPoint = 580f;
                    reboundPointInAttack = 610f;
                    reboundPointInDefence = 190f;
                }
                else
                {
                    baseEndPoint = 400f;
                    reboundPointInAttack = 650f;
                    reboundPointInDefence = 120f;
                }
            }

            // 2. Set defensive stance point and default end position

            defensePoint = player.Side == -1 ? mlpObjectsData.DefensePoint : mlpConstants.Width - mlpObjectsData.DefensePoint;
            endPoint = baseEndPoint;
        }

        /// <summary>
        /// Lazy acquisition of ball and opponent references from the game core on first use.

        /// </summary>
        protected void EnsureRuntimeLinks()
        {
            if (initialized)
            {
                return;
            }

            ball = player.GameCore.Ball;
            opponents = player.Side == -1
                ? new List<mlpPlayerObject>(player.GameCore.PlayersRight)
                : new List<mlpPlayerObject>(player.GameCore.PlayersLeft);
            opponent = opponents.Count > 0 ? opponents[0] : null;
            initialized = true;
        }

        /// <summary>
        /// Reset all AI states (strategy, delays, inputs, feint counts) for the new round.

        /// </summary>
        protected void ResetForRestart()
        {
            // 1. Set the default strategy to "jump ball" (number 3) and reset the boring wait timer

            strategy = 3;
            deltaDownTime = 0f;
            // 2. Restore the default defensive end position

            endPoint = baseEndPoint;
            // 3. Reset all delay timers and current inputs

            ResetAllDelays();
            ResetCurrents();
            // 4. Clear the status of blocks, ultimate moves, queued ultimate moves, etc.

            CurrentBlockOrPump = false;
            CurrentSuper = false;
            queuedSuperInput = false;
            // 5. Clear the cheated status and count of fake actions

            isPumped = false;
            pumpCount = 0;
            // 6. Clear the queue rebound jump and immediate attack flags

            queuedReboundJump = false;
            willAttackAtOnce = false;
            // 7. The rebound position defaults to the defensive rebound position.

            reboundPoint = reboundPointInDefence;
            // 8. Clear steal avoidance status

            ResetAvoidSteal();
        }

        /// <summary>
        /// Zeroes out all controller input flags (move, jump, action, sprint).

        /// </summary>
        protected void ResetCurrents()
        {
            CurrentMove = 0;
            CurrentJump = false;
            CurrentAction = false;
            CurrentDash = 0;
        }

        /// <summary>
        /// Reset delay timers for offense, defense, movement, steals, and attack jumps.
        /// </summary>
        protected void ResetBaseDelays()
        {
            attackJumpDelay.Reset();
            attack.Reset();
            defenceDelay.Reset();
            moveDelay.Reset();
            stealDelay.Reset();
            ResetAvoidSteal();
        }

        /// <summary>
        /// Resets all delay timers, including sprint decision and super sprint timers.

        /// </summary>
        protected void ResetAllDelays()
        {
            dashDecisionDelay.Reset();
            superDashDelay.Reset();
            ResetBaseDelays();
        }

        /// <summary>
        /// Clear jump and move flags for steals and dodges.

        /// </summary>
        protected void ResetAvoidSteal()
        {
            avoidStealJump = false;
            avoidStealMove = 0;
        }

        /// <summary>
        /// Search the opponent list for the player with the specified number, or return null if not found.
        /// </summary>
        /// <param name="targetPlayerNo">Player number to search</param>
        /// <returns>Search results. </returns>
        protected mlpPlayerObject FindOpponentByPlayerNo(int targetPlayerNo)
        {
            if (opponents == null)
            {
                return null;
            }

            for (var i = 0; i < opponents.Count; i++)
            {
                if (opponents[i] != null && opponents[i].PlayerNo == targetPlayerNo)
                {
                    return opponents[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Select the X position of the shot, using distance-based randomization and clutch ball logic.
        /// </summary>
        /// <param name="point">Attack X position (0 means automatic selection)</param>

        /// <param name="jump">Jump point X position, used to determine when to take off and shoot</param>

        protected void SetAttackPoint(float point, float jump)
        {
            // 1. If the caller specifies a specific attack point (non-zero), use it directly

            if (!Mathf.Approximately(point, 0f))
            {
                attackPoint = point;
                jumpPoint = jump;
            }
            else
            {
                // 2. No attack point specified → Automatically select shooting position based on game situation and probability

                if (ShouldForceClutchThree())
                {
                    // 2a. The game is almost over and behind → Forced selection of the three-point line position

                    attackPoint = 500f + 24f * UnityEngine.Random.value;
                }
                else if (ShouldPreferSafeClutchTwo())
                {
                    // 2b. The game is almost over and you are ahead → Choose a safe close two-point position
                    attackPoint = 140f + 80f * UnityEngine.Random.value;
                }
                else if ((player.Position.x - 450f) * player.Side > 0f && UnityEngine.Random.value <= mlpObjectsData.ChanceForThree)
                {
                    // 2c. Already in the opponent's half and hit a random hit → try a three-pointer

                    attackPoint = 510f;
                }
                else if (UnityEngine.Random.value <= 0.7f)
                {
                    // 2d. Choose a mid-range shooting position with high probability

                    attackPoint = 120f + 200f * UnityEngine.Random.value;
                }
                else
                {
                    // 2e. There is a small probability of choosing a long-range mid-range shot position.

                    attackPoint = 320f + 160f * UnityEngine.Random.value;
                }

                // 3. Jump point: To shoot from close range, you need to jump closer to the basket before shooting.

                jumpPoint = attackPoint <= 200f ? attackPoint + 100f : attackPoint;
            }

            // 4. The left camp needs to mirror the X coordinate and flip it

            if (player.Side == -1)
            {
                attackPoint = mlpConstants.Width - attackPoint;
                jumpPoint = mlpConstants.Width - jumpPoint;
            }
        }

        /// <summary>
        /// Returns true on Hell difficulty to enable trajectory-based ball prediction.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldUseTechnicalPrediction()
        {
            return difficulty == mlpAiDifficulty.Hell;
        }

        /// <summary>
        /// Predicts the landing point of a free ball on Hell difficulty, otherwise returns the ball's current X coordinate.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetTechnicalLooseBallTargetX()
        {
            if (!ShouldUseTechnicalPrediction() || ball == null)
            {
                return ball != null ? ball.Position.x : player.Position.x;
            }

            if (ball.State == "bounce" || ball.State == "steal")
            {
                return ball.Position.x;
            }

            return ball.PredictFloorLandingX();
        }

        /// <summary>
        /// Predict the landing point of a missed shot on Hell difficulty, otherwise return to the fixed rebound position.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float GetTechnicalReboundTargetX()
        {
            if (!ShouldUseTechnicalPrediction() || ball == null)
            {
                return reboundPoint;
            }

            if (ball.State == "shooting" || ball.State == "basket" || ball.State == "block" || ball.State == "dunk" || ball.State == "alleyOop")
            {
                return ball.PredictFloorLandingX();
            }

            return reboundPoint;
        }

        /// <summary>
        /// Returns true when the AI ​​falls behind late in the game and should force a 3-pointer.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldForceClutchThree()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 12f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) <= -2;
        }

        /// <summary>
        /// Returns true when the AI ​​leads late in the game and should choose a safe two-point shot.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldPreferSafeClutchTwo()
        {
            return SupportsClutchShotSelection() &&
                   player.GameCore.RemainingMatchTime <= 15f &&
                   player.GameCore.GetScoreLeadForSide(player.Side) >= 3;
        }

        /// <summary>
        /// Returns true on Hard or Hell difficulty to enable late shot selection logic.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool SupportsClutchShotSelection()
        {
            return difficulty == mlpAiDifficulty.Hard || difficulty == mlpAiDifficulty.Hell;
        }

        /// <summary>
        /// On Hell difficulty, returns true when an opponent is close and takes a threatening shot.
        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldUsePerfectContestOnJump()
        {
            return difficulty == mlpAiDifficulty.Hell &&
                   IsOpponentImmediateShotThreat() &&
                   IsOpponentCloseAbs(GetDefenceContestDistance() + 24f);
        }

        /// <summary>
        /// On Hell difficulty, returns true when the opponent is unlikely to actually shoot.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool ShouldIgnorePumpFake()
        {
            if (difficulty != mlpAiDifficulty.Hell)
            {
                return false;
            }

            if (pumpCount > 1)
            {
                return false;
            }

            return !IsOpponentImmediateShotThreat();
        }

        /// <summary>
        /// Returns true when the opponent is close to their attack target or when the match time is about to end.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsOpponentImmediateShotThreat()
        {
            if (opponent == null || !opponent.WithBall)
            {
                return false;
            }

            var distanceToTargetBasket = Mathf.Abs(opponent.Position.x - opponent.AttackTargetX);
            if (distanceToTargetBasket <= 220f)
            {
                return true;
            }

            return player.GameCore.RemainingMatchTime <= 8f &&
                   player.GameCore.GetScoreLeadForSide(opponent.Side) <= 0;
        }

        /// <summary>
        /// Returns -1, 0, or 1, indicating whether the player is before, inside, or behind the attacking zone.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected int IsReboundInAttackZone()
        {
            var playerX = player.Position.x;
            var zone = 0;
            if ((playerX - attackZoneStart) * player.Side <= 0f)
            {
                zone = -1;
            }
            else if ((playerX - attackZoneEnd) * player.Side >= 0f)
            {
                zone = 1;
            }

            return zone;
        }

        /// <summary>
        /// Returns -1, 0, or 1 indicating the direction of movement toward the given X position.

        /// </summary>
        /// <param name="x">Horizontal coordinates in pixel space</param>

        /// <returns>Calculation results. </returns>
        protected int MoveTo(float x)
        {
            var delta = player.Position.x - x;
            return Mathf.Abs(delta) <= DeltaDistance ? 0 : delta > 0f ? -1 : 1;
        }

        /// <summary>
        /// Decide to move towards the jump point or attack point, and set the attack jump flag.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected int MoveInAttack()
        {
            // 1. First try to move to the jump point

            var move = MoveTo(jumpPoint);
            if (move == 0)
            {
                // 2. The jumping point has been reached → Mark "You can take off and shoot", and then determine whether you need to continue walking towards the attack point.

                attackJump = true;
                move = Mathf.Approximately(jumpPoint, attackPoint) ? 0 : MoveTo(attackPoint);
            }
            else
            {
                // 3. Not yet at the jumping point → Mark "Don't jump" and move towards the attack point (first go in the general direction)

                attackJump = false;
                move = MoveTo(attackPoint);
            }

            return move;
        }

        /// <summary>
        /// Returns the horizontal distance from the player to the ball (positive values mean the ball is to the left of the player).

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float DeltaBallX()
        {
            return player.Position.x - ball.Position.x;
        }

        /// <summary>
        /// Returns the vertical distance from the player to the ball.

        /// </summary>
        /// <returns>Calculation results. </returns>
        protected float DeltaBallY()
        {
            return player.Position.y - ball.Position.y;
        }

        /// <summary>
        /// Returns true when the ball is close horizontally but far vertically, indicating a rebound opportunity.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsBallInReboundZone()
        {
            return Mathf.Abs(DeltaBallX()) < 60f && Mathf.Abs(DeltaBallY()) > 70f;
        }

        /// <summary>
        /// Returns true when the opponent is within the specified distance behind the player.

        /// </summary>
        /// <param name="distance">Estimated distance to target basket</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsOpponentCloseBehind(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta > 0f && delta <= distance;
        }

        /// <summary>
        /// Returns true when the opponent is behind the player and within the min-max distance range.

        /// </summary>
        /// <param name="min">Minimum distance threshold</param>

        /// <param name="max">Maximum distance threshold</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsOpponentInRangeBehind(float min = 40f, float max = 180f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta > 0f && delta >= min && delta <= max;
        }

        /// <summary>
        /// Returns true when an opponent is within the specified distance between the player and the basket.

        /// </summary>
        /// <param name="distance">Estimated distance to target basket</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsOpponentCloseToBasket(float distance = 30f)
        {
            if (opponent == null)
            {
                return false;
            }

            var delta = (player.Position.x - opponent.Position.x) * player.Side;
            return delta < 0f && delta + distance >= 0f;
        }

        /// <summary>
        /// Returns true when the AI ​​player is closer to the opponent's basket than the opponent.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsAICloserForBasket()
        {
            if (opponent == null)
            {
                return false;
            }

            return (player.Position.x - opponent.Position.x) * player.Side < 0f;
        }

        /// <summary>
        /// Returns true when the player is standing under his own basket.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsUnderOwnBasket()
        {
            return player.Side == 1 ? player.Position.x > 700f : player.Position.x < 100f;
        }

        /// <summary>
        /// Returns true when the player's X position is within the configured sprint area.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool InDashingZone()
        {
            return player.Position.x >= dashZoneStart && player.Position.x <= dashZoneEnd;
        }

        /// <summary>
        /// Returns true when the absolute horizontal distance to the opponent is within the threshold.

        /// </summary>
        /// <param name="distance">Estimated distance to target basket</param>

        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsOpponentCloseAbs(float distance = 100f)
        {
            if (opponent == null)
            {
                return false;
            }

            return Mathf.Abs(player.Position.x - opponent.Position.x) <= distance;
        }

        /// <summary>
        /// Returns true when the player is in the opponent's half of the field.

        /// </summary>
        /// <returns>Returns true if the operation is successful; otherwise returns false. </returns>
        protected bool IsInAttackZone()
        {
            return player.Side == 1 ? player.Position.x < 600f : player.Position.x > 200f;
        }
    }
}
