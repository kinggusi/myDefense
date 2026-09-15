using System;
using MyDefense.Shared.Contracts;
using UnityEngine;

namespace MyDefense.Battle.Runtime
{
    public sealed class DailyBattleResultPayload
    {
        public string RunId { get; }
        public string BattleSessionId { get; }
        public string ContentType { get; }
        public int Stage { get; }
        public string MapId { get; }
        public string Result { get; }
        public int FinalWave { get; }

        public DailyBattleResultPayload(DailyBattleSessionContext context, string result, int finalWave)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (result != "CLEARED" && result != "FAILED")
                throw new ArgumentException("Daily Battle result must be CLEARED or FAILED.", nameof(result));
            if (finalWave < 0) throw new ArgumentOutOfRangeException(nameof(finalWave));
            RunId = context.runId;
            BattleSessionId = context.battleSessionId;
            ContentType = context.contentType;
            Stage = context.stage;
            MapId = context.mapId;
            Result = result;
            FinalWave = finalWave;
        }
    }

    /// <summary>
    /// Battle-owned injection point. The User/System adapter must make RunId
    /// submission idempotent and return true only after the trusted result was accepted.
    /// </summary>
    public interface IDailyBattleResultSink
    {
        bool TrySubmit(DailyBattleResultPayload payload, out string error);
    }

    public sealed class DailyBattleResultCoordinator : MonoBehaviour
    {
        private BattleWaveExecutor _executor;
        private DailyBattleExecutionPlan _plan;
        private IDailyBattleResultSink _sink;
        private Func<int> _highestClearedWave;
        private bool _terminalCaptured;

        public DailyBattleResultPayload PendingResult { get; private set; }
        public bool IsDelivered { get; private set; }
        public string LastError { get; private set; }

        public bool ConfigureForStateAuthority(
            BattleWaveExecutor executor,
            BattleWaveStateAuthority authority,
            DailyBattleExecutionPlan plan,
            IDailyBattleResultSink sink,
            out string error)
        {
            if (authority == null || !authority.IsAuthoritative)
            {
                error = "Daily result coordinator requires Fusion State Authority.";
                return false;
            }
            return Configure(executor, plan, sink, () => authority.HighestClearedWave, out error);
        }

#if UNITY_EDITOR
        public bool ConfigureForTests(
            BattleWaveExecutor executor,
            DailyBattleExecutionPlan plan,
            IDailyBattleResultSink sink,
            Func<int> highestClearedWave,
            out string error)
        {
            return Configure(executor, plan, sink, highestClearedWave, out error);
        }
#endif

        public bool TryFlushPending()
        {
            if (IsDelivered) return true;
            if (PendingResult == null)
            {
                LastError = "No terminal Daily Battle result is pending.";
                return false;
            }
            if (_sink == null)
            {
                LastError = "Daily Battle result sink is unavailable; result remains pending (fail-closed).";
                return false;
            }
            if (!_sink.TrySubmit(PendingResult, out string error))
            {
                LastError = string.IsNullOrWhiteSpace(error) ? "Daily Battle result submission failed." : error;
                return false;
            }
            IsDelivered = true;
            LastError = null;
            return true;
        }

        public void ResetCoordinator()
        {
            Unsubscribe();
            _plan = null;
            _sink = null;
            _highestClearedWave = null;
            _terminalCaptured = false;
            PendingResult = null;
            IsDelivered = false;
            LastError = null;
        }

        private bool Configure(
            BattleWaveExecutor executor,
            DailyBattleExecutionPlan plan,
            IDailyBattleResultSink sink,
            Func<int> highestClearedWave,
            out string error)
        {
            error = null;
            if (executor == null || plan == null || highestClearedWave == null)
            {
                error = "Daily result coordinator requires executor, plan and authoritative Wave source.";
                return false;
            }
            Unsubscribe();
            _executor = executor;
            _plan = plan;
            _sink = sink;
            _highestClearedWave = highestClearedWave;
            _terminalCaptured = false;
            PendingResult = null;
            IsDelivered = false;
            LastError = null;
            _executor.OnMatchStateChanged += HandleMatchStateChanged;
            return true;
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (_terminalCaptured || (state != MatchState.CLEARED && state != MatchState.FAILED))
                return;
            _terminalCaptured = true;
            PendingResult = new DailyBattleResultPayload(
                _plan.SessionContext,
                state == MatchState.CLEARED ? "CLEARED" : "FAILED",
                Math.Max(0, _highestClearedWave()));
            if (!TryFlushPending())
                Debug.LogWarning("[DailyBattle] " + LastError);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_executor != null)
                _executor.OnMatchStateChanged -= HandleMatchStateChanged;
            _executor = null;
        }
    }
}
