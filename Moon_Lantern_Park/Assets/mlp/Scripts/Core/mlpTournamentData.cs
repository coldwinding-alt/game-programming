// 锦标赛赛制数据和对阵管理 / 管理 4 人锦标赛的完整流程：随机分组、半决赛、决赛、记录每场比赛结果、计算排名和战绩。还负责保存和读取锦标赛进度，让玩家可以中途退出再回来继续。

using System.Collections.Generic;
using UnityEngine;

namespace mlp
{
    /// <summary>
    /// 锦标赛阶段：无、常规赛、半决赛、三四名决赛、决赛、已完成。标识锦标赛进行到哪一步。
    /// </summary>
    public enum mlpTournamentStage
    {
        None,
        RegularSeason,
        SemiFinal,
        ThirdPlace,
        Final,
        Complete
    }

    /// <summary>
    /// 锦标赛单场比赛结果：记录双方角色、比分和获胜方。平局时左侧自动胜出。
    /// </summary>
    public sealed class mlpTournamentMatchResult
    {
        public int LeftCharacterId { get; private set; } = -1;
        public int RightCharacterId { get; private set; } = -1;
        public int LeftScore { get; private set; }
        public int RightScore { get; private set; }
        public int WinnerCharacterId { get; private set; } = -1;
        public bool Completed { get; private set; }

        /// <summary>
        /// 重置本场比赛结果，并可选地指定双方参赛角色。
        /// </summary>
        public void Reset(int leftCharacterId = -1, int rightCharacterId = -1)
        {
            LeftCharacterId = leftCharacterId;
            RightCharacterId = rightCharacterId;
            LeftScore = 0;
            RightScore = 0;
            WinnerCharacterId = -1;
            Completed = false;
        }

        /// <summary>
        /// 记录最终比分并判定获胜方；平局时左侧胜出。
        /// </summary>
        public void Complete(int leftScore, int rightScore)
        {
            if (LeftCharacterId < 0 || RightCharacterId < 0)
            {
                return;
            }

            if (leftScore == rightScore)
            {
                leftScore++;
            }

            LeftScore = leftScore;
            RightScore = rightScore;
            WinnerCharacterId = leftScore > rightScore ? LeftCharacterId : RightCharacterId;
            Completed = true;
        }
    }

    /// <summary>
    /// 锦标赛排名记录：记录某个角色在分区中的胜负场次、总得分和净胜分，用于排名。
    /// </summary>
    public sealed class mlpTournamentStandingEntry
    {
        public int CharacterId { get; private set; } = -1;
        public int DivisionIndex { get; private set; } = -1;
        public int DivisionSlot { get; private set; } = -1;
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int PointsFor { get; private set; }
        public int PointsAgainst { get; private set; }
        public int GamesPlayed => Wins + Losses;
        public int PointDiff => PointsFor - PointsAgainst;
        public float Percentage => GamesPlayed > 0 ? Wins / (float)GamesPlayed : 0f;

        /// <summary>
        /// 重置本条排名记录，并分配给指定的角色、分区和位置。
        /// </summary>
        public void Reset(int characterId, int divisionIndex, int divisionSlot)
        {
            CharacterId = characterId;
            DivisionIndex = divisionIndex;
            DivisionSlot = divisionSlot;
            Wins = 0;
            Losses = 0;
            PointsFor = 0;
            PointsAgainst = 0;
        }

        /// <summary>
        /// 将一场比赛的结果添加到本条排名记录中，更新胜场、负场和总得分。
        /// </summary>
        public void ApplyResult(int scored, int allowed)
        {
            PointsFor += scored;
            PointsAgainst += allowed;
            if (scored > allowed)
            {
                Wins++;
            }
            else
            {
                Losses++;
            }
        }
    }

