using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleQuestSettlementProjectionTests
    {
        private const string SessionId = "quest-settlement-session";

        [Test]
        public void FinalDto_DeduplicatesDuplicateRuntimeMonsterId()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.CLEARED,
                1,
                new[]
                {
                    Kill(1, "MONSTER_A", "player-a", spawnWave: 1),
                    Kill(1, "MONSTER_A", "player-a", spawnWave: 1)
                });

            Assert.That(settlement.players.Single(player => player.playerId == "player-a").kills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single().totalKills, Is.EqualTo(1));
        }

        [Test]
        public void FinalDto_AggregatesSameSpecAcrossDistinctRuntimeMonsterIds()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.CLEARED,
                1,
                new[]
                {
                    Kill(1, "MONSTER_A", "player-a", spawnWave: 1),
                    Kill(2, "MONSTER_A", "player-b", spawnWave: 1)
                });

            BattleSettlementMonsterSummary monster = settlement.monsterKills.Single();
            Assert.That(monster.monsterSpecId, Is.EqualTo("MONSTER_A"));
            Assert.That(monster.totalKills, Is.EqualTo(2));
            Assert.That(settlement.players.Sum(player => player.kills), Is.EqualTo(2));
        }

        [Test]
        public void FinalDto_ProjectsLastAttackerAndSupportWithoutReplicatingKill()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.CLEARED,
                1,
                new[] { Kill(1, "MONSTER_A", "player-a", spawnWave: 1, supportPlayerId: "player-b") });

            BattleSettlementPlayerSummary attacker = settlement.players.Single(player => player.playerId == "player-a");
            BattleSettlementPlayerSummary support = settlement.players.Single(player => player.playerId == "player-b");
            Assert.That(attacker.kills, Is.EqualTo(1));
            Assert.That(attacker.supportKills, Is.Zero);
            Assert.That(support.kills, Is.Zero);
            Assert.That(support.supportKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single().totalKills, Is.EqualTo(1));
        }

        [Test]
        public void FinalDto_ClassifiesBossByAuthoritativeBossLane()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.CLEARED,
                10,
                new[]
                {
                    Kill(1, "BOSS_SPEC", "player-a", 10, lanePolicy: BattleMonsterLanePolicy.BOSS_SHARED),
                    Kill(2, "BOSS_NAMED_NORMAL", "player-b", 10)
                });

            Assert.That(settlement.players.Single(player => player.playerId == "player-a").bossKills, Is.EqualTo(1));
            Assert.That(settlement.players.Single(player => player.playerId == "player-a").kills, Is.EqualTo(1));
            Assert.That(settlement.players.Single(player => player.playerId == "player-b").bossKills, Is.Zero);
            Assert.That(settlement.monsterKills.Single(monster => monster.monsterSpecId == "BOSS_SPEC").bossKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single(monster => monster.monsterSpecId == "BOSS_SPEC").totalKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single(monster => monster.monsterSpecId == "BOSS_NAMED_NORMAL").bossKills, Is.Zero);
        }

        [Test]
        public void FailedPartialWave_IncludesAuthorityKillBeyondHighestClearedWave()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.FAILED,
                69,
                new[] { Kill(70001, "W070_MONSTER", "player-a", spawnWave: 70, supportPlayerId: "player-b") });

            Assert.That(settlement.result, Is.EqualTo(BattleSettlementResultValues.Defeat));
            Assert.That(settlement.finalWave, Is.EqualTo(69));
            Assert.That(settlement.players.Single(player => player.playerId == "player-a").kills, Is.EqualTo(1));
            Assert.That(settlement.players.Single(player => player.playerId == "player-b").kills, Is.Zero);
            Assert.That(settlement.players.Single(player => player.playerId == "player-b").supportKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Sum(monster => monster.totalKills), Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single(monster => monster.monsterSpecId == "W070_MONSTER").totalKills, Is.EqualTo(1));
        }

        [Test]
        public void FinalDto_PreservesResultMapFinalWaveAndAbandonedFacts()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.FAILED,
                37,
                new[] { Kill(1, "MONSTER_A", "player-a", spawnWave: 38) },
                mapId: "MARS",
                playerAAbandoned: true);

            Assert.That(settlement.result, Is.EqualTo(BattleSettlementResultValues.Defeat));
            Assert.That(settlement.mapId, Is.EqualTo("MARS"));
            Assert.That(settlement.finalWave, Is.EqualTo(37));
            Assert.That(settlement.players.Single(player => player.playerId == "player-a").abandoned, Is.True);
            Assert.That(settlement.players.Single(player => player.playerId == "player-a").kills, Is.EqualTo(1));
            Assert.That(settlement.players.Single(player => player.playerId == "player-b").abandoned, Is.False);
            Assert.That(settlement.monsterKills.Single().totalKills, Is.EqualTo(1));
        }

        [Test]
        public void Summary_RejectsForeignSessionKillRecord()
        {
            BattleKillAuditRecord foreign = new BattleKillAuditRecord(
                new BattleRuntimeMonsterKey("foreign-session", 1),
                "MONSTER_A",
                "player-a",
                "player-a",
                BattleMonsterLanePolicy.EACH_FIELD,
                1,
                1);

            Assert.Throws<ArgumentException>(() => BuildSummary(
                MatchState.FAILED,
                0,
                new[] { foreign }));
        }

        [Test]
        public void VictoryWave80_IncludesWave80BossKill()
        {
            BattleSettlementSummary settlement = BuildSettlement(
                MatchState.CLEARED,
                80,
                new[] { Kill(80001, "SUN_BOSS_W080", "player-b", 80, BattleMonsterLanePolicy.BOSS_SHARED) },
                mapId: "SUN");

            Assert.That(settlement.result, Is.EqualTo(BattleSettlementResultValues.Victory));
            Assert.That(settlement.finalWave, Is.EqualTo(80));
            Assert.That(settlement.mapId, Is.EqualTo("SUN"));
            Assert.That(settlement.players.Single(player => player.playerId == "player-b").bossKills, Is.EqualTo(1));
            Assert.That(settlement.monsterKills.Single().bossKills, Is.EqualTo(1));
        }

        [Test]
        public void Summary_RejectsKillerOutsideParticipantRoster()
        {
            Assert.Throws<ArgumentException>(() => BuildSummary(
                MatchState.FAILED,
                0,
                new[] { Kill(1, "MONSTER_A", "foreign-player", spawnWave: 1) }));
        }

        [Test]
        public void Summary_RejectsSupportOutsideParticipantRoster()
        {
            Assert.Throws<ArgumentException>(() => BuildSummary(
                MatchState.FAILED,
                0,
                new[] { Kill(1, "MONSTER_A", "player-a", spawnWave: 1, supportPlayerId: "foreign-player") }));
        }

        private static BattleSettlementSummary BuildSettlement(
            MatchState result,
            int finalWave,
            IEnumerable<BattleKillAuditRecord> records,
            string mapId = "NEPTUNE",
            bool playerAAbandoned = false)
        {
            BattleSummary summary = BuildSummary(result, finalWave, records, mapId, playerAAbandoned);
            return BattleSettlementSummaryBuilder.Build(
                summary,
                "quest-request",
                new DateTime(2026, 8, 28, 0, 0, 0),
                new DateTime(2026, 8, 28, 0, 5, 0),
                "quest-summary-hash");
        }

        private static BattleSummary BuildSummary(
            MatchState result,
            int finalWave,
            IEnumerable<BattleKillAuditRecord> records,
            string mapId = "NEPTUNE",
            bool playerAAbandoned = false)
        {
            var session = new BattleSessionContext(
                SessionId,
                "balance-v1",
                "content-hash-v1",
                "battle-v1",
                "battle-hash-v1",
                100,
                mapId);
            var players = new[]
            {
                new BattlePlayerSummarySeed("player-a", 1, false, null, 100, 0, 0, 100, playerAAbandoned),
                new BattlePlayerSummarySeed("player-b", 2, false, null, 100, 0, 0, 100)
            };
            return BattleSummaryBuilder.Build(session, result, finalWave, players, records);
        }

        private static BattleKillAuditRecord Kill(
            ulong runtimeMonsterId,
            string monsterSpecId,
            string killerPlayerId,
            int spawnWave,
            BattleMonsterLanePolicy lanePolicy = BattleMonsterLanePolicy.EACH_FIELD,
            string supportPlayerId = null)
        {
            return new BattleKillAuditRecord(
                new BattleRuntimeMonsterKey(SessionId, runtimeMonsterId),
                monsterSpecId,
                killerPlayerId,
                lanePolicy == BattleMonsterLanePolicy.EACH_FIELD ? killerPlayerId : null,
                lanePolicy,
                spawnWave,
                (long)runtimeMonsterId,
                supportPlayerId);
        }
    }
}
