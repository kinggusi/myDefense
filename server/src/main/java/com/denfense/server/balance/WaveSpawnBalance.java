package com.denfense.server.balance;

import com.fasterxml.jackson.annotation.JsonPropertyOrder;

import java.math.BigDecimal;

@JsonPropertyOrder({"spawnGroupId", "order", "monsterId", "spawnCountPerField", "startDelaySeconds", "spawnIntervalSeconds", "lanePolicy"})
public record WaveSpawnBalance(
        String spawnGroupId,
        int order,
        String monsterId,
        int spawnCountPerField,
        BigDecimal startDelaySeconds,
        BigDecimal spawnIntervalSeconds,
        String lanePolicy
) {
}
