package com.denfense.server.balance;

public record MutationConfigBalance(
        String modeId,
        int initialActivationCost,
        int rerollCost1,
        int rerollCost2,
        int rerollCost3,
        int rerollCost4,
        int rerollCostAfterMax,
        int injectorReplaceCost) {
}
