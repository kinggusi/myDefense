using MyDefense.Shop;
using UnityEngine;

/// <summary>UI-only fixture. It never sends HTTP or changes an account. Excluded from production scenes.</summary>
public sealed class ShopCatalogFixture : MonoBehaviour
{
    public ShopCatalogView view;
    private void Start()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var catalog=Example();
        view.Render(catalog,offer=>view.Confirm(offer,()=>view.Message("UI 테스트","실제 결제나 지급은 없습니다.")));
        view.statusText.text="UI 전용 테스트 · 실제 계정과 무관";
        view.refreshButton.onClick.AddListener(()=>view.Render(catalog,offer=>view.Confirm(offer,()=>view.Message("UI 테스트","실제 결제나 지급은 없습니다."))));
        view.rerollButton.onClick.AddListener(()=>view.ShowDevelopmentAd(()=>view.Message("완료 테스트","서버 통신 없음"),()=>view.Message("취소 테스트","서버 통신 없음"),()=>view.Message("실패 테스트","서버 통신 없음")));
#else
        gameObject.SetActive(false);
#endif
    }
    public static ShopCatalog Example() => new ShopCatalog
    {
        catalogRevision="UI_FIXTURE",dayKey="2026-09-19",weekKey="2026-09-14",wallet=new ShopWallet{gold=100000,diamond=10000},
        rerollsRemaining=3,adAvailable=true,developmentAd=true,
        offers=new[]{
            Offer("GACHA_SINGLE","GACHA","GACHA",1,300,"DIAMOND"),
            Offer("GACHA_TEN","GACHA","GACHA",10,2700,"DIAMOND",regular:3000,discount:3),
            Offer("GOLD","STATIC","GOLD",10000,500,"DIAMOND",remaining:3),
            Offer("DNA","STATIC","DNA",20,500,"DIAMOND",remaining:2),
            Offer("CELL","STATIC","GROWTH_CELL",10,300,"DIAMOND",remaining:3),
            Offer("DAILY_NORMAL","DAILY","ALIEN_PIECE",20,1000,"GOLD",remaining:1,grade:"NORMAL",alien:22),
            Offer("DAILY_EPIC","DAILY","ALIEN_PIECE",10,2500,"GOLD",remaining:1,grade:"EPIC",alien:15),
            Offer("DAILY_UNIQUE","DAILY","ALIEN_PIECE",5,5000,"GOLD",remaining:1,grade:"UNIQUE",alien:8),
            Offer("DAILY_LEGEND","DAILY","ALIEN_PIECE",2,10000,"GOLD",remaining:1,grade:"LEGEND",alien:1),
            Offer("LUCKY_DNA","LUCKY","DNA",20,30000,"GOLD",remaining:1)
        }
    };
    private static ShopOffer Offer(string id,string category,string reward,int amount,int price,string currency,int remaining=-1,int regular=0,int discount=0,string grade="",long alien=0)
        =>new ShopOffer{offerId=id,slotKey=category=="DAILY"||category=="LUCKY"?id:"",category=category,rewardType=reward,rewardAmount=amount,
            price=price,regularPrice=regular==0?price:regular,currencyType=currency,remainingPurchases=remaining,discountRemaining=discount,grade=grade,alienId=alien};
}
