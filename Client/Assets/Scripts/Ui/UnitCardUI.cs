using UnityEngine;
using UnityEngine.UI;

public class UnitCardUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public Text text_Name;
    public Text text_Grade;
    public Text text_Level;
    public Text text_Pieces;
    public Image image_Lock;

    public void SetData(AlienInventoryDto data)
    {
        if (data == null) return;

        if (text_Name != null) text_Name.text = data.name;
        if (text_Grade != null) text_Grade.text = data.grade;
        if (text_Level != null) text_Level.text = $"Lv.{data.level}";
        if (text_Pieces != null) text_Pieces.text = $"{data.pieces}/{data.requiredPieces}";
        if (image_Lock != null) image_Lock.gameObject.SetActive(data.locked);
    }
}
