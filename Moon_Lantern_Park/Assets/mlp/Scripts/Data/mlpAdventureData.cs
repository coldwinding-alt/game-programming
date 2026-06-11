// 冒险模式关卡数据
// 定义冒险模式的 8 个关卡，每关有不同的对手、场景和特殊规则。玩家需要一关一关打通，收集灯笼印记来解锁最终逃脱路线。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 冒险模式特殊规则类型：每关有不同的规则效果，比如糖果充能、双倍篮筐、迷雾风、血月等。
    /// </summary>
    public enum mlpAdventureMechanic
    {
        /// <summary>基础对决：纯 1v1 投篮比赛，无任何特殊规则，是第一关的教学关卡。</summary>
        BasicDuel,
        /// <summary>糖果充能：玩家的超级计量槽（super meter）充能速度加快，更容易释放必杀技。</summary>
        CandyCharge,
        /// <summary>双倍篮筐：场上出现活动窗口（active windows），在此期间进球双倍计分。</summary>
        DoubleHoop,
        /// <summary>烛光圆环：双方超级计量槽都会自动充能，但玩家的充能速度更快，形成节奏博弈。</summary>
        CandleCircle,
        /// <summary>迷雾风：飞行中的篮球会被风横向吹偏一小段距离，考验玩家的投篮预判和修正能力。</summary>
        FogWind,
        /// <summary>血月：整场比赛节奏加快（时间流速 ×1.14），增加反应压力。</summary>
        BloodMoon,
        /// <summary>丰收时刻：比赛最后 15 秒进入丰收状态，每个进球额外 +1 分，鼓励终场绝杀。</summary>
        HarvestTime,
        /// <summary>月灯混合：最终 Boss 关，每 10 秒自动轮换上述所有机制（充能→双倍→风→加速→循环），考验全面能力。</summary>
        MoonLanternMix
    }

    /// <summary>
    /// 冒险模式单关定义：一个不可变的数据容器，描述一个关卡的全部信息。
    /// 游戏的各个系统（场景加载、AI 配置、地图渲染、UI 显示）都会从这里各取所需。
    /// </summary>
    public sealed class mlpAdventureLevelDefinition
    {
        /// <summary>关卡索引（从 0 开始），用于标识关卡顺序和进度判断。</summary>
        public readonly int Index;
        /// <summary>关卡区域的显示名称，如 "PUMPKIN GATEWAY"，展示在 UI 和地图上。</summary>
        public readonly string AreaName;
        /// <summary>守护该关卡的守卫者角色 ID，决定对手使用哪个角色模型和技能。</summary>
        public readonly int WardenCharacterId;
        /// <summary>关卡的简短氛围描述，供美术和音效参考，如 "First gate."、"Red pressure."。</summary>
        public readonly string Mood;
        /// <summary>该关卡使用的独特玩法机制，决定比赛中的特殊规则。</summary>
        public readonly mlpAdventureMechanic Mechanic;
        /// <summary>玩法机制的显示标题，展示在规则覆盖层上，如 "CANDY CHARGE"、"BLOOD MOON"。</summary>
        public readonly string MechanicTitle;
        /// <summary>玩法机制的简要说明文字，帮助玩家理解当前关卡的特殊规则。</summary>
        public readonly string MechanicSummary;
        /// <summary>场景的美术指导说明，描述关卡的视觉风格和场景元素。</summary>
        public readonly string SceneDirection;
        /// <summary>规则覆盖层上显示的图标键名数组，如 { "1V1", "SIGIL", "GATE" }。</summary>
        public readonly string[] RuleIcons;
        /// <summary>在冒险地图上的 X 坐标，用于地图界面定位关卡节点。</summary>
        public readonly float MapX;
        /// <summary>在冒险地图上的 Y 坐标，用于地图界面定位关卡节点。</summary>
        public readonly float MapY;
        /// <summary>该关卡使用的篮球皮肤，决定比赛中篮球的外观。</summary>
        public readonly mlpBallSelection BallSelection;
        // 旧版冒险模式曾用这个字段表示每一关的基础 AI 技能值。
        // 当前固定四档难度模式下，实际比赛强度只看玩家选择的 Easy/Normal/Hard/Hell，
        // 所以这个字段暂时不直接参与 AI 技能计算；继续保留它是为了不改动关卡数据结构和旧数据。
        public readonly int OpponentSkill;
        public readonly string VictoryBeat;
        public readonly string[] VictoryLines;
        public readonly string[] DefeatLines;

        /// <summary>
        /// 创建冒险关卡定义，包含所有玩法、叙事和地图数据。
        /// </summary>
        /// <param name="index">从零开始的关卡索引。</param>
        /// <param name="areaName">关卡区域的显示名称。</param>
        /// <param name="wardenCharacterId">守护该关卡的守卫者角色 ID。</param>
        /// <param name="mood">关卡的简短氛围描述。</param>
        /// <param name="mechanic">该关卡使用的独特玩法机制。</param>
        /// <param name="mechanicTitle">玩法机制的显示标题。</param>
        /// <param name="mechanicSummary">玩法机制的简要说明。</param>
        /// <param name="sceneDirection">场景的美术指导说明。</param>
        /// <param name="ruleIcons">规则覆盖层上显示的图标键名。</param>
        /// <param name="mapX">在冒险地图上的 X 坐标。</param>
        /// <param name="mapY">在冒险地图上的 Y 坐标。</param>
        /// <param name="ballSelection">该关卡使用的篮球皮肤。</param>
        /// <param name="opponentSkill">旧版关卡基础 AI 技能值；当前固定四档难度模式下不直接参与比赛强度计算。</param>
        /// <param name="victoryLines">玩家获胜时显示的对话台词。</param>
        /// <param name="defeatLines">玩家失败时显示的对话台词。</param>
        public mlpAdventureLevelDefinition(
            int index,
            string areaName,
            int wardenCharacterId,
            string mood,
            mlpAdventureMechanic mechanic,
            string mechanicTitle,
            string mechanicSummary,
            string sceneDirection,
            string[] ruleIcons,
            float mapX,
            float mapY,
            mlpBallSelection ballSelection,
            int opponentSkill,
            string[] victoryLines,
            string[] defeatLines)
        {
            // 1. 设置关卡基本信息（索引、区域名、守卫者、氛围）
            Index = index;
            AreaName = areaName;
            WardenCharacterId = wardenCharacterId;
            Mood = mood;
            // 2. 设置玩法机制信息（类型、标题、说明、场景指导）
            Mechanic = mechanic;
            MechanicTitle = mechanicTitle;
            MechanicSummary = mechanicSummary;
            SceneDirection = sceneDirection;
            // 3. 设置规则图标（null 时用空数组代替）
            RuleIcons = ruleIcons ?? new string[0];
            // 4. 设置地图坐标和球皮
            MapX = mapX;
            MapY = mapY;
            BallSelection = ballSelection;
            // 5. 设置对手技能等级
            OpponentSkill = opponentSkill;
            // 6. 设置胜利和失败台词（null 时用空数组代替）
            VictoryLines = victoryLines ?? new string[0];
            DefeatLines = defeatLines ?? new string[0];
            // 7. 提取第一条胜利台词作为默认展示文本
            VictoryBeat = VictoryLines.Length > 0 ? VictoryLines[0] : string.Empty;
        }

        /// <summary>
        /// 从胜利或失败的台词池中随机选取一条对话。
        /// </summary>
        /// <param name="playerWon">传 true 选取胜利台词，传 false 选取失败台词。</param>
        /// <returns>随机选取的结果台词，如果台词池为空则返回默认文本。</returns>
        public string GetRandomResultLine(bool playerWon)
        {
            // 1. 根据胜负结果选择对应的台词池（胜利台词或失败台词）
            var pool = playerWon ? VictoryLines : DefeatLines;
            // 2. 如果台词池不为空，随机选一条返回
            if (pool != null && pool.Length > 0)
            {
                return pool[Random.Range(0, pool.Length)];
            }

            // 3. 如果台词池为空，返回默认的备用台词
            return playerWon
                ? "Take the Lantern Sigil and keep moving."
                : "The Lantern Sigil stays out of reach.";
        }
    }

    /// <summary>
    /// 冒险模式关卡目录：存储所有 8 个关卡的定义数据，供游戏读取和使用。
    /// </summary>
    public static class mlpAdventureCatalog
    {
        private static readonly mlpAdventureLevelDefinition[] Levels =
        {
            new mlpAdventureLevelDefinition(
                0,
                "PUMPKIN GATEWAY",
                5,
                "First gate.",
                mlpAdventureMechanic.BasicDuel,
                "WARDEN DUEL",
                "Pure 1v1. Win the baseline duel.",
                "Pumpkin gate and court lights.",
                new[] { "1V1", "SIGIL", "GATE" },
                96f,
                356f,
                mlpBallSelection.PumpkinEmber,
                1,
                new[]
                {
                    "You got me. Take the first Sigil and follow the lantern trail.",
                    "Heh, lucky hands. The Pumpkin Gate will answer that Sigil.",
                    "The park picked you tonight. Go on before the fog closes in."
                },
                new[]
                {
                    "Too soft. The Pumpkin Gate stays shut.",
                    "You want out of Moon Lantern Park? Win the court first.",
                    "Come back with steadier hands, challenger."
                }),
            new mlpAdventureLevelDefinition(
                1,
                "CANDY ARCH STREET",
                7,
                "Fast lane.",
                mlpAdventureMechanic.CandyCharge,
                "CANDY CHARGE",
                "Your super meter charges faster.",
                "Candy arches and fast lanes.",
                new[] { "CANDY", "CHARGE", "BOOST" },
                168f,
                296f,
                mlpBallSelection.CandySwirl,
                2,
                new[]
                {
                    "Fast feet. Fine, the street Sigil is yours.",
                    "You kept up. Take the Sigil before the candy lights fade.",
                    "Not bad. The lockdown route just opened one more turn."
                },
                new[]
                {
                    "Too slow. Candy Arch eats late runners.",
                    "You chased me and lost the lane.",
                    "No Sigil for you. Try again when you can keep the pace."
                }),
            new mlpAdventureLevelDefinition(
                2,
                "LAUGHING MIRROR HOUSE",
                1,
                "Trick court.",
                mlpAdventureMechanic.DoubleHoop,
                "DOUBLE RIM",
                "Active windows make every basket double.",
                "Mirror panels and second-rim lights.",
                new[] { "RIM X2", "TIMED", "RUSH" },
                148f,
                220f,
                mlpBallSelection.EvilEye,
                3,
                new[]
                {
                    "Ha! You beat the joke. Take the mirror Sigil.",
                    "Well played. Even the glass says that round was yours.",
                    "The Heart Lantern saw that shot. Go claim the next court."
                },
                new[]
                {
                    "The mirrors laughed before I did.",
                    "You bit on every trick. Come back sharper.",
                    "No encore yet. The mirror house keeps its Sigil."
                }),
            new mlpAdventureLevelDefinition(
                3,
                "CANDLE HALL",
                4,
                "Ritual hall.",
                mlpAdventureMechanic.CandleCircle,
                "CANDLE RING",
                "Both supers auto-charge; yours is faster.",
                "Candle rings and gold smoke.",
                new[] { "CANDLE", "CHARGE", "TEMPO" },
                246f,
                184f,
                mlpBallSelection.JackOLantern,
                4,
                new[]
                {
                    "The flames bend for winners. Take the candle Sigil.",
                    "You stayed calm in the glow. The route ahead is yours.",
                    "A steady hand earns steady fire. Go while the wax still burns."
                },
                new[]
                {
                    "Your rhythm broke before the candles did.",
                    "The hall stays dim until you prove your nerve.",
                    "Too restless. Come back when your game stops flickering."
                }),
            new mlpAdventureLevelDefinition(
                4,
                "FOG DOCK",
                2,
                "Mist drift.",
                mlpAdventureMechanic.FogWind,
                "FOG WIND",
                "Airborne balls drift lightly sideways.",
                "Dock planks and lantern buoys.",
                new[] { "WIND", "BALL", "DRIFT" },
                340f,
                210f,
                mlpBallSelection.GhoulGreen,
                5,
                new[]
                {
                    "A clean raid. Take the dock Sigil and sail on.",
                    "You stole that one fair and square. The mist parts for you.",
                    "Ha! Even the tide liked that finish. Keep moving."
                },
                new[]
                {
                    "Blown off course. The dock keeps its Sigil.",
                    "The mist fooled your aim and I took the rest.",
                    "You'll need sturdier sea legs than that, challenger."
                }),
            new mlpAdventureLevelDefinition(
                5,
                "BLOOD MOON TERRACE",
                3,
                "Red pressure.",
                mlpAdventureMechanic.BloodMoon,
                "BLOOD MOON",
                "The whole match plays slightly faster.",
                "Red moonlight and long shadows.",
                new[] { "MOON", "SPEED", "PULSE" },
                432f,
                226f,
                mlpBallSelection.MoonlitViolet,
                6,
                new[]
                {
                    "Impressive. Take the terrace Sigil before the red moon turns.",
                    "You survived the pressure. The Heart Lantern will remember it.",
                    "Very well. The night yields one more key to you."
                },
                new[]
                {
                    "The red moon thinned your courage.",
                    "You rushed the dark and the dark answered.",
                    "Not enough bite. The terrace stays mine."
                }),
            new mlpAdventureLevelDefinition(
                6,
                "CLOCKTOWER GRAVEYARD",
                0,
                "Final-minute clutch.",
                mlpAdventureMechanic.HarvestTime,
                "HARVEST TIME",
                "Last 15 seconds: every basket is +1.",
                "Clock hands and low fog.",
                new[] { "LAST 15", "+1", "CLUTCH" },
                500f,
                296f,
                mlpBallSelection.Cursed8Ball,
                7,
                new[]
                {
                    "You lived through the closing seconds. Take the graveyard Sigil.",
                    "Clutch enough. The last path to the dome is open.",
                    "The clock spared you tonight. Go claim the final court."
                },
                new[]
                {
                    "When the clock tightened, so did your game.",
                    "The graveyard keeps the next gate shut.",
                    "You blinked at the harvest bell. Try the last seconds again."
                }),
            new mlpAdventureLevelDefinition(
                7,
                "MOON LANTERN DOME",
                6,
                "Final dome.",
                mlpAdventureMechanic.MoonLanternMix,
                "MOON MIX",
                "Every 10 seconds rotates charge, double, wind, speed.",
                "Heart Lantern and center court.",
                new[] { "MIX", "FINAL", "DOME" },
                548f,
                188f,
                mlpBallSelection.Random,
                8,
                new[]
                {
                    "Then the park chooses you. Take the final Sigil and open the gates.",
                    "The Heart Lantern heard that win. Go now, before dawn slips away.",
                    "You earned the last light. Leave this dome before it changes its mind."
                },
                new[]
                {
                    "So close. The dome still answers to me.",
                    "The final light will not wake for a half-finished run.",
                    "No gate opens tonight. Return when you can finish the ritual."
                })
        };

        /// <summary>
        /// 获取目录中定义的冒险关卡总数。
        /// </summary>
        public static int LevelCount => Levels.Length;

        /// <summary>
        /// 获取所有冒险关卡定义的数组。
        /// </summary>
        public static mlpAdventureLevelDefinition[] AllLevels => Levels;

        /// <summary>
        /// 获取指定索引处的冒险关卡定义，索引会被限制在有效范围内。
        /// </summary>
        /// <param name="index">要查找的关卡索引。</param>
        /// <returns>对应索引的关卡定义。</returns>
        public static mlpAdventureLevelDefinition GetLevel(int index)
        {
            return Levels[Mathf.Clamp(index, 0, Levels.Length - 1)];
        }
    }

    /// <summary>
    /// 冒险模式进度数据：管理玩家在冒险模式中的当前关卡、已通关状态和灯笼印记收集情况。
    /// </summary>
    public sealed class mlpAdventureData
    {
        private readonly bool[] levelCompleted = new bool[mlpAdventureCatalog.LevelCount];

        /// <summary>冒险模式是否正在进行中。</summary>
        public bool Active { get; private set; }
        /// <summary>玩家是否已完成所有冒险关卡。</summary>
        public bool Completed { get; private set; }
        /// <summary>玩家在本次冒险中选择的角色 ID。</summary>
        public int PlayerCharacterId { get; private set; }
        /// <summary>玩家当前所在或正在选择的关卡索引。</summary>
        public int CurrentLevelIndex { get; private set; }
        /// <summary>玩家目前已解锁的最高关卡索引。</summary>
        public int HighestUnlockedLevelIndex { get; private set; }
        /// <summary>最近一次已结算比赛结果的关卡索引，如果没有则为 -1。</summary>
        public int LastResolvedLevelIndex { get; private set; } = -1;
        /// <summary>玩家是否赢得了最近一次已结算的比赛。</summary>
        public bool LastPlayerWon { get; private set; }
        /// <summary>
        /// 获取玩家已收集的灯笼印记数量。
        /// </summary>
        public int SigilsCollected
        {
            get
            {
                var count = 0;
                for (var i = 0; i < levelCompleted.Length; i++)
                {
                    if (levelCompleted[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 判断是否存在正在进行、尚未完成且当前关卡有效的冒险，可以开始比赛。
        /// </summary>
        public bool HasPendingPlayerMatch => Active && !Completed && CurrentLevelIndex >= 0 && CurrentLevelIndex < mlpAdventureCatalog.LevelCount;
        /// <summary>
        /// 获取当前关卡索引对应的关卡定义。
        /// </summary>
        public mlpAdventureLevelDefinition CurrentLevel => mlpAdventureCatalog.GetLevel(CurrentLevelIndex);

        /// <summary>
        /// 重置所有冒险进度，将冒险状态恢复为未激活。
        /// </summary>
        public void Reset()
        {
            Active = false;
            Completed = false;
            PlayerCharacterId = 0;
            CurrentLevelIndex = 0;
            HighestUnlockedLevelIndex = 0;
            LastResolvedLevelIndex = -1;
            LastPlayerWon = false;
            for (var i = 0; i < levelCompleted.Length; i++)
            {
                levelCompleted[i] = false;
            }
        }

        /// <summary>
        /// 使用指定的玩家角色开始一次新的冒险。
        /// </summary>
        /// <param name="playerCharacterId">本次冒险使用的角色 ID。</param>
        public void Create(int playerCharacterId)
        {
            // 1. 先清空所有之前的冒险进度
            Reset();
            // 2. 标记冒险模式为"进行中"
            Active = true;
            // 3. 验证并记录玩家选择的角色编号（如果无效则自动修正为可用角色）
            PlayerCharacterId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
        }

        /// <summary>
        /// 尝试选择指定索引的关卡作为当前关卡。如果冒险未激活、已完成或该关卡未解锁，则选择失败。
        /// </summary>
        /// <param name="levelIndex">要选择的关卡索引。</param>
        /// <returns>关卡选择成功返回 true，否则返回 false。</returns>
        public bool SelectLevel(int levelIndex)
        {
            // 1. 检查冒险是否正在进行、未完成、且该关卡已解锁
            if (!Active || Completed || !IsLevelUnlocked(levelIndex))
            {
                return false;
            }

            // 2. 将关卡索引限制在有效范围内，并设置为当前关卡
            CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, mlpAdventureCatalog.LevelCount - 1);
            return true;
        }

        /// <summary>
        /// 检查指定索引的关卡是否已被玩家解锁。
        /// </summary>
        /// <param name="levelIndex">要检查的关卡索引。</param>
        /// <returns>如果关卡在已解锁范围内返回 true，否则返回 false。</returns>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedLevelIndex && levelIndex < mlpAdventureCatalog.LevelCount;
        }

        /// <summary>
        /// 检查指定索引的关卡是否已被玩家完成。
        /// </summary>
        /// <param name="levelIndex">要检查的关卡索引。</param>
        /// <returns>如果关卡已完成返回 true，否则返回 false。</returns>
        public bool IsLevelCompleted(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < levelCompleted.Length && levelCompleted[levelIndex];
        }

        /// <summary>
        /// 结算当前关卡的比赛结果。如果玩家获胜，将该关卡标记为已完成并解锁下一关；如果失败，仅记录结果，不推进进度。
        /// </summary>
        /// <param name="playerWon">玩家赢得比赛则传 true。</param>
        public void ApplyCurrentMatchResult(bool playerWon)
        {
            // 1. 检查冒险是否有效（正在进行、未完成、当前关卡索引合法）
            if (!Active || Completed || CurrentLevelIndex < 0 || CurrentLevelIndex >= levelCompleted.Length)
            {
                return;
            }

            // 2. 记录最近一次比赛的结果（哪一关、赢了没有）
            LastResolvedLevelIndex = CurrentLevelIndex;
            LastPlayerWon = playerWon;
            // 3. 如果玩家输了，只记录结果，不推进冒险进度
            if (!playerWon)
            {
                return;
            }

            // 4. 玩家赢了：把当前关卡标记为"已完成"
            levelCompleted[CurrentLevelIndex] = true;
            // 5. 如果已经是最后一关，标记整个冒险为"已完成"
            if (CurrentLevelIndex >= mlpAdventureCatalog.LevelCount - 1)
            {
                Completed = true;
                return;
            }

            // 6. 解锁下一关，并把当前关卡推进到下一关
            HighestUnlockedLevelIndex = Mathf.Max(HighestUnlockedLevelIndex, CurrentLevelIndex + 1);
            CurrentLevelIndex = Mathf.Max(CurrentLevelIndex + 1, HighestUnlockedLevelIndex);
        }
    }
}
