using System;
using System.Collections.Generic;
using System.Linq;

namespace MyDefense.Battle.Balance.Canonical
{
    public interface ICanonicalCompositeBattleBalanceProvider : IBattleBalanceProvider
    {
        string CanonicalBalanceVersion { get; }
        string CanonicalContentHash { get; }
        string BattleContentVersion { get; }
        string BattleContentHash { get; }
        CanonicalFieldLimit FieldLimit { get; }
        CanonicalPlanetBattleRegistry PlanetBattles { get; }
        CanonicalSummonBalance Summon { get; }
        IReadOnlyDictionary<string, CanonicalSummonPool> SummonPools { get; }
        IReadOnlyList<CanonicalMutationSpec> MutationSpecs { get; }
        CanonicalMutationConfig MutationConfig { get; }
        IReadOnlyList<CanonicalInjectorPoolEntry> InjectorPool { get; }
        CanonicalResonanceRegistry Resonance { get; }
        CanonicalDailyBattleStageRegistry DailyBattleStages { get; }
        IMonsterDefinitionProvider MonsterDefinitions { get; }
    }

    public sealed class CanonicalCompositeBattleBalanceProvider : ICanonicalCompositeBattleBalanceProvider
    {
        private static readonly string[] BattleOwnedPaths =
        {
            BattleBalanceResourcePaths.BossPatternSpec,
            BattleBalanceResourcePaths.SkillSpec,
            BattleBalanceResourcePaths.AlienSkillLinks,
            BattleBalanceResourcePaths.ProjectileSpec,
            BattleBalanceResourcePaths.SkillEffectSpec
        };

        public int SchemaVersion { get; }
        public string BalanceVersion => CanonicalBalanceVersion;
        public string ContentHash => CanonicalContentHash;
        public BattleBalanceCatalog Catalog { get; }
        public bool IsValid { get; }
        public IReadOnlyList<string> ValidationErrors { get; }
        public string CanonicalBalanceVersion { get; }
        public string CanonicalContentHash { get; }
        public string BattleContentVersion { get; }
        public string BattleContentHash { get; }
        public CanonicalFieldLimit FieldLimit { get; }
        public CanonicalPlanetBattleRegistry PlanetBattles { get; }
        public CanonicalSummonBalance Summon { get; }
        public IReadOnlyDictionary<string, CanonicalSummonPool> SummonPools { get; }
        public IReadOnlyList<CanonicalMutationSpec> MutationSpecs { get; }
        public CanonicalMutationConfig MutationConfig { get; }
        public IReadOnlyList<CanonicalInjectorPoolEntry> InjectorPool { get; }
        public CanonicalResonanceRegistry Resonance { get; }
        public CanonicalDailyBattleStageRegistry DailyBattleStages { get; }
        public IMonsterDefinitionProvider MonsterDefinitions { get; }

        private CanonicalCompositeBattleBalanceProvider(
            CanonicalBalanceBundle canonical,
            BattleBalanceManifestData battleManifest,
            BattleBalanceCatalog catalog,
            CanonicalFieldLimit fieldLimit,
            IEnumerable<string> errors)
        {
            SchemaVersion = canonical?.Manifest.SchemaVersion ?? 0;
            CanonicalBalanceVersion = canonical?.Manifest.BalanceVersion;
            CanonicalContentHash = canonical?.Manifest.ContentHash;
            BattleContentVersion = battleManifest?.BalanceVersion;
            BattleContentHash = battleManifest?.BundleHash;
            MonsterDefinitions = canonical?.MonsterDefinitions;
            FieldLimit = fieldLimit;
            PlanetBattles = canonical?.PlanetBattles;
            Summon = canonical?.Summon;
            SummonPools = canonical?.SummonPools;
            MutationSpecs = canonical?.MutationSpecs;
            MutationConfig = canonical?.MutationConfig;
            InjectorPool = canonical?.InjectorPool;
            Resonance = canonical?.Resonance;
            DailyBattleStages = canonical?.DailyBattleStages;
            Catalog = catalog;
            ValidationErrors = Array.AsReadOnly(new List<string>(errors ?? Array.Empty<string>()).ToArray());
            IsValid = canonical != null && catalog != null && fieldLimit != null && PlanetBattles != null
                && Summon != null && DailyBattleStages != null && ValidationErrors.Count == 0;
        }

