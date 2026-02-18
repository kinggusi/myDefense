using UnityEngine;
using System.Collections; // Coroutine을 쓰기 위해 필수

public class WaveManager : MonoBehaviour
{
    [Header("생성 정보")]
    public GameObject monsterPrefab; // 찍어낼 몬스터 도장 (Prefab)
    public Transform spawnPoint;     // 소환될 위치 (WP0)
    
    [Header("웨이브 설정")]
    public float spawnInterval = 1.0f; // 몇 초마다 나올지 (Interval)

    private void Start()
    {
        // 게임 시작하면 '생산 라인' 가동!
        StartCoroutine(SpawnWave());
    }

    // JS의 setInterval과 비슷한 역할을 하는 '코루틴'입니다.
    IEnumerator SpawnWave()
    {
        while (true) // 무한 루프 (일단 계속 나옵니다)
        {
            SpawnMonster();
            // 지정한 시간(1초)만큼 대기하고 다시 루프
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnMonster()
    {
        if (monsterPrefab == null || spawnPoint == null) 
        {
            Debug.LogError("프리팹이나 스폰 위치가 연결되지 않았습니다!");
            return;
        }

        // Instantiate = new Monster() 와 같습니다. (생성, 위치, 회전)
        Instantiate(monsterPrefab, spawnPoint.position, Quaternion.identity);
    }
}