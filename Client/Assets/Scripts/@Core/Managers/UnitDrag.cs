using UnityEngine;

public class UnitDrag : MonoBehaviour
{
    private Vector3 offset;
    private Camera cam;
    private Vector3 startPos;
    private bool isDragging = false;

    void Start() { cam = Camera.main; }

    // 1. 마우스 클릭 시
    void OnMouseDown()
    {
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

    // 3. 마우스 뗐을 때 (머지 판정)
    void OnMouseUp()
    {
        isDragging = false;
        
        // 마우스 뗀 위치에 다른 유닛이 있는지 확인
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 자기 자신이 아닌 객체와 충돌했을 때
            if (hit.collider.gameObject != gameObject)
            {
                // 공통 헬퍼로 양쪽의 보드 오브젝트 타입 판정 검사
                if (BoardObjectHelper.TryGetBoardObject(gameObject, out long myId, out BoardObjectKind myKind) &&
                    BoardObjectHelper.TryGetBoardObject(hit.collider.gameObject, out long targetId, out BoardObjectKind targetKind))
                {
                    // Merge 판정은 Alien끼리만 허용
                    if (myKind == BoardObjectKind.Alien && targetKind == BoardObjectKind.Alien)
                    {
                        UnitData myData = GetComponent<UnitData>();
                        UnitData targetData = hit.collider.GetComponent<UnitData>();

                        if (myData != null && targetData != null && myData.grade == targetData.grade && myData.specId == targetData.specId)
                        {
                            RequestMerge(hit.collider.gameObject);
                            return;
                        }
                    }
                }
            }
        }
        
        // 머지 대상이 없거나 조건에 맞지 않으면 원래 자리로 복귀
        transform.position = startPos;
    }

    void RequestMerge(GameObject targetUnit)
    {
        long sourceId = GetComponent<UnitData>().serverId;
        long targetId = targetUnit.GetComponent<UnitData>().serverId;

        // 서버에 보낼 데이터 구성 (MergeRequestDto)
        MergeRequestDto request = new MergeRequestDto {
            userId = 1, // 테스트용
            sourceId = sourceId,
            targetId = targetId
        };

        string json = JsonUtility.ToJson(request);

        // 서버에 머지 요청 전송
        NetworkManager.Instance.PostJson("/merge", json, (resJson) => {
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(resJson);
            
            // 성공하면 기존 두 마리 지우기
            Destroy(gameObject);
            Destroy(targetUnit);

            // 서버가 준 새로운 유닛 소환 (GameManager의 함수 호출)
            // 🚨 수정: isMine을 true로 줘서 "내 필드"에 스폰되도록 수정!
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