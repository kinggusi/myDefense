using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattlePlayerSummarySeed
    {
        public string PlayerId { get; }
        public bool Eliminated { get; }
        public int? EliminatedWave { get; }
        public int InGameGoldEarned { get; }
        public int InGameGoldSpent { get; }

        public BattlePlayerSummarySeed(
            string playerId,
            bool eliminated,
            int? eliminatedWave,
            int inGameGoldEarned,
            int inGameGoldSpent)
        {
            PlayerId = BattleSessionContext.RequireText(playerId, nameof(playerId));
            if (eliminated && (!eliminatedWave.HasValue || eliminatedWave.Value < 1))
                throw new ArgumentException("Eliminated players require a positive eliminated wave.", nameof(eliminatedWave));
            if (!eliminated && eliminatedWave.HasValue)
                throw new ArgumentException("Active players cannot have an eliminated wave.", nameof(eliminatedWave));
            if (inGameGoldEarned < 0) throw new ArgumentOutOfRangeException(nameof(inGameGoldEarned));
            if (inGameGoldSpent < 0) throw new ArgumentOutOfRangeException(nameof(inGameGoldSpent));

            Eliminated = eliminated;
            EliminatedWave = eliminatedWave;
            InGameGoldEarned = inGameGoldEarned;
            InGameGoldSpent = inGameGoldSpent;
        }
    }

    public sealed class BattlePlayerSummary
    {
        public string PlayerId { get; }
        public bool Eliminated { get; }
        public int? EliminatedWave { get; }
        public int Kills { get; }
        public int BossKills { get; }
        public int InGameGoldEarned { get; }
        public int InGameGoldSpent { get; }

        internal BattlePlayerSummary(BattlePlayerSummarySeed seed, int kills, int bossKills)
        {
            PlayerId = seed.PlayerId;
            Eliminated = seed.Eliminated;
            EliminatedWave = seed.EliminatedWave;
            Kills = kills;
            BossKills = bossKills;
            InGameGoldEarned = seed.InGameGoldEarned;
            InGameGoldSpent = seed.InGameGoldSpent;
        }
    }

    public sealed class BattleMonsterKillSummary
    {
        public string MonsterId { get; }
        public int KillCount { get; }

        internal BattleMonsterKillSummary(string monsterId, int killCount)
        {
            MonsterId = monsterId;
            KillCount = killCount;
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
            IEnumerable<BattleKillAuditRecord> killRecords)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (result == MatchState.RUNNING) throw new ArgumentException("A final summary requires a terminal result.", nameof(result));
            if (finalWave < 0) throw new ArgumentOutOfRangeException(nameof(finalWave));
            if (playerSeeds == null) throw new ArgumentNullException(nameof(playerSeeds));
            if (killRecords == null) throw new ArgumentNullException(nameof(killRecords));

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
                int kills = orderedRecords.Count(record => string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal));
                int bossKills = orderedRecords.Count(record =>
                    string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal)
                    && record.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED);
                players.Add(new BattlePlayerSummary(seed, kills, bossKills));
            }

            var byMonster = orderedRecords
                .GroupBy(record => record.MonsterId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new BattleMonsterKillSummary(group.Key, group.Count()))
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
}
