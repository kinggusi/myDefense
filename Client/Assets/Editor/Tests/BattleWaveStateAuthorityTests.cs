using System.Reflection;
using Fusion;
using MyDefense.Battle;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleWaveStateAuthorityTests
    {
        [Test]
        public void AuthorityBoundaryIsAFusionNetworkBehaviour()
        {
            Assert.That(typeof(BattleWaveStateAuthority).BaseType, Is.EqualTo(typeof(NetworkBehaviour)));
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.InitializeSession)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.TryStartNextWave)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.ValidateWaveStart)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.ValidateWaveEnd)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.ValidateMatchState)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.CurrentWave)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.IsWaveRunning)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.MatchStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1AliveMonsterCount)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2AliveMonsterCount)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.PlayerMonsterLimit)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1WarningReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2WarningReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1DangerReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2DangerReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1Eliminated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2Eliminated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1BattleStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2BattleStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1BattleState)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2BattleState)), Is.Not.Null);
        }

        [Test]
        public void ExecutorIsResolvedFromTheSameNetworkObjectOnSpawn()
        {
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.Spawned)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetField("_executor", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }
    }
}
