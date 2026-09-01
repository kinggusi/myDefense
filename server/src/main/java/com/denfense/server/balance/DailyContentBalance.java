package com.denfense.server.balance;

public record DailyContentBalance(
        String contentType,
        int stage,
        int repeatReward,
        int firstClearReward,
        boolean enabled) {
}
