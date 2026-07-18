using Fusion;
using MyDefense.Battle.Runtime;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace MyDefense.Battle.Tests
{
    public sealed class BattlePlayerIdentityTests
    {
        [Test]
        public void FirstTwoPlayersReceiveStableSlots()
        {
            var roster = new BattlePlayerRoster();
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(0), "user-a", out BattlePlayerIdentity first), Is.True);
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(1), "user-b", out BattlePlayerIdentity second), Is.True);
            Assert.That(first.PlayerSlot, Is.EqualTo(1));
            Assert.That(second.PlayerSlot, Is.EqualTo(2));
            Assert.That(roster.Count, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateUserAndThirdPlayerAreRejected()
        {
            var roster = new BattlePlayerRoster();
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(0), "user-a", out _), Is.True);
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(1), "user-a", out _), Is.False);
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(1), "user-b", out _), Is.True);
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(2), "user-c", out _), Is.False);
        }

        [Test]
        public void RemovingPlayerFreesTheirSlotForTheNextConnection()
        {
            var roster = new BattlePlayerRoster();
            var player = PlayerRef.FromIndex(0);
            Assert.That(roster.TryAdd(player, "user-a", out _), Is.True);
            Assert.That(roster.Remove(player), Is.True);
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(1), "user-b", out BattlePlayerIdentity replacement), Is.True);
            Assert.That(replacement.PlayerSlot, Is.EqualTo(1));
        }

        [Test]
        public void UserTokenRoundTripsAndRejectsEmptyTokens()
        {
            byte[] token = BattlePlayerIdentityToken.Encode("user-a");
            Assert.That(BattlePlayerIdentityToken.TryDecode(token, out string userId), Is.True);
            Assert.That(userId, Is.EqualTo("user-a"));
            Assert.That(BattlePlayerIdentityToken.TryDecode(null, out _), Is.False);
            Assert.Throws<System.ArgumentException>(() => BattlePlayerIdentityToken.Encode(" "));
        }

        [Test]
        public void MalformedOrOversizedTokensAreRejected()
        {
            Assert.That(BattlePlayerIdentityToken.TryDecode(new byte[] { 0xC3, 0x28 }, out _), Is.False);
            Assert.That(BattlePlayerIdentityToken.TryDecode(new byte[257], out _), Is.False);
        }

        [Test]
        public void ClearingRosterRemovesAllMappingsForRunnerRestart()
        {
            var roster = new BattlePlayerRoster();
            roster.TryAdd(PlayerRef.FromIndex(0), "user-a", out _);
            roster.TryAdd(PlayerRef.FromIndex(1), "user-b", out _);
            roster.Clear();
            Assert.That(roster.Count, Is.Zero);
            Assert.That(roster.TryGetByUserId("user-a", out _), Is.False);
        }

        [Test]
        public void PlayersChangedFiresOnlyForSuccessfulRosterChanges()
        {
            var roster = new BattlePlayerRoster();
            int changes = 0;
            roster.PlayersChanged += () => changes++;

            Assert.That(roster.TryAdd(PlayerRef.FromIndex(0), "user-a", out _), Is.True);
            Assert.That(roster.TryAdd(PlayerRef.FromIndex(0), "user-a", out _), Is.False);
            Assert.That(roster.Remove(PlayerRef.FromIndex(0)), Is.True);
            roster.Clear();

            Assert.That(changes, Is.EqualTo(2));
        }
    }
}
