using System;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSessionRosterRegistrationTests
    {
        [Test]
        public void RequestMatchesSpringTrustedRosterContract()
        {
            var session = new BattleSessionContext(
                "fusion-session",
                "balance-v1",
                "content-hash",
                "battle-v1",
                "battle-hash",
                10,
                "EARTH");

            var request = BattleSessionRosterRegistrar.BuildRequest(session, "player-one", "player-two");

            Assert.That(request.battleSessionId, Is.EqualTo("fusion-session"));
            Assert.That(request.mapId, Is.EqualTo("EARTH"));
            Assert.That(request.balanceVersion, Is.EqualTo("balance-v1"));
            Assert.That(request.contentHash, Is.EqualTo("content-hash"));
            Assert.That(request.players, Has.Length.EqualTo(2));
            Assert.That(request.players[0].playerSlot, Is.EqualTo(1));
            Assert.That(request.players[0].playerId, Is.EqualTo("player-one"));
            Assert.That(request.players[1].playerSlot, Is.EqualTo(2));
            Assert.That(request.players[1].playerId, Is.EqualTo("player-two"));
        }

        [Test]
        public void DefaultMapAndDistinctPlayerInvariantAreEnforced()
        {
            var session = new BattleSessionContext(
                "fusion-session", "balance-v1", "content-hash", "battle-v1", "battle-hash", 10);

            var request = BattleSessionRosterRegistrar.BuildRequest(session, "one", "two");
            Assert.That(request.mapId, Is.EqualTo(BattleRunnerLifecycle.DefaultMapId));
            Assert.Throws<ArgumentException>(() =>
                BattleSessionRosterRegistrar.BuildRequest(session, "same", "same"));
        }

        [TestCase("local", true)]
        [TestCase("dev", true)]
        [TestCase("production", false)]
        [TestCase("prod", false)]
        public void DevelopmentRegistrarFailsClosedOutsideLocalProfiles(string environment, bool expected)
        {
            Assert.That(BattleSessionRosterRegistrar.IsLocalOrDev(environment), Is.EqualTo(expected));
        }

        [Test]
        public void FactoryUsesLocalRegistrarOnlyForLocalEnvironment()
        {
            var localHost = new GameObject("LocalRosterHost");
            var productionHost = new GameObject("ProductionRosterHost");
            try
            {
                Assert.That(
                    BattleSessionRosterRegistrationFactory.ResolveOrCreate(localHost, "local"),
                    Is.TypeOf<BattleSessionRosterRegistrar>());
                Assert.That(
                    BattleSessionRosterRegistrationFactory.ResolveOrCreate(productionHost, "prod"),
                    Is.TypeOf<MissingAuthenticatedRosterRegistrar>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(localHost);
                UnityEngine.Object.DestroyImmediate(productionHost);
            }
        }
    }
}
