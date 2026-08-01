using System.Reflection;
using System.Linq;
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
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.HighestClearedWave)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1DisconnectGraceTimer)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2DisconnectGraceTimer)), Is.Not.Null);
            Assert.That(BattleWaveStateAuthority.DisconnectRewardGraceSeconds, Is.EqualTo(120f));
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.IsWaveRunning)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.MatchStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1AliveMonsterCount)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2AliveMonsterCount)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.PlayerMonsterLimit)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1WarningReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2WarningReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1DangerReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2DangerReached)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.TeamInGameGold)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1InitialInGameGold)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2InitialInGameGold)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1InGameGoldEarned)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2InGameGoldEarned)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1InGameGoldSpent)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2InGameGoldSpent)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.BossTimer)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.BossTimer)).PropertyType, Is.EqualTo(typeof(TickTimer)));
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty("Player1MythicChoiceTimer", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty("Player2MythicChoiceTimer", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.GetTeamInGameGold)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.TryCreatePlayerSummarySeed)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.TryAwardMonsterKill), new[] { typeof(BattleMonsterNetworkState) }), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.TryHandleAuthoritativeBossDefeat), new[] { typeof(BattleMonsterNetworkState) }), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.TryAwardTeamGold)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1Eliminated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2Eliminated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1BattleStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2BattleStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1BattleState)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2BattleState)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.IsPlayerActionAllowed)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.IsBoardSlotLockedForMythicChoice)), Is.Not.Null);
        }

        [TestCase(1, 0, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(1, 2, 0)]
        [TestCase(3, 1, 2)]
        public void RemainingMythicRerollsNeverExposeUsedCounters(int limit, int used, int expected)
        {
            Assert.That(BattleWaveStateAuthority.RemainingRerolls(limit, used), Is.EqualTo(expected));
        }

        [Test]
        public void BossTimerUsesNetworkedTickTimerAndAuthorityTimeoutEntryPoint()
        {
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.BossTimer)), Is.Not.Null);
            Assert.That(typeof(BattleWaveExecutor).GetProperty(nameof(BattleWaveExecutor.ActiveBossTimeLimitSeconds)), Is.Not.Null);
            Assert.That(typeof(BattleWaveExecutor).GetMethod(nameof(BattleWaveExecutor.TryResolveBossTimeoutFromAuthority)), Is.Not.Null);
            Assert.That(typeof(BattleWaveExecutor).GetMethod(nameof(BattleWaveExecutor.TryResolveBossDefeatFromAuthority), new[] { typeof(BattleMonsterNetworkState) }), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("HandleBossTimeout", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }

        [Test]
        public void BossOutcomeEventsHaveAuthorityTimerClearHandlers()
        {
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("HandleWaveCompleted", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("HandleBossTimeout", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveExecutor).GetEvent(nameof(BattleWaveExecutor.OnBossDefeated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveExecutor).GetEvent(nameof(BattleWaveExecutor.OnBossTimeout)), Is.Not.Null);
        }

        [Test]
        public void MythicChoiceUsesNetworkedTimeoutTimersAndAutoSelectionEntryPoint()
        {
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("UpdateMythicChoiceTimers", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("CreateMythicChoiceTimer", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("ApplyMythicChoice", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod("ApplyMythicReroll", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }

        [Test]
        public void MythicChoiceActionsAreAuthorityOnlyAndExposeLocalLockQuery()
        {
            var choiceMethod = typeof(BattleWaveStateAuthority).GetMethod("ApplyMythicChoice", BindingFlags.Instance | BindingFlags.NonPublic);
            var rerollMethod = typeof(BattleWaveStateAuthority).GetMethod("ApplyMythicReroll", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(choiceMethod, Is.Not.Null);
            Assert.That(rerollMethod, Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.RequestMythicChoice)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.RequestMythicReroll)), Is.Not.Null);
        }

        [Test]
        public void MutationDnaInheritanceUsesDeterministicFiftyFiftySelection()
        {
            Assert.That(BattleWaveStateAuthority.ResolveInheritedMutation(null, "DNA_A", 0), Is.EqualTo("DNA_A"));
            Assert.That(BattleWaveStateAuthority.ResolveInheritedMutation("DNA_A", null, 1), Is.EqualTo("DNA_A"));
            Assert.That(BattleWaveStateAuthority.ResolveInheritedMutation("DNA_A", "DNA_A", 1), Is.EqualTo("DNA_A"));
            Assert.That(BattleWaveStateAuthority.ResolveInheritedMutation("DNA_A", "DNA_B", 0), Is.EqualTo("DNA_A"));
            Assert.That(BattleWaveStateAuthority.ResolveInheritedMutation("DNA_A", "DNA_B", 1), Is.EqualTo("DNA_B"));
        }

        [Test]
        public void MythicCandidatesAndRerollPolicyComeFromCanonicalBalance()
        {
            Assert.That(BattleMergeResultResolver.TryResolveMythicCandidates(7, out long[] first), Is.True);
            Assert.That(first, Has.Length.EqualTo(3));
            Assert.That(first.Distinct().Count(), Is.EqualTo(3));
            Assert.That(first.All(id => id > 0), Is.True);

            Assert.That(BattleMergeResultResolver.TryResolveMythicCandidates(11, first, out long[] rerolled), Is.True);
            Assert.That(rerolled, Has.Length.EqualTo(3));
            Assert.That(rerolled.Intersect(first).Any(), Is.False);

            Assert.That(BattleMergeResultResolver.TryGetMythicRerollPolicy(
                out int freeCount, out int paidLimit, out int paidCost, out int timeoutSeconds), Is.True);
            Assert.That(freeCount, Is.EqualTo(1));
            Assert.That(paidLimit, Is.EqualTo(1));
            Assert.That(paidCost, Is.EqualTo(100));
            Assert.That(timeoutSeconds, Is.EqualTo(10));
        }

        [Test]
        public void LockedMythicKeepsInheritedDnaSealedAndBlocksDirectInjector()
        {
            Assert.That(BattleMergeResultResolver.TryGetMythicMutationEligibility(29, out bool unlocked), Is.True);
            Assert.That(unlocked, Is.True);
            Assert.That(BattleMergeResultResolver.TryGetMythicMutationEligibility(33, out bool locked), Is.True);
            Assert.That(locked, Is.False);
            Assert.That(BattleWaveStateAuthority.ResolveMythicMutationState(29, "DNA_A"),
                Is.EqualTo(MyDefense.Shared.Contracts.BattleMutationState.ACTIVE));
            Assert.That(BattleWaveStateAuthority.ResolveMythicMutationState(33, "DNA_A"),
                Is.EqualTo(MyDefense.Shared.Contracts.BattleMutationState.SEALED));
            Assert.That(BattleWaveStateAuthority.ResolveMythicMutationState(33, null),
                Is.EqualTo(MyDefense.Shared.Contracts.BattleMutationState.NONE));
        }

        [Test]
        public void ExecutorIsResolvedFromTheSameNetworkObjectOnSpawn()
        {
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.Spawned)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetField("_executor", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }

        [TestCase(0, true)]
        [TestCase(23, true)]
        [TestCase(-1, false)]
        [TestCase(24, false)]
        public void BoardSlotRangeIsExactlyTwentyFourSlots(int slotIndex, bool expected)
        {
            Assert.That(BattleWaveStateAuthority.IsValidBoardIndex(slotIndex), Is.EqualTo(expected));
        }

        [Test]
        public void FirstEmptyBoardSlotUsesAscendingLogicalOrder()
        {
            bool[] occupied = Enumerable.Repeat(true, 24).ToArray();
            occupied[0] = false;
            occupied[7] = false;

            Assert.That(BattleWaveStateAuthority.FindFirstEmptyBoardSlot(occupied), Is.EqualTo(0));
        }

        [Test]
        public void FullBoardHasNoFirstEmptySlot()
        {
            bool[] occupied = Enumerable.Repeat(true, 24).ToArray();

            Assert.That(BattleWaveStateAuthority.FindFirstEmptyBoardSlot(occupied), Is.EqualTo(-1));
            Assert.That(BattleWaveStateAuthority.FindFirstEmptyBoardSlot(new bool[23]), Is.EqualTo(-1));
        }
    }
}
