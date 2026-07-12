using UnityEngine;
using UnityEngine.EventSystems;

public class InjectorDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPos;
    private bool isDragging = false;

    void Start() { cam = Camera.main; }

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
            if (hit.collider.name.StartsWith("Tile_"))
            {
                targetTile = hit.collider.transform;
            }
            else if (hit.collider.gameObject != gameObject)
            {
                if (BoardObjectHelper.TryGetBoardObject(hit.collider.gameObject, out _, out _))
                {
                    targetObj = hit.collider.gameObject;
                }
            }
        }

        if (targetTile == null)
        {
            transform.position = startPos;
            return;
        }

        // 타일 이름(Tile_{y}_{x})에서 newX/newY 추출
        string[] parts = targetTile.name.Split('_');
        if (parts.Length < 3 || !int.TryParse(parts[1], out int newY) || !int.TryParse(parts[2], out int newX))
        {
            Debug.LogError("🚨 [InjectorDrag] 타일 좌표 파싱 실패: " + targetTile.name);
            transform.position = startPos;
            return;
        }

        // source 정보 획득
        if (!BoardObjectHelper.TryGetBoardObject(gameObject, out long sourceId, out BoardObjectKind sourceKind, out int oldGridX, out int oldGridY, out bool isMine, out _, out InjectorData myIdData))
        {
            transform.position = startPos;
            return;
        }

        // 2. 사용 조건 판단 (Injector -> Alien 사용 시)
        if (targetObj != null)
        {
            if (BoardObjectHelper.TryGetBoardObject(targetObj, out long targetId, out BoardObjectKind targetKind, out _, out _, out _, out UnitData targetUd, out _))
            {
                if (targetKind == BoardObjectKind.Alien && sourceId > 0 && targetId > 0)
                {
                    RequestUseInjector(targetObj, sourceId, targetId);
                    return;
                }
            }
        }

        // 같은 칸 드롭인 경우 no-op 처리
        if (oldGridX == newX && oldGridY == newY)
        {
            transform.position = startPos;
            return;
        }

        // 3. Move / Swap 가동
        RequestMove(targetTile, targetObj, sourceId, oldGridX, oldGridY, newX, newY);
    }

    void RequestUseInjector(GameObject targetAlien, long sourceId, long targetId)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && (gm.IsSyncingBoard || gm.IsGameOver))
        {
            transform.position = startPos;
            return;
        }
        // A. 요청 전 상태 캐싱 및 입력 잠금
        Vector3 sourceStartPos = startPos;
        Vector3 targetStartPos = targetAlien.transform.position;

        // gridX/y 데이터 백업
        int sourceGridX = -1;
        int sourceGridY = -1;
        if (BoardObjectHelper.TryGetBoardObject(gameObject, out _, out _, out sourceGridX, out sourceGridY, out _, out _, out _)) {}

        int targetGridX = -1;
        int targetGridY = -1;
        UnitData alienUd = targetAlien.GetComponent<UnitData>();
        if (alienUd != null)
        {
            targetGridX = alienUd.gridX;
            targetGridY = alienUd.gridY;
        }

        // Alien 이전 변이 상태 백업 (원자성 확보)
        string oldPending = alienUd != null ? alienUd.pendingMutationType : "NONE";
        string oldActive = alienUd != null ? alienUd.activeMutationType : "NONE";
        int oldReroll = alienUd != null ? alienUd.mutationRerollCount : 0;

        InjectorDrag sourceDrag = this;
        UnitDrag targetDrag = targetAlien.GetComponent<UnitDrag>();

        sourceDrag.enabled = false;
        if (targetDrag != null) targetDrag.enabled = false;

        // B. API 요청 전송 (UseInjectorResponseDto 가 바디로 직접 반환됨)
        gm = FindObjectOfType<GameManager>();
        long userId = gm != null ? gm.UserId : 1;

        UseInjectorRequestDto req = new UseInjectorRequestDto
        {
            userId = userId,
            injectorId = sourceId,
            alienId = targetId
        };

        string requestUri = "/game/use-injector";
        NetworkManager.Instance.PostJsonAsync<UseInjectorRequestDto, UseInjectorResponseDto>(requestUri, req, (result) =>
        {
            System.Action unlockAll = () =>
            {
                if (sourceDrag != null) sourceDrag.enabled = true;
                if (targetDrag != null) targetDrag.enabled = true;
            };

            if (result.IsSuccess)
            {
                UseInjectorResponseDto res = result.Data;
                if (res != null)
                {
                    // 1. 응답 ID 및 데이터 정밀 검증
                    if (res.alienId == targetId && res.consumedInjectorId == sourceId)
                    {
                        // grid 범위 검증
                        if (res.gridX >= 0 && res.gridX < 4 && res.gridY >= 0 && res.gridY < 6)
                        {
                            try
                            {
                                // 2. 변이 데이터 적용 (성공 처리 원자성)
                                if (alienUd != null)
                                {
                                    alienUd.pendingMutationType = res.pendingMutationType;
                                    alienUd.activeMutationType = res.activeMutationType;
                                    // ※ 참고: UseInjectorResponseDto 에는 mutationRerollCount 가 누락되어 있어 백업값을 유지합니다.
                                }

                                // 3. 인젝터 비활성화 및 소멸
                                gameObject.SetActive(false);
                                Destroy(gameObject);

                                // 4. 대상 Alien 잠금 해제
                                if (targetDrag != null) targetDrag.enabled = true;

                                Debug.Log($"🎉 [인젝터 사용 성공] Alien ID: {res.alienId} 에 pending: {res.pendingMutationType}, active: {res.activeMutationType} 가 반영되었습니다!");
                            }
                            catch (System.Exception ex)
                            {
                                // 적용 중 오류 시 복구
                                if (alienUd != null)
                                {
                                    alienUd.pendingMutationType = oldPending;
                                    alienUd.activeMutationType = oldActive;
                                    alienUd.mutationRerollCount = oldReroll;
                                }
                                transform.position = sourceStartPos;
                                unlockAll();
                                Debug.LogError($"🚨 [로컬 적용 실패] 인젝터 사용 성공 후 데이터 갱신 중 오류가 발생했습니다: {ex.Message}");
                            }
                        }
                        else
                        {
                            transform.position = sourceStartPos;
                            unlockAll();
                            Debug.LogError($"🚨 [정합성 오류] 서버가 반환한 Alien 좌표가 보드 범위를 벗어납니다. (gridX: {res.gridX}, gridY: {res.gridY}). [TODO: SyncBoardState API]");
                        }
                    }
                    else
                    {
                        transform.position = sourceStartPos;
                        unlockAll();
                        Debug.LogError($"🚨 [정합성 오류] 서버 반환 식별자가 일치하지 않습니다. (요청 alien: {targetId}, 응답 alien: {res.alienId} / 요청 injector: {sourceId}, 응답 injector: {res.consumedInjectorId}). [TODO: SyncBoardState API]");
                    }
                }
                else
                {
                    transform.position = sourceStartPos;
                    unlockAll();
                    Debug.LogError("🚨 [인젝터 사용 실패] 서버 응답 바디가 누락되었습니다.");
                }
            }
            else
            {
                // C. 실패 시 상태 원복 및 잠금 해제
                transform.position = sourceStartPos;
                if (targetAlien != null)
                {
                    targetAlien.transform.position = targetStartPos;
                }

                unlockAll();

                if (result.Error != null)
                {
                    string errCode = result.Error.code;
                    if (errCode == "BOARD_STATE_INCONSISTENT")
                    {
                        Debug.LogError($"🚨 [보드 상태 불일치] 인젝터 사용 대상 상태가 정합성을 잃었습니다. (ErrorCode: BOARD_STATE_INCONSISTENT). [TODO: SyncBoardState API]");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [인젝터 사용 실패] 비즈니스 에러 ({errCode}): {result.Error.message}");
                    }
                }
                else
                {
                    Debug.LogError("🚨 [인젝터 사용 실패] 네트워크 오류 또는 알 수 없는 실패 발생: " + result.NetworkError);
                }
            }
        });
    }

    void RequestMove(Transform targetTile, GameObject targetObj, long sourceId, int oldX, int oldY, int newX, int newY)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && (gm.IsSyncingBoard || gm.IsGameOver))
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
        InjectorDrag sourceDrag = this;
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
            targetObj.transform.position = sourceStartPos;
        }

        // C. API 요청 전송 (PostJsonAsync 사용)
        gm = FindObjectOfType<GameManager>();
        long userId = gm != null ? gm.UserId : 1;

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
            System.Action unlockAll = () =>
            {
                if (sourceDrag != null) sourceDrag.enabled = true;
                if (targetDragAlien != null) targetDragAlien.enabled = true;
                if (targetDragInjector != null) targetDragInjector.enabled = true;
            };

            if (result.IsSuccess)
            {
                // D. 성공 시 데이터 확정 및 gridX/Y 갱신
                if (BoardObjectHelper.TryGetBoardObject(gameObject, out _, out _, out _, out _, out _, out UnitData myUd, out InjectorData myIdData))
                {
                    if (myUd != null) { myUd.gridX = newX; myUd.gridY = newY; }
                    else if (myIdData != null) { myIdData.gridX = newX; myIdData.gridY = newY; }
                }
                string owner = name.Contains("Me") ? "Me" : "Enemy";
                name = name.StartsWith("Unit_") ? $"Unit_{owner}_{newX}_{newY}" : $"Injector_{owner}_{newX}_{newY}";

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
                Debug.Log($"🎉 [인젝터 이동 성공] Object {sourceId}가 ({oldX}, {oldY}) -> ({newX}, {newY})로 안착 완료!");
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
                        Debug.LogWarning($"⚠️ [인젝터 이동 실패] 비즈니스 에러 ({errCode}): {result.Error.message}");
                    }
                }
                else
                {
                    Debug.LogError("🚨 [인젝터 이동 실패] 네트워크 오류 또는 알 수 없는 실패 발생: " + result.NetworkError);
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
