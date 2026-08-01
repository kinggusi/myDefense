using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Balance;
using NUnit.Framework;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public class BattleBalanceResourcesE2ETests
    {
        [Test]
        public void ActualResources_LoadAsValidBattleCatalog()
        {
            var provider = new ResourcesBattleBalanceProvider(new E2EMonsterProvider(), new ProductionAlienProvider());

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Assert.That(provider.SchemaVersion, Is.EqualTo(1));
            Assert.That(provider.BalanceVersion, Is.EqualTo("battle-v1"));
            Assert.That(provider.ContentHash, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(provider.Catalog.Waves.All.Count, Is.EqualTo(20));
        }

        [Test]
        public void ActualResources_ExposeExpectedWaveProgressionAndBossRules()
        {
            BattleBalanceCatalog catalog = ValidCatalog();
            for (int round = 1; round <= 20; round++)
            {
                WaveSpecData wave;
                Assert.That(catalog.Waves.TryGetByRound(round, out wave), Is.True, "Missing round " + round);
                Assert.That(wave.RoundNumber, Is.EqualTo(round));
            }

            WaveSpecData next;
            Assert.That(catalog.Waves.TryGetNextEnabledWave(1, out next), Is.True);
            Assert.That(next.RoundNumber, Is.EqualTo(2));
            Assert.That(catalog.Waves.TryGetNextEnabledWave(9, out next), Is.True);
            Assert.That(next.RoundNumber, Is.EqualTo(10));
            Assert.That(next.WaveType, Is.EqualTo(WaveType.BOSS));
            Assert.That(next.BossTimeLimitSeconds, Is.EqualTo(30f));
            Assert.That(catalog.Waves.TryGetNextEnabledWave(20, out next), Is.False);
        }

        [Test]
        public void ActualResources_ExposeExpectedSpawnPoliciesAndMultipliers()
        {
            BattleBalanceCatalog catalog = ValidCatalog();
            WaveSpawnSpecData roundOne = catalog.Waves.GetSpawns("WAVE_001").Single();
            WaveSpawnSpecData roundTen = catalog.Waves.GetSpawns("WAVE_010").Single();
            WaveSpawnSpecData roundTwenty = catalog.Waves.GetSpawns("WAVE_020").Single();

            Assert.That(roundOne.LanePolicy, Is.EqualTo(BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE));
            Assert.That(roundOne.SpawnCount, Is.EqualTo(10));
            Assert.That(roundOne.HpMultiplier, Is.EqualTo(1f));
            Assert.That(roundOne.MoveSpeedMultiplier, Is.EqualTo(1f));
            Assert.That(roundTen.LanePolicy, Is.EqualTo(BattleLanePolicy.BOSS_SHARED));
            Assert.That(roundTen.SpawnCount, Is.EqualTo(1));
            Assert.That(roundTen.HpMultiplier, Is.EqualTo(19f));
            Assert.That(roundTwenty.SpawnCount, Is.EqualTo(1));
            Assert.That(roundTwenty.HpMultiplier, Is.EqualTo(29f));
        }

        [Test]
        public void ActualResources_ExposeCanonicalBasicAttackPipeline()
        {
            BattleBalanceCatalog catalog = ValidCatalog();

            Assert.That(catalog.BossPatterns.GetByWave("WAVE_010"), Is.Empty);
            Assert.That(catalog.Skills.All.Select(item => item.SkillId), Is.EquivalentTo(new[] { "SKILL_BASIC" }));
            Assert.That(catalog.AlienSkills.GetByAlien(1).Single().SkillId, Is.EqualTo("SKILL_BASIC"));
            Assert.That(catalog.AlienSkills.GetByAlien(48).Single().SkillId, Is.EqualTo("SKILL_BASIC"));
            Assert.That(catalog.Projectiles.All.Select(item => item.ProjectileId), Is.EquivalentTo(new[] { "PROJ_BASIC" }));
            Assert.That(catalog.SkillEffects.GetBySkill("SKILL_BASIC"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ActualResources_HaveConsistentSchemaVersionAndManifestHashes()
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>(BattleBalanceResourcePaths.Manifest);
            Assert.That(manifestAsset, Is.Not.Null);
            ManifestRaw manifest = JsonUtility.FromJson<ManifestRaw>(manifestAsset.text);

            Assert.That(manifest.schemaVersion, Is.EqualTo(1));
            Assert.That(manifest.balanceVersion, Is.EqualTo("battle-v1"));
            Assert.That(manifest.files, Has.Count.EqualTo(7));
            foreach (string resourcePath in BattleBalanceResourcePaths.RequiredDocumentPaths)
            {
                FileRaw entry = manifest.files.Single(item => item.resourcePath == resourcePath);
                TextAsset documentAsset = Resources.Load<TextAsset>(resourcePath);
                Assert.That(documentAsset, Is.Not.Null, resourcePath);
                DocumentHeaderRaw document = JsonUtility.FromJson<DocumentHeaderRaw>(documentAsset.text);
                Assert.That(document.schemaVersion, Is.EqualTo(manifest.schemaVersion), resourcePath);
                Assert.That(document.balanceVersion, Is.EqualTo(manifest.balanceVersion), resourcePath);
                Assert.That(document.contentHash, Is.EqualTo(entry.contentHash), resourcePath);
                Assert.That(document.contentHash, Does.Match("^[0-9a-f]{64}$"), resourcePath);
            }
        }

        private static BattleBalanceCatalog ValidCatalog()
        {
            var provider = new ResourcesBattleBalanceProvider(new E2EMonsterProvider(), new ProductionAlienProvider());
            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            return provider.Catalog;
        }

        private sealed class E2EMonsterProvider : IMonsterDefinitionProvider
        {
            private readonly Dictionary<string, BattleMonsterDefinition> _definitions =
                new Dictionary<string, BattleMonsterDefinition>(StringComparer.Ordinal)
                {
                    { "MONSTER_NORMAL_DEFAULT", new BattleMonsterDefinition("MONSTER_NORMAL_DEFAULT", "NORMAL", 100f, 2f, "Monster", true) },
                    { "MONSTER_BOSS_DEFAULT", new BattleMonsterDefinition("MONSTER_BOSS_DEFAULT", "BOSS", 1000f, 1f, "Boss", false) }
                };

            public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
            {
                return _definitions.TryGetValue(monsterId, out definition);
            }
        }

        private sealed class ProductionAlienProvider : IAlienIdProvider
        {
            public bool Contains(long alienId)
            {
                return alienId >= 1 && alienId <= 48;
            }
        }

        [Serializable]
        private sealed class ManifestRaw
        {
            public int schemaVersion;
            public string balanceVersion;
            public string bundleHash;
            public List<FileRaw> files;
        }

        [Serializable]
        private sealed class FileRaw
        {
            public string resourcePath;
            public string contentHash;
        }

        [Serializable]
        private sealed class DocumentHeaderRaw
        {
            public int schemaVersion;
            public string balanceVersion;
            public string contentHash;
        }
    }
}
