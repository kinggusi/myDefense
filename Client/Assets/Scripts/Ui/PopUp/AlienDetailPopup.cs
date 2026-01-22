using UnityEngine;
using UnityEngine.UI;

public class AlienDetailPopup : MonoBehaviour
{
    [Header("UI Components")]
    public Text nameText;    // 예: "1번 왹져 (Lv.1)"
    public Text statsText;   // 예: "공격력: 15"
    public Text pieceText;   // 예: "조각: 5 / 10"
    public Button upgradeBtn;

    private int currentAlienId;
    private const int UPGRADE_COST = 10; // 일단 10개 고정

    // 팝업 열릴 때 호출
    public void OpenPopup(int alienId)
    {
        this.currentAlienId = alienId;
        this.gameObject.SetActive(true);
        RefreshUI();
    }

    // UI 새로고침 (데이터 매니저에서 값 가져옴)
    public void RefreshUI()
    {
        var myData = DataManager.Instance.GetUserAlien(currentAlienId);
        
        // (참고: AlienSpec 엑셀 데이터도 가져와야 완벽하지만, 지금은 서버 데이터만 표시)
        nameText.text = $"왹져 #{currentAlienId} (Lv.{myData.level})";
        statsText.text = $"보유 조각: {myData.pieces}";

        // 버튼 상태 설정
        bool canUpgrade = myData.pieces >= UPGRADE_COST;
        pieceText.text = canUpgrade 
            ? $"강화 가능! ({myData.pieces}/{UPGRADE_COST})" 
            : $"조각 부족 ({myData.pieces}/{UPGRADE_COST})";
        
        upgradeBtn.interactable = canUpgrade;
    }

    // [강화 버튼]에 연결하세요
    public void OnClickUpgrade()
    {
        string username = "MyDev"; // 하드코딩

        HttpManager.Instance.PostUpgrade(username, currentAlienId, (json) => 
        {
            Debug.Log("강화 성공!");
            // 1. 데이터 갱신
            DataManager.Instance.UpdateSingleAlien(json);
            // 2. UI 즉시 갱신 (레벨 오르고 조각 깎인 모습)
            RefreshUI();
        });
    }

    // [닫기 버튼]에 연결하세요
    public void OnClickClose()
    {
        this.gameObject.SetActive(false);
    }
}