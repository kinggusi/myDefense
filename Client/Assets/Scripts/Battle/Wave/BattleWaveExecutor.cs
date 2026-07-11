using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MyDefense.Battle
{
    public class BattleWaveExecutor : MonoBehaviour
    {
        public static BattleWaveExecutor Instance { get; private set; }

        [Header("웨이브 설정")]
        [SerializeField] private GameObject _monsterPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _spawnInterval = 1.0f;
        [SerializeField] private int _monstersPerWave = 10;
        
        [Header("보스전 설정")]
        [SerializeField] private float _bossTimeLimit = 30f; // 테스트용 기본값 30초
        [SerializeField] private float _bossSpeed = 2f;

        [Header("런타임 정보")]
        [SerializeField] private int _currentRound = 0;
        [SerializeField] private bool _isWaveRunning = false;
        [SerializeField] private bool _isBossActive = false;

        private GameObject _currentBossInstance = null;
        private Coroutine _bossTimerCoroutine = null;

        // --- 외부 도메인 구독용 이벤트 목록 ---
        public event System.Action OnBossTimeout;               // 보스 타임아웃 만료 알림
        public event System.Action<float> OnBossTimerTick;      // 남은 시간 갱신 알림 (UI 바인딩용)
        public event System.Action OnBossDefeated;              // 보스 처치 성공 알림

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
        }

        private void Update()
        {
            // 보스 전투 중일 때 보스 사망(오브젝트 파괴) 실시간 체크
            if (_isBossActive && _currentBossInstance == null)
            {
                HandleBossDefeated();
            }
        }

        [ContextMenu("다음 웨이브 시작 (StartNextWave)")]
        public void StartNextWave()
        {
            // 1. 보스가 활동 중이면 차단
            if (_isBossActive)
            {
                Debug.LogWarning("[BattleWaveExecutor] 🚨 보스가 아직 필드에 존재하여 다음 웨이브를 시작할 수 없습니다!");
                return;
            }

            // 2. 이미 웨이브가 돌아가고 있으면 차단
            if (_isWaveRunning)
            {
                Debug.LogWarning("[BattleWaveExecutor] 🚨 이미 웨이브가 진행 중입니다.");
                return;
            }

            _currentRound++;
            _isWaveRunning = true;

            Debug.Log($"[BattleWaveExecutor] ▶ 라운드 {_currentRound} 시작!");

            // 10의 배수 라운드 체크 -> 보스 스폰 분기
            if (_currentRound % 10 == 0)
            {
                StartCoroutine(SpawnBossRoutine());
            }
            else
            {
                StartCoroutine(SpawnRegularWaveRoutine());
            }
        }

        // 일반 웨이브 코루틴
        private IEnumerator SpawnRegularWaveRoutine()
        {
            bool testToggle = false;

            for (int i = 0; i < _monstersPerWave; i++)
            {
                SpawnMonster(testToggle ? LaneType.Player1Lane : LaneType.Player2Lane, 5f, 1f);
                testToggle = !testToggle;
                yield return new WaitForSeconds(_spawnInterval);
            }

            _isWaveRunning = false;
            Debug.Log($"[BattleWaveExecutor] ■ 라운드 {_currentRound} 일반 웨이브 스폰 종료.");
        }

        // 보스 스폰 코루틴
        private IEnumerator SpawnBossRoutine()
        {
            _isBossActive = true;
            Debug.Log($"[BattleWaveExecutor] 👹 10의 배수 라운드 진입! 보스 출현!");

            // 보스 몬스터 생성
            Vector3 finalSpawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            _currentBossInstance = Instantiate(_monsterPrefab, finalSpawnPos, Quaternion.identity);

            // 구형 이동 스크립트 강제 비활성화
            MonsterMovement oldMove = _currentBossInstance.GetComponent<MonsterMovement>();
            if (oldMove != null) oldMove.enabled = false;

            // 새 이동 스크립트 연결 및 보스 레인 강제
            BattleMonsterMovement newMove = _currentBossInstance.GetComponent<BattleMonsterMovement>();
            if (newMove == null) newMove = _currentBossInstance.AddComponent<BattleMonsterMovement>();

            newMove.Lane = LaneType.BossSharedLane;
            newMove.Speed = _bossSpeed;
            _currentBossInstance.transform.localScale = Vector3.one * 2.0f; // 크기 확대

            // 보스 타이머 가동
            if (_bossTimerCoroutine != null) StopCoroutine(_bossTimerCoroutine);
            _bossTimerCoroutine = StartCoroutine(BossTimerRoutine());

            yield return null;
            _isWaveRunning = false; // 보스 스폰 자체는 끝났으므로 웩져 스폰 러프 상태는 끎 (보스 락만 유지)
        }

        // 보스 제한시간 카운트다운 타이머
        private IEnumerator BossTimerRoutine()
        {
            float timeLeft = _bossTimeLimit;

            while (timeLeft > 0)
            {
                OnBossTimerTick?.Invoke(timeLeft);
                yield return new WaitForSeconds(1.0f);
                timeLeft -= 1.0f;

                // 보스가 중간에 처치되면 타이머 루프 즉시 탈출
                if (!_isBossActive) yield break;
            }

            // 제한시간 종료 처리
            OnBossTimerTick?.Invoke(0f);
            Debug.LogError($"[BattleWaveExecutor] 💀 보스 제한시간 {_bossTimeLimit}초 초과! 배틀 미션 실패 조건 충족.");
            
            // 외부 구독자들에게 실패 상태 전달
            OnBossTimeout?.Invoke();
        }

        // 보스 처치 완료 시점 호출
        private void HandleBossDefeated()
        {
            _isBossActive = false;
            if (_bossTimerCoroutine != null)
            {
                StopCoroutine(_bossTimerCoroutine);
                _bossTimerCoroutine = null;
            }

            Debug.Log($"[BattleWaveExecutor] 🎉 보스 처치 완료! 다음 라운드 진입 조건 해제.");
            OnBossDefeated?.Invoke();
        }

        // 일반 몬스터 스폰 편의 함수
        private void SpawnMonster(LaneType lane, float speed, float scale)
        {
            if (_monsterPrefab == null) return;

            Vector3 finalSpawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            GameObject go = Instantiate(_monsterPrefab, finalSpawnPos, Quaternion.identity);

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
