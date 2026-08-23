using System;
using UnityEngine;
using Fusion;
using MyDefense.Battle;

namespace MyDefense.Battle.Runtime
{
    /// <summary>
    /// Bridges the Fusion session lifecycle to the scene-local wave executor.
    /// The adapter does not create a runner; it only applies an already-created
    /// session and roster to the battle scene.
    /// </summary>
    public sealed class BattleSceneSessionAdapter : MonoBehaviour
    {
        [SerializeField] private BattleRunnerLifecycle _runnerLifecycle;
        [SerializeField] private BattleWaveExecutor _waveExecutor;
        [SerializeField] private BattleWaveStateAuthority _stateAuthority;
        [SerializeField] private PathManager _pathManager;

        public BattleSessionContext SessionContext { get; private set; }
        public bool IsInitialized { get; private set; }

        public BattleRunnerLifecycle RunnerLifecycle => _runnerLifecycle;
        public BattleWaveExecutor WaveExecutor => _waveExecutor;

        public bool TryCaptureReconnectSnapshot(out MyDefense.Shared.Contracts.BattleSessionSnapshot snapshot)
        {
            snapshot = null;
            if (!IsInitialized || SessionContext == null || _stateAuthority == null
                || !_stateAuthority.IsAuthoritative || _waveExecutor == null)
                return false;
            snapshot = BattleReconnectSnapshotBuilder.Capture(SessionContext, _stateAuthority, _waveExecutor);
            return true;
        }

        private void OnEnable()
        {
            if (_runnerLifecycle != null)
            {
                _runnerLifecycle.SessionContextCreated += OnSessionContextCreated;
                _runnerLifecycle.PlayerRoster.PlayersChanged += OnPlayersChanged;
                _runnerLifecycle.PlayerConnected += OnPlayerConnected;
                _runnerLifecycle.PlayerDisconnected += OnPlayerDisconnected;
            }
        }

        private void OnDisable()
        {
            if (_runnerLifecycle != null)
            {
                _runnerLifecycle.SessionContextCreated -= OnSessionContextCreated;
                _runnerLifecycle.PlayerRoster.PlayersChanged -= OnPlayersChanged;
                _runnerLifecycle.PlayerConnected -= OnPlayerConnected;
                _runnerLifecycle.PlayerDisconnected -= OnPlayerDisconnected;
            }
            ResetAdapter();
        }

        private void OnSessionContextCreated(BattleSessionContext _)
        {
            TryInitializeFromRunner();
        }

        private void OnPlayersChanged()
        {
            TryInitializeFromRunner();
        }

        private void OnPlayerConnected(BattlePlayerIdentity identity)
        {
            if (_stateAuthority != null && _stateAuthority.IsAuthoritative)
                _stateAuthority.SetPlayerConnectionState(identity.PlayerSlot, identity.UserId, identity.PlayerRef, true);
            TryInitializeFromRunner();
        }

        private void OnPlayerDisconnected(BattlePlayerIdentity identity)
        {
            if (_stateAuthority != null && _stateAuthority.IsAuthoritative)
                _stateAuthority.SetPlayerConnectionState(identity.PlayerSlot, identity.UserId, identity.PlayerRef, false);
        }

        private void Update()
        {
            if (!IsInitialized)
                TryInitializeFromRunner();
        }