    /// <summary>
    /// 锦标赛完整数据：管理 8 人锦标赛的全部流程——随机分两个区、3 轮常规赛、半决赛、决赛、记录所有比赛结果、计算排名。支持保存和恢复进度。
    /// </summary>
    public sealed class mlpTournamentData
    {
        private const int DivisionCount = 2;
        private const int TeamsPerDivision = 4;
        private const int RegularSeasonRoundCount = 3;
        private const int MatchesPerRegularSeasonRound = 4;

        private static readonly int[,,] RoundRobinPairings =
        {
            { { 0, 3 }, { 1, 2 } },
            { { 0, 2 }, { 3, 1 } },
            { { 0, 1 }, { 2, 3 } }
        };

        public bool Active { get; private set; }
        public bool Completed { get; private set; }
        public bool RegularSeasonCompleted { get; private set; }
        public bool PlayerQualifiedForPlayoffs { get; private set; }
        public mlpAiDifficulty Difficulty { get; private set; }
        public mlpTournamentStage CurrentStage { get; private set; }
        public int CurrentOpponentCharacterId { get; private set; } = -1;
        public int PlayerCharacterId { get; private set; } = -1;
        public int ChampionCharacterId { get; private set; } = -1;
        public int PlayerPlacement { get; private set; }
        public int CurrentRegularSeasonRoundIndex { get; private set; }
        public bool HasPendingPlayerMatch => Active && !Completed && CurrentOpponentCharacterId >= 0;

        public int[][] DivisionEntrantCharacterIds { get; } =
        {
            new[] { -1, -1, -1, -1 },
            new[] { -1, -1, -1, -1 }
        };

        public mlpTournamentStandingEntry[][] DivisionStandings { get; } =
        {
            new[]
            {
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry()
            },
            new[]
            {
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry(),
                new mlpTournamentStandingEntry()
            }
        };

        public mlpTournamentMatchResult[][] RegularSeasonRounds { get; } =
        {
            new[]
            {
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult()
            },
            new[]
            {
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult()
            },
            new[]
            {
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult(),
                new mlpTournamentMatchResult()
            }
        };

        public mlpTournamentMatchResult[] SemiFinalResults { get; } =
        {
            new mlpTournamentMatchResult(),
            new mlpTournamentMatchResult()
        };

        public mlpTournamentMatchResult ThirdPlaceResult { get; } = new mlpTournamentMatchResult();
        public mlpTournamentMatchResult FinalResult { get; } = new mlpTournamentMatchResult();

        /// <summary>
        /// 清除所有锦标赛状态，重置每场比赛结果和排名记录。
        /// </summary>
        public void Reset()
        {
            // 1. 重置所有基本状态标记和当前阶段信息
            Active = false;
            Completed = false;
            RegularSeasonCompleted = false;
            PlayerQualifiedForPlayoffs = false;
            Difficulty = mlpAiDifficulty.Normal;
            CurrentStage = mlpTournamentStage.None;
            CurrentOpponentCharacterId = -1;
            PlayerCharacterId = -1;
            ChampionCharacterId = -1;
            PlayerPlacement = 0;
            CurrentRegularSeasonRoundIndex = 0;

            // 2. 遍历两个分区，重置所有参赛者 ID 和排名记录
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    DivisionEntrantCharacterIds[division][slot] = -1;
                    DivisionStandings[division][slot].Reset(-1, division, slot);
                }
            }

            // 3. 遍历所有常规赛轮次，重置每场比赛结果
            for (var round = 0; round < RegularSeasonRoundCount; round++)
            {
                for (var matchIndex = 0; matchIndex < MatchesPerRegularSeasonRound; matchIndex++)
                {
                    RegularSeasonRounds[round][matchIndex].Reset();
                }
            }

