package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.math.BigDecimal;

@JsonPropertyOrder({"mapId", "order", "hpMultiplier", "speedMultiplier", "bossHpMultiplier", "enabled"})
public record PlanetBattleBalance(
        String mapId,
        int order,
        BigDecimal hpMultiplier,
        BigDecimal speedMultiplier,
        BigDecimal bossHpMultiplier,
        boolean enabled
) {
}
