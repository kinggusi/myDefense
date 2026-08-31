using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using MyDefense.Shared.Contracts;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Sends one authoritative terminal Battle summary to Spring. This
    /// coordinator is created by BattleSceneSessionAdapter at runtime so the
    /// production Battle scene does not need a hand-authored YAML reference.
    /// </summary>
    public sealed class BattleSettlementCoordinator : MonoBehaviour
    {
        private const string SettlementPath = "/battle/settlements";

        private BattleRunnerLifecycle _runnerLifecycle;
        private BattleWaveExecutor _waveExecutor;
        private BattleWaveStateAuthority _stateAuthority;
        private BattleSessionContext _session;
        private IBattleSessionRosterRegistration _rosterRegistration;
        private DateTime _startedAtUtc;
        private bool _configured;
        private bool _submitted;
        private bool _requestInFlight;
        private BattleSettlementSummary _pendingSummary;

        public bool IsConfigured => _configured;
        public bool IsSubmitted => _submitted;
        public bool HasPendingSettlement => _pendingSummary != null;
        public string LastError { get; private set; }

        public void Configure(
            BattleRunnerLifecycle runnerLifecycle,
            BattleWaveExecutor waveExecutor,
            BattleWaveStateAuthority stateAuthority,
            BattleSessionContext session,
            IBattleSessionRosterRegistration rosterRegistration = null)
        {
            if (runnerLifecycle == null) throw new ArgumentNullException(nameof(runnerLifecycle));
            if (waveExecutor == null) throw new ArgumentNullException(nameof(waveExecutor));
            if (stateAuthority == null) throw new ArgumentNullException(nameof(stateAuthority));
            if (session == null) throw new ArgumentNullException(nameof(session));

            if (_configured && ReferenceEquals(_session, session))
                return;

            Unsubscribe();
            _runnerLifecycle = runnerLifecycle;
            _waveExecutor = waveExecutor;
            _stateAuthority = stateAuthority;
            _session = session;
            _rosterRegistration = rosterRegistration;
            _startedAtUtc = DateTime.UtcNow;
            _submitted = false;
            _requestInFlight = false;
            _pendingSummary = null;
            LastError = null;
            _waveExecutor.OnMatchStateChanged += HandleMatchStateChanged;
            _configured = true;
        }

        private void OnDisable()
        {
            Unsubscribe();
            _configured = false;
        }

        private void Unsubscribe()
        {
            if (_waveExecutor != null)
                _waveExecutor.OnMatchStateChanged -= HandleMatchStateChanged;
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (state != MatchState.CLEARED && state != MatchState.FAILED)
                return;
            SubmitTerminalSummary(state, DateTime.UtcNow);
        }

        public void SubmitTerminalSummary(MatchState state, DateTime finishedAtUtc)
        {
            if (!_configured || _submitted || _requestInFlight || !_stateAuthority.IsAuthoritative)
                return;
            if (state != MatchState.CLEARED && state != MatchState.FAILED)
                return;

            if (_pendingSummary == null && !TryBuildSummary(state, finishedAtUtc, out _pendingSummary))
                return;
            SendPendingSettlement();
        }

        /// <summary>
        /// Retries a failed request with the exact same requestId and summary
        /// payload. The server can therefore safely return alreadyProcessed.
        /// No automatic retry loop is started by this component.
        /// </summary>
        public bool RetryPendingSettlement()
        {
            if (!_configured || _submitted || _requestInFlight || _pendingSummary == null || !_stateAuthority.IsAuthoritative)
                return false;
            SendPendingSettlement();
            return true;
        }

        private void SendPendingSettlement()
        {
            if (_rosterRegistration == null || !_rosterRegistration.IsRegistered)
            {
                _requestInFlight = false;
                LastError = _rosterRegistration?.LastError ?? "Trusted Battle roster is not registered.";
                Debug.LogError("[BattleSettlement] " + LastError);
                return;
            }
            if (_pendingSummary == null || NetworkManager.Instance == null)
            {
                _requestInFlight = false;
                LastError = "NetworkManager is not available for Battle Settlement.";
                Debug.LogError("[BattleSettlement] " + LastError);
                return;
            }

            BattleSettlementSummary summary = _pendingSummary;
            _requestInFlight = true;
            NetworkManager.Instance.PostJson(
                SettlementPath,
                BattleSettlementSummaryJson.Serialize(summary),
                response => HandleResponse(summary, response),
                error =>
                {
                    _requestInFlight = false;
                    LastError = error;
                    Debug.LogError($"[BattleSettlement] POST failed for session {summary.battleSessionId}: {error}");
                });
        }

        public bool TryBuildSummary(
            MatchState state,
            DateTime finishedAtUtc,
            out BattleSettlementSummary summary)
        {
            summary = null;
            if (!_configured || !_stateAuthority.IsAuthoritative)
                return false;
            if (state != MatchState.CLEARED && state != MatchState.FAILED)
                return false;

            int? player1EliminatedWave = _stateAuthority.Player1Eliminated
                ? _stateAuthority.Player1EliminatedWave
                : (int?)null;
            int? player2EliminatedWave = _stateAuthority.Player2Eliminated
                ? _stateAuthority.Player2EliminatedWave
                : (int?)null;
            if (!_stateAuthority.TryCreatePlayerSummarySeed(
                    1,
                    _stateAuthority.Player1Eliminated,
                    player1EliminatedWave,
                    out BattlePlayerSummarySeed player1)
                || !_stateAuthority.TryCreatePlayerSummarySeed(
                    2,
                    _stateAuthority.Player2Eliminated,
                    player2EliminatedWave,
                    out BattlePlayerSummarySeed player2))
            {
                LastError = "Both authoritative player identities are required for Settlement.";
                Debug.LogError("[BattleSettlement] " + LastError);
                return false;
            }

            try
            {
                BattleSummary battleSummary = BattleSummaryBuilder.Build(
                    _session,
                    state,
                    _stateAuthority.HighestClearedWave,
                    new[] { player1, player2 },
                    _stateAuthority.KillAuditRecords,
                    _waveExecutor.SpawnAuditRecords);
                string requestId = Guid.NewGuid().ToString("N");
                summary = BuildRequest(battleSummary, requestId, _startedAtUtc, finishedAtUtc);
                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                Debug.LogError($"[BattleSettlement] Summary build failed: {exception.Message}");
                return false;
            }
        }

        public static BattleSettlementSummary BuildRequest(
            BattleSummary battleSummary,
            string requestId,
            DateTime startedAtUtc,
            DateTime finishedAtUtc)
        {
            if (battleSummary == null) throw new ArgumentNullException(nameof(battleSummary));
            BattleSettlementSummary summary = BattleSettlementSummaryBuilder.Build(
                battleSummary,
                requestId,
                startedAtUtc,
                finishedAtUtc,
                "pending");
            summary.summaryHash = ComputeSummaryHash(summary);
            return summary;
        }

        public static string ComputeSummaryHash(BattleSettlementSummary summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            string canonicalJson = BattleSettlementSummaryJson.SerializeForHash(summary);
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(canonicalJson);
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private void HandleResponse(BattleSettlementSummary summary, string responseJson)
        {
            try
            {
                BattleSettlementResponse response = JsonUtility.FromJson<BattleSettlementResponse>(responseJson);
                if (response == null || !string.Equals(response.battleSessionId, summary.battleSessionId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Settlement response session does not match the submitted session.");
                if (!string.Equals(response.status, "ACCEPTED", StringComparison.Ordinal)
                    && !string.Equals(response.status, "COMPLETED", StringComparison.Ordinal))
                    throw new InvalidOperationException("Settlement response status is not accepted.");
                if (response.rewards == null)
                    response.rewards = Array.Empty<BattleSettlementReward>();
                _requestInFlight = false;
                _submitted = true;
                _pendingSummary = null;
                Debug.Log($"[BattleSettlement] accepted session={response.battleSessionId} status={response.status} alreadyProcessed={response.alreadyProcessed}");
            }
            catch (Exception exception)
            {
                _requestInFlight = false;
                LastError = "Invalid Settlement response: " + exception.Message;
                Debug.LogError("[BattleSettlement] " + LastError);
            }
        }
    }
}
