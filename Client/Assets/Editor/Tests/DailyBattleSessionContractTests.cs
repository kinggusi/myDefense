using System.IO;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Shared.Tests
{
    public sealed class DailyBattleSessionContractTests
    {
        [Test]
        public void CanonicalJsonMatchesSpringFixtureByteForByte()
        {
            var context = new DailyBattleSessionContext
            {
                runId = "run-daily-001",
                battleSessionId = "daily-session-001",
                contentType = "CULTIVATION_ZONE",
                stage = 3,
                mapId = "DAILY_CULTIVATION_ZONE",
                balanceVersion = "1-dailybalance0001",
                contentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
            };
            string fixturePath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../contracts/daily-battle-session-v1.json"));

            Assert.That(DailyBattleSessionContextJson.Serialize(context),
                Is.EqualTo(File.ReadAllText(fixturePath).Trim()));
        }

        [TestCase("CULTIVATION_ZONE", "DAILY_MUTATION_LAB")]
        [TestCase("MUTATION_LAB", "DAILY_CULTIVATION_ZONE")]
        [TestCase("UNKNOWN", "DAILY_CULTIVATION_ZONE")]
        public void RejectsContentTypeMapMismatch(string contentType, string mapId)
        {
            var context = ValidContext();
            context.contentType = contentType;
            context.mapId = mapId;

            Assert.Throws<System.ArgumentException>(() => DailyBattleSessionContextValidator.Validate(context));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void RejectsStageOutsideOneThroughFive(int stage)
        {
            var context = ValidContext();
            context.stage = stage;

            Assert.Throws<System.ArgumentException>(() => DailyBattleSessionContextValidator.Validate(context));
        }

        private static DailyBattleSessionContext ValidContext()
        {
            return new DailyBattleSessionContext
            {
                runId = "run",
                battleSessionId = "session",
                contentType = "CULTIVATION_ZONE",
                stage = 1,
                mapId = "DAILY_CULTIVATION_ZONE",
                balanceVersion = "balance",
                contentHash = "hash"
            };
        }
    }
}
