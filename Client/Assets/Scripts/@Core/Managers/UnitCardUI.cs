using UnityEngine;
using TMPro;

public class UnitCardUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text piecesText;
    [SerializeField] private GameObject lockOverlay;

    public void SetData(AlienInventoryDto data)
    {
        if (data == null) return;

        // NullReferenceException 방어 코드 적용 (프리팹 미바인딩 시에도 에러 방지)
        if (nameText != null)
        {
            nameText.text = data.name;
        }

        if (levelText != null)
        {
            levelText.text = $"Lv.{data.level}";
        }

        if (piecesText != null)
        {
            piecesText.text = $"{data.pieces}/{data.requiredPieces}";
        }

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(data.locked);
        }

        Debug.Log($"[UnitCardUI] Card data bound: {data.name}, Level: {data.level}, Locked: {data.locked}");
    }
}
