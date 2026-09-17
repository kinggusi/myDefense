#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class QuestUiPrefabBuilder
{
    private const string Folder = "Assets/Resources/Prefabs/Lobby/Quest";

    [MenuItem("Tools/MyDefense/Quest/Rebuild UI Prefabs")]
    public static void Rebuild()
    {
        EnsureFolder(Folder);
        BuildShortcut();
        BuildScreen();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Quest] Quest UI Prefab 2종을 생성했습니다.");
    }

    private static void BuildShortcut()
    {
        GameObject root = UiObject("QuestShortcut", null, new Color(0.15f, 0.28f, 0.50f, 0.98f));
        Rect(root).sizeDelta = new Vector2(410f, 118f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        Text title = TextObject("Title", root.transform, "퀘스트", 30, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(title.rectTransform, new Vector2(22f, -12f), new Vector2(280f, 48f), new Vector2(0f, 1f));
        Text status = TextObject("Status", root.transform, "오늘의 임무 확인", 20, FontStyle.Normal, TextAnchor.MiddleLeft);
        SetRect(status.rectTransform, new Vector2(22f, -62f), new Vector2(330f, 38f), new Vector2(0f, 1f));
        GameObject badge = UiObject("Badge", root.transform, new Color(0.96f, 0.32f, 0.26f, 1f));
        SetRect(Rect(badge), new Vector2(-18f, -18f), new Vector2(54f, 54f), new Vector2(1f, 1f));
        Text badgeText = TextObject("Count", badge.transform, "1", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(badgeText.rectTransform);
        QuestShortcutView view = root.AddComponent<QuestShortcutView>();
        view.button = button;
        view.statusText = status;
        view.badgeObject = badge;
        view.badgeText = badgeText;
        LobbyNeonUiBuilder.StylePopup(root);
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/QuestShortcut.prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildScreen()
    {
        GameObject root = UiObject("QuestScreen", null, new Color(0.035f, 0.06f, 0.12f, 0.995f));
        RectTransform rootRect = Rect(root);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = new Vector2(0f, -150f);
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

        GameObject header = UiObject("Header", root.transform, new Color(0.09f, 0.18f, 0.34f, 1f));
        TopBand(Rect(header), 0f, 110f, 0f, 0f);
        Button close = ButtonObject("CloseButton", header.transform, "닫기", 24, new Color(0.22f, 0.33f, 0.52f, 1f));
        SetRect(Rect(close.gameObject), new Vector2(18f, -12f), new Vector2(150f, 86f), new Vector2(0f, 1f));
        Text title = TextObject("Title", header.transform, "일일 퀘스트", 38, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, new Vector2(-35f, -10f), new Vector2(430f, 88f), new Vector2(0.5f, 1f));
        Text reset = TextObject("Reset", header.transform, "초기화 서버 기준", 20, FontStyle.Normal, TextAnchor.MiddleRight);
        SetRect(reset.rectTransform, new Vector2(-20f, -25f), new Vector2(285f, 58f), new Vector2(1f, 1f));

        Button daily = ButtonObject("DailyTab", root.transform, "일일", 28, new Color(0.20f, 0.46f, 0.75f, 1f));
        SetRect(Rect(daily.gameObject), new Vector2(-250f, -120f), new Vector2(220f, 68f), new Vector2(0.5f, 1f));
        Button weekly = ButtonObject("WeeklyTab", root.transform, "주간", 28, new Color(0.22f, 0.32f, 0.54f, 1f));
        SetRect(Rect(weekly.gameObject), new Vector2(0f, -120f), new Vector2(220f, 68f), new Vector2(0.5f, 1f));
        Button achievement = ButtonObject("AchievementTab", root.transform, "업적", 28, new Color(0.34f, 0.25f, 0.55f, 1f));
        SetRect(Rect(achievement.gameObject), new Vector2(250f, -120f), new Vector2(220f, 68f), new Vector2(0.5f, 1f));

        Text activity = TextObject("Activity", root.transform, "활동도 0 / 100", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(activity.rectTransform, new Vector2(35f, -210f), new Vector2(270f, 50f), new Vector2(0f, 1f));
        Slider slider = CreateSlider(root.transform);
        TopBand(Rect(slider.gameObject), 218f, 32f, 310f, 35f);

        GameObject milestonePanel = new GameObject("Milestones", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        milestonePanel.transform.SetParent(root.transform, false);
        TopBand(Rect(milestonePanel), 280f, 140f, 30f, 30f);
        HorizontalLayoutGroup milestoneLayout = milestonePanel.GetComponent<HorizontalLayoutGroup>();
        milestoneLayout.spacing = 14f;
        milestoneLayout.padding = new RectOffset(12, 12, 10, 10);
        milestoneLayout.childControlWidth = true;
        milestoneLayout.childForceExpandWidth = true;
        milestoneLayout.childControlHeight = true;
        milestoneLayout.childForceExpandHeight = true;
        Button milestoneTemplate = ButtonObject("MilestoneTemplate", milestonePanel.transform, "25\n보상\n잠김", 19,
                new Color(0.20f, 0.36f, 0.56f, 1f));
        milestoneTemplate.gameObject.SetActive(false);

        ScrollRect questScroll = CreateScroll("QuestScroll", root.transform);
        RectTransform scrollRect = Rect(questScroll.gameObject);
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(30f, 100f);
        scrollRect.offsetMax = new Vector2(-30f, -QuestUiView.CycleScrollTop);
        VerticalLayoutGroup questLayout = questScroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
        questLayout.spacing = 14f;
        questLayout.padding = new RectOffset(12, 12, 12, 12);
        questLayout.childControlHeight = true;
        questLayout.childForceExpandHeight = false;
        questLayout.childControlWidth = true;
        questLayout.childForceExpandWidth = true;
        ContentSizeFitter fitter = questScroll.content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Button questTemplate = ButtonObject("QuestTemplate", questScroll.content, "퀘스트\n진행도\n보상", 22,
                new Color(0.10f, 0.20f, 0.34f, 1f));
        questTemplate.gameObject.AddComponent<LayoutElement>().preferredHeight = 135f;
        questTemplate.gameObject.SetActive(false);

        Text status = TextObject("Status", root.transform, string.Empty, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(status.rectTransform, new Vector2(0f, 52f), new Vector2(-60f, 58f), new Vector2(0f, 0f), new Vector2(1f, 0f));

        QuestUiView view = root.AddComponent<QuestUiView>();
        view.inputCanvasGroup = canvasGroup;
        view.closeButton = close;
        view.dailyTabButton = daily;
        view.weeklyTabButton = weekly;
        view.achievementTabButton = achievement;
        view.titleText = title;
        view.resetText = reset;
        view.activityText = activity;
        view.activitySlider = slider;
        view.milestoneRoot = milestonePanel.transform;
        view.milestoneTemplate = milestoneTemplate;
        view.questRoot = questScroll.content;
        view.questScrollRect = scrollRect;
        view.questTemplate = questTemplate;
        view.statusText = status;
        LobbyNeonUiBuilder.StylePopup(root);
        // Stable header is drawn above scrolling/list content, including its close hit area.
        header.transform.SetAsLastSibling();
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/QuestScreen.prefab");
        Object.DestroyImmediate(root);
    }

    private static Slider CreateSlider(Transform parent)
    {
        GameObject root = UiObject("ActivitySlider", parent, new Color(0.08f, 0.13f, 0.22f, 1f));
        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        Stretch(Rect(fillArea));
        GameObject fill = UiObject("Fill", fillArea.transform, new Color(0.27f, 0.72f, 0.92f, 1f));
        Stretch(Rect(fill));
        Slider slider = root.AddComponent<Slider>();
        slider.fillRect = Rect(fill);
        slider.targetGraphic = root.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        return slider;
    }

    private static ScrollRect CreateScroll(string name, Transform parent)
    {
        GameObject root = UiObject(name, parent, new Color(0.055f, 0.10f, 0.18f, 1f));
        GameObject viewport = UiObject("Viewport", root.transform, new Color(1f, 1f, 1f, 0.01f));
        Stretch(Rect(viewport));
        viewport.AddComponent<RectMask2D>();
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = Rect(content);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = Rect(viewport);
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return scroll;
    }

    private static Button ButtonObject(string name, Transform parent, string label, int size, Color color)
    {
        GameObject root = UiObject(name, parent, color);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        Text text = TextObject("Label", root.transform, label, size, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    private static Text TextObject(string name, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject UiObject(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();
    private static void TopBand(RectTransform rect, float top, float height, float left, float right)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        => SetRect(rect, position, size, anchor, anchor);
    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
}
#endif
