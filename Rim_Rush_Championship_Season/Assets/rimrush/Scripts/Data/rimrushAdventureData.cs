// 冒险模式关卡数据
// 定义冒险模式的 8 个关卡，每关有不同的对手、场景和特殊规则。玩家需要一关一关打通，收集灯笼印记来解锁最终逃脱路线。

using UnityEngine;

namespace rimrush
{
    public enum rimrushAdventureMechanic
    {
        BasicDuel,
        CandyCharge,
        DoubleHoop,
        CandleCircle,
        FogWind,
        BloodMoon,
        HarvestTime,
        MoonLanternMix
    }

    public sealed class rimrushAdventureLevelDefinition
    {
        public readonly int Index;
        public readonly string AreaName;
        public readonly int WardenCharacterId;
        public readonly string Mood;
        public readonly rimrushAdventureMechanic Mechanic;
        public readonly string MechanicTitle;
        public readonly string MechanicSummary;
        public readonly string SceneDirection;
        public readonly string[] RuleIcons;
        public readonly float MapX;
        public readonly float MapY;
        public readonly rimrushBallSelection BallSelection;
        public readonly int OpponentSkill;
        public readonly string VictoryBeat;
        public readonly string[] VictoryLines;
        public readonly string[] DefeatLines;

        /// <summary>
        /// Creates a new adventure level definition with all gameplay, narrative, and map data.
        /// </summary>
        /// <param name="index">Zero-based level index.</param>
        /// <param name="areaName">Display name of the level area.</param>
        /// <param name="wardenCharacterId">Character ID of the Warden guarding this level.</param>
        /// <param name="mood">Short mood description for the level.</param>
        /// <param name="mechanic">The unique gameplay mechanic used in this level.</param>
        /// <param name="mechanicTitle">Display title for the mechanic.</param>
        /// <param name="mechanicSummary">Short summary of how the mechanic works.</param>
        /// <param name="sceneDirection">Art direction notes for the scene.</param>
        /// <param name="ruleIcons">Icon keys shown on the rule overlay.</param>
        /// <param name="mapX">X position on the adventure map.</param>
        /// <param name="mapY">Y position on the adventure map.</param>
        /// <param name="ballSelection">Ball skin used for this level.</param>
        /// <param name="opponentSkill">AI difficulty tier of the opponent.</param>
        /// <param name="victoryLines">Dialogue lines shown when the player wins.</param>
        /// <param name="defeatLines">Dialogue lines shown when the player loses.</param>
        public rimrushAdventureLevelDefinition(
            int index,
            string areaName,
            int wardenCharacterId,
            string mood,
            rimrushAdventureMechanic mechanic,
            string mechanicTitle,
            string mechanicSummary,
            string sceneDirection,
            string[] ruleIcons,
            float mapX,
            float mapY,
            rimrushBallSelection ballSelection,
            int opponentSkill,
            string[] victoryLines,
            string[] defeatLines)
        {
            Index = index;
            AreaName = areaName;
            WardenCharacterId = wardenCharacterId;
            Mood = mood;
            Mechanic = mechanic;
            MechanicTitle = mechanicTitle;
            MechanicSummary = mechanicSummary;
            SceneDirection = sceneDirection;
            RuleIcons = ruleIcons ?? new string[0];
            MapX = mapX;
            MapY = mapY;
            BallSelection = ballSelection;
            OpponentSkill = opponentSkill;
            VictoryLines = victoryLines ?? new string[0];
            DefeatLines = defeatLines ?? new string[0];
            VictoryBeat = VictoryLines.Length > 0 ? VictoryLines[0] : string.Empty;
        }

        /// <summary>
        /// Returns a random dialogue line from the victory or defeat pool.
        /// </summary>
        /// <param name="playerWon">True to pick from victory lines, false for defeat lines.</param>
        /// <returns>A random result line, or a default fallback if the pool is empty.</returns>
        public string GetRandomResultLine(bool playerWon)
        {
            var pool = playerWon ? VictoryLines : DefeatLines;
            if (pool != null && pool.Length > 0)
            {
                return pool[Random.Range(0, pool.Length)];
            }

            return playerWon
                ? "Take the Lantern Sigil and keep moving."
                : "The Lantern Sigil stays out of reach.";
        }
    }

