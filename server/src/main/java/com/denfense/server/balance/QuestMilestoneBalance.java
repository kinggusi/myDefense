package com.denfense.server.balance;

public record QuestMilestoneBalance(
        QuestCycleType cycleType,
        int requiredActivityPoints,
        int rewardGold,
        int rewardUniversalPiece,
        int rewardDiamond,
        int sortOrder,
        boolean enabled
) {
}
