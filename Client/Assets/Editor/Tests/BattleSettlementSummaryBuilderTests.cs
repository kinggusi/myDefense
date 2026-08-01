using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSettlementSummaryBuilderTests
    {
        [Test]
        public void Build_MapsBattleSummaryToSpringSettlementContract()
        {
            BattleSettlementSummary settlement = BattleSettlementSummaryBuilder.Build(
                CreateSummary(),
                "request-1",
                new DateTime(2026, 7, 27, 12, 0, 0),
                new DateTime(2026, 7, 27, 12, 5, 0),
                "summary-hash");

            Assert.That(settlement.result, Is.EqualTo(BattleSettlementResultValues.Victory));
            Assert.That(settlement.players.Select(player => player.playerSlot), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(settlement.players[0].initialInGameGold, Is.EqualTo(100));
            Assert.That(settlement.players[0].finalInGameGold, Is.EqualTo(125));
            Assert.That(settlement.players[0].abandoned, Is.False);
            Assert.That(settlement.mapId, Is.EqualTo("EARTH"));
            Assert.That(settlement.players[1].supportKills, Is.EqualTo(1));
            Assert.That(settlement.players[1].bossKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single(item => item.monsterSpecId == "NORMAL_MONSTER").totalKillGold, Is.EqualTo(40));
            Assert.That(settlement.monsterKills.Single(item => item.monsterSpecId == "WAVE_BOSS").bossKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single(item => item.monsterSpecId == "WAVE_BOSS").totalKillGold, Is.EqualTo(200));
        }

        [Test]
        public void Build_MapsFailedMatchAndPreservesNullableEliminatedWave()
        {
            BattleSummary summary = CreateSummary(MatchState.FAILED);
            BattleSettlementSummary settlement = BattleSettlementSummaryBuilder.Build(
                summary,
                "request-2",
                new DateTime(2026, 7, 27, 12, 0, 0),
                new DateTime(2026, 7, 27, 12, 5, 0),
                "hash-2");

            Assert.That(settlement.result, Is.EqualTo(BattleSettlementResultValues.Defeat));
            Assert.That(settlement.players.Single(player => player.playerSlot == 1).eliminatedWave, Is.Null);
            Assert.That(settlement.players.Single(player => player.playerSlot == 2).eliminatedWave, Is.EqualTo(9));
            StringAssert.Contains("\"eliminatedWave\":null", BattleSettlementSummaryJson.Serialize(settlement));
            StringAssert.Contains("\"mapId\":\"EARTH\"", BattleSettlementSummaryJson.Serialize(settlement));
        }

        [Test]
        public void Build_RejectsNonTerminalMatchAndInvalidPlayerSlots()
        {
            Assert.Throws<ArgumentException>(() => BattleSettlementSummaryBuilder.Build(
                CreateSummary(MatchState.RUNNING), "request", DateTime.UnixEpoch, DateTime.UnixEpoch, "hash"));

            BattleSummary invalid = BattleSummaryBuilder.Build(
                new BattleSessionContext("s", "v", "h", "bv", "bh", 100),
                MatchState.CLEARED,
                1,
                new[]
                {
                    new BattlePlayerSummarySeed("p1", 1, false, null, 100, 0, 0, 100),
                    new BattlePlayerSummarySeed("p2", 3, false, null, 100, 0, 0, 100)
                },
                Array.Empty<BattleKillAuditRecord>());

            Assert.Throws<ArgumentException>(() => BattleSettlementSummaryBuilder.Build(
                invalid, "request", DateTime.UnixEpoch, DateTime.UnixEpoch, "hash"));
        }

        [Test]
        public void Build_RejectsUnbalancedGoldAtSummarySeedBoundary()
        {
            Assert.Throws<ArgumentException>(() => new BattlePlayerSummarySeed(
                "p1", 1, false, null, 100, 10, 2, 107));
        }

        [Test]
        public void Build_PreservesAbandonedEligibilityFlagWithoutChangingGoldLedger()
        {
            var session = new BattleSessionContext("abandoned-session", "balance-v1", "content-v1", "battle-v1", "battle-hash", 100, "EARTH");
            BattleSummary summary = BattleSummaryBuilder.Build(
                session,
                MatchState.FAILED,
                10,
                new[]
                {
                    new BattlePlayerSummarySeed("player-a", 1, false, null, 100, 20, 5, 115, true),
                    new BattlePlayerSummarySeed("player-b", 2, false, null, 100, 0, 0, 100)
                },
                Array.Empty<BattleKillAuditRecord>());

            BattleSettlementSummary settlement = BattleSettlementSummaryBuilder.Build(
                summary,
                "abandoned-request",
                DateTime.UnixEpoch,
                DateTime.UnixEpoch,
                "hash");

            Assert.That(settlement.players.Single(player => player.playerSlot == 1).abandoned, Is.True);
            StringAssert.Contains("\"abandoned\":true", BattleSettlementSummaryJson.Serialize(settlement));
        }

        [Test]
        public void Summary_CountsSupportAndCanonicalKillGoldWithoutAwardingAgain()
        {
            BattleSummary summary = CreateSummary();

            Assert.That(summary.Players.Sum(player => player.SupportKills), Is.EqualTo(1));
            Assert.That(summary.Kills.ByLanePolicy.Sum(item => item.AwardedGold), Is.EqualTo(240));
            Assert.That(summary.Kills.ByMonster.Sum(item => item.TotalKillGold), Is.EqualTo(240));
        }

        private static BattleSummary CreateSummary(MatchState result = MatchState.CLEARED)
        {
            var session = new BattleSessionContext("settlement-session", "balance-v1", "content-v1", "battle-v1", "battle-hash", 100, "EARTH");
            var seeds = new[]
            {
                new BattlePlayerSummarySeed("player-a", 1, false, null, 100, 50, 25, 125),
                new BattlePlayerSummarySeed("player-b", 2, true, 9, 100, 0, 0, 100)
            };
            var records = new List<BattleKillAuditRecord>
            {
                new BattleKillAuditRecord(
                    new BattleRuntimeMonsterKey("settlement-session", 1),
                    "NORMAL_MONSTER", "player-a", "player-a", BattleMonsterLanePolicy.EACH_FIELD,
                    1, 10, "player-b", 20),
                new BattleKillAuditRecord(
                    new BattleRuntimeMonsterKey("settlement-session", 2),
                    "NORMAL_MONSTER", "player-a", "player-a", BattleMonsterLanePolicy.EACH_FIELD,
                    1, 11, killGold: 20),
                new BattleKillAuditRecord(
                    new BattleRuntimeMonsterKey("settlement-session", 3),
                    "WAVE_BOSS", "player-b", null, BattleMonsterLanePolicy.BOSS_SHARED,
                    10, 12, killGold: 200)
            };
            return BattleSummaryBuilder.Build(session, result, 10, seeds, records);
        }
    }
}
