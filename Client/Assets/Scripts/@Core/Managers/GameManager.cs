using UnityEngine;
using UnityEngine.UI; // UI 제어를 위해 추가
using MyDefense.Battle.Runtime;

public class GameManager : MonoBehaviour
{
    [Header("설정 - 내 필드")]
    public GameObject unitPrefab;
    public GameObject injectorPrefab; // 신규 추가: 인젝터 프리팹 에셋 슬롯
    public Transform myGridParent; // 기존 gridParent 이름을 변경하거나 그대로 두고 Inspector에서 할당
    
    [Header("설정 - 상대방(Enemy) 필드")]
    public Transform enemyGridParent; // 적 필드의 부모 오브젝트

    [Header("UI 연결 (Inspector에서 꼭 끌어다 넣으세요!)")]
    public Button summonBtn; // 광클 방지(Disabled) 처리를 위한 소환 버튼
    public Text goldText;    // 하단 중앙에 있는 1200 적힌 골드 텍스트를 연결해주세요!
    public Text waveText;    // 상단 웨이브/몬스터 정보 텍스트를 연결해주세요!
    public Text oppText;     // 상단 파트너 정보 텍스트를 연결해주세요!
    public Text summonCostText; // 신규: 소환 비용 동기화 텍스트

    [Header("테스트 정보")]
    private long userId = 1;       // 내 ID
    private long enemyId = 2;      // 가상의 상대방 ID
    public long UserId => userId;  // 읽기전용 프로퍼티 제공
    private float tileSize = 1.1f;
    private int cols = 6;
    private int rows = 4;

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
        UpdateKidnapCostUI(0);
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
            // 최초 게임 진입 시 서버 보드 상태 동기화 기동
            RequestSyncBoardState();
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
        if (!BattlePlayerActionGate.CanUseBattleAction("Kidnap")) return;
        // 🚨 디바운스(Debounce) & 동기화 중 & 게임 오버 상태 가드
        if (isSummoning || isSyncingBoard || isGameOver) return;
        isSummoning = true;
        
        if (summonBtn != null) 
            summonBtn.interactable = false; // 버튼 비활성화 (HTML disabled=true)

        // 내 소환 요청 (비동기 POST 공통 처리)
        string requestUri = "/game/summon?userId=" + userId;
        NetworkManager.Instance.PostJsonAsync<EmptyRequestBody, GameResponseObjectDto>(requestUri, new EmptyRequestBody(), (result) =>
        {
            if (result.IsSuccess)
            {
                // 1. result.Data 및 alien null 검증
                GameResponseObjectDto res = result.Data;
                if (res != null && res.alien != null)
                {
                    // 2. SpawnBoardObject 처리 및 3. 로컬 등록 성공 여부 확인
                    bool isLocallySpawned = SpawnBoardObject(res.alien, true);

                    // 4. remainingGold 갱신 (서버 성공 시 UI 골드는 무조건 동기화하여 갱신)
                    UpdateGoldUI(res.remainingGold);
                    UpdateKidnapCostUI(res.currentKidnapCost);

                    if (!isLocallySpawned)
                    {
                        // 서버 상태는 이미 변경됐는데(골드차감됨) 로컬 생성이 실패한 불일치 경고 로그
                        Debug.LogError($"🚨 [로컬 동기화 실패] 서버 소환은 성공(골드차감됨)하였으나 로컬 보드 오브젝트 생성에 실패했습니다. (서버 객체 ID: {res.alien.id}, 타입: {res.alien.objectType}). 추후 보드 상태 전수 동기화 API 호출이 필요합니다. [TODO: SyncBoardState API]");
                    }
                }
                else
                {
                    Debug.LogError("🚨 [소환 성공 응답] 서버 성공 응답에 유효한 유닛 데이터가 누락되었습니다.");
                }

                // 5. 버튼 잠금 해제 (성공 분기)
                isSummoning = false;
                if (summonBtn != null) summonBtn.interactable = true;
            }
            else
            {
                // 모든 실패 경로에서 버튼 잠금 해제 및 디바운스 초기화 보장
                isSummoning = false;
                if (summonBtn != null) summonBtn.interactable = true;

                // 실패 처리 (에러 코드 분기)
                if (result.Error != null)
                {
                    string errCode = result.Error.code;
                    if (errCode == "BOARD_FULL")
                    {
                        Debug.LogWarning("🚨 [소환 실패] 보드가 가득 찼습니다.");
                    }
                    else if (errCode == "INSUFFICIENT_GOLD")
                    {
                        Debug.LogWarning("🚨 [소환 실패] 골드가 부족합니다.");
                    }
                    else if (errCode == "GAME_SESSION_NOT_FOUND")
                    {
                        Debug.LogError("🚨 [소환 실패] 진행 중인 게임을 찾을 수 없습니다.");
                    }
                    else if (errCode == "GAME_ALREADY_OVER")
                    {
                        Debug.LogError("🚨 [소환 실패] 이미 종료된 게임입니다.");
                    }
                    else
                    {
                        Debug.LogError($"🚨 [소환 실패] 비즈니스 에러 (코드: {errCode}): {result.Error.message}");
                    }
                }
                else if (!string.IsNullOrEmpty(result.NetworkError))
                {
                    // 네트워크 장애 대응
                    string netErr = result.NetworkError;
                    if (netErr.Contains("TIMEOUT_ERROR"))
                    {
                        Debug.LogError("🚨 [네트워크 오류] 서버 응답 시간이 초과되었습니다.");
                    }
                    else if (netErr.Contains("CONNECTION_FAILED"))
                    {
                        Debug.LogError("🚨 [네트워크 오류] 서버에 연결할 수 없습니다.");
                    }
                    else if (netErr.Contains("JSON_PARSE_ERROR") || netErr.Contains("DATA_PROCESSING_ERROR"))
                    {
                        Debug.LogError("🚨 [네트워크 오류] 서버 응답을 처리하지 못했습니다.");
                    }
                    else
                    {
                        Debug.LogError($"🚨 [네트워크 오류] {netErr}");
                    }
                }
            }
        });

