using System;
using AlienUpgrade.Core;
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
    public Image portraitImage;
    public Text portraitFallbackText;
    public Text ownershipStatusText;
    public LobbyNeonGraphic neonFrame;

    private readonly AlienCardSelection selection = new AlienCardSelection();
    private Button cardButton;
    private LobbyUnitPortraitCatalog portraitCatalog;
    private Sprite originalPortrait;
    private bool portraitInitialized;

    public void SetData(AlienInventoryDto data, Action<long> onSelected)
    {
        if (data == null) return;

        if (text_Name != null) text_Name.text = data.name;
        if (text_Grade != null) text_Grade.text = data.grade;
        if (text_Grade != null) text_Grade.color = LobbyNeonGraphic.GradeBorderColor(data.grade);
        if (text_Name != null) text_Name.color = Color.white;
        if (text_Level != null) text_Level.text = data.owned ? $"Lv.{data.level}" : "미보유";
        if (text_Pieces != null) text_Pieces.text = data.owned
            ? (data.requiredPieces > 0 ? $"{data.pieces}/{data.requiredPieces}" : "MAX")
            : (data.requiredPieces > 0 ? $"{data.pieces}/{data.requiredPieces}" : $"{data.pieces}/—");
        if (text_Pieces != null) text_Pieces.color = Color.white;
        if (image_Lock != null) image_Lock.gameObject.SetActive(false);
        if (ownershipStatusText != null)
        {
            ownershipStatusText.text = data.owned ? "보유" : "미보유 · 획득 필요";
            ownershipStatusText.color = data.owned
                ? new Color(0.55f, 0.95f, 0.90f) : new Color(0.65f, 0.69f, 0.77f);
            ownershipStatusText.gameObject.SetActive(false);
        }
        if (neonFrame != null)
        {
            neonFrame.SetGrade(data.grade);
            neonFrame.SetOwned(data.owned);
        }
        if (portraitImage != null)
        {
            if (!portraitInitialized)
            {
                originalPortrait = portraitImage.sprite;
                portraitCatalog = Resources.Load<LobbyUnitPortraitCatalog>(LobbyUnitPortraitCatalog.ResourcePath);
                portraitInitialized = true;
            }
            Sprite mappedPortrait = portraitCatalog != null ? portraitCatalog.Find(data.id, data.grade) : null;
            portraitImage.sprite = mappedPortrait != null ? mappedPortrait : originalPortrait;
            portraitImage.preserveAspect = true;
            portraitImage.color = data.owned ? Color.white : new Color(0.34f, 0.39f, 0.47f);
            portraitImage.enabled = portraitImage.sprite != null;
        }
        if (portraitFallbackText != null)
        {
            portraitFallbackText.gameObject.SetActive(portraitImage == null || portraitImage.sprite == null);
            string grade = data.grade ?? "?";
            portraitFallbackText.text = grade.Length > 0 ? grade.Substring(0, 1).ToUpperInvariant() : "?";
            portraitFallbackText.color = data.owned
                ? new Color(0.48f, 0.91f, 0.95f) : new Color(0.28f, 0.34f, 0.43f);
        }

        selection.Bind(data.id, onSelected);
        cardButton = GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(selection.Select);
            cardButton.onClick.AddListener(selection.Select);
        }
    }
}
