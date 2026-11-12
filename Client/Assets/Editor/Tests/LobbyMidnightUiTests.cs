using System;
using System.Linq;
using System.Reflection;
using MyDefense.Shop;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public sealed class LobbyMidnightUiTests
{
    private const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    [TestCase(1.5f,.5f,1f/3f,1f)]
    [TestCase(.5f,1.5f,1f,1f/3f)]
    [TestCase(1f,1f,1f,1f)]
    public void ArtworkCoverUvKeepsAspectAndCropsCentrally(float source,float target,float width,float height)
    {
        var rect=MidnightArtworkCrop.CoverUv(source,target);
        Assert.That(rect.width,Is.EqualTo(width).Within(.001f));Assert.That(rect.height,Is.EqualTo(height).Within(.001f));
        Assert.That(rect.center,Is.EqualTo(new Vector2(.5f,.5f)));
    }
    [Test] public void MidnightPreferenceDoesNotStyleEditorOrModifyCurrentSession()
    {
        bool existed=PlayerPrefs.HasKey(MidnightLobbyTheme.PreferenceKey);int old=PlayerPrefs.GetInt(MidnightLobbyTheme.PreferenceKey);
        try
        {
            MidnightLobbyTheme.SelectForNextSession(true);Assert.IsFalse(MidnightLobbyTheme.Enabled);
            MidnightLobbyTheme.SelectForNextSession(false);Assert.AreEqual(0,PlayerPrefs.GetInt(MidnightLobbyTheme.PreferenceKey));
        }
        finally {if(existed)PlayerPrefs.SetInt(MidnightLobbyTheme.PreferenceKey,old);else PlayerPrefs.DeleteKey(MidnightLobbyTheme.PreferenceKey);PlayerPrefs.Save();}
    }
    [Test] public void ShellRestylesProfileButPreservesBoundTextAndAncestorTransforms()
    {
        var root=new GameObject("MidnightProfileTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            var canvas=root.GetComponent<Canvas>();var bar=ShopCatalogView.Rect("tab_bar",root.transform);
            bar.anchoredPosition=new Vector2(3,-19);bar.sizeDelta=new Vector2(111,112);bar.localScale=new Vector3(.9f,.8f,1);
            var profile=ShopCatalogView.Rect("LobbyProfileStatus",bar);profile.sizeDelta=new Vector2(310,98);
            var frame=profile.gameObject.AddComponent<LobbyNeonGraphic>();frame.UseFinalStyle();
            var name=new GameObject("Name",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));name.transform.SetParent(profile,false);
            var label=name.GetComponent<TMPro.TMP_Text>();label.text="Guest-23";label.fontSize=26;label.color=Color.yellow;
            var managerObject=new GameObject("Manager",typeof(LobbyManager));managerObject.transform.SetParent(root.transform);
            var lobby=managerObject.GetComponent<LobbyManager>();lobby.text_UserName=label;
            lobby.viewObjects=Enumerable.Range(0,5).Select(i=>ShopCatalogView.Rect("View"+i,root.transform).gameObject).ToArray();
            var skin=managerObject.AddComponent<MidnightLobbyPresentation>();
            typeof(MidnightLobbyPresentation).GetField("lobby",Private).SetValue(skin,lobby);
            typeof(MidnightLobbyPresentation).GetField("canvas",Private).SetValue(skin,canvas);
            var barBefore=new RectSnapshot(bar);var canvasBefore=new RectSnapshot((RectTransform)root.transform);
            typeof(MidnightLobbyPresentation).GetMethod("BuildShell",Private).Invoke(skin,null);
            barBefore.AssertUnchanged(bar,false);canvasBefore.AssertUnchanged((RectTransform)root.transform);
            Assert.AreSame(label,lobby.text_UserName);Assert.AreEqual("Guest-23",label.text);Assert.AreEqual(28,label.fontSizeMax);Assert.AreEqual(MidnightLobbyTheme.Ink,label.color);
            Assert.IsFalse(frame.enabled,"The old profile frame must be hidden, not layered under the compact profile.");
            Assert.NotNull(profile.Find("MidnightAvatar/MidnightAtlasIcon"));
            var main=lobby.viewObjects[2].transform.Find("MidnightMainBody");
            var progressPlate=main.Find("MidnightProgressPlate").GetComponent<Image>();
            Assert.IsFalse(progressPlate.raycastTarget);Assert.GreaterOrEqual(progressPlate.color.a,.85f);
            Assert.Less(Mathf.Max(progressPlate.color.r,progressPlate.color.g,progressPlate.color.b),.1f);
            Assert.AreEqual(MidnightLobbyTheme.Ink,progressPlate.GetComponentInChildren<Text>().color);
            var battle=main.Find("MidnightBattleUnavailable").GetComponent<Button>();Assert.IsFalse(battle.interactable);Assert.AreEqual(0,battle.onClick.GetPersistentEventCount());
            Assert.IsTrue(battle.GetComponent<LobbyNeonGraphic>().HasMidnightMainHex);Assert.AreEqual(105,battle.GetComponentInChildren<Text>().resizeTextMaxSize);
            Assert.AreEqual(120,lobby.viewObjects[2].transform.Find("MidnightTitle").GetComponent<Text>().resizeTextMaxSize);
            Assert.That(progressPlate.GetComponentInChildren<Text>().text,Does.Contain("준비 중"));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ShopMidnightCardsPreserveServerPriceCallbackAndUnavailableAds()
    {
        var root=new GameObject("MidnightShopTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var catalog=ShopCatalogFixture.Example();catalog.offers[1].price=2345;catalog.offers[2].regularPrice=700;catalog.adAvailable=false;
            ShopOffer selected=null;
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{catalog,(Action<ShopOffer>)(offer=>selected=offer)});
            var button=view.content.Find("GachaObservatory/Offer_GACHA_TEN").GetComponent<Button>();
            Assert.That(button.GetComponentInChildren<Text>().text,Does.Contain("2,345"));button.onClick.Invoke();Assert.AreSame(catalog.offers[1],selected);
            Assert.That(view.content.Find("GachaObservatory/RegularPrice_GACHA_TEN").GetComponent<Text>().text,Does.Contain("3,000"));
            Assert.That(view.content.Find("ProductRow_SUPPLY_0/Offer_GOLD/RegularPrice").GetComponent<Text>().text,Does.Contain("700"));
            Assert.IsFalse(view.rerollButton.interactable);
            Assert.AreEqual(catalog.offers.Length,view.content.GetComponentsInChildren<Button>().Count(b=>b!=view.rerollButton));
            Assert.AreEqual(890,view.content.Find("GachaObservatory").GetComponent<LayoutElement>().preferredHeight);
            Assert.AreEqual(48,button.GetComponentInChildren<Text>().resizeTextMaxSize);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase("DNA")][TestCase("CELL")][TestCase("GOLD")]
    public void ShopFocusSupportsNestedMidnightRows(string offerId)
    {
        var root=new GameObject("MidnightFocusTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{ShopCatalogFixture.Example(),(Action<ShopOffer>)(_=>{})});
            Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(view.content);
            view.FocusOffer(offerId);
            Assert.That(view.content.anchoredPosition.y,Is.GreaterThan(0));
            Assert.That(view.content.anchoredPosition.y,Is.LessThanOrEqualTo(Mathf.Max(0,view.content.rect.height-view.scroll.viewport.rect.height)+.01f));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ArtworkResourcesExistWithoutSpriteImporterRequirement()
    {
        foreach(var key in new[]{MidnightLobbyTheme.BackgroundResource,MidnightLobbyTheme.GachaResource,MidnightLobbyTheme.PlanetResource})
            Assert.NotNull(Resources.Load<Texture2D>(key),key);
    }
    [TestCase("observatory-background")]
    [TestCase("gacha-prism")]
    [TestCase("neptune-observatory")]
    [TestCase("status-material-icons-v3")]
    [TestCase("shop-item-icons-v4")]
    [TestCase("main-observatory-v4")]
    [TestCase("gacha-prism-cutout-v5")]
    public void ArtworkImporterPreservesRawPngDimensionsAndAspect(string name)
    {
        string path="Assets/Resources/Lobby/Midnight/"+name+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.NotNull(importer);Assert.AreEqual(TextureImporterNPOTScale.None,importer.npotScale);
        Assert.AreEqual(2048,importer.maxTextureSize);Assert.AreEqual(TextureWrapMode.Clamp,importer.wrapMode);
        Assert.IsFalse(importer.mipmapEnabled);Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);
        var original=new Texture2D(2,2);
        try
        {
            Assert.IsTrue(ImageConversion.LoadImage(original,System.IO.File.ReadAllBytes(System.IO.Path.Combine(Application.dataPath,"Resources/Lobby/Midnight/"+name+".png")),true));
            var imported=Resources.Load<Texture2D>("Lobby/Midnight/"+name);Assert.NotNull(imported);
            Assert.AreEqual(original.width,imported.width);Assert.AreEqual(original.height,imported.height);
            Assert.That((float)imported.width/imported.height,Is.EqualTo((float)original.width/original.height).Within(.00001f));
        }
        finally{UnityEngine.Object.DestroyImmediate(original);}
    }
    [TestCase("Assets/Art/Lobby/Final/SpaceshipBackground.png")]
    [TestCase("Assets/Resources/Lobby/Midnight/other.png")]
    [TestCase("Assets/Battle/map.png")]
    public void ImporterCannotChangeLegacyOrBattleArtwork(string path)
    {Assert.IsFalse(MidnightArtworkImporter.IsMidnightArtwork(path));}
    [TestCase(MyDefense.Clans.ClanSurfaceGraphic.Shape.Panel)]
    [TestCase(MyDefense.Clans.ClanSurfaceGraphic.Shape.Shield)]
    [TestCase(MyDefense.Clans.ClanSurfaceGraphic.Shape.Circle)]
    public void ClanResolvedSkinIsStableWithoutMutatingAuthoredFields(MyDefense.Clans.ClanSurfaceGraphic.Shape shape)
    {
        var root=new GameObject("ClanSkinTest",typeof(RectTransform),typeof(MyDefense.Clans.ClanSurfaceGraphic));
        try
        {
            var graphic=root.GetComponent<MyDefense.Clans.ClanSurfaceGraphic>();graphic.shape=shape;
            graphic.color=new Color(.20f,.30f,.40f,.8f);graphic.border=Color.yellow;graphic.corner=27;
            var original=graphic.color;var first=graphic.ResolveFill(true);root.SetActive(false);root.SetActive(true);
            Assert.AreEqual(first,graphic.ResolveFill(true));Assert.AreEqual(original,graphic.color);Assert.AreEqual(Color.yellow,graphic.border);Assert.AreEqual(27,graphic.corner);
            Assert.AreEqual(original,graphic.ResolveFill(false));
            if(shape!=MyDefense.Clans.ClanSurfaceGraphic.Shape.Panel){Assert.AreEqual(original,graphic.ResolveFill(true));Assert.AreEqual(Color.yellow,graphic.ResolveBorder(true));Assert.AreEqual(27,graphic.ResolveCorner(true));}
            else{Assert.AreEqual(MidnightLobbyTheme.Surface.r,graphic.ResolveFill(true).r);Assert.AreEqual(10,graphic.ResolveCorner(true));}
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ShopFooterAndScrollStayAboveNavigationAndSafeArea()
    {
        var root=new GameObject("MidnightSafeTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            typeof(ShopCatalogView).GetMethod("ApplyMidnightSafeBottom",Private).Invoke(view,new object[]{120f});
            Assert.That(((RectTransform)view.rerollButton.transform).offsetMin.y,Is.GreaterThan(180+120));
            Assert.That(((RectTransform)view.scroll.transform).offsetMin.y,Is.GreaterThan(((RectTransform)view.rerollButton.transform).offsetMax.y));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0f,0f)][TestCase(.5f,.25f)][TestCase(1f,.5f)]
    public void WideArtworkFocalPointKeepsFullHeightAndMovesCrop(float focal,float expectedX)
    {
        var uv=MidnightArtworkCrop.CoverUv(2,1,new Vector2(focal,.5f));
        Assert.That(uv.x,Is.EqualTo(expectedX).Within(.001f));Assert.AreEqual(.5f,uv.width);Assert.AreEqual(1,uv.height);
    }
    [Test] public void RibbonIsOnlyDrawnForGradedCardsAndButtonsKeepReducedSlantedCuts()
    {
        var root=new GameObject("RibbonTest",typeof(RectTransform),typeof(LobbyNeonGraphic));
        try
        {
            var frame=root.GetComponent<LobbyNeonGraphic>();frame.Configure(LobbyNeonGraphic.FrameStyle.Card);frame.UseMidnightStyle();Assert.IsFalse(frame.HasMidnightRibbon);
            frame.SetGrade("NORMAL");Assert.IsTrue(frame.HasMidnightRibbon);
            frame.Configure(LobbyNeonGraphic.FrameStyle.Panel);Assert.IsFalse(frame.HasMidnightRibbon);
            Assert.Greater(LobbyNeonGraphic.MidnightCorner(LobbyNeonGraphic.FrameStyle.Button,450,140),LobbyNeonGraphic.MidnightCorner(LobbyNeonGraphic.FrameStyle.Panel,450,140));
            Assert.Less(LobbyNeonGraphic.MidnightCorner(LobbyNeonGraphic.FrameStyle.Button,450,140),20);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void TypographyUpdatesBestFitCapAndCardQuantityPlateStaysBehindText()
    {
        var root=new GameObject("TypographyTest",typeof(RectTransform),typeof(UnitCardUI));
        try
        {
            var card=root.GetComponent<UnitCardUI>();card.text_Grade=ShopCatalogView.Label(root.transform,"Grade","NORMAL",19,0,0,1,1);
            card.text_Name=ShopCatalogView.Label(root.transform,"Name","NORMAL 1",23,0,0,1,1);
            card.text_Level=ShopCatalogView.Label(root.transform,"Level","Lv.1",20,0,0,1,1);
            card.text_Pieces=ShopCatalogView.Label(root.transform,"Pieces","0/5",20,0,0,1,1);
            MidnightLobbyTheme.StyleCard(card);MidnightLobbyTheme.StyleCard(card);
            Assert.AreEqual(28,card.text_Name.resizeTextMaxSize);Assert.AreEqual(25,card.text_Pieces.resizeTextMaxSize);
            Assert.Greater(card.text_Pieces.transform.GetSiblingIndex(),root.transform.Find("MidnightQuantityPlate").GetSiblingIndex());
            Assert.AreEqual(Color.white,card.text_Grade.color);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void SupplyShelfUsesActualDailyProductsAndPreservesEveryCategoryCallbackExactlyOnce()
    {
        var root=new GameObject("MidnightRerenderTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var method=typeof(ShopCatalogView).GetMethod("RenderMidnight",Private);var catalog=ShopCatalogFixture.Example();int clicks=0;
            var categories=catalog.offers.ToDictionary(o=>o.offerId,o=>o.category);var purchased=new System.Collections.Generic.List<ShopOffer>();
            view.rerollButton.onClick.AddListener(()=>clicks++);
            for(int i=0;i<3;i++)method.Invoke(view,new object[]{catalog,(Action<ShopOffer>)(o=>purchased.Add(o))});
            view.rerollButton.onClick.Invoke();Assert.AreEqual(1,clicks);
            var row=view.content.Find("ProductRow_SUPPLY_0");Assert.AreEqual(4,row.childCount);
            var first=(RectTransform)row.GetChild(0);var last=(RectTransform)row.GetChild(row.childCount-1);
            Assert.That(first.anchorMax.x-first.anchorMin.x,Is.EqualTo(.25f-.014f).Within(.001f));Assert.That(last.anchorMax.x,Is.GreaterThan(.98f));
            Assert.AreEqual(catalog.offers.Length,view.content.GetComponentsInChildren<Button>().Count(b=>b!=view.rerollButton));
            Assert.AreEqual("Section_SUPPLY",view.rerollButton.transform.parent.name);
            Assert.That(view.content.Find("Section_SUPPLY/RerollScope").GetComponent<Text>().text,Does.Contain("일일·럭키"));
            Assert.AreEqual(4,view.content.GetComponentsInChildren<Text>().Count(t=>t.name=="DailyBadge"));
            Assert.NotNull(view.content.Find("Section_LUCKY"));Assert.IsNull(view.content.Find("Section_DAILY"));
            foreach(var buy in view.content.GetComponentsInChildren<Button>().Where(b=>b!=view.rerollButton))buy.onClick.Invoke();
            Assert.AreEqual(catalog.offers.Length,purchased.Count);Assert.AreEqual(catalog.offers.Length,purchased.Distinct().Count());
            foreach(var offer in catalog.offers){Assert.IsTrue(purchased.Any(o=>ReferenceEquals(o,offer)));Assert.AreEqual(categories[offer.offerId],offer.category);}
            Assert.IsTrue(view.content.GetComponentsInChildren<LobbyNeonGraphic>().Where(g=>g.name.StartsWith("Offer_")&&g.Style==LobbyNeonGraphic.FrameStyle.Panel).All(g=>!g.HasMidnightRibbon));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void DirectControllerErrorTextReappearsAfterNormalStatusWasHidden()
    {
        var root=new GameObject("MidnightStatusTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var refresh=typeof(ShopCatalogView).GetMethod("RefreshMidnightStatus",Private);
            view.statusText.text="";refresh.Invoke(view,null);Assert.IsFalse(view.statusText.gameObject.activeSelf);
            view.statusText.text="상점 조회 실패 · 다시 조회해 주세요.";refresh.Invoke(view,null);Assert.IsTrue(view.statusText.gameObject.activeSelf);Assert.AreEqual(-450,((RectTransform)view.scroll.transform).offsetMax.y);
            view.statusText.text="이전 요청 처리 확인 필요";refresh.Invoke(view,null);Assert.IsTrue(view.statusText.gameObject.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void MainAccountAndQuestHaveSeparateColumnsAndContainedChildren()
    {
        var root=new GameObject("MainShortcutTest",typeof(RectTransform));
        try
        {
            var account=ShopCatalogView.Rect("AccountLinkShortcut",root.transform);account.gameObject.AddComponent<LobbyNeonGraphic>();ShopCatalogView.Label(account,"Label","계정 연동",30,0,0,1,1);
            var quest=ShopCatalogView.Rect("QuestShortcut",root.transform).gameObject.AddComponent<QuestShortcutView>();
            ShopCatalogView.Label(quest.transform,"Title","퀘스트",30,0,0,1,1);quest.statusText=ShopCatalogView.Label(quest.transform,"Status","오늘의 임무 확인",20,0,0,1,1);
            MidnightLobbyPresentation.PositionAccountLink(account);MidnightLobbyPresentation.PositionQuest(quest);
            var qr=(RectTransform)quest.transform;Assert.Less(account.anchorMax.x,qr.anchorMin.x);Assert.AreEqual(account.offsetMin.y,qr.offsetMin.y);Assert.AreEqual(account.offsetMax.y,qr.offsetMax.y);
            Assert.IsFalse(quest.statusText.gameObject.activeSelf);Assert.IsTrue(account.GetComponent<LobbyNeonGraphic>().IsMidnightStyle);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ShopFixtureUsesServerCanonicalCategoryNames()
    {
        string[] allowed={"GACHA","RESOURCE","DAILY","LUCKY"};
        Assert.IsTrue(ShopCatalogFixture.Example().offers.All(offer=>allowed.Contains(offer.category)));
        Assert.AreEqual(3,ShopCatalogFixture.Example().offers.Count(offer=>offer.category=="RESOURCE"));
    }
    [Test] public void CurrencyPlusGlyphGeneratesVisibleGeometryInsideSmallSquare()
    {
        var root=new GameObject("PlusGlyphTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var rect=ShopCatalogView.Rect("PlusButton",root.transform);rect.sizeDelta=new Vector2(26,26);
            var label=ShopCatalogView.Label(rect,"NeonPlusLabel","+",25,0,0,1,1);
            label.resizeTextForBestFit=false;label.verticalOverflow=VerticalWrapMode.Truncate;
            MidnightLobbyPresentation.StyleCurrencyPlus(rect);
            Assert.AreEqual(VerticalWrapMode.Overflow,label.verticalOverflow);Assert.IsTrue(label.resizeTextForBestFit);Assert.AreEqual(22,label.resizeTextMaxSize);
            Canvas.ForceUpdateCanvases();
            Assert.NotNull(label.canvas);Assert.IsTrue(label.font.HasCharacter('+'));
            label.font.RequestCharactersInTexture("+",label.fontSize,label.fontStyle);
            using(var generator=new TextGenerator())
            {
                Assert.IsTrue(generator.Populate(label.text,label.GetGenerationSettings(new Vector2(26,26))));
                // Unity versions differ in whether a terminal quad is included. Inspect actual
                // glyph geometry, UVs and opacity instead of inferring visibility from count.
                var vertices=generator.verts;int visibleQuads=0;
                for(int i=0;i+3<vertices.Count;i+=4)
                {
                    var quad=vertices.Skip(i).Take(4).ToArray();
                    float minX=quad.Min(v=>v.position.x),maxX=quad.Max(v=>v.position.x);
                    float minY=quad.Min(v=>v.position.y),maxY=quad.Max(v=>v.position.y);
                    float uvWidth=quad.Max(v=>v.uv0.x)-quad.Min(v=>v.uv0.x);
                    float uvHeight=quad.Max(v=>v.uv0.y)-quad.Min(v=>v.uv0.y);
                    if(maxX-minX<=.1f||maxY-minY<=.1f||uvWidth<=0||uvHeight<=0||quad.Any(v=>v.color.a==0))continue;
                    Assert.That(minX,Is.GreaterThanOrEqualTo(-13f));Assert.That(maxX,Is.LessThanOrEqualTo(13f));
                    Assert.That(minY,Is.GreaterThanOrEqualTo(-13f));Assert.That(maxY,Is.LessThanOrEqualTo(13f));
                    visibleQuads++;
                }
                Assert.AreEqual(1,visibleQuads,"Exactly one opaque, textured, non-degenerate plus glyph must fit inside the 26px button.");
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0,0,512)][TestCase(1,512,512)][TestCase(2,1024,512)]
    [TestCase(3,0,0)][TestCase(4,512,0)][TestCase(5,1024,0)]
    public void IconAtlasUsesExactTileAndCachedSprite(int index,int x,int y)
    {
        var icon=(MidnightIconAtlas.Icon)index;
        Assert.AreEqual(new Rect(x,y,512,512),MidnightIconAtlas.TileRect(icon,1536,1024));
        var sprite=MidnightIconAtlas.Get(icon);Assert.NotNull(sprite);Assert.AreSame(sprite,MidnightIconAtlas.Get(icon));
        Assert.AreEqual(new Rect(x,y,512,512),sprite.rect);Assert.AreEqual(1536,sprite.texture.width);Assert.AreEqual(1024,sprite.texture.height);
    }
    [Test] public void AtlasRepeatedPlacementUsesOneNonBlockingImage()
    {
        var root=new GameObject("AtlasPlacement",typeof(RectTransform));
        try
        {
            var first=MidnightIconAtlas.Place(root.transform,MidnightIconAtlas.Icon.Dna);
            var second=MidnightIconAtlas.Place(root.transform,MidnightIconAtlas.Icon.Dna);
            Assert.AreSame(first,second);Assert.AreEqual(1,root.transform.childCount);Assert.IsFalse(first.raycastTarget);Assert.IsTrue(first.preserveAspect);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(1440)][TestCase(1920)][TestCase(2340)]
    public void CompactHeaderPreservesDataCallbacksAndFitsNineDigitWallet(int height)
    {
        var root=new GameObject("CompactHeader",typeof(RectTransform),typeof(Canvas));
        try
        {
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(1080,height);
            var bar=ShopCatalogView.Rect("tab_bar",root.transform);bar.sizeDelta=new Vector2(1080,116);
            var profile=ShopCatalogView.Rect("LobbyProfileStatus",bar);profile.gameObject.AddComponent<LobbyNeonGraphic>();
            var owner=new GameObject("Manager",typeof(LobbyManager));owner.transform.SetParent(root.transform);var lobby=owner.GetComponent<LobbyManager>();
            lobby.text_UserName=Tmp(profile,"Name","Guest-999999");lobby.text_UserLevel=Tmp(profile,"Level","123");
            var xp=ShopCatalogView.Rect("Experience_Slider",profile).gameObject.AddComponent<Slider>();xp.value=.15f;
            int xpEvents=0;xp.onValueChanged.AddListener(_=>xpEvents++);
            string[] names={"Heart","Coin","Gem"};var texts=new TMPro.TMP_Text[3];var buttons=new Button[3];int clicks=0;
            for(int i=0;i<3;i++)
            {
                var panel=ShopCatalogView.Rect("Wallet"+i,bar);panel.gameObject.AddComponent<Image>();
                ShopCatalogView.Rect(names[i],panel).gameObject.AddComponent<Image>();texts[i]=Tmp(panel,"Amount","999,999,999");
                var plus=ShopCatalogView.Rect("Plus Button",panel);buttons[i]=plus.gameObject.AddComponent<Button>();buttons[i].onClick.AddListener(()=>clicks++);
                ShopCatalogView.Label(plus,"NeonPlusLabel","+",22,0,0,1,1);
            }
            lobby.text_Heart=texts[0];lobby.text_Gold=texts[1];lobby.text_Diamond=texts[2];
            var before=new RectSnapshot(bar);MidnightHeaderPresentation.Apply(lobby);MidnightHeaderPresentation.Apply(lobby);before.AssertUnchanged(bar);
            Assert.AreEqual("Guest-999999",lobby.text_UserName.text);Assert.AreEqual("123",lobby.text_UserLevel.text);
            Assert.AreEqual(.15f,xp.value);Assert.AreEqual(0,xpEvents);Assert.IsFalse(xp.gameObject.activeSelf);
            Assert.AreEqual(1,profile.GetComponentsInChildren<Image>().Count(i=>i.name=="MidnightAtlasIcon"));
            Canvas.ForceUpdateCanvases();
            for(int i=0;i<3;i++)
            {
                Assert.AreEqual("999,999,999",texts[i].text);texts[i].ForceMeshUpdate(true,true);
                Assert.IsFalse(texts[i].isTextOverflowing,"Nine-digit wallet must fit without ellipsis.");
                Assert.Greater(texts[i].rectTransform.rect.width,90);Assert.AreSame(texts[i],i==0?lobby.text_Heart:i==1?lobby.text_Gold:lobby.text_Diamond);
                var hit=(RectTransform)buttons[i].transform;Assert.AreEqual(new Vector2(44,88),hit.sizeDelta);
                Assert.AreEqual(new Vector2(44,44),((RectTransform)buttons[i].targetGraphic.transform).sizeDelta);
                Assert.IsTrue(hit.GetComponent<Image>().raycastTarget);buttons[i].onClick.Invoke();
            }
            Assert.AreEqual(3,clicks);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void SatinSurfaceContainsRoundedNonDegenerateGeometryAndSubtleHighlights()
    {
        var bounds=new Rect(0,0,240,160);Assert.AreEqual(24,MidnightSurfaceMesh.Outline(bounds,10,false).Length);
        using(var helper=new VertexHelper())
        {
            MidnightSurfaceMesh.Draw(helper,bounds,new Color(.10f,.135f,.205f,.97f),new Color(.57f,.66f,.81f,.42f),Color.white,10,1);
            var mesh=new Mesh();try
            {
                helper.FillMesh(mesh);Assert.Greater(mesh.vertexCount,100);var vertices=mesh.vertices;var triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                    Assert.Greater(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude,.000001f);
                Assert.Greater(mesh.colors32.Select(c=>c.a).Distinct().Count(),3);
                Assert.IsTrue(mesh.colors32.Any(c=>c.a>200&&c.b<100&&c.r<70),"Main fill must remain dark navy, not whitewashed.");
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
    [TestCase(0f)][TestCase(.5f)]
    public void ClanMidnightOpacityScalesEveryLayerExactlyOnce(float opacity)
    {
        var root=new GameObject("ClanOpacity",typeof(RectTransform),typeof(MyDefense.Clans.ClanSurfaceGraphic));
        try
        {
            var graphic=root.GetComponent<MyDefense.Clans.ClanSurfaceGraphic>();graphic.shape=MyDefense.Clans.ClanSurfaceGraphic.Shape.Panel;
            var draw=typeof(MyDefense.Clans.ClanSurfaceGraphic).GetMethod("DrawMidnightPanel",Private);
            Color32[][] colors=new Color32[2][];
            for(int pass=0;pass<2;pass++)using(var helper=new VertexHelper())
            {
                graphic.color=new Color(.2f,.3f,.4f,pass==0?1f:opacity);
                draw.Invoke(graphic,new object[]{helper,new Rect(0,0,240,160)});
                var mesh=new Mesh();try{helper.FillMesh(mesh);colors[pass]=mesh.colors32;}finally{UnityEngine.Object.DestroyImmediate(mesh);}
            }
            Assert.AreEqual(colors[0].Length,colors[1].Length);Assert.Greater(colors[0].Length,100);
            for(int i=0;i<colors[0].Length;i++)
                Assert.That((float)colors[1][i].a,Is.EqualTo(colors[0][i].a*opacity).Within(1f),"All fill, rim, shadow and inner-highlight vertices must receive opacity once.");
            Assert.That((float)colors[1].Max(c=>c.a),Is.EqualTo(255f*opacity).Within(1f),"Fill alpha must not be squared.");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(true,true)][TestCase(false,true)][TestCase(false,false)]
    public void ButtonGlossAddsClippedSpecularAndDepthWithoutChangingSatinBase(bool primary,bool slanted)
    {
        var rect=new Rect(0,0,450,140);var surface=primary?MidnightLobbyTheme.Accent:MidnightLobbyTheme.Surface;
        var edge=new Color(.6f,.75f,.9f,.5f);
        using(var satin=new VertexHelper())using(var glossy=new VertexHelper())
        {
            MidnightSurfaceMesh.Draw(satin,rect,surface,edge,Color.white,12,.36f,slanted);
            MidnightSurfaceMesh.DrawButton(glossy,rect,surface,edge,Color.white,12,.36f,primary,slanted);
            var a=new Mesh();var b=new Mesh();try
            {
                satin.FillMesh(a);glossy.FillMesh(b);Assert.Greater(b.vertexCount,a.vertexCount);
                for(int i=0;i<a.vertexCount;i++){Assert.AreEqual(a.vertices[i],b.vertices[i]);Assert.AreEqual(a.colors32[i],b.colors32[i]);}
                var outline=MidnightSurfaceMesh.Outline(new Rect(1f/.36f,1f/.36f,450-2f/.36f,140-2f/.36f),12,slanted);
                var vertices=b.vertices;var colors=b.colors32;bool specular=false,depth=false;
                for(int i=a.vertexCount;i<b.vertexCount;i++)
                {
                    var p=(Vector2)vertices[i];
                    for(int j=0;j<outline.Length;j++)
                    {
                        var d=outline[(j+1)%outline.Length]-outline[j];var q=p-outline[j];
                        Assert.LessOrEqual(d.x*q.y-d.y*q.x,.01f,"Gloss must be clipped inside the same visual outline.");
                    }
                    if(p.y>125&&colors[i].r>200&&colors[i].a>140)specular=true;
                    if(p.y<14&&colors[i].r<10&&colors[i].a>100)depth=true;
                    Assert.IsTrue(p.y>=95||p.y<=20,"Gloss must not wash over the central label area.");
                }
                Assert.IsTrue(specular,"A clear upper specular strip is required, not only a subtle fill change.");Assert.IsTrue(depth);
                var triangles=b.triangles;
                for(int i=0;i<triangles.Length;i+=3)Assert.Greater(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude,.000001f);
            }
            finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
        }
    }
    [TestCase(0f)][TestCase(.5f)]
    public void ButtonGlossOpacityIncludesSpecularBevelAndSheen(float opacity)
    {
        Color32[][] colors=new Color32[2][];
        for(int pass=0;pass<2;pass++)using(var helper=new VertexHelper())
        {
            MidnightSurfaceMesh.DrawButton(helper,new Rect(0,0,450,140),MidnightLobbyTheme.Accent,Color.white,new Color(1,1,1,pass==0?1:opacity),12,1,true,true);
            var mesh=new Mesh();try{helper.FillMesh(mesh);colors[pass]=mesh.colors32;}finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        Assert.AreEqual(colors[0].Length,colors[1].Length);
        for(int i=0;i<colors[0].Length;i++)Assert.That((float)colors[1][i].a,Is.EqualTo(colors[0][i].a*opacity).Within(1f));
    }
    [Test] public void GlossOptInPreservesButtonStateRectAndCallbacksAndExcludesCardsAndBadges()
    {
        var root=new GameObject("ButtonGloss",typeof(RectTransform),typeof(LobbyNeonGraphic));
        try
        {
            var frame=root.GetComponent<LobbyNeonGraphic>();frame.Configure(LobbyNeonGraphic.FrameStyle.Button);
            frame.UseMidnightStyle();frame.UseMidnightButtonGloss();Assert.IsFalse(frame.HasMidnightButtonGloss,"Decorative badges have no Button owner.");
            var button=root.AddComponent<Button>();button.targetGraphic=frame;button.interactable=false;int clicks=0;button.onClick.AddListener(()=>clicks++);
            var before=new RectSnapshot((RectTransform)root.transform);
            frame.UseMidnightButtonGloss();frame.UseMidnightButtonGloss();before.AssertUnchanged((RectTransform)root.transform);
            Assert.IsTrue(frame.HasMidnightButtonGloss);Assert.IsFalse(button.interactable);Assert.AreSame(frame,button.targetGraphic);
            button.onClick.Invoke();Assert.AreEqual(1,clicks);
            frame.Configure(LobbyNeonGraphic.FrameStyle.Card);frame.UseMidnightStyle();frame.UseMidnightButtonGloss();Assert.IsFalse(frame.HasMidnightButtonGloss);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void PreservedStatusPlusMeshMatchesPreviousFinishVertexForVertex()
    {
        var root=new GameObject("StatusFinish",typeof(RectTransform),typeof(Canvas));
        try
        {
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            var bar=ShopCatalogView.Rect("tab_bar",root.transform);
            var button=ShopCatalogView.Rect("Plus Button",bar);button.gameObject.AddComponent<Button>();
            var rect=ShopCatalogView.Rect("LobbyNeonFrame",button);rect.sizeDelta=new Vector2(44,44);
            var frame=rect.gameObject.AddComponent<LobbyNeonGraphic>();frame.Configure(LobbyNeonGraphic.FrameStyle.Panel);frame.UseMidnightButtonGloss();
            Assert.IsTrue(frame.UsesPreservedStatusFinish);
            using(var actual=new VertexHelper())using(var expected=new VertexHelper())
            {
                typeof(LobbyNeonGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(frame,new object[]{actual});
                MidnightSurfaceMesh.DrawButton(expected,frame.GetPixelAdjustedRect(),new Color(.10f,.135f,.205f,.97f),new Color(.57f,.66f,.81f,.42f),frame.color,LobbyNeonGraphic.MidnightCorner(LobbyNeonGraphic.FrameStyle.Panel,44,44),canvas.scaleFactor,false,false);
                var a=new Mesh();var b=new Mesh();try
                {actual.FillMesh(a);expected.FillMesh(b);CollectionAssert.AreEqual(b.vertices,a.vertices);CollectionAssert.AreEqual(b.colors32,a.colors32);CollectionAssert.AreEqual(b.triangles,a.triangles);}
                finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(true)][TestCase(false)]
    public void ReferenceButtonsUseSoftBlueGradientWithoutOpaqueSilverCap(bool primary)
    {
        using(var helper=new VertexHelper())
        {
            MidnightSurfaceMesh.DrawReferenceButton(helper,new Rect(0,0,450,140),Color.white,12,1,primary,true);
            var mesh=new Mesh();try
            {
                helper.FillMesh(mesh);Assert.Greater(mesh.vertexCount,20);
                Assert.IsFalse(mesh.colors32.Any(c=>c.r>200&&c.g>220&&c.a>140),"No broad silver-white cap should survive outside the preserved status bar.");
                var colors=mesh.colors32;var vertices=mesh.vertices;
                var top=Enumerable.Range(0,7).Where(i=>vertices[i].y>120).Select(i=>(float)colors[i].b).Average();
                var bottom=Enumerable.Range(0,7).Where(i=>vertices[i].y<20).Select(i=>(float)colors[i].b).Average();
                Assert.Greater(top,bottom+20,"The surface must retain visible blue gradient depth.");
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
    [Test] public void ReferenceButtonPreservesBreedingPurpleAccent()
    {
        var root=new GameObject("BreedingFinish",typeof(RectTransform),typeof(LobbyNeonGraphic),typeof(Button));
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(300,150);
            var frame=root.GetComponent<LobbyNeonGraphic>();frame.Configure(LobbyNeonGraphic.FrameStyle.Button);frame.UseMidnightAccent(new Color(.23f,.14f,.30f,.98f));
            Assert.IsFalse(frame.UsesPreservedStatusFinish);
            using(var helper=new VertexHelper())
            {
                typeof(LobbyNeonGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(frame,new object[]{helper});
                var mesh=new Mesh();try{helper.FillMesh(mesh);var center=mesh.colors32[0];Assert.Greater(center.r,center.g+15);Assert.Greater(center.b,center.r+10);}
                finally{UnityEngine.Object.DestroyImmediate(mesh);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0,0,627)][TestCase(1,627,627)][TestCase(2,0,0)][TestCase(3,627,0)]
    public void ShopAtlasCachesFourExactTilesWithoutChangingStatusAtlas(int index,int x,int y)
    {
        var status=MidnightIconAtlas.Get(MidnightIconAtlas.Icon.Coin);var icon=(MidnightShopIconAtlas.Icon)index;
        Assert.AreEqual(new Rect(x,y,627,627),MidnightShopIconAtlas.TileRect(icon,1254,1254));
        var sprite=MidnightShopIconAtlas.Get(icon);Assert.NotNull(sprite);Assert.AreSame(sprite,MidnightShopIconAtlas.Get(icon));
        Assert.AreEqual(new Rect(x,y,627,627),sprite.rect);Assert.AreEqual(1254,sprite.texture.width);
        Assert.AreSame(status,MidnightIconAtlas.Get(MidnightIconAtlas.Icon.Coin));Assert.AreNotSame(status.texture,sprite.texture);
    }
    [TestCase(1440)][TestCase(1920)][TestCase(2168)][TestCase(2340)]
    public void MainCtaHeightIsCappedAndUtilityButtonsRemainSeparated(int height)
    {
        var body=new GameObject("MainSizing",typeof(RectTransform));
        try
        {
            var rect=(RectTransform)body.transform;rect.sizeDelta=new Vector2(1080,height-595);
            string[] names={"MidnightBattleUnavailable","MidnightMode","MidnightBattleNotice","MidnightDailyUnavailable","TutorialEntry"};
            foreach(var name in names)ShopCatalogView.Rect(name,rect);
            MidnightLobbyPresentation.LayoutMainControls(rect);
            var battle=(RectTransform)rect.Find(names[0]);var mode=(RectTransform)rect.Find(names[1]);var notice=(RectTransform)rect.Find(names[2]);var daily=(RectTransform)rect.Find(names[3]);
            Assert.That(battle.rect.height,Is.GreaterThan(100).And.LessThanOrEqualTo(200));
            Assert.Greater(mode.offsetMin.y,battle.offsetMax.y);Assert.Greater(notice.offsetMin.y,daily.offsetMax.y);
            Assert.LessOrEqual(notice.offsetMax.y,battle.offsetMin.y);
        }
        finally{UnityEngine.Object.DestroyImmediate(body);}
    }
    [Test] public void SupplyShelfNeverInventsAnOfferToFillMissingFourthSlot()
    {
        var root=new GameObject("ActualSupplyOnly",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var catalog=ShopCatalogFixture.Example();catalog.offers=catalog.offers.Where(o=>o.category=="RESOURCE").ToArray();
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{catalog,(Action<ShopOffer>)(_=>{})});
            Assert.AreEqual(3,view.content.Find("ProductRow_SUPPLY_0").childCount);
            Assert.AreEqual(3,view.content.GetComponentsInChildren<Button>().Count(b=>b!=view.rerollButton));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0f)][TestCase(.5f)]
    public void ReferenceButtonOpacityMultipliesEveryLayerOnce(float opacity)
    {
        Color32[][] colors=new Color32[2][];
        for(int pass=0;pass<2;pass++)using(var helper=new VertexHelper())
        {
            MidnightSurfaceMesh.DrawReferenceButton(helper,new Rect(0,0,450,140),new Color(1,1,1,pass==0?1:opacity),12,1,true,true);
            var mesh=new Mesh();try{helper.FillMesh(mesh);colors[pass]=mesh.colors32;}finally{UnityEngine.Object.DestroyImmediate(mesh);}
        }
        Assert.AreEqual(colors[0].Length,colors[1].Length);
        for(int i=0;i<colors[0].Length;i++)Assert.That((float)colors[1][i].a,Is.EqualTo(colors[0][i].a*opacity).Within(1f));
    }
    [Test] public void CollectionRestyleKeepsRegisteredPortraitAndExactQuantityText()
    {
        var root=new GameObject("PortraitPreservation",typeof(RectTransform),typeof(UnitCardUI));
        var texture=new Texture2D(16,32);var sprite=Sprite.Create(texture,new Rect(0,0,16,32),new Vector2(.5f,.5f));
        try
        {
            var card=root.GetComponent<UnitCardUI>();var well=ShopCatalogView.Rect("PortraitWell",root.transform);
            card.portraitImage=well.gameObject.AddComponent<Image>();card.portraitImage.sprite=sprite;
            card.text_Pieces=ShopCatalogView.Label(root.transform,"Count","127/5",26,0,0,1,1);
            MidnightLobbyTheme.StyleCard(card);MidnightLobbyTheme.StyleCard(card);
            Assert.AreSame(sprite,card.portraitImage.sprite);Assert.AreEqual("127/5",card.text_Pieces.text);
            Assert.IsTrue(card.portraitImage.preserveAspect);Assert.IsFalse(card.portraitImage.raycastTarget);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test] public void ShopAtlasRepeatedPlacementReusesOneNonBlockingImage()
    {
        var root=new GameObject("ShopAtlasPlacement",typeof(RectTransform));
        try
        {
            var first=MidnightShopIconAtlas.Place(root.transform,MidnightShopIconAtlas.Icon.Coin);
            Assert.AreSame(first,MidnightShopIconAtlas.Place(root.transform,MidnightShopIconAtlas.Icon.Coin));
            Assert.AreEqual(1,root.transform.childCount);Assert.IsFalse(first.raycastTarget);Assert.IsTrue(first.preserveAspect);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase("왹져 뽑기")][TestCase("상점")][TestCase("내 유닛")][TestCase("메인")][TestCase("전투 시작")]
    public void DisplayFontContainsRealKoreanGlyphsAndAvoidsSyntheticBold(string value)
    {
        var font=Resources.Load<Font>(MidnightLobbyTheme.DisplayFontResource);Assert.NotNull(font);
        var result=UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(System.IO.Path.Combine(Application.dataPath,"Resources/Lobby/Midnight/Fonts/BlackHanSans-Regular.ttf"));
        Assert.AreEqual(UnityEngine.TextCore.LowLevel.FontEngineError.Success,result);
        foreach(char ch in value.Where(c=>c!=' '))
        {Assert.IsTrue(UnityEngine.TextCore.LowLevel.FontEngine.TryGetGlyphIndex(ch,out uint glyph),ch.ToString());Assert.Greater(glyph,0,ch.ToString());}
        var root=new GameObject("DisplayType",typeof(RectTransform));
        try
        {
            var label=ShopCatalogView.Label(root.transform,"Title",value,82,0,0,1,1);
            MidnightLobbyTheme.DisplayTypography(label,82);
            Assert.AreSame(font,label.font);Assert.AreEqual(FontStyle.Italic,label.fontStyle);Assert.AreEqual(82,label.resizeTextMaxSize);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
    public void ReferenceNavigationAllFiveHaveCenteredGeometryAndSelectionDoesNotTouchCallbacks(int index)
    {
        var root=new GameObject("ReferenceNavigation",typeof(RectTransform),typeof(Button),typeof(MidnightNavGraphic));
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(106,106);
            var icon=root.GetComponent<MidnightNavGraphic>();icon.Configure(index);var button=root.GetComponent<Button>();int clicks=0;button.onClick.AddListener(()=>clicks++);
            var before=new RectSnapshot((RectTransform)root.transform);int baseVertices=0;
            for(int pass=0;pass<2;pass++)using(var helper=new VertexHelper())
            {
                icon.SetSelected(pass==1);typeof(MidnightNavGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(icon,new object[]{helper});
                var mesh=new Mesh();try
                {
                    helper.FillMesh(mesh);Assert.Greater(mesh.vertexCount,20);Assert.LessOrEqual(mesh.bounds.size.x,106);Assert.LessOrEqual(mesh.bounds.size.y,106);
                    Assert.Less(Mathf.Abs(mesh.bounds.center.x),8);Assert.Less(Mathf.Abs(mesh.bounds.center.y),12);
                    if(pass==0)baseVertices=mesh.vertexCount;else Assert.Greater(mesh.vertexCount,baseVertices);
                }
                finally{UnityEngine.Object.DestroyImmediate(mesh);}
            }
            before.AssertUnchanged((RectTransform)root.transform);Assert.IsFalse(icon.raycastTarget);Assert.IsTrue(icon.Selected);button.onClick.Invoke();Assert.AreEqual(1,clicks);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ReferenceButtonHasLongSlantedEndsButStatusOutlineIsUnchanged()
    {
        var bounds=new Rect(0,0,450,140);var outline=MidnightSurfaceMesh.ReferenceButtonOutline(bounds);
        Assert.AreEqual(6,outline.Length);Assert.Greater(outline[0].x,30);Assert.Less(outline[3].x,420);
        Assert.AreEqual(8,MidnightSurfaceMesh.Outline(bounds,12,true).Length);
    }
    [Test] public void HeroCutoutIsContainedAndDoesNotInterceptShopInput()
    {
        var root=new GameObject("HeroContain",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{ShopCatalogFixture.Example(),(Action<ShopOffer>)(_=>{})});
            var art=view.transform.Find("MidnightHeroArtworkClip/ArtworkBox/CrystalArtwork").GetComponent<RawImage>();
            Assert.NotNull(art.texture);Assert.IsFalse(art.raycastTarget);Assert.IsFalse(art.GetComponent<MidnightArtworkCrop>().enabled);
            Assert.AreEqual(new Rect(0,0,1,1),art.uvRect);Assert.AreEqual(AspectRatioFitter.AspectMode.FitInParent,art.GetComponent<AspectRatioFitter>().aspectMode);
            Assert.That(art.GetComponent<AspectRatioFitter>().aspectRatio,Is.EqualTo(2f/3f).Within(.001f));
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void MainHexIsSymmetricAndDistinctFromSlantedPurchaseButtons()
    {
        var rect=new Rect(0,0,900,200);var hex=MidnightSurfaceMesh.MainHexOutline(rect);
        Assert.AreEqual(6,hex.Length);Assert.AreEqual(100,hex[2].y);Assert.AreEqual(100,hex[5].y);
        Assert.AreEqual(rect.width-hex[0].x,hex[1].x);Assert.AreEqual(hex[0].x,hex[4].x);Assert.AreEqual(hex[1].x,hex[3].x);
        CollectionAssert.AreNotEqual(hex,MidnightSurfaceMesh.ReferenceButtonOutline(rect));
    }
    [Test] public void HeroLayerTracksScrollAndRerenderWithoutEscapingItsOwnMask()
    {
        var root=new GameObject("HeroScroll",typeof(RectTransform),typeof(Canvas));
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(1080,2160);
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var render=typeof(ShopCatalogView).GetMethod("RenderMidnight",Private);
            render.Invoke(view,new object[]{ShopCatalogFixture.Example(),(Action<ShopOffer>)(_=>{})});
            Canvas.ForceUpdateCanvases();LayoutRebuilder.ForceRebuildLayoutImmediate(view.content);
            var clip=(RectTransform)view.transform.Find("MidnightHeroArtworkClip");var follower=clip.GetComponent<MidnightHeroArtworkFollower>();
            Assert.NotNull(clip.GetComponent<RectMask2D>());Assert.IsFalse(clip.GetComponent<CanvasGroup>().blocksRaycasts);Assert.AreEqual(-145,clip.offsetMax.y);Assert.GreaterOrEqual(clip.offsetMin.y,MidnightLobbyTheme.NavigationHeight);
            follower.Refresh();var before=follower.artworkBox.anchoredPosition;
            view.content.anchoredPosition+=Vector2.up*100;follower.Refresh();Assert.That(follower.artworkBox.anchoredPosition.y-before.y,Is.EqualTo(100).Within(.01f));
            render.Invoke(view,new object[]{ShopCatalogFixture.Example(),(Action<ShopOffer>)(_=>{})});
            Assert.AreSame(clip,view.transform.Find("MidnightHeroArtworkClip"));Assert.AreEqual(1,view.GetComponentsInChildren<MidnightHeroArtworkFollower>().Length);
            follower.Bind(null);Assert.AreEqual(0,clip.GetComponent<CanvasGroup>().alpha);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
    public void UtilityPictogramsAreReusedAndDoNotInterceptExistingButtons(int index)
    {
        var root=new GameObject("UtilityIcon",typeof(RectTransform),typeof(Button));
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(400,100);int clicks=0;root.GetComponent<Button>().onClick.AddListener(()=>clicks++);
            var first=MidnightLobbyPresentation.UtilityIcon(root.transform,index);
            Assert.AreSame(first,MidnightLobbyPresentation.UtilityIcon(root.transform,index));Assert.AreEqual(1,root.transform.childCount);Assert.IsFalse(first.raycastTarget);
            using(var helper=new VertexHelper())
            {typeof(MidnightNavGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(first,new object[]{helper});Assert.Greater(helper.currentVertCount,8);}
            root.GetComponent<Button>().onClick.Invoke();Assert.AreEqual(1,clicks);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase(0)][TestCase(1)][TestCase(3)]
    public void ShopV6ShapesAndRemainingLabelsPreserveActualOffers(int remaining)
    {
        var root=new GameObject("ShopV6",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var catalog=ShopCatalogFixture.Example();catalog.rerollsRemaining=remaining;catalog.adAvailable=true;
            var ten=catalog.offers.First(o=>o.rewardType=="GACHA"&&o.rewardAmount==10);ten.discountRemaining=remaining;
            ShopOffer selected=null;
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{catalog,(Action<ShopOffer>)(offer=>selected=offer)});
            Assert.AreEqual($"주간 할인 {remaining}회",view.content.Find("GachaObservatory/DiscountBadge/Label").GetComponent<Text>().text);
            Assert.AreEqual($"광고 리롤 {remaining}회",view.rerollText.text);
            Assert.AreEqual(LobbyNeonGraphic.ShopButtonShape.Compact,view.rerollButton.GetComponent<LobbyNeonGraphic>().MidnightShopShape);
            foreach(var offer in catalog.offers)
            {
                var frame=view.content.GetComponentsInChildren<LobbyNeonGraphic>().First(g=>g.name=="Offer_"+offer.offerId);
                var button=offer.rewardType=="GACHA"?frame.GetComponent<Button>():frame.transform.Find("Buy").GetComponent<Button>();
                Assert.AreEqual(offer.rewardType=="GACHA"?LobbyNeonGraphic.ShopButtonShape.Gacha:LobbyNeonGraphic.ShopButtonShape.Compact,button.GetComponent<LobbyNeonGraphic>().MidnightShopShape);
                button.onClick.Invoke();Assert.AreSame(offer,selected);
            }
            Assert.IsFalse(view.refreshButton.gameObject.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ShopGachaHasEightDistinctConvexClippedCorners()
    {
        var rect=new Rect(0,0,450,140);var points=MidnightSurfaceMesh.ShopGachaOutline(rect);
        Assert.AreEqual(8,points.Length);Assert.AreEqual(8,points.Distinct().Count());
        for(int i=0;i<8;i++)
        {
            var a=points[(i+1)%8]-points[i];var b=points[(i+2)%8]-points[(i+1)%8];
            Assert.Less(a.x*b.y-a.y*b.x,0,"Clockwise convex corners are required by gloss clipping.");
        }
        Assert.AreEqual(rect.yMax,points[0].y);Assert.AreEqual(rect.yMin,points[4].y);
        Assert.Less(Vector2.Distance(points[1],points[2]),Vector2.Distance(points[2],points[3]));
        CollectionAssert.AreNotEqual(MidnightSurfaceMesh.ReferenceButtonOutline(rect),points);
    }
    [Test] public void DiscountBadgeAloneUsesLongSlantedNonInteractiveChip()
    {
        var root=new GameObject("DiscountChip",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{ShopCatalogFixture.Example(),(Action<ShopOffer>)(_=>{})});
            var chip=view.content.Find("GachaObservatory/DiscountBadge").GetComponent<LobbyNeonGraphic>();
            Assert.IsTrue(chip.HasMidnightDiscountChip);Assert.IsFalse(chip.raycastTarget);Assert.IsNull(chip.GetComponent<Button>());Assert.IsFalse(chip.HasMidnightButtonGloss);
            Assert.AreEqual(1,view.GetComponentsInChildren<LobbyNeonGraphic>().Count(g=>g.HasMidnightDiscountChip));
            var outline=MidnightSurfaceMesh.DiscountChipOutline(new Rect(0,0,300,54));
            Assert.AreEqual(4,outline.Length);Assert.Greater(outline[0].x,54*.30f);
            Assert.That((outline[0]-outline[3]).x,Is.EqualTo((outline[1]-outline[2]).x).Within(.001f));
            Assert.AreEqual((outline[0]-outline[3]).y,(outline[1]-outline[2]).y);
            ((RectTransform)chip.transform).sizeDelta=new Vector2(300,54);
            using(var helper=new VertexHelper())
            {typeof(LobbyNeonGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(chip,new object[]{helper});Assert.Greater(helper.currentVertCount,4);}
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ShopRecoveryRefreshReusesCallbackAcrossHiddenNormalAndVisibleErrorStates()
    {
        var root=new GameObject("ShopRecovery",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var button=view.refreshButton;int calls=0;button.onClick.AddListener(()=>calls++);
            Assert.IsFalse(button.gameObject.activeSelf);
            view.ShowRecoveryRefresh(true);Assert.IsTrue(button.gameObject.activeSelf);button.onClick.Invoke();
            view.ShowRecoveryRefresh(false);Assert.IsFalse(button.gameObject.activeSelf);
            view.ShowRecoveryRefresh(true);Assert.IsTrue(button.gameObject.activeSelf);
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{ShopCatalogFixture.Example(),(Action<ShopOffer>)(_=>{})});
            Assert.IsFalse(button.gameObject.activeSelf);Assert.AreSame(button,view.refreshButton);
            view.ShowRecoveryRefresh(true);button.onClick.Invoke();Assert.AreEqual(2,calls);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [TestCase("1")][TestCase("123")][TestCase("123456789")]
    public void UnknownExperienceNeverInventsProgressOrOverlapsLevel(string value)
    {
        var root=new GameObject("UnknownExperience",typeof(RectTransform),typeof(Canvas));
        try
        {
            var profile=(RectTransform)root.transform;profile.sizeDelta=new Vector2(351,106);
            var level=Tmp(profile,"Level",value);level.fontSize=23;
            LobbyFinalPresentation.Place(level.rectTransform,.335f,.08f,.98f,.48f);
            var snapshot=new RectSnapshot(level.rectTransform);
            var placeholder=root.AddComponent<MidnightExperiencePlaceholder>();placeholder.Bind(level);placeholder.Bind(level);
            var track=(RectTransform)profile.Find("MidnightExperienceTrack");var unknown=profile.Find("MidnightExperienceUnknown").GetComponent<Text>();
            Assert.AreEqual("—/—",unknown.text);Assert.AreEqual(3,profile.childCount);Assert.IsFalse(track.GetComponent<Image>().raycastTarget);
            Assert.IsNull(track.Find("Fill"));Assert.IsNull(track.GetComponent<Slider>());Assert.AreEqual(value,level.text);snapshot.AssertUnchanged(level.rectTransform);
            if(track.gameObject.activeSelf)
            {
                float levelEnd=level.rectTransform.anchorMin.x*profile.rect.width+level.GetPreferredValues(value,float.PositiveInfinity,float.PositiveInfinity).x;
                Assert.GreaterOrEqual(track.offsetMin.x,levelEnd+10);Assert.LessOrEqual(track.offsetMax.x,profile.rect.width*.98f+.01f);
            }
            level.text="999999999999999999999999999999999999999";placeholder.Refresh();
            Assert.IsFalse(track.gameObject.activeSelf);Assert.IsFalse(unknown.gameObject.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void NonShopButtonStillUsesUnchangedReferenceSurface()
    {
        var root=new GameObject("UnchangedButton",typeof(RectTransform),typeof(Button),typeof(LobbyNeonGraphic));
        try
        {
            var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(450,140);
            var frame=root.GetComponent<LobbyNeonGraphic>();frame.Configure(LobbyNeonGraphic.FrameStyle.Button);frame.UseMidnightButtonGloss();
            Assert.AreEqual(LobbyNeonGraphic.ShopButtonShape.None,frame.MidnightShopShape);
            using(var actual=new VertexHelper())using(var expected=new VertexHelper())
            {
                typeof(LobbyNeonGraphic).GetMethod("OnPopulateMesh",Private,null,new[]{typeof(VertexHelper)},null).Invoke(frame,new object[]{actual});
                MidnightSurfaceMesh.DrawReferenceButton(expected,frame.GetPixelAdjustedRect(),Color.white,LobbyNeonGraphic.MidnightCorner(LobbyNeonGraphic.FrameStyle.Button,450,140),1,false,true);
                var a=new Mesh();var b=new Mesh();try
                {actual.FillMesh(a);expected.FillMesh(b);CollectionAssert.AreEqual(b.vertices,a.vertices);CollectionAssert.AreEqual(b.colors32,a.colors32);CollectionAssert.AreEqual(b.triangles,a.triangles);}
                finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void UnknownExperienceWithoutLevelCreatesNothingAndNullRebindHidesExistingWidgets()
    {
        var root=new GameObject("MissingExperienceLevel",typeof(RectTransform));
        try
        {
            ((RectTransform)root.transform).sizeDelta=new Vector2(351,106);
            var placeholder=root.AddComponent<MidnightExperiencePlaceholder>();placeholder.Bind(null);
            Assert.AreEqual(0,root.transform.childCount);
            var level=Tmp(root.transform,"Level","1");level.fontSize=23;
            LobbyFinalPresentation.Place(level.rectTransform,.335f,.08f,.98f,.48f);
            placeholder.Bind(level);Assert.IsTrue(root.transform.Find("MidnightExperienceTrack").gameObject.activeSelf);
            placeholder.Bind(null);Assert.IsFalse(root.transform.Find("MidnightExperienceTrack").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("MidnightExperienceUnknown").gameObject.activeSelf);
            placeholder.Bind(level);Assert.IsTrue(root.transform.Find("MidnightExperienceTrack").gameObject.activeSelf);
            Assert.AreEqual(3,root.transform.childCount);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void SupplyHidesOnlyRemainingLabelAndPreservesLimitsCompletionLuckyAndCallbacks()
    {
        var root=new GameObject("SupplyLimitLabel",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var catalog=ShopCatalogFixture.Example();
            var completed=catalog.offers.First(o=>o.offerId=="DAILY_NORMAL");completed.purchased=true;completed.remainingPurchases=0;
            var exhausted=catalog.offers.First(o=>o.offerId=="DAILY_EPIC");exhausted.remainingPurchases=0;
            var original=catalog.offers.ToDictionary(o=>o.offerId,o=>o.remainingPurchases);
            ShopOffer selected=null;
            typeof(ShopCatalogView).GetMethod("RenderMidnight",Private).Invoke(view,new object[]{catalog,(Action<ShopOffer>)(offer=>selected=offer)});
            foreach(var offer in catalog.offers.Where(o=>o.category=="RESOURCE"||o.category=="DAILY"))
            {
                var card=view.content.GetComponentsInChildren<LobbyNeonGraphic>().First(g=>g.name=="Offer_"+offer.offerId);
                var limit=card.transform.Find("Limit");
                if(offer.purchased)Assert.AreEqual("구매 완료",limit.GetComponent<Text>().text);
                else Assert.IsNull(limit,"Only the visual remaining-count label is removed from supply.");
                var button=card.transform.Find("Buy").GetComponent<Button>();Assert.AreEqual(offer.CanBuy,button.interactable);
                if(offer.CanBuy){button.onClick.Invoke();Assert.AreSame(offer,selected);}
                Assert.AreEqual(original[offer.offerId],offer.remainingPurchases);
            }
            Assert.IsFalse(completed.CanBuy);Assert.IsFalse(exhausted.CanBuy);
            Assert.AreEqual("남은 1회",view.content.Find("ProductRow_LUCKY_0/Offer_LUCKY_DNA/Limit").GetComponent<Text>().text);
            Assert.AreEqual("주간 할인 3회",view.content.Find("GachaObservatory/DiscountBadge/Label").GetComponent<Text>().text);
            Assert.AreEqual("광고 리롤 3회",view.rerollText.text);
            CollectionAssert.AreEqual(new[]{3,2,3},catalog.offers.Where(o=>o.category=="RESOURCE").Select(o=>o.remainingPurchases).ToArray());
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    private static TMPro.TMP_Text Tmp(Transform parent,string name,string value)
    {
        var rect=ShopCatalogView.Rect(name,parent);var text=rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.font=TMPro.TMP_Settings.defaultFontAsset;Assert.NotNull(text.font);text.text=value;return text;
    }
    private sealed class RectSnapshot
    {
        private readonly Vector2 min,max,pivot,position,size;
        private readonly Vector3 scale;
        private readonly Quaternion rotation;
        private readonly Transform parent;
        private readonly int sibling;
        public RectSnapshot(RectTransform rect)
        {min=rect.anchorMin;max=rect.anchorMax;pivot=rect.pivot;position=rect.anchoredPosition;size=rect.sizeDelta;scale=rect.localScale;rotation=rect.localRotation;parent=rect.parent;sibling=rect.GetSiblingIndex();}
        public void AssertUnchanged(RectTransform rect,bool compareSibling=true)
        {
            Assert.AreEqual(min,rect.anchorMin);Assert.AreEqual(max,rect.anchorMax);Assert.AreEqual(pivot,rect.pivot);
            Assert.AreEqual(position,rect.anchoredPosition);Assert.AreEqual(size,rect.sizeDelta);Assert.AreEqual(scale,rect.localScale);
            Assert.AreEqual(rotation,rect.localRotation);Assert.AreSame(parent,rect.parent);
            if(compareSibling)Assert.AreEqual(sibling,rect.GetSiblingIndex());
        }
    }
}
