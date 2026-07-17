using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MyDefense.Battle;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyDefense.Battle.Tests
{
    public class BattleWaveExecutorBalanceTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string MonsterPrefabPath = "Assets/Prefabs/Monsters/Monster.prefab";
        private const string ExpectedBundleHash = "ab535ca2986ca84b196567cefa30c2e20c8f3e277efa131df9766a24c98633bf";

        private GameObject _executorObject;
        private GameObject _spawnPointObject;
        private BattleWaveExecutor _executor;
        private GameObject _monsterPrefab;
        private IMonsterDefinitionProvider _monsterDefinitions;
        private IBattleMonsterPrefabResolver _prefabResolver;
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private int _sessionNumber;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects.Clear();
            _monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            Assert.That(_monsterPrefab, Is.Not.Null);

            TemporaryBattleMonsterDefinitionProvider temporaryProvider;
            string error;
            Assert.That(
                TemporaryBattleMonsterDefinitionProvider.TryCreate(_monsterPrefab, out temporaryProvider, out error),
                Is.True,
                error);
            _monsterDefinitions = temporaryProvider;
            _prefabResolver = new ExplicitBattleMonsterPrefabResolver(
                TemporaryBattleMonsterDefinitionProvider.ExistingMonsterPrefabKey,
                _monsterPrefab);

            _executorObject = new GameObject("BattleWaveExecutor_BalanceTest");
            _executor = _executorObject.AddComponent<BattleWaveExecutor>();
            _spawnPointObject = new GameObject("BattleWaveExecutor_BalanceSpawnPoint");
            SetField("_monsterPrefab", _monsterPrefab);
            SetField("_spawnPoint", _spawnPointObject.transform);
            SetField("_totalMonsterGoal", 100);
            Configure(new ResourcesBattleBalanceProvider(_monsterDefinitions, EmptyBattleAlienIdProvider.Instance));
            InitializeRuntimeSession();
        }

        [TearDown]
        public void TearDown()
        {
            CaptureSpawnedObjects();
            foreach (GameObject spawned in _spawnedObjects)
            {
                if (spawned != null) UnityEngine.Object.DestroyImmediate(spawned);
            }

            if (_spawnPointObject != null) UnityEngine.Object.DestroyImmediate(_spawnPointObject);
            if (_executorObject != null) UnityEngine.Object.DestroyImmediate(_executorObject);
        }

        [Test]
        public void ActualResources_InitializeExecutorWithExpectedBundleHashAndRounds()
        {
            Assert.That(Invoke<bool>("EnsureBalanceInitialized"), Is.True);
            Assert.That(_executor.BattleBalanceContentHash, Is.EqualTo(ExpectedBundleHash));

            IBattleBalanceProvider provider = GetField<IBattleBalanceProvider>("_battleBalanceProvider");
            Assert.That(provider.Catalog.Waves.All.Count, Is.EqualTo(20));
            Assert.That(provider.Catalog.Waves.All.Count(wave => wave.WaveType == WaveType.BOSS), Is.EqualTo(2));
            Assert.That(provider.Catalog.Waves.All.Where(wave => wave.WaveType == WaveType.BOSS)
                .Select(wave => wave.RoundNumber), Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void StartNextWave_SelectsRoundOneFromCatalog()
        {
            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);

            Assert.That(_executor.CurrentRound, Is.EqualTo(1));
            Assert.That(GetField<WaveSpecData>("_currentWaveSpec").WaveId, Is.EqualTo("WAVE_001"));
            Assert.That(GetField<bool>("_isCurrentWaveBoss"), Is.False);
        }

        [Test]
        public void DisabledRoundIsSkipped_AndWaveTypeNotModuloDeterminesBoss()
        {
            IMonsterDefinitionProvider monsters = BattleBalanceTestFixture.Monsters();
            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(
                monsters: monsters,
                aliens: BattleBalanceTestFixture.Aliens());
            Configure(provider, monsters, new RejectingPrefabResolver());
            InitializeRuntimeSession();
            SetField("_currentRound", 1);

            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
            Assert.That(_executor.CurrentRound, Is.EqualTo(3));
            Assert.That(GetField<bool>("_isCurrentWaveBoss"), Is.True);
        }

        [Test]
        public void CatalogExhausted_IsReportedOnceAndFutureStartIsRejected()
        {
            SetField("_currentRound", 20);
            int events = 0;
            _executor.OnCatalogExhausted += () => events++;

            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.False);
            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.False);

            Assert.That(_executor.CurrentRound, Is.EqualTo(20));
            Assert.That(_executor.IsCatalogExhausted, Is.True);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(_executor.IsWaveRunning, Is.False);
        }

        [Test]
        public void EachActivePlayerLane_SpawnCountThreeMeansThreePerLane()
        {
            BeginActualRoundOne();
            WaveSpawnSpecData actual = GetField<IReadOnlyList<WaveSpawnSpecData>>("_currentWaveSpawns").Single();
            SetField(
                "_currentWaveSpawns",
                Array.AsReadOnly(new[]
                {
                    new WaveSpawnSpecData(
                        actual.WaveId,
                        actual.SpawnOrder,
                        actual.LanePolicy,
                        actual.MonsterId,
                        3,
                        0f,
                        0f,
                        actual.HpMultiplier,
                        actual.MoveSpeedMultiplier)
                }));

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(3));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(3));
        }

        [Test]
        public void ActualRoundOne_BothActivePlayersReceiveTenMonstersEach()
        {
            BeginActualRoundOne();

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(10));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(10));
        }

        [Test]
        public void EliminatedLane_SpawnsZeroWhileActiveLaneReceivesAllTen()
        {
            SetField("_player1BattleState", PlayerBattleState.ELIMINATED);
            BeginActualRoundOne();

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            Assert.That(_executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(10));
            Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
        }

        [Test]
        public void LaneEliminatedDuringSpawn_DoesNotReduceOtherLaneSequence()
        {
            BeginActualRoundOne();
            IEnumerator routine = Invoke<IEnumerator>("SpawnRegularWaveRoutine");
            Assert.That(routine.MoveNext(), Is.True);
            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(1));

            SetField("_player1BattleState", PlayerBattleState.ELIMINATED);
            RunToCompletion(routine);

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(1));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(10));
            Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
        }

        [Test]
        public void HundredthRegularMonster_EliminatesOnlyThatPlayerImmediately()
        {
            for (int index = 0; index < 100; index++)
                Invoke("RegisterMonsterSpawned", LaneType.Player1Lane);

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(100));
            Assert.That(_executor.Player1BattleState, Is.EqualTo(PlayerBattleState.ELIMINATED));
            Assert.That(_executor.Player2BattleState, Is.EqualTo(PlayerBattleState.ACTIVE));
            Assert.That(_executor.MatchState, Is.EqualTo(MatchState.RUNNING));
        }

        [Test]
        public void RoundOne_AppliesJsonHpSpeedIntervalAndNextWaveDelay()
        {
            BeginActualRoundOne();
            IEnumerator routine = Invoke<IEnumerator>("SpawnRegularWaveRoutine");

            Assert.That(routine.MoveNext(), Is.True);
            WaitForSeconds wait = routine.Current as WaitForSeconds;
            Assert.That(wait, Is.Not.Null);
            Assert.That(ReadWaitSeconds(wait), Is.EqualTo(1f));

            CaptureSpawnedObjects();
            MonsterStat spawnedStat = _spawnedObjects.Select(item => item.GetComponent<MonsterStat>()).First();
            BattleMonsterMovement movement = spawnedStat.GetComponent<BattleMonsterMovement>();
            Assert.That(spawnedStat.MaxHp, Is.EqualTo(30f));
            Assert.That(movement.Speed, Is.EqualTo(5f));
            Assert.That(GetField<WaveSpecData>("_currentWaveSpec").NextWaveDelaySeconds, Is.EqualTo(3f));
        }

        [Test]
        public void RoundTenBoss_UsesSharedLaneDataAndDoesNotIncrementPlayerCounts()
        {
            SetField("_currentRound", 9);
            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
            WaveSpecData wave = GetField<WaveSpecData>("_currentWaveSpec");
            WaveSpawnSpecData spawn = GetField<IReadOnlyList<WaveSpawnSpecData>>("_currentWaveSpawns").Single();
            BattleMonsterDefinition definition;
            Assert.That(_monsterDefinitions.TryGet(spawn.MonsterId, out definition), Is.True);

            object[] arguments = { LaneType.BossSharedLane, definition, spawn, 2f, null };
            Assert.That(InvokeWithArguments<bool>("SpawnConfiguredMonster", arguments), Is.True);
            GameObject boss = (GameObject)arguments[4];
            _spawnedObjects.Add(boss);

            Assert.That(_executor.CurrentRound, Is.EqualTo(10));
            Assert.That(wave.WaveType, Is.EqualTo(WaveType.BOSS));
            Assert.That(wave.BossTimeLimitSeconds, Is.EqualTo(30f));
            Assert.That(boss.GetComponent<BattleMonsterMovement>().Lane, Is.EqualTo(LaneType.BossSharedLane));
            Assert.That(boss.GetComponent<BattleMonsterMovement>().Speed, Is.EqualTo(2f));
            Assert.That(boss.GetComponent<MonsterStat>().MaxHp, Is.EqualTo(570f));
            Assert.That(_executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
        }

        [Test]
        public void RoundTenBossRoutine_AppliesJsonTimeoutAndActivatesOneBoss()
        {
            SetField("_currentRound", 9);
            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
            IEnumerator routine = Invoke<IEnumerator>("SpawnBossRoutine");

            Assert.That(routine.MoveNext(), Is.True);
            GameObject boss = GetField<GameObject>("_currentBossInstance");
            _spawnedObjects.Add(boss);

            Assert.That(boss, Is.Not.Null);
            Assert.That(_executor.IsBossActive, Is.True);
            Assert.That(GetField<float>("_activeBossTimeLimitSeconds"), Is.EqualTo(30f));
            Assert.That(boss.GetComponent<BattleMonsterMovement>().Lane, Is.EqualTo(LaneType.BossSharedLane));
            Assert.That(_executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(_executor.Player2AliveMonsterCount, Is.Zero);
        }

        [Test]
        public void InvalidProvider_ReportsAllErrorsOnceAndFaultsExecutor()
        {
            Configure(new InvalidBalanceProvider("validation one", "validation two"));
            InitializeRuntimeSession();
            LogAssert.Expect(LogType.Error, new Regex("validation one[\\s\\S]*validation two"));

            Assert.That(Invoke<bool>("EnsureBalanceInitialized"), Is.False);
            Assert.That(Invoke<bool>("EnsureBalanceInitialized"), Is.False);
            Assert.That(GetField<bool>("_isFaulted"), Is.True);
            Assert.That(GetField("_activeWaveCoroutine"), Is.Null);
        }

        [Test]
        public void UnresolvedPrefabKey_FaultsAndClearsWaveState()
        {
            Configure(
                new ResourcesBattleBalanceProvider(_monsterDefinitions, EmptyBattleAlienIdProvider.Instance),
                _monsterDefinitions,
                new RejectingPrefabResolver());
            InitializeRuntimeSession();
            BeginActualRoundOne();
            LogAssert.Expect(LogType.Error, new Regex("Cannot resolve prefabKey"));

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            AssertFaultedAndStopped();
        }

        [Test]
        public void UnknownMonsterIdAtExecution_FaultsAndClearsWaveState()
        {
            Configure(
                new ResourcesBattleBalanceProvider(_monsterDefinitions, EmptyBattleAlienIdProvider.Instance),
                new RejectingMonsterProvider(),
                _prefabResolver);
            InitializeRuntimeSession();
            BeginActualRoundOne();
            LogAssert.Expect(LogType.Error, new Regex("unknown monsterId 'MONSTER_NORMAL_DEFAULT'"));

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            AssertFaultedAndStopped();
        }

        [Test]
        public void PartialSpawnThenResolverFailure_ClearsCoroutineAndWaveState()
        {
            Dictionary<string, string> documents = BattleBalanceTestFixture.CreateDocuments();
            documents[BattleBalanceResourcePaths.WaveSpawnSpec] = documents[BattleBalanceResourcePaths.WaveSpawnSpec]
                .Replace(
                    "\"spawnOrder\":2,\"lanePolicy\":\"EACH_ACTIVE_PLAYER_LANE\",\"monsterId\":\"MON_NORMAL\"",
                    "\"spawnOrder\":2,\"lanePolicy\":\"EACH_ACTIVE_PLAYER_LANE\",\"monsterId\":\"MON_MISSING_PREFAB\"");
            IMonsterDefinitionProvider monsters = BattleBalanceTestFixture.MonsterProvider(
                new BattleMonsterDefinition("MON_NORMAL", "NORMAL", 30f, 5f, "Monster", true),
                new BattleMonsterDefinition("MON_MISSING_PREFAB", "NORMAL", 30f, 5f, "Missing", true),
                new BattleMonsterDefinition("MON_BOSS", "BOSS", 30f, 2f, "Monster", false));
            BattleBalanceProvider provider = BattleBalanceTestFixture.Load(
                documents,
                monsters,
                BattleBalanceTestFixture.Aliens());
            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            Configure(
                provider,
                monsters,
                new ExplicitBattleMonsterPrefabResolver("Monster", _monsterPrefab));
            InitializeRuntimeSession();
            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
            LogAssert.Expect(LogType.Error, new Regex("Cannot resolve prefabKey 'Missing'"));

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            Assert.That(_executor.Player1AliveMonsterCount, Is.EqualTo(3));
            Assert.That(_executor.Player2AliveMonsterCount, Is.EqualTo(3));
            AssertFaultedAndStopped();
        }

        [Test]
        public void PrefabWithoutMovement_FaultsAndClearsWaveState()
        {
            GameObject invalid = new GameObject("NoMovementPrefab");
            invalid.AddComponent<MonsterStat>();
            _spawnedObjects.Add(invalid);
            Configure(
                new ResourcesBattleBalanceProvider(_monsterDefinitions, EmptyBattleAlienIdProvider.Instance),
                _monsterDefinitions,
                new ExplicitBattleMonsterPrefabResolver("Monster", invalid));
            InitializeRuntimeSession();
            BeginActualRoundOne();
            LogAssert.Expect(LogType.Error, new Regex("exactly one BattleMonsterMovement"));

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            AssertFaultedAndStopped();
        }

        [Test]
        public void PrefabWithoutMonsterStat_FaultsAndClearsWaveState()
        {
            GameObject invalid = new GameObject("NoMonsterStatPrefab");
            invalid.AddComponent<BattleMonsterMovement>();
            _spawnedObjects.Add(invalid);
            Configure(
                new ResourcesBattleBalanceProvider(_monsterDefinitions, EmptyBattleAlienIdProvider.Instance),
                _monsterDefinitions,
                new ExplicitBattleMonsterPrefabResolver("Monster", invalid));
            InitializeRuntimeSession();
            BeginActualRoundOne();
            LogAssert.Expect(LogType.Error, new Regex("must contain MonsterStat"));

            RunToCompletion(Invoke<IEnumerator>("SpawnRegularWaveRoutine"));

            AssertFaultedAndStopped();
        }

        private void BeginActualRoundOne()
        {
            Assert.That(Invoke<bool>("TryBeginNextWave"), Is.True);
            Assert.That(_executor.CurrentRound, Is.EqualTo(1));
        }

        private void Configure(
            IBattleBalanceProvider provider,
            IMonsterDefinitionProvider monsters = null,
            IBattleMonsterPrefabResolver resolver = null)
        {
            Invoke(
                "ConfigureBalanceDependenciesForTests",
                provider,
                monsters ?? _monsterDefinitions,
                resolver ?? _prefabResolver);
        }

        private void InitializeRuntimeSession()
        {
            _sessionNumber++;
            _executor.InitializeSession(
                new BattleSessionContext(
                    "executor-fixture-session-" + _sessionNumber,
                    "fixture-canonical-v1",
                    "fixture-canonical-hash",
                    "fixture-battle-v1",
                    "fixture-battle-hash",
                    _sessionNumber),
                new BattlePlayerIdentityMap("fixture-player-alpha", "fixture-player-beta"));
        }

        private void AssertFaultedAndStopped()
        {
            Assert.That(GetField<bool>("_isFaulted"), Is.True);
            Assert.That(_executor.IsWaveRunning, Is.False);
            Assert.That(GetField("_activeWaveCoroutine"), Is.Null);
            Assert.That(GetField("_bossTimerCoroutine"), Is.Null);
        }

        private void CaptureSpawnedObjects()
        {
            foreach (MonsterStat stat in UnityEngine.Object.FindObjectsByType<MonsterStat>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (stat.gameObject.scene.IsValid()
                    && stat.gameObject != _monsterPrefab
                    && !_spawnedObjects.Contains(stat.gameObject))
                    _spawnedObjects.Add(stat.gameObject);
            }
        }

        private static void RunToCompletion(IEnumerator routine)
        {
            int guard = 0;
            while (routine.MoveNext())
            {
                guard++;
                Assert.That(guard, Is.LessThan(1000), "Coroutine did not terminate.");
            }
        }

        private static float ReadWaitSeconds(WaitForSeconds wait)
        {
            FieldInfo field = typeof(WaitForSeconds).GetField("m_Seconds", PrivateInstance);
            Assert.That(field, Is.Not.Null);
            return (float)field.GetValue(wait);
        }

        private object GetField(string fieldName)
        {
            FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
            return field.GetValue(_executor);
        }

        private T GetField<T>(string fieldName)
        {
            return (T)GetField(fieldName);
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
            field.SetValue(_executor, value);
        }

        private void Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = typeof(BattleWaveExecutor).GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, "Missing private method: " + methodName);
            method.Invoke(_executor, arguments);
        }

        private T Invoke<T>(string methodName, params object[] arguments)
        {
            return InvokeWithArguments<T>(methodName, arguments);
        }

        private T InvokeWithArguments<T>(string methodName, object[] arguments)
        {
            MethodInfo method = typeof(BattleWaveExecutor).GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, "Missing private method: " + methodName);
            return (T)method.Invoke(_executor, arguments);
        }

        private sealed class RejectingPrefabResolver : IBattleMonsterPrefabResolver
        {
            public bool TryResolve(string prefabKey, out GameObject prefab)
            {
                prefab = null;
                return false;
            }
        }

        private sealed class RejectingMonsterProvider : IMonsterDefinitionProvider
        {
            public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
            {
                definition = null;
                return false;
            }
        }

        private sealed class InvalidBalanceProvider : IBattleBalanceProvider
        {
            public int SchemaVersion => 1;
            public string BalanceVersion => "INVALID";
            public string ContentHash => string.Empty;
            public BattleBalanceCatalog Catalog => null;
            public bool IsValid => false;
            public IReadOnlyList<string> ValidationErrors { get; }

            public InvalidBalanceProvider(params string[] errors)
            {
                ValidationErrors = Array.AsReadOnly(errors);
            }
        }
    }
}
