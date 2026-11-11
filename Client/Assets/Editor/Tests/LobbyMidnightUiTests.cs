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
            Assert.That(view.content.Find("ProductRow_RESOURCE_0/Offer_GOLD/RegularPrice").GetComponent<Text>().text,Does.Contain("700"));
            Assert.IsFalse(view.rerollButton.interactable);
            Assert.AreEqual(catalog.offers.Length,view.content.GetComponentsInChildren<Button>().Count(b=>b!=view.rerollButton));
            Assert.AreEqual(900,view.content.Find("GachaObservatory").GetComponent<LayoutElement>().preferredHeight);
            Assert.AreEqual(52,button.GetComponentInChildren<Text>().resizeTextMaxSize);
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
            Assert.AreEqual(31,card.text_Name.resizeTextMaxSize);Assert.AreEqual(27,card.text_Pieces.resizeTextMaxSize);
            Assert.Greater(card.text_Pieces.transform.GetSiblingIndex(),root.transform.Find("MidnightQuantityPlate").GetSiblingIndex());
            Assert.AreEqual(Color.white,card.text_Grade.color);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void ResourceThreeProductsFillThreeColumnsAndRerenderPreservesAdListener()
    {
        var root=new GameObject("MidnightRerenderTest",typeof(RectTransform),typeof(Canvas));
        try
        {
            var view=(ShopCatalogView)typeof(ShopCatalogView).GetMethod("BuildMidnight",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root.transform});
            var method=typeof(ShopCatalogView).GetMethod("RenderMidnight",Private);var catalog=ShopCatalogFixture.Example();int clicks=0;
            view.rerollButton.onClick.AddListener(()=>clicks++);
            for(int i=0;i<3;i++)method.Invoke(view,new object[]{catalog,(Action<ShopOffer>)(_=>{})});
            view.rerollButton.onClick.Invoke();Assert.AreEqual(1,clicks);
            var row=view.content.Find("ProductRow_RESOURCE_0");Assert.AreEqual(3,row.childCount);
            var first=(RectTransform)row.GetChild(0);var last=(RectTransform)row.GetChild(2);
            Assert.That(first.anchorMax.x-first.anchorMin.x,Is.EqualTo(1f/3-.014f).Within(.001f));Assert.That(last.anchorMax.x,Is.GreaterThan(.98f));
            Assert.AreEqual(catalog.offers.Length,view.content.GetComponentsInChildren<Button>().Count(b=>b!=view.rerollButton));
            Assert.AreEqual("Section_RESOURCE",view.rerollButton.transform.parent.name);
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
            view.statusText.text="상점 조회 실패 · 다시 조회해 주세요.";refresh.Invoke(view,null);Assert.IsTrue(view.statusText.gameObject.activeSelf);Assert.AreEqual(-385,((RectTransform)view.scroll.transform).offsetMax.y);
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
