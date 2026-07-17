using System;
using System.Collections.Generic;

namespace MyDefense.Battle.Balance
{
    public interface IBattleBalanceProvider
    {
        int SchemaVersion { get; }
        string BalanceVersion { get; }
        string ContentHash { get; }
        BattleBalanceCatalog Catalog { get; }
        bool IsValid { get; }
        IReadOnlyList<string> ValidationErrors { get; }
    }

    public sealed class BattleBalanceProvider : IBattleBalanceProvider
    {
        public int SchemaVersion { get; }
        public string BalanceVersion { get; }
        public string ContentHash { get; }
        public BattleBalanceCatalog Catalog { get; }
        public bool IsValid { get; }
        public IReadOnlyList<string> ValidationErrors { get; }

        private BattleBalanceProvider(
            int schemaVersion,
            string balanceVersion,
            string contentHash,
            BattleBalanceCatalog catalog,
            IEnumerable<string> errors)
        {
            SchemaVersion = schemaVersion;
            BalanceVersion = balanceVersion;
            ContentHash = contentHash;
            Catalog = catalog;
            ValidationErrors = BattleBalanceCollections.Copy(errors);
            IsValid = catalog != null && ValidationErrors.Count == 0;
        }

        public static BattleBalanceProvider Load(
            IBattleBalanceTextSource textSource,
            IMonsterDefinitionProvider monsterDefinitions,
            IAlienIdProvider alienIds)
        {
            var errors = new List<string>();
            if (textSource == null)
            {
                errors.Add("Battle balance text source is required.");
                return Invalid(errors);
            }

            var parser = new BattleBalanceJsonParser();
            string json;
            BattleBalanceManifestData manifest = null;
            if (!textSource.TryLoad(BattleBalanceResourcePaths.Manifest, out json))
            {
                errors.Add("Missing required Battle balance resource: " + BattleBalanceResourcePaths.Manifest + ". No fallback is available.");
            }
            else
            {
                BattleBalanceParseResult<BattleBalanceManifestData> result = parser.ParseManifest(json);
                AddErrors(BattleBalanceResourcePaths.Manifest, result.Errors, errors);
                manifest = result.Value;
            }

            BattleBalanceDocument<WaveSpecData> waves = LoadDocument(textSource, BattleBalanceResourcePaths.WaveSpec, parser.ParseWaveDocument, errors);
            BattleBalanceDocument<WaveSpawnSpecData> spawns = LoadDocument(textSource, BattleBalanceResourcePaths.WaveSpawnSpec, parser.ParseWaveSpawnDocument, errors);
            BattleBalanceDocument<BossPatternSpecData> bossPatterns = LoadDocument(textSource, BattleBalanceResourcePaths.BossPatternSpec, parser.ParseBossPatternDocument, errors);
            BattleBalanceDocument<SkillSpecData> skills = LoadDocument(textSource, BattleBalanceResourcePaths.SkillSpec, parser.ParseSkillDocument, errors);
            BattleBalanceDocument<AlienSkillLinkData> alienSkills = LoadDocument(textSource, BattleBalanceResourcePaths.AlienSkillLinks, parser.ParseAlienSkillLinkDocument, errors);
            BattleBalanceDocument<ProjectileSpecData> projectiles = LoadDocument(textSource, BattleBalanceResourcePaths.ProjectileSpec, parser.ParseProjectileDocument, errors);
            BattleBalanceDocument<SkillEffectSpecData> skillEffects = LoadDocument(textSource, BattleBalanceResourcePaths.SkillEffectSpec, parser.ParseSkillEffectDocument, errors);

            int schemaVersion = manifest != null ? manifest.SchemaVersion : 0;
            string balanceVersion = manifest != null ? manifest.BalanceVersion : null;
            string contentHash = manifest != null ? manifest.BundleHash : null;
            if (errors.Count > 0 || manifest == null || waves == null || spawns == null || bossPatterns == null
                || skills == null || alienSkills == null || projectiles == null || skillEffects == null)
                return new BattleBalanceProvider(schemaVersion, balanceVersion, contentHash, null, errors);

            var documents = new BattleBalanceDocuments(waves, spawns, bossPatterns, skills, alienSkills, projectiles, skillEffects);
            BattleBalanceCatalogBuildResult buildResult = BattleBalanceCatalogBuilder.Build(manifest, documents, monsterDefinitions, alienIds);
            errors.AddRange(buildResult.Errors);
            return new BattleBalanceProvider(schemaVersion, balanceVersion, contentHash, buildResult.Catalog, errors);
        }

        private static BattleBalanceDocument<T> LoadDocument<T>(
            IBattleBalanceTextSource textSource,
            string path,
            Func<string, BattleBalanceParseResult<BattleBalanceDocument<T>>> parse,
            List<string> errors)
        {
            string json;
            if (!textSource.TryLoad(path, out json))
            {
                errors.Add("Missing required Battle balance resource: " + path + ". No fallback is available.");
                return null;
            }

            BattleBalanceParseResult<BattleBalanceDocument<T>> result = parse(json);
            AddErrors(path, result.Errors, errors);
            return result.Value;
        }

        private static void AddErrors(string path, IReadOnlyList<string> source, List<string> destination)
        {
            for (int index = 0; index < source.Count; index++)
                destination.Add(path + ": " + source[index]);
        }

        private static BattleBalanceProvider Invalid(IEnumerable<string> errors)
        {
            return new BattleBalanceProvider(0, null, null, null, errors);
        }
    }

    public sealed class ResourcesBattleBalanceProvider : IBattleBalanceProvider
    {
        private readonly BattleBalanceProvider _provider;

        public int SchemaVersion => _provider.SchemaVersion;
        public string BalanceVersion => _provider.BalanceVersion;
        public string ContentHash => _provider.ContentHash;
        public BattleBalanceCatalog Catalog => _provider.Catalog;
        public bool IsValid => _provider.IsValid;
        public IReadOnlyList<string> ValidationErrors => _provider.ValidationErrors;

        public ResourcesBattleBalanceProvider(IMonsterDefinitionProvider monsterDefinitions, IAlienIdProvider alienIds)
        {
            _provider = BattleBalanceProvider.Load(new ResourcesBattleBalanceTextSource(), monsterDefinitions, alienIds);
        }
    }
}