        // ==========================================
        // 💡 [적 소환 시도] 통합 테스트 씬에서는 상대방 소환을 진행하지 않고 비워 둡니다.
        // ==========================================
    }

    // 💡 신규 BoardObjectDto 다형성 수용 어댑터 메서드
    private bool SpawnBoardObject(BoardObjectDto data, bool isMine)
    {
        if (data == null)
        {
            Debug.LogError("🚨 [SpawnBoardObject] 전달된 BoardObjectDto 데이터가 null 입니다.");
            return false;
        }

        if (string.IsNullOrEmpty(data.objectType))
        {
            Debug.LogError("🚨 [SpawnBoardObject] 데이터의 objectType이 누락되었습니다.");
            return false;
        }

        if (data.objectType == BoardObjectDto.TypeAlien)
        {
            // BoardObjectDto -> InGameAlien 모델 변환 (기존 SpawnUnit 호환성 보장)
            InGameAlien alien = new InGameAlien();
            alien.id = data.id;
            alien.gridX = data.gridX;
            alien.gridY = data.gridY;
            alien.pendingMutationType = data.pendingMutationType;
            alien.activeMutationType = data.activeMutationType;
            alien.mutationRerollCount = data.mutationRerollCount;
            alien.alienSpec = data.alienSpec;

            SpawnUnit(alien, isMine);
            return true;
        }
        else if (data.objectType == BoardObjectDto.TypeInjector)
        {
            return SpawnInjector(data, isMine);
        }
        else
        {
            Debug.LogError($"🚨 [SpawnBoardObject] 지원하지 않는 알 수 없는 objectType 감지: {data.objectType}");
            return false;
        }
    }

    // 💡 신규 SpawnInjector 구현 (프리팹 기반의 실제 인스턴스화 및 데이터 정합성 수립)
    private bool SpawnInjector(BoardObjectDto data, bool isMine)
    {
        if (data == null)
        {
            Debug.LogError("🚨 [SpawnInjector] BoardObjectDto 데이터가 null 입니다.");
            return false;
        }

        Transform targetGridParent = isMine ? myGridParent : enemyGridParent;
        if (targetGridParent == null)
        {
            Debug.LogError("🚨 [SpawnInjector] targetGridParent가 null 입니다.");
            return false;
        }

        // 1. 타일 위치 검색 및 정렬
        string targetTileName = $"Tile_{data.gridY}_{data.gridX}";
        Transform targetTile = targetGridParent.Find(targetTileName);
        if (targetTile == null)
        {
            Debug.LogError($"🚨 [SpawnInjector] 대상 타일 '{targetTileName}' 을 찾을 수 없습니다.");
            return false;
        }

        // 2. 인젝터 프리팹 검증
        if (injectorPrefab == null)
        {
            Debug.LogError("🚨 [SpawnInjector] GameManager에 injectorPrefab이 연결되지 않았습니다! (리소스 부재)");
            return false;
        }

        GameObject injectorObj = null;
        try
        {
            // 3. 인스턴스화 및 타일 위치 정밀 배치
            Vector3 finalPos = targetTile.position + new Vector3(0, 0.8f, 0);
            injectorObj = Instantiate(injectorPrefab, finalPos, Quaternion.identity);
            injectorObj.transform.SetParent(targetGridParent);

            // 4. 이름 및 태그 설정
            string owner = isMine ? "Me" : "Enemy";
            injectorObj.name = $"Injector_{owner}_{data.gridX}_{data.gridY}";
            
            // 머지/전투 시의 안전 분리를 위해 인젝터 전용 태그 설정 또는 Untagged 유지
            injectorObj.tag = "Untagged"; // Unit 태그를 주지 않아 전투 타겟 및 머지 합성군에서 배제

            // 5. InjectorData 컴포넌트 정보 바인딩 (프리팹 컴포넌트 필수 검증 정책)
            InjectorData injectorData = injectorObj.GetComponent<InjectorData>();
            if (injectorData == null)
            {
                Debug.LogError("🚨 [SpawnInjector] 생성된 프리팹에 필수 컴포넌트인 'InjectorData' 가 누락되었습니다!");
                Destroy(injectorObj); // 실패한 잔해물 정리
                return false;
            }

            injectorData.serverId = data.id;
            injectorData.gridX = data.gridX;
            injectorData.gridY = data.gridY;
            injectorData.mutationType = data.mutationType;
            injectorData.isMine = isMine;

            if (isMine)
            {
                InjectorDrag drag = injectorObj.GetComponent<InjectorDrag>();
                if (drag == null)
                {
                    drag = injectorObj.AddComponent<InjectorDrag>();
                }
                drag.enabled = true;

                Collider col = injectorObj.GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }

            Debug.Log($"🎉 [SpawnInjector] 인젝터 씬 배치 성공! ID: {data.id}, DNA: {data.mutationType}, 위치: ({data.gridX}, {data.gridY}), 소유: {owner}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🚨 [SpawnInjector] 씬 인스턴스화 과정 중 예외가 발생했습니다: {ex.Message}");
            if (injectorObj != null)
            {
                Destroy(injectorObj); // 실패한 잔해물 정리
            }
            return false;
        }
    }

    // 3. 유닛 소환 (클라이언트 랜덤 삭제, 서버 데이터 100% 신뢰)
    // isMine 파라미터를 추가하여 내 필드인지 상대 필드인지 구분합니다.
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
        if (unitData != null) unitData.SetInfo(data);

        // 내 유닛인 경우 입력 및 드래그 활성화
        if (isMine)
        {
            UnitDrag drag = unit.GetComponent<UnitDrag>();
            if (drag == null)
            {
                drag = unit.AddComponent<UnitDrag>();
            }
            drag.enabled = true;

            Collider col = unit.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }
        
        Debug.Log($"👽 [{owner} 소환 완료] 왹져가 DB 좌표 ({data.gridX}, {data.gridY}) -> 클라 좌표 ({data.gridY}, {data.gridX}) 에 배치되었습니다!");
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

    public void UpdateKidnapCostUI(int cost)
    {
        if (summonCostText != null)
        {
            if (cost <= 0)
            {
                summonCostText.text = "왹져 소환 (불러오는 중)";
            }
            else
            {
                summonCostText.text = $"왹져 소환 ({cost:N0}골드)";
            }
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

    // 💡 동기화 상태 관리 가드 변수들
    private bool isSyncingBoard = false;
    private bool isGameOver = false;

    public bool IsSyncingBoard => isSyncingBoard;
    public bool IsGameOver => isGameOver;

    // 💡 신규 Merge 결과 Alien 전용 안전 생성 메서드
    public bool TrySpawnMergedAlien(BoardObjectDto data, bool isMine)
    {
        if (data == null)
        {
            Debug.LogError("🚨 [TrySpawnMergedAlien] 전달된 BoardObjectDto 데이터가 null 입니다.");
            return false;
        }

        if (data.objectType != BoardObjectDto.TypeAlien)
        {
            Debug.LogError($"🚨 [TrySpawnMergedAlien] 허용되지 않는 objectType 감지: {data.objectType}");
            return false;
        }

        try
        {
            // BoardObjectDto -> InGameAlien 모델 변환
            InGameAlien alien = new InGameAlien();
            alien.id = data.id;
            alien.gridX = data.gridX;
            alien.gridY = data.gridY;
            alien.pendingMutationType = data.pendingMutationType;
            alien.activeMutationType = data.activeMutationType;
            alien.mutationRerollCount = data.mutationRerollCount;
            alien.alienSpec = data.alienSpec;

            SpawnUnit(alien, isMine);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🚨 [TrySpawnMergedAlien] 결과 Alien 생성 중 예외 발생: {ex.Message}");
            return false;
        }
    }

    // 💡 신규 격리 생성 API (Staging 용)
    public bool TryCreateBoardObject(BoardObjectDto data, bool activateImmediately, out GameObject created)
    {
        created = null;
        if (data == null) return false;

        try
        {
            GameObject prefab = (data.objectType == BoardObjectDto.TypeAlien) ? unitPrefab : injectorPrefab;
            if (prefab == null)
            {
                Debug.LogError($"🚨 [TryCreateBoardObject] 프리팹을 찾을 수 없습니다. (Type: {data.objectType})");
                return false;
            }

            // myGridParent 아래에 임시 부모 격리 생성 진행
            created = Instantiate(prefab, myGridParent);
            if (created == null) return false;

            // Awake는 돌지만 즉시 비활성화 처리하여 씬 전체 충돌/조회 배제
            created.SetActive(false);

            // 컴포넌트 정보 탑재
            if (data.objectType == BoardObjectDto.TypeAlien)
            {
                UnitData ud = created.GetComponent<UnitData>();
                if (ud != null)
                {
                    InGameAlien alien = new InGameAlien
                    {
                        id = data.id,
                        alienSpec = data.alienSpec,
                        gridX = data.gridX,
                        gridY = data.gridY,
                        pendingMutationType = data.pendingMutationType,
                        activeMutationType = data.activeMutationType,
                        mutationRerollCount = data.mutationRerollCount
                    };
                    ud.SetInfo(alien);
                }

                UnitDrag udDrag = created.GetComponent<UnitDrag>();
                if (udDrag != null) udDrag.enabled = false;
            }
            else
            {
                InjectorData idData = created.GetComponent<InjectorData>();
                if (idData != null)
                {
                    idData.serverId = data.id;
                    idData.gridX = data.gridX;
                    idData.gridY = data.gridY;
                    idData.mutationType = data.mutationType;
                    idData.isMine = true;
                }

                InjectorDrag idDrag = created.GetComponent<InjectorDrag>();
                if (idDrag != null) idDrag.enabled = false;
            }

            // 콜라이더 비활성화
            Collider col = created.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 보조 격리 수단: 보드판 지하로 멀리 강제 이동
            created.transform.position = Vector3.down * 100f;

            if (activateImmediately)
            {
                created.SetActive(true);
                if (col != null) col.enabled = true;
                if (data.objectType == BoardObjectDto.TypeAlien)
                {
                    UnitDrag udDrag = created.GetComponent<UnitDrag>();
                    if (udDrag != null) udDrag.enabled = true;
                }
                else
                {
                    InjectorDrag idDrag = created.GetComponent<InjectorDrag>();
                    if (idDrag != null) idDrag.enabled = true;
                }
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🚨 [TryCreateBoardObject] 객체 생성 중 예외 발생: {ex.Message}");
            if (created != null) Destroy(created);
            created = null;
            return false;
        }
    }

    // 💡 보드 전체 동기화 및 재접속 복구 공개 진입점
    public void RequestSyncBoardState()
    {
        if (isSyncingBoard) return;
        isSyncingBoard = true;

        // 1. 기존 왹져/인젝터 객체 수집 및 원래 상태 백업
        UnitData[] existingAliens = myGridParent.GetComponentsInChildren<UnitData>(true);
        InjectorData[] existingInjectors = myGridParent.GetComponentsInChildren<InjectorData>(true);

        System.Collections.Generic.List<ExistingObjectCache> cachedObjects = new System.Collections.Generic.List<ExistingObjectCache>();

        foreach (var alien in existingAliens)
        {
            if (alien.gameObject.name.Contains("Me"))
            {
                cachedObjects.Add(new ExistingObjectCache(alien.gameObject));
            }
        }
        foreach (var injector in existingInjectors)
        {
            if (injector.isMine)
            {
                cachedObjects.Add(new ExistingObjectCache(injector.gameObject));
            }
        }

        // 수집된 기존 객체들 물리/드래그 잠금
        foreach (var cache in cachedObjects)
        {
            cache.ApplyLock();
        }

        if (summonBtn != null) summonBtn.interactable = false;

        // 2. 서버 POST /api/game/state 호출
        GameStateRequestDto req = new GameStateRequestDto { userId = UserId };
        NetworkManager.Instance.PostJsonAsync<GameStateRequestDto, GameSessionStateDto>("/game/state", req, (result) =>
        {
            System.Action rollbackEverything = () =>
            {
                // 실패 시 원래 백업값으로 씬 복구
                foreach (var cache in cachedObjects)
                {
                    cache.Restore();
                }
                if (summonBtn != null && !isGameOver) summonBtn.interactable = true;
                isSyncingBoard = false;
            };

            if (!result.IsSuccess)
            {
                Debug.LogError("🚨 [보드 동기화 실패] 서버 통신 중 오류: " + result.NetworkError);
                rollbackEverything();
                return;
            }

            GameSessionStateDto res = result.Data;
            if (res == null || res.boardObjects == null)
            {
                Debug.LogError("🚨 [보드 동기화 실패] 서버 응답 바디가 null 이거나 boardObjects 가 누락되었습니다.");
                rollbackEverything();
                return;
            }

            // 3. 데이터 검증 (중복 ID, 범위 좌표, 동일 타일 검사)
            if (res.boardObjects.Count > 24)
            {
                Debug.LogError($"🚨 [보드 동기화 실패] 서버 보드 유닛 수 한도 초과: {res.boardObjects.Count}");
                rollbackEverything();
                return;
            }

            System.Collections.Generic.HashSet<long> uniqueIds = new System.Collections.Generic.HashSet<long>();
            System.Collections.Generic.HashSet<string> uniqueCoords = new System.Collections.Generic.HashSet<string>();

            foreach (var obj in res.boardObjects)
            {
                if (obj.id <= 0)
                {
                    Debug.LogError("🚨 [보드 동기화 실패] 유효하지 않은 ID 감지: " + obj.id);
                    rollbackEverything();
                    return;
                }
                if (uniqueIds.Contains(obj.id))
                {
                    Debug.LogError("🚨 [보드 동기화 실패] 중복된 ID 감지: " + obj.id);
                    rollbackEverything();
                    return;
                }
                uniqueIds.Add(obj.id);

                if (obj.gridX < 0 || obj.gridX >= 4 || obj.gridY < 0 || obj.gridY >= 6)
                {
                    Debug.LogError($"🚨 [보드 동기화 실패] 좌표 허용한도 초과: ({obj.gridX}, {obj.gridY})");
                    rollbackEverything();
                    return;
                }

                string coordKey = $"{obj.gridX}_{obj.gridY}";
                if (uniqueCoords.Contains(coordKey))
                {
                    Debug.LogError($"🚨 [보드 동기화 실패] 동일 좌표에 중복 유닛 배치 감지: ({obj.gridX}, {obj.gridY})");
                    rollbackEverything();
                    return;
                }
                uniqueCoords.Add(coordKey);

                if (obj.objectType != BoardObjectDto.TypeAlien && obj.objectType != BoardObjectDto.TypeInjector)
                {
                    Debug.LogError("🚨 [보드 동기화 실패] 알 수 없는 objectType 수신: " + obj.objectType);
                    rollbackEverything();
                    return;
                }

                if (obj.objectType == BoardObjectDto.TypeAlien && obj.alienSpec == null)
                {
                    Debug.LogError("🚨 [보드 동기화 실패] Alien 유닛 정보 누락!");
                    rollbackEverything();
                    return;
                }
            }

            // 4. Staging 부모 생성 및 격리 생성
            GameObject stagingRoot = new GameObject("__BoardSyncStaging");
            stagingRoot.SetActive(false); // stagingRoot 자체를 비활성화하여 activeInHierarchy=false 유지

            System.Collections.Generic.List<GameObject> tempCreatedList = new System.Collections.Generic.List<GameObject>();
            bool spawnChainSuccess = true;

            foreach (var obj in res.boardObjects)
            {
                GameObject tempObj;
                // activateImmediately = false 로 격리 생성
                if (TryCreateBoardObject(obj, false, out tempObj))
                {
                    if (tempObj != null)
                    {
                        tempObj.transform.SetParent(stagingRoot.transform, false);
                        tempCreatedList.Add(tempObj);
                    }
                }
                else
                {
                    spawnChainSuccess = false;
                    break;
                }
            }

            if (!spawnChainSuccess)
            {
                foreach (var temp in tempCreatedList)
                {
                    if (temp != null) Destroy(temp);
                }
                Destroy(stagingRoot);

                Debug.LogError("🚨 [로컬 동기화 실패] 신규 유닛 로컬 임시 생성 도중 예외가 발생했습니다.");
                rollbackEverything();
                return;
            }

            // 5. 교체 성공 단행 (기존 객체 비활성화)
            foreach (var cache in cachedObjects)
            {
                if (cache.obj != null)
                {
                    cache.obj.SetActive(false); // 중복 Collider 입력 즉시 차단
                }
            }

            // 신규 유닛들을 정식 그리드 부모 아래로 정렬 이동 및 활성화
            foreach (var temp in tempCreatedList)
            {
                if (temp == null) continue;

                temp.transform.SetParent(myGridParent, false);

                int gx = 0; int gy = 0;
                UnitData ud = temp.GetComponent<UnitData>();
                InjectorData id = temp.GetComponent<InjectorData>();

                if (ud != null) { gx = ud.gridX; gy = ud.gridY; temp.name = $"Unit_Me_{gx}_{gy}"; }
                else if (id != null) { gx = id.gridX; gy = id.gridY; temp.name = $"Injector_Me_{gx}_{gy}"; }

                // 정식 로컬 씬 좌표 지정
                temp.transform.localPosition = new Vector3(gx * tileSize, 0.8f, gy * tileSize);

                // 컴포넌트/Collider 복구 및 활성화
                temp.SetActive(true);
                Collider col = temp.GetComponent<Collider>();
                if (col != null) col.enabled = true;

                if (ud != null)
                {
                    UnitDrag drag = temp.GetComponent<UnitDrag>();
                    if (drag != null) drag.enabled = true;
                }
                else if (id != null)
                {
                    InjectorDrag drag = temp.GetComponent<InjectorDrag>();
                    if (drag != null) drag.enabled = true;
                }
            }

            // 기존 객체 Destroy 소멸 (GC가 아님 - 프레임 종료 시 제거)
            foreach (var cache in cachedObjects)
            {
                if (cache.obj != null)
                {
                    Destroy(cache.obj);
                }
            }

            Destroy(stagingRoot);

            // 6. Gold 와 게임종료(isGameOver) 상태 동기화 반영
            UpdateGoldUI(res.remainingGold);
            UpdateKidnapCostUI(res.currentKidnapCost);
            isGameOver = res.isGameOver;

            if (isGameOver)
            {
                TriggerGameOver();
            }
            else
            {
                ResetGameOverState();
            }

            isSyncingBoard = false;
            Debug.Log("🎉 [보드 동기화 성공] 보드가 서버 정합 정보로 재구성되었습니다.");
        });
    }

    private void ResetGameOverState()
    {
        if (waveText != null)
        {
            waveText.color = Color.white; // 원래 하얀색 폰트 색 복구
            UpdateMonsterUI(); // 몬스터 UI 텍스트 정상 복구
        }
        if (summonBtn != null)
        {
            summonBtn.interactable = true; // 소환 버튼 interactable 복구
        }
    }

    // 기존 객체의 원래 enabled/active 상태를 보존하기 위한 백업 구조체
    private struct ExistingObjectCache
    {
        public GameObject obj;
        public bool activeSelf;
        public bool dragEnabled;
        public bool colliderEnabled;

        public ExistingObjectCache(GameObject target)
        {
            obj = target;
            activeSelf = target.activeSelf;

            dragEnabled = false;
            UnitDrag ud = target.GetComponent<UnitDrag>();
            InjectorDrag id = target.GetComponent<InjectorDrag>();
            if (ud != null) dragEnabled = ud.enabled;
            else if (id != null) dragEnabled = id.enabled;

            colliderEnabled = false;
            Collider col = target.GetComponent<Collider>();
            if (col != null) colliderEnabled = col.enabled;
        }

        public void ApplyLock()
        {
            if (obj == null) return;
            UnitDrag ud = obj.GetComponent<UnitDrag>();
            InjectorDrag id = obj.GetComponent<InjectorDrag>();
            if (ud != null) ud.enabled = false;
            if (id != null) id.enabled = false;

            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        public void Restore()
        {
            if (obj == null) return;
            obj.SetActive(activeSelf);

            UnitDrag ud = obj.GetComponent<UnitDrag>();
            InjectorDrag id = obj.GetComponent<InjectorDrag>();
            if (ud != null) ud.enabled = dragEnabled;
            if (id != null) id.enabled = dragEnabled;

            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = colliderEnabled;
        }
    }
}
