package com.denfense.server.balance;

public record QuestBalance(
        String questId,
        QuestCycleType cycleType,
        String title,
        String description,
        String conditionId,
        long targetAmount,
        int activityPoints,
        int rewardGold,
        int rewardUniversalPiece,
        int rewardDiamond,
        int sortOrder,
        boolean enabled
) {
}
