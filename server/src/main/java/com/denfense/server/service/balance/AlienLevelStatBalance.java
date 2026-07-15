package com.denfense.server.service.balance;

import java.math.BigDecimal;

public record AlienLevelStatBalance(
        int level,
        BigDecimal atkMultiplier,
        BigDecimal mpMultiplier,
        BigDecimal atkSpeedMultiplier,
        BigDecimal rangeMultiplier
) {
}
