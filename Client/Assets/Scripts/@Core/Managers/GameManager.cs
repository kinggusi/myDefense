using UnityEngine;
using UnityEngine.UI; // UI 제어를 위해 추가

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

    [Header("테스트 정보")]
    private long userId = 1;       // 내 ID
    private long enemyId = 2;      // 가상의 상대방 ID
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
            // 서버에서 응답 올 때 아직 골드 정보는 없지만 기본 500으로 맞춰줍니다.
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
        // 🚨 디바운스(Debounce) & 광클 방지 처리
        if (isSummoning) return; 
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
        // 💡 [적 소환 시도] 상대방도 같이 소환되게 하기 (기존 레거시 API 및 오버로드 호환성 유지)
        // ==========================================
        WWWForm enemyForm = new WWWForm();
        enemyForm.AddField("userId", enemyId.ToString());

        NetworkManager.Instance.Post(ApiSummon, enemyForm, (json) =>
        {
            Debug.Log($"[적 소환 시도] 서버 응답: {json}");
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);
            if (res.alien != null)
            {
                SpawnUnit(res.alien, false); // false: 적 필드
            }
        }, (err) => {
            Debug.Log("상대방 소환 에러 (무시가능)");
        });
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