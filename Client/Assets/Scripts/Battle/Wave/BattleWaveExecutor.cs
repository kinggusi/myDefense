using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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
        [SerializeField] private float _spawnInterval = 1.0f;
        [SerializeField] private int _monstersPerWave = 10;

        [Header("Boss Configuration")]
        [SerializeField] private float _bossTimeLimit = 30f;
        [SerializeField] private float _bossSpeed = 2f;

        [Header("Monster Count UI Configuration")]
        [SerializeField] private TMP_Text _monsterCountText;
        [SerializeField] private int _totalMonsterGoal = 100;

        [Header("Runtime Info")]
        [SerializeField] private int _currentRound = 0;
        [SerializeField] private bool _isWaveRunning = false;
        [SerializeField] private bool _isBossActive = false;
        [SerializeField] private bool _autoStartOnPlay = false;

        [Header("Continuous Wave Settings")]
        [SerializeField] private bool _continuousWaves = true;
        [SerializeField] private float _interWaveDelay = 3f;

        private GameObject _currentBossInstance = null;
        private Coroutine _bossTimerCoroutine = null;
        private Coroutine _waveLoopCoroutine = null;
        private int _spawnedMonsterCount;
        private bool _isFaulted = false;
        private BossStatusState _bossState = BossStatusState.None;
        private GameManager _gameManagerCached = null;
        private bool _isGameOverLogged = false;

        public int SpawnedMonsterCount => _spawnedMonsterCount;
        public int TotalMonsterGoal => _totalMonsterGoal;

        public event System.Action OnBossTimeout;
        public event System.Action<float> OnBossTimerTick;
        public event System.Action OnBossDefeated;

        public int CurrentRound => _currentRound;
        public bool IsBossActive => _isBossActive;
        public bool IsWaveRunning => _isWaveRunning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            _gameManagerCached = Object.FindFirstObjectByType<GameManager>();
        }

        private void OnDisable()
        {
            if (_waveLoopCoroutine != null)
            {
                StopCoroutine(_waveLoopCoroutine);
                _waveLoopCoroutine = null;
            }
            if (_bossTimerCoroutine != null)
            {
                StopCoroutine(_bossTimerCoroutine);
                _bossTimerCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void UpdateMonsterCountUI()
        {
            if (_monsterCountText != null)
            {
                _monsterCountText.text = $"{_spawnedMonsterCount} / {_totalMonsterGoal}";
            }
        }

        private void RegisterMonsterSpawned()
        {
            _spawnedMonsterCount++;
            UpdateMonsterCountUI();
        }

        public void RegisterMonsterKilled()
        {
            if (_spawnedMonsterCount > 0)
            {
                _spawnedMonsterCount--;
                UpdateMonsterCountUI();
            }
        }

        private void Start()
        {
            UpdateMonsterCountUI();

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
                _isWaveRunning = false;
                return true;
            }
            return false;
        }

        private IEnumerator ContinuousWaveLoopRoutine()
        {
            while (true)
            {
                if (CheckGameOverState()) yield break;
                if (_isFaulted) yield break;
                if (_bossState == BossStatusState.TimedOut) yield break;

                while (_isWaveRunning || _isBossActive)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                if (CheckGameOverState()) yield break;
                if (_isFaulted) yield break;
                if (_bossState == BossStatusState.TimedOut) yield break;

                StartNextWave();

                yield return new WaitForSeconds(0.5f);

                while (_isWaveRunning || _isBossActive)
                {
                    if (_bossState == BossStatusState.TimedOut) yield break;
                    yield return new WaitForSeconds(0.5f);
                }

                if (CheckGameOverState()) yield break;
                if (_isFaulted) yield break;
                if (_bossState == BossStatusState.TimedOut) yield break;

                yield return new WaitForSeconds(_interWaveDelay);
            }
        }

        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            if (CheckGameOverState()) return;

            if (_isBossActive)
            {
                Debug.LogWarning("[BattleWaveExecutor] Cannot start next wave: Boss is active!");
                return;
            }

            if (_isWaveRunning)
            {
                Debug.LogWarning("[BattleWaveExecutor] Cannot start next wave: Wave is already running.");
                return;
            }

            _currentRound++;
            _isWaveRunning = true;

            Debug.Log($"[BattleWaveExecutor] Round {_currentRound} started!");

            if (_currentRound % 10 == 0)
            {
                StartCoroutine(SpawnBossRoutine());
            }
            else
            {
                StartCoroutine(SpawnRegularWaveRoutine());
            }
        }

        private IEnumerator SpawnRegularWaveRoutine()
        {
            bool testToggle = false;

            for (int i = 0; i < _monstersPerWave; i++)
            {
                if (CheckGameOverState())
                {
                    _isWaveRunning = false;
                    yield break;
                }

                if (_monsterPrefab == null || _spawnPoint == null)
                {
                    Debug.LogError("[BattleWaveExecutor] Aborting wave: prefab or spawnPoint is null!");
                    _isFaulted = true;
                    _isWaveRunning = false;
                    yield break;
                }

                SpawnMonster(testToggle ? LaneType.Player1Lane : LaneType.Player2Lane, 5f, 1f);
                testToggle = !testToggle;
                yield return new WaitForSeconds(_spawnInterval);
            }

            _isWaveRunning = false;
            Debug.Log($"[BattleWaveExecutor] Round {_currentRound} regular wave spawn completed.");
        }

        private IEnumerator SpawnBossRoutine()
        {
            _isBossActive = true;
            _bossState = BossStatusState.Active;
            Debug.Log($"[BattleWaveExecutor] Boss round {_currentRound} entered! Boss spawned!");

            if (_monsterPrefab == null || _spawnPoint == null)
            {
                Debug.LogError("[BattleWaveExecutor] Aborting boss spawn: prefab or spawnPoint is null!");
                _isFaulted = true;
                _isBossActive = false;
                _isWaveRunning = false;
                yield break;
            }

            Vector3 finalSpawnPos = _spawnPoint.position;
            _currentBossInstance = Instantiate(_monsterPrefab, finalSpawnPos, Quaternion.identity);

            if (_currentBossInstance == null)
            {
                Debug.LogError("[BattleWaveExecutor] Boss instantiation failed!");
                _isFaulted = true;
                _isBossActive = false;
                _isWaveRunning = false;
                yield break;
            }

            RegisterMonsterSpawned();

            MonsterMovement oldMove = _currentBossInstance.GetComponent<MonsterMovement>();
            if (oldMove != null) oldMove.enabled = false;

            BattleMonsterMovement newMove = _currentBossInstance.GetComponent<BattleMonsterMovement>();
            if (newMove == null) newMove = _currentBossInstance.AddComponent<BattleMonsterMovement>();

            newMove.Lane = LaneType.BossSharedLane;
            newMove.Speed = _bossSpeed;
            _currentBossInstance.transform.localScale = Vector3.one * 2.0f;

            if (_bossTimerCoroutine != null) StopCoroutine(_bossTimerCoroutine);
            _bossTimerCoroutine = StartCoroutine(BossTimerRoutine());

            yield return null;
            _isWaveRunning = false;
        }

        private IEnumerator BossTimerRoutine()
        {
            float timeLeft = _bossTimeLimit;

            while (timeLeft > 0)
            {
                OnBossTimerTick?.Invoke(timeLeft);
                yield return new WaitForSeconds(1.0f);
                timeLeft -= 1.0f;

                if (_bossState != BossStatusState.Active) yield break;
            }

            OnBossTimerTick?.Invoke(0f);
            _bossState = BossStatusState.TimedOut;
            Debug.LogError($"[BattleWaveExecutor] Boss limit {_bossTimeLimit}s exceeded! Wave loop halted.");

            OnBossTimeout?.Invoke();
        }

        private void HandleBossDefeated()
        {
            _isBossActive = false;
            _bossState = BossStatusState.Defeated;

            if (_bossTimerCoroutine != null)
            {
                StopCoroutine(_bossTimerCoroutine);
                _bossTimerCoroutine = null;
            }

            Debug.Log("[BattleWaveExecutor] Boss defeated! Next round criteria cleared.");
            OnBossDefeated?.Invoke();
        }

        private void SpawnMonster(LaneType lane, float speed, float scale)
        {
            if (_monsterPrefab == null || _spawnPoint == null)
            {
                Debug.LogError("[BattleWaveExecutor] SpawnMonster failed: prefab or spawnPoint is null!");
                _isFaulted = true;
                return;
            }

            Vector3 finalSpawnPos = _spawnPoint.position;
            GameObject go = Instantiate(_monsterPrefab, finalSpawnPos, Quaternion.identity);

            if (go == null)
            {
                Debug.LogError("[BattleWaveExecutor] Monster instantiation failed!");
                _isFaulted = true;
                return;
            }

            RegisterMonsterSpawned();

            MonsterMovement oldMove = go.GetComponent<MonsterMovement>();
            if (oldMove != null) oldMove.enabled = false;

            BattleMonsterMovement newMove = go.GetComponent<BattleMonsterMovement>();
            if (newMove == null) newMove = go.AddComponent<BattleMonsterMovement>();

            newMove.Lane = lane;
            newMove.Speed = speed;
            go.transform.localScale = Vector3.one * scale;
        }
    }
}
