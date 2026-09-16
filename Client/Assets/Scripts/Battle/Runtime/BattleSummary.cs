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
        public int PlayerSlot { get; }
        public int InitialInGameGold { get; }
        public int FinalInGameGold { get; }
        public bool Abandoned { get; }

        public BattlePlayerSummarySeed(
            string playerId,
            bool eliminated,
            int? eliminatedWave,
            int inGameGoldEarned,
            int inGameGoldSpent)
            : this(
                playerId,
                0,
                eliminated,
                eliminatedWave,
                0,
                inGameGoldEarned,
                inGameGoldSpent,
                inGameGoldEarned - inGameGoldSpent,
                false)
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
            int finalInGameGold,
            bool abandoned = false)
        {
            PlayerId = BattleSessionContext.RequireText(playerId, nameof(playerId));
            if (playerSlot < 0) throw new ArgumentOutOfRangeException(nameof(playerSlot));
            if (eliminated && (!eliminatedWave.HasValue || eliminatedWave.Value < 1))
                throw new ArgumentException("Eliminated players require a positive eliminated wave.", nameof(eliminatedWave));
            if (!eliminated && eliminatedWave.HasValue)
                throw new ArgumentException("Active players cannot have an eliminated wave.", nameof(eliminatedWave));
            if (inGameGoldEarned < 0) throw new ArgumentOutOfRangeException(nameof(inGameGoldEarned));
            if (inGameGoldSpent < 0) throw new ArgumentOutOfRangeException(nameof(inGameGoldSpent));
            if (initialInGameGold < 0) throw new ArgumentOutOfRangeException(nameof(initialInGameGold));
            if (finalInGameGold < 0) throw new ArgumentOutOfRangeException(nameof(finalInGameGold));
            if ((long)initialInGameGold + inGameGoldEarned - inGameGoldSpent != finalInGameGold)
                throw new ArgumentException("In-game Gold ledger does not balance.", nameof(finalInGameGold));

            Eliminated = eliminated;
            EliminatedWave = eliminatedWave;
            InGameGoldEarned = inGameGoldEarned;
            InGameGoldSpent = inGameGoldSpent;
            PlayerSlot = playerSlot;
            InitialInGameGold = initialInGameGold;
            FinalInGameGold = finalInGameGold;
            Abandoned = abandoned;
        }
    }

    public sealed class BattlePlayerSummary
    {
        public string PlayerId { get; }
        public bool Eliminated { get; }
        public int? EliminatedWave { get; }
        public int Kills { get; }
        public int SupportKills { get; }
        public int BossKills { get; }
        public int InGameGoldEarned { get; }
        public int InGameGoldSpent { get; }
        public int PlayerSlot { get; }
        public int InitialInGameGold { get; }
        public int FinalInGameGold { get; }
        public bool Abandoned { get; }

        internal BattlePlayerSummary(BattlePlayerSummarySeed seed, int kills, int supportKills, int bossKills)
        {
            PlayerId = seed.PlayerId;
            Eliminated = seed.Eliminated;
            EliminatedWave = seed.EliminatedWave;
            Kills = kills;
            SupportKills = supportKills;
            BossKills = bossKills;
            InGameGoldEarned = seed.InGameGoldEarned;
            InGameGoldSpent = seed.InGameGoldSpent;
            PlayerSlot = seed.PlayerSlot;
            InitialInGameGold = seed.InitialInGameGold;
            FinalInGameGold = seed.FinalInGameGold;
            Abandoned = seed.Abandoned;
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

        internal BattleLaneKillSummary(BattleMonsterLanePolicy lanePolicy, int killCount, int awardedGold = 0)
        {
            LanePolicy = lanePolicy;
            KillCount = killCount;
            AwardedGold = awardedGold;
        }
    }

    public sealed class BattleKillSummary
    {
        public IReadOnlyList<BattleMonsterKillSummary> ByMonster { get; }
        public IReadOnlyList<BattleRuntimeMonsterKey> ProcessedRuntimeKeys { get; }
        public IReadOnlyList<BattleLaneKillSummary> ByLanePolicy { get; }
        public IReadOnlyList<BattleKillAuditRecord> AuditRecords { get; }

        internal BattleKillSummary(
            IReadOnlyList<BattleMonsterKillSummary> byMonster,
            IReadOnlyList<BattleRuntimeMonsterKey> processedRuntimeKeys,
            IReadOnlyList<BattleLaneKillSummary> byLanePolicy,
            IReadOnlyList<BattleKillAuditRecord> auditRecords)
        {
            ByMonster = byMonster;
            ProcessedRuntimeKeys = processedRuntimeKeys;
            ByLanePolicy = byLanePolicy;
            AuditRecords = auditRecords;
        }
    }

    public sealed class BattleSummary
    {
        public string BattleSessionId { get; }
        public string CanonicalBalanceVersion { get; }
        public string CanonicalContentHash { get; }
        public string MapId { get; }
        public MatchState Result { get; }
        public int FinalWave { get; }
        public IReadOnlyList<BattlePlayerSummary> Players { get; }
        public BattleKillSummary Kills { get; }
        public IReadOnlyList<BattleSpawnAuditRecord> SpawnRecords { get; }

        internal BattleSummary(
            BattleSessionContext session,
            MatchState result,
            int finalWave,
            IReadOnlyList<BattlePlayerSummary> players,
            BattleKillSummary kills,
            IReadOnlyList<BattleSpawnAuditRecord> spawnRecords)
        {
            BattleSessionId = session.BattleSessionId;
            CanonicalBalanceVersion = session.CanonicalBalanceVersion;
            CanonicalContentHash = session.CanonicalContentHash;
            MapId = session.MapId;
            Result = result;
            FinalWave = finalWave;
            Players = players;
            Kills = kills;
            SpawnRecords = spawnRecords;
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
            return Build(
                session,
                result,
                finalWave,
                playerSeeds,
                killRecords,
                Array.Empty<BattleSpawnAuditRecord>());
        }

        public static BattleSummary Build(
            BattleSessionContext session,
            MatchState result,
            int finalWave,
            IEnumerable<BattlePlayerSummarySeed> playerSeeds,
            IEnumerable<BattleKillAuditRecord> killRecords,
            IEnumerable<BattleSpawnAuditRecord> spawnRecords)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (result == MatchState.RUNNING) throw new ArgumentException("A final summary requires a terminal result.", nameof(result));
            if (finalWave < 0) throw new ArgumentOutOfRangeException(nameof(finalWave));
            if (playerSeeds == null) throw new ArgumentNullException(nameof(playerSeeds));
            if (killRecords == null) throw new ArgumentNullException(nameof(killRecords));
            if (spawnRecords == null) throw new ArgumentNullException(nameof(spawnRecords));

            List<BattlePlayerSummarySeed> seeds = playerSeeds.OrderBy(seed => seed.PlayerId, StringComparer.Ordinal).ToList();
            if (seeds.Count == 0) throw new ArgumentException("At least one player summary is required.", nameof(playerSeeds));
            if (seeds.Select(seed => seed.PlayerId).Distinct(StringComparer.Ordinal).Count() != seeds.Count)
                throw new ArgumentException("Player summary IDs must be unique.", nameof(playerSeeds));
            List<int> assignedSlots = seeds
                .Where(seed => seed.PlayerSlot > 0)
                .Select(seed => seed.PlayerSlot)
                .ToList();
            if (assignedSlots.Distinct().Count() != assignedSlots.Count)
                throw new ArgumentException("Player summary slots must be unique.", nameof(playerSeeds));

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

            var uniqueSpawns = new Dictionary<BattleRuntimeMonsterKey, BattleSpawnAuditRecord>();
            var spawnPositions = new HashSet<string>(StringComparer.Ordinal);
            int maxRecordWave = result == MatchState.FAILED ? finalWave + 1 : finalWave;
            foreach (BattleSpawnAuditRecord record in spawnRecords)
            {
                if (record == null) throw new ArgumentException("Spawn records cannot contain null.", nameof(spawnRecords));
                if (!string.Equals(record.BattleSessionId, session.BattleSessionId, StringComparison.Ordinal))
                    throw new ArgumentException("Spawn records must belong to the summarized session.", nameof(spawnRecords));
                if (record.SpawnWave > maxRecordWave)
                    throw new ArgumentException("Spawn records cannot be later than the terminal Wave.", nameof(spawnRecords));
                if (!uniqueSpawns.TryAdd(record.RuntimeKey, record))
                    throw new ArgumentException("Spawn runtime monster IDs must be unique.", nameof(spawnRecords));

                string position = record.SpawnWave + "\u001f" + record.SpawnGroupId + "\u001f"
                    + record.SpawnOrder + "\u001f" + (record.FieldOwnerPlayerSlot ?? 0) + "\u001f"
                    + record.SpawnOrdinal;
                if (!spawnPositions.Add(position))
                    throw new ArgumentException("Canonical Spawn positions must be unique.", nameof(spawnRecords));
            }
            List<BattleSpawnAuditRecord> orderedSpawns = uniqueSpawns.Values
                .OrderBy(record => record.RuntimeKey)
                .ToList();

            var seedById = seeds.ToDictionary(seed => seed.PlayerId, StringComparer.Ordinal);
            foreach (BattleKillAuditRecord record in orderedRecords)
            {
                if (record.SpawnWave > maxRecordWave)
                    throw new ArgumentException("Kill records cannot be later than the terminal Wave.", nameof(killRecords));
                if (!seedById.ContainsKey(record.KillerPlayerId))
                    throw new ArgumentException("Every killer must exist in the player summary.", nameof(killRecords));
                if (!string.IsNullOrWhiteSpace(record.SupportPlayerId)
                    && !seedById.ContainsKey(record.SupportPlayerId))
                    throw new ArgumentException("Every support player must exist in the player summary.", nameof(killRecords));

                if (record.SpawnWave > finalWave)
                {
                    if (!uniqueSpawns.TryGetValue(record.RuntimeKey, out BattleSpawnAuditRecord spawn)
                        || spawn.SpawnWave != record.SpawnWave
                        || !string.Equals(spawn.MonsterId, record.MonsterId, StringComparison.Ordinal)
                        || spawn.LanePolicy != record.LanePolicy
                        || spawn.IsBoss != record.IsBoss)
                    {
                        throw new ArgumentException(
                            "Every unfinished-Wave Kill requires matching authoritative Spawn evidence.",
                            nameof(killRecords));
                    }
                }
            }

            var players = new List<BattlePlayerSummary>(seeds.Count);
            foreach (BattlePlayerSummarySeed seed in seeds)
            {
                int kills = orderedRecords.Count(record => string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal));
                int bossKills = orderedRecords.Count(record =>
                    string.Equals(record.KillerPlayerId, seed.PlayerId, StringComparison.Ordinal)
                    && record.IsBoss);
                int supportKills = orderedRecords.Count(record =>
                    string.Equals(record.SupportPlayerId, seed.PlayerId, StringComparison.Ordinal));
                players.Add(new BattlePlayerSummary(seed, kills, supportKills, bossKills));
            }

            var byMonster = orderedRecords
                .GroupBy(record => record.MonsterId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new BattleMonsterKillSummary(
                    group.Key,
                    group.Count(),
                    group.Count(record => record.IsBoss),
                    group.Sum(record => record.KillGold)))
                .ToList()
                .AsReadOnly();
            var runtimeKeys = orderedRecords.Select(record => record.RuntimeKey).ToList().AsReadOnly();
            var byLane = Enum.GetValues(typeof(BattleMonsterLanePolicy))
                .Cast<BattleMonsterLanePolicy>()
                .OrderBy(policy => (int)policy)
                .Select(policy => new BattleLaneKillSummary(
                    policy,
                    orderedRecords.Count(record => record.LanePolicy == policy),
                    orderedRecords
                        .Where(record => record.LanePolicy == policy)
                        .Sum(record => record.KillGold)))
                .ToList()
                .AsReadOnly();

            return new BattleSummary(
                session,
                result,
                finalWave,
                players.AsReadOnly(),
                new BattleKillSummary(byMonster, runtimeKeys, byLane, orderedRecords.AsReadOnly()),
                orderedSpawns.AsReadOnly());
        }
    }
}
