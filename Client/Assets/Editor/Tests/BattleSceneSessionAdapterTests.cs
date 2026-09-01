using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSceneSessionAdapterTests
    {
        private GameObject _executorObject;
        private GameObject _adapterObject;
        private BattleWaveExecutor _executor;
        private BattleSceneSessionAdapter _adapter;
        private PlanetContentTestFactory _planetContent;

        [SetUp]
        public void SetUp()
        {
            _executorObject = new GameObject("BattleWaveExecutor_SessionAdapterTest");
            _executor = _executorObject.AddComponent<BattleWaveExecutor>();
            _adapterObject = new GameObject("BattleSceneSessionAdapter_Test");
            _adapter = _adapterObject.AddComponent<BattleSceneSessionAdapter>();
            _planetContent = new PlanetContentTestFactory();
            PlanetContentApplicator applicator = _adapterObject.AddComponent<PlanetContentApplicator>();
            applicator.ConfigureForTests(_planetContent.Catalog);
            var field = typeof(BattleSceneSessionAdapter).GetField("_waveExecutor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(_adapter, _executor);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_adapterObject);
            Object.DestroyImmediate(_executorObject);
            _planetContent.Dispose();
        }

        [Test]
        public void Initialize_AppliesSessionIdentityAndLocalLane()
        {
            var session = new BattleSessionContext("session-1", "balance-1", "hash-1", "battle-1", "battle-hash-1", 10, "NEPTUNE");
            var identities = new BattlePlayerIdentityMap("p1", "p2");

            Assert.That(_adapter.Initialize(session, identities, LaneType.Player2Lane), Is.True);
            Assert.That(_adapter.IsInitialized, Is.True);
            Assert.That(_adapter.SessionContext, Is.SameAs(session));
            Assert.That(_executor.LocalPlayerLane, Is.EqualTo(LaneType.Player2Lane));
            Assert.That(_executor.RuntimeSession, Is.SameAs(session));
        }

        [Test]
        public void Initialize_RejectsSharedLane()
        {
            var session = new BattleSessionContext("session-1", "balance-1", "hash-1", "battle-1", "battle-hash-1", 10, "NEPTUNE");
            var identities = new BattlePlayerIdentityMap("p1", "p2");

            Assert.That(_adapter.Initialize(session, identities, LaneType.BossSharedLane), Is.False);
            Assert.That(_adapter.IsInitialized, Is.False);
        }

        [Test]
        public void ResetAdapter_ClearsBindingState()
        {
            var session = new BattleSessionContext("session-1", "balance-1", "hash-1", "battle-1", "battle-hash-1", 10, "NEPTUNE");
            _adapter.Initialize(session, new BattlePlayerIdentityMap("p1", "p2"), LaneType.Player1Lane);
            Assert.That(_adapter.PlanetContentApplicator.ActiveEnvironment, Is.Not.Null);

            _adapter.ResetAdapter();

            Assert.That(_adapter.IsInitialized, Is.False);
            Assert.That(_adapter.SessionContext, Is.Null);
            Assert.That(_adapter.PlanetContentApplicator.ActiveEnvironment, Is.Null);
            Assert.That(_adapter.PlanetContentApplicator.ActiveMapId, Is.Null);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [TestCase(false)]
        [TestCase(true)]
        public void P1ValidationSession_DoesNotCreateAndDisablesExistingSettlementCoordinator(
            bool addExistingCoordinator)
        {
            BattleRunnerLifecycle lifecycle = _adapterObject.AddComponent<BattleRunnerLifecycle>();
            BattleWaveStateAuthority stateAuthority = _adapterObject.AddComponent<BattleWaveStateAuthority>();
            SetAdapterField("_runnerLifecycle", lifecycle);
            SetAdapterField("_stateAuthority", stateAuthority);
            const string sessionName = "P1VAL-EARTH-W009-0123456789ab";
            Assert.That(lifecycle.TryPrepareP1ValidationSession(sessionName, true, out string reason), Is.True, reason);

            BattleSettlementCoordinator existing = addExistingCoordinator
                ? _adapterObject.AddComponent<BattleSettlementCoordinator>()
                : null;
            if (existing != null)
                Assert.That(existing.enabled, Is.True);
            var session = new BattleSessionContext(
                sessionName, "balance", "content", "battle", "battle-hash", 1, "EARTH");

            Assert.That(_adapter.Initialize(
                session,
                new BattlePlayerIdentityMap("p1", "p2"),
                LaneType.Player2Lane), Is.True);

            Assert.That(_adapterObject.GetComponents<BattleSettlementCoordinator>(),
                Has.Length.EqualTo(addExistingCoordinator ? 1 : 0));
            if (existing != null)
            {
                Assert.That(existing.enabled, Is.False);
                Assert.That(existing.IsConfigured, Is.False);
            }
        }

        [Test]
        public void NormalSession_StillCreatesAndConfiguresSettlementCoordinator()
        {
            BattleRunnerLifecycle lifecycle = _adapterObject.AddComponent<BattleRunnerLifecycle>();
            BattleWaveStateAuthority stateAuthority = _adapterObject.AddComponent<BattleWaveStateAuthority>();
            SetAdapterField("_runnerLifecycle", lifecycle);
            SetAdapterField("_stateAuthority", stateAuthority);
            var session = new BattleSessionContext(
                "normal-session", "balance", "content", "battle", "battle-hash", 1, "NEPTUNE");

            Assert.That(_adapter.Initialize(
                session,
                new BattlePlayerIdentityMap("p1", "p2"),
                LaneType.Player1Lane), Is.True);

            BattleSettlementCoordinator coordinator = _adapterObject.GetComponent<BattleSettlementCoordinator>();
            Assert.That(coordinator, Is.Not.Null);
            Assert.That(coordinator.enabled, Is.True);
            Assert.That(coordinator.IsConfigured, Is.True);
        }

        [Test]
        public void NormalSession_ReenablesPreviouslySuppressedSettlementCoordinator()
        {
            BattleRunnerLifecycle lifecycle = _adapterObject.AddComponent<BattleRunnerLifecycle>();
            BattleWaveStateAuthority stateAuthority = _adapterObject.AddComponent<BattleWaveStateAuthority>();
            BattleSettlementCoordinator coordinator = _adapterObject.AddComponent<BattleSettlementCoordinator>();
            coordinator.enabled = false;
            SetAdapterField("_runnerLifecycle", lifecycle);
            SetAdapterField("_stateAuthority", stateAuthority);
            var session = new BattleSessionContext(
                "normal-session-after-validation", "balance", "content", "battle", "battle-hash", 1, "NEPTUNE");

            Assert.That(_adapter.Initialize(
                session,
                new BattlePlayerIdentityMap("p1", "p2"),
                LaneType.Player1Lane), Is.True);

            Assert.That(coordinator.enabled, Is.True);
            Assert.That(coordinator.IsConfigured, Is.True);
        }

        [Test]
        public void P1ValidationInitializationFailure_StillDisablesExistingSettlementCoordinator()
        {
            BattleRunnerLifecycle lifecycle = _adapterObject.AddComponent<BattleRunnerLifecycle>();
            BattleWaveStateAuthority stateAuthority = _adapterObject.AddComponent<BattleWaveStateAuthority>();
            BattleSettlementCoordinator coordinator = _adapterObject.AddComponent<BattleSettlementCoordinator>();
            SetAdapterField("_runnerLifecycle", lifecycle);
            SetAdapterField("_stateAuthority", stateAuthority);
            const string sessionName = "P1VAL-EARTH-W009-0123456789ab";
            Assert.That(lifecycle.TryPrepareP1ValidationSession(sessionName, true, out string reason), Is.True, reason);
            var mismatchedSession = new BattleSessionContext(
                sessionName, "balance", "content", "battle", "battle-hash", 1, "SUN");

            LogAssert.Expect(LogType.Error, "[P1Validation] Session context does not match the bound validation profile.");
            Assert.That(_adapter.Initialize(
                mismatchedSession,
                new BattlePlayerIdentityMap("p1", "p2"),
                LaneType.Player1Lane), Is.False);

            Assert.That(coordinator.enabled, Is.False);
            Assert.That(coordinator.IsConfigured, Is.False);
        }

        private void SetAdapterField(string name, object value)
        {
            var field = typeof(BattleSceneSessionAdapter).GetField(
                name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_adapter, value);
        }
#endif

    }
}
