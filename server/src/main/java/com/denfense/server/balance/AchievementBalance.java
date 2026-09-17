package com.denfense.server.balance;

public record AchievementBalance(
        String achievementId,
        String category,
        int tier,
        String title,
        String description,
        String conditionId,
        long targetAmount,
        int rewardGold,
        int rewardUniversalPiece,
        int rewardDiamond,
        int sortOrder,
        boolean enabled
) {
}
