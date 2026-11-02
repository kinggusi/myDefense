using UnityEngine;
using UnityEngine.UI;

public sealed class QuestUiView : MonoBehaviour
{
    public const float CycleScrollTop = 450f;
    public const float AchievementScrollTop = 210f;
    public CanvasGroup inputCanvasGroup;
    public Button closeButton;
    public Button dailyTabButton;
    public Button weeklyTabButton;
    public Button achievementTabButton;
    public Text titleText;
    public Text resetText;
    public Text activityText;
    public Slider activitySlider;
    public Transform milestoneRoot;
    public Button milestoneTemplate;
    public Transform questRoot;
    public RectTransform questScrollRect;
    public Button questTemplate;
    public Text statusText;
}
