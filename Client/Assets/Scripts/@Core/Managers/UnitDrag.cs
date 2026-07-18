using UnityEngine;
using UnityEngine.EventSystems;
using MyDefense.Battle.Runtime;

public class UnitDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPos;
    private bool isDragging = false;
    private GameManager gameManager; // 캐싱용 변수

    void Start() 
    { 
        cam = Camera.main; 
        gameManager = Object.FindFirstObjectByType<GameManager>(); // 씬 검색 최소화 단 1회 수행
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        BeginDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ContinueDrag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndDrag(eventData.position);
    }

    void BeginDrag(Vector2 screenPos)
    {
        startPos = transform.position;
        offset = transform.position - GetMouseWorldPos(screenPos);
        isDragging = true;
    }

    void ContinueDrag(Vector2 screenPos)
    {
        if (!isDragging) return;
        Vector3 newPos = GetMouseWorldPos(screenPos) + offset;
        // 3D 바닥(Plane) 위에서 움직이도록 y값은 고정 (0.5f)
        transform.position = new Vector3(newPos.x, 0.5f, newPos.z);
    }

    void EndDrag(Vector2 screenPos)
    {
        isDragging = false;
        
        // 1. RaycastAll 로 마우스 아래의 타일 및 위에 얹힌 객체들 동시 검출 (빈칸 판정 오인 방지)
        Ray ray = cam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        Transform targetTile = null;
        GameObject targetObj = null;

        foreach (var hit in hits)
        {
            // 타일 검출
            if (hit.collider.name.StartsWith("Tile_"))
            {
                targetTile = hit.collider.transform;
            }
            // 타겟 보드 오브젝트 검출 (자기 자신 제외)
            else if (hit.collider.gameObject != gameObject)
            {
                if (BoardObjectHelper.TryGetBoardObject(hit.collider.gameObject, out _, out _))
                {
                    targetObj = hit.collider.gameObject;
                }
            }
        }

        // 타일이 감지되지 않았으면 원래 포지션 복귀
        if (targetTile == null)
        {
            transform.position = startPos;
            return;
        }

        // 타일 이름(Tile_{y}_{x})에서 newX/newY 추출
        string[] parts = targetTile.name.Split('_');
        if (parts.Length < 3 || !int.TryParse(parts[1], out int newY) || !int.TryParse(parts[2], out int newX))
        {
            Debug.LogError("🚨 [UnitDrag] 타일 좌표 파싱 실패: " + targetTile.name);
            transform.position = startPos;
            return;
        }

        // source 정보 획득
        if (!BoardObjectHelper.TryGetBoardObject(gameObject, out long sourceId, out BoardObjectKind sourceKind, out int oldGridX, out int oldGridY, out bool isMine, out UnitData myData, out _))
        {
            transform.position = startPos;
            return;
        }

        // 2. Merge 조건 체크 (같은 종/등급 Alien 위 드롭)
        if (targetObj != null)
        {
            if (BoardObjectHelper.TryGetBoardObject(targetObj, out long targetId, out BoardObjectKind targetKind, out int targetGridX, out int targetGridY, out _, out UnitData targetData, out _))
            {
                if (sourceKind == BoardObjectKind.Alien && targetKind == BoardObjectKind.Alien)
                {
                    if (myData != null && targetData != null && myData.grade == targetData.grade && myData.specId == targetData.specId)
                    {
                        // 기존 Merge 로직으로 위임하고 Move 통신은 전면 스킵
                        RequestMerge(targetObj);
                        return;
                    }
                }
            }
        }

        // 같은 칸 드롭인 경우 no-op 처리 (서버 호출 없이 로컬 확정)
        if (oldGridX == newX && oldGridY == newY)
        {
            transform.position = startPos;
            return;
        }

        // 3. Move / Swap 가동
        RequestMove(targetTile, targetObj, sourceId, oldGridX, oldGridY, newX, newY);
    }

    void RequestMove(Transform targetTile, GameObject targetObj, long sourceId, int oldX, int oldY, int newX, int newY)
    {
        if (gameManager != null && (gameManager.IsSyncingBoard || gameManager.IsGameOver))
        {
            transform.position = startPos;
            return;
        }
        // A. 요청 전 상태 캐싱
        Vector3 sourceStartPos = startPos;
        Vector3 targetStartPos = Vector3.zero;
        long targetId = -1;
        bool hasTarget = targetObj != null;

        // 드래그 컴포넌트들 캐시 (입력 잠금 목적)
        UnitDrag sourceDrag = this;
        UnitDrag targetDragAlien = null;
        InjectorDrag targetDragInjector = null;

        sourceDrag.enabled = false; // source 잠금

        if (hasTarget)
        {
            targetStartPos = targetObj.transform.position;
            if (BoardObjectHelper.TryGetBoardObject(targetObj, out targetId, out BoardObjectKind targetKind, out _, out _, out _, out UnitData tdAlien, out InjectorData tdInjector))
            {
                if (targetKind == BoardObjectKind.Alien)
                {
                    targetDragAlien = targetObj.GetComponent<UnitDrag>();
                    if (targetDragAlien != null) targetDragAlien.enabled = false;
                }
                else
                {
                    targetDragInjector = targetObj.GetComponent<InjectorDrag>();
                    if (targetDragInjector != null) targetDragInjector.enabled = false;
                }
            }
        }

        // B. 낙관적 UI 업데이트 (씬 이동 선반영)
        Vector3 sourceDestPos = targetTile.position + new Vector3(0, 0.8f, 0);
        transform.position = sourceDestPos;

        if (hasTarget)
        {
            // Swap 상대는 source의 시작 위치로 이동
            targetObj.transform.position = sourceStartPos;
        }

        // C. API 요청 전송 (PostJsonAsync 사용)
        GameManager gm = FindObjectOfType<GameManager>();
        long userId = gm != null ? gm.UserId : 1; // GameManager에서 동적으로 userId 획득

        MoveObjectRequestDto req = new MoveObjectRequestDto
        {
            userId = userId,
            objectId = sourceId,
            newX = newX,
            newY = newY
        };

        string requestUri = "/game/move";
        NetworkManager.Instance.PostJsonAsync<MoveObjectRequestDto, GameResponseObjectDto>(requestUri, req, (result) =>
        {
            // 잠금 해제 헬퍼
            System.Action unlockAll = () =>
            {
                if (sourceDrag != null) sourceDrag.enabled = true;
                if (targetDragAlien != null) targetDragAlien.enabled = true;
                if (targetDragInjector != null) targetDragInjector.enabled = true;
            };

            if (result.IsSuccess)
            {
                // D. 성공 시 데이터 확정 및 gridX/Y 갱신
                // 1. source 갱신
                if (BoardObjectHelper.TryGetBoardObject(gameObject, out _, out _, out _, out _, out _, out UnitData myUd, out InjectorData myIdData))
                {
                    if (myUd != null) { myUd.gridX = newX; myUd.gridY = newY; }
                    else if (myIdData != null) { myIdData.gridX = newX; myIdData.gridY = newY; }
                }
                string owner = name.Contains("Me") ? "Me" : "Enemy";
                name = name.StartsWith("Unit_") ? $"Unit_{owner}_{newX}_{newY}" : $"Injector_{owner}_{newX}_{newY}";

                // 2. target 갱신
                if (hasTarget && targetObj != null)
                {
                    if (BoardObjectHelper.TryGetBoardObject(targetObj, out _, out _, out _, out _, out _, out UnitData tgtUd, out InjectorData tgtIdData))
                    {
                        if (tgtUd != null) { tgtUd.gridX = oldX; tgtUd.gridY = oldY; }
                        else if (tgtIdData != null) { tgtIdData.gridX = oldX; tgtIdData.gridY = oldY; }
                    }
                    targetObj.name = targetObj.name.StartsWith("Unit_") ? $"Unit_{owner}_{oldX}_{oldY}" : $"Injector_{owner}_{oldX}_{oldY}";
                }

                unlockAll();
                Debug.Log($"🎉 [이동 성공] Object {sourceId}가 ({oldX}, {oldY}) -> ({newX}, {newY})로 안착 완료!");
            }
            else
            {
                // E. 실패 시 롤백 처리
                transform.position = sourceStartPos;
                if (hasTarget && targetObj != null)
                {
                    targetObj.transform.position = targetStartPos;
                }

                unlockAll();

                if (result.Error != null)
                {
                    string errCode = result.Error.code;
                    if (errCode == "BOARD_STATE_INCONSISTENT")
                    {
                        Debug.LogError($"🚨 [보드 상태 불일치] 서버와 클라이언트의 보드 좌표 정합성이 손상되었습니다. (ErrorCode: BOARD_STATE_INCONSISTENT). [TODO: SyncBoardState API]");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [이동 실패] 비즈니스 에러 ({errCode}): {result.Error.message}");
                    }
                }
                else
                {
                    Debug.LogError("🚨 [이동 실패] 네트워크 오류 또는 알 수 없는 실패 발생: " + result.NetworkError);
                }
            }
        });
    }

    void RequestMerge(GameObject targetUnit)
    {
        if (!BattlePlayerActionGate.CanUseBattleAction("Merge"))
        {
            transform.position = startPos;
            return;
        }
        if (gameManager != null && (gameManager.IsSyncingBoard || gameManager.IsGameOver))
        {
            transform.position = startPos;
            return;
        }
        // A. 머지 요청 전 상태 캐싱 및 입력 잠금
        Vector3 sourceStartPos = startPos;
        Vector3 targetStartPos = targetUnit.transform.position;

        UnitDrag sourceDrag = this;
        UnitDrag targetDrag = targetUnit.GetComponent<UnitDrag>();

        sourceDrag.enabled = false;
        if (targetDrag != null) targetDrag.enabled = false;

        // serverId 획득
        long sourceId = GetComponent<UnitData>().serverId;
        long targetId = targetUnit.GetComponent<UnitData>().serverId;

        long userId = gameManager != null ? gameManager.UserId : 1;

        MergeRequestDto req = new MergeRequestDto
        {
            userId = userId,
            sourceId = sourceId,
            targetId = targetId
        };

        string requestUri = "/game/merge";
        NetworkManager.Instance.PostJsonAsync<MergeRequestDto, GameResponseObjectDto>(requestUri, req, (result) =>
        {
            // 잠금 해제 헬퍼 (실패 시에만 원복하여 켜줌)
            System.Action unlockAll = () =>
            {
                if (sourceDrag != null) sourceDrag.enabled = true;
                if (targetDrag != null) targetDrag.enabled = true;
            };

            if (result.IsSuccess)
            {
                GameResponseObjectDto res = result.Data;
                if (res != null && res.alien != null)
                {
                    if (res.alien.objectType == BoardObjectDto.TypeAlien)
                    {
                        // 1/2. 결과 Alien 로컬 생성 시도 및 성공 여부 확인
                        bool spawnSuccess = gameManager != null && gameManager.TrySpawnMergedAlien(res.alien, true);

                        if (spawnSuccess)
                        {
                            // 3/4. 한 프레임 내 중복 조작을 막기 위해 즉시 비활성화 처리 후 파괴
                            gameObject.SetActive(false);
                            targetUnit.SetActive(false);

                            Destroy(gameObject);
                            Destroy(targetUnit);

                            // remainingGold 갱신 (서버 응답 데이터를 바탕으로 GameManager 내에서 동기화됨)
                            if (gameManager != null)
                            {
                                // GameManager의 TrySpawnMergedAlien 내부에서 Gold UI가 갱신되도록 연계
                            }

                            Debug.Log($"🎉 [머지 성공] 새로운 유닛 ID: {res.alien.id} ({res.alien.alienSpec.name}) 생성 완료!");
                        }
                        else
                        {
                            // 결과 Alien 생성 실패 시 기존 객체 제거 금지 및 유지
                            unlockAll();
                            Debug.LogError($"🚨 [로컬 동기화 실패] 서버 머지는 성공하였으나 결과 Alien 씬 배치에 실패했습니다. (결과 ID: {res.alien.id}). [TODO: SyncBoardState API]");
                        }
                    }
                    else
                    {
                        unlockAll();
                        Debug.LogError($"🚨 [머지 오류] 반환된 객체 타입이 ALIEN이 아닙니다: {res.alien.objectType}");
                    }
                }
                else
                {
                    unlockAll();
                    Debug.LogError("🚨 [머지 성공 응답] 서버 성공 응답에 결과 유닛 데이터가 누락되었습니다.");
                }
            }
            else
            {
                // B. 실패 시 상태 원복 및 잠금 해제
                transform.position = sourceStartPos;
                if (targetUnit != null)
                {
                    targetUnit.transform.position = targetStartPos;
                }

                unlockAll();

                if (result.Error != null)
                {
                    string errCode = result.Error.code;
                    if (errCode == "BOARD_STATE_INCONSISTENT")
                    {
                        Debug.LogError($"🚨 [보드 상태 불일치] 머지 대상 보드 정합성이 손상되었습니다. (ErrorCode: BOARD_STATE_INCONSISTENT). [TODO: SyncBoardState API]");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [머지 실패] 비즈니스 에러 ({errCode}): {result.Error.message}");
                    }
                }
                else
                {
                    Debug.LogError("🚨 [머지 실패] 네트워크 오류 또는 알 수 없는 실패 발생: " + result.NetworkError);
                }
            }
        });
    }

    Vector3 GetMouseWorldPos(Vector2 screenPos)
    {
        if (cam == null)
            return transform.position;

        Ray ray = cam.ScreenPointToRay(screenPos);

        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        return dragPlane.Raycast(ray, out float distance)
            ? ray.GetPoint(distance)
            : transform.position;
    }
}
