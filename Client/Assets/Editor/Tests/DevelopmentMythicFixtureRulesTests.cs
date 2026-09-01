#if UNITY_EDITOR
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Tests.EditMode
{
    public sealed class DevelopmentMythicFixtureRulesTests
    {
        [TestCase("GIANT")]
        [TestCase("BERSERK")]
        [TestCase("SWIFT")]
        [TestCase("TOXIC")]
        [TestCase("GREEDY")]
        [TestCase("OBESE")]
        [TestCase("FROZEN")]
        [TestCase("BLANK")]
        [TestCase("NONE")]
        public void UnlockedMythicAndSupportedMutation_AreAccepted(string mutationType)
        {
            Assert.That(DevelopmentMythicFixtureRules.TryNormalize(
                29, mutationType, out string normalized, out string reason), Is.True, reason);
            Assert.That(normalized, Is.EqualTo(mutationType));
        }

        [Test]
        public void BlankMutation_IsNormalizedToPureMythic()
        {
            Assert.That(DevelopmentMythicFixtureRules.TryNormalize(
                29, "  ", out string normalized, out string reason), Is.True, reason);
            Assert.That(normalized, Is.EqualTo(DevelopmentMythicFixtureRules.NoneMutation));
        }

        [Test]
        public void NonMythic_IsRejected()
        {
            Assert.That(DevelopmentMythicFixtureRules.TryNormalize(
                22, "TOXIC", out _, out string reason), Is.False);
            StringAssert.Contains("not a canonical Mythic", reason);
        }

        [Test]
        public void LockedMythic_IsRejected()
        {
            Assert.That(DevelopmentMythicFixtureRules.TryNormalize(
                33, "TOXIC", out _, out string reason), Is.False);
            StringAssert.Contains("locked", reason);
        }

        [Test]
        public void UnknownMutation_IsRejected()
        {
            Assert.That(DevelopmentMythicFixtureRules.TryNormalize(
                29, "UNKNOWN", out _, out string reason), Is.False);
            StringAssert.Contains("not supported", reason);
        }
        [TestCase(true, true, true, false, true)]
        [TestCase(false, true, true, false, false)]
        [TestCase(true, false, true, false, false)]
        [TestCase(true, true, false, false, false)]
        [TestCase(true, true, true, true, false)]
        public void ValidationWaveStartButton_IsHostAuthorityOneShotOnly(
            bool isSpawned,
            bool isStateAuthority,
            bool isValidationArmed,
            bool isValidationStartConsumed,
            bool expected)
        {
            Assert.That(DevelopmentMythicFixtureRules.CanStartValidationWave(
                isSpawned,
                isStateAuthority,
                isValidationArmed,
                isValidationStartConsumed), Is.EqualTo(expected));
        }

        [Test]
        public void PartialSettlementFailure_RequiresRealPartialWaveEvidence()
        {
            Assert.That(DevelopmentPartialSettlementFixtureRules.TryValidate(
                true, true, false, MatchState.RUNNING, true,
                7, 6, 12, 3, 9, true, out string reason), Is.True, reason);
        }

        [TestCase(false, true, false, MatchState.RUNNING, true, 7, 6, 12, 3, 9, true)]
        [TestCase(true, false, false, MatchState.RUNNING, true, 7, 6, 12, 3, 9, true)]
        [TestCase(true, true, true, MatchState.RUNNING, true, 7, 6, 12, 3, 9, true)]
        [TestCase(true, true, false, MatchState.FAILED, true, 7, 6, 12, 3, 9, true)]
        [TestCase(true, true, false, MatchState.RUNNING, false, 7, 6, 12, 3, 9, true)]
        [TestCase(true, true, false, MatchState.RUNNING, true, 8, 6, 12, 3, 9, true)]
        [TestCase(true, true, false, MatchState.RUNNING, true, 7, 6, 0, 0, 0, true)]
        [TestCase(true, true, false, MatchState.RUNNING, true, 7, 6, 12, 0, 12, true)]
        [TestCase(true, true, false, MatchState.RUNNING, true, 7, 6, 12, 12, 0, true)]
        [TestCase(true, true, false, MatchState.RUNNING, true, 7, 6, 12, 3, 9, false)]
        public void PartialSettlementFailure_RejectsUnsafeContexts(
            bool isSpawned,
            bool isStateAuthority,
            bool isP1ValidationArmed,
            MatchState matchState,
            bool isWaveRunning,
            int currentWave,
            int highestClearedWave,
            int spawnCount,
            int killCount,
            int remainingCount,
            bool evidenceConsistent)
        {
            Assert.That(DevelopmentPartialSettlementFixtureRules.TryValidate(
                isSpawned,
                isStateAuthority,
                isP1ValidationArmed,
                matchState,
                isWaveRunning,
                currentWave,
                highestClearedWave,
                spawnCount,
                killCount,
                remainingCount,
                evidenceConsistent,
                out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }
    }
}
#endif
