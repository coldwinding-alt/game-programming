using UnityEngine;

namespace rimrush
{
    public enum rimrushCharacterSkillType
    {
        SoulReap,
        CarnivalJackpot,
        GhostSail,
        BloodMoonBlink,
        WaxOverdrive,
        HarvestTime,
        HexGate,
        BadLuck
    }

    public readonly struct rimrushCharacterSkillDefinition
    {
        public readonly rimrushCharacterSkillType SkillType;
        public readonly int IconSuperId;
        public readonly string SkillName;
        public readonly string ActivateNotice;
        public readonly string ScoreNotice;
        public readonly Color PrimaryColor;
        public readonly Color SecondaryColor;
        public readonly Color AccentColor;
        public readonly float EffectDuration;
        public readonly float BonusDuration;
        public readonly float MoveSpeedMultiplier;
        public readonly float AccuracyModifier;
        public readonly float AccuracyPenalty;
        public readonly float ScoreRefundFraction;
        public readonly int FlatScoreBonus;

        public rimrushCharacterSkillDefinition(
            rimrushCharacterSkillType skillType,
            int iconSuperId,
            string skillName,
            string activateNotice,
            string scoreNotice,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor,
            float effectDuration = 0f,
            float bonusDuration = 0f,
            float moveSpeedMultiplier = 1f,
            float accuracyModifier = 0f,
            float accuracyPenalty = 0f,
            float scoreRefundFraction = 0f,
            int flatScoreBonus = 0)
        {
            SkillType = skillType;
            IconSuperId = iconSuperId;
            SkillName = skillName;
            ActivateNotice = activateNotice;
            ScoreNotice = scoreNotice;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            AccentColor = accentColor;
            EffectDuration = effectDuration;
            BonusDuration = bonusDuration;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            AccuracyModifier = accuracyModifier;
            AccuracyPenalty = accuracyPenalty;
            ScoreRefundFraction = scoreRefundFraction;
            FlatScoreBonus = flatScoreBonus;
        }

        public bool UsesTeleportDunk =>
            SkillType == rimrushCharacterSkillType.BloodMoonBlink ||
            SkillType == rimrushCharacterSkillType.HexGate;

        public bool UsesBasketShield => SkillType == rimrushCharacterSkillType.GhostSail;

        public bool UsesDashSkill => SkillType == rimrushCharacterSkillType.SoulReap;

        public bool UsesPossessionSkill =>
            SkillType == rimrushCharacterSkillType.CarnivalJackpot ||
            SkillType == rimrushCharacterSkillType.BloodMoonBlink ||
            SkillType == rimrushCharacterSkillType.WaxOverdrive ||
            SkillType == rimrushCharacterSkillType.HarvestTime ||
            SkillType == rimrushCharacterSkillType.HexGate;

        public bool UsesCurseSkill => SkillType == rimrushCharacterSkillType.BadLuck;

        public bool UsesScoreUpgrade =>
            SkillType == rimrushCharacterSkillType.CarnivalJackpot ||
            SkillType == rimrushCharacterSkillType.HarvestTime;

        public bool RequiresBallToCast =>
            SkillType == rimrushCharacterSkillType.CarnivalJackpot ||
            SkillType == rimrushCharacterSkillType.BloodMoonBlink ||
            SkillType == rimrushCharacterSkillType.WaxOverdrive ||
            SkillType == rimrushCharacterSkillType.HarvestTime ||
            SkillType == rimrushCharacterSkillType.HexGate;
    }

    public static class rimrushCharacterSkillsData
    {
        private static readonly rimrushCharacterSkillDefinition[] Skills =
        {
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.SoulReap,
                3,
                "SOUL REAP",
                "SOUL REAP!",
                "REAPED",
                new Color32(0xB9, 0xFF, 0xE4, 0xFF),
                new Color32(0x4F, 0x86, 0x79, 0xFF),
                new Color32(0xEC, 0xFF, 0xF7, 0xFF),
                bonusDuration: 6f,
                flatScoreBonus: 1),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.CarnivalJackpot,
                0,
                "CARNIVAL JACKPOT",
                "JACKPOT!",
                "JACKPOT",
                new Color32(0xFF, 0xD0, 0x67, 0xFF),
                new Color32(0x65, 0xD3, 0xD4, 0xFF),
                new Color32(0xFF, 0xF4, 0xCC, 0xFF),
                effectDuration: 4f),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.GhostSail,
                1,
                "GHOST SAIL",
                "GHOST SAIL!",
                "GHOST SAIL",
                new Color32(0xB8, 0xFF, 0xD5, 0xFF),
                new Color32(0x5C, 0x86, 0x7D, 0xFF),
                new Color32(0xF7, 0xE2, 0x9F, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.BloodMoonBlink,
                2,
                "BLOOD MOON BLINK",
                "BLOOD MOON!",
                "BLOOD MOON",
                new Color32(0xFF, 0x8B, 0x9D, 0xFF),
                new Color32(0x7D, 0x19, 0x31, 0xFF),
                new Color32(0xFF, 0xF0, 0xF4, 0xFF),
                scoreRefundFraction: 0.35f),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.WaxOverdrive,
                3,
                "WAX OVERDRIVE",
                "OVERDRIVE!",
                "EMBER +1",
                new Color32(0xFF, 0xB1, 0x45, 0xFF),
                new Color32(0x8C, 0x45, 0x17, 0xFF),
                new Color32(0xFF, 0xEC, 0xB0, 0xFF),
                effectDuration: 4f,
                moveSpeedMultiplier: 1.32f,
                accuracyModifier: -0.015f,
                flatScoreBonus: 1),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.HarvestTime,
                0,
                "HARVEST TIME",
                "HARVEST!",
                "HARVEST",
                new Color32(0xE0, 0x9D, 0x2A, 0xFF),
                new Color32(0x6E, 0x5A, 0x2C, 0xFF),
                new Color32(0xF4, 0xDE, 0x99, 0xFF),
                effectDuration: 5f),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.HexGate,
                2,
                "HEX GATE",
                "HEX GATE!",
                "HEX GATE",
                new Color32(0xD2, 0x9D, 0xFF, 0xFF),
                new Color32(0x4A, 0x1F, 0x77, 0xFF),
                new Color32(0xF4, 0xEB, 0xFF, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.BadLuck,
                1,
                "BAD LUCK",
                "BAD LUCK!",
                "BAD LUCK",
                new Color32(0xB8, 0x98, 0xFF, 0xFF),
                new Color32(0x22, 0x22, 0x44, 0xFF),
                new Color32(0xFF, 0xD9, 0x74, 0xFF),
                effectDuration: 4f,
                accuracyPenalty: 0.12f)
        };

        public static rimrushCharacterSkillDefinition Get(int characterId)
        {
            return characterId >= 0 && characterId < Skills.Length
                ? Skills[characterId]
                : Skills[0];
        }
    }
}
