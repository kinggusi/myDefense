using System;
using System.Linq;
using System.Reflection;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Shared.Tests
{
    public sealed class BattleSettlementSummaryContractTests
    {
        [Test]
        public void SummaryFields_MatchSpringRequestRecord()
        {
            AssertPublicFieldNames<BattleSettlementSummary>(
                "requestId",
                "battleSessionId",
                "balanceVersion",
                "contentHash",
                "result",
                "finalWave",
                "startedAt",
                "finishedAt",
                "players",
                "monsterKills",
                "summaryHash");
        }

        [Test]
        public void PlayerFields_MatchSpringPlayerRecord()
        {
            AssertPublicFieldNames<BattleSettlementPlayerSummary>(
                "playerId",
                "playerSlot",
                "eliminated",
                "eliminatedWave",
                "kills",
                "supportKills",
                "bossKills",
                "initialInGameGold",
                "inGameGoldEarned",
                "inGameGoldSpent",
                "finalInGameGold");
        }

        [Test]
        public void MonsterFields_MatchSpringMonsterRecord()
        {
            AssertPublicFieldNames<BattleSettlementMonsterSummary>(
                "monsterSpecId",
                "totalKills",
                "bossKills",
                "totalKillGold");
        }

        [Test]
        public void ResultValues_MatchSpringBattleResult()
        {
            Assert.That(BattleSettlementResultValues.IsDefined("VICTORY"), Is.True);
            Assert.That(BattleSettlementResultValues.IsDefined("DEFEAT"), Is.True);
            Assert.That(BattleSettlementResultValues.IsDefined("ABORTED"), Is.True);
            Assert.That(BattleSettlementResultValues.IsDefined("CLEARED"), Is.False);
            Assert.That(BattleSettlementResultValues.IsDefined(null), Is.False);
        }

        [Test]
        public void FieldTypes_MatchSpringJsonContract()
        {
            AssertPublicFieldTypes<BattleSettlementSummary>(
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(int),
                typeof(string),
                typeof(string),
                typeof(BattleSettlementPlayerSummary[]),
                typeof(BattleSettlementMonsterSummary[]),
                typeof(string));
            AssertPublicFieldTypes<BattleSettlementPlayerSummary>(
                typeof(string),
                typeof(int),
                typeof(bool),
                typeof(int?),
                typeof(int),
                typeof(int),
                typeof(int),
                typeof(int),
                typeof(int),
                typeof(int),
                typeof(int));
            AssertPublicFieldTypes<BattleSettlementMonsterSummary>(
                typeof(string),
                typeof(int),
                typeof(int),
                typeof(int));
        }

        [Test]
        public void Serializer_PreservesTransportFieldNamesNullableWaveAndValues()
        {
            var summary = new BattleSettlementSummary
            {
                requestId = "request-1",
                battleSessionId = "session-1",
                balanceVersion = "balance-v1",
                contentHash = "content-hash",
                result = BattleSettlementResultValues.Victory,
                finalWave = 80,
                startedAt = "2026-07-18T12:00:00",
                finishedAt = "2026-07-18T12:20:00",
                players = new[]
                {
                    new BattleSettlementPlayerSummary
                    {
                        playerId = "player-a",
                        playerSlot = 1,
                        eliminated = false,
                        eliminatedWave = null,
                        kills = 10,
                        supportKills = 2,
                        bossKills = 1,
                        initialInGameGold = 100,
                        inGameGoldEarned = 50,
                        inGameGoldSpent = 20,
                        finalInGameGold = 130
                    },
                    new BattleSettlementPlayerSummary
                    {
                        playerId = "player-b",
                        playerSlot = 2,
                        eliminated = true,
                        eliminatedWave = 9,
                        kills = 5,
                        supportKills = 1,
                        bossKills = 0,
                        initialInGameGold = 100,
                        inGameGoldEarned = 20,
                        inGameGoldSpent = 10,
                        finalInGameGold = 110
                    }
                },
                monsterKills = new[]
                {
                    new BattleSettlementMonsterSummary
                    {
                        monsterSpecId = "NORMAL_MONSTER",
                        totalKills = 10,
                        bossKills = 0,
                        totalKillGold = 200
                    }
                },
                summaryHash = "summary-hash"
            };

            string json = BattleSettlementSummaryJson.Serialize(summary);

            StringAssert.Contains("\"battleSessionId\":\"session-1\"", json);
            StringAssert.Contains("\"supportKills\":2", json);
            StringAssert.Contains("\"eliminatedWave\":null", json);
            StringAssert.Contains("\"eliminatedWave\":9", json);
            StringAssert.Contains("\"monsterSpecId\":\"NORMAL_MONSTER\"", json);
            StringAssert.Contains("\"finalInGameGold\":130", json);
            StringAssert.Contains("\"totalKillGold\":200", json);
        }

        [Test]
        public void Serializer_EscapesJsonControlCharacters()
        {
            var summary = new BattleSettlementSummary
            {
                requestId = "request-\"1\\\b\f\n\r\t\u0001",
                result = BattleSettlementResultValues.Aborted,
                players = Array.Empty<BattleSettlementPlayerSummary>(),
                monsterKills = Array.Empty<BattleSettlementMonsterSummary>()
            };

            string json = BattleSettlementSummaryJson.Serialize(summary);

            StringAssert.Contains(
                "\"requestId\":\"request-\\\"1\\\\\\b\\f\\n\\r\\t\\u0001\"",
                json);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("CLEARED")]
        public void Serializer_RejectsUndefinedResult(string result)
        {
            var summary = new BattleSettlementSummary { result = result };

            Assert.Throws<ArgumentException>(() => BattleSettlementSummaryJson.Serialize(summary));
        }

        private static void AssertPublicFieldNames<T>(params string[] expected)
        {
            string[] actual = typeof(T)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => field.Name)
                .ToArray();

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static void AssertPublicFieldTypes<T>(params Type[] expected)
        {
            Type[] actual = typeof(T)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => field.FieldType)
                .ToArray();

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
