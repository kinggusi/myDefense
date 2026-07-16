using System;
using System.Collections.Generic;
using System.Text;

namespace GachaShop.Core
{
    public static class GachaPurchasePresenter
    {
        public const string SingleProductId = "ALIEN_GACHA_SINGLE";
        public const string TenProductId = "ALIEN_GACHA_TEN";
        public const string PurchaseEndpoint = "/shop/gacha/purchase";

        private static readonly HashSet<string> FatalCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "USER_NOT_FOUND",
            "SHOP_PRODUCT_NOT_FOUND",
            "SHOP_PRODUCT_INACTIVE",
            "GACHA_POOL_NOT_FOUND",
            "GACHA_POOL_INACTIVE",
            "UNSUPPORTED_CURRENCY",
            "INSUFFICIENT_DIAMOND",
            "PURCHASE_REQUEST_CONFLICT",
            "ALIEN_SPEC_NOT_FOUND",
            "INVALID_REQUEST"
        };

        private static readonly HashSet<string> RetryableCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "PURCHASE_ALREADY_PROCESSING",
            "PURCHASE_RESPONSE_RESTORE_FAILED",
            "INTERNAL_SERVER_ERROR"
        };

        public static string BuildPurchaseUri(string username, string productId, string purchaseRequestId)
        {
            return PurchaseEndpoint +
                   "?username=" + Uri.EscapeDataString(username ?? string.Empty) +
                   "&productId=" + Uri.EscapeDataString(productId ?? string.Empty) +
                   "&purchaseRequestId=" + Uri.EscapeDataString(purchaseRequestId ?? string.Empty);
        }

        public static GachaPurchaseFailureKind ClassifyFailure(
            long statusCode,
            string errorCode,
            bool hasNetworkError,
            bool jsonParseFailed)
        {
            if (!string.IsNullOrEmpty(errorCode))
            {
                if (RetryableCodes.Contains(errorCode))
                {
                    return GachaPurchaseFailureKind.Retryable;
                }

                if (FatalCodes.Contains(errorCode))
                {
                    return GachaPurchaseFailureKind.Fatal;
                }
            }

            if (hasNetworkError || jsonParseFailed || statusCode == 0 || statusCode >= 500)
            {
                return GachaPurchaseFailureKind.Retryable;
            }

            if (statusCode == 400 || statusCode == 404 || statusCode == 409 || statusCode == 422)
            {
                return GachaPurchaseFailureKind.Fatal;
            }

            return GachaPurchaseFailureKind.Retryable;
        }

        public static GachaPurchaseViewModel CreateViewModel(GachaPurchaseResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            string summary =
                $"상품: {response.productId}\n" +
                $"사용 다이아: {response.price:N0}\n" +
                $"남은 다이아: {response.remainingDiamond:N0}\n" +
                $"뽑기 수: {response.drawCount}";

            var details = new StringBuilder();
            GachaDraw[] draws = response.draws ?? Array.Empty<GachaDraw>();
            foreach (GachaDraw draw in draws)
            {
                details.Append('#').Append(draw.order)
                    .Append(' ').Append(draw.grade)
                    .Append(" / Alien ").Append(draw.alienId)
                    .AppendLine();
            }

            GachaReward[] rewards = response.rewards ?? Array.Empty<GachaReward>();
            if (rewards.Length > 0)
            {
                details.AppendLine("보상:");
            }

            foreach (GachaReward reward in rewards)
            {
                details.Append("Alien ").Append(reward.alienId)
                    .Append(" ×").Append(reward.occurrenceCount)
                    .Append(" / ").Append(reward.grade);
                if (reward.newlyUnlocked)
                {
                    details.Append(" / 신규");
                }

                details.Append(" / 추가 조각 ").Append(reward.piecesAdded)
                    .Append(" / Lv.").Append(reward.currentLevel)
                    .Append(" / 현재 조각 ").Append(reward.currentPieces)
                    .AppendLine();
            }

            return new GachaPurchaseViewModel(response.remainingDiamond, summary, details.ToString().TrimEnd());
        }
    }
}
