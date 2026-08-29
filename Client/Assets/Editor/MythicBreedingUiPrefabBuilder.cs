#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MythicBreedingUiPrefabBuilder
{
    private const string Folder = "Assets/Resources/Prefabs/Lobby/MythicBreeding";

    [MenuItem("Tools/MyDefense/Breeding/Rebuild UI Prefabs")]
    public static void Rebuild()
    {
        EnsureFolder(Folder);
        BuildShortcut();
        BuildScreen();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Breeding] 정식 UI Prefab 2종을 생성했습니다.");
    }

    private static void BuildShortcut()
    {
        GameObject root = UiObject("MythicBreedingShortcut", null, new Color(0.19f, 0.10f, 0.36f, 0.98f));
        Rect(root).sizeDelta = new Vector2(430f, 130f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        Text title = TextObject("Title", root.transform, "신화 교배", 30, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(title.rectTransform, new Vector2(24f, -18f), new Vector2(320f, 48f), new Vector2(0f, 1f));
        Text status = TextObject("Status", root.transform, "교배 상태 확인", 20, FontStyle.Normal, TextAnchor.MiddleLeft);
        SetRect(status.rectTransform, new Vector2(24f, -70f), new Vector2(360f, 40f), new Vector2(0f, 1f));
        GameObject badge = UiObject("RewardBadge", root.transform, new Color(0.95f, 0.28f, 0.38f, 1f));
        SetRect(Rect(badge), new Vector2(-18f, -18f), new Vector2(56f, 56f), new Vector2(1f, 1f));
        Text badgeText = TextObject("Count", badge.transform, "1", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(badgeText.rectTransform);
        var view = root.AddComponent<MythicBreedingShortcutView>();
        view.button = button;
        view.titleText = title;
        view.statusText = status;
        view.badgeObject = badge;
        view.badgeText = badgeText;
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/MythicBreedingShortcut.prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildScreen()
    {
        GameObject root = UiObject("MythicBreedingScreen", null, new Color(0.035f, 0.025f, 0.075f, 0.99f));
        RectTransform rootRect = Rect(root);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = new Vector2(0f, -150f);
        CanvasGroup inputCanvasGroup = root.AddComponent<CanvasGroup>();

        GameObject header = UiObject("Header", root.transform, new Color(0.12f, 0.065f, 0.22f, 1f));
        SetRect(Rect(header), new Vector2(0f, -52.5f), new Vector2(0f, 105f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        Button back = ButtonObject("BackButton", header.transform, "뒤로", 24, new Color(0.31f, 0.18f, 0.48f, 1f));
        SetRect(Rect(back.gameObject), new Vector2(20f, -20f), new Vector2(145f, 64f), new Vector2(0f, 1f));
        Text title = TextObject("Title", header.transform, "신화 교배", 38, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, Vector2.zero, new Vector2(600f, 90f), new Vector2(0.5f, 1f));
        Button combinations = ButtonObject("CombinationButton", header.transform, "조합표 보기", 22, new Color(0.36f, 0.20f, 0.55f, 1f));
        SetRect(Rect(combinations.gameObject), new Vector2(-20f, -20f), new Vector2(190f, 64f), new Vector2(1f, 1f));

        GameObject slotsPanel = UiObject("SlotsPanel", root.transform, new Color(0.09f, 0.06f, 0.16f, 1f));
        SetRect(Rect(slotsPanel), new Vector2(0f, -202.5f), new Vector2(-60f, 155f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        HorizontalLayoutGroup slotLayout = slotsPanel.AddComponent<HorizontalLayoutGroup>();
        slotLayout.padding = new RectOffset(16, 16, 16, 16);
        slotLayout.spacing = 16f;
        slotLayout.childControlWidth = true;
        slotLayout.childForceExpandWidth = true;
        slotLayout.childControlHeight = true;
        slotLayout.childForceExpandHeight = true;
        Button slotTemplate = ButtonObject("SlotTemplate", slotsPanel.transform, "슬롯", 22, new Color(0.23f, 0.14f, 0.38f, 1f));
        slotTemplate.gameObject.SetActive(false);

        Text parents = TextObject("ParentSelection", root.transform, "서로 다른 보유 신화 2종을 선택하세요.", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(parents.rectTransform, new Vector2(0f, -319f), new Vector2(-76f, 48f), new Vector2(0f, 1f), new Vector2(1f, 1f));

        ScrollRect candidates = CreateScroll("CandidateScroll", root.transform, new Vector2(0f, -710f), new Vector2(-60f, 710f));
        GridLayoutGroup grid = candidates.content.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(14, 14, 14, 14);
        grid.spacing = new Vector2(12f, 12f);
        grid.cellSize = new Vector2(184f, 98f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        ContentSizeFitter candidateFitter = candidates.content.gameObject.AddComponent<ContentSizeFitter>();
        candidateFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Button candidateTemplate = ButtonObject("CandidateTemplate", candidates.content, "신화", 20, new Color(0.17f, 0.12f, 0.30f, 1f));
        candidateTemplate.gameObject.SetActive(false);

        GameObject actionBar = UiObject("ActionBar", root.transform, new Color(0.07f, 0.045f, 0.12f, 1f));
        SetRect(Rect(actionBar), new Vector2(0f, 95f), new Vector2(0f, 190f), new Vector2(0f, 0f), new Vector2(1f, 0f));
        Button primary = ButtonObject("PrimaryAction", actionBar.transform, "교배 시작", 26, new Color(0.42f, 0.20f, 0.62f, 1f));
        SetRect(Rect(primary.gameObject), new Vector2(-345f, 92f), new Vector2(310f, 74f), new Vector2(0.5f, 0f));
        Button accelerate = ButtonObject("Accelerate10Minutes", actionBar.transform, "10분 단축 (100)", 24, new Color(0.20f, 0.38f, 0.58f, 1f));
        SetRect(Rect(accelerate.gameObject), new Vector2(0f, 92f), new Vector2(310f, 74f), new Vector2(0.5f, 0f));
        Button instant = ButtonObject("AccelerateInstant", actionBar.transform, "즉시 완료", 24, new Color(0.36f, 0.32f, 0.56f, 1f));
        SetRect(Rect(instant.gameObject), new Vector2(345f, 92f), new Vector2(310f, 74f), new Vector2(0.5f, 0f));
        Text status = TextObject("Status", actionBar.transform, string.Empty, 21, FontStyle.Normal, TextAnchor.MiddleCenter);
        SetRect(status.rectTransform, new Vector2(0f, 41f), new Vector2(-60f, 58f), new Vector2(0f, 0f), new Vector2(1f, 0f));

        GameObject combinationPanel = UiObject("CombinationPanel", root.transform, new Color(0.025f, 0.018f, 0.055f, 0.995f));
        Stretch(Rect(combinationPanel));
        Text combinationTitle = TextObject("Title", combinationPanel.transform, "공개 신화 교배 조합표", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(combinationTitle.rectTransform, Vector2.zero, new Vector2(700f, 90f), new Vector2(0.5f, 1f));
        Button combinationClose = ButtonObject("CloseButton", combinationPanel.transform, "닫기", 24, new Color(0.35f, 0.18f, 0.48f, 1f));
        SetRect(Rect(combinationClose.gameObject), new Vector2(-24f, -20f), new Vector2(150f, 64f), new Vector2(1f, 1f));
        ScrollRect combinationScroll = CreateScroll("CombinationScroll", combinationPanel.transform, Vector2.zero, Vector2.zero);
        RectTransform combinationScrollRect = Rect(combinationScroll.gameObject);
        combinationScrollRect.anchorMin = Vector2.zero;
        combinationScrollRect.anchorMax = Vector2.one;
        combinationScrollRect.offsetMin = new Vector2(35f, 55f);
        combinationScrollRect.offsetMax = new Vector2(-35f, -105f);
        Text combinationText = TextObject("CombinationText", combinationScroll.content, "공개 조합표를 불러오는 중...", 20, FontStyle.Normal, TextAnchor.UpperLeft);
        combinationText.horizontalOverflow = HorizontalWrapMode.Overflow;
        combinationText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform tableRect = combinationText.rectTransform;
        tableRect.anchorMin = new Vector2(0f, 1f);
        tableRect.anchorMax = new Vector2(1f, 1f);
        tableRect.pivot = new Vector2(0.5f, 1f);
        tableRect.anchoredPosition = Vector2.zero;
        tableRect.sizeDelta = new Vector2(-30f, 0f);
        ContentSizeFitter tableFitter = combinationText.gameObject.AddComponent<ContentSizeFitter>();
        tableFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        combinationPanel.SetActive(false);

        var view = root.AddComponent<MythicBreedingUiView>();
        view.inputCanvasGroup = inputCanvasGroup;
        view.backButton = back;
        view.combinationButton = combinations;
        view.primaryActionButton = primary;
        view.accelerateButton = accelerate;
        view.instantButton = instant;
        view.parentSelectionText = parents;
        view.statusText = status;
        view.slotRoot = slotsPanel.transform;
        view.candidateRoot = candidates.content;
        view.slotButtonTemplate = slotTemplate;
        view.candidateButtonTemplate = candidateTemplate;
        view.combinationPanel = combinationPanel;
        view.combinationCloseButton = combinationClose;
        view.combinationText = combinationText;
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/MythicBreedingScreen.prefab");
        Object.DestroyImmediate(root);
    }

    private static ScrollRect CreateScroll(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject root = UiObject(name, parent, new Color(0.055f, 0.04f, 0.10f, 1f));
        SetRect(Rect(root), position, size, new Vector2(0f, 1f), new Vector2(1f, 1f));
        GameObject viewport = UiObject("Viewport", root.transform, new Color(1f, 1f, 1f, 0.01f));
        Stretch(Rect(viewport));
        viewport.AddComponent<RectMask2D>();
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = Rect(content);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = Rect(viewport);
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
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
        text.resizeTextForBestFit = false;
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
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
