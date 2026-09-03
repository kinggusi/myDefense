using System;
using System.Collections.Generic;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    public enum DailyBattleSessionTrust
    {
        Untrusted = 0,
        DevelopmentFixture = 1,
        ProductionAdapter = 2
    }

    public sealed class DailyBattleWavePlan
    {
        public int Wave { get; }
        public string MonsterSpecId { get; }
        public int SpawnCount { get; }
        public float SpawnIntervalSeconds { get; }
        public float HpMultiplier { get; }
        public float MoveSpeedMultiplier { get; }

        internal DailyBattleWavePlan(CanonicalDailyBattleStage source)
        {
            Wave = source.Wave;
            MonsterSpecId = source.MonsterSpecId;
            SpawnCount = source.SpawnCount;
            SpawnIntervalSeconds = source.SpawnIntervalSeconds;
            HpMultiplier = source.HpMultiplier;
            MoveSpeedMultiplier = source.MoveSpeedMultiplier;
        }

        internal WaveSpecData ToRuntimeWave()
        {
            return new WaveSpecData(RuntimeWaveId, Wave, WaveType.REGULAR, 0f, 0f, true);
        }

        internal WaveSpawnSpecData ToRuntimeSpawn()
        {
            return new WaveSpawnSpecData(
                RuntimeWaveId,
                1,
                BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE,
                MonsterSpecId,
                SpawnCount,
                0f,
                SpawnIntervalSeconds,
                HpMultiplier,
                MoveSpeedMultiplier,
                RuntimeWaveId);
        }

        private string RuntimeWaveId => "DAILY_CULTIVATION_ZONE:" + Wave;
    }

    public sealed class DailyBattleExecutionPlan
    {
        public DailyBattleSessionContext SessionContext { get; }
        public DailyBattleSessionTrust Trust { get; }
        public int TimeLimitSeconds { get; }
        public IReadOnlyList<DailyBattleWavePlan> Waves { get; }

        internal DailyBattleExecutionPlan(
            DailyBattleSessionContext sessionContext,
            DailyBattleSessionTrust trust,
            int timeLimitSeconds,
            IEnumerable<DailyBattleWavePlan> waves)
        {
            SessionContext = sessionContext;
            Trust = trust;
            TimeLimitSeconds = timeLimitSeconds;
            Waves = Array.AsReadOnly(new List<DailyBattleWavePlan>(waves).ToArray());
        }

        public bool TryGetWaveAfter(int completedWave, out DailyBattleWavePlan wave)
        {
            wave = null;
            int index = completedWave;
            if (index < 0 || index >= Waves.Count)
                return false;
            wave = Waves[index];
            return wave.Wave == completedWave + 1;
        }
    }

    /// <summary>
    /// Converts the trusted Daily Session and canonical DailyBattleStage rows into
    /// an immutable Battle-owned execution plan. No client-authored fallback is
    /// permitted: production must supply a trusted adapter, while local validation
    /// must opt into the Development fixture boundary explicitly.
    /// </summary>
    public static class DailyBattleExecutionPlanBuilder
    {
        public const string CultivationContentType = "CULTIVATION_ZONE";
        public const string CultivationMapId = "DAILY_CULTIVATION_ZONE";

        public static bool TryBuildCultivation(
            DailyBattleSessionContext context,
            ICanonicalCompositeBattleBalanceProvider provider,
            DailyBattleSessionTrust trust,
            out DailyBattleExecutionPlan plan,
            out string error)
        {
            plan = null;
            error = null;
            if (trust == DailyBattleSessionTrust.Untrusted)
                return Fail("Daily Battle requires an explicit trusted Session source.", out error);
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (trust == DailyBattleSessionTrust.DevelopmentFixture)
                return Fail("Development Daily Battle fixture is unavailable in production builds.", out error);
#endif
            try
            {
                DailyBattleSessionContextValidator.Validate(context);
            }
            catch (Exception exception)
            {
                return Fail(exception.Message, out error);
            }

            if (!string.Equals(context.contentType, CultivationContentType, StringComparison.Ordinal)
                || !string.Equals(context.mapId, CultivationMapId, StringComparison.Ordinal))
                return Fail("Only the Cultivation Zone is accepted by the P2-2-2 execution boundary.", out error);
            if (provider == null || !provider.IsValid || provider.DailyBattleStages == null)
                return Fail("Canonical DailyBattleStage provider is unavailable or invalid.", out error);
            if (!string.Equals(context.balanceVersion, provider.CanonicalBalanceVersion, StringComparison.Ordinal)
                || !string.Equals(context.contentHash, provider.CanonicalContentHash, StringComparison.Ordinal))
                return Fail("Daily Session balance version/hash does not match the loaded canonical bundle.", out error);
            if (!provider.DailyBattleStages.TryGet(context.contentType, context.stage,
                    out IReadOnlyList<CanonicalDailyBattleStage> rows))
                return Fail("Canonical DailyBattleStage rows are missing for the requested Stage.", out error);

            int expectedWaveCount = context.stage + 2;
            int expectedTimeLimit = 90 + context.stage * 30;
            if (rows.Count != expectedWaveCount)
                return Fail("Cultivation Stage Wave count does not match the approved 3/4/5/6/7 contract.", out error);

            var waves = new List<DailyBattleWavePlan>(rows.Count);
            for (int index = 0; index < rows.Count; index++)
            {
                CanonicalDailyBattleStage row = rows[index];
                if (row == null || !row.Enabled
                    || row.Stage != context.stage
                    || row.Wave != index + 1
                    || row.TimeLimitSeconds != expectedTimeLimit
                    || !string.Equals(row.ContentType, context.contentType, StringComparison.Ordinal)
                    || !string.Equals(row.MapId, context.mapId, StringComparison.Ordinal)
                    || row.LanePolicy != CanonicalDailyBattleLanePolicy.PLAYER_ONE_ONLY
                    || row.Boss
                    || row.StatusEffectType != CanonicalDailyBattleStatusEffect.NONE
                    || Math.Abs(row.StatusEffectValue) > 0.0001f
                    || row.SpawnCount <= 0
                    || row.SpawnIntervalSeconds <= 0f
                    || row.HpMultiplier <= 0f
                    || row.MoveSpeedMultiplier <= 0f)
                    return Fail("Cultivation DailyBattleStage row violates the approved solo/no-Boss/no-status contract.", out error);
                if (provider.MonsterDefinitions == null
                    || !provider.MonsterDefinitions.TryGet(row.MonsterSpecId, out BattleMonsterDefinition definition)
                    || definition == null
                    || !string.Equals(definition.MonsterType, "NORMAL", StringComparison.Ordinal))
                    return Fail("Cultivation row must resolve an enabled NORMAL Monster.", out error);
                waves.Add(new DailyBattleWavePlan(row));
            }

            plan = new DailyBattleExecutionPlan(context, trust, expectedTimeLimit, waves);
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }

    public sealed class DailyBattlePlayerIdentityMap : IBattlePlayerIdentityProvider
    {
        private readonly string _player1Id;

        public DailyBattlePlayerIdentityMap(string player1Id)
        {
            _player1Id = BattleSessionContext.RequireText(player1Id, nameof(player1Id));
        }

        public bool TryGetPlayerId(LaneType lane, out string playerId)
        {
            playerId = lane == LaneType.Player1Lane ? _player1Id : null;
            return playerId != null;
        }
    }
}
