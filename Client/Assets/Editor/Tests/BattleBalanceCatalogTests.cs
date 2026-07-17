using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Balance;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public class BattleBalanceCatalogTests
    {
        [Test]
        public void Catalog_SortsWavesAndFindsNextEnabledWave()
        {
            BattleBalanceProvider provider = ValidProvider();

            Assert.That(provider.Catalog.Waves.All.Select(item => item.WaveId), Is.EqualTo(new[] { "WAVE_001", "WAVE_002", "WAVE_003" }));
            WaveSpecData wave;
            Assert.That(provider.Catalog.Waves.TryGetByRound(1, out wave), Is.True);
            Assert.That(wave.WaveId, Is.EqualTo("WAVE_001"));
            Assert.That(provider.Catalog.Waves.TryGetById("WAVE_003", out wave), Is.True);
            Assert.That(wave.RoundNumber, Is.EqualTo(3));
            Assert.That(provider.Catalog.Waves.TryGetNextEnabledWave(1, out wave), Is.True);
            Assert.That(wave.WaveId, Is.EqualTo("WAVE_003"));
            Assert.That(provider.Catalog.Waves.TryGetNextEnabledWave(3, out wave), Is.False);
        }

        [Test]
        public void Catalog_SortsSpawnsAndBossPatterns()
        {
            BattleBalanceCatalog catalog = ValidProvider().Catalog;

            Assert.That(catalog.Waves.GetSpawns("WAVE_001").Select(item => item.SpawnOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(catalog.BossPatterns.GetByWave("WAVE_003").Select(item => item.PatternOrder), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Catalog_ResolvesSkillProjectileEffectAndAlienLink()
        {
            BattleBalanceCatalog catalog = ValidProvider().Catalog;
            ProjectileSpecData projectile;
            AlienSkillLinkData link;

            Assert.That(catalog.TryGetProjectileForSkill("SKILL_BASIC", out projectile), Is.True);
            Assert.That(projectile.ProjectileId, Is.EqualTo("PROJ_BASIC"));
            Assert.That(catalog.SkillEffects.GetBySkill("SKILL_BASIC").Select(item => item.ExecutionOrder), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(catalog.AlienSkills.TryGet(101, 0, out link), Is.True);
            Assert.That(link.SkillId, Is.EqualTo("SKILL_BASIC"));
        }

        [Test]
        public void FakeExternalProviders_SatisfyMonsterAndAlienForeignKeys()
        {
            BattleBalanceProvider provider = ValidProvider();

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Assert.That(provider.Catalog.Waves.GetSpawns("WAVE_001").All(item => item.MonsterId == "MON_NORMAL"), Is.True);
            Assert.That(provider.Catalog.AlienSkills.GetByAlien(101).Count, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateWaveId_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.WaveSpec, "\"waveId\":\"WAVE_002\"", "\"waveId\":\"WAVE_001\""), "duplicate ID");
        }

        [Test]
        public void DuplicateEnabledRoundNumber_IsRejected()
        {
            Dictionary<string, string> documents = Mutate(BattleBalanceResourcePaths.WaveSpec, "\"roundNumber\":2", "\"roundNumber\":1");
            documents[BattleBalanceResourcePaths.WaveSpec] = documents[BattleBalanceResourcePaths.WaveSpec].Replace("\"enabled\":false", "\"enabled\":true");
            AssertInvalid(documents, "duplicate roundNumber");
        }

        [Test]
        public void UnknownWaveForeignKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.WaveSpawnSpec, "\"waveId\":\"WAVE_002\"", "\"waveId\":\"WAVE_MISSING\""), "unknown waveId");
        }

        [Test]
        public void UnknownMonsterForeignKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.WaveSpawnSpec, "MON_NORMAL", "MON_MISSING"), "unknown monsterId");
        }

        [Test]
        public void UnknownAlienForeignKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.AlienSkillLinks, "\"alienId\":101", "\"alienId\":999"), "unknown alienId");
        }

        [Test]
        public void UnknownSkillForeignKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.AlienSkillLinks, "SKILL_BASIC", "SKILL_MISSING"), "unknown skillId");
        }

        [Test]
        public void UnknownProjectileForeignKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.SkillSpec, "PROJ_BASIC", "PROJ_MISSING"), "unknown projectileId");
        }

        [Test]
        public void RegularWaveUsingBossSharedLane_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            int index = documents[BattleBalanceResourcePaths.WaveSpawnSpec].IndexOf("EACH_ACTIVE_PLAYER_LANE", StringComparison.Ordinal);
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = ReplaceAt(documents[BattleBalanceResourcePaths.WaveSpawnSpec], index, "EACH_ACTIVE_PLAYER_LANE".Length, "BOSS_SHARED");
            AssertInvalid(documents, "must use EACH_ACTIVE_PLAYER_LANE");
        }

        [Test]
        public void BossWaveUsingPlayerLane_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.WaveSpawnSpec, "BOSS_SHARED", "EACH_ACTIVE_PLAYER_LANE"), "must use BOSS_SHARED");
        }

        [Test]
        public void BossSpawnCountOtherThanOne_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            string source = "\"monsterId\":\"MON_BOSS\",\"spawnCount\":1";
            string replacement = "\"monsterId\":\"MON_BOSS\",\"spawnCount\":2";
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = documents[BattleBalanceResourcePaths.WaveSpawnSpec].Replace(source, replacement);
            AssertInvalid(documents, "exactly one monster");
        }

        [Test]
        public void InvalidBossTimeLimit_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.WaveSpec, "\"bossTimeLimitSeconds\":30", "\"bossTimeLimitSeconds\":0"), "greater than 0");
        }

        [Test]
        public void InvalidMultiplier_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            int index = documents[BattleBalanceResourcePaths.WaveSpawnSpec].IndexOf("\"hpMultiplier\":1", StringComparison.Ordinal);
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = ReplaceAt(documents[BattleBalanceResourcePaths.WaveSpawnSpec], index, "\"hpMultiplier\":1".Length, "\"hpMultiplier\":0");
            AssertInvalid(documents, "hpMultiplier");
        }

        [TestCase("HOMING")]
        [TestCase("LINEAR")]
        [TestCase("BALLISTIC")]
        public void NonInstantProjectileWithZeroSpeed_IsRejected(string moveType)
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.ProjectileSpec] = documents[BattleBalanceResourcePaths.ProjectileSpec]
                .Replace("\"moveType\":\"HOMING\"", "\"moveType\":\"" + moveType + "\"")
                .Replace("\"speed\":8", "\"speed\":0");
            AssertInvalid(documents, "speed must be greater than 0");
        }

        [Test]
        public void InstantProjectileWithZeroSpeed_IsValid()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.ProjectileSpec] = documents[BattleBalanceResourcePaths.ProjectileSpec]
                .Replace("\"moveType\":\"HOMING\"", "\"moveType\":\"INSTANT\"")
                .Replace("\"speed\":8", "\"speed\":0");

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
        }

        [Test]
        public void InstantProjectileWithNegativeSpeed_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.ProjectileSpec] = documents[BattleBalanceResourcePaths.ProjectileSpec]
                .Replace("\"moveType\":\"HOMING\"", "\"moveType\":\"INSTANT\"")
                .Replace("\"speed\":8", "\"speed\":-1");
            AssertInvalid(documents, "speed must be non-negative");
        }

        [Test]
        public void DestroyOnHitWithPiercing_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.ProjectileSpec, "\"pierceCount\":0", "\"pierceCount\":1"), "pierceCount=0");
        }

        [Test]
        public void InvalidDotTick_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            int index = documents[BattleBalanceResourcePaths.SkillEffectSpec].LastIndexOf("\"effectType\":\"DAMAGE\"", StringComparison.Ordinal);
            documents[BattleBalanceResourcePaths.SkillEffectSpec] = ReplaceAt(documents[BattleBalanceResourcePaths.SkillEffectSpec], index, "\"effectType\":\"DAMAGE\"".Length, "\"effectType\":\"DAMAGE_OVER_TIME\"");
            AssertInvalid(documents, "DAMAGE_OVER_TIME");
        }

        [Test]
        public void SplashDamageWithZeroRadius_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            string source = "\"effectType\":\"SPLASH_DAMAGE\",\"magnitudeSource\":\"ATTACK_SNAPSHOT_DAMAGE\",\"baseMagnitude\":0,\"coefficient\":0.5,\"chance\":1,\"durationSeconds\":0,\"tickIntervalSeconds\":0,\"radius\":2";
            string replacement = source.Replace("\"radius\":2", "\"radius\":0");
            documents[BattleBalanceResourcePaths.SkillEffectSpec] = documents[BattleBalanceResourcePaths.SkillEffectSpec].Replace(source, replacement);
            AssertInvalid(documents, "radius must be greater than 0");
        }

        [Test]
        public void ChanceOutsideRange_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            int index = documents[BattleBalanceResourcePaths.SkillEffectSpec].IndexOf("\"chance\":1", StringComparison.Ordinal);
            documents[BattleBalanceResourcePaths.SkillEffectSpec] = ReplaceAt(documents[BattleBalanceResourcePaths.SkillEffectSpec], index, "\"chance\":1".Length, "\"chance\":1.1");
            AssertInvalid(documents, "chance must be between 0 and 1");
        }

        [Test]
        public void DuplicateEffectExecutionOrder_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            int index = documents[BattleBalanceResourcePaths.SkillEffectSpec].IndexOf("\"skillId\":\"SKILL_BASIC\",\"executionOrder\":2", StringComparison.Ordinal);
            string source = "\"skillId\":\"SKILL_BASIC\",\"executionOrder\":2";
            string replacement = "\"skillId\":\"SKILL_BASIC\",\"executionOrder\":1";
            documents[BattleBalanceResourcePaths.SkillEffectSpec] = ReplaceAt(documents[BattleBalanceResourcePaths.SkillEffectSpec], index, source.Length, replacement);
            AssertInvalid(documents, "duplicate (skillId, executionOrder)");
        }

        [Test]
        public void MaxStacksOne_IsValid()
        {
            Assert.That(ValidProvider().IsValid, Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void MaxStacksBelowOne_IsRejected(int maxStacks)
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.SkillEffectSpec, "\"maxStacks\":1", "\"maxStacks\":" + maxStacks), "maxStacks must be at least 1");
        }

        [Test]
        public void MaxTargetCountZero_IsValidAndPreserved()
        {
            Dictionary<string, string> documents = Mutate(BattleBalanceResourcePaths.SkillSpec, "\"maxTargetCount\":1", "\"maxTargetCount\":0");
            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);
            SkillSpecData skill;

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Assert.That(provider.Catalog.Skills.TryGet("SKILL_BASIC", out skill), Is.True);
            Assert.That(skill.MaxTargetCount, Is.Zero);
        }

        [Test]
        public void MaxTargetCountOne_IsValid()
        {
            SkillSpecData skill;
            Assert.That(ValidProvider().Catalog.Skills.TryGet("SKILL_BASIC", out skill), Is.True);
            Assert.That(skill.MaxTargetCount, Is.EqualTo(1));
        }

        [Test]
        public void NegativeMaxTargetCount_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.SkillSpec, "\"maxTargetCount\":1", "\"maxTargetCount\":-1"), "maxTargetCount must be non-negative");
        }

        [Test]
        public void RegularNormalCountingMonster_IsValid()
        {
            Assert.That(ValidProvider().IsValid, Is.True);
        }

        [Test]
        public void RegularEliteCountingMonster_IsValid()
        {
            Dictionary<string, string> documents = Mutate(BattleBalanceResourcePaths.WaveSpawnSpec, "MON_NORMAL", "MON_ELITE");
            IMonsterDefinitionProvider monsters = BattleBalanceTestFixture.MonsterProvider(
                new BattleMonsterDefinition("MON_ELITE", "ELITE", 150f, 2f, "Elite", true),
                new BattleMonsterDefinition("MON_BOSS", "BOSS", 1000f, 1f, "Boss", false));

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents, monsters);

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
        }

        [Test]
        public void RegularBossMonster_IsRejected()
        {
            IMonsterDefinitionProvider monsters = MonsterProvider("BOSS", true, "BOSS", false);
            AssertInvalid(BattleBalanceTestFixture.CreateDocuments(), monsters, "only NORMAL or ELITE");
        }

        [Test]
        public void RegularNonCountingMonster_IsRejected()
        {
            IMonsterDefinitionProvider monsters = MonsterProvider("NORMAL", false, "BOSS", false);
            AssertInvalid(BattleBalanceTestFixture.CreateDocuments(), monsters, "CountsTowardLaneLimit=true");
        }

        [Test]
        public void BossBossNonCountingMonster_IsValid()
        {
            Assert.That(ValidProvider().IsValid, Is.True);
        }

        [TestCase("NORMAL")]
        [TestCase("ELITE")]
        public void BossNonBossMonster_IsRejected(string monsterType)
        {
            IMonsterDefinitionProvider monsters = MonsterProvider("NORMAL", true, monsterType, false);
            AssertInvalid(BattleBalanceTestFixture.CreateDocuments(), monsters, "only BOSS monsters");
        }

        [Test]
        public void BossCountingMonster_IsRejected()
        {
            IMonsterDefinitionProvider monsters = MonsterProvider("NORMAL", true, "BOSS", true);
            AssertInvalid(BattleBalanceTestFixture.CreateDocuments(), monsters, "CountsTowardLaneLimit=false");
        }

        [Test]
        public void UnknownMonsterType_IsRejectedWithoutCaseNormalization()
        {
            IMonsterDefinitionProvider monsters = MonsterProvider("normal", true, "BOSS", false);
            AssertInvalid(BattleBalanceTestFixture.CreateDocuments(), monsters, "unsupported monsterType");
        }

        [Test]
        public void EnabledRegularWaveWithoutSpawn_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = RemoveJsonObjectsContaining(
                documents[BattleBalanceResourcePaths.WaveSpawnSpec], "\"waveId\":\"WAVE_001\"");
            AssertInvalid(documents, "at least one WaveSpawnSpec row");
        }

        [Test]
        public void EnabledRegularWaveWithSpawn_IsValid()
        {
            Assert.That(ValidProvider().Catalog.Waves.GetSpawns("WAVE_001").Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void EnabledBossWaveWithoutSpawn_IsRejected()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = RemoveJsonObjectsContaining(
                documents[BattleBalanceResourcePaths.WaveSpawnSpec], "\"waveId\":\"WAVE_003\"");
            AssertInvalid(documents, "exactly one monster");
        }

        [Test]
        public void DisabledRegularWaveWithoutSpawn_RemainsValid()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = RemoveJsonObjectsContaining(
                documents[BattleBalanceResourcePaths.WaveSpawnSpec], "\"waveId\":\"WAVE_002\"");

            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents);

            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Assert.That(provider.Catalog.Waves.GetSpawns("WAVE_002"), Is.Empty);
        }

        [Test]
        public void NegativeBaseMagnitude_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.SkillEffectSpec, "\"baseMagnitude\":0", "\"baseMagnitude\":-1"), "baseMagnitude must be non-negative");
        }

        [Test]
        public void NegativeCoefficient_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.SkillEffectSpec, "\"coefficient\":1", "\"coefficient\":-1"), "coefficient must be non-negative");
        }

        [Test]
        public void ZeroBaseMagnitudeAndCoefficient_AreValid()
        {
            Dictionary<string, string> documents = Mutate(BattleBalanceResourcePaths.SkillEffectSpec, "\"coefficient\":0.5", "\"coefficient\":0");
            Assert.That(BattleBalanceTestFixture.Load(documents).IsValid, Is.True);
        }

        [Test]
        public void BlankSkillNameKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.SkillSpec, "\"nameKey\":\"basic\"", "\"nameKey\":\" \""), "SkillSpec.nameKey");
        }

        [Test]
        public void BlankProjectilePrefabKey_IsRejected()
        {
            AssertInvalid(Mutate(BattleBalanceResourcePaths.ProjectileSpec, "\"prefabKey\":\"Bullet\"", "\"prefabKey\":\" \""), "ProjectileSpec.prefabKey");
        }

        private static BattleBalanceProvider ValidProvider()
        {
            BattleBalanceProvider provider = BattleBalanceTestFixture.Load();
            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            return provider;
        }

        private static Dictionary<string, string> Mutate(string path, string source, string replacement)
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            Assert.That(documents[path].Contains(source), Is.True, "Fixture mutation source was not found: " + source);
            documents[path] = documents[path].Replace(source, replacement);
            return documents;
        }

        private static void AssertInvalid(Dictionary<string, string> documents, string expectedError)
        {
            AssertInvalid(documents, BattleBalanceTestFixture.Monsters(), expectedError);
        }

        private static void AssertInvalid(
            Dictionary<string, string> documents,
            IMonsterDefinitionProvider monsters,
            string expectedError)
        {
            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(documents, monsters);
            Assert.That(provider.IsValid, Is.False);
            Assert.That(provider.Catalog, Is.Null);
            Assert.That(provider.ValidationErrors.Any(error => error.Contains(expectedError)), Is.True,
                "Expected error containing '" + expectedError + "'. Actual:\n" + string.Join("\n", provider.ValidationErrors));
        }

        private static string ReplaceAt(string value, int index, int length, string replacement)
        {
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "Fixture mutation target was not found.");
            return value.Remove(index, length).Insert(index, replacement);
        }

        private static IMonsterDefinitionProvider MonsterProvider(
            string regularType,
            bool regularCounts,
            string bossType,
            bool bossCounts)
        {
            return BattleBalanceTestFixture.MonsterProvider(
                new BattleMonsterDefinition("MON_NORMAL", regularType, 100f, 2f, "Monster", regularCounts),
                new BattleMonsterDefinition("MON_BOSS", bossType, 1000f, 1f, "Boss", bossCounts));
        }

        private static string RemoveJsonObjectsContaining(string json, string marker)
        {
            while (true)
            {
                int markerIndex = json.IndexOf(marker, StringComparison.Ordinal);
                if (markerIndex < 0) return json;
                int start = json.LastIndexOf('{', markerIndex);
                int end = json.IndexOf('}', markerIndex);
                Assert.That(start, Is.GreaterThanOrEqualTo(0));
                Assert.That(end, Is.GreaterThan(start));

                int removeStart = start;
                int removeLength = end - start + 1;
                if (end + 1 < json.Length && json[end + 1] == ',') removeLength++;
                else if (start > 0 && json[start - 1] == ',')
                {
                    removeStart--;
                    removeLength++;
                }
                json = json.Remove(removeStart, removeLength);
            }
        }
    }
}
