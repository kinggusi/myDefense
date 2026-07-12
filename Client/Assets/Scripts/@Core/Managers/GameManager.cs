using UnityEngine;
using UnityEngine.UI; // UI 제어를 위해 추가

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("설정 - 내 필드")]
    public GameObject unitPrefab;
    public GameObject injectorPrefab; // 인젝터 프리팹 참조 추가
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
    private int cols = 6;
    private int rows = 4;

    private void Awake()
    {
        Instance = this;
        if (NetworkManager.Instance == null)
        {
            GameObject nmObj = new GameObject("NetworkManager");
            nmObj.AddComponent<NetworkManager>();
            Debug.Log("[GameManager] NetworkManager 싱글톤이 존재하지 않아 임시 동적 스폰을 실행했습니다.");
        }
    }

    // --- API 경로 상수 ---
    private const string ApiStart = "/game/start";
    private const string ApiSummon = "/game/summon";
    private const string ApiKill = "/game/enemy/kill";
    private const string ApiGameOver = "/game/gameover";

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

        NetworkManager.Instance.Post(ApiStart, myForm, (json) =>
        {
            Debug.Log($"[내 게임 시작 성공] 서버 응답: {json}");
            UpdateGoldUI(500);
            UpdateMonsterUI();
        },
        (err) => {
            Debug.LogError($"[내 게임 시작 실패] 서버 켜져 있나요? 에러: {err}");
        });

        // 가상 상대방 세션 생성
        WWWForm enemyForm = new WWWForm();
        enemyForm.AddField("userId", enemyId.ToString());

        NetworkManager.Instance.Post(ApiStart, enemyForm, (json) =>
        {
            Debug.Log($"[적 게임 시작 성공] 서버 응답: {json}");
        },
        (err) => {
            Debug.LogError($"[적 게임 시작 실패] 에러: {err}");
        });
    }

    // 2. 소환 버튼 누르면 실행
    public void OnClickSummon()
    {
        if (isSummoning) return;
        isSummoning = true;

        if (summonBtn != null)
            summonBtn.interactable = false;

        // 내 소환 요청
        WWWForm myForm = new WWWForm();
        myForm.AddField("userId", userId.ToString());

        NetworkManager.Instance.Post(ApiSummon, myForm, (json) =>
        {
            isSummoning = false;
            if (summonBtn != null) summonBtn.interactable = true;

            Debug.Log($"[내 소환 시도] 서버 응답: {json}");

            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);

            if (res == null || res.alien == null)
            {
                Debug.LogWarning($"[GameManager] 소환 응답 본문 또는 alien 정보 누락: {(res != null ? res.message : "")}");
                return;
            }

            if (res.message != null && res.message.Contains("진행 중인 게임이 없습니다"))
            {
                Debug.LogWarning("게임이 안 켜져 있어서 다시 시작합니다...");
                GameStart();
                return;
            }

            // 골드 UI 갱신 (조기 리턴 전에 수행!)
            UpdateGoldUI(res.remainingGold);

            // 타입 판별 분기
            if (res.alien.alienSpec != null && !string.IsNullOrEmpty(res.alien.alienSpec.name))
            {
                SpawnUnit(res.alien, true); // Alien 스폰
            }
            else if (res.alien.objectType == "MUTATION_INJECTOR")
            {
                SpawnMutationInjector(res.alien, true); // Mutation Injector 스폰
            }
            else
            {
                Debug.LogWarning($"[GameManager] 알 수 없는 BoardObject 스폰 건너뜀 (objectType: {res.alien.objectType}, msg: {res.message})");
            }
        }, (err) => 
        {
            isSummoning = false;
            if (summonBtn != null) summonBtn.interactable = true;
            Debug.LogError("통신 에러: " + err);
        });

        // ==========================================
        // 💡 [테스트용] 상대방도 함께 소환 트리거
        // ==========================================
        WWWForm enemyForm = new WWWForm();
        enemyForm.AddField("userId", enemyId.ToString());

        NetworkManager.Instance.Post(ApiSummon, enemyForm, (json) =>
        {
            Debug.Log($"[적 소환 시도] 서버 응답: {json}");
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);

            if (res == null || res.alien == null)
            {
                Debug.LogWarning("[GameManager] 적 소환 응답 본문 또는 데이터 누락");
                return;
            }

            if (res.alien.alienSpec != null && !string.IsNullOrEmpty(res.alien.alienSpec.name))
            {
                SpawnUnit(res.alien, false);
            }
            else if (res.alien.objectType == "MUTATION_INJECTOR")
            {
                SpawnMutationInjector(res.alien, false);
            }
            else
            {
                Debug.LogWarning($"[GameManager] 알 수 없는 적 BoardObject 스킵 (objectType: {res.alien.objectType}, msg: {res.message})");
            }
        }, (err) => {
            Debug.Log("상대방 소환 에러 (무시가능)");
        });
    }

