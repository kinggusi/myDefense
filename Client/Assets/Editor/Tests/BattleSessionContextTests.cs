using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleSessionContextTests
    {
        [Test]
        public void ContextStartsRunningAndAllowsOneTerminalTransition()
        {
            var context = new BattleSessionContext(
                "session-1", "canonical-v1", "canonical-hash", "battle-v1", "battle-hash", 10);

            Assert.That(context.MatchState, Is.EqualTo(MatchState.RUNNING));
            Assert.That(context.TryTransitionMatchState(MatchState.CLEARED), Is.True);
            Assert.That(context.MatchState, Is.EqualTo(MatchState.CLEARED));
            Assert.That(context.TryTransitionMatchState(MatchState.FAILED), Is.False);
            Assert.That(context.TryTransitionMatchState(MatchState.RUNNING), Is.False);
        }

        [Test]
        public void ContextKeepsCanonicalAndBattleMetadataTogether()
        {
            var context = new BattleSessionContext(
                "fusion-session", "canonical-v2", "canonical-hash", "battle-v2", "battle-hash", 42);

            Assert.That(context.BattleSessionId, Is.EqualTo("fusion-session"));
            Assert.That(context.CanonicalBalanceVersion, Is.EqualTo("canonical-v2"));
            Assert.That(context.CanonicalContentHash, Is.EqualTo("canonical-hash"));
            Assert.That(context.BattleContentVersion, Is.EqualTo("battle-v2"));
            Assert.That(context.BattleContentHash, Is.EqualTo("battle-hash"));
            Assert.That(context.StartedAtTick, Is.EqualTo(42));
        }
    }
}
