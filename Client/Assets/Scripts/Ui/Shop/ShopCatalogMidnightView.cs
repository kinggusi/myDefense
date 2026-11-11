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
        if(scroll!=null){var rect=(RectTransform)scroll.transform;rect.offsetMin=new Vector2(rect.offsetMin.x,(adInHeader?225:310)+midnightSafeBottom);}
        if(rerollButton!=null&&!adInHeader){var rect=(RectTransform)rerollButton.transform;rect.offsetMin=new Vector2(rect.offsetMin.x,232+midnightSafeBottom);rect.offsetMax=new Vector2(rect.offsetMax.x,292+midnightSafeBottom);}
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
        if(title!=null){MidnightLobbyPresentation.Top(title.rectTransform,.055f,.65f,165,275);MidnightLobbyTheme.Typography(title,88,FontStyle.BoldAndItalic);}
        var eyebrow=transform.Find("MidnightEyebrow");if(eyebrow!=null)eyebrow.gameObject.SetActive(false);
        if(walletText!=null)walletText.gameObject.SetActive(false);
        if(statusText!=null){MidnightLobbyPresentation.Top(statusText.rectTransform,.06f,.94f,285,370);statusText.color=MidnightLobbyTheme.Muted;MidnightLobbyTheme.Typography(statusText,30,FontStyle.Normal);}
        if(discountText!=null)discountText.gameObject.SetActive(false);
        if(refreshButton!=null)
        {
            MidnightLobbyPresentation.Top((RectTransform)refreshButton.transform,.85f,.95f,177,267);
            var refreshLabel=refreshButton.GetComponentInChildren<Text>();if(refreshLabel!=null){refreshLabel.text="↻";MidnightLobbyTheme.Typography(refreshLabel,48);}
        }
        if(scroll!=null)
        {
            var rect=(RectTransform)scroll.transform;rect.anchorMin=new Vector2(.04f,0);rect.anchorMax=new Vector2(.96f,1);
            rect.offsetMin=new Vector2(0,310);rect.offsetMax=new Vector2(0,-285);
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
            var hero=Row("GachaObservatory",900);
            var art=MidnightLobbyTheme.Art(hero,"CrystalArtwork",MidnightLobbyTheme.GachaResource,0,.17f,1,1);
            var crop=art.GetComponent<MidnightArtworkCrop>();crop.focalPoint=new Vector2(1,.5f);crop.Refresh();
            var title=Label(hero,"HeroTitle","왹져 뽑기",88,.025f,.57f,.64f,.81f,TextAnchor.MiddleLeft);title.fontStyle=FontStyle.BoldAndItalic;
            Label(hero,"HeroDescription","신규 왹져 획득!\n다양한 왹져를\n만나보세요!",34,.035f,.33f,.50f,.57f,TextAnchor.MiddleLeft).color=MidnightLobbyTheme.Ink;
            if(ten!=null)
            {
                var chip=Rect("DiscountBadge",hero);Stretch(chip,.015f,.205f,.38f,.275f);Frame(chip,LobbyNeonGraphic.FrameStyle.Button);
                Label(chip,"Label",$"주간 할인 {ten.discountRemaining}회 남음",28,.05f,.05f,.95f,.95f);
            }
            for(int i=0;i<gachas.Length;i++)
            {
                var offer=gachas[i];float width=1f/gachas.Length;
                var button=Button(hero,"Offer_"+offer.offerId,$"{offer.rewardAmount}회  {offer.price:N0}",i*width+.01f,.015f,(i+1)*width-.01f,.17f);
                PriceButton(button,offer.currencyType,52);
                button.onClick.AddListener(()=>purchase(offer));button.interactable=offer.CanBuy&&!busy;
                buyButtons.Add(button);cards[offer.offerId]=(RectTransform)button.transform;
                if(offer.rewardAmount==10)AccentButton(button);
                if(offer.regularPrice>offer.price)
                    Label(hero,"RegularPrice_"+offer.offerId,$"정상가 {offer.regularPrice:N0} {ShopPresentation.Currency(offer.currencyType)}",25,i*width+.01f,.175f,(i+1)*width-.01f,.215f).color=MidnightLobbyTheme.Muted;
            }
        }
        foreach(var group in offers.Where(o=>o.rewardType!="GACHA").GroupBy(o=>o.category))
        {
            var heading=Row("Section_"+group.Key,96);
            string label=group.Key=="DAILY"?"오늘의 조각":group.Key=="LUCKY"?"럭키 골드 특가":"오늘의 보급";
            Label(heading,"SectionTitle",label,52,.01f,0,adInHeader?.99f:.53f,1,TextAnchor.MiddleLeft).fontStyle=FontStyle.Bold;
            if(!adInHeader)
            {
                rerollButton.transform.SetParent(heading,false);Stretch((RectTransform)rerollButton.transform,.55f,.1f,.99f,.9f);
                MidnightLobbyTheme.Typography(rerollText,30);adInHeader=true;
            }
            var entries=group.ToArray();
            for(int start=0;start<entries.Length;start+=4)
            {
                var row=Row("ProductRow_"+group.Key+"_"+start,350);
                row.GetComponent<LayoutElement>().preferredHeight=400;
                int columns=group.Key=="RESOURCE"?Mathf.Min(4,entries.Length-start):4;
                for(int column=0;column<columns&&start+column<entries.Length;column++)
                {
                    float width=1f/columns;
                    var offer=entries[start+column];var card=Rect("Offer_"+offer.offerId,row);Stretch(card,column*width+.007f,0,(column+1)*width-.007f,1);
                    Frame(card,LobbyNeonGraphic.FrameStyle.Panel);card.GetComponent<LobbyNeonGraphic>().raycastTarget=true;
                    if(!string.IsNullOrEmpty(offer.grade))card.GetComponent<LobbyNeonGraphic>().SetGrade(offer.grade);
                    var icon=Rect("Icon",card);Stretch(icon,.15f,.48f,.85f,.88f);
                    if(offer.rewardType=="DNA"||offer.rewardType=="GROWTH_CELL"||offer.rewardType=="GOLD")
                        MidnightIconAtlas.Place(icon,offer.rewardType=="DNA"?MidnightIconAtlas.Icon.Dna:offer.rewardType=="GROWTH_CELL"?MidnightIconAtlas.Icon.Cells:MidnightIconAtlas.Icon.Coin);
                    else
                    {
                        var sprite=Resources.Load<LobbyUnitPortraitCatalog>(LobbyUnitPortraitCatalog.ResourcePath)?.Find(offer.alienId,offer.grade);
                        if(sprite!=null){var image=icon.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;}
                        else Label(icon,"Symbol","◆",70,0,0,1,1).color=new Color(.77f,.64f,1f);
                    }
                    bool discounted=offer.regularPrice>offer.price;
                    string product=offer.rewardType=="GOLD"?"골드":offer.rewardType=="DNA"?"왹져DNA":offer.rewardType=="GROWTH_CELL"?"성장세포":AlienName?.Invoke(offer.alienId)??"유닛 조각";
                    Label(card,"Name",$"{product}\nx{offer.rewardAmount:N0}",32,.06f,.30f,.94f,.51f);
                    if(discounted)Label(card,"RegularPrice",$"정상가 {offer.regularPrice:N0}",23,.05f,.25f,.95f,.31f).color=MidnightLobbyTheme.Muted;
                    Label(card,"Limit",offer.purchased?"구매 완료":offer.remainingPurchases<0?"":$"남은 구매 {offer.remainingPurchases}회",23,.04f,.19f,.96f,.25f).color=MidnightLobbyTheme.Muted;
                    var buy=Button(card,"Buy",offer.purchased?"완료":$"{offer.price:N0}",.05f,.025f,.95f,.185f);
                    PriceButton(buy,offer.currencyType,36);
                    buy.onClick.AddListener(()=>purchase(offer));buy.interactable=offer.CanBuy&&!busy;buyButtons.Add(buy);cards[offer.offerId]=card;
                }
            }
        }
        rerollText.text=catalog.adAvailable?$"광고 리롤 {catalog.rerollsRemaining}회  ↻":"광고 연결 준비 중";
        rerollButton.interactable=!busy&&catalog.adAvailable&&catalog.rerollsRemaining>0;
        if(!adInHeader){var rect=(RectTransform)rerollButton.transform;rect.anchorMin=new Vector2(.06f,0);rect.anchorMax=new Vector2(.94f,0);}
        statusText.text="";RefreshMidnightStatus();ApplyMidnightSafeBottom(Mathf.Max(0,midnightSafeBottom));
    }
    private RectTransform Row(string name,float height)
    {
        var row=Rect(name,content);row.gameObject.AddComponent<LayoutElement>().preferredHeight=height;return row;
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
        var rect=(RectTransform)scroll.transform;rect.offsetMax=new Vector2(rect.offsetMax.x,visible?-385:-285);
    }
    private static void PriceButton(Button button,string currency,int fontSize)
    {
        var label=button.GetComponentInChildren<Text>();MidnightLobbyTheme.Typography(label,fontSize);
        LobbyFinalPresentation.Place(label.rectTransform,.24f,.06f,.96f,.94f);
        var icon=Rect("CurrencyIcon",button.transform);Stretch(icon,.065f,.21f,.23f,.79f);
        MidnightIconAtlas.Place(icon,currency=="GOLD"?MidnightIconAtlas.Icon.Coin:MidnightIconAtlas.Icon.Diamond);
    }
}
