package com.denfense.server.service.balance;

public record AlienUpgradeCostBalance(
        int currentLevel,
        int requiredPieces,
        int requiredGold,
        int requiredGrowthCell
) {
}
