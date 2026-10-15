using System;
using System.Linq;
using MyDefense.Auth;
using MyDefense.Shop;
using UnityEngine;

/// <summary>Account-bound shop UI. Prices, outcomes, limits and ad rewards are always decided by Spring.</summary>
public sealed class ShopCatalogController : MonoBehaviour
{
    private LobbyManager lobby;
    private ShopCatalogView view;
    private ShopCatalog catalog;
    private ShopPendingRequestStore store;
    private ShopPendingRequest pending;
    private long userId;
    private string endpoint;
    private bool requesting;
    private bool shown;
    private bool storageError;
    private bool catalogFresh;
    private string focusReward;
    private GachaShopController gacha;
    public ShopCatalog Catalog => catalog;
    public bool HasPending => pending!=null;
    private bool ValidAccount => this!=null&&AuthSession.Instance?.User?.userId==userId&&NetworkManager.Instance!=null&&NetworkManager.Instance.BaseUrl==endpoint;

    public void Initialize(LobbyManager owner)
    {
        lobby=owner;userId=AuthSession.Instance?.User?.userId??0;
        if(userId<=0||NetworkManager.Instance==null)return;
        endpoint=NetworkManager.Instance.BaseUrl;
        store=new ShopPendingRequestStore(endpoint,userId);
        try{pending=store.Load();}catch(Exception){storageError=true;}
        var shop=owner.viewObjects!=null&&owner.viewObjects.Length>0?owner.viewObjects[0]:null;
        if(shop==null)return;
        view=shop.GetComponentInChildren<ShopCatalogView>(true);
        if(view==null)
        {
            var prefab=Resources.Load<GameObject>("Prefabs/Shop/ShopCatalog");
            view=prefab!=null?Instantiate(prefab,shop.transform,false).GetComponent<ShopCatalogView>():ShopCatalogView.Build(shop.transform);
        }
        HideLegacyPlaceholder(shop.transform);
        gacha=FindFirstObjectByType<GachaShopController>(FindObjectsInactive.Include);
        view.refreshButton.onClick.AddListener(Refresh);
        view.AlienName = owner.GetAlienDisplayName;
        view.rerollButton.onClick.AddListener(StartAd);
        ConnectGoldPlus();
    }
    public static void HideLegacyPlaceholder(Transform shopRoot)
    {
        // Old entry and all dummy sections are nested here; GachaScreen is a separate Canvas child.
        var legacy = shopRoot == null ? null : shopRoot.Find("Shop_ScrollView");
        if(legacy != null) legacy.gameObject.SetActive(false);
    }
    public void SetGachaVisible(bool visible)
    {
        if(view != null) view.gameObject.SetActive(!visible);
    }
    private void ConnectGoldPlus()
    {
        var parent=lobby.text_Gold==null?null:lobby.text_Gold.transform.parent;
        var plus=parent==null?null:parent.Find("Plus Button")?.GetComponent<UnityEngine.UI.Button>();
        if(plus!=null){plus.onClick.RemoveAllListeners();plus.onClick.AddListener(()=>OpenReward("GOLD"));}
    }
    public void SetLobbyTab(int tab)
    {
        shown=tab==0;
        if(!shown){view?.CloseModal();return;}
        Refresh();
    }
    public void OpenReward(string reward)
    {
        focusReward=reward;lobby.OpenTab(0);
    }
    public void Refresh()
    {
        if(view==null||!ValidAccount||requesting)return;
        requesting=true;catalogFresh=false;view.SetBusy(true);view.statusText.text="상점 정보를 확인하는 중...";
        NetworkManager.Instance.Get("/shop/catalog",json=>
        {
            if(!ValidAccount)return;requesting=false;
            try{Apply(JsonUtility.FromJson<ShopCatalog>(json));}
            catch(Exception){view.statusText.text="상점 응답을 읽지 못했습니다. 다시 조회해 주세요.";view.refreshButton.interactable=true;return;}
            if(storageError)view.Message("구매 복구 오류","저장된 요청을 읽지 못했습니다. 고객 지원이 필요합니다.");
            else if(pending!=null&&shown)view.Message("처리 확인 필요","이전 구매·새로고침 요청의 결과가 아직 확인되지 않았습니다.\n같은 요청으로 안전하게 확인합니다.",Retry);
        },error=>{if(!ValidAccount)return;requesting=false;view.statusText.text="상점 조회 실패 · 다시 조회해 주세요.";view.refreshButton.interactable=true;});
    }
    private void Apply(ShopCatalog value)
    {
        if(value==null||value.wallet==null||value.offers==null||string.IsNullOrEmpty(value.catalogRevision))throw new InvalidOperationException();
        catalog=value;catalogFresh=true;view.SetBusy(requesting||pending!=null||storageError);view.Render(value,SelectOffer);
        lobby.UpdateQuestWallet(value.wallet.gold,value.wallet.universalPiece,value.wallet.diamond);
        if(lobby.text_GrowthCell!=null)lobby.text_GrowthCell.text=value.wallet.growthCell.ToString("N0");
        gacha?.SetCatalogPrices(value.offers.FirstOrDefault(o=>o.rewardType=="GACHA"&&o.rewardAmount==1)?.price,
            value.offers.FirstOrDefault(o=>o.rewardType=="GACHA"&&o.rewardAmount==10)?.price,
            value.offers.FirstOrDefault(o=>o.rewardType=="GACHA"&&o.rewardAmount==10)?.discountRemaining??0);
        if(!string.IsNullOrEmpty(focusReward))
        {
            var offer=value.offers.FirstOrDefault(o=>o.rewardType==focusReward&&o.category!="LUCKY");
            if(offer!=null)view.FocusOffer(offer.offerId);focusReward=null;
        }
    }
    public void RequestGacha(string offerId)
    {
        if(catalog==null||!catalogFresh){Refresh();return;}
        var offer=catalog.offers.FirstOrDefault(o=>o.offerId==ShopPresentation.GachaOfferId(offerId));
        if(offer!=null)SelectOffer(offer);
    }
    public void SelectOffer(ShopOffer offer)
    {
        if(requesting||pending!=null||storageError||!ValidAccount||!catalogFresh||catalog==null||offer==null||!offer.CanBuy)return;
        var request=ShopPurchaseRequest.Confirm(catalog,offer);
        view.Confirm(offer,()=>
        {
            if(!ValidAccount||requesting||pending!=null)return;
            if(Queue("purchase",JsonUtility.ToJson(request)))Retry();
        });
    }
    private bool Queue(string kind,string json)
    {
        try{store.Save(kind,json);pending=new ShopPendingRequest{kind=kind,json=json};return true;}
        catch(Exception){storageError=true;view.Message("구매 중단","요청을 저장하지 못해 결제를 진행하지 않았습니다.");return false;}
    }
    public void Retry()
    {
        if(!ValidAccount||pending==null||requesting||storageError)return;
        requesting=true;view.SetBusy(true);
        if(pending.kind=="purchase")
        {
            var request=JsonUtility.FromJson<ShopPurchaseRequest>(pending.json);
            NetworkManager.Instance.PostJsonAsync<ShopPurchaseRequest,ShopPurchaseResponse>("/shop/purchases",request,result=>
            {
                if(!ValidAccount)return;requesting=false;
                if(result!=null&&result.IsSuccess&&result.Data?.catalog!=null&&result.Data.requestId==request.requestId)
                {
                    if(!ClearPending())return;
                    // A replayed receipt contains a historical catalog. Never publish it as current account state.
                    Refresh();lobby.LoadLobbyData();
                    if(result.Data.gacha!=null&&gacha!=null)gacha.PresentShopPurchase(result.Data.gacha);
                    else view.Message("구매 완료",$"{result.Data.rewardAmount:N0}개 지급 완료\n사용 {result.Data.chargedPrice:N0} {ShopPresentation.Currency(result.Data.currencyType)}");
                }
                else Failure(result?.StatusCode??0,result?.Error?.code,result?.IsSuccess==true);
            });
        }
        else if(pending.kind=="reroll")
        {
            var request=JsonUtility.FromJson<ShopRerollRequest>(pending.json);
            NetworkManager.Instance.PostJsonAsync<ShopRerollRequest,ShopRerollResponse>("/shop/rerolls",request,result=>
            {
                if(!ValidAccount)return;requesting=false;
                if(result!=null&&result.IsSuccess&&result.Data?.catalog!=null&&result.Data.requestId==request.requestId)
                {if(!ClearPending())return;Refresh();view.Message("새로고침 완료","미구매 상품이 변경되었습니다.\n구매 완료 상품과 구매 한도는 유지됩니다.");}
                else Failure(result?.StatusCode??0,result?.Error?.code,result?.IsSuccess==true);
            });
        }
        else{requesting=false;storageError=true;view.Message("복구 오류","알 수 없는 저장 요청입니다. 고객 지원이 필요합니다.");}
    }
    private bool ClearPending()
    {
        try{store.Clear();pending=null;return true;}
        catch(Exception){storageError=true;view.Message("저장 확인 필요","서버 처리는 완료됐지만 로컬 기록을 정리하지 못했습니다. 재시작 후 같은 요청을 확인하세요.");return false;}
    }
    private void Failure(long status,string code,bool invalidSuccess)
    {
        if(invalidSuccess||!ShopPresentation.IsDefinitiveRejection(status,code))
        {
            string pendingMessage=status==401||status==403 ? "로그인 상태를 복원한 뒤 같은 요청으로 다시 확인해 주세요. 구매 기록은 보존됩니다." :
                "응답을 확인하지 못했습니다. 같은 요청 재시도는 중복 결제하지 않습니다.";
            view.Message("결과 확인 필요",pendingMessage,Retry);view.refreshButton.interactable=true;return;
        }
        if(!ClearPending())return;
        string message=code=="SHOP_QUOTE_CHANGED"?"가격 또는 상품이 갱신됐습니다. 다시 선택하고 변경된 가격을 확인해 주세요.":
            code=="INSUFFICIENT_ACCOUNT_GOLD"?"골드가 부족합니다.":code=="INSUFFICIENT_DIAMOND"?"다이아가 부족합니다.":
            code=="SHOP_LIMIT_REACHED"?"구매·새로고침 한도에 도달했습니다.":"요청이 거절되었습니다. 상점 정보를 다시 확인해 주세요.";
        view.Message("처리되지 않음",message);Refresh();
    }
    private bool CanUseDevelopmentAd => ShopPresentation.DevelopmentAdAllowed(Application.isEditor||Debug.isDebugBuild,
        Environment.GetEnvironmentVariable("MYDEFENSE_ENV"),endpoint,catalog);
    private void StartAd()
    {
        if(!ValidAccount||catalog==null||requesting||pending!=null||storageError)return;
        // Swap this boundary for an SDK + server-to-server verification provider at launch. Never trust a client boolean.
        if(!CanUseDevelopmentAd){view.Message("광고 준비 중","운영 광고 서비스가 아직 연결되지 않았습니다.");return;}
        requesting=true;view.SetBusy(true);
        NetworkManager.Instance.PostJsonAsync<ShopChallengeRequest,ShopChallenge>("/shop/reroll-challenges",new ShopChallengeRequest{requestId=Guid.NewGuid().ToString()},result=>
        {
            if(!ValidAccount)return;requesting=false;
            if(result?.IsSuccess!=true||result.Data==null||!result.Data.developmentAd||!CanUseDevelopmentAd)
            {Apply(catalog);view.Message("광고 실행 실패","횟수는 차감되지 않았습니다.");return;}
            var challenge=result.Data;
            IShopRewardedAdProvider provider = CanUseDevelopmentAd ? (IShopRewardedAdProvider)new DevelopmentShopAdProvider(view.ShowDevelopmentAd) : new UnavailableShopAdProvider();
            provider.Show(outcome=>
            {
                if(!ValidAccount)return;
                if(outcome==ShopAdOutcome.Completed)CompleteDevelopmentAd(challenge);
                else {Apply(catalog);view.Message(outcome==ShopAdOutcome.Cancelled?"광고 취소":"광고 실패","상품과 남은 횟수는 바뀌지 않았습니다.");}
            });
        });
    }
    private void CompleteDevelopmentAd(ShopChallenge challenge)
    {
        if(!ValidAccount||!CanUseDevelopmentAd||requesting||!Guid.TryParse(challenge.challengeId,out _))return;
        requesting=true;view.SetBusy(true);
        NetworkManager.Instance.PostJson("/shop/development/ad-challenges/"+challenge.challengeId+"/complete","{}",json=>
        {
            if(!ValidAccount)return;requesting=false;
            var request=new ShopRerollRequest{requestId=Guid.NewGuid().ToString(),challengeId=challenge.challengeId,expectedRevision=catalog.catalogRevision};
            if(Queue("reroll",JsonUtility.ToJson(request)))Retry();
        },error=>{if(!ValidAccount)return;requesting=false;Apply(catalog);view.Message("광고 확인 실패","새로고침하지 않았으며 횟수는 차감되지 않았습니다.");});
    }
}
