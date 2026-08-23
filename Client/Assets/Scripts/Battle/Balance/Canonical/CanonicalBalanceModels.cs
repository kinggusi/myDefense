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
        public const string PlanetBattleFileName = "planet-battle-balance.json";
        public const string FieldLimitFileName = "field-limit.json";
        public const string SummonFileName = "summon-balance.json";
        public const string SummonPoolFileName = "summon-pools.json";
        public const string MutationSpecFileName = "mutation-spec.json";
        public const string MutationConfigFileName = "mutation-config.json";
        public const string InjectorPoolFileName = "injector-pool.json";
        public const string ResonanceFileName = "resonance-balance.json";
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

    public sealed class CanonicalSummonBalance
    {
        public string ModeId { get; }
        public string SummonType { get; }
        public int BaseCost { get; }
        public int CostIncreasePerUse { get; }
        public int MaxUses { get; }
        public string ResultPoolId { get; }
        public bool Enabled { get; }

        public CanonicalSummonBalance(string modeId, string summonType, int baseCost, int costIncreasePerUse, int maxUses, string resultPoolId, bool enabled)
        {
            ModeId = modeId;
            SummonType = summonType;
            BaseCost = baseCost;
            CostIncreasePerUse = costIncreasePerUse;
            MaxUses = maxUses;
            ResultPoolId = resultPoolId;
            Enabled = enabled;
        }

        public bool TryGetCost(int useCount, out int cost)
        {
            cost = 0;
            if (!Enabled || useCount < 0 || (MaxUses >= 0 && useCount >= MaxUses)) return false;
            long value = (long)BaseCost + (long)CostIncreasePerUse * useCount;
            if (value <= 0 || value > int.MaxValue) return false;
            cost = (int)value;
            return true;
        }
    }

    public sealed class CanonicalSummonPoolEntry
    {
        public string Grade { get; }
        public int Weight { get; }
        public IReadOnlyList<long> AlienIds { get; }

        public CanonicalSummonPoolEntry(string grade, int weight, IEnumerable<long> alienIds)
        {
            Grade = grade;
            Weight = weight;
            AlienIds = Array.AsReadOnly(new List<long>(alienIds ?? Array.Empty<long>()).ToArray());
        }
    }

    public sealed class CanonicalSummonPool
    {
        public string PoolId { get; }
        public string Name { get; }
        public bool Active { get; }
        public IReadOnlyList<CanonicalSummonPoolEntry> Entries { get; }

        public CanonicalSummonPool(string poolId, string name, bool active, IEnumerable<CanonicalSummonPoolEntry> entries)
        {
            PoolId = poolId;
            Name = name;
            Active = active;
            Entries = Array.AsReadOnly(new List<CanonicalSummonPoolEntry>(entries ?? Array.Empty<CanonicalSummonPoolEntry>()).ToArray());
        }
    }

    public sealed class CanonicalMutationSpec
    {
        public string MutationType { get; }
        public bool Enabled { get; }
        public bool InjectorEnabled { get; }
        public bool RandomActivationEnabled { get; }
        public int Weight { get; }
        public float AttackMultiplier { get; }
        public float MpMultiplier { get; }
        public float AttackSpeedMultiplier { get; }
        public float RangeMultiplier { get; }
        public float GoldMultiplier { get; }
        public string Mechanic { get; }
        public float SplashRadius { get; }
        public float SplashDamageMultiplier { get; }
        public float BossDamageMultiplier { get; }
        public float DotDamageMultiplier { get; }
        public int DotTickCount { get; }
        public float DotTickIntervalSeconds { get; }
        public float SlowMultiplier { get; }
        public float SlowDurationSeconds { get; }
        public int GoldPerHit { get; }
        public float GambleSuccessChance { get; }
        public float GambleSuccessMultiplier { get; }
        public float GambleFailureMultiplier { get; }

        public CanonicalMutationSpec(string mutationType, bool enabled, bool injectorEnabled, bool randomActivationEnabled,
            int weight, float attackMultiplier, float mpMultiplier, float attackSpeedMultiplier, float rangeMultiplier, float goldMultiplier,
            string mechanic = "NONE", float splashRadius = 0f, float splashDamageMultiplier = 0f,
            float bossDamageMultiplier = 1f, float dotDamageMultiplier = 0f, int dotTickCount = 0,
            float dotTickIntervalSeconds = 0f, float slowMultiplier = 1f, float slowDurationSeconds = 0f,
            int goldPerHit = 0, float gambleSuccessChance = 0f, float gambleSuccessMultiplier = 1f,
            float gambleFailureMultiplier = 1f)
        {
            MutationType = mutationType; Enabled = enabled; InjectorEnabled = injectorEnabled; RandomActivationEnabled = randomActivationEnabled;
            Weight = weight; AttackMultiplier = attackMultiplier; MpMultiplier = mpMultiplier; AttackSpeedMultiplier = attackSpeedMultiplier;
            RangeMultiplier = rangeMultiplier; GoldMultiplier = goldMultiplier;
            Mechanic = string.IsNullOrWhiteSpace(mechanic) ? "NONE" : mechanic.Trim().ToUpperInvariant();
            SplashRadius = splashRadius; SplashDamageMultiplier = splashDamageMultiplier;
            BossDamageMultiplier = bossDamageMultiplier; DotDamageMultiplier = dotDamageMultiplier;
            DotTickCount = dotTickCount; DotTickIntervalSeconds = dotTickIntervalSeconds;
            SlowMultiplier = slowMultiplier; SlowDurationSeconds = slowDurationSeconds;
            GoldPerHit = goldPerHit; GambleSuccessChance = gambleSuccessChance;
            GambleSuccessMultiplier = gambleSuccessMultiplier; GambleFailureMultiplier = gambleFailureMultiplier;
        }
    }

    public sealed class CanonicalMutationConfig
    {
        public string ModeId { get; }
        public int InitialActivationCost { get; }
        public int RerollCost1 { get; }
        public int RerollCost2 { get; }
        public int RerollCost3 { get; }
        public int RerollCost4 { get; }
        public int RerollCostAfterMax { get; }
        public int InjectorReplaceCost { get; }

        public CanonicalMutationConfig(string modeId, int initialActivationCost, int rerollCost1, int rerollCost2,
            int rerollCost3, int rerollCost4, int rerollCostAfterMax, int injectorReplaceCost)
        {
            ModeId = modeId; InitialActivationCost = initialActivationCost; RerollCost1 = rerollCost1; RerollCost2 = rerollCost2;
            RerollCost3 = rerollCost3; RerollCost4 = rerollCost4; RerollCostAfterMax = rerollCostAfterMax; InjectorReplaceCost = injectorReplaceCost;
        }
    }

    public sealed class CanonicalInjectorPoolEntry
    {
        public string PoolId { get; }
        public string PoolName { get; }
        public bool Active { get; }
        public string MutationType { get; }
        public int Weight { get; }
        public string ResultType { get; }

        public CanonicalInjectorPoolEntry(string poolId, string poolName, bool active, string mutationType, int weight, string resultType)
        {
            PoolId = poolId; PoolName = poolName; Active = active; MutationType = mutationType; Weight = weight; ResultType = resultType;
        }
    }

    public enum CanonicalResonanceTrack
    {
        NORMAL = 0,
        MYTHIC = 1
    }

    public sealed class CanonicalResonanceLevel
    {
        public CanonicalResonanceTrack Track { get; }
        public int Level { get; }
        public int RequiredGold { get; }
        public float AttackMultiplier { get; }
        public float AttackSpeedMultiplier { get; }
        public float RangeMultiplier { get; }

        public CanonicalResonanceLevel(
            CanonicalResonanceTrack track,
            int level,
            int requiredGold,
            float attackMultiplier,
            float attackSpeedMultiplier,
            float rangeMultiplier)
        {
            Track = track;
            Level = level;
            RequiredGold = requiredGold;
            AttackMultiplier = attackMultiplier;
            AttackSpeedMultiplier = attackSpeedMultiplier;
            RangeMultiplier = rangeMultiplier;
        }
    }

    public sealed class CanonicalResonanceRegistry
    {
        public const int MaxLevel = 5;
        private readonly Dictionary<CanonicalResonanceTrack, CanonicalResonanceLevel[]> _levels;

        public CanonicalResonanceRegistry(IEnumerable<CanonicalResonanceLevel> levels)
        {
            _levels = new Dictionary<CanonicalResonanceTrack, CanonicalResonanceLevel[]>();
            foreach (CanonicalResonanceTrack track in Enum.GetValues(typeof(CanonicalResonanceTrack)))
            {
                var ordered = new List<CanonicalResonanceLevel>();
                foreach (CanonicalResonanceLevel level in levels ?? Array.Empty<CanonicalResonanceLevel>())
                    if (level != null && level.Track == track) ordered.Add(level);
                ordered.Sort((left, right) => left.Level.CompareTo(right.Level));
                _levels[track] = ordered.ToArray();
            }
        }

        public bool IsComplete => _levels[CanonicalResonanceTrack.NORMAL].Length == MaxLevel
                                  && _levels[CanonicalResonanceTrack.MYTHIC].Length == MaxLevel;

        public bool TryGet(CanonicalResonanceTrack track, int level, out CanonicalResonanceLevel value)
        {
            value = null;
            if (level < 1 || level > MaxLevel || !_levels.TryGetValue(track, out CanonicalResonanceLevel[] levels))
                return false;
            CanonicalResonanceLevel candidate = levels[level - 1];
            if (candidate.Level != level) return false;
            value = candidate;
            return true;
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

    public sealed class CanonicalPlanetBattle
    {
        public string MapId { get; }
        public int Order { get; }
        public float HpMultiplier { get; }
        public float SpeedMultiplier { get; }
        public float BossHpMultiplier { get; }
        public bool Enabled { get; }

        public CanonicalPlanetBattle(string mapId, int order, float hpMultiplier, float speedMultiplier, float bossHpMultiplier, bool enabled)
        {
            MapId = mapId;
            Order = order;
            HpMultiplier = hpMultiplier;
            SpeedMultiplier = speedMultiplier;
            BossHpMultiplier = bossHpMultiplier;
            Enabled = enabled;
        }
    }

    public sealed class CanonicalPlanetBattleRegistry
    {
        private readonly Dictionary<string, CanonicalPlanetBattle> _byMapId;
        private readonly IReadOnlyList<CanonicalPlanetBattle> _all;

        internal CanonicalPlanetBattleRegistry(IEnumerable<CanonicalPlanetBattle> planets)
        {
            var sorted = new List<CanonicalPlanetBattle>(planets);
            sorted.Sort((left, right) => left.Order.CompareTo(right.Order));
            _all = Array.AsReadOnly(sorted.ToArray());
            _byMapId = new Dictionary<string, CanonicalPlanetBattle>(StringComparer.Ordinal);
            foreach (CanonicalPlanetBattle planet in sorted) _byMapId.Add(planet.MapId, planet);
        }

        public IReadOnlyList<CanonicalPlanetBattle> All => _all;

        public bool TryGet(string mapId, out CanonicalPlanetBattle planet)
        {
            planet = null;
            return mapId != null && _byMapId.TryGetValue(mapId, out planet) && planet.Enabled;
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
        public CanonicalPlanetBattleRegistry PlanetBattles { get; }
        public CanonicalSummonBalance Summon { get; }
        public IReadOnlyDictionary<string, CanonicalSummonPool> SummonPools { get; }
        public IReadOnlyList<CanonicalMutationSpec> MutationSpecs { get; }
        public CanonicalMutationConfig MutationConfig { get; }
        public IReadOnlyList<CanonicalInjectorPoolEntry> InjectorPool { get; }
        public CanonicalResonanceRegistry Resonance { get; }
        public IMonsterDefinitionProvider MonsterDefinitions { get; }
        internal BattleBalanceDocument<WaveSpecData> RuntimeWaves { get; }
        internal BattleBalanceDocument<WaveSpawnSpecData> RuntimeSpawns { get; }

        internal CanonicalBalanceBundle(CanonicalBalanceManifest manifest, CanonicalMonsterRegistry monsters, CanonicalWaveRegistry waves, CanonicalWaveSpawnRegistry waveSpawns, CanonicalFieldLimitRegistry fieldLimits, CanonicalPlanetBattleRegistry planetBattles, CanonicalSummonBalance summon, IReadOnlyDictionary<string, CanonicalSummonPool> summonPools, IReadOnlyList<CanonicalMutationSpec> mutationSpecs, CanonicalMutationConfig mutationConfig, IReadOnlyList<CanonicalInjectorPoolEntry> injectorPool, CanonicalResonanceRegistry resonance, BattleBalanceDocument<WaveSpecData> runtimeWaves, BattleBalanceDocument<WaveSpawnSpecData> runtimeSpawns)
        {
            Manifest = manifest;
            Monsters = monsters;
            Waves = waves;
            WaveSpawns = waveSpawns;
            FieldLimits = fieldLimits;
            PlanetBattles = planetBattles;
            Summon = summon;
            SummonPools = summonPools ?? new Dictionary<string, CanonicalSummonPool>(StringComparer.Ordinal);
            MutationSpecs = mutationSpecs ?? Array.Empty<CanonicalMutationSpec>();
            MutationConfig = mutationConfig;
            InjectorPool = injectorPool ?? Array.Empty<CanonicalInjectorPoolEntry>();
            Resonance = resonance ?? new CanonicalResonanceRegistry(Array.Empty<CanonicalResonanceLevel>());
            RuntimeWaves = runtimeWaves;
            RuntimeSpawns = runtimeSpawns;
            MonsterDefinitions = new CanonicalMonsterDefinitionProvider(monsters);
        }
    }
}
