// 角色技能数据
// 定义 8 个角色各自的专属技能：技能名称、冷却时间、效果描述。每个角色的技能都不一样，有的能加速，有的能增强投篮。

using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 角色技能类型：每个角色对应一种专属技能，如灵魂收割、幽灵帆、血月闪现等。
    /// </summary>
    public enum mlpCharacterSkillType
    {
        SoulReap,
        CarnivalJackpot,
        GhostSail,
        BloodMoonBlink,
        WaxOverdrive,
        HarvestTime,
        HexGate,
        BadLuck,
        ReboundMagnet,
        SureBlock
    }

    /// <summary>
    /// 角色技能定义：描述一个技能的全部信息——名称、图标、充能效果、激活提示、冷却时间等。
    /// </summary>
    public readonly struct mlpCharacterSkillDefinition
    {
        public readonly mlpCharacterSkillType SkillType;
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

        /// <summary>
        /// 创建角色技能定义，包含所有视觉效果、玩法和平衡性数据。
        /// </summary>
        /// <param name="skillType">技能类型。</param>
        /// <param name="iconSuperId">必杀技能量条 UI 使用的图标索引。</param>
        /// <param name="skillName">技能的显示名称。</param>
        /// <param name="iconImageKey">技能图标的图片键名。</param>
        /// <param name="chargeMaskImageKey">充能遮罩层的图片键名。</param>
        /// <param name="activateNotice">技能激活时显示的提示文本。</param>
        /// <param name="scoreNotice">技能影响得分时显示的提示文本。</param>
        /// <param name="primaryColor">技能的主要 UI 颜色。</param>
        /// <param name="secondaryColor">技能的次要 UI 颜色。</param>
        /// <param name="accentColor">技能的强调 UI 颜色。</param>
        /// <param name="effectDuration">技能效果持续时间（秒）。</param>
        /// <param name="bonusDuration">额外的奖励持续时间（秒）。</param>
        /// <param name="moveSpeedMultiplier">技能激活时的移动速度倍率。</param>
        /// <param name="accuracyModifier">技能激活时的命中率加成。</param>
        /// <param name="scoreRefundFraction">成功操作后返还的得分比例。</param>
        /// <param name="flatScoreBonus">成功操作后额外增加的固定分数。</param>
        public mlpCharacterSkillDefinition(
            mlpCharacterSkillType skillType,
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

        /// <summary>
        /// 判断该技能是否使用传送扣篮机制。
        /// </summary>
        public bool UsesTeleportDunk =>
            SkillType == mlpCharacterSkillType.BloodMoonBlink ||
            SkillType == mlpCharacterSkillType.HexGate;

        /// <summary>
        /// 判断该技能是否使用篮筐护盾机制。
        /// </summary>
        public bool UsesBasketShield => SkillType == mlpCharacterSkillType.GhostSail;

        /// <summary>
        /// 判断该技能是否使用冲刺机制。
        /// </summary>
        public bool UsesDashSkill => SkillType == mlpCharacterSkillType.SoulReap;

        /// <summary>
        /// 判断该技能是否需要持球才能激活。
        /// </summary>
        public bool UsesPossessionSkill =>
            SkillType == mlpCharacterSkillType.CarnivalJackpot ||
            SkillType == mlpCharacterSkillType.BloodMoonBlink ||
            SkillType == mlpCharacterSkillType.WaxOverdrive ||
            SkillType == mlpCharacterSkillType.HarvestTime ||
            SkillType == mlpCharacterSkillType.HexGate;

        /// <summary>
        /// 判断该技能是否使用冰冻机制。
        /// </summary>
        public bool UsesFreezeSkill => SkillType == mlpCharacterSkillType.BadLuck;

        /// <summary>
        /// 判断该技能是否能将自由篮板球拉向施法者。
        /// </summary>
        public bool UsesReboundMagnetSkill => SkillType == mlpCharacterSkillType.ReboundMagnet;

        /// <summary>
        /// 判断该技能是否能立即封盖对手的投篮。
        /// </summary>
        public bool UsesGuaranteedBlockSkill => SkillType == mlpCharacterSkillType.SureBlock;

        /// <summary>
        /// 判断该技能是否能提升下一次投篮的得分值。
        /// </summary>
        public bool UsesScoreUpgrade =>
            SkillType == mlpCharacterSkillType.CarnivalJackpot ||
            SkillType == mlpCharacterSkillType.HarvestTime;

        /// <summary>
        /// 判断玩家是否必须持球才能激活该技能。
        /// </summary>
        public bool RequiresBallToCast =>
            SkillType == mlpCharacterSkillType.CarnivalJackpot ||
            SkillType == mlpCharacterSkillType.BloodMoonBlink ||
            SkillType == mlpCharacterSkillType.HarvestTime ||
            SkillType == mlpCharacterSkillType.HexGate;

        /// <summary>
        /// 判断该技能是否拥有独立的图标和充能遮罩美术资源。
        /// </summary>
        public bool HasStandaloneIconArt =>
            !string.IsNullOrEmpty(IconImageKey) &&
            !string.IsNullOrEmpty(ChargeMaskImageKey);
    }

    /// <summary>
    /// 角色技能数据表：存储所有 8 个角色的技能定义，根据角色 ID 获取对应的技能信息。
    /// </summary>
    public static class mlpCharacterSkillsData
    {
        private static readonly mlpCharacterSkillDefinition[] Skills =
        {
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.SoulReap,
                3,
                "DASH STEAL",
                mlpAssets.Images.SkillIcons.Reaper,
                mlpAssets.Images.SkillIcons.ReaperMask,
                "DASH STEAL!",
                "BALL STOLEN",
                new Color32(0xB9, 0xFF, 0xE4, 0xFF),
                new Color32(0x4F, 0x86, 0x79, 0xFF),
                new Color32(0xEC, 0xFF, 0xF7, 0xFF)),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.ReboundMagnet,
                2,
                "REBOUND MAGNET",
                mlpAssets.Images.SkillIcons.GhostClown,
                mlpAssets.Images.SkillIcons.GhostClownMask,
                "REBOUND MAGNET!",
                "REBOUND SECURED",
                new Color32(0xFF, 0xD0, 0x67, 0xFF),
                new Color32(0x65, 0xD3, 0xD4, 0xFF),
                new Color32(0xFF, 0xF4, 0xCC, 0xFF),
                effectDuration: 1.55f),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.GhostSail,
                1,
                "HOOP SHIELD",
                mlpAssets.Images.SkillIcons.SkullPirate,
                mlpAssets.Images.SkillIcons.SkullPirateMask,
                "HOOP SHIELD!",
                "SHIELD BLOCK",
                new Color32(0xB8, 0xFF, 0xD5, 0xFF),
                new Color32(0x5C, 0x86, 0x7D, 0xFF),
                new Color32(0xF7, 0xE2, 0x9F, 0xFF)),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.BloodMoonBlink,
                2,
                "BLINK DUNK",
                mlpAssets.Images.SkillIcons.Vampire,
                mlpAssets.Images.SkillIcons.VampireMask,
                "BLINK DUNK!",
                "BLINK DUNK",
                new Color32(0xFF, 0x8B, 0x9D, 0xFF),
                new Color32(0x7D, 0x19, 0x31, 0xFF),
                new Color32(0xFF, 0xF0, 0xF4, 0xFF)),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.WaxOverdrive,
                3,
                "SPEED BOOST",
                mlpAssets.Images.SkillIcons.Candleman,
                mlpAssets.Images.SkillIcons.CandlemanMask,
                "SPEED BOOST!",
                "FAST BREAK",
                new Color32(0xFF, 0xB1, 0x45, 0xFF),
                new Color32(0x8C, 0x45, 0x17, 0xFF),
                new Color32(0xFF, 0xEC, 0xB0, 0xFF),
                effectDuration: 3.5f,
                moveSpeedMultiplier: 1.35f),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.CarnivalJackpot,
                0,
                "NEXT SCORE +2",
                mlpAssets.Images.SkillIcons.Scarecrow,
                mlpAssets.Images.SkillIcons.ScarecrowMask,
                "NEXT SCORE +2!",
                "+2 SCORE",
                new Color32(0xE0, 0x9D, 0x2A, 0xFF),
                new Color32(0x6E, 0x5A, 0x2C, 0xFF),
                new Color32(0xF4, 0xDE, 0x99, 0xFF)),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.BadLuck,
                1,
                "FREEZE 2 SEC",
                mlpAssets.Images.SkillIcons.Witch,
                mlpAssets.Images.SkillIcons.WitchMask,
                "FREEZE 2 SEC!",
                "FROZEN",
                new Color32(0x9C, 0xE8, 0xFF, 0xFF),
                new Color32(0x25, 0x3A, 0x78, 0xFF),
                new Color32(0xEA, 0xFB, 0xFF, 0xFF),
                effectDuration: 2f),
            new mlpCharacterSkillDefinition(
                mlpCharacterSkillType.SureBlock,
                3,
                "SURE BLOCK",
                mlpAssets.Images.SkillIcons.BlackCat,
                mlpAssets.Images.SkillIcons.BlackCatMask,
                "SURE BLOCK!",
                "SHOT BLOCKED",
                new Color32(0xB8, 0x98, 0xFF, 0xFF),
                new Color32(0x22, 0x22, 0x44, 0xFF),
                new Color32(0xFF, 0xD9, 0x74, 0xFF))
        };

        /// <summary>
        /// 获取指定角色 ID 的技能定义。如果 ID 超出范围，回退到第一个技能。
        /// </summary>
        /// <param name="characterId">要查找的角色 ID。</param>
        /// <returns>该角色的技能定义。</returns>
        public static mlpCharacterSkillDefinition Get(int characterId)
        {
            return characterId >= 0 && characterId < Skills.Length
                ? Skills[characterId]
                : Skills[0];
        }
    }
}
