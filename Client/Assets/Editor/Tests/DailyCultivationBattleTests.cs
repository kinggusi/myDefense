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
