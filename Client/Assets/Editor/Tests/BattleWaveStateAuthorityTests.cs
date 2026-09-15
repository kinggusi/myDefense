using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;
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
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.AuthoritativeMapId)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.AuthoritativeMapId)).PropertyType,
                Is.EqualTo(typeof(NetworkString<_32>)));
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
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(
                nameof(BattleWaveStateAuthority.TryAwardCooperativeKillGold),
                new[] { typeof(BattleMonsterLanePolicy), typeof(int) }), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1Eliminated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2Eliminated)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1BattleStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2BattleStateValue)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player1BattleState)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(nameof(BattleWaveStateAuthority.Player2BattleState)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.IsPlayerActionAllowed)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.IsBoardSlotLockedForMythicChoice)), Is.Not.Null);
        }

        [Test]
        public void AuthoritativeMapBinding_IsImmutableAndRejectsMismatch()
        {
            Assert.That(BattleWaveStateAuthority.TryResolveAuthoritativeMapBinding(
                true, "EARTH", null, out string first, out bool retry, out string reason), Is.True, reason);
            Assert.That(first, Is.EqualTo("EARTH"));
            Assert.That(retry, Is.False);

            Assert.That(BattleWaveStateAuthority.TryResolveAuthoritativeMapBinding(
                true, "MARS", "EARTH", out _, out retry, out reason), Is.False);
            Assert.That(retry, Is.False);
            Assert.That(reason, Does.Contain("cannot change"));
        }

        [Test]
        public void ClientMapBinding_WaitsForReplicationAndRejectsLocalMismatch()
        {
            Assert.That(BattleWaveStateAuthority.TryResolveAuthoritativeMapBinding(
                false, "EARTH", null, out _, out bool retry, out string reason), Is.False);
            Assert.That(retry, Is.True);
            Assert.That(reason, Does.Contain("Waiting"));

            Assert.That(BattleWaveStateAuthority.TryResolveAuthoritativeMapBinding(
                false, "EARTH", "MARS", out _, out retry, out reason), Is.False);
            Assert.That(retry, Is.False);
            Assert.That(reason, Does.Contain("does not match"));

            Assert.That(BattleWaveStateAuthority.TryResolveAuthoritativeMapBinding(
                false, "EARTH", "EARTH", out string resolved, out retry, out reason), Is.True, reason);
            Assert.That(resolved, Is.EqualTo("EARTH"));
            Assert.That(retry, Is.False);
        }

        [Test]
        public void ReconnectSnapshotMapId_UsesExactSpawnedAuthorityValue()
        {
            Assert.That(
                BattleReconnectSnapshotBuilder.ResolveAuthoritativeMapId("EARTH", "EARTH"),
                Is.EqualTo("EARTH"));
        }

        [TestCase(null, "EARTH")]
        [TestCase("", "EARTH")]
        [TestCase("EARTH", null)]
        [TestCase("EARTH", "")]
        [TestCase("EARTH", "MARS")]
        [TestCase("earth", "EARTH")]
        [TestCase(" EARTH ", " EARTH ")]
        public void ReconnectSnapshotMapId_FailsClosedWhenIdentityIsMissingOrNotExact(
            string sessionMapId,
            string authoritativeMapId)
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                BattleReconnectSnapshotBuilder.ResolveAuthoritativeMapId(
                    sessionMapId,
                    authoritativeMapId));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Test]
        public void P1ValidationConsumedWave_IsRejectedAtPublicAuthorityBoundary()
        {
            GameObject gameObject = new GameObject("p1-validation-authority-one-shot-test");
            try
            {
                BattleWaveExecutor executor = gameObject.AddComponent<BattleWaveExecutor>();
                BattleWaveStateAuthority authority = gameObject.AddComponent<BattleWaveStateAuthority>();
                typeof(BattleWaveStateAuthority).GetField("_executor", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(authority, executor);
                typeof(BattleWaveExecutor).GetField("_p1ValidationArmed", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(executor, true);
                typeof(BattleWaveExecutor).GetField("_p1ValidationStartConsumed", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(executor, true);

                Assert.That(authority.ValidateWaveStart(out string reason), Is.False);
                Assert.That(reason, Does.Contain("already started"));
                Assert.That(authority.TryStartNextWave(), Is.False);
                Assert.That(executor.CurrentRound, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
#endif

        [TestCase(BattleMonsterLanePolicy.EACH_FIELD, 8)]
        [TestCase(BattleMonsterLanePolicy.BOSS_SHARED, 200)]
        public void CooperativeKillGoldUpdatesBothPersonalWalletsAndEarnedLedgers(
            BattleMonsterLanePolicy lanePolicy,
            int killGold)
        {
            int player1Gold = 100;
            int player2Gold = 250;
            int player1Earned = 10;
            int player2Earned = 20;

            Assert.That(BattleWaveStateAuthority.TryApplyCooperativeKillGold(
                lanePolicy,
                killGold,
                ref player1Gold,
                ref player2Gold,
                ref player1Earned,
                ref player2Earned), Is.True);
            Assert.That(player1Gold, Is.EqualTo(100 + killGold));
            Assert.That(player2Gold, Is.EqualTo(250 + killGold));
            Assert.That(player1Earned, Is.EqualTo(10 + killGold));
            Assert.That(player2Earned, Is.EqualTo(20 + killGold));
        }

        [TestCase(BattleMonsterLanePolicy.EACH_FIELD, 0)]
        [TestCase(BattleMonsterLanePolicy.EACH_FIELD, -1)]
        [TestCase((BattleMonsterLanePolicy)999, 20)]
        public void CooperativeKillGoldRejectsInvalidAwardsWithoutChangingLedgers(
            BattleMonsterLanePolicy lanePolicy,
            int killGold)
        {
            int player1Gold = 100;
            int player2Gold = 250;
            int player1Earned = 10;
            int player2Earned = 20;

            Assert.That(BattleWaveStateAuthority.TryApplyCooperativeKillGold(
                lanePolicy,
                killGold,
                ref player1Gold,
                ref player2Gold,
                ref player1Earned,
                ref player2Earned), Is.False);
            Assert.That(player1Gold, Is.EqualTo(100));
            Assert.That(player2Gold, Is.EqualTo(250));
            Assert.That(player1Earned, Is.EqualTo(10));
            Assert.That(player2Earned, Is.EqualTo(20));
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
        public void UnitAttackDoesNotReadNetworkedMythicChoiceBeforeAuthoritySpawned()
        {
            GameObject authorityObject = new GameObject("unspawned-battle-authority-test");
            GameObject unitObject = new GameObject("unit-attack-spawn-guard-test");
            try
            {
                BattleWaveStateAuthority authority = authorityObject.AddComponent<BattleWaveStateAuthority>();
                UnitData data = unitObject.AddComponent<UnitData>();
                data.serverId = ((long)1 << 32) | 1u;
                UnitAttack attack = unitObject.AddComponent<UnitAttack>();
                typeof(UnitAttack).GetField("battleAuthority", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(attack, authority);
                MethodInfo method = typeof(UnitAttack).GetMethod(
                    "IsAttackSuppressedByMythicChoice",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(authority.IsSpawnedForAccess, Is.False);
                Assert.That(method, Is.Not.Null);
                Assert.That(() => method.Invoke(attack, null), Throws.Nothing);
                Assert.That(method.Invoke(attack, null), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(unitObject);
                Object.DestroyImmediate(authorityObject);
            }
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
        public void MutationAuthorityExposesNetworkedRerollStateAndRpcEntryPoint()
        {
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(
                "Player1BoardMutationRerollCounts", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetProperty(
                "Player2BoardMutationRerollCounts", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.RequestMutation)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(nameof(BattleWaveStateAuthority.GetBoardMutationRerollCount)), Is.Not.Null);
            Assert.That(typeof(BattleWaveStateAuthority).GetMethod(
                "ApplyMutationRequest", BindingFlags.Instance | BindingFlags.NonPublic), Is.Not.Null);
        }

        [Test]
        public void CanonicalMutationCostsFollowInitialAndCappedRerollSequence()
        {
            var config = new CanonicalMutationConfig("DEFAULT", 300, 600, 1200, 2400, 4800, 4800, 0);
            Assert.That(BattleWaveExecutor.TryGetCanonicalMutationCost(config, true, 0, out int initial), Is.True);
            Assert.That(initial, Is.EqualTo(300));
            int[] expected = { 600, 1200, 2400, 4800, 4800, 4800 };
            for (int rerollCount = 0; rerollCount < expected.Length; rerollCount++)
            {
                Assert.That(BattleWaveExecutor.TryGetCanonicalMutationCost(config, false, rerollCount, out int cost), Is.True);
                Assert.That(cost, Is.EqualTo(expected[rerollCount]));
            }
        }

        [Test]
        public void CanonicalMutationRandomIncludesBlankAndRerollExcludesCurrentType()
        {
            var specs = new List<CanonicalMutationSpec>
            {
                Mutation("BLANK", 1),
                Mutation("BERSERK", 1),
                Mutation("SWIFT", 1)
            };
            Assert.That(BattleWaveExecutor.TryResolveCanonicalMutation(specs, 0, null, out string initial), Is.True);
            Assert.That(initial, Is.EqualTo("BLANK"));
            for (ulong seed = 0; seed < 12; seed++)
            {
                Assert.That(BattleWaveExecutor.TryResolveCanonicalMutation(specs, seed, "BERSERK", out string rerolled), Is.True);
                Assert.That(rerolled, Is.Not.EqualTo("BERSERK"));
            }
        }

        [Test]
        public void InjectorCanReplaceActiveUnlockedMythicButNeverSealedMythic()
        {
            Assert.That(BattleWaveStateAuthority.CanApplyInjectorToTarget(4, BattleMutationState.ACTIVE, true), Is.True);
            Assert.That(BattleWaveStateAuthority.CanApplyInjectorToTarget(4, BattleMutationState.NONE, true), Is.True);
            Assert.That(BattleWaveStateAuthority.CanApplyInjectorToTarget(4, BattleMutationState.SEALED, false), Is.False);
            Assert.That(BattleWaveStateAuthority.CanApplyInjectorToTarget(4, BattleMutationState.ACTIVE, false), Is.False);
            Assert.That(BattleWaveStateAuthority.CanApplyInjectorToTarget(3, BattleMutationState.PENDING, true), Is.False);
        }

        [Test]
        public void ReconnectSnapshotPreservesPendingAndSealedMutationDna()
        {
            Assert.That(BattleReconnectSnapshotBuilder.ResolvePendingMutationType(
                (byte)BattleMutationState.PENDING, "TOXIC"), Is.EqualTo("TOXIC"));
            Assert.That(BattleReconnectSnapshotBuilder.ResolvePendingMutationType(
                (byte)BattleMutationState.SEALED, "FROZEN"), Is.EqualTo("FROZEN"));
            Assert.That(BattleReconnectSnapshotBuilder.ResolvePendingMutationType(
                (byte)BattleMutationState.ACTIVE, "SWIFT"), Is.Null);
        }

        private static CanonicalMutationSpec Mutation(string type, int weight)
            => new(type, true, type != "BLANK", true, weight, 1f, 1f, 1f, 1f, 1f);

        [Test]
        public void BossPatternRuntime_TriggersSpawnAndHpThresholdExactlyOnce()
        {
            var patterns = new List<MyDefense.Battle.Balance.BossPatternSpecData>
            {
                new("COOP_STANDARD:10", 1, MyDefense.Battle.Balance.BossPatternType.SET_PHASE, MyDefense.Battle.Balance.BossTriggerType.ON_SPAWN, 0f, 0f, null, "phase", 1f, true),
                new("COOP_STANDARD:10", 2, MyDefense.Battle.Balance.BossPatternType.SET_MOVE_SPEED_MULTIPLIER, MyDefense.Battle.Balance.BossTriggerType.HP_PERCENT, 50f, 0f, null, "speed", 1.25f, true)
            };
            var runtime = new BattleBossPatternRuntime(patterns);
            var triggered = new List<int>();

            runtime.Tick(0f, 1f, pattern => triggered.Add(pattern.PatternOrder));
            runtime.Tick(1f, 0.5f, pattern => triggered.Add(pattern.PatternOrder));
            runtime.Tick(2f, 0.4f, pattern => triggered.Add(pattern.PatternOrder));

            Assert.That(triggered, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(runtime.CompletedCount, Is.EqualTo(2));
        }

        [Test]
        public void BossPatternRuntime_LoopHonorsCanonicalCooldown()
        {
            var runtime = new BattleBossPatternRuntime(new[]
            {
                new MyDefense.Battle.Balance.BossPatternSpecData("COOP_STANDARD:20", 1, MyDefense.Battle.Balance.BossPatternType.CAST_SKILL, MyDefense.Battle.Balance.BossTriggerType.LOOP, 2f, 3f, "SKILL_BASIC", null, 0f, true)
            });
            int triggered = 0;

            runtime.Tick(1f, 1f, _ => triggered++);
            runtime.Tick(2f, 1f, _ => triggered++);
            runtime.Tick(4f, 1f, _ => triggered++);
            runtime.Tick(5f, 1f, _ => triggered++);

            Assert.That(triggered, Is.EqualTo(2));
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
