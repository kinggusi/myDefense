using Fusion;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleMatchStartCoordinatorTests
    {
        [Test]
        public void MatchWaitsForTwoPlayersAndBothReadySignals()
        {
            var roster = new BattlePlayerRoster();
            var first = PlayerRef.FromIndex(0);
            var second = PlayerRef.FromIndex(1);
            roster.TryAdd(first, "user-a", out _);
            var coordinator = new BattleMatchStartCoordinator(roster);

            Assert.That(coordinator.State, Is.EqualTo(BattleStartState.WAITING_FOR_PLAYERS));
            Assert.That(coordinator.SetReady(first, true), Is.True);
            Assert.That(coordinator.TryStart(), Is.False);

            roster.TryAdd(second, "user-b", out _);
            Assert.That(coordinator.SetReady(second, true), Is.True);
            Assert.That(coordinator.TryStart(), Is.True);
            Assert.That(coordinator.State, Is.EqualTo(BattleStartState.STARTED));
        }

        [Test]
        public void UnreadyOrUnknownPlayerCannotStart()
        {
            var roster = new BattlePlayerRoster();
            var first = PlayerRef.FromIndex(0);
            var second = PlayerRef.FromIndex(1);
            roster.TryAdd(first, "user-a", out _);
            roster.TryAdd(second, "user-b", out _);
            var coordinator = new BattleMatchStartCoordinator(roster);

            Assert.That(coordinator.SetReady(PlayerRef.FromIndex(2), true), Is.False);
            Assert.That(coordinator.SetReady(first, true), Is.True);
            Assert.That(coordinator.TryStart(), Is.False);
            Assert.That(coordinator.SetReady(first, false), Is.True);
            Assert.That(coordinator.SetReady(second, true), Is.True);
            Assert.That(coordinator.TryStart(), Is.False);
        }

        [Test]
        public void StartIsOneShotUntilCoordinatorReset()
        {
            var roster = new BattlePlayerRoster();
            var first = PlayerRef.FromIndex(0);
            var second = PlayerRef.FromIndex(1);
            roster.TryAdd(first, "user-a", out _);
            roster.TryAdd(second, "user-b", out _);
            var coordinator = new BattleMatchStartCoordinator(roster);
            coordinator.SetReady(first, true);
            coordinator.SetReady(second, true);

            Assert.That(coordinator.TryStart(), Is.True);
            Assert.That(coordinator.TryStart(), Is.False);
            coordinator.Reset();
            Assert.That(coordinator.State, Is.EqualTo(BattleStartState.WAITING_FOR_PLAYERS));
        }

        [Test]
        public void ADisconnectedReadyPlayerCannotSatisfyAReplacementPlayer()
        {
            var roster = new BattlePlayerRoster();
            var first = PlayerRef.FromIndex(0);
            var second = PlayerRef.FromIndex(1);
            var replacement = PlayerRef.FromIndex(2);
            roster.TryAdd(first, "user-a", out _);
            roster.TryAdd(second, "user-b", out _);
            var coordinator = new BattleMatchStartCoordinator(roster);
            coordinator.SetReady(first, true);
            coordinator.SetReady(second, true);
            Assert.That(coordinator.TryStart(), Is.True);

            roster.Remove(second);
            coordinator.Reset();
            roster.TryAdd(replacement, "user-c", out _);
            coordinator.SetReady(first, true);
            Assert.That(coordinator.TryStart(), Is.False);
            coordinator.SetReady(replacement, true);
            Assert.That(coordinator.TryStart(), Is.True);
        }
    }
}
