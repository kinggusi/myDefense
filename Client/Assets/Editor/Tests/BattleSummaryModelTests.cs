using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSummaryModelTests
    {
        [Test]
        public void Summary_AggregatesMonsterPlayerBossAndLaneCounts()
        {
            BattleSummary summary = BuildSummary(RecordsInReverseOrder());

            Assert.That(summary.BattleSessionId, Is.EqualTo("summary-session"));
            Assert.That(summary.Result, Is.EqualTo(MatchState.CLEARED));
            Assert.That(summary.FinalWave, Is.EqualTo(10));
            Assert.That(summary.Kills.ByMonster.Select(item => item.MonsterId), Is.EqualTo(new[] { "BOSS", "MONSTER_A", "MONSTER_B" }));
            Assert.That(summary.Kills.ByMonster.Select(item => item.KillCount), Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(summary.Players.Single(player => player.PlayerId == "player-alpha").Kills, Is.EqualTo(3));
            Assert.That(summary.Players.Single(player => player.PlayerId == "player-beta").Kills, Is.EqualTo(1));
            Assert.That(summary.Players.Single(player => player.PlayerId == "player-beta").BossKills, Is.EqualTo(1));
            Assert.That(summary.Kills.ByLanePolicy.Single(item => item.LanePolicy == BattleMonsterLanePolicy.EACH_FIELD).KillCount, Is.EqualTo(3));
            Assert.That(summary.Kills.ByLanePolicy.Single(item => item.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED).KillCount, Is.EqualTo(1));
            Assert.That(summary.Kills.ByLanePolicy.Sum(item => item.AwardedGold), Is.Zero);
        }

        [Test]
        public void Summary_DeduplicatesRuntimeKeyAndCountsBossOnce()
        {
            List<BattleKillAuditRecord> records = RecordsInReverseOrder();
            records.Add(records.Single(record => record.LanePolicy == BattleMonsterLanePolicy.BOSS_SHARED));

            BattleSummary summary = BuildSummary(records);

            Assert.That(summary.Kills.ProcessedRuntimeKeys.Count, Is.EqualTo(4));
            Assert.That(summary.Players.Sum(player => player.BossKills), Is.EqualTo(1));
        }

        [Test]
        public void Summary_OutputOrderingIsDeterministicForSameInput()
        {
            BattleSummary first = BuildSummary(RecordsInReverseOrder());
            List<BattleKillAuditRecord> reordered = RecordsInReverseOrder().OrderBy(record => record.MonsterId).ToList();
            BattleSummary second = BuildSummary(reordered);

            Assert.That(Signature(first), Is.EqualTo(Signature(second)));
            Assert.That(first.Players.Select(player => player.PlayerId), Is.EqualTo(new[] { "player-alpha", "player-beta" }));
            Assert.That(first.Kills.ProcessedRuntimeKeys.Select(key => key.RuntimeMonsterId), Is.EqualTo(new ulong[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Summary_CollectionsCannotBeMutatedExternally()
        {
            BattleSummary summary = BuildSummary(RecordsInReverseOrder());

            Assert.Throws<NotSupportedException>(() => ((IList<BattlePlayerSummary>)summary.Players).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BattleMonsterKillSummary>)summary.Kills.ByMonster).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BattleRuntimeMonsterKey>)summary.Kills.ProcessedRuntimeKeys).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<BattleLaneKillSummary>)summary.Kills.ByLanePolicy).Clear());
        }

        [Test]
        public void Summary_GoldFieldsAreInputOnlyAndNoSettlementSideEffectOccurs()
        {
            BattleSummary summary = BuildSummary(RecordsInReverseOrder());
            BattlePlayerSummary alpha = summary.Players.Single(player => player.PlayerId == "player-alpha");

            Assert.That(alpha.InGameGoldEarned, Is.EqualTo(25));
            Assert.That(alpha.InGameGoldSpent, Is.EqualTo(7));
            Assert.That(summary.Kills.ByLanePolicy.All(item => item.AwardedGold == 0), Is.True);
        }

        [Test]
        public void SettlementBuilderMapsBattleSummaryToSpringContract()
        {
            var session = new BattleSessionContext(
                "settlement-session",
                "canonical-v1",
                "canonical-hash",
                "battle-v1",
                "battle-hash",
                100);
            var players = new[]
            {
                new BattlePlayerSummarySeed("player-alpha", 1, false, null, 100, 50, 20, 130),
                new BattlePlayerSummarySeed("player-beta", 2, true, 9, 100, 20, 10, 110)
            };
            BattleSummary summary = BattleSummaryBuilder.Build(
                session,
                MatchState.CLEARED,
                10,
                players,
                RecordsInReverseOrder(),
                monsterId => monsterId == "BOSS" ? 200 : 20);

            var transport = BattleSettlementSummaryBuilder.Build(
                summary,
                "request-1",
                new DateTime(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 18, 12, 20, 0, DateTimeKind.Utc),
                "summary-hash");

            Assert.That(transport.result, Is.EqualTo(BattleSettlementResultValues.Victory));
            Assert.That(transport.players.Select(player => player.playerSlot), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(transport.players[0].finalInGameGold, Is.EqualTo(130));
            Assert.That(transport.monsterKills.Single(monster => monster.monsterSpecId == "BOSS").totalKillGold, Is.EqualTo(200));
            Assert.That(transport.startedAt, Is.EqualTo("2026-07-18T12:00:00.000"));
            Assert.That(transport.finishedAt, Is.EqualTo("2026-07-18T12:20:00.000"));
        }

        private static BattleSummary BuildSummary(IEnumerable<BattleKillAuditRecord> records)
        {
            var session = new BattleSessionContext(
                "summary-session",
                "canonical-v1",
                "canonical-hash",
                "battle-v1",
                "battle-hash",
                100);
            var players = new[]
            {
                new BattlePlayerSummarySeed("player-beta", true, 9, 10, 3),
                new BattlePlayerSummarySeed("player-alpha", false, null, 25, 7)
            };
            return BattleSummaryBuilder.Build(session, MatchState.CLEARED, 10, players, records);
        }

        private static List<BattleKillAuditRecord> RecordsInReverseOrder()
        {
            return new List<BattleKillAuditRecord>
            {
                Kill(4, "BOSS", "player-beta", BattleMonsterLanePolicy.BOSS_SHARED),
                Kill(3, "MONSTER_B", "player-alpha", BattleMonsterLanePolicy.EACH_FIELD),
                Kill(2, "MONSTER_A", "player-alpha", BattleMonsterLanePolicy.EACH_FIELD),
                Kill(1, "MONSTER_A", "player-alpha", BattleMonsterLanePolicy.EACH_FIELD)
            };
        }

        private static BattleKillAuditRecord Kill(
            ulong runtimeId,
            string monsterId,
            string killer,
            BattleMonsterLanePolicy lanePolicy)
        {
            return new BattleKillAuditRecord(
                new BattleRuntimeMonsterKey("summary-session", runtimeId),
                monsterId,
                killer,
                lanePolicy == BattleMonsterLanePolicy.EACH_FIELD ? "field-owner" : null,
                lanePolicy,
                runtimeId == 4 ? 10 : 1,
                (long)runtimeId * 10);
        }

        private static string Signature(BattleSummary summary)
        {
            return string.Join(
                "|",
                summary.Players.Select(player => player.PlayerId + ":" + player.Kills + ":" + player.BossKills)
                    .Concat(summary.Kills.ByMonster.Select(item => item.MonsterId + ":" + item.KillCount))
                    .Concat(summary.Kills.ProcessedRuntimeKeys.Select(key => key.ToString()))
                    .Concat(summary.Kills.ByLanePolicy.Select(item => item.LanePolicy + ":" + item.KillCount)));
        }
    }
}
