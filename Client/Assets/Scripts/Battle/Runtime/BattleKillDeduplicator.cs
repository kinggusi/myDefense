using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Runtime
{
    public sealed class BattleKillAuditRecord
    {
        public BattleRuntimeMonsterKey RuntimeKey { get; }
        public string BattleSessionId => RuntimeKey.BattleSessionId;
        public ulong RuntimeMonsterId => RuntimeKey.RuntimeMonsterId;
        public string MonsterId { get; }
        public string KillerPlayerId { get; }
        public string FieldOwnerPlayerId { get; }
        public BattleMonsterLanePolicy LanePolicy { get; }
        public int SpawnWave { get; }
        public long KilledAtTick { get; }

        public BattleKillAuditRecord(
            BattleRuntimeMonsterKey runtimeKey,
            string monsterId,
            string killerPlayerId,
            string fieldOwnerPlayerId,
            BattleMonsterLanePolicy lanePolicy,
            int spawnWave,
            long killedAtTick)
        {
            if (string.IsNullOrWhiteSpace(runtimeKey.BattleSessionId) || runtimeKey.RuntimeMonsterId == 0)
                throw new ArgumentException("A valid runtime monster key is required.", nameof(runtimeKey));
            if (spawnWave < 1) throw new ArgumentOutOfRangeException(nameof(spawnWave));
            if (killedAtTick < 0) throw new ArgumentOutOfRangeException(nameof(killedAtTick));

            RuntimeKey = runtimeKey;
            MonsterId = BattleSessionContext.RequireText(monsterId, nameof(monsterId));
            KillerPlayerId = BattleSessionContext.RequireText(killerPlayerId, nameof(killerPlayerId));
            LanePolicy = lanePolicy;
            if (lanePolicy == BattleMonsterLanePolicy.EACH_FIELD)
                FieldOwnerPlayerId = BattleSessionContext.RequireText(fieldOwnerPlayerId, nameof(fieldOwnerPlayerId));
            else if (lanePolicy == BattleMonsterLanePolicy.BOSS_SHARED)
            {
                if (!string.IsNullOrEmpty(fieldOwnerPlayerId))
                    throw new ArgumentException("BOSS_SHARED kills cannot have a field owner.", nameof(fieldOwnerPlayerId));
                FieldOwnerPlayerId = null;
            }
            else
                throw new ArgumentOutOfRangeException(nameof(lanePolicy));

            SpawnWave = spawnWave;
            KilledAtTick = killedAtTick;
        }
    }

    public sealed class BattleKillDeduplicator
    {
        private readonly HashSet<BattleRuntimeMonsterKey> _processedKeys = new HashSet<BattleRuntimeMonsterKey>();
        private readonly List<BattleKillAuditRecord> _records = new List<BattleKillAuditRecord>();
        private readonly IReadOnlyList<BattleKillAuditRecord> _recordsView;

        public BattleKillDeduplicator()
        {
            _recordsView = _records.AsReadOnly();
        }

        public IReadOnlyCollection<BattleRuntimeMonsterKey> ProcessedKeys
        {
            get
            {
                var snapshot = new List<BattleRuntimeMonsterKey>(_processedKeys);
                snapshot.Sort();
                return snapshot.AsReadOnly();
            }
        }

        public IReadOnlyList<BattleKillAuditRecord> Records => _recordsView;

        public bool TryRegister(BattleKillAuditRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (!_processedKeys.Add(record.RuntimeKey)) return false;

            _records.Add(record);
            return true;
        }

        public bool Contains(BattleRuntimeMonsterKey key) => _processedKeys.Contains(key);
    }
}
