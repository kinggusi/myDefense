using System;
using UnityEngine;
using Fusion;
using MyDefense.Battle;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Shared.Contracts;

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
        [SerializeField] private DailyBattleContentCatalog _dailyBattleContentCatalog;
        private IBattleSessionRosterRegistration _rosterRegistration;
        private string _planetContentFailureKey;
        private DailyBattleSoloPresentationController _dailySoloPresentation;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string _dailyInitializationDiagnosticKey;
#endif

        public BattleSessionContext SessionContext { get; private set; }
        public bool IsInitialized { get; private set; }

        public BattleRunnerLifecycle RunnerLifecycle => _runnerLifecycle;
        public BattleWaveExecutor WaveExecutor => _waveExecutor;
        public PlanetContentApplicator PlanetContentApplicator => _planetContentApplicator;
        public DailyBattleContentCatalog DailyContentCatalog => _dailyBattleContentCatalog;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool isDailyDevelopmentSession = _runnerLifecycle?.DailyBattleDevelopmentProfile != null;
#endif
            if (_runnerLifecycle == null || _runnerLifecycle.SessionContext == null)
            {
                if (!TryCreateSessionContext())
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (isDailyDevelopmentSession)
                    {
                        string reason = _runnerLifecycle?.Runner == null
                            ? "Waiting for Fusion NetworkRunner."
                            : !_runnerLifecycle.Runner.IsRunning
                                ? "Waiting for Fusion NetworkRunner to enter Running state."
                                : _waveExecutor == null
                                    ? "BattleWaveExecutor is unavailable."
                                    : "Waiting to create Daily BattleSessionContext from canonical metadata.";
                        ReportDailyInitializationState(reason);
                    }
#endif
                    return false;
                }
            }

            if (_runnerLifecycle.SessionContext == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (isDailyDevelopmentSession)
                    ReportDailyInitializationState("Daily BattleSessionContext was not created.");
#endif
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (isDailyDevelopmentSession)
            {
                if (_stateAuthority == null)
                {
                    ReportDailyInitializationState("BattleWaveStateAuthority component is unavailable.");
                    return false;
                }
                if (!_stateAuthority.IsSpawnedForAccess)
                {
                    ReportDailyInitializationState("Waiting for BattleWaveStateAuthority NetworkObject.Spawned().");
                    return false;
                }
                if (!_stateAuthority.IsAuthoritative)
                {
                    ReportDailyInitializationState("BattleWaveStateAuthority is spawned without State Authority on the Daily Host.");
                    return false;
                }
            }
#endif

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
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (isDailyDevelopmentSession)
                    ReportDailyInitializationState("Fusion roster or NetworkRunner is unavailable.");
#endif
                return false;
            }

            if (!roster.TryGet(_runnerLifecycle.Runner.LocalPlayer, out BattlePlayerIdentity localIdentity))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (isDailyDevelopmentSession)
                    ReportDailyInitializationState("Waiting for the Daily Host local identity in the Fusion roster.");
#endif
                return false;
            }

            // A client may not know the remote user's account ID yet. Bind its
            // local lane immediately; full authoritative initialization waits
            // until the host has both identities.
            if (_waveExecutor != null)
                _waveExecutor.SetLocalPlayerLane(
                    localIdentity.PlayerSlot == 1 ? LaneType.Player1Lane : LaneType.Player2Lane);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DailyBattleDevelopmentSessionProfile dailyProfile = _runnerLifecycle.DailyBattleDevelopmentProfile;
            if (dailyProfile != null)
            {
                if (!_runnerLifecycle.Runner.IsServer || localIdentity.PlayerSlot != 1)
                {
                    FailDailyInitialization("Solo Daily Development session requires the Host in Player 1 slot.");
                    return false;
                }
                if (!_waveExecutor.TryGetCanonicalDailyBattleProvider(out ICanonicalCompositeBattleBalanceProvider provider))
                {
                    FailDailyInitialization("Canonical DailyBattleStage provider is unavailable.");
                    return false;
                }
                DailyBattleSessionContext dailyContext = dailyProfile.CreateContext(provider);
                if (!DailyBattleExecutionPlanBuilder.TryBuild(
                        dailyContext,
                        provider,
                        DailyBattleSessionTrust.DevelopmentFixture,
                        out DailyBattleExecutionPlan plan,
                        out string dailyError))
                {
                    FailDailyInitialization("Daily execution plan validation failed: " + dailyError);
                    return false;
                }
                return InitializeDaily(
                    _runnerLifecycle.SessionContext,
                    new DailyBattlePlayerIdentityMap(localIdentity.UserId),
                    plan);
            }
#endif

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
            GetComponent<DailyBattleResultCoordinator>()?.ResetCoordinator();
            if (_dailySoloPresentation != null)
                _dailySoloPresentation.SetSoloPlayerOneMode(false, out _);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _dailyInitializationDiagnosticKey = null;
#endif
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
            GetComponent<DailyBattleResultCoordinator>()?.ResetCoordinator();
            if (_dailySoloPresentation != null)
                _dailySoloPresentation.SetSoloPlayerOneMode(false, out _);
            _planetContentApplicator?.Clear();
            SessionContext = null;
            IsInitialized = false;
            _planetContentFailureKey = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _dailyInitializationDiagnosticKey = null;