    public static class rimrushAdventureCatalog
    {
        private static readonly rimrushAdventureLevelDefinition[] Levels =
        {
            new rimrushAdventureLevelDefinition(
                0,
                "PUMPKIN GATEWAY",
                5,
                "First gate.",
                rimrushAdventureMechanic.BasicDuel,
                "BASIC DUEL",
                "Pure 1v1. Take the Sigil.",
                "Pumpkin gate and court lights.",
                new[] { "1V1", "SIGIL", "GATE" },
                96f,
                356f,
                rimrushBallSelection.PumpkinEmber,
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
            new rimrushAdventureLevelDefinition(
                1,
                "CANDY ARCH STREET",
                7,
                "Fast lane.",
                rimrushAdventureMechanic.CandyCharge,
                "CANDY CHARGE",
                "Super meter fills faster.",
                "Candy arches and fast lanes.",
                new[] { "CANDY", "CHARGE", "BOOST" },
                168f,
                296f,
                rimrushBallSelection.CandySwirl,
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
            new rimrushAdventureLevelDefinition(
                2,
                "LAUGHING MIRROR HOUSE",
                1,
                "Trick court.",
                rimrushAdventureMechanic.DoubleHoop,
                "DOUBLE RIM",
                "Lit mirrors double made baskets.",
                "Mirror panels and second-rim lights.",
                new[] { "RIM X2", "TIMED", "RUSH" },
                148f,
                220f,
                rimrushBallSelection.EvilEye,
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
            new rimrushAdventureLevelDefinition(
                3,
                "CANDLE HALL",
                4,
                "Ritual hall.",
                rimrushAdventureMechanic.CandleCircle,
                "CANDLE RING",
                "Both meters slowly warm up.",
                "Candle rings and gold smoke.",
                new[] { "CANDLE", "CHARGE", "TEMPO" },
                246f,
                184f,
                rimrushBallSelection.JackOLantern,
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
            new rimrushAdventureLevelDefinition(
                4,
                "FOG DOCK",
                2,
                "Mist drift.",
                rimrushAdventureMechanic.FogWind,
                "FOG WIND",
                "Airborne balls drift sideways.",
                "Dock planks and lantern buoys.",
                new[] { "WIND", "BALL", "DRIFT" },
                340f,
                210f,
                rimrushBallSelection.GhoulGreen,
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
            new rimrushAdventureLevelDefinition(
                5,
                "BLOOD MOON TERRACE",
                3,
                "Red pressure.",
                rimrushAdventureMechanic.BloodMoon,
                "BLOOD MOON",
                "Moon pulses speed up the match.",
                "Red moonlight and long shadows.",
                new[] { "MOON", "SPEED", "PULSE" },
                432f,
                226f,
                rimrushBallSelection.MoonlitViolet,
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
            new rimrushAdventureLevelDefinition(
                6,
                "CLOCKTOWER GRAVEYARD",
                0,
                "Final-minute clutch.",
                rimrushAdventureMechanic.HarvestTime,
                "HARVEST TIME",
                "Last 15 seconds: baskets are +1.",
                "Clock hands and low fog.",
                new[] { "LAST 15", "+1", "CLUTCH" },
                500f,
                296f,
                rimrushBallSelection.Cursed8Ball,
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
            new rimrushAdventureLevelDefinition(
                7,
                "MOON LANTERN DOME",
                6,
                "Final dome.",
                rimrushAdventureMechanic.MoonLanternMix,
                "MOON MIX",
                "Rules rotate until the dome opens.",
                "Heart Lantern and center court.",
                new[] { "MIX", "FINAL", "DOME" },
                548f,
                188f,
                rimrushBallSelection.Random,
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
        /// Returns the total number of adventure levels defined in the catalog.
        /// </summary>
        public static int LevelCount => Levels.Length;

        /// <summary>
        /// Returns all adventure level definitions as an array.
        /// </summary>
        public static rimrushAdventureLevelDefinition[] AllLevels => Levels;

        /// <summary>
        /// Returns the adventure level definition at the given index, clamped to valid bounds.
        /// </summary>
        /// <param name="index">The level index to look up.</param>
        /// <returns>The level definition for the requested index.</returns>
        public static rimrushAdventureLevelDefinition GetLevel(int index)
        {
            return Levels[Mathf.Clamp(index, 0, Levels.Length - 1)];
        }
    }

    public sealed class rimrushAdventureData
    {
        private readonly bool[] levelCompleted = new bool[rimrushAdventureCatalog.LevelCount];

        /// <summary>Whether an adventure run is currently in progress.</summary>
        public bool Active { get; private set; }
        /// <summary>Whether the player has completed all adventure levels.</summary>
        public bool Completed { get; private set; }
        /// <summary>The character ID chosen by the player for this adventure run.</summary>
        public int PlayerCharacterId { get; private set; }
        /// <summary>The index of the level the player is currently on or selecting.</summary>
        public int CurrentLevelIndex { get; private set; }
        /// <summary>The highest level index the player has unlocked so far.</summary>
        public int HighestUnlockedLevelIndex { get; private set; }
        /// <summary>The index of the last level whose match result was resolved, or -1 if none.</summary>
        public int LastResolvedLevelIndex { get; private set; } = -1;
        /// <summary>Whether the player won the most recently resolved match.</summary>
        public bool LastPlayerWon { get; private set; }
        /// <summary>
        /// Returns the number of Lantern Sigils the player has collected.
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
        /// Returns true if there is an active, incomplete adventure with a valid current level ready for a match.
        /// </summary>
        public bool HasPendingPlayerMatch => Active && !Completed && CurrentLevelIndex >= 0 && CurrentLevelIndex < rimrushAdventureCatalog.LevelCount;
        /// <summary>
        /// Returns the level definition for the current level index.
        /// </summary>
        public rimrushAdventureLevelDefinition CurrentLevel => rimrushAdventureCatalog.GetLevel(CurrentLevelIndex);

        /// <summary>
        /// Resets all adventure progress, returning the run to an inactive state.
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
        /// Starts a new adventure run with the given player character.
        /// </summary>
        /// <param name="playerCharacterId">The character ID to use for this run.</param>
        public void Create(int playerCharacterId)
        {
            Reset();
            Active = true;
            PlayerCharacterId = rimrushPlayersData.SanitizeCharacterId(playerCharacterId);
        }

        /// <summary>
        /// Attempts to select the given level index as the current level.
        /// Fails if the adventure is not active, is completed, or the level is not unlocked.
        /// </summary>
        /// <param name="levelIndex">The level index to select.</param>
        /// <returns>True if the level was selected successfully, false otherwise.</returns>
        public bool SelectLevel(int levelIndex)
        {
            if (!Active || Completed || !IsLevelUnlocked(levelIndex))
            {
                return false;
            }

            CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, rimrushAdventureCatalog.LevelCount - 1);
            return true;
        }

        /// <summary>
        /// Returns whether the given level index has been unlocked by the player.
        /// </summary>
        /// <param name="levelIndex">The level index to check.</param>
        /// <returns>True if the level is within the unlocked range, false otherwise.</returns>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedLevelIndex && levelIndex < rimrushAdventureCatalog.LevelCount;
        }

        /// <summary>
        /// Returns whether the given level index has been completed by the player.
        /// </summary>
        /// <param name="levelIndex">The level index to check.</param>
        /// <returns>True if the level has been completed, false otherwise.</returns>
        public bool IsLevelCompleted(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < levelCompleted.Length && levelCompleted[levelIndex];
        }

        /// <summary>
        /// Applies the result of the current level match. If the player won, marks the level
        /// as completed and unlocks the next level. If the player lost, records the result
        /// but does not advance progress.
        /// </summary>
        /// <param name="playerWon">True if the player won the match.</param>
        public void ApplyCurrentMatchResult(bool playerWon)
        {
            if (!Active || Completed || CurrentLevelIndex < 0 || CurrentLevelIndex >= levelCompleted.Length)
            {
                return;
            }

            LastResolvedLevelIndex = CurrentLevelIndex;
            LastPlayerWon = playerWon;
            if (!playerWon)
            {
                return;
            }

            levelCompleted[CurrentLevelIndex] = true;
            if (CurrentLevelIndex >= rimrushAdventureCatalog.LevelCount - 1)
            {
                Completed = true;
                return;
            }

            HighestUnlockedLevelIndex = Mathf.Max(HighestUnlockedLevelIndex, CurrentLevelIndex + 1);
            CurrentLevelIndex = Mathf.Max(CurrentLevelIndex + 1, HighestUnlockedLevelIndex);
        }
    }
}
