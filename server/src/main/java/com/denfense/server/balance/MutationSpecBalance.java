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
        BigDecimal goldMultiplier,
        String mechanic,
        BigDecimal splashRadius,
        BigDecimal splashDamageMultiplier,
        BigDecimal bossDamageMultiplier,
        BigDecimal dotDamageMultiplier,
        int dotTickCount,
        BigDecimal dotTickIntervalSeconds,
        BigDecimal slowMultiplier,
        BigDecimal slowDurationSeconds,
        int goldPerHit,
        BigDecimal gambleSuccessChance,
        BigDecimal gambleSuccessMultiplier,
        BigDecimal gambleFailureMultiplier) {
}
