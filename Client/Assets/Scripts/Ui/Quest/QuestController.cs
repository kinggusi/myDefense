using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class QuestController : MonoBehaviour
{
    private const string ScreenResource = "Prefabs/Lobby/Quest/QuestScreen";
    private const string ShortcutResource = "Prefabs/Lobby/Quest/QuestShortcut";

    private LobbyManager lobby;
    private QuestUiView view;
    private QuestShortcutView shortcut;
    private QuestBoardResponse board;
    private Tab selectedTab;
    private bool requesting;

    public void Initialize(LobbyManager manager)
    {
        if (lobby != null) return;
        lobby = manager;
        Canvas canvas = FindFirstObjectByType<Canvas>();
        GameObject screenPrefab = Resources.Load<GameObject>(ScreenResource);
        GameObject shortcutPrefab = Resources.Load<GameObject>(ShortcutResource);
        if (canvas == null || screenPrefab == null || shortcutPrefab == null || manager.viewObjects.Length <= 2)
        {
            Debug.LogWarning("[Quest] Quest UI Prefab 또는 메인 화면을 찾지 못했습니다.");
            return;
        }

        GameObject screen = Instantiate(screenPrefab, canvas.transform, false);
        screen.name = "QuestScreen";
        view = screen.GetComponent<QuestUiView>();
        screen.SetActive(false);
        shortcut = Instantiate(shortcutPrefab, manager.viewObjects[2].transform, false).GetComponent<QuestShortcutView>();
        shortcut.name = "QuestShortcut";
        RectTransform shortcutRect = shortcut.GetComponent<RectTransform>();
        shortcutRect.anchorMin = shortcutRect.anchorMax = shortcutRect.pivot = new Vector2(0f, 1f);
        shortcutRect.anchoredPosition = new Vector2(28f, -150f);
        WireEvents();
    }

    private void WireEvents()
    {
        shortcut.button.onClick.AddListener(Open);
        view.closeButton.onClick.AddListener(Close);
        view.dailyTabButton.onClick.AddListener(() => SelectCycle(false));
        view.weeklyTabButton.onClick.AddListener(() => SelectCycle(true));
        view.achievementTabButton.onClick.AddListener(() => SelectTab(Tab.Achievement));
    }

    public void SetLobbyTab(int index)
    {
        if (view != null && view.gameObject.activeSelf && index != 2) Close();
        if (index == 2) RefreshStatus();
    }

    public void RefreshStatus()
    {
        if (requesting || lobby == null || NetworkManager.Instance == null || shortcut == null) return;
        RequestBoard();
    }

    public void Open()
    {
        if (view == null) return;
        view.gameObject.SetActive(true);
        view.transform.SetAsLastSibling();
        if (board != null) Render();
        if (!requesting) RequestBoard();
    }

    public void Close()
    {
        if (view == null) return;
        view.gameObject.SetActive(false);
    }

    private void SelectCycle(bool selectWeekly)
    {
        SelectTab(selectWeekly ? Tab.Weekly : Tab.Daily);
    }

    private void SelectTab(Tab tab)
    {
        selectedTab = tab;
        Render();
    }

    private void RequestBoard()
    {
        requesting = true;
        SetBusy(true);
        string path = QuestClientContract.BoardPath + UnityWebRequest.EscapeURL(lobby.CurrentUsername);
        NetworkManager.Instance.Get(path, json =>
        {
            requesting = false;
            board = JsonUtility.FromJson<QuestBoardResponse>(json);
            shortcut.SetClaimable(QuestClientContract.CountClaimable(board?.daily)
                                  + QuestClientContract.CountClaimable(board?.weekly)
                                  + QuestClientContract.CountClaimable(board?.achievements));
            SetBusy(false);
            if (view.gameObject.activeSelf) Render();
        }, Fail);
    }

    private void Render()
    {
        if (view == null || board == null) return;
        if (selectedTab == Tab.Achievement)
        {
            RenderAchievements();
            return;
        }

        bool weekly = selectedTab == Tab.Weekly;
        QuestCycleDto cycle = QuestClientContract.SelectCycle(board, weekly);
        if (cycle == null) return;
        view.titleText.text = weekly ? "주간 퀘스트" : "일일 퀘스트";
        view.activityText.text = "활동도 " + cycle.activityPoints + " / 100";
        view.activitySlider.minValue = 0;
        view.activitySlider.maxValue = 100;
        view.activitySlider.value = cycle.activityPoints;
        view.resetText.text = "초기화 " + QuestClientContract.FormatReset(cycle.nextResetAt, board.serverTime);
        view.dailyTabButton.interactable = weekly;
        view.weeklyTabButton.interactable = !weekly;
        view.achievementTabButton.interactable = true;
        SetCyclePanelsVisible(true);
        ClearGenerated(view.milestoneRoot, view.milestoneTemplate.gameObject);
        ClearGenerated(view.questRoot, view.questTemplate.gameObject);

        if (cycle.milestones != null)
            foreach (QuestMilestoneDto milestone in cycle.milestones)
            {
                Button button = Instantiate(view.milestoneTemplate, view.milestoneRoot, false);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<Text>().text = milestone.requiredActivityPoints + "\n"
                    + QuestClientContract.FormatReward(milestone.reward) + "\n"
                    + (milestone.claimed ? "수령 완료" : milestone.unlocked ? "받기" : "잠김");
                button.interactable = milestone.unlocked && !milestone.claimed && !requesting;
                int points = milestone.requiredActivityPoints;
                button.onClick.AddListener(() => ClaimMilestone(cycle.cycleType, points));
            }

        if (cycle.quests != null)
            foreach (QuestItemDto quest in cycle.quests)
            {
                Button button = Instantiate(view.questTemplate, view.questRoot, false);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<Text>().text = quest.title + "\n" + quest.description + "   "
                    + QuestClientContract.FormatProgress(quest.progress, quest.targetAmount) + "\n"
                    + QuestClientContract.FormatReward(quest.reward) + "   "
                    + (quest.claimed ? "수령 완료" : quest.completed ? "받기" : "진행 중");
                button.interactable = quest.completed && !quest.claimed && !requesting;
                string questId = quest.questId;
                button.onClick.AddListener(() => ClaimQuest(questId));
            }
        SetStatus(string.Empty);
    }

    private void RenderAchievements()
    {
        view.titleText.text = "업적";
        view.resetText.text = "영구 누적";
        view.dailyTabButton.interactable = true;
        view.weeklyTabButton.interactable = true;
        view.achievementTabButton.interactable = false;
        SetCyclePanelsVisible(false);
        ClearGenerated(view.milestoneRoot, view.milestoneTemplate.gameObject);
        ClearGenerated(view.questRoot, view.questTemplate.gameObject);

        if (board.achievements != null)
            foreach (QuestAchievementDto achievement in board.achievements)
            {
                Button button = Instantiate(view.questTemplate, view.questRoot, false);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<Text>().text = achievement.title + "  T" + achievement.tier + "\n"
                    + achievement.description + "   "
                    + QuestClientContract.FormatProgress(achievement.progress, achievement.targetAmount) + "\n"
                    + QuestClientContract.FormatReward(achievement.reward) + "   "
                    + (achievement.claimed ? "수령 완료" : achievement.completed ? "받기" : "진행 중");
                button.interactable = achievement.completed && !achievement.claimed && !requesting;
                string achievementId = achievement.achievementId;
                button.onClick.AddListener(() => ClaimAchievement(achievementId));
            }
        SetStatus(string.Empty);
    }

    private void SetCyclePanelsVisible(bool visible)
    {
        view.activityText.gameObject.SetActive(visible);
        view.activitySlider.gameObject.SetActive(visible);
        view.milestoneRoot.gameObject.SetActive(visible);
        if (view.questScrollRect != null)
            view.questScrollRect.offsetMax = new Vector2(-30f,
                visible ? -QuestUiView.CycleScrollTop : -QuestUiView.AchievementScrollTop);
    }

    private void ClaimQuest(string questId)
    {
        var request = new QuestClaimRequest
        {
            requestId = QuestClientContract.NewRequestId(), username = lobby.CurrentUsername, questId = questId
        };
        PostClaim(QuestClientContract.QuestClaimPath, JsonUtility.ToJson(request));
    }

    private void ClaimMilestone(string cycleType, int points)
    {
        var request = new QuestMilestoneClaimRequest
        {
            requestId = QuestClientContract.NewRequestId(), username = lobby.CurrentUsername,
            cycleType = cycleType, requiredActivityPoints = points
        };
        PostClaim(QuestClientContract.MilestoneClaimPath, JsonUtility.ToJson(request));
    }

    private void ClaimAchievement(string achievementId)
    {
        var request = new QuestAchievementClaimRequest
        {
            requestId = QuestClientContract.NewRequestId(), username = lobby.CurrentUsername,
            achievementId = achievementId
        };
        PostClaim(QuestClientContract.AchievementClaimPath, JsonUtility.ToJson(request));
    }

    private void PostClaim(string path, string body)
    {
        if (requesting) return;
        requesting = true;
        SetBusy(true);
        NetworkManager.Instance.PostJson(path, body, json =>
        {
            QuestClaimResponse response = JsonUtility.FromJson<QuestClaimResponse>(json);
            lobby.UpdateQuestWallet(response.wallet.gold, response.wallet.universalPiece, response.wallet.diamond);
            requesting = false;
            SetStatus(response.alreadyProcessed ? "이미 받은 보상입니다." : "보상을 받았습니다.");
            RequestBoard();
        }, Fail);
    }

    private void Fail(string error)
    {
        requesting = false;
        SetBusy(false);
        SetStatus("요청 실패: " + error);
        Debug.LogError("[Quest] " + error);
    }

    private void SetBusy(bool busy)
    {
        if (view?.inputCanvasGroup == null) return;
        // Network 요청 중에도 닫기와 로비 탭 이동은 항상 가능해야 한다.
        // 중복 보상 요청은 requesting guard가 막고, 새로 그린 보상 버튼은 Render에서 잠근다.
        view.inputCanvasGroup.interactable = true;
        view.inputCanvasGroup.blocksRaycasts = true;
        view.closeButton.interactable = true;
    }

    private void SetStatus(string message)
    {
        if (view?.statusText != null) view.statusText.text = message;
    }

    private static void ClearGenerated(Transform root, GameObject template)
    {
        if (root == null) return;
        for (int index = root.childCount - 1; index >= 0; index--)
        {
            GameObject child = root.GetChild(index).gameObject;
            if (child != template)
            {
                child.SetActive(false);
                Destroy(child);
            }
        }
    }

    private enum Tab
    {
        Daily,
        Weekly,
        Achievement
    }

}
