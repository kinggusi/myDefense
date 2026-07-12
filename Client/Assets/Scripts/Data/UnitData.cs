using UnityEngine;

public class UnitData : MonoBehaviour
{
    [Header("서버 데이터")]
    public long serverId;
    public long specId;
    public string grade;
    public string unitName;
    public string pendingMutationType;
    public string activeMutationType;
    public int mutationRerollCount;
    public int gridX; // 신규 추가: 보드 상의 grid X 좌표
    public int gridY; // 신규 추가: 보드 상의 grid Y 좌표

    [Header("서버 좌표")]
    public int gridX;
    public int gridY;

    [Header("소유권 정보")]
    public bool isMine;

    // 서버 데이터를 이 유닛에 주입하는 함수
    public void SetInfo(InGameAlien data)
    {
        this.serverId = data.id;
        this.specId = data.alienSpec.id;
        this.grade = data.alienSpec.grade;
        this.unitName = data.alienSpec.name;
        this.pendingMutationType = data.pendingMutationType;
        this.activeMutationType = data.activeMutationType;
        this.mutationRerollCount = data.mutationRerollCount;
        this.gridX = data.gridX;
        this.gridY = data.gridY;

        Debug.Log($"[UnitData] ID:{serverId}, 이름:{unitName}, 등급:{grade}, 위치:({gridX}, {gridY})");

        UpdateColor();
    }

    public void UpdateGridPosition(int x, int y)
    {
        this.gridX = x;
        this.gridY = y;
        Debug.Log($"[UnitData] ID:{serverId} 위치 업데이트 -> gridX:{gridX}, gridY:{gridY}");
    }
    
    void UpdateColor()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return; 

        switch (this.grade)
        {
            case "NORMAL":
                rend.material.color = Color.blue;
                break;
            case "EPIC":
            case "UNIQUE":
                rend.material.color = Color.magenta;
                break;
            case "LEGEND":
                rend.material.color = Color.yellow;
                break;
            case "MYTHIC":
                rend.material.color = Color.red; // 신화 등용 빨간색
                break;
            default:
                rend.material.color = Color.white;
                break;
        }
    }
}