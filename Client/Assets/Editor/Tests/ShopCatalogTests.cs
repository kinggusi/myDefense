using System;
using MyDefense.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ShopCatalogTests
{
    [Test] public void ConfirmationCapturesServerPriceAndRevision()
    {
        var catalog=ShopCatalogFixture.Example();var offer=catalog.offers[1];var request=ShopPurchaseRequest.Confirm(catalog,offer);
        offer.price=3000;catalog.catalogRevision="new";
        Assert.AreEqual(2700,request.expectedPrice);Assert.AreEqual("UI_FIXTURE",request.expectedRevision);
        Assert.IsTrue(Guid.TryParse(request.requestId,out _));Assert.AreEqual("DIAMOND",request.expectedCurrency);
    }
    [Test] public void RetrySerializationKeepsExactRequestIdentity()
    {
        var request=ShopPurchaseRequest.Confirm(ShopCatalogFixture.Example(),ShopCatalogFixture.Example().offers[0]);
        var restored=JsonUtility.FromJson<ShopPurchaseRequest>(JsonUtility.ToJson(request));
        Assert.AreEqual(request.requestId,restored.requestId);Assert.AreEqual(request.expectedPrice,restored.expectedPrice);
        Assert.AreEqual(request.expectedRevision,restored.expectedRevision);Assert.AreEqual(request.slotKey,restored.slotKey);
    }
    [Test] public void PendingScopeSeparatesAccountAndServer()
    {
        Assert.AreNotEqual(ShopPresentation.Scope("http://localhost:1/api",1),ShopPresentation.Scope("http://localhost:1/api",2));
        Assert.AreNotEqual(ShopPresentation.Scope("http://localhost:1/api",1),ShopPresentation.Scope("http://localhost:2/api",1));
    }
    [TestCase("ALIEN_GACHA_SINGLE","GACHA_SINGLE")][TestCase("ALIEN_GACHA_TEN","GACHA_TEN")]
    public void ExistingGachaButtonsMapToCatalogOffer(string oldId,string newId)
    {Assert.AreEqual(newId,ShopPresentation.GachaOfferId(oldId));}
    [TestCase(401,null)][TestCase(403,"FORBIDDEN")][TestCase(200,"JSON_PARSE_ERROR")][TestCase(400,null)][TestCase(500,"INTERNAL_SERVER_ERROR")]
    public void AmbiguousAndAuthenticationFailuresKeepPending(long status,string code)
    {Assert.IsFalse(ShopPresentation.IsDefinitiveRejection(status,code));}
    [TestCase("SHOP_QUOTE_CHANGED")][TestCase("INSUFFICIENT_ACCOUNT_GOLD")][TestCase("SHOP_LIMIT_REACHED")]
    public void ExplicitNoChargeRejectionMayClearPending(string code)
    {Assert.IsTrue(ShopPresentation.IsDefinitiveRejection(400,code));}
    [TestCase(false,"local","http://localhost:8196/api",false)]
    [TestCase(true,"production","http://localhost:8196/api",false)]
    [TestCase(true,"local","https://game.example/api",false)]
    [TestCase(true,"local","http://127.0.0.1:8196/api",true)]
    public void DevelopmentAdsAreStrictlyGated(bool development,string environment,string endpoint,bool expected)
    { Assert.AreEqual(expected,ShopPresentation.DevelopmentAdAllowed(development,environment,endpoint,ShopCatalogFixture.Example())); }
    [Test] public void ServerMustEnableDevelopmentAds()
    {
        var catalog=ShopCatalogFixture.Example();catalog.developmentAd=false;
        Assert.IsFalse(ShopPresentation.DevelopmentAdAllowed(true,"local","http://localhost/api",catalog));
        catalog.developmentAd=true;catalog.adAvailable=false;
        Assert.IsFalse(ShopPresentation.DevelopmentAdAllowed(true,"local","http://localhost/api",catalog));
    }
    [Test] public void PurchasedOrSoldOutOfferCannotBeBought()
    {
        var offer=new ShopOffer{remainingPurchases=-1};Assert.IsTrue(offer.CanBuy);
        offer.purchased=true;Assert.IsFalse(offer.CanBuy);offer.purchased=false;offer.remainingPurchases=0;Assert.IsFalse(offer.CanBuy);
    }
    [Test] public void DnaLabelAndCurrencyAreUserFacing()
    {
        Assert.AreEqual("왹져DNA 20개",ShopPresentation.Title(new ShopOffer{rewardType="DNA",rewardAmount=20}));
        Assert.AreEqual("골드",ShopPresentation.Currency("GOLD"));
    }
    [Test] public void DailyTitleUsesCollectionNameInsteadOfDatabaseId()
    {Assert.AreEqual("NORMAL 4 조각 20개",ShopPresentation.Title(new ShopOffer{rewardType="ALIEN_PIECE",grade="NORMAL",alienId=25,rewardAmount=20},"NORMAL 4"));}
    [Test] public void PendingStoreSurvivesRecreationWithoutNewRequestId()
    {
        string endpoint="https://shop-test.invalid/"+Guid.NewGuid();var store=new ShopPendingRequestStore(endpoint,1);
        try
        {
            var request=ShopPurchaseRequest.Confirm(ShopCatalogFixture.Example(),ShopCatalogFixture.Example().offers[0]);
            store.Save("purchase",JsonUtility.ToJson(request));
            var restored=new ShopPendingRequestStore(endpoint,1).Load();
            Assert.AreEqual("purchase",restored.kind);Assert.AreEqual(request.requestId,JsonUtility.FromJson<ShopPurchaseRequest>(restored.json).requestId);
        }
        finally{store.Clear();}
    }
    [TestCase("null")][TestCase("{}")][TestCase("{\"kind\":\"unknown\",\"json\":\"{}\"}")]
    public void CorruptPendingFailsClosedAndIsNotDeleted(string json)
    {
        string endpoint="https://shop-test.invalid/"+Guid.NewGuid();string key=ShopPresentation.Scope(endpoint,1);
        PlayerPrefs.SetString(key,json);
        try{Assert.Throws<InvalidOperationException>(()=>new ShopPendingRequestStore(endpoint,1).Load());Assert.IsTrue(PlayerPrefs.HasKey(key));}
        finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
    }
    [Test] public void UnavailableProviderCannotReportCompletion()
    {
        var provider=new UnavailableShopAdProvider();Assert.IsFalse(provider.Available);
        provider.Show(outcome=>Assert.AreEqual(ShopAdOutcome.Failed,outcome));
    }
    [Test] public void LegacyPlaceholderIsHiddenWithoutTouchingCatalogOrGachaScreen()
    {
        var canvas=new GameObject("TestCanvas");
        try
        {
            var shop=new GameObject("view_Shop");shop.transform.SetParent(canvas.transform);
            var old=new GameObject("Shop_ScrollView");old.transform.SetParent(shop.transform);
            var content=new GameObject("Content");content.transform.SetParent(old.transform);
            var entry=new GameObject("GachaEntry");entry.transform.SetParent(content.transform);
            var catalog=new GameObject("ShopCatalog");catalog.transform.SetParent(shop.transform);
            var gacha=new GameObject("GachaScreen");gacha.transform.SetParent(canvas.transform);
            ShopCatalogController.HideLegacyPlaceholder(shop.transform);
            Assert.IsFalse(old.activeSelf);Assert.IsFalse(entry.activeInHierarchy);
            Assert.IsTrue(catalog.activeSelf);Assert.IsTrue(gacha.activeSelf);Assert.IsTrue(shop.activeSelf);
        }
        finally{UnityEngine.Object.DestroyImmediate(canvas);}
    }
    [TestCase(300)][TestCase(2700)][TestCase(3000)]
    public void GachaTitleAndSeparatePriceLabelBothUseCurrentOffer(int price)
    {
        var root=new GameObject("PurchaseButton",typeof(RectTransform),typeof(UnityEngine.UI.Button));
        try
        {
            var title=ShopCatalogView.Label(root.transform,"LabelText","10회 뽑기 · 5,000",25,0,.5f,1,1);
            var cost=ShopCatalogView.Label(root.transform,"PriceText","♦ 5,000",25,0,0,1,.5f);
            GachaShopController.SetPurchaseLabels(root.GetComponent<UnityEngine.UI.Button>(),"왹져 뽑기",price);
            Assert.AreEqual("왹져 뽑기",title.text);Assert.AreEqual("다이아 "+price.ToString("N0"),cost.text);
            Assert.IsFalse(title.text.Contains("5,000"));Assert.IsFalse(cost.text.Contains("5,000"));
            GachaShopController.SetPurchaseLabels(root.GetComponent<UnityEngine.UI.Button>(),"왹져 뽑기",null);
            Assert.AreEqual("상점 조회 필요",cost.text);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void CardBodyDragHandlerAndWheelMoveCatalogContent()
    {
        var canvasRoot=new GameObject("ShopDragTest",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        var eventRoot=new GameObject("ShopDragEvents",typeof(UnityEngine.EventSystems.EventSystem));
        try
        {
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var scaler=canvasRoot.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var view=ShopCatalogView.Build(canvasRoot.transform);view.Render(ShopCatalogFixture.Example(),_=>{});
            Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(view.content);
            view.scroll.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);
            Assert.Greater(view.content.rect.height,view.scroll.viewport.rect.height);
            var card=(RectTransform)view.content.Find("Offer_GACHA_SINGLE");
            Assert.IsTrue(card.GetComponent<LobbyNeonGraphic>().raycastTarget);
            Vector3 world=card.TransformPoint(new Vector3(card.rect.xMin+card.rect.width*.35f,card.rect.center.y,0));
            var pointer=new UnityEngine.EventSystems.PointerEventData(eventRoot.GetComponent<UnityEngine.EventSystems.EventSystem>());
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,world);pointer.pressPosition=pointer.position;
            pointer.button=UnityEngine.EventSystems.PointerEventData.InputButton.Left;
            // EditMode has no rendered Graphic depth, so test event routing independently.
            // Actual raycast selection is exercised in the isolated PlayMode fixture as well.
            pointer.pointerPressRaycast=new UnityEngine.EventSystems.RaycastResult {gameObject=card.gameObject,module=canvasRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>()};
            var target=UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(card.gameObject);
            Assert.AreSame(view.scroll.gameObject,target,"Card text/body must route dragging to the new catalog, not legacy content.");
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.initializePotentialDrag);
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
            float before=view.content.anchoredPosition.y;pointer.position+=Vector2.up*180f;
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.dragHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
            Assert.Greater(view.content.anchoredPosition.y,before+100f);
            before=view.content.anchoredPosition.y;pointer.scrollDelta=Vector2.down;
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.scrollHandler);
            Assert.GreaterOrEqual(view.content.anchoredPosition.y,before+30f);
        }
        finally{UnityEngine.Object.DestroyImmediate(canvasRoot);UnityEngine.Object.DestroyImmediate(eventRoot);}
    }
    [TestCase(0,true)][TestCase(503,true)][TestCase(408,true)][TestCase(409,false)][TestCase(403,false)]
    public void FailureRetryClassification(long status,bool expected){Assert.AreEqual(expected,ShopPresentation.Retryable(status));}
    [Test] public void CatalogPrefabHasAllReferencesAndScrollClip()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ShopCatalogAssetBuilder.PrefabPath);Assert.NotNull(prefab);
        var view=prefab.GetComponent<ShopCatalogView>();Assert.NotNull(view);Assert.NotNull(view.content);Assert.NotNull(view.scroll);
        Assert.NotNull(view.scroll.viewport.GetComponent<UnityEngine.UI.RectMask2D>());
        Assert.IsFalse(view.scroll.horizontal);Assert.NotNull(view.rerollButton);Assert.NotNull(view.refreshButton);
        Assert.LessOrEqual(view.scroll.viewport.anchorMax.y,.75f);Assert.GreaterOrEqual(view.scroll.viewport.anchorMin.y,.20f);
    }
    [Test] public void FixtureIsNotInProductionBuild()
    {
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopCatalogAssetBuilder.FixturePath));
        foreach(var scene in EditorBuildSettings.scenes)Assert.IsFalse(scene.enabled&&scene.path==ShopCatalogAssetBuilder.FixturePath);
    }
}
