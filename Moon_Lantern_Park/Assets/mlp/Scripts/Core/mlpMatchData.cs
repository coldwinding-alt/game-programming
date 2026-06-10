// 比赛配置数据和物品清单
// 定义一场比赛需要的配置信息：双方角色、难度、游戏模式。还包含 mlpInventory 类，用来保存玩家的进度、选择和解锁状态。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// AI 难度等级：全游戏只保留 Easy、Normal、Hard、Hell 四档。
    /// 这些枚举值只表示玩家选择的难度档位，不再表示锦标赛轮次、冒险关卡编号等递增进度。
    /// 具体会转换成哪个 AI 技能索引，由 mlpMatchData.GetOpponentSkillForDifficulty 统一决定。
    /// </summary>
    public enum mlpAiDifficulty
    {
        /// <summary>简单：给新玩家和练习使用，AI 技能值最低。</summary>
        Easy,

        /// <summary>普通：默认难度，适合作为标准体验。</summary>
        Normal,

        /// <summary>困难：AI 更主动、更稳定，但不会因为赛段继续变强。</summary>
        Hard,

        /// <summary>地狱：最高难度，仍保留 Hell 专属强化，但基础技能值固定。</summary>
        Hell
    }

    /// <summary>
    /// 参与模式：单人、双人、训练、教程。用来区分当前是几个人在玩、什么目的。
    /// </summary>
    public enum mlpParticipantMode
    {
        OnePlayer,
        TwoPlayers,
        Training,
        Tutorial
    }

    /// <summary>
    /// 会话模式：当前正在进行的游戏类型（无、快速比赛、冒险、锦标赛、训练、教程）。
    /// </summary>
    public enum mlpSessionMode
    {
        None,
        QuickMatch,
        Adventure,
        Tournament,
        Training,
        Tutorial
    }

    /// <summary>
    /// 篮球皮肤主题：经典、幽灵绿、南瓜余烬、月光紫等不同外观的篮球。
    /// </summary>
    public enum mlpBallTheme
    {
        ClassicOriginal,
        GhoulGreen,
        PumpkinEmber,
        MoonlitViolet,
        JackOLantern,
        EvilEye,
        Cursed8Ball,
        CandySwirl
    }

    /// <summary>
    /// 篮球皮肤选择：包含"随机"选项和所有具体皮肤，用于菜单中的选择界面。
    /// </summary>
    public enum mlpBallSelection
    {
        Random,
        ClassicOriginal,
        GhoulGreen,
        PumpkinEmber,
        MoonlitViolet,
        JackOLantern,
        EvilEye,
        Cursed8Ball,
        CandySwirl
    }

    /// <summary>
    /// 篮球皮肤目录：管理所有篮球皮肤的切换、解析和标签显示。把选择界面上的选项转换成实际使用的皮肤。
    /// </summary>
    public static class mlpBallCatalog
    {
        private static readonly mlpBallSelection[] OrderedSelections =
        {
            mlpBallSelection.Random,
            mlpBallSelection.ClassicOriginal,
            mlpBallSelection.GhoulGreen,
            mlpBallSelection.PumpkinEmber,
            mlpBallSelection.MoonlitViolet,
            mlpBallSelection.JackOLantern,
            mlpBallSelection.EvilEye,
            mlpBallSelection.Cursed8Ball,
            mlpBallSelection.CandySwirl
        };

        private static readonly mlpBallTheme[] ConcreteThemes =
        {
            mlpBallTheme.ClassicOriginal,
            mlpBallTheme.GhoulGreen,
            mlpBallTheme.PumpkinEmber,
            mlpBallTheme.MoonlitViolet,
            mlpBallTheme.JackOLantern,
            mlpBallTheme.EvilEye,
            mlpBallTheme.Cursed8Ball,
            mlpBallTheme.CandySwirl
        };

        /// <summary>
        /// 在有序列表中向前或向后切换球皮选择，到末尾时自动循环回到开头。
        /// </summary>
        /// <param name="current">当前选中的球皮。</param>
        /// <param name="direction">正数表示向后切换，负数表示向前切换。</param>
        /// <returns>指定方向上的下一个球皮选项。</returns>
        public static mlpBallSelection StepSelection(mlpBallSelection current, int direction)
        {
            var index = Array.IndexOf(OrderedSelections, current);
            if (index < 0)
            {
                index = 0;
            }

            index += direction >= 0 ? 1 : -1;
            if (index < 0)
            {
                index = OrderedSelections.Length - 1;
            }
            else if (index >= OrderedSelections.Length)
            {
                index = 0;
            }

            return OrderedSelections[index];
        }

        /// <summary>
        /// 将球皮选项转换为具体的主题。如果选择了"随机"，则随机选取一个非随机主题。
        /// </summary>
        /// <param name="selection">要解析的球皮选项。</param>
        /// <returns>用于渲染的具体球皮主题。</returns>
        public static mlpBallTheme ResolveTheme(mlpBallSelection selection)
        {
            return selection == mlpBallSelection.Random
                ? ConcreteThemes[UnityEngine.Random.Range(0, ConcreteThemes.Length)]
                : ToTheme(selection);
        }

        /// <summary>
        /// 返回在 UI 预览中显示的主题。选择"随机"时，始终显示经典主题作为占位。
        /// </summary>
        /// <param name="selection">要预览的球皮选项。</param>
        /// <returns>在选择界面中显示的主题。</returns>
        public static mlpBallTheme PreviewTheme(mlpBallSelection selection)
        {
            return selection == mlpBallSelection.Random
                ? mlpBallTheme.ClassicOriginal
                : ToTheme(selection);
        }

        /// <summary>
        /// 将非随机的球皮选项直接映射为对应的球皮主题。
        /// </summary>
        /// <param name="selection">球皮选项（不能是 Random）。</param>
        /// <returns>匹配的球皮主题。</returns>
        public static mlpBallTheme ToTheme(mlpBallSelection selection)
        {
            return selection switch
            {
                mlpBallSelection.GhoulGreen => mlpBallTheme.GhoulGreen,
                mlpBallSelection.PumpkinEmber => mlpBallTheme.PumpkinEmber,
                mlpBallSelection.MoonlitViolet => mlpBallTheme.MoonlitViolet,
                mlpBallSelection.JackOLantern => mlpBallTheme.JackOLantern,
                mlpBallSelection.EvilEye => mlpBallTheme.EvilEye,
                mlpBallSelection.Cursed8Ball => mlpBallTheme.Cursed8Ball,
                mlpBallSelection.CandySwirl => mlpBallTheme.CandySwirl,
                _ => mlpBallTheme.ClassicOriginal
            };
        }

        /// <summary>
        /// 返回球皮选项的短标签文本（如 "GHOUL"、"EMBER"、"RANDOM"）。
        /// </summary>
        /// <param name="selection">要获取标签的球皮选项。</param>
        /// <returns>在 UI 中显示的短大写字符串。</returns>
        public static string Label(mlpBallSelection selection)
        {
            return selection switch
            {
                mlpBallSelection.Random => "RANDOM",
                mlpBallSelection.GhoulGreen => "GHOUL",
                mlpBallSelection.PumpkinEmber => "EMBER",
                mlpBallSelection.MoonlitViolet => "VIOLET",
                mlpBallSelection.JackOLantern => "JACK",
                mlpBallSelection.EvilEye => "EYE",
                mlpBallSelection.Cursed8Ball => "8-BALL",
                mlpBallSelection.CandySwirl => "SWIRL",
                _ => "CLASSIC"
            };
        }
    }

    /// <summary>
    /// 比赛配置数据：存储一场比赛的所有设置——双方角色、难度、脑控制方式、技能等级、比分。不同模式（快速、训练、锦标赛等）通过不同的方法来配置。
    /// </summary>
    public sealed class mlpMatchData
    {
        public bool Restarted;
        public int FirstCharacterId;
        public int MatchMode;
        public mlpBallTheme BallTheme;
        public int[] CharacterIds = new int[2];
        public string[][] Pb = { new string[0], new string[0] };
        public int[][] Skills = { new int[0], new int[0] };
        public int[] MatchScore = { 0, 0 };

        /// <summary>
        /// 创建比赛数据，默认初始化角色 ID 和随机球皮主题。
        /// </summary>
        /// <param name="local">本地游戏传入 true（默认使用角色索引 0），联网传入 false。</param>
        public mlpMatchData(bool local)
        {
            FirstCharacterId = mlpPlayersData.SanitizeCharacterId(local ? 0 : 1);
            BaseInit();
            ResetPartly();
            RollBallTheme();
        }

        /// <summary>
        /// 将双方队伍的角色 ID 重置为默认值。
        /// </summary>
        public void ResetData()
        {
            CharacterIds = new[] { mlpPlayersData.SanitizeCharacterId(0), mlpPlayersData.SanitizeCharacterId(1, 0) };
        }

        /// <summary>
        /// 重置比赛模式、玩家脑字符串、技能等级和比分。不会更改角色 ID 和球皮主题。
        /// </summary>
        public void ResetPartly()
        {
            MatchMode = 0;
            Pb = new[] { new string[0], new string[0] };
            Skills = new[] { new int[0], new int[0] };
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// 将所有数据重置为默认值：角色 ID、脑字符串、技能等级、比分和比赛模式。
        /// </summary>
        public void ResetAll()
        {
            ResetData();
            ResetPartly();
        }

        /// <summary>
        /// 设置默认的比赛配置：双方队伍角色、默认脑字符串和默认技能等级。
        /// </summary>
        public void BaseInit()
        {
            MatchMode = 0;
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(FirstCharacterId),
                mlpPlayersData.StepCharacterId(FirstCharacterId, 1)
            };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetQuickMatchOpponentSkill(mlpAiDifficulty.Normal) } };
        }

        /// <summary>
        /// 将双方队伍的比分重置为零。
        /// </summary>
        public void ResetScore()
        {
            MatchScore = new[] { 0, 0 };
        }

        /// <summary>
        /// 配置一场快速比赛：随机选取对手，设置人机对战的脑字符串，并解析球皮主题。
        /// </summary>
        /// <param name="playerCharacterId">玩家选择的角色 ID。</param>
        /// <param name="difficulty">AI 难度等级。</param>
        /// <param name="ballSelection">使用的球皮（Random 表示随机选取）。</param>
        public void StartQuickMatch(int playerCharacterId, mlpAiDifficulty difficulty, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            // 1. 重置所有比赛数据，解析球皮主题
            ResetAll();
            ResolveBallSelection(ballSelection);
            // 2. 验证玩家角色，随机选一个不同的对手
            var playerId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
            var excluded = new List<int> { playerId };
            var opponentId = mlpPlayersData.GetRandomCharacterId(excluded);
            // 3. 根据难度获取对手的 AI 技能等级
            var opponentSkill = GetQuickMatchOpponentSkill(difficulty);

            // 4. 配置双方角色、脑控制方式和技能等级
            CharacterIds = new[] { playerId, opponentId };
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        /// <summary>
        /// 配置一场本地双人对战比赛，使用两个不同的角色和玩家脑字符串。
        /// </summary>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartQuickLocalVersusMatch(mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var left = mlpPlayersData.SanitizeCharacterId(0);
            var right = mlpPlayersData.StepCharacterId(left, 1);

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// 配置一场训练赛，只有玩家角色，没有对手。
        /// </summary>
        /// <param name="characterId">玩家选择的角色 ID。</param>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartTraining(int characterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);

            CharacterIds = new[] { resolvedCharacterId, resolvedCharacterId };
            Pb = new[] { new[] { "P0" }, new string[0] };
            Skills = new[] { new[] { 0 }, new int[0] };
        }

        /// <summary>
        /// 配置一场教程比赛，使用指定的对手角色和教程专用脑字符串。
        /// </summary>
        /// <param name="characterId">玩家选择的角色 ID。</param>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartTutorial(int characterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            ResetAll();
            ResolveBallSelection(ballSelection);
            var resolvedCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
            var opponentCharacterId = mlpPlayersData.SanitizeCharacterId(7, resolvedCharacterId);

            CharacterIds = new[] { resolvedCharacterId, opponentCharacterId };
            Pb = new[] { new[] { "P0" }, new[] { "T0" } };
            Skills = new[] { new[] { 0 }, new[] { mlpAISkillsData.NormalSkillIndex } };
        }

        /// <summary>
        /// 开始一场随机比赛。目前直接委托给 StartQuickMatch 方法处理。
        /// </summary>
        /// <param name="playerCharacterId">玩家选择的角色 ID。</param>
        /// <param name="difficulty">AI 难度等级。</param>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartRandomMatch(int playerCharacterId, mlpAiDifficulty difficulty, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            StartQuickMatch(playerCharacterId, difficulty, ballSelection);
        }

        /// <summary>
        /// 使用当前存储的角色 ID 配置一场本地双人对战比赛。
        /// </summary>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartPlayers2Match(mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            var left = mlpPlayersData.SanitizeCharacterId(CharacterIds[0]);
            var right = mlpPlayersData.SanitizeCharacterId(CharacterIds[1], mlpPlayersData.StepCharacterId(left, 1));

            CharacterIds = new[] { left, right };
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// 配置锦标赛中的下一场比赛，使用锦标赛当前对手和固定四档难度。
        /// </summary>
        /// <param name="tournament">当前进行中的锦标赛数据。</param>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartTournamentMatch(mlpTournamentData tournament, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            // 1. 重置所有比赛数据
            ResetAll();
            // 2. 锦标赛无效、已完成或无待打比赛时直接返回
            if (tournament == null || !tournament.Active || tournament.Completed || !tournament.HasPendingPlayerMatch)
            {
                return;
            }

            // 3. 解析球皮，设置双方角色 ID
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(tournament.PlayerCharacterId),
                mlpPlayersData.SanitizeCharacterId(tournament.CurrentOpponentCharacterId)
            };

            // 4. 根据锦标赛难度计算对手技能
            var opponentSkill = GetTournamentOpponentSkill(tournament);

            // 5. 配置脑控制方式和技能等级
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { opponentSkill } };
        }

        public void StartAdventureMatch(mlpAdventureData adventure)
        {
            StartAdventureMatch(adventure, mlpAiDifficulty.Normal);
        }

        /// <summary>
        /// 配置冒险模式中的下一场比赛。
        /// </summary>
        /// <param name="adventure">当前进行中的冒险数据。</param>
        /// <param name="difficulty">玩家选择的固定四档 AI 难度。</param>
        public void StartAdventureMatch(mlpAdventureData adventure, mlpAiDifficulty difficulty)
        {
            // 1. 重置所有比赛数据
            ResetAll();
            // 2. 冒险无效、已完成或无待打比赛时直接返回
            if (adventure == null || !adventure.Active || adventure.Completed || !adventure.HasPendingPlayerMatch)
            {
                return;
            }

            // 3. 获取当前关卡定义，解析关卡指定的球皮
            var level = adventure.CurrentLevel;
            MatchMode = 0;
            ResolveBallSelection(level.BallSelection);
            // 4. 设置玩家和守卫者的角色 ID
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(adventure.PlayerCharacterId),
                mlpPlayersData.SanitizeCharacterId(level.WardenCharacterId)
            };
            // 5. 配置脑控制方式和对手技能等级
            Pb = new[] { new[] { "P0" }, new[] { "B0" } };
            Skills = new[] { new[] { 0 }, new[] { GetAdventureOpponentSkill(level, difficulty) } };
        }

        /// <summary>
        /// 配置一场双人对战比赛，为每位玩家指定各自的角色。
        /// </summary>
        /// <param name="leftCharacterId">左侧玩家的角色 ID。</param>
        /// <param name="rightCharacterId">右侧玩家的角色 ID。</param>
        /// <param name="ballSelection">使用的球皮。</param>
        public void StartSelectedTwoPlayerMatch(int leftCharacterId, int rightCharacterId, mlpBallSelection ballSelection = mlpBallSelection.Random)
        {
            // 1. 重置数据，解析球皮
            ResetAll();
            MatchMode = 0;
            ResolveBallSelection(ballSelection);
            // 2. 验证双方角色 ID（确保不重复）
            CharacterIds = new[]
            {
                mlpPlayersData.SanitizeCharacterId(leftCharacterId),
                mlpPlayersData.SanitizeCharacterId(rightCharacterId, leftCharacterId)
            };
            // 3. 双方都是人类玩家（P1 和 P2），无 AI 技能
            Pb = new[] { new[] { "P1" }, new[] { "P2" } };
            Skills = new[] { new[] { 0 }, new[] { 0 } };
        }

        /// <summary>
        /// 比较双方比分以确定获胜方。
        /// </summary>
        /// <returns>左侧获胜返回 -1，右侧获胜返回 1，平局返回 0。</returns>
        public int WhoWins()
        {
            return MatchScore[0] > MatchScore[1] ? -1 : MatchScore[0] < MatchScore[1] ? 1 : 0;
        }

        /// <summary>
        /// 将玩家选择的四档难度映射为 AI 技能索引。
        /// </summary>
        /// <param name="difficulty">选择的 AI 难度。</param>
        /// <returns>四档固定技能索引：0 = Easy，1 = Normal，2 = Hard，3 = Hell。</returns>
        private static int GetOpponentSkillForDifficulty(mlpAiDifficulty difficulty)
        {
            // 这里是所有单人模式获取 AI 技能索引的唯一入口。
            // 返回值只会是 0、1、2、3 四个固定档位：
            // 0 = Easy：基础行动完整，但反应、进攻和防守更宽松。
            // 1 = Normal：默认体验，适合作为标准比赛强度。
            // 2 = Hard：更积极、更稳定，但不会因关卡或赛段继续递增。
            // 3 = Hell：四档中最高基础强度；Hell 专属额外强化仍由难度调校处理。
            // 不要在快速赛、随机赛、冒险或锦标赛里再单独叠加轮次/关卡偏移，
            // 否则就会重新变成隐藏多档难度，和当前固定四档设计相冲突。
            return mlpAISkillsData.GetSkillIndex(difficulty);
        }

        /// <summary>
        /// 快速赛也使用统一的四档难度映射。
        /// </summary>
        private static int GetQuickMatchOpponentSkill(mlpAiDifficulty difficulty)
        {
            return GetOpponentSkillForDifficulty(difficulty);
        }

        private static int GetAdventureOpponentSkill(mlpAdventureLevelDefinition level, mlpAiDifficulty difficulty)
        {
            // 1. 冒险关卡保留特殊规则和剧情，但 AI 技能统一由难度决定
            // 2. 不再读取 level.OpponentSkill，不再随关卡进度递增
            return GetOpponentSkillForDifficulty(difficulty);
        }

        /// <summary>
        /// 根据锦标赛选择的固定四档难度计算 AI 技能等级。
        /// </summary>
        /// <param name="tournament">当前进行中的锦标赛数据。</param>
        /// <returns>该难度固定对应的技能索引（经范围限制）。</returns>
        private static int GetTournamentOpponentSkill(mlpTournamentData tournament)
        {
            // 1. 锦标赛数据为空时返回默认技能 0
            if (tournament == null)
            {
                return 0;
            }

            // 2. 锦标赛保留赛制流程（常规赛、半决赛、决赛等），但 AI 技能统一由难度决定
            // 3. 赛段只决定对阵和排名，不影响 AI 技能值
            // 4. 从第一轮到决赛都保持玩家选择的同一个难度
            return GetOpponentSkillForDifficulty(tournament.Difficulty);
        }

        /// <summary>
        /// 为当前比赛随机选取一个新的球皮主题。
        /// </summary>
        public void RollBallTheme()
        {
            BallTheme = mlpBallCatalog.ResolveTheme(mlpBallSelection.Random);
        }

        /// <summary>
        /// 将球皮选项转换为具体的球皮主题并保存。
        /// </summary>
        /// <param name="ballSelection">选择的球皮（可以是 Random）。</param>
        private void ResolveBallSelection(mlpBallSelection ballSelection)
        {
            BallTheme = mlpBallCatalog.ResolveTheme(ballSelection);
        }
    }

    /// <summary>
    /// 全局物品清单（单例）：保存玩家的所有选择和进度——当前游戏模式、选中的角色和篮球皮肤、难度、冒险/锦标赛的进行状态。是整个游戏状态的中央存储。
    /// </summary>
    public sealed class mlpInventory
    {
        private static mlpInventory instance;

        public static mlpInventory Instance => instance ?? (instance = new mlpInventory());

        public int GameMode;
        public mlpMatchData MatchData;
        public mlpAdventureData Adventure;
        public mlpTournamentData Tournament;
        public bool FirstRun = true;
        public bool FirstRun2 = true;
        public bool MatchPrepared;
        public mlpAiDifficulty Difficulty;
        public mlpParticipantMode ParticipantMode;
        public mlpSessionMode SessionMode;
        public mlpTutorialNextAction PendingTutorialNextAction;
        public int SelectedQuickCharacterId;
        public int SelectedTournamentCharacterId;
        public int SelectedTrainingCharacterId;
        public mlpBallSelection SelectedQuickBallSelection;
        public mlpBallSelection SelectedTournamentBallSelection;
        public mlpBallSelection SelectedTrainingBallSelection;
        public mlpBallSelection SelectedVersusBallSelection;

        /// <summary>
        /// 初始化全局物品栏，设置默认的游戏模式、难度、角色选择和球皮选择。
        /// </summary>
        private mlpInventory()
        {
            // 1. 设置默认游戏模式和参与模式
            GameMode = 1;
            ParticipantMode = mlpParticipantMode.OnePlayer;
            SessionMode = mlpSessionMode.None;
            // 2. 创建比赛数据、冒险数据和锦标赛数据实例
            MatchData = new mlpMatchData(true);
            Adventure = new mlpAdventureData();
            Tournament = new mlpTournamentData();
            // 3. 设置默认难度和教程状态
            MatchData.MatchMode = 0;
            Difficulty = mlpAiDifficulty.Normal;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 4. 各模式默认选中第一个角色
            SelectedQuickCharacterId = MatchData.FirstCharacterId;
            SelectedTournamentCharacterId = MatchData.FirstCharacterId;
            SelectedTrainingCharacterId = MatchData.FirstCharacterId;
            // 5. 各模式默认使用经典球皮
            SelectedQuickBallSelection = mlpBallSelection.ClassicOriginal;
            SelectedTournamentBallSelection = mlpBallSelection.ClassicOriginal;
            SelectedTrainingBallSelection = mlpBallSelection.ClassicOriginal;
            SelectedVersusBallSelection = mlpBallSelection.ClassicOriginal;
        }

        public string DifficultyLabel => Difficulty switch
        {
            mlpAiDifficulty.Easy => "AI: EASY",
            mlpAiDifficulty.Hard => "AI: HARD",
            mlpAiDifficulty.Hell => "AI: HELL",
            _ => "AI: NORMAL"
        };

        public bool IsTournamentActive => SessionMode == mlpSessionMode.Tournament && Tournament.Active;
        public bool IsAdventureActive => SessionMode == mlpSessionMode.Adventure && Adventure.Active;

        /// <summary>
        /// 按顺序循环切换难度等级：简单 -> 普通 -> 困难 -> 地狱 -> 简单。
        /// </summary>
        public void ToggleDifficulty()
        {
            Difficulty = Difficulty switch
            {
                mlpAiDifficulty.Easy => mlpAiDifficulty.Normal,
                mlpAiDifficulty.Normal => mlpAiDifficulty.Hard,
                mlpAiDifficulty.Hard => mlpAiDifficulty.Hell,
                _ => mlpAiDifficulty.Easy
            };
        }

        /// <summary>
        /// 设置当前是单人、双人、训练还是教程模式，同时更新对应的会话模式。
        /// </summary>
        /// <param name="participantMode">要设置的参与模式。</param>
        public void SetParticipantMode(mlpParticipantMode participantMode)
        {
            ParticipantMode = participantMode;
            if (participantMode == mlpParticipantMode.Training)
            {
                SessionMode = mlpSessionMode.Training;
            }
            else if (participantMode == mlpParticipantMode.Tutorial)
            {
                SessionMode = mlpSessionMode.Tutorial;
            }
        }

        /// <summary>
        /// 保存玩家在快速比赛模式下选择的角色。
        /// </summary>
        /// <param name="characterId">快速比赛使用的角色 ID。</param>
        public void SetQuickSelection(int characterId)
        {
            SelectedQuickCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// 保存玩家在锦标赛模式下选择的角色。
        /// </summary>
        /// <param name="characterId">锦标赛使用的角色 ID。</param>
        public void SetTournamentSelection(int characterId)
        {
            SelectedTournamentCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// 保存玩家在训练模式下选择的角色。
        /// </summary>
        /// <param name="characterId">训练模式使用的角色 ID。</param>
        public void SetTrainingSelection(int characterId)
        {
            SelectedTrainingCharacterId = mlpPlayersData.SanitizeCharacterId(characterId);
        }

        /// <summary>
        /// 保存快速比赛模式下选择的球皮。
        /// </summary>
        /// <param name="selection">球皮选项。</param>
        public void SetQuickBallSelection(mlpBallSelection selection)
        {
            SelectedQuickBallSelection = selection;
        }

        /// <summary>
        /// 保存锦标赛模式下选择的球皮。
        /// </summary>
        /// <param name="selection">球皮选项。</param>
        public void SetTournamentBallSelection(mlpBallSelection selection)
        {
            SelectedTournamentBallSelection = selection;
        }

        /// <summary>
        /// 保存训练模式下选择的球皮。
        /// </summary>
        /// <param name="selection">球皮选项。</param>
        public void SetTrainingBallSelection(mlpBallSelection selection)
        {
            SelectedTrainingBallSelection = selection;
        }

        /// <summary>
        /// 保存双人对战模式下选择的球皮。
        /// </summary>
        /// <param name="selection">球皮选项。</param>
        public void SetVersusBallSelection(mlpBallSelection selection)
        {
            SelectedVersusBallSelection = selection;
        }

        /// <summary>
        /// 重置当前进行中的冒险/锦标赛，然后使用当前设置准备一场快速比赛。
        /// </summary>
        public void StartQuickGame()
        {
            // 1. 重置冒险和锦标赛状态
            Adventure.Reset();
            Tournament.Reset();
            // 2. 设置会话为快速比赛模式
            SessionMode = mlpSessionMode.QuickMatch;
            MatchPrepared = true;
            ParticipantMode = mlpParticipantMode.OnePlayer;
            GameMode = mlpGameModeIds.QuickMatch;
            // 3. 清除教程待办状态
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 4. 配置比赛数据
            MatchData.MatchMode = 0;
            MatchData.StartQuickMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
        }

        /// <summary>
        /// 从主菜单开始一场单人比赛。重置冒险/锦标赛状态，使用随机对手。
        /// </summary>
        public void StartOnePlayer()
        {
            ParticipantMode = mlpParticipantMode.OnePlayer;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            GameMode = mlpGameModeIds.RandomQuick;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartRandomMatch(SelectedQuickCharacterId, Difficulty, SelectedQuickBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// 从主菜单开始一场本地双人对战比赛。
        /// </summary>
        public void StartTwoPlayers()
        {
            ParticipantMode = mlpParticipantMode.TwoPlayers;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            GameMode = mlpGameModeIds.TwoPlayers;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.MatchMode = 0;
            MatchData.StartPlayers2Match(SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// 开始一场双人对战比赛，为每位玩家明确指定角色。
        /// </summary>
        /// <param name="leftCharacterId">左侧玩家的角色 ID。</param>
        /// <param name="rightCharacterId">右侧玩家的角色 ID。</param>
        public void StartTwoPlayerVersus(int leftCharacterId, int rightCharacterId)
        {
            ParticipantMode = mlpParticipantMode.TwoPlayers;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.QuickMatch;
            GameMode = mlpGameModeIds.TwoPlayers;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.StartSelectedTwoPlayerMatch(leftCharacterId, rightCharacterId, SelectedVersusBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// 使用当前选择的训练角色和球皮开始一场训练赛。
        /// </summary>
        public void StartTraining()
        {
            ParticipantMode = mlpParticipantMode.Training;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.Training;
            GameMode = mlpGameModeIds.Training;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.StartTraining(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// 使用当前选择的训练角色和球皮开始一场教程。
        /// </summary>
        public void StartTutorial()
        {
            ParticipantMode = mlpParticipantMode.Tutorial;
            Adventure.Reset();
            Tournament.Reset();
            SessionMode = mlpSessionMode.Tutorial;
            GameMode = mlpGameModeIds.Tutorial;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            MatchData.StartTutorial(SelectedTrainingCharacterId, SelectedTrainingBallSelection);
            MatchPrepared = true;
        }

        /// <summary>
        /// 使用选定的角色和难度创建一场新锦标赛，并准备第一场比赛。
        /// </summary>
        /// <returns>锦标赛创建成功时返回 true。</returns>
        public bool BeginTournament()
        {
            // 1. 设置单人模式，重置冒险，切换到锦标赛会话
            ParticipantMode = mlpParticipantMode.OnePlayer;
            Adventure.Reset();
            SessionMode = mlpSessionMode.Tournament;
            GameMode = mlpGameModeIds.RandomQuick;
            // 2. 清除教程待办状态
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            // 3. 创建锦标赛，失败则返回
            if (!Tournament.Create(SelectedTournamentCharacterId, Difficulty))
            {
                MatchPrepared = false;
                return false;
            }

            // 4. 有待打比赛时配置第一场比赛
            MatchPrepared = false;
            if (Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            return true;
        }

        /// <summary>
        /// 将进行中的锦标赛推进到决赛阶段，如有待进行的比赛则准备下一场。
        /// </summary>
        /// <returns>有决赛比赛待进行时返回 true。</returns>
        public bool BeginTournamentFinals()
        {
            // 1. 锦标赛未激活时返回失败
            if (!IsTournamentActive)
            {
                return false;
            }

            // 2. 推进锦标赛到决赛阶段
            Tournament.BeginFinals();
            // 3. 有待打比赛时配置比赛
            MatchPrepared = false;
            if (Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            // 4. 返回是否有决赛比赛需要打
            return Tournament.HasPendingPlayerMatch;
        }

        /// <summary>
        /// 记录当前比赛结果并推进锦标赛对阵表。如有下一场比赛则进行准备。
        /// </summary>
        /// <returns>锦标赛已结束时返回 true。</returns>
        public bool AdvanceTournament()
        {
            // 1. 锦标赛未激活时返回失败
            if (!IsTournamentActive)
            {
                return false;
            }

            // 2. 将当前比赛结果提交给锦标赛
            Tournament.ApplyCurrentMatchResult(MatchData.MatchScore[0], MatchData.MatchScore[1]);
            // 3. 未完成且有下一场比赛时，配置下一场
            MatchPrepared = false;
            if (!Tournament.Completed && Tournament.HasPendingPlayerMatch)
            {
                MatchData.StartTournamentMatch(Tournament, SelectedTournamentBallSelection);
                MatchPrepared = true;
            }

            // 4. 返回锦标赛是否已完成
            return Tournament.Completed;
        }

        /// <summary>
        /// 取消当前锦标赛并清除所有锦标赛状态。
        /// </summary>
        public void AbandonTournament()
        {
            Tournament.Reset();
            SessionMode = mlpSessionMode.None;
            MatchPrepared = false;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
        }

        public void BeginAdventure(int playerCharacterId)
        {
            ParticipantMode = mlpParticipantMode.OnePlayer;
            Tournament.Reset();
            SessionMode = mlpSessionMode.Adventure;
            GameMode = mlpGameModeIds.RandomQuick;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
            Adventure.Create(playerCharacterId);
            MatchPrepared = false;
        }

        public bool StartAdventureLevel(int levelIndex, int playerCharacterId)
        {
            // 1. 验证角色 ID
            var resolvedPlayerCharacterId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);
            // 2. 如果冒险未激活、已完成或角色不匹配，重新开始冒险
            if (!IsAdventureActive || Adventure.Completed || Adventure.PlayerCharacterId != resolvedPlayerCharacterId)
            {
                BeginAdventure(resolvedPlayerCharacterId);
            }

            // 3. 选择关卡，失败则返回
            if (!Adventure.SelectLevel(levelIndex))
            {
                return false;
            }

            // 4. 配置冒险比赛数据并标记已准备好
            MatchData.StartAdventureMatch(Adventure, Difficulty);
            MatchPrepared = true;
            // 5. 验证比赛数据完整性
            return MatchData.Pb != null && MatchData.Pb.Length >= 2 && MatchData.Pb[1].Length > 0;
        }

        public bool RestartAdventureLevel()
        {
            if (!IsAdventureActive || Adventure.Completed || !Adventure.HasPendingPlayerMatch)
            {
                return false;
            }

            MatchData.StartAdventureMatch(Adventure, Difficulty);
            MatchPrepared = true;
            return true;
        }

        public void AdvanceAdventure(bool playerWon)
        {
            if (!IsAdventureActive)
            {
                return;
            }

            Adventure.ApplyCurrentMatchResult(playerWon);
            MatchPrepared = false;
        }

        public void AbandonAdventure()
        {
            Adventure.Reset();
            SessionMode = mlpSessionMode.None;
            MatchPrepared = false;
            PendingTutorialNextAction = mlpTutorialNextAction.None;
        }
    }
}
