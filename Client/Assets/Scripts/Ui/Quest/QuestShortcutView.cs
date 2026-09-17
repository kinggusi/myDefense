using UnityEngine;
using UnityEngine.UI;

public sealed class QuestShortcutView : MonoBehaviour
{
    public Button button;
    public Text statusText;
    public GameObject badgeObject;
    public Text badgeText;

    public void SetClaimable(int count)
    {
        if (statusText != null) statusText.text = count > 0 ? "받을 보상 " + count + "개" : "오늘의 임무 확인";
        if (badgeObject != null) badgeObject.SetActive(count > 0);
        if (badgeText != null) badgeText.text = count.ToString();
    }
}
