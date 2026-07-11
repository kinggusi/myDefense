using UnityEngine;

public class InjectorDrag : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPos;
    private bool isDragging = false;

    void Start() { cam = Camera.main; }

    void OnMouseDown()
    {
        startPos = transform.position;
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        Vector3 newPos = GetMouseWorldPos() + offset;
        transform.position = new Vector3(newPos.x, 0.5f, newPos.z);
    }

    void OnMouseUp()
    {
        isDragging = false;
        
        // 1. RaycastAll 로 마우스 아래의 타일 및 위에 얹힌 객체들 동시 검출 (빈칸 판정 오인 방지)
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
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

        // 같은 칸 드롭인 경우 no-op 처리
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
        GameManager gm = FindObjectOfType<GameManager>();
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

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = cam.WorldToScreenPoint(transform.position).z;
        return cam.ScreenToWorldPoint(mousePoint);
    }
}
