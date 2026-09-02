using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MyDefense.Battle.Balance.Canonical
{
    public interface ICanonicalBalanceFileSource
    {
        bool TryReadAllBytes(string fileName, out byte[] bytes);
    }

    public sealed class StreamingAssetsCanonicalBalanceFileSource : ICanonicalBalanceFileSource
    {
        private readonly string _rootDirectory;

        public StreamingAssetsCanonicalBalanceFileSource()
            : this(Path.Combine(Application.streamingAssetsPath, "Balance", "generated"))
        {
        }

        public StreamingAssetsCanonicalBalanceFileSource(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
        }

        public bool TryReadAllBytes(string fileName, out byte[] bytes)
        {
            bytes = null;
            if (!string.Equals(fileName, CanonicalBalanceContract.ManifestFileName, StringComparison.Ordinal)
                && !CanonicalBalanceLoader.IsSafeFileName(fileName)) return false;
            string path = Path.Combine(_rootDirectory, fileName);
            if (!File.Exists(path)) return false;
            bytes = File.ReadAllBytes(path);
            return true;
        }
    }

    public sealed class InMemoryCanonicalBalanceFileSource : ICanonicalBalanceFileSource
    {
        private readonly Dictionary<string, byte[]> _files;

        public InMemoryCanonicalBalanceFileSource(IDictionary<string, byte[]> files)
        {
            _files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            if (files == null) return;
            foreach (KeyValuePair<string, byte[]> pair in files)
                _files[pair.Key] = pair.Value == null ? null : (byte[])pair.Value.Clone();
        }

        public bool TryReadAllBytes(string fileName, out byte[] bytes)
        {
            bytes = null;
            if (!_files.TryGetValue(fileName, out byte[] stored) || stored == null) return false;
            bytes = (byte[])stored.Clone();
            return true;
        }
    }

    public sealed class CanonicalBalanceLoadResult
    {
        public CanonicalBalanceBundle Bundle { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Bundle != null && Errors.Count == 0;

        internal CanonicalBalanceLoadResult(CanonicalBalanceBundle bundle, IEnumerable<string> errors)
        {
            Bundle = bundle;
            Errors = Array.AsReadOnly(new List<string>(errors ?? Array.Empty<string>()).ToArray());
        }
    }

    public static class CanonicalBalanceLoader
    {
        private static readonly string[] RequiredRuntimeFiles =
        {
            CanonicalBalanceContract.MonsterFileName,
            CanonicalBalanceContract.WaveFileName,
            CanonicalBalanceContract.WaveSpawnFileName,
            CanonicalBalanceContract.PlanetBattleFileName,
            CanonicalBalanceContract.FieldLimitFileName,
            CanonicalBalanceContract.SummonFileName,
            CanonicalBalanceContract.SummonPoolFileName
            , CanonicalBalanceContract.MutationSpecFileName
            , CanonicalBalanceContract.MutationConfigFileName
            , CanonicalBalanceContract.InjectorPoolFileName
            , CanonicalBalanceContract.ResonanceFileName
            , CanonicalBalanceContract.DailyBattleStageFileName
        };

        public static CanonicalBalanceLoadResult Load(
            ICanonicalBalanceFileSource source,
            ICanonicalMonsterRuntimeMapping runtimeMapping,
            string expectedBalanceVersion = null)
        {
            var errors = new List<string>();
            if (source == null) errors.Add("Canonical balance file source is required.");
            if (runtimeMapping == null) errors.Add("Canonical Monster runtime mapping is required.");
            if (errors.Count > 0) return Invalid(errors);

            if (!source.TryReadAllBytes(CanonicalBalanceContract.ManifestFileName, out byte[] manifestBytes))
            {
                errors.Add("Missing canonical balance manifest: " + CanonicalBalanceContract.ManifestFileName + ".");
                return Invalid(errors);
            }

            CanonicalManifestJson manifestJson = ParseJson<CanonicalManifestJson>(manifestBytes, "canonical manifest", errors);
            if (manifestJson == null) return Invalid(errors);

            ValidateManifestHeader(manifestJson, expectedBalanceVersion, errors);
            var entries = new List<CanonicalManifestFileEntry>();
            var fileBytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            string previousName = null;
            CanonicalManifestFileJson[] declaredFiles = manifestJson.files ?? Array.Empty<CanonicalManifestFileJson>();
            foreach (CanonicalManifestFileJson declared in declaredFiles)
            {
                if (declared == null || !IsSafeFileName(declared.name))
                {
                    errors.Add("Canonical manifest contains an unsafe file name: " + (declared?.name ?? "<null>") + ".");
                    continue;
                }
                if (!names.Add(declared.name))
                {
                    errors.Add("Canonical manifest contains duplicate file entry: " + declared.name + ".");
                    continue;
                }
                if (previousName != null && string.CompareOrdinal(previousName, declared.name) >= 0)
                    errors.Add("Canonical manifest files must be sorted by name.");
                previousName = declared.name;

                if (!source.TryReadAllBytes(declared.name, out byte[] bytes))
                {
                    errors.Add("Canonical balance file is missing: " + declared.name + ".");
                    continue;
                }

                string actualHash = Sha256(bytes);
                if (declared.size != bytes.LongLength)
                    errors.Add("Canonical balance size mismatch: " + declared.name + ".");
                if (!string.Equals(declared.sha256, actualHash, StringComparison.Ordinal))
                    errors.Add("Canonical balance SHA-256 mismatch: " + declared.name + ".");

                entries.Add(new CanonicalManifestFileEntry(declared.name, actualHash, bytes.LongLength));
                fileBytes[declared.name] = bytes;
            }

            foreach (string required in RequiredRuntimeFiles)
            {
                if (!names.Contains(required)) errors.Add("Canonical manifest is missing required file: " + required + ".");
            }

            string actualContentHash = ComputeContentHash(entries);
            if (!string.Equals(manifestJson.contentHash, actualContentHash, StringComparison.Ordinal))
                errors.Add("Canonical manifest contentHash mismatch.");
            string actualBalanceVersion = CanonicalBalanceContract.SchemaVersion + "-" + actualContentHash.Substring(0, 16);
            if (!string.Equals(manifestJson.balanceVersion, actualBalanceVersion, StringComparison.Ordinal))
                errors.Add("Canonical manifest balanceVersion mismatch.");
            if (errors.Count > 0) return Invalid(errors);

            MonsterDocumentJson monsterDocument = ParseDocument<MonsterDocumentJson>(fileBytes, CanonicalBalanceContract.MonsterFileName, errors);
            WaveDocumentJson waveDocument = ParseDocument<WaveDocumentJson>(fileBytes, CanonicalBalanceContract.WaveFileName, errors);
            WaveSpawnDocumentJson spawnDocument = ParseDocument<WaveSpawnDocumentJson>(fileBytes, CanonicalBalanceContract.WaveSpawnFileName, errors);
            PlanetBattleDocumentJson planetDocument = ParseDocument<PlanetBattleDocumentJson>(fileBytes, CanonicalBalanceContract.PlanetBattleFileName, errors);
            FieldLimitDocumentJson fieldLimitDocument = ParseDocument<FieldLimitDocumentJson>(fileBytes, CanonicalBalanceContract.FieldLimitFileName, errors);
            SummonDocumentJson summonDocument = names.Contains(CanonicalBalanceContract.SummonFileName)
                ? ParseDocument<SummonDocumentJson>(fileBytes, CanonicalBalanceContract.SummonFileName, errors)
                : null;
            SummonPoolDocumentJson summonPoolDocument = ParseDocument<SummonPoolDocumentJson>(fileBytes, CanonicalBalanceContract.SummonPoolFileName, errors);
            MutationSpecJson[] mutationSpecDocument = ParseArrayJson<MutationSpecJson>(fileBytes[CanonicalBalanceContract.MutationSpecFileName], CanonicalBalanceContract.MutationSpecFileName, errors);
            MutationConfigJson mutationConfigDocument = ParseJson<MutationConfigJson>(fileBytes[CanonicalBalanceContract.MutationConfigFileName], CanonicalBalanceContract.MutationConfigFileName, errors);
            InjectorPoolJson[] injectorPoolDocument = ParseArrayJson<InjectorPoolJson>(fileBytes[CanonicalBalanceContract.InjectorPoolFileName], CanonicalBalanceContract.InjectorPoolFileName, errors);
            ResonanceJson[] resonanceDocument = names.Contains(CanonicalBalanceContract.ResonanceFileName)
                ? ParseArrayJson<ResonanceJson>(fileBytes[CanonicalBalanceContract.ResonanceFileName], CanonicalBalanceContract.ResonanceFileName, errors)
                : Array.Empty<ResonanceJson>();
            DailyBattleStageDocumentJson dailyBattleDocument = ParseDocument<DailyBattleStageDocumentJson>(
                fileBytes, CanonicalBalanceContract.DailyBattleStageFileName, errors);
            if (errors.Count > 0) return Invalid(errors);

            List<CanonicalMonsterSpec> monsters = BuildMonsters(monsterDocument.monsters, runtimeMapping, errors);
            List<CanonicalWaveSpec> waves = BuildWaves(waveDocument.waves, errors);
            List<CanonicalWaveSpawn> spawns = BuildSpawns(spawnDocument.spawns, errors);
            List<CanonicalPlanetBattle> planets = BuildPlanetBattles(planetDocument.planets, errors);
            List<CanonicalFieldLimit> fieldLimits = BuildFieldLimits(fieldLimitDocument.fieldLimits, errors);
            CanonicalSummonBalance summon = BuildSummon(summonDocument?.summons, errors);
            IReadOnlyDictionary<string, CanonicalSummonPool> summonPools = BuildSummonPools(summonPoolDocument?.pools, errors);
            List<CanonicalMutationSpec> mutationSpecs = BuildMutationSpecs(mutationSpecDocument, errors);
            CanonicalMutationConfig mutationConfig = BuildMutationConfig(mutationConfigDocument, errors);
            List<CanonicalInjectorPoolEntry> injectorPool = BuildInjectorPool(injectorPoolDocument, errors);
            CanonicalResonanceRegistry resonance = BuildResonance(resonanceDocument, errors);
            CanonicalDailyBattleStageRegistry dailyBattleStages = BuildDailyBattleStages(
                dailyBattleDocument?.stages, monsters, errors);
            ValidateRelationships(monsters, waves, spawns, errors);
            if (errors.Count > 0) return Invalid(errors);

            var monsterRegistry = new CanonicalMonsterRegistry(monsters);
            var waveRegistry = new CanonicalWaveRegistry(waves);
            var spawnRegistry = new CanonicalWaveSpawnRegistry(spawns);
            var fieldLimitRegistry = new CanonicalFieldLimitRegistry(fieldLimits);
            var planetRegistry = new CanonicalPlanetBattleRegistry(planets);
            var runtimeWaves = new List<WaveSpecData>();
            var runtimeSpawns = new List<WaveSpawnSpecData>();
            foreach (CanonicalWaveSpec wave in waves)
            {
                runtimeWaves.Add(new WaveSpecData(
                    wave.RuntimeWaveId,
                    wave.Wave,
                    wave.IsBossWave ? WaveType.BOSS : WaveType.REGULAR,
                    wave.InterWaveDelaySeconds,
                    wave.BossTimeLimitSeconds,
                    wave.Enabled));

                IReadOnlyList<CanonicalWaveSpawn> group = spawnRegistry.GetByGroup(wave.SpawnGroupId);
                foreach (CanonicalWaveSpawn spawn in group)
                {
                    runtimeSpawns.Add(new WaveSpawnSpecData(
                        wave.RuntimeWaveId,
                        spawn.Order,
                        spawn.LanePolicy == CanonicalLanePolicy.EACH_FIELD
                            ? BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE
                            : BattleLanePolicy.BOSS_SHARED,
                        spawn.MonsterId,
                        spawn.SpawnCountPerField,
                        spawn.StartDelaySeconds,
                        spawn.SpawnIntervalSeconds,
                        wave.HpMultiplier,
                        1f,
                        spawn.SpawnGroupId));
                }
            }

            var manifest = new CanonicalBalanceManifest(manifestJson.schemaVersion, manifestJson.balanceVersion, manifestJson.contentHash, entries);
            var runtimeWaveDocument = new BattleBalanceDocument<WaveSpecData>(manifest.SchemaVersion, manifest.BalanceVersion, manifest.ContentHash, runtimeWaves);
            var runtimeSpawnDocument = new BattleBalanceDocument<WaveSpawnSpecData>(manifest.SchemaVersion, manifest.BalanceVersion, manifest.ContentHash, runtimeSpawns);
            return new CanonicalBalanceLoadResult(
                new CanonicalBalanceBundle(manifest, monsterRegistry, waveRegistry, spawnRegistry, fieldLimitRegistry, planetRegistry, summon, summonPools, mutationSpecs, mutationConfig, injectorPool, resonance, dailyBattleStages, runtimeWaveDocument, runtimeSpawnDocument),
                errors);
        }

        internal static bool IsSafeFileName(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName)
                && !string.Equals(fileName, CanonicalBalanceContract.ManifestFileName, StringComparison.Ordinal)
                && !fileName.Contains("..")
                && Regex.IsMatch(fileName, "^[A-Za-z0-9][A-Za-z0-9._-]*\\.json$");
        }

        private static void ValidateManifestHeader(CanonicalManifestJson manifest, string expectedBalanceVersion, List<string> errors)
        {
            if (manifest.schemaVersion != CanonicalBalanceContract.SchemaVersion)
                errors.Add("Unsupported canonical schemaVersion: " + manifest.schemaVersion + ".");
            if (string.IsNullOrWhiteSpace(manifest.balanceVersion)) errors.Add("Canonical balanceVersion is required.");
            if (!string.IsNullOrWhiteSpace(expectedBalanceVersion)
                && !string.Equals(manifest.balanceVersion, expectedBalanceVersion, StringComparison.Ordinal))
                errors.Add("Canonical balanceVersion does not match the expected session version.");
            if (!IsSha256(manifest.contentHash)) errors.Add("Canonical contentHash must be a lowercase SHA-256 value.");
            if (manifest.files == null || manifest.files.Length == 0) errors.Add("Canonical manifest must contain files.");
        }

        private static List<CanonicalMonsterSpec> BuildMonsters(MonsterJson[] source, ICanonicalMonsterRuntimeMapping mapping, List<string> errors)
        {
            var result = new List<CanonicalMonsterSpec>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (MonsterJson raw in source ?? Array.Empty<MonsterJson>())
            {
                string id = raw?.monsterId;
                if (string.IsNullOrWhiteSpace(id)) { errors.Add("MonsterSpec.monsterId is required."); continue; }
                if (!ids.Add(id)) { errors.Add("Duplicate canonical monsterId: " + id + "."); continue; }
                if (raw.baseHp <= 0f) errors.Add("MonsterSpec baseHp must be positive: " + id + ".");
                if (raw.moveSpeed <= 0f) errors.Add("MonsterSpec moveSpeed must be positive: " + id + ".");
                if (raw.killGold < 0) errors.Add("MonsterSpec killGold must be non-negative: " + id + ".");
                if (!mapping.TryMap(raw.monsterType, out string prefabKey, out bool countsTowardLaneLimit))
                {
                    errors.Add("Unsupported canonical monsterType: " + (raw.monsterType ?? "<null>") + ".");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(prefabKey)) errors.Add("Runtime prefabKey mapping is missing: " + id + ".");
                result.Add(new CanonicalMonsterSpec(id, raw.monsterType, raw.baseHp, raw.moveSpeed, raw.killGold, raw.enabled, prefabKey, countsTowardLaneLimit));
            }
            if (result.Count == 0) errors.Add("Canonical MonsterSpec must not be empty.");
            return result;
        }

        private static List<CanonicalMutationSpec> BuildMutationSpecs(MutationSpecJson[] source, List<string> errors)
        {
            var result = new List<CanonicalMutationSpec>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (MutationSpecJson raw in source ?? Array.Empty<MutationSpecJson>())
            {
                if (raw == null || string.IsNullOrWhiteSpace(raw.mutationType) || !ids.Add(raw.mutationType) || raw.weight <= 0)
                { errors.Add("MutationSpec must have unique positive-weight mutationType."); continue; }
                if (raw.attackMultiplier <= 0 || raw.mpMultiplier <= 0 || raw.attackSpeedMultiplier <= 0 || raw.rangeMultiplier <= 0 || raw.goldMultiplier <= 0)
                    errors.Add("MutationSpec multipliers must be positive: " + raw.mutationType + ".");
                ValidateMutationMechanic(raw, errors);
                result.Add(new CanonicalMutationSpec(raw.mutationType, raw.enabled, raw.injectorEnabled, raw.randomActivationEnabled, raw.weight,
                    raw.attackMultiplier, raw.mpMultiplier, raw.attackSpeedMultiplier, raw.rangeMultiplier, raw.goldMultiplier,
                    raw.mechanic, raw.splashRadius, raw.splashDamageMultiplier, raw.bossDamageMultiplier,
                    raw.dotDamageMultiplier, raw.dotTickCount, raw.dotTickIntervalSeconds,
                    raw.slowMultiplier, raw.slowDurationSeconds, raw.goldPerHit,
                    raw.gambleSuccessChance, raw.gambleSuccessMultiplier, raw.gambleFailureMultiplier));
            }
            if (result.Count != 8) errors.Add("MutationSpec must contain exactly 8 rows.");
            return result;
        }

        private static void ValidateMutationMechanic(MutationSpecJson raw, List<string> errors)
        {
            string expected = raw.mutationType switch
            {
                "GIANT" => "SPLASH",
                "BERSERK" => "BOSS_SINGLE",
                "SWIFT" => "ATTACK_SPEED",
                "TOXIC" => "DOT",
                "GREEDY" => "ECONOMY",
                "OBESE" => "GAMBLE",
                "FROZEN" => "SLOW",
                "BLANK" => "NONE",
                _ => null
            };
            if (expected == null || !string.Equals(raw.mechanic, expected, StringComparison.Ordinal))
                errors.Add("MutationSpec mechanic does not match mutationType: " + raw.mutationType + ".");
            if (raw.bossDamageMultiplier <= 0f || raw.slowMultiplier <= 0f
                || raw.gambleSuccessMultiplier <= 0f || raw.gambleFailureMultiplier <= 0f
                || raw.splashRadius < 0f || raw.splashDamageMultiplier < 0f
                || raw.dotDamageMultiplier < 0f || raw.dotTickCount < 0 || raw.dotTickIntervalSeconds < 0f
                || raw.slowDurationSeconds < 0f || raw.goldPerHit < 0
                || raw.gambleSuccessChance < 0f || raw.gambleSuccessChance > 1f)
                errors.Add("MutationSpec mechanic values are invalid: " + raw.mutationType + ".");
            if (expected == "SPLASH" && (raw.splashRadius <= 0f || raw.splashDamageMultiplier <= 0f))
                errors.Add("GIANT requires positive splash settings.");
            if (expected == "BOSS_SINGLE" && raw.bossDamageMultiplier <= 1f)
                errors.Add("BERSERK requires bossDamageMultiplier greater than one.");
            if (expected == "DOT" && (raw.dotDamageMultiplier <= 0f || raw.dotTickCount <= 0 || raw.dotTickIntervalSeconds <= 0f))
                errors.Add("TOXIC requires positive DoT settings.");
            if (expected == "SLOW" && (raw.slowMultiplier >= 1f || raw.slowDurationSeconds <= 0f))
                errors.Add("FROZEN requires a slowing multiplier and duration.");
            if (expected == "ECONOMY" && raw.goldPerHit <= 0)
                errors.Add("GREEDY requires positive goldPerHit.");
            if (expected == "GAMBLE" && (raw.gambleSuccessChance <= 0f || raw.gambleSuccessChance >= 1f
                || raw.gambleSuccessMultiplier <= 1f || raw.gambleFailureMultiplier >= 1f))
                errors.Add("OBESE requires valid gamble success/failure settings.");
        }

        private static CanonicalMutationConfig BuildMutationConfig(MutationConfigJson raw, List<string> errors)
        {
            if (raw == null || string.IsNullOrWhiteSpace(raw.modeId) || raw.initialActivationCost < 0 || raw.rerollCost1 <= 0 || raw.rerollCost2 <= 0 || raw.rerollCost3 <= 0 || raw.rerollCost4 <= 0 || raw.rerollCostAfterMax <= 0 || raw.injectorReplaceCost < 0)
            { errors.Add("Invalid mutation config."); return null; }
            return new CanonicalMutationConfig(raw.modeId, raw.initialActivationCost, raw.rerollCost1, raw.rerollCost2, raw.rerollCost3, raw.rerollCost4, raw.rerollCostAfterMax, raw.injectorReplaceCost);
        }

        private static List<CanonicalInjectorPoolEntry> BuildInjectorPool(InjectorPoolJson[] source, List<string> errors)
        {
            var result = new List<CanonicalInjectorPoolEntry>();
            var types = new HashSet<string>(StringComparer.Ordinal);
            foreach (InjectorPoolJson raw in source ?? Array.Empty<InjectorPoolJson>())
            {
                if (raw == null || string.IsNullOrWhiteSpace(raw.mutationType) || !types.Add(raw.mutationType) || raw.weight <= 0 || !string.Equals(raw.resultType, "MUTATION_INJECTOR", StringComparison.Ordinal) || string.Equals(raw.mutationType, "BLANK", StringComparison.Ordinal))
                { errors.Add("Invalid injector pool row."); continue; }
                result.Add(new CanonicalInjectorPoolEntry(raw.poolId, raw.poolName, raw.poolActive, raw.mutationType, raw.weight, raw.resultType));
            }
            if (result.Count == 0) errors.Add("Injector pool must not be empty.");
            return result;
        }

        private static CanonicalResonanceRegistry BuildResonance(ResonanceJson[] source, List<string> errors)
        {
            if (source == null || source.Length == 0)
                return new CanonicalResonanceRegistry(Array.Empty<CanonicalResonanceLevel>());

            var result = new List<CanonicalResonanceLevel>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (ResonanceJson raw in source)
            {
                if (raw == null
                    || !Enum.TryParse(raw.track, false, out CanonicalResonanceTrack track)
                    || raw.level < 1 || raw.level > CanonicalResonanceRegistry.MaxLevel
                    || !keys.Add(raw.track + ":" + raw.level)
                    || raw.requiredGold <= 0
                    || raw.attackMultiplier <= 1f
                    || raw.attackSpeedMultiplier <= 1f
                    || Math.Abs(raw.rangeMultiplier - 1f) > 0.0001f
                    || !raw.enabled)
                {
                    errors.Add("Invalid ResonanceBalance row: " + (raw?.track ?? "<null>") + ":" + (raw?.level ?? 0) + ".");
                    continue;
                }
                result.Add(new CanonicalResonanceLevel(track, raw.level, raw.requiredGold,
                    raw.attackMultiplier, raw.attackSpeedMultiplier, raw.rangeMultiplier));
            }

            var registry = new CanonicalResonanceRegistry(result);
            if (!registry.IsComplete || result.Count != 10)
                errors.Add("ResonanceBalance must contain NORMAL and MYTHIC levels 1 through 5 exactly once.");

            foreach (CanonicalResonanceTrack track in Enum.GetValues(typeof(CanonicalResonanceTrack)))
            {
                int previousCost = 0;
                float previousAttack = 1f;
                float previousSpeed = 1f;
                for (int level = 1; level <= CanonicalResonanceRegistry.MaxLevel; level++)
                {
                    if (!registry.TryGet(track, level, out CanonicalResonanceLevel value)) continue;
                    if (value.RequiredGold <= previousCost
                        || value.AttackMultiplier <= previousAttack
                        || value.AttackSpeedMultiplier <= previousSpeed)
                        errors.Add("ResonanceBalance costs and multipliers must increase by level: " + track + ".");
                    previousCost = value.RequiredGold;
                    previousAttack = value.AttackMultiplier;
                    previousSpeed = value.AttackSpeedMultiplier;
                }
            }
            return registry;
        }

        private static CanonicalSummonBalance BuildSummon(SummonJson[] source, List<string> errors)
        {
            if (source == null || source.Length == 0)
            {
                errors.Add("Canonical summon balance must contain a KIDNAP row.");
                return null;
            }
            SummonJson raw = source.FirstOrDefault(item => item != null
                && string.Equals(item.modeId, CanonicalBalanceContract.DefaultModeId, StringComparison.Ordinal)
                && string.Equals(item.summonType, "KIDNAP", StringComparison.Ordinal));
            if (raw == null)
            {
                errors.Add("Canonical summon balance is missing COOP_STANDARD KIDNAP.");
                return null;
            }
            if (raw.baseCost <= 0 || raw.costIncreasePerUse < 0 || raw.maxUses < -1 || string.IsNullOrWhiteSpace(raw.resultPoolId))
            {
                errors.Add("Invalid canonical summon balance.");
                return null;
            }
            return new CanonicalSummonBalance(raw.modeId, raw.summonType, raw.baseCost, raw.costIncreasePerUse, raw.maxUses, raw.resultPoolId, raw.enabled);
        }

        private static IReadOnlyDictionary<string, CanonicalSummonPool> BuildSummonPools(SummonPoolJson[] source, List<string> errors)
        {
            var result = new Dictionary<string, CanonicalSummonPool>(StringComparer.Ordinal);
            foreach (SummonPoolJson raw in source ?? Array.Empty<SummonPoolJson>())
            {
                if (raw == null || string.IsNullOrWhiteSpace(raw.poolId) || result.ContainsKey(raw.poolId))
                {
                    errors.Add("SummonPool.poolId must be unique and non-empty.");
                    continue;
                }
                var entries = new List<CanonicalSummonPoolEntry>();
                int totalWeight = 0;
                foreach (SummonPoolEntryJson entry in raw.entries ?? Array.Empty<SummonPoolEntryJson>())
                {
                    if (entry == null || !string.Equals(entry.grade, "NORMAL", StringComparison.Ordinal)
                        || entry.weight <= 0 || entry.alienIds == null || entry.alienIds.Length == 0)
                    {
                        errors.Add("Battle SummonPool permits NORMAL entries with positive weight only.");
                        continue;
                    }
                    totalWeight += entry.weight;
                    entries.Add(new CanonicalSummonPoolEntry(entry.grade, entry.weight, entry.alienIds));
                }
                if (raw.active && (entries.Count == 0 || totalWeight != 10000))
                    errors.Add("Active SummonPool weights must total 10000: " + raw.poolId + ".");
                result[raw.poolId] = new CanonicalSummonPool(raw.poolId, raw.name, raw.active, entries);
            }
            if (!result.ContainsKey("STANDARD_SUMMON_POOL")) errors.Add("Missing STANDARD_SUMMON_POOL.");
            return result;
        }

        private static List<CanonicalWaveSpec> BuildWaves(WaveJson[] source, List<string> errors)
        {
            var result = new List<CanonicalWaveSpec>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (WaveJson raw in source ?? Array.Empty<WaveJson>())
            {
                if (raw == null) { errors.Add("WaveSpec contains null row."); continue; }
                string key = raw.modeId + ":" + raw.wave;
                if (string.IsNullOrWhiteSpace(raw.modeId)) errors.Add("WaveSpec.modeId is required.");
                if (!keys.Add(key)) errors.Add("Duplicate canonical WaveSpec key: " + key + ".");
                if (raw.wave < 1) errors.Add("WaveSpec.wave must be positive: " + key + ".");
                if (raw.hpMultiplier <= 0f) errors.Add("WaveSpec.hpMultiplier must be positive: " + key + ".");
                if (raw.interWaveDelaySeconds < 0f) errors.Add("WaveSpec.interWaveDelaySeconds must be non-negative: " + key + ".");
                if (raw.isBossWave ? raw.bossTimeLimitSeconds <= 0f : raw.bossTimeLimitSeconds != 0f)
                    errors.Add("WaveSpec.bossTimeLimitSeconds does not match isBossWave: " + key + ".");
                if (string.IsNullOrWhiteSpace(raw.spawnGroupId)) errors.Add("WaveSpec.spawnGroupId is required: " + key + ".");
                else if (!groups.Add(raw.spawnGroupId)) errors.Add("WaveSpec.spawnGroupId must be unique: " + raw.spawnGroupId + ".");
                result.Add(new CanonicalWaveSpec(raw.modeId, raw.wave, raw.hpMultiplier, raw.interWaveDelaySeconds, raw.isBossWave, raw.bossTimeLimitSeconds, raw.spawnGroupId, raw.enabled));
            }
            if (result.Count == 0) errors.Add("Canonical WaveSpec must not be empty.");
            ValidateStandardWaveProgression(result, errors);
            return result;
        }

        private static void ValidateStandardWaveProgression(List<CanonicalWaveSpec> waves, List<string> errors)
        {
            List<CanonicalWaveSpec> standard = waves
                .FindAll(wave => wave != null
                    && wave.Enabled
                    && string.Equals(wave.ModeId, CanonicalBalanceContract.DefaultModeId, StringComparison.Ordinal));
            if (standard.Count != 80)
            {
                errors.Add("COOP_STANDARD must contain exactly 80 enabled waves.");
                return;
            }

            standard.Sort((left, right) => left.Wave.CompareTo(right.Wave));
            for (int index = 0; index < standard.Count; index++)
            {
                int expectedWave = index + 1;
                CanonicalWaveSpec wave = standard[index];
                if (wave.Wave != expectedWave)
                    errors.Add("COOP_STANDARD waves must be continuous from 1 through 80.");
                bool expectedBoss = expectedWave % 10 == 0;
                if (wave.IsBossWave != expectedBoss)
                    errors.Add("COOP_STANDARD Boss waves must be exactly every tenth wave: " + expectedWave + ".");
            }
        }

        private static List<CanonicalWaveSpawn> BuildSpawns(WaveSpawnJson[] source, List<string> errors)
        {
            var result = new List<CanonicalWaveSpawn>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (WaveSpawnJson raw in source ?? Array.Empty<WaveSpawnJson>())
            {
                if (raw == null) { errors.Add("WaveSpawn contains null row."); continue; }
                string key = raw.spawnGroupId + ":" + raw.order;
                if (string.IsNullOrWhiteSpace(raw.spawnGroupId)) errors.Add("WaveSpawn.spawnGroupId is required.");
                if (!keys.Add(key)) errors.Add("Duplicate canonical WaveSpawn key: " + key + ".");
                if (raw.order < 1) errors.Add("WaveSpawn.order must be positive: " + key + ".");
                if (string.IsNullOrWhiteSpace(raw.monsterId)) errors.Add("WaveSpawn.monsterId is required: " + key + ".");
                if (raw.spawnCountPerField < 1) errors.Add("WaveSpawn.spawnCountPerField must be positive: " + key + ".");
                if (raw.startDelaySeconds < 0f || raw.spawnIntervalSeconds < 0f) errors.Add("WaveSpawn delays must be non-negative: " + key + ".");
                if (!Enum.TryParse(raw.lanePolicy, false, out CanonicalLanePolicy lanePolicy))
                {
                    errors.Add("Unsupported canonical lanePolicy: " + (raw.lanePolicy ?? "<null>") + ".");
                    continue;
                }
                result.Add(new CanonicalWaveSpawn(raw.spawnGroupId, raw.order, raw.monsterId, raw.spawnCountPerField, raw.startDelaySeconds, raw.spawnIntervalSeconds, lanePolicy));
            }
            if (result.Count == 0) errors.Add("Canonical WaveSpawn must not be empty.");
            return result;
        }

        private static List<CanonicalFieldLimit> BuildFieldLimits(FieldLimitJson[] source, List<string> errors)
        {
            var result = new List<CanonicalFieldLimit>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (FieldLimitJson raw in source ?? Array.Empty<FieldLimitJson>())
            {
                if (raw == null) { errors.Add("FieldLimit contains null row."); continue; }
                string key = raw.modeId + ":" + raw.playerCount;
                if (string.IsNullOrWhiteSpace(raw.modeId)) errors.Add("FieldLimit.modeId is required.");
                if (!keys.Add(key)) errors.Add("Duplicate canonical FieldLimit key: " + key + ".");
                if (raw.playerCount < 1 || raw.maxAliveMonsterCountPerField < 1)
                    errors.Add("FieldLimit playerCount and maxAliveMonsterCountPerField must be positive: " + key + ".");
                if (raw.warningThreshold < 0 || raw.dangerThreshold <= raw.warningThreshold || raw.dangerThreshold >= raw.maxAliveMonsterCountPerField)
                    errors.Add("FieldLimit thresholds must satisfy 0 <= warning < danger < max: " + key + ".");
                result.Add(new CanonicalFieldLimit(raw.modeId, raw.playerCount, raw.maxAliveMonsterCountPerField, raw.warningThreshold, raw.dangerThreshold));
            }
            if (result.Count == 0) errors.Add("Canonical FieldLimit must not be empty.");
            return result;
        }

        private static List<CanonicalPlanetBattle> BuildPlanetBattles(PlanetBattleJson[] source, List<string> errors)
        {
            string[] expectedIds = { "NEPTUNE", "URANUS", "SATURN", "JUPITER", "MARS", "EARTH", "VENUS", "MERCURY", "SUN" };
            float[] expectedHp = { 1f, 1.35f, 1.8f, 2.4f, 3.2f, 4.3f, 5.8f, 7.8f, 11f };
            float[] expectedSpeed = { 1f, 1.03f, 1.06f, 1.09f, 1.12f, 1.15f, 1.18f, 1.21f, 1.25f };
            var result = new List<CanonicalPlanetBattle>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlanetBattleJson raw in source ?? Array.Empty<PlanetBattleJson>())
            {
                if (raw == null) { errors.Add("PlanetBattle contains null row."); continue; }
                string mapId = raw.mapId?.Trim();
                if (string.IsNullOrWhiteSpace(mapId)) errors.Add("PlanetBattle.mapId is required.");
                else if (!ids.Add(mapId)) errors.Add("Duplicate canonical PlanetBattle mapId: " + mapId + ".");
                if (raw.order < 1 || raw.hpMultiplier <= 0f || raw.speedMultiplier <= 0f || raw.bossHpMultiplier <= 0f)
                    errors.Add("PlanetBattle order and multipliers must be positive: " + (mapId ?? "<null>") + ".");
                if (!raw.enabled) errors.Add("All canonical PlanetBattle rows must be enabled: " + (mapId ?? "<null>") + ".");
                result.Add(new CanonicalPlanetBattle(mapId, raw.order, raw.hpMultiplier, raw.speedMultiplier, raw.bossHpMultiplier, raw.enabled));
            }

            if (result.Count != expectedIds.Length)
                errors.Add("Canonical PlanetBattle must contain exactly nine enabled planets.");
            for (int index = 0; index < expectedIds.Length; index++)
            {
                CanonicalPlanetBattle planet = result.Find(value => value != null && string.Equals(value.MapId, expectedIds[index], StringComparison.Ordinal));
                if (planet == null) { errors.Add("Canonical PlanetBattle is missing mapId: " + expectedIds[index] + "."); continue; }
                if (planet.Order != index + 1
                    || Math.Abs(planet.HpMultiplier - expectedHp[index]) > 0.0001f
                    || Math.Abs(planet.SpeedMultiplier - expectedSpeed[index]) > 0.0001f
                    || Math.Abs(planet.BossHpMultiplier - 3f) > 0.0001f)
                    errors.Add("Canonical PlanetBattle policy mismatch: " + expectedIds[index] + ".");
            }
            return result;
        }

        private static CanonicalDailyBattleStageRegistry BuildDailyBattleStages(
            DailyBattleStageJson[] source,
            List<CanonicalMonsterSpec> monsters,
            List<string> errors)
        {
            DailyBattleStageJson[] rows = source ?? Array.Empty<DailyBattleStageJson>();
            var result = new List<CanonicalDailyBattleStage>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var monstersById = monsters.Where(monster => monster.Enabled)
                .ToDictionary(monster => monster.MonsterId, StringComparer.Ordinal);
            var expectedMaps = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "CULTIVATION_ZONE", "DAILY_CULTIVATION_ZONE" },
                { "MUTATION_LAB", "DAILY_MUTATION_LAB" }
            };
            int[] expectedWaves = { 3, 4, 5, 6, 7 };
            int[] expectedTimes = { 120, 150, 180, 210, 240 };

            foreach (DailyBattleStageJson raw in rows)
            {
                if (raw == null) { errors.Add("DailyBattleStage contains null row."); continue; }
                string key = raw.contentType + ":" + raw.stage + ":" + raw.wave;
                if (!expectedMaps.TryGetValue(raw.contentType ?? string.Empty, out string expectedMap)
                    || !string.Equals(raw.mapId, expectedMap, StringComparison.Ordinal)
                    || raw.stage < 1 || raw.stage > 5
                    || raw.wave < 1 || raw.wave > expectedWaves[raw.stage - 1]
                    || raw.timeLimitSeconds != expectedTimes[raw.stage - 1]
                    || !keys.Add(key)
                    || !monstersById.TryGetValue(raw.monsterSpecId ?? string.Empty, out CanonicalMonsterSpec monster)
                    || raw.spawnCount < 1 || raw.spawnIntervalSeconds < 0f
                    || raw.hpMultiplier <= 0f || raw.moveSpeedMultiplier <= 0f
                    || !raw.enabled)
                {
                    errors.Add("Invalid DailyBattleStage row: " + key + ".");
                    continue;
                }
                if (!Enum.TryParse(raw.lanePolicy, false, out CanonicalDailyBattleLanePolicy lanePolicy)
                    || !string.Equals(raw.lanePolicy, lanePolicy.ToString(), StringComparison.Ordinal)
                    || !Enum.TryParse(raw.statusEffectType, false, out CanonicalDailyBattleStatusEffect effect)
                    || !string.Equals(raw.statusEffectType, effect.ToString(), StringComparison.Ordinal)
                    || raw.statusEffectValue < 0f || raw.statusEffectValue > 0.5f
                    || (effect == CanonicalDailyBattleStatusEffect.NONE) != (Math.Abs(raw.statusEffectValue) < 0.0001f))
                {
                    errors.Add("Invalid DailyBattleStage policy/effect: " + key + ".");
                    continue;
                }
                bool finalWave = raw.wave == expectedWaves[raw.stage - 1];
                if (raw.boss != (string.Equals(raw.contentType, "MUTATION_LAB", StringComparison.Ordinal) && finalWave)
                    || (raw.boss && (!string.Equals(monster.MonsterType, "WAVE_BOSS", StringComparison.Ordinal)
                        || raw.spawnCount != 1 || Math.Abs(raw.spawnIntervalSeconds) > 0.0001f))
                    || (!raw.boss && (string.Equals(monster.MonsterType, "WAVE_BOSS", StringComparison.Ordinal)
                        || raw.spawnIntervalSeconds <= 0f))
                    || (string.Equals(raw.contentType, "CULTIVATION_ZONE", StringComparison.Ordinal)
                        && (!string.Equals(raw.monsterSpecId, "NORMAL_MONSTER", StringComparison.Ordinal)
                            || effect != CanonicalDailyBattleStatusEffect.NONE)))
                {
                    errors.Add("DailyBattleStage Boss/content policy mismatch: " + key + ".");
                    continue;
                }
                result.Add(new CanonicalDailyBattleStage(
                    raw.contentType, raw.mapId, raw.stage, raw.wave, raw.timeLimitSeconds,
                    raw.monsterSpecId, raw.spawnCount, raw.spawnIntervalSeconds, raw.hpMultiplier,
                    raw.moveSpeedMultiplier, lanePolicy, raw.boss, effect, raw.statusEffectValue, raw.enabled));
            }

            if (result.Count != 50) errors.Add("DailyBattleStage must contain exactly 50 valid rows.");
            foreach (KeyValuePair<string, string> content in expectedMaps)
            {
                for (int stage = 1; stage <= 5; stage++)
                {
                    int targetStage = stage;
                    List<CanonicalDailyBattleStage> stageRows = result
                        .Where(row => row.ContentType == content.Key && row.Stage == targetStage)
                        .OrderBy(row => row.Wave)
                        .ToList();
                    if (stageRows.Count != expectedWaves[stage - 1]
                        || stageRows.Where(row => row.Boss).Count() != (content.Key == "MUTATION_LAB" ? 1 : 0))
                        errors.Add("DailyBattleStage stage is incomplete: " + content.Key + ":" + stage + ".");
                }
            }
            return new CanonicalDailyBattleStageRegistry(result);
        }

        private static void ValidateRelationships(List<CanonicalMonsterSpec> monsters, List<CanonicalWaveSpec> waves, List<CanonicalWaveSpawn> spawns, List<string> errors)
        {
            var monstersById = new Dictionary<string, CanonicalMonsterSpec>(StringComparer.Ordinal);
            foreach (CanonicalMonsterSpec monster in monsters) monstersById[monster.MonsterId] = monster;
            var spawnsByGroup = new Dictionary<string, List<CanonicalWaveSpawn>>(StringComparer.Ordinal);
            foreach (CanonicalWaveSpawn spawn in spawns)
            {
                if (!monstersById.TryGetValue(spawn.MonsterId, out CanonicalMonsterSpec monster))
                    errors.Add("WaveSpawn references unknown monsterId: " + spawn.MonsterId + ".");
                else if (!monster.Enabled)
                    errors.Add("WaveSpawn references disabled monsterId: " + spawn.MonsterId + ".");
                if (!spawnsByGroup.TryGetValue(spawn.SpawnGroupId, out List<CanonicalWaveSpawn> group))
                {
                    group = new List<CanonicalWaveSpawn>();
                    spawnsByGroup.Add(spawn.SpawnGroupId, group);
                }
                group.Add(spawn);
            }

            var knownGroups = new HashSet<string>(StringComparer.Ordinal);
            foreach (CanonicalWaveSpec wave in waves)
            {
                knownGroups.Add(wave.SpawnGroupId);
                if (!spawnsByGroup.TryGetValue(wave.SpawnGroupId, out List<CanonicalWaveSpawn> group) || group.Count == 0)
                {
                    errors.Add("WaveSpec references missing spawnGroupId: " + wave.SpawnGroupId + ".");
                    continue;
                }
                if (wave.IsBossWave)
                {
                    if (group.Count != 1 || group[0].LanePolicy != CanonicalLanePolicy.BOSS_SHARED || group[0].SpawnCountPerField != 1)
                        errors.Add("Boss Wave requires exactly one BOSS_SHARED spawn: " + wave.RuntimeWaveId + ".");
                    else if (monstersById.TryGetValue(group[0].MonsterId, out CanonicalMonsterSpec boss)
                        && !string.Equals(boss.MonsterType, "WAVE_BOSS", StringComparison.Ordinal))
                        errors.Add("BOSS_SHARED requires WAVE_BOSS: " + wave.RuntimeWaveId + ".");
                }
                else
                {
                    foreach (CanonicalWaveSpawn spawn in group)
                    {
                        if (spawn.LanePolicy != CanonicalLanePolicy.EACH_FIELD)
                            errors.Add("Regular Wave requires EACH_FIELD: " + wave.RuntimeWaveId + ".");
                        if (monstersById.TryGetValue(spawn.MonsterId, out CanonicalMonsterSpec monster)
                            && string.Equals(monster.MonsterType, "WAVE_BOSS", StringComparison.Ordinal))
                            errors.Add("EACH_FIELD cannot spawn WAVE_BOSS: " + wave.RuntimeWaveId + ".");
                    }
                }
            }
            foreach (string group in spawnsByGroup.Keys)
                if (!knownGroups.Contains(group)) errors.Add("WaveSpawn contains orphan spawnGroupId: " + group + ".");
        }

        private static T ParseDocument<T>(Dictionary<string, byte[]> files, string fileName, List<string> errors) where T : class
        {
            return files.TryGetValue(fileName, out byte[] bytes)
                ? ParseJson<T>(bytes, fileName, errors)
                : null;
        }

        private static T ParseJson<T>(byte[] bytes, string label, List<string> errors) where T : class
        {
            try
            {
                T value = JsonUtility.FromJson<T>(Encoding.UTF8.GetString(bytes));
                if (value == null) errors.Add("Failed to parse " + label + ".");
                return value;
            }
            catch (Exception exception)
            {
                errors.Add("Failed to parse " + label + ": " + exception.Message);
                return null;
            }
        }

        private static T[] ParseArrayJson<T>(byte[] bytes, string label, List<string> errors) where T : class
        {
            try
            {
                string raw = Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>());
                ArrayDocument<T> document = JsonUtility.FromJson<ArrayDocument<T>>("{\"items\":" + raw + "}");
                if (document == null || document.items == null) errors.Add("Failed to parse " + label + ".");
                return document?.items;
            }
            catch (Exception exception)
            {
                errors.Add("Failed to parse " + label + ": " + exception.Message);
                return null;
            }
        }

        private static bool IsSha256(string value)
        {
            return value != null && Regex.IsMatch(value, "^[0-9a-f]{64}$");
        }

        private static string ComputeContentHash(IEnumerable<CanonicalManifestFileEntry> entries)
        {
            var canonical = new StringBuilder();
            foreach (CanonicalManifestFileEntry entry in entries)
                canonical.Append(entry.Name).Append('\0').Append(entry.Sha256).Append('\0').Append(entry.Size).Append('\n');
            return Sha256(Encoding.UTF8.GetBytes(canonical.ToString()));
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(bytes);
                var result = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash) result.Append(value.ToString("x2"));
                return result.ToString();
            }
        }

        private static CanonicalBalanceLoadResult Invalid(IEnumerable<string> errors)
        {
            return new CanonicalBalanceLoadResult(null, errors);
        }

        [Serializable] private sealed class CanonicalManifestJson { public int schemaVersion; public string balanceVersion; public string contentHash; public CanonicalManifestFileJson[] files; }
        [Serializable] private sealed class CanonicalManifestFileJson { public string name; public string sha256; public long size; }
        [Serializable] private sealed class MonsterDocumentJson { public MonsterJson[] monsters; }
        [Serializable] private sealed class MonsterJson { public string monsterId; public string monsterType; public float baseHp; public float moveSpeed; public int killGold; public bool enabled; }
        [Serializable] private sealed class WaveDocumentJson { public WaveJson[] waves; }
        [Serializable] private sealed class WaveJson { public string modeId; public int wave; public float hpMultiplier; public float interWaveDelaySeconds; public bool isBossWave; public float bossTimeLimitSeconds; public string spawnGroupId; public bool enabled; }
        [Serializable] private sealed class WaveSpawnDocumentJson { public WaveSpawnJson[] spawns; }
        [Serializable] private sealed class WaveSpawnJson { public string spawnGroupId; public int order; public string monsterId; public int spawnCountPerField; public float startDelaySeconds; public float spawnIntervalSeconds; public string lanePolicy; }
        [Serializable] private sealed class PlanetBattleDocumentJson { public PlanetBattleJson[] planets; }
        [Serializable] private sealed class PlanetBattleJson { public string mapId; public int order; public float hpMultiplier; public float speedMultiplier; public float bossHpMultiplier; public bool enabled; }
        [Serializable] private sealed class FieldLimitDocumentJson { public FieldLimitJson[] fieldLimits; }
        [Serializable] private sealed class FieldLimitJson { public string modeId; public int playerCount; public int maxAliveMonsterCountPerField; public int warningThreshold; public int dangerThreshold; }
        [Serializable] private sealed class SummonDocumentJson { public SummonJson[] summons; }
        [Serializable] private sealed class SummonJson { public string modeId; public string summonType; public int baseCost; public int costIncreasePerUse; public int maxUses; public string resultPoolId; public bool enabled; }
        [Serializable] private sealed class SummonPoolDocumentJson { public SummonPoolJson[] pools; }
        [Serializable] private sealed class SummonPoolJson { public string poolId; public string name; public bool active; public SummonPoolEntryJson[] entries; }
        [Serializable] private sealed class SummonPoolEntryJson { public string grade; public int weight; public long[] alienIds; }
        [Serializable] private sealed class MutationSpecJson
        {
            public string mutationType;
            public bool enabled;
            public bool injectorEnabled;
            public bool randomActivationEnabled;
            public int weight;
            public float attackMultiplier;
            public float mpMultiplier;
            public float attackSpeedMultiplier;
            public float rangeMultiplier;
            public float goldMultiplier;
            public string mechanic;
            public float splashRadius;
            public float splashDamageMultiplier;
            public float bossDamageMultiplier = 1f;
            public float dotDamageMultiplier;
            public int dotTickCount;
            public float dotTickIntervalSeconds;
            public float slowMultiplier = 1f;
            public float slowDurationSeconds;
            public int goldPerHit;
            public float gambleSuccessChance;
            public float gambleSuccessMultiplier = 1f;
            public float gambleFailureMultiplier = 1f;
        }
        [Serializable] private sealed class MutationConfigJson { public string modeId; public int initialActivationCost; public int rerollCost1; public int rerollCost2; public int rerollCost3; public int rerollCost4; public int rerollCostAfterMax; public int injectorReplaceCost; }
        [Serializable] private sealed class InjectorPoolJson { public string poolId; public string poolName; public bool poolActive; public string mutationType; public int weight; public string resultType; }
        [Serializable] private sealed class ResonanceJson { public string track; public int level; public int requiredGold; public float attackMultiplier; public float attackSpeedMultiplier; public float rangeMultiplier; public bool enabled; }
        [Serializable] private sealed class DailyBattleStageDocumentJson { public DailyBattleStageJson[] stages; }
        [Serializable] private sealed class DailyBattleStageJson
        {
            public string contentType;
            public string mapId;
            public int stage;
            public int wave;
            public int timeLimitSeconds;
            public string monsterSpecId;
            public int spawnCount;
            public float spawnIntervalSeconds;
            public float hpMultiplier;
            public float moveSpeedMultiplier;
            public string lanePolicy;
            public bool boss;
            public string statusEffectType;
            public float statusEffectValue;
            public bool enabled;
        }
        [Serializable] private sealed class ArrayDocument<T> { public T[] items; }
    }
}
