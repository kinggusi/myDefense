using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace MyDefense.Battle.Tests
{
    public sealed class DailyCultivationBattleTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject value in _objects)
                if (value != null) UnityEngine.Object.DestroyImmediate(value);
            _objects.Clear();
        }

        [TestCase(1, 3, 120)]
        [TestCase(2, 4, 150)]
        [TestCase(3, 5, 180)]
        [TestCase(4, 6, 210)]
        [TestCase(5, 7, 240)]
        public void CultivationPlan_UsesApprovedStageWaveAndTimeContract(
            int stage,
            int waveCount,
            int timeLimit)
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = Context(provider, stage);

            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildCultivation(
                context,
                provider,
                DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan,
                out string error), Is.True, error);
            Assert.That(plan.Waves, Has.Count.EqualTo(waveCount));
            Assert.That(plan.TimeLimitSeconds, Is.EqualTo(timeLimit));
            Assert.That(plan.Waves.Select(wave => wave.Wave),
                Is.EqualTo(Enumerable.Range(1, waveCount)));
            Assert.That(plan.Waves.All(wave => wave.SpawnCount > 0
                && wave.SpawnIntervalSeconds > 0f
                && wave.HpMultiplier > 0f
                && wave.MoveSpeedMultiplier > 0f), Is.True);
        }

        [Test]
        public void CultivationPlan_RejectsUntrustedAndMismatchedCanonicalContext()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = Context(provider, 1);

            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildCultivation(
                context, provider, DailyBattleSessionTrust.Untrusted, out _, out string untrusted), Is.False);
            Assert.That(untrusted, Does.Contain("trusted"));

            context.contentHash = "wrong";
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildCultivation(
                context, provider, DailyBattleSessionTrust.DevelopmentFixture, out _, out string mismatch), Is.False);
            Assert.That(mismatch, Does.Contain("version/hash"));
        }

        [Test]
        public void CultivationPlan_RejectsMutationLabAtP222Boundary()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = Context(provider, 1);
            context.contentType = "MUTATION_LAB";
            context.mapId = "DAILY_MUTATION_LAB";

            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildCultivation(
                context, provider, DailyBattleSessionTrust.DevelopmentFixture, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("Cultivation Zone"));
        }

        [Test]
        public void DevelopmentProfile_RequiresExactStageAndCreatesCanonicalContext()
        {
            Assert.That(DailyBattleDevelopmentSessionProfile.Parse(
                "P22-CULT-S5-local-001", out DailyBattleDevelopmentSessionProfile profile, out string error),
                Is.EqualTo(DailyBattleDevelopmentParseState.Valid), error);
            Assert.That(profile.Stage, Is.EqualTo(5));

            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = profile.CreateContext(provider);
            Assert.That(context.battleSessionId, Is.EqualTo(profile.SessionName));
            Assert.That(context.mapId, Is.EqualTo(DailyBattleExecutionPlanBuilder.CultivationMapId));
            Assert.That(context.balanceVersion, Is.EqualTo(provider.CanonicalBalanceVersion));
            Assert.That(context.contentHash, Is.EqualTo(provider.CanonicalContentHash));

            Assert.That(DailyBattleDevelopmentSessionProfile.Parse(
                "P22-CULT-S6-invalid", out _, out _),
                Is.EqualTo(DailyBattleDevelopmentParseState.Malformed));
        }

        [TestCase("P22-CULT-S1-")]
        [TestCase("P22-CULT-S1-   ")]
        [TestCase("P22-CULT-S1-a")]
        [TestCase("P22-CULT-S1-bad token")]
        [TestCase("P22-CULT-S1--bad")]
        public void DevelopmentProfile_RejectsUnsafeOrMissingUniqueToken(string sessionName)
        {
            Assert.That(DailyBattleDevelopmentSessionProfile.Parse(
                sessionName, out _, out string error),
                Is.EqualTo(DailyBattleDevelopmentParseState.Malformed));
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void DailyIdentityMap_ResolvesPlayerOneOnly()
        {
            var identities = new DailyBattlePlayerIdentityMap("solo-player");
            Assert.That(identities.TryGetPlayerId(LaneType.Player1Lane, out string player1), Is.True);
            Assert.That(player1, Is.EqualTo("solo-player"));
            Assert.That(identities.TryGetPlayerId(LaneType.Player2Lane, out _), Is.False);
            Assert.That(identities.TryGetPlayerId(LaneType.BossSharedLane, out _), Is.False);
        }

        [Test]
        public void AuthoritativeMapCapacity_PreservesDailyCultivationMapId()
        {
            Fusion.NetworkString<Fusion._32> mapId = DailyBattleExecutionPlanBuilder.CultivationMapId;
            Assert.That(mapId.ToString(), Is.EqualTo(DailyBattleExecutionPlanBuilder.CultivationMapId));
        }

        [Test]
        public void Executor_DailyInitializationDoesNotRequireVirtualPlayerTwo()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext dailyContext = Context(provider, 1);
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildCultivation(
                dailyContext, provider, DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan, out string error), Is.True, error);

            BattleWaveExecutor executor = CreateExecutor();
            var session = new BattleSessionContext(
                dailyContext.battleSessionId,
                provider.CanonicalBalanceVersion,
                provider.CanonicalContentHash,
                provider.BattleContentVersion,
                provider.BattleContentHash,
                1,
                dailyContext.mapId);
            Assert.DoesNotThrow(() => executor.InitializeDailySession(
                session,
                new DailyBattlePlayerIdentityMap("solo-player"),
                plan));
            Assert.That(executor.IsDailyBattle, Is.True);
            Assert.That(executor.Player2AliveMonsterCount, Is.Zero);
            Assert.That(executor.DailyBattleRemainingSeconds, Is.EqualTo(120f));
        }

        [Test]
        public void Executor_RegularInitializationClearsResidualDailyModeAndTimerState()
        {
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", BuildPlan(1));
            SetField(executor, "_dailyBattleRemainingSeconds", 42f);
            var regularSession = new BattleSessionContext(
                "regular-after-daily", "canonical", "canonical-hash",
                "battle", "battle-hash", 1, "NEPTUNE");

            executor.InitializeSession(
                regularSession,
                new BattlePlayerIdentityMap("player-one", "player-two"));

            Assert.That(executor.IsDailyBattle, Is.False);
            Assert.That(executor.DailyBattleRemainingSeconds, Is.Zero);
            Assert.That(executor.AreAllPlayersEliminated, Is.False);
        }

        [Test]
        public void Executor_DailyEliminationUsesPlayerOneOnly()
        {
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", BuildPlan(1));
            SetField(executor, "_player1BattleState", PlayerBattleState.ELIMINATED);
            SetField(executor, "_player2BattleState", PlayerBattleState.ACTIVE);

            Assert.That(executor.AreAllPlayersEliminated, Is.True);
        }

        [Test]
        public void Executor_DailySpawnGateAllowsPlayerOneOnly()
        {
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", BuildPlan(1));
            MethodInfo canSpawn = typeof(BattleWaveExecutor).GetMethod(
                "CanSpawnInLane",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(canSpawn, Is.Not.Null);
            Assert.That(canSpawn.Invoke(executor, new object[] { LaneType.Player1Lane }), Is.True);
            Assert.That(canSpawn.Invoke(executor, new object[] { LaneType.Player2Lane }), Is.False);
        }

        [Test]
        public void BoardView_SoloModeDisablesPlayerTwoBoard()
        {
            var remoteBoard = new GameObject("EnemyGridParent");
            _objects.Add(remoteBoard);
            var viewObject = new GameObject("DailyBoardView");
            _objects.Add(viewObject);
            FusionKidnapBoardView view = viewObject.AddComponent<FusionKidnapBoardView>();

            view.SetSoloPlayerOneMode(true);

            Assert.That(remoteBoard.activeSelf, Is.False);
        }

        [Test]
        public void SoloPresentation_DisablesAllPlayerTwoObjectsAndRestoresRegularMode()
        {
            var targets = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (string objectName in DailyBattleSoloPresentationController.PlayerTwoOnlyObjectNames)
            {
                var target = new GameObject(objectName);
                _objects.Add(target);
                targets.Add(objectName, target);
            }
            targets["EnemyWaypointGroup"].SetActive(false);
            var controllerObject = new GameObject("DailySoloPresentation");
            _objects.Add(controllerObject);
            DailyBattleSoloPresentationController controller =
                controllerObject.AddComponent<DailyBattleSoloPresentationController>();

            Assert.That(controller.SetSoloPlayerOneMode(true, out string error), Is.True, error);
            Assert.That(targets.Values.All(target => !target.activeSelf), Is.True);
            Assert.That(controller.SetSoloPlayerOneMode(false, out error), Is.True, error);
            Assert.That(targets.Where(pair => pair.Key != "EnemyWaypointGroup")
                .All(pair => pair.Value.activeSelf), Is.True);
            Assert.That(targets["EnemyWaypointGroup"].activeSelf, Is.False);
        }

        [Test]
        public void DailyKillGold_UpdatesPlayerOneOnlyAndRejectsBossPolicy()
        {
            int player1Gold = 100;
            int player2Gold = 250;
            int player1Earned = 10;
            int player2Earned = 20;
            Assert.That(BattleWaveStateAuthority.TryApplyDailyKillGold(
                BattleMonsterLanePolicy.EACH_FIELD, 30,
                ref player1Gold, ref player2Gold, ref player1Earned, ref player2Earned), Is.True);
            Assert.That(player1Gold, Is.EqualTo(130));
            Assert.That(player1Earned, Is.EqualTo(40));
            Assert.That(player2Gold, Is.EqualTo(250));
            Assert.That(player2Earned, Is.EqualTo(20));
            Assert.That(BattleWaveStateAuthority.TryApplyDailyKillGold(
                BattleMonsterLanePolicy.BOSS_SHARED, 30,
                ref player1Gold, ref player2Gold, ref player1Earned, ref player2Earned), Is.False);
        }

        [Test]
        public void DailyWalletInitialization_ZeroesEveryPlayerTwoAndTeamLedger()
        {
            var wallet = new DailyBattleWalletInitialization(100000);
            Assert.That(wallet.Player1Current, Is.EqualTo(100000));
            Assert.That(wallet.Player1Initial, Is.EqualTo(100000));
            Assert.That(wallet.Player1Earned, Is.Zero);
            Assert.That(wallet.Player1Spent, Is.Zero);
            Assert.That(wallet.Player2Current, Is.Zero);
            Assert.That(wallet.Player2Initial, Is.Zero);
            Assert.That(wallet.Player2Earned, Is.Zero);
            Assert.That(wallet.Player2Spent, Is.Zero);
            Assert.That(wallet.TeamGold, Is.Zero);
        }

        [Test]
        public void DailyResultCoordinator_DeliversTerminalResultExactlyOnce()
        {
            BattleWaveExecutor executor = CreateExecutor();
            DailyBattleExecutionPlan plan = BuildPlan(1);
            var coordinatorObject = new GameObject("DailyResultCoordinator");
            _objects.Add(coordinatorObject);
            DailyBattleResultCoordinator coordinator = coordinatorObject.AddComponent<DailyBattleResultCoordinator>();
            var sink = new RecordingResultSink();
            Assert.That(coordinator.ConfigureForTests(
                executor, plan, sink, () => 3, out string error), Is.True, error);

            InvokeTransition(executor, MatchState.CLEARED);
            InvokeTransition(executor, MatchState.FAILED);

            Assert.That(sink.SubmitCount, Is.EqualTo(1));
            Assert.That(coordinator.IsDelivered, Is.True);
            Assert.That(sink.LastPayload.Result, Is.EqualTo("CLEARED"));
            Assert.That(sink.LastPayload.FinalWave, Is.EqualTo(3));
        }

        [Test]
        public void DailyResultCoordinator_MissingSinkKeepsTerminalResultPendingFailClosed()
        {
            BattleWaveExecutor executor = CreateExecutor();
            DailyBattleExecutionPlan plan = BuildPlan(1);
            var coordinatorObject = new GameObject("DailyPendingResultCoordinator");
            _objects.Add(coordinatorObject);
            DailyBattleResultCoordinator coordinator = coordinatorObject.AddComponent<DailyBattleResultCoordinator>();
            Assert.That(coordinator.ConfigureForTests(
                executor, plan, null, () => 1, out string error), Is.True, error);

            InvokeTransition(executor, MatchState.FAILED);

            Assert.That(coordinator.IsDelivered, Is.False);
            Assert.That(coordinator.PendingResult, Is.Not.Null);
            Assert.That(coordinator.PendingResult.Result, Is.EqualTo("FAILED"));
            Assert.That(coordinator.LastError, Does.Contain("pending"));
        }

        [Test]
        public void Executor_DailyTimeoutFailsOnlyAfterTimerExpires()
        {
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", BuildPlan(1));
            SetField(executor, "_dailyBattleRemainingSeconds", 1f);
            Assert.That(executor.TryResolveDailyBattleTimeoutFromAuthority(), Is.False);

            SetField(executor, "_dailyBattleRemainingSeconds", 0f);
            Assert.That(executor.TryResolveDailyBattleTimeoutFromAuthority(), Is.True);
            Assert.That(executor.MatchState, Is.EqualTo(MatchState.FAILED));
        }

        [Test]
        public void Executor_DailyCatalogExhaustionClearsRun()
        {
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", BuildPlan(1));

            MethodInfo report = typeof(BattleWaveExecutor).GetMethod(
                "ReportCatalogExhausted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(report, Is.Not.Null);
            report.Invoke(executor, null);

            Assert.That(executor.MatchState, Is.EqualTo(MatchState.CLEARED));
        }

        [TestCase(1, 3, 120)]
        [TestCase(2, 4, 150)]
        [TestCase(3, 5, 180)]
        [TestCase(4, 6, 210)]
        [TestCase(5, 7, 240)]
        public void MutationLabPlan_UsesCanonicalStatusAndFinalBossContract(
            int stage,
            int waveCount,
            int timeLimit)
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = MutationLabContext(provider, stage);

            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildMutationLab(
                context,
                provider,
                DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan,
                out string error), Is.True, error);
            Assert.That(plan.Waves, Has.Count.EqualTo(waveCount));
            Assert.That(plan.TimeLimitSeconds, Is.EqualTo(timeLimit));
            Assert.That(plan.Waves.Take(waveCount - 1).All(wave => !wave.Boss), Is.True);
            Assert.That(plan.Waves.Last().Boss, Is.True);
            Assert.That(plan.Waves.Last().MonsterSpecId, Is.EqualTo("WAVE_BOSS"));
            Assert.That(plan.Waves.Last().SpawnCount, Is.EqualTo(1));
            MethodInfo toRuntimeWave = typeof(DailyBattleWavePlan).GetMethod(
                "ToRuntimeWave", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo toRuntimeSpawn = typeof(DailyBattleWavePlan).GetMethod(
                "ToRuntimeSpawn", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(toRuntimeWave, Is.Not.Null);
            Assert.That(toRuntimeSpawn, Is.Not.Null);
            WaveSpecData finalRuntimeWave = (WaveSpecData)toRuntimeWave.Invoke(plan.Waves.Last(), null);
            WaveSpawnSpecData finalRuntimeSpawn = (WaveSpawnSpecData)toRuntimeSpawn.Invoke(plan.Waves.Last(), null);
            Assert.That(finalRuntimeWave.BossTimeLimitSeconds, Is.Zero);
            Assert.That(finalRuntimeSpawn.LanePolicy,
                Is.EqualTo(BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE));
            Assert.That(plan.Waves.All(wave => wave.RuntimeWaveId ==
                $"DAILY:MUTATION_LAB:S{stage}:W{wave.Wave}"), Is.True);
            Assert.That(plan.Waves.Any(wave =>
                wave.StatusEffectType != CanonicalDailyBattleStatusEffect.NONE), Is.True);
            Assert.That(plan.Waves.All(wave => wave.StatusEffectValue >= 0f
                && wave.StatusEffectValue < 1f), Is.True);
        }

        [Test]
        public void MutationLabPlan_RequiresTrustedMatchingContextAndGenericBuilderSelectsIt()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = MutationLabContext(provider, 1);

            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildMutationLab(
                context, provider, DailyBattleSessionTrust.Untrusted, out _, out string untrusted), Is.False);
            Assert.That(untrusted, Does.Contain("trusted"));
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuild(
                context, provider, DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan, out string error), Is.True, error);
            Assert.That(plan.SessionContext.contentType,
                Is.EqualTo(DailyBattleExecutionPlanBuilder.MutationLabContentType));

            context.mapId = DailyBattleExecutionPlanBuilder.CultivationMapId;
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildMutationLab(
                context, provider, DailyBattleSessionTrust.DevelopmentFixture, out _, out string mismatch), Is.False);
            Assert.That(mismatch, Does.Contain("contentType/mapId mismatch"));
        }

        [Test]
        public void MutationLabDevelopmentProfile_UsesExactPrefixAndCanonicalContext()
        {
            Assert.That(DailyBattleDevelopmentSessionProfile.Parse(
                "P22-MUT-S5-local-001", out DailyBattleDevelopmentSessionProfile profile, out string error),
                Is.EqualTo(DailyBattleDevelopmentParseState.Valid), error);
            Assert.That(profile.Stage, Is.EqualTo(5));
            Assert.That(profile.ContentType,
                Is.EqualTo(DailyBattleExecutionPlanBuilder.MutationLabContentType));
            Assert.That(profile.MapId,
                Is.EqualTo(DailyBattleExecutionPlanBuilder.MutationLabMapId));

            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            DailyBattleSessionContext context = profile.CreateContext(provider);
            Assert.That(context.battleSessionId, Is.EqualTo(profile.SessionName));
            Assert.That(context.contentType, Is.EqualTo(profile.ContentType));
            Assert.That(context.mapId, Is.EqualTo(profile.MapId));

            Assert.That(DailyBattleDevelopmentSessionProfile.Parse(
                "P22-MUT-S6-invalid", out _, out string malformed),
                Is.EqualTo(DailyBattleDevelopmentParseState.Malformed));
            Assert.That(malformed, Does.Contain("P22-MUT-S"));
        }

        [Test]
        public void MutationLabAttackDown_ScalesDirectAndDerivedDamageFromOriginalSnapshot()
        {
            AlienAttackSnapshot source = AlienAttackSnapshot.FromCalculatedStats(
                17, 120f, 2f, 8f, "TOXIC");
            source.SplashDamageMultiplier = 0.5f;
            source.DotDamagePerTick = 24f;

            AlienAttackSnapshot first = DailyBattleAttackSnapshotCalculator.Apply(
                source, CanonicalDailyBattleStatusEffect.ATTACK_DOWN, 0.25f);
            AlienAttackSnapshot recalculated = DailyBattleAttackSnapshotCalculator.Apply(
                source, CanonicalDailyBattleStatusEffect.ATTACK_DOWN, 0.25f);

            Assert.That(source.Damage, Is.EqualTo(120f));
            Assert.That(source.DotDamagePerTick, Is.EqualTo(24f));
            Assert.That(first.Damage, Is.EqualTo(90f).Within(0.001f));
            Assert.That(first.DotDamagePerTick, Is.EqualTo(18f).Within(0.001f));
            Assert.That(first.SplashDamageMultiplier, Is.EqualTo(0.5f));
            Assert.That(recalculated.Damage, Is.EqualTo(first.Damage));
            Assert.That(recalculated.DotDamagePerTick, Is.EqualTo(first.DotDamagePerTick));
        }

        [Test]
        public void MutationLabAttackSpeedDown_ScalesRateOnly()
        {
            AlienAttackSnapshot source = AlienAttackSnapshot.FromCalculatedStats(
                17, 120f, 2f, 8f, "NONE");

            AlienAttackSnapshot result = DailyBattleAttackSnapshotCalculator.Apply(
                source, CanonicalDailyBattleStatusEffect.ATTACK_SPEED_DOWN, 0.3f);

            Assert.That(result.Damage, Is.EqualTo(120f));
            Assert.That(result.AttackRate, Is.EqualTo(1.4f).Within(0.001f));
            Assert.That(result.Range, Is.EqualTo(8f));
        }

        [Test]
        public void Executor_DailyStatusAppliesOnlyWhileConfiguredWaveIsRunning()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildMutationLab(
                MutationLabContext(provider, 1), provider, DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan, out string error), Is.True, error);
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", plan);
            SetField(executor, "_currentRound", 2);
            SetField(executor, "_isWaveRunning", true);
            AlienAttackSnapshot source = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "NONE");

            Assert.That(executor.TryApplyActiveDailyBattleStatus(source, out AlienAttackSnapshot active), Is.True);
            Assert.That(active.AttackRate, Is.EqualTo(1.8f).Within(0.001f));

            SetField(executor, "_isWaveRunning", false);
            Assert.That(executor.TryApplyActiveDailyBattleStatus(source, out AlienAttackSnapshot completed), Is.False);
            Assert.That(completed.AttackRate, Is.EqualTo(source.AttackRate));

            SetField(executor, "_isWaveRunning", true);
            SetField(executor, "_matchState", MatchState.FAILED);
            Assert.That(executor.TryApplyActiveDailyBattleStatus(source, out AlienAttackSnapshot terminal), Is.False);
            Assert.That(terminal.AttackRate, Is.EqualTo(source.AttackRate));

            SetField(executor, "_dailyBattlePlan", null);
            SetField(executor, "_matchState", MatchState.RUNNING);
            Assert.That(executor.TryApplyActiveDailyBattleStatus(source, out AlienAttackSnapshot regular), Is.False);
            Assert.That(regular.AttackRate, Is.EqualTo(source.AttackRate));
        }

        [Test]
        public void DailyFinalBoss_IsTrackedAsPlayerOneRegularWaveRemainder()
        {
            Assert.That(BattleWaveExecutor.ShouldTrackDailyMonsterForWaveCompletion(
                true, LaneType.Player1Lane, false), Is.True);
            Assert.That(BattleWaveExecutor.ShouldTrackDailyMonsterForWaveCompletion(
                true, LaneType.BossSharedLane, false), Is.False);
            Assert.That(BattleWaveExecutor.ShouldTrackDailyMonsterForWaveCompletion(
                false, LaneType.Player1Lane, false), Is.False);
            Assert.That(BattleWaveExecutor.ShouldTrackDailyMonsterForWaveCompletion(
                false, LaneType.Player1Lane, true), Is.True);
        }

        [Test]
        public void DailyFinalBoss_CompletesPlayerOneWaveExactlyOnceWithoutExclusiveBossTimer()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildMutationLab(
                MutationLabContext(provider, 1), provider, DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan, out string error), Is.True, error);
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", plan);
            SetField(executor, "_currentRound", 3);
            SetField(executor, "_isWaveRunning", true);
            SetField(executor, "_regularWaveSpawnCompleted", true);
            SetField(executor, "_player1AliveMonsterCount", 1);
            SetEnumField(executor, "_bossState", "Active");
            int bossDefeated = 0;
            int waveCompleted = 0;
            executor.OnBossDefeated += () => bossDefeated++;
            executor.OnRegularWaveCompleted += _ => waveCompleted++;

            executor.RegisterMonsterKilled(LaneType.Player1Lane);
            executor.RegisterMonsterKilled(LaneType.Player1Lane);

            Assert.That(executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(executor.IsBossActive, Is.False);
            Assert.That(executor.ActiveBossTimeLimitSeconds, Is.Zero);
            Assert.That(bossDefeated, Is.EqualTo(1));
            Assert.That(waveCompleted, Is.EqualTo(1));
        }

        [Test]
        public void DailyPlayerOneLaneBoss_PreservesRuntimeAndSummaryBossSemantics()
        {
            var session = new BattleSessionContext(
                "daily-boss-summary",
                "canonical",
                "canonical-hash",
                "battle",
                "battle-hash",
                1,
                DailyBattleExecutionPlanBuilder.MutationLabMapId);
            var identity = new BattleMonsterRuntimeIdentity(
                session,
                1,
                "WAVE_BOSS",
                BattleMonsterLanePolicy.EACH_FIELD,
                "solo-player",
                3,
                1,
                isBoss: true);
            var spawn = new BattleSpawnAuditRecord(
                identity.RuntimeKey,
                3,
                "DAILY:MUTATION_LAB:S1:W3",
                "WAVE_BOSS",
                BattleMonsterLanePolicy.EACH_FIELD,
                1,
                1,
                1,
                isBoss: true);
            var kill = new BattleKillAuditRecord(
                identity.RuntimeKey,
                "WAVE_BOSS",
                "solo-player",
                "solo-player",
                BattleMonsterLanePolicy.EACH_FIELD,
                3,
                10,
                killGold: 200,
                isBoss: true);

            BattleSummary summary = BattleSummaryBuilder.Build(
                session,
                MatchState.FAILED,
                2,
                new[] { new BattlePlayerSummarySeed("solo-player", false, null, 200, 0) },
                new[] { kill },
                new[] { spawn });

            Assert.That(identity.LanePolicy, Is.EqualTo(BattleMonsterLanePolicy.EACH_FIELD));
            Assert.That(identity.IsBoss, Is.True);
            Assert.That(spawn.IsBoss, Is.True);
            Assert.That(kill.IsBoss, Is.True);
            Assert.That(summary.Players.Single().BossKills, Is.EqualTo(1));
            Assert.That(summary.Kills.ByMonster.Single().BossKillCount, Is.EqualTo(1));
        }

        [Test]
        public void DailyPlayerOneLaneBoss_ActivatesSharedBossPatternWithoutExclusiveTimer()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_battleBalanceProvider", provider);
            var boss = new GameObject("DailyPlayerOneLaneBoss");
            _objects.Add(boss);
            boss.AddComponent<BattleMonsterMovement>();
            int patternCount = 0;
            executor.OnBossPatternTriggered += _ => patternCount++;

            MethodInfo activate = typeof(BattleWaveExecutor).GetMethod(
                "ActivateDailyLaneBoss", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo resolve = typeof(BattleWaveExecutor).GetMethod(
                "ResolveDailyBossPatternWaveId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(activate, Is.Not.Null);
            Assert.That(resolve, Is.Not.Null);
            string patternWaveId = (string)resolve.Invoke(executor, null);
            Assert.That(patternWaveId, Is.Not.Null.And.Not.Empty);
            activate.Invoke(executor, new object[] { boss, patternWaveId });

            Assert.That(patternCount, Is.EqualTo(1));
            Assert.That(executor.IsBossActive, Is.False);
            Assert.That(executor.ActiveBossTimeLimitSeconds, Is.Zero);
            Assert.That(GetField<int>(executor, "_bossPhase"), Is.EqualTo(1));
        }

        [Test]
        public void DailyBossPatternResolution_UsesLowestCanonicalBossRoundThenOrdinalWaveId()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_battleBalanceProvider", provider);
            WaveSpecData expected = provider.Catalog.Waves.All
                .Where(wave => wave.Enabled
                    && wave.WaveType == WaveType.BOSS
                    && provider.Catalog.BossPatterns.GetByWave(wave.WaveId).Count > 0)
                .OrderBy(wave => wave.RoundNumber)
                .ThenBy(wave => wave.WaveId, StringComparer.Ordinal)
                .First();

            MethodInfo resolve = typeof(BattleWaveExecutor).GetMethod(
                "ResolveDailyBossPatternWaveId", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(resolve, Is.Not.Null);
            Assert.That(resolve.Invoke(executor, null), Is.EqualTo(expected.WaveId));
            Assert.That(resolve.Invoke(executor, null), Is.EqualTo(expected.WaveId));
        }

        [Test]
        public void DailyBossPatternMissing_FailsBeforeSpawnAuditOrAliveCountChanges()
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildMutationLab(
                MutationLabContext(provider, 1), provider, DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan, out string error), Is.True, error);
            DailyBattleWavePlan finalWave = plan.Waves.Last();
            MethodInfo toRuntimeWave = typeof(DailyBattleWavePlan).GetMethod(
                "ToRuntimeWave", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo spawnRoutine = typeof(BattleWaveExecutor).GetMethod(
                "SpawnRegularWaveRoutine", BindingFlags.Instance | BindingFlags.NonPublic);
            BattleWaveExecutor executor = CreateExecutor();
            SetField(executor, "_dailyBattlePlan", plan);
            SetField(executor, "_currentRound", finalWave.Wave);
            SetField(executor, "_currentWaveSpec", toRuntimeWave.Invoke(finalWave, null));
            // A missing provider represents a catalog with no resolvable canonical Boss pattern.
            SetField(executor, "_battleBalanceProvider", null);
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Error,
                "[BattleWaveExecutor] Daily Lane Boss requires an enabled canonical Boss pattern.");

            var routine = (System.Collections.IEnumerator)spawnRoutine.Invoke(executor, null);

            Assert.That(routine.MoveNext(), Is.False);
            Assert.That(executor.Player1AliveMonsterCount, Is.Zero);
            Assert.That(executor.Player2AliveMonsterCount, Is.Zero);
            Assert.That(executor.SpawnAuditRecords, Is.Empty);
            Assert.That(GetField<Dictionary<string, int>>(executor, "_spawnOrdinals"), Is.Empty);
            Assert.That(GetField<GameObject>(executor, "_currentBossInstance"), Is.Null);
        }

        private BattleWaveExecutor CreateExecutor()
        {
            var go = new GameObject("DailyCultivationExecutorTest");
            _objects.Add(go);
            return go.AddComponent<BattleWaveExecutor>();
        }

        private DailyBattleExecutionPlan BuildPlan(int stage)
        {
            CanonicalCompositeBattleBalanceProvider provider = LoadProvider();
            Assert.That(DailyBattleExecutionPlanBuilder.TryBuildCultivation(
                Context(provider, stage), provider, DailyBattleSessionTrust.DevelopmentFixture,
                out DailyBattleExecutionPlan plan, out string error), Is.True, error);
            return plan;
        }

        private static DailyBattleSessionContext Context(
            CanonicalCompositeBattleBalanceProvider provider,
            int stage)
        {
            return new DailyBattleSessionContext
            {
                schemaVersion = DailyBattleSessionContext.CurrentSchemaVersion,
                runId = "dev:daily-cultivation-test",
                battleSessionId = "P22-CULT-S" + stage + "-test",
                contentType = DailyBattleExecutionPlanBuilder.CultivationContentType,
                stage = stage,
                mapId = DailyBattleExecutionPlanBuilder.CultivationMapId,
                balanceVersion = provider.CanonicalBalanceVersion,
                contentHash = provider.CanonicalContentHash
            };
        }

        private static DailyBattleSessionContext MutationLabContext(
            CanonicalCompositeBattleBalanceProvider provider,
            int stage)
        {
            return new DailyBattleSessionContext
            {
                schemaVersion = DailyBattleSessionContext.CurrentSchemaVersion,
                runId = "dev:daily-mutation-lab-test",
                battleSessionId = "P22-MUT-S" + stage + "-test",
                contentType = DailyBattleExecutionPlanBuilder.MutationLabContentType,
                stage = stage,
                mapId = DailyBattleExecutionPlanBuilder.MutationLabMapId,
                balanceVersion = provider.CanonicalBalanceVersion,
                contentHash = provider.CanonicalContentHash
            };
        }

        private static CanonicalCompositeBattleBalanceProvider LoadProvider()
        {
            CanonicalBalanceLoadResult canonical = CanonicalBalanceLoader.Load(
                new StreamingAssetsCanonicalBalanceFileSource(),
                new ExistingMonsterPrefabRuntimeMapping());
            Assert.That(canonical.IsValid, Is.True, string.Join("\n", canonical.Errors));
            Assert.That(CanonicalBattleAlienIdProvider.TryCreate(
                out CanonicalBattleAlienIdProvider aliens,
                out string alienError), Is.True, alienError);
            CanonicalCompositeBattleBalanceProvider provider = CanonicalCompositeBattleBalanceProvider.Load(
                canonical,
                new ResourcesBattleBalanceTextSource(),
                aliens);
            Assert.That(provider.IsValid, Is.True, string.Join("\n", provider.ValidationErrors));
            return provider;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void SetEnumField(object target, string name, string enumValue)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, Enum.Parse(field.FieldType, enumValue));
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }

        private static void InvokeTransition(BattleWaveExecutor executor, MatchState state)
        {
            MethodInfo transition = typeof(BattleWaveExecutor).GetMethod(
                "TryTransitionMatchState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(transition, Is.Not.Null);
            transition.Invoke(executor, new object[] { state });
        }

        private sealed class RecordingResultSink : IDailyBattleResultSink
        {
            public int SubmitCount { get; private set; }
            public DailyBattleResultPayload LastPayload { get; private set; }

            public bool TrySubmit(DailyBattleResultPayload payload, out string error)
            {
                SubmitCount++;
                LastPayload = payload;
                error = null;
                return true;
            }
        }
    }
}
