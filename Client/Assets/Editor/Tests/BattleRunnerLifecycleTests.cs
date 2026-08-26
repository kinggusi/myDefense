using System;
using MyDefense.Battle.Runtime;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleRunnerLifecycleTests
    {
        [Test]
        public void LifecycleStartsStopped()
        {
            var gameObject = new UnityEngine.GameObject("runner-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                Assert.That(lifecycle.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(lifecycle.Runner, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EmptySerializedMapUsesCanonicalFirstPlanet()
        {
            var gameObject = new UnityEngine.GameObject("runner-map-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                Assert.That(lifecycle.MapId, Is.EqualTo(BattleRunnerLifecycle.DefaultMapId));
                Assert.That(lifecycle.MapId, Is.EqualTo("NEPTUNE"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EmptySessionNameIsRejectedBeforeRunnerCreation()
        {
            var gameObject = new UnityEngine.GameObject("runner-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                Assert.ThrowsAsync<ArgumentException>(() => lifecycle.StartHostAsync(" "));
                Assert.That(lifecycle.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(lifecycle.Runner, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [TestCase("MyDefense-Dev", BattleP1ValidationParseState.NotValidation)]
        [TestCase("P1VAL-SUN-W080-0123456789ab", BattleP1ValidationParseState.Valid)]
        [TestCase("P1VAL-NEPTUNE-W001-ABCDEF0123456789", BattleP1ValidationParseState.Valid)]
        [TestCase("p1val-SUN-W080-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-PLUTO-W080-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-SUN-W000-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-SUN-W081-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-SUN-W80-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-SUN-W+01-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase(" P1VAL-SUN-W001-0123456789ab", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-SUN-W080-0123456789a", BattleP1ValidationParseState.Malformed)]
        [TestCase("P1VAL-SUN-W080-0123456789ag", BattleP1ValidationParseState.Malformed)]
        public void P1ValidationProfile_UsesThreeStateFailClosedParser(
            string sessionName,
            BattleP1ValidationParseState expected)
        {
            BattleP1ValidationParseState actual = BattleP1ValidationSessionProfile.Parse(
                sessionName,
                out BattleP1ValidationSessionProfile profile,
                out string reason);

            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(profile, expected == BattleP1ValidationParseState.Valid ? Is.Not.Null : Is.Null);
            Assert.That(reason, expected == BattleP1ValidationParseState.Malformed ? Is.Not.Empty : Is.Empty);
        }

        [Test]
        public void P1ValidationProfile_AcceptsExactlyTheNineCanonicalPlanetIds()
        {
            string[] mapIds =
            {
                "NEPTUNE", "URANUS", "SATURN", "JUPITER", "MARS",
                "EARTH", "VENUS", "MERCURY", "SUN"
            };

            foreach (string mapId in mapIds)
            {
                string sessionName = $"P1VAL-{mapId}-W010-0123456789ab";
                Assert.That(BattleP1ValidationSessionProfile.Parse(
                    sessionName,
                    out BattleP1ValidationSessionProfile profile,
                    out string reason), Is.EqualTo(BattleP1ValidationParseState.Valid), reason);
                Assert.That(profile.MapId, Is.EqualTo(mapId));
                Assert.That(profile.InitialWave, Is.EqualTo(10));
            }
        }

        [Test]
        public void P1ValidationProfile_BindsOnceBeforeRunnerAndOverridesMapImmutably()
        {
            var gameObject = new UnityEngine.GameObject("runner-p1-validation-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                const string sessionName = "P1VAL-SUN-W080-0123456789ab";

                Assert.That(lifecycle.TryPrepareP1ValidationSession(sessionName, true, out string reason), Is.True, reason);
                Assert.That(lifecycle.P1ValidationProfile, Is.Not.Null);
                Assert.That(lifecycle.P1ValidationProfile.InitialWave, Is.EqualTo(80));
                Assert.That(lifecycle.MapId, Is.EqualTo("SUN"));
                Assert.That(lifecycle.TryPrepareP1ValidationSession(sessionName, true, out reason), Is.False);
                Assert.That(reason, Does.Contain("exactly once"));
                Assert.Throws<InvalidOperationException>(() => lifecycle.CreateSessionContext(
                    "balance", "content", "battle", "battle-hash", 0, "EARTH"));
                Assert.That(lifecycle.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(lifecycle.Runner, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MalformedOrAutoRoleP1ValidationSession_IsRejectedBeforeRunnerCreation()
        {
            var malformedObject = new UnityEngine.GameObject("runner-p1-malformed-test");
            var autoObject = new UnityEngine.GameObject("runner-p1-auto-test");
            try
            {
                var malformed = malformedObject.AddComponent<BattleRunnerLifecycle>();
                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    malformed.StartHostAsync("P1VAL-SUN-W080-not-hex"));
                Assert.That(malformed.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(malformed.Runner, Is.Null);

                var auto = autoObject.AddComponent<BattleRunnerLifecycle>();
                Assert.ThrowsAsync<InvalidOperationException>(() =>
                    auto.StartHostOrClientAsync("P1VAL-SUN-W080-0123456789ab", "p1"));
                Assert.That(auto.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(auto.Runner, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(malformedObject);
                UnityEngine.Object.DestroyImmediate(autoObject);
            }
        }
#endif
    }
}