#endif
            LastInitializationError = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool InitializeDaily(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider,
            DailyBattleExecutionPlan plan)
        {
            if (_waveExecutor == null)
                return FailDailyInitialization("BattleWaveExecutor is unavailable during Daily initialization.");
            if (_stateAuthority == null)
                return FailDailyInitialization("BattleWaveStateAuthority component is unavailable during Daily initialization.");
            if (!_stateAuthority.IsSpawnedForAccess)
                return FailDailyInitialization("Waiting for BattleWaveStateAuthority NetworkObject.Spawned().");
            if (!_stateAuthority.IsAuthoritative)
                return FailDailyInitialization("BattleWaveStateAuthority is not authoritative on the Daily Host.");
            if (!string.Equals(sessionContext.BattleSessionId, plan.SessionContext.battleSessionId, StringComparison.Ordinal)
                || !string.Equals(sessionContext.MapId, plan.SessionContext.mapId, StringComparison.Ordinal))
            {
                return FailDailyInitialization("Daily Session context does not match the immutable Battle session.");
            }
            if (!_stateAuthority.TryResolveMapForInitialization(
                    sessionContext.MapId,
                    out string authoritativeMapId,
                    out bool shouldRetry,
                    out string reason))
            {
                return FailDailyInitialization(reason ?? "Daily authoritative map resolution failed.");
            }
            if (shouldRetry || !string.Equals(authoritativeMapId, sessionContext.MapId, StringComparison.Ordinal))
            {
                return FailDailyInitialization(reason ?? "Authoritative Daily mapId mismatch.");
            }
            if (!TryApplyDailyContent(
                    authoritativeMapId,
                    plan.SessionContext.mapId,
                    out string dailyContentError))
            {
                return FailDailyInitialization("Daily content validation failed: " + dailyContentError);
            }

            // Cache and validate both Scene waypoint groups while they still have
            // their authored active state. Daily presentation disables Player 2
            // objects only after PathManager has completed its regular setup.
            _pathManager?.InitializePaths();
            _dailySoloPresentation ??= GetComponent<DailyBattleSoloPresentationController>();
            _dailySoloPresentation ??= gameObject.AddComponent<DailyBattleSoloPresentationController>();
            if (!_dailySoloPresentation.SetSoloPlayerOneMode(true, out string presentationError))
            {
                return RollbackDailyPresentationAndFail(
                    "Daily solo presentation failed: " + presentationError);
            }
            if (!_stateAuthority.InitializeDailySession(sessionContext, playerIdentityProvider, plan))
            {
                return RollbackDailyPresentationAndFail(
                    "BattleWaveStateAuthority rejected Daily plan initialization.");
            }

            _waveExecutor.SetLocalPlayerLane(LaneType.Player1Lane);
            DailyBattleResultCoordinator resultCoordinator = GetComponent<DailyBattleResultCoordinator>();
            resultCoordinator ??= gameObject.AddComponent<DailyBattleResultCoordinator>();
            if (!resultCoordinator.ConfigureForStateAuthority(
                    _waveExecutor,
                    _stateAuthority,
                    plan,
                    null,
                    out string resultError))
            {
                return RollbackDailyPresentationAndFail(
                    "Daily result coordinator failed: " + resultError);
            }
            SuppressDailySettlement();
            SessionContext = sessionContext;
            IsInitialized = true;
            _dailyInitializationDiagnosticKey = null;
            LastInitializationError = null;
            _waveExecutor.StartConfiguredWavesIfReady();
            Debug.Log(
                $"[DailyBattle] Development {plan.SessionContext.contentType} Stage "
                + $"{plan.SessionContext.stage} initialized for Player 1 only.");
            return true;
        }

        private bool TryApplyDailyContent(
            string authoritativeMapId,
            string plannedMapId,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(plannedMapId)
                || !string.Equals(authoritativeMapId, plannedMapId, StringComparison.Ordinal))
            {
                error = "Authoritative Daily mapId does not match the validated execution plan mapId.";
                return false;
            }

            DailyBattleContentCatalog catalog = _dailyBattleContentCatalog != null
                ? _dailyBattleContentCatalog
                : Resources.Load<DailyBattleContentCatalog>(DailyBattleContentCatalog.ResourcesPath);
            if (catalog == null)
            {
                error = "DailyBattleContentCatalog was not found at Resources/"
                    + DailyBattleContentCatalog.ResourcesPath + ".asset.";
                return false;
            }
            if (!catalog.TryResolve(authoritativeMapId, out PlanetContentProfile profile, out error))
                return false;

            _planetContentApplicator ??= GetComponent<PlanetContentApplicator>();
            _planetContentApplicator ??= gameObject.AddComponent<PlanetContentApplicator>();
            return _planetContentApplicator.TryApplyResolvedProfile(authoritativeMapId, profile, out error);
        }

        private bool RollbackDailyPresentationAndFail(string reason)
        {
            if (_dailySoloPresentation != null)
                _dailySoloPresentation.SetSoloPlayerOneMode(false, out _);
            _planetContentApplicator?.Clear();
            return FailDailyInitialization(reason);
        }

        private bool FailDailyInitialization(string reason)
        {
            LastInitializationError = string.IsNullOrWhiteSpace(reason)
                ? "Daily initialization failed for an unspecified reason."
                : reason;
            ReportDailyInitializationState(LastInitializationError);
            return false;
        }

        private void ReportDailyInitializationState(string reason)
        {
            string key = string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
            if (string.Equals(_dailyInitializationDiagnosticKey, key, StringComparison.Ordinal))
                return;
            _dailyInitializationDiagnosticKey = key;
            Debug.LogWarning("[DailyBattle] initialization waiting/failed: " + key);
        }

        private void SuppressDailySettlement()
        {
            if (_rosterRegistration != null)
                _rosterRegistration.Registered -= HandleRosterRegistered;
            _rosterRegistration = null;
            BattleSettlementCoordinator coordinator = GetComponent<BattleSettlementCoordinator>();
            if (coordinator != null)
                coordinator.enabled = false;
            Debug.Log("[DailyBattle] General roster registration and Settlement POST are disabled.");
        }
#endif

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
