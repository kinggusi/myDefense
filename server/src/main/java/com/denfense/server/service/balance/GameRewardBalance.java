package com.denfense.server.service.balance;

public record GameRewardBalance(
        int baseRewardGold,
        int goldPerWave,
        int maxRewardGold
) {
}
