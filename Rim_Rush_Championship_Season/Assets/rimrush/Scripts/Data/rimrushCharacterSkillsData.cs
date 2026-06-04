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
        public readonly string IconImageKey;
        public readonly string ChargeMaskImageKey;
        public readonly string ActivateNotice;
        public readonly string ScoreNotice;
        public readonly Color PrimaryColor;
        public readonly Color SecondaryColor;
        public readonly Color AccentColor;
        public readonly float EffectDuration;
        public readonly float BonusDuration;
        public readonly float MoveSpeedMultiplier;
        public readonly float AccuracyModifier;
        public readonly float ScoreRefundFraction;
        public readonly int FlatScoreBonus;

        public rimrushCharacterSkillDefinition(
            rimrushCharacterSkillType skillType,
            int iconSuperId,
            string skillName,
            string iconImageKey,
            string chargeMaskImageKey,
            string activateNotice,
            string scoreNotice,
            Color primaryColor,
            Color secondaryColor,
            Color accentColor,
            float effectDuration = 0f,
            float bonusDuration = 0f,
            float moveSpeedMultiplier = 1f,
            float accuracyModifier = 0f,
            float scoreRefundFraction = 0f,
            int flatScoreBonus = 0)
        {
            SkillType = skillType;
            IconSuperId = iconSuperId;
            SkillName = skillName;
            IconImageKey = iconImageKey;
            ChargeMaskImageKey = chargeMaskImageKey;
            ActivateNotice = activateNotice;
            ScoreNotice = scoreNotice;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            AccentColor = accentColor;
            EffectDuration = effectDuration;
            BonusDuration = bonusDuration;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            AccuracyModifier = accuracyModifier;
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

        public bool UsesFreezeSkill => SkillType == rimrushCharacterSkillType.BadLuck;

        public bool UsesScoreUpgrade =>
            SkillType == rimrushCharacterSkillType.CarnivalJackpot ||
            SkillType == rimrushCharacterSkillType.HarvestTime;

        public bool RequiresBallToCast =>
            SkillType == rimrushCharacterSkillType.CarnivalJackpot ||
            SkillType == rimrushCharacterSkillType.BloodMoonBlink ||
            SkillType == rimrushCharacterSkillType.HarvestTime ||
            SkillType == rimrushCharacterSkillType.HexGate;

        public bool HasStandaloneIconArt =>
            !string.IsNullOrEmpty(IconImageKey) &&
            !string.IsNullOrEmpty(ChargeMaskImageKey);
    }

    public static class rimrushCharacterSkillsData
    {
        private static readonly rimrushCharacterSkillDefinition[] Skills =
        {
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.SoulReap,
                3,
                "DASH STEAL",
                rimrushAssets.Images.SkillIcons.Reaper,
                rimrushAssets.Images.SkillIcons.ReaperMask,
                "DASH STEAL!",
                "BALL STOLEN",
                new Color32(0xB9, 0xFF, 0xE4, 0xFF),
                new Color32(0x4F, 0x86, 0x79, 0xFF),
                new Color32(0xEC, 0xFF, 0xF7, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.BloodMoonBlink,
                2,
                "BLINK DUNK",
                rimrushAssets.Images.SkillIcons.GhostClown,
                rimrushAssets.Images.SkillIcons.GhostClownMask,
                "BLINK DUNK!",
                "BLINK DUNK",
                new Color32(0xFF, 0xD0, 0x67, 0xFF),
                new Color32(0x65, 0xD3, 0xD4, 0xFF),
                new Color32(0xFF, 0xF4, 0xCC, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.GhostSail,
                1,
                "HOOP SHIELD",
                rimrushAssets.Images.SkillIcons.SkullPirate,
                rimrushAssets.Images.SkillIcons.SkullPirateMask,
                "HOOP SHIELD!",
                "SHIELD BLOCK",
                new Color32(0xB8, 0xFF, 0xD5, 0xFF),
                new Color32(0x5C, 0x86, 0x7D, 0xFF),
                new Color32(0xF7, 0xE2, 0x9F, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.BloodMoonBlink,
                2,
                "BLINK DUNK",
                rimrushAssets.Images.SkillIcons.Vampire,
                rimrushAssets.Images.SkillIcons.VampireMask,
                "BLINK DUNK!",
                "BLINK DUNK",
                new Color32(0xFF, 0x8B, 0x9D, 0xFF),
                new Color32(0x7D, 0x19, 0x31, 0xFF),
                new Color32(0xFF, 0xF0, 0xF4, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.WaxOverdrive,
                3,
                "SPEED BOOST",
                rimrushAssets.Images.SkillIcons.Candleman,
                rimrushAssets.Images.SkillIcons.CandlemanMask,
                "SPEED BOOST!",
                "FAST BREAK",
                new Color32(0xFF, 0xB1, 0x45, 0xFF),
                new Color32(0x8C, 0x45, 0x17, 0xFF),
                new Color32(0xFF, 0xEC, 0xB0, 0xFF),
                effectDuration: 3.5f,
                moveSpeedMultiplier: 1.35f),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.CarnivalJackpot,
                0,
                "NEXT SCORE +2",
                rimrushAssets.Images.SkillIcons.Scarecrow,
                rimrushAssets.Images.SkillIcons.ScarecrowMask,
                "NEXT SCORE +2!",
                "+2 SCORE",
                new Color32(0xE0, 0x9D, 0x2A, 0xFF),
                new Color32(0x6E, 0x5A, 0x2C, 0xFF),
                new Color32(0xF4, 0xDE, 0x99, 0xFF)),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.BadLuck,
                1,
                "FREEZE 2 SEC",
                rimrushAssets.Images.SkillIcons.Witch,
                rimrushAssets.Images.SkillIcons.WitchMask,
                "FREEZE 2 SEC!",
                "FROZEN",
                new Color32(0x9C, 0xE8, 0xFF, 0xFF),
                new Color32(0x25, 0x3A, 0x78, 0xFF),
                new Color32(0xEA, 0xFB, 0xFF, 0xFF),
                effectDuration: 2f),
            new rimrushCharacterSkillDefinition(
                rimrushCharacterSkillType.SoulReap,
                3,
                "DASH STEAL",
                rimrushAssets.Images.SkillIcons.BlackCat,
                rimrushAssets.Images.SkillIcons.BlackCatMask,
                "DASH STEAL!",
                "BALL STOLEN",
                new Color32(0xB8, 0x98, 0xFF, 0xFF),
                new Color32(0x22, 0x22, 0x44, 0xFF),
                new Color32(0xFF, 0xD9, 0x74, 0xFF))
        };

        public static rimrushCharacterSkillDefinition Get(int characterId)
        {
            return characterId >= 0 && characterId < Skills.Length
                ? Skills[characterId]
                : Skills[0];
        }
    }
}
