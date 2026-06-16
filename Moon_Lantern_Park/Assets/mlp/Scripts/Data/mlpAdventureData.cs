// Adventure mode level data
// 8 levels that define Adventure Mode, each with different opponents, scenarios, and special rules. Players need to clear one level after another and collect lantern marks to unlock the final escape route.

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// Special rule types in adventure mode: Each level has different rule effects, such as candy charging, double baskets, misty wind, blood moon, etc.
    /// </summary>
    public enum mlpAdventureMechanic
    {
        /// <summary>Basic duel: a pure 1v1 shooting competition without any special rules. It is the first teaching level. </summary>
        BasicDuel,
        /// <summary>Candy recharge: The player's super meter recharges faster, making it easier to release special moves. </summary>
        CandyCharge,
        /// <summary>Double Basket: Active windows appear on the field, and goals scored during this period are scored double. </summary>
        DoubleHoop,
        /// <summary>Candlelight Circle: Both sides' super gauges will automatically recharge, but the player's recharging speed will be faster, creating a rhythm game. </summary>
        CandleCircle,
        /// <summary>Misty Wind: The flying basketball will be blown a short distance laterally by the wind, testing the player's shot prediction and correction abilities. </summary>
        FogWind,
        /// <summary>Blood Moon: The pace of the entire game is accelerated (time flow rate × 1.14), increasing reaction pressure. </summary>
        BloodMoon,
        /// <summary>Harvest moment: The last 15 seconds of the game enter a harvest state, with an extra +1 point for each goal, encouraging the final victory. </summary>
        HarvestTime,
        /// <summary>Moonlight Mix: The final boss level, all the above mechanisms are automatically rotated every 10 seconds (charge → double → wind → acceleration → cycle), testing all-round abilities. </summary>
        MoonLanternMix
    }

    /// <summary>
    /// Adventure mode single level definition: an immutable data container that describes all the information of a level.
    /// Each system of the game (scene loading, AI configuration, map rendering, UI display) will take what they need from here.
    /// </summary>
    public sealed class mlpAdventureLevelDefinition
    {
        /// <summary>Level index (starting from 0), used to identify the level sequence and progress judgment. </summary>
        public readonly int Index;
        /// <summary>The display name of the level area, such as "PUMPKIN GATEWAY", as shown on the UI and map. </summary>
        public readonly string AreaName;
        /// <summary>The guardian character ID guarding this level determines which character model and skills the opponent uses. </summary>
        public readonly int WardenCharacterId;
        /// <summary>A brief description of the level's atmosphere, for reference in art and sound effects, such as "First gate.", "Red pressure." </summary>
        public readonly string Mood;
        /// <summary>The unique gameplay mechanism used in this level determines the special rules in the game. </summary>
        public readonly mlpAdventureMechanic Mechanic;
        /// <summary>The display title of the gameplay mechanism is displayed on the rules overlay, such as "CANDY CHARGE", "BLOOD MOON". </summary>
        public readonly string MechanicTitle;
        /// <summary>A brief description of the gameplay mechanism to help players understand the special rules of the current level. </summary>
        public readonly string MechanicSummary;
        /// <summary>Art direction description of the scene, describing the visual style and scene elements of the level. </summary>
        public readonly string SceneDirection;
        /// <summary>The array of icon key names displayed on the rule overlay, such as { "1V1", "SIGIL", "GATE" }. </summary>
        public readonly string[] RuleIcons;
        /// <summary>The X coordinate on the adventure map, used to locate level nodes in the map interface. </summary>
        public readonly float MapX;
        /// <summary>The Y coordinate on the adventure map, used to locate level nodes in the map interface. </summary>
        public readonly float MapY;
        /// <summary>The basketball skin used in this level determines the appearance of the basketball in the game. </summary>
        public readonly mlpBallSelection BallSelection;
        // The old version of Adventure Mode used this field to represent the basic AI skill value of each level.

        // In the current fixed four-level difficulty mode, the actual competition intensity only depends on the Easy/Normal/Hard/Hell selected by the player.
        // Therefore, this field is not directly involved in AI skill calculation for the time being; it is retained so as not to change the level data structure and old data.
        public readonly int OpponentSkill;
        public readonly string VictoryBeat;
        public readonly string[] VictoryLines;
        public readonly string[] DefeatLines;

        /// <summary>
        /// Create an adventure level definition, including all gameplay, narrative and map data.
        /// </summary>
        /// <param name="index">The zero-based level index. </param>
        /// <param name="areaName">The display name of the level area. </param>
        /// <param name="wardenCharacterId">The ID of the guardian character guarding this level. </param>
        /// <param name="mood">A short description of the level's mood. </param>
        /// <param name="mechanic">The unique gameplay mechanism used in this level. </param>
        /// <param name="mechanicTitle">The display title of the gameplay mechanism. </param>
        /// <param name="mechanicSummary">A brief description of the gameplay mechanics. </param>
        /// <param name="sceneDirection">Art direction description of the scene. </param>
        /// <param name="ruleIcons">The icon key name displayed on the rule overlay. </param>
        /// <param name="mapX">The X coordinate on the adventure map. </param>
        /// <param name="mapY">The Y coordinate on the adventure map. </param>
        /// <param name="ballSelection">The basketball skin used in this level. </param>
        /// <param name="opponentSkill">The basic AI skill value of the old version of the level; it is not directly involved in the calculation of competition intensity in the current fixed four-level difficulty mode. </param>
        /// <param name="victoryLines">Dialogue lines displayed when the player wins. </param>
        /// <param name="defeatLines">Dialogue lines displayed when the player fails. </param>
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
            // 1. Set the basic information of the level (index, area name, guardian, atmosphere)
            Index = index;
            AreaName = areaName;
            WardenCharacterId = wardenCharacterId;
            Mood = mood;
            // 2. Set gameplay mechanism information (type, title, description, scene guidance)

            Mechanic = mechanic;
            MechanicTitle = mechanicTitle;
            MechanicSummary = mechanicSummary;
            SceneDirection = sceneDirection;
            // 3. Set the rule icon (replace it with an empty array when null)

            RuleIcons = ruleIcons ?? new string[0];
            // 4. Set map coordinates and ball skin
            MapX = mapX;
            MapY = mapY;
            BallSelection = ballSelection;
            // 5. Set opponent skill level

            OpponentSkill = opponentSkill;
            // 6. Set victory and defeat lines (replace null with empty array)

            VictoryLines = victoryLines ?? new string[0];
            DefeatLines = defeatLines ?? new string[0];
            // 7. Extract the first victory line as the default display text
            VictoryBeat = VictoryLines.Length > 0 ? VictoryLines[0] : string.Empty;
        }

        /// <summary>
        /// A line of dialogue is randomly selected from the pool of winning or losing lines.
        /// </summary>
        /// <param name="playerWon">Pass true to select victory lines, and pass false to select failure lines. </param>
        /// <returns> Randomly selected result lines, or default text if the line pool is empty. </returns>
        public string GetRandomResultLine(bool playerWon)
        {
            // 1. Select the corresponding line pool according to the outcome of victory or defeat (victory lines or failure lines)

            var pool = playerWon ? VictoryLines : DefeatLines;
            // 2. If the line pool is not empty, randomly select one and return it.

            if (pool != null && pool.Length > 0)
            {
                return pool[Random.Range(0, pool.Length)];
            }

            // 3. If the line pool is empty, return the default backup lines.

            return playerWon
                ? "Take the Lantern Sigil and keep moving."
                : "The Lantern Sigil stays out of reach.";
        }
    }

    /// <summary>
    /// Adventure mode level directory: stores the definition data of all 8 levels for the game to read and use.

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
                "Crosswind gusts.",
                mlpAdventureMechanic.FogWind,
                "FOG WIND",
                "Airborne balls get shoved hard sideways by crosswind gusts.",
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
        /// Gets the total number of adventure levels defined in the directory.

        /// </summary>
        public static int LevelCount => Levels.Length;

        /// <summary>
        /// Get an array of all adventure level definitions.
        /// </summary>
        public static mlpAdventureLevelDefinition[] AllLevels => Levels;

        /// <summary>
        /// Get the adventure level definition at the specified index. The index will be limited to the valid range.
        /// </summary>
        /// <param name="index">The level index to find. </param>
        /// <returns>The level definition corresponding to the index. </returns>
        public static mlpAdventureLevelDefinition GetLevel(int index)
        {
            return Levels[Mathf.Clamp(index, 0, Levels.Length - 1)];
        }
    }

    /// <summary>
    /// Adventure mode progress data: manages the player's current level, cleared status, and lantern mark collection status in the adventure mode.
    /// </summary>
    public sealed class mlpAdventureData
    {
        private readonly bool[] levelCompleted = new bool[mlpAdventureCatalog.LevelCount];

        /// <summary>Whether adventure mode is in progress. </summary>
        public bool Active { get; private set; }
        /// <summary>Whether the player has completed all adventure levels. </summary>
        public bool Completed { get; private set; }
        /// <summary>The character ID chosen by the player in this adventure. </summary>
        public int PlayerCharacterId { get; private set; }
        /// <summary>The level index that the player is currently in or is selecting. </summary>
        public int CurrentLevelIndex { get; private set; }
        /// <summary>The highest level index that the player has unlocked so far. </summary>
        public int HighestUnlockedLevelIndex { get; private set; }
        /// <summary>Level index of the last settled match result, or -1 if none. </summary>
        public int LastResolvedLevelIndex { get; private set; } = -1;
        /// <summary>Whether the player won the last settled match. </summary>
        public bool LastPlayerWon { get; private set; }
        /// <summary>
        /// Gets the number of Lantern Marks the player has collected.
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
        /// Determine whether there is an ongoing adventure that has not yet been completed and the current level is valid, so you can start the game.

        /// </summary>
        public bool HasPendingPlayerMatch => Active && !Completed && CurrentLevelIndex >= 0 && CurrentLevelIndex < mlpAdventureCatalog.LevelCount;
        /// <summary>
        /// Get the level definition corresponding to the current level index.

        /// </summary>
        public mlpAdventureLevelDefinition CurrentLevel => mlpAdventureCatalog.GetLevel(CurrentLevelIndex);

        /// <summary>
        /// Resets all adventure progress, returning the adventure status to inactive.
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
        /// Start a new adventure with a designated player character.
        /// </summary>
        /// <param name="playerCharacterId">The character ID to use for this adventure. </param>
        public void Create(int playerCharacterId)
        {
            // 1. Clear all previous adventure progress first.
            Reset();
            // 2. Mark Adventure Mode as "In Progress"
            Active = true;
            // 3. Verify and record the character number selected by the player (if invalid, it will be automatically corrected to an available character)
            PlayerCharacterId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
        }

        /// <summary>
        /// Try to select the level at the specified index as the current level. If the adventure is not active, completed, or the level is not unlocked, the selection fails.
        /// </summary>
        /// <param name="levelIndex">The level index to select. </param>
        /// <returns>Returns true if level selection is successful, otherwise returns false. </returns>
        public bool SelectLevel(int levelIndex)
        {
            // 1. Check whether the adventure is in progress, not completed, and the level has been unlocked
            if (!Active || Completed || !IsLevelUnlocked(levelIndex))
            {
                return false;
            }

            // 2. Limit the level index to the valid range and set it to the current level
            CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, mlpAdventureCatalog.LevelCount - 1);
            return true;
        }

        /// <summary>
        /// Checks whether the level at the specified index has been unlocked by the player.
        /// </summary>
        /// <param name="levelIndex">The level index to check. </param>
        /// <returns>Returns true if the level is within unlocked range, false otherwise. </returns>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedLevelIndex && levelIndex < mlpAdventureCatalog.LevelCount;
        }

        /// <summary>
        /// Checks whether the level at the specified index has been completed by the player.
        /// </summary>
        /// <param name="levelIndex">The level index to check. </param>
        /// <returns>Returns true if the level has been completed, false otherwise. </returns>
        public bool IsLevelCompleted(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < levelCompleted.Length && levelCompleted[levelIndex];
        }

        /// <summary>
        /// Calculate the results of the current level. If the player wins, the level is marked as completed and the next level is unlocked; if the player fails, the result is only recorded without progress.
        /// </summary>
        /// <param name="playerWon">Pass true if the player won the game. </param>
        public void ApplyCurrentMatchResult(bool playerWon)
        {
            // 1. Check whether the adventure is valid (in progress, unfinished, current level index is legal)
            if (!Active || Completed || CurrentLevelIndex < 0 || CurrentLevelIndex >= levelCompleted.Length)
            {
                return;
            }

            // 2. Record the result of the latest game (which level, whether you won or not)

            LastResolvedLevelIndex = CurrentLevelIndex;
            LastPlayerWon = playerWon;
            // 3. If the player loses, only the result will be recorded and the adventure progress will not be advanced.

            if (!playerWon)
            {
                return;
            }

            // 4. The player wins: Mark the current level as "Completed"
            levelCompleted[CurrentLevelIndex] = true;
            // 5. If it's the last level, mark the entire adventure as "Complete"

            if (CurrentLevelIndex >= mlpAdventureCatalog.LevelCount - 1)
            {
                Completed = true;
                return;
            }

            // 6. Unlock the next level and advance the current level to the next level

            HighestUnlockedLevelIndex = Mathf.Max(HighestUnlockedLevelIndex, CurrentLevelIndex + 1);
            CurrentLevelIndex = Mathf.Max(CurrentLevelIndex + 1, HighestUnlockedLevelIndex);
        }
    }
}
