using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleCanonicalBalanceTests
    {
        private const string MonsterPrefabPath = "Assets/Prefabs/Monsters/Monster.prefab";

        [Test]
        public void CanonicalBundle_LoadsManifestRegistriesAndMappings()
        {
            CanonicalBalanceLoadResult result = LoadProduction();

            Assert.That(result.IsValid, Is.True, JoinErrors(result.Errors));
            Assert.That(result.Bundle.Manifest.SchemaVersion, Is.EqualTo(1));
            Assert.That(result.Bundle.Manifest.BalanceVersion, Is.EqualTo("1-50da09ac4ade04f8"));
            Assert.That(result.Bundle.Manifest.ContentHash, Is.EqualTo("50da09ac4ade04f8630987b89af7cc1c9a48a79a29212199c415aafc82f3a2ba"));

            AssertMonster(result.Bundle.MonsterDefinitions, "NORMAL_MONSTER", "NORMAL", 30f, 5f, 20, true);
            AssertMonster(result.Bundle.MonsterDefinitions, "ELITE_MONSTER", "ELITE", 60f, 4f, 40, true);
            AssertMonster(result.Bundle.MonsterDefinitions, "WAVE_BOSS", "WAVE_BOSS", 300f, 2f, 200, false);
            Assert.That(result.Bundle.MonsterDefinitions.TryGet("UNKNOWN", out _), Is.False);

            Assert.That(result.Bundle.FieldLimits.TryGet("COOP_STANDARD", 2, out CanonicalFieldLimit limit), Is.True);
            Assert.That(limit.MaxAliveMonsterCountPerField, Is.EqualTo(100));
            Assert.That(limit.WarningThreshold, Is.EqualTo(80));
            Assert.That(limit.DangerThreshold, Is.EqualTo(90));
            Assert.That(result.Bundle.Waves.All.Count, Is.EqualTo(80));
            Assert.That(result.Bundle.WaveSpawns.GetByGroup("WAVE_10_BOSS").Single().LanePolicy, Is.EqualTo(CanonicalLanePolicy.BOSS_SHARED));
            Assert.That(result.Bundle.PlanetBattles.All, Has.Count.EqualTo(9));
            Assert.That(result.Bundle.PlanetBattles.TryGet("EARTH", out CanonicalPlanetBattle earth), Is.True);
            Assert.That(earth.HpMultiplier, Is.EqualTo(4.3f).Within(0.001f));
            Assert.That(earth.SpeedMultiplier, Is.EqualTo(1.15f).Within(0.001f));
            Assert.That(earth.BossHpMultiplier, Is.EqualTo(3f));
            Assert.That(result.Bundle.PlanetBattles.TryGet("SUN", out CanonicalPlanetBattle sun), Is.True);
            Assert.That(sun.HpMultiplier, Is.EqualTo(11f));
            Assert.That(sun.SpeedMultiplier, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(result.Bundle.Summon, Is.Not.Null);
            Assert.That(result.Bundle.Summon.TryGetCost(0, out int firstCost), Is.True);
            Assert.That(firstCost, Is.EqualTo(50));
            Assert.That(result.Bundle.Summon.TryGetCost(1, out int secondCost), Is.True);
            Assert.That(secondCost, Is.EqualTo(60));
            Assert.That(result.Bundle.SummonPools.TryGetValue("STANDARD_SUMMON_POOL", out CanonicalSummonPool pool), Is.True);
            Assert.That(pool.Entries, Has.Count.EqualTo(1));
            Assert.That(pool.Entries[0].Grade, Is.EqualTo("NORMAL"));
            Assert.That(pool.Entries[0].Weight, Is.EqualTo(10000));
            Assert.That(pool.Entries[0].AlienIds, Is.EqualTo(new long[] { 22, 23, 24, 25, 26, 27, 28 }));
        }

        [Test]
        public void CompositeProvider_UsesCanonicalWavesAndSkipsLegacyWaveResources()
        {
            CanonicalBalanceLoadResult canonical = LoadProduction();
            var source = new RecordingBattleTextSource(new ResourcesBattleBalanceTextSource());

            Assert.That(CanonicalBattleAlienIdProvider.TryCreate(out CanonicalBattleAlienIdProvider alienIds, out string alienError), Is.True, alienError);
            CanonicalCompositeBattleBalanceProvider provider = CanonicalCompositeBattleBalanceProvider.Load(
                canonical,
                source,
                alienIds);

            Assert.That(provider.IsValid, Is.True, JoinErrors(provider.ValidationErrors));
            Assert.That(source.Requested, Does.Not.Contain(BattleBalanceResourcePaths.WaveSpec));
            Assert.That(source.Requested, Does.Not.Contain(BattleBalanceResourcePaths.WaveSpawnSpec));
            Assert.That(provider.Catalog.Waves.TryGetByRound(1, out WaveSpecData wave), Is.True);
            Assert.That(wave.WaveId, Is.EqualTo("COOP_STANDARD:1"));
            Assert.That(wave.NextWaveDelaySeconds, Is.EqualTo(3f));
            WaveSpawnSpecData spawn = provider.Catalog.Waves.GetSpawns(wave.WaveId).Single();
            Assert.That(spawn.MonsterId, Is.EqualTo("NORMAL_MONSTER"));
            Assert.That(spawn.SpawnCount, Is.EqualTo(12));
            Assert.That(spawn.HpMultiplier, Is.EqualTo(1f));
            Assert.That(provider.Catalog.BossPatterns.GetByWave("COOP_STANDARD:10"), Has.Count.EqualTo(2));
            Assert.That(provider.Catalog.BossPatterns.GetByWave("COOP_STANDARD:80"), Has.Count.EqualTo(2));
            Assert.That(provider.Catalog.BossPatterns.GetByWave("WAVE_010"), Is.Empty);
        }

        [Test]
        public void KidnapPoolResolver_IsDeterministicAndUsesCanonicalIds()
        {
            CanonicalBalanceLoadResult result = LoadProduction();
            Assert.That(result.IsValid, Is.True, JoinErrors(result.Errors));

            bool first = BattleKidnapPoolResolver.TrySelect(
                result.Bundle.SummonPools,
                "STANDARD_SUMMON_POOL",
                12345UL,
                out long firstAlienId,
                out byte firstGrade);
            bool second = BattleKidnapPoolResolver.TrySelect(
                result.Bundle.SummonPools,
                "STANDARD_SUMMON_POOL",
                12345UL,
                out long secondAlienId,
                out byte secondGrade);

            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(secondAlienId, Is.EqualTo(firstAlienId));
            Assert.That(firstGrade, Is.EqualTo(0));
            Assert.That(secondGrade, Is.EqualTo(firstGrade));
            Assert.That(firstAlienId, Is.InRange(22L, 28L));
        }

        [Test]
        public void ManifestHashMismatch_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            byte[] bytes = files[CanonicalBalanceContract.MonsterFileName];
            bytes[bytes.Length - 2] ^= 1;

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "SHA-256 mismatch");
        }

        [Test]
        public void ManifestSizeMismatch_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            ManifestJson manifest = ParseManifest(files);
            manifest.files.Single(entry => entry.name == CanonicalBalanceContract.MonsterFileName).size++;
            files[CanonicalBalanceContract.ManifestFileName] = Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest, true));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "size mismatch");
        }

        [Test]
        public void ExpectedBalanceVersionMismatch_IsRejected()
        {
            CanonicalBalanceLoadResult result = CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(),
                new ExistingMonsterPrefabRuntimeMapping(),
                "wrong-version");

            AssertInvalidContaining(result, "expected session version");
        }

        [Test]
        public void SchemaVersionMismatch_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            ManifestJson manifest = ParseManifest(files);
            manifest.schemaVersion = 2;
            files[CanonicalBalanceContract.ManifestFileName] = Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest, true));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "schemaVersion");
        }

        [Test]
        public void MissingManifestRegistration_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            ManifestJson manifest = ParseManifest(files);
            manifest.files = manifest.files
                .Where(entry => entry.name != CanonicalBalanceContract.FieldLimitFileName)
                .ToArray();
            files[CanonicalBalanceContract.ManifestFileName] = Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest, true));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "missing required file");
        }

        [Test]
        public void MissingCanonicalFile_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            files.Remove(CanonicalBalanceContract.FieldLimitFileName);

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "file is missing");
        }

        [Test]
        public void MergeValidation_RequiresSameAlienAndGradeBelowMythic()
        {
            Assert.That(BattleWaveStateAuthority.CanMerge(0, 1, true, true, 22, 22, 0, 0), Is.True);
            Assert.That(BattleWaveStateAuthority.CanMerge(0, 1, true, true, 22, 23, 0, 0), Is.False);
            Assert.That(BattleWaveStateAuthority.CanMerge(0, 1, true, true, 22, 22, 0, 1), Is.False);
            Assert.That(BattleWaveStateAuthority.CanMerge(0, 1, true, true, 22, 22, 4, 4), Is.False);
            Assert.That(BattleWaveStateAuthority.CanMerge(0, 1, true, false, 22, 22, 0, 0), Is.False);
        }

        [Test]
        public void MergeResultResolver_UsesCanonicalNextGradePool()
        {
            Assert.That(BattleMergeResultResolver.TryResolveRandomNextGrade(0, 0UL, out long alienId, out byte grade), Is.True);
            Assert.That(alienId, Is.InRange(15L, 21L));
            Assert.That(grade, Is.EqualTo(1));
            Assert.That(BattleMergeResultResolver.TryResolveRandomNextGrade(3, 0UL, out _, out _), Is.False);
        }

        [Test]
        public void MissingSummonBalance_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            files.Remove(CanonicalBalanceContract.SummonFileName);

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "summon-balance.json");
        }

        [Test]
        public void UnknownMonsterId_IsRejectedAfterIntegrityValidation()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            MutateAndRebuild(files, CanonicalBalanceContract.WaveSpawnFileName,
                json => json.Replace("NORMAL_MONSTER", "UNKNOWN_MONSTER"));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "unknown monsterId");
        }

        [Test]
        public void DisabledMonsterSpawn_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            MutateAndRebuild(files, CanonicalBalanceContract.MonsterFileName,
                json => json.Replace("\"enabled\" : true", "\"enabled\" : false"));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "disabled monsterId");
        }

        [Test]
        public void InvalidLanePolicy_IsRejected()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            MutateAndRebuild(files, CanonicalBalanceContract.WaveSpawnFileName,
                json => json.Replace("EACH_FIELD", "PLAYER1_ONLY"));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "lanePolicy");
        }

        [Test]
        public void BossSharedSpawn_MustBeExactlyOne()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            MutateAndRebuild(files, CanonicalBalanceContract.WaveSpawnFileName,
                json => json.Replace("\"spawnCountPerField\" : 1,", "\"spawnCountPerField\" : 2,"));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "exactly one BOSS_SHARED");
        }

        [Test]
        public void StandardWaveCatalog_MustContainContinuousOneThroughEighty()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            MutateAndRebuild(files, CanonicalBalanceContract.WaveFileName,
                json => json.Replace("\"wave\" : 80,", "\"wave\" : 79,"));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "continuous from 1 through 80");
        }

        [Test]
        public void StandardWaveCatalog_RequiresBossExactlyEveryTenthWave()
        {
            Dictionary<string, byte[]> files = LoadProductionFiles();
            MutateAndRebuild(files, CanonicalBalanceContract.WaveFileName,
                json => json.Replace("\"isBossWave\" : true", "\"isBossWave\" : false"));

            CanonicalBalanceLoadResult result = Load(files);

            AssertInvalidContaining(result, "Boss waves must be exactly every tenth wave");
        }

        [Test]
        public void ProductionExecutor_UsesCanonicalProviderAndFieldLimit()
        {
            var gameObject = new GameObject("Canonical Executor Test");
            try
            {
                BattleWaveExecutor executor = gameObject.AddComponent<BattleWaveExecutor>();
                SetField(executor, "_monsterPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath));

                bool initialized = (bool)Invoke(executor, "EnsureBalanceInitialized");

                Assert.That(initialized, Is.True);
                object provider = GetField(executor, "_battleBalanceProvider");
                object monsters = GetField(executor, "_monsterDefinitionProvider");
                Assert.That(provider, Is.TypeOf<CanonicalCompositeBattleBalanceProvider>());
                Assert.That(monsters, Is.TypeOf<CanonicalMonsterDefinitionProvider>());
                Assert.That(monsters, Is.Not.TypeOf<TemporaryBattleMonsterDefinitionProvider>());
                Assert.That(executor.MonsterLimit, Is.EqualTo(100));
                Assert.That(executor.MonsterWarningThreshold, Is.EqualTo(80));
                Assert.That(executor.MonsterDangerThreshold, Is.EqualTo(90));
                CanonicalBalanceLoadResult production = LoadProduction();
                Assert.That(production.IsValid, Is.True, JoinErrors(production.Errors));
                Assert.That(executor.CanonicalBalanceVersion, Is.EqualTo(production.Bundle.Manifest.BalanceVersion));
                Assert.That(executor.BattleContentVersion, Is.EqualTo("battle-v1"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [TestCase("P1VAL-EARTH-W009-0123456789ab", 9, WaveType.REGULAR)]
        [TestCase("P1VAL-SUN-W080-abcdef0123456789", 80, WaveType.BOSS)]
        public void P1ValidationExecutor_PausesAutoStartAndConsumesCanonicalTargetOnce(
            string sessionName,
            int expectedWave,
            WaveType expectedType)
        {
            var gameObject = new GameObject("P1 Validation Executor Test");
            try
            {
                Assert.That(BattleP1ValidationSessionProfile.Parse(
                    sessionName,
                    out BattleP1ValidationSessionProfile profile,
                    out string parseReason), Is.EqualTo(BattleP1ValidationParseState.Valid), parseReason);

                BattleWaveExecutor executor = gameObject.AddComponent<BattleWaveExecutor>();
                SetField(executor, "_monsterPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath));
                Assert.That((bool)Invoke(executor, "EnsureBalanceInitialized"), Is.True);
                executor.InitializeSession(
                    new BattleSessionContext(
                        sessionName,
                        executor.CanonicalBalanceVersion,
                        executor.CanonicalContentHash,
                        executor.BattleContentVersion,
                        executor.BattleContentHash,
                        1,
                        profile.MapId),
                    new BattlePlayerIdentityMap("validation-p1", "validation-p2"));

                Assert.That(executor.TryArmP1ValidationInitialWave(profile, out string armReason), Is.True, armReason);
                Assert.That(executor.CurrentRound, Is.Zero, "Arming must not publish a synthetic previous round.");
                executor.StartConfiguredWavesIfReady();
                Assert.That((bool)GetField(executor, "_configuredWaveExecutionStarted"), Is.True);
                Assert.That(executor.IsWaveRunning, Is.False, "Automatic Wave execution must remain paused.");

                SetField(executor, "_isWaveRunning", true);
                Assert.That((bool)Invoke(executor, "TryBeginNextWave"), Is.False);
                Assert.That(executor.IsP1ValidationStartConsumed, Is.False,
                    "A readiness failure must not consume the one allowed validation Wave start.");
                Assert.That(executor.CurrentRound, Is.Zero);
                SetField(executor, "_isWaveRunning", false);

                Assert.That((bool)Invoke(executor, "TryBeginNextWave"), Is.True);
                Assert.That(executor.CurrentRound, Is.EqualTo(expectedWave));
                Assert.That(executor.IsCurrentWaveBoss, Is.EqualTo(expectedType == WaveType.BOSS));
                Assert.That(executor.IsP1ValidationStartConsumed, Is.True);

                Assert.That((bool)Invoke(executor, "TryBeginNextWave"), Is.False);
                Assert.That(executor.CurrentRound, Is.EqualTo(expectedWave));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
#endif

        [Test]
        public void SyncedBundle_MatchesManifestBytes()
        {
            CanonicalBalanceLoadResult result = LoadProduction();

            Assert.That(result.IsValid, Is.True, JoinErrors(result.Errors));
            string root = Path.Combine(Application.streamingAssetsPath, "Balance", "generated");
            foreach (CanonicalManifestFileEntry entry in result.Bundle.Manifest.Files)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(root, entry.Name));
                Assert.That(bytes.LongLength, Is.EqualTo(entry.Size), entry.Name);
                Assert.That(Sha256(bytes), Is.EqualTo(entry.Sha256), entry.Name);
            }
        }

        private static CanonicalBalanceLoadResult LoadProduction()
        {
            return CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(),
                new ExistingMonsterPrefabRuntimeMapping());
        }

        private static CanonicalBalanceLoadResult Load(Dictionary<string, byte[]> files)
        {
            return CanonicalBalanceLoader.Load(
                new InMemoryCanonicalBalanceFileSource(files),
                new ExistingMonsterPrefabRuntimeMapping());
        }

        private static Dictionary<string, byte[]> LoadProductionFiles()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Balance", "generated");
            byte[] manifestBytes = File.ReadAllBytes(Path.Combine(root, CanonicalBalanceContract.ManifestFileName));
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                { CanonicalBalanceContract.ManifestFileName, manifestBytes }
            };
            ManifestJson manifest = JsonUtility.FromJson<ManifestJson>(Encoding.UTF8.GetString(manifestBytes));
            foreach (ManifestFileJson entry in manifest.files)
                result.Add(entry.name, File.ReadAllBytes(Path.Combine(root, entry.name)));
            return result;
        }

        private static void MutateAndRebuild(Dictionary<string, byte[]> files, string fileName, Func<string, string> mutate)
        {
            files[fileName] = Encoding.UTF8.GetBytes(mutate(Encoding.UTF8.GetString(files[fileName])));
            ManifestJson manifest = ParseManifest(files);
            foreach (ManifestFileJson entry in manifest.files)
            {
                byte[] bytes = files[entry.name];
                entry.size = bytes.LongLength;
                entry.sha256 = Sha256(bytes);
            }
            var canonical = new StringBuilder();
            foreach (ManifestFileJson entry in manifest.files)
                canonical.Append(entry.name).Append('\0').Append(entry.sha256).Append('\0').Append(entry.size).Append('\n');
            manifest.contentHash = Sha256(Encoding.UTF8.GetBytes(canonical.ToString()));
            manifest.balanceVersion = manifest.schemaVersion + "-" + manifest.contentHash.Substring(0, 16);
            files[CanonicalBalanceContract.ManifestFileName] = Encoding.UTF8.GetBytes(JsonUtility.ToJson(manifest, true));
        }

        private static ManifestJson ParseManifest(Dictionary<string, byte[]> files)
        {
            return JsonUtility.FromJson<ManifestJson>(Encoding.UTF8.GetString(files[CanonicalBalanceContract.ManifestFileName]));
        }

        private static void AssertMonster(IMonsterDefinitionProvider provider, string id, string type, float hp, float speed, int killGold, bool counts)
        {
            Assert.That(provider.TryGet(id, out BattleMonsterDefinition monster), Is.True, id);
            Assert.That(monster.MonsterType, Is.EqualTo(type));
            Assert.That(monster.BaseMaxHp, Is.EqualTo(hp));
            Assert.That(monster.MoveSpeed, Is.EqualTo(speed));
            Assert.That(monster.KillGold, Is.EqualTo(killGold));
            Assert.That(monster.PrefabKey, Is.EqualTo(ExistingMonsterPrefabRuntimeMapping.ExistingPrefabKey));
            Assert.That(monster.CountsTowardLaneLimit, Is.EqualTo(counts));
        }

        private static void AssertInvalidContaining(CanonicalBalanceLoadResult result, string text)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0),
                Is.True,
                JoinErrors(result.Errors));
        }

        private static string Sha256(byte[] bytes)
        {
            using (SHA256 algorithm = SHA256.Create())
                return string.Concat(algorithm.ComputeHash(bytes).Select(value => value.ToString("x2")));
        }

        private static string JoinErrors(IReadOnlyList<string> errors)
        {
            return string.Join(Environment.NewLine, errors);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static object Invoke(object target, string name)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        [Serializable]
        private sealed class ManifestJson
        {
            public int schemaVersion;
            public string balanceVersion;
            public string contentHash;
            public ManifestFileJson[] files;
        }

        [Serializable]
        private sealed class ManifestFileJson
        {
            public string name;
            public string sha256;
            public long size;
        }

        private sealed class RecordingBattleTextSource : IBattleBalanceTextSource
        {
            private readonly IBattleBalanceTextSource _inner;
            public List<string> Requested { get; } = new List<string>();

            public RecordingBattleTextSource(IBattleBalanceTextSource inner)
            {
                _inner = inner;
            }

            public bool TryLoad(string resourcePath, out string json)
            {
                Requested.Add(resourcePath);
                return _inner.TryLoad(resourcePath, out json);
            }
        }
    }
}
