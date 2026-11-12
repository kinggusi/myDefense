using System;
using System.Linq;
using MyDefense.Lobby;
using MyDefense.Shop;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ShopCatalogView
{
    private bool midnightLayout;
    private float midnightSafeBottom=-1;
    private Canvas midnightCanvas;
    private bool adInHeader;
    private string lastMidnightStatus;
    private RectTransform midnightHeroClip;
    private MidnightHeroArtworkFollower heroFollower;
    private bool recoveryRefreshVisible;
    public void ShowRecoveryRefresh(bool visible)
    {
        recoveryRefreshVisible=visible;
        if(midnightLayout&&refreshButton!=null)refreshButton.gameObject.SetActive(visible);
    }
    private void OnTransformParentChanged(){midnightCanvas=null;}
    private void LateUpdate()
    {
        if(!midnightLayout)return;
        if(midnightCanvas==null)midnightCanvas=GetComponentInParent<Canvas>();
        float bottom=Screen.safeArea.yMin/Mathf.Max(.01f,midnightCanvas==null?1:midnightCanvas.scaleFactor);
        if(Mathf.Abs(midnightSafeBottom-bottom)>.1f)ApplyMidnightSafeBottom(bottom);
        RefreshMidnightStatus();
    }
    private void ApplyMidnightSafeBottom(float bottom)
    {
        midnightSafeBottom=Mathf.Max(0,bottom);
        if(scroll!=null){var rect=(RectTransform)scroll.transform;rect.offsetMin=new Vector2(rect.offsetMin.x,(adInHeader?MidnightLobbyTheme.ContentBottom:355)+midnightSafeBottom);}
        if(rerollButton!=null&&!adInHeader){var rect=(RectTransform)rerollButton.transform;rect.offsetMin=new Vector2(rect.offsetMin.x,277+midnightSafeBottom);rect.offsetMax=new Vector2(rect.offsetMax.x,337+midnightSafeBottom);}
        if(midnightHeroClip!=null)midnightHeroClip.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom+midnightSafeBottom);
    }
    private static ShopCatalogView BuildMidnight(Transform parent)
    {
        var root=Rect("ShopCatalog",parent);Stretch(root,0,0,1,1);
        var view=root.gameObject.AddComponent<ShopCatalogView>();
        Label(root,"Title","상점",72,.055f,.845f,.65f,.90f,TextAnchor.MiddleLeft).fontStyle=FontStyle.BoldAndItalic;
        view.statusText=Label(root,"Status","상품을 불러오는 중...",21,.06f,.104f,.94f,.13f,TextAnchor.MiddleLeft);
        view.walletText=Label(root,"Wallet","",1,0,0,0,0);view.walletText.gameObject.SetActive(false);
        view.discountText=Label(root,"Discount","",24,.06f,.803f,.70f,.835f,TextAnchor.MiddleLeft);
        view.refreshButton=Button(root,"Refresh","새로고침",.75f,.803f,.94f,.84f);
        var viewport=Rect("Viewport",root);Stretch(viewport,.04f,.15f,.96f,.80f);
        viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.001f);viewport.gameObject.AddComponent<RectMask2D>();
        view.content=Rect("Products",viewport);view.content.anchorMin=new Vector2(0,1);view.content.anchorMax=new Vector2(1,1);view.content.pivot=new Vector2(.5f,1);view.content.sizeDelta=Vector2.zero;
        var layout=view.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(8,8,8,35);layout.spacing=20;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        view.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        view.scroll=viewport.gameObject.AddComponent<ScrollRect>();view.scroll.viewport=viewport;view.scroll.content=view.content;
        view.scroll.horizontal=false;view.scroll.vertical=true;view.scroll.movementType=ScrollRect.MovementType.Clamped;view.scroll.scrollSensitivity=32;
        view.rerollButton=Button(root,"AdReroll","광고 리롤",.51f,.135f,.94f,.175f);
        view.rerollText=view.rerollButton.GetComponentInChildren<Text>();
        view.midnightLayout=true;
        view.EnsureMidnightLayout();
        return view;
    }

    // Resources prefabs were authored before Midnight. Restyle existing references instead of
    // replacing them, so controller event bindings, busy state and JWT retry behavior survive.
    private void EnsureMidnightLayout()
    {
        if(midnightLayout&&content==null)return;
        midnightLayout=true;
        var title=transform.Find("Title")?.GetComponent<Text>();
        if(title!=null){MidnightLobbyPresentation.Top(title.rectTransform,.055f,.65f,200,360);MidnightLobbyTheme.DisplayTypography(title,120);}
        var eyebrow=transform.Find("MidnightEyebrow");if(eyebrow!=null)eyebrow.gameObject.SetActive(false);
        if(walletText!=null)walletText.gameObject.SetActive(false);
        if(statusText!=null){MidnightLobbyPresentation.Top(statusText.rectTransform,.06f,.94f,350,435);statusText.color=MidnightLobbyTheme.Muted;MidnightLobbyTheme.Typography(statusText,30,FontStyle.Normal);}
        if(discountText!=null)discountText.gameObject.SetActive(false);
        if(refreshButton!=null)
        {
            refreshButton.gameObject.SetActive(recoveryRefreshVisible);
            MidnightLobbyPresentation.Top((RectTransform)refreshButton.transform,.88f,.96f,250,320);
            var refreshFrame=refreshButton.GetComponent<LobbyNeonGraphic>();if(refreshFrame!=null)refreshFrame.color=new Color(1,1,1,0);
            var refreshLabel=refreshButton.GetComponentInChildren<Text>();if(refreshLabel!=null){refreshLabel.text="↻";MidnightLobbyTheme.Typography(refreshLabel,40);refreshLabel.color=MidnightLobbyTheme.Muted;}
        }
        if(scroll!=null)
        {
            var rect=(RectTransform)scroll.transform;rect.anchorMin=new Vector2(.025f,0);rect.anchorMax=new Vector2(.975f,1);
            rect.offsetMin=new Vector2(0,355);rect.offsetMax=new Vector2(0,-350);
            var image=scroll.GetComponent<Image>();if(image!=null)image.color=new Color(0,0,0,.001f);
        }
        if(rerollButton!=null&&!adInHeader){var rect=(RectTransform)rerollButton.transform;rect.anchorMin=new Vector2(.06f,0);rect.anchorMax=new Vector2(.94f,0);rect.offsetMin=new Vector2(0,232);rect.offsetMax=new Vector2(0,292);}
        var reset=transform.Find("ResetNotice");if(reset!=null)reset.gameObject.SetActive(false);
        var layout=content.GetComponent<VerticalLayoutGroup>();if(layout!=null){layout.spacing=20;layout.padding=new RectOffset(8,8,8,35);}
        if(midnightCanvas==null)midnightCanvas=GetComponentInParent<Canvas>();
        ApplyMidnightSafeBottom(Screen.safeArea.yMin/Mathf.Max(.01f,midnightCanvas==null?1:midnightCanvas.scaleFactor));
    }

    private void RenderMidnight(ShopCatalog catalog,Action<ShopOffer> purchase)
    {
        if(heroFollower!=null)heroFollower.Bind(null);
        if(rerollButton!=null&&rerollButton.transform.IsChildOf(content))rerollButton.transform.SetParent(transform,false);
        adInHeader=false;
        for(int i=content.childCount-1;i>=0;i--){var child=content.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
        buyButtons.Clear();cards.Clear();
        walletText.text=catalog.wallet==null?"":$"골드 {catalog.wallet.gold:N0} | 다이아 {catalog.wallet.diamond:N0}";
        var offers=(catalog.offers??Array.Empty<ShopOffer>()).Where(o=>o!=null).ToArray();
        var ten=offers.FirstOrDefault(o=>o.rewardType=="GACHA"&&o.rewardAmount==10);
        discountText.text=ten==null?"":$"10회 뽑기 · 주간 할인 {ten.discountRemaining}회 남음";
        var gachas=offers.Where(o=>o.rewardType=="GACHA").ToArray();
        if(gachas.Length>0)
        {
            var hero=Row("GachaObservatory",890);
            var artBox=Rect("CrystalArtworkBox",hero);Stretch(artBox,.39f,.26f,1.03f,1.16f);
            EnsureHeroArtworkLayer(artBox);
            var title=Label(hero,"HeroTitle","왹져 뽑기",120,.025f,.72f,.62f,.94f,TextAnchor.MiddleLeft);MidnightLobbyTheme.DisplayTypography(title,120);
            Label(hero,"HeroDescription","신규 왹져 획득!\n다양한 왹져를\n만나보세요!",31,.035f,.55f,.43f,.73f,TextAnchor.MiddleLeft).color=MidnightLobbyTheme.Ink;
            if(ten!=null)
            {
                var chip=Rect("DiscountBadge",hero);Stretch(chip,.015f,.205f,.30f,.265f);Frame(chip,LobbyNeonGraphic.FrameStyle.Button);
                chip.GetComponent<LobbyNeonGraphic>().UseMidnightAccent(new Color(.12f,.26f,.44f,.94f));
                chip.GetComponent<LobbyNeonGraphic>().UseMidnightDiscountChip();
                Label(chip,"Label",$"주간 할인 {ten.discountRemaining}회",26,.05f,.05f,.95f,.95f);
            }
            for(int i=0;i<gachas.Length;i++)
            {
                var offer=gachas[i];float width=1f/gachas.Length;
                var button=Button(hero,"Offer_"+offer.offerId,$"{offer.rewardAmount}회  {offer.price:N0}",i*width+.01f,.015f,(i+1)*width-.01f,.17f);
                PriceButton(button,offer.currencyType,48);
                button.GetComponent<LobbyNeonGraphic>().UseMidnightShopButton(LobbyNeonGraphic.ShopButtonShape.Gacha);
                button.onClick.AddListener(()=>purchase(offer));button.interactable=offer.CanBuy&&!busy;
                buyButtons.Add(button);cards[offer.offerId]=(RectTransform)button.transform;
                if(offer.rewardAmount==10)AccentButton(button);
                if(offer.regularPrice>offer.price)
                    Label(hero,"RegularPrice_"+offer.offerId,$"정상가 {offer.regularPrice:N0} {ShopPresentation.Currency(offer.currencyType)}",25,i*width+.01f,.175f,(i+1)*width-.01f,.215f).color=MidnightLobbyTheme.Muted;
            }
        }
        // Presentation grouping only: the original ShopOffer, category, limit and callback are
        // retained. RESOURCE plus actual DAILY offers share a four-column supply shelf.
        foreach(var group in offers.Where(o=>o.rewardType!="GACHA").GroupBy(o=>o.category=="RESOURCE"||o.category=="DAILY"?"SUPPLY":o.category))
        {
            var heading=Row("Section_"+group.Key,88);
            string label=group.Key=="LUCKY"?"럭키 골드 특가":"오늘의 보급";
            Label(heading,"SectionTitle",label,48,.01f,.12f,adInHeader?.99f:.53f,1,TextAnchor.MiddleLeft).fontStyle=FontStyle.Bold;
            if(!adInHeader)
            {
                rerollButton.transform.SetParent(heading,false);Stretch((RectTransform)rerollButton.transform,.61f,.30f,.99f,.94f);
                rerollButton.GetComponent<LobbyNeonGraphic>().UseMidnightShopButton(LobbyNeonGraphic.ShopButtonShape.Compact);
                MidnightLobbyTheme.Typography(rerollText,27);
                MidnightLobbyPresentation.UtilityIcon(rerollButton.transform,8,"MidnightUtilityIcon",.045f,.16f);
                MidnightLobbyPresentation.UtilityIcon(rerollButton.transform,9,"MidnightRefreshIcon",.84f,.955f);
                LobbyFinalPresentation.Place(rerollText.rectTransform,.18f,.06f,.82f,.94f);
                Label(heading,"RerollScope","일일·럭키 상품만 변경",20,.54f,0,.99f,.28f).color=MidnightLobbyTheme.Muted;adInHeader=true;
            }
            var entries=group.ToArray();
            for(int start=0;start<entries.Length;start+=4)
            {
                var row=Row("ProductRow_"+group.Key+"_"+start,380);
                int columns=4;
                for(int column=0;column<columns&&start+column<entries.Length;column++)
                {
                    float width=1f/columns;
                    var offer=entries[start+column];var card=Rect("Offer_"+offer.offerId,row);Stretch(card,column*width+.007f,0,(column+1)*width-.007f,1);
                    Frame(card,LobbyNeonGraphic.FrameStyle.Panel);card.GetComponent<LobbyNeonGraphic>().raycastTarget=true;
                    card.GetComponent<LobbyNeonGraphic>().UseMidnightAccent(new Color(.085f,.125f,.20f,.97f));
                    if(!string.IsNullOrEmpty(offer.grade))card.GetComponent<LobbyNeonGraphic>().SetGrade(offer.grade);
                    var icon=Rect("Icon",card);Stretch(icon,.055f,.44f,.945f,.94f);
                    if(offer.rewardType=="DNA"||offer.rewardType=="GROWTH_CELL"||offer.rewardType=="GOLD")
                        MidnightShopIconAtlas.Place(icon,offer.rewardType=="DNA"?MidnightShopIconAtlas.Icon.Dna:offer.rewardType=="GROWTH_CELL"?MidnightShopIconAtlas.Icon.Cells:MidnightShopIconAtlas.Icon.Coin);
                    else
                    {
                        var sprite=Resources.Load<LobbyUnitPortraitCatalog>(LobbyUnitPortraitCatalog.ResourcePath)?.Find(offer.alienId,offer.grade);
                        if(sprite!=null){var image=icon.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;}
                        else MidnightShopIconAtlas.Place(icon,MidnightShopIconAtlas.Icon.Shards);
                    }
                    bool discounted=offer.regularPrice>offer.price;
                    string product=offer.rewardType=="GOLD"?"골드":offer.rewardType=="DNA"?"왹져DNA":offer.rewardType=="GROWTH_CELL"?"성장세포":AlienName?.Invoke(offer.alienId)??"유닛 조각";
                    if(offer.category=="DAILY")Label(card,"DailyBadge","일일",19,.06f,.90f,.94f,.98f,TextAnchor.MiddleLeft).color=MidnightLobbyTheme.Muted;
                    Label(card,"Name",$"{product}\nx{offer.rewardAmount:N0}",28,.06f,.29f,.94f,.49f);
                    if(discounted)Label(card,"RegularPrice",$"정상가 {offer.regularPrice:N0}",20,.05f,.24f,.95f,.30f).color=MidnightLobbyTheme.Muted;
                    if(offer.purchased||group.Key!="SUPPLY")
                        Label(card,"Limit",offer.purchased?"구매 완료":offer.remainingPurchases<0?"":$"남은 {offer.remainingPurchases}회",20,.04f,.18f,.96f,.24f).color=MidnightLobbyTheme.Muted;
                    var buy=Button(card,"Buy",offer.purchased?"완료":$"{offer.price:N0}",.05f,.025f,.95f,.185f);
                    PriceButton(buy,offer.currencyType,32);
                    buy.GetComponent<LobbyNeonGraphic>().UseMidnightShopButton(LobbyNeonGraphic.ShopButtonShape.Compact);
                    buy.onClick.AddListener(()=>purchase(offer));buy.interactable=offer.CanBuy&&!busy;buyButtons.Add(buy);cards[offer.offerId]=card;
                }
            }
        }
        rerollText.text=catalog.adAvailable?$"광고 리롤 {catalog.rerollsRemaining}회":"광고 연결 준비 중";
        rerollButton.interactable=!busy&&catalog.adAvailable&&catalog.rerollsRemaining>0;
        if(!adInHeader){var rect=(RectTransform)rerollButton.transform;rect.anchorMin=new Vector2(.06f,0);rect.anchorMax=new Vector2(.94f,0);}
        ShowRecoveryRefresh(false);statusText.text="";RefreshMidnightStatus();ApplyMidnightSafeBottom(Mathf.Max(0,midnightSafeBottom));
    }
    private RectTransform Row(string name,float height)
    {
        var row=Rect(name,content);row.gameObject.AddComponent<LayoutElement>().preferredHeight=height;return row;
    }
    private void EnsureHeroArtworkLayer(RectTransform placeholder)
    {
        if(midnightHeroClip==null)
        {
            midnightHeroClip=Rect("MidnightHeroArtworkClip",transform);
            midnightHeroClip.anchorMin=new Vector2(.025f,0);midnightHeroClip.anchorMax=new Vector2(.975f,1);
            midnightHeroClip.offsetMin=new Vector2(0,MidnightLobbyTheme.ContentBottom+Mathf.Max(0,midnightSafeBottom));midnightHeroClip.offsetMax=new Vector2(0,-145);
            midnightHeroClip.gameObject.AddComponent<RectMask2D>();
            midnightHeroClip.SetSiblingIndex(scroll.transform.GetSiblingIndex());
            var group=midnightHeroClip.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            var box=Rect("ArtworkBox",midnightHeroClip);
            var art=MidnightLobbyTheme.Art(box,"CrystalArtwork",MidnightLobbyTheme.GachaCutoutResource,0,0,1,1);
            art.GetComponent<MidnightArtworkCrop>().enabled=false;art.uvRect=new Rect(0,0,1,1);
            var contain=art.gameObject.AddComponent<AspectRatioFitter>();contain.aspectMode=AspectRatioFitter.AspectMode.FitInParent;contain.aspectRatio=art.texture!=null?(float)art.texture.width/art.texture.height:.75f;
            heroFollower=midnightHeroClip.gameObject.AddComponent<MidnightHeroArtworkFollower>();heroFollower.artworkBox=box;heroFollower.visibility=group;
        }
        heroFollower.Bind(placeholder);
    }
    private static void AccentButton(Button button)
    {
        var frame=button.GetComponent<LobbyNeonGraphic>();if(frame!=null){frame.UseMidnightPrimary();frame.color=Color.white;}
        var label=button.GetComponentInChildren<Text>();if(label!=null)label.color=new Color(.025f,.065f,.10f);
    }
    private void RefreshMidnightStatus()
    {
        if(statusText==null||scroll==null)return;
        string message=statusText.text??"";
        if(lastMidnightStatus==message)return;lastMidnightStatus=message;
        bool visible=!string.IsNullOrWhiteSpace(message);
        statusText.gameObject.SetActive(visible);
        var rect=(RectTransform)scroll.transform;rect.offsetMax=new Vector2(rect.offsetMax.x,visible?-450:-350);
    }
    private static void PriceButton(Button button,string currency,int fontSize)
    {
        var label=button.GetComponentInChildren<Text>();MidnightLobbyTheme.Typography(label,fontSize);
        LobbyFinalPresentation.Place(label.rectTransform,.24f,.06f,.96f,.94f);
        var icon=Rect("CurrencyIcon",button.transform);Stretch(icon,.065f,.21f,.23f,.79f);
        MidnightIconAtlas.Place(icon,currency=="GOLD"?MidnightIconAtlas.Icon.Coin:MidnightIconAtlas.Icon.Diamond);
    }
}