        public bool TryInitializeFromRunner()
        {
            _runnerLifecycle ??= FindFirstObjectByType<BattleRunnerLifecycle>();
            _waveExecutor ??= FindFirstObjectByType<BattleWaveExecutor>();
            _stateAuthority ??= FindFirstObjectByType<BattleWaveStateAuthority>();
            if (_runnerLifecycle == null || _runnerLifecycle.SessionContext == null)
            {
                if (!TryCreateSessionContext())
                    return false;
            }

            if (_runnerLifecycle.SessionContext == null)
                return false;

            // CreateSessionContext raises SessionContextCreated synchronously.
            // The callback may have completed initialization before this call
            // resumes, so do not initialize the same session a second time.
            if (IsInitialized
                && ReferenceEquals(SessionContext, _runnerLifecycle.SessionContext)
                && (_stateAuthority == null || _stateAuthority.IsSpawnedForAccess))
                return true;

            BattlePlayerRoster roster = _runnerLifecycle.PlayerRoster;
            if (roster == null || _runnerLifecycle.Runner == null)
                return false;

            if (!roster.TryGet(_runnerLifecycle.Runner.LocalPlayer, out BattlePlayerIdentity localIdentity))
                return false;

            // A client may not know the remote user's account ID yet. Bind its
            // local lane immediately; full authoritative initialization waits
            // until the host has both identities.
            if (_waveExecutor != null)
                _waveExecutor.SetLocalPlayerLane(
                    localIdentity.PlayerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane);

            string player1Id;
            string player2Id;
            if (roster.TryGetByUserIdForSlot(1, out BattlePlayerIdentity player1)
                && roster.TryGetByUserIdForSlot(2, out BattlePlayerIdentity player2))
            {
                player1Id = player1.UserId;
                player2Id = player2.UserId;
            }
            else if (_stateAuthority != null
                && _stateAuthority.IsSpawnedForAccess
                && !string.IsNullOrWhiteSpace(_stateAuthority.Player1UserId.ToString())
                && !string.IsNullOrWhiteSpace(_stateAuthority.Player2UserId.ToString()))
            {
                player1Id = _stateAuthority.Player1UserId.ToString();
                player2Id = _stateAuthority.Player2UserId.ToString();
            }
            else
            {
                return false;
            }

            return Initialize(
                _runnerLifecycle.SessionContext,
                new BattlePlayerIdentityMap(player1Id, player2Id),
                localIdentity.PlayerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane);
        }

        private bool TryCreateSessionContext()
        {
            if (_runnerLifecycle == null
                || _runnerLifecycle.SessionContext != null
                || _runnerLifecycle.Runner == null
                || !_runnerLifecycle.Runner.IsRunning
                || _waveExecutor == null)
                return false;

            if (!_waveExecutor.TryGetCanonicalSessionMetadata(
                    out string canonicalBalanceVersion,
                    out string canonicalContentHash,
                    out string battleContentVersion,
                    out string battleContentHash))
                return false;

            try
            {
                _runnerLifecycle.CreateSessionContext(
                    canonicalBalanceVersion,
                    canonicalContentHash,
                    battleContentVersion,
                    battleContentHash,
                    _runnerLifecycle.Runner.Tick.Raw);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public bool Initialize(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider,
            LaneType localPlayerLane)
        {
            if (sessionContext == null)
                throw new ArgumentNullException(nameof(sessionContext));
            if (playerIdentityProvider == null)
                throw new ArgumentNullException(nameof(playerIdentityProvider));
            if (_waveExecutor == null)
                return false;
            if (localPlayerLane != LaneType.Player1Lane && localPlayerLane != LaneType.Player2Lane)
                return false;

            if (_stateAuthority != null)
            {
                if (_stateAuthority.IsAuthoritative)
                {
                    if (!_stateAuthority.InitializeSession(sessionContext, playerIdentityProvider))
                        return false;
                }
                else
                {
                    // The State Authority owns the replicated wave state, but
                    // every peer still needs the same local session metadata
                    // for presentation, validation and late callback binding.
                    // Do not ask a client to initialize Networked properties;
                    // only bind its scene-local executor context.
                    _waveExecutor.InitializeSession(sessionContext, playerIdentityProvider);
                }
            }
            else
            {
                _waveExecutor.InitializeSession(sessionContext, playerIdentityProvider);
            }

            _pathManager?.InitializePaths();
            _waveExecutor.SetLocalPlayerLane(localPlayerLane);
            EnsureSettlementCoordinator(sessionContext);
            SessionContext = sessionContext;
            IsInitialized = true;
            if (_stateAuthority == null || _stateAuthority.IsAuthoritative)
                _waveExecutor.StartConfiguredWavesIfReady();
            return true;
        }

        public void ResetAdapter()
        {
            SessionContext = null;
            IsInitialized = false;
        }

        private void EnsureSettlementCoordinator(BattleSessionContext sessionContext)
        {
            if (_runnerLifecycle == null || _waveExecutor == null || _stateAuthority == null)
                return;
            BattleSettlementCoordinator coordinator = GetComponent<BattleSettlementCoordinator>();
            if (coordinator == null)
                coordinator = gameObject.AddComponent<BattleSettlementCoordinator>();
            coordinator.Configure(_runnerLifecycle, _waveExecutor, _stateAuthority, sessionContext);
        }
    }
}
