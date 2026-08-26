using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSettlementCoordinatorTests
    {
        [Test]
        public void CoordinatorExposesExplicitIdempotentRetryBoundary()
        {
            Assert.That(typeof(BattleSettlementCoordinator).GetMethod(nameof(BattleSettlementCoordinator.RetryPendingSettlement), BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
            Assert.That(typeof(BattleSettlementCoordinator).GetProperty(nameof(BattleSettlementCoordinator.HasPendingSettlement)), Is.Not.Null);
            Assert.That(typeof(BattleSettlementResponse).GetField(nameof(BattleSettlementResponse.alreadyProcessed)), Is.Not.Null);
            Assert.That(typeof(BattleSettlementResponse).GetField(nameof(BattleSettlementResponse.rewards)), Is.Not.Null);
            Assert.That(typeof(BattleSettlementReward).GetField(nameof(BattleSettlementReward.rewardKey)), Is.Not.Null);
            Assert.That(typeof(BattleSettlementReward).GetField(nameof(BattleSettlementReward.universalPiece)), Is.Not.Null);
            Assert.That(typeof(BattleSettlementReward).GetField(nameof(BattleSettlementReward.diamond)), Is.Not.Null);
        }

        [Test]
        public void SendPendingSettlement_FailsClosedBeforeNetworkWhenRosterIsUnregistered()
        {
            var hostObject = new GameObject("SettlementRosterGuardTest");
            try
            {
                BattleSettlementCoordinator coordinator = hostObject.AddComponent<BattleSettlementCoordinator>();
                typeof(BattleSettlementCoordinator)
                    .GetField("_rosterRegistration", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(coordinator, new UnregisteredRoster());

                LogAssert.Expect(LogType.Error, "[BattleSettlement] roster not registered");
                typeof(BattleSettlementCoordinator)
                    .GetMethod("SendPendingSettlement", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(coordinator, null);

                Assert.That(coordinator.LastError, Is.EqualTo("roster not registered"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void SendPendingSettlement_FailsClosedBeforeNetworkWhenRosterIsMissing()
        {
            var hostObject = new GameObject("SettlementMissingRosterGuardTest");
            try
            {
                BattleSettlementCoordinator coordinator = hostObject.AddComponent<BattleSettlementCoordinator>();

                LogAssert.Expect(LogType.Error, "[BattleSettlement] Trusted Battle roster is not registered.");
                typeof(BattleSettlementCoordinator)
                    .GetMethod("SendPendingSettlement", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(coordinator, null);

                Assert.That(coordinator.LastError, Is.EqualTo("Trusted Battle roster is not registered."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void BuildRequestCreatesDeterministicHashAndSpringPayload()
        {
            BattleSummary summary = CreateSummary();
            DateTime started = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
            DateTime finished = started.AddMinutes(5);

            BattleSettlementSummary first = BattleSettlementCoordinator.BuildRequest(summary, "request-1", started, finished);
            BattleSettlementSummary second = BattleSettlementCoordinator.BuildRequest(summary, "request-1", started, finished);

            Assert.That(first.summaryHash, Is.Not.Null.And.Length.EqualTo(64));
            Assert.That(first.mapId, Is.EqualTo("EARTH"));
            Assert.That(first.summaryHash, Is.EqualTo(second.summaryHash));
            Assert.That(BattleSettlementSummaryJson.Serialize(first), Is.EqualTo(BattleSettlementSummaryJson.Serialize(second)));
            Assert.That(first.startedAt, Is.EqualTo("2026-07-27T12:00:00"));
            Assert.That(first.finishedAt, Is.EqualTo("2026-07-27T12:05:00"));
        }

        [Test]
        public void BuildRequestUsesDifferentRequestIdentityForRetryBoundary()
        {
            BattleSummary summary = CreateSummary();
            DateTime started = new DateTime(2026, 7, 27, 12, 0, 0);
            DateTime finished = started.AddMinutes(1);

            BattleSettlementSummary first = BattleSettlementCoordinator.BuildRequest(summary, "request-a", started, finished);
            BattleSettlementSummary second = BattleSettlementCoordinator.BuildRequest(summary, "request-b", started, finished);

            Assert.That(first.summaryHash, Is.Not.EqualTo(second.summaryHash));
        }

        [Test]
        public void BuildRequestPreservesVictoryWave80AndRealPlayerIdentity()
        {
            var session = new BattleSessionContext("e2e-session", "balance-v1", "content-v1", "battle-v1", "battle-hash", 1, "NEPTUNE");
            var players = new[]
            {
                new BattlePlayerSummarySeed("account-host", 1, false, null, 100, 20, 0, 120),
                new BattlePlayerSummarySeed("account-client", 2, false, null, 100, 20, 0, 120)
            };
            var kills = new List<BattleKillAuditRecord>
            {
                new(new BattleRuntimeMonsterKey("e2e-session", 1), "NORMAL_MONSTER", "account-host", "account-host", BattleMonsterLanePolicy.EACH_FIELD, 1, 1, killGold: 20),
                new(new BattleRuntimeMonsterKey("e2e-session", 2), "NORMAL_MONSTER", "account-client", "account-client", BattleMonsterLanePolicy.EACH_FIELD, 1, 1, killGold: 20)
            };

            BattleSummary battle = BattleSummaryBuilder.Build(session, MatchState.CLEARED, 80, players, kills);
            BattleSettlementSummary request = BattleSettlementCoordinator.BuildRequest(
                battle,
                "e2e-request",
                new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 2, 12, 20, 0, DateTimeKind.Utc));

            Assert.That(request.result, Is.EqualTo("VICTORY"));
            Assert.That(request.finalWave, Is.EqualTo(80));
            Assert.That(request.mapId, Is.EqualTo("NEPTUNE"));
            Assert.That(request.players.Select(player => player.playerId),
                Is.EqualTo(new[] { "account-host", "account-client" }));
        }

        private static BattleSummary CreateSummary()
        {
            var session = new BattleSessionContext("settlement-coordinator-session", "balance-v1", "content-v1", "battle-v1", "battle-hash", 1, "EARTH");
            var seeds = new[]
            {
                new BattlePlayerSummarySeed("player-a", 1, false, null, 100, 20, 5, 115),
                new BattlePlayerSummarySeed("player-b", 2, false, null, 100, 0, 0, 100)
            };
            var records = new List<BattleKillAuditRecord>
            {
                new BattleKillAuditRecord(
                    new BattleRuntimeMonsterKey("settlement-coordinator-session", 1),
                    "NORMAL_MONSTER", "player-a", "player-a", BattleMonsterLanePolicy.EACH_FIELD,
                    1, 1, killGold: 20)
            };
            return BattleSummaryBuilder.Build(session, MatchState.CLEARED, 1, seeds, records);
        }

        private sealed class UnregisteredRoster : IBattleSessionRosterRegistration
        {
            public bool IsRegistered => false;
            public bool IsRequestInFlight => false;
            public string LastError => "roster not registered";
            public event Action Registered { add { } remove { } }
            public void Configure(BattleSessionContext session, IBattlePlayerIdentityProvider identities) { }
            public void EnsureRegistered() { }
            public bool RetryRegistration() => false;
        }
    }
}
