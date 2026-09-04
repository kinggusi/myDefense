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
        public string SupportPlayerId { get; }
        public string FieldOwnerPlayerId { get; }
        public BattleMonsterLanePolicy LanePolicy { get; }
        public int SpawnWave { get; }
        public long KilledAtTick { get; }
        public int KillGold { get; }
        public bool IsBoss { get; }

        public BattleKillAuditRecord(
            BattleRuntimeMonsterKey runtimeKey,
            string monsterId,
            string killerPlayerId,
            string fieldOwnerPlayerId,
            BattleMonsterLanePolicy lanePolicy,
            int spawnWave,
            long killedAtTick,
            string supportPlayerId = null,
            int killGold = 0,
            bool isBoss = false)
        {
            if (string.IsNullOrWhiteSpace(runtimeKey.BattleSessionId) || runtimeKey.RuntimeMonsterId == 0)
                throw new ArgumentException("A valid runtime monster key is required.", nameof(runtimeKey));
            if (spawnWave < 1) throw new ArgumentOutOfRangeException(nameof(spawnWave));
            if (killedAtTick < 0) throw new ArgumentOutOfRangeException(nameof(killedAtTick));
            if (killGold < 0) throw new ArgumentOutOfRangeException(nameof(killGold));

            RuntimeKey = runtimeKey;
            MonsterId = BattleSessionContext.RequireText(monsterId, nameof(monsterId));
            KillerPlayerId = BattleSessionContext.RequireText(killerPlayerId, nameof(killerPlayerId));
            if (!string.IsNullOrWhiteSpace(supportPlayerId)
                && string.Equals(KillerPlayerId, supportPlayerId, StringComparison.Ordinal))
                throw new ArgumentException("Support player must differ from the killer.", nameof(supportPlayerId));
            SupportPlayerId = string.IsNullOrWhiteSpace(supportPlayerId) ? null : supportPlayerId;
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
            KillGold = killGold;
            IsBoss = isBoss || lanePolicy == BattleMonsterLanePolicy.BOSS_SHARED;
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
            if (!TryReserve(record.RuntimeKey)) return false;

            _records.Add(record);
            return true;
        }

        public bool Contains(BattleRuntimeMonsterKey key) => _processedKeys.Contains(key);

        public bool TryReserve(BattleRuntimeMonsterKey key) => _processedKeys.Add(key);

        public bool TryAttachAudit(BattleKillAuditRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (!_processedKeys.Contains(record.RuntimeKey)
                || _records.Exists(existing => existing.RuntimeKey.Equals(record.RuntimeKey)))
                return false;

            _records.Add(record);
            return true;
        }

        public bool TryAttachSupport(BattleRuntimeMonsterKey key, string supportPlayerId)
        {
            if (string.IsNullOrWhiteSpace(supportPlayerId)) return false;
            int index = _records.FindIndex(record => record.RuntimeKey.Equals(key));
            if (index < 0) return false;
            BattleKillAuditRecord current = _records[index];
            if (!string.IsNullOrWhiteSpace(current.SupportPlayerId)) return false;
            try
            {
                _records[index] = new BattleKillAuditRecord(
                    current.RuntimeKey,
                    current.MonsterId,
                    current.KillerPlayerId,
                    current.FieldOwnerPlayerId,
                    current.LanePolicy,
                    current.SpawnWave,
                    current.KilledAtTick,
                    supportPlayerId,
                    current.KillGold,
                    current.IsBoss);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public void Release(BattleRuntimeMonsterKey key)
        {
            _processedKeys.Remove(key);
            _records.RemoveAll(record => record.RuntimeKey.Equals(key));
        }

        public void Clear()
        {
            _processedKeys.Clear();
            _records.Clear();
        }
    }
}
