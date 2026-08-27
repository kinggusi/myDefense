using System;
using UnityEngine;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Balance.Canonical;

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
        [SerializeField] private PlanetContentApplicator _planetContentApplicator;
        private IBattleSessionRosterRegistration _rosterRegistration;
        private string _planetContentFailureKey;

        public BattleSessionContext SessionContext { get; private set; }
        public bool IsInitialized { get; private set; }

        public BattleRunnerLifecycle RunnerLifecycle => _runnerLifecycle;
        public BattleWaveExecutor WaveExecutor => _waveExecutor;
        public PlanetContentApplicator PlanetContentApplicator => _planetContentApplicator;
        public string LastInitializationError { get; private set; }

        public bool TryCaptureReconnectSnapshot(out MyDefense.Shared.Contracts.BattleSessionSnapshot snapshot)
        {
            snapshot = null;
            if (!IsInitialized || SessionContext == null || _stateAuthority == null
                || !_stateAuthority.IsAuthoritative || _waveExecutor == null)
                return false;
            snapshot = BattleReconnectSnapshotBuilder.Capture(SessionContext, _stateAuthority, _waveExecutor);
            return true;
        }

        public bool RetryRosterRegistration()
        {
            return _rosterRegistration != null && _rosterRegistration.RetryRegistration();
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
            if (_rosterRegistration != null)
                _rosterRegistration.Registered -= HandleRosterRegistered;
            _rosterRegistration = null;
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
                && ReferenceEquals(SessionContext, _runnerLifecycle.SessionContext))
            {
                bool runnerIsRunning = _runnerLifecycle.Runner != null && _runnerLifecycle.Runner.IsRunning;
                if (RequiresSpawnedAuthorityMap(
                        runnerIsRunning,
                        _stateAuthority != null,
                        _stateAuthority != null && _stateAuthority.IsSpawnedForAccess))
                {
                    LastInitializationError = "Waiting for BattleWaveStateAuthority network spawn.";
                    return false;
                }
                if (_stateAuthority == null || !_stateAuthority.IsSpawnedForAccess)
                    return true;
                if (!_stateAuthority.TryResolveMapForInitialization(
                        SessionContext.MapId,
                        out string alreadyBoundMapId,
                        out bool shouldRetryExistingMap,
                        out string existingMapReason))
                {
                    if (shouldRetryExistingMap)
                    {
                        LastInitializationError = existingMapReason;
                        return false;
                    }
                    ReportPlanetContentFailure(SessionContext, existingMapReason);
                    return false;
                }
                if (!string.Equals(alreadyBoundMapId, SessionContext.MapId, StringComparison.Ordinal))
                {
                    ReportPlanetContentFailure(
                        SessionContext,
                        "Already initialized Session mapId no longer matches authoritative mapId.");
                    return false;
                }
                return true;
            }

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
            if (RequiresSpawnedAuthorityMap(
                    _runnerLifecycle?.Runner != null && _runnerLifecycle.Runner.IsRunning,
                    _stateAuthority != null,
                    _stateAuthority != null && _stateAuthority.IsSpawnedForAccess))
            {
                LastInitializationError = "Waiting for BattleWaveStateAuthority network spawn.";
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BattleP1ValidationSessionProfile p1ValidationProfile = _runnerLifecycle?.P1ValidationProfile;
            bool isP1ValidationSession = p1ValidationProfile != null;
            if (isP1ValidationSession)
                SuppressP1ValidationSettlement();
            if (isP1ValidationSession
                && (!string.Equals(sessionContext.BattleSessionId, p1ValidationProfile.SessionName, StringComparison.Ordinal)
                    || !string.Equals(sessionContext.MapId, p1ValidationProfile.MapId, StringComparison.Ordinal)))
            {
                Debug.LogError("[P1Validation] Session context does not match the bound validation profile.");
                return false;
            }
            if (isP1ValidationSession && _stateAuthority == null)
            {
                Debug.LogError("[P1Validation] Fusion State Authority component is required.");
                return false;
            }
#endif

            _planetContentApplicator ??= GetComponent<PlanetContentApplicator>();
            _planetContentApplicator ??= gameObject.AddComponent<PlanetContentApplicator>();
            string authoritativeMapId = sessionContext.MapId;
            if (_stateAuthority != null && _stateAuthority.IsSpawnedForAccess)
            {
                if (!_stateAuthority.TryResolveMapForInitialization(
                        sessionContext.MapId,
                        out authoritativeMapId,
                        out bool shouldRetryMapBinding,
                        out string mapBindingReason))
                {
                    if (shouldRetryMapBinding)
                    {
                        LastInitializationError = mapBindingReason;
                        return false;
                    }
                    ReportPlanetContentFailure(sessionContext, mapBindingReason);
                    return false;
                }
                if (!string.Equals(authoritativeMapId, sessionContext.MapId, StringComparison.Ordinal))
                {
                    ReportPlanetContentFailure(
                        sessionContext,
                        "Resolved authoritative mapId does not match the immutable BattleSessionContext.MapId.");
                    return false;
                }
            }
            if (!_waveExecutor.TryGetCanonicalPlanetBattles(out CanonicalPlanetBattleRegistry canonicalPlanets))
            {
                ReportPlanetContentFailure(
                    sessionContext,
                    "Canonical PlanetBattle registry is unavailable or invalid.");
                return false;
            }
            if (!_planetContentApplicator.TryApply(
                    authoritativeMapId,
                    canonicalPlanets,
                    out string planetContentError))
            {
                ReportPlanetContentFailure(sessionContext, planetContentError);
                return false;
            }
            _planetContentFailureKey = null;
            LastInitializationError = null;

            if (_stateAuthority != null)
            {
                if (_stateAuthority.IsAuthoritative)
                {
                    if (!_stateAuthority.InitializeSession(sessionContext, playerIdentityProvider))
                        return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (isP1ValidationSession
                        && !_waveExecutor.TryArmP1ValidationInitialWave(p1ValidationProfile, out string reason))
                    {
                        Debug.LogError("[P1Validation] " + reason);
                        return false;
                    }
#endif
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!isP1ValidationSession)
#endif
            EnsureSettlementCoordinator(sessionContext, playerIdentityProvider);
            SessionContext = sessionContext;
            IsInitialized = true;
            if (_stateAuthority == null || _stateAuthority.IsAuthoritative)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (isP1ValidationSession)
                {
                    _waveExecutor.StartConfiguredWavesIfReady();
                }
                else
#endif
                if (_rosterRegistration == null || _rosterRegistration.IsRegistered)
                    _waveExecutor.StartConfiguredWavesIfReady();
                else
                    _rosterRegistration.EnsureRegistered();
            }
            return true;
        }

        public void ResetAdapter()
        {
            _planetContentApplicator?.Clear();
            SessionContext = null;
            IsInitialized = false;
            _planetContentFailureKey = null;
            LastInitializationError = null;
        }

        public static bool RequiresSpawnedAuthorityMap(
            bool runnerIsRunning,
            bool hasStateAuthorityComponent,
            bool stateAuthorityIsSpawned)
        {
            return runnerIsRunning && (!hasStateAuthorityComponent || !stateAuthorityIsSpawned);
        }

        private void ReportPlanetContentFailure(BattleSessionContext sessionContext, string reason)
        {
            LastInitializationError = reason;
            string key = sessionContext.BattleSessionId + "\n" + sessionContext.MapId + "\n" + reason;
            if (string.Equals(_planetContentFailureKey, key, StringComparison.Ordinal))
                return;

            _planetContentFailureKey = key;
            Debug.LogError(
                "[PlanetContent] Battle initialization failed closed for session='"
                + sessionContext.BattleSessionId + "', mapId='"
                + (sessionContext.MapId ?? "<null>") + "': " + reason);
        }

        private void EnsureSettlementCoordinator(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider)
        {
            if (_runnerLifecycle == null || _waveExecutor == null || _stateAuthority == null)
                return;
            if (_stateAuthority.IsAuthoritative)
            {
                if (_rosterRegistration != null)
                    _rosterRegistration.Registered -= HandleRosterRegistered;
                _rosterRegistration = BattleSessionRosterRegistrationFactory.ResolveOrCreate(gameObject);
                _rosterRegistration.Configure(sessionContext, playerIdentityProvider);
                _rosterRegistration.Registered -= HandleRosterRegistered;
                _rosterRegistration.Registered += HandleRosterRegistered;
            }
            BattleSettlementCoordinator coordinator = GetComponent<BattleSettlementCoordinator>();
            if (coordinator == null)
                coordinator = gameObject.AddComponent<BattleSettlementCoordinator>();
            coordinator.enabled = true;
            coordinator.Configure(
                _runnerLifecycle,
                _waveExecutor,
                _stateAuthority,
                sessionContext,
                _rosterRegistration);
        }

        private void HandleRosterRegistered()
        {
            if (_stateAuthority != null && _stateAuthority.IsAuthoritative)
                _waveExecutor?.StartConfiguredWavesIfReady();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void SuppressP1ValidationSettlement()
        {
            if (_rosterRegistration != null)
                _rosterRegistration.Registered -= HandleRosterRegistered;
            _rosterRegistration = null;
            BattleSettlementCoordinator coordinator = GetComponent<BattleSettlementCoordinator>();
            if (coordinator != null)
                coordinator.enabled = false;
            Debug.Log("[P1Validation] Settlement coordinator creation and POST are disabled for this session.");
        }
#endif
    }
}
