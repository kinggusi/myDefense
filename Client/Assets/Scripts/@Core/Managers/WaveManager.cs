using UnityEngine;
using System.Collections;
using MyDefense.Battle;

public class WaveManager : MonoBehaviour
{
    [Header("생성 정보")]
    public GameObject monsterPrefab; // 찍어낼 몬스터 도장 (Prefab)
    public Transform spawnPoint;     // 소환될 위치 (WP0)
    
    [Header("웨이브 설정")]
    public float spawnInterval = 1.0f; // 몇 초마다 나올지 (Interval)
    public bool autoStart = false;     // 레거시 스포너 자동시작 정지 (신규 실행기와 중복 방지)

    // ★ [테스트 전용] P1/P2 레인 번갈아가며 스폰하기 위한 로컬 디버그용 상태값
    private bool _testSpawnToggle = false;

    private void Start()
    {
        if (autoStart)
        {
            // 게임 시작하면 '생산 라인' 가동!
            StartCoroutine(SpawnWave());
        }
    }

    // JS의 setInterval과 비슷한 역할을 하는 '코루틴'입니다.
    IEnumerator SpawnWave()
    {
        while (true) // 무한 루프 (일단 계속 나옵니다)
        {
            SpawnMonsterTestOnly();
            // 지정한 시간(1초)만큼 대기하고 다시 루프
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>
    /// ★ [테스트 전용] P1/P2 번갈아 가며 배치하는 테스트 스폰 유틸리티입니다.
    /// 실제 게임 기획 상의 최종 스폰 로직이 아닙니다.
    /// </summary>
    public void SpawnMonsterTestOnly()
    {
        if (monsterPrefab == null) 
        {
            Debug.LogError("프리팹이 연결되지 않았습니다!");
            return;
        }

        Vector3 finalSpawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject go = Instantiate(monsterPrefab, finalSpawnPos, Quaternion.identity);

        // 구 버전 이동 스크립트 비활성화
        MonsterMovement oldMove = go.GetComponent<MonsterMovement>();
        if (oldMove != null)
        {
            oldMove.enabled = false;
        }

        // 신규 이동 스크립트 설정
        BattleMonsterMovement newMove = go.GetComponent<BattleMonsterMovement>();
        if (newMove == null)
        {
            newMove = go.AddComponent<BattleMonsterMovement>();
        }

        // 라인 설정: P1과 P2 레인을 번갈아 가며 할당
        LaneType targetLane = _testSpawnToggle ? LaneType.Player1Lane : LaneType.Player2Lane;
        newMove.Lane = targetLane;
        
        _testSpawnToggle = !_testSpawnToggle;
    }

    [ContextMenu("테스트: 보스 스폰 (공용 레인)")]
    public void SpawnBossTest()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("보스 스폰 테스트 실패: monsterPrefab이 지정되지 않았습니다.");
            return;
        }

        // 보스 스폰
        Vector3 finalSpawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject go = Instantiate(monsterPrefab, finalSpawnPos, Quaternion.identity);

        // 구 버전 이동 스크립트 비활성화
        MonsterMovement oldMove = go.GetComponent<MonsterMovement>();
        if (oldMove != null)
        {
            oldMove.enabled = false;
        }

        // 신규 이동 스크립트 설정
        BattleMonsterMovement newMove = go.GetComponent<BattleMonsterMovement>();
        if (newMove == null)
        {
            newMove = go.AddComponent<BattleMonsterMovement>();
        }

        // 보스는 공용 레인으로 이동하도록 강제
        newMove.Lane = LaneType.BossSharedLane;
        newMove.Speed = 3f; // 보스는 조금 더 천천히 가도록 속도 설정

        // 크기도 보스답게 스케일링
        go.transform.localScale = Vector3.one * 2.0f;

        Debug.Log("👹 공용 보스 레인으로 이동하는 보스 테스트 스폰 완료!");
    }
}