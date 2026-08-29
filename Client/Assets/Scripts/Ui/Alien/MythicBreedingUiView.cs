using UnityEngine;
using UnityEngine.UI;

public sealed class MythicBreedingUiView : MonoBehaviour
{
    public CanvasGroup inputCanvasGroup;
    public Button backButton;
    public Button combinationButton;
    public Button primaryActionButton;
    public Button accelerateButton;
    public Button instantButton;
    public Text parentSelectionText;
    public Text statusText;
    public Transform slotRoot;
    public Transform candidateRoot;
    public Button slotButtonTemplate;
    public Button candidateButtonTemplate;
    public GameObject combinationPanel;
    public Button combinationCloseButton;
    public Text combinationText;
}
