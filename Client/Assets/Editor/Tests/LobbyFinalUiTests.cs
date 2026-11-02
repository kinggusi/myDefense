using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyFinalUiTests
{
    [TestCase(.975f, .866f)]
    [TestCase(.975f, 1f)]
    [TestCase(1.80f, .866f)]
    public void FinalFrameOuterStrokeKeepsMinimumScreenPixelWidth(float nominalWidth, float canvasScale)
    {
        float width = LobbyNeonGraphic.FinalOuterStrokeWidth(nominalWidth, canvasScale);
        Assert.That(width * canvasScale, Is.GreaterThanOrEqualTo(1.25f - .0001f));
        Assert.That(width, Is.GreaterThanOrEqualTo(nominalWidth));
        if (nominalWidth * canvasScale >= 1.25f)
            Assert.That(width, Is.EqualTo(nominalWidth));
    }

    [TestCase(.975f, .866f)]
    [TestCase(.975f, 1f)]
    [TestCase(1.80f, .866f)]
    [TestCase(.455f, .37f)]
    [TestCase(.65f, .25f)]
    [TestCase(4f, 1f)]
    public void FinalCardStrokeKeepsMinimumScreenPixelWidth(float nominalWidth, float canvasScale)
    {
        float width = LobbyNeonGraphic.FinalCardStrokeWidth(nominalWidth, canvasScale);
        Assert.That(width * canvasScale, Is.GreaterThanOrEqualTo(3f - .0001f));
        Assert.That(width, Is.GreaterThanOrEqualTo(nominalWidth));
        if (nominalWidth * canvasScale >= 3f)
            Assert.That(width, Is.EqualTo(nominalWidth));
    }

    [TestCase(LobbyNeonGraphic.FrameStyle.Panel)]
    [TestCase(LobbyNeonGraphic.FrameStyle.Button)]
    public void FinalNonCardFramesRetainTheirOriginalOuterAndInnerThickness(LobbyNeonGraphic.FrameStyle style)
    {
        WithCardMesh(360f, 94f, mesh =>
        {
            float unit = .65f;
            Vector3[] vertices = mesh.vertices;
            Assert.That(vertices.Length, Is.EqualTo(41));
            Assert.That(vertices[10].x - vertices[9].x, Is.EqualTo(1.25f).Within(.0001f));
            Assert.That(vertices[26].x - vertices[25].x, Is.EqualTo(.7f * unit).Within(.0001f));
        }, style);
    }

    [TestCase(180f, 255.6f)]
    [TestCase(220f, 312.4f)]
    [TestCase(244f, 346.48f)]
    public void FinalCardRingsAreClosedAndSymmetricWithOppositeEdges(float width, float height)
    {
        WithCardMesh(width, height, mesh =>
        {
            var vertices = mesh.vertices;
            var colors = mesh.colors32;
            var triangles = mesh.triangles;
            Assert.That(vertices.Length, Is.EqualTo(45)); // Fill + two 8-corner rings + divider.
            for (int ring = 0; ring < 2; ring++)
            {
                int start = 9 + ring * 16;
                for (int i = 0; i < 16; i++)
                {
                    Vector3 point = vertices[start + i];
                    Assert.That(Enumerable.Range(start, 16).Any(j =>
                        Vector3.Distance(vertices[j], new Vector3(-point.x, point.y, 0)) < .001f), Is.True, "Left/right mirror");
                    Assert.That(Enumerable.Range(start, 16).Any(j =>
                        Vector3.Distance(vertices[j], new Vector3(point.x, -point.y, 0)) < .001f), Is.True, "Top/bottom mirror");
                    Assert.That(colors[start + i], Is.EqualTo(colors[start]));
                }
                for (int edge = 0; edge < 8; edge++)
                {
                    int next = (edge + 1) % 8;
                    int offset = 24 + ring * 48 + edge * 6;
                    CollectionAssert.AreEqual(new[] { start + edge * 2, start + next * 2, start + edge * 2 + 1,
                        start + edge * 2 + 1, start + next * 2, start + next * 2 + 1 }, triangles.Skip(offset).Take(6));
                }
            }
            Assert.That(vertices[41].x, Is.EqualTo(-width * .41f).Within(.001f));
            Assert.That(vertices[43].x, Is.EqualTo(width * .41f).Within(.001f));
            Assert.That((vertices[41].y + vertices[42].y) * .5f, Is.EqualTo(-height * .31f).Within(.001f));
        });
    }

    [TestCase(.37f, 0f)]
    [TestCase(.37f, .13f)]
    [TestCase(.37f, .5f)]
    [TestCase(.37f, .83f)]
    [TestCase(.5f, .5f)]
    [TestCase(1f, .5f)]
    public void FinalCardEveryEdgeAndDividerCoverPixelCentersAtReducedPreviewScale(float scale, float phase)
    {
        // CPU mesh coverage, not a screenshot or a UI interaction test.
        WithCardMesh(220f, 312.4f, mesh =>
        {
            Vector2[] vertices = mesh.vertices.Select(v => new Vector2(v.x * scale + phase, v.y * scale + phase)).ToArray();
            for (int ring = 0; ring < 2; ring++)
            {
                int start = 9 + ring * 16;
                for (int edge = 0; edge < 8; edge++)
                {
                    int next = (edge + 1) % 8;
                    AssertPixelCoverage(vertices[start + edge * 2], vertices[start + next * 2],
                        vertices[start + next * 2 + 1], vertices[start + edge * 2 + 1], "Ring " + ring + " edge " + edge);
                }
            }
            AssertPixelCoverage(vertices[41], vertices[42], vertices[43], vertices[44], "Quantity divider");
        });
    }

    private static void WithCardMesh(float width, float height, System.Action<Mesh> verify,
        LobbyNeonGraphic.FrameStyle style = LobbyNeonGraphic.FrameStyle.Card)
    {
        var root = new GameObject("CardFrameMeshTest", typeof(RectTransform), typeof(LobbyNeonGraphic));
        var mesh = new Mesh();
        try
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(width, height);
            var graphic = root.GetComponent<LobbyNeonGraphic>();
            graphic.Configure(style);
            graphic.SetGrade("NORMAL");
            graphic.UseFinalStyle();
            using (var helper = new VertexHelper())
            {
                typeof(LobbyNeonGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                    .Invoke(graphic, new object[] { helper });
                helper.FillMesh(mesh);
            }
            verify(mesh);
        }
        finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(root); }
    }

    private static void AssertPixelCoverage(Vector2 a, Vector2 b, Vector2 c, Vector2 d, string label)
    {
        float minX = Mathf.Min(a.x, b.x, c.x, d.x), maxX = Mathf.Max(a.x, b.x, c.x, d.x);
        float minY = Mathf.Min(a.y, b.y, c.y, d.y), maxY = Mathf.Max(a.y, b.y, c.y, d.y);
        bool horizontal = maxX - minX >= maxY - minY;
        // Leave joins to the adjoining edge; check each pixel row/column along the edge body.
        int first = Mathf.CeilToInt((horizontal ? minX : minY) + 1f);
        int last = Mathf.FloorToInt((horizontal ? maxX : maxY) - 1f);
        for (int along = first; along < last; along++)
        {
            bool covered = false;
            int acrossFirst = Mathf.FloorToInt(horizontal ? minY : minX) - 1;
            int acrossLast = Mathf.CeilToInt(horizontal ? maxY : maxX) + 1;
            for (int across = acrossFirst; across <= acrossLast; across++)
            {
                var point = horizontal ? new Vector2(along + .5f, across + .5f) : new Vector2(across + .5f, along + .5f);
                if (InTriangle(point, a, b, c) || InTriangle(point, a, c, d)) { covered = true; break; }
            }
            Assert.That(covered, Is.True, label + " lost pixel coverage at " + along);
        }
    }

    private static bool InTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float ab = Cross(b - a, point - a), bc = Cross(c - b, point - b), ca = Cross(a - c, point - c);
        return (ab >= -.0001f && bc >= -.0001f && ca >= -.0001f) || (ab <= .0001f && bc <= .0001f && ca <= .0001f);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void BreedingReadyBadgeHasDedicatedSpaceAwayFromArrowAndText(int readyCount)
    {
        var parent = new GameObject("BreedingFinalTest", typeof(RectTransform));
        try
        {
            ((RectTransform)parent.transform).sizeDelta = new Vector2(1080, 1920);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingShortcut.prefab");
            var root = Object.Instantiate(prefab, parent.transform);
            var view = root.GetComponent<MythicBreedingShortcutView>();
            LobbyFinalPresentation.StyleBreeding(view);
            LobbyFinalPresentation.StyleBreeding(view);
            view.SetContent("신화 교배", "사용 가능 슬롯 1개", readyCount);
            Assert.That(view.badgeObject.activeSelf, Is.EqualTo(readyCount > 0));
            Assert.That(view.badgeText.text, Is.EqualTo(readyCount.ToString()));
            var badge = (RectTransform)view.badgeObject.transform;
            var arrow = (RectTransform)view.transform.Find("EntryArrow");
            Assert.That(badge.rect.width, Is.GreaterThan(0));
            Assert.That(badge.anchorMin.x, Is.GreaterThan(view.titleText.rectTransform.anchorMax.x));
            Assert.That(badge.anchorMax.x, Is.LessThan(arrow.anchorMin.x));
            Assert.That(badge.anchorMin.y, Is.GreaterThan(view.statusText.rectTransform.anchorMax.y));
            Assert.That(view.badgeObject.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(view.badgeText.raycastTarget, Is.False);
            Assert.That(arrow.GetComponent<Text>().raycastTarget, Is.False);
            Assert.That(root.GetComponentsInChildren<Text>(true).Count(t => t.name == "EntryArrow"), Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(parent); }
    }
    [TestCase(0f)]
    [TestCase(90f)]
    [TestCase(160f)]
    public void BottomSafeAreaMovesNavigationAndCollectionTogether(float inset)
    {
        var root=new GameObject("SafeAreaTest",typeof(LobbyFinalPresentation));
        try
        {
            var presenter=root.GetComponent<LobbyFinalPresentation>();
            presenter.navigation=LobbyFinalPresentation.Child(root.transform,"Navigation");
            presenter.collectionScroll=LobbyFinalPresentation.Child(root.transform,"CollectionScroll");
            presenter.ApplySafeBottom(inset);
            Assert.That(presenter.navigation.offsetMin.y,Is.EqualTo(inset));
            Assert.That(presenter.navigation.offsetMax.y,Is.EqualTo(inset+180));
            Assert.That(presenter.collectionScroll.offsetMin.y-presenter.navigation.offsetMax.y,Is.EqualTo(45f));
        }
        finally{Object.DestroyImmediate(root);}
    }
    [Test]
    public void CurrencyStylingDoesNotTouchProfileOrAncestorsAndKeepsCallbacks()
    {
        var canvas=new GameObject("FinalProfileTest",typeof(Canvas));
        try
        {
            var lobby=canvas.AddComponent<LobbyManager>();
            var bar=LobbyFinalPresentation.Child(canvas.transform,"tab_bar");bar.sizeDelta=new Vector2(1080,116);
            var profile=LobbyFinalPresentation.Child(bar,"LobbyProfileStatus");
            profile.anchoredPosition=new Vector2(13,27);profile.sizeDelta=new Vector2(301,94);profile.localScale=new Vector3(.91f,.93f,1);
            var name=LobbyFinalPresentation.Child(profile,"Name").gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            name.text="프로필 보존";name.color=Color.magenta;name.fontSize=37;lobby.text_UserName=name;
            string profileBefore=EditorJsonUtility.ToJson(profile);
            string textBefore=EditorJsonUtility.ToJson(name);
            Vector2 barPosition=bar.anchoredPosition,barSize=bar.sizeDelta;Vector3 scale=bar.localScale;
            int calls=0;var values=new TMPro.TMP_Text[3];string[] names={"Heart","Coin","Gem"};
            for(int i=0;i<3;i++)
            {
                var card=LobbyFinalPresentation.Child(bar,names[i]+"Card");card.gameObject.AddComponent<Image>();
                LobbyFinalPresentation.Child(card,names[i]).gameObject.AddComponent<Image>();
                var plus=LobbyFinalPresentation.Child(card,"Plus Button").gameObject.AddComponent<Button>();plus.onClick.AddListener(()=>calls++);
                values[i]=LobbyFinalPresentation.Child(card,"Value").gameObject.AddComponent<TMPro.TextMeshProUGUI>();values[i].text="1,234,567";
            }
            lobby.text_Heart=values[0];lobby.text_Gold=values[1];lobby.text_Diamond=values[2];
            LobbyFinalUiBuilder.StyleCurrencies(lobby);LobbyFinalUiBuilder.StyleCurrencies(lobby);
            Assert.That(EditorJsonUtility.ToJson(profile),Is.EqualTo(profileBefore));
            Assert.That(EditorJsonUtility.ToJson(name),Is.EqualTo(textBefore));
            Assert.That(bar.anchoredPosition,Is.EqualTo(barPosition));Assert.That(bar.sizeDelta,Is.EqualTo(barSize));Assert.That(bar.localScale,Is.EqualTo(scale));
            foreach(var value in values)
            {
                Assert.That(value.text,Is.EqualTo("1,234,567"));
                Assert.That(value.alignment,Is.EqualTo(TMPro.TextAlignmentOptions.MidlineRight));
                var plus=value.transform.parent.Find("Plus Button");plus.GetComponent<Button>().onClick.Invoke();
                Assert.That(((RectTransform)plus).anchorMin.x-value.rectTransform.anchorMax.x,Is.GreaterThan(.06f));
            }
            Assert.That(calls,Is.EqualTo(3));
        }
        finally {Object.DestroyImmediate(canvas);}
    }

    [Test]
    public void MaterialPanelHasTwoAlignedNamedRowsAndSingleVisibleFrame()
    {
        var canvas=new GameObject("MaterialFinalTest",typeof(Canvas));
        try
        {
            var unit=LobbyFinalPresentation.Child(canvas.transform,"view_Unit");unit.sizeDelta=new Vector2(1080,1920);
            var lobby=canvas.AddComponent<LobbyManager>();lobby.viewObjects=new[]{unit.gameObject,unit.gameObject};
            var ensure=typeof(LobbyManager).GetMethod("EnsureMaterialCurrencyUI",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            ensure.Invoke(lobby,null);ensure.Invoke(lobby,null);Canvas.ForceUpdateCanvases();
            var panel=unit.Find("LobbyMaterialCurrencies");
            Assert.That(panel.GetComponent<HorizontalLayoutGroup>().enabled,Is.False);
            Assert.That(panel.Find("LobbyNeonFrame").gameObject.activeSelf,Is.True);
            var rows=new[]{panel.Find("WakjeoDnaCard"),panel.Find("GrowthCellCard")};
            Assert.That(rows[0].Find("MaterialName").GetComponent<Text>().text,Is.EqualTo("왹져DNA"));
            Assert.That(rows[1].Find("MaterialName").GetComponent<Text>().text,Is.EqualTo("성장세포"));
            foreach(string child in new[]{"MaterialIcon","MaterialName","Value","PurchasePlus"})
            {
                var first=(RectTransform)rows[0].Find(child);var second=(RectTransform)rows[1].Find(child);
                Assert.That(first.anchorMin.x,Is.EqualTo(second.anchorMin.x));Assert.That(first.anchorMax.x,Is.EqualTo(second.anchorMax.x));
            }
            Assert.That(panel.GetComponentsInChildren<Button>(true).Length,Is.EqualTo(2));
            Assert.That(rows.All(row=>!row.Find("LobbyNeonFrame").gameObject.activeSelf),Is.True);
        }
        finally {Object.DestroyImmediate(canvas);}
    }

    [Test]
    public void NavigationSelectionFollowsActualTabAndDoesNotReplaceOpenTabEvents()
    {
        var root=new GameObject("NavigationFinalTest",typeof(LobbyManager),typeof(LobbyFinalPresentation));
        try
        {
            var lobby=root.GetComponent<LobbyManager>();var view=root.GetComponent<LobbyFinalPresentation>();
            lobby.viewObjects=new GameObject[5];view.indicators=new GameObject[5];view.symbols=new LobbyNavIconGraphic[4];int s=0;
            for(int i=0;i<5;i++)
            {
                lobby.viewObjects[i]=LobbyFinalPresentation.Child(root.transform,"View"+i).gameObject;
                view.indicators[i]=LobbyFinalPresentation.Child(root.transform,"Selected"+i).gameObject;
                if(i!=1){var icon=LobbyFinalPresentation.Child(root.transform,"Icon"+i).gameObject.AddComponent<LobbyNavIconGraphic>();icon.Configure(i);view.symbols[s++]=icon;}
            }
            for(int selected=0;selected<5;selected++)
            {
                lobby.OpenTab(selected);
                for(int i=0;i<5;i++)
                {Assert.That(lobby.viewObjects[i].activeSelf,Is.EqualTo(i==selected));Assert.That(view.indicators[i].activeSelf,Is.EqualTo(i==selected));}
                foreach(var icon in view.symbols)Assert.That(icon.color.g,Is.EqualTo(icon.TabIndex==selected?.88f:.51f).Within(.001f));
            }
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void FinalCardKeepsPortraitAndUsesRealCountsWithoutOwnedLabel()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lobby/UnitCard.prefab");
        var root=Object.Instantiate(prefab);
        var texture=new Texture2D(4,4);var sprite=Sprite.Create(texture,new Rect(0,0,4,4),new Vector2(.5f,.5f));
        try
        {
            var card=root.GetComponent<UnitCardUI>();card.portraitImage.sprite=sprite;
            LobbyFinalUiBuilder.StyleCard(root);
            card.SetData(new AlienInventoryDto{id=31,name="실제 유닛",grade="MYTHIC",owned=true,level=7,pieces=12,requiredPieces=30},null);
            Assert.That(card.portraitImage.sprite,Is.SameAs(sprite));Assert.That(card.portraitImage.enabled,Is.True);
            Assert.That(card.portraitFallbackText.gameObject.activeSelf,Is.False);
            Assert.That(card.text_Pieces.text,Is.EqualTo("12/30"));Assert.That(card.text_Name.color,Is.EqualTo(Color.white));
            Assert.That(card.text_Grade.color,Is.EqualTo(LobbyNeonGraphic.GradeBorderColor("MYTHIC")));
            Assert.That(card.ownershipStatusText.gameObject.activeSelf,Is.False);
        }
        finally{Object.DestroyImmediate(root);Object.DestroyImmediate(sprite);Object.DestroyImmediate(texture);}
    }
}
