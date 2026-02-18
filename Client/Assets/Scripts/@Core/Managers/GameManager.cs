// using UnityEngine;

// public class GameManager : MonoBehaviour
// {
//     public GameObject unitPrefab; // 여기에 에셋이나 큐브 프리팹을 할당하세요.
//     private long userId = 100;    // 테스트용 유저 ID

//     // 버튼에 연결할 함수
//     public void OnClickSummon()
//     {
//         WWWForm form = new WWWForm();
//         form.AddField("userId", userId.ToString());

//         NetworkManager.Instance.Post("/summon", form, (json) => {
//             GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);
            
//             if (res.alien != null) {
//                 SpawnUnit(res.alien);
//                 Debug.Log($"소환 성공! 남은 골드: {res.remainingGold}");
//             }
//         }, (err) => Debug.LogError("소환 실패: " + err));
//     }

//     public void SpawnUnit(InGameAlien data)
//     {
//         // 3D 공간의 랜덤 좌표 (y값은 바닥 높이인 0.5f 정도)
//         Vector3 spawnPos = new Vector3(Random.Range(-4, 4), 0.5f, Random.Range(-4, 4));
//         GameObject unit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

//         // 데이터 명찰 달아주기
//         UnitData unitData = unit.GetComponent<UnitData>();
//         if (unitData != null) {
//             unitData.SetInfo(data);
//         }
//     }
// }

using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("설정")]
    public GameObject unitPrefab;
    public Transform gridParent;

    [Header("테스트 정보")]
    private long userId = 1;
    private float tileSize = 1.1f;
    private int cols = 7;
    private int rows = 4;

    // ★ 게임 켜지자마자 실행되는 함수
    void Start()
    {
        GameStart();
    }

    // 1. 게임 시작 신고 (이걸 해야 소환이 됨)
    void GameStart()
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());

        // "/start" 엔드포인트로 요청
        NetworkManager.Instance.Post("/start", form, (json) =>
        {
            Debug.Log($"[게임 시작 성공] 서버 응답: {json}");
        },
        (err) => Debug.LogError($"[게임 시작 실패] 서버 켜져 있나요? 에러: {err}"));
    }

    // 2. 소환 버튼 누르면 실행
    public void OnClickSummon()
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());

        NetworkManager.Instance.Post("/summon", form, (json) =>
        {
            Debug.Log($"[소환 시도] 서버 응답: {json}");

            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);

            // "진행 중인 게임이 없습니다" 에러가 또 뜨면 재시작 시도
            if (res.message.Contains("진행 중인 게임이 없습니다"))
            {
                Debug.LogWarning("게임이 안 켜져 있어서 다시 시작합니다...");
                GameStart();
                return;
            }

            if (res.alien != null)
            {
                SpawnUnit(res.alien);
            }
        }, (err) => Debug.LogError("통신 에러: " + err));
    }

    public void SpawnUnit(InGameAlien data)
    {
        float interval = 1.1f;
        float offsetX = (cols - 1) * interval * 0.5f;
        float offsetZ = (rows - 1) * interval * 0.5f;

        // 1. 비어있는 타일들 중에서 "진짜 랜덤"하게 하나를 가져옴
        Vector2Int finalGridPos = GetRandomEmptyTile();

        // 2. 만약 빈칸이 하나도 없다면 소환 취소
        if (finalGridPos.x == -1)
        {
            Debug.LogWarning("⚠️ 그리드에 빈자리가 없습니다!");
            return;
        }

        // 3. 최종 좌표 계산
        float localX = (finalGridPos.x * interval) - offsetX;
        float localZ = (finalGridPos.y * interval) - offsetZ;
        Vector3 finalPos = gridParent.position + new Vector3(localX, 0.8f, localZ);

        // 4. 소환
        GameObject unit = Instantiate(unitPrefab, finalPos, Quaternion.identity);
        unit.transform.SetParent(gridParent);
        
        // 중요: 유닛 이름을 좌표로 설정해야 다음 소환 때 IsTileOccupied가 인식함
        unit.name = $"Unit_{finalGridPos.x}_{finalGridPos.y}";

        UnitData unitData = unit.GetComponent<UnitData>();
        if (unitData != null) unitData.SetInfo(data);
        
        Debug.Log($"🎲 랜덤 소환 완료: {finalGridPos.x}, {finalGridPos.y}");
    }

    // 비어있는 모든 타일을 찾아서 그중 하나를 랜덤하게 뽑는 함수
    Vector2Int GetRandomEmptyTile()
    {
        // 1. 비어있는 좌표들을 담을 리스트 생성
        System.Collections.Generic.List<Vector2Int> emptyTiles = new System.Collections.Generic.List<Vector2Int>();

        // 2. 전체 그리드를 돌면서 빈칸을 리스트에 다 넣음
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (!IsTileOccupied(x, y))
                {
                    emptyTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        // 3. 빈칸이 있으면 랜덤하게 하나 선택, 없으면 -1 리턴
        if (emptyTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, emptyTiles.Count);
            return emptyTiles[randomIndex];
        }

        return new Vector2Int(-1, -1);
    }

    // 해당 칸에 유닛이 있는지 확인하는 함수
    bool IsTileOccupied(int x, int y)
    {
        // 이름으로 찾거나 거리를 체크하는 방식
        return GameObject.Find($"Unit_{x}_{y}") != null;
    }
    
    // 몬스터 죽였을때 이벤트
    public void OnKillMonster(long monsterSpecId)
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());
        form.AddField("monsterSpecId", monsterSpecId.ToString()); // 잡은 몬스터의 ID

        // ★ 서버로 처치 신고 전송 (/enemy/kill)
        NetworkManager.Instance.Post("/enemy/kill", form, (json) =>
        {
            Debug.Log($"💰 [처치 신고 성공] 서버 응답: {json}");

            // (선택사항) 서버가 보내준 남은 골드 정보로 UI 업데이트 가능
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);
            Debug.Log($"현재 보유 골드: {res.remainingGold}");

        }, (err) => Debug.LogError("처치 신고 실패: " + err));
    }
}