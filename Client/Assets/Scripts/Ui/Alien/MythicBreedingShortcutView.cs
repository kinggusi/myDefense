using UnityEngine;
using UnityEngine.UI;

public sealed class MythicBreedingShortcutView : MonoBehaviour
{
    public Button button;
    public Text titleText;
    public Text statusText;
    public GameObject badgeObject;
    public Text badgeText;

    public void SetContent(string title, string status, int readyCount)
    {
        titleText.text = title;
        statusText.text = status;
        badgeObject.SetActive(readyCount > 0);
        badgeText.text = readyCount.ToString();
    }
}
