package com.denfense.server.balance;

import java.math.BigDecimal;

public record MutationSpecBalance(
        String mutationType,
        boolean enabled,
        boolean injectorEnabled,
        boolean randomActivationEnabled,
        int weight,
        BigDecimal attackMultiplier,
        BigDecimal mpMultiplier,
        BigDecimal attackSpeedMultiplier,
        BigDecimal rangeMultiplier,
        BigDecimal goldMultiplier) {
}
