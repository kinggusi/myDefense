using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MyDefense.Battle.Balance;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Runtime;
using MyDefense.Shared.Contracts;
using Object = UnityEngine.Object;

namespace MyDefense.Battle
{
    public class BattleWaveExecutor : MonoBehaviour
    {
        public static BattleWaveExecutor Instance { get; private set; }

        private enum BossStatusState
        {
            None,
            Active,
            Defeated,
            TimedOut
        }

        [Header("Wave Configuration")]
        [SerializeField] private GameObject _monsterPrefab;
        [SerializeField] private Transform _spawnPoint;
        // Legacy serialized values retained only to preserve the existing Battle scene.
        // Production wave execution is driven by WaveSpec/WaveSpawnSpec.
        [SerializeField] private float _spawnInterval = 1.0f;
        [SerializeField] private int _monstersPerWave = 10;

        [Header("Boss Configuration")]
        [SerializeField] private float _bossTimeLimit = 30f;
        [SerializeField] private float _bossSpeed = 2f;

        [Header("Monster Limit Configuration")]
        [SerializeField] private int _totalMonsterGoal = 100;

        [Header("Runtime Info")]
        [SerializeField] private int _currentRound = 0;
        [SerializeField] private bool _isWaveRunning = false;
        [SerializeField] private bool _isBossActive = false;
        [SerializeField] private bool _autoStartOnPlay = false;

        [Header("Continuous Wave Settings")]
        [SerializeField] private bool _continuousWaves = true;
        [SerializeField] private float _interWaveDelay = 3f;
        [SerializeField] private float _healthGrowthPerRound = 0.10f;
        [SerializeField] private float _bossHpMultiplier = 10f;
        [SerializeField] private LaneType _localPlayerLane = LaneType.Player1Lane;

        private GameObject _currentBossInstance = null;
        private Coroutine _bossTimerCoroutine = null;
        private Coroutine _waveLoopCoroutine = null;
        private Coroutine _activeWaveCoroutine = null;
        private bool _isCurrentWaveBoss;
        private bool _regularWaveSpawnCompleted;
        private bool _regularWaveCompletionReported;
        private int _player1AliveMonsterCount;
        private int _player2AliveMonsterCount;
        private PlayerBattleState _player1BattleState = PlayerBattleState.ACTIVE;
        private PlayerBattleState _player2BattleState = PlayerBattleState.ACTIVE;
        private MatchState _matchState = MatchState.RUNNING;
        private bool _isFaulted = false;
        private BossStatusState _bossState = BossStatusState.None;
        private GameManager _gameManagerCached = null;
        private bool _isGameOverLogged = false;
        private bool _allPlayersEliminatedReported = false;
        private IBattleBalanceProvider _battleBalanceProvider;
        private IMonsterDefinitionProvider _monsterDefinitionProvider;
        private IBattleMonsterPrefabResolver _monsterPrefabResolver;
        private WaveSpecData _currentWaveSpec;
        private IReadOnlyList<WaveSpawnSpecData> _currentWaveSpawns = Array.AsReadOnly(Array.Empty<WaveSpawnSpecData>());
        private bool _balanceInitializationAttempted;
        private bool _catalogExhausted;
        private bool _catalogExhaustedReported;
        private float _activeBossTimeLimitSeconds;
        private int _monsterWarningThreshold = 80;
        private int _monsterDangerThreshold = 90;
        private BattleSessionContext _runtimeSession;
        private IBattlePlayerIdentityProvider _playerIdentityProvider;
        private BattleSpawnSequenceIssuer _spawnSequenceIssuer;

        public LaneType LocalPlayerLane => _localPlayerLane;

        public void SetLocalPlayerLane(LaneType lane)
        {
            if (lane == LaneType.BossSharedLane)
            {
                Debug.LogWarning("[BattleWaveExecutor] SetLocalPlayerLane: BossSharedLane is not allowed!");
                return;
            }
            _localPlayerLane = lane;
        }

        public int Player1AliveMonsterCount => _player1AliveMonsterCount;
        public int Player2AliveMonsterCount => _player2AliveMonsterCount;
        public PlayerBattleState Player1BattleState => _player1BattleState;
        public PlayerBattleState Player2BattleState => _player2BattleState;
        public MatchState MatchState => _matchState;
        public bool Player1LimitReached => _player1BattleState == PlayerBattleState.ELIMINATED;
        public bool Player2LimitReached => _player2BattleState == PlayerBattleState.ELIMINATED;
        public bool AreAllPlayersEliminated => Player1LimitReached && Player2LimitReached;
        public int MonsterLimit => _totalMonsterGoal;
        public int MonsterWarningThreshold => _monsterWarningThreshold;
        public int MonsterDangerThreshold => _monsterDangerThreshold;
        public int SpawnedMonsterCount => _player1AliveMonsterCount + _player2AliveMonsterCount;
        public int TotalMonsterGoal => _totalMonsterGoal;

        public event System.Action OnBossTimeout;
        public event System.Action<float> OnBossTimerTick;
        public event System.Action OnBossDefeated;
        public event System.Action OnAllPlayersEliminated;
        public event System.Action<int> OnRoundChanged;
        public event System.Action<LaneType, int, int> OnPlayerMonsterCountChanged;
        public event System.Action<LaneType> OnPlayerMonsterLimitReached;
        public event System.Action<LaneType, PlayerBattleState> OnPlayerBattleStateChanged;
        public event System.Action<MatchState> OnMatchStateChanged;
        public event System.Action<int> OnRegularWaveCompleted;
        public event System.Action OnCatalogExhausted;

