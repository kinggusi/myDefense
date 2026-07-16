using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Balance
{
    public static class BattleBalanceSchema
    {
        public const int Version = 1;
    }

    public sealed class BattleBalanceFileEntryData
    {
        public string ResourcePath { get; }
        public string ContentHash { get; }

        public BattleBalanceFileEntryData(string resourcePath, string contentHash)
        {
            ResourcePath = resourcePath;
            ContentHash = contentHash;
        }
    }

    public sealed class BattleBalanceManifestData
    {
        public int SchemaVersion { get; }
        public string BalanceVersion { get; }
        public string BundleHash { get; }
        public IReadOnlyList<BattleBalanceFileEntryData> Files { get; }

        public BattleBalanceManifestData(
            int schemaVersion,
            string balanceVersion,
            string bundleHash,
            IEnumerable<BattleBalanceFileEntryData> files)
        {
            SchemaVersion = schemaVersion;
            BalanceVersion = balanceVersion;
            BundleHash = bundleHash;
            Files = BattleBalanceCollections.Copy(files);
        }
    }

    public sealed class BattleBalanceDocument<T>
    {
        public int SchemaVersion { get; }
        public string BalanceVersion { get; }
        public string ContentHash { get; }
        public IReadOnlyList<T> Items { get; }

        public BattleBalanceDocument(
            int schemaVersion,
            string balanceVersion,
            string contentHash,
            IEnumerable<T> items)
        {
            SchemaVersion = schemaVersion;
            BalanceVersion = balanceVersion;
            ContentHash = contentHash;
            Items = BattleBalanceCollections.Copy(items);
        }
    }

    public sealed class WaveSpecData
    {
        public string WaveId { get; }
        public int RoundNumber { get; }
        public WaveType WaveType { get; }
        public float NextWaveDelaySeconds { get; }
        public float BossTimeLimitSeconds { get; }
        public bool Enabled { get; }

        public WaveSpecData(string waveId, int roundNumber, WaveType waveType, float nextWaveDelaySeconds, float bossTimeLimitSeconds, bool enabled)
        {
            WaveId = waveId;
            RoundNumber = roundNumber;
            WaveType = waveType;
            NextWaveDelaySeconds = nextWaveDelaySeconds;
            BossTimeLimitSeconds = bossTimeLimitSeconds;
            Enabled = enabled;
        }
    }

    public sealed class WaveSpawnSpecData
    {
        public string WaveId { get; }
        public int SpawnOrder { get; }
        public BattleLanePolicy LanePolicy { get; }
        public string MonsterId { get; }
        public int SpawnCount { get; }
        public float SpawnDelaySeconds { get; }
        public float SpawnIntervalSeconds { get; }
        public float HpMultiplier { get; }
        public float MoveSpeedMultiplier { get; }

        public WaveSpawnSpecData(string waveId, int spawnOrder, BattleLanePolicy lanePolicy, string monsterId, int spawnCount, float spawnDelaySeconds, float spawnIntervalSeconds, float hpMultiplier, float moveSpeedMultiplier)
        {
            WaveId = waveId;
            SpawnOrder = spawnOrder;
            LanePolicy = lanePolicy;
            MonsterId = monsterId;
            SpawnCount = spawnCount;
            SpawnDelaySeconds = spawnDelaySeconds;
            SpawnIntervalSeconds = spawnIntervalSeconds;
            HpMultiplier = hpMultiplier;
            MoveSpeedMultiplier = moveSpeedMultiplier;
        }
    }

    public sealed class BossPatternSpecData
    {
        public string WaveId { get; }
        public int PatternOrder { get; }
        public BossPatternType PatternType { get; }
        public BossTriggerType TriggerType { get; }
        public float TriggerValue { get; }
        public float CooldownSeconds { get; }
        public string SkillId { get; }
        public string ParameterKey { get; }
        public float ParameterValue { get; }
        public bool Enabled { get; }

        public BossPatternSpecData(string waveId, int patternOrder, BossPatternType patternType, BossTriggerType triggerType, float triggerValue, float cooldownSeconds, string skillId, string parameterKey, float parameterValue, bool enabled)
        {
            WaveId = waveId;
            PatternOrder = patternOrder;
            PatternType = patternType;
            TriggerType = triggerType;
            TriggerValue = triggerValue;
            CooldownSeconds = cooldownSeconds;
            SkillId = skillId;
            ParameterKey = parameterKey;
            ParameterValue = parameterValue;
            Enabled = enabled;
        }
    }

    public sealed class SkillSpecData
    {
        public string SkillId { get; }
        public string NameKey { get; }
        public string DescriptionKey { get; }
        public BattleSkillType SkillType { get; }
        public BattleSkillTriggerType TriggerType { get; }
        public float CooldownSeconds { get; }
        public float MpCost { get; }
        public float CastRange { get; }
        public BattleTargetPolicy TargetPolicy { get; }
        public int MaxTargetCount { get; }
        public string ProjectileId { get; }
        public string AnimationKey { get; }
        public string VfxKey { get; }
        public string SfxKey { get; }
        public bool Enabled { get; }

        public SkillSpecData(string skillId, string nameKey, string descriptionKey, BattleSkillType skillType, BattleSkillTriggerType triggerType, float cooldownSeconds, float mpCost, float castRange, BattleTargetPolicy targetPolicy, int maxTargetCount, string projectileId, string animationKey, string vfxKey, string sfxKey, bool enabled)
        {
            SkillId = skillId;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            SkillType = skillType;
            TriggerType = triggerType;
            CooldownSeconds = cooldownSeconds;
            MpCost = mpCost;
            CastRange = castRange;
            TargetPolicy = targetPolicy;
            MaxTargetCount = maxTargetCount;
            ProjectileId = projectileId;
            AnimationKey = animationKey;
            VfxKey = vfxKey;
            SfxKey = sfxKey;
            Enabled = enabled;
        }
    }

    public sealed class AlienSkillLinkData
    {
        public long AlienId { get; }
        public string SkillId { get; }
        public int SlotIndex { get; }
        public int CastPriority { get; }
        public bool Enabled { get; }

        public AlienSkillLinkData(long alienId, string skillId, int slotIndex, int castPriority, bool enabled)
        {
            AlienId = alienId;
            SkillId = skillId;
            SlotIndex = slotIndex;
            CastPriority = castPriority;
            Enabled = enabled;
        }
    }

    public sealed class ProjectileSpecData
    {
        public string ProjectileId { get; }
        public string PrefabKey { get; }
        public ProjectileMoveType MoveType { get; }
        public float Speed { get; }
        public float LifetimeSeconds { get; }
        public float HitRadius { get; }
        public int PierceCount { get; }
        public bool DestroyOnHit { get; }
        public ProjectileLostTargetPolicy LostTargetPolicy { get; }
        public bool Enabled { get; }

        public ProjectileSpecData(string projectileId, string prefabKey, ProjectileMoveType moveType, float speed, float lifetimeSeconds, float hitRadius, int pierceCount, bool destroyOnHit, ProjectileLostTargetPolicy lostTargetPolicy, bool enabled)
        {
            ProjectileId = projectileId;
            PrefabKey = prefabKey;
            MoveType = moveType;
            Speed = speed;
            LifetimeSeconds = lifetimeSeconds;
            HitRadius = hitRadius;
            PierceCount = pierceCount;
            DestroyOnHit = destroyOnHit;
            LostTargetPolicy = lostTargetPolicy;
            Enabled = enabled;
        }
    }

    public sealed class SkillEffectSpecData
    {
        public string SkillId { get; }
        public int ExecutionOrder { get; }
        public SkillEffectTriggerPhase TriggerPhase { get; }
        public BattleSkillEffectType EffectType { get; }
        public SkillMagnitudeSource MagnitudeSource { get; }
        public float BaseMagnitude { get; }
        public float Coefficient { get; }
        public float Chance { get; }
        public float DurationSeconds { get; }
        public float TickIntervalSeconds { get; }
        public float Radius { get; }
        public int MaxStacks { get; }
        public SkillEffectStackPolicy StackPolicy { get; }
        public float BossMultiplier { get; }

        public SkillEffectSpecData(string skillId, int executionOrder, SkillEffectTriggerPhase triggerPhase, BattleSkillEffectType effectType, SkillMagnitudeSource magnitudeSource, float baseMagnitude, float coefficient, float chance, float durationSeconds, float tickIntervalSeconds, float radius, int maxStacks, SkillEffectStackPolicy stackPolicy, float bossMultiplier)
        {
            SkillId = skillId;
            ExecutionOrder = executionOrder;
            TriggerPhase = triggerPhase;
            EffectType = effectType;
            MagnitudeSource = magnitudeSource;
            BaseMagnitude = baseMagnitude;
            Coefficient = coefficient;
            Chance = chance;
            DurationSeconds = durationSeconds;
            TickIntervalSeconds = tickIntervalSeconds;
            Radius = radius;
            MaxStacks = maxStacks;
            StackPolicy = stackPolicy;
            BossMultiplier = bossMultiplier;
        }
    }

    public sealed class BattleBalanceDocuments
    {
        public BattleBalanceDocument<WaveSpecData> Waves { get; }
        public BattleBalanceDocument<WaveSpawnSpecData> Spawns { get; }
        public BattleBalanceDocument<BossPatternSpecData> BossPatterns { get; }
        public BattleBalanceDocument<SkillSpecData> Skills { get; }
        public BattleBalanceDocument<AlienSkillLinkData> AlienSkills { get; }
        public BattleBalanceDocument<ProjectileSpecData> Projectiles { get; }
        public BattleBalanceDocument<SkillEffectSpecData> SkillEffects { get; }

        public BattleBalanceDocuments(BattleBalanceDocument<WaveSpecData> waves, BattleBalanceDocument<WaveSpawnSpecData> spawns, BattleBalanceDocument<BossPatternSpecData> bossPatterns, BattleBalanceDocument<SkillSpecData> skills, BattleBalanceDocument<AlienSkillLinkData> alienSkills, BattleBalanceDocument<ProjectileSpecData> projectiles, BattleBalanceDocument<SkillEffectSpecData> skillEffects)
        {
            Waves = waves;
            Spawns = spawns;
            BossPatterns = bossPatterns;
            Skills = skills;
            AlienSkills = alienSkills;
            Projectiles = projectiles;
            SkillEffects = skillEffects;
        }
    }

    internal static class BattleBalanceCollections
    {
        public static IReadOnlyList<T> Copy<T>(IEnumerable<T> source)
        {
            if (source == null) return Array.AsReadOnly(Array.Empty<T>());
            return Array.AsReadOnly(new List<T>(source).ToArray());
        }
    }
}
