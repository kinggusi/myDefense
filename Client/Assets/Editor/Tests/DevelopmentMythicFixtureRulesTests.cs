#if UNITY_EDITOR
using MyDefense.Battle.Runtime;
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
    }
}
#endif