        public static CanonicalCompositeBattleBalanceProvider LoadProduction(
            ICanonicalMonsterRuntimeMapping runtimeMapping,
            IAlienIdProvider alienIds)
        {
            CanonicalBalanceLoadResult canonicalResult = CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(),
                runtimeMapping);
            return Load(canonicalResult, new ResourcesBattleBalanceTextSource(), alienIds);
        }

        public static CanonicalCompositeBattleBalanceProvider Load(
            CanonicalBalanceLoadResult canonicalResult,
            IBattleBalanceTextSource battleTextSource,
            IAlienIdProvider alienIds)
        {
            var errors = new List<string>();
            if (canonicalResult == null)
            {
                errors.Add("Canonical balance load result is required.");
                return Invalid(errors);
            }
            errors.AddRange(canonicalResult.Errors);
            CanonicalBalanceBundle canonical = canonicalResult.Bundle;
            if (canonical == null) return Invalid(errors);
            if (battleTextSource == null) errors.Add("Battle-owned balance text source is required.");
            if (alienIds == null) errors.Add("Alien ID provider is required.");
            if (errors.Count > 0) return new CanonicalCompositeBattleBalanceProvider(canonical, null, null, null, errors);

            var parser = new BattleBalanceJsonParser();
            BattleBalanceManifestData manifest = LoadManifest(battleTextSource, parser, errors);
            BattleBalanceDocument<BossPatternSpecData> bossPatterns = LoadDocument(battleTextSource, BattleBalanceResourcePaths.BossPatternSpec, parser.ParseBossPatternDocument, errors);
            BattleBalanceDocument<SkillSpecData> skills = LoadDocument(battleTextSource, BattleBalanceResourcePaths.SkillSpec, parser.ParseSkillDocument, errors);
            BattleBalanceDocument<AlienSkillLinkData> alienSkills = LoadDocument(battleTextSource, BattleBalanceResourcePaths.AlienSkillLinks, parser.ParseAlienSkillLinkDocument, errors);
            BattleBalanceDocument<ProjectileSpecData> projectiles = LoadDocument(battleTextSource, BattleBalanceResourcePaths.ProjectileSpec, parser.ParseProjectileDocument, errors);
            BattleBalanceDocument<SkillEffectSpecData> skillEffects = LoadDocument(battleTextSource, BattleBalanceResourcePaths.SkillEffectSpec, parser.ParseSkillEffectDocument, errors);

            if (manifest != null && bossPatterns != null && skills != null && alienSkills != null && projectiles != null && skillEffects != null)
            {
                var headers = new Dictionary<string, DocumentHeader>(StringComparer.Ordinal)
                {
                    { BattleBalanceResourcePaths.BossPatternSpec, Header(bossPatterns) },
                    { BattleBalanceResourcePaths.SkillSpec, Header(skills) },
                    { BattleBalanceResourcePaths.AlienSkillLinks, Header(alienSkills) },
                    { BattleBalanceResourcePaths.ProjectileSpec, Header(projectiles) },
                    { BattleBalanceResourcePaths.SkillEffectSpec, Header(skillEffects) }
                };
                ValidateBattleOwnedManifest(manifest, headers, errors);
            }

            CanonicalFieldLimit fieldLimit = null;
            if (!canonical.FieldLimits.TryGet(CanonicalBalanceContract.DefaultModeId, CanonicalBalanceContract.DefaultPlayerCount, out fieldLimit))
                errors.Add("Canonical FieldLimit is missing COOP_STANDARD for two players.");

            BattleBalanceCatalog catalog = null;
            if (errors.Count == 0)
            {
                bossPatterns = ExpandBossPatternTemplates(canonical.RuntimeWaves, bossPatterns);
                var documents = new BattleBalanceDocuments(
                    canonical.RuntimeWaves,
                    canonical.RuntimeSpawns,
                    bossPatterns,
                    skills,
                    alienSkills,
                    projectiles,
                    skillEffects);
                BattleBalanceCatalogBuildResult build = BattleBalanceCatalogBuilder.BuildComposite(documents, canonical.MonsterDefinitions, alienIds);
                errors.AddRange(build.Errors);
                catalog = build.Catalog;
            }

            return new CanonicalCompositeBattleBalanceProvider(canonical, manifest, catalog, fieldLimit, errors);
        }

