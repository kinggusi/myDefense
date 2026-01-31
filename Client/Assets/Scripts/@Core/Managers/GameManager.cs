using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject unitPrefab; // 여기에 에셋이나 큐브 프리팹을 할당하세요.
    private long userId = 100;    // 테스트용 유저 ID

    // 버튼에 연결할 함수
    public void OnClickSummon()
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userId.ToString());

        NetworkManager.Instance.Post("/summon", form, (json) => {
            GameResponseDto res = JsonUtility.FromJson<GameResponseDto>(json);
            
            if (res.alien != null) {
                SpawnUnit(res.alien);
                Debug.Log($"소환 성공! 남은 골드: {res.remainingGold}");
            }
        }, (err) => Debug.LogError("소환 실패: " + err));
    }

    public void SpawnUnit(InGameAlien data)
    {
        // 3D 공간의 랜덤 좌표 (y값은 바닥 높이인 0.5f 정도)
        Vector3 spawnPos = new Vector3(Random.Range(-4, 4), 0.5f, Random.Range(-4, 4));
        GameObject unit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        // 데이터 명찰 달아주기
        UnitData unitData = unit.GetComponent<UnitData>();
        if (unitData != null) {
            unitData.SetInfo(data);
        }
    }
}