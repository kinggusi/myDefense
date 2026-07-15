using GachaShop.Core;
using UnityEngine;
using UnityEngine.UI;

public sealed class GachaResultCardView : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Outline outline;
    [SerializeField] private Text gradeText;
    [SerializeField] private Text alienIdText;
    [SerializeField] private Text statusText;

    public RectTransform RectTransform => (RectTransform)transform;

    public void Bind(GachaDraw draw, GachaReward reward)
    {
        gradeText.text = draw.grade;
        alienIdText.text = $"Alien {draw.alienId}";

        if (reward == null)
        {
            statusText.text = string.Empty;
        }
        else
        {
            string unlock = reward.newlyUnlocked ? "신규 획득\n" : string.Empty;
            statusText.text =
                $"{unlock}획득 {reward.occurrenceCount}회 / 조각 +{reward.piecesAdded}\n" +
                $"Lv.{reward.currentLevel} / 조각 {reward.currentPieces}";
        }

        Color gradeColor = GradeColor(draw.grade);
        backgroundImage.color = new Color(gradeColor.r * 0.28f, gradeColor.g * 0.28f, gradeColor.b * 0.28f, 0.96f);
        outline.effectColor = gradeColor;
        gradeText.color = gradeColor;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        RectTransform.localScale = Vector3.one;
    }

    private static Color GradeColor(string grade)
    {
        switch (grade)
        {
            case "EPIC": return new Color(0.74f, 0.36f, 1f);
            case "UNIQUE": return new Color(0.2f, 0.72f, 1f);
            case "LEGEND": return new Color(1f, 0.66f, 0.12f);
            case "MYTHIC": return new Color(1f, 0.2f, 0.55f);
            default: return new Color(0.7f, 0.82f, 0.9f);
        }
    }
}
