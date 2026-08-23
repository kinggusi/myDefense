using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSceneSessionAdapterTests
    {
        private GameObject _executorObject;
        private GameObject _adapterObject;
        private BattleWaveExecutor _executor;
        private BattleSceneSessionAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _executorObject = new GameObject("BattleWaveExecutor_SessionAdapterTest");
            _executor = _executorObject.AddComponent<BattleWaveExecutor>();
            _adapterObject = new GameObject("BattleSceneSessionAdapter_Test");
            _adapter = _adapterObject.AddComponent<BattleSceneSessionAdapter>();
            var field = typeof(BattleSceneSessionAdapter).GetField("_waveExecutor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(_adapter, _executor);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_adapterObject);
            Object.DestroyImmediate(_executorObject);
        }

        [Test]
        public void Initialize_AppliesSessionIdentityAndLocalLane()
        {
            var session = new BattleSessionContext("session-1", "balance-1", "hash-1", "battle-1", "battle-hash-1", 10);
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
            var session = new BattleSessionContext("session-1", "balance-1", "hash-1", "battle-1", "battle-hash-1", 10);
            var identities = new BattlePlayerIdentityMap("p1", "p2");

            Assert.That(_adapter.Initialize(session, identities, LaneType.BossSharedLane), Is.False);
            Assert.That(_adapter.IsInitialized, Is.False);
        }

        [Test]
        public void ResetAdapter_ClearsBindingState()
        {
            var session = new BattleSessionContext("session-1", "balance-1", "hash-1", "battle-1", "battle-hash-1", 10);
            _adapter.Initialize(session, new BattlePlayerIdentityMap("p1", "p2"), LaneType.Player1Lane);

            _adapter.ResetAdapter();

            Assert.That(_adapter.IsInitialized, Is.False);
            Assert.That(_adapter.SessionContext, Is.Null);
        }

    }
}
