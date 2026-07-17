using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleRuntimeIdentityTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string MonsterPrefabPath = "Assets/Prefabs/Monsters/Monster.prefab";
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                    UnityEngine.Object.DestroyImmediate(_createdObjects[index]);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void SessionContext_ValidatesRequiredFieldsAndIsImmutable()
        {
            BattleSessionContext session = Session("session-alpha", 17);

            Assert.That(session.BattleSessionId, Is.EqualTo("session-alpha"));
            Assert.That(session.StartedAtTick, Is.EqualTo(17));
            Assert.That(typeof(BattleSessionContext).GetProperty(nameof(BattleSessionContext.BattleSessionId)).CanWrite, Is.False);
            Assert.Throws<ArgumentException>(() => new BattleSessionContext(" ", "c-v1", "c-hash", "b-v1", "b-hash", 0));
        }

        [Test]
        public void RuntimeKeys_AllowSameSequenceInDifferentSessionsWithoutPairCollision()
        {
            var first = new BattleRuntimeMonsterKey("session-alpha", 1);
            var same = new BattleRuntimeMonsterKey("session-alpha", 1);
            var otherSession = new BattleRuntimeMonsterKey("session-beta", 1);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(otherSession));
            Assert.That(new HashSet<BattleRuntimeMonsterKey> { first, same, otherSession }.Count, Is.EqualTo(2));
        }

        [Test]
        public void SpawnSequence_StartsAtOneIsMonotonicAndDoesNotReuseDiscardedValues()
        {
            var issuer = new BattleSpawnSequenceIssuer();

            Assert.That(issuer.IssueNext(), Is.EqualTo(1UL));
            ulong discardedAfterFailedSpawn = issuer.IssueNext();
            Assert.That(discardedAfterFailedSpawn, Is.EqualTo(2UL));
            Assert.That(issuer.IssueNext(), Is.EqualTo(3UL));
        }

        [Test]
        public void PlayerIdentityMap_ResolvesDistinctLaneOwnersAndRejectsDuplicates()
        {
            var identities = new BattlePlayerIdentityMap("player-alpha", "player-beta");

            Assert.That(identities.TryGetPlayerId(LaneType.Player1Lane, out string player1), Is.True);
            Assert.That(identities.TryGetPlayerId(LaneType.Player2Lane, out string player2), Is.True);
            Assert.That(player1, Is.EqualTo("player-alpha"));
            Assert.That(player2, Is.EqualTo("player-beta"));
            Assert.That(identities.TryGetPlayerId(LaneType.BossSharedLane, out _), Is.False);
            Assert.Throws<ArgumentException>(() => new BattlePlayerIdentityMap("same", "same"));
        }

        [Test]
        public void RuntimeIdentity_RequiresEachFieldOwnerAndForbidsBossOwner()
        {
            BattleSessionContext session = Session("session-alpha");

            Assert.Throws<ArgumentException>(() => Identity(session, 1, BattleMonsterLanePolicy.EACH_FIELD, null));
            Assert.Throws<ArgumentException>(() => Identity(session, 1, BattleMonsterLanePolicy.BOSS_SHARED, "player-alpha"));

            BattleMonsterRuntimeIdentity boss = Identity(session, 1, BattleMonsterLanePolicy.BOSS_SHARED, null);
            Assert.That(boss.FieldOwnerPlayerId, Is.Null);
            Assert.That(boss.RuntimeMonsterId, Is.EqualTo(boss.SpawnSequence));
        }

        [Test]
        public void RuntimeContext_InitializesOnceAndRetainsImmutableOwner()
        {
            GameObject gameObject = CreateGameObject("Runtime Context Test");
            BattleMonsterRuntimeContext context = gameObject.AddComponent<BattleMonsterRuntimeContext>();
            BattleMonsterRuntimeIdentity identity = Identity(
                Session("session-alpha"),
                1,
                BattleMonsterLanePolicy.EACH_FIELD,
                "player-alpha");

            context.Initialize(identity);

            Assert.That(context.IsInitialized, Is.True);
            Assert.That(context.Identity.FieldOwnerPlayerId, Is.EqualTo("player-alpha"));
            Assert.That(typeof(BattleMonsterRuntimeIdentity).GetProperty(nameof(BattleMonsterRuntimeIdentity.FieldOwnerPlayerId)).CanWrite, Is.False);
            Assert.Throws<InvalidOperationException>(() => context.Initialize(identity));
        }

        [Test]
        public void MonsterPrefab_HasExactlyOneUninitializedRuntimeContext()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            BattleMonsterRuntimeContext[] contexts = prefab.GetComponents<BattleMonsterRuntimeContext>();
            Assert.That(contexts.Length, Is.EqualTo(1));
            Assert.That(contexts[0].IsInitialized, Is.False);
        }

        [Test]
        public void ExecutorSpawn_AssignsUniqueP1P2AndBossRuntimeContexts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            BattleWaveExecutor executor = CreateExecutor(prefab, "runtime-spawn-session");
            var definition = new BattleMonsterDefinition("MONSTER_FIXTURE", "NORMAL", 100f, 2f, "Monster", true);
            var regularSpawn = new WaveSpawnSpecData(
                "WAVE_001", 1, BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE, definition.MonsterId, 1, 0f, 0f, 1f, 1f);

            GameObject player1 = InvokeSpawn(executor, LaneType.Player1Lane, definition, regularSpawn);
            GameObject player2 = InvokeSpawn(executor, LaneType.Player2Lane, definition, regularSpawn);
            var bossDefinition = new BattleMonsterDefinition("BOSS_FIXTURE", "BOSS", 1000f, 1f, "Monster", false);
            var bossSpawn = new WaveSpawnSpecData(
                "WAVE_010", 1, BattleLanePolicy.BOSS_SHARED, bossDefinition.MonsterId, 1, 0f, 0f, 1f, 1f);
            GameObject boss = InvokeSpawn(executor, LaneType.BossSharedLane, bossDefinition, bossSpawn);

            BattleMonsterRuntimeIdentity p1 = player1.GetComponent<BattleMonsterRuntimeContext>().Identity;
            BattleMonsterRuntimeIdentity p2 = player2.GetComponent<BattleMonsterRuntimeContext>().Identity;
            BattleMonsterRuntimeIdentity sharedBoss = boss.GetComponent<BattleMonsterRuntimeContext>().Identity;
            Assert.That(new[] { p1.RuntimeMonsterId, p2.RuntimeMonsterId, sharedBoss.RuntimeMonsterId }, Is.EqualTo(new ulong[] { 1, 2, 3 }));
            Assert.That(p1.FieldOwnerPlayerId, Is.EqualTo("fixture-player-alpha"));
            Assert.That(p2.FieldOwnerPlayerId, Is.EqualTo("fixture-player-beta"));
            Assert.That(sharedBoss.FieldOwnerPlayerId, Is.Null);
            Assert.That(sharedBoss.LanePolicy, Is.EqualTo(BattleMonsterLanePolicy.BOSS_SHARED));
            Assert.That(boss.GetComponents<BattleMonsterRuntimeContext>().Length, Is.EqualTo(1));
        }

        [Test]
        public void RegularWaveRoutine_AssignsContextToEveryMonsterInDeterministicLaneOrder()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            BattleWaveExecutor executor = CreateExecutor(prefab, "runtime-regular-wave-session");
            SetField(executor, "_currentWaveSpec", new WaveSpecData("WAVE_001", 1, WaveType.REGULAR, 0f, 0f, true));
            SetField(
                executor,
                "_currentWaveSpawns",
                Array.AsReadOnly(new[]
                {
                    new WaveSpawnSpecData(
                        "WAVE_001",
                        1,
                        BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE,
                        "MONSTER_FIXTURE",
                        1,
                        0f,
                        0f,
                        1f,
                        1f)
                }));
            Invoke(executor, "BeginWaveExecution", false);

            MethodInfo routineMethod = typeof(BattleWaveExecutor).GetMethod("SpawnRegularWaveRoutine", PrivateInstance);
            Assert.That(routineMethod, Is.Not.Null);
            IEnumerator routine = (IEnumerator)routineMethod.Invoke(executor, null);
            while (routine.MoveNext()) { }

            BattleMonsterRuntimeContext[] contexts = UnityEngine.Object.FindObjectsByType<BattleMonsterRuntimeContext>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(context => context.IsInitialized
                    && context.Identity.BattleSessionId == "runtime-regular-wave-session")
                .OrderBy(context => context.Identity.RuntimeMonsterId)
                .ToArray();
            foreach (BattleMonsterRuntimeContext context in contexts)
                _createdObjects.Add(context.gameObject);

            Assert.That(contexts.Length, Is.EqualTo(2));
            Assert.That(contexts.Select(context => context.Identity.RuntimeMonsterId), Is.EqualTo(new ulong[] { 1, 2 }));
            Assert.That(contexts[0].Identity.FieldOwnerPlayerId, Is.EqualTo("fixture-player-alpha"));
            Assert.That(contexts[1].Identity.FieldOwnerPlayerId, Is.EqualTo("fixture-player-beta"));
        }

        [Test]
        public void ExecutorSessionReinitialize_RequiresNewIdAndRestartsSequenceAtOne()
        {
            GameObject executorObject = CreateGameObject("Session Reinitialize Test");
            BattleWaveExecutor executor = executorObject.AddComponent<BattleWaveExecutor>();
            var identities = new BattlePlayerIdentityMap("fixture-player-alpha", "fixture-player-beta");
            BattleSessionContext first = Session("session-alpha");
            executor.InitializeSession(first, identities);
            BattleSpawnSequenceIssuer firstIssuer = GetField<BattleSpawnSequenceIssuer>(executor, "_spawnSequenceIssuer");
            Assert.That(firstIssuer.IssueNext(), Is.EqualTo(1UL));

            Assert.Throws<InvalidOperationException>(() => executor.InitializeSession(first, identities));
            executor.InitializeSession(Session("session-beta"), identities);

            BattleSpawnSequenceIssuer secondIssuer = GetField<BattleSpawnSequenceIssuer>(executor, "_spawnSequenceIssuer");
            Assert.That(secondIssuer, Is.Not.SameAs(firstIssuer));
            Assert.That(secondIssuer.IssueNext(), Is.EqualTo(1UL));
        }

        [Test]
        public void KillDeduplicator_UsesSessionAndRuntimeIdPair()
        {
            var deduplicator = new BattleKillDeduplicator();
            BattleKillAuditRecord first = Kill("session-alpha", 1, "MONSTER", BattleMonsterLanePolicy.EACH_FIELD, "player-alpha");
            BattleKillAuditRecord duplicate = Kill("session-alpha", 1, "MONSTER", BattleMonsterLanePolicy.EACH_FIELD, "player-alpha");
            BattleKillAuditRecord otherMonster = Kill("session-alpha", 2, "MONSTER", BattleMonsterLanePolicy.EACH_FIELD, "player-alpha");
            BattleKillAuditRecord otherSession = Kill("session-beta", 1, "MONSTER", BattleMonsterLanePolicy.EACH_FIELD, "player-alpha");
            BattleKillAuditRecord boss = Kill("session-alpha", 3, "BOSS", BattleMonsterLanePolicy.BOSS_SHARED, "player-beta");

            Assert.That(deduplicator.TryRegister(first), Is.True);
            Assert.That(deduplicator.TryRegister(duplicate), Is.False);
            Assert.That(deduplicator.TryRegister(otherMonster), Is.True);
            Assert.That(deduplicator.TryRegister(otherSession), Is.True);
            Assert.That(deduplicator.TryRegister(boss), Is.True);
            Assert.That(deduplicator.TryRegister(boss), Is.False);
            Assert.That(deduplicator.Records.Count, Is.EqualTo(4));
            Assert.That(deduplicator.ProcessedKeys.Count, Is.EqualTo(4));
            Assert.That(deduplicator.Contains(first.RuntimeKey), Is.True);
            Assert.That(deduplicator.ProcessedKeys, Is.Not.InstanceOf<HashSet<BattleRuntimeMonsterKey>>());
        }

        private BattleWaveExecutor CreateExecutor(GameObject prefab, string sessionId)
        {
            GameObject executorObject = CreateGameObject("Runtime Identity Executor");
            BattleWaveExecutor executor = executorObject.AddComponent<BattleWaveExecutor>();
            GameObject spawnPoint = CreateGameObject("Runtime Identity Spawn Point");
            SetField(executor, "_spawnPoint", spawnPoint.transform);
            SetField(executor, "_currentRound", 1);
            Invoke(
                executor,
                "ConfigureBalanceDependenciesForTests",
                new RuntimeTestBalanceProvider(),
                new RuntimeTestMonsterProvider(),
                new RuntimeTestPrefabResolver(prefab));
            executor.InitializeSession(
                Session(sessionId),
                new BattlePlayerIdentityMap("fixture-player-alpha", "fixture-player-beta"));
            SetField(executor, "_currentRound", 1);
            return executor;
        }

        private GameObject InvokeSpawn(
            BattleWaveExecutor executor,
            LaneType lane,
            BattleMonsterDefinition definition,
            WaveSpawnSpecData spawn)
        {
            MethodInfo method = typeof(BattleWaveExecutor).GetMethod("SpawnConfiguredMonster", PrivateInstance);
            Assert.That(method, Is.Not.Null);
            object[] arguments = { lane, definition, spawn, 1f, null };
            Assert.That((bool)method.Invoke(executor, arguments), Is.True);
            var spawned = (GameObject)arguments[4];
            Assert.That(spawned, Is.Not.Null);
            _createdObjects.Add(spawned);
            return spawned;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static BattleSessionContext Session(string id, long tick = 0)
        {
            return new BattleSessionContext(id, "canonical-v1", "canonical-hash", "battle-v1", "battle-hash", tick);
        }

        private static BattleMonsterRuntimeIdentity Identity(
            BattleSessionContext session,
            ulong id,
            BattleMonsterLanePolicy policy,
            string owner)
        {
            return new BattleMonsterRuntimeIdentity(session, id, "MONSTER", policy, owner, 1, id);
        }

        private static BattleKillAuditRecord Kill(
            string sessionId,
            ulong runtimeId,
            string monsterId,
            BattleMonsterLanePolicy lanePolicy,
            string killer)
        {
            string owner = lanePolicy == BattleMonsterLanePolicy.EACH_FIELD ? "field-owner" : null;
            return new BattleKillAuditRecord(
                new BattleRuntimeMonsterKey(sessionId, runtimeId),
                monsterId,
                killer,
                owner,
                lanePolicy,
                1,
                10);
        }

        private static void SetField(BattleWaveExecutor executor, string fieldName, object value)
        {
            FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(executor, value);
        }

        private static T GetField<T>(BattleWaveExecutor executor, string fieldName)
        {
            FieldInfo field = typeof(BattleWaveExecutor).GetField(fieldName, PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(executor);
        }

        private static void Invoke(BattleWaveExecutor executor, string methodName, params object[] arguments)
        {
            MethodInfo method = typeof(BattleWaveExecutor).GetMethod(methodName, PrivateInstance);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(executor, arguments);
        }

        private sealed class RuntimeTestMonsterProvider : IMonsterDefinitionProvider
        {
            public bool TryGet(string monsterId, out BattleMonsterDefinition definition)
            {
                definition = new BattleMonsterDefinition(monsterId, "NORMAL", 100f, 2f, "Monster", true);
                return true;
            }
        }

        private sealed class RuntimeTestPrefabResolver : IBattleMonsterPrefabResolver
        {
            private readonly GameObject _prefab;
            public RuntimeTestPrefabResolver(GameObject prefab) => _prefab = prefab;
            public bool TryResolve(string prefabKey, out GameObject prefab)
            {
                prefab = _prefab;
                return prefab != null;
            }
        }

        private sealed class RuntimeTestBalanceProvider : IBattleBalanceProvider
        {
            public int SchemaVersion => 1;
            public string BalanceVersion => "fixture";
            public string ContentHash => "fixture";
            public BattleBalanceCatalog Catalog => null;
            public bool IsValid => true;
            public IReadOnlyList<string> ValidationErrors => Array.AsReadOnly(Array.Empty<string>());
        }
    }
}