        public int CurrentRound => _currentRound;
        public string CurrentWaveId => _currentWaveSpec?.WaveId;
        public bool IsCurrentWaveBoss => _isCurrentWaveBoss;
        public bool IsBossActive => _isBossActive;
        public bool IsWaveRunning => _isWaveRunning;
        public bool IsCatalogExhausted => _catalogExhausted;
        public string BattleBalanceContentHash => _battleBalanceProvider?.ContentHash;
        public string CanonicalBalanceVersion => (_battleBalanceProvider as ICanonicalCompositeBattleBalanceProvider)?.CanonicalBalanceVersion;
        public string CanonicalContentHash => (_battleBalanceProvider as ICanonicalCompositeBattleBalanceProvider)?.CanonicalContentHash;
        public string BattleContentVersion => (_battleBalanceProvider as ICanonicalCompositeBattleBalanceProvider)?.BattleContentVersion;
        public string BattleContentHash => (_battleBalanceProvider as ICanonicalCompositeBattleBalanceProvider)?.BattleContentHash;
        public BattleSessionContext RuntimeSession => _runtimeSession;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            _gameManagerCached = Object.FindFirstObjectByType<GameManager>();
        }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDisable()
        {
            StopSessionCoroutines();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDestroy()
        {
            StopSessionCoroutines();
            ReleaseCurrentBoss();
            _runtimeSession = null;
            _playerIdentityProvider = null;
            _spawnSequenceIssuer = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool EnsureBalanceInitialized()
        {
            if (_balanceInitializationAttempted)
            {
                return !_isFaulted
                    && _battleBalanceProvider != null
                    && _battleBalanceProvider.IsValid
                    && _battleBalanceProvider.Catalog != null
                    && _monsterDefinitionProvider != null
                    && _monsterPrefabResolver != null;
            }

            _balanceInitializationAttempted = true;
            if (_battleBalanceProvider == null)
            {
                CanonicalCompositeBattleBalanceProvider canonicalProvider =
                    CanonicalCompositeBattleBalanceProvider.LoadProduction(
                        new ExistingMonsterPrefabRuntimeMapping(),
                        EmptyBattleAlienIdProvider.Instance);
                _battleBalanceProvider = canonicalProvider;
                _monsterDefinitionProvider = canonicalProvider.MonsterDefinitions;
                _monsterPrefabResolver = new ExplicitBattleMonsterPrefabResolver(
                    ExistingMonsterPrefabRuntimeMapping.ExistingPrefabKey,
                    _monsterPrefab);
            }

            var dependencyErrors = new List<string>();
            if (_monsterDefinitionProvider == null)
                dependencyErrors.Add("Monster definition provider is required for wave execution.");
            if (_monsterPrefabResolver == null)
                dependencyErrors.Add("Monster prefab resolver is required for wave execution.");
            if (_battleBalanceProvider == null)
            {
                dependencyErrors.Add("Battle balance provider is required for wave execution.");
            }
            else
            {
                for (int index = 0; index < _battleBalanceProvider.ValidationErrors.Count; index++)
                    dependencyErrors.Add(_battleBalanceProvider.ValidationErrors[index]);
                if (_battleBalanceProvider.Catalog == null && dependencyErrors.Count == 0)
                    dependencyErrors.Add("Battle balance provider returned no catalog.");
            }

            if (dependencyErrors.Count > 0)
            {
                FaultExecution(
                    "Battle balance initialization failed:" + Environment.NewLine
                    + " - " + string.Join(Environment.NewLine + " - ", dependencyErrors));
                return false;
            }

            if (_battleBalanceProvider is ICanonicalCompositeBattleBalanceProvider composite)
            {
                ApplyCanonicalFieldLimit(composite.FieldLimit);
            }

            Debug.Log(
                $"[BattleWaveExecutor] Battle balance initialized: version={_battleBalanceProvider.BalanceVersion}, "
                + $"bundleHash={_battleBalanceProvider.ContentHash}.");
            return true;
        }

        private void ApplyCanonicalFieldLimit(CanonicalFieldLimit fieldLimit)
        {
            if (fieldLimit == null)
            {
                FaultExecution("Canonical FieldLimit is required for Battle execution.");
                return;
            }

            _totalMonsterGoal = fieldLimit.MaxAliveMonsterCountPerField;
            _monsterWarningThreshold = fieldLimit.WarningThreshold;
            _monsterDangerThreshold = fieldLimit.DangerThreshold;
            OnPlayerMonsterCountChanged?.Invoke(LaneType.Player1Lane, _player1AliveMonsterCount, _totalMonsterGoal);
            OnPlayerMonsterCountChanged?.Invoke(LaneType.Player2Lane, _player2AliveMonsterCount, _totalMonsterGoal);
        }

        private void ConfigureBalanceDependenciesForTests(
            IBattleBalanceProvider balanceProvider,
            IMonsterDefinitionProvider monsterDefinitions,
            IBattleMonsterPrefabResolver prefabResolver)
        {
            _battleBalanceProvider = balanceProvider;
            _monsterDefinitionProvider = monsterDefinitions;
            _monsterPrefabResolver = prefabResolver;
            _balanceInitializationAttempted = false;
            _isFaulted = false;
        }

        private bool EnsureRuntimeSessionReady()
        {
            if (_runtimeSession == null || _spawnSequenceIssuer == null || _playerIdentityProvider == null)
            {
                FaultExecution(
                    "Battle runtime session must be injected before a wave can start. "
                    + "Use InitializeSession(BattleSessionContext, IBattlePlayerIdentityProvider).");
                return false;
            }

            string player1Id;
            string player2Id;
            if (!_playerIdentityProvider.TryGetPlayerId(LaneType.Player1Lane, out player1Id)
                || string.IsNullOrWhiteSpace(player1Id)
                || !_playerIdentityProvider.TryGetPlayerId(LaneType.Player2Lane, out player2Id)
                || string.IsNullOrWhiteSpace(player2Id)
                || string.Equals(player1Id, player2Id, StringComparison.Ordinal))
            {
                FaultExecution("Battle player identity provider must resolve two distinct, non-empty player IDs.");
                return false;
            }

            if (_battleBalanceProvider is ICanonicalCompositeBattleBalanceProvider canonical
                && (!string.Equals(_runtimeSession.CanonicalBalanceVersion, canonical.CanonicalBalanceVersion, StringComparison.Ordinal)
                    || !string.Equals(_runtimeSession.CanonicalContentHash, canonical.CanonicalContentHash, StringComparison.Ordinal)
                    || !string.Equals(_runtimeSession.BattleContentVersion, canonical.BattleContentVersion, StringComparison.Ordinal)
                    || !string.Equals(_runtimeSession.BattleContentHash, canonical.BattleContentHash, StringComparison.Ordinal)))
            {
                FaultExecution("Injected Battle session balance version/hash does not match the loaded canonical bundle.");
                return false;
            }

            return true;
        }

        private static void ValidatePlayerIdentityProvider(IBattlePlayerIdentityProvider playerIdentityProvider)
        {
            if (playerIdentityProvider == null) throw new ArgumentNullException(nameof(playerIdentityProvider));

            string player1Id;
            string player2Id;
            if (!playerIdentityProvider.TryGetPlayerId(LaneType.Player1Lane, out player1Id)
                || string.IsNullOrWhiteSpace(player1Id)
                || !playerIdentityProvider.TryGetPlayerId(LaneType.Player2Lane, out player2Id)
                || string.IsNullOrWhiteSpace(player2Id)
                || string.Equals(player1Id, player2Id, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Player identity provider must resolve two distinct, non-empty player IDs.",
                    nameof(playerIdentityProvider));
            }
        }

        private void FaultExecution(string reason)
        {
            if (_isFaulted) return;

            _isFaulted = true;
            _isWaveRunning = false;
            _isBossActive = false;
            _activeWaveCoroutine = null;
            StopBossTimer();
            if (_waveLoopCoroutine != null)
            {
                StopCoroutine(_waveLoopCoroutine);
                _waveLoopCoroutine = null;
            }

            Debug.LogError("[BattleWaveExecutor] " + reason);
        }

        private bool CanSpawnInLane(LaneType lane)
        {
            if (_matchState != MatchState.RUNNING) return false;

            return lane switch
            {
                LaneType.Player1Lane => _player1BattleState == PlayerBattleState.ACTIVE,
                LaneType.Player2Lane => _player2BattleState == PlayerBattleState.ACTIVE,
                _ => false
            };
        }

        private void EliminatePlayer(LaneType lane)
        {
            if (lane == LaneType.Player1Lane)
            {
                if (_player1BattleState != PlayerBattleState.ACTIVE) return;
                _player1BattleState = PlayerBattleState.ELIMINATED;
                OnPlayerBattleStateChanged?.Invoke(lane, _player1BattleState);
            }
            else if (lane == LaneType.Player2Lane)
            {
                if (_player2BattleState != PlayerBattleState.ACTIVE) return;
                _player2BattleState = PlayerBattleState.ELIMINATED;
                OnPlayerBattleStateChanged?.Invoke(lane, _player2BattleState);
            }
            else
            {
                return;
            }

            OnPlayerMonsterLimitReached?.Invoke(lane);
            Debug.Log($"[BattleWaveExecutor] Player {lane} reached monster limit. Spawn suspended for this lane.");

            if (AreAllPlayersEliminated)
            {
                ReportAllPlayersEliminated();
                TryTransitionMatchState(MatchState.FAILED);
            }
        }

        private void ReportAllPlayersEliminated()
        {
            if (_allPlayersEliminatedReported) return;

            _allPlayersEliminatedReported = true;
            OnAllPlayersEliminated?.Invoke();
            Debug.Log("[BattleWaveExecutor] All players eliminated. Halting wave execution.");
        }

        private bool TryTransitionMatchState(MatchState nextState)
        {
            if (_matchState != MatchState.RUNNING || nextState == MatchState.RUNNING)
            {
                return false;
            }

            if (_runtimeSession != null && !_runtimeSession.TryTransitionMatchState(nextState))
            {
                return false;
            }

            _matchState = nextState;
            _isWaveRunning = false;

            if (_waveLoopCoroutine != null)
            {
                StopCoroutine(_waveLoopCoroutine);
                _waveLoopCoroutine = null;
            }

            OnMatchStateChanged?.Invoke(_matchState);
            return true;
        }

        private void StopSessionCoroutines()
        {
            if (_waveLoopCoroutine != null)
            {
                StopCoroutine(_waveLoopCoroutine);
                _waveLoopCoroutine = null;
            }

            if (_activeWaveCoroutine != null)
            {
                StopCoroutine(_activeWaveCoroutine);
                _activeWaveCoroutine = null;
            }

            StopBossTimer();
            _isWaveRunning = false;
            _isCurrentWaveBoss = false;
            _regularWaveSpawnCompleted = false;
            _regularWaveCompletionReported = false;
        }

        private void StopBossTimer()
        {
            if (_bossTimerCoroutine == null) return;

            StopCoroutine(_bossTimerCoroutine);
            _bossTimerCoroutine = null;
        }

        private void ReleaseCurrentBoss()
        {
            if (_currentBossInstance == null)
            {
                _currentBossInstance = null;
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_currentBossInstance);
            }
            else
            {
                DestroyImmediate(_currentBossInstance);
            }

            _currentBossInstance = null;
        }

        private void RegisterMonsterSpawned(LaneType lane)
        {
            if (!CanSpawnInLane(lane)) return;

            if (lane == LaneType.Player1Lane)
            {
                _player1AliveMonsterCount++;
                OnPlayerMonsterCountChanged?.Invoke(lane, _player1AliveMonsterCount, _totalMonsterGoal);

                if (_player1AliveMonsterCount >= _totalMonsterGoal)
                {
                    EliminatePlayer(lane);
                }
            }
            else if (lane == LaneType.Player2Lane)
            {
                _player2AliveMonsterCount++;
                OnPlayerMonsterCountChanged?.Invoke(lane, _player2AliveMonsterCount, _totalMonsterGoal);

                if (_player2AliveMonsterCount >= _totalMonsterGoal)
                {
                    EliminatePlayer(lane);
                }
            }

        }

        public void RegisterMonsterKilled(LaneType lane)
        {
            if (lane == LaneType.BossSharedLane) return;

            bool countChanged = false;
            if (lane == LaneType.Player1Lane)
            {
                if (_player1AliveMonsterCount > 0)
                {
                    _player1AliveMonsterCount--;
                    countChanged = true;
                    OnPlayerMonsterCountChanged?.Invoke(lane, _player1AliveMonsterCount, _totalMonsterGoal);
                }
            }
            else if (lane == LaneType.Player2Lane)
            {
                if (_player2AliveMonsterCount > 0)
                {
                    _player2AliveMonsterCount--;
                    countChanged = true;
                    OnPlayerMonsterCountChanged?.Invoke(lane, _player2AliveMonsterCount, _totalMonsterGoal);
                }
            }

            if (countChanged) TryCompleteRegularWave();
        }

        public void InitializeSession()
        {
            if (_runtimeSession != null)
            {
                throw new InvalidOperationException(
                    "Reinitializing a runtime Battle session requires a new BattleSessionContext.");
            }

            _playerIdentityProvider = null;
            _spawnSequenceIssuer = null;
            ResetSessionState();
        }

        public void InitializeSession(
            BattleSessionContext sessionContext,
            IBattlePlayerIdentityProvider playerIdentityProvider)
        {
            if (sessionContext == null) throw new ArgumentNullException(nameof(sessionContext));
            ValidatePlayerIdentityProvider(playerIdentityProvider);
            if (_runtimeSession != null
                && string.Equals(_runtimeSession.BattleSessionId, sessionContext.BattleSessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Session reinitialization requires a new battleSessionId.");
            }

            _runtimeSession = sessionContext;
            _playerIdentityProvider = playerIdentityProvider;
            _spawnSequenceIssuer = new BattleSpawnSequenceIssuer();
            ResetSessionState();
        }

        private void ResetSessionState()
        {
            StopSessionCoroutines();
            ReleaseCurrentBoss();
            _currentRound = 0;
            _isWaveRunning = false;
            _isBossActive = false;
            _bossState = BossStatusState.None;
            _isCurrentWaveBoss = false;
            _regularWaveSpawnCompleted = false;
            _regularWaveCompletionReported = false;
            _player1AliveMonsterCount = 0;
            _player2AliveMonsterCount = 0;
            _player1BattleState = PlayerBattleState.ACTIVE;
            _player2BattleState = PlayerBattleState.ACTIVE;
            _matchState = MatchState.RUNNING;
            _isFaulted = _balanceInitializationAttempted
                && (_battleBalanceProvider == null
                    || !_battleBalanceProvider.IsValid
                    || _battleBalanceProvider.Catalog == null
                    || _monsterDefinitionProvider == null
                    || _monsterPrefabResolver == null);
            _isGameOverLogged = false;
            _allPlayersEliminatedReported = false;
            _currentWaveSpec = null;
            _currentWaveSpawns = Array.AsReadOnly(Array.Empty<WaveSpawnSpecData>());
            _catalogExhausted = false;
            _catalogExhaustedReported = false;
            _activeBossTimeLimitSeconds = 0f;
            PublishSessionState();
            Debug.Log("[BattleWaveExecutor] Battle session initialized.");
        }

        private void PublishSessionState()
        {
            OnRoundChanged?.Invoke(_currentRound);
            OnPlayerMonsterCountChanged?.Invoke(LaneType.Player1Lane, _player1AliveMonsterCount, _totalMonsterGoal);
            OnPlayerMonsterCountChanged?.Invoke(LaneType.Player2Lane, _player2AliveMonsterCount, _totalMonsterGoal);
            OnPlayerBattleStateChanged?.Invoke(LaneType.Player1Lane, _player1BattleState);
            OnPlayerBattleStateChanged?.Invoke(LaneType.Player2Lane, _player2BattleState);
            OnMatchStateChanged?.Invoke(_matchState);
        }

        private void Start()
        {
            if (_runtimeSession == null)
                InitializeSession();

            if (!EnsureBalanceInitialized())
            {
                return;
            }

            if (_autoStartOnPlay)
            {
                if (_continuousWaves)
                {
                    if (_waveLoopCoroutine != null) StopCoroutine(_waveLoopCoroutine);
                    _waveLoopCoroutine = StartCoroutine(ContinuousWaveLoopRoutine());
                }
                else
                {
                    StartNextWave();
                }
            }
        }

        private void Update()
        {
            if (_isBossActive && _currentBossInstance == null)
            {
                if (_bossState == BossStatusState.Active)
                {
                    HandleBossDefeated();
                }
            }
        }

        private bool CheckGameOverState()
        {
            if (_gameManagerCached != null && _gameManagerCached.IsGameOver)
            {
                if (!_isGameOverLogged)
                {
                    Debug.Log("[BattleWaveExecutor] GameManager.IsGameOver detected. Halting operations.");
                    _isGameOverLogged = true;
                }
                TryTransitionMatchState(MatchState.FAILED);
                return true;
            }
            return false;
        }

        private IEnumerator ContinuousWaveLoopRoutine()
        {
            while (true)
            {
                if (CheckGameOverState()) yield break;
                if (_matchState != MatchState.RUNNING) yield break;
                if (_isFaulted) yield break;

                while (_isWaveRunning || _isBossActive)
                {
                    if (_matchState != MatchState.RUNNING) yield break;
                    yield return new WaitForSeconds(0.5f);
                }

                if (CheckGameOverState()) yield break;
                if (_matchState != MatchState.RUNNING) yield break;
                if (_isFaulted) yield break;

                StartNextWave();

                if (_catalogExhausted || _isFaulted) yield break;

                yield return new WaitForSeconds(0.5f);

                while (_isWaveRunning || _isBossActive)
                {
                    if (_matchState != MatchState.RUNNING) yield break;
                    yield return new WaitForSeconds(0.5f);
                }

                if (CheckGameOverState()) yield break;
                if (_matchState != MatchState.RUNNING) yield break;
                if (_isFaulted) yield break;

                float nextWaveDelay = _currentWaveSpec != null
                    ? _currentWaveSpec.NextWaveDelaySeconds
                    : 0f;
                if (nextWaveDelay > 0f)
                    yield return new WaitForSeconds(nextWaveDelay);
                else
                    yield return null;
            }
        }

        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            if (!EnsureBalanceInitialized()) return;
            if (!TryBeginNextWave()) return;

            if (_isCurrentWaveBoss)
            {
                _activeWaveCoroutine = StartCoroutine(SpawnBossRoutine());
            }
            else
            {
                _activeWaveCoroutine = StartCoroutine(SpawnRegularWaveRoutine());
            }
        }

        private bool TryBeginNextWave()
        {
            if (!EnsureBalanceInitialized()) return false;
            if (_matchState != MatchState.RUNNING) return false;
            if (CheckGameOverState()) return false;
            if (!EnsureRuntimeSessionReady()) return false;

            if (AreAllPlayersEliminated)
            {
                ReportAllPlayersEliminated();
                TryTransitionMatchState(MatchState.FAILED);
                return false;
            }

            if (_isBossActive)
            {
                Debug.LogWarning("[BattleWaveExecutor] Cannot start next wave: Boss is active!");
                return false;
            }

            if (_isWaveRunning)
            {
                Debug.LogWarning("[BattleWaveExecutor] Cannot start next wave: Wave is already running.");
                return false;
            }

            if (HasAliveRegularMonsters())
            {
                Debug.LogWarning("[BattleWaveExecutor] Cannot start next wave: Regular monsters are still alive.");
                return false;
            }

            WaveSpecData nextWave;
            if (!_battleBalanceProvider.Catalog.Waves.TryGetNextEnabledWave(_currentRound, out nextWave))
            {
                ReportCatalogExhausted();
                return false;
            }

            IReadOnlyList<WaveSpawnSpecData> spawns = _battleBalanceProvider.Catalog.Waves.GetSpawns(nextWave.WaveId);
            if (spawns.Count == 0)
            {
                FaultExecution($"Wave '{nextWave.WaveId}' has no spawn rows.");
                return false;
            }

            for (int index = 0; index < spawns.Count; index++)
            {
                WaveSpawnSpecData spawn = spawns[index];
                if (!string.Equals(spawn.WaveId, nextWave.WaveId, StringComparison.Ordinal))
                {
                    FaultExecution(
                        $"Wave '{nextWave.WaveId}' contains an unknown waveId spawn row '{spawn.WaveId}'.");
                    return false;
                }

                bool policyMatches = nextWave.WaveType == WaveType.REGULAR
                    ? spawn.LanePolicy == BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE
                    : spawn.LanePolicy == BattleLanePolicy.BOSS_SHARED;
                if (!policyMatches)
                {
                    FaultExecution(
                        $"Wave '{nextWave.WaveId}' has invalid lane policy '{spawn.LanePolicy}' "
                        + $"for wave type '{nextWave.WaveType}'.");
                    return false;
                }
            }

            _currentWaveSpec = nextWave;
            _currentWaveSpawns = spawns;
            _currentRound = nextWave.RoundNumber;
            BeginWaveExecution(nextWave.WaveType == WaveType.BOSS);

            OnRoundChanged?.Invoke(_currentRound);

            Debug.Log(
                $"[BattleWaveExecutor] Round {_currentRound} ({nextWave.WaveType}) started from wave '{nextWave.WaveId}'.");
            return true;
        }

        private void ReportCatalogExhausted()
        {
            _catalogExhausted = true;
            _isWaveRunning = false;
            if (_catalogExhaustedReported) return;

            _catalogExhaustedReported = true;
            Debug.Log($"[BattleWaveExecutor] Battle wave catalog exhausted after round {_currentRound}.");
            OnCatalogExhausted?.Invoke();
        }

        private void BeginWaveExecution(bool isBossWave)
        {
            _isCurrentWaveBoss = isBossWave;
            _regularWaveSpawnCompleted = false;
            _regularWaveCompletionReported = false;
            _isWaveRunning = true;
        }

        private bool HasAliveRegularMonsters()
        {
            return _player1AliveMonsterCount > 0 || _player2AliveMonsterCount > 0;
        }

        private void MarkRegularWaveSpawnCompleted()
        {
            _regularWaveSpawnCompleted = true;
            _activeWaveCoroutine = null;
            Debug.Log($"[BattleWaveExecutor] Round {_currentRound} regular wave spawn completed. Waiting for remaining monsters.");
            TryCompleteRegularWave();
        }

        private bool TryCompleteRegularWave()
        {
            if (_matchState != MatchState.RUNNING) return false;
            if (_isCurrentWaveBoss || _isBossActive) return false;
            if (!_isWaveRunning || !_regularWaveSpawnCompleted) return false;
            if (HasAliveRegularMonsters()) return false;
            if (_regularWaveCompletionReported) return false;

            _regularWaveCompletionReported = true;
            _isWaveRunning = false;
            Debug.Log($"[BattleWaveExecutor] Round {_currentRound} regular wave completed.");
            OnRegularWaveCompleted?.Invoke(_currentRound);
            return true;
        }

        private IEnumerator SpawnRegularWaveRoutine()
        {
            if (_currentWaveSpec == null || _currentWaveSpec.WaveType != WaveType.REGULAR)
            {
                FaultExecution("Regular wave routine started without a REGULAR WaveSpec.");
                yield break;
            }

            for (int rowIndex = 0; rowIndex < _currentWaveSpawns.Count; rowIndex++)
            {
                WaveSpawnSpecData spawn = _currentWaveSpawns[rowIndex];
                if (spawn.LanePolicy != BattleLanePolicy.EACH_ACTIVE_PLAYER_LANE)
                {
                    FaultExecution(
                        $"Regular wave '{_currentWaveSpec.WaveId}' cannot execute lane policy '{spawn.LanePolicy}'.");
                    yield break;
                }

                BattleMonsterDefinition definition;
                if (!_monsterDefinitionProvider.TryGet(spawn.MonsterId, out definition))
                {
                    FaultExecution(
                        $"Wave '{_currentWaveSpec.WaveId}' references unknown monsterId '{spawn.MonsterId}'.");
                    yield break;
                }

                if (spawn.SpawnDelaySeconds > 0f)
                    yield return new WaitForSeconds(spawn.SpawnDelaySeconds);

                int player1Remaining = CanSpawnInLane(LaneType.Player1Lane) ? spawn.SpawnCount : 0;
                int player2Remaining = CanSpawnInLane(LaneType.Player2Lane) ? spawn.SpawnCount : 0;
                while (player1Remaining > 0 || player2Remaining > 0)
                {
                    if (!CanContinueWaveExecution()) yield break;

                    if (player1Remaining > 0)
                    {
                        if (CanSpawnInLane(LaneType.Player1Lane))
                        {
                            if (!SpawnConfiguredMonster(LaneType.Player1Lane, definition, spawn, 1f, out _))
                                yield break;
                            player1Remaining--;
                        }
                        else
                        {
                            player1Remaining = 0;
                        }
                    }

                    if (player2Remaining > 0)
                    {
                        if (CanSpawnInLane(LaneType.Player2Lane))
                        {
                            if (!SpawnConfiguredMonster(LaneType.Player2Lane, definition, spawn, 1f, out _))
                                yield break;
                            player2Remaining--;
                        }
                        else
                        {
                            player2Remaining = 0;
                        }
                    }

                    if (player1Remaining > 0 || player2Remaining > 0)
                    {
                        if (spawn.SpawnIntervalSeconds > 0f)
                            yield return new WaitForSeconds(spawn.SpawnIntervalSeconds);
                        else
                            yield return null;
                    }
                }
            }

            if (!_isFaulted) MarkRegularWaveSpawnCompleted();
        }

        private IEnumerator SpawnBossRoutine()
        {
            if (_currentWaveSpec == null || _currentWaveSpec.WaveType != WaveType.BOSS)
            {
                FaultExecution("Boss wave routine started without a BOSS WaveSpec.");
                yield break;
            }

            if (_currentWaveSpawns.Count != 1
                || _currentWaveSpawns[0].LanePolicy != BattleLanePolicy.BOSS_SHARED
                || _currentWaveSpawns[0].SpawnCount != 1)
            {
                FaultExecution(
                    $"Boss wave '{_currentWaveSpec.WaveId}' must contain exactly one BOSS_SHARED spawn.");
                yield break;
            }

            WaveSpawnSpecData spawn = _currentWaveSpawns[0];
            BattleMonsterDefinition definition;
            if (!_monsterDefinitionProvider.TryGet(spawn.MonsterId, out definition))
            {
                FaultExecution(
                    $"Boss wave '{_currentWaveSpec.WaveId}' references unknown monsterId '{spawn.MonsterId}'.");
                yield break;
            }

            if (spawn.SpawnDelaySeconds > 0f)
                yield return new WaitForSeconds(spawn.SpawnDelaySeconds);

            if (!CanContinueWaveExecution()) yield break;

            GameObject boss;
            if (!SpawnConfiguredMonster(LaneType.BossSharedLane, definition, spawn, 2f, out boss))
                yield break;

            ActivateBoss(boss);
            _activeBossTimeLimitSeconds = _currentWaveSpec.BossTimeLimitSeconds;
            Debug.Log(
                $"[BattleWaveExecutor] Boss round {_currentRound} entered from wave '{_currentWaveSpec.WaveId}'.");

            StopBossTimer();
            _bossTimerCoroutine = StartCoroutine(BossTimerRoutine());
            yield return null;
            _activeWaveCoroutine = null;
        }

        private bool CanContinueWaveExecution()
        {
            if (_isFaulted || _matchState != MatchState.RUNNING)
            {
                _isWaveRunning = false;
                _activeWaveCoroutine = null;
                return false;
            }

            if (CheckGameOverState())
            {
                _isWaveRunning = false;
                _activeWaveCoroutine = null;
                return false;
            }

            return true;
        }

        private void ActivateBoss(GameObject bossInstance)
        {
            _currentBossInstance = bossInstance;
            _isBossActive = bossInstance != null;
            _bossState = _isBossActive ? BossStatusState.Active : BossStatusState.None;
        }

        private IEnumerator BossTimerRoutine()
        {
            float timeLeft = _activeBossTimeLimitSeconds;

            while (timeLeft > 0f)
            {
                if (_bossState != BossStatusState.Active || _matchState != MatchState.RUNNING)
                {
                    _bossTimerCoroutine = null;
                    yield break;
                }

                OnBossTimerTick?.Invoke(timeLeft);
                yield return new WaitForSeconds(1.0f);
                timeLeft -= 1.0f;

                if (_currentBossInstance == null)
                {
                    _bossTimerCoroutine = null;
                    HandleBossDefeated();
                    yield break;
                }
            }

            TryResolveBossTimeout();
        }

        private bool TryResolveBossTimeout()
        {
            if (_bossState != BossStatusState.Active || _matchState != MatchState.RUNNING)
            {
                _bossTimerCoroutine = null;
                return false;
            }

            if (_currentBossInstance == null)
            {
                HandleBossDefeated();
                return false;
            }

            _bossState = BossStatusState.TimedOut;
            _isBossActive = false;
            _bossTimerCoroutine = null;

            if (!TryTransitionMatchState(MatchState.FAILED))
            {
                return false;
            }

            OnBossTimerTick?.Invoke(0f);
            Debug.Log(
                $"[BattleWaveExecutor] Boss limit {_activeBossTimeLimitSeconds}s exceeded. "
                + "Match failed and wave loop halted.");
            OnBossTimeout?.Invoke();
            return true;
        }

        private bool HandleBossDefeated()
        {
            if (_bossState != BossStatusState.Active)
            {
                return false;
            }

            _bossState = BossStatusState.Defeated;
            _isBossActive = false;
            _isWaveRunning = false;
            _currentBossInstance = null;

            StopBossTimer();

            Debug.Log("[BattleWaveExecutor] Boss defeated! Next round criteria cleared.");
            OnBossDefeated?.Invoke();
            return true;
        }

        private bool SpawnConfiguredMonster(
            LaneType lane,
            BattleMonsterDefinition definition,
            WaveSpawnSpecData spawn,
            float scale,
            out GameObject spawnedInstance)
        {
            spawnedInstance = null;
            if (!EnsureRuntimeSessionReady()) return false;
            if (definition == null)
            {
                FaultExecution("Cannot spawn a null MonsterDefinition.");
                return false;
            }

            if (_spawnPoint == null)
            {
                FaultExecution("Cannot spawn monster: the existing spawn point reference is missing.");
                return false;
            }

            GameObject prefab;
            if (_monsterPrefabResolver == null
                || !_monsterPrefabResolver.TryResolve(definition.PrefabKey, out prefab)
                || prefab == null)
            {
                FaultExecution(
                    $"Cannot resolve prefabKey '{definition.PrefabKey}' for monsterId '{definition.MonsterId}'.");
                return false;
            }

            int movementCount = prefab.GetComponents<BattleMonsterMovement>().Length;
            if (movementCount != 1)
            {
                FaultExecution(
                    $"Prefab '{prefab.name}' for monsterId '{definition.MonsterId}' must contain exactly one "
                    + $"BattleMonsterMovement, but found {movementCount}.");
                return false;
            }

            if (prefab.GetComponent<MonsterStat>() == null)
            {
                FaultExecution(
                    $"Prefab '{prefab.name}' for monsterId '{definition.MonsterId}' must contain MonsterStat.");
                return false;
            }

            int runtimeContextCount = prefab.GetComponents<BattleMonsterRuntimeContext>().Length;
            if (runtimeContextCount != 1)
            {
                FaultExecution(
                    $"Prefab '{prefab.name}' for monsterId '{definition.MonsterId}' must contain exactly one "
                    + $"BattleMonsterRuntimeContext, but found {runtimeContextCount}.");
                return false;
            }

            BattleMonsterLanePolicy runtimeLanePolicy;
            string fieldOwnerPlayerId;
            if (lane == LaneType.BossSharedLane)
            {
                runtimeLanePolicy = BattleMonsterLanePolicy.BOSS_SHARED;
                fieldOwnerPlayerId = null;
            }
            else
            {
                runtimeLanePolicy = BattleMonsterLanePolicy.EACH_FIELD;
                if (!_playerIdentityProvider.TryGetPlayerId(lane, out fieldOwnerPlayerId)
                    || string.IsNullOrWhiteSpace(fieldOwnerPlayerId))
                {
                    FaultExecution($"Player identity provider cannot resolve owner for lane '{lane}'.");
                    return false;
                }
            }

            ulong spawnSequence;
            try
            {
                spawnSequence = _spawnSequenceIssuer.IssueNext();
            }
            catch (OverflowException exception)
            {
                FaultExecution(exception.Message);
                return false;
            }

            spawnedInstance = Instantiate(prefab, _spawnPoint.position, Quaternion.identity);
            if (spawnedInstance == null)
            {
                FaultExecution(
                    $"Instantiation failed for monsterId '{definition.MonsterId}' and prefabKey '{definition.PrefabKey}'.");
                return false;
            }

            float resolvedMoveSpeed = definition.MoveSpeed * spawn.MoveSpeedMultiplier;
            float resolvedMaxHp = definition.BaseMaxHp * spawn.HpMultiplier;
            BattleMonsterRuntimeContext runtimeContext = spawnedInstance.GetComponent<BattleMonsterRuntimeContext>();
            try
            {
                runtimeContext.Initialize(new BattleMonsterRuntimeIdentity(
                    _runtimeSession,
                    spawnSequence,
                    definition.MonsterId,
                    runtimeLanePolicy,
                    fieldOwnerPlayerId,
                    _currentRound,
                    spawnSequence));
            }
            catch (Exception exception)
            {
                DestroySpawnedInstance(spawnedInstance);
                spawnedInstance = null;
                FaultExecution(
                    $"Runtime context initialization failed for monsterId '{definition.MonsterId}': {exception.Message}");
                return false;
            }

            if (!TryConfigureSpawnedMovement(spawnedInstance, lane, resolvedMoveSpeed))
            {
                spawnedInstance = null;
                return false;
            }

            MonsterStat stat = spawnedInstance.GetComponent<MonsterStat>();
            stat.InitializeHp(resolvedMaxHp);
            stat.InitializeBattleContext(lane, definition.CountsTowardLaneLimit);
            spawnedInstance.transform.localScale = Vector3.one * scale;

            if (definition.CountsTowardLaneLimit)
                RegisterMonsterSpawned(lane);

            if (_isFaulted)
            {
                DestroySpawnedInstance(spawnedInstance);
                spawnedInstance = null;
                return false;
            }

            return true;
        }

        // Retained for existing reflection-based state tests. Production routines use
        // SpawnConfiguredMonster and never read legacy growth/count/interval fields.
        private bool SpawnMonster(LaneType lane, float speed, float scale)
        {
            if (!CanSpawnInLane(lane)) return false;

            if (_monsterPrefab == null || _spawnPoint == null)
            {
                FaultExecution("SpawnMonster failed: prefab or spawnPoint is null.");
                return false;
            }

            Vector3 finalSpawnPos = _spawnPoint.position;
            GameObject go = Instantiate(_monsterPrefab, finalSpawnPos, Quaternion.identity);

            if (go == null)
            {
                FaultExecution("Monster instantiation failed.");
                return false;
            }

            if (!TryConfigureSpawnedMovement(go, lane, speed)) return false;

            go.transform.localScale = Vector3.one * scale;

            MonsterStat stat = go.GetComponent<MonsterStat>();
            if (stat != null)
            {
                stat.InitializeHp(stat.hp);
                stat.InitializeBattleContext(lane, true);
            }

            RegisterMonsterSpawned(lane);
            return true;
        }

        private bool TryConfigureSpawnedMovement(GameObject instance, LaneType lane, float speed)
        {
            BattleMonsterMovement[] movements = instance.GetComponents<BattleMonsterMovement>();
            if (movements.Length != 1)
            {
                string error =
                    $"Spawned instance '{instance.name}' must contain exactly one BattleMonsterMovement, "
                    + $"but found {movements.Length}. Spawn rejected.";
                DestroySpawnedInstance(instance);
                FaultExecution(error);
                return false;
            }

            movements[0].Lane = lane;
            movements[0].Speed = speed;
            return true;
        }

        private void DestroySpawnedInstance(GameObject instance)
        {
            if (instance == null) return;

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
        }
    }
}
