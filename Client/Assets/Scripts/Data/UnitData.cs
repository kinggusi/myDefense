using UnityEngine;

public class UnitData : MonoBehaviour
{
    [Header("서버 데이터")]
    public long serverId;
    public string grade;
    public string unitName;

    // 서버 데이터를 이 유닛에 주입하는 함수
    public void SetInfo(InGameAlien data) {
        this.serverId = data.id;
        this.grade = data.alienSpec.grade;
        this.unitName = data.alienSpec.name;
    }
}