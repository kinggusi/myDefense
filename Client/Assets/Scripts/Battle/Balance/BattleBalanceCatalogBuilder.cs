using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Balance
{
    public sealed class BattleBalanceCatalogBuildResult
    {
        public bool IsValid { get; }
        public BattleBalanceCatalog Catalog { get; }
        public IReadOnlyList<string> Errors { get; }

        internal BattleBalanceCatalogBuildResult(BattleBalanceCatalog catalog, IEnumerable<string> errors)
        {
            Catalog = catalog;
            Errors = BattleBalanceCollections.Copy(errors);
            IsValid = catalog != null && Errors.Count == 0;
        }
    }

    public static class BattleBalanceCatalogBuilder
    {
        public static BattleBalanceCatalogBuildResult Build(
            BattleBalanceManifestData manifest,
            BattleBalanceDocuments documents,
            IMonsterDefinitionProvider monsterDefinitions,
            IAlienIdProvider alienIds)
        {
            var errors = new List<string>();
            if (manifest == null) errors.Add("Manifest is required.");
            if (documents == null) errors.Add("All Battle balance documents are required.");
            if (monsterDefinitions == null) errors.Add("Monster definition provider is required.");
            if (alienIds == null) errors.Add("Alien ID provider is required.");
            if (errors.Count > 0)
                return new BattleBalanceCatalogBuildResult(null, errors);

            ValidateManifest(manifest, documents, errors);
            ValidateDocuments(documents, monsterDefinitions, alienIds, errors);
            BattleBalanceCatalog catalog = errors.Count == 0 ? new BattleBalanceCatalog(documents) : null;
            return new BattleBalanceCatalogBuildResult(catalog, errors);
        }

        public static BattleBalanceCatalogBuildResult BuildComposite(
            BattleBalanceDocuments documents,
            IMonsterDefinitionProvider monsterDefinitions,
            IAlienIdProvider alienIds)
        {
            var errors = new List<string>();
            if (documents == null) errors.Add("All composite Battle balance documents are required.");
            if (monsterDefinitions == null) errors.Add("Monster definition provider is required.");
            if (alienIds == null) errors.Add("Alien ID provider is required.");
            if (errors.Count > 0) return new BattleBalanceCatalogBuildResult(null, errors);

            ValidateDocuments(documents, monsterDefinitions, alienIds, errors);
            BattleBalanceCatalog catalog = errors.Count == 0 ? new BattleBalanceCatalog(documents) : null;
            return new BattleBalanceCatalogBuildResult(catalog, errors);
        }

        private static void ValidateDocuments(
            BattleBalanceDocuments documents,
            IMonsterDefinitionProvider monsterDefinitions,
            IAlienIdProvider alienIds,
            List<string> errors)
        {
            ValidateWaves(documents.Waves.Items, errors);

            var wavesById = BuildUniqueMap(documents.Waves.Items, item => item.WaveId, "WaveSpec.waveId", errors);
            var skillsById = BuildUniqueMap(documents.Skills.Items, item => item.SkillId, "SkillSpec.skillId", errors);
            var projectilesById = BuildUniqueMap(documents.Projectiles.Items, item => item.ProjectileId, "ProjectileSpec.projectileId", errors);

            ValidateSpawns(documents.Spawns.Items, wavesById, monsterDefinitions, errors);
            ValidateBossPatterns(documents.BossPatterns.Items, wavesById, skillsById, errors);
            ValidateSkills(documents.Skills.Items, projectilesById, errors);
            ValidateAlienSkills(documents.AlienSkills.Items, skillsById, alienIds, errors);
            ValidateProjectiles(documents.Projectiles.Items, errors);
            ValidateSkillEffects(documents.SkillEffects.Items, skillsById, errors);
        }

        private static void ValidateManifest(BattleBalanceManifestData manifest, BattleBalanceDocuments documents, List<string> errors)
        {
            if (manifest.SchemaVersion != BattleBalanceSchema.Version)
                errors.Add("Manifest schemaVersion does not match the supported version.");

            var entries = new Dictionary<string, BattleBalanceFileEntryData>(StringComparer.Ordinal);
            foreach (BattleBalanceFileEntryData file in manifest.Files)
            {
                if (file == null || string.IsNullOrWhiteSpace(file.ResourcePath))
                {
                    errors.Add("Manifest contains an empty resourcePath.");
                    continue;
                }

                if (BattleBalanceResourcePaths.HasFileExtension(file.ResourcePath))
                    errors.Add("Manifest resourcePath must not contain a file extension: " + file.ResourcePath + ".");
                if (entries.ContainsKey(file.ResourcePath))
                    errors.Add("Manifest contains duplicate resourcePath: " + file.ResourcePath + ".");
                else
                    entries.Add(file.ResourcePath, file);
            }

            var documentsByPath = new Dictionary<string, IDocumentHeader>(StringComparer.Ordinal)
            {
                { BattleBalanceResourcePaths.WaveSpec, Header(documents.Waves) },
                { BattleBalanceResourcePaths.WaveSpawnSpec, Header(documents.Spawns) },
                { BattleBalanceResourcePaths.BossPatternSpec, Header(documents.BossPatterns) },
                { BattleBalanceResourcePaths.SkillSpec, Header(documents.Skills) },
                { BattleBalanceResourcePaths.AlienSkillLinks, Header(documents.AlienSkills) },
                { BattleBalanceResourcePaths.ProjectileSpec, Header(documents.Projectiles) },
                { BattleBalanceResourcePaths.SkillEffectSpec, Header(documents.SkillEffects) }
            };

            foreach (string requiredPath in BattleBalanceResourcePaths.RequiredDocumentPaths)
            {
                BattleBalanceFileEntryData file;
                if (!entries.TryGetValue(requiredPath, out file))
                {
                    errors.Add("Manifest is missing required Battle file: " + requiredPath + ".");
                    continue;
                }

                IDocumentHeader document = documentsByPath[requiredPath];
                if (!string.Equals(file.ContentHash, document.ContentHash, StringComparison.OrdinalIgnoreCase))
                    errors.Add("Manifest contentHash does not match document contentHash for " + requiredPath + ".");
            }

            foreach (KeyValuePair<string, IDocumentHeader> pair in documentsByPath)
            {
                IDocumentHeader document = pair.Value;
                if (document.SchemaVersion != manifest.SchemaVersion)
                    errors.Add(pair.Key + " schemaVersion does not match manifest schemaVersion.");
                if (!string.Equals(document.BalanceVersion, manifest.BalanceVersion, StringComparison.Ordinal))
                    errors.Add(pair.Key + " balanceVersion does not match manifest balanceVersion.");
            }
        }

        private static void ValidateWaves(IReadOnlyList<WaveSpecData> waves, List<string> errors)
        {
            var enabledRounds = new HashSet<int>();
            foreach (WaveSpecData wave in waves)
            {
                RequireId(wave.WaveId, "WaveSpec.waveId", errors);
                if (wave.RoundNumber < 1)
                    errors.Add("WaveSpec " + Label(wave.WaveId) + " roundNumber must be at least 1.");
                if (wave.NextWaveDelaySeconds < 0f)
                    errors.Add("WaveSpec " + Label(wave.WaveId) + " nextWaveDelaySeconds must be non-negative.");
                if (wave.WaveType == WaveType.REGULAR && wave.BossTimeLimitSeconds != 0f)
                    errors.Add("REGULAR WaveSpec " + Label(wave.WaveId) + " bossTimeLimitSeconds must be 0.");
                if (wave.WaveType == WaveType.BOSS && wave.BossTimeLimitSeconds <= 0f)
                    errors.Add("BOSS WaveSpec " + Label(wave.WaveId) + " bossTimeLimitSeconds must be greater than 0.");
                if (wave.Enabled && !enabledRounds.Add(wave.RoundNumber))
                    errors.Add("Enabled WaveSpec contains duplicate roundNumber: " + wave.RoundNumber + ".");
            }
        }

        private static void ValidateSpawns(
            IReadOnlyList<WaveSpawnSpecData> spawns,
            Dictionary<string, WaveSpecData> wavesById,
            IMonsterDefinitionProvider monsterDefinitions,
            List<string> errors)
        {
            var compositeKeys = new HashSet<string>(StringComparer.Ordinal);
            var bossSpawnCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var spawnRowsByWave = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (WaveSpawnSpecData spawn in spawns)
            {
                string key = Label(spawn.WaveId) + ":" + spawn.SpawnOrder;
                if (!compositeKeys.Add(key))
                    errors.Add("WaveSpawnSpec contains duplicate (waveId, spawnOrder): " + key + ".");
                RequireId(spawn.WaveId, "WaveSpawnSpec.waveId", errors);
                RequireId(spawn.MonsterId, "WaveSpawnSpec.monsterId", errors);
                if (spawn.SpawnOrder < 1) errors.Add("WaveSpawnSpec " + key + " spawnOrder must be at least 1.");
                if (spawn.SpawnCount < 1) errors.Add("WaveSpawnSpec " + key + " spawnCount must be at least 1.");
                if (spawn.SpawnDelaySeconds < 0f) errors.Add("WaveSpawnSpec " + key + " spawnDelaySeconds must be non-negative.");
                if (spawn.SpawnIntervalSeconds < 0f) errors.Add("WaveSpawnSpec " + key + " spawnIntervalSeconds must be non-negative.");
                if (spawn.HpMultiplier <= 0f) errors.Add("WaveSpawnSpec " + key + " hpMultiplier must be greater than 0.");
                if (spawn.MoveSpeedMultiplier <= 0f) errors.Add("WaveSpawnSpec " + key + " moveSpeedMultiplier must be greater than 0.");

                BattleMonsterDefinition monsterDefinition = null;
                bool hasMonsterDefinition = !string.IsNullOrWhiteSpace(spawn.MonsterId)
                    && monsterDefinitions.TryGet(spawn.MonsterId, out monsterDefinition);
                if (!hasMonsterDefinition && !string.IsNullOrWhiteSpace(spawn.MonsterId))
                    errors.Add("WaveSpawnSpec " + key + " references unknown monsterId: " + spawn.MonsterId + ".");

                WaveSpecData wave;
                if (string.IsNullOrWhiteSpace(spawn.WaveId) || !wavesById.TryGetValue(spawn.WaveId, out wave))
                {
                    errors.Add("WaveSpawnSpec " + key + " references unknown waveId: " + Label(spawn.WaveId) + ".");
                    continue;
                }

                if (wave.WaveType == WaveType.REGULAR && spawn.LanePolicy != BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE)
                    errors.Add("REGULAR wave " + wave.WaveId + " must use EACH_ACTIVE_PLAYER_LANE.");
                if (wave.WaveType == WaveType.BOSS && spawn.LanePolicy != BattleLanePolicy.BOSS_SHARED)
                    errors.Add("BOSS wave " + wave.WaveId + " must use BOSS_SHARED.");

                int rowCount;
                spawnRowsByWave.TryGetValue(wave.WaveId, out rowCount);
                spawnRowsByWave[wave.WaveId] = rowCount + 1;
                if (hasMonsterDefinition)
                    ValidateMonsterDefinitionForWave(monsterDefinition, wave, key, errors);

                if (wave.WaveType == WaveType.BOSS)
                {
                    int accumulated;
                    bossSpawnCounts.TryGetValue(wave.WaveId, out accumulated);
                    bossSpawnCounts[wave.WaveId] = accumulated + spawn.SpawnCount;
                }
            }

            foreach (KeyValuePair<string, WaveSpecData> pair in wavesById)
            {
                if (pair.Value.WaveType == WaveType.BOSS)
                {
                    int count;
                    bossSpawnCounts.TryGetValue(pair.Key, out count);
                    if (count != 1)
                        errors.Add("BOSS wave " + pair.Key + " must spawn exactly one monster, but configured " + count + ".");
                }
                else if (pair.Value.Enabled)
                {
                    int rowCount;
                    spawnRowsByWave.TryGetValue(pair.Key, out rowCount);
                    if (rowCount < 1)
                        errors.Add("Enabled REGULAR wave " + pair.Key + " must contain at least one WaveSpawnSpec row.");
                }
            }
        }

        private static void ValidateMonsterDefinitionForWave(
            BattleMonsterDefinition definition,
            WaveSpecData wave,
            string spawnKey,
            List<string> errors)
        {
            bool isNormal = string.Equals(definition.MonsterType, "NORMAL", StringComparison.Ordinal);
            bool isElite = string.Equals(definition.MonsterType, "ELITE", StringComparison.Ordinal);
            bool isBoss = string.Equals(definition.MonsterType, "BOSS", StringComparison.Ordinal)
                || string.Equals(definition.MonsterType, "WAVE_BOSS", StringComparison.Ordinal);
            if (!isNormal && !isElite && !isBoss)
            {
                errors.Add("WaveSpawnSpec " + spawnKey + " references MonsterDefinition with unsupported monsterType: " + Label(definition.MonsterType) + ".");
                return;
            }

            if (wave.WaveType == WaveType.REGULAR)
            {
                if (!isNormal && !isElite)
                    errors.Add("REGULAR wave " + wave.WaveId + " may reference only NORMAL or ELITE monsters.");
                if (!definition.CountsTowardLaneLimit)
                    errors.Add("REGULAR wave " + wave.WaveId + " requires CountsTowardLaneLimit=true.");
            }
            else
            {
                if (!isBoss)
                    errors.Add("BOSS wave " + wave.WaveId + " may reference only BOSS monsters.");
                if (definition.CountsTowardLaneLimit)
                    errors.Add("BOSS wave " + wave.WaveId + " requires CountsTowardLaneLimit=false.");
            }
        }

        private static void ValidateBossPatterns(
            IReadOnlyList<BossPatternSpecData> patterns,
            Dictionary<string, WaveSpecData> wavesById,
            Dictionary<string, SkillSpecData> skillsById,
            List<string> errors)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (BossPatternSpecData pattern in patterns)
            {
                string key = Label(pattern.WaveId) + ":" + pattern.PatternOrder;
                if (!keys.Add(key)) errors.Add("BossPatternSpec contains duplicate (waveId, patternOrder): " + key + ".");
                if (pattern.PatternOrder < 1) errors.Add("BossPatternSpec " + key + " patternOrder must be at least 1.");
                if (pattern.TriggerValue < 0f) errors.Add("BossPatternSpec " + key + " triggerValue must be non-negative.");
                if (pattern.CooldownSeconds < 0f) errors.Add("BossPatternSpec " + key + " cooldownSeconds must be non-negative.");

                WaveSpecData wave;
                if (string.IsNullOrWhiteSpace(pattern.WaveId) || !wavesById.TryGetValue(pattern.WaveId, out wave))
                    errors.Add("BossPatternSpec " + key + " references unknown waveId: " + Label(pattern.WaveId) + ".");
                else if (wave.WaveType != WaveType.BOSS)
                    errors.Add("BossPatternSpec " + key + " must reference a BOSS wave.");

                if (pattern.PatternType == BossPatternType.CAST_SKILL
                    && (string.IsNullOrWhiteSpace(pattern.SkillId) || !skillsById.ContainsKey(pattern.SkillId)))
                    errors.Add("CAST_SKILL BossPatternSpec " + key + " references unknown skillId: " + Label(pattern.SkillId) + ".");
            }
        }

        private static void ValidateSkills(
            IReadOnlyList<SkillSpecData> skills,
            Dictionary<string, ProjectileSpecData> projectilesById,
            List<string> errors)
        {
            foreach (SkillSpecData skill in skills)
            {
                RequireId(skill.SkillId, "SkillSpec.skillId", errors);
                RequireId(skill.NameKey, "SkillSpec.nameKey", errors);
                if (skill.CooldownSeconds < 0f) errors.Add("SkillSpec " + Label(skill.SkillId) + " cooldownSeconds must be non-negative.");
                if (skill.MpCost < 0f) errors.Add("SkillSpec " + Label(skill.SkillId) + " mpCost must be non-negative.");
                if (skill.CastRange < 0f) errors.Add("SkillSpec " + Label(skill.SkillId) + " castRange must be non-negative.");
                if (skill.MaxTargetCount < 0)
                    errors.Add("SkillSpec " + Label(skill.SkillId) + " maxTargetCount must be non-negative.");
                if (!string.IsNullOrWhiteSpace(skill.ProjectileId) && !projectilesById.ContainsKey(skill.ProjectileId))
                    errors.Add("SkillSpec " + Label(skill.SkillId) + " references unknown projectileId: " + skill.ProjectileId + ".");
            }
        }

        private static void ValidateAlienSkills(
            IReadOnlyList<AlienSkillLinkData> links,
            Dictionary<string, SkillSpecData> skillsById,
            IAlienIdProvider alienIds,
            List<string> errors)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (AlienSkillLinkData link in links)
            {
                string key = AlienSkillCatalog.BuildKey(link.AlienId, link.SlotIndex);
                if (!keys.Add(key)) errors.Add("AlienSkillLink contains duplicate (alienId, slotIndex): " + key + ".");
                if (link.AlienId <= 0) errors.Add("AlienSkillLink alienId must be greater than 0: " + link.AlienId + ".");
                if (link.SlotIndex < 0) errors.Add("AlienSkillLink " + key + " slotIndex must be non-negative.");
                if (link.CastPriority < 0) errors.Add("AlienSkillLink " + key + " castPriority must be non-negative.");
                if (!alienIds.Contains(link.AlienId)) errors.Add("AlienSkillLink references unknown alienId: " + link.AlienId + ".");
                if (string.IsNullOrWhiteSpace(link.SkillId) || !skillsById.ContainsKey(link.SkillId))
                    errors.Add("AlienSkillLink " + key + " references unknown skillId: " + Label(link.SkillId) + ".");
            }
        }

        private static void ValidateProjectiles(IReadOnlyList<ProjectileSpecData> projectiles, List<string> errors)
        {
            foreach (ProjectileSpecData projectile in projectiles)
            {
                RequireId(projectile.ProjectileId, "ProjectileSpec.projectileId", errors);
                RequireId(projectile.PrefabKey, "ProjectileSpec.prefabKey", errors);
                if (projectile.Speed < 0f)
                    errors.Add("ProjectileSpec " + Label(projectile.ProjectileId) + " speed must be non-negative.");
                else if (projectile.MoveType != ProjectileMoveType.INSTANT && projectile.Speed == 0f)
                    errors.Add("Non-INSTANT ProjectileSpec " + Label(projectile.ProjectileId) + " speed must be greater than 0.");
                if (projectile.LifetimeSeconds <= 0f) errors.Add("ProjectileSpec " + Label(projectile.ProjectileId) + " lifetimeSeconds must be greater than 0.");
                if (projectile.HitRadius < 0f) errors.Add("ProjectileSpec " + Label(projectile.ProjectileId) + " hitRadius must be non-negative.");
                if (projectile.PierceCount < 0) errors.Add("ProjectileSpec " + Label(projectile.ProjectileId) + " pierceCount must be non-negative.");
                if (projectile.DestroyOnHit && projectile.PierceCount != 0)
                    errors.Add("ProjectileSpec " + Label(projectile.ProjectileId) + " must use pierceCount=0 when destroyOnHit=true.");
            }
        }

        private static void ValidateSkillEffects(
            IReadOnlyList<SkillEffectSpecData> effects,
            Dictionary<string, SkillSpecData> skillsById,
            List<string> errors)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (SkillEffectSpecData effect in effects)
            {
                string key = Label(effect.SkillId) + ":" + effect.ExecutionOrder;
                if (!keys.Add(key)) errors.Add("SkillEffectSpec contains duplicate (skillId, executionOrder): " + key + ".");
                if (effect.ExecutionOrder < 1) errors.Add("SkillEffectSpec " + key + " executionOrder must be at least 1.");
                if (string.IsNullOrWhiteSpace(effect.SkillId) || !skillsById.ContainsKey(effect.SkillId))
                    errors.Add("SkillEffectSpec " + key + " references unknown skillId: " + Label(effect.SkillId) + ".");
                if (effect.BaseMagnitude < 0f)
                    errors.Add("SkillEffectSpec " + key + " baseMagnitude must be non-negative.");
                if (effect.Coefficient < 0f)
                    errors.Add("SkillEffectSpec " + key + " coefficient must be non-negative.");
                if (effect.Chance < 0f || effect.Chance > 1f)
                    errors.Add("SkillEffectSpec " + key + " chance must be between 0 and 1.");
                if (effect.DurationSeconds < 0f) errors.Add("SkillEffectSpec " + key + " durationSeconds must be non-negative.");
                if (effect.TickIntervalSeconds < 0f) errors.Add("SkillEffectSpec " + key + " tickIntervalSeconds must be non-negative.");
                if (effect.Radius < 0f) errors.Add("SkillEffectSpec " + key + " radius must be non-negative.");
                if (effect.MaxStacks < 1) errors.Add("SkillEffectSpec " + key + " maxStacks must be at least 1.");
                if (effect.BossMultiplier < 0f) errors.Add("SkillEffectSpec " + key + " bossMultiplier must be non-negative.");
                if (effect.EffectType == BattleSkillEffectType.DAMAGE_OVER_TIME
                    && (effect.DurationSeconds <= 0f || effect.TickIntervalSeconds <= 0f || effect.TickIntervalSeconds > effect.DurationSeconds))
                    errors.Add("DAMAGE_OVER_TIME SkillEffectSpec " + key + " requires durationSeconds > 0 and 0 < tickIntervalSeconds <= durationSeconds.");
                if (effect.EffectType == BattleSkillEffectType.SPLASH_DAMAGE && effect.Radius <= 0f)
                    errors.Add("SPLASH_DAMAGE SkillEffectSpec " + key + " radius must be greater than 0.");
            }
        }

        private static Dictionary<string, T> BuildUniqueMap<T>(
            IReadOnlyList<T> values,
            Func<T, string> keySelector,
            string label,
            List<string> errors)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (T value in values)
            {
                string key = keySelector(value);
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (result.ContainsKey(key)) errors.Add(label + " contains duplicate ID: " + key + ".");
                else result.Add(key, value);
            }
            return result;
        }

        private static void RequireId(string value, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value)) errors.Add(label + " must not be empty.");
        }

        private static string Label(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
        }

        private static IDocumentHeader Header<T>(BattleBalanceDocument<T> document)
        {
            return new DocumentHeader(document.SchemaVersion, document.BalanceVersion, document.ContentHash);
        }

        private interface IDocumentHeader
        {
            int SchemaVersion { get; }
            string BalanceVersion { get; }
            string ContentHash { get; }
        }

        private sealed class DocumentHeader : IDocumentHeader
        {
            public int SchemaVersion { get; }
            public string BalanceVersion { get; }
            public string ContentHash { get; }

            public DocumentHeader(int schemaVersion, string balanceVersion, string contentHash)
            {
                SchemaVersion = schemaVersion;
                BalanceVersion = balanceVersion;
                ContentHash = contentHash;
            }
        }
    }
}
