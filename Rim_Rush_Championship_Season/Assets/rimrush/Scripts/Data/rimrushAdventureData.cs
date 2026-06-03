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
            string victoryBeat)
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
            VictoryBeat = victoryBeat;
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
                "First Sigil, clear and welcoming.",
                rimrushAdventureMechanic.BasicDuel,
                "BASIC DUEL",
                "Classic 1v1 rules. Win the gate and claim the first Sigil.",
                "Locked front gate, pumpkin lamps, and a simple court poster.",
                new[] { "1V1", "SIGIL", "GATE" },
                128f,
                332f,
                rimrushBallSelection.PumpkinEmber,
                1,
                "The first Lantern Sigil snaps into the park map."),
            new rimrushAdventureLevelDefinition(
                1,
                "CANDY ARCH STREET",
                7,
                "Light, quick, and agile.",
                rimrushAdventureMechanic.CandyCharge,
                "CANDY CHARGE",
                "Your super meter gains a steady candy boost.",
                "Candy stalls, bright arches, and a fast Black Cat poster.",
                new[] { "CANDY", "CHARGE", "BOOST" },
                214f,
                272f,
                rimrushBallSelection.CandySwirl,
                2,
                "The broadcast admits the lockdown is an old park ritual, not a normal outage."),
            new rimrushAdventureLevelDefinition(
                2,
                "LAUGHING MIRROR HOUSE",
                1,
                "Noisy, playful, and theatrical.",
                rimrushAdventureMechanic.DoubleHoop,
                "DOUBLE RIM",
                "When the mirror lights up, made baskets score double.",
                "Comic mirror panels, carnival bulbs, and a glowing second rim sign.",
                new[] { "RIM X2", "TIMED", "RUSH" },
                304f,
                326f,
                rimrushBallSelection.EvilEye,
                3,
                "A mirror reflection shows the Pumpkin Heart Lantern flickering above the main dome."),
            new rimrushAdventureLevelDefinition(
                3,
                "CANDLE HALL",
                4,
                "Warm, ceremonial, and rhythmic.",
                rimrushAdventureMechanic.CandleCircle,
                "CANDLE RING",
                "Candle heat slowly feeds both players' super meters.",
                "A long candle corridor with glowing floor rings and gold smoke.",
                new[] { "CANDLE", "CHARGE", "TEMPO" },
                390f,
                246f,
                rimrushBallSelection.JackOLantern,
                4,
                "The recovered Sigils begin pointing toward the main dome."),
            new rimrushAdventureLevelDefinition(
                4,
                "FOG DOCK",
                2,
                "Sideways motion, mist, and drifting shots.",
                rimrushAdventureMechanic.FogWind,
                "FOG WIND",
                "A light wind nudges airborne balls sideways.",
                "Foggy dock planks, lantern buoys, and a pirate court banner.",
                new[] { "WIND", "BALL", "DRIFT" },
                482f,
                318f,
                rimrushBallSelection.GhoulGreen,
                5,
                "The map reveals that the public Cup was built from this same hidden ritual."),
            new rimrushAdventureLevelDefinition(
                5,
                "BLOOD MOON TERRACE",
                3,
                "Pressure rises under a red moon.",
                rimrushAdventureMechanic.BloodMoon,
                "BLOOD MOON",
                "During moon pulses, match tempo speeds up.",
                "A moonlit terrace washed in red light and long court shadows.",
                new[] { "MOON", "SPEED", "PULSE" },
                572f,
                260f,
                rimrushBallSelection.MoonlitViolet,
                6,
                "The Pumpkin Heart Lantern brightens whenever a duel reaches its peak."),
            new rimrushAdventureLevelDefinition(
                6,
                "CLOCKTOWER GRAVEYARD",
                0,
                "Late-route pressure and defensive focus.",
                rimrushAdventureMechanic.HarvestTime,
                "HARVEST TIME",
                "In the final 15 seconds, every made basket gains +1.",
                "Clock hands, low fog, and a final-minute Reaper poster.",
                new[] { "LAST 15", "+1", "CLUTCH" },
                654f,
                332f,
                rimrushBallSelection.Cursed8Ball,
                7,
                "One last Sigil remains before the main dome unlocks."),
            new rimrushAdventureLevelDefinition(
                7,
                "MOON LANTERN DOME",
                6,
                "Final stage, bright and ceremonial.",
                rimrushAdventureMechanic.MoonLanternMix,
                "MOON MIX",
                "The final duel rotates Candy, Double Rim, Fog Wind, and Blood Moon beats.",
                "Main dome spotlights, the Heart Lantern, and the Witch on center court.",
                new[] { "MIX", "FINAL", "DOME" },
                728f,
                248f,
                rimrushBallSelection.Random,
                8,
                "The Pumpkin Heart Lantern steadies, and Moon Lantern Park opens its gate before dawn.")
        };

        public static int LevelCount => Levels.Length;

        public static rimrushAdventureLevelDefinition[] AllLevels => Levels;

        public static rimrushAdventureLevelDefinition GetLevel(int index)
        {
            return Levels[Mathf.Clamp(index, 0, Levels.Length - 1)];
        }
    }

    public sealed class rimrushAdventureData
    {
        private readonly bool[] levelCompleted = new bool[rimrushAdventureCatalog.LevelCount];

        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public int PlayerCharacterId { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public int HighestUnlockedLevelIndex { get; private set; }
        public int LastResolvedLevelIndex { get; private set; } = -1;
        public bool LastPlayerWon { get; private set; }
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

        public bool HasPendingPlayerMatch => Active && !Completed && CurrentLevelIndex >= 0 && CurrentLevelIndex < rimrushAdventureCatalog.LevelCount;
        public rimrushAdventureLevelDefinition CurrentLevel => rimrushAdventureCatalog.GetLevel(CurrentLevelIndex);

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

        public void Create(int playerCharacterId)
        {
            Reset();
            Active = true;
            PlayerCharacterId = rimrushPlayersData.SanitizeCharacterId(playerCharacterId);
        }

        public bool SelectLevel(int levelIndex)
        {
            if (!Active || Completed || !IsLevelUnlocked(levelIndex))
            {
                return false;
            }

            CurrentLevelIndex = Mathf.Clamp(levelIndex, 0, rimrushAdventureCatalog.LevelCount - 1);
            return true;
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedLevelIndex && levelIndex < rimrushAdventureCatalog.LevelCount;
        }

        public bool IsLevelCompleted(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < levelCompleted.Length && levelCompleted[levelIndex];
        }

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
