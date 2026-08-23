package com.denfense.server.balance;

import java.math.BigDecimal;

/** Canonical player-wide in-game upgrade row. Multipliers are cumulative. */
public record ResonanceBalance(
        String track,
        int level,
        int requiredGold,
        BigDecimal attackMultiplier,
        BigDecimal attackSpeedMultiplier,
        BigDecimal rangeMultiplier,
        boolean enabled
) {
}
