#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit, idempotent editor styling. Never runs automatically on import.</summary>
public static class LobbyNeonUiBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CardPath = "Assets/Prefabs/Lobby/UnitCard.prefab";
    private static readonly Color TextColor = new Color(0.86f, 0.96f, 1f, 1f);
    private static readonly string[] PopupPaths =
    {
        "Assets/Prefabs/Alien/AlienDetailScreen.prefab",
        "Assets/Resources/Prefabs/Lobby/Quest/QuestScreen.prefab",
        "Assets/Resources/Prefabs/Lobby/Quest/QuestShortcut.prefab",
        "Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingScreen.prefab",
        "Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingShortcut.prefab"
    };

    [MenuItem("Tools/MyDefense/Lobby/Apply Neon Collection Theme")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 로비 디자인을 적용하세요.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("SampleScene을 열고 사용자 변경을 저장한 뒤 실행하세요.");
        if (scene.isDirty)
            throw new InvalidOperationException("현재 Scene에 저장되지 않은 변경이 있어 자동 적용하지 않았습니다.");

        EditPrefab(CardPath, StyleCard);
        foreach (string path in PopupPaths) EditPrefab(path, StylePopup);

        LobbyManager lobby = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyManager>(true)).FirstOrDefault();
        if (lobby == null) throw new InvalidOperationException("SampleScene에서 LobbyManager를 찾지 못했습니다.");
        if (lobby.unitGridContent == null) throw new InvalidOperationException("유닛 Grid 참조가 없습니다.");
        StyleHeader(lobby);
        EnsureSectionGrid(lobby.unitGridContent.gameObject);
        LobbyCollectionGrid collectionGrid = lobby.unitGridContent.GetComponent<LobbyCollectionGrid>();
        if (collectionGrid == null) collectionGrid = lobby.unitGridContent.gameObject.AddComponent<LobbyCollectionGrid>();
        collectionGrid.RefreshLayout();
        ScrollRect scroll = lobby.unitGridContent.GetComponentInParent<ScrollRect>(true);
        if (scroll != null)
        {
            // Keep card clipping inside the decorative border, not on its outside edge.
            RectTransform viewport = EnsureChild(scroll.transform, "CollectionViewport");
            Place(viewport, 0f, 0f, 1f, 1f);
            viewport.offsetMin = new Vector2(0f, 30f);
            viewport.offsetMax = new Vector2(0f, -18f);
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
            lobby.unitGridContent.SetParent(viewport, false);
            RectTransform content = (RectTransform)lobby.unitGridContent;
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            scroll.viewport = viewport;
            scroll.verticalNormalizedPosition = 1f;
            collectionGrid.RefreshLayout();
            ApplyFrame(scroll.gameObject, LobbyNeonGraphic.FrameStyle.Panel);
            if (scroll.viewport != null)
            {
                Image viewportImage = scroll.viewport.GetComponent<Image>();
                if (viewportImage != null && scroll.viewport.GetComponent<Mask>() == null)
                    viewportImage.color = Color.clear;
            }
        }
        if (lobby.alienDetailController != null && lobby.alienDetailController.alienDetailScreen != null)
            StylePopup(lobby.alienDetailController.alienDetailScreen);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[LobbyNeon] 내 유닛 카드/목록 및 강화·교배·퀘스트 UI 네온 프레임 적용 완료.");
    }

    // May also be used by existing prefab generation tools after constructing their UI.
    public static void StyleHeader(LobbyManager lobby)
    {
        Canvas canvas = lobby.text_UserName.GetComponentInParent<Canvas>();
        RectTransform bar = canvas.transform.Find("tab_bar") as RectTransform;
        if (bar == null) throw new InvalidOperationException("로비 tab_bar를 찾지 못했습니다.");
        bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = Vector2.one;
        bar.pivot = new Vector2(.5f, 1f); bar.localScale = Vector3.one;
        bar.sizeDelta = new Vector2(0f, 116f); bar.anchoredPosition = new Vector2(0f, -20f);

        RectTransform profile = EnsureChild(bar, "LobbyProfileStatus");
        Place(profile, .03f, .08f, .365f, .92f);
        ApplyFrame(profile.gameObject, LobbyNeonGraphic.FrameStyle.Panel);
        Transform levelIcon = lobby.text_UserLevel.transform.parent;
        var slider = bar.GetComponentInChildren<Slider>(true);
        if (slider == null) throw new InvalidOperationException("경험치 Slider를 찾지 못했습니다.");
        levelIcon.SetParent(profile, false);
        Place((RectTransform)levelIcon, .025f, .12f, .19f, .88f);
        ApplyFrame(levelIcon.gameObject, LobbyNeonGraphic.FrameStyle.Button);
        Place(lobby.text_UserLevel.rectTransform, .08f, .08f, .92f, .92f);
        StyleHeaderText(lobby.text_UserLevel, 42f);
        lobby.text_UserName.transform.SetParent(profile, false);
        Place(lobby.text_UserName.rectTransform, .23f, .52f, .95f, .94f);
        StyleHeaderText(lobby.text_UserName, 26f);
        lobby.text_UserName.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

        slider.transform.SetParent(profile, false);
        Place((RectTransform)slider.transform, .23f, .14f, .95f, .46f);
        RectTransform bg = slider.transform.Find("Background") as RectTransform;
        if (bg != null) { Place(bg, 0f, 0f, 1f, 1f); ApplyFrame(bg.gameObject, LobbyNeonGraphic.FrameStyle.Panel); }
        RectTransform fillArea = slider.fillRect.parent as RectTransform;
        Place(fillArea, .02f, .12f, .98f, .88f);
        slider.fillRect.localScale = Vector3.one;
        slider.fillRect.offsetMin = slider.fillRect.offsetMax = Vector2.zero;
        Image fill = slider.fillRect.GetComponent<Image>();
        if (fill != null) { fill.sprite = null; fill.color = new Color(.20f, .53f, .58f, .72f); }
        foreach (var text in slider.GetComponentsInChildren<TMPro.TMP_Text>(true))
        { Place(text.rectTransform, 0f, 0f, 1f, 1f); StyleHeaderText(text, 20f); }
        // Preserve Slider references/value/listeners; its invisible handle is not part of the new artwork.
        if (slider.handleRect != null)
        {
            Place((RectTransform)slider.handleRect.parent, 0f, 0f, 1f, 1f);
            var handle = slider.handleRect.GetComponent<Image>();
            if (handle != null) handle.color = Color.clear;
        }

        StyleCurrency(bar, lobby.text_Heart, "Heart", .385f, .575f);
        StyleCurrency(bar, lobby.text_Gold, "Coin", .5875f, .7775f);
        StyleCurrency(bar, lobby.text_Diamond, "Gem", .79f, .98f);
    }

    private static void StyleCurrency(RectTransform bar, TMPro.TMP_Text value, string iconName, float left, float right)
    {
        RectTransform card = (RectTransform)value.transform.parent;
        GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(card.gameObject);
        if (instanceRoot != null)
        {
            if (!instanceRoot.transform.IsChildOf(bar))
                throw new InvalidOperationException("상단 바 외부 프리팹은 자동으로 해제하지 않습니다.");
            // Nested sample currency prefabs prohibit reparenting an inner child.
            // Unpack only this scene-local currency instance, never the source prefab.
            PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        card.SetParent(bar, false);
        if (card.parent != bar) throw new InvalidOperationException("재화 UI 부모 변경에 실패했습니다: " + value.name);
        Place(card, left, .22f, right, .78f);
        ApplyFrame(card.gameObject, LobbyNeonGraphic.FrameStyle.Panel);
        Place(value.rectTransform, .27f, .15f, .70f, .85f);
        StyleHeaderText(value, 25f);
        value.alignment = TMPro.TextAlignmentOptions.MidlineRight;
        var icon = card.Find(iconName) as RectTransform;
        if (icon != null)
        {
            Place(icon, .04f, .18f, .25f, .82f);
            var image = icon.GetComponent<Image>();
            if (image != null)
            {
                image.preserveAspect = true; image.raycastTarget = false;
                if (iconName == "Heart") image.color = new Color(1f, .20f, .25f, 1f);
                if (iconName == "Coin") image.color = new Color(1f, .84f, .38f, 1f);
                if (iconName == "Gem")
                {
                    Sprite gem = AssetDatabase.LoadAllAssetsAtPath(
                        "Assets/Space_Exploration_GUI_Kit/Icons/gem-1-64.png").OfType<Sprite>().FirstOrDefault();
                    if (gem == null) throw new InvalidOperationException("기존 젬 Sprite를 찾지 못했습니다.");
                    image.sprite = gem;
                    image.color = Color.white;
                    icon.localRotation = Quaternion.identity;
                }
            }
        }
        var plus = card.Find("Plus Button");
        if (plus != null)
        {
            Place((RectTransform)plus, .77f, .5f, .94f, .5f);
            var square = plus.GetComponent<AspectRatioFitter>();
            if (square == null) square = plus.gameObject.AddComponent<AspectRatioFitter>();
            square.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            square.aspectRatio = 1f;
            var frame = ApplyFrame(plus.gameObject, LobbyNeonGraphic.FrameStyle.Button);
            var button = plus.GetComponent<Button>();
            if (button != null) ConfigureButton(button, frame);
            var label = EnsureText(plus, "NeonPlusLabel", "+", 25);
            Place(label.rectTransform, 0f, 0f, 1f, 1f);
        }
    }

    private static void StyleHeaderText(TMPro.TMP_Text text, float maxSize)
    {
        text.color = TextColor; text.raycastTarget = false;
        text.enableAutoSizing = true; text.fontSizeMin = 14f; text.fontSizeMax = maxSize;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    }

    private static void EnsureSectionGrid(GameObject content)
    {
        var old = content.GetComponent<GridLayoutGroup>();
        if (old is LobbySectionGridLayout) return;
        var spacing = old != null ? old.spacing : new Vector2(20f, 10f);
        var padding = old != null ? old.padding : new RectOffset(36, 36, 30, 30);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var grid = content.AddComponent<LobbySectionGridLayout>();
        grid.spacing = spacing;
        grid.padding = padding;
    }

    public static void StylePopup(GameObject root)
    {
        ApplyFrame(root, root.GetComponent<Button>() != null
            ? LobbyNeonGraphic.FrameStyle.Button : LobbyNeonGraphic.FrameStyle.Panel);
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform == root.transform || image.GetComponent<Mask>() != null) continue;
            string name = image.name;
            bool panel = name.EndsWith("Panel", StringComparison.Ordinal) || name == "Header"
                || name == "TopBar" || name == "ActionBar" || name.EndsWith("Scroll", StringComparison.Ordinal);
            if (panel && image.sprite == null)
            {
                // Generated list containers delete their children on refresh. Keep decoration
                // on the stable popup/scroll parent rather than inside those lists.
                if (image.GetComponent<LayoutGroup>() != null)
                {
                    Transform staleFrame = image.transform.Find("LobbyNeonFrame");
                    if (staleFrame != null) UnityEngine.Object.DestroyImmediate(staleFrame.gameObject);
                    image.color = new Color(0.025f, 0.06f, 0.12f, 0.92f);
                }
                else ApplyFrame(image.gameObject, LobbyNeonGraphic.FrameStyle.Panel);
            }
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            LobbyNeonGraphic frame = ApplyFrame(button.gameObject, LobbyNeonGraphic.FrameStyle.Button);
            ConfigureButton(button, frame);
        }
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            // Error/status colours are runtime feedback and retain their existing semantics.
            if (text.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) < 0
                && text.name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) < 0)
                text.color = TextColor;
            text.raycastTarget = false;
        }
    }

    public static void StyleCard(GameObject root)
    {
        UnitCardUI card = root.GetComponent<UnitCardUI>();
        if (card == null) throw new InvalidOperationException("UnitCardUI 참조가 없는 프리팹입니다.");
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(210f, 275f);
        LobbyNeonGraphic frame = ApplyFrame(root, LobbyNeonGraphic.FrameStyle.Card);
        frame.SetOwned(true);
        SetReference(card, "neonFrame", frame);
        Button button = root.GetComponent<Button>();
        if (button != null) ConfigureButton(button, frame);

        RectTransform portraitWell = EnsureChild(root.transform, "PortraitWell");
        Place(portraitWell, 0.15f, 0.40f, 0.85f, 0.87f);
        LobbyNeonGraphic portraitFrame = ApplyFrame(portraitWell.gameObject, LobbyNeonGraphic.FrameStyle.Panel);
        portraitFrame.color = new Color(0.65f, 0.8f, 0.88f, 0.5f);
        Image portrait = EnsureChild(portraitWell, "Portrait").gameObject.GetComponent<Image>();
        if (portrait == null) portrait = portraitWell.Find("Portrait").gameObject.AddComponent<Image>();
        Place(portrait.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portrait.enabled = portrait.sprite != null;
        SetReference(card, "portraitImage", portrait);
        Text glyph = EnsureText(portraitWell, "PortraitFallback", "N", 68);
        Place(glyph.rectTransform, 0.1f, 0.1f, 0.9f, 0.9f);
        SetReference(card, "portraitFallbackText", glyph);

        ConfigureText(card.text_Name, 22, FontStyle.Bold, 0.07f, 0.28f, 0.93f, 0.40f);
        ConfigureText(card.text_Grade, 17, FontStyle.Bold, 0.07f, 0.87f, 0.60f, 0.96f);
        if (card.text_Grade != null) card.text_Grade.alignment = TextAnchor.MiddleLeft;
        ConfigureText(card.text_Level, 17, FontStyle.Normal, 0.58f, 0.87f, 0.94f, 0.96f);
        if (card.text_Level != null) card.text_Level.alignment = TextAnchor.MiddleRight;
        ConfigureText(card.text_Pieces, 20, FontStyle.Bold, 0.09f, 0.045f, 0.91f, 0.15f);

        Text ownership = EnsureText(root.transform, "OwnershipStatus", "보유", 17);
        Place(ownership.rectTransform, 0.07f, 0.17f, 0.93f, 0.25f);
        ownership.color = new Color(0.57f, 0.8f, 0.85f, 1f);
        SetReference(card, "ownershipStatusText", ownership);
        if (card.image_Lock != null)
        {
            // Preserve its serialized reference, but remove the opaque card-covering overlay.
            Place(card.image_Lock.rectTransform, 0.07f, 0.17f, 0.93f, 0.25f);
            card.image_Lock.color = Color.clear;
            card.image_Lock.raycastTarget = false;
            card.image_Lock.gameObject.SetActive(false);
        }
        frame.transform.SetAsFirstSibling();
        portraitWell.SetSiblingIndex(1);
    }

    public static LobbyNeonGraphic ApplyFrame(GameObject owner, LobbyNeonGraphic.FrameStyle style)
    {
        RectTransform visual = EnsureChild(owner.transform, "LobbyNeonFrame");
        Place(visual, 0f, 0f, 1f, 1f);
        visual.SetAsFirstSibling();
        LayoutElement layout = visual.GetComponent<LayoutElement>();
        if (layout == null) layout = visual.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        // RequireComponent covers new graphics; explicitly repair frames saved by older builders.
        if (visual.GetComponent<CanvasRenderer>() == null)
            visual.gameObject.AddComponent<CanvasRenderer>();
        LobbyNeonGraphic graphic = visual.GetComponent<LobbyNeonGraphic>();
        if (graphic == null) graphic = visual.gameObject.AddComponent<LobbyNeonGraphic>();
        graphic.Configure(style);
        graphic.raycastTarget = false;
        Image oldBackground = owner.GetComponent<Image>();
        if (oldBackground != null && owner.GetComponent<Mask>() == null)
            oldBackground.color = Color.clear;
        return graphic;
    }

    private static void ConfigureButton(Button button, LobbyNeonGraphic frame)
    {
        button.targetGraphic = frame;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.8f, 1f, 1f, 1f);
        colors.selectedColor = new Color(0.85f, 0.83f, 1f, 1f);
        colors.pressedColor = new Color(0.5f, 0.7f, 0.85f, 1f);
        colors.disabledColor = new Color(0.48f, 0.53f, 0.61f, 0.85f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
    }

    private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException("필수 UI 필드가 없습니다: " + field);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureText(Text text, int size, FontStyle style, float x0, float y0, float x1, float y1)
    {
        if (text == null) return;
        Place(text.rectTransform, x0, y0, x1, y1);
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = TextColor;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(14, size - 4);
        text.resizeTextMaxSize = size;
    }

    private static Text EnsureText(Transform parent, string name, string value, int size)
    {
        RectTransform rect = EnsureChild(parent, name);
        Text text = rect.GetComponent<Text>();
        if (text == null) text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = TextColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<RectTransform>();
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void EditPrefab(string path, Action<GameObject> apply)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            throw new InvalidOperationException("대상 UI 프리팹을 찾을 수 없습니다: " + path);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try { apply(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
