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

        private void OnEnable()
        {
            if (_runnerLifecycle != null)
            {
                _runnerLifecycle.SessionContextCreated += OnSessionContextCreated;
                _runnerLifecycle.PlayerRoster.PlayersChanged += OnPlayersChanged;
            }
        }

        private void OnDisable()
        {
            if (_runnerLifecycle != null)
            {
                _runnerLifecycle.SessionContextCreated -= OnSessionContextCreated;
                _runnerLifecycle.PlayerRoster.PlayersChanged -= OnPlayersChanged;
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

        private void Update()
        {
            if (!IsInitialized)
                TryInitializeFromRunner();
        }

        public bool TryInitializeFromRunner()
        {
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
            if (IsInitialized && ReferenceEquals(SessionContext, _runnerLifecycle.SessionContext))
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

            if (roster.Count != 2)
                return false;

            if (!roster.TryGetByUserIdForSlot(1, out BattlePlayerIdentity player1)
                || !roster.TryGetByUserIdForSlot(2, out BattlePlayerIdentity player2))
                return false;

            return Initialize(
                _runnerLifecycle.SessionContext,
                new BattlePlayerIdentityMap(player1.UserId, player2.UserId),
                localIdentity.PlayerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane);
        }

        private bool TryCreateSessionContext()
        {
            if (_runnerLifecycle == null
                || _runnerLifecycle.SessionContext != null
                || _runnerLifecycle.Runner == null
                || !_runnerLifecycle.Runner.IsRunning
                || _waveExecutor == null
                || _runnerLifecycle.PlayerRoster.Count != 2)
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
                if (!_stateAuthority.IsAuthoritative)
                    return false;
                if (!_stateAuthority.InitializeSession(sessionContext, playerIdentityProvider))
                    return false;
            }
            else
            {
                _waveExecutor.InitializeSession(sessionContext, playerIdentityProvider);
            }

            _pathManager?.InitializePaths();
            _waveExecutor.SetLocalPlayerLane(localPlayerLane);
            SessionContext = sessionContext;
            IsInitialized = true;
            return true;
        }

        public void ResetAdapter()
        {
            SessionContext = null;
            IsInitialized = false;
        }
    }
}