public void SpawnUnit(InGameAlien data, bool isMine)
    {
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

        // 🚨 핵심 수정: 수식 추정 대신 타일 Transform 검색 및 정렬
        // 서버의 gridX(0~3)는 세로(row, z), gridY(0~5)는 가로(col, x)입니다.
        // 타일 이름은 Tile_{gridY}_{gridX} 입니다.
        string targetTileName = $"Tile_{data.gridY}_{data.gridX}";
        Transform targetTile = targetGridParent.Find(targetTileName);

        Vector3 finalPos;
        if (targetTile != null)
        {
            // 타일이 발견되면 타일 정중앙 + Y오프셋(0.8f) 배치
            finalPos = targetTile.position + new Vector3(0, 0.8f, 0);
            Debug.Log($"👽 [SpawnUnit] Target tile '{targetTileName}' found! Aligning unit perfectly to center.");
        }
        else
        {
            // 타일을 못 찾는 극단적 예외 상황을 위한 기존 수학 수식 폴백(Fallback) 방어막
            float interval = 1.1f;
            float offsetX = (cols - 1) * interval * 0.5f;
            float offsetZ = (rows - 1) * interval * 0.5f;
            int x = data.gridY;
            int y = data.gridX;
            float localX = (x * interval) - offsetX;
            float localZ = (y * interval) - offsetZ;
            finalPos = targetGridParent.position + new Vector3(localX, 0.8f, localZ);
            Debug.LogWarning($"[SpawnUnit] Target tile '{targetTileName}' NOT found! Falling back to mathematical approximation.");
        }

        // 소환 (DOM 렌더링)
        GameObject unit = Instantiate(unitPrefab, finalPos, Quaternion.identity);
        unit.transform.SetParent(targetGridParent);
        
        // 중요: 유닛 이름(DOM ID)을 서버 좌표와 동일하게 설정 (누구 것인지도 표기)
        string owner = isMine ? "Me" : "Enemy";
        unit.name = $"Unit_{owner}_{data.gridX}_{data.gridY}";

        // 데이터 바인딩
        UnitData unitData = unit.GetComponent<UnitData>();
        if (unitData != null)
        {
            unitData.SetInfo(data);
            unitData.isMine = isMine;
        }
        
        Debug.Log($"👽 [{owner} 소환 완료] 왹져가 DB 좌표 ({data.gridX}, {data.gridY}) -> 클라 좌표 ({data.gridY}, {data.gridX}) 에 배치되었습니다!");
    }

    public void SpawnMutationInjector(InGameAlien data, bool isMine)
    {
        Transform targetGridParent = isMine ? myGridParent : enemyGridParent;

        if (targetGridParent == null)
        {
            Debug.LogError($"[SpawnMutationInjector] {(isMine ? "My" : "Enemy")} Grid Parent가 할당되지 않았습니다.");
            return;
        }

        if (injectorPrefab == null)
        {
            Debug.LogError("[SpawnMutationInjector] injectorPrefab이 할당되지 않았습니다.");
            return;
        }

        string targetTileName = $"Tile_{data.gridY}_{data.gridX}";
        Transform targetTile = targetGridParent.Find(targetTileName);

        if (targetTile != null)
        {
            Vector3 finalPos = targetTile.position + new Vector3(0, 0.8f, 0);
            GameObject injector = Instantiate(injectorPrefab, finalPos, Quaternion.identity);
            injector.transform.SetParent(targetGridParent);
            
            string owner = isMine ? "Me" : "Enemy";
            injector.name = $"MutationInjector_{owner}_{data.gridX}_{data.gridY}";

            MutationInjectorData injectorData = injector.GetComponent<MutationInjectorData>();
            if (injectorData != null)
            {
                injectorData.Initialize(data.id, data.mutationType, data.gridX, data.gridY, isMine);
            }

            Debug.Log($"💊 [{owner} 인젝터 소환 완료] 인젝터가 DB 좌표 ({data.gridX}, {data.gridY}) -> 클라 좌표 ({data.gridY}, {data.gridX}) 에 배치되었습니다!");
        }
        else
        {
            Debug.LogError($"[SpawnMutationInjector] 🚨 목적 타일 '{targetTileName}' 을 찾을 수 없어 인젝터 생성을 중단하였습니다.");
        }
    }

    // 4. 몬스터 처치 신고
    public void OnKillMonster(long monsterSpecId)
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());
        form.AddField("monsterSpecId", monsterSpecId.ToString()); 

        NetworkManager.Instance.Post(ApiKill, form, (json) =>
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
        NetworkManager.Instance.Post(ApiGameOver, form, (json) =>
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