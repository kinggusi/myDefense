using System;
using System.Security.Cryptography;
using System.Text;
using GachaShop.Core;
using UnityEngine;

namespace MyDefense.Shop
{
    [Serializable] public sealed class ShopWallet { public int gold, diamond, universalPiece, growthCell; }
    [Serializable] public sealed class ShopOffer
    {
        public string offerId, slotKey, category, currencyType, rewardType, grade;
        public int price, regularPrice, rewardAmount, remainingPurchases, discountRemaining;
        public long alienId;
        public bool purchased;
        public bool CanBuy => !purchased && remainingPurchases != 0;
    }
    [Serializable] public sealed class ShopCatalog
    {
        public string serverTime, dayKey, weekKey, dailyResetAt, weeklyResetAt, catalogRevision;
        public ShopWallet wallet;
        public ShopOffer[] offers;
        public int rerollsRemaining, luckyPurchasesRemaining;
        public bool adAvailable, developmentAd;
    }
    [Serializable] public sealed class ShopPurchaseRequest
    {
        public string requestId, offerId, slotKey, expectedRevision, expectedCurrency;
        public int expectedPrice;
        public static ShopPurchaseRequest Confirm(ShopCatalog catalog, ShopOffer offer) => new ShopPurchaseRequest
        { requestId = Guid.NewGuid().ToString(), offerId = offer.offerId, slotKey = offer.slotKey ?? "",
          expectedRevision = catalog.catalogRevision, expectedCurrency = offer.currencyType, expectedPrice = offer.price };
    }
    [Serializable] public sealed class ShopPurchaseResponse
    {
        public string requestId, offerId, currencyType, rewardType;
        public int chargedPrice, rewardAmount;
        public long alienId;
        public GachaPurchaseResponse gacha;
        public ShopCatalog catalog;
    }
    [Serializable] public sealed class ShopChallengeRequest { public string requestId; }
    [Serializable] public sealed class ShopChallenge { public string challengeId, expiresAt; public bool developmentAd; }
    [Serializable] public sealed class ShopRerollRequest { public string requestId, challengeId, expectedRevision; }
    [Serializable] public sealed class ShopRerollResponse { public string requestId; public ShopCatalog catalog; }
    [Serializable] public sealed class ShopPendingRequest { public string kind, json; }

    public static class ShopPresentation
    {
        public static string Currency(string value) => value == "GOLD" ? "골드" : "다이아";
        public static string Title(ShopOffer offer, string alienName = null)
        {
            switch (offer.rewardType)
            {
                case "GACHA": return "왹져 " + offer.rewardAmount + "회 뽑기";
                case "GOLD": return "골드 " + offer.rewardAmount.ToString("N0");
                case "DNA": return "왹져DNA " + offer.rewardAmount + "개";
                case "GROWTH_CELL": return "성장세포 " + offer.rewardAmount + "개";
                default: return (string.IsNullOrEmpty(alienName) ? offer.grade + " 유닛" : alienName) + " 조각 " + offer.rewardAmount + "개";
            }
        }
        public static bool DevelopmentAdAllowed(bool isDevelopment, string environment, string endpoint, ShopCatalog catalog)
            => isDevelopment && (environment == "local" || environment == "dev") && catalog != null
               && catalog.adAvailable && catalog.developmentAd && Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
               && uri.IsLoopback && (uri.Scheme == "http" || uri.Scheme == "https");
        public static bool Retryable(long status) => status == 0 || status >= 500 || status == 408 || status == 429;
        public static string GachaOfferId(string legacyId) => legacyId == "ALIEN_GACHA_SINGLE" ? "GACHA_SINGLE" : legacyId == "ALIEN_GACHA_TEN" ? "GACHA_TEN" : legacyId;
        public static bool IsDefinitiveRejection(long status,string code)
        {
            if(status!=400&&status!=409&&status!=422)return false;
            switch(code)
            {
                case "SHOP_QUOTE_CHANGED": case "SHOP_LIMIT_REACHED": case "INSUFFICIENT_ACCOUNT_GOLD":
                case "INSUFFICIENT_DIAMOND": case "SHOP_PRODUCT_INACTIVE": case "SHOP_PRODUCT_NOT_FOUND":
                case "SHOP_BALANCE_CAP": case "SHOP_AD_INVALID": case "SHOP_AD_UNAVAILABLE": return true;
                default:return false;
            }
        }
        public static string Scope(string endpoint, long userId)
        {
            using (var sha = SHA256.Create())
                return "MyDefense.Shop.Pending." + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(endpoint + "|" + userId))).Replace("-", "");
        }
    }
    public sealed class ShopPendingRequestStore
    {
        private readonly string key;
        public ShopPendingRequestStore(string endpoint, long userId) { key = ShopPresentation.Scope(endpoint, userId); }
        public ShopPendingRequest Load()
        {
            if (!PlayerPrefs.HasKey(key)) return null;
            try
            {
                var value=JsonUtility.FromJson<ShopPendingRequest>(PlayerPrefs.GetString(key));
                if(value==null||string.IsNullOrEmpty(value.json))throw new FormatException();
                if(value.kind=="purchase")
                {
                    var request=JsonUtility.FromJson<ShopPurchaseRequest>(value.json);
                    if(request==null||!Guid.TryParse(request.requestId,out _)||string.IsNullOrEmpty(request.offerId)
                       ||string.IsNullOrEmpty(request.expectedRevision)||string.IsNullOrEmpty(request.expectedCurrency)||request.expectedPrice<0)throw new FormatException();
                }
                else if(value.kind=="reroll")
                {
                    var request=JsonUtility.FromJson<ShopRerollRequest>(value.json);
                    if(request==null||!Guid.TryParse(request.requestId,out _)||!Guid.TryParse(request.challengeId,out _)
                       ||string.IsNullOrEmpty(request.expectedRevision))throw new FormatException();
                }
                else throw new FormatException();
                return value;
            }
            catch (Exception) { throw new InvalidOperationException("저장된 구매 요청을 읽지 못했습니다. 지원 요청이 필요합니다."); }
        }
        public void Save(string kind, string json) { PlayerPrefs.SetString(key, JsonUtility.ToJson(new ShopPendingRequest {kind=kind,json=json})); PlayerPrefs.Save(); }
        public void Clear() { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
    }
}
