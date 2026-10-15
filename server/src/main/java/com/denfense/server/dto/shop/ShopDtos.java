package com.denfense.server.dto.shop;

import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import java.util.List;
import java.util.UUID;

/** Server quotes are mandatory; a stale quote never silently charges a new price. */
public final class ShopDtos {
    private ShopDtos() {}
    public record Wallet(int gold, int diamond, int universalPiece, int growthCell) {}
    public record Offer(String offerId, String slotKey, String category, String currencyType, int price,
            int regularPrice, String rewardType, int rewardAmount, String grade, long alienId,
            int remainingPurchases, boolean purchased, int discountRemaining) {}
    public record Catalog(String serverTime, String dayKey, String weekKey, String dailyResetAt,
            String weeklyResetAt, String catalogRevision, Wallet wallet, List<Offer> offers,
            int rerollsRemaining, boolean adAvailable, boolean developmentAd, int luckyPurchasesRemaining) {}
    public record PurchaseRequest(UUID requestId, String offerId, String slotKey, String expectedRevision,
            int expectedPrice, String expectedCurrency) {}
    public record PurchaseResponse(UUID requestId, String offerId, int chargedPrice, String currencyType,
            String rewardType, int rewardAmount, long alienId, GachaPurchaseResponseDto gacha, Catalog catalog) {}
    public record ChallengeRequest(UUID requestId) {}
    public record Challenge(UUID challengeId, String expiresAt, boolean developmentAd) {}
    public record RerollRequest(UUID requestId, UUID challengeId, String expectedRevision) {}
    public record RerollResponse(UUID requestId, Catalog catalog) {}
}