            // 4. 重置季后赛所有比赛结果
            SemiFinalResults[0].Reset();
            SemiFinalResults[1].Reset();
            ThirdPlaceResult.Reset();
            FinalResult.Reset();
        }

        /// <summary>
        /// 使用指定的玩家角色和难度创建一场新锦标赛，将参赛者分为两个分区。
        /// </summary>
        public bool Create(int playerCharacterId, mlpAiDifficulty difficulty)
        {
            // 1. 重置所有数据，检查是否有足够的角色（至少 8 个）
            Reset();
            var activeCharacters = mlpPlayersData.GetActiveCharacterIds();
            if (activeCharacters.Length < DivisionCount * TeamsPerDivision)
            {
                return false;
            }

            // 2. 设置锦标赛基本信息：激活状态、难度、玩家角色
            Active = true;
            Difficulty = difficulty;
            PlayerCharacterId = mlpPlayersData.SanitizeCharacterId(playerCharacterId);

            // 3. 从剩余角色中随机选取 7 个对手，打乱顺序
            var availableOpponents = new List<int>(activeCharacters.Length - 1);
            for (var i = 0; i < activeCharacters.Length; i++)
            {
                if (activeCharacters[i] != PlayerCharacterId)
                {
                    availableOpponents.Add(activeCharacters[i]);
                }
            }
            Shuffle(availableOpponents);

            // 4. 分配到两个区：A 区放玩家 + 3 个对手，B 区放 4 个对手
            DivisionEntrantCharacterIds[0][0] = PlayerCharacterId;
            for (var slot = 1; slot < TeamsPerDivision; slot++)
            {
                DivisionEntrantCharacterIds[0][slot] = availableOpponents[slot - 1];
            }
            for (var slot = 0; slot < TeamsPerDivision; slot++)
            {
                DivisionEntrantCharacterIds[1][slot] = availableOpponents[slot + TeamsPerDivision - 1];
            }

            // 5. 初始化每个区的排名记录
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    DivisionStandings[division][slot].Reset(
                        DivisionEntrantCharacterIds[division][slot],
                        division,
                        slot);
                }
            }

            // 6. 生成常规赛赛程，进入第一轮
            BuildRegularSeasonSchedule();
            CurrentStage = mlpTournamentStage.RegularSeason;
            CurrentRegularSeasonRoundIndex = 0;
            CurrentOpponentCharacterId = GetPlayerOpponentForRound(CurrentRegularSeasonRoundIndex);
            return true;
        }

        /// <summary>
        /// 从常规赛阶段过渡到半决赛季后赛阶段。
        /// </summary>
        public void BeginFinals()
        {
            if (!Active || Completed || !RegularSeasonCompleted || !PlayerQualifiedForPlayoffs)
            {
                return;
            }

            if (CurrentStage != mlpTournamentStage.RegularSeason)
            {
                return;
            }

            CurrentStage = mlpTournamentStage.SemiFinal;
            CurrentOpponentCharacterId = GetPlayerOpponentForMatchSet(SemiFinalResults);
        }

        /// <summary>
        /// 记录玩家的比赛结果，并推进到锦标赛的下一个阶段。
        /// </summary>
        public void ApplyCurrentMatchResult(int playerScore, int opponentScore)
        {
            if (!Active || Completed)
            {
                return;
            }

            switch (CurrentStage)
            {
                case mlpTournamentStage.RegularSeason:
                    ApplyRegularSeasonResult(playerScore, opponentScore);
                    break;
                case mlpTournamentStage.SemiFinal:
                    ApplySemiFinalResult(playerScore, opponentScore);
                    break;
                case mlpTournamentStage.ThirdPlace:
                    ApplyPlacementResult(ThirdPlaceResult, playerScore, opponentScore);
                    break;
                case mlpTournamentStage.Final:
                    ApplyPlacementResult(FinalResult, playerScore, opponentScore);
                    break;
            }
        }

        /// <summary>
        /// 返回指定分区的排名，按胜场、净胜分和总得分排序。
        /// </summary>
        public mlpTournamentStandingEntry[] GetDivisionStandings(int divisionIndex)
        {
            if (divisionIndex < 0 || divisionIndex >= DivisionCount)
            {
                return new mlpTournamentStandingEntry[0];
            }

            var standings = new mlpTournamentStandingEntry[TeamsPerDivision];
            for (var i = 0; i < TeamsPerDivision; i++)
            {
                standings[i] = DivisionStandings[divisionIndex][i];
            }

            System.Array.Sort(standings, CompareDivisionStandings);
            return standings;
        }

        /// <summary>
        /// 记录玩家的常规赛比赛结果，模拟本轮剩余比赛，推进到下一轮或季后赛。
        /// </summary>
        private void ApplyRegularSeasonResult(int playerScore, int opponentScore)
        {
            // 1. 前置检查：常规赛已结束或轮次索引无效时跳过
            if (RegularSeasonCompleted || CurrentRegularSeasonRoundIndex < 0 || CurrentRegularSeasonRoundIndex >= RegularSeasonRoundCount)
            {
                return;
            }

            // 2. 找到玩家本轮的比赛，确认未完成
            var playerMatch = GetPlayerMatchForRound(CurrentRegularSeasonRoundIndex);
            if (playerMatch == null || playerMatch.Completed)
            {
                return;
            }

            // 3. 记录玩家比赛结果，更新双方排名
            playerMatch.Complete(playerScore, opponentScore);
            ApplyStandingUpdate(playerMatch);

            // 4. 模拟本轮其他比赛并更新排名
            var roundMatches = RegularSeasonRounds[CurrentRegularSeasonRoundIndex];
            for (var i = 0; i < roundMatches.Length; i++)
            {
                if (roundMatches[i] == playerMatch || roundMatches[i].Completed)
                {
                    continue;
                }

                SimulateMatch(roundMatches[i]);
                ApplyStandingUpdate(roundMatches[i]);
            }

            // 5. 推进到下一轮，如果还有剩余轮次则设置下一场对手
            CurrentRegularSeasonRoundIndex++;
            if (CurrentRegularSeasonRoundIndex < RegularSeasonRoundCount)
            {
                CurrentOpponentCharacterId = GetPlayerOpponentForRound(CurrentRegularSeasonRoundIndex);
                return;
            }

            // 6. 常规赛结束，构建季后赛对阵
            RegularSeasonCompleted = true;
            CurrentOpponentCharacterId = -1;
            BuildPlayoffBracket();

            // 7. 玩家未晋级时自动模拟全部季后赛
            if (!PlayerQualifiedForPlayoffs)
            {
                SimulateEntirePlayoffs();
                FinalizeTournament();
            }
        }

        /// <summary>
        /// 记录玩家的半决赛结果，模拟另一场半决赛，并安排决赛和三四名决赛。
        /// </summary>
        private void ApplySemiFinalResult(int playerScore, int opponentScore)
        {
            // 1. 找到玩家的半决赛，确认未完成
            var playerSemi = GetPlayerMatchFromSet(SemiFinalResults);
            if (playerSemi == null || playerSemi.Completed)
            {
                return;
            }

            // 2. 记录玩家半决赛结果
            playerSemi.Complete(playerScore, opponentScore);

            // 3. 模拟另一场半决赛
            for (var i = 0; i < SemiFinalResults.Length; i++)
            {
                if (SemiFinalResults[i].Completed)
                {
                    continue;
                }

                SimulateMatch(SemiFinalResults[i]);
            }

            // 4. 根据半决赛结果安排决赛和三四名决赛
            ConfigurePlacementMatchesFromSemiFinals();
            // 5. 玩家赢了进决赛，输了打三四名决赛
            if (playerSemi.WinnerCharacterId == PlayerCharacterId)
            {
                SimulateMatch(ThirdPlaceResult);
                CurrentStage = mlpTournamentStage.Final;
                CurrentOpponentCharacterId = GetOpponentCharacterId(FinalResult, PlayerCharacterId);
                return;
            }

            SimulateMatch(FinalResult);
            CurrentStage = mlpTournamentStage.ThirdPlace;
            CurrentOpponentCharacterId = GetOpponentCharacterId(ThirdPlaceResult, PlayerCharacterId);
        }

        /// <summary>
        /// 记录名次赛结果（三四名决赛或决赛）并完成锦标赛。
        /// </summary>
        private void ApplyPlacementResult(mlpTournamentMatchResult match, int playerScore, int opponentScore)
        {
            if (match == null || match.Completed)
            {
                return;
            }

            match.Complete(playerScore, opponentScore);
            FinalizeTournament();
        }

        /// <summary>
        /// 当决赛和三四名决赛都完成后，确定冠军和玩家排名。
        /// </summary>
        private void FinalizeTournament()
        {
            // 1. 决赛和三四名决赛都完成才能结算
            if (!FinalResult.Completed || !ThirdPlaceResult.Completed)
            {
                return;
            }

            // 2. 确定冠军，计算玩家最终排名
            ChampionCharacterId = FinalResult.WinnerCharacterId;
            PlayerPlacement = ResolvePlayerPlacement();
            // 3. 清除对手，标记锦标赛完成
            CurrentOpponentCharacterId = -1;
            CurrentStage = mlpTournamentStage.Complete;
            Completed = true;
        }

        /// <summary>
        /// 根据季后赛结果和常规赛排名确定玩家的最终名次（第 1 到第 8 名）。
        /// </summary>
        private int ResolvePlayerPlacement()
        {
            // 1. 获取前四名的角色 ID
            var champion = FinalResult.WinnerCharacterId;
            var runnerUp = GetMatchLoserCharacterId(FinalResult);
            var third = ThirdPlaceResult.WinnerCharacterId;
            var fourth = GetMatchLoserCharacterId(ThirdPlaceResult);

            // 2. 检查玩家是否是冠军、亚军、季军或第四名
            if (PlayerCharacterId == champion)
            {
                return 1;
            }

            if (PlayerCharacterId == runnerUp)
            {
                return 2;
            }

            if (PlayerCharacterId == third)
            {
                return 3;
            }

            if (PlayerCharacterId == fourth)
            {
                return 4;
            }

            // 3. 未进季后赛的队伍，按排名排序后确定第 5-8 名
            var nonPlayoffEntries = new List<mlpTournamentStandingEntry>(4);
            var playoffCharacters = new HashSet<int> { champion, runnerUp, third, fourth };
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    var entry = DivisionStandings[division][slot];
                    if (entry.CharacterId >= 0 && !playoffCharacters.Contains(entry.CharacterId))
                    {
                        nonPlayoffEntries.Add(entry);
                    }
                }
            }

            // 4. 排序后找到玩家位置
            nonPlayoffEntries.Sort(CompareOverallStandings);
            for (var i = 0; i < nonPlayoffEntries.Count; i++)
            {
                if (nonPlayoffEntries[i].CharacterId == PlayerCharacterId)
                {
                    return 5 + i;
                }
            }

            // 5. 兜底返回第 8 名
            return 8;
        }

        /// <summary>
        /// 生成三个常规赛轮次的循环赛赛程，覆盖两个分区。
        /// </summary>
        private void BuildRegularSeasonSchedule()
        {
            // 1. 遍历每一轮
            for (var round = 0; round < RegularSeasonRoundCount; round++)
            {
                // 2. 遍历每个分区
                for (var division = 0; division < DivisionCount; division++)
                {
                    // 3. 每轮每区 2 场比赛，从循环赛配对表中读取对阵双方
                    for (var pair = 0; pair < 2; pair++)
                    {
                        var matchIndex = division * 2 + pair;
                        var leftSlot = RoundRobinPairings[round, pair, 0];
                        var rightSlot = RoundRobinPairings[round, pair, 1];
                        RegularSeasonRounds[round][matchIndex].Reset(
                            DivisionEntrantCharacterIds[division][leftSlot],
                            DivisionEntrantCharacterIds[division][rightSlot]);
                    }
                }
            }
        }

        /// <summary>
        /// 将每个分区的前两名队伍配对为半决赛对阵，并检查玩家是否晋级。
        /// </summary>
        private void BuildPlayoffBracket()
        {
            // 1. 获取两个分区的排名
            var divisionA = GetDivisionStandings(0);
            var divisionB = GetDivisionStandings(1);

            // 2. 交叉配对：A 区第 1 vs B 区第 2，B 区第 1 vs A 区第 2
            SemiFinalResults[0].Reset(divisionA[0].CharacterId, divisionB[1].CharacterId);
            SemiFinalResults[1].Reset(divisionB[0].CharacterId, divisionA[1].CharacterId);
            ThirdPlaceResult.Reset();
            FinalResult.Reset();

            // 3. 检查玩家是否在半决赛对阵中
            PlayerQualifiedForPlayoffs =
                SemiFinalResults[0].LeftCharacterId == PlayerCharacterId ||
                SemiFinalResults[0].RightCharacterId == PlayerCharacterId ||
                SemiFinalResults[1].LeftCharacterId == PlayerCharacterId ||
                SemiFinalResults[1].RightCharacterId == PlayerCharacterId;
        }

        /// <summary>
        /// 根据半决赛的胜者和败者安排决赛和三四名决赛。
        /// </summary>
        private void ConfigurePlacementMatchesFromSemiFinals()
        {
            FinalResult.Reset(
                SemiFinalResults[0].WinnerCharacterId,
                SemiFinalResults[1].WinnerCharacterId);
            ThirdPlaceResult.Reset(
                GetMatchLoserCharacterId(SemiFinalResults[0]),
                GetMatchLoserCharacterId(SemiFinalResults[1]));
        }

        /// <summary>
        /// 当玩家未晋级时，自动模拟所有剩余的季后赛比赛。
        /// </summary>
        private void SimulateEntirePlayoffs()
        {
            // 1. 模拟两场半决赛
            for (var i = 0; i < SemiFinalResults.Length; i++)
            {
                SimulateMatch(SemiFinalResults[i]);
            }

            // 2. 根据半决赛结果安排决赛和三四名决赛
            ConfigurePlacementMatchesFromSemiFinals();
            // 3. 模拟三四名决赛和决赛
            SimulateMatch(ThirdPlaceResult);
            SimulateMatch(FinalResult);
        }

        /// <summary>
        /// 在指定的常规赛轮次中查找并返回玩家的比赛，未找到时返回 null。
        /// </summary>
        private mlpTournamentMatchResult GetPlayerMatchForRound(int roundIndex)
        {
            if (roundIndex < 0 || roundIndex >= RegularSeasonRounds.Length)
            {
                return null;
            }

            var matches = RegularSeasonRounds[roundIndex];
            for (var i = 0; i < matches.Length; i++)
            {
                if (matches[i].LeftCharacterId == PlayerCharacterId || matches[i].RightCharacterId == PlayerCharacterId)
                {
                    return matches[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 从比赛结果数组中查找并返回玩家的比赛，未找到时返回 null。
        /// </summary>
        private mlpTournamentMatchResult GetPlayerMatchFromSet(mlpTournamentMatchResult[] matches)
        {
            if (matches == null)
            {
                return null;
            }

            for (var i = 0; i < matches.Length; i++)
            {
                if (matches[i].LeftCharacterId == PlayerCharacterId || matches[i].RightCharacterId == PlayerCharacterId)
                {
                    return matches[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定常规赛轮次中玩家对手的角色 ID。
        /// </summary>
        private int GetPlayerOpponentForRound(int roundIndex)
        {
            return GetOpponentCharacterId(GetPlayerMatchForRound(roundIndex), PlayerCharacterId);
        }

        /// <summary>
        /// 从比赛结果数组中获取玩家对手的角色 ID。
        /// </summary>
        private int GetPlayerOpponentForMatchSet(mlpTournamentMatchResult[] matches)
        {
            return GetOpponentCharacterId(GetPlayerMatchFromSet(matches), PlayerCharacterId);
        }

        /// <summary>
        /// 根据已结束比赛的结果更新双方队伍的排名记录。
        /// </summary>
        private void ApplyStandingUpdate(mlpTournamentMatchResult match)
        {
            // 1. 查找双方的排名记录
            var leftEntry = GetStandingEntry(match.LeftCharacterId);
            var rightEntry = GetStandingEntry(match.RightCharacterId);
            if (leftEntry == null || rightEntry == null)
            {
                return;
            }

            // 2. 分别更新双方的胜负场次和得分
            leftEntry.ApplyResult(match.LeftScore, match.RightScore);
            rightEntry.ApplyResult(match.RightScore, match.LeftScore);
        }

        /// <summary>
        /// 查找并返回指定角色的排名记录，未找到时返回 null。
        /// </summary>
        private mlpTournamentStandingEntry GetStandingEntry(int characterId)
        {
            for (var division = 0; division < DivisionCount; division++)
            {
                for (var slot = 0; slot < TeamsPerDivision; slot++)
                {
                    var entry = DivisionStandings[division][slot];
                    if (entry.CharacterId == characterId)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 按胜场、净胜分、总得分和位置索引排序两个分区排名记录。
        /// </summary>
        private static int CompareDivisionStandings(mlpTournamentStandingEntry left, mlpTournamentStandingEntry right)
        {
            // 1. 首先比较胜场数（多的排前面）
            var winCompare = right.Wins.CompareTo(left.Wins);
            if (winCompare != 0)
            {
                return winCompare;
            }

            // 2. 胜场相同时比较净胜分（高的排前面）
            var diffCompare = right.PointDiff.CompareTo(left.PointDiff);
            if (diffCompare != 0)
            {
                return diffCompare;
            }

            // 3. 净胜分相同时比较总得分（高的排前面）
            var pointsCompare = right.PointsFor.CompareTo(left.PointsFor);
            if (pointsCompare != 0)
            {
                return pointsCompare;
            }

            // 4. 所有指标都相同时按初始位置排序
            return left.DivisionSlot.CompareTo(right.DivisionSlot);
        }

        /// <summary>
        /// 按分区排名、分区索引和位置排序两个总体排名记录。
        /// </summary>
        private static int CompareOverallStandings(mlpTournamentStandingEntry left, mlpTournamentStandingEntry right)
        {
            var compare = CompareDivisionStandings(left, right);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.DivisionIndex.CompareTo(right.DivisionIndex);
            if (compare != 0)
            {
                return compare;
            }

            return left.DivisionSlot.CompareTo(right.DivisionSlot);
        }

        /// <summary>
        /// 根据已知参赛者的角色 ID，返回该场比赛对手的角色 ID。
        /// </summary>
        private static int GetOpponentCharacterId(mlpTournamentMatchResult match, int characterId)
        {
            if (match == null)
            {
                return -1;
            }

            if (match.LeftCharacterId == characterId)
            {
                return match.RightCharacterId;
            }

            if (match.RightCharacterId == characterId)
            {
                return match.LeftCharacterId;
            }

            return -1;
        }

        /// <summary>
        /// 返回已结束比赛中败方的角色 ID。
        /// </summary>
        private static int GetMatchLoserCharacterId(mlpTournamentMatchResult match)
        {
            if (match == null || !match.Completed)
            {
                return -1;
            }

            if (match.WinnerCharacterId == match.LeftCharacterId)
            {
                return match.RightCharacterId;
            }

            if (match.WinnerCharacterId == match.RightCharacterId)
            {
                return match.LeftCharacterId;
            }

            return -1;
        }

        /// <summary>
        /// 为未进行的比赛自动生成随机比分并标记为已完成。
        /// </summary>
        private static void SimulateMatch(mlpTournamentMatchResult match)
        {
            if (match == null || match.Completed)
            {
                return;
            }

            var leftScore = 16 + Random.Range(0, 15);
            var rightScore = 14 + Random.Range(0, 15);
            match.Complete(leftScore, rightScore);
        }

        /// <summary>
        /// 使用 Fisher-Yates 算法对整数列表进行随机洗牌。
        /// </summary>
        private static void Shuffle(List<int> values)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                var temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }
    }
}
