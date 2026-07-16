using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyDefense.Battle.Balance;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public class BattleBalanceLoaderTests
    {
        [Test]
        public void InMemorySevenDocumentsAndManifest_LoadSuccessfully()
        {
            BattleBalanceProvider provider = BattleBalanceTestFixture.Load();

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Assert.That(provider.SchemaVersion, Is.EqualTo(1));
            Assert.That(provider.BalanceVersion, Is.EqualTo(BattleBalanceTestFixture.BalanceVersion));
            Assert.That(provider.ContentHash, Is.EqualTo(BattleBalanceTestFixture.BundleHash));
            Assert.That(provider.Catalog, Is.Not.Null);
        }

        [Test]
        public void MalformedJson_ReturnsFailureWithoutFallback()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.WaveSpec] = "{";

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.Catalog, Is.Null);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("JSON")), Is.True);
        }

        [Test]
        public void MissingRequiredRootField_ReturnsFailure()
        {
            var parser = new BattleBalanceJsonParser();
            string json = BattleBalanceTestFixture.Document("[]").Replace("\"contentHash\":\"" + BattleBalanceTestFixture.ContentHash + "\",", string.Empty);

            BattleBalanceParseResult<BattleBalanceDocument<WaveSpecData>> result = parser.ParseWaveDocument(json);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.Contains("contentHash")), Is.True);
        }

        [Test]
        public void SchemaVersionMismatch_ReturnsFailure()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.ProjectileSpec] = documents[BattleBalanceResourcePaths.ProjectileSpec]
                .Replace("\"schemaVersion\":1", "\"schemaVersion\":2");

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("schemaVersion")), Is.True);
        }

        [Test]
        public void BalanceVersionMismatch_ReturnsFailure()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.ProjectileSpec] = documents[BattleBalanceResourcePaths.ProjectileSpec]
                .Replace(BattleBalanceTestFixture.BalanceVersion, "BATTLE_OTHER_V1");

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("balanceVersion")), Is.True);
        }

        [Test]
        public void ManifestMissingRequiredFile_ReturnsFailure()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.Manifest] = documents[BattleBalanceResourcePaths.Manifest]
                .Replace(BattleBalanceResourcePaths.SkillEffectSpec, "Balance/Battle/unregistered-spec");

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("missing required Battle file")), Is.True);
        }

        [Test]
        public void EnumParsing_IsCaseSensitive()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.WaveSpec] = documents[BattleBalanceResourcePaths.WaveSpec]
                .Replace("\"waveType\":\"REGULAR\"", "\"waveType\":\"regular\"");

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("case-sensitive")), Is.True);
        }

        [Test]
        public void InMemorySource_RequestsOnlyExtensionlessResourcePaths()
        {
            var source = new InMemoryBattleBalanceTextSource(BattleBalanceTestFixture.CreateDocuments());

            BattleBalanceProvider provider = BattleBalanceProvider.Load(source, BattleBalanceTestFixture.Monsters(), BattleBalanceTestFixture.Aliens());

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Assert.That(source.RequestedPaths.Count, Is.EqualTo(8));
            Assert.That(source.RequestedPaths.All(path => !BattleBalanceResourcePaths.HasFileExtension(path)), Is.True);
        }

        [Test]
        public void MissingResource_ReturnsInvalidAndDoesNotUseFallback()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents.Remove(BattleBalanceResourcePaths.WaveSpawnSpec);

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.Catalog, Is.Null);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("No fallback")), Is.True);
        }

        [Test]
        public void ResourcesSource_MissingAsset_ReturnsFalseWithoutFallback()
        {
            var source = new ResourcesBattleBalanceTextSource();
            string json;

            bool loaded = source.TryLoad("Balance/Battle/__task9_missing_resource__", out json);

            Assert.That(loaded, Is.False);
            Assert.That(json, Is.Null);
        }

        [Test]
        public void SkillMissingRequiredMaxTargetCount_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.SkillSpec] = documents[BattleBalanceResourcePaths.SkillSpec]
                .Replace(",\"maxTargetCount\":1", string.Empty);

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("missing required field 'maxTargetCount'")), Is.True);
        }

        [Test]
        public void BossPatternMissingRequiredParameterValue_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.BossPatternSpec] = documents[BattleBalanceResourcePaths.BossPatternSpec]
                .Replace(",\"parameterValue\":0", string.Empty);

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains("missing required field 'parameterValue'")), Is.True);
        }

        [Test]
        public void ExplicitZeroParameterValue_ParsesAsZero()
        {
            var parser = new BattleBalanceJsonParser();
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();

            BattleBalanceParseResult<BattleBalanceDocument<BossPatternSpecData>> result = parser.ParseBossPatternDocument(
                documents[BattleBalanceResourcePaths.BossPatternSpec]);

            Assert.That(result.IsValid, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Value.Items.All(item => item.ParameterValue == 0f), Is.True);
        }
    }

    internal static class BattleBalanceTestFixture
    {
        internal const string BalanceVersion = "BATTLE_TEST_V1";
        internal const string ContentHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        internal const string BundleHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        internal static BattleBalanceProvider Load(
            Dictionary<string, string> documents = null,
            IMonsterDefinitionProvider monsters = null,
            IAlienIdProvider aliens = null)
        {
            return BattleBalanceProvider.Load(
                new InMemoryBattleBalanceTextSource(documents ?? CreateDocuments()),
                monsters ?? Monsters(),
                aliens ?? Aliens());
        }

        internal static Dictionary<string, string> CreateDocuments()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            result.Add(BattleBalanceResourcePaths.Manifest, Manifest());
            result.Add(BattleBalanceResourcePaths.WaveSpec, Document(
                "["
                + "{\"waveId\":\"WAVE_003\",\"roundNumber\":3,\"waveType\":\"BOSS\",\"nextWaveDelaySeconds\":2,\"bossTimeLimitSeconds\":30,\"enabled\":true},"
                + "{\"waveId\":\"WAVE_001\",\"roundNumber\":1,\"waveType\":\"REGULAR\",\"nextWaveDelaySeconds\":1,\"bossTimeLimitSeconds\":0,\"enabled\":true},"
                + "{\"waveId\":\"WAVE_002\",\"roundNumber\":2,\"waveType\":\"REGULAR\",\"nextWaveDelaySeconds\":1,\"bossTimeLimitSeconds\":0,\"enabled\":false}"
                + "]"));
            result.Add(BattleBalanceResourcePaths.WaveSpawnSpec, Document(
                "["
                + "{\"waveId\":\"WAVE_001\",\"spawnOrder\":2,\"lanePolicy\":\"EACH_ACTIVE_PLAYER_LANE\",\"monsterId\":\"MON_NORMAL\",\"spawnCount\":2,\"spawnDelaySeconds\":1,\"spawnIntervalSeconds\":0.5,\"hpMultiplier\":1,\"moveSpeedMultiplier\":1},"
                + "{\"waveId\":\"WAVE_003\",\"spawnOrder\":1,\"lanePolicy\":\"BOSS_SHARED\",\"monsterId\":\"MON_BOSS\",\"spawnCount\":1,\"spawnDelaySeconds\":0,\"spawnIntervalSeconds\":0,\"hpMultiplier\":2,\"moveSpeedMultiplier\":1},"
                + "{\"waveId\":\"WAVE_001\",\"spawnOrder\":1,\"lanePolicy\":\"EACH_ACTIVE_PLAYER_LANE\",\"monsterId\":\"MON_NORMAL\",\"spawnCount\":3,\"spawnDelaySeconds\":0,\"spawnIntervalSeconds\":0.5,\"hpMultiplier\":1,\"moveSpeedMultiplier\":1},"
                + "{\"waveId\":\"WAVE_002\",\"spawnOrder\":1,\"lanePolicy\":\"EACH_ACTIVE_PLAYER_LANE\",\"monsterId\":\"MON_NORMAL\",\"spawnCount\":1,\"spawnDelaySeconds\":0,\"spawnIntervalSeconds\":0.5,\"hpMultiplier\":1,\"moveSpeedMultiplier\":1}"
                + "]"));
            result.Add(BattleBalanceResourcePaths.BossPatternSpec, Document(
                "["
                + "{\"waveId\":\"WAVE_003\",\"patternOrder\":2,\"patternType\":\"WAIT\",\"triggerType\":\"TIME\",\"triggerValue\":5,\"cooldownSeconds\":1,\"skillId\":\"\",\"parameterKey\":\"\",\"parameterValue\":0,\"enabled\":true},"
                + "{\"waveId\":\"WAVE_003\",\"patternOrder\":1,\"patternType\":\"CAST_SKILL\",\"triggerType\":\"ON_SPAWN\",\"triggerValue\":0,\"cooldownSeconds\":2,\"skillId\":\"SKILL_BOSS\",\"parameterKey\":\"\",\"parameterValue\":0,\"enabled\":true}"
                + "]"));
            result.Add(BattleBalanceResourcePaths.SkillSpec, Document(
                "["
                + "{\"skillId\":\"SKILL_BOSS\",\"nameKey\":\"boss\",\"descriptionKey\":\"boss.desc\",\"skillType\":\"BOSS\",\"triggerType\":\"BOSS_PATTERN\",\"cooldownSeconds\":2,\"mpCost\":0,\"castRange\":10,\"targetPolicy\":\"DEFAULT_PROGRESS\",\"maxTargetCount\":1,\"projectileId\":\"\",\"animationKey\":\"boss_cast\",\"vfxKey\":\"boss_vfx\",\"sfxKey\":\"boss_sfx\",\"enabled\":true},"
                + "{\"skillId\":\"SKILL_BASIC\",\"nameKey\":\"basic\",\"descriptionKey\":\"basic.desc\",\"skillType\":\"BASIC_ATTACK\",\"triggerType\":\"BASIC_ATTACK\",\"cooldownSeconds\":1,\"mpCost\":0,\"castRange\":5,\"targetPolicy\":\"DEFAULT_PROGRESS\",\"maxTargetCount\":1,\"projectileId\":\"PROJ_BASIC\",\"animationKey\":\"attack\",\"vfxKey\":\"hit\",\"sfxKey\":\"shot\",\"enabled\":true}"
                + "]"));
            result.Add(BattleBalanceResourcePaths.AlienSkillLinks, Document(
                "[{\"alienId\":101,\"skillId\":\"SKILL_BASIC\",\"slotIndex\":0,\"castPriority\":1,\"enabled\":true}]"));
            result.Add(BattleBalanceResourcePaths.ProjectileSpec, Document(
                "[{\"projectileId\":\"PROJ_BASIC\",\"prefabKey\":\"Bullet\",\"moveType\":\"HOMING\",\"speed\":8,\"lifetimeSeconds\":5,\"hitRadius\":0.2,\"pierceCount\":0,\"destroyOnHit\":true,\"lostTargetPolicy\":\"DESTROY\",\"enabled\":true}]"));
            result.Add(BattleBalanceResourcePaths.SkillEffectSpec, Document(
                "["
                + "{\"skillId\":\"SKILL_BASIC\",\"executionOrder\":2,\"triggerPhase\":\"ON_HIT\",\"effectType\":\"SPLASH_DAMAGE\",\"magnitudeSource\":\"ATTACK_SNAPSHOT_DAMAGE\",\"baseMagnitude\":0,\"coefficient\":0.5,\"chance\":1,\"durationSeconds\":0,\"tickIntervalSeconds\":0,\"radius\":2,\"maxStacks\":1,\"stackPolicy\":\"NONE\",\"bossMultiplier\":1},"
                + "{\"skillId\":\"SKILL_BOSS\",\"executionOrder\":1,\"triggerPhase\":\"ON_CAST\",\"effectType\":\"DAMAGE\",\"magnitudeSource\":\"FLAT\",\"baseMagnitude\":10,\"coefficient\":1,\"chance\":1,\"durationSeconds\":0,\"tickIntervalSeconds\":0,\"radius\":0,\"maxStacks\":1,\"stackPolicy\":\"NONE\",\"bossMultiplier\":1},"
                + "{\"skillId\":\"SKILL_BASIC\",\"executionOrder\":1,\"triggerPhase\":\"ON_HIT\",\"effectType\":\"DAMAGE\",\"magnitudeSource\":\"ATTACK_SNAPSHOT_DAMAGE\",\"baseMagnitude\":0,\"coefficient\":1,\"chance\":1,\"durationSeconds\":0,\"tickIntervalSeconds\":0,\"radius\":0,\"maxStacks\":1,\"stackPolicy\":\"NONE\",\"bossMultiplier\":1}"
                + "]"));
            return result;
        }

        internal static string Document(string items)
        {
            return "{\"schemaVersion\":1,\"balanceVersion\":\"" + BalanceVersion
                + "\",\"contentHash\":\"" + ContentHash + "\",\"items\":" + items + "}";
        }

        internal static IMonsterDefinitionProvider Monsters()
        {
            return MonsterProvider(
                new BattleMonsterDefinition("MON_NORMAL", "NORMAL", 100f, 2f, "Monster", true),
                new BattleMonsterDefinition("MON_BOSS", "BOSS", 1000f, 1f, "Boss", false));
        }

        internal static IMonsterDefinitionProvider MonsterProvider(params BattleMonsterDefinition[] definitions)
        {
            return new FakeMonsterProvider(definitions);
        }

        internal static IAlienIdProvider Aliens()
        {
            return new FakeAlienProvider(101);
        }

        private static string Manifest()
        {
            var builder = new StringBuilder();
            builder.Append("{\"schemaVersion\":1,\"balanceVersion\":\"").Append(BalanceVersion)
                .Append("\",\"bundleHash\":\"").Append(BundleHash).Append("\",\"files\":[");
            for (int index = 0; index < BattleBalanceResourcePaths.RequiredDocumentPaths.Count; index++)
            {
                if (index > 0) builder.Append(',');
                builder.Append("{\"resourcePath\":\"").Append(BattleBalanceResourcePaths.RequiredDocumentPaths[index])
                    .Append("\",\"contentHash\":\"").Append(ContentHash).Append("\"}");
            }
            return builder.Append("]}").ToString();
        }

        private sealed class FakeMonsterProvider : IMonsterDefinitionProvider
        {
            private readonly Dictionary<string, BattleMonsterDefinition> _definitions;

            public FakeMonsterProvider(IEnumerable<BattleMonsterDefinition> definitions)
            {
                _definitions = definitions.ToDictionary(item => item.MonsterId, StringComparer.Ordinal);
            }

            public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
            {
                return _definitions.TryGetValue(monsterId, out definition);
            }
        }

        private sealed class FakeAlienProvider : IAlienIdProvider
        {
            private readonly HashSet<long> _alienIds;

            public FakeAlienProvider(params long[] alienIds)
            {
                _alienIds = new HashSet<long>(alienIds);
            }

            public bool Contains(long alienId)
            {
                return _alienIds.Contains(alienId);
            }
        }
    }
}
