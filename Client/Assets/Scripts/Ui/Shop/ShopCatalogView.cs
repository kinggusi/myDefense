using System;
using System.Collections.Generic;
using System.Linq;
using MyDefense.Lobby;
using MyDefense.Shop;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopCatalogView : MonoBehaviour
{
    public Text statusText;
    public Text walletText;
    public Text discountText;
    public Text rerollText;
    public Button rerollButton;
    public Button refreshButton;
    public RectTransform content;
    public ScrollRect scroll;
    private readonly List<Button> buyButtons = new List<Button>();
    private readonly Dictionary<string, RectTransform> cards = new Dictionary<string, RectTransform>();
    private GameObject modal;
    private bool busy;
    public Func<long,string> AlienName;

    public static ShopCatalogView Build(Transform parent)
    {
        var root = Rect("ShopCatalog", parent); Stretch(root, 0, 0, 1, 1);
        var view = root.gameObject.AddComponent<ShopCatalogView>();
        Label(root,"Title","상점",44,.07f,.845f,.5f,.89f,TextAnchor.MiddleLeft);
        view.statusText=Label(root,"Status","상품을 불러오는 중...",24,.07f,.785f,.93f,.812f,TextAnchor.MiddleLeft);
        view.walletText=Label(root,"Wallet","",25,.07f,.815f,.93f,.845f,TextAnchor.MiddleLeft);
        view.discountText=Label(root,"Discount","",24,.07f,.755f,.68f,.785f,TextAnchor.MiddleLeft);
        view.refreshButton=Button(root,"Refresh","조회",.77f,.755f,.93f,.797f);
        var viewport=Rect("Viewport",root);Stretch(viewport,.065f,.205f,.935f,.748f);
        var clip=viewport.gameObject.AddComponent<Image>();clip.color=new Color(.005f,.02f,.04f,.88f);
        viewport.gameObject.AddComponent<RectMask2D>();
        view.content=Rect("Products",viewport);
        view.content.anchorMin=new Vector2(0,1);view.content.anchorMax=new Vector2(1,1);view.content.pivot=new Vector2(.5f,1);
        view.content.anchoredPosition=Vector2.zero;view.content.sizeDelta=Vector2.zero;
        var layout=view.content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(14,14,12,24);layout.spacing=12;
        layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        var fit=view.content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        view.scroll=viewport.gameObject.AddComponent<ScrollRect>();view.scroll.viewport=viewport;view.scroll.content=view.content;
        view.scroll.horizontal=false;view.scroll.vertical=true;view.scroll.movementType=ScrollRect.MovementType.Clamped;
        view.scroll.scrollSensitivity=32f;
        view.rerollButton=Button(root,"AdReroll","광고 보고 새로고침",.07f,.143f,.93f,.19f);
        view.rerollText=view.rerollButton.GetComponentInChildren<Text>();
        Label(root,"ResetNotice","매일 00:00 · 매주 월요일 00:00 갱신 (한국 시간)",21,.07f,.108f,.93f,.14f);
        return view;
    }

    public void Render(ShopCatalog catalog,Action<ShopOffer> purchase)
    {
        if(catalog==null)return;
        foreach(Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        buyButtons.Clear();cards.Clear();
        walletText.text=catalog.wallet==null?"":$"골드 {catalog.wallet.gold:N0}   |   다이아 {catalog.wallet.diamond:N0}";
        var ten=(catalog.offers??Array.Empty<ShopOffer>()).FirstOrDefault(o=>o.rewardType=="GACHA"&&o.rewardAmount==10);
        discountText.text=ten==null?"":$"10회 뽑기 주간 할인 남음 {ten.discountRemaining}회";
        string previous=null;
        foreach(var offer in catalog.offers??Array.Empty<ShopOffer>())
        {
            if(offer==null)continue;
            if(previous!=offer.category)
            {
                previous=offer.category;
                string title=previous=="GACHA"?"왹져 뽑기":previous=="DAILY"?"골드 일일 조각":previous=="LUCKY"?"럭키 골드 특가":"다이아 보급";
                var header=Rect("Section_"+previous,content);header.gameObject.AddComponent<LayoutElement>().preferredHeight=49;
                Label(header,"SectionTitle",title,29,.01f,0,.99f,1,TextAnchor.MiddleLeft).color=new Color(.4f,.84f,.86f);
            }
            var card=Rect("Offer_"+offer.offerId,content);card.gameObject.AddComponent<LayoutElement>().preferredHeight=161;
            Frame(card,LobbyNeonGraphic.FrameStyle.Panel);
            // The card body routes drag to this catalog's ScrollRect rather than any UI behind it.
            card.GetComponent<LobbyNeonGraphic>().raycastTarget=true;
            if(!string.IsNullOrEmpty(offer.grade))card.GetComponent<LobbyNeonGraphic>().SetGrade(offer.grade);
            var icon=Rect("Icon",card);Stretch(icon,.025f,.18f,.15f,.83f);
            if(offer.rewardType=="DNA"||offer.rewardType=="GROWTH_CELL")
                icon.gameObject.AddComponent<LobbyMaterialIconGraphic>().Configure(offer.rewardType=="DNA"?LobbyMaterialIconGraphic.MaterialKind.WakjeoDna:LobbyMaterialIconGraphic.MaterialKind.GrowthCell);
            else
            {
                var portrait=Resources.Load<LobbyUnitPortraitCatalog>(LobbyUnitPortraitCatalog.ResourcePath)?.Find(offer.alienId,offer.grade);
                if(portrait!=null){var image=icon.gameObject.AddComponent<Image>();image.sprite=portrait;image.preserveAspect=true;image.raycastTarget=false;}
                else Label(icon,"Symbol",offer.rewardType=="GOLD"?"G":offer.rewardType=="GACHA"?"?":string.IsNullOrEmpty(offer.grade)?"+":offer.grade.Substring(0,1),43,0,0,1,1);
            }
            Label(card,"Name",ShopPresentation.Title(offer,AlienName?.Invoke(offer.alienId)),29,.18f,.50f,.71f,.90f,TextAnchor.MiddleLeft);
            string limit=offer.purchased?"구매 완료":offer.remainingPurchases<0?"구매 제한 없음":$"남은 구매 {offer.remainingPurchases}회";
            if(offer.price<offer.regularPrice)limit+=$" · 정상가 {offer.regularPrice:N0}";
            Label(card,"Limit",limit,22,.18f,.16f,.71f,.48f,TextAnchor.MiddleLeft);
            var button=Button(card,"Buy",offer.purchased?"구매 완료":$"{offer.price:N0}\n{ShopPresentation.Currency(offer.currencyType)}",.73f,.16f,.97f,.84f);
            button.onClick.AddListener(()=>purchase(offer));button.interactable=offer.CanBuy&&!busy;
            buyButtons.Add(button);cards[offer.offerId]=card;
        }
        rerollText.text=$"광고 보고 새로고침 · {catalog.rerollsRemaining}회 남음";
        if(!catalog.adAvailable)rerollText.text="광고 새로고침 · 광고 연결 준비 중";
        rerollButton.interactable=!busy&&catalog.adAvailable&&catalog.rerollsRemaining>0;
        statusText.text="구매한 상품은 새로고침 후에도 구매 완료로 유지됩니다.";
    }
    public void SetBusy(bool value)
    {
        busy=value;refreshButton.interactable=!value;rerollButton.interactable=false;
        foreach(var button in buyButtons)if(button!=null)button.interactable=false;
    }
    public void FocusOffer(string offerId)
    {
        if(!cards.TryGetValue(offerId,out var card))return;
        Canvas.ForceUpdateCanvases();
        float available=Mathf.Max(0,content.rect.height-scroll.viewport.rect.height);
        content.anchoredPosition=new Vector2(0,Mathf.Clamp(-card.anchoredPosition.y-card.rect.height*.5f,0,available));
    }
    public void Confirm(ShopOffer offer,Action accepted)
    {
        ShowDialog("구매 확인",ShopPresentation.Title(offer,AlienName?.Invoke(offer.alienId))+$"\n\n{offer.price:N0} {ShopPresentation.Currency(offer.currencyType)}를 사용합니다.",
            new[]{"취소","구매"},new Action[]{CloseModal,()=>{CloseModal();accepted();}});
    }
    public void Message(string title,string message,Action retry=null)
    {
        ShowDialog(title,message,retry==null?new[]{"확인"}:new[]{"나중에","같은 요청 재시도"},
            retry==null?new Action[]{CloseModal}:new Action[]{CloseModal,()=>{CloseModal();retry();}});
    }
    public void ShowDevelopmentAd(Action complete,Action cancel,Action fail)
    {
        ShowDialog("개발용 모의 광고", "실제 광고가 아닙니다.\n운영에서는 이 화면을 사용할 수 없습니다.",new[]{"취소","실패","완료"},
            new Action[]{()=>{CloseModal();cancel();},()=>{CloseModal();fail();},()=>{CloseModal();complete();}});
    }
    private void ShowDialog(string title,string message,string[] labels,Action[] actions)
    {
        CloseModal();
        var canvas=GetComponentInParent<Canvas>();
        var overlay=Rect("ShopDialog",canvas==null?transform:canvas.transform);Stretch(overlay,0,0,1,1);
        overlay.gameObject.AddComponent<Image>().color=new Color(0,.01f,.025f,.86f);modal=overlay.gameObject;
        var panel=Rect("Panel",overlay);Stretch(panel,.075f,.30f,.925f,.70f);Frame(panel,LobbyNeonGraphic.FrameStyle.Panel);
        Label(panel,"Title",title,40,.055f,.78f,.945f,.96f);
        Label(panel,"Message",message,31,.065f,.29f,.935f,.76f);
        for(int i=0;i<labels.Length;i++)
        {
            float w=.88f/labels.Length;var button=Button(panel,"Action_"+i,labels[i],.06f+i*w,.065f,.06f+(i+1)*w-.025f,.23f);
            var action=actions[i];button.onClick.AddListener(()=>action?.Invoke());
        }
    }
    public void CloseModal(){if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;}}
    private void OnDisable(){CloseModal();}
    private void OnDestroy(){CloseModal();}
    public static RectTransform Rect(string name,Transform parent)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=LayerMask.NameToLayer("UI");go.transform.SetParent(parent,false);return (RectTransform)go.transform;
    }
    public static void Stretch(RectTransform rect,float x0,float y0,float x1,float y1)
    {rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    public static void Frame(RectTransform rect,LobbyNeonGraphic.FrameStyle style)
    {var graphic=rect.gameObject.AddComponent<LobbyNeonGraphic>();graphic.Configure(style);graphic.UseFinalStyle();graphic.raycastTarget=false;}
    public static Text Label(Transform parent,string name,string value,int size,float x0,float y0,float x1,float y1,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        var rect=Rect(name,parent);Stretch(rect,x0,y0,x1,y1);var text=rect.gameObject.AddComponent<Text>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=size;text.alignment=alignment;
        text.color=Color.white;text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Max(18,size-6);text.resizeTextMaxSize=size;return text;
    }
    public static Button Button(Transform parent,string name,string text,float x0,float y0,float x1,float y1)
    {
        var rect=Rect(name,parent);Stretch(rect,x0,y0,x1,y1);Frame(rect,LobbyNeonGraphic.FrameStyle.Button);
        var graphic=rect.GetComponent<LobbyNeonGraphic>();graphic.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=graphic;
        Label(rect,"Label",text,26,.04f,.04f,.96f,.96f);return button;
    }
}
