using UnityEngine;
using UnityEngine.UI; // UI 제어를 위해 추가

public class GameManager : MonoBehaviour
{
    [Header("설정 - 내 필드")]
    public GameObject unitPrefab;
    public Transform myGridParent; // 기존 gridParent 이름을 변경하거나 그대로 두고 Inspector에서 할당
    
    [Header("설정 - 상대방(Enemy) 필드")]
    public Transform enemyGridParent; // 적 필드의 부모 오브젝트

    [Header("UI 연결 (Inspector에서 꼭 끌어다 넣으세요!)")]
    public Button summonBtn; // 광클 방지(Disabled) 처리를 위한 소환 버튼
    public Text goldText;    // 하단 중앙에 있는 1200 적힌 골드 텍스트를 연결해주세요!
    public Text waveText;    // 상단 웨이브/몬스터 정보 텍스트를 연결해주세요!
    public Text oppText;     // 상단 파트너 정보 텍스트를 연결해주세요!

    [Header("테스트 정보")]
    private long userId = 1;       // 내 ID
    private long enemyId = 2;      // 가상의 상대방 ID
    private float tileSize = 1.1f;
    private int cols = 7;
    private int rows = 4;

    // ★ 상태 관리 (웹의 const [isLoading, setIsLoading] = useState(false) 와 동일)
    private bool isSummoning = false; 

    // 적 숫자 관리
    private int totalMonsters = 100;
    private int currentMonsters = 0;

    void Start()
    {
        GameStart();
    }

    // 1. 게임 시작 신고 (나와 상대방 모두)
    void GameStart()
    {
        // 내 세션 생성
        WWWForm myForm = new WWWForm();
        myForm.AddField("userId", userId.ToString());

        NetworkManager.Instance.Post("/start", myForm, (json) =>
        {
            Debug.Log($"[내 게임 시작 성공] 서버 응답: {json}");
            // 서버에서 응답 올 때 아직 골드 정보는 없지만 기본 500으로 맞춰줍니다.
            UpdateGoldUI(500);
            UpdateMonsterUI();
        },
        (err) => Debug.LogError($"[내 게임 시작 실패] 서버 켜져 있나요? 에러: {err}"));

        // 가상 상대방 세션 생성
        WWWForm enemyForm = new WWWForm();
        enemyForm.AddField("userId", enemyId.ToString());

        NetworkManager.Instance.Post("/start", enemyForm, (json) =>
        {
            Debug.Log($"[적 게임 시작 성공] 서버 응답: {json}");
        },
        (err) => Debug.LogError($"[적 게임 시작 실패] 에러: {err}"));
    }

    // 2. 소환 버튼 누르면 실행
    public void OnClickSummon()
    {
        // 🚨 디바운스(Debounce) & 광클 방지 처리
        if (isSummoning) return; 
        isSummoning = true;
        
        if (summonBtn != null) 
            summonBtn.interactable = false; // 버튼 비활성화 (HTML disabled=true)

        // 내 소환 요청
        WWWForm myForm = new WWWForm();
        myForm.AddField("userId", userId.ToString());

        NetworkManager.Instance.Post("/summon", myForm, (json) =>
        {
            // 통신 완료 시 다시 버튼 활성화
            isSummoning = false;
            if (summonBtn != null) summonBtn.interactable = true;

            Debug.Log($"[내 소환 시도] 서버 응답: {json}");

            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);

            if (res.message != null && res.message.Contains("진행 중인 게임이 없습니다"))
            {
                Debug.LogWarning("게임이 안 켜져 있어서 다시 시작합니다...");
                GameStart();
                return;
            }

            // [추가] 서버에서 남은 골드를 알려주면 UI 갱신!
            UpdateGoldUI(res.remainingGold);

            // 서버 파싱 데이터가 정상적으로 들어왔다면 소환! (내 필드에)
            if (res.alien != null)
            {
                SpawnUnit(res.alien, true); // true: 내 필드
            }
        }, (err) => 
        {
            // 에러가 나도 버튼은 다시 살려줘야 함
            isSummoning = false;
            if (summonBtn != null) summonBtn.interactable = true;
            Debug.LogError("통신 에러: " + err);
        });

        // ==========================================
        // 💡 [테스트용] 내가 소환할 때 상대방도 같이 소환되게 하기
        // (실제 게임에서는 상대방이 알아서 버튼을 누를 때 호출됨)
        // ==========================================
        WWWForm enemyForm = new WWWForm();
        enemyForm.AddField("userId", enemyId.ToString());

