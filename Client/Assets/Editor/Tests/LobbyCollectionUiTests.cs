using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyCollectionUiTests
{
    [Test]
    public void SavedSampleHeaderCurrencyRectsHaveUsableWidth()
    {
        const string path = "Assets/Scenes/SampleScene.unity";
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,
            UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            LobbyManager lobby = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                lobby = root.GetComponentInChildren<LobbyManager>(true);
                if (lobby != null) break;
            }
            Assert.That(lobby, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            var scroll = lobby.unitGridContent.GetComponentInParent<ScrollRect>();
            Assert.That(scroll.viewport, Is.Not.SameAs(scroll.transform));
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(lobby.unitGridContent.parent, Is.SameAs(scroll.viewport));
            Assert.That(lobby.unitGridContent.GetComponent<LobbySectionGridLayout>(), Is.Not.Null);
            Assert.That(scroll.viewport.offsetMin.y, Is.GreaterThanOrEqualTo(30f));
            var gem = lobby.text_Diamond.transform.parent.Find("Gem").GetComponent<Image>();
            Assert.That(AssetDatabase.GetAssetPath(gem.sprite), Does.EndWith("/gem-1-64.png"));
            Assert.That(gem.transform.localRotation, Is.EqualTo(Quaternion.identity));
            float goldWidth = ((RectTransform)lobby.text_Gold.transform.parent).rect.width;
            foreach (var text in new[] { lobby.text_Heart, lobby.text_Gold, lobby.text_Diamond })
            {
                var card = text.transform.parent as RectTransform;
                Assert.That(card.parent.name, Is.EqualTo("tab_bar"), text.name);
                Assert.That(card.rect.width, Is.GreaterThan(100f), text.name);
                Assert.That(card.rect.width, Is.EqualTo(goldWidth).Within(.01f), text.name);
                Assert.That(text.rectTransform.rect.width, Is.GreaterThan(50f), text.name);
                Assert.That(text.alignment, Is.EqualTo(TMPro.TextAlignmentOptions.MidlineRight));
                Assert.That(((RectTransform)card.Find("Plus Button")).anchorMin.x - text.rectTransform.anchorMax.x,
                    Is.GreaterThanOrEqualTo(.069f), "Number needs visible breathing room before plus.");
                Assert.That(card.GetComponent<Image>().color.a, Is.Zero);
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(card), Is.False,
                    "Scene-local currency styling must survive reload.");
                Assert.That(card.Find("LobbyNeonFrame"), Is.Not.Null);
                Assert.That(card.Find("Plus Button").GetComponent<Button>(), Is.Not.Null);
                Assert.That(((RectTransform)card.Find("Plus Button")).anchorMax.x, Is.LessThanOrEqualTo(.94f));
                AssertSquare((RectTransform)card.Find("Plus Button"));
            }
            var heart = lobby.text_Heart.transform.parent.Find("Heart").GetComponent<Image>().color;
            Assert.That(heart.r, Is.GreaterThan(heart.g * 3f));
            Assert.That(heart.b, Is.LessThan(.3f));
        }
        finally { if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true); }
    }

    [TestCase("NORMAL", 220f/255f, 229f/255f, 232f/255f)]
    [TestCase("EPIC", 189f/255f, 101f/255f, 242f/255f)]
    [TestCase("UNIQUE", 245f/255f, 196f/255f, 81f/255f)]
    [TestCase("LEGEND", 66f/255f, 232f/255f, 135f/255f)]
    [TestCase("LEGENDARY", 66f/255f, 232f/255f, 135f/255f)]
    [TestCase("MYTHIC", 1f, 79f/255f, 131f/255f)]
    public void GradeFrameUsesApprovedFinalPalette(string grade, float red, float green, float blue)
    {
        var color = LobbyNeonGraphic.GradeBorderColor(grade);
        Assert.That(color.r, Is.EqualTo(red).Within(.001f));
        Assert.That(color.g, Is.EqualTo(green).Within(.001f));
        Assert.That(color.b, Is.EqualTo(blue).Within(.001f));
    }

    [Test]
    public void MaterialIconsAndPlusButtonsOpenCorrectPendingConfirmation()
    {
        var canvas = new GameObject("MaterialCanvasTest", typeof(Canvas));
        var unit = new GameObject("view_Unit", typeof(RectTransform));
        var manager = new GameObject("MaterialLobbyTest", typeof(LobbyManager));
        unit.transform.SetParent(canvas.transform, false);
        manager.transform.SetParent(canvas.transform, false);
        try
        {
            var lobby = manager.GetComponent<LobbyManager>();
            lobby.viewObjects = new[] { unit, unit };
            var ensure = typeof(LobbyManager).GetMethod("EnsureMaterialCurrencyUI",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(ensure, Is.Not.Null);
            ensure.Invoke(lobby, null);
            ensure.Invoke(lobby, null);
            var icons = unit.GetComponentsInChildren<MyDefense.Lobby.LobbyMaterialIconGraphic>(true);
            Assert.That(icons.Length, Is.EqualTo(2));
            var plus = unit.GetComponentsInChildren<Button>(true);
            Assert.That(plus.Length, Is.EqualTo(2));
            Canvas.ForceUpdateCanvases();
            foreach (var button in plus) AssertSquare((RectTransform)button.transform);
            foreach (var icon in icons)
            {
                var border = icon.transform.parent.Find("LobbyNeonFrame");
                Assert.That(border, Is.Not.Null);
                Assert.That(border.GetComponent<LobbyNeonGraphic>().raycastTarget, Is.False);
            }
            foreach (var icon in icons) Assert.That(icon.GetComponent<CanvasRenderer>(), Is.Not.Null);
            plus[0].onClick.Invoke();
            var popup = manager.GetComponent<MyDefense.Lobby.LobbyMaterialPurchasePopup>();
            Assert.That(popup.IsOpen, Is.True);
            Assert.That(System.Array.Exists(canvas.GetComponentsInChildren<Text>(true), t => t.text == "왹져 DNA 구매"), Is.True);
            popup.Close();
            plus[1].onClick.Invoke();
            Assert.That(System.Array.Exists(canvas.GetComponentsInChildren<Text>(true), t => t.text == "성장 세포 구매"), Is.True);
            var buy = System.Array.Find(canvas.GetComponentsInChildren<Button>(true), b => b.name == "Purchase");
            Assert.That(buy.interactable, Is.False, "가격 미확정 상태에서 재화 차감은 허용하지 않는다.");
            popup.Close();
            Assert.That(popup.IsOpen, Is.False);
        }
        finally { Object.DestroyImmediate(manager); Object.DestroyImmediate(canvas); }
    }

    private static void AssertSquare(RectTransform rect)
    {
        var fitter = rect.GetComponent<AspectRatioFitter>();
        Assert.That(fitter, Is.Not.Null);
        Assert.That(fitter.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.WidthControlsHeight));
        Assert.That(fitter.aspectRatio, Is.EqualTo(1f));
        Assert.That(rect.rect.width, Is.GreaterThan(0f));
        Assert.That(rect.rect.width, Is.EqualTo(rect.rect.height).Within(.01f), rect.name);
    }

    [TestCase(0, 640f)]
    [TestCase(2, 940f)]
    [TestCase(4, 1200f)]
    [TestCase(7, 940f)]
    public void LockedSectionStartsOnNewRowAndFitsFullWidth(int ownedMythics, float width)
    {
        var root = new GameObject("SectionGridTest", typeof(RectTransform), typeof(LobbySectionGridLayout));
        var manager = new GameObject("SectionLobby", typeof(LobbyManager));
        try
        {
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(width, 1000f);
            var grid = root.GetComponent<LobbySectionGridLayout>();
            grid.padding = new RectOffset(36, 36, 30, 30);
            grid.spacing = new Vector2(20f, 10f);
            root.AddComponent<LobbyCollectionGrid>().RefreshLayout();
            var lobby = manager.GetComponent<LobbyManager>();
            lobby.unitGridContent = root.transform;
            lobby.unitCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lobby/UnitCard.prefab");
            var data = new System.Collections.Generic.List<AlienInventoryDto>();
            for (int id = 1; id <= 48; id++) data.Add(new AlienInventoryDto {
                id = id, name = "Unit " + id, grade = id > 28 ? "MYTHIC" : "NORMAL",
                owned = id <= 28 + ownedMythics, level = id <= 28 + ownedMythics ? 1 : 0 });
            var spawn = typeof(LobbyManager).GetMethod("SpawnMyUnits",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            spawn.Invoke(lobby, new object[] { data });
            spawn.Invoke(lobby, new object[] { data });
            grid.CalculateLayoutInputHorizontal(); grid.SetLayoutHorizontal();
            grid.CalculateLayoutInputVertical(); grid.SetLayoutVertical();
            Assert.That(root.GetComponentsInChildren<UnitCardUI>().Length, Is.EqualTo(48));
            Assert.That(root.transform.childCount, Is.EqualTo(49), "No duplicate section on reload.");
            var header = (RectTransform)root.transform.Find(LobbySectionGridLayout.LockedHeaderName);
            Assert.That(header.GetSiblingIndex(), Is.EqualTo(28 + ownedMythics));
            Assert.That(header.rect.width, Is.EqualTo(width - 72f).Within(.01f));
            var previous = (RectTransform)root.transform.GetChild(header.GetSiblingIndex() - 1);
            var firstLocked = (RectTransform)root.transform.GetChild(header.GetSiblingIndex() + 1);
            Assert.That(header.anchoredPosition.y + header.rect.height * header.pivot.y,
                Is.LessThan(previous.anchoredPosition.y - previous.rect.height * (1f - previous.pivot.y)));
            Assert.That(firstLocked.anchoredPosition.x - firstLocked.rect.width * firstLocked.pivot.x,
                Is.EqualTo(36f).Within(.01f));
            Assert.That(firstLocked.anchoredPosition.y + firstLocked.rect.height * firstLocked.pivot.y,
                Is.LessThan(header.anchoredPosition.y - header.rect.height * (1f - header.pivot.y)));
            Assert.That(grid.preferredHeight, Is.GreaterThan(-firstLocked.anchoredPosition.y));
            foreach (var item in data) { item.owned = true; item.level = 1; }
            spawn.Invoke(lobby, new object[] { data });
            Assert.That(root.transform.Find(LobbySectionGridLayout.LockedHeaderName), Is.Null);
            Assert.That(root.transform.childCount, Is.EqualTo(48));
        }
        finally { Object.DestroyImmediate(manager); Object.DestroyImmediate(root); }
    }

    [Test]
    public void NewNeonGraphicRequiresCanvasRenderer()
    {
        var root = new GameObject("NeonRendererTest", typeof(RectTransform));
        try
        {
            root.AddComponent<LobbyNeonGraphic>();
            Assert.That(root.GetComponent<CanvasRenderer>(), Is.Not.Null);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase("Assets/Prefabs/Lobby/UnitCard.prefab")]
    [TestCase("Assets/Prefabs/Alien/AlienDetailScreen.prefab")]
    [TestCase("Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingScreen.prefab")]
    [TestCase("Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingShortcut.prefab")]
    [TestCase("Assets/Resources/Prefabs/Lobby/Quest/QuestScreen.prefab")]
    [TestCase("Assets/Resources/Prefabs/Lobby/Quest/QuestShortcut.prefab")]
    public void SavedNeonPrefabsHaveRenderersAndRebuildOnCanvas(string path)
    {
        var canvasRoot = new GameObject("NeonCanvasTest", typeof(Canvas));
        try
        {
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            var frames = prefab.GetComponentsInChildren<LobbyNeonGraphic>(true);
            Assert.That(frames, Is.Not.Empty);
            foreach (var frame in frames)
                Assert.That(frame.GetComponent<CanvasRenderer>(), Is.Not.Null, path + ": " + frame.transform.parent.name);
            var instance = Object.Instantiate(prefab, canvasRoot.transform);
            instance.SetActive(true);
            foreach (var frame in instance.GetComponentsInChildren<LobbyNeonGraphic>(true))
            {
                frame.Rebuild(CanvasUpdate.PreRender);
                Assert.That(frame.canvasRenderer, Is.Not.Null);
            }
            Canvas.ForceUpdateCanvases();
        }
        finally { Object.DestroyImmediate(canvasRoot); }
    }

    [TestCase(940f)]
    [TestCase(640f)]
    [TestCase(1200f)]
    public void GridFitsExactlyFourColumnsAtDifferentWidths(float width)
    {
        var root = new GameObject("CollectionTest", typeof(RectTransform), typeof(GridLayoutGroup));
        try
        {
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, 1000f);
            var grid = root.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.spacing = new Vector2(16f, 16f);
            var responsive = root.AddComponent<LobbyCollectionGrid>();
            responsive.RefreshLayout();
            Assert.That(grid.constraintCount, Is.EqualTo(4));
            Assert.That(grid.padding.left, Is.EqualTo(36));
            Assert.That(grid.padding.right, Is.EqualTo(36));
            Assert.That(grid.constraint, Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.cellSize.x * 4 + grid.spacing.x * 3 + grid.padding.horizontal,
                Is.EqualTo(width).Within(0.01f));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void HeaderRestylePreservesValuesSliderAndButtonsWithoutDuplicateFrames()
    {
        var canvas = new GameObject("HeaderTest", typeof(Canvas));
        try
        {
            var lobby = canvas.AddComponent<LobbyManager>();
            var bar = HeaderChild(canvas.transform, "tab_bar");
            var profile = HeaderChild(bar, "OldProfile");
            var level = HeaderChild(profile, "Experience_Icon");
            level.gameObject.AddComponent<Image>();
            lobby.text_UserLevel = HeaderText(level, "Level", "23");
            lobby.text_UserName = HeaderText(profile, "Name", "test-user");
            var sliderRoot = HeaderChild(profile, "Experience_Slider");
            var slider = sliderRoot.gameObject.AddComponent<Slider>();
            HeaderChild(sliderRoot, "Background").gameObject.AddComponent<Image>();
            var fillArea = HeaderChild(sliderRoot, "Fill Area");
            var fill = HeaderChild(fillArea, "Fill"); fill.gameObject.AddComponent<Image>();
            slider.fillRect = fill;
            var handleArea = HeaderChild(sliderRoot, "Handle Slide Area");
            var handle = HeaderChild(handleArea, "Handle"); handle.gameObject.AddComponent<Image>();
            slider.handleRect = handle;
            HeaderText(fillArea, "Experience Points", "75/500");
            slider.maxValue = 500f; slider.value = 75f;
            int sliderEvents = 0;
            slider.onValueChanged.AddListener(_ => sliderEvents++);
            int calls = 0;
            var values = new TMPro.TMP_Text[3];
            string[] names = { "Heart", "Coin", "Gem" };
            for (int i = 0; i < names.Length; i++)
            {
                var card = HeaderChild(bar, names[i] + "Card"); card.gameObject.AddComponent<Image>();
                HeaderChild(card, names[i]).gameObject.AddComponent<Image>();
                var button = HeaderChild(card, "Plus Button").gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => calls++);
                values[i] = HeaderText(card, "Value", "100,000");
            }
            lobby.text_Heart = values[0]; lobby.text_Gold = values[1]; lobby.text_Diamond = values[2];
            LobbyNeonUiBuilder.StyleHeader(lobby);
            int count = bar.GetComponentsInChildren<LobbyNeonGraphic>(true).Length;
            LobbyNeonUiBuilder.StyleHeader(lobby);
            Assert.That(bar.GetComponentsInChildren<LobbyNeonGraphic>(true).Length, Is.EqualTo(count));
            Assert.That(slider.value, Is.EqualTo(75f));
            Assert.That(slider.maxValue, Is.EqualTo(500f));
            Assert.That(slider.fillRect, Is.SameAs(fill));
            Assert.That(slider.handleRect, Is.SameAs(handle));
            Assert.That(sliderEvents, Is.Zero, "디자인 적용은 경험치 변경 이벤트를 발생시키지 않는다.");
            slider.value = 80f;
            Assert.That(sliderEvents, Is.EqualTo(1), "기존 경험치 리스너를 보존한다.");
            Assert.That(lobby.text_UserLevel.text, Is.EqualTo("23"));
            foreach (var text in values) Assert.That(text.text, Is.EqualTo("100,000"));
            foreach (var button in bar.GetComponentsInChildren<Button>()) button.onClick.Invoke();
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(values[0].transform.parent.parent, Is.EqualTo(bar));
            Assert.That(bar.localScale, Is.EqualTo(Vector3.one));
        }
        finally { Object.DestroyImmediate(canvas); }
    }

    private static RectTransform HeaderChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TMPro.TMP_Text HeaderText(Transform parent, string name, string value)
    {
        var text = HeaderChild(parent, name).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = value;
        return text;
    }

    [Test]
    public void UnownedCardKeepsIdentityAndCanOpenDetailWithoutDuplicateListener()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lobby/UnitCard.prefab");
        Assert.That(prefab, Is.Not.Null);
        GameObject cardObject = Object.Instantiate(prefab);
        try
        {
            var card = cardObject.GetComponent<UnitCardUI>();
            var data = new AlienInventoryDto { id = 29, name = "신화 1", grade = "MYTHIC", owned = false };
            int calls = 0;
            long selected = 0;
            System.Action<long> click = id => { calls++; selected = id; };
            card.SetData(data, click);
            card.SetData(data, click);
            Assert.That(card.text_Name.text, Is.EqualTo("신화 1"));
            Assert.That(card.text_Level.text, Is.EqualTo("미보유"));
            Assert.That(card.ownershipStatusText, Is.Not.Null);
            Assert.That(card.ownershipStatusText.text, Does.Contain("미보유"));
            Assert.That(card.neonFrame, Is.Not.Null);
            Assert.That(card.neonFrame.IsOwned, Is.False);
            cardObject.GetComponent<Button>().onClick.Invoke();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(selected, Is.EqualTo(29));
            data.owned = true;
            data.level = 1;
            card.SetData(data, click);
            Assert.That(card.text_Level.text, Is.EqualTo("Lv.1"));
            Assert.That(card.ownershipStatusText.text, Is.EqualTo("보유"));
            Assert.That(card.neonFrame.IsOwned, Is.True);
        }
        finally { Object.DestroyImmediate(cardObject); }
    }

    [Test]
    public void BreedingGeneratedSlotContainerKeepsBackgroundWithoutDisposableFrame()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingScreen.prefab");
        var panel = System.Array.Find(prefab.GetComponentsInChildren<RectTransform>(true),
            item => item.name == "SlotsPanel");
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.GetComponent<LayoutGroup>(), Is.Not.Null);
        Assert.That(panel.Find("LobbyNeonFrame"), Is.Null);
        Assert.That(panel.GetComponent<Image>().color.a, Is.GreaterThan(0.8f));
        Assert.That(prefab.transform.Find("LobbyNeonFrame"), Is.Not.Null);
    }
}