        private static BattleBalanceDocument<BossPatternSpecData> ExpandBossPatternTemplates(
            BattleBalanceDocument<WaveSpecData> runtimeWaves,
            BattleBalanceDocument<BossPatternSpecData> source)
        {
            if (runtimeWaves == null || source == null || source.Items.Count == 0)
                return source;

            IReadOnlyList<BossPatternSpecData> template = source.Items
                .Where(pattern => string.Equals(pattern.WaveId, "WAVE_010", StringComparison.Ordinal))
                .OrderBy(pattern => pattern.PatternOrder)
                .ToArray();
            if (template.Count == 0)
                return source;

            var expanded = new List<BossPatternSpecData>();
            foreach (WaveSpecData bossWave in runtimeWaves.Items.Where(wave => wave.Enabled && wave.WaveType == WaveType.BOSS))
            {
                foreach (BossPatternSpecData pattern in template)
                {
                    expanded.Add(new BossPatternSpecData(
                        bossWave.WaveId,
                        pattern.PatternOrder,
                        pattern.PatternType,
                        pattern.TriggerType,
                        pattern.TriggerValue,
                        pattern.CooldownSeconds,
                        pattern.SkillId,
                        pattern.ParameterKey,
                        pattern.ParameterValue,
                        pattern.Enabled));
                }
            }

            return new BattleBalanceDocument<BossPatternSpecData>(
                source.SchemaVersion,
                source.BalanceVersion,
                source.ContentHash,
                expanded);
        }

        private static BattleBalanceManifestData LoadManifest(IBattleBalanceTextSource source, BattleBalanceJsonParser parser, List<string> errors)
        {
            if (!source.TryLoad(BattleBalanceResourcePaths.Manifest, out string json))
            {
                errors.Add("Missing Battle-owned manifest: " + BattleBalanceResourcePaths.Manifest + ".");
                return null;
            }
            BattleBalanceParseResult<BattleBalanceManifestData> result = parser.ParseManifest(json);
            AddErrors(BattleBalanceResourcePaths.Manifest, result.Errors, errors);
            return result.Value;
        }

        private static BattleBalanceDocument<T> LoadDocument<T>(
            IBattleBalanceTextSource source,
            string path,
            Func<string, BattleBalanceParseResult<BattleBalanceDocument<T>>> parser,
            List<string> errors)
        {
            if (!source.TryLoad(path, out string json))
            {
                errors.Add("Missing Battle-owned resource: " + path + ".");
                return null;
            }
            BattleBalanceParseResult<BattleBalanceDocument<T>> result = parser(json);
            AddErrors(path, result.Errors, errors);
            return result.Value;
        }

        private static void ValidateBattleOwnedManifest(
            BattleBalanceManifestData manifest,
            Dictionary<string, DocumentHeader> headers,
            List<string> errors)
        {
            if (manifest.SchemaVersion != BattleBalanceSchema.Version)
                errors.Add("Battle-owned manifest schemaVersion is unsupported.");
            var entries = new Dictionary<string, BattleBalanceFileEntryData>(StringComparer.Ordinal);
            foreach (BattleBalanceFileEntryData entry in manifest.Files)
            {
                if (!entries.TryAdd(entry.ResourcePath, entry))
                    errors.Add("Battle-owned manifest contains duplicate resourcePath: " + entry.ResourcePath + ".");
            }

            foreach (string path in BattleOwnedPaths)
            {
                if (!entries.TryGetValue(path, out BattleBalanceFileEntryData entry))
                {
                    errors.Add("Battle-owned manifest is missing: " + path + ".");
                    continue;
                }
                DocumentHeader header = headers[path];
                if (header.SchemaVersion != manifest.SchemaVersion)
                    errors.Add(path + " schemaVersion does not match Battle-owned manifest.");
                if (!string.Equals(header.BalanceVersion, manifest.BalanceVersion, StringComparison.Ordinal))
                    errors.Add(path + " balanceVersion does not match Battle-owned manifest.");
                if (!string.Equals(header.ContentHash, entry.ContentHash, StringComparison.OrdinalIgnoreCase))
                    errors.Add(path + " contentHash does not match Battle-owned manifest.");
            }
        }

        private static DocumentHeader Header<T>(BattleBalanceDocument<T> document)
        {
            return new DocumentHeader(document.SchemaVersion, document.BalanceVersion, document.ContentHash);
        }

        private static void AddErrors(string path, IReadOnlyList<string> source, List<string> destination)
        {
            foreach (string error in source) destination.Add(path + ": " + error);
        }

        private static CanonicalCompositeBattleBalanceProvider Invalid(IEnumerable<string> errors)
        {
            return new CanonicalCompositeBattleBalanceProvider(null, null, null, null, errors);
        }

        private readonly struct DocumentHeader
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