        NetworkManager.Instance.Post("/summon", enemyForm, (json) =>
        {
            Debug.Log($"[적 소환 시도] 서버 응답: {json}");
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);
            if (res.alien != null)
            {
                SpawnUnit(res.alien, false); // false: 적 필드
            }
        }, (err) => Debug.Log("상대방 소환 에러 (무시가능)"));
    }

    // 3. 유닛 소환 (클라이언트 랜덤 삭제, 서버 데이터 100% 신뢰)
    // isMine 파라미터를 추가하여 내 필드인지 상대 필드인지 구분합니다.
    public void SpawnUnit(InGameAlien data, bool isMine)
    {
        float interval = 1.1f;
        // CSS의 margin: 0 auto 처럼 중앙 정렬을 위한 오프셋
        float offsetX = (cols - 1) * interval * 0.5f;
        float offsetZ = (rows - 1) * interval * 0.5f;

        // 🚨 핵심 수정: x와 y를 바꾸어서 매핑합니다!
        // 서버의 grid[4][7] 구조: 첫번째 인덱스(0~3)가 세로(row, y축), 두번째 인덱스(0~6)가 가로(col, x축)입니다.
        int x = data.gridY; // 서버의 0~6 값이 가로 축 (x)
        int y = data.gridX; // 서버의 0~3 값이 세로 축 (z/y)

        // 최종 3D 월드 좌표 계산 (x가 가로, y가 세로(z)로 매핑)
        float localX = (x * interval) - offsetX;
        float localZ = (y * interval) - offsetZ;

        // 내 필드인지 상대 필드인지에 따라 부모 Transform 결정
        Transform targetGridParent = isMine ? myGridParent : enemyGridParent;

        // 타겟 그리드 부모가 할당되지 않은 경우 (Null 에러 방지)
        if (targetGridParent == null)
        {
            if (isMine)
            {
                Debug.LogError("🚨 My Grid Parent 가 할당되지 않았습니다! Inspector에서 내 필드를 넣어주세요.");
            }
            else
            {
                Debug.LogWarning("상대방 필드(Enemy Grid Parent)가 할당되지 않았습니다. Inspector를 확인하세요.");
            }
            return;
        }

        Vector3 finalPos = targetGridParent.position + new Vector3(localX, 0.8f, localZ);

        // 소환 (DOM 렌더링)
        GameObject unit = Instantiate(unitPrefab, finalPos, Quaternion.identity);
        unit.transform.SetParent(targetGridParent);
        
        // 중요: 유닛 이름(DOM ID)을 서버 좌표와 동일하게 설정 (누구 것인지도 표기)
        string owner = isMine ? "Me" : "Enemy";
        unit.name = $"Unit_{owner}_{data.gridX}_{data.gridY}";

        // 데이터 바인딩
        UnitData unitData = unit.GetComponent<UnitData>();
        if (unitData != null) unitData.SetInfo(data);
        
        Debug.Log($"👽 [{owner} 소환 완료] 왹져가 DB 좌표 ({data.gridX}, {data.gridY}) -> 클라 좌표 ({x}, {y}) 에 배치되었습니다!");
    }

    // 4. 몬스터 처치 신고
    public void OnKillMonster(long monsterSpecId)
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());
        form.AddField("monsterSpecId", monsterSpecId.ToString()); 

        NetworkManager.Instance.Post("/enemy/kill", form, (json) =>
        {
            Debug.Log($"💰 [처치 신고 성공] 서버 응답: {json}");

            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);

            // [추가] 몬스터 잡고 돈 벌었을 때 UI 갱신
            UpdateGoldUI(res.remainingGold);

        }, (err) => Debug.LogError("처치 신고 실패: " + err));
    }

    // [추가] 골드 UI 텍스트 변경 함수
    private void UpdateGoldUI(int currentGold)
    {
        if (goldText != null)
        {
            goldText.text = $"💰 {currentGold:N0}"; // N0 포맷으로 1,000 단위 콤마 찍기
        }
    }

    // [추가] 몬스터 추가 시 호출 (테스트용)
    [ContextMenu("테스트: 몬스터 1마리 추가")]
    public void TestAddMonster()
    {
        if (currentMonsters < totalMonsters)
        {
            currentMonsters++;
            UpdateMonsterUI();

            if (currentMonsters >= totalMonsters)
            {
                TriggerGameOver();
            }
        }
    }

    // [추가] 몬스터 UI 업데이트
    private void UpdateMonsterUI()
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE 1\n몬스터: {currentMonsters} / {totalMonsters}";
        }
    }

    // [추가] 게임 오버 처리
    private void TriggerGameOver()
    {
        Debug.Log("💀 [게임 오버] 몬스터가 100마리를 초과했습니다!");
        if (waveText != null)
        {
            waveText.text = $"💀 GAME OVER 💀\n(패배)";
            waveText.color = Color.red;
        }

        // 서버에도 게임 오버 알리기
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());
        NetworkManager.Instance.Post("/gameover", form, (json) =>
        {
            Debug.Log("서버에 게임 오버 등록 완료");
        }, (err) => Debug.LogError("게임 오버 등록 실패"));

        // 소환 버튼 비활성화
        if (summonBtn != null)
        {
            summonBtn.interactable = false;
        }
    }
}