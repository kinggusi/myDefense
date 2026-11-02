package com.denfense.server.balance;

public record ShopOfferBalance(String offerId, String category, String currencyType, int price,
        String rewardType, int rewardAmount, String grade, String gachaProductId,
        String limitPeriod, int limitCount, String limitGroup, boolean active, int displayOrder) {}
