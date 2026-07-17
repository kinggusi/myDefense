using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattlePlayerSummarySeed
    {
        public string PlayerId { get; }
        public int PlayerSlot { get; }
        public bool Eliminated { get; }
        public int? EliminatedWave { get; }
        public int InitialInGameGold { get; }
        public int InGameGoldEarned { get; }
        public int InGameGoldSpent { get; }
        public int FinalInGameGold { get; }

        public BattlePlayerSummarySeed(
            string playerId,
            bool eliminated,
            int? eliminatedWave,
            int inGameGoldEarned,
            int inGameGoldSpent)
            : this(playerId, 0, eliminated, eliminatedWave, 0, inGameGoldEarned, inGameGoldSpent,
                inGameGoldEarned - inGameGoldSpent)
        {
        }

        public BattlePlayerSummarySeed(
            string playerId,
            int playerSlot,
            bool eliminated,
            int? eliminatedWave,
            int initialInGameGold,
            int inGameGoldEarned,
            int inGameGoldSpent,
            int finalInGameGold)
        {
            PlayerId = BattleSessionContext.RequireText(playerId, nameof(playerId));
            if (playerSlot < 0) throw new ArgumentOutOfRangeException(nameof(playerSlot));
            if (eliminated && (!eliminatedWave.HasValue || eliminatedWave.Value < 1))
                throw new ArgumentException("Eliminated players require a positive eliminated wave.", nameof(eliminatedWave));
            if (!eliminated && eliminatedWave.HasValue)
                throw new ArgumentException("Active players cannot have an eliminated wave.", nameof(eliminatedWave));
            if (initialInGameGold < 0) throw new ArgumentOutOfRangeException(nameof(initialInGameGold));
            if (inGameGoldEarned < 0) throw new ArgumentOutOfRangeException(nameof(inGameGoldEarned));
            if (inGameGoldSpent < 0) throw new ArgumentOutOfRangeException(nameof(inGameGoldSpent));
            if (finalInGameGold < 0 || initialInGameGold + inGameGoldEarned - inGameGoldSpent != finalInGameGold)
                throw new ArgumentException("Player gold ledger is inconsistent.", nameof(finalInGameGold));

            Eliminated = eliminated;
            EliminatedWave = eliminatedWave;
            PlayerSlot = playerSlot;
            InitialInGameGold = initialInGameGold;
            InGameGoldEarned = inGameGoldEarned;
            InGameGoldSpent = inGameGoldSpent;
            FinalInGameGold = finalInGameGold;
        }
    }

    public sealed class BattlePlayerSummary
    {
        public string PlayerId { get; }
        public int PlayerSlot { get; }
        public bool Eliminated { get; }
        public int? EliminatedWave { get; }
        public int Kills { get; }
        public int SupportKills { get; }
        public int BossKills { get; }
        public int InitialInGameGold { get; }
        public int InGameGoldEarned { get; }
        public int InGameGoldSpent { get; }
        public int FinalInGameGold { get; }

        internal BattlePlayerSummary(BattlePlayerSummarySeed seed, int kills, int supportKills, int bossKills)
        {
            PlayerId = seed.PlayerId;
            PlayerSlot = seed.PlayerSlot;
            Eliminated = seed.Eliminated;
            EliminatedWave = seed.EliminatedWave;
            Kills = kills;
            SupportKills = supportKills;
            BossKills = bossKills;
            InitialInGameGold = seed.InitialInGameGold;
            InGameGoldEarned = seed.InGameGoldEarned;
            InGameGoldSpent = seed.InGameGoldSpent;
            FinalInGameGold = seed.FinalInGameGold;
        }
    }

    public sealed class BattleMonsterKillSummary
    {
        public string MonsterId { get; }
        public int KillCount { get; }
        public int BossKillCount { get; }
        public int TotalKillGold { get; }

        internal BattleMonsterKillSummary(string monsterId, int killCount, int bossKillCount, int totalKillGold)
        {
            MonsterId = monsterId;
            KillCount = killCount;
            BossKillCount = bossKillCount;
            TotalKillGold = totalKillGold;
        }
    }

    public sealed class BattleLaneKillSummary
    {
        public BattleMonsterLanePolicy LanePolicy { get; }
        public int KillCount { get; }
        public int AwardedGold { get; }

        internal BattleLaneKillSummary(BattleMonsterLanePolicy lanePolicy, int killCount)
        {
            LanePolicy = lanePolicy;
            KillCount = killCount;
            AwardedGold = 0;
        }
    }

    public sealed class BattleKillSummary
    {
        public IReadOnlyList<BattleMonsterKillSummary> ByMonster { get; }
        public IReadOnlyList<BattleRuntimeMonsterKey> ProcessedRuntimeKeys { get; }
        public IReadOnlyList<BattleLaneKillSummary> ByLanePolicy { get; }

        internal BattleKillSummary(
            IReadOnlyList<BattleMonsterKillSummary> byMonster,
            IReadOnlyList<BattleRuntimeMonsterKey> processedRuntimeKeys,
            IReadOnlyList<BattleLaneKillSummary> byLanePolicy)
        {
            ByMonster = byMonster;
            ProcessedRuntimeKeys = processedRuntimeKeys;
            ByLanePolicy = byLanePolicy;
        }
    }

    public sealed class BattleSummary
    {
        public string BattleSessionId { get; }
        public string CanonicalBalanceVersion { get; }
        public string CanonicalContentHash { get; }
        public MatchState Result { get; }
        public int FinalWave { get; }
        public IReadOnlyList<BattlePlayerSummary> Players { get; }
        public BattleKillSummary Kills { get; }

        internal BattleSummary(
            BattleSessionContext session,
            MatchState result,
            int finalWave,
            IReadOnlyList<BattlePlayerSummary> players,
            BattleKillSummary kills)
        {
            BattleSessionId = session.BattleSessionId;
            CanonicalBalanceVersion = session.CanonicalBalanceVersion;
            CanonicalContentHash = session.CanonicalContentHash;
            Result = result;
            FinalWave = finalWave;
            Players = players;
            Kills = kills;
        }
    }

    public static class BattleSummaryBuilder
    {
        public static BattleSummary Build(
            BattleSessionContext session,
            MatchState result,
            int finalWave,
            IEnumerable<BattlePlayerSummarySeed> playerSeeds,
            IEnumerable<BattleKillAuditRecord> killRecords,
            Func<string, int> killGoldResolver = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (result == MatchState.RUNNING) throw new ArgumentException("A final summary requires a terminal result.", nameof(result));
            if (finalWave < 0) throw new ArgumentOutOfRangeException(nameof(finalWave));
            if (playerSeeds == null) throw new ArgumentNullException(nameof(playerSeeds));
            if (killRecords == null) throw new ArgumentNullException(nameof(killRecords));
            killGoldResolver ??= _ => 0;

            List<BattlePlayerSummarySeed> seeds = playerSeeds.OrderBy(seed => seed.PlayerId, StringComparer.Ordinal).ToList();
            if (seeds.Count == 0) throw new ArgumentException("At least one player summary is required.", nameof(playerSeeds));
            if (seeds.Select(seed => seed.PlayerId).Distinct(StringComparer.Ordinal).Count() != seeds.Count)
                throw new ArgumentException("Player summary IDs must be unique.", nameof(playerSeeds));

            var uniqueRecords = new Dictionary<BattleRuntimeMonsterKey, BattleKillAuditRecord>();
            foreach (BattleKillAuditRecord record in killRecords)
            {
                if (record == null) throw new ArgumentException("Kill records cannot contain null.", nameof(killRecords));
                if (!string.Equals(record.BattleSessionId, session.BattleSessionId, StringComparison.Ordinal))
                    throw new ArgumentException("Kill records must belong to the summarized session.", nameof(killRecords));
                if (!uniqueRecords.ContainsKey(record.RuntimeKey))
                    uniqueRecords.Add(record.RuntimeKey, record);
            }

            List<BattleKillAuditRecord> orderedRecords = uniqueRecords.Values
                .OrderBy(record => record.RuntimeKey)
                .ToList();
            var seedById = seeds.ToDictionary(seed => seed.PlayerId, StringComparer.Ordinal);
            foreach (BattleKillAuditRecord record in orderedRecords)
            {
                if (!seedById.ContainsKey(record.KillerPlayerId))
                    throw new ArgumentException("Every killer must exist in the player summary.", nameof(killRecords));
            }

            var players = new List<BattlePlayerSummary>(seeds.Count);
            foreach (BattlePlayerSummarySeed seed in seeds)
            {
                int kills = orderedRecords.Count(record =>
                    string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal)
                    && !record.IsSupportKill);
                int supportKills = orderedRecords.Count(record =>
                    string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal)
                    && record.IsSupportKill);
                int bossKills = orderedRecords.Count(record =>
                    string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal)
                    && record.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED);
                players.Add(new BattlePlayerSummary(seed, kills, supportKills, bossKills));
            }

            var byMonster = orderedRecords
                .GroupBy(record => record.MonsterId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new BattleMonsterKillSummary(
                    group.Key,
                    group.Count(),
                    group.Count(record => record.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED),
                    checked(group.Count() * killGoldResolver(group.Key))))
                .ToList()
                .AsReadOnly();
            var runtimeKeys = orderedRecords.Select(record => record.RuntimeKey).ToList().AsReadOnly();
            var byLane = Enum.GetValues(typeof(BattleMonsterLanePolicy))
                .Cast<BattleMonsterLanePolicy>()
                .OrderBy(policy => (int)policy)
                .Select(policy => new BattleLaneKillSummary(policy, orderedRecords.Count(record => record.LanePolicy == policy)))
                .ToList()
                .AsReadOnly();

            return new BattleSummary(
                session,
                result,
                finalWave,
                players.AsReadOnly(),
                new BattleKillSummary(byMonster, runtimeKeys, byLane));
        }
    }

    /// <summary>
    /// Converts the authoritative Battle summary into the Spring Settlement transport contract.
    /// Runtime audit fields remain internal to Battle; only canonical aggregate fields cross the API boundary.
    /// </summary>
    public static class BattleSettlementSummaryBuilder
    {
        public static BattleSettlementSummary Build(
            BattleSummary summary,
            string requestId,
            DateTime startedAtUtc,
            DateTime finishedAtUtc,
            string summaryHash)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("A request ID is required.", nameof(requestId));
            if (string.IsNullOrWhiteSpace(summaryHash)) throw new ArgumentException("A summary hash is required.", nameof(summaryHash));
            if (finishedAtUtc < startedAtUtc) throw new ArgumentException("Settlement finish time cannot precede start time.", nameof(finishedAtUtc));
            if (summary.Players.Count != 2 || summary.Players.Any(player => player.PlayerSlot < 1)
                || summary.Players.Select(player => player.PlayerSlot).Distinct().Count() != 2
                || !summary.Players.Select(player => player.PlayerSlot).OrderBy(slot => slot).SequenceEqual(new[] { 1, 2 }))
                throw new InvalidOperationException("A settlement requires exactly player slots 1 and 2.");

            return new BattleSettlementSummary
            {
                requestId = requestId,
                battleSessionId = summary.BattleSessionId,
                balanceVersion = summary.CanonicalBalanceVersion,
                contentHash = summary.CanonicalContentHash,
                result = ToSettlementResult(summary.Result),
                finalWave = summary.FinalWave,
                startedAt = FormatUtcAsContractTime(startedAtUtc),
                finishedAt = FormatUtcAsContractTime(finishedAtUtc),
                players = summary.Players.Select(ToPlayer).ToArray(),
                monsterKills = summary.Kills.ByMonster.Select(ToMonster).ToArray(),
                summaryHash = summaryHash
            };
        }

        private static BattleSettlementPlayerSummary ToPlayer(BattlePlayerSummary player)
        {
            return new BattleSettlementPlayerSummary
            {
                playerId = player.PlayerId,
                playerSlot = player.PlayerSlot,
                eliminated = player.Eliminated,
                eliminatedWave = player.EliminatedWave,
                kills = player.Kills,
                supportKills = player.SupportKills,
                bossKills = player.BossKills,
                initialInGameGold = player.InitialInGameGold,
                inGameGoldEarned = player.InGameGoldEarned,
                inGameGoldSpent = player.InGameGoldSpent,
                finalInGameGold = player.FinalInGameGold
            };
        }

        private static BattleSettlementMonsterSummary ToMonster(BattleMonsterKillSummary monster)
        {
            return new BattleSettlementMonsterSummary
            {
                monsterSpecId = monster.MonsterId,
                totalKills = monster.KillCount,
                bossKills = monster.BossKillCount,
                totalKillGold = monster.TotalKillGold
            };
        }

        private static string ToSettlementResult(MatchState result)
        {
            return result switch
            {
                MatchState.CLEARED => BattleSettlementResultValues.Victory,
                MatchState.FAILED => BattleSettlementResultValues.Defeat,
                _ => throw new ArgumentException("A terminal MatchState is required.", nameof(result))
            };
        }

        private static string FormatUtcAsContractTime(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }
    }
}
