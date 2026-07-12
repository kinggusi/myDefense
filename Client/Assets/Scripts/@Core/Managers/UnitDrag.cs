using UnityEngine;

public class UnitDrag : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPos;
    private bool isDragging = false;

    void Start() { cam = Camera.main; }

    // 1. 마우스 클릭 시 (내 유닛만 드래그 허용)
    void OnMouseDown()
    {
        UnitData myData = GetComponent<UnitData>();
        if (myData == null || !myData.isMine)
        {
            isDragging = false;
            Debug.Log("[UnitDrag] 내 소속이 아닌 유닛은 드래그할 수 없습니다.");
            return;
        }

        startPos = transform.position;
        offset = transform.position - GetMouseWorldPos();
        isDragging = true;
    }

    // 2. 마우스 드래그 시
    void OnMouseDrag()
    {
        if (!isDragging) return;
        Vector3 newPos = GetMouseWorldPos() + offset;
        // 3D 바닥(Plane) 위에서 움직이도록 y값은 고정 (0.5f)
        transform.position = new Vector3(newPos.x, 0.5f, newPos.z);
    }

    // 3. 마우스 뗐을 때 (머지, 이동, 스왑 판정)
    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;
        
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 1. 다른 유닛 위에 드롭했을 때
            if (hit.collider.CompareTag("Unit") && hit.collider.gameObject != gameObject)
            {
                UnitData myData = GetComponent<UnitData>();
                UnitData targetData = hit.collider.GetComponent<UnitData>();

                if (myData != null && targetData != null && myData.isMine)
                {
                    // 조건 A: 같은 종 & 같은 등급 -> 머지
                    if (myData.grade == targetData.grade && myData.specId == targetData.specId)
                    {
                        RequestMerge(hit.collider.gameObject);
                        return;
                    }
                    // 조건 B: 다른 유닛 -> 스왑
                    else
                    {
                        int targetNewX = targetData.gridX;
                        int targetNewY = targetData.gridY;

                        // 동일 타일 드롭 방어
                        if (targetNewX == myData.gridX && targetNewY == myData.gridY)
                        {
                            transform.position = startPos;
                            return;
                        }

                        RequestMove(hit.collider.gameObject, targetNewX, targetNewY);
                        return;
                    }
                }
            }
            // 2. 빈 타일 위에 드롭했을 때
            else if (hit.collider.name.StartsWith("Tile_"))
            {
                UnitData myData = GetComponent<UnitData>();
                if (myData != null && myData.isMine)
                {
                    if (TryParseTileCoords(hit.collider.name, out int col, out int row))
                    {
                        int targetNewX = row;
                        int targetNewY = col;

                        // 동일 타일 드롭 방어
                        if (targetNewX == myData.gridX && targetNewY == myData.gridY)
                        {
                            transform.position = startPos;
                            return;
                        }

                        RequestMove(null, targetNewX, targetNewY);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[UnitDrag] 유효하지 않은 타일 좌표 또는 범위 초과: {hit.collider.name}");
                    }
                }
            }
        }
        
        // 머지/이동 대상이 없거나 조건에 맞지 않으면 원래 자리로 복귀
        transform.position = startPos;
    }

    bool TryParseTileCoords(string tileName, out int col, out int row)
    {
        col = -1;
        row = -1;
        string cleanName = tileName.Replace("(Clone)", "").Trim();
        string[] parts = cleanName.Split('_');
        if (parts.Length >= 3 && parts[0] == "Tile")
        {
            if (int.TryParse(parts[1], out col) && int.TryParse(parts[2], out row))
            {
                // 4x6 범위 체크: row(gridX) 0~3, col(gridY) 0~5
                return (row >= 0 && row < 4 && col >= 0 && col < 6);
            }
        }
        return false;
    }

    void RequestMove(GameObject targetUnit, int newX, int newY)
    {
        UnitData myData = GetComponent<UnitData>();
        if (myData == null || !myData.isMine) return;

        // 🚨 로컬 안전 검증 1: 보드 부모 존재 여부
        if (transform.parent == null)
        {
            Debug.LogWarning("[UnitDrag] 이동 실패: 유닛의 보드 부모가 존재하지 않습니다.");
            transform.position = startPos;
            return;
        }

        // 🚨 로컬 안전 검증 2: 목적 타일 존재 여부 (부모 보드 내부 한정)
        string destTileName = $"Tile_{newY}_{newX}";
        Transform destTile = transform.parent.Find(destTileName);
        if (destTile == null)
        {
            Debug.LogWarning($"[UnitDrag] 이동 실패: 목적 타일 {destTileName}을 찾을 수 없습니다.");
            transform.position = startPos;
            return;
        }

        int oldX = myData.gridX;
        int oldY = myData.gridY;

        // 🚨 로컬 안전 검증 3: 스왑 시 원래 타일 존재 여부 & 대상이 동일 보드 소속인지 여부
        if (targetUnit != null)
        {
            string srcTileName = $"Tile_{oldY}_{oldX}";
            Transform srcTile = transform.parent.Find(srcTileName);
            if (srcTile == null)
            {
                Debug.LogWarning($"[UnitDrag] 스왑 실패: 원본 타일 {srcTileName}을 찾을 수 없습니다.");
                transform.position = startPos;
                return;
            }

            if (targetUnit.transform.parent != transform.parent)
            {
                Debug.LogWarning("[UnitDrag] 스왑 실패: 스왑 대상 유닛이 동일한 보드 소속이 아닙니다.");
                transform.position = startPos;
                return;
            }
        }

        long sourceId = myData.serverId;

        MoveObjectRequestDto request = new MoveObjectRequestDto {
            userId = 1, // 테스트용
            objectId = sourceId,
            newX = newX,
            newY = newY
        };

        string jsonPayload = JsonUtility.ToJson(request);

        NetworkManager.Instance.PostJson("/game/move", jsonPayload, (resJson) => {
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(resJson);

            if (res == null || res.alien == null)
            {
                string errMsg = res != null ? res.message : "응답 데이터 누락";
                Debug.LogWarning($"[UnitDrag] 이동 요청 거절됨: {errMsg}");
                transform.position = startPos;
                return;
            }

            // [성공 반영] - 전역 탐색 없이 부모 보드 내부에서 찾아 이동
            transform.position = destTile.position + new Vector3(0, 0.8f, 0);
            myData.UpdateGridPosition(newX, newY);

            if (targetUnit != null)
            {
                UnitData targetData = targetUnit.GetComponent<UnitData>();
                if (targetData != null)
                {
                    string srcTileName = $"Tile_{oldY}_{oldX}";
                    Transform srcTile = transform.parent.Find(srcTileName);
                    if (srcTile != null)
                    {
                        targetUnit.transform.position = srcTile.position + new Vector3(0, 0.8f, 0);
                        targetData.UpdateGridPosition(oldX, oldY);
                    }
                }
            }

            Debug.Log($"[UnitDrag] 이동/스왑 성공: {res.message}");
        }, (err) => {
            Debug.LogError($"[UnitDrag] 이동 요청 실패: {err}");
            transform.position = startPos; // 에러 복구
        });
    }

    void RequestMerge(GameObject targetUnit)
    {
        UnitData myData = GetComponent<UnitData>();
        if (myData == null || !myData.isMine) return;

        // 🚨 로컬 안전 검증: 머지 대상 유닛이 동일한 보드 소속인지 확인
        if (targetUnit.transform.parent != transform.parent)
        {
            Debug.LogWarning("[UnitDrag] 머지 실패: 머지 대상 유닛이 동일한 보드 소속이 아닙니다.");
            transform.position = startPos;
            return;
        }

        long sourceId = myData.serverId;
        long targetId = targetUnit.GetComponent<UnitData>().serverId;

        MergeRequestDto request = new MergeRequestDto {
            userId = 1, // 테스트용
            sourceId = sourceId,
            targetId = targetId
        };

        string json = JsonUtility.ToJson(request);

        NetworkManager.Instance.PostJson("/game/merge", json, (resJson) => {
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(resJson);
            
            // 🚨 안전장치 추가: res가 null이거나 생성된 alien 정보가 없는 경우 (병합 거절)
            if (res == null || res.alien == null)
            {
                string errMsg = res != null ? res.message : "응답 데이터 누락";
                Debug.LogWarning($"[UnitDrag] 머지 요청 거절됨: {errMsg}");
                transform.position = startPos; // 원래 위치로 복구
                return;
            }
            
            // 성공하면 기존 두 마리 지우기
            Destroy(gameObject);
            Destroy(targetUnit);

            // 서버가 준 새로운 유닛 소환
            FindObjectOfType<GameManager>().SpawnUnit(res.alien, true);
            Debug.Log("머지 성공! " + res.message);
        }, (err) => {
            Debug.LogError("머지 실패: " + err);
            transform.position = startPos; // 에러나면 복귀
        });
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = cam.WorldToScreenPoint(transform.position).z;
        return cam.ScreenToWorldPoint(mousePoint);
    }
}