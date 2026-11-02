package com.denfense.server.balance;

public record ShopPolicyBalance(String policyId, String resetZone, int luckyWeight, int luckyTotalWeight,
        int dailyAdRerolls, String discountOfferId, int discountPrice, int weeklyDiscountCount,
        int adChallengeTtlSeconds) {}
