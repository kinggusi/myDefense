using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Balance.Canonical
{
    public static class CanonicalBalanceContract
    {
        public const int SchemaVersion = 1;
        public const string DefaultModeId = "COOP_STANDARD";
        public const int DefaultPlayerCount = 2;
        public const string ManifestFileName = "balance-manifest.json";
        public const string MonsterFileName = "monster-spec.json";
        public const string WaveFileName = "wave-spec.json";
        public const string WaveSpawnFileName = "wave-spawn.json";
        public const string FieldLimitFileName = "field-limit.json";
    }

    public sealed class CanonicalManifestFileEntry
    {
        public string Name { get; }
        public string Sha256 { get; }
        public long Size { get; }

        public CanonicalManifestFileEntry(string name, string sha256, long size)
        {
            Name = name;
            Sha256 = sha256;
            Size = size;
        }
    }

    public sealed class CanonicalBalanceManifest
    {
        public int SchemaVersion { get; }
        public string BalanceVersion { get; }
        public string ContentHash { get; }
        public IReadOnlyList<CanonicalManifestFileEntry> Files { get; }

        public CanonicalBalanceManifest(int schemaVersion, string balanceVersion, string contentHash, IEnumerable<CanonicalManifestFileEntry> files)
        {
            SchemaVersion = schemaVersion;
            BalanceVersion = balanceVersion;
            ContentHash = contentHash;
            Files = Array.AsReadOnly(new List<CanonicalManifestFileEntry>(files ?? Array.Empty<CanonicalManifestFileEntry>()).ToArray());
        }
    }

    public sealed class CanonicalMonsterSpec
    {
        public string MonsterId { get; }
        public string MonsterType { get; }
        public float BaseHp { get; }
        public float MoveSpeed { get; }
        public int KillGold { get; }
        public bool Enabled { get; }
        public string PrefabKey { get; }
        public bool CountsTowardLaneLimit { get; }

        public CanonicalMonsterSpec(string monsterId, string monsterType, float baseHp, float moveSpeed, int killGold, bool enabled, string prefabKey, bool countsTowardLaneLimit)
        {
            MonsterId = monsterId;
            MonsterType = monsterType;
            BaseHp = baseHp;
            MoveSpeed = moveSpeed;
            KillGold = killGold;
            Enabled = enabled;
            PrefabKey = prefabKey;
            CountsTowardLaneLimit = countsTowardLaneLimit;
        }
    }

    public sealed class CanonicalWaveSpec
    {
        public string ModeId { get; }
        public int Wave { get; }
        public float HpMultiplier { get; }
        public float InterWaveDelaySeconds { get; }
        public bool IsBossWave { get; }
        public float BossTimeLimitSeconds { get; }
        public string SpawnGroupId { get; }
        public bool Enabled { get; }

        public CanonicalWaveSpec(string modeId, int wave, float hpMultiplier, float interWaveDelaySeconds, bool isBossWave, float bossTimeLimitSeconds, string spawnGroupId, bool enabled)
        {
            ModeId = modeId;
            Wave = wave;
            HpMultiplier = hpMultiplier;
            InterWaveDelaySeconds = interWaveDelaySeconds;
            IsBossWave = isBossWave;
            BossTimeLimitSeconds = bossTimeLimitSeconds;
            SpawnGroupId = spawnGroupId;
            Enabled = enabled;
        }

        public string RuntimeWaveId => ModeId + ":" + Wave;
    }

    public enum CanonicalLanePolicy
    {
        EACH_FIELD,
        BOSS_SHARED
    }

    public sealed class CanonicalWaveSpawn
    {
        public string SpawnGroupId { get; }
        public int Order { get; }
        public string MonsterId { get; }
        public int SpawnCountPerField { get; }
        public float StartDelaySeconds { get; }
        public float SpawnIntervalSeconds { get; }
        public CanonicalLanePolicy LanePolicy { get; }

        public CanonicalWaveSpawn(string spawnGroupId, int order, string monsterId, int spawnCountPerField, float startDelaySeconds, float spawnIntervalSeconds, CanonicalLanePolicy lanePolicy)
        {
            SpawnGroupId = spawnGroupId;
            Order = order;
            MonsterId = monsterId;
            SpawnCountPerField = spawnCountPerField;
            StartDelaySeconds = startDelaySeconds;
            SpawnIntervalSeconds = spawnIntervalSeconds;
            LanePolicy = lanePolicy;
        }
    }

    public sealed class CanonicalFieldLimit
    {
        public string ModeId { get; }
        public int PlayerCount { get; }
        public int MaxAliveMonsterCountPerField { get; }
        public int WarningThreshold { get; }
        public int DangerThreshold { get; }

        public CanonicalFieldLimit(string modeId, int playerCount, int maxAliveMonsterCountPerField, int warningThreshold, int dangerThreshold)
        {
            ModeId = modeId;
            PlayerCount = playerCount;
            MaxAliveMonsterCountPerField = maxAliveMonsterCountPerField;
            WarningThreshold = warningThreshold;
            DangerThreshold = dangerThreshold;
        }
    }

    public sealed class CanonicalMonsterRegistry
    {
        private readonly Dictionary<string, CanonicalMonsterSpec> _byId;

        internal CanonicalMonsterRegistry(IEnumerable<CanonicalMonsterSpec> monsters)
        {
            _byId = new Dictionary<string, CanonicalMonsterSpec>(StringComparer.Ordinal);
            foreach (CanonicalMonsterSpec monster in monsters) _byId.Add(monster.MonsterId, monster);
        }

        public bool TryGet(string monsterId, out CanonicalMonsterSpec monster)
        {
            monster = null;
            return monsterId != null && _byId.TryGetValue(monsterId, out monster);
        }
    }

    public sealed class CanonicalWaveRegistry
    {
        private readonly IReadOnlyList<CanonicalWaveSpec> _all;

        internal CanonicalWaveRegistry(IEnumerable<CanonicalWaveSpec> waves)
        {
            var sorted = new List<CanonicalWaveSpec>(waves);
            sorted.Sort((left, right) =>
            {
                int modeOrder = string.CompareOrdinal(left.ModeId, right.ModeId);
                return modeOrder != 0 ? modeOrder : left.Wave.CompareTo(right.Wave);
            });
            _all = Array.AsReadOnly(sorted.ToArray());
        }

        public IReadOnlyList<CanonicalWaveSpec> All => _all;
    }

    public sealed class CanonicalWaveSpawnRegistry
    {
        private readonly Dictionary<string, IReadOnlyList<CanonicalWaveSpawn>> _byGroup;

        internal CanonicalWaveSpawnRegistry(IEnumerable<CanonicalWaveSpawn> spawns)
        {
            var mutable = new Dictionary<string, List<CanonicalWaveSpawn>>(StringComparer.Ordinal);
            foreach (CanonicalWaveSpawn spawn in spawns)
            {
                if (!mutable.TryGetValue(spawn.SpawnGroupId, out List<CanonicalWaveSpawn> group))
                {
                    group = new List<CanonicalWaveSpawn>();
                    mutable.Add(spawn.SpawnGroupId, group);
                }
                group.Add(spawn);
            }

            _byGroup = new Dictionary<string, IReadOnlyList<CanonicalWaveSpawn>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<CanonicalWaveSpawn>> pair in mutable)
            {
                pair.Value.Sort((left, right) => left.Order.CompareTo(right.Order));
                _byGroup.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
            }
        }

        public IReadOnlyList<CanonicalWaveSpawn> GetByGroup(string spawnGroupId)
        {
            return spawnGroupId != null && _byGroup.TryGetValue(spawnGroupId, out IReadOnlyList<CanonicalWaveSpawn> result)
                ? result
                : Array.AsReadOnly(Array.Empty<CanonicalWaveSpawn>());
        }
    }

    public sealed class CanonicalFieldLimitRegistry
    {
        private readonly Dictionary<string, CanonicalFieldLimit> _byModeAndPlayers;

        internal CanonicalFieldLimitRegistry(IEnumerable<CanonicalFieldLimit> limits)
        {
            _byModeAndPlayers = new Dictionary<string, CanonicalFieldLimit>(StringComparer.Ordinal);
            foreach (CanonicalFieldLimit limit in limits) _byModeAndPlayers.Add(BuildKey(limit.ModeId, limit.PlayerCount), limit);
        }

        public bool TryGet(string modeId, int playerCount, out CanonicalFieldLimit limit)
        {
            return _byModeAndPlayers.TryGetValue(BuildKey(modeId, playerCount), out limit);
        }

        private static string BuildKey(string modeId, int playerCount)
        {
            return modeId + ":" + playerCount;
        }
    }

    public interface ICanonicalMonsterRuntimeMapping
    {
        bool TryMap(string monsterType, out string prefabKey, out bool countsTowardLaneLimit);
    }

    public sealed class ExistingMonsterPrefabRuntimeMapping : ICanonicalMonsterRuntimeMapping
    {
        public const string ExistingPrefabKey = "Monster";

        public bool TryMap(string monsterType, out string prefabKey, out bool countsTowardLaneLimit)
        {
            prefabKey = ExistingPrefabKey;
            countsTowardLaneLimit = true;
            if (string.Equals(monsterType, "NORMAL", StringComparison.Ordinal)
                || string.Equals(monsterType, "ELITE", StringComparison.Ordinal)) return true;

            if (string.Equals(monsterType, "WAVE_BOSS", StringComparison.Ordinal))
            {
                countsTowardLaneLimit = false;
                return true;
            }

            prefabKey = null;
            countsTowardLaneLimit = false;
            return false;
        }
    }

    public sealed class CanonicalMonsterDefinitionProvider : IMonsterDefinitionProvider
    {
        private readonly CanonicalMonsterRegistry _registry;

        public CanonicalMonsterDefinitionProvider(CanonicalMonsterRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
        {
            definition = null;
            if (!_registry.TryGet(monsterId, out CanonicalMonsterSpec monster) || !monster.Enabled) return false;
            definition = new BattleMonsterDefinition(monster.MonsterId, monster.MonsterType, monster.BaseHp, monster.MoveSpeed, monster.PrefabKey, monster.CountsTowardLaneLimit, monster.KillGold);
            return true;
        }
    }

    public sealed class CanonicalBalanceBundle
    {
        public CanonicalBalanceManifest Manifest { get; }
        public CanonicalMonsterRegistry Monsters { get; }
        public CanonicalWaveRegistry Waves { get; }
        public CanonicalWaveSpawnRegistry WaveSpawns { get; }
        public CanonicalFieldLimitRegistry FieldLimits { get; }
        public IMonsterDefinitionProvider MonsterDefinitions { get; }
        internal BattleBalanceDocument<WaveSpecData> RuntimeWaves { get; }
        internal BattleBalanceDocument<WaveSpawnSpecData> RuntimeSpawns { get; }

        internal CanonicalBalanceBundle(CanonicalBalanceManifest manifest, CanonicalMonsterRegistry monsters, CanonicalWaveRegistry waves, CanonicalWaveSpawnRegistry waveSpawns, CanonicalFieldLimitRegistry fieldLimits, BattleBalanceDocument<WaveSpecData> runtimeWaves, BattleBalanceDocument<WaveSpawnSpecData> runtimeSpawns)
        {
            Manifest = manifest;
            Monsters = monsters;
            Waves = waves;
            WaveSpawns = waveSpawns;
            FieldLimits = fieldLimits;
            RuntimeWaves = runtimeWaves;
            RuntimeSpawns = runtimeSpawns;
            MonsterDefinitions = new CanonicalMonsterDefinitionProvider(monsters);
        }
    }
}
